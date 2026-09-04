using Eidren.Presentation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyMoorThrowerProductionTests
	{
		private const string PrefabPath = "Assets/_Game/Prefabs/Actors/3D/MoorThrower_3D.prefab";
		private const string EnemyPath = "Assets/_Game/Prefabs/Enemies/TierTwo/MoorThrower.prefab";
		private const string GlbPath = "Assets/_Game/Art/MidPoly/GoldenMasters/MoorThrower/Runtime/CRE_MoorThrower_Mid_Production.glb";
		private static readonly string[] ExpectedClips =
		{
			"Angriff", "Erscheinen", "Gehen", "Ruhe", "Taumeln", "Telegraph", "Tod", "Treffer"
		};

		[Test]
		public void MoorThrowerPrefab_PreservesProductionContract()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(prefab, Is.Not.Null);
			Assert.That(AssetDatabase.AssetPathToGUID(PrefabPath), Is.EqualTo("bd92fc1ded2dbad4b8810a6aab560df2"));
			Assert.That(AssetDatabase.GetLabels(prefab), Does.Contain("MoorThrowerGoldenMaster"));
			Assert.That(prefab.GetComponent<CreatureMeshPresentation>(), Is.Not.Null);
			Assert.That(prefab.GetComponent<CreatureMeshPresentation>().WorldHeight, Is.EqualTo(1.90f).Within(0.001f));
			Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);

			LOD[] lods = prefab.GetComponent<LODGroup>().GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3));
			Assert.That(lods.Select(lod => lod.screenRelativeTransitionHeight),
				Is.EqualTo(new[] { 0.55f, 0.25f, 0.01f /* F34-003 */ }).Within(0.001f));
			SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			Assert.That(Triangles(RenderersFor(renderers, "LOD0")), Is.EqualTo(15680));
			Assert.That(Triangles(RenderersFor(renderers, "LOD1")), Is.EqualTo(8466));
			Assert.That(Triangles(RenderersFor(renderers, "LOD2")), Is.EqualTo(3436));
			Assert.That(renderers.SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null).Select(material => material.name).Distinct().Count(), Is.EqualTo(3));
			Animation animation = prefab.GetComponentInChildren<Animation>(true);
			Assert.That(animation.Cast<AnimationState>().Select(state => state.name).OrderBy(name => name), Is.EqualTo(ExpectedClips));
			Transform socket = Find(prefab.transform, "ProjectileSocket");
			Assert.That(socket, Is.Not.Null);
			Assert.That(HierarchyContains(socket, "handR"), Is.True);

			GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
			Assert.That(AssetDatabase.AssetPathToGUID(EnemyPath), Is.EqualTo("d19d8f6ced8032a4396758dd936618db"));
			CapsuleCollider collider = enemy.GetComponent<CapsuleCollider>();
			Assert.That(collider.radius, Is.EqualTo(0.4992f).Within(0.001f));
			Assert.That(collider.height, Is.EqualTo(2.016f).Within(0.001f));
			Assert.That(enemy.GetComponentInChildren<CreatureMeshPresentation>(true), Is.Not.Null);
		}

		[Test]
		public void MoorThrowerSkin_AllLodsHaveNormalizedWeightsAndExactRig()
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
		public void MoorThrowerClips_SampleFiniteAndKeepReadableFootprint()
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

