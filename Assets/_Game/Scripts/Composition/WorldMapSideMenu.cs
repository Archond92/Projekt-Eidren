using Eidren.Data;
using System.Linq;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapSideMenu : MonoBehaviour
	{
		[SerializeField]
		private Image surface;

		[SerializeField]
		private Button[] placeholderButtons = Array.Empty<Button>();

		[SerializeField]
		private Text[] placeholderLabels = Array.Empty<Text>();

		[SerializeField]
		private Button backButton;

		[SerializeField]
		private Text backLabel;

		public Button[] PlaceholderButtons => placeholderButtons;

		public Button BackButton => backButton;

		public event Action BackRequested;

		public void ConfigureUi(Image menuSurface, Button[] placeholders, Text[] labels, Button back, Text configuredBackLabel)
		{
			if (backButton != null)
			{
				backButton.onClick.RemoveListener(NotifyBack);
			}
			surface = menuSurface;
			placeholderButtons = placeholders ?? Array.Empty<Button>();
			placeholderLabels = labels ?? Array.Empty<Text>();
			backButton = back;
			backLabel = configuredBackLabel;
			backButton.onClick.AddListener(NotifyBack);
		}

		public void ApplyTheme(WorldMapThemeData theme)
		{
			if (surface == null || backButton == null || backLabel == null || placeholderButtons.Any((Button button) => button == null) || placeholderLabels.Any((Text label) => label == null) || placeholderLabels.Length != placeholderButtons.Length)
			{
				throw new InvalidOperationException("Side menu '" + base.name + "' has missing references.");
			}
			surface.color = theme.BackgroundRaised;
			for (int num = 0; num < placeholderButtons.Length; num++)
			{
				placeholderButtons[num].interactable = false;
				theme.ApplyTypography(placeholderLabels[num], WorldMapTypographyRole.SmallMetaText, theme.TextSecondary);
			}
			theme.ApplyTypography(backLabel, WorldMapTypographyRole.ButtonText, theme.TextPrimary);
		}

		private void NotifyBack()
		{
			this.BackRequested?.Invoke();
		}

		private void OnDestroy()
		{
			if (backButton != null)
			{
				backButton.onClick.RemoveListener(NotifyBack);
			}
		}
	}
}
