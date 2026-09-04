using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class EquipmentDurabilityService
	{
		private static readonly EquipmentSlot[] ArmorSlots = new EquipmentSlot[4]
		{
			EquipmentSlot.Head,
			EquipmentSlot.Chest,
			EquipmentSlot.Hands,
			EquipmentSlot.Legs
		};

		private readonly PlayerEquipment _equipment;

		private readonly ContentDatabase _content;

		public EquipmentDurabilityService(PlayerEquipment equipment, ContentDatabase content)
		{
			_equipment = equipment ?? throw new ArgumentNullException("equipment");
			_content = content ?? throw new ArgumentNullException("content");
		}

		public bool TryWearWeapon(string itemId, int wear, out bool broken)
		{
			broken = false;
			if (string.IsNullOrWhiteSpace(itemId) || wear <= 0)
			{
				return false;
			}
			if (Matches(EquipmentSlot.Weapon1, itemId))
			{
				return TryWear(EquipmentSlot.Weapon1, wear, out broken);
			}
			return Matches(EquipmentSlot.Weapon2, itemId) && TryWear(EquipmentSlot.Weapon2, wear, out broken);
		}

		public bool TryWear(EquipmentSlot slot, int wear, out bool broken)
		{
			broken = false;
			if (wear <= 0 || !_equipment.TryGetSlot(slot, out var stack) || stack.Durability <= 0 || !_content.TryGetItem(stack.ItemId, out var _))
			{
				return false;
			}
			DurabilityResult durabilityResult = DurabilityRules.Apply(stack.Durability, wear);
			broken = durabilityResult.IsBroken;
			ItemStack removed;
			if (broken)
			{
				return _equipment.TryUnequip(slot, out removed);
			}
			ItemStack stack2 = new ItemStack(stack.ItemId, stack.Quantity, stack.InstanceId, durabilityResult.Remaining);
			string error;
			return _equipment.TryEquip(slot, stack2, out error);
		}

		public float TotalArmorProtection(float maximumProtection = 0.6f)
		{
			List<float> list = new List<float>(ArmorSlots.Length);
			EquipmentSlot[] armorSlots = ArmorSlots;
			foreach (EquipmentSlot slot in armorSlots)
			{
				if (TryGetArmor(slot, out var _, out var item))
				{
					list.Add(item.ProtectionContribution);
				}
			}
			return ProtectionRules.TotalProtection(list, maximumProtection);
		}

		public float TotalArmorHealthBonus()
		{
			List<float> list = new List<float>(ArmorSlots.Length);
			EquipmentSlot[] armorSlots = ArmorSlots;
			foreach (EquipmentSlot slot in armorSlots)
			{
				if (TryGetArmor(slot, out var _, out var item))
				{
					list.Add(item.ProtectionContribution);
				}
			}
			return ProtectionRules.ArmorHealthBonus(list);
		}

		public ArmorWearSummary WearArmor(float receivedDamage)
		{
			int num = DurabilityRules.ArmorWear(receivedDamage);
			if (num <= 0)
			{
				return new ArmorWearSummary(0, Array.Empty<string>());
			}
			int num2 = 0;
			List<string> list = new List<string>();
			EquipmentSlot[] armorSlots = ArmorSlots;
			foreach (EquipmentSlot slot in armorSlots)
			{
				if (TryGetArmor(slot, out var _, out var item) && TryWear(slot, num, out var broken))
				{
					num2++;
					if (broken)
					{
						list.Add(item.DisplayName);
					}
				}
			}
			return new ArmorWearSummary(num2, list.ToArray());
		}

		private bool Matches(EquipmentSlot slot, string itemId)
		{
			ItemStack stack;
			return _equipment.TryGetSlot(slot, out stack) && string.Equals(stack.ItemId, itemId, StringComparison.Ordinal);
		}

		private bool TryGetArmor(EquipmentSlot slot, out ItemStack stack, out ItemDefinition item)
		{
			item = null;
			return _equipment.TryGetSlot(slot, out stack) && _content.TryGetItem(stack.ItemId, out item) && item.Category == ItemCategory.Armor;
		}
	}

	public readonly struct ArmorWearSummary
	{
		public int WornParts { get; }

		public IReadOnlyList<string> BrokenItemNames { get; }

		public ArmorWearSummary(int wornParts, string[] brokenItemNames)
		{
			WornParts = wornParts;
			BrokenItemNames = brokenItemNames ?? Array.Empty<string>();
		}
	}
}
