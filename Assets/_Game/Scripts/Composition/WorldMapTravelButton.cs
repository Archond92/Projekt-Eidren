using Eidren.Data;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapTravelButton : MonoBehaviour
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private Image surface;

		[SerializeField]
		private Text label;

		[SerializeField]
		private RectTransform animatedRoot;

		private WorldMapThemeData _theme;

		private Coroutine _confirmationRoutine;

		public Button Button => button;

		public bool IsAvailable => button != null && button.interactable;

		public string DisplayedLabel => (label != null) ? label.text : string.Empty;

		public Color LabelColor => (label != null) ? label.color : Color.clear;

		public event Action TravelRequested;

		private void Awake()
		{
			BindButton();
		}

		public void ConfigureUi(Button travelButton, Image buttonSurface, Text buttonLabel, RectTransform motionRoot)
		{
			button = travelButton;
			surface = buttonSurface;
			label = buttonLabel;
			animatedRoot = motionRoot;
			BindButton();
		}

		public void ApplyTheme(WorldMapThemeData theme)
		{
			_theme = theme ?? throw new ArgumentNullException("theme");
			ValidateReferences();
			surface.color = theme.Available;
			theme.ApplyTypography(label, WorldMapTypographyRole.ButtonText, theme.TextPrimary);
			ColorBlock colors = button.colors;
			colors.normalColor = Color.white;
			colors.highlightedColor = theme.EnergyGlow;
			colors.pressedColor = theme.Selection;
			colors.disabledColor = theme.Locked;
			button.colors = colors;
		}

		public void SetAvailable(bool available)
		{
			button.interactable = available;
			label.text = (available ? "REISEN" : "NICHT VERFÜGBAR");
		}

		public void PlayConfirmation()
		{
			if (_confirmationRoutine != null)
			{
				StopCoroutine(_confirmationRoutine);
			}
			_confirmationRoutine = StartCoroutine(AnimateConfirmation());
		}

		public void ValidateReferences()
		{
			if (button == null || surface == null || label == null || animatedRoot == null)
			{
				throw new InvalidOperationException("Travel button '" + base.name + "' has missing references.");
			}
		}

		private void NotifyRequested()
		{
			this.TravelRequested?.Invoke();
		}

		private void BindButton()
		{
			if (!(button == null))
			{
				button.onClick.RemoveListener(NotifyRequested);
				button.onClick.AddListener(NotifyRequested);
			}
		}

		private IEnumerator AnimateConfirmation()
		{
			float duration = Mathf.Max(0.01f, _theme.TravelConfirmationDuration);
			Vector3 baseScale = Vector3.one;
			for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
			{
				float phase = Mathf.Sin(elapsed / duration * (float)Math.PI);
				animatedRoot.localScale = baseScale * Mathf.Lerp(1f, 1.035f, phase);
				yield return null;
			}
			animatedRoot.localScale = baseScale;
			_confirmationRoutine = null;
		}

		private void OnDestroy()
		{
			if (button != null)
			{
				button.onClick.RemoveListener(NotifyRequested);
			}
		}
	}
}
