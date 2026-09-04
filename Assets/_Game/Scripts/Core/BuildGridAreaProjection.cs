using Eidren.Core.BuildGrid;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Core.Services
{
	public static class BuildGridAreaProjection
	{
		private const float Half = 0.5f;

		public static GridCellRect Buildable(in BuildingPlacementArea area)
		{
			Rect buildBounds = area.BuildBounds;
			GridCellRect result = new GridCellRect(Mathf.CeilToInt(buildBounds.xMin - area.Origin.WorldX + 0.5f), Mathf.CeilToInt(buildBounds.yMin - area.Origin.WorldZ + 0.5f), Mathf.FloorToInt(buildBounds.xMax - area.Origin.WorldX - 0.5f), Mathf.FloorToInt(buildBounds.yMax - area.Origin.WorldZ - 0.5f));
			if (!result.IsEmpty)
			{
				return result;
			}
			return GridCellRect.Empty;
		}

		public static void Blocked(in BuildingPlacementArea area, List<GridCellRect> target)
		{
			if (target == null)
			{
				return;
			}
			target.Clear();
			IReadOnlyList<Rect> blockedBounds = area.BlockedBounds;
			for (int i = 0; i < blockedBounds.Count; i++)
			{
				GridCellRect item = Overlapping(area.Origin, blockedBounds[i]);
				if (!item.IsEmpty)
				{
					target.Add(item);
				}
			}
		}

		private static GridCellRect Overlapping(BuildGridOrigin origin, Rect bounds)
		{
			GridCellRect result = new GridCellRect(Mathf.FloorToInt(bounds.xMin - origin.WorldX - 0.5f) + 1, Mathf.FloorToInt(bounds.yMin - origin.WorldZ - 0.5f) + 1, Mathf.CeilToInt(bounds.xMax - origin.WorldX + 0.5f) - 1, Mathf.CeilToInt(bounds.yMax - origin.WorldZ + 0.5f) - 1);
			if (!result.IsEmpty)
			{
				return result;
			}
			return GridCellRect.Empty;
		}
	}
}
