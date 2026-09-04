using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class PlayerEquipment
	{
		public const int BaseInventorySlots = 16;

		private readonly Dictionary<EquipmentSlot, ItemStack> _slots = new Dictionary<EquipmentSlot, ItemStack>();

		private ContentDatabase _database;

		public int AdditionalInventorySlots => 0;

		public event Action<EquipmentChange> Changed;

		public PlayerEquipment(ContentDatabase database = null)
		{
			_database = database;
		}

		public void Configure(ContentDatabase database)
		{
			_database = database ?? throw new ArgumentNullException("database");
		}

		public static bool IsUsableInV01(EquipmentSlot slot)
		{
			if (1 == 0)
			{
			}
			bool result = (((uint)slot <= 5u || (uint)(slot - 11) <= 1u) ? true : false);
			if (1 == 0)
			{
			}
			return result;
		}

		public bool TryGetSlot(EquipmentSlot slot, out ItemStack stack)
		{
			if (_slots.TryGetValue(slot, out var value))
			{
				stack = value.Copy();
				return !stack.IsEmpty;
			}
			stack = ItemStack.Empty;
			return false;
		}

		public bool TryEquip(EquipmentSlot slot, ItemStack stack, out string error)
		{
			if (!CanEquip(slot, stack, out error))
			{
				return false;
			}
			_slots[slot] = stack.Copy();
			error = string.Empty;
			this.Changed?.Invoke(new EquipmentChange(slot));
			return true;
		}

		internal bool CanEquip(EquipmentSlot slot, ItemStack stack, out string error)
		{
			if (!Enum.IsDefined(typeof(EquipmentSlot), slot))
			{
				error = $"Unknown equipment slot '{(int)slot}'.";
				return false;
			}
			if (!IsUsableInV01(slot))
			{
				error = $"Equipment slot '{slot}' exists but takes nothing " + "in v0.1.";
				return false;
			}
			if (stack.IsEmpty)
			{
				error = "An equipment slot cannot hold an empty stack.";
				return false;
			}
			if (stack.Quantity != 1)
			{
				error = $"Equipment slot '{slot}' holds exactly one item.";
				return false;
			}
			if (_database == null)
			{
				error = string.Empty;
				return true;
			}
			if (!stack.TryValidate(_database, out error) || !_database.TryGetItem(stack.ItemId, out var value))
			{
				return false;
			}
			if (1 == 0)
			{
			}
			bool flag;
			switch (slot)
			{
			case EquipmentSlot.Weapon1:
			case EquipmentSlot.Weapon2:
				flag = value.Category == ItemCategory.Weapon;
				break;
			case EquipmentSlot.Head:
				flag = IsArmorFor(value, WearableSlot.Head);
				break;
			case EquipmentSlot.Chest:
				flag = IsArmorFor(value, WearableSlot.Chest);
				break;
			case EquipmentSlot.Hands:
				flag = IsArmorFor(value, WearableSlot.Hands);
				break;
			case EquipmentSlot.Legs:
				flag = IsArmorFor(value, WearableSlot.Legs);
				break;
			case EquipmentSlot.CatchDevice:
				flag = value.Id == "catch_device";
				break;
			case EquipmentSlot.Battery:
				flag = value.Id == "battery";
				break;
			default:
				flag = false;
				break;
			}
			if (1 == 0)
			{
			}
			if (!flag)
			{
				error = "Item '" + value.Id + "' is not compatible with slot " + $"'{slot}'.";
				return false;
			}
			error = string.Empty;
			return true;
		}

		internal static bool TryGetDefaultEquipmentSlot(string itemId, out EquipmentSlot slot)
		{
			switch (itemId)
			{
			case "hammer":
				slot = EquipmentSlot.Weapon1;
				return true;
			case "daggers":
				slot = EquipmentSlot.Weapon2;
				return true;
			case "armor_wanderer_hood":
				slot = EquipmentSlot.Head;
				return true;
			case "armor_wanderer_coat":
				slot = EquipmentSlot.Chest;
				return true;
			case "armor_wanderer_bracers":
				slot = EquipmentSlot.Hands;
				return true;
			case "armor_wanderer_legs":
				slot = EquipmentSlot.Legs;
				return true;
			case "catch_device":
				slot = EquipmentSlot.CatchDevice;
				return true;
			case "battery":
				slot = EquipmentSlot.Battery;
				return true;
			default:
				slot = EquipmentSlot.Weapon1;
				return false;
			}
		}

		public bool TryUnequip(EquipmentSlot slot, out ItemStack removed)
		{
			if (!TryGetSlot(slot, out removed))
			{
				return false;
			}
			_slots.Remove(slot);
			this.Changed?.Invoke(new EquipmentChange(slot));
			return true;
		}

		public void Clear()
		{
			if (_slots.Count != 0)
			{
				_slots.Clear();
				this.Changed?.Invoke(new EquipmentChange(EquipmentSlot.Weapon1));
			}
		}

		public EquipmentAssignment[] ExportSlots()
		{
			List<EquipmentAssignment> list = new List<EquipmentAssignment>(_slots.Count);
			foreach (EquipmentSlot value2 in Enum.GetValues(typeof(EquipmentSlot)))
			{
				if (_slots.TryGetValue(value2, out var value) && !value.IsEmpty)
				{
					list.Add(new EquipmentAssignment(value2, value.Copy()));
				}
			}
			return list.ToArray();
		}

		public bool TryImportSlots(IReadOnlyList<EquipmentAssignment> assignments, out string error)
		{
			Dictionary<EquipmentSlot, ItemStack> dictionary = new Dictionary<EquipmentSlot, ItemStack>();
			if (assignments != null)
			{
				foreach (EquipmentAssignment assignment in assignments)
				{
					if (!assignment.Stack.IsEmpty)
					{
						if (dictionary.ContainsKey(assignment.Slot))
						{
							error = $"Equipment slot '{assignment.Slot}' is " + "assigned twice.";
							return false;
						}
						if (!CanEquip(assignment.Slot, assignment.Stack, out error))
						{
							return false;
						}
						dictionary[assignment.Slot] = assignment.Stack.Copy();
					}
				}
			}
			_slots.Clear();
			foreach (KeyValuePair<EquipmentSlot, ItemStack> item in dictionary)
			{
				_slots[item.Key] = item.Value;
			}
			error = string.Empty;
			this.Changed?.Invoke(new EquipmentChange(EquipmentSlot.Weapon1));
			return true;
		}

		private static bool IsArmorFor(ItemDefinition item, WearableSlot slot)
		{
			return item.Category == ItemCategory.Armor && item.WearableSlot == slot;
		}
	}

	public readonly struct EquipmentAssignment
	{
		public EquipmentSlot Slot { get; }

		public ItemStack Stack { get; }

		public EquipmentAssignment(EquipmentSlot slot, ItemStack stack)
		{
			Slot = slot;
			Stack = stack;
		}
	}

	public readonly struct EquipmentChange
	{
		public EquipmentSlot Slot { get; }

		public EquipmentChange(EquipmentSlot slot)
		{
			Slot = slot;
		}
	}
}
