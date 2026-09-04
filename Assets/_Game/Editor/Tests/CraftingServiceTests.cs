using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class CraftingServiceTests
{
	private GameObject _databaseObject;

	private ContentDatabase _database;

	private PlayerInventory _inventory;

	private PlayerEquipment _equipment;

	private WeaponProgressionState _progression;

	private PlayerProgressionService _playerProgression;

	private TechnologyTreeDefinition _tree;

	private TechnologyUnlockService _technology;

	private CraftingService _service;

	[SetUp]
	public void SetUp()
	{
		_databaseObject = new GameObject("CraftingDatabase_Test");
		_database = _databaseObject.AddComponent<ContentDatabase>();
		_inventory = new PlayerInventory(_database);
		_equipment = new PlayerEquipment(_database);
		_inventory.ConfigureEquipment(_equipment);
		_progression = new WeaponProgressionState();
		ProgressionCurveDefinition curve = AssetDatabase.LoadAssetAtPath<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
		Assert.That<ProgressionCurveDefinition>(curve, (IResolveConstraint)(object)Is.Not.Null);
		_playerProgression = new PlayerProgressionService(curve);
		_tree = AssetDatabase.LoadAssetAtPath<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
		Assert.That<TechnologyTreeDefinition>(_tree, (IResolveConstraint)(object)Is.Not.Null);
		_technology = new TechnologyUnlockService(_tree, _playerProgression);
		UnlockAllTechnologies();
		_playerProgression.ResetForNewGame();
		_service = new CraftingService(_database, _inventory, _progression, _technology, _equipment, _playerProgression);
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_databaseObject);
	}

	[Test]
	public void ValidRecipe_RemovesIngredientsAndAddsResult()
	{
		AddPotionIngredients(1);
		Assert.That<bool>(_service.TryCraft("craft_healing_potion", CraftingStationType.CookingPot).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(_inventory.GetTotalAmount("copper_bar"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_inventory.GetTotalAmount("berry"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_inventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void LockedRecipe_CannotBeCraftedThroughTheService()
	{
		TechnologyUnlockService locked = new TechnologyUnlockService(_tree, _playerProgression);
		CraftingService craftingService = new CraftingService(_database, _inventory, _progression, locked, _equipment, _playerProgression);
		_inventory.Add("wood", 4);
		_inventory.Add("stone", 3);
		_inventory.Add("plant_fiber", 3);
		Assert.That<CraftingResultCode>(craftingService.TryCraft("craft_hammer", CraftingStationType.Workbench).Code, (IResolveConstraint)(object)Is.EqualTo((object)CraftingResultCode.RecipeLocked));
		Assert.That<int>(_inventory.GetTotalAmount("axe"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_inventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)4));
	}

	[Test]
	public void TierZeroTool_IsUnlockedAndCraftedByHand()
	{
		TechnologyUnlockService initial = new TechnologyUnlockService(_tree, _playerProgression);
		CraftingService craftingService = new CraftingService(_database, _inventory, _progression, initial, _equipment, _playerProgression);
		_inventory.Add("wood", 3);
		_inventory.Add("stone", 2);
		_inventory.Add("plant_fiber", 2);
		Assert.That<bool>(craftingService.TryCraft("craft_axe", CraftingStationType.None).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(_inventory.GetTotalAmount("axe"), (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void TierZeroTool_CanAlsoBeCraftedAtTheWorkbench()
	{
		TechnologyUnlockService initial = new TechnologyUnlockService(_tree, _playerProgression);
		CraftingService craftingService = new CraftingService(_database, _inventory, _progression, initial, _equipment, _playerProgression);
		_inventory.Add("wood", 3);
		_inventory.Add("stone", 2);
		_inventory.Add("plant_fiber", 2);
		Assert.That<bool>(craftingService.TryCraft("craft_axe", CraftingStationType.Workbench).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(_inventory.GetTotalAmount("axe"), (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void SuccessfulCraft_AwardsDiscoveryXpOnlyOnce()
	{
		AddPotionIngredients(2);
		Assert.That<bool>(_service.TryCraft("craft_healing_potion", CraftingStationType.CookingPot).Succeeded, (IResolveConstraint)(object)Is.True);
		int afterFirst = _playerProgression.State.Experience;
		Assert.That<bool>(_service.TryCraft("craft_healing_potion", CraftingStationType.CookingPot).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(afterFirst, (IResolveConstraint)(object)Is.EqualTo((object)50));
		Assert.That<int>(_playerProgression.State.Experience - afterFirst, (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	[Test]
	public void HammerRecipe_ConsumesM89CostsAndEquipsWeapon()
	{
		_inventory.Add("wood", 4);
		_inventory.Add("stone", 3);
		_inventory.Add("plant_fiber", 3);
		Assert.That<bool>(_service.TryCraft("craft_hammer", CraftingStationType.Workbench).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_equipment.TryGetSlot(EquipmentSlot.Weapon1, out var hammer), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(hammer.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"hammer"));
		Assert.That<int>(_inventory.GetTotalAmount("hammer"), (IResolveConstraint)(object)Is.Zero, "The first crafted weapon belongs in its equipment slot.", Array.Empty<object>());
		Assert.That<int>(_inventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_inventory.GetTotalAmount("stone"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_inventory.GetTotalAmount("plant_fiber"), (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void DaggersRecipe_UsesM89CostsAndDuplicateGoesToBackpack()
	{
		_inventory.Add("wood", 4);
		_inventory.Add("stone", 10);
		_inventory.Add("plant_fiber", 6);
		Assert.That<bool>(_service.TryCraft("craft_daggers", CraftingStationType.Workbench).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_service.TryCraft("craft_daggers", CraftingStationType.Workbench).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_equipment.TryGetSlot(EquipmentSlot.Weapon2, out var daggers), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(daggers.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"daggers"));
		Assert.That<int>(_inventory.GetTotalAmount("daggers"), (IResolveConstraint)(object)Is.EqualTo((object)1), "A replacement weapon costs one backpack slot (M5).", Array.Empty<object>());
	}

	[Test]
	public void MissingIngredients_ChangeNothing()
	{
		_inventory.Add("berry", 1);
		Assert.That<CraftingResultCode>(_service.TryCraft("craft_healing_potion", CraftingStationType.CookingPot).Code, (IResolveConstraint)(object)Is.EqualTo((object)CraftingResultCode.MissingIngredients));
		Assert.That<int>(_inventory.GetTotalAmount("berry"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_inventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void FullInventory_ChangesNothing()
	{
		_inventory.Add("berry", 4);
		_inventory.Add("copper_bar", 2);
		FillEveryFreeSlot("stone");
		Assert.That<CraftingResultCode>(_service.TryCraft("craft_healing_potion", CraftingStationType.CookingPot).Code, (IResolveConstraint)(object)Is.EqualTo((object)CraftingResultCode.InventoryFull));
		Assert.That<int>(_inventory.GetTotalAmount("berry"), (IResolveConstraint)(object)Is.EqualTo((object)4));
		Assert.That<int>(_inventory.GetTotalAmount("copper_bar"), (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(_inventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void NoRecipeProducesAWeaponOrToolUpgrade()
	{
		foreach (CraftingRecipeDefinition recipe in AllRecipes())
		{
			Assert.That<CraftingResultType>(recipe.ResultType, (IResolveConstraint)(object)Is.EqualTo((object)CraftingResultType.Item), recipe.Id + ": Upgrades kommen erst nach v0.1.", Array.Empty<object>());
		}
	}

	[Test]
	public void WeaponProgression_StaysAtLevelOneWithoutUpgrades()
	{
		string[] array = new string[2] { "hammer", "daggers" };
		foreach (string weaponId in array)
		{
			Assert.That<int>(_progression.GetLevel(weaponId), (IResolveConstraint)(object)Is.EqualTo((object)1), weaponId, Array.Empty<object>());
			Assert.That<float>(_progression.GetHealthDamageMultiplier(weaponId), (IResolveConstraint)(object)Is.EqualTo((object)1f), weaponId, Array.Empty<object>());
			Assert.That<float>(_progression.GetStaggerDamageMultiplier(weaponId), (IResolveConstraint)(object)Is.EqualTo((object)1f), weaponId, Array.Empty<object>());
		}
	}

	[Test]
	public void ParallelCraftRequest_ProducesOnlyOneResult()
	{
		AddPotionIngredients(2);
		CraftingResult nested = default(CraftingResult);
		_service.CraftCompleted += delegate
		{
			nested = _service.TryCraft("craft_healing_potion", CraftingStationType.CookingPot);
		};
		Assert.That<bool>(_service.TryCraft("craft_healing_potion", CraftingStationType.CookingPot).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<CraftingResultCode>(nested.Code, (IResolveConstraint)(object)Is.EqualTo((object)CraftingResultCode.Busy));
		Assert.That<int>(_inventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_inventory.GetTotalAmount("berry"), (IResolveConstraint)(object)Is.EqualTo((object)3));
	}

	[Test]
	public void HealingPotion_NeedsTheCookingPotAndNotTheWorkbench()
	{
		AddPotionIngredients(1);
		Assert.That<CraftingResultCode>(_service.TryCraft("craft_healing_potion", CraftingStationType.Workbench).Code, (IResolveConstraint)(object)Is.EqualTo((object)CraftingResultCode.WrongStation));
		Assert.That<int>(_inventory.GetTotalAmount("berry"), (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(_inventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void WrongStation_ChangesNothing()
	{
		AddPotionIngredients(1);
		Assert.That<CraftingResultCode>(_service.TryCraft("craft_healing_potion", CraftingStationType.None).Code, (IResolveConstraint)(object)Is.EqualTo((object)CraftingResultCode.WrongStation));
		Assert.That<int>(_inventory.GetTotalAmount("berry"), (IResolveConstraint)(object)Is.EqualTo((object)3));
	}

	[Test]
	public void NoRecipeExceedsCentralFiveIngredientLimit()
	{
		foreach (CraftingRecipeDefinition recipe in AllRecipes())
		{
			Assert.That<int>(recipe.Ingredients.Count, (IResolveConstraint)(object)Is.LessThanOrEqualTo((object)5), recipe.Id + ": zentrale v0.2-UI-Grenze.", Array.Empty<object>());
			Assert.That<IReadOnlyList<CraftingIngredient>>(recipe.Ingredients, (IResolveConstraint)(object)Is.Not.Empty, recipe.Id, Array.Empty<object>());
		}
	}

	[Test]
	public void EveryRecipeIdResolvesInTheContentDatabase()
	{
		foreach (CraftingRecipeDefinition recipe in AllRecipes())
		{
			ItemDefinition value;
			foreach (CraftingIngredient ingredient in recipe.Ingredients)
			{
				Assert.That<bool>(_database.TryGetItem(ingredient.ItemId, out value), (IResolveConstraint)(object)Is.True, recipe.Id + ": Zutat '" + ingredient.ItemId + "' ist nicht auflösbar.", Array.Empty<object>());
			}
			if (recipe.ResultType == CraftingResultType.Item)
			{
				Assert.That<bool>(_database.TryGetItem(recipe.ResultItemId, out value), (IResolveConstraint)(object)Is.True, recipe.Id + ": Ergebnis '" + recipe.ResultItemId + "' ist nicht auflösbar.", Array.Empty<object>());
			}
		}
	}

	[Test]
	public void EveryRecipeNamesAKnownCraftingStation()
	{
		foreach (CraftingRecipeDefinition recipe in AllRecipes())
		{
			Assert.That<bool>(Enum.IsDefined(typeof(CraftingStationType), recipe.RequiredStation), (IResolveConstraint)(object)Is.True, recipe.Id + ": unbekannte Station " + $"'{(int)recipe.RequiredStation}'.", Array.Empty<object>());
			bool isHandCraftedTool = recipe.Id == "craft_axe" || recipe.Id == "craft_scythe" || recipe.Id == "craft_pickaxe";
			Assert.That<bool>(recipe.RequiredStation == CraftingStationType.None, (IResolveConstraint)(object)Is.EqualTo((object)isHandCraftedTool), recipe.Id + ": Nur die drei T0-Werkzeuge sind Handarbeit ohne Station.", Array.Empty<object>());
		}
	}

	[Test]
	public void RefinementRecipes_HaveExactlyOneIngredient()
	{
		string[] array = new string[4] { "craft_plank", "craft_rope", "craft_stone_block", "craft_copper_bar" };
		foreach (string id in array)
		{
			Assert.That<int>(_database.GetCraftingRecipe(id).Ingredients.Count, (IResolveConstraint)(object)Is.EqualTo((object)1), id, Array.Empty<object>());
		}
	}

	private IEnumerable<CraftingRecipeDefinition> AllRecipes()
	{
		CraftingRecipeCatalogDefinition catalog = AssetDatabase.LoadAssetAtPath<CraftingRecipeCatalogDefinition>("Assets/_Game/Resources/Data/CraftingRecipeCatalog_V01.asset");
		Assert.That<CraftingRecipeCatalogDefinition>(catalog, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<IReadOnlyList<CraftingRecipeDefinition>>(catalog.Recipes, (IResolveConstraint)(object)Is.Not.Empty);
		foreach (CraftingRecipeDefinition recipe in catalog.Recipes)
		{
			Assert.That<CraftingRecipeDefinition>(recipe, (IResolveConstraint)(object)Is.Not.Null);
			yield return recipe;
		}
	}

	private void FillEveryFreeSlot(string itemId)
	{
		_inventory.Add(itemId, 16 * _database.GetItem(itemId).MaximumStackSize);
	}

	private void AddPotionIngredients(int crafts)
	{
		_inventory.Add("copper_bar", crafts);
		_inventory.Add("berry", 3 * crafts);
	}

	private void UnlockAllTechnologies()
	{
		_playerProgression.RecordEnemyDefeated(100000);
		_playerProgression.GrantTechnologyPoints(100);
		bool changed;
		do
		{
			changed = false;
			foreach (TechnologyNodeDefinition node in _tree.Nodes)
			{
				if (_technology.GetNodeState(node.Id) == TechnologyNodeState.Available && _technology.TryUnlock(node.Id) == TechnologyUnlockResult.Success)
				{
					changed = true;
				}
			}
		}
		while (changed);
	}
}
}
