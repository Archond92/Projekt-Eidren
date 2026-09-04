using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

namespace Eidren.Editor
{
public static class AreaArtAssetBuilder
{
	internal sealed class AreaSpec
	{
		public string Key;

		public string Id;

		public string BaseTexture;

		public string[] BlendTextures;

		public Color Background;

		public Color Light;

		public string[] VariantResourceIds;

		public string[] VariantSources;

		public string[] Decorations;
	}

	public const string DataRoot = "Assets/_Game/Data/AreaArt";

	public const string MaterialRoot = "Assets/_Game/Art/Zones/Materials";

	public const string VariantRoot = "Assets/_Game/Prefabs/Environment/AreaArtVariants";

	public const string ProfileRoot = "Assets/_Game/Settings/AreaArt";

	private const string ResourceVisualRoot = "Assets/_Game/Prefabs/Resources/Visuals";

	private const string DecoRoot = "Assets/_Game/Prefabs/Environment/StyleProof";

	[MenuItem("Eidren/Art/Areas/Build Area Art Assets")]
	public static void BuildAll()
	{
		EnsureFolders();
		AreaArtVariantPrefabBuilder.ResetBranchMeshSequence();
		List<AreaSpec> specs = new List<AreaSpec>(Specs());
		foreach (AreaSpec item in specs)
		{
			Build(item);
		}
		AssignZoneDefinitions(specs);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		VerifyZoneDefinitions(specs);
		Debug.Log("Area-art assets built.");
	}

	// Schmaler Einstieg fuer Etappe 3, Task 4: klont ausschliesslich die sechs
	// T2-Zonenvarianten (x Active/Exhausted) aus den neuen Basis-Visuals neu.
	// Nutzt NUR CloneVariants (siehe unten) -- anders als Build(spec) schreibt
	// dieser Einstieg bewusst weder AreaArt_*.asset noch Zone_*.asset neu, denn
	// die Variant-Prefab-GUIDs bleiben stabil (CloneVariant ueberschreibt immer
	// denselben Pfad), sodass vorhandene Referenzen in AreaArt_*.asset gueltig
	// bleiben, ohne neu verdrahtet zu werden. Tier-1-Varianten, HomeBase und alle
	// Zonen-/AreaArt-Daten bleiben unangetastet -- Tier-1 ist tabu (siehe
	// STILUMBAU_E3_BAKEKETTE.md, Abschnitt b). Keine Szene wird beruehrt.
	private static readonly string[] TierTwoKeys = { "TwilightGrove", "VeilMarsh", "GreyRifts" };

