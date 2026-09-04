using Eidren.Interaction;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyStorageChestProductionTests
	{
		private const string PrefabPath = "Assets/_Game/Prefabs/Stations/StorageChest.prefab";
		private const string FallbackPath = "Assets/_Game/Prefabs/Stations/Fallback/StorageChest_Legacy.prefab";

		[Test]
		public void ProductionPrefabPreservesGameplayAndLodContracts()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(prefab, Is.Not.Null);
			Assert.That(AssetDatabase.AssetPathToGUID(PrefabPath), Is.EqualTo("bed0f5490a6014e4b955be7cdc8845ba"));
			Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(FallbackPath), Is.Null);
			Assert.That(System.IO.File.Exists("Documentation/Etappen/MidPoly/LegacyArchive/" + FallbackPath), Is.True);
			Assert.That(prefab.transform.Find("Geometry_A14"), Is.Not.Null);
			Assert.That(prefab.transform.Find("ContactShadow"), Is.Not.Null);

			BoxCollider trigger = prefab.GetComponent<BoxCollider>();
			Assert.That(trigger, Is.Not.Null);
			Assert.That(trigger.isTrigger, Is.True);
			Assert.That(trigger.center, Is.EqualTo(new Vector3(0f, 0.75f, 0f)));
			Assert.That(trigger.size, Is.EqualTo(new Vector3(1f, 1.5f, 1f)));

			StorageContainer storage = prefab.GetComponent<StorageContainer>();
			Assert.That(storage, Is.Not.Null);
			Assert.That(storage.ContainerId, Is.EqualTo("home_base.storage.main"));
			Assert.That(storage.DisplayText, Is.EqualTo("Lagerkiste öffnen"));
			Assert.That(storage.InteractionRange, Is.EqualTo(InteractionUtility.StandardSurfaceRange).Within(0.0001f));
			Assert.That(storage.SlotCapacity, Is.EqualTo(24));

			LODGroup group = prefab.GetComponent<LODGroup>();
			Assert.That(group, Is.Not.Null);
			LOD[] lods = group.GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3));
			Assert.That(lods[lods.Length - 1].screenRelativeTransitionHeight,
				Is.LessThan(group.size / (2f * 7.4f)),
				"Die baubare Lagerkiste wird bei der normalen Spielkamera ausgeblendet (F33-001).");
			Assert.That(Triangles(lods[0].renderers), Is.EqualTo(7240));
			Assert.That(Triangles(lods[1].renderers), Is.EqualTo(3556));
			Assert.That(Triangles(lods[2].renderers), Is.EqualTo(1420));
			Assert.That(lods[0].renderers.SelectMany(item => item.sharedMaterials)
				.Where(item => item != null).Select(item => item.name).Distinct().Count(), Is.EqualTo(3));
			Assert.That(prefab.GetComponentsInChildren<Collider>(true), Has.Length.EqualTo(1));
		}

		[Test]
		public void LidIsACompleteSeparateAssemblyWithWorkingRearHinge()
		{
			GameObject instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
			try
			{
				Transform[] pivots = instance.GetComponentsInChildren<Transform>(true)
					.Where(item => item.name.StartsWith("ChestLid_LOD", StringComparison.Ordinal)).ToArray();
				Assert.That(pivots, Has.Length.EqualTo(3));
				Transform pivot = pivots.Single(item => item.name.StartsWith("ChestLid_LOD0", StringComparison.Ordinal));
				Renderer[] renderers = pivot.GetComponentsInChildren<Renderer>(true);
				Assert.That(renderers, Has.Length.GreaterThanOrEqualTo(10));
				Bounds closed = BoundsOf(renderers);
				Quaternion rotation = pivot.localRotation;
				pivot.localRotation = rotation * Quaternion.AngleAxis(-105f, Vector3.right);
				Bounds opened = BoundsOf(renderers);
				Assert.That(opened.size.y, Is.GreaterThan(0.45f));
				Assert.That(Mathf.Abs(opened.center.y - closed.center.y), Is.GreaterThan(0.08f));
				pivot.localRotation = rotation;
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
		}

		[Test]
		public void LegacySemanticAndBuilderGuardNamesRemainReachable()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Transform geometry = prefab.transform.Find("Geometry_A14");
			foreach (string name in new[]
			{
				"ChestBody", "ChestLid", "ChestFrontPlank_0", "LidPlank_0", "FrontBand",
				"Latch", "LatchRing", "LidHinge", "LootSocket", "InteractionPoint"
			})
			{
				Assert.That(geometry.Find(name), Is.Not.Null, name);
			}
		}

		private static int Triangles(Renderer[] renderers)
		{
			return renderers.Select(RendererMesh).Where(mesh => mesh != null).Distinct()
				.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount).Sum(index => (int)mesh.GetIndexCount(index) / 3));
		}

		private static Mesh RendererMesh(Renderer renderer)
		{
			MeshFilter filter = renderer.GetComponent<MeshFilter>();
			return filter != null ? filter.sharedMesh : null;
		}

		private static Bounds BoundsOf(Renderer[] renderers)
		{
			Bounds bounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
			return bounds;
		}
	}
}
