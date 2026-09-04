using System.Collections.Generic;
using System;
using UnityEngine.Rendering;
using UnityEngine;

namespace Eidren.Presentation
{
	[RequireComponent(typeof(SpriteRenderer))]
	public sealed class SpriteActorPresentation : MonoBehaviour, IActorPresentation, ILocomotionPresentation, IAuthoredStatePresentation, IArmorPresentation
	{
		private sealed class AtlasSet
		{
			public Sprite[] Sprites { get; }

			public Texture2D Normal { get; }

			public Texture2D Emission { get; }

			public int FrameCount { get; }

			public AtlasSet(Sprite[] sprites, Texture2D normal, Texture2D emission, int frameCount)
			{
				Sprites = sprites;
				Normal = normal;
				Emission = emission;
				FrameCount = frameCount;
			}
		}

		private const int Columns = 4;

		private const int Rows = 2;

		private const int DirectionCount = 8;

		private static readonly Dictionary<string, AtlasSet> AtlasCache = new Dictionary<string, AtlasSet>();

		[SerializeField]
		private SpriteRenderer targetRenderer;

		[SerializeField]
		private DynamicActorGroundShadow groundShadow;

		[SerializeField]
		private string actorId = "terrock";

		[SerializeField]
		private string resourceRoot = "Art/Actors/Terrock";

		[SerializeField]
		private float worldHeight = 1f;

		[SerializeField]
		private int runtimeCellWidth = 256;

		[SerializeField]
		private int runtimeCellHeight = 205;

		[SerializeField]
		private float visibleHeightPixels = 152f;

		[SerializeField]
		private float pivotY = 9f / 128f;

		[SerializeField]
		private float outlinePixels = 1.25f;

		[SerializeField]
		private float alphaClip = 0.06f;

		[SerializeField]
		private float emissionStrength = 1.15f;

		[SerializeField]
		[Range(12f, 18f)]
		private float framesPerSecond = 14f;

		[SerializeField]
		[Range(0f, 0.08f)]
		private float locomotionStride = 0.035f;

		[SerializeField]
		[Range(2f, 12f)]
		private float locomotionResponse = 9f;

		[SerializeField]
		private string ability1Stem = "rockbreaker";

		[SerializeField]
		private string ability2Stem = "stonehide";

		[SerializeField]
		private string hitStem = "hit";

		[SerializeField]
		private string appearStem = "appear";

		[SerializeField]
		private string deathStem = "death";

		[SerializeField]
		private string[] prewarmStateStems = Array.Empty<string>();

		private MaterialPropertyBlock _properties;

		private AtlasSet _atlas;

		private Camera _camera;

		private Color _tint = Color.white;

		private float _stateStartedAt;

		private int _frameIndex;

		private string _currentStem = "idle";

		private bool _loopCurrentState = true;

		private Vector3 _baseLocalPosition;

		private Vector3 _locomotionDirection;

		private float _locomotionAmount;

		private Texture2D _armorAlbedo;

		private Texture2D _armorNormal;

		private Texture2D _armorEmission;

		private Vector4 _armorParts;

		private Vector4 _armorTiers;

		public ActorFacing8 Facing { get; private set; } = ActorFacing8.S;

		public ActorVisualState VisualState { get; private set; } = ActorVisualState.Idle;

		public SpriteRenderer TargetRenderer => targetRenderer;

		public string ActorId => actorId;

		public float WorldHeight => worldHeight;

		private void LoadAtlas(string stem)
		{
			_atlas = ResolveAtlas(stem);
			if (actorId.StartsWith("player_", StringComparison.Ordinal))
			{
				string text = ResolveAuthoredStem(stem);
				string text2 = resourceRoot + "/player_wanderer_" + text;
				_armorAlbedo = Resources.Load<Texture2D>(text2 + "_albedo");
				_armorNormal = Resources.Load<Texture2D>(text2 + "_normal");
				_armorEmission = Resources.Load<Texture2D>(text2 + "_emission");
			}
		}

		private AtlasSet ResolveAtlas(string stem)
		{
			string key = $"{resourceRoot}/{actorId}_{stem}:{worldHeight:0.###}:" + $"{runtimeCellWidth}x{runtimeCellHeight}:" + $"{visibleHeightPixels:0.###}:{pivotY:0.####}";
			if (!AtlasCache.TryGetValue(key, out var value))
			{
				value = BuildAtlas(stem);
				if (value != null)
				{
					AtlasCache[key] = value;
				}
			}
			return value;
		}

