using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	internal static class SaveEquipmentMapper
	{
		internal static EquipmentAssignment[] ToRuntime(SaveEquipmentEntry[] source, ContentDatabase content, List<SaveQuarantineEntry> quarantine)
		{
			if (source == null || source.Length == 0)
			{
				return Array.Empty<EquipmentAssignment>();
			}
			List<EquipmentAssignment> list = new List<EquipmentAssignment>(source.Length);
			HashSet<int> hashSet = new HashSet<int>();
			for (int i = 0; i < source.Length; i++)
			{
				SaveEquipmentEntry saveEquipmentEntry = source[i];
				string context = $"player.equipment[{i}]";
				if (saveEquipmentEntry?.stack == null || saveEquipmentEntry.stack.quantity <= 0 || saveEquipmentEntry.stack.durability < 0 || !Enum.IsDefined(typeof(EquipmentSlot), saveEquipmentEntry.slot) || !hashSet.Add(saveEquipmentEntry.slot) || !PlayerEquipment.IsUsableInV01((EquipmentSlot)saveEquipmentEntry.slot) || string.IsNullOrWhiteSpace(saveEquipmentEntry.stack.itemId) || content == null || !content.TryGetItem(saveEquipmentEntry.stack.itemId, out var value) || saveEquipmentEntry.stack.quantity > value.MaximumStackSize)
				{
					Quarantine(quarantine, context, saveEquipmentEntry?.stack?.itemId, "Unknown equipment slot or invalid stack.");
				}
				else
				{
					list.Add(new EquipmentAssignment((EquipmentSlot)saveEquipmentEntry.slot, new ItemStack(saveEquipmentEntry.stack.itemId, saveEquipmentEntry.stack.quantity, saveEquipmentEntry.stack.instanceId, saveEquipmentEntry.stack.durability)));
				}
			}
			return list.ToArray();
		}

		internal static SaveEquipmentEntry[] ToSave(EquipmentAssignment[] source)
		{
			if (source == null || source.Length == 0)
			{
				return Array.Empty<SaveEquipmentEntry>();
			}
			SaveEquipmentEntry[] array = new SaveEquipmentEntry[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				ItemStack stack = source[i].Stack;
				array[i] = new SaveEquipmentEntry
				{
					slot = (int)source[i].Slot,
					stack = new SaveItemStackData
					{
						itemId = stack.ItemId,
						quantity = stack.Quantity,
						instanceId = stack.InstanceId,
						durability = stack.Durability
					}
				};
			}
			return array;
		}

		internal static DeathBagLocation ToRuntime(SaveDeathBagData source)
		{
			if (source == null || !source.exists || string.IsNullOrWhiteSpace(source.sceneKey))
			{
				return DeathBagLocation.None;
			}
			return new DeathBagLocation(source.sceneKey, new Vector3(source.positionX, source.positionY, source.positionZ));
		}

		internal static SaveDeathBagData ToSave(DeathBagLocation source)
		{
			return new SaveDeathBagData
			{
				exists = source.Exists,
				sceneKey = source.SceneKey,
				positionX = source.Position.x,
				positionY = source.Position.y,
				positionZ = source.Position.z
			};
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
