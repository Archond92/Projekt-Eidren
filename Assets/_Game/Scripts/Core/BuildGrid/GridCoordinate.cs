using System;

namespace Eidren.Core.BuildGrid
{
	public readonly struct GridCoordinate : IEquatable<GridCoordinate>
	{
		public int X { get; }

		public int Z { get; }

		public GridCoordinate(int x, int z)
		{
			X = x;
			Z = z;
		}

		public GridCoordinate Neighbour(GridDirection direction)
		{
			return direction switch
			{
				GridDirection.North => new GridCoordinate(X, Z + 1), 
				GridDirection.East => new GridCoordinate(X + 1, Z), 
				GridDirection.South => new GridCoordinate(X, Z - 1), 
				GridDirection.West => new GridCoordinate(X - 1, Z), 
				_ => throw new ArgumentOutOfRangeException("direction", direction, "Unbekannte Rasterrichtung."), 
			};
		}

		public bool Equals(GridCoordinate other)
		{
			return X == other.X && Z == other.Z;
		}

		public override bool Equals(object obj)
		{
			return obj is GridCoordinate other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (X * 397) ^ Z;
		}

		public override string ToString()
		{
			return $"({X}, {Z})";
		}

		public static bool operator ==(GridCoordinate left, GridCoordinate right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GridCoordinate left, GridCoordinate right)
		{
			return !left.Equals(right);
		}
	}
}
