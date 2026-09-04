using Eidren.Data;
using Eidren.Interaction;
using System.IO;
using System;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class LootContentBuilder
{
	public const string WildlingLootTablePath = "Assets/_Game/Data/Loot/WildlingLoot.asset";

	public const string WorldItemPrefabPath = "Assets/_Game/Prefabs/Items/WorldItem.prefab";

	private const string MaterialFolder = "Assets/_Game/Art/Items/Materials";

	[MenuItem("Eidren/Data/Build Loot V0.1")]
	public static void BuildLootContent()
	{
		EnsureFolder("Assets/_Game/Data", "Loot");
		EnsureFolder("Assets/_Game/Prefabs", "Items");
		EnsureFolder("Assets/_Game/Art/Items", "Materials");
		Material baseMaterial = EnsureMaterial("ITEM_WorldDropBase", new Color(0.12f, 0.18f, 0.14f, 1f));
		Material highlightMaterial = EnsureMaterial("ITEM_WorldDropHighlight", new Color(0.35f, 0.82f, 0.49f, 0.58f));
		GameObject worldItemPrefab = BuildWorldItemPrefab(baseMaterial, highlightMaterial);
		AssignWildlingLootTable(BuildWildlingLootTable());
		AssignWorldDropPrefab(worldItemPrefab);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		EidrenSceneStructureBuilder.BuildMapExitScenes();
		Debug.Log("Eidren: built Loot V0.1, WorldItem prefab, and outdoor loot systems.");
	}

	private static LootTableDefinition BuildWildlingLootTable()
	{
		LootTableDefinition table = AssetDatabase.LoadAssetAtPath<LootTableDefinition>("Assets/_Game/Data/Loot/WildlingLoot.asset");
		if (table == null)
		{
			table = ScriptableObject.CreateInstance<LootTableDefinition>();
			table.name = "WildlingLoot";
			AssetDatabase.CreateAsset(table, "Assets/_Game/Data/Loot/WildlingLoot.asset");
		}
		SerializedObject serializedObject = new SerializedObject(table);
		serializedObject.FindProperty("id").stringValue = "loot.wildling.v01";
		SerializedProperty serializedProperty = serializedObject.FindProperty("entries");
		serializedProperty.arraySize = 4;
		SetEntry(serializedProperty.GetArrayElementAtIndex(0), "plant_fiber", 1f, 1, 2, guaranteed: true);
		SetEntry(serializedProperty.GetArrayElementAtIndex(1), "healing_potion", 0.15f, 1, 1, guaranteed: false);
		SetEntry(serializedProperty.GetArrayElementAtIndex(2), "buff_food", 0.08f, 1, 1, guaranteed: false);
		SetEntry(serializedProperty.GetArrayElementAtIndex(3), "wood", 0.2f, 1, 2, guaranteed: false);
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(table);
		string[] errors = table.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException("Wildling loot table is invalid: " + string.Join("; ", errors));
		}
		return table;
	}

	private static void SetEntry(SerializedProperty entry, string itemId, float chance, int minimum, int maximum, bool guaranteed, string group = "")
	{
		entry.FindPropertyRelative("itemId").stringValue = itemId;
		entry.FindPropertyRelative("chance").floatValue = chance;
		entry.FindPropertyRelative("minimumAmount").intValue = minimum;
		entry.FindPropertyRelative("maximumAmount").intValue = maximum;
		entry.FindPropertyRelative("guaranteed").boolValue = guaranteed;
		entry.FindPropertyRelative("selectionGroup").stringValue = group;
	}

	private static GameObject BuildWorldItemPrefab(Material baseMaterial, Material highlightMaterial)
	{
		GameObject authored = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Items/WorldItem.prefab");
		if (authored != null && MidpolyWorldLootMigration.IsApprovedWorldItem())
		{
			return authored;
		}
		if (authored != null && authored.transform.Find("Geometry_A14") != null)
		{
			return authored;
		}
		GameObject root = new GameObject("WorldItem");
		SphereCollider trigger = root.AddComponent<SphereCollider>();
		trigger.isTrigger = true;
		trigger.radius = 0.72f;
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		gameObject.name = "DropBase";
		gameObject.transform.SetParent(root.transform, worldPositionStays: false);
		gameObject.transform.localPosition = new Vector3(0f, 0.12f, 0f);
		gameObject.transform.localScale = new Vector3(0.48f, 0.12f, 0.48f);
		UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
		gameObject.GetComponent<Renderer>().sharedMaterial = baseMaterial;
		GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		highlight.name = "StaticHighlight";
		highlight.transform.SetParent(root.transform, worldPositionStays: false);
		highlight.transform.localPosition = new Vector3(0f, 0.025f, 0f);
		highlight.transform.localScale = new Vector3(0.72f, 0.025f, 0.72f);
		UnityEngine.Object.DestroyImmediate(highlight.GetComponent<Collider>());
		Renderer component = highlight.GetComponent<Renderer>();
		component.sharedMaterial = highlightMaterial;
		component.shadowCastingMode = ShadowCastingMode.Off;
		component.receiveShadows = false;
		GameObject gameObject2 = new GameObject("ItemIcon");
		gameObject2.transform.SetParent(root.transform, worldPositionStays: false);
		gameObject2.transform.localPosition = new Vector3(0f, 0.88f, 0f);
		gameObject2.transform.localScale = Vector3.one * 0.72f;
		SpriteRenderer icon = gameObject2.AddComponent<SpriteRenderer>();
		icon.sortingOrder = 45;
		GameObject gameObject3 = new GameObject("Quantity");
		gameObject3.transform.SetParent(root.transform, worldPositionStays: false);
		gameObject3.transform.localPosition = new Vector3(0.42f, 1.28f, 0f);
		TextMesh quantity = gameObject3.AddComponent<TextMesh>();
		quantity.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		quantity.fontSize = 52;
		quantity.fontStyle = FontStyle.Bold;
		quantity.characterSize = 0.055f;
		quantity.anchor = TextAnchor.MiddleCenter;
		quantity.alignment = TextAlignment.Center;
		quantity.color = Color.white;
		MeshRenderer component2 = quantity.GetComponent<MeshRenderer>();
		component2.sortingOrder = 46;
		component2.shadowCastingMode = ShadowCastingMode.Off;
		component2.receiveShadows = false;
		root.AddComponent<AudioSource>().playOnAwake = false;
		root.AddComponent<WorldItemController>().ConfigurePrefab(icon, quantity, highlight, trigger);
		GameObject result = PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/Items/WorldItem.prefab");
		UnityEngine.Object.DestroyImmediate(root);
		return result;
	}

	private static void AssignWildlingLootTable(LootTableDefinition lootTable)
	{
		EnemyDefinition enemyDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/Wildling.asset");
		if (enemyDefinition == null)
		{
			throw new FileNotFoundException("Assets/_Game/Data/Enemies/Wildling.asset");
		}
		SerializedObject serializedObject = new SerializedObject(enemyDefinition);
		serializedObject.FindProperty("lootTable").objectReferenceValue = lootTable;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(enemyDefinition);
	}

	private static void AssignWorldDropPrefab(GameObject worldItemPrefab)
	{
		string[] array = new string[4] { "PlantFiber", "HealingPotion", "BuffFood", "Wood" };
		foreach (string assetName in array)
		{
			string path = "Assets/_Game/Data/Items/" + assetName + ".asset";
			ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
			if (itemDefinition == null)
			{
				throw new FileNotFoundException(path);
			}
			SerializedObject serializedObject = new SerializedObject(itemDefinition);
			serializedObject.FindProperty("worldDropPrefab").objectReferenceValue = worldItemPrefab;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(itemDefinition);
		}
	}

	private static Material EnsureMaterial(string name, Color color)
	{
		string path = "Assets/_Game/Art/Items/Materials/" + name + ".mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (material == null)
		{
			Material material2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Resources/EidrenRuntimeMaterial.mat");
			if (material2 == null)
			{
				throw new FileNotFoundException("EidrenRuntimeMaterial.mat");
			}
			material = new Material(material2)
			{
				name = name
			};
			AssetDatabase.CreateAsset(material, path);
		}
		material.color = color;
		EditorUtility.SetDirty(material);
		return material;
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
