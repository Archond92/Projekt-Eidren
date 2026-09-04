using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Loot/Eidra Forge Profile")]
	public sealed class EidraForgeLootProfile : ScriptableObject
	{
		[SerializeField]
		private string id = "loot.eidra_forge.v02";

		[SerializeField]
		private string[] commonMaterialItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] refinedMaterialItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] consumableItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] highQualityConsumableItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] regularTierOneEquipmentItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] namedWeaponItemIds = Array.Empty<string>();

		[SerializeField]
		private EidraForgeLootChances chances;

		public string Id => id ?? string.Empty;

		public IReadOnlyList<string> CommonMaterials => commonMaterialItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> RefinedMaterials => refinedMaterialItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> Consumables => consumableItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> HighQualityConsumables => highQualityConsumableItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> RegularTierOneEquipment => regularTierOneEquipmentItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> NamedWeapons => namedWeaponItemIds ?? Array.Empty<string>();

		public EidraForgeLootChances Chances => chances;

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (string.IsNullOrWhiteSpace(Id))
			{
				list.Add("Eidra-forge loot profile requires a stable ID.");
			}
			Validate(CommonMaterials, "common material", list);
			Validate(RefinedMaterials, "refined material", list);
			Validate(Consumables, "consumable", list);
			Validate(HighQualityConsumables, "high-quality consumable", list);
			Validate(RegularTierOneEquipment, "regular T1 equipment", list);
			Validate(NamedWeapons, "named weapon", list);
			return list.ToArray();
		}

		private static void Validate(IReadOnlyList<string> values, string label, List<string> errors)
		{
			if (values == null || values.Count == 0)
			{
				errors.Add("Eidra-forge " + label + " pool is empty.");
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (string value in values)
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					errors.Add("Eidra-forge " + label + " pool has an empty ID.");
				}
				else if (!hashSet.Add(value))
				{
					errors.Add("Eidra-forge " + label + " pool duplicates '" + value + "'.");
				}
			}
		}
	}

	[Serializable]
	public struct EidraForgeLootChances
	{
		[SerializeField]
		[Range(0f, 100f)]
		private int supplyEquipment;

		[SerializeField]
		[Range(0f, 100f)]
		private int optionalEquipment;

		[SerializeField]
		[Range(0f, 100f)]
		private int optionalFitting;

		[SerializeField]
		[Range(0f, 100f)]
		private int eliteEquipment;

		[SerializeField]
		[Range(0f, 100f)]
		private int eliteFitting;

		[SerializeField]
		[Range(0f, 100f)]
		private int firstCompletionNamedWeapon;

		[SerializeField]
		[Range(0f, 100f)]
		private int repeatCompletionNamedWeapon;

		public int SupplyEquipment => supplyEquipment;

		public int OptionalEquipment => optionalEquipment;

		public int OptionalFitting => optionalFitting;

		public int EliteEquipment => eliteEquipment;

		public int EliteFitting => eliteFitting;

		public int FirstCompletionNamedWeapon => firstCompletionNamedWeapon;

		public int RepeatCompletionNamedWeapon => repeatCompletionNamedWeapon;
	}
}
