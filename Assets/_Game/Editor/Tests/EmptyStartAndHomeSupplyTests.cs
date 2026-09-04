using Eidren.Core.Services;
using Eidren.Data;
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
public sealed class EmptyStartAndHomeSupplyTests
{
	private const string HomePath = "Assets/_Game/Data/Zones/Zone_HomeBase.asset";

	[Test]
	public void NewGame_HasNoItemsEquipmentWeaponOrEidra()
	{
		GameObject root = new GameObject("EmptyStart_Test");
		try
		{
			ContentDatabase content = root.AddComponent<ContentDatabase>();
			GameSession gameSession = root.AddComponent<GameSession>();
			gameSession.Initialize();
			gameSession.ConfigureContentDatabase(content);
			gameSession.StartNewGame();
			Assert.That<bool>(gameSession.PlayerInventory.ExportSlots().All((ItemStack stack) => stack.IsEmpty), (IResolveConstraint)(object)Is.True, "M8.2: Der Rucksack beginnt leer.", Array.Empty<object>());
			Assert.That<EquipmentAssignment[]>(gameSession.PlayerEquipment.ExportSlots(), (IResolveConstraint)(object)Is.Empty, "M8.2/M5: Kein Ausruestungsplatz ist belegt.", Array.Empty<object>());
			Assert.That<string>(gameSession.ActiveWeaponId, (IResolveConstraint)(object)Is.Empty);
			Assert.That<string>(gameSession.ActiveEidraId, (IResolveConstraint)(object)Is.Empty);
			Assert.That<int>(gameSession.PlayerInventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.Zero);
			Assert.That<int>(gameSession.PlayerInventory.GetTotalAmount("buff_food"), (IResolveConstraint)(object)Is.Zero);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void EmptySelections_SurviveSaveMapping()
	{
		GameObject root = new GameObject("EmptySelectionSave_Test");
		try
		{
			ContentDatabase content = root.AddComponent<ContentDatabase>();
			GameSession session = root.AddComponent<GameSession>();
			session.Initialize();
			session.ConfigureContentDatabase(content);
			session.StartNewGame();
			SaveGameMapper saveGameMapper = new SaveGameMapper(content);
			SaveGameData save = saveGameMapper.Capture(session, "2026-07-29T00:00:00Z", "2026-07-29T00:00:00Z", "test");
			Assert.That<bool>(saveGameMapper.TryMapToRuntime(save, out var runtime, out var _, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
			Assert.That<string>(runtime.ActiveWeaponId, (IResolveConstraint)(object)Is.Empty);
			Assert.That<string>(runtime.ActiveEidraId, (IResolveConstraint)(object)Is.Empty);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void HomeBase_CarriesTheFiniteOneTimeSupply()
	{
		ZoneDefinition home = Home();
		Dictionary<string, int> dictionary = home.ResourceAllocations.ToDictionary((ZoneResourceAllocation allocation) => allocation.Definition.Id, (ZoneResourceAllocation allocation) => allocation.Count, StringComparer.Ordinal);
		Assert.That<bool>(home.ResourcesAllowed, (IResolveConstraint)(object)Is.True);
		Assert.That<ResourceRespawnMode>(home.ResourceRespawnMode, (IResolveConstraint)(object)Is.EqualTo((object)ResourceRespawnMode.None));
		Assert.That<int>(dictionary["resource.tree"], (IResolveConstraint)(object)Is.EqualTo((object)26));
		Assert.That<int>(dictionary["resource.stone_deposit"], (IResolveConstraint)(object)Is.EqualTo((object)18));
		Assert.That<int>(dictionary["resource.fiber_plant"], (IResolveConstraint)(object)Is.EqualTo((object)10));
		Assert.That<string>(home.SideNodeAllocations.Single().Definition.Id, (IResolveConstraint)(object)Is.EqualTo((object)"resource.berry_bush"));
		Assert.That<int>(home.SideNodeAllocations.Single().Count, (IResolveConstraint)(object)Is.EqualTo((object)4));
	}

	[Test]
	public void HarvestedHomeNode_RemainsSpentAfterLeavingAndHomecoming()
	{
		ZoneStateService zoneStateService = new ZoneStateService();
		zoneStateService.Reset(606);
		zoneStateService.GetOrCreate("home_base", 2).MarkHarvested("home_base.resource.tree.001");
		ZoneState afterLeaving = zoneStateService.GetOrCreate("home_base", 2);
		zoneStateService.InvalidateAllForHomecoming(2);
		Assert.That<bool>(afterLeaving.IsHarvested("home_base.resource.tree.001"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(zoneStateService.TryGet("home_base", out var afterHomecoming), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(afterHomecoming.IsHarvested("home_base.resource.tree.001"), (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void HomeSupply_CoversFirstFourUnlocksAndLeavesTheM89Remainder()
	{
		Dictionary<string, int> dictionary = Supply(Home());
		Dictionary<string, int> cost = new Dictionary<string, int>(StringComparer.Ordinal);
		AddCost(cost, Load<BuildingCostDefinition>("Assets/_Game/Data/Buildings/Workbench.asset").Cost);
		AddCost(cost, Load<CraftingRecipeDefinition>("Assets/_Game/Data/Crafting/Recipe_Axe.asset").Ingredients);
		AddCost(cost, Load<BuildingCostDefinition>("Assets/_Game/Data/Buildings/StorageChest.asset").Cost);
		Add(cost, "wood", 4);
		Add(cost, "stone", 3);
		Add(cost, "plant_fiber", 3);
		Assert.That<int>(cost["wood"], (IResolveConstraint)(object)Is.EqualTo((object)25));
		Assert.That<int>(cost["stone"], (IResolveConstraint)(object)Is.EqualTo((object)15));
		Assert.That<int>(cost["plant_fiber"], (IResolveConstraint)(object)Is.EqualTo((object)5));
		Assert.That<int>(dictionary["wood"] - cost["wood"], (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(dictionary["stone"] - cost["stone"], (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(dictionary["plant_fiber"] - cost["plant_fiber"], (IResolveConstraint)(object)Is.EqualTo((object)5));
	}

	private static Dictionary<string, int> Supply(ZoneDefinition home)
	{
		Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (ZoneResourceAllocation allocation in home.ResourceAllocations)
		{
			ResourceNodeDefinition definition = allocation.Definition;
			HarvestOutcome handYield = new HarvestYieldResolver(definition.HarvestYields).Resolve(definition, null);
			Assert.That<bool>(handYield.IsAllowed, (IResolveConstraint)(object)Is.True, definition.Id, Array.Empty<object>());
			Assert.That<int>(handYield.Amount, (IResolveConstraint)(object)Is.EqualTo((object)1), definition.Id, Array.Empty<object>());
			Add(result, definition.OutputItem.Id, allocation.Count * handYield.Amount);
		}
		return result;
	}

	private static void AddCost(Dictionary<string, int> target, IReadOnlyList<CraftingIngredient> ingredients)
	{
		foreach (CraftingIngredient ingredient in ingredients)
		{
			Add(target, ingredient.ItemId, ingredient.Amount);
		}
	}

	private static void Add(Dictionary<string, int> target, string itemId, int amount)
	{
		target[itemId] = (target.TryGetValue(itemId, out var existing) ? (existing + amount) : amount);
	}

	private static ZoneDefinition Home()
	{
		return Load<ZoneDefinition>("Assets/_Game/Data/Zones/Zone_HomeBase.asset");
	}

	private static T Load<T>(string path) where T : UnityEngine.Object
	{
		T val = AssetDatabase.LoadAssetAtPath<T>(path);
		Assert.That<T>(val, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		return val;
	}
}
}
