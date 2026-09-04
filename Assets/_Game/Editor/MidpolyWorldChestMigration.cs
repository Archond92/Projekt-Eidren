using Eidren.Interaction;
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
    /// Phase-4-Grafikmigration der drei Gebietskisten. Gameplaykomponenten,
    /// Prefab-GUIDs, Collider, Interaktionswerte und die vier bestehenden
    /// Sichtzustaende bleiben erhalten; ausgetauscht wird nur die Huelle.
    /// </summary>
    public static class MidpolyWorldChestMigration
    {
        private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
        private const string RuntimeFolder = "Assets/_Game/Art/MidPoly/Loot/WorldChests/Runtime";
        private const string PrefabFolder = "Assets/_Game/Prefabs/Loot/WorldChests";
        private const string FallbackFolder = PrefabFolder + "/Fallback";
        private const string ReportPath = "Documentation/Etappen/MidPoly/Phase4/WORLD_CHESTS_TECHNICAL_REPORT.json";

        private static readonly string[] Names = { "Common", "Guarded", "Hidden" };

        private static readonly Dictionary<string, Vector3> ColliderSizes = new Dictionary<string, Vector3>
        {
            { "Common", new Vector3(1.15f, 0.54f, 0.72f) },
            { "Guarded", new Vector3(1.45f, 0.70f, 0.88f) },
            { "Hidden", new Vector3(1.00f, 0.48f, 0.64f) },
        };

        private static readonly Dictionary<string, int[]> ExpectedTriangles = new Dictionary<string, int[]>
        {
            { "Common", new[] { 8478, 3726, 2560 } },
            { "Guarded", new[] { 9350, 4262, 3092 } },
            { "Hidden", new[] { 8724, 3816, 2726 } },
        };

        [Serializable]
        private sealed class MigrationRegistry
        {
            public bool worldChestKitEnabled;
        }

        private sealed class ChestContract
        {
            public string guid;
            public Vector3 boxCenter;
            public Vector3 boxSize;
            public bool boxTrigger;
            public bool boxEnabled;
            public Vector3 sphereCenter;
            public float sphereRadius;
            public bool sphereTrigger;
            public bool sphereEnabled;
            public UnityEngine.Object icon;
            public float interactionRange;
            public float holdDuration;
            public string[] rootComponents;
        }

        [Serializable]
        private sealed class TechnicalReport
        {
            public string generatedUtc;
            public bool pass;
            public ChestReport[] chests;
            public string[] errors;
        }

        [Serializable]
        private sealed class ChestReport
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
            public float triggerRadius;
            public float interactionRange;
            public float holdDuration;
            public bool fourStatesWired;
            public bool handsRemoved;
            public string[] semanticAnchors;
        }

        [MenuItem("Eidren/Mid-Poly/Phase 4/Weltkisten bauen und pruefen")]
        public static void BuildAndValidate()
        {
            if (!TryBuildApprovedVisuals())
                throw new InvalidOperationException("Phase-4-Weltkistenkit ist nicht aktiviert oder Runtime-Dateien fehlen.");
        }

        public static bool TryBuildApprovedVisuals()
        {
            if (!PilotEnabled() || !RequiredFilesExist()) return false;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Dictionary<string, ChestContract> before = Names.ToDictionary(name => name, CaptureContract);
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

        public static bool IsApprovedWorldChest(string prefabName)
        {
            string name = NormalizeName(prefabName);
            return Names.Contains(name, StringComparer.Ordinal) && PilotEnabled()
                && File.Exists(CombinedPath(name));
        }

        public static bool IsApprovedWorldChestPath(string path)
        {
            return Names.Any(name => string.Equals(path, PrefabPath(name), StringComparison.Ordinal))
                && PilotEnabled();
        }

        private static string NormalizeName(string value)
        {
            const string prefix = "WorldChest_";
            return value != null && value.StartsWith(prefix, StringComparison.Ordinal)
                ? value.Substring(prefix.Length) : value;
        }

        private static bool PilotEnabled()
        {
            if (!File.Exists(RegistryPath)) return false;
            MigrationRegistry registry = JsonUtility.FromJson<MigrationRegistry>(File.ReadAllText(RegistryPath));
            return registry != null && registry.worldChestKitEnabled;
        }

        private static bool RequiredFilesExist()
        {
            return Names.All(name => File.Exists(CombinedPath(name))
                && File.Exists(LodPath(name, 0)) && File.Exists(LodPath(name, 1))
                && File.Exists(LodPath(name, 2)) && File.Exists(PrefabPath(name)));
        }

        private static string CombinedPath(string name) => RuntimeFolder + "/PRP_WorldChest_" + name + "_Mid_Production.glb";
        private static string LodPath(string name, int lod) => RuntimeFolder + "/PRP_WorldChest_" + name + "_Mid_LOD" + lod + ".glb";
        private static string PrefabPath(string name) => PrefabFolder + "/WorldChest_" + name + ".prefab";
        private static string FallbackPath(string name) => FallbackFolder + "/WorldChest_" + name + "_Legacy.prefab";

        private static ChestContract CaptureContract(string name)
        {
            string path = PrefabPath(name);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Weltkisten-Prefab fehlt: " + name);
            BoxCollider box = prefab.GetComponent<BoxCollider>();
            SphereCollider sphere = prefab.GetComponent<SphereCollider>();
            WorldChestContainer container = prefab.GetComponent<WorldChestContainer>();
            if (box == null || sphere == null || container == null)
                throw new InvalidOperationException(name + " besitzt keinen vollstaendigen Gameplay-Vertrag.");
            SerializedObject serialized = new SerializedObject(container);
            return new ChestContract
            {
                guid = AssetDatabase.AssetPathToGUID(path),
                boxCenter = box.center,
                boxSize = box.size,
                boxTrigger = box.isTrigger,
                boxEnabled = box.enabled,
                sphereCenter = sphere.center,
                sphereRadius = sphere.radius,
                sphereTrigger = sphere.isTrigger,
                sphereEnabled = sphere.enabled,
                icon = serialized.FindProperty("interactionIcon").objectReferenceValue,
                interactionRange = serialized.FindProperty("interactionRange").floatValue,
                holdDuration = serialized.FindProperty("holdDuration").floatValue,
                rootComponents = prefab.GetComponents<Component>().Where(component => component != null && !(component is LODGroup))
                    .Select(component => component.GetType().FullName).OrderBy(value => value, StringComparer.Ordinal).ToArray()
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
                copy.name = "WorldChest_" + name + "_Legacy";
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
                {
                    if (child.name != "ContactShadow")
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
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
                Transform carvedBase = NewRoot(root.transform, "CarvedBase");
                Transform lidPivot = NewRoot(root.transform, "LidPivot");
                lidPivot.localPosition = new Vector3(0f, size.y * 0.62f, size.z * 0.42f);
                // Der bestehende Zustandsvertrag oeffnet mit -72 Grad um die
                // lokale X-Achse. Die importierte Vorderseite liegt nach der
                // Weltorientierung auf -Z; eine um Y gewendete Pivotbasis laesst
                // das alte Vorzeichen den Deckel nach oben statt in den Korpus drehen.
                lidPivot.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Transform closed = NewRoot(root.transform, "ClosedDetails");
                Transform opened = NewRoot(root.transform, "OpenedDetails");
                Transform emptied = NewRoot(root.transform, "EmptiedDetails");
                Transform lootFull = NewRoot(root.transform, "LootFill_Full");
                Transform lootPartial = NewRoot(root.transform, "LootFill_Partial");
                Transform lootSocket = NewRoot(root.transform, "LootSocket");
                lootSocket.localPosition = new Vector3(0f, size.y * 0.62f, 0f);
                Transform interactionPoint = NewRoot(root.transform, "InteractionPoint");
                interactionPoint.localPosition = new Vector3(0f, 0f, -size.z * 0.62f);

                for (int lod = 0; lod < 3; lod++)
                {
                    MoveBranch(geometry.transform, name + "_Body_LOD" + lod, carvedBase);
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
                WorldChestContainer container = root.GetComponent<WorldChestContainer>();
                if (visual == null || container == null)
                    throw new InvalidOperationException(name + ": bestehende Weltkistenkomponenten fehlen.");
                visual.Configure(lidPivot, closed.gameObject, opened.gameObject, emptied.gameObject,
                    null, null, lootFull.gameObject, lootPartial.gameObject);
                SerializedObject serializedContainer = new SerializedObject(container);
                serializedContainer.FindProperty("visual").objectReferenceValue = visual;
                serializedContainer.ApplyModifiedPropertiesWithoutUndo();
                visual.Apply(WorldChestVisualState.Closed);

                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null)
                    throw new IOException("Weltkisten-Prefab konnte nicht gespeichert werden: " + path);
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
                foreach (string path in new[] { CombinedPath(name), LodPath(name, 0), LodPath(name, 1), LodPath(name, 2), PrefabPath(name), FallbackPath(name) })
                {
                    UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset != null) AssetDatabase.SetLabels(asset,
                        new[] { "MIDPOLY-000", "MidPoly", "Phase4", "WorldChestKit", "ApprovedProduction" });
                }
            }
        }

        private static void ValidateAndWriteReport(Dictionary<string, ChestContract> before)
        {
            List<string> errors = new List<string>();
            List<ChestReport> reports = new List<ChestReport>();
            foreach (string name in Names)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
                ChestContract after = CaptureContract(name);
                LODGroup group = prefab.GetComponent<LODGroup>();
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => !IsContactShadow(renderer.transform)).ToArray();
                int[] triangles =
                {
                    TriangleCount(RenderersForLod(renderers, "LOD0")),
                    TriangleCount(RenderersForLod(renderers, "LOD1")),
                    TriangleCount(RenderersForLod(renderers, "LOD2"))
                };
                string[] materials = RenderersForLod(renderers, "LOD0").SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null).Select(material => material.name)
                    .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                bool contract = ContractsEqual(before[name], after);
                bool states = FourStatesWired(prefab);
                bool handsRemoved = prefab.GetComponentsInChildren<Transform>(true)
                    .All(child => !child.name.StartsWith("OpeningHand", StringComparison.Ordinal));
                string[] anchors = new[] { "CarvedBase", "LidPivot", "ClosedDetails", "OpenedDetails", "EmptiedDetails", "LootFill_Full", "LootFill_Partial", "LootSocket", "InteractionPoint" };
                string[] found = anchors.Where(anchor => prefab.transform.Find(anchor) != null).ToArray();

                if (before[name].guid != after.guid) errors.Add(name + ": Prefab-GUID veraendert.");
                if (!contract) errors.Add(name + ": Collider-/Trigger-/Interaktionsvertrag veraendert.");
                if (!MidpolyLegacyArchiveGuard.HasBackup(FallbackPath(name))) errors.Add(name + ": Legacy-Fallback fehlt.");
                if (group == null || group.GetLODs().Length != 3) errors.Add(name + ": LODGroup unvollstaendig.");
                if (!triangles.SequenceEqual(ExpectedTriangles[name]))
                    errors.Add(name + ": Dreiecksvertrag " + string.Join("/", triangles) + ".");
                if (materials.Length < 4 || materials.Length > 5 || materials.Any(material => !material.StartsWith("MP_WorldChest_", StringComparison.Ordinal)))
                    errors.Add(name + ": Materialvertrag verletzt: " + string.Join(", ", materials) + ".");
                if (!states) errors.Add(name + ": vier Sichtzustaende nicht vollstaendig verdrahtet.");
                if (!handsRemoved) errors.Add(name + ": entfernte Handsymbole wurden wieder eingefuehrt.");
                if (found.Length != anchors.Length) errors.Add(name + ": semantische Anker fehlen.");
                if (prefab.GetComponentsInChildren<Collider>(true).Any(collider => collider.transform != prefab.transform))
                    errors.Add(name + ": Grafik-Hierarchie traegt unerlaubte Collider.");

                reports.Add(new ChestReport
                {
                    name = name,
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
                    triggerRadius = after.sphereRadius,
                    interactionRange = after.interactionRange,
                    holdDuration = after.holdDuration,
                    fourStatesWired = states,
                    handsRemoved = handsRemoved,
                    semanticAnchors = found
                });
            }

            TechnicalReport report = new TechnicalReport
            {
                generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                pass = errors.Count == 0,
                chests = reports.ToArray(),
                errors = errors.ToArray()
            };
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();
            if (!report.pass) throw new InvalidOperationException("Weltkisten-Migration FAIL:\n" + string.Join("\n", errors));
            Debug.Log("[MIDPOLY-000] Weltkisten PASS: drei Varianten, 3 LODs, vier Zustaende, GUID/Collider/Interaktion erhalten.");
        }

        private static bool ContractsEqual(ChestContract left, ChestContract right)
        {
            return left.guid == right.guid && Approximately(left.boxCenter, right.boxCenter)
                && Approximately(left.boxSize, right.boxSize) && left.boxTrigger == right.boxTrigger
                && left.boxEnabled == right.boxEnabled && Approximately(left.sphereCenter, right.sphereCenter)
                && Mathf.Abs(left.sphereRadius - right.sphereRadius) < 0.0001f
                && left.sphereTrigger == right.sphereTrigger && left.sphereEnabled == right.sphereEnabled
                && left.icon == right.icon && Mathf.Abs(left.interactionRange - right.interactionRange) < 0.0001f
                && Mathf.Abs(left.holdDuration - right.holdDuration) < 0.0001f
                && left.rootComponents.SequenceEqual(right.rootComponents, StringComparer.Ordinal);
        }

        private static bool FourStatesWired(GameObject prefab)
        {
            WorldChestVisual visual = prefab.GetComponent<WorldChestVisual>();
            WorldChestContainer container = prefab.GetComponent<WorldChestContainer>();
            if (visual == null || container == null) return false;
            SerializedObject serializedVisual = new SerializedObject(visual);
            SerializedObject serializedContainer = new SerializedObject(container);
            string[] visualRefs = { "lid", "closedDetails", "openedDetails", "emptiedDetails", "lootFull", "lootPartial" };
            return visualRefs.All(name => serializedVisual.FindProperty(name).objectReferenceValue != null)
                && serializedVisual.FindProperty("leftHand").objectReferenceValue == null
                && serializedVisual.FindProperty("rightHand").objectReferenceValue == null
                && serializedContainer.FindProperty("visual").objectReferenceValue == visual;
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude < 0.000001f;
        }
    }
}
