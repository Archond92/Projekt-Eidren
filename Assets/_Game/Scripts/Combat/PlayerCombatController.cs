using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Eidra;
using Eidren.Input;
using Eidren.Player;
using Eidren.Presentation;
using System.Collections;
using System;
using UnityEngine;

namespace Eidren.Combat
{
	public sealed class PlayerCombatController : MonoBehaviour, ITransitionCancellable
	{
		private const string PlayerSourceId = "player";

		private readonly WeaponData[] _weapons = new WeaponData[2];

		private readonly bool[] _weaponAvailable = new bool[2];

		private readonly WeaponSwitchBuffer _weaponSwitchBuffer = new WeaponSwitchBuffer();

		private PlayerInputReader _input;

		private PlayerMotor _motor;

		private ICombatTargetQuery _targetQuery;
		public PersistentCombatTargetQuery Targeting => _targetQuery as PersistentCombatTargetQuery;
		private void Update()
		{
			if (_input != null && _input.GameplayEnabled) Targeting?.Tick(Time.time);
			else Targeting?.Clear();
		}

		private MeleeWeaponHitbox _weaponHitbox;

		private EidraTeamController _eidraTeam;

		private Transform _weaponVisual;

		private int _activeWeaponIndex = -1;

		private int _comboIndex;

		private bool _queuedAttack;

		private bool _canQueue;

		private bool _nudgedThisWindow;

		private bool _attackHitRegistered;

		private AttackStepData _currentStep;

		private Coroutine _attackRoutine;

		private float _consumableDamageMultiplier = 1f;

		private float _consumableStaggerMultiplier = 1f;

		private WeaponProgressionState _weaponProgression;

		private Damageable _damageable;

		public WeaponData ActiveWeapon => (_activeWeaponIndex >= 0 && _activeWeaponIndex < _weapons.Length) ? _weapons[_activeWeaponIndex] : null;

		public bool IsAttacking { get; private set; }

		public PlayerAttackPhase AttackPhase { get; private set; }

		public bool HasBufferedWeaponSwitch => _weaponSwitchBuffer.HasBufferedSwitch;

		/// <summary>
		/// Die verfuegbare Waffe, auf die der Wechselbutton umschalten wuerde —
		/// null, wenn nur eine (oder keine) Waffe getragen wird (W-004).
		/// </summary>
		public WeaponData AlternateWeapon
		{
			get
			{
				for (int i = 0; i < _weapons.Length; i++)
				{
					if (i != _activeWeaponIndex && _weapons[i] != null && _weaponAvailable[i])
					{
						return _weapons[i];
					}
				}
				return null;
			}
		}

		public event Action<WeaponData, WeaponData> WeaponChanged;

		public event Action<int> ComboChanged;

		public event Action<bool> HitLanded;

		public event Action<float, int, WeaponFamily> AttackStarted;

		public event Action WeaponSwitchStarted;

		public float GetModifiedBaseHealthDamage(WeaponData weapon)
		{
			if (weapon == null)
			{
				return 0f;
			}
			return weapon.BaseDamage * ((_weaponProgression != null) ? _weaponProgression.GetHealthDamageMultiplier(weapon.Id) : 1f);
		}

		public float GetModifiedBaseStaggerDamage(WeaponData weapon)
		{
			if (weapon == null)
			{
				return 0f;
			}
			return weapon.BaseStaggerDamage * ((_weaponProgression != null) ? _weaponProgression.GetStaggerDamageMultiplier(weapon.Id) : 1f);
		}

		public void Initialize(PlayerInputReader input, PlayerMotor motor, ICombatTargetQuery targetQuery, MeleeWeaponHitbox weaponHitbox, EidraTeamController eidraTeam, WeaponData hammer, WeaponData daggers, Transform weaponVisual, WeaponProgressionState weaponProgression = null)
		{
			if (_input != null)
			{
				_input.AttackPressed -= OnAttackPressed;
				_input.SwitchWeaponPressed -= OnSwitchWeaponPressed;
			}
			if (_motor != null)
			{
				_motor.DodgeStarted -= HandleDodgeStarted;
			}
			if (_damageable != null)
			{
				_damageable.Damaged -= HandleInterruptingDamage;
			}
			_input = input;
			_motor = motor;
			Targeting?.Clear();
			_targetQuery = targetQuery ?? throw new ArgumentNullException("targetQuery");
			_weaponHitbox = weaponHitbox ?? throw new ArgumentNullException("weaponHitbox");
			_eidraTeam = eidraTeam;
			_weapons[0] = hammer;
			_weapons[1] = daggers;
			_weaponAvailable[0] = hammer != null;
			_weaponAvailable[1] = daggers != null;
			_weaponVisual = weaponVisual;
			_weaponProgression = weaponProgression;
			_damageable = GetComponent<Damageable>();
			_input.AttackPressed += OnAttackPressed;
			_input.SwitchWeaponPressed += OnSwitchWeaponPressed;
			_motor.DodgeStarted += HandleDodgeStarted;
			if (_damageable != null)
			{
				_damageable.Damaged += HandleInterruptingDamage;
			}
			RefreshWeaponVisual();
			if (ActiveWeapon != null)
			{
				_weaponHitbox.SetPreview(ActiveWeapon.ResolvedMoveset.Combo[0]);
			}
		}

