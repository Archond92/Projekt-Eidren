using Eidren.Editor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Invarianten des Schmiede-Grundrisses (SCHMIEDE_ENTWURF.md Abschnitt 3).
/// Prueft die reinen Layoutdaten ohne Szenenoeffnung. Jeder Test hier haelt
/// einen Fehler des alten Builders dauerhaft geschlossen.
/// </summary>
public sealed class EidraForgeLayoutTests
{
	[Test]
	public void AlleTruhen_LiegenAufBoden()
	{
		foreach (ForgePlatz truhe in EidraForgeLayout.Truhen)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(truhe.X, truhe.Z, out _), Is.True,
				$"Truhe {truhe.Id} liegt bei ({truhe.X}|{truhe.Z}) nicht auf Boden.");
		}
	}

	[Test]
	public void AlleGegneranker_LiegenAufBoden()
	{
		foreach (ForgePlatz anker in EidraForgeLayout.Gegneranker)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(anker.X, anker.Z, out _), Is.True,
				$"Anker {anker.Id} liegt bei ({anker.X}|{anker.Z}) nicht auf Boden.");
		}
	}

	[Test]
	public void KeineZweiGegneranker_TeilenDieselbePosition()
	{
		List<ForgePlatz> anker = EidraForgeLayout.Gegneranker.ToList();
		for (int i = 0; i < anker.Count; i++)
		{
			for (int j = i + 1; j < anker.Count; j++)
			{
				float abstand = Mathf.Sqrt(
					(anker[i].X - anker[j].X) * (anker[i].X - anker[j].X) +
					(anker[i].Z - anker[j].Z) * (anker[i].Z - anker[j].Z));
				Assert.That(abstand, Is.GreaterThan(1.5f),
					$"{anker[i].Id} und {anker[j].Id} stehen ineinander.");
			}
		}
	}

	[Test]
	public void Gegneranker_HabenDieKanonischeZusammensetzung()
	{
		Assert.That(EidraForgeLayout.Gegneranker.Count, Is.EqualTo(24));
		Assert.That(Zaehle("ember_eater"), Is.EqualTo(12));
		Assert.That(Zaehle("ash_runner"), Is.EqualTo(6));
		Assert.That(Zaehle("forge_guardian"), Is.EqualTo(4));
		Assert.That(Zaehle("seal_guardian"), Is.EqualTo(1));
		Assert.That(Zaehle("core_guardian"), Is.EqualTo(1));
	}

	[Test]
	public void AlleFlaechen_SindVomWindfangErreichbar()
	{
		HashSet<string> erreicht = new HashSet<string> { "Windfang" };
		bool gewachsen = true;
		while (gewachsen)
		{
			gewachsen = false;
			foreach (ForgeFlaeche kandidat in EidraForgeLayout.Flaechen)
			{
				if (erreicht.Contains(kandidat.Name))
				{
					continue;
				}
				foreach (ForgeFlaeche offen in EidraForgeLayout.Flaechen.Where(f => erreicht.Contains(f.Name)))
				{
					if (Beruehren(kandidat, offen))
					{
						erreicht.Add(kandidat.Name);
						gewachsen = true;
						break;
					}
				}
			}
		}
		string[] fehlend = EidraForgeLayout.Flaechen
			.Where(f => !erreicht.Contains(f.Name))
			.Select(f => f.Name)
			.ToArray();
		Assert.That(fehlend, Is.Empty, "Nicht erreichbar: " + string.Join(", ", fehlend));
	}

	[Test]
	public void Kacheln_DeckenDieFlaecheVollstaendigUndUeberschreitenSieNicht()
	{
		ForgeFlaeche windfang = EidraForgeLayout.Flaechen.First(f => f.Name == "Windfang");
		var kacheln = EidraForgeGeometryBuilder.Kacheln(windfang).ToList();
		float summe = kacheln.Sum(k => k.breite * k.tiefe);
		float soll = (windfang.MaxX - windfang.MinX) * (windfang.MaxZ - windfang.MinZ);
		Assert.That(summe, Is.EqualTo(soll).Within(0.01f), "Kachelflaeche weicht von der Sollflaeche ab.");
		foreach (var kachel in kacheln)
		{
			Assert.That(kachel.breite, Is.LessThanOrEqualTo(EidraForgeLayout.Kachelgroesse + 0.01f));
			Assert.That(kachel.tiefe, Is.LessThanOrEqualTo(EidraForgeLayout.Kachelgroesse + 0.01f));
		}
	}

	[Test]
	public void JedeFlaeche_ErzeugtMindestensEineKachel()
	{
		foreach (ForgeFlaeche flaeche in EidraForgeLayout.Flaechen)
		{
			Assert.That(EidraForgeGeometryBuilder.Kacheln(flaeche).Any(), Is.True, flaeche.Name);
		}
	}

	[Test]
	public void VollhoheWaende_StehenNichtAufBegehbaremBoden()
	{
		foreach (var kante in EidraForgeGeometryBuilder.Wandkanten())
		{
			if (Mathf.Abs(kante.hoehe - EidraForgeLayout.Wandhoehe) > 0.01f)
			{
				continue;
			}
			Assert.That(EidraForgeLayout.IstAufBoden(kante.x, kante.z, out _), Is.False,
				$"Wandstueck bei ({kante.x}|{kante.z}) steht mitten auf begehbarem Boden.");
		}
	}

	/// <summary>
	/// Die Grube ist im Layout eine normal angrenzende Flaeche, nur sechs Einheiten
	/// tiefer. Ohne Wand am Hoehensprung haette sie keinen Rand und man fiele von
	/// der Galerie ueberall hinein. Die beiden Treppen muessen offen bleiben.
	/// </summary>
	[Test]
	public void DieGrube_IstGefasstUndDieTreppenBleibenOffen()
	{
		var kanten = EidraForgeGeometryBuilder.Wandkanten().ToList();
		int stuetzwaende = kanten.Count(k => Mathf.Abs(k.fuss - EidraForgeLayout.Grubentiefe) < 0.01f
			&& Mathf.Abs(k.hoehe - 6f) < 0.01f);
		Assert.That(stuetzwaende, Is.GreaterThan(0), "Die Grube hat keine Stuetzwaende.");
		foreach (var kante in kanten)
		{
			bool aufTreppeWest = kante.x > -16f && kante.x < -10f && kante.z > 26f && kante.z < 30f;
			bool aufTreppeOst = kante.x > 10f && kante.x < 16f && kante.z > 34f && kante.z < 38f;
			Assert.That(aufTreppeWest || aufTreppeOst, Is.False,
				$"Wand versperrt eine Treppe bei ({kante.x}|{kante.z}).");
		}
	}

	/// <summary>
	/// Der eigentliche Schutz: jede Kante, hinter der kein Boden liegt, muss von
	/// einer Wand gedeckt sein. Sonst laeuft die Figur dort ins Leere. Grobe
	/// Wandabschnitte reissen genau hier Luecken, wenn eine Oeffnungskante mitten
	/// in einen Abschnitt faellt.
	/// </summary>
	[Test]
	public void JedeAussenkante_IstDurchEineWandGedeckt()
	{
		var kanten = EidraForgeGeometryBuilder.Wandkanten().ToList();
		foreach (ForgeFlaeche flaeche in EidraForgeLayout.Flaechen)
		{
			for (float x = flaeche.MinX + 0.5f; x < flaeche.MaxX; x += 1f)
			{
				PruefeKante(kanten, x, flaeche.MinZ - 0.5f);
				PruefeKante(kanten, x, flaeche.MaxZ + 0.5f);
			}
			for (float z = flaeche.MinZ + 0.5f; z < flaeche.MaxZ; z += 1f)
			{
				PruefeKante(kanten, flaeche.MinX - 0.5f, z);
				PruefeKante(kanten, flaeche.MaxX + 0.5f, z);
			}
		}
	}

	[Test]
	public void Windfang_HatWaendeAufDenAussenseiten()
	{
		var kanten = EidraForgeGeometryBuilder.Wandkanten().ToList();
		ForgeFlaeche windfang = EidraForgeLayout.Flaechen.First(f => f.Name == "Windfang");
		Assert.That(kanten.Any(k => k.z < windfang.MinZ), Is.True, "Suedliche Stirnwand fehlt.");
		Assert.That(kanten.Any(k => k.x < windfang.MinX && k.z > windfang.MinZ && k.z < windfang.MaxZ), Is.True,
			"Westwand fehlt.");
		Assert.That(kanten.Any(k => k.x > windfang.MaxX && k.z > windfang.MinZ && k.z < windfang.MaxZ), Is.True,
			"Ostwand fehlt.");
	}

	[Test]
	public void Truhen_HabenDieKanonischeFamilienverteilung()
	{
		Assert.That(EidraForgeLayout.Truhen.Count(t => t.Id.StartsWith("forge.supply.")), Is.EqualTo(3));
		Assert.That(EidraForgeLayout.Truhen.Count(t => t.Id.StartsWith("forge.optional.")), Is.EqualTo(2));
		Assert.That(EidraForgeLayout.Truhen.Count(t => t.Id.StartsWith("forge.elite.")), Is.EqualTo(1));
		Assert.That(EidraForgeLayout.Truhen.Count(t => t.Id.StartsWith("forge.completion.")), Is.EqualTo(1));
	}

	[Test]
	public void Truhen_KollidierenNichtMitGegnerankern()
	{
		foreach (ForgePlatz truhe in EidraForgeLayout.Truhen)
		{
			foreach (ForgePlatz anker in EidraForgeLayout.Gegneranker)
			{
				float abstand = Mathf.Sqrt(
					(truhe.X - anker.X) * (truhe.X - anker.X) + (truhe.Z - anker.Z) * (truhe.Z - anker.Z));
				Assert.That(abstand, Is.GreaterThan(1.5f), $"{truhe.Id} steckt in {anker.Id}.");
			}
		}
	}

	private static void PruefeKante(List<(float x, float z, float breite, float tiefe, float fuss, float hoehe)> kanten,
		float x, float z)
	{
		if (EidraForgeLayout.IstAufBoden(x, z, out _))
		{
			return;
		}
		bool gedeckt = kanten.Any(k =>
			x >= k.x - k.breite * 0.5f - 0.01f && x <= k.x + k.breite * 0.5f + 0.01f &&
			z >= k.z - k.tiefe * 0.5f - 0.01f && z <= k.z + k.tiefe * 0.5f + 0.01f);
		Assert.That(gedeckt, Is.True, $"Offene Kante bei ({x}|{z}) ohne Wand.");
	}

	private static int Zaehle(string stamm)
	{
		return EidraForgeLayout.Gegneranker.Count(platz => platz.Id.Contains(stamm));
	}

	private static bool Beruehren(ForgeFlaeche a, ForgeFlaeche b)
	{
		bool xUeberlappt = a.MinX <= b.MaxX + 0.01f && b.MinX <= a.MaxX + 0.01f;
		bool zUeberlappt = a.MinZ <= b.MaxZ + 0.01f && b.MinZ <= a.MaxZ + 0.01f;
		return xUeberlappt && zUeberlappt;
	}
}
