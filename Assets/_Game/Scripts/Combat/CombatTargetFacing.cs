using Eidren.Data;
using Eidren.Core;
using UnityEngine;

namespace Eidren.Combat
{
	public static class CombatTargetFacing
	{
		// Nur am Aktionsstart aufgerufen: keine Bewegung, kein Auto-Angriff und keine freie Verfolgung.
		public static void Apply(Transform owner, ICombatTargetQuery query, AttackStepData step, WeaponData moveset)
		{
			if (!query.TryFindClosest(owner, step.HitboxRange * 1.5f, out CombatTarget target)) return;
			Vector3 direction = CombatTargetGeometry.ClosestBodyPoint(target.Transform, owner.position) - owner.position;
			direction.y = 0f;
			if (direction.sqrMagnitude <= .001f) return;
			float targetAngle = Vector3.Angle(owner.forward, direction.normalized);
			if (targetAngle > moveset.AttackAssistAngle * .5f) return;
			CombatTargetFacingProfile profile = CombatTargetFacingRules.For(moveset.Identity.Family);
			float turnDegrees = CombatTargetFacingRules.ResolveTurnDegrees(targetAngle, profile);
			owner.rotation = Quaternion.RotateTowards(owner.rotation, Quaternion.LookRotation(direction.normalized), turnDegrees);
			if (query is PersistentCombatTargetQuery persistent) persistent.ConfirmEngagement(target);
		}
	}
}
