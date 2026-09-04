using System;
using UnityEngine;

namespace Eidren.Eidra
{
	public static class EidraAbilityRules
	{
		private static readonly float[] ShadowStepSearchAngles = new float[5] { 0f, 25f, -25f, 50f, -50f };

		public static bool IsTargetInRange(Vector3 source, Transform target, bool targetIsAlive, bool targetIsActive, float range)
		{
			if (target == null || !targetIsAlive || !targetIsActive || range <= 0f)
			{
				return false;
			}
			return FlatDistance(source, target.position) <= range;
		}

		public static bool TryFindShadowStepDestination(Vector3 source, Transform target, bool targetIsAlive, bool targetIsActive, float range, float behindDistance, Func<Vector3, bool> isPositionFree, out Vector3 destination)
		{
			destination = default(Vector3);
			if (!IsTargetInRange(source, target, targetIsAlive, targetIsActive, range) || behindDistance <= 0f || isPositionFree == null)
			{
				return false;
			}
			Vector3 forward = target.forward;
			forward.y = 0f;
			if (forward.sqrMagnitude < 0.001f)
			{
				forward = Vector3.forward;
			}
			forward.Normalize();
			Vector3 vector = -forward;
			float[] shadowStepSearchAngles = ShadowStepSearchAngles;
			foreach (float y in shadowStepSearchAngles)
			{
				Vector3 vector2 = Quaternion.Euler(0f, y, 0f) * vector * behindDistance;
				Vector3 vector3 = target.position + vector2;
				vector3.y = source.y;
				if (isPositionFree(vector3))
				{
					destination = vector3;
					return true;
				}
			}
			return false;
		}

		public static float FlatDistance(Vector3 first, Vector3 second)
		{
			first.y = 0f;
			second.y = 0f;
			return Vector3.Distance(first, second);
		}
	}
}
