using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Resources/Harvest Yield Table")]
	public sealed class HarvestYieldTable : ScriptableObject
	{
		[SerializeField]
		[Min(1f)]
		private int handYield = 1;

		[SerializeField]
		private HarvestToolYield[] toolYields = Array.Empty<HarvestToolYield>();

		public int HandYield => Mathf.Max(1, handYield);

		public IReadOnlyList<HarvestToolYield> ToolYields => toolYields ?? Array.Empty<HarvestToolYield>();

		public bool TryGetToolForFamily(string materialFamily, out HarvestToolYield entry)
		{
			if (toolYields != null && !string.IsNullOrWhiteSpace(materialFamily))
			{
				for (int i = 0; i < toolYields.Length; i++)
				{
					if (toolYields[i].Serves(materialFamily))
					{
						entry = toolYields[i];
						return entry.Tool != null;
					}
				}
			}
			entry = default(HarvestToolYield);
			return false;
		}

		public bool TryGetToolById(string toolItemId, out HarvestToolYield entry)
		{
			if (toolYields != null && !string.IsNullOrWhiteSpace(toolItemId))
			{
				for (int i = 0; i < toolYields.Length; i++)
				{
					ItemDefinition tool = toolYields[i].Tool;
					if (tool != null && string.Equals(tool.Id, toolItemId, StringComparison.Ordinal))
					{
						entry = toolYields[i];
						return true;
					}
				}
			}
			entry = default(HarvestToolYield);
			return false;
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (handYield < 1)
			{
				list.Add("Harvest table '" + base.name + "' needs a hand yield of at least 1.");
			}
			if (toolYields == null || toolYields.Length == 0)
			{
				list.Add("Harvest table '" + base.name + "' lists no tools.");
				return list.ToArray();
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> seenFamilyTiers = new HashSet<string>(StringComparer.Ordinal);
			HarvestToolYield[] array = toolYields;
			for (int i = 0; i < array.Length; i++)
			{
				HarvestToolYield entry = array[i];
				if (entry.Tool == null)
				{
					list.Add("Harvest table '" + base.name + "' has an entry without a tool item.");
					continue;
				}
				if (entry.Tool.Category != ItemCategory.Tool)
				{
					list.Add("Item '" + entry.Tool.Id + "' grants a harvest yield but is not of category 'Tool'.");
				}
				if (!hashSet.Add(entry.Tool.Id))
				{
					list.Add("Harvest table '" + base.name + "' lists tool '" + entry.Tool.Id + "' more than once.");
				}
				if (entry.MaterialFamilies.Count == 0)
				{
					list.Add("Tool '" + entry.Tool.Id + "' serves no material family.");
				}
				ValidateFamilies(entry, seenFamilyTiers, list);
			}
			return list.ToArray();
		}

		private static void ValidateFamilies(HarvestToolYield entry, HashSet<string> seenFamilyTiers, List<string> errors)
		{
			foreach (string materialFamily in entry.MaterialFamilies)
			{
				if (!MaterialFamilies.IsKnown(materialFamily))
				{
					errors.Add("Tool '" + entry.Tool.Id + "' names unknown material family '" + materialFamily + "'.");
					continue;
				}
				string item = $"{materialFamily}:{entry.Tool.Tier}";
				if (!seenFamilyTiers.Add(item))
				{
					errors.Add("Material family '" + materialFamily + "' tier " + $"{entry.Tool.Tier} is served by more than one " + "tool.");
				}
			}
		}
	}

	[Serializable]
	public struct HarvestToolYield
	{
		[SerializeField]
		private ItemDefinition tool;

		[SerializeField]
		private string[] materialFamilies;

		[SerializeField]
		[Min(1f)]
		private int yield;

		public ItemDefinition Tool => tool;

		public IReadOnlyList<string> MaterialFamilies => materialFamilies ?? Array.Empty<string>();

		public int Yield => Mathf.Max(1, yield);

		public bool Serves(string materialFamily)
		{
			if (materialFamilies == null || string.IsNullOrWhiteSpace(materialFamily))
			{
				return false;
			}
			for (int i = 0; i < materialFamilies.Length; i++)
			{
				if (string.Equals(materialFamilies[i], materialFamily, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}
	}
}
