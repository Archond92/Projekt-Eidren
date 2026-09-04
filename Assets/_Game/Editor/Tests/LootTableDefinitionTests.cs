using Eidren.Data;
using Eidren.Interaction;
using Eidren.Loot;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class LootTableDefinitionTests
{
	private const string LootPath = "Assets/_Game/Data/Loot/WildlingLoot.asset";

	[Test]
	public void WildlingLootAsset_HasRequiredDataDrivenEntries()
	{
		LootTableDefinition lootTableDefinition = AssetDatabase.LoadAssetAtPath<LootTableDefinition>("Assets/_Game/Data/Loot/WildlingLoot.asset");
		Assert.That<LootTableDefinition>(lootTableDefinition, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<string>(lootTableDefinition.Id, (IResolveConstraint)(object)Is.EqualTo((object)"loot.wildling.v01"));
		Assert.That<string[]>(lootTableDefinition.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty);
		AssertEntry(lootTableDefinition, "plant_fiber", 1f, 1, 2, guaranteed: true);
		AssertEntry(lootTableDefinition, "healing_potion", 0.15f, 1, 1, guaranteed: false);
		AssertEntry(lootTableDefinition, "buff_food", 0.08f, 1, 1, guaranteed: false);
		AssertEntry(lootTableDefinition, "wood", 0.2f, 1, 2, guaranteed: false);
	}

	[Test]
	public void GuaranteedDropAppearsAndAmountStaysInRange()
	{
		LootTableDefinition table = AssetDatabase.LoadAssetAtPath<LootTableDefinition>("Assets/_Game/Data/Loot/WildlingLoot.asset");
		for (int seed = 0; seed < 100; seed++)
		{
			Assert.That<int>(LootRoller.Roll(table, seed).Single((LootRollResult value) => value.ItemId == "plant_fiber").Amount, (IResolveConstraint)(object)Is.InRange((IComparable)1, (IComparable)2), $"Seed {seed}", Array.Empty<object>());
		}
	}

	[Test]
	public void SameSeedProducesSameRoll()
	{
		LootTableDefinition table = AssetDatabase.LoadAssetAtPath<LootTableDefinition>("Assets/_Game/Data/Loot/WildlingLoot.asset");
		string first = Signature(LootRoller.Roll(table, 72491));
		Assert.That<string>(Signature(LootRoller.Roll(table, 72491)), (IResolveConstraint)(object)Is.EqualTo((object)first));
	}

	[Test]
	public void EqualItemsFromOneRollAreGroupedIntoOneStack()
	{
		LootTableDefinition table = CreateTable(new LootTableEntry("wood", 1f, 1, 1, isGuaranteed: true), new LootTableEntry("wood", 1f, 2, 2, isGuaranteed: true));
		try
		{
			IReadOnlyList<LootRollResult> readOnlyList = LootRoller.Roll(table, 11);
			Assert.That<int>(readOnlyList.Count, (IResolveConstraint)(object)Is.EqualTo((object)1));
			Assert.That<string>(readOnlyList[0].ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"wood"));
			Assert.That<int>(readOnlyList[0].Amount, (IResolveConstraint)(object)Is.EqualTo((object)3));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(table);
		}
	}

	[Test]
	public void WildlingAndItemsReferenceLootAssets()
	{
		EnemyDefinition enemyDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/Wildling.asset");
		GameObject worldItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Items/WorldItem.prefab");
		Assert.That<EnemyDefinition>(enemyDefinition, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<LootTableDefinition>(enemyDefinition.LootTable, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<string>(enemyDefinition.LootTable.Id, (IResolveConstraint)(object)Is.EqualTo((object)"loot.wildling.v01"));
		Assert.That<GameObject>(worldItemPrefab, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<WorldItemController>(worldItemPrefab.GetComponent<WorldItemController>(), (IResolveConstraint)(object)Is.Not.Null);
		string[] array = new string[4] { "PlantFiber", "HealingPotion", "BuffFood", "Wood" };
		foreach (string itemName in array)
		{
			ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/" + itemName + ".asset");
			Assert.That<ItemDefinition>(itemDefinition, (IResolveConstraint)(object)Is.Not.Null, itemName, Array.Empty<object>());
			Assert.That<GameObject>(itemDefinition.WorldDropPrefab, (IResolveConstraint)(object)Is.EqualTo((object)worldItemPrefab), itemName, Array.Empty<object>());
		}
	}

	private static void AssertEntry(LootTableDefinition table, string itemId, float chance, int minimum, int maximum, bool guaranteed)
	{
		LootTableEntry entry = table.Entries.Single((LootTableEntry value) => value.ItemId == itemId);
		Assert.That<float>(entry.Chance, (IResolveConstraint)(object)Is.EqualTo((object)chance).Within((object)0.0001f));
		Assert.That<int>(entry.MinimumAmount, (IResolveConstraint)(object)Is.EqualTo((object)minimum));
		Assert.That<int>(entry.MaximumAmount, (IResolveConstraint)(object)Is.EqualTo((object)maximum));
		Assert.That<bool>(entry.Guaranteed, (IResolveConstraint)(object)Is.EqualTo((object)guaranteed));
	}

	private static string Signature(IReadOnlyList<LootRollResult> results)
	{
		return string.Join("|", results.Select((LootRollResult value) => $"{value.ItemId}:{value.Amount}"));
	}

	private static LootTableDefinition CreateTable(params LootTableEntry[] entries)
	{
		LootTableDefinition table = ScriptableObject.CreateInstance<LootTableDefinition>();
		SerializedObject serialized = new SerializedObject(table);
		serialized.FindProperty("id").stringValue = "loot.test.grouping";
		SerializedProperty values = serialized.FindProperty("entries");
		values.arraySize = entries.Length;
		for (int index = 0; index < entries.Length; index++)
		{
			LootTableEntry entry = entries[index];
			SerializedProperty arrayElementAtIndex = values.GetArrayElementAtIndex(index);
			arrayElementAtIndex.FindPropertyRelative("itemId").stringValue = entry.ItemId;
			arrayElementAtIndex.FindPropertyRelative("chance").floatValue = entry.Chance;
			arrayElementAtIndex.FindPropertyRelative("minimumAmount").intValue = entry.MinimumAmount;
			arrayElementAtIndex.FindPropertyRelative("maximumAmount").intValue = entry.MaximumAmount;
			arrayElementAtIndex.FindPropertyRelative("guaranteed").boolValue = entry.Guaranteed;
			arrayElementAtIndex.FindPropertyRelative("selectionGroup").stringValue = entry.SelectionGroup;
		}
		serialized.ApplyModifiedPropertiesWithoutUndo();
		return table;
	}
}
}
