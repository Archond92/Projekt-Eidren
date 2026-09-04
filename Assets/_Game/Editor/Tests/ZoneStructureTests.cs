using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class ZoneStructureTests
{
	private static readonly string[] SceneNames = new string[9] { "HomeBase", "Zone_Greenwood", "Zone_Quarry", "Zone_Marsh", "Zone_EmberRuins", "Zone_TwilightGrove", "Zone_VeilMarsh", "Zone_GreyRifts", "EidraForge" };

	[Test]
	public void ZoneDefinitions_HaveUniqueIdsAndValidSceneKeys()
	{
		ZoneDefinition[] array = (from guid in AssetDatabase.FindAssets("t:ZoneDefinition", new string[1] { "Assets/_Game/Data/Zones" })
			select AssetDatabase.LoadAssetAtPath<ZoneDefinition>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
		Assert.That<ZoneDefinition[]>(array, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)9));
		Assert.That<int>(array.Select((ZoneDefinition zoneDefinition) => zoneDefinition.Id).Distinct(StringComparer.Ordinal).Count(), (IResolveConstraint)(object)Is.EqualTo((object)9));
		ZoneDefinition[] array2 = array;
		foreach (ZoneDefinition definition in array2)
		{
			Assert.That<string>(definition.Id, (IResolveConstraint)(object)Is.Not.Empty);
			Assert.That<string>(definition.SceneKey, (IResolveConstraint)(object)Is.Not.Empty);
			Assert.That<string[]>(SceneNames, (IResolveConstraint)(object)Does.Contain(definition.SceneKey));
			Assert.That<bool>(EditorBuildSettings.scenes.Any((EditorBuildSettingsScene scene) => scene.enabled && scene.path.EndsWith("/" + definition.SceneKey + ".unity", StringComparison.Ordinal)), (IResolveConstraint)(object)Is.True, definition.SceneKey, Array.Empty<object>());
		}
	}

	[TestCase("HomeBase", false)]
	[TestCase("Zone_Greenwood", true)]
	[TestCase("Zone_Quarry", true)]
	[TestCase("Zone_Marsh", true)]
	[TestCase("Zone_EmberRuins", true)]
	[TestCase("Zone_TwilightGrove", true)]
	[TestCase("Zone_VeilMarsh", true)]
	[TestCase("Zone_GreyRifts", true)]
	public void ZoneScene_HasRequiredReferencesAndStaticNavigation(string sceneName, bool requiresNavigation)
	{
		Scene scene = OpenZone(sceneName);
		try
		{
			ZoneController controller = FindInScene<ZoneController>(scene);
			Assert.That<ZoneController>(controller, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<string[]>(controller.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty);
			Assert.That<Transform>(controller.transform.Find("Systems"), (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<Transform>(controller.transform.Find("PlayerSpawnPoints/Spawn_Default"), (IResolveConstraint)(object)Is.Not.Null);
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
				Assert.That<Transform>(controller.GetSpawnForEntry(direction), (IResolveConstraint)(object)Is.Not.Null, direction.ToString(), Array.Empty<object>());
			}
			if (requiresNavigation)
			{
				Assert.That<NavMeshSurface>(controller.NavigationSurface, (IResolveConstraint)(object)Is.Not.Null);
				Assert.That<NavMeshData>(controller.NavigationSurface.navMeshData, (IResolveConstraint)(object)Is.Not.Null, sceneName + " requires a saved NavMesh bake.", Array.Empty<object>());
				Assert.That<CollectObjects>(controller.NavigationSurface.collectObjects, (IResolveConstraint)(object)Is.EqualTo((object)CollectObjects.All));
			}
			else
			{
				Assert.That<NavMeshSurface>(controller.NavigationSurface, (IResolveConstraint)(object)Is.Null);
			}
		}
		finally
		{
			EditorSceneManager.CloseScene(scene, removeScene: true);
		}
	}

	[Test]
	public void DirectionalSpawn_IsOppositeOfPreviousExit()
	{
		Assert.That<MapExitDirection>(ZoneEntryRules.Opposite(MapExitDirection.North), (IResolveConstraint)(object)Is.EqualTo((object)MapExitDirection.South));
		Assert.That<MapExitDirection>(ZoneEntryRules.Opposite(MapExitDirection.East), (IResolveConstraint)(object)Is.EqualTo((object)MapExitDirection.West));
		Assert.That<MapExitDirection>(ZoneEntryRules.Opposite(MapExitDirection.South), (IResolveConstraint)(object)Is.EqualTo((object)MapExitDirection.North));
		Assert.That<MapExitDirection>(ZoneEntryRules.Opposite(MapExitDirection.West), (IResolveConstraint)(object)Is.EqualTo((object)MapExitDirection.East));
		GameObject sessionObject = new GameObject("ZoneEntrySessionTest");
		try
		{
			GameSession gameSession = sessionObject.AddComponent<GameSession>();
			gameSession.Initialize();
			gameSession.RecordConfirmedExit("zone_quarry", MapExitDirection.North, "Zone_Greenwood");
			Assert.That<bool>(ZoneEntryRules.ShouldUseDirectionalSpawn(gameSession, "Zone_Greenwood"), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(gameSession.BeginWorldTravel("zone_greenwood", "Zone_Greenwood"), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(ZoneEntryRules.ShouldUseDirectionalSpawn(gameSession, "Zone_Greenwood"), (IResolveConstraint)(object)Is.False, "WorldMap travel must use Spawn_Default.", Array.Empty<object>());
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(sessionObject);
		}
	}

	[Test]
	public void MissingCameraBounds_HasUnderstandableValidationError()
	{
		GameObject root = new GameObject("ZoneValidationTest");
		try
		{
			Assert.That<bool>(root.AddComponent<ZoneController>().GetValidationErrors().Any((string error) => error.Contains("cameraBounds", StringComparison.Ordinal)), (IResolveConstraint)(object)Is.True);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void MissingNavMeshSurface_IsDetectedForOutdoorZone()
	{
		Scene scene = OpenZone("Zone_Greenwood");
		try
		{
			ZoneController controller = FindInScene<ZoneController>(scene);
			FieldInfo field = typeof(ZoneController).GetField("navigationSurface", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That<FieldInfo>(field, (IResolveConstraint)(object)Is.Not.Null);
			field.SetValue(controller, null);
			Assert.That<bool>(controller.GetValidationErrors().Any((string error) => error.Contains("NavMeshSurface", StringComparison.Ordinal)), (IResolveConstraint)(object)Is.True);
		}
		finally
		{
			EditorSceneManager.CloseScene(scene, removeScene: true);
		}
	}

	[Test]
	public void AllZoneSpawns_AreOutsideExitVolumes()
	{
		string[] sceneNames = SceneNames;
		foreach (string sceneName in sceneNames)
		{
			Scene scene = OpenZone(sceneName);
			try
			{
				Assert.That<IEnumerable<string>>(from error in FindInScene<ZoneController>(scene).GetValidationErrors()
					where error.Contains("overlaps exit", StringComparison.Ordinal)
					select error, (IResolveConstraint)(object)Is.Empty, sceneName, Array.Empty<object>());
			}
			finally
			{
				EditorSceneManager.CloseScene(scene, removeScene: true);
			}
		}
	}

	[Test]
	public void HomeBase_HasNoGiftedStorageOrWorkbench()
	{
		Scene scene = OpenZone("HomeBase");
		try
		{
			StorageContainer storageContainer = FindInScene<StorageContainer>(scene);
			WorkbenchController workbench = FindInScene<WorkbenchController>(scene);
			Assert.That<StorageContainer>(storageContainer, (IResolveConstraint)(object)Is.Null);
			Assert.That<WorkbenchController>(workbench, (IResolveConstraint)(object)Is.Null);
		}
		finally
		{
			EditorSceneManager.CloseScene(scene, removeScene: true);
		}
	}

	[Test]
	public void ZoneScenes_CarryNoBakedResourceNodes()
	{
		string[] sceneNames = SceneNames;
		foreach (string sceneName in sceneNames)
		{
			Scene scene = OpenZone(sceneName);
			try
			{
				List<string> baked = new List<string>();
				GameObject[] rootGameObjects = scene.GetRootGameObjects();
				for (int j = 0; j < rootGameObjects.Length; j++)
				{
					ResourceNode[] componentsInChildren = rootGameObjects[j].GetComponentsInChildren<ResourceNode>(includeInactive: true);
					foreach (ResourceNode node in componentsInChildren)
					{
						baked.Add(node.name);
					}
				}
				Assert.That<List<string>>(baked, (IResolveConstraint)(object)Is.Empty, sceneName + ": Die Platzierung gehoert in den Seed, nicht in die Szene:\n" + string.Join("\n", baked), Array.Empty<object>());
			}
			finally
			{
				EditorSceneManager.CloseScene(scene, removeScene: true);
			}
		}
	}

	[Test]
	public void EveryOutdoorZone_ProvidesRoomForItsGeneratedNodes()
	{
		string[] array = new string[4] { "Zone_Greenwood", "Zone_Quarry", "Zone_Marsh", "Zone_EmberRuins" };
		foreach (string sceneName in array)
		{
			Scene scene = OpenZone(sceneName);
			try
			{
				ZoneController zone = FindInScene<ZoneController>(scene);
				Assert.That<ZoneController>(zone, (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
				Assert.That<Collider>(zone.WalkableGround, (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
				int budget = zone.Definition.ResourceAllocations.Sum((ZoneResourceAllocation allocation) => allocation.Count) + zone.Definition.SideNodeAllocations.Sum((ZoneResourceAllocation allocation) => allocation.Count);
				Bounds ground = zone.WalkableGround.bounds;
				float usable = (ground.size.x - 12f) * (ground.size.z - 12f);
				Assert.That<int>(budget, (IResolveConstraint)(object)Is.GreaterThan((object)0), sceneName, Array.Empty<object>());
				Assert.That<float>(usable / (float)budget, (IResolveConstraint)(object)Is.GreaterThan((object)25f), $"{sceneName}: {budget} Knoten auf {usable:0} m² " + "waeren dichter als M1.7 annimmt.", Array.Empty<object>());
			}
			finally
			{
				EditorSceneManager.CloseScene(scene, removeScene: true);
			}
		}
	}

	private static Scene OpenZone(string sceneName)
	{
		return EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + sceneName + ".unity", OpenSceneMode.Additive);
	}

	private static T FindInScene<T>(Scene scene) where T : Component
	{
		GameObject[] rootGameObjects = scene.GetRootGameObjects();
		for (int i = 0; i < rootGameObjects.Length; i++)
		{
			T result = rootGameObjects[i].GetComponentInChildren<T>(includeInactive: true);
			if (result != null)
			{
				return result;
			}
		}
		return null;
	}
}
}
