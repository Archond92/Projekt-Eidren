using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class PlayerInventory : IItemContainer, IMaterialStock
	{
		public const int SlotCount = 16;

		private readonly ItemStack[] _slots = new ItemStack[16];

		private ContentDatabase _database;

		private PlayerEquipment _equipment;

		public int Count => SlotCapacity;

		public int SlotCapacity => 16 + ((_equipment != null) ? _equipment.AdditionalInventorySlots : 0);

		public event Action<InventoryChange> Changed;

		public event Action<InventoryFeedback> Feedback;

		public void ConfigureEquipment(PlayerEquipment equipment)
		{
			_equipment = equipment ?? throw new ArgumentNullException("equipment");
			if (_equipment.AdditionalInventorySlots != 0)
			{
				throw new NotSupportedException("Backpack items grow the inventory only after v0.1; the slot array does not resize yet (M5).");
			}
		}

		public PlayerInventory(ContentDatabase database = null)
		{
			_database = database;
		}

		public void Configure(ContentDatabase database)
		{
			_database = database ?? throw new ArgumentNullException("database");
			for (int i = 0; i < 16; i++)
			{
				if (!_slots[i].TryValidate(_database, out var error))
				{
					throw new InvalidOperationException($"Inventory slot {i} is invalid: {error}");
				}
			}
		}

		public bool TryGetSlot(int index, out ItemStack stack)
		{
			if (!IsValidIndex(index))
			{
				stack = ItemStack.Empty;
				return false;
			}
			stack = _slots[index].Copy();
			return true;
		}

		public ItemStack[] ExportSlots()
		{
			ItemStack[] array = new ItemStack[16];
			for (int i = 0; i < 16; i++)
			{
				array[i] = _slots[i].Copy();
			}
			return array;
		}

		public bool TryImportSlots(IReadOnlyList<ItemStack> slots, out string error)
		{
			if (slots == null || slots.Count != 16)
			{
				error = "Player inventory import requires exactly " + $"{16} slots.";
				return false;
			}
			for (int i = 0; i < slots.Count; i++)
			{
				if (_database != null && !slots[i].TryValidate(_database, out error))
				{
					error = $"Player inventory slot {i} is invalid: " + error;
					return false;
				}
			}
			Reset(slots);
			error = string.Empty;
			return true;
		}

		public bool CanAdd(string itemId, int amount, out int remainder)
		{
			ValidateNonNegative(amount, "amount");
			if (!TryResolve(itemId, out var definition))
			{
				remainder = amount;
				return false;
			}
			int num = 0;
			for (int i = 0; i < 16; i++)
			{
				ItemStack itemStack = _slots[i];
				if (itemStack.IsEmpty)
				{
					num += definition.MaximumStackSize;
				}
				else if (string.Equals(itemStack.ItemId, definition.Id, StringComparison.Ordinal))
				{
					num += Math.Max(0, definition.MaximumStackSize - itemStack.Quantity);
				}
				if (num >= amount)
				{
					break;
				}
			}
			remainder = Math.Max(0, amount - num);
			return remainder == 0;
		}

		public int Add(string itemId, int amount)
		{
			ValidateNonNegative(amount, "amount");
			if (amount == 0)
			{
				return 0;
			}
			if (!TryResolve(itemId, out var definition))
			{
				PublishFeedback(InventoryFeedbackType.UnknownItemId, itemId, amount);
				return amount;
			}
			int remainder = amount;
			for (int i = 0; i < 16; i++)
			{
				if (remainder <= 0)
				{
					break;
				}
				ItemStack itemStack = _slots[i];
				if (!itemStack.IsEmpty && string.Equals(itemStack.ItemId, definition.Id, StringComparison.Ordinal))
				{
					itemStack.TryAdd(definition, remainder, out var updated, out remainder);
					_slots[i] = updated;
				}
			}
			for (int j = 0; j < 16; j++)
			{
				if (remainder <= 0)
				{
					break;
				}
				if (_slots[j].IsEmpty)
				{
					_slots[j].TryAdd(definition, remainder, out var updated2, out remainder);
					_slots[j] = updated2;
				}
			}
			int num = amount - remainder;
			if (num > 0)
			{
				this.Changed?.Invoke(new InventoryChange(InventoryChangeType.Added, definition.Id, num));
				PublishFeedback(InventoryFeedbackType.ItemAdded, definition.Id, num);
			}
			if (remainder > 0)
			{
				PublishFeedback(InventoryFeedbackType.InventoryFull, definition.Id, remainder);
			}
			return remainder;
		}

		public bool TryAddAll(string itemId, int amount)
		{
			ValidateNonNegative(amount, "amount");
			if (amount == 0)
			{
				return true;
			}
			if (!TryResolve(itemId, out var _))
			{
				PublishFeedback(InventoryFeedbackType.UnknownItemId, itemId, amount);
				return false;
			}
			if (!CanAdd(itemId, amount, out var remainder))
			{
				PublishFeedback(InventoryFeedbackType.InventoryFull, itemId, remainder);
				return false;
			}
			return Add(itemId, amount) == 0;
		}

		public bool CanAddBatch(IReadOnlyList<InventoryItemAmount> additions)
		{
			ItemStack[] candidate;
			InventoryTransactionFailure failure;
			return TryCreateAdditionBatch(additions, out candidate, out failure);
		}

		public bool TryAddBatch(IReadOnlyList<InventoryItemAmount> additions)
		{
			if (!TryCreateAdditionBatch(additions, out var candidate, out var failure))
			{
				if (failure == InventoryTransactionFailure.InventoryFull)
				{
					PublishFeedback(InventoryFeedbackType.InventoryFull, string.Empty, 0);
				}
				return false;
			}
			Array.Copy(candidate, _slots, 16);
			int num = 0;
			foreach (InventoryItemAmount addition in additions)
			{
				num = checked(num + addition.Amount);
			}
			this.Changed?.Invoke(new InventoryChange(InventoryChangeType.Transaction, string.Empty, num));
			return true;
		}

		public bool Remove(string itemId, int amount)
		{
			ValidateNonNegative(amount, "amount");
			if (amount == 0)
			{
				return true;
			}
			if (!TryResolve(itemId, out var definition))
			{
				PublishFeedback(InventoryFeedbackType.UnknownItemId, itemId, amount);
				return false;
			}
			if (!Contains(definition.Id, amount))
			{
				PublishFeedback(InventoryFeedbackType.NotEnoughItems, definition.Id, amount);
				return false;
			}
			int num = amount;
			int num2 = 15;
			while (num2 >= 0 && num > 0)
			{
				ItemStack itemStack = _slots[num2];
				if (string.Equals(itemStack.ItemId, definition.Id, StringComparison.Ordinal))
				{
					int num3 = Math.Min(itemStack.Quantity, num);
					int num4 = itemStack.Quantity - num3;
					_slots[num2] = ((num4 == 0) ? ItemStack.Empty : itemStack.WithQuantity(definition, num4));
					num -= num3;
				}
				num2--;
			}
			this.Changed?.Invoke(new InventoryChange(InventoryChangeType.Removed, definition.Id, amount));
			return true;
		}

		public bool TryWearDurableItem(string itemId, int wear, out bool broken)
		{
			broken = false;
			if (wear <= 0 || !TryResolve(itemId, out var definition) || definition.MaximumDurability <= 0)
			{
				return false;
			}
			for (int i = 0; i < 16; i++)
			{
				ItemStack itemStack = _slots[i];
				if (string.Equals(itemStack.ItemId, definition.Id, StringComparison.Ordinal) && itemStack.Durability > 0)
				{
					DurabilityResult durabilityResult = DurabilityRules.Apply(itemStack.Durability, wear);
					broken = durabilityResult.IsBroken;
					_slots[i] = (broken ? ItemStack.Empty : new ItemStack(itemStack.ItemId, itemStack.Quantity, itemStack.InstanceId, durabilityResult.Remaining));
					this.Changed?.Invoke(new InventoryChange(broken ? InventoryChangeType.Removed : InventoryChangeType.Used, definition.Id, wear));
					return true;
				}
			}
			return false;
		}

		public bool Contains(string itemId, int amount = 1)
		{
			ValidateNonNegative(amount, "amount");
			if (amount != 0)
			{
				return GetTotalAmount(itemId) >= amount;
			}
			return true;
		}

		private bool TryCreateAdditionBatch(IReadOnlyList<InventoryItemAmount> additions, out ItemStack[] candidate, out InventoryTransactionFailure failure)
		{
			candidate = null;
			if (additions == null || additions.Count == 0)
			{
				failure = InventoryTransactionFailure.InvalidRequest;
				return false;
			}
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (InventoryItemAmount addition in additions)
			{
				if (string.IsNullOrWhiteSpace(addition.ItemId) || addition.Amount <= 0)
				{
					failure = InventoryTransactionFailure.InvalidRequest;
					return false;
				}
				if (!TryResolve(addition.ItemId, out var definition))
				{
					failure = InventoryTransactionFailure.UnknownItem;
					return false;
				}
				dictionary.TryGetValue(definition.Id, out var value);
				dictionary[definition.Id] = checked(value + addition.Amount);
			}
			candidate = new ItemStack[16];
			for (int i = 0; i < 16; i++)
			{
				candidate[i] = _slots[i].Copy();
			}
			foreach (KeyValuePair<string, int> item2 in dictionary)
			{
				ItemDefinition item = _database.GetItem(item2.Key);
				int remainder = item2.Value;
				AddToCandidate(candidate, item, ref remainder);
				if (remainder > 0)
				{
					candidate = null;
					failure = InventoryTransactionFailure.InventoryFull;
					return false;
				}
			}
			failure = InventoryTransactionFailure.None;
			return true;
		}

		public bool TryApplyTransaction(IReadOnlyList<InventoryItemAmount> removals, string additionItemId, int additionAmount, out InventoryTransactionFailure failure)
		{
			if (!TryCreateTransactionState(removals, additionItemId, additionAmount, out var candidate, out failure))
			{
				return false;
			}
			Array.Copy(candidate, _slots, 16);
			this.Changed?.Invoke(new InventoryChange(InventoryChangeType.Transaction, additionItemId, additionAmount));
			return true;
		}

		public bool CanApplyTransaction(IReadOnlyList<InventoryItemAmount> removals, string additionItemId, int additionAmount, out InventoryTransactionFailure failure)
		{
			ItemStack[] candidate;
			return TryCreateTransactionState(removals, additionItemId, additionAmount, out candidate, out failure);
		}

		private bool TryCreateTransactionState(IReadOnlyList<InventoryItemAmount> removals, string additionItemId, int additionAmount, out ItemStack[] candidate, out InventoryTransactionFailure failure)
		{
			candidate = null;
			if (removals == null || additionAmount < 0)
			{
				failure = InventoryTransactionFailure.InvalidRequest;
				return false;
			}
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (InventoryItemAmount removal in removals)
			{
				if (string.IsNullOrWhiteSpace(removal.ItemId) || removal.Amount <= 0)
				{
					failure = InventoryTransactionFailure.InvalidRequest;
					return false;
				}
				if (!TryResolve(removal.ItemId, out var definition))
				{
					failure = InventoryTransactionFailure.UnknownItem;
					return false;
				}
				dictionary.TryGetValue(definition.Id, out var value);
				dictionary[definition.Id] = checked(value + removal.Amount);
			}
			ItemDefinition definition2 = null;
			if (additionAmount > 0 && !TryResolve(additionItemId, out definition2))
			{
				failure = InventoryTransactionFailure.UnknownItem;
				return false;
			}
			foreach (KeyValuePair<string, int> item in dictionary)
			{
				if (GetTotalAmount(item.Key) < item.Value)
				{
					failure = InventoryTransactionFailure.MissingItems;
					return false;
				}
			}
			candidate = new ItemStack[16];
			for (int i = 0; i < 16; i++)
			{
				candidate[i] = _slots[i].Copy();
			}
			foreach (KeyValuePair<string, int> item2 in dictionary)
			{
				int num = item2.Value;
				int num2 = 15;
				while (num2 >= 0 && num > 0)
				{
					ItemStack itemStack = candidate[num2];
					if (string.Equals(itemStack.ItemId, item2.Key, StringComparison.Ordinal))
					{
						int num3 = Math.Min(itemStack.Quantity, num);
						int num4 = itemStack.Quantity - num3;
						candidate[num2] = ((num4 == 0) ? ItemStack.Empty : itemStack.WithQuantity(_database.GetItem(item2.Key), num4));
						num -= num3;
					}
					num2--;
				}
			}
			int remainder = additionAmount;
			if (definition2 != null)
			{
				AddToCandidate(candidate, definition2, ref remainder);
			}
			if (remainder > 0)
			{
				failure = InventoryTransactionFailure.InventoryFull;
				return false;
			}
			failure = InventoryTransactionFailure.None;
			return true;
		}

		public int GetTotalAmount(string itemId)
		{
			if (string.IsNullOrWhiteSpace(itemId))
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < 16; i++)
			{
				if (string.Equals(_slots[i].ItemId, itemId, StringComparison.Ordinal))
				{
					num += _slots[i].Quantity;
				}
			}
			return num;
		}

		public bool Move(int sourceIndex, int targetIndex)
		{
			if (!IsValidIndex(sourceIndex) || !IsValidIndex(targetIndex) || sourceIndex == targetIndex || _slots[sourceIndex].IsEmpty)
			{
				return false;
			}
			ItemStack itemStack = _slots[sourceIndex];
			ItemStack itemStack2 = _slots[targetIndex];
			if (itemStack2.IsEmpty)
			{
				_slots[targetIndex] = itemStack;
				_slots[sourceIndex] = ItemStack.Empty;
			}
			else
			{
				if (!string.Equals(itemStack.ItemId, itemStack2.ItemId, StringComparison.Ordinal))
				{
					return false;
				}
				if (!TryResolve(itemStack.ItemId, out var definition))
				{
					PublishFeedback(InventoryFeedbackType.UnknownItemId, itemStack.ItemId, itemStack.Quantity);
					return false;
				}
				int num = definition.MaximumStackSize - itemStack2.Quantity;
				if (num <= 0)
				{
					return false;
				}
				int num2 = Math.Min(num, itemStack.Quantity);
				_slots[targetIndex] = itemStack2.WithQuantity(definition, itemStack2.Quantity + num2);
				int num3 = itemStack.Quantity - num2;
				_slots[sourceIndex] = ((num3 == 0) ? ItemStack.Empty : itemStack.WithQuantity(definition, num3));
			}
			this.Changed?.Invoke(new InventoryChange(InventoryChangeType.Moved, itemStack.ItemId, itemStack.Quantity));
			return true;
		}

		public bool Swap(int firstIndex, int secondIndex)
		{
			if (!IsValidIndex(firstIndex) || !IsValidIndex(secondIndex) || firstIndex == secondIndex)
			{
				return false;
			}
			if (StacksEqual(_slots[firstIndex], _slots[secondIndex]))
			{
				return false;
			}
			ItemStack itemStack = _slots[firstIndex];
			_slots[firstIndex] = _slots[secondIndex];
			_slots[secondIndex] = itemStack;
			this.Changed?.Invoke(new InventoryChange(InventoryChangeType.Swapped, string.Empty, 0));
			return true;
		}

		public bool Use(int slotIndex, IItemUseHandler handler)
		{
			if (!IsValidIndex(slotIndex) || _slots[slotIndex].IsEmpty || handler == null)
			{
				PublishFeedback(InventoryFeedbackType.UseNotPossible, string.Empty, 0);
				return false;
			}
			ItemStack itemStack = _slots[slotIndex];
			if (!TryResolve(itemStack.ItemId, out var definition))
			{
				PublishFeedback(InventoryFeedbackType.UnknownItemId, itemStack.ItemId, itemStack.Quantity);
				return false;
			}
			if (!definition.CanBeUsed || !handler.TryUse(definition))
			{
				PublishFeedback(InventoryFeedbackType.UseNotPossible, definition.Id, 1);
				return false;
			}
			int num = itemStack.Quantity - 1;
			_slots[slotIndex] = ((num == 0) ? ItemStack.Empty : itemStack.WithQuantity(definition, num));
			this.Changed?.Invoke(new InventoryChange(InventoryChangeType.Used, definition.Id, 1));
			PublishFeedback(InventoryFeedbackType.ItemUsed, definition.Id, 1);
			return true;
		}

		public bool UseFirst(string itemId, IItemUseHandler handler)
		{
			if (!TryResolve(itemId, out var _))
			{
				PublishFeedback(InventoryFeedbackType.UnknownItemId, itemId, 1);
				return false;
			}
			for (int i = 0; i < 16; i++)
			{
				if (string.Equals(_slots[i].ItemId, itemId, StringComparison.Ordinal))
				{
					return Use(i, handler);
				}
			}
			PublishFeedback(InventoryFeedbackType.NotEnoughItems, itemId, 1);
			return false;
		}

		public void Clear()
		{
			bool flag = false;
			for (int i = 0; i < 16; i++)
			{
				flag |= !_slots[i].IsEmpty;
				_slots[i] = ItemStack.Empty;
			}
			if (flag)
			{
				this.Changed?.Invoke(new InventoryChange(InventoryChangeType.Cleared, string.Empty, 0));
			}
		}

		public void Reset(IReadOnlyList<ItemStack> stacks)
		{
			if (stacks == null)
			{
				throw new ArgumentNullException("stacks");
			}
			if (stacks.Count > 16)
			{
				throw new ArgumentException($"An inventory can contain at most {16} stacks.", "stacks");
			}
			ItemStack[] array = new ItemStack[16];
			bool flag = false;
			for (int i = 0; i < 16; i++)
			{
				array[i] = ((i < stacks.Count) ? stacks[i].Copy() : ItemStack.Empty);
				if (_database != null && !array[i].TryValidate(_database, out var error))
				{
					throw new ArgumentException($"Starting stack {i} is invalid: {error}", "stacks");
				}
				flag |= !StacksEqual(_slots[i], array[i]);
			}
			if (flag)
			{
				Array.Copy(array, _slots, 16);
				this.Changed?.Invoke(new InventoryChange(InventoryChangeType.Reset, string.Empty, 0));
			}
		}

		private bool TryResolve(string itemId, out ItemDefinition definition)
		{
			if (_database == null || !_database.TryGetItem(itemId, out definition))
			{
				definition = null;
				return false;
			}
			return true;
		}

		private static void AddToCandidate(ItemStack[] candidate, ItemDefinition definition, ref int remainder)
		{
			for (int i = 0; i < 16; i++)
			{
				if (remainder <= 0)
				{
					break;
				}
				ItemStack itemStack = candidate[i];
				if (!itemStack.IsEmpty && string.Equals(itemStack.ItemId, definition.Id, StringComparison.Ordinal))
				{
					itemStack.TryAdd(definition, remainder, out var updated, out remainder);
					candidate[i] = updated;
				}
			}
			for (int j = 0; j < 16; j++)
			{
				if (remainder <= 0)
				{
					break;
				}
				if (candidate[j].IsEmpty)
				{
					candidate[j].TryAdd(definition, remainder, out var updated2, out remainder);
					candidate[j] = updated2;
				}
			}
		}

		private void PublishFeedback(InventoryFeedbackType type, string itemId, int amount)
		{
			this.Feedback?.Invoke(new InventoryFeedback(type, itemId, amount));
		}

		private static bool IsValidIndex(int index)
		{
			if (index >= 0)
			{
				return index < 16;
			}
			return false;
		}

		private static bool StacksEqual(ItemStack first, ItemStack second)
		{
			if (first.Quantity == second.Quantity && first.Durability == second.Durability && string.Equals(first.ItemId, second.ItemId, StringComparison.Ordinal))
			{
				return string.Equals(first.InstanceId, second.InstanceId, StringComparison.Ordinal);
			}
			return false;
		}

		private static void ValidateNonNegative(int value, string parameterName)
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(parameterName, value, "Inventory quantities cannot be negative.");
			}
		}
	}

	public readonly struct InventoryChange
	{
		public InventoryChangeType Type { get; }

		public string ItemId { get; }

		public int Amount { get; }

		public InventoryChange(InventoryChangeType type, string itemId, int amount)
		{
			Type = type;
			ItemId = itemId ?? string.Empty;
			Amount = amount;
		}
	}

	public readonly struct InventoryFeedback
	{
		public InventoryFeedbackType Type { get; }

		public string ItemId { get; }

		public int Amount { get; }

		public InventoryFeedback(InventoryFeedbackType type, string itemId, int amount)
		{
			Type = type;
			ItemId = itemId ?? string.Empty;
			Amount = amount;
		}
	}

	public enum InventoryFeedbackType
	{
		InventoryFull = 0,
		ItemAdded = 1,
		ItemUsed = 2,
		UseNotPossible = 3,
		NotEnoughItems = 4,
		UnknownItemId = 5
	}

	public readonly struct InventoryItemAmount
	{
		public string ItemId { get; }

		public int Amount { get; }

		public InventoryItemAmount(string itemId, int amount)
		{
			ItemId = itemId ?? string.Empty;
			Amount = amount;
		}
	}
}
