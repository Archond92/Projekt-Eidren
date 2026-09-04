using System;

namespace Eidren.Core.BuildGrid
{
	public readonly struct GridWallConnection : IEquatable<GridWallConnection>
	{
		private static readonly GridDirection[] Directions = new GridDirection[4]
		{
			GridDirection.North,
			GridDirection.East,
			GridDirection.South,
			GridDirection.West
		};

		public GridWallShape Lower { get; }

		public GridWallShape Upper { get; }

		public GridWallConnection(GridWallShape lower, GridWallShape upper)
		{
			Lower = lower;
			Upper = upper;
		}

		public static GridWallConnection Classify(GridEdge edge, GridOccupancy occupancy)
		{
			if (occupancy == null)
			{
				throw new ArgumentNullException("occupancy");
			}
			return new GridWallConnection(ShapeAt(edge, edge.LowerNode, occupancy), ShapeAt(edge, edge.UpperNode, occupancy));
		}

		private static GridWallShape ShapeAt(GridEdge edge, GridNode node, GridOccupancy occupancy)
		{
			int num = 0;
			bool flag = false;
			GridDirection[] directions = Directions;
			foreach (GridDirection direction in directions)
			{
				GridEdge gridEdge = node.Arm(direction);
				if (!(gridEdge == edge) && occupancy.IsEdgeOccupied(gridEdge))
				{
					num++;
					if (gridEdge.Orientation == edge.Orientation)
					{
						flag = true;
					}
				}
			}
			switch (num)
			{
			case 0:
				return GridWallShape.EndCap;
			case 1:
				if (!flag)
				{
					return GridWallShape.Corner;
				}
				return GridWallShape.Straight;
			case 2:
				return GridWallShape.Tee;
			default:
				return GridWallShape.Cross;
			}
		}

		public bool Equals(GridWallConnection other)
		{
			if (Lower == other.Lower)
			{
				return Upper == other.Upper;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is GridWallConnection other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((int)Lower * 397) ^ (int)Upper;
		}

		public override string ToString()
		{
			return $"{Lower} | {Upper}";
		}

		public static bool operator ==(GridWallConnection left, GridWallConnection right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GridWallConnection left, GridWallConnection right)
		{
			return !left.Equals(right);
		}
	}
}
