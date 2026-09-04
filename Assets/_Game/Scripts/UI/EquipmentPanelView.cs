using Eidren.Core.Services;
using Eidren.Data;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class EquipmentPanelView : MonoBehaviour
	{
		[SerializeField]
		private EquipmentSlotView[] slotViews;

		[SerializeField]
		private Text feedbackText;

		private PlayerEquipment _equipment;

		private PlayerInventory _inventory;

		private ContentDatabase _database;

		private Func<int> _selectedInventorySlot;

		private Action _changed;

		private bool _isBound;

		public EquipmentSlotView[] SlotViews => slotViews;

		public Text FeedbackText => feedbackText;

		public void ConfigureReferences(EquipmentSlotView[] configuredViews, Text configuredFeedback)
		{
			slotViews = configuredViews;
			feedbackText = configuredFeedback;
		}

		public void Initialize(PlayerEquipment equipment, PlayerInventory inventory, ContentDatabase database, Func<int> selectedInventorySlot, Action changed)
		{
			Unbind();
			_equipment = equipment ?? throw new ArgumentNullException("equipment");
			_inventory = inventory ?? throw new ArgumentNullException("inventory");
			_database = database ?? throw new ArgumentNullException("database");
			_selectedInventorySlot = selectedInventorySlot ?? throw new ArgumentNullException("selectedInventorySlot");
			_changed = changed;
			EquipmentSlotView[] array = slotViews;
			foreach (EquipmentSlotView equipmentSlotView in array)
			{
				equipmentSlotView.Bind(HandleSlotPressed);
			}
			Bind();
			feedbackText.text = string.Empty;
			Refresh();
		}

		public void Refresh()
		{
			if (_equipment == null || _database == null)
			{
				return;
			}
			EquipmentSlotView[] array = slotViews;
			foreach (EquipmentSlotView equipmentSlotView in array)
			{
				_equipment.TryGetSlot(equipmentSlotView.Slot, out var stack);
				ItemDefinition value = null;
				if (!stack.IsEmpty)
				{
					_database.TryGetItem(stack.ItemId, out value);
				}
				equipmentSlotView.Render(stack, value);
			}
			if (string.IsNullOrWhiteSpace(feedbackText.text) || feedbackText.text.StartsWith("Gesamtschutz", StringComparison.Ordinal))
			{
				feedbackText.text = ProtectionSummary();
			}
		}

		private void HandleSlotPressed(EquipmentSlot slot)
		{
			if (_equipment.TryGetSlot(slot, out var stack))
			{
				Unequip(slot, stack);
			}
			else
			{
				EquipSelected(slot);
			}
		}

		private void EquipSelected(EquipmentSlot slot)
		{
			int num = _selectedInventorySlot();
			if (!_inventory.TryGetSlot(num, out var stack) || stack.IsEmpty || !_database.TryGetItem(stack.ItemId, out var value))
			{
				SetFeedback("Zuerst links einen Gegenstand waehlen.");
				return;
			}
			ItemStack stack2 = stack.WithQuantity(value, 1);
			if (!_equipment.TryEquip(slot, stack2, out var error))
			{
				SetFeedback(ToPlayerError(error));
				return;
			}
			ItemStack[] array = _inventory.ExportSlots();
			array[num] = ((stack.Quantity == 1) ? ItemStack.Empty : stack.WithQuantity(value, stack.Quantity - 1));
			if (!_inventory.TryImportSlots(array, out var _))
			{
				_equipment.TryUnequip(slot, out var _);
				SetFeedback("Anlegen fehlgeschlagen.");
			}
			else
			{
				SetFeedback(value.DisplayName + " angelegt.");
				_changed?.Invoke();
			}
		}

		private void Unequip(EquipmentSlot slot, ItemStack equipped)
		{
			ItemStack[] slots = _inventory.ExportSlots();
			ItemStack removed;
			string error;
			if (!TryAddExact(slots, equipped, _selectedInventorySlot()))
			{
				SetFeedback("Rucksack voll – kein Platz zum Ablegen.");
			}
			else if (!_equipment.TryUnequip(slot, out removed))
			{
				SetFeedback("Ablegen fehlgeschlagen.");
			}
			else if (!_inventory.TryImportSlots(slots, out error))
			{
				_equipment.TryEquip(slot, removed, out error);
				SetFeedback("Ablegen fehlgeschlagen.");
			}
			else
			{
				ItemDefinition value;
				string text = (_database.TryGetItem(removed.ItemId, out value) ? value.DisplayName : removed.ItemId);
				SetFeedback(text + " in den Rucksack gelegt.");
				_changed?.Invoke();
			}
		}

		private bool TryAddExact(ItemStack[] slots, ItemStack addition, int preferredIndex)
		{
			if (!_database.TryGetItem(addition.ItemId, out var value))
			{
				return false;
			}
			if (preferredIndex >= 0 && preferredIndex < slots.Length && slots[preferredIndex].IsEmpty && addition.Quantity <= value.MaximumStackSize)
			{
				slots[preferredIndex] = new ItemStack(addition.ItemId, addition.Quantity, addition.InstanceId, addition.Durability);
				return true;
			}
			int remainder = addition.Quantity;
			if (string.IsNullOrEmpty(addition.InstanceId))
			{
				for (int i = 0; i < slots.Length; i++)
				{
					if (remainder <= 0)
					{
						break;
					}
					if (!slots[i].IsEmpty && !(slots[i].ItemId != addition.ItemId))
					{
						slots[i].TryAdd(value, remainder, out slots[i], out remainder);
					}
				}
			}
			for (int j = 0; j < slots.Length; j++)
			{
				if (remainder <= 0)
				{
					break;
				}
				if (slots[j].IsEmpty)
				{
					int num = Mathf.Min(remainder, value.MaximumStackSize);
					slots[j] = new ItemStack(addition.ItemId, num, addition.InstanceId, addition.Durability);
					remainder -= num;
				}
			}
			return remainder == 0;
		}

		private void HandleEquipmentChanged(EquipmentChange _)
		{
			Refresh();
		}

		private void SetFeedback(string value)
		{
			feedbackText.text = (string.IsNullOrWhiteSpace(value) ? ProtectionSummary() : (value + " · " + ProtectionSummary()));
			Refresh();
		}

		private string ProtectionSummary()
		{
			if (_equipment == null || _database == null)
			{
				return "Gesamtschutz 0 %";
			}
			float num = new EquipmentDurabilityService(_equipment, _database).TotalArmorProtection();
			return $"Gesamtschutz {num:P0}";
		}

		private static string ToPlayerError(string error)
		{
			if (error != null && error.Contains("not compatible"))
			{
				return "Dieser Gegenstand passt nicht in den Slot.";
			}
			return "Der Gegenstand kann hier nicht angelegt werden.";
		}

		private void Unbind()
		{
			if (_equipment != null && _isBound)
			{
				_equipment.Changed -= HandleEquipmentChanged;
			}
			_isBound = false;
		}

		private void Bind()
		{
			if (_equipment != null && !_isBound && base.isActiveAndEnabled)
			{
				_equipment.Changed += HandleEquipmentChanged;
				_isBound = true;
			}
		}

		private void OnEnable()
		{
			Bind();
			Refresh();
		}

		private void OnDisable()
		{
			Unbind();
		}

		private void OnDestroy()
		{
			Unbind();
		}
	}
}
