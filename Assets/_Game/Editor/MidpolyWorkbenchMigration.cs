using Eidren.Data;
using Eidren.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Eidren.Editor
{
	/// <summary>
	/// Integriert den freigegebenen Mid-Poly-Werkbank-Golden-Master, ohne den
	/// produktiven Prefabpfad oder seine Interaktions-/Physikvertraege zu aendern.
	/// Vor dem ersten Umbau wird der vollstaendige Alt-Prefab gesichert.
	/// </summary>
	public static class MidpolyWorkbenchMigration
	{
		private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
		private const string CombinedPath = "Assets/_Game/Art/MidPoly/GoldenMasters/Workbench/Runtime/BLD_Workbench_Mid_Production.glb";
		private const string Lod0Path = "Assets/_Game/Art/MidPoly/GoldenMasters/Workbench/Runtime/BLD_Workbench_Mid_Lod0.glb";
		private const string Lod1Path = "Assets/_Game/Art/MidPoly/GoldenMasters/Workbench/Runtime/BLD_Workbench_Mid_Lod1.glb";
		private const string Lod2Path = "Assets/_Game/Art/MidPoly/GoldenMasters/Workbench/Runtime/BLD_Workbench_Mid_Lod2.glb";
		private const string PrefabPath = "Assets/_Game/Prefabs/Stations/Workbench.prefab";
		private const string FallbackPath = "Assets/_Game/Prefabs/Stations/Fallback/Workbench_Legacy.prefab";
		private const string LegacyArchiveRoot = "Documentation/Etappen/MidPoly/LegacyArchive/";
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase1/WORKBENCH_TECHNICAL_REPORT.json";

		private static readonly string[] SemanticAnchors =
		{
			"Worktop", "ToolRack", "Vise", "ToolSet", "InteractionPoint"
		};

		[Serializable]
		private sealed class MigrationRegistry
		{
			public bool workbenchPilotEnabled;
		}

		[Serializable]
		private sealed class WorkbenchReport
		{
			public string generatedUtc;
			public bool pass;
			public string prefabGuidBefore;
			public string prefabGuidAfter;
			public string legacyFallbackPath;
			public int lod0Triangles;
			public int lod1Triangles;
			public int lod2Triangles;
			public int materials;
			public int lodLevels;
			public BoundsData rendererBounds;
			public Vector3 triggerCenter;
			public Vector3 triggerSize;
			public Vector3 navigationCenter;
			public Vector3 navigationSize;
			public string interactionId;
			public float interactionRange;
			public string[] semanticAnchors;
			public string[] errors;
		}

		[Serializable]
		private struct BoundsData
		{
			public Vector3 center;
			public Vector3 size;
		}

		[MenuItem("Eidren/Mid-Poly/Phase 1/Werkbank bauen und pruefen")]
		public static void BuildAndValidate()
		{
			if (!TryBuildApprovedVisual())
			{
				throw new InvalidOperationException("Werkbank-Mid-Poly-Pilot ist nicht aktiviert oder seine Runtime-Dateien fehlen.");
			}
		}

		public static bool TryBuildApprovedVisual()
		{
			if (!PilotEnabled() || !RequiredFilesExist()) return false;

			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			string guidBefore = AssetDatabase.AssetPathToGUID(PrefabPath);
			EnsureLegacyFallback();
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
			if (!File.Exists(RegistryPath)) return false;
			MigrationRegistry registry = JsonUtility.FromJson<MigrationRegistry>(File.ReadAllText(RegistryPath));
			return registry != null && registry.workbenchPilotEnabled;
		}

		private static bool RequiredFilesExist()
		{
			return File.Exists(CombinedPath) && File.Exists(Lod0Path)
				&& File.Exists(Lod1Path) && File.Exists(Lod2Path) && File.Exists(PrefabPath);
		}

		private static void EnsureLegacyFallback()
		{
			if (File.Exists(LegacyArchiveRoot + FallbackPath)) return;
			if (File.Exists(FallbackPath)) return;
			GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			if (source == null) throw new InvalidOperationException("Produktiver Werkbank-Prefab fehlt.");
			Directory.CreateDirectory(Path.GetDirectoryName(FallbackPath));
			GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(source);
			try
			{
				copy.name = "Workbench_Legacy";
				if (PrefabUtility.SaveAsPrefabAsset(copy, FallbackPath) == null)
					throw new IOException("Legacy-Fallback konnte nicht gespeichert werden: " + FallbackPath);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(copy);
			}
		}

		private static void BuildPrefab()
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(CombinedPath);
			if (model == null) throw new InvalidOperationException("Produktions-Werkbank wurde nicht als GameObject importiert.");

			GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
			try
			{
				for (int index = root.transform.childCount - 1; index >= 0; index--)
				{
					Transform child = root.transform.GetChild(index);
					if (!string.Equals(child.name, "ContactShadow", StringComparison.Ordinal))
						UnityEngine.Object.DestroyImmediate(child.gameObject);
				}

				LODGroup oldGroup = root.GetComponent<LODGroup>();
				if (oldGroup != null) UnityEngine.Object.DestroyImmediate(oldGroup);

				GameObject geometry = (GameObject)PrefabUtility.InstantiatePrefab(model, root.scene);
				geometry.name = "Geometry_MidPoly";
				geometry.transform.SetParent(root.transform, false);
				Renderer[] renderers = geometry.GetComponentsInChildren<Renderer>(true);
				foreach (Renderer renderer in renderers)
				{
					renderer.shadowCastingMode = ShadowCastingMode.On;
					renderer.receiveShadows = true;
				}

				Renderer[] lod0 = RenderersForLod(renderers, "LOD0");
				Renderer[] lod1 = RenderersForLod(renderers, "LOD1");
				Renderer[] lod2 = RenderersForLod(renderers, "LOD2");
				if (lod0.Length == 0 || lod1.Length == 0 || lod2.Length == 0)
					throw new InvalidOperationException("Mindestens eine Werkbank-LOD-Stufe besitzt keinen Renderer.");
				NormalizeToScaleTable(geometry.transform, lod0);

				LODGroup group = root.AddComponent<LODGroup>();
				group.fadeMode = LODFadeMode.CrossFade;
				group.animateCrossFading = true;
				group.SetLODs(new[]
				{
					new LOD(0.55f, lod0) { fadeTransitionWidth = 0.12f },
					new LOD(0.25f, lod1) { fadeTransitionWidth = 0.12f },
					// F33-001: Einheitliche Sicherheitsreserve fuer alle
					// baubaren Mid-Poly-Objekte bei der 7,4er-Spielkamera.
					new LOD(0.01f, lod2) { fadeTransitionWidth = 0.12f }
				});
				group.RecalculateBounds();
				// OP-11: Schwellen fuer die orthografische Spielkamera (LOD0 im Spielzoom sichtbar).
				MidpolyLodThresholds.ApplyOrthographicThresholds(group);

				Transform anchors = new GameObject("SemanticAnchors").transform;
				anchors.SetParent(root.transform, false);
				foreach (string name in SemanticAnchors)
				{
					Transform anchor = new GameObject(name).transform;
					anchor.SetParent(anchors, false);
				}
				anchors.Find("Worktop").localPosition = new Vector3(0f, 1.05f, 0f);
				anchors.Find("ToolRack").localPosition = new Vector3(0f, 1.15f, 0.3f);
				anchors.Find("Vise").localPosition = new Vector3(-0.42f, 1.02f, -0.18f);
				anchors.Find("ToolSet").localPosition = new Vector3(0.25f, 1.02f, 0f);
				anchors.Find("InteractionPoint").localPosition = new Vector3(0f, 0f, -0.72f);

				PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		private static void NormalizeToScaleTable(Transform geometry, Renderer[] lod0)
		{
			Bounds bounds = lod0[0].bounds;
			foreach (Renderer renderer in lod0.Skip(1)) bounds.Encapsulate(renderer.bounds);
			if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f)
				throw new InvalidOperationException("Werkbank-LOD0 besitzt ungueltige Bounds.");
			Vector3 scale = geometry.localScale;
			scale.x *= 1.0f / bounds.size.x;
			scale.y *= 1.1f / bounds.size.y;
			scale.z *= 1.0f / bounds.size.z;
			geometry.localScale = scale;
		}

		private static Renderer[] RenderersForLod(IEnumerable<Renderer> renderers, string lod)
		{
			return renderers.Where(renderer => HierarchyContains(renderer.transform, lod)).ToArray();
		}

		private static bool HierarchyContains(Transform transform, string value)
		{
			for (Transform current = transform; current != null; current = current.parent)
				if (current.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
			return false;
		}

		private static void LabelAssets()
		{
			foreach (string path in new[] { CombinedPath, Lod0Path, Lod1Path, Lod2Path, PrefabPath, FallbackPath })
			{
				UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
				if (asset != null) AssetDatabase.SetLabels(asset,
					new[] { "MIDPOLY-000", "MidPoly", "Approved", "WorkbenchGoldenMaster" });
			}
		}

		private static void ValidateAndWriteReport(string guidBefore, string guidAfter)
		{
			List<string> errors = new List<string>();
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			LODGroup group = prefab != null ? prefab.GetComponent<LODGroup>() : null;
			Renderer[] renderers = prefab != null ? prefab.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
			Renderer[] lod0Renderers = RenderersForLod(renderers, "LOD0");
			Renderer[] lod1Renderers = RenderersForLod(renderers, "LOD1");
			Renderer[] lod2Renderers = RenderersForLod(renderers, "LOD2");
			int lod0 = TriangleCount(lod0Renderers);
			int lod1 = TriangleCount(lod1Renderers);
			int lod2 = TriangleCount(lod2Renderers);
			string[] materials = lod0Renderers.SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null).Select(material => material.name)
				.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
			BoxCollider trigger = prefab != null ? prefab.GetComponent<BoxCollider>() : null;
			NavMeshObstacle obstacle = prefab != null ? prefab.GetComponent<NavMeshObstacle>() : null;
			WorkbenchController controller = prefab != null ? prefab.GetComponent<WorkbenchController>() : null;
			Transform anchors = prefab != null ? prefab.transform.Find("SemanticAnchors") : null;
			string[] foundAnchors = anchors == null ? Array.Empty<string>() : anchors.Cast<Transform>()
				.Select(child => child.name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
			Bounds bounds = CombinedBounds(prefab, lod0Renderers);

			if (prefab == null) errors.Add("Produktiver Werkbank-Prefab fehlt.");
			if (string.IsNullOrEmpty(guidBefore) || guidBefore != guidAfter) errors.Add("Prefab-GUID wurde nicht erhalten.");
			if (File.Exists(FallbackPath)) errors.Add("Legacy-Fallback liegt nach Abschluss noch im Runtime-Baum.");
			if (!File.Exists(LegacyArchiveRoot + FallbackPath)) errors.Add("Archivierter Legacy-Fallback fehlt.");
			if (group == null || group.GetLODs().Length != 3) errors.Add("LODGroup besitzt nicht genau drei Stufen.");
			if (lod0 != 18876 || lod1 != 9438 || lod2 != 3764)
				errors.Add("LOD-Dreiecksvertrag weicht ab: " + lod0 + "/" + lod1 + "/" + lod2 + ".");
			if (materials.Length != 5) errors.Add("Erwartet sind fuenf produktive Materialien, erhalten: " + materials.Length + ".");
			if (prefab != null && prefab.GetComponentsInChildren<Animation>(true).Length != 0)
				errors.Add("Statische Werkbank darf keine Animation enthalten.");
			if (prefab != null && prefab.GetComponentsInChildren<MeshCollider>(true).Length != 0)
				errors.Add("Werkbank darf keinen MeshCollider enthalten.");
			if (bounds.size.x > 1.01f || Mathf.Abs(bounds.size.y - 1.10f) > 0.012f || bounds.size.z > 1.01f || bounds.center.z >= 0f)
				errors.Add("Renderer-Bounds/Bedienseite verletzen den produktiven Footprint: " + bounds + ".");
			if (trigger == null || !trigger.isTrigger || !Approximately(trigger.center, new Vector3(0f, 0.75f, 0f))
				|| !Approximately(trigger.size, new Vector3(1f, 1.5f, 1f)))
				errors.Add("Bestehender Triggervertrag wurde veraendert.");
			if (obstacle == null || !obstacle.carving || !Approximately(obstacle.center, new Vector3(0f, 0.75f, 0f))
				|| !Approximately(obstacle.size, new Vector3(1f, 1.5f, 1f)))
				errors.Add("Bestehender Navigationsvertrag wurde veraendert.");
			if (controller == null || controller.InteractionId != "home_base.workbench"
				|| controller.DisplayText != "Werkbank benutzen" || Mathf.Abs(controller.InteractionRange - 2f) > 0.001f
				|| controller.Station != CraftingStationType.Workbench)
				errors.Add("Bestehender Interaktionsvertrag wurde veraendert.");
			if (prefab == null || prefab.transform.Find("ContactShadow") == null) errors.Add("Kontaktschatten fehlt.");
			if (!foundAnchors.SequenceEqual(SemanticAnchors.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal))
				errors.Add("Semantische Funktionsanker fehlen oder weichen ab.");

			WorkbenchReport report = new WorkbenchReport
			{
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				pass = errors.Count == 0,
				prefabGuidBefore = guidBefore,
				prefabGuidAfter = guidAfter,
				legacyFallbackPath = FallbackPath,
				lod0Triangles = lod0,
				lod1Triangles = lod1,
				lod2Triangles = lod2,
				materials = materials.Length,
				lodLevels = group == null ? 0 : group.GetLODs().Length,
				rendererBounds = new BoundsData { center = bounds.center, size = bounds.size },
				triggerCenter = trigger == null ? Vector3.zero : trigger.center,
				triggerSize = trigger == null ? Vector3.zero : trigger.size,
				navigationCenter = obstacle == null ? Vector3.zero : obstacle.center,
				navigationSize = obstacle == null ? Vector3.zero : obstacle.size,
				interactionId = controller == null ? string.Empty : controller.InteractionId,
				interactionRange = controller == null ? 0f : controller.InteractionRange,
				semanticAnchors = foundAnchors,
				errors = errors.ToArray()
			};
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
			AssetDatabase.Refresh();
			if (!report.pass) throw new InvalidOperationException("Werkbank-Migration FAIL:\n" + string.Join("\n", errors));
			Debug.Log("[MIDPOLY-000] Werkbank PASS: " + lod0 + "/" + lod1 + "/" + lod2
				+ " Tris, 5 Materialien, Interaktion/Physik erhalten, GUID erhalten.");
		}

		private static int TriangleCount(IEnumerable<Renderer> renderers)
		{
			return renderers.Select(RendererMesh).Where(mesh => mesh != null).Distinct()
				.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount).Sum(index => (int)mesh.GetIndexCount(index) / 3));
		}

		private static Mesh RendererMesh(Renderer renderer)
		{
			SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
			if (skinned != null) return skinned.sharedMesh;
			MeshFilter filter = renderer.GetComponent<MeshFilter>();
			return filter != null ? filter.sharedMesh : null;
		}

		private static Bounds CombinedBounds(GameObject prefab, Renderer[] sourceRenderers)
		{
			if (prefab == null || sourceRenderers.Length == 0) return new Bounds();
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				Renderer[] renderers = RenderersForLod(instance.GetComponentsInChildren<Renderer>(true), "LOD0");
				if (renderers.Length == 0) return new Bounds();
				Bounds bounds = renderers[0].bounds;
				foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
				return bounds;
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
		}

		private static bool Approximately(Vector3 left, Vector3 right)
		{
			return (left - right).sqrMagnitude < 0.000001f;
		}
	}
}
