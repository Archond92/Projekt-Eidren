using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Player;
using System;
using UnityEngine;

namespace Eidren.Interaction
{
	public sealed class InteractionController : MonoBehaviour, ITransitionCancellable
	{
		[SerializeField]
		[Min(0.02f)]
		private float scanInterval = 0.08f;

		[SerializeField]
		[Min(0.5f)]
		private float detectionRadius = 6f;

		[SerializeField]
		[Min(0f)]
		private float directionHysteresis = 0.12f;

		// F32-001: Mit den kleineren Rang- und Blickboni liegen die wirksamen
		// Entfernungen dichter beieinander; 0,3 haette den Nachbarn bei
		// schraeger Blickrichtung weiter festgehalten.
		[SerializeField]
		[Min(0f)]
		private float distanceHysteresis = 0.2f;

		[SerializeField]
		private LayerMask interactionLayers = -1;

		private readonly Collider[] _overlapBuffer = new Collider[32];

		private readonly IInteractable[] _candidateBuffer = new IInteractable[32];

		private readonly InteractionSession _session = new InteractionSession();

		private PlayerInputReader _input;

		private PlayerMotor _motor;

		private PlayerCombatController _combat;

		private Damageable _health;

		private SceneFlowService _sceneFlow;

		private PlayerInventory _inventory;

		private IInteractable _selectedTarget;

		private ResourceTargetIndicator _targetIndicator;

		private bool _selectedCanInteract;

		private string _blockedReason = string.Empty;

		private bool _requiresRelease;

		private bool _resumeHeldInteraction;

		private float _nextScanTime;

		private bool _initialized;

		public IInteractable CurrentTarget => _selectedTarget;

		public InteractionState State
		{
			get
			{
				if (_session.IsActive || _session.State == InteractionState.Cancelled || _session.State == InteractionState.Completed)
				{
					return _session.State;
				}
				if (_selectedTarget == null)
				{
					return InteractionState.None;
				}
				return _selectedCanInteract ? InteractionState.Available : InteractionState.Blocked;
			}
		}

		public float Progress => _session.Progress;

		public InteractionCancelReason LastCancelReason => _session.LastCancelReason;

		public bool IsInteracting => _session.IsActive;

		public event Action<InteractionSnapshot> SnapshotChanged;

		public void Initialize(PlayerInputReader input, PlayerMotor motor, PlayerCombatController combat, Damageable health, SceneFlowService sceneFlow, PlayerInventory inventory)
		{
			Unbind();
			_input = input ?? throw new ArgumentNullException("input");
			_motor = motor ?? throw new ArgumentNullException("motor");
			_combat = combat ?? throw new ArgumentNullException("combat");
			_health = health ?? throw new ArgumentNullException("health");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_inventory = inventory ?? throw new ArgumentNullException("inventory");
			_requiresRelease = _input.InteractHeld;
			_resumeHeldInteraction = false;
			_targetIndicator = GetComponent<ResourceTargetIndicator>() ?? base.gameObject.AddComponent<ResourceTargetIndicator>();
			_input.InteractPressed += HandleInteractPressed;
			_input.InteractReleased += HandleInteractReleased;
			_input.GameplayEnabledChanged += HandleGameplayEnabledChanged;
			_motor.DodgeStarted += HandleDodgeStarted;
			_combat.AttackStarted += HandleAttackStarted;
			_health.Damaged += HandleDamaged;
			_health.Died += HandleDied;
			_sceneFlow.SceneTransitionStarted += HandleSceneTransitionStarted;
			_initialized = true;
			_nextScanTime = 0f;
			ScanForTargets();
		}

		public void RefreshTargetsNow()
		{
			if (_initialized)
			{
				ScanForTargets();
			}
		}

		public void CancelForSceneTransition()
		{
			CancelCurrent(InteractionCancelReason.SceneTransition);
			BlockUntilRelease();
		}

		// Sperrt bis zum Loslassen und verwirft ein vorgemerktes Fortsetzen. Beide
		// gehoeren immer zusammen: Was ein erneutes Druecken verlangt, darf erst
		// recht nicht von selbst weiterlaufen.
		private void BlockUntilRelease()
		{
			_requiresRelease = true;
			_resumeHeldInteraction = false;
		}

