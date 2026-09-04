using Eidren.Core.BuildGrid;
using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class BuildingPlacementRule : IBuildingPlacementRule
	{
		public const float GridSize = 1f;

		private readonly ContentDatabase _content;

		private readonly BuildingPlacementArea _area;

		private readonly GridOccupancy _occupancy = new GridOccupancy();

		private readonly Dictionary<string, string> _ownerBuilding = new Dictionary<string, string>(StringComparer.Ordinal);

		public BuildGridOrigin Origin => _area.Origin;

		public BuildingPlacementRule(ContentDatabase content, BuildingPlacementArea area)
		{
			_content = content ?? throw new ArgumentNullException("content");
			_area = area;
		}

		public BuildingPlacementEvaluation Evaluate(in BuildingPlacementCandidate candidate, IReadOnlyList<BuildingInstanceState> existing)
		{
			if (!_content.TryGetBuilding(candidate.BuildingId, out var value))
			{
				return new BuildingPlacementEvaluation(BuildingActionResult.UnknownBuilding);
			}
			if (!value.TryGetLevel(candidate.Level, out var footprint, out var _))
			{
				return new BuildingPlacementEvaluation(BuildingActionResult.InvalidLevel);
			}
			Rebuild(existing);
			return (value.PlacementKind == BuildingPlacementKind.Edge) ? EvaluateEdge(in candidate, value) : EvaluateCells(in candidate, value, footprint);
		}

		public GridOccupancy DescribeOccupancy(IReadOnlyList<BuildingInstanceState> existing)
		{
			Rebuild(existing);
			return _occupancy;
		}

		private void Rebuild(IReadOnlyList<BuildingInstanceState> existing)
		{
			_occupancy.Clear();
			_ownerBuilding.Clear();
			foreach (BuildingInstanceState item in existing ?? Array.Empty<BuildingInstanceState>())
			{
				if (item == null || !_content.TryGetBuilding(item.BuildingId, out var value) || !value.TryGetLevel(item.Level, out var footprint, out var _))
				{
					continue;
				}
				_ownerBuilding[item.InstanceId] = item.BuildingId;
				if (value.PlacementKind == BuildingPlacementKind.Edge)
				{
					_occupancy.TryOccupyEdge(EdgeOf(item.Position, item.QuarterTurns), item.InstanceId);
					continue;
				}
				foreach (GridCoordinate item2 in FootprintOf(item.Position, footprint, item.QuarterTurns).Cells())
				{
					_occupancy.TryOccupyCell(value.PlacementKind, item2, item.InstanceId);
				}
			}
		}

		private BuildingPlacementEvaluation EvaluateCells(in BuildingPlacementCandidate candidate, BuildingCostDefinition building, BuildingFootprint footprint)
		{
			GridFootprint area = FootprintOf(candidate.Position, footprint, candidate.QuarterTurns);
			foreach (GridCoordinate item in area.Cells())
			{
				Rect rect = CellBounds(item);
				if (!Contains(_area.BuildBounds, rect))
				{
					return new BuildingPlacementEvaluation(BuildingActionResult.OutsideBuildArea);
				}
				foreach (Rect blockedBound in _area.BlockedBounds)
				{
					if (Overlaps(rect, blockedBound))
					{
						return new BuildingPlacementEvaluation(BuildingActionResult.BlockedZone);
					}
				}
				if (_occupancy.IsCellOccupied(building.PlacementKind, item))
				{
					return new BuildingPlacementEvaluation(BuildingActionResult.Overlap);
				}
			}
			if (building.PlacementKind == BuildingPlacementKind.Floor)
			{
				return EvaluateFloorUnderExisting(in area);
			}
			return new BuildingPlacementEvaluation(BuildingGroundRuleCheck.Evaluate(_occupancy, area, building.GroundRule));
		}

		private BuildingPlacementEvaluation EvaluateFloorUnderExisting(in GridFootprint area)
		{
			foreach (GridCoordinate item in area.Cells())
			{
				if (!_occupancy.TryGetCellOwner(BuildingPlacementKind.Object, item, out var instanceId) || !_ownerBuilding.TryGetValue(instanceId, out var value) || !_content.TryGetBuilding(value, out var value2) || (value2.GroundRule != BuildingGroundRule.GroundOnly && value2.GroundRule != BuildingGroundRule.OutdoorOnly))
				{
					continue;
				}
				return new BuildingPlacementEvaluation(BuildingActionResult.NaturalGroundRequired);
			}
			return new BuildingPlacementEvaluation(BuildingActionResult.Success);
		}

		private BuildingPlacementEvaluation EvaluateEdge(in BuildingPlacementCandidate candidate, BuildingCostDefinition building)
		{
			GridEdge edge = EdgeOf(candidate.Position, candidate.QuarterTurns);
			Rect rect = EdgeBounds(edge);
			if (!Contains(_area.BuildBounds, rect))
			{
				return new BuildingPlacementEvaluation(BuildingActionResult.OutsideBuildArea);
			}
			foreach (Rect blockedBound in _area.BlockedBounds)
			{
				if (Overlaps(rect, blockedBound))
				{
					return new BuildingPlacementEvaluation(BuildingActionResult.BlockedZone);
				}
			}
			if (!_occupancy.TryGetEdgeOwner(edge, out var instanceId))
			{
				return new BuildingPlacementEvaluation(BuildingActionResult.Success);
			}
			string value;
			return (string.Equals(building.Id, "building.door", StringComparison.Ordinal) && _ownerBuilding.TryGetValue(instanceId, out value) && string.Equals(value, "building.wall", StringComparison.Ordinal)) ? new BuildingPlacementEvaluation(BuildingActionResult.Success, instanceId) : new BuildingPlacementEvaluation(BuildingActionResult.EdgeOccupied);
		}

		private GridFootprint FootprintOf(Vector3 position, BuildingFootprint footprint, int quarterTurns)
		{
			Vector3 vector = Snap(position);
			_area.Origin.ToGrid(vector.x, vector.z, out var gridX, out var gridZ);
			return GridFootprint.FromCentre(gridX, gridZ, footprint.Width, footprint.Depth, quarterTurns);
		}

		private GridEdge EdgeOf(Vector3 position, int quarterTurns)
		{
			Vector3 vector = Snap(position);
			return _area.Origin.ToEdge(vector.x, vector.z, BuildGridOrigin.OrientationFor(quarterTurns));
		}

		private Rect CellBounds(GridCoordinate cell)
		{
			_area.Origin.ToWorldCentre(cell, out var worldX, out var worldZ);
			return new Rect(worldX - 0.5f, worldZ - 0.5f, 1f, 1f);
		}

		private Rect EdgeBounds(GridEdge edge)
		{
			_area.Origin.ToWorldCentre(edge, out var worldX, out var worldZ);
			return (edge.Orientation == GridEdgeOrientation.NorthSouth) ? new Rect(worldX, worldZ - 0.5f, 0f, 1f) : new Rect(worldX - 0.5f, worldZ, 1f, 0f);
		}

		public static Vector3 Snap(Vector3 position)
		{
			return new Vector3(Mathf.Round(position.x / 1f) * 1f, position.y, Mathf.Round(position.z / 1f) * 1f);
		}

		private static bool Contains(Rect outer, Rect inner)
		{
			return inner.xMin >= outer.xMin && inner.xMax <= outer.xMax && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
		}

		private static bool Overlaps(Rect left, Rect right)
		{
			return left.xMin < right.xMax && left.xMax > right.xMin && left.yMin < right.yMax && left.yMax > right.yMin;
		}

		BuildingPlacementEvaluation IBuildingPlacementRule.Evaluate(in BuildingPlacementCandidate candidate, IReadOnlyList<BuildingInstanceState> existing)
		{
			return Evaluate(in candidate, existing);
		}
	}

	public readonly struct BuildingPlacementArea
	{
		public Rect BuildBounds { get; }

		public IReadOnlyList<Rect> BlockedBounds { get; }

		public BuildGridOrigin Origin { get; }

		public BuildingPlacementArea(Rect buildBounds, IReadOnlyList<Rect> blockedBounds, BuildGridOrigin origin = default(BuildGridOrigin))
		{
			BuildBounds = buildBounds;
			BlockedBounds = blockedBounds ?? Array.Empty<Rect>();
			Origin = origin;
		}
	}

	public readonly struct BuildingPlacementCandidate
	{
		public string BuildingId { get; }

		public Vector3 Position { get; }

		public int QuarterTurns { get; }

		public int Level { get; }

		public BuildingPlacementCandidate(string buildingId, Vector3 position, int quarterTurns, int level = 1)
		{
			BuildingId = buildingId ?? string.Empty;
			Position = position;
			QuarterTurns = (quarterTurns % 4 + 4) % 4;
			Level = level;
		}
	}

	public readonly struct BuildingPlacementEvaluation
	{
		public BuildingActionResult Result { get; }

		public string ReplacedInstanceId { get; }

		public bool ReplacesExisting => !string.IsNullOrEmpty(ReplacedInstanceId);

		public BuildingPlacementEvaluation(BuildingActionResult result, string replacedInstanceId = null)
		{
			Result = result;
			ReplacedInstanceId = replacedInstanceId ?? string.Empty;
		}

		public static implicit operator BuildingActionResult(BuildingPlacementEvaluation evaluation)
		{
			return evaluation.Result;
		}
	}

	public interface IBuildingPlacementRule
	{
		BuildingPlacementEvaluation Evaluate(in BuildingPlacementCandidate candidate, IReadOnlyList<BuildingInstanceState> existing);
	}
}
