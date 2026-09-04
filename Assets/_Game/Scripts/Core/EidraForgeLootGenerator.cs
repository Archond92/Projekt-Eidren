using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public static class EidraForgeLootGenerator
	{
		public static ItemStack[] GenerateRunChest(EidraForgeLootProfile profile, EidraForgeChestFamily family, string chestId, int runSeed, bool repeatCompletion, ContentDatabase content)
		{
			Validate(profile, chestId, content);
			Random random = RandomFor(runSeed, chestId, 0);
			List<ItemStack> list = new List<ItemStack>(5);
			EidraForgeLootChances chances = profile.Chances;
			switch (family)
			{
			case EidraForgeChestFamily.Supply:
				AddRandom(list, profile.Consumables, random.Next(1, 3), chestId, random, content);
				AddRandom(list, profile.CommonMaterials, random.Next(1, 4), chestId, random, content);
				AddChanceEquipment(list, profile, chances.SupplyEquipment, worn: true, chestId, random, content);
				break;
			case EidraForgeChestFamily.Optional:
				AddRandom(list, profile.RefinedMaterials, random.Next(1, 3), chestId, random, content);
				AddRandom(list, profile.Consumables, 1, chestId, random, content);
				AddChanceEquipment(list, profile, chances.OptionalEquipment, worn: false, chestId, random, content);
				AddChanceItem(list, "smithing_fitting", chances.OptionalFitting, chestId, random, content);
				break;
			case EidraForgeChestFamily.Elite:
				AddRandom(list, profile.RefinedMaterials, random.Next(2, 4), chestId, random, content);
				AddRandom(list, profile.Consumables, 2, chestId, random, content);
				AddChanceEquipment(list, profile, chances.EliteEquipment, worn: false, chestId, random, content);
				AddChanceItem(list, "smithing_fitting", chances.EliteFitting, chestId, random, content);
				break;
			case EidraForgeChestFamily.Completion:
				Add(list, "smithing_fitting", 2, worn: false, chestId, content);
				AddEquipment(list, profile.RegularTierOneEquipment, worn: false, chestId, random, content);
				AddRandom(list, profile.HighQualityConsumables, repeatCompletion ? random.Next(1, 3) : random.Next(2, 4), chestId, random, content);
				AddChanceEquipment(list, profile.NamedWeapons, repeatCompletion ? chances.RepeatCompletionNamedWeapon : chances.FirstCompletionNamedWeapon, worn: false, chestId, random, content);
				break;
			default:
				throw new ArgumentOutOfRangeException("family");
			}
			return list.ToArray();
		}

		public static ItemStack[] GenerateRewardChest(EidraForgeLootProfile profile, ForgeRewardChestSize size, int seed, int purchaseIndex, ContentDatabase content)
		{
			Validate(profile, $"forge.reward.{size}", content);
			string text = $"forge.reward.{size.ToString().ToLowerInvariant()}.{purchaseIndex}";
			Random random = RandomFor(seed, text, purchaseIndex);
			List<ItemStack> list = new List<ItemStack>(5);
			if (1 == 0)
			{
			}
			int num = size switch
			{
				ForgeRewardChestSize.Small => 1, 
				ForgeRewardChestSize.Medium => 2, 
				ForgeRewardChestSize.Large => 5, 
				_ => throw new ArgumentOutOfRangeException("size"), 
			};
			if (1 == 0)
			{
			}
			int amount = num;
			Add(list, "smithing_fitting", amount, worn: false, text, content);
			int amount2 = ((size == ForgeRewardChestSize.Large) ? random.Next(3, 5) : random.Next(1, 3));
			AddRandom(list, profile.HighQualityConsumables, amount2, text, random, content);
			if (size != ForgeRewardChestSize.Small)
			{
				AddEquipment(list, profile.RegularTierOneEquipment, worn: false, text, random, content);
			}
			if (1 == 0)
			{
			}
			num = size switch
			{
				ForgeRewardChestSize.Small => 5, 
				ForgeRewardChestSize.Medium => 10, 
				_ => 25, 
			};
			if (1 == 0)
			{
			}
			int percent = num;
			AddChanceEquipment(list, profile, percent, worn: false, text, random, content);
			if (size == ForgeRewardChestSize.Large)
			{
				AddChanceEquipment(list, profile.NamedWeapons, 25, worn: false, text, random, content);
			}
			return list.ToArray();
		}

		private static void AddChanceEquipment(List<ItemStack> result, EidraForgeLootProfile profile, int percent, bool worn, string chestId, Random random, ContentDatabase content)
		{
			AddChanceEquipment(result, profile.RegularTierOneEquipment, percent, worn, chestId, random, content);
		}

		private static void AddChanceEquipment(List<ItemStack> result, IReadOnlyList<string> pool, int percent, bool worn, string chestId, Random random, ContentDatabase content)
		{
			if (Roll(random, percent))
			{
				AddEquipment(result, pool, worn, chestId, random, content);
			}
		}

		private static void AddEquipment(List<ItemStack> result, IReadOnlyList<string> pool, bool worn, string chestId, Random random, ContentDatabase content)
		{
			string text = Pick(pool, random);
			ItemDefinition item = content.GetItem(text);
			if (item.Tier != 1 || item.MaximumDurability <= 0 || (item.Category != ItemCategory.Weapon && item.Category != ItemCategory.Armor && item.Category != ItemCategory.Tool))
			{
				throw new InvalidOperationException("Forge equipment '" + text + "' must be durable regular T1 gear.");
			}
			int remainingDurability = item.MaximumDurability;
			if (worn)
			{
				int num = random.Next(40, 81);
				remainingDurability = Math.Max(1, (int)Math.Ceiling((double)(item.MaximumDurability * num) / 100.0));
			}
			result.Add(ItemStack.Create(item, 1, $"{chestId}.item.{result.Count + 1:00}", remainingDurability));
		}

		private static void AddChanceItem(List<ItemStack> result, string itemId, int percent, string chestId, Random random, ContentDatabase content)
		{
			if (Roll(random, percent))
			{
				Add(result, itemId, 1, worn: false, chestId, content);
			}
		}

		private static void AddRandom(List<ItemStack> result, IReadOnlyList<string> pool, int amount, string chestId, Random random, ContentDatabase content)
		{
			Add(result, Pick(pool, random), amount, worn: false, chestId, content);
		}

		private static void Add(List<ItemStack> result, string itemId, int amount, bool worn, string chestId, ContentDatabase content)
		{
			ItemDefinition item = content.GetItem(itemId);
			if (amount > item.MaximumStackSize)
			{
				throw new InvalidOperationException("Forge stack for '" + itemId + "' is too large.");
			}
			int remainingDurability = ((worn && item.MaximumDurability > 0) ? Math.Max(1, item.MaximumDurability * 40 / 100) : (-1));
			string uniqueInstanceId = ((item.MaximumDurability > 0) ? $"{chestId}.item.{result.Count + 1:00}" : string.Empty);
			result.Add(ItemStack.Create(item, amount, uniqueInstanceId, remainingDurability));
		}

		private static string Pick(IReadOnlyList<string> pool, Random random)
		{
			if (pool == null || pool.Count == 0)
			{
				throw new InvalidOperationException("A required forge-loot pool is empty.");
			}
			return pool[random.Next(pool.Count)];
		}

		private static bool Roll(Random random, int percent)
		{
			return percent > 0 && random.Next(100) < percent;
		}

		private static Random RandomFor(int seed, string id, int salt)
		{
			uint num = 2166136261u;
			foreach (char c in id)
			{
				num = (num ^ c) * 16777619;
			}
			return new Random((int)((uint)seed ^ num) ^ (salt * 397));
		}

		private static void Validate(EidraForgeLootProfile profile, string chestId, ContentDatabase content)
		{
			if (profile == null)
			{
				throw new ArgumentNullException("profile");
			}
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			if (string.IsNullOrWhiteSpace(chestId))
			{
				throw new ArgumentException("A stable forge chest ID is required.");
			}
			string[] validationErrors = profile.GetValidationErrors();
			if (validationErrors.Length != 0)
			{
				throw new InvalidOperationException(string.Join("; ", validationErrors));
			}
		}
	}
}
