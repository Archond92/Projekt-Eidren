using Eidren.AI;
using Eidren.Data;
using UnityEngine;

namespace Eidren.Combat
{
	public enum CombatTargetCategory { Normal, Elite, Boss }
	public readonly struct CombatTargetVitals
	{
		public readonly Vector2 Health, Stagger;
		public readonly bool Alive, Active;
		public CombatTargetVitals(Vector2 health, Vector2 stagger, bool alive, bool active)
		{ Health = health; Stagger = stagger; Alive = alive; Active = active; }
	}

	public readonly struct CombatTargetSnapshot
	{
		public readonly int Identity;
		public readonly CombatTarget Target;
		public readonly Vector3 Point;
		public readonly string DisplayName;
		public readonly CombatTargetCategory Category;
		public readonly CombatTargetVitals Vitals;
		public readonly EnemyState State;
		public readonly float Distance, SelectedAt, ConfirmedAt;
		public CombatTargetSnapshot(IDamageable target, Vector3 origin, float selectedAt, float confirmedAt, string cachedName = null)
		{
			Target = new CombatTarget(target); Identity = Target.HitIdentity;
			Point = CombatTargetGeometry.ClosestBodyPoint(Target.Transform, origin);
			Vector3 delta = Point - origin; delta.y = 0; Distance = delta.magnitude;
			EnemyControllerBase enemy = target as EnemyControllerBase;
			Damageable health = target as Damageable;
			Category = enemy is BossController || enemy is CoreGuardianController ? CombatTargetCategory.Boss
				: enemy is WildlingController wildling && wildling.Definition != null && wildling.Definition.CombatStyle == EnemyCombatStyle.AreaElite
				? CombatTargetCategory.Elite : CombatTargetCategory.Normal;
			DisplayName = cachedName ?? (enemy != null && !string.IsNullOrWhiteSpace(enemy.DisplayName) ? enemy.DisplayName
				: enemy is BossController ? "Garon" : Target.Transform.name);
			State = enemy != null ? enemy.EnemyState : EnemyState.Idle;
			Vitals = new CombatTargetVitals(enemy != null ? new Vector2(enemy.CurrentHealth, enemy.MaxHealth)
				: health != null ? new Vector2(health.CurrentHealth, health.MaxHealth) : Vector2.zero,
				enemy != null ? new Vector2(enemy.CurrentStagger, enemy.MaxStagger) : Vector2.zero,
				target.IsAlive, Target.Transform.gameObject.activeInHierarchy);
			SelectedAt = selectedAt; ConfirmedAt = confirmedAt;
		}

		public bool SameVisibleState(CombatTargetSnapshot other) => Identity == other.Identity && Point == other.Point
			&& DisplayName == other.DisplayName && Category == other.Category && State == other.State
			&& Vitals.Health == other.Vitals.Health && Vitals.Stagger == other.Vitals.Stagger
			&& Vitals.Alive == other.Vitals.Alive && Vitals.Active == other.Vitals.Active && Distance == other.Distance;
	}
}
