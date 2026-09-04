using Eidren.Data;
using System.Linq;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapQuickActions : MonoBehaviour
	{
		[SerializeField]
		private Image surface;

		[SerializeField]
		private Button[] placeholderButtons = Array.Empty<Button>();

		[SerializeField]
		private Text[] placeholderLabels = Array.Empty<Text>();

		public Button[] PlaceholderButtons => placeholderButtons;

		public void ConfigureUi(Image actionSurface, Button[] buttons, Text[] labels)
		{
			surface = actionSurface;
			placeholderButtons = buttons ?? Array.Empty<Button>();
			placeholderLabels = labels ?? Array.Empty<Text>();
		}

		public void ApplyTheme(WorldMapThemeData theme)
		{
			if (surface == null || placeholderButtons.Any((Button button) => button == null))
			{
				throw new InvalidOperationException("Quick actions '" + base.name + "' has missing references.");
			}
			surface.color = theme.BackgroundRaised;
			if (placeholderLabels.Length != placeholderButtons.Length || placeholderLabels.Any((Text label) => label == null))
			{
				throw new InvalidOperationException("Quick actions '" + base.name + "' has invalid label references.");
			}
			for (int num = 0; num < placeholderButtons.Length; num++)
			{
				placeholderButtons[num].interactable = false;
				theme.ApplyTypography(placeholderLabels[num], WorldMapTypographyRole.SmallMetaText, theme.TextSecondary);
			}
		}
	}
}