	[MenuItem("Eidren/V0.2/Stilumbau/T2-Zonenvarianten klonen")]
	public static void BuildTierTwo()
	{
		EnsureFolders();
		List<AreaSpec> tierTwo = new List<AreaSpec>(Specs()).FindAll((AreaSpec s) => Array.IndexOf(TierTwoKeys, s.Key) >= 0);
		foreach (AreaSpec item in tierTwo)
		{
			CloneVariants(item, out _, out _);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Tier-two zone variants cloned.");
	}

	// Schmaler Einstieg fuer Etappe 3b, Task 4: Spiegelbild von BuildTierTwo() fuer die vier
	// T1-Specs (STILUMBAU_E3B_BAKEKETTE.md, Abschnitt b). HomeBase ist bewusst NICHT dabei --
	// ihre VariantResourceIds sind leer (siehe Abschnitt a), es gaebe nichts zu klonen. Nutzt
	// wie BuildTierTwo nur CloneVariants, schreibt also weder AreaArt_*.asset noch Zone_*.asset
	// neu (Variant-Prefab-GUIDs bleiben stabil).
	private static readonly string[] TierOneKeys = { "Greenwood", "Marsh", "Quarry", "EmberRuins" };

	[MenuItem("Eidren/V0.2/Stilumbau/T1-Zonenvarianten klonen")]
	public static void BuildTierOne()
	{
		EnsureFolders();
		AreaArtVariantPrefabBuilder.ResetBranchMeshSequence();
		List<AreaSpec> tierOne = new List<AreaSpec>(Specs()).FindAll((AreaSpec s) => Array.IndexOf(TierOneKeys, s.Key) >= 0);
		foreach (AreaSpec item in tierOne)
		{
			CloneVariants(item, out _, out _);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Tier-one zone variants cloned.");
	}

	private static void AssignZoneDefinitions(IReadOnlyList<AreaSpec> specs)
	{
		foreach (AreaSpec spec in specs)
		{
			string areaPath = "Assets/_Game/Data/AreaArt/AreaArt_" + spec.Key + ".asset";
			string zonePath = "Assets/_Game/Data/Zones/Zone_" + spec.Key + ".asset";
			ZoneAreaArtDefinition area = AssetDatabase.LoadAssetAtPath<ZoneAreaArtDefinition>(areaPath);
			ZoneDefinition zone = AssetDatabase.LoadAssetAtPath<ZoneDefinition>(zonePath);
			if (area == null || zone == null)
			{
				throw new InvalidOperationException("Missing zone binding assets: " + zonePath + ", " + areaPath);
			}
			SerializedObject serializedObject = new SerializedObject(zone);
			serializedObject.FindProperty("areaArt").objectReferenceValue = area;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(zone);
		}
	}

	private static void VerifyZoneDefinitions(IReadOnlyList<AreaSpec> specs)
	{
		foreach (AreaSpec spec in specs)
		{
			string zonePath = "Assets/_Game/Data/Zones/Zone_" + spec.Key + ".asset";
			AssetDatabase.ImportAsset(zonePath, ImportAssetOptions.ForceUpdate);
			ZoneDefinition zone = AssetDatabase.LoadAssetAtPath<ZoneDefinition>(zonePath);
			if (zone == null || zone.AreaArt == null || zone.AreaArt.Id != spec.Id)
			{
				throw new InvalidOperationException("Area-art binding did not persist: " + zonePath);
			}
		}
	}

	// Ausgelagert aus Build(spec) fuer Task 4 (Etappe 3): der reine Klon-Teil, den
	// sowohl der volle Build(spec) als auch der schmale T2-only-Einstieg
	// BuildTierTwo() braucht -- ohne die AreaArt-/Zonen-Datenschreibungen, die nur
	// Build(spec) noch anschliesst.
	private static void CloneVariants(AreaSpec spec, out List<GameObject> activeVariants, out List<GameObject> exhaustedVariants)
	{
		activeVariants = new List<GameObject>();
		exhaustedVariants = new List<GameObject>();
		for (int i = 0; i < spec.VariantResourceIds.Length; i++)
		{
			string resourceId = spec.VariantResourceIds[i];
			string sourceStem = spec.VariantSources[i];
			if (resourceId == "resource.copper_vein")
			{
				activeVariants.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/" + sourceStem + "_Active.prefab"));
				exhaustedVariants.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/" + sourceStem + "_Exhausted.prefab"));
			}
			else
			{
				activeVariants.Add(AreaArtVariantPrefabBuilder.CloneVariant(spec, resourceId, "Assets/_Game/Prefabs/Resources/Visuals/" + sourceStem + "_Active.prefab", exhausted: false));
				exhaustedVariants.Add(AreaArtVariantPrefabBuilder.CloneVariant(spec, resourceId, "Assets/_Game/Prefabs/Resources/Visuals/" + sourceStem + "_Exhausted.prefab", exhausted: true));
			}
		}
	}

	private static void Build(AreaSpec spec)
	{
		Texture2D baseTexture = LoadTexture(spec.BaseTexture);
		Texture2D[] blendTextures = Array.ConvertAll(spec.BlendTextures, LoadTexture);
		VolumeProfile profile = EnsureProfile(spec.Key);
		CloneVariants(spec, out List<GameObject> activeVariants, out List<GameObject> exhaustedVariants);
		string path = "Assets/_Game/Data/AreaArt/AreaArt_" + spec.Key + ".asset";
		ZoneAreaArtDefinition asset = AssetDatabase.LoadAssetAtPath<ZoneAreaArtDefinition>(path);
		if (asset == null)
		{
			asset = ScriptableObject.CreateInstance<ZoneAreaArtDefinition>();
			AssetDatabase.CreateAsset(asset, path);
		}
		SerializedObject serializedObject = new SerializedObject(asset);
		serializedObject.FindProperty("id").stringValue = spec.Id;
		serializedObject.FindProperty("baseGround").objectReferenceValue = baseTexture;
		serializedObject.FindProperty("volumeProfile").objectReferenceValue = profile;
		serializedObject.FindProperty("backgroundColor").colorValue = spec.Background;
		serializedObject.FindProperty("lightColor").colorValue = spec.Light;
		WriteGroundBlends(serializedObject.FindProperty("groundBlends"), spec, blendTextures);
		WriteVariants(serializedObject.FindProperty("resourceVariants"), spec, activeVariants, exhaustedVariants);
		WriteDecorations(serializedObject.FindProperty("decorations"), spec);
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(asset);
	}

	private static void WriteGroundBlends(SerializedProperty array, AreaSpec spec, IReadOnlyList<Texture2D> textures)
	{
		array.arraySize = textures.Count;
		for (int i = 0; i < textures.Count; i++)
		{
			SerializedProperty arrayElementAtIndex = array.GetArrayElementAtIndex(i);
			arrayElementAtIndex.FindPropertyRelative("id").stringValue = $"ground_{spec.Key.ToLowerInvariant()}_blend_{i + 1}";
			arrayElementAtIndex.FindPropertyRelative("texture").objectReferenceValue = textures[i];
			arrayElementAtIndex.FindPropertyRelative("coverage").floatValue = ((i == 0) ? 0.34f : 0.24f);
		}
	}

	private static void WriteVariants(SerializedProperty array, AreaSpec spec, IReadOnlyList<GameObject> active, IReadOnlyList<GameObject> exhausted)
	{
		array.arraySize = spec.VariantResourceIds.Length;
		for (int i = 0; i < spec.VariantResourceIds.Length; i++)
		{
			SerializedProperty arrayElementAtIndex = array.GetArrayElementAtIndex(i);
			string resourceId = spec.VariantResourceIds[i];
			arrayElementAtIndex.FindPropertyRelative("resourceNodeId").stringValue = resourceId;
			arrayElementAtIndex.FindPropertyRelative("activeVisualPrefab").objectReferenceValue = active[i];
			arrayElementAtIndex.FindPropertyRelative("exhaustedVisualPrefab").objectReferenceValue = exhausted[i];
			arrayElementAtIndex.FindPropertyRelative("medium").enumValueIndex = 1;
			arrayElementAtIndex.FindPropertyRelative("recognitionMarker").stringValue = ((resourceId == "resource.tree") ? "Axt mit ockerfarbenem Tuch am Stamm" : ((resourceId == "resource.copper_vein") ? "Dunkle Erzkrone mit blanken Kupferflaechen" : "Gebietseigene Form auf der Interaktionssilhouette"));
		}
	}

	private static void WriteDecorations(SerializedProperty array, AreaSpec spec)
	{
		array.arraySize = spec.Decorations.Length;
		for (int i = 0; i < spec.Decorations.Length; i++)
		{
			SerializedProperty arrayElementAtIndex = array.GetArrayElementAtIndex(i);
			string stem = spec.Decorations[i];
			arrayElementAtIndex.FindPropertyRelative("id").stringValue = $"decoration_{spec.Key.ToLowerInvariant()}_{i + 1}";
			arrayElementAtIndex.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Environment/StyleProof/" + stem + ".prefab");
			arrayElementAtIndex.FindPropertyRelative("density").floatValue = ScatterDensity(stem);
		}
	}

	// G-002: Density war bisher aus dem Listenplatz abgeleitet (0,18 fuer die
	// ersten drei, sonst 0,1) und wurde von niemandem gelesen. Jetzt steuert der
	// Wert die Streuung in AreaArtGroundCoverBuilder und richtet sich nach dem,
	// was die Requisite tatsaechlich ist.
	//
	// Einheit: Instanzen je 100 Quadrateinheiten. Null bedeutet, dass die
	// Requisite nur auf ihren gesetzten Positionen steht und nicht gestreut
	// wird. Das gilt fuer alles, was der Spieler umgehen oder als Landmarke
	// lesen soll: Baeume, Ruinen, grosse und mittlere Felsen, Runen.
	private static float ScatterDensity(string stem)
	{
		switch (stem)
		{
		case "SP_GroundCover_Grass":
		case "SP_GroundCover_Moss":
			return 1.6f;
		case "SP_Plant_Flowers":
			return 0.7f;
		case "SP_Plant_Fern":
			return 0.6f;
		case "SP_Rock_Small":
			return 0.5f;
		case "SP_Plant_Bush":
			return 0.4f;
		case "SP_Accent_GlowMushrooms":
			return 0.3f;
		default:
			return 0f;
		}
	}

	private static VolumeProfile EnsureProfile(string key)
	{
		string path = "Assets/_Game/Settings/AreaArt/" + key + "_AreaArt_Volume.asset";
		VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
		if (profile != null)
		{
			return profile;
		}
		profile = ScriptableObject.CreateInstance<VolumeProfile>();
		AssetDatabase.CreateAsset(profile, path);
		return profile;
	}

	private static Texture2D LoadTexture(string path)
	{
		if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
		{
			throw new InvalidOperationException("Required area-art texture is missing: " + path);
		}
		TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
		if (importer != null)
		{
			importer.textureType = TextureImporterType.Default;
			importer.wrapMode = TextureWrapMode.Repeat;
			importer.mipmapEnabled = true;
			importer.alphaIsTransparency = false;
			importer.SaveAndReimport();
		}
		return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
	}

	private static void EnsureFolders()
	{
		EnsureFolder("Assets/_Game/Data", "AreaArt");
		EnsureFolder("Assets/_Game/Art/Zones", "Materials");
		// Etappe 3b, Task 4: Ablage der Ast-Meshes der Baum-Gebietsidentitaet, siehe
		// AreaArtVariantPrefabBuilder.PersistAst.
		EnsureFolder("Assets/_Game/Art/Zones", "Meshes");
		EnsureFolder("Assets/_Game/Prefabs/Environment", "AreaArtVariants");
		EnsureFolder("Assets/_Game/Settings", "AreaArt");
	}

	private static void EnsureFolder(string parent, string name)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + name))
		{
			AssetDatabase.CreateFolder(parent, name);
		}
	}

