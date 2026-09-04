using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Core.Services
{
	/// <summary>
	/// Was die Minimap zeichnen kann (N04-001).
	/// </summary>
	public enum MinimapMarkerKind
	{
		Resource,
		Enemy,
		Boss,
		Chest
	}

	/// <summary>
	/// Ein Eintrag der Minimap. Die Objekte melden sich selbst an, statt dass die
	/// Karte sie sucht — §8 verbietet <c>Find*</c> im Kartentakt.
	/// </summary>
	public interface IMinimapMarker
	{
		MinimapMarkerKind MinimapKind { get; }

		Vector3 MinimapPosition { get; }

		/// <summary>
		/// Ob der Marker gerade gezeichnet wird. Abgebaute Knoten, noch nicht
		/// entdeckte Kisten und gefallene Gegner liefern hier false.
		/// </summary>
		bool ShowsOnMinimap { get; }

		/// <summary>
		/// Schlüssel für die Einfärbung — bei Ressourcen die Id der Definition
		/// (z. B. <c>resource.copper_vein</c>), sonst leer.
		/// </summary>
		string MinimapPaletteId { get; }
	}

	/// <summary>
	/// Ein Marker, der erst nach der ersten Sichtung erscheint (Kisten). Die
	/// Sichtprüfung macht die Karte, weil nur sie die Kamera kennt; gemerkt wird
	/// die Entdeckung hier. Einbahnstraße: einmal gesehen bleibt gesehen.
	/// </summary>
	public interface IMinimapDiscoverable : IMinimapMarker
	{
		void MarkSeenOnMinimap();
	}

	/// <summary>
	/// Sammelstelle aller Minimap-Marker, nach dem Muster von
	/// <c>CombatTargetRegistry</c>: An- und Abmeldung im Lebenszyklus des Objekts,
	/// statischer Zustand mit Reset nach §2.
	/// </summary>
	public static class MinimapRegistry
	{
		private static readonly List<IMinimapMarker> RegisteredMarkers = new List<IMinimapMarker>();

		public static IReadOnlyList<IMinimapMarker> Markers => RegisteredMarkers;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			RegisteredMarkers.Clear();
		}

		public static void Register(IMinimapMarker marker)
		{
			if (marker != null && !RegisteredMarkers.Contains(marker))
			{
				RegisteredMarkers.Add(marker);
			}
		}

		public static void Unregister(IMinimapMarker marker)
		{
			if (marker != null)
			{
				RegisteredMarkers.Remove(marker);
			}
		}
	}
}
