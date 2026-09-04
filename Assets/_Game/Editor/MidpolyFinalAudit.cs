using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>Automated Phase-7 legacy, material, LOD, actor and scene audit.</summary>
	internal static class MidpolyFinalAudit
	{
		private const string OutputPath = "Documentation/Etappen/MidPoly/Phase7/MIDPOLY_FINAL_ASSET_REPORT.json";
		private const string StatusPath = "Documentation/Etappen/MidPoly/Phase0/MIGRATION_STATUS.csv";
		private const string PerformancePath = "Documentation/Etappen/MidPoly/Phase6/PERFORMANCE_REPORT.json";
		private const string LowPolyWandererPath = "Assets/_Game/Prefabs/Actors/3D/Player_3D.prefab";

		private static readonly string[] ProductionRoots =
		{
			"Assets/_Game/Prefabs/Actors/3D",
			"Assets/_Game/Prefabs/Buildings/Level01",
			"Assets/_Game/Prefabs/Containers/Forge",
			"Assets/_Game/Prefabs/Environment/AreaArtVariants",
			"Assets/_Game/Prefabs/Environment/StyleProof",
			"Assets/_Game/Prefabs/Items",
			"Assets/_Game/Prefabs/Loot/WorldChests",
			"Assets/_Game/Prefabs/Resources/Nodes",
			"Assets/_Game/Prefabs/Resources/Visuals",
			"Assets/_Game/Prefabs/Stations",
			"Assets/_Game/Resources/Prefabs"
		};

		private static readonly string[] RequiredReports =
		{
			"Documentation/Etappen/MidPoly/Phase1/RESOURCE_PILOT_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase1/WANDERER_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase1/WILDLING_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase1/WORKBENCH_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase2/ReferenceUpgrade/CompleteWanderer/WANDERER_COMPLETE_HIGHDETAIL_V1_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase2/ReferenceUpgrade/CompleteWanderer/WANDERER_COMPLETE_PRODUCTION_LOD_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase2/ReferenceUpgrade/CompleteWanderer/WANDERER_UNITY_INTEGRATION_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase3/PHASE3_FINAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase3/REMAINING_CREATURES_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase3/REMAINING_CREATURES_UNITY_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase4/BASE_MODULES_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase4/FORGE_CONTAINERS_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase4/PRODUCTION_STATIONS_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase4/STORAGE_CHEST_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase4/WORLD_CHESTS_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase4/WORLD_LOOT_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase5/ENVIRONMENT_KIT_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase5/TIER_TWO_ORE_TECHNICAL_REPORT.json",
			"Documentation/Etappen/MidPoly/Phase5/TIER_TWO_VEGETATION_TECHNICAL_REPORT.json"
		};

		[MenuItem("Eidren/Mid-Poly/Phase 7/Gesamt-QA Assetreport")]
		public static void Run()
		{
			FinalReport report = new FinalReport
			{
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				issues = new List<string>(),
				technicalReports = new List<ReportResult>(),
				families = new List<FamilyResult>(),
				documentedExceptions = new List<string>
				{
					"Der Wanderer verwendet auf ausdruecklichen Nutzerwunsch seit 25.08.2026 wieder die produktive Low-Poly-Fassung; alle anderen Assetfamilien bleiben Mid-Poly."
				}
			};

			List<string> prefabs = ProductionRoots
				.SelectMany(root => AssetDatabase.FindAssets("t:Prefab", new[] { root }).Select(AssetDatabase.GUIDToAssetPath))
				.Where(IsProductionPath).Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).ToList();
			report.productionPrefabs = prefabs.Count;
			foreach (string path in prefabs) ValidatePrefab(path, report);

			ValidateActors(report);
			ValidateEnvironment(report);
			ValidateResourcePairs(report);
			ValidateScenes(report);
			ValidateStatusRegister(report);
			ValidateReports(report);
			ValidatePerformance(report);
			ValidateLegacyArchive(report);

			report.pass = report.issues.Count == 0 && report.technicalReports.All(item => item.pass) && report.families.All(item => item.pass);
			Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
			File.WriteAllText(OutputPath, JsonUtility.ToJson(report, true));
			if (!report.pass) throw new InvalidOperationException("MIDPOLY-000 Gesamt-QA FAIL. Siehe " + OutputPath + ".");
			Debug.Log(string.Format("[MIDPOLY-000] Gesamt-QA PASS: {0} Produktionsprefabs, {1} Szenen, keine Legacy-Abhaengigkeit.", report.productionPrefabs, report.scenes));
		}

		private static void ValidatePrefab(string path, FinalReport report)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab == null) { report.issues.Add("Prefab nicht ladbar: " + path); return; }
			foreach (string dependency in AssetDatabase.GetDependencies(path, true))
			{
				if (IsLegacyPath(dependency)) report.issues.Add(path + " referenziert Legacy: " + dependency);
			}
			if (prefab.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null))
				report.issues.Add("Belegter 2D-Renderer im produktiven 3D-Prefab: " + path);
			foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
			{
				if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.Any(material => material == null))
					report.issues.Add("Fehlendes Material: " + path + " / " + renderer.name);
				if (renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 4)
					report.issues.Add("Mehr als vier Materialslots ohne Asset-Split: " + path + " / " + renderer.name);
			}
			foreach (LODGroup group in prefab.GetComponentsInChildren<LODGroup>(true))
			{
				LOD[] lods = group.GetLODs();
				if (lods.Length < 2 || lods.Any(lod => lod.renderers == null || lod.renderers.Length == 0) ||
					lods.Select(lod => lod.screenRelativeTransitionHeight).Where(value => value > 0f).Zip(lods.Select(lod => lod.screenRelativeTransitionHeight).Where(value => value > 0f).Skip(1), (left, right) => left > right).Any(valid => !valid))
				{
					report.issues.Add("Ungueltige LODGroup: " + path + " / " + group.name);
				}
			}
		}

		private static void ValidateActors(FinalReport report)
		{
			string root = "Assets/_Game/Prefabs/Actors/3D";
			List<string> actors = AssetDatabase.FindAssets("t:Prefab", new[] { root }).Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => IsProductionPath(path) && string.Equals(Path.GetDirectoryName(path)?.Replace('\\', '/'), root, StringComparison.Ordinal)).ToList();
			FamilyResult family = new FamilyResult { family = "Actors", assets = actors.Count, pass = true, issues = new List<string>() };
			if (actors.Count != 16) family.issues.Add("Erwartet 16 produktive Figuren, gefunden " + actors.Count + ".");
			foreach (string path in actors)
			{
				GameObject actor = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0) family.issues.Add("Kein SkinnedMesh: " + path);
				LODGroup group = actor.GetComponent<LODGroup>();
				if (string.Equals(path, LowPolyWandererPath, StringComparison.Ordinal))
				{
					if (group != null) family.issues.Add("Low-Poly-Wanderer traegt unerwartet eine Mid-Poly-LODGroup: " + path);
				}
				else if (group == null || group.GetLODs().Length != 3) family.issues.Add("Figuren-LOD-Vertrag fehlt: " + path);
				int clips = AssetDatabase.GetDependencies(path, true).SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>()
					.Count(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
				if (clips == 0) family.issues.Add("Kein reimportierter Clip: " + path);
			}
			family.pass = family.issues.Count == 0;
			report.families.Add(family);
		}

		private static void ValidateEnvironment(FinalReport report)
		{
			string root = "Assets/_Game/Prefabs/Environment/StyleProof";
			List<string> assets = AssetDatabase.FindAssets("t:Prefab", new[] { root }).Select(AssetDatabase.GUIDToAssetPath).Where(IsProductionPath).ToList();
			FamilyResult family = new FamilyResult { family = "Environment", assets = assets.Count, pass = true, issues = new List<string>() };
			if (assets.Count != 16) family.issues.Add("Erwartet 16 Umgebungsprops, gefunden " + assets.Count + ".");
			foreach (string path in assets)
			{
				LODGroup group = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<LODGroup>();
				if (group == null || group.GetLODs().Length != 3) family.issues.Add("Umgebungs-LOD-Vertrag fehlt: " + path);
			}
			family.pass = family.issues.Count == 0;
			report.families.Add(family);
		}

		private static void ValidateResourcePairs(FinalReport report)
		{
			string root = "Assets/_Game/Prefabs/Resources/Visuals";
			HashSet<string> paths = AssetDatabase.FindAssets("t:Prefab", new[] { root }).Select(AssetDatabase.GUIDToAssetPath).Where(IsProductionPath).ToHashSet(StringComparer.Ordinal);
			List<string> active = paths.Where(path => path.EndsWith("_Active.prefab", StringComparison.Ordinal)).ToList();
			FamilyResult family = new FamilyResult { family = "ResourceStates", assets = paths.Count, pass = true, issues = new List<string>() };
			foreach (string path in active)
			{
				string exhausted = path.Replace("_Active.prefab", "_Exhausted.prefab");
				if (!paths.Contains(exhausted)) family.issues.Add("Exhausted-Paar fehlt: " + path);
			}
			family.pass = active.Count > 0 && family.issues.Count == 0;
			report.families.Add(family);
		}

		private static void ValidateScenes(FinalReport report)
		{
			foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
			{
				report.scenes++;
				if (string.IsNullOrEmpty(scene.path) || !File.Exists(scene.path)) { report.issues.Add("Build-Szene fehlt: " + scene.path); continue; }
				foreach (string dependency in AssetDatabase.GetDependencies(scene.path, true))
					if (IsLegacyPath(dependency)) report.issues.Add(scene.path + " referenziert Legacy: " + dependency);
			}
		}

		private static void ValidateStatusRegister(FinalReport report)
		{
			if (!File.Exists(StatusPath)) { report.issues.Add("Migrationsstatus fehlt."); return; }
			string[] rows = File.ReadAllLines(StatusPath).Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
			report.statusRows = rows.Length;
			report.acceptedStatusRows = rows.Count(line => line.Contains(";\"abgenommen\";"));
			if (report.statusRows == 0 || report.acceptedStatusRows != report.statusRows)
				report.issues.Add(string.Format("Migrationsstatus nicht abgeschlossen: {0}/{1} abgenommen.", report.acceptedStatusRows, report.statusRows));
		}

		private static void ValidateReports(FinalReport report)
		{
			foreach (string path in RequiredReports)
			{
				ReportResult result = new ReportResult { path = path };
				if (!File.Exists(path)) result.issue = "Report fehlt.";
				else
				{
					string json = File.ReadAllText(path);
					PassEnvelope envelope = JsonUtility.FromJson<PassEnvelope>(json);
					result.pass = envelope != null && (envelope.pass || string.Equals(envelope.status, "complete", StringComparison.OrdinalIgnoreCase));
					if (!result.pass) result.issue = "Report ist nicht PASS/complete.";
				}
				report.technicalReports.Add(result);
			}
		}

		private static void ValidatePerformance(FinalReport report)
		{
			if (!File.Exists(PerformancePath)) { report.issues.Add("Performancebericht fehlt."); return; }
			PassEnvelope envelope = JsonUtility.FromJson<PassEnvelope>(File.ReadAllText(PerformancePath));
			if (envelope == null || !envelope.pass) report.issues.Add("Performancebericht ist nicht PASS.");
		}

		private static void ValidateLegacyArchive(FinalReport report)
		{
			string assetsRoot = Application.dataPath + "/_Game";
			report.legacyAssetsInRuntime = Directory.GetFiles(assetsRoot, "*", SearchOption.AllDirectories)
				.Select(path => path.Replace('\\', '/')).Count(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) && IsLegacyPath(path));
			if (report.legacyAssetsInRuntime != 0) report.issues.Add("Legacy-/Fallback-Assets liegen noch im Runtime-Assetbaum: " + report.legacyAssetsInRuntime + ".");
		}

		private static bool IsProductionPath(string path) => !string.IsNullOrEmpty(path) && !IsLegacyPath(path) && !path.Contains("/Review/");
		private static bool IsLegacyPath(string path) => path.Replace('\\', '/').Contains("/Fallback/") || path.IndexOf("_Legacy", StringComparison.OrdinalIgnoreCase) >= 0 || path.Contains("/Prefabs/Actors/2D/");

		[Serializable] private sealed class PassEnvelope { public bool pass; public string status; }
		[Serializable] private sealed class ReportResult { public string path; public bool pass; public string issue; }
		[Serializable] private sealed class FamilyResult { public string family; public bool pass; public int assets; public List<string> issues; }

		[Serializable]
		private sealed class FinalReport
		{
			public bool pass;
			public string generatedUtc;
			public int productionPrefabs;
			public int scenes;
			public int statusRows;
			public int acceptedStatusRows;
			public int legacyAssetsInRuntime;
			public List<FamilyResult> families;
			public List<ReportResult> technicalReports;
			public List<string> documentedExceptions;
			public List<string> issues;
		}
	}
}
