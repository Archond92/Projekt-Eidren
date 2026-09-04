using System.Collections.Generic;

namespace Eidren.Core.BuildGrid
{
	public readonly struct GridCellRect
	{
		public static readonly GridCellRect Empty = new GridCellRect(0, 0, -1, -1);

		public int MinX { get; }

		public int MinZ { get; }

		public int MaxX { get; }

		public int MaxZ { get; }

		public bool IsEmpty => MaxX < MinX || MaxZ < MinZ;

		public GridCellRect(int minX, int minZ, int maxX, int maxZ)
		{
			MinX = minX;
			MinZ = minZ;
			MaxX = maxX;
			MaxZ = maxZ;
		}

		public bool Contains(GridCoordinate cell)
		{
			return cell.X >= MinX && cell.X <= MaxX && cell.Z >= MinZ && cell.Z <= MaxZ;
		}

		public static GridCellRect Around(GridCoordinate centre, int radius)
		{
			return (radius < 0) ? Empty : new GridCellRect(centre.X - radius, centre.Z - radius, centre.X + radius, centre.Z + radius);
		}

		public IEnumerable<GridCoordinate> Cells()
		{
			for (int x = MinX; x <= MaxX; x++)
			{
				for (int z = MinZ; z <= MaxZ; z++)
				{
					yield return new GridCoordinate(x, z);
				}
			}
		}

		public override string ToString()
		{
			return IsEmpty ? "leer" : $"({MinX}, {MinZ}) bis ({MaxX}, {MaxZ})";
		}
	}
}
