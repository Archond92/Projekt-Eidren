using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	internal static class V02SaveMigration
	{
		internal static void Apply(SaveGameData data)
		{
			SaveGameData saveGameData = data;
			if (saveGameData.player == null)
			{
				saveGameData.player = new SavePlayerData();
			}
			saveGameData = data;
			if (saveGameData.progression == null)
			{
				saveGameData.progression = new SaveProgressionData();
			}
			saveGameData = data;
			if (saveGameData.world == null)
			{
				saveGameData.world = new SaveWorldData();
			}
			MigrateDurableItems(data);
			MigrateExperience(data.progression);
			MigrateTechnology(data.progression);
			MigrateGaronRewards(data);
			data.player.weaponUpgrades = Array.Empty<SaveWeaponUpgradeData>();
		}

		private static void MigrateDurableItems(SaveGameData data)
		{
			MigrateStacks(data.player.inventory, "inventory");
			if (data.player.equipment != null)
			{
				SaveEquipmentEntry[] equipment = data.player.equipment;
				foreach (SaveEquipmentEntry saveEquipmentEntry in equipment)
				{
					if (saveEquipmentEntry?.stack != null)
					{
						MigrateStack(saveEquipmentEntry.stack, $"equipment.{saveEquipmentEntry.slot}");
					}
				}
			}
			if (data.storageContainers != null)
			{
				SaveStorageData[] storageContainers = data.storageContainers;
				foreach (SaveStorageData saveStorageData in storageContainers)
				{
					MigrateStacks(saveStorageData?.slots, "storage." + saveStorageData?.containerId);
				}
			}
		}

		private static void MigrateExperience(SaveProgressionData progression)
		{
			progression.stage = Math.Max(1, progression.stage);
			progression.level = Math.Max(1, Math.Min(20, progression.level));
			int num = ((progression.level < 20) ? (60 + 30 * (progression.level - 1)) : 0);
			if (num <= 0)
			{
				progression.experience = 0;
				return;
			}
			double num2 = Math.Min(1.0, Math.Max(0.0, progression.experience) / (double)num);
			int num3 = ExperienceRules.ExperienceToNextLevel(progression.level);
			progression.experience = Math.Min(num3 - 1, (int)Math.Floor(num2 * (double)num3));
		}

		private static void MigrateTechnology(SaveProgressionData progression)
		{
			HashSet<string> hashSet = new HashSet<string>(progression.unlockedTechnologyNodeIds ?? Array.Empty<string>(), StringComparer.Ordinal);
			int num = 0;
			num += (hashSet.Remove("technology.20.cloth_hood") ? 1 : 0);
			num += (hashSet.Remove("technology.21.cloth_coat") ? 1 : 0);
			num += (hashSet.Remove("technology.22.cloth_bracers") ? 1 : 0);
			num += (hashSet.Remove("technology.23.cloth_shoes") ? 1 : 0);
			int num2 = 0;
			if (num > 0)
			{
				hashSet.Add("technology.20.cloth_hood");
				num2 += num - 1;
			}
			if (hashSet.Remove("technology.19.t1_weapons"))
			{
				num2++;
			}
			progression.availableTechnologyPoints = Math.Max(0, progression.availableTechnologyPoints + num2);
			progression.spentTechnologyPoints = Math.Max(0, progression.spentTechnologyPoints - num2);
			progression.unlockedTechnologyNodeIds = Sorted(hashSet);
		}

		private static void MigrateGaronRewards(SaveGameData data)
		{
			HashSet<string> hashSet = new HashSet<string>(data.world.progressFlags ?? Array.Empty<string>(), StringComparer.Ordinal);
			if (hashSet.Contains("garon_defeated") && !hashSet.Contains("garon_v02_rewards_granted"))
			{
				HashSet<string> values = new HashSet<string>(data.progression.knownBlueprintIds ?? Array.Empty<string>(), StringComparer.Ordinal) { "blueprint.copper_spear" };
				data.progression.knownBlueprintIds = Sorted(values);
				data.progression.availableTechnologyPoints++;
				hashSet.Add("garon_v02_rewards_granted");
				data.world.progressFlags = Sorted(hashSet);
			}
		}

		private static void MigrateStacks(SaveItemStackData[] stacks, string context)
		{
			if (stacks != null)
			{
				for (int i = 0; i < stacks.Length; i++)
				{
					MigrateStack(stacks[i], $"{context}.{i}");
				}
			}
		}

		private static void MigrateStack(SaveItemStackData stack, string context)
		{
			if (stack != null && stack.quantity > 0 && TryGetMaximum(stack.itemId, out var maximum))
			{
				stack.quantity = 1;
				stack.durability = maximum;
				if (string.IsNullOrWhiteSpace(stack.instanceId))
				{
					stack.instanceId = "item.legacy." + context;
				}
			}
		}

		private static bool TryGetMaximum(string itemId, out int maximum)
		{
			if (1 == 0)
			{
			}
			int num;
			switch (itemId)
			{
			case "hammer":
				num = 120;
				break;
			case "daggers":
				num = 220;
				break;
			case "axe":
			case "scythe":
			case "pickaxe":
				num = 100;
				break;
			case "armor_wanderer_hood":
			case "armor_wanderer_coat":
			case "armor_wanderer_bracers":
			case "armor_wanderer_legs":
				num = 80;
				break;
			default:
				num = 0;
				break;
			}
			if (1 == 0)
			{
			}
			maximum = num;
			return maximum > 0;
		}

		private static string[] Sorted(HashSet<string> values)
		{
			string[] array = new string[values.Count];
			values.CopyTo(array);
			Array.Sort(array, StringComparer.Ordinal);
			return array;
		}
	}
}
