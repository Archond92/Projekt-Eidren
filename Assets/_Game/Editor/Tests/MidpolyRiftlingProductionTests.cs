using Eidren.Presentation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyRiftlingProductionTests
	{
		private const string PrefabPath = "Assets/_Game/Prefabs/Actors/3D/Riftling_3D.prefab";
		private const string EnemyPath = "Assets/_Game/Prefabs/Enemies/TierTwo/Riftling.prefab";
		private const string GlbPath = "Assets/_Game/Art/MidPoly/Creatures/Riftling/Runtime/CRE_Riftling_Mid_Production.glb";
		private static readonly string[] ExpectedClips =
		{
			"Angriff", "Erscheinen", "Gehen", "Ruhe", "Taumeln", "Telegraph", "Tod", "Treffer"
		};

		[Test]
		public void RiftlingPrefab_PreservesProductionContract()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(prefab, Is.Not.Null);
			Assert.That(AssetDatabase.AssetPathToGUID(PrefabPath), Is.EqualTo("99f93ac7e8a4c1949a23b066046844e8"));
			Assert.That(AssetDatabase.GetLabels(prefab), Does.Contain("RiftlingProduction"));
			Assert.That(prefab.GetComponent<CreatureMeshPresentation>(), Is.Not.Null);
			Assert.That(prefab.GetComponent<CreatureMeshPresentation>().WorldHeight, Is.EqualTo(1.70f).Within(0.001f));
			Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);

			LOD[] lods = prefab.GetComponent<LODGroup>().GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3));
			Assert.That(lods.Select(lod => lod.screenRelativeTransitionHeight),
				Is.EqualTo(new[] { 0.55f, 0.25f, 0.01f /* F34-003 */ }).Within(0.001f));
			SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			Assert.That(Triangles(RenderersFor(renderers, "LOD0")), Is.EqualTo(15376));
			Assert.That(Triangles(RenderersFor(renderers, "LOD1")), Is.EqualTo(8302));
			Assert.That(Triangles(RenderersFor(renderers, "LOD2")), Is.EqualTo(3382));
			Assert.That(renderers.SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null).Select(material => material.name).Distinct().Count(), Is.EqualTo(3));
			Animation animation = prefab.GetComponentInChildren<Animation>(true);
			Assert.That(animation.Cast<AnimationState>().Select(state => state.name).OrderBy(name => name), Is.EqualTo(ExpectedClips));
			GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
			Assert.That(AssetDatabase.AssetPathToGUID(EnemyPath), Is.EqualTo("60c418f9d6491814fad6e3a97e7970d2"));
			CapsuleCollider collider = enemy.GetComponent<CapsuleCollider>();
			Assert.That(collider.radius, Is.EqualTo(0.4576f).Within(0.001f));
			Assert.That(collider.height, Is.EqualTo(1.848f).Within(0.001f));
			Assert.That(enemy.GetComponentInChildren<CreatureMeshPresentation>(true), Is.Not.Null);
		}

		[Test]
		public void RiftlingSkin_AllLodsHaveNormalizedWeightsAndExactRig()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			foreach (SkinnedMeshRenderer renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				Assert.That(renderer.bones, Has.Length.EqualTo(17), renderer.name);
				Assert.That(renderer.rootBone, Is.Not.Null, renderer.name);
				foreach (BoneWeight weight in renderer.sharedMesh.boneWeights)
					Assert.That(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3,
						Is.EqualTo(1f).Within(0.002f), renderer.name);
			}
		}

		[Test]
		public void RiftlingClips_SampleFiniteAndKeepReadableFootprint()
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
				Transform[] feet = { Find(instance.transform, "footL"), Find(instance.transform, "footR") };
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
						Assert.That(bounds.size.x, Is.LessThan(2.90f), clip.name);
						Assert.That(bounds.size.y, Is.LessThan(2.70f), clip.name);
						Assert.That(bounds.size.z, Is.LessThan(3.10f), clip.name);
						Assert.That(bounds.min.y, Is.GreaterThan(-0.55f), clip.name);
						if (sample == 0) rootStart = root.position;
						if (sample == 6) rootEnd = root.position;
					}
					Assert.That(new Vector2(rootEnd.x - rootStart.x, rootEnd.z - rootStart.z).magnitude,
						Is.LessThan(0.85f), clip.name + " besitzt unerwartete Root-Motion.");
					if (clip.name == "Ruhe" || clip.name == "Gehen" || clip.name == "Telegraph")
					{
						clip.SampleAnimation(instance, clip.length * 0.5f);
						Assert.That(feet.Min(foot => Mathf.Abs(foot.position.y)), Is.LessThan(0.45f),
							clip.name + " verliert den Bodenkontakt beider Fuesse.");
					}
				}
			}
			finally { UnityEngine.Object.DestroyImmediate(instance); }
		}

		[Test]
		public void RiftlingAttack_IsOffsetRiftSlashWithPreservedTiming()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				AnimationClip attack = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
					.OfType<AnimationClip>().Single(clip => clip.name == "Angriff");
				Assert.That(attack.length, Is.InRange(0.65f, 0.68f),
					"Das bestehende Gameplay-Timing des Riftling-Angriffs wurde veraendert.");
				Animation animationPlayer = instance.GetComponentInChildren<Animation>(true);
				Assert.That(animationPlayer, Is.Not.Null);
				GameObject animationRoot = animationPlayer.gameObject;
				Transform leftUpperArm = Find(animationRoot.transform, "armL_up");
				Transform rightUpperArm = Find(animationRoot.transform, "armR_up");
				attack.SampleAnimation(animationRoot, attack.length * 9f / 16f);
				Assert.That(Quaternion.Angle(leftUpperArm.localRotation, rightUpperArm.localRotation),
					Is.GreaterThan(45f), "Der Riftling nutzt weiterhin den spiegelgleichen Zweiarmschlag.");
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
			for (int index = 0; index < root.childCount; index++)
			{
				Transform result = Find(root.GetChild(index), name);
				if (result != null) return result;
			}
			return null;
		}
		private static void AssertHierarchyFinite(Transform root, string clip)
		{
			foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
			{
				Assert.That(float.IsNaN(transform.position.x) || float.IsInfinity(transform.position.x), Is.False, clip + "/" + transform.name);
				Assert.That(float.IsNaN(transform.lossyScale.x) || float.IsInfinity(transform.lossyScale.x), Is.False, clip + "/" + transform.name);
				Assert.That(float.IsNaN(transform.rotation.w) || float.IsInfinity(transform.rotation.w), Is.False, clip + "/" + transform.name);
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

