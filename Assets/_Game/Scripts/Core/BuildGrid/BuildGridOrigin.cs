using System;

namespace Eidren.Core.BuildGrid
{
	public readonly struct BuildGridOrigin
	{
		public const float CellSize = 1f;

		public static readonly BuildGridOrigin HomeBase = new BuildGridOrigin(0f, 0f);

		public float WorldX { get; }

		public float WorldZ { get; }

		public BuildGridOrigin(float worldX, float worldZ)
		{
			if (float.IsNaN(worldX) || float.IsInfinity(worldX) || float.IsNaN(worldZ) || float.IsInfinity(worldZ))
			{
				throw new ArgumentException("Der Bauursprung muss endlich sein.");
			}
			WorldX = worldX;
			WorldZ = worldZ;
		}

		public GridCoordinate ToCell(float worldX, float worldZ)
		{
			return new GridCoordinate(RoundToInt((worldX - WorldX) / 1f), RoundToInt((worldZ - WorldZ) / 1f));
		}

		public void ToGrid(float worldX, float worldZ, out float gridX, out float gridZ)
		{
			gridX = (worldX - WorldX) / 1f;
			gridZ = (worldZ - WorldZ) / 1f;
		}

		public GridEdge ToEdge(float worldX, float worldZ, GridEdgeOrientation orientation)
		{
			ToGrid(worldX, worldZ, out var gridX, out var gridZ);
			return (orientation == GridEdgeOrientation.NorthSouth) ? GridEdge.From(new GridCoordinate(RoundToInt(gridX - 0.5f), RoundToInt(gridZ)), GridDirection.East) : GridEdge.From(new GridCoordinate(RoundToInt(gridX), RoundToInt(gridZ - 0.5f)), GridDirection.North);
		}

		public static GridEdgeOrientation OrientationFor(int quarterTurns)
		{
			return ((quarterTurns % 4 + 4) % 4 % 2 == 0) ? GridEdgeOrientation.EastWest : GridEdgeOrientation.NorthSouth;
		}

		public void ToWorldCentre(GridCoordinate cell, out float worldX, out float worldZ)
		{
			worldX = WorldX + (float)cell.X * 1f;
			worldZ = WorldZ + (float)cell.Z * 1f;
		}

		public void ToWorldCentre(GridEdge edge, out float worldX, out float worldZ)
		{
			ToWorldCentre(edge.Anchor, out worldX, out worldZ);
			if (edge.Orientation == GridEdgeOrientation.NorthSouth)
			{
				worldX += 0.5f;
			}
			else
			{
				worldZ += 0.5f;
			}
		}

		public void ToWorldCentre(GridNode node, out float worldX, out float worldZ)
		{
			worldX = WorldX + ((float)node.X + 0.5f) * 1f;
			worldZ = WorldZ + ((float)node.Z + 0.5f) * 1f;
		}

		private static int RoundToInt(float value)
		{
			return (int)Math.Floor(value + 0.5f);
		}
	}
}
