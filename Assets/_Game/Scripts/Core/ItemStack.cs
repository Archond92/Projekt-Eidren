using Eidren.Data;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	[Serializable]
	public struct ItemStack
	{
		[SerializeField]
		private string itemId;

		[SerializeField]
		private int quantity;

		[SerializeField]
		private string instanceId;

		[SerializeField]
		private int durability;

		public string ItemId => itemId ?? string.Empty;

		public int Quantity => quantity;

		public string InstanceId => instanceId ?? string.Empty;

		public int Durability => durability;

		public bool IsEmpty => quantity == 0 && string.IsNullOrEmpty(ItemId);

		public static ItemStack Empty => default(ItemStack);

		public ItemStack(string stableItemId, int amount, string uniqueInstanceId = "", int remainingDurability = 0)
		{
			if (amount < 0)
			{
				throw new ArgumentOutOfRangeException("amount", amount, "Item quantity cannot be negative.");
			}
			if (remainingDurability < 0)
			{
				throw new ArgumentOutOfRangeException("remainingDurability", remainingDurability, "Item durability cannot be negative.");
			}
			if (amount > 0 && string.IsNullOrWhiteSpace(stableItemId))
			{
				throw new ArgumentException("A non-empty stack requires a stable item ID.", "stableItemId");
			}
			itemId = ((amount == 0) ? string.Empty : stableItemId);
			quantity = amount;
			instanceId = ((amount == 0) ? string.Empty : (uniqueInstanceId ?? string.Empty));
			durability = ((amount != 0) ? remainingDurability : 0);
		}

		public static ItemStack Create(ItemDefinition definition, int amount, string uniqueInstanceId = "", int remainingDurability = -1)
		{
			RequireDefinition(definition);
			if (amount > definition.MaximumStackSize)
			{
				throw new ArgumentOutOfRangeException("amount", amount, "Item '" + definition.Id + "' allows at most " + $"{definition.MaximumStackSize} per stack.");
			}
			bool flag = definition.MaximumDurability > 0;
			if (flag && amount > 1)
			{
				throw new ArgumentOutOfRangeException("amount", amount, "A durable item is one persistent instance.");
			}
			string uniqueInstanceId2 = ((flag && amount > 0 && string.IsNullOrWhiteSpace(uniqueInstanceId)) ? ItemInstanceIds.Create() : uniqueInstanceId);
			int num = ((!flag || amount <= 0) ? Math.Max(0, remainingDurability) : ((remainingDurability < 0) ? definition.MaximumDurability : remainingDurability));
			if (num > definition.MaximumDurability && flag)
			{
				throw new ArgumentOutOfRangeException("remainingDurability");
			}
			return new ItemStack(definition.Id, amount, uniqueInstanceId2, num);
		}

		public ItemStack Copy()
		{
			return new ItemStack(ItemId, quantity, InstanceId, durability);
		}

		public ItemStack WithQuantity(ItemDefinition definition, int amount)
		{
			RequireMatchingDefinition(definition);
			return Create(definition, amount, InstanceId, durability);
		}

		public bool TryAdd(ItemDefinition definition, int amount, out ItemStack updated, out int remainder)
		{
			RequireDefinition(definition);
			if (amount < 0)
			{
				throw new ArgumentOutOfRangeException("amount", amount, "Added item quantity cannot be negative.");
			}
			if (!IsEmpty)
			{
				RequireMatchingDefinition(definition);
			}
			int b = definition.MaximumStackSize - ((!IsEmpty) ? quantity : 0);
			int num = Mathf.Min(amount, Mathf.Max(0, b));
			int amount2 = ((!IsEmpty) ? quantity : 0) + num;
			updated = Create(definition, amount2, IsEmpty ? string.Empty : InstanceId, IsEmpty ? (-1) : durability);
			remainder = amount - num;
			return remainder == 0;
		}

		public bool TryValidate(ContentDatabase database, out string error)
		{
			if (quantity < 0)
			{
				error = "Item quantity cannot be negative.";
				return false;
			}
			if (durability < 0)
			{
				error = "Item durability cannot be negative.";
				return false;
			}
			if (quantity == 0)
			{
				if (!string.IsNullOrEmpty(ItemId) || !string.IsNullOrEmpty(InstanceId) || durability != 0)
				{
					error = "An empty stack must not retain item or instance IDs or durability.";
					return false;
				}
				error = string.Empty;
				return true;
			}
			if (database == null)
			{
				error = "A ContentDatabase is required for validation.";
				return false;
			}
			if (!database.TryGetItem(ItemId, out var value))
			{
				error = "ItemDefinition with ID '" + ItemId + "' is not registered.";
				return false;
			}
			if (quantity > value.MaximumStackSize)
			{
				error = $"Stack quantity {quantity} exceeds the maximum " + $"{value.MaximumStackSize} for '{ItemId}'.";
				return false;
			}
			if (value.MaximumDurability > 0)
			{
				if (quantity != 1 || string.IsNullOrWhiteSpace(InstanceId) || durability <= 0 || durability > value.MaximumDurability)
				{
					error = "Durable item '" + ItemId + "' requires one identified instance with durability between 1 and " + $"{value.MaximumDurability}.";
					return false;
				}
			}
			else if (durability != 0)
			{
				error = "Item '" + ItemId + "' does not support durability.";
				return false;
			}
			error = string.Empty;
			return true;
		}

		private void RequireMatchingDefinition(ItemDefinition definition)
		{
			RequireDefinition(definition);
			if (!IsEmpty && !string.Equals(ItemId, definition.Id, StringComparison.Ordinal))
			{
				throw new InvalidOperationException("Stack contains '" + ItemId + "', not '" + definition.Id + "'.");
			}
		}

		private static void RequireDefinition(ItemDefinition definition)
		{
			if (definition == null)
			{
				throw new ArgumentNullException("definition");
			}
			if (string.IsNullOrWhiteSpace(definition.Id))
			{
				throw new ArgumentException("ItemDefinition requires a stable ID.", "definition");
			}
		}
	}
}
