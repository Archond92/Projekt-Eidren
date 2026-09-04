using Eidren.UI;
using System.IO;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
public static class BuildingMenuUiBuilder
{
	private const string PrefabPath = "Assets/_Game/Resources/UI/BuildingMenuWindow.prefab";

	private const float HeaderHeight = 30f;

	private static readonly Color Bone = new Color(0.9f, 0.88f, 0.8f);

	private static readonly Color Amber = new Color(0.95f, 0.76f, 0.35f);

	private static readonly Color Slate = new Color(0.16f, 0.19f, 0.24f, 0.92f);

	[MenuItem("Eidren/UI/Upgrade Building Menu")]
	public static void UpgradeForAutomation()
	{
		GameObject root = PrefabUtility.LoadPrefabContents("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab");
		try
		{
			Font font = AnyFont(root);
			BuildingMenuItemView[] componentsInChildren = root.GetComponentsInChildren<BuildingMenuItemView>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Upgrade(componentsInChildren[i], font);
			}
			UpgradeHorizontalLayout(root);
			UpgradeCatalogLayout(root);
			BuildingPlacementBar bar = EnsureBar(EnsureSafeArea(root), font);
			WireWindow(root, bar, EnsureMoveButton(root, font));
			PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/UI/BuildingMenuWindow.prefab");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Verify();
		Debug.Log("Eidren: building catalog authored — categories, cost with stock, placement bar and safe area.");
	}

	public static void BuildBuildingMenuContent()
	{
		CraftingContentBuilder.BuildWindowForAutomation();
		BuildingCostContentBuilder.BuildBuildingCosts();
		UpgradeForAutomation();
	}

	private static void Upgrade(BuildingMenuItemView view, Font font)
	{
		SerializedObject serializedObject = new SerializedObject(view);
		Button button = serializedObject.FindProperty("button").objectReferenceValue as Button;
		Text label = serializedObject.FindProperty("label").objectReferenceValue as Text;
		Image marker = serializedObject.FindProperty("stateMarker").objectReferenceValue as Image;
		if (button == null || label == null)
		{
			throw new InvalidDataException("Building row '" + view.name + "' is missing authored refs.");
		}
		Image icon = EnsureIcon(view);
		// F31-013: Karte als vertikaler Stapel — Kopfstreifen, Icon, Name,
		// Kosten. Der alte Zuschnitt gab dem Namen ~77 px neben der
		// Kostenspalte; lange Namen brachen mitten im Wort um.
		RectTransform labelRect = label.rectTransform;
		labelRect.anchorMin = new Vector2(0f, 1f);
		labelRect.anchorMax = new Vector2(1f, 1f);
		labelRect.pivot = new Vector2(0.5f, 1f);
		labelRect.anchoredPosition = new Vector2(0f, -128f);
		labelRect.sizeDelta = new Vector2(-20f, 52f);
		label.alignment = TextAnchor.MiddleCenter;
		label.fontSize = 20;
		label.horizontalOverflow = HorizontalWrapMode.Wrap;
		label.verticalOverflow = VerticalWrapMode.Truncate;
		label.raycastTarget = false;
		if (marker != null)
		{
			RectTransform markerRect = marker.rectTransform;
			markerRect.anchorMin = new Vector2(0f, 0f);
			markerRect.anchorMax = new Vector2(1f, 0f);
			markerRect.pivot = new Vector2(0.5f, 0f);
			markerRect.anchoredPosition = new Vector2(0f, 2f);
			markerRect.sizeDelta = new Vector2(-16f, 4f);
			marker.raycastTarget = false;
		}
		Text cost = EnsureCost(view, font);
		Text category = EnsureHeader(view, font);
		button.navigation = new Navigation
		{
			mode = Navigation.Mode.Automatic
		};
		view.ConfigureReferences(button, icon, label, marker, cost, category.transform.parent.gameObject, category);
		EditorUtility.SetDirty(view);
	}

