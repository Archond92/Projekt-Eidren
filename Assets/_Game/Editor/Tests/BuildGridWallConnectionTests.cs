using Eidren.Core.BuildGrid;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System;

namespace Eidren.Tests.EditMode
{
public sealed class BuildGridWallConnectionTests
{
	[Test]
	public void AnEdgeEndsInTwoDistinctNodes()
	{
		GridEdge eastWest = Edge(2, 3, GridEdgeOrientation.EastWest);
		Assert.That<GridNode>(eastWest.LowerNode, (IResolveConstraint)(object)Is.EqualTo((object)new GridNode(1, 3)));
		Assert.That<GridNode>(eastWest.UpperNode, (IResolveConstraint)(object)Is.EqualTo((object)new GridNode(2, 3)));
		GridEdge northSouth = Edge(2, 3, GridEdgeOrientation.NorthSouth);
		Assert.That<GridNode>(northSouth.LowerNode, (IResolveConstraint)(object)Is.EqualTo((object)new GridNode(2, 2)));
		Assert.That<GridNode>(northSouth.UpperNode, (IResolveConstraint)(object)Is.EqualTo((object)new GridNode(2, 3)));
	}

	[Test]
	public void TouchingEdgesShareExactlyOneNode()
	{
		GridEdge running = Edge(0, 0, GridEdgeOrientation.EastWest);
		GridEdge turning = Edge(0, 1, GridEdgeOrientation.NorthSouth);
		Assert.That<GridNode>(running.UpperNode, (IResolveConstraint)(object)Is.EqualTo((object)turning.LowerNode), "Sonst wuesste die Ecke nichts von ihrem eigenen Winkel.", Array.Empty<object>());
	}

	[Test]
	public void EveryArmOfANodeIsADifferentEdge()
	{
		GridNode node = new GridNode(4, -2);
		GridEdge north = node.Arm(GridDirection.North);
		GridEdge east = node.Arm(GridDirection.East);
		GridEdge south = node.Arm(GridDirection.South);
		GridEdge west = node.Arm(GridDirection.West);
		Assert.That<GridEdge[]>(new GridEdge[4] { north, east, south, west }, (IResolveConstraint)(object)Is.Unique, "Vier Arme, vier Kanten — sonst faellt die Zaehlung in sich zusammen.", Array.Empty<object>());
	}

	[Test]
	public void EveryArmTouchesTheNodeItCameFrom()
	{
		GridNode node = new GridNode(-3, 5);
		GridDirection[] array = new GridDirection[4]
		{
			GridDirection.North,
			GridDirection.East,
			GridDirection.South,
			GridDirection.West
		};
		foreach (GridDirection direction in array)
		{
			GridEdge arm = node.Arm(direction);
			Assert.That<bool>(arm.LowerNode == node || arm.UpperNode == node, (IResolveConstraint)(object)Is.True, $"Der Arm nach {direction} endet nicht im Knoten.", Array.Empty<object>());
		}
	}

