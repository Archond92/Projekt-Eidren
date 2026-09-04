using Eidren.Core.BuildGrid;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class BuildGridOverlayTests
{
	private const string ResourceFolder = "Art/BuildGrid";

	private const int WindowCells = 25;

	private const int WindowEdges = 60;

	private const int WindowNodes = 36;

	private static readonly GridCellRect Wide = new GridCellRect(-20, -20, 20, 20);

	private static readonly IReadOnlyList<GridCellRect> NoZones = new List<GridCellRect>();

	private static readonly string[] GridPrefabs = new string[6] { "BLD_Grid_Area", "BLD_Grid_Zone", "BLD_Grid_Boundary", "BLD_Grid_Cell", "BLD_Grid_Edge", "BLD_Grid_Node" };

	private static readonly string[] GridMaterials = new string[5] { "BLD_Grid_Area", "BLD_Grid_Boundary", "BLD_Grid_Focus", "BLD_Grid_Blocked", "BLD_Grid_Conflict" };

	[Test]
	public void FloorSelection_EmphasisesCells_AndNoEdges()
	{
		GridOverlayPlan gridOverlayPlan = Build(in Wide, NoZones, new GridCoordinate(0, 0), BuildingPlacementKind.Floor, new GridOccupancy());
		Assert.That<int>(gridOverlayPlan.Cells.Count, (IResolveConstraint)(object)Is.EqualTo((object)25));
		Assert.That<IReadOnlyList<GridOverlayEdge>>(gridOverlayPlan.Edges, (IResolveConstraint)(object)Is.Empty);
		Assert.That<IReadOnlyList<GridNode>>(gridOverlayPlan.Nodes, (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void WallSelection_EmphasisesEdgesAndCorners_AndNoCells()
	{
		GridOverlayPlan gridOverlayPlan = Build(in Wide, NoZones, new GridCoordinate(0, 0), BuildingPlacementKind.Edge, new GridOccupancy());
		Assert.That<IReadOnlyList<GridOverlayCell>>(gridOverlayPlan.Cells, (IResolveConstraint)(object)Is.Empty);
		Assert.That<int>(gridOverlayPlan.Edges.Count, (IResolveConstraint)(object)Is.EqualTo((object)60));
		Assert.That<int>(gridOverlayPlan.Nodes.Count, (IResolveConstraint)(object)Is.EqualTo((object)36));
	}

	[Test]
	public void SharedEdgeOfTwoCells_YieldsExactlyOneMark()
	{
		GridOverlayPlan plan = Build(in Wide, NoZones, new GridCoordinate(0, 0), BuildingPlacementKind.Edge, new GridOccupancy());
		Assert.That<int>(plan.Edges.Select((GridOverlayEdge mark) => mark.Edge).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)plan.Edges.Count), "Gegenüberliegende Benennungen derselben Kante dürfen keine zweite Marke erzeugen.", Array.Empty<object>());
	}

	[Test]
	public void TheWindowStopsAtTheBuildBoundary()
	{
		GridCellRect area = new GridCellRect(0, 0, 10, 10);
		GridOverlayPlan gridOverlayPlan = Build(in area, NoZones, new GridCoordinate(0, 0), BuildingPlacementKind.Floor, new GridOccupancy());
		Assert.That<int>(gridOverlayPlan.Cells.Count, (IResolveConstraint)(object)Is.EqualTo((object)9), "Vom 5x5-Fenster liegen an der Ecke nur 3x3 Zellen in der Baufläche.", Array.Empty<object>());
		Assert.That<bool>(gridOverlayPlan.Cells.All((GridOverlayCell mark) => area.Contains(mark.Cell)), (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void ACellOccupiedOnTheSameLayer_IsAConflict()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridCoordinate taken = new GridCoordinate(1, 1);
		occupancy.TryOccupyCell(BuildingPlacementKind.Floor, taken, "floor-1");
		GridOverlayPlan plan = Build(in Wide, NoZones, new GridCoordinate(0, 0), BuildingPlacementKind.Floor, occupancy);
		Assert.That<GridOverlayState>(StateOf(plan, taken), (IResolveConstraint)(object)Is.EqualTo((object)GridOverlayState.Conflict));
		Assert.That<GridOverlayState>(StateOf(plan, new GridCoordinate(0, 0)), (IResolveConstraint)(object)Is.EqualTo((object)GridOverlayState.Focus));
	}

	[Test]
	public void ACellOccupiedOnAnotherLayer_IsNoConflict()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridCoordinate cell = new GridCoordinate(1, 1);
		occupancy.TryOccupyCell(BuildingPlacementKind.Floor, cell, "floor-1");
		Assert.That<GridOverlayState>(StateOf(Build(in Wide, NoZones, new GridCoordinate(0, 0), BuildingPlacementKind.Object, occupancy), cell), (IResolveConstraint)(object)Is.EqualTo((object)GridOverlayState.Focus), "Ein Gebäude darf auf einem Boden stehen — die Schichten sind getrennt (Abschnitt 2).", Array.Empty<object>());
	}

	[Test]
	public void AnOccupiedEdge_IsAConflict()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridEdge taken = GridEdge.From(new GridCoordinate(0, 0), GridDirection.East);
		occupancy.TryOccupyEdge(taken, "wall-1");
		Assert.That<GridOverlayState>(Build(in Wide, NoZones, new GridCoordinate(0, 0), BuildingPlacementKind.Edge, occupancy).Edges.Single((GridOverlayEdge mark) => mark.Edge == taken).State, (IResolveConstraint)(object)Is.EqualTo((object)GridOverlayState.Conflict));
	}

	[Test]
	public void ALockedZone_IsMarkedAsBlocked()
	{
		List<GridCellRect> zones = new List<GridCellRect>
		{
			new GridCellRect(1, 1, 2, 2)
		};
		GridOverlayPlan plan = Build(in Wide, zones, new GridCoordinate(0, 0), BuildingPlacementKind.Floor, new GridOccupancy());
		Assert.That<GridOverlayState>(StateOf(plan, new GridCoordinate(2, 2)), (IResolveConstraint)(object)Is.EqualTo((object)GridOverlayState.Blocked));
		Assert.That<GridOverlayState>(StateOf(plan, new GridCoordinate(0, 0)), (IResolveConstraint)(object)Is.EqualTo((object)GridOverlayState.Focus));
	}

	[Test]
	public void AnEdgeInsideALockedZone_IsBlocked_AtItsRimItIsNot()
	{
		List<GridCellRect> zones = new List<GridCellRect>
		{
			new GridCellRect(0, 0, 2, 2)
		};
		GridOverlayPlan plan = Build(in Wide, zones, new GridCoordinate(1, 1), BuildingPlacementKind.Edge, new GridOccupancy());
		Assert.That<GridOverlayState>(StateAt(plan, GridEdge.From(new GridCoordinate(0, 0), GridDirection.East)), (IResolveConstraint)(object)Is.EqualTo((object)GridOverlayState.Blocked), "Diese Kante liegt zwischen zwei gesperrten Zellen.", Array.Empty<object>());
		Assert.That<GridOverlayState>(StateAt(plan, GridEdge.From(new GridCoordinate(2, 0), GridDirection.East)), (IResolveConstraint)(object)Is.EqualTo((object)GridOverlayState.Focus), "Am Rand der Zone verläuft die Kante nur an ihr entlang.", Array.Empty<object>());
	}

	[Test]
	public void ClearedPlan_KeepsNothingFromTheRunBefore()
	{
		GridOverlayPlan gridOverlayPlan = Build(in Wide, NoZones, new GridCoordinate(0, 0), BuildingPlacementKind.Edge, new GridOccupancy());
		gridOverlayPlan.Clear();
		Assert.That<IReadOnlyList<GridOverlayCell>>(gridOverlayPlan.Cells, (IResolveConstraint)(object)Is.Empty);
		Assert.That<IReadOnlyList<GridOverlayEdge>>(gridOverlayPlan.Edges, (IResolveConstraint)(object)Is.Empty);
		Assert.That<IReadOnlyList<GridNode>>(gridOverlayPlan.Nodes, (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void RebuildingTwice_DoesNotAccumulate()
	{
		GridOverlayPlan plan = new GridOverlayPlan();
		GridOccupancy occupancy = new GridOccupancy();
		for (int run = 0; run < 3; run++)
		{
			plan.Build(in Wide, NoZones, new GridCoordinate(run, run), BuildingPlacementKind.Edge, occupancy);
		}
		Assert.That<int>(plan.Edges.Count, (IResolveConstraint)(object)Is.EqualTo((object)60));
		Assert.That<int>(plan.Nodes.Count, (IResolveConstraint)(object)Is.EqualTo((object)36));
	}

	[Test]
	public void OnlyFullyEnclosedCells_CountAsBuildable()
	{
		GridCellRect cells = BuildGridAreaProjection.Buildable(new BuildingPlacementArea(new Rect(-5f, -5f, 10f, 10f), new List<Rect>(), BuildGridOrigin.HomeBase));
		Assert.That<int>(cells.MinX, (IResolveConstraint)(object)Is.EqualTo((object)(-4)));
		Assert.That<int>(cells.MaxX, (IResolveConstraint)(object)Is.EqualTo((object)4));
		Assert.That<int>(cells.MinZ, (IResolveConstraint)(object)Is.EqualTo((object)(-4)));
		Assert.That<int>(cells.MaxZ, (IResolveConstraint)(object)Is.EqualTo((object)4));
	}

	[Test]
	public void ATinyBuildArea_YieldsNoCellAtAll()
	{
		Assert.That<bool>(BuildGridAreaProjection.Buildable(new BuildingPlacementArea(new Rect(0f, 0f, 0.5f, 0.5f), new List<Rect>(), BuildGridOrigin.HomeBase)).IsEmpty, (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void ALockedZone_CoversEveryCellItTouches_AndNoNeighbour()
	{
		BuildingPlacementArea area = new BuildingPlacementArea(new Rect(-20f, -20f, 40f, 40f), new List<Rect>
		{
			new Rect(-1f, -1f, 2f, 2f)
		}, BuildGridOrigin.HomeBase);
		List<GridCellRect> zones = new List<GridCellRect>();
		BuildGridAreaProjection.Blocked(in area, zones);
		Assert.That<int>(zones.Count, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(zones[0].MinX, (IResolveConstraint)(object)Is.EqualTo((object)(-1)));
		Assert.That<int>(zones[0].MaxX, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(zones[0].Contains(new GridCoordinate(2, 0)), (IResolveConstraint)(object)Is.False, "Die Nachbarzelle berührt die Zone nur und bleibt baubar.", Array.Empty<object>());
	}

	[Test]
	public void TheBuildAreaOfTheHomeBase_IsNotShiftedByTheOrigin()
	{
		GridCellRect cells = BuildGridAreaProjection.Buildable(new BuildingPlacementArea(new Rect(95f, 45f, 10f, 10f), new List<Rect>(), new BuildGridOrigin(100f, 50f)));
		Assert.That<int>(cells.MinX, (IResolveConstraint)(object)Is.EqualTo((object)(-4)));
		Assert.That<int>(cells.MaxX, (IResolveConstraint)(object)Is.EqualTo((object)4));
	}

	[Test]
	public void EveryGridPrefab_IsAuthoredAndLoadable()
	{
		string[] gridPrefabs = GridPrefabs;
		foreach (string assetName in gridPrefabs)
		{
			GameObject gameObject = Resources.Load<GameObject>("Art/BuildGrid/" + assetName);
			Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null, "Rasterprefab fehlt: Art/BuildGrid/" + assetName, Array.Empty<object>());
			BuildGridMark component = gameObject.GetComponent<BuildGridMark>();
			Assert.That<BuildGridMark>(component, (IResolveConstraint)(object)Is.Not.Null, "'" + assetName + "' hat keine BuildGridMark-Komponente.", Array.Empty<object>());
			Assert.That<int>(component.Pieces.Length, (IResolveConstraint)(object)Is.GreaterThan((object)0), "'" + assetName + "' hat keine autorierten Teile.", Array.Empty<object>());
		}
	}

	[Test]
	public void NoGridPrefab_CarriesAColliderOrObstacle()
	{
		string[] gridPrefabs = GridPrefabs;
		foreach (string assetName in gridPrefabs)
		{
			GameObject gameObject = Resources.Load<GameObject>("Art/BuildGrid/" + assetName);
			Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<Collider>(gameObject.GetComponentInChildren<Collider>(includeInactive: true), (IResolveConstraint)(object)Is.Null, "'" + assetName + "' darf kein Raycast-Ziel erzeugen (Abschnitt 12).", Array.Empty<object>());
		}
	}

	[Test]
	public void GridSizesAreAuthoredInThePrefab()
	{
		AssertPlateSize("BLD_Grid_Cell", 0.92f, 0.92f);
		AssertPlateSize("BLD_Grid_Edge", 1f, 0.16f);
		AssertPlateSize("BLD_Grid_Node", 0.18f, 0.18f);
		AssertPlateSize("BLD_Grid_Boundary", 1f, 0.18f);
	}

	[Test]
	public void GridPlatesLieFlat()
	{
		string[] gridPrefabs = GridPrefabs;
		foreach (string assetName in gridPrefabs)
		{
			Assert.That<float>(Mathf.Abs(Vector3.Dot(Resources.Load<GameObject>("Art/BuildGrid/" + assetName).GetComponent<BuildGridMark>().Pieces[0].transform.rotation * Vector3.forward, Vector3.up)), (IResolveConstraint)(object)Is.GreaterThan((object)0.99f), "'" + assetName + "' steht statt zu liegen (M10.4).", Array.Empty<object>());
		}
	}

	[Test]
	public void EveryGridMaterial_IsAuthoredAndTransparent()
	{
		string[] gridMaterials = GridMaterials;
		foreach (string assetName in gridMaterials)
		{
			Material material = Resources.Load<Material>("Art/BuildGrid/" + assetName);
			Assert.That<Material>(material, (IResolveConstraint)(object)Is.Not.Null, "Rastermaterial fehlt: Art/BuildGrid/" + assetName, Array.Empty<object>());
			Assert.That<float>(material.color.a, (IResolveConstraint)(object)Is.LessThan((object)1f), "'" + assetName + "' muss durchscheinen — das Raster liegt über der Welt, es ersetzt sie nicht.", Array.Empty<object>());
			Assert.That<Texture>(material.GetTexture("_BaseMap"), (IResolveConstraint)(object)Is.Not.Null, "'" + assetName + "' hat keine Rastertextur.", Array.Empty<object>());
		}
	}

	[Test]
	public void TheFaintAreaAndTheFocusMarks_AreTwoDistinctMaterials()
	{
		Material material = Resources.Load<Material>("Art/BuildGrid/BLD_Grid_Area");
		Material focus = Resources.Load<Material>("Art/BuildGrid/BLD_Grid_Focus");
		Assert.That<float>(material.color.a, (IResolveConstraint)(object)Is.LessThan((object)focus.color.a), "Die gesamte Baufläche erscheint schwach, das Fenster deutlich (Abschnitt 12).", Array.Empty<object>());
	}

	private static void AssertPlateSize(string assetName, float sizeX, float sizeZ)
	{
		GameObject gameObject = Resources.Load<GameObject>("Art/BuildGrid/" + assetName);
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null, assetName, Array.Empty<object>());
		Vector3 localScale = gameObject.GetComponent<BuildGridMark>().Pieces[0].transform.localScale;
		Assert.That<float>(localScale.x, (IResolveConstraint)(object)Is.EqualTo((object)sizeX).Within((object)0.0001f), assetName, Array.Empty<object>());
		Assert.That<float>(localScale.y, (IResolveConstraint)(object)Is.EqualTo((object)sizeZ).Within((object)0.0001f), assetName, Array.Empty<object>());
	}

	private static GridOverlayPlan Build(in GridCellRect area, IReadOnlyList<GridCellRect> zones, GridCoordinate focus, BuildingPlacementKind layer, GridOccupancy occupancy)
	{
		GridOverlayPlan gridOverlayPlan = new GridOverlayPlan();
		gridOverlayPlan.Build(in area, zones, focus, layer, occupancy);
		return gridOverlayPlan;
	}

	private static GridOverlayState StateOf(GridOverlayPlan plan, GridCoordinate cell)
	{
		return plan.Cells.Single((GridOverlayCell mark) => mark.Cell == cell).State;
	}

	private static GridOverlayState StateAt(GridOverlayPlan plan, GridEdge edge)
	{
		return plan.Edges.Single((GridOverlayEdge mark) => mark.Edge == edge).State;
	}
}
}
