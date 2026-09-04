using Eidren.Core.Services;
using Eidren.Data;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	[RequireComponent(typeof(Button))]
	public sealed class EquipmentSlotView : MonoBehaviour
	{
		[SerializeField]
		private EquipmentSlot slot;

		[SerializeField]
		private Button button;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Text slotLabel;

		[SerializeField]
		private Text itemName;

		[SerializeField]
		private Text emptyHint;

		[SerializeField]
		private Image occupiedMarker;

		private Action<EquipmentSlot> _pressed;

		public EquipmentSlot Slot => slot;

		public Button Button => button;

		public Image Icon => icon;

		public Text ItemName => itemName;

		public void ConfigureReferences(EquipmentSlot configuredSlot, Button configuredButton, Image configuredIcon, Text configuredSlotLabel, Text configuredItemName, Text configuredEmptyHint, Image configuredOccupiedMarker)
		{
			slot = configuredSlot;
			button = configuredButton;
			icon = configuredIcon;
			slotLabel = configuredSlotLabel;
			itemName = configuredItemName;
			emptyHint = configuredEmptyHint;
			occupiedMarker = configuredOccupiedMarker;
		}

		public void Bind(Action<EquipmentSlot> pressed)
		{
			_pressed = pressed;
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(delegate
			{
				_pressed?.Invoke(slot);
			});
		}

		public void Render(ItemStack stack, ItemDefinition definition)
		{
			bool flag = !stack.IsEmpty && definition != null;
			icon.enabled = flag && definition.Icon != null;
			icon.sprite = (icon.enabled ? definition.Icon : null);
			slotLabel.text = SlotLabel(slot).ToUpperInvariant();
			itemName.text = (flag ? EquipmentText(stack, definition) : "Leer");
			emptyHint.text = (flag ? "ANTIPPEN ZUM ABLEGEN" : "ANTIPPEN ZUM ANLEGEN");
			occupiedMarker.gameObject.SetActive(flag);
			button.interactable = true;
		}

		private static string SlotLabel(EquipmentSlot value)
		{
			if (1 == 0)
			{
			}
			string result = value switch
			{
				EquipmentSlot.Weapon1 => "Waffe I", 
				EquipmentSlot.Weapon2 => "Waffe II", 
				EquipmentSlot.Head => "Kopf", 
				EquipmentSlot.Chest => "Brust", 
				EquipmentSlot.Hands => "Haende", 
				EquipmentSlot.Legs => "Beine", 
				EquipmentSlot.CatchDevice => "Fanggeraet", 
				EquipmentSlot.Battery => "Batterie", 
				_ => value.ToString(), 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private static string EquipmentText(ItemStack stack, ItemDefinition definition)
		{
			string text = ((definition.MaximumDurability > 0) ? $" · {stack.Durability}/{definition.MaximumDurability}" : string.Empty);
			string text2 = ((definition.ProtectionContribution > 0f) ? $" · Schutz {definition.ProtectionContribution:P0}" : string.Empty);
			return definition.DisplayName + text + text2;
		}
	}
}
