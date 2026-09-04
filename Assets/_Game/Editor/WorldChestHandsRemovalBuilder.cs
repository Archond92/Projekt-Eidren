using Eidren.Presentation;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// W-009: Entfernt die 2D-Handsprites (OpeningHand_Left/Right) aus den
	/// drei Gebietskisten-Prefabs und leert die Referenzen im
	/// WorldChestVisual. Die Oeffnung erzaehlt jetzt die kniende Spielfigur
	/// (Oeffnen-Clip); erledigt zugleich F-005. Idempotent.
	/// </summary>
	public static class WorldChestHandsRemovalBuilder
	{
		private static readonly string[] PrefabPaths =
		{
			"Assets/_Game/Prefabs/Loot/WorldChests/WorldChest_Common.prefab",
			"Assets/_Game/Prefabs/Loot/WorldChests/WorldChest_Guarded.prefab",
			"Assets/_Game/Prefabs/Loot/WorldChests/WorldChest_Hidden.prefab"
		};

		[MenuItem("Eidren/V0.2/W-009 Kistenhaende entfernen")]
		public static void Build()
		{
			foreach (string path in PrefabPaths)
			{
				GameObject root = PrefabUtility.LoadPrefabContents(path);
				try
				{
					WorldChestVisual visual = root.GetComponentInChildren<WorldChestVisual>(true);
					if (visual != null)
					{
						SerializedObject serialized = new SerializedObject(visual);
						serialized.FindProperty("leftHand").objectReferenceValue = null;
						serialized.FindProperty("rightHand").objectReferenceValue = null;
						serialized.ApplyModifiedPropertiesWithoutUndo();
					}
					Transform[] children = root.GetComponentsInChildren<Transform>(true);
					foreach (Transform child in children)
					{
						if (child != null && child.name.StartsWith("OpeningHand", StringComparison.Ordinal))
						{
							UnityEngine.Object.DestroyImmediate(child.gameObject);
						}
					}
					if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
					{
						throw new InvalidOperationException(path + " liess sich nicht speichern.");
					}
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
			}
			AssetDatabase.SaveAssets();
			Debug.Log("[W009] 2D-Kistenhaende entfernt; die kniende Figur uebernimmt die Oeffnung.");
		}
	}
}
