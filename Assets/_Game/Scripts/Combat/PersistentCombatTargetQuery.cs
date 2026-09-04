using System;
using System.Collections.Generic;
using Eidren.AI;
using Eidren.Core;
using Eidren.Player;
using UnityEngine;

namespace Eidren.Combat
{
	/// <summary>Ein Zielzustand pro Spieler; Auswahl mit 10 Hz, harte Invalidierung pro Tick/Aktion.</summary>
	public sealed class PersistentCombatTargetQuery : ICombatTargetQuery
	{
		private const float EvaluationInterval = .1f;
		private readonly Transform _owner;
		private readonly PlayerMotor _motor;
		private readonly PlayerCombatController _combat;
		private readonly Damageable _health;
		private readonly CombatTargetSelectionSettings _settings;
		private readonly CombatTargetSelection _selection;
		private readonly List<IDamageable> _targets = new List<IDamageable>(64);
		private readonly List<CombatTargetCandidate> _candidates = new List<CombatTargetCandidate>(64);
		private readonly RaycastHit[] _obstacles = new RaycastHit[32];
		private CombatTargetSnapshot _current;
		private float _nextEvaluation;
		public event Action<CombatTargetSnapshot> Changed;
		public CombatTargetSnapshot Current
		{
			get { if (_current.Identity != 0 && (!OwnerValid() || !Valid(_current.Target.Damageable))) Clear(); return _current; }
		}

		public PersistentCombatTargetQuery(Transform owner, PlayerMotor motor = null, PlayerCombatController combat = null, Damageable health = null, CombatTargetSelectionSettings settings = null)
		{
			if (owner == null) throw new ArgumentNullException(nameof(owner));
			_owner = owner; _motor = motor; _combat = combat; _health = health;
			_settings = settings ?? new CombatTargetSelectionSettings(); _selection = new CombatTargetSelection(_settings);
		}

		public void Tick(float now, bool force = false)
		{
			if (!OwnerValid()) { Clear(); return; }
			if (_current.Identity != 0 && !Valid(_current.Target.Damageable)) { Clear(); force = true; }
			if (!force && now < _nextEvaluation) return;
			_nextEvaluation = now + EvaluationInterval;
			Scan(_settings.ReleaseRange);
			int id = _selection.Select(_candidates, now);
			IDamageable selected = Find(id);
			Publish(selected == null ? default : new CombatTargetSnapshot(selected, _owner.position, _selection.SelectedAt, _selection.ConfirmedAt,
				_current.Identity == id ? _current.DisplayName : null));
		}

		public bool TryFindClosest(Transform attacker, float range, out CombatTarget target)
		{
			target = default;
			if (attacker != _owner) return false;
			return Resolve(range, false, out target);
		}

		public bool TryFindEnemy(float range, out EnemyControllerBase enemy, Predicate<EnemyControllerBase> allowed = null)
		{
			enemy = null;
			if (!Resolve(range, true, out CombatTarget target, allowed)) return false;
			enemy = target.Damageable as EnemyControllerBase;
			return enemy != null;
		}

		public void ConfirmEngagement(CombatTarget target)
		{
			if (Valid(target.Damageable)) _selection.ConfirmEngagement(target.HitIdentity, Time.time);
		}

		public void Clear()
		{
			_selection.Clear(); _targets.Clear(); _candidates.Clear(); _nextEvaluation = 0f;
			Publish(default);
		}

		private bool Resolve(float range, bool enemyOnly, out CombatTarget result, Predicate<EnemyControllerBase> allowed = null)
		{
			result = default;
			if (!OwnerValid() || range <= 0 || float.IsNaN(range) || float.IsInfinity(range)) return false;
			Tick(Time.time, true);
			// Ein Faehigkeits-Fallback bleibt aktionslokal und veraendert die gemeinsame Auswahl nicht.
			Scan(Mathf.Max(_settings.ReleaseRange, range));
			IDamageable best = null; float bestScore = float.NegativeInfinity; int bestId = 0;
			for (int i = 0; i < _candidates.Count; i++)
			{
				CombatTargetCandidate candidate = _candidates[i];
				if (!candidate.Reachable || candidate.Distance > range) continue;
				IDamageable target = Find(candidate.Id);
				if (target == null || (enemyOnly && !(target is EnemyControllerBase))) continue;
				if (allowed != null && !allowed((EnemyControllerBase)target)) continue;
				if (candidate.Id == _current.Identity) { result = new CombatTarget(target); return true; }
				float score = _selection.Score(candidate, Time.time);
				if (score > bestScore || (score == bestScore && (bestId == 0 || candidate.Id < bestId)))
				{ best = target; bestScore = score; bestId = candidate.Id; }
			}
			if (best == null) return false;
			result = new CombatTarget(best); return true;
		}

