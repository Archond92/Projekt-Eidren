using Eidren.Core.BuildGrid;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Eidren.Tests.EditMode
{
public sealed class BuildGridDomainTests
{
	[Test]
	public void EastEdge_AndWestEdgeOfNeighbour_AreTheSameEdge()
	{
		GridEdge east = GridEdge.From(new GridCoordinate(4, 7), GridDirection.East);
		GridEdge west = GridEdge.From(new GridCoordinate(5, 7), GridDirection.West);
		Assert.That<GridEdge>(east, (IResolveConstraint)(object)Is.EqualTo((object)west));
		Assert.That<int>(east.GetHashCode(), (IResolveConstraint)(object)Is.EqualTo((object)west.GetHashCode()));
	}

	[Test]
	public void NorthEdge_AndSouthEdgeOfNeighbour_AreTheSameEdge()
	{
		GridEdge north = GridEdge.From(new GridCoordinate(-2, 3), GridDirection.North);
		GridEdge south = GridEdge.From(new GridCoordinate(-2, 4), GridDirection.South);
		Assert.That<GridEdge>(north, (IResolveConstraint)(object)Is.EqualTo((object)south));
		Assert.That<int>(north.GetHashCode(), (IResolveConstraint)(object)Is.EqualTo((object)south.GetHashCode()));
	}

	[Test]
	public void OppositeDescriptions_DoNotCreateTwoWalls()
	{
		GridOccupancy gridOccupancy = new GridOccupancy();
		GridCoordinate cell = new GridCoordinate(0, 0);
		Assert.That<bool>(gridOccupancy.TryOccupyEdge(GridEdge.From(cell, GridDirection.East), "wall-a"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(gridOccupancy.TryOccupyEdge(GridEdge.From(cell.Neighbour(GridDirection.East), GridDirection.West), "wall-b"), (IResolveConstraint)(object)Is.False, "Dieselbe Kante darf nicht zweimal belegt werden.", Array.Empty<object>());
		Assert.That<int>(gridOccupancy.OccupiedEdges.Count, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void TryBetween_IsIndependentOfArgumentOrder()
	{
		GridCoordinate left = new GridCoordinate(1, 1);
		GridCoordinate right = new GridCoordinate(2, 1);
		Assert.That<bool>(GridEdge.TryBetween(left, right, out var forward), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(GridEdge.TryBetween(right, left, out var backward), (IResolveConstraint)(object)Is.True);
		Assert.That<GridEdge>(forward, (IResolveConstraint)(object)Is.EqualTo((object)backward));
	}

	[Test]
	public void TryBetween_RejectsCellsThatAreNotNeighbours()
	{
		Assert.That<bool>(GridEdge.TryBetween(new GridCoordinate(0, 0), new GridCoordinate(1, 1), out var edge), (IResolveConstraint)(object)Is.False, "Diagonale Zellen teilen keine Kante.", Array.Empty<object>());
		Assert.That<bool>(GridEdge.TryBetween(new GridCoordinate(0, 0), new GridCoordinate(2, 0), out edge), (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void Edge_KnowsBothAdjacentCells()
	{
		GridEdge edge = GridEdge.From(new GridCoordinate(3, 3), GridDirection.North);
		Assert.That<GridCoordinate>(edge.LowerCell, (IResolveConstraint)(object)Is.EqualTo((object)new GridCoordinate(3, 3)));
		Assert.That<GridCoordinate>(edge.UpperCell, (IResolveConstraint)(object)Is.EqualTo((object)new GridCoordinate(3, 4)));
	}

	[Test]
	public void FloorAndObject_MayShareTheSameCell()
	{
		GridOccupancy gridOccupancy = new GridOccupancy();
		GridCoordinate cell = new GridCoordinate(2, 2);
		Assert.That<bool>(gridOccupancy.TryOccupyCell(BuildingPlacementKind.Floor, cell, "floor-1"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(gridOccupancy.TryOccupyCell(BuildingPlacementKind.Object, cell, "bench-1"), (IResolveConstraint)(object)Is.True, "Ein Gebaeude darf auf einem Boden stehen.", Array.Empty<object>());
		Assert.That<bool>(gridOccupancy.TryOccupyCell(BuildingPlacementKind.Decoration, cell, "deco-1"), (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void SameLayerTwice_OnTheSameCell_IsRejected()
	{
		GridOccupancy gridOccupancy = new GridOccupancy();
		GridCoordinate cell = new GridCoordinate(0, 0);
		gridOccupancy.TryOccupyCell(BuildingPlacementKind.Floor, cell, "floor-1");
		Assert.That<bool>(gridOccupancy.TryOccupyCell(BuildingPlacementKind.Floor, cell, "floor-2"), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(gridOccupancy.TryGetCellOwner(BuildingPlacementKind.Floor, cell, out var owner), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(owner, (IResolveConstraint)(object)Is.EqualTo((object)"floor-1"));
	}

	[Test]
	public void EdgeLayer_IsNotAddressableThroughTheCellApi()
	{
		GridOccupancy occupancy = new GridOccupancy();
		Assert.That<bool>((ActualValueDelegate<bool>)(() => occupancy.TryOccupyCell(BuildingPlacementKind.Edge, new GridCoordinate(0, 0), "wall-1")), (IResolveConstraint)(object)Throws.ArgumentException);
	}

	[Test]
	public void ReleasedCell_BecomesFreeAgain()
	{
		GridOccupancy gridOccupancy = new GridOccupancy();
		GridCoordinate cell = new GridCoordinate(5, 5);
		gridOccupancy.TryOccupyCell(BuildingPlacementKind.Object, cell, "chest-1");
		Assert.That<bool>(gridOccupancy.ReleaseCell(BuildingPlacementKind.Object, cell), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(gridOccupancy.IsCellOccupied(BuildingPlacementKind.Object, cell), (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void WorldCentre_AndCell_RoundTrip()
	{
		BuildGridOrigin origin = new BuildGridOrigin(12.5f, -3.5f);
		GridCoordinate cell = new GridCoordinate(4, -7);
		origin.ToWorldCentre(cell, out var worldX, out var worldZ);
		Assert.That<GridCoordinate>(origin.ToCell(worldX, worldZ), (IResolveConstraint)(object)Is.EqualTo((object)cell));
	}

	[Test]
	public void ToCell_SnapsToTheNearestCell()
	{
		BuildGridOrigin origin = new BuildGridOrigin(0f, 0f);
		Assert.That<GridCoordinate>(origin.ToCell(2.4f, 2.4f), (IResolveConstraint)(object)Is.EqualTo((object)new GridCoordinate(2, 2)));
		Assert.That<GridCoordinate>(origin.ToCell(2.6f, 2.6f), (IResolveConstraint)(object)Is.EqualTo((object)new GridCoordinate(3, 3)));
		Assert.That<GridCoordinate>(origin.ToCell(-2.4f, -2.4f), (IResolveConstraint)(object)Is.EqualTo((object)new GridCoordinate(-2, -2)));
	}

	[Test]
	public void ToCell_ResolvesExactHalves_Deterministically()
	{
		BuildGridOrigin origin = new BuildGridOrigin(0f, 0f);
		Assert.That<GridCoordinate>(origin.ToCell(0.5f, 1.5f), (IResolveConstraint)(object)Is.EqualTo((object)new GridCoordinate(1, 2)), "Ein genaues .5 geht immer nach oben, damit die spaetere Save-Migration reproduzierbar bleibt.", Array.Empty<object>());
		Assert.That<GridCoordinate>(origin.ToCell(2.5f, 2.5f), (IResolveConstraint)(object)Is.EqualTo((object)new GridCoordinate(3, 3)));
	}

	[Test]
	public void EdgeCentre_LiesBetweenItsTwoCells()
	{
		BuildGridOrigin origin = new BuildGridOrigin(0f, 0f);
		GridEdge edge = GridEdge.From(new GridCoordinate(1, 1), GridDirection.East);
		origin.ToWorldCentre(edge, out var worldX, out var worldZ);
		Assert.That<float>(worldX, (IResolveConstraint)(object)Is.EqualTo((object)1.5f).Within((object)0.0001f));
		Assert.That<float>(worldZ, (IResolveConstraint)(object)Is.EqualTo((object)1f).Within((object)0.0001f));
	}

	[Test]
	public void AuthoredOrigin_ShiftsTheWholeGrid()
	{
		BuildGridOrigin shifted = new BuildGridOrigin(100f, 50f);
		Assert.That<GridCoordinate>(shifted.ToCell(100f, 50f), (IResolveConstraint)(object)Is.EqualTo((object)new GridCoordinate(0, 0)));
		Assert.That<GridCoordinate>(shifted.ToCell(103f, 50f), (IResolveConstraint)(object)Is.EqualTo((object)new GridCoordinate(3, 0)));
	}

	[Test]
	public void ClosedFloorWithClosedBorder_IsAClosedRoom()
	{
		IReadOnlyList<GridRoom> readOnlyList = GridRoomAnalysis.Derive(SquareRoom(2, closeAll: true));
		Assert.That<int>(readOnlyList.Count, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(readOnlyList[0].Cells.Count, (IResolveConstraint)(object)Is.EqualTo((object)4));
		Assert.That<bool>(readOnlyList[0].IsClosed, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(readOnlyList[0].BoundaryEdges.Count, (IResolveConstraint)(object)Is.EqualTo((object)8));
	}

	[Test]
	public void FloorWithAGap_IsAnOpenRoom()
	{
		IReadOnlyList<GridRoom> readOnlyList = GridRoomAnalysis.Derive(SquareRoom(2, closeAll: false));
		Assert.That<int>(readOnlyList.Count, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(readOnlyList[0].IsClosed, (IResolveConstraint)(object)Is.False, "Eine offene Flaeche bekommt spaeter kein Dach.", Array.Empty<object>());
	}

	[Test]
	public void BareFloorWithoutAnyWall_IsNotClosed()
	{
		GridOccupancy gridOccupancy = new GridOccupancy();
		gridOccupancy.TryOccupyCell(BuildingPlacementKind.Floor, new GridCoordinate(0, 0), "floor-1");
		IReadOnlyList<GridRoom> readOnlyList = GridRoomAnalysis.Derive(gridOccupancy);
		Assert.That<int>(readOnlyList.Count, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(readOnlyList[0].IsClosed, (IResolveConstraint)(object)Is.False);
		Assert.That<int>(readOnlyList[0].BoundaryEdges.Count, (IResolveConstraint)(object)Is.EqualTo((object)4));
	}

	[Test]
	public void AWallBetweenTwoFloorCells_SplitsThemIntoTwoRooms()
	{
		GridOccupancy gridOccupancy = new GridOccupancy();
		GridCoordinate left = new GridCoordinate(0, 0);
		GridCoordinate right = new GridCoordinate(1, 0);
		gridOccupancy.TryOccupyCell(BuildingPlacementKind.Floor, left, "floor-1");
		gridOccupancy.TryOccupyCell(BuildingPlacementKind.Floor, right, "floor-2");
		gridOccupancy.TryOccupyEdge(GridEdge.From(left, GridDirection.East), "wall-1");
		IReadOnlyList<GridRoom> readOnlyList = GridRoomAnalysis.Derive(gridOccupancy);
		Assert.That<int>(readOnlyList.Count, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<bool>(readOnlyList.All((GridRoom room) => room.Cells.Count == 1), (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void DisconnectedFloorAreas_BecomeSeparateRooms()
	{
		GridOccupancy gridOccupancy = new GridOccupancy();
		gridOccupancy.TryOccupyCell(BuildingPlacementKind.Floor, new GridCoordinate(0, 0), "floor-1");
		gridOccupancy.TryOccupyCell(BuildingPlacementKind.Floor, new GridCoordinate(9, 9), "floor-2");
		Assert.That<int>(GridRoomAnalysis.Derive(gridOccupancy).Count, (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	[Test]
	public void NoFloor_YieldsNoRoom()
	{
		Assert.That<IReadOnlyList<GridRoom>>(GridRoomAnalysis.Derive(new GridOccupancy()), (IResolveConstraint)(object)Is.Empty);
	}

	private static GridOccupancy SquareRoom(int size, bool closeAll)
	{
		GridOccupancy occupancy = new GridOccupancy();
		List<GridCoordinate> cells = new List<GridCoordinate>();
		for (int x = 0; x < size; x++)
		{
			for (int z = 0; z < size; z++)
			{
				GridCoordinate cell = new GridCoordinate(x, z);
				cells.Add(cell);
				occupancy.TryOccupyCell(BuildingPlacementKind.Floor, cell, $"floor-{x}-{z}");
			}
		}
		bool skipped = false;
		foreach (GridCoordinate cell2 in cells)
		{
			GridDirection[] array = new GridDirection[4]
			{
				GridDirection.North,
				GridDirection.East,
				GridDirection.South,
				GridDirection.West
			};
			foreach (GridDirection side in array)
			{
				if (!cells.Contains(cell2.Neighbour(side)))
				{
					if (!closeAll && !skipped)
					{
						skipped = true;
					}
					else
					{
						occupancy.TryOccupyEdge(GridEdge.From(cell2, side), $"wall-{cell2.X}-{cell2.Z}-{side}");
					}
				}
			}
		}
		return occupancy;
	}
}
}
