using Eidren.AI;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Input;
using Eidren.Player;
using Eidren.Presentation;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Eidra
{
	public sealed class EidraTeamController : MonoBehaviour, ITransitionCancellable
	{
		private const string ShieldInvulnerabilitySource = "eidra.steinhaut";

		private const float FelsbrecherRecoveryDuration = 0.32f;

		private const float ShadowStepRecoveryDuration = 0.25f;

		private const float BackMarkRecoveryDuration = 0.3f;

		private readonly EidraData[] _team = new EidraData[2];

		private readonly Dictionary<AbilityData, float> _abilityReadyAt = new Dictionary<AbilityData, float>();

		private PlayerInputReader _input;

		private PlayerMotor _motor;

		private Damageable _playerHealth;

		private PlayerCombatController _combat;

		private BossController _boss;

		private EnemyControllerBase _combatTarget;

		private bool _initialized;

		private Transform _visual;

		private IActorPresentation _actorPresentation;

		private IActorAnimationDriver _actorAnimation;

		private string _visualVariantId;

		private GameObject _rangePreview;

		private LineRenderer _rangeLine;

		private int _activeIndex;

		private float _nextSwitchTime;

		private float _shieldUntil;

		private float _companionLunge;

		private bool _abilityRunning;

		private bool _isSwitching;

		private bool _shieldActive;

		private Coroutine _abilityRoutine;

		private Coroutine _markRoutine;

		private GameObject _abilityTelegraph;

		private GameObject _shieldVisual;

		private EnemyControllerBase _markedTarget;

		private Vector3 _abilityStartScale = Vector3.one;

		public EidraData ActiveData => (_activeIndex >= 0 && _activeIndex < _team.Length) ? _team[_activeIndex] : null;

		public float SwitchCooldownRemaining => Mathf.Max(0f, _nextSwitchTime - Time.time);

		public float ShieldRemaining => Mathf.Max(0f, _shieldUntil - Time.time);

		public bool IsAbilityRunning => _abilityRunning;

		public bool HasActiveAbilityTelegraph => _abilityTelegraph != null;

		public string PassiveDescription => EidraPassiveText.For(ActiveData);

		// F31-016b: Feldgegner sind immer "kampfaktiv" — nur Bosse haben ein
		// eigenes Aktivierungsfenster. Vorher bestand diese Pruefung nur fuer
		// die zwei Bosstypen, womit alle Faehigkeiten im regulaeren Spiel tot
		// waren.
		private bool TargetBattleActive => (_combatTarget is BossController bossController) ? bossController.BattleActive : (_combatTarget != null);

		public bool IsInitialized => _initialized;
		public PersistentCombatTargetQuery Targeting { get; private set; }

		public event Action<EidraData> ActiveEidraChanged;

		public event Action<int, float> AbilityCooldownStarted;

		public event Action<int, string> AbilityFailed;

		public void ResetCooldowns()
		{
			_abilityReadyAt.Clear();
			_nextSwitchTime = 0f;
		}

		public void AttachBoss(BossController boss)
		{
			if (_initialized && !(_boss == boss))
			{
				if (_boss != null)
				{
					_boss.Died -= OnTargetDied;
				}
				_boss = boss;
				if (Targeting == null) _combatTarget = boss;
				if (_boss != null)
				{
					_boss.Died += OnTargetDied;
				}
			}
		}

		public void AttachForgeBoss(CoreGuardianController boss)
		{
			if (_initialized && !(_combatTarget == boss))
			{
				if (_boss != null) _boss.Died -= OnTargetDied;
				if (_combatTarget != null)
				{
					_combatTarget.Died -= OnTargetDied;
				}
				_boss = null;
				if (Targeting == null) _combatTarget = boss;
				if (Targeting == null && _combatTarget != null)
				{
					_combatTarget.Died += OnTargetDied;
				}
			}
		}

		public void Initialize(PlayerInputReader input, PlayerMotor motor, Damageable playerHealth, BossController boss, EidraData terrock, EidraData noctarion, Material visualMaterial, Material rangeMaterial, PersistentCombatTargetQuery targeting = null)
		{
			if (targeting != null && _combatTarget != null) _combatTarget.Died -= OnTargetDied;
			Targeting = targeting;
			if (_initialized)
			{
				SetTeam(terrock, noctarion);
				AttachBoss(boss);
				return;
			}
			_input = input;
			_motor = motor;
			_playerHealth = playerHealth;
			_combat = GetComponent<PlayerCombatController>();
			_boss = boss;
			_combatTarget = boss;
			BuildRangePreview(rangeMaterial);
			SetTeam(terrock, noctarion);
			_input.SwitchEidraPressed += TrySwitch;
			_initialized = true;
			_input.SkillPressed += TryUseSkill;
			_input.SkillPreviewChanged += SetRangePreview;
			_playerHealth.Died += OnPlayerDied;
			if (_boss != null)
			{
				_boss.Died += OnTargetDied;
			}
			if (_combat != null)
			{
				_combat.WeaponChanged += OnWeaponChanged;
			}
			StartCoroutine(CompanionRoutine());
		}

		public void SetTeam(EidraData first, EidraData second)
		{
			EidraData eidraData = first ?? second;
			EidraData eidraData2 = ((first != null) ? second : null);
			EidraData activeData = ActiveData;
			bool flag = _team[0] != eidraData || _team[1] != eidraData2;
			_team[0] = eidraData;
			_team[1] = eidraData2;
			if (_team[0] == null)
			{
				_activeIndex = -1;
			}
			else if (activeData != null && activeData == _team[1])
			{
				_activeIndex = 1;
			}
			else
			{
				_activeIndex = 0;
			}
			if (flag && _initialized)
			{
				CancelAbilitiesAndEffects();
			}
			EnsureVisual();
			RefreshVisual();
			if (flag)
			{
				this.ActiveEidraChanged?.Invoke(ActiveData);
			}
		}

		public bool TrySelectEidra(string eidraId)
		{
			if (string.IsNullOrWhiteSpace(eidraId) || _team[0] == null || _team[1] == null || _abilityRunning || _isSwitching)
			{
				return false;
			}
			int num = (string.Equals(_team[1].Id, eidraId, StringComparison.Ordinal) ? 1 : ((!string.Equals(_team[0].Id, eidraId, StringComparison.Ordinal)) ? (-1) : 0));
			if (num < 0)
			{
				return false;
			}
			_activeIndex = num;
			RefreshVisual();
			this.ActiveEidraChanged?.Invoke(ActiveData);
			return true;
		}

		private void LateUpdate()
		{
			if (_visual == null)
			{
				return;
			}
			Vector3 vector = base.transform.right * ((_activeIndex == 0) ? (-1.35f) : 1.35f);
			Vector3 b = base.transform.position + vector - base.transform.forward * 0.75f;
			b.y = base.transform.position.y;
			if (_combatTarget != null && _combatTarget.IsAlive && _companionLunge > 0f)
			{
				Vector3 vector2 = _combatTarget.transform.position - base.transform.position;
				vector2.y = 0f;
				if (vector2.sqrMagnitude > 0.01f)
				{
					b += vector2.normalized * _companionLunge;
				}
			}
			_visual.position = Vector3.Lerp(_visual.position, b, 8f * Time.deltaTime);
		}

		public void SetRangePreview(int skillIndex, bool visible)
		{
			if (_rangePreview == null || ActiveData == null)
			{
				return;
			}
			if (!visible)
			{
				_rangePreview.SetActive(value: false);
				return;
			}
			AbilityData abilityData = ((skillIndex == 0) ? ActiveData.Skill1 : ActiveData.Skill2);
			float num = ((abilityData.Range > 0f) ? abilityData.Range : 2.4f);
			if (_rangeLine != null)
			{
				Color uiAccent = ActiveData.UiAccent;
				uiAccent.a = 0.9f;
				_rangeLine.startColor = uiAccent;
				_rangeLine.endColor = uiAccent;
				_rangeLine.material.color = uiAccent;
				_rangeLine.positionCount = 96;
				for (int i = 0; i < 96; i++)
				{
					float f = (float)i / 96f * (float)Math.PI * 2f;
					_rangeLine.SetPosition(i, new Vector3(Mathf.Cos(f) * num, 0f, Mathf.Sin(f) * num));
				}
			}
			_rangePreview.SetActive(value: true);
		}

		public float GetCooldownRemaining(int skillIndex)
		{
			if (ActiveData == null)
			{
				return 0f;
			}
			AbilityData key = ((skillIndex == 0) ? ActiveData.Skill1 : ActiveData.Skill2);
			float value;
			return _abilityReadyAt.TryGetValue(key, out value) ? Mathf.Max(0f, value - Time.time) : 0f;
		}

		private void TrySwitch()
		{
			if (!(_input == null) && _input.GameplayEnabled && !(_playerHealth == null) && _playerHealth.IsAlive && !_isSwitching && !(Time.time < _nextSwitchTime) && !(_team[0] == null) && !(_team[1] == null))
			{
				CancelAbilitiesAndEffects();
				StartCoroutine(SwitchRoutine());
			}
		}

		private IEnumerator SwitchRoutine()
		{
			_nextSwitchTime = Time.time + 4f;
			_isSwitching = true;
			Vector3 normalScale = Vector3.one;
			float elapsed = 0f;
			while (elapsed < 0.2f)
			{
				elapsed += Time.deltaTime;
				_visual.localScale = Vector3.Lerp(normalScale, Vector3.zero, elapsed / 0.2f);
				yield return null;
			}
			_activeIndex = 1 - _activeIndex;
			RefreshVisual();
			_actorPresentation?.SetVisualState(ActorVisualState.Appear);
			this.ActiveEidraChanged?.Invoke(ActiveData);
			elapsed = 0f;
			while (elapsed < 0.2f)
			{
				elapsed += Time.deltaTime;
				_visual.localScale = Vector3.Lerp(Vector3.zero, normalScale, elapsed / 0.2f);
				yield return null;
			}
			_visual.localScale = normalScale;
			_isSwitching = false;
		}

		private void TryUseSkill(int skillIndex)
		{
			if (_input == null || !_input.GameplayEnabled || _playerHealth == null || !_playerHealth.IsAlive || _abilityRunning || _isSwitching)
			{
				return;
			}
			if (ActiveData == null)
			{
				NotifyAbilityFailed(skillIndex, "KEIN EIDRA AKTIV");
				return;
			}
			AbilityData abilityData = ((skillIndex == 0) ? ActiveData.Skill1 : ActiveData.Skill2);
			if (abilityData != null)
			{
				TryAcquireFieldTarget(abilityData.Range);
			}
			if (!_abilityReadyAt.TryGetValue(abilityData, out var value) || !(Time.time < value))
			{
				if (!TryValidateAbility(abilityData, out var _, out var failure))
				{
					NotifyAbilityFailed(skillIndex, failure);
				}
				else
				{
					_abilityRoutine = StartCoroutine(ExecuteAbility(abilityData, skillIndex));
				}
			}
		}

		private IEnumerator ExecuteAbility(AbilityData ability, int skillIndex)
		{
			_abilityRunning = true;
			_actorAnimation?.PlayAbility(skillIndex);
			_abilityStartScale = ((_visual != null) ? _visual.localScale : Vector3.one);
			Vector3 destination;
			switch (ability.ExecutionType)
			{
			case AbilityExecutionType.StaggerStrike:
			{
				yield return RunCastTelegraph(ability, _combatTarget.transform, new Color(1f, 0.68f, 0.18f), "FELSBRUCH LÄDT");
				if (!TryValidateAbility(ability, out destination, out var staggerFailure))
				{
					FailActiveAbility(skillIndex, staggerFailure);
					yield break;
				}
				StartAbilityCooldown(ability, skillIndex);
				CombatFeedback.SpawnImpact(_combatTarget.transform.position + Vector3.up * 2.1f, new Color(1f, 0.68f, 0.18f));
				_combatTarget.AddStagger(ability.StaggerAmount);
				yield return new WaitForSeconds(0.32f);
				break;
			}
			case AbilityExecutionType.Shield:
			{
				yield return RunCastTelegraph(ability, base.transform, new Color(1f, 0.7f, 0.2f, 0.95f), "STEINHAUT FORMT SICH");
				if (!TryValidateAbility(ability, out destination, out var shieldFailure))
				{
					FailActiveAbility(skillIndex, shieldFailure);
					yield break;
				}
				StartAbilityCooldown(ability, skillIndex);
				_shieldActive = true;
				_shieldUntil = Time.time + ability.EffectDuration;
				_playerHealth.SetInvulnerabilitySource("eidra.steinhaut", active: true);
				_shieldVisual = CombatFeedback.SpawnAura(base.transform, new Color(1f, 0.7f, 0.2f, 0.95f), ability.EffectDuration, "STEINHAUT", 1.35f, 3.25f);
				CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 3.2f, "STEINHAUT\nUNVERWUNDBAR", new Color(1f, 0.75f, 0.28f));
				yield return new WaitForSeconds(ability.EffectDuration);
				ClearShield();
				break;
			}
			case AbilityExecutionType.ShadowStep:
			{
				if (!TryValidateAbility(ability, out var destination2, out var shadowFailure))
				{
					FailActiveAbility(skillIndex, shadowFailure);
					yield break;
				}
				Vector3 origin = base.transform.position;
				if (!_motor.TryTeleport(destination2))
				{
					FailActiveAbility(skillIndex, "KEINE FREIE POSITION");
					yield break;
				}
				StartAbilityCooldown(ability, skillIndex);
				CombatFeedback.SpawnImpact(origin + Vector3.up * 1.3f, new Color(0.65f, 0.3f, 1f));
				Vector3 lookDirection = _combatTarget.transform.position - base.transform.position;
				lookDirection.y = 0f;
				if (lookDirection.sqrMagnitude > 0.001f)
				{
					base.transform.rotation = Quaternion.LookRotation(lookDirection.normalized);
				}
				CombatFeedback.SpawnImpact(destination2 + Vector3.up * 1.3f, new Color(0.65f, 0.3f, 1f));
				CombatFeedback.SpawnStatusText(destination2 + Vector3.up * 3.1f, "SCHATTENSCHRITT", new Color(0.72f, 0.4f, 1f));
				yield return new WaitForSeconds(0.25f);
				break;
			}
			case AbilityExecutionType.BackMark:
			{
				if (!TryValidateAbility(ability, out destination, out var markFailure))
				{
					FailActiveAbility(skillIndex, markFailure);
					yield break;
				}
				StartAbilityCooldown(ability, skillIndex);
				BeginBackMark(ability);
				yield return new WaitForSeconds(0.3f);
				break;
			}
			case AbilityExecutionType.EmberCircle:
			{
				yield return RunCastTelegraph(ability, _combatTarget.transform, new Color(1f, 0.24f, 0.04f), "GLUTKREIS LÄDT");
				if (!TryValidateAbility(ability, out destination, out var emberFailure))
				{
					FailActiveAbility(skillIndex, emberFailure);
					yield break;
				}
				Vector3 center = _combatTarget.transform.position;
				StartAbilityCooldown(ability, skillIndex);
				EmberCircleField.Spawn(center, ability.Radius, ability.EffectDuration, ability.HealthDamagePerSecond, (_visual != null) ? _visual.gameObject : base.gameObject);
				yield return new WaitForSeconds(0.3f);
				break;
			}
			case AbilityExecutionType.MoltenBrand:
			{
				yield return RunCastTelegraph(ability, _combatTarget.transform, new Color(1f, 0.42f, 0.08f), "SCHMELZBRAND LÄDT");
				if (!TryValidateAbility(ability, out destination, out var brandFailure))
				{
					FailActiveAbility(skillIndex, brandFailure);
					yield break;
				}
				StartAbilityCooldown(ability, skillIndex);
				Damageable branded = _combatTarget.GetComponent<Damageable>();
				float brandAmount = Mathf.Abs(ability.ProtectionReduction);
				// F32-004: Gegen ein Ziel MIT Schutz senkt der Brand den Schutz.
				// Ohne Schutz gaebe es nichts zu senken — dort traegt er
				// stattdessen denselben Betrag als Schadensaufschlag. So wirkt
				// die Faehigkeit gegen jeden Gegner, statt gegen 3 von 11.
				string brandLabel;
				if (branded.Protection > 0f)
				{
					branded.SetTimedProtectionModifier("eidra.ignivar.molten_brand", 0f - brandAmount, ability.EffectDuration);
					brandLabel = $"SCHMELZBRAND · SCHUTZ -{brandAmount * 100f:0}%P";
				}
				else
				{
					branded.SetTimedDamageTakenModifier("eidra.ignivar.molten_brand", 1f + brandAmount, ability.EffectDuration);
					brandLabel = $"SCHMELZBRAND · SCHADEN +{brandAmount * 100f:0}%";
				}
				CombatFeedback.SpawnAura(_combatTarget.transform, new Color(1f, 0.38f, 0.05f, 0.95f), ability.EffectDuration, brandLabel, 2.1f, 4.5f);
				yield return new WaitForSeconds(0.3f);
				break;
			}
			}
			CompleteActiveAbility();
		}

		private IEnumerator RunCastTelegraph(AbilityData ability, Transform target, Color color, string label)
		{
			if (ability.CastDuration <= 0f)
			{
				yield break;
			}
			float radius = ((ability.ExecutionType == AbilityExecutionType.StaggerStrike) ? 2.35f : 1.35f);
			float labelHeight = ((ability.ExecutionType == AbilityExecutionType.StaggerStrike) ? 4.7f : 3.25f);
			_abilityTelegraph = CombatFeedback.SpawnAura(target, color, ability.CastDuration, label, radius, labelHeight);
			CombatFeedback.SpawnStatusText(target.position + Vector3.up * labelHeight, label, color, ability.CastDuration);
			float elapsed = 0f;
			while (elapsed < ability.CastDuration)
			{
				elapsed += Time.deltaTime;
				float normalized = Mathf.Clamp01(elapsed / ability.CastDuration);
				float pulse = Mathf.Sin(normalized * (float)Math.PI);
				if (_visual != null)
				{
					_visual.localScale = _abilityStartScale * (1f + pulse * 0.6f);
				}
				yield return null;
			}
			DestroyAbilityTelegraph();
			if (_visual != null)
			{
				_visual.localScale = _abilityStartScale;
			}
		}

		/// <summary>
		/// HUD-100: pro Aktion gemeinsames Ziel oder reichweitengueltiger Fallback.
		/// Laufende Casts und Begleitschlaege behalten ihren Aktionskontext.
		/// </summary>
		private void TryAcquireFieldTarget(float range)
		{
			_combatTarget = EidraCombatTargetResolver.Resolve(Targeting, transform, _combatTarget, _boss, range);
		}

		private bool TryValidateAbility(AbilityData ability, out Vector3 destination, out string failure)
		{
			destination = default(Vector3);
			failure = string.Empty;
			if (ability == null || _playerHealth == null || !_playerHealth.IsAlive)
			{
				failure = "NICHT VERFÜGBAR";
				return false;
			}
			// F31-016a: Der Schild wirkt auf die eigene Figur (Reichweite 0 in
			// den Daten) und braucht kein Kampfziel — seine Pruefung muss VOR
			// der Zielpruefung stehen, sonst scheitert er in jeder Zone ohne
			// Boss an "KEIN GUELTIGES ZIEL".
			if (ability.ExecutionType == AbilityExecutionType.Shield)
			{
				if (_shieldActive)
				{
					failure = "BEREITS AKTIV";
					return false;
				}
				return true;
			}
			if (_combatTarget == null || !_combatTarget.IsAlive || !TargetBattleActive || !_combatTarget.isActiveAndEnabled || _combatTarget.EnemyState == EnemyState.Return)
			{
				failure = "KEIN GÜLTIGES ZIEL";
				return false;
			}
			if (!EidraAbilityRules.IsTargetInRange(base.transform.position, _combatTarget.transform, _combatTarget.IsAlive, TargetBattleActive, ability.Range))
			{
				failure = "AUSSER REICHWEITE";
				return false;
			}
			// F32-004: Hier stand bis zur Fixrunde v0.3.2 je eine Sperre fuer
			// BackMark (verlangte einen Boss) und MoltenBrand (verlangte einen
			// Schutzwert am Ziel). Beide banden eine Faehigkeit an den
			// Gegnertyp und sind ersatzlos entfallen — der Schmelzbrand traegt
			// seine Wirkung gegen ungeschuetzte Ziele jetzt selbst.
			if (ability.ExecutionType != AbilityExecutionType.ShadowStep)
			{
				return true;
			}
			if (EidraAbilityRules.TryFindShadowStepDestination(base.transform.position, _combatTarget.transform, _combatTarget.IsAlive, TargetBattleActive, ability.Range, ability.TeleportBehindDistance, _motor.CanTeleportTo, out destination))
			{
				return true;
			}
			failure = "KEINE FREIE POSITION";
			return false;
		}

		private void StartAbilityCooldown(AbilityData ability, int skillIndex)
		{
			_abilityReadyAt[ability] = Time.time + ability.Cooldown;
			this.AbilityCooldownStarted?.Invoke(skillIndex, ability.Cooldown);
		}

		private void BeginBackMark(AbilityData ability)
		{
			ClearBackMark();
			// F32-003: Markiert wird das Kampfziel — Boss wie Feldgegner.
			_markedTarget = _combatTarget;
			_markedTarget.MarkBack(ability.EffectDuration, ability.BackDamageMultiplier);
			_markRoutine = StartCoroutine(BackMarkLifetime(_markedTarget, ability.EffectDuration));
		}

		private IEnumerator BackMarkLifetime(EnemyControllerBase target, float duration)
		{
			float elapsed = 0f;
			while (target != null && target.IsAlive && elapsed < duration)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}
			if (_markedTarget == target)
			{
				target?.ClearBackMark();
				_markedTarget = null;
				_markRoutine = null;
			}
		}

		private void NotifyAbilityFailed(int skillIndex, string failure)
		{
			this.AbilityFailed?.Invoke(skillIndex, failure);
		}

		private void FailActiveAbility(int skillIndex, string failure)
		{
			NotifyAbilityFailed(skillIndex, failure);
			CompleteActiveAbility();
		}

		private void CompleteActiveAbility()
		{
			DestroyAbilityTelegraph();
			if (_visual != null)
			{
				_visual.localScale = _abilityStartScale;
			}
			_abilityRoutine = null;
			_abilityRunning = false;
		}

		private void CancelAbilitiesAndEffects()
		{
			if (_abilityRoutine != null)
			{
				StopCoroutine(_abilityRoutine);
			}
			_abilityRoutine = null;
			_abilityRunning = false;
			DestroyAbilityTelegraph();
			ClearShield();
			ClearBackMark();
			SetRangePreview(0, visible: false);
			if (_visual != null)
			{
				_visual.localScale = Vector3.one;
			}
		}

		public void CancelForSceneTransition()
		{
			StopAllCoroutines();
			CancelAbilitiesAndEffects();
			_isSwitching = false;
			_companionLunge = 0f;
		}

		private void ClearShield()
		{
			_shieldActive = false;
			_shieldUntil = 0f;
			_playerHealth?.SetInvulnerabilitySource("eidra.steinhaut", active: false);
			if (!(_shieldVisual == null))
			{
				CombatFeedback.Release(_shieldVisual);
				_shieldVisual = null;
			}
		}

		private void ClearBackMark()
		{
			if (_markRoutine != null)
			{
				StopCoroutine(_markRoutine);
			}
			_markRoutine = null;
			_markedTarget?.ClearBackMark();
			_markedTarget = null;
		}

		private void DestroyAbilityTelegraph()
		{
			if (!(_abilityTelegraph == null))
			{
				CombatFeedback.Release(_abilityTelegraph);
				_abilityTelegraph = null;
			}
		}

		private void OnPlayerDied()
		{
			CancelAbilitiesAndEffects();
		}

		private void OnTargetDied()
		{
			if (Targeting != null && (_combatTarget == null || _combatTarget.IsAlive)) return;
			CancelAbilitiesAndEffects();
		}

		private void OnWeaponChanged(WeaponData current, WeaponData previous)
		{
			CancelAbilitiesAndEffects();
		}

		private IEnumerator CompanionRoutine()
		{
			yield return new WaitForSeconds(1.2f);
			while (_playerHealth != null && _playerHealth.IsAlive)
			{
				if (Targeting != null && !_abilityRunning && !_isSwitching)
				{
					TryAcquireFieldTarget(10f);
					// Die automatische Begleitaktion darf keine ruhenden Gegner anlocken.
					if (_combatTarget != null && (_combatTarget.EnemyState == EnemyState.Idle || _combatTarget.EnemyState == EnemyState.Patrol)) _combatTarget = null;
				}
				if (ActiveData == null || _combatTarget == null || !_combatTarget.IsAlive || !TargetBattleActive || _abilityRunning || _isSwitching || Vector3.Distance(base.transform.position, _combatTarget.transform.position) > 10f)
				{
					yield return new WaitForSeconds(0.35f);
					continue;
				}
				bool defends = ActiveData != null && ActiveData.Role == EidraRole.Defend;
				yield return CompanionStrike(defends);
				yield return new WaitForSeconds(defends ? 3.15f : 1.85f);
			}
		}

		private IEnumerator CompanionStrike(bool defends)
		{
			EnemyControllerBase strikeTarget = _combatTarget;
			_actorAnimation?.PlayCompanionAttack();
			Vector3 baseScale = Vector3.one;
			float elapsed = 0f;
			while (elapsed < 0.18f)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / 0.18f);
				_companionLunge = Mathf.Lerp(0f, 1.9f, t);
				if (_visual != null)
				{
					_visual.localScale = baseScale * Mathf.Lerp(1f, 1.28f, t);
				}
				yield return null;
			}
			if (strikeTarget != null && strikeTarget.IsAlive && strikeTarget.isActiveAndEnabled && strikeTarget.EnemyState != EnemyState.Return
				&& (!(strikeTarget is BossController boss) || boss.BattleActive))
			{
				if (defends)
				{
					strikeTarget.AddStagger(11f);
				}
				else
				{
					strikeTarget.ApplyDamage(new DamageInfo(7f, 1.5f, strikeTarget.transform.position + Vector3.up, (_visual != null) ? _visual.gameObject : base.gameObject, isBackAttack: false, "eidra.noctarion.autonomous", "noctarion"));
				}
			}
			elapsed = 0f;
			while (elapsed < 0.18f)
			{
				elapsed += Time.deltaTime;
				float t2 = Mathf.Clamp01(elapsed / 0.18f);
				_companionLunge = Mathf.Lerp(1.9f, 0f, t2);
				if (_visual != null)
				{
					_visual.localScale = baseScale * Mathf.Lerp(1.28f, 1f, t2);
				}
				yield return null;
			}
			_companionLunge = 0f;
			if (_visual != null && !_abilityRunning)
			{
				_visual.localScale = baseScale;
			}
		}

		private void BuildRangePreview(Material rangeMaterial)
		{
			_rangePreview = new GameObject("Ability_Range_Preview");
			_rangePreview.name = "Ability_Range_Preview";
			_rangePreview.transform.SetParent(base.transform, worldPositionStays: false);
			_rangePreview.transform.localPosition = new Vector3(0f, 0.13f, 0f);
			_rangeLine = _rangePreview.AddComponent<LineRenderer>();
			_rangeLine.useWorldSpace = false;
			_rangeLine.loop = true;
			_rangeLine.widthMultiplier = 0.18f;
			_rangeLine.numCornerVertices = 4;
			_rangeLine.numCapVertices = 4;
			if (rangeMaterial != null)
			{
				_rangeLine.material = new Material(rangeMaterial);
			}
			_rangePreview.SetActive(value: false);
		}

		private void RefreshVisual()
		{
			if (ActiveData == null)
			{
				if (_visual != null)
				{
					_visual.gameObject.SetActive(value: false);
				}
				if (_rangePreview != null)
				{
					_rangePreview.SetActive(value: false);
				}
				if (_playerHealth != null)
				{
					_playerHealth.DamageTakenMultiplier = 1f;
				}
				return;
			}
			if (_visual != null)
			{
				_visual.gameObject.SetActive(value: true);
			}
			EnsureVisualVariant();
			if (_actorPresentation != null)
			{
				_actorPresentation.SetTint(Color.white);
			}
			if (_visual != null)
			{
				_visual.name = "Active_Eidra_" + ActiveData.DisplayName;
			}
			if (_playerHealth != null)
			{
				_playerHealth.DamageTakenMultiplier = ((ActiveData.Role == EidraRole.Defend) ? EidraPassiveText.DefendDamageTakenMultiplier : 1f);
			}
		}

		private void EnsureVisualVariant()
		{
			if (!(ActiveData == null) && !(_visual == null) && !(_visualVariantId == ActiveData.Id))
			{
				for (int num = _visual.childCount - 1; num >= 0; num--)
				{
					UnityEngine.Object.Destroy(_visual.GetChild(num).gameObject);
				}
				string id = ActiveData.Id;
				if (1 == 0)
				{
				}
				// Seit dem Kreaturen-Einbau die 3D-Wrapper (EidraVisual3DBuilder),
				// dieselbe Tabelle wie in EidraWildController.EnsureVisualVariant.
				string text = id switch
				{
					"terrock" => "Prefabs/Actors/3D/Terrock_3D",
					"noctarion" => "Prefabs/Actors/3D/Noctarion_3D",
					"ignivar" => "Prefabs/Actors/3D/Ignivar_3D",
					_ => "Prefabs/Visuals/Eidra/" + ActiveData.Id,
				};
				if (1 == 0)
				{
				}
				string text2 = text;
				GameObject gameObject = Resources.Load<GameObject>(text2);
				if (gameObject == null)
				{
					Debug.LogError("Canonical authored Eidra visual is missing: " + text2, this);
					_actorPresentation = null;
					_actorAnimation = null;
					_visualVariantId = null;
					return;
				}
				GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, _visual);
				gameObject2.name = gameObject.name;
				gameObject2.transform.localPosition = Vector3.zero;
				gameObject2.transform.localRotation = Quaternion.identity;
				gameObject2.transform.localScale = Vector3.one;
				_actorPresentation = ActorPresentationLocator.Find(gameObject2);
				_actorAnimation = ActorAnimationDriverLocator.Find(gameObject2);
				_visualVariantId = ActiveData.Id;
			}
		}

		private void EnsureVisual()
		{
			if (!(ActiveData == null) && !(_visual != null))
			{
				GameObject gameObject = new GameObject("Active_Eidra");
				gameObject.transform.localScale = Vector3.one;
				_visual = gameObject.transform;
				EnsureVisualVariant();
			}
		}

		private void OnDisable()
		{
			CancelForSceneTransition();
		}

		private void OnDestroy()
		{
			if (_boss != null) _boss.Died -= OnTargetDied;
			CancelAbilitiesAndEffects();
			if (_input != null)
			{
				_input.SwitchEidraPressed -= TrySwitch;
				_input.SkillPressed -= TryUseSkill;
				_input.SkillPreviewChanged -= SetRangePreview;
			}
			if (_playerHealth != null)
			{
				_playerHealth.Died -= OnPlayerDied;
			}
			if (_combatTarget != null)
			{
				_combatTarget.Died -= OnTargetDied;
			}
			if (_combat != null)
			{
				_combat.WeaponChanged -= OnWeaponChanged;
			}
			if (_playerHealth != null)
			{
				_playerHealth.DamageTakenMultiplier = 1f;
			}
		}
	}
}
