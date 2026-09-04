using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Interaction;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyResourcePilotTests
	{
		private static readonly (string Asset, int Lod0, int Lod1, int Lod2, int Exhausted)[] Specs =
		{
			("Tree", 7856, 4320, 1368, 1236),
			("StoneDeposit", 1022, 562, 223, 1114),
			("CopperVein", 1042, 572, 227, 1134),
			("BerryBush", 3816, 2098, 784, 1424),
			("FiberPlant", 3836, 2108, 692, 1290)
		};

		[Test]
		public void ApprovedResourcePairsUseMidPolyLodsAndRuntimeAnimation()
		{
			foreach ((string asset, int lod0, int lod1, int lod2, int exhausted) in Specs)
			{
				GameObject active = LoadVisual(asset, "Active");
				GameObject spent = LoadVisual(asset, "Exhausted");
				Assert.That(AssetDatabase.GetLabels(active), Does.Contain("Approved"), asset);
				Assert.That(active.GetComponent<LODGroup>().GetLODs(), Has.Length.EqualTo(3), asset);
				Assert.That(active.GetComponent<MidpolyResourceAnimationDriver>().IsConfigured, Is.True, asset);
				Assert.That(Triangles(active.transform.Find("LOD0")), Is.EqualTo(lod0), asset + " LOD0");
				Assert.That(Triangles(active.transform.Find("LOD1")), Is.EqualTo(lod1), asset + " LOD1");
				Assert.That(Triangles(active.transform.Find("LOD2")), Is.EqualTo(lod2), asset + " LOD2");
				Assert.That(Triangles(spent.transform.Find("LOD0")), Is.EqualTo(exhausted), asset + " Exhausted");
				Assert.That(active.GetComponentsInChildren<Collider>(true), Is.Empty, asset + " Visual-Collider");
				Assert.That(spent.GetComponentsInChildren<Collider>(true), Is.Empty, asset + " Exhausted-Visual-Collider");
			}
		}

		[Test]
		public void ResourceDefinitionsAndNodesKeepTheApprovedPairs()
		{
			foreach ((string asset, _, _, _, _) in Specs)
			{
				ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/_Game/Data/Resources/" + asset + ".asset");
				Assert.That(definition, Is.Not.Null, asset);
				Assert.That(definition.ActiveVisualPrefab, Is.SameAs(LoadVisual(asset, "Active")), asset);
				Assert.That(definition.ExhaustedVisualPrefab, Is.SameAs(LoadVisual(asset, "Exhausted")), asset);
				Assert.That(definition.NodePrefab, Is.Not.Null, asset);
				ResourceNode node = definition.NodePrefab.GetComponent<ResourceNode>();
				Assert.That(node, Is.Not.Null, asset);
				Assert.That(node.GetComponentsInChildren<MidpolyResourceAnimationDriver>(true), Has.Length.EqualTo(1), asset);
			}
		}

		private static GameObject LoadVisual(string asset, string state)
		{
			string path = "Assets/_Game/Prefabs/Resources/Visuals/" + asset + "_" + state + ".prefab";
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			Assert.That(prefab, Is.Not.Null, path);
			return prefab;
		}

		private static int Triangles(Transform root)
		{
			return root.GetComponentsInChildren<MeshFilter>(true)
				.Where(filter => filter.sharedMesh != null)
				.Sum(filter => Enumerable.Range(0, filter.sharedMesh.subMeshCount)
					.Sum(index => (int)filter.sharedMesh.GetIndexCount(index) / 3));
		}
	}
}
