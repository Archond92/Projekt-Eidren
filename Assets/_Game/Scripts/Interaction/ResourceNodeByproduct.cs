using Eidren.Core.Services;
using Eidren.Data;
using System;
using UnityEngine;

namespace Eidren.Interaction
{
	[Serializable]
	public sealed class ResourceNodeByproduct
	{
		[SerializeField]
		private ItemDefinition item;

		[SerializeField]
		[Min(0f)]
		private int amount;

		private readonly InventoryItemAmount[] _capacityProbe = new InventoryItemAmount[2];

		public ItemDefinition Item => item;

		public int Amount
		{
			get
			{
				if (!(item != null))
				{
					return 0;
				}
				return Mathf.Max(0, amount);
			}
		}

		public bool Exists => Amount > 0;

		public void Configure(ItemDefinition byproduct, int count)
		{
			item = ((count > 0) ? byproduct : null);
			amount = ((byproduct != null) ? Mathf.Max(0, count) : 0);
		}

		public bool FitsWith(PlayerInventory inventory, string outputItemId, int outputAmount)
		{
			if (inventory == null)
			{
				return false;
			}
			int remainder;
			if (!Exists)
			{
				return inventory.CanAdd(outputItemId, outputAmount, out remainder);
			}
			_capacityProbe[0] = new InventoryItemAmount(outputItemId, outputAmount);
			_capacityProbe[1] = new InventoryItemAmount(item.Id, Amount);
			return inventory.CanAddBatch(_capacityProbe);
		}

		public void AwardTo(PlayerInventory inventory)
		{
			if (Exists)
			{
				inventory?.TryAddAll(item.Id, Amount);
			}
		}
	}
}
