using Eidren.Core.Services;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class MapExitFeedback : MonoBehaviour
	{
		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private Text label;

		[SerializeField]
		private Image progressFill;

		private void Awake()
		{
			EnsureView();
			Hide();
		}

		public void Show(MapExitDirection direction, float progress)
		{
			EnsureView();
			canvasGroup.alpha = 1f;
			canvasGroup.blocksRaycasts = false;
			canvasGroup.interactable = false;
			label.text = "GEBIET VERLASSEN  ·  " + DirectionLabel(direction);
			progressFill.fillAmount = Mathf.Clamp01(progress);
		}

		public void Hide()
		{
			if (!(canvasGroup == null))
			{
				canvasGroup.alpha = 0f;
				canvasGroup.blocksRaycasts = false;
				canvasGroup.interactable = false;
				if (progressFill != null)
				{
					progressFill.fillAmount = 0f;
				}
			}
		}

		private void EnsureView()
		{
			if (!(canvasGroup != null) || !(label != null) || !(progressFill != null))
			{
				GameObject gameObject = new GameObject("MapExitCountdownCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
				gameObject.transform.SetParent(base.transform, worldPositionStays: false);
				Canvas component = gameObject.GetComponent<Canvas>();
				component.renderMode = RenderMode.ScreenSpaceOverlay;
				component.sortingOrder = 1000;
				CanvasScaler component2 = gameObject.GetComponent<CanvasScaler>();
				component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				component2.referenceResolution = new Vector2(1920f, 1080f);
				component2.matchWidthOrHeight = 1f;
				canvasGroup = gameObject.GetComponent<CanvasGroup>();
				GameObject gameObject2 = new GameObject("ExitPanel", typeof(RectTransform), typeof(Image));
				gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
				RectTransform component3 = gameObject2.GetComponent<RectTransform>();
				component3.anchorMin = new Vector2(0.5f, 0f);
				component3.anchorMax = new Vector2(0.5f, 0f);
				component3.pivot = new Vector2(0.5f, 0f);
				component3.anchoredPosition = new Vector2(0f, 90f);
				component3.sizeDelta = new Vector2(520f, 70f);
				gameObject2.GetComponent<Image>().color = new Color(0.025f, 0.06f, 0.07f, 0.88f);
				GameObject gameObject3 = new GameObject("Label", typeof(RectTransform), typeof(Text));
				gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
				RectTransform component4 = gameObject3.GetComponent<RectTransform>();
				component4.anchorMin = new Vector2(0f, 0.35f);
				component4.anchorMax = Vector2.one;
				component4.offsetMin = new Vector2(20f, 0f);
				component4.offsetMax = new Vector2(-20f, -4f);
				label = gameObject3.GetComponent<Text>();
				label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
				label.fontSize = 21;
				label.alignment = TextAnchor.MiddleCenter;
				label.color = new Color(0.9f, 0.85f, 0.64f);
				GameObject gameObject4 = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image));
				gameObject4.transform.SetParent(gameObject2.transform, worldPositionStays: false);
				RectTransform component5 = gameObject4.GetComponent<RectTransform>();
				component5.anchorMin = new Vector2(0f, 0f);
				component5.anchorMax = new Vector2(1f, 0f);
				component5.pivot = new Vector2(0.5f, 0f);
				component5.anchoredPosition = new Vector2(0f, 8f);
				component5.sizeDelta = new Vector2(-32f, 8f);
				gameObject4.GetComponent<Image>().color = new Color(0.18f, 0.24f, 0.23f, 0.9f);
				GameObject gameObject5 = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
				gameObject5.transform.SetParent(gameObject4.transform, worldPositionStays: false);
				RectTransform component6 = gameObject5.GetComponent<RectTransform>();
				component6.anchorMin = Vector2.zero;
				component6.anchorMax = Vector2.one;
				component6.offsetMin = Vector2.zero;
				component6.offsetMax = Vector2.zero;
				progressFill = gameObject5.GetComponent<Image>();
				progressFill.color = new Color(0.84f, 0.58f, 0.2f, 1f);
				progressFill.type = Image.Type.Filled;
				progressFill.fillMethod = Image.FillMethod.Horizontal;
				progressFill.fillOrigin = 0;
			}
		}

		private static string DirectionLabel(MapExitDirection direction)
		{
			if (1 == 0)
			{
			}
			string result = direction switch
			{
				MapExitDirection.North => "NORD", 
				MapExitDirection.East => "OST", 
				MapExitDirection.South => "SÜD", 
				MapExitDirection.West => "WEST", 
				_ => string.Empty, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}

	public interface IMapExitFeedback
	{
		void Show(MapExitDirection direction, float progress);

		void Hide();
	}
}
