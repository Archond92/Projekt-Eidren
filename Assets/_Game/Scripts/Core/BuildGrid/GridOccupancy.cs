using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.BuildGrid
{
	public sealed class GridOccupancy
	{
		private readonly Dictionary<GridCoordinate, string> _floor = new Dictionary<GridCoordinate, string>();

		private readonly Dictionary<GridCoordinate, string> _object = new Dictionary<GridCoordinate, string>();

		private readonly Dictionary<GridCoordinate, string> _decoration = new Dictionary<GridCoordinate, string>();

		private readonly Dictionary<GridEdge, string> _edges = new Dictionary<GridEdge, string>();

		public IReadOnlyCollection<GridEdge> OccupiedEdges => _edges.Keys;

		public bool TryOccupyCell(BuildingPlacementKind layer, GridCoordinate cell, string instanceId)
		{
			Dictionary<GridCoordinate, string> dictionary = CellMap(layer);
			if (dictionary.ContainsKey(cell))
			{
				return false;
			}
			dictionary[cell] = Require(instanceId);
			return true;
		}

		public bool TryOccupyEdge(GridEdge edge, string instanceId)
		{
			if (_edges.ContainsKey(edge))
			{
				return false;
			}
			_edges[edge] = Require(instanceId);
			return true;
		}

		public bool IsCellOccupied(BuildingPlacementKind layer, GridCoordinate cell)
		{
			return CellMap(layer).ContainsKey(cell);
		}

		public bool IsEdgeOccupied(GridEdge edge)
		{
			return _edges.ContainsKey(edge);
		}

		public bool TryGetCellOwner(BuildingPlacementKind layer, GridCoordinate cell, out string instanceId)
		{
			return CellMap(layer).TryGetValue(cell, out instanceId);
		}

		public bool TryGetEdgeOwner(GridEdge edge, out string instanceId)
		{
			return _edges.TryGetValue(edge, out instanceId);
		}

		public bool ReleaseCell(BuildingPlacementKind layer, GridCoordinate cell)
		{
			return CellMap(layer).Remove(cell);
		}

		public bool ReleaseEdge(GridEdge edge)
		{
			return _edges.Remove(edge);
		}

		public IReadOnlyCollection<GridCoordinate> CellsOn(BuildingPlacementKind layer)
		{
			return CellMap(layer).Keys;
		}

		public void Clear()
		{
			_floor.Clear();
			_object.Clear();
			_decoration.Clear();
			_edges.Clear();
		}

		private Dictionary<GridCoordinate, string> CellMap(BuildingPlacementKind layer)
		{
			return layer switch
			{
				BuildingPlacementKind.Floor => _floor, 
				BuildingPlacementKind.Object => _object, 
				BuildingPlacementKind.Decoration => _decoration, 
				BuildingPlacementKind.Edge => throw new ArgumentException("Die Kantenschicht wird ueber TryOccupyEdge und IsEdgeOccupied angesprochen, nicht ueber Zellen.", "layer"), 
				_ => throw new ArgumentOutOfRangeException("layer", layer, "Unbekannte Belegungsschicht."), 
			};
		}

		private static string Require(string instanceId)
		{
			if (!string.IsNullOrWhiteSpace(instanceId))
			{
				return instanceId;
			}
			throw new ArgumentException("Eine Belegung braucht eine Instanz-ID.", "instanceId");
		}
	}
}
