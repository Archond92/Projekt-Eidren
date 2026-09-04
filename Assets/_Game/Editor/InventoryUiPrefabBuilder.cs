using Eidren.Core.Services;
using Eidren.UI;
using System.IO;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
public static class InventoryUiPrefabBuilder
{
	public const string PrefabPath = "Assets/_Game/Resources/UI/InventoryWindow.prefab";

	private static readonly Color Ink = new Color(0.025f, 0.05f, 0.07f, 0.98f);

	private static readonly Color Panel = new Color(0.055f, 0.105f, 0.13f, 0.98f);

	private static readonly Color Slot = new Color(0.09f, 0.16f, 0.18f, 1f);

	private static readonly Color Cream = new Color(0.96f, 0.92f, 0.8f, 1f);

	private static readonly Color Gold = new Color(0.95f, 0.64f, 0.17f, 1f);

	private static Font _font;

	private static Sprite _uiSprite;

	[MenuItem("Eidren/UI/Build Inventory Prefab")]
	public static void BuildInventoryPrefab()
	{
		EnsureFolder("Assets/_Game/Resources", "UI");
		_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		_uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
		GameObject root = new GameObject("InventoryWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(InventoryWindow));
		try
		{
			ConfigureCanvas(root);
			RectTransform safeArea = Rect("SafeArea", root.transform);
			Stretch(safeArea);
			safeArea.gameObject.AddComponent<SafeAreaPanel>();
			Button open = ButtonElement("OpenInventory", safeArea, "RUCKSACK  [I]", Gold);
			Place(open.GetComponent<RectTransform>(), new Vector2(-22f, -20f), new Vector2(190f, 64f), new Vector2(1f, 1f));
			RectTransform panel = Rect("InventoryPanel", safeArea);
			Stretch(panel);
			panel.gameObject.AddComponent<Image>().color = new Color(0.005f, 0.015f, 0.02f, 0.84f);
			RectTransform card = Rect("InventoryCard", panel);
			Place(card, Vector2.zero, new Vector2(1740f, 820f), new Vector2(0.5f, 0.5f));
			Image image = card.gameObject.AddComponent<Image>();
			image.sprite = _uiSprite;
			image.type = Image.Type.Sliced;
			image.color = Ink;
			Place(TextElement("Title", card, "RUCKSACK & AUSRUESTUNG", 34, TextAnchor.MiddleLeft, Cream).rectTransform, new Vector2(42f, -28f), new Vector2(500f, 58f), new Vector2(0f, 1f));
			Place(TextElement("InputHint", card, "Links wählen · rechts anlegen · belegten Slot antippen: ablegen", 16, TextAnchor.MiddleRight, new Color(0.66f, 0.76f, 0.75f)).rectTransform, new Vector2(-42f, -36f), new Vector2(590f, 42f), new Vector2(1f, 1f));
			RectTransform grid = Rect("SlotGrid", card);
			Place(grid, new Vector2(42f, -110f), new Vector2(658f, 638f), new Vector2(0f, 1f));
			GridLayoutGroup gridLayoutGroup = grid.gameObject.AddComponent<GridLayoutGroup>();
			gridLayoutGroup.cellSize = new Vector2(148f, 142f);
			gridLayoutGroup.spacing = new Vector2(16f, 16f);
			gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			gridLayoutGroup.constraintCount = 4;
			gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;
			InventorySlotView[] slots = new InventorySlotView[16];
			for (int index = 0; index < slots.Length; index++)
			{
				slots[index] = BuildSlot(grid, index);
			}
			RectTransform details = Rect("DetailsPanel", card);
			Place(details, new Vector2(730f, -110f), new Vector2(390f, 638f), new Vector2(0f, 1f));
			Image image2 = details.gameObject.AddComponent<Image>();
			image2.sprite = _uiSprite;
			image2.type = Image.Type.Sliced;
			image2.color = Panel;
			Image detailIcon = ImageElement("ItemIcon", details);
			Place(detailIcon.rectTransform, new Vector2(40f, -42f), new Vector2(150f, 150f), new Vector2(0f, 1f));
			detailIcon.preserveAspect = true;
			Text detailName = TextElement("ItemName", details, "Leerer Platz", 28, TextAnchor.MiddleLeft, Cream);
			Place(detailName.rectTransform, new Vector2(40f, -214f), new Vector2(310f, 54f), new Vector2(0f, 1f));
			Text description = TextElement("Description", details, "Wähle einen belegten Inventarplatz.", 20, TextAnchor.UpperLeft, new Color(0.75f, 0.82f, 0.79f));
			Place(description.rectTransform, new Vector2(40f, -282f), new Vector2(310f, 150f), new Vector2(0f, 1f));
			description.horizontalOverflow = HorizontalWrapMode.Wrap;
			description.verticalOverflow = VerticalWrapMode.Truncate;
			Text feedback = TextElement("Feedback", details, string.Empty, 18, TextAnchor.MiddleCenter, Gold);
			Place(feedback.rectTransform, new Vector2(40f, -450f), new Vector2(310f, 42f), new Vector2(0f, 1f));
			Button use = ButtonElement("UseButton", details, "VERWENDEN", Gold);
			Place(use.GetComponent<RectTransform>(), new Vector2(40f, 96f), new Vector2(310f, 76f), new Vector2(0f, 0f));
			Button close = ButtonElement("CloseButton", details, "SCHLIESSEN", new Color(0.2f, 0.42f, 0.45f));
			Place(close.GetComponent<RectTransform>(), new Vector2(40f, 20f), new Vector2(310f, 60f), new Vector2(0f, 0f));
			EquipmentPanelView equipmentPanel = BuildEquipmentPanel(card);
			root.GetComponent<InventoryWindow>().ConfigureReferences(panel.gameObject, open, use, close, slots, detailIcon, detailName, description, feedback, equipmentPanel);
			panel.gameObject.SetActive(value: false);
			if (PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/UI/InventoryWindow.prefab") == null)
			{
				throw new IOException("Could not save Assets/_Game/Resources/UI/InventoryWindow.prefab.");
			}
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: built inventory prefab at Assets/_Game/Resources/UI/InventoryWindow.prefab.");
	}

	private static EquipmentPanelView BuildEquipmentPanel(Transform card)
	{
		GameObject root = new GameObject("EquipmentPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(EquipmentPanelView));
		root.transform.SetParent(card, worldPositionStays: false);
		Place(root.GetComponent<RectTransform>(), new Vector2(-42f, -110f), new Vector2(520f, 638f), new Vector2(1f, 1f));
		Image component = root.GetComponent<Image>();
		component.sprite = _uiSprite;
		component.type = Image.Type.Sliced;
		component.color = Panel;
		Text text = TextElement("EquipmentTitle", root.transform, "AUSRUESTUNG", 28, TextAnchor.MiddleLeft, Cream);
		text.fontStyle = FontStyle.Bold;
		Place(text.rectTransform, new Vector2(24f, -16f), new Vector2(472f, 42f), new Vector2(0f, 1f));
		Place(TextElement("EquipmentHint", root.transform, "Inventar waehlen, dann Slot tippen", 15, TextAnchor.MiddleLeft, new Color(0.66f, 0.76f, 0.75f)).rectTransform, new Vector2(24f, -58f), new Vector2(472f, 30f), new Vector2(0f, 1f));
		RectTransform grid = Rect("EquipmentGrid", root.transform);
		Place(grid, new Vector2(24f, -104f), new Vector2(472f, 414f), new Vector2(0f, 1f));
		GridLayoutGroup gridLayoutGroup = grid.gameObject.AddComponent<GridLayoutGroup>();
		gridLayoutGroup.cellSize = new Vector2(224f, 92f);
		gridLayoutGroup.spacing = new Vector2(24f, 14f);
		gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		gridLayoutGroup.constraintCount = 2;
		gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;
		EquipmentSlot[] configuredSlots = new EquipmentSlot[8]
		{
			EquipmentSlot.Weapon1,
			EquipmentSlot.Weapon2,
			EquipmentSlot.Head,
			EquipmentSlot.Chest,
			EquipmentSlot.Hands,
			EquipmentSlot.Legs,
			EquipmentSlot.CatchDevice,
			EquipmentSlot.Battery
		};
		EquipmentSlotView[] views = new EquipmentSlotView[configuredSlots.Length];
		for (int index = 0; index < configuredSlots.Length; index++)
		{
			views[index] = BuildEquipmentSlot(grid, configuredSlots[index]);
		}
		Text feedback = TextElement("EquipmentFeedback", root.transform, string.Empty, 16, TextAnchor.MiddleCenter, Gold);
		Place(feedback.rectTransform, new Vector2(24f, 20f), new Vector2(472f, 72f), new Vector2(0f, 0f));
		feedback.horizontalOverflow = HorizontalWrapMode.Wrap;
		feedback.verticalOverflow = VerticalWrapMode.Truncate;
		EquipmentPanelView component2 = root.GetComponent<EquipmentPanelView>();
		component2.ConfigureReferences(views, feedback);
		return component2;
	}

	private static EquipmentSlotView BuildEquipmentSlot(Transform parent, EquipmentSlot slot)
	{
		GameObject root = new GameObject($"EquipmentSlot_{slot}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(EquipmentSlotView));
		root.transform.SetParent(parent, worldPositionStays: false);
		Image surface = root.GetComponent<Image>();
		surface.sprite = _uiSprite;
		surface.type = Image.Type.Sliced;
		surface.color = ((slot == EquipmentSlot.CatchDevice || slot == EquipmentSlot.Battery) ? new Color(0.13f, 0.18f, 0.25f, 1f) : Slot);
		Button button = root.GetComponent<Button>();
		button.targetGraphic = surface;
		Image icon = ImageElement("Icon", root.transform);
		Place(icon.rectTransform, new Vector2(10f, -16f), new Vector2(54f, 60f), new Vector2(0f, 1f));
		icon.preserveAspect = true;
		icon.raycastTarget = false;
		Text slotLabel = TextElement("SlotLabel", root.transform, slot.ToString(), 12, TextAnchor.MiddleLeft, Gold);
		slotLabel.fontStyle = FontStyle.Bold;
		Place(slotLabel.rectTransform, new Vector2(76f, -8f), new Vector2(136f, 20f), new Vector2(0f, 1f));
		Text itemName = TextElement("ItemName", root.transform, "Leer", 17, TextAnchor.MiddleLeft, Cream);
		Place(itemName.rectTransform, new Vector2(76f, -30f), new Vector2(136f, 28f), new Vector2(0f, 1f));
		itemName.horizontalOverflow = HorizontalWrapMode.Wrap;
		Text action = TextElement("ActionHint", root.transform, "ANTIPPEN ZUM ANLEGEN", 10, TextAnchor.MiddleLeft, new Color(0.62f, 0.73f, 0.72f));
		Place(action.rectTransform, new Vector2(76f, -61f), new Vector2(140f, 20f), new Vector2(0f, 1f));
		Image marker = ImageElement("OccupiedMarker", root.transform);
		marker.color = Gold;
		Place(marker.rectTransform, new Vector2(-5f, 0f), new Vector2(5f, 92f), new Vector2(1f, 0f));
		marker.gameObject.SetActive(value: false);
		EquipmentSlotView component = root.GetComponent<EquipmentSlotView>();
		component.ConfigureReferences(slot, button, icon, slotLabel, itemName, action, marker);
		return component;
	}

	private static InventorySlotView BuildSlot(Transform parent, int index)
	{
		GameObject root = new GameObject($"InventorySlot_{index:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InventorySlotView));
		root.transform.SetParent(parent, worldPositionStays: false);
		Image surface = root.GetComponent<Image>();
		surface.sprite = _uiSprite;
		surface.type = Image.Type.Sliced;
		surface.color = Slot;
		Button button = root.GetComponent<Button>();
		button.targetGraphic = surface;
		button.navigation = new Navigation
		{
			mode = Navigation.Mode.Automatic
		};
		Image icon = ImageElement("Icon", root.transform);
		SetAnchors(icon.rectTransform, new Vector2(0.14f, 0.16f), new Vector2(0.86f, 0.88f));
		icon.preserveAspect = true;
		icon.raycastTarget = false;
		Text fallback = TextElement("Fallback", root.transform, string.Empty, 42, TextAnchor.MiddleCenter, Cream);
		Stretch(fallback.rectTransform, 14f);
		fallback.raycastTarget = false;
		Text quantity = TextElement("Quantity", root.transform, string.Empty, 22, TextAnchor.LowerRight, Cream);
		Stretch(quantity.rectTransform, 10f);
		quantity.raycastTarget = false;
		GameObject selection = new GameObject("SelectionFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
		selection.transform.SetParent(root.transform, worldPositionStays: false);
		Stretch(selection.GetComponent<RectTransform>(), 4f);
		Image component = selection.GetComponent<Image>();
		component.sprite = _uiSprite;
		component.type = Image.Type.Sliced;
		component.color = new Color(Gold.r, Gold.g, Gold.b, 0.18f);
		component.raycastTarget = false;
		Outline component2 = selection.GetComponent<Outline>();
		component2.effectColor = Gold;
		component2.effectDistance = new Vector2(4f, -4f);
		selection.SetActive(value: false);
		InventorySlotView component3 = root.GetComponent<InventorySlotView>();
		component3.ConfigureReferences(button, icon, quantity, fallback, selection);
		return component3;
	}

	private static void ConfigureCanvas(GameObject root)
	{
		Canvas component = root.GetComponent<Canvas>();
		component.renderMode = RenderMode.ScreenSpaceOverlay;
		component.sortingOrder = 80;
		CanvasScaler component2 = root.GetComponent<CanvasScaler>();
		component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		component2.referenceResolution = new Vector2(1920f, 1080f);
		component2.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		component2.matchWidthOrHeight = 1f;
	}

	private static Button ButtonElement(string name, Transform parent, string label, Color color)
	{
		GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		root.transform.SetParent(parent, worldPositionStays: false);
		Image image = root.GetComponent<Image>();
		image.sprite = _uiSprite;
		image.type = Image.Type.Sliced;
		image.color = color;
		Button component = root.GetComponent<Button>();
		component.targetGraphic = image;
		Text text = TextElement("Label", root.transform, label, 20, TextAnchor.MiddleCenter, Ink);
		text.fontStyle = FontStyle.Bold;
		Stretch(text.rectTransform, 8f);
		return component;
	}

	private static Image ImageElement(string name, Transform parent)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		return gameObject.GetComponent<Image>();
	}

	private static Text TextElement(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		Text component = gameObject.GetComponent<Text>();
		component.font = _font;
		component.text = value;
		component.fontSize = size;
		component.alignment = alignment;
		component.color = color;
		return component;
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

	private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
	{
		rect.anchorMin = min;
		rect.anchorMax = max;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
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
