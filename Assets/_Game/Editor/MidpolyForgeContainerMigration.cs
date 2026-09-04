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
    /// Phase-4-Migration der acht visuellen Schmiedecontainer. Der eigentliche
    /// Interaktionshost wird vom EidraForgeSceneBuilder erzeugt und bleibt
    /// unangetastet. Dieses Werkzeug erhaelt Prefab-GUID, Root-BoxCollider und
    /// den bestehenden Vier-Zustaende-Vertrag des visuellen Kind-Prefabs.
    /// </summary>
    public static class MidpolyForgeContainerMigration
    {
        private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
        private const string RuntimeFolder = "Assets/_Game/Art/MidPoly/Containers/Forge/Runtime";
        private const string PrefabFolder = "Assets/_Game/Prefabs/Containers/Forge";
        private const string FallbackFolder = PrefabFolder + "/Fallback";
        private const string ReportPath = "Documentation/Etappen/MidPoly/Phase4/FORGE_CONTAINERS_TECHNICAL_REPORT.json";

        public static readonly string[] Names =
        {
            "SupplyChest", "OptionalChest", "EliteChest", "CompletionChest",
            "SmallRewardChest", "MediumRewardChest", "LargeRewardChest", "RecoveryContainer"
        };

        private static readonly Dictionary<string, Vector3> ColliderSizes = new Dictionary<string, Vector3>
        {
            { "SupplyChest", new Vector3(1.18f, 0.80f, 0.78f) },
            { "OptionalChest", new Vector3(1.25f, 0.80f, 0.82f) },
            { "EliteChest", new Vector3(1.42f, 1.00f, 0.92f) },
            { "CompletionChest", new Vector3(1.62f, 1.20f, 1.02f) },
            { "SmallRewardChest", new Vector3(0.95f, 0.70f, 0.68f) },
            { "MediumRewardChest", new Vector3(1.20f, 0.90f, 0.82f) },
            { "LargeRewardChest", new Vector3(1.55f, 1.20f, 1.02f) },
            { "RecoveryContainer", new Vector3(1.45f, 0.80f, 0.82f) },
        };

        private static readonly Dictionary<string, int[]> ExpectedTriangles = new Dictionary<string, int[]>
        {
            { "SupplyChest", new[] { 8510, 3758, 2560 } },
            { "OptionalChest", new[] { 8956, 3932, 2792 } },
            { "EliteChest", new[] { 9884, 4468, 3176 } },
            { "CompletionChest", new[] { 11024, 5148, 3544 } },
            { "SmallRewardChest", new[] { 7554, 3250, 2390 } },
            { "MediumRewardChest", new[] { 8324, 3628, 2616 } },
            { "LargeRewardChest", new[] { 10022, 4542, 3238 } },
            { "RecoveryContainer", new[] { 9264, 4176, 3000 } },
        };

        [Serializable]
        private sealed class MigrationRegistry
        {
            public bool forgeContainerKitEnabled;
        }

        private sealed class ContainerContract
        {
            public string guid;
            public Vector3 boxCenter;
            public Vector3 boxSize;
            public bool boxTrigger;
            public bool boxEnabled;
            public string[] rootComponents;
        }

        [Serializable]
        private sealed class TechnicalReport
        {
            public string generatedUtc;
            public bool pass;
            public ContainerReport[] containers;
            public string[] errors;
        }

        [Serializable]
        private sealed class ContainerReport
        {
            public string name;
            public string role;
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
            public bool fourStatesWired;
            public string[] semanticAnchors;
        }

        [MenuItem("Eidren/Mid-Poly/Phase 4/Schmiedecontainer bauen und pruefen")]
        public static void BuildAndValidate()
        {
            if (!TryBuildApprovedVisuals())
                throw new InvalidOperationException("Phase-4-Schmiedecontainerkit ist nicht aktiviert oder Runtime-Dateien fehlen.");
        }

        public static bool TryBuildApprovedVisuals()
        {
            if (!PilotEnabled() || !RequiredFilesExist()) return false;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Dictionary<string, ContainerContract> before = Names.ToDictionary(name => name, CaptureContract);
            foreach (string name in Names)
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

        public static bool IsApprovedContainer(string value)
        {
            string name = NormalizeName(value);
            return Names.Contains(name, StringComparer.Ordinal) && PilotEnabled()
                && File.Exists(CombinedPath(name));
        }

        public static string PrefabPathFor(string name) => PrefabPath(NormalizeName(name));
        public static string FallbackPathFor(string name) => FallbackPath(NormalizeName(name));

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.EndsWith("_2D", StringComparison.Ordinal) || value.EndsWith("_3D", StringComparison.Ordinal))
                return value.Substring(0, value.Length - 3);
            return value;
        }

        private static bool PilotEnabled()
        {
            if (!File.Exists(RegistryPath)) return false;
            MigrationRegistry registry = JsonUtility.FromJson<MigrationRegistry>(File.ReadAllText(RegistryPath));
            return registry != null && registry.forgeContainerKitEnabled;
        }

        private static bool RequiredFilesExist()
        {
            return Names.All(name => File.Exists(CombinedPath(name))
                && File.Exists(LodPath(name, 0)) && File.Exists(LodPath(name, 1))
                && File.Exists(LodPath(name, 2)) && File.Exists(PrefabPath(name)));
        }

        private static string CombinedPath(string name) => RuntimeFolder + "/PRP_ForgeContainer_" + name + "_Mid_Production.glb";
        private static string LodPath(string name, int lod) => RuntimeFolder + "/PRP_ForgeContainer_" + name + "_Mid_LOD" + lod + ".glb";
        private static string PrefabPath(string name) => PrefabFolder + "/" + name + "_2D.prefab";
        private static string FallbackPath(string name) => FallbackFolder + "/" + name + "_Legacy.prefab";

        private static ContainerContract CaptureContract(string name)
        {
            string path = PrefabPath(name);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Schmiedecontainer-Prefab fehlt: " + name);
            BoxCollider box = prefab.GetComponent<BoxCollider>();
            WorldChestVisual visual = prefab.GetComponent<WorldChestVisual>();
            if (box == null || visual == null)
                throw new InvalidOperationException(name + " besitzt keinen vollstaendigen visuellen Vertrag.");
            return new ContainerContract
            {
                guid = AssetDatabase.AssetPathToGUID(path),
                boxCenter = box.center,
                boxSize = box.size,
                boxTrigger = box.isTrigger,
                boxEnabled = box.enabled,
                rootComponents = prefab.GetComponents<Component>()
                    .Where(component => component != null && !(component is LODGroup))
                    .Select(component => component.GetType().FullName)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()
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
                copy.name = name + "_Legacy";
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
                foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                    if (child.name != "ContactShadow") UnityEngine.Object.DestroyImmediate(child.gameObject);
                LODGroup oldGroup = root.GetComponent<LODGroup>();
                if (oldGroup != null) UnityEngine.Object.DestroyImmediate(oldGroup);

                GameObject geometry = (GameObject)PrefabUtility.InstantiatePrefab(model, root.scene);
                geometry.name = "Geometry_MidPoly";
                geometry.transform.SetParent(root.transform, false);
                geometry.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                PrefabUtility.UnpackPrefabInstance(geometry, PrefabUnpackMode.Completely,
                    UnityEditor.InteractionMode.AutomatedAction);
                foreach (Renderer renderer in geometry.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }

                Vector3 size = ColliderSizes[name];
                Transform forgedBase = NewRoot(root.transform, "ForgedBase");
                Transform lidPivot = NewRoot(root.transform, "LidPivot");
                lidPivot.localPosition = new Vector3(0f, size.y * 0.61f, size.z * 0.45f);
                // WorldChestVisual oeffnet mit -72 Grad. Die Y-Wendung der
                // Pivotbasis richtet das alte Vorzeichen zur offenen Rueckseite.
                lidPivot.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Transform closed = NewRoot(root.transform, "ClosedDetails");
                Transform opened = NewRoot(root.transform, "OpenedDetails");
                Transform emptied = NewRoot(root.transform, "EmptiedDetails");
                Transform lootFull = NewRoot(root.transform, "LootFill_Full");
                Transform lootPartial = NewRoot(root.transform, "LootFill_Partial");
                Transform lootSocket = NewRoot(root.transform, "LootSocket");
                lootSocket.localPosition = new Vector3(0f, size.y * 0.63f, 0f);
                Transform interactionPoint = NewRoot(root.transform, "InteractionPoint");
                interactionPoint.localPosition = new Vector3(0f, size.y * 0.45f, -size.z * 0.62f);

                for (int lod = 0; lod < 3; lod++)
                {
                    MoveBranch(geometry.transform, name + "_Body_LOD" + lod, forgedBase);
                    MoveBranch(geometry.transform, name + "_Lid_LOD" + lod, lidPivot);
                    MoveBranch(geometry.transform, name + "_Closed_LOD" + lod, closed);
                    MoveBranch(geometry.transform, name + "_Opened_LOD" + lod, opened);
                    MoveBranch(geometry.transform, name + "_Emptied_LOD" + lod, emptied);
                    MoveBranch(geometry.transform, name + "_LootFull_LOD" + lod, lootFull);
                    MoveBranch(geometry.transform, name + "_LootPartial_LOD" + lod, lootPartial);
                }

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => !IsContactShadow(renderer.transform)).ToArray();
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
                    new LOD(0.58f, lod0) { fadeTransitionWidth = 0.10f },
                    new LOD(0.28f, lod1) { fadeTransitionWidth = 0.10f },
                    new LOD(0.01f, lod2) { fadeTransitionWidth = 0.08f }, // F34-003: wie F33-001, letzte Stufe bleibt bei der 7,4er-Spielkamera sichtbar
                });
                group.RecalculateBounds();
                // OP-11: Schwellen fuer die orthografische Spielkamera (LOD0 im Spielzoom sichtbar).
                MidpolyLodThresholds.ApplyOrthographicThresholds(group);

                WorldChestVisual visual = root.GetComponent<WorldChestVisual>();
                if (visual == null) throw new InvalidOperationException(name + ": WorldChestVisual fehlt.");
                visual.Configure(lidPivot, closed.gameObject, opened.gameObject, emptied.gameObject,
                    null, null, lootFull.gameObject, lootPartial.gameObject);
                visual.Apply(WorldChestVisualState.Closed);

                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                    throw new IOException("Schmiedecontainer-Prefab konnte nicht gespeichert werden: " + path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform NewRoot(Transform parent, string name)
        {
            Transform result = new GameObject(name).transform;
            result.SetParent(parent, false);
            return result;
        }

        private static void MoveBranch(Transform geometry, string exactName, Transform destination)
        {
            Transform found = geometry.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => string.Equals(item.name, exactName, StringComparison.Ordinal));
            if (found == null) throw new InvalidOperationException("Importzweig fehlt: " + exactName);
            found.SetParent(destination, true);
        }

        private static bool IsContactShadow(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
                if (current.name == "ContactShadow") return true;
            return false;
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
                .Where(filter => filter != null && filter.sharedMesh != null).Distinct()
                .Sum(filter => filter.sharedMesh.triangles.Length / 3);
        }

        private static void LabelAssets()
        {
            foreach (string name in Names)
            {
                foreach (string path in new[]
                {
                    CombinedPath(name), LodPath(name, 0), LodPath(name, 1), LodPath(name, 2),
                    PrefabPath(name), FallbackPath(name)
                })
                {
                    UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset != null) AssetDatabase.SetLabels(asset,
                        new[] { "MIDPOLY-000", "MidPoly", "Phase4", "ForgeContainerKit", "ApprovedProduction" });
                }
            }
        }

        private static void ValidateAndWriteReport(Dictionary<string, ContainerContract> before)
        {
            List<string> errors = new List<string>();
            List<ContainerReport> reports = new List<ContainerReport>();
            foreach (string name in Names)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
                ContainerContract after = CaptureContract(name);
                LODGroup group = prefab.GetComponent<LODGroup>();
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => !IsContactShadow(renderer.transform)).ToArray();
                int[] triangles =
                {
                    TriangleCount(RenderersForLod(renderers, "LOD0")),
                    TriangleCount(RenderersForLod(renderers, "LOD1")),
                    TriangleCount(RenderersForLod(renderers, "LOD2"))
                };
                string[] materials = RenderersForLod(renderers, "LOD0")
                    .SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null)
                    .Select(material => material.name).Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                bool states = FourStatesWired(prefab);
                string[] anchors =
                {
                    "ForgedBase", "LidPivot", "ClosedDetails", "OpenedDetails", "EmptiedDetails",
                    "LootFill_Full", "LootFill_Partial", "LootSocket", "InteractionPoint"
                };
                string[] found = anchors.Where(anchor => prefab.transform.Find(anchor) != null).ToArray();

                if (!ContractsEqual(before[name], after)) errors.Add(name + ": GUID-/Collider-/Komponentenvertrag veraendert.");
                if (!MidpolyLegacyArchiveGuard.HasBackup(FallbackPath(name))) errors.Add(name + ": Legacy-Fallback fehlt.");
                if (group == null || group.GetLODs().Length != 3) errors.Add(name + ": LODGroup unvollstaendig.");
                if (!triangles.SequenceEqual(ExpectedTriangles[name]))
                    errors.Add(name + ": Dreieckszahlen weichen ab (" + string.Join(",", triangles) + ").");
                if (!(triangles[0] > triangles[1] && triangles[1] > triangles[2]))
                    errors.Add(name + ": LODs werden nicht progressiv leichter.");
                if (materials.Length != 5 || materials.Any(material => !material.StartsWith("MP_ForgeContainer_", StringComparison.Ordinal)))
                    errors.Add(name + ": Materialvertrag verletzt (" + string.Join(",", materials) + ").");
                if (!states) errors.Add(name + ": Vier-Zustaende-Vertrag unvollstaendig.");
                if (found.Length != anchors.Length) errors.Add(name + ": Semantische Anker fehlen.");
                if (prefab.GetComponentsInChildren<Collider>(true).Count(collider => collider.transform != prefab.transform) != 0)
                    errors.Add(name + ": unerlaubter Child-Collider.");

                reports.Add(new ContainerReport
                {
                    name = name,
                    role = RoleFor(name),
                    prefabPath = PrefabPath(name),
                    fallbackPath = FallbackPath(name),
                    guidBefore = before[name].guid,
                    guidAfter = after.guid,
                    lod0Triangles = triangles[0],
                    lod1Triangles = triangles[1],
                    lod2Triangles = triangles[2],
                    lodLevels = group == null ? 0 : group.GetLODs().Length,
                    materials = materials.Length,
                    colliderCenter = after.boxCenter,
                    colliderSize = after.boxSize,
                    fourStatesWired = states,
                    semanticAnchors = found
                });
            }

            TechnicalReport report = new TechnicalReport
            {
                generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                pass = errors.Count == 0,
                containers = reports.ToArray(),
                errors = errors.ToArray()
            };
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();
            if (!report.pass)
                throw new InvalidOperationException("Schmiedecontainer-Migration FAIL:\n" + string.Join("\n", errors));
            Debug.Log("[MIDPOLY-000] Schmiedecontainer PASS: acht Rollen, 3 LODs, vier Zustaende, GUID/Collider erhalten.");
        }

        private static bool ContractsEqual(ContainerContract left, ContainerContract right)
        {
            return left.guid == right.guid && Approximately(left.boxCenter, right.boxCenter)
                && Approximately(left.boxSize, right.boxSize) && left.boxTrigger == right.boxTrigger
                && left.boxEnabled == right.boxEnabled
                && left.rootComponents.SequenceEqual(right.rootComponents, StringComparer.Ordinal);
        }

        private static bool FourStatesWired(GameObject prefab)
        {
            WorldChestVisual visual = prefab.GetComponent<WorldChestVisual>();
            if (visual == null) return false;
            SerializedObject serialized = new SerializedObject(visual);
            string[] refs = { "lid", "closedDetails", "openedDetails", "emptiedDetails", "lootFull", "lootPartial" };
            return refs.All(property => serialized.FindProperty(property).objectReferenceValue != null)
                && serialized.FindProperty("leftHand").objectReferenceValue == null
                && serialized.FindProperty("rightHand").objectReferenceValue == null;
        }

        private static string RoleFor(string name)
        {
            switch (name)
            {
                case "SupplyChest": return "supply";
                case "OptionalChest": return "optional";
                case "EliteChest": return "elite-boss-locked";
                case "CompletionChest": return "completion-boss-locked";
                case "SmallRewardChest": return "reward-tier-1";
                case "MediumRewardChest": return "reward-tier-2";
                case "LargeRewardChest": return "reward-tier-3";
                default: return "recovery";
            }
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude < 0.000001f;
        }
    }
}
