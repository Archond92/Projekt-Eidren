using Eidren.Interaction;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	/// <summary>
	/// Schwebendes Weltlabel über einer Schmiedekiste mit ihrem
	/// Markenpreis — von weitem lesbar, damit der Preis nicht erst im
	/// Interaktionsprompt auftaucht. Sichtbar nur, solange der Kauf
	/// ansteht; nach dem Bezahlen verschwindet es und kehrt zurück,
	/// wenn die geleerte Kiste wieder kaufbar wird. Angebracht vom
	/// EidraForgeSceneController (die UI-Schicht sucht selbst nichts).
	/// </summary>
	public sealed class ForgeChestPriceLabel : MonoBehaviour
	{
		private const float LabelHeight = 1.9f;

		private EidraForgeChestContainer _chest;

		private Canvas _canvas;

		private Text _text;

		private Camera _camera;

		public void Configure(EidraForgeChestContainer chest)
		{
			_chest = chest;
			if (_canvas == null)
			{
				BuildLabel();
			}
			_chest.Changed += OnChestChanged;
			Refresh();
		}

		private void BuildLabel()
		{
			GameObject canvasObject = new GameObject("PriceLabel", typeof(RectTransform), typeof(Canvas));
			canvasObject.transform.SetParent(base.transform, worldPositionStays: false);
			canvasObject.transform.localPosition = new Vector3(0f, LabelHeight, 0f);
			canvasObject.transform.localScale = Vector3.one * 0.012f;
			_canvas = canvasObject.GetComponent<Canvas>();
			_canvas.renderMode = RenderMode.WorldSpace;
			_canvas.sortingOrder = 16;
			((RectTransform)canvasObject.transform).sizeDelta = new Vector2(220f, 26f);
			GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
			textObject.transform.SetParent(canvasObject.transform, worldPositionStays: false);
			_text = textObject.GetComponent<Text>();
			_text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			_text.fontSize = 16;
			_text.fontStyle = FontStyle.Bold;
			_text.alignment = TextAnchor.MiddleCenter;
			// Marken-Gold — dieselbe Familie wie die Kisten-Markierung der Karte.
			_text.color = new Color(0.95f, 0.78f, 0.36f, 1f);
			_text.horizontalOverflow = HorizontalWrapMode.Overflow;
			_text.raycastTarget = false;
			Outline outline = textObject.GetComponent<Outline>();
			outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
			outline.effectDistance = new Vector2(1f, -1f);
			RectTransform rect = _text.rectTransform;
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}

		private void OnChestChanged(StorageContainerChange change)
		{
			Refresh();
		}

		private void Refresh()
		{
			if (_canvas == null || _chest == null)
			{
				return;
			}
			bool sichtbar = _chest.RequiresMarkPayment;
			if (_canvas.gameObject.activeSelf != sichtbar)
			{
				_canvas.gameObject.SetActive(sichtbar);
			}
			if (sichtbar && _text != null)
			{
				_text.text = $"{_chest.MarkPrice} MARKEN";
			}
		}

		private void LateUpdate()
		{
			if (_canvas == null || !_canvas.gameObject.activeSelf)
			{
				return;
			}
			if (_camera == null)
			{
				_camera = Camera.main;
			}
			if (_camera != null)
			{
				_canvas.transform.rotation = _camera.transform.rotation;
			}
		}

		private void OnDestroy()
		{
			if (_chest != null)
			{
				_chest.Changed -= OnChestChanged;
			}
		}
	}
}
