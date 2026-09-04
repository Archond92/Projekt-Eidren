using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public static class ProtectionRules
	{
		public const float DefaultMaximumProtection = 0.6f;

		public static float TotalProtection(IEnumerable<float> contributions, float maximumProtection = 0.6f)
		{
			if (contributions == null)
			{
				throw new ArgumentNullException("contributions");
			}
			if (maximumProtection < 0f || maximumProtection > 1f)
			{
				throw new ArgumentOutOfRangeException("maximumProtection");
			}
			float num = 0f;
			foreach (float contribution in contributions)
			{
				if (contribution < 0f || contribution > 1f)
				{
					throw new ArgumentOutOfRangeException("contributions", contribution, "Protection contributions must be between 0 and 1.");
				}
				num += contribution;
			}
			return Math.Min(num, maximumProtection);
		}

		/// <summary>
		/// F31-018: Lebenspunkte-Bonus der Ruestung — je Teil so viele
		/// Lebenspunkte, wie es Prozent Schutz beitraegt (Schutz x 100).
		/// Bewusst OHNE die 60-Prozent-Kappung: die gilt der
		/// Schadensreduktion, nicht dem Maximum.
		/// </summary>
		public static float ArmorHealthBonus(IEnumerable<float> contributions)
		{
			if (contributions == null)
			{
				throw new ArgumentNullException("contributions");
			}
			float sum = 0f;
			foreach (float contribution in contributions)
			{
				if (contribution < 0f || contribution > 1f)
				{
					throw new ArgumentOutOfRangeException("contributions", contribution, "Protection contributions must be between 0 and 1.");
				}
				sum += contribution;
			}
			return sum * 100f;
		}

		public static float ReceivedDamage(float baseDamage, float totalProtection)
		{
			if (baseDamage < 0f)
			{
				throw new ArgumentOutOfRangeException("baseDamage");
			}
			if (totalProtection < 0f || totalProtection > 1f)
			{
				throw new ArgumentOutOfRangeException("totalProtection");
			}
			return baseDamage * (1f - totalProtection);
		}
	}
}
