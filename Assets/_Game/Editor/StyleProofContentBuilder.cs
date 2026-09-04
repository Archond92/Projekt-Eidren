using Eidren.Composition;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.Presentation;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine;
using Random = System.Random;

namespace Eidren.Editor
{
public static class StyleProofContentBuilder
{
	public const string ScenePath = "Assets/_Game/Scenes/Zone_Greenwood.unity";

	public const string ArtRoot = "Assets/_Game/Art/StyleProof";

	public const string MaterialRoot = "Assets/_Game/Art/StyleProof/Materials";

	public const string TextureRoot = "Assets/_Game/Art/StyleProof/Textures";

	public const string PrefabRoot = "Assets/_Game/Prefabs/Environment/StyleProof";

	public const string VolumePath = "Assets/_Game/Settings/Greenwood_StyleProof_Volume.asset";

	[MenuItem("Eidren/Art/Build V0.1 Style Proof")]
	public static void BuildAll()
	{
		EnsureFolders();
		ConfigureAuthoredTextures();
		Dictionary<string, Material> materials = BuildMaterials();
		Dictionary<string, GameObject> prefabs = BuildPrefabs();
		BuildResourceVisuals();
		BuildWildlingVisual();
		BuildVisualLibrary();
		VolumeProfile volume = BuildVolume();
		BuildGreenwood(materials, prefabs, volume);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("[StyleProof] Greenwood reference area, reusable assets, materials and volume built successfully.");
	}

	private static void EnsureFolders()
	{
		EnsureFolder("Assets/_Game/Art", "StyleProof");
		EnsureFolder("Assets/_Game/Art/StyleProof", "Materials");
		EnsureFolder("Assets/_Game/Art/StyleProof", "Textures");
		EnsureFolder("Assets/_Game/Prefabs", "Environment");
		EnsureFolder("Assets/_Game/Prefabs/Environment", "StyleProof");
		EnsureFolder("Assets/_Game/Resources", "StyleProof");
	}

	private static void ConfigureAuthoredTextures()
	{
		ConfigureTexture("Assets/_Game/Art/StyleProof/Textures/greenwood_ground_v01.png", sprites: false, 1, 1);
		ConfigureTexture("Assets/_Game/Art/StyleProof/Textures/greenwood_rocks_v01.png", sprites: true, 3, 1);
		ConfigureTexture("Assets/_Game/Art/StyleProof/Textures/greenwood_trees_v01.png", sprites: true, 3, 1);
		ConfigureTexture("Assets/_Game/Art/StyleProof/Textures/greenwood_vegetation_v01.png", sprites: true, 4, 3);
		ConfigureTexture("Assets/_Game/Art/StyleProof/Textures/greenwood_ruins_vfx_v01.png", sprites: true, 4, 3, 1);
		ConfigureTexture("Assets/_Game/Art/StyleProof/Textures/greenwood_wildling_v01.png", sprites: true, 1, 1);
	}

	private static void ConfigureTexture(string path, bool sprites, int columns, int rows, int centeredFromRow = int.MaxValue)
	{
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
		TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
		if (importer == null)
		{
			throw new InvalidOperationException("Texture importer is missing for '" + path + "'.");
		}
		importer.sRGBTexture = true;
		importer.mipmapEnabled = !sprites;
		importer.alphaIsTransparency = sprites;
		importer.wrapMode = (sprites ? TextureWrapMode.Clamp : TextureWrapMode.Repeat);
		importer.textureCompression = TextureImporterCompression.CompressedHQ;
		importer.maxTextureSize = 2048;
		if (!sprites)
		{
			importer.textureType = TextureImporterType.Default;
			importer.SaveAndReimport();
			return;
		}
		importer.textureType = TextureImporterType.Sprite;
		importer.spriteImportMode = SpriteImportMode.Multiple;
		importer.spritePixelsPerUnit = 256f;
		Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		float cellWidth = (float)texture.width / (float)columns;
		float cellHeight = (float)texture.height / (float)rows;
		string stem = Path.GetFileNameWithoutExtension(path);
		SpriteMetaData[] metadata = new SpriteMetaData[columns * rows];
		for (int row = 0; row < rows; row++)
		{
			for (int column = 0; column < columns; column++)
			{
				int index = row * columns + column;
				metadata[index] = new SpriteMetaData
				{
					name = $"{stem}_{index:00}",
					rect = new Rect((float)column * cellWidth, (float)texture.height - (float)(row + 1) * cellHeight, cellWidth, cellHeight),
					alignment = 9,
					pivot = ((row >= centeredFromRow) ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.02f))
				};
			}
		}
		importer.spritesheet = metadata;
		importer.SaveAndReimport();
	}