		private void PrewarmConfiguredAtlases()
		{
			if (prewarmStateStems == null)
			{
				return;
			}
			string[] array = prewarmStateStems;
			foreach (string text in array)
			{
				if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, _currentStem, StringComparison.Ordinal))
				{
					ResolveAtlas(text);
				}
			}
		}

		private AtlasSet BuildAtlas(string stem)
		{
			string text = ResolveAuthoredStem(stem);
			string text2 = resourceRoot + "/" + actorId + "_" + text;
			Texture2D texture2D = Resources.Load<Texture2D>(text2 + "_albedo");
			Texture2D texture2D2 = Resources.Load<Texture2D>(text2 + "_normal");
			Texture2D texture2D3 = Resources.Load<Texture2D>(text2 + "_emission");
			if (texture2D == null || texture2D2 == null || texture2D3 == null)
			{
				Debug.LogError("Actor atlas set is incomplete: " + text2, this);
				return null;
			}
			if (texture2D2.width != texture2D.width || texture2D2.height != texture2D.height || texture2D3.width != texture2D.width || texture2D3.height != texture2D.height)
			{
				Debug.LogError("Actor atlas dimensions are invalid: " + text2, this);
				return null;
			}
			float num = (float)texture2D.width / 4f;
			float num2 = num * (float)runtimeCellHeight / Mathf.Max(1f, runtimeCellWidth);
			int num3 = Mathf.RoundToInt((float)texture2D.height / Mathf.Max(1f, num2 * 2f));
			if (num3 <= 0)
			{
				Debug.LogError("Actor atlas frame grid is invalid: " + text2, this);
				return null;
			}
			int num4 = num3 * 2;
			float num5 = (float)texture2D.height / (float)num4;
			float num6 = num5 / Mathf.Max(1f, runtimeCellHeight);
			float num7 = visibleHeightPixels * num6;
			float pixelsPerUnit = num7 / Mathf.Max(0.1f, worldHeight);
			Sprite[] array = new Sprite[8 * num3];
			for (int i = 0; i < num3; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					int num8 = j / 4;
					int num9 = j % 4;
					int num10 = i * 2 + num8;
					int num11 = Mathf.RoundToInt((float)(texture2D.width * num9) / 4f);
					int num12 = Mathf.RoundToInt((float)(texture2D.width * (num9 + 1)) / 4f);
					int num13 = Mathf.RoundToInt((float)(texture2D.height * (num4 - num10 - 1)) / (float)num4);
					int num14 = Mathf.RoundToInt((float)(texture2D.height * (num4 - num10)) / (float)num4);
					Rect rect = new Rect(num11, num13, num12 - num11, num14 - num13);
					Sprite sprite = Sprite.Create(texture2D, rect, new Vector2(0.5f, pivotY), pixelsPerUnit, 0u, SpriteMeshType.FullRect);
					sprite.name = $"{actorId}_{stem}_{(ActorFacing8)j}_" + $"{i:00}";
					sprite.hideFlags = HideFlags.DontSave;
					array[i * 8 + j] = sprite;
				}
			}
			return new AtlasSet(array, texture2D2, texture2D3, num3);
		}

		private static string ResolveAuthoredStem(string stem)
		{
			if (stem != null && stem.StartsWith("spear", StringComparison.Ordinal))
			{
				return "hammer" + stem.Substring("spear".Length);
			}
			return stem;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetCache()
		{
			AtlasCache.Clear();
		}

		public void Configure(SpriteRenderer renderer, DynamicActorGroundShadow shadow, string configuredActorId, string configuredResourceRoot, float configuredWorldHeight, int configuredCellWidth = 256, int configuredCellHeight = 205, float configuredVisibleHeightPixels = 152f, float configuredPivotY = 9f / 128f)
		{
			targetRenderer = renderer;
			groundShadow = shadow;
			actorId = configuredActorId;
			resourceRoot = configuredResourceRoot;
			worldHeight = Mathf.Max(0.1f, configuredWorldHeight);
			runtimeCellWidth = Mathf.Max(1, configuredCellWidth);
			runtimeCellHeight = Mathf.Max(1, configuredCellHeight);
			visibleHeightPixels = Mathf.Max(1f, configuredVisibleHeightPixels);
			pivotY = Mathf.Clamp01(configuredPivotY);
		}

		public void SetFacing(ActorFacing8 facing)
		{
			if (Facing != facing || !(targetRenderer != null) || !(targetRenderer.sprite != null))
			{
				Facing = facing;
				ApplyFrame();
			}
		}

		public void SetVisualState(ActorVisualState state, float normalizedTime = 0f)
		{
			bool restart = VisualState != state;
			VisualState = state;
			string stem = StateStem(state);
			bool loop = state == ActorVisualState.Idle || state == ActorVisualState.Move;
			SetAuthoredState(stem, normalizedTime, loop, restart);
			groundShadow?.SetPose(state, normalizedTime);
		}

		public void SetAuthoredState(string stem, float normalizedTime = 0f, bool loop = false, bool restart = false)
		{
			if (string.IsNullOrWhiteSpace(stem))
			{
				stem = "idle";
			}
			bool flag = restart || _atlas == null || !string.Equals(_currentStem, stem, StringComparison.Ordinal);
			_loopCurrentState = loop;
			if (flag)
			{
				_currentStem = stem;
				_stateStartedAt = Time.time;
				_frameIndex = -1;
				LoadAtlas(stem);
			}
			if (flag || normalizedTime > 0f)
			{
				SetFrameFromNormalized(normalizedTime);
			}
		}

		public void SetAuthoredTimedState(string stem, float durationSeconds)
		{
			// 2D kennt keine Zeitskalierung: Atlas-Timing bleibt massgeblich.
			SetAuthoredState(stem, 0f, loop: false, restart: true);
		}

		public void SetAssetVariant(string configuredActorId)
		{
			if (!string.IsNullOrWhiteSpace(configuredActorId) && !string.Equals(actorId, configuredActorId, StringComparison.Ordinal))
			{
				actorId = configuredActorId;
				_atlas = null;
				_frameIndex = -1;
				LoadAtlas(_currentStem);
				ApplyFrame();
			}
		}

		public void SetArmorParts(bool head, bool chest, bool hands, bool legs)
		{
			_armorParts = new Vector4(head ? 1f : 0f, chest ? 1f : 0f, hands ? 1f : 0f, legs ? 1f : 0f);
			ApplyMaterialProperties();
		}

		public void SetArmorTiers(int head, int chest, int hands, int legs)
		{
			_armorTiers = new Vector4(Mathf.Clamp(head, 0, 3), Mathf.Clamp(chest, 0, 3), Mathf.Clamp(hands, 0, 3), Mathf.Clamp(legs, 0, 3));
			SetArmorParts(head > 0, chest > 0, hands > 0, legs > 0);
		}

		public void ConfigureStateStems(string configuredAbility1Stem, string configuredAbility2Stem, string configuredHitStem = "hit", string configuredAppearStem = "appear", string configuredDeathStem = "death")
		{
			ability1Stem = configuredAbility1Stem;
			ability2Stem = configuredAbility2Stem;
			hitStem = configuredHitStem;
			appearStem = configuredAppearStem;
			deathStem = configuredDeathStem;
		}

		public void ConfigurePrewarmStateStems(params string[] stems)
		{
			prewarmStateStems = ((stems != null) ? ((string[])stems.Clone()) : Array.Empty<string>());
		}

		public void SetTint(Color color)
		{
			_tint = color;
			ApplyMaterialProperties();
		}

		public void SetLocomotion(Vector3 worldDirection, bool moving)
		{
			worldDirection.y = 0f;
			if (moving && worldDirection.sqrMagnitude > 0.0001f)
			{
				_locomotionDirection = ((base.transform.parent != null) ? base.transform.parent.InverseTransformDirection(worldDirection.normalized) : worldDirection.normalized);
				_locomotionDirection.y = 0f;
			}
			_locomotionAmount = (moving ? 1f : 0f);
		}

		private void Awake()
		{
			if (targetRenderer == null)
			{
				targetRenderer = GetComponent<SpriteRenderer>();
			}
			targetRenderer.shadowCastingMode = ShadowCastingMode.Off;
			targetRenderer.receiveShadows = false;
			_baseLocalPosition = base.transform.localPosition;
			_currentStem = StateStem(VisualState);
			_loopCurrentState = VisualState == ActorVisualState.Idle || VisualState == ActorVisualState.Move;
			LoadAtlas(_currentStem);
			PrewarmConfiguredAtlases();
			_stateStartedAt = Time.time;
			ApplyFrame();
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
			ApplyLocomotionMotion();
			if (_camera == null)
			{
				_camera = Camera.main;
			}
			if (_camera != null)
			{
				base.transform.rotation = _camera.transform.rotation;
			}
			AdvanceUnforcedAnimation();
		}

		private void ApplyLocomotionMotion()
		{
			float locomotionAmount = _locomotionAmount;
			float t = 1f - Mathf.Exp((0f - Mathf.Max(0.01f, locomotionResponse)) * Time.deltaTime);
			float num = ((locomotionAmount > 0f) ? (0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.time * framesPerSecond * (float)Math.PI))) : 0f);
			Vector3 vector = _locomotionDirection * (locomotionStride * worldHeight * num * locomotionAmount);
			base.transform.localPosition = Vector3.Lerp(base.transform.localPosition, _baseLocalPosition + vector, t);
		}

		private void ApplyFrame()
		{
			if (!(targetRenderer == null) && _atlas != null)
			{
				int num = Mathf.Clamp(_frameIndex, 0, _atlas.FrameCount - 1);
				targetRenderer.sprite = _atlas.Sprites[(int)(num * 8 + Facing)];
				ApplyMaterialProperties();
			}
		}

		private void SetFrameFromNormalized(float normalizedTime)
		{
			if (_atlas != null)
			{
				float num = Mathf.Clamp01(normalizedTime);
				int num2 = Mathf.Min(_atlas.FrameCount - 1, Mathf.FloorToInt(num * (float)_atlas.FrameCount));
				if (_frameIndex != num2 || !(targetRenderer.sprite != null))
				{
					_frameIndex = num2;
					ApplyFrame();
				}
			}
		}

		private void AdvanceUnforcedAnimation()
		{
			if (_atlas != null && _atlas.FrameCount > 1)
			{
				float num = Mathf.Max(0f, Time.time - _stateStartedAt);
				int num2 = ((!_loopCurrentState) ? Mathf.Min(_atlas.FrameCount - 1, Mathf.FloorToInt(num * framesPerSecond)) : (Mathf.FloorToInt(num * framesPerSecond) % _atlas.FrameCount));
				if (num2 != _frameIndex)
				{
					_frameIndex = num2;
					ApplyFrame();
				}
			}
		}

		private void ApplyMaterialProperties()
		{
			if (!(targetRenderer == null) && _atlas != null)
			{
				if (_properties == null)
				{
					_properties = new MaterialPropertyBlock();
				}
				targetRenderer.GetPropertyBlock(_properties);
				_properties.SetTexture("_NormalMap", _atlas.Normal);
				_properties.SetTexture("_EmissionMap", _atlas.Emission);
				if (_armorAlbedo != null)
				{
					_properties.SetTexture("_ArmorTex", _armorAlbedo);
				}
				if (_armorNormal != null)
				{
					_properties.SetTexture("_ArmorNormalMap", _armorNormal);
				}
				if (_armorEmission != null)
				{
					_properties.SetTexture("_ArmorEmissionMap", _armorEmission);
				}
				_properties.SetVector("_ArmorParts", _armorParts);
				_properties.SetVector("_ArmorTiers", _armorTiers);
				Sprite sprite = targetRenderer.sprite;
				if (sprite != null && sprite.texture != null)
				{
					Rect textureRect = sprite.textureRect;
					_properties.SetVector("_SpriteUvRect", new Vector4(textureRect.x / (float)sprite.texture.width, textureRect.y / (float)sprite.texture.height, textureRect.width / (float)sprite.texture.width, textureRect.height / (float)sprite.texture.height));
				}
				_properties.SetColor("_Color", _tint);
				_properties.SetFloat("_OutlinePixels", outlinePixels);
				_properties.SetFloat("_AlphaClip", alphaClip);
				_properties.SetFloat("_EmissionStrength", emissionStrength);
				targetRenderer.SetPropertyBlock(_properties);
			}
		}

		private string StateStem(ActorVisualState state)
		{
			if (1 == 0)
			{
			}
			string result = state switch
			{
				ActorVisualState.Move => "move", 
				ActorVisualState.Ability1 => ability1Stem, 
				ActorVisualState.Ability2 => ability2Stem, 
				ActorVisualState.Hit => hitStem, 
				ActorVisualState.Stagger => hitStem, 
				ActorVisualState.Death => deathStem, 
				ActorVisualState.Appear => appearStem, 
				_ => "idle", 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}
}
