using Eidren.Core.Services;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Combat
{
	public sealed class Damageable : MonoBehaviour, IDamageable
	{
		private readonly struct ProtectionModifier
		{
			public float Amount { get; }

			public float Until { get; }

			public ProtectionModifier(float amount, float until)
			{
				Amount = amount;
				Until = until;
			}
		}

		private readonly HashSet<string> _invulnerabilitySources = new HashSet<string>();

		private readonly Dictionary<string, ProtectionModifier> _protectionModifiers = new Dictionary<string, ProtectionModifier>(StringComparer.Ordinal);

		private readonly Dictionary<string, ProtectionModifier> _damageTakenModifiers = new Dictionary<string, ProtectionModifier>(StringComparer.Ordinal);

		private bool _directInvulnerability;

		private float _baseProtection;

		private float _baseMaxHealth = 1f;

		private float _maxHealthBonus;

		public float CurrentHealth { get; private set; }

		public float MaxHealth { get; private set; }

		public bool IsAlive => CurrentHealth > 0f;

		public Transform TargetTransform => base.transform;

		public bool IsInvulnerable
		{
			get
			{
				return _directInvulnerability || _invulnerabilitySources.Count > 0;
			}
			set
			{
				_directInvulnerability = value;
			}
		}

		public float DamageTakenMultiplier { get; set; } = 1f;

		public float Protection
		{
			get
			{
				return CalculateProtection();
			}
			set
			{
				_baseProtection = Mathf.Clamp01(value);
			}
		}

		public event Action<float, float> HealthChanged;

		public event Action<DamageInfo> Damaged;

		public event Action Died;

		private void OnEnable()
		{
			CombatTargetRegistry.Register(this);
		}

		private void OnDisable()
		{
			CombatTargetRegistry.Unregister(this);
		}

		/// <summary>
		/// F32-002: Setzt den Grundwert und fuellt auf das ECHTE Maximum —
		/// der Ruestungsbonus bleibt erhalten. Vorher setzte Initialize ihn
		/// auf 0 zurueck; da der Spawn zuerst initialisiert und die Ruestung
		/// erst danach anbindet, stand die Figur nach jedem Zonenwechsel auf
		/// 100/110 und die oberen Punkte waren nie gefuellt.
		/// </summary>
		public void Initialize(float maxHealth)
		{
			_baseMaxHealth = Mathf.Max(1f, maxHealth);
			MaxHealth = _baseMaxHealth + _maxHealthBonus;
			CurrentHealth = MaxHealth;
			_directInvulnerability = false;
			_invulnerabilitySources.Clear();
			_protectionModifiers.Clear();
			Protection = 0f;
			this.HealthChanged?.Invoke(CurrentHealth, MaxHealth);
		}

		public void SetInvulnerabilitySource(string sourceId, bool active)
		{
			if (string.IsNullOrWhiteSpace(sourceId))
			{
				throw new ArgumentException("An invulnerability source requires a stable ID.", "sourceId");
			}
			if (active)
			{
				_invulnerabilitySources.Add(sourceId);
			}
			else
			{
				_invulnerabilitySources.Remove(sourceId);
			}
		}

		public void SetTimedProtectionModifier(string sourceId, float percentagePoints, float duration)
		{
			if (string.IsNullOrWhiteSpace(sourceId))
			{
				throw new ArgumentException("A protection modifier requires a stable source ID.", "sourceId");
			}
			if (duration <= 0f || Mathf.Approximately(percentagePoints, 0f))
			{
				_protectionModifiers.Remove(sourceId);
			}
			else
			{
				_protectionModifiers[sourceId] = new ProtectionModifier(percentagePoints, Time.time + duration);
			}
		}

		public void ApplyDamage(DamageInfo damage)
		{
			if (IsAlive && !IsInvulnerable && !(damage.HealthDamage <= 0f))
			{
				float baseDamage = damage.HealthDamage * Mathf.Clamp(DamageTakenMultiplier * CalculateTimedDamageTaken(), 0f, 10f);
				float num = ProtectionRules.ReceivedDamage(baseDamage, Mathf.Clamp01(Protection));
				CurrentHealth = Mathf.Max(0f, CurrentHealth - num);
				this.Damaged?.Invoke(new DamageInfo(num, damage.StaggerDamage, damage.HitPoint, damage.Source, damage.IsBackAttack, damage.AttackId, damage.SourceId));
				this.HealthChanged?.Invoke(CurrentHealth, MaxHealth);
				if (CurrentHealth <= 0f)
				{
					this.Died?.Invoke();
				}
			}
		}

		/// <summary>
		/// F31-018: Ruestungsbonus auf das Lebensmaximum. Hebt nur das
		/// Maximum (Anlegen heilt nicht); sinkt es unter den aktuellen Wert
		/// (Ablegen, Bruch), wird der aktuelle Wert geklemmt — nie 130/100.
		/// </summary>
		public void SetMaxHealthBonus(float bonus)
		{
			bonus = Mathf.Max(0f, bonus);
			if (Mathf.Approximately(bonus, _maxHealthBonus))
			{
				return;
			}
			_maxHealthBonus = bonus;
			MaxHealth = _baseMaxHealth + _maxHealthBonus;
			if (CurrentHealth > MaxHealth)
			{
				CurrentHealth = MaxHealth;
			}
			this.HealthChanged?.Invoke(CurrentHealth, MaxHealth);
		}

		public void HealToFull()
		{
			CurrentHealth = MaxHealth;
			this.HealthChanged?.Invoke(CurrentHealth, MaxHealth);
		}

		public bool Heal(float amount)
		{
			if (!IsAlive || amount <= 0f || CurrentHealth >= MaxHealth)
			{
				return false;
			}
			CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
			this.HealthChanged?.Invoke(CurrentHealth, MaxHealth);
			return true;
		}

		/// <summary>
		/// F32-004: Zeitlich begrenzter Aufschlag auf den erlittenen Schaden.
		/// Der Schmelzbrand nutzt ihn gegen Ziele OHNE Schutzwert — dort gibt
		/// es keinen Schutz zu senken, und ohne Ersatzwirkung waere die
		/// Faehigkeit gegen die Mehrheit der Gegner sinnlos.
		/// </summary>
		public void SetTimedDamageTakenModifier(string sourceId, float multiplier, float duration)
		{
			if (string.IsNullOrWhiteSpace(sourceId))
			{
				throw new ArgumentException("A damage modifier requires a stable source ID.", "sourceId");
			}
			if (duration <= 0f || multiplier <= 0f || Mathf.Approximately(multiplier, 1f))
			{
				_damageTakenModifiers.Remove(sourceId);
			}
			else
			{
				_damageTakenModifiers[sourceId] = new ProtectionModifier(multiplier, Time.time + duration);
			}
		}

		private float CalculateTimedDamageTaken()
		{
			if (_damageTakenModifiers.Count == 0)
			{
				return 1f;
			}
			float num = 1f;
			List<string> list = null;
			foreach (KeyValuePair<string, ProtectionModifier> modifier in _damageTakenModifiers)
			{
				if (Time.time >= modifier.Value.Until)
				{
					if (list == null)
					{
						list = new List<string>();
					}
					list.Add(modifier.Key);
				}
				else
				{
					num *= modifier.Value.Amount;
				}
			}
			if (list != null)
			{
				foreach (string item in list)
				{
					_damageTakenModifiers.Remove(item);
				}
			}
			return num;
		}

		private float CalculateProtection()
		{
			float num = _baseProtection;
			if (_protectionModifiers.Count == 0)
			{
				return Mathf.Clamp01(num);
			}
			List<string> list = null;
			foreach (KeyValuePair<string, ProtectionModifier> protectionModifier in _protectionModifiers)
			{
				if (Time.time >= protectionModifier.Value.Until)
				{
					if (list == null)
					{
						list = new List<string>();
					}
					list.Add(protectionModifier.Key);
				}
				else
				{
					num += protectionModifier.Value.Amount;
				}
			}
			if (list != null)
			{
				foreach (string item in list)
				{
					_protectionModifiers.Remove(item);
				}
			}
			return Mathf.Clamp01(num);
		}
	}
}
