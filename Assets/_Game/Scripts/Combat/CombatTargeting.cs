using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Combat
{
	public static class CombatHitResolver
	{
		public static bool TryApply(CombatTarget target, DamageInfo damage, ISet<int> hitTargetIds)
		{
			if (target.Damageable == null || target.Transform == null || !target.IsAlive)
			{
				return false;
			}
			int hitIdentity = target.HitIdentity;
			if (hitTargetIds != null && !hitTargetIds.Add(hitIdentity))
			{
				return false;
			}
			target.Damageable.ApplyDamage(damage);
			return true;
		}
	}

	public readonly struct CombatTarget
	{
		public IDamageable Damageable { get; }

		public Transform Transform => Damageable.TargetTransform;

		public bool IsAlive => Damageable.IsAlive;

		public int HitIdentity => Transform.GetInstanceID();

		public CombatTarget(IDamageable damageable)
		{
			Damageable = damageable ?? throw new ArgumentNullException("damageable");
		}

		public bool IsBackAttack(Transform attacker, float threshold = -0.35f)
		{
			if (Damageable is IBackAttackTarget backAttackTarget)
			{
				return backAttackTarget.IsBackAttack(attacker, threshold);
			}
			return CombatTargetGeometry.IsBackAttack(Transform, attacker, threshold);
		}
	}

	public static class CombatTargetGeometry
	{
		private static readonly List<Collider> ColliderBuffer = new List<Collider>(16);

		private static readonly List<Renderer> RendererBuffer = new List<Renderer>(16);

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetBuffers()
		{
			ColliderBuffer.Clear();
			RendererBuffer.Clear();
		}

		public static Vector3 ClosestBodyPoint(Transform target, Vector3 origin)
		{
			if (target == null)
			{
				return origin;
			}
			Vector3 point = target.position;
			float best = float.PositiveInfinity;
			bool found = false;
			ColliderBuffer.Clear();
			target.GetComponentsInChildren(false, ColliderBuffer);
			foreach (Collider collider in ColliderBuffer)
			{
				if (collider == null || !collider.enabled || collider.isTrigger)
				{
					continue;
				}
				Vector3 candidate = collider.bounds.ClosestPoint(origin);
				float distance = FlatSqrDistance(origin, candidate);
				if (distance < best)
				{
					best = distance;
					point = candidate;
					found = true;
				}
			}
			if (found)
			{
				return point;
			}
			RendererBuffer.Clear();
			target.GetComponentsInChildren(false, RendererBuffer);
			foreach (Renderer renderer in RendererBuffer)
			{
				if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
				{
					continue;
				}
				Vector3 candidate = renderer.bounds.ClosestPoint(origin);
				float distance = FlatSqrDistance(origin, candidate);
				if (distance < best)
				{
					best = distance;
					point = candidate;
				}
			}
			return point;
		}

		public static Vector3 ClosestBodyPoint(Collider collider, Vector3 origin)
		{
			return collider == null ? origin : collider.bounds.ClosestPoint(origin);
		}

		public static float FlatDistanceToBody(Vector3 origin, Transform target)
		{
			return Mathf.Sqrt(FlatSqrDistance(origin, ClosestBodyPoint(target, origin)));
		}

		public static bool IsBackAttack(Transform target, Transform attacker, float threshold = -0.35f)
		{
			if (target == null || attacker == null)
			{
				return false;
			}
			Vector3 vector = attacker.position - target.position;
			vector.y = 0f;
			if (vector.sqrMagnitude < 0.001f)
			{
				return false;
			}
			return Vector3.Dot(target.forward, vector.normalized) < threshold;
		}

		private static float FlatSqrDistance(Vector3 first, Vector3 second)
		{
			float x = first.x - second.x;
			float z = first.z - second.z;
			return x * x + z * z;
		}
	}

	public static class CombatTargetRegistry
	{
		private static readonly List<IDamageable> RegisteredTargets = new List<IDamageable>();

		public static IReadOnlyList<IDamageable> Targets => RegisteredTargets;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			RegisteredTargets.Clear();
		}

		public static void Register(IDamageable target)
		{
			if (target != null && !RegisteredTargets.Contains(target))
			{
				RegisteredTargets.Add(target);
			}
		}

		public static void Unregister(IDamageable target)
		{
			if (target != null)
			{
				RegisteredTargets.Remove(target);
			}
		}

		public static bool TryResolve(Transform colliderTransform, Transform excludedRoot, IDamageable intendedTarget, out IDamageable result)
		{
			result = null;
			if (colliderTransform == null)
			{
				return false;
			}
			foreach (IDamageable registeredTarget in RegisteredTargets)
			{
				if (registeredTarget != null && registeredTarget.IsAlive && (intendedTarget == null || registeredTarget == intendedTarget))
				{
					Transform targetTransform = registeredTarget.TargetTransform;
					if (!(targetTransform == null) && !(targetTransform.root == excludedRoot) && (!(colliderTransform != targetTransform) || colliderTransform.IsChildOf(targetTransform)) && (result == null || (registeredTarget is IStaggerable && !(result is IStaggerable))))
					{
						result = registeredTarget;
					}
				}
			}
			return result != null;
		}
	}

	public interface ICombatTargetQuery
	{
		bool TryFindClosest(Transform attacker, float range, out CombatTarget target);
	}

	public sealed class SceneCombatTargetQuery : ICombatTargetQuery
	{
		public bool TryFindClosest(Transform attacker, float range, out CombatTarget target)
		{
			target = default(CombatTarget);
			if (attacker == null || range <= 0f)
			{
				return false;
			}
			IDamageable damageable = null;
			float num = range;
			foreach (IDamageable target2 in CombatTargetRegistry.Targets)
			{
				if (target2 == null || !target2.IsAlive)
				{
					continue;
				}
				Transform targetTransform = target2.TargetTransform;
				if (targetTransform == null || targetTransform.root == attacker.root)
				{
					continue;
				}
				float distance = CombatTargetGeometry.FlatDistanceToBody(attacker.position, targetTransform);
				if (!(distance > num))
				{
					bool flag = damageable != null && damageable.TargetTransform == targetTransform && target2 is IStaggerable && !(damageable is IStaggerable);
					if (damageable == null || distance < num || flag)
					{
						damageable = target2;
						num = distance;
					}
				}
			}
			if (damageable == null)
			{
				return false;
			}
			target = new CombatTarget(damageable);
			return true;
		}
	}
}
