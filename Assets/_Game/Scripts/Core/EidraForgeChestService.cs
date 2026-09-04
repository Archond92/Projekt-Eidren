using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class EidraForgeChestService
	{
		private readonly EidraForgeDungeonService _dungeon;

		public EidraForgeChestService(EidraForgeDungeonService dungeon)
		{
			_dungeon = dungeon ?? throw new ArgumentNullException("dungeon");
		}

		public void EnsureRunChestsGenerated(EidraForgeLootProfile profile, ContentDatabase content)
		{
			EidraForgeRunState mutableState = _dungeon.MutableState;
			if (mutableState.Status != EidraForgeRunStatus.InProgress && mutableState.Status != EidraForgeRunStatus.Completed)
			{
				throw new InvalidOperationException("The Eidra Forge has no active run.");
			}
			if (!EidraForgeContainerRules.HasCanonicalPopulation(mutableState.Chests))
			{
				mutableState.Chests = EidraForgeContainerRules.CreateEmptyRunChests();
			}
			ForgeContainerState[] chests = mutableState.Chests;
			foreach (ForgeContainerState forgeContainerState in chests)
			{
				if (!forgeContainerState.Opened && !HasContents(forgeContainerState.Slots))
				{
					EidraForgeChestFamily family = (EidraForgeChestFamily)forgeContainerState.Family;
					forgeContainerState.Slots = EidraForgeLootGenerator.GenerateRunChest(profile, family, forgeContainerState.InstanceId, mutableState.LootSeed, mutableState.RepeatRun, content);
				}
			}
		}

		public ForgeContainerState GetRunChest(string instanceId)
		{
			ForgeContainerState[] chests = _dungeon.MutableState.Chests;
			foreach (ForgeContainerState forgeContainerState in chests)
			{
				if (forgeContainerState != null && string.Equals(forgeContainerState.InstanceId, instanceId, StringComparison.Ordinal))
				{
					return forgeContainerState.Copy();
				}
			}
			return null;
		}

		public bool UpdateRunChestContents(string instanceId, ItemStack[] remaining)
		{
			ForgeContainerState[] chests = _dungeon.MutableState.Chests;
			foreach (ForgeContainerState forgeContainerState in chests)
			{
				if (forgeContainerState != null && string.Equals(forgeContainerState.InstanceId, instanceId, StringComparison.Ordinal))
				{
					if (!DoesNotIncrease(forgeContainerState.Slots, remaining))
					{
						return false;
					}
					forgeContainerState.Opened = true;
					forgeContainerState.Slots = DungeonStateCopy.Stacks(remaining);
					return true;
				}
			}
			return false;
		}

		public ForgeRewardChestState GetRewardChest(ForgeRewardChestSize size)
		{
			return FindReward(size)?.Copy() ?? new ForgeRewardChestState
			{
				Size = (int)size,
				Slots = Array.Empty<ItemStack>()
			};
		}

		public bool TryPurchaseRewardChest(ForgeRewardChestSize size, PlayerInventory inventory, EidraForgeLootProfile profile, ContentDatabase content, out InventoryTransactionFailure failure)
		{
			failure = InventoryTransactionFailure.InvalidRequest;
			if (inventory == null || !Enum.IsDefined(typeof(ForgeRewardChestSize), size))
			{
				return false;
			}
			ForgeRewardChestState orCreateReward = GetOrCreateReward(size);
			if (HasContents(orCreateReward.Slots))
			{
				return false;
			}
			ItemStack[] slots = EidraForgeLootGenerator.GenerateRewardChest(profile, size, _dungeon.MutableState.LootSeed, orCreateReward.PurchaseIndex, content);
			InventoryItemAmount[] removals = new InventoryItemAmount[1]
			{
				new InventoryItemAmount("smithing_mark", Price(size))
			};
			if (!inventory.TryApplyTransaction(removals, string.Empty, 0, out failure))
			{
				return false;
			}
			orCreateReward.Slots = slots;
			orCreateReward.PurchaseIndex++;
			return true;
		}

		public bool UpdateRewardChestContents(ForgeRewardChestSize size, ItemStack[] remaining)
		{
			ForgeRewardChestState forgeRewardChestState = FindReward(size);
			if (forgeRewardChestState == null || !DoesNotIncrease(forgeRewardChestState.Slots, remaining))
			{
				return false;
			}
			forgeRewardChestState.Slots = DungeonStateCopy.Stacks(remaining);
			return true;
		}

		public static int Price(ForgeRewardChestSize size)
		{
			if (1 == 0)
			{
			}
			int result = size switch
			{
				ForgeRewardChestSize.Small => 15, 
				ForgeRewardChestSize.Medium => 30, 
				ForgeRewardChestSize.Large => 90, 
				_ => throw new ArgumentOutOfRangeException("size"), 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private ForgeRewardChestState GetOrCreateReward(ForgeRewardChestSize size)
		{
			ForgeRewardChestState forgeRewardChestState = FindReward(size);
			if (forgeRewardChestState != null)
			{
				return forgeRewardChestState;
			}
			List<ForgeRewardChestState> list = new List<ForgeRewardChestState>(_dungeon.MutableState.RewardChests ?? Array.Empty<ForgeRewardChestState>());
			forgeRewardChestState = new ForgeRewardChestState
			{
				Size = (int)size,
				Slots = Array.Empty<ItemStack>()
			};
			list.Add(forgeRewardChestState);
			_dungeon.MutableState.RewardChests = list.ToArray();
			return forgeRewardChestState;
		}

		private ForgeRewardChestState FindReward(ForgeRewardChestSize size)
		{
			ForgeRewardChestState[] array = _dungeon.MutableState.RewardChests ?? Array.Empty<ForgeRewardChestState>();
			foreach (ForgeRewardChestState forgeRewardChestState in array)
			{
				if (forgeRewardChestState != null && forgeRewardChestState.Size == (int)size)
				{
					return forgeRewardChestState;
				}
			}
			return null;
		}

		private static bool HasContents(ItemStack[] slots)
		{
			if (slots == null)
			{
				return false;
			}
			foreach (ItemStack itemStack in slots)
			{
				if (!itemStack.IsEmpty)
				{
					return true;
				}
			}
			return false;
		}

		private static bool DoesNotIncrease(ItemStack[] before, ItemStack[] after)
		{
			if (after == null)
			{
				return false;
			}
			List<ItemStack> list = new List<ItemStack>(before ?? Array.Empty<ItemStack>());
			for (int i = 0; i < after.Length; i++)
			{
				ItemStack candidate = after[i];
				if (!candidate.IsEmpty)
				{
					int num = list.FindIndex((ItemStack old) => string.Equals(old.ItemId, candidate.ItemId, StringComparison.Ordinal) && string.Equals(old.InstanceId, candidate.InstanceId, StringComparison.Ordinal) && old.Durability == candidate.Durability && old.Quantity >= candidate.Quantity);
					if (num < 0)
					{
						return false;
					}
					list.RemoveAt(num);
				}
			}
			return true;
		}
	}
}
