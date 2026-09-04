using Eidren.Data;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// F31-009: Der eine Ort, der die Gegnerbesetzung aller Zonen schreibt.
	/// Vorher haben drei Builder (Wildling, TierTwo, EidraCapture) dasselbe
	/// Array in unterschiedlicher Semantik beschrieben — die Besetzung hing
	/// von der Laufreihenfolge ab. Die Summen folgen der Gefahrenstufe der
	/// Weltkarte (3 x Gefahr - 1, ZoneEnemyPopulationRules.TargetTotal), die
	/// Mischung staffelt sich: Gefahr 2 rein Wildling, ab 3 kommen
	/// Gebietstiere dazu, ab 4 überwiegen sie.
	/// </summary>
	public static class ZoneEnemyPopulationBuilder
	{
		private const string EnemyRoot = "Assets/_Game/Data/Enemies/";

		private const string ZoneRoot = "Assets/_Game/Data/Zones/";

		[MenuItem("Eidren/Data/Build Zone Enemy Population")]
		public static void Build()
		{
			Apply();
			AssetDatabase.SaveAssets();
			Debug.Log("Eidren: zone enemy population rebuilt from danger levels.");
		}

		public static void Apply()
		{
			// Gefahr 1: Heimatbasis bleibt sichere Zone ohne Besetzung.
			SetZone("Zone_HomeBase");
			// Gefahr 2 (5): reines Wildling-Gebiet.
			SetZone("Zone_Greenwood", ("Wildling", 5));
			// Gefahr 3 (8): je ein thematisches Gebietstier + Fang-Eidra.
			SetZone("Zone_Quarry", ("Wildling", 4), ("Riftling", 2), ("Enemy_Terrock", 2));
			SetZone("Zone_Marsh", ("Wildling", 4), ("MoorThrower", 2), ("Enemy_Noctarion", 2));
			// Gefahr 4 (11): Tier-2-Gegner überwiegen.
			SetZone("Zone_TwilightGrove", ("Wildling", 2), ("Riftling", 3), ("RootCharger", 2), ("MoorThrower", 2), ("GraniteShell", 2));
			SetZone("Zone_VeilMarsh", ("Wildling", 2), ("Riftling", 2), ("RootCharger", 2), ("MoorThrower", 3), ("GraniteShell", 2));
			// Gefahr 5 (14): fast nur noch starke Gegner; Glutruinen als
			// Boss-Vorfeld risslinglastig, Grauklüfte granitlastig.
			SetZone("Zone_EmberRuins", ("Wildling", 2), ("Riftling", 4), ("RootCharger", 3), ("MoorThrower", 2), ("GraniteShell", 3));
			SetZone("Zone_GreyRifts", ("Wildling", 2), ("Riftling", 3), ("RootCharger", 3), ("MoorThrower", 2), ("GraniteShell", 4));
		}

		private static void SetZone(string zoneAsset, params (string enemyAsset, int count)[] rows)
		{
			ZoneDefinition zone = AssetDatabase.LoadAssetAtPath<ZoneDefinition>(ZoneRoot + zoneAsset + ".asset");
			if (zone == null)
			{
				throw new FileNotFoundException(ZoneRoot + zoneAsset + ".asset");
			}
			List<(EnemyDefinition definition, int count)> resolved = new List<(EnemyDefinition, int)>(rows.Length);
			foreach ((string enemyAsset, int count) row in rows)
			{
				string path = EnemyRoot + row.enemyAsset + ".asset";
				EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
				if (definition == null)
				{
					// Buildreihenfolge tolerieren: Wildling-/Eidra-Builder dürfen
					// laufen, bevor die Tier-2-Serie gebaut ist. Die Tests der
					// Fixrunde erzwingen den Vollzustand.
					Debug.LogWarning("Zone enemy population: '" + path + "' fehlt noch; Zeile übersprungen.");
				}
				else
				{
					resolved.Add((definition, row.count));
				}
			}
			SerializedObject serialized = new SerializedObject(zone);
			SerializedProperty array = serialized.FindProperty("enemyAllocations");
			array.arraySize = resolved.Count;
			for (int index = 0; index < resolved.Count; index++)
			{
				SerializedProperty element = array.GetArrayElementAtIndex(index);
				element.FindPropertyRelative("definition").objectReferenceValue = resolved[index].definition;
				element.FindPropertyRelative("count").intValue = resolved[index].count;
			}
			serialized.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(zone);
		}
	}
}
