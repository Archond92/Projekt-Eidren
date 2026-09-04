using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// F32-007: Weltobjekte, die freigehalten werden sollen wie ein Akteur.
	/// Die Sichtlinien-Ausblendung kannte bis dahin nur Figuren und Kreaturen
	/// (<c>ActorPresentationRegistry</c>) — eine Kiste hinter einem Fels blieb
	/// unsichtbar, waehrend ihr Preisschild frei im Raum schwebte.
	///
	/// Bewusst eine EIGENE, kurze Liste statt „alle Interaktionsziele": Jeder
	/// Eintrag kostet pro Bild einen Strahl. Hier stehen nur Dinge, deren
	/// Verschwinden den Spieler ratlos zuruecklaesst.
	/// </summary>
	public static class OcclusionFocusRegistry
	{
		private static readonly List<Component> Registered = new List<Component>();

		public static IReadOnlyList<Component> Active => Registered;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			Registered.Clear();
		}

		public static void Register(Component focus)
		{
			if (focus != null && !Registered.Contains(focus))
			{
				Registered.Add(focus);
			}
		}

		public static void Unregister(Component focus)
		{
			if (focus != null)
			{
				Registered.Remove(focus);
			}
		}
	}
}
