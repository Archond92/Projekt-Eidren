using Eidren.Data;
using System.Linq;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapTopStatusBar : MonoBehaviour
	{
		[SerializeField]
		private Image surface;

		[SerializeField]
		private WorldMapStatusEntry[] entries = Array.Empty<WorldMapStatusEntry>();

		public WorldMapStatusEntry[] Entries => entries;

		public void ConfigureUi(Image barSurface, WorldMapStatusEntry[] statusEntries)
		{
			surface = barSurface;
			entries = statusEntries ?? Array.Empty<WorldMapStatusEntry>();
		}

		public void ApplyTheme(WorldMapThemeData theme)
		{
			if (surface == null || entries == null || entries.Any((WorldMapStatusEntry entry) => entry == null))
			{
				throw new InvalidOperationException("Top status bar '" + base.name + "' has missing references.");
			}
			surface.color = theme.BackgroundRaised;
			for (int num = 0; num < entries.Length; num++)
			{
				entries[num].ApplyTheme(theme, num == 1);
			}
		}

		public void SetEntries(string[,] values)
		{
			for (int i = 0; i < entries.Length; i++)
			{
				bool flag = values != null && i < values.GetLength(0);
				entries[i].SetEntry(flag ? values[i, 0] : string.Empty, flag ? values[i, 1] : string.Empty, flag);
			}
		}
	}
}
