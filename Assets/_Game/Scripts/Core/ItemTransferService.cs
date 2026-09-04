using Eidren.Data;
using System;

namespace Eidren.Core.Services
{
	public sealed class ItemTransferService
	{
		private readonly ContentDatabase _database;

		private bool _isTransferring;

		public bool IsTransferring => _isTransferring;

		public event Action<ItemTransferResult> TransferCompleted;

		public ItemTransferService(ContentDatabase database)
		{
			_database = database ?? throw new ArgumentNullException("database");
		}

		public ItemTransferResult Transfer(IItemContainer source, int sourceSlot, IItemContainer target, int requestedAmount = int.MaxValue)
		{
			if (_isTransferring)
			{
				return Result(ItemTransferResultCode.Busy, string.Empty, 0, 0);
			}
			_isTransferring = true;
			try
			{
				ItemTransferResult itemTransferResult = TransferInternal(source, sourceSlot, target, requestedAmount);
				this.TransferCompleted?.Invoke(itemTransferResult);
				return itemTransferResult;
			}
			finally
			{
				_isTransferring = false;
			}
		}

		private ItemTransferResult TransferInternal(IItemContainer source, int sourceSlot, IItemContainer target, int requestedAmount)
		{
			if (source == null || target == null || source == target || sourceSlot < 0 || sourceSlot >= source.SlotCapacity || requestedAmount <= 0)
			{
				return Result(ItemTransferResultCode.InvalidRequest, string.Empty, 0, 0);
			}
			if (!source.TryGetSlot(sourceSlot, out var stack) || stack.IsEmpty)
			{
				return Result(ItemTransferResultCode.SourceEmpty, string.Empty, 0, 0);
			}
			if (!_database.TryGetItem(stack.ItemId, out var value))
			{
				return Result(ItemTransferResultCode.InvalidRequest, stack.ItemId, 0, stack.Quantity);
			}
			int num = Math.Min(stack.Quantity, requestedAmount);
			ItemStack[] array = source.ExportSlots();
			ItemStack[] array2 = target.ExportSlots();
			if (array.Length != source.SlotCapacity || array2.Length != target.SlotCapacity)
			{
				return Result(ItemTransferResultCode.InvalidRequest, stack.ItemId, 0, num);
			}
			ItemStack[] array3 = Copy(array);
			ItemStack[] slots = Copy(array2);
			int remainder = num;
			AddToTarget(slots, stack, value, ref remainder);
			int num2 = num - remainder;
			if (num2 == 0)
			{
				return Result(ItemTransferResultCode.TargetFull, stack.ItemId, 0, num);
			}
			int num3 = stack.Quantity - num2;
			array3[sourceSlot] = ((num3 == 0) ? ItemStack.Empty : stack.WithQuantity(value, num3));
			if (!target.TryImportSlots(slots, out var error) || !source.TryImportSlots(array3, out error))
			{
				target.TryImportSlots(array2, out error);
				source.TryImportSlots(array, out error);
				return Result(ItemTransferResultCode.InvalidRequest, stack.ItemId, 0, num);
			}
			return Result((remainder != 0) ? ItemTransferResultCode.Partial : ItemTransferResultCode.Success, stack.ItemId, num2, remainder);
		}

		private static void AddToTarget(ItemStack[] slots, ItemStack sourceStack, ItemDefinition definition, ref int remainder)
		{
			if (definition.MaximumDurability > 0 || !string.IsNullOrEmpty(sourceStack.InstanceId))
			{
				for (int i = 0; i < slots.Length; i++)
				{
					if (remainder <= 0)
					{
						break;
					}
					if (slots[i].IsEmpty)
					{
						int num = Math.Min(sourceStack.Quantity, remainder);
						slots[i] = sourceStack.WithQuantity(definition, num);
						remainder -= num;
					}
				}
				return;
			}
			for (int j = 0; j < slots.Length; j++)
			{
				if (remainder <= 0)
				{
					break;
				}
				if (!slots[j].IsEmpty && string.Equals(slots[j].ItemId, definition.Id, StringComparison.Ordinal))
				{
					slots[j].TryAdd(definition, remainder, out slots[j], out remainder);
				}
			}
			for (int k = 0; k < slots.Length; k++)
			{
				if (remainder <= 0)
				{
					break;
				}
				if (slots[k].IsEmpty)
				{
					slots[k].TryAdd(definition, remainder, out slots[k], out remainder);
				}
			}
		}

		private static ItemStack[] Copy(ItemStack[] source)
		{
			ItemStack[] array = new ItemStack[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = source[i].Copy();
			}
			return array;
		}

		private static ItemTransferResult Result(ItemTransferResultCode code, string itemId, int transferred, int remainder)
		{
			return new ItemTransferResult(code, itemId, transferred, remainder);
		}
	}

	public readonly struct ItemTransferResult
	{
		public ItemTransferResultCode Code { get; }

		public string ItemId { get; }

		public int Transferred { get; }

		public int Remainder { get; }

		public bool Succeeded
		{
			get
			{
				if (Code != ItemTransferResultCode.Success)
				{
					return Code == ItemTransferResultCode.Partial;
				}
				return true;
			}
		}

		public ItemTransferResult(ItemTransferResultCode code, string itemId, int transferred, int remainder)
		{
			Code = code;
			ItemId = itemId ?? string.Empty;
			Transferred = transferred;
			Remainder = remainder;
		}
	}
}
