using Eidren.Core.Services;
using NUnit.Framework;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Feste Weltsaat für die Testsuite (Empfehlung aus der v0.3.2-Runde).
	///
	/// Die Zonen werden pro Lauf neu gewürfelt — der Saatgeber wurde bisher
	/// ungesetzt angelegt. Folge: Ein grüner Wiederholungslauf bewies nichts,
	/// und ein echter Regressionsfund sah aus wie Zufall. Beides ist in dieser
	/// Runde vorgekommen.
	///
	/// Der entscheidende Punkt ist <see cref="ZoneStateService.Reset"/> OHNE
	/// Argument: Genau so ruft <c>GameSession.StartNewGame</c> auf. Würde der
	/// Aufruf die Vorgabesaat verwerfen, wäre jede Sitzung trotz gesetzter Saat
	/// wieder zufällig — das Festnageln liefe ins Leere.
	/// </summary>
	public sealed class WeltsaatTests
	{
		[TearDown]
		public void Aufraeumen()
		{
			ZoneStateService.DefaultMasterSeed = null;
		}

		[Test]
		public void OhneVorgabe_BleibtDieWeltZufaellig()
		{
			Assert.That(ZoneStateService.DefaultMasterSeed, Is.Null,
				"Produktiv darf keine feste Saat gesetzt sein — sonst spielt jeder dieselbe Welt.");
		}

		[Test]
		public void MitVorgabe_WuerfelnZweiSitzungenDieselbeWelt()
		{
			ZoneStateService.DefaultMasterSeed = 4711;

			Assert.That(ErsteSaat(new ZoneStateService()), Is.EqualTo(ErsteSaat(new ZoneStateService())),
				"Bei gesetzter Vorgabe muss jede neue Sitzung dieselbe Welt würfeln.");
		}

		[Test]
		public void ResetOhneArgument_BehaeltDieVorgabe()
		{
			ZoneStateService.DefaultMasterSeed = 4711;
			ZoneStateService zonen = new ZoneStateService();
			int vorher = ErsteSaat(zonen);

			// Genau dieser Aufruf steht in GameSession.StartNewGame.
			zonen.Reset();

			Assert.That(ErsteSaat(zonen), Is.EqualTo(vorher),
				"Reset() ohne Argument darf die Vorgabesaat nicht wegwerfen.");
		}

		[Test]
		public void ResetMitArgument_SchlaegtDieVorgabe()
		{
			ZoneStateService.DefaultMasterSeed = 4711;
			ZoneStateService zonen = new ZoneStateService();
			zonen.Reset(1234);
			int ausdruecklich = ErsteSaat(zonen);

			ZoneStateService vergleich = new ZoneStateService();
			vergleich.Reset(1234);

			Assert.That(ausdruecklich, Is.EqualTo(ErsteSaat(vergleich)),
				"Eine ausdrücklich übergebene Saat muss die Vorgabe überstimmen.");
			Assert.That(ausdruecklich, Is.Not.EqualTo(ErsteSaat(new ZoneStateService())),
				"Sonst wäre die ausdrückliche Saat wirkungslos.");
		}

		private static int ErsteSaat(ZoneStateService zonen)
		{
			return zonen.GetOrCreate("zone_probe", 1).Seed;
		}
	}
}