		private void Update()
		{
			if (!_initialized)
			{
				return;
			}
			if (_requiresRelease && !_input.InteractHeld)
			{
				_requiresRelease = false;
			}
			if (Time.unscaledTime >= _nextScanTime)
			{
				ScanForTargets();
				_nextScanTime = Time.unscaledTime + Mathf.Max(0.02f, scanInterval);
			}
			if (!_session.IsActive)
			{
				return;
			}
			if (_session.Target != _selectedTarget)
			{
				CancelCurrent(InteractionCancelReason.TargetChanged);
				return;
			}
			InteractionContext context = CreateContext();
			_session.Tick(Time.deltaTime, _input.InteractHeld, in context);
			if (!_session.IsActive && _session.State == InteractionState.Cancelled)
			{
				// Bricht die Sitzung selbst ab (losgelassen, ausser Reichweite,
				// Ziel weg), ist erneutes Druecken richtig.
				BlockUntilRelease();
			}
			PublishSnapshot();
		}

		private void ScanForTargets()
		{
			InteractionContext context = CreateContext();
			int num = Physics.OverlapSphereNonAlloc(base.transform.position, detectionRadius, _overlapBuffer, interactionLayers, QueryTriggerInteraction.Collide);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				Collider collider = _overlapBuffer[i];
				if (!(collider == null))
				{
					IInteractable componentInParent = collider.GetComponentInParent<IInteractable>();
					if (InteractionUtility.IsActive(componentInParent) && !ContainsCandidate(componentInParent, num2) && num2 < _candidateBuffer.Length)
					{
						_candidateBuffer[num2++] = componentInParent;
					}
				}
			}
			IInteractable selectedTarget = _selectedTarget;
			_selectedTarget = InteractionTargetSelector.SelectBest(_candidateBuffer, num2, in context, _selectedTarget, directionHysteresis, distanceHysteresis, out _selectedCanInteract, out _blockedReason);
			_targetIndicator?.SetTarget(_selectedTarget);
			for (int j = 0; j < num2; j++)
			{
				_candidateBuffer[j] = null;
			}
			for (int k = 0; k < num; k++)
			{
				_overlapBuffer[k] = null;
			}
			if (_session.IsActive && selectedTarget != _selectedTarget)
			{
				CancelCurrent(InteractionCancelReason.TargetChanged);
			}
			TryResumeHeldInteraction();
			PublishSnapshot();
		}

		// Setzt nach einem Treffer fort, solange die Taste gehalten bleibt. Bewusst
		// am Ziel-Abtasten aufgehaengt: So ist das Ziel frisch geprueft — Reichweite,
		// Verfuegbarkeit, Rang —, bevor erneut angesetzt wird. Ein Rueckstoss kann
		// den Spieler schliesslich aus der Reichweite getragen haben.
		private void TryResumeHeldInteraction()
		{
			if (!_resumeHeldInteraction)
			{
				return;
			}
			if (!_input.InteractHeld || !_input.GameplayEnabled || !_health.IsAlive)
			{
				_resumeHeldInteraction = false;
				return;
			}
			if (_session.IsActive || _combat.IsAttacking || _motor.IsDodging || _selectedTarget == null || !_selectedCanInteract)
			{
				return;
			}
			_resumeHeldInteraction = false;
			_session.Begin(_selectedTarget, CreateContext());
		}

