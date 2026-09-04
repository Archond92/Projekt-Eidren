using Eidren.Composition;
using Eidren.Data;
using Eidren.UI;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
internal static class WorldMapPrefabBuilder
{
	internal const string PrefabFolder = "Assets/_Game/UI/WorldMap/Prefabs";

	internal const string ScenePath = "Assets/_Game/Scenes/WorldMap/WorldMap.unity";

	private static string Prefab(string name)
	{
		return "Assets/_Game/UI/WorldMap/Prefabs/" + name + ".prefab";
	}

	internal static void Build(WorldMapDefinition map, IReadOnlyList<WorldMapNodeDefinition> nodes)
	{
		GameObject dangerPrefab = BuildDangerIndicator();
		GameObject resource = BuildResourceEntry();
		GameObject statusEntry = BuildStatusEntry();
		GameObject node = BuildNodeView(dangerPrefab);
		GameObject connection = BuildConnectionView();
		GameObject travel = BuildTravelButton();
		GameObject info = BuildInfoPanel(dangerPrefab, resource, travel);
		GameObject status = BuildTopStatusBar(statusEntry);
		GameObject quick = BuildQuickActions();
		GameObject side = BuildSideMenu();
		GameObject transition = BuildTransitionOverlay();
		BuildScene(BuildCanvas(map, nodes, node, connection, info, status, quick, side, transition));
	}

	private static GameObject BuildDangerIndicator()
	{
		GameObject gameObject = RectObject("WorldMapDangerIndicator", typeof(WorldMapDangerIndicator));
		SetSize(gameObject, new Vector2(190f, 34f));
		Text label = TextChild(gameObject.transform, "DangerLabel", "GEFAHR 1/5", 13, TextAnchor.MiddleLeft);
		SetAnchors(label.rectTransform, Vector2.zero, new Vector2(0.52f, 1f));
		Text symbols = TextChild(gameObject.transform, "DangerSymbols", "◆◇◇◇◇", 13, TextAnchor.MiddleRight);
		SetAnchors(symbols.rectTransform, new Vector2(0.5f, 0f), Vector2.one);
		gameObject.GetComponent<WorldMapDangerIndicator>().ConfigureUi(label, symbols);
		return SavePrefab(gameObject, "WorldMapDangerIndicator");
	}

	private static GameObject BuildResourceEntry()
	{
		GameObject gameObject = RectObject("WorldMapResourceEntry", typeof(WorldMapResourceEntry));
		SetSize(gameObject, new Vector2(290f, 42f));
		Image icon = ImageChild(gameObject.transform, "ResourceIcon");
		SetAnchors(icon.rectTransform, new Vector2(0f, 0.08f), new Vector2(0.14f, 0.92f));
		icon.preserveAspect = true;
		Text label = TextChild(gameObject.transform, "ResourceName", "Ressource", 15, TextAnchor.MiddleLeft);
		SetAnchors(label.rectTransform, new Vector2(0.17f, 0f), Vector2.one);
		gameObject.GetComponent<WorldMapResourceEntry>().ConfigureUi(icon, label);
		return SavePrefab(gameObject, "WorldMapResourceEntry");
	}

	private static GameObject BuildStatusEntry()
	{
		GameObject gameObject = RectObject("WorldMapStatusEntry", typeof(Image), typeof(WorldMapStatusEntry));
		SetSize(gameObject, new Vector2(220f, 72f));
		Image surface = gameObject.GetComponent<Image>();
		Text label = TextChild(gameObject.transform, "StatusLabel", "STATUS", 12, TextAnchor.MiddleCenter);
		SetAnchors(label.rectTransform, new Vector2(0f, 0.52f), Vector2.one);
		Text value = TextChild(gameObject.transform, "StatusValue", "—", 17, TextAnchor.MiddleCenter);
		SetAnchors(value.rectTransform, Vector2.zero, new Vector2(1f, 0.58f));
		gameObject.GetComponent<WorldMapStatusEntry>().ConfigureUi(surface, label, value);
		return SavePrefab(gameObject, "WorldMapStatusEntry");
	}

