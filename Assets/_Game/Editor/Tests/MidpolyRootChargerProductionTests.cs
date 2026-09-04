using Eidren.Presentation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyRootChargerProductionTests
	{
		private const string PrefabPath = "Assets/_Game/Prefabs/Actors/3D/RootCharger_3D.prefab";
		private const string EnemyPath = "Assets/_Game/Prefabs/Enemies/TierTwo/RootCharger.prefab";
		private const string GlbPath = "Assets/_Game/Art/MidPoly/GoldenMasters/RootCharger/Runtime/CRE_RootCharger_Mid_Production.glb";
		private static readonly string[] ExpectedClips =
		{
			"Angriff", "Erscheinen", "Gehen", "Ruhe", "Taumeln", "Telegraph", "Tod", "Treffer"
		};

		[Test]
		public void RootChargerPrefab_PreservesProductionContract()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(prefab, Is.Not.Null);
			Assert.That(AssetDatabase.AssetPathToGUID(PrefabPath), Is.EqualTo("42013866670934843a6e96f462d6a3ed"));
			Assert.That(AssetDatabase.GetLabels(prefab), Does.Contain("RootChargerGoldenMaster"));
			Assert.That(prefab.GetComponent<CreatureMeshPresentation>(), Is.Not.Null);
			Assert.That(prefab.GetComponent<CreatureMeshPresentation>().WorldHeight, Is.EqualTo(2.20f).Within(0.001f));
			Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);

			LODGroup group = prefab.GetComponent<LODGroup>();
			Assert.That(group, Is.Not.Null);
			LOD[] lods = group.GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3));
			Assert.That(lods.Select(lod => lod.screenRelativeTransitionHeight),
				Is.EqualTo(new[] { 0.55f, 0.25f, 0.01f /* F34-003 */ }).Within(0.001f));

			SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			Assert.That(RenderersFor(renderers, "LOD0"), Is.Not.Empty);
			Assert.That(RenderersFor(renderers, "LOD1"), Is.Not.Empty);
			Assert.That(RenderersFor(renderers, "LOD2"), Is.Not.Empty);
			Assert.That(Triangles(RenderersFor(renderers, "LOD0")), Is.EqualTo(25172));
			Assert.That(Triangles(RenderersFor(renderers, "LOD1")), Is.EqualTo(13592));
			Assert.That(Triangles(RenderersFor(renderers, "LOD2")), Is.EqualTo(5528));
			Assert.That(renderers.SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null).Select(material => material.name).Distinct().Count(), Is.EqualTo(2));

			Animation animation = prefab.GetComponentInChildren<Animation>(true);
			Assert.That(animation, Is.Not.Null);
			Assert.That(animation.Cast<AnimationState>().Select(state => state.name).OrderBy(name => name),
				Is.EqualTo(ExpectedClips));

			GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
			Assert.That(AssetDatabase.AssetPathToGUID(EnemyPath), Is.EqualTo("873800d3c030313479e6dd6d4fdb8bb9"));
			CapsuleCollider collider = enemy.GetComponent<CapsuleCollider>();
			Assert.That(collider.radius, Is.EqualTo(0.6136f).Within(0.001f));
			Assert.That(collider.height, Is.EqualTo(2.478f).Within(0.001f));
			Assert.That(enemy.GetComponentInChildren<CreatureMeshPresentation>(true), Is.Not.Null);
		}

		[Test]
		public void RootChargerSkin_AllLodsHaveNormalizedWeightsAndExactRig()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			foreach (SkinnedMeshRenderer renderer in renderers)
			{
				Assert.That(renderer.bones, Has.Length.EqualTo(19), renderer.name);
				Assert.That(renderer.rootBone, Is.Not.Null, renderer.name);
				foreach (BoneWeight weight in renderer.sharedMesh.boneWeights)
				{
					float sum = weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3;
					Assert.That(sum, Is.EqualTo(1f).Within(0.002f), renderer.name);
				}
			}
		}

		[Test]
		public void RootChargerClips_SampleFiniteAndKeepReadableFootprint()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				Dictionary<string, AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
					.OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
					.ToDictionary(clip => clip.name, StringComparer.Ordinal);
				Assert.That(clips.Keys.OrderBy(name => name), Is.EqualTo(ExpectedClips));
				Transform root = Find(instance.transform, "root");
				Transform[] feet = { Find(instance.transform, "footFL"), Find(instance.transform, "footFR"),
					Find(instance.transform, "footRL"), Find(instance.transform, "footRR") };
				Assert.That(root, Is.Not.Null);
				Assert.That(feet, Has.None.Null);

				foreach (AnimationClip clip in clips.Values)
				{
					Vector3 rootStart = Vector3.zero;
					Vector3 rootEnd = Vector3.zero;
					for (int sample = 0; sample <= 6; sample++)
					{
						clip.SampleAnimation(instance, clip.length * sample / 6f);
						AssertHierarchyFinite(instance.transform, clip.name);
						Bounds bounds = CombinedLod0Bounds(instance);
						Assert.That(bounds.size.x, Is.LessThan(2.25f), clip.name);
						Assert.That(bounds.size.y, Is.LessThan(2.80f), clip.name);
						Assert.That(bounds.size.z, Is.LessThan(3.20f), clip.name);
						Assert.That(bounds.min.y, Is.GreaterThan(-0.45f), clip.name);
						if (sample == 0) rootStart = root.position;
						if (sample == 6) rootEnd = root.position;
					}
					Vector2 drift = new Vector2(rootEnd.x - rootStart.x, rootEnd.z - rootStart.z);
					Assert.That(drift.magnitude, Is.LessThan(0.85f), clip.name + " besitzt unerwartete Root-Motion.");
					if (clip.name == "Ruhe" || clip.name == "Gehen" || clip.name == "Telegraph")
					{
						clip.SampleAnimation(instance, clip.length * 0.5f);
						Assert.That(feet.Min(foot => Mathf.Abs(foot.position.y)), Is.LessThan(0.42f),
							clip.name + " verliert den Bodenkontakt aller vier Pranken.");
					}
				}
			}
			finally { UnityEngine.Object.DestroyImmediate(instance); }
		}

		private static Renderer[] RenderersFor(IEnumerable<SkinnedMeshRenderer> renderers, string lod)
		{
			return renderers.Where(renderer => HierarchyContains(renderer.transform, lod)).Cast<Renderer>().ToArray();
		}

		private static bool HierarchyContains(Transform transform, string value)
		{
			for (Transform current = transform; current != null; current = current.parent)
				if (current.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
			return false;
		}

		private static int Triangles(IEnumerable<Renderer> renderers)
		{
			return renderers.OfType<SkinnedMeshRenderer>().Select(renderer => renderer.sharedMesh).Distinct()
				.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount).Sum(index => (int)mesh.GetIndexCount(index) / 3));
		}

		private static Transform Find(Transform root, string name)
		{
			if (root.name == name) return root;
			for (int i = 0; i < root.childCount; i++)
			{
				Transform result = Find(root.GetChild(i), name);
				if (result != null) return result;
			}
			return null;
		}

		private static void AssertHierarchyFinite(Transform root, string clip)
		{
			foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
			{
				Vector3 position = transform.position;
				Vector3 scale = transform.lossyScale;
				Quaternion rotation = transform.rotation;
				Assert.That(float.IsNaN(position.x) || float.IsInfinity(position.x), Is.False, clip + "/" + transform.name);
				Assert.That(float.IsNaN(scale.x) || float.IsInfinity(scale.x), Is.False, clip + "/" + transform.name);
				Assert.That(float.IsNaN(rotation.w) || float.IsInfinity(rotation.w), Is.False, clip + "/" + transform.name);
			}
		}

		private static Bounds CombinedLod0Bounds(GameObject instance)
		{
			Renderer[] renderers = RenderersFor(instance.GetComponentsInChildren<SkinnedMeshRenderer>(true), "LOD0");
			Bounds bounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
			return bounds;
		}
	}
}
