using Eidren.Data;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class BuildingCostContentBuilder
{
	private const string BuildingDataFolder = "Assets/_Game/Data/Buildings";

	[MenuItem("Eidren/Data/Build Building Costs V0.1")]
	public static void BuildBuildingCosts()
	{
		if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Buildings"))
		{
			AssetDatabase.CreateFolder("Assets/_Game/Data", "Buildings");
		}
		BuildingCostDefinition[] buildings = new BuildingCostDefinition[11]
		{
			Ensure("Workbench", "building.workbench", "Werkbank", 0, new(string, int)[2]
			{
				("wood", 8),
				("stone", 6)
			}, BuildingPlacementKind.Object, BuildingGroundRule.GroundOrFloor, BuildingCategory.Workshops, 1, 1),
			Ensure("StorageChest", "building.storage_chest", "Lagerkiste", 0, new(string, int)[2]
			{
				("wood", 10),
				("stone", 4)
			}, BuildingPlacementKind.Object, BuildingGroundRule.GroundOrFloor, BuildingCategory.Supply, 1, 1),
			Ensure("FarmPlot", "building.farm_plot", "Acker", 0, new(string, int)[2]
			{
				("wood", 6),
				("plant_fiber", 4)
			}, BuildingPlacementKind.Object, BuildingGroundRule.GroundOnly, BuildingCategory.Farming, 2, 2, blocksNavigation: true, maximumCount: 2),
			Ensure("Wall", "building.wall", "Mauer", 0, new(string, int)[1] { ("wood", 2) }, BuildingPlacementKind.Edge, BuildingGroundRule.GroundOrFloor, BuildingCategory.Structure, 1, 1),
			Ensure("Floor", "building.floor", "Boden", 0, new(string, int)[1] { ("wood", 2) }, BuildingPlacementKind.Floor, BuildingGroundRule.GroundOnly, BuildingCategory.Structure, 1, 1, blocksNavigation: false),
			Ensure("Door", "building.door", "Tür", 0, new(string, int)[2]
			{
				("wood", 4),
				("plant_fiber", 2)
			}, BuildingPlacementKind.Edge, BuildingGroundRule.GroundOrFloor, BuildingCategory.Structure, 1, 1, blocksNavigation: false),
			Ensure("Smelter", "building.smelter", "Schmelzofen", 1, new(string, int)[2]
			{
				("wood", 6),
				("stone", 12)
			}, BuildingPlacementKind.Object, BuildingGroundRule.RequiresFloor, BuildingCategory.Workshops, 1, 1),
			Ensure("Sawmill", "building.sawmill", "Sägewerk", 1, new(string, int)[3]
			{
				("wood", 10),
				("stone", 6),
				("copper_bar", 1)
			}, BuildingPlacementKind.Object, BuildingGroundRule.RequiresFloor, BuildingCategory.Workshops, 1, 1),
			Ensure("Ropewalk", "building.ropewalk", "Seilerei", 1, new(string, int)[3]
			{
				("wood", 8),
				("stone", 4),
				("copper_bar", 1)
			}, BuildingPlacementKind.Object, BuildingGroundRule.RequiresFloor, BuildingCategory.Workshops, 1, 1),
			Ensure("Stonecutter", "building.stonecutter", "Steinmetz", 1, new(string, int)[3]
			{
				("wood", 6),
				("stone", 10),
				("copper_bar", 1)
			}, BuildingPlacementKind.Object, BuildingGroundRule.RequiresFloor, BuildingCategory.Workshops, 1, 1),
			Ensure("CookingPot", "building.cooking_pot", "Kochtopf", 1, new(string, int)[3]
			{
				("wood", 4),
				("stone", 8),
				("copper_bar", 2)
			}, BuildingPlacementKind.Object, BuildingGroundRule.GroundOrFloor, BuildingCategory.Supply, 1, 1)
		};
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		VerifyOnDisk(buildings.Length);
		Debug.Log($"Eidren: built {buildings.Length} V0.1 building cost " + "definitions.");
	}

	private static BuildingCostDefinition Ensure(string assetName, string id, string displayName, int tier, (string id, int amount)[] cost, BuildingPlacementKind placementKind, BuildingGroundRule groundRule, BuildingCategory category, int width, int depth, bool blocksNavigation = true, int maximumCount = 0)
	{
		string path = "Assets/_Game/Data/Buildings/" + assetName + ".asset";
		BuildingCostDefinition building = AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>(path);
		if (building == null)
		{
			building = ScriptableObject.CreateInstance<BuildingCostDefinition>();
			building.name = assetName;
			AssetDatabase.CreateAsset(building, path);
		}
		SerializedObject serialized = new SerializedObject(building);
		serialized.FindProperty("id").stringValue = id;
		serialized.FindProperty("displayName").stringValue = displayName;
		serialized.FindProperty("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath(assetName));
		serialized.FindProperty("tier").intValue = tier;
		serialized.FindProperty("occupiesBuildField").boolValue = placementKind == BuildingPlacementKind.Object;
		serialized.FindProperty("placementKind").enumValueIndex = (int)placementKind;
		serialized.FindProperty("groundRule").enumValueIndex = (int)groundRule;
		serialized.FindProperty("category").enumValueIndex = (int)category;
		serialized.FindProperty("blocksNavigation").boolValue = blocksNavigation;
		serialized.FindProperty("craftingStation").enumValueIndex = (int)StationFor(id);
		serialized.FindProperty("baseRate").floatValue = 1f;
		serialized.FindProperty("eidraFactor").floatValue = 1f;
		// F31-010: 0 = ohne Mengengrenze.
		serialized.FindProperty("maximumCount").intValue = maximumCount;
		SerializedProperty costProperty = serialized.FindProperty("cost");
		costProperty.arraySize = cost.Length;
		for (int index = 0; index < cost.Length; index++)
		{
			SerializedProperty arrayElementAtIndex = costProperty.GetArrayElementAtIndex(index);
			arrayElementAtIndex.FindPropertyRelative("itemId").stringValue = cost[index].id;
			arrayElementAtIndex.FindPropertyRelative("amount").intValue = cost[index].amount;
		}
		SerializedProperty serializedProperty = serialized.FindProperty("levelFootprints");
		serializedProperty.arraySize = 1;
		SerializedProperty arrayElementAtIndex2 = serializedProperty.GetArrayElementAtIndex(0);
		arrayElementAtIndex2.FindPropertyRelative("width").intValue = width;
		arrayElementAtIndex2.FindPropertyRelative("depth").intValue = depth;
		SerializedProperty serializedProperty2 = serialized.FindProperty("levelPrefabs");
		serializedProperty2.arraySize = 1;
		serializedProperty2.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(assetName));
		serialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(building);
		if (!building.TryValidate(out var error))
		{
			throw new InvalidOperationException("Invalid building cost '" + id + "': " + error);
		}
		return building;
	}

	private static string PrefabPath(string assetName)
	{
		if (!(assetName == "Workbench"))
		{
			if (assetName == "StorageChest")
			{
				return "Assets/_Game/Prefabs/Stations/StorageChest.prefab";
			}
			return "Assets/_Game/Prefabs/Buildings/Level01/BLD_" + assetName + "_L01.prefab";
		}
		return "Assets/_Game/Prefabs/Stations/Workbench.prefab";
	}

	private static string IconPath(string assetName)
	{
		// F31-013: Jedes Gebaeude zeigt sein eigenes Render. Werkbank und
		// Lagerkiste kommen aus dem BuildingIconRenderTool (Engine-Render des
		// Spielmodells), bis ein Grafikauftrag gemalte Fassungen nachliefert —
		// die frueheren Behelfe (Hammer- und Brett-Item) waren fachfremd.
		return "Assets/_Game/Art/Buildings/Level01/BLD_" + assetName + "_L01.png";
	}

	private static CraftingStationType StationFor(string buildingId)
	{
		return buildingId switch
		{
			"building.workbench" => CraftingStationType.Workbench, 
			"building.smelter" => CraftingStationType.Smelter, 
			"building.sawmill" => CraftingStationType.Sawmill, 
			"building.ropewalk" => CraftingStationType.Ropewalk, 
			"building.stonecutter" => CraftingStationType.Stonecutter, 
			"building.cooking_pot" => CraftingStationType.CookingPot, 
			_ => CraftingStationType.None, 
		};
	}

	private static void VerifyOnDisk(int expectedCount)
	{
		string[] guids = AssetDatabase.FindAssets("t:BuildingCostDefinition", new string[1] { "Assets/_Game/Data/Buildings" });
		if (guids.Length != expectedCount)
		{
			throw new InvalidOperationException($"Expected {expectedCount} BuildingCostDefinition " + $"assets on disk, found {guids.Length}.");
		}
		string[] array = guids;
		for (int i = 0; i < array.Length; i++)
		{
			string path = AssetDatabase.GUIDToAssetPath(array[i]);
			BuildingCostDefinition buildingCostDefinition = AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>(path);
			if (buildingCostDefinition == null)
			{
				throw new InvalidOperationException("Building cost at '" + path + "' is missing on disk.");
			}
			if (!buildingCostDefinition.TryValidate(out var error))
			{
				throw new InvalidOperationException("Building cost at '" + path + "' is invalid on disk: " + error);
			}
		}
	}
}
}
