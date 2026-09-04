using Eidren.Editor;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

/// <summary>
/// Invarianten der Verliesbeleuchtung. Die erste bildet eine harte Rendergrenze
/// ab, keine Stilfrage: m_AdditionalLightsPerObjectLimit steht auf 4.
/// </summary>
public sealed class EidraForgeLichtTests
{
	[Test]
	public void KeineBodenkachel_WirdVonMehrAlsVierLichternErreicht()
	{
		foreach (ForgeFlaeche flaeche in EidraForgeLayout.Flaechen)
		{
			foreach (var kachel in EidraForgeGeometryBuilder.Kacheln(flaeche))
			{
				int treffer = 0;
				string namen = string.Empty;
				foreach (ForgeLicht licht in EidraForgeLicht.Punktlichter)
				{
					float abstand = Mathf.Sqrt(
						(licht.X - kachel.x) * (licht.X - kachel.x) +
						(licht.Z - kachel.z) * (licht.Z - kachel.z));
					if (abstand <= licht.Reichweite)
					{
						treffer++;
						namen += licht.Id + " ";
					}
				}
				Assert.That(treffer, Is.LessThanOrEqualTo(4),
					$"Kachel ({kachel.x}|{kachel.z}) wird von {treffer} Lichtern erreicht: {namen}");
			}
		}
	}

	/// <summary>
	/// Ein Raum ohne Lichtquelle ist unbespielbar: die 5,5 hohen Waende werfen
	/// ihren Schatten hinein, und das Umgebungslicht liegt bei 0,055.
	/// </summary>
	[Test]
	public void JederKampfraum_HatMindestensEineLichtquelle()
	{
		string[] pflicht = { "Windfang", "Balgkammer", "MasselI", "MasselII", "EinbruchB", "RingSued", "Hammerwerk" };
		foreach (string name in pflicht)
		{
			ForgeFlaeche raum = EidraForgeLayout.Flaechen.First(f => f.Name == name);
			bool beleuchtet = EidraForgeLicht.Punktlichter.Any(l =>
				l.X >= raum.MinX - l.Reichweite && l.X <= raum.MaxX + l.Reichweite &&
				l.Z >= raum.MinZ - l.Reichweite && l.Z <= raum.MaxZ + l.Reichweite);
			Assert.That(beleuchtet, Is.True, $"{name} hat keine Lichtquelle - der Raum waere unbespielbar.");
		}
	}

	/// <summary>Hoechstens drei Lichtfarben im Verlies, Blau genau einmal.</summary>
	[Test]
	public void DieFarbregel_WirdEingehalten()
	{
		var farben = EidraForgeLicht.Punktlichter
			.Select(l => $"{l.Farbe.r:0.00}/{l.Farbe.g:0.00}/{l.Farbe.b:0.00}")
			.Distinct()
			.ToList();
		Assert.That(farben.Count, Is.LessThanOrEqualTo(3), "Zu viele Lichtfarben: " + string.Join(", ", farben));
		int kalte = EidraForgeLicht.Punktlichter.Count(l => l.Farbe.b > l.Farbe.r);
		Assert.That(kalte, Is.EqualTo(1), "Kaltes Licht darf genau einmal vorkommen.");
	}
}
