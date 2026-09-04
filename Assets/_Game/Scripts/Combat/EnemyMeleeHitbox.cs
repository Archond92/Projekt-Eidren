using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Combat
{
	[DisallowMultipleComponent]
	public sealed class EnemyMeleeHitbox : MonoBehaviour
	{
		private const int OverlapCapacity = 16;

		[SerializeField]
		private BoxCollider hitbox;

		[SerializeField]
		private LayerMask targetLayers = -1;

		private readonly Collider[] _overlaps = new Collider[16];

		private readonly HashSet<int> _hitTargetIds = new HashSet<int>();

		public bool IsWindowActive { get; private set; }

		public int HitCount => _hitTargetIds.Count;

		public BoxCollider Hitbox => hitbox;

		public void Configure(BoxCollider configuredHitbox)
		{
			hitbox = configuredHitbox ?? throw new ArgumentNullException("configuredHitbox");
			hitbox.isTrigger = true;
			hitbox.enabled = false;
		}

		public void BeginWindow()
		{
			if (hitbox == null)
			{
				throw new InvalidOperationException("Enemy melee hitbox is not configured.");
			}
			_hitTargetIds.Clear();
			IsWindowActive = true;
			hitbox.enabled = true;
		}

		public int Evaluate(Transform attacker, IDamageable intendedTarget, Action<CombatTarget> onTarget)
		{
			if (!IsWindowActive || attacker == null || intendedTarget == null || onTarget == null || hitbox == null)
			{
				return 0;
			}
			Vector3 center = hitbox.transform.TransformPoint(hitbox.center);
			Vector3 lossyScale = hitbox.transform.lossyScale;
			Vector3 halfExtents = Vector3.Scale(hitbox.size * 0.5f, new Vector3(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
			int num = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlaps, hitbox.transform.rotation, targetLayers, QueryTriggerInteraction.Collide);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				if (TryResolve(_overlaps[i], attacker, intendedTarget, out var target) && _hitTargetIds.Add(target.HitIdentity))
				{
					onTarget(target);
					num2++;
				}
			}
			return num2;
		}

		public void EndWindow()
		{
			IsWindowActive = false;
			if (hitbox != null)
			{
				hitbox.enabled = false;
			}
		}

		private static bool TryResolve(Collider overlap, Transform attacker, IDamageable intendedTarget, out CombatTarget target)
		{
			target = default(CombatTarget);
			if (overlap == null)
			{
				return false;
			}
			if (!CombatTargetRegistry.TryResolve(overlap.transform, attacker.root, intendedTarget, out var result))
			{
				return false;
			}
			target = new CombatTarget(result);
			return true;
		}

		private void OnDisable()
		{
			EndWindow();
		}
	}
}
