using Eidren.Data;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapStatusEntry : MonoBehaviour
	{
		[SerializeField]
		private Image surface;

		[SerializeField]
		private Text label;

		[SerializeField]
		private Text value;

		public void ConfigureUi(Image entrySurface, Text moduleLabel, Text moduleValue)
		{
			surface = entrySurface;
			label = moduleLabel;
			value = moduleValue;
		}

		public void ApplyTheme(WorldMapThemeData theme, bool timer)
		{
			if (surface == null || label == null || value == null)
			{
				throw new InvalidOperationException("Status entry '" + base.name + "' is not configured.");
			}
			Color backgroundRaised = theme.BackgroundRaised;
			backgroundRaised.a = 0.7f;
			surface.color = backgroundRaised;
			theme.ApplyTypography(label, WorldMapTypographyRole.SmallMetaText, theme.TextSecondary);
			theme.ApplyTypography(value, timer ? WorldMapTypographyRole.TimerText : WorldMapTypographyRole.BodyText, theme.TextPrimary);
		}

		public void SetEntry(string moduleLabel, string moduleValue, bool visible)
		{
			base.gameObject.SetActive(visible);
			if (visible)
			{
				label.text = moduleLabel;
				value.text = moduleValue;
			}
		}
	}
}
