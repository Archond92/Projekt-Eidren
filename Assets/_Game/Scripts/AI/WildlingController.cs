using Eidren.Combat;
using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Interaction;
using Eidren.Presentation;
using System.Collections;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.AI
{
	public sealed class WildlingController : EnemyControllerBase
	{
		[SerializeField]
		private EnemyDefinition definition;

		[SerializeField]
		private EnemyMeleeHitbox attackHitbox;

		[SerializeField]
		private GameObject attackTelegraph;

		[SerializeField]
		private Transform visualRoot;

		[SerializeField]
		private GameObject projectileVisualPrefab;

		private Coroutine _perceptionRoutine;

		private Quaternion _visualRestRotation;

		private Vector3 _telegraphRestScale;

		private bool _wildlingInitialized;

		private bool _discoveryRaised;

		private bool _lootRequested;

		private SpriteActorAnimator _spriteAnimator;

		private float _postStaggerResistanceUntil;

		private float _resolvedAttackDamage;

		private int _attackSequence;

		private Vector3 _hitboxRestSize;

		private Vector3 _hitboxRestCenter;

		public EnemyDefinition Definition => definition;

		public override string DisplayName => (definition != null) ? definition.DisplayName : string.Empty;

		public EnemyMeleeHitbox AttackHitbox => attackHitbox;

		public bool HasActiveTelegraph => attackTelegraph != null && attackTelegraph.activeSelf;

		public bool HasActiveAttack => attackHitbox != null && attackHitbox.IsWindowActive;

		public event Action<WildlingFeedbackCue> FeedbackRequested;

		public event Action<WildlingLootRequest> LootRequested;

		public void ConfigurePrefab(EnemyDefinition configuredDefinition, EnemyMeleeHitbox configuredHitbox, GameObject configuredTelegraph, Transform configuredVisualRoot, GameObject configuredProjectileVisualPrefab = null)
		{
			definition = configuredDefinition ?? throw new ArgumentNullException("configuredDefinition");
			attackHitbox = configuredHitbox ?? throw new ArgumentNullException("configuredHitbox");
			attackTelegraph = configuredTelegraph ?? throw new ArgumentNullException("configuredTelegraph");
			visualRoot = configuredVisualRoot ?? throw new ArgumentNullException("configuredVisualRoot");
			projectileVisualPrefab = configuredProjectileVisualPrefab;
			attackTelegraph.SetActive(value: false);
		}

		public void Initialize(Transform player, Damageable playerHealth)
		{
			if (!_wildlingInitialized)
			{
				if (definition == null || attackHitbox == null || attackTelegraph == null || visualRoot == null)
				{
					throw new InvalidOperationException("Wildling '" + base.name + "' is missing prefab references.");
				}
				_visualRestRotation = visualRoot.localRotation;
				_telegraphRestScale = attackTelegraph.transform.localScale;
				_hitboxRestSize = attackHitbox.Hitbox.size;
				_hitboxRestCenter = attackHitbox.Hitbox.center;
				_spriteAnimator = visualRoot.GetComponentInChildren<SpriteActorAnimator>(includeInactive: true);
				_spriteAnimator?.Bind(this);
				_wildlingInitialized = true;
				InitializeEnemy(definition.Id, definition.MaximumHealth, definition.MaximumStagger, definition.StaggerDuration, player, playerHealth, definition.Navigation);
				GetComponent<Damageable>().Protection = definition.Protection;
				_perceptionRoutine = StartCoroutine(PerceptionRoutine());
			}
		}

		public bool TryDetectTarget()
		{
			if (!_wildlingInitialized || !base.IsAlive || base.CombatAuthorized || base.TrackedTarget == null || base.TrackedTargetHealth == null || !base.TrackedTargetHealth.IsAlive)
			{
				return false;
			}
			Vector3 vector = base.TrackedTarget.position - base.transform.position;
			vector.y = 0f;
			if (vector.sqrMagnitude > base.PerceptionRange * base.PerceptionRange)
			{
				return false;
			}
			ActivateCombat();
			return true;
		}

		protected override void OnAttackInterrupted()
		{
			if (attackHitbox != null)
			{
				attackHitbox.EndWindow();
				RestoreHitbox();
			}
			if (attackTelegraph != null)
			{
				attackTelegraph.SetActive(value: false);
				attackTelegraph.transform.localScale = ((_telegraphRestScale == Vector3.zero) ? Vector3.one : _telegraphRestScale);
			}
		}

		protected override void OnDamageResolved(DamageInfo originalDamage, float appliedHealthDamage, float appliedStaggerDamage)
		{
			if (!base.CombatAuthorized)
			{
				ActivateCombat();
			}
			this.FeedbackRequested?.Invoke(WildlingFeedbackCue.Hit);
			StyleCue cue = (originalDamage.AttackId.Contains(".daggers.") ? StyleCue.DaggerHit : StyleCue.HammerHit);
			CombatFeedback.SpawnStyleCue(originalDamage.HitPoint, cue);
		}

		protected override void OnStaggerStarted()
		{
			OnAttackInterrupted();
			this.FeedbackRequested?.Invoke(WildlingFeedbackCue.Stagger);
			if (visualRoot != null && _spriteAnimator == null)
			{
				visualRoot.localRotation = _visualRestRotation * Quaternion.Euler(0f, 0f, 22f);
			}
			CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 2.6f, "GESTAGGERT", new Color(1f, 0.68f, 0.18f), definition.StaggerDuration);
			CombatFeedback.SpawnStyleCue(base.transform.position + Vector3.up, StyleCue.Stagger);
		}

		protected override void OnReturnedHome()
		{
			base.OnReturnedHome();
			_discoveryRaised = false;
			if (visualRoot != null && _spriteAnimator == null)
			{
				visualRoot.localRotation = _visualRestRotation;
			}
		}

		protected override void OnEnemyDied()
		{
			OnAttackInterrupted();
			this.FeedbackRequested?.Invoke(WildlingFeedbackCue.Death);
			if (!_lootRequested)
			{
				_lootRequested = true;
				this.LootRequested?.Invoke(new WildlingLootRequest(definition.Id, definition.ExperienceReward, base.transform.position, definition.LootTable, base.transform));
			}
			if (_spriteAnimator == null)
			{
				EnemyDeathPose.ApplyTipOver(visualRoot, _visualRestRotation);
			}
			CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 2.2f, "BESIEGT", new Color(0.72f, 0.74f, 0.7f));
			StartCoroutine(DisableAfterDeath());
		}

		protected override void OnEnemyStateEntered(EnemyState state)
		{
			if (state == EnemyState.Alert && !_discoveryRaised)
			{
				_discoveryRaised = true;
				this.FeedbackRequested?.Invoke(WildlingFeedbackCue.Discovered);
				CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 2.7f, "!", new Color(1f, 0.78f, 0.24f), (definition != null) ? definition.Navigation.AlertDuration : 0.25f);
			}
		}

		private IEnumerator PerceptionRoutine()
		{
			while (base.IsAlive && !base.CombatAuthorized)
			{
				TryDetectTarget();
				yield return new WaitForSeconds(0.1f);
			}
			_perceptionRoutine = null;
		}

		private void DamageTarget(CombatTarget target)
		{
			if (!base.IsAlive || base.EnemyState != EnemyState.ExecuteAttack || target.Damageable == null || !target.IsAlive)
			{
				return;
			}
			float num = ((target.Damageable is Damageable damageable) ? damageable.CurrentHealth : 0f);
			target.Damageable.ApplyDamage(new DamageInfo((_resolvedAttackDamage > 0f) ? _resolvedAttackDamage : definition.AttackDamage, 0f, target.Transform.position + Vector3.up, base.gameObject, isBackAttack: false, definition.Id + ".melee", definition.Id));
			if (target.Damageable is Damageable damageable2)
			{
				float num2 = num - damageable2.CurrentHealth;
				if (num2 > 0f)
				{
					CombatFeedback.SpawnPlayerDamage(target.Transform.position + Vector3.up * 2.5f, num2);
					CombatFeedback.SpawnStyleCue(target.Transform.position + Vector3.up, StyleCue.EnemyHit);
				}
			}
		}

		private bool ShouldAbortAttack()
		{
			return !base.IsAlive || base.TrackedTarget == null || base.TrackedTargetHealth == null || !base.TrackedTargetHealth.IsAlive || ShouldReturnToHome();
		}

		private IEnumerator DisableAfterDeath()
		{
			if (definition.DeathDisableDelay > 0f)
			{
				yield return new WaitForSeconds(definition.DeathDisableDelay);
			}
			EnemyLootContainer loot = GetComponentInChildren<EnemyLootContainer>(includeInactive: true);
			while (loot != null && !loot.IsEmpty)
			{
				yield return new WaitForSeconds(0.25f);
			}
			base.gameObject.SetActive(value: false);
		}

		protected override IEnumerator ExecuteAttack()
		{
			SetEnemyState(EnemyState.Telegraph);
			Vector3 direction = FlatDirectionToTarget();
			if (direction.sqrMagnitude > 0.001f)
			{
				base.transform.rotation = Quaternion.LookRotation(direction);
			}
			attackTelegraph.SetActive(value: true);
			this.FeedbackRequested?.Invoke(WildlingFeedbackCue.Attack);
			float elapsed = 0f;
			while (elapsed < definition.TelegraphDuration)
			{
				if (ShouldAbortAttack())
				{
					OnAttackInterrupted();
					yield break;
				}
				elapsed += Time.deltaTime;
				float pulse = 0.82f + Mathf.PingPong(elapsed * 2.8f, 0.28f);
				attackTelegraph.transform.localScale = _telegraphRestScale * pulse;
				yield return null;
			}
			attackTelegraph.SetActive(value: false);
			attackTelegraph.transform.localScale = _telegraphRestScale;
			SetEnemyState(EnemyState.ExecuteAttack);
			_attackSequence++;
			_resolvedAttackDamage = definition.AttackDamage;
			if (definition.CombatStyle == EnemyCombatStyle.Projectile)
			{
				yield return ExecuteProjectile();
				if (!ShouldAbortAttack())
				{
					yield return Recover();
				}
				yield break;
			}
			bool charge = definition.CombatStyle == EnemyCombatStyle.Charge;
			if (definition.CombatStyle == EnemyCombatStyle.AreaElite && _attackSequence % 3 == 0)
			{
				attackHitbox.Hitbox.size = new Vector3(5.2f, 1.8f, 5.2f);
				attackHitbox.Hitbox.center = new Vector3(0f, 0.9f, 0f);
				_resolvedAttackDamage = definition.SecondaryAttackDamage;
			}
			if (charge)
			{
				PauseNavigationForSpecialMovement();
			}
			attackHitbox.BeginWindow();
			elapsed = 0f;
			while (elapsed < definition.AttackWindowDuration)
			{
				if (ShouldAbortAttack())
				{
					OnAttackInterrupted();
					yield break;
				}
				attackHitbox.Evaluate(base.transform, base.TrackedTargetHealth, DamageTarget);
				if (charge)
				{
					MoveDuringSpecialAttack(direction * 8.5f * Time.deltaTime);
				}
				elapsed += Time.deltaTime;
				yield return null;
			}
			attackHitbox.EndWindow();
			RestoreHitbox();
			if (charge)
			{
				ResumeNavigationAfterSpecialMovement();
			}
			if (!ShouldAbortAttack())
			{
				yield return Recover();
			}
		}

		private IEnumerator ExecuteProjectile()
		{
			if (projectileVisualPrefab == null || base.TrackedTarget == null)
			{
				yield break;
			}
			Vector3 start = base.transform.position + Vector3.up * 1.15f;
			Vector3 target = base.TrackedTarget.position + Vector3.up * 0.9f;
			GameObject projectile = UnityEngine.Object.Instantiate(projectileVisualPrefab, start, Quaternion.LookRotation((target - start).normalized));
			float duration = Mathf.Max(0.35f, definition.AttackWindowDuration);
			float elapsed = 0f;
			while (elapsed < duration && projectile != null && !ShouldAbortAttack())
			{
				elapsed += Time.deltaTime;
				projectile.transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
				yield return null;
			}
			if (!(projectile == null))
			{
				if (base.TrackedTarget != null && Vector3.Distance(projectile.transform.position, base.TrackedTarget.position + Vector3.up * 0.9f) < 0.8f)
				{
					DamageTrackedTarget(_resolvedAttackDamage, definition.Id + ".projectile");
				}
				UnityEngine.Object.Destroy(projectile);
			}
		}

		private IEnumerator Recover()
		{
			SetEnemyState(EnemyState.Recover);
			float elapsed = 0f;
			while (elapsed < definition.RecoveryDuration && !ShouldAbortAttack())
			{
				elapsed += Time.deltaTime;
				yield return null;
			}
		}

		private void RestoreHitbox()
		{
			if (!(attackHitbox == null) && !(attackHitbox.Hitbox == null))
			{
				attackHitbox.Hitbox.size = _hitboxRestSize;
				attackHitbox.Hitbox.center = _hitboxRestCenter;
			}
		}

		protected override void OnStaggerCompleted()
		{
			if (visualRoot != null && _spriteAnimator == null)
			{
				visualRoot.localRotation = _visualRestRotation;
			}
			_postStaggerResistanceUntil = Time.time + definition.PostStaggerResistanceDuration;
		}

		protected override float ModifyStaggerDamage(DamageInfo damage)
		{
			float num = base.ModifyStaggerDamage(damage);
			return (Time.time < _postStaggerResistanceUntil) ? (num * definition.PostStaggerDamageMultiplier) : num;
		}

		private void DamageTrackedTarget(float damage, string attackId)
		{
			if (!(base.TrackedTargetHealth == null) && base.TrackedTargetHealth.IsAlive)
			{
				float currentHealth = base.TrackedTargetHealth.CurrentHealth;
				base.TrackedTargetHealth.ApplyDamage(new DamageInfo(damage, 0f, base.TrackedTarget.position + Vector3.up, base.gameObject, isBackAttack: false, attackId, definition.Id));
				float num = currentHealth - base.TrackedTargetHealth.CurrentHealth;
				if (num > 0f)
				{
					CombatFeedback.SpawnPlayerDamage(base.TrackedTarget.position + Vector3.up * 2.5f, num);
				}
			}
		}
	}

	public readonly struct WildlingLootRequest
	{
		public string EnemyId { get; }

		public int ExperienceReward { get; }

		public Vector3 Position { get; }

		public LootTableDefinition LootTable { get; }

		public Transform Source { get; }

		public WildlingLootRequest(string enemyId, Vector3 position, LootTableDefinition lootTable, Transform source)
			: this(enemyId, 0, position, lootTable, source)
		{
		}

		public WildlingLootRequest(string enemyId, int experienceReward, Vector3 position, LootTableDefinition lootTable, Transform source)
		{
			EnemyId = enemyId ?? string.Empty;
			ExperienceReward = Mathf.Max(0, experienceReward);
			Position = position;
			LootTable = lootTable;
			Source = source;
		}
	}
}
