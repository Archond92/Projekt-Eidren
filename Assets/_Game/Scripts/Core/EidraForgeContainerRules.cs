using Eidren.Data;
using System;

namespace Eidren.Core.Services
{
	public static class EidraForgeContainerRules
	{
		public const int RunChestCount = 7;

		public static ForgeContainerState[] CreateEmptyRunChests()
		{
			return new ForgeContainerState[7]
			{
				Chest("forge.supply.01", EidraForgeChestFamily.Supply),
				Chest("forge.supply.02", EidraForgeChestFamily.Supply),
				Chest("forge.supply.03", EidraForgeChestFamily.Supply),
				Chest("forge.optional.01", EidraForgeChestFamily.Optional),
				Chest("forge.optional.02", EidraForgeChestFamily.Optional),
				Chest("forge.elite.01", EidraForgeChestFamily.Elite),
				Chest("forge.completion.01", EidraForgeChestFamily.Completion)
			};
		}

		public static bool HasCanonicalPopulation(ForgeContainerState[] chests)
		{
			if (chests == null || chests.Length != 7)
			{
				return false;
			}
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			foreach (ForgeContainerState forgeContainerState in chests)
			{
				if (forgeContainerState == null || string.IsNullOrWhiteSpace(forgeContainerState.InstanceId))
				{
					return false;
				}
				switch ((EidraForgeChestFamily)forgeContainerState.Family)
				{
				case EidraForgeChestFamily.Supply:
					num++;
					break;
				case EidraForgeChestFamily.Optional:
					num2++;
					break;
				case EidraForgeChestFamily.Elite:
					num3++;
					break;
				case EidraForgeChestFamily.Completion:
					num4++;
					break;
				default:
					return false;
				}
			}
			if (num == 3 && num2 == 2 && num3 == 1)
			{
				return num4 == 1;
			}
			return false;
		}

		private static ForgeContainerState Chest(string id, EidraForgeChestFamily family)
		{
			return new ForgeContainerState
			{
				InstanceId = id,
				Family = (int)family,
				Slots = Array.Empty<ItemStack>()
			};
		}
	}
}
