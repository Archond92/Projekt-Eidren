using Eidren.Data;
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
public static class CraftingContentBuilder
{
	public const string CraftingDataFolder = "Assets/_Game/Data/Crafting";

	public const string CatalogPath = "Assets/_Game/Resources/Data/CraftingRecipeCatalog_V01.asset";

	public const string CraftingWindowPrefabPath = "Assets/_Game/Resources/UI/CraftingWindow.prefab";

	public const string WorkbenchPrefabPath = "Assets/_Game/Prefabs/Stations/Workbench.prefab";

	private static readonly Color Ink = new Color(0.025f, 0.05f, 0.07f, 0.98f);

	private static readonly Color Panel = new Color(0.055f, 0.105f, 0.13f, 0.98f);

	private static readonly Color Slot = new Color(0.09f, 0.16f, 0.18f, 1f);

	private static readonly Color Cream = new Color(0.96f, 0.92f, 0.8f, 1f);

	private static readonly Color Gold = new Color(0.95f, 0.64f, 0.17f, 1f);

	private static Font _font;

	private static Sprite _uiSprite;

	[MenuItem("Eidren/Crafting/Build V0.1")]
	public static void BuildAll()
	{
		EnsureFolder("Assets/_Game/Data", "Crafting");
		EnsureFolder("Assets/_Game/Resources", "Data");
		EnsureFolder("Assets/_Game/Resources", "UI");
		EnsureFolder("Assets/_Game/Prefabs", "Stations");
		ItemContentAssetBuilder.BuildItemDefinitions();
		CraftingRecipeTable.BuildRecipes();
		BuildCraftingWindowPrefab();
		BuildWorkbenchPrefab();
		PlaceWorkbenchInHomeBase();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: crafting V0.1 data, UI, workbench, and HomeBase placement built.");
	}

	public static void BuildWindowForAutomation()
	{
		EnsureFolder("Assets/_Game/Resources", "UI");
		BuildCraftingWindowPrefab();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: crafting window rebuilt from the current recipe catalog.");
	}

