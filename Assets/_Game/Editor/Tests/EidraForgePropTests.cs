using Eidren.Editor;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

/// <summary>
/// Invarianten der Requisitenplatzierung (SCHMIEDE_ENTWURF.md Abschnitt 8).
/// Prueft die reinen Daten ohne Szenenoeffnung, wie schon beim Grundriss.
/// </summary>
public sealed class EidraForgePropTests
{
	[Test]
	public void Windrohr_LaeuftDurchgehendUndAchsenparallel()
	{
		var verlauf = EidraForgeProps.Windrohrverlauf;
		Assert.That(verlauf.Count, Is.GreaterThanOrEqualTo(2));
		for (int index = 1; index < verlauf.Count; index++)
		{
			Vector2 a = verlauf[index - 1];
			Vector2 b = verlauf[index];
			bool nurX = Mathf.Abs(a.y - b.y) < 0.01f && Mathf.Abs(a.x - b.x) > 0.01f;
			bool nurZ = Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) > 0.01f;
			Assert.That(nurX || nurZ, Is.True,
				$"Abschnitt {index} von ({a.x}|{a.y}) nach ({b.x}|{b.y}) ist nicht achsenparallel.");
		}
	}

	[Test]
	public void Windrohr_BeginntInDerBalgkammerUndEndetAnDerEsse()
	{
		Vector2 anfang = EidraForgeProps.Windrohrverlauf.First();
		Vector2 ende = EidraForgeProps.Windrohrverlauf.Last();
		ForgeFlaeche balgkammer = EidraForgeLayout.Flaechen.First(f => f.Name == "Balgkammer");
		Assert.That(anfang.x, Is.InRange(balgkammer.MinX, balgkammer.MaxX));
		Assert.That(anfang.y, Is.InRange(balgkammer.MinZ, balgkammer.MaxZ));
		Assert.That(Vector2.Distance(ende, EidraForgeProps.EssenFuss), Is.LessThan(5f),
			"Das Rohr muss an der Esse enden.");
	}

	[Test]
	public void Kohlenpfannen_StehenAufBodenUndNichtInWegenAnderer()
	{
		foreach (ForgePlatz pfanne in EidraForgeProps.Kohlenpfannen)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(pfanne.X, pfanne.Z, out _), Is.True,
				$"Kohlenpfanne {pfanne.Id} steht nicht auf Boden.");
			foreach (ForgePlatz anker in EidraForgeLayout.Gegneranker)
			{
				Assert.That(Abstand(pfanne, anker), Is.GreaterThan(1.5f), $"{pfanne.Id} steckt in {anker.Id}.");
			}
			foreach (ForgePlatz truhe in EidraForgeLayout.Truhen)
			{
				Assert.That(Abstand(pfanne, truhe), Is.GreaterThan(1.5f), $"{pfanne.Id} steckt in {truhe.Id}.");
			}
		}
	}

	[Test]
	public void Glutadern_LiegenVollstaendigAufBoden()
	{
		foreach (ForgeGlutader ader in EidraForgeProps.Glutadern)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(ader.X, ader.Z, out _), Is.True,
				$"Glutader bei ({ader.X}|{ader.Z}) liegt nicht auf Boden.");
			float halb = ader.Laenge * 0.5f;
			foreach (int richtung in new[] { -1, 1 })
			{
				float endeX = ader.EntlangZ ? ader.X : ader.X + halb * richtung;
				float endeZ = ader.EntlangZ ? ader.Z + halb * richtung : ader.Z;
				Assert.That(EidraForgeLayout.IstAufBoden(endeX, endeZ, out _), Is.True,
					$"Glutader bei ({ader.X}|{ader.Z}) ragt ueber die Bodenkante hinaus.");
			}
		}
	}

	[Test]
	public void AlleAufbauten_StehenAufBodenUndBlockierenNiemanden()
	{
		foreach (ForgeAufbau aufbau in EidraForgeProps.AlleAufbauten)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(aufbau.X, aufbau.Z, out _), Is.True,
				$"{aufbau.Id} steht nicht auf Boden.");
			foreach (ForgePlatz anker in EidraForgeLayout.Gegneranker)
			{
				Assert.That(Punktabstand(aufbau.X, aufbau.Z, anker.X, anker.Z), Is.GreaterThan(1.2f),
					$"{aufbau.Id} steckt in {anker.Id}.");
			}
			foreach (ForgePlatz truhe in EidraForgeLayout.Truhen)
			{
				Assert.That(Punktabstand(aufbau.X, aufbau.Z, truhe.X, truhe.Z), Is.GreaterThan(1.2f),
					$"{aufbau.Id} steckt in {truhe.Id}.");
			}
			foreach (ForgePlatz pfanne in EidraForgeProps.Kohlenpfannen)
			{
				Assert.That(Punktabstand(aufbau.X, aufbau.Z, pfanne.X, pfanne.Z), Is.GreaterThan(1.2f),
					$"{aufbau.Id} steckt in {pfanne.Id}.");
			}
		}
	}

	/// <summary>
	/// Kamerabudget: nur die Esse darf die Wandhoehe ueberragen. Alles andere
	/// wuerde bei 52 Grad Blickwinkel die Spielfigur verdecken.
	/// </summary>
	[Test]
	public void KeinAufbau_UeberragtDieWandhoehe()
	{
		foreach (ForgeAufbau aufbau in EidraForgeProps.AlleAufbauten)
		{
			Assert.That(aufbau.Hoehe, Is.LessThanOrEqualTo(EidraForgeLayout.Wandhoehe),
				$"{aufbau.Id} ist hoeher als die Wand.");
		}
	}

	[Test]
	public void Gelaender_UmschliesstDieGrubeUndLaesstZugaengeFrei()
	{
		var kanten = EidraForgePropBuilder.Gelaenderkanten().ToList();
		Assert.That(kanten, Is.Not.Empty, "Die Grube hat kein Gelaender.");
		foreach (var kante in kanten)
		{
			bool aufTreppeWest = kante.x > -16f && kante.x < -10f && kante.z > 26f && kante.z < 30f;
			bool aufTreppeOst = kante.x > 10f && kante.x < 16f && kante.z > 34f && kante.z < 38f;
			bool aufAbstich = kante.x > -3f && kante.x < 3f && kante.z > 42f;
			Assert.That(aufTreppeWest || aufTreppeOst || aufAbstich, Is.False,
				$"Gelaender versperrt einen Zugang bei ({kante.x}|{kante.z}).");
		}
	}

	/// <summary>
	/// Der Blasebalg ist 8 lang und passt nur laengs in die 8 tiefe Balgkammer.
	/// Sein Stutzen sitzt deshalb an der Nordkante - dort muss das Windrohr
	/// beginnen, sonst haengt es neben dem Balg in der Luft.
	/// </summary>
	[Test]
	public void Windrohr_BeginntAmBlasebalgStutzen()
	{
		ForgeAufbau balg = EidraForgeProps.Einzelstuecke.First(e => e.Id == "forge.prop.blasebalg");
		Vector2 anfang = EidraForgeProps.Windrohrverlauf.First();
		Assert.That(Mathf.Abs(anfang.x - balg.X), Is.LessThan(1f),
			"Das Rohr muss auf der Achse des Blasebalgs beginnen.");
		Assert.That(anfang.y, Is.GreaterThan(balg.Z),
			"Der Stutzen zeigt nach Norden, das Rohr muss dort weiterlaufen.");
	}

	[Test]
	public void Einzelstuecke_StehenAufBodenUndBlockierenKeineTruhe()
	{
		foreach (ForgeAufbau stueck in EidraForgeProps.Einzelstuecke)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(stueck.X, stueck.Z, out _), Is.True,
				$"{stueck.Id} steht nicht auf Boden.");
			foreach (ForgePlatz truhe in EidraForgeLayout.Truhen)
			{
				Assert.That(Punktabstand(stueck.X, stueck.Z, truhe.X, truhe.Z), Is.GreaterThan(1.5f),
					$"{stueck.Id} steckt in {truhe.Id}.");
			}
		}
	}

	private static float Punktabstand(float ax, float az, float bx, float bz)
	{
		return Mathf.Sqrt((ax - bx) * (ax - bx) + (az - bz) * (az - bz));
	}

	private static float Abstand(ForgePlatz a, ForgePlatz b)
	{
		return Mathf.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
	}
}
