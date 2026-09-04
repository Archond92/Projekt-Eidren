using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
	/// <summary>
	/// Phase-1-Eingangsaudit der drei MIDPOLY-000-Golden-Master-Kandidaten.
	/// Der Audit integriert bewusst nichts; er trennt erfolgreichen Unity-
	/// Reimport von der spaeteren Produktionsfreigabe.
	/// </summary>
	public static class MidpolyGoldenMasterAudit
	{
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase1/GOLDEN_MASTER_CANDIDATE_REPORT.json";

		[MenuItem("Eidren/Mid-Poly/Phase 1/Golden-Master-Kandidaten pruefen")]
		public static void Run()
		{
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			AuditReport report = new AuditReport
			{
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				candidates = new List<CandidateResult>()
			};

			report.candidates.Add(AuditActor(
				"Wildling",
				"Assets/_Game/Art/MidPoly/GoldenMasters/Wildling/Runtime/CHR_Wildling_Mid_Lod0.glb",
				"Assets/_Game/Art/Actors/Wildling/Wildling3D/Wildling.glb",
				35000));

			report.candidates.Add(AuditActor(
				"Wanderer",
				"Assets/_Game/Art/MidPoly/GoldenMasters/Wanderer/Runtime/CHR_Wanderer_Mid_LOD0.glb",
				"Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb",
				65000));

			report.candidates.Add(AuditWorkbench());
			report.allImported = report.candidates.All(candidate => candidate.importPass);
			report.allProductionReady = report.candidates.All(candidate => candidate.productionReady);

			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, prettyPrint: true));
			AssetDatabase.Refresh();
			Debug.Log("[MIDPOLY-000] Golden-Master-Eingangsaudit: Unity-Import "
				+ (report.allImported ? "PASS" : "FAIL") + ", Produktionsfreigabe "
				+ (report.allProductionReady ? "PASS" : "OFFEN") + ". Bericht: " + ReportPath);
		}

		private static CandidateResult AuditActor(string name, string path, string referencePath, int triangleBudget, params string[] knownBlockers)
		{
			GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			GameObject reference = AssetDatabase.LoadAssetAtPath<GameObject>(referencePath);
			CandidateResult result = BaseResult(name, path, candidate);
			result.referencePath = referencePath;
			result.triangleBudget = triangleBudget;
			result.referenceBounds = MeasureBounds(reference);
			result.referenceClips = ClipNames(referencePath);
			result.referenceBones = BoneNames(reference);
			result.clips = ClipNames(path);
			result.bones = BoneNames(candidate);

			if (candidate == null)
			{
				result.importIssues.Add("Kandidat wurde nicht als Unity-GameObject importiert.");
			}
			if (result.triangles <= 0)
			{
				result.importIssues.Add("Keine importierte Mesh-Geometrie.");
			}
			if (!result.clips.SequenceEqual(result.referenceClips, StringComparer.Ordinal))
			{
				result.importIssues.Add("Clipset weicht von der produktiven Referenz ab: erwartet [" + string.Join(", ", result.referenceClips) + "], erhalten [" + string.Join(", ", result.clips) + "].");
			}
			if (!result.bones.SequenceEqual(result.referenceBones, StringComparer.Ordinal))
			{
				result.importIssues.Add("Bone-Namen/-Anzahl weichen von der produktiven Referenz ab.");
			}
			// Phase 2 erweitert die Wanderer-Silhouette durch Helm, Schulter-/Arm-
			// und Beinpanzer deutlich gegenueber dem alten ungeruesteten GLB. Hoehe,
			// Controller und Bodenkontakt werden im Produktionsvertrag separat eng
			// geprueft; der Eingangsaudit erlaubt deshalb nur hier die Ausruestungsbreite.
			float boundsTolerance = name == "Wanderer" ? 0.60f : 0.18f;
			if (!BoundsCompatible(result.bounds, result.referenceBounds, boundsTolerance))
			{
				result.importIssues.Add("Import-Bounds weichen um mehr als " + Mathf.RoundToInt(boundsTolerance * 100f) + " % von der produktiven Referenz ab.");
			}

			if (result.triangles > triangleBudget)
			{
				result.productionBlockers.Add("LOD0 liegt mit " + result.triangles + " Tris ueber dem Auditbudget " + triangleBudget + ".");
			}
			if (name == "Wildling")
			{
				if (result.materialSlots > 4)
				{
					result.productionBlockers.Add("Wildling verwendet mehr als vier produktive Materialien.");
				}
				GameObject productionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
					"Assets/_Game/Prefabs/Actors/3D/Wildling_3D.prefab");
				LODGroup lodGroup = productionPrefab != null ? productionPrefab.GetComponent<LODGroup>() : null;
				if (lodGroup == null || lodGroup.GetLODs().Length != 3)
				{
					result.productionBlockers.Add("Produktiver Wildling-Prefab besitzt keine drei LOD-Stufen.");
				}
			}
			if (name == "Wanderer")
			{
				if (result.materialSlots > 5)
					result.productionBlockers.Add("Wanderer verwendet mehr als fuenf produktive Materialien.");
				GameObject productionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
					"Assets/_Game/Prefabs/Actors/3D/Player_3D.prefab");
				LODGroup lodGroup = productionPrefab != null ? productionPrefab.GetComponent<LODGroup>() : null;
				if (lodGroup == null || lodGroup.GetLODs().Length != 3)
					result.productionBlockers.Add("Produktiver Wanderer-Prefab besitzt keine drei LOD-Stufen.");
				string[] modules =
				{
					"Basis", "Helm_Stoff", "Helm_Kupfer", "Helm_Eisen", "Harnisch_Stoff", "Harnisch_Kupfer", "Harnisch_Eisen",
					"Haende_Stoff", "Haende_Kupfer", "Haende_Eisen", "Beine_Stoff", "Beine_Kupfer", "Beine_Eisen",
					"Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer", "Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense",
					"Waffe_Hammer_Base", "Waffe_Hammer_Kupfer", "Waffe_Hammer_Eisen", "Waffe_Hammer_Sealbreaker",
					"Waffe_Dolche_Base", "Waffe_Dolche_Kupfer", "Waffe_Dolche_Eisen", "Waffe_Dolche_AshFangs",
					"Waffe_Speer_Kupfer", "Waffe_Speer_Eisen", "Waffe_Speer_EmberThorn",
					"Waffe_Axt_Base", "Waffe_Axt_Kupfer", "Waffe_Axt_Eisen",
					"Waffe_Spitzhacke_Base", "Waffe_Spitzhacke_Kupfer", "Waffe_Spitzhacke_Eisen",
					"Waffe_Sense_Base", "Waffe_Sense_Kupfer", "Waffe_Sense_Eisen"
				};
				if (productionPrefab == null || modules.Any(module => productionPrefab.GetComponentsInChildren<Transform>(true).All(child => child.name != module)))
					result.productionBlockers.Add("Produktiver Wanderer besitzt nicht alle 39 Ausruestungsmodule.");
			}
			result.productionBlockers.AddRange(knownBlockers);
			result.importPass = result.importIssues.Count == 0;
			result.productionReady = result.importPass && result.productionBlockers.Count == 0;
			return result;
		}

		private static CandidateResult AuditWorkbench()
		{
			const string path = "Assets/_Game/Art/MidPoly/GoldenMasters/Workbench/Runtime/BLD_Workbench_Mid_Lod0.glb";
			const string referencePath = "Assets/_Game/Prefabs/Stations/Workbench.prefab";
			GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			GameObject reference = AssetDatabase.LoadAssetAtPath<GameObject>(referencePath);
			CandidateResult result = BaseResult("Workbench", path, candidate);
			result.referencePath = referencePath;
			result.triangleBudget = 30000;
			result.referenceBounds = MeasureBounds(reference);

			if (candidate == null || result.triangles <= 0)
			{
				result.importIssues.Add("Werkbank-Golden-Master besitzt keine importierte Mesh-Geometrie.");
			}
			if (result.triangles < 8000 || result.triangles > result.triangleBudget)
			{
				result.importIssues.Add("Werkbank liegt ausserhalb des LOD0-Zielbereichs 8.000-30.000 Tris.");
			}
			if (!BoundsCompatible(result.bounds, result.referenceBounds, 0.18f))
			{
				result.productionBlockers.Add("Produktionsbounds weichen um mehr als 18 % vom Werkbank-Prefab ab.");
			}
			if (result.materialSlots > 5)
			{
				result.productionBlockers.Add("Werkbank verwendet mehr als fuenf produktive Materialien (aktuell " + result.materialSlots + ").");
			}
			LODGroup lodGroup = reference != null ? reference.GetComponent<LODGroup>() : null;
			if (lodGroup == null || lodGroup.GetLODs().Length != 3)
				result.productionBlockers.Add("Produktiver Werkbank-Prefab besitzt keine drei LOD-Stufen.");
			if (reference == null || reference.transform.Find("SemanticAnchors/InteractionPoint") == null)
				result.productionBlockers.Add("Produktiver Werkbank-Interaktionsanker fehlt.");
			if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Stations/Fallback/Workbench_Legacy.prefab") == null)
				result.productionBlockers.Add("Werkbank-Legacy-Fallback fehlt.");
			result.importPass = result.importIssues.Count == 0;
			result.productionReady = result.importPass && result.productionBlockers.Count == 0;
			return result;
		}

		private static CandidateResult BaseResult(string name, string path, GameObject candidate)
		{
			return new CandidateResult
			{
				asset = name,
				candidatePath = path,
				triangles = TriangleCount(candidate),
				materialSlots = MaterialNames(candidate).Length,
				materials = MaterialNames(candidate),
				bounds = MeasureBounds(candidate),
				clips = Array.Empty<string>(),
				bones = Array.Empty<string>(),
				referenceClips = Array.Empty<string>(),
				referenceBones = Array.Empty<string>(),
				importIssues = new List<string>(),
				productionBlockers = new List<string>()
			};
		}

		private static int TriangleCount(GameObject root)
		{
			if (root == null)
			{
				return 0;
			}
			IEnumerable<Mesh> meshes = root.GetComponentsInChildren<MeshFilter>(true)
				.Where(filter => filter.sharedMesh != null).Select(filter => filter.sharedMesh)
				.Concat(root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
					.Where(renderer => renderer.sharedMesh != null).Select(renderer => renderer.sharedMesh))
				.Distinct();
			return meshes.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount)
				.Sum(index => (int)mesh.GetIndexCount(index) / 3));
		}

		private static string[] MaterialNames(GameObject root)
		{
			return root == null ? Array.Empty<string>() : root.GetComponentsInChildren<Renderer>(true)
				.SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null)
				.Select(material => material.name)
				.Distinct(StringComparer.Ordinal)
				.OrderBy(name => name, StringComparer.Ordinal)
				.ToArray();
		}

		private static string[] ClipNames(string path)
		{
			return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
				.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
				.Select(clip => clip.name)
				.Distinct(StringComparer.Ordinal)
				.OrderBy(name => name, StringComparer.Ordinal)
				.ToArray();
		}

		private static string[] BoneNames(GameObject root)
		{
			return root == null ? Array.Empty<string>() : root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
				.SelectMany(renderer => renderer.bones ?? Array.Empty<Transform>())
				.Where(bone => bone != null)
				.Select(bone => bone.name)
				.Distinct(StringComparer.Ordinal)
				.OrderBy(name => name, StringComparer.Ordinal)
				.ToArray();
		}

		private static BoundsData MeasureBounds(GameObject prefab)
		{
			if (prefab == null)
			{
				return new BoundsData();
			}
			GameObject instance = Object.Instantiate(prefab);
			try
			{
				Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
				if (renderers.Length == 0)
				{
					return new BoundsData();
				}
				Bounds bounds = renderers[0].bounds;
				foreach (Renderer renderer in renderers.Skip(1))
				{
					bounds.Encapsulate(renderer.bounds);
				}
				return new BoundsData { center = bounds.center, size = bounds.size };
			}
			finally
			{
				Object.DestroyImmediate(instance);
			}
		}

		private static bool BoundsCompatible(BoundsData candidate, BoundsData reference, float tolerance)
		{
			if (candidate.size.sqrMagnitude <= 0f || reference.size.sqrMagnitude <= 0f)
			{
				return false;
			}
			return AxisCompatible(candidate.size.x, reference.size.x, tolerance)
				&& AxisCompatible(candidate.size.y, reference.size.y, tolerance)
				&& AxisCompatible(candidate.size.z, reference.size.z, tolerance);
		}

		private static bool AxisCompatible(float candidate, float reference, float tolerance)
		{
			return Mathf.Abs(candidate - reference) <= Mathf.Max(0.05f, Mathf.Abs(reference) * tolerance);
		}

		[Serializable]
		private sealed class AuditReport
		{
			public string generatedUtc;
			public bool allImported;
			public bool allProductionReady;
			public List<CandidateResult> candidates;
		}

		[Serializable]
		private sealed class CandidateResult
		{
			public string asset;
			public string candidatePath;
			public string referencePath;
			public bool importPass;
			public bool productionReady;
			public int triangles;
			public int triangleBudget;
			public int materialSlots;
			public string[] materials;
			public string[] clips;
			public string[] referenceClips;
			public string[] bones;
			public string[] referenceBones;
			public BoundsData bounds;
			public BoundsData referenceBounds;
			public List<string> importIssues;
			public List<string> productionBlockers;
		}

		[Serializable]
		private struct BoundsData
		{
			public Vector3 center;
			public Vector3 size;
		}
	}
}
