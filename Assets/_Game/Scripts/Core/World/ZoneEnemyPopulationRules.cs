using UnityEngine;
using Random = System.Random;

namespace Eidren.Core.Services
{
	/// <summary>
	/// F31-009: Regeln für die Gegnerbesetzung der Außenzonen. Die Soll-Anzahl
	/// folgt der Gefahrenstufe der Weltkarte (3 x Gefahr - 1), ein Teil der
	/// Gegner bewacht die pro Lauf gewürfelten Weltkisten in einem
	/// Abstandsring: nah genug, dass die Kiste bewacht wirkt, aber nicht auf
	/// der Kiste, damit der Zugriff frei bleibt.
	/// </summary>
	public static class ZoneEnemyPopulationRules
	{
		public const float ChestGuardMinDistance = 3.5f;

		// Unterhalb der Witterungsreichweite der Gegner (10), damit die Wache
		// den Zugriff auf die Kiste sicher bemerkt.
		public const float ChestGuardMaxDistance = 7f;

		public static int TargetTotal(int dangerLevel)
		{
			return Mathf.Max(0, 3 * dangerLevel - 1);
		}

		/// <summary>
		/// Höchstens eine Wache je Kiste und höchstens die Hälfte der
		/// Besetzung — sonst räumen die Wachen den Rest des Gebiets leer.
		/// </summary>
		public static int ChestGuardBudget(int chestCount, int regularCount)
		{
			return Mathf.Clamp(regularCount / 2, 0, chestCount);
		}

		public static Vector3 ChestGuardCandidate(Vector3 chestPosition, Random random)
		{
			float winkel = (float)random.NextDouble() * Mathf.PI * 2f;
			float abstand = Mathf.Lerp(ChestGuardMinDistance, ChestGuardMaxDistance, (float)random.NextDouble());
			return chestPosition + new Vector3(Mathf.Cos(winkel) * abstand, 0f, Mathf.Sin(winkel) * abstand);
		}
	}
}
