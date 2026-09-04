using Eidren.UI;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// W-004: Verdrahtet das Speer-Icon im Kampf-HUD. Der Presenter kannte nur
	/// Hammer- und Dolch-Icon (Stand v0.1); mit dem Speer als dritter Familie
	/// braucht der Angriffs- und Wechselbutton eine eigene Speer-Grafik.
	/// Idempotent: mehrfache Laeufe erzeugen denselben Endzustand.
	/// </summary>
	public static class WeaponIconHudBuilder
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";
		private const string SpearIconPath = "Assets/Sprite/ITEM_TMP_IronSpear.asset";

		[MenuItem("Eidren/V0.2/W-004 Speer-Icon im Kampf-HUD verdrahten")]
		public static void Build()
		{
			Sprite spear = AssetDatabase.LoadAssetAtPath<Sprite>(SpearIconPath);
			if (spear == null)
			{
				throw new InvalidOperationException("Speer-Icon fehlt: " + SpearIconPath);
			}
			GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
			try
			{
				CombatHudActionPresenter presenter = root.GetComponentInChildren<CombatHudActionPresenter>(true);
				if (presenter == null)
				{
					throw new InvalidOperationException("CombatHudActionPresenter fehlt in " + PrefabPath);
				}
				SerializedObject serialized = new SerializedObject(presenter);
				SerializedProperty property = serialized.FindProperty("spearIcon");
				if (property == null)
				{
					throw new InvalidOperationException("Feld spearIcon fehlt am Presenter.");
				}
				property.objectReferenceValue = spear;
				serialized.ApplyModifiedPropertiesWithoutUndo();
				if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
				{
					throw new InvalidOperationException("CombatHUD.prefab liess sich nicht speichern.");
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
			AssetDatabase.SaveAssets();
			Debug.Log("[W004] Speer-Icon im Kampf-HUD verdrahtet.");
		}
	}
}
