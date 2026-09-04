using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	/// <summary>
	/// F32-006: Rechnet aus, wie das aktive Gespann nach einem Zuweisen
	/// aussieht — „setze Instanz X auf Platz i". Reine Logik, damit die
	/// Regel ohne Fenster pruefbar ist.
	///
	/// Das Ergebnis erfuellt den Vertrag von <c>EidraRoster.TrySetActiveTeam</c>:
	/// dichte Liste, keine Duplikate, hoechstens capacity Eintraege.
	/// </summary>
	public static class EidraTeamAssignment
	{
		public static string[] Assign(IReadOnlyList<string> active, int capacity, string instanceId, int slot)
		{
			int num = Math.Clamp(capacity, 0, 2);
			if (num <= 0 || string.IsNullOrWhiteSpace(instanceId))
			{
				return Array.Empty<string>();
			}
			List<string> list = new List<string>(num);
			if (active != null)
			{
				foreach (string id in active)
				{
					if (!string.IsNullOrWhiteSpace(id) && !list.Contains(id) && list.Count < num)
					{
						list.Add(id);
					}
				}
			}
			// Bei Kapazitaet 1 gibt es nur Platz 0 — ein Klick auf den
			// gesperrten zweiten Platz ersetzt dann den einzigen.
			int ziel = Math.Clamp(slot, 0, num - 1);
			int vorhanden = list.IndexOf(instanceId);
			if (vorhanden >= 0)
			{
				if (vorhanden == ziel)
				{
					return list.ToArray();
				}
				if (ziel < list.Count)
				{
					string getauscht = list[ziel];
					list[ziel] = instanceId;
					list[vorhanden] = getauscht;
				}
				else
				{
					list.RemoveAt(vorhanden);
					list.Add(instanceId);
				}
				return list.ToArray();
			}
			if (ziel < list.Count)
			{
				list[ziel] = instanceId;
			}
			else if (list.Count < num)
			{
				list.Add(instanceId);
			}
			else
			{
				list[num - 1] = instanceId;
			}
			return list.ToArray();
		}
	}
}
