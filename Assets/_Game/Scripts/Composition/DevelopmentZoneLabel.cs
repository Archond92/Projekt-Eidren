using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class DevelopmentZoneLabel : MonoBehaviour
	{
		[SerializeField]
		private string label;

		public void Configure(string value)
		{
			label = value;
		}

		private void Awake()
		{
			if (Application.isEditor || Debug.isDebugBuild)
			{
				GameObject gameObject = new GameObject("DevelopmentLabelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
				gameObject.transform.SetParent(base.transform, worldPositionStays: false);
				Canvas component = gameObject.GetComponent<Canvas>();
				component.renderMode = RenderMode.ScreenSpaceOverlay;
				CanvasScaler component2 = gameObject.GetComponent<CanvasScaler>();
				component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				component2.referenceResolution = new Vector2(1920f, 1080f);
				GameObject gameObject2 = new GameObject("DevelopmentLabel", typeof(RectTransform), typeof(Text));
				gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
				RectTransform component3 = gameObject2.GetComponent<RectTransform>();
				component3.anchorMin = new Vector2(0.5f, 1f);
				component3.anchorMax = new Vector2(0.5f, 1f);
				component3.pivot = new Vector2(0.5f, 1f);
				component3.anchoredPosition = new Vector2(0f, -100f);
				component3.sizeDelta = new Vector2(760f, 70f);
				Text component4 = gameObject2.GetComponent<Text>();
				component4.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
				component4.fontSize = 28;
				component4.alignment = TextAnchor.MiddleCenter;
				component4.color = new Color(0.95f, 0.72f, 0.25f);
				component4.text = label + "  ·  ENTWICKLUNGSPLATZHALTER";
			}
		}
	}
}
