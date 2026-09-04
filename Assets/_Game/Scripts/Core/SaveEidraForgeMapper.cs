using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public static class SaveEidraForgeMapper
	{
		public static SaveEidraForgeData ToSave(EidraForgeRunState source)
		{
			EidraForgeRunState eidraForgeRunState = source ?? EidraForgeRunState.CreateUnstarted();
			return new SaveEidraForgeData
			{
				status = (int)eidraForgeRunState.Status,
				runId = (eidraForgeRunState.RunId ?? string.Empty),
				lootSeed = eidraForgeRunState.LootSeed,
				completedUtcTicks = eidraForgeRunState.CompletedUtcTicks,
				firstCompletionGranted = eidraForgeRunState.FirstCompletionGranted,
				repeatRun = eidraForgeRunState.RepeatRun,
				ignivarCaptured = eidraForgeRunState.IgnivarCaptured,
				teamSelectionPending = eidraForgeRunState.TeamSelectionPending,
				formwallGebaut = eidraForgeRunState.FormwallGebaut,
				formwallErtragOffen = eidraForgeRunState.FormwallErtragOffen,
				enemies = SaveEnemies(eidraForgeRunState.Enemies),
				chests = SaveChests(eidraForgeRunState.Chests),
				drops = SaveDrops(eidraForgeRunState.Drops),
				rewardChests = SaveRewards(eidraForgeRunState.RewardChests),
				recoverySlots = SaveStacks(eidraForgeRunState.RecoverySlots)
			};
		}

		public static EidraForgeRunState ToRuntime(SaveEidraForgeData source, ContentDatabase content, List<SaveQuarantineEntry> quarantine)
		{
			if (source == null)
			{
				return EidraForgeRunState.CreateUnstarted();
			}
			EidraForgeRunStatus eidraForgeRunStatus = (Enum.IsDefined(typeof(EidraForgeRunStatus), source.status) ? ((EidraForgeRunStatus)source.status) : EidraForgeRunStatus.NeverStarted);
			if (eidraForgeRunStatus != EidraForgeRunStatus.NeverStarted && string.IsNullOrWhiteSpace(source.runId))
			{
				eidraForgeRunStatus = EidraForgeRunStatus.NeverStarted;
			}
			return new EidraForgeRunState
			{
				Status = eidraForgeRunStatus,
				RunId = (source.runId ?? string.Empty),
				LootSeed = source.lootSeed,
				CompletedUtcTicks = Math.Max(0L, source.completedUtcTicks),
				FirstCompletionGranted = source.firstCompletionGranted,
				RepeatRun = source.repeatRun,
				IgnivarCaptured = source.ignivarCaptured,
				TeamSelectionPending = source.teamSelectionPending,
				/* Altstaende kennen die beiden Felder nicht; der Standardwert false
				   ist inhaltlich richtig - der Formwall gilt dann als nicht gebaut.
				   Deshalb braucht es hier keine Migration, wohl aber einen Test. */
				FormwallGebaut = source.formwallGebaut,
				FormwallErtragOffen = source.formwallErtragOffen,
				Enemies = LoadEnemies(source.enemies),
				Chests = LoadChests(source.chests, content, quarantine),
				Drops = LoadDrops(source.drops, content, quarantine),
				RewardChests = LoadRewards(source.rewardChests, content, quarantine),
				RecoverySlots = LoadStacks(source.recoverySlots, content, quarantine, "eidraForge.recovery")
			};
		}

		private static SaveForgeEnemyData[] SaveEnemies(ForgeEnemyState[] source)
		{
			if (source == null)
			{
				return Array.Empty<SaveForgeEnemyData>();
			}
			SaveForgeEnemyData[] array = new SaveForgeEnemyData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				ForgeEnemyState forgeEnemyState = source[i] ?? new ForgeEnemyState();
				array[i] = new SaveForgeEnemyData
				{
					spawnId = forgeEnemyState.SpawnId,
					health = forgeEnemyState.Health,
					stagger = forgeEnemyState.Stagger,
					defeated = forgeEnemyState.Defeated,
					markDropped = forgeEnemyState.MarkDropped
				};
			}
			return array;
		}

		private static ForgeEnemyState[] LoadEnemies(SaveForgeEnemyData[] source)
		{
			if (source == null)
			{
				return Array.Empty<ForgeEnemyState>();
			}
			ForgeEnemyState[] array = new ForgeEnemyState[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				SaveForgeEnemyData saveForgeEnemyData = source[i] ?? new SaveForgeEnemyData();
				array[i] = new ForgeEnemyState
				{
					SpawnId = (saveForgeEnemyData.spawnId ?? string.Empty),
					Health = Math.Max(0f, saveForgeEnemyData.health),
					Stagger = Math.Max(0f, saveForgeEnemyData.stagger),
					Defeated = saveForgeEnemyData.defeated,
					MarkDropped = saveForgeEnemyData.markDropped
				};
			}
			return array;
		}

		private static SaveForgeContainerData[] SaveChests(ForgeContainerState[] source)
		{
			if (source == null)
			{
				return Array.Empty<SaveForgeContainerData>();
			}
			SaveForgeContainerData[] array = new SaveForgeContainerData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				ForgeContainerState forgeContainerState = source[i] ?? new ForgeContainerState();
				array[i] = new SaveForgeContainerData
				{
					instanceId = forgeContainerState.InstanceId,
					family = forgeContainerState.Family,
					opened = forgeContainerState.Opened,
					slots = SaveStacks(forgeContainerState.Slots)
				};
			}
			return array;
		}

		private static ForgeContainerState[] LoadChests(SaveForgeContainerData[] source, ContentDatabase content, List<SaveQuarantineEntry> quarantine)
		{
			if (source == null)
			{
				return Array.Empty<ForgeContainerState>();
			}
			ForgeContainerState[] array = new ForgeContainerState[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				SaveForgeContainerData saveForgeContainerData = source[i] ?? new SaveForgeContainerData();
				array[i] = new ForgeContainerState
				{
					InstanceId = (saveForgeContainerData.instanceId ?? string.Empty),
					Family = saveForgeContainerData.family,
					Opened = saveForgeContainerData.opened,
					Slots = LoadStacks(saveForgeContainerData.slots, content, quarantine, $"eidraForge.chests[{i}]")
				};
			}
			return array;
		}

		private static SaveForgeDropData[] SaveDrops(ForgeDropState[] source)
		{
			if (source == null)
			{
				return Array.Empty<SaveForgeDropData>();
			}
			SaveForgeDropData[] array = new SaveForgeDropData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				ForgeDropState forgeDropState = source[i] ?? new ForgeDropState();
				array[i] = new SaveForgeDropData
				{
					dropId = forgeDropState.DropId,
					stack = SaveStack(forgeDropState.Stack),
					positionX = forgeDropState.Position.x,
					positionY = forgeDropState.Position.y,
					positionZ = forgeDropState.Position.z
				};
			}
			return array;
		}

		private static ForgeDropState[] LoadDrops(SaveForgeDropData[] source, ContentDatabase content, List<SaveQuarantineEntry> quarantine)
		{
			if (source == null)
			{
				return Array.Empty<ForgeDropState>();
			}
			List<ForgeDropState> list = new List<ForgeDropState>();
			for (int i = 0; i < source.Length; i++)
			{
				SaveForgeDropData saveForgeDropData = source[i];
				ItemStack[] array = LoadStacks((saveForgeDropData == null) ? null : new SaveItemStackData[1] { saveForgeDropData.stack }, content, quarantine, $"eidraForge.drops[{i}]");
				if (saveForgeDropData != null && array.Length == 1 && !array[0].IsEmpty)
				{
					list.Add(new ForgeDropState
					{
						DropId = (saveForgeDropData.dropId ?? string.Empty),
						Stack = array[0],
						Position = new Vector3(saveForgeDropData.positionX, saveForgeDropData.positionY, saveForgeDropData.positionZ)
					});
				}
			}
			return list.ToArray();
		}

		private static SaveForgeRewardChestData[] SaveRewards(ForgeRewardChestState[] source)
		{
			if (source == null)
			{
				return Array.Empty<SaveForgeRewardChestData>();
			}
			SaveForgeRewardChestData[] array = new SaveForgeRewardChestData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				ForgeRewardChestState forgeRewardChestState = source[i] ?? new ForgeRewardChestState();
				array[i] = new SaveForgeRewardChestData
				{
					size = forgeRewardChestState.Size,
					purchaseIndex = forgeRewardChestState.PurchaseIndex,
					slots = SaveStacks(forgeRewardChestState.Slots)
				};
			}
			return array;
		}

		private static ForgeRewardChestState[] LoadRewards(SaveForgeRewardChestData[] source, ContentDatabase content, List<SaveQuarantineEntry> quarantine)
		{
			if (source == null)
			{
				return Array.Empty<ForgeRewardChestState>();
			}
			ForgeRewardChestState[] array = new ForgeRewardChestState[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				SaveForgeRewardChestData saveForgeRewardChestData = source[i] ?? new SaveForgeRewardChestData();
				array[i] = new ForgeRewardChestState
				{
					Size = saveForgeRewardChestData.size,
					PurchaseIndex = Math.Max(0, saveForgeRewardChestData.purchaseIndex),
					Slots = LoadStacks(saveForgeRewardChestData.slots, content, quarantine, $"eidraForge.rewardChests[{i}]")
				};
			}
			return array;
		}

		private static SaveItemStackData[] SaveStacks(ItemStack[] source)
		{
			if (source == null)
			{
				return Array.Empty<SaveItemStackData>();
			}
			SaveItemStackData[] array = new SaveItemStackData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = SaveStack(source[i]);
			}
			return array;
		}

		private static SaveItemStackData SaveStack(ItemStack value)
		{
			return new SaveItemStackData
			{
				itemId = (value.ItemId ?? string.Empty),
				quantity = value.Quantity,
				instanceId = (value.InstanceId ?? string.Empty),
				durability = value.Durability
			};
		}

		private static ItemStack[] LoadStacks(SaveItemStackData[] source, ContentDatabase content, List<SaveQuarantineEntry> quarantine, string context)
		{
			if (source == null)
			{
				return Array.Empty<ItemStack>();
			}
			ItemStack[] array = new ItemStack[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				SaveItemStackData saveItemStackData = source[i];
				if (saveItemStackData != null && saveItemStackData.quantity != 0)
				{
					if (saveItemStackData.quantity < 0 || saveItemStackData.durability < 0 || content == null || !content.TryGetItem(saveItemStackData.itemId, out var value) || saveItemStackData.quantity > value.MaximumStackSize)
					{
						quarantine?.Add(new SaveQuarantineEntry
						{
							source = $"{context}[{i}]",
							stableId = (saveItemStackData?.itemId ?? string.Empty),
							reason = "Unknown item ID or invalid stack quantity."
						});
					}
					else
					{
						array[i] = new ItemStack(saveItemStackData.itemId, saveItemStackData.quantity, saveItemStackData.instanceId, saveItemStackData.durability);
					}
				}
			}
			return array;
		}
	}
}