		public bool TrySelectWeapon(string weaponId)
		{
			if (IsAttacking)
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(weaponId))
			{
				WeaponData activeWeapon = ActiveWeapon;
				_activeWeaponIndex = -1;
				_comboIndex = 0;
				_weaponSwitchBuffer.Clear();
				_weaponHitbox.EndWindow();
				RefreshWeaponVisual();
				this.WeaponChanged?.Invoke(null, activeWeapon);
				return true;
			}
			if (IsAttacking)
			{
				return false;
			}
			int num = FindWeaponIndex(weaponId);
			if (num < 0)
			{
				return false;
			}
			if (!_weaponAvailable[num])
			{
				return false;
			}
			if (num == _activeWeaponIndex)
			{
				RefreshWeaponVisual();
				if (ActiveWeapon != null)
				{
					_weaponHitbox.SetPreview(ActiveWeapon.ResolvedMoveset.Combo[0]);
				}
				return true;
			}
			WeaponData activeWeapon2 = ActiveWeapon;
			_activeWeaponIndex = num;
			_comboIndex = 0;
			_weaponSwitchBuffer.Clear();
			RefreshWeaponVisual();
			_weaponHitbox.SetPreview(ActiveWeapon.ResolvedMoveset.Combo[0]);
			this.WeaponChanged?.Invoke(ActiveWeapon, activeWeapon2);
			return true;
		}

		public void SetWeaponAvailability(bool hammerAvailable, bool daggersAvailable, string preferredWeaponId)
		{
			_weaponAvailable[0] = hammerAvailable;
			_weaponAvailable[1] = daggersAvailable;
			if ((string.IsNullOrWhiteSpace(preferredWeaponId) || !TrySelectWeapon(preferredWeaponId)) && (_activeWeaponIndex < 0 || !_weaponAvailable[_activeWeaponIndex]))
			{
				if (_weaponAvailable[0])
				{
					TrySelectWeapon(_weapons[0].Id);
				}
				else if (_weaponAvailable[1])
				{
					TrySelectWeapon(_weapons[1].Id);
				}
				else
				{
					TrySelectWeapon(string.Empty);
				}
			}
		}

		private void OnAttackPressed()
		{
			if (_input == null || !_input.GameplayEnabled || ActiveWeapon == null)
			{
				return;
			}
			if (IsAttacking)
			{
				if (_canQueue)
				{
					_queuedAttack = true;
				}
			}
			else
			{
				_attackRoutine = StartCoroutine(AttackRoutine());
			}
		}

