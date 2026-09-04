using Eidren.Data;
using Eidren.Interaction;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class ResourceContentBuilder
{
	private const string DataFolder = "Assets/_Game/Data/Resources";

	private const string CatalogPath = "Assets/_Game/Resources/Data/ResourceNodeCatalog_V01.asset";

	private const string NodeFolder = "Assets/_Game/Prefabs/Resources/Nodes";

	private const string YieldTablePath = "Assets/_Game/Data/Resources/HarvestYields_V01.asset";

	private const int HandYield = 1;

	private const int ToolYieldT0 = 2;

	private const int BerryBushesPerZone = 6;

	private const int GreenwoodBerryBushes = 12;

	[MenuItem("Eidren/Data/Build Resource Definitions V0.1")]
	public static ResourceNodeDefinition[] BuildResourceDefinitions()
	{
		ItemContentAssetBuilder.BuildItemDefinitions();
		EnsureFolders();
		T2ResourceVisualBuilder.Build();
		Dictionary<string, ResourceNodeVisualTable.VisualPair> visuals = ResourceNodeVisualTable.BuildAll();
		HarvestYieldTable yields = BuildYieldTable();
		ResourceNodeDefinition[] definitions = new ResourceNodeDefinition[9]
		{
			Definition("Tree", "resource.tree", "Baum", "HOLZ SAMMELN", "wood", yields, 1.8f, visuals["resource.tree"], blocksNavigation: true),
			Definition("StoneDeposit", "resource.stone_deposit", "Steinvorkommen", "STEIN SAMMELN", "stone", yields, 2f, visuals["resource.stone_deposit"], blocksNavigation: true),
			Definition("FiberPlant", "resource.fiber_plant", "Faserpflanze", "FASERN SAMMELN", "plant_fiber", yields, 0.9f, visuals["resource.fiber_plant"], blocksNavigation: false),
			// W-005 am 15.08.2026 zurueckgenommen: Wechsel zur Abgebaut-Optik gilt wieder.
			Definition("CopperVein", "resource.copper_vein", "Kupferader", "KUPFER ABBAUEN", "copper_ore", yields, 2.25f, visuals["resource.copper_vein"], blocksNavigation: true, "pickaxe"),
			Definition("BerryBush", "resource.berry_bush", "Beerenstrauch", "BEEREN SAMMELN", "berry", yields, 0.9f, visuals["resource.berry_bush"], blocksNavigation: false),
			Definition("HardwoodTree", "resource.hardwood_tree", "Hartholzbaum", "HARTHOLZ SCHLAGEN", "hardwood", yields, 2.4f, visuals["resource.hardwood_tree"], blocksNavigation: true, "copper_axe"),
			Definition("SwampHemp", "resource.swamp_hemp", "Sumpfhanf", "SUMPFHANF SCHNEIDEN", "swamp_hemp", yields, 1.35f, visuals["resource.swamp_hemp"], blocksNavigation: false, "copper_scythe"),
			Definition("GraniteDeposit", "resource.granite_deposit", "Granitvorkommen", "GRANIT BRECHEN", "granite", yields, 2.5f, visuals["resource.granite_deposit"], blocksNavigation: true, "copper_pickaxe"),
			Definition("IronVein", "resource.iron_vein", "Eisenader", "EISENERZ ABBAUEN", "iron_ore", yields, 2.65f, visuals["resource.iron_vein"], blocksNavigation: true, "copper_pickaxe")
		};
		ResourceNodeDefinition[] array = definitions;
		for (int i = 0; i < array.Length; i++)
		{
			NodePrefab(array[i]);
		}
		array = definitions;
		for (int i = 0; i < array.Length; i++)
		{
			LinkNodePrefab(array[i]);
		}
		BuildCatalog(definitions);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log($"Eidren: built {definitions.Length} data-driven V0.1 " + "resource types. Der Beerenstrauch bleibt unplatziert -- er ist ein Nebenknoten ausserhalb des 72er-Budgets (M1.7).");
		return definitions;
	}

	[MenuItem("Eidren/Data/Build Resource Collection V0.1")]
	public static void BuildResourceCollection()
	{
		ConfigureZoneAllocations(BuildResourceDefinitions());
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		EidrenSceneStructureBuilder.BuildResourceZoneScenes();
		Debug.Log("Eidren: populated all outdoor zones.");
	}

	// ACHTUNG (3a-Auflage, Etappe 3b Task 4): trotz des Namens baut dieser Einstieg -- ueber
	// BuildResourceDefinitions() -- ALLE NEUN Ressourcentypen (T1 und T2) neu und schreibt via
	// ConfigureZoneAllocations() alle Zonenbudgets aller neun Zonen neu (nicht nur T2). Fuer
	// einen T1-only-Lauf ohne T2-Beruehrung stattdessen RebuildTierOneNodePrefabs() nutzen.
	[MenuItem("Eidren/Data/Build Tier Two Resource Collection")]
	public static void BuildTierTwoResourceCollection()
	{
		ConfigureZoneAllocations(BuildResourceDefinitions());
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: built nine resource types and all T2 budgets.");
	}

	// Schmaler Einstieg fuer Etappe 3, Task 4: die vier T2-Node-Prefabs betten die
	// Visuals als gebackene Kopien ein (0 PrefabInstance-Bloecke, siehe
	// STILUMBAU_E3_BAKEKETTE.md, Abschnitt d) und aktualisieren sich deshalb nicht
	// automatisch, wenn Task 3 die Basis-Visuals neu baut. Ruehrt nur die vier
	// bestehenden ResourceNodeDefinition-Assets an -- kein T1-Ressourcentyp, kein
	// ItemContentAssetBuilder, keine ConfigureZoneAllocations, kein BuildCatalog.
	private static readonly string[] TierTwoResourceIds =
	{
		"resource.hardwood_tree", "resource.swamp_hemp", "resource.granite_deposit", "resource.iron_vein"
	};

	[MenuItem("Eidren/V0.2/Stilumbau/T2-Node-Prefabs neu bauen")]
	public static void RebuildTierTwoNodePrefabs()
	{
		foreach (string id in TierTwoResourceIds)
		{
			string assetName = Path.GetFileNameWithoutExtension(NodePrefabPath(id));
			ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(DataFolder + "/" + assetName + ".asset");
			if (definition == null)
			{
				throw new FileNotFoundException("Ressourcendefinition fehlt fuer '" + id + "': " + assetName + ".asset");
			}
			NodePrefab(definition);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Tier-two node prefabs rebuilt.");
	}

	// Schmaler Einstieg fuer Etappe 3b, Task 4: Spiegelbild von RebuildTierTwoNodePrefabs()
	// fuer die fuenf T1-Ressourcentypen (STILUMBAU_E3B_BAKEKETTE.md, Abschnitt e). Betten die
	// frisch von T1ResourceVisualBuilder gebauten Basis-Visuals als gebackene Kopien in die
	// Node-Prefabs ein; ruehrt nur die fuenf bestehenden T1-ResourceNodeDefinition-Assets an --
	// kein T2-Ressourcentyp, kein ItemContentAssetBuilder, keine ConfigureZoneAllocations, kein
	// BuildCatalog.
	private static readonly string[] TierOneResourceIds =
	{
		"resource.tree", "resource.stone_deposit", "resource.fiber_plant", "resource.copper_vein", "resource.berry_bush"
	};

	[MenuItem("Eidren/V0.2/Stilumbau/T1-Node-Prefabs neu bauen")]
	public static void RebuildTierOneNodePrefabs()
	{
		foreach (string id in TierOneResourceIds)
		{
			string assetName = Path.GetFileNameWithoutExtension(NodePrefabPath(id));
			ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(DataFolder + "/" + assetName + ".asset");
			if (definition == null)
			{
				throw new FileNotFoundException("Ressourcendefinition fehlt fuer '" + id + "': " + assetName + ".asset");
			}
			NodePrefab(definition);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Tier-one node prefabs rebuilt.");
	}

	private static HarvestYieldTable BuildYieldTable()
	{
		HarvestYieldTable table = LoadOrCreate<HarvestYieldTable>("Assets/_Game/Data/Resources/HarvestYields_V01.asset");
		SerializedObject serialized = new SerializedObject(table);
		serialized.FindProperty("handYield").intValue = 1;
		SerializedProperty entries = serialized.FindProperty("toolYields");
		entries.arraySize = 9;
		ToolYield(entries.GetArrayElementAtIndex(0), "Axe", "wood");
		ToolYield(entries.GetArrayElementAtIndex(1), "Scythe", "fiber");
		ToolYield(entries.GetArrayElementAtIndex(2), "Pickaxe", "stone", "ore");
		ToolYield(entries.GetArrayElementAtIndex(3), "CopperAxe", "wood");
		ToolYield(entries.GetArrayElementAtIndex(4), "CopperScythe", "fiber");
		ToolYield(entries.GetArrayElementAtIndex(5), "CopperPickaxe", "stone", "ore");
		ToolYield(entries.GetArrayElementAtIndex(6), "IronAxe", "wood");
		ToolYield(entries.GetArrayElementAtIndex(7), "IronScythe", "fiber");
		ToolYield(entries.GetArrayElementAtIndex(8), "IronPickaxe", "stone", "ore");
		serialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(table);
		string[] errors = table.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException("Invalid harvest yield table: " + string.Join("; ", errors));
		}
		return table;
	}

	private static void ToolYield(SerializedProperty entry, string toolAssetName, params string[] materialFamilies)
	{
		ItemDefinition tool = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/" + toolAssetName + ".asset");
		if (tool == null)
		{
			throw new FileNotFoundException("Tool item is missing: " + toolAssetName);
		}
		entry.FindPropertyRelative("tool").objectReferenceValue = tool;
		entry.FindPropertyRelative("yield").intValue = 2;
		SerializedProperty families = entry.FindPropertyRelative("materialFamilies");
		families.arraySize = materialFamilies.Length;
		for (int index = 0; index < materialFamilies.Length; index++)
		{
			families.GetArrayElementAtIndex(index).stringValue = materialFamilies[index];
		}
	}

	private static ResourceNodeDefinition Definition(string assetName, string id, string displayName, string interactionText, string itemId, HarvestYieldTable yields, float duration, ResourceNodeVisualTable.VisualPair visual, bool blocksNavigation, string requiredToolItemId = "", bool keepsAppearanceWhenExhausted = false)
	{
		ResourceNodeDefinition resourceNodeDefinition = LoadOrCreate<ResourceNodeDefinition>("Assets/_Game/Data/Resources/" + assetName + ".asset");
		ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/" + ItemAssetName(itemId) + ".asset");
		SerializedObject serializedObject = new SerializedObject(resourceNodeDefinition);
		serializedObject.FindProperty("id").stringValue = id;
		serializedObject.FindProperty("displayName").stringValue = displayName;
		serializedObject.FindProperty("interactionText").stringValue = interactionText;
		serializedObject.FindProperty("outputItem").objectReferenceValue = item;
		serializedObject.FindProperty("harvestYields").objectReferenceValue = yields;
		serializedObject.FindProperty("interactionDuration").floatValue = duration;
		serializedObject.FindProperty("interactionRange").floatValue = 2.7f;
		serializedObject.FindProperty("respawnMode").enumValueIndex = 1;
		serializedObject.FindProperty("activeVisualPrefab").objectReferenceValue = visual.Active;
		serializedObject.FindProperty("exhaustedVisualPrefab").objectReferenceValue = visual.Exhausted;
		serializedObject.FindProperty("collectionIcon").objectReferenceValue = item.Icon;
		serializedObject.FindProperty("requiresTool").boolValue = !string.IsNullOrWhiteSpace(requiredToolItemId);
		serializedObject.FindProperty("requiredToolItemId").stringValue = requiredToolItemId ?? string.Empty;
		serializedObject.FindProperty("blocksNavigation").boolValue = blocksNavigation;
		serializedObject.FindProperty("keepsAppearanceWhenExhausted").boolValue = keepsAppearanceWhenExhausted;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(resourceNodeDefinition);
		return resourceNodeDefinition;
	}

	private static void LinkNodePrefab(ResourceNodeDefinition definition)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NodePrefabPath(definition.Id));
		if (prefab == null)
		{
			throw new FileNotFoundException("Node prefab missing for '" + definition.Id + "'.");
		}
		SerializedObject serializedObject = new SerializedObject(definition);
		serializedObject.FindProperty("nodePrefab").objectReferenceValue = prefab;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(definition);
		string[] errors = definition.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException("Invalid resource '" + definition.Id + "': " + string.Join("; ", errors));
		}
	}

	private static string ActiveVisualId(string resourceNodeId)
	{
		return resourceNodeId switch
		{
			"resource.tree" => "visual.tree.active", 
			"resource.stone_deposit" => "visual.stone_deposit.active", 
			"resource.fiber_plant" => "visual.fiber_plant.active", 
			"resource.copper_vein" => "visual.copper_vein.active", 
			"resource.berry_bush" => "visual.berry_bush.active", 
			"resource.hardwood_tree" => "visual.hardwood_tree.active", 
			"resource.swamp_hemp" => "visual.swamp_hemp.active", 
			"resource.granite_deposit" => "visual.granite_deposit.active", 
			"resource.iron_vein" => "visual.iron_vein.active", 
			_ => throw new InvalidOperationException("Ressourcenknoten '" + resourceNodeId + "' hat keine Kennung in der Groessentabelle (M10.3)."), 
		};
	}

	private static void NodePrefab(ResourceNodeDefinition definition)
	{
		GameObject root = new GameObject(definition.DisplayName);
		SphereCollider trigger = root.AddComponent<SphereCollider>();
		trigger.isTrigger = true;
		trigger.radius = definition.InteractionRange * 0.45f;
		Collider blocker = null;
		if (definition.BlocksNavigation)
		{
			Vector3 size = VisualScaleTableBuilder.Load().Require(ActiveVisualId(definition.Id)).ColliderSize;
			if (size.sqrMagnitude <= 0f)
			{
				throw new InvalidOperationException("'" + definition.Id + "' sperrt die Navigation, hat in der Groessentabelle aber kein Collider-Mass.");
			}
			CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
			capsule.radius = size.x * 0.5f;
			capsule.height = size.y;
			capsule.center = Vector3.up * capsule.height * 0.5f;
			blocker = capsule;
		}
		GameObject active = (GameObject)PrefabUtility.InstantiatePrefab(definition.ActiveVisualPrefab);
		active.name = "ActiveVisual";
		active.transform.SetParent(root.transform, worldPositionStays: false);
		GameObject spent = (GameObject)PrefabUtility.InstantiatePrefab(definition.ExhaustedVisualPrefab);
		spent.name = "ExhaustedVisual";
		spent.transform.SetParent(root.transform, worldPositionStays: false);
		spent.SetActive(value: false);
		root.AddComponent<ResourceNode>().Configure(definition, definition.Id + ".prefab", active, spent, trigger, blocker);
		PrefabUtility.SaveAsPrefabAsset(root, NodePrefabPath(definition.Id));
		UnityEngine.Object.DestroyImmediate(root);
	}

	private static void BuildCatalog(IReadOnlyList<ResourceNodeDefinition> definitions)
	{
		ResourceNodeCatalogDefinition catalog = LoadOrCreate<ResourceNodeCatalogDefinition>("Assets/_Game/Resources/Data/ResourceNodeCatalog_V01.asset");
		SerializedObject serialized = new SerializedObject(catalog);
		SerializedProperty entries = serialized.FindProperty("definitions");
		entries.arraySize = definitions.Count;
		for (int index = 0; index < definitions.Count; index++)
		{
			entries.GetArrayElementAtIndex(index).objectReferenceValue = definitions[index];
		}
		serialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(catalog);
	}

	private static void ConfigureZoneAllocations(IReadOnlyList<ResourceNodeDefinition> definitions)
	{
		Dictionary<string, ResourceNodeDefinition> byId = new Dictionary<string, ResourceNodeDefinition>();
		foreach (ResourceNodeDefinition definition in definitions)
		{
			byId.Add(definition.Id, definition);
		}
		ResourceNodeDefinition berry = byId["resource.berry_bush"];
		SetAllocations("Zone_HomeBase", ResourceRespawnMode.None, (berry, 4), (byId["resource.tree"], 26), (byId["resource.stone_deposit"], 18), (byId["resource.fiber_plant"], 10));
		SetAllocations("Zone_Greenwood", ResourceRespawnMode.OnZoneEntry, (berry, 12), (byId["resource.tree"], 45), (byId["resource.fiber_plant"], 18), (byId["resource.stone_deposit"], 6), (byId["resource.copper_vein"], 3));
		SetAllocations("Zone_Marsh", ResourceRespawnMode.OnZoneEntry, (berry, 6), (byId["resource.fiber_plant"], 45), (byId["resource.tree"], 18), (byId["resource.copper_vein"], 6), (byId["resource.stone_deposit"], 3));
		SetAllocations("Zone_Quarry", ResourceRespawnMode.OnZoneEntry, (berry, 6), (byId["resource.stone_deposit"], 45), (byId["resource.copper_vein"], 18), (byId["resource.tree"], 6), (byId["resource.fiber_plant"], 3));
		SetAllocations("Zone_EmberRuins", ResourceRespawnMode.OnZoneEntry, (berry, 6), (byId["resource.copper_vein"], 45), (byId["resource.stone_deposit"], 18), (byId["resource.fiber_plant"], 6), (byId["resource.tree"], 3));
		SetAllocations("Zone_TwilightGrove", ResourceRespawnMode.OnZoneEntry, (berry, 6), (byId["resource.hardwood_tree"], 45), (byId["resource.swamp_hemp"], 18), (byId["resource.granite_deposit"], 6), (byId["resource.iron_vein"], 3));
		SetAllocations("Zone_VeilMarsh", ResourceRespawnMode.OnZoneEntry, (berry, 6), (byId["resource.swamp_hemp"], 45), (byId["resource.hardwood_tree"], 18), (byId["resource.iron_vein"], 6), (byId["resource.granite_deposit"], 3));
		SetAllocations("Zone_GreyRifts", ResourceRespawnMode.OnZoneEntry, (berry, 6), (byId["resource.granite_deposit"], 45), (byId["resource.iron_vein"], 18), (byId["resource.hardwood_tree"], 6), (byId["resource.swamp_hemp"], 3));
	}

	private static void SetAllocations(string assetName, ResourceRespawnMode respawnMode, params (ResourceNodeDefinition definition, int count)[] values)
	{
		ZoneDefinition zoneDefinition = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/" + assetName + ".asset");
		if (zoneDefinition == null)
		{
			throw new FileNotFoundException("Zone definition is missing: " + assetName);
		}
		SerializedObject serializedObject = new SerializedObject(zoneDefinition);
		serializedObject.FindProperty("resourcesAllowed").boolValue = true;
		serializedObject.FindProperty("resourceRespawnMode").enumValueIndex = (int)respawnMode;
		WriteAllocations(serializedObject.FindProperty("sideNodeAllocations"), values, 0, 1);
		WriteAllocations(serializedObject.FindProperty("resourceAllocations"), values, 1, values.Length - 1);
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(zoneDefinition);
	}

	private static void WriteAllocations(SerializedProperty allocations, IReadOnlyList<(ResourceNodeDefinition definition, int count)> values, int offset, int length)
	{
		allocations.arraySize = length;
		for (int index = 0; index < length; index++)
		{
			SerializedProperty arrayElementAtIndex = allocations.GetArrayElementAtIndex(index);
			arrayElementAtIndex.FindPropertyRelative("definition").objectReferenceValue = values[offset + index].definition;
			arrayElementAtIndex.FindPropertyRelative("count").intValue = values[offset + index].count;
		}
	}

	public static string NodePrefabPath(string resourceId)
	{
		return "Assets/_Game/Prefabs/Resources/Nodes/" + resourceId switch
		{
			"resource.tree" => "Tree", 
			"resource.stone_deposit" => "StoneDeposit", 
			"resource.fiber_plant" => "FiberPlant", 
			"resource.copper_vein" => "CopperVein", 
			"resource.berry_bush" => "BerryBush", 
			"resource.hardwood_tree" => "HardwoodTree", 
			"resource.swamp_hemp" => "SwampHemp", 
			"resource.granite_deposit" => "GraniteDeposit", 
			"resource.iron_vein" => "IronVein", 
			_ => throw new ArgumentOutOfRangeException("resourceId", resourceId, null), 
		} + ".prefab";
	}

	private static string ItemAssetName(string itemId)
	{
		return itemId switch
		{
			"wood" => "Wood", 
			"stone" => "Stone", 
			"plant_fiber" => "PlantFiber", 
			"copper_ore" => "CopperOre", 
			"berry" => "Berry", 
			"hardwood" => "Hardwood", 
			"swamp_hemp" => "SwampHemp", 
			"granite" => "Granite", 
			"iron_ore" => "IronOre", 
			_ => throw new ArgumentOutOfRangeException("itemId", itemId, null), 
		};
	}

	private static T LoadOrCreate<T>(string path) where T : ScriptableObject
	{
		T value = AssetDatabase.LoadAssetAtPath<T>(path);
		if (value != null)
		{
			return value;
		}
		if (File.Exists(path))
		{
			AssetDatabase.DeleteAsset(path);
		}
		value = ScriptableObject.CreateInstance<T>();
		value.name = Path.GetFileNameWithoutExtension(path);
		AssetDatabase.CreateAsset(value, path);
		return value;
	}

	private static void EnsureFolders()
	{
		Folder("Assets/_Game/Data", "Resources");
		Folder("Assets/_Game/Resources", "Data");
		Folder("Assets/_Game/Prefabs", "Resources");
		Folder("Assets/_Game/Prefabs/Resources", "Visuals");
		Folder("Assets/_Game/Prefabs/Resources", "Nodes");
		Folder("Assets/_Game/Art", "Resources");
		Folder("Assets/_Game/Art/Resources", "Materials");
	}

	private static void Folder(string parent, string name)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + name))
		{
			AssetDatabase.CreateFolder(parent, name);
		}
	}
}
}
