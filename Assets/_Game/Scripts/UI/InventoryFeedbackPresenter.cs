using Eidren.Core.Services;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class InventoryFeedbackPresenter : MonoBehaviour
	{
		private PlayerInventory _inventory;

		private ContentDatabase _database;

		private GameObject _panel;

		private Image _icon;

		private Text _label;

		private Coroutine _hideRoutine;

		public string LastMessage { get; private set; } = string.Empty;

		public bool IsVisible => _panel != null && _panel.activeSelf;

		public void Initialize(PlayerInventory inventory, ContentDatabase database)
		{
			Unbind();
			_inventory = inventory ?? throw new ArgumentNullException("inventory");
			_database = database ?? throw new ArgumentNullException("database");
			BuildCanvas();
			_inventory.Feedback += HandleFeedback;
		}

		private void BuildCanvas()
		{
			Canvas canvas = base.gameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 55;
			CanvasScaler canvasScaler = base.gameObject.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
			canvasScaler.matchWidthOrHeight = 0.5f;
			base.gameObject.AddComponent<GraphicRaycaster>();
			RectTransform rectTransform = Rect("SafeArea", base.transform);
			Stretch(rectTransform);
			rectTransform.gameObject.AddComponent<SafeAreaPanel>();
			RectTransform rectTransform2 = Rect("PickupFeedback", rectTransform);
			Vector2 anchorMin = (rectTransform2.anchorMax = new Vector2(0.5f, 1f));
			rectTransform2.anchorMin = anchorMin;
			rectTransform2.pivot = new Vector2(0.5f, 1f);
			rectTransform2.anchoredPosition = new Vector2(0f, -34f);
			rectTransform2.sizeDelta = new Vector2(430f, 72f);
			_panel = rectTransform2.gameObject;
			Image image = rectTransform2.gameObject.AddComponent<Image>();
			image.color = new Color(0.055f, 0.09f, 0.08f, 0.94f);
			RectTransform rectTransform3 = Rect("Icon", rectTransform2);
			anchorMin = (rectTransform3.anchorMax = new Vector2(0f, 0.5f));
			rectTransform3.anchorMin = anchorMin;
			rectTransform3.pivot = new Vector2(0f, 0.5f);
			rectTransform3.anchoredPosition = new Vector2(16f, 0f);
			rectTransform3.sizeDelta = Vector2.one * 48f;
			_icon = rectTransform3.gameObject.AddComponent<Image>();
			_icon.preserveAspect = true;
			_icon.raycastTarget = false;
			RectTransform rectTransform4 = Rect("Label", rectTransform2);
			rectTransform4.anchorMin = Vector2.zero;
			rectTransform4.anchorMax = Vector2.one;
			rectTransform4.offsetMin = new Vector2(76f, 8f);
			rectTransform4.offsetMax = new Vector2(-14f, -8f);
			_label = rectTransform4.gameObject.AddComponent<Text>();
			_label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			_label.fontSize = 24;
			_label.fontStyle = FontStyle.Bold;
			_label.alignment = TextAnchor.MiddleLeft;
			_label.color = new Color(0.9f, 0.87f, 0.68f);
			_label.raycastTarget = false;
			_panel.SetActive(value: false);
		}

		private void HandleFeedback(InventoryFeedback feedback)
		{
			if (feedback.Type == InventoryFeedbackType.ItemAdded || feedback.Type == InventoryFeedbackType.InventoryFull)
			{
				_database.TryGetItem(feedback.ItemId, out var value);
				if (feedback.Type == InventoryFeedbackType.InventoryFull)
				{
					Show("INVENTAR VOLL", (value != null) ? value.Icon : null, new Color(1f, 0.48f, 0.3f));
					return;
				}
				string arg = ((value != null) ? value.DisplayName : feedback.ItemId);
				Show($"+{feedback.Amount} {arg}", (value != null) ? value.Icon : null, new Color(0.9f, 0.87f, 0.68f));
			}
		}

		private void Show(string message, Sprite icon, Color color)
		{
			LastMessage = message ?? string.Empty;
			_label.text = LastMessage;
			_label.color = color;
			_icon.sprite = icon;
			_icon.enabled = icon != null;
			_panel.SetActive(value: true);
			if (_hideRoutine != null)
			{
				StopCoroutine(_hideRoutine);
			}
			_hideRoutine = StartCoroutine(HideAfterDelay());
		}

		private IEnumerator HideAfterDelay()
		{
			yield return new WaitForSecondsRealtime(1.55f);
			if (_panel != null)
			{
				_panel.SetActive(value: false);
			}
			_hideRoutine = null;
		}

		private static RectTransform Rect(string name, Transform parent)
		{
			GameObject gameObject = new GameObject(name, typeof(RectTransform));
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.SetParent(parent, worldPositionStays: false);
			return component;
		}

		private static void Stretch(RectTransform rect)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}

		private void Unbind()
		{
			if (_inventory != null)
			{
				_inventory.Feedback -= HandleFeedback;
			}
		}

		private void OnDestroy()
		{
			Unbind();
		}
	}
}
