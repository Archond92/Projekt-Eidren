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
	/// <summary>GUID-erhaltende Phase-3-Migration des freigegebenen Ignivars.</summary>
	public static class MidpolyIgnivarMigration
	{
		private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
		private const string CombinedPath = "Assets/_Game/Art/MidPoly/Creatures/Ignivar/Runtime/CRE_Ignivar_Mid_Production.glb";
		private const string Lod0Path = "Assets/_Game/Art/MidPoly/Creatures/Ignivar/Runtime/CRE_Ignivar_Mid_LOD0.glb";
		private const string Lod1Path = "Assets/_Game/Art/MidPoly/Creatures/Ignivar/Runtime/CRE_Ignivar_Mid_LOD1.glb";
		private const string Lod2Path = "Assets/_Game/Art/MidPoly/Creatures/Ignivar/Runtime/CRE_Ignivar_Mid_LOD2.glb";
		private const string PrefabPath = "Assets/_Game/Prefabs/Actors/3D/Ignivar_3D.prefab";
		private const string PresentationPrefabPath = "Assets/_Game/Resources/Prefabs/Actors/3D/Ignivar_3D.prefab";
		private const string FallbackPath = "Assets/_Game/Prefabs/Actors/3D/Fallback/Ignivar_3D_Legacy.prefab";
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase3/IGNIVAR_TECHNICAL_REPORT.json";
		private const string AnimationMethodPropertyPath = "importSettings.animationMethod";
		private const int GltfAnimationMethodLegacy = 1;
		private static readonly string[] ExpectedClips = { "Angriff", "Erscheinen", "Gehen", "Ruhe", "Taumeln", "Telegraph", "Tod", "Treffer" };
		private static readonly string[] ExpectedBones = { "chest", "footFL", "footFR", "footRL", "footRR", "head", "hips", "jaw", "legFL_lo", "legFL_up", "legFR_lo", "legFR_up", "legRL_lo", "legRL_up", "legRR_lo", "legRR_up", "neck", "root", "spine", "tail1", "tail2", "tail3" };

		[Serializable] private sealed class Registry { public bool ignivarPilotEnabled; }
		[Serializable] private sealed class Report
		{
			public string generatedUtc; public bool pass; public string prefabGuidBefore; public string prefabGuidAfter;
			public string presentationGuidBefore; public string presentationGuidAfter; public int lod0Triangles;
			public int lod1Triangles; public int lod2Triangles; public int materials; public int bones;
			public string[] clips; public int lodLevels; public Vector3 rendererBoundsSize; public string legacyFallbackPath;
			public string[] errors;
		}

		[MenuItem("Eidren/Mid-Poly/Phase 3/Ignivar bauen und pruefen")]
		public static void BuildAndValidate()
		{
			if (!TryBuildApprovedVisual()) throw new InvalidOperationException("Ignivar ist nicht aktiviert oder seine Runtime-Dateien fehlen.");
		}

		public static bool TryBuildApprovedVisual()
		{
			if (!PilotEnabled() || !RequiredFilesExist()) return false;
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			EnsureLegacyAnimationImport(CombinedPath);
			string prefabBefore = AssetDatabase.AssetPathToGUID(PrefabPath);
			string presentationBefore = AssetDatabase.AssetPathToGUID(PresentationPrefabPath);
			EnsureLegacyFallback();
			BuildPrefab();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			string prefabAfter = AssetDatabase.AssetPathToGUID(PrefabPath);
			string presentationAfter = AssetDatabase.AssetPathToGUID(PresentationPrefabPath);
			LabelAssets();
			ValidateAndReport(prefabBefore, prefabAfter, presentationBefore, presentationAfter);
			return true;
		}

		private static bool PilotEnabled()
		{
			if (!File.Exists(RegistryPath)) return false;
			Registry registry = JsonUtility.FromJson<Registry>(File.ReadAllText(RegistryPath));
			return registry != null && registry.ignivarPilotEnabled;
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
			if (!AssetDatabase.CopyAsset(PrefabPath, FallbackPath)) throw new InvalidOperationException("Ignivar-Legacy-Fallback konnte nicht angelegt werden.");
		}

		private static void BuildPrefab()
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(CombinedPath);
			if (model == null) throw new InvalidOperationException("Produktiver Ignivar wurde nicht als GameObject importiert.");
			GameObject root = new GameObject("Ignivar_3D");
			try
			{
				GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(model);
				rig.name = "Ignivar_MidPoly";
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
				if (lod0.Length == 0 || lod1.Length == 0 || lod2.Length == 0) throw new InvalidOperationException("Mindestens eine Ignivar-LOD-Stufe besitzt keinen Renderer.");
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
				foreach (AnimationClip clip in ClipsAt(CombinedPath)) if (animation.GetClip(clip.name) == null) animation.AddClip(clip, clip.name);
				CreatureMeshPresentation presentation = root.AddComponent<CreatureMeshPresentation>();
				presentation.Configure(animation, rig.transform, 1.0f);
				PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			}
			finally { UnityEngine.Object.DestroyImmediate(root); }
		}

		private static AnimationClip[] ClipsAt(string path)
		{
			return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
				.Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
				.OrderBy(c => c.name, StringComparer.Ordinal).ToArray();
		}

		private static Renderer[] RenderersForLod(IEnumerable<SkinnedMeshRenderer> renderers, string lod)
		{
			return renderers.Where(r => HierarchyContains(r.transform, lod)).Cast<Renderer>().ToArray();
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
				if (asset != null) AssetDatabase.SetLabels(asset, new[] { "MIDPOLY-000", "MidPoly", "Approved", "IgnivarProduction" });
			}
		}

		private static void ValidateAndReport(string prefabBefore, string prefabAfter, string presentationBefore, string presentationAfter)
		{
			List<string> errors = new List<string>();
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			GameObject presentationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PresentationPrefabPath);
			LODGroup group = prefab != null ? prefab.GetComponent<LODGroup>() : null;
			SkinnedMeshRenderer[] renderers = prefab != null ? prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true) : Array.Empty<SkinnedMeshRenderer>();
			Animation animation = prefab != null ? prefab.GetComponentInChildren<Animation>(true) : null;
			string[] clips = animation == null ? Array.Empty<string>() : animation.Cast<AnimationState>().Select(s => s.name).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
			string[] bones = renderers.SelectMany(r => r.bones ?? Array.Empty<Transform>()).Where(b => b != null).Select(b => b.name).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
			string[] materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct(StringComparer.Ordinal).ToArray();
			if (prefab == null) errors.Add("Produktiver Ignivar-Prefab fehlt.");
			if (string.IsNullOrEmpty(prefabBefore) || prefabBefore != prefabAfter) errors.Add("Visual-Prefab-GUID wurde nicht erhalten.");
			if (string.IsNullOrEmpty(presentationBefore) || presentationBefore != presentationAfter) errors.Add("Praesentations-Prefab-GUID wurde veraendert.");
			if (group == null || group.GetLODs().Length != 3) errors.Add("LODGroup besitzt nicht genau drei Stufen.");
			if (!clips.SequenceEqual(ExpectedClips, StringComparer.Ordinal)) errors.Add("Clipvertrag weicht ab.");
			if (!bones.SequenceEqual(ExpectedBones, StringComparer.Ordinal)) errors.Add("Bonevertrag weicht ab.");
			if (materials.Length != 3) errors.Add("Erwartet sind drei produktive Materialien, erhalten: " + materials.Length + ".");
			if (!MidpolyLegacyArchiveGuard.HasBackup(FallbackPath)) errors.Add("Legacy-Fallback fehlt.");
			int lod0 = TriangleCount(RenderersForLod(renderers, "LOD0"));
			int lod1 = TriangleCount(RenderersForLod(renderers, "LOD1"));
			int lod2 = TriangleCount(RenderersForLod(renderers, "LOD2"));
			if (lod0 != 25446 || lod1 != 14248 || lod2 != 5996) errors.Add("LOD-Dreiecke weichen ab: " + lod0 + "/" + lod1 + "/" + lod2 + ".");
			if (presentationPrefab == null || presentationPrefab.GetComponentInChildren<CreatureMeshPresentation>(true) == null) errors.Add("Praesentationsprefab erreicht den Mid-Poly-Visual-Prefab nicht.");
			Bounds bounds = CombinedBounds(prefab);
			if (bounds.size.x > 0.75f || bounds.size.y > 1.10f || bounds.size.z > 1.65f) errors.Add("Ignivar-Renderer ueberschreiten den freigegebenen Footprint: " + bounds.size + ".");
			Report report = new Report
			{
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), pass = errors.Count == 0,
				prefabGuidBefore = prefabBefore, prefabGuidAfter = prefabAfter,
				presentationGuidBefore = presentationBefore, presentationGuidAfter = presentationAfter,
				lod0Triangles = lod0, lod1Triangles = lod1, lod2Triangles = lod2,
				materials = materials.Length, bones = bones.Length, clips = clips,
				lodLevels = group == null ? 0 : group.GetLODs().Length, rendererBoundsSize = bounds.size,
				legacyFallbackPath = FallbackPath, errors = errors.ToArray()
			};
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
			AssetDatabase.Refresh();
			if (!report.pass) throw new InvalidOperationException("Ignivar-Migration FAIL:\n" + string.Join("\n", errors));
			Debug.Log("[MIDPOLY-000] Ignivar PASS: " + lod0 + "/" + lod1 + "/" + lod2 + " Tris, 3 Materialien, 22 Bones, 8 Clips, GUIDs erhalten.");
		}

		private static int TriangleCount(IEnumerable<Renderer> renderers)
		{
			return renderers.OfType<SkinnedMeshRenderer>().Where(r => r.sharedMesh != null).Select(r => r.sharedMesh).Distinct()
				.Sum(m => Enumerable.Range(0, m.subMeshCount).Sum(i => (int)m.GetIndexCount(i) / 3));
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
