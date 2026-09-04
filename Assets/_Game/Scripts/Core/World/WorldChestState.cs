using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class WorldChestState
	{
		public const int SlotCount = 24;

		private ItemStack[] _slots;

		public string InstanceId { get; }

		public string SpawnPointId { get; }

		public WorldChestFamily Family { get; }

		public bool Opened { get; private set; }

		public ItemStack[] Slots => Copy(_slots);

		public WorldChestStatus Status
		{
			get
			{
				int num = 0;
				ItemStack[] slots = _slots;
				foreach (ItemStack itemStack in slots)
				{
					if (!itemStack.IsEmpty)
					{
						num++;
					}
				}
				if (!Opened)
				{
					return WorldChestStatus.Closed;
				}
				if (num == 0)
				{
					return WorldChestStatus.Emptied;
				}
				return (CountQuantity(_slots) == InitialTotalQuantity) ? WorldChestStatus.Opened : WorldChestStatus.PartiallyEmptied;
			}
		}

		public int InitialOccupiedSlotCount { get; private set; }

		public int InitialTotalQuantity { get; private set; }

		public WorldChestState(string instanceId, string spawnPointId, WorldChestFamily family, IReadOnlyList<ItemStack> slots, bool opened = false, int initialOccupiedSlotCount = -1, int initialTotalQuantity = -1)
		{
			if (string.IsNullOrWhiteSpace(instanceId))
			{
				throw new ArgumentException("A chest instance ID is required.");
			}
			if (string.IsNullOrWhiteSpace(spawnPointId))
			{
				throw new ArgumentException("A chest spawn-point ID is required.");
			}
			if (slots == null || slots.Count != 24)
			{
				throw new ArgumentException($"A chest requires {24} slots.");
			}
			InstanceId = instanceId;
			SpawnPointId = spawnPointId;
			Family = family;
			_slots = Copy(slots);
			Opened = opened;
			InitialOccupiedSlotCount = ((initialOccupiedSlotCount >= 0) ? initialOccupiedSlotCount : CountOccupied(_slots));
			InitialTotalQuantity = ((initialTotalQuantity >= 0) ? initialTotalQuantity : CountQuantity(_slots));
		}

		public bool MarkOpened()
		{
			if (Opened)
			{
				return false;
			}
			Opened = true;
			return true;
		}

		public void ReplaceSlots(IReadOnlyList<ItemStack> slots)
		{
			if (slots == null || slots.Count != 24)
			{
				throw new ArgumentException($"A chest requires {24} slots.");
			}
			_slots = Copy(slots);
		}

		private static int CountOccupied(IReadOnlyList<ItemStack> slots)
		{
			int num = 0;
			foreach (ItemStack slot in slots)
			{
				if (!slot.IsEmpty)
				{
					num++;
				}
			}
			return num;
		}

		private static int CountQuantity(IReadOnlyList<ItemStack> slots)
		{
			int num = 0;
			foreach (ItemStack slot in slots)
			{
				num += slot.Quantity;
			}
			return num;
		}

		private static ItemStack[] Copy(IReadOnlyList<ItemStack> source)
		{
			ItemStack[] array = new ItemStack[source.Count];
			for (int i = 0; i < source.Count; i++)
			{
				array[i] = source[i].Copy();
			}
			return array;
		}
	}
}
