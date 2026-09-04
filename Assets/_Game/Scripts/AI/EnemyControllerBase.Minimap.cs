using Eidren.Core.Services;
using UnityEngine;

namespace Eidren.AI
{
	/// <summary>
	/// N04-001: Gegner melden sich selbst bei der Minimap an. Eigene Teildatei,
	/// weil <c>EnemyControllerBase.cs</c> bereits über dem Klassenbudget aus §7
	/// liegt. Die An- und Abmeldung selbst steht dort im Lebenszyklus, direkt
	/// neben der des <c>CombatTargetRegistry</c>.
	/// </summary>
	public abstract partial class EnemyControllerBase : IMinimapMarker
	{
		/// <summary>
		/// Welches Symbol der Gegner auf der Karte bekommt. Der Boss überschreibt
		/// das; alles andere ist ein gewöhnlicher roter Punkt.
		/// </summary>
		protected virtual MinimapMarkerKind MinimapKind => MinimapMarkerKind.Enemy;

		MinimapMarkerKind IMinimapMarker.MinimapKind => MinimapKind;

		Vector3 IMinimapMarker.MinimapPosition => base.transform.position;

		// Ein gefallener Gegner verschwindet von der Karte, auch bevor sein Objekt
		// abgeraeumt ist.
		bool IMinimapMarker.ShowsOnMinimap => IsAlive;

		string IMinimapMarker.MinimapPaletteId => string.Empty;
	}
}
