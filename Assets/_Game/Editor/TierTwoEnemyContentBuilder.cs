using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using Eidren.UI;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class TierTwoEnemyContentBuilder
{
	private readonly struct Spec
	{
		public readonly string Stem;

		public readonly string Id;

		public readonly string DisplayName;

		public readonly EnemyCombatStyle Style;

		public readonly float Health;

		public readonly float Stagger;

		public readonly float Damage;

		public readonly float Secondary;

		public readonly float Protection;

		public readonly float StaggerDuration;

		public readonly float Speed;

		public readonly float AttackRange;

		public readonly float Telegraph;

		public readonly float AttackWindow;

		public readonly float Recovery;

		public readonly float Scale;

		public readonly int Xp;

		public readonly Color Tint;

		public Spec(string stem, string id, string displayName, EnemyCombatStyle style, float health, float stagger, float damage, float secondary, float protection, int xp, float staggerDuration, float speed, float attackRange, float telegraph, float attackWindow, float recovery, Color tint, float scale)
		{
			Stem = stem;
			Id = id;
			DisplayName = displayName;
			Style = style;
			Health = health;
			Stagger = stagger;
			Damage = damage;
			Secondary = secondary;
			Protection = protection;
			Xp = xp;
			StaggerDuration = staggerDuration;
			Speed = speed;
			AttackRange = attackRange;
			Telegraph = telegraph;
			AttackWindow = attackWindow;
			Recovery = recovery;
			Tint = tint;
			Scale = scale;
		}
	}

	private const string DataRoot = "Assets/_Game/Data/Enemies";

	private const string PrefabRoot = "Assets/_Game/Prefabs/Enemies/TierTwo";

	private const string MaterialRoot = "Assets/_Game/Art/Enemies/TierTwo/Materials";

	[MenuItem("Eidren/Data/Build Tier Two Enemies")]
	public static void Build()
	{
		EnsureFolders();
		GameObject projectile = BuildProjectile();
		Dictionary<string, EnemyDefinition> definitions = new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal);
		foreach (Spec spec in Specs())
		{
			EnemyDefinition definition = BuildDefinition(spec);
			GameObject prefab = BuildPrefab(spec, definition, projectile);
			SerializedObject serializedObject = new SerializedObject(definition);
			serializedObject.FindProperty("prefab").objectReferenceValue = prefab;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(definition);
			definitions.Add(spec.Id, definition);
		}
		ConfigureZones(definitions);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: five fixed-stat T2 enemies built.");
	}

	private static IEnumerable<Spec> Specs()
	{
		yield return new Spec("Riftling", "enemy.riftling", "Rissling", EnemyCombatStyle.Melee, 220f, 80f, 20f, 0f, 0f, 25, 2.3f, 4.7f, 1.65f, 0.38f, 0.16f, 0.55f, new Color(0.62f, 0.78f, 0.9f), 0.88f);
		yield return new Spec("RootCharger", "enemy.root_charger", "Wurzelstürmer", EnemyCombatStyle.Charge, 380f, 140f, 32f, 0f, 0f, 45, 2.7f, 3.25f, 4.8f, 0.95f, 0.62f, 1.05f, new Color(0.45f, 0.68f, 0.32f), 1.18f);
		yield return new Spec("MoorThrower", "enemy.moor_thrower", "Moorwerfer", EnemyCombatStyle.Projectile, 200f, 70f, 24f, 0f, 0f, 30, 2.2f, 3.15f, 7.5f, 0.72f, 0.9f, 1.15f, new Color(0.44f, 0.82f, 0.7f), 0.96f);
		yield return new Spec("GraniteShell", "enemy.granite_shell", "Granitpanzer", EnemyCombatStyle.ArmoredMelee, 450f, 180f, 30f, 0f, 0.2f, 65, 3f, 2.35f, 1.85f, 0.78f, 0.22f, 1.25f, new Color(0.62f, 0.63f, 0.67f), 1.3f);
		yield return new Spec("RiftGuardian", "enemy.rift_guardian", "Risswächter", EnemyCombatStyle.AreaElite, 850f, 260f, 30f, 40f, 0.15f, 180, 3.5f, 2.75f, 2.2f, 0.92f, 0.34f, 1.1f, new Color(0.82f, 0.48f, 0.88f), 1.55f);
	}

	private static EnemyDefinition BuildDefinition(Spec spec)
	{
		string path = "Assets/_Game/Data/Enemies/" + spec.Stem + ".asset";
		EnemyDefinition value = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
		if (value == null)
		{
			value = ScriptableObject.CreateInstance<EnemyDefinition>();
			value.name = spec.Stem;
			AssetDatabase.CreateAsset(value, path);
		}
		SerializedObject serializedObject = new SerializedObject(value);
		Set(serializedObject, "id", spec.Id);
		Set(serializedObject, "displayName", spec.DisplayName);
		serializedObject.FindProperty("maximumHealth").floatValue = spec.Health;
		serializedObject.FindProperty("maximumStagger").floatValue = spec.Stagger;
		serializedObject.FindProperty("staggerDuration").floatValue = spec.StaggerDuration;
		serializedObject.FindProperty("attackDamage").floatValue = spec.Damage;
		serializedObject.FindProperty("secondaryAttackDamage").floatValue = spec.Secondary;
		serializedObject.FindProperty("protection").floatValue = spec.Protection;
		serializedObject.FindProperty("combatStyle").enumValueIndex = (int)spec.Style;
		serializedObject.FindProperty("postStaggerResistanceDuration").floatValue = ((spec.Id == "enemy.rift_guardian") ? 3f : 0f);
		serializedObject.FindProperty("postStaggerDamageMultiplier").floatValue = ((spec.Id == "enemy.rift_guardian") ? 0.5f : 1f);
		serializedObject.FindProperty("telegraphDuration").floatValue = spec.Telegraph;
		serializedObject.FindProperty("attackWindowDuration").floatValue = spec.AttackWindow;
		serializedObject.FindProperty("recoveryDuration").floatValue = spec.Recovery;
		serializedObject.FindProperty("deathDisableDelay").floatValue = 2.5f;
		serializedObject.FindProperty("experienceReward").intValue = spec.Xp;
		serializedObject.FindProperty("lootTable").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LootTableDefinition>("Assets/_Game/Data/Loot/WildlingLoot.asset");
		SerializedProperty serializedProperty = serializedObject.FindProperty("navigation");
		serializedProperty.FindPropertyRelative("MoveSpeed").floatValue = spec.Speed;
		serializedProperty.FindPropertyRelative("Acceleration").floatValue = 14f;
		serializedProperty.FindPropertyRelative("AngularSpeed").floatValue = 420f;
		serializedProperty.FindPropertyRelative("AttackRange").floatValue = spec.AttackRange;
		serializedProperty.FindPropertyRelative("DetectionRange").floatValue = 10f;
		serializedProperty.FindPropertyRelative("LeashRange").floatValue = 18f;
		serializedProperty.FindPropertyRelative("LeashFollowDistance").floatValue = 3f;
		serializedProperty.FindPropertyRelative("PatrolRadius").floatValue = 3.5f;
		serializedProperty.FindPropertyRelative("PatrolWait").floatValue = 1.2f;
		serializedProperty.FindPropertyRelative("AlertDuration").floatValue = 0.3f;
		serializedProperty.FindPropertyRelative("ReturnTolerance").floatValue = 0.55f;
		serializedProperty.FindPropertyRelative("NavMeshSampleDistance").floatValue = 4f;
		serializedProperty.FindPropertyRelative("AgentRadius").floatValue = 0.52f * spec.Scale;
		serializedProperty.FindPropertyRelative("AgentHeight").floatValue = 2.1f * spec.Scale;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(value);
		return value;
	}

	private static GameObject BuildPrefab(Spec spec, EnemyDefinition definition, GameObject projectile)
	{
		string source = "Assets/_Game/Prefabs/Enemies/Wildling.prefab";
		string path = "Assets/_Game/Prefabs/Enemies/TierTwo/" + spec.Stem + ".prefab";
		if (!File.Exists(path) && !AssetDatabase.CopyAsset(source, path))
		{
			throw new IOException("Could not create enemy prefab '" + path + "'.");
		}
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		try
		{
			root.name = spec.Stem;
			SerializedObject serializedObject = new SerializedObject(root.GetComponent<WildlingController>());
			serializedObject.FindProperty("definition").objectReferenceValue = definition;
			serializedObject.FindProperty("projectileVisualPrefab").objectReferenceValue = ((spec.Style == EnemyCombatStyle.Projectile) ? projectile : null);
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			ReplaceVisual(root, spec.Stem);
			CapsuleCollider body = root.GetComponent<CapsuleCollider>();
			body.radius = 0.52f * spec.Scale;
			body.height = 2.1f * spec.Scale;
			body.center = Vector3.up * body.height * 0.5f;
			AddProtectionLabel(root);
			PrefabUtility.SaveAsPrefabAsset(root, path);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
		return AssetDatabase.LoadAssetAtPath<GameObject>(path);
	}

	private static void ReplaceVisual(GameObject root, string stem)
	{
		Transform transform = root.transform.Find("Visual");
		if (transform == null)
		{
			throw new InvalidOperationException("Enemy visual root is missing.");
		}
		UnityEngine.Object.DestroyImmediate(transform.gameObject);
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(V02ActorVisualBuilder.PrefabPath(stem, forge: false));
		if (gameObject == null)
		{
			throw new FileNotFoundException(stem + " visual");
		}
		GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(gameObject);
		instance.name = "Visual";
		instance.transform.SetParent(root.transform, worldPositionStays: false);
		instance.transform.localScale = Vector3.one;
		WildlingController component = root.GetComponent<WildlingController>();
		component.ConfigurePrefab(configuredHitbox: root.GetComponent<EnemyMeleeHitbox>(), configuredTelegraph: root.transform.Find("AttackTelegraph")?.gameObject, configuredDefinition: component.Definition, configuredVisualRoot: instance.transform);
	}

	private static void AddProtectionLabel(GameObject root)
	{
		WildlingStatusBars bars = root.GetComponent<WildlingStatusBars>();
		if (!(bars == null) && !(bars.WorldCanvas == null))
		{
			Transform existing = bars.WorldCanvas.transform.Find("ProtectionLabel");
			Text label;
			if (existing == null)
			{
				GameObject gameObject = new GameObject("ProtectionLabel", typeof(RectTransform), typeof(Text));
				gameObject.transform.SetParent(bars.WorldCanvas.transform, worldPositionStays: false);
				label = gameObject.GetComponent<Text>();
			}
			else
			{
				label = existing.GetComponent<Text>();
			}
			label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			label.fontSize = 12;
			label.alignment = TextAnchor.MiddleRight;
			label.color = new Color(0.78f, 0.86f, 0.94f);
			RectTransform rectTransform = label.rectTransform;
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.sizeDelta = new Vector2(110f, 20f);
			rectTransform.anchoredPosition = new Vector2(82f, 20f);
			bars.ConfigureReferences(bars.WorldCanvas, bars.HealthFill, bars.StaggerFill, label);
		}
	}

	private static GameObject BuildProjectile()
	{
		string path = "Assets/_Game/Prefabs/Enemies/TierTwo/MoorProjectile.prefab";
		Material material = Material("MoorProjectile", new Color(0.3f, 0.92f, 0.72f));
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		gameObject.name = "MoorProjectile";
		gameObject.transform.localScale = Vector3.one * 0.34f;
		UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
		gameObject.GetComponent<Renderer>().sharedMaterial = material;
		GameObject result = PrefabUtility.SaveAsPrefabAsset(gameObject, path);
		UnityEngine.Object.DestroyImmediate(gameObject);
		return result;
	}

	private static void ConfigureZones(IReadOnlyDictionary<string, EnemyDefinition> values)
	{
		// F31-009: Die Besetzung schreibt zentral der
		// ZoneEnemyPopulationBuilder — die frühere Tabelle hier galt nur für
		// die drei Tier-2-Zonen und kollidierte mit den anderen Schreibern.
		ZoneEnemyPopulationBuilder.Apply();
	}

	private static Material Material(string name, Color color)
	{
		string path = "Assets/_Game/Art/Enemies/TierTwo/Materials/" + name + ".mat";
		Material value = AssetDatabase.LoadAssetAtPath<Material>(path);
		Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
		if (value == null)
		{
			value = new Material(shader)
			{
				name = name
			};
			AssetDatabase.CreateAsset(value, path);
		}
		value.color = color;
		if (value.HasProperty("_EmissionColor"))
		{
			value.SetColor("_EmissionColor", color * 0.35f);
		}
		EditorUtility.SetDirty(value);
		return value;
	}

	private static void Set(SerializedObject value, string property, string text)
	{
		value.FindProperty(property).stringValue = text;
	}

	private static void EnsureFolders()
	{
		Folder("Assets/_Game/Prefabs/Enemies", "TierTwo");
		Folder("Assets/_Game/Art/Enemies", "TierTwo");
		Folder("Assets/_Game/Art/Enemies/TierTwo", "Materials");
	}

	private static void Folder(string parent, string name)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + name))
		{
			AssetDatabase.CreateFolder(parent, name);
		}
	}
}
}
