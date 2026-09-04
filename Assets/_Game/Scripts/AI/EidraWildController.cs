using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using System.Collections;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.AI
{
	public sealed class EidraWildController : EnemyControllerBase
	{
		[SerializeField]
		private EnemyDefinition definition;

		[SerializeField]
		private EnemyMeleeHitbox attackHitbox;

		[SerializeField]
		private GameObject attackTelegraph;

		[SerializeField]
		private Renderer visualRenderer;

		[SerializeField]
		private Transform visualRoot;

		private string _instanceId = string.Empty;

		private bool _eidraInitialized;

		private bool _departed;

		private bool _captureChannelActive;

		private Vector3 _telegraphRestScale = Vector3.one;

		private SpriteActorAnimator _spriteAnimator;

		public EnemyDefinition Definition => definition;

		public EidraData Eidra => (definition != null) ? definition.CapturableEidra : null;

		public string InstanceId => _instanceId;

		public float HealthFraction => (base.MaxHealth > 0f) ? Mathf.Clamp01(base.CurrentHealth / base.MaxHealth) : 0f;

		public bool IsNeutral => !base.CombatAuthorized;

		public bool HasDeparted => _departed;

		public bool IsCaptureChannelActive => _captureChannelActive;

		public event Action<EidraDepartureReason> Departed;

		public void SetCaptureChannelActive(bool active)
		{
			if (!_departed && _captureChannelActive != active)
			{
				_captureChannelActive = active;
				if (active)
				{
					OnAttackInterrupted();
				}
				SetInteractionPaused(active);
			}
		}

		public void ConfigureInstance(string instanceId, EnemyDefinition enemyDefinition)
		{
			_instanceId = instanceId ?? string.Empty;
			if (enemyDefinition != null)
			{
				definition = enemyDefinition;
			}
		}

		public void Initialize(Transform player, Damageable playerHealth)
		{
			if (!_eidraInitialized)
			{
				if (definition == null || !definition.IsCapturableEidra || attackHitbox == null || attackTelegraph == null)
				{
					throw new InvalidOperationException("Wild eidra '" + base.name + "' is missing prefab references or capture data.");
				}
				_telegraphRestScale = attackTelegraph.transform.localScale;
				attackTelegraph.SetActive(value: false);
				if (!EnsureVisualVariant() && visualRenderer != null)
				{
					visualRenderer.material.color = Eidra.UiAccent;
				}
				_eidraInitialized = true;
				InitializeEnemy(definition.Id, definition.MaximumHealth, definition.MaximumStagger, definition.StaggerDuration, player, playerHealth, definition.Navigation);
			}
		}

		private bool EnsureVisualVariant()
		{
			if (Eidra == null)
			{
				return false;
			}
			if (visualRoot == null)
			{
				visualRoot = ((visualRenderer != null) ? visualRenderer.transform : base.transform.Find("Visual"));
			}
			if (visualRoot == null)
			{
				return false;
			}
			string id = Eidra.Id;
			if (1 == 0)
			{
			}
			// Seit dem Kreaturen-Einbau die 3D-Wrapper (EidraVisual3DBuilder):
			// gleicher Aufbau wie die 2D-Prefabs — Animator, VFX, Anker,
			// Bodenschatten — nur das Aktorbild ist die CreatureMeshPresentation.
			string text = id switch
			{
				"terrock" => "Prefabs/Actors/3D/Terrock_3D",
				"noctarion" => "Prefabs/Actors/3D/Noctarion_3D",
				"ignivar" => "Prefabs/Actors/3D/Ignivar_3D",
				_ => "Prefabs/Visuals/Eidra/" + Eidra.Id,
			};
			if (1 == 0)
			{
			}
			string text2 = text;
			GameObject gameObject = Resources.Load<GameObject>(text2);
			if (gameObject == null)
			{
				Debug.LogError("Canonical wild Eidra visual is missing: " + text2, this);
				return false;
			}
			for (int num = visualRoot.childCount - 1; num >= 0; num--)
			{
				UnityEngine.Object.Destroy(visualRoot.GetChild(num).gameObject);
			}
			if (visualRenderer != null && visualRenderer.transform == visualRoot)
			{
				visualRenderer.enabled = false;
			}
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, visualRoot);
			gameObject2.name = gameObject.name;
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			gameObject2.transform.localScale = Vector3.one;
			ActorPresentationLocator.Find(gameObject2)?.SetTint(Color.Lerp(Color.white, Eidra.UiAccent, 0.18f));
			_spriteAnimator = ActorAnimationDriverLocator.Find(gameObject2) as SpriteActorAnimator;
			if (_spriteAnimator != null)
			{
				_spriteAnimator.ConfigureEnemyStateStems("telegraph", "wildattack", "stagger", "flee");
				_spriteAnimator.Bind(this);
			}
			return true;
		}

		public bool TryTakeForCapture()
		{
			if (_departed)
			{
				return false;
			}
			Depart(EidraDepartureReason.Captured);
			return true;
		}

		protected override float ModifyHealthDamage(DamageInfo damage)
		{
			// F32-003: Erst das Rueckenmal (Basis), dann die Fangklemmung —
			// sonst waere das wilde Eidra der einzige Gegner, gegen den die
			// Markierung nichts bewirkt.
			float marked = base.ModifyHealthDamage(damage);
			EidraData eidra = Eidra;
			return (eidra == null) ? marked : CaptureResolution.ClampDamageAboveFlee(eidra.Capture, base.MaxHealth, base.CurrentHealth, marked);
		}

		protected override void OnDamageResolved(DamageInfo originalDamage, float appliedHealthDamage, float appliedStagger)
		{
			if (IsNeutral)
			{
				ActivateCombat();
			}
			if (!_departed && base.CurrentHealth <= FleeHealthFloor())
			{
				Depart(EidraDepartureReason.Fled);
			}
		}

		protected override IEnumerator ExecuteAttack()
		{
			while (_captureChannelActive && base.IsAlive && !_departed)
			{
				yield return null;
			}
			if (!base.IsAlive || _departed)
			{
				yield break;
			}
			SetEnemyState(EnemyState.Telegraph);
			Vector3 direction = FlatDirectionToTarget();
			if (direction.sqrMagnitude > 0.001f)
			{
				base.transform.rotation = Quaternion.LookRotation(direction);
			}
			attackTelegraph.SetActive(value: true);
			float elapsed = 0f;
			while (elapsed < definition.TelegraphDuration)
			{
				if (ShouldAbortAttack())
				{
					OnAttackInterrupted();
					yield break;
				}
				elapsed += Time.deltaTime;
				attackTelegraph.transform.localScale = _telegraphRestScale * (0.82f + Mathf.PingPong(elapsed * 2.8f, 0.28f));
				yield return null;
			}
			attackTelegraph.SetActive(value: false);
			attackTelegraph.transform.localScale = _telegraphRestScale;
			SetEnemyState(EnemyState.ExecuteAttack);
			attackHitbox.BeginWindow();
			elapsed = 0f;
			while (elapsed < definition.AttackWindowDuration)
			{
				if (ShouldAbortAttack())
				{
					attackHitbox.EndWindow();
					OnAttackInterrupted();
					yield break;
				}
				attackHitbox.Evaluate(base.transform, base.TrackedTargetHealth, DamageTarget);
				elapsed += Time.deltaTime;
				yield return null;
			}
			attackHitbox.EndWindow();
			SetEnemyState(EnemyState.Recover);
			elapsed = 0f;
			while (elapsed < definition.RecoveryDuration && !ShouldAbortAttack())
			{
				elapsed += Time.deltaTime;
				yield return null;
			}
		}

		protected override void OnAttackInterrupted()
		{
			attackHitbox?.EndWindow();
			if (!(attackTelegraph == null))
			{
				attackTelegraph.SetActive(value: false);
				attackTelegraph.transform.localScale = _telegraphRestScale;
			}
		}

		private void DamageTarget(CombatTarget target)
		{
			if (base.IsAlive && !_departed && !_captureChannelActive && base.EnemyState == EnemyState.ExecuteAttack && target.Damageable != null && target.IsAlive)
			{
				target.Damageable.ApplyDamage(new DamageInfo(definition.AttackDamage, 0f, target.Transform.position + Vector3.up, base.gameObject, isBackAttack: false, definition.Id + ".melee", definition.Id));
			}
		}

		private bool ShouldAbortAttack()
		{
			return !base.IsAlive || _departed || _captureChannelActive || base.TrackedTarget == null || base.TrackedTargetHealth == null || !base.TrackedTargetHealth.IsAlive || ShouldReturnToHome();
		}

		private float FleeHealthFloor()
		{
			EidraData eidra = Eidra;
			return (eidra == null) ? 0f : CaptureResolution.FleeHealthFloor(eidra.Capture, base.MaxHealth);
		}

		private void Depart(EidraDepartureReason reason)
		{
			_departed = true;
			StopAllCoroutines();
			attackHitbox?.EndWindow();
			_spriteAnimator?.PlayFlee();
			this.Departed?.Invoke(reason);
			if (_spriteAnimator != null && base.gameObject.activeInHierarchy)
			{
				StartCoroutine(HideAfterFlee());
			}
			else
			{
				base.gameObject.SetActive(value: false);
			}
		}

		private IEnumerator HideAfterFlee()
		{
			yield return new WaitForSeconds(0.28f);
			base.gameObject.SetActive(value: false);
		}
	}
}