	private static GameObject BuildNodeView(GameObject dangerPrefab)
	{
		GameObject root = RectObject("WorldMapNodeView", typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(WorldMapNodeView));
		SetSize(root, new Vector2(220f, 160f));
		Image frame = root.GetComponent<Image>();
		frame.sprite = WorldMapContentBuilder.FrameSprite("Node");
		frame.type = Image.Type.Sliced;
		Button button = root.GetComponent<Button>();
		button.targetGraphic = frame;
		CanvasGroup group = root.GetComponent<CanvasGroup>();
		RectTransform motion = RectChild(root.transform, "AnimatedContent");
		Stretch(motion);
		Image accent = ImageChild(motion, "RegionAccent");
		SetAnchors(accent.rectTransform, new Vector2(0.12f, 0.11f), new Vector2(0.88f, 0.15f));
		Image icon = ImageChild(motion, "NodeIcon");
		SetAnchors(icon.rectTransform, new Vector2(0.32f, 0.38f), new Vector2(0.68f, 0.86f));
		icon.preserveAspect = true;
		Text name = TextChild(motion, "RegionName", "Region", 20, TextAnchor.MiddleCenter);
		SetAnchors(name.rectTransform, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.39f));
		GameObject danger = Instantiate(dangerPrefab, motion, "DangerIndicator");
		SetAnchors(danger.GetComponent<RectTransform>(), new Vector2(0.08f, 0.01f), new Vector2(0.92f, 0.2f));
		Image selected = Marker(motion, "SelectedOverlay", WorldMapContentBuilder.MarkerSprite("Selected"));
		Stretch(selected.rectTransform);
		Image visited = Marker(motion, "VisitedOverlay", WorldMapContentBuilder.MarkerSprite("Visited"));
		SetAnchors(visited.rectTransform, new Vector2(0.05f, 0.7f), new Vector2(0.22f, 0.93f));
		Image unvisited = Marker(motion, "NewOverlay", WorldMapContentBuilder.MarkerSprite("New"));
		SetAnchors(unvisited.rectTransform, new Vector2(0.05f, 0.7f), new Vector2(0.22f, 0.93f));
		Image locked = Marker(motion, "LockedOverlay", WorldMapContentBuilder.MarkerSprite("Locked"));
		SetAnchors(locked.rectTransform, new Vector2(0.37f, 0.4f), new Vector2(0.63f, 0.75f));
		Image eventMarker = Marker(motion, "EventOverlay", WorldMapContentBuilder.MarkerSprite("Event"));
		SetAnchors(eventMarker.rectTransform, new Vector2(0.78f, 0.69f), new Vector2(0.95f, 0.92f));
		Image boss = Marker(motion, "BossOverlay", WorldMapContentBuilder.MarkerSprite("Boss"));
		SetAnchors(boss.rectTransform, new Vector2(0.78f, 0.13f), new Vector2(0.96f, 0.38f));
		Image notification = Marker(motion, "NotificationOverlay", WorldMapContentBuilder.MarkerSprite("Notification"));
		SetAnchors(notification.rectTransform, new Vector2(0.86f, 0.82f), new Vector2(0.98f, 0.98f));
		Image[] array = new Image[7] { selected, visited, unvisited, locked, eventMarker, boss, notification };
		foreach (Image obj in array)
		{
			obj.raycastTarget = false;
			obj.gameObject.SetActive(value: false);
		}
		root.GetComponent<WorldMapNodeView>().ConfigureUi(button, group, motion, frame, accent, icon, name, danger.GetComponent<WorldMapDangerIndicator>(), selected.gameObject, visited.gameObject, unvisited.gameObject, locked.gameObject, eventMarker, boss.gameObject, notification.gameObject);
		return SavePrefab(root, "WorldMapNodeView");
	}

	private static GameObject BuildConnectionView()
	{
		GameObject gameObject = RectObject("WorldMapConnectionView", typeof(WorldMapConnectionView));
		Stretch(gameObject.GetComponent<RectTransform>());
		Image line = ImageChild(gameObject.transform, "EnergyPath");
		line.raycastTarget = false;
		gameObject.GetComponent<WorldMapConnectionView>().ConfigureUi(line);
		return SavePrefab(gameObject, "WorldMapConnectionView");
	}

	private static GameObject BuildTravelButton()
	{
		GameObject root = RectObject("WorldMapTravelButton", typeof(Image), typeof(Button), typeof(WorldMapTravelButton));
		SetSize(root, new Vector2(260f, 64f));
		Image surface = root.GetComponent<Image>();
		surface.sprite = WorldMapContentBuilder.FrameSprite("Panel");
		surface.type = Image.Type.Sliced;
		Button button = root.GetComponent<Button>();
		button.targetGraphic = surface;
		Text label = TextChild(root.transform, "ButtonLabel", "REISEN", 17, TextAnchor.MiddleCenter);
		Stretch(label.rectTransform);
		root.GetComponent<WorldMapTravelButton>().ConfigureUi(button, surface, label, root.GetComponent<RectTransform>());
		return SavePrefab(root, "WorldMapTravelButton");
	}

	private static GameObject BuildInfoPanel(GameObject dangerPrefab, GameObject resourcePrefab, GameObject travelPrefab)
	{
		GameObject root = RectObject("WorldMapInfoPanel", typeof(Image), typeof(CanvasGroup), typeof(WorldMapInfoPanel));
		SetSize(root, new Vector2(430f, 810f));
		Image surface = root.GetComponent<Image>();
		surface.sprite = WorldMapContentBuilder.FrameSprite("Panel");
		surface.type = Image.Type.Sliced;
		CanvasGroup group = root.GetComponent<CanvasGroup>();
		Text title = PanelText(root.transform, "RegionHeading", 0.88f, 0.98f, 27, TextAnchor.MiddleLeft);
		Text description = PanelText(root.transform, "RegionDescription", 0.74f, 0.88f, 16, TextAnchor.UpperLeft);
		GameObject danger = Instantiate(dangerPrefab, root.transform, "DangerIndicator");
		SetAnchors(danger.GetComponent<RectTransform>(), new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.74f));
		Text progress = PanelText(root.transform, "RecommendedProgress", 0.62f, 0.68f, 13, TextAnchor.MiddleLeft);
		Text resourcesHeading = PanelText(root.transform, "ResourcesHeading", 0.56f, 0.62f, 13, TextAnchor.MiddleLeft);
		resourcesHeading.text = "RESSOURCENVORSCHAU";
		WorldMapResourceEntry[] entries = new WorldMapResourceEntry[4];
		for (int index = 0; index < entries.Length; index++)
		{
			GameObject entry = Instantiate(resourcePrefab, root.transform, $"ResourceEntry_{index + 1}");
			float top = 0.56f - (float)index * 0.055f;
			SetAnchors(entry.GetComponent<RectTransform>(), new Vector2(0.05f, top - 0.05f), new Vector2(0.95f, top));
			entries[index] = entry.GetComponent<WorldMapResourceEntry>();
		}
		Text enemies = PanelText(root.transform, "EnemyPreview", 0.22f, 0.33f, 15, TextAnchor.UpperLeft);
		Text places = PanelText(root.transform, "SpecialPlaces", 0.11f, 0.22f, 15, TextAnchor.UpperLeft);
		Text boss = PanelText(root.transform, "BossStatus", 0.065f, 0.11f, 13, TextAnchor.MiddleLeft);
		SetAnchors(Instantiate(travelPrefab, root.transform, "TravelAction").GetComponent<RectTransform>(), new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.065f));
		root.GetComponent<WorldMapInfoPanel>().ConfigureUi(group, root.GetComponent<RectTransform>(), surface, title, description, danger.GetComponent<WorldMapDangerIndicator>(), progress, resourcesHeading, enemies, places, boss, entries);
		return SavePrefab(root, "WorldMapInfoPanel");
	}

	private static GameObject BuildTopStatusBar(GameObject statusEntryPrefab)
	{
		GameObject root = RectObject("WorldMapTopStatusBar", typeof(Image), typeof(WorldMapTopStatusBar));
		SetSize(root, new Vector2(1880f, 78f));
		Image surface = root.GetComponent<Image>();
		WorldMapStatusEntry[] entries = new WorldMapStatusEntry[7];
		for (int index = 0; index < entries.Length; index++)
		{
			GameObject entry = Instantiate(statusEntryPrefab, root.transform, $"StatusEntry_{index + 1}");
			float start = (float)index / 7f;
			float end = ((float)index + 1f) / 7f;
			SetAnchors(entry.GetComponent<RectTransform>(), new Vector2(start + 0.002f, 0.08f), new Vector2(end - 0.002f, 0.92f));
			entries[index] = entry.GetComponent<WorldMapStatusEntry>();
		}
		root.GetComponent<WorldMapTopStatusBar>().ConfigureUi(surface, entries);
		return SavePrefab(root, "WorldMapTopStatusBar");
	}

	private static GameObject BuildQuickActions()
	{
		GameObject root = RectObject("WorldMapQuickActions", typeof(Image), typeof(WorldMapQuickActions));
		SetSize(root, new Vector2(1100f, 60f));
		Image surface = root.GetComponent<Image>();
		string[] labels = new string[4] { "FILTER · SPÄTER", "LEGENDE · SPÄTER", "REITTIER · SPÄTER", "AUTO-REISE · SPÄTER" };
		Button[] buttons = new Button[labels.Length];
		Text[] buttonLabels = new Text[labels.Length];
		for (int index = 0; index < labels.Length; index++)
		{
			Button button = ButtonChild(root.transform, $"Placeholder_{index + 1}", labels[index]);
			float start = (float)index / 4f;
			SetAnchors(button.GetComponent<RectTransform>(), new Vector2(start + 0.005f, 0.08f), new Vector2(start + 0.245f, 0.92f));
			button.interactable = false;
			buttons[index] = button;
			buttonLabels[index] = button.GetComponentInChildren<Text>(includeInactive: true);
		}
		root.GetComponent<WorldMapQuickActions>().ConfigureUi(surface, buttons, buttonLabels);
		return SavePrefab(root, "WorldMapQuickActions");
	}

	private static GameObject BuildSideMenu()
	{
		GameObject root = RectObject("WorldMapSideMenu", typeof(Image), typeof(WorldMapSideMenu));
		SetSize(root, new Vector2(230f, 810f));
		Image surface = root.GetComponent<Image>();
		string[] labels = new string[4] { "JOURNAL\nSPÄTER", "CODEX\nSPÄTER", "ERFOLGE\nSPÄTER", "EINSTELLUNGEN\nSPÄTER" };
		Button[] placeholders = new Button[labels.Length];
		Text[] placeholderLabels = new Text[labels.Length];
		for (int index = 0; index < labels.Length; index++)
		{
			Button button = ButtonChild(root.transform, $"Placeholder_{index + 1}", labels[index]);
			float top = 0.96f - (float)index * 0.14f;
			SetAnchors(button.GetComponent<RectTransform>(), new Vector2(0.08f, top - 0.115f), new Vector2(0.92f, top));
			button.interactable = false;
			placeholders[index] = button;
			placeholderLabels[index] = button.GetComponentInChildren<Text>(includeInactive: true);
		}
		Button back = ButtonChild(root.transform, "BackAction", "SCHLIESSEN\nZURÜCK");
		SetAnchors(back.GetComponent<RectTransform>(), new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.16f));
		root.GetComponent<WorldMapSideMenu>().ConfigureUi(surface, placeholders, placeholderLabels, back, back.GetComponentInChildren<Text>(includeInactive: true));
		return SavePrefab(root, "WorldMapSideMenu");
	}

	private static GameObject BuildTransitionOverlay()
	{
		GameObject gameObject = RectObject("WorldMapTransitionOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(WorldMapTransitionOverlay));
		Stretch(gameObject.GetComponent<RectTransform>());
		Canvas component = gameObject.GetComponent<Canvas>();
		component.renderMode = RenderMode.ScreenSpaceOverlay;
		component.overrideSorting = true;
		component.sortingOrder = 500;
		CanvasScaler component2 = gameObject.GetComponent<CanvasScaler>();
		component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		component2.referenceResolution = new Vector2(1920f, 1080f);
		component2.matchWidthOrHeight = 0.5f;
		CanvasGroup group = gameObject.GetComponent<CanvasGroup>();
		Image surface = ImageChild(gameObject.transform, "FadeSurface");
		Stretch(surface.rectTransform);
		Image sigil = Marker(gameObject.transform, "EidrenEnergySigil", WorldMapContentBuilder.EffectSprite("EnergySigil"));
		SetRect(sigil.rectTransform, Vector2.zero, new Vector2(220f, 220f));
		gameObject.GetComponent<WorldMapTransitionOverlay>().ConfigureUi(group, surface, sigil);
		return SavePrefab(gameObject, "WorldMapTransitionOverlay");
	}

	private static GameObject BuildCanvas(WorldMapDefinition map, IReadOnlyList<WorldMapNodeDefinition> nodes, GameObject nodePrefab, GameObject connectionPrefab, GameObject infoPrefab, GameObject statusPrefab, GameObject quickPrefab, GameObject sidePrefab, GameObject transitionPrefab)
	{
		GameObject root = RectObject("WorldMapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WorldMapCanvasView), typeof(WorldMapController));
		root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
		CanvasScaler component = root.GetComponent<CanvasScaler>();
		component.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		component.referenceResolution = new Vector2(1920f, 1080f);
		component.matchWidthOrHeight = 0.5f;
		Image backdrop = ImageChild(root.transform, "BackgroundDeep");
		Stretch(backdrop.rectTransform);
		GameObject safe = RectObject("SafeArea", typeof(SafeAreaPanel), typeof(WorldMapResponsiveLayout));
		safe.transform.SetParent(root.transform, worldPositionStays: false);
		Stretch(safe.GetComponent<RectTransform>());
		GameObject status = Instantiate(statusPrefab, safe.transform, "TopStatusBar");
		SetAnchors(status.GetComponent<RectTransform>(), new Vector2(0.015f, 0.91f), new Vector2(0.985f, 0.99f));
		Image mapVisual = ImageChild(safe.transform, "RegionalMapViewport");
		mapVisual.sprite = map.MapVisual;
		mapVisual.preserveAspect = true;
		Text mapTitle = TextChild(mapVisual.transform, "MapTitle", map.DisplayName, 32, TextAnchor.UpperLeft);
		SetAnchors(mapTitle.rectTransform, new Vector2(0.035f, 0.87f), new Vector2(0.56f, 0.98f));
		RectTransform connectionsRoot = RectChild(mapVisual.transform, "ConnectionLayer");
		Stretch(connectionsRoot);
		Vector2 center = nodes[0].MapPosition;
		WorldMapConnectionView[] connections = new WorldMapConnectionView[nodes.Count - 1];
		for (int index = 0; index < connections.Length; index++)
		{
			GameObject connection = Instantiate(connectionPrefab, connectionsRoot, $"Connection_{index + 1}");
			Stretch(connection.GetComponent<RectTransform>());
			connections[index] = connection.GetComponent<WorldMapConnectionView>();
			connections[index].SetEndpoints(center, nodes[index + 1].MapPosition);
		}
		RectTransform nodesRoot = RectChild(mapVisual.transform, "NodeLayer");
		Stretch(nodesRoot);
		WorldMapNodeView[] nodeViews = new WorldMapNodeView[nodes.Count];
		for (int i = 0; i < nodes.Count; i++)
		{
			GameObject node = Instantiate(nodePrefab, nodesRoot, $"NodeSlot_{i + 1}");
			RectTransform component2 = node.GetComponent<RectTransform>();
			component2.anchorMin = nodes[i].MapPosition;
			component2.anchorMax = nodes[i].MapPosition;
			component2.anchoredPosition = Vector2.zero;
			nodeViews[i] = node.GetComponent<WorldMapNodeView>();
		}
		GameObject info = Instantiate(infoPrefab, safe.transform, "RegionInfoPanel");
		GameObject quick = Instantiate(quickPrefab, safe.transform, "QuickActions");
		GameObject side = Instantiate(sidePrefab, safe.transform, "SideMenu");
		GameObject transition = Instantiate(transitionPrefab, root.transform, "TransitionOverlay");
		Stretch(transition.GetComponent<RectTransform>());
		GameObject gameObject = new GameObject("WorldMapEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
		gameObject.transform.SetParent(root.transform, worldPositionStays: false);
		EventSystem eventSystem = gameObject.GetComponent<EventSystem>();
		WorldMapResponsiveLayout responsive = safe.GetComponent<WorldMapResponsiveLayout>();
		responsive.ConfigureUi(mapVisual.rectTransform, info.GetComponent<RectTransform>(), side.GetComponent<RectTransform>(), quick.GetComponent<RectTransform>());
		WorldMapCanvasView canvasView = root.GetComponent<WorldMapCanvasView>();
		canvasView.ConfigureUi(backdrop, mapVisual, mapTitle, nodeViews, connections, info.GetComponent<WorldMapInfoPanel>(), info.GetComponentInChildren<WorldMapTravelButton>(includeInactive: true), status.GetComponent<WorldMapTopStatusBar>(), quick.GetComponent<WorldMapQuickActions>(), side.GetComponent<WorldMapSideMenu>(), transition.GetComponent<WorldMapTransitionOverlay>(), responsive, eventSystem);
		root.GetComponent<WorldMapController>().ConfigureUi(map, canvasView);
		eventSystem.firstSelectedGameObject = nodeViews[0].Button.gameObject;
		return SavePrefab(root, "WorldMapCanvas");
	}

	private static void BuildScene(GameObject canvasPrefab)
	{
		Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		GameObject gameObject = new GameObject("WorldMapCamera", typeof(Camera), typeof(AudioListener));
		Camera component = gameObject.GetComponent<Camera>();
		component.orthographic = true;
		component.clearFlags = CameraClearFlags.Color;
		component.backgroundColor = new Color(0.02f, 0.04f, 0.05f);
		gameObject.tag = "MainCamera";
		PrefabUtility.InstantiatePrefab(canvasPrefab, scene);
		if (!EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/WorldMap/WorldMap.unity"))
		{
			throw new IOException("Could not save 'Assets/_Game/Scenes/WorldMap/WorldMap.unity'.");
		}
	}

	private static GameObject SavePrefab(GameObject root, string name)
	{
		string path = Prefab(name);
		GameObject gameObject = PrefabUtility.SaveAsPrefabAsset(root, path);
		UnityEngine.Object.DestroyImmediate(root);
		if (gameObject == null)
		{
			throw new IOException("Could not save WorldMap prefab '" + path + "'.");
		}
		return gameObject;
	}

	private static GameObject Instantiate(GameObject prefab, Transform parent, string name)
	{
		GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
		obj.name = name;
		obj.transform.SetParent(parent, worldPositionStays: false);
		return obj;
	}

	private static GameObject RectObject(string name, params Type[] components)
	{
		List<Type> types = new List<Type> { typeof(RectTransform) };
		types.AddRange(components);
		return new GameObject(name, types.ToArray());
	}

	private static RectTransform RectChild(Transform parent, string name)
	{
		GameObject gameObject = RectObject(name);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		return gameObject.GetComponent<RectTransform>();
	}

	private static Image ImageChild(Transform parent, string name)
	{
		GameObject gameObject = RectObject(name, typeof(Image));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		return gameObject.GetComponent<Image>();
	}

	private static Image Marker(Transform parent, string name, Sprite sprite)
	{
		Image image = ImageChild(parent, name);
		image.sprite = sprite;
		image.preserveAspect = true;
		return image;
	}

	private static Text TextChild(Transform parent, string name, string value, int size, TextAnchor alignment)
	{
		GameObject gameObject = RectObject(name, typeof(Text));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		Text component = gameObject.GetComponent<Text>();
		component.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		component.fontSize = size;
		component.alignment = alignment;
		component.text = value;
		component.color = Color.white;
		component.horizontalOverflow = HorizontalWrapMode.Wrap;
		component.verticalOverflow = VerticalWrapMode.Truncate;
		component.raycastTarget = false;
		return component;
	}

	private static Text PanelText(Transform parent, string name, float bottom, float top, int size, TextAnchor alignment)
	{
		Text text = TextChild(parent, name, string.Empty, size, alignment);
		SetAnchors(text.rectTransform, new Vector2(0.05f, bottom), new Vector2(0.95f, top));
		return text;
	}

	private static Button ButtonChild(Transform parent, string name, string label)
	{
		GameObject gameObject = RectObject(name, typeof(Image), typeof(Button));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		Image image = gameObject.GetComponent<Image>();
		image.sprite = WorldMapContentBuilder.FrameSprite("Panel");
		image.type = Image.Type.Sliced;
		Button button = gameObject.GetComponent<Button>();
		button.targetGraphic = image;
		Stretch(TextChild(gameObject.transform, "Label", label, 14, TextAnchor.MiddleCenter).rectTransform);
		return button;
	}

	private static void SetSize(GameObject value, Vector2 size)
	{
		value.GetComponent<RectTransform>().sizeDelta = size;
	}

	private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
	{
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	private static void SetAnchors(RectTransform rect, Vector2 minimum, Vector2 maximum)
	{
		rect.anchorMin = minimum;
		rect.anchorMax = maximum;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	private static void Stretch(RectTransform rect)
	{
		SetAnchors(rect, Vector2.zero, Vector2.one);
	}
}
}