	private static IEnumerable<AreaSpec> Specs()
	{
		yield return new AreaSpec
		{
			Key = "Greenwood",
			Id = "area_art_greenwood",
			BaseTexture = "Assets/_Game/Art/Zones/Greenwood/greenwood_meadow_v01.png",
			BlendTextures = new string[2] { "Assets/_Game/Art/Zones/Greenwood/greenwood_soil_v01.png", "Assets/_Game/Art/StyleProof/Textures/greenwood_ground_v01.png" },
			Background = new Color(0.1f, 0.16f, 0.12f),
			Light = new Color(0.86f, 0.93f, 0.78f),
			VariantResourceIds = new string[1] { "resource.tree" },
			VariantSources = new string[1] { "Tree" },
			Decorations = new string[8] { "SP_Rock_Large", "SP_Rock_Medium", "SP_Rock_Small", "SP_Plant_Bush", "SP_Plant_Fern", "SP_Plant_Flowers", "SP_GroundCover_Moss", "SP_EidrenRune" }
		};
		yield return new AreaSpec
		{
			Key = "Marsh",
			Id = "area_art_marsh",
			BaseTexture = "Assets/_Game/Art/Zones/Marsh/marsh_peat_moss_v01.png",
			BlendTextures = new string[2] { "Assets/_Game/Art/Zones/Marsh/marsh_mud_v01.png", "Assets/_Game/Art/Zones/Marsh/marsh_reed_grass_v01.png" },
			Background = new Color(0.12f, 0.16f, 0.17f),
			Light = new Color(0.65f, 0.78f, 0.75f),
			VariantResourceIds = new string[2] { "resource.tree", "resource.fiber_plant" },
			VariantSources = new string[2] { "Tree", "FiberPlant" },
			Decorations = new string[8] { "SP_Tree_C", "SP_Rock_Small", "SP_Rock_Medium", "SP_Plant_Fern", "SP_Plant_Bush", "SP_Plant_Flowers", "SP_GroundCover_Moss", "SP_Accent_GlowMushrooms" }
		};
		yield return new AreaSpec
		{
			Key = "Quarry",
			Id = "area_art_quarry",
			BaseTexture = "Assets/_Game/Art/Zones/Quarry/quarry_gravel_v01.png",
			BlendTextures = new string[2] { "Assets/_Game/Art/Zones/Quarry/quarry_rock_v01.png", "Assets/_Game/Art/Zones/Quarry/quarry_clay_v01.png" },
			Background = new Color(0.2f, 0.19f, 0.17f),
			Light = new Color(0.92f, 0.82f, 0.65f),
			VariantResourceIds = new string[2] { "resource.tree", "resource.stone_deposit" },
			VariantSources = new string[2] { "Tree", "StoneDeposit" },
			Decorations = new string[8] { "SP_Rock_Large", "SP_Rock_Medium", "SP_Rock_Small", "SP_RuinWall_A", "SP_RuinWall_B", "SP_Plant_Bush", "SP_GroundCover_Grass", "SP_EidrenRune" }
		};
		yield return new AreaSpec
		{
			Key = "EmberRuins",
			Id = "area_art_ember_ruins",
			BaseTexture = "Assets/_Game/Art/Zones/EmberRuins/ember_ash_v01.png",
			BlendTextures = new string[2] { "Assets/_Game/Art/Zones/EmberRuins/ember_ruin_tiles_v01.png", "Assets/_Game/Art/Zones/EmberRuins/ember_slag_v01.png" },
			Background = new Color(0.16f, 0.08f, 0.06f),
			Light = new Color(1f, 0.52f, 0.28f),
			VariantResourceIds = new string[2] { "resource.tree", "resource.copper_vein" },
			VariantSources = new string[2] { "Tree", "CopperVein" },
			Decorations = new string[8] { "SP_RuinMonument", "SP_RuinWall_A", "SP_RuinWall_B", "SP_Rock_Large", "SP_Rock_Medium", "SP_Plant_Bush", "SP_GroundCover_Grass", "SP_EidrenRune" }
		};
		yield return new AreaSpec
		{
			Key = "TwilightGrove",
			Id = "area_art_twilight_grove",
			// G-002: lag zuvor auf greenwood_soil und war damit von Greenwood
			// nicht zu unterscheiden (Abnahmekriterium 7). Eigene Basistextur.
			BaseTexture = "Assets/_Game/Art/Zones/TwilightGrove/twilight_leaf_litter_v01.png",
			BlendTextures = new string[2] { "Assets/_Game/Art/Zones/Greenwood/greenwood_meadow_v01.png", "Assets/_Game/Art/Zones/Greenwood/greenwood_soil_v01.png" },
			Background = new Color(0.035f, 0.075f, 0.055f),
			Light = new Color(0.5f, 0.68f, 0.52f),
			VariantResourceIds = new string[2] { "resource.hardwood_tree", "resource.swamp_hemp" },
			VariantSources = new string[2] { "HardwoodTree", "SwampHemp" },
			Decorations = new string[8] { "SP_Tree_A", "SP_Tree_B", "SP_Tree_C", "SP_Plant_Fern", "SP_Plant_Bush", "SP_GroundCover_Moss", "SP_RuinMonument", "SP_Accent_GlowMushrooms" }
		};
		yield return new AreaSpec
		{
			Key = "VeilMarsh",
			Id = "area_art_veil_marsh",
			// G-002: lag zuvor auf marsh_mud, siehe TwilightGrove.
			BaseTexture = "Assets/_Game/Art/Zones/VeilMarsh/veil_wet_silt_v01.png",
			BlendTextures = new string[2] { "Assets/_Game/Art/Zones/Marsh/marsh_peat_moss_v01.png", "Assets/_Game/Art/Zones/Marsh/marsh_reed_grass_v01.png" },
			Background = new Color(0.055f, 0.105f, 0.115f),
			Light = new Color(0.52f, 0.68f, 0.65f),
			VariantResourceIds = new string[2] { "resource.swamp_hemp", "resource.iron_vein" },
			VariantSources = new string[2] { "SwampHemp", "IronVein" },
			Decorations = new string[8] { "SP_Tree_C", "SP_Plant_Fern", "SP_GroundCover_Moss", "SP_Accent_GlowMushrooms", "SP_Rock_Small", "SP_RuinWall_A", "SP_Plant_Bush", "SP_EidrenRune" }
		};
		yield return new AreaSpec
		{
			Key = "GreyRifts",
			Id = "area_art_grey_rifts",
			// G-002: lag zuvor auf quarry_rock, siehe TwilightGrove.
			BaseTexture = "Assets/_Game/Art/Zones/GreyRifts/rift_cold_scree_v01.png",
			BlendTextures = new string[2] { "Assets/_Game/Art/Zones/Quarry/quarry_gravel_v01.png", "Assets/_Game/Art/Zones/Quarry/quarry_clay_v01.png" },
			Background = new Color(0.115f, 0.12f, 0.135f),
			Light = new Color(0.72f, 0.7f, 0.68f),
			VariantResourceIds = new string[2] { "resource.granite_deposit", "resource.iron_vein" },
			VariantSources = new string[2] { "GraniteDeposit", "IronVein" },
			Decorations = new string[8] { "SP_Rock_Large", "SP_Rock_Medium", "SP_Rock_Small", "SP_RuinWall_A", "SP_RuinWall_B", "SP_RuinMonument", "SP_GroundCover_Grass", "SP_EidrenRune" }
		};
		yield return new AreaSpec
		{
			Key = "HomeBase",
			Id = "area_art_home_base",
			BaseTexture = "Assets/_Game/Art/Zones/HomeBase/home_trampled_clay_v01.png",
			// G-002: HomeBase hatte als einziges Gebiet nur zwei Bodenmaterialien
			// (Basis plus eine Blendlage). Abnahmekriterium 3 verlangt mindestens
			// drei mischbare. greenwood_soil passt zum getretenen Lehm der Basis
			// und liegt bereits gemalt vor - eine eigene Textur waere hier
			// Aufwand ohne sichtbaren Gewinn.
			BlendTextures = new string[2] { "Assets/_Game/Art/Zones/HomeBase/home_grass_v01.png", "Assets/_Game/Art/Zones/Greenwood/greenwood_soil_v01.png" },
			Background = new Color(0.18f, 0.21f, 0.16f),
			Light = new Color(0.95f, 0.88f, 0.7f),
			VariantResourceIds = Array.Empty<string>(),
			VariantSources = Array.Empty<string>(),
			Decorations = new string[4] { "SP_Rock_Small", "SP_Plant_Bush", "SP_GroundCover_Grass", "SP_EidrenRune" }
		};
	}
}
}