	private static Dictionary<string, Material> BuildMaterials()
	{
		Dictionary<string, Material> dictionary = new Dictionary<string, Material>();
		Texture2D groundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/StyleProof/Textures/greenwood_ground_v01.png");
		dictionary["Ground"] = Material("M_SP_Ground", new Color(0.74f, 0.79f, 0.69f), 0.08f, groundTexture, new Vector2(2.6f, 2.6f));
		dictionary["GroundVariation"] = Material("M_SP_GroundVariation", new Color(0.53f, 0.64f, 0.52f), 0.04f, groundTexture, new Vector2(2.2f, 2.2f));
		dictionary["Rock"] = Material("M_SP_Rock", new Color(0.31f, 0.34f, 0.32f), 0.18f);
		dictionary["RockMoss"] = Material("M_SP_RockMoss", new Color(0.31f, 0.4f, 0.25f), 0.08f);
		dictionary["Wood"] = Material("M_SP_Wood", new Color(0.25f, 0.16f, 0.1f), 0.12f);
		dictionary["LeafDark"] = Material("M_SP_LeafDark", new Color(0.12f, 0.27f, 0.17f), 0.04f);
		dictionary["LeafMid"] = Material("M_SP_LeafMid", new Color(0.2f, 0.38f, 0.22f), 0.04f);
		dictionary["Plant"] = Material("M_SP_Plant", new Color(0.27f, 0.44f, 0.24f), 0.02f);
		dictionary["Accent"] = Material("M_SP_AccentPlant", new Color(0.42f, 0.32f, 0.48f), 0.08f);
		dictionary["Ruin"] = Material("M_SP_Ruin", new Color(0.57f, 0.61f, 0.54f), 0.16f, groundTexture, new Vector2(1.3f, 1.3f));
		dictionary["Rune"] = Material("M_SP_EidrenRune", new Color(0.12f, 0.55f, 0.7f), 0.05f, null, Vector2.one, new Color(0.05f, 1.3f, 2.2f));
		dictionary["Loot"] = Material("M_SP_LootAccent", new Color(0.72f, 0.52f, 0.15f), 0.2f, null, Vector2.one, new Color(0.5f, 0.25f, 0.03f));
		return dictionary;
	}

	private static Material Material(string name, Color color, float smoothness, Texture2D texture = null, Vector2 textureScale = default(Vector2), Color emission = default(Color))
	{
		string path = "Assets/_Game/Art/StyleProof/Materials/" + name + ".mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		Shader shader = Shader.Find("Universal Render Pipeline/Lit");
		if (material == null)
		{
			material = new Material(shader)
			{
				name = name
			};
			AssetDatabase.CreateAsset(material, path);
		}
		else if (material.shader != shader)
		{
			material.shader = shader;
		}
		material.SetColor("_BaseColor", color);
		material.SetFloat("_Smoothness", smoothness);
		if (texture != null)
		{
			material.SetTexture("_BaseMap", texture);
			material.SetTextureScale("_BaseMap", (textureScale == default(Vector2)) ? Vector2.one : textureScale);
		}
		if (emission.maxColorComponent > 0f)
		{
			material.EnableKeyword("_EMISSION");
			material.SetColor("_EmissionColor", emission);
			material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
		}
		EditorUtility.SetDirty(material);
		return material;
	}

