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
	/// <summary>GUID-erhaltende Phase-3-Migration des freigegebenen Riftlings.</summary>
	public static class MidpolyRiftlingMigration
	{
		private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
		private const string CombinedPath = "Assets/_Game/Art/MidPoly/Creatures/Riftling/Runtime/CRE_Riftling_Mid_Production.glb";
		private const string Lod0Path = "Assets/_Game/Art/MidPoly/Creatures/Riftling/Runtime/CRE_Riftling_Mid_LOD0.glb";
		private const string Lod1Path = "Assets/_Game/Art/MidPoly/Creatures/Riftling/Runtime/CRE_Riftling_Mid_LOD1.glb";
		private const string Lod2Path = "Assets/_Game/Art/MidPoly/Creatures/Riftling/Runtime/CRE_Riftling_Mid_LOD2.glb";
		private const string PrefabPath = "Assets/_Game/Prefabs/Actors/3D/Riftling_3D.prefab";
		private const string EnemyPrefabPath = "Assets/_Game/Prefabs/Enemies/TierTwo/Riftling.prefab";
		private const string FallbackPath = "Assets/_Game/Prefabs/Actors/3D/Fallback/Riftling_3D_Legacy.prefab";
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase3/RIFTLING_TECHNICAL_REPORT.json";
		private const string AnimationMethodPropertyPath = "importSettings.animationMethod";
		private const int GltfAnimationMethodLegacy = 1;
		private const float WorldHeight = 1.70f;

		private static readonly string[] ExpectedClips =
		{
			"Angriff", "Erscheinen", "Gehen", "Ruhe", "Taumeln", "Telegraph", "Tod", "Treffer"
		};
		private static readonly string[] ExpectedBones =
		{
			"armL_fore", "armL_up", "armR_fore", "armR_up", "chest", "footL", "footR", "handL", "handR",
			"head", "hips", "root", "shinL", "shinR", "spine", "thighL", "thighR"
		};

		[Serializable] private sealed class Registry { public bool riftlingPilotEnabled; }
		[Serializable] private sealed class Report
		{
			public string generatedUtc;
			public bool pass;
			public string prefabGuidBefore;
			public string prefabGuidAfter;
			public string enemyGuidBefore;
			public string enemyGuidAfter;
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
			public string legacyFallbackPath;
			public string[] errors;
		}

		[MenuItem("Eidren/Mid-Poly/Phase 3/Riftling bauen und pruefen")]
		public static void BuildAndValidate()
		{
			if (!TryBuildApprovedVisual())
				throw new InvalidOperationException("Riftling-Golden-Master ist nicht aktiviert oder seine Runtime-Dateien fehlen.");
		}

		public static bool TryBuildApprovedVisual()
		{
			if (!PilotEnabled() || !RequiredFilesExist()) return false;
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			EnsureLegacyAnimationImport(CombinedPath);
			string prefabBefore = AssetDatabase.AssetPathToGUID(PrefabPath);
			string enemyBefore = AssetDatabase.AssetPathToGUID(EnemyPrefabPath);
			EnsureLegacyFallback();
			BuildPrefab();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			string prefabAfter = AssetDatabase.AssetPathToGUID(PrefabPath);
			string enemyAfter = AssetDatabase.AssetPathToGUID(EnemyPrefabPath);
			LabelAssets();
			ValidateAndReport(prefabBefore, prefabAfter, enemyBefore, enemyAfter);
			return true;
		}

		private static bool PilotEnabled()
		{
			if (!File.Exists(RegistryPath)) return false;
			Registry registry = JsonUtility.FromJson<Registry>(File.ReadAllText(RegistryPath));
			return registry != null && registry.riftlingPilotEnabled;
		}

		private static bool RequiredFilesExist()
		{
			return File.Exists(CombinedPath) && File.Exists(Lod0Path) && File.Exists(Lod1Path) && File.Exists(Lod2Path);
		}

		private static void EnsureLegacyAnimationImport(string path)
		{
			AssetImporter importer = AssetImporter.GetAtPath(path);
			if (importer == null) throw new InvalidOperationException("GLB-Importer nicht gefunden: " + path);
			SerializedObject serialized = new SerializedObject(importer);
			SerializedProperty method = serialized.FindProperty(AnimationMethodPropertyPath);
			if (method == null) throw new InvalidOperationException("GLB-Importer besitzt keine Animationseinstellung: " + path);
			if (method.intValue == GltfAnimationMethodLegacy) return;
			method.intValue = GltfAnimationMethodLegacy;
			serialized.ApplyModifiedProperties();
			importer.SaveAndReimport();
		}

		private static void EnsureLegacyFallback()
		{
			if (MidpolyLegacyArchiveGuard.HasBackup(FallbackPath)) return;
			Directory.CreateDirectory(Path.GetDirectoryName(FallbackPath));
			if (!AssetDatabase.CopyAsset(PrefabPath, FallbackPath))
				throw new InvalidOperationException("Riftling-Legacy-Fallback konnte nicht angelegt werden.");
		}

		private static void BuildPrefab()
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(CombinedPath);
			if (model == null) throw new InvalidOperationException("Produktions-Riftling wurde nicht als GameObject importiert.");
			GameObject root = new GameObject("Riftling_3D");
			try
			{
				GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(model);
				rig.name = "Riftling_MidPoly";
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
					throw new InvalidOperationException("Mindestens eine Riftling-LOD-Stufe besitzt keinen Renderer.");
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

				Animation animation = rig.GetComponentInChildren<Animation>(true) ?? rig.AddComponent<Animation>();
				animation.playAutomatically = false;
				foreach (AnimationClip clip in ClipsAt(CombinedPath))
					if (animation.GetClip(clip.name) == null) animation.AddClip(clip, clip.name);
				CreatureMeshPresentation presentation = root.AddComponent<CreatureMeshPresentation>();
				presentation.Configure(animation, rig.transform, WorldHeight);
				PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			}
			finally { UnityEngine.Object.DestroyImmediate(root); }
		}

		private static AnimationClip[] ClipsAt(string path)
		{
			return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
				.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
				.OrderBy(clip => clip.name, StringComparer.Ordinal).ToArray();
		}

		private static Renderer[] RenderersForLod(IEnumerable<SkinnedMeshRenderer> renderers, string lod)
		{
			return renderers.Where(renderer => HierarchyContains(renderer.transform, lod)).Cast<Renderer>().ToArray();
		}

		private static bool HierarchyContains(Transform transform, string value)
		{
			for (Transform current = transform; current != null; current = current.parent)
				if (current.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
			return false;
		}

		private static void LabelAssets()
		{
			foreach (string path in new[] { CombinedPath, Lod0Path, Lod1Path, Lod2Path, PrefabPath })
			{
				UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
				if (asset != null) AssetDatabase.SetLabels(asset,
					new[] { "MIDPOLY-000", "MidPoly", "Approved", "RiftlingProduction" });
			}
		}

		private static void ValidateAndReport(string prefabBefore, string prefabAfter, string enemyBefore, string enemyAfter)
		{
			List<string> errors = new List<string>();
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
			LODGroup group = prefab != null ? prefab.GetComponent<LODGroup>() : null;
			SkinnedMeshRenderer[] renderers = prefab != null ? prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true) : Array.Empty<SkinnedMeshRenderer>();
			Animation animation = prefab != null ? prefab.GetComponentInChildren<Animation>(true) : null;
			string[] clips = animation == null ? Array.Empty<string>() : animation.Cast<AnimationState>()
				.Select(state => state.name).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
			string[] bones = renderers.SelectMany(renderer => renderer.bones ?? Array.Empty<Transform>())
				.Where(bone => bone != null).Select(bone => bone.name).Distinct(StringComparer.Ordinal)
				.OrderBy(name => name, StringComparer.Ordinal).ToArray();
			string[] materials = renderers.SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null).Select(material => material.name).Distinct(StringComparer.Ordinal).ToArray();
			if (prefab == null) errors.Add("Produktiver Riftling-Prefab fehlt.");
			if (string.IsNullOrEmpty(prefabBefore) || prefabBefore != prefabAfter) errors.Add("Visual-Prefab-GUID wurde nicht erhalten.");
			if (string.IsNullOrEmpty(enemyBefore) || enemyBefore != enemyAfter) errors.Add("Gegner-Prefab-GUID wurde veraendert.");
			if (group == null || group.GetLODs().Length != 3) errors.Add("LODGroup besitzt nicht genau drei Stufen.");
			if (!clips.SequenceEqual(ExpectedClips, StringComparer.Ordinal)) errors.Add("Clipvertrag weicht ab.");
			if (!bones.SequenceEqual(ExpectedBones, StringComparer.Ordinal)) errors.Add("Bonevertrag weicht ab.");
			if (materials.Length != 3) errors.Add("Erwartet sind drei produktive Materialien, erhalten: " + materials.Length + ".");
			if (!MidpolyLegacyArchiveGuard.HasBackup(FallbackPath)) errors.Add("Legacy-Fallback fehlt.");

			int lod0 = TriangleCount(RenderersForLod(renderers, "LOD0"));
			int lod1 = TriangleCount(RenderersForLod(renderers, "LOD1"));
			int lod2 = TriangleCount(RenderersForLod(renderers, "LOD2"));
			if (lod0 != 15376 || lod1 != 8302 || lod2 != 3382)
				errors.Add("LOD-Dreiecke weichen ab: " + lod0 + "/" + lod1 + "/" + lod2 + ".");

			CapsuleCollider collider = enemy != null ? enemy.GetComponent<CapsuleCollider>() : null;
			if (collider == null) errors.Add("Produktiver Riftling-CapsuleCollider fehlt.");
			else if (Mathf.Abs(collider.radius - 0.4576f) > 0.001f || Mathf.Abs(collider.height - 1.848f) > 0.001f)
				errors.Add("Bestehender Collidervertrag wurde veraendert.");
			if (enemy == null || enemy.GetComponentInChildren<CreatureMeshPresentation>(true) == null)
				errors.Add("Gegnerprefab erreicht den Mid-Poly-Visual-Prefab nicht.");

			Bounds bounds = CombinedBounds(prefab);
			if (bounds.size.x > 2.40f || bounds.size.y > 2.20f || bounds.size.z > 2.60f)
				errors.Add("Riftling-Renderer ueberschreiten den freigegebenen Footprint: " + bounds.size + ".");

			Report report = new Report
			{
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), pass = errors.Count == 0,
				prefabGuidBefore = prefabBefore, prefabGuidAfter = prefabAfter,
				enemyGuidBefore = enemyBefore, enemyGuidAfter = enemyAfter,
				lod0Triangles = lod0, lod1Triangles = lod1, lod2Triangles = lod2,
				materials = materials.Length, bones = bones.Length, clips = clips,
				lodLevels = group == null ? 0 : group.GetLODs().Length,
				rendererBoundsSize = bounds.size,
				colliderRadius = collider == null ? 0f : collider.radius,
				colliderHeight = collider == null ? 0f : collider.height,
				legacyFallbackPath = FallbackPath, errors = errors.ToArray()
			};
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
			AssetDatabase.Refresh();
			if (!report.pass) throw new InvalidOperationException("Riftling-Migration FAIL:\n" + string.Join("\n", errors));
			Debug.Log("[MIDPOLY-000] Riftling PASS: " + lod0 + "/" + lod1 + "/" + lod2
				+ " Tris, 3 Materialien, 17 Bones, 8 Clips, GUIDs erhalten.");
		}

		private static int TriangleCount(IEnumerable<Renderer> renderers)
		{
			return renderers.OfType<SkinnedMeshRenderer>().Where(renderer => renderer.sharedMesh != null)
				.Select(renderer => renderer.sharedMesh).Distinct()
				.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount).Sum(index => (int)mesh.GetIndexCount(index) / 3));
		}

		private static Bounds CombinedBounds(GameObject prefab)
		{
			if (prefab == null) return new Bounds();
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				Renderer[] renderers = RenderersForLod(instance.GetComponentsInChildren<SkinnedMeshRenderer>(true), "LOD0");
				if (renderers.Length == 0) return new Bounds();
				Bounds bounds = renderers[0].bounds;
				foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
				return bounds;
			}
			finally { UnityEngine.Object.DestroyImmediate(instance); }
		}
	}
}
