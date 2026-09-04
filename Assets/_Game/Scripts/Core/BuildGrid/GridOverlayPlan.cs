using Eidren.Data;
using System.Collections.Generic;

namespace Eidren.Core.BuildGrid
{
	public sealed class GridOverlayPlan
	{
		public const int FocusRadius = 2;

		private readonly List<GridOverlayCell> _cells = new List<GridOverlayCell>();

		private readonly List<GridOverlayEdge> _edges = new List<GridOverlayEdge>();

		private readonly List<GridNode> _nodes = new List<GridNode>();

		private readonly HashSet<GridEdge> _seenEdges = new HashSet<GridEdge>();

		private readonly HashSet<GridNode> _seenNodes = new HashSet<GridNode>();

		private static readonly GridDirection[] AllSides = new GridDirection[4]
		{
			GridDirection.North,
			GridDirection.East,
			GridDirection.South,
			GridDirection.West
		};

		public IReadOnlyList<GridOverlayCell> Cells => _cells;

		public IReadOnlyList<GridOverlayEdge> Edges => _edges;

		public IReadOnlyList<GridNode> Nodes => _nodes;

		public void Clear()
		{
			_cells.Clear();
			_edges.Clear();
			_nodes.Clear();
			_seenEdges.Clear();
			_seenNodes.Clear();
		}

		public void Build(in GridCellRect buildArea, IReadOnlyList<GridCellRect> blockedAreas, GridCoordinate focus, BuildingPlacementKind layer, GridOccupancy occupancy)
		{
			Clear();
			GridCellRect window = GridCellRect.Around(focus, 2);
			if (layer == BuildingPlacementKind.Edge)
			{
				BuildEdges(in window, in buildArea, blockedAreas, occupancy);
			}
			else
			{
				BuildCells(in window, in buildArea, blockedAreas, layer, occupancy);
			}
		}

		private void BuildCells(in GridCellRect window, in GridCellRect buildArea, IReadOnlyList<GridCellRect> blockedAreas, BuildingPlacementKind layer, GridOccupancy occupancy)
		{
			foreach (GridCoordinate item in window.Cells())
			{
				if (buildArea.Contains(item))
				{
					_cells.Add(new GridOverlayCell(item, CellState(item, blockedAreas, layer, occupancy)));
				}
			}
		}

		private GridOverlayState CellState(GridCoordinate cell, IReadOnlyList<GridCellRect> blockedAreas, BuildingPlacementKind layer, GridOccupancy occupancy)
		{
			if (IsBlocked(cell, blockedAreas))
			{
				return GridOverlayState.Blocked;
			}
			return (occupancy == null || !occupancy.IsCellOccupied(layer, cell)) ? GridOverlayState.Focus : GridOverlayState.Conflict;
		}

		private void BuildEdges(in GridCellRect window, in GridCellRect buildArea, IReadOnlyList<GridCellRect> blockedAreas, GridOccupancy occupancy)
		{
			foreach (GridCoordinate item in window.Cells())
			{
				if (buildArea.Contains(item))
				{
					GridDirection[] allSides = AllSides;
					foreach (GridDirection side in allSides)
					{
						AddEdge(GridEdge.From(item, side), blockedAreas, occupancy);
					}
				}
			}
		}

		private void AddEdge(GridEdge edge, IReadOnlyList<GridCellRect> blockedAreas, GridOccupancy occupancy)
		{
			if (_seenEdges.Add(edge))
			{
				_edges.Add(new GridOverlayEdge(edge, EdgeState(edge, blockedAreas, occupancy)));
				AddNode(edge.LowerNode);
				AddNode(edge.UpperNode);
			}
		}

		private static GridOverlayState EdgeState(GridEdge edge, IReadOnlyList<GridCellRect> blockedAreas, GridOccupancy occupancy)
		{
			if (IsBlocked(edge.LowerCell, blockedAreas) && IsBlocked(edge.UpperCell, blockedAreas))
			{
				return GridOverlayState.Blocked;
			}
			return (occupancy == null || !occupancy.IsEdgeOccupied(edge)) ? GridOverlayState.Focus : GridOverlayState.Conflict;
		}

		private void AddNode(GridNode node)
		{
			if (_seenNodes.Add(node))
			{
				_nodes.Add(node);
			}
		}

		private static bool IsBlocked(GridCoordinate cell, IReadOnlyList<GridCellRect> blockedAreas)
		{
			if (blockedAreas == null)
			{
				return false;
			}
			for (int i = 0; i < blockedAreas.Count; i++)
			{
				if (blockedAreas[i].Contains(cell))
				{
					return true;
				}
			}
			return false;
		}
	}

	public readonly struct GridOverlayCell
	{
		public GridCoordinate Cell { get; }

		public GridOverlayState State { get; }

		public GridOverlayCell(GridCoordinate cell, GridOverlayState state)
		{
			Cell = cell;
			State = state;
		}
	}

	public readonly struct GridOverlayEdge
	{
		public GridEdge Edge { get; }

		public GridOverlayState State { get; }

		public GridOverlayEdge(GridEdge edge, GridOverlayState state)
		{
			Edge = edge;
			State = state;
		}
	}
}