	private static Dictionary<string, GameObject> BuildPrefabs()
	{
		Dictionary<string, GameObject> dictionary = new Dictionary<string, GameObject>();
		Sprite[] rocks = LoadSprites("Assets/_Game/Art/StyleProof/Textures/greenwood_rocks_v01.png");
		Sprite[] trees = LoadSprites("Assets/_Game/Art/StyleProof/Textures/greenwood_trees_v01.png");
		Sprite[] vegetation = LoadSprites("Assets/_Game/Art/StyleProof/Textures/greenwood_vegetation_v01.png");
		Sprite[] ruins = LoadSprites("Assets/_Game/Art/StyleProof/Textures/greenwood_ruins_vfx_v01.png");
		dictionary["RockSmall"] = SaveSpriteProp("SP_Rock_Small", rocks[0], "visual.prop.rock_small");
		dictionary["RockMedium"] = SaveSpriteProp("SP_Rock_Medium", rocks[1], "visual.prop.rock_medium");
		dictionary["RockLarge"] = SaveSpriteProp("SP_Rock_Large", rocks[2], "visual.prop.rock_large");
		dictionary["TreeA"] = SaveSpriteProp("SP_Tree_A", trees[0], "visual.prop.tree_a");
		dictionary["TreeB"] = SaveSpriteProp("SP_Tree_B", trees[1], "visual.prop.tree_b");
		dictionary["TreeC"] = SaveSpriteProp("SP_Tree_C", trees[2], "visual.prop.tree_c");
		dictionary["PlantA"] = SaveSpriteProp("SP_Plant_Fern", vegetation[0], "visual.prop.fern");
		dictionary["PlantB"] = SaveSpriteProp("SP_Plant_Bush", vegetation[1], "visual.prop.bush");
		dictionary["PlantC"] = SaveSpriteProp("SP_Plant_Flowers", vegetation[2], "visual.prop.flowers");
		dictionary["GroundCoverA"] = SaveSpriteProp("SP_GroundCover_Grass", vegetation[3], "visual.prop.ground_grass");
		dictionary["GroundCoverB"] = SaveSpriteProp("SP_GroundCover_Moss", vegetation[5], "visual.prop.ground_moss");
		dictionary["Accent"] = SaveSpriteProp("SP_Accent_GlowMushrooms", vegetation[7], "visual.prop.glow_mushrooms");
		dictionary["RuinWallA"] = SaveSpriteProp("SP_RuinWall_A", ruins[0], "visual.prop.ruin_wall_a");
		dictionary["RuinWallB"] = SaveSpriteProp("SP_RuinWall_B", ruins[1], "visual.prop.ruin_wall_b");
		dictionary["Monument"] = SaveSpriteProp("SP_RuinMonument", ruins[2], "visual.prop.ruin_monument");
		dictionary["Rune"] = SaveSpriteProp("SP_EidrenRune", ruins[3], "visual.prop.rune");
		GameObject eidraReference = new GameObject("SP_EidraReference");
		eidraReference.AddComponent<StyleProofEidraCompanion>();
		dictionary["EidraReference"] = SavePrefab(eidraReference, "SP_EidraReference");
		return dictionary;
	}

	private static void BuildResourceVisuals()
	{
		Sprite[] rocks = LoadSprites("Assets/_Game/Art/StyleProof/Textures/greenwood_rocks_v01.png");
		Sprite[] vegetation = LoadSprites("Assets/_Game/Art/StyleProof/Textures/greenwood_vegetation_v01.png");
		SaveSpriteVisualAtPath("Assets/_Game/Prefabs/Resources/Visuals/Tree_Active.prefab", "Tree_Active", vegetation[8], "visual.tree.active");
		SaveSpriteVisualAtPath("Assets/_Game/Prefabs/Resources/Visuals/FiberPlant_Active.prefab", "FiberPlant_Active", vegetation[9], "visual.fiber_plant.active");
		SaveSpriteVisualAtPath("Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Active.prefab", "CopperVein_Active", vegetation[10], "visual.copper_vein.active");
		SaveSpriteVisualAtPath("Assets/_Game/Prefabs/Resources/Visuals/StoneDeposit_Active.prefab", "StoneDeposit_Active", rocks[0], "visual.stone_deposit.active");
	}

	private static void BuildWildlingVisual()
	{
		RequireGeometryPrefab("Assets/_Game/Prefabs/Enemies/Visuals/Wildling_Visual.prefab");
	}

	private static Sprite[] LoadSprites(string path)
	{
		return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy((Sprite sprite) => sprite.name, StringComparer.Ordinal)
			.ToArray();
	}

	private static GameObject SaveSpriteProp(string name, Sprite sprite, string visualId)
	{
		return RequireGeometryPrefab("Assets/_Game/Prefabs/Environment/StyleProof/" + name + ".prefab");
	}

	private static void SaveSpriteVisualAtPath(string path, string name, Sprite sprite, string visualId)
	{
		RequireGeometryPrefab(path);
	}

