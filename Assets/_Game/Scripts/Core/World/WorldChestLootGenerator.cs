using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public static class WorldChestLootGenerator
	{
		public static ItemStack[] Generate(WorldChestLootProfile profile, WorldChestFamily family, string chestInstanceId, int deterministicSeed, WorldChestProgressContext progress, ContentDatabase content)
		{
			if (profile == null)
			{
				throw new ArgumentNullException("profile");
			}
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			if (string.IsNullOrWhiteSpace(chestInstanceId))
			{
				throw new ArgumentException("A stable chest instance ID is required.");
			}
			string[] validationErrors = profile.GetValidationErrors();
			if (validationErrors.Length != 0)
			{
				throw new InvalidOperationException(string.Join("; ", validationErrors));
			}
			Random random = new Random(deterministicSeed);
			ItemStack[] result = new ItemStack[24];
			int nextSlot = 0;
			int amount = ((family == WorldChestFamily.Common) ? random.Next(1, 4) : random.Next(2, 5));
			AddRandom(profile.RegionalRawItemIds, amount, chestInstanceId, random, content, result, ref nextSlot);
			if (1 == 0)
			{
			}
			IReadOnlyList<string> readOnlyList = family switch
			{
				WorldChestFamily.Common => profile.SmallConsumableItemIds, 
				WorldChestFamily.Guarded => profile.UsefulConsumableItemIds, 
				_ => profile.ValuableConsumableItemIds, 
			};
			if (1 == 0)
			{
			}
			IReadOnlyList<string> pool = readOnlyList;
			AddRandom(pool, 1, chestInstanceId, random, content, result, ref nextSlot);
			if (1 == 0)
			{
			}
			int num = family switch
			{
				WorldChestFamily.Guarded => 20, 
				WorldChestFamily.Hidden => 50, 
				_ => 0, 
			};
			if (1 == 0)
			{
			}
			int percent = num;
			if (profile.RegionalRefinementItemIds.Count > 0 && RollPercent(random, percent))
			{
				AddRandom(profile.RegionalRefinementItemIds, random.Next(1, 3), chestInstanceId, random, content, result, ref nextSlot);
			}
			if (1 == 0)
			{
			}
			num = family switch
			{
				WorldChestFamily.Common => 1, 
				WorldChestFamily.Guarded => 5, 
				WorldChestFamily.Hidden => 10, 
				_ => 0, 
			};
			if (1 == 0)
			{
			}
			int percent2 = num;
			if (RollPercent(random, percent2))
			{
				int tier = RollEquipmentTier(profile, progress, random);
				AddEquipment(profile, tier, family, chestInstanceId, random, content, result, ref nextSlot);
			}
			if (profile.IsTierTwoZone && family == WorldChestFamily.Hidden)
			{
				if (RollPercent(random, 10))
				{
					Add("smithing_fitting", 1, -1, chestInstanceId, content, result, ref nextSlot);
				}
				if (progress.TierTwoUnlocked && RollBasisPoints(random, 100))
				{
					string itemId = Pick(profile.NamedWeaponItemIds, random);
					Add(itemId, 1, -1, chestInstanceId, content, result, ref nextSlot);
				}
			}
			if (nextSlot < 2)
			{
				throw new InvalidOperationException("A generated chest lost its guaranteed core.");
			}
			return result;
		}

		private static int RollEquipmentTier(WorldChestLootProfile profile, WorldChestProgressContext progress, Random random)
		{
			int num = random.Next(100);
			if (profile.IsTierTwoZone)
			{
				return (num >= 25) ? ((num < 60) ? 1 : 2) : 0;
			}
			if (!progress.GaronDefeated)
			{
				return 0;
			}
			return (num >= 50) ? 1 : 0;
		}

		private static void AddEquipment(WorldChestLootProfile profile, int tier, WorldChestFamily family, string chestId, Random random, ContentDatabase content, ItemStack[] result, ref int nextSlot)
		{
			List<string> list = new List<string>();
			foreach (string regularEquipmentItemId in profile.RegularEquipmentItemIds)
			{
				if (content.TryGetItem(regularEquipmentItemId, out var value) && value.Tier == tier && (value.Category == ItemCategory.Weapon || value.Category == ItemCategory.Armor || value.Category == ItemCategory.Tool))
				{
					list.Add(regularEquipmentItemId);
				}
			}
			if (list.Count == 0)
			{
				throw new InvalidOperationException($"No regular T{tier} equipment is authored.");
			}
			string text = list[random.Next(list.Count)];
			ItemDefinition item = content.GetItem(text);
			int durability = -1;
			if (family == WorldChestFamily.Common && item.MaximumDurability > 0)
			{
				int num = random.Next(40, 81);
				durability = Math.Max(1, (int)Math.Ceiling((double)(item.MaximumDurability * num) / 100.0));
			}
			Add(text, 1, durability, chestId, content, result, ref nextSlot);
		}

		private static void AddRandom(IReadOnlyList<string> pool, int amount, string chestId, Random random, ContentDatabase content, ItemStack[] result, ref int nextSlot)
		{
			Add(Pick(pool, random), amount, -1, chestId, content, result, ref nextSlot);
		}

		private static void Add(string itemId, int amount, int durability, string chestId, ContentDatabase content, ItemStack[] result, ref int nextSlot)
		{
			if (nextSlot >= result.Length)
			{
				throw new InvalidOperationException("World chest slot budget exceeded.");
			}
			ItemDefinition item = content.GetItem(itemId);
			if (amount > item.MaximumStackSize)
			{
				throw new InvalidOperationException("Chest amount for '" + itemId + "' exceeds its stack size.");
			}
			string uniqueInstanceId = ((item.MaximumDurability > 0) ? $"{chestId}.item.{nextSlot + 1:00}" : string.Empty);
			result[nextSlot] = ItemStack.Create(item, amount, uniqueInstanceId, durability);
			nextSlot++;
		}

		private static string Pick(IReadOnlyList<string> pool, Random random)
		{
			if (pool == null || pool.Count == 0)
			{
				throw new InvalidOperationException("A required world-chest pool is empty.");
			}
			return pool[random.Next(pool.Count)];
		}

		private static bool RollPercent(Random random, int percent)
		{
			return percent > 0 && random.Next(100) < percent;
		}

		private static bool RollBasisPoints(Random random, int basisPoints)
		{
			return basisPoints > 0 && random.Next(10000) < basisPoints;
		}
	}

	public readonly struct WorldChestProgressContext
	{
		public bool GaronDefeated { get; }

		public bool TierTwoUnlocked { get; }

		public WorldChestProgressContext(bool garonDefeated, bool tierTwoUnlocked)
		{
			GaronDefeated = garonDefeated;
			TierTwoUnlocked = tierTwoUnlocked;
		}
	}
}