	private static void BuildCraftingWindowPrefab()
	{
		_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		_uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
		GameObject root = new GameObject("CraftingWindow", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CraftingWindow));
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
			RectTransform panel = Rect("CraftingPanel", safe);
			Stretch(panel);
			panel.gameObject.AddComponent<Image>().color = new Color(0.005f, 0.015f, 0.02f, 0.88f);
			RectTransform card = Rect("CraftingCard", panel);
			Place(card, Vector2.zero, new Vector2(1380f, 820f), new Vector2(0.5f, 0.5f));
			Image image = card.gameObject.AddComponent<Image>();
			image.sprite = _uiSprite;
			image.type = Image.Type.Sliced;
			image.color = Ink;
			Place(TextElement("Title", card, "WERKBANK", 36, TextAnchor.MiddleLeft, Cream).rectTransform, new Vector2(44f, -26f), new Vector2(500f, 60f), new Vector2(0f, 1f));
			Place(TextElement("Hint", card, "Tippen oder Steuerkreuz: Rezept wählen · A/Enter: herstellen · B/Esc: schließen", 17, TextAnchor.MiddleRight, new Color(0.67f, 0.78f, 0.76f)).rectTransform, new Vector2(-44f, -34f), new Vector2(720f, 44f), new Vector2(1f, 1f));
			RectTransform viewport = Rect("RecipeViewport", card);
			Place(viewport, new Vector2(44f, -112f), new Vector2(500f, 640f), new Vector2(0f, 1f));
			viewport.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.05f, 0.72f);
			viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
			ScrollRect recipeScroll = viewport.gameObject.AddComponent<ScrollRect>();
			recipeScroll.horizontal = false;
			recipeScroll.vertical = true;
			recipeScroll.movementType = ScrollRect.MovementType.Clamped;
			recipeScroll.scrollSensitivity = 42f;
			RectTransform list = Rect("RecipeList", viewport);
			list.anchorMin = new Vector2(0f, 1f);
			list.anchorMax = new Vector2(1f, 1f);
			list.pivot = new Vector2(0.5f, 1f);
			list.anchoredPosition = Vector2.zero;
			list.sizeDelta = Vector2.zero;
			VerticalLayoutGroup verticalLayoutGroup = list.gameObject.AddComponent<VerticalLayoutGroup>();
			verticalLayoutGroup.spacing = 18f;
			verticalLayoutGroup.childControlHeight = false;
			verticalLayoutGroup.childControlWidth = true;
			verticalLayoutGroup.childForceExpandHeight = false;
			list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			recipeScroll.viewport = viewport;
			recipeScroll.content = list;
			CraftingRecipeCatalogDefinition recipeCatalog = AssetDatabase.LoadAssetAtPath<CraftingRecipeCatalogDefinition>("Assets/_Game/Resources/Data/CraftingRecipeCatalog_V01.asset");
			CraftingRecipeButtonView[] recipeViews = new CraftingRecipeButtonView[Mathf.Max(3, (recipeCatalog != null) ? recipeCatalog.Recipes.Count : 3)];
			for (int index = 0; index < recipeViews.Length; index++)
			{
				recipeViews[index] = BuildRecipeView(list, index);
			}
			RectTransform details = Rect("Details", card);
			Place(details, new Vector2(-44f, -112f), new Vector2(750f, 640f), new Vector2(1f, 1f));
			Image image2 = details.gameObject.AddComponent<Image>();
			image2.sprite = _uiSprite;
			image2.type = Image.Type.Sliced;
			image2.color = Panel;
			Image detailIcon = ImageElement("Icon", details);
			Place(detailIcon.rectTransform, new Vector2(34f, -32f), new Vector2(132f, 132f), new Vector2(0f, 1f));
			detailIcon.preserveAspect = true;
			Text detailName = TextElement("RecipeName", details, string.Empty, 30, TextAnchor.MiddleLeft, Cream);
			Place(detailName.rectTransform, new Vector2(184f, -36f), new Vector2(520f, 50f), new Vector2(0f, 1f));
			Text stationText = TextElement("Station", details, "Station: Werkbank", 18, TextAnchor.MiddleLeft, Gold);
			Place(stationText.rectTransform, new Vector2(184f, -92f), new Vector2(500f, 34f), new Vector2(0f, 1f));
			Text description = TextElement("Description", details, string.Empty, 20, TextAnchor.UpperLeft, new Color(0.76f, 0.83f, 0.8f));
			Place(description.rectTransform, new Vector2(34f, -182f), new Vector2(680f, 72f), new Vector2(0f, 1f));
			Place(TextElement("IngredientsTitle", details, "ZUTATEN", 20, TextAnchor.MiddleLeft, Cream).rectTransform, new Vector2(34f, -266f), new Vector2(300f, 36f), new Vector2(0f, 1f));
			CraftingIngredientView[] ingredientViews = new CraftingIngredientView[5];
			for (int i = 0; i < ingredientViews.Length; i++)
			{
				ingredientViews[i] = BuildIngredientView(details, i, -310f - (float)i * 54f);
			}
			Text resultText = TextElement("Result", details, string.Empty, 20, TextAnchor.UpperLeft, Cream);
			Place(resultText.rectTransform, new Vector2(34f, -590f), new Vector2(680f, 64f), new Vector2(0f, 1f));
			Text feedbackText = TextElement("Feedback", details, string.Empty, 19, TextAnchor.MiddleCenter, Gold);
			Place(feedbackText.rectTransform, new Vector2(34f, 92f), new Vector2(680f, 38f), new Vector2(0f, 0f));
			Button craft = ButtonElement("CraftButton", details, "HERSTELLEN", Gold);
			Place(craft.GetComponent<RectTransform>(), new Vector2(34f, 20f), new Vector2(430f, 66f), new Vector2(0f, 0f));
			Button close = ButtonElement("CloseButton", details, "SCHLIESSEN", new Color(0.2f, 0.42f, 0.45f));
			Place(close.GetComponent<RectTransform>(), new Vector2(-34f, 20f), new Vector2(220f, 66f), new Vector2(1f, 0f));
			root.GetComponent<CraftingWindow>().ConfigureReferences(panel.gameObject, recipeViews, ingredientViews, detailIcon, detailName, description, resultText, stationText, feedbackText, craft, close, recipeScroll);
			panel.gameObject.SetActive(value: false);
			if (PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/UI/CraftingWindow.prefab") == null)
			{
				throw new IOException("Could not save Assets/_Game/Resources/UI/CraftingWindow.prefab.");
			}
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static CraftingRecipeButtonView BuildRecipeView(Transform parent, int index)
	{
		GameObject root = new GameObject($"Recipe_{index + 1:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(CanvasGroup), typeof(CraftingRecipeButtonView));
		root.transform.SetParent(parent, worldPositionStays: false);
		root.GetComponent<LayoutElement>().preferredHeight = 132f;
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
		Place(icon.rectTransform, new Vector2(18f, -18f), new Vector2(92f, 92f), new Vector2(0f, 1f));
		icon.preserveAspect = true;
		icon.raycastTarget = false;
		Text label = TextElement("Name", root.transform, string.Empty, 22, TextAnchor.MiddleLeft, Cream);
		Place(label.rectTransform, new Vector2(126f, -20f), new Vector2(340f, 48f), new Vector2(0f, 1f));
		Text state = TextElement("State", root.transform, string.Empty, 16, TextAnchor.MiddleLeft, Gold);
		Place(state.rectTransform, new Vector2(126f, 22f), new Vector2(340f, 36f), new Vector2(0f, 0f));
		Image frame = ImageElement("Selection", root.transform);
		Stretch(frame.rectTransform, 4f);
		frame.sprite = _uiSprite;
		frame.type = Image.Type.Sliced;
		frame.color = new Color(Gold.r, Gold.g, Gold.b, 0.25f);
		frame.raycastTarget = false;
		root.GetComponent<CraftingRecipeButtonView>().ConfigureReferences(button, icon, label, state, frame, root.GetComponent<CanvasGroup>());
		return root.GetComponent<CraftingRecipeButtonView>();
	}

	private static CraftingIngredientView BuildIngredientView(Transform parent, int index, float y)
	{
		RectTransform root = Rect($"Ingredient_{index + 1:00}", parent);
		Place(root, new Vector2(34f, y), new Vector2(680f, 54f), new Vector2(0f, 1f));
		root.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.17f, 1f);
		Image icon = ImageElement("Icon", root);
		Place(icon.rectTransform, new Vector2(8f, -5f), new Vector2(44f, 44f), new Vector2(0f, 1f));
		icon.preserveAspect = true;
		icon.raycastTarget = false;
		Text label = TextElement("Name", root, string.Empty, 18, TextAnchor.MiddleLeft, Cream);
		Place(label.rectTransform, new Vector2(64f, -5f), new Vector2(400f, 44f), new Vector2(0f, 1f));
		Text amount = TextElement("Amount", root, string.Empty, 18, TextAnchor.MiddleRight, Cream);
		Place(amount.rectTransform, new Vector2(-16f, -5f), new Vector2(180f, 44f), new Vector2(1f, 1f));
		CraftingIngredientView craftingIngredientView = root.gameObject.AddComponent<CraftingIngredientView>();
		craftingIngredientView.ConfigureReferences(icon, label, amount);
		return craftingIngredientView;
	}

	private static void BuildWorkbenchPrefab()
	{
		if (MidpolyWorkbenchMigration.TryBuildApprovedVisual())
		{
			return;
		}
		GameObject authored = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Stations/Workbench.prefab");
		if (authored != null && authored.transform.Find("Geometry_A14") != null)
		{
			return;
		}
		GameObject root = new GameObject("Workbench", typeof(BoxCollider), typeof(WorkbenchController));
		try
		{
			BoxCollider component = root.GetComponent<BoxCollider>();
			component.isTrigger = true;
			component.center = new Vector3(0f, 0.8f, 0f);
			component.size = new Vector3(3.2f, 2.2f, 2.2f);
			Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/ITEM_TMP_Axe.png");
			root.GetComponent<WorkbenchController>().Configure("home_base.workbench", "Werkbank benutzen", icon, InteractionUtility.StandardSurfaceRange, CraftingStationType.Workbench);
			Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Resources/Materials/RES_Wood.mat");
			Material copper = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Resources/Materials/RES_Copper.mat");
			VisualCube(root.transform, "WorkSurface", new Vector3(0f, 1.05f, 0f), new Vector3(2.8f, 0.25f, 1.35f), wood);
			VisualCube(root.transform, "LegLeft", new Vector3(-1.05f, 0.48f, 0f), new Vector3(0.3f, 0.95f, 1f), wood);
			VisualCube(root.transform, "LegRight", new Vector3(1.05f, 0.48f, 0f), new Vector3(0.3f, 0.95f, 1f), wood);
			VisualCube(root.transform, "ToolRail", new Vector3(0f, 1.48f, 0.48f), new Vector3(2.3f, 0.13f, 0.13f), copper);
			// G-004: Bodenerdung als Bauteil.
			//
			// Achtung: Dieser Zweig laeuft im Bestand NICHT. Der Waechter am
			// Methodenanfang ueberspringt das Prefab, solange es die handgebaute
			// Geometry_A14-Geometrie traegt (Etappe-2-Entscheid). Das Decal
			// setzt deshalb GroundContactRebuildChain direkt am Prefab. Der
			// Aufruf hier bleibt, damit ein spaeterer Neubau die Erdung nicht
			// verliert.
			WorldContactShadowBuilder.Attach(root);
			if (PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/Stations/Workbench.prefab") == null)
			{
				throw new IOException("Could not save Assets/_Game/Prefabs/Stations/Workbench.prefab.");
			}
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static void PlaceWorkbenchInHomeBase()
	{
		Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/HomeBase.unity", OpenSceneMode.Single);
		Transform gameplayRoot = null;
		GameObject[] rootGameObjects = scene.GetRootGameObjects();
		foreach (GameObject root in rootGameObjects)
		{
			Transform candidate = root.transform.Find("GameplayRoot");
			if (candidate == null)
			{
				Transform zoneRoot = root.transform.Find("ZoneRoot");
				candidate = ((zoneRoot != null) ? zoneRoot.Find("GameplayRoot") : null);
			}
			if (candidate != null)
			{
				gameplayRoot = candidate;
				break;
			}
		}
		if (gameplayRoot == null)
		{
			GameObject zoneRoot2 = GameObject.Find("ZoneRoot");
			gameplayRoot = ((zoneRoot2 != null) ? zoneRoot2.transform.Find("GameplayRoot") : null);
		}
		if (gameplayRoot == null)
		{
			throw new InvalidOperationException("HomeBase has no ZoneRoot/GameplayRoot.");
		}
		Transform existing = gameplayRoot.Find("Workbench");
		if (existing != null)
		{
			UnityEngine.Object.DestroyImmediate(existing.gameObject);
		}
		GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Stations/Workbench.prefab"), scene);
		obj.name = "Workbench";
		obj.transform.SetParent(gameplayRoot, worldPositionStays: false);
		obj.transform.position = new Vector3(4.5f, 0f, -2.5f);
		obj.transform.rotation = Quaternion.Euler(0f, -28f, 0f);
		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
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

	private static void EnsureFolder(string parent, string child)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + child))
		{
			AssetDatabase.CreateFolder(parent, child);
		}
	}
}
}
