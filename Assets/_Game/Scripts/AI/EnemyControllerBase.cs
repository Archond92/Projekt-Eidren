using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Presentation;
using System.Collections;
using System;
using UnityEngine.AI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eidren.AI
{
	[RequireComponent(typeof(NavMeshAgent))]
	public abstract partial class EnemyControllerBase : MonoBehaviour, IDamageable, IStaggerable, IBackAttackTarget
	{
		private readonly EnemyStateMachine _stateMachine = new EnemyStateMachine();

		private Damageable _health;

		private NavMeshAgent _agent;

		private Coroutine _brainRoutine;

		private Coroutine _staggerRoutine;

		private float _staggerDuration;

		private bool _aggroActive;

		private bool _initialized;

		private bool _specialMovementActive;

		private bool _interactionPaused;

		private Vector3 _lastDestination;

		private float _nextRepathAt;

		private bool _hasDestination;

		private const float RepathInterval = 0.2f;

		private const float RepathDistanceSquared = 0.25f;

		public bool IsAlive => _health != null && _health.IsAlive;

		/// <summary>
		/// Anzeigename fuer die Statusleiste. Leer heisst: kein Namenslabel.
		/// </summary>
		public virtual string DisplayName => string.Empty;

		public Transform TargetTransform => base.transform;

		public float CurrentHealth => (_health != null) ? _health.CurrentHealth : 0f;

		public float MaxHealth => (_health != null) ? _health.MaxHealth : 1f;

		public float CurrentStagger { get; private set; }

		public float MaxStagger { get; private set; } = 1f;

		public EnemyState EnemyState => _stateMachine.CurrentState;

		public NavMeshAgent NavigationAgent => _agent;

		public Vector3 HomePosition { get; private set; }

		public float PerceptionRange => Navigation.DetectionRange;

		public float Protection => (_health != null) ? _health.Protection : 0f;

		protected Transform TrackedTarget { get; private set; }

		protected Damageable TrackedTargetHealth { get; private set; }

		protected EnemyNavigationData Navigation { get; private set; }

		protected string EnemyId { get; private set; } = "enemy";

		protected bool CombatAuthorized { get; private set; }

		public event Action<float, float> HealthChanged;

		public event Action<float, float> StaggerChanged;

		public event Action<EnemyState, EnemyState> EnemyStateChanged;

		public event Action<DamageInfo, float, float> DamageResolved;

		public event Action Died;

		protected void InitializeEnemy(string enemyId, float maxHealth, float maxStagger, float staggerDuration, Transform target, Damageable targetHealth, EnemyNavigationData navigation)
		{
			EnemyId = (string.IsNullOrWhiteSpace(enemyId) ? "enemy" : enemyId);
			TrackedTarget = target;
			TrackedTargetHealth = targetHealth;
			if (TrackedTargetHealth != null)
			{
				TrackedTargetHealth.Died += HandleTrackedTargetDeath;
			}
			Navigation = navigation;
			HomePosition = base.transform.position;
			MaxStagger = Mathf.Max(1f, maxStagger);
			_staggerDuration = Mathf.Max(0.01f, staggerDuration);
			_health = GetComponent<Damageable>();
			if (_health == null)
			{
				_health = base.gameObject.AddComponent<Damageable>();
			}
			_health.Initialize(maxHealth);
			_health.HealthChanged += ForwardHealthChanged;
			_health.Died += HandleDeath;
			_agent = GetComponent<NavMeshAgent>();
			if (_agent == null)
			{
				_agent = base.gameObject.AddComponent<NavMeshAgent>();
			}
			ConfigureAgent();
			PlaceAgentOnNavMesh();
			_stateMachine.StateChanged += ForwardEnemyStateChanged;
			_initialized = true;
			OnEnemyStateEntered(_stateMachine.CurrentState);
			StartBrain();
		}

		protected void ActivateCombat()
		{
			if (IsAlive)
			{
				CombatAuthorized = true;
				_aggroActive = true;
				StartBrain();
				SetEnemyState(EnemyState.Alert);
			}
		}

		public virtual void ApplyDamage(DamageInfo damage)
		{
			if (!CanReceiveDamage(damage))
			{
				return;
			}
			float num = Mathf.Max(0f, ModifyHealthDamage(damage));
			float num2 = Mathf.Max(0f, ModifyStaggerDamage(damage));
			if (EnemyState == EnemyState.Return)
			{
				num2 = 0f;
			}
			_health.ApplyDamage(new DamageInfo(num, num2, damage.HitPoint, damage.Source, damage.IsBackAttack, damage.AttackId, damage.SourceId));
			if (IsAlive)
			{
				float num3 = Mathf.Min(MaxStagger - CurrentStagger, num2);
				CurrentStagger += num3;
				this.StaggerChanged?.Invoke(CurrentStagger, MaxStagger);
				OnDamageResolved(damage, num, num3);
				// Gemeinsame Rueckmeldung fuer jeden Gegnertyp: genau ein Aufrufer,
				// damit ein Treffer weder doppelte noch fehlende Zahlen erzeugt.
				// Wirkungslose Treffer unterdrueckt SpawnDamage selbst.
				CombatFeedback.SpawnDamage(base.transform.position + Vector3.up * DamageFeedbackHeight, num, num3, damage.IsBackAttack);
				this.DamageResolved?.Invoke(damage, num, num3);
				if (CurrentStagger >= MaxStagger && EnemyState != EnemyState.Staggered && EnemyState != EnemyState.Return)
				{
					InterruptWithStagger();
				}
			}
		}

		public virtual void AddStagger(float amount)
		{
			ApplyDamage(new DamageInfo(0f, amount, base.transform.position, base.gameObject, isBackAttack: false, "stagger.direct", EnemyId));
		}

		public bool IsBackAttack(Transform attacker, float threshold = -0.35f)
		{
			return CombatTargetGeometry.IsBackAttack(base.transform, attacker, threshold);
		}

		protected virtual bool CanReceiveDamage(DamageInfo damage)
		{
			return IsAlive;
		}

		protected virtual float ModifyHealthDamage(DamageInfo damage)
		{
			return ApplyBackMark(damage);
		}

		protected virtual float ModifyStaggerDamage(DamageInfo damage)
		{
			return damage.StaggerDamage;
		}

		/// <summary>
		/// Hoehe der Schadenszahl ueber dem Gegnerursprung. Grosse Gegner
		/// ueberschreiben den Wert, damit die Zahl nicht in der Figur liegt.
		/// </summary>
		protected virtual float DamageFeedbackHeight => 2.4f;

		protected virtual void OnDamageResolved(DamageInfo originalDamage, float appliedHealthDamage, float appliedStaggerDamage)
		{
		}

		protected abstract IEnumerator ExecuteAttack();

		protected virtual void OnAttackInterrupted()
		{
		}

		protected virtual void OnStaggerStarted()
		{
		}

		protected virtual void OnStaggerCompleted()
		{
		}

		protected virtual void OnReturnedHome()
		{
			_health.HealToFull();
			ResetStagger();
		}

		protected virtual void OnEnemyDied()
		{
		}

		protected virtual void OnEnemyStateEntered(EnemyState state)
		{
		}

		protected bool ShouldReturnToHome()
		{
			return TrackedTarget != null && EnemyLeashRules.ShouldReturn(HomePosition, base.transform.position, TrackedTarget.position, Navigation.LeashRange, Navigation.LeashFollowDistance);
		}

		protected float FlatDistanceToTarget()
		{
			return (TrackedTarget == null) ? (1f / 0f) : FlatDistance(base.transform.position, TrackedTarget.position);
		}

		protected Vector3 FlatDirectionToTarget()
		{
			return (TrackedTarget == null) ? Vector3.zero : FlatDirection(base.transform.position, TrackedTarget.position);
		}

		protected void PauseNavigationForSpecialMovement()
		{
			StopNavigation();
			_specialMovementActive = true;
			if (_agent != null && _agent.enabled)
			{
				_agent.updateRotation = false;
			}
		}

		protected void SetInteractionPaused(bool paused)
		{
			if (_interactionPaused == paused)
			{
				return;
			}
			_interactionPaused = paused;
			if (paused)
			{
				if (_brainRoutine != null)
				{
					StopCoroutine(_brainRoutine);
				}
				_brainRoutine = null;
				OnAttackInterrupted();
				ResumeNavigationAfterSpecialMovement();
				StopNavigation();
			}
			else
			{
				StartBrain();
			}
		}

		protected void MoveDuringSpecialAttack(Vector3 motion)
		{
			if (_specialMovementActive && !(_agent == null) && _agent.enabled && _agent.isOnNavMesh)
			{
				_agent.Move(motion);
			}
		}

		protected void ResumeNavigationAfterSpecialMovement()
		{
			if (!_specialMovementActive)
			{
				return;
			}
			_specialMovementActive = false;
			if (!(_agent == null) && _agent.enabled)
			{
				_agent.updateRotation = false;
				if (_agent.isOnNavMesh)
				{
					_agent.isStopped = true;
					_agent.ResetPath();
				}
			}
		}

		protected void FaceDirection(Vector3 direction)
		{
			direction.y = 0f;
			if (!(direction.sqrMagnitude < 0.001f))
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(direction.normalized), Navigation.AngularSpeed * Time.deltaTime);
			}
		}

		private IEnumerator BrainRoutine()
		{
			yield return null;
			while (IsAlive)
			{
				if (CanReacquireTarget())
				{
					_aggroActive = true;
					SetEnemyState(EnemyState.Alert);
				}
				if (!CombatAuthorized || !_aggroActive)
				{
					yield return PatrolRoutine();
					continue;
				}
				if (!HasLivingTarget())
				{
					StopNavigation();
					SetEnemyState(EnemyState.Idle);
					yield return new WaitForSeconds(0.2f);
					continue;
				}
				if (ShouldReturnToHome())
				{
					_aggroActive = false;
					yield return ReturnRoutine();
					continue;
				}
				if (EnemyState == EnemyState.Alert)
				{
					yield return AlertRoutine();
					continue;
				}
				if (FlatDistanceToTarget() > Navigation.AttackRange)
				{
					SetEnemyState(EnemyState.Chase);
					MoveTowards(TrackedTarget.position);
					yield return null;
					continue;
				}
				StopNavigation();
				SetEnemyState(EnemyState.ChooseAttack);
				yield return ExecuteAttack();
				if (!IsAlive || EnemyState == EnemyState.Staggered)
				{
					break;
				}
				if (ShouldReturnToHome())
				{
					_aggroActive = false;
					yield return ReturnRoutine();
				}
				yield return null;
			}
		}

		private IEnumerator AlertRoutine()
		{
			StopNavigation();
			SetEnemyState(EnemyState.Alert);
			float elapsed = 0f;
			while (elapsed < Mathf.Max(0f, Navigation.AlertDuration))
			{
				if (!HasLivingTarget() || ShouldReturnToHome())
				{
					yield break;
				}
				elapsed += Time.deltaTime;
				yield return null;
			}
			if (HasLivingTarget() && !ShouldReturnToHome())
			{
				SetEnemyState(EnemyState.Chase);
			}
		}

		private IEnumerator PatrolRoutine()
		{
			StopNavigation();
			SetEnemyState(EnemyState.Idle);
			float elapsed = 0f;
			while (elapsed < Navigation.PatrolWait)
			{
				if (CombatAuthorized && _aggroActive)
				{
					yield break;
				}
				// F31-020: Auch waehrend der Patrouille wittern — vorher
				// konnte nur der Schleifenkopf des Gehirns den Spieler
				// entdecken, und der lief erst nach der Patrouille wieder.
				if (TryReacquireDuringPatrol())
				{
					yield break;
				}
				elapsed += Time.deltaTime;
				yield return null;
			}
			if (Navigation.PatrolRadius <= 0f || !TryFindPatrolDestination(out var destination))
			{
				yield break;
			}
			SetEnemyState(EnemyState.Patrol);
			SetStoppingDistanceForCombat(combat: false);
			while (!EnemyLeashRules.HasReached(base.transform.position, destination, Navigation.ReturnTolerance))
			{
				if (CombatAuthorized && _aggroActive)
				{
					SetStoppingDistanceForCombat(combat: true);
					yield break;
				}
				if (TryReacquireDuringPatrol())
				{
					yield break;
				}
				MoveTowards(destination);
				yield return null;
			}
			StopNavigation();
			SetStoppingDistanceForCombat(combat: true);
		}

		private bool TryReacquireDuringPatrol()
		{
			if (!CanReacquireTarget())
			{
				return false;
			}
			_aggroActive = true;
			SetStoppingDistanceForCombat(combat: true);
			SetEnemyState(EnemyState.Alert);
			return true;
		}

		private void SetStoppingDistanceForCombat(bool combat)
		{
			if (_agent != null)
			{
				_agent.stoppingDistance = (combat ? Mathf.Max(0f, Navigation.AttackRange - 0.1f) : 0f);
			}
		}

		private IEnumerator ReturnRoutine()
		{
			OnAttackInterrupted();
			ResumeNavigationAfterSpecialMovement();
			SetEnemyState(EnemyState.Return);
			// F31-020: Ausserhalb des Kampfes ohne Stoppdistanz fahren — mit
			// der Kampf-Stoppdistanz (AttackRange - 0,1) haelt der Agent vor
			// Heim- und Patrouillenzielen an und die Ankunftstoleranz (0,55)
			// wird nie erreicht: Der Gegner hing danach ewig in der Schleife.
			SetStoppingDistanceForCombat(combat: false);
			yield return null;
			while (IsAlive && !EnemyLeashRules.HasReached(base.transform.position, HomePosition, Navigation.ReturnTolerance))
			{
				MoveTowards(HomePosition);
				yield return null;
			}
			StopNavigation();
			SetStoppingDistanceForCombat(combat: true);
			if (IsAlive)
			{
				OnReturnedHome();
				SetEnemyState(EnemyState.Idle);
			}
		}

		private IEnumerator ReturnAfterTargetDeathRoutine()
		{
			yield return ReturnRoutine();
			_brainRoutine = null;
			StartBrain();
		}

		private void MoveTowards(Vector3 destination)
		{
			if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
			{
				_agent.isStopped = false;
				if (EnemyRepathPolicy.ShouldUpdateDestination(_hasDestination, Time.time, _nextRepathAt, _lastDestination, destination, 0.25f))
				{
					_agent.SetDestination(destination);
					_lastDestination = destination;
					_nextRepathAt = Time.time + 0.2f;
					_hasDestination = true;
				}
			}
		}

		private bool TryFindPatrolDestination(out Vector3 destination)
		{
			destination = HomePosition;
			Vector2 vector = UnityEngine.Random.insideUnitCircle * Navigation.PatrolRadius;
			Vector3 sourcePosition = HomePosition + new Vector3(vector.x, 0f, vector.y);
			if (!NavMesh.SamplePosition(sourcePosition, out var hit, Navigation.NavMeshSampleDistance, -1))
			{
				return false;
			}
			destination = hit.position;
			return true;
		}

		private bool CanReacquireTarget()
		{
			return CombatAuthorized && !_aggroActive && HasLivingTarget() && FlatDistanceToTarget() <= Navigation.DetectionRange;
		}

		private bool HasLivingTarget()
		{
			return TrackedTarget != null && TrackedTargetHealth != null && TrackedTargetHealth.IsAlive;
		}

		private void InterruptWithStagger()
		{
			if (EnemyState != EnemyState.Return && EnemyState != EnemyState.Dead)
			{
				if (_brainRoutine != null)
				{
					StopCoroutine(_brainRoutine);
				}
				_brainRoutine = null;
				OnAttackInterrupted();
				ResumeNavigationAfterSpecialMovement();
				StopNavigation();
				if (_staggerRoutine != null)
				{
					StopCoroutine(_staggerRoutine);
				}
				_staggerRoutine = StartCoroutine(StaggerRoutine());
			}
		}

		private IEnumerator StaggerRoutine()
		{
			SetEnemyState(EnemyState.Staggered);
			OnStaggerStarted();
			float elapsed = 0f;
			while (elapsed < _staggerDuration && IsAlive)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}
			if (IsAlive)
			{
				ResetStagger();
				OnStaggerCompleted();
				_staggerRoutine = null;
				_aggroActive = CombatAuthorized;
				StartBrain();
			}
		}

		private void ResetStagger()
		{
			CurrentStagger = 0f;
			this.StaggerChanged?.Invoke(CurrentStagger, MaxStagger);
		}

		private void HandleDeath()
		{
			StopAllCoroutines();
			_brainRoutine = null;
			_staggerRoutine = null;
			OnAttackInterrupted();
			ResumeNavigationAfterSpecialMovement();
			StopNavigation();
			if (_agent != null)
			{
				_agent.enabled = false;
			}
			SetEnemyState(EnemyState.Dead);
			OnEnemyDied();
			this.Died?.Invoke();
		}

		private void HandleTrackedTargetDeath()
		{
			if (_initialized && base.isActiveAndEnabled && IsAlive)
			{
				if (_brainRoutine != null)
				{
					StopCoroutine(_brainRoutine);
				}
				if (_staggerRoutine != null)
				{
					StopCoroutine(_staggerRoutine);
				}
				_brainRoutine = null;
				_staggerRoutine = null;
				_aggroActive = false;
				OnAttackInterrupted();
				ResumeNavigationAfterSpecialMovement();
				StopNavigation();
				_brainRoutine = StartCoroutine(ReturnAfterTargetDeathRoutine());
			}
		}

		private void ConfigureAgent()
		{
			_agent.speed = Mathf.Max(0.01f, Navigation.MoveSpeed);
			_agent.acceleration = Mathf.Max(0.01f, Navigation.Acceleration);
			_agent.angularSpeed = Mathf.Max(0.01f, Navigation.AngularSpeed);
			_agent.stoppingDistance = Mathf.Max(0f, Navigation.AttackRange - 0.1f);
			_agent.radius = Mathf.Max(0.05f, Navigation.AgentRadius);
			_agent.height = Mathf.Max(_agent.radius * 2f, Navigation.AgentHeight);
			_agent.autoBraking = true;
			_agent.updateRotation = false;
		}

		private void Update()
		{
			if (!_specialMovementActive && !(_agent == null) && _agent.enabled && _agent.isOnNavMesh && !(_agent.desiredVelocity.sqrMagnitude < 0.0025f))
			{
				FaceDirection(_agent.desiredVelocity);
			}
			// F32-005: Nachbrand haengt am bestehenden Update, damit kein
			// zweiter Frame-Pfad entsteht (§8).
			TickBurn(Time.deltaTime);
		}

		private void PlaceAgentOnNavMesh()
		{
			if (NavMesh.SamplePosition(base.transform.position, out var hit, Navigation.NavMeshSampleDistance, -1))
			{
				base.transform.position = hit.position;
				if (!_agent.enabled)
				{
					_agent.enabled = true;
				}
				if (_agent.isOnNavMesh)
				{
					_agent.Warp(hit.position);
				}
			}
			else
			{
				_agent.enabled = false;
				Debug.LogWarning("Enemy '" + EnemyId + "' could not find a NavMesh position " + $"within {Navigation.NavMeshSampleDistance:0.##} m.", this);
			}
		}

		private void StopNavigation()
		{
			if (!(_agent == null) && _agent.enabled && _agent.isOnNavMesh)
			{
				_agent.isStopped = true;
				_agent.ResetPath();
				_hasDestination = false;
				_nextRepathAt = 0f;
			}
		}

		protected void SetEnemyState(EnemyState state)
		{
			_stateMachine.TrySetState(state);
		}

		private void StartBrain()
		{
			if (_initialized && base.isActiveAndEnabled && IsAlive && !_interactionPaused && _brainRoutine == null && _staggerRoutine == null)
			{
				_brainRoutine = StartCoroutine(BrainRoutine());
			}
		}

		private void ForwardHealthChanged(float current, float maximum)
		{
			this.HealthChanged?.Invoke(current, maximum);
		}

		private void ForwardEnemyStateChanged(EnemyState previous, EnemyState current)
		{
			this.EnemyStateChanged?.Invoke(previous, current);
			OnEnemyStateEntered(current);
		}

		private static float FlatDistance(Vector3 first, Vector3 second)
		{
			first.y = 0f;
			second.y = 0f;
			return Vector3.Distance(first, second);
		}

		private static Vector3 FlatDirection(Vector3 from, Vector3 to)
		{
			Vector3 vector = to - from;
			vector.y = 0f;
			return (vector.sqrMagnitude < 0.001f) ? Vector3.zero : vector.normalized;
		}

		private void OnEnable()
		{
			CombatTargetRegistry.Register(this);
			MinimapRegistry.Register(this);
			StartBrain();
		}

		private void OnDisable()
		{
			CombatTargetRegistry.Unregister(this);
			MinimapRegistry.Unregister(this);
			StopAllCoroutines();
			_brainRoutine = null;
			_staggerRoutine = null;
			OnAttackInterrupted();
			ResumeNavigationAfterSpecialMovement();
			StopNavigation();
		}

		private void OnDestroy()
		{
			CombatTargetRegistry.Unregister(this);
			MinimapRegistry.Unregister(this);
			_stateMachine.StateChanged -= ForwardEnemyStateChanged;
			if (TrackedTargetHealth != null)
			{
				TrackedTargetHealth.Died -= HandleTrackedTargetDeath;
			}
			if (!(_health == null))
			{
				_health.HealthChanged -= ForwardHealthChanged;
				_health.Died -= HandleDeath;
			}
		}
	}

	public static class EnemyRepathPolicy
	{
		public static bool ShouldUpdateDestination(bool hasDestination, float currentTime, float nextRepathAt, Vector3 lastDestination, Vector3 destination, float minimumDistanceSquared)
		{
			return !hasDestination || currentTime >= nextRepathAt || (destination - lastDestination).sqrMagnitude >= minimumDistanceSquared;
		}
	}
}