		private IEnumerator AttackRoutine()
		{
			IsAttacking = true;
			AttackPhase = PlayerAttackPhase.Windup;
			_queuedAttack = false;
			_canQueue = false;
			_nudgedThisWindow = false;
			_attackHitRegistered = false;
			WeaponData moveset = ActiveWeapon.ResolvedMoveset;
			AttackStepData step = moveset.Combo[_comboIndex];
			_currentStep = step;
			string attackId = $"weapon.{ActiveWeapon.Id}.combo.{_comboIndex + 1}";
			this.AttackStarted?.Invoke(step.Duration, _comboIndex, ActiveWeapon.Identity.Family);
			CombatTargetFacing.Apply(transform, _targetQuery, step, moveset);
			float elapsed = 0f;
			bool windowStarted = false;
			float windowEnd = step.HitTime + step.HitWindowDuration;
			Vector3 startingScale = _weaponVisual.localScale;
			while (elapsed < step.Duration)
			{
				float previousElapsed = elapsed;
				elapsed += Time.deltaTime;
				float normalized = elapsed / step.Duration;
				_canQueue = normalized >= step.ComboQueueStart;
				float pulse = 1f + Mathf.Sin(normalized * (float)Math.PI) * 0.35f;
				_weaponVisual.localScale = startingScale * pulse;
				if (!windowStarted && elapsed >= step.HitTime)
				{
					windowStarted = true;
					_weaponHitbox.BeginWindow(step);
					AttackPhase = PlayerAttackPhase.HitWindow;
				}
				if (windowStarted && _weaponHitbox.IsWindowActive)
				{
					bool insideWindow = elapsed >= step.HitTime && elapsed <= windowEnd;
					bool skippedWindow = previousElapsed < step.HitTime && elapsed > windowEnd;
					if (insideWindow || skippedWindow)
					{
						_weaponHitbox.Evaluate(base.transform, delegate(CombatTarget target)
						{
							ApplyHit(step, attackId, target);
						});
					}
					if (elapsed >= windowEnd)
					{
						_weaponHitbox.EndWindow();
						AttackPhase = PlayerAttackPhase.Recovery;
						if (TryExecuteBufferedSwitch(step, startingScale))
						{
							yield break;
						}
					}
				}
				yield return null;
			}
			_weaponHitbox.EndWindow();
			AttackPhase = PlayerAttackPhase.Recovery;
			_weaponVisual.localScale = startingScale;
			_canQueue = false;
			if (_weaponSwitchBuffer.Consume())
			{
				FinishAttackAndSwitch(startingScale);
				yield break;
			}
			if (_queuedAttack)
			{
				_comboIndex = (_comboIndex + 1) % ActiveWeapon.ResolvedMoveset.Combo.Length;
				this.ComboChanged?.Invoke(_comboIndex);
				_attackRoutine = StartCoroutine(AttackRoutine());
				yield break;
			}
			IsAttacking = false;
			AttackPhase = PlayerAttackPhase.None;
			_currentStep = default(AttackStepData);
			_attackRoutine = null;
			_comboIndex = 0;
			this.ComboChanged?.Invoke(_comboIndex);
		}

		private bool TryExecuteBufferedSwitch(AttackStepData step, Vector3 restingScale)
		{
			if (!_weaponSwitchBuffer.HasBufferedSwitch || !WeaponSwitchRules.CanSwitchImmediately(PlayerAttackPhase.Recovery, step))
			{
				return false;
			}
			_weaponSwitchBuffer.Consume();
			FinishAttackAndSwitch(restingScale);
			return true;
		}

		private void ApplyHit(AttackStepData step, string attackId, CombatTarget target)
		{
			bool flag = target.IsBackAttack(base.transform);
			WeaponData resolvedMoveset = ActiveWeapon.ResolvedMoveset;
			Vector3 vector = CombatTargetGeometry.ClosestBodyPoint(target.Transform, base.transform.position) - base.transform.position;
			vector.y = 0f;
			WeaponHitModifiers weaponHitModifiers = WeaponCombatRules.Resolve(ActiveWeapon.Identity.Signature, _comboIndex, flag, vector.magnitude, step.HitboxRange);
			float num = GetModifiedBaseHealthDamage(ActiveWeapon) * step.DamageMultiplier * weaponHitModifiers.HealthMultiplier;
			float num2 = GetModifiedBaseStaggerDamage(ActiveWeapon) * step.StaggerMultiplier * weaponHitModifiers.StaggerMultiplier;
			num *= _consumableDamageMultiplier;
			num2 *= _consumableStaggerMultiplier;
			if (_eidraTeam != null && _eidraTeam.ActiveData != null)
			{
				num2 *= _eidraTeam.ActiveData.PassiveStaggerMultiplier;
				if (flag)
				{
					num *= _eidraTeam.ActiveData.PassiveBackDamageMultiplier;
				}
			}
			if (CombatHitResolver.TryApply(damage: new DamageInfo(num, num2, target.Transform.position + Vector3.up, base.gameObject, flag, attackId, "player"), target: target, hitTargetIds: null))
			{
				ApplyPassiveBurn(target, num);
				Targeting?.ConfirmEngagement(target);
				SpawnSignatureFeedback(weaponHitModifiers.Cue, target.Transform.position);
				if (!_attackHitRegistered)
				{
					_attackHitRegistered = true;
					this.HitLanded?.Invoke(flag);
				}
				if (!_nudgedThisWindow)
				{
					_nudgedThisWindow = true;
					_motor.Nudge(base.transform.forward, step.ForwardMotion);
				}
			}
		}

