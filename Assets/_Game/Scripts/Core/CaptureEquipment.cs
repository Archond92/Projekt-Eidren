using Eidren.Data;
using System;

namespace Eidren.Core.Services
{
	public sealed class CaptureEquipment
	{
		private readonly PlayerEquipment _equipment;

		private readonly ContentDatabase _content;

		private readonly PlayerInventory _inventory;

		public CaptureEquipment(PlayerEquipment equipment, ContentDatabase content, PlayerInventory inventory = null)
		{
			_equipment = equipment ?? throw new ArgumentNullException("equipment");
			_content = content ?? throw new ArgumentNullException("content");
			_inventory = inventory;
		}

		public CaptureLoadout ReadLoadout()
		{
			ItemStack stack;
			bool hasDevice = _equipment.TryGetSlot(EquipmentSlot.CatchDevice, out stack) && string.Equals(stack.ItemId, "catch_device", StringComparison.Ordinal);
			if (!_equipment.TryGetSlot(EquipmentSlot.Battery, out var stack2) || !string.Equals(stack2.ItemId, "battery", StringComparison.Ordinal))
			{
				return new CaptureLoadout(hasDevice, hasBattery: false, chargeIsKnown: false, 0f);
			}
			CaptureRulesDefinition captureRules = _content.GetCaptureRules();
			if (captureRules == null || !_content.TryGetItem(stack2.ItemId, out var value) || !captureRules.TryGetCharge(value.Tier, out var charge))
			{
				return new CaptureLoadout(hasDevice, hasBattery: true, chargeIsKnown: false, 0f);
			}
			return new CaptureLoadout(hasDevice, hasBattery: true, chargeIsKnown: true, charge);
		}

		public bool TryConsumeBattery(out string error)
		{
			if (!_equipment.TryUnequip(EquipmentSlot.Battery, out var removed) || removed.IsEmpty)
			{
				error = "No battery was equipped to consume.";
				return false;
			}
			ReloadFromBackpack();
			error = string.Empty;
			return true;
		}

		private void ReloadFromBackpack()
		{
			if (_inventory != null && _inventory.Remove("battery", 1) && !_equipment.TryEquip(EquipmentSlot.Battery, new ItemStack("battery", 1), out var _))
			{
				_inventory.Add("battery", 1);
			}
		}
	}
}
