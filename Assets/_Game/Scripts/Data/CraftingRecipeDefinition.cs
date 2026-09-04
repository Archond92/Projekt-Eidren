using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Crafting/Recipe Definition")]
	public sealed class CraftingRecipeDefinition : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private string displayName;

		[SerializeField]
		[TextArea(2, 4)]
		private string description;

		[SerializeField]
		private CraftingResultType resultType;

		[SerializeField]
		private string resultItemId;

		[SerializeField]
		[Min(1f)]
		private int resultAmount = 1;

		[SerializeField]
		private CraftingIngredient[] ingredients = Array.Empty<CraftingIngredient>();

		[SerializeField]
		private CraftingStationType requiredStation = CraftingStationType.Workbench;

		[SerializeField]
		private Sprite icon;

		[SerializeField]
		private int sortOrder;

		[SerializeField]
		private WeaponUpgradeConfiguration weaponUpgrade;

		public string Id => id ?? string.Empty;

		public string DisplayName => displayName ?? string.Empty;

		public string Description => description ?? string.Empty;

		public CraftingResultType ResultType => resultType;

		public string ResultItemId => resultItemId ?? string.Empty;

		public int ResultAmount => Mathf.Max(1, resultAmount);

		public IReadOnlyList<CraftingIngredient> Ingredients => ingredients ?? Array.Empty<CraftingIngredient>();

		public CraftingStationType RequiredStation => requiredStation;

		public Sprite Icon => icon;

		public int SortOrder => sortOrder;

		public WeaponUpgradeConfiguration WeaponUpgrade => weaponUpgrade;

		public bool TryValidate(out string error)
		{
			if (string.IsNullOrWhiteSpace(Id))
			{
				error = "Crafting recipe '" + base.name + "' has no stable ID.";
				return false;
			}
			if (string.IsNullOrWhiteSpace(DisplayName))
			{
				error = "Crafting recipe '" + base.name + "' has no display name.";
				return false;
			}
			if (Ingredients.Count == 0)
			{
				error = "Crafting recipe '" + Id + "' has no ingredients.";
				return false;
			}
			if (Ingredients.Count > 5)
			{
				error = $"Crafting recipe '{Id}' has {Ingredients.Count} " + "ingredient types; maximum is " + $"{5}.";
				return false;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (CraftingIngredient ingredient in Ingredients)
			{
				if (string.IsNullOrWhiteSpace(ingredient.ItemId))
				{
					error = "Crafting recipe '" + Id + "' has an empty ingredient ID.";
					return false;
				}
				if (!hashSet.Add(ingredient.ItemId))
				{
					error = "Crafting recipe '" + Id + "' contains duplicate ingredient '" + ingredient.ItemId + "'.";
					return false;
				}
			}
			if (ResultType == CraftingResultType.Item && string.IsNullOrWhiteSpace(ResultItemId))
			{
				error = "Item recipe '" + Id + "' has no result item ID.";
				return false;
			}
			if (ResultType == CraftingResultType.WeaponUpgrade && string.IsNullOrWhiteSpace(WeaponUpgrade.WeaponId))
			{
				error = "Upgrade recipe '" + Id + "' has no weapon ID.";
				return false;
			}
			error = string.Empty;
			return true;
		}
	}

	[Serializable]
	public struct CraftingIngredient
	{
		[SerializeField]
		private string itemId;

		[SerializeField]
		[Min(1f)]
		private int amount;

		public string ItemId => itemId ?? string.Empty;

		public int Amount => Mathf.Max(1, amount);
	}

	public static class CraftingRecipeIds
	{
		public const string HealingPotion = "craft_healing_potion";

		public const string CopperBar = "craft_copper_bar";

		public const string Axe = "craft_axe";

		public const string Scythe = "craft_scythe";

		public const string Pickaxe = "craft_pickaxe";

		public const string Hammer = "craft_hammer";

		public const string Daggers = "craft_daggers";

		public const string Plank = "craft_plank";

		public const string Rope = "craft_rope";

		public const string StoneBlock = "craft_stone_block";

		public const string CatchDevice = "craft_catch_device";

		public const string Battery = "craft_battery";

		public const string WandererHood = "craft_wanderer_hood";

		public const string WandererCoat = "craft_wanderer_coat";

		public const string WandererBracers = "craft_wanderer_bracers";

		public const string WandererLegs = "craft_wanderer_legs";

		public const string Bread = "craft_bread";

		public const string HardwoodPlank = "craft_hardwood_plank";

		public const string CutGranite = "craft_cut_granite";

		public const string RobustCloth = "craft_robust_cloth";

		public const string IronBar = "craft_iron_bar";

		public const string CopperHammer = "craft_copper_hammer";

		public const string CopperDaggers = "craft_copper_daggers";

		public const string CopperSpear = "craft_copper_spear";

		public const string CopperHelmet = "craft_copper_helmet";

		public const string CopperChest = "craft_copper_chest";

		public const string CopperGloves = "craft_copper_gloves";

		public const string CopperLegs = "craft_copper_legs";

		public const string CopperAxe = "craft_copper_axe";

		public const string CopperPickaxe = "craft_copper_pickaxe";

		public const string CopperScythe = "craft_copper_scythe";

		public const string IronHammer = "craft_iron_hammer";

		public const string IronDaggers = "craft_iron_daggers";

		public const string IronSpear = "craft_iron_spear";

		public const string IronHelmet = "craft_iron_helmet";

		public const string IronChest = "craft_iron_chest";

		public const string IronGloves = "craft_iron_gloves";

		public const string IronLegs = "craft_iron_legs";

		public const string IronAxe = "craft_iron_axe";

		public const string IronPickaxe = "craft_iron_pickaxe";

		public const string IronScythe = "craft_iron_scythe";
	}

	[Serializable]
	public struct WeaponUpgradeConfiguration
	{
		[SerializeField]
		private string weaponId;

		[SerializeField]
		[Min(1f)]
		private int targetLevel;

		[SerializeField]
		[Min(1f)]
		private float healthDamageMultiplier;

		[SerializeField]
		[Min(1f)]
		private float staggerDamageMultiplier;

		public string WeaponId => weaponId ?? string.Empty;

		public int TargetLevel => Mathf.Max(1, targetLevel);

		public float HealthDamageMultiplier => Mathf.Max(1f, healthDamageMultiplier);

		public float StaggerDamageMultiplier => Mathf.Max(1f, staggerDamageMultiplier);
	}
}
