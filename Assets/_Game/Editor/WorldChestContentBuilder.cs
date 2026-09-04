using Eidren.Data;
using Eidren.Interaction;
using Eidren.Presentation;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class WorldChestContentBuilder
{
	public const string DataRoot = "Assets/_Game/Data/Loot/WorldChests";

	public const string PrefabRoot = "Assets/_Game/Prefabs/Loot/WorldChests";

	private const string MaterialRoot = "Assets/_Game/Art/Loot/Materials";

	private const string MeshRoot = "Assets/_Game/Art/Loot/Meshes";

	private static int _meshSequence;

	[MenuItem("Eidren/V0.2/Build World Chests")]
	public static void Build()
	{
		EnsureFolders();
		_meshSequence = 0;
		GameObject common = LoadApprovedOrBuild("WorldChest_Common", WorldChestFamily.Common, new Color(0.31f, 0.18f, 0.09f), new Color(0.7f, 0.52f, 0.25f), new Vector3(1.15f, 0.54f, 0.72f));
		GameObject guarded = LoadApprovedOrBuild("WorldChest_Guarded", WorldChestFamily.Guarded, new Color(0.16f, 0.14f, 0.13f), new Color(0.55f, 0.32f, 0.13f), new Vector3(1.45f, 0.7f, 0.88f));
		GameObject hidden = LoadApprovedOrBuild("WorldChest_Hidden", WorldChestFamily.Hidden, new Color(0.2f, 0.24f, 0.13f), new Color(0.42f, 0.3f, 0.16f), new Vector3(1f, 0.48f, 0.64f));
		string[] regularEquipment = new string[29]
		{
			"hammer", "daggers", "armor_wanderer_hood", "armor_wanderer_coat", "armor_wanderer_bracers", "armor_wanderer_legs", "axe", "pickaxe", "scythe", "copper_hammer",
			"copper_daggers", "copper_spear", "armor_copper_helmet", "armor_copper_chest", "armor_copper_gloves", "armor_copper_legs", "copper_axe", "copper_pickaxe", "copper_scythe", "iron_hammer",
			"iron_daggers", "iron_spear", "armor_iron_helmet", "armor_iron_chest", "armor_iron_gloves", "armor_iron_legs", "iron_axe", "iron_pickaxe", "iron_scythe"
		};
		string[] named = new string[3] { "sealbreaker", "ash_fangs", "ember_thorn" };
		string[] legacyRaw = new string[4] { "wood", "plant_fiber", "stone", "copper_ore" };
		string[] legacyRefined = new string[4] { "plank", "rope", "stone_block", "copper_bar" };
		string[] tierTwoRaw = new string[4] { "hardwood", "swamp_hemp", "granite", "iron_ore" };
		string[] tierTwoRefined = new string[4] { "hardwood_plank", "robust_cloth", "cut_granite", "iron_bar" };
		ConfigureZone("Zone_Greenwood", "world_chests.greenwood", tierTwo: false, legacyRaw, legacyRefined, regularEquipment, named, common, guarded, hidden);
		ConfigureZone("Zone_Marsh", "world_chests.marsh", tierTwo: false, legacyRaw, legacyRefined, regularEquipment, named, common, guarded, hidden);
		ConfigureZone("Zone_Quarry", "world_chests.quarry", tierTwo: false, legacyRaw, legacyRefined, regularEquipment, named, common, guarded, hidden);
		ConfigureZone("Zone_EmberRuins", "world_chests.ember_ruins", tierTwo: false, legacyRaw, legacyRefined, regularEquipment, named, common, guarded, hidden);
		ConfigureZone("Zone_TwilightGrove", "world_chests.twilight_grove", tierTwo: true, tierTwoRaw, tierTwoRefined, regularEquipment, named, common, guarded, hidden);
		ConfigureZone("Zone_VeilMarsh", "world_chests.veil_marsh", tierTwo: true, tierTwoRaw, tierTwoRefined, regularEquipment, named, common, guarded, hidden);
		ConfigureZone("Zone_GreyRifts", "world_chests.grey_rifts", tierTwo: true, tierTwoRaw, tierTwoRefined, regularEquipment, named, common, guarded, hidden);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	public static void RebuildPrefabsForFixes()
	{
		EnsureFolders();
		_meshSequence = 0;
		LoadApprovedOrBuild("WorldChest_Common", WorldChestFamily.Common, new Color(0.31f, 0.18f, 0.09f), new Color(0.7f, 0.52f, 0.25f), new Vector3(1.15f, 0.54f, 0.72f));
		LoadApprovedOrBuild("WorldChest_Guarded", WorldChestFamily.Guarded, new Color(0.16f, 0.14f, 0.13f), new Color(0.55f, 0.32f, 0.13f), new Vector3(1.45f, 0.7f, 0.88f));
		LoadApprovedOrBuild("WorldChest_Hidden", WorldChestFamily.Hidden, new Color(0.2f, 0.24f, 0.13f), new Color(0.42f, 0.3f, 0.16f), new Vector3(1f, 0.48f, 0.64f));
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private static GameObject LoadApprovedOrBuild(string name, WorldChestFamily family, Color bodyColor, Color accentColor, Vector3 size)
	{
		if (MidpolyWorldChestMigration.IsApprovedWorldChest(name))
		{
			return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/" + name + ".prefab");
		}
		return BuildChestPrefab(name, family, bodyColor, accentColor, size);
	}

	private static void ConfigureZone(string zoneAssetName, string profileId, bool tierTwo, string[] raw, string[] refined, string[] equipment, string[] named, GameObject common, GameObject guarded, GameObject hidden)
	{
		ZoneDefinition zone = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/" + zoneAssetName + ".asset");
		if (zone == null)
		{
			return;
		}
		string profileName = zoneAssetName.Replace("Zone_", string.Empty);
		string profilePath = "Assets/_Game/Data/Loot/WorldChests/" + profileName + ".asset";
		WorldChestLootProfile profile = AssetDatabase.LoadAssetAtPath<WorldChestLootProfile>(profilePath);
		if (profile == null)
		{
			if (AssetDatabase.LoadMainAssetAtPath(profilePath) != null)
			{
				AssetDatabase.DeleteAsset(profilePath);
			}
			profile = ScriptableObject.CreateInstance<WorldChestLootProfile>();
			profile.name = profileName;
			AssetDatabase.CreateAsset(profile, profilePath);
		}
		SerializedObject profileSerialized = new SerializedObject(profile);
		Set(profileSerialized, "id", profileId);
		profileSerialized.FindProperty("tierTwoZone").boolValue = tierTwo;
		SetStrings(profileSerialized, "regionalRawItemIds", raw);
		SetStrings(profileSerialized, "regionalRefinementItemIds", refined);
		SetStrings(profileSerialized, "smallConsumableItemIds", new string[1] { "berry" });
		SetStrings(profileSerialized, "usefulConsumableItemIds", new string[2] { "healing_potion", "buff_food" });
		SetStrings(profileSerialized, "valuableConsumableItemIds", new string[2] { "healing_potion", "buff_food" });
		SetStrings(profileSerialized, "regularEquipmentItemIds", equipment);
		SetStrings(profileSerialized, "namedWeaponItemIds", named);
		profileSerialized.FindProperty("commonPrefab").objectReferenceValue = common;
		profileSerialized.FindProperty("guardedPrefab").objectReferenceValue = guarded;
		profileSerialized.FindProperty("hiddenPrefab").objectReferenceValue = hidden;
		GameObject elite = (tierTwo ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/TierTwo/RiftGuardian.prefab") : null);
		profileSerialized.FindProperty("guardedElitePrefab").objectReferenceValue = elite;
		SerializedProperty companions = profileSerialized.FindProperty("guardedCompanionPrefabs");
		companions.arraySize = (tierTwo ? 2 : 0);
		if (tierTwo)
		{
			companions.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/TierTwo/Riftling.prefab");
			companions.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/TierTwo/MoorThrower.prefab");
		}
		profileSerialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(profile);
		SerializedObject serializedObject = new SerializedObject(zone);
		serializedObject.FindProperty("worldChestLootProfile").objectReferenceValue = profile;
		WritePoints(serializedObject.FindProperty("worldChestSpawnPoints"));
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(zone);
	}

	private static void WritePoints(SerializedProperty points)
	{
		(string, Vector3, float, WorldChestFamilyMask)[] values = new(string, Vector3, float, WorldChestFamilyMask)[10]
		{
			("common_01", new Vector3(-26f, 0.12f, -18f), 24f, WorldChestFamilyMask.Common),
			("common_02", new Vector3(-13f, 0.12f, -5f), 332f, WorldChestFamilyMask.Common),
			("common_03", new Vector3(11f, 0.12f, -9f), 18f, WorldChestFamilyMask.Common),
			("common_04", new Vector3(25f, 0.12f, 11f), 288f, WorldChestFamilyMask.Common),
			("common_05", new Vector3(-17f, 0.12f, 20f), 78f, WorldChestFamilyMask.Common),
			("common_06", new Vector3(8f, 0.12f, 26f), 205f, WorldChestFamilyMask.Common),
			("guarded_01", new Vector3(1f, 0.12f, 10f), 180f, WorldChestFamilyMask.Guarded),
			("guarded_02", new Vector3(23f, 0.12f, -22f), 310f, WorldChestFamilyMask.Guarded),
			("hidden_01", new Vector3(-30f, 0.12f, 26f), 122f, WorldChestFamilyMask.Hidden),
			("hidden_02", new Vector3(30f, 0.12f, -27f), 302f, WorldChestFamilyMask.Hidden)
		};
		points.arraySize = values.Length;
		for (int index = 0; index < values.Length; index++)
		{
			SerializedProperty arrayElementAtIndex = points.GetArrayElementAtIndex(index);
			arrayElementAtIndex.FindPropertyRelative("stableId").stringValue = values[index].Item1;
			arrayElementAtIndex.FindPropertyRelative("position").vector3Value = values[index].Item2;
			arrayElementAtIndex.FindPropertyRelative("rotationY").floatValue = values[index].Item3;
			arrayElementAtIndex.FindPropertyRelative("allowedFamilies").intValue = (int)values[index].Item4;
		}
	}

	private readonly struct ChestPalette
	{
		public ChestPalette(Color body, Color accent)
		{
			Body = body;
			Plank = Color.Lerp(body, Color.white, 0.12f);
			Accent = accent;
			Metal = Color.Lerp(accent, Color.black, 0.35f);
			Interior = new Color(0.035f, 0.028f, 0.02f);
			Lining = Color.Lerp(body, Color.white, 0.28f);
			Gold = new Color(0.84f, 0.66f, 0.29f);
		}

		public Color Body { get; }

		public Color Plank { get; }

		public Color Accent { get; }

		public Color Metal { get; }

		public Color Interior { get; }

		public Color Lining { get; }

		public Color Gold { get; }
	}

	private static GameObject BuildChestPrefab(string name, WorldChestFamily family, Color bodyColor, Color accentColor, Vector3 size)
	{
		ChestPalette palette = new ChestPalette(bodyColor, accentColor);
		Material material = EidrenWorldStyleAssets.EnsureWorldMaterial();
		GameObject root = new GameObject(name);

		// Korpus: Sockel + zwei Plankenlagen + geschnitzte Frontplatte unter "CarvedBase".
		GameObject basis = new GameObject("CarvedBase");
		basis.transform.SetParent(root.transform, worldPositionStays: false);
		MeshObject("Sockel", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 1.06f, size.y * 0.11f, size.z * 1.1f), 1f, palette.Accent)), material, basis.transform);
		MeshObject("Planke_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x, size.y * 0.28f, size.z), 1f, palette.Body)), material, basis.transform).transform.localPosition = Vector3.up * (size.y * 0.11f);
		MeshObject("Planke_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x, size.y * 0.26f, size.z), 0.98f, palette.Plank)), material, basis.transform).transform.localPosition = Vector3.up * (size.y * 0.39f);
		MeshObject("Frontplatte", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.58f, size.y * 0.3f, size.z * 0.06f), 0.92f, palette.Accent)), material, basis.transform).transform.localPosition = new Vector3(0f, size.y * 0.16f, (0f - size.z) * 0.5f);

		// Deckel: Bogen-Loft + Firstleiste am LidPivot (Position wie Bestand).
		GameObject lidPivot = new GameObject("LidPivot");
		lidPivot.transform.SetParent(root.transform, worldPositionStays: false);
		lidPivot.transform.localPosition = new Vector3(0f, size.y * 0.62f, size.z * 0.42f);
		EidrenMeshFactory.LoftProfile[] bogen =
		{
			new EidrenMeshFactory.LoftProfile(0f, size.x * 0.98f, size.z * 0.92f),
			new EidrenMeshFactory.LoftProfile(size.y * 0.16f, size.x * 0.92f, size.z * 0.88f),
			new EidrenMeshFactory.LoftProfile(size.y * 0.3f, size.x * 0.76f, size.z * 0.72f),
			new EidrenMeshFactory.LoftProfile(size.y * 0.42f, size.x * 0.5f, size.z * 0.5f)
		};
		GameObject lid = MeshObject("ArchedLid", Persist(EidrenMeshFactory.Loft(bogen, EidrenMeshFactory.LoftShape.Rect, palette.Body, capBottom: false, capTop: true)), material, lidPivot.transform);
		lid.transform.localPosition = new Vector3(0f, 0f, (0f - size.z) * 0.42f);
		MeshObject("Firstleiste", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.62f, size.y * 0.06f, size.z * 0.3f), 0.9f, palette.Plank)), material, lidPivot.transform).transform.localPosition = new Vector3(0f, size.y * 0.42f, (0f - size.z) * 0.42f);

		// Nur-geschlossen-Details: Schnalle/Schloss + Beschlagbaender.
		GameObject closed = new GameObject("ClosedDetails");
		closed.transform.SetParent(root.transform, worldPositionStays: false);
		string schlossName = ((family == WorldChestFamily.Guarded) ? "SealLock" : "CarvedClasp");
		float schlossBreite = ((family == WorldChestFamily.Guarded) ? (size.x * 0.22f) : (size.x * 0.15f));
		MeshObject(schlossName, Persist(EidrenMeshFactory.TaperedBox(new Vector3(schlossBreite, size.y * 0.34f, size.z * 0.1f), 0.8f, palette.Metal)), material, closed.transform).transform.localPosition = new Vector3(0f, size.y * 0.42f, (0f - size.z) * 0.5f);
		int bandCount = ((family == WorldChestFamily.Guarded) ? 3 : 2);
		for (int index = 0; index < bandCount; index++)
		{
			// Baender haengen wie im Bestand an der Wurzel und bleiben in jedem Zustand sichtbar.
			float x = ((bandCount == 3) ? ((float)(index - 1) * size.x * 0.32f) : (((index == 0) ? (-1f) : 1f) * size.x * 0.31f));
			MeshObject($"ForgedBand_{index + 1}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.08f, size.y * 1.02f, size.z * 1.06f), 0.98f, palette.Metal)), material, root.transform).transform.localPosition = new Vector3(x, 0f, 0f);
		}
		if (family == WorldChestFamily.Guarded)
		{
			for (int ecke = 0; ecke < 4; ecke++)
			{
				float ex = (((ecke & 1) == 0) ? (-1f) : 1f) * size.x * 0.47f;
				float ez = (((ecke & 2) == 0) ? (-1f) : 1f) * size.z * 0.42f;
				MeshObject($"Kantenpanzer_{ecke + 1}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.09f, size.y * 0.5f, size.x * 0.09f), 0.9f, palette.Accent)), material, basis.transform).transform.localPosition = new Vector3(ex, 0f, ez);
			}
		}
		if (family == WorldChestFamily.Hidden)
		{
			EidrenMeshFactory.LoftProfile[] wurzel =
			{
				new EidrenMeshFactory.LoftProfile(0f, size.x * 1.14f, size.z * 1.14f),
				new EidrenMeshFactory.LoftProfile(size.y * 0.2f, size.x * 1.02f, size.z * 1.02f)
			};
			GameObject rootWrap = MeshObject("RootWrap", Persist(EidrenMeshFactory.Loft(wurzel, EidrenMeshFactory.LoftShape.Oct, palette.Accent, capBottom: false, capTop: false)), material, root.transform);
			rootWrap.transform.localPosition = new Vector3(0.06f, size.y * 0.12f, 0.02f);
			rootWrap.transform.localRotation = Quaternion.Euler(0f, 17f, -7f);
			MeshObject("Moos_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.2f, size.y * 0.08f, size.z * 0.24f), 0.6f, new Color(0.29f, 0.39f, 0.18f))), material, lidPivot.transform).transform.localPosition = new Vector3((0f - size.x) * 0.18f, size.y * 0.38f, (0f - size.z) * 0.36f);
			MeshObject("Moos_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.14f, size.y * 0.06f, size.z * 0.16f), 0.6f, new Color(0.29f, 0.39f, 0.18f))), material, lidPivot.transform).transform.localPosition = new Vector3(size.x * 0.22f, size.y * 0.34f, (0f - size.z) * 0.5f);
		}

		// Offen/Leer-Details wie Bestand, nur mit Fabrik-Meshes.
		GameObject opened = new GameObject("OpenedDetails");
		opened.transform.SetParent(root.transform, worldPositionStays: false);
		MeshObject("DarkInterior", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.78f, 0.035f, size.z * 0.7f), 0.97f, palette.Interior)), material, opened.transform).transform.localPosition = new Vector3(0f, size.y * 0.64f, 0f);
		GameObject emptied = new GameObject("EmptiedDetails");
		emptied.transform.SetParent(root.transform, worldPositionStays: false);
		MeshObject("EmptyLining", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.62f, 0.025f, size.z * 0.52f), 0.98f, palette.Lining)), material, emptied.transform).transform.localPosition = new Vector3(0f, size.y * 0.645f, 0f);

		// Loot-Fuellstand: voller Haufen und flacher Rest.
		GameObject lootFull = new GameObject("LootFill_Full");
		lootFull.transform.SetParent(root.transform, worldPositionStays: false);
		MeshObject("Gold_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.42f, size.y * 0.18f, size.z * 0.4f), 0.55f, palette.Gold)), material, lootFull.transform).transform.localPosition = new Vector3((0f - size.x) * 0.08f, size.y * 0.66f, 0f);
		MeshObject("Gold_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.26f, size.y * 0.12f, size.z * 0.26f), 0.5f, palette.Gold)), material, lootFull.transform).transform.localPosition = new Vector3(size.x * 0.16f, size.y * 0.66f, (0f - size.z) * 0.1f);
		MeshObject("Gold_3", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.16f, size.y * 0.1f, size.z * 0.16f), 0.45f, palette.Gold)), material, lootFull.transform).transform.localPosition = new Vector3(0f, size.y * 0.78f, size.z * 0.06f);
		GameObject lootPartial = new GameObject("LootFill_Partial");
		lootPartial.transform.SetParent(root.transform, worldPositionStays: false);
		MeshObject("GoldRest_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.2f, size.y * 0.07f, size.z * 0.18f), 0.55f, palette.Gold)), material, lootPartial.transform).transform.localPosition = new Vector3((0f - size.x) * 0.22f, size.y * 0.655f, size.z * 0.14f);
		MeshObject("GoldRest_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.14f, size.y * 0.05f, size.z * 0.12f), 0.55f, palette.Gold)), material, lootPartial.transform).transform.localPosition = new Vector3(size.x * 0.24f, size.y * 0.65f, (0f - size.z) * 0.18f);

		// Collider, Haende, Verdrahtung: exakt wie Bestand.
		BoxCollider boxCollider = root.AddComponent<BoxCollider>();
		boxCollider.size = new Vector3(size.x, size.y, size.z);
		boxCollider.center = Vector3.up * size.y * 0.5f;
		SphereCollider sphereCollider = root.AddComponent<SphereCollider>();
		sphereCollider.isTrigger = true;
		sphereCollider.radius = 1.45f;
		Sprite handSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Resources/Art/UI/ui_interaction_hand.png");
		SpriteRenderer leftHand = OpeningHand(root.transform, "OpeningHand_Left", handSprite, new Vector3((0f - size.x) * 0.25f, size.y * 0.72f, (0f - size.z) * 0.62f), mirrored: false);
		SpriteRenderer rightHand = OpeningHand(root.transform, "OpeningHand_Right", handSprite, new Vector3(size.x * 0.25f, size.y * 0.72f, (0f - size.z) * 0.62f), mirrored: true);
		WorldChestVisual visual = root.AddComponent<WorldChestVisual>();
		visual.Configure(lidPivot.transform, closed, opened, emptied, leftHand, rightHand, lootFull, lootPartial);
		root.AddComponent<WorldChestContainer>().ConfigureVisual(visual, null, InteractionUtility.StandardSurfaceRange);
		visual.Apply(WorldChestVisualState.Closed);
		int dreiecke = 0;
		foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
		{
			dreiecke += filter.sharedMesh.triangles.Length / 3;
		}
		Debug.Log($"[StilE1] {name}: {dreiecke} Dreiecke");
		string path = "Assets/_Game/Prefabs/Loot/WorldChests/" + name + ".prefab";
		// G-004: Bodenerdung als Bauteil, nicht als Nachlauf. Attach misst nur
		// MeshRenderer — die Sprite-Glyphen OpeningHand_Left/Right (F-005)
		// wuerden die Bounds sonst von 1,2 auf 11,4 Einheiten aufblaehen.
		WorldContactShadowBuilder.Attach(root);
		GameObject result = PrefabUtility.SaveAsPrefabAsset(root, path);
		UnityEngine.Object.DestroyImmediate(root);
		return result;
	}

	private static SpriteRenderer OpeningHand(Transform parent, string objectName, Sprite sprite, Vector3 position, bool mirrored)
	{
		GameObject gameObject = new GameObject(objectName, typeof(SpriteRenderer));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.localPosition = position;
		gameObject.transform.localScale = Vector3.one * 0.72f;
		SpriteRenderer component = gameObject.GetComponent<SpriteRenderer>();
		component.sprite = sprite;
		component.flipX = mirrored;
		component.sortingOrder = 24;
		component.enabled = false;
		return component;
	}

	private static Mesh Persist(Mesh mesh)
	{
		string path = string.Format("{0}/WorldChestMesh_{1:000}_{2}.asset", MeshRoot, _meshSequence++, mesh.name);
		Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
		if (existing != null)
		{
			EditorUtility.CopySerialized(mesh, existing);
			UnityEngine.Object.DestroyImmediate(mesh);
			EditorUtility.SetDirty(existing);
			return existing;
		}
		AssetDatabase.CreateAsset(mesh, path);
		return mesh;
	}

	private static GameObject MeshObject(string name, Mesh mesh, Material material, Transform parent)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
		return gameObject;
	}

	private static void SetStrings(SerializedObject serialized, string property, string[] values)
	{
		SerializedProperty array = serialized.FindProperty(property);
		array.arraySize = values.Length;
		for (int index = 0; index < values.Length; index++)
		{
			array.GetArrayElementAtIndex(index).stringValue = values[index];
		}
	}

	private static void Set(SerializedObject serialized, string property, string value)
	{
		serialized.FindProperty(property).stringValue = value;
	}

	private static void EnsureFolders()
	{
		EnsureFolder("Assets/_Game/Data/Loot/WorldChests");
		EnsureFolder("Assets/_Game/Prefabs/Loot/WorldChests");
		EnsureFolder("Assets/_Game/Art/Loot/Materials");
		EnsureFolder("Assets/_Game/Art/Loot/Meshes");
	}

	private static void EnsureFolder(string path)
	{
		if (!AssetDatabase.IsValidFolder(path))
		{
			string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
			if (!string.IsNullOrWhiteSpace(parent))
			{
				EnsureFolder(parent);
			}
			AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
		}
	}
}
}
