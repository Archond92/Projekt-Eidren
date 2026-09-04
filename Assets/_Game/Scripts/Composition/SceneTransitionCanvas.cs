using Eidren.Core.Services;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class SceneTransitionCanvas : MonoBehaviour, ISceneTransitionView
	{
		[SerializeField]
		private float fadeDuration = 0.28f;

		[SerializeField]
		private Color fadeColor = new Color(0.025f, 0.04f, 0.055f, 1f);

		private Canvas _canvas;

		private CanvasGroup _canvasGroup;

		private void Awake()
		{
			BuildCanvas();
			SetInputBlocked(blocked: false);
		}

		public void SetInputBlocked(bool blocked)
		{
			BuildCanvas();
			_canvas.enabled = blocked;
			_canvasGroup.blocksRaycasts = blocked;
			_canvasGroup.interactable = blocked;
			if (!blocked)
			{
				_canvasGroup.alpha = 0f;
			}
		}

		public IEnumerator FadeOut()
		{
			BuildCanvas();
			_canvas.enabled = true;
			yield return Fade(0f, 1f);
		}

		public IEnumerator FadeIn()
		{
			BuildCanvas();
			yield return Fade(1f, 0f);
		}

		private IEnumerator Fade(float from, float to)
		{
			float duration = Mathf.Max(0f, fadeDuration);
			if (duration <= 0f)
			{
				_canvasGroup.alpha = to;
				yield break;
			}
			float elapsed = 0f;
			_canvasGroup.alpha = from;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				_canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
				yield return null;
			}
			_canvasGroup.alpha = to;
		}

		private void BuildCanvas()
		{
			if (!(_canvas != null))
			{
				GameObject gameObject = new GameObject("TransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
				gameObject.transform.SetParent(base.transform, worldPositionStays: false);
				_canvas = gameObject.GetComponent<Canvas>();
				_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				_canvas.sortingOrder = 32767;
				CanvasScaler component = gameObject.GetComponent<CanvasScaler>();
				component.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				component.referenceResolution = new Vector2(1920f, 1080f);
				_canvasGroup = gameObject.GetComponent<CanvasGroup>();
				GameObject gameObject2 = new GameObject("Fade", typeof(RectTransform), typeof(Image));
				gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
				RectTransform component2 = gameObject2.GetComponent<RectTransform>();
				component2.anchorMin = Vector2.zero;
				component2.anchorMax = Vector2.one;
				component2.offsetMin = Vector2.zero;
				component2.offsetMax = Vector2.zero;
				Image component3 = gameObject2.GetComponent<Image>();
				component3.color = fadeColor;
				component3.raycastTarget = true;
			}
		}
	}
}
