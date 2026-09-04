using Eidren.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eidren.Editor
{
	/// <summary>
	/// Phase-1-Pilot fuer den freigegebenen kantigen Wildling. Der produktive
	/// Prefabpfad bleibt bestehen; damit bleiben alle Szenen- und Gegner-GUIDs
	/// intakt. Das alte GLB bleibt als abschaltbarer Fallback erhalten.
	/// </summary>
	public static class MidpolyWildlingMigration
	{
		private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
		private const string CombinedPath = "Assets/_Game/Art/MidPoly/GoldenMasters/Wildling/Runtime/CHR_Wildling_Mid_Production.glb";
		private const string Lod0Path = "Assets/_Game/Art/MidPoly/GoldenMasters/Wildling/Runtime/CHR_Wildling_Mid_Lod0.glb";
		private const string Lod1Path = "Assets/_Game/Art/MidPoly/GoldenMasters/Wildling/Runtime/CHR_Wildling_Mid_Lod1.glb";
		private const string Lod2Path = "Assets/_Game/Art/MidPoly/GoldenMasters/Wildling/Runtime/CHR_Wildling_Mid_Lod2.glb";
		private const string PrefabPath = "Assets/_Game/Prefabs/Actors/3D/Wildling_3D.prefab";
		private const string EnemyPrefabPath = "Assets/_Game/Prefabs/Enemies/Wildling.prefab";
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase1/WILDLING_TECHNICAL_REPORT.json";
		private const int GltfAnimationMethodLegacy = 1;
		private const string AnimationMethodPropertyPath = "importSettings.animationMethod";
		private const float WorldHeight = 1.90f;

		private static readonly string[] ExpectedClips =
		{
			"Angriff", "Erscheinen", "Gehen", "Ruhe", "Taumeln", "Telegraph", "Tod", "Treffer"
		};

		private static readonly string[] ExpectedBones =
		{
			"armL_fore", "armL_up", "armR_fore", "armR_up", "chest", "footL", "footR",
			"handL", "handR", "head", "hips", "root", "shinL", "shinR", "spine", "thighL", "thighR"
		};

		[Serializable]
		private sealed class MigrationRegistry
		{
			public bool wildlingPilotEnabled;
		}

		[Serializable]
		private sealed class WildlingReport
		{
			public string generatedUtc;
			public bool pass;
			public string prefabGuidBefore;
			public string prefabGuidAfter;
			public int lod0Triangles;
			public int lod1Triangles;
			public int lod2Triangles;
			public int materials;
			public int bones;
			public string[] clips;
			public int lodLevels;
			public Vector3 rendererBoundsSize;
			public float colliderRadius;
			public float colliderHeight;
			public string[] errors;
		}

		[MenuItem("Eidren/Mid-Poly/Phase 1/Wildling bauen und pruefen")]
		public static void BuildAndValidate()
		{
			if (!TryBuildApprovedVisual())
			{
				throw new InvalidOperationException("Wildling-Mid-Poly-Pilot ist nicht aktiviert oder seine Runtime-Dateien fehlen.");
			}
		}

		public static bool TryBuildApprovedVisual()
		{
			if (!PilotEnabled() || !RequiredFilesExist())
			{
				return false;
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			EnsureLegacyAnimationImport(CombinedPath);
			string guidBefore = AssetDatabase.AssetPathToGUID(PrefabPath);
			BuildPrefab();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			string guidAfter = AssetDatabase.AssetPathToGUID(PrefabPath);
			LabelAssets();
			ValidateAndWriteReport(guidBefore, guidAfter);
			return true;
		}

		private static bool PilotEnabled()
		{
			if (!File.Exists(RegistryPath))
			{
				return false;
			}
			MigrationRegistry registry = JsonUtility.FromJson<MigrationRegistry>(File.ReadAllText(RegistryPath));
			return registry != null && registry.wildlingPilotEnabled;
		}

		private static bool RequiredFilesExist()
		{
			return File.Exists(CombinedPath) && File.Exists(Lod0Path)
				&& File.Exists(Lod1Path) && File.Exists(Lod2Path);
		}

		private static void EnsureLegacyAnimationImport(string glb)
		{
			AssetImporter importer = AssetImporter.GetAtPath(glb);
			if (importer == null)
			{
				throw new InvalidOperationException("GLB-Importer nicht gefunden: " + glb);
			}
			SerializedObject serialized = new SerializedObject(importer);
			SerializedProperty method = serialized.FindProperty(AnimationMethodPropertyPath);
			if (method == null)
			{
				throw new InvalidOperationException("GLB-Importer besitzt keine Animationseinstellung: " + glb);
			}
			if (method.intValue != GltfAnimationMethodLegacy)
			{
				method.intValue = GltfAnimationMethodLegacy;
				serialized.ApplyModifiedProperties();
				importer.SaveAndReimport();
			}
		}

		private static void BuildPrefab()
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(CombinedPath);
			if (model == null)
			{
				throw new InvalidOperationException("Produktions-Wildling wurde nicht als GameObject importiert.");
			}

			GameObject root = new GameObject("Wildling_3D");
			try
			{
				GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(model);
				rig.name = "Wildling_MidPoly";
				rig.transform.SetParent(root.transform, false);
				SkinnedMeshRenderer[] renderers = rig.GetComponentsInChildren<SkinnedMeshRenderer>(true);
				foreach (SkinnedMeshRenderer renderer in renderers)
				{
					renderer.shadowCastingMode = ShadowCastingMode.On;
					renderer.receiveShadows = true;
					renderer.updateWhenOffscreen = false;
				}

				Renderer[] lod0 = RenderersForLod(renderers, "LOD0");
				Renderer[] lod1 = RenderersForLod(renderers, "LOD1");
				Renderer[] lod2 = RenderersForLod(renderers, "LOD2");
				if (lod0.Length == 0 || lod1.Length == 0 || lod2.Length == 0)
				{
					throw new InvalidOperationException("Mindestens eine Wildling-LOD-Stufe besitzt keinen Renderer.");
				}
				LODGroup group = root.AddComponent<LODGroup>();
				group.fadeMode = LODFadeMode.CrossFade;
				group.animateCrossFading = true;
				group.SetLODs(new[]
				{
					new LOD(0.55f, lod0) { fadeTransitionWidth = 0.12f },
					new LOD(0.25f, lod1) { fadeTransitionWidth = 0.12f },
					new LOD(0.01f, /* F34-003: Ausblendgrenze wie F33-001 */ lod2) { fadeTransitionWidth = 0.12f }
				});
				group.RecalculateBounds();

				Animation animationPlayer = rig.GetComponentInChildren<Animation>(true);
				if (animationPlayer == null)
				{
					animationPlayer = rig.AddComponent<Animation>();
				}
				animationPlayer.playAutomatically = false;
				foreach (AnimationClip clip in ClipsAt(CombinedPath))
				{
					if (animationPlayer.GetClip(clip.name) == null)
					{
						animationPlayer.AddClip(clip, clip.name);
					}
				}

				CreatureMeshPresentation presentation = root.AddComponent<CreatureMeshPresentation>();
				presentation.Configure(animationPlayer, rig.transform, WorldHeight);
				PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		private static Renderer[] RenderersForLod(IEnumerable<SkinnedMeshRenderer> renderers, string lod)
		{
			return renderers.Where(renderer => HierarchyContains(renderer.transform, lod)).Cast<Renderer>().ToArray();
		}

		private static bool HierarchyContains(Transform transform, string value)
		{
			for (Transform current = transform; current != null; current = current.parent)
			{
				if (current.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		private static AnimationClip[] ClipsAt(string path)
		{
			return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
				.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
				.OrderBy(clip => clip.name, StringComparer.Ordinal).ToArray();
		}

		private static void LabelAssets()
		{
			foreach (string path in new[] { CombinedPath, Lod0Path, Lod1Path, Lod2Path, PrefabPath })
			{
				UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
				if (asset != null)
				{
					AssetDatabase.SetLabels(asset, new[] { "MIDPOLY-000", "MidPoly", "Approved", "WildlingGoldenMaster" });
				}
			}
		}

		private static void ValidateAndWriteReport(string guidBefore, string guidAfter)
		{
			List<string> errors = new List<string>();
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
			LODGroup group = prefab != null ? prefab.GetComponent<LODGroup>() : null;
			SkinnedMeshRenderer[] renderers = prefab != null
				? prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true) : Array.Empty<SkinnedMeshRenderer>();
			Animation animationPlayer = prefab != null ? prefab.GetComponentInChildren<Animation>(true) : null;
			string[] clips = animationPlayer == null ? Array.Empty<string>()
				: animationPlayer.Cast<AnimationState>().Select(state => state.name)
					.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
			string[] bones = renderers.SelectMany(renderer => renderer.bones ?? Array.Empty<Transform>())
				.Where(bone => bone != null).Select(bone => bone.name).Distinct(StringComparer.Ordinal)
				.OrderBy(name => name, StringComparer.Ordinal).ToArray();
			string[] materials = renderers.SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null).Select(material => material.name)
				.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();

			if (prefab == null) errors.Add("Produktiver Wildling-Prefab fehlt.");
			if (string.IsNullOrEmpty(guidBefore) || guidBefore != guidAfter) errors.Add("Prefab-GUID wurde nicht erhalten.");
			if (group == null || group.GetLODs().Length != 3) errors.Add("LODGroup besitzt nicht genau drei Stufen.");
			if (!clips.SequenceEqual(ExpectedClips, StringComparer.Ordinal)) errors.Add("Clipvertrag weicht ab.");
			if (!bones.SequenceEqual(ExpectedBones, StringComparer.Ordinal)) errors.Add("Bonevertrag weicht ab.");
			if (materials.Length != 4) errors.Add("Erwartet sind vier produktive Materialien, erhalten: " + materials.Length + ".");

			int lod0 = TriangleCount(RenderersForLod(renderers, "LOD0"));
			int lod1 = TriangleCount(RenderersForLod(renderers, "LOD1"));
			int lod2 = TriangleCount(RenderersForLod(renderers, "LOD2"));
			if (lod0 <= 0 || lod0 > 35000) errors.Add("LOD0 liegt ausserhalb 1-35.000 Tris: " + lod0 + ".");
			if (lod1 < lod0 * 0.45f || lod1 > lod0 * 0.60f) errors.Add("LOD1 liegt nicht im Zielkorridor 45-60 % von LOD0: " + lod1 + ".");
			if (lod2 < lod0 * 0.15f || lod2 > lod0 * 0.25f) errors.Add("LOD2 liegt nicht im Zielkorridor 15-25 % von LOD0: " + lod2 + ".");

			CapsuleCollider collider = enemy != null ? enemy.GetComponent<CapsuleCollider>() : null;
			if (collider == null) errors.Add("Produktiver Wildling-CapsuleCollider fehlt.");
			else if (Mathf.Abs(collider.radius - 0.52f) > 0.001f || Mathf.Abs(collider.height - 2.10f) > 0.001f)
				errors.Add("Bestehender Collidervertrag wurde veraendert.");

			Bounds bounds = CombinedBounds(renderers);
			if (bounds.size.x > 1.10f || bounds.size.y > 2.10f || bounds.size.z > 1.10f)
				errors.Add("Wildling-Renderer ueberschreiten den produktiven Grund-Footprint: " + bounds.size + ".");

			WildlingReport report = new WildlingReport
			{
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				pass = errors.Count == 0,
				prefabGuidBefore = guidBefore,
				prefabGuidAfter = guidAfter,
				lod0Triangles = lod0,
				lod1Triangles = lod1,
				lod2Triangles = lod2,
				materials = materials.Length,
				bones = bones.Length,
				clips = clips,
				lodLevels = group == null ? 0 : group.GetLODs().Length,
				rendererBoundsSize = bounds.size,
				colliderRadius = collider == null ? 0f : collider.radius,
				colliderHeight = collider == null ? 0f : collider.height,
				errors = errors.ToArray()
			};
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
			AssetDatabase.Refresh();
			if (!report.pass)
			{
				throw new InvalidOperationException("Wildling-Migration FAIL:\n" + string.Join("\n", errors));
			}
			Debug.Log("[MIDPOLY-000] Wildling PASS: " + lod0 + "/" + lod1 + "/" + lod2
				+ " Tris, 4 Materialien, 17 Bones, 8 Clips, GUID erhalten.");
		}

		private static int TriangleCount(IEnumerable<Renderer> renderers)
		{
			return renderers.OfType<SkinnedMeshRenderer>().Where(renderer => renderer.sharedMesh != null)
				.Select(renderer => renderer.sharedMesh).Distinct()
				.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount).Sum(index => (int)mesh.GetIndexCount(index) / 3));
		}

		private static Bounds CombinedBounds(Renderer[] renderers)
		{
			if (renderers.Length == 0) return new Bounds();
			GameObject instance = UnityEngine.Object.Instantiate(renderers[0].transform.root.gameObject);
			try
			{
				Renderer[] instanceRenderers = RenderersForLod(instance.GetComponentsInChildren<SkinnedMeshRenderer>(true), "LOD0");
				if (instanceRenderers.Length == 0) return new Bounds();
				Bounds bounds = instanceRenderers[0].bounds;
				foreach (Renderer renderer in instanceRenderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
				return bounds;
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
		}
	}
}
