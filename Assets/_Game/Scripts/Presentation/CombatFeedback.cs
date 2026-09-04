using System.Collections.Generic;
using System;
using UnityEngine.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Presentation
{
	public static class CombatFeedback
	{
		private static Camera _billboardCamera;

		private static readonly Stack<GameObject> TextPool = new Stack<GameObject>();

		private static readonly Stack<GameObject> ImpactPool = new Stack<GameObject>();

		private static readonly Stack<GameObject> AuraPool = new Stack<GameObject>();

		private static Transform _poolRoot;

		private const float ImpactWorldHeight = 1.7f;

		private const float ImpactPivotY = 0.5f;

		internal static Camera BillboardCamera
		{
			get
			{
				if (_billboardCamera == null)
				{
					_billboardCamera = Camera.main;
				}
				return _billboardCamera;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			TextPool.Clear();
			ImpactPool.Clear();
			AuraPool.Clear();
			_billboardCamera = null;
			_poolRoot = null;
		}

		public static void SpawnDamage(Vector3 position, float healthDamage, float staggerDamage, bool backAttack = false)
		{
			string text = ((healthDamage >= 1.5f) ? $"-{healthDamage:0}" : "");
			if (backAttack)
			{
				text = "RÜCKEN!  " + text;
			}
			if (staggerDamage >= 1f)
			{
				text += string.Format("{0}STAGGER +{1:0}", (text.Length > 0) ? "\n" : "", staggerDamage);
			}
			if (text.Length != 0)
			{
				Color color = (backAttack ? new Color(0.82f, 0.34f, 1f) : ((healthDamage >= 1.5f) ? new Color(1f, 0.78f, 0.25f) : new Color(0.96f, 0.56f, 0.16f)));
				SpawnText(position, text, color, 1.15f);
				SpawnImpact(position - Vector3.up * 0.35f, color);
			}
		}

		public static void SpawnPlayerDamage(Vector3 position, float amount)
		{
			SpawnText(position, $"-{amount:0} HP", new Color(1f, 0.25f, 0.18f), 1.05f);
			SpawnImpact(position - Vector3.up * 0.5f, new Color(1f, 0.25f, 0.18f));
		}

		public static void SpawnStatusText(Vector3 position, string value, Color color, float duration = 1.2f)
		{
			SpawnText(position, value, color, duration);
		}

		public static void SpawnImpact(Vector3 position, Color color)
		{
			SpawnImpact(position, color, null);
		}

		private static void SpawnImpact(Vector3 position, Color color, StyleCue? cue)
		{
			GameObject gameObject = Acquire(ImpactPool, CreateImpact);
			gameObject.transform.position = position;
			Sprite authoredSprite = (cue.HasValue ? StyleProofVisualLibrary.GetCueSprite(cue.Value) : null);
			gameObject.GetComponent<WorldImpactBurst>().Activate(color, authoredSprite);
		}

		public static void SpawnStyleCue(Vector3 position, StyleCue cue, string label = "")
		{
			Color color = cue switch
			{
				StyleCue.HammerHit => new Color(1f, 0.62f, 0.2f), 
				StyleCue.DaggerHit => new Color(0.72f, 0.88f, 1f), 
				StyleCue.SpearHit => new Color(0.42f, 0.92f, 0.78f), 
				StyleCue.EnemyHit => new Color(0.95f, 0.24f, 0.18f), 
				StyleCue.Stagger => new Color(1f, 0.78f, 0.22f), 
				StyleCue.ItemPickup => new Color(0.95f, 0.72f, 0.24f), 
				StyleCue.ResourceHarvest => new Color(0.46f, 0.86f, 0.42f), 
				StyleCue.Healing => new Color(0.2f, 0.95f, 0.62f), 
				StyleCue.EidraEnergy => new Color(0.2f, 0.82f, 1f), 
				_ => Color.white, 
			};
			SpawnImpact(position, color, cue);
			if (!string.IsNullOrWhiteSpace(label))
			{
				SpawnStatusText(position + Vector3.up * 0.9f, label, color, 0.85f);
			}
		}

		public static GameObject SpawnAura(Transform target, Color color, float duration, string label, float radius, float labelHeight)
		{
			GameObject gameObject = Acquire(AuraPool, CreateAura);
			gameObject.name = "Aura_" + label;
			gameObject.GetComponent<TimedWorldAura>().Activate(target, color, duration, label, radius, labelHeight);
			return gameObject;
		}

		public static void Release(GameObject effect)
		{
			if (!(effect == null) && effect.activeSelf)
			{
				FloatingCombatText component;
				WorldImpactBurst component2;
				TimedWorldAura component3;
				if (!Application.isPlaying)
				{
					UnityEngine.Object.DestroyImmediate(effect);
				}
				else if (effect.TryGetComponent<FloatingCombatText>(out component))
				{
					component.ResetForPool();
					Return(effect, TextPool);
				}
				else if (effect.TryGetComponent<WorldImpactBurst>(out component2))
				{
					component2.ResetForPool();
					Return(effect, ImpactPool);
				}
				else if (effect.TryGetComponent<TimedWorldAura>(out component3))
				{
					component3.ResetForPool();
					Return(effect, AuraPool);
				}
				else
				{
					UnityEngine.Object.Destroy(effect);
				}
			}
		}

		private static void SpawnText(Vector3 position, string value, Color color, float duration)
		{
			GameObject gameObject = Acquire(TextPool, CreateText);
			gameObject.transform.position = position;
			TextMesh component = gameObject.GetComponent<TextMesh>();
			component.text = value;
			component.color = color;
			gameObject.GetComponent<FloatingCombatText>().Activate(component, duration);
		}

		private static GameObject Acquire(Stack<GameObject> pool, Func<GameObject> factory)
		{
			GameObject gameObject = null;
			while (pool.Count > 0 && gameObject == null)
			{
				gameObject = pool.Pop();
			}
			if (gameObject == null)
			{
				gameObject = factory();
			}
			gameObject.transform.SetParent(null, worldPositionStays: false);
			gameObject.SetActive(value: true);
			return gameObject;
		}

		private static void Return(GameObject effect, Stack<GameObject> pool)
		{
			effect.SetActive(value: false);
			effect.transform.SetParent(GetPoolRoot(), worldPositionStays: false);
			pool.Push(effect);
		}

		private static Transform GetPoolRoot()
		{
			if (_poolRoot != null)
			{
				return _poolRoot;
			}
			GameObject gameObject = new GameObject("[CombatFeedbackPool]");
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			_poolRoot = gameObject.transform;
			return _poolRoot;
		}

		private static GameObject CreateText()
		{
			GameObject gameObject = new GameObject("Combat_Text");
			TextMesh textMesh = gameObject.AddComponent<TextMesh>();
			textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			textMesh.fontSize = 72;
			textMesh.fontStyle = FontStyle.Bold;
			textMesh.characterSize = 0.052f;
			textMesh.anchor = TextAnchor.MiddleCenter;
			textMesh.alignment = TextAlignment.Center;
			MeshRenderer component = gameObject.GetComponent<MeshRenderer>();
			component.sortingOrder = 100;
			component.shadowCastingMode = ShadowCastingMode.Off;
			component.receiveShadows = false;
			gameObject.AddComponent<FloatingCombatText>();
			return gameObject;
		}

		private static GameObject CreateImpact()
		{
			GameObject gameObject = new GameObject("Hit_Impact");
			RuntimeSpriteVisual runtimeSpriteVisual = gameObject.AddComponent<RuntimeSpriteVisual>();
			if (!runtimeSpriteVisual.SetArt("Art/UI/ui_hit", 1.7f, 80, 0.5f))
			{
				Debug.LogWarning("[CombatFeedback] Impact art could not be loaded.");
			}
			gameObject.AddComponent<WorldImpactBurst>().Configure(runtimeSpriteVisual);
			return gameObject;
		}

		private static GameObject CreateAura()
		{
			GameObject gameObject = new GameObject("Aura");
			gameObject.AddComponent<TimedWorldAura>().Configure();
			return gameObject;
		}

		internal static Material CreateLineMaterial(Color color)
		{
			Material material = Resources.Load<Material>("EidrenRuntimeMaterial");
			Shader shader = ((material != null) ? material.shader : Shader.Find("Universal Render Pipeline/Unlit"));
			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}
			Material obj = ((material != null) ? new Material(material) : new Material(shader));
			obj.color = color;
			obj.hideFlags = HideFlags.DontSave;
			return obj;
		}
	}

	public sealed class FloatingCombatText : MonoBehaviour
	{
		private TextMesh _text;

		private Color _color;

		private float _duration;

		private float _startedAt;

		public void Activate(TextMesh text, float duration)
		{
			_text = text;
			_color = text.color;
			_duration = Mathf.Max(0.4f, duration);
			_startedAt = Time.time;
			base.transform.localScale = Vector3.one * 0.45f;
		}

		internal void ResetForPool()
		{
			if (_text != null)
			{
				_text.text = string.Empty;
				_text.color = Color.white;
			}
			base.transform.localScale = Vector3.one;
		}

		private void Update()
		{
			float num = Mathf.Clamp01((Time.time - _startedAt) / _duration);
			base.transform.position += Vector3.up * (1.15f * Time.deltaTime);
			Camera billboardCamera = CombatFeedback.BillboardCamera;
			if (billboardCamera != null)
			{
				base.transform.rotation = billboardCamera.transform.rotation;
			}
			float num2 = ((num < 0.2f) ? Mathf.Lerp(0.45f, 1.18f, num / 0.2f) : Mathf.Lerp(1.18f, 0.92f, (num - 0.2f) / 0.8f));
			base.transform.localScale = Vector3.one * num2;
			Color color = _color;
			color.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, num));
			_text.color = color;
			if (num >= 1f)
			{
				CombatFeedback.Release(base.gameObject);
			}
		}
	}

	public enum StyleCue
	{
		HammerHit = 0,
		DaggerHit = 1,
		SpearHit = 2,
		EnemyHit = 3,
		Stagger = 4,
		ItemPickup = 5,
		ResourceHarvest = 6,
		Healing = 7,
		EidraEnergy = 8
	}

	public sealed class TimedWorldAura : MonoBehaviour
	{
		private Transform _target;

		private TextMesh _label;

		private Color _color;

		private string _labelValue;

		private float _duration;

		private float _startedAt;

		private LineRenderer _ring;

		private int _lastDisplayedTenth = -1;

		public void Configure()
		{
			_ring = base.gameObject.AddComponent<LineRenderer>();
			_ring.useWorldSpace = false;
			_ring.loop = true;
			_ring.positionCount = 64;
			_ring.widthMultiplier = 0.15f;
			_ring.numCornerVertices = 3;
			_ring.material = CombatFeedback.CreateLineMaterial(Color.white);
			GameObject gameObject = new GameObject("Aura_Label");
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			_label = gameObject.AddComponent<TextMesh>();
			_label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			_label.fontSize = 52;
			_label.fontStyle = FontStyle.Bold;
			_label.characterSize = 0.045f;
			_label.anchor = TextAnchor.MiddleCenter;
			_label.alignment = TextAlignment.Center;
			_label.GetComponent<MeshRenderer>().sortingOrder = 95;
		}

		public void Activate(Transform target, Color color, float duration, string label, float radius, float labelHeight)
		{
			_target = target;
			_color = color;
			_duration = duration;
			_labelValue = label;
			_startedAt = Time.time;
			_lastDisplayedTenth = -1;
			_ring.startColor = color;
			_ring.endColor = color;
			_ring.sharedMaterial.color = color;
			for (int i = 0; i < _ring.positionCount; i++)
			{
				float f = (float)i / (float)_ring.positionCount * (float)Math.PI * 2f;
				_ring.SetPosition(i, new Vector3(Mathf.Cos(f) * radius, 0.12f, Mathf.Sin(f) * radius));
			}
			_label.transform.localPosition = Vector3.up * labelHeight;
			_label.color = color;
		}

		internal void ResetForPool()
		{
			_target = null;
			_labelValue = string.Empty;
			_lastDisplayedTenth = -1;
			if (_label != null)
			{
				_label.text = string.Empty;
				_label.color = Color.white;
			}
		}

		private void LateUpdate()
		{
			if (_target == null)
			{
				CombatFeedback.Release(base.gameObject);
				return;
			}
			base.transform.position = _target.position;
			float num = Mathf.Max(0f, _duration - (Time.time - _startedAt));
			int num2 = Mathf.CeilToInt(num * 10f);
			if (num2 != _lastDisplayedTenth)
			{
				_lastDisplayedTenth = num2;
				_label.text = $"{_labelValue}\n{(float)num2 * 0.1f:0.0}s";
			}
			Camera billboardCamera = CombatFeedback.BillboardCamera;
			if (billboardCamera != null)
			{
				_label.transform.rotation = billboardCamera.transform.rotation;
			}
			float num3 = 0.82f + Mathf.PingPong(Time.time * 1.6f, 0.18f);
			Color color = _color;
			color.a *= num3;
			_label.color = color;
			if (num <= 0f)
			{
				CombatFeedback.Release(base.gameObject);
			}
		}
	}

	public sealed class WorldImpactBurst : MonoBehaviour
	{
		private RuntimeSpriteVisual _visual;

		private Sprite _defaultSprite;

		private Color _color;

		private bool _useAuthoredColor;

		private float _startedAt;

		public void Configure(RuntimeSpriteVisual visual)
		{
			_visual = visual;
			_defaultSprite = ((visual != null && visual.SpriteRenderer != null) ? visual.SpriteRenderer.sprite : null);
		}

		public void Activate(Color color, Sprite authoredSprite = null)
		{
			_color = color;
			_useAuthoredColor = authoredSprite != null;
			if (_visual != null)
			{
				if (authoredSprite != null)
				{
					_visual.SetSprite(authoredSprite, 80);
				}
				else if (_defaultSprite != null)
				{
					_visual.SetSprite(_defaultSprite, 80);
				}
				_visual.SetTint((authoredSprite != null) ? Color.white : color);
			}
			_startedAt = Time.time;
			base.transform.localScale = Vector3.one * 0.2f;
		}

		internal void ResetForPool()
		{
			if (_visual != null)
			{
				_visual.SetTint(Color.white);
			}
			base.transform.localScale = Vector3.one;
		}

		private void Update()
		{
			float num = Mathf.Clamp01((Time.time - _startedAt) / 0.34f);
			base.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.45f, num);
			Color tint = (_useAuthoredColor ? Color.white : _color);
			tint.a = 1f - num;
			_visual.SetTint(tint);
			if (num >= 1f)
			{
				CombatFeedback.Release(base.gameObject);
			}
		}
	}
}
