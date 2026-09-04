using System;
using UnityEngine;

namespace Eidren.UI
{
	/// <summary>
	/// Farben und Grössen der Minimap-Marker (N04-001). Bewusst als eigene
	/// Struktur, damit sich alles an einer Stelle im Inspector einstellen lässt —
	/// die Werte werden nach dem ersten Bildabgleich noch gedreht.
	/// </summary>
	[Serializable]
	public struct MinimapStyle
	{
		public Color enemyColor;

		public Color bossColor;

		public Color chestColor;

		/// <summary>Rückfallfarbe für Knoten ohne eigenen Eintrag.</summary>
		public Color resourceColor;

		public float enemySize;

		public float bossSize;

		public float chestSize;

		public float resourceSize;

		/// <summary>Farbe des Kartenrands.</summary>
		public Color edgeColor;

		/// <summary>Strichstärke des Kartenrands in Kartenpunkten.</summary>
		public float edgeThickness;

		public static MinimapStyle Default => new MinimapStyle
		{
			enemyColor = new Color(0.84f, 0.27f, 0.24f, 1f),
			bossColor = new Color(0.95f, 0.32f, 0.26f, 1f),
			chestColor = new Color(0.91f, 0.69f, 0.33f, 1f),
			resourceColor = new Color(0.74f, 0.78f, 0.72f, 1f),
			enemySize = 14f,
			bossSize = 32f,
			chestSize = 26f,
			resourceSize = 12f,
			edgeColor = new Color(1f, 1f, 1f, 0.85f),
			edgeThickness = 2.5f
		};
	}

	/// <summary>
	/// Einfärbung eines Knotentyps. Der Schlüssel ist die Id der
	/// <c>ResourceNodeDefinition</c>, etwa <c>resource.copper_vein</c>.
	/// </summary>
	[Serializable]
	public struct MinimapResourceColor
	{
		public string paletteId;

		public Color color;
	}
}
