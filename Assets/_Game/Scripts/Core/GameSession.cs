using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class GameSession : MonoBehaviour
	{
		public const string HomeNodeId = "home_base";

		public const string HomeSceneKey = "HomeBase";

		private readonly HashSet<string> _visitedWorldMapNodes = new HashSet<string>(StringComparer.Ordinal);

		private readonly HashSet<string> _eventWorldMapNodes = new HashSet<string>(StringComparer.Ordinal);

		private readonly HashSet<string> _progressFlags = new HashSet<string>(StringComparer.Ordinal);

		private readonly HashSet<string> _completedWorldMapNodes = new HashSet<string>(StringComparer.Ordinal);

		private readonly Dictionary<string, StorageContainerState> _storageStates = new Dictionary<string, StorageContainerState>(StringComparer.Ordinal);

		public bool IsInitialized { get; private set; }

		public string SessionId { get; private set; }

		public GameSessionPhase Phase { get; private set; }

		public PlayerLifeState LifeState { get; private set; }

		public string LastSafeNodeId { get; private set; }

		public string LastSafeSceneKey { get; private set; }

		public int NewGameGeneration { get; private set; }

		public string PreviousZoneId { get; private set; }

		public MapExitDirection LastExitDirection { get; private set; }

		public string RequestedTargetScene { get; private set; }

		public AreaStatus LastValidAreaStatus { get; private set; }

		public string SelectedWorldMapNodeId { get; private set; }

		public string CurrentWorldMapNodeId { get; private set; }

		public string PendingWorldMapNodeId { get; private set; }

		public string PendingWorldMapSceneKey { get; private set; }

		public bool IsWorldTravelPending => !string.IsNullOrEmpty(PendingWorldMapNodeId);

		public PlayerInventory PlayerInventory { get; private set; }

		public PlayerEquipment PlayerEquipment { get; private set; }

		public DeathBagLocation DeathBag { get; private set; }

		public WeaponProgressionState WeaponProgression { get; private set; }

		public ZoneStateService ZoneStates { get; private set; }

		public BuildingRegistry Buildings { get; private set; }

		public EidraRoster Eidra { get; private set; }

		public FarmProductionService FarmProduction { get; private set; }

		public EidraForgeDungeonService EidraForge { get; private set; }

		public EidraForgeChestService EidraForgeChests { get; private set; }

		public EidraForgeEnemyService EidraForgeEnemies { get; private set; }

		public string ActiveWeaponId { get; private set; }

		public string ActiveEidraId { get; private set; }

		public event Action<SaveRequestReason> SaveRequested;

		public void Initialize()
		{
			if (!IsInitialized)
			{
				PlayerInventory = new PlayerInventory();
				PlayerEquipment = new PlayerEquipment();
				WeaponProgression = new WeaponProgressionState();
				ZoneStates = new ZoneStateService();
				Buildings = new BuildingRegistry();
				Eidra = new EidraRoster();
				FarmProduction = new FarmProductionService(Buildings, PlayerInventory);
				EidraForge = new EidraForgeDungeonService();
				EidraForgeChests = new EidraForgeChestService(EidraForge);
				EidraForgeEnemies = new EidraForgeEnemyService(EidraForge);
				SessionId = Guid.NewGuid().ToString("N");
				Phase = GameSessionPhase.Shell;
				LifeState = PlayerLifeState.Alive;
				LastSafeNodeId = "home_base";
				LastSafeSceneKey = "HomeBase";
				ActiveWeaponId = string.Empty;
				ActiveEidraId = string.Empty;
				ResetAreaTransition();
				ResetWorldMapState();
				IsInitialized = true;
			}
		}

		public void StartNewGame()
		{
			Initialize();
			SessionId = Guid.NewGuid().ToString("N");
			NewGameGeneration++;
			Phase = GameSessionPhase.ActiveGame;
			LifeState = PlayerLifeState.Alive;
			LastSafeNodeId = "home_base";
			LastSafeSceneKey = "HomeBase";
			ActiveWeaponId = string.Empty;
			ActiveEidraId = string.Empty;
			ResetAreaTransition();
			ResetWorldMapState();
			WeaponProgression.Reset();
			PlayerEquipment.Clear();
			ZoneStates.Reset();
			Buildings.Reset();
			Eidra.Reset();
			EidraForge.ResetForNewGame();
			DeathBag = DeathBagLocation.None;
			_storageStates.Clear();
			PlayerInventory.Reset(Array.Empty<ItemStack>());
		}

		public bool RecordPlayerDeath()
		{
			Initialize();
			if (Phase != GameSessionPhase.ActiveGame || LifeState != PlayerLifeState.Alive)
			{
				return false;
			}
			LifeState = PlayerLifeState.Dead;
			CancelWorldTravel();
			LastValidAreaStatus = AreaStatus.SafeInside;
			return true;
		}

		public bool BeginRespawn(string safeNodeId, string safeSceneKey)
		{
			Initialize();
			if (Phase != GameSessionPhase.ActiveGame || LifeState != PlayerLifeState.Dead || string.IsNullOrWhiteSpace(safeNodeId) || string.IsNullOrWhiteSpace(safeSceneKey))
			{
				return false;
			}
			LastSafeNodeId = safeNodeId;
			LastSafeSceneKey = safeSceneKey;
			SelectedWorldMapNodeId = safeNodeId;
			CurrentWorldMapNodeId = safeNodeId;
			_visitedWorldMapNodes.Add(safeNodeId);
			CancelWorldTravel();
			ResetAreaTransition();
			LifeState = PlayerLifeState.Respawning;
			return true;
		}

		public void CancelRespawn()
		{
			if (LifeState == PlayerLifeState.Respawning)
			{
				LifeState = PlayerLifeState.Dead;
			}
		}

		public bool CompleteRespawn(string loadedSceneKey)
		{
			if (LifeState != PlayerLifeState.Respawning || !string.Equals(LastSafeSceneKey, loadedSceneKey, StringComparison.Ordinal))
			{
				return false;
			}
			LifeState = PlayerLifeState.Alive;
			LastValidAreaStatus = AreaStatus.SafeInside;
			return true;
		}

		public void PrepareForMainMenu(bool requestSave = true)
		{
			Initialize();
			if (requestSave)
			{
				RequestSave(SaveRequestReason.MainMenuRequested);
			}
			Phase = GameSessionPhase.Shell;
			LifeState = PlayerLifeState.Alive;
			CancelWorldTravel();
			ResetAreaTransition();
		}

		public void RestoreDeathAfterMenuFailure()
		{
			Initialize();
			Phase = GameSessionPhase.ActiveGame;
			LifeState = PlayerLifeState.Dead;
		}

		public void ConfigureContentDatabase(ContentDatabase contentDatabase)
		{
			Initialize();
			PlayerInventory.Configure(contentDatabase);
			PlayerEquipment.Configure(contentDatabase);
			PlayerInventory.ConfigureEquipment(PlayerEquipment);
		}

		public void SetAreaStatus(AreaStatus status)
		{
			Initialize();
			LastValidAreaStatus = status;
		}

		public void RecordConfirmedExit(string previousZoneId, MapExitDirection direction, string requestedTargetScene)
		{
			Initialize();
			PreviousZoneId = previousZoneId ?? string.Empty;
			LastExitDirection = direction;
			RequestedTargetScene = requestedTargetScene ?? string.Empty;
			LastValidAreaStatus = AreaStatus.TransitionConfirmed;
			RequestSave(SaveRequestReason.AreaTransitionConfirmed);
		}

		/// <summary>
		/// Rückkehr-Anker (19.08.2026): Der Verlies-Ausgang hinterlässt seine
		/// Herkunft in <see cref="PreviousZoneId"/>; der Eingang der Zielzone
		/// verbraucht sie hier genau einmal und stellt den Spieler dann vor
		/// das Portal. Der Ankunftsstatus taugt dafür nicht — den setzt der
		/// MapExitCoordinator schon beim Spawn auf SafeInside.
		/// </summary>
		public bool TryConsumeReturnFrom(string zoneId)
		{
			Initialize();
			if (!string.Equals(PreviousZoneId, zoneId, StringComparison.Ordinal))
			{
				return false;
			}
			PreviousZoneId = string.Empty;
			return true;
		}

		public void SetActiveWeaponId(string weaponId)
		{
			ActiveWeaponId = weaponId ?? string.Empty;
		}

		public void SetActiveEidraId(string eidraId)
		{
			if (!string.IsNullOrWhiteSpace(eidraId))
			{
				ActiveEidraId = eidraId;
			}
		}

		public void SelectWorldMapNode(string nodeId)
		{
			Initialize();
			SelectedWorldMapNodeId = nodeId ?? string.Empty;
		}

		public bool BeginWorldTravel(string nodeId, string sceneKey)
		{
			Initialize();
			if (IsWorldTravelPending || string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(sceneKey))
			{
				return false;
			}
			PendingWorldMapNodeId = nodeId;
			PendingWorldMapSceneKey = sceneKey;
			return true;
		}

		public bool CompleteWorldTravel(string loadedSceneKey)
		{
			if (!IsWorldTravelPending || !string.Equals(PendingWorldMapSceneKey, loadedSceneKey, StringComparison.Ordinal))
			{
				return false;
			}
			CurrentWorldMapNodeId = PendingWorldMapNodeId;
			SelectedWorldMapNodeId = PendingWorldMapNodeId;
			_visitedWorldMapNodes.Add(PendingWorldMapNodeId);
			ClearPendingWorldTravel();
			return true;
		}

		public void CancelWorldTravel()
		{
			ClearPendingWorldTravel();
		}

		public bool IsWorldMapNodeVisited(string nodeId)
		{
			return !string.IsNullOrWhiteSpace(nodeId) && _visitedWorldMapNodes.Contains(nodeId);
		}

		public string[] GetVisitedWorldMapNodeIds()
		{
			string[] array = new string[_visitedWorldMapNodes.Count];
			_visitedWorldMapNodes.CopyTo(array);
			return array;
		}

		public string[] GetEventWorldMapNodeIds()
		{
			return CopySet(_eventWorldMapNodes);
		}

		public string[] GetProgressFlags()
		{
			return CopySet(_progressFlags);
		}

		public string[] GetCompletedWorldMapNodeIds()
		{
			return CopySet(_completedWorldMapNodes);
		}

		public StorageContainerState[] GetStorageStates()
		{
			StorageContainerState[] array = new StorageContainerState[_storageStates.Count];
			int num = 0;
			foreach (StorageContainerState value in _storageStates.Values)
			{
				array[num++] = value.Copy();
			}
			return array;
		}

		public GameSessionRuntimeState ExportRuntimeState()
		{
			Initialize();
			return new GameSessionRuntimeState
			{
				ActiveWeaponId = ActiveWeaponId,
				ActiveEidraId = ActiveEidraId,
				LastSafeNodeId = LastSafeNodeId,
				LastSafeSceneKey = LastSafeSceneKey,
				CurrentNodeId = CurrentWorldMapNodeId,
				SelectedNodeId = SelectedWorldMapNodeId,
				Inventory = PlayerInventory.ExportSlots(),
				Equipment = PlayerEquipment.ExportSlots(),
				DeathBag = DeathBag,
				StorageContainers = GetStorageStates(),
				Buildings = Buildings.GetAll(),
				EidraRoster = Eidra.GetAll(),
				ActiveEidraInstanceIds = Eidra.GetActiveInstanceIds(),
				ZoneStates = ZoneStates.Export(),
				WeaponUpgrades = WeaponProgression.Export(),
				VisitedNodeIds = GetVisitedWorldMapNodeIds(),
				EventNodeIds = GetEventWorldMapNodeIds(),
				ProgressFlags = GetProgressFlags(),
				CompletedNodeIds = GetCompletedWorldMapNodeIds(),
				EidraForge = EidraForge.Capture()
			};
		}

		public bool TryRestoreRuntimeState(GameSessionRuntimeState state, out string error)
		{
			Initialize();
			if (state == null)
			{
				error = "Runtime save state is missing.";
				return false;
			}
			if (!PlayerInventory.TryImportSlots(state.Inventory, out error))
			{
				return false;
			}
			if (!PlayerEquipment.TryImportSlots(state.Equipment, out error))
			{
				return false;
			}
			DeathBag = state.DeathBag;
			SessionId = Guid.NewGuid().ToString("N");
			Phase = GameSessionPhase.ActiveGame;
			LifeState = PlayerLifeState.Alive;
			LastSafeNodeId = "home_base";
			LastSafeSceneKey = "HomeBase";
			ActiveWeaponId = state.ActiveWeaponId ?? string.Empty;
			ActiveEidraId = state.ActiveEidraId ?? string.Empty;
			SelectedWorldMapNodeId = "home_base";
			CurrentWorldMapNodeId = "home_base";
			RestoreSet(_visitedWorldMapNodes, state.VisitedNodeIds);
			_visitedWorldMapNodes.Add("home_base");
			RestoreSet(_eventWorldMapNodes, state.EventNodeIds);
			RestoreSet(_progressFlags, state.ProgressFlags);
			RestoreSet(_completedWorldMapNodes, state.CompletedNodeIds);
			_storageStates.Clear();
			if (state.StorageContainers != null)
			{
				StorageContainerState[] storageContainers = state.StorageContainers;
				foreach (StorageContainerState storageContainerState in storageContainers)
				{
					if (storageContainerState != null && !string.IsNullOrWhiteSpace(storageContainerState.ContainerId))
					{
						_storageStates[storageContainerState.ContainerId] = storageContainerState.Copy();
					}
				}
			}
			WeaponProgression.Restore(state.WeaponUpgrades);
			Buildings.Restore(state.Buildings);
			Eidra.Restore(state.EidraRoster, state.ActiveEidraInstanceIds);
			ZoneStates.Restore(state.ZoneStates);
			EidraForge.Restore(state.EidraForge);
			ResetAreaTransition();
			CancelWorldTravel();
			error = string.Empty;
			return true;
		}

		public void RequestSave(SaveRequestReason reason)
		{
			if (Phase == GameSessionPhase.ActiveGame)
			{
				this.SaveRequested?.Invoke(reason);
			}
		}

		public void SetWorldMapNodeEventActive(string nodeId, bool active)
		{
			if (!string.IsNullOrWhiteSpace(nodeId))
			{
				if (active)
				{
					_eventWorldMapNodes.Add(nodeId);
				}
				else
				{
					_eventWorldMapNodes.Remove(nodeId);
				}
			}
		}

		public bool IsWorldMapNodeEventActive(string nodeId)
		{
			return !string.IsNullOrWhiteSpace(nodeId) && _eventWorldMapNodes.Contains(nodeId);
		}

		public bool TrySetProgressFlag(string flagId)
		{
			Initialize();
			return !string.IsNullOrWhiteSpace(flagId) && _progressFlags.Add(flagId);
		}

		public bool HasProgressFlag(string flagId)
		{
			Initialize();
			return !string.IsNullOrWhiteSpace(flagId) && _progressFlags.Contains(flagId);
		}

		public bool MarkWorldMapNodeCompleted(string nodeId)
		{
			Initialize();
			return !string.IsNullOrWhiteSpace(nodeId) && _completedWorldMapNodes.Add(nodeId);
		}

		public bool IsWorldMapNodeCompleted(string nodeId)
		{
			Initialize();
			return !string.IsNullOrWhiteSpace(nodeId) && _completedWorldMapNodes.Contains(nodeId);
		}

		public bool TryGetStorageState(string containerId, out StorageContainerState state)
		{
			Initialize();
			if (!string.IsNullOrWhiteSpace(containerId) && _storageStates.TryGetValue(containerId, out var value))
			{
				state = value.Copy();
				return true;
			}
			state = null;
			return false;
		}

		public bool TryDropDeathBag(string sceneKey, Vector3 position)
		{
			Initialize();
			if (string.IsNullOrWhiteSpace(sceneKey) || LifeState != PlayerLifeState.Dead)
			{
				return false;
			}
			ItemStack[] array = PlayerInventory.ExportSlots();
			ItemStack[] array2 = new ItemStack[24];
			for (int i = 0; i < array.Length && i < array2.Length; i++)
			{
				array2[i] = array[i];
			}
			SetStorageState(new StorageContainerState("death.bag", array2));
			DeathBag = new DeathBagLocation(sceneKey, position);
			PlayerInventory.Clear();
			return true;
		}

		public bool HasDeathBagContents()
		{
			Initialize();
			if (!DeathBag.Exists || !TryGetStorageState("death.bag", out var state))
			{
				return false;
			}
			ItemStack[] slots = state.Slots;
			foreach (ItemStack itemStack in slots)
			{
				if (!itemStack.IsEmpty)
				{
					return true;
				}
			}
			return false;
		}

		public void SetStorageState(StorageContainerState state)
		{
			Initialize();
			if (state == null || string.IsNullOrWhiteSpace(state.ContainerId))
			{
				throw new ArgumentException("Storage state requires a stable container ID.", "state");
			}
			_storageStates[state.ContainerId] = state.Copy();
		}

		public bool IsStorageEmpty(string containerId)
		{
			if (!TryGetStorageState(containerId, out var state))
			{
				return true;
			}
			ItemStack[] slots = state.Slots;
			foreach (ItemStack itemStack in slots)
			{
				if (!itemStack.IsEmpty)
				{
					return false;
				}
			}
			return true;
		}

		public void RemoveStorageState(string containerId)
		{
			if (!string.IsNullOrWhiteSpace(containerId))
			{
				_storageStates.Remove(containerId);
			}
		}

		private void ResetAreaTransition()
		{
			PreviousZoneId = string.Empty;
			LastExitDirection = MapExitDirection.None;
			RequestedTargetScene = string.Empty;
			LastValidAreaStatus = AreaStatus.Unknown;
		}

		private void ResetWorldMapState()
		{
			SelectedWorldMapNodeId = "home_base";
			CurrentWorldMapNodeId = "home_base";
			_visitedWorldMapNodes.Clear();
			_visitedWorldMapNodes.Add("home_base");
			_eventWorldMapNodes.Clear();
			_progressFlags.Clear();
			_completedWorldMapNodes.Clear();
			ClearPendingWorldTravel();
		}

		private void ClearPendingWorldTravel()
		{
			PendingWorldMapNodeId = string.Empty;
			PendingWorldMapSceneKey = string.Empty;
		}

		private static string[] CopySet(HashSet<string> source)
		{
			string[] array = new string[source.Count];
			source.CopyTo(array);
			Array.Sort(array, StringComparer.Ordinal);
			return array;
		}

		private static void RestoreSet(HashSet<string> target, IEnumerable<string> values)
		{
			target.Clear();
			if (values == null)
			{
				return;
			}
			foreach (string value in values)
			{
				if (!string.IsNullOrWhiteSpace(value))
				{
					target.Add(value);
				}
			}
		}

		public bool TryResetEidraForge(DateTime utcNow, int nextSeed, bool playerInside, out InventoryTransactionFailure failure)
		{
			Initialize();
			ItemStack[] recoveryContents = Array.Empty<ItemStack>();
			bool flag = DeathBag.Exists && string.Equals(DeathBag.SceneKey, "EidraForge", StringComparison.Ordinal);
			if (flag && TryGetStorageState("death.bag", out var state))
			{
				recoveryContents = state.Slots;
			}
			if (!EidraForge.TryPaidReset(PlayerInventory, utcNow, nextSeed, playerInside, recoveryContents, out failure))
			{
				return false;
			}
			if (flag)
			{
				_storageStates.Remove("death.bag");
				DeathBag = DeathBagLocation.None;
			}
			RequestSave(SaveRequestReason.BossDefeated);
			return true;
		}
	}
}
