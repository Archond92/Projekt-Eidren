using Eidren.Data;
using System.Collections.Generic;

namespace Eidren.Eidra
{
	/// <summary>
	/// N04-003: Beschreibungstext einer Faehigkeit fuer den Tooltip. Erzeugt
	/// aus denselben Zahlen, die der Kampf anwendet — ein geschriebener Text
	/// haette dasselbe Problem wie Ignivars Passivanzeige (F32-005): Er
	/// verspricht etwas, das die Daten nicht halten, sobald jemand an der
	/// Balance dreht.
	/// </summary>
	public static class EidraAbilityText
	{
		public static string For(AbilityData ability)
		{
			if (ability == null)
			{
				return string.Empty;
			}
			List<string> teile = new List<string>(3);
			string wirkung = Wirkung(ability);
			if (!string.IsNullOrEmpty(wirkung))
			{
				teile.Add(wirkung);
			}
			// Reichweite 0 heisst „wirkt auf die eigene Figur" (Steinhaut).
			// „Reichweite 0" hinzuschreiben waere irrefuehrender als nichts —
			// an dieser 0 ist die Faehigkeit in F31-016 schon einmal
			// gescheitert, weil eine Pruefung sie falsch gelesen hat.
			if (ability.Range > 0f)
			{
				teile.Add($"Reichweite {ability.Range:0.##}");
			}
			if (ability.Cooldown > 0f)
			{
				teile.Add($"Abklingzeit {ability.Cooldown:0.##} s");
			}
			string kopf = string.IsNullOrWhiteSpace(ability.DisplayName) ? "Fähigkeit" : ability.DisplayName;
			return (teile.Count == 0) ? kopf : (kopf + "  ·  " + string.Join("  ·  ", teile));
		}

		private static string Wirkung(AbilityData ability)
		{
			return ability.ExecutionType switch
			{
				AbilityExecutionType.StaggerStrike => $"{ability.StaggerAmount:0.##} Stagger",
				AbilityExecutionType.Shield => $"{ability.EffectDuration:0.##} s unverwundbar",
				AbilityExecutionType.ShadowStep => $"Sprung {ability.TeleportBehindDistance:0.##} m hinter das Ziel",
				AbilityExecutionType.BackMark => $"Rückentreffer ×{ability.BackDamageMultiplier:0.##} für {ability.EffectDuration:0.##} s",
				AbilityExecutionType.EmberCircle => $"{ability.HealthDamagePerSecond:0.##} Schaden/s über {ability.EffectDuration:0.##} s im Umkreis {ability.Radius:0.##}",
				// F32-004: Gegen ungeschuetzte Ziele traegt der Brand denselben
				// Betrag als Schadensaufschlag — beide Faelle gehoeren in den Text.
				AbilityExecutionType.MoltenBrand => $"Schutz −{ability.ProtectionReduction * 100f:0} %P, sonst +{ability.ProtectionReduction * 100f:0} % Schaden, für {ability.EffectDuration:0.##} s",
				_ => string.Empty
			};
		}
	}
}
