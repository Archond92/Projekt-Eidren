using Eidren.Data;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// F31-006: Baut die Tutorial-Questkette (17 Schritte bis zum ersten
	/// Eidra, 940 EP). Kette und EP-Werte sind im Entwurf
	/// (ENTWURF_QUESTSYSTEM_V031.md) und in der Bibel (M13.4) festgehalten —
	/// Änderungen laufen über diesen Builder, nie über den Inspector.
	/// </summary>
	public static class QuestChainContentBuilder
	{
		public const string AssetPath = "Assets/_Game/Resources/Data/Progression/QuestChain_V01.asset";

		public const string ChainId = "quest.tutorial.first_eidra";

		[MenuItem("Eidren/Data/Build Quest Chain V0.1")]
		public static void Build()
		{
			QuestChainDefinition definition = AssetDatabase.LoadAssetAtPath<QuestChainDefinition>(AssetPath);
			if (definition == null)
			{
				definition = ScriptableObject.CreateInstance<QuestChainDefinition>();
				definition.name = "QuestChain_V01";
				AssetDatabase.CreateAsset(definition, AssetPath);
			}
			SerializedObject serialized = new SerializedObject(definition);
			serialized.FindProperty("id").stringValue = ChainId;
			(string id, string text, QuestConditionKind kind, string targetId, int count, int xp)[] steps = new[]
			{
				("fasern", "Ernte 2 Faserpflanzen", QuestConditionKind.ResourceNodeCompleted, "resource.fiber_plant", 2, 30),
				("werkbank", "Baue eine Werkbank", QuestConditionKind.BuildingConstructed, "building.workbench", 1, 40),
				("axt", "Fertige die Axt", QuestConditionKind.RecipeCrafted, "craft_axe", 1, 40),
				("baeume", "Fälle 2 Bäume", QuestConditionKind.ResourceNodeCompleted, "resource.tree", 2, 30),
				("lagerkiste", "Baue eine Lagerkiste", QuestConditionKind.BuildingConstructed, "building.storage_chest", 1, 40),
				("hammer", "Fertige den Hammer", QuestConditionKind.RecipeCrafted, "craft_hammer", 1, 40),
				("steine", "Baue 2 Steinvorkommen ab", QuestConditionKind.ResourceNodeCompleted, "resource.stone_deposit", 2, 40),
				("spitzhacke", "Fertige die Spitzhacke", QuestConditionKind.RecipeCrafted, "craft_pickaxe", 1, 50),
				("sense", "Fertige die Sense", QuestConditionKind.RecipeCrafted, "craft_scythe", 1, 50),
				("wildlinge", "Besiege 2 Wildlinge", QuestConditionKind.EnemyDefeated, (string)null, 2, 60),
				("kupfer", "Baue 2 Kupferadern ab", QuestConditionKind.ResourceNodeCompleted, "resource.copper_vein", 2, 50),
				("schmelzofen", "Baue den Schmelzofen", QuestConditionKind.BuildingConstructed, "building.smelter", 1, 60),
				("kupferbarren", "Fertige einen Kupferbarren", QuestConditionKind.RecipeCrafted, "craft_copper_bar", 1, 60),
				("saegewerk", "Baue das Sägewerk", QuestConditionKind.BuildingConstructed, "building.sawmill", 1, 60),
				("seilerei", "Baue die Seilerei", QuestConditionKind.BuildingConstructed, "building.ropewalk", 1, 60),
				("fanggeraet", "Fertige das Fanggerät", QuestConditionKind.RecipeCrafted, "craft_catch_device", 1, 80),
				("erster_eidra", "Fange deinen ersten Eidra", QuestConditionKind.EidraCaptured, (string)null, 1, 150)
			};
			SerializedProperty array = serialized.FindProperty("steps");
			array.arraySize = steps.Length;
			for (int index = 0; index < steps.Length; index++)
			{
				SerializedProperty element = array.GetArrayElementAtIndex(index);
				element.FindPropertyRelative("id").stringValue = steps[index].id;
				element.FindPropertyRelative("displayText").stringValue = steps[index].text;
				element.FindPropertyRelative("condition").enumValueIndex = (int)steps[index].kind;
				element.FindPropertyRelative("targetId").stringValue = steps[index].targetId ?? string.Empty;
				element.FindPropertyRelative("targetCount").intValue = steps[index].count;
				element.FindPropertyRelative("experienceReward").intValue = steps[index].xp;
			}
			serialized.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(definition);
			AssetDatabase.SaveAssets();
			Debug.Log("Eidren: tutorial quest chain built (17 steps, 940 XP).");
		}
	}
}
