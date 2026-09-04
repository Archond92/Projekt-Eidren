using System.Collections.Generic;

namespace Eidren.Editor
{
	/// <summary>Eine rechteckige Bodenflaeche des Dungeons.</summary>
	public readonly struct ForgeFlaeche
	{
		public ForgeFlaeche(string name, float minX, float maxX, float minZ, float maxZ, float hoehe = 0f,
			bool istRampe = false, float gefaelleX = 0f, float gefaelleZ = 0f)
		{
			Name = name;
			MinX = minX;
			MaxX = maxX;
			MinZ = minZ;
			MaxZ = maxZ;
			Hoehe = hoehe;
			IstRampe = istRampe;
			GefaelleX = gefaelleX;
			GefaelleZ = gefaelleZ;
		}

		public string Name { get; }

		public float MinX { get; }

		public float MaxX { get; }

		public float MinZ { get; }

		public float MaxZ { get; }

		public float Hoehe { get; }

		/// <summary>Rampen ueberbruecken einen Hoehensprung; an ihnen steht keine Wand.</summary>
		public bool IstRampe { get; }

		/// <summary>Richtung des Gefaelles: 1 heisst bergab zu groesseren Werten,
		/// -1 bergab zu kleineren, 0 keine Neigung auf dieser Achse.</summary>
		public float GefaelleX { get; }

		public float GefaelleZ { get; }
	}

	/// <summary>Ein benannter Platz auf dem Boden: Truhe oder Gegneranker.</summary>
	public readonly struct ForgePlatz
	{
		public ForgePlatz(string id, float x, float z)
		{
			Id = id;
			X = x;
			Z = z;
		}

		public string Id { get; }

		public float X { get; }

		public float Z { get; }
	}

	/// <summary>
	/// Grundriss der Verlassenen Eidra-Schmiede als reine Daten, ohne Szenenbezug
	/// (SCHMIEDE_ENTWURF.md Abschnitt 3). Bewusst vom Bau getrennt, damit die
	/// Invarianten ohne Szenenoeffnung pruefbar sind - der alte Builder hatte
	/// Truhen im Nichts, deckungsgleiche Gegner und abgeriegelte Seitenwege,
	/// weil Layout und Bau in einer Methode verwoben waren.
	/// </summary>
	public static class EidraForgeLayout
	{
		public const float Wandhoehe = 5.5f;

		public const float Grubentiefe = -6f;

		public const float Kachelgroesse = 4f;

		private static readonly ForgeFlaeche[] FlaechenIntern =
		{
			// Akt 1 - Blasebalg: verengt sich von 20 auf 6 Einheiten Breite.
			new ForgeFlaeche("Windfang", -10f, 10f, -38f, -30f),
			new ForgeFlaeche("Schlund", -5f, 5f, -30f, -28f),
			new ForgeFlaeche("Balgkammer", -7f, 7f, -28f, -20f),
			new ForgeFlaeche("Duese", -3f, 3f, -20f, -10f),
			// Akt 2 - Giesshalle: Rinne als schneller Weg, links die intakte
			// Masselreihe, rechts der Einbruch. Beide Seiten sind Rundwege.
			new ForgeFlaeche("Rinne", -4f, 4f, -10f, 14f),
			new ForgeFlaeche("MasselI", -24f, -6f, -8f, -4f),
			new ForgeFlaeche("MasselII", -24f, -6f, 0f, 4f),
			new ForgeFlaeche("Steg", -24f, -20f, -4f, 0f),
			new ForgeFlaeche("VerbinderMasselI", -6f, -4f, -7f, -5f),
			new ForgeFlaeche("VerbinderMasselII", -6f, -4f, 1f, 3f),
			new ForgeFlaeche("VerbinderMasselRing", -20f, -16f, 4f, 14f),
			new ForgeFlaeche("EinbruchA", 5f, 24f, -8f, -2f),
			new ForgeFlaeche("EinbruchB", 8f, 28f, -2f, 4f),
			new ForgeFlaeche("EinbruchC", 5f, 20f, 4f, 10f),
			new ForgeFlaeche("VerbinderEinbruch", 4f, 6f, -6f, 0f),
			new ForgeFlaeche("VerbinderEinbruchRing", 12f, 18f, 10f, 14f),
			// Akt 3 - Esse: geschlossener Ring um die sechs Einheiten tiefe Grube.
			new ForgeFlaeche("RingSued", -20f, 20f, 14f, 22f),
			new ForgeFlaeche("RingWest", -20f, -10f, 22f, 42f),
			new ForgeFlaeche("RingOst", 10f, 20f, 22f, 42f),
			new ForgeFlaeche("RingNord", -20f, 20f, 42f, 50f),
			new ForgeFlaeche("Essenkern", -10f, 10f, 22f, 42f, Grubentiefe),
			/* Rampen ueber die volle Schenkelbreite: 10 Einheiten Lauf auf 6
			   Einheiten Gefaelle ergeben rund 31 Grad. Bei den urspruenglichen
			   6 Einheiten Lauf waeren es 45 Grad gewesen - genau die Grenze, ab
			   der der NavMesh-Agent nicht mehr laeuft. */
			new ForgeFlaeche("TreppeWest", -20f, -10f, 26f, 30f, 0f, istRampe: true, gefaelleX: 1f),
			new ForgeFlaeche("TreppeOst", 10f, 20f, 34f, 38f, 0f, istRampe: true, gefaelleX: -1f),
			new ForgeFlaeche("Abstichrinne", -3f, 3f, 42f, 50f, 0f, istRampe: true, gefaelleZ: -1f),
			new ForgeFlaeche("Hammerwerk", 22f, 36f, 26f, 38f),
			new ForgeFlaeche("VerbinderHammerwerk", 20f, 22f, 30f, 34f),
			new ForgeFlaeche("Bindungskammer", -36f, -22f, 26f, 38f),
			new ForgeFlaeche("VerbinderBindungskammer", -22f, -20f, 30f, 34f)
		};

