using System;

namespace Eidren.Core.BuildGrid
{
	public readonly struct GridEdge : IEquatable<GridEdge>
	{
		public GridCoordinate Anchor { get; }

		public GridEdgeOrientation Orientation { get; }

		public GridCoordinate LowerCell => Anchor;

		public GridCoordinate UpperCell
		{
			get
			{
				if (Orientation != GridEdgeOrientation.NorthSouth)
				{
					return Anchor.Neighbour(GridDirection.North);
				}
				return Anchor.Neighbour(GridDirection.East);
			}
		}

		public GridNode LowerNode
		{
			get
			{
				if (Orientation != GridEdgeOrientation.NorthSouth)
				{
					return new GridNode(Anchor.X - 1, Anchor.Z);
				}
				return new GridNode(Anchor.X, Anchor.Z - 1);
			}
		}

		public GridNode UpperNode => new GridNode(Anchor.X, Anchor.Z);

		private GridEdge(GridCoordinate anchor, GridEdgeOrientation orientation)
		{
			Anchor = anchor;
			Orientation = orientation;
		}

		public static GridEdge From(GridCoordinate cell, GridDirection side)
		{
			return side switch
			{
				GridDirection.East => new GridEdge(cell, GridEdgeOrientation.NorthSouth), 
				GridDirection.West => new GridEdge(cell.Neighbour(GridDirection.West), GridEdgeOrientation.NorthSouth), 
				GridDirection.North => new GridEdge(cell, GridEdgeOrientation.EastWest), 
				GridDirection.South => new GridEdge(cell.Neighbour(GridDirection.South), GridEdgeOrientation.EastWest), 
				_ => throw new ArgumentOutOfRangeException("side", side, "Unbekannte Kantenseite."), 
			};
		}

		public static bool TryBetween(GridCoordinate first, GridCoordinate second, out GridEdge edge)
		{
			int num = second.X - first.X;
			int num2 = second.Z - first.Z;
			if (num2 == 0 && (num == 1 || num == -1))
			{
				edge = new GridEdge((num == 1) ? first : second, GridEdgeOrientation.NorthSouth);
				return true;
			}
			if (num == 0 && (num2 == 1 || num2 == -1))
			{
				edge = new GridEdge((num2 == 1) ? first : second, GridEdgeOrientation.EastWest);
				return true;
			}
			edge = default(GridEdge);
			return false;
		}

		public bool Equals(GridEdge other)
		{
			if (Anchor.Equals(other.Anchor))
			{
				return Orientation == other.Orientation;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is GridEdge other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (Anchor.GetHashCode() * 397) ^ (int)Orientation;
		}

		public override string ToString()
		{
			return $"{Orientation} {Anchor}";
		}

		public static bool operator ==(GridEdge left, GridEdge right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GridEdge left, GridEdge right)
		{
			return !left.Equals(right);
		}
	}
}
