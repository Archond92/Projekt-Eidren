using System.Collections.Generic;
using System;

namespace Eidren.Core.BuildGrid
{
	public readonly struct GridFootprint
	{
		private static readonly GridDirection[] AllSides = new GridDirection[4]
		{
			GridDirection.North,
			GridDirection.East,
			GridDirection.South,
			GridDirection.West
		};

		public GridCoordinate Origin { get; }

		public int Width { get; }

		public int Depth { get; }

		public int CellCount => Width * Depth;

		public GridFootprint(GridCoordinate origin, int width, int depth)
		{
			if (width < 1 || depth < 1)
			{
				throw new ArgumentOutOfRangeException("width", "Eine Grundflaeche ist mindestens 1x1 Zellen gross.");
			}
			Origin = origin;
			Width = width;
			Depth = depth;
		}

		public static GridFootprint FromCentre(float gridX, float gridZ, int width, int depth, int quarterTurns)
		{
			bool num = (quarterTurns % 4 + 4) % 4 % 2 == 1;
			int num2 = (num ? depth : width);
			int num3 = (num ? width : depth);
			return new GridFootprint(new GridCoordinate(Anchor(gridX, num2), Anchor(gridZ, num3)), num2, num3);
		}

		public IEnumerable<GridCoordinate> Cells()
		{
			for (int offsetX = 0; offsetX < Width; offsetX++)
			{
				for (int offsetZ = 0; offsetZ < Depth; offsetZ++)
				{
					yield return new GridCoordinate(Origin.X + offsetX, Origin.Z + offsetZ);
				}
			}
		}

		public IEnumerable<GridEdge> BoundaryEdges()
		{
			foreach (GridCoordinate cell in Cells())
			{
				GridDirection[] allSides = AllSides;
				foreach (GridDirection gridDirection in allSides)
				{
					GridCoordinate gridCoordinate = cell.Neighbour(gridDirection);
					if (gridCoordinate.X < Origin.X || gridCoordinate.Z < Origin.Z || gridCoordinate.X >= Origin.X + Width || gridCoordinate.Z >= Origin.Z + Depth)
					{
						yield return GridEdge.From(cell, gridDirection);
					}
				}
			}
		}

		private static int Anchor(float gridValue, int length)
		{
			return (int)Math.Floor(gridValue - (float)(length - 1) * 0.5f + 0.5f);
		}
	}
}
