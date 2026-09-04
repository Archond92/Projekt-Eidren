using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Combat
{
	[DisallowMultipleComponent]
	public sealed class MeleeWeaponHitbox : MonoBehaviour
	{
		private const int OverlapCapacity = 32;

		private const float QueryHeight = 1f;

		[SerializeField]
		private LayerMask targetLayers = -1;

		[SerializeField]
		private WeaponData editorPreviewWeapon;

		private readonly Collider[] _overlaps = new Collider[32];

		private readonly HashSet<int> _hitTargetIds = new HashSet<int>();

		private AttackStepData _currentStep;

		private bool _hasPreview;

		public bool IsWindowActive { get; private set; }

		public int HitCount => _hitTargetIds.Count;

		public AttackStepData CurrentStep => _currentStep;

		public WeaponData EditorPreviewWeapon => editorPreviewWeapon;

		public event Action WindowOpened;

		public event Action WindowClosed;

		public void ConfigureEditorPreview(WeaponData weapon)
		{
			editorPreviewWeapon = weapon;
		}

		public void SetPreview(AttackStepData step)
		{
			_currentStep = step;
			_hasPreview = step.HitboxRange > 0f;
		}

		public void BeginWindow(AttackStepData step)
		{
			if (IsWindowActive)
			{
				throw new InvalidOperationException("A melee hitbox window is already active.");
			}
			if (step.HitboxRange <= 0f)
			{
				throw new ArgumentOutOfRangeException("step", "Hitbox range must be greater than zero.");
			}
			if (!step.IsOmnidirectional && step.HitboxAngle <= 0f)
			{
				throw new ArgumentOutOfRangeException("step", "A directional hitbox requires an angle greater than zero.");
			}
			_currentStep = step;
			_hasPreview = true;
			_hitTargetIds.Clear();
			IsWindowActive = true;
			this.WindowOpened?.Invoke();
		}

		public int Evaluate(Transform attacker, Action<CombatTarget> onTarget)
		{
			if (!IsWindowActive || attacker == null || onTarget == null)
			{
				return 0;
			}
			Vector3 position = attacker.position + Vector3.up * 1f;
			int num = Physics.OverlapSphereNonAlloc(position, _currentStep.HitboxRange, _overlaps, targetLayers, QueryTriggerInteraction.Collide);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				Collider overlap = _overlaps[i];
				Vector3 bodyPoint = CombatTargetGeometry.ClosestBodyPoint(overlap, attacker.position);
				if (TryResolveTarget(overlap, attacker, out var target) && MeleeHitboxGeometry.Contains(attacker.position, attacker.forward, bodyPoint, _currentStep.HitboxRange, _currentStep.HitboxAngle, _currentStep.IsOmnidirectional) && _hitTargetIds.Add(target.HitIdentity))
				{
					onTarget(target);
					num2++;
				}
			}
			return num2;
		}

		public void EndWindow()
		{
			if (IsWindowActive)
			{
				IsWindowActive = false;
				this.WindowClosed?.Invoke();
			}
		}

		private static bool TryResolveTarget(Collider overlap, Transform attacker, out CombatTarget target)
		{
			target = default(CombatTarget);
			if (overlap == null)
			{
				return false;
			}
			if (!CombatTargetRegistry.TryResolve(overlap.transform, attacker.root, null, out var result))
			{
				return false;
			}
			target = new CombatTarget(result);
			return true;
		}

		private void OnDisable()
		{
			EndWindow();
		}

		private void OnDrawGizmosSelected()
		{
			AttackStepData attackStepData = _currentStep;
			if (!_hasPreview)
			{
				if (editorPreviewWeapon == null || editorPreviewWeapon.Combo == null || editorPreviewWeapon.Combo.Length == 0)
				{
					return;
				}
				attackStepData = editorPreviewWeapon.Combo[0];
			}
			if (!(attackStepData.HitboxRange <= 0f))
			{
				Gizmos.color = (IsWindowActive ? new Color(1f, 0.2f, 0.1f, 0.9f) : new Color(1f, 0.72f, 0.15f, 0.75f));
				Vector3 center = base.transform.position + Vector3.up * 1f;
				MeleeHitboxGizmos.DrawSector(center, base.transform.forward, attackStepData.HitboxRange, attackStepData.HitboxAngle, attackStepData.IsOmnidirectional);
			}
		}
	}

	public static class MeleeHitboxGeometry
	{
		public static bool Contains(Vector3 origin, Vector3 forward, Vector3 target, float range, float fullAngle, bool omnidirectional = false)
		{
			if (range <= 0f)
			{
				return false;
			}
			Vector3 vector = target - origin;
			vector.y = 0f;
			if (vector.sqrMagnitude > range * range)
			{
				return false;
			}
			if (vector.sqrMagnitude < 0.001f)
			{
				return true;
			}
			if (omnidirectional || fullAngle >= 359.9f)
			{
				return true;
			}
			if (fullAngle <= 0f)
			{
				return false;
			}
			forward.y = 0f;
			if (forward.sqrMagnitude < 0.001f)
			{
				return false;
			}
			float num = Vector3.Angle(forward.normalized, vector.normalized);
			return num <= fullAngle * 0.5f;
		}
	}

	internal static class MeleeHitboxGizmos
	{
		public static void DrawSector(Vector3 center, Vector3 forward, float range, float fullAngle, bool omnidirectional)
		{
			forward.y = 0f;
			if (forward.sqrMagnitude < 0.001f)
			{
				forward = Vector3.forward;
			}
			forward.Normalize();
			float num = (omnidirectional ? 360f : Mathf.Clamp(fullAngle, 0f, 360f));
			float num2 = (omnidirectional ? (-180f) : ((0f - num) * 0.5f));
			Vector3 vector = center + Quaternion.AngleAxis(num2, Vector3.up) * forward * range;
			Vector3 vector2 = vector;
			for (int i = 1; i <= 32; i++)
			{
				float angle = num2 + num * ((float)i / 32f);
				Vector3 vector3 = center + Quaternion.AngleAxis(angle, Vector3.up) * forward * range;
				Gizmos.DrawLine(vector2, vector3);
				vector2 = vector3;
			}
			if (omnidirectional)
			{
				Gizmos.DrawLine(vector2, vector);
				return;
			}
			Gizmos.DrawLine(center, vector);
			Gizmos.DrawLine(center, vector2);
		}
	}
}
