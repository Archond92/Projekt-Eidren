using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class WildlingContentBuilder
{
	public const string DefinitionPath = "Assets/_Game/Data/Enemies/Wildling.asset";

	public const string PrefabPath = "Assets/_Game/Prefabs/Enemies/Wildling.prefab";

	private const string VisualPath = "Assets/_Game/Prefabs/Actors/2D/Wildling_2D.prefab";

	private const string MaterialFolder = "Assets/_Game/Art/Enemies/Materials";

	[MenuItem("Eidren/Data/Build Wildling V0.1")]
	public static void BuildWildling()
	{
		EnsureFolders();
		Material telegraph = Material("EN_WildlingTelegraph", new Color(0.95f, 0.22f, 0.12f, 0.62f));
		GameObject visual = LoadVisual();
		EnemyDefinition definition = BuildDefinition();
		BuildPrefab(definition, visual, telegraph);
		ConfigureZoneAllocations(definition);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		EidrenSceneStructureBuilder.BuildMapExitScenes();
		Debug.Log("Eidren: built Wildling V0.1 and populated all configured outdoor zones.");
	}

	/// <summary>
	/// F31-009: Baut das fehlende Wildling-Prefab für den Laufzeit-Populator.
	/// Der Wildling war der einzige Gegner ohne eigenes Prefab — seine
	/// Instanzen lagen ausgerollt in den Szenen (WildlingSceneRewire), und
	/// der alte 2D-Baupfad dieses Builders bricht seit jeher am fehlenden
	/// Wildling_2D.prefab ab. Quelle ist deshalb das Riftling-Prefab: eine
	/// Kopie des einstigen Wildling-Prefabs mit voller Ausstattung
	/// (Statusbalken, Hitbox, Telegraph) und fertigem 3D-Einbau, den
	/// CreatureEnemyRewire anschließend auf Wildling_3D umstellt.
	/// </summary>
	[MenuItem("Eidren/Data/Rebuild Wildling Prefab (3D)")]
	public static void RebuildPrefab()
	{
		EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/Wildling.asset");
		if (definition == null)
		{
			throw new FileNotFoundException("Wildling definition is missing.");
		}
		if (!File.Exists(PrefabPath) && !AssetDatabase.CopyAsset("Assets/_Game/Prefabs/Enemies/TierTwo/Riftling.prefab", PrefabPath))
		{
			throw new IOException("Could not create '" + PrefabPath + "' from the Riftling template.");
		}
		GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
		try
		{
			root.name = "Wildling";
			SerializedObject controller = new SerializedObject(root.GetComponent<WildlingController>());
			controller.FindProperty("definition").objectReferenceValue = definition;
			controller.FindProperty("projectileVisualPrefab").objectReferenceValue = null;
			controller.ApplyModifiedPropertiesWithoutUndo();
			CapsuleCollider body = root.GetComponent<CapsuleCollider>();
			body.radius = 0.52f;
			body.height = 2.1f;
			body.center = Vector3.up * body.height * 0.5f;
			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
		CreatureEnemyRewire.Rewire("Wildling", PrefabPath);
		SerializedObject serializedDefinition = new SerializedObject(definition);
		serializedDefinition.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
		serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(definition);
		AssetDatabase.SaveAssets();
		Debug.Log("Eidren: Wildling prefab rebuilt (3D) and referenced by its definition.");
	}

	private static EnemyDefinition BuildDefinition()
	{
		EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/Wildling.asset");
		if (definition == null)
		{
			definition = ScriptableObject.CreateInstance<EnemyDefinition>();
			definition.name = "Wildling";
			AssetDatabase.CreateAsset(definition, "Assets/_Game/Data/Enemies/Wildling.asset");
		}
		SerializedObject serializedObject = new SerializedObject(definition);
		serializedObject.FindProperty("id").stringValue = "enemy.wildling";
		serializedObject.FindProperty("displayName").stringValue = "Wildling";
		serializedObject.FindProperty("maximumHealth").floatValue = 180f;
		serializedObject.FindProperty("maximumStagger").floatValue = 90f;
		serializedObject.FindProperty("staggerDuration").floatValue = 2.5f;
		serializedObject.FindProperty("attackDamage").floatValue = 18f;
		serializedObject.FindProperty("telegraphDuration").floatValue = 0.55f;
		serializedObject.FindProperty("attackWindowDuration").floatValue = 0.18f;
		serializedObject.FindProperty("recoveryDuration").floatValue = 0.8f;
		serializedObject.FindProperty("deathDisableDelay").floatValue = 2.5f;
		LootTableDefinition lootTable = AssetDatabase.LoadAssetAtPath<LootTableDefinition>("Assets/_Game/Data/Loot/WildlingLoot.asset");
		serializedObject.FindProperty("lootTable").objectReferenceValue = lootTable;
		SerializedProperty serializedProperty = serializedObject.FindProperty("navigation");
		serializedProperty.FindPropertyRelative("MoveSpeed").floatValue = 3.6f;
		serializedProperty.FindPropertyRelative("Acceleration").floatValue = 14f;
		serializedProperty.FindPropertyRelative("AngularSpeed").floatValue = 360f;
		serializedProperty.FindPropertyRelative("AttackRange").floatValue = 1.7f;
		serializedProperty.FindPropertyRelative("DetectionRange").floatValue = 8f;
		serializedProperty.FindPropertyRelative("LeashRange").floatValue = 14f;
		serializedProperty.FindPropertyRelative("LeashFollowDistance").floatValue = 2f;
		serializedProperty.FindPropertyRelative("PatrolRadius").floatValue = 3f;
		serializedProperty.FindPropertyRelative("PatrolWait").floatValue = 1.2f;
		serializedProperty.FindPropertyRelative("AlertDuration").floatValue = 0.35f;
		serializedProperty.FindPropertyRelative("ReturnTolerance").floatValue = 0.55f;
		serializedProperty.FindPropertyRelative("NavMeshSampleDistance").floatValue = 4f;
		serializedProperty.FindPropertyRelative("AgentRadius").floatValue = 0.52f;
		serializedProperty.FindPropertyRelative("AgentHeight").floatValue = 2.1f;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(definition);
		string[] errors = definition.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException("Wildling definition is invalid: " + string.Join("; ", errors));
		}
		return definition;
	}

	private static GameObject LoadVisual()
	{
		GameObject authored = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Actors/2D/Wildling_2D.prefab");
		if (authored != null)
		{
			return authored;
		}
		throw new FileNotFoundException("The canonical Wildling sprite prefab is required.", "Assets/_Game/Prefabs/Actors/2D/Wildling_2D.prefab");
	}

	private static void BuildPrefab(EnemyDefinition definition, GameObject visualPrefab, Material telegraphMaterial)
	{
		GameObject root = new GameObject("Wildling");
		CapsuleCollider capsuleCollider = root.AddComponent<CapsuleCollider>();
		capsuleCollider.radius = 0.52f;
		capsuleCollider.height = 2.1f;
		capsuleCollider.center = Vector3.up * 1.05f;
		GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
		visual.name = "Visual";
		visual.transform.SetParent(root.transform, worldPositionStays: false);
		GameObject gameObject = new GameObject("AttackHitbox");
		gameObject.transform.SetParent(root.transform, worldPositionStays: false);
		gameObject.transform.localPosition = new Vector3(0f, 0.95f, 1.05f);
		BoxCollider attackCollider = gameObject.AddComponent<BoxCollider>();
		attackCollider.size = new Vector3(1.5f, 1.6f, 1.45f);
		attackCollider.isTrigger = true;
		attackCollider.enabled = false;
		EnemyMeleeHitbox hitbox = root.AddComponent<EnemyMeleeHitbox>();
		hitbox.Configure(attackCollider);
		GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cube);
		telegraph.name = "AttackTelegraph";
		telegraph.transform.SetParent(root.transform, worldPositionStays: false);
		telegraph.transform.localPosition = new Vector3(0f, 0.06f, 0.9f);
		telegraph.transform.localScale = new Vector3(1.5f, 0.04f, 1.7f);
		UnityEngine.Object.DestroyImmediate(telegraph.GetComponent<Collider>());
		telegraph.GetComponent<Renderer>().sharedMaterial = telegraphMaterial;
		telegraph.SetActive(value: false);
		root.AddComponent<AudioSource>().playOnAwake = false;
		root.AddComponent<WildlingController>().ConfigurePrefab(definition, hitbox, telegraph, visual.transform);
		GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/Enemies/Wildling.prefab");
		UnityEngine.Object.DestroyImmediate(root);
		// F31-009: Der Laufzeit-Populator stellt Gegner über Definition.Prefab
		// auf — der alte Szenen-Bake hat das leere Feld still ersetzt.
		SerializedObject serializedDefinition = new SerializedObject(definition);
		serializedDefinition.FindProperty("prefab").objectReferenceValue = prefabAsset;
		serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(definition);
	}

	private static void ConfigureZoneAllocations(EnemyDefinition definition)
	{
		// F31-009: Die Besetzung schreibt zentral der
		// ZoneEnemyPopulationBuilder — vorher hat dieser Builder das Array
		// auf eine einzige Wildling-Zeile gekürzt und damit je nach
		// Laufreihenfolge die Tier-2- und Eidra-Zeilen gelöscht.
		ZoneEnemyPopulationBuilder.Apply();
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
		Folder("Assets/_Game/Prefabs/Enemies", "Visuals");
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