		private static readonly ForgePlatz[] TruhenIntern =
		{
			new ForgePlatz("forge.reward.small", -5f, -34f),
			new ForgePlatz("forge.reward.medium", 0f, -34f),
			new ForgePlatz("forge.reward.large", 5f, -34f),
			new ForgePlatz("forge.recovery", 8f, -32f),
			new ForgePlatz("forge.supply.01", -22f, -6f),
			new ForgePlatz("forge.supply.02", -22f, 2f),
			new ForgePlatz("forge.supply.03", -15f, 38f),
			new ForgePlatz("forge.optional.01", -26f, 30f),
			new ForgePlatz("forge.optional.02", 15f, 26f),
			new ForgePlatz("forge.elite.01", 30f, 34f),
			new ForgePlatz("forge.completion.01", 8f, 47f)
		};

		/* Verteilung laut SCHMIEDE_ENTWURF.md Abschnitt 5. Die Zusammensetzung
		   bleibt exakt bei 24, damit EidraForgePopulationRules und
		   EidraForgeBalance unveraendert gueltig bleiben. */
		private static readonly ForgePlatz[] GegnerankerIntern =
		{
			new ForgePlatz("forge.enemy.ember_eater.01", -3f, -25f),
			new ForgePlatz("forge.enemy.ember_eater.02", 3f, -23f),
			new ForgePlatz("forge.enemy.ember_eater.03", -17f, -6f),
			new ForgePlatz("forge.enemy.ember_eater.04", -13f, -6f),
			new ForgePlatz("forge.enemy.ember_eater.05", -9f, -6f),
			new ForgePlatz("forge.enemy.ember_eater.06", -17f, 2f),
			new ForgePlatz("forge.enemy.ember_eater.07", -13f, 2f),
			new ForgePlatz("forge.enemy.ember_eater.08", 12f, -6f),
			new ForgePlatz("forge.enemy.ember_eater.09", 20f, 2f),
			new ForgePlatz("forge.enemy.ember_eater.10", 12f, 7f),
			new ForgePlatz("forge.enemy.ember_eater.11", -14f, 18f),
			new ForgePlatz("forge.enemy.ember_eater.12", 14f, 18f),
			new ForgePlatz("forge.enemy.ash_runner.01", 0f, -17f),
			new ForgePlatz("forge.enemy.ash_runner.02", 0f, -13f),
			new ForgePlatz("forge.enemy.ash_runner.03", 10f, -5f),
			new ForgePlatz("forge.enemy.ash_runner.04", 18f, -5f),
			new ForgePlatz("forge.enemy.ash_runner.05", 14f, 1f),
			new ForgePlatz("forge.enemy.ash_runner.06", 22f, 1f),
			new ForgePlatz("forge.enemy.forge_guardian.01", 0f, -2f),
			new ForgePlatz("forge.enemy.forge_guardian.02", 0f, 9f),
			new ForgePlatz("forge.enemy.forge_guardian.03", -9f, 2f),
			new ForgePlatz("forge.enemy.forge_guardian.04", 15f, 32f),
			new ForgePlatz("forge.enemy.seal_guardian.01", 29f, 32f),
			new ForgePlatz("forge.enemy.core_guardian.01", 0f, 32f)
		};

		public static IReadOnlyList<ForgeFlaeche> Flaechen => FlaechenIntern;

		public static IReadOnlyList<ForgePlatz> Truhen => TruhenIntern;

		public static IReadOnlyList<ForgePlatz> Gegneranker => GegnerankerIntern;

		/// <summary>Liegt der Punkt auf einer Bodenflaeche? Liefert deren Hoehe mit.</summary>
		public static bool IstAufBoden(float x, float z, out float hoehe)
		{
			if (TryFinde(x, z, out ForgeFlaeche flaeche))
			{
				hoehe = flaeche.Hoehe;
				return true;
			}
			hoehe = 0f;
			return false;
		}

		/// <summary>
		/// Liefert die Flaeche unter dem Punkt. Rampen gewinnen gegen normale Flaechen,
		/// damit ein Punkt auf einer Treppe auch als Rampe erkannt wird, obwohl er
		/// zugleich im Ringschenkel liegt.
		/// </summary>
		public static bool TryFinde(float x, float z, out ForgeFlaeche treffer)
		{
			bool gefunden = false;
			treffer = default;
			foreach (ForgeFlaeche flaeche in FlaechenIntern)
			{
				if (x < flaeche.MinX || x > flaeche.MaxX || z < flaeche.MinZ || z > flaeche.MaxZ)
				{
					continue;
				}
				if (!gefunden || flaeche.IstRampe)
				{
					treffer = flaeche;
					gefunden = true;
				}
				if (flaeche.IstRampe)
				{
					return true;
				}
			}
			return gefunden;
		}
	}
}
