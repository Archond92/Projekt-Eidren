using Eidren.UI;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// F32-006: Baut das Fenster fuer das Eidra-Gespann. Muster und Farben
	/// wie das Inventarfenster; die Zeilen sind autoriert (16 Stueck in einer
	/// Scrollflaeche) und werden zur Laufzeit nur ein- und ausgeblendet.
	/// </summary>
	public static class EidraTeamUiPrefabBuilder
	{
		public const string PrefabPath = "Assets/_Game/Resources/UI/EidraTeamWindow.prefab";

		private const int RowCount = 16;

		private static readonly Color Ink = new Color(0.025f, 0.05f, 0.07f, 0.98f);

		private static readonly Color Slot = new Color(0.09f, 0.16f, 0.18f, 1f);

		private static readonly Color Cream = new Color(0.96f, 0.92f, 0.8f, 1f);

		private static readonly Color Gold = new Color(0.95f, 0.64f, 0.17f, 1f);

		private static Font _font;

		private static Sprite _uiSprite;

		[MenuItem("Eidren/UI/F32-006 Eidra-Fenster bauen")]
		public static void BuildPrefab()
		{
			EnsureFolder("Assets/_Game/Resources", "UI");
			_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			_uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
			GameObject root = new GameObject("EidraTeamWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(EidraTeamWindow));
			try
			{
				ConfigureCanvas(root);
				RectTransform safeArea = Rect("SafeArea", root.transform);
				Stretch(safeArea);
				RectTransform panel = Rect("EidraPanel", safeArea);
				Stretch(panel);
				panel.gameObject.AddComponent<Image>().color = new Color(0.005f, 0.015f, 0.02f, 0.84f);
				RectTransform card = Rect("EidraCard", panel);
				Place(card, Vector2.zero, new Vector2(1180f, 800f), new Vector2(0.5f, 0.5f));
				Image cardImage = card.gameObject.AddComponent<Image>();
				cardImage.sprite = _uiSprite;
				cardImage.type = Image.Type.Sliced;
				cardImage.color = Ink;

				Place(TextElement("Title", card, "EIDRA-GESPANN", 34, TextAnchor.MiddleLeft, Cream).rectTransform, new Vector2(42f, -28f), new Vector2(520f, 58f), new Vector2(0f, 1f));
				Place(TextElement("InputHint", card, "[G] schliessen · Platz waehlen, um zu tauschen", 16, TextAnchor.MiddleRight, new Color(0.66f, 0.76f, 0.75f)).rectTransform, new Vector2(-42f, -34f), new Vector2(560f, 42f), new Vector2(1f, 1f));
				Text summary = TextElement("Summary", card, "PLATZ 1: –  ·  PLATZ 2: –", 22, TextAnchor.MiddleLeft, Gold);
				Place(summary.rectTransform, new Vector2(42f, -92f), new Vector2(1000f, 40f), new Vector2(0f, 1f));

				RectTransform viewport = Rect("Viewport", card);
				Place(viewport, new Vector2(42f, -140f), new Vector2(1096f, 552f), new Vector2(0f, 1f));
				Image viewportImage = viewport.gameObject.AddComponent<Image>();
				viewportImage.color = new Color(0.02f, 0.04f, 0.05f, 0.9f);
				viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
				RectTransform content = Rect("Content", viewport);
				content.anchorMin = new Vector2(0f, 1f);
				content.anchorMax = new Vector2(1f, 1f);
				content.pivot = new Vector2(0.5f, 1f);
				content.anchoredPosition = Vector2.zero;
				content.sizeDelta = new Vector2(0f, RowCount * 60f);
				VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
				layout.spacing = 6f;
				layout.padding = new RectOffset(10, 10, 10, 10);
				layout.childControlHeight = false;
				layout.childControlWidth = true;
				layout.childForceExpandHeight = false;
				layout.childForceExpandWidth = true;
				ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
				scroll.content = content;
				scroll.viewport = viewport;
				scroll.horizontal = false;
				scroll.movementType = ScrollRect.MovementType.Clamped;
				scroll.scrollSensitivity = 28f;

				List<EidraTeamRowView> rows = new List<EidraTeamRowView>(RowCount);
				for (int i = 0; i < RowCount; i++)
				{
					rows.Add(BuildRow(content, i));
				}

				Text overflow = TextElement("Overflow", card, string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.72f, 0.66f, 0.5f));
				Place(overflow.rectTransform, new Vector2(42f, 108f), new Vector2(700f, 32f), new Vector2(0f, 0f));
				Text feedback = TextElement("Feedback", card, string.Empty, 20, TextAnchor.MiddleLeft, new Color(1f, 0.55f, 0.35f));
				Place(feedback.rectTransform, new Vector2(42f, 62f), new Vector2(880f, 40f), new Vector2(0f, 0f));
				Button close = ButtonElement("Close", card, "SCHLIESSEN", Gold);
				Place(close.GetComponent<RectTransform>(), new Vector2(-42f, 46f), new Vector2(220f, 60f), new Vector2(1f, 0f));

				root.GetComponent<EidraTeamWindow>().ConfigureReferences(panel.gameObject, summary, feedback, overflow, close, rows.ToArray());
				PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
				AssetDatabase.SaveAssets();
				Debug.Log("[Eidren] F32-006: Eidra-Fenster gebaut unter " + PrefabPath);
			}
			finally
			{
				Object.DestroyImmediate(root);
			}
		}

		private static EidraTeamRowView BuildRow(Transform parent, int index)
		{
			GameObject rowObject = new GameObject($"Row_{index:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(EidraTeamRowView));
			rowObject.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rect = rowObject.GetComponent<RectTransform>();
			rect.sizeDelta = new Vector2(0f, 54f);
			LayoutElement element = rowObject.AddComponent<LayoutElement>();
			element.minHeight = 54f;
			element.preferredHeight = 54f;
			Image background = rowObject.GetComponent<Image>();
			background.sprite = _uiSprite;
			background.type = Image.Type.Sliced;
			background.color = Slot;

			Text name = TextElement("Name", rowObject.transform, "–", 22, TextAnchor.MiddleLeft, Cream);
			Place(name.rectTransform, new Vector2(16f, 0f), new Vector2(520f, 46f), new Vector2(0f, 0.5f));
			Text state = TextElement("State", rowObject.transform, string.Empty, 17, TextAnchor.MiddleLeft, new Color(0.7f, 0.8f, 0.78f));
			Place(state.rectTransform, new Vector2(548f, 0f), new Vector2(260f, 46f), new Vector2(0f, 0.5f));
			Button first = ButtonElement("ToSlot1", rowObject.transform, "AUF PLATZ 1", Gold);
			Place(first.GetComponent<RectTransform>(), new Vector2(-232f, 0f), new Vector2(190f, 42f), new Vector2(1f, 0.5f));
			Button second = ButtonElement("ToSlot2", rowObject.transform, "AUF PLATZ 2", Gold);
			Place(second.GetComponent<RectTransform>(), new Vector2(-24f, 0f), new Vector2(190f, 42f), new Vector2(1f, 0.5f));

			EidraTeamRowView view = rowObject.GetComponent<EidraTeamRowView>();
			view.ConfigureReferences(name, state, first, second);
			return view;
		}

		private static void ConfigureCanvas(GameObject root)
		{
			Canvas canvas = root.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 80;
			CanvasScaler scaler = root.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 1f;
		}

		private static Button ButtonElement(string name, Transform parent, string label, Color color)
		{
			GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
			root.transform.SetParent(parent, worldPositionStays: false);
			Image image = root.GetComponent<Image>();
			image.sprite = _uiSprite;
			image.type = Image.Type.Sliced;
			image.color = color;
			Button button = root.GetComponent<Button>();
			button.targetGraphic = image;
			Text text = TextElement("Label", root.transform, label, 18, TextAnchor.MiddleCenter, Ink);
			text.fontStyle = FontStyle.Bold;
			Stretch(text.rectTransform, 6f);
			return button;
		}

		private static Text TextElement(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
		{
			GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			Text text = gameObject.GetComponent<Text>();
			text.font = _font;
			text.text = value;
			text.fontSize = size;
			text.alignment = alignment;
			text.color = color;
			text.horizontalOverflow = HorizontalWrapMode.Overflow;
			return text;
		}

		private static RectTransform Rect(string name, Transform parent)
		{
			GameObject gameObject = new GameObject(name, typeof(RectTransform));
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			return gameObject.GetComponent<RectTransform>();
		}

		private static void Place(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
		{
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.pivot = anchor;
			rect.anchoredPosition = position;
			rect.sizeDelta = size;
		}

		private static void Stretch(RectTransform rect, float inset = 0f)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.one * inset;
			rect.offsetMax = Vector2.one * (0f - inset);
		}

		private static void EnsureFolder(string parent, string child)
		{
			if (!AssetDatabase.IsValidFolder(parent + "/" + child))
			{
				AssetDatabase.CreateFolder(parent, child);
			}
		}
	}
}
