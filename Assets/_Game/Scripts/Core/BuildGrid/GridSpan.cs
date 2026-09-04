using System.Collections.Generic;
using System;

namespace Eidren.Core.BuildGrid
{
	public static class GridSpan
	{
		public static IReadOnlyList<GridCoordinate> Cells(GridCoordinate from, GridCoordinate to)
		{
			int num = Math.Min(from.X, to.X);
			int num2 = Math.Max(from.X, to.X);
			int num3 = Math.Min(from.Z, to.Z);
			int num4 = Math.Max(from.Z, to.Z);
			List<GridCoordinate> list = new List<GridCoordinate>((num2 - num + 1) * (num4 - num3 + 1));
			for (int i = num; i <= num2; i++)
			{
				for (int j = num3; j <= num4; j++)
				{
					list.Add(new GridCoordinate(i, j));
				}
			}
			return list;
		}

		public static IReadOnlyList<GridEdge> EdgeLine(GridCoordinate from, GridCoordinate to, GridEdgeOrientation orientation)
		{
			List<GridEdge> list = new List<GridEdge>();
			if (orientation == GridEdgeOrientation.EastWest)
			{
				int num = Math.Min(from.X, to.X);
				int num2 = Math.Max(from.X, to.X);
				for (int i = num; i <= num2; i++)
				{
					list.Add(GridEdge.From(new GridCoordinate(i, from.Z), GridDirection.North));
				}
				return list;
			}
			int num3 = Math.Min(from.Z, to.Z);
			int num4 = Math.Max(from.Z, to.Z);
			for (int j = num3; j <= num4; j++)
			{
				list.Add(GridEdge.From(new GridCoordinate(from.X, j), GridDirection.East));
			}
			return list;
		}

		public static GridCoordinate OwningCell(GridEdge edge)
		{
			return edge.Anchor;
		}
	}
}
