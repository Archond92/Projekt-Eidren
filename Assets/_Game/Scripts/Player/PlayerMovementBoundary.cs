using Eidren.Core.Services;
using System;
using UnityEngine;

namespace Eidren.Player
{
	[Serializable]
	public struct PlayerMovementBoundary
	{
		[SerializeField]
		private MovementBoundaryShape shape;

		[SerializeField]
		private Vector3 center;

		[SerializeField]
		private float radius;

		[SerializeField]
		private Vector2 size;

		[SerializeField]
		private MapSideFlags openSides;

		public MovementBoundaryShape Shape => shape;

		public Vector3 Center => center;

		public float Radius => radius;

		public Vector2 Size => size;

		public MapSideFlags OpenSides => openSides;

		public bool HasHardBoundary => shape != MovementBoundaryShape.None;

		public static PlayerMovementBoundary Unbounded(MapSideFlags openMapSides = MapSideFlags.All)
		{
			return new PlayerMovementBoundary
			{
				shape = MovementBoundaryShape.None,
				openSides = openMapSides
			};
		}

		public static PlayerMovementBoundary Circle(Vector3 boundaryCenter, float boundaryRadius, MapSideFlags openMapSides = MapSideFlags.None)
		{
			return new PlayerMovementBoundary
			{
				shape = MovementBoundaryShape.Circle,
				center = boundaryCenter,
				radius = Mathf.Max(0f, boundaryRadius),
				openSides = openMapSides
			};
		}

		public static PlayerMovementBoundary Rectangle(Vector3 boundaryCenter, Vector2 boundarySize, MapSideFlags openMapSides = MapSideFlags.None)
		{
			return new PlayerMovementBoundary
			{
				shape = MovementBoundaryShape.Rectangle,
				center = boundaryCenter,
				size = new Vector2(Mathf.Max(0f, boundarySize.x), Mathf.Max(0f, boundarySize.y)),
				openSides = openMapSides
			};
		}

		public bool Contains(Vector3 position, float inset = 0f)
		{
			if (!HasHardBoundary)
			{
				return true;
			}
			if (shape == MovementBoundaryShape.Circle)
			{
				Vector2 offset = new Vector2(position.x - center.x, position.z - center.z);
				float num = Mathf.Max(0f, radius - inset);
				if (!(offset.sqrMagnitude <= num * num))
				{
					return IsDirectionOpen(offset);
				}
				return true;
			}
			Vector2 vector = size * 0.5f - Vector2.one * Mathf.Max(0f, inset);
			vector.x = Mathf.Max(0f, vector.x);
			vector.y = Mathf.Max(0f, vector.y);
			Vector3 vector2 = position - center;
			bool num2 = Mathf.Abs(vector2.x) <= vector.x || (vector2.x > vector.x && openSides.HasFlag(MapSideFlags.East)) || (vector2.x < 0f - vector.x && openSides.HasFlag(MapSideFlags.West));
			bool flag = Mathf.Abs(vector2.z) <= vector.y || (vector2.z > vector.y && openSides.HasFlag(MapSideFlags.North)) || (vector2.z < 0f - vector.y && openSides.HasFlag(MapSideFlags.South));
			return num2 && flag;
		}

		public Vector3 Constrain(Vector3 position)
		{
			if (!HasHardBoundary)
			{
				return position;
			}
			if (shape == MovementBoundaryShape.Circle)
			{
				return ConstrainCircle(position);
			}
			return ConstrainRectangle(position);
		}

		private Vector3 ConstrainCircle(Vector3 position)
		{
			Vector2 offset = new Vector2(position.x - center.x, position.z - center.z);
			if (offset.sqrMagnitude <= radius * radius || IsDirectionOpen(offset))
			{
				return position;
			}
			offset = offset.normalized * radius;
			position.x = center.x + offset.x;
			position.z = center.z + offset.y;
			return position;
		}

		private Vector3 ConstrainRectangle(Vector3 position)
		{
			Vector2 vector = size * 0.5f;
			if (!openSides.HasFlag(MapSideFlags.West))
			{
				position.x = Mathf.Max(position.x, center.x - vector.x);
			}
			if (!openSides.HasFlag(MapSideFlags.East))
			{
				position.x = Mathf.Min(position.x, center.x + vector.x);
			}
			if (!openSides.HasFlag(MapSideFlags.South))
			{
				position.z = Mathf.Max(position.z, center.z - vector.y);
			}
			if (!openSides.HasFlag(MapSideFlags.North))
			{
				position.z = Mathf.Min(position.z, center.z + vector.y);
			}
			return position;
		}

		private bool IsDirectionOpen(Vector2 offset)
		{
			if (Mathf.Abs(offset.x) > Mathf.Abs(offset.y))
			{
				if (!(offset.x >= 0f))
				{
					return openSides.HasFlag(MapSideFlags.West);
				}
				return openSides.HasFlag(MapSideFlags.East);
			}
			if (!(offset.y >= 0f))
			{
				return openSides.HasFlag(MapSideFlags.South);
			}
			return openSides.HasFlag(MapSideFlags.North);
		}
	}
}
