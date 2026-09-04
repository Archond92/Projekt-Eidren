using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Loot/World Chest Profile")]
	public sealed class WorldChestLootProfile : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private bool tierTwoZone;

		[SerializeField]
		private string[] regionalRawItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] regionalRefinementItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] smallConsumableItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] usefulConsumableItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] valuableConsumableItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] regularEquipmentItemIds = Array.Empty<string>();

		[SerializeField]
		private string[] namedWeaponItemIds = Array.Empty<string>();

		[SerializeField]
		private GameObject commonPrefab;

		[SerializeField]
		private GameObject guardedPrefab;

		[SerializeField]
		private GameObject hiddenPrefab;

		[SerializeField]
		private GameObject guardedElitePrefab;

		[SerializeField]
		private GameObject[] guardedCompanionPrefabs = Array.Empty<GameObject>();

		public string Id => id ?? string.Empty;

		public bool IsTierTwoZone => tierTwoZone;

		public IReadOnlyList<string> RegionalRawItemIds => regionalRawItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> RegionalRefinementItemIds => regionalRefinementItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> SmallConsumableItemIds => smallConsumableItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> UsefulConsumableItemIds => usefulConsumableItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> ValuableConsumableItemIds => valuableConsumableItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> RegularEquipmentItemIds => regularEquipmentItemIds ?? Array.Empty<string>();

		public IReadOnlyList<string> NamedWeaponItemIds => namedWeaponItemIds ?? Array.Empty<string>();

		public GameObject GuardedElitePrefab => guardedElitePrefab;

		public IReadOnlyList<GameObject> GuardedCompanionPrefabs => guardedCompanionPrefabs ?? Array.Empty<GameObject>();

		public GameObject PrefabFor(WorldChestFamily family)
		{
			if (1 == 0)
			{
			}
			GameObject result = family switch
			{
				WorldChestFamily.Common => commonPrefab, 
				WorldChestFamily.Guarded => guardedPrefab, 
				WorldChestFamily.Hidden => hiddenPrefab, 
				_ => null, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (string.IsNullOrWhiteSpace(Id))
			{
				list.Add("World-chest profile requires a stable ID.");
			}
			ValidateIds(regionalRawItemIds, "regional raw", required: true, list);
			ValidateIds(regionalRefinementItemIds, "regional refinement", required: false, list);
			ValidateIds(smallConsumableItemIds, "small consumable", required: true, list);
			ValidateIds(usefulConsumableItemIds, "useful consumable", required: true, list);
			ValidateIds(valuableConsumableItemIds, "valuable consumable", required: true, list);
			ValidateIds(regularEquipmentItemIds, "regular equipment", required: true, list);
			ValidateIds(namedWeaponItemIds, "named weapon", tierTwoZone, list);
			if (commonPrefab == null || guardedPrefab == null || hiddenPrefab == null)
			{
				list.Add("World-chest profile '" + Id + "' requires all three prefabs.");
			}
			if (tierTwoZone && (guardedElitePrefab == null || guardedCompanionPrefabs == null || guardedCompanionPrefabs.Length == 0))
			{
				list.Add("T2 world-chest profile '" + Id + "' requires its guarded encounter.");
			}
			return list.ToArray();
		}

		private static void ValidateIds(IReadOnlyList<string> values, string label, bool required, List<string> errors)
		{
			if (values == null || values.Count == 0)
			{
				if (required)
				{
					errors.Add("World-chest " + label + " pool is empty.");
				}
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < values.Count; i++)
			{
				if (string.IsNullOrWhiteSpace(values[i]))
				{
					errors.Add("World-chest " + label + " pool contains an empty ID.");
				}
				else if (!hashSet.Add(values[i]))
				{
					errors.Add("World-chest " + label + " pool duplicates '" + values[i] + "'.");
				}
			}
		}
	}
}
