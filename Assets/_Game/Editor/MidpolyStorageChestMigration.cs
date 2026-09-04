using Eidren.Interaction;
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
	/// Tauscht ausschliesslich die Grafik der Heimatlagerkiste. Root, GUID,
	/// Containerzustand, Interaktionsreichweite und Gameplay-Trigger bleiben
	/// erhalten. Der getrennte Deckel wird auf seiner realen Scharnierachse
	/// geprueft, ohne dem bisher statischen StorageContainer neue Logik zu geben.
	/// </summary>
	public static class MidpolyStorageChestMigration
	{
		private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
		private const string CombinedPath = "Assets/_Game/Art/MidPoly/Stations/StorageChest/Runtime/PRP_StorageChest_Mid_Production.glb";
		private const string Lod0Path = "Assets/_Game/Art/MidPoly/Stations/StorageChest/Runtime/PRP_StorageChest_Mid_LOD0.glb";
		private const string Lod1Path = "Assets/_Game/Art/MidPoly/Stations/StorageChest/Runtime/PRP_StorageChest_Mid_LOD1.glb";
		private const string Lod2Path = "Assets/_Game/Art/MidPoly/Stations/StorageChest/Runtime/PRP_StorageChest_Mid_LOD2.glb";
		private const string PrefabPath = "Assets/_Game/Prefabs/Stations/StorageChest.prefab";
		private const string FallbackPath = "Assets/_Game/Prefabs/Stations/Fallback/StorageChest_Legacy.prefab";
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase4/STORAGE_CHEST_TECHNICAL_REPORT.json";

		private static readonly string[] SemanticAnchors =
		{
			"ChestBody", "ChestLid", "ChestFrontPlank_0", "LidPlank_0", "FrontBand",
			"Latch", "LatchRing", "LidHinge", "LootSocket", "InteractionPoint"
		};

		[Serializable]
		private sealed class MigrationRegistry
		{
			public bool storageChestPilotEnabled;
		}

		private sealed class GameplayContract
		{
			public string containerId;
			public string displayText;
			public float interactionRange;
			public int slots;
			public Sprite icon;
			public bool trigger;
			public Vector3 triggerCenter;
			public Vector3 triggerSize;
		}

		[Serializable]
		private sealed class StorageChestReport
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
			public BoundsData closedBounds;
			public BoundsData openedLidBounds;
			public Vector3 triggerCenter;
			public Vector3 triggerSize;
			public string interactionId;
			public string displayText;
			public float interactionRange;
			public int slots;
			public string[] semanticAnchors;
			public string[] lidPivots;
			public string[] errors;
		}

		[Serializable]
		private struct BoundsData
		{
			public Vector3 center;
			public Vector3 size;
		}

		[MenuItem("Eidren/Mid-Poly/Phase 4/Lagerkiste bauen und pruefen")]
		public static void BuildAndValidate()
		{
			if (!TryBuildApprovedVisual())
				throw new InvalidOperationException("Lagerkisten-Mid-Poly-Pilot ist nicht aktiviert oder seine Runtime-Dateien fehlen.");
		}

		public static bool TryBuildApprovedVisual()
		{
			if (!PilotEnabled() || !RequiredFilesExist()) return false;
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			string guidBefore = AssetDatabase.AssetPathToGUID(PrefabPath);
			GameplayContract before = CaptureContract(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
			EnsureLegacyFallback();
			BuildPrefab();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			string guidAfter = AssetDatabase.AssetPathToGUID(PrefabPath);
			LabelAssets();
			ValidateAndWriteReport(guidBefore, guidAfter, before);
			return true;
		}

		private static bool PilotEnabled()
		{
			if (!File.Exists(RegistryPath)) return false;
			MigrationRegistry registry = JsonUtility.FromJson<MigrationRegistry>(File.ReadAllText(RegistryPath));
			return registry != null && registry.storageChestPilotEnabled;
		}

		private static bool RequiredFilesExist()
		{
			return File.Exists(CombinedPath) && File.Exists(Lod0Path) && File.Exists(Lod1Path)
				&& File.Exists(Lod2Path) && File.Exists(PrefabPath);
		}

		private static GameplayContract CaptureContract(GameObject prefab)
		{
			if (prefab == null) throw new InvalidOperationException("Produktiver Lagerkisten-Prefab fehlt.");
			StorageContainer storage = prefab.GetComponent<StorageContainer>();
			BoxCollider trigger = prefab.GetComponent<BoxCollider>();
			if (storage == null || trigger == null) throw new InvalidOperationException("Lagerkisten-Gameplayvertrag ist unvollstaendig.");
			return new GameplayContract
			{
				containerId = storage.ContainerId,
				displayText = storage.DisplayText,
				interactionRange = storage.InteractionRange,
				slots = storage.SlotCapacity,
				icon = storage.Icon,
				trigger = trigger.isTrigger,
				triggerCenter = trigger.center,
				triggerSize = trigger.size
			};
		}

		private static void EnsureLegacyFallback()
		{
			if (MidpolyLegacyArchiveGuard.HasBackup(FallbackPath)) return;
			GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Directory.CreateDirectory(Path.GetDirectoryName(FallbackPath));
			GameObject copy = UnityEngine.Object.Instantiate(source);
			try
			{
				copy.name = "StorageChest_Legacy";
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
			if (model == null) throw new InvalidOperationException("Produktions-Lagerkiste wurde nicht als GameObject importiert.");
			GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
			try
			{
				Transform oldGeometry = root.transform.Find("Geometry_A14");
				if (oldGeometry != null) UnityEngine.Object.DestroyImmediate(oldGeometry.gameObject);
				Transform oldMid = root.transform.Find("Geometry_MidPoly");
				if (oldMid != null) UnityEngine.Object.DestroyImmediate(oldMid.gameObject);
				LODGroup oldGroup = root.GetComponent<LODGroup>();
				if (oldGroup != null) UnityEngine.Object.DestroyImmediate(oldGroup);

				GameObject geometry = (GameObject)PrefabUtility.InstantiatePrefab(model, root.scene);
				geometry.name = "Geometry_A14";
				geometry.transform.SetParent(root.transform, false);
				// Blender -Y ist nach dem GLB-Roundtrip Unity +Z. Der produktive
				// Bedienpunkt der Kiste liegt jedoch wie beim Alt-Prefab auf -Z.
				geometry.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
				foreach (Renderer renderer in geometry.GetComponentsInChildren<Renderer>(true))
				{
					renderer.shadowCastingMode = ShadowCastingMode.On;
					renderer.receiveShadows = true;
				}

				Renderer[] renderers = geometry.GetComponentsInChildren<Renderer>(true);
				Renderer[] lod0 = RenderersForLod(renderers, "LOD0");
				Renderer[] lod1 = RenderersForLod(renderers, "LOD1");
				Renderer[] lod2 = RenderersForLod(renderers, "LOD2");
				if (lod0.Length == 0 || lod1.Length == 0 || lod2.Length == 0)
					throw new InvalidOperationException("Mindestens eine Lagerkisten-LOD-Stufe besitzt keinen Renderer.");

				LODGroup group = root.AddComponent<LODGroup>();
				group.fadeMode = LODFadeMode.CrossFade;
				group.animateCrossFading = true;
				group.SetLODs(new[]
				{
					new LOD(0.58f, lod0) { fadeTransitionWidth = 0.10f },
					new LOD(0.28f, lod1) { fadeTransitionWidth = 0.10f },
					// F33-001: Die Lagerkiste lag bei der 7,4er-Spielkamera
					// unter 0,08 und wurde nach dem Bauen komplett ausgeblendet.
					new LOD(0.01f, lod2) { fadeTransitionWidth = 0.10f }
				});
				group.RecalculateBounds();
				// OP-11: Schwellen fuer die orthografische Spielkamera (LOD0 im Spielzoom sichtbar).
				MidpolyLodThresholds.ApplyOrthographicThresholds(group);

				foreach (string name in SemanticAnchors)
				{
					Transform anchor = new GameObject(name).transform;
					anchor.SetParent(geometry.transform, false);
				}
				geometry.transform.Find("ChestBody").localPosition = new Vector3(0f, 0.28f, 0f);
				geometry.transform.Find("ChestLid").localPosition = new Vector3(0f, 0.53f, -0.34f);
				geometry.transform.Find("ChestFrontPlank_0").localPosition = new Vector3(-0.35f, 0.30f, 0.36f);
				geometry.transform.Find("LidPlank_0").localPosition = new Vector3(0f, 0.62f, 0.24f);
				geometry.transform.Find("FrontBand").localPosition = new Vector3(0f, 0.30f, 0.39f);
				geometry.transform.Find("Latch").localPosition = new Vector3(0f, 0.36f, 0.43f);
				geometry.transform.Find("LatchRing").localPosition = new Vector3(0f, 0.33f, 0.47f);
				geometry.transform.Find("LidHinge").localPosition = new Vector3(0f, 0.53f, -0.34f);
				geometry.transform.Find("LootSocket").localPosition = new Vector3(0f, 0.46f, 0f);
				geometry.transform.Find("InteractionPoint").localPosition = new Vector3(0f, 0f, 0.55f);

				if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
					throw new IOException("Produktiver Lagerkisten-Prefab konnte nicht gespeichert werden.");
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
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
					new[] { "MIDPOLY-000", "MidPoly", "Phase4", "StorageChest", "ProductionCandidate" });
			}
		}

		private static void ValidateAndWriteReport(string guidBefore, string guidAfter, GameplayContract before)
		{
			List<string> errors = new List<string>();
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			GameplayContract after = prefab == null ? null : CaptureContract(prefab);
			LODGroup group = prefab != null ? prefab.GetComponent<LODGroup>() : null;
			Renderer[] all = prefab != null ? prefab.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
			Renderer[] lod0Renderers = RenderersForLod(all, "LOD0");
			Renderer[] lod1Renderers = RenderersForLod(all, "LOD1");
			Renderer[] lod2Renderers = RenderersForLod(all, "LOD2");
			int lod0 = TriangleCount(lod0Renderers);
			int lod1 = TriangleCount(lod1Renderers);
			int lod2 = TriangleCount(lod2Renderers);
			string[] materials = lod0Renderers.SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null).Select(material => material.name)
				.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
			Transform geometry = prefab != null ? prefab.transform.Find("Geometry_A14") : null;
			string[] foundAnchors = geometry == null ? Array.Empty<string>() : SemanticAnchors
				.Where(name => geometry.Find(name) != null).OrderBy(name => name, StringComparer.Ordinal).ToArray();
			Transform[] lidPivots = prefab == null ? Array.Empty<Transform>() : prefab.GetComponentsInChildren<Transform>(true)
				.Where(item => item.name.StartsWith("ChestLid_LOD", StringComparison.Ordinal)).ToArray();
			Bounds closed = CombinedBounds(prefab, "LOD0");
			Bounds openedLid = ValidateOpeningAxis(prefab, errors);

			if (prefab == null) errors.Add("Produktiver Lagerkisten-Prefab fehlt.");
			if (string.IsNullOrEmpty(guidBefore) || guidBefore != guidAfter) errors.Add("Prefab-GUID wurde nicht erhalten.");
			if (!MidpolyLegacyArchiveGuard.HasBackup(FallbackPath)) errors.Add("Legacy-Fallback fehlt.");
			if (group == null || group.GetLODs().Length != 3) errors.Add("LODGroup besitzt nicht genau drei Stufen.");
			if (lod0 != 7240 || lod1 != 3556 || lod2 != 1420)
				errors.Add("LOD-Dreiecksvertrag weicht ab: " + lod0 + "/" + lod1 + "/" + lod2 + ".");
			if (materials.Length != 3) errors.Add("Erwartet sind drei produktive Materialien, erhalten: " + materials.Length + ".");
			if (prefab != null && prefab.GetComponentsInChildren<Collider>(true).Any(collider => collider.transform != prefab.transform))
				errors.Add("Die Grafik-Hierarchie darf keine zusaetzlichen Collider tragen.");
			if (closed.size.x > 1.001f || closed.size.z > 1.001f || Mathf.Abs(closed.size.y - 0.80f) > 0.012f)
				errors.Add("Renderer-Bounds verletzen 1x1-Footprint oder 0,80-m-Hoehe: " + closed + ".");
			if (after == null || !ContractsEqual(before, after)) errors.Add("Bestehender Container-/Triggervertrag wurde veraendert.");
			if (geometry == null) errors.Add("Geometry_A14 fehlt; produktiver Builderwaechter waere wirkungslos.");
			if (!foundAnchors.SequenceEqual(SemanticAnchors.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal))
				errors.Add("Semantische Funktionsanker fehlen oder weichen ab.");
			if (lidPivots.Length != 3) errors.Add("Erwartet sind drei getrennte Deckel-Pivots, erhalten: " + lidPivots.Length + ".");

			StorageChestReport report = new StorageChestReport
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
				closedBounds = new BoundsData { center = closed.center, size = closed.size },
				openedLidBounds = new BoundsData { center = openedLid.center, size = openedLid.size },
				triggerCenter = after == null ? Vector3.zero : after.triggerCenter,
				triggerSize = after == null ? Vector3.zero : after.triggerSize,
				interactionId = after == null ? string.Empty : after.containerId,
				displayText = after == null ? string.Empty : after.displayText,
				interactionRange = after == null ? 0f : after.interactionRange,
				slots = after == null ? 0 : after.slots,
				semanticAnchors = foundAnchors,
				lidPivots = lidPivots.Select(item => item.name).OrderBy(name => name, StringComparer.Ordinal).ToArray(),
				errors = errors.ToArray()
			};
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
			AssetDatabase.Refresh();
			if (!report.pass) throw new InvalidOperationException("Lagerkisten-Migration FAIL:\n" + string.Join("\n", errors));
			Debug.Log("[MIDPOLY-000] Lagerkiste PASS: " + lod0 + "/" + lod1 + "/" + lod2
				+ " Tris, Deckelachse/3 Materialien/Interaktion/Trigger/GUID erhalten.");
		}

		private static bool ContractsEqual(GameplayContract left, GameplayContract right)
		{
			return left.containerId == right.containerId && left.displayText == right.displayText
				&& Mathf.Abs(left.interactionRange - right.interactionRange) < 0.0001f
				&& left.slots == right.slots && left.icon == right.icon && left.trigger == right.trigger
				&& Approximately(left.triggerCenter, right.triggerCenter) && Approximately(left.triggerSize, right.triggerSize);
		}

		private static Bounds ValidateOpeningAxis(GameObject prefab, List<string> errors)
		{
			if (prefab == null) return new Bounds();
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				LODGroup group = instance.GetComponent<LODGroup>();
				if (group != null) group.ForceLOD(0);
				Transform pivot = instance.GetComponentsInChildren<Transform>(true)
					.FirstOrDefault(item => item.name.StartsWith("ChestLid_LOD0", StringComparison.Ordinal));
				if (pivot == null)
				{
					errors.Add("LOD0-Deckelpivot fehlt.");
					return new Bounds();
				}
				Renderer[] lidRenderers = pivot.GetComponentsInChildren<Renderer>(true);
				if (lidRenderers.Length < 10) errors.Add("Deckel ist nicht als vollstaendige getrennte Baugruppe lesbar.");
				Quaternion closedRotation = pivot.localRotation;
				Bounds before = CombinedBounds(lidRenderers);
				pivot.localRotation = closedRotation * Quaternion.AngleAxis(-105f, Vector3.right);
				Bounds opened = CombinedBounds(lidRenderers);
				pivot.localRotation = closedRotation;
				if (opened.size.y < 0.45f || Mathf.Abs(opened.center.y - before.center.y) < 0.08f)
					errors.Add("Deckel-Scharnierachse erzeugt keine plausible geoeffnete Pose.");
				return opened;
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
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

		private static Bounds CombinedBounds(GameObject prefab, string lod)
		{
			if (prefab == null) return new Bounds();
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				Renderer[] renderers = RenderersForLod(instance.GetComponentsInChildren<Renderer>(true), lod);
				return CombinedBounds(renderers);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
		}

		private static Bounds CombinedBounds(Renderer[] renderers)
		{
			if (renderers.Length == 0) return new Bounds();
			Bounds bounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
			return bounds;
		}

		private static bool Approximately(Vector3 left, Vector3 right)
		{
			return (left - right).sqrMagnitude < 0.000001f;
		}
	}
}
