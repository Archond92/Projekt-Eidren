using Eidren.Presentation;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyRemainingCreatureProductionTests
	{
		[TestCase("Terrock", "dcb10d767bcd1964fa72c7628a1f4cb6", "Assets/_Game/Resources/Prefabs/Actors/3D/Terrock_3D.prefab", 30872, 17288, 7409, 22, 10, 1.0f)]
		[TestCase("Noctarion", "4b118cbf760ed724899bb2d9a8ca0b24", "Assets/_Game/Resources/Prefabs/Actors/3D/Noctarion_3D.prefab", 24150, 13524, 5778, 22, 10, 1.0f)]
		[TestCase("Garon", "b788d5c7094b868478cef736aad5e9c6", "Assets/_Game/Prefabs/Bosses/Garon.prefab", 53660, 30048, 12878, 22, 9, 4.5f)]
		[TestCase("CoreGuardian", "9db5d40ad344b4449bc989a71c9cac46", "Assets/_Game/Prefabs/Enemies/Forge/CoreGuardian.prefab", 54360, 30440, 13046, 17, 8, 4.5f)]
		public void RemainingCreature_PreservesProductionContract(string name, string guid, string consumerPath, int tri0, int tri1, int tri2, int bones, int clips, float height)
		{
			string prefabPath = "Assets/_Game/Prefabs/Actors/3D/" + name + "_3D.prefab";
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			Assert.That(prefab, Is.Not.Null, name);
			Assert.That(AssetDatabase.AssetPathToGUID(prefabPath), Is.EqualTo(guid), name);
			Assert.That(AssetDatabase.GetLabels(prefab), Does.Contain(name + "Production"), name);
			Assert.That(prefab.GetComponent<CreatureMeshPresentation>().WorldHeight, Is.EqualTo(height).Within(.001f), name);
			Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty, name);
			Assert.That(prefab.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.name.IndexOf("Icosphere", StringComparison.OrdinalIgnoreCase) >= 0), Is.False, name);
			LOD[] lods = prefab.GetComponent<LODGroup>().GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3), name);
			SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			Assert.That(Triangles(renderers, "LOD0"), Is.EqualTo(tri0), name);
			Assert.That(Triangles(renderers, "LOD1"), Is.EqualTo(tri1), name);
			Assert.That(Triangles(renderers, "LOD2"), Is.EqualTo(tri2), name);
			Assert.That(renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct().Count(), Is.EqualTo(3), name);
			Assert.That(renderers.SelectMany(r => r.bones).Where(b => b != null).Select(b => b.name).Distinct().Count(), Is.EqualTo(bones), name);
			Animation animation = prefab.GetComponentInChildren<Animation>(true);
			Assert.That(animation.Cast<AnimationState>().Select(s => s.name).Distinct().Count(), Is.EqualTo(clips), name);
			foreach (SkinnedMeshRenderer renderer in renderers)
				foreach (BoneWeight weight in renderer.sharedMesh.boneWeights)
					Assert.That(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3, Is.EqualTo(1f).Within(.002f), name + "/" + renderer.name);
			GameObject consumer = AssetDatabase.LoadAssetAtPath<GameObject>(consumerPath);
			Assert.That(consumer, Is.Not.Null, name);
			Assert.That(consumer.GetComponentInChildren<CreatureMeshPresentation>(true), Is.Not.Null, name);
		}

		[Test]
		public void CoreGuardianAttack_IsTwoStageBossSlamWithPreservedTiming()
		{
			const string prefabPath = "Assets/_Game/Prefabs/Actors/3D/CoreGuardian_3D.prefab";
			const string glbPath = "Assets/_Game/Art/MidPoly/Creatures/CoreGuardian/Runtime/CRE_CoreGuardian_Mid_Production.glb";
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath); GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				AnimationClip attack = AssetDatabase.LoadAllAssetsAtPath(glbPath).OfType<AnimationClip>().Single(clip => clip.name == "Angriff");
				// The source action keeps its historical 0..25.6 frame range. Unity's
				// glTF importer quantizes that range to 25 samples at 24 fps.
				Assert.That(attack.length, Is.InRange(1.03f, 1.05f), "Das Gameplay-Timing des Bossangriffs wurde veraendert.");
				Animation player = instance.GetComponentInChildren<Animation>(true); Assert.That(player, Is.Not.Null); GameObject animationRoot = player.gameObject;
				Transform left = Find(animationRoot.transform, "armL_up"); Transform right = Find(animationRoot.transform, "armR_up"); Transform rootBone = Find(animationRoot.transform, "root");
				Assert.That(left, Is.Not.Null); Assert.That(right, Is.Not.Null); Assert.That(rootBone, Is.Not.Null);
				attack.SampleAnimation(animationRoot, 0f); Vector3 rootStart = rootBone.localPosition;
				attack.SampleAnimation(animationRoot, attack.length * 11f / 25.6f); Quaternion firstLeft = left.localRotation; Quaternion firstRight = right.localRotation;
				Assert.That(Vector3.Distance(rootStart, rootBone.localPosition), Is.LessThan(.001f), "Der erste Einschlag veraendert die Root-Motion-Semantik.");
				Assert.That(Quaternion.Angle(firstLeft, firstRight), Is.GreaterThan(45f), "Der erste Einschlag ist weiterhin ein spiegelgleicher Zweiarmschlag.");
				attack.SampleAnimation(animationRoot, attack.length * 18f / 25.6f); Quaternion secondLeft = left.localRotation; Quaternion secondRight = right.localRotation;
				Assert.That(Vector3.Distance(rootStart, rootBone.localPosition), Is.LessThan(.001f), "Der zweite Einschlag veraendert die Root-Motion-Semantik.");
				Assert.That(Quaternion.Angle(secondLeft, secondRight), Is.GreaterThan(45f), "Der zweite Einschlag ist weiterhin spiegelgleich.");
				Assert.That(Quaternion.Angle(firstLeft, secondLeft) + Quaternion.Angle(firstRight, secondRight), Is.GreaterThan(100f), "Die beiden Boss-Slam-Stufen sind nicht unterscheidbar.");
			}
			finally { UnityEngine.Object.DestroyImmediate(instance); }
		}

		private static int Triangles(SkinnedMeshRenderer[] renderers, string lod)
		{
			return renderers.Where(r => Contains(r.transform, lod)).Select(r => r.sharedMesh).Distinct().Sum(mesh => Enumerable.Range(0, mesh.subMeshCount).Sum(i => (int)mesh.GetIndexCount(i) / 3));
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

		private static bool Contains(Transform transform, string value)
		{
			for (Transform current = transform; current != null; current = current.parent)
				if (current.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
			return false;
		}
	}
}
