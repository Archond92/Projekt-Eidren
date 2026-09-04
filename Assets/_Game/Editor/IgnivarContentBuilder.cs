using Eidren.Data;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class IgnivarContentBuilder
{
	public const string EidraPath = "Assets/_Game/Data/Eidren/Ignivar.asset";

	public const string EnemyPath = "Assets/_Game/Data/Enemies/Enemy_Ignivar.asset";

	public const string EmberCirclePath = "Assets/_Game/Data/Abilities/Glutkreis.asset";

	public const string MoltenBrandPath = "Assets/_Game/Data/Abilities/Schmelzbrand.asset";

	public const string EmberCircleIconPath = "Assets/_Game/Art/UI/Abilities/UI_Ability_Ignivar_EmberCircle.png";

	public const string MoltenBrandIconPath = "Assets/_Game/Art/UI/Abilities/UI_Ability_Ignivar_MoltenBrand.png";

	public const string SmithingMarkIconPath = "Assets/_Game/Art/Items/ITEM_SmithingMark.png";

	public const string SmithingMarkPath = "Assets/_Game/Data/Items/SmithingMark.asset";

	[MenuItem("Eidren/Data/Build Ignivar V0.2")]
	public static void Build()
	{
		EidraCaptureContentBuilder.BuildEidraCapture();
		Sprite emberIcon = ImportIcon("Assets/_Game/Art/UI/Abilities/UI_Ability_Ignivar_EmberCircle.png");
		Sprite brandIcon = ImportIcon("Assets/_Game/Art/UI/Abilities/UI_Ability_Ignivar_MoltenBrand.png");
		Sprite smithingMarkIcon = ImportIcon("Assets/_Game/Art/Items/ITEM_SmithingMark.png");
		AbilityData ember = Ability("Assets/_Game/Data/Abilities/Glutkreis.asset", "Glutkreis", "glutkreis", AbilityExecutionType.EmberCircle, 10f, 8f, 0.35f, 5f, 2.75f, 6f, 0f, emberIcon);
		AbilityData brand = Ability("Assets/_Game/Data/Abilities/Schmelzbrand.asset", "Schmelzbrand", "schmelzbrand", AbilityExecutionType.MoltenBrand, 12f, 10f, 0.35f, 6f, 0f, 0f, 0.15f, brandIcon);
		BindSmithingMarkIcon(smithingMarkIcon);
		EidraData ignivar = BuildEidra(ember, brand);
		BuildEnemy(ignivar);
		UpdateCatalog(ignivar);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: built Ignivar, abilities, enemy and catalog entry.");
	}

	private static AbilityData Ability(string path, string displayName, string id, AbilityExecutionType type, float cooldown, float range, float castDuration, float effectDuration, float radius, float damagePerSecond, float protectionReduction, Sprite icon)
	{
		AbilityData abilityData = LoadOrCreate<AbilityData>(path);
		abilityData.name = displayName;
		SerializedObject serializedObject = new SerializedObject(abilityData);
		serializedObject.FindProperty("id").stringValue = id;
		serializedObject.FindProperty("DisplayName").stringValue = displayName;
		serializedObject.FindProperty("Icon").objectReferenceValue = icon;
		serializedObject.FindProperty("Cooldown").floatValue = cooldown;
		serializedObject.FindProperty("Range").floatValue = range;
		serializedObject.FindProperty("ExecutionType").enumValueIndex = (int)type;
		serializedObject.FindProperty("CastDuration").floatValue = castDuration;
		serializedObject.FindProperty("EffectDuration").floatValue = effectDuration;
		serializedObject.FindProperty("StaggerAmount").floatValue = 0f;
		serializedObject.FindProperty("BackDamageMultiplier").floatValue = 1f;
		serializedObject.FindProperty("TeleportBehindDistance").floatValue = 0f;
		serializedObject.FindProperty("Radius").floatValue = radius;
		serializedObject.FindProperty("HealthDamagePerSecond").floatValue = damagePerSecond;
		serializedObject.FindProperty("ProtectionReduction").floatValue = protectionReduction;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(abilityData);
		return abilityData;
	}

	private static Sprite ImportIcon(string path)
	{
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
		TextureImporter obj = AssetImporter.GetAtPath(path) as TextureImporter;
		if (obj == null)
		{
			throw new FileNotFoundException(path);
		}
		obj.textureType = TextureImporterType.Sprite;
		obj.spriteImportMode = SpriteImportMode.Single;
		obj.alphaIsTransparency = true;
		obj.mipmapEnabled = false;
		obj.filterMode = FilterMode.Bilinear;
		obj.textureCompression = TextureImporterCompression.Compressed;
		obj.SaveAndReimport();
		Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
		if (sprite == null)
		{
			throw new InvalidOperationException("No sprite imported at " + path + ".");
		}
		return sprite;
	}

	private static void BindSmithingMarkIcon(Sprite icon)
	{
		ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/SmithingMark.asset");
		if (itemDefinition == null)
		{
			throw new FileNotFoundException("Assets/_Game/Data/Items/SmithingMark.asset");
		}
		SerializedObject serializedObject = new SerializedObject(itemDefinition);
		serializedObject.FindProperty("icon").objectReferenceValue = icon;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(itemDefinition);
	}

	private static EidraData BuildEidra(AbilityData ember, AbilityData brand)
	{
		EidraData eidraData = LoadOrCreate<EidraData>("Assets/_Game/Data/Eidren/Ignivar.asset");
		eidraData.name = "Ignivar";
		SerializedObject serializedObject = new SerializedObject(eidraData);
		serializedObject.FindProperty("id").stringValue = "ignivar";
		serializedObject.FindProperty("DisplayName").stringValue = "Ignivar";
		serializedObject.FindProperty("UiAccent").colorValue = new Color(0.96f, 0.28f, 0.06f, 1f);
		serializedObject.FindProperty("Role").enumValueIndex = 1;
		serializedObject.FindProperty("PassiveStaggerMultiplier").floatValue = 1f;
		serializedObject.FindProperty("PassiveBackDamageMultiplier").floatValue = 1f;
		serializedObject.FindProperty("Skill1").objectReferenceValue = ember;
		serializedObject.FindProperty("Skill2").objectReferenceValue = brand;
		serializedObject.FindProperty("productionRole").enumValueIndex = 0;
		SerializedProperty serializedProperty = serializedObject.FindProperty("capture");
		serializedProperty.FindPropertyRelative("requirementAtFullHealth").floatValue = 170f;
		serializedProperty.FindPropertyRelative("minimumRequirement").floatValue = 70f;
		serializedProperty.FindPropertyRelative("fleeHealthFraction").floatValue = 0.15f;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(eidraData);
		string[] errors = eidraData.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException(string.Join("; ", errors));
		}
		return eidraData;
	}

	private static void BuildEnemy(EidraData ignivar)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/WildEidra.prefab");
		if (prefab == null)
		{
			throw new FileNotFoundException("Assets/_Game/Prefabs/Enemies/WildEidra.prefab");
		}
		EnemyDefinition enemyDefinition = LoadOrCreate<EnemyDefinition>("Assets/_Game/Data/Enemies/Enemy_Ignivar.asset");
		enemyDefinition.name = "Enemy_Ignivar";
		SerializedObject serializedObject = new SerializedObject(enemyDefinition);
		serializedObject.FindProperty("id").stringValue = "enemy.eidra.ignivar";
		serializedObject.FindProperty("displayName").stringValue = "Ignivar";
		serializedObject.FindProperty("maximumHealth").floatValue = 240f;
		serializedObject.FindProperty("maximumStagger").floatValue = 100f;
		serializedObject.FindProperty("staggerDuration").floatValue = 2.2f;
		serializedObject.FindProperty("attackDamage").floatValue = 20f;
		serializedObject.FindProperty("protection").floatValue = 0f;
		serializedObject.FindProperty("combatStyle").enumValueIndex = 0;
		serializedObject.FindProperty("telegraphDuration").floatValue = 0.55f;
		serializedObject.FindProperty("attackWindowDuration").floatValue = 0.18f;
		serializedObject.FindProperty("recoveryDuration").floatValue = 0.9f;
		serializedObject.FindProperty("deathDisableDelay").floatValue = 0f;
		serializedObject.FindProperty("experienceReward").intValue = 0;
		serializedObject.FindProperty("lootTable").objectReferenceValue = null;
		serializedObject.FindProperty("capturableEidra").objectReferenceValue = ignivar;
		serializedObject.FindProperty("prefab").objectReferenceValue = prefab;
		SetNavigation(serializedObject.FindProperty("navigation"));
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(enemyDefinition);
		string[] errors = enemyDefinition.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException(string.Join("; ", errors));
		}
	}

	private static void SetNavigation(SerializedProperty navigation)
	{
		Set(navigation, "MoveSpeed", 3.4f);
		Set(navigation, "Acceleration", 12f);
		Set(navigation, "AngularSpeed", 360f);
		Set(navigation, "AttackRange", 1.9f);
		Set(navigation, "DetectionRange", 8f);
		Set(navigation, "LeashRange", 12f);
		Set(navigation, "LeashFollowDistance", 2f);
		Set(navigation, "PatrolRadius", 2.5f);
		Set(navigation, "PatrolWait", 1.4f);
		Set(navigation, "AlertDuration", 0.3f);
		Set(navigation, "ReturnTolerance", 0.55f);
		Set(navigation, "NavMeshSampleDistance", 4f);
		Set(navigation, "AgentRadius", 0.5f);
		Set(navigation, "AgentHeight", 2f);
	}

	private static void UpdateCatalog(EidraData ignivar)
	{
		EidraCatalogDefinition eidraCatalogDefinition = AssetDatabase.LoadAssetAtPath<EidraCatalogDefinition>("Assets/_Game/Resources/Data/EidraCatalog_V01.asset");
		if (eidraCatalogDefinition == null)
		{
			throw new FileNotFoundException("Assets/_Game/Resources/Data/EidraCatalog_V01.asset");
		}
		EidraData terrock = AssetDatabase.LoadAssetAtPath<EidraData>("Assets/_Game/Data/Eidren/Terrock.asset");
		EidraData noctarion = AssetDatabase.LoadAssetAtPath<EidraData>("Assets/_Game/Data/Eidren/Noctarion.asset");
		SerializedObject serializedObject = new SerializedObject(eidraCatalogDefinition);
		SerializedProperty serializedProperty = serializedObject.FindProperty("eidren");
		serializedProperty.arraySize = 3;
		serializedProperty.GetArrayElementAtIndex(0).objectReferenceValue = terrock;
		serializedProperty.GetArrayElementAtIndex(1).objectReferenceValue = noctarion;
		serializedProperty.GetArrayElementAtIndex(2).objectReferenceValue = ignivar;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(eidraCatalogDefinition);
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

	private static void Set(SerializedProperty parent, string name, float value)
	{
		parent.FindPropertyRelative(name).floatValue = value;
	}
}
}
