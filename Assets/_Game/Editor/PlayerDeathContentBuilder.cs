using Eidren.UI;
using System.IO;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
public static class PlayerDeathContentBuilder
{
	public const string PrefabPath = "Assets/_Game/Resources/UI/PlayerDeathWindow.prefab";

	private static readonly Color Ink = new Color(0.018f, 0.03f, 0.042f, 0.98f);

	private static readonly Color Panel = new Color(0.055f, 0.085f, 0.1f, 1f);

	private static readonly Color Cream = new Color(0.96f, 0.92f, 0.8f, 1f);

	private static readonly Color Ember = new Color(0.82f, 0.31f, 0.18f, 1f);

	private static readonly Color Gold = new Color(0.95f, 0.64f, 0.17f, 1f);

	private static Font _font;

	private static Sprite _uiSprite;

	[MenuItem("Eidren/Player/Build Death Window")]
	public static void Build()
	{
		EnsureFolder("Assets/_Game/Resources", "UI");
		_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		_uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
		GameObject root = new GameObject("PlayerDeathWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlayerDeathWindow));
		try
		{
			Canvas component = root.GetComponent<Canvas>();
			component.renderMode = RenderMode.ScreenSpaceOverlay;
			component.sortingOrder = 120;
			CanvasScaler component2 = root.GetComponent<CanvasScaler>();
			component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			component2.referenceResolution = new Vector2(1920f, 1080f);
			component2.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			component2.matchWidthOrHeight = 1f;
			RectTransform safe = Rect("SafeArea", root.transform);
			Stretch(safe);
			safe.gameObject.AddComponent<SafeAreaPanel>();
			RectTransform overlay = Rect("DeathPanel", safe);
			Stretch(overlay);
			overlay.gameObject.AddComponent<Image>().color = new Color(0.005f, 0.01f, 0.016f, 0.9f);
			RectTransform card = Rect("DefeatCard", overlay);
			Place(card, Vector2.zero, new Vector2(760f, 500f), new Vector2(0.5f, 0.5f));
			Image image = card.gameObject.AddComponent<Image>();
			image.sprite = _uiSprite;
			image.type = Image.Type.Sliced;
			image.color = Ink;
			RectTransform rectTransform = Rect("EmberAccent", card);
			Place(rectTransform, new Vector2(0f, -2f), new Vector2(620f, 8f), new Vector2(0.5f, 1f));
			rectTransform.gameObject.AddComponent<Image>().color = Ember;
			Place(TextElement("Title", card, "BESIEGT", 54, TextAnchor.MiddleCenter, Cream).rectTransform, new Vector2(0f, -64f), new Vector2(650f, 82f), new Vector2(0.5f, 1f));
			Place(TextElement("Subtitle", card, "Die Heimat ruft dich zurück.", 23, TextAnchor.MiddleCenter, new Color(0.7f, 0.78f, 0.76f, 1f)).rectTransform, new Vector2(0f, -154f), new Vector2(650f, 54f), new Vector2(0.5f, 1f));
			Text area = TextElement("Area", card, string.Empty, 20, TextAnchor.MiddleCenter, Gold);
			Place(area.rectTransform, new Vector2(0f, -218f), new Vector2(650f, 46f), new Vector2(0.5f, 1f));
			Button revive = ButtonElement("ReviveButton", card, "WIEDERBELEBEN", Gold);
			Place(revive.GetComponent<RectTransform>(), new Vector2(0f, 118f), new Vector2(430f, 76f), new Vector2(0.5f, 0f));
			Button menu = ButtonElement("MainMenuButton", card, "HAUPTMENÜ", Panel);
			Place(menu.GetComponent<RectTransform>(), new Vector2(0f, 34f), new Vector2(430f, 62f), new Vector2(0.5f, 0f));
			root.GetComponent<PlayerDeathWindow>().ConfigureReferences(overlay.gameObject, revive, menu, area);
			overlay.gameObject.SetActive(value: false);
			if (PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/UI/PlayerDeathWindow.prefab") == null)
			{
				throw new IOException("Could not save Assets/_Game/Resources/UI/PlayerDeathWindow.prefab.");
			}
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: player death window prefab built.");
	}

	private static Button ButtonElement(string name, Transform parent, string label, Color color)
	{
		RectTransform rect = Rect(name, parent);
		Image image = rect.gameObject.AddComponent<Image>();
		image.sprite = _uiSprite;
		image.type = Image.Type.Sliced;
		image.color = color;
		Button button = rect.gameObject.AddComponent<Button>();
		ColorBlock colors = button.colors;
		colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
		colors.pressedColor = Color.Lerp(color, Color.black, 0.16f);
		colors.disabledColor = new Color(0.18f, 0.22f, 0.23f, 0.72f);
		button.colors = colors;
		Stretch(TextElement("Label", rect, label, 24, TextAnchor.MiddleCenter, Cream).rectTransform, 8f);
		return button;
	}

	private static Text TextElement(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Color color)
	{
		Text text = Rect(name, parent).gameObject.AddComponent<Text>();
		text.font = _font;
		text.text = value;
		text.fontSize = fontSize;
		text.fontStyle = FontStyle.Bold;
		text.alignment = alignment;
		text.color = color;
		text.raycastTarget = false;
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
