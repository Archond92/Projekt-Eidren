using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Interaction;
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyTierTwoVegetationTests
	{
		private static readonly (string Asset, int Lod0, int Lod1, int Lod2, int Exhausted, float Height, float ExhaustedHeight)[] Specs =
		{
			("HardwoodTree", 7856, 4320, 1368, 1236, 6.65f, 0.84f),
			("SwampHemp", 3836, 2108, 692, 1290, 1.372f, 0.228f)
		};

		[Test]
		public void ApprovedVegetationPairsUseProductionLodsAnimationsAndDimensions()
		{
			foreach ((string asset, int lod0, int lod1, int lod2, int exhausted, float height, float exhaustedHeight) in Specs)
			{
				GameObject active = LoadVisual(asset, "Active");
				GameObject spent = LoadVisual(asset, "Exhausted");
				Assert.That(AssetDatabase.GetLabels(active), Does.Contain("Approved"), asset);
				Assert.That(AssetDatabase.GetLabels(active), Does.Contain("Phase5Vegetation"), asset);
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

				string glb = "Assets/_Game/Art/MidPoly/Resources/" + asset + "/Runtime/RES_" + asset + "_Active_Lod0_Animated.glb";
				string[] clips = AssetDatabase.LoadAllAssetsAtPath(glb).OfType<AnimationClip>().Select(clip => clip.name).ToArray();
				Assert.That(clips, Does.Contain("Idle_Sway"), asset);
				Assert.That(clips, Does.Contain("Harvest_Recoil"), asset);
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
				CapsuleCollider[] blockers = definition.NodePrefab.GetComponents<CapsuleCollider>();
				if (asset == "HardwoodTree")
				{
					Assert.That(blockers, Has.Length.EqualTo(1), asset);
					Assert.That(blockers[0].isTrigger, Is.False, asset);
					Assert.That(blockers[0].radius, Is.EqualTo(0.75f).Within(0.0001f), asset);
					Assert.That(blockers[0].height, Is.EqualTo(6f).Within(0.0001f), asset);
					Assert.That(blockers[0].center, Is.EqualTo(new Vector3(0f, 3f, 0f)), asset);
				}
				else
				{
					Assert.That(blockers, Is.Empty, asset);
				}
			}
		}

		[Test]
		public void TwilightGroveAndVeilMarshVariantsUseTheApprovedFamily()
		{
			string[] stems =
			{
				"TwilightGrove_hardwood_tree",
				"TwilightGrove_swamp_hemp",
				"VeilMarsh_swamp_hemp"
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
				.Sum(filter => Enumerable.Range(0, filter.sharedMesh.subMeshCount)
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
