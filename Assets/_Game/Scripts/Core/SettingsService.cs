using System.IO;
using System;
using UnityEngine.Audio;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class SettingsService : MonoBehaviour
	{
		public const int CurrentSettingsVersion = 1;

		public const string SettingsFileName = "settings.json";

		public const string SettingsTempFileName = "settings.temp.json";

		public const string MasterParameter = "MasterVolume";

		public const string MusicParameter = "MusicVolume";

		public const string SfxParameter = "SFXVolume";

		private SettingsData _current = new SettingsData();

		private AudioMixer _mixer;

		private ISaveFileSystem _files;

		private string _rootPath;

		private bool _initialized;

		private bool _dirty;

		public SettingsData Current => _current.Copy();

		public string SettingsPath => Path.Combine(_rootPath, "settings.json");

		public AudioMixerGroup MasterGroup { get; private set; }

		public AudioMixerGroup MusicGroup { get; private set; }

		public AudioMixerGroup SfxGroup { get; private set; }

		public AudioMixerGroup UiGroup { get; private set; }

		public event Action<SettingsData> SettingsChanged;

		public event Action<string> SettingsSaveFailed;

		public void Initialize(AudioMixer mixer, ISaveFileSystem fileSystem = null, string rootPath = null)
		{
			_mixer = mixer;
			_files = fileSystem ?? new PhysicalSaveFileSystem();
			_rootPath = (string.IsNullOrWhiteSpace(rootPath) ? ResolveDefaultRootPath() : rootPath);
			ResolveMixerGroups();
			_current = (TryRead(out var settings) ? Sanitize(settings) : new SettingsData());
			ApplyAll();
			_dirty = false;
			_initialized = true;
		}

		public void SetMasterVolume(float value)
		{
			SetVolume(ref _current.masterVolume, value);
		}

		public void SetMusicVolume(float value)
		{
			SetVolume(ref _current.musicVolume, value);
		}

		public void SetSfxVolume(float value)
		{
			SetVolume(ref _current.sfxVolume, value);
		}

		public void SetQuality(EidrenQualityPreset preset)
		{
			EnsureInitialized();
			if (!Enum.IsDefined(typeof(EidrenQualityPreset), preset))
			{
				preset = EidrenQualityPreset.Medium;
			}
			if (_current.quality != preset)
			{
				_current.quality = preset;
				ApplyQuality();
				PublishChanged();
			}
		}

		public void SetTargetFrameRate(int frameRate)
		{
			EnsureInitialized();
			int num = ((frameRate == 30) ? 30 : 60);
			if (_current.targetFrameRate == num)
			{
				ApplyFrameRate();
				return;
			}
			_current.targetFrameRate = num;
			ApplyFrameRate();
			PublishChanged();
		}

		public bool Save(out string error)
		{
			EnsureInitialized();
			string text = Path.Combine(_rootPath, "settings.temp.json");
			try
			{
				_files.CreateDirectory(_rootPath);
				if (_files.FileExists(text))
				{
					_files.Delete(text);
				}
				_files.WriteAllText(text, JsonUtility.ToJson(_current, prettyPrint: true));
				if (_files.FileExists(SettingsPath))
				{
					_files.Replace(text, SettingsPath);
				}
				else
				{
					_files.Move(text, SettingsPath);
				}
				_dirty = false;
				error = string.Empty;
				return true;
			}
			catch (Exception ex)
			{
				TryDelete(text);
				error = "Settings save failed: " + ex.Message;
				this.SettingsSaveFailed?.Invoke(error);
				return false;
			}
		}

		public static float LinearToDecibels(float linear)
		{
			return (linear <= 0.0001f) ? (-80f) : Mathf.Clamp(20f * Mathf.Log10(linear), -80f, 0f);
		}

		private void SetVolume(ref float target, float value)
		{
			EnsureInitialized();
			float num = Mathf.Clamp01(value);
			if (!Mathf.Approximately(target, num))
			{
				target = num;
				ApplyVolumes();
				PublishChanged();
			}
		}

		private void PublishChanged()
		{
			_dirty = true;
			this.SettingsChanged?.Invoke(Current);
		}

		private void ApplyAll()
		{
			ApplyVolumes();
			ApplyQuality();
			ApplyFrameRate();
			Screen.orientation = ScreenOrientation.LandscapeLeft;
		}

		private void ApplyVolumes()
		{
			if (!(_mixer == null))
			{
				_mixer.SetFloat("MasterVolume", LinearToDecibels(_current.masterVolume));
				_mixer.SetFloat("MusicVolume", LinearToDecibels(_current.musicVolume));
				_mixer.SetFloat("SFXVolume", LinearToDecibels(_current.sfxVolume));
			}
		}

		private void ApplyQuality()
		{
			string requested = _current.quality.ToString();
			string[] names = QualitySettings.names;
			int num = Array.FindIndex(names, (string name) => string.Equals(name, requested, StringComparison.OrdinalIgnoreCase));
			if (num >= 0 && QualitySettings.GetQualityLevel() != num)
			{
				QualitySettings.SetQualityLevel(num, applyExpensiveChanges: true);
			}
			QualitySettings.vSyncCount = 0;
		}

		private void ApplyFrameRate()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = _current.targetFrameRate;
		}

		private void ResolveMixerGroups()
		{
			MasterGroup = FindGroup("Master");
			MusicGroup = FindGroup("Music");
			SfxGroup = FindGroup("SFX");
			UiGroup = FindGroup("UI");
		}

		private AudioMixerGroup FindGroup(string name)
		{
			if (_mixer == null)
			{
				return null;
			}
			AudioMixerGroup[] array = _mixer.FindMatchingGroups(name);
			return (array.Length != 0) ? array[0] : null;
		}

		private bool TryRead(out SettingsData settings)
		{
			settings = null;
			try
			{
				if (!_files.FileExists(SettingsPath))
				{
					return false;
				}
				string text = _files.ReadAllText(SettingsPath);
				if (string.IsNullOrWhiteSpace(text) || !text.TrimStart().StartsWith("{", StringComparison.Ordinal))
				{
					return false;
				}
				settings = JsonUtility.FromJson<SettingsData>(text);
				return settings != null && settings.settingsVersion == 1;
			}
			catch
			{
				return false;
			}
		}

		private static SettingsData Sanitize(SettingsData source)
		{
			SettingsData settingsData = new SettingsData();
			settingsData.masterVolume = Mathf.Clamp01(source.masterVolume);
			settingsData.musicVolume = Mathf.Clamp01(source.musicVolume);
			settingsData.sfxVolume = Mathf.Clamp01(source.sfxVolume);
			settingsData.quality = ((!Enum.IsDefined(typeof(EidrenQualityPreset), source.quality)) ? EidrenQualityPreset.Medium : source.quality);
			settingsData.targetFrameRate = ((source.targetFrameRate == 30) ? 30 : 60);
			return settingsData;
		}

		private static string ResolveDefaultRootPath()
		{
			if (Application.isEditor && Application.isBatchMode)
			{
				return Path.Combine(Application.temporaryCachePath, "EidrenBatchSettingsTests");
			}
			return Application.persistentDataPath;
		}

		private void EnsureInitialized()
		{
			if (!_initialized)
			{
				throw new InvalidOperationException("SettingsService must be initialized before use.");
			}
		}

		private void TryDelete(string path)
		{
			try
			{
				if (_files.FileExists(path))
				{
					_files.Delete(path);
				}
			}
			catch
			{
			}
		}

		private void OnApplicationQuit()
		{
			if (_initialized && _dirty)
			{
				Save(out var _);
			}
		}
	}
}
