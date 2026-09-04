using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Input;
using System.Collections;
using System;
using UnityEngine;

namespace Eidren.Player
{
	[RequireComponent(typeof(CharacterController))]
	public sealed class PlayerMotor : MonoBehaviour, ITransitionCancellable
	{
		private PlayerInputReader _input;

		private CharacterController _controller;

		private Damageable _damageable;

		private Camera _camera;

		private float _moveSpeed;

		private float _rotationSpeed;

		private PlayerMovementBoundary _movementBoundary;

		private bool _canMove = true;

		private bool _locomotionLocked;

		private Coroutine _dodgeRoutine;

		private Vector3 _lastFootstepPosition;

		private float _footstepDistance;

		private readonly Collider[] _teleportOverlaps = new Collider[16];

		public float CurrentStamina { get; private set; } = 100f;

		public float MaxStamina => 100f;

		public bool IsDodging { get; private set; }

		public bool IsMoving { get; private set; }

		public Vector2 MoveInput { get; private set; }

		public Vector3 WorldMoveDirection { get; private set; }

		public Vector3 LastValidWorldMoveDirection { get; private set; } = Vector3.forward;

		public bool IsLocomotionLocked => _locomotionLocked;

		public event Action<float, float> StaminaChanged;

		public event Action DodgeStarted;

		public event Action<Vector3> Footstep;

		public void Initialize(PlayerInputReader input, Camera camera, Damageable damageable, Vector3 arenaCenter, float arenaRadius, float moveSpeed = 5.4f, float rotationSpeed = 14f)
		{
			Initialize(input, camera, damageable, PlayerMovementBoundary.Circle(arenaCenter, arenaRadius), moveSpeed, rotationSpeed);
		}

		public void Initialize(PlayerInputReader input, Camera camera, Damageable damageable, PlayerMovementBoundary movementBoundary, float moveSpeed = 5.4f, float rotationSpeed = 14f)
		{
			if (_input != null)
			{
				_input.DodgePressed -= TryDodge;
			}
			_input = input;
			_camera = camera;
			_damageable = damageable;
			_movementBoundary = movementBoundary;
			_moveSpeed = moveSpeed;
			_rotationSpeed = rotationSpeed;
			_controller = GetComponent<CharacterController>();
			_input.DodgePressed += TryDodge;
			_lastFootstepPosition = base.transform.position;
			_footstepDistance = 0f;
			_locomotionLocked = false;
			RestoreStaminaToFull();
			SetMovementEnabled(enabled: true);
		}

		private void Update()
		{
			if (_input == null || _camera == null || !_canMove)
			{
				IsMoving = false;
				MoveInput = Vector2.zero;
				WorldMoveDirection = Vector3.zero;
				RegenerateStamina();
				ResetFootstepTracking();
				return;
			}
			Vector3 forward = _camera.transform.forward;
			forward.y = 0f;
			forward.Normalize();
			Vector3 right = _camera.transform.right;
			right.y = 0f;
			right.Normalize();
			Vector2 vector = (MoveInput = (_locomotionLocked ? Vector2.zero : _input.Move));
			Vector3 vector3 = forward * vector.y + right * vector.x;
			vector3 = (WorldMoveDirection = Vector3.ClampMagnitude(vector3, 1f));
			if (vector3.sqrMagnitude > 0.0025f)
			{
				LastValidWorldMoveDirection = vector3.normalized;
			}
			IsMoving = vector3.sqrMagnitude > 0.01f && !IsDodging;
			if (IsMoving)
			{
				Quaternion b = Quaternion.LookRotation(vector3);
				base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, _rotationSpeed * Time.deltaTime);
				MoveAndConstrain(vector3 * (_moveSpeed * Time.deltaTime));
			}
			if (!_controller.isGrounded)
			{
				_controller.Move(Physics.gravity * Time.deltaTime);
			}
			UpdateFootsteps();
			RegenerateStamina();
		}

		public void SetMovementEnabled(bool enabled)
		{
			_canMove = enabled;
		}

		public void SetLocomotionLocked(bool locked)
		{
			_locomotionLocked = locked;
			if (locked)
			{
				IsMoving = false;
				MoveInput = Vector2.zero;
				WorldMoveDirection = Vector3.zero;
				ResetFootstepTracking();
			}
		}

		public void RestoreStaminaToFull()
		{
			CurrentStamina = MaxStamina;
			this.StaminaChanged?.Invoke(CurrentStamina, MaxStamina);
		}

		public void ConfigureBoundary(PlayerMovementBoundary movementBoundary)
		{
			_movementBoundary = movementBoundary;
		}

		public void CancelTransientMovement()
		{
			if (_dodgeRoutine != null)
			{
				StopCoroutine(_dodgeRoutine);
			}
			_dodgeRoutine = null;
			IsDodging = false;
			IsMoving = false;
			MoveInput = Vector2.zero;
			WorldMoveDirection = Vector3.zero;
			ResetFootstepTracking();
			if (_damageable != null)
			{
				_damageable.IsInvulnerable = false;
			}
		}

		public void CancelForSceneTransition()
		{
			CancelTransientMovement();
			_locomotionLocked = false;
			SetMovementEnabled(enabled: false);
		}

