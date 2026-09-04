using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Loot/Loot Table")]
	public sealed class LootTableDefinition : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private LootTableEntry[] entries = Array.Empty<LootTableEntry>();

		public string Id => id ?? string.Empty;

		public IReadOnlyList<LootTableEntry> Entries => entries ?? Array.Empty<LootTableEntry>();

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (string.IsNullOrWhiteSpace(id))
			{
				list.Add("Loot table ID is empty.");
			}
			if (entries == null || entries.Length == 0)
			{
				list.Add("Loot table '" + base.name + "' has no entries.");
				return list.ToArray();
			}
			Dictionary<string, float> dictionary = new Dictionary<string, float>(StringComparer.Ordinal);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < entries.Length; i++)
			{
				LootTableEntry lootTableEntry = entries[i];
				string validationError = lootTableEntry.GetValidationError();
				if (!string.IsNullOrEmpty(validationError))
				{
					list.Add($"Entry {i}: {validationError}");
				}
				if (!lootTableEntry.IsGrouped)
				{
					continue;
				}
				string selectionGroup = lootTableEntry.SelectionGroup;
				if (lootTableEntry.Guaranteed)
				{
					if (!hashSet.Add(selectionGroup))
					{
						list.Add("Selection group '" + selectionGroup + "' has more than one guaranteed entry.");
					}
				}
				else
				{
					dictionary.TryGetValue(selectionGroup, out var value);
					dictionary[selectionGroup] = value + lootTableEntry.Chance;
				}
			}
			foreach (KeyValuePair<string, float> item in dictionary)
			{
				if (item.Value > 1.0001f)
				{
					list.Add("Selection group '" + item.Key + "' has combined " + $"chance {item.Value:0.###}, above 1.");
				}
			}
			return list.ToArray();
		}
	}

	[Serializable]
	public struct LootTableEntry
	{
		[SerializeField]
		private string itemId;

		[SerializeField]
		[Range(0f, 1f)]
		private float chance;

		[SerializeField]
		[Min(1f)]
		private int minimumAmount;

		[SerializeField]
		[Min(1f)]
		private int maximumAmount;

		[SerializeField]
		private bool guaranteed;

		[SerializeField]
		private string selectionGroup;

		public string ItemId => itemId ?? string.Empty;

		public float Chance => Mathf.Clamp01(chance);

		public int MinimumAmount => Mathf.Max(1, minimumAmount);

		public int MaximumAmount => Mathf.Max(MinimumAmount, maximumAmount);

		public bool Guaranteed => guaranteed;

		public string SelectionGroup => selectionGroup ?? string.Empty;

		public bool IsGrouped => !string.IsNullOrWhiteSpace(SelectionGroup);

		public LootTableEntry(string stableItemId, float dropChance, int minimum, int maximum, bool isGuaranteed = false, string group = "")
		{
			itemId = stableItemId ?? string.Empty;
			chance = dropChance;
			minimumAmount = minimum;
			maximumAmount = maximum;
			guaranteed = isGuaranteed;
			selectionGroup = group ?? string.Empty;
		}

		internal string GetValidationError()
		{
			if (string.IsNullOrWhiteSpace(itemId))
			{
				return "A loot entry requires a stable item ID.";
			}
			if (chance < 0f || chance > 1f)
			{
				return "Loot entry '" + itemId + "' has invalid chance " + $"{chance}.";
			}
			if (minimumAmount < 1 || maximumAmount < minimumAmount)
			{
				return "Loot entry '" + itemId + "' has invalid amount range " + $"{minimumAmount}..{maximumAmount}.";
			}
			return string.Empty;
		}
	}
}
