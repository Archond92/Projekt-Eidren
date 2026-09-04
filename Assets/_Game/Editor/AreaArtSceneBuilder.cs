using Eidren.Composition;
using Eidren.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class AreaArtSceneBuilder
{
	public const string RootName = "AreaArt";

	private const float OutdoorSize = 80f;

	private const float HomeSize = 48f;

	private const string SceneRoot = "Assets/_Game/Scenes";

	[MenuItem("Eidren/Art/Areas/Build Greenwood")]
	public static void BuildGreenwood()
	{
		Build("Zone_Greenwood", "Greenwood", homeBase: false);
	}

	[MenuItem("Eidren/Art/Areas/Build Marsh")]
	public static void BuildMarsh()
	{
		Build("Zone_Marsh", "Marsh", homeBase: false);
	}

	[MenuItem("Eidren/Art/Areas/Build Quarry")]
	public static void BuildQuarry()
	{
		Build("Zone_Quarry", "Quarry", homeBase: false);
	}

	[MenuItem("Eidren/Art/Areas/Build Ember Ruins")]
	public static void BuildEmberRuins()
	{
		Build("Zone_EmberRuins", "EmberRuins", homeBase: false);
	}

	[MenuItem("Eidren/Art/Areas/Build Twilight Grove")]
	public static void BuildTwilightGrove()
	{
		Build("Zone_TwilightGrove", "TwilightGrove", homeBase: false);
	}

	[MenuItem("Eidren/Art/Areas/Build Veil Marsh")]
	public static void BuildVeilMarsh()
	{
		Build("Zone_VeilMarsh", "VeilMarsh", homeBase: false);
	}

	[MenuItem("Eidren/Art/Areas/Build Grey Rifts")]
	public static void BuildGreyRifts()
	{
		Build("Zone_GreyRifts", "GreyRifts", homeBase: false);
	}

	[MenuItem("Eidren/Art/Areas/Build Home Base")]
	public static void BuildHomeBase()
	{
		Build("HomeBase", "HomeBase", homeBase: true);
	}

	public static void BuildAllForAutomation()
	{
		AreaArtAssetBuilder.BuildAll();
		BuildGreenwood();
		BuildMarsh();
		BuildQuarry();
		BuildEmberRuins();
		BuildTwilightGrove();
		BuildVeilMarsh();
		BuildGreyRifts();
		BuildHomeBase();
		Debug.Log("All area scenes built and verified.");
	}

	public static void BuildTierTwoForAutomation()
	{
		BuildTwilightGrove();
		BuildVeilMarsh();
		BuildGreyRifts();
		Debug.Log("All tier-two area scenes built and verified.");
	}

	private static void Build(string sceneName, string areaKey, bool homeBase)
	{
		string scenePath = "Assets/_Game/Scenes/" + sceneName + ".unity";
		string areaPath = "Assets/_Game/Data/AreaArt/AreaArt_" + areaKey + ".asset";
		ZoneAreaArtDefinition areaArt = AssetDatabase.LoadAssetAtPath<ZoneAreaArtDefinition>(areaPath);
		if (areaArt == null)
		{
			throw new InvalidOperationException("Missing area art: " + areaPath);
		}
		Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
		GameObject gameObject = scene.GetRootGameObjects().FirstOrDefault((GameObject item) => item.name == "ZoneRoot");
		if (gameObject == null)
		{
			throw new InvalidOperationException("ZoneRoot is missing in " + sceneName + ".");
		}
		Transform environment = gameObject.transform.Find("EnvironmentRoot");
		Transform ground = ((environment != null) ? environment.Find("WalkableGround") : null);
		if (ground == null)
		{
			throw new InvalidOperationException("WalkableGround is missing in " + sceneName + ".");
		}
		RemoveLegacy(gameObject.transform, environment);
		Transform areaRoot = ReplaceRoot(environment, "AreaArt");
		float size = (homeBase ? 48f : 80f);
		AreaArtGroundBuilder.Build(ground, areaRoot, areaArt, areaKey, size);
		BuildDecoration(areaRoot, areaArt, areaKey);
		BuildMood(areaRoot, areaArt);
		ConfigureSpawns(gameObject.transform, homeBase, size);
		// G-002: nach ConfigureSpawns, damit der Bewuchs den Startpunkt kennt und
		// ihn freilaesst. Vor BuildNavMesh, aber ohne Wirkung darauf: der Bewuchs
		// traegt keine Collider.
		AreaArtGroundCoverBuilder.Build(areaRoot, areaArt, areaKey, size, SpawnLocal(homeBase, size));
		AssignAreaArt(gameObject, areaArt);
		NavMeshSurface surface = gameObject.GetComponentInChildren<NavMeshSurface>(includeInactive: true);
		if (surface != null)
		{
			surface.BuildNavMesh();
		}
		EditorSceneManager.MarkSceneDirty(scene);
		if (!EditorSceneManager.SaveScene(scene, scenePath))
		{
			throw new IOException("Could not save " + scenePath + ".");
		}
		VerifyPersisted(scenePath, areaKey, homeBase, size);
	}

	private static void BuildDecoration(Transform root, ZoneAreaArtDefinition areaArt, string key)
	{
		Transform decorationRoot = new GameObject("AuthoredDecoration").transform;
		decorationRoot.SetParent(root, worldPositionStays: false);
		Vector3[] positions = new Vector3[12]
		{
			new Vector3(-27f, 0f, 22f),
			new Vector3(-20f, 0f, 28f),
			new Vector3(25f, 0f, 25f),
			new Vector3(29f, 0f, 12f),
			new Vector3(-29f, 0f, -10f),
			new Vector3(-22f, 0f, -25f),
			new Vector3(22f, 0f, -27f),
			new Vector3(29f, 0f, -14f),
			new Vector3(-12f, 0f, 30f),
			new Vector3(12f, 0f, 29f),
			new Vector3(-30f, 0f, 3f),
			new Vector3(30f, 0f, -2f)
		};
		IReadOnlyList<AreaArtDecoration> decorations = areaArt.Decorations;
		if (decorations.Count == 0)
		{
			return;
		}
		int count = ((key == "HomeBase") ? 6 : positions.Length);
		for (int i = 0; i < count; i++)
		{
			AreaArtDecoration decoration = decorations[i % decorations.Count];
			if (!(decoration.Prefab == null))
			{
				GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(decoration.Prefab);
				obj.name = $"{key}_{decoration.Prefab.name}_{i + 1:00}";
				obj.transform.SetParent(decorationRoot, worldPositionStays: false);
				obj.transform.localPosition = ((key == "HomeBase") ? (positions[i] * 0.55f) : positions[i]);
				obj.transform.localRotation = Quaternion.Euler(0f, (float)i * 47f % 360f, 0f);
				Collider[] componentsInChildren = obj.GetComponentsInChildren<Collider>(includeInactive: true);
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					UnityEngine.Object.DestroyImmediate(componentsInChildren[j]);
				}
			}
		}
	}

	private static void BuildMood(Transform root, ZoneAreaArtDefinition areaArt)
	{
		VolumeProfile profile = areaArt.VolumeProfile as VolumeProfile;
		if (!(profile == null))
		{
			GameObject gameObject = new GameObject("AreaArtGlobalVolume");
			gameObject.transform.SetParent(root, worldPositionStays: false);
			Volume volume = gameObject.AddComponent<Volume>();
			volume.isGlobal = true;
			volume.priority = 2f;
			volume.sharedProfile = profile;
		}
	}

	// Der Startpunkt wird an zwei Stellen gebraucht: ConfigureSpawns setzt ihn,
	// der Bodenbewuchs muss ihn freilassen. Eine Quelle statt zwei Rechnungen.
	private static Vector3 SpawnLocal(bool homeBase, float size)
	{
		float edge = size * 0.5f - 7f;
		return homeBase ? new Vector3(0f, 0.05f, 0f - edge) : new Vector3(-8f, 0.05f, 0f - edge);
	}

	private static void ConfigureSpawns(Transform zoneRoot, bool homeBase, float size)
	{
		Transform transform = zoneRoot.Find("PlayerSpawnPoints");
		if (transform == null)
		{
			throw new InvalidOperationException("Spawn root is missing.");
		}
		Transform transform2 = transform.Find("Spawn_Default");
		transform2.position = SpawnLocal(homeBase, size);
		Vector3 toCenter = -transform2.position;
		toCenter.y = 0f;
		transform2.rotation = Quaternion.LookRotation(toCenter.normalized);
	}

	private static void AssignAreaArt(GameObject zoneRoot, ZoneAreaArtDefinition areaArt)
	{
		ZoneController controller = zoneRoot.GetComponent<ZoneController>();
		ZoneDefinition obj = ((controller != null) ? controller.Definition : null);
		if (obj == null)
		{
			throw new InvalidOperationException("Zone definition is missing.");
		}
		SerializedObject serializedObject = new SerializedObject(obj);
		serializedObject.FindProperty("areaArt").objectReferenceValue = areaArt;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(obj);
		AssetDatabase.SaveAssetIfDirty(obj);
	}

	private static void RemoveLegacy(Transform zoneRoot, Transform environment)
	{
		string[] array = new string[16]
		{
			"StyleProofReferenceArea", "Ground_StyleLayer", "GroundTransition_West", "GroundTransition_North", "ReferencePath", "SafeArea", "LargeTree_Trunk", "LargeRock_Shelf", "MassiveDeadTree", "MassiveObstacle",
			"LargeObstacle_West", "LargeObstacle_East", "LargeRock_East", "RuinsWall_West", "RuinsPillar_NorthEast", "RuinsWall_SouthEast"
		};
		foreach (string name in array)
		{
			Transform found = FindRecursive(zoneRoot, name);
			if (found != null)
			{
				UnityEngine.Object.DestroyImmediate(found.gameObject);
			}
		}
	}

	private static Transform ReplaceRoot(Transform parent, string name)
	{
		Transform existing = parent.Find(name);
		if (existing != null)
		{
			UnityEngine.Object.DestroyImmediate(existing.gameObject);
		}
		Transform transform = new GameObject(name).transform;
		transform.SetParent(parent, worldPositionStays: false);
		return transform;
	}

	private static Transform FindRecursive(Transform root, string name)
	{
		if (root.name == name)
		{
			return root;
		}
		foreach (Transform item in root)
		{
			Transform found = FindRecursive(item, name);
			if (found != null)
			{
				return found;
			}
		}
		return null;
	}

	private static void VerifyPersisted(string scenePath, string areaKey, bool homeBase, float size)
	{
		GameObject zoneRoot = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single).GetRootGameObjects().First((GameObject item) => item.name == "ZoneRoot");
		Transform transform = zoneRoot.transform.Find("EnvironmentRoot");
		Transform areaRoot = transform.Find("AreaArt");
		Transform ground = transform.Find("WalkableGround");
		Transform defaultSpawn = zoneRoot.transform.Find("PlayerSpawnPoints/Spawn_Default");
		if (areaRoot == null || ground == null || !ground.GetComponent<Renderer>().enabled)
		{
			throw new InvalidOperationException(areaKey + " did not persist its area-art ground.");
		}
		float edge = size * 0.5f - 7f;
		if (Mathf.Abs(defaultSpawn.position.z + edge) > 0.02f || (!homeBase && Mathf.Abs(defaultSpawn.position.x + 8f) > 0.02f))
		{
			throw new InvalidOperationException(areaKey + " did not persist its edge spawn.");
		}
		string[] array = new string[4] { "Ground_StyleLayer", "ReferencePath", "LargeObstacle_West", "RuinsWall_West" };
		foreach (string name in array)
		{
			if (FindRecursive(zoneRoot.transform, name) != null)
			{
				throw new InvalidOperationException(areaKey + " still contains legacy '" + name + "'.");
			}
		}
	}
}
}
