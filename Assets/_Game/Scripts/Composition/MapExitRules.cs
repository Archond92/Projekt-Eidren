using Eidren.Core.Services;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class MapExitCountdown
	{
		private const float MaxStepFraction = 0.25f;

		private const int MinimumTicks = 3;

		private int _ticks;

		public bool IsRunning { get; private set; }

		public string ExitId { get; private set; } = string.Empty;

		public MapExitDirection Direction { get; private set; }

		public string TargetScene { get; private set; } = string.Empty;

		public float Duration { get; private set; }

		public float Remaining { get; private set; }

		public float Progress => (Duration <= 0f) ? 1f : Mathf.Clamp01(1f - Remaining / Duration);

		public bool Begin(string exitId, MapExitDirection direction, string targetScene, float duration)
		{
			if (IsRunning || string.IsNullOrWhiteSpace(exitId) || direction == MapExitDirection.None)
			{
				return false;
			}
			ExitId = exitId;
			Direction = direction;
			TargetScene = targetScene ?? string.Empty;
			Duration = Mathf.Max(0.01f, duration);
			Remaining = Duration;
			_ticks = 0;
			IsRunning = true;
			return true;
		}

		public void UpdateSelection(string exitId, MapExitDirection direction, string targetScene)
		{
			if (IsRunning && !string.IsNullOrWhiteSpace(exitId) && direction != MapExitDirection.None)
			{
				ExitId = exitId;
				Direction = direction;
				TargetScene = targetScene ?? string.Empty;
			}
		}

		public bool Tick(float deltaTime, bool fullyInsideSafeArea, bool participantAlive)
		{
			if (!IsRunning)
			{
				return false;
			}
			if (fullyInsideSafeArea || !participantAlive)
			{
				Cancel();
				return false;
			}
			float num = Mathf.Min(Mathf.Max(0f, deltaTime), Duration * 0.25f);
			Remaining = Mathf.Max(0f, Remaining - num);
			_ticks++;
			if (Remaining > 0f || _ticks < 3)
			{
				return false;
			}
			IsRunning = false;
			return true;
		}

		public void Cancel()
		{
			_ticks = 0;
			IsRunning = false;
			ExitId = string.Empty;
			Direction = MapExitDirection.None;
			TargetScene = string.Empty;
			Duration = 0f;
			Remaining = 0f;
		}
	}

	public static class MapExitDirectionResolver
	{
		private const float MinimumDirectionSqrMagnitude = 0.0025f;

		public static MapExitDirection Resolve(Vector3 currentWorldMovement, Vector3 lastValidWorldMovement, MapSideFlags availableSides)
		{
			Vector3 vector = ((currentWorldMovement.sqrMagnitude >= 0.0025f) ? currentWorldMovement : lastValidWorldMovement);
			vector.y = 0f;
			if (vector.sqrMagnitude < 0.0025f)
			{
				vector = Vector3.forward;
			}
			bool flag = Mathf.Abs(vector.x) > Mathf.Abs(vector.z);
			MapExitDirection mapExitDirection = ((!flag) ? ((vector.z >= 0f) ? MapExitDirection.North : MapExitDirection.South) : ((vector.x >= 0f) ? MapExitDirection.East : MapExitDirection.West));
			if (Contains(availableSides, mapExitDirection))
			{
				return mapExitDirection;
			}
			MapExitDirection mapExitDirection2 = ((!flag) ? ((vector.x >= 0f) ? MapExitDirection.East : MapExitDirection.West) : ((vector.z >= 0f) ? MapExitDirection.North : MapExitDirection.South));
			if (Contains(availableSides, mapExitDirection2))
			{
				return mapExitDirection2;
			}
			MapExitDirection[] array = new MapExitDirection[4]
			{
				MapExitDirection.North,
				MapExitDirection.East,
				MapExitDirection.South,
				MapExitDirection.West
			};
			foreach (MapExitDirection mapExitDirection3 in array)
			{
				if (Contains(availableSides, mapExitDirection3))
				{
					return mapExitDirection3;
				}
			}
			return MapExitDirection.None;
		}

		public static MapSideFlags ToFlag(MapExitDirection direction)
		{
			return direction switch
			{
				MapExitDirection.North => MapSideFlags.North, 
				MapExitDirection.East => MapSideFlags.East, 
				MapExitDirection.South => MapSideFlags.South, 
				MapExitDirection.West => MapSideFlags.West, 
				_ => MapSideFlags.None, 
			};
		}

		private static bool Contains(MapSideFlags sides, MapExitDirection direction)
		{
			return (sides & ToFlag(direction)) != 0;
		}
	}
}
