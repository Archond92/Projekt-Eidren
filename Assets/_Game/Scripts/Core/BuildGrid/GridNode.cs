using System;

namespace Eidren.Core.BuildGrid
{
	public readonly struct GridNode : IEquatable<GridNode>
	{
		public int X { get; }

		public int Z { get; }

		public GridNode(int x, int z)
		{
			X = x;
			Z = z;
		}

		public GridEdge Arm(GridDirection direction)
		{
			return direction switch
			{
				GridDirection.North => GridEdge.From(new GridCoordinate(X, Z + 1), GridDirection.East), 
				GridDirection.East => GridEdge.From(new GridCoordinate(X + 1, Z), GridDirection.North), 
				GridDirection.South => GridEdge.From(new GridCoordinate(X, Z), GridDirection.East), 
				GridDirection.West => GridEdge.From(new GridCoordinate(X, Z), GridDirection.North), 
				_ => throw new ArgumentOutOfRangeException("direction", direction, "Unbekannte Rasterrichtung."), 
			};
		}

		public bool Equals(GridNode other)
		{
			if (X == other.X)
			{
				return Z == other.Z;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is GridNode other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (X * 397) ^ Z;
		}

		public override string ToString()
		{
			return $"Knoten ({X}, {Z})";
		}

		public static bool operator ==(GridNode left, GridNode right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GridNode left, GridNode right)
		{
			return !left.Equals(right);
		}
	}
}
