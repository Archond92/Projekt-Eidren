using UnityEngine;

namespace Eidren.Presentation
{
	[RequireComponent(typeof(SpriteRenderer))]
	public sealed class DynamicActorGroundShadow : MonoBehaviour
	{
		[SerializeField]
		private SpriteRenderer shadowRenderer;

		[SerializeField]
		private Transform actorRoot;

		[SerializeField]
		private string shadowResource = "Art/Actors/Terrock/terrock_ground_shadow";

		[SerializeField]
		private Vector2 baseSize = new Vector2(1.22f, 0.48f);

		[SerializeField]
		private float baseOpacity = 0.38f;

		[SerializeField]
		private float lightOffset = 0.18f;

		[SerializeField]
		private float groundLift = 0.018f;

		private Light _mainLight;

		private Vector3 _poseScale = Vector3.one;

		private float _poseOpacity = 1f;

		public SpriteRenderer ShadowRenderer => shadowRenderer;

		public void Configure(SpriteRenderer renderer, Transform configuredActorRoot, string configuredResource, float configuredWidth = 1.22f, float configuredDepth = 0.48f, float configuredOpacity = 0.38f, float configuredLightOffset = 0.18f)
		{
			shadowRenderer = renderer;
			actorRoot = configuredActorRoot;
			shadowResource = configuredResource;
			baseSize = new Vector2(Mathf.Max(0.1f, configuredWidth), Mathf.Max(0.1f, configuredDepth));
			baseOpacity = Mathf.Clamp01(configuredOpacity);
			lightOffset = Mathf.Max(0f, configuredLightOffset);
		}

		public void SetPose(ActorVisualState state, float normalizedTime = 0f)
		{
			switch (state)
			{
			case ActorVisualState.Move:
				_poseScale = new Vector3(1.08f, 0.9f, 1f);
				_poseOpacity = 0.86f;
				break;
			case ActorVisualState.Ability1:
				_poseScale = new Vector3(1.18f, 1.08f, 1f);
				_poseOpacity = 1.08f;
				break;
			case ActorVisualState.Death:
				_poseScale = new Vector3(1.28f, 0.92f, 1f);
				_poseOpacity = 0.62f;
				break;
			default:
				_poseScale = Vector3.one;
				_poseOpacity = 1f;
				break;
			}
		}

		private void Awake()
		{
			if (shadowRenderer == null)
			{
				shadowRenderer = GetComponent<SpriteRenderer>();
			}
			if (actorRoot == null)
			{
				actorRoot = base.transform.parent;
			}
			Texture2D texture2D = Resources.Load<Texture2D>(shadowResource);
			if (texture2D != null && shadowRenderer.sprite == null)
			{
				// G-003: pixelsPerUnit = Texturbreite normiert den Sprite auf
				// 1 Welteinheit Breite; die Weltgroesse setzt allein LateUpdate
				// ueber localScale = baseSize * Pose. Vorher normierte schon
				// dieser Aufruf auf baseSize.x und localScale multiplizierte
				// baseSize ein zweites Mal - der Schatten war ein Streifen von
				// 1,49 x 0,20 statt der 1,22 x 0,48 aus baseSize.
				shadowRenderer.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), texture2D.width, 0u, SpriteMeshType.FullRect);
			}
			// G-003: Alpha allein aus baseOpacity. Der Farbton kommt aus _Color
			// des Materials (der Shader ignoriert die RGB-Vertexfarbe); die
			// Materialien tragen seit G-003 Alpha 1, damit die Deckkraft nicht
			// doppelt multipliziert - Material 0,34-0,46 mal Komponente 0,38
			// ergab eine Spitzendeckung von unter 7 Prozent, der Schatten war
			// in jedem Capture unsichtbar.
			shadowRenderer.color = new Color(1f, 1f, 1f, baseOpacity);
		}

		private void LateUpdate()
		{
			if (!(actorRoot == null) && !(shadowRenderer == null))
			{
				if (_mainLight == null)
				{
					_mainLight = FindMainLight();
				}
				Vector3 vector = ((_mainLight != null) ? (-_mainLight.transform.forward) : new Vector3(-0.45f, 1f, -0.35f).normalized);
				Vector3 vector2 = new Vector3(0f - vector.x, 0f, 0f - vector.z);
				if (vector2.sqrMagnitude < 0.001f)
				{
					vector2 = Vector3.back;
				}
				vector2.Normalize();
				float num = Mathf.Clamp01(1f - Mathf.Abs(vector.y));
				base.transform.position = actorRoot.position + vector2 * (lightOffset * (0.35f + num)) + Vector3.up * groundLift;
				base.transform.rotation = Quaternion.LookRotation(Vector3.up, vector2);
				// G-003: baseSize ist die Weltgrundflaeche. Der Sprite wird auf
				// seine tatsaechliche lokale Groesse normiert, damit der Vertrag
				// unabhaengig von pixelsPerUnit und Seitenverhaeltnis der
				// Schattentextur gilt.
				Vector3 spriteSize = ((shadowRenderer.sprite != null) ? shadowRenderer.sprite.bounds.size : Vector3.one);
				base.transform.localScale = Vector3.Scale(new Vector3(baseSize.x / Mathf.Max(0.01f, spriteSize.x), baseSize.y / Mathf.Max(0.01f, spriteSize.y), 1f), _poseScale);
				Color color = shadowRenderer.color;
				color.a = Mathf.Clamp01(baseOpacity * _poseOpacity * (1f - num * 0.28f));
				shadowRenderer.color = color;
			}
		}

		private static Light FindMainLight()
		{
			Light[] array = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			Light[] array2 = array;
			foreach (Light light in array2)
			{
				if (light.type == LightType.Directional && light.enabled && light.gameObject.activeInHierarchy)
				{
					return light;
				}
			}
			return null;
		}
	}
}
