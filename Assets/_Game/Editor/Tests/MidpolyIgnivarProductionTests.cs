using Eidren.Presentation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyIgnivarProductionTests
	{
		private const string PrefabPath = "Assets/_Game/Prefabs/Actors/3D/Ignivar_3D.prefab";
		private const string PresentationPath = "Assets/_Game/Resources/Prefabs/Actors/3D/Ignivar_3D.prefab";
		private const string GlbPath = "Assets/_Game/Art/MidPoly/Creatures/Ignivar/Runtime/CRE_Ignivar_Mid_Production.glb";
		private static readonly string[] ExpectedClips = { "Angriff", "Erscheinen", "Gehen", "Ruhe", "Taumeln", "Telegraph", "Tod", "Treffer" };

		[Test]
		public void IgnivarPrefab_PreservesProductionContract()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(prefab, Is.Not.Null);
			Assert.That(AssetDatabase.AssetPathToGUID(PrefabPath), Is.EqualTo("7930733b4c603dc489768eefece97e75"));
			Assert.That(AssetDatabase.GetLabels(prefab), Does.Contain("IgnivarProduction"));
			Assert.That(prefab.GetComponent<CreatureMeshPresentation>().WorldHeight, Is.EqualTo(1.0f).Within(0.001f));
			Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty);
			LOD[] lods = prefab.GetComponent<LODGroup>().GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3));
			Assert.That(lods.Select(l => l.screenRelativeTransitionHeight), Is.EqualTo(new[] { 0.55f, 0.25f, 0.01f /* F34-003 */ }).Within(0.001f));
			SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			Assert.That(Triangles(RenderersFor(renderers, "LOD0")), Is.EqualTo(25446));
			Assert.That(Triangles(RenderersFor(renderers, "LOD1")), Is.EqualTo(14248));
			Assert.That(Triangles(RenderersFor(renderers, "LOD2")), Is.EqualTo(5996));
			Assert.That(renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct().Count(), Is.EqualTo(3));
			Animation animation = prefab.GetComponentInChildren<Animation>(true);
			Assert.That(animation.Cast<AnimationState>().Select(s => s.name).OrderBy(n => n), Is.EqualTo(ExpectedClips));
			GameObject presentation = AssetDatabase.LoadAssetAtPath<GameObject>(PresentationPath);
			Assert.That(AssetDatabase.AssetPathToGUID(PresentationPath), Is.EqualTo("47e7e5fe41fdef844977734f028aa57b"));
			Assert.That(presentation.GetComponentInChildren<CreatureMeshPresentation>(true), Is.Not.Null);
		}

		[Test]
		public void IgnivarSkin_AllLodsHaveNormalizedWeightsAndExactRig()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			foreach (SkinnedMeshRenderer renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				Assert.That(renderer.bones, Has.Length.EqualTo(22), renderer.name);
				Assert.That(renderer.rootBone, Is.Not.Null, renderer.name);
				foreach (BoneWeight weight in renderer.sharedMesh.boneWeights)
					Assert.That(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3, Is.EqualTo(1f).Within(0.002f), renderer.name);
			}
		}

		[Test]
		public void IgnivarClips_SampleFiniteAndKeepReadableFootprint()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				Dictionary<string, AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(GlbPath).OfType<AnimationClip>()
					.Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToDictionary(c => c.name, StringComparer.Ordinal);
				Assert.That(clips.Keys.OrderBy(n => n), Is.EqualTo(ExpectedClips));
				GameObject animationTarget = instance.GetComponentInChildren<Animation>(true).gameObject;
				Transform root = Find(instance.transform, "root");
				Transform[] feet = { Find(instance.transform, "footFL"), Find(instance.transform, "footFR"), Find(instance.transform, "footRL"), Find(instance.transform, "footRR") };
				Assert.That(root, Is.Not.Null);
				Assert.That(feet, Has.None.Null);
				foreach (AnimationClip clip in clips.Values)
				{
					Vector3 start = Vector3.zero, end = Vector3.zero;
					for (int sample = 0; sample <= 6; sample++)
					{
						clip.SampleAnimation(animationTarget, clip.length * sample / 6f);
						AssertFinite(instance.transform, clip.name);
						Bounds bounds = CombinedBounds(instance);
						Assert.That(bounds.size.x, Is.LessThan(2.2f), clip.name);
						Assert.That(bounds.size.y, Is.LessThan(2.1f), clip.name);
						Assert.That(bounds.size.z, Is.LessThan(2.8f), clip.name);
						Assert.That(bounds.min.y, Is.GreaterThan(-0.8f), clip.name);
						if (sample == 0) start = root.position;
						if (sample == 6) end = root.position;
					}
					Assert.That(new Vector2(end.x - start.x, end.z - start.z).magnitude, Is.LessThan(1.1f), clip.name + " besitzt unerwartete Root-Motion.");
				}
			}
			finally { UnityEngine.Object.DestroyImmediate(instance); }
		}

		private static Renderer[] RenderersFor(IEnumerable<SkinnedMeshRenderer> renderers, string lod) { return renderers.Where(r => Contains(r.transform, lod)).Cast<Renderer>().ToArray(); }
		private static bool Contains(Transform t, string value) { for (Transform c = t; c != null; c = c.parent) if (c.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true; return false; }
		private static int Triangles(IEnumerable<Renderer> renderers) { return renderers.OfType<SkinnedMeshRenderer>().Select(r => r.sharedMesh).Distinct().Sum(m => Enumerable.Range(0, m.subMeshCount).Sum(i => (int)m.GetIndexCount(i) / 3)); }
		private static Transform Find(Transform root, string name) { if (root.name == name) return root; for (int i = 0; i < root.childCount; i++) { Transform found = Find(root.GetChild(i), name); if (found != null) return found; } return null; }
		private static void AssertFinite(Transform root, string clip) { foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) { Assert.That(float.IsNaN(t.position.x) || float.IsInfinity(t.position.x), Is.False, clip + "/" + t.name); Assert.That(float.IsNaN(t.rotation.w) || float.IsInfinity(t.rotation.w), Is.False, clip + "/" + t.name); } }
		private static Bounds CombinedBounds(GameObject instance) { Renderer[] renderers = RenderersFor(instance.GetComponentsInChildren<SkinnedMeshRenderer>(true), "LOD0"); Bounds bounds = renderers[0].bounds; foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); return bounds; }
	}
}