	private static GameObject RequireGeometryPrefab(string path)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		if (prefab == null || prefab.GetComponentInChildren<MeshRenderer>(includeInactive: true) == null || prefab.GetComponentInChildren<SpriteRenderer>(includeInactive: true) != null)
		{
			throw new InvalidOperationException("Authored geometry prefab is missing: " + path + ". Run the visual-scale migration instead of rebuilding a billboard.");
		}
		return prefab;
	}

	private static VisualScaleTable ScaleTable()
	{
		return VisualScaleTableBuilder.Load();
	}

	private static void BuildVisualLibrary()
	{
		StyleProofVisualLibrary library = AssetDatabase.LoadAssetAtPath<StyleProofVisualLibrary>("Assets/_Game/Resources/StyleProof/StyleProofVisualLibrary.asset");
		if (library == null)
		{
			library = ScriptableObject.CreateInstance<StyleProofVisualLibrary>();
			AssetDatabase.CreateAsset(library, "Assets/_Game/Resources/StyleProof/StyleProofVisualLibrary.asset");
		}
		Sprite[] atlas = LoadSprites("Assets/_Game/Art/StyleProof/Textures/greenwood_ruins_vfx_v01.png");
		SerializedObject serialized = new SerializedObject(library);
		SerializedProperty cues = serialized.FindProperty("styleCueSprites");
		cues.arraySize = 8;
		for (int i = 0; i < 8; i++)
		{
			cues.GetArrayElementAtIndex(i).objectReferenceValue = atlas[i + 4];
		}
		string[] forestPropPaths = new string[12]
		{
			"Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_A.prefab", "Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_B.prefab", "Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_C.prefab", "Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Large.prefab", "Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Medium.prefab", "Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Small.prefab", "Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Bush.prefab", "Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Fern.prefab", "Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Flowers.prefab", "Assets/_Game/Prefabs/Environment/StyleProof/SP_GroundCover_Grass.prefab",
			"Assets/_Game/Prefabs/Environment/StyleProof/SP_RuinWall_A.prefab", "Assets/_Game/Prefabs/Environment/StyleProof/SP_Accent_GlowMushrooms.prefab"
		};
		SerializedProperty forestProps = serialized.FindProperty("forestPropPrefabs");
		forestProps.arraySize = forestPropPaths.Length;
		for (int index = 0; index < forestPropPaths.Length; index++)
		{
			forestProps.GetArrayElementAtIndex(index).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(forestPropPaths[index]);
		}
		serialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(library);
	}

	private static GameObject SavePrefab(GameObject root, string name)
	{
		string path = "Assets/_Game/Prefabs/Environment/StyleProof/" + name + ".prefab";
		GameObject result = PrefabUtility.SaveAsPrefabAsset(root, path);
		UnityEngine.Object.DestroyImmediate(root);
		return result;
	}

	private static VolumeProfile BuildVolume()
	{
		VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Game/Settings/Greenwood_StyleProof_Volume.asset");
		if (profile == null)
		{
			profile = ScriptableObject.CreateInstance<VolumeProfile>();
			AssetDatabase.CreateAsset(profile, "Assets/_Game/Settings/Greenwood_StyleProof_Volume.asset");
		}
		profile.components.RemoveAll((VolumeComponent component) => component == null);
		if (!profile.TryGet<Tonemapping>(out var tone))
		{
			tone = AddVolumeComponent<Tonemapping>(profile);
		}
		tone.mode.Override(TonemappingMode.ACES);
		if (!profile.TryGet<Bloom>(out var bloom))
		{
			bloom = AddVolumeComponent<Bloom>(profile);
		}
		bloom.threshold.Override(1.05f);
		bloom.intensity.Override(0.18f);
		bloom.scatter.Override(0.55f);
		if (!profile.TryGet<ColorAdjustments>(out var color))
		{
			color = AddVolumeComponent<ColorAdjustments>(profile);
		}
		color.postExposure.Override(-0.12f);
		color.contrast.Override(9f);
		color.saturation.Override(-6f);
		color.colorFilter.Override(new Color(0.96f, 1f, 0.95f));
		if (!profile.TryGet<Vignette>(out var vignette))
		{
			vignette = AddVolumeComponent<Vignette>(profile);
		}
		vignette.intensity.Override(0.08f);
		vignette.smoothness.Override(0.45f);
		EditorUtility.SetDirty(profile);
		return profile;
	}

	private static T AddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
	{
		T component = ScriptableObject.CreateInstance<T>();
		component.name = typeof(T).Name;
		component.active = true;
		profile.components.Add(component);
		AssetDatabase.AddObjectToAsset(component, profile);
		EditorUtility.SetDirty(component);
		EditorUtility.SetDirty(profile);
		return component;
	}

	private static void BuildGreenwood(IReadOnlyDictionary<string, Material> materials, IReadOnlyDictionary<string, GameObject> prefabs, VolumeProfile volumeProfile)
	{
		Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_Greenwood.unity", OpenSceneMode.Single);
		Transform zoneRoot = Find(scene, "ZoneRoot");
		if (zoneRoot == null)
		{
			throw new InvalidOperationException("ZoneRoot is missing.");
		}
		Transform existing = zoneRoot.Find("StyleProofReferenceArea");
		if (existing != null)
		{
			UnityEngine.Object.DestroyImmediate(existing.gameObject);
		}
		GameObject gameObject = new GameObject("StyleProofReferenceArea");
		gameObject.transform.SetParent(zoneRoot, worldPositionStays: false);
		Transform transform = gameObject.transform;
		BuildGround(transform, materials);
		StyleLegacyObstacles(transform, zoneRoot, prefabs);
		BuildRockGroups(transform, prefabs);
		BuildTreeGroups(transform, prefabs);
		BuildVegetation(transform, prefabs);
		BuildRuin(transform, prefabs, materials);
		BuildWorldItem(transform);
		Place(transform, prefabs["EidraReference"], "StyleProof_ActiveEidra", new Vector3(-6f, 1f, 11f), 0f, 1f);
		BuildLighting(transform, volumeProfile);
		NavMeshSurface surface = zoneRoot.GetComponentInChildren<NavMeshSurface>(includeInactive: true);
		if (surface != null)
		{
			surface.BuildNavMesh();
		}
		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Zone_Greenwood.unity");
	}

	private static void BuildGround(Transform parent, IReadOnlyDictionary<string, Material> materials)
	{
		AddPrimitive(parent, "Ground_StyleLayer", PrimitiveType.Cube, new Vector3(0f, 0.01f, 0f), new Vector3(78f, 0.02f, 78f), materials["Ground"]).isStatic = true;
		AddPrimitive(parent, "GroundTransition_West", PrimitiveType.Cube, new Vector3(-24f, 0.026f, 18f), new Vector3(22f, 0.012f, 36f), materials["GroundVariation"]).transform.localRotation = Quaternion.Euler(0f, 9f, 0f);
		AddPrimitive(parent, "GroundTransition_North", PrimitiveType.Cylinder, new Vector3(11f, 0.027f, 25f), new Vector3(12f, 0.012f, 8f), materials["GroundVariation"]);
	}

	private static void StyleLegacyObstacles(Transform parent, Transform zoneRoot, IReadOnlyDictionary<string, GameObject> prefabs)
	{
		Transform environment = zoneRoot.Find("EnvironmentRoot");
		if (environment == null)
		{
			return;
		}
		string[] array = new string[4] { "WalkableGround", "LargeTree_Trunk", "LargeObstacle_West", "LargeObstacle_East" };
		foreach (string obstacleName in array)
		{
			Transform obstacle = environment.Find(obstacleName);
			Renderer renderer = ((obstacle != null) ? obstacle.GetComponent<Renderer>() : null);
			if (renderer != null)
			{
				renderer.enabled = false;
			}
		}
		PlaceVisualOnly(parent, prefabs["TreeB"], "ObstacleCover_CenterNorth", new Vector3(0f, 0f, 3.1f), 18f, 1.12f);
		PlaceVisualOnly(parent, prefabs["TreeA"], "ObstacleCover_CenterSouth", new Vector3(0f, 0f, -3.2f), -21f, 1.05f);
		PlaceVisualOnly(parent, prefabs["TreeC"], "ObstacleCover_West", new Vector3(-12f, 0f, 7f), 37f, 1.08f);
		PlaceVisualOnly(parent, prefabs["RockLarge"], "ObstacleCover_EastLarge", new Vector3(12f, 0f, -7f), -24f, 1.15f);
		PlaceVisualOnly(parent, prefabs["RockMedium"], "ObstacleCover_EastMedium", new Vector3(10.4f, 0f, -5.6f), 31f, 1f);
	}

	private static void BuildRockGroups(Transform parent, IReadOnlyDictionary<string, GameObject> prefabs)
	{
		Place(parent, prefabs["RockSmall"], "Rock_S_01", new Vector3(-8f, 0f, -5f), 23f, 0.9f);
		Place(parent, prefabs["RockSmall"], "Rock_S_02", new Vector3(17f, 0f, 13f), -31f, 1.1f);
		Place(parent, prefabs["RockMedium"], "Rock_M_01", new Vector3(-25f, 0f, 8f), 52f, 1f);
		Place(parent, prefabs["RockMedium"], "Rock_M_02", new Vector3(4f, 0f, 20f), 12f, 0.95f);
		Place(parent, prefabs["RockLarge"], "Rock_L_01", new Vector3(27f, 0f, 23f), -18f, 1f);
		Place(parent, prefabs["RockLarge"], "Rock_L_02", new Vector3(-31f, 0f, 30f), 36f, 0.92f);
	}

	private static void BuildTreeGroups(Transform parent, IReadOnlyDictionary<string, GameObject> prefabs)
	{
		(string, Vector3, float, float)[] trees = new(string, Vector3, float, float)[7]
		{
			("TreeA", new Vector3(-22f, 0f, -15f), 8f, 1f),
			("TreeB", new Vector3(-27f, 0f, -2f), 47f, 0.94f),
			("TreeC", new Vector3(-13f, 0f, 16f), -18f, 1.05f),
			("TreeA", new Vector3(25f, 0f, 7f), 71f, 0.92f),
			("TreeB", new Vector3(29f, 0f, 29f), -39f, 0.9f),
			("TreeC", new Vector3(5f, 0f, 36f), 26f, 1f),
			("TreeA", new Vector3(-20f, 0f, 38f), -13f, 0.9f)
		};
		for (int i = 0; i < trees.Length; i++)
		{
			var (key, position, rotation, scale) = trees[i];
			Place(parent, prefabs[key], $"Tree_{i:00}_{key}", position, rotation, scale);
		}
	}

	private static void BuildVegetation(Transform parent, IReadOnlyDictionary<string, GameObject> prefabs)
	{
		(string, Vector3, int)[] obj = new(string, Vector3, int)[6]
		{
			("PlantA", new Vector3(-12f, 0f, -2f), 5),
			("PlantB", new Vector3(-23f, 0f, 15f), 4),
			("PlantC", new Vector3(18f, 0f, 17f), 5),
			("GroundCoverA", new Vector3(7f, 0f, 3f), 6),
			("GroundCoverB", new Vector3(-2f, 0f, 29f), 6),
			("Accent", new Vector3(-23f, 0f, 31f), 4)
		};
		System.Random random = new System.Random(9821);
		(string, Vector3, int)[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			(string, Vector3, int) tuple = array[i];
			string key = tuple.Item1;
			Vector3 center = tuple.Item2;
			int count = tuple.Item3;
			for (int j = 0; j < count; j++)
			{
				float angle = (float)random.NextDouble() * (float)Math.PI * 2f;
				float radius = 0.6f + (float)random.NextDouble() * 2.2f;
				Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
				Place(parent, prefabs[key], $"{key}_{center.x:0}_{center.z:0}_{j:00}", position, (float)random.NextDouble() * 360f, 0.88f + (float)random.NextDouble() * 0.22f);
			}
		}
	}

	private static void BuildRuin(Transform parent, IReadOnlyDictionary<string, GameObject> prefabs, IReadOnlyDictionary<string, Material> materials)
	{
		Transform transform = new GameObject("RuinFocus").transform;
		transform.SetParent(parent, worldPositionStays: false);
		Place(transform, prefabs["RuinWallA"], "WallRemnant_West", new Vector3(-29f, 0f, 32f), 12f, 1f);
		Place(transform, prefabs["RuinWallB"], "WallRemnant_North", new Vector3(-24f, 0f, 37f), 92f, 1f);
		Place(transform, prefabs["Monument"], "DamagedRuneMonument", new Vector3(-25f, 0f, 33f), -8f, 1f);
		Place(transform, prefabs["Rune"], "EidrenRune", new Vector3(-23.7f, 0.8f, 32.6f), 0f, 0.9f);
		AddPrimitive(transform, "RuinFloor", PrimitiveType.Cube, new Vector3(-25f, 0.035f, 34f), new Vector3(8f, 0.07f, 7f), materials["Ruin"]);
	}

	private static void BuildWorldItem(Transform parent)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Items/WorldItem.prefab");
		UnityEngine.Object item = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Game/Data/Items/HealingPotion.asset");
		if (!(prefab == null) && !(item == null))
		{
			GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			obj.name = "StyleProof_WorldItem";
			obj.transform.SetParent(parent, worldPositionStays: false);
			obj.transform.position = new Vector3(-21.5f, 0.1f, 32f);
			SerializedObject serializedObject = new SerializedObject(obj.GetComponent<WorldItemController>());
			serializedObject.FindProperty("item").objectReferenceValue = item;
			serializedObject.FindProperty("quantity").intValue = 1;
			serializedObject.FindProperty("instanceId").stringValue = "zone_greenwood.styleproof.healing_potion";
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			Transform icon = obj.transform.Find("ItemIcon");
			if (icon != null)
			{
				icon.localScale = Vector3.one * 0.28f;
				icon.localPosition = new Vector3(0f, 0.62f, 0f);
			}
		}
	}

	private static void BuildLighting(Transform parent, VolumeProfile profile)
	{
		Transform systems = parent.parent.Find("Systems");
		Light sun = ((systems != null) ? systems.GetComponentInChildren<Light>(includeInactive: true) : null);
		if (sun != null)
		{
			sun.color = new Color(1f, 0.88f, 0.7f);
			sun.intensity = 1.08f;
			sun.shadows = LightShadows.Hard;
			sun.shadowStrength = 0.72f;
			sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
		}
		GameObject gameObject = new GameObject("Greenwood_StyleProof_GlobalVolume");
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		Volume volume = gameObject.AddComponent<Volume>();
		volume.isGlobal = true;
		volume.priority = 10f;
		volume.sharedProfile = profile;
		gameObject.AddComponent<StyleProofSceneVisualController>();
		RenderSettings.ambientMode = AmbientMode.Trilight;
		RenderSettings.ambientSkyColor = new Color(0.26f, 0.34f, 0.32f);
		RenderSettings.ambientEquatorColor = new Color(0.18f, 0.25f, 0.22f);
		RenderSettings.ambientGroundColor = new Color(0.1f, 0.13f, 0.11f);
		RenderSettings.ambientIntensity = 0.82f;
		RenderSettings.fog = true;
		RenderSettings.fogMode = FogMode.Linear;
		RenderSettings.fogColor = new Color(0.16f, 0.22f, 0.2f);
		RenderSettings.fogStartDistance = 28f;
		RenderSettings.fogEndDistance = 78f;
	}

	private static GameObject Place(Transform parent, GameObject prefab, string name, Vector3 position, float yaw, float uniformScale)
	{
		GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
		obj.name = name;
		obj.transform.SetParent(parent, worldPositionStays: false);
		obj.transform.position = position;
		obj.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
		obj.transform.localScale = Vector3.one * Mathf.Clamp(uniformScale, 0.75f, 1.25f);
		return obj;
	}

	private static GameObject PlaceVisualOnly(Transform parent, GameObject prefab, string name, Vector3 position, float yaw, float uniformScale)
	{
		GameObject instance = Place(parent, prefab, name, position, yaw, uniformScale);
		Collider[] componentsInChildren = instance.GetComponentsInChildren<Collider>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.DestroyImmediate(componentsInChildren[i]);
		}
		return instance;
	}

	private static GameObject AddPrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
	{
		GameObject gameObject = GameObject.CreatePrimitive(type);
		gameObject.name = name;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.position = position;
		gameObject.transform.localScale = scale;
		gameObject.GetComponent<Renderer>().sharedMaterial = material;
		Collider collider = gameObject.GetComponent<Collider>();
		if (collider != null)
		{
			UnityEngine.Object.DestroyImmediate(collider);
		}
		return gameObject;
	}

	private static GameObject AddMesh(Transform parent, string name, Mesh mesh, Vector3 localPosition, Vector3 localScale, Material material)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.localPosition = localPosition;
		gameObject.transform.localScale = localScale;
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
		return gameObject;
	}

	private static Transform Find(Scene scene, string rootName)
	{
		GameObject[] rootGameObjects = scene.GetRootGameObjects();
		foreach (GameObject root in rootGameObjects)
		{
			if (root.name == rootName)
			{
				return root.transform;
			}
		}
		return null;
	}

	private static void EnsureFolder(string parent, string name)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + name))
		{
			AssetDatabase.CreateFolder(parent, name);
		}
	}
}
}
