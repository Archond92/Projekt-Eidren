using Eidren.AI;
using System;
using UnityEngine.AI;
using UnityEngine;

namespace Eidren.Composition
{
	public static class LootDropPositionResolver
	{
		private const int MaximumAttempts = 24;

		private static readonly Collider[] OverlapBuffer = new Collider[24];

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetBuffer()
		{
			Array.Clear(OverlapBuffer, 0, OverlapBuffer.Length);
		}

		public static bool TryResolve(ZoneController zone, Vector3 deathPosition, Transform ignoredSource, int dropIndex, out Vector3 position)
		{
			position = deathPosition;
			if (zone == null || zone.WalkableGround == null)
			{
				return false;
			}
			for (int i = 0; i < 24; i++)
			{
				float num = 0.95f + (float)(i / 8) * 0.72f;
				float f = ((float)dropIndex * 137.5f + (float)i * 47f) * ((float)Math.PI / 180f);
				Vector3 sourcePosition = deathPosition + new Vector3(Mathf.Cos(f) * num, 0f, Mathf.Sin(f) * num);
				if (NavMesh.SamplePosition(sourcePosition, out var hit, 1.6f, -1))
				{
					sourcePosition = hit.position;
					if (IsInsideWalkableGround(zone, sourcePosition) && !IsInsideExit(zone, sourcePosition) && !IsBlocked(zone, sourcePosition, ignoredSource))
					{
						position = sourcePosition + Vector3.up * 0.08f;
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsInsideWalkableGround(ZoneController zone, Vector3 position)
		{
			Bounds bounds = zone.WalkableGround.bounds;
			if (position.x < bounds.min.x + 0.45f || position.x > bounds.max.x - 0.45f || position.z < bounds.min.z + 0.45f || position.z > bounds.max.z - 0.45f)
			{
				return false;
			}
			Ray ray = new Ray(position + Vector3.up * 2f, Vector3.down);
			RaycastHit hitInfo;
			return zone.WalkableGround.Raycast(ray, out hitInfo, 4f);
		}

		private static bool IsInsideExit(ZoneController zone, Vector3 position)
		{
			foreach (MapExitVolume exitVolume in zone.ExitVolumes)
			{
				if (!(exitVolume == null))
				{
					Bounds triggerBounds = exitVolume.TriggerBounds;
					triggerBounds.Expand(new Vector3(0.8f, 0f, 0.8f));
					if (position.x >= triggerBounds.min.x && position.x <= triggerBounds.max.x && position.z >= triggerBounds.min.z && position.z <= triggerBounds.max.z)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsBlocked(ZoneController zone, Vector3 position, Transform ignoredSource)
		{
			int num = Physics.OverlapSphereNonAlloc(position + Vector3.up * 0.42f, 0.36f, OverlapBuffer, -5, QueryTriggerInteraction.Ignore);
			bool result = false;
			for (int i = 0; i < num; i++)
			{
				Collider collider = OverlapBuffer[i];
				OverlapBuffer[i] = null;
				if (!(collider == null) && !(collider == zone.WalkableGround) && !IsPartOf(collider.transform, ignoredSource) && !(collider.GetComponentInParent<EnemyControllerBase>() != null))
				{
					result = true;
				}
			}
			return result;
		}

		private static bool IsPartOf(Transform candidate, Transform root)
		{
			if (root != null)
			{
				if (!(candidate == root))
				{
					return candidate.IsChildOf(root);
				}
				return true;
			}
			return false;
		}
	}
}
