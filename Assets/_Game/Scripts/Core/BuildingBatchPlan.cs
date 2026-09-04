using Eidren.Core.BuildGrid;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Core.Services
{
	public static class BuildingBatchPlan
	{
		public static IReadOnlyList<BuildingPlacementCandidate> Floor(BuildGridOrigin origin, string buildingId, Vector3 from, Vector3 to, int level = 1)
		{
			IReadOnlyList<GridCoordinate> readOnlyList = GridSpan.Cells(origin.ToCell(from.x, from.z), origin.ToCell(to.x, to.z));
			List<BuildingPlacementCandidate> list = new List<BuildingPlacementCandidate>(readOnlyList.Count);
			foreach (GridCoordinate item in readOnlyList)
			{
				origin.ToWorldCentre(item, out var worldX, out var worldZ);
				list.Add(new BuildingPlacementCandidate(buildingId, new Vector3(worldX, from.y, worldZ), 0, level));
			}
			return list;
		}

		public static IReadOnlyList<BuildingPlacementCandidate> Wall(BuildGridOrigin origin, string buildingId, Vector3 from, Vector3 to, int quarterTurns, int level = 1)
		{
			IReadOnlyList<GridEdge> readOnlyList = GridSpan.EdgeLine(origin.ToCell(from.x, from.z), origin.ToCell(to.x, to.z), BuildGridOrigin.OrientationFor(quarterTurns));
			List<BuildingPlacementCandidate> list = new List<BuildingPlacementCandidate>(readOnlyList.Count);
			foreach (GridEdge item in readOnlyList)
			{
				origin.ToWorldCentre(GridSpan.OwningCell(item), out var worldX, out var worldZ);
				list.Add(new BuildingPlacementCandidate(buildingId, new Vector3(worldX, from.y, worldZ), quarterTurns, level));
			}
			return list;
		}
	}
}
