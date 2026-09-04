using Eidren.Interaction;
using Eidren.UI;
using System.IO;
using System;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class StorageContentBuilder
{
	public const string StorageWindowPrefabPath = "Assets/_Game/Resources/UI/StorageWindow.prefab";

	public const string StorageChestPrefabPath = "Assets/_Game/Prefabs/Stations/StorageChest.prefab";

	private static readonly Color Ink = new Color(0.025f, 0.05f, 0.07f, 0.98f);

	private static readonly Color Panel = new Color(0.055f, 0.105f, 0.13f, 0.98f);

	private static readonly Color Slot = new Color(0.09f, 0.16f, 0.18f, 1f);

	private static readonly Color Cream = new Color(0.96f, 0.92f, 0.8f, 1f);

	private static readonly Color Gold = new Color(0.95f, 0.64f, 0.17f, 1f);

	private static Font _font;

	private static Sprite _uiSprite;

	[MenuItem("Eidren/Storage/Build V0.1")]
	public static void BuildAll()
	{
		EnsureFolder("Assets/_Game/Resources", "UI");
		EnsureFolder("Assets/_Game/Prefabs", "Stations");
		BuildStorageWindowPrefab();
		BuildStorageChestPrefab();
		BuildDeathBagPrefab();
		PlaceStorageChestInHomeBase();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: storage UI, chest prefab, and HomeBase placement built.");
	}

	public static void BuildWindowForFixes()
	{
		EnsureFolder("Assets/_Game/Resources", "UI");
		BuildStorageWindowPrefab();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private static void BuildStorageWindowPrefab()
	{
		_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		_uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
		GameObject root = new GameObject("StorageWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(StorageWindow));
		try
		{
			Canvas component = root.GetComponent<Canvas>();
			component.renderMode = RenderMode.ScreenSpaceOverlay;
			component.sortingOrder = 90;
			CanvasScaler component2 = root.GetComponent<CanvasScaler>();
			component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			component2.referenceResolution = new Vector2(1920f, 1080f);
			component2.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			component2.matchWidthOrHeight = 1f;
			RectTransform safe = Rect("SafeArea", root.transform);
			Stretch(safe);
			safe.gameObject.AddComponent<SafeAreaPanel>();
			RectTransform panel = Rect("StoragePanel", safe);
			Stretch(panel);
			panel.gameObject.AddComponent<Image>().color = new Color(0.005f, 0.015f, 0.02f, 0.88f);
			RectTransform card = Rect("StorageCard", panel);
			Place(card, Vector2.zero, new Vector2(1640f, 900f), new Vector2(0.5f, 0.5f));
			Image image = card.gameObject.AddComponent<Image>();
			image.sprite = _uiSprite;
			image.type = Image.Type.Sliced;
			image.color = Ink;
			Place(TextElement("Title", card, "LAGERKISTE", 36, TextAnchor.MiddleLeft, Cream).rectTransform, new Vector2(42f, -24f), new Vector2(480f, 58f), new Vector2(0f, 1f));
			Place(TextElement("Hint", card, "Tippen/Steuerkreuz: wählen · A/Enter: übertragen · B/Esc: schließen", 17, TextAnchor.MiddleRight, new Color(0.67f, 0.78f, 0.76f)).rectTransform, new Vector2(-42f, -32f), new Vector2(770f, 42f), new Vector2(1f, 1f));
			InventorySlotView[] playerSlots = BuildGrid(BuildSection(card, "PlayerInventory", "RUCKSACK · 16", new Vector2(38f, -100f), new Vector2(500f, 744f), new Vector2(0f, 1f)), 16, "PlayerSlot", new Vector2(106f, 112f));
			InventorySlotView[] storageSlots = BuildGrid(BuildSection(card, "StorageInventory", "LAGER · 24", new Vector2(560f, -100f), new Vector2(500f, 744f), new Vector2(0f, 1f)), 24, "StorageSlot", new Vector2(106f, 102f));
			RectTransform details = BuildSection(card, "Details", "AUSWAHL", new Vector2(-38f, -100f), new Vector2(500f, 744f), new Vector2(1f, 1f));
			Image detailIcon = ImageElement("Icon", details);
			Place(detailIcon.rectTransform, new Vector2(46f, -88f), new Vector2(150f, 150f), new Vector2(0f, 1f));
			detailIcon.preserveAspect = true;
			Text detailName = TextElement("ItemName", details, "Leerer Platz", 28, TextAnchor.MiddleLeft, Cream);
			Place(detailName.rectTransform, new Vector2(46f, -258f), new Vector2(408f, 56f), new Vector2(0f, 1f));
			Text amount = TextElement("Amount", details, "Menge: 0", 22, TextAnchor.MiddleLeft, Cream);
			Place(amount.rectTransform, new Vector2(46f, -324f), new Vector2(408f, 44f), new Vector2(0f, 1f));
			Text feedback = TextElement("Feedback", details, string.Empty, 19, TextAnchor.MiddleCenter, Gold);
			Place(feedback.rectTransform, new Vector2(46f, -390f), new Vector2(408f, 86f), new Vector2(0f, 1f));
			Button transfer = ButtonElement("TransferButton", details, "ÜBERTRAGEN", Gold);
			Place(transfer.GetComponent<RectTransform>(), new Vector2(46f, 104f), new Vector2(408f, 72f), new Vector2(0f, 0f));
			Button takeAll = ButtonElement("TakeAllButton", details, "ALLES NEHMEN", new Color(0.55f, 0.72f, 0.34f));
			Place(takeAll.GetComponent<RectTransform>(), new Vector2(46f, 184f), new Vector2(408f, 72f), new Vector2(0f, 0f));
			// Beutequellen und Heimatlager schliessen einander aus; beide Sammelaktionen
			// teilen sich deshalb denselben Platz ueber dem Uebertragen-Knopf.
			Button depositMatching = ButtonElement("DepositMatchingButton", details, "GLEICHES ABLEGEN", new Color(0.42f, 0.6f, 0.72f));
			Place(depositMatching.GetComponent<RectTransform>(), new Vector2(46f, 184f), new Vector2(408f, 72f), new Vector2(0f, 0f));
			Button close = ButtonElement("CloseButton", details, "SCHLIESSEN", new Color(0.2f, 0.42f, 0.45f));
			Place(close.GetComponent<RectTransform>(), new Vector2(46f, 24f), new Vector2(408f, 62f), new Vector2(0f, 0f));
			root.GetComponent<StorageWindow>().ConfigureReferences(panel.gameObject, playerSlots, storageSlots, detailIcon, detailName, amount, feedback, transfer, takeAll, close, depositMatching);
			panel.gameObject.SetActive(value: false);
			if (PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/UI/StorageWindow.prefab") == null)
			{
				throw new IOException("Could not save Assets/_Game/Resources/UI/StorageWindow.prefab.");
			}
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static RectTransform BuildSection(Transform parent, string name, string title, Vector2 position, Vector2 size, Vector2 anchor)
	{
		RectTransform section = Rect(name, parent);
		Place(section, position, size, anchor);
		Image image = section.gameObject.AddComponent<Image>();
		image.sprite = _uiSprite;
		image.type = Image.Type.Sliced;
		image.color = Panel;
		Place(TextElement("SectionTitle", section, title, 22, TextAnchor.MiddleLeft, Gold).rectTransform, new Vector2(24f, -14f), new Vector2(430f, 48f), new Vector2(0f, 1f));
		return section;
	}

	private static InventorySlotView[] BuildGrid(RectTransform section, int count, string prefix, Vector2 cellSize)
	{
		RectTransform grid = Rect("Grid", section);
		grid.anchorMin = new Vector2(0f, 0f);
		grid.anchorMax = new Vector2(1f, 1f);
		grid.offsetMin = new Vector2(24f, 24f);
		grid.offsetMax = new Vector2(-24f, -76f);
		GridLayoutGroup gridLayoutGroup = grid.gameObject.AddComponent<GridLayoutGroup>();
		gridLayoutGroup.cellSize = cellSize;
		gridLayoutGroup.spacing = new Vector2(10f, 10f);
		gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		gridLayoutGroup.constraintCount = 4;
		gridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
		InventorySlotView[] slots = new InventorySlotView[count];
		for (int index = 0; index < count; index++)
		{
			slots[index] = BuildSlot(grid, prefix, index);
		}
		return slots;
	}

	private static InventorySlotView BuildSlot(Transform parent, string prefix, int index)
	{
		GameObject root = new GameObject($"{prefix}_{index:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InventorySlotView));
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
		SetAnchors(icon.rectTransform, new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.86f));
		icon.preserveAspect = true;
		icon.raycastTarget = false;
		Text fallback = TextElement("Fallback", root.transform, string.Empty, 34, TextAnchor.MiddleCenter, Cream);
		Stretch(fallback.rectTransform, 10f);
		Text quantity = TextElement("Quantity", root.transform, string.Empty, 18, TextAnchor.LowerRight, Cream);
		Stretch(quantity.rectTransform, 8f);
		GameObject selection = new GameObject("SelectionFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
		selection.transform.SetParent(root.transform, worldPositionStays: false);
		Stretch(selection.GetComponent<RectTransform>(), 3f);
		Image component = selection.GetComponent<Image>();
		component.sprite = _uiSprite;
		component.type = Image.Type.Sliced;
		component.color = new Color(Gold.r, Gold.g, Gold.b, 0.2f);
		component.raycastTarget = false;
		Outline component2 = selection.GetComponent<Outline>();
		component2.effectColor = Gold;
		component2.effectDistance = new Vector2(3f, -3f);
		selection.SetActive(value: false);
		InventorySlotView component3 = root.GetComponent<InventorySlotView>();
		component3.ConfigureReferences(button, icon, quantity, fallback, selection);
		return component3;
	}

	private static void BuildDeathBagPrefab()
	{
		GameObject authored = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/Prefabs/DeathBag.prefab");
		if (authored != null && MidpolyWorldLootMigration.IsApprovedDeathBag())
		{
			return;
		}
		if (authored != null && authored.transform.Find("Geometry_A14") != null)
		{
			return;
		}
		if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources/Prefabs"))
		{
			AssetDatabase.CreateFolder("Assets/_Game/Resources", "Prefabs");
		}
		GameObject root = new GameObject("DeathBag", typeof(BoxCollider), typeof(StorageContainer));
		try
		{
			BoxCollider component = root.GetComponent<BoxCollider>();
			component.isTrigger = true;
			component.center = new Vector3(0f, 0.4f, 0f);
			component.size = new Vector3(1.8f, 1.4f, 1.8f);
			root.GetComponent<StorageContainer>().Configure("death.bag", "Beutel bergen", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/ITEM_TMP_PlantFiber.png"), InteractionUtility.StandardSurfaceRange);
			Material fiber = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Resources/Materials/RES_Fiber.mat");
			VisualCube(root.transform, "Sack", new Vector3(0f, 0.32f, 0f), new Vector3(0.95f, 0.64f, 0.95f), fiber);
			VisualCube(root.transform, "Knot", new Vector3(0f, 0.72f, 0f), new Vector3(0.34f, 0.24f, 0.34f), fiber);
			PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/Prefabs/DeathBag.prefab");
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static void BuildStorageChestPrefab()
	{
		GameObject authored = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Stations/StorageChest.prefab");
		if (authored != null && authored.transform.Find("Geometry_A14") != null)
		{
			return;
		}
		GameObject root = new GameObject("StorageChest", typeof(BoxCollider), typeof(StorageContainer));
		try
		{
			BoxCollider component = root.GetComponent<BoxCollider>();
			component.isTrigger = true;
			component.center = new Vector3(0f, 0.75f, 0f);
			component.size = new Vector3(2.7f, 2f, 2.1f);
			Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/ITEM_TMP_Wood.png");
			root.GetComponent<StorageContainer>().Configure("home_base.storage.main", "Lagerkiste öffnen", icon, InteractionUtility.StandardSurfaceRange);
			Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Resources/Materials/RES_Wood.mat");
			Material copper = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Resources/Materials/RES_Copper.mat");
			VisualCube(root.transform, "ChestBody", new Vector3(0f, 0.55f, 0f), new Vector3(2.25f, 1.05f, 1.45f), wood);
			VisualCube(root.transform, "ChestLid", new Vector3(0f, 1.18f, 0f), new Vector3(2.4f, 0.28f, 1.58f), wood);
			VisualCube(root.transform, "FrontBand", new Vector3(0f, 0.75f, -0.76f), new Vector3(0.25f, 1.25f, 0.12f), copper);
			VisualCube(root.transform, "Latch", new Vector3(0f, 0.85f, -0.88f), new Vector3(0.48f, 0.38f, 0.16f), copper);
			// G-004: Bodenerdung als Bauteil. Nur hier — StorageWindow ist ein
			// UI-Fenster und DeathBag liegt ausserhalb der sieben
			// Weltobjektordner; beide bekommen keine Erdung.
			//
			// Achtung: Dieser Zweig laeuft im Bestand NICHT. Der Waechter am
			// Methodenanfang ueberspringt das Prefab, solange es die handgebaute
			// Geometry_A14-Geometrie traegt (Etappe-2-Entscheid). Das Decal
			// setzt deshalb GroundContactRebuildChain direkt am Prefab. Der
			// Aufruf hier bleibt, damit ein spaeterer Neubau die Erdung nicht
			// verliert.
			WorldContactShadowBuilder.Attach(root);
			if (PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/Stations/StorageChest.prefab") == null)
			{
				throw new IOException("Could not save Assets/_Game/Prefabs/Stations/StorageChest.prefab.");
			}
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static void PlaceStorageChestInHomeBase()
	{
		Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/HomeBase.unity", OpenSceneMode.Single);
		Transform gameplayRoot = FindGameplayRoot(scene);
		if (gameplayRoot == null)
		{
			throw new InvalidOperationException("HomeBase has no ZoneRoot/GameplayRoot.");
		}
		Transform existing = gameplayRoot.Find("StorageChest");
		if (existing != null)
		{
			UnityEngine.Object.DestroyImmediate(existing.gameObject);
		}
		GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Stations/StorageChest.prefab"), scene);
		obj.name = "StorageChest";
		obj.transform.SetParent(gameplayRoot, worldPositionStays: false);
		obj.transform.position = new Vector3(-4.5f, 0f, -2.5f);
		obj.transform.rotation = Quaternion.Euler(0f, 28f, 0f);
		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
	}

	private static Transform FindGameplayRoot(Scene scene)
	{
		GameObject[] rootGameObjects = scene.GetRootGameObjects();
		foreach (GameObject root in rootGameObjects)
		{
			if (root.name == "ZoneRoot")
			{
				return root.transform.Find("GameplayRoot");
			}
			Transform zoneRoot = root.transform.Find("ZoneRoot");
			if (zoneRoot != null)
			{
				return zoneRoot.Find("GameplayRoot");
			}
		}
		return null;
	}

	private static void VisualCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
	{
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		gameObject.name = name;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.localPosition = localPosition;
		gameObject.transform.localScale = localScale;
		UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
		gameObject.GetComponent<Renderer>().sharedMaterial = material;
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
		component.horizontalOverflow = HorizontalWrapMode.Wrap;
		component.verticalOverflow = VerticalWrapMode.Truncate;
		component.raycastTarget = false;
		return component;
	}

	private static RectTransform Rect(string name, Transform parent)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		return gameObject.GetComponent<RectTransform>();
	}

	private static void Place(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
	{
		rect.anchorMin = anchor;
		rect.anchorMax = anchor;
		rect.pivot = anchor;
		rect.anchoredPosition = anchoredPosition;
		rect.sizeDelta = size;
	}

	private static void Stretch(RectTransform rect, float inset = 0f)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = new Vector2(inset, inset);
		rect.offsetMax = new Vector2(0f - inset, 0f - inset);
	}

	private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
	{
		rect.anchorMin = min;
		rect.anchorMax = max;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
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
