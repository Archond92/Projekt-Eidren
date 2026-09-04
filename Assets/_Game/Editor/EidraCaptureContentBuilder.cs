using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.UI;
using System.IO;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class EidraCaptureContentBuilder
{
	public const string CaptureRulesPath = "Assets/_Game/Resources/Data/CaptureRules_V01.asset";

	public const string PrefabPath = "Assets/_Game/Prefabs/Enemies/WildEidra.prefab";

	public const string TerrockEnemyPath = "Assets/_Game/Data/Enemies/Enemy_Terrock.asset";

	public const string NoctarionEnemyPath = "Assets/_Game/Data/Enemies/Enemy_Noctarion.asset";

	private const string MaterialFolder = "Assets/_Game/Art/Enemies/Materials";

	private const float TierOneBatteryCharge = 100f;

	[MenuItem("Eidren/Data/Build Eidra Capture V0.1")]
	public static void BuildEidraCapture()
	{
		EnsureFolders();
		BuildEidraCaptureData();
		BuildCaptureRules();
		GameObject prefab = BuildPrefab();
		EnemyDefinition terrock = BuildEnemy("Assets/_Game/Data/Enemies/Enemy_Terrock.asset", "Enemy_Terrock", "enemy.eidra.terrock", "Terrock", "Assets/_Game/Data/Eidren/Terrock.asset", prefab, 150f, 80f, 14f);
		EnemyDefinition noctarion = BuildEnemy("Assets/_Game/Data/Enemies/Enemy_Noctarion.asset", "Enemy_Noctarion", "enemy.eidra.noctarion", "Noctarion", "Assets/_Game/Data/Eidren/Noctarion.asset", prefab, 130f, 70f, 16f);
		UpsertZoneAllocation("Zone_Quarry", terrock, 2);
		UpsertZoneAllocation("Zone_Marsh", noctarion, 2);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: built eidra capture content -- capture values, battery charges, wild eidra prefab and zone allocations.");
	}

	public static void RebuildPrefabForFixes()
	{
		EnsureFolders();
		BuildPrefab();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private static void BuildEidraCaptureData()
	{
		SetCapture("Assets/_Game/Data/Eidren/Terrock.asset", 140f, 60f, 0.15f);
		SetCapture("Assets/_Game/Data/Eidren/Noctarion.asset", 150f, 65f, 0.15f);
	}

	private static void SetCapture(string path, float requirementAtFullHealth, float minimumRequirement, float fleeHealthFraction)
	{
		EidraData eidra = AssetDatabase.LoadAssetAtPath<EidraData>(path);
		if (eidra == null)
		{
			throw new FileNotFoundException(path);
		}
		SerializedObject serializedObject = new SerializedObject(eidra);
		SerializedProperty serializedProperty = serializedObject.FindProperty("capture");
		serializedProperty.FindPropertyRelative("requirementAtFullHealth").floatValue = requirementAtFullHealth;
		serializedProperty.FindPropertyRelative("minimumRequirement").floatValue = minimumRequirement;
		serializedProperty.FindPropertyRelative("fleeHealthFraction").floatValue = fleeHealthFraction;
		serializedObject.FindProperty("productionRole").enumValueIndex = 0;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(eidra);
		string[] errors = eidra.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException("Eidra '" + eidra.name + "' is invalid: " + string.Join("; ", errors));
		}
	}

	private static void BuildCaptureRules()
	{
		CaptureRulesDefinition rules = AssetDatabase.LoadAssetAtPath<CaptureRulesDefinition>("Assets/_Game/Resources/Data/CaptureRules_V01.asset");
		if (rules == null)
		{
			rules = ScriptableObject.CreateInstance<CaptureRulesDefinition>();
			rules.name = "CaptureRules_V01";
			AssetDatabase.CreateAsset(rules, "Assets/_Game/Resources/Data/CaptureRules_V01.asset");
		}
		SerializedObject serializedObject = new SerializedObject(rules);
		SerializedProperty serializedProperty = serializedObject.FindProperty("batteryCharges");
		serializedProperty.arraySize = 1;
		SerializedProperty arrayElementAtIndex = serializedProperty.GetArrayElementAtIndex(0);
		arrayElementAtIndex.FindPropertyRelative("tier").intValue = 1;
		arrayElementAtIndex.FindPropertyRelative("charge").floatValue = 100f;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(rules);
		string[] errors = rules.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException("Capture rules are invalid: " + string.Join("; ", errors));
		}
	}

	private static EnemyDefinition BuildEnemy(string path, string assetName, string id, string displayName, string eidraPath, GameObject prefab, float maximumHealth, float maximumStagger, float attackDamage)
	{
		EidraData eidra = AssetDatabase.LoadAssetAtPath<EidraData>(eidraPath);
		if (eidra == null)
		{
			throw new FileNotFoundException(eidraPath);
		}
		EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
		if (definition == null)
		{
			definition = ScriptableObject.CreateInstance<EnemyDefinition>();
			definition.name = assetName;
			AssetDatabase.CreateAsset(definition, path);
		}
		SerializedObject serializedObject = new SerializedObject(definition);
		serializedObject.FindProperty("id").stringValue = id;
		serializedObject.FindProperty("displayName").stringValue = displayName;
		serializedObject.FindProperty("maximumHealth").floatValue = maximumHealth;
		serializedObject.FindProperty("maximumStagger").floatValue = maximumStagger;
		serializedObject.FindProperty("staggerDuration").floatValue = 2.2f;
		serializedObject.FindProperty("attackDamage").floatValue = attackDamage;
		serializedObject.FindProperty("telegraphDuration").floatValue = 0.6f;
		serializedObject.FindProperty("attackWindowDuration").floatValue = 0.18f;
		serializedObject.FindProperty("recoveryDuration").floatValue = 0.9f;
		serializedObject.FindProperty("deathDisableDelay").floatValue = 0f;
		serializedObject.FindProperty("lootTable").objectReferenceValue = null;
		serializedObject.FindProperty("capturableEidra").objectReferenceValue = eidra;
		serializedObject.FindProperty("prefab").objectReferenceValue = prefab;
		SerializedProperty serializedProperty = serializedObject.FindProperty("navigation");
		serializedProperty.FindPropertyRelative("MoveSpeed").floatValue = 3.2f;
		serializedProperty.FindPropertyRelative("Acceleration").floatValue = 12f;
		serializedProperty.FindPropertyRelative("AngularSpeed").floatValue = 340f;
		serializedProperty.FindPropertyRelative("AttackRange").floatValue = 1.8f;
		serializedProperty.FindPropertyRelative("DetectionRange").floatValue = 7f;
		serializedProperty.FindPropertyRelative("LeashRange").floatValue = 12f;
		serializedProperty.FindPropertyRelative("LeashFollowDistance").floatValue = 2f;
		serializedProperty.FindPropertyRelative("PatrolRadius").floatValue = 2.5f;
		serializedProperty.FindPropertyRelative("PatrolWait").floatValue = 1.6f;
		serializedProperty.FindPropertyRelative("AlertDuration").floatValue = 0.3f;
		serializedProperty.FindPropertyRelative("ReturnTolerance").floatValue = 0.55f;
		serializedProperty.FindPropertyRelative("NavMeshSampleDistance").floatValue = 4f;
		serializedProperty.FindPropertyRelative("AgentRadius").floatValue = 0.5f;
		serializedProperty.FindPropertyRelative("AgentHeight").floatValue = 2f;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(definition);
		string[] errors = definition.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException("Enemy '" + assetName + "' is invalid: " + string.Join("; ", errors));
		}
		return definition;
	}

	private static GameObject BuildPrefab()
	{
		Material telegraph = Material("EN_WildEidraTelegraph", new Color(0.95f, 0.62f, 0.18f, 0.6f));
		GameObject root = new GameObject("WildEidra");
		CapsuleCollider capsuleCollider = root.AddComponent<CapsuleCollider>();
		capsuleCollider.radius = 0.5f;
		capsuleCollider.height = 2f;
		capsuleCollider.center = Vector3.up;
		GameObject visual = new GameObject("Visual");
		visual.name = "Visual";
		visual.transform.SetParent(root.transform, worldPositionStays: false);
		visual.transform.localPosition = Vector3.zero;
		visual.transform.localScale = Vector3.one;
		GameObject gameObject = new GameObject("AttackHitbox");
		gameObject.transform.SetParent(root.transform, worldPositionStays: false);
		gameObject.transform.localPosition = new Vector3(0f, 0.95f, 1f);
		BoxCollider attackCollider = gameObject.AddComponent<BoxCollider>();
		attackCollider.size = new Vector3(1.4f, 1.5f, 1.4f);
		attackCollider.isTrigger = true;
		attackCollider.enabled = false;
		EnemyMeleeHitbox hitbox = root.AddComponent<EnemyMeleeHitbox>();
		hitbox.Configure(attackCollider);
		GameObject telegraphObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		telegraphObject.name = "AttackTelegraph";
		telegraphObject.transform.SetParent(root.transform, worldPositionStays: false);
		telegraphObject.transform.localPosition = new Vector3(0f, 0.06f, 0.85f);
		telegraphObject.transform.localScale = new Vector3(1.4f, 0.04f, 1.6f);
		UnityEngine.Object.DestroyImmediate(telegraphObject.GetComponent<Collider>());
		telegraphObject.GetComponent<Renderer>().sharedMaterial = telegraph;
		telegraphObject.SetActive(value: false);
		EidraWildController controller = root.AddComponent<EidraWildController>();
		EidraCaptureTarget obj = root.AddComponent<EidraCaptureTarget>();
		SerializedObject serializedObject = new SerializedObject(controller);
		serializedObject.FindProperty("attackHitbox").objectReferenceValue = hitbox;
		serializedObject.FindProperty("attackTelegraph").objectReferenceValue = telegraphObject;
		serializedObject.FindProperty("visualRenderer").objectReferenceValue = null;
		serializedObject.FindProperty("visualRoot").objectReferenceValue = visual.transform;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		SerializedObject serializedObject2 = new SerializedObject(obj);
		serializedObject2.FindProperty("controller").objectReferenceValue = controller;
		serializedObject2.ApplyModifiedPropertiesWithoutUndo();
		AddStatusBars(root);
		GameObject result = PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/Enemies/WildEidra.prefab");
		UnityEngine.Object.DestroyImmediate(root);
		return result;
	}

	private static void AddStatusBars(GameObject root)
	{
		GameObject gameObject = new GameObject("StatusBars", typeof(RectTransform), typeof(Canvas));
		gameObject.transform.SetParent(root.transform, worldPositionStays: false);
		gameObject.transform.localPosition = new Vector3(0f, 2.35f, 0f);
		gameObject.transform.localScale = Vector3.one * 0.01f;
		Canvas canvas = gameObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.WorldSpace;
		canvas.sortingOrder = 16;
		RectTransform obj = (RectTransform)gameObject.transform;
		obj.sizeDelta = new Vector2(150f, 24f);
		Image health = BarImage(BarImage(obj, "HealthBack", new Vector2(0f, 6f), new Color(0.12f, 0.08f, 0.07f, 0.92f)).rectTransform, "HealthFill", Vector2.zero, new Color(0.24f, 0.82f, 0.38f, 1f));
		Image image = BarImage(obj, "StaggerBack", new Vector2(0f, -7f), new Color(0.12f, 0.08f, 0.07f, 0.92f));
		image.rectTransform.sizeDelta = new Vector2(150f, 7f);
		Image stagger = BarImage(image.rectTransform, "StaggerFill", Vector2.zero, new Color(0.97f, 0.61f, 0.17f, 1f));
		stagger.rectTransform.sizeDelta = Vector2.zero;
		root.AddComponent<WildlingStatusBars>().ConfigureReferences(canvas, health, stagger);
	}

	private static Image BarImage(RectTransform parent, string objectName, Vector2 position, Color color)
	{
		GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform rect = (RectTransform)gameObject.transform;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = new Vector2(150f, 10f);
		Image image = gameObject.GetComponent<Image>();
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
			// Ohne Fuellgrafik ignoriert Unity den Fuellwert und zeichnet voll (F-002).
			image.sprite = FixCollectionHudBuilder.EnsureBarSprite();
			// Der Staggerbalken beginnt leer und fuellt sich erst durch Treffer.
			image.fillAmount = (objectName.StartsWith("Stagger", StringComparison.Ordinal) ? 0f : 1f);
		}
		return image;
	}

	private static void UpsertZoneAllocation(string assetName, EnemyDefinition definition, int count)
	{
		string path = "Assets/_Game/Data/Zones/" + assetName + ".asset";
		ZoneDefinition zoneDefinition = AssetDatabase.LoadAssetAtPath<ZoneDefinition>(path);
		if (zoneDefinition == null)
		{
			throw new FileNotFoundException(path);
		}
		SerializedObject serializedObject = new SerializedObject(zoneDefinition);
		SerializedProperty allocations = serializedObject.FindProperty("enemyAllocations");
		int index = IndexOf(allocations, definition);
		if (index < 0)
		{
			index = allocations.arraySize;
			allocations.InsertArrayElementAtIndex(index);
		}
		SerializedProperty arrayElementAtIndex = allocations.GetArrayElementAtIndex(index);
		arrayElementAtIndex.FindPropertyRelative("definition").objectReferenceValue = definition;
		arrayElementAtIndex.FindPropertyRelative("count").intValue = count;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(zoneDefinition);
	}

	private static int IndexOf(SerializedProperty allocations, EnemyDefinition definition)
	{
		for (int index = 0; index < allocations.arraySize; index++)
		{
			if (allocations.GetArrayElementAtIndex(index).FindPropertyRelative("definition").objectReferenceValue == definition)
			{
				return index;
			}
		}
		return -1;
	}

	private static Material Material(string name, Color color)
	{
		string path = "Assets/_Game/Art/Enemies/Materials/" + name + ".mat";
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

	private static void EnsureFolders()
	{
		Folder("Assets/_Game/Data", "Enemies");
		Folder("Assets/_Game/Art", "Enemies");
		Folder("Assets/_Game/Art/Enemies", "Materials");
		Folder("Assets/_Game/Prefabs", "Enemies");
		Folder("Assets/_Game/Resources", "Data");
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
