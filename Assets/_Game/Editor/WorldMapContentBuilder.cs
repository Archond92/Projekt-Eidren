using Eidren.Data;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class WorldMapContentBuilder
{
	private enum SupplementalSprite
	{
		New,
		Visited,
		Notification,
		NodeFrame,
		PanelFrame,
		EnergySigil
	}

	internal const string ArtRoot = "Assets/_Game/Art/WorldMap";

	internal const string BackgroundFolder = "Assets/_Game/Art/WorldMap/Backgrounds";

	internal const string NodeArtFolder = "Assets/_Game/Art/WorldMap/Nodes";

	internal const string ResourceArtFolder = "Assets/_Game/Art/WorldMap/Resources";

	internal const string MarkerFolder = "Assets/_Game/Art/WorldMap/Markers";

	internal const string FrameFolder = "Assets/_Game/Art/WorldMap/Frames";

	internal const string EffectFolder = "Assets/_Game/Art/WorldMap/Effects";

	internal const string MapVisualPath = "Assets/_Game/Art/WorldMap/Backgrounds/WM_TMP_RegionalMap_EidrenV02.png";

	internal const string DataRoot = "Assets/_Game/Data/WorldMap";

	internal const string NodeFolder = "Assets/_Game/Data/WorldMap/Nodes";

	internal const string MapFolder = "Assets/_Game/Data/WorldMap/Maps";

	internal const string ThemeFolder = "Assets/_Game/Data/WorldMap/Themes";

	internal const string MapAssetPath = "Assets/_Game/Data/WorldMap/Maps/WorldMap_RegionalV01.asset";

	internal const string ThemeAssetPath = "Assets/_Game/Data/WorldMap/Themes/WM_Theme_EidrenV01.asset";

	[MenuItem("Eidren/World Map/Build Production Prefabs")]
	[MenuItem("Eidren/World Map/Build Regional V0.1")]
	public static void BuildRegionalWorldMap()
	{
		EnsureFolders();
		CreateTierTwoSprites();
		RequireVisuals();
		ConfigureSpriteImports();
		CreateSupplementalSprites();
		AssetDatabase.Refresh();
		WorldMapThemeData theme = BuildTheme();
		WorldMapNodeDefinition[] nodes = CreateNodeAssets();
		WorldMapDefinition map = LoadOrCreate<WorldMapDefinition>("Assets/_Game/Data/WorldMap/Maps/WorldMap_RegionalV01.asset");
		ConfigureMap(map, theme, nodes);
		WorldMapPrefabBuilder.Build(map, nodes);
		EidrenSceneStructureBuilder.SetBuildSceneOrder();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: WorldMap art direction, theme, modular prefabs and responsive scene were built.");
	}

	internal static Sprite NodeSprite(string name)
	{
		return LoadSprite(name.StartsWith("T2_", StringComparison.Ordinal) ? ("Assets/_Game/Art/WorldMap/Nodes/WM_Node_" + name.Substring(3) + ".png") : ("Assets/_Game/Art/WorldMap/Nodes/WM_TMP_Node_" + name + ".png"));
	}

	internal static Sprite MarkerSprite(string name)
	{
		return LoadSprite("Assets/_Game/Art/WorldMap/Markers/WM_TMP_Marker_" + name + ".png");
	}

	internal static Sprite FrameSprite(string name)
	{
		return LoadSprite("Assets/_Game/Art/WorldMap/Frames/WM_TMP_Frame_" + name + ".png");
	}

	internal static Sprite EffectSprite(string name)
	{
		return LoadSprite("Assets/_Game/Art/WorldMap/Effects/WM_TMP_Effect_" + name + ".png");
	}

	private static WorldMapNodeDefinition[] CreateNodeAssets()
	{
		WorldMapNodeDefinition home = Node("Node_HomeBase", "home_base", "Heimatbasis", "Ein geschützter Höhenring, in dem Eidren und Reisende zwischen ihren Expeditionen zur Ruhe kommen.", "HomeBase", new Vector2(0.5f, 0.5f), "HomeBase", WorldRegionType.SafeHomeland, 1, "Ausgangspunkt", hasBoss: false, Array.Empty<WorldMapResourcePreview>(), new string[1] { "Keine feindlichen Gruppen" }, new string[2] { "Resonanzhof", "Westliche Aussicht" }, new string[7] { "zone_greenwood", "zone_quarry", "zone_marsh", "zone_ember_ruins", "zone_twilight_grove", "zone_veil_marsh", "zone_grey_rifts" });
		WorldMapNodeDefinition greenwood = Node("Node_Greenwood", "zone_greenwood", "Grünwald", "Ein alter Wald aus moosbedeckten Terrassen, Wurzelpfaden und stillen Ruinen.", "Zone_Greenwood", new Vector2(0.5f, 0.79f), "Greenwood", WorldRegionType.Forest, 2, "Fortschritt 1–2", hasBoss: false, new WorldMapResourcePreview[3]
		{
			Resource("wood", "Holz", "Wood"),
			Resource("fiber", "Pflanzenfaser", "Fiber"),
			Resource("stone", "Wenig Stein", "Stone")
		}, new string[2] { "Wildlinge", "Schattenkriecher · später" }, new string[2] { "Wurzelpfad", "Verwitterte Waldruinen" }, new string[1] { "home_base" });
		WorldMapNodeDefinition quarry = Node("Node_Quarry", "zone_quarry", "Steinbruch", "Helle Felsterrassen und tiefe Schnitte legen ergiebige Erzadern frei.", "Zone_Quarry", new Vector2(0.78f, 0.5f), "Quarry", WorldRegionType.Quarry, 3, "Fortschritt 2–3", hasBoss: false, new WorldMapResourcePreview[2]
		{
			Resource("stone", "Stein", "Stone"),
			Resource("copper", "Kupfererz", "Copper")
		}, new string[1] { "Felspirscher · später" }, new string[2] { "Gebrochene Förderstufen", "Kristallrinne" }, new string[1] { "home_base" });
		WorldMapNodeDefinition marsh = Node("Node_Marsh", "zone_marsh", "Nebelmoor", "Kaltes Wasser, violette Torfinseln und wandernder Nebel verbergen seltene Gewächse.", "Zone_Marsh", new Vector2(0.5f, 0.21f), "Marsh", WorldRegionType.Marsh, 3, "Fortschritt 2–4", hasBoss: false, new WorldMapResourcePreview[2]
		{
			Resource("fiber", "Pflanzenfaser", "Fiber"),
			Resource("rare_plants", "Seltene Pflanzen", "Fiber", future: true)
		}, new string[1] { "Moorschleicher · später" }, new string[2] { "Nebelsenke", "Versunkene Stege" }, new string[1] { "home_base" });
		WorldMapNodeDefinition ember = Node("Node_EmberRuins", "zone_ember_ruins", "Glutruinen", "Schwarze Plateaus tragen noch die Wärme einer untergegangenen Schmiedekultur.", "Zone_EmberRuins", new Vector2(0.22f, 0.5f), "EmberRuins", WorldRegionType.EmberRuins, 5, "Fortschritt 4–5", hasBoss: true, new WorldMapResourcePreview[2]
		{
			Resource("copper", "Kupfererz", "Copper"),
			Resource("rare_materials", "Seltene Materialien", "Copper", future: true)
		}, new string[2] { "Glutwächter · später", "Garon" }, new string[2] { "Geborstener Ring", "Aschenpforte" }, new string[1] { "home_base" });
		WorldMapNodeDefinition twilight = Node("Node_TwilightGrove", "zone_twilight_grove", "Dämmerhain", "Ein uralter, dichter Wald aus mächtigen Hartholzbäumen, dunklem Blattwerk und überwachsenen Steinringen.", "Zone_TwilightGrove", new Vector2(0.3f, 0.76f), "T2_TwilightGrove", WorldRegionType.TwilightGrove, 4, "Tier 2", hasBoss: false, new WorldMapResourcePreview[2]
		{
			Resource("hardwood", "Hartholz", "T2_Hardwood"),
			Resource("swamp_hemp", "Sumpfhanf", "T2_SwampHemp")
		}, new string[2] { "Schattenkriecher", "Dornwolf" }, new string[2] { "Dämmerkrone", "Überwachsener Steinring" }, new string[1] { "home_base" }, initiallyAvailable: false);
		WorldMapNodeDefinition veil = Node("Node_VeilMarsh", "zone_veil_marsh", "Schleiermoor", "Nebel liegt über schwarzen Wasserflächen, hohem Sumpfhanf und den bleichen Stämmen eines ertrunkenen Waldes.", "Zone_VeilMarsh", new Vector2(0.7f, 0.24f), "T2_VeilMarsh", WorldRegionType.VeilMarsh, 4, "Tier 2", hasBoss: false, new WorldMapResourcePreview[2]
		{
			Resource("swamp_hemp", "Sumpfhanf", "T2_SwampHemp"),
			Resource("iron_ore", "Eisenerz", "T2_IronOre")
		}, new string[2] { "Moorklaue", "Nebelwyrm" }, new string[2] { "Versunkene Stege", "Toter Hain" }, new string[1] { "home_base" }, initiallyAvailable: false);
		WorldMapNodeDefinition rifts = Node("Node_GreyRifts", "zone_grey_rifts", "Grauklüfte", "Aufgebrochene Granitwände, tiefe Schluchten und rote Eisenadern formen eine raue, steinerne Wildnis.", "Zone_GreyRifts", new Vector2(0.76f, 0.73f), "T2_GreyRifts", WorldRegionType.GreyRifts, 5, "Tier 2", hasBoss: false, new WorldMapResourcePreview[2]
		{
			Resource("granite", "Granit", "T2_Granite"),
			Resource("iron_ore", "Eisenerz", "T2_IronOre")
		}, new string[2] { "Kluftläufer", "Eisenrücken" }, new string[2] { "Graue Narbe", "Eisenspalt" }, new string[1] { "home_base" }, initiallyAvailable: false);
		return new WorldMapNodeDefinition[8] { home, greenwood, quarry, marsh, ember, twilight, veil, rifts };
	}

	private static WorldMapNodeDefinition Node(string assetName, string id, string displayName, string description, string sceneKey, Vector2 position, string iconName, WorldRegionType regionType, int danger, string recommended, bool hasBoss, WorldMapResourcePreview[] resources, string[] enemies, string[] places, string[] neighbors, bool initiallyAvailable = true)
	{
		WorldMapNodeDefinition worldMapNodeDefinition = LoadOrCreate<WorldMapNodeDefinition>("Assets/_Game/Data/WorldMap/Nodes/" + assetName + ".asset");
		SerializedObject serializedObject = new SerializedObject(worldMapNodeDefinition);
		Set(serializedObject, "id", id);
		Set(serializedObject, "displayName", displayName);
		Set(serializedObject, "description", description);
		Set(serializedObject, "sceneKey", sceneKey);
		serializedObject.FindProperty("mapPosition").vector2Value = position;
		serializedObject.FindProperty("icon").objectReferenceValue = NodeSprite(iconName);
		serializedObject.FindProperty("regionType").enumValueIndex = (int)regionType;
		serializedObject.FindProperty("dangerLevel").intValue = danger;
		Set(serializedObject, "recommendedProgress", recommended);
		serializedObject.FindProperty("initiallyAvailable").boolValue = initiallyAvailable;
		serializedObject.FindProperty("hasBoss").boolValue = hasBoss;
		SetResources(serializedObject.FindProperty("resources"), resources);
		SetStrings(serializedObject.FindProperty("possibleEnemies"), enemies);
		SetStrings(serializedObject.FindProperty("specialPlaces"), places);
		SetStrings(serializedObject.FindProperty("directNeighborIds"), neighbors);
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(worldMapNodeDefinition);
		return worldMapNodeDefinition;
	}

	private static void ConfigureMap(WorldMapDefinition map, WorldMapThemeData theme, WorldMapNodeDefinition[] nodes)
	{
		SerializedObject serialized = new SerializedObject(map);
		Set(serialized, "id", "eidren_regional_v01");
		Set(serialized, "displayName", "Die Randlande");
		serialized.FindProperty("mapVisual").objectReferenceValue = LoadSprite("Assets/_Game/Art/WorldMap/Backgrounds/WM_TMP_RegionalMap_EidrenV02.png");
		serialized.FindProperty("fallbackNodeIcon").objectReferenceValue = NodeSprite("Fallback");
		serialized.FindProperty("theme").objectReferenceValue = theme;
		SerializedProperty nodeArray = serialized.FindProperty("nodes");
		nodeArray.arraySize = nodes.Length;
		for (int index = 0; index < nodes.Length; index++)
		{
			nodeArray.GetArrayElementAtIndex(index).objectReferenceValue = nodes[index];
		}
		serialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(map);
		map.ValidateOrThrow();
	}

	private static WorldMapThemeData BuildTheme()
	{
		WorldMapThemeData worldMapThemeData = LoadOrCreate<WorldMapThemeData>("Assets/_Game/Data/WorldMap/Themes/WM_Theme_EidrenV01.asset");
		SerializedObject serializedObject = new SerializedObject(worldMapThemeData);
		SetColor(serializedObject, "backgroundDeep", "#07191FFF");
		SetColor(serializedObject, "backgroundRaised", "#102A30F7");
		SetColor(serializedObject, "panelSurface", "#102B31F7");
		SetColor(serializedObject, "frameNeutral", "#9B8968FF");
		SetColor(serializedObject, "textPrimary", "#F2EEE2FF");
		SetColor(serializedObject, "textSecondary", "#AFC1BDFF");
		SetColor(serializedObject, "selection", "#D6A95FFF");
		SetColor(serializedObject, "available", "#719C82FF");
		SetColor(serializedObject, "dangerous", "#D47A4DFF");
		SetColor(serializedObject, "locked", "#536069FF");
		SetColor(serializedObject, "eventColor", "#66C9C6FF");
		SetColor(serializedObject, "boss", "#C95E4EFF");
		SetColor(serializedObject, "energyGlow", "#72DFD2FF");
		SetColor(serializedObject, "homelandAccent", "#C9A766FF");
		SetColor(serializedObject, "forestAccent", "#668E68FF");
		SetColor(serializedObject, "quarryAccent", "#8293A3FF");
		SetColor(serializedObject, "marshAccent", "#567F7FFF");
		SetColor(serializedObject, "emberAccent", "#A85C43FF");
		Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		SetTypography(serializedObject, "mapTitle", font, 32, FontStyle.Bold);
		SetTypography(serializedObject, "regionName", font, 20, FontStyle.Bold);
		SetTypography(serializedObject, "panelHeading", font, 27, FontStyle.Bold);
		SetTypography(serializedObject, "bodyText", font, 16, FontStyle.Normal);
		SetTypography(serializedObject, "smallMetaText", font, 13, FontStyle.Bold);
		SetTypography(serializedObject, "timerText", font, 17, FontStyle.Bold);
		SetTypography(serializedObject, "buttonText", font, 17, FontStyle.Bold);
		SetTypography(serializedObject, "dangerLabel", font, 13, FontStyle.Bold);
		serializedObject.FindProperty("selectionDuration").floatValue = 0.12f;
		serializedObject.FindProperty("panelDuration").floatValue = 0.24f;
		serializedObject.FindProperty("eventPulseDuration").floatValue = 1.8f;
		serializedObject.FindProperty("travelConfirmationDuration").floatValue = 0.18f;
		serializedObject.FindProperty("transitionFadeDuration").floatValue = 0.28f;
		serializedObject.FindProperty("selectedScale").floatValue = 1.04f;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(worldMapThemeData);
		return worldMapThemeData;
	}

	private static void ConfigureSpriteImports()
	{
		ImportAsSprite("Assets/_Game/Art/WorldMap/Backgrounds/WM_TMP_RegionalMap_EidrenV02.png", 2048, compressed: true);
		string[] files = Directory.GetFiles("Assets/_Game/Art/WorldMap/Nodes", "*.png");
		for (int i = 0; i < files.Length; i++)
		{
			ImportAsSprite(files[i].Replace('\\', '/'), 256, compressed: false);
		}
		files = Directory.GetFiles("Assets/_Game/Art/WorldMap/Resources", "*.png");
		for (int i = 0; i < files.Length; i++)
		{
			ImportAsSprite(files[i].Replace('\\', '/'), 256, compressed: false);
		}
		files = Directory.GetFiles("Assets/_Game/Art/WorldMap/Markers", "*.png");
		for (int i = 0; i < files.Length; i++)
		{
			ImportAsSprite(files[i].Replace('\\', '/'), 256, compressed: false);
		}
	}

	private static void CreateSupplementalSprites()
	{
		CreateSprite("Assets/_Game/Art/WorldMap/Markers/WM_TMP_Marker_New.png", SupplementalSprite.New);
		CreateSprite("Assets/_Game/Art/WorldMap/Markers/WM_TMP_Marker_Visited.png", SupplementalSprite.Visited);
		CreateSprite("Assets/_Game/Art/WorldMap/Markers/WM_TMP_Marker_Notification.png", SupplementalSprite.Notification);
		CreateSprite("Assets/_Game/Art/WorldMap/Frames/WM_TMP_Frame_Node.png", SupplementalSprite.NodeFrame, border: true);
		CreateSprite("Assets/_Game/Art/WorldMap/Frames/WM_TMP_Frame_Panel.png", SupplementalSprite.PanelFrame, border: true);
		CreateSprite("Assets/_Game/Art/WorldMap/Effects/WM_TMP_Effect_EnergySigil.png", SupplementalSprite.EnergySigil);
	}

	private static void CreateSprite(string path, SupplementalSprite kind, bool border = false)
	{
		Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, mipChain: false);
		Color[] pixels = new Color[16384];
		Color ink = new Color(0.84f, 0.72f, 0.48f, 1f);
		for (int y = 0; y < 128; y++)
		{
			for (int x = 0; x < 128; x++)
			{
				float nx = ((float)x + 0.5f) / 128f * 2f - 1f;
				float ny = ((float)y + 0.5f) / 128f * 2f - 1f;
				float radius = Mathf.Sqrt(nx * nx + ny * ny);
				bool filled = kind switch
				{
					SupplementalSprite.New => Mathf.Abs(nx * ny) < 0.04f && Mathf.Abs(nx) + Mathf.Abs(ny) < 0.92f, 
					SupplementalSprite.Visited => (Mathf.Abs(ny + 0.22f * nx + 0.08f) < 0.065f && nx < 0.08f) || (Mathf.Abs(ny - 0.7f * nx + 0.28f) < 0.065f && nx > -0.05f), 
					SupplementalSprite.Notification => radius < 0.34f || (radius > 0.58f && radius < 0.67f), 
					SupplementalSprite.NodeFrame => (radius > 0.78f && radius < 0.91f) || (Mathf.Abs(nx) < 0.035f && Mathf.Abs(ny) > 0.72f) || (Mathf.Abs(ny) < 0.035f && Mathf.Abs(nx) > 0.72f), 
					SupplementalSprite.PanelFrame => Mathf.Abs(nx) > 0.86f || Mathf.Abs(ny) > 0.86f || (Mathf.Abs(Mathf.Abs(nx) - Mathf.Abs(ny)) < 0.025f && radius > 0.92f), 
					_ => (radius > 0.43f && radius < 0.52f) || (Mathf.Abs(nx) < 0.045f && Mathf.Abs(ny) < 0.72f) || (Mathf.Abs(ny) < 0.045f && Mathf.Abs(nx) < 0.72f), 
				};
				float grain = (float)((x * 73 + y * 151 + x * y * 3) % 97) / 9700f;
				pixels[y * 128 + x] = (filled ? new Color(ink.r + grain, ink.g + grain * 0.7f, ink.b + grain * 0.4f, ink.a) : new Color(grain, grain * 0.7f, grain * 0.4f, 0f));
			}
		}
		texture.SetPixels(pixels);
		texture.Apply();
		File.WriteAllBytes(path, texture.EncodeToPNG());
		UnityEngine.Object.DestroyImmediate(texture);
		ImportAsSprite(path, 256, compressed: false, border);
	}

	private static void ImportAsSprite(string path, int maxSize, bool compressed, bool border = false)
	{
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
		TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
		if (!(importer == null))
		{
			importer.textureType = TextureImporterType.Sprite;
			importer.spriteImportMode = SpriteImportMode.Single;
			importer.alphaIsTransparency = true;
			importer.mipmapEnabled = false;
			importer.maxTextureSize = maxSize;
			importer.filterMode = FilterMode.Bilinear;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.textureCompression = (compressed ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed);
			if (border)
			{
				importer.spriteBorder = new Vector4(24f, 24f, 24f, 24f);
			}
			importer.SaveAndReimport();
		}
	}

	private static WorldMapResourcePreview Resource(string id, string displayName, string iconName, bool future = false)
	{
		return new WorldMapResourcePreview
		{
			StableId = id,
			DisplayName = displayName,
			Icon = LoadSprite(iconName.StartsWith("T2_", StringComparison.Ordinal) ? ("Assets/_Game/Art/WorldMap/Resources/WM_Resource_" + iconName.Substring(3) + ".png") : ("Assets/_Game/Art/WorldMap/Resources/WM_TMP_Resource_" + iconName + ".png")),
			IsFuturePlaceholder = future
		};
	}

	private static Sprite LoadSprite(string path)
	{
		Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
		if (sprite == null)
		{
			throw new FileNotFoundException("WorldMap sprite could not be loaded: " + path, path);
		}
		return sprite;
	}

	private static T LoadOrCreate<T>(string path) where T : ScriptableObject
	{
		T asset = AssetDatabase.LoadAssetAtPath<T>(path);
		if (asset != null)
		{
			return asset;
		}
		asset = ScriptableObject.CreateInstance<T>();
		asset.name = Path.GetFileNameWithoutExtension(path);
		AssetDatabase.CreateAsset(asset, path);
		return asset;
	}

	private static void Set(SerializedObject serialized, string property, string value)
	{
		serialized.FindProperty(property).stringValue = value ?? string.Empty;
	}

	private static void SetColor(SerializedObject serialized, string property, string hex)
	{
		if (!ColorUtility.TryParseHtmlString(hex, out var color))
		{
			throw new InvalidOperationException("Invalid theme color '" + hex + "'.");
		}
		serialized.FindProperty(property).colorValue = color;
	}

	private static void SetTypography(SerializedObject serialized, string property, Font font, int size, FontStyle style)
	{
		SerializedProperty serializedProperty = serialized.FindProperty(property);
		serializedProperty.FindPropertyRelative("font").objectReferenceValue = font;
		serializedProperty.FindPropertyRelative("size").intValue = size;
		serializedProperty.FindPropertyRelative("style").enumValueIndex = (int)style;
	}

	private static void SetStrings(SerializedProperty property, string[] values)
	{
		property.arraySize = ((values != null) ? values.Length : 0);
		for (int index = 0; index < property.arraySize; index++)
		{
			property.GetArrayElementAtIndex(index).stringValue = values[index];
		}
	}

	private static void SetResources(SerializedProperty property, WorldMapResourcePreview[] values)
	{
		property.arraySize = ((values != null) ? values.Length : 0);
		for (int index = 0; index < property.arraySize; index++)
		{
			SerializedProperty arrayElementAtIndex = property.GetArrayElementAtIndex(index);
			Set(arrayElementAtIndex, "StableId", values[index].StableId);
			Set(arrayElementAtIndex, "DisplayName", values[index].DisplayName);
			arrayElementAtIndex.FindPropertyRelative("Icon").objectReferenceValue = values[index].Icon;
			arrayElementAtIndex.FindPropertyRelative("IsFuturePlaceholder").boolValue = values[index].IsFuturePlaceholder;
		}
	}

	private static void Set(SerializedProperty serialized, string property, string value)
	{
		serialized.FindPropertyRelative(property).stringValue = value ?? string.Empty;
	}

	private static void RequireVisuals()
	{
		string[] array = new string[22]
		{
			"Assets/_Game/Art/WorldMap/Backgrounds/WM_TMP_RegionalMap_EidrenV02.png", "Assets/_Game/Art/WorldMap/Nodes/WM_TMP_Node_Fallback.png", "Assets/_Game/Art/WorldMap/Nodes/WM_TMP_Node_HomeBase.png", "Assets/_Game/Art/WorldMap/Nodes/WM_TMP_Node_Greenwood.png", "Assets/_Game/Art/WorldMap/Nodes/WM_TMP_Node_Quarry.png", "Assets/_Game/Art/WorldMap/Nodes/WM_TMP_Node_Marsh.png", "Assets/_Game/Art/WorldMap/Nodes/WM_TMP_Node_EmberRuins.png", "Assets/_Game/Art/WorldMap/Nodes/WM_Node_TwilightGrove.png", "Assets/_Game/Art/WorldMap/Nodes/WM_Node_VeilMarsh.png", "Assets/_Game/Art/WorldMap/Nodes/WM_Node_GreyRifts.png",
			"Assets/_Game/Art/WorldMap/Resources/WM_TMP_Resource_Wood.png", "Assets/_Game/Art/WorldMap/Resources/WM_TMP_Resource_Stone.png", "Assets/_Game/Art/WorldMap/Resources/WM_TMP_Resource_Fiber.png", "Assets/_Game/Art/WorldMap/Resources/WM_TMP_Resource_Copper.png", "Assets/_Game/Art/WorldMap/Resources/WM_Resource_Hardwood.png", "Assets/_Game/Art/WorldMap/Resources/WM_Resource_SwampHemp.png", "Assets/_Game/Art/WorldMap/Resources/WM_Resource_Granite.png", "Assets/_Game/Art/WorldMap/Resources/WM_Resource_IronOre.png", "Assets/_Game/Art/WorldMap/Markers/WM_TMP_Marker_Boss.png", "Assets/_Game/Art/WorldMap/Markers/WM_TMP_Marker_Event.png",
			"Assets/_Game/Art/WorldMap/Markers/WM_TMP_Marker_Locked.png", "Assets/_Game/Art/WorldMap/Markers/WM_TMP_Marker_Selected.png"
		};
		foreach (string path in array)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("Required Eidren WorldMap visual is missing: " + path, path);
			}
		}
	}

	private static void EnsureFolders()
	{
		string[] array = new string[19]
		{
			"Assets/_Game/Art/WorldMap", "Assets/_Game/Art/WorldMap/Backgrounds", "Assets/_Game/Art/WorldMap/Regions", "Assets/_Game/Art/WorldMap/Nodes", "Assets/_Game/Art/WorldMap/Resources", "Assets/_Game/Art/WorldMap/Markers", "Assets/_Game/Art/WorldMap/Frames", "Assets/_Game/Art/WorldMap/Effects", "Assets/_Game/Art/WorldMap/Temporary", "Assets/_Game/UI/WorldMap",
			"Assets/_Game/UI/WorldMap/Prefabs", "Assets/_Game/UI/WorldMap/Sprites", "Assets/_Game/UI/WorldMap/Themes", "Assets/_Game/UI/WorldMap/Animations", "Assets/_Game/Data/WorldMap", "Assets/_Game/Data/WorldMap/Maps", "Assets/_Game/Data/WorldMap/Nodes", "Assets/_Game/Data/WorldMap/Themes", "Assets/_Game/Scenes/WorldMap"
		};
		for (int i = 0; i < array.Length; i++)
		{
			EnsureFolder(array[i]);
		}
	}

	private static void CreateTierTwoSprites()
	{
		CreateTerrainSprite("TwilightGrove", new Color(0.05f, 0.12f, 0.09f), new Color(0.17f, 0.38f, 0.22f), 0);
		CreateTerrainSprite("VeilMarsh", new Color(0.08f, 0.15f, 0.17f), new Color(0.36f, 0.56f, 0.5f), 1);
		CreateTerrainSprite("GreyRifts", new Color(0.15f, 0.16f, 0.18f), new Color(0.55f, 0.47f, 0.42f), 2);
		CreateResourceSprite("Hardwood", new Color(0.22f, 0.13f, 0.08f), 0);
		CreateResourceSprite("SwampHemp", new Color(0.45f, 0.58f, 0.27f), 1);
		CreateResourceSprite("Granite", new Color(0.55f, 0.58f, 0.62f), 2);
		CreateResourceSprite("IronOre", new Color(0.67f, 0.38f, 0.29f), 3);
	}

	private static void CreateTerrainSprite(string name, Color background, Color accent, int motif)
	{
		Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, mipChain: false);
		Color[] pixels = new Color[65536];
		for (int y = 0; y < 256; y++)
		{
			for (int x = 0; x < 256; x++)
			{
				float nx = ((float)x + 0.5f) / 256f * 2f - 1f;
				float ny = ((float)y + 0.5f) / 256f * 2f - 1f;
				if (Mathf.Sqrt(nx * nx + ny * ny) > 0.98f)
				{
					pixels[y * 256 + x] = Color.clear;
					continue;
				}
				float noise = Mathf.Sin((float)x * 0.17f + (float)y * 0.11f) * 0.035f;
				Color color = background * (0.92f + noise);
				pixels[y * 256 + x] = ((motif switch
				{
					0 => (ny < -0.05f && Mathf.Abs(nx) < 0.12f + (ny + 1f) * 0.18f) || (nx + 0.35f) * (nx + 0.35f) + (ny - 0.15f) * (ny - 0.15f) < 0.18f || (nx - 0.32f) * (nx - 0.32f) + (ny - 0.02f) * (ny - 0.02f) < 0.22f, 
					1 => Mathf.Abs(ny + 0.22f * Mathf.Sin(nx * 7f)) < 0.12f || (Mathf.Abs(nx) > 0.48f && Mathf.Abs(nx) < 0.57f && ny > -0.55f), 
					_ => Mathf.Abs(ny - nx * 0.52f) < 0.12f || Mathf.Abs(ny + nx * 0.75f + 0.22f) < 0.08f, 
				}) ? accent : color);
			}
		}
		texture.SetPixels(pixels);
		texture.Apply();
		string path = "Assets/_Game/Art/WorldMap/Nodes/WM_Node_" + name + ".png";
		File.WriteAllBytes(path, texture.EncodeToPNG());
		UnityEngine.Object.DestroyImmediate(texture);
		ImportAsSprite(path, 256, compressed: false);
	}

	private static void CreateResourceSprite(string name, Color ink, int motif)
	{
		Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, mipChain: false);
		Color[] pixels = new Color[16384];
		for (int y = 0; y < 128; y++)
		{
			for (int x = 0; x < 128; x++)
			{
				float nx = ((float)x + 0.5f) / 128f * 2f - 1f;
				float ny = ((float)y + 0.5f) / 128f * 2f - 1f;
				bool fill = motif switch
				{
					0 => (Mathf.Abs(nx) < 0.22f && ny < 0.45f) || nx * nx + (ny - 0.28f) * (ny - 0.28f) < 0.34f, 
					1 => Mathf.Abs(nx - 0.28f * Mathf.Sin((ny + 1f) * 5f)) < 0.08f && ny < 0.72f, 
					2 => Mathf.Abs(nx) + Mathf.Abs(ny) < 0.78f, 
					_ => Mathf.Abs(nx) + Mathf.Abs(ny) < 0.72f && Mathf.Abs(nx - ny * 0.3f) > 0.08f, 
				};
				float radius = Mathf.Sqrt(nx * nx + ny * ny);
				bool ring = radius > 0.8f && radius < 0.91f;
				float grain = (float)((x * 83 + y * 139 + x * y * 5) % 101) / 10100f;
				pixels[y * 128 + x] = ((fill || ring) ? new Color(ink.r + grain, ink.g + grain * 0.6f, ink.b + grain * 0.3f, ink.a) : new Color(grain, grain * 0.6f, grain * 0.3f, 0f));
			}
		}
		texture.SetPixels(pixels);
		texture.Apply();
		string path = "Assets/_Game/Art/WorldMap/Resources/WM_Resource_" + name + ".png";
		File.WriteAllBytes(path, texture.EncodeToPNG());
		UnityEngine.Object.DestroyImmediate(texture);
		ImportAsSprite(path, 256, compressed: false);
	}

	private static void EnsureFolder(string path)
	{
		if (!AssetDatabase.IsValidFolder(path))
		{
			string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
			string name = Path.GetFileName(path);
			if (!string.IsNullOrWhiteSpace(parent))
			{
				EnsureFolder(parent);
			}
			AssetDatabase.CreateFolder(parent, name);
		}
	}
}
}
