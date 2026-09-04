using Eidren.AI;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Interaction;
using Eidren.Presentation;
using Eidren.UI;
using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class ZonePlayerSpawner : MonoBehaviour
	{
		[SerializeField]
		private PlayerPrefabBindings playerPrefab;

		[SerializeField]
		private Camera gameplayCamera;

		[SerializeField]
		private MapExitCoordinator exitCoordinator;

		[SerializeField]
		private WeaponData hammer;

		[SerializeField]
		private WeaponData daggers;

		[SerializeField]
		private EidraData terrock;

		[SerializeField]
		private EidraData noctarion;

		private PlayerInputReader _input;

		private Action<EnemyLootContainer> _openEnemyLoot;

		private SceneFlowService _sceneFlow;

		private GameSession _gameSession;

		private EidraTeamRosterBinding _eidraTeamBinding;

		private bool _spawnCompleted;

		public PlayerPrefabBindings SpawnedPlayer { get; private set; }

		public PlayerInputReader Input => _input;

		private void BindEnemyLoot(ZoneLootController[] lootControllers, StorageWindow window)
		{
			foreach (ZoneLootController zoneLootController in lootControllers)
			{
				if (zoneLootController == null)
				{
					continue;
				}
				if (_openEnemyLoot != null)
				{
					zoneLootController.LootContainerCreated -= _openEnemyLoot;
				}
				zoneLootController.LootContainerCreated += Open;
				foreach (EnemyLootContainer lootContainer in zoneLootController.LootContainers)
				{
					Open(lootContainer);
				}
			}
			_openEnemyLoot = Open;
			void Open(EnemyLootContainer container)
			{
				container.Activated -= window.Open;
				container.Activated += window.Open;
			}
		}

		private void BindCraftingStations(CraftingWindow window)
		{
			GameObject[] rootGameObjects = base.gameObject.scene.GetRootGameObjects();
			foreach (GameObject gameObject in rootGameObjects)
			{
				WorkbenchController[] componentsInChildren = gameObject.GetComponentsInChildren<WorkbenchController>(includeInactive: true);
				WorkbenchController[] array = componentsInChildren;
				foreach (WorkbenchController workbenchController in array)
				{
					workbenchController.Activated -= window.Open;
					workbenchController.Activated += window.Open;
				}
			}
		}

		public void Configure(PlayerPrefabBindings prefab, Camera camera, MapExitCoordinator configuredExitCoordinator, WeaponData configuredHammer = null, WeaponData configuredDaggers = null, EidraData configuredTerrock = null, EidraData configuredNoctarion = null)
		{
			playerPrefab = prefab;
			gameplayCamera = camera;
			exitCoordinator = configuredExitCoordinator;
			hammer = configuredHammer;
			daggers = configuredDaggers;
			terrock = configuredTerrock;
			noctarion = configuredNoctarion;
		}

		public PlayerPrefabBindings SpawnAt(Transform spawn, ZoneController zone, EidrenServiceRoot services)
		{
			if (_spawnCompleted && SpawnedPlayer != null)
			{
				return SpawnedPlayer;
			}
			if (playerPrefab == null || spawn == null || gameplayCamera == null || zone == null || services == null)
			{
				Debug.LogError("ZonePlayerSpawner in '" + base.gameObject.scene.name + "' is missing its prefab, spawn, zone, services, or camera reference.", this);
				base.enabled = false;
				return null;
			}
			_sceneFlow = services.SceneFlowService;
			_gameSession = services.GameSession;
			if (services.GameSession.Phase == GameSessionPhase.Shell)
			{
				services.GameSession.StartNewGame();
			}
			if (_input == null)
			{
				_input = GetComponentInChildren<PlayerInputReader>();
				if (_input == null)
				{
					GameObject gameObject = new GameObject("PlayerInputReader");
					gameObject.transform.SetParent(base.transform, worldPositionStays: false);
					_input = gameObject.AddComponent<PlayerInputReader>();
				}
			}
			if (SpawnedPlayer == null)
			{
				SpawnedPlayer = FindExistingScenePlayer();
			}
			if (SpawnedPlayer == null)
			{
				SpawnedPlayer = UnityEngine.Object.Instantiate(playerPrefab, spawn.position, spawn.rotation);
				SpawnedPlayer.name = "Player";
			}
			else
			{
				CharacterController characterController = SpawnedPlayer.CharacterController;
				if (characterController != null)
				{
					characterController.enabled = false;
				}
				SpawnedPlayer.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
				if (characterController != null)
				{
					characterController.enabled = true;
				}
			}
			SpawnedPlayer.PrepareForRuntime();
			DeathBagSpawner.TrySpawn(services.GameSession, base.gameObject.scene.name);
			StorageContainer[] array = FindSceneComponents<StorageContainer>();
			StorageContainer[] array2 = array;
			foreach (StorageContainer storageContainer in array2)
			{
				storageContainer.Initialize(services.ContentDatabase, services.GameSession);
			}
			WorldChestContainer[] array3 = FindSceneComponents<WorldChestContainer>();
			EidraForgeChestContainer[] array4 = FindSceneComponents<EidraForgeChestContainer>();
			SpawnedPlayer.BindExplorationDependencies(_input, gameplayCamera, zone.GetComponent<ZoneBoundarySettings>().CreatePlayerBoundary());
			SpawnedPlayer.BindInteractionDependencies(_input, _sceneFlow, services.GameSession.PlayerInventory);
			EidraTeamComposition.Resolve(services, out var first, out var second, terrock, noctarion);
			SpawnedPlayer.BindExplorationCombat(_input, hammer ?? services.ContentDatabase.GetWeapon("hammer"), daggers ?? services.ContentDatabase.GetWeapon("daggers"), services.GameSession.WeaponProgression, services.GameSession, first, second, services.ContentDatabase);
			_eidraTeamBinding?.Dispose();
			_eidraTeamBinding = new EidraTeamRosterBinding(services, SpawnedPlayer.EidraTeam, terrock, noctarion);
			SpawnedPlayer.BindExplorationConsumables(_input, services.GameSession.PlayerInventory, services.ContentDatabase.GetItem("healing_potion"), services.ContentDatabase.GetItem("buff_food"));
			SpawnedPlayer.BindInventoryWindow(services.GameSession.PlayerInventory, services.GameSession.PlayerEquipment, services.ContentDatabase, _input, _sceneFlow);
			bool flag = string.Equals(base.gameObject.scene.name, "HomeBase", StringComparison.Ordinal);
			SpawnedPlayer.BindTechnologyTreeWindow(services.TechnologyTree, services.TechnologyUnlocks, services.PlayerProgression, _input, _sceneFlow);
			// F32-006: Ohne dieses Fenster bleibt jedes dritte gefangene Eidra
			// dauerhaft auf der Bank — es gab keinen Weg, das Gespann zu ändern.
			SpawnedPlayer.BindEidraTeamWindow(services.EidraRoster, services.ContentDatabase, _input, _sceneFlow, services.GameSession);
			IMaterialStock materialStock;
			if (!flag)
			{
				IMaterialStock playerInventory = services.GameSession.PlayerInventory;
				materialStock = playerInventory;
			}
			else
			{
				IMaterialStock playerInventory = new HomeBaseMaterialStock(services.ContentDatabase, services.GameSession);
				materialStock = playerInventory;
			}
			IMaterialStock materials = materialStock;
			CraftingWindow craftingWindow = SpawnedPlayer.BindCraftingWindow(services.GameSession.PlayerInventory, services.GameSession.PlayerEquipment, services.GameSession.WeaponProgression, services.PlayerProgression, services.TechnologyUnlocks, services.ContentDatabase, _input, _sceneFlow, services.SaveGameService, materials);
			BindCraftingStations(craftingWindow);
			StorageWindow storageWindow = null;
			ZoneLootController[] array5 = FindSceneComponents<ZoneLootController>();
			if (flag || array.Length != 0 || array3.Length != 0 || array4.Length != 0 || array5.Length != 0)
			{
				storageWindow = SpawnedPlayer.BindStorageWindow(services.GameSession.PlayerInventory, services.ContentDatabase, _input, _sceneFlow, services.SaveGameService);
				StorageContainer[] array6 = array;
				foreach (StorageContainer storageContainer2 in array6)
				{
					storageContainer2.Activated -= storageWindow.Open;
					storageContainer2.Activated += storageWindow.Open;
				}
				WorldChestContainer[] array7 = array3;
				foreach (WorldChestContainer worldChestContainer in array7)
				{
					worldChestContainer.Activated -= storageWindow.Open;
					worldChestContainer.Activated += storageWindow.Open;
				}
				EidraForgeChestContainer[] array8 = array4;
				foreach (EidraForgeChestContainer eidraForgeChestContainer in array8)
				{
					eidraForgeChestContainer.Activated -= storageWindow.Open;
					eidraForgeChestContainer.Activated += storageWindow.Open;
				}
				BindEnemyLoot(array5, storageWindow);
			}
			if (flag)
			{
				SpawnedPlayer.BindBuildingSystem(services.Buildings, services.TechnologyUnlocks, services.ContentDatabase, services.GameSession, _input, _sceneFlow, zone, craftingWindow, storageWindow, gameplayCamera);
			}
			SpawnedPlayer.BindInventoryFeedback(services.GameSession.PlayerInventory, services.ContentDatabase);
			zone.InitializeEnemies(SpawnedPlayer);
			zone.InitializeBoss(SpawnedPlayer, services);
			BossController boss = ((zone.BossAreaController != null) ? zone.BossAreaController.ActiveBoss : null);
			SpawnedPlayer.EidraTeam.AttachBoss(boss);
			SpawnedPlayer.BindCombatHud(_input, services.GameSession.PlayerInventory, services.PlayerProgression, boss);
			// N04-001: Das Ende der Karte kommt aus der Zonengeometrie — die
			// Bewegungsgrenze ist in den Zonen Unbounded und taugt nicht dafuer.
			if (SpawnedPlayer.CombatHud != null && SpawnedPlayer.CombatHud.Minimap != null)
			{
				SpawnedPlayer.CombatHud.Minimap.SetMapEdge(zone.GetComponent<ZoneBoundarySettings>().CreateMapEdgeBoundary());
			}
			// F31-006: Das Aufgaben-Label bekommt den Questdienst von hier —
			// die UI-Schicht kennt den ServiceRoot nicht.
			if (SpawnedPlayer.CombatHud != null)
			{
				SpawnedPlayer.CombatHud.GetComponentInChildren<Eidren.UI.QuestHudPresenter>(includeInactive: true)?.Configure(services.QuestProgress);
			}
			IsometricCamera isometricCamera = gameplayCamera.GetComponent<IsometricCamera>();
			if (isometricCamera == null)
			{
				isometricCamera = gameplayCamera.gameObject.AddComponent<IsometricCamera>();
			}
			ZoneCameraSettings camera = zone.Definition.Camera;
			isometricCamera.ConfigureZoneView(camera.Rotation, camera.BaseOrthographicSize, camera.MinimumOrthographicSize, camera.BoundsInset);
			isometricCamera.InitializeWithinBounds(SpawnedPlayer.transform, null, camera.Offset, zone);
			exitCoordinator?.BindPlayer(SpawnedPlayer, _input, _sceneFlow, services.GameSession);
			SpawnedPlayer.BindDeathFlow(_input, services.GameSession, _sceneFlow, zone.Definition.DisplayName);
			services.PauseMenuController?.BindGameplay(SpawnedPlayer, _input, exitCoordinator);
			services.GameSession.CompleteRespawn(zone.Definition.SceneKey);
			_sceneFlow.InputBlockedChanged += HandleInputBlocked;
			HandleInputBlocked(_sceneFlow.IsInputBlocked);
			_spawnCompleted = true;
			return SpawnedPlayer;
		}

		private T[] FindSceneComponents<T>() where T : Component
		{
			List<T> list = new List<T>();
			GameObject[] rootGameObjects = base.gameObject.scene.GetRootGameObjects();
			foreach (GameObject gameObject in rootGameObjects)
			{
				list.AddRange(gameObject.GetComponentsInChildren<T>(includeInactive: true));
			}
			return list.ToArray();
		}

		private PlayerPrefabBindings FindExistingScenePlayer()
		{
			GameObject[] rootGameObjects = base.gameObject.scene.GetRootGameObjects();
			foreach (GameObject gameObject in rootGameObjects)
			{
				PlayerPrefabBindings[] componentsInChildren = gameObject.GetComponentsInChildren<PlayerPrefabBindings>(includeInactive: true);
				if (componentsInChildren.Length != 0)
				{
					return componentsInChildren[0];
				}
			}
			return null;
		}

		private void HandleInputBlocked(bool blocked)
		{
			bool flag = !blocked && SpawnedPlayer != null && SpawnedPlayer.Damageable.IsAlive && (_gameSession == null || _gameSession.LifeState == PlayerLifeState.Alive);
			_input?.SetGameplayEnabled(flag);
			if (SpawnedPlayer != null)
			{
				SpawnedPlayer.Motor.SetMovementEnabled(flag);
			}
		}

		private void OnDestroy()
		{
			_eidraTeamBinding?.Dispose();
			_eidraTeamBinding = null;
			if (_sceneFlow != null)
			{
				_sceneFlow.InputBlockedChanged -= HandleInputBlocked;
			}
		}
	}
}