	/// <summary>
	/// Ordnet Katalog, Details und Aktionen als zusammenhängenden Baukatalog an
	/// (F-014). Der Katalog wird zum flachen Streifen — der bisherige hohe Block
	/// hinterliess bei einer 92 px hohen Kartenreihe eine grosse leere Flaeche.
	/// Die Aktionen stehen als Spalte rechts, gewichtet von PLATZIEREN (primaer)
	/// bis SCHLIESSEN (sekundaer).
	/// </summary>
	private static void UpgradeCatalogLayout(GameObject root)
	{
		Place(root, "BuildingList", new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.88f));
		Place(root, "Details", new Vector2(0.04f, 0.20f), new Vector2(0.60f, 0.54f));
		Place(root, "Feedback", new Vector2(0.04f, 0.08f), new Vector2(0.60f, 0.18f));
		Place(root, "PlaceButton", new Vector2(0.64f, 0.40f), new Vector2(0.96f, 0.54f));
		Place(root, "MoveButton", new Vector2(0.64f, 0.28f), new Vector2(0.96f, 0.38f));
		Place(root, "DemolishButton", new Vector2(0.64f, 0.16f), new Vector2(0.96f, 0.26f));
		Place(root, "CloseButton", new Vector2(0.64f, 0.06f), new Vector2(0.96f, 0.14f));
		Label(root, "PlaceButton", "PLATZIEREN");
		Label(root, "MoveButton", "VERSCHIEBEN");
		Label(root, "DemolishButton", "ABREISSEN");
		Label(root, "CloseButton", "SCHLIESSEN");
	}

	private static void Place(GameObject root, string childName, Vector2 anchorMin, Vector2 anchorMax)
	{
		Transform found = FindDeep(root.transform, childName);
		if (found == null)
		{
			throw new InvalidDataException("Building menu is missing '" + childName + "'.");
		}
		RectTransform rect = (RectTransform)found;
		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		rect.anchoredPosition = Vector2.zero;
		rect.sizeDelta = Vector2.zero;
	}

	private static void Label(GameObject root, string childName, string text)
	{
		Transform found = FindDeep(root.transform, childName);
		Text label = ((found != null) ? found.GetComponentInChildren<Text>(includeInactive: true) : null);
		if (label == null)
		{
			throw new InvalidDataException("Building menu button '" + childName + "' has no label.");
		}
		label.text = text;
	}

	private static Transform FindDeep(Transform root, string childName)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
		{
			if (child.name == childName)
			{
				return child;
			}
		}
		return null;
	}

	private static void UpgradeHorizontalLayout(GameObject root)
	{
		BuildingMenuItemView[] componentsInChildren = root.GetComponentsInChildren<BuildingMenuItemView>(includeInactive: true);
		if (componentsInChildren.Length == 0)
		{
			throw new InvalidDataException("Building rows are missing.");
		}
		GameObject listPanel = componentsInChildren[0].GetComponentInParent<ScrollRect>(includeInactive: true) is ScrollRect vorhandener
			? vorhandener.gameObject
			: componentsInChildren[0].transform.parent.gameObject;
		// F31-012: Die Kartenzeile (11 x 200 px) lief ueber den Bildschirmrand,
		// weil der ContentSizeFitter direkt auf dem verankerten Panel sass.
		// Neu: Panel -> maskierter Viewport -> scrollbarer Content.
		foreach (var alt1 in listPanel.GetComponents<VerticalLayoutGroup>()) Object.DestroyImmediate(alt1);
		foreach (var alt2 in listPanel.GetComponents<HorizontalLayoutGroup>()) Object.DestroyImmediate(alt2);
		foreach (var alt3 in listPanel.GetComponents<ContentSizeFitter>()) Object.DestroyImmediate(alt3);
		RectTransform viewport = EnsureRect(listPanel.transform, "CatalogViewport");
		Stretch(viewport);
		if (viewport.GetComponent<RectMask2D>() == null)
		{
			viewport.gameObject.AddComponent<RectMask2D>();
		}
		RectTransform contentRect = EnsureRect(viewport, "CatalogContent");
		contentRect.anchorMin = new Vector2(0f, 0f);
		contentRect.anchorMax = new Vector2(0f, 1f);
		contentRect.pivot = new Vector2(0f, 0.5f);
		contentRect.anchoredPosition = Vector2.zero;
		contentRect.sizeDelta = new Vector2(0f, 0f);
		GameObject content = contentRect.gameObject;
		foreach (BuildingMenuItemView view in componentsInChildren)
		{
			view.transform.SetParent(contentRect, worldPositionStays: false);
		}
		HorizontalLayoutGroup horizontal = content.GetComponent<HorizontalLayoutGroup>();
		if (horizontal == null)
		{
			horizontal = content.AddComponent<HorizontalLayoutGroup>();
		}
		horizontal.padding = new RectOffset(12, 12, 8, 8);
		horizontal.spacing = 12f;
		horizontal.childAlignment = TextAnchor.MiddleLeft;
		// W-007: childControlWidth MUSS an sein, sonst ignoriert die LayoutGroup
		// die preferredWidth der Eintraege und alle Karten kollabieren auf 0 px.
		horizontal.childControlWidth = true;
		horizontal.childControlHeight = true;
		horizontal.childForceExpandWidth = false;
		horizontal.childForceExpandHeight = true;
		BuildingMenuItemView[] array = componentsInChildren;
		foreach (BuildingMenuItemView row in array)
		{
			LayoutElement layout = row.GetComponent<LayoutElement>();
			if (layout == null)
			{
				layout = row.gameObject.AddComponent<LayoutElement>();
			}
			layout.preferredWidth = 200f;
			layout.preferredHeight = 240f;
		}
		ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
		if (fitter == null)
		{
			fitter = content.AddComponent<ContentSizeFitter>();
		}
		fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
		ScrollRect scroll = listPanel.GetComponent<ScrollRect>();
		if (scroll == null)
		{
			scroll = listPanel.AddComponent<ScrollRect>();
		}
		scroll.content = contentRect;
		scroll.viewport = viewport;
		scroll.horizontal = true;
		scroll.vertical = false;
		scroll.movementType = ScrollRect.MovementType.Clamped;
		scroll.inertia = true;
		scroll.scrollSensitivity = 30f;
	}

	private static Button EnsureMoveButton(GameObject root, Font font)
	{
		SerializedObject serialized = new SerializedObject(root.GetComponent<BuildingMenuWindow>());
		Button existing = serialized.FindProperty("moveButton").objectReferenceValue as Button;
		if (existing != null)
		{
			return existing;
		}
		Button demolish = serialized.FindProperty("demolishButton").objectReferenceValue as Button;
		if (demolish == null)
		{
			throw new InvalidDataException("Demolish button is missing.");
		}
		GameObject gameObject = Object.Instantiate(demolish.gameObject, demolish.transform.parent);
		gameObject.name = "MoveButton";
		Button button = gameObject.GetComponent<Button>();
		button.navigation = new Navigation
		{
			mode = Navigation.Mode.Automatic
		};
		Text label = gameObject.GetComponentInChildren<Text>(includeInactive: true);
		if (label != null)
		{
			label.font = font;
			label.text = "VERSCHIEBEN";
		}
		return button;
	}

	private static Image EnsureIcon(BuildingMenuItemView view)
	{
		Image image = Ensure<Image>(view.transform, "BuildingIcon");
		RectTransform rectTransform = image.rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 1f);
		rectTransform.anchorMax = new Vector2(0.5f, 1f);
		rectTransform.pivot = new Vector2(0.5f, 1f);
		rectTransform.anchoredPosition = new Vector2(0f, -32f);
		rectTransform.sizeDelta = new Vector2(88f, 88f);
		image.preserveAspect = true;
		image.raycastTarget = false;
		return image;
	}

	private static Text EnsureCost(BuildingMenuItemView view, Font font)
	{
		Text text = Ensure<Text>(view.transform, "CostLabel");
		RectTransform rectTransform = text.rectTransform;
		rectTransform.anchorMin = new Vector2(0f, 0f);
		rectTransform.anchorMax = new Vector2(1f, 0f);
		rectTransform.pivot = new Vector2(0.5f, 0f);
		rectTransform.anchoredPosition = new Vector2(0f, 10f);
		rectTransform.sizeDelta = new Vector2(-16f, 58f);
		text.font = font;
		text.fontSize = 16;
		text.alignment = TextAnchor.UpperCenter;
		text.horizontalOverflow = HorizontalWrapMode.Wrap;
		text.verticalOverflow = VerticalWrapMode.Truncate;
		text.raycastTarget = false;
		return text;
	}

	private static Text EnsureHeader(BuildingMenuItemView view, Font font)
	{
		RectTransform header = EnsureRect(view.transform, "CategoryHeader");
		header.anchorMin = new Vector2(0f, 1f);
		header.anchorMax = new Vector2(1f, 1f);
		header.pivot = new Vector2(0.5f, 1f);
		header.anchoredPosition = Vector2.zero;
		header.sizeDelta = new Vector2(0f, 26f);
		Text text = Ensure<Text>(header, "CategoryLabel");
		RectTransform rectTransform = text.rectTransform;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = new Vector2(12f, 0f);
		rectTransform.offsetMax = new Vector2(-12f, 0f);
		text.font = font;
		text.fontSize = 18;
		text.fontStyle = FontStyle.Bold;
		text.color = Amber;
		text.alignment = TextAnchor.MiddleCenter;
		text.raycastTarget = false;
		header.gameObject.SetActive(value: false);
		return text;
	}

	private static RectTransform EnsureSafeArea(GameObject root)
	{
		RectTransform safeArea = EnsureRect(root.transform, "SafeArea");
		Stretch(safeArea);
		if (safeArea.GetComponent<SafeAreaPanel>() == null)
		{
			safeArea.gameObject.AddComponent<SafeAreaPanel>();
		}
		safeArea.SetAsFirstSibling();
		for (int index = root.transform.childCount - 1; index >= 0; index--)
		{
			Transform child = root.transform.GetChild(index);
			if (child != safeArea)
			{
				child.SetParent(safeArea, worldPositionStays: false);
			}
		}
		return safeArea;
	}

	private static BuildingPlacementBar EnsureBar(RectTransform safeArea, Font font)
	{
		RectTransform bar = EnsureRect(safeArea, "PlacementBar");
		bar.anchorMin = new Vector2(0.5f, 0f);
		bar.anchorMax = new Vector2(0.5f, 0f);
		bar.pivot = new Vector2(0.5f, 0f);
		bar.anchoredPosition = new Vector2(0f, 48f);
		bar.sizeDelta = new Vector2(760f, 176f);
		Button cancel = BarButton(bar, "CancelButton", "ABBRECHEN", font, -252f, 200f);
		Button confirm = BarButton(bar, "ConfirmButton", "BAUEN", font, 0f, 248f);
		Button rotate = BarButton(bar, "RotateButton", "DREHEN", font, 252f, 200f);
		GameObject keyboard = Hints(bar, "KeyboardHints", "Enter bauen  ·  Esc abbrechen  ·  Q drehen", font);
		GameObject gamepad = Hints(bar, "GamepadHints", "A bauen  ·  B abbrechen  ·  X drehen", font);
		BuildingPlacementBar component = bar.GetComponent<BuildingPlacementBar>();
		if (component == null)
		{
			component = bar.gameObject.AddComponent<BuildingPlacementBar>();
		}
		component.ConfigureReferences(bar.gameObject, confirm, cancel, rotate, keyboard, gamepad);
		EditorUtility.SetDirty(component);
		bar.gameObject.SetActive(value: false);
		return component;
	}

	private static Button BarButton(RectTransform parent, string objectName, string caption, Font font, float offsetX, float width)
	{
		Image background = Ensure<Image>(parent, objectName);
		RectTransform rectTransform = background.rectTransform;
		rectTransform.anchorMin = new Vector2(0.5f, 1f);
		rectTransform.anchorMax = new Vector2(0.5f, 1f);
		rectTransform.pivot = new Vector2(0.5f, 1f);
		rectTransform.anchoredPosition = new Vector2(offsetX, 0f);
		rectTransform.sizeDelta = new Vector2(width, 112f);
		background.color = Slate;
		Text text = Ensure<Text>(rectTransform, "Label");
		Stretch(text.rectTransform);
		text.font = font;
		text.fontSize = 26;
		text.fontStyle = FontStyle.Bold;
		text.color = Bone;
		text.alignment = TextAnchor.MiddleCenter;
		text.raycastTarget = false;
		text.text = caption;
		Button button = background.GetComponent<Button>();
		if (button == null)
		{
			button = background.gameObject.AddComponent<Button>();
		}
		button.targetGraphic = background;
		button.navigation = new Navigation
		{
			mode = Navigation.Mode.Automatic
		};
		return button;
	}

	private static GameObject Hints(RectTransform parent, string objectName, string caption, Font font)
	{
		Text text = Ensure<Text>(parent, objectName);
		RectTransform rectTransform = text.rectTransform;
		rectTransform.anchorMin = new Vector2(0f, 0f);
		rectTransform.anchorMax = new Vector2(1f, 0f);
		rectTransform.pivot = new Vector2(0.5f, 0f);
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.sizeDelta = new Vector2(0f, 44f);
		text.font = font;
		text.fontSize = 20;
		text.color = Bone;
		text.alignment = TextAnchor.MiddleCenter;
		text.raycastTarget = false;
		text.text = caption;
		text.gameObject.SetActive(value: false);
		return text.gameObject;
	}

	private static void WireWindow(GameObject root, BuildingPlacementBar bar, Button moveButton)
	{
		BuildingMenuWindow component = root.GetComponent<BuildingMenuWindow>();
		if (component == null)
		{
			throw new InvalidDataException("BuildingMenuWindow component is missing on the prefab.");
		}
		SerializedObject serializedObject = new SerializedObject(component);
		serializedObject.FindProperty("placementBar").objectReferenceValue = bar;
		serializedObject.FindProperty("moveButton").objectReferenceValue = moveButton;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(component);
	}

	private static T Ensure<T>(Transform parent, string objectName) where T : Component
	{
		RectTransform rect = EnsureRect(parent, objectName);
		T component = rect.GetComponent<T>();
		if (component == null)
		{
			component = rect.gameObject.AddComponent<T>();
		}
		return component;
	}

	private static RectTransform EnsureRect(Transform parent, string objectName)
	{
		Transform existing = parent.Find(objectName);
		if (existing != null)
		{
			return (existing as RectTransform) ?? throw new InvalidDataException("'" + objectName + "' exists but is no UI element.");
		}
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		return (RectTransform)gameObject.transform;
	}

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	private static Font AnyFont(GameObject root)
	{
		Text[] componentsInChildren = root.GetComponentsInChildren<Text>(includeInactive: true);
		foreach (Text text in componentsInChildren)
		{
			if (text.font != null)
			{
				return text.font;
			}
		}
		throw new InvalidDataException("The building menu prefab has no authored font to reuse.");
	}

	private static void Verify()
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab");
		if (gameObject == null)
		{
			throw new InvalidDataException("Building menu was not saved.");
		}
		if (gameObject.GetComponentInChildren<SafeAreaPanel>(includeInactive: true) == null)
		{
			throw new InvalidDataException("Safe area is missing on disk.");
		}
		BuildingMenuWindow window = gameObject.GetComponent<BuildingMenuWindow>();
		if (window == null || window.PlacementBar == null)
		{
			throw new InvalidDataException("The placement bar is not wired on disk.");
		}
		BuildingMenuItemView[] componentsInChildren = gameObject.GetComponentsInChildren<BuildingMenuItemView>(includeInactive: true);
		foreach (BuildingMenuItemView view in componentsInChildren)
		{
			if (view.CostLabel == null || view.CategoryHeader == null)
			{
				throw new InvalidDataException("Row '" + view.name + "' has no cost label or category header on disk.");
			}
		}
	}
}
}
