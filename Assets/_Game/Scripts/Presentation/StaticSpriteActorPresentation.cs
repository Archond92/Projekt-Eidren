using UnityEngine;

namespace Eidren.Presentation
{
	[RequireComponent(typeof(SpriteRenderer))]
	public sealed class StaticSpriteActorPresentation : MonoBehaviour, IActorPresentation
	{
		[SerializeField]
		private SpriteRenderer target;

		[SerializeField]
		private bool faceCamera = true;

		[SerializeField]
		private float hitPulseSeconds = 0.12f;

		private Camera _camera;

		private Vector3 _baseScale;

		private float _pulseUntil;

		public ActorFacing8 Facing { get; private set; } = ActorFacing8.S;

		public ActorVisualState VisualState { get; private set; } = ActorVisualState.Idle;

		// Bisheriger Occlusion-Standard fuer statische Akteure (keine echte Groessenermittlung noetig).
		public float WorldHeight => 2f;

		public void Configure(SpriteRenderer renderer, bool billboard = true)
		{
			target = renderer;
			faceCamera = billboard;
		}

		public void SetFacing(ActorFacing8 facing)
		{
			Facing = facing;
			if (target != null)
			{
				target.flipX = facing == ActorFacing8.W || facing == ActorFacing8.NW || facing == ActorFacing8.SW;
			}
		}

		public void SetVisualState(ActorVisualState state, float normalizedTime = 0f)
		{
			VisualState = state;
			if (state == ActorVisualState.Hit || state == ActorVisualState.Stagger)
			{
				_pulseUntil = Time.time + hitPulseSeconds;
			}
		}

		public void SetTint(Color color)
		{
			if (target != null)
			{
				target.color = color;
			}
		}

		private void Awake()
		{
			if (target == null)
			{
				target = GetComponent<SpriteRenderer>();
			}
			_baseScale = base.transform.localScale;
		}

		private void OnEnable()
		{
			ActorPresentationRegistry.Register(this);
		}

		private void OnDisable()
		{
			ActorPresentationRegistry.Unregister(this);
		}

		private void LateUpdate()
		{
			if (faceCamera)
			{
				if (_camera == null)
				{
					_camera = Camera.main;
				}
				if (_camera != null)
				{
					base.transform.rotation = Quaternion.LookRotation(_camera.transform.forward, Vector3.up);
				}
			}
			float num = ((Time.time < _pulseUntil) ? 1.08f : 1f);
			float num2 = ((VisualState == ActorVisualState.Move) ? (1f + Mathf.Sin(Time.time * 10f) * 0.025f) : 1f);
			base.transform.localScale = _baseScale * num * num2;
			if (target != null)
			{
				target.enabled = VisualState != ActorVisualState.Death;
			}
		}
	}
}
