using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.Player;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class EidraForgeSceneBuilder
{
	public const string ScenePath = "Assets/_Game/Scenes/EidraForge.unity";

	public const string ZonePath = "Assets/_Game/Data/Zones/Zone_EidraForge.asset";

	private const string SourceScene = "Assets/_Game/Scenes/Zone_EmberRuins.unity";

	private const string SourceZone = "Assets/_Game/Data/Zones/Zone_EmberRuins.asset";

	[MenuItem("Eidren/Scenes/Build Eidra Forge")]
	public static void Build()
	{
		BuildZoneData();
		AddEntranceToEmberRuins();
		if (!File.Exists("Assets/_Game/Scenes/EidraForge.unity") && !AssetDatabase.CopyAsset("Assets/_Game/Scenes/Zone_EmberRuins.unity", "Assets/_Game/Scenes/EidraForge.unity"))
		{
			throw new IOException("Could not create Eidra Forge scene.");
		}
		Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/EidraForge.unity", OpenSceneMode.Single);
		ZoneController zone = Find<ZoneController>(scene);
		ConfigureZone(zone);
		ForgeSceneCleanup.Aufraeumen(scene, zone);
		BuildLayout(scene, zone);
		zone.NavigationSurface?.BuildNavMesh();
		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/EidraForge.unity");
		AddExitToEidraForge();
		AddToBuildSettings();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: built the hand-authored Eidra Forge scene.");
	}

	private static void BuildZoneData()
	{
		if (!File.Exists("Assets/_Game/Data/Zones/Zone_EidraForge.asset") && !AssetDatabase.CopyAsset("Assets/_Game/Data/Zones/Zone_EmberRuins.asset", "Assets/_Game/Data/Zones/Zone_EidraForge.asset"))
		{
			throw new IOException("Could not create forge zone data.");
		}
		ZoneDefinition zoneDefinition = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/Zone_EidraForge.asset");
		SerializedObject serializedObject = new SerializedObject(zoneDefinition);
		serializedObject.FindProperty("id").stringValue = "eidra_forge";
		serializedObject.FindProperty("displayName").stringValue = "Verlassene Eidra-Schmiede";
		serializedObject.FindProperty("sceneKey").stringValue = "EidraForge";
		serializedObject.FindProperty("allowedExitSides").intValue = 15;
		serializedObject.FindProperty("safeZone").boolValue = false;
		serializedObject.FindProperty("enemiesAllowed").boolValue = false;
		serializedObject.FindProperty("resourcesAllowed").boolValue = false;
		serializedObject.FindProperty("bossZone").boolValue = false;
		serializedObject.FindProperty("worldMapNodeId").stringValue = string.Empty;
		serializedObject.FindProperty("areaArt").objectReferenceValue = null;
		serializedObject.FindProperty("resourceAllocations").arraySize = 0;
		serializedObject.FindProperty("sideNodeAllocations").arraySize = 0;
		serializedObject.FindProperty("enemyAllocations").arraySize = 0;
		serializedObject.FindProperty("worldChestSpawnPoints").arraySize = 0;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(zoneDefinition);
	}

	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Der Eingang lebt nur in der Szene und
	/// fiel beim Struktur-Rebuild weg — eigener Einstieg, damit er ohne
	/// Forge-Neubau wiederherstellbar ist; der Struktur-Builder ruft ihn
	/// nach jedem Glutruinen-Bau selbst auf.
	/// </summary>
	[MenuItem("Eidren/Scenes/Rebuild Ember Ruins Forge Entrance")]
	public static void AddEntranceToEmberRuins()
	{
		Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_EmberRuins.unity", OpenSceneMode.Single);
		ZoneController zone = Find<ZoneController>(scene);
		Vector3 position = ((zone.BossArea != null) ? (zone.BossArea.position + new Vector3(0f, 0f, 5f)) : new Vector3(0f, 0f, 18f));
		GameObject bestehend = FindNamed(scene, "EidraForgeEntrance");
		if (bestehend != null)
		{
			/* 19.08.2026: Die alte Fassung (rote Scheibe + Quader, den die
			   Eltern-Skalierung 0,16 auf Bankhoehe plattdrueckte) weicht dem
			   Felsportal. Position bleibt erhalten. */
			position = bestehend.transform.position;
			UnityEngine.Object.DestroyImmediate(bestehend);
		}
		BuildEntrancePortal(position);
		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Zone_EmberRuins.unity");
	}

	/// <summary>
	/// Felsportal im Ruinen-Stil: flankierende Zonen-Felsen, aufrechter
	/// Steinsturz, dunkle Oeffnung mit Glutlicht. Die Wurzel bleibt
	/// UNSKALIERT — Kinder unter einer gequetschten Wurzel verlieren ihre
	/// Hoehe (die Lehre aus der alten Fassung).
	/// </summary>
	private static void BuildEntrancePortal(Vector3 position)
	{
		GameObject wurzel = new GameObject("EidraForgeEntrance");
		wurzel.transform.position = position;
		wurzel.AddComponent<EidraForgeEntrance>();
		GameObject schwelle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		schwelle.name = "PortalSchwelle";
		schwelle.transform.SetParent(wurzel.transform, worldPositionStays: false);
		schwelle.transform.localPosition = new Vector3(0f, 0.02f, 0f);
		schwelle.transform.localScale = new Vector3(2.2f, 0.16f, 2.2f);
		schwelle.GetComponent<Renderer>().sharedMaterial = Material("ForgeBasalt", new Color(0.1f, 0.085f, 0.09f));
		schwelle.GetComponent<Collider>().isTrigger = true;
		PlacePortalRock("PortalFels_West", wurzel.transform, "SP_Rock_Large", new Vector3(-2.7f, 0f, 0.6f), 2.2f, 15f);
		PlacePortalRock("PortalFels_Ost", wurzel.transform, "SP_Rock_Large", new Vector3(2.7f, 0f, 0.6f), 2.2f, 200f);
		PlacePortalRock("PortalFels_Sturzstein", wurzel.transform, "SP_Rock_Medium", new Vector3(1.7f, 3.9f, 0.6f), 1.6f, 35f);
		GameObject sturz = GameObject.CreatePrimitive(PrimitiveType.Cube);
		sturz.name = "PortalSturz";
		sturz.transform.SetParent(wurzel.transform, worldPositionStays: false);
		sturz.transform.localPosition = new Vector3(0f, 3.7f, 0.6f);
		sturz.transform.localScale = new Vector3(5.8f, 1.1f, 1.5f);
		sturz.GetComponent<Renderer>().sharedMaterial = Material("ForgeStone", new Color(0.14f, 0.12f, 0.13f));
		GameObject oeffnung = GameObject.CreatePrimitive(PrimitiveType.Cube);
		oeffnung.name = "PortalOeffnung";
		oeffnung.transform.SetParent(wurzel.transform, worldPositionStays: false);
		oeffnung.transform.localPosition = new Vector3(0f, 1.55f, 0.8f);
		oeffnung.transform.localScale = new Vector3(3.1f, 3.1f, 0.5f);
		oeffnung.GetComponent<Renderer>().sharedMaterial = Material("ForgeMaw", new Color(0.03f, 0.02f, 0.03f));
		GameObject glut = new GameObject("PortalGlut");
		glut.transform.SetParent(wurzel.transform, worldPositionStays: false);
		glut.transform.localPosition = new Vector3(0f, 1.7f, -0.8f);
		Light licht = glut.AddComponent<Light>();
		licht.type = LightType.Point;
		licht.color = new Color(1f, 0.42f, 0.1f);
		licht.range = 7f;
		licht.intensity = 1.6f;
	}

	private static void PlacePortalRock(string name, Transform parent, string prefabName, Vector3 localPosition, float scale, float yaw)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Environment/StyleProof/" + prefabName + ".prefab");
		if (prefab == null)
		{
			throw new FileNotFoundException(prefabName);
		}
		GameObject fels = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
		fels.name = name;
		fels.transform.SetParent(parent, worldPositionStays: false);
		fels.transform.localPosition = localPosition;
		fels.transform.localScale = Vector3.one * scale;
		fels.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
	}

	/// <summary>
	/// 19.08.2026: Der Ausgang des Verlieses am Windfang — Halte-Interaktion
	/// zurueck in die Glutruinen. Idempotent; im Build()-Fluss verankert,
	/// damit ihn kein Szenen-Rebuild abwirft.
	/// </summary>
	public static void AddExitToEidraForge()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
		if (FindNamed(scene, "EidraForgeExit") == null)
		{
			GameObject wurzel = new GameObject("EidraForgeExit");
			wurzel.transform.position = new Vector3(0f, 0f, -37f);
			wurzel.AddComponent<EidraForgeExit>();
			GameObject schwelle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			schwelle.name = "AusgangSchwelle";
			schwelle.transform.SetParent(wurzel.transform, worldPositionStays: false);
			schwelle.transform.localPosition = new Vector3(0f, 0.02f, 0f);
			schwelle.transform.localScale = new Vector3(2.2f, 0.16f, 2.2f);
			schwelle.GetComponent<Renderer>().sharedMaterial = Material("ForgeBasalt", new Color(0.1f, 0.085f, 0.09f));
			schwelle.GetComponent<Collider>().isTrigger = true;
			BuildExitFrame(wurzel.transform);
		}
		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene, ScenePath);
	}

	private static void BuildExitFrame(Transform wurzel)
	{
		Material stein = Material("ForgeStone", new Color(0.14f, 0.12f, 0.13f));
		foreach ((string name, Vector3 pos, Vector3 groesse) in new (string, Vector3, Vector3)[]
		{
			("AusgangPfeiler_West", new Vector3(-1.9f, 1.75f, -0.6f), new Vector3(0.9f, 3.5f, 0.9f)),
			("AusgangPfeiler_Ost", new Vector3(1.9f, 1.75f, -0.6f), new Vector3(0.9f, 3.5f, 0.9f)),
			("AusgangSturz", new Vector3(0f, 3.85f, -0.6f), new Vector3(4.7f, 0.9f, 0.9f))
		})
		{
			GameObject teil = GameObject.CreatePrimitive(PrimitiveType.Cube);
			teil.name = name;
			teil.transform.SetParent(wurzel, worldPositionStays: false);
			teil.transform.localPosition = pos;
			teil.transform.localScale = groesse;
			teil.GetComponent<Renderer>().sharedMaterial = stein;
		}
		GameObject oeffnung = GameObject.CreatePrimitive(PrimitiveType.Cube);
		oeffnung.name = "AusgangOeffnung";
		oeffnung.transform.SetParent(wurzel, worldPositionStays: false);
		oeffnung.transform.localPosition = new Vector3(0f, 1.55f, -0.9f);
		oeffnung.transform.localScale = new Vector3(3f, 3.1f, 0.4f);
		oeffnung.GetComponent<Renderer>().sharedMaterial = Material("ForgeMaw", new Color(0.03f, 0.02f, 0.03f));
		GameObject schein = new GameObject("AusgangsSchein");
		schein.transform.SetParent(wurzel, worldPositionStays: false);
		schein.transform.localPosition = new Vector3(0f, 2f, 0.5f);
		Light licht = schein.AddComponent<Light>();
		licht.type = LightType.Point;
		licht.color = new Color(1f, 0.86f, 0.6f);
		licht.range = 6f;
		licht.intensity = 1.2f;
	}

	private static void ConfigureZone(ZoneController zone)
	{
		SerializedObject serializedObject = new SerializedObject(zone);
		serializedObject.FindProperty("definition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/Zone_EidraForge.asset");
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		if (zone.BossArea != null)
		{
			zone.BossArea.gameObject.SetActive(value: false);
		}
		Clear(zone.PopulationRoot);
		SerializedObject zoneSo = new SerializedObject(zone);
		/* Der Dungeon hat nur einen Eingang: alle Richtungsspawns zeigen auf den
		   Windfang (SCHMIEDE_ENTWURF.md Abschnitt 3). */
		string[] array = new string[5] { "spawnDefault", "spawnFromNorth", "spawnFromEast", "spawnFromSouth", "spawnFromWest" };
		foreach (string name in array)
		{
			Transform spawn = zoneSo.FindProperty(name).objectReferenceValue as Transform;
			if (spawn != null)
			{
				spawn.position = new Vector3(0f, 0.2f, -35f);
			}
		}
		/* Kamerabegrenzung auf die Gesamtausdehnung des neuen Grundrisses:
		   X -36..36, Z -38..50. ZoneController.CameraBounds ist schreibgeschuetzt,
		   der Collider steckt im privaten Feld cameraBounds. */
		BoxCollider bounds = zoneSo.FindProperty("cameraBounds").objectReferenceValue as BoxCollider;
		if (bounds != null)
		{
			bounds.transform.position = Vector3.zero;
			bounds.center = new Vector3(0f, 0f, 6f);
			bounds.size = new Vector3(72f, 12f, 88f);
		}
		/* Kartenrand der Minimap: die Szenenkopie trug noch das Zonenquadrat der
		   Emberruinen um den Ursprung, das Kartenende lag damit mitten im
		   Verlies. Das Rechteck muss den Grundriss X -36..36, Z -38..50 fassen
		   (EidraForgeLayout); die Bewegungsgrenze bleibt wie in allen
		   Zonenszenen None/Unbounded, die Waende halten die Figur. */
		ZoneBoundarySettings boundary = zone.GetComponent<ZoneBoundarySettings>();
		if (boundary != null)
		{
			boundary.Configure(MovementBoundaryShape.None, new Vector3(0f, 0f, 6f), 0f, new Vector2(72f, 88f), MapSideFlags.All, new Vector3(0f, 0f, 6f), new Vector2(68f, 84f));
		}
	}

	private static void BuildLayout(Scene scene, ZoneController zone)
	{
		GameObject old = FindNamed(scene, "EidraForgeAuthoredLayout");
		if (old != null)
		{
			UnityEngine.Object.DestroyImmediate(old);
		}
		GameObject parent = new GameObject("EidraForgeAuthoredLayout");
		GameObject environment = Child(parent, "Environment");
		GameObject systems = Child(parent, "Systems");
		Transform population = Child(parent, "ForgePopulation").transform;
		Transform drops = Child(parent, "PersistentDrops").transform;
		EidraForgeGeometryBuilder.BaueBoeden(environment.transform);
		EidraForgeGeometryBuilder.BaueWaende(environment.transform);
		EidraForgePropBuilder.BaueWindrohr(environment.transform);
		EidraForgePropBuilder.BaueEsse(environment.transform);
		EidraForgePropBuilder.BaueGlut(environment.transform);
		EidraForgePropBuilder.BaueAufbauten(environment.transform);
		EidraForgePropBuilder.BaueGelaender(environment.transform);
		EidraForgePropBuilder.BaueEinzelstuecke(environment.transform);
		EidraForgeLichtBuilder.BauePunktlichter(Child(parent, "Beleuchtung").transform);
		EidraForgeLichtBuilder.SetzeSzenenlicht(zone);
		BuildChests(systems.transform);
		BuildEnemyAnchors(population);
		systems.AddComponent<EidraForgeSceneController>().Configure(AssetDatabase.LoadAssetAtPath<EidraForgeLootProfile>("Assets/_Game/Resources/Data/EidraForgeLoot_V02.asset"), population, drops);
	}

	/* Positionen kommen aus EidraForgeLayout, damit sie gegen die Flaechentabelle
	   pruefbar bleiben. Die alte Fassung hatte forge.optional.02 auf (-10|0|7)
	   stehen - dort gab es keinen Boden. */
	private static void BuildChests(Transform root)
	{
		foreach (ForgePlatz platz in EidraForgeLayout.Truhen)
		{
			EidraForgeLayout.IstAufBoden(platz.X, platz.Z, out float hoehe);
			Vector3 position = new Vector3(platz.X, hoehe, platz.Z);
			switch (platz.Id)
			{
			case "forge.reward.small":
				Chest(root, platz.Id, position, "SmallRewardChest", ForgePhysicalChestKind.Reward);
				break;
			case "forge.reward.medium":
				Chest(root, platz.Id, position, "MediumRewardChest", ForgePhysicalChestKind.Reward, ForgeRewardChestSize.Medium);
				break;
			case "forge.reward.large":
				Chest(root, platz.Id, position, "LargeRewardChest", ForgePhysicalChestKind.Reward, ForgeRewardChestSize.Large);
				break;
			case "forge.recovery":
				Chest(root, platz.Id, position, "RecoveryContainer", ForgePhysicalChestKind.Recovery);
				break;
			case "forge.elite.01":
				Chest(root, platz.Id, position, "EliteChest");
				break;
			case "forge.completion.01":
				Chest(root, platz.Id, position, "CompletionChest");
				break;
			default:
				Chest(root, platz.Id, position,
					platz.Id.StartsWith("forge.optional.", StringComparison.Ordinal) ? "OptionalChest" : "SupplyChest");
				break;
			}
		}
	}

	/* Ebenfalls aus EidraForgeLayout. Die alte Indexformel in PlaceGroup lieferte
	   fuer die ersten sechs AshRunner exakt dieselben Koordinaten wie fuer die
	   ersten sechs EmberEater - sechs Paare standen ineinander. */
	private static void BuildEnemyAnchors(Transform root)
	{
		foreach (ForgePlatz platz in EidraForgeLayout.Gegneranker)
		{
			EidraForgeLayout.IstAufBoden(platz.X, platz.Z, out float hoehe);
			Vector3 position = new Vector3(platz.X, hoehe, platz.Z);
			if (platz.Id.Contains("core_guardian", StringComparison.Ordinal))
			{
				Anchor(root, platz.Id, null, position,
					AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/Forge/CoreGuardian.prefab"),
					null, AssetDatabase.LoadAssetAtPath<CoreGuardianData>("Assets/_Game/Data/Bosses/CoreGuardian.asset"));
				continue;
			}
			Anchor(root, platz.Id, Prefabstamm(platz.Id), position);
		}
		EnemyDefinition ignivar = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/Enemy_Ignivar.asset");
		EidraForgeLayout.IstAufBoden(-30f, 34f, out float ignivarHoehe);
		Anchor(root, "forge.ignivar.01", null, new Vector3(-30f, ignivarHoehe, 34f),
			AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/WildEidra.prefab"), ignivar);
	}

	private static string Prefabstamm(string spawnId)
	{
		if (spawnId.Contains("ember_eater", StringComparison.Ordinal))
		{
			return "EmberEater";
		}
		if (spawnId.Contains("ash_runner", StringComparison.Ordinal))
		{
			return "AshRunner";
		}
		if (spawnId.Contains("forge_guardian", StringComparison.Ordinal))
		{
			return "ForgeGuardian";
		}
		return "SealGuardian";
	}

	private static void Anchor(Transform root, string id, string stem, Vector3 position, GameObject prefab = null, EnemyDefinition definition = null, CoreGuardianData core = null)
	{
		GameObject gameObject = new GameObject(id);
		gameObject.transform.SetParent(root, worldPositionStays: false);
		gameObject.transform.position = position;
		if (prefab == null && stem != null)
		{
			prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/Forge/" + stem + ".prefab");
		}
		if (definition == null && stem != null)
		{
			definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/Forge/" + stem + ".asset");
		}
		gameObject.AddComponent<EidraForgeEnemyAnchor>().Configure(id, prefab, definition, core);
	}

	private static void Chest(Transform root, string id, Vector3 position, string visual, ForgePhysicalChestKind kind = ForgePhysicalChestKind.Run, ForgeRewardChestSize size = ForgeRewardChestSize.Small)
	{
		GameObject host = new GameObject(id);
		host.transform.SetParent(root, worldPositionStays: false);
		host.transform.position = position;
		SphereCollider sphereCollider = host.AddComponent<SphereCollider>();
		sphereCollider.isTrigger = true;
		sphereCollider.radius = 1.2f;
		host.AddComponent<EidraForgeChestContainer>().Configure(kind, id, size);
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Containers/Forge/" + visual + "_2D.prefab");
		if (prefab != null)
		{
			((GameObject)PrefabUtility.InstantiatePrefab(prefab)).transform.SetParent(host.transform, worldPositionStays: false);
		}
	}

	private static void AddToBuildSettings()
	{
		List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
		foreach (EditorBuildSettingsScene item in scenes)
		{
			if (item.path == "Assets/_Game/Scenes/EidraForge.unity")
			{
				return;
			}
		}
		scenes.Add(new EditorBuildSettingsScene("Assets/_Game/Scenes/EidraForge.unity", enabled: true));
		EditorBuildSettings.scenes = scenes.ToArray();
	}

	private static T Find<T>(Scene scene) where T : Component
	{
		GameObject[] rootGameObjects = scene.GetRootGameObjects();
		for (int i = 0; i < rootGameObjects.Length; i++)
		{
			T value = rootGameObjects[i].GetComponentInChildren<T>(includeInactive: true);
			if ((object)value != null)
			{
				return value;
			}
		}
		throw new InvalidOperationException(typeof(T).Name + " is missing.");
	}

	private static GameObject FindNamed(Scene scene, string name)
	{
		GameObject[] rootGameObjects = scene.GetRootGameObjects();
		for (int i = 0; i < rootGameObjects.Length; i++)
		{
			Transform[] componentsInChildren = rootGameObjects[i].GetComponentsInChildren<Transform>(includeInactive: true);
			foreach (Transform value in componentsInChildren)
			{
				if (value.name == name)
				{
					return value.gameObject;
				}
			}
		}
		return null;
	}

	private static void Clear(Transform root)
	{
		if (!(root == null))
		{
			for (int index = root.childCount - 1; index >= 0; index--)
			{
				UnityEngine.Object.DestroyImmediate(root.GetChild(index).gameObject);
			}
		}
	}

	private static GameObject Child(GameObject parent, string name)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(parent.transform, worldPositionStays: false);
		return gameObject;
	}

	private static Material Material(string name, Color color)
	{
		string path = "Assets/_Game/Art/Actors/Forge" + "/" + name + ".mat";
		Material value = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (value == null)
		{
			value = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
			AssetDatabase.CreateAsset(value, path);
		}
		value.color = color;
		EditorUtility.SetDirty(value);
		return value;
	}
}
}
