using Eidren.Editor;
using Eidren.Interaction;
using Eidren.Presentation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyBaseModuleProductionTests
	{
		private static readonly Dictionary<string, int[]> Expected = new Dictionary<string, int[]>
		{
			{ "Wall", new[] { 3492, 956, 484 } },
			{ "Floor", new[] { 2876, 1068, 308 } },
			{ "Door", new[] { 3644, 1264, 800 } }
		};

		[TestCase("Wall", 1f, 2.6f, 0.2f, 0f, 1.3f, 0f)]
		[TestCase("Floor", 1f, 0.15f, 1f, 0f, 0.075f, 0f)]
		[TestCase("Door", 1f, 2.6f, 0.2f, 0f, 1.3f, 0f)]
		public void ProduktiveBasismoduleHabenDreiLodsUndUnveraenderteRootPhysik(
			string name, float sx, float sy, float sz, float cx, float cy, float cz)
		{
			GameObject prefab = Load(name);
			Assert.That(prefab.transform.Find("Geometry_A14"), Is.Not.Null);
			Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(true), Is.Null);
			LODGroup group = prefab.GetComponent<LODGroup>();
			Assert.That(group, Is.Not.Null);
			LOD[] lods = group.GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3));
			float standardCameraHeight = group.size / (2f * 7.4f);
			Assert.That(lods[lods.Length - 1].screenRelativeTransitionHeight,
				Is.LessThan(standardCameraHeight),
				name + " wird bei der normalen Spielkamera vollstaendig ausgeblendet (F33-001).");
			for (int index = 0; index < 3; index++)
			{
				Assert.That(TriangleCount(lods[index].renderers), Is.EqualTo(Expected[name][index]), name + "/LOD" + index);
			}
			foreach (Renderer renderer in lods[0].renderers)
			{
				foreach (Material material in renderer.sharedMaterials.Where(material => material != null))
					Assert.That(material.name, Does.StartWith("MP_BaseModule_"), renderer.name);
			}
			BoxCollider collider = prefab.GetComponent<BoxCollider>();
			Assert.That(collider, Is.Not.Null);
			Assert.That(collider.size, Is.EqualTo(new Vector3(sx, sy, sz)));
			Assert.That(collider.center, Is.EqualTo(new Vector3(cx, cy, cz)));
			Assert.That(prefab.transform.Find("Geometry_A14").GetComponentsInChildren<Collider>(true), Is.Empty,
				"Grafik darf die Bauphysik nicht veraendern.");
		}

		[Test]
		public void TuerblattSchwenktGemeinsamUmDieLinkeAngelWaerendDerRahmenSteht()
		{
			GameObject instance = UnityEngine.Object.Instantiate(Load("Door"));
			try
			{
				instance.GetComponent<LODGroup>().ForceLOD(0);
				BuildingDoorView view = instance.GetComponentInChildren<BuildingDoorView>(true);
				Assert.That(view, Is.Not.Null);
				Assert.That(view.Blade.localPosition, Is.EqualTo(new Vector3(-0.5f, 0f, 0f)));
				Transform[] leaves = view.Blade.GetComponentsInChildren<Transform>(true)
					.Where(item => item.name.StartsWith("DoorLeaf_LOD", StringComparison.Ordinal)).ToArray();
				Assert.That(leaves, Has.Length.EqualTo(3));
				Transform frame = instance.GetComponentsInChildren<Transform>(true)
					.First(item => item.name.StartsWith("RahmenLinksMesh_LOD0", StringComparison.Ordinal));
				Vector3 frameBefore = frame.position;
				Renderer leafRenderer = leaves.First(item => item.name.StartsWith("DoorLeaf_LOD0", StringComparison.Ordinal))
					.GetComponentInChildren<Renderer>(true);
				Vector3 leafBefore = leafRenderer.bounds.center;
				view.Blade.localRotation = Quaternion.Euler(0f, 105f, 0f);
				Assert.That(Vector3.Distance(frame.position, frameBefore), Is.LessThan(0.0001f));
				Assert.That(Vector3.Distance(leafRenderer.bounds.center, leafBefore), Is.GreaterThan(0.15f));
				Assert.That(view.BlockingCollider, Is.SameAs(instance.GetComponent<BoxCollider>()));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
		}

		[TestCase("Wall")]
		[TestCase("Door")]
		public void WandverbinderNutzenKitmeshUndBleibenColliderfrei(string name)
		{
			GameObject prefab = Load(name);
			Assert.That(prefab.GetComponent<WallConnectionView>(), Is.Not.Null);
			foreach (string pieceName in new[] { "Joint_MinusX", "Joint_PlusX", "Cap_MinusX", "Cap_PlusX" })
			{
				Transform piece = prefab.transform.Find(pieceName);
				Assert.That(piece, Is.Not.Null, pieceName);
				Assert.That(piece.GetComponent<MeshFilter>().sharedMesh.name, Does.Contain("ConnectionPieceTemplate"));
				Assert.That(piece.GetComponent<MeshRenderer>().sharedMaterial.name, Does.StartWith("MP_BaseModule_Wood"));
				Assert.That(piece.GetComponent<Collider>(), Is.Null);
				Assert.That(Mathf.Abs(piece.localPosition.x), Is.EqualTo(0.5f).Within(0.001f));
			}
		}

		[Test]
		public void LegacyFallbacksSindArchiviertUndProduktiveBuilderwaechterSindVorhanden()
		{
			foreach (string name in Expected.Keys)
			{
				Assert.That(MidpolyBaseModuleMigration.IsApprovedModule(name), Is.True, name);
				string fallback = "Assets/_Game/Prefabs/Buildings/Fallback/BLD_" + name + "_L01_Legacy.prefab";
				Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(fallback), Is.Null, fallback + " darf nicht mehr im Runtime-Baum liegen");
				Assert.That(File.Exists("Documentation/Etappen/MidPoly/LegacyArchive/" + fallback), Is.True, fallback + " fehlt im Archiv");
			}
		}

		private static GameObject Load(string name)
		{
			string path = "Assets/_Game/Prefabs/Buildings/Level01/BLD_" + name + "_L01.prefab";
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			Assert.That(prefab, Is.Not.Null, path);
			return prefab;
		}

		private static int TriangleCount(IEnumerable<Renderer> renderers)
		{
			return renderers.Select(renderer => renderer.GetComponent<MeshFilter>())
				.Where(filter => filter != null && filter.sharedMesh != null).Select(filter => filter.sharedMesh).Distinct()
				.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount).Sum(index => (int)mesh.GetIndexCount(index) / 3));
		}
	}
}
