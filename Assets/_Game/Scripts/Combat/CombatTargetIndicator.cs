using Eidren.AI;
using Eidren.Interaction;
using UnityEngine.Rendering;
using UnityEngine;

namespace Eidren.Combat
{
	[DisallowMultipleComponent]
	public sealed class CombatTargetIndicator : MonoBehaviour
	{
		private const int RingSegments = 64;
		private const float FadeInDuration = .11f;
		private const float FadeOutDuration = .18f;
		[SerializeField] private LineRenderer primaryRing;
		[SerializeField] private LineRenderer accentRing;
		[SerializeField] private LayerMask groundLayers = -1;
		private readonly RaycastHit[] _groundHits = new RaycastHit[24];
		private PlayerCombatController _combat;
		private PersistentCombatTargetQuery _query;
		private ResourceTargetIndicator _interaction;
		private Transform _target;
		private Renderer[] _renderers;
		private Collider[] _colliders;
		private CombatTargetRingStyle _style;
		private CombatTargetCategory _category;
		private Vector3 _positionVelocity;
		private float _radius, _radiusVelocity, _alpha;
		private int _targetIdentity;
		private bool _desiredVisible, _poseInitialized;

		public bool IsVisible => _alpha > .01f;
		public int TargetIdentity => _targetIdentity;
		public CombatTargetCategory Category => _category;
		public float Alpha => _alpha;
		public Transform RingTransform => transform;
		public LineRenderer PrimaryRing => primaryRing;
		public LineRenderer AccentRing => accentRing;

		public void ConfigurePrefabReferences(LineRenderer primary, LineRenderer accent)
		{
			primaryRing = primary;
			accentRing = accent;
			BuildUnitGeometry();
			SetRenderers(false);
		}

		public void Bind(PersistentCombatTargetQuery query, ResourceTargetIndicator interaction = null)
		{
			if (_query != null) _query.Changed -= HandleSnapshot;
			_query = query;
			_interaction = interaction != null ? interaction : GetComponentInParent<ResourceTargetIndicator>();
			if (_query != null)
			{
				_query.Changed += HandleSnapshot;
				HandleSnapshot(_query.Current);
			}
			else HandleSnapshot(default);
		}

		private void Awake()
		{
			_combat = GetComponentInParent<PlayerCombatController>();
			BuildUnitGeometry();
			_style = CombatTargetRingStyles.Resolve(CombatTargetCategory.Normal);
			SetRenderers(false);
		}

		private void LateUpdate()
		{
			if (_query == null && _combat != null && _combat.Targeting != null) Bind(_combat.Targeting);
			if (_interaction == null) _interaction = GetComponentInParent<ResourceTargetIndicator>();
			if (_query != null && _query.Current.Identity != _targetIdentity) HandleSnapshot(_query.Current);
			bool targetValid = _desiredVisible && _target != null && _target.gameObject.activeInHierarchy;
			float duration = targetValid ? FadeInDuration : FadeOutDuration;
			_alpha = Mathf.MoveTowards(_alpha, targetValid ? 1f : 0f, Time.unscaledDeltaTime / duration);
			if (targetValid) UpdatePose();
			UpdateAppearance();
			SetRenderers(_alpha > .01f);
			_interaction?.SetCombatSuppressed(_alpha > .01f || targetValid);
			if (!targetValid && _alpha <= 0f) ClearReleasedTarget();
		}

		private void HandleSnapshot(CombatTargetSnapshot snapshot)
		{
			if (snapshot.Identity == 0 || !snapshot.Vitals.Alive || !snapshot.Vitals.Active)
			{
				_desiredVisible = false;
				_targetIdentity = 0;
				return;
			}
			if (_targetIdentity != snapshot.Identity)
			{
				_targetIdentity = snapshot.Identity;
				_target = snapshot.Target.Transform;
				_renderers = _target.GetComponentsInChildren<Renderer>(false);
				_colliders = _target.GetComponentsInChildren<Collider>(false);
				_poseInitialized = false;
				_alpha = Mathf.Min(_alpha, .35f);
			}
			_category = snapshot.Category;
			_style = CombatTargetRingStyles.Resolve(_category);
			_desiredVisible = true;
		}

