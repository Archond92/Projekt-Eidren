using Eidren.Data;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class CraftingRecipeTable
{
	private const string ItemIconFolder = "Assets/_Game/Art/Items";

	[MenuItem("Eidren/Crafting/Build Recipes V0.1")]
	public static void BuildRecipesOnly()
	{
		ItemContentAssetBuilder.BuildItemDefinitions();
		BuildRecipes();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		VerifyOnDisk();
	}

	internal static CraftingRecipeDefinition[] BuildRecipes()
	{
		CraftingRecipeDefinition[] obj = new CraftingRecipeDefinition[41]
		{
			Recipe("Recipe_Axe", "craft_axe", "Axt", "Holzfällerwerkzeug aus Holz, Stein und Faser.", "axe", 1, new(string, int)[3]
			{
				("wood", 3),
				("stone", 2),
				("plant_fiber", 2)
			}, CraftingStationType.None, "ITEM_TMP_Axe", 10),
			Recipe("Recipe_Scythe", "craft_scythe", "Sense", "Schneidwerkzeug für Fasern.", "scythe", 1, new(string, int)[3]
			{
				("wood", 2),
				("stone", 2),
				("plant_fiber", 3)
			}, CraftingStationType.None, "ITEM_TMP_Scythe", 11),
			Recipe("Recipe_Pickaxe", "craft_pickaxe", "Spitzhacke", "Öffnet den Kupferabbau und erhöht den Steinertrag.", "pickaxe", 1, new(string, int)[3]
			{
				("wood", 3),
				("stone", 4),
				("plant_fiber", 2)
			}, CraftingStationType.None, "ITEM_TMP_Pickaxe", 12),
			Recipe("Recipe_Hammer", "craft_hammer", "Hammer", "Schwere Stagger-Waffe aus Holz, Stein und Faser.", "hammer", 1, new(string, int)[3]
			{
				("wood", 4),
				("stone", 3),
				("plant_fiber", 3)
			}, CraftingStationType.Workbench, "ITEM_TMP_Hammer", 13),
			Recipe("Recipe_Daggers", "craft_daggers", "Dolche", "Schnelle Klingen aus Holz, Stein und Faser.", "daggers", 1, new(string, int)[3]
			{
				("wood", 2),
				("stone", 5),
				("plant_fiber", 3)
			}, CraftingStationType.Workbench, "ITEM_TMP_Daggers", 14),
			Recipe("Recipe_Plank", "craft_plank", "Brett", "Schneidet Weichholz zu einem Brett.", "plank", 1, new(string, int)[1] { ("wood", 2) }, CraftingStationType.Sawmill, "ITEM_TMP_Plank", 20),
			Recipe("Recipe_Rope", "craft_rope", "Seil", "Dreht Pflanzenfasern zu einem Seil.", "rope", 1, new(string, int)[1] { ("plant_fiber", 2) }, CraftingStationType.Ropewalk, "ITEM_TMP_Rope", 21),
			Recipe("Recipe_StoneBlock", "craft_stone_block", "Steinblock", "Behaut Bruchstein zu einem Block.", "stone_block", 1, new(string, int)[1] { ("stone", 2) }, CraftingStationType.Stonecutter, "ITEM_TMP_StoneBlock", 22),
			Recipe("Recipe_CopperBar", "craft_copper_bar", "Kupferbarren", "Verhüttet Kupfererz zu einem brauchbaren Barren.", "copper_bar", 1, new(string, int)[1] { ("copper_ore", 3) }, CraftingStationType.Smelter, "ITEM_TMP_CopperBar", 23),
			Recipe("Recipe_CatchDevice", "craft_catch_device", "Fanggerät", "Permanentes Gerät zum Fangen von Eidra.", "catch_device", 1, new(string, int)[3]
			{
				("plank", 2),
				("rope", 2),
				("copper_bar", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_CatchDevice", 30),
			Recipe("Recipe_Battery", "craft_battery", "Batterie", "Kupferspule als Ladung für das Fanggerät.", "battery", 1, new(string, int)[2]
			{
				("rope", 2),
				("copper_bar", 1)
			}, CraftingStationType.Workbench, "ITEM_TMP_Battery", 31),
			Recipe("Recipe_WandererHood", "craft_wanderer_hood", "Stoffkapuze", "Fertigt die sichtbare Kopfrüstung.", "armor_wanderer_hood", 1, new(string, int)[2]
			{
				("rope", 2),
				("plant_fiber", 2)
			}, CraftingStationType.Workbench, "ITEM_WandererHood", 32),
			Recipe("Recipe_WandererCoat", "craft_wanderer_coat", "Stoffmantel", "Fertigt einen langen Mantel aus Pflanzenfaser.", "armor_wanderer_coat", 1, new(string, int)[2]
			{
				("rope", 4),
				("plant_fiber", 4)
			}, CraftingStationType.Workbench, "ITEM_WandererCoat", 33),
			Recipe("Recipe_WandererBracers", "craft_wanderer_bracers", "Stoffarmschienen", "Fertigt Handschuhe und Armschienen.", "armor_wanderer_bracers", 1, new(string, int)[2]
			{
				("rope", 2),
				("plant_fiber", 2)
			}, CraftingStationType.Workbench, "ITEM_WandererBracers", 34),
			Recipe("Recipe_WandererLegs", "craft_wanderer_legs", "Stoffschuhe", "Fertigt leichte Schuhe aus Faser und Seil.", "armor_wanderer_legs", 1, new(string, int)[2]
			{
				("rope", 3),
				("plant_fiber", 2)
			}, CraftingStationType.Workbench, "ITEM_WandererLegs", 35),
			Recipe("Recipe_HealingPotion", "craft_healing_potion", "Heiltrank", "Der Barren ist die Phiole, die Beeren sind der Inhalt.", "healing_potion", 1, new(string, int)[2]
			{
				("copper_bar", 1),
				("berry", 3)
			}, CraftingStationType.CookingPot, "ITEM_TMP_HealingPotion", 40),
			Recipe("Recipe_Bread", "craft_bread", "Brot", "Backt Weizen der Ackerernte zu zwei Broten.", "buff_food", 2, new(string, int)[1] { ("wheat", 6) }, CraftingStationType.CookingPot, "ITEM_TMP_BuffFood", 41),
			Recipe("Recipe_HardwoodPlank", "craft_hardwood_plank", "Hartholzbrett", "Schneidet Hartholz zu einem Brett.", "hardwood_plank", 1, new(string, int)[1] { ("hardwood", 2) }, CraftingStationType.Sawmill, "ITEM_TMP_HardwoodPlank", 50),
			Recipe("Recipe_CutGranite", "craft_cut_granite", "Behauener Granit", "Behaut Granit zu einem Werkstück.", "cut_granite", 1, new(string, int)[1] { ("granite", 2) }, CraftingStationType.Stonecutter, "ITEM_TMP_CutGranite", 51),
			Recipe("Recipe_RobustCloth", "craft_robust_cloth", "Robustes Tuch", "Verwebt Sumpfhanf mit einem Seil.", "robust_cloth", 1, new(string, int)[2]
			{
				("swamp_hemp", 2),
				("rope", 1)
			}, CraftingStationType.Ropewalk, "ITEM_TMP_RobustCloth", 52),
			Recipe("Recipe_IronBar", "craft_iron_bar", "Eisenbarren", "Verhüttet drei Einheiten Eisenerz.", "iron_bar", 1, new(string, int)[1] { ("iron_ore", 3) }, CraftingStationType.Smelter, "ITEM_TMP_IronBar", 53),
			Recipe("Recipe_CopperHammer", "craft_copper_hammer", "Kupferhammer", "T1-Hammer mit Wucht-Signatur.", "copper_hammer", 1, new(string, int)[4]
			{
				("copper_bar", 3),
				("plank", 2),
				("stone_block", 1),
				("rope", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_CopperHammer", 60),
			Recipe("Recipe_CopperDaggers", "craft_copper_daggers", "Kupferdolche", "T1-Dolche mit Hinterhalt-Signatur.", "copper_daggers", 1, new(string, int)[3]
			{
				("copper_bar", 3),
				("plank", 1),
				("rope", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_CopperDaggers", 61),
			Recipe("Recipe_CopperSpear", "craft_copper_spear", "Kupferspeer", "T1-Speer mit Distanzfenster.", "copper_spear", 1, new(string, int)[3]
			{
				("copper_bar", 3),
				("plank", 2),
				("rope", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_CopperSpear", 62),
			Recipe("Recipe_CopperHelmet", "craft_copper_helmet", "Kupferhelm", "T1-Kopfschutz.", "armor_copper_helmet", 1, new(string, int)[2]
			{
				("copper_bar", 1),
				("rope", 1)
			}, CraftingStationType.Workbench, "ITEM_TMP_CopperHelmet", 63),
			Recipe("Recipe_CopperChest", "craft_copper_chest", "Kupferharnisch", "T1-Brustschutz.", "armor_copper_chest", 1, new(string, int)[2]
			{
				("copper_bar", 3),
				("rope", 3)
			}, CraftingStationType.Workbench, "ITEM_TMP_CopperChest", 64),
			Recipe("Recipe_CopperGloves", "craft_copper_gloves", "Kupferhandschuhe", "T1-Handschutz.", "armor_copper_gloves", 1, new(string, int)[2]
			{
				("copper_bar", 1),
				("rope", 1)
			}, CraftingStationType.Workbench, "ITEM_TMP_CopperGloves", 65),
			Recipe("Recipe_CopperLegs", "craft_copper_legs", "Kupferbeinschutz", "T1-Beinschutz.", "armor_copper_legs", 1, new(string, int)[2]
			{
				("copper_bar", 3),
				("rope", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_CopperLegs", 66),
			Recipe("Recipe_CopperAxe", "craft_copper_axe", "Kupferaxt", "T1-Holzfällerwerkzeug.", "copper_axe", 1, new(string, int)[3]
			{
				("copper_bar", 2),
				("plank", 2),
				("rope", 1)
			}, CraftingStationType.Workbench, "ITEM_TMP_CopperAxe", 67),
			Recipe("Recipe_CopperPickaxe", "craft_copper_pickaxe", "Kupferspitzhacke", "T1-Bergbauwerkzeug.", "copper_pickaxe", 1, new(string, int)[3]
			{
				("copper_bar", 2),
				("plank", 2),
				("stone_block", 1)
			}, CraftingStationType.Workbench, "ITEM_TMP_CopperPickaxe", 68),
			Recipe("Recipe_CopperScythe", "craft_copper_scythe", "Kupfersense", "T1-Faserwerkzeug.", "copper_scythe", 1, new(string, int)[3]
			{
				("copper_bar", 2),
				("plank", 1),
				("rope", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_CopperScythe", 69),
			Recipe("Recipe_IronHammer", "craft_iron_hammer", "Eisenhammer", "T2-Hammer mit Wucht-Signatur.", "iron_hammer", 1, new(string, int)[5]
			{
				("iron_bar", 4),
				("hardwood_plank", 2),
				("cut_granite", 2),
				("rope", 2),
				("smithing_fitting", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_IronHammer", 80),
			Recipe("Recipe_IronDaggers", "craft_iron_daggers", "Eisendolche", "T2-Dolche mit Hinterhalt-Signatur.", "iron_daggers", 1, new(string, int)[4]
			{
				("iron_bar", 4),
				("hardwood_plank", 1),
				("robust_cloth", 2),
				("smithing_fitting", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_IronDaggers", 81),
			Recipe("Recipe_IronSpear", "craft_iron_spear", "Eisenspeer", "T2-Speer mit Distanzfenster.", "iron_spear", 1, new(string, int)[5]
			{
				("iron_bar", 3),
				("hardwood_plank", 3),
				("robust_cloth", 2),
				("rope", 2),
				("smithing_fitting", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_IronSpear", 82),
			Recipe("Recipe_IronHelmet", "craft_iron_helmet", "Eisenhelm", "T2-Kopfschutz.", "armor_iron_helmet", 1, new(string, int)[3]
			{
				("iron_bar", 2),
				("robust_cloth", 1),
				("smithing_fitting", 1)
			}, CraftingStationType.Workbench, "ITEM_TMP_IronHelmet", 83),
			Recipe("Recipe_IronChest", "craft_iron_chest", "Eisenharnisch", "T2-Brustschutz.", "armor_iron_chest", 1, new(string, int)[3]
			{
				("iron_bar", 5),
				("robust_cloth", 3),
				("smithing_fitting", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_IronChest", 84),
			Recipe("Recipe_IronGloves", "craft_iron_gloves", "Eisenhandschuhe", "T2-Handschutz.", "armor_iron_gloves", 1, new(string, int)[3]
			{
				("iron_bar", 2),
				("robust_cloth", 1),
				("smithing_fitting", 1)
			}, CraftingStationType.Workbench, "ITEM_TMP_IronGloves", 85),
			Recipe("Recipe_IronLegs", "craft_iron_legs", "Eisenbeinschutz", "T2-Beinschutz.", "armor_iron_legs", 1, new(string, int)[3]
			{
				("iron_bar", 4),
				("robust_cloth", 2),
				("smithing_fitting", 2)
			}, CraftingStationType.Workbench, "ITEM_TMP_IronLegs", 86),
			Recipe("Recipe_IronAxe", "craft_iron_axe", "Eisenaxt", "T2-Holzfällerwerkzeug.", "iron_axe", 1, new(string, int)[5]
			{
				("iron_bar", 3),
				("hardwood_plank", 2),
				("robust_cloth", 1),
				("rope", 1),
				("smithing_fitting", 1)
			}, CraftingStationType.Workbench, "ITEM_TMP_IronAxe", 87),
			Recipe("Recipe_IronPickaxe", "craft_iron_pickaxe", "Eisenspitzhacke", "T2-Bergbauwerkzeug.", "iron_pickaxe", 1, new(string, int)[5]
			{
				("iron_bar", 3),
				("hardwood_plank", 2),
				("cut_granite", 2),
				("rope", 1),
				("smithing_fitting", 1)
			}, CraftingStationType.Workbench, "ITEM_TMP_IronPickaxe", 88),
			Recipe("Recipe_IronScythe", "craft_iron_scythe", "Eisensense", "T2-Faserwerkzeug.", "iron_scythe", 1, new(string, int)[5]
			{
				("iron_bar", 3),
				("hardwood_plank", 2),
				("robust_cloth", 1),
				("rope", 1),
				("smithing_fitting", 1)
			}, CraftingStationType.Workbench, "ITEM_TMP_IronScythe", 89)
		};
		EnsureCatalog(obj);
		return obj;
	}

	private static CraftingRecipeDefinition Recipe(string assetName, string id, string displayName, string description, string resultItemId, int resultAmount, (string id, int amount)[] ingredients, CraftingStationType station, string iconItemAssetName, int sortOrder)
	{
		return EnsureRecipe(assetName, id, displayName, description, CraftingResultType.Item, resultItemId, resultAmount, ingredients, station, iconItemAssetName, sortOrder);
	}

	private static CraftingRecipeDefinition EnsureRecipe(string assetName, string id, string displayName, string description, CraftingResultType resultType, string resultItemId, int resultAmount, (string id, int amount)[] ingredients, CraftingStationType station, string iconItemAssetName, int sortOrder)
	{
		if (ingredients.Length > 5)
		{
			throw new InvalidOperationException($"Recipe '{id}' has {ingredients.Length} ingredients; " + $"{5} is the " + "central v0.2 limit.");
		}
		string path = "Assets/_Game/Data/Crafting/" + assetName + ".asset";
		CraftingRecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeDefinition>(path);
		if (recipe == null)
		{
			recipe = ScriptableObject.CreateInstance<CraftingRecipeDefinition>();
			AssetDatabase.CreateAsset(recipe, path);
		}
		SerializedObject serialized = new SerializedObject(recipe);
		serialized.FindProperty("id").stringValue = id;
		serialized.FindProperty("displayName").stringValue = displayName;
		serialized.FindProperty("description").stringValue = description;
		serialized.FindProperty("resultType").enumValueIndex = (int)resultType;
		serialized.FindProperty("resultItemId").stringValue = resultItemId;
		serialized.FindProperty("resultAmount").intValue = resultAmount;
		serialized.FindProperty("requiredStation").enumValueIndex = (int)station;
		// Icon-Namen WIRKLICH durchreichen (F31-002): das fest angehaengte
			// "ITEM_TMP_"-Praefix machte die fertigen ITEM_Wanderer*-Icons
			// unerreichbar — dieselbe Falle wie im Material-Helfer der
			// ItemContentTable.
			serialized.FindProperty("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/" + iconItemAssetName + ".png");
		serialized.FindProperty("sortOrder").intValue = sortOrder;
		SerializedProperty ingredientProperty = serialized.FindProperty("ingredients");
		ingredientProperty.arraySize = ingredients.Length;
		for (int index = 0; index < ingredients.Length; index++)
		{
			SerializedProperty arrayElementAtIndex = ingredientProperty.GetArrayElementAtIndex(index);
			arrayElementAtIndex.FindPropertyRelative("itemId").stringValue = ingredients[index].id;
			arrayElementAtIndex.FindPropertyRelative("amount").intValue = ingredients[index].amount;
		}
		serialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(recipe);
		if (!recipe.TryValidate(out var error))
		{
			throw new InvalidOperationException("Invalid crafting recipe '" + id + "': " + error);
		}
		return recipe;
	}

	private static void EnsureCatalog(CraftingRecipeDefinition[] recipes)
	{
		CraftingRecipeCatalogDefinition catalog = AssetDatabase.LoadAssetAtPath<CraftingRecipeCatalogDefinition>("Assets/_Game/Resources/Data/CraftingRecipeCatalog_V01.asset");
		if (catalog == null)
		{
			catalog = ScriptableObject.CreateInstance<CraftingRecipeCatalogDefinition>();
			AssetDatabase.CreateAsset(catalog, "Assets/_Game/Resources/Data/CraftingRecipeCatalog_V01.asset");
		}
		SerializedObject serialized = new SerializedObject(catalog);
		SerializedProperty values = serialized.FindProperty("recipes");
		values.arraySize = recipes.Length;
		for (int index = 0; index < recipes.Length; index++)
		{
			values.GetArrayElementAtIndex(index).objectReferenceValue = recipes[index];
		}
		serialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(catalog);
	}

	private static void VerifyOnDisk()
	{
		CraftingRecipeCatalogDefinition catalog = AssetDatabase.LoadAssetAtPath<CraftingRecipeCatalogDefinition>("Assets/_Game/Resources/Data/CraftingRecipeCatalog_V01.asset");
		if (catalog == null)
		{
			throw new InvalidOperationException("Crafting recipe catalog is missing after saving.");
		}
		foreach (CraftingRecipeDefinition recipe in catalog.Recipes)
		{
			if (recipe == null || !recipe.TryValidate(out var _))
			{
				throw new InvalidOperationException("Crafting catalog holds an invalid recipe on disk: " + ((recipe == null) ? "null" : recipe.Id));
			}
		}
		Debug.Log($"Eidren: verified {catalog.Recipes.Count} V0.1 crafting " + "recipes on disk.");
	}
}
}
