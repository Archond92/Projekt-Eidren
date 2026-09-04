using Eidren.AI;
using Eidren.Combat;
using UnityEngine;

namespace Eidren.Eidra
{
	public static class EidraCombatTargetResolver
	{
		public static EnemyControllerBase Resolve(PersistentCombatTargetQuery shared, Transform owner, EnemyControllerBase current, BossController boss, float range)
		{
			if (shared != null) return shared.TryFindEnemy(range, out EnemyControllerBase selected,
				enemy => EidraAbilityRules.IsTargetInRange(owner.position, enemy.transform, enemy.IsAlive, enemy.isActiveAndEnabled, range)) ? selected : null;
			// Kompatibilitaet fuer isolierte Alt-Tests/Aufbauten ohne produktive Komposition.
			if (boss != null && boss.IsAlive && boss.BattleActive && EidraAbilityRules.FlatDistance(owner.position, boss.transform.position) <= range) return boss;
			if (range <= 0) return current;
			if (current != null && !(current is BossController) && current.IsAlive && current.isActiveAndEnabled
				&& EidraAbilityRules.FlatDistance(owner.position, current.transform.position) <= range) return current;
			EnemyControllerBase nearest = null; float best = range * range;
			foreach (IDamageable candidate in CombatTargetRegistry.Targets)
			{
				EnemyControllerBase enemy = candidate as EnemyControllerBase;
				if (enemy == null || !enemy.IsAlive || !enemy.isActiveAndEnabled || enemy.transform.root == owner.root) continue;
				Vector3 offset = enemy.transform.position - owner.position; offset.y = 0;
				if (offset.sqrMagnitude <= best) { best = offset.sqrMagnitude; nearest = enemy; }
			}
			return nearest != null ? nearest : current;
		}
	}
}
