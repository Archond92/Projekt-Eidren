using NUnit.Framework;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyEnvironmentTests
	{
		private static readonly (string Name, float Width, float Height, float Depth, int Lod0, int Lod1, int Lod2, bool Collider)[] Specs =
		{
			("SP_Tree_A", 3.570f, 7.500f, 2.736f, 5404, 2800, 1124, true),
			("SP_Tree_B", 4.024f, 6.000f, 3.096f, 5404, 2800, 1124, true),
			("SP_Tree_C", 2.715f, 4.000f, 2.028f, 4124, 2136, 856, true),
			("SP_Plant_Bush", 1.501f, 0.900f, 1.176f, 3840, 1992, 804, false),
			("SP_Plant_Fern", 1.305f, 0.700f, 1.037f, 504, 252, 98, false),
			("SP_Plant_Flowers", 0.493f, 0.400f, 0.459f, 3780, 1960, 784, false),
			("SP_GroundCover_Grass", 1.188f, 0.150f, 1.200f, 640, 320, 120, false),
			("SP_GroundCover_Moss", 1.200f, 0.120f, 1.137f, 1920, 996, 396, false),
			("SP_Accent_GlowMushrooms", 0.540f, 0.350f, 0.484f, 3000, 1548, 612, false),
			("SP_Rock_Small", 1.826f, 0.503f, 1.684f, 2560, 1328, 536, true),
			("SP_Rock_Medium", 2.506f, 1.106f, 1.819f, 3840, 1992, 804, true),
			("SP_Rock_Large", 2.646f, 2.403f, 2.064f, 5120, 2656, 1072, true),
			("SP_RuinWall_A", 2.772f, 2.600f, 1.506f, 3240, 1680, 660, true),
			("SP_RuinWall_B", 2.168f, 2.600f, 1.240f, 2592, 1344, 528, true),
			("SP_RuinMonument", 1.795f, 5.000f, 1.436f, 4208, 2182, 858, true),
			("SP_EidrenRune", 0.721f, 0.800f, 0.440f, 1240, 644, 256, false)
		};

		[Test]
		public void ApprovedEnvironmentKitUsesProductionLodsMaterialsAndDimensions()
		{
			foreach ((string name, float width, float height, float depth, int lod0, int lod1, int lod2, bool _) in Specs)
			{
				GameObject prefab = Load(name);
				Assert.That(AssetDatabase.GetLabels(prefab), Does.Contain("Approved"), name);
				Assert.That(AssetDatabase.GetLabels(prefab), Does.Contain("Phase5Environment"), name);
				Assert.That(prefab.GetComponent<LODGroup>(), Is.Not.Null, name);
				Assert.That(prefab.GetComponent<LODGroup>().GetLODs(), Has.Length.EqualTo(3), name);
				Assert.That(prefab.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty, name);
				Assert.That(Triangles(prefab.transform.Find("LOD0")), Is.EqualTo(lod0), name + " LOD0");
				Assert.That(Triangles(prefab.transform.Find("LOD1")), Is.EqualTo(lod1), name + " LOD1");
				Assert.That(Triangles(prefab.transform.Find("LOD2")), Is.EqualTo(lod2), name + " LOD2");
				Bounds bounds = BoundsOf(prefab.transform.Find("LOD0"));
				Assert.That(bounds.size.x, Is.EqualTo(width).Within(0.03f), name + " width");
				Assert.That(bounds.size.y, Is.EqualTo(height).Within(0.015f), name + " height");
				Assert.That(bounds.size.z, Is.EqualTo(depth).Within(0.03f), name + " depth");
				Material[] materials = prefab.transform.Find("LOD0").GetComponentsInChildren<Renderer>(true).SelectMany(renderer => renderer.sharedMaterials).ToArray();
				Assert.That(materials, Has.None.Null, name);
				Assert.That(materials.All(material => material.enableInstancing), Is.True, name + " GPU instancing");
				Assert.That(materials.All(material => AssetDatabase.GetAssetPath(material).StartsWith("Assets/_Game/Art/MidPoly/Environment/Phase5/Materials/", StringComparison.Ordinal)), Is.True, name + " shared materials");
			}
		}

		[Test]
		public void EnvironmentRootColliderContractsRemainUnchanged()
		{
			foreach ((string name, _, _, _, _, _, _, bool collider) in Specs)
			{
				BoxCollider[] colliders = Load(name).GetComponents<BoxCollider>();
				Assert.That(colliders.Length, Is.EqualTo(collider ? 1 : 0), name);
			}
		}

		[Test]
		public void LegacyPropBuilderCannotOverwriteApprovedEnvironmentKit()
		{
			string[] paths = Specs.Select(spec => Path(spec.Name)).ToArray();
			string[] beforeGuids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
			int[] beforeTriangles = Specs.Select(spec => Triangles(Load(spec.Name).transform.Find("LOD0"))).ToArray();
			Assert.That(EditorApplication.ExecuteMenuItem("Eidren/V0.2/Stilumbau/Props bauen"), Is.True);
			AssetDatabase.SaveAssets();
			for (int index = 0; index < Specs.Length; index++)
			{
				Assert.That(AssetDatabase.AssetPathToGUID(paths[index]), Is.EqualTo(beforeGuids[index]), Specs[index].Name + " GUID");
				Assert.That(Triangles(Load(Specs[index].Name).transform.Find("LOD0")), Is.EqualTo(beforeTriangles[index]), Specs[index].Name + " LOD0");
			}
		}

		private static string Path(string name) => "Assets/_Game/Prefabs/Environment/StyleProof/" + name + ".prefab";

		private static GameObject Load(string name)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path(name));
			Assert.That(prefab, Is.Not.Null, name);
			return prefab;
		}

		private static int Triangles(Transform value)
		{
			return value.GetComponentsInChildren<MeshFilter>(true)
				.Where(filter => filter.sharedMesh != null)
				.Sum(filter => Enumerable.Range(0, filter.sharedMesh.subMeshCount).Sum(index => (int)filter.sharedMesh.GetIndexCount(index) / 3));
		}

		private static Bounds BoundsOf(Transform value)
		{
			Renderer[] renderers = value.GetComponentsInChildren<Renderer>(true);
			Bounds bounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
			return bounds;
		}
	}
}
