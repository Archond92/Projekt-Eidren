using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using Eidren.Presentation;
using UnityEngine;

namespace Eidren.Gameplay.Presentation
{
	public sealed class SpriteActorAnimator : MonoBehaviour, IActorAnimationDriver
	{
		[SerializeField]
		private MonoBehaviour presentationBehaviour;

		[SerializeField]
		private Transform movementRoot;

		[SerializeField]
		private float movementThreshold = 0.025f;

		[SerializeField]
		private float directionHysteresisDegrees = 6f;

		[SerializeField]
		private string enemyTelegraphStem = "";

		[SerializeField]
		private string enemyAttackStem = "";

		[SerializeField]
		private string enemyStaggerStem = "";

		[SerializeField]
		private string enemyFleeStem = "";

		[SerializeField]
		private string bossFrontStem = "";

		[SerializeField]
		private string bossChargeStem = "";

		[SerializeField]
		private string bossSpinStem = "";

		[SerializeField]
		private string bossReturnStem = "";

		private IActorPresentation _presentation;

		private EightDirectionResolver _directionResolver;

		private EnemyControllerBase _enemy;

		private ActorSpriteVfx _vfx;

		private Vector3 _lastPosition;

		private float _forcedStateUntil;

		private float _forcedStateDuration;

		private ActorVisualState _forcedState;

		private BossController _boss;

		private string _bossAttackStem = string.Empty;

		public IActorPresentation Presentation => _presentation;

		public void Configure(MonoBehaviour configuredPresentation, Transform configuredMovementRoot)
		{
			presentationBehaviour = configuredPresentation;
			movementRoot = configuredMovementRoot;
			ResolvePresentation();
		}

		public void ConfigureEnemyStateStems(string telegraphStem, string attackStem, string staggerStem, string fleeStem)
		{
			enemyTelegraphStem = telegraphStem ?? string.Empty;
			enemyAttackStem = attackStem ?? string.Empty;
			enemyStaggerStem = staggerStem ?? string.Empty;
			enemyFleeStem = fleeStem ?? string.Empty;
		}

		public void ConfigureBossStateStems(string frontStem, string chargeStem, string spinStem, string returnStem)
		{
			bossFrontStem = frontStem ?? string.Empty;
			bossChargeStem = chargeStem ?? string.Empty;
			bossSpinStem = spinStem ?? string.Empty;
			bossReturnStem = returnStem ?? string.Empty;
		}

		public void PlayFlee()
		{
			if (_presentation is IAuthoredStatePresentation authored && !string.IsNullOrWhiteSpace(enemyFleeStem))
			{
				_forcedStateUntil = 0f;
				authored.SetAuthoredState(enemyFleeStem, 0f, loop: false, restart: true);
				ResolveVfx()?.PlayAbility(1);
			}
		}

		public void Bind(EnemyControllerBase enemy)
		{
			if (_enemy == enemy)
			{
				return;
			}
			Unbind();
			_enemy = enemy;
			if (!(_enemy == null))
			{
				_enemy.EnemyStateChanged += OnEnemyStateChanged;
				_enemy.DamageResolved += OnDamageResolved;
				_boss = _enemy as BossController;
				if (_boss != null)
				{
					_boss.AttackSelected += OnBossAttackSelected;
				}
			}
		}

		public void PlayHit()
		{
			PlayTemporary(ActorVisualState.Hit, 0.16f);
			ResolveVfx()?.PlayHit();
		}

		public void PlayAbility(int skillIndex)
		{
			PlayTemporary((skillIndex == 0) ? ActorVisualState.Ability1 : ActorVisualState.Ability2, 0.45f);
			ResolveVfx()?.PlayAbility(skillIndex);
		}

		public void PlayCompanionAttack()
		{
			PlayTemporary(ActorVisualState.Ability1, 0.38f);
			ResolveVfx()?.PlayAbility(0);
		}

		private void Awake()
		{
			if (movementRoot == null)
			{
				movementRoot = base.transform;
			}
			ResolvePresentation();
			_directionResolver = new EightDirectionResolver(directionHysteresisDegrees);
			_lastPosition = movementRoot.position;
		}

		private void Start()
		{
			Bind(GetComponentInParent<EnemyControllerBase>());
			_presentation?.SetVisualState(ActorVisualState.Idle);
		}

