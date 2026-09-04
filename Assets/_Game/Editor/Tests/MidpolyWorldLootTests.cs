using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
    public sealed class MidpolyWorldLootTests
    {
        [Test]
        public void EsGibtGenauEinenGemeinsamenLootbeutel_OhneKategoriezweige()
        {
            GameObject prefab = Load(Eidren.Editor.MidpolyWorldLootMigration.WorldItemPrefabPath);
            Assert.That(Eidren.Editor.MidpolyWorldLootMigration.IsApprovedWorldItem(), Is.True);
            Transform geometry = prefab.transform.Find("Geometry_A14");
            Assert.That(geometry, Is.Not.Null);
            string[] names = geometry.GetComponentsInChildren<Transform>(true).Select(item => item.name).ToArray();
            Assert.That(names.Count(name => name == "WorldItem_LOD0"), Is.EqualTo(1));
            Assert.That(names.Count(name => name == "WorldItem_LOD1"), Is.EqualTo(1));
            Assert.That(names.Count(name => name == "WorldItem_LOD2"), Is.EqualTo(1));
            Assert.That(names.Any(name => name.IndexOf("Resource", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Consumable", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Armor", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
        }

        [Test]
        public void Lootbeutel_BehaeltIconMengeTriggerUndInteraktionsvertrag()
        {
            GameObject prefab = Load(Eidren.Editor.MidpolyWorldLootMigration.WorldItemPrefabPath);
            SphereCollider sphere = prefab.GetComponent<SphereCollider>();
            WorldItemController controller = prefab.GetComponent<WorldItemController>();
            Assert.That(sphere, Is.Not.Null);
            Assert.That(sphere.isTrigger, Is.True);
            Assert.That(sphere.radius, Is.EqualTo(0.72f).Within(0.0001f));
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.InteractionRange, Is.EqualTo(InteractionUtility.StandardSurfaceRange).Within(0.0001f));
            Assert.That(controller.Priority, Is.EqualTo(30));
            SerializedObject serialized = new SerializedObject(controller);
            foreach (string property in new[] { "itemIcon", "quantityLabel", "highlight", "interactionTrigger" })
                Assert.That(serialized.FindProperty(property).objectReferenceValue, Is.Not.Null, property);
            Assert.That(prefab.GetComponent<AudioSource>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true)
                .Count(collider => collider.transform != prefab.transform), Is.Zero);
        }

        [Test]
        public void Lootbeutel_InitialisierungZeigtWeiterhinIconUndMenge()
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/Wood.asset");
            Assert.That(item, Is.Not.Null);
            GameObject instance = UnityEngine.Object.Instantiate(Load(Eidren.Editor.MidpolyWorldLootMigration.WorldItemPrefabPath));
            try
            {
                WorldItemController controller = instance.GetComponent<WorldItemController>();
                controller.Initialize(item, 3, "test.world-loot.3");
                Assert.That(controller.Item, Is.SameAs(item));
                Assert.That(controller.Quantity, Is.EqualTo(3));
                Assert.That(instance.transform.Find("Quantity").GetComponent<TextMesh>().text, Is.EqualTo("x3"));
                SpriteRenderer icon = instance.transform.Find("ItemIcon").GetComponent<SpriteRenderer>();
                Assert.That(icon.sprite, Is.EqualTo(item.Icon));
                Assert.That(icon.enabled, Is.EqualTo(item.Icon != null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Todesbeutel_IstEinEinzelmodellMitUnveraendertemStoragevertrag()
        {
            GameObject prefab = Load(Eidren.Editor.MidpolyWorldLootMigration.DeathBagPrefabPath);
            Assert.That(Eidren.Editor.MidpolyWorldLootMigration.IsApprovedDeathBag(), Is.True);
            BoxCollider box = prefab.GetComponent<BoxCollider>();
            StorageContainer storage = prefab.GetComponent<StorageContainer>();
            Assert.That(box.center, Is.EqualTo(new Vector3(0f, 0.4f, 0f)));
            Assert.That(box.size, Is.EqualTo(new Vector3(1.8f, 1.4f, 1.8f)));
            Assert.That(box.isTrigger, Is.True);
            Assert.That(storage.ContainerId, Is.EqualTo("death.bag"));
            Assert.That(storage.SlotCapacity, Is.EqualTo(24));
            Assert.That(storage.InteractionRange, Is.EqualTo(InteractionUtility.StandardSurfaceRange).Within(0.0001f));
            Transform geometry = prefab.transform.Find("Geometry_A14");
            Assert.That(geometry.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == "DeathBag_LOD0"), Is.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<Collider>(true)
                .Count(collider => collider.transform != prefab.transform), Is.Zero);
        }

        [Test]
        public void BeideBeutel_HabenDreiProgressiveLodsUndExakteSichthoehen()
        {
            CheckLods(Eidren.Editor.MidpolyWorldLootMigration.WorldItemPrefabPath,
                new[] { 1144, 468, 444 }, 0.40f, 4);
            CheckLods(Eidren.Editor.MidpolyWorldLootMigration.DeathBagPrefabPath,
                new[] { 1448, 420, 392 }, 0.70f, 5);
        }

        [Test]
        public void BeideLegacyFallbacks_SindArchiviert()
        {
            foreach (string fallback in new[] { Eidren.Editor.MidpolyWorldLootMigration.WorldItemFallbackPath, Eidren.Editor.MidpolyWorldLootMigration.DeathBagFallbackPath })
            {
                Assert.That(File.Exists(fallback), Is.False, fallback + " liegt noch im Runtime-Baum");
                Assert.That(File.Exists("Documentation/Etappen/MidPoly/LegacyArchive/" + fallback), Is.True, fallback + " fehlt im Archiv");
            }
        }

        private static void CheckLods(string path, int[] expectedTriangles, float expectedHeight, int expectedMaterials)
        {
            GameObject prefab = Load(path);
            LODGroup group = prefab.GetComponent<LODGroup>();
            Assert.That(group, Is.Not.Null, path);
            Assert.That(group.GetLODs(), Has.Length.EqualTo(3), path);
            Assert.That(group.GetLODs()[2].screenRelativeTransitionHeight,
                Is.LessThan(group.size / (2f * 7.4f) * 0.7f),
                path + " wird bei der normalen Spielkamera (7,4, LOD-Bias 0,7 der Stufe Medium) ausgeblendet (F34-003).");
            int[] triangles = group.GetLODs().Select(lod => lod.renderers
                .Select(renderer => renderer.GetComponent<MeshFilter>())
                .Where(filter => filter != null && filter.sharedMesh != null).Distinct()
                .Sum(filter => filter.sharedMesh.triangles.Length / 3)).ToArray();
            Assert.That(triangles, Is.EqualTo(expectedTriangles), path);
            Assert.That(triangles[0], Is.GreaterThan(triangles[1]), path);
            Assert.That(triangles[1], Is.GreaterThan(triangles[2]), path);
            string[] materials = group.GetLODs()[0].renderers.SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null).Select(material => material.name)
                .Distinct(StringComparer.Ordinal).ToArray();
            Assert.That(materials, Has.Length.EqualTo(expectedMaterials), path);
            Assert.That(materials.All(material => material.StartsWith("MP_WorldLoot_", StringComparison.Ordinal)), Is.True, path);
            Assert.That(VisualHeight(prefab), Is.EqualTo(expectedHeight).Within(0.012f), path);
        }

        private static float VisualHeight(GameObject prefab)
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Transform geometry = instance.transform.Find("Geometry_A14");
                Renderer[] renderers = geometry.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => HasLod(renderer.transform, "LOD0")).ToArray();
                Assert.That(renderers, Is.Not.Empty);
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                return bounds.size.y;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static bool HasLod(Transform transform, string value)
        {
            for (Transform current = transform; current != null; current = current.parent)
                if (current.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static GameObject Load(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }
    }
}
