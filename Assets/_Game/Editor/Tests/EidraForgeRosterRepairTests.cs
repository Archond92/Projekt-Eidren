using Eidren.Core.Services;
using NUnit.Framework;
using System.Collections.Generic;
using System;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Ein gespeicherter Schmiede-Lauf behält
	/// sein Gegner-Roster bis zum nächsten Reset. Fehlt darin ein Eintrag
	/// aus einer älteren Fassung, spawnt der Gegner in diesem Lauf NIE —
	/// Testermeldung „der Wächter spawnt nicht". RepairRoster ergänzt
	/// fehlende Einträge additiv: nie besiegte Gegner erscheinen, besiegte
	/// bleiben besiegt.
	/// </summary>
	public sealed class EidraForgeRosterRepairTests
	{
		[Test]
		public void RepairRoster_ErgaenztFehlendeGegner()
		{
			ForgeEnemyState[] voll = EidraForgePopulationRules.CreateEnemyStates();
			List<ForgeEnemyState> beschnitten = new List<ForgeEnemyState>();
			foreach (ForgeEnemyState state in voll)
			{
				if (!state.SpawnId.Contains("seal_guardian", StringComparison.Ordinal))
				{
					beschnitten.Add(state);
				}
			}
			beschnitten[0].Defeated = true;
			beschnitten[0].Health = 0f;
			ForgeEnemyState[] repariert = EidraForgePopulationRules.RepairRoster(beschnitten.ToArray());
			Assert.That(repariert.Length, Is.EqualTo(EidraForgePopulationRules.TotalEnemyCount),
				"Der Lauf muss das vollständige Roster tragen.");
			ForgeEnemyState waechter = null;
			foreach (ForgeEnemyState state in repariert)
			{
				if (state.SpawnId.Contains("seal_guardian", StringComparison.Ordinal))
				{
					waechter = state;
				}
			}
			Assert.That(waechter, Is.Not.Null, "Der ergänzte Wächter fehlt weiterhin.");
			Assert.That(waechter.Defeated, Is.False, "Ein nachgerüsteter Gegner gilt als nie besiegt.");
			Assert.That(waechter.Health, Is.GreaterThan(0f));
			Assert.That(repariert[0].Defeated, Is.True, "Bestehende Zustände bleiben unangetastet.");
			Assert.That(repariert[0].Health, Is.Zero);
		}

		[Test]
		public void RepairRoster_LaesstVollstaendigeRosterUnveraendert()
		{
			ForgeEnemyState[] voll = EidraForgePopulationRules.CreateEnemyStates();
			ForgeEnemyState[] repariert = EidraForgePopulationRules.RepairRoster(voll);
			Assert.That(repariert, Is.SameAs(voll), "Ein vollständiges Roster wird nicht kopiert.");
		}
	}
}
