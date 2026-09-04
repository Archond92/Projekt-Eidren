using Eidren.UI;
using System.IO;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Repariert die Weltanzeigen aller Gegner (F-011): Jede gefuellte Grafik
	/// braucht eine Fuellgrafik, sonst ignoriert Unity den Wert und der Balken
	/// steht dauerhaft voll. Der Staggerbalken beginnt leer. Idempotent.
	/// </summary>
	public static class FixCollectionEnemyBarBuilder
	{
		private const string EnemyPrefabFolder = "Assets/_Game/Prefabs/Enemies";

		[MenuItem("Eidren/V0.2/Fixes/Upgrade Enemy Status Bars")]
		public static void UpgradeEnemyStatusBars()
		{
			Sprite bar = FixCollectionHudBuilder.EnsureBarSprite();
			int patchedPrefabs = 0;
			int patchedSprites = 0;
			int patchedStagger = 0;
			string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[1] { EnemyPrefabFolder });
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				GameObject root = PrefabUtility.LoadPrefabContents(path);
				try
				{
					if (root.GetComponentInChildren<WildlingStatusBars>(includeInactive: true) == null)
					{
						continue;
					}
					bool changed = false;
					Image[] images = root.GetComponentsInChildren<Image>(includeInactive: true);
					foreach (Image image in images)
					{
						if (image.type != Image.Type.Filled)
						{
							continue;
						}
						if (image.sprite == null)
						{
							image.sprite = bar;
							patchedSprites++;
							changed = true;
						}
						if (image.name.StartsWith("Stagger", System.StringComparison.Ordinal) && image.fillAmount != 0f)
						{
							image.fillAmount = 0f;
							patchedStagger++;
							changed = true;
						}
					}
					if (changed)
					{
						if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
						{
							throw new IOException("Could not save enemy prefab: " + path);
						}
						patchedPrefabs++;
					}
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
			}
			Debug.Log($"[F-011] Gegner-Prefabs angepasst: {patchedPrefabs}; Fuellgrafiken gesetzt: {patchedSprites}; Staggerbalken geleert: {patchedStagger}");
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}
