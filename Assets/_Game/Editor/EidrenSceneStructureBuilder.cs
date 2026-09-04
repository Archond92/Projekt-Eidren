using Eidren.AI;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.Player;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Unity.AI.Navigation;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class EidrenSceneStructureBuilder
{
	private const string SceneFolder = "Assets/_Game/Scenes";

	private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player/Player.prefab";

	private const string ZoneDataFolder = "Assets/_Game/Data/Zones";

	private const string TerrockDataPath = "Assets/_Game/Data/Eidren/Terrock.asset";

	private const string NoctarionDataPath = "Assets/_Game/Data/Eidren/Noctarion.asset";

	public const float StandardZoneSize = 80f;

	private const float HomeBaseSize = 48f;

	private static readonly string[] OrderedScenePaths = new string[12]
	{
		ScenePath("Bootstrap"),
		ScenePath("MainMenu"),
		ScenePath("WorldMap"),
		ScenePath("HomeBase"),
		ScenePath("Zone_Greenwood"),
		ScenePath("Zone_Quarry"),
		ScenePath("Zone_Marsh"),
		ScenePath("Zone_EmberRuins"),
		ScenePath("Zone_TwilightGrove"),
		ScenePath("Zone_VeilMarsh"),
		ScenePath("Zone_GreyRifts"),
		// F31-009: Die Schmiede gehört fest in die Build-Liste — vorher hat
		// jeder Struktur-Rebuild sie aus den BuildSettings geworfen, weil nur
		// der EidraForgeSceneBuilder sie nachträglich ergänzte.
		ScenePath("EidraForge")
	};

	[MenuItem("Eidren/Scenes/Build Core Scene Structure")]
	public static void BuildCoreSceneStructure()
	{
		EnsureSceneFolder();
		PlayerPrefabBindings playerPrefab = AssetDatabase.LoadAssetAtPath<PlayerPrefabBindings>("Assets/_Game/Prefabs/Player/Player.prefab");
		if (playerPrefab == null)
		{
			throw new FileNotFoundException("The existing player prefab is required.", "Assets/_Game/Prefabs/Player/Player.prefab");
		}
		BuildBootstrapScene();
		BuildMainMenuScene();
		BuildWorldMapScene();
		BuildZoneScene("HomeBase", playerPrefab, 48f, homeBase: true);
		BuildZoneScene("Zone_Greenwood", playerPrefab, 80f, homeBase: false);
		BuildZoneScene("Zone_Quarry", playerPrefab, 80f, homeBase: false, "STEINBRUCH");
		BuildZoneScene("Zone_Marsh", playerPrefab, 80f, homeBase: false, "NEBELMOOR");
		BuildZoneScene("Zone_EmberRuins", playerPrefab, 80f, homeBase: false, "GLUTRUINEN");
		SetBuildSceneOrder();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: core scene structure and shared build scene list were generated.");
	}

	[MenuItem("Eidren/Scenes/Build Map Exit Scenes")]
	public static void BuildMapExitScenes()
	{
		EnsureSceneFolder();
		PlayerPrefabBindings playerPrefab = AssetDatabase.LoadAssetAtPath<PlayerPrefabBindings>("Assets/_Game/Prefabs/Player/Player.prefab");
		if (playerPrefab == null)
		{
			throw new FileNotFoundException("The existing player prefab is required.", "Assets/_Game/Prefabs/Player/Player.prefab");
		}
		BuildZoneScene("HomeBase", playerPrefab, 48f, homeBase: true);
		BuildZoneScene("Zone_Greenwood", playerPrefab, 80f, homeBase: false);
		BuildZoneScene("Zone_Quarry", playerPrefab, 80f, homeBase: false, "STEINBRUCH");
		BuildZoneScene("Zone_Marsh", playerPrefab, 80f, homeBase: false, "NEBELMOOR");
		BuildZoneScene("Zone_EmberRuins", playerPrefab, 80f, homeBase: false, "GLUTRUINEN");
		SetBuildSceneOrder();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: HomeBase and Zone_Greenwood map exit structures were generated.");
	}

	[MenuItem("Eidren/Scenes/Resize Outdoor Zones To Standard")]
	public static void ResizeOutdoorZonesToStandard()
	{
		EnsureSceneFolder();
		PlayerPrefabBindings playerPrefab = AssetDatabase.LoadAssetAtPath<PlayerPrefabBindings>("Assets/_Game/Prefabs/Player/Player.prefab");
		if (playerPrefab == null)
		{
			throw new FileNotFoundException("The existing player prefab is required.", "Assets/_Game/Prefabs/Player/Player.prefab");
		}
		BuildZoneScene("Zone_Quarry", playerPrefab, 80f, homeBase: false, "STEINBRUCH");
		BuildZoneScene("Zone_Marsh", playerPrefab, 80f, homeBase: false, "NEBELMOOR");
		BuildZoneScene("Zone_EmberRuins", playerPrefab, 80f, homeBase: false, "GLUTRUINEN");
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: Steinbruch, Nebelmoor und Glutruinen stehen jetzt " + $"auf {80f}. Greenwood blieb unangetastet.");
	}

	[MenuItem("Eidren/Scenes/Build Resource Zone Scenes")]
	public static void BuildResourceZoneScenes()
	{
		EnsureSceneFolder();
		PlayerPrefabBindings playerPrefab = AssetDatabase.LoadAssetAtPath<PlayerPrefabBindings>("Assets/_Game/Prefabs/Player/Player.prefab");
		if (playerPrefab == null)
		{
			throw new FileNotFoundException("The existing player prefab is required.", "Assets/_Game/Prefabs/Player/Player.prefab");
		}
		BuildZoneScene("Zone_Greenwood", playerPrefab, 80f, homeBase: false);
		BuildZoneScene("Zone_Quarry", playerPrefab, 64f, homeBase: false, "STEINBRUCH");
		BuildZoneScene("Zone_Marsh", playerPrefab, 64f, homeBase: false, "NEBELMOOR");
		BuildZoneScene("Zone_EmberRuins", playerPrefab, 72f, homeBase: false, "GLUTRUINEN");
		SetBuildSceneOrder();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	[MenuItem("Eidren/Scenes/Build Tier Two Zone Scenes")]
	public static void BuildTierTwoZoneScenes()
	{
		EnsureSceneFolder();
		PlayerPrefabBindings playerPrefab = AssetDatabase.LoadAssetAtPath<PlayerPrefabBindings>("Assets/_Game/Prefabs/Player/Player.prefab");
		if (playerPrefab == null)
		{
			throw new FileNotFoundException("The player prefab is required.");
		}
		BuildZoneScene("Zone_TwilightGrove", playerPrefab, 80f, homeBase: false, "DAEMMERHAIN");
		BuildZoneScene("Zone_VeilMarsh", playerPrefab, 80f, homeBase: false, "SCHLEIERMOOR");
		BuildZoneScene("Zone_GreyRifts", playerPrefab, 80f, homeBase: false, "GRAUKLUEFTE");
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	public static void EnsureCoreScenesAndBuildOrder()
	{
		if (OrderedScenePaths.Any((string path) => !File.Exists(path)))
		{
			BuildCoreSceneStructure();
		}
		else
		{
			SetBuildSceneOrder();
		}
	}

	public static void SetBuildSceneOrder()
	{
		string[] orderedScenePaths = OrderedScenePaths;
		foreach (string path in orderedScenePaths)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("Required build scene is missing: " + path, path);
			}
		}
		EditorBuildSettings.scenes = OrderedScenePaths.Select((string path2) => new EditorBuildSettingsScene(path2, enabled: true)).ToArray();
	}

	private static void BuildBootstrapScene()
	{
		Scene scene = NewScene();
		new GameObject("EIDREN_BOOTSTRAP").AddComponent<ApplicationBootstrap>();
		Save(scene, "Bootstrap");
	}

	private static void BuildMainMenuScene()
	{
		Scene scene = NewScene();
		CreateMenuCamera();
		EventSystem eventSystem = CreateEventSystem();
		GameObject gameObject = new GameObject("MainMenuRoot");
		MainMenuController controller = gameObject.AddComponent<MainMenuController>();
		Canvas canvas = CreateCanvas(gameObject.transform, "MainMenuCanvas");
		Stretch(CreateImage(canvas.transform, "Background", new Color(0.035f, 0.075f, 0.09f, 1f)).rectTransform);
		SetRect(CreateText(canvas.transform, "Title", "EIDREN", 72, TextAnchor.MiddleCenter, new Color(0.94f, 0.82f, 0.48f, 1f)).rectTransform, new Vector2(0f, 330f), new Vector2(700f, 110f));
		// W-006: Versionslabel; bei einem neuen Beinamen hier UND in MainMenu.unity ersetzen.
		SetRect(CreateText(canvas.transform, "Subtitle", "V0.3  ·  TESTVERSION", 24, TextAnchor.MiddleCenter, new Color(0.62f, 0.76f, 0.72f, 1f)).rectTransform, new Vector2(0f, 260f), new Vector2(700f, 55f));
		Button newGame = CreateButton(canvas.transform, "NewGameButton", "NEUES SPIEL", new Vector2(0f, 120f));
		Button continueGame = CreateButton(canvas.transform, "ContinueButton", "FORTSETZEN  ·  NOCH NICHT VERFÜGBAR", new Vector2(0f, 35f));
		Button settings = CreateButton(canvas.transform, "SettingsButton", "EINSTELLUNGEN", new Vector2(0f, -50f));
		Button quit = CreateButton(canvas.transform, "QuitButton", "BEENDEN", new Vector2(0f, -135f));
		GameObject settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
		settingsPanel.transform.SetParent(canvas.transform, worldPositionStays: false);
		SetRect(settingsPanel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(720f, 430f));
		settingsPanel.GetComponent<Image>().color = new Color(0.055f, 0.11f, 0.13f, 0.98f);
		SetRect(CreateText(settingsPanel.transform, "SettingsTitle", "EINSTELLUNGEN", 42, TextAnchor.MiddleCenter, new Color(0.94f, 0.82f, 0.48f, 1f)).rectTransform, new Vector2(0f, 130f), new Vector2(600f, 70f));
		SetRect(CreateText(settingsPanel.transform, "SettingsPlaceholder", "Audio-, Grafik- und Steuerungsoptionen\nfolgen in einer späteren Version.", 25, TextAnchor.MiddleCenter, new Color(0.78f, 0.84f, 0.8f, 1f)).rectTransform, new Vector2(0f, 20f), new Vector2(600f, 120f));
		Button closeSettings = CreateButton(settingsPanel.transform, "CloseSettingsButton", "SCHLIESSEN", new Vector2(0f, -125f));
		controller.ConfigureUi(newGame, continueGame, settings, quit, closeSettings, settingsPanel, continueGame.GetComponentInChildren<Text>());
		settingsPanel.SetActive(value: false);
		continueGame.interactable = false;
		eventSystem.firstSelectedGameObject = newGame.gameObject;
		Save(scene, "MainMenu");
	}

	private static void BuildWorldMapScene()
	{
		if (!File.Exists("Assets/_Game/Scenes/WorldMap/WorldMap.unity"))
		{
			Scene scene = NewScene();
			CreateMenuCamera();
			Canvas canvas = CreateCanvas(new GameObject("WorldMapRoot").transform, "WorldMapCanvas");
			Stretch(CreateImage(canvas.transform, "Background", new Color(0.055f, 0.09f, 0.075f, 1f)).rectTransform);
			SetRect(CreateText(canvas.transform, "WorldMapPlaceholder", "WELTKARTE\n\nStruktureller Platzhalter", 48, TextAnchor.MiddleCenter, new Color(0.88f, 0.81f, 0.55f, 1f)).rectTransform, Vector2.zero, new Vector2(850f, 300f));
			Save(scene, "WorldMap");
		}
	}

	private static void BuildZoneScene(string sceneName, PlayerPrefabBindings playerPrefab, float size, bool homeBase, string developmentLabel = null)
	{
		ZoneDefinition definition = EnsureZoneDefinition(sceneName);
		Scene scene = NewScene();
		GameObject zoneRoot = new GameObject("ZoneRoot");
		ZoneBoundarySettings boundarySettings = zoneRoot.AddComponent<ZoneBoundarySettings>();
		ZoneController zoneController = zoneRoot.AddComponent<ZoneController>();
		boundarySettings.Configure(MovementBoundaryShape.None, Vector3.zero, 0f, new Vector2(size, size), MapSideFlags.All, Vector3.zero, new Vector2(size - 4f, size - 4f));
		GameObject systemsRoot = Child(zoneRoot.transform, "Systems");
		GameObject spawnPoints = Child(zoneRoot.transform, "PlayerSpawnPoints");
		float spawnInset = 7f;
		float halfSize = size * 0.5f;
		GameObject defaultSpawn = CreateSpawn(spawnPoints.transform, "Spawn_Default", new Vector3(0f, 0.05f, homeBase ? (-4f) : (-12f)), Vector3.forward);
		GameObject fromNorth = CreateSpawn(spawnPoints.transform, "Spawn_FromNorth", new Vector3(0f, 0.05f, halfSize - spawnInset), Vector3.back);
		GameObject fromEast = CreateSpawn(spawnPoints.transform, "Spawn_FromEast", new Vector3(halfSize - spawnInset, 0.05f, 0f), Vector3.left);
		GameObject fromSouth = CreateSpawn(spawnPoints.transform, "Spawn_FromSouth", new Vector3(0f, 0.05f, 0f - halfSize + spawnInset), Vector3.forward);
		GameObject fromWest = CreateSpawn(spawnPoints.transform, "Spawn_FromWest", new Vector3(0f - halfSize + spawnInset, 0.05f, 0f), Vector3.right);
		GameObject environmentRoot = Child(zoneRoot.transform, "EnvironmentRoot");
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		gameObject.name = "WalkableGround";
		gameObject.transform.SetParent(environmentRoot.transform, worldPositionStays: false);
		gameObject.transform.position = new Vector3(0f, -0.5f, 0f);
		gameObject.transform.localScale = new Vector3(size, 1f, size);
		gameObject.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Resources/EidrenRuntimeMaterial.mat");
		Collider groundCollider = gameObject.GetComponent<Collider>();
		// Frueher entstanden hier graue Testkoerper (SafeArea-Zylinder, massive
		// Hindernisse) als Kollisions- und Navigationshilfe des Szenenaufbaus.
		// Sie waren im Spiel als Platzhalter sichtbar und werden nicht mehr
		// erzeugt (F-012) — sonst holt ein Neuaufbau sie in bereinigte Szenen
		// zurueck. Die Gebietsgrafik liefert AreaArtSceneBuilder.
		GameObject gameplayRoot = Child(zoneRoot.transform, "GameplayRoot");
		GameObject lootRoot = ((!homeBase) ? Child(gameplayRoot.transform, "LootRoot") : null);
		if (!string.IsNullOrWhiteSpace(developmentLabel))
		{
			gameplayRoot.AddComponent<DevelopmentZoneLabel>().Configure(developmentLabel);
		}
		// F31-009: Gegner werden nicht mehr eingebacken — der
		// ZoneEnemyPopulator stellt die Besetzung zur Laufzeit auf, damit die
		// Kistenwachen an den pro Lauf gewürfelten Kisten stehen.
		GameObject populationRoot = Child(zoneRoot.transform, "PopulationRoot");
		GameObject navigationRoot = Child(zoneRoot.transform, "NavigationRoot");
		NavMeshSurface navigationSurface = null;
		if (!homeBase)
		{
			navigationSurface = navigationRoot.AddComponent<NavMeshSurface>();
			navigationSurface.collectObjects = CollectObjects.All;
			navigationSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
			navigationSurface.layerMask = -1;
		}
		BoxCollider boundsCollider = Child(zoneRoot.transform, "CameraBounds").AddComponent<BoxCollider>();
		boundsCollider.isTrigger = true;
		boundsCollider.size = new Vector3(size - 4f, 8f, size - 4f);
		GameObject exitsRoot = Child(zoneRoot.transform, "ExitVolumesRoot");
		MapExitCoordinator exitCoordinator = exitsRoot.AddComponent<MapExitCoordinator>();
		MapExitFeedback feedback = Child(gameplayRoot.transform, "MapExitFeedback").AddComponent<MapExitFeedback>();
		exitCoordinator.Configure(ZoneId(sceneName), boundarySettings, feedback);
		CreateExitVolumes(exitsRoot.transform, size, ZoneId(sceneName), exitCoordinator);
		MapExitVolume[] exitVolumes = exitsRoot.GetComponentsInChildren<MapExitVolume>();
		Transform bossArea = null;
		if (definition.IsBossZone)
		{
			GameObject gameObject3 = Child(zoneRoot.transform, "BossArea");
			BossAreaController area = gameObject3.AddComponent<BossAreaController>();
			GameObject home = Child(gameObject3.transform, "HomePoint");
			home.transform.position = new Vector3(0f, 0.05f, 8f);
			GameObject bossSpawn = Child(gameObject3.transform, "BossSpawn");
			bossSpawn.transform.position = home.transform.position;
			bossSpawn.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
			GameObject gameObject4 = Child(gameObject3.transform, "CameraFramingZone");
			gameObject4.transform.position = home.transform.position;
			BoxCollider framingCollider = gameObject4.AddComponent<BoxCollider>();
			framingCollider.isTrigger = true;
			framingCollider.size = new Vector3(34f, 6f, 34f);
			BossController garonPrefab = AssetDatabase.LoadAssetAtPath<BossController>("Assets/_Game/Prefabs/Bosses/Garon.prefab");
			BossData garonData = AssetDatabase.LoadAssetAtPath<BossData>("Assets/_Game/Data/Bosses/Garon.asset");
			Material telegraph = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Enemies/Materials/EN_GaronTelegraph.mat");
			BossEncounterView encounterView = AssetDatabase.LoadAssetAtPath<BossEncounterView>("Assets/_Game/Resources/UI/BossEncounterView.prefab");
			if (garonPrefab == null || garonData == null || telegraph == null || encounterView == null)
			{
				throw new FileNotFoundException("Garon Ember Ruins boss content is missing. Run Eidren/Bosses/Build Garon in Ember Ruins.");
			}
			area.Configure("boss_area.ember_ruins.garon", home.transform, bossSpawn.transform, 18f, 11f, garonData.Navigation.LeashRange, framingCollider, garonPrefab, garonData, telegraph, encounterView);
			bossArea = gameObject3.transform;
		}
		Camera camera = CreateZoneCamera(systemsRoot.transform);
		CreateZoneLight(systemsRoot.transform);
		ZonePlayerSpawner spawner = Child(systemsRoot.transform, "PlayerSpawner").AddComponent<ZonePlayerSpawner>();
		WeaponData hammer = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Hammer.asset");
		WeaponData daggers = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Daggers.asset");
		if (hammer == null || daggers == null)
		{
			throw new FileNotFoundException("Outdoor combat requires Hammer.asset and Daggers.asset.");
		}
		EidraData terrock = AssetDatabase.LoadAssetAtPath<EidraData>("Assets/_Game/Data/Eidren/Terrock.asset");
		EidraData noctarion = AssetDatabase.LoadAssetAtPath<EidraData>("Assets/_Game/Data/Eidren/Noctarion.asset");
		if (terrock == null || noctarion == null)
		{
			throw new FileNotFoundException("Zones require Terrock.asset and Noctarion.asset.");
		}
		spawner.Configure(playerPrefab, camera, exitCoordinator, hammer, daggers, terrock, noctarion);
		ZoneLootController lootController = null;
		if (!homeBase)
		{
			WorldItemController worldItemPrefab = AssetDatabase.LoadAssetAtPath<WorldItemController>("Assets/_Game/Prefabs/Items/WorldItem.prefab");
			if (worldItemPrefab == null)
			{
				throw new FileNotFoundException("Outdoor loot requires WorldItem.prefab. Run Eidren/Data/Build Loot V0.1.");
			}
			lootController = Child(systemsRoot.transform, "LootSystem").AddComponent<ZoneLootController>();
			lootController.Configure(zoneController, lootRoot.transform, worldItemPrefab, 20491);
		}
		zoneController.Configure(definition, systemsRoot.transform, spawnPoints.transform, defaultSpawn.transform, fromNorth.transform, fromEast.transform, fromSouth.transform, fromWest.transform, environmentRoot.transform, gameplayRoot.transform, populationRoot.transform, navigationRoot.transform, boundsCollider, exitsRoot.transform, bossArea, groundCollider, boundarySettings, spawner, lootController, exitCoordinator, navigationSurface, exitVolumes);
		if (navigationSurface != null)
		{
			navigationSurface.BuildNavMesh();
		}
		Save(scene, sceneName);
		if (sceneName == "Zone_EmberRuins")
		{
			// Der Verlies-Eingang haengt als nachgelagerter Aufbau an dieser
			// Szene — ohne diesen Aufruf wirft jeder Rebuild ihn ab (die
			// Falle hat die veroeffentlichte v0.3.1 getroffen).
			EidraForgeSceneBuilder.AddEntranceToEmberRuins();
		}
	}

	private static GameObject CreateSpawn(Transform parent, string name, Vector3 position, Vector3 forward)
	{
		GameObject gameObject = Child(parent, name);
		gameObject.transform.position = position;
		gameObject.transform.rotation = Quaternion.LookRotation(forward);
		return gameObject;
	}

	private static ZoneDefinition EnsureZoneDefinition(string sceneName)
	{
		EnsureFolder("Assets/_Game/Data", "Zones");
		string assetName = ((sceneName == "HomeBase") ? "Zone_HomeBase" : sceneName);
		string path = "Assets/_Game/Data/Zones/" + assetName + ".asset";
		ZoneDefinition definition = AssetDatabase.LoadAssetAtPath<ZoneDefinition>(path);
		if (definition == null)
		{
			definition = ScriptableObject.CreateInstance<ZoneDefinition>();
			AssetDatabase.CreateAsset(definition, path);
		}
		SerializedObject serializedObject = new SerializedObject(definition);
		serializedObject.FindProperty("id").stringValue = ZoneId(sceneName);
		serializedObject.FindProperty("displayName").stringValue = ZoneDisplayName(sceneName);
		serializedObject.FindProperty("sceneKey").stringValue = sceneName;
		serializedObject.FindProperty("regionType").enumValueIndex = (int)ZoneRegionType(sceneName);
		serializedObject.FindProperty("allowedExitSides").intValue = 15;
		bool homeBase = sceneName == "HomeBase";
		serializedObject.FindProperty("safeZone").boolValue = homeBase;
		serializedObject.FindProperty("enemiesAllowed").boolValue = !homeBase;
		serializedObject.FindProperty("resourcesAllowed").boolValue = true;
		serializedObject.FindProperty("bossZone").boolValue = sceneName == "Zone_EmberRuins";
		serializedObject.FindProperty("worldMapNodeId").stringValue = ZoneId(sceneName);
		SerializedProperty serializedProperty = serializedObject.FindProperty("camera");
		ZoneCameraSettings cameraSettings = ZoneCameraSettings.Default;
		serializedProperty.FindPropertyRelative("Offset").vector3Value = cameraSettings.Offset;
		serializedProperty.FindPropertyRelative("Rotation").vector3Value = cameraSettings.Rotation;
		serializedProperty.FindPropertyRelative("BaseOrthographicSize").floatValue = cameraSettings.BaseOrthographicSize;
		serializedProperty.FindPropertyRelative("MinimumOrthographicSize").floatValue = cameraSettings.MinimumOrthographicSize;
		serializedProperty.FindPropertyRelative("BoundsInset").floatValue = cameraSettings.BoundsInset;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(definition);
		return definition;
	}

	private static string ZoneDisplayName(string sceneName)
	{
		return sceneName switch
		{
			"HomeBase" => "Heimatbasis", 
			"Zone_Greenwood" => "Grünwald", 
			"Zone_Quarry" => "Steinbruch", 
			"Zone_Marsh" => "Nebelmoor", 
			"Zone_EmberRuins" => "Glutruinen", 
			"Zone_TwilightGrove" => "Dämmerhain", 
			"Zone_VeilMarsh" => "Schleiermoor", 
			"Zone_GreyRifts" => "Grauklüfte", 
			_ => sceneName, 
		};
	}

	private static WorldRegionType ZoneRegionType(string sceneName)
	{
		return sceneName switch
		{
			"HomeBase" => WorldRegionType.SafeHomeland, 
			"Zone_Greenwood" => WorldRegionType.Forest, 
			"Zone_Quarry" => WorldRegionType.Quarry, 
			"Zone_Marsh" => WorldRegionType.Marsh, 
			"Zone_TwilightGrove" => WorldRegionType.TwilightGrove, 
			"Zone_VeilMarsh" => WorldRegionType.VeilMarsh, 
			"Zone_GreyRifts" => WorldRegionType.GreyRifts, 
			_ => WorldRegionType.EmberRuins, 
		};
	}

	private static void CreateExitVolumes(Transform parent, float size, string zoneId, MapExitCoordinator coordinator)
	{
		float safeHalf = size * 0.5f - 2f;
		float sideSpan = size + 24f;
		CreateExit(parent, "Exit_North", new Vector3(0f, 1f, safeHalf + 6f), new Vector3(sideSpan, 4f, 12f), zoneId, MapExitDirection.North, coordinator);
		CreateExit(parent, "Exit_South", new Vector3(0f, 1f, 0f - safeHalf - 6f), new Vector3(sideSpan, 4f, 12f), zoneId, MapExitDirection.South, coordinator);
		CreateExit(parent, "Exit_East", new Vector3(safeHalf + 6f, 1f, 0f), new Vector3(12f, 4f, sideSpan), zoneId, MapExitDirection.East, coordinator);
		CreateExit(parent, "Exit_West", new Vector3(0f - safeHalf - 6f, 1f, 0f), new Vector3(12f, 4f, sideSpan), zoneId, MapExitDirection.West, coordinator);
	}

	private static void CreateExit(Transform parent, string name, Vector3 position, Vector3 size, string zoneId, MapExitDirection direction, MapExitCoordinator coordinator)
	{
		GameObject gameObject = Child(parent, name);
		gameObject.transform.position = position;
		BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
		boxCollider.isTrigger = true;
		boxCollider.size = size;
		gameObject.AddComponent<MapExitVolume>().Configure(zoneId + ".exit." + direction.ToString().ToLowerInvariant(), direction, zoneId, active: true, 0.4f, "WorldMap", string.Empty, coordinator);
	}

	private static string ZoneId(string sceneName)
	{
		return sceneName switch
		{
			"HomeBase" => "home_base", 
			"Zone_Greenwood" => "zone_greenwood", 
			"Zone_Quarry" => "zone_quarry", 
			"Zone_Marsh" => "zone_marsh", 
			"Zone_EmberRuins" => "zone_ember_ruins", 
			"Zone_TwilightGrove" => "zone_twilight_grove", 
			"Zone_VeilMarsh" => "zone_veil_marsh", 
			"Zone_GreyRifts" => "zone_grey_rifts", 
			_ => throw new ArgumentOutOfRangeException("sceneName", sceneName, "Unknown zone scene."), 
		};
	}

	private static Camera CreateZoneCamera(Transform parent)
	{
		GameObject gameObject = new GameObject("Main Camera");
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.tag = "MainCamera";
		Camera camera = gameObject.AddComponent<Camera>();
		camera.orthographic = true;
		camera.orthographicSize = 7.4f;
		camera.nearClipPlane = 0.1f;
		camera.farClipPlane = 120f;
		camera.clearFlags = CameraClearFlags.Color;
		camera.backgroundColor = new Color(0.075f, 0.12f, 0.14f);
		// G-003: ohne dieses Flag rendert URP keine Volume-Profile.
		camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
		return camera;
	}

	private static void CreateZoneLight(Transform parent)
	{
		// G-003: Werte kommen aus ZoneLightingBuilder, damit neu gebaute und
		// nachtraeglich umgestellte Szenen dieselbe Sonne tragen. Die Farbe
		// setzt ZoneLightingBuilder.ApplyAll je Gebiet aus der AreaArt-
		// Definition; hier steht nur der neutrale Ausgangswert.
		Light light = Child(parent, "Sun").AddComponent<Light>();
		light.type = LightType.Directional;
		light.intensity = ZoneLightingBuilder.SunIntensity;
		light.color = new Color(1f, 0.93f, 0.78f);
		light.transform.rotation = Quaternion.Euler(ZoneLightingBuilder.SunEuler);
		light.shadows = ZoneLightingBuilder.SunShadows;
		light.shadowStrength = ZoneLightingBuilder.SunShadowStrength;
	}

	private static void CreateMenuCamera()
	{
		Camera camera = new GameObject("Main Camera")
		{
			tag = "MainCamera"
		}.AddComponent<Camera>();
		camera.clearFlags = CameraClearFlags.Color;
		camera.backgroundColor = new Color(0.025f, 0.04f, 0.055f);
	}

	private static EventSystem CreateEventSystem()
	{
		return new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
	}

	private static Canvas CreateCanvas(Transform parent, string name)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		Canvas canvas = gameObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		CanvasScaler component = gameObject.GetComponent<CanvasScaler>();
		component.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		component.referenceResolution = new Vector2(1920f, 1080f);
		component.matchWidthOrHeight = 1f;
		return canvas;
	}

	private static Button CreateButton(Transform parent, string name, string label, Vector2 position)
	{
		GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
		buttonObject.transform.SetParent(parent, worldPositionStays: false);
		SetRect(buttonObject.GetComponent<RectTransform>(), position, new Vector2(520f, 66f));
		buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.24f, 0.25f, 0.98f);
		Button component = buttonObject.GetComponent<Button>();
		ColorBlock colors = component.colors;
		colors.highlightedColor = new Color(0.22f, 0.48f, 0.42f, 1f);
		colors.pressedColor = new Color(0.85f, 0.59f, 0.2f, 1f);
		colors.disabledColor = new Color(0.09f, 0.12f, 0.13f, 0.72f);
		component.colors = colors;
		Stretch(CreateText(buttonObject.transform, "Label", label, 24, TextAnchor.MiddleCenter, Color.white).rectTransform);
		return component;
	}

	private static Image CreateImage(Transform parent, string name, Color color)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		Image component = gameObject.GetComponent<Image>();
		component.color = color;
		return component;
	}

	private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment, Color color)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		Text component = gameObject.GetComponent<Text>();
		component.text = value;
		component.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		component.fontSize = fontSize;
		component.alignment = alignment;
		component.color = color;
		return component;
	}

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
	{
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = anchoredPosition;
		rect.sizeDelta = size;
	}

	private static GameObject Child(Transform parent, string name)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		return gameObject;
	}

	private static Scene NewScene()
	{
		return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
	}

	private static void Save(Scene scene, string sceneName)
	{
		string path = ScenePath(sceneName);
		if (!EditorSceneManager.SaveScene(scene, path))
		{
			throw new IOException("Could not save scene '" + path + "'.");
		}
	}

	private static string ScenePath(string sceneName)
	{
		if (sceneName == "WorldMap")
		{
			return "Assets/_Game/Scenes/WorldMap/WorldMap.unity";
		}
		return "Assets/_Game/Scenes/" + sceneName + ".unity";
	}

	private static void EnsureSceneFolder()
	{
		if (!AssetDatabase.IsValidFolder("Assets/_Game"))
		{
			AssetDatabase.CreateFolder("Assets", "_Game");
		}
		if (!AssetDatabase.IsValidFolder("Assets/_Game/Scenes"))
		{
			AssetDatabase.CreateFolder("Assets/_Game", "Scenes");
		}
		if (!AssetDatabase.IsValidFolder("Assets/_Game/Scenes/WorldMap"))
		{
			AssetDatabase.CreateFolder("Assets/_Game/Scenes", "WorldMap");
		}
	}

	private static void EnsureFolder(string parent, string name)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + name))
		{
			AssetDatabase.CreateFolder(parent, name);
		}
	}
}
}
