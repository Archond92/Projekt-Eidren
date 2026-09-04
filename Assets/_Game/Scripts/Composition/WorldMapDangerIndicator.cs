using Eidren.Data;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapDangerIndicator : MonoBehaviour
	{
		[SerializeField]
		private Text label;

		[SerializeField]
		private Text symbols;

		public int DisplayedDanger { get; private set; }

		public void ConfigureUi(Text dangerLabel, Text dangerSymbols)
		{
			label = dangerLabel;
			symbols = dangerSymbols;
		}

		public void SetDanger(int level, WorldMapThemeData theme)
		{
			if (label == null || symbols == null)
			{
				throw new InvalidOperationException("Danger indicator '" + base.name + "' is not configured.");
			}
			DisplayedDanger = Mathf.Clamp(level, 1, 5);
			label.text = $"GEFAHR {DisplayedDanger}/5";
			symbols.text = new string('◆', DisplayedDanger) + new string('◇', 5 - DisplayedDanger);
			theme.ApplyTypography(label, WorldMapTypographyRole.DangerLabel, theme.Dangerous);
			theme.ApplyTypography(symbols, WorldMapTypographyRole.DangerLabel, theme.Dangerous);
		}
	}
}
