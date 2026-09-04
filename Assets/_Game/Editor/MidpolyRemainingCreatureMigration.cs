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
	/// <summary>Produktive, GUID-erhaltende Migration der letzten vier Kreaturenmodelle.</summary>
	public static class MidpolyRemainingCreatureMigration
	{
		private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase3/REMAINING_CREATURES_UNITY_REPORT.json";
		private const string AnimationMethodPropertyPath = "importSettings.animationMethod";
		private const int GltfAnimationMethodLegacy = 1;
		private static readonly string[] QuadrupedBones = { "chest", "footFL", "footFR", "footRL", "footRR", "head", "hips", "jaw", "legFL_lo", "legFL_up", "legFR_lo", "legFR_up", "legRL_lo", "legRL_up", "legRR_lo", "legRR_up", "neck", "root", "spine", "tail1", "tail2", "tail3" };
		private static readonly string[] HumanoidBones = { "armL_fore", "armL_up", "armR_fore", "armR_up", "chest", "footL", "footR", "handL", "handR", "head", "hips", "root", "shinL", "shinR", "spine", "thighL", "thighR" };

		[Serializable] private sealed class Registry
		{
			public bool terrockPilotEnabled;
			public bool noctarionPilotEnabled;
			public bool garonPilotEnabled;
			public bool coreGuardianPilotEnabled;
		}

		private sealed class Config
		{
			public string Name; public string Combined; public string Lod0; public string Lod1; public string Lod2;
			public string Prefab; public string Consumer; public string Fallback; public float Height;
			public int Tri0; public int Tri1; public int Tri2; public string[] Clips; public string[] Bones;
			public Func<Registry, bool> Enabled;
		}

		[Serializable] private sealed class AssetReport
		{
			public string asset; public bool pass; public string prefabGuidBefore; public string prefabGuidAfter;
			public string consumerGuidBefore; public string consumerGuidAfter; public int lod0Triangles;
			public int lod1Triangles; public int lod2Triangles; public int materials; public int bones;
			public string[] clips; public int strippedHelpers; public Vector3 rendererBoundsSize; public string[] errors;
		}

		[Serializable] private sealed class Report
		{
			public string generatedUtc; public bool pass; public AssetReport[] assets;
		}

		private static readonly Config[] Configs =
		{
			Make("Terrock", 1.0f, 30872, 17288, 7409,
				new[] { "Erscheinen", "Felsbrecher", "Flucht", "Gehen", "Ruhe", "Steinhaut", "Taumeln", "Telegraph", "Treffer", "Wildangriff" }, QuadrupedBones,
				"Assets/_Game/Prefabs/Actors/3D/Terrock_3D.prefab", "Assets/_Game/Resources/Prefabs/Actors/3D/Terrock_3D.prefab", r => r.terrockPilotEnabled),
			Make("Noctarion", 1.0f, 24150, 13524, 5778,
				new[] { "Erscheinen", "Flucht", "Gehen", "Ruhe", "Schattenmal", "Schattenschritt", "Taumeln", "Telegraph", "Treffer", "Wildangriff" }, QuadrupedBones,
				"Assets/_Game/Prefabs/Actors/3D/Noctarion_3D.prefab", "Assets/_Game/Resources/Prefabs/Actors/3D/Noctarion_3D.prefab", r => r.noctarionPilotEnabled),
			Make("Garon", 4.5f, 53660, 30048, 12878,
				new[] { "Ansturm", "Frontschlag", "Gehen", "Rueckkehr", "Ruhe", "Taumeln", "Tod", "Treffer", "Wirbel" }, QuadrupedBones,
				"Assets/_Game/Prefabs/Actors/3D/Garon_3D.prefab", "Assets/_Game/Prefabs/Bosses/Garon.prefab", r => r.garonPilotEnabled),
			Make("CoreGuardian", 4.5f, 54360, 30440, 13046,
				new[] { "Angriff", "Erscheinen", "Gehen", "Ruhe", "Taumeln", "Telegraph", "Tod", "Treffer" }, HumanoidBones,
				"Assets/_Game/Prefabs/Actors/3D/CoreGuardian_3D.prefab", "Assets/_Game/Prefabs/Enemies/Forge/CoreGuardian.prefab", r => r.coreGuardianPilotEnabled),
		};

		[MenuItem("Eidren/Mid-Poly/Phase 3/Restliche Kreaturen bauen und pruefen")]
		public static void BuildAndValidate()
		{
			Registry registry = LoadRegistry();
			List<AssetReport> reports = new List<AssetReport>();
			foreach (Config config in Configs)
			{
				if (!config.Enabled(registry)) throw new InvalidOperationException(config.Name + " ist im Migrationsregister nicht aktiviert.");
				if (!RequiredFilesExist(config)) throw new FileNotFoundException(config.Name + " besitzt kein vollstaendiges Runtime-Paket.");
				reports.Add(BuildOne(config));
			}
			Report report = new Report { generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), pass = reports.All(r => r.pass), assets = reports.ToArray() };
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
			AssetDatabase.Refresh();
			if (!report.pass) throw new InvalidOperationException("Migration der restlichen Kreaturen FAIL:\n" + string.Join("\n", reports.SelectMany(r => r.errors)));
			Debug.Log("[MIDPOLY-000] Restliche Kreaturen PASS: " + string.Join(", ", reports.Select(r => r.asset + " " + r.lod0Triangles + "/" + r.lod1Triangles + "/" + r.lod2Triangles)));
		}

		private static Config Make(string name, float height, int tri0, int tri1, int tri2, string[] clips, string[] bones, string prefab, string consumer, Func<Registry, bool> enabled)
		{
			string root = "Assets/_Game/Art/MidPoly/Creatures/" + name + "/Runtime/CRE_" + name + "_Mid_";
			return new Config
			{
				Name = name, Combined = root + "Production.glb", Lod0 = root + "LOD0.glb", Lod1 = root + "LOD1.glb", Lod2 = root + "LOD2.glb",
				Prefab = prefab, Consumer = consumer, Fallback = "Assets/_Game/Prefabs/Actors/3D/Fallback/" + name + "_3D_Legacy.prefab",
				Height = height, Tri0 = tri0, Tri1 = tri1, Tri2 = tri2,
				Clips = clips.OrderBy(n => n, StringComparer.Ordinal).ToArray(), Bones = bones.OrderBy(n => n, StringComparer.Ordinal).ToArray(), Enabled = enabled
			};
		}

		private static Registry LoadRegistry()
		{
			if (!File.Exists(RegistryPath)) throw new FileNotFoundException("Mid-Poly-Migrationsregister fehlt.");
			return JsonUtility.FromJson<Registry>(File.ReadAllText(RegistryPath)) ?? new Registry();
		}

		private static bool RequiredFilesExist(Config config)
		{
			return File.Exists(config.Combined) && File.Exists(config.Lod0) && File.Exists(config.Lod1) && File.Exists(config.Lod2);
		}

		private static AssetReport BuildOne(Config config)
		{
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			EnsureLegacyAnimationImport(config.Combined);
			string prefabBefore = AssetDatabase.AssetPathToGUID(config.Prefab);
			string consumerBefore = AssetDatabase.AssetPathToGUID(config.Consumer);
			EnsureFallback(config);
			int stripped = BuildPrefab(config);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			LabelAssets(config);
			return Validate(config, prefabBefore, AssetDatabase.AssetPathToGUID(config.Prefab), consumerBefore, AssetDatabase.AssetPathToGUID(config.Consumer), stripped);
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

		private static void EnsureFallback(Config config)
		{
			if (MidpolyLegacyArchiveGuard.HasBackup(config.Fallback)) return;
			Directory.CreateDirectory(Path.GetDirectoryName(config.Fallback));
			if (!AssetDatabase.CopyAsset(config.Prefab, config.Fallback)) throw new InvalidOperationException(config.Name + "-Legacy-Fallback konnte nicht angelegt werden.");
		}

		private static int BuildPrefab(Config config)
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(config.Combined);
			if (model == null) throw new InvalidOperationException(config.Name + " wurde nicht als GameObject importiert.");
			GameObject root = new GameObject(config.Name + "_3D");
			try
			{
				GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(model);
				rig.name = config.Name + "_MidPoly";
				rig.transform.SetParent(root.transform, false);
				int stripped = 0;
				foreach (MeshRenderer helper in rig.GetComponentsInChildren<MeshRenderer>(true))
				{
					if (helper.name.IndexOf("Icosphere", StringComparison.OrdinalIgnoreCase) < 0) continue;
					UnityEngine.Object.DestroyImmediate(helper.gameObject);
					stripped++;
				}
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
				if (lod0.Length == 0 || lod1.Length == 0 || lod2.Length == 0) throw new InvalidOperationException(config.Name + " besitzt eine leere LOD-Stufe.");
				LODGroup group = root.AddComponent<LODGroup>();
				group.fadeMode = LODFadeMode.CrossFade;
				group.animateCrossFading = true;
				group.SetLODs(new[] { new LOD(.55f, lod0) { fadeTransitionWidth = .12f }, new LOD(.25f, lod1) { fadeTransitionWidth = .12f }, new LOD(.07f, lod2) { fadeTransitionWidth = .12f } });
				group.RecalculateBounds();
				Animation animation = rig.GetComponentInChildren<Animation>(true) ?? rig.AddComponent<Animation>();
				animation.playAutomatically = false;
				foreach (AnimationClip clip in ClipsAt(config.Combined)) if (animation.GetClip(clip.name) == null) animation.AddClip(clip, clip.name);
				CreatureMeshPresentation presentation = root.AddComponent<CreatureMeshPresentation>();
				presentation.Configure(animation, rig.transform, config.Height);
				PrefabUtility.SaveAsPrefabAsset(root, config.Prefab);
				return stripped;
			}
			finally { UnityEngine.Object.DestroyImmediate(root); }
		}

		private static AnimationClip[] ClipsAt(string path)
		{
			return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).OrderBy(c => c.name, StringComparer.Ordinal).ToArray();
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

		private static void LabelAssets(Config config)
		{
			foreach (string path in new[] { config.Combined, config.Lod0, config.Lod1, config.Lod2, config.Prefab })
			{
				UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
				if (asset != null) AssetDatabase.SetLabels(asset, new[] { "MIDPOLY-000", "MidPoly", "Approved", config.Name + "Production" });
			}
		}

		private static AssetReport Validate(Config config, string prefabBefore, string prefabAfter, string consumerBefore, string consumerAfter, int stripped)
		{
			List<string> errors = new List<string>();
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(config.Prefab);
			GameObject consumer = AssetDatabase.LoadAssetAtPath<GameObject>(config.Consumer);
			LODGroup group = prefab != null ? prefab.GetComponent<LODGroup>() : null;
			SkinnedMeshRenderer[] renderers = prefab != null ? prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true) : Array.Empty<SkinnedMeshRenderer>();
			Animation animation = prefab != null ? prefab.GetComponentInChildren<Animation>(true) : null;
			string[] clips = animation == null ? Array.Empty<string>() : animation.Cast<AnimationState>().Select(s => s.name).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
			string[] bones = renderers.SelectMany(r => r.bones ?? Array.Empty<Transform>()).Where(b => b != null).Select(b => b.name).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
			string[] materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct(StringComparer.Ordinal).ToArray();
			if (prefab == null) errors.Add(config.Name + ": Produktionsprefab fehlt.");
			if (string.IsNullOrEmpty(prefabBefore) || prefabBefore != prefabAfter) errors.Add(config.Name + ": Visual-Prefab-GUID wurde nicht erhalten.");
			if (string.IsNullOrEmpty(consumerBefore) || consumerBefore != consumerAfter) errors.Add(config.Name + ": Consumer-Prefab-GUID wurde veraendert.");
			if (group == null || group.GetLODs().Length != 3) errors.Add(config.Name + ": LODGroup besitzt nicht genau drei Stufen.");
			if (!clips.SequenceEqual(config.Clips, StringComparer.Ordinal)) errors.Add(config.Name + ": Clipvertrag weicht ab.");
			if (!bones.SequenceEqual(config.Bones, StringComparer.Ordinal)) errors.Add(config.Name + ": Bonevertrag weicht ab.");
			if (materials.Length != 3) errors.Add(config.Name + ": erwartet drei Materialien, erhalten " + materials.Length + ".");
			if (!MidpolyLegacyArchiveGuard.HasBackup(config.Fallback)) errors.Add(config.Name + ": Legacy-Fallback fehlt.");
			if (consumer == null || consumer.GetComponentInChildren<CreatureMeshPresentation>(true) == null) errors.Add(config.Name + ": produktiver Consumer erreicht das Mid-Poly-Visual nicht.");
			if (prefab != null && prefab.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.name.IndexOf("Icosphere", StringComparison.OrdinalIgnoreCase) >= 0)) errors.Add(config.Name + ": GLB-Hilfsmesh wurde nicht entfernt.");
			int lod0 = TriangleCount(RenderersForLod(renderers, "LOD0")); int lod1 = TriangleCount(RenderersForLod(renderers, "LOD1")); int lod2 = TriangleCount(RenderersForLod(renderers, "LOD2"));
			if (lod0 != config.Tri0 || lod1 != config.Tri1 || lod2 != config.Tri2) errors.Add(config.Name + ": LOD-Dreiecke weichen ab: " + lod0 + "/" + lod1 + "/" + lod2 + ".");
			Bounds bounds = CombinedBounds(prefab);
			return new AssetReport
			{
				asset = config.Name, pass = errors.Count == 0, prefabGuidBefore = prefabBefore, prefabGuidAfter = prefabAfter,
				consumerGuidBefore = consumerBefore, consumerGuidAfter = consumerAfter,
				lod0Triangles = lod0, lod1Triangles = lod1, lod2Triangles = lod2,
				materials = materials.Length, bones = bones.Length, clips = clips, strippedHelpers = stripped,
				rendererBoundsSize = bounds.size, errors = errors.ToArray()
			};
		}

		private static int TriangleCount(IEnumerable<Renderer> renderers)
		{
			return renderers.OfType<SkinnedMeshRenderer>().Where(r => r.sharedMesh != null).Select(r => r.sharedMesh).Distinct().Sum(m => Enumerable.Range(0, m.subMeshCount).Sum(i => (int)m.GetIndexCount(i) / 3));
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
