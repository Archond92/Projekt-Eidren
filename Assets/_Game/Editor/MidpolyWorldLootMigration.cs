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
    /// Phase-4-Migration fuer genau einen gemeinsamen Lootbeutel und genau
    /// einen groesseren Todesbeutel. Gameplay, Prefab-GUIDs, Trigger,
    /// Item-Icon, Mengenlabel und der Storage-Vertrag bleiben erhalten.
    /// </summary>
    public static class MidpolyWorldLootMigration
    {
        private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
        private const string RuntimeFolder = "Assets/_Game/Art/MidPoly/Loot/WorldItems/Runtime";
        private const string WorldItemPath = "Assets/_Game/Prefabs/Items/WorldItem.prefab";
        private const string DeathBagPath = "Assets/_Game/Resources/Prefabs/DeathBag.prefab";
        private const string WorldFallbackPath = "Assets/_Game/Prefabs/Items/Fallback/WorldItem_Legacy.prefab";
        private const string DeathFallbackPath = "Assets/_Game/Resources/Prefabs/Fallback/DeathBag_Legacy.prefab";
        private const string ReportPath = "Documentation/Etappen/MidPoly/Phase4/WORLD_LOOT_TECHNICAL_REPORT.json";

        private static readonly int[] WorldTriangles = { 1144, 468, 444 };
        private static readonly int[] DeathTriangles = { 1448, 420, 392 };

        [Serializable]
        private sealed class MigrationRegistry
        {
            public bool worldLootKitEnabled;
        }

        private sealed class PrefabContract
        {
            public string guid;
            public Vector3 colliderCenter;
            public Vector3 colliderSize;
            public float colliderRadius;
            public bool colliderTrigger;
            public bool colliderEnabled;
            public string[] rootComponents;
            public string[] serializedValues;
        }

        [Serializable]
        private sealed class TechnicalReport
        {
            public string generatedUtc;
            public bool pass;
            public AssetReport worldItem;
            public AssetReport deathBag;
            public string[] errors;
        }

        [Serializable]
        private sealed class AssetReport
        {
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
            public float visualHeight;
            public Vector3 colliderCenter;
            public Vector3 colliderSize;
            public float colliderRadius;
            public string[] anchors;
        }

        [MenuItem("Eidren/Mid-Poly/Phase 4/Weltbeutel bauen und pruefen")]
        public static void BuildAndValidate()
        {
            if (!TryBuildApprovedVisuals())
                throw new InvalidOperationException("Phase-4-Weltbeutelkit ist nicht aktiviert oder Runtime-Dateien fehlen.");
        }

        public static bool TryBuildApprovedVisuals()
        {
            if (!PilotEnabled() || !RequiredFilesExist()) return false;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            PrefabContract worldBefore = CaptureWorldContract();
            PrefabContract deathBefore = CaptureDeathContract();
            EnsureLegacyFallback(WorldItemPath, WorldFallbackPath, "WorldItem_Legacy");
            EnsureLegacyFallback(DeathBagPath, DeathFallbackPath, "DeathBag_Legacy");
            BuildWorldItem();
            BuildDeathBag();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            LabelAssets();
            ValidateAndWriteReport(worldBefore, deathBefore);
            return true;
        }

        public static bool IsApprovedWorldItem()
        {
            return PilotEnabled() && File.Exists(CombinedPath("WorldItem")) && File.Exists(WorldItemPath);
        }

        public static bool IsApprovedDeathBag()
        {
            return PilotEnabled() && File.Exists(CombinedPath("DeathBag")) && File.Exists(DeathBagPath);
        }

        public static string WorldItemPrefabPath => WorldItemPath;
        public static string DeathBagPrefabPath => DeathBagPath;
        public static string WorldItemFallbackPath => WorldFallbackPath;
        public static string DeathBagFallbackPath => DeathFallbackPath;

        private static bool PilotEnabled()
        {
            if (!File.Exists(RegistryPath)) return false;
            MigrationRegistry registry = JsonUtility.FromJson<MigrationRegistry>(File.ReadAllText(RegistryPath));
            return registry != null && registry.worldLootKitEnabled;
        }

        private static bool RequiredFilesExist()
        {
            return new[]
            {
                CombinedPath("WorldItem"), LodPath("WorldItem", 0), LodPath("WorldItem", 1), LodPath("WorldItem", 2),
                CombinedPath("DeathBag"), LodPath("DeathBag", 0), LodPath("DeathBag", 1), LodPath("DeathBag", 2),
                WorldItemPath, DeathBagPath
            }.All(File.Exists);
        }

        private static string CombinedPath(string name) => RuntimeFolder + "/PRP_" + name + "_Mid_Production.glb";
        private static string LodPath(string name, int lod) => RuntimeFolder + "/PRP_" + name + "_Mid_LOD" + lod + ".glb";

        private static PrefabContract CaptureWorldContract()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldItemPath);
            if (prefab == null) throw new FileNotFoundException(WorldItemPath);
            SphereCollider sphere = prefab.GetComponent<SphereCollider>();
            WorldItemController controller = prefab.GetComponent<WorldItemController>();
            AudioSource audio = prefab.GetComponent<AudioSource>();
            if (sphere == null || controller == null || audio == null)
                throw new InvalidOperationException("WorldItem besitzt keinen vollstaendigen Root-Vertrag.");
            SerializedObject serialized = new SerializedObject(controller);
            return new PrefabContract
            {
                guid = AssetDatabase.AssetPathToGUID(WorldItemPath),
                colliderCenter = sphere.center,
                colliderRadius = sphere.radius,
                colliderTrigger = sphere.isTrigger,
                colliderEnabled = sphere.enabled,
                rootComponents = RootComponents(prefab),
                serializedValues = new[]
                {
                    "itemIcon=" + ReferencePath(serialized.FindProperty("itemIcon").objectReferenceValue, prefab.transform),
                    "quantityLabel=" + ReferencePath(serialized.FindProperty("quantityLabel").objectReferenceValue, prefab.transform),
                    "highlight=" + ReferencePath(serialized.FindProperty("highlight").objectReferenceValue, prefab.transform),
                    "interactionTrigger=" + ReferencePath(serialized.FindProperty("interactionTrigger").objectReferenceValue, prefab.transform),
                    "interactionRange=" + serialized.FindProperty("interactionRange").floatValue.ToString("R"),
                    "priority=" + serialized.FindProperty("priority").intValue,
                    "audioPlayOnAwake=" + audio.playOnAwake
                }
            };
        }

        private static PrefabContract CaptureDeathContract()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeathBagPath);
            if (prefab == null) throw new FileNotFoundException(DeathBagPath);
            BoxCollider box = prefab.GetComponent<BoxCollider>();
            StorageContainer storage = prefab.GetComponent<StorageContainer>();
            if (box == null || storage == null)
                throw new InvalidOperationException("DeathBag besitzt keinen vollstaendigen Root-Vertrag.");
            SerializedObject serialized = new SerializedObject(storage);
            return new PrefabContract
            {
                guid = AssetDatabase.AssetPathToGUID(DeathBagPath),
                colliderCenter = box.center,
                colliderSize = box.size,
                colliderTrigger = box.isTrigger,
                colliderEnabled = box.enabled,
                rootComponents = RootComponents(prefab),
                serializedValues = new[]
                {
                    "containerId=" + serialized.FindProperty("containerId").stringValue,
                    "displayText=" + serialized.FindProperty("displayText").stringValue,
                    "icon=" + AssetReference(serialized.FindProperty("icon").objectReferenceValue),
                    "interactionRange=" + serialized.FindProperty("interactionRange").floatValue.ToString("R"),
                    "initialSlots=" + serialized.FindProperty("initialSlots").arraySize
                }
            };
        }

        private static void EnsureLegacyFallback(string sourcePath, string fallbackPath, string fallbackName)
        {
            if (MidpolyLegacyArchiveGuard.HasBackup(fallbackPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(fallbackPath));
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            GameObject copy = UnityEngine.Object.Instantiate(source);
            try
            {
                copy.name = fallbackName;
                if (PrefabUtility.SaveAsPrefabAsset(copy, fallbackPath) == null)
                    throw new IOException("Legacy-Fallback konnte nicht gespeichert werden: " + fallbackPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        private static void BuildWorldItem()
        {
            GameObject model = LoadModel("WorldItem");
            GameObject root = PrefabUtility.LoadPrefabContents(WorldItemPath);
            try
            {
                RemoveLodGroup(root);
                foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                    if (child.name == "DropBase" || child.name == "Geometry_A14" || child.name == "Geometry_MidPoly")
                        UnityEngine.Object.DestroyImmediate(child.gameObject);

                Transform staticHighlight = root.transform.Find("StaticHighlight");
                if (staticHighlight == null)
                    throw new InvalidOperationException("WorldItem/StaticHighlight fehlt.");
                foreach (MeshRenderer renderer in staticHighlight.GetComponents<MeshRenderer>())
                    UnityEngine.Object.DestroyImmediate(renderer);
                foreach (MeshFilter filter in staticHighlight.GetComponents<MeshFilter>())
                    UnityEngine.Object.DestroyImmediate(filter);

                Transform geometry = NewRoot(root.transform, "Geometry_A14");
                GameObject imported = ImportModel(model, root, "WorldItem_Imported");
                for (int lod = 0; lod < 3; lod++)
                    MoveBranch(imported.transform, "WorldItem_LOD" + lod, geometry);
                MoveBranch(imported.transform, "WorldItem_Highlight", staticHighlight);
                UnityEngine.Object.DestroyImmediate(imported);

                LODGroup group = BuildLodGroup(root, geometry, 0.58f, 0.28f, 0.01f);
                group.RecalculateBounds();
                // OP-11: Schwellen fuer die orthografische Spielkamera (LOD0 im Spielzoom sichtbar).
                MidpolyLodThresholds.ApplyOrthographicThresholds(group);
                if (PrefabUtility.SaveAsPrefabAsset(root, WorldItemPath) == null)
                    throw new IOException("WorldItem-Prefab konnte nicht gespeichert werden.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildDeathBag()
        {
            GameObject model = LoadModel("DeathBag");
            GameObject root = PrefabUtility.LoadPrefabContents(DeathBagPath);
            try
            {
                RemoveLodGroup(root);
                foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                Transform geometry = NewRoot(root.transform, "Geometry_A14");
                GameObject imported = ImportModel(model, root, "DeathBag_Imported");
                for (int lod = 0; lod < 3; lod++)
                    MoveBranch(imported.transform, "DeathBag_LOD" + lod, geometry);
                UnityEngine.Object.DestroyImmediate(imported);
                Transform interactionPoint = NewRoot(root.transform, "InteractionPoint");
                interactionPoint.localPosition = new Vector3(0f, 0.52f, -0.40f);
                LODGroup group = BuildLodGroup(root, geometry, 0.62f, 0.30f, 0.01f);
                group.RecalculateBounds();
                // OP-11: Schwellen fuer die orthografische Spielkamera (LOD0 im Spielzoom sichtbar).
                MidpolyLodThresholds.ApplyOrthographicThresholds(group);
                if (PrefabUtility.SaveAsPrefabAsset(root, DeathBagPath) == null)
                    throw new IOException("DeathBag-Prefab konnte nicht gespeichert werden.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject LoadModel(string name)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(CombinedPath(name));
            if (model == null) throw new InvalidOperationException(name + "-Modell wurde nicht importiert.");
            return model;
        }

        private static GameObject ImportModel(GameObject model, GameObject root, string name)
        {
            GameObject imported = (GameObject)PrefabUtility.InstantiatePrefab(model, root.scene);
            imported.name = name;
            imported.transform.SetParent(root.transform, false);
            imported.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            PrefabUtility.UnpackPrefabInstance(imported, PrefabUnpackMode.Completely,
                UnityEditor.InteractionMode.AutomatedAction);
            foreach (Renderer renderer in imported.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return imported;
        }

        private static LODGroup BuildLodGroup(GameObject root, Transform geometry, float lod0Height, float lod1Height, float lod2Height)
        {
            Renderer[] renderers = geometry.GetComponentsInChildren<Renderer>(true);
            Renderer[] lod0 = RenderersForLod(renderers, "LOD0");
            Renderer[] lod1 = RenderersForLod(renderers, "LOD1");
            Renderer[] lod2 = RenderersForLod(renderers, "LOD2");
            if (lod0.Length == 0 || lod1.Length == 0 || lod2.Length == 0)
                throw new InvalidOperationException(root.name + " besitzt nicht drei renderbare LOD-Stufen.");
            LODGroup group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = true;
            group.SetLODs(new[]
            {
                new LOD(lod0Height, lod0) { fadeTransitionWidth = 0.10f },
                new LOD(lod1Height, lod1) { fadeTransitionWidth = 0.10f },
                new LOD(lod2Height, lod2) { fadeTransitionWidth = 0.08f },
            });
            return group;
        }

        private static void RemoveLodGroup(GameObject root)
        {
            LODGroup oldGroup = root.GetComponent<LODGroup>();
            if (oldGroup != null) UnityEngine.Object.DestroyImmediate(oldGroup);
        }

        private static Transform NewRoot(Transform parent, string name)
        {
            Transform result = new GameObject(name).transform;
            result.SetParent(parent, false);
            return result;
        }

        private static void MoveBranch(Transform source, string exactName, Transform destination)
        {
            Transform found = source.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => string.Equals(item.name, exactName, StringComparison.Ordinal));
            if (found == null) throw new InvalidOperationException("Importzweig fehlt: " + exactName);
            found.SetParent(destination, true);
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
            foreach (string path in new[]
            {
                CombinedPath("WorldItem"), LodPath("WorldItem", 0), LodPath("WorldItem", 1), LodPath("WorldItem", 2),
                CombinedPath("DeathBag"), LodPath("DeathBag", 0), LodPath("DeathBag", 1), LodPath("DeathBag", 2),
                WorldItemPath, DeathBagPath, WorldFallbackPath, DeathFallbackPath
            })
            {
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset != null) AssetDatabase.SetLabels(asset,
                    new[] { "MIDPOLY-000", "MidPoly", "Phase4", "WorldLootKit", "ApprovedProduction" });
            }
        }

        private static void ValidateAndWriteReport(PrefabContract worldBefore, PrefabContract deathBefore)
        {
            List<string> errors = new List<string>();
            AssetReport world = ValidateAsset("WorldItem", WorldItemPath, WorldFallbackPath, worldBefore,
                CaptureWorldContract(), WorldTriangles, 0.40f, 4, errors);
            AssetReport death = ValidateAsset("DeathBag", DeathBagPath, DeathFallbackPath, deathBefore,
                CaptureDeathContract(), DeathTriangles, 0.70f, 5, errors);

            GameObject worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldItemPath);
            SerializedObject controller = new SerializedObject(worldPrefab.GetComponent<WorldItemController>());
            foreach (string property in new[] { "itemIcon", "quantityLabel", "highlight", "interactionTrigger" })
                if (controller.FindProperty(property).objectReferenceValue == null)
                    errors.Add("WorldItem: Referenz fehlt: " + property + ".");
            if (worldPrefab.transform.Find("StaticHighlight") == null || worldPrefab.transform.Find("ItemIcon") == null
                || worldPrefab.transform.Find("Quantity") == null)
                errors.Add("WorldItem: Icon-/Mengen-/Highlight-Anker unvollstaendig.");

            GameObject deathPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeathBagPath);
            StorageContainer storage = deathPrefab.GetComponent<StorageContainer>();
            if (storage == null || storage.ContainerId != "death.bag" || storage.SlotCapacity != 24
                || Mathf.Abs(storage.InteractionRange - 2.6f) > 0.0001f)
                errors.Add("DeathBag: Storagevertrag death.bag/24/2.6 verletzt.");

            TechnicalReport report = new TechnicalReport
            {
                generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                pass = errors.Count == 0,
                worldItem = world,
                deathBag = death,
                errors = errors.ToArray()
            };
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();
            if (!report.pass)
                throw new InvalidOperationException("Weltbeutel-Migration FAIL:\n" + string.Join("\n", errors));
            Debug.Log("[MIDPOLY-000] Weltbeutel PASS: ein Lootbeutel, ein Todesbeutel, je 3 LODs, GUID/Gameplay erhalten.");
        }

        private static AssetReport ValidateAsset(string name, string prefabPath, string fallbackPath,
            PrefabContract before, PrefabContract after, int[] expectedTriangles, float expectedHeight,
            int expectedMaterials, List<string> errors)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            LODGroup group = prefab.GetComponent<LODGroup>();
            int[] triangles = group == null ? new int[3] : group.GetLODs().Select(lod => TriangleCount(lod.renderers)).ToArray();
            int materials = group == null || group.GetLODs().Length == 0 ? 0 : group.GetLODs()[0].renderers
                .SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null)
                .Select(material => material.name).Distinct(StringComparer.Ordinal).Count();
            float height = VisualHeight(prefab);
            string[] anchors = name == "WorldItem"
                ? new[] { "Geometry_A14", "StaticHighlight", "ItemIcon", "Quantity" }
                : new[] { "Geometry_A14", "InteractionPoint" };
            string[] found = anchors.Where(anchor => prefab.transform.Find(anchor) != null).ToArray();

            if (!ContractsEqual(before, after)) errors.Add(name + ": GUID-/Collider-/Gameplayvertrag veraendert.");
            if (!MidpolyLegacyArchiveGuard.HasBackup(fallbackPath)) errors.Add(name + ": Legacy-Fallback fehlt.");
            if (group == null || group.GetLODs().Length != 3) errors.Add(name + ": LODGroup unvollstaendig.");
            if (!triangles.SequenceEqual(expectedTriangles))
                errors.Add(name + ": Dreieckszahlen " + string.Join("/", triangles) + " statt " + string.Join("/", expectedTriangles) + ".");
            if (!(triangles[0] > triangles[1] && triangles[1] > triangles[2]))
                errors.Add(name + ": LODs werden nicht progressiv leichter.");
            if (materials != expectedMaterials) errors.Add(name + ": Materialanzahl " + materials + " statt " + expectedMaterials + ".");
            if (Mathf.Abs(height - expectedHeight) > 0.012f)
                errors.Add(name + ": Sichthoehe " + height.ToString("F4") + " statt " + expectedHeight.ToString("F2") + ".");
            if (found.Length != anchors.Length) errors.Add(name + ": semantische Anker fehlen.");
            if (prefab.GetComponentsInChildren<Collider>(true).Any(collider => collider.transform != prefab.transform))
                errors.Add(name + ": unerlaubter Child-Collider.");

            return new AssetReport
            {
                role = name == "WorldItem" ? "single-shared-loot-pouch" : "single-persistent-death-bag",
                prefabPath = prefabPath,
                fallbackPath = fallbackPath,
                guidBefore = before.guid,
                guidAfter = after.guid,
                lod0Triangles = triangles.ElementAtOrDefault(0),
                lod1Triangles = triangles.ElementAtOrDefault(1),
                lod2Triangles = triangles.ElementAtOrDefault(2),
                lodLevels = group == null ? 0 : group.GetLODs().Length,
                materials = materials,
                visualHeight = height,
                colliderCenter = after.colliderCenter,
                colliderSize = after.colliderSize,
                colliderRadius = after.colliderRadius,
                anchors = found
            };
        }

        private static float VisualHeight(GameObject prefab)
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Transform geometry = instance.transform.Find("Geometry_A14");
                Renderer[] renderers = geometry.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => HierarchyContains(renderer.transform, "LOD0")).ToArray();
                if (renderers.Length == 0) return 0f;
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                return bounds.size.y;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static string[] RootComponents(GameObject prefab)
        {
            return prefab.GetComponents<Component>().Where(component => component != null && !(component is LODGroup))
                .Select(component => component.GetType().FullName).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static string ReferencePath(UnityEngine.Object reference, Transform root)
        {
            if (reference == null) return "null";
            Transform transform = reference is Component component ? component.transform
                : reference is GameObject gameObject ? gameObject.transform : null;
            return reference.GetType().FullName + ":" + (transform == null ? reference.name
                : AnimationUtility.CalculateTransformPath(transform, root));
        }

        private static string AssetReference(UnityEngine.Object reference)
        {
            if (reference == null) return "null";
            string path = AssetDatabase.GetAssetPath(reference);
            return reference.GetType().FullName + ":" + AssetDatabase.AssetPathToGUID(path) + ":" + reference.name;
        }

        private static bool ContractsEqual(PrefabContract left, PrefabContract right)
        {
            return left.guid == right.guid && Approximately(left.colliderCenter, right.colliderCenter)
                && Approximately(left.colliderSize, right.colliderSize)
                && Mathf.Abs(left.colliderRadius - right.colliderRadius) < 0.0001f
                && left.colliderTrigger == right.colliderTrigger && left.colliderEnabled == right.colliderEnabled
                && left.rootComponents.SequenceEqual(right.rootComponents, StringComparer.Ordinal)
                && left.serializedValues.SequenceEqual(right.serializedValues, StringComparer.Ordinal);
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude < 0.000001f;
        }
    }
}