		private void OnSwitchWeaponPressed()
		{
			if (_input == null || !_input.GameplayEnabled || ActiveWeapon == null || _weaponSwitchBuffer.Request(IsAttacking ? AttackPhase : PlayerAttackPhase.None, _currentStep) != WeaponSwitchRequestResult.ExecuteImmediately)
			{
				return;
			}
			if (!IsAttacking)
			{
				SwitchWeapon();
				return;
			}
			if (_attackRoutine != null)
			{
				StopCoroutine(_attackRoutine);
			}
			FinishAttackAndSwitch(GetRestingWeaponScale());
		}

		private void FinishAttackAndSwitch(Vector3 restingScale)
		{
			_weaponHitbox.EndWindow();
			if (_weaponVisual != null)
			{
				_weaponVisual.localScale = restingScale;
			}
			_queuedAttack = false;
			_canQueue = false;
			_weaponSwitchBuffer.Clear();
			IsAttacking = false;
			AttackPhase = PlayerAttackPhase.None;
			_currentStep = default(AttackStepData);
			_attackRoutine = null;
			_comboIndex = 0;
			this.ComboChanged?.Invoke(_comboIndex);
			SwitchWeapon();
		}

		private void SwitchWeapon()
		{
			int num = 1 - _activeWeaponIndex;
			if (num >= 0 && num < _weaponAvailable.Length && _weaponAvailable[num])
			{
				_weaponHitbox.EndWindow();
				_weaponSwitchBuffer.Clear();
				WeaponData activeWeapon = ActiveWeapon;
				_activeWeaponIndex = num;
				_comboIndex = 0;
				RefreshWeaponVisual();
				_weaponHitbox.SetPreview(ActiveWeapon.ResolvedMoveset.Combo[0]);
				this.WeaponSwitchStarted?.Invoke();
				this.WeaponChanged?.Invoke(ActiveWeapon, activeWeapon);
			}
		}

		private Vector3 GetRestingWeaponScale()
		{
			if (ActiveWeapon == null)
			{
				return Vector3.zero;
			}
			return (ActiveWeapon.Identity.Family == WeaponFamily.Hammer) ? Vector3.one : (Vector3.one * 0.82f);
		}

		private void RefreshWeaponVisual()
		{
			IPlayerWeaponPresentation playerWeaponVisual = ((_weaponVisual != null) ? _weaponVisual.GetComponent<IPlayerWeaponPresentation>() : null);
			if (ActiveWeapon == null)
			{
				if (playerWeaponVisual != null)
				{
					playerWeaponVisual.Show(null);
				}
				else if (_weaponVisual != null)
				{
					_weaponVisual.gameObject.SetActive(value: false);
				}
				return;
			}
			bool flag = ActiveWeapon.Identity.Family == WeaponFamily.Hammer;
			if (!(_weaponVisual != null))
			{
				return;
			}
			_weaponVisual.gameObject.SetActive(value: true);
			_weaponVisual.localScale = (flag ? Vector3.one : (Vector3.one * 0.82f));
			if (playerWeaponVisual != null)
			{
				playerWeaponVisual.Show(ActiveWeapon);
				return;
			}
			Renderer component = _weaponVisual.GetComponent<Renderer>();
			if (component != null)
			{
				component.material.color = ActiveWeapon.Accent;
			}
		}

		public void SetConsumableBuff(float damageMultiplier, float staggerMultiplier)
		{
			_consumableDamageMultiplier = Mathf.Max(0.1f, damageMultiplier);
			_consumableStaggerMultiplier = Mathf.Max(0.1f, staggerMultiplier);
		}

		public void CancelForSceneTransition()
		{
			Targeting?.Clear();
			if (_attackRoutine != null)
			{
				StopCoroutine(_attackRoutine);
			}
			_attackRoutine = null;
			_weaponHitbox?.EndWindow();
			_weaponSwitchBuffer.Clear();
			_queuedAttack = false;
			_canQueue = false;
			IsAttacking = false;
			AttackPhase = PlayerAttackPhase.None;
			_currentStep = default(AttackStepData);
			_attackHitRegistered = false;
			if (_weaponVisual != null)
			{
				_weaponVisual.localScale = GetRestingWeaponScale();
			}
		}

		private void OnDisable()
		{
			CancelForSceneTransition();
		}

