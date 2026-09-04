using Eidren.Core.Services;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class ZonePhysicsPlacementSpace : IZonePlacementSpace
	{
		private const float SpawnClearance = 5f;

		private const float GroundProbeHeight = 2f;

		private const float GroundProbeDistance = 5f;

		private const float ProbeLift = 0.8f;

		private const float HomeBuildAreaRadius = 12f;

		private readonly Collider _ground;

		private readonly bool _reserveHomeBuildArea;

		private readonly Vector3 _homeBuildAreaCenter;

		private readonly List<Vector3> _spawnPositions = new List<Vector3>();

		private readonly List<Bounds> _exitBounds = new List<Bounds>();

		private static readonly MapExitDirection[] ExitDirections = new MapExitDirection[5]
		{
			MapExitDirection.None,
			MapExitDirection.North,
			MapExitDirection.East,
			MapExitDirection.South,
			MapExitDirection.West
		};

		public ZonePhysicsPlacementSpace(ZoneController zone)
		{
			_ground = ((zone != null) ? zone.WalkableGround : null);
			if (zone == null)
			{
				return;
			}
			_reserveHomeBuildArea = zone.Definition != null && string.Equals(zone.Definition.Id, "home_base", StringComparison.Ordinal);
			if (_ground != null)
			{
				_homeBuildAreaCenter = _ground.bounds.center;
			}
			MapExitDirection[] exitDirections = ExitDirections;
			foreach (MapExitDirection entryDirection in exitDirections)
			{
				Transform spawnForEntry = zone.GetSpawnForEntry(entryDirection);
				if (spawnForEntry != null)
				{
					_spawnPositions.Add(spawnForEntry.position);
				}
			}
			foreach (MapExitVolume exitVolume in zone.ExitVolumes)
			{
				if (exitVolume != null)
				{
					_exitBounds.Add(exitVolume.TriggerBounds);
				}
			}
		}

		public bool IsPlaceable(Vector3 position, float clearance)
		{
			if (_ground == null)
			{
				return false;
			}
			Bounds bounds = _ground.bounds;
			if (position.x < bounds.min.x || position.x > bounds.max.x || position.z < bounds.min.z || position.z > bounds.max.z)
			{
				return false;
			}
			foreach (Bounds exitBound in _exitBounds)
			{
				if (exitBound.Contains(position))
				{
					return false;
				}
			}
			foreach (Vector3 spawnPosition in _spawnPositions)
			{
				if (new Vector3(spawnPosition.x - position.x, 0f, spawnPosition.z - position.z).sqrMagnitude < 25f)
				{
					return false;
				}
			}
			if (_reserveHomeBuildArea)
			{
				Vector3 vector = position - _homeBuildAreaCenter;
				vector.y = 0f;
				if (vector.sqrMagnitude < 144f)
				{
					return false;
				}
			}
			Ray ray = new Ray(position + Vector3.up * 2f, Vector3.down);
			if (!_ground.Raycast(ray, out var _, 5f))
			{
				return false;
			}
			Collider[] array = Physics.OverlapSphere(position + Vector3.up * 0.8f, clearance, -5, QueryTriggerInteraction.Ignore);
			foreach (Collider collider in array)
			{
				if (collider != null && collider != _ground)
				{
					return false;
				}
			}
			return true;
		}
	}
}
