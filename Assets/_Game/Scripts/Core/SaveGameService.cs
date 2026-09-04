using System.IO;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed partial class SaveGameService : MonoBehaviour
	{
		public const int CurrentSaveVersion = 13;

		public const string MainFileName = "savegame.json";

		public const string BackupFileName = "savegame.backup.json";

		public const string TempFileName = "savegame.temp.json";

		private GameSession _session;

		private ContentDatabase _content;

		private SceneFlowService _sceneFlow;

		private SaveAdvancementBridge _advancement;

		private ISaveFileSystem _files;

		private string _rootPath;

		private string _createdUtc = string.Empty;

		private bool _initialized;

		private bool _newGameAwaitingFirstSave;

		private bool _saveInProgress;

		private int _lastHandledRequestFrame = -1;

		private SaveRequestReason _lastHandledRequestReason;

		public string MainPath => Path.Combine(_rootPath, "savegame.json");

		public string BackupPath => Path.Combine(_rootPath, "savegame.backup.json");

		public string TempPath => Path.Combine(_rootPath, "savegame.temp.json");

		public SaveQuarantineEntry[] LastQuarantine { get; private set; } = Array.Empty<SaveQuarantineEntry>();

		public event Action<SaveRequestReason> SaveCompleted;

		public event Action<string> SaveFailed;

		public event Action<bool> AvailabilityChanged;

		public void Initialize(GameSession session, ContentDatabase content, SceneFlowService sceneFlow, ISaveFileSystem fileSystem = null, string rootPath = null, PlayerProgressionService progression = null, TechnologyUnlockService technology = null, QuestProgressService quest = null)
		{
			if (_initialized)
			{
				Unbind();
			}
			_session = session ?? throw new ArgumentNullException("session");
			_content = content ?? throw new ArgumentNullException("content");
			_sceneFlow = sceneFlow;
			_advancement = new SaveAdvancementBridge(progression, technology, quest);
			_files = fileSystem ?? new PhysicalSaveFileSystem();
			_rootPath = (string.IsNullOrWhiteSpace(rootPath) ? ResolveDefaultRootPath() : rootPath);
			_session.SaveRequested += HandleSaveRequested;
			if (_sceneFlow != null)
			{
				_sceneFlow.SceneTransitionCompleted += HandleSceneTransitionCompleted;
			}
			_initialized = true;
			this.AvailabilityChanged?.Invoke(HasRecoverableSave());
		}

		public bool HasRecoverableSave()
		{
			return IsValidCandidate(MainPath) || IsValidCandidate(BackupPath);
		}

		public bool HasAnySaveFile()
		{
			EnsureInitialized();
			return _files.FileExists(MainPath) || _files.FileExists(BackupPath);
		}

		public void BeginNewGame()
		{
			_createdUtc = string.Empty;
			_newGameAwaitingFirstSave = true;
			LastQuarantine = Array.Empty<SaveQuarantineEntry>();
			_advancement.ResetForNewGame();
		}

		public bool SaveNow(SaveRequestReason reason, out string error)
		{
			EnsureInitialized();
			if (_session.Phase != GameSessionPhase.ActiveGame)
			{
				error = "No active game session is available to save.";
				return false;
			}
			if (_newGameAwaitingFirstSave && reason != SaveRequestReason.HomeBaseEntered)
			{
				error = "New game save is deferred until HomeBase loaded successfully.";
				return false;
			}
			if (_saveInProgress)
			{
				error = "A save operation is already in progress.";
				return false;
			}
			_saveInProgress = true;
			try
			{
				string text = DateTime.UtcNow.ToString("O");
				if (string.IsNullOrEmpty(_createdUtc))
				{
					_createdUtc = (_newGameAwaitingFirstSave ? text : (ReadCreatedUtc() ?? text));
				}
				SaveGameData saveGameData = new SaveGameMapper(_content).Capture(_session, _createdUtc, text, Application.version);
				saveGameData.progression = _advancement.Capture();
				saveGameData.quest = _advancement.CaptureQuest();
				saveGameData.quarantinedEntries = LastQuarantine;
				string contents = JsonUtility.ToJson(saveGameData, prettyPrint: true);
				try
				{
					_files.CreateDirectory(_rootPath);
					if (_files.FileExists(TempPath))
					{
						_files.Delete(TempPath);
					}
					_files.WriteAllText(TempPath, contents);
					if (_files.FileExists(MainPath) && IsValidCandidate(MainPath))
					{
						_files.Copy(MainPath, BackupPath, overwrite: true);
					}
					if (_files.FileExists(MainPath))
					{
						_files.Replace(TempPath, MainPath);
					}
					else
					{
						_files.Move(TempPath, MainPath);
					}
					_newGameAwaitingFirstSave = false;
					error = string.Empty;
					this.SaveCompleted?.Invoke(reason);
					this.AvailabilityChanged?.Invoke(obj: true);
					return true;
				}
				catch (Exception ex)
				{
					TryDeleteTemp();
					error = "Save failed: " + ex.Message;
					this.SaveFailed?.Invoke(error);
					return false;
				}
			}
			finally
			{
				_saveInProgress = false;
			}
		}

		public bool TryLoad(out string error)
		{
			EnsureInitialized();
			if (TryLoadCandidate(MainPath, out error))
			{
				return true;
			}
			string text = error;
			if (TryLoadCandidate(BackupPath, out error))
			{
				return true;
			}
			error = "Main save invalid: " + text + " Backup invalid: " + error;
			return false;
		}

		public bool TryReadSave(out SaveGameData save, out bool usedBackup, out string error)
		{
			if (TryReadCandidate(MainPath, out save, out error))
			{
				usedBackup = false;
				return true;
			}
			if (TryReadCandidate(BackupPath, out save, out error))
			{
				usedBackup = true;
				return true;
			}
			usedBackup = false;
			return false;
		}

		private bool TryLoadCandidate(string path, out string error)
		{
			if (!TryReadCandidate(path, out var data, out error))
			{
				return false;
			}
			return TryRestoreFrom(data, out error);
		}

		private bool TryRestoreFrom(SaveGameData data, out string error)
		{
			if (!SaveGameMigration.TryMigrate(data, out var result, out error))
			{
				return false;
			}
			SaveGameMapper saveGameMapper = new SaveGameMapper(_content);
			if (!saveGameMapper.TryMapToRuntime(result, out var state, out var quarantine, out error))
			{
				return false;
			}
			if (!_advancement.TryMap(result.progression, out var state2, out error))
			{
				return false;
			}
			if (!_session.TryRestoreRuntimeState(state, out error))
			{
				return false;
			}
			_advancement.Restore(state2);
			// F31-006: Queststand nach der Progression — die Ableitung fuer
			// Altbestaende liest deren frisch wiederhergestellte Erstlisten.
			_advancement.RestoreQuest(result.quest, result.player?.eidraRoster != null && result.player.eidraRoster.Length != 0);
			LastQuarantine = quarantine;
			_createdUtc = result.createdUtc;
			_newGameAwaitingFirstSave = false;
			return true;
		}

		private bool IsValidCandidate(string path)
		{
			if (!TryReadCandidate(path, out var data, out var error) || !SaveGameMigration.TryMigrate(data, out var result, out error))
			{
				return false;
			}
			GameSessionRuntimeState state;
			SaveQuarantineEntry[] quarantine;
			PlayerProgressionRuntimeState state2;
			return new SaveGameMapper(_content).TryMapToRuntime(result, out state, out quarantine, out error) && _advancement.TryMap(result.progression, out state2, out error);
		}

		private bool TryReadCandidate(string path, out SaveGameData data, out string error)
		{
			data = null;
			if (_files == null || string.IsNullOrEmpty(_rootPath))
			{
				error = "Save service is not initialized.";
				return false;
			}
			try
			{
				if (!_files.FileExists(path))
				{
					error = "Save file '" + Path.GetFileName(path) + "' is missing.";
					return false;
				}
				string text = _files.ReadAllText(path);
				if (string.IsNullOrWhiteSpace(text) || !text.TrimStart().StartsWith("{", StringComparison.Ordinal))
				{
					error = "Save content is not a JSON object.";
					return false;
				}
				data = JsonUtility.FromJson<SaveGameData>(text);
				if (data == null || data.saveVersion <= 0 || string.IsNullOrWhiteSpace(data.createdUtc) || string.IsNullOrWhiteSpace(data.lastSavedUtc) || data.player == null || data.world == null || data.player.inventory == null || data.player.inventory.Length != 16)
				{
					error = "Save JSON has an invalid or incomplete envelope.";
					return false;
				}
				error = string.Empty;
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}

		private string ReadCreatedUtc()
		{
			SaveGameData save;
			bool usedBackup;
			string error;
			return (TryReadSave(out save, out usedBackup, out error) && !string.IsNullOrWhiteSpace(save.createdUtc)) ? save.createdUtc : null;
		}

		private void HandleSaveRequested(SaveRequestReason reason)
		{
			if (_lastHandledRequestFrame != Time.frameCount || _lastHandledRequestReason != reason)
			{
				_lastHandledRequestFrame = Time.frameCount;
				_lastHandledRequestReason = reason;
				SaveNow(reason, out var _);
			}
		}

		private void HandleSceneTransitionCompleted(string sceneName)
		{
			if (string.Equals(sceneName, "HomeBase", StringComparison.Ordinal))
			{
				SaveNow(SaveRequestReason.HomeBaseEntered, out var _);
			}
		}

		private void OnApplicationQuit()
		{
			if (_initialized && _session.Phase == GameSessionPhase.ActiveGame)
			{
				SaveNow(SaveRequestReason.ApplicationQuit, out var _);
			}
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_session != null)
			{
				_session.SaveRequested -= HandleSaveRequested;
			}
			if (_sceneFlow != null)
			{
				_sceneFlow.SceneTransitionCompleted -= HandleSceneTransitionCompleted;
			}
			_initialized = false;
		}

		private void EnsureInitialized()
		{
			if (!_initialized)
			{
				throw new InvalidOperationException("SaveGameService must be initialized before use.");
			}
		}

		private static string ResolveDefaultRootPath()
		{
			if (Application.isEditor && Application.isBatchMode)
			{
				return Path.Combine(Application.temporaryCachePath, "EidrenBatchSaveTests");
			}
			return Application.persistentDataPath;
		}

		private void TryDeleteTemp()
		{
			try
			{
				if (_files.FileExists(TempPath))
				{
					_files.Delete(TempPath);
				}
			}
			catch
			{
			}
		}
	}
}
