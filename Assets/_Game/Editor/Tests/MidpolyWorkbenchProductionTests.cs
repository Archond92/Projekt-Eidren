using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Eidren.Tests.EditMode
{
	public sealed class MidpolyWorkbenchProductionTests
	{
		private const string PrefabPath = "Assets/_Game/Prefabs/Stations/Workbench.prefab";
		private const string FallbackPath = "Assets/_Game/Prefabs/Stations/Fallback/Workbench_Legacy.prefab";

		[Test]
		public void ProductionPrefabPreservesGameplayAndGuidContract()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(prefab, Is.Not.Null);
			Assert.That(AssetDatabase.AssetPathToGUID(PrefabPath), Is.EqualTo("16ce8a1c41a4d574a906f32c4b7dc5e6"));
			Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(FallbackPath), Is.Null, "Legacy-Fallback liegt noch im Runtime-Baum.");
			Assert.That(System.IO.File.Exists("Documentation/Etappen/MidPoly/LegacyArchive/" + FallbackPath), Is.True, "Legacy-Archiv fehlt.");
			BoxCollider trigger = prefab.GetComponent<BoxCollider>();
			Assert.That(trigger, Is.Not.Null);
			Assert.That(trigger.isTrigger, Is.True);
			Assert.That(trigger.center, Is.EqualTo(new Vector3(0f, 0.75f, 0f)));
			Assert.That(trigger.size, Is.EqualTo(new Vector3(1f, 1.5f, 1f)));
			Assert.That(prefab.GetComponentsInChildren<MeshCollider>(true), Is.Empty);

			NavMeshObstacle obstacle = prefab.GetComponent<NavMeshObstacle>();
			Assert.That(obstacle, Is.Not.Null);
			Assert.That(obstacle.carving, Is.True);
			Assert.That(obstacle.center, Is.EqualTo(new Vector3(0f, 0.75f, 0f)));
			Assert.That(obstacle.size, Is.EqualTo(new Vector3(1f, 1.5f, 1f)));

			WorkbenchController controller = prefab.GetComponent<WorkbenchController>();
			Assert.That(controller, Is.Not.Null);
			Assert.That(controller.InteractionId, Is.EqualTo("home_base.workbench"));
			Assert.That(controller.DisplayText, Is.EqualTo("Werkbank benutzen"));
			Assert.That(controller.InteractionRange, Is.EqualTo(InteractionUtility.StandardSurfaceRange).Within(0.001f));
			Assert.That(controller.Station, Is.EqualTo(CraftingStationType.Workbench));
			Assert.That(prefab.transform.Find("ContactShadow"), Is.Not.Null);
		}

		[Test]
		public void ProductionPrefabHasExactThreeLodContractAndFiveMaterials()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			LODGroup group = prefab.GetComponent<LODGroup>();
			LOD[] lods = group.GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3));
			Assert.That(TriangleCount(lods[0].renderers), Is.EqualTo(18876));
			Assert.That(TriangleCount(lods[1].renderers), Is.EqualTo(9438));
			Assert.That(TriangleCount(lods[2].renderers), Is.EqualTo(3764));
			// OP-11: Schwellen aus der Bildschirmhoehe bei der Spielkamera (MidpolyLodThresholds).
			float height = Eidren.Editor.MidpolyLodThresholds.ScreenHeightAtGameCamera(group.size);
			Assert.That(lods[0].screenRelativeTransitionHeight, Is.EqualTo(Eidren.Editor.MidpolyLodThresholds.ThresholdFor(0, 3, height)).Within(0.001f));
			Assert.That(lods[1].screenRelativeTransitionHeight, Is.EqualTo(Eidren.Editor.MidpolyLodThresholds.ThresholdFor(1, 3, height)).Within(0.001f));
			Assert.That(lods[2].screenRelativeTransitionHeight, Is.EqualTo(Eidren.Editor.MidpolyLodThresholds.ThresholdFor(2, 3, height)).Within(0.001f));
			Assert.That(lods[2].screenRelativeTransitionHeight,
				Is.LessThan(group.size / (2f * 7.4f)),
				"Die baubare Werkbank braucht bei der normalen Spielkamera Sichtreserve (F33-001).");

			Material[] materials = lods[0].renderers.SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null).Distinct().ToArray();
			Assert.That(materials, Has.Length.EqualTo(5));
			foreach (Material material in materials)
			{
				Assert.That(material.shader, Is.Not.Null, material.name);
				Assert.That(material.shader.name.IndexOf("glTF", StringComparison.OrdinalIgnoreCase) >= 0
					|| material.shader.name.IndexOf("Universal Render Pipeline", StringComparison.OrdinalIgnoreCase) >= 0,
					Is.True, material.name + " verwendet keinen PBR-faehigen Unity-Shader: " + material.shader.name);
			}
			Assert.That(prefab.GetComponentsInChildren<Animation>(true), Is.Empty);
		}

		[Test]
		public void ProductionPrefabKeepsReadableFunctionalAnchorsAndFootprint()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Transform anchors = prefab.transform.Find("SemanticAnchors");
			Assert.That(anchors, Is.Not.Null);
			foreach (string part in new[] { "Worktop", "ToolRack", "Vise", "ToolSet", "InteractionPoint" })
				Assert.That(anchors.Find(part), Is.Not.Null, part);

			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				Renderer[] renderers = instance.GetComponent<LODGroup>().GetLODs()[0].renderers;
				Bounds bounds = renderers[0].bounds;
				foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
				Assert.That(bounds.size.x, Is.LessThanOrEqualTo(1.43f));
				Assert.That(bounds.size.y, Is.LessThanOrEqualTo(1.50f));
				Assert.That(bounds.size.z, Is.LessThanOrEqualTo(1.15f));
				Assert.That(bounds.center.z, Is.LessThan(0f), "Bedienseite muss zur bestehenden negativen Z-Seite zeigen.");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
		}

		private static int TriangleCount(IEnumerable<Renderer> renderers)
		{
			return renderers.Select(RendererMesh).Where(mesh => mesh != null).Distinct()
				.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount).Sum(index => (int)mesh.GetIndexCount(index) / 3));
		}

		private static Mesh RendererMesh(Renderer renderer)
		{
			SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
			if (skinned != null) return skinned.sharedMesh;
			MeshFilter filter = renderer.GetComponent<MeshFilter>();
			return filter != null ? filter.sharedMesh : null;
		}
	}
}
