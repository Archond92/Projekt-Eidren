using Eidren.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// W-009: Die 2D-Handsymbole an den Gebietskisten entfallen — die
	/// Oeffnung erzaehlt jetzt die kniende 3D-Figur (Oeffnen-Clip). Erledigt
	/// zugleich F-005 (doppelte Kontrollbutton-Glyphe an der Kiste).
	/// </summary>
	public sealed class WorldChestHandsTests
	{
		private static readonly string[] ChestPrefabPaths =
		{
			"Assets/_Game/Prefabs/Loot/WorldChests/WorldChest_Common.prefab",
			"Assets/_Game/Prefabs/Loot/WorldChests/WorldChest_Guarded.prefab",
			"Assets/_Game/Prefabs/Loot/WorldChests/WorldChest_Hidden.prefab"
		};

		[Test]
		public void Kistenprefabs_TragenKeineHandSpritesMehr()
		{
			foreach (string path in ChestPrefabPaths)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				Assert.That(prefab, Is.Not.Null, path);
				foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
				{
					Assert.That(child.name, Does.Not.StartWith("OpeningHand"),
						path + " traegt noch " + child.name);
				}
				WorldChestVisual visual = prefab.GetComponentInChildren<WorldChestVisual>(true);
				if (visual != null)
				{
					SerializedObject serialized = new SerializedObject(visual);
					Assert.That(serialized.FindProperty("leftHand").objectReferenceValue == null, Is.True,
						path + ": leftHand-Referenz muss leer sein");
					Assert.That(serialized.FindProperty("rightHand").objectReferenceValue == null, Is.True,
						path + ": rightHand-Referenz muss leer sein");
				}
			}
		}
	}
}
