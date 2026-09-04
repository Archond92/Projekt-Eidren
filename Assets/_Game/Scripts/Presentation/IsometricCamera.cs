using UnityEngine;

namespace Eidren.Presentation
{
	public sealed class IsometricCamera : MonoBehaviour
	{
		private Transform _target;

		private Transform _secondaryTarget;

		private Camera _camera;

		private Vector3 _offset;

		private Vector3 _velocity;

		private Vector3 _lookAhead;

		private Vector3 _lastTargetPosition;

		private Vector3 _arenaCenter;

		private float _arenaRadius;

		private Bounds _rectangularBounds;

		private bool _useRectangularBounds;

		private float _combatSeparation;

		private float _followSmoothTime = 0.16f;

		private float _lookAheadDistance = 1.8f;

		private float _baseOrthographicSize = 7.4f;

		private float _minimumOrthographicSize = 5.5f;

		private float _boundsInset = 4.8f;

		private Vector3 _fixedRotation = new Vector3(52f, 45f, 0f);

		public Bounds ActiveBounds => _rectangularBounds;

		public bool UsesBoundsProvider => _useRectangularBounds;

		public void ConfigureZoneView(Vector3 fixedRotation, float baseOrthographicSize, float minimumOrthographicSize, float boundsInset)
		{
			_fixedRotation = fixedRotation;
			_baseOrthographicSize = Mathf.Max(1f, baseOrthographicSize);
			_minimumOrthographicSize = Mathf.Clamp(minimumOrthographicSize, 1f, _baseOrthographicSize);
			_boundsInset = Mathf.Max(0f, boundsInset);
		}

		public void Initialize(Transform target, Transform secondaryTarget, Vector3 offset, Vector3 arenaCenter, float arenaRadius)
		{
			_target = target;
			_secondaryTarget = secondaryTarget;
			_offset = offset;
			_arenaCenter = arenaCenter;
			_arenaRadius = arenaRadius;
			_useRectangularBounds = false;
			_camera = GetComponent<Camera>();
			_lastTargetPosition = target.position;
			SetFixedRotation();
			Vector3 vector = ClampFocusToArena(GetFramingPosition(target.position));
			base.transform.position = vector + offset;
			UpdateZoom(immediate: true);
		}

		public void InitializeWithinBounds(Transform target, Transform secondaryTarget, Vector3 offset, Bounds cameraBounds)
		{
			_target = target;
			_secondaryTarget = secondaryTarget;
			_offset = offset;
			_rectangularBounds = cameraBounds;
			_arenaCenter = cameraBounds.center;
			_arenaRadius = 0f;
			_useRectangularBounds = true;
			_camera = GetComponent<Camera>();
			_lastTargetPosition = target.position;
			SetFixedRotation();
			Vector3 vector = ClampFocusToBounds(GetFramingPosition(target.position));
			base.transform.position = vector + offset;
			UpdateZoom(immediate: true);
		}

		public void InitializeWithinBounds(Transform target, Transform secondaryTarget, Vector3 offset, ICameraBoundsProvider boundsProvider)
		{
			if (boundsProvider == null)
			{
				Debug.LogError("IsometricCamera requires a camera bounds provider.", this);
			}
			else
			{
				InitializeWithinBounds(target, secondaryTarget, offset, boundsProvider.CameraBounds);
			}
		}

		private void LateUpdate()
		{
			if (!(_target == null))
			{
				float num = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
				Vector3 vector = (_target.position - _lastTargetPosition) / num;
				vector.y = 0f;
				_lastTargetPosition = _target.position;
				Vector3 b = Vector3.ClampMagnitude(vector * 0.22f, _lookAheadDistance);
				_lookAhead = Vector3.Lerp(_lookAhead, b, 1f - Mathf.Exp(-7f * num));
				Vector3 vector2 = ClampFocusToBounds(GetFramingPosition(_target.position + _lookAhead));
				base.transform.position = Vector3.SmoothDamp(base.transform.position, vector2 + _offset, ref _velocity, _followSmoothTime, 1f / 0f, num);
				SetFixedRotation();
				UpdateZoom(immediate: false);
			}
		}

		private Vector3 GetFramingPosition(Vector3 primaryPosition)
		{
			_combatSeparation = 0f;
			if (_secondaryTarget == null)
			{
				return primaryPosition;
			}
			Vector3 position = _secondaryTarget.position;
			_combatSeparation = new Vector2(position.x - _target.position.x, position.z - _target.position.z).magnitude;
			if (_combatSeparation > 12.5f)
			{
				return primaryPosition;
			}
			float t = Mathf.InverseLerp(12.5f, 8.5f, _combatSeparation);
			float t2 = Mathf.Lerp(0f, 0.38f, t);
			return Vector3.Lerp(primaryPosition, position, t2);
		}

		private Vector3 ClampFocusToArena(Vector3 targetPosition)
		{
			Vector2 vector = new Vector2(targetPosition.x - _arenaCenter.x, targetPosition.z - _arenaCenter.z);
			float num = Mathf.Max(0f, _arenaRadius - 4.8f);
			if (vector.magnitude > num)
			{
				vector = vector.normalized * num;
				targetPosition.x = _arenaCenter.x + vector.x;
				targetPosition.z = _arenaCenter.z + vector.y;
			}
			targetPosition.y = _arenaCenter.y + 1.35f;
			return targetPosition;
		}

		private Vector3 ClampFocusToBounds(Vector3 targetPosition)
		{
			if (!_useRectangularBounds)
			{
				return ClampFocusToArena(targetPosition);
			}
			float num = _rectangularBounds.min.x + _boundsInset;
			float num2 = _rectangularBounds.max.x - _boundsInset;
			float num3 = _rectangularBounds.min.z + _boundsInset;
			float num4 = _rectangularBounds.max.z - _boundsInset;
			if (num > num2)
			{
				num = (num2 = _rectangularBounds.center.x);
			}
			if (num3 > num4)
			{
				num3 = (num4 = _rectangularBounds.center.z);
			}
			targetPosition.x = Mathf.Clamp(targetPosition.x, num, num2);
			targetPosition.z = Mathf.Clamp(targetPosition.z, num3, num4);
			targetPosition.y = _rectangularBounds.center.y + 1.35f;
			return targetPosition;
		}

		private void SetFixedRotation()
		{
			base.transform.rotation = Quaternion.Euler(_fixedRotation);
		}

		private void UpdateZoom(bool immediate)
		{
			if (!(_camera == null) && _camera.orthographic)
			{
				float num = Mathf.Max(1f, _camera.aspect);
				float num2 = (_useRectangularBounds ? Mathf.Max(1f, _rectangularBounds.extents.x - 1f) : (_arenaRadius - 1f));
				float b = (_useRectangularBounds ? Mathf.Max(1f, _rectangularBounds.extents.z - 1f) : (_arenaRadius - 1f));
				float a = num2 / num;
				float num3 = Mathf.Clamp(Mathf.Min(a, b), _minimumOrthographicSize, _baseOrthographicSize);
				if (_combatSeparation > 0f && _combatSeparation <= 12.5f)
				{
					float b2 = Mathf.Lerp(6.2f, _baseOrthographicSize, Mathf.InverseLerp(4f, 12.5f, _combatSeparation));
					num3 = Mathf.Max(num3, b2);
				}
				_camera.orthographicSize = (immediate ? num3 : Mathf.Lerp(_camera.orthographicSize, num3, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime)));
			}
		}
	}
}
