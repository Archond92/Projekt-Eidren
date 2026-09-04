using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public static class EidraForgePopulationRules
	{
		public const int TotalEnemyCount = 24;

		public static ForgeEnemyState[] CreateEnemyStates()
		{
			List<ForgeEnemyState> list = new List<ForgeEnemyState>(24);
			Add(list, "ember_eater", 12, EidraForgeBalance.Get("enemy.ember_eater").Health);
			Add(list, "ash_runner", 6, EidraForgeBalance.Get("enemy.ash_runner").Health);
			Add(list, "forge_guardian", 4, EidraForgeBalance.Get("enemy.forge_guardian").Health);
			Add(list, "seal_guardian", 1, EidraForgeBalance.Get("enemy.seal_guardian").Health);
			Add(list, "core_guardian", 1, EidraForgeBalance.Get("enemy.core_guardian").Health);
			return list.ToArray();
		}

		/// <summary>
		/// Ergänzt in einem gespeicherten Lauf-Roster die Einträge, die eine
		/// ältere Fassung noch nicht kannte — additiv: nie besiegte Gegner
		/// erscheinen mit vollem Leben, bestehende Zustände bleiben
		/// unangetastet. Ein vollständiges Roster kommt unverändert zurück.
		/// </summary>
		public static ForgeEnemyState[] RepairRoster(ForgeEnemyState[] existing)
		{
			ForgeEnemyState[] vorhanden = existing ?? Array.Empty<ForgeEnemyState>();
			HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
			foreach (ForgeEnemyState state in vorhanden)
			{
				if (state != null && !string.IsNullOrEmpty(state.SpawnId))
				{
					ids.Add(state.SpawnId);
				}
			}
			List<ForgeEnemyState> fehlend = null;
			foreach (ForgeEnemyState soll in CreateEnemyStates())
			{
				if (!ids.Contains(soll.SpawnId))
				{
					(fehlend ?? (fehlend = new List<ForgeEnemyState>())).Add(soll);
				}
			}
			if (fehlend == null)
			{
				return existing;
			}
			List<ForgeEnemyState> ergebnis = new List<ForgeEnemyState>(vorhanden.Length + fehlend.Count);
			ergebnis.AddRange(vorhanden);
			ergebnis.AddRange(fehlend);
			return ergebnis.ToArray();
		}

		public static bool TryGetReward(string spawnId, out int experience, out int marks)
		{
			experience = 0;
			marks = 0;
			if (string.IsNullOrWhiteSpace(spawnId))
			{
				return false;
			}
			if (spawnId.Contains("ember_eater", StringComparison.Ordinal))
			{
				return Values(EidraForgeBalance.Get("enemy.ember_eater"), out experience, out marks);
			}
			if (spawnId.Contains("ash_runner", StringComparison.Ordinal))
			{
				return Values(EidraForgeBalance.Get("enemy.ash_runner"), out experience, out marks);
			}
			if (spawnId.Contains("forge_guardian", StringComparison.Ordinal))
			{
				return Values(EidraForgeBalance.Get("enemy.forge_guardian"), out experience, out marks);
			}
			if (spawnId.Contains("seal_guardian", StringComparison.Ordinal))
			{
				return Values(EidraForgeBalance.Get("enemy.seal_guardian"), out experience, out marks);
			}
			if (spawnId.Contains("core_guardian", StringComparison.Ordinal))
			{
				return Values(EidraForgeBalance.Get("enemy.core_guardian"), out experience, out marks);
			}
			return false;
		}

		private static void Add(List<ForgeEnemyState> target, string stem, int count, float health)
		{
			for (int i = 1; i <= count; i++)
			{
				target.Add(new ForgeEnemyState
				{
					SpawnId = $"forge.enemy.{stem}.{i:00}",
					Health = health
				});
			}
		}

		private static bool Values(ForgeEnemyBalance value, out int experience, out int marks)
		{
			experience = value.Experience;
			marks = value.Marks;
			return true;
		}
	}

	public readonly struct EidraForgeEnemyReward
	{
		public int Experience { get; }

		public ItemStack MarkDrop { get; }

		public EidraForgeEnemyReward(int experience, ItemStack markDrop)
		{
			Experience = experience;
			MarkDrop = markDrop;
		}
	}
}
