using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// Aktive Akteur-Darstellungen (Sprite wie Mesh) melden sich hier an,
	/// damit Querschnittssysteme (z. B. Verdeckungs-Transparenz) sie ohne
	/// teure Szenensuche und ohne konkreten Typ finden.
	/// </summary>
	public static class ActorPresentationRegistry
	{
		private static readonly List<MonoBehaviour> Entries = new List<MonoBehaviour>();

		public static IReadOnlyList<MonoBehaviour> Active => Entries;

		public static void Register(MonoBehaviour presentation)
		{
			if (presentation is IActorPresentation && !Entries.Contains(presentation))
			{
				Entries.Add(presentation);
			}
		}

		public static void Unregister(MonoBehaviour presentation)
		{
			Entries.Remove(presentation);
		}

		// Domain Reload ist in Unity 6 standardmaessig deaktiviert: ohne Reset
		// wuerde die Liste zerstoerte Objekte aus einem vorherigen Play-Modus-Lauf behalten.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetRegistry()
		{
			Entries.Clear();
		}
	}
}
