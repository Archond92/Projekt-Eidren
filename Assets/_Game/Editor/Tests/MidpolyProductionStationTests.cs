using Eidren.Interaction;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Eidren.Editor.Tests
{
    public sealed class MidpolyProductionStationTests
    {
        private static readonly Dictionary<string, int[]> ExpectedTriangles = new Dictionary<string, int[]>
        {
            { "Smelter", new[] { 4704, 1984, 1492 } },
            { "Sawmill", new[] { 3380, 1544, 1438 } },
            { "Stonecutter", new[] { 2912, 1152, 996 } },
            { "Ropewalk", new[] { 5724, 2328, 2192 } },
            { "CookingPot", new[] { 5436, 2396, 1452 } },
            { "FarmPlot", new[] { 1448, 752, 460 } },
        };

        [TestCase("Smelter", "78b57420ade324645b8ba72ddd30e9c8", 1f, 2f, 1f, 0f, 1f, 0f)]
        [TestCase("Sawmill", "4bb27fd0bd78fc146b07013df30a0a12", 1f, 2.2f, 1f, 0f, 1.1f, 0f)]
        [TestCase("Stonecutter", "4c529c806cfe65243a315794b338feaa", 1f, 1.4f, 1f, 0f, 0.7f, 0f)]
        [TestCase("Ropewalk", "9a073ed884e040f44a3c64e9d1194986", 1f, 1.6f, 1f, 0f, 0.8f, 0f)]
        [TestCase("CookingPot", "fb568eae807259340a1e73de8adf60e3", 1f, 1f, 1f, 0f, 0.5f, 0f)]
        [TestCase("FarmPlot", "f2a380512a473b643a3fe840862b44f3", 2f, 0.15f, 2f, 0f, 0.075f, 0f)]
        public void StationPreservesGameplayContractAndUsesThreeLods(string name, string guid,
            float sizeX, float sizeY, float sizeZ, float centerX, float centerY, float centerZ)
        {
            string path = "Assets/_Game/Prefabs/Buildings/Level01/BLD_" + name + "_L01.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid), name + " GUID");
            Assert.That(MidpolyProductionStationMigration.IsApprovedStation(name), Is.True, name);
            Assert.That(prefab.transform.Find("Geometry_A14"), Is.Not.Null, name);
            Assert.That(prefab.transform.Find("Geometry_A14").GetComponentsInChildren<Collider>(true), Is.Empty, name);

            BoxCollider collider = prefab.GetComponent<BoxCollider>();
            Assert.That(collider, Is.Not.Null, name);
            Assert.That(collider.size, Is.EqualTo(new Vector3(sizeX, sizeY, sizeZ)), name);
            Assert.That(collider.center, Is.EqualTo(new Vector3(centerX, centerY, centerZ)), name);
            Assert.That(prefab.GetComponent<NavMeshObstacle>(), Is.Not.Null, name);
            Assert.That(name == "FarmPlot" ? (Component)prefab.GetComponent<FarmPlotController>()
                : prefab.GetComponent<WorkbenchController>(), Is.Not.Null, name);

            LODGroup group = prefab.GetComponent<LODGroup>();
            Assert.That(group, Is.Not.Null, name);
            LOD[] lods = group.GetLODs();
            Assert.That(lods, Has.Length.EqualTo(3), name);
            float standardCameraHeight = group.size / (2f * 7.4f);
            Assert.That(lods[lods.Length - 1].screenRelativeTransitionHeight,
                Is.LessThan(standardCameraHeight),
                name + " wird bei der normalen Spielkamera vollstaendig ausgeblendet (F33-001).");
            int[] triangles = lods.Select(lod => lod.renderers
                .Select(renderer => renderer.GetComponent<MeshFilter>())
                .Where(filter => filter != null && filter.sharedMesh != null)
                .Distinct().Sum(filter => filter.sharedMesh.triangles.Length / 3)).ToArray();
            Assert.That(triangles, Is.EqualTo(ExpectedTriangles[name]), name);
            foreach (Renderer renderer in lods[0].renderers)
            {
                Assert.That(renderer.sharedMaterial, Is.Not.Null, name + "/" + renderer.name);
                Assert.That(renderer.sharedMaterial.name, Does.StartWith("MP_Production_"), name + "/" + renderer.name);
            }
        }

        [Test]
        public void FarmPlotKeepsThreeDistinctWiredVisualStates()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Buildings/Level01/BLD_FarmPlot_L01.prefab");
            FarmPlotController controller = prefab.GetComponent<FarmPlotController>();
            SerializedObject serialized = new SerializedObject(controller);
            GameObject empty = serialized.FindProperty("emptyVisual").objectReferenceValue as GameObject;
            GameObject planted = serialized.FindProperty("plantedVisual").objectReferenceValue as GameObject;
            GameObject ready = serialized.FindProperty("readyVisual").objectReferenceValue as GameObject;
            Assert.That(empty.name, Is.EqualTo("VIS_Empty"));
            Assert.That(planted.name, Is.EqualTo("VIS_Planted"));
            Assert.That(ready.name, Is.EqualTo("VIS_Ready"));
            Assert.That(planted.GetComponentsInChildren<MeshRenderer>(true), Is.Not.Empty);
            Assert.That(ready.GetComponentsInChildren<MeshRenderer>(true), Is.Not.Empty);
            Assert.That(planted, Is.Not.SameAs(ready));
        }

        [Test]
        public void EveryStationHasArchivedLegacyAndWorkingSideAnchors()
        {
            foreach (string name in ExpectedTriangles.Keys)
            {
                string fallback = "Assets/_Game/Prefabs/Buildings/Fallback/BLD_" + name + "_L01_Legacy.prefab";
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(fallback), Is.Null, fallback + " darf nicht mehr im Runtime-Baum liegen");
                Assert.That(File.Exists("Documentation/Etappen/MidPoly/LegacyArchive/" + fallback), Is.True, fallback + " fehlt im Archiv");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Game/Prefabs/Buildings/Level01/BLD_" + name + "_L01.prefab");
                Transform geometry = prefab.transform.Find("Geometry_A14");
                Assert.That(geometry.Find("WorkSide"), Is.Not.Null, name);
                Assert.That(geometry.Find("InteractionPoint"), Is.Not.Null, name);
            }
        }
    }
}
