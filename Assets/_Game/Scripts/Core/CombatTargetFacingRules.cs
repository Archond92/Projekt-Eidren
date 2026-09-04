using System;
using Eidren.Data;

namespace Eidren.Core
{
	public readonly struct CombatTargetFacingProfile
	{
		public readonly float CorrectionFraction;
		public readonly float MaximumTurnDegrees;

		public CombatTargetFacingProfile(float correctionFraction, float maximumTurnDegrees)
		{
			if (!CombatTargetSelectionSettings.Finite(correctionFraction)
				|| !CombatTargetSelectionSettings.Finite(maximumTurnDegrees)
				|| correctionFraction < 0f || correctionFraction > 1f || maximumTurnDegrees < 0f)
				throw new ArgumentOutOfRangeException(nameof(correctionFraction));
			CorrectionFraction = correctionFraction;
			MaximumTurnDegrees = maximumTurnDegrees;
		}
	}

	/// <summary>Einmalige, waffenspezifische Drehhilfe fuer eine vom Spieler gestartete Aktion.</summary>
	public static class CombatTargetFacingRules
	{
		private static readonly CombatTargetFacingProfile Hammer = new CombatTargetFacingProfile(.70f, 32f);
		private static readonly CombatTargetFacingProfile Daggers = new CombatTargetFacingProfile(.85f, 28f);
		private static readonly CombatTargetFacingProfile Spear = new CombatTargetFacingProfile(.65f, 22f);
		private static readonly CombatTargetFacingProfile Fallback = new CombatTargetFacingProfile(.65f, 22f);

		public static CombatTargetFacingProfile For(WeaponFamily family)
		{
			switch (family)
			{
				case WeaponFamily.Hammer: return Hammer;
				case WeaponFamily.Daggers: return Daggers;
				case WeaponFamily.Spear: return Spear;
				default: return Fallback;
			}
		}

		public static float ResolveTurnDegrees(float targetAngle, CombatTargetFacingProfile profile)
		{
			if (!CombatTargetSelectionSettings.Finite(targetAngle) || targetAngle <= 0f) return 0f;
			return Math.Min(targetAngle * profile.CorrectionFraction, profile.MaximumTurnDegrees);
		}
	}
}

