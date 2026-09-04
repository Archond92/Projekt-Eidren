using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class ZoneStructurePlayModeTests
{
	private static readonly string[] OutdoorScenes = new string[4] { "Zone_Greenwood", "Zone_Quarry", "Zone_Marsh", "Zone_EmberRuins" };

	[UnityTest]
	public IEnumerator Greenwood_SpawnsOnePlayerAndUsesCameraBounds()
	{
		yield return LoadGreenwood();
		ZoneController zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
		ZonePlayerSpawner zonePlayerSpawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		Assert.That<ZoneController>(zone, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<ZonePlayerSpawner>(zonePlayerSpawner, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerPrefabBindings>(zonePlayerSpawner.SpawnedPlayer, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerPrefabBindings[]>(UnityEngine.Object.FindObjectsByType<PlayerPrefabBindings>(FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		zonePlayerSpawner.SpawnAt(zone.ActiveSpawn, zone, EidrenServiceRoot.FindOrCreate());
		Assert.That<PlayerPrefabBindings[]>(UnityEngine.Object.FindObjectsByType<PlayerPrefabBindings>(FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		IsometricCamera isometricCamera = UnityEngine.Object.FindFirstObjectByType<IsometricCamera>();
		Assert.That<IsometricCamera>(isometricCamera, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(isometricCamera.UsesBoundsProvider, (IResolveConstraint)(object)Is.True);
		Assert.That<Vector3>(isometricCamera.ActiveBounds.center, (IResolveConstraint)(object)Is.EqualTo((object)zone.CameraBounds.center));
	}

	[UnityTest]
	public IEnumerator Greenwood_AllDirectionalSpawnsAreUsable()
	{
		yield return LoadGreenwood();
		ZoneController zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
		Assert.That<ZoneController>(zone, (IResolveConstraint)(object)Is.Not.Null);
		MapExitDirection[] array = new MapExitDirection[4]
		{
			MapExitDirection.North,
			MapExitDirection.East,
			MapExitDirection.South,
			MapExitDirection.West
		};
		for (int i = 0; i < array.Length; i++)
		{
			MapExitDirection direction = array[i];
			Transform spawn = zone.GetSpawnForEntry(direction);
			Assert.That<Transform>(spawn, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<bool>(zone.IsSpawnValid(spawn), (IResolveConstraint)(object)Is.True, direction.ToString(), Array.Empty<object>());
		}
	}

	[UnityTest]
	public IEnumerator CurrentWallPrefab_BlocksPlayerMovement()
	{
		yield return LoadGreenwood();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		Assert.That<ZonePlayerSpawner>(spawner, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerPrefabBindings>(spawner.SpawnedPlayer, (IResolveConstraint)(object)Is.Not.Null);
		BuildingCatalogDefinition buildingCatalogDefinition = Resources.Load<BuildingCatalogDefinition>("Data/BuildingCatalog_V01");
		Assert.That<BuildingCatalogDefinition>(buildingCatalogDefinition, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(buildingCatalogDefinition.Buildings.First((BuildingCostDefinition item) => item.Id == "building.wall").TryGetLevel(1, out var _, out var wallPrefab), (IResolveConstraint)(object)Is.True);
		GameObject wallInstance = UnityEngine.Object.Instantiate(wallPrefab, Vector3.zero, Quaternion.identity);
		try
		{
			Assert.That<Collider[]>(wallInstance.GetComponentsInChildren<Collider>(includeInactive: true), (IResolveConstraint)(object)Is.Not.Empty, "The current wall prefab must carry collision geometry.", Array.Empty<object>());
			spawner.SpawnedPlayer.Motor.Teleport(new Vector3(0f, 0.05f, -2f));
			Physics.SyncTransforms();
			for (int step = 0; step < 40; step++)
			{
				spawner.SpawnedPlayer.Motor.Nudge(Vector3.forward, 0.1f);
				Physics.SyncTransforms();
			}
			float stopped = spawner.SpawnedPlayer.transform.position.z;
			Assert.That<float>(stopped, (IResolveConstraint)(object)Is.LessThan((object)(-0.4f)), "CharacterController movement must stop at the current " + $"wall prefab; stopped at z={stopped}.", Array.Empty<object>());
		}
		finally
		{
			UnityEngine.Object.Destroy(wallInstance);
		}
	}

	[UnityTest]
	public IEnumerator AllOutdoorZones_NavMeshConnectsAcrossMap()
	{
		string[] array = new string[4] { "Zone_Greenwood", "Zone_Quarry", "Zone_Marsh", "Zone_EmberRuins" };
		foreach (string sceneName in array)
		{
			yield return LoadZone(sceneName);
			ZoneController zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
			Assert.That<ZoneController>(zone, (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			Assert.That<NavMeshSurface>(zone.NavigationSurface, (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			Assert.That<NavMeshData>(zone.NavigationSurface.navMeshData, (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			IsometricCamera isometricCamera = UnityEngine.Object.FindFirstObjectByType<IsometricCamera>();
			Assert.That<IsometricCamera>(isometricCamera, (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			Assert.That<bool>(isometricCamera.UsesBoundsProvider, (IResolveConstraint)(object)Is.True, sceneName, Array.Empty<object>());
			Assert.That<Vector3>(isometricCamera.ActiveBounds.center, (IResolveConstraint)(object)Is.EqualTo((object)zone.CameraBounds.center), sceneName, Array.Empty<object>());
			MapExitDirection[] array2 = new MapExitDirection[4]
			{
				MapExitDirection.North,
				MapExitDirection.East,
				MapExitDirection.South,
				MapExitDirection.West
			};
			foreach (MapExitDirection direction in array2)
			{
				Assert.That<bool>(zone.IsSpawnValid(zone.GetSpawnForEntry(direction)), (IResolveConstraint)(object)Is.True, $"{sceneName} {direction}", Array.Empty<object>());
			}
			Assert.That<bool>(NavMesh.SamplePosition(new Vector3(-10f, 0f, 0f), out var start, 4f, -1), (IResolveConstraint)(object)Is.True, sceneName, Array.Empty<object>());
			Assert.That<bool>(NavMesh.SamplePosition(new Vector3(10f, 0f, 0f), out var end, 4f, -1), (IResolveConstraint)(object)Is.True, sceneName, Array.Empty<object>());
			NavMeshPath path = new NavMeshPath();
			Assert.That<bool>(NavMesh.CalculatePath(start.position, end.position, -1, path), (IResolveConstraint)(object)Is.True, sceneName, Array.Empty<object>());
			Assert.That<NavMeshPathStatus>(path.status, (IResolveConstraint)(object)Is.EqualTo((object)NavMeshPathStatus.PathComplete), sceneName, Array.Empty<object>());
			Assert.That<int>(path.corners.Length, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)2), sceneName + " must remain traversable across the map after removal of the legacy massive test obstacles.", Array.Empty<object>());
		}
	}

	[UnityTest]
	public IEnumerator EveryOutdoorZone_Generates72NodesPlusBerryBushes()
	{
		string[] outdoorScenes = OutdoorScenes;
		foreach (string sceneName in outdoorScenes)
		{
			yield return LoadZone(sceneName);
			ZoneController zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
			Assert.That<ZoneController>(zone, (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			Assert.That<ZoneLayout>(zone.ResourceLayout, (IResolveConstraint)(object)Is.Not.Null, sceneName + ": keine Laufzeitgenerierung gelaufen.", Array.Empty<object>());
			Assert.That<int>(zone.ResourceLayout.EconomyNodeCount, (IResolveConstraint)(object)Is.EqualTo((object)72), sceneName, Array.Empty<object>());
			ResourceNode[] array = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			Assert.That<ResourceNode[]>(array, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)zone.ResourceLayout.Placements.Count), sceneName + ": Jeder Platz traegt genau einen Knoten.", Array.Empty<object>());
			Assert.That<int>(array.Count((ResourceNode node) => node.Definition != null && node.Definition.Id == "resource.berry_bush"), (IResolveConstraint)(object)Is.GreaterThan((object)0), sceneName + ": Beeren stehen in allen vier Gebieten (M1.7, M8.4).", Array.Empty<object>());
			Assert.That<int>(array.Count((ResourceNode node) => node.Byproduct.Exists), (IResolveConstraint)(object)Is.EqualTo((object)zone.ResourceLayout.WheatSeedNodeCount), sceneName + ": Weizensamen sind deterministisch (M8.4).", Array.Empty<object>());
		}
	}

	[UnityTest]
	public IEnumerator ReenteringWithoutHomecoming_RestoresTheSameMap()
	{
		yield return LoadZone("Zone_Greenwood");
		ZoneController zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
		Assert.That<ZoneController>(zone, (IResolveConstraint)(object)Is.Not.Null);
		int seed = zone.ResourceLayout.Seed;
		string[] before = NodeFingerprints();
		GameSession gameSession = EidrenServiceRoot.FindOrCreate().GameSession;
		string harvested = before[0].Split('|', StringSplitOptions.None)[0];
		gameSession.ZoneStates.MarkHarvested(zone.ZoneId, harvested);
		yield return LoadZone("Zone_Greenwood");
		zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
		Assert.That<int>(zone.ResourceLayout.Seed, (IResolveConstraint)(object)Is.EqualTo((object)seed));
		Assert.That<string[]>(NodeFingerprints(), (IResolveConstraint)(object)Is.EqualTo((object)before));
		Assert.That<bool>(UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single((ResourceNode node) => node.InstanceId == harvested).IsExhausted, (IResolveConstraint)(object)Is.True, "Ein abgebauter Knoten bleibt abgebaut, bis die Heimkehr die Zone neu wuerfelt (M1.1).", Array.Empty<object>());
	}

	[UnityTest]
	public IEnumerator Homecoming_RerollsTheZone()
	{
		yield return LoadZone("Zone_Greenwood");
		ZoneController zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
		int seed = zone.ResourceLayout.Seed;
		string[] before = NodeFingerprints();
		ZoneRegenerationCoordinator component = EidrenServiceRoot.FindOrCreate().GetComponent<ZoneRegenerationCoordinator>();
		Assert.That<ZoneRegenerationCoordinator>(component, (IResolveConstraint)(object)Is.Not.Null, "§21: Die Regeneration haengt am Kompositionspfad.", Array.Empty<object>());
		Assert.That<int>(component.RegenerateForHomecoming(), (IResolveConstraint)(object)Is.GreaterThan((object)0));
		yield return LoadZone("Zone_Greenwood");
		zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
		Assert.That<int>(zone.ResourceLayout.Seed, (IResolveConstraint)(object)Is.Not.EqualTo((object)seed));
		Assert.That<string[]>(NodeFingerprints(), (IResolveConstraint)(object)Is.Not.EqualTo((object)before));
		Assert.That<int>(zone.ResourceLayout.EconomyNodeCount, (IResolveConstraint)(object)Is.EqualTo((object)72), "Die Menge streut nicht -- nur die Platzierung (M1.3).", Array.Empty<object>());
	}

	private static string[] NodeFingerprints()
	{
		return (from node in UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			select node.InstanceId + "|" + $"{node.transform.position.x:F3}," + $"{node.transform.position.z:F3}").OrderBy((string value) => value, StringComparer.Ordinal).ToArray();
	}

	private static IEnumerator LoadGreenwood()
	{
		yield return LoadZone("Zone_Greenwood");
	}

	private static IEnumerator LoadZone(string sceneName)
	{
		// Entgiftung: die DontDestroyOnLoad-Dienste ueberleben die Vorklassen.
		// Ein toter Testspieler (LifeState != Alive) oder ein haengender
		// Input-Block sperrt sonst den ZonePlayerSpawner — genau das liess
		// CurrentWallPrefab_BlocksPlayerMovement im Suitenlauf flackern,
		// waehrend der Sololauf (jungfraeuliche Dienste) immer gruen war.
		// Beide Eingriffe laufen NUR im vergifteten Fall; der gesunde Lauf
		// bleibt unangetastet.
		EidrenServiceRoot altlast = EidrenServiceRoot.Instance;
		if (altlast != null)
		{
			if (altlast.GameSession != null && altlast.GameSession.LifeState != PlayerLifeState.Alive)
			{
				altlast.GameSession.StartNewGame();
			}
			if (altlast.SceneFlowService != null && altlast.SceneFlowService.IsInputBlocked)
			{
				// Der Disable/Enable-Zyklus nutzt den Produktpfad: OnDisable
				// stoppt eine haengengebliebene Uebergangsroutine und hebt
				// die Eingabesperre auf (SceneFlowService.OnDisable).
				altlast.SceneFlowService.enabled = false;
				altlast.SceneFlowService.enabled = true;
			}
		}
		AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
		Assert.That<AsyncOperation>(load, (IResolveConstraint)(object)Is.Not.Null);
		while (!load.isDone)
		{
			yield return null;
		}
		for (int i = 0; i < 3; i++)
		{
			yield return null;
		}
	}
}
}
