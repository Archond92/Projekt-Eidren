using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class WandererAnimationFixTests
	{
		// Der Mid-Poly-Golden-Master wurde am 27.08.2026 fuer den Neubau entfernt
		// (Sicherung: Eidren-Sicherungen/MidPolyWanderer_Komplett_20260827).
		private static readonly string[] ModelPaths =
		{
			"Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb"
		};

		[Test]
		public void IdleWalkAndRunClipsCloseAtTheirLoopBoundary()
		{
			foreach (string modelPath in ModelPaths)
			{
				GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
				AnimationClip[] clips = Clips(modelPath)
					.Where(clip => clip.name.StartsWith("Ruhe_", StringComparison.Ordinal)
						|| clip.name.StartsWith("Gehen_", StringComparison.Ordinal)
						|| clip.name.StartsWith("Laufen_", StringComparison.Ordinal)).ToArray();
				Assert.That(model, Is.Not.Null, modelPath);
				Assert.That(clips, Has.Length.EqualTo(21), modelPath);
				foreach (AnimationClip clip in clips)
				{
					GameObject instance = UnityEngine.Object.Instantiate(model);
					try
					{
						clip.SampleAnimation(instance, 0f);
						Dictionary<Transform, Pose> start = CapturePose(instance);
						clip.SampleAnimation(instance, clip.length);
						Dictionary<Transform, Pose> end = CapturePose(instance);
						foreach (Transform bone in start.Keys)
						{
							Assert.That(Vector3.Distance(start[bone].position, end[bone].position), Is.LessThan(0.0001f),
								$"{modelPath}/{clip.name}/{bone.name}: Translation");
							Assert.That(Quaternion.Angle(start[bone].rotation, end[bone].rotation), Is.LessThan(0.01f),
								$"{modelPath}/{clip.name}/{bone.name}: Rotation");
							Assert.That(Vector3.Distance(start[bone].scale, end[bone].scale), Is.LessThan(0.0001f),
								$"{modelPath}/{clip.name}/{bone.name}: Skalierung");
						}
					}
					finally { UnityEngine.Object.DestroyImmediate(instance); }
				}
			}
		}

		[Test]
		public void OpeningClipStartsStandingAndContainsAReadableReachMotion()
		{
			foreach (string modelPath in ModelPaths)
			{
				GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
				AnimationClip clip = Clips(modelPath).Single(item => item.name == "Oeffnen");
				GameObject instance = UnityEngine.Object.Instantiate(model);
				try
				{
					Transform root = FindDeep(instance.transform, "root");
					Transform arm = FindDeep(instance.transform, "armL_up");
					Assert.That(root, Is.Not.Null, modelPath);
					Assert.That(arm, Is.Not.Null, modelPath);
					clip.SampleAnimation(instance, 0f);
					Vector3 startRoot = root.localPosition;
					Quaternion startArm = arm.localRotation;
					clip.SampleAnimation(instance, clip.length * 0.5f);
					Assert.That(Vector3.Distance(startRoot, root.localPosition), Is.GreaterThan(0.05f), modelPath);
					Assert.That(Quaternion.Angle(startArm, arm.localRotation), Is.GreaterThan(35f), modelPath);
					clip.SampleAnimation(instance, clip.length);
					// 5 mm / 0,5°: Unitys Animations-Kompression laesst auf den grossen Oeffnen-Amplituden
					// (root 0,54 m, armL_up 57°) am Clipende Restfehler dieser Groessenordnung zurueck
					Assert.That(Vector3.Distance(startRoot, root.localPosition), Is.LessThan(0.005f), modelPath);
					Assert.That(Quaternion.Angle(startArm, arm.localRotation), Is.LessThan(0.5f), modelPath);
				}
				finally { UnityEngine.Object.DestroyImmediate(instance); }
			}
		}

		[Test]
		public void CorrectedClipsNeverAnimateBoneScale()
		{
			foreach (string modelPath in ModelPaths)
			{
				GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
				GameObject instance = UnityEngine.Object.Instantiate(model);
				try
				{
					Dictionary<Transform, Vector3> baseScales = Bones(instance)
						.ToDictionary(transform => transform, transform => transform.localScale);
					foreach (AnimationClip clip in Clips(modelPath))
					foreach (float normalizedTime in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
					{
						clip.SampleAnimation(instance, clip.length * normalizedTime);
						foreach (Transform bone in baseScales.Keys)
							Assert.That(Vector3.Distance(baseScales[bone], bone.localScale), Is.LessThan(0.0001f),
								$"{modelPath}/{clip.name}/{bone.name} @ {normalizedTime:0.00}");
					}
				}
				finally { UnityEngine.Object.DestroyImmediate(instance); }
			}
		}

		private static AnimationClip[] Clips(string path)
		{
			return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
				.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
		}

		private static Dictionary<Transform, Pose> CapturePose(GameObject root)
		{
			return Bones(root).ToDictionary(
				transform => transform,
				transform => new Pose(transform.localPosition, transform.localRotation, transform.localScale));
		}

		private static Transform[] Bones(GameObject root)
		{
			return root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
				.SelectMany(renderer => renderer.bones).Where(bone => bone != null).Distinct().ToArray();
		}

		private static Transform FindDeep(Transform root, string name)
		{
			return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);
		}

		private readonly struct Pose
		{
			public readonly Vector3 position;
			public readonly Quaternion rotation;
			public readonly Vector3 scale;

			public Pose(Vector3 position, Quaternion rotation, Vector3 scale)
			{
				this.position = position;
				this.rotation = rotation;
				this.scale = scale;
			}
		}
	}
}
