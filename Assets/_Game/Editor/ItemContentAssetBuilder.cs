using Eidren.Data;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class ItemContentAssetBuilder
{
	private const string ItemDataFolder = "Assets/_Game/Data/Items";

	private const string ItemArtFolder = "Assets/_Game/Art/Items";

	private const string CatalogFolder = "Assets/_Game/Resources/Data";

	private const string CatalogPath = "Assets/_Game/Resources/Data/ItemCatalog_V01.asset";

	private const string CommonWorldItemPrefabPath = "Assets/_Game/Prefabs/Items/WorldItem.prefab";

	[MenuItem("Eidren/Data/Build Item Definitions V0.1")]
	public static void BuildItemDefinitions()
	{
		EnsureFolder("Assets/_Game/Data", "Items");
		EnsureFolder("Assets/_Game/Art", "Items");
		EnsureFolder("Assets/_Game/Resources", "Data");
		ItemDefinition[] items = ItemContentTable.EnsureAll();
		ItemCatalogDefinition catalog = LoadOrCreate<ItemCatalogDefinition>("Assets/_Game/Resources/Data/ItemCatalog_V01.asset");
		SerializedObject catalogSerialized = new SerializedObject(catalog);
		SerializedProperty itemProperty = catalogSerialized.FindProperty("items");
		itemProperty.arraySize = items.Length;
		for (int index = 0; index < items.Length; index++)
		{
			itemProperty.GetArrayElementAtIndex(index).objectReferenceValue = items[index];
		}
		catalogSerialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(catalog);
		Validate(items);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		VerifyOnDisk(items.Length);
		Debug.Log($"Eidren: built {items.Length} V0.1 ItemDefinitions and " + "ItemCatalog_V01.");
	}

	internal static ItemDefinition EnsureItem(string assetName, string id, string displayName, string description, ItemCategory category, string materialFamily, int tier, string iconSourcePath, int maximumStackSize, bool canBeUsed, bool canBeDropped, bool canBeStored, bool canBeCrafted, string[] tags, ItemUseActionType actionType, float healingAmount = 0f, float duration = 0f, float damageMultiplier = 0f, float staggerDamageMultiplier = 0f, int maximumDurability = 0, WearableSlot wearableSlot = WearableSlot.None, float protectionContribution = 0f)
	{
		ItemDefinition item = LoadOrCreate<ItemDefinition>("Assets/_Game/Data/Items/" + assetName + ".asset");
		SerializedObject serialized = new SerializedObject(item);
		serialized.FindProperty("id").stringValue = id;
		serialized.FindProperty("displayName").stringValue = displayName;
		serialized.FindProperty("description").stringValue = description;
		serialized.FindProperty("category").enumValueIndex = (int)category;
		serialized.FindProperty("materialFamily").stringValue = materialFamily ?? string.Empty;
		serialized.FindProperty("tier").intValue = tier;
		serialized.FindProperty("maximumDurability").intValue = maximumDurability;
		serialized.FindProperty("protectionContribution").floatValue = protectionContribution;
		serialized.FindProperty("wearableSlot").enumValueIndex = (int)wearableSlot;
		// F31-002: FERTIGE Icons (ITEM_* ohne TMP, z. B. ITEM_Wanderer*,
		// ITEM_SmithingMark) gelten direkt — das feste "ITEM_TMP_"-Ziel
		// machte sie unerreichbar. ITEM_TMP_-Quellen behalten dagegen den
		// Spiegel auf das eigene ITEM_TMP_<assetName>.png: mehrere Items
		// teilen sich dort bewusst eine Quelldatei (IronBar zeigt auf
		// CopperBar) und werden erst im eigenen Spiegel per
		// ItemIconFamilienToenung unterscheidbar gemacht.
		string normalizedSource = (iconSourcePath ?? string.Empty).Replace('\\', '/');
		string sourceFileName = Path.GetFileNameWithoutExtension(normalizedSource);
		bool finishedIcon = normalizedSource.StartsWith("Assets/_Game/Art/Items/ITEM_", StringComparison.OrdinalIgnoreCase)
			&& !sourceFileName.StartsWith("ITEM_TMP_", StringComparison.OrdinalIgnoreCase);
		string iconAssetName = (finishedIcon ? sourceFileName : ("ITEM_TMP_" + assetName));
		serialized.FindProperty("icon").objectReferenceValue = EnsureItemIcon(iconAssetName, iconSourcePath);
		serialized.FindProperty("maximumStackSize").intValue = maximumStackSize;
		serialized.FindProperty("canBeUsed").boolValue = canBeUsed;
		serialized.FindProperty("canBeDropped").boolValue = canBeDropped;
		serialized.FindProperty("canBeStored").boolValue = canBeStored;
		serialized.FindProperty("canBeCrafted").boolValue = canBeCrafted;
		serialized.FindProperty("sellValue").intValue = -1;
		serialized.FindProperty("worldDropPrefab").objectReferenceValue = (canBeDropped ? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Items/WorldItem.prefab") : null);
		SerializedProperty tagProperty = serialized.FindProperty("tags");
		tagProperty.arraySize = ((tags != null) ? tags.Length : 0);
		for (int index = 0; index < tagProperty.arraySize; index++)
		{
			tagProperty.GetArrayElementAtIndex(index).stringValue = tags[index];
		}
		SerializedProperty serializedProperty = serialized.FindProperty("useConfiguration");
		serializedProperty.FindPropertyRelative("actionType").enumValueIndex = (int)actionType;
		serializedProperty.FindPropertyRelative("healingAmount").floatValue = healingAmount;
		serializedProperty.FindPropertyRelative("duration").floatValue = duration;
		serializedProperty.FindPropertyRelative("damageMultiplier").floatValue = damageMultiplier;
		serializedProperty.FindPropertyRelative("staggerDamageMultiplier").floatValue = staggerDamageMultiplier;
		serialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(item);
		return item;
	}

	private static Sprite EnsureItemIcon(string itemIconName, string sourcePath)
	{
		string destinationPath = "Assets/_Game/Art/Items/" + itemIconName + ".png";
		if (string.Equals(sourcePath.Replace('\\', '/'), destinationPath, StringComparison.OrdinalIgnoreCase))
		{
			ArtAssetImportUtility.ConfigureSprite(destinationPath, 256);
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(destinationPath);
			if (sprite == null)
			{
				throw new InvalidOperationException("Item icon could not be loaded: " + destinationPath);
			}
			return sprite;
		}
		return ArtAssetImportUtility.EnsureMirroredSprite(sourcePath, destinationPath, 256);
	}

	private static T LoadOrCreate<T>(string path) where T : ScriptableObject
	{
		T asset = AssetDatabase.LoadAssetAtPath<T>(path);
		if (asset != null)
		{
			return asset;
		}
		asset = ScriptableObject.CreateInstance<T>();
		asset.name = Path.GetFileNameWithoutExtension(path);
		AssetDatabase.CreateAsset(asset, path);
		return asset;
	}

	private static void Validate(ItemDefinition[] items)
	{
		for (int index = 0; index < items.Length; index++)
		{
			ItemDefinitionValidation validation = items[index].ValidateDefinition();
			if (!validation.IsValid)
			{
				throw new InvalidOperationException("Invalid ItemDefinition '" + items[index].name + "': " + string.Join("; ", validation.Errors));
			}
			if (validation.Warnings.Length != 0)
			{
				Debug.LogWarning("ItemDefinition '" + items[index].name + "': " + string.Join("; ", validation.Warnings), items[index]);
			}
		}
	}

	private static void VerifyOnDisk(int expectedCount)
	{
		string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new string[1] { "Assets/_Game/Data/Items" });
		if (guids.Length != expectedCount)
		{
			throw new InvalidOperationException($"Expected {expectedCount} ItemDefinition assets on " + $"disk, found {guids.Length}.");
		}
		string[] array = guids;
		for (int i = 0; i < array.Length; i++)
		{
			string path = AssetDatabase.GUIDToAssetPath(array[i]);
			ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
			if (item == null || string.IsNullOrWhiteSpace(item.Id))
			{
				throw new InvalidOperationException("ItemDefinition at '" + path + "' has no stable ID after saving.");
			}
			ItemDefinitionValidation validation = item.ValidateDefinition();
			if (!validation.IsValid)
			{
				throw new InvalidOperationException("ItemDefinition '" + item.Id + "' is invalid on disk: " + string.Join("; ", validation.Errors));
			}
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
