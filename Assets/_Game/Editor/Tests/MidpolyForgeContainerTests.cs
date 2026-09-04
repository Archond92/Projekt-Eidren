using Eidren.Presentation;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
    public sealed class MidpolyForgeContainerTests
    {
        private static readonly (string name, Vector3 size)[] Cases =
        {
            ("SupplyChest", new Vector3(1.18f, 0.80f, 0.78f)),
            ("OptionalChest", new Vector3(1.25f, 0.80f, 0.82f)),
            ("EliteChest", new Vector3(1.42f, 1.00f, 0.92f)),
            ("CompletionChest", new Vector3(1.62f, 1.20f, 1.02f)),
            ("SmallRewardChest", new Vector3(0.95f, 0.70f, 0.68f)),
            ("MediumRewardChest", new Vector3(1.20f, 0.90f, 0.82f)),
            ("LargeRewardChest", new Vector3(1.55f, 1.20f, 1.02f)),
            ("RecoveryContainer", new Vector3(1.45f, 0.80f, 0.82f)),
        };

        [Test]
        public void AchtRollen_SindAlsFreigegebeneMidpolyFamilieIntegriert()
        {
            foreach ((string name, Vector3 _) in Cases)
            {
                GameObject prefab = Load(name);
                Assert.That(Eidren.Editor.MidpolyForgeContainerMigration.IsApprovedContainer(name), Is.True, name);
                LODGroup group = prefab.GetComponent<LODGroup>();
                Assert.That(group, Is.Not.Null, name);
                Assert.That(group.GetLODs(), Has.Length.EqualTo(3), name);
                Assert.That(group.GetLODs().All(lod => lod.renderers.Length > 0), Is.True, name);
                Assert.That(group.GetLODs()[group.GetLODs().Length - 1].screenRelativeTransitionHeight,
                    Is.LessThan(group.size / (2f * 7.4f) * 0.7f),
                    name + " wird bei der normalen Spielkamera (7,4, LOD-Bias 0,7 der Stufe Medium) ausgeblendet (F34-003).");
                int[] triangles = group.GetLODs().Select(lod => lod.renderers
                    .Select(renderer => renderer.GetComponent<MeshFilter>())
                    .Where(filter => filter != null && filter.sharedMesh != null).Distinct()
                    .Sum(filter => filter.sharedMesh.triangles.Length / 3)).ToArray();
                Assert.That(triangles[0], Is.GreaterThan(triangles[1]), name);
                Assert.That(triangles[1], Is.GreaterThan(triangles[2]), name);
                string[] materials = group.GetLODs()[0].renderers.SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null).Select(material => material.name)
                    .Distinct(StringComparer.Ordinal).ToArray();
                Assert.That(materials, Has.Length.EqualTo(5), name);
                Assert.That(materials.All(material => material.StartsWith("MP_ForgeContainer_", StringComparison.Ordinal)), Is.True, name);
            }
        }

        [Test]
        public void RootCollider_BleibtExaktUndOhneChildCollider()
        {
            foreach ((string name, Vector3 size) in Cases)
            {
                GameObject prefab = Load(name);
                BoxCollider box = prefab.GetComponent<BoxCollider>();
                Assert.That(box, Is.Not.Null, name);
                Assert.That(box.center, Is.EqualTo(Vector3.up * size.y * 0.5f), name);
                Assert.That(box.size, Is.EqualTo(size), name);
                Assert.That(box.isTrigger, Is.False, name);
                Assert.That(prefab.GetComponentsInChildren<Collider>(true)
                    .Count(collider => collider.transform != prefab.transform), Is.Zero, name);
            }
        }

        [Test]
        public void DeckelUndVierLootzustaende_SindVollstaendigVerdrahtet()
        {
            foreach ((string name, Vector3 _) in Cases)
            {
                GameObject prefab = Load(name);
                WorldChestVisual visual = prefab.GetComponent<WorldChestVisual>();
                Assert.That(visual, Is.Not.Null, name);
                foreach (string anchor in new[]
                {
                    "ForgedBase", "LidPivot", "ClosedDetails", "OpenedDetails", "EmptiedDetails",
                    "LootFill_Full", "LootFill_Partial", "LootSocket", "InteractionPoint"
                })
                    Assert.That(prefab.transform.Find(anchor), Is.Not.Null, name + "/" + anchor);
                SerializedObject serialized = new SerializedObject(visual);
                foreach (string property in new[] { "lid", "closedDetails", "openedDetails", "emptiedDetails", "lootFull", "lootPartial" })
                    Assert.That(serialized.FindProperty(property).objectReferenceValue, Is.Not.Null, name + "/" + property);
                Assert.That(serialized.FindProperty("leftHand").objectReferenceValue, Is.Null, name);
                Assert.That(serialized.FindProperty("rightHand").objectReferenceValue, Is.Null, name);
            }
        }

        [Test]
        public void DeckelOeffnetUm72GradUndHebtDieSilhouette()
        {
            foreach ((string name, Vector3 _) in Cases)
            {
                GameObject instance = UnityEngine.Object.Instantiate(Load(name));
                try
                {
                    WorldChestVisual visual = instance.GetComponent<WorldChestVisual>();
                    Transform lid = instance.transform.Find("LidPivot");
                    Renderer[] renderers = lid.GetComponentsInChildren<Renderer>(true)
                        .Where(renderer => HasLod(renderer.transform, "LOD0")).ToArray();
                    visual.Apply(WorldChestVisualState.Closed);
                    Quaternion closedRotation = lid.localRotation;
                    Bounds closed = BoundsOf(renderers);
                    visual.Apply(WorldChestVisualState.Opened);
                    Bounds opened = BoundsOf(renderers);
                    Assert.That(Quaternion.Angle(closedRotation, lid.localRotation), Is.EqualTo(72f).Within(0.05f), name);
                    Assert.That(opened.center.y, Is.GreaterThan(closed.center.y + 0.04f), name + " Deckel hebt sich nicht");
                    Assert.That(opened.size.y, Is.GreaterThan(closed.size.y + 0.06f), name + " Silhouette bleibt geschlossen");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        public void LegacyFallbacks_SindFuerAlleAchtVariantenArchiviert()
        {
            foreach ((string name, Vector3 _) in Cases)
            {
                string fallback = Eidren.Editor.MidpolyForgeContainerMigration.FallbackPathFor(name);
                Assert.That(File.Exists(fallback), Is.False, name + " liegt noch im Runtime-Baum");
                Assert.That(File.Exists("Documentation/Etappen/MidPoly/LegacyArchive/" + fallback), Is.True, name + " fehlt im Archiv");
            }
        }

        private static GameObject Load(string name)
        {
            string path = Eidren.Editor.MidpolyForgeContainerMigration.PrefabPathFor(name);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }

        private static bool HasLod(Transform transform, string value)
        {
            for (Transform current = transform; current != null; current = current.parent)
                if (current.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            Assert.That(renderers, Is.Not.Empty);
            Bounds result = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) result.Encapsulate(renderer.bounds);
            return result;
        }
    }
}
