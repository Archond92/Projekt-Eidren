using Eidren.Interaction;
using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace Eidren.Gameplay.Presentation
{
	public sealed class CopperMiningVisualFeedback : MonoBehaviour
	{
		private static readonly float[] StrikeProgress = new float[2] { 0.36f, 0.84f };

		[SerializeField]
		private Transform animatedOreRoot;

		[SerializeField]
		private Transform[] impactPoints = Array.Empty<Transform>();

		[SerializeField]
		private Renderer[] oreRenderers = Array.Empty<Renderer>();

		[SerializeField]
		private Material stoneFragmentMaterial;

		[SerializeField]
		private Material copperSparkMaterial;

		private ResourceNode _node;

		private Vector3 _restPosition;

		private Quaternion _restRotation;

		private Vector3 _restScale;

		private Color[] _restColors = Array.Empty<Color>();

		private MaterialPropertyBlock _propertyBlock;

		private float _impactKick;

		private int _nextStrike;

		public bool IsMining { get; private set; }

		public int ImpactCount { get; private set; }

		public int ImpactPointCount
		{
			get
			{
				Transform[] array = impactPoints;
				return (array != null) ? array.Length : 0;
			}
		}

		public bool HasFragmentMaterials => stoneFragmentMaterial != null && copperSparkMaterial != null;

		public void Configure(Transform oreRoot, Transform[] configuredImpactPoints, Renderer[] configuredOreRenderers, Material configuredStoneMaterial, Material configuredCopperMaterial)
		{
			animatedOreRoot = oreRoot;
			impactPoints = configuredImpactPoints ?? Array.Empty<Transform>();
			oreRenderers = configuredOreRenderers ?? Array.Empty<Renderer>();
			stoneFragmentMaterial = configuredStoneMaterial;
			copperSparkMaterial = configuredCopperMaterial;
			CaptureRestState();
		}

		private void Awake()
		{
			CaptureRestState();
		}

		private void OnEnable()
		{
			Bind();
			ResetFeedback();
		}

		private void OnDisable()
		{
			Unbind();
			ResetFeedback();
		}

		private void Update()
		{
			if (!(animatedOreRoot == null))
			{
				_impactKick = Mathf.MoveTowards(_impactKick, 0f, Time.deltaTime * 4.8f);
				float num = ((ImpactCount % 2 == 0) ? 1f : (-1f));
				animatedOreRoot.localPosition = _restPosition + new Vector3(num * _impactKick * 0.018f, _impactKick * 0.032f, (0f - _impactKick) * 0.014f);
				animatedOreRoot.localRotation = _restRotation * Quaternion.Euler((0f - _impactKick) * 3.5f, num * _impactKick * 2.2f, num * _impactKick * 2.8f);
				animatedOreRoot.localScale = _restScale * (1f + _impactKick * 0.018f);
				ApplyOreFlash(_impactKick);
			}
		}

		private void Bind()
		{
			if (!(_node != null))
			{
				_node = GetComponentInParent<ResourceNode>();
				if (_node != null)
				{
					_node.ProgressChanged += HandleProgress;
				}
			}
		}

		private void Unbind()
		{
			if (_node != null)
			{
				_node.ProgressChanged -= HandleProgress;
			}
			_node = null;
		}

		private void HandleProgress(float normalizedProgress)
		{
			float num = Mathf.Clamp01(normalizedProgress);
			if (num <= 0.001f)
			{
				IsMining = false;
				_nextStrike = 0;
				_impactKick = 0f;
				RestoreOreState();
				return;
			}
			IsMining = num < 1f;
			while (_nextStrike < StrikeProgress.Length && num >= StrikeProgress[_nextStrike])
			{
				TriggerImpact(_nextStrike);
				_nextStrike++;
			}
			if (num >= 1f)
			{
				IsMining = false;
			}
		}

		private void TriggerImpact(int strikeIndex)
		{
			ImpactCount++;
			_impactKick = 1f;
			if (Application.isPlaying && impactPoints != null && impactPoints.Length != 0)
			{
				Transform transform = impactPoints[strikeIndex % impactPoints.Length];
				Vector3 position = base.transform.position;
				for (int i = 0; i < 7; i++)
				{
					Vector3 vector = transform.position - position;
					vector.y = Mathf.Max(0.2f, vector.y);
					vector = ((vector.sqrMagnitude > 0.001f) ? vector.normalized : Vector3.up);
					Vector3 velocity = (vector + UnityEngine.Random.insideUnitSphere * 0.72f + Vector3.up * 0.85f).normalized * UnityEngine.Random.Range(0.85f, 1.65f);
					bool flag = i >= 4;
					CopperMiningDebrisParticle.Spawn(transform.position + UnityEngine.Random.insideUnitSphere * 0.055f, velocity, flag ? copperSparkMaterial : stoneFragmentMaterial, flag);
				}
			}
		}

		private void CaptureRestState()
		{
			if (animatedOreRoot != null)
			{
				_restPosition = animatedOreRoot.localPosition;
				_restRotation = animatedOreRoot.localRotation;
				_restScale = animatedOreRoot.localScale;
			}
			if (_propertyBlock == null)
			{
				_propertyBlock = new MaterialPropertyBlock();
			}
			Renderer[] array = oreRenderers;
			_restColors = new Color[(array != null) ? array.Length : 0];
			for (int i = 0; i < _restColors.Length; i++)
			{
				Material material = ((oreRenderers[i] != null) ? oreRenderers[i].sharedMaterial : null);
				_restColors[i] = ((material != null && material.HasProperty("_BaseColor")) ? material.GetColor("_BaseColor") : Color.white);
			}
		}

		private void ApplyOreFlash(float amount)
		{
			if (oreRenderers == null || _propertyBlock == null)
			{
				return;
			}
			Color b = new Color(1f, 0.68f, 0.18f, 1f);
			for (int i = 0; i < oreRenderers.Length; i++)
			{
				Renderer renderer = oreRenderers[i];
				if (!(renderer == null))
				{
					renderer.GetPropertyBlock(_propertyBlock);
					Color a = ((i < _restColors.Length) ? _restColors[i] : Color.white);
					_propertyBlock.SetColor("_BaseColor", Color.Lerp(a, b, amount * 0.72f));
					renderer.SetPropertyBlock(_propertyBlock);
				}
			}
		}

		private void RestoreOreState()
		{
			if (animatedOreRoot != null)
			{
				animatedOreRoot.localPosition = _restPosition;
				animatedOreRoot.localRotation = _restRotation;
				animatedOreRoot.localScale = _restScale;
			}
			ApplyOreFlash(0f);
		}

		private void ResetFeedback()
		{
			IsMining = false;
			_nextStrike = 0;
			_impactKick = 0f;
			RestoreOreState();
		}
	}

	internal sealed class CopperMiningDebrisParticle : MonoBehaviour
	{
		private Vector3 _velocity;

		private Vector3 _angularVelocity;

		private float _remaining;

		private float _lifetime;

		private bool _spark;

		public static void Spawn(Vector3 position, Vector3 velocity, Material material, bool spark)
		{
			GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
			gameObject.name = (spark ? "CopperImpactSpark" : "CopperRockChip");
			gameObject.transform.position = position;
			gameObject.transform.rotation = Random.rotation;
			float num = Random.Range(0.018f, 0.036f);
			gameObject.transform.localScale = (spark ? new Vector3(num * 0.45f, num * 2.8f, num * 0.45f) : new Vector3(num * 1.2f, num, num * 0.9f));
			Object.Destroy(gameObject.GetComponent<Collider>());
			gameObject.GetComponent<Renderer>().sharedMaterial = material;
			CopperMiningDebrisParticle copperMiningDebrisParticle = gameObject.AddComponent<CopperMiningDebrisParticle>();
			copperMiningDebrisParticle._velocity = velocity;
			copperMiningDebrisParticle._angularVelocity = Random.insideUnitSphere * (spark ? 520f : 260f);
			copperMiningDebrisParticle._lifetime = (spark ? Random.Range(0.2f, 0.34f) : Random.Range(0.38f, 0.62f));
			copperMiningDebrisParticle._remaining = copperMiningDebrisParticle._lifetime;
			copperMiningDebrisParticle._spark = spark;
		}

		private void Update()
		{
			float deltaTime = Time.deltaTime;
			_remaining -= deltaTime;
			if (_remaining <= 0f)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			if (!_spark)
			{
				_velocity += Physics.gravity * (deltaTime * 0.82f);
			}
			base.transform.position += _velocity * deltaTime;
			base.transform.Rotate(_angularVelocity * deltaTime, Space.World);
			float t = Mathf.Clamp01(_remaining / _lifetime);
			base.transform.localScale *= Mathf.Lerp(0.86f, 1f, t);
		}
	}
}