		public void Nudge(Vector3 direction, float distance)
		{
			if (_controller != null)
			{
				MoveAndConstrain(direction.normalized * distance);
			}
		}

		public void Teleport(Vector3 position)
		{
			_controller.enabled = false;
			base.transform.position = position;
			_controller.enabled = true;
		}

		public bool TryTeleport(Vector3 position)
		{
			if (!CanTeleportTo(position))
			{
				return false;
			}
			Teleport(position);
			return true;
		}

		public bool CanTeleportTo(Vector3 position)
		{
			if (_controller == null)
			{
				_controller = GetComponent<CharacterController>();
			}
			if (_controller == null)
			{
				return false;
			}
			float num = _controller.radius * Mathf.Max(Mathf.Abs(base.transform.lossyScale.x), Mathf.Abs(base.transform.lossyScale.z));
			if (_movementBoundary.HasHardBoundary && !_movementBoundary.Contains(position, num))
			{
				return false;
			}
			float num2 = Mathf.Max(_controller.height * Mathf.Abs(base.transform.lossyScale.y), num * 2f);
			Vector3 vector = position + base.transform.rotation * Vector3.Scale(_controller.center, base.transform.lossyScale);
			float num3 = num2 * 0.5f - num;
			Vector3 vector2 = base.transform.up * (_controller.skinWidth + 0.02f);
			Vector3 point = vector - base.transform.up * num3 + vector2;
			Vector3 point2 = vector + base.transform.up * num3 + vector2;
			int num4 = Physics.OverlapCapsuleNonAlloc(point, point2, Mathf.Max(0.05f, num - _controller.skinWidth), _teleportOverlaps, -5, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < num4; i++)
			{
				Collider collider = _teleportOverlaps[i];
				if (collider != null && collider.transform.root != base.transform.root)
				{
					return false;
				}
			}
			return true;
		}

		private void TryDodge()
		{
			if (_canMove && !IsDodging && !(CurrentStamina < 28f))
			{
				this.DodgeStarted?.Invoke();
				_dodgeRoutine = StartCoroutine(DodgeRoutine());
			}
		}

		private IEnumerator DodgeRoutine()
		{
			IsDodging = true;
			IsMoving = true;
			CurrentStamina -= 28f;
			this.StaminaChanged?.Invoke(CurrentStamina, MaxStamina);
			_damageable.IsInvulnerable = true;
			Vector3 direction = base.transform.forward;
			if (_input.Move.sqrMagnitude > 0.05f)
			{
				Vector3 cameraForward = _camera.transform.forward;
				cameraForward.y = 0f;
				cameraForward.Normalize();
				Vector3 cameraRight = _camera.transform.right;
				cameraRight.y = 0f;
				cameraRight.Normalize();
				direction = (cameraForward * _input.Move.y + cameraRight * _input.Move.x).normalized;
			}
			WorldMoveDirection = direction;
			if (direction.sqrMagnitude > 0.0025f)
			{
				LastValidWorldMoveDirection = direction.normalized;
			}
			float elapsed = 0f;
			while (elapsed < 0.32f)
			{
				elapsed += Time.deltaTime;
				MoveAndConstrain(direction * (10.5f * Time.deltaTime));
				yield return null;
			}
			_damageable.IsInvulnerable = false;
			IsDodging = false;
			IsMoving = false;
			MoveInput = Vector2.zero;
			WorldMoveDirection = Vector3.zero;
			_dodgeRoutine = null;
		}

		private void RegenerateStamina()
		{
			if (!IsDodging && !(CurrentStamina >= MaxStamina))
			{
				CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + 23f * Time.deltaTime);
				this.StaminaChanged?.Invoke(CurrentStamina, MaxStamina);
			}
		}

		private void UpdateFootsteps()
		{
			Vector3 position = base.transform.position;
			Vector3 vector = position - _lastFootstepPosition;
			vector.y = 0f;
			_lastFootstepPosition = position;
			if (!IsMoving || !_controller.isGrounded)
			{
				_footstepDistance = 0f;
				return;
			}
			_footstepDistance += vector.magnitude;
			if (!(_footstepDistance < 1.65f))
			{
				_footstepDistance = 0f;
				this.Footstep?.Invoke(base.transform.position);
			}
		}

		private void ResetFootstepTracking()
		{
			_lastFootstepPosition = base.transform.position;
			_footstepDistance = 0f;
		}

		private void MoveAndConstrain(Vector3 motion)
		{
			_controller.Move(motion);
			if (_movementBoundary.HasHardBoundary)
			{
				Vector3 position = base.transform.position;
				Vector3 vector = _movementBoundary.Constrain(position);
				if (!((vector - position).sqrMagnitude <= 1E-06f))
				{
					_controller.enabled = false;
					base.transform.position = vector;
					_controller.enabled = true;
				}
			}
		}

		private void OnDisable()
		{
			CancelTransientMovement();
		}

		private void OnDestroy()
		{
			CancelTransientMovement();
			if (_input != null)
			{
				_input.DodgePressed -= TryDodge;
			}
		}
	}
}
