using Eidren.Core.Services;
using Eidren.Data;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	[RequireComponent(typeof(Button))]
	public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, ISelectHandler, ISubmitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Text quantity;

		[SerializeField]
		private Text fallback;

		[SerializeField]
		private GameObject selectionFrame;

		private int _slotIndex;

		private Action<int> _pointerPressed;

		private Action<int> _focused;

		private Action<int> _submitted;

		private Action<int, PointerEventData> _dragStarted;

		private Action<PointerEventData> _dragged;

		private Action<PointerEventData> _dragEnded;

		private Action<int> _dropped;

		private bool _suppressClick;

		public int SlotIndex => _slotIndex;

		public Button Button => button;

		public void ConfigureReferences(Button configuredButton, Image configuredIcon, Text configuredQuantity, Text configuredFallback, GameObject configuredSelectionFrame)
		{
			button = configuredButton;
			icon = configuredIcon;
			quantity = configuredQuantity;
			fallback = configuredFallback;
			selectionFrame = configuredSelectionFrame;
		}

		public void Bind(int slotIndex, Action<int> pointerPressed, Action<int> focused, Action<int> submitted, Action<int, PointerEventData> dragStarted, Action<PointerEventData> dragged, Action<PointerEventData> dragEnded, Action<int> dropped)
		{
			_slotIndex = slotIndex;
			_pointerPressed = pointerPressed;
			_focused = focused;
			_submitted = submitted;
			_dragStarted = dragStarted;
			_dragged = dragged;
			_dragEnded = dragEnded;
			_dropped = dropped;
			button.interactable = true;
		}

		public void Bind(int slotIndex, Action<int> pointerPressed, Action<int> focused, Action<int> submitted)
		{
			Bind(slotIndex, pointerPressed, focused, submitted, null, null, null, null);
		}

		public void Render(ItemStack stack, ItemDefinition definition, bool selected)
		{
			bool flag = !stack.IsEmpty;
			icon.enabled = flag && definition != null && definition.Icon != null;
			icon.sprite = (icon.enabled ? definition.Icon : null);
			bool flag2 = flag && definition != null && definition.MaximumDurability > 0;
			quantity.text = (flag2 ? DurabilityLabel(stack.Durability, definition.MaximumDurability) : ((flag && stack.Quantity > 1) ? stack.Quantity.ToString() : string.Empty));
			quantity.color = (flag2 ? DurabilityColor(stack.Durability, definition.MaximumDurability) : Color.white);
			fallback.text = ((flag && !icon.enabled) ? GetFallback(definition, stack.ItemId) : string.Empty);
			selectionFrame.SetActive(selected);
			base.gameObject.name = $"InventorySlot_{_slotIndex:00}";
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (_suppressClick)
			{
				_suppressClick = false;
			}
			else
			{
				_pointerPressed?.Invoke(_slotIndex);
			}
		}

		public void OnSelect(BaseEventData eventData)
		{
			_focused?.Invoke(_slotIndex);
		}

		public void OnSubmit(BaseEventData eventData)
		{
			_submitted?.Invoke(_slotIndex);
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			_dragStarted?.Invoke(_slotIndex, eventData);
		}

		public void OnDrag(PointerEventData eventData)
		{
			_dragged?.Invoke(eventData);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			_suppressClick = true;
			_dragEnded?.Invoke(eventData);
		}

		public void OnDrop(PointerEventData eventData)
		{
			_dropped?.Invoke(_slotIndex);
		}

		private static string GetFallback(ItemDefinition definition, string itemId)
		{
			string text = ((definition != null) ? definition.DisplayName : itemId);
			return string.IsNullOrWhiteSpace(text) ? "?" : text.Substring(0, 1).ToUpperInvariant();
		}

		private static Color DurabilityColor(int current, int maximum)
		{
			float num = ((maximum > 0) ? ((float)current / (float)maximum) : 1f);
			if (num < 0.1f)
			{
				return new Color(1f, 0.25f, 0.2f);
			}
			if (num < 0.25f)
			{
				return new Color(1f, 0.58f, 0.16f);
			}
			return Color.white;
		}

		private static string DurabilityLabel(int current, int maximum)
		{
			float num = ((maximum > 0) ? Mathf.Clamp01((float)current / (float)maximum) : 1f);
			int num2 = Mathf.RoundToInt(num * 4f);
			return "[" + new string('■', num2) + $"{new string('□', 4 - num2)}] {current}/{maximum}";
		}
	}
}
