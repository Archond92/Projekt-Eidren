using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests
{
	/// <summary>
	/// F34-001/F34-002/F34-003: Alle Mid-Poly-LODGroups muessen bei der normalen
	/// Spielkamera (orthografisch 7,4, Qualitaetsstufe Medium mit LOD-Bias 0,7)
	/// sichtbar bleiben. Vegetation, Ressourcen und Umweltkit zeigen dort
	/// ausserdem ihre feinste Stufe, sonst fehlen die hellen Blattspitzen der
	/// Buesche und die Faserpflanze verschwindet ganz.
	/// </summary>
	public sealed class MidpolyOrthographicLodTests
	{
		private static readonly string[] Lod0Folders =
		{
			"Assets/_Game/Prefabs/Environment/StyleProof",
			"Assets/_Game/Prefabs/Environment/AreaArtVariants",
			"Assets/_Game/Prefabs/Resources/Visuals",
			// OP-11: Gebaeude, Truhen und Loot zeigen im Spielzoom ebenfalls LOD0.
			"Assets/_Game/Prefabs/Buildings",
			"Assets/_Game/Prefabs/Containers/Forge",
			"Assets/_Game/Prefabs/Loot/WorldChests",
			"Assets/_Game/Prefabs/Items",
			"Assets/_Game/Prefabs/Stations",
			"Assets/_Game/Resources/Prefabs",
		};

		private static readonly string[] VisibleFolders =
		{
			"Assets/_Game/Prefabs/Actors/3D",
		};

		[Test]
		public void VegetationRessourcenUndUmweltkit_ZeigenBeiDerSpielkameraLod0()
		{
			List<string> failures = new List<string>();
			int checkedGroups = 0;
			foreach (LODGroup group in LoadGroups(Lod0Folders))
			{
				if (AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(group) ?? group).Contains("/Actors/"))
				{
					// Kreaturen (auch als verschachtelte Begleiter-Prefabs) behalten ihre LOD-Kette; fuer sie gilt nur die Ausblendgrenze.
					continue;
				}
				checkedGroups++;
				float height = Eidren.Editor.MidpolyLodThresholds.ScreenHeightAtGameCamera(group.size);
				LOD[] lods = group.GetLODs();
				if (lods[0].screenRelativeTransitionHeight >= height)
				{
					failures.Add($"{group.gameObject.name}: LOD0 ab {lods[0].screenRelativeTransitionHeight:F4}, Objekt nur {height:F4} hoch");
				}
			}
			Assert.That(checkedGroups, Is.GreaterThanOrEqualTo(30), "Prefab-Ordner nicht gefunden?");
			Assert.That(failures, Is.Empty, string.Join("\n", failures));
		}

		[Test]
		public void AlleMidpolyLodGroups_WerdenBeiDerSpielkameraNichtAusgeblendet()
		{
			List<string> failures = new List<string>();
			int checkedGroups = 0;
			foreach (LODGroup group in LoadGroups(Lod0Folders.Concat(VisibleFolders).ToArray()))
			{
				checkedGroups++;
				float height = Eidren.Editor.MidpolyLodThresholds.ScreenHeightAtGameCamera(group.size);
				LOD[] lods = group.GetLODs();
				float cull = lods[lods.Length - 1].screenRelativeTransitionHeight;
				if (cull >= height)
				{
					failures.Add($"{group.gameObject.name}: ausgeblendet ab {cull:F4}, Objekt nur {height:F4} hoch");
				}
			}
			Assert.That(checkedGroups, Is.GreaterThanOrEqualTo(60), "Prefab-Ordner nicht gefunden?");
			Assert.That(failures, Is.Empty, string.Join("\n", failures));
		}

		[Test]
		public void Schwellenformel_LiefertFallendeWerteUndDieAusblendgrenzeAusF33001()
		{
			float height = Eidren.Editor.MidpolyLodThresholds.ScreenHeightAtGameCamera(1.5f);
			Assert.That(height, Is.EqualTo(1.5f / 14.8f * 0.7f).Within(0.0001f));
			float t0 = Eidren.Editor.MidpolyLodThresholds.ThresholdFor(0, 3, height);
			float t1 = Eidren.Editor.MidpolyLodThresholds.ThresholdFor(1, 3, height);
			float t2 = Eidren.Editor.MidpolyLodThresholds.ThresholdFor(2, 3, height);
			Assert.That(t0, Is.LessThan(height));
			Assert.That(t0, Is.GreaterThan(t1));
			Assert.That(t1, Is.GreaterThan(t2));
			Assert.That(t2, Is.LessThanOrEqualTo(Eidren.Editor.MidpolyLodThresholds.CullHeight));
		}

		private static IEnumerable<LODGroup> LoadGroups(string[] folders)
		{
			foreach (string folder in folders)
			{
				if (!Directory.Exists(folder))
				{
					continue;
				}
				foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
				{
					GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
					if (prefab == null)
					{
						continue;
					}
					foreach (LODGroup group in prefab.GetComponentsInChildren<LODGroup>(true))
					{
						if (group.GetLODs().Length > 0)
						{
							yield return group;
						}
					}
				}
			}
		}
	}
}
