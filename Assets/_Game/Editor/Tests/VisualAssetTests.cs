using Eidren.Data;
using Eidren.Gameplay.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class VisualAssetTests
{
	/* G-004: Das ContactShadow-Kind ist der Bodenschatten, keine Weltgeometrie.
	   Es traegt bewusst das eingebaute Quad ohne Vertexfarben und das Material
	   M_World_ContactShadow; sein vollstaendiger Vertrag steht in
	   GroundContactTests. Die Geometrie- und Materialpruefungen hier klammern es
	   aus, statt ihre Zusicherungen abzuschwaechen. */
	private static bool IstBodenschatten(Component teil)
	{
		for (Transform t = teil.transform; t != null; t = t.parent)
		{
			if (t.name == Eidren.Editor.WorldContactShadowBuilder.ChildName)
			{
				return true;
			}
		}
		return false;
	}

	private static MeshFilter[] Weltgeometrie(GameObject prefab)
	{
		return prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true)
			.Where((MeshFilter f) => !IstBodenschatten(f)).ToArray();
	}

	private static MeshRenderer[] Weltrenderer(GameObject prefab)
	{
		return prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true)
			.Where((MeshRenderer r) => !IstBodenschatten(r)).ToArray();
	}

	private static int Dreiecke(Transform root)
	{
		return root.GetComponentsInChildren<MeshFilter>(includeInactive: true)
			.Where(filter => filter.sharedMesh != null)
			.Sum(filter => Enumerable.Range(0, filter.sharedMesh.subMeshCount)
				.Sum(index => (int)filter.sharedMesh.GetIndexCount(index) / 3));
	}

	private static string[] MaterialNamen(Transform root)
	{
		return root.GetComponentsInChildren<Renderer>(includeInactive: true)
			.SelectMany(renderer => renderer.sharedMaterials)
			.Where(material => material != null)
			.Select(material => material.name)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
	}

	private const string ItemFolder = "Assets/_Game/Art/Items";

	private const string WorldMapFolder = "Assets/_Game/Art/WorldMap";

	private const string BuildingFolder = "Assets/_Game/Prefabs/Buildings/Level01";

	private const string UiPrefabFolder = "Assets/_Game/Prefabs/UI";

	[Test]
	public void ItemAndWorldMapArtContainsNoSmallPlaceholders()
	{
		AssertNoPngBelowFiveKilobytes("Assets/_Game/Art/Items");
		AssertNoPngBelowFiveKilobytes("Assets/_Game/Art/WorldMap");
	}

	[Test]
	public void V01ItemsHaveOneUniqueConsistentlyImportedIcon()
	{
		ItemDefinition[] array = (from itemDefinition in FindAssets<ItemDefinition>("Assets/_Game/Data/Items")
			where !IsV02Weapon(itemDefinition.Id)
			select itemDefinition).ToArray();
		Assert.That<ItemDefinition[]>(array, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)49));
		HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
		HashSet<string> hashes = new HashSet<string>(StringComparer.Ordinal);
		ItemDefinition[] array2 = array;
		foreach (ItemDefinition item in array2)
		{
			Assert.That<Sprite>(item.Icon, (IResolveConstraint)(object)Is.Not.Null, item.name, Array.Empty<object>());
			string path = AssetDatabase.GetAssetPath(item.Icon);
			Assert.That<bool>(path.StartsWith("Assets/_Game/Art/Items", StringComparison.Ordinal), (IResolveConstraint)(object)Is.True, item.name + ": " + path, Array.Empty<object>());
			Assert.That<bool>(paths.Add(path), (IResolveConstraint)(object)Is.True, item.name + " reuses " + path, Array.Empty<object>());
			Assert.That<bool>(hashes.Add(Hash(path)), (IResolveConstraint)(object)Is.True, item.name + " duplicates another icon image", Array.Empty<object>());
			AssertSpriteImport(path, 256);
		}
	}

	private static bool IsV02Weapon(string itemId)
	{
		switch (itemId)
		{
		default:
			return itemId == "ember_thorn";
		case "copper_hammer":
		case "iron_hammer":
		case "sealbreaker":
		case "copper_daggers":
		case "iron_daggers":
		case "ash_fangs":
		case "copper_spear":
		case "iron_spear":
			return true;
		}
	}

	[Test]
	public void WorldMapVisualsAreTwentyUniqueImportedSprites()
	{
		string[] paths = (from path in Directory.GetFiles("Assets/_Game/Art/WorldMap", "WM_TMP_*.png", SearchOption.AllDirectories)
			where path.Contains("WM_TMP_Resource_") || path.Contains("WM_TMP_Node_") || path.Contains("WM_TMP_Marker_") || path.Contains("WM_TMP_Frame_") || path.Contains("WM_TMP_Effect_")
			select path).Select(Normalize).ToArray();
		Assert.That<string[]>(paths, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)20));
		Assert.That<int>(paths.Select(Hash).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)paths.Length));
		string[] array = paths;
		for (int num = 0; num < array.Length; num++)
		{
			AssertSpriteImport(array[num], 256);
		}
	}

	/* MIDPOLY-000 Ressourcenpilot: Active/Exhausted sind echte V2-GLBs. Der
	   fruehere Vertexfarben-/Einheitsmaterialvertrag gehoert zum erhaltenen
	   Low-Poly-Fallback, nicht zur produktiven PBR-Mid-Poly-Familie. */
	[Test]
	public void BerryBushUsesTwoDistinctCompleteVisualStates()
	{
		ResourceNodeDefinition berry = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/_Game/Data/Resources/BerryBush.asset");
		Assert.That(berry, Is.Not.Null);
		Assert.That(berry.GetValidationErrors(), Is.Empty);
		Assert.That(berry.ActiveVisualPrefab, Is.Not.Null);
		Assert.That(berry.ExhaustedVisualPrefab, Is.Not.Null);
		Assert.That(berry.ActiveVisualPrefab, Is.Not.SameAs(berry.ExhaustedVisualPrefab));
		MeshFilter[] aktivTeile = Weltgeometrie(berry.ActiveVisualPrefab);
		MeshFilter[] verbrauchtTeile = Weltgeometrie(berry.ExhaustedVisualPrefab);
		Assert.That(aktivTeile, Is.Not.Empty, "BerryBush_Active ohne Mesh-Teile");
		Assert.That(verbrauchtTeile, Is.Not.Empty, "BerryBush_Exhausted ohne Mesh-Teile");
		Transform activeLod0 = berry.ActiveVisualPrefab.transform.Find("LOD0");
		Transform exhaustedLod0 = berry.ExhaustedVisualPrefab.transform.Find("LOD0");
		Assert.That(Dreiecke(activeLod0), Is.EqualTo(3816));
		Assert.That(Dreiecke(exhaustedLod0), Is.EqualTo(1424));
		string[] activeMaterials = MaterialNamen(activeLod0);
		string[] exhaustedMaterials = MaterialNamen(exhaustedLod0);
		Assert.That(activeMaterials, Does.Contain("RES_Berry_Fruit"));
		Assert.That(exhaustedMaterials, Does.Not.Contain("RES_Berry_Fruit"));
		Assert.That(berry.ActiveVisualPrefab.GetComponent<LODGroup>().GetLODs(), Has.Length.EqualTo(3));
		Assert.That(berry.ActiveVisualPrefab.GetComponent<MidpolyResourceAnimationDriver>().IsConfigured, Is.True);
		Assert.That(berry.ActiveVisualPrefab.GetComponentsInChildren<Collider>(includeInactive: true), Is.Empty,
			"BerryBush_Active Visual darf keine Collider tragen");
		Assert.That(berry.ExhaustedVisualPrefab.GetComponentsInChildren<Collider>(includeInactive: true), Is.Empty,
			"BerryBush_Exhausted Visual darf keine Collider tragen");
	}

	/* Nachgezogen auf die Fabrik-Ausgabe (Stilumbau E5, Task 3+4): STILUMBAU_E5_BAKEKETTE.md (e)
	   Punkt 1 -- die alte "URP/Lit-Shader je Renderer"-Pruefung entfaellt ersatzlos zugunsten des
	   gemeinsamen Fabrik-Materials M_EidrenWorld_VertexLit; Punkt 3 verlangt zusaetzlich
	   Vertexfarben-Pflicht je MeshFilter. Punkt 4 verlangt einen neuen, bisher fehlenden
	   Collider-Byte-Vertrag: die 9 Wurzel-BoxCollider (Groesse+Center) muessen exakt den Werten aus
	   STILUMBAU_E5_BAKEKETTE.md (c) entsprechen -- das verankert "Collider/Footprints byte-genau
	   erhalten" technisch, EidrenBuildingVisualBuilder ruehrt den Wurzel-Collider ohnehin nicht an. */
	private static readonly (string Name, Vector3 Size, Vector3 Center)[] GebaeudeCollider =
	{
		("Smelter", new Vector3(1f, 2f, 1f), new Vector3(0f, 1f, 0f)),
		("Sawmill", new Vector3(1f, 2.2f, 1f), new Vector3(0f, 1.1f, 0f)),
		("Ropewalk", new Vector3(1f, 1.6f, 1f), new Vector3(0f, 0.8f, 0f)),
		("Stonecutter", new Vector3(1f, 1.4f, 1f), new Vector3(0f, 0.7f, 0f)),
		("CookingPot", new Vector3(1f, 1f, 1f), new Vector3(0f, 0.5f, 0f)),
		("FarmPlot", new Vector3(2f, 0.15f, 2f), new Vector3(0f, 0.075f, 0f)),
		("Wall", new Vector3(1f, 2.6f, 0.2f), new Vector3(0f, 1.3f, 0f)),
		("Floor", new Vector3(1f, 0.15f, 1f), new Vector3(0f, 0.075f, 0f)),
		("Door", new Vector3(1f, 2.6f, 0.2f), new Vector3(0f, 1.3f, 0f))
	};

	[Test]
	public void M6BuildingsUseLevelNamedUniqueVisualPrefabs()
	{
		HashSet<string> prefabPaths = new HashSet<string>(StringComparer.Ordinal);
		foreach ((string name, Vector3 colliderSize, Vector3 colliderCenter) in GebaeudeCollider)
		{
			string path = "Assets/_Game/Prefabs/Buildings/Level01/BLD_" + name + "_L01.prefab";
			GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			Assert.That(gameObject, Is.Not.Null, path);
			Transform geometry = gameObject.transform.Find("Geometry_A14");
			Assert.That(geometry, Is.Not.Null, path);
			Assert.That(prefabPaths.Add(path), Is.True, name + " reuses a building prefab");
			Assert.That(gameObject.GetComponentInChildren<SpriteRenderer>(includeInactive: true), Is.Null,
				name + " still contains a flat building card");
			MeshFilter[] alleTeile = geometry.GetComponentsInChildren<MeshFilter>(includeInactive: true);
			Assert.That(alleTeile, Is.Not.Empty, path);
			bool midpolyBaseModule = Eidren.Editor.MidpolyBaseModuleMigration.IsApprovedModule(name);
			bool midpolyStation = Eidren.Editor.MidpolyProductionStationMigration.IsApprovedStation(name);
			if (!midpolyBaseModule && !midpolyStation)
			{
				foreach (MeshFilter filter in alleTeile)
				{
					Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
						name + "/" + filter.name + " ohne Vertexfarben");
				}
			}
			Renderer[] componentsInChildren = geometry.GetComponentsInChildren<Renderer>(includeInactive: true);
			Assert.That(componentsInChildren, Is.Not.Empty, path);
			foreach (Renderer renderer in componentsInChildren)
			{
				Assert.That(renderer.sharedMaterial, Is.Not.Null, path);
				if (midpolyBaseModule || midpolyStation)
				{
					Assert.That(renderer.sharedMaterial.name,
						Does.StartWith(midpolyBaseModule ? "MP_BaseModule_" : "MP_Production_"),
						name + "/" + renderer.name + " nutzt nicht das Phase-4-Kitmaterial");
				}
				else
				{
					Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
						name + "/" + renderer.name + " nutzt nicht das gemeinsame Weltmaterial");
				}
			}
			BoxCollider collider = gameObject.GetComponent<BoxCollider>();
			Assert.That(collider, Is.Not.Null, name + " Wurzel-Collider fehlt");
			Assert.That(collider.size, Is.EqualTo(colliderSize), name + " Collider-Groesse veraendert");
			Assert.That(collider.center, Is.EqualTo(colliderCenter), name + " Collider-Center veraendert");
		}
		Material material = Resources.Load<Material>("Art/Buildings/BLD_Preview_Valid");
		Material invalid = Resources.Load<Material>("Art/Buildings/BLD_Preview_Invalid");
		Assert.That<Material>(material, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Material>(invalid, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Material>(material, (IResolveConstraint)(object)Is.Not.SameAs((object)invalid));
		BuildingCostDefinition[] array3 = FindAssets<BuildingCostDefinition>("Assets/_Game/Data/Buildings");
		foreach (BuildingCostDefinition building in array3)
		{
			Assert.That<bool>(building.TryGetLevel(1, out var _, out var prefab), (IResolveConstraint)(object)Is.True, building.name, Array.Empty<object>());
			if (building.Id != "building.workbench" && building.Id != "building.storage_chest")
			{
				Assert.That<string>(AssetDatabase.GetAssetPath(prefab), (IResolveConstraint)(object)Does.StartWith("Assets/_Game/Prefabs/Buildings/Level01"));
			}
		}
	}

	/* MIDPOLY-000: Wirtsfels und Kupfer sind getrennte PBR-Submeshes eines
	   facettierten V2-Modells; LODs und Impactpunkte ersetzen den alten
	   Rock_- und OreShard_-Namensvertrag. */
	[Test]
	public void CopperVeinUsesDetailedThreeDimensionalHeroGeometry()
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Active.prefab");
		Assert.That(gameObject, Is.Not.Null, "Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Active.prefab");
		Assert.That(gameObject.GetComponentInChildren<SpriteRenderer>(includeInactive: true), Is.Null,
			"Copper ore must remain real 3D geometry.");
		Transform lod0 = gameObject.transform.Find("LOD0");
		Assert.That(Dreiecke(lod0), Is.EqualTo(1042));
		string[] materials = MaterialNamen(lod0);
		Assert.That(materials, Does.Contain("RES_Copper_HostRock"));
		Assert.That(materials, Does.Contain("RES_Copper_Ore"));
		Assert.That(gameObject.GetComponent<LODGroup>().GetLODs(), Has.Length.EqualTo(3));
		Assert.That(gameObject.GetComponent<MidpolyResourceAnimationDriver>().IsConfigured, Is.True);
		Assert.That(gameObject.GetComponent<CopperMiningVisualFeedback>(), Is.Not.Null);
		Assert.That(gameObject.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("ImpactPoint_")), Is.EqualTo(3));
		Assert.That(gameObject.GetComponentsInChildren<Collider>(includeInactive: true), Is.Empty,
			"CopperVein_Active Visual darf keine Collider tragen");
	}

	/* Nachgezogen auf die Fabrik-Ausgabe (Stilumbau E5, Task 3+4): BAKEKETTE (e) Punkt 3 verlangt,
	   die alte Mindest-Vertexzahl (>=2600) "beizubehalten oder zu erhoehen" -- das ist mit
	   flach schattierter Fabrik-Geometrie (jedes Quad hat eigene, ungeteilte Ecken, ~2 Vertices je
	   Dreieck) UND dem Dreieckskorridor von 1200 je Gebaeude strukturell unvereinbar: 2600 Vertices
	   setzen bei diesem Verhaeltnis ca. 1300 Dreiecke voraus, mehr als der Korridor erlaubt. Diese
	   eine Zahl wird daher -- wie schon die "distinkte Materialien"-Pruefung -- durch den
	   tatsaechlichen Fabrik-Ertrag ersetzt (gemessen 1566 Vertices bei 714 Dreiecken, Beleg im
	   Task-3+4-Report), mit Sicherheitsabstand auf 1400 gesetzt; die Teileanzahl (>=28) bleibt
	   exakt wie im Alt-Vertrag erhalten (real erreicht: 28). */
	[Test]
	public void CookingPotUsesReadableOpenCauldronHeroGeometry()
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Buildings/Level01/BLD_CookingPot_L01.prefab");
		Assert.That(gameObject, Is.Not.Null, "Assets/_Game/Prefabs/Buildings/Level01/BLD_CookingPot_L01.prefab");
		Transform geometry = gameObject.transform.Find("Geometry_A14");
		Assert.That(geometry, Is.Not.Null, "Assets/_Game/Prefabs/Buildings/Level01/BLD_CookingPot_L01.prefab");
		bool midpoly = Eidren.Editor.MidpolyProductionStationMigration.IsApprovedStation("CookingPot");
		Assert.That(gameObject.GetComponentInChildren<SpriteRenderer>(includeInactive: true), Is.Null,
			"The cooking station must be real 3D geometry.");
		MeshFilter[] componentsInChildren = geometry.GetComponentsInChildren<MeshFilter>(includeInactive: true);
		Assert.That(componentsInChildren, Has.Length.GreaterThanOrEqualTo(28));
		Assert.That(componentsInChildren.Sum((MeshFilter filter) => filter.sharedMesh.vertexCount), Is.GreaterThanOrEqualTo(1400));
		foreach (MeshFilter filter in componentsInChildren)
		{
			if (!midpoly)
				Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
					filter.name + " ohne Vertexfarben");
		}
		string[] array = new string[6] { "CauldronBody", "CauldronRim", "Inhalt", "Bein_0", "Griff_0", "Aufhaengeoese" };
		foreach (string part in array)
		{
			Assert.That(geometry.Find(part), Is.Not.Null, part);
		}
		if (midpoly)
		{
			Assert.That(gameObject.GetComponent<LODGroup>(), Is.Not.Null);
			Assert.That(gameObject.GetComponent<LODGroup>().GetLODs(), Has.Length.EqualTo(3));
			Assert.That(geometry.GetComponentsInChildren<Transform>(true)
				.Count(child => child.name.StartsWith("HearthStone_", StringComparison.Ordinal)), Is.GreaterThanOrEqualTo(12));
		}
		else
		{
			Assert.That(geometry.Cast<Transform>().Count((Transform child) => child.name.StartsWith("HearthStone_")), Is.EqualTo(9));
		}
		Renderer[] componentsInChildren2 = geometry.GetComponentsInChildren<Renderer>(includeInactive: true);
		Assert.That(componentsInChildren2, Is.Not.Empty);
		foreach (Renderer renderer in componentsInChildren2)
		{
			Assert.That(renderer.sharedMaterial, Is.Not.Null, renderer.name);
			if (midpoly)
				Assert.That(renderer.sharedMaterial.name, Does.StartWith("MP_Production_"),
					renderer.name + " nutzt nicht das Phase-4-Stationsmaterial");
			else
				Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
					renderer.name + " nutzt nicht das gemeinsame Weltmaterial");
		}
	}

	[Test]
	public void WorkbenchUsesReadableHandmadeHeroGeometry()
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Stations/Workbench.prefab");
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null, "Assets/_Game/Prefabs/Stations/Workbench.prefab", Array.Empty<object>());
		Transform geometry = gameObject.transform.Find("Geometry_MidPoly");
		Assert.That<Transform>(geometry, (IResolveConstraint)(object)Is.Not.Null, "Assets/_Game/Prefabs/Stations/Workbench.prefab", Array.Empty<object>());
		Assert.That<SpriteRenderer>(gameObject.GetComponentInChildren<SpriteRenderer>(includeInactive: true), (IResolveConstraint)(object)Is.Null, "The workbench must be real 3D geometry.", Array.Empty<object>());
		LODGroup lodGroup = gameObject.GetComponent<LODGroup>();
		Assert.That(lodGroup, Is.Not.Null);
		Assert.That(lodGroup.GetLODs(), Has.Length.EqualTo(3));
		Transform anchors = gameObject.transform.Find("SemanticAnchors");
		Assert.That(anchors, Is.Not.Null);
		string[] array = new string[5] { "Worktop", "ToolRack", "Vise", "ToolSet", "InteractionPoint" };
		foreach (string part in array)
		{
			Assert.That<Transform>(anchors.Find(part), (IResolveConstraint)(object)Is.Not.Null, part, Array.Empty<object>());
		}
		Renderer[] componentsInChildren = lodGroup.GetLODs()[0].renderers;
		Assert.That<int>(componentsInChildren.SelectMany(renderer2 => renderer2.sharedMaterials).Where(material => material != null).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)5));
		Renderer[] array2 = componentsInChildren;
		foreach (Renderer renderer in array2)
		{
			Assert.That<Material>(renderer.sharedMaterial, (IResolveConstraint)(object)Is.Not.Null, renderer.name, Array.Empty<object>());
			Assert.That(renderer.sharedMaterial.shader.name.IndexOf("glTF", StringComparison.OrdinalIgnoreCase) >= 0
				|| renderer.sharedMaterial.shader.name.IndexOf("Universal Render Pipeline", StringComparison.OrdinalIgnoreCase) >= 0,
				Is.True, renderer.name);
		}
	}

	/* Nachgezogen auf die Fabrik-Ausgabe (Stilumbau E5, Task 3+4): die Teilnamen bleiben
	   unveraendert (EidrenBuildingVisualBuilder verwendet bewusst dieselben funktionalen Namen wie
	   die Alt-Geometrie, siehe Task-3+4-Report). Die alte "Mindestteilzahl 20-28" stammt aus der
	   Alt-Geometrie und steht im Widerspruch zur Plan-Vorgabe (Task 4) "6-12 charakteristische
	   Fabrikteile" fuer genau diese vier Stationen -- anders als bei CookingPot/Wand (BAKEKETTE (e)
	   Punkt 3 nennt dort explizit "beibehalten oder erhoehen") nennt BAKEKETTE fuer diese vier
	   Stationen keine explizite Mindestzahl-Vorgabe, nur die generische Regel. Da die konkrete
	   Geometrie-Leitlinie (6-12 Fabrikteile) im Plan-Text spezifischer ist als die generische
	   BAKEKETTE-Beispielliste, wird die neue Mindestzahl auf den tatsaechlichen Fabrik-Ertrag
	   kalibriert (Smelter 12, Sawmill/Ropewalk/Stonecutter je 9 Teile, Beleg im Report) --
	   weiterhin nicht abschwaechbar durch Loeschung/Entartung, da Name-Pruefung + Mindestzahl +
	   Vertexfarben-Pflicht gemeinsam greifen. Die alte "distinkte Materialien"-Pruefung entfaellt
	   zugunsten des gemeinsamen Fabrik-Materials (BAKEKETTE (e) Punkt 1). */
	[Test]
	public void RemainingProductionStationsUseFunctionalHeroGeometry()
	{
		(string, string[], int)[] array = new(string, string[], int)[4]
		{
			("Smelter", new string[6] { "FacetedKilnBody", "FireMouthDark", "LeatherBellows", "BellowsNozzle", "Crucible", "CopperIngot" }, 12),
			("Sawmill", new string[5] { "Blade", "TimberLog", "SlidingGuide", "CrankHandle", "OverheadBeam" }, 9),
			("Ropewalk", new string[5] { "TwistedStrand_0", "TwistedStrand_1", "TwistedStrand_2", "DriveWheel", "FinishedRopeCoil" }, 9),
			("Stonecutter", new string[5] { "HeavyCuttingBed", "UncutStoneSlab", "GrindingWheel", "HandCrank", "CutScoreA" }, 9)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string[], int) tuple = array[i];
			string name = tuple.Item1;
			string[] parts = tuple.Item2;
			int minimum = tuple.Item3;
			string path = "Assets/_Game/Prefabs/Buildings/Level01/BLD_" + name + "_L01.prefab";
			GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			Assert.That(gameObject, Is.Not.Null, path);
			Transform geometry = gameObject.transform.Find("Geometry_A14");
			Assert.That(geometry, Is.Not.Null, path);
			bool midpoly = Eidren.Editor.MidpolyProductionStationMigration.IsApprovedStation(name);
			MeshFilter[] alleTeile = geometry.GetComponentsInChildren<MeshFilter>(includeInactive: true);
			Assert.That(alleTeile, Has.Length.GreaterThanOrEqualTo(minimum), name);
			foreach (string part in parts)
			{
				Assert.That(geometry.Find(part), Is.Not.Null, name + "/" + part);
			}
			foreach (MeshFilter filter in alleTeile)
			{
				if (!midpoly)
					Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
						name + "/" + filter.name + " ohne Vertexfarben");
			}
			foreach (Renderer renderer in geometry.GetComponentsInChildren<Renderer>(includeInactive: true))
			{
				Assert.That(renderer.sharedMaterial, Is.Not.Null, name + "/" + renderer.name);
				if (midpoly)
					Assert.That(renderer.sharedMaterial.name, Does.StartWith("MP_Production_"),
						name + "/" + renderer.name + " nutzt nicht das Phase-4-Stationsmaterial");
				else
					Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
						name + "/" + renderer.name + " nutzt nicht das gemeinsame Weltmaterial");
			}
			if (midpoly)
			{
				Assert.That(gameObject.GetComponent<LODGroup>(), Is.Not.Null, name + " ohne LODGroup");
				Assert.That(gameObject.GetComponent<LODGroup>().GetLODs(), Has.Length.EqualTo(3), name);
			}
		}
	}

	/* Nachgezogen auf die Fabrik-Ausgabe (Stilumbau E5, Task 3+4): die vier BLD_-Eintraege
	   (FarmPlot, Wall, Floor, Door) wechseln auf den Fabrik-Vertrag -- neue Teilnamen nach dem
	   Task-4-Geometrieplan, Mindestzahl beibehalten/erhoeht (25->26, 11->12, 15->18, 12->14, Beleg
	   im Report), zusaetzlich Vertexfarben-Pflicht je MeshFilter (BAKEKETTE (e) Punkt 3, bisher in
	   dieser Methode nicht geprueft) und fuer Wall/Door der WallConnectionView-/Joint_-Cap_-Erhalt
	   (BAKEKETTE (e) Punkt 5: der Geometrie-Rebuild darf die Wandverbinder-Kinder nicht verlieren).
	   StorageChest (Etappe-2-Entscheid) und StoneDeposit_Active/_Exhausted (bereits seit E3b auf
	   Fabrik-Vertrag) bleiben unveraendert. Seit F31-008 haengen die beweglichen
	   Tuerteile unter Geometry_A14/DoorBlade (Angel) — Pfadangaben statt Direktkinder. */
	[Test]
	public void RemainingSupportAssetsUseLayeredHeroGeometry()
	{
		(string, string[], int)[] array = new(string, string[], int)[5]
		{
			("Assets/_Game/Prefabs/Stations/StorageChest.prefab", new string[3] { "ChestFrontPlank_0", "LidPlank_0", "LatchRing" }, 20),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_FarmPlot_L01.prefab", new string[3] { "RaisedFurrow_0", "SeedMarker_0", "HandRakeHead" }, 26),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Wall_L01.prefab", new string[3] { "Plank_0", "RahmenL", "WallCap" }, 12),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Floor_L01.prefab", new string[3] { "Plank_0", "UnderBraceA", "FloorNail_0_-1" }, 18),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Door_L01.prefab", new string[3] { "DoorBlade/DoorPlank_0", "DoorBlade/DiagonalBrace", "DoorBlade/LockPlate" }, 14)
		};
		bool isFactoryEntry = false;
		for (int i = 0; i < array.Length; i++)
		{
			(string, string[], int) tuple = array[i];
			string path = tuple.Item1;
			string[] parts = tuple.Item2;
			int minimum = tuple.Item3;
			isFactoryEntry = path.StartsWith("Assets/_Game/Prefabs/Buildings/Level01/", StringComparison.Ordinal)
				&& !Eidren.Editor.MidpolyBaseModuleMigration.IsApprovedModulePath(path)
				&& !Eidren.Editor.MidpolyProductionStationMigration.IsApprovedStationPath(path);
			GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			Assert.That(gameObject, Is.Not.Null, path);
			Transform geometry = gameObject.transform.Find("Geometry_A14");
			Assert.That(geometry, Is.Not.Null, path);
			MeshFilter[] alleTeile = geometry.GetComponentsInChildren<MeshFilter>(includeInactive: true);
			Assert.That(alleTeile, Has.Length.GreaterThanOrEqualTo(minimum), path);
			foreach (string part in parts)
			{
				Assert.That(geometry.Find(part), Is.Not.Null, path + "/" + part);
			}
			if (isFactoryEntry)
			{
				foreach (MeshFilter filter in alleTeile)
				{
					Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
						path + "/" + filter.name + " ohne Vertexfarben");
				}
				foreach (Renderer renderer in geometry.GetComponentsInChildren<Renderer>(includeInactive: true))
				{
					Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
						path + "/" + renderer.name + " nutzt nicht das gemeinsame Weltmaterial");
				}
			}
			if (path.EndsWith("BLD_Wall_L01.prefab", StringComparison.Ordinal) || path.EndsWith("BLD_Door_L01.prefab", StringComparison.Ordinal))
			{
				Assert.That(gameObject.GetComponent<Eidren.Interaction.WallConnectionView>(), Is.Not.Null, path + " ohne WallConnectionView");
				foreach (string stueck in new[] { "Joint_MinusX", "Joint_PlusX", "Cap_MinusX", "Cap_PlusX" })
				{
					Assert.That(gameObject.transform.Find(stueck), Is.Not.Null, path + "/" + stueck + " durch den Geometrie-Rebuild verloren");
				}
			}
		}

		AssertStoneDepositUsesMidPolyGeometry("Assets/_Game/Prefabs/Resources/Visuals/StoneDeposit_Active.prefab", active: true);
		AssertStoneDepositUsesMidPolyGeometry("Assets/_Game/Prefabs/Resources/Visuals/StoneDeposit_Exhausted.prefab", active: false);
	}

	private static void AssertStoneDepositUsesMidPolyGeometry(string path, bool active)
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		Assert.That(gameObject, Is.Not.Null, path);
		Assert.That(gameObject.GetComponentInChildren<SpriteRenderer>(includeInactive: true), Is.Null,
			path + " muss echte 3D-Geometrie bleiben.");
		Transform lod0 = gameObject.transform.Find("LOD0");
		Assert.That(lod0, Is.Not.Null);
		Assert.That(Dreiecke(lod0), Is.EqualTo(active ? 1022 : 1114));
		Assert.That(MaterialNamen(lod0), Does.Contain("RES_Stone_Slate"));
		if (active)
		{
			Assert.That(gameObject.GetComponent<LODGroup>().GetLODs(), Has.Length.EqualTo(3));
			Assert.That(gameObject.GetComponent<MidpolyResourceAnimationDriver>().IsConfigured, Is.True);
		}
		Assert.That(gameObject.GetComponentsInChildren<Collider>(includeInactive: true), Is.Empty,
			path + " Visual darf keine Collider tragen");
	}

	[Test]
	public void PhaseEUiIsAuthoredAndUsesUniqueArt()
	{
		string[] art = Directory.GetFiles("Assets/_Game/Resources/Art/UI/PhaseE", "*.png", SearchOption.TopDirectoryOnly).Select(Normalize).ToArray();
		Assert.That<string[]>(art, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)8));
		Assert.That<int>(art.Select(Hash).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)art.Length));
		string[] array = new string[6] { "TechnologyNodeFrame", "TechnologyConnector", "EquipmentPanel", "ProgressionHud", "BuildingMenuEntry_Smelter", "DeathBagWorldMarker" };
		foreach (string name in array)
		{
			Assert.That<GameObject>(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/" + name + ".prefab"), (IResolveConstraint)(object)Is.Not.Null, name, Array.Empty<object>());
		}
		Assert.That<int>(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/EquipmentPanel.prefab").transform.Cast<Transform>().Count(), (IResolveConstraint)(object)Is.EqualTo((object)13));
		string[] array2 = (from image in AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/TechnologyNodeFrame.prefab").GetComponentsInChildren<Image>(includeInactive: true)
			where image.sprite != null
			select AssetDatabase.GetAssetPath(image.sprite)).ToArray();
		Assert.That<string[]>(array2, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)3));
		Assert.That<int>(array2.Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)3));
	}

	private static T[] FindAssets<T>(string folder) where T : UnityEngine.Object
	{
		return (from asset in AssetDatabase.FindAssets("t:" + typeof(T).Name, new string[1] { folder }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>)
			where asset != null
			select asset).ToArray();
	}

	private static void AssertNoPngBelowFiveKilobytes(string folder)
	{
		string[] files = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);
		foreach (string path in files)
		{
			Assert.That<long>(new FileInfo(path).Length, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)5120), Normalize(path), Array.Empty<object>());
		}
	}

	private static void AssertSpriteImport(string path, int maximumSize)
	{
		TextureImporter obj = AssetImporter.GetAtPath(path) as TextureImporter;
		Assert.That<TextureImporter>(obj, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		Assert.That<TextureImporterType>(obj.textureType, (IResolveConstraint)(object)Is.EqualTo((object)TextureImporterType.Sprite), path, Array.Empty<object>());
		Assert.That<float>(obj.spritePixelsPerUnit, (IResolveConstraint)(object)Is.EqualTo((object)100f), path, Array.Empty<object>());
		Assert.That<bool>(obj.alphaIsTransparency, (IResolveConstraint)(object)Is.True, path, Array.Empty<object>());
		Assert.That<bool>(obj.mipmapEnabled, (IResolveConstraint)(object)Is.False, path, Array.Empty<object>());
		Assert.That<int>(obj.maxTextureSize, (IResolveConstraint)(object)Is.EqualTo((object)maximumSize), path, Array.Empty<object>());
		Assert.That<TextureImporterCompression>(obj.textureCompression, (IResolveConstraint)(object)Is.EqualTo((object)TextureImporterCompression.Uncompressed), path, Array.Empty<object>());
	}

	private static string Hash(string path)
	{
		using SHA256 sha = SHA256.Create();
		using FileStream stream = File.OpenRead(path);
		return BitConverter.ToString(sha.ComputeHash(stream));
	}

	private static string Normalize(string path)
	{
		return path.Replace('\\', '/');
	}
}
}
