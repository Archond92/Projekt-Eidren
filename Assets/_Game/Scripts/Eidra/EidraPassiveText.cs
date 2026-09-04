using Eidren.Data;
using System.Collections.Generic;

namespace Eidren.Eidra
{
	/// <summary>
	/// Passivbeschreibung fuer das HUD. Ausgelagert, damit sie pruefbar ist —
	/// die Fassung im EidraTeamController war eine Eigenschaft und liess sich
	/// ohne kompletten Dienst nicht testen.
	/// </summary>
	public static class EidraPassiveText
	{
		/// <summary>
		/// Schadensreduktion der Defend-Rolle. EINE Quelle fuer Wirkung und
		/// Anzeige — vorher stand die 0,78 im Kampfweg und die daraus
		/// abgeleiteten 22 % getrennt davon im Text.
		/// </summary>
		public const float DefendDamageTakenMultiplier = 0.78f;

		public static string For(EidraData data)
		{
			if (data == null)
			{
				return "NOCH KEIN EIDRA IM TEAM";
			}
			List<string> teile = new List<string>(4);
			if (data.Role == EidraRole.Defend)
			{
				teile.Add($"{(1f - DefendDamageTakenMultiplier) * 100f:0}% SCHADENSREDUKTION");
				teile.Add("AUTONOMER STAGGER");
			}
			else
			{
				teile.Add("AUTONOME ANGRIFFE");
			}
			// Nur ansagen, was die Daten auch hergeben — sonst steht wieder
			// eine feste Zahl im HUD, die niemand einloest.
			if (data.PassiveBackDamageMultiplier > 1f)
			{
				teile.Add($"+{(data.PassiveBackDamageMultiplier - 1f) * 100f:0}% RÜCKENSCHADEN");
			}
			if (data.PassiveStaggerMultiplier > 1f)
			{
				teile.Add($"+{(data.PassiveStaggerMultiplier - 1f) * 100f:0}% STAGGER");
			}
			if (data.PassiveBurnFraction > 0f && data.PassiveBurnSeconds > 0)
			{
				teile.Add($"TREFFER ENTZÜNDEN  ·  {data.PassiveBurnFraction * 100f:0}% ÜBER {data.PassiveBurnSeconds} S");
			}
			return "PASSIV  ·  " + string.Join("  ·  ", teile);
		}
	}
}
