using Eidren.Combat;
using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using System.Collections;
using System;
using UnityEngine.AI;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace Eidren.AI
{
	public sealed partial class BossController : EnemyControllerBase
	{
		private const float HitFlashDuration = 0.09f;

		private readonly RaycastHit[] _chargePathHits = new RaycastHit[16];

		private BossData _data;

		private Damageable _playerHealth;

		private Renderer[] _renderers;

		private Material _telegraphMaterial;

		private float _staggerResistanceUntil;

		private SpriteActorAnimator _spriteAnimator;

		private IActorPresentation _actorPresentation;

		private GameObject _activeTelegraph;

		private readonly GameObject[] _telegraphPool = new GameObject[3];

		private Color _baseColor;

		private bool _attackDamageActive;

		private GaronAttackType? _previousAttack;

		private int _consecutiveAttackUses;

		public bool BattleActive { get; private set; }

		public bool HasActiveTelegraph => _activeTelegraph != null;

		public bool HasActiveAttackDamage => _attackDamageActive;

		// F32-003: Rueckenmal, Aura-Masse und Schadensregel liegen jetzt an
		// EnemyControllerBase — jeder Gegner ist markierbar, nicht nur der
		// Boss. Garon behaelt hier nur seine groesseren Aura-Masse.
		protected override float BackMarkAuraRadius => 2.35f;

		protected override float BackMarkLabelHeight => 4.7f;

		public event Action<bool> BattleActivityChanged;

		public event Action<GaronAttackType> AttackSelected;

		public void Initialize(BossData data, Transform player, Damageable playerHealth, Material telegraphMaterial)
		{
			_data = data;
			_playerHealth = playerHealth;
			_telegraphMaterial = telegraphMaterial;
			_renderers = GetComponentsInChildren<Renderer>();
			if (_renderers.Length != 0)
			{
				_baseColor = GetRendererColor(_renderers[0]);
			}
			_actorPresentation = ActorPresentationLocator.Find(base.gameObject);
			_spriteAnimator = GetComponentInChildren<SpriteActorAnimator>(includeInactive: true);
			if (_spriteAnimator != null)
			{
				_spriteAnimator.Bind(this);
			}
			InitializeEnemy(_data.Id, _data.MaxHealth, _data.MaxStagger, _data.StaggerDuration, player, playerHealth, _data.Navigation);
		}

		public void BeginBattle()
		{
			if (!BattleActive && base.IsAlive)
			{
				SetBattleActive(active: true);
				ActivateCombat();
			}
		}

		protected override bool CanReceiveDamage(DamageInfo damage)
		{
			return BattleActive && base.CanReceiveDamage(damage);
		}

		protected override float ModifyStaggerDamage(DamageInfo damage)
		{
			float num = ((Time.time < _staggerResistanceUntil) ? _data.PostStaggerResistanceMultiplier : 1f);
			return damage.StaggerDamage * num;
		}

		protected override void OnDamageResolved(DamageInfo originalDamage, float appliedHealthDamage, float appliedStaggerDamage)
		{
			StartCoroutine(HitFlash(originalDamage.IsBackAttack ? new Color(1f, 0.3f, 0.8f) : Color.white));
		}

		// Garon ist deutlich groesser als die uebrigen Gegner.
		protected override float DamageFeedbackHeight => 4.25f;

		protected override IEnumerator ExecuteAttack()
		{
			GaronAttackSetData attacks = _data.Attacks;
			float elapsed = 0f;
			while (elapsed < Mathf.Max(0f, attacks.ChoiceDuration))
			{
				if (ShouldAbortAttack())
				{
					yield break;
				}
				elapsed += Time.deltaTime;
				yield return null;
			}
			float distance = FlatDistanceToTarget();
			bool chargePathClear = IsChargePathClear(attacks.Charge.PathRadius);
			if (!GaronAttackSelection.TryChoose(attacks, distance, chargePathClear, _previousAttack, _consecutiveAttackUses, UnityEngine.Random.Range(0, 2147483647), out var selected))
			{
				_consecutiveAttackUses = 0;
				SetEnemyState(EnemyState.Chase);
				yield return WaitWhileAttackValid(attacks.FailedChoiceDelay);
				yield break;
			}
			if (_previousAttack == selected)
			{
				_consecutiveAttackUses++;
			}
			else
			{
				_consecutiveAttackUses = 1;
			}
			_previousAttack = selected;
			GaronAttackData attack = GetAttackData(selected);
			this.AttackSelected?.Invoke(selected);
			yield return RunAttack(selected, attack);
			if (!ShouldAbortAttack() && base.EnemyState != EnemyState.Staggered && base.EnemyState != EnemyState.Dead)
			{
				SetEnemyState(EnemyState.Recover);
				yield return WaitWhileAttackValid(attack.RecoveryDuration);
			}
		}

		private IEnumerator RunAttack(GaronAttackType attackType, GaronAttackData attack)
		{
			SetEnemyState(EnemyState.Telegraph);
			Vector3 lockedDirection = FlatDirectionToTarget();
			if (lockedDirection.sqrMagnitude > 0f)
			{
				base.transform.rotation = Quaternion.LookRotation(lockedDirection.normalized);
			}
			_activeTelegraph = CreateTelegraph(attackType, attack);
			float elapsed = 0f;
			while (elapsed < Mathf.Max(0f, attack.TelegraphDuration))
			{
				if (ShouldAbortAttack())
				{
					OnAttackInterrupted();
					yield break;
				}
				elapsed += Time.deltaTime;
				float pulse = 0.65f + Mathf.PingPong(elapsed * 2.6f, 0.35f);
				if (_activeTelegraph != null)
				{
					_activeTelegraph.transform.localScale = attack.TelegraphScale * pulse;
				}
				yield return null;
			}
			DestroyActiveTelegraph();
			SetEnemyState(EnemyState.ExecuteAttack);
			_attackDamageActive = true;
			if (attackType == GaronAttackType.Charge)
			{
				PauseNavigationForSpecialMovement();
			}
			elapsed = 0f;
			bool hit = false;
			while (elapsed < Mathf.Max(0f, attack.ExecuteDuration))
			{
				if (ShouldAbortAttack())
				{
					OnAttackInterrupted();
					yield break;
				}
				elapsed += Time.deltaTime;
				if (attackType == GaronAttackType.Charge)
				{
					MoveDuringSpecialAttack(lockedDirection * (attack.MoveSpeed * Time.deltaTime));
					if (!hit && FlatDistanceToTarget() < attack.HitDistance)
					{
						hit = true;
						DamagePlayer(attack.Damage, "garon.charge");
					}
				}
				else if (!hit)
				{
					hit = true;
					bool inRange = FlatDistanceToTarget() < attack.HitDistance;
					bool inArc = attackType == GaronAttackType.Spin || Vector3.Dot(base.transform.forward, FlatDirectionToTarget()) > 0.1f;
					if (inRange && inArc)
					{
						DamagePlayer(attack.Damage, (attackType == GaronAttackType.Front) ? "garon.front" : "garon.spin");
					}
				}
				yield return null;
			}
			_attackDamageActive = false;
			ResumeNavigationAfterSpecialMovement();
		}

		private IEnumerator WaitWhileAttackValid(float duration)
		{
			float elapsed = 0f;
			while (elapsed < Mathf.Max(0f, duration) && !ShouldAbortAttack())
			{
				elapsed += Time.deltaTime;
				yield return null;
			}
		}

		protected override void OnAttackInterrupted()
		{
			_attackDamageActive = false;
			DestroyActiveTelegraph();
			ResumeNavigationAfterSpecialMovement();
			RestoreRendererColors();
		}

		protected override void OnStaggerStarted()
		{
		}

		protected override void OnStaggerCompleted()
		{
			_staggerResistanceUntil = Time.time + _data.PostStaggerResistanceDuration;
		}

		protected override void OnReturnedHome()
		{
			base.OnReturnedHome();
			SetBattleActive(active: false);
			ClearBackMark();
			_staggerResistanceUntil = 0f;
			_previousAttack = null;
			_consecutiveAttackUses = 0;
		}

		protected override void OnEnemyDied()
		{
			SetBattleActive(active: false);
			ClearBackMark();
			RestoreRendererColors();
		}

		protected override void OnEnemyStateEntered(EnemyState state)
		{
			if (state == EnemyState.Alert && base.CombatAuthorized && !BattleActive)
			{
				SetBattleActive(active: true);
			}
		}

		private void SetBattleActive(bool active)
		{
			if (BattleActive != active)
			{
				BattleActive = active;
				this.BattleActivityChanged?.Invoke(active);
			}
		}

		private bool ShouldAbortAttack()
		{
			return !base.IsAlive || base.TrackedTarget == null || base.TrackedTargetHealth == null || !base.TrackedTargetHealth.IsAlive || ShouldReturnToHome();
		}

		private GaronAttackData GetAttackData(GaronAttackType attack)
		{
			if (1 == 0)
			{
			}
			GaronAttackData result = attack switch
			{
				GaronAttackType.Front => _data.Attacks.Front, 
				GaronAttackType.Charge => _data.Attacks.Charge, 
				_ => _data.Attacks.Spin, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private bool IsChargePathClear(float radius)
		{
			if (base.TrackedTarget == null)
			{
				return false;
			}
			Vector3 position = base.transform.position;
			Vector3 position2 = base.TrackedTarget.position;
			Vector3 direction = position2 - position;
			direction.y = 0f;
			float magnitude = direction.magnitude;
			if (magnitude < 0.1f)
			{
				return false;
			}
			direction /= magnitude;
			NavMeshAgent navigationAgent = base.NavigationAgent;
			if (navigationAgent != null && navigationAgent.enabled && navigationAgent.isOnNavMesh && NavMesh.Raycast(position, position2, out var _, -1))
			{
				return false;
			}
			float b = ((navigationAgent != null) ? (navigationAgent.height * 0.5f) : 1f);
			Vector3 origin = position + Vector3.up * Mathf.Max(radius + 0.1f, b);
			int num = Physics.SphereCastNonAlloc(origin, Mathf.Max(0.05f, radius), direction, _chargePathHits, Mathf.Max(0f, magnitude - 0.5f), -1, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < num; i++)
			{
				Transform transform = _chargePathHits[i].collider.transform;
				if (!transform.IsChildOf(base.transform) && !transform.IsChildOf(base.TrackedTarget))
				{
					return false;
				}
			}
			return true;
		}

		private GameObject CreateTelegraph(GaronAttackType attackType, GaronAttackData attack)
		{
			GameObject gameObject = _telegraphPool[(int)attackType];
			if (gameObject == null)
			{
				gameObject = GameObject.CreatePrimitive((attackType == GaronAttackType.Spin) ? PrimitiveType.Cylinder : PrimitiveType.Cube);
				GameObject gameObject2 = gameObject;
				if (1 == 0)
				{
				}
				string text = attackType switch
				{
					GaronAttackType.Front => "Telegraph_Front", 
					GaronAttackType.Charge => "Telegraph_Charge", 
					_ => "Telegraph_Spin", 
				};
				if (1 == 0)
				{
				}
				gameObject2.name = text;
				gameObject.transform.SetParent(base.transform, worldPositionStays: true);
				Collider component = gameObject.GetComponent<Collider>();
				if (component != null)
				{
					component.enabled = false;
					UnityEngine.Object.Destroy(component);
				}
				if (_telegraphMaterial != null)
				{
					gameObject.GetComponent<Renderer>().sharedMaterial = _telegraphMaterial;
				}
				_telegraphPool[(int)attackType] = gameObject;
			}
			gameObject.SetActive(value: true);
			gameObject.transform.position = base.transform.position + base.transform.forward * attack.TelegraphForwardOffset + Vector3.up * 0.05f;
			gameObject.transform.rotation = Quaternion.LookRotation(base.transform.forward);
			gameObject.transform.localScale = attack.TelegraphScale;
			return gameObject;
		}

		private IEnumerator HitFlash(Color color)
		{
			if (_actorPresentation != null)
			{
				_actorPresentation.SetTint((color == Color.white) ? new Color(1.45f, 1.25f, 1.05f, 1f) : color);
				yield return new WaitForSeconds(0.09f);
				_actorPresentation.SetTint(Color.white);
				yield break;
			}
			Renderer[] renderers = _renderers;
			foreach (Renderer renderer in renderers)
			{
				SetRendererColor(renderer, color);
			}
			yield return new WaitForSeconds(0.09f);
			RestoreRendererColors();
		}

		private void RestoreRendererColors()
		{
			if (_actorPresentation != null)
			{
				_actorPresentation.SetTint(Color.white);
			}
			else
			{
				if (_renderers == null)
				{
					return;
				}
				Renderer[] renderers = _renderers;
				foreach (Renderer renderer in renderers)
				{
					if (renderer != null)
					{
						SetRendererColor(renderer, _baseColor);
					}
				}
			}
		}

		private static Color GetRendererColor(Renderer renderer)
		{
			Material material = (Application.isPlaying ? renderer.material : renderer.sharedMaterial);
			return (material != null) ? material.color : Color.white;
		}

		private static void SetRendererColor(Renderer renderer, Color color)
		{
			Material material = (Application.isPlaying ? renderer.material : renderer.sharedMaterial);
			if (material != null)
			{
				material.color = color;
			}
		}

		private void DamagePlayer(float amount, string attackId)
		{
			if (base.IsAlive && _attackDamageActive && base.EnemyState == EnemyState.ExecuteAttack && !(_playerHealth == null) && _playerHealth.IsAlive)
			{
				float currentHealth = _playerHealth.CurrentHealth;
				_playerHealth.ApplyDamage(new DamageInfo(amount, 0f, base.TrackedTarget.position, base.gameObject, isBackAttack: false, attackId, _data.Id));
				float num = currentHealth - _playerHealth.CurrentHealth;
				if (num > 0f)
				{
					CombatFeedback.SpawnPlayerDamage(base.TrackedTarget.position + Vector3.up * 3.1f, num);
				}
				else
				{
					CombatFeedback.SpawnStatusText(base.TrackedTarget.position + Vector3.up * 3.1f, "GEBLOCKT", new Color(0.35f, 0.95f, 0.68f));
				}
			}
		}

		private void DestroyActiveTelegraph()
		{
			if (!(_activeTelegraph == null))
			{
				_activeTelegraph.SetActive(value: false);
				_activeTelegraph = null;
			}
		}

	}
}
