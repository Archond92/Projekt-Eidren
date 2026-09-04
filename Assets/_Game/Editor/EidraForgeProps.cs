using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>Ein stehender Aufbau: Masselbett, Kastenstapel, Schlackenhaufen, Steg.</summary>
	public readonly struct ForgeAufbau
	{
		public ForgeAufbau(string id, float x, float z, float breite, float tiefe, float hoehe, float drehung = 0f)
		{
			Id = id;
			X = x;
			Z = z;
			Breite = breite;
			Tiefe = tiefe;
			Hoehe = hoehe;
			Drehung = drehung;
		}

		public string Id { get; }

		public float X { get; }

		public float Z { get; }

		public float Breite { get; }

		public float Tiefe { get; }

		public float Hoehe { get; }

		public float Drehung { get; }
	}

	/// <summary>Eine gluehende Ader in einem Bodenriss.</summary>
	public readonly struct ForgeGlutader
	{
		public ForgeGlutader(float x, float z, float laenge, bool entlangZ)
		{
			X = x;
			Z = z;
			Laenge = laenge;
			EntlangZ = entlangZ;
		}

		public float X { get; }

		public float Z { get; }

		public float Laenge { get; }

		public bool EntlangZ { get; }
	}

	/// <summary>
	/// Platzierung der Requisiten der Schmiede als reine Daten
	/// (SCHMIEDE_ENTWURF.md Abschnitt 8), getrennt vom Bau, damit Bodenhaftung
	/// und Kollisionen ohne Szenenoeffnung pruefbar bleiben.
	/// </summary>
	public static class EidraForgeProps
	{
		public const float Rohrhoehe = 3.5f;

		public const float Rohrdurchmesser = 1.2f;

		/// <summary>Mittelpunkt der Esse auf dem Grubenboden.</summary>
		public static readonly Vector2 EssenFuss = new Vector2(0f, 32f);

		/* Das Leitmotiv: das Rohr beginnt am Blasebalg in der Balgkammer, zieht
		   auf der Westseite durch Duese und Rinne nach Norden, schwenkt auf der
		   Galerie zur Mitte und endet an der Suedflanke der Esse. Ein Objekt,
		   das in jedem Raum wiederkehrt und immer zum Feuer zeigt. */
		private static readonly Vector2[] VerlaufIntern =
		{
			/* Am Stutzen des Blasebalgs an der Nordkante der Balgkammer: der Balg
			   ist 8 lang und passt nur laengs in die 8 tiefe Kammer, sein Stutzen
			   sitzt deshalb dort und nicht an der Westwand. */
			new Vector2(-4.5f, -20f),
			new Vector2(-2.5f, -20f),
			/* Ab der Galerie an den Waenden entlang statt geradeaus zur Esse.
			   Grund ist die Isometrie: Westversatz und Nordversatz schieben ein
			   Objekt beide nach oben-links, deshalb landet ein Rohr, das knapp
			   westlich am Ofen vorbeizieht, auf der Bildschirmhoehe der
			   Feueroeffnung und zerschneidet sie. Der Weg ueber West- und
			   Nordschenkel haelt Abstand, laeuft dort, wo eine Leitung real
			   liegen wuerde, und tritt von hinten in den Ofen - die Oeffnung
			   bleibt frei. */
			new Vector2(-2.5f, 16f),
			new Vector2(-15f, 16f),
			new Vector2(-15f, 46f),
			new Vector2(0f, 46f),
			/* 33,5, nicht 34: auf Rohrhoehe ist der Ofen auf 3,6 verjuengt und
			   reicht nur bis z=33,8. Ein Ende bei 34 stuende knapp davor. */
			new Vector2(0f, 33.5f)
		};

		private static readonly ForgePlatz[] PfannenIntern =
		{
			new ForgePlatz("forge.prop.pfanne.01", 5f, -26f),
			new ForgePlatz("forge.prop.pfanne.02", 3f, 6f),
			new ForgePlatz("forge.prop.pfanne.03", -16f, 18f),
			new ForgePlatz("forge.prop.pfanne.04", 17f, 36f),
			new ForgePlatz("forge.prop.pfanne.05", 24f, 0f),
			new ForgePlatz("forge.prop.pfanne.06", 24f, 28f)
		};

		/* Der erstarrte Auslauf im Einbruch verzweigt ueber den Boden; in der
		   Rinne liegt das Rinnenmetall, in der Grube die Reste der Abstiche. */
		private static readonly ForgeGlutader[] AdernIntern =
		{
			new ForgeGlutader(10f, -4f, 6f, entlangZ: true),
			new ForgeGlutader(14f, -1f, 5f, entlangZ: false),
			new ForgeGlutader(18f, 2f, 4f, entlangZ: true),
			new ForgeGlutader(12f, 5f, 3f, entlangZ: false),
			new ForgeGlutader(0f, -6f, 6f, entlangZ: true),
			new ForgeGlutader(0f, 8f, 5f, entlangZ: true),
			new ForgeGlutader(-6f, 26f, 4f, entlangZ: true),
			new ForgeGlutader(6f, 36f, 4f, entlangZ: true)
		};

		/* Zwei Reihen je Masselkammer, dazwischen ein Gang. Die Gegneranker stehen
		   auf z=-6 bzw. z=2 - genau in diesem Gang. */
		private static readonly ForgeAufbau[] MasselbettenIntern =
		{
			new ForgeAufbau("forge.prop.massel.1a", -15f, -6.9f, 14f, 1f, 0.4f),
			new ForgeAufbau("forge.prop.massel.1b", -15f, -5.1f, 14f, 1f, 0.4f),
			new ForgeAufbau("forge.prop.massel.2a", -15f, 1.1f, 14f, 1f, 0.4f),
			new ForgeAufbau("forge.prop.massel.2b", -15f, 2.9f, 14f, 1f, 0.4f)
		};

		/* Kastenstapel an den Waenden: in den Masselkammern ordentlich gestapelt,
		   im Einbruch gekippt - dort ist die Formwand geborsten. */
		private static readonly ForgeAufbau[] KastenIntern =
		{
			new ForgeAufbau("forge.prop.kasten.01", -23f, -7f, 1.8f, 1.2f, 1.2f),
			new ForgeAufbau("forge.prop.kasten.02", -23f, 3f, 1.8f, 1.2f, 1.8f),
			new ForgeAufbau("forge.prop.kasten.03", -8f, 3.4f, 1.8f, 1.2f, 0.6f),
			new ForgeAufbau("forge.prop.kasten.04", 7f, -7f, 1.8f, 1.2f, 1.2f, 18f),
			new ForgeAufbau("forge.prop.kasten.05", 26f, 3f, 1.8f, 1.2f, 0.6f, 34f),
			new ForgeAufbau("forge.prop.kasten.06", 7f, 9f, 1.8f, 1.2f, 1.8f, 9f)
		};

		private static readonly ForgeAufbau[] SchlackeIntern =
		{
			new ForgeAufbau("forge.prop.schlacke.01", 6f, -27f, 2.2f, 1.8f, 0.8f),
			new ForgeAufbau("forge.prop.schlacke.02", -22f, -4.5f, 1.6f, 1.4f, 0.6f),
			new ForgeAufbau("forge.prop.schlacke.03", 22f, -7f, 2.4f, 2f, 0.9f),
			new ForgeAufbau("forge.prop.schlacke.04", 19f, 8f, 1.8f, 1.6f, 0.7f),
			new ForgeAufbau("forge.prop.schlacke.05", -18f, 20f, 2f, 1.8f, 0.8f),
			new ForgeAufbau("forge.prop.schlacke.06", 18f, 20f, 1.6f, 1.4f, 0.6f),
			new ForgeAufbau("forge.prop.schlacke.07", -7f, 24f, 2.4f, 2f, 0.9f),
			new ForgeAufbau("forge.prop.schlacke.08", 7f, 40f, 2f, 1.8f, 0.8f),
			new ForgeAufbau("forge.prop.schlacke.09", 34f, 36f, 1.8f, 1.6f, 0.7f),
			new ForgeAufbau("forge.prop.schlacke.10", -34f, 36f, 1.6f, 1.4f, 0.6f)
		};

		/* Stege quer ueber die Rinne auf Hoehe 4,5: das einzige, was seit dem
		   Wegfall der Decken noch ueber dem Kopf liegt. Die Hoehe im Datensatz
		   meint die Balkenstaerke, nicht die Hoehe ueber dem Boden. */
		private static readonly ForgeAufbau[] StegeIntern =
		{
			new ForgeAufbau("forge.prop.steg.01", 0f, -4f, 11f, 0.7f, 0.6f),
			new ForgeAufbau("forge.prop.steg.02", 0f, 4f, 11f, 0.7f, 0.6f),
			new ForgeAufbau("forge.prop.steg.03", 0f, 12f, 11f, 0.7f, 0.6f)
		};

		/* Einzelstuecke: je Raum das Objekt, an dem man ihn wiedererkennt. Hohe
		   Stuecke stehen an Nord- und Ostwaenden, damit sie bei 52 Grad Kulisse
		   sind und nicht die Spielfigur verdecken. */
		private static readonly ForgeAufbau[] EinzelstueckeIntern =
		{
			new ForgeAufbau("forge.prop.blasebalg", -4.5f, -24f, 3.5f, 8f, 3f),
			new ForgeAufbau("forge.prop.duesenstein", 0f, -15f, 6f, 2.5f, 4f),
			new ForgeAufbau("forge.prop.erzkarren", -8f, -32f, 2.2f, 1.4f, 1.4f, 22f),
			new ForgeAufbau("forge.prop.schwanzhammer", 25f, 29f, 3f, 5f, 4f),
			new ForgeAufbau("forge.prop.reif.01", -33f, 29f, 2.5f, 0.4f, 2.5f),
			new ForgeAufbau("forge.prop.reif.02", -33f, 32f, 2.5f, 0.4f, 2.5f),
			new ForgeAufbau("forge.prop.reif.03", -33f, 35f, 2.5f, 0.4f, 2.5f),
			/* Abseits der Rampen: TreppeWest liegt bei z 26..30, TreppeOst bei
			   z 34..38. Ein Mast dort stuende auf der Schraege statt auf der
			   Galerie und haenge in der Luft. */
			new ForgeAufbau("forge.prop.gichtkuebel.west", -12f, 36f, 1.4f, 1.4f, 1.6f),
			new ForgeAufbau("forge.prop.gichtkuebel.ost", 12f, 26f, 1.4f, 1.4f, 1.6f),
			new ForgeAufbau("forge.prop.formwall", 20f, -5f, 8f, 1.2f, 2.2f, 12f),
			new ForgeAufbau("forge.prop.bauplatz", 16f, 6f, 4f, 3f, 0.3f)
		};

		/* Deckentruemmer im Einbruch. Ihr Zweck ist nicht Dekoration, sondern das
		   Brechen der Sichtlinien: der Einbruch ist der einzige Raum, in dem man
		   beim Betreten nicht sofort sieht, was drin ist. */
		private static readonly ForgeAufbau[] TruemmerIntern =
		{
			new ForgeAufbau("forge.prop.truemmer.01", 9f, -6.5f, 2.6f, 2.2f, 1.6f, 12f),
			new ForgeAufbau("forge.prop.truemmer.02", 16f, -4f, 3.2f, 2.6f, 2.1f, 48f),
			new ForgeAufbau("forge.prop.truemmer.03", 21f, -1f, 2.2f, 2f, 1.4f, 71f),
			new ForgeAufbau("forge.prop.truemmer.04", 11f, 2f, 2.8f, 2.4f, 1.8f, 26f),
			new ForgeAufbau("forge.prop.truemmer.05", 24f, 3f, 2.4f, 2f, 1.5f, 55f),
			new ForgeAufbau("forge.prop.truemmer.06", 16f, 8f, 3f, 2.5f, 2f, 8f)
		};

		public static IReadOnlyList<Vector2> Windrohrverlauf => VerlaufIntern;

		public static IReadOnlyList<ForgeAufbau> Einzelstuecke => EinzelstueckeIntern;

		public static IReadOnlyList<ForgeAufbau> Deckentruemmer => TruemmerIntern;

		public static IReadOnlyList<ForgeAufbau> Masselbetten => MasselbettenIntern;

		public static IReadOnlyList<ForgeAufbau> Formkastenstapel => KastenIntern;

		public static IReadOnlyList<ForgeAufbau> Schlackenhaufen => SchlackeIntern;

		public static IReadOnlyList<ForgeAufbau> Rinnenstege => StegeIntern;

		/// <summary>Alle stehenden Aufbauten, fuer die gemeinsamen Invarianten.</summary>
		public static IEnumerable<ForgeAufbau> AlleAufbauten =>
			MasselbettenIntern.Concat(KastenIntern).Concat(SchlackeIntern).Concat(StegeIntern)
				.Concat(EinzelstueckeIntern).Concat(TruemmerIntern);

		public static IReadOnlyList<ForgePlatz> Kohlenpfannen => PfannenIntern;

		public static IReadOnlyList<ForgeGlutader> Glutadern => AdernIntern;
	}
}
