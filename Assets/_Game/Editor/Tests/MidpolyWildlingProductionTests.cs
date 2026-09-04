using Eidren.Presentation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyWildlingProductionTests
	{
		private const string PrefabPath = "Assets/_Game/Prefabs/Actors/3D/Wildling_3D.prefab";
		private const string EnemyPath = "Assets/_Game/Prefabs/Enemies/Wildling.prefab";
		private const string GlbPath = "Assets/_Game/Art/MidPoly/GoldenMasters/Wildling/Runtime/CHR_Wildling_Mid_Production.glb";

		private static readonly string[] ExpectedClips =
		{
			"Angriff", "Erscheinen", "Gehen", "Ruhe", "Taumeln", "Telegraph", "Tod", "Treffer"
		};

		[Test]
		public void WildlingPrefab_PreservesProductionContract()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(prefab, Is.Not.Null);
			Assert.That(AssetDatabase.AssetPathToGUID(PrefabPath), Is.EqualTo("b31d4ff87020a7d47a2bcb86b984b5eb"));
			Assert.That(AssetDatabase.GetLabels(prefab), Does.Contain("WildlingGoldenMaster"));
			Assert.That(prefab.GetComponent<CreatureMeshPresentation>(), Is.Not.Null);
			Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty,
				"Der Visual-Prefab darf den Gameplay-Collider nicht duplizieren.");

			LODGroup group = prefab.GetComponent<LODGroup>();
			Assert.That(group, Is.Not.Null);
			LOD[] lods = group.GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3));
			Assert.That(lods.Select(lod => lod.screenRelativeTransitionHeight),
				Is.EqualTo(new[] { 0.55f, 0.25f, 0.01f /* F34-003 */ }).Within(0.001f));

			SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			Assert.That(Triangles(RenderersFor(renderers, "LOD0")), Is.EqualTo(32594));
			Assert.That(Triangles(RenderersFor(renderers, "LOD1")), Is.EqualTo(18520));
			Assert.That(Triangles(RenderersFor(renderers, "LOD2")), Is.EqualTo(7408));
			Assert.That(renderers.SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null).Select(material => material.name).Distinct().Count(), Is.EqualTo(4));

			Animation animationPlayer = prefab.GetComponentInChildren<Animation>(true);
			Assert.That(animationPlayer, Is.Not.Null);
			Assert.That(animationPlayer.Cast<AnimationState>().Select(state => state.name).OrderBy(name => name),
				Is.EqualTo(ExpectedClips));

			GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
			CapsuleCollider collider = enemy.GetComponent<CapsuleCollider>();
			Assert.That(collider.radius, Is.EqualTo(0.52f).Within(0.001f));
			Assert.That(collider.height, Is.EqualTo(2.10f).Within(0.001f));
			Assert.That(enemy.GetComponentInChildren<CreatureMeshPresentation>(true), Is.Not.Null,
				"Der produktive Gegner muss ueber den erhaltenen Prefab-GUID auf den Mid-Poly-Wildling zeigen.");
		}

		[Test]
		public void WildlingSkin_AllLodsHaveNormalizedWeightsAndExactRig()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			Assert.That(renderers, Has.Length.EqualTo(12));
			Assert.That(RenderersFor(renderers, "LOD0"), Has.Length.EqualTo(4));
			Assert.That(RenderersFor(renderers, "LOD1"), Has.Length.EqualTo(4));
			Assert.That(RenderersFor(renderers, "LOD2"), Has.Length.EqualTo(4));
			foreach (SkinnedMeshRenderer renderer in renderers)
			{
				Assert.That(renderer.bones, Has.Length.EqualTo(17), renderer.name);
				Assert.That(renderer.rootBone, Is.Not.Null, renderer.name);
				foreach (BoneWeight weight in renderer.sharedMesh.boneWeights)
				{
					float sum = weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3;
					Assert.That(sum, Is.EqualTo(1f).Within(0.002f), renderer.name);
				}
			}
		}

		[Test]
		public void WildlingClips_SampleWithoutInvalidTransformsOrGroundLoss()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				Dictionary<string, AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
					.OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
					.ToDictionary(clip => clip.name, StringComparer.Ordinal);
				Assert.That(clips.Keys.OrderBy(name => name), Is.EqualTo(ExpectedClips));
				Transform footL = Find(instance.transform, "footL");
				Transform footR = Find(instance.transform, "footR");
				Transform root = Find(instance.transform, "root");
				Assert.That(footL, Is.Not.Null);
				Assert.That(footR, Is.Not.Null);
				Assert.That(root, Is.Not.Null);

				foreach (AnimationClip clip in clips.Values)
				{
					Vector3 rootStart = Vector3.zero;
					Vector3 rootEnd = Vector3.zero;
					for (int sample = 0; sample <= 4; sample++)
					{
						clip.SampleAnimation(instance, clip.length * sample / 4f);
						AssertHierarchyFinite(instance.transform, clip.name);
						Bounds bounds = CombinedLod0Bounds(instance);
						Assert.That(bounds.size.x, Is.LessThan(2.60f), clip.name);
						Assert.That(bounds.size.y, Is.LessThan(2.80f), clip.name);
						Assert.That(bounds.size.z, Is.LessThan(2.60f), clip.name);
						Assert.That(bounds.min.y, Is.GreaterThan(-0.40f), clip.name);
						if (sample == 0) rootStart = root.position;
						if (sample == 4) rootEnd = root.position;
					}
					Vector2 rootDrift = new Vector2(rootEnd.x - rootStart.x, rootEnd.z - rootStart.z);
					Assert.That(rootDrift.magnitude, Is.LessThan(0.40f), clip.name + " besitzt unerwartete Root-Motion.");

					if (clip.name == "Ruhe" || clip.name == "Gehen")
					{
						clip.SampleAnimation(instance, clip.length * 0.5f);
						Assert.That(Mathf.Min(Mathf.Abs(footL.position.y), Mathf.Abs(footR.position.y)),
							Is.LessThan(0.36f), clip.name + " verliert den Bodenkontakt beider Fuesse.");
					}
				}
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
		}

		[Test]
		public void WildlingAttack_IsAsymmetricClawStrikeWithPreservedTiming()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				AnimationClip attack = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
					.OfType<AnimationClip>().Single(clip => clip.name == "Angriff");
				Assert.That(attack.length, Is.InRange(0.78f, 0.81f),
					"Das bestehende Gameplay-Timing des Angriffs wurde veraendert.");
				Animation animationPlayer = instance.GetComponentInChildren<Animation>(true);
				Assert.That(animationPlayer, Is.Not.Null);
				GameObject animationRoot = animationPlayer.gameObject;
				Transform leftUpperArm = Find(animationRoot.transform, "armL_up");
				Transform rightUpperArm = Find(animationRoot.transform, "armR_up");
				attack.SampleAnimation(animationRoot, attack.length * 10f / 19f);
				Assert.That(Quaternion.Angle(leftUpperArm.localRotation, rightUpperArm.localRotation),
					Is.GreaterThan(45f), "Der Angriff ist weiterhin ein spiegelgleicher Zweiarmschlag.");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
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
