using Eidren.Core.Services;
using NUnit.Framework;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// F32-006: Ein gefangenes Eidra jenseits der zwei aktiven Plätze war
	/// nicht mehr erreichbar — es gab keine Möglichkeit, das Gespann zu
	/// ändern. Diese Tests halten die Zuweisungsregel fest, die das Fenster
	/// benutzt; sie muss den Vertrag des Rosters erfüllen (dichte Liste,
	/// keine Duplikate, höchstens zwei Einträge).
	/// </summary>
	public sealed class Fixrunde032TeamTests
	{
		[Test]
		public void FreierPlatz_NimmtDasEidraAuf()
		{
			string[] neu = EidraTeamAssignment.Assign(new string[1] { "terrock.001" }, 2, "noctarion.001", 1);
			Assert.That(neu, Is.EqualTo(new string[2] { "terrock.001", "noctarion.001" }));
		}

		[Test]
		public void BelegterPlatz_WirdErsetzt()
		{
			string[] neu = EidraTeamAssignment.Assign(new string[2] { "terrock.001", "noctarion.002" }, 2, "noctarion.001", 1);
			Assert.That(neu, Is.EqualTo(new string[2] { "terrock.001", "noctarion.001" }),
				"Der Bankspieler nimmt den Platz ein, der bisherige rutscht auf die Bank.");
		}

		[Test]
		public void BereitsAktives_Eidra_TauschtDiePlaetze()
		{
			string[] neu = EidraTeamAssignment.Assign(new string[2] { "terrock.001", "noctarion.002" }, 2, "noctarion.002", 0);
			Assert.That(neu, Is.EqualTo(new string[2] { "noctarion.002", "terrock.001" }),
				"Wer schon aktiv ist, tauscht mit dem anderen Platz statt sich zu verdoppeln.");
		}

		[Test]
		public void ZweiterPlatz_OhneFreischaltung_BleibtZu()
		{
			string[] neu = EidraTeamAssignment.Assign(new string[1] { "terrock.001" }, 1, "noctarion.001", 1);
			Assert.That(neu, Is.EqualTo(new string[1] { "noctarion.001" }),
				"Bei Kapazität 1 gibt es keinen zweiten Platz — die Zuweisung ersetzt den einzigen.");
		}

		[Test]
		public void DasErgebnis_HatNieDoppelteEintraege()
		{
			string[] neu = EidraTeamAssignment.Assign(new string[2] { "terrock.001", "noctarion.002" }, 2, "terrock.001", 1);
			Assert.That(neu, Is.EqualTo(new string[2] { "noctarion.002", "terrock.001" }),
				"Dasselbe Eidra auf den anderen Platz zu setzen tauscht, es dupliziert nicht.");
		}
	}
}