		private void OnDestroy()
		{
			if (_weaponHitbox != null)
			{
				_weaponHitbox.EndWindow();
			}
			if (!(_input == null))
			{
				_input.AttackPressed -= OnAttackPressed;
				_input.SwitchWeaponPressed -= OnSwitchWeaponPressed;
				if (_motor != null)
				{
					_motor.DodgeStarted -= HandleDodgeStarted;
				}
				if (_damageable != null)
				{
					_damageable.Damaged -= HandleInterruptingDamage;
				}
			}
		}

		public void SetEquippedWeapons(WeaponData first, WeaponData second, string preferredWeaponId)
		{
			string text = ((ActiveWeapon != null) ? ActiveWeapon.Id : string.Empty);
			_weapons[0] = first;
			_weapons[1] = second;
			_weaponAvailable[0] = first != null;
			_weaponAvailable[1] = second != null;
			string text2 = ((!string.IsNullOrWhiteSpace(preferredWeaponId)) ? preferredWeaponId : text);
			if (FindWeaponIndex(text2) < 0 && IsAttacking)
			{
				InterruptCurrentAttack();
			}
			if ((string.IsNullOrWhiteSpace(text2) || !TrySelectWeapon(text2)) && (!(first != null) || !TrySelectWeapon(first.Id)) && (!(second != null) || !TrySelectWeapon(second.Id)))
			{
				TrySelectWeapon(string.Empty);
			}
		}

		private int FindWeaponIndex(string weaponId)
		{
			if (string.IsNullOrWhiteSpace(weaponId))
			{
				return -1;
			}
			for (int i = 0; i < _weapons.Length; i++)
			{
				if (_weapons[i] != null && string.Equals(_weapons[i].Id, weaponId, StringComparison.Ordinal))
				{
					return i;
				}
			}
			return -1;
		}

		private void HandleDodgeStarted()
		{
			InterruptCurrentAttack();
		}

		private void HandleInterruptingDamage(DamageInfo damage)
		{
			InterruptCurrentAttack();
		}

		private void InterruptCurrentAttack()
		{
			if (!IsAttacking)
			{
				_comboIndex = 0;
				return;
			}
			if (_attackRoutine != null)
			{
				StopCoroutine(_attackRoutine);
			}
			_attackRoutine = null;
			_weaponHitbox?.EndWindow();
			_weaponSwitchBuffer.Clear();
			_queuedAttack = false;
			_canQueue = false;
			_attackHitRegistered = false;
			IsAttacking = false;
			AttackPhase = PlayerAttackPhase.None;
			_currentStep = default(AttackStepData);
			_comboIndex = 0;
			this.ComboChanged?.Invoke(_comboIndex);
			if (_weaponVisual != null)
			{
				_weaponVisual.localScale = GetRestingWeaponScale();
			}
		}

		/// <summary>
		/// F32-005: Ignivars Passiv entzuendet das Ziel — ein Anteil des
		/// Trefferschadens brennt nach. Haengt an keiner Eigenschaft des
		/// Ziels und wirkt damit gegen jeden Gegner (oberste Praemisse).
		/// </summary>
		private void ApplyPassiveBurn(CombatTarget target, float healthDamage)
		{
			EidraData active = (_eidraTeam != null) ? _eidraTeam.ActiveData : null;
			if (active == null || active.PassiveBurnFraction <= 0f || active.PassiveBurnSeconds <= 0)
			{
				return;
			}
			if (target.Damageable is Eidren.AI.EnemyControllerBase enemy)
			{
				enemy.ApplyBurn(healthDamage * active.PassiveBurnFraction, active.PassiveBurnSeconds, base.gameObject);
			}
		}

		private static void SpawnSignatureFeedback(WeaponSignatureCue cue, Vector3 targetPosition)
		{
			Vector3 position = targetPosition + Vector3.up;
			switch (cue)
			{
			case WeaponSignatureCue.HammerImpact:
				CombatFeedback.SpawnStyleCue(position, StyleCue.HammerHit, "WUCHT");
				break;
			case WeaponSignatureCue.DaggerAmbush:
				CombatFeedback.SpawnStyleCue(position, StyleCue.DaggerHit, "HINTERHALT");
				break;
			case WeaponSignatureCue.SpearReachWindow:
				CombatFeedback.SpawnStyleCue(position, StyleCue.SpearHit, "DISTANZ");
				break;
			}
		}
	}
}
