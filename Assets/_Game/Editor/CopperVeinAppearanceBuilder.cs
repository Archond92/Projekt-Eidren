using Eidren.Data;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// W-005 (zurueckgenommen am 15.08.2026): Setzt das Erhaltens-Flag der
	/// Kupferader auf den gewuenschten Stand, ohne die uebrigen Ressourcen neu
	/// zu bauen. Nach dem Spieltest gilt wieder der sichtbare Wechsel zur
	/// Abgebaut-Optik (Flag aus); der Mechanismus selbst bleibt bestehen.
	/// Idempotent.
	/// </summary>
	public static class CopperVeinAppearanceBuilder
	{
		private const string AssetPath = "Assets/_Game/Data/Resources/CopperVein.asset";

		[MenuItem("Eidren/V0.2/W-005 Kupferader-Optik (Ruecknahme: Abgebaut-Wechsel)")]
		public static void Build()
		{
			ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(AssetPath);
			if (definition == null)
			{
				throw new InvalidOperationException("Definition fehlt: " + AssetPath);
			}
			SerializedObject serialized = new SerializedObject(definition);
			SerializedProperty property = serialized.FindProperty("keepsAppearanceWhenExhausted");
			if (property == null)
			{
				throw new InvalidOperationException("Feld keepsAppearanceWhenExhausted fehlt an ResourceNodeDefinition.");
			}
			property.boolValue = false;
			serialized.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(definition);
			AssetDatabase.SaveAssets();
			Debug.Log("[W005] Ruecknahme aktiv: Kupferader wechselt nach dem Abbau wieder zur Abgebaut-Optik.");
		}
	}
}
