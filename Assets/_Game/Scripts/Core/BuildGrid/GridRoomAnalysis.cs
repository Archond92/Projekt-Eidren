using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.BuildGrid
{
	public static class GridRoomAnalysis
	{
		private static readonly GridDirection[] Sides = new GridDirection[4]
		{
			GridDirection.North,
			GridDirection.East,
			GridDirection.South,
			GridDirection.West
		};

		public static IReadOnlyList<GridRoom> Derive(GridOccupancy occupancy)
		{
			if (occupancy == null)
			{
				throw new ArgumentNullException("occupancy");
			}
			HashSet<GridCoordinate> hashSet = new HashSet<GridCoordinate>(occupancy.CellsOn(BuildingPlacementKind.Floor));
			HashSet<GridCoordinate> hashSet2 = new HashSet<GridCoordinate>();
			List<GridRoom> list = new List<GridRoom>();
			foreach (GridCoordinate item in hashSet)
			{
				if (hashSet2.Add(item))
				{
					list.Add(Flood(occupancy, hashSet, hashSet2, item));
				}
			}
			return list;
		}

		private static GridRoom Flood(GridOccupancy occupancy, HashSet<GridCoordinate> floor, HashSet<GridCoordinate> visited, GridCoordinate start)
		{
			HashSet<GridCoordinate> hashSet = new HashSet<GridCoordinate> { start };
			Queue<GridCoordinate> queue = new Queue<GridCoordinate>();
			queue.Enqueue(start);
			while (queue.Count > 0)
			{
				GridCoordinate cell = queue.Dequeue();
				GridDirection[] sides = Sides;
				foreach (GridDirection gridDirection in sides)
				{
					if (!occupancy.IsEdgeOccupied(GridEdge.From(cell, gridDirection)))
					{
						GridCoordinate item = cell.Neighbour(gridDirection);
						if (floor.Contains(item) && hashSet.Add(item))
						{
							visited.Add(item);
							queue.Enqueue(item);
						}
					}
				}
			}
			return Close(occupancy, hashSet);
		}

		private static GridRoom Close(GridOccupancy occupancy, HashSet<GridCoordinate> region)
		{
			HashSet<GridEdge> hashSet = new HashSet<GridEdge>();
			bool isClosed = true;
			foreach (GridCoordinate item in region)
			{
				GridDirection[] sides = Sides;
				foreach (GridDirection gridDirection in sides)
				{
					if (!region.Contains(item.Neighbour(gridDirection)))
					{
						GridEdge gridEdge = GridEdge.From(item, gridDirection);
						hashSet.Add(gridEdge);
						if (!occupancy.IsEdgeOccupied(gridEdge))
						{
							isClosed = false;
						}
					}
				}
			}
			return new GridRoom(region, hashSet, isClosed);
		}
	}

	public sealed class GridRoom
	{
		public IReadOnlyCollection<GridCoordinate> Cells { get; }

		public IReadOnlyCollection<GridEdge> BoundaryEdges { get; }

		public bool IsClosed { get; }

		internal GridRoom(IReadOnlyCollection<GridCoordinate> cells, IReadOnlyCollection<GridEdge> boundaryEdges, bool isClosed)
		{
			Cells = cells;
			BoundaryEdges = boundaryEdges;
			IsClosed = isClosed;
		}
	}
}
