using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using Eidren.UI;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class EidraForgeEnemyContentBuilder
{
	private readonly struct Spec
	{
		public string Stem { get; }

		public string Id { get; }

		public string Name { get; }

		public float Health { get; }

		public float Stagger { get; }

		public float Damage { get; }

		public float Secondary { get; }

		public float Protection { get; }

		public int Xp { get; }

		public EnemyCombatStyle Style { get; }

		public float Scale { get; }

		public Spec(string stem, string id, string name, float health, float stagger, float damage, float secondary, float protection, int xp, EnemyCombatStyle style, float scale)
		{
			Stem = stem;
			Id = id;
			Name = name;
			Health = health;
			Stagger = stagger;
			Damage = damage;
			Secondary = secondary;
			Protection = protection;
			Xp = xp;
			Style = style;
			Scale = scale;
		}
	}

	public const string DataRoot = "Assets/_Game/Data/Enemies/Forge";

	public const string PrefabRoot = "Assets/_Game/Prefabs/Enemies/Forge";

	public const string CoreDataPath = "Assets/_Game/Data/Bosses/CoreGuardian.asset";

	public const string CorePrefabPath = "Assets/_Game/Prefabs/Enemies/Forge/CoreGuardian.prefab";

	[MenuItem("Eidren/Data/Build Eidra Forge Enemies")]
	public static void Build()
	{
		EnsureFolders();
		foreach (Spec item in Specs())
		{
			EnemyDefinition definition = BuildDefinition(item);
			GameObject prefab = BuildEnemyPrefab(item, definition);
			SerializedObject serializedObject = new SerializedObject(definition);
			serializedObject.FindProperty("prefab").objectReferenceValue = prefab;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(definition);
		}
		BuildCorePrefab(BuildCoreData());
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: built the fixed Eidra Forge enemy cast.");
	}

	private static IEnumerable<Spec> Specs()
	{
		yield return new Spec("EmberEater", "enemy.ember_eater", "Glutzehrer", 95f, 40f, 12f, 0f, 0f, 8, EnemyCombatStyle.Melee, 0.86f);
		yield return new Spec("AshRunner", "enemy.ash_runner", "Aschenläufer", 180f, 70f, 22f, 0f, 0f, 20, EnemyCombatStyle.Charge, 1.05f);
		yield return new Spec("ForgeGuardian", "enemy.forge_guardian", "Schmiedewächter", 360f, 170f, 30f, 0f, 0.2f, 45, EnemyCombatStyle.ArmoredMelee, 1.22f);
		yield return new Spec("SealGuardian", "enemy.seal_guardian", "Siegelwächter", 750f, 240f, 30f, 38f, 0.15f, 140, EnemyCombatStyle.AreaElite, 1.48f);
	}

	private static EnemyDefinition BuildDefinition(Spec spec)
	{
		EnemyDefinition enemyDefinition = LoadOrCreate<EnemyDefinition>("Assets/_Game/Data/Enemies/Forge/" + spec.Stem + ".asset");
		enemyDefinition.name = spec.Stem;
		SerializedObject serializedObject = new SerializedObject(enemyDefinition);
		serializedObject.FindProperty("id").stringValue = spec.Id;
		serializedObject.FindProperty("displayName").stringValue = spec.Name;
		serializedObject.FindProperty("maximumHealth").floatValue = spec.Health;
		serializedObject.FindProperty("maximumStagger").floatValue = spec.Stagger;
		serializedObject.FindProperty("staggerDuration").floatValue = 3f;
		serializedObject.FindProperty("attackDamage").floatValue = spec.Damage;
		serializedObject.FindProperty("secondaryAttackDamage").floatValue = spec.Secondary;
		serializedObject.FindProperty("protection").floatValue = spec.Protection;
		serializedObject.FindProperty("combatStyle").enumValueIndex = (int)spec.Style;
		serializedObject.FindProperty("postStaggerResistanceDuration").floatValue = ((spec.Id == "enemy.seal_guardian") ? 3f : 0f);
		serializedObject.FindProperty("postStaggerDamageMultiplier").floatValue = ((spec.Id == "enemy.seal_guardian") ? 0.5f : 1f);
		serializedObject.FindProperty("telegraphDuration").floatValue = ((spec.Style == EnemyCombatStyle.AreaElite) ? 1.05f : 0.55f);
		serializedObject.FindProperty("attackWindowDuration").floatValue = 0.2f;
		serializedObject.FindProperty("recoveryDuration").floatValue = 0.9f;
		serializedObject.FindProperty("deathDisableDelay").floatValue = 120f;
		serializedObject.FindProperty("experienceReward").intValue = spec.Xp;
		serializedObject.FindProperty("lootTable").objectReferenceValue = null;
		SetNavigation(serializedObject.FindProperty("navigation"), spec.Scale);
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(enemyDefinition);
		return enemyDefinition;
	}

	private static GameObject BuildEnemyPrefab(Spec spec, EnemyDefinition definition)
	{
		string path = "Assets/_Game/Prefabs/Enemies/Forge/" + spec.Stem + ".prefab";
		if (!File.Exists(path) && !AssetDatabase.CopyAsset("Assets/_Game/Prefabs/Enemies/Wildling.prefab", path))
		{
			throw new IOException("Could not create '" + path + "'.");
		}
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		try
		{
			root.name = spec.Stem;
			SerializedObject serializedObject = new SerializedObject(root.GetComponent<WildlingController>());
			serializedObject.FindProperty("definition").objectReferenceValue = definition;
			serializedObject.FindProperty("projectileVisualPrefab").objectReferenceValue = null;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			ReplaceVisual(root.transform.Find("Visual"), spec.Stem);
			CapsuleCollider body = root.GetComponent<CapsuleCollider>();
			body.radius = 0.48f * spec.Scale;
			body.height = 2f * spec.Scale;
			body.center = Vector3.up * body.height * 0.5f;
			PrefabUtility.SaveAsPrefabAsset(root, path);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
		return AssetDatabase.LoadAssetAtPath<GameObject>(path);
	}

	private static CoreGuardianData BuildCoreData()
	{
		CoreGuardianData coreGuardianData = LoadOrCreate<CoreGuardianData>("Assets/_Game/Data/Bosses/CoreGuardian.asset");
		coreGuardianData.name = "CoreGuardian";
		SerializedObject serializedObject = new SerializedObject(coreGuardianData);
		serializedObject.FindProperty("id").stringValue = "enemy.core_guardian";
		serializedObject.FindProperty("maximumHealth").floatValue = 3000f;
		serializedObject.FindProperty("maximumStagger").floatValue = 400f;
		serializedObject.FindProperty("closedProtection").floatValue = 0.35f;
		serializedObject.FindProperty("overheatSeconds").floatValue = 1.2f;
		serializedObject.FindProperty("normalOpenSeconds").floatValue = 8f;
		serializedObject.FindProperty("staggerOpenSeconds").floatValue = 6f;
		serializedObject.FindProperty("overheatHazardDamage").floatValue = 25f;
		serializedObject.FindProperty("overheatHazardRadius").floatValue = 2.6f;
		SetNavigation(serializedObject.FindProperty("navigation"), 1.8f);
		SerializedProperty serializedProperty = serializedObject.FindProperty("attacks");
		serializedProperty.arraySize = 3;
		SetAttack(serializedProperty, 0, CoreGuardianAttackType.HammerSlam, 34f, 0.75f, 2.2f);
		SetAttack(serializedProperty, 1, CoreGuardianAttackType.FurnaceSweep, 40f, 0.95f, 3.4f);
		SetAttack(serializedProperty, 2, CoreGuardianAttackType.EmberVent, 28f, 0.8f, 2.5f);
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(coreGuardianData);
		return coreGuardianData;
	}

	/// <summary>
	/// Nur der Kernwaechter — der Volllauf Build() fasst alle Prefabs und
	/// Definitionen an und ersetzt deren Visuals destruktiv.
	/// </summary>
	[MenuItem("Eidren/Data/Build Eidra Forge Core Guardian")]
	public static void BuildCore()
	{
		BuildCorePrefab(BuildCoreData());
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private static void BuildCorePrefab(CoreGuardianData data)
	{
		// Patch statt Neuaufbau: Der Stilumbau hat am Bestand Animator-,
		// Schatten- und VFX-Komponenten ergaenzt, die nur im Prefab leben —
		// ein from-scratch-Bau wuerfe sie ab (dokumentierte Rebuild-Falle).
		bool existed = File.Exists(CorePrefabPath);
		GameObject root = existed
			? PrefabUtility.LoadPrefabContents(CorePrefabPath)
			: new GameObject("CoreGuardian");
		try
		{
			Ensure<NavMeshAgent>(root);
			Ensure<Damageable>(root);
			CapsuleCollider capsuleCollider = Ensure<CapsuleCollider>(root);
			capsuleCollider.radius = 1.1f;
			capsuleCollider.height = 3.6f;
			capsuleCollider.center = Vector3.up * 1.8f;
			Ensure<CoreGuardianController>(root);
			if (root.transform.Find("Visual") == null)
			{
				GameObject visualObject = new GameObject("Visual");
				visualObject.transform.SetParent(root.transform, worldPositionStays: false);
				ReplaceVisual(visualObject.transform, "CoreGuardian");
			}
			if (root.transform.Find("StatusBars") == null)
			{
				AddCoreStatusBars(root, capsuleCollider);
			}
			PrefabUtility.SaveAsPrefabAsset(root, CorePrefabPath);
		}
		finally
		{
			if (existed)
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
			else
			{
				Object.DestroyImmediate(root);
			}
		}
	}

	private static T Ensure<T>(GameObject root) where T : Component
	{
		T component = root.GetComponent<T>();
		return (component != null) ? component : root.AddComponent<T>();
	}

	/// <summary>
	/// #34: Auch der Kernwaechter traegt die Gegner-Statusleiste. Ohne sie
	/// wirkten Treffer gegen 3000 HP folgenlos und der Schutz von 0,35 las
	/// sich als Unverwundbarkeit. Der Aufbau lebt hier im Builder, damit ihn
	/// der naechste Content-Rebuild nicht wieder abwirft.
	/// </summary>
	private static void AddCoreStatusBars(GameObject root, CapsuleCollider body)
	{
		GameObject barsObject = new GameObject("StatusBars", typeof(RectTransform), typeof(Canvas));
		barsObject.transform.SetParent(root.transform, worldPositionStays: false);
		float kopfhoehe = body.center.y + body.height * 0.5f;
		barsObject.transform.localPosition = new Vector3(0f, kopfhoehe + 0.45f, 0f);
		barsObject.transform.localScale = Vector3.one * 0.01f;
		Canvas canvas = barsObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.WorldSpace;
		canvas.sortingOrder = 16;
		RectTransform canvasRect = (RectTransform)barsObject.transform;
		canvasRect.sizeDelta = new Vector2(150f, 24f);
		Image health = BarImage(BarImage(canvasRect, "HealthBack", new Vector2(0f, 6f), new Color(0.12f, 0.08f, 0.07f, 0.92f)).rectTransform, "HealthFill", Vector2.zero, new Color(0.24f, 0.82f, 0.38f, 1f));
		Image staggerBack = BarImage(canvasRect, "StaggerBack", new Vector2(0f, -7f), new Color(0.12f, 0.08f, 0.07f, 0.92f));
		staggerBack.rectTransform.sizeDelta = new Vector2(150f, 7f);
		Image stagger = BarImage(staggerBack.rectTransform, "StaggerFill", Vector2.zero, new Color(0.97f, 0.61f, 0.17f, 1f));
		stagger.rectTransform.sizeDelta = Vector2.zero;
		Text protection = CoreLabel(canvasRect, "ProtectionLabel", 12, TextAnchor.MiddleRight,
			new Color(0.78f, 0.86f, 0.94f), new Vector2(110f, 20f), new Vector2(82f, 20f));
		Text name = CoreLabel(canvasRect, "NameLabel", 13, TextAnchor.MiddleCenter,
			new Color(0.96f, 0.94f, 0.88f), new Vector2(220f, 18f), new Vector2(0f, 12f));
		name.fontStyle = FontStyle.Bold;
		RectTransform nameRect = name.rectTransform;
		nameRect.pivot = new Vector2(0.5f, 0f);
		root.AddComponent<WildlingStatusBars>().ConfigureReferences(canvas, health, stagger, protection, name);
	}

	private static Image BarImage(RectTransform parent, string objectName, Vector2 position, Color color)
	{
		GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		imageObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform rect = (RectTransform)imageObject.transform;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = new Vector2(150f, 10f);
		Image image = imageObject.GetComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
		if (objectName.EndsWith("Fill", StringComparison.Ordinal))
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			image.type = Image.Type.Filled;
			image.fillMethod = Image.FillMethod.Horizontal;
			image.fillOrigin = 0;
			// Ohne Fuellgrafik ignoriert Unity den Fuellwert und zeichnet voll.
			image.sprite = FixCollectionHudBuilder.EnsureBarSprite();
			// Der Staggerbalken beginnt leer und fuellt sich erst durch Treffer.
			image.fillAmount = (objectName.StartsWith("Stagger", StringComparison.Ordinal) ? 0f : 1f);
		}
		return image;
	}

	private static Text CoreLabel(RectTransform parent, string objectName, int fontSize, TextAnchor alignment, Color color, Vector2 size, Vector2 position)
	{
		GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
		labelObject.transform.SetParent(parent, worldPositionStays: false);
		Text label = labelObject.GetComponent<Text>();
		label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		label.fontSize = fontSize;
		label.alignment = alignment;
		label.color = color;
		label.horizontalOverflow = HorizontalWrapMode.Overflow;
		label.raycastTarget = false;
		Outline outline = labelObject.GetComponent<Outline>();
		outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
		outline.effectDistance = new Vector2(1f, -1f);
		RectTransform rect = label.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.sizeDelta = size;
		rect.anchoredPosition = position;
		return label;
	}

	private static void ReplaceVisual(Transform visual, string stem)
	{
		if (!(visual == null))
		{
			for (int index = visual.childCount - 1; index >= 0; index--)
			{
				Object.DestroyImmediate(visual.GetChild(index).gameObject);
			}
			// Stilumbau (10.08.2026): Die Actor-Visuals liegen seither unter
			// Prefabs/Actors/3D — der alte Resources-Pfad existiert nicht mehr.
			GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Actors/3D/" + stem + "_3D.prefab");
			if (gameObject == null)
			{
				throw new FileNotFoundException(stem + " visual");
			}
			((GameObject)PrefabUtility.InstantiatePrefab(gameObject)).transform.SetParent(visual, worldPositionStays: false);
		}
	}

	private static void SetAttack(SerializedProperty array, int index, CoreGuardianAttackType type, float damage, float telegraph, float radius)
	{
		SerializedProperty arrayElementAtIndex = array.GetArrayElementAtIndex(index);
		arrayElementAtIndex.FindPropertyRelative("type").enumValueIndex = (int)type;
		arrayElementAtIndex.FindPropertyRelative("damage").floatValue = damage;
		arrayElementAtIndex.FindPropertyRelative("telegraphSeconds").floatValue = telegraph;
		arrayElementAtIndex.FindPropertyRelative("radius").floatValue = radius;
	}

	private static void SetNavigation(SerializedProperty nav, float scale)
	{
		nav.FindPropertyRelative("MoveSpeed").floatValue = 2.8f;
		nav.FindPropertyRelative("Acceleration").floatValue = 14f;
		nav.FindPropertyRelative("AngularSpeed").floatValue = 360f;
		nav.FindPropertyRelative("AttackRange").floatValue = 1.7f * scale;
		nav.FindPropertyRelative("DetectionRange").floatValue = 10f;
		nav.FindPropertyRelative("LeashRange").floatValue = 18f;
		nav.FindPropertyRelative("LeashFollowDistance").floatValue = 3f;
		nav.FindPropertyRelative("PatrolRadius").floatValue = 2.5f;
		nav.FindPropertyRelative("PatrolWait").floatValue = 1.1f;
		nav.FindPropertyRelative("AlertDuration").floatValue = 0.3f;
		nav.FindPropertyRelative("ReturnTolerance").floatValue = 0.55f;
		nav.FindPropertyRelative("NavMeshSampleDistance").floatValue = 4f;
		nav.FindPropertyRelative("AgentRadius").floatValue = 0.48f * scale;
		nav.FindPropertyRelative("AgentHeight").floatValue = 2f * scale;
	}

	private static T LoadOrCreate<T>(string path) where T : ScriptableObject
	{
		T value = AssetDatabase.LoadAssetAtPath<T>(path);
		if (value != null)
		{
			return value;
		}
		value = ScriptableObject.CreateInstance<T>();
		AssetDatabase.CreateAsset(value, path);
		return value;
	}

	private static void EnsureFolders()
	{
		Folder("Assets/_Game/Data/Enemies", "Forge");
		Folder("Assets/_Game/Data", "Bosses");
		Folder("Assets/_Game/Prefabs/Enemies", "Forge");
	}

	private static void Folder(string parent, string child)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + child))
		{
			AssetDatabase.CreateFolder(parent, child);
		}
	}
}
}
