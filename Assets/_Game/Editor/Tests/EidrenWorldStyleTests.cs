using System.Collections.Generic;
using System.Linq;
using Eidren.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class EidrenWorldStyleTests
	{
		/* G-004: Das ContactShadow-Kind ist der Bodenschatten, keine
		   Weltgeometrie. Es traegt bewusst das eingebaute Quad ohne
		   Vertexfarben und das Material M_World_ContactShadow. Sein eigener
		   Vertrag steht vollstaendig in GroundContactTests: Anwesenheit nach
		   der Aufnahmeregel, Material, ShadowCastingMode und Einmaligkeit.

		   Die Stilpruefung klammert es deshalb aus, statt ihre Zusicherungen
		   abzuschwaechen — die Absicht "jede Weltgeometrie traegt Vertexfarben
		   und das gemeinsame Material" bleibt fuer jedes echte Bauteil scharf. */
		private static bool IstBodenschatten(Component teil)
		{
			for (Transform t = teil.transform; t != null; t = t.parent)
			{
				if (t.name == WorldContactShadowBuilder.ChildName)
				{
					return true;
				}
			}
			return false;
		}

		private static IEnumerable<MeshFilter> Weltgeometrie(GameObject prefab)
		{
			foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true))
			{
				if (!IstBodenschatten(filter))
				{
					yield return filter;
				}
			}
		}

		private static IEnumerable<MeshRenderer> Weltrenderer(GameObject prefab)
		{
			foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
			{
				if (!IstBodenschatten(renderer))
				{
					yield return renderer;
				}
			}
		}

		private static bool IstFreigegebenesMidpoly(GameObject prefab)
		{
			string[] labels = AssetDatabase.GetLabels(prefab);
			return labels.Contains("MidPoly") && labels.Contains("Approved");
		}

		private static void PruefeFreigegebenesMidpoly(GameObject prefab, string name)
		{
			LODGroup group = prefab.GetComponentInChildren<LODGroup>(includeInactive: true);
			if (group == null)
			{
				Assert.That(name, Does.EndWith("_Exhausted"), name + " ohne LODGroup");
				Renderer[] exhaustedRenderers = Weltrenderer(prefab).Cast<Renderer>().ToArray();
				Assert.That(exhaustedRenderers, Is.Not.Empty, name + " ohne Renderer");
				Assert.That(PruefeMidpolyRenderer(exhaustedRenderers, name + "/LOD0"), Is.GreaterThan(0));
				Assert.That(prefab.GetComponentsInChildren<SpriteRenderer>(includeInactive: true), Is.Empty,
					name + " darf keinen 2D-Platzhalter enthalten");
				return;
			}
			LOD[] lods = group.GetLODs();
			Assert.That(lods, Has.Length.EqualTo(3), name + " braucht LOD0/1/2");
			long previousTriangles = long.MaxValue;
			for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
			{
				Renderer[] renderers = lods[lodIndex].renderers.Where(renderer => renderer != null).ToArray();
				Assert.That(renderers, Is.Not.Empty, name + "/LOD" + lodIndex + " ohne Renderer");
				long triangles = PruefeMidpolyRenderer(renderers, name + "/LOD" + lodIndex);
				Assert.That(triangles, Is.GreaterThan(0), name + "/LOD" + lodIndex + " ohne Geometrie");
				Assert.That(triangles, Is.LessThan(previousTriangles), name + "/LOD" + lodIndex + " reduziert die Geometrie nicht");
				previousTriangles = triangles;
			}
			Assert.That(prefab.GetComponentsInChildren<SpriteRenderer>(includeInactive: true), Is.Empty,
				name + " darf keinen 2D-Platzhalter enthalten");
		}

		private static long PruefeMidpolyRenderer(IEnumerable<Renderer> renderers, string context)
		{
			long triangles = 0;
			foreach (Renderer renderer in renderers)
			{
				Assert.That(renderer.sharedMaterials, Is.Not.Empty, context + "/" + renderer.name + " ohne Materialslots");
				Assert.That(renderer.sharedMaterials.All(material => material != null), Is.True,
					context + "/" + renderer.name + " mit fehlendem Material");
				Mesh mesh = renderer is SkinnedMeshRenderer skinned
					? skinned.sharedMesh
					: renderer.GetComponent<MeshFilter>()?.sharedMesh;
				Assert.That(mesh, Is.Not.Null, context + "/" + renderer.name + " ohne Mesh");
				for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++) triangles += (long)mesh.GetIndexCount(subMesh) / 3L;
			}
			return triangles;
		}

		[Test]
		public void WeltShader_ExistiertUndHatTintProperty()
		{
			Shader shader = Shader.Find(EidrenWorldStyleAssets.ShaderName);
			Assert.That(shader, Is.Not.Null, "Shader Eidren/World/VertexLit fehlt");
			Material material = new Material(shader);
			Assert.That(material.HasProperty("_Tint"), Is.True, "_Tint-Property fehlt");
			Object.DestroyImmediate(material);
		}

		[Test]
		public void WeltMaterial_WirdErzeugtUndNutztDenShader()
		{
			Material material = EidrenWorldStyleAssets.EnsureWorldMaterial();
			Assert.That(material, Is.Not.Null);
			Assert.That(material.shader.name, Is.EqualTo(EidrenWorldStyleAssets.ShaderName));
		}

		[TestCase("WorldChest_Common", 1.15f, 0.54f, 0.72f)]
		[TestCase("WorldChest_Guarded", 1.45f, 0.7f, 0.88f)]
		[TestCase("WorldChest_Hidden", 1f, 0.48f, 0.64f)]
		public void Weltkiste_IstImVertexfarbenStil(string name, float breite, float hoehe, float tiefe)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Loot/WorldChests/" + name + ".prefab");
			Assert.That(prefab, Is.Not.Null, name);
			bool midpoly = MidpolyWorldChestMigration.IsApprovedWorldChest(name);
			MeshFilter[] filters;
			if (midpoly)
			{
				LODGroup group = prefab.GetComponent<LODGroup>();
				Assert.That(group, Is.Not.Null, name + " ohne LODGroup");
				Assert.That(group.GetLODs(), Has.Length.EqualTo(3), name);
				filters = group.GetLODs()[0].renderers.Select(renderer => renderer.GetComponent<MeshFilter>())
					.Where(filter => filter != null).Distinct().ToArray();
			}
			else
			{
				filters = Weltgeometrie(prefab).ToArray();
			}
			int dreiecke = 0;
			foreach (MeshFilter filter in filters)
			{
				if (!midpoly)
				{
					Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
						name + "/" + filter.name + " ohne Vertexfarben");
				}
				dreiecke += filter.sharedMesh.triangles.Length / 3;
			}
			int korridor = name == "WorldChest_Guarded" ? 10000 : 9000;
			Assert.That(dreiecke, Is.LessThanOrEqualTo(midpoly ? korridor : 400), name + " ueberschreitet den Dreieckskorridor");
			IEnumerable<MeshRenderer> materialRenderers = midpoly
				? prefab.GetComponent<LODGroup>().GetLODs()[0].renderers.OfType<MeshRenderer>()
				: Weltrenderer(prefab);
			foreach (MeshRenderer renderer in materialRenderers)
			{
				if (midpoly)
				{
					Assert.That(renderer.sharedMaterial.name, Does.StartWith("MP_WorldChest_"),
						name + "/" + renderer.name + " nutzt nicht das Phase-4-Weltkistenmaterial");
				}
				else
				{
					Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
						name + "/" + renderer.name + " nutzt nicht das gemeinsame Material");
				}
			}
			Assert.That(prefab.transform.Find("LootFill_Full"), Is.Not.Null, name + " ohne LootFill_Full");
			Assert.That(prefab.transform.Find("LootFill_Partial"), Is.Not.Null, name + " ohne LootFill_Partial");
			BoxCollider collider = prefab.GetComponent<BoxCollider>();
			Assert.That(collider.size, Is.EqualTo(new Vector3(breite, hoehe, tiefe)), name + " Collider veraendert");
			foreach (string teil in new[] { "CarvedBase", "LidPivot", "ClosedDetails", "OpenedDetails", "EmptiedDetails" })
			{
				Assert.That(prefab.transform.Find(teil), Is.Not.Null, name + " Teil fehlt: " + teil);
			}
		}

		[TestCase("SupplyChest", 1.18f, 0.8f, 0.78f)]
		[TestCase("OptionalChest", 1.25f, 0.8f, 0.82f)]
		[TestCase("EliteChest", 1.42f, 1f, 0.92f)]
		[TestCase("CompletionChest", 1.62f, 1.2f, 1.02f)]
		[TestCase("SmallRewardChest", 0.95f, 0.7f, 0.68f)]
		[TestCase("MediumRewardChest", 1.2f, 0.9f, 0.82f)]
		[TestCase("LargeRewardChest", 1.55f, 1.2f, 1.02f)]
		[TestCase("RecoveryContainer", 1.45f, 0.8f, 0.82f)]
		public void ForgeKiste_IstImVertexfarbenStil(string name, float breite, float hoehe, float tiefe)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Containers/Forge/" + name + "_2D.prefab");
			Assert.That(prefab, Is.Not.Null, name);
			if (Eidren.Editor.MidpolyForgeContainerMigration.IsApprovedContainer(name))
			{
				LODGroup group = prefab.GetComponent<LODGroup>();
				Assert.That(group, Is.Not.Null, name + " ohne LODGroup");
				Assert.That(group.GetLODs(), Has.Length.EqualTo(3), name + " LOD-Vertrag");
				int lod0Dreiecke = group.GetLODs()[0].renderers
					.Select(renderer => renderer.GetComponent<MeshFilter>())
					.Where(filter => filter != null && filter.sharedMesh != null).Distinct()
					.Sum(filter => filter.sharedMesh.triangles.Length / 3);
				Assert.That(lod0Dreiecke, Is.InRange(7000, 12000), name + " Mid-Poly-Korridor");
				Assert.That(group.GetLODs()[0].renderers.SelectMany(renderer => renderer.sharedMaterials)
					.Where(material => material != null)
					.All(material => material.name.StartsWith("MP_ForgeContainer_", System.StringComparison.Ordinal)),
					Is.True, name + " nutzt ein fremdes Produktionsmaterial");
			}
			else
			{
			int dreiecke = 0;
			foreach (MeshFilter filter in Weltgeometrie(prefab))
			{
				Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
					name + "/" + filter.name + " ohne Vertexfarben");
				dreiecke += filter.sharedMesh.triangles.Length / 3;
			}
			Assert.That(dreiecke, Is.LessThanOrEqualTo(500), name + " ueberschreitet den Dreieckskorridor");
			foreach (MeshRenderer renderer in Weltrenderer(prefab))
			{
				Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
					name + "/" + renderer.name + " nutzt nicht das gemeinsame Material");
			}
			}
			Assert.That(prefab.transform.Find("LootFill_Full"), Is.Not.Null, name + " ohne LootFill_Full");
			Assert.That(prefab.transform.Find("LootFill_Partial"), Is.Not.Null, name + " ohne LootFill_Partial");
			BoxCollider collider = prefab.GetComponent<BoxCollider>();
			Assert.That(collider.size, Is.EqualTo(new Vector3(breite, hoehe, tiefe)), name + " Collider veraendert");
		}

		[TestCase("HardwoodTree_Active", 700)]
		[TestCase("HardwoodTree_Exhausted", 500)]
		[TestCase("SwampHemp_Active", 500)]
		[TestCase("SwampHemp_Exhausted", 500)]
		public void T2Visual_IstImVertexfarbenStil(string name, int korridor)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/" + name + ".prefab");
			Assert.That(prefab, Is.Not.Null, name);
			if (IstFreigegebenesMidpoly(prefab))
			{
				PruefeFreigegebenesMidpoly(prefab, name);
			}
			else
			{
				int dreiecke = 0;
				foreach (MeshFilter filter in Weltgeometrie(prefab))
				{
					Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
						name + "/" + filter.name + " ohne Vertexfarben");
					dreiecke += filter.sharedMesh.triangles.Length / 3;
				}
				Assert.That(dreiecke, Is.LessThanOrEqualTo(korridor), name + " ueberschreitet den Dreieckskorridor");
				foreach (MeshRenderer renderer in Weltrenderer(prefab))
				{
					Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
						name + "/" + renderer.name + " nutzt nicht das gemeinsame Material");
				}
			}
			Assert.That(prefab.GetComponentsInChildren<Collider>(includeInactive: true), Is.Empty,
				name + " Visual darf keine Collider tragen");
		}

		[TestCase("Tree_Active", 800)]
		[TestCase("Tree_Exhausted", 500)]
		[TestCase("BerryBush_Active", 500)]
		[TestCase("BerryBush_Exhausted", 500)]
		[TestCase("FiberPlant_Active", 500)]
		[TestCase("FiberPlant_Exhausted", 500)]
		[TestCase("StoneDeposit_Active", 500)]
		[TestCase("StoneDeposit_Exhausted", 500)]
		[TestCase("CopperVein_Active", 500)]
		[TestCase("CopperVein_Exhausted", 500)]
		public void T1Visual_IstImVertexfarbenStil(string name, int korridor)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/" + name + ".prefab");
			Assert.That(prefab, Is.Not.Null, name);
			if (IstFreigegebenesMidpoly(prefab))
			{
				PruefeFreigegebenesMidpoly(prefab, name);
			}
			else
			{
				int dreiecke = 0;
				foreach (MeshFilter filter in Weltgeometrie(prefab))
				{
					Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
						name + "/" + filter.name + " ohne Vertexfarben");
					dreiecke += filter.sharedMesh.triangles.Length / 3;
				}
				Assert.That(dreiecke, Is.LessThanOrEqualTo(korridor), name + " ueberschreitet den Dreieckskorridor");
				foreach (MeshRenderer renderer in Weltrenderer(prefab))
				{
					Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
						name + "/" + renderer.name + " nutzt nicht das gemeinsame Material");
				}
			}
			Assert.That(prefab.GetComponentsInChildren<Collider>(includeInactive: true), Is.Empty,
				name + " Visual darf keine Collider tragen");
		}

		/* Collider-Erwartung je Prop aus STILUMBAU_E4_BAKEKETTE.md (c): die 9 Collider-Props
		   behalten ihren BoxCollider exakt (Groesse+Center aus VisualScaleTableBuilder.ColliderSize),
		   die uebrigen 7 sind colliderfrei. */
		[TestCase("SP_Tree_A", 500, true, 0.9f, 3f, 0.9f, 0f, 1.5f, 0f)]
		[TestCase("SP_Tree_B", 500, true, 0.8f, 2.4f, 0.8f, 0f, 1.2f, 0f)]
		[TestCase("SP_Tree_C", 500, true, 0.7f, 1.6f, 0.7f, 0f, 0.8f, 0f)]
		[TestCase("SP_Rock_Small", 500, true, 0.8f, 0.4f, 0.8f, 0f, 0.2f, 0f)]
		[TestCase("SP_Rock_Medium", 500, true, 1.3f, 0.8f, 1.3f, 0f, 0.4f, 0f)]
		[TestCase("SP_Rock_Large", 500, true, 2.2f, 1.8f, 2.2f, 0f, 0.9f, 0f)]
		[TestCase("SP_RuinWall_A", 500, true, 2.6f, 2.2f, 0.8f, 0f, 1.1f, 0f)]
		[TestCase("SP_RuinWall_B", 500, true, 2f, 2.2f, 0.8f, 0f, 1.1f, 0f)]
		[TestCase("SP_RuinMonument", 500, true, 1.4f, 3f, 1.4f, 0f, 1.5f, 0f)]
		[TestCase("SP_Plant_Bush", 500, false, 0f, 0f, 0f, 0f, 0f, 0f)]
		[TestCase("SP_Plant_Fern", 500, false, 0f, 0f, 0f, 0f, 0f, 0f)]
		[TestCase("SP_Plant_Flowers", 500, false, 0f, 0f, 0f, 0f, 0f, 0f)]
		[TestCase("SP_GroundCover_Grass", 500, false, 0f, 0f, 0f, 0f, 0f, 0f)]
		[TestCase("SP_GroundCover_Moss", 500, false, 0f, 0f, 0f, 0f, 0f, 0f)]
		[TestCase("SP_Accent_GlowMushrooms", 500, false, 0f, 0f, 0f, 0f, 0f, 0f)]
		[TestCase("SP_EidrenRune", 500, false, 0f, 0f, 0f, 0f, 0f, 0f)]
		public void Prop_IstImVertexfarbenStil(string name, int korridor, bool hatCollider, float groesseX, float groesseY, float groesseZ, float centerX, float centerY, float centerZ)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Environment/StyleProof/" + name + ".prefab");
			Assert.That(prefab, Is.Not.Null, name);
			if (IstFreigegebenesMidpoly(prefab))
			{
				PruefeFreigegebenesMidpoly(prefab, name);
			}
			else
			{
				int dreiecke = 0;
				foreach (MeshFilter filter in Weltgeometrie(prefab))
				{
					Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
						name + "/" + filter.name + " ohne Vertexfarben");
					dreiecke += filter.sharedMesh.triangles.Length / 3;
				}
				Assert.That(dreiecke, Is.LessThanOrEqualTo(korridor), name + " ueberschreitet den Dreieckskorridor");
				foreach (MeshRenderer renderer in Weltrenderer(prefab))
				{
					Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
						name + "/" + renderer.name + " nutzt nicht das gemeinsame Material");
				}
			}
			BoxCollider collider = prefab.GetComponent<BoxCollider>();
			if (hatCollider)
			{
				Assert.That(collider, Is.Not.Null, name + " Collider fehlt");
				Assert.That(collider.size, Is.EqualTo(new Vector3(groesseX, groesseY, groesseZ)), name + " Collider-Groesse veraendert");
				Assert.That(collider.center, Is.EqualTo(new Vector3(centerX, centerY, centerZ)), name + " Collider-Center veraendert");
			}
			else
			{
				Assert.That(prefab.GetComponentsInChildren<Collider>(includeInactive: true), Is.Empty,
					name + " Prop darf keinen Collider tragen");
			}
		}

		/* Stilumbau E5, Task 3+4: die 9 BLD_-Gebaeude, Muster wie Prop_IstImVertexfarbenStil, aber
		   das Geometry_A14-Kind wird gemessen (nicht die Wurzel) und der BoxCollider sitzt an der
		   Wurzel, nicht auf einem Prop-Root ohne Kinder. Korridor- und Collider-Werte aus
		   STILUMBAU_E5_BAKEKETTE.md (c) bzw. dem gemessenen Fabrik-Ertrag (Task-3+4-Report, mit
		   Sicherheitsabstand). */
		[TestCase("Wall", 4000, 1f, 2.6f, 0.2f, 0f, 1.3f, 0f)]
		[TestCase("Door", 4000, 1f, 2.6f, 0.2f, 0f, 1.3f, 0f)]
		[TestCase("Floor", 3200, 1f, 0.15f, 1f, 0f, 0.075f, 0f)]
		[TestCase("FarmPlot", 1800, 2f, 0.15f, 2f, 0f, 0.075f, 0f)]
		[TestCase("CookingPot", 6000, 1f, 1f, 1f, 0f, 0.5f, 0f)]
		[TestCase("Smelter", 5200, 1f, 2f, 1f, 0f, 1f, 0f)]
		[TestCase("Sawmill", 3800, 1f, 2.2f, 1f, 0f, 1.1f, 0f)]
		[TestCase("Stonecutter", 3300, 1f, 1.4f, 1f, 0f, 0.7f, 0f)]
		[TestCase("Ropewalk", 6200, 1f, 1.6f, 1f, 0f, 0.8f, 0f)]
		public void Gebaeude_IstImVertexfarbenStil(string name, int korridor, float groesseX, float groesseY, float groesseZ, float centerX, float centerY, float centerZ)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Buildings/Level01/BLD_" + name + "_L01.prefab");
			Assert.That(prefab, Is.Not.Null, name);
			Transform geometrie = prefab.transform.Find("Geometry_A14");
			Assert.That(geometrie, Is.Not.Null, name + " ohne Geometry_A14");
			bool baseModuleMidpoly = MidpolyBaseModuleMigration.IsApprovedModule(name);
			bool stationMidpoly = MidpolyProductionStationMigration.IsApprovedStation(name);
			bool midpoly = baseModuleMidpoly || stationMidpoly;
			MeshFilter[] filters;
			if (midpoly)
			{
				LODGroup group = prefab.GetComponent<LODGroup>();
				Assert.That(group, Is.Not.Null, name + " ohne LODGroup");
				Assert.That(group.GetLODs(), Has.Length.EqualTo(3), name);
				filters = group.GetLODs()[0].renderers.Select(renderer => renderer.GetComponent<MeshFilter>())
					.Where(filter => filter != null).Distinct().ToArray();
			}
			else
			{
				filters = geometrie.GetComponentsInChildren<MeshFilter>(includeInactive: true);
			}
			int dreiecke = 0;
			foreach (MeshFilter filter in filters)
			{
				if (!midpoly)
				{
					Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
						name + "/" + filter.name + " ohne Vertexfarben");
				}
				dreiecke += filter.sharedMesh.triangles.Length / 3;
			}
			Assert.That(dreiecke, Is.LessThanOrEqualTo(korridor), name + " ueberschreitet den Dreieckskorridor");
			IEnumerable<MeshRenderer> materialRenderers = midpoly
				? prefab.GetComponent<LODGroup>().GetLODs()[0].renderers.OfType<MeshRenderer>()
				: geometrie.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
			foreach (MeshRenderer renderer in materialRenderers)
			{
				if (midpoly)
				{
					Assert.That(renderer.sharedMaterial.name,
						Does.StartWith(baseModuleMidpoly ? "MP_BaseModule_" : "MP_Production_"),
						name + "/" + renderer.name + " nutzt nicht das Phase-4-Kitmaterial");
				}
				else
				{
					Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
						name + "/" + renderer.name + " nutzt nicht das gemeinsame Material");
				}
			}
			BoxCollider collider = prefab.GetComponent<BoxCollider>();
			Assert.That(collider, Is.Not.Null, name + " Wurzel-Collider fehlt");
			Assert.That(collider.size, Is.EqualTo(new Vector3(groesseX, groesseY, groesseZ)), name + " Collider-Groesse veraendert");
			Assert.That(collider.center, Is.EqualTo(new Vector3(centerX, centerY, centerZ)), name + " Collider-Center veraendert");
		}
	}
}
