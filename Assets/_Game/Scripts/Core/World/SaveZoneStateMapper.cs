using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	internal static class SaveZoneStateMapper
	{
		internal static ZoneState[] ToRuntime(SaveZoneStateData[] source, List<SaveQuarantineEntry> quarantine)
		{
			if (source == null || source.Length == 0)
			{
				return Array.Empty<ZoneState>();
			}
			List<ZoneState> list = new List<ZoneState>(source.Length);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < source.Length; i++)
			{
				SaveZoneStateData saveZoneStateData = source[i];
				if (saveZoneStateData == null || string.IsNullOrWhiteSpace(saveZoneStateData.zoneId) || saveZoneStateData.generatorVersion <= 0 || !hashSet.Add(saveZoneStateData.zoneId))
				{
					Quarantine(quarantine, $"zones[{i}]", saveZoneStateData?.zoneId, "Missing, duplicate, or unversioned zone state.");
				}
				else
				{
					list.Add(new ZoneState(saveZoneStateData.zoneId, saveZoneStateData.seed, saveZoneStateData.generatorVersion, saveZoneStateData.harvestedNodeIds, ToRuntimeChests(saveZoneStateData, i, quarantine)));
				}
			}
			return list.ToArray();
		}

		internal static SaveZoneStateData[] ToSave(ZoneState[] source)
		{
			if (source == null || source.Length == 0)
			{
				return Array.Empty<SaveZoneStateData>();
			}
			SaveZoneStateData[] array = new SaveZoneStateData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				ZoneState zoneState = source[i];
				array[i] = new SaveZoneStateData
				{
					zoneId = zoneState.ZoneId,
					seed = zoneState.Seed,
					generatorVersion = zoneState.GeneratorVersion,
					harvestedNodeIds = zoneState.GetHarvestedNodeIds(),
					worldChests = ToSaveChests(zoneState.GetWorldChests())
				};
			}
			return array;
		}

		private static WorldChestState[] ToRuntimeChests(SaveZoneStateData zone, int zoneIndex, List<SaveQuarantineEntry> quarantine)
		{
			if (zone.worldChests == null || zone.worldChests.Length == 0)
			{
				return Array.Empty<WorldChestState>();
			}
			List<WorldChestState> list = new List<WorldChestState>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < zone.worldChests.Length; i++)
			{
				SaveWorldChestData saveWorldChestData = zone.worldChests[i];
				if (saveWorldChestData == null || string.IsNullOrWhiteSpace(saveWorldChestData.instanceId) || string.IsNullOrWhiteSpace(saveWorldChestData.spawnPointId) || !Enum.IsDefined(typeof(WorldChestFamily), saveWorldChestData.family) || saveWorldChestData.slots == null || saveWorldChestData.slots.Length != 24 || !hashSet.Add(saveWorldChestData.instanceId))
				{
					Quarantine(quarantine, $"zones[{zoneIndex}].worldChests[{i}]", saveWorldChestData?.instanceId, "Invalid or duplicate persistent world chest.");
				}
				else
				{
					list.Add(new WorldChestState(saveWorldChestData.instanceId, saveWorldChestData.spawnPointId, (WorldChestFamily)saveWorldChestData.family, ToRuntimeStacks(saveWorldChestData.slots), saveWorldChestData.opened, saveWorldChestData.initialOccupiedSlotCount, saveWorldChestData.initialTotalQuantity));
				}
			}
			return list.ToArray();
		}

		private static SaveWorldChestData[] ToSaveChests(IReadOnlyList<WorldChestState> source)
		{
			if (source == null || source.Count == 0)
			{
				return Array.Empty<SaveWorldChestData>();
			}
			SaveWorldChestData[] array = new SaveWorldChestData[source.Count];
			for (int i = 0; i < source.Count; i++)
			{
				WorldChestState worldChestState = source[i];
				array[i] = new SaveWorldChestData
				{
					instanceId = worldChestState.InstanceId,
					spawnPointId = worldChestState.SpawnPointId,
					family = (int)worldChestState.Family,
					opened = worldChestState.Opened,
					initialOccupiedSlotCount = worldChestState.InitialOccupiedSlotCount,
					initialTotalQuantity = worldChestState.InitialTotalQuantity,
					slots = ToSaveStacks(worldChestState.Slots)
				};
			}
			return array;
		}

		private static ItemStack[] ToRuntimeStacks(IReadOnlyList<SaveItemStackData> source)
		{
			ItemStack[] array = new ItemStack[source.Count];
			for (int i = 0; i < source.Count; i++)
			{
				SaveItemStackData saveItemStackData = source[i];
				array[i] = ((saveItemStackData == null) ? ItemStack.Empty : new ItemStack(saveItemStackData.itemId, saveItemStackData.quantity, saveItemStackData.instanceId, saveItemStackData.durability));
			}
			return array;
		}

		private static SaveItemStackData[] ToSaveStacks(IReadOnlyList<ItemStack> source)
		{
			SaveItemStackData[] array = new SaveItemStackData[source.Count];
			for (int i = 0; i < source.Count; i++)
			{
				ItemStack itemStack = source[i];
				array[i] = new SaveItemStackData
				{
					itemId = itemStack.ItemId,
					quantity = itemStack.Quantity,
					instanceId = itemStack.InstanceId,
					durability = itemStack.Durability
				};
			}
			return array;
		}

		private static void Quarantine(List<SaveQuarantineEntry> quarantine, string context, string stableId, string reason)
		{
			quarantine?.Add(new SaveQuarantineEntry
			{
				source = context,
				stableId = (stableId ?? string.Empty),
				reason = reason
			});
		}
	}
}
