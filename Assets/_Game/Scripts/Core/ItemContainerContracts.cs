using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	[Serializable]
	public sealed class StorageContainerState
	{
		public const int DefaultSlotCount = 24;

		[SerializeField]
		private string containerId;

		[SerializeField]
		private ItemStack[] slots;

		public string ContainerId => containerId ?? string.Empty;

		public ItemStack[] Slots => CopySlots(slots);

		public StorageContainerState(string stableContainerId, IReadOnlyList<ItemStack> source)
		{
			containerId = stableContainerId ?? string.Empty;
			slots = CopySlots(source);
		}

		public StorageContainerState Copy()
		{
			return new StorageContainerState(ContainerId, slots);
		}

		private static ItemStack[] CopySlots(IReadOnlyList<ItemStack> source)
		{
			if (source == null)
			{
				return Array.Empty<ItemStack>();
			}
			ItemStack[] array = new ItemStack[source.Count];
			for (int i = 0; i < source.Count; i++)
			{
				array[i] = source[i].Copy();
			}
			return array;
		}
	}
}