	[Test]
	public void ALoneWallIsCappedOnBothEnds()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridWallConnection shape = GridWallConnection.Classify(Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest), occupancy);
		Assert.That<GridWallShape>(shape.Lower, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.EndCap));
		Assert.That<GridWallShape>(shape.Upper, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.EndCap));
	}

	[Test]
	public void AWallInAStraightRunIsStraightInTheMiddle()
	{
		GridOccupancy occupancy = new GridOccupancy();
		Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest);
		GridEdge edge = Occupy(occupancy, 1, 0, GridEdgeOrientation.EastWest);
		Occupy(occupancy, 2, 0, GridEdgeOrientation.EastWest);
		GridWallConnection shape = GridWallConnection.Classify(edge, occupancy);
		Assert.That<GridWallShape>(shape.Lower, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.Straight));
		Assert.That<GridWallShape>(shape.Upper, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.Straight));
	}

	[Test]
	public void AWallAtTheEndOfARunIsStraightInsideAndCappedOutside()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridEdge edge = Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest);
		Occupy(occupancy, 1, 0, GridEdgeOrientation.EastWest);
		GridWallConnection shape = GridWallConnection.Classify(edge, occupancy);
		Assert.That<GridWallShape>(shape.Lower, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.EndCap), "Nach Westen kommt nichts mehr.", Array.Empty<object>());
		Assert.That<GridWallShape>(shape.Upper, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.Straight), "Nach Osten laeuft die Mauer weiter.", Array.Empty<object>());
	}

	[Test]
	public void TwoWallsMeetingAtRightAnglesFormACorner()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridEdge edge = Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest);
		GridEdge turning = Occupy(occupancy, 0, 1, GridEdgeOrientation.NorthSouth);
		Assert.That<GridWallShape>(GridWallConnection.Classify(edge, occupancy).Upper, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.Corner));
		Assert.That<GridWallShape>(GridWallConnection.Classify(turning, occupancy).Lower, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.Corner));
	}

	[Test]
	public void AWallBranchingOffAStraightRunFormsATee()
	{
		GridOccupancy occupancy = new GridOccupancy();
		Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest);
		Occupy(occupancy, 1, 0, GridEdgeOrientation.EastWest);
		Assert.That<GridWallShape>(GridWallConnection.Classify(Occupy(occupancy, 0, 1, GridEdgeOrientation.NorthSouth), occupancy).Lower, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.Tee), "Der Abzweig sieht die durchlaufende Mauer als zwei Nachbarn.", Array.Empty<object>());
	}

	[Test]
	public void TwoPerpendicularNeighboursAlsoFormATee()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridEdge edge = Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest);
		Occupy(occupancy, 0, 1, GridEdgeOrientation.NorthSouth);
		Occupy(occupancy, 0, 0, GridEdgeOrientation.NorthSouth);
		Assert.That<GridWallShape>(GridWallConnection.Classify(edge, occupancy).Upper, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.Tee));
	}

	[Test]
	public void AllFourArmsFormACross()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridEdge edge = Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest);
		Occupy(occupancy, 1, 0, GridEdgeOrientation.EastWest);
		Occupy(occupancy, 0, 1, GridEdgeOrientation.NorthSouth);
		Occupy(occupancy, 0, 0, GridEdgeOrientation.NorthSouth);
		Assert.That<GridWallShape>(GridWallConnection.Classify(edge, occupancy).Upper, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.Cross));
	}

	[Test]
	public void AClosedSquareIsFourCorners()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridEdge south = Occupy(occupancy, 0, -1, GridEdgeOrientation.EastWest);
		GridEdge north = Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest);
		GridEdge west = Occupy(occupancy, -1, 0, GridEdgeOrientation.NorthSouth);
		GridEdge east = Occupy(occupancy, 0, 0, GridEdgeOrientation.NorthSouth);
		GridEdge[] array = new GridEdge[4] { south, north, west, east };
		for (int i = 0; i < array.Length; i++)
		{
			GridEdge edge = array[i];
			GridWallConnection shape = GridWallConnection.Classify(edge, occupancy);
			Assert.That<GridWallShape>(shape.Lower, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.Corner), edge.ToString(), Array.Empty<object>());
			Assert.That<GridWallShape>(shape.Upper, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.Corner), edge.ToString(), Array.Empty<object>());
		}
	}

	[Test]
	public void ADiagonalNeighbourIsNoNeighbour()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridEdge edge = Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest);
		Occupy(occupancy, 1, 1, GridEdgeOrientation.EastWest);
		GridWallConnection shape = GridWallConnection.Classify(edge, occupancy);
		Assert.That<GridWallShape>(shape.Lower, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.EndCap));
		Assert.That<GridWallShape>(shape.Upper, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.EndCap));
	}

	[Test]
	public void TheOppositeNamingOfTheSameEdgeGivesTheSameShape()
	{
		GridOccupancy occupancy = new GridOccupancy();
		Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest);
		Occupy(occupancy, 1, 0, GridEdgeOrientation.EastWest);
		GridWallConnection gridWallConnection = GridWallConnection.Classify(GridEdge.From(new GridCoordinate(0, 0), GridDirection.North), occupancy);
		GridWallConnection viaSouth = GridWallConnection.Classify(GridEdge.From(new GridCoordinate(0, 1), GridDirection.South), occupancy);
		Assert.That<GridWallConnection>(gridWallConnection, (IResolveConstraint)(object)Is.EqualTo((object)viaSouth));
	}

	[Test]
	public void ShapeIgnoresCellsAndReadsOnlyEdges()
	{
		GridOccupancy occupancy = new GridOccupancy();
		GridEdge wall = Occupy(occupancy, 0, 0, GridEdgeOrientation.EastWest);
		int[] array = new int[3] { -1, 0, 1 };
		foreach (int x in array)
		{
			occupancy.TryOccupyCell(BuildingPlacementKind.Floor, new GridCoordinate(x, 0), $"floor.{x}");
			occupancy.TryOccupyCell(BuildingPlacementKind.Object, new GridCoordinate(x, 1), $"object.{x}");
		}
		GridWallConnection shape = GridWallConnection.Classify(wall, occupancy);
		Assert.That<GridWallShape>(shape.Lower, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.EndCap));
		Assert.That<GridWallShape>(shape.Upper, (IResolveConstraint)(object)Is.EqualTo((object)GridWallShape.EndCap));
	}

	private static GridEdge Edge(int x, int z, GridEdgeOrientation orientation)
	{
		return GridEdge.From(new GridCoordinate(x, z), (orientation != GridEdgeOrientation.EastWest) ? GridDirection.East : GridDirection.North);
	}

	private static GridEdge Occupy(GridOccupancy occupancy, int x, int z, GridEdgeOrientation orientation)
	{
		GridEdge edge = Edge(x, z, orientation);
		occupancy.TryOccupyEdge(edge, $"wall.{edge}");
		return edge;
	}
}
}