		private bool ContainsCandidate(IInteractable candidate, int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (_candidateBuffer[i] == candidate)
				{
					return true;
				}
			}
			return false;
		}

		private void HandleInteractPressed()
		{
			if (_initialized && !_requiresRelease && _input.GameplayEnabled && !_combat.IsAttacking && !_motor.IsDodging && !_session.IsActive)
			{
				ScanForTargets();
				if (_selectedTarget == null || !_selectedCanInteract)
				{
					PublishSnapshot();
					return;
				}
				_session.Begin(_selectedTarget, CreateContext());
				PublishSnapshot();
			}
		}

		private void HandleInteractReleased()
		{
			if (_session.IsActive && _session.Target.Mode == InteractionMode.Hold)
			{
				CancelCurrent(InteractionCancelReason.InputReleased);
			}
			_requiresRelease = false;
			_resumeHeldInteraction = false;
			if (_session.State == InteractionState.Cancelled || _session.State == InteractionState.Completed)
			{
				_session.ResetToIdle();
			}
			PublishSnapshot();
		}

		private void HandleGameplayEnabledChanged(bool enabled)
		{
			if (!enabled)
			{
				CancelCurrent(InteractionCancelReason.InputDisabled);
				BlockUntilRelease();
			}
		}

		// Angriff und Ausweichen sind eigene Entscheidungen des Spielers, kein
		// Zwischenfall — hier bleibt es beim erneuten Druecken.
		private void HandleAttackStarted(float duration, int comboIndex, WeaponFamily family)
		{
			CancelCurrent(InteractionCancelReason.AttackStarted);
			BlockUntilRelease();
		}

		private void HandleDodgeStarted()
		{
			CancelCurrent(InteractionCancelReason.DodgeStarted);
			BlockUntilRelease();
		}

		// Ein Treffer unterbricht den laufenden Vorgang, nimmt dem Spieler aber
		// nicht die Absicht: Wer die Taste gedrueckt haelt, setzt danach von selbst
		// fort, statt loslassen und neu druecken zu muessen. Der Fortschritt
		// beginnt dabei neu — abgebrochen ist abgebrochen.
		private void HandleDamaged(DamageInfo damage)
		{
			bool flag = _session.IsActive && _input.InteractHeld;
			CancelCurrent(InteractionCancelReason.PlayerDamaged);
			if (flag)
			{
				_resumeHeldInteraction = true;
			}
			else
			{
				BlockUntilRelease();
			}
		}

		private void HandleDied()
		{
			CancelCurrent(InteractionCancelReason.PlayerDied);
			_session.UpdateCancelledReason(InteractionCancelReason.PlayerDied);
			// Raeumt auch das Fortsetzen ab, das HandleDamaged eine Zeile zuvor
			// gesetzt haben kann: Damageable meldet bei einem toedlichen Treffer
			// erst Damaged, dann Died.
			BlockUntilRelease();
			PublishSnapshot();
		}

		private void HandleSceneTransitionStarted(string sceneName)
		{
			CancelForSceneTransition();
		}

		private void CancelCurrent(InteractionCancelReason reason)
		{
			if (_session.IsActive)
			{
				_session.Cancel(CreateContext(), reason);
				PublishSnapshot();
			}
		}

		private InteractionContext CreateContext()
		{
			Vector3 facingDirection = ((_motor != null && _motor.WorldMoveDirection.sqrMagnitude > 0.0025f) ? _motor.WorldMoveDirection : base.transform.forward);
			return new InteractionContext(base.gameObject, base.transform, facingDirection, _inventory);
		}

		private void PublishSnapshot()
		{
			IInteractable target = (_session.IsActive ? _session.Target : _selectedTarget);
			bool canInteract = _session.IsActive || _selectedCanInteract;
			this.SnapshotChanged?.Invoke(new InteractionSnapshot(State, target, canInteract, _session.Progress, _blockedReason));
		}

		private void Unbind()
		{
			if (_input != null)
			{
				_input.InteractPressed -= HandleInteractPressed;
				_input.InteractReleased -= HandleInteractReleased;
				_input.GameplayEnabledChanged -= HandleGameplayEnabledChanged;
			}
			if (_motor != null)
			{
				_motor.DodgeStarted -= HandleDodgeStarted;
			}
			if (_combat != null)
			{
				_combat.AttackStarted -= HandleAttackStarted;
			}
			if (_health != null)
			{
				_health.Damaged -= HandleDamaged;
				_health.Died -= HandleDied;
			}
			if (_sceneFlow != null)
			{
				_sceneFlow.SceneTransitionStarted -= HandleSceneTransitionStarted;
			}
		}

		private void OnDisable()
		{
			CancelCurrent(InteractionCancelReason.ControllerDisabled);
			_targetIndicator?.SetTarget(null);
		}

		private void OnDestroy()
		{
			CancelCurrent(InteractionCancelReason.ControllerDisabled);
			Unbind();
		}
	}
}
