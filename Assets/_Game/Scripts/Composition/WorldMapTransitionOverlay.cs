using Eidren.Core.Services;
using Eidren.Data;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class WorldMapTransitionOverlay : MonoBehaviour, ISceneTransitionView
	{
		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private Image surface;

		[SerializeField]
		private Image energySigil;

		private WorldMapThemeData _theme;

		private bool _preservedForTransition;

		public bool IsPreservedForTransition => _preservedForTransition;

		public void ConfigureUi(CanvasGroup group, Image overlaySurface, Image sigil)
		{
			canvasGroup = group;
			surface = overlaySurface;
			energySigil = sigil;
		}

		public void ApplyTheme(WorldMapThemeData theme)
		{
			_theme = theme ?? throw new ArgumentNullException("theme");
			if (canvasGroup == null || surface == null || energySigil == null)
			{
				throw new InvalidOperationException("Transition overlay '" + base.name + "' has missing references.");
			}
			surface.color = theme.BackgroundDeep;
			energySigil.color = theme.EnergyGlow;
			SetVisual(0f);
			SetInputBlocked(blocked: false);
		}

		public void SetInputBlocked(bool blocked)
		{
			canvasGroup.blocksRaycasts = blocked;
			canvasGroup.interactable = blocked;
		}

		public IEnumerator FadeOut()
		{
			yield return FadeTo(1f);
		}

		public IEnumerator FadeIn()
		{
			yield return FadeTo(0f);
			if (_preservedForTransition)
			{
				_preservedForTransition = false;
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		public void PreserveThroughSceneLoad()
		{
			if (!_preservedForTransition)
			{
				base.transform.SetParent(null, worldPositionStays: false);
				UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
				_preservedForTransition = true;
			}
		}

		private IEnumerator FadeTo(float target)
		{
			float from = canvasGroup.alpha;
			float duration = Mathf.Max(0.01f, _theme.TransitionFadeDuration);
			for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
			{
				float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
				SetVisual(Mathf.Lerp(from, target, t));
				yield return null;
			}
			SetVisual(target);
		}

		private void SetVisual(float alpha)
		{
			canvasGroup.alpha = alpha;
			energySigil.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, alpha);
		}
	}
}
