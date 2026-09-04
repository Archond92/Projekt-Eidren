using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Presentation;
using System.Collections;
using UnityEngine;

namespace Eidren.AI
{
	public sealed class CoreGuardianController : EnemyControllerBase
	{
		private CoreGuardianData _data;

		private Damageable _playerHealth;

		private CoreGuardianCycle _cycle;

		private int _nextAttack;

		public CoreGuardianPhase Phase => _cycle?.Phase ?? CoreGuardianPhase.Closed;

		public override string DisplayName => "Kernwächter";

		public void Initialize(CoreGuardianData data, Transform player, Damageable playerHealth)
		{
			_data = data;
			_playerHealth = playerHealth;
			_cycle = new CoreGuardianCycle();
			InitializeEnemy(data.Id, data.MaximumHealth, data.MaximumStagger, data.StaggerOpenSeconds, player, playerHealth, data.Navigation);
			ApplyProtection();
		}

		public void BeginBattle()
		{
			ActivateCombat();
		}

		public override void ApplyDamage(DamageInfo damage)
		{
			base.ApplyDamage(damage);
			if (base.IsAlive && _cycle != null && _cycle.AddStagger(Mathf.Max(0f, damage.StaggerDamage)))
			{
				ApplyProtection();
				CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 4.5f, "KERN OFFEN · 6 SEKUNDEN", new Color(1f, 0.48f, 0.08f), 1.5f);
			}
		}

		protected override IEnumerator ExecuteAttack()
		{
			if (_cycle == null || _data == null)
			{
				yield break;
			}
			if (_cycle.Phase == CoreGuardianPhase.Open)
			{
				while (_cycle.Phase == CoreGuardianPhase.Open && base.IsAlive)
				{
					yield return null;
				}
			}
			else if (_cycle.Phase == CoreGuardianPhase.Overheating)
			{
				yield return OverheatAndOpen();
			}
			else if (_data.Attacks.Count != 0)
			{
				CoreGuardianAttackData attack = _data.Attacks[_nextAttack++ % _data.Attacks.Count];
				Vector3 center = ((base.TrackedTarget != null) ? base.TrackedTarget.position : base.transform.position);
				yield return TelegraphAndDamage(center, attack.Radius, attack.Damage, attack.TelegraphSeconds, AttackLabel(attack.Type), AttackColor(attack.Type), $"core_guardian.{attack.Type}");
				if (_cycle.RecordClosedAttack())
				{
					yield return OverheatAndOpen();
				}
				else
				{
					yield return new WaitForSeconds(0.9f * _cycle.IntervalMultiplier(base.CurrentHealth / base.MaxHealth));
				}
			}
		}

		private IEnumerator OverheatAndOpen()
		{
			ApplyProtection();
			CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 4.8f, "ÜBERHITZUNG", new Color(1f, 0.22f, 0.03f), _data.OverheatSeconds);
			int count = _cycle.HazardCount(base.CurrentHealth / base.MaxHealth);
			Vector3 first = ((base.TrackedTarget != null) ? base.TrackedTarget.position : base.transform.position);
			Vector3 second = first + base.transform.right * 3.2f;
			GameObject markerA = HazardMarker(first, _data.OverheatHazardRadius);
			GameObject markerB = ((count > 1) ? HazardMarker(second, _data.OverheatHazardRadius) : null);
			yield return new WaitForSeconds(_data.OverheatSeconds);
			if (_playerHealth != null && _playerHealth.IsAlive && (FlatDistance(first, _playerHealth.transform.position) <= _data.OverheatHazardRadius || (count > 1 && FlatDistance(second, _playerHealth.transform.position) <= _data.OverheatHazardRadius)))
			{
				_playerHealth.ApplyDamage(new DamageInfo(_data.OverheatHazardDamage, 0f, _playerHealth.transform.position, base.gameObject, isBackAttack: false, "core_guardian.overheat", "enemy.core_guardian"));
			}
			Object.Destroy(markerA);
			if (markerB != null)
			{
				Object.Destroy(markerB);
			}
			_cycle.FinishOverheat();
			ApplyProtection();
			CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 4.8f, "KERN OFFEN · 8 SEKUNDEN", new Color(1f, 0.55f, 0.1f), 1.5f);
			while (_cycle.Phase == CoreGuardianPhase.Open && base.IsAlive)
			{
				yield return null;
			}
		}

		private IEnumerator TelegraphAndDamage(Vector3 center, float radius, float damage, float seconds, string label, Color color, string attackId)
		{
			GameObject marker = new GameObject("CoreGuardianTelegraph");
			marker.transform.position = center;
			CombatFeedback.SpawnAura(marker.transform, color, seconds, label, radius, 4.8f);
			yield return new WaitForSeconds(seconds);
			if (_playerHealth != null && _playerHealth.IsAlive && FlatDistance(center, _playerHealth.transform.position) <= radius)
			{
				_playerHealth.ApplyDamage(new DamageInfo(damage, 0f, _playerHealth.transform.position, base.gameObject, isBackAttack: false, attackId, "enemy.core_guardian"));
			}
			Object.Destroy(marker);
		}

		private GameObject HazardMarker(Vector3 center, float radius)
		{
			GameObject gameObject = new GameObject("OverheatHazard");
			gameObject.transform.position = center;
			CombatFeedback.SpawnAura(gameObject.transform, new Color(1f, 0.12f, 0.01f, 0.92f), _data.OverheatSeconds, "ÜBERHITZTE FLÄCHE", radius, 4.8f);
			return gameObject;
		}

		private void Update()
		{
			if (_cycle != null && _cycle.Phase == CoreGuardianPhase.Open && _cycle.Tick(Time.deltaTime))
			{
				ApplyProtection();
			}
		}

		private void ApplyProtection()
		{
			Damageable component = GetComponent<Damageable>();
			if (component != null)
			{
				component.Protection = _cycle?.Protection ?? 0.35f;
			}
		}

		private static string AttackLabel(CoreGuardianAttackType type)
		{
			if (1 == 0)
			{
			}
			string result = type switch
			{
				CoreGuardianAttackType.HammerSlam => "KERNSCHLAG", 
				CoreGuardianAttackType.FurnaceSweep => "OFENBOGEN", 
				_ => "GLUTVENTIL", 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private static Color AttackColor(CoreGuardianAttackType type)
		{
			return (type == CoreGuardianAttackType.EmberVent) ? new Color(1f, 0.16f, 0.02f, 0.9f) : new Color(1f, 0.42f, 0.06f, 0.9f);
		}

		private static float FlatDistance(Vector3 first, Vector3 second)
		{
			first.y = 0f;
			second.y = 0f;
			return Vector3.Distance(first, second);
		}
	}
}
