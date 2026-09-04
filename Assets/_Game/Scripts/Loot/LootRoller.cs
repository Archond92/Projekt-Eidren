using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Loot
{
	public static class LootRoller
	{
		public static IReadOnlyList<LootRollResult> Roll(LootTableDefinition table, int deterministicSeed)
		{
			return Roll(table, new Random(deterministicSeed));
		}

		public static IReadOnlyList<LootRollResult> Roll(LootTableDefinition table, Random random)
		{
			if (table == null)
			{
				throw new ArgumentNullException("table");
			}
			if (random == null)
			{
				throw new ArgumentNullException("random");
			}
			string[] validationErrors = table.GetValidationErrors();
			if (validationErrors.Length != 0)
			{
				throw new InvalidOperationException("Loot table '" + table.name + "' is invalid: " + string.Join("; ", validationErrors));
			}
			List<LootRollResult> list = new List<LootRollResult>();
			Dictionary<string, int> resultIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
			Dictionary<string, List<LootTableEntry>> dictionary = new Dictionary<string, List<LootTableEntry>>(StringComparer.Ordinal);
			List<string> list2 = new List<string>();
			foreach (LootTableEntry entry in table.Entries)
			{
				if (entry.IsGrouped)
				{
					if (!dictionary.TryGetValue(entry.SelectionGroup, out var value))
					{
						value = new List<LootTableEntry>();
						dictionary.Add(entry.SelectionGroup, value);
						list2.Add(entry.SelectionGroup);
					}
					value.Add(entry);
				}
				else if (entry.Guaranteed || random.NextDouble() < (double)entry.Chance)
				{
					Add(entry, random, list, resultIndexes);
				}
			}
			foreach (string item in list2)
			{
				List<LootTableEntry> list3 = dictionary[item];
				int num = GuaranteedIndex(list3);
				if (num < 0)
				{
					double num2 = random.NextDouble();
					double num3 = 0.0;
					for (int i = 0; i < list3.Count; i++)
					{
						num3 += (double)list3[i].Chance;
						if (num2 < num3)
						{
							num = i;
							break;
						}
					}
				}
				if (num >= 0)
				{
					Add(list3[num], random, list, resultIndexes);
				}
			}
			return list;
		}

		private static int GuaranteedIndex(IReadOnlyList<LootTableEntry> entries)
		{
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries[i].Guaranteed)
				{
					return i;
				}
			}
			return -1;
		}

		private static void Add(LootTableEntry entry, Random random, List<LootRollResult> results, IDictionary<string, int> resultIndexes)
		{
			int num = random.Next(entry.MinimumAmount, entry.MaximumAmount + 1);
			if (resultIndexes.TryGetValue(entry.ItemId, out var value))
			{
				LootRollResult lootRollResult = results[value];
				results[value] = new LootRollResult(lootRollResult.ItemId, lootRollResult.Amount + num);
			}
			else
			{
				resultIndexes.Add(entry.ItemId, results.Count);
				results.Add(new LootRollResult(entry.ItemId, num));
			}
		}
	}

	public readonly struct LootRollResult
	{
		public string ItemId { get; }

		public int Amount { get; }

		public LootRollResult(string itemId, int amount)
		{
			ItemId = itemId ?? string.Empty;
			Amount = amount;
		}
	}
}
