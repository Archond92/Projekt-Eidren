using Eidren.Interaction;
using Eidren.Presentation;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
    public sealed class MidpolyWorldChestTests
    {
        private static readonly (string name, Vector3 size)[] Cases =
        {
            ("Common", new Vector3(1.15f, 0.54f, 0.72f)),
            ("Guarded", new Vector3(1.45f, 0.70f, 0.88f)),
            ("Hidden", new Vector3(1.00f, 0.48f, 0.64f)),
        };

        [Test]
        public void DreiWeltkisten_SindAlsFreigegebeneMidpolyFamilieIntegriert()
        {
            foreach ((string name, Vector3 _) in Cases)
            {
                string path = PrefabPath(name);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                Assert.That(Eidren.Editor.MidpolyWorldChestMigration.IsApprovedWorldChest("WorldChest_" + name), Is.True, name);
                LODGroup group = prefab.GetComponent<LODGroup>();
                Assert.That(group, Is.Not.Null, name);
                Assert.That(group.GetLODs(), Has.Length.EqualTo(3), name);
                Assert.That(group.GetLODs().All(lod => lod.renderers.Length > 0), Is.True, name);
                Assert.That(group.GetLODs()[group.GetLODs().Length - 1].screenRelativeTransitionHeight,
                    Is.LessThan(group.size / (2f * 7.4f) * 0.7f),
                    name + " wird bei der normalen Spielkamera (7,4, LOD-Bias 0,7 der Stufe Medium) ausgeblendet (F34-003).");
                Assert.That(group.GetLODs()[0].renderers.SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null).All(material => material.name.StartsWith("MP_WorldChest_", StringComparison.Ordinal)),
                    Is.True, name + " nutzt ein fremdes Material");
            }
        }

        [Test]
        public void GameplayColliderUndTrigger_BleibenAufDemAltvertrag()
        {
            foreach ((string name, Vector3 size) in Cases)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
                BoxCollider box = prefab.GetComponent<BoxCollider>();
                SphereCollider sphere = prefab.GetComponent<SphereCollider>();
                Assert.That(box, Is.Not.Null, name);
                Assert.That(box.center, Is.EqualTo(Vector3.up * size.y * 0.5f), name);
                Assert.That(box.size, Is.EqualTo(size), name);
                Assert.That(box.isTrigger, Is.False, name);
                Assert.That(sphere, Is.Not.Null, name);
                Assert.That(sphere.isTrigger, Is.True, name);
                Assert.That(sphere.radius, Is.EqualTo(1.45f), name);
                Assert.That(prefab.GetComponentsInChildren<Collider>(true).Count(collider => collider.transform != prefab.transform), Is.Zero, name);
            }
        }

        [Test]
        public void DeckelUndVierLootzustaende_SindVollstaendigVerdrahtet()
        {
            foreach ((string name, Vector3 _) in Cases)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
                WorldChestVisual visual = prefab.GetComponent<WorldChestVisual>();
                WorldChestContainer container = prefab.GetComponent<WorldChestContainer>();
                Assert.That(visual, Is.Not.Null, name);
                Assert.That(container, Is.Not.Null, name);
                foreach (string anchor in new[] { "CarvedBase", "LidPivot", "ClosedDetails", "OpenedDetails", "EmptiedDetails", "LootFill_Full", "LootFill_Partial", "LootSocket", "InteractionPoint" })
                    Assert.That(prefab.transform.Find(anchor), Is.Not.Null, name + "/" + anchor);
                SerializedObject serializedVisual = new SerializedObject(visual);
                foreach (string property in new[] { "lid", "closedDetails", "openedDetails", "emptiedDetails", "lootFull", "lootPartial" })
                    Assert.That(serializedVisual.FindProperty(property).objectReferenceValue, Is.Not.Null, name + "/" + property);
                Assert.That(serializedVisual.FindProperty("leftHand").objectReferenceValue, Is.Null, name);
                Assert.That(serializedVisual.FindProperty("rightHand").objectReferenceValue, Is.Null, name);
                Assert.That(prefab.GetComponentsInChildren<Transform>(true).Any(child => child.name.StartsWith("OpeningHand", StringComparison.Ordinal)), Is.False, name);
                SerializedObject serializedContainer = new SerializedObject(container);
                Assert.That(serializedContainer.FindProperty("visual").objectReferenceValue, Is.EqualTo(visual), name);
                Assert.That(serializedContainer.FindProperty("interactionRange").floatValue, Is.EqualTo(InteractionUtility.StandardSurfaceRange), name);
                Assert.That(serializedContainer.FindProperty("holdDuration").floatValue, Is.EqualTo(1.6f), name);
            }
        }

        [Test]
        public void DeckelOeffnetUm72GradUndHebtDieSilhouette()
        {
            foreach ((string name, Vector3 _) in Cases)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    LODGroup group = instance.GetComponent<LODGroup>();
                    group.ForceLOD(0);
                    WorldChestVisual visual = instance.GetComponent<WorldChestVisual>();
                    Transform lid = instance.transform.Find("LidPivot");
                    Renderer[] lidRenderers = lid.GetComponentsInChildren<Renderer>(true)
                        .Where(renderer => renderer.transform.name.IndexOf("LOD0", StringComparison.OrdinalIgnoreCase) >= 0
                            || renderer.transform.parent != null).ToArray();
                    visual.Apply(WorldChestVisualState.Closed);
                    Quaternion closedRotation = lid.localRotation;
                    Bounds closed = BoundsOf(lidRenderers);
                    visual.Apply(WorldChestVisualState.Opened);
                    Bounds opened = BoundsOf(lidRenderers);
                    Assert.That(Quaternion.Angle(closedRotation, lid.localRotation), Is.EqualTo(72f).Within(0.05f), name);
                    Assert.That(opened.center.y, Is.GreaterThan(closed.center.y + 0.06f), name + " Deckel hebt sich nicht");
                    Assert.That(opened.size.y, Is.GreaterThan(closed.size.y + 0.08f), name + " Silhouette bleibt geschlossen");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        public void LegacyFallbacks_SindFuerAlleVariantenArchiviert()
        {
            foreach ((string name, Vector3 _) in Cases)
            {
                string fallback = "Assets/_Game/Prefabs/Loot/WorldChests/Fallback/WorldChest_" + name + "_Legacy.prefab";
                Assert.That(File.Exists(fallback), Is.False, name + " liegt noch im Runtime-Baum");
                Assert.That(File.Exists("Documentation/Etappen/MidPoly/LegacyArchive/" + fallback), Is.True, name + " fehlt im Archiv");
            }
        }

        private static string PrefabPath(string name)
        {
            return "Assets/_Game/Prefabs/Loot/WorldChests/WorldChest_" + name + ".prefab";
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
