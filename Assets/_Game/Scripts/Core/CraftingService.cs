using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class CraftingService
	{
		private readonly ContentDatabase _database;

		private readonly PlayerInventory _inventory;

		private readonly WeaponProgressionState _weaponProgression;

		private readonly PlayerEquipment _equipment;

		private readonly PlayerProgressionService _progression;

		private readonly TechnologyUnlockService _unlocks;

		private readonly IMaterialStock _materials;

		private bool _isCrafting;

		public bool IsCrafting => _isCrafting;

		public event Action<CraftingResult> CraftCompleted;

		public CraftingService(ContentDatabase database, PlayerInventory inventory, WeaponProgressionState weaponProgression, TechnologyUnlockService unlocks, PlayerEquipment equipment = null, PlayerProgressionService progression = null, IMaterialStock materials = null)
		{
			_database = database ?? throw new ArgumentNullException("database");
			_inventory = inventory ?? throw new ArgumentNullException("inventory");
			_weaponProgression = weaponProgression ?? throw new ArgumentNullException("weaponProgression");
			_equipment = equipment;
			_progression = progression;
			_unlocks = unlocks ?? throw new ArgumentNullException("unlocks");
			_materials = materials ?? inventory;
		}

		public int GetAvailableAmount(string itemId)
		{
			return _materials.GetTotalAmount(itemId);
		}

		public CraftingResult TryCraft(string recipeId, CraftingStationType station)
		{
			if (_isCrafting)
			{
				return Result(CraftingResultCode.Busy, recipeId);
			}
			_isCrafting = true;
			try
			{
				CraftingResult craftingResult = EvaluateAndCraft(recipeId, station);
				if (craftingResult.Succeeded)
				{
					_progression?.RecordRecipeCrafted(craftingResult.RecipeId, _database.GetItem(_database.GetCraftingRecipe(craftingResult.RecipeId).ResultItemId).Tier);
				}
				this.CraftCompleted?.Invoke(craftingResult);
				return craftingResult;
			}
			finally
			{
				_isCrafting = false;
			}
		}

		public CraftingResult Evaluate(CraftingRecipeDefinition recipe, CraftingStationType station)
		{
			return EvaluateInternal(recipe, station, commit: false);
		}

		private CraftingResult EvaluateAndCraft(string recipeId, CraftingStationType station)
		{
			if (!_database.TryGetCraftingRecipe(recipeId, out var value))
			{
				return Result(CraftingResultCode.UnknownRecipe, recipeId);
			}
			return EvaluateInternal(value, station, commit: true);
		}

		private CraftingResult EvaluateInternal(CraftingRecipeDefinition recipe, CraftingStationType station, bool commit)
		{
			if (recipe == null || !recipe.TryValidate(out var error))
			{
				return Result(CraftingResultCode.InvalidRecipe, (recipe != null) ? recipe.Id : string.Empty);
			}
			if (!_unlocks.IsRecipeUnlocked(recipe.Id))
			{
				return Result(CraftingResultCode.RecipeLocked, recipe.Id);
			}
			if (!IsStationCompatible(recipe.RequiredStation, station))
			{
				return Result(CraftingResultCode.WrongStation, recipe.Id);
			}
			if (recipe.ResultType == CraftingResultType.WeaponUpgrade && !_weaponProgression.CanApply(recipe.WeaponUpgrade.WeaponId, recipe.WeaponUpgrade.TargetLevel))
			{
				return Result(CraftingResultCode.UpgradeAlreadyOwned, recipe.Id);
			}
			List<InventoryItemAmount> list = new List<InventoryItemAmount>(recipe.Ingredients.Count);
			foreach (CraftingIngredient ingredient in recipe.Ingredients)
			{
				if (_materials.GetTotalAmount(ingredient.ItemId) < ingredient.Amount)
				{
					return Result(CraftingResultCode.MissingIngredients, recipe.Id);
				}
				list.Add(new InventoryItemAmount(ingredient.ItemId, ingredient.Amount));
			}
			string text = ((recipe.ResultType == CraftingResultType.Item) ? recipe.ResultItemId : string.Empty);
			int additionAmount = ((recipe.ResultType == CraftingResultType.Item) ? recipe.ResultAmount : 0);
			EquipmentSlot slot;
			bool flag = ShouldEquipResult(recipe, out slot);
			if (flag)
			{
				text = string.Empty;
				additionAmount = 0;
			}
			if (!commit)
			{
				return CanApplyInventoryTransaction(list, text, additionAmount, recipe.Id);
			}
			if (!_materials.TryApplyTransaction(list, text, additionAmount, out var failure))
			{
				return Result(MapFailure(failure), recipe.Id);
			}
			if (recipe.ResultType == CraftingResultType.WeaponUpgrade)
			{
				WeaponUpgradeConfiguration weaponUpgrade = recipe.WeaponUpgrade;
				_weaponProgression.TryApply(weaponUpgrade.WeaponId, weaponUpgrade.TargetLevel, weaponUpgrade.HealthDamageMultiplier, weaponUpgrade.StaggerDamageMultiplier);
			}
			else if (flag && !_equipment.TryEquip(slot, ItemStack.Create(_database.GetItem(recipe.ResultItemId), 1), out error))
			{
				return Result(CraftingResultCode.InvalidRecipe, recipe.Id);
			}
			return Result(CraftingResultCode.Success, recipe.Id);
		}

		private bool ShouldEquipResult(CraftingRecipeDefinition recipe, out EquipmentSlot slot)
		{
			slot = EquipmentSlot.Weapon1;
			if (_equipment == null || recipe.ResultType != CraftingResultType.Item || recipe.ResultAmount != 1 || !PlayerEquipment.TryGetDefaultEquipmentSlot(recipe.ResultItemId, out slot) || _equipment.TryGetSlot(slot, out var _))
			{
				return false;
			}
			string error;
			return _equipment.CanEquip(slot, ItemStack.Create(_database.GetItem(recipe.ResultItemId), 1), out error);
		}

		private CraftingResult CanApplyInventoryTransaction(IReadOnlyList<InventoryItemAmount> removals, string additionId, int additionAmount, string recipeId)
		{
			if (!_materials.CanApplyTransaction(removals, additionId, additionAmount, out var failure))
			{
				return Result(MapFailure(failure), recipeId);
			}
			return Result(CraftingResultCode.Success, recipeId);
		}

		private static CraftingResultCode MapFailure(InventoryTransactionFailure failure)
		{
			return failure switch
			{
				InventoryTransactionFailure.MissingItems => CraftingResultCode.MissingIngredients, 
				InventoryTransactionFailure.InventoryFull => CraftingResultCode.InventoryFull, 
				_ => CraftingResultCode.InvalidRecipe, 
			};
		}

		private static CraftingResult Result(CraftingResultCode code, string recipeId)
		{
			return new CraftingResult(code, recipeId);
		}

		public static bool IsStationCompatible(CraftingStationType required, CraftingStationType available)
		{
			if (required != available)
			{
				if (required == CraftingStationType.None)
				{
					return available == CraftingStationType.Workbench;
				}
				return false;
			}
			return true;
		}
	}

	public readonly struct CraftingResult
	{
		public CraftingResultCode Code { get; }

		public string RecipeId { get; }

		public bool Succeeded => Code == CraftingResultCode.Success;

		public CraftingResult(CraftingResultCode code, string recipeId)
		{
			Code = code;
			RecipeId = recipeId ?? string.Empty;
		}
	}
}
