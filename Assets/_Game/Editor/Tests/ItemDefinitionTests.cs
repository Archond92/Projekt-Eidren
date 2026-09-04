using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class ItemDefinitionTests
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Func<ItemDefinition, string> _003C_003E9__2_0;

		public static Func<ItemDefinition, string> _003C_003E9__2_1;

		public static Func<PropertyInfo, bool> _003C_003E9__7_1;

		public static Func<string, bool> _003C_003E9__12_0;

		public static Func<string, bool> _003C_003E9__12_1;

		public static Func<string, bool> _003C_003E9__12_2;

		public static Func<string, bool> _003C_003E9__12_3;

		public static Func<string, bool> _003C_003E9__12_4;

		public static TestDelegate _003C_003E9__13_0;

		public static TestDelegate _003C_003E9__15_0;

		public static Func<FieldInfo, bool> _003C_003E9__20_0;

		public static Func<string, bool> _003C_003E9__22_0;

		public static Func<string, bool> _003C_003E9__22_1;

		public static Func<ItemDefinition, bool> _003C_003E9__23_0;

		internal string _003CV01Items_HaveUniqueStableIdsAndValidDefinitions_003Eb__2_0(ItemDefinition item)
		{
			return item.Id;
		}

		internal string _003CV01Items_HaveUniqueStableIdsAndValidDefinitions_003Eb__2_1(ItemDefinition item)
		{
			return item.Id;
		}

		internal bool _003CStorageAndBackpack_ShareTheSameStackLimit_003Eb__7_1(PropertyInfo property)
		{
			return property.Name.Contains("StackSize");
		}

		internal bool _003CItemDefinitionValidation_ChecksTierFamilyAndDurability_003Eb__12_0(string value)
		{
			return value.Contains("material family");
		}

		internal bool _003CItemDefinitionValidation_ChecksTierFamilyAndDurability_003Eb__12_1(string value)
		{
			return value.Contains("negative tier");
		}

		internal bool _003CItemDefinitionValidation_ChecksTierFamilyAndDurability_003Eb__12_2(string value)
		{
			return value.Contains("negative maximum durability");
		}

		internal bool _003CItemDefinitionValidation_ChecksTierFamilyAndDurability_003Eb__12_3(string value)
		{
			return value.Contains("unknown material family");
		}

		internal bool _003CItemDefinitionValidation_ChecksTierFamilyAndDurability_003Eb__12_4(string value)
		{
			if (!value.Contains("material family") && !value.Contains("negative tier"))
			{
				return value.Contains("negative maximum durability");
			}
			return true;
		}

		internal void _003CItemStack_CarriesDurabilityThroughEveryCopyPath_003Eb__13_0()
		{
			new ItemStack("wood", 1, string.Empty, -1);
		}

		internal void _003CItemStack_CannotBecomeNegative_003Eb__15_0()
		{
			new ItemStack("wood", -1);
		}

		internal bool _003CRuntimeStackChanges_DoNotModifyItemDefinitionAsset_003Eb__20_0(FieldInfo field)
		{
			return typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType);
		}

		internal bool _003CItemDefinitionValidation_ReportsRequiredProblems_003Eb__22_0(string value)
		{
			return value.Contains("no icon");
		}

		internal bool _003CItemDefinitionValidation_ReportsRequiredProblems_003Eb__22_1(string value)
		{
			return value.Contains("no use configuration");
		}

		internal bool _003CLoadAllItems_003Eb__23_0(ItemDefinition item)
		{
			return item != null;
		}
	}

	private const string ItemFolder = "Assets/_Game/Data/Items";

	private static readonly string[] ExpectedIds = new string[58]
	{
		"wood", "stone", "plant_fiber", "copper_ore", "copper_bar", "healing_potion", "buff_food", "berry", "wheat_seed", "axe",
		"scythe", "pickaxe", "plank", "rope", "stone_block", "wheat", "catch_device", "battery", "hammer", "daggers",
		"copper_hammer", "iron_hammer", "sealbreaker", "copper_daggers", "iron_daggers", "ash_fangs", "copper_spear", "iron_spear", "ember_thorn", "armor_wanderer_hood",
		"armor_wanderer_coat", "armor_wanderer_bracers", "armor_wanderer_legs", "hardwood", "hardwood_plank", "granite", "cut_granite", "swamp_hemp", "robust_cloth", "iron_ore",
		"iron_bar", "smithing_fitting", "smithing_mark", "blueprint_item_copper_spear", "copper_axe", "copper_scythe", "copper_pickaxe", "iron_axe", "iron_scythe", "iron_pickaxe",
		"armor_copper_helmet", "armor_copper_chest", "armor_copper_gloves", "armor_copper_legs", "armor_iron_helmet", "armor_iron_chest", "armor_iron_gloves", "armor_iron_legs"
	};

	[Test]
	public void V01Items_HaveUniqueStableIdsAndValidDefinitions()
	{
		ItemDefinition[] items = LoadAllItems();
		Assert.That<ItemDefinition[]>(items, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)ExpectedIds.Length));
		Assert.That<IEnumerable<string>>(items.Select((ItemDefinition itemDefinition) => itemDefinition.Id), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)ExpectedIds));
		Assert.That<int>(items.Select((ItemDefinition itemDefinition) => itemDefinition.Id).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)items.Length));
		ItemDefinition[] array = items;
		foreach (ItemDefinition item in array)
		{
			ItemDefinitionValidation validation = item.ValidateDefinition();
			Assert.That<string[]>(validation.Errors, (IResolveConstraint)(object)Is.Empty, item.name + ": " + string.Join("; ", validation.Errors), Array.Empty<object>());
			Assert.That<string[]>(validation.Warnings, (IResolveConstraint)(object)Is.Empty, item.name + ": " + string.Join("; ", validation.Warnings), Array.Empty<object>());
		}
	}

	[Test]
	public void V01Items_MatchTheMaterialMatrixFromM16()
	{
		AssertMaterial("Wood", "wood", 0);
		AssertMaterial("Stone", "stone", 0);
		AssertMaterial("PlantFiber", "fiber", 0);
		AssertMaterial("Plank", "wood", 1);
		AssertMaterial("StoneBlock", "stone", 1);
		AssertMaterial("Rope", "fiber", 1);
		AssertMaterial("CopperOre", "ore", 1);
		AssertMaterial("CopperBar", "ore", 1);
	}

	[Test]
	public void V01Items_CarryTheTiersFromM85()
	{
		AssertTier(0, "Berry", "WheatSeed", "Axe", "Scythe", "Pickaxe", "Hammer", "Daggers", "WandererHood", "WandererCoat", "WandererBracers", "WandererLegs");
		AssertTier(1, "Wheat", "CatchDevice", "Battery", "HealingPotion", "BuffFood");
	}

	[Test]
	public void FoodAndTools_CarryNoMaterialFamily()
	{
		string[] array = new string[14]
		{
			"Berry", "WheatSeed", "Wheat", "Axe", "Scythe", "Pickaxe", "Hammer", "Daggers", "CatchDevice", "Battery",
			"WandererHood", "WandererCoat", "WandererBracers", "WandererLegs"
		};
		foreach (string assetName in array)
		{
			Assert.That<string>(LoadItem(assetName).MaterialFamily, (IResolveConstraint)(object)Is.Empty, assetName, Array.Empty<object>());
		}
	}

	[Test]
	public void V01Items_UseTheStackSizesFromM5()
	{
		ItemDefinition[] array = LoadAllItems();
		foreach (ItemDefinition item in array)
		{
			Assert.That<int>(item.MaximumStackSize, (IResolveConstraint)(object)Is.InRange((IComparable)1, (IComparable)10), $"{item.name} stapelt {item.MaximumStackSize}.", Array.Empty<object>());
		}
		string[] array2 = new string[10] { "Axe", "Scythe", "Pickaxe", "Hammer", "Daggers", "CatchDevice", "WandererHood", "WandererCoat", "WandererBracers", "WandererLegs" };
		foreach (string assetName in array2)
		{
			Assert.That<int>(LoadItem(assetName).MaximumStackSize, (IResolveConstraint)(object)Is.EqualTo((object)1), assetName, Array.Empty<object>());
		}
		array2 = new string[14]
		{
			"Wood", "Stone", "PlantFiber", "CopperOre", "CopperBar", "Plank", "Rope", "StoneBlock", "Berry", "WheatSeed",
			"Wheat", "HealingPotion", "BuffFood", "Battery"
		};
		foreach (string assetName2 in array2)
		{
			Assert.That<int>(LoadItem(assetName2).MaximumStackSize, (IResolveConstraint)(object)Is.EqualTo((object)10), assetName2, Array.Empty<object>());
		}
	}

	[Test]
	public void StorageAndBackpack_ShareTheSameStackLimit()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		ItemDefinition wood = LoadItem("Wood");
		Assert.Throws<ArgumentOutOfRangeException>((TestDelegate)delegate
		{
			ItemStack.Create(wood, wood.MaximumStackSize + 1);
		});
		Assert.That<bool>(typeof(StorageContainerState).GetProperties().Any((PropertyInfo property) => property.Name.Contains("StackSize")), (IResolveConstraint)(object)Is.False, "Die Kiste darf keine eigene Stapelgrenze tragen.", Array.Empty<object>());
	}

	[Test]
	public void V01Armor_DeclaresItsVisibleWearableSlot()
	{
		Assert.That<WearableSlot>(LoadItem("WandererHood").WearableSlot, (IResolveConstraint)(object)Is.EqualTo((object)WearableSlot.Head));
		Assert.That<WearableSlot>(LoadItem("WandererCoat").WearableSlot, (IResolveConstraint)(object)Is.EqualTo((object)WearableSlot.Chest));
		Assert.That<WearableSlot>(LoadItem("WandererBracers").WearableSlot, (IResolveConstraint)(object)Is.EqualTo((object)WearableSlot.Hands));
		Assert.That<WearableSlot>(LoadItem("WandererLegs").WearableSlot, (IResolveConstraint)(object)Is.EqualTo((object)WearableSlot.Legs));
	}

	[Test]
	public void V02DurableItems_CarryTheirMaximumValues()
	{
		Assert.That<int>(LoadItem("Hammer").MaximumDurability, (IResolveConstraint)(object)Is.EqualTo((object)120));
		Assert.That<int>(LoadItem("Daggers").MaximumDurability, (IResolveConstraint)(object)Is.EqualTo((object)220));
		Assert.That<int>(LoadItem("Axe").MaximumDurability, (IResolveConstraint)(object)Is.EqualTo((object)100));
		Assert.That<int>(LoadItem("Scythe").MaximumDurability, (IResolveConstraint)(object)Is.EqualTo((object)100));
		Assert.That<int>(LoadItem("Pickaxe").MaximumDurability, (IResolveConstraint)(object)Is.EqualTo((object)100));
		Assert.That<ItemDefinition[]>(new ItemDefinition[4]
		{
			LoadItem("WandererHood"),
			LoadItem("WandererCoat"),
			LoadItem("WandererBracers"),
			LoadItem("WandererLegs")
		}, (IResolveConstraint)(object)((ConstraintExpression)Has.All.Property("MaximumDurability")).EqualTo((object)80));
	}

	[Test]
	public void BuffFood_KeepsItsIdAndBecomesBread()
	{
		ItemDefinition itemDefinition = LoadItem("BuffFood");
		Assert.That<string>(itemDefinition.Id, (IResolveConstraint)(object)Is.EqualTo((object)"buff_food"));
		Assert.That<string>(itemDefinition.DisplayName, (IResolveConstraint)(object)Is.EqualTo((object)"Brot"));
	}

	[Test]
	public void Berry_HealsLessThanTheHealingPotion()
	{
		ItemDefinition itemDefinition = LoadItem("Berry");
		ItemDefinition potion = LoadItem("HealingPotion");
		Assert.That<bool>(itemDefinition.CanBeUsed, (IResolveConstraint)(object)Is.True);
		Assert.That<ItemUseActionType>(itemDefinition.UseConfiguration.ActionType, (IResolveConstraint)(object)Is.EqualTo((object)ItemUseActionType.Heal));
		Assert.That<float>(itemDefinition.UseConfiguration.HealingAmount, (IResolveConstraint)(object)Is.LessThan((object)potion.UseConfiguration.HealingAmount));
	}

	[Test]
	public void ItemDefinitionValidation_ChecksTierFamilyAndDurability()
	{
		ItemDefinition invalid = ScriptableObject.CreateInstance<ItemDefinition>();
		try
		{
			SetDefinition(invalid, "invalid_material", "Ohne Familie", ItemCategory.Material, 1, canBeUsed: false);
			SetMaterialFields(invalid, string.Empty, -1, -5);
			string[] errors = invalid.ValidateDefinition().Errors;
			Assert.That<bool>(errors.Any((string value) => value.Contains("material family")), (IResolveConstraint)(object)Is.True, string.Join("; ", errors), Array.Empty<object>());
			Assert.That<bool>(errors.Any((string value) => value.Contains("negative tier")), (IResolveConstraint)(object)Is.True, string.Join("; ", errors), Array.Empty<object>());
			Assert.That<bool>(errors.Any((string value) => value.Contains("negative maximum durability")), (IResolveConstraint)(object)Is.True, string.Join("; ", errors), Array.Empty<object>());
			SetMaterialFields(invalid, "granite", 0, 0);
			errors = invalid.ValidateDefinition().Errors;
			Assert.That<bool>(errors.Any((string value) => value.Contains("unknown material family")), (IResolveConstraint)(object)Is.True, string.Join("; ", errors), Array.Empty<object>());
			SetMaterialFields(invalid, "wood", 0, 0);
			errors = invalid.ValidateDefinition().Errors;
			Assert.That<bool>(errors.Any((string value) => value.Contains("material family") || value.Contains("negative tier") || value.Contains("negative maximum durability")), (IResolveConstraint)(object)Is.False, string.Join("; ", errors), Array.Empty<object>());
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(invalid);
		}
	}

	[Test]
	public void ItemStack_CarriesDurabilityThroughEveryCopyPath()
	{
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		ItemDefinition wood = LoadItem("Wood");
		ItemStack stack = ItemStack.Create(wood, 2, "instance-1", 37);
		Assert.That<int>(stack.Durability, (IResolveConstraint)(object)Is.EqualTo((object)37));
		Assert.That<int>(stack.Copy().Durability, (IResolveConstraint)(object)Is.EqualTo((object)37));
		Assert.That<int>(stack.WithQuantity(wood, 5).Durability, (IResolveConstraint)(object)Is.EqualTo((object)37));
		stack.TryAdd(wood, 3, out var added, out var remainder);
		Assert.That<int>(added.Durability, (IResolveConstraint)(object)Is.EqualTo((object)37));
		ItemStack.Empty.TryAdd(wood, 3, out var fresh, out remainder);
		Assert.That<int>(fresh.Durability, (IResolveConstraint)(object)Is.Zero);
		object obj = _003C_003Ec._003C_003E9__13_0;
		if (obj == null)
		{
			TestDelegate val = delegate
			{
				new ItemStack("wood", 1, string.Empty, -1);
			};
			_003C_003Ec._003C_003E9__13_0 = val;
			obj = (object)val;
		}
		Assert.Throws<ArgumentOutOfRangeException>((TestDelegate)obj);
	}

	[Test]
	public void FreshDurableItem_StartsFullAndHasPersistentIdentity()
	{
		ItemDefinition definition = LoadItem("Hammer");
		ItemStack first = ItemStack.Create(definition, 1);
		ItemStack second = ItemStack.Create(definition, 1);
		Assert.That<int>(first.Durability, (IResolveConstraint)(object)Is.EqualTo((object)120));
		Assert.That<string>(first.InstanceId, (IResolveConstraint)(object)Is.Not.Empty);
		Assert.That<string>(second.InstanceId, (IResolveConstraint)(object)Is.Not.EqualTo((object)first.InstanceId));
		Assert.That<string>(first.Copy().InstanceId, (IResolveConstraint)(object)Is.EqualTo((object)first.InstanceId));
	}

	[Test]
	public void ItemStack_CannotBecomeNegative()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		object obj = _003C_003Ec._003C_003E9__15_0;
		if (obj == null)
		{
			TestDelegate val = delegate
			{
				new ItemStack("wood", -1);
			};
			_003C_003Ec._003C_003E9__15_0 = val;
			obj = (object)val;
		}
		Assert.Throws<ArgumentOutOfRangeException>((TestDelegate)obj);
		ItemStack empty = ItemStack.Empty;
		Assert.That<bool>(empty.IsEmpty, (IResolveConstraint)(object)Is.True);
		Assert.That<string>(empty.ItemId, (IResolveConstraint)(object)Is.Empty);
		Assert.That<int>(empty.Quantity, (IResolveConstraint)(object)Is.Zero);
		Assert.That<string>(empty.InstanceId, (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void ItemStack_RespectsDefinitionStackLimit()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		ItemDefinition wood = LoadItem("Wood");
		Assert.Throws<ArgumentOutOfRangeException>((TestDelegate)delegate
		{
			ItemStack.Create(wood, wood.MaximumStackSize + 1);
		});
		ItemStack original = ItemStack.Create(wood, wood.MaximumStackSize - 1);
		Assert.That<bool>(original.TryAdd(wood, 4, out var updated, out var remainder), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(updated.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)wood.MaximumStackSize));
		Assert.That<int>(remainder, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(original.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)(wood.MaximumStackSize - 1)));
	}

	[Test]
	public void ContentDatabase_ResolvesEveryV01ItemByStableId()
	{
		GameObject root = new GameObject("ItemDatabase_Test");
		try
		{
			ContentDatabase database = root.AddComponent<ContentDatabase>();
			string[] expectedIds = ExpectedIds;
			foreach (string id in expectedIds)
			{
				ItemDefinition item = database.GetItem(id);
				Assert.That<ItemDefinition>(item, (IResolveConstraint)(object)Is.Not.Null);
				Assert.That<string>(item.Id, (IResolveConstraint)(object)Is.EqualTo((object)id));
				Assert.That<bool>(database.TryGetItem(id, out var found), (IResolveConstraint)(object)Is.True);
				Assert.That<ItemDefinition>(found, (IResolveConstraint)(object)Is.SameAs((object)item));
			}
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void ContentDatabase_UnknownItemIdIsControlledError()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		GameObject root = new GameObject("UnknownItem_Test");
		try
		{
			ContentDatabase database = root.AddComponent<ContentDatabase>();
			KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>((TestDelegate)delegate
			{
				database.GetItem("missing.item");
			});
			StringAssert.Contains("ItemDefinition with ID 'missing.item' is not registered", exception.Message);
			Assert.That<bool>(database.TryGetItem("missing.item", out var missing), (IResolveConstraint)(object)Is.False);
			Assert.That<ItemDefinition>(missing, (IResolveConstraint)(object)Is.Null);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void ContentDatabase_RejectsDuplicateItemIds()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		GameObject root = new GameObject("DuplicateItems_Test");
		ItemDefinition first = ScriptableObject.CreateInstance<ItemDefinition>();
		ItemDefinition second = ScriptableObject.CreateInstance<ItemDefinition>();
		try
		{
			SetDefinition(first, "duplicate", "First", ItemCategory.Resource, 1, canBeUsed: false);
			SetDefinition(second, "duplicate", "Second", ItemCategory.Material, 1, canBeUsed: false);
			ContentDatabase database = root.AddComponent<ContentDatabase>();
			InvalidOperationException exception = Assert.Throws<InvalidOperationException>((TestDelegate)delegate
			{
				database.ConfigureItems(new ItemDefinition[2] { first, second });
			});
			StringAssert.Contains("Duplicate ItemDefinition ID 'duplicate'", exception.Message);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
			UnityEngine.Object.DestroyImmediate(first);
			UnityEngine.Object.DestroyImmediate(second);
		}
	}

	[Test]
	public void RuntimeStackChanges_DoNotModifyItemDefinitionAsset()
	{
		ItemDefinition wood = LoadItem("Wood");
		string before = EditorJsonUtility.ToJson(wood);
		int full = wood.MaximumStackSize;
		ItemStack stack = ItemStack.Create(wood, 4);
		ItemStack copy = stack.Copy().WithQuantity(wood, full);
		Assert.That<int>(stack.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)4));
		Assert.That<int>(copy.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)full));
		Assert.That<string>(EditorJsonUtility.ToJson(wood), (IResolveConstraint)(object)Is.EqualTo((object)before));
		Assert.That<bool>(typeof(ItemStack).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Any((FieldInfo field) => typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)), (IResolveConstraint)(object)Is.False, "ItemStack must persist IDs and primitive values only.", Array.Empty<object>());
	}

	[Test]
	public void ConsumableDefinitions_PreservePrototypeValues()
	{
		ItemDefinition itemDefinition = LoadItem("HealingPotion");
		Assert.That<ItemCategory>(itemDefinition.Category, (IResolveConstraint)(object)Is.EqualTo((object)ItemCategory.Consumable));
		Assert.That<int>(itemDefinition.MaximumStackSize, (IResolveConstraint)(object)Is.EqualTo((object)10));
		Assert.That<bool>(itemDefinition.CanBeUsed, (IResolveConstraint)(object)Is.True);
		Assert.That<ItemUseActionType>(itemDefinition.UseConfiguration.ActionType, (IResolveConstraint)(object)Is.EqualTo((object)ItemUseActionType.Heal));
		Assert.That<float>(itemDefinition.UseConfiguration.HealingAmount, (IResolveConstraint)(object)Is.EqualTo((object)55f));
		ItemDefinition itemDefinition2 = LoadItem("BuffFood");
		Assert.That<ItemCategory>(itemDefinition2.Category, (IResolveConstraint)(object)Is.EqualTo((object)ItemCategory.Consumable));
		Assert.That<int>(itemDefinition2.MaximumStackSize, (IResolveConstraint)(object)Is.EqualTo((object)10));
		Assert.That<bool>(itemDefinition2.CanBeUsed, (IResolveConstraint)(object)Is.True);
		Assert.That<ItemUseActionType>(itemDefinition2.UseConfiguration.ActionType, (IResolveConstraint)(object)Is.EqualTo((object)ItemUseActionType.TimedCombatBuff));
		// F31-019: Buff-Dauer auf 2 Minuten angehoben (Nutzerentscheid 17.08.2026).
		Assert.That<float>(itemDefinition2.UseConfiguration.Duration, (IResolveConstraint)(object)Is.EqualTo((object)120f));
		Assert.That<float>(itemDefinition2.UseConfiguration.DamageMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)1.2f));
		Assert.That<float>(itemDefinition2.UseConfiguration.StaggerDamageMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)1.15f));
	}

	[Test]
	public void ItemDefinitionValidation_ReportsRequiredProblems()
	{
		ItemDefinition invalid = ScriptableObject.CreateInstance<ItemDefinition>();
		try
		{
			SetDefinition(invalid, string.Empty, string.Empty, (ItemCategory)999, 0, canBeUsed: true);
			ItemDefinitionValidation validation = invalid.ValidateDefinition();
			Assert.That<bool>(validation.IsValid, (IResolveConstraint)(object)Is.False);
			Assert.That<string[]>(validation.Errors, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).GreaterThanOrEqualTo((object)5));
			Assert.That<bool>(validation.Warnings.Any((string value) => value.Contains("no icon")), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(validation.Errors.Any((string value) => value.Contains("no use configuration")), (IResolveConstraint)(object)Is.True);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(invalid);
		}
	}

	private static ItemDefinition[] LoadAllItems()
	{
		return (from item in AssetDatabase.FindAssets("t:ItemDefinition", new string[1] { "Assets/_Game/Data/Items" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ItemDefinition>)
			where item != null
			select item).ToArray();
	}

	private static ItemDefinition LoadItem(string assetName)
	{
		ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/" + assetName + ".asset");
		Assert.That<ItemDefinition>(itemDefinition, (IResolveConstraint)(object)Is.Not.Null, "Missing ItemDefinition asset '" + assetName + "'.", Array.Empty<object>());
		return itemDefinition;
	}

	private static void AssertMaterial(string assetName, string materialFamily, int tier)
	{
		ItemDefinition itemDefinition = LoadItem(assetName);
		Assert.That<string>(itemDefinition.MaterialFamily, (IResolveConstraint)(object)Is.EqualTo((object)materialFamily), assetName, Array.Empty<object>());
		Assert.That<int>(itemDefinition.Tier, (IResolveConstraint)(object)Is.EqualTo((object)tier), assetName, Array.Empty<object>());
	}

	private static void AssertTier(int tier, params string[] assetNames)
	{
		foreach (string assetName in assetNames)
		{
			Assert.That<int>(LoadItem(assetName).Tier, (IResolveConstraint)(object)Is.EqualTo((object)tier), assetName, Array.Empty<object>());
		}
	}

	private static void SetMaterialFields(ItemDefinition item, string materialFamily, int tier, int maximumDurability)
	{
		SerializedObject serializedObject = new SerializedObject(item);
		serializedObject.FindProperty("materialFamily").stringValue = materialFamily;
		serializedObject.FindProperty("tier").intValue = tier;
		serializedObject.FindProperty("maximumDurability").intValue = maximumDurability;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
	}

	private static void SetDefinition(ItemDefinition item, string id, string displayName, ItemCategory category, int maximumStackSize, bool canBeUsed)
	{
		SerializedObject serializedObject = new SerializedObject(item);
		serializedObject.FindProperty("id").stringValue = id;
		serializedObject.FindProperty("displayName").stringValue = displayName;
		serializedObject.FindProperty("category").intValue = (int)category;
		serializedObject.FindProperty("maximumStackSize").intValue = maximumStackSize;
		serializedObject.FindProperty("canBeUsed").boolValue = canBeUsed;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
	}
}
}
