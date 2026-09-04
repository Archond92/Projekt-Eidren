using Eidren.Interaction;
using Eidren.Presentation;
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
	/// Integriert Wall, Floor und Door als zusammenhaengendes Phase-4-Modulkit.
	/// Die bestehenden Prefab-GUIDs, Root-Collider, Grid-/Nav-Komponenten,
	/// Wandverbinder und die automatische Tuerlogik bleiben erhalten.
	/// </summary>
	public static class MidpolyBaseModuleMigration
	{
		private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
		private const string RuntimeFolder = "Assets/_Game/Art/MidPoly/Buildings/BaseModules/Runtime";
		private const string ConnectorPath = RuntimeFolder + "/BLD_BaseModule_Connector_Mid.glb";
		private const string PrefabFolder = "Assets/_Game/Prefabs/Buildings/Level01";
		private const string FallbackFolder = "Assets/_Game/Prefabs/Buildings/Fallback";
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase4/BASE_MODULES_TECHNICAL_REPORT.json";

		private static readonly string[] ModuleNames = { "Wall", "Floor", "Door" };

		private static readonly Dictionary<string, int[]> ExpectedTriangles = new Dictionary<string, int[]>
		{
			{ "Wall", new[] { 3492, 956, 484 } },
			{ "Floor", new[] { 2876, 1068, 308 } },
			{ "Door", new[] { 3644, 1264, 800 } }
		};

		[Serializable]
		private sealed class MigrationRegistry
		{
			public bool baseModuleKitEnabled;
		}

		private sealed class ModuleContract
		{
			public string guid;
			public Vector3 colliderCenter;
			public Vector3 colliderSize;
			public bool colliderTrigger;
			public bool colliderEnabled;
			public bool hasObstacle;
			public Vector3 obstacleCenter;
			public Vector3 obstacleSize;
			public bool obstacleCarving;
		}

		[Serializable]
		private sealed class TechnicalReport
		{
			public string generatedUtc;
			public bool pass;
			public ModuleReport[] modules;
			public string connectorAsset;
			public string[] errors;
		}

		[Serializable]
		private sealed class ModuleReport
		{
			public string name;
			public string prefabPath;
			public string fallbackPath;
			public string guidBefore;
			public string guidAfter;
			public int lod0Triangles;
			public int lod1Triangles;
			public int lod2Triangles;
			public int materials;
			public int lodLevels;
			public BoundsData rendererBounds;
			public Vector3 colliderCenter;
			public Vector3 colliderSize;
			public bool wallConnectionsPreserved;
			public bool doorBehaviourPreserved;
			public string[] semanticAnchors;
		}

		[Serializable]
		private struct BoundsData
		{
			public Vector3 center;
			public Vector3 size;
		}

		[MenuItem("Eidren/Mid-Poly/Phase 4/Basismodule bauen und pruefen")]
		public static void BuildAndValidate()
		{
			if (!TryBuildApprovedVisuals())
				throw new InvalidOperationException("Phase-4-Basismodulkit ist nicht aktiviert oder seine Runtime-Dateien fehlen.");
		}

		public static bool TryBuildApprovedVisuals()
		{
			if (!PilotEnabled() || !RequiredFilesExist()) return false;
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			Dictionary<string, ModuleContract> before = ModuleNames.ToDictionary(name => name, CaptureContract);
			foreach (string name in ModuleNames)
			{
				EnsureLegacyFallback(name);
				BuildPrefab(name);
			}

			// Die Presenter-Builder stellen ihre privaten Referenzen erneut her.
			// Durch TryApplyApprovedConnectionPieceVisual erhalten die vier
			// schaltbaren Root-Stuecke dabei das neue, abgerundete Kit-Mesh.
			WallConnectionContentBuilder.ApplyPieces();
			BuildingDoorContentBuilder.BuildDoorBehaviour();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			LabelAssets();
			ValidateAndWriteReport(before);
			return true;
		}

		public static bool IsApprovedModule(string name)
		{
			return ModuleNames.Contains(name, StringComparer.Ordinal) && PilotEnabled()
				&& File.Exists(CombinedPath(name));
		}

		public static bool IsApprovedModulePath(string path)
		{
			return ModuleNames.Any(name => string.Equals(path, PrefabPath(name), StringComparison.Ordinal))
				&& PilotEnabled();
		}

		/// <summary>
		/// Wird auch vom bestehenden WallConnectionContentBuilder verwendet,
		/// damit ein spaeterer Rebuild nicht wieder den harten Built-in-Cube setzt.
		/// </summary>
		public static bool TryApplyApprovedConnectionPieceVisual(GameObject piece)
		{
			if (piece == null || !PilotEnabled() || !File.Exists(ConnectorPath)) return false;
			GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ConnectorPath);
			if (source == null) return false;
			MeshFilter sourceFilter = source.GetComponentsInChildren<MeshFilter>(true)
				.FirstOrDefault(filter => filter.name.IndexOf("ConnectionPieceTemplate", StringComparison.Ordinal) >= 0);
			MeshRenderer sourceRenderer = sourceFilter != null ? sourceFilter.GetComponent<MeshRenderer>() : null;
			if (sourceFilter == null || sourceFilter.sharedMesh == null || sourceRenderer == null || sourceRenderer.sharedMaterial == null)
				return false;

			MeshFilter filter = piece.GetComponent<MeshFilter>();
			if (filter == null) filter = piece.AddComponent<MeshFilter>();
			filter.sharedMesh = sourceFilter.sharedMesh;
			MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
			if (renderer == null) renderer = piece.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = sourceRenderer.sharedMaterial;
			renderer.shadowCastingMode = ShadowCastingMode.On;
			renderer.receiveShadows = true;
			return true;
		}

		private static bool PilotEnabled()
		{
			if (!File.Exists(RegistryPath)) return false;
			MigrationRegistry registry = JsonUtility.FromJson<MigrationRegistry>(File.ReadAllText(RegistryPath));
			return registry != null && registry.baseModuleKitEnabled;
		}

		private static bool RequiredFilesExist()
		{
			return File.Exists(ConnectorPath) && ModuleNames.All(name => File.Exists(CombinedPath(name))
				&& File.Exists(LodPath(name, 0)) && File.Exists(LodPath(name, 1))
				&& File.Exists(LodPath(name, 2)) && File.Exists(PrefabPath(name)));
		}

		private static string CombinedPath(string name) => RuntimeFolder + "/BLD_" + name + "_Mid_Production.glb";
		private static string LodPath(string name, int lod) => RuntimeFolder + "/BLD_" + name + "_Mid_LOD" + lod + ".glb";
		private static string PrefabPath(string name) => PrefabFolder + "/BLD_" + name + "_L01.prefab";
		private static string FallbackPath(string name) => FallbackFolder + "/BLD_" + name + "_L01_Legacy.prefab";

		private static ModuleContract CaptureContract(string name)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
			if (prefab == null) throw new InvalidOperationException("Produktiver Prefab fehlt: " + name);
			BoxCollider collider = prefab.GetComponent<BoxCollider>();
			if (collider == null) throw new InvalidOperationException(name + " hat keinen Root-BoxCollider.");
			NavMeshObstacle obstacle = prefab.GetComponent<NavMeshObstacle>();
			return new ModuleContract
			{
				guid = AssetDatabase.AssetPathToGUID(PrefabPath(name)),
				colliderCenter = collider.center,
				colliderSize = collider.size,
				colliderTrigger = collider.isTrigger,
				colliderEnabled = collider.enabled,
				hasObstacle = obstacle != null,
				obstacleCenter = obstacle != null ? obstacle.center : Vector3.zero,
				obstacleSize = obstacle != null ? obstacle.size : Vector3.zero,
				obstacleCarving = obstacle != null && obstacle.carving
			};
		}

		private static void EnsureLegacyFallback(string name)
		{
			string path = FallbackPath(name);
			if (MidpolyLegacyArchiveGuard.HasBackup(path)) return;
			GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			GameObject copy = UnityEngine.Object.Instantiate(source);
			try
			{
				copy.name = "BLD_" + name + "_L01_Legacy";
				if (PrefabUtility.SaveAsPrefabAsset(copy, path) == null)
					throw new IOException("Legacy-Fallback konnte nicht gespeichert werden: " + path);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(copy);
			}
		}

		private static void BuildPrefab(string name)
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(CombinedPath(name));
			if (model == null) throw new InvalidOperationException(name + "-Produktionsmodell wurde nicht importiert.");
			string path = PrefabPath(name);
			GameObject root = PrefabUtility.LoadPrefabContents(path);
			try
			{
				Transform oldGeometry = root.transform.Find("Geometry_A14");
				if (oldGeometry != null) UnityEngine.Object.DestroyImmediate(oldGeometry.gameObject);
				LODGroup oldLod = root.GetComponent<LODGroup>();
				if (oldLod != null) UnityEngine.Object.DestroyImmediate(oldLod);

				GameObject geometry = (GameObject)PrefabUtility.InstantiatePrefab(model, root.scene);
				geometry.name = "Geometry_A14";
				geometry.transform.SetParent(root.transform, false);
				// Das GLB kommt als verschachtelte Model-Prefab-Instanz. Fuer die
				// gemeinsame Tuerangel muessen die drei Leaf-Gruppen umgehaengt
				// werden; daher die reine Grafikinstanz vor dem Autorieren loesen.
				PrefabUtility.UnpackPrefabInstance(geometry, PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
				foreach (Renderer renderer in geometry.GetComponentsInChildren<Renderer>(true))
				{
					renderer.shadowCastingMode = ShadowCastingMode.On;
					renderer.receiveShadows = true;
				}

				if (name == "Door") AuthorDoorHierarchy(geometry.transform);
				AuthorSemanticAnchors(name, geometry.transform);
				Renderer[] renderers = geometry.GetComponentsInChildren<Renderer>(true);
				Renderer[] lod0 = RenderersForLod(renderers, "LOD0");
				Renderer[] lod1 = RenderersForLod(renderers, "LOD1");
				Renderer[] lod2 = RenderersForLod(renderers, "LOD2");
				if (lod0.Length == 0 || lod1.Length == 0 || lod2.Length == 0)
					throw new InvalidOperationException(name + " besitzt nicht drei renderbare LOD-Stufen.");

				LODGroup group = root.AddComponent<LODGroup>();
				group.fadeMode = LODFadeMode.CrossFade;
				group.animateCrossFading = true;
				group.SetLODs(new[]
				{
					new LOD(name == "Floor" ? 0.62f : 0.58f, lod0) { fadeTransitionWidth = 0.10f },
					new LOD(name == "Floor" ? 0.32f : 0.28f, lod1) { fadeTransitionWidth = 0.10f },
					// F33-001: 0,08 blendete den 1 m grossen Boden bereits bei der
					// normalen 7,4er-Orthokamera vollstaendig aus. LOD2 bleibt bis
					// weit ausserhalb des spielbaren Kamerabereichs sichtbar.
					new LOD(0.01f, lod2) { fadeTransitionWidth = 0.10f }
				});
				group.RecalculateBounds();
				// OP-11: Schwellen fuer die orthografische Spielkamera (LOD0 im Spielzoom sichtbar).
				MidpolyLodThresholds.ApplyOrthographicThresholds(group);

				if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
					throw new IOException("Produktiver Prefab konnte nicht gespeichert werden: " + path);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		private static void AuthorDoorHierarchy(Transform geometry)
		{
			Transform blade = new GameObject("DoorBlade").transform;
			blade.SetParent(geometry, false);
			blade.localPosition = new Vector3(-0.5f, 0f, 0f);
			blade.localRotation = Quaternion.identity;
			foreach (Transform leaf in geometry.GetComponentsInChildren<Transform>(true)
				.Where(item => item.name.StartsWith("DoorLeaf_LOD", StringComparison.Ordinal)).ToArray())
			{
				leaf.SetParent(blade, true);
			}
		}

		private static void AuthorSemanticAnchors(string name, Transform geometry)
		{
			if (name == "Wall")
			{
				foreach (string anchor in new[] { "Plank_0", "RahmenL", "RahmenR", "WallCap", "StrebeA", "StrebeB", "Riegel", "Sockel" })
					CreateAnchor(geometry, anchor);
			}
			else if (name == "Floor")
			{
				foreach (string anchor in new[] { "Plank_0", "UnderBraceA", "UnderBraceB", "FloorNail_0_-1" })
					CreateAnchor(geometry, anchor);
			}
			else if (name == "Door")
			{
				foreach (string anchor in new[] { "RahmenLinks", "RahmenRechts", "RahmenOben", "RahmenUnten" })
					CreateAnchor(geometry, anchor);
				Transform blade = geometry.Find("DoorBlade");
				foreach (string anchor in new[] { "DoorPlank_0", "DoorPlank_1", "DoorPlank_2", "DoorPlank_3", "DoorPlank_4", "DiagonalBrace", "Griff", "LockPlate", "Scharnierband_0", "Scharnierband_1" })
					CreateAnchor(blade, anchor);
			}
		}

		private static void CreateAnchor(Transform parent, string name)
		{
			Transform anchor = new GameObject(name).transform;
			anchor.SetParent(parent, false);
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
			List<string> paths = new List<string> { ConnectorPath };
			foreach (string name in ModuleNames)
			{
				paths.Add(CombinedPath(name));
				paths.Add(LodPath(name, 0));
				paths.Add(LodPath(name, 1));
				paths.Add(LodPath(name, 2));
				paths.Add(PrefabPath(name));
				paths.Add(FallbackPath(name));
			}
			foreach (string path in paths)
			{
				UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
				if (asset != null) AssetDatabase.SetLabels(asset,
					new[] { "MIDPOLY-000", "MidPoly", "Phase4", "BaseModuleKit", "ProductionCandidate" });
			}
		}

		private static void ValidateAndWriteReport(Dictionary<string, ModuleContract> before)
		{
			List<string> errors = new List<string>();
			List<ModuleReport> reports = new List<ModuleReport>();
			foreach (string name in ModuleNames)
			{
				string path = PrefabPath(name);
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				ModuleContract after = CaptureContract(name);
				LODGroup group = prefab.GetComponent<LODGroup>();
				Renderer[] all = prefab.GetComponentsInChildren<Renderer>(true);
				Renderer[] lod0Renderers = RenderersForLod(all, "LOD0");
				Renderer[] lod1Renderers = RenderersForLod(all, "LOD1");
				Renderer[] lod2Renderers = RenderersForLod(all, "LOD2");
				int[] triangles = { TriangleCount(lod0Renderers), TriangleCount(lod1Renderers), TriangleCount(lod2Renderers) };
				string[] materials = lod0Renderers.SelectMany(renderer => renderer.sharedMaterials)
					.Where(material => material != null).Select(material => material.name)
					.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
				Bounds bounds = CombinedBounds(prefab, "LOD0");
				bool connections = name == "Floor" || HasConnections(prefab);
				bool door = name != "Door" || HasDoorBehaviour(prefab, errors);
				string[] anchors = SemanticAnchorNames(name);
				Transform geometry = prefab.transform.Find("Geometry_A14");
				string[] foundAnchors = anchors.Where(anchor => geometry != null && geometry.Find(anchor) != null)
					.OrderBy(value => value, StringComparer.Ordinal).ToArray();

				if (string.IsNullOrEmpty(before[name].guid) || before[name].guid != after.guid)
					errors.Add(name + ": Prefab-GUID wurde nicht erhalten.");
				if (!ContractsEqual(before[name], after)) errors.Add(name + ": Root-Physik-/Nav-Vertrag wurde veraendert.");
				if (!MidpolyLegacyArchiveGuard.HasBackup(FallbackPath(name))) errors.Add(name + ": Legacy-Fallback fehlt.");
				if (group == null || group.GetLODs().Length != 3) errors.Add(name + ": LODGroup besitzt nicht drei Stufen.");
				if (!triangles.SequenceEqual(ExpectedTriangles[name]))
					errors.Add(name + ": LOD-Dreiecksvertrag weicht ab: " + string.Join("/", triangles) + ".");
				int expectedMaterials = name == "Door" ? 3 : 2;
				if (materials.Length != expectedMaterials) errors.Add(name + ": Materialzahl weicht ab: " + materials.Length + ".");
				if (materials.Any(material => !material.StartsWith("MP_BaseModule_", StringComparison.Ordinal)))
					errors.Add(name + ": Fremdmaterial im LOD0.");
				if (prefab.GetComponentsInChildren<Collider>(true).Any(collider => collider.transform.IsChildOf(geometry)))
					errors.Add(name + ": Grafik-Hierarchie traegt einen Collider.");
				if (!connections) errors.Add(name + ": Wandverbinder fehlen oder sind nicht direkt renderbar.");
				if (!door) errors.Add(name + ": Tuerverhalten/Angel unvollstaendig.");
				if (!foundAnchors.SequenceEqual(anchors.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
					errors.Add(name + ": Semantische Alt-Anker fehlen.");

				Vector3 expectedSize = name == "Floor" ? new Vector3(1f, 0.15f, 1f)
					: new Vector3(1f, 2.6f, name == "Wall" ? 0.19f : 0.22f);
				if (!Approximately(bounds.size, expectedSize, 0.012f))
					errors.Add(name + ": Renderer-Bounds weichen ab: " + bounds.size + ".");

				reports.Add(new ModuleReport
				{
					name = name,
					prefabPath = path,
					fallbackPath = FallbackPath(name),
					guidBefore = before[name].guid,
					guidAfter = after.guid,
					lod0Triangles = triangles[0],
					lod1Triangles = triangles[1],
					lod2Triangles = triangles[2],
					materials = materials.Length,
					lodLevels = group == null ? 0 : group.GetLODs().Length,
					rendererBounds = new BoundsData { center = bounds.center, size = bounds.size },
					colliderCenter = after.colliderCenter,
					colliderSize = after.colliderSize,
					wallConnectionsPreserved = connections,
					doorBehaviourPreserved = door,
					semanticAnchors = foundAnchors
				});
			}

			TechnicalReport report = new TechnicalReport
			{
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				pass = errors.Count == 0,
				modules = reports.ToArray(),
				connectorAsset = ConnectorPath,
				errors = errors.ToArray()
			};
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
			AssetDatabase.Refresh();
			if (!report.pass) throw new InvalidOperationException("Basismodul-Migration FAIL:\n" + string.Join("\n", errors));
			Debug.Log("[MIDPOLY-000] Basismodulkit PASS: Wall/Floor/Door, 3 LODs, Snap-/Collider-/Verbinder-/Tuervertraege und GUIDs erhalten.");
		}

		private static string[] SemanticAnchorNames(string name)
		{
			if (name == "Wall") return new[] { "Plank_0", "RahmenL", "RahmenR", "WallCap", "StrebeA", "StrebeB", "Riegel", "Sockel" };
			if (name == "Floor") return new[] { "Plank_0", "UnderBraceA", "UnderBraceB", "FloorNail_0_-1" };
			return new[] { "RahmenLinks", "RahmenRechts", "RahmenOben", "RahmenUnten", "DoorBlade/DoorPlank_0", "DoorBlade/DiagonalBrace", "DoorBlade/LockPlate" };
		}

		private static bool HasConnections(GameObject prefab)
		{
			if (prefab.GetComponent<WallConnectionView>() == null) return false;
			foreach (string name in new[] { "Joint_MinusX", "Joint_PlusX", "Cap_MinusX", "Cap_PlusX" })
			{
				Transform piece = prefab.transform.Find(name);
				if (piece == null || piece.GetComponent<MeshFilter>() == null || piece.GetComponent<MeshRenderer>() == null
					|| piece.GetComponent<Collider>() != null) return false;
			}
			return true;
		}

		private static bool HasDoorBehaviour(GameObject prefab, List<string> errors)
		{
			BuildingDoorView view = prefab.GetComponentInChildren<BuildingDoorView>(true);
			if (view == null || view.Blade == null || view.BlockingCollider == null) return false;
			if (view.Blade.name != "DoorBlade" || !Approximately(view.Blade.localPosition, new Vector3(-0.5f, 0f, 0f))) return false;
			if (view.Blade.GetComponentsInChildren<Transform>(true).Count(item => item.name.StartsWith("DoorLeaf_LOD", StringComparison.Ordinal)) != 3)
				return false;
			SphereCollider sensor = view.GetComponent<SphereCollider>();
			if (sensor == null || !sensor.isTrigger || !Approximately(sensor.center, new Vector3(0f, 1f, 0f))
				|| Mathf.Abs(sensor.radius - 2.1f) > 0.001f) return false;
			return true;
		}

		private static bool ContractsEqual(ModuleContract left, ModuleContract right)
		{
			return left.colliderTrigger == right.colliderTrigger && left.colliderEnabled == right.colliderEnabled
				&& Approximately(left.colliderCenter, right.colliderCenter) && Approximately(left.colliderSize, right.colliderSize)
				&& left.hasObstacle == right.hasObstacle && (!left.hasObstacle ||
				(Approximately(left.obstacleCenter, right.obstacleCenter) && Approximately(left.obstacleSize, right.obstacleSize)
				&& left.obstacleCarving == right.obstacleCarving));
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
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				Renderer[] renderers = RenderersForLod(instance.GetComponentsInChildren<Renderer>(true), lod);
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

		private static bool Approximately(Vector3 left, Vector3 right, float tolerance = 0.0001f)
		{
			return Mathf.Abs(left.x - right.x) <= tolerance && Mathf.Abs(left.y - right.y) <= tolerance
				&& Mathf.Abs(left.z - right.z) <= tolerance;
		}
	}
}