		private void UpdatePose()
		{
			Bounds bounds = CalculateBounds();
			ResolveGround(bounds, out Vector3 desiredPosition, out Vector3 normal);
			float extent = Mathf.Max(bounds.extents.x, bounds.extents.z);
			float desiredRadius = Mathf.Clamp(extent * _style.RadiusFactor, .68f, _style.MaximumRadius);
			Quaternion desiredRotation = Quaternion.FromToRotation(Vector3.up, normal);
			if (!_poseInitialized)
			{
				transform.position = desiredPosition;
				transform.rotation = desiredRotation;
				_radius = desiredRadius;
				_positionVelocity = Vector3.zero;
				_radiusVelocity = 0f;
				_poseInitialized = true;
				return;
			}
			float delta = Mathf.Max(.001f, Time.unscaledDeltaTime);
			transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _positionVelocity, .055f, 30f, delta);
			transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-18f * delta));
			_radius = Mathf.SmoothDamp(_radius, desiredRadius, ref _radiusVelocity, .075f, 20f, delta);
		}

		private Bounds CalculateBounds()
		{
			Bounds bounds = new Bounds(_target.position + Vector3.up, new Vector3(1f, 2f, 1f));
			bool found = false;
			if (_renderers != null)
				foreach (Renderer renderer in _renderers)
					if (renderer != null && renderer.enabled && !(renderer is ParticleSystemRenderer) && !(renderer is TrailRenderer) && !(renderer is LineRenderer))
					{ if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds); }
			if (_colliders != null)
				foreach (Collider collider in _colliders)
					if (collider != null && collider.enabled && !collider.isTrigger)
					{ if (!found) { bounds = collider.bounds; found = true; } else bounds.Encapsulate(collider.bounds); }
			return bounds;
		}

		private void ResolveGround(Bounds bounds, out Vector3 position, out Vector3 normal)
		{
			Vector3 origin = new Vector3(bounds.center.x, bounds.max.y + 1.5f, bounds.center.z);
			float distance = Mathf.Max(5f, bounds.size.y + 4f);
			int count = Physics.RaycastNonAlloc(origin, Vector3.down, _groundHits, distance, groundLayers, QueryTriggerInteraction.Ignore);
			float nearest = float.PositiveInfinity; RaycastHit ground = default; bool found = false;
			for (int i = 0; i < count; i++)
			{
				Transform hit = _groundHits[i].transform;
				if (hit == null || hit.IsChildOf(_target) || hit.root == transform.root || _groundHits[i].distance >= nearest) continue;
				nearest = _groundHits[i].distance; ground = _groundHits[i]; found = true;
			}
			if (found) { normal = ground.normal; position = ground.point + normal * .075f; }
			else { normal = Vector3.up; position = new Vector3(bounds.center.x, bounds.min.y + .075f, bounds.center.z); }
		}

		private void UpdateAppearance()
		{
			float pulse = 1f + Mathf.Sin(Time.unscaledTime * _style.PulseSpeed * Mathf.PI * 2f) * _style.PulseAmount;
			transform.localScale = Vector3.one * (_radius * pulse);
			Color primary = _style.PrimaryColor; primary.a *= _alpha;
			Color accent = _style.AccentColor; accent.a *= _alpha;
			if (primaryRing != null) { primaryRing.widthMultiplier = _style.Width / Mathf.Max(.01f, _radius); primaryRing.startColor = primary; primaryRing.endColor = primary; }
			if (accentRing != null)
			{
				accentRing.widthMultiplier = _style.Width * .58f / Mathf.Max(.01f, _radius);
				accentRing.startColor = accent; accentRing.endColor = accent;
				accentRing.enabled = _alpha > .01f && _style.HasAccent;
				accentRing.transform.localRotation = Quaternion.Euler(0f, Time.unscaledTime * (_category == CombatTargetCategory.Boss ? 18f : -12f), 0f);
			}
		}

		private void BuildUnitGeometry()
		{
			if (primaryRing != null)
			{
				primaryRing.useWorldSpace = false; primaryRing.loop = true; primaryRing.positionCount = RingSegments;
				for (int i = 0; i < RingSegments; i++)
				{
					float angle = i * Mathf.PI * 2f / RingSegments;
					primaryRing.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
				}
			}
			if (accentRing != null)
			{
				accentRing.useWorldSpace = false; accentRing.loop = true; accentRing.positionCount = 16;
				for (int i = 0; i < 16; i++)
				{
					float angle = i * Mathf.PI * 2f / 16f;
					float radius = i % 2 == 0 ? 1.10f : 1.02f;
					accentRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .006f, Mathf.Sin(angle) * radius));
				}
			}
		}

		private void SetRenderers(bool visible)
		{
			if (primaryRing != null) primaryRing.enabled = visible;
			if (accentRing != null) accentRing.enabled = visible && _style.HasAccent;
		}

		private void ClearReleasedTarget()
		{
			_target = null; _renderers = null; _colliders = null; _poseInitialized = false;
		}

		private void OnDisable()
		{
			if (_query != null) _query.Changed -= HandleSnapshot;
			_query = null; _desiredVisible = false; _alpha = 0f; SetRenderers(false);
			_interaction?.SetCombatSuppressed(false);
		}
	}
}

