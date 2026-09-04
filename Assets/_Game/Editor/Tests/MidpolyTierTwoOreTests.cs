using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Interaction;
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyTierTwoOreTests
	{
		private static readonly (string Asset, int Lod0, int Lod1, int Lod2, int Exhausted, float Height, float ExhaustedHeight)[] Specs =
		{
			("GraniteDeposit", 1022, 562, 223, 1114, 1.266f, 0.749f),
			("IronVein", 1042, 572, 227, 1134, 1.408f, 0.749f)
		};

		[Test]
		public void ApprovedOrePairsUseProductionLodsAnimationAndDimensions()
		{
			foreach ((string asset, int lod0, int lod1, int lod2, int exhausted, float height, float exhaustedHeight) in Specs)
			{
				GameObject active = LoadVisual(asset, "Active");
				GameObject spent = LoadVisual(asset, "Exhausted");
				Assert.That(AssetDatabase.GetLabels(active), Does.Contain("Approved"), asset);
				Assert.That(AssetDatabase.GetLabels(active), Does.Contain("Phase5Ore"), asset);
				Assert.That(active.GetComponent<LODGroup>().GetLODs(), Has.Length.EqualTo(3), asset);
				Assert.That(active.GetComponent<MidpolyResourceAnimationDriver>().IsConfigured, Is.True, asset);
				Assert.That(Triangles(active.transform.Find("LOD0")), Is.EqualTo(lod0), asset + " LOD0");
				Assert.That(Triangles(active.transform.Find("LOD1")), Is.EqualTo(lod1), asset + " LOD1");
				Assert.That(Triangles(active.transform.Find("LOD2")), Is.EqualTo(lod2), asset + " LOD2");
				Assert.That(Triangles(spent.transform.Find("LOD0")), Is.EqualTo(exhausted), asset + " Exhausted");
				Assert.That(Height(active), Is.EqualTo(height).Within(0.015f), asset + " Active height");
				Assert.That(Height(spent), Is.EqualTo(exhaustedHeight).Within(0.015f), asset + " Exhausted height");
				Assert.That(active.GetComponentsInChildren<Collider>(true), Is.Empty, asset + " Visual-Collider");
				Assert.That(spent.GetComponentsInChildren<Collider>(true), Is.Empty, asset + " Exhausted-Visual-Collider");
				Assert.That(active.transform.Find("ImpactPoint_00"), Is.Not.Null, asset);
				Assert.That(active.transform.Find("ImpactPoint_01"), Is.Not.Null, asset);
				Assert.That(active.transform.Find("ImpactPoint_02"), Is.Not.Null, asset);
			}
		}

		[Test]
		public void DefinitionsNodesAndCollidersKeepTheirGameplayContracts()
		{
			foreach ((string asset, _, _, _, _, _, _) in Specs)
			{
				ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/_Game/Data/Resources/" + asset + ".asset");
				Assert.That(definition, Is.Not.Null, asset);
				Assert.That(definition.ActiveVisualPrefab, Is.SameAs(LoadVisual(asset, "Active")), asset);
				Assert.That(definition.ExhaustedVisualPrefab, Is.SameAs(LoadVisual(asset, "Exhausted")), asset);
				Assert.That(definition.NodePrefab, Is.Not.Null, asset);
				Assert.That(definition.NodePrefab.GetComponent<ResourceNode>(), Is.Not.Null, asset);
				Assert.That(definition.NodePrefab.GetComponentsInChildren<MidpolyResourceAnimationDriver>(true), Has.Length.EqualTo(1), asset);

				SphereCollider interaction = definition.NodePrefab.GetComponents<SphereCollider>().Single();
				Assert.That(interaction.isTrigger, Is.True, asset);
				Assert.That(interaction.radius, Is.EqualTo(1.215f).Within(0.0001f), asset);
				CapsuleCollider blocking = definition.NodePrefab.GetComponents<CapsuleCollider>().Single();
				Assert.That(blocking.isTrigger, Is.False, asset);
				Assert.That(blocking.radius, Is.EqualTo(0.95f).Within(0.0001f), asset);
				Assert.That(blocking.height, Is.EqualTo(1.35f).Within(0.0001f), asset);
				Assert.That(blocking.center, Is.EqualTo(new Vector3(0f, 0.675f, 0f)), asset);
			}
		}

		[Test]
		public void GreyRiftsAndVeilMarshVariantsUseTheApprovedFamily()
		{
			string[] stems =
			{
				"GreyRifts_granite_deposit",
				"GreyRifts_iron_vein",
				"VeilMarsh_iron_vein"
			};
			foreach (string stem in stems)
			{
				GameObject active = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Environment/AreaArtVariants/" + stem + "_Active.prefab");
				GameObject spent = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Environment/AreaArtVariants/" + stem + "_Exhausted.prefab");
				Assert.That(active, Is.Not.Null, stem);
				Assert.That(spent, Is.Not.Null, stem);
				Assert.That(active.GetComponent<LODGroup>().GetLODs(), Has.Length.EqualTo(3), stem);
				Assert.That(active.GetComponent<MidpolyResourceAnimationDriver>().IsConfigured, Is.True, stem);
				Assert.That(spent.GetComponentInChildren<MeshRenderer>(true), Is.Not.Null, stem);
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
				.Sum(filter => (int)filter.sharedMesh.GetIndexCount(0) / 3 +
					Enumerable.Range(1, filter.sharedMesh.subMeshCount - 1)
						.Sum(index => (int)filter.sharedMesh.GetIndexCount(index) / 3));
		}

		private static float Height(GameObject prefab)
		{
			Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true)
				.Where(renderer => renderer.gameObject.name != "WorldContactShadow")
				.ToArray();
			Bounds bounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers.Skip(1))
			{
				bounds.Encapsulate(renderer.bounds);
			}
			return bounds.size.y;
		}
	}
}
