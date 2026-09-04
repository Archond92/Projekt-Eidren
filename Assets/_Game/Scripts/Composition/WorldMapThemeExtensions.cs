using Eidren.Data;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	internal static class WorldMapThemeExtensions
	{
		internal static void ApplyTypography(this WorldMapThemeData theme, Text text, WorldMapTypographyRole role, Color color)
		{
			if (!(text == null))
			{
				WorldMapTypographyStyle typography = theme.GetTypography(role);
				if (typography.Font != null)
				{
					text.font = typography.Font;
				}
				text.fontSize = typography.Size;
				text.fontStyle = typography.Style;
				text.color = color;
			}
		}
	}
}
