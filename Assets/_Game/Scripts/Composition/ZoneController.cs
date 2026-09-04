using Eidren.AI;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.Presentation;
using System.Collections.Generic;
using System;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class ZoneController : MonoBehaviour, ICameraBoundsProvider
	{
		[SerializeField]
		private ZoneDefinition definition;

		[SerializeField]
		private Transform systemsRoot;

		[SerializeField]
		private Transform playerSpawnPointsRoot;

		[SerializeField]
		private Transform spawnDefault;

		[SerializeField]
		private Transform spawnFromNorth;

		[SerializeField]
		private Transform spawnFromEast;

		[SerializeField]
		private Transform spawnFromSouth;

		[SerializeField]
		private Transform spawnFromWest;

		[SerializeField]
		private Transform environmentRoot;

		[SerializeField]
		private Transform gameplayRoot;

		[SerializeField]
		private Transform populationRoot;

		[SerializeField]
		private Transform navigationRoot;

		[SerializeField]
		private BoxCollider cameraBounds;

		[SerializeField]
		private Transform exitVolumesRoot;

		[SerializeField]
		private Transform bossArea;

		[SerializeField]
		private Collider walkableGround;

		[SerializeField]
		private ZoneBoundarySettings boundarySettings;

		[SerializeField]
		private ZonePlayerSpawner playerSpawner;

		[SerializeField]
		private ZoneLootController lootController;

		[SerializeField]
		private MapExitCoordinator exitCoordinator;

		[SerializeField]
		private NavMeshSurface navigationSurface;

		[SerializeField]
		private MapExitVolume[] exitVolumes = Array.Empty<MapExitVolume>();

		private bool _initialized;

		private ZoneResourcePopulator _resources;

		private ZoneEidraPopulator _eidra;

		private ZoneEnemyPopulator _enemyPopulator;

		private ZoneWorldChestPopulator _worldChests;

		public ZoneDefinition Definition => definition;

		public ZoneLayout ResourceLayout { get; private set; }

		public ZoneEnemyPopulator EnemyPopulator => _enemyPopulator;

		public IReadOnlyList<WorldChestContainer> WorldChests => _worldChests?.Spawned ?? Array.Empty<WorldChestContainer>();

		public string ZoneId => (definition != null) ? definition.Id : string.Empty;

		public MapExitDirection EntryDirection { get; private set; }

		public Transform ActiveSpawn { get; private set; }

		public Transform SystemsRoot => systemsRoot;

		public Transform EnvironmentRoot => environmentRoot;

		public Transform GameplayRoot => gameplayRoot;

		public Transform PopulationRoot => populationRoot;

		public Transform NavigationRoot => navigationRoot;

		public Transform BossArea => bossArea;

		public BossAreaController BossAreaController => (bossArea != null) ? bossArea.GetComponent<BossAreaController>() : null;

		public Collider WalkableGround => walkableGround;

		public NavMeshSurface NavigationSurface => navigationSurface;

		public IReadOnlyList<MapExitVolume> ExitVolumes => exitVolumes;

		public Bounds CameraBounds => (cameraBounds != null) ? cameraBounds.bounds : new Bounds(base.transform.position, Vector3.zero);

		public event Action<ZoneController> ZoneEntered;

		public event Action<MapExitDirection> ZoneExitStarted;

		public void Configure(ZoneDefinition configuredDefinition, Transform configuredSystemsRoot, Transform configuredSpawnPointsRoot, Transform configuredDefaultSpawn, Transform configuredFromNorth, Transform configuredFromEast, Transform configuredFromSouth, Transform configuredFromWest, Transform configuredEnvironmentRoot, Transform configuredGameplayRoot, Transform configuredPopulationRoot, Transform configuredNavigationRoot, BoxCollider configuredCameraBounds, Transform configuredExitVolumesRoot, Transform configuredBossArea, Collider configuredWalkableGround, ZoneBoundarySettings configuredBoundarySettings, ZonePlayerSpawner configuredPlayerSpawner, ZoneLootController configuredLootController, MapExitCoordinator configuredExitCoordinator, NavMeshSurface configuredNavigationSurface, MapExitVolume[] configuredExitVolumes)
		{
			definition = configuredDefinition;
			systemsRoot = configuredSystemsRoot;
			playerSpawnPointsRoot = configuredSpawnPointsRoot;
			spawnDefault = configuredDefaultSpawn;
			spawnFromNorth = configuredFromNorth;
			spawnFromEast = configuredFromEast;
			spawnFromSouth = configuredFromSouth;
			spawnFromWest = configuredFromWest;
			environmentRoot = configuredEnvironmentRoot;
			gameplayRoot = configuredGameplayRoot;
			populationRoot = configuredPopulationRoot;
			navigationRoot = configuredNavigationRoot;
			cameraBounds = configuredCameraBounds;
			exitVolumesRoot = configuredExitVolumesRoot;
			bossArea = configuredBossArea;
			walkableGround = configuredWalkableGround;
			boundarySettings = configuredBoundarySettings;
			playerSpawner = configuredPlayerSpawner;
			lootController = configuredLootController;
			exitCoordinator = configuredExitCoordinator;
			navigationSurface = configuredNavigationSurface;
			exitVolumes = configuredExitVolumes ?? Array.Empty<MapExitVolume>();
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			Require(definition, "definition", list);
			Require(systemsRoot, "systemsRoot", list);
			Require(playerSpawnPointsRoot, "playerSpawnPointsRoot", list);
			Require(spawnDefault, "Spawn_Default", list);
			Require(spawnFromNorth, "Spawn_FromNorth", list);
			Require(spawnFromEast, "Spawn_FromEast", list);
			Require(spawnFromSouth, "Spawn_FromSouth", list);
			Require(spawnFromWest, "Spawn_FromWest", list);
			Require(environmentRoot, "environmentRoot", list);
			Require(gameplayRoot, "gameplayRoot", list);
			Require(populationRoot, "populationRoot", list);
			Require(navigationRoot, "navigationRoot", list);
			Require(cameraBounds, "cameraBounds", list);
			Require(exitVolumesRoot, "exitVolumesRoot", list);
			Require(walkableGround, "walkableGround", list);
			Require(boundarySettings, "boundarySettings", list);
			Require(playerSpawner, "playerSpawner", list);
			Require(exitCoordinator, "exitCoordinator", list);
			if (definition != null && definition.EnemiesAllowed && navigationSurface == null)
			{
				list.Add("Zone '" + definition.Id + "' allows enemies but has no NavMeshSurface.");
			}
			if (definition != null && definition.EnemiesAllowed && lootController == null)
			{
				list.Add("Zone '" + definition.Id + "' allows enemies but has no ZoneLootController.");
			}
			if (definition != null && definition.IsBossZone && BossAreaController == null)
			{
				list.Add("Boss zone '" + definition.Id + "' has no configured BossAreaController.");
			}
			if (exitVolumes == null || exitVolumes.Length != 4)
			{
				list.Add("Exactly four MapExitVolumes are required.");
			}
			else
			{
				ValidateExitDirections(list);
			}
			ValidateSpawnOutsideExits(spawnDefault, "Spawn_Default", list);
			ValidateSpawnOutsideExits(spawnFromNorth, "Spawn_FromNorth", list);
			ValidateSpawnOutsideExits(spawnFromEast, "Spawn_FromEast", list);
			ValidateSpawnOutsideExits(spawnFromSouth, "Spawn_FromSouth", list);
			ValidateSpawnOutsideExits(spawnFromWest, "Spawn_FromWest", list);
			return list.ToArray();
		}

		public Transform GetSpawnForEntry(MapExitDirection entryDirection)
		{
			if (1 == 0)
			{
			}
			Transform result = entryDirection switch
			{
				MapExitDirection.North => spawnFromNorth, 
				MapExitDirection.East => spawnFromEast, 
				MapExitDirection.South => spawnFromSouth, 
				MapExitDirection.West => spawnFromWest, 
				_ => spawnDefault, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		public bool IsSpawnValid(Transform spawn, float radius = 0.48f, float height = 2f, float enemyClearance = 3f)
		{
			if (spawn == null || walkableGround == null)
			{
				return false;
			}
			Vector3 position = spawn.position;
			Bounds bounds = walkableGround.bounds;
			if (position.x < bounds.min.x || position.x > bounds.max.x || position.z < bounds.min.z || position.z > bounds.max.z)
			{
				return false;
			}
			Ray ray = new Ray(position + Vector3.up * 2f, Vector3.down);
			if (!walkableGround.Raycast(ray, out var _, 5f))
			{
				return false;
			}
			MapExitVolume[] array = exitVolumes;
			foreach (MapExitVolume mapExitVolume in array)
			{
				if (mapExitVolume != null && mapExitVolume.TriggerBounds.Contains(position))
				{
					return false;
				}
			}
			Collider[] array2 = Physics.OverlapCapsule(position + Vector3.up * radius, position + Vector3.up * Mathf.Max(radius, height - radius), Mathf.Max(0.05f, radius - 0.08f), -5, QueryTriggerInteraction.Ignore);
			Collider[] array3 = array2;
			foreach (Collider collider in array3)
			{
				if (collider != null && collider != walkableGround && !collider.transform.IsChildOf(playerSpawnPointsRoot))
				{
					return false;
				}
			}
			Collider[] array4 = Physics.OverlapSphere(position, Mathf.Max(0f, enemyClearance), -5, QueryTriggerInteraction.Ignore);
			Collider[] array5 = array4;
			foreach (Collider collider2 in array5)
			{
				if (collider2 != null && collider2.GetComponentInParent<EnemyControllerBase>() != null)
				{
					return false;
				}
			}
			if (navigationSurface != null && navigationSurface.navMeshData != null && !NavMesh.SamplePosition(position, out var _, 1.5f, -1))
			{
				return false;
			}
			return true;
		}

		public void InitializeZone()
		{
			if (_initialized)
			{
				return;
			}
			string[] validationErrors = GetValidationErrors();
			if (validationErrors.Length != 0)
			{
				Debug.LogError("ZoneController in '" + base.gameObject.scene.name + "' is invalid:\n- " + string.Join("\n- ", validationErrors), this);
				base.enabled = false;
				return;
			}
			EidrenServiceRoot eidrenServiceRoot = EidrenServiceRoot.FindOrCreate();
			GameSession gameSession = eidrenServiceRoot.GameSession;
			if (gameSession.Phase == GameSessionPhase.Shell)
			{
				gameSession.StartNewGame();
			}
			bool flag = ZoneEntryRules.ShouldUseDirectionalSpawn(gameSession, definition.SceneKey);
			EntryDirection = (flag ? ZoneEntryRules.Opposite(gameSession.LastExitDirection) : MapExitDirection.None);
			ActiveSpawn = GetSpawnForEntry(EntryDirection);
			BuildZoneResources(eidrenServiceRoot);
			lootController?.Initialize(eidrenServiceRoot.ContentDatabase);
			if (!IsSpawnValid(ActiveSpawn))
			{
				Debug.LogError("Zone '" + definition.Id + "' spawn '" + ActiveSpawn.name + "' is not safe.", ActiveSpawn);
				base.enabled = false;
			}
			else
			{
				exitCoordinator.ExitConfirmed += HandleExitConfirmed;
				playerSpawner.SpawnAt(ActiveSpawn, this, eidrenServiceRoot);
				_initialized = true;
				this.ZoneEntered?.Invoke(this);
			}
		}

		public void InitializeEnemies(PlayerPrefabBindings player)
		{
			if (!(populationRoot == null) && !(player == null) && definition.EnemiesAllowed)
			{
				WildlingController[] componentsInChildren = populationRoot.GetComponentsInChildren<WildlingController>(includeInactive: true);
				WildlingController[] array = componentsInChildren;
				foreach (WildlingController wildlingController in array)
				{
					wildlingController.Initialize(player.transform, player.Damageable);
				}
				_eidra?.InitializeWithPlayer(player);
			}
		}

		public void InitializeBoss(PlayerPrefabBindings player, EidrenServiceRoot services)
		{
			if (!(definition == null) && definition.IsBossZone)
			{
				BossAreaController?.Initialize(player, services, lootController);
			}
		}

		private void BuildZoneResources(EidrenServiceRoot services)
		{
			_worldChests = new ZoneWorldChestPopulator(this, services.GameSession, services.ContentDatabase);
			WorldChestPlacement[] chestPlacements = _worldChests.Populate();
			_resources = new ZoneResourcePopulator(this, services.GameSession, services.ContentDatabase, services.PlayerProgression);
			ResourceLayout = _resources.Populate();
			_eidra = new ZoneEidraPopulator(this, services.GameSession, services.CaptureEquipment, services.EidraRoster, services.PlayerProgression);
			_eidra.Populate(ResourceLayout);
			// F31-009: Reguläre Gegner zuletzt — so weichen sie den bereits
			// gewürfelten Kisten, Ressourcen und Eidra physisch aus, und die
			// Kistenwachen kennen die tatsächlich besetzten Kistenplätze.
			_enemyPopulator = new ZoneEnemyPopulator(this, services.GameSession);
			_enemyPopulator.Populate(chestPlacements);
		}

		private void Awake()
		{
			InitializeZone();
		}

		private void HandleExitConfirmed(MapExitDirection direction)
		{
			this.ZoneExitStarted?.Invoke(direction);
		}

		private void ValidateExitDirections(List<string> errors)
		{
			HashSet<MapExitDirection> hashSet = new HashSet<MapExitDirection>();
			MapExitVolume[] array = exitVolumes;
			foreach (MapExitVolume mapExitVolume in array)
			{
				if (mapExitVolume == null)
				{
					errors.Add("A MapExitVolume reference is missing.");
				}
				else
				{
					hashSet.Add(mapExitVolume.Direction);
				}
			}
			MapExitDirection[] array2 = new MapExitDirection[4]
			{
				MapExitDirection.North,
				MapExitDirection.East,
				MapExitDirection.South,
				MapExitDirection.West
			};
			foreach (MapExitDirection mapExitDirection in array2)
			{
				if (!hashSet.Contains(mapExitDirection))
				{
					errors.Add($"Exit direction {mapExitDirection} is missing.");
				}
			}
		}

		private void ValidateSpawnOutsideExits(Transform spawn, string label, List<string> errors)
		{
			if (spawn == null || exitVolumes == null)
			{
				return;
			}
			MapExitVolume[] array = exitVolumes;
			foreach (MapExitVolume mapExitVolume in array)
			{
				if (mapExitVolume != null && mapExitVolume.TriggerBounds.Contains(spawn.position))
				{
					errors.Add(label + " overlaps exit '" + mapExitVolume.StableExitId + "'.");
				}
			}
		}

		private static void Require(UnityEngine.Object value, string label, List<string> errors)
		{
			if (value == null)
			{
				errors.Add("Missing required reference: " + label + ".");
			}
		}

		private void OnDestroy()
		{
			if (exitCoordinator != null)
			{
				exitCoordinator.ExitConfirmed -= HandleExitConfirmed;
			}
		}
	}
}
