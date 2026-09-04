using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Items/Item Definition")]
	public sealed class ItemDefinition : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private string displayName;

		[SerializeField]
		[TextArea(2, 4)]
		private string description;

		[SerializeField]
		private ItemCategory category;

		[SerializeField]
		private string materialFamily = string.Empty;

		[SerializeField]
		private int tier;

		[SerializeField]
		private int maximumDurability;

		[SerializeField]
		[Range(0f, 1f)]
		private float protectionContribution;

		[SerializeField]
		private Sprite icon;

		[Tooltip("Optional compact HUD glyph. Falls leer, wird das Itembild verwendet.")]
		[SerializeField]
		private Sprite interactionIcon;

		[SerializeField]
		[Min(1f)]
		private int maximumStackSize = 1;

		[SerializeField]
		private bool canBeUsed;

		[SerializeField]
		private bool canBeDropped = true;

		[SerializeField]
		private bool canBeStored = true;

		[SerializeField]
		private bool canBeCrafted;

		[SerializeField]
		private int sellValue = -1;

		[SerializeField]
		private string[] tags = Array.Empty<string>();

		[SerializeField]
		private GameObject worldDropPrefab;

		[SerializeField]
		private ItemUseConfiguration useConfiguration;

		[SerializeField]
		private WearableSlot wearableSlot;

		public string Id => id ?? string.Empty;

		public string DisplayName => displayName ?? string.Empty;

		public string Description => description ?? string.Empty;

		public ItemCategory Category => category;

		public string MaterialFamily => materialFamily ?? string.Empty;

		public int Tier => Mathf.Max(0, tier);

		public int MaximumDurability => Mathf.Max(0, maximumDurability);

		public float ProtectionContribution => Mathf.Clamp01(protectionContribution);

		public Sprite Icon => icon;

		public Sprite InteractionIcon => (interactionIcon != null) ? interactionIcon : icon;

		public int MaximumStackSize => Mathf.Max(1, maximumStackSize);

		public bool CanBeUsed => canBeUsed;

		public bool CanBeDropped => canBeDropped;

		public bool CanBeStored => canBeStored;

		public bool CanBeCrafted => canBeCrafted;

		public bool HasSellValue => sellValue >= 0;

		public int SellValue => sellValue;

		public IReadOnlyList<string> Tags => tags ?? Array.Empty<string>();

		public GameObject WorldDropPrefab => worldDropPrefab;

		public ItemUseConfiguration UseConfiguration => useConfiguration;

		public WearableSlot WearableSlot => wearableSlot;

		public ItemDefinitionValidation ValidateDefinition()
		{
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			if (string.IsNullOrWhiteSpace(id))
			{
				list.Add("Stable item ID is empty.");
			}
			if (string.IsNullOrWhiteSpace(displayName))
			{
				list.Add("Item '" + base.name + "' has no display name.");
			}
			if (!Enum.IsDefined(typeof(ItemCategory), category) || category == ItemCategory.Unknown)
			{
				list.Add("Item '" + base.name + "' has an unknown category value " + $"'{(int)category}'.");
			}
			if (maximumStackSize < 1)
			{
				list.Add("Item '" + base.name + "' has invalid maximum stack size " + $"{maximumStackSize}.");
			}
			if (tier < 0)
			{
				list.Add($"Item '{base.name}' has a negative tier {tier}.");
			}
			if (maximumDurability < 0)
			{
				list.Add("Item '" + base.name + "' has a negative maximum durability " + $"{maximumDurability}.");
			}
			if ((category == ItemCategory.Weapon || category == ItemCategory.Armor) && maximumDurability <= 0)
			{
				list.Add("Durable item '" + base.name + "' requires positive maximum durability.");
			}
			if (maximumDurability > 0 && maximumStackSize != 1)
			{
				list.Add("Durable item '" + base.name + "' must have stack size 1.");
			}
			if (protectionContribution < 0f || protectionContribution > 1f)
			{
				list.Add("Item '" + base.name + "' has invalid protection contribution " + $"{protectionContribution}.");
			}
			if (category == ItemCategory.Armor && protectionContribution <= 0f)
			{
				list.Add("Armor item '" + base.name + "' requires a positive direct protection contribution.");
			}
			if (category != ItemCategory.Armor && protectionContribution != 0f)
			{
				list.Add("Non-armor item '" + base.name + "' declares protection.");
			}
			if (!string.IsNullOrWhiteSpace(materialFamily) && !MaterialFamilies.IsKnown(materialFamily))
			{
				list.Add("Item '" + base.name + "' has unknown material family '" + materialFamily + "'.");
			}
			if (category == ItemCategory.Material && string.IsNullOrWhiteSpace(materialFamily))
			{
				list.Add("Item '" + base.name + "' of category 'Material' has no material family.");
			}
			if (canBeUsed && !useConfiguration.IsConfigured)
			{
				list.Add("Usable item '" + base.name + "' has no use configuration.");
			}
			if (!Enum.IsDefined(typeof(ItemUseActionType), useConfiguration.ActionType))
			{
				list.Add("Item '" + base.name + "' has an unknown use action value " + $"'{(int)useConfiguration.ActionType}'.");
			}
			if (useConfiguration.ActionType == ItemUseActionType.Heal && useConfiguration.HealingAmount <= 0f)
			{
				list.Add("Healing item '" + base.name + "' requires a positive healing amount.");
			}
			if (useConfiguration.ActionType == ItemUseActionType.TimedCombatBuff && (useConfiguration.Duration <= 0f || useConfiguration.DamageMultiplier <= 0f || useConfiguration.StaggerDamageMultiplier <= 0f))
			{
				list.Add("Timed combat buff '" + base.name + "' requires positive duration and multipliers.");
			}
			if (!canBeUsed && useConfiguration.IsConfigured)
			{
				list2.Add("Item '" + base.name + "' has use data but is not marked usable.");
			}
			if (category == ItemCategory.Armor && wearableSlot == WearableSlot.None)
			{
				list.Add("Armor item '" + base.name + "' has no wearable slot.");
			}
			if (category != ItemCategory.Armor && wearableSlot != WearableSlot.None)
			{
				list.Add("Non-armor item '" + base.name + "' declares wearable slot " + $"'{wearableSlot}'.");
			}
			if (icon == null)
			{
				list2.Add("Item '" + base.name + "' has no icon.");
			}
			if (sellValue < -1)
			{
				list.Add($"Item '{base.name}' has invalid sell value {sellValue}. " + "Use -1 for no value.");
			}
			return new ItemDefinitionValidation(list.ToArray(), list2.ToArray());
		}
	}

	public readonly struct ItemDefinitionValidation
	{
		public readonly string[] Errors;

		public readonly string[] Warnings;

		public bool IsValid => Errors.Length == 0;

		public ItemDefinitionValidation(string[] errors, string[] warnings)
		{
			Errors = errors ?? Array.Empty<string>();
			Warnings = warnings ?? Array.Empty<string>();
		}
	}

	public static class ItemIds
	{
		public const string Wood = "wood";

		public const string Stone = "stone";

		public const string PlantFiber = "plant_fiber";

		public const string CopperOre = "copper_ore";

		public const string CopperBar = "copper_bar";

		public const string HealingPotion = "healing_potion";

		public const string BuffFood = "buff_food";

		public const string Berry = "berry";

		public const string WheatSeed = "wheat_seed";

		public const string Axe = "axe";

		public const string Scythe = "scythe";

		public const string Pickaxe = "pickaxe";

		public const string Plank = "plank";

		public const string Rope = "rope";

		public const string StoneBlock = "stone_block";

		public const string Wheat = "wheat";

		public const string CatchDevice = "catch_device";

		public const string Battery = "battery";

		public const string Hammer = "hammer";

		public const string Daggers = "daggers";

		public const string CopperHammer = "copper_hammer";

		public const string IronHammer = "iron_hammer";

		public const string Sealbreaker = "sealbreaker";

		public const string CopperDaggers = "copper_daggers";

		public const string IronDaggers = "iron_daggers";

		public const string AshFangs = "ash_fangs";

		public const string CopperSpear = "copper_spear";

		public const string IronSpear = "iron_spear";

		public const string EmberThorn = "ember_thorn";

		public const string WandererHood = "armor_wanderer_hood";

		public const string WandererCoat = "armor_wanderer_coat";

		public const string WandererBracers = "armor_wanderer_bracers";

		public const string WandererLegs = "armor_wanderer_legs";

		public const string Hardwood = "hardwood";

		public const string HardwoodPlank = "hardwood_plank";

		public const string Granite = "granite";

		public const string CutGranite = "cut_granite";

		public const string SwampHemp = "swamp_hemp";

		public const string RobustCloth = "robust_cloth";

		public const string IronOre = "iron_ore";

		public const string IronBar = "iron_bar";

		public const string SmithingFitting = "smithing_fitting";

		public const string SmithingMark = "smithing_mark";

		public const string CopperAxe = "copper_axe";

		public const string CopperScythe = "copper_scythe";

		public const string CopperPickaxe = "copper_pickaxe";

		public const string IronAxe = "iron_axe";

		public const string IronScythe = "iron_scythe";

		public const string IronPickaxe = "iron_pickaxe";

		public const string CopperHelmet = "armor_copper_helmet";

		public const string CopperChest = "armor_copper_chest";

		public const string CopperGloves = "armor_copper_gloves";

		public const string CopperLegs = "armor_copper_legs";

		public const string IronHelmet = "armor_iron_helmet";

		public const string IronChest = "armor_iron_chest";

		public const string IronGloves = "armor_iron_gloves";

		public const string IronLegs = "armor_iron_legs";

		public const string CopperSpearBlueprint = "blueprint_item_copper_spear";
	}

	[Serializable]
	public struct ItemUseConfiguration
	{
		[SerializeField]
		private ItemUseActionType actionType;

		[SerializeField]
		[Min(0f)]
		private float healingAmount;

		[SerializeField]
		[Min(0f)]
		private float duration;

		[SerializeField]
		[Min(0f)]
		private float damageMultiplier;

		[SerializeField]
		[Min(0f)]
		private float staggerDamageMultiplier;

		public ItemUseActionType ActionType => actionType;

		public float HealingAmount => Mathf.Max(0f, healingAmount);

		public float Duration => Mathf.Max(0f, duration);

		public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);

		public float StaggerDamageMultiplier => Mathf.Max(0f, staggerDamageMultiplier);

		public bool IsConfigured => actionType != ItemUseActionType.None;
	}

	public static class MaterialFamilies
	{
		public const string Wood = "wood";

		public const string Stone = "stone";

		public const string Fiber = "fiber";

		public const string Ore = "ore";

		public static bool IsKnown(string materialFamily)
		{
			switch (materialFamily)
			{
			case "wood":
			case "stone":
			case "fiber":
			case "ore":
				return true;
			default:
				return false;
			}
		}
	}
}
