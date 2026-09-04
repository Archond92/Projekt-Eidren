using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>Ein Punktlicht des Verlieses.</summary>
	public readonly struct ForgeLicht
	{
		public ForgeLicht(string id, float x, float z, float hoehe, float reichweite, float intensitaet, Color farbe)
		{
			Id = id;
			X = x;
			Z = z;
			Hoehe = hoehe;
			Reichweite = reichweite;
			Intensitaet = intensitaet;
			Farbe = farbe;
		}

		public string Id { get; }

		public float X { get; }

		public float Z { get; }

		public float Hoehe { get; }

		public float Reichweite { get; }

		public float Intensitaet { get; }

		public Color Farbe { get; }
	}

	/// <summary>
	/// Beleuchtung der Verlassenen Eidra-Schmiede als reine Daten
	/// (SCHMIEDE_ENTWURF.md Abschnitt 9). Alle Grundwerte sind mit ForgeGlowProbe
	/// gemessen, nicht geschaetzt.
	/// </summary>
	public static class EidraForgeLicht
	{
		/* Angehoben gegenueber der Probe. Dort stand ein einzelnes Objekt nah an der
		   Kamera; im vollen Raum mit 5,5 hohen Waenden, die ihren Schatten
		   hineinwerfen, trugen die Probewerte nicht - Boden und Ofenkoerper blieben
		   schwarz. Der Vorsprung der Glut bleibt erhalten, weil sie unbeleuchtet
		   bei vollem Wert liegt. */
		public static readonly Color Umgebung = new Color(0.16f, 0.165f, 0.2f);

		public static readonly Color Richtungsfarbe = new Color(0.72f, 0.78f, 1f);

		public const float Richtungsintensitaet = 1f;

		/// <summary>Neigung des Richtungslichts: flach aus Westsuedwest, damit die
		/// 5,5 hohen Waende lange Schraegschatten werfen - das Dunkel, das seit dem
		/// Verzicht auf Decken die Enge erzeugt.</summary>
		public static readonly Vector3 Richtungswinkel = new Vector3(30f, 250f, 0f);

		private static readonly Color Glut = new Color(1f, 0.5f, 0.16f);

		private static readonly Color Kalt = new Color(0.55f, 0.68f, 1f);

		/* Reichweiten bewusst knapp: das Limit von vier Zusatzlichtern gilt pro
		   Objekt, und grosszuegige Reichweiten summieren sich schnell. Die Esse
		   allein belegt mit Reichweite 18 in der ganzen Essenkammer einen Platz.
		   Glutflaechen brauchen kein eigenes Licht - das hat die Probe belegt;
		   diese Lichter faerben nur die Umgebung mit. */
		private static readonly ForgeLicht[] LichterIntern =
		{
			new ForgeLicht("forge.licht.esse", 0f, 32f, 4f, 18f, 9f, Glut),
			new ForgeLicht("forge.licht.pfanne.01", 5f, -26f, 1.1f, 7f, 5f, Glut),
			new ForgeLicht("forge.licht.pfanne.02", 3f, 6f, 1.1f, 7f, 5f, Glut),
			new ForgeLicht("forge.licht.pfanne.03", -16f, 18f, 1.1f, 7f, 5f, Glut),
			new ForgeLicht("forge.licht.pfanne.04", 17f, 36f, 1.1f, 7f, 5f, Glut),
			new ForgeLicht("forge.licht.pfanne.05", 24f, 0f, 1.1f, 7f, 5f, Glut),
			new ForgeLicht("forge.licht.pfanne.06", 24f, 28f, 1.1f, 7f, 5f, Glut),
			/* Raeume ohne Glutquelle. In 2B war die Masselkammer im Verlieslicht
			   praktisch schwarz, weil die 5,5 hohe Wand ihren Schatten hineinwirft
			   und dort nichts leuchtet. */
			new ForgeLicht("forge.licht.windfang", 0f, -34f, 2.4f, 9f, 4.2f, Glut),
			new ForgeLicht("forge.licht.massel.01", -15f, -6f, 2.4f, 8f, 4f, Glut),
			new ForgeLicht("forge.licht.massel.02", -15f, 2f, 2.4f, 8f, 4f, Glut),
			/* Das einzige kalte Licht im ganzen Verlies. Die Bindungskammer ist
			   bewusst der eine Raum, der nicht nach Feuer aussieht. */
			new ForgeLicht("forge.licht.bindung", -30f, 32f, 2.6f, 9f, 3.6f, Kalt)
		};

		public static IReadOnlyList<ForgeLicht> Punktlichter => LichterIntern;
	}
}