		private void Scan(float range)
		{
			_targets.Clear(); _candidates.Clear();
			IReadOnlyList<IDamageable> registered = CombatTargetRegistry.Targets;
			for (int entry = 0; entry < registered.Count; entry++)
			{
				IDamageable target = registered[entry];
				if (target == null || (target is UnityEngine.Object obj && obj == null) || target.TargetTransform == null) continue;
				int duplicate = -1;
				for (int i = 0; i < _targets.Count; i++) if (_targets[i].TargetTransform == target.TargetTransform) { duplicate = i; break; }
				if (duplicate < 0) _targets.Add(target);
				else if (target is EnemyControllerBase) _targets[duplicate] = target;
			}
			Vector3 forward = _motor != null && _motor.WorldMoveDirection.sqrMagnitude > .01f ? _motor.WorldMoveDirection : _owner.forward;
			forward.y = 0; forward.Normalize();
			float weaponRange = 0f;
			if (_combat != null && _combat.ActiveWeapon != null)
				foreach (var step in _combat.ActiveWeapon.ResolvedMoveset.Combo) weaponRange = Mathf.Max(weaponRange, step.HitboxRange);
			foreach (IDamageable target in _targets)
			{
				if (!Valid(target)) continue;
				Vector3 point = CombatTargetGeometry.ClosestBodyPoint(target.TargetTransform, _owner.position);
				Vector3 offset = point - _owner.position; offset.y = 0;
				float distance = offset.magnitude;
				if (distance > range) continue;
				EnemyControllerBase enemy = target as EnemyControllerBase;
				_candidates.Add(new CombatTargetCandidate(target.TargetTransform.GetInstanceID(), distance,
					distance < .001f ? 1f : Vector3.Dot(forward, offset / distance), distance <= weaponRange,
					enemy is BossController || enemy is CoreGuardianController, Reachable(target.TargetTransform, point),
					enemy != null && enemy.EnemyState == EnemyState.Staggered ? .3f : 0f));
			}
		}

		private bool Reachable(Transform target, Vector3 point)
		{
			Vector3 start = _owner.position + Vector3.up * .8f;
			Vector3 delta = point + Vector3.up * .8f - start;
			int count = Physics.RaycastNonAlloc(start, delta.normalized, _obstacles, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
			if (count == _obstacles.Length) return false;
			for (int i = 0; i < count; i++)
			{
				Transform hit = _obstacles[i].transform;
				if (hit != null && hit.root != _owner.root && hit != target && !hit.IsChildOf(target)) return false;
			}
			return true;
		}

		private bool OwnerValid() => _owner != null && _owner.gameObject.activeInHierarchy && (_health == null || _health.IsAlive);
		private bool Valid(IDamageable target)
		{
			if (target == null || (target is UnityEngine.Object obj && obj == null) || !target.IsAlive) return false;
			Transform transform = target.TargetTransform;
			if (transform == null || !transform.gameObject.activeInHierarchy || transform.root == _owner.root) return false;
			if (target is Behaviour behaviour && !behaviour.isActiveAndEnabled) return false;
			if (target is EnemyControllerBase enemy && enemy.EnemyState == EnemyState.Return) return false;
			return !(target is BossController boss) || boss.BattleActive;
		}
		private IDamageable Find(int id)
		{
			if (id == 0) return null;
			foreach (IDamageable target in _targets) if (Valid(target) && target.TargetTransform.GetInstanceID() == id) return target;
			return null;
		}
		private void Publish(CombatTargetSnapshot value)
		{
			bool changed = !_current.SameVisibleState(value); _current = value;
			if (changed) Changed?.Invoke(value);
		}
	}
}