		private void Update()
		{
			if (_presentation == null || movementRoot == null)
			{
				return;
			}
			Vector3 position = movementRoot.position;
			Vector3 worldDirection = position - _lastPosition;
			worldDirection.y = 0f;
			_lastPosition = position;
			bool flag = worldDirection.sqrMagnitude > movementThreshold * movementThreshold * Mathf.Max(Time.deltaTime, 0.0001f) * Mathf.Max(Time.deltaTime, 0.0001f);
			(_presentation as ILocomotionPresentation)?.SetLocomotion(worldDirection, flag);
			if (flag)
			{
				ActorFacing8 facing = _directionResolver.Resolve(worldDirection);
				_presentation.SetFacing(facing);
			}
			if (Time.time < _forcedStateUntil)
			{
				_presentation.SetVisualState(_forcedState, 1f - (_forcedStateUntil - Time.time) / Mathf.Max(0.001f, _forcedStateDuration));
			}
			else if (_enemy != null)
			{
				if (!flag)
				{
					Vector3 forward = _enemy.transform.forward;
					forward.y = 0f;
					if (forward.sqrMagnitude > 0.001f)
					{
						_presentation.SetFacing(_directionResolver.Resolve(forward));
					}
				}
			}
			else
			{
				_presentation.SetVisualState(flag ? ActorVisualState.Move : ActorVisualState.Idle);
			}
		}

		private void OnEnemyStateChanged(EnemyState previous, EnemyState current)
		{
			if (_presentation == null || Time.time < _forcedStateUntil || (!string.IsNullOrWhiteSpace(_bossAttackStem) && (current == EnemyState.Telegraph || current == EnemyState.ExecuteAttack)))
			{
				return;
			}
			if (current != EnemyState.Telegraph && current != EnemyState.ExecuteAttack)
			{
				_bossAttackStem = string.Empty;
			}
			if (_presentation is IAuthoredStatePresentation authored && TryResolveEnemyStem(current, out var stem))
			{
				bool loop = current == EnemyState.Patrol || current == EnemyState.Chase || current == EnemyState.Return;
				authored.SetAuthoredState(stem, 0f, loop, restart: true);
				return;
			}
			if (1 == 0)
			{
			}
			ActorVisualState actorVisualState = current switch
			{
				EnemyState.Patrol => ActorVisualState.Move, 
				EnemyState.Chase => ActorVisualState.Move, 
				EnemyState.Return => ActorVisualState.Move, 
				EnemyState.Telegraph => ActorVisualState.Ability1, 
				EnemyState.ExecuteAttack => ActorVisualState.Ability1, 
				EnemyState.Staggered => ActorVisualState.Stagger, 
				EnemyState.Dead => ActorVisualState.Death, 
				_ => ActorVisualState.Idle, 
			};
			if (1 == 0)
			{
			}
			ActorVisualState state = actorVisualState;
			_presentation.SetVisualState(state);
		}

		private bool TryResolveEnemyStem(EnemyState state, out string stem)
		{
			if (1 == 0)
			{
			}
			string text = state switch
			{
				EnemyState.Telegraph => enemyTelegraphStem, 
				EnemyState.ExecuteAttack => enemyAttackStem, 
				EnemyState.Staggered => enemyStaggerStem, 
				EnemyState.Return => bossReturnStem, 
				_ => string.Empty, 
			};
			if (1 == 0)
			{
			}
			stem = text;
			return !string.IsNullOrWhiteSpace(stem);
		}

		private void OnBossAttackSelected(GaronAttackType attack)
		{
			if (_presentation is IAuthoredStatePresentation authored)
			{
				if (1 == 0)
				{
				}
				string bossAttackStem = attack switch
				{
					GaronAttackType.Front => bossFrontStem,
					GaronAttackType.Charge => bossChargeStem,
					GaronAttackType.Spin => bossSpinStem,
					_ => bossFrontStem,
				};
				if (1 == 0)
				{
				}
				_bossAttackStem = bossAttackStem;
				if (!string.IsNullOrWhiteSpace(_bossAttackStem))
				{
					_forcedStateUntil = 0f;
					authored.SetAuthoredState(_bossAttackStem, 0f, loop: false, restart: true);
					ResolveVfx()?.PlayAbility((int)attack);
				}
			}
		}

		private void OnDamageResolved(DamageInfo damage, float healthDamage, float staggerDamage)
		{
			if (_enemy != null && _enemy.EnemyState != EnemyState.Staggered)
			{
				PlayHit();
			}
		}

		private void PlayTemporary(ActorVisualState state, float duration)
		{
			if (_presentation != null)
			{
				_forcedState = state;
				_forcedStateDuration = Mathf.Max(0.001f, duration);
				_forcedStateUntil = Time.time + duration;
				_presentation.SetVisualState(state);
			}
		}

		private void ResolvePresentation()
		{
			_presentation = (presentationBehaviour as IActorPresentation) ?? ActorPresentationLocator.Find(base.gameObject);
		}

		private ActorSpriteVfx ResolveVfx()
		{
			if (_vfx == null)
			{
				_vfx = GetComponent<ActorSpriteVfx>();
			}
			return _vfx;
		}

		private void Unbind()
		{
			if (_enemy != null)
			{
				_enemy.EnemyStateChanged -= OnEnemyStateChanged;
				_enemy.DamageResolved -= OnDamageResolved;
			}
			if (_boss != null)
			{
				_boss.AttackSelected -= OnBossAttackSelected;
			}
			_boss = null;
			_bossAttackStem = string.Empty;
			_enemy = null;
		}

		private void OnDisable()
		{
			Unbind();
		}
	}
}
