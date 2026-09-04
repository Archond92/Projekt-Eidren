using Eidren.Data;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class EidraForgeLootAssetBuilder
{
	public const string AssetPath = "Assets/_Game/Resources/Data/EidraForgeLoot_V02.asset";

	[MenuItem("Eidren/Data/Build Eidra Forge Loot V0.2")]
	public static void Build()
	{
		ItemContentAssetBuilder.BuildItemDefinitions();
		EidraForgeLootProfile profile = AssetDatabase.LoadAssetAtPath<EidraForgeLootProfile>("Assets/_Game/Resources/Data/EidraForgeLoot_V02.asset");
		if (profile == null)
		{
			profile = ScriptableObject.CreateInstance<EidraForgeLootProfile>();
			profile.name = "EidraForgeLoot_V02";
			AssetDatabase.CreateAsset(profile, "Assets/_Game/Resources/Data/EidraForgeLoot_V02.asset");
		}
		SerializedObject serialized = new SerializedObject(profile);
		serialized.FindProperty("id").stringValue = "loot.eidra_forge.v02";
		Set(serialized, "commonMaterialItemIds", "wood", "stone", "plant_fiber");
		Set(serialized, "refinedMaterialItemIds", "plank", "rope", "stone_block", "copper_bar");
		Set(serialized, "consumableItemIds", "healing_potion", "buff_food");
		Set(serialized, "highQualityConsumableItemIds", "healing_potion", "buff_food");
		Set(serialized, "regularTierOneEquipmentItemIds", "copper_hammer", "copper_daggers", "copper_spear", "copper_axe", "copper_scythe", "copper_pickaxe", "armor_copper_helmet", "armor_copper_chest", "armor_copper_gloves", "armor_copper_legs");
		Set(serialized, "namedWeaponItemIds", "sealbreaker", "ash_fangs", "ember_thorn");
		SerializedProperty serializedProperty = serialized.FindProperty("chances");
		serializedProperty.FindPropertyRelative("supplyEquipment").intValue = 1;
		serializedProperty.FindPropertyRelative("optionalEquipment").intValue = 5;
		serializedProperty.FindPropertyRelative("optionalFitting").intValue = 10;
		serializedProperty.FindPropertyRelative("eliteEquipment").intValue = 10;
		serializedProperty.FindPropertyRelative("eliteFitting").intValue = 25;
		serializedProperty.FindPropertyRelative("firstCompletionNamedWeapon").intValue = 25;
		serializedProperty.FindPropertyRelative("repeatCompletionNamedWeapon").intValue = 15;
		serialized.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(profile);
		AssetDatabase.SaveAssets();
		Debug.Log("Eidren: built Eidra Forge loot profile v0.2.");
	}

	private static void Set(SerializedObject serialized, string propertyName, params string[] values)
	{
		SerializedProperty property = serialized.FindProperty(propertyName);
		property.arraySize = values.Length;
		for (int index = 0; index < values.Length; index++)
		{
			property.GetArrayElementAtIndex(index).stringValue = values[index];
		}
	}
}
}
