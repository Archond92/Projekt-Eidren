using Eidren.Composition;
using Eidren.Core.Services;
using NUnit.Framework;

namespace Eidren.Tests
{
	/// <summary>
	/// Nagelt die Weltsaat fuer die gesamte PlayMode-Suite fest.
	///
	/// Ohne das wuerfelt jeder Lauf die Zonen neu. Zwei Folgen, beide in der
	/// v0.3.2-Runde eingetreten: Ein gruener Wiederholungslauf beweist nichts,
	/// und ein echter Regressionsfund sieht aus wie Zufall.
	///
	/// Der Haken sitzt hier und nicht in den einzelnen Tests, weil die Saat
	/// gesetzt sein muss, BEVOR eine Sitzung angelegt wird — und das passiert
	/// mitten im Test, beim Laden der Zone. <c>ZoneStateService</c> liest die
	/// Vorgabe beim Anlegen UND bei jedem parameterlosen Reset, also greift
	/// sie auch fuer Sitzungen, die erst waehrend eines Tests entstehen.
	///
	/// Als Assembly-Attribut mit <c>ITestAction</c> war das zuerst gebaut —
	/// Unity fuehrt die nicht aus, der Beweistest
	/// (<c>WeltsaatHakenTests</c>) hat es gezeigt. Ein SetUpFixture im
	/// OBERSTEN Namensraum der Assembly gilt fuer diesen und alle
	/// darunterliegenden; die Tests dieser Assembly liegen in
	/// <c>Eidren.Tests</c> und <c>Eidren.Tests.PlayMode</c>.
	/// </summary>
	[SetUpFixture]
	public sealed class FesteWeltsaat
	{
		[OneTimeSetUp]
		public void VorDerSuite()
		{
			ZoneStateService.DefaultMasterSeed = Eidren.Tests.PlayMode.Testumgebung.Saat;

			// Eine Sitzung, die schon steht, wurde ohne Vorgabe angelegt.
			// Nachziehen — aber nur, solange sie leer ist: Ein Reset auf einer
			// belegten Sitzung wuerfe deren Zonen weg.
			ZoneStateService zonen = EidrenServiceRoot.Instance?.GameSession?.ZoneStates;
			if (zonen != null && zonen.Count == 0)
			{
				zonen.Reset();
			}
		}

		[OneTimeTearDown]
		public void NachDerSuite()
		{
			// Sonst bliebe die Vorgabe bei abgeschalteter Domain-Neuladung im
			// Editor stehen, und wer nach einem Testlauf spielt, bekaeme
			// immer wieder dieselbe Welt.
			ZoneStateService.DefaultMasterSeed = null;
		}
	}
}
