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
    /// Phase-4-Migration der sechs Produktionsstationen. Ersetzt ausschliesslich
    /// die Grafik, erhaelt Prefab-GUID, Root-Collider, NavMeshObstacle und
    /// Interaktionscontroller. Beim FarmPlot werden die drei Controller-Zustaende
    /// explizit neu verdrahtet.
    /// </summary>
    public static class MidpolyProductionStationMigration
    {
        private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
        private const string RuntimeFolder = "Assets/_Game/Art/MidPoly/Buildings/ProductionStations/Runtime";
        private const string PrefabFolder = "Assets/_Game/Prefabs/Buildings/Level01";
        private const string FallbackFolder = "Assets/_Game/Prefabs/Buildings/Fallback";
        private const string ReportPath = "Documentation/Etappen/MidPoly/Phase4/PRODUCTION_STATIONS_TECHNICAL_REPORT.json";

        private static readonly string[] StationNames =
        {
            "Smelter", "Sawmill", "Stonecutter", "Ropewalk", "CookingPot", "FarmPlot"
        };

        private static readonly Dictionary<string, int[]> ExpectedTriangles = new Dictionary<string, int[]>
        {
            { "Smelter", new[] { 4704, 1984, 1492 } },
            { "Sawmill", new[] { 3380, 1544, 1438 } },
            { "Stonecutter", new[] { 2912, 1152, 996 } },
            { "Ropewalk", new[] { 5724, 2328, 2192 } },
            { "CookingPot", new[] { 5436, 2396, 1452 } },
            { "FarmPlot", new[] { 1448, 752, 460 } },
        };

        [Serializable]
        private sealed class MigrationRegistry
        {
            public bool productionStationKitEnabled;
        }

        private sealed class StationContract
        {
            public string guid;
            public Vector3 colliderCenter;
            public Vector3 colliderSize;
            public bool colliderTrigger;
            public bool colliderEnabled;
            public Vector3 obstacleCenter;
            public Vector3 obstacleSize;
            public bool obstacleCarving;
            public string controllerJson;
        }

        [Serializable]
        private sealed class TechnicalReport
        {
            public string generatedUtc;
            public bool pass;
            public StationReport[] stations;
            public string[] errors;
        }

        [Serializable]
        private sealed class StationReport
        {
            public string name;
            public string prefabPath;
            public string fallbackPath;
            public string guidBefore;
            public string guidAfter;
            public int lod0Triangles;
            public int lod1Triangles;
            public int lod2Triangles;
            public int lodLevels;
            public int materials;
            public Vector3 colliderCenter;
            public Vector3 colliderSize;
            public bool interactionPreserved;
            public bool farmStatesWired;
            public string[] semanticAnchors;
        }

        [MenuItem("Eidren/Mid-Poly/Phase 4/Produktionsstationen bauen und pruefen")]
        public static void BuildAndValidate()
        {
            if (!TryBuildApprovedVisuals())
                throw new InvalidOperationException("Phase-4-Produktionsstationskit ist nicht aktiviert oder Runtime-Dateien fehlen.");
        }

        public static bool TryBuildApprovedVisuals()
        {
            if (!PilotEnabled() || !RequiredFilesExist()) return false;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Dictionary<string, StationContract> before = StationNames.ToDictionary(name => name, CaptureContract);
            foreach (string name in StationNames)
            {
                EnsureLegacyFallback(name);
                BuildPrefab(name);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            LabelAssets();
            ValidateAndWriteReport(before);
            return true;
        }

        public static bool IsApprovedStation(string name)
        {
            return StationNames.Contains(name, StringComparer.Ordinal) && PilotEnabled()
                && File.Exists(CombinedPath(name));
        }

        public static bool IsApprovedStationPath(string path)
        {
            return StationNames.Any(name => string.Equals(path, PrefabPath(name), StringComparison.Ordinal))
                && PilotEnabled();
        }

        private static bool PilotEnabled()
        {
            if (!File.Exists(RegistryPath)) return false;
            MigrationRegistry registry = JsonUtility.FromJson<MigrationRegistry>(File.ReadAllText(RegistryPath));
            return registry != null && registry.productionStationKitEnabled;
        }

        private static bool RequiredFilesExist()
        {
            return StationNames.All(name => File.Exists(CombinedPath(name))
                && File.Exists(LodPath(name, 0)) && File.Exists(LodPath(name, 1))
                && File.Exists(LodPath(name, 2)) && File.Exists(PrefabPath(name)))
                && File.Exists(FarmStatePath("Planted")) && File.Exists(FarmStatePath("Ready"));
        }

        private static string CombinedPath(string name) => RuntimeFolder + "/BLD_" + name + "_Mid_Production.glb";
        private static string LodPath(string name, int lod) => RuntimeFolder + "/BLD_" + name + "_Mid_LOD" + lod + ".glb";
        private static string FarmStatePath(string state) => RuntimeFolder + "/BLD_FarmPlot_State_" + state + "_Mid.glb";
        private static string PrefabPath(string name) => PrefabFolder + "/BLD_" + name + "_L01.prefab";
        private static string FallbackPath(string name) => FallbackFolder + "/BLD_" + name + "_L01_Legacy.prefab";

        private static StationContract CaptureContract(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
            if (prefab == null) throw new InvalidOperationException("Produktions-Prefab fehlt: " + name);
            BoxCollider collider = prefab.GetComponent<BoxCollider>();
            NavMeshObstacle obstacle = prefab.GetComponent<NavMeshObstacle>();
            Component controller = name == "FarmPlot"
                ? (Component)prefab.GetComponent<FarmPlotController>()
                : prefab.GetComponent<WorkbenchController>();
            if (collider == null || obstacle == null || controller == null)
                throw new InvalidOperationException(name + " besitzt keinen vollstaendigen Gameplay-Vertrag.");
            return new StationContract
            {
                guid = AssetDatabase.AssetPathToGUID(PrefabPath(name)),
                colliderCenter = collider.center,
                colliderSize = collider.size,
                colliderTrigger = collider.isTrigger,
                colliderEnabled = collider.enabled,
                obstacleCenter = obstacle.center,
                obstacleSize = obstacle.size,
                obstacleCarving = obstacle.carving,
                controllerJson = EditorJsonUtility.ToJson(controller)
            };
        }

        private static void EnsureLegacyFallback(string name)
        {
            string path = FallbackPath(name);
            if (MidpolyLegacyArchiveGuard.HasBackup(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
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
            if (model == null) throw new InvalidOperationException(name + "-Modell wurde nicht importiert.");
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
                PrefabUtility.UnpackPrefabInstance(geometry, PrefabUnpackMode.Completely,
                    UnityEditor.InteractionMode.AutomatedAction);
                foreach (Renderer renderer in geometry.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }

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
                    new LOD(name == "FarmPlot" ? 0.52f : 0.58f, lod0) { fadeTransitionWidth = 0.10f },
                    new LOD(name == "FarmPlot" ? 0.24f : 0.28f, lod1) { fadeTransitionWidth = 0.10f },
                    // F33-001: Kleine Stationen (vor allem der Kochtopf) lagen
                    // bei der normalen 7,4er-Orthokamera schon unter 0,07 und
                    // verschwanden deshalb direkt nach dem Platzieren.
                    new LOD(0.01f, lod2) { fadeTransitionWidth = 0.08f },
                });
                group.RecalculateBounds();
                // OP-11: Schwellen fuer die orthografische Spielkamera (LOD0 im Spielzoom sichtbar).
                MidpolyLodThresholds.ApplyOrthographicThresholds(group);

                if (name == "FarmPlot") AuthorFarmStates(root);
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                    throw new IOException("Produktions-Prefab konnte nicht gespeichert werden: " + path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AuthorFarmStates(GameObject root)
        {
            FarmPlotController controller = root.GetComponent<FarmPlotController>();
            if (controller == null) throw new InvalidOperationException("FarmPlotController fehlt.");
            Transform oldStates = root.transform.Find("Visual_A20");
            if (oldStates != null) UnityEngine.Object.DestroyImmediate(oldStates.gameObject);

            GameObject stateRoot = new GameObject("Visual_A20");
            stateRoot.transform.SetParent(root.transform, false);
            GameObject empty = NewState(stateRoot.transform, "VIS_Empty", null, root);
            GameObject planted = NewState(stateRoot.transform, "VIS_Planted", FarmStatePath("Planted"), root);
            GameObject ready = NewState(stateRoot.transform, "VIS_Ready", FarmStatePath("Ready"), root);
            planted.SetActive(false);
            ready.SetActive(false);
            controller.ConfigureVisuals(empty, planted, ready);
        }

        private static GameObject NewState(Transform parent, string name, string modelPath, GameObject prefabRoot)
        {
            GameObject state = new GameObject(name);
            state.transform.SetParent(parent, false);
            if (!string.IsNullOrEmpty(modelPath))
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null) throw new InvalidOperationException("Farmzustandsmodell fehlt: " + modelPath);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, prefabRoot.scene);
                instance.name = "StateGeometry";
                instance.transform.SetParent(state.transform, false);
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }
            }
            return state;
        }

        private static void AuthorSemanticAnchors(string name, Transform geometry)
        {
            string[] names;
            if (name == "Smelter") names = new[] { "FacetedKilnBody", "FireMouthDark", "LeatherBellows", "BellowsNozzle", "Crucible", "CopperIngot", "WorkSide", "InteractionPoint" };
            else if (name == "Sawmill") names = new[] { "Blade", "TimberLog", "SlidingGuide", "CrankHandle", "OverheadBeam", "WorkSide", "InteractionPoint" };
            else if (name == "Stonecutter") names = new[] { "HeavyCuttingBed", "UncutStoneSlab", "GrindingWheel", "HandCrank", "CutScoreA", "WorkSide", "InteractionPoint" };
            else if (name == "Ropewalk") names = new[] { "TwistedStrand_0", "TwistedStrand_1", "TwistedStrand_2", "DriveWheel", "FinishedRopeCoil", "WorkSide", "InteractionPoint" };
            else if (name == "CookingPot") names = new[] { "CauldronBody", "CauldronRim", "Inhalt", "Bein_0", "Griff_0", "Aufhaengeoese", "WorkSide", "InteractionPoint" };
            else names = new[] { "RaisedFurrow_0", "SeedMarker_0", "HandRakeHead", "WorkSide", "InteractionPoint" };
            foreach (string anchorName in names)
            {
                Transform anchor = new GameObject(anchorName).transform;
                anchor.SetParent(geometry, false);
                if (anchorName == "InteractionPoint" || anchorName == "WorkSide")
                    anchor.localPosition = new Vector3(0f, name == "FarmPlot" ? 0.15f : 0.2f, -0.62f);
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

        private static int TriangleCount(IEnumerable<Renderer> renderers)
        {
            return renderers.Select(renderer => renderer.GetComponent<MeshFilter>())
                .Where(filter => filter != null && filter.sharedMesh != null)
                .Distinct().Sum(filter => filter.sharedMesh.triangles.Length / 3);
        }

        private static void LabelAssets()
        {
            List<string> paths = new List<string> { FarmStatePath("Planted"), FarmStatePath("Ready") };
            foreach (string name in StationNames)
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
                    new[] { "MIDPOLY-000", "MidPoly", "Phase4", "ProductionStationKit", "ApprovedProduction" });
            }
        }

        private static void ValidateAndWriteReport(Dictionary<string, StationContract> before)
        {
            List<string> errors = new List<string>();
            List<StationReport> stationReports = new List<StationReport>();
            foreach (string name in StationNames)
            {
                string path = PrefabPath(name);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                StationContract after = CaptureContract(name);
                LODGroup group = prefab.GetComponent<LODGroup>();
                Renderer[] all = prefab.transform.Find("Geometry_A14").GetComponentsInChildren<Renderer>(true);
                int[] triangles =
                {
                    TriangleCount(RenderersForLod(all, "LOD0")),
                    TriangleCount(RenderersForLod(all, "LOD1")),
                    TriangleCount(RenderersForLod(all, "LOD2"))
                };
                string[] materials = RenderersForLod(all, "LOD0").SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null).Select(material => material.name)
                    .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                bool physicalContract = before[name].colliderCenter == after.colliderCenter
                    && before[name].colliderSize == after.colliderSize
                    && before[name].colliderTrigger == after.colliderTrigger
                    && before[name].colliderEnabled == after.colliderEnabled
                    && before[name].obstacleCenter == after.obstacleCenter
                    && before[name].obstacleSize == after.obstacleSize
                    && before[name].obstacleCarving == after.obstacleCarving;
                bool controllerPreserved = name == "FarmPlot" || before[name].controllerJson == after.controllerJson;
                bool farmStates = name != "FarmPlot" || FarmStatesWired(prefab);
                string[] expectedAnchors = SemanticAnchorNames(name);
                Transform geometry = prefab.transform.Find("Geometry_A14");
                string[] foundAnchors = expectedAnchors.Where(anchor => geometry.Find(anchor) != null).ToArray();

                if (before[name].guid != after.guid) errors.Add(name + ": Prefab-GUID veraendert.");
                if (!physicalContract) errors.Add(name + ": Collider-/NavMesh-Vertrag veraendert.");
                if (!controllerPreserved) errors.Add(name + ": Interaktionscontroller veraendert.");
                if (!farmStates) errors.Add(name + ": Farmzustaende nicht korrekt verdrahtet.");
                if (!MidpolyLegacyArchiveGuard.HasBackup(FallbackPath(name))) errors.Add(name + ": Legacy-Fallback fehlt.");
                if (group == null || group.GetLODs().Length != 3) errors.Add(name + ": LODGroup unvollstaendig.");
                if (!triangles.SequenceEqual(ExpectedTriangles[name]))
                    errors.Add(name + ": Dreiecksvertrag " + string.Join("/", triangles) + ".");
                if (materials.Length < 2 || materials.Length > 6 || materials.Any(material => !material.StartsWith("MP_Production_", StringComparison.Ordinal)))
                    errors.Add(name + ": Materialvertrag verletzt: " + string.Join(", ", materials) + ".");
                if (foundAnchors.Length != expectedAnchors.Length) errors.Add(name + ": Semantische Anker fehlen.");
                if (prefab.transform.Find("Geometry_A14").GetComponentsInChildren<Collider>(true).Length != 0)
                    errors.Add(name + ": Grafik traegt Collider.");

                stationReports.Add(new StationReport
                {
                    name = name,
                    prefabPath = path,
                    fallbackPath = FallbackPath(name),
                    guidBefore = before[name].guid,
                    guidAfter = after.guid,
                    lod0Triangles = triangles[0],
                    lod1Triangles = triangles[1],
                    lod2Triangles = triangles[2],
                    lodLevels = group == null ? 0 : group.GetLODs().Length,
                    materials = materials.Length,
                    colliderCenter = after.colliderCenter,
                    colliderSize = after.colliderSize,
                    interactionPreserved = controllerPreserved,
                    farmStatesWired = farmStates,
                    semanticAnchors = foundAnchors
                });
            }

            TechnicalReport report = new TechnicalReport
            {
                generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                pass = errors.Count == 0,
                stations = stationReports.ToArray(),
                errors = errors.ToArray()
            };
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();
            if (!report.pass) throw new InvalidOperationException("Produktionsstations-Migration FAIL:\n" + string.Join("\n", errors));
            Debug.Log("[MIDPOLY-000] Produktionsstationen PASS: sechs Stationen, drei LODs, GUID-/Collider-/Nav-/Interaktions- und Farmzustandsvertraege erhalten.");
        }

        private static bool FarmStatesWired(GameObject prefab)
        {
            FarmPlotController controller = prefab.GetComponent<FarmPlotController>();
            Transform root = prefab.transform.Find("Visual_A20");
            if (controller == null || root == null) return false;
            SerializedObject serialized = new SerializedObject(controller);
            GameObject empty = serialized.FindProperty("emptyVisual").objectReferenceValue as GameObject;
            GameObject planted = serialized.FindProperty("plantedVisual").objectReferenceValue as GameObject;
            GameObject ready = serialized.FindProperty("readyVisual").objectReferenceValue as GameObject;
            return empty != null && planted != null && ready != null
                && empty.name == "VIS_Empty" && planted.name == "VIS_Planted" && ready.name == "VIS_Ready"
                && planted.GetComponentsInChildren<MeshRenderer>(true).Length > 0
                && ready.GetComponentsInChildren<MeshRenderer>(true).Length > 0;
        }

        private static string[] SemanticAnchorNames(string name)
        {
            if (name == "Smelter") return new[] { "FacetedKilnBody", "FireMouthDark", "LeatherBellows", "BellowsNozzle", "Crucible", "CopperIngot", "WorkSide", "InteractionPoint" };
            if (name == "Sawmill") return new[] { "Blade", "TimberLog", "SlidingGuide", "CrankHandle", "OverheadBeam", "WorkSide", "InteractionPoint" };
            if (name == "Stonecutter") return new[] { "HeavyCuttingBed", "UncutStoneSlab", "GrindingWheel", "HandCrank", "CutScoreA", "WorkSide", "InteractionPoint" };
            if (name == "Ropewalk") return new[] { "TwistedStrand_0", "TwistedStrand_1", "TwistedStrand_2", "DriveWheel", "FinishedRopeCoil", "WorkSide", "InteractionPoint" };
            if (name == "CookingPot") return new[] { "CauldronBody", "CauldronRim", "Inhalt", "Bein_0", "Griff_0", "Aufhaengeoese", "WorkSide", "InteractionPoint" };
            return new[] { "RaisedFurrow_0", "SeedMarker_0", "HandRakeHead", "WorkSide", "InteractionPoint" };
        }
    }
}
