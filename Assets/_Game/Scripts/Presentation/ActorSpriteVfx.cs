using System;
using UnityEngine;
using Random = System.Random;

namespace Eidren.Presentation
{
	public sealed class ActorSpriteVfx : MonoBehaviour
	{
		private struct Particle
		{
			public Transform Transform;

			public SpriteRenderer Renderer;

			public Vector3 Origin;

			public Vector3 Velocity;

			public Color Color;

			public float StartTime;

			public float EndTime;

			public float Size;

			public bool Active;
		}

		private const int PoolSize = 40;

		[SerializeField]
		private Transform abilityAnchor;

		[SerializeField]
		private Transform bodyAnchor;

		[SerializeField]
		private Transform footAnchor;

		[SerializeField]
		private Material particleMaterial;

		[SerializeField]
		private string particleResourcePath = "Art/Actors/Terrock/terrock_particle_soft";

		[SerializeField]
		private Color ability1Color = new Color(0.18f, 1f, 0.62f, 0.92f);

		[SerializeField]
		private Color ability2Color = new Color(0.78f, 0.95f, 0.34f, 0.82f);

		[SerializeField]
		private Color hitColor = new Color(0.95f, 0.72f, 0.25f, 0.9f);

		[SerializeField]
		private Color appearColor = new Color(0.2f, 1f, 0.68f, 0.82f);

		[SerializeField]
		private float effectScale = 1f;

		private readonly Particle[] _particles = new Particle[40];

		private readonly System.Random _random = new System.Random(190719);

		private Sprite _particleSprite;

		private int _nextParticle;

		public Transform AbilityAnchor => abilityAnchor;

		public Transform BodyAnchor => bodyAnchor;

		public Transform FootAnchor => footAnchor;

		public int PoolCapacity => _particles.Length;

		public void Configure(Transform configuredAbilityAnchor, Transform configuredBodyAnchor, Transform configuredFootAnchor, Material configuredParticleMaterial, string configuredParticleResourcePath)
		{
			abilityAnchor = configuredAbilityAnchor;
			bodyAnchor = configuredBodyAnchor;
			footAnchor = configuredFootAnchor;
			particleMaterial = configuredParticleMaterial;
			particleResourcePath = configuredParticleResourcePath;
		}

		public void ConfigurePalette(Color configuredAbility1Color, Color configuredAbility2Color, Color configuredHitColor, Color configuredAppearColor, float configuredEffectScale = 1f)
		{
			ability1Color = configuredAbility1Color;
			ability2Color = configuredAbility2Color;
			hitColor = configuredHitColor;
			appearColor = configuredAppearColor;
			effectScale = Mathf.Max(0.25f, configuredEffectScale);
		}

		public void PlayAbility(int skillIndex)
		{
			if (skillIndex == 0)
			{
				Emit(abilityAnchor, ability1Color, 18, 0.52f, 1.7f, 0.09f * effectScale, 0.24f * effectScale);
			}
			else
			{
				Emit(bodyAnchor, ability2Color, 14, 0.72f, 0.48f, 0.075f * effectScale, 0.48f * effectScale);
			}
		}

		public void PlayHit()
		{
			Emit(bodyAnchor, hitColor, 9, 0.3f, 1.15f, 0.065f * effectScale, 0.2f * effectScale);
		}

		public void PlayAppear()
		{
			Emit(footAnchor, appearColor, 24, 0.8f, 0.75f, 0.08f * effectScale, 0.38f * effectScale);
		}

		private void Awake()
		{
			BuildPool();
		}

		private void Start()
		{
			PlayAppear();
		}

		private void Update()
		{
			float time = Time.time;
			for (int i = 0; i < _particles.Length; i++)
			{
				Particle particle = _particles[i];
				if (particle.Active)
				{
					float num = Mathf.InverseLerp(particle.StartTime, particle.EndTime, time);
					if (num >= 1f)
					{
						particle.Active = false;
						particle.Renderer.enabled = false;
						_particles[i] = particle;
						continue;
					}
					float num2 = time - particle.StartTime;
					Vector3 position = particle.Origin + particle.Velocity * num2 + Vector3.down * (0.7f * num2 * num2);
					particle.Transform.position = position;
					float num3 = Mathf.Sin(Mathf.Clamp01(num) * (float)Math.PI);
					Color color = particle.Color;
					color.a *= num3;
					particle.Renderer.color = color;
					float num4 = particle.Size * Mathf.Lerp(0.55f, 1.15f, num3);
					particle.Transform.localScale = new Vector3(num4, num4, num4);
					_particles[i] = particle;
				}
			}
		}

		private void BuildPool()
		{
			Texture2D texture2D = Resources.Load<Texture2D>(particleResourcePath);
			if (texture2D == null || particleMaterial == null)
			{
				Debug.LogError("Actor VFX resources are incomplete: " + particleResourcePath, this);
				return;
			}
			_particleSprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), texture2D.height);
			_particleSprite.name = base.name + "_VfxParticle";
			_particleSprite.hideFlags = HideFlags.DontSave;
			for (int i = 0; i < _particles.Length; i++)
			{
				GameObject gameObject = new GameObject($"VfxParticle_{i:00}");
				gameObject.transform.SetParent(base.transform, worldPositionStays: false);
				SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
				spriteRenderer.sprite = _particleSprite;
				spriteRenderer.sharedMaterial = particleMaterial;
				spriteRenderer.sortingOrder = 6;
				spriteRenderer.enabled = false;
				_particles[i] = new Particle
				{
					Transform = gameObject.transform,
					Renderer = spriteRenderer
				};
			}
		}

		private void Emit(Transform anchor, Color color, int count, float lifetime, float speed, float size, float horizontalRadius)
		{
			if (!(anchor == null) && !(_particleSprite == null))
			{
				for (int i = 0; i < count; i++)
				{
					int num = _nextParticle++ % _particles.Length;
					Particle particle = _particles[num];
					float f = (float)(_random.NextDouble() * Math.PI * 2.0);
					float num2 = horizontalRadius * Mathf.Sqrt((float)_random.NextDouble());
					Vector3 vector = new Vector3(Mathf.Cos(f) * num2, 0f, Mathf.Sin(f) * num2);
					Vector3 vector2 = new Vector3(Mathf.Cos(f), Mathf.Lerp(0.35f, 1f, (float)_random.NextDouble()), Mathf.Sin(f));
					particle.Active = true;
					particle.Origin = anchor.position + vector;
					particle.Velocity = vector2.normalized * speed * Mathf.Lerp(0.72f, 1.18f, (float)_random.NextDouble());
					particle.StartTime = Time.time;
					particle.EndTime = Time.time + lifetime * Mathf.Lerp(0.82f, 1.18f, (float)_random.NextDouble());
					particle.Color = color;
					particle.Size = size * Mathf.Lerp(0.72f, 1.28f, (float)_random.NextDouble());
					particle.Transform.position = particle.Origin;
					particle.Renderer.color = color;
					particle.Renderer.enabled = true;
					_particles[num] = particle;
				}
			}
		}
	}
}
