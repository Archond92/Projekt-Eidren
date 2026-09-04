using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class ProductionContentTests
{
	[Test]
	public void ProductionBuildingsHaveNeutralRatesAndRuntimeComponents()
	{
		GameObject root = new GameObject("ProductionContentTests");
		try
		{
			ContentDatabase content = root.AddComponent<ContentDatabase>();
			BuildingFootprint footprint;
			foreach (KeyValuePair<string, CraftingStationType> pair in new Dictionary<string, CraftingStationType>
			{
				["building.workbench"] = CraftingStationType.Workbench,
				["building.smelter"] = CraftingStationType.Smelter,
				["building.sawmill"] = CraftingStationType.Sawmill,
				["building.ropewalk"] = CraftingStationType.Ropewalk,
				["building.stonecutter"] = CraftingStationType.Stonecutter,
				["building.cooking_pot"] = CraftingStationType.CookingPot
			})
			{
				Assert.That<bool>(content.TryGetBuilding(pair.Key, out var building), (IResolveConstraint)(object)Is.True, pair.Key, Array.Empty<object>());
				Assert.That<CraftingStationType>(building.CraftingStation, (IResolveConstraint)(object)Is.EqualTo((object)pair.Value), pair.Key, Array.Empty<object>());
				Assert.That<float>(building.BaseRate, (IResolveConstraint)(object)Is.EqualTo((object)1f), pair.Key, Array.Empty<object>());
				Assert.That<float>(building.EidraFactor, (IResolveConstraint)(object)Is.EqualTo((object)1f), pair.Key + " must work without an Eidra.", Array.Empty<object>());
				Assert.That<bool>(building.TryGetLevel(1, out footprint, out var prefab), (IResolveConstraint)(object)Is.True, pair.Key, Array.Empty<object>());
				Assert.That<WorkbenchController>(prefab.GetComponent<WorkbenchController>(), (IResolveConstraint)(object)Is.Not.Null, pair.Key, Array.Empty<object>());
			}
			Assert.That<bool>(content.TryGetBuilding("building.farm_plot", out var farm), (IResolveConstraint)(object)Is.True);
			Assert.That<float>(farm.BaseRate, (IResolveConstraint)(object)Is.EqualTo((object)1f));
			Assert.That<float>(farm.EidraFactor, (IResolveConstraint)(object)Is.EqualTo((object)1f));
			Assert.That<bool>(farm.TryGetLevel(1, out footprint, out var farmPrefab), (IResolveConstraint)(object)Is.True);
			Assert.That<FarmPlotController>(farmPrefab.GetComponent<FarmPlotController>(), (IResolveConstraint)(object)Is.Not.Null);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void FullFarmBecomesFourBreadAtCookingPotWithoutEidra()
	{
		GameObject root = new GameObject("ProductionBreadChain");
		try
		{
			ContentDatabase database = root.AddComponent<ContentDatabase>();
			PlayerInventory inventory = new PlayerInventory(database);
			PlayerEquipment equipment = new PlayerEquipment(database);
			inventory.ConfigureEquipment(equipment);
			ProgressionCurveDefinition curve = Load<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
			TechnologyTreeDefinition tree = Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
			PlayerProgressionService progression = new PlayerProgressionService(curve);
			TechnologyUnlockService unlocks = new TechnologyUnlockService(tree, progression);
			UnlockAll(tree, progression, unlocks);
			BuildingRegistry buildingRegistry = new BuildingRegistry();
			Assert.That<bool>(buildingRegistry.TryAdd(new BuildingInstanceState("bread.farm", "building.farm_plot", Vector3.zero, 0, 1, 4, 0)), (IResolveConstraint)(object)Is.True);
			FarmProductionService farmProductionService = new FarmProductionService(buildingRegistry, inventory);
			Assert.That<int>(farmProductionService.AdvanceForHomecoming(), (IResolveConstraint)(object)Is.EqualTo((object)12));
			Assert.That<FarmActionResult>(farmProductionService.TryHarvest("bread.farm"), (IResolveConstraint)(object)Is.EqualTo((object)FarmActionResult.Success));
			CraftingService craftingService = new CraftingService(database, inventory, new WeaponProgressionState(), unlocks, equipment, progression);
			Assert.That<bool>(craftingService.TryCraft("craft_bread", CraftingStationType.CookingPot).Succeeded, (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(craftingService.TryCraft("craft_bread", CraftingStationType.CookingPot).Succeeded, (IResolveConstraint)(object)Is.True);
			Assert.That<int>(inventory.GetTotalAmount("wheat"), (IResolveConstraint)(object)Is.Zero);
			Assert.That<int>(inventory.GetTotalAmount("buff_food"), (IResolveConstraint)(object)Is.EqualTo((object)4));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static void UnlockAll(TechnologyTreeDefinition tree, PlayerProgressionService progression, TechnologyUnlockService unlocks)
	{
		progression.RecordEnemyDefeated(100000);
		foreach (TechnologyNodeDefinition node in tree.Nodes)
		{
			if (node.SortOrder <= 17 && unlocks.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
			{
				Assert.That<TechnologyUnlockResult>(unlocks.TryUnlock(node.Id), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success), node.Id, Array.Empty<object>());
			}
		}
	}

	private static T Load<T>(string path) where T : UnityEngine.Object
	{
		T val = AssetDatabase.LoadAssetAtPath<T>(path);
		Assert.That<T>(val, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		return val;
	}
}
}
