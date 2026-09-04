using Eidren.Data;
using System;

namespace Eidren.Core.Services
{
	public static class WeaponCombatRules
	{
		public const float DaggerBackMultiplier = 1.75f;

		public const float SpearOuterRangeMultiplier = 1.25f;

		public const float SpearOuterRangeStart = 2f / 3f;

		public static WeaponHitModifiers Resolve(WeaponSignature signature, int comboIndex, bool isBackAttack, float targetDistance, float attackRange)
		{
			if (comboIndex < 0)
			{
				throw new ArgumentOutOfRangeException("comboIndex");
			}
			if (targetDistance < 0f)
			{
				throw new ArgumentOutOfRangeException("targetDistance");
			}
			if (attackRange <= 0f)
			{
				throw new ArgumentOutOfRangeException("attackRange");
			}
			switch (signature)
			{
			case WeaponSignature.Impact:
				if (comboIndex == 2)
				{
					return new WeaponHitModifiers(1f, 1f, WeaponSignatureCue.HammerImpact);
				}
				break;
			case WeaponSignature.Ambush:
				if (isBackAttack)
				{
					return new WeaponHitModifiers(1.75f, 1f, WeaponSignatureCue.DaggerAmbush);
				}
				break;
			case WeaponSignature.ReachWindow:
				if (targetDistance + 0.0001f >= attackRange * (2f / 3f))
				{
					return new WeaponHitModifiers(1.25f, 1f, WeaponSignatureCue.SpearReachWindow);
				}
				break;
			}
			return new WeaponHitModifiers(1f, 1f, WeaponSignatureCue.None);
		}
	}

	public readonly struct WeaponHitModifiers
	{
		public float HealthMultiplier { get; }

		public float StaggerMultiplier { get; }

		public WeaponSignatureCue Cue { get; }

		public WeaponHitModifiers(float healthMultiplier, float staggerMultiplier, WeaponSignatureCue cue)
		{
			HealthMultiplier = healthMultiplier;
			StaggerMultiplier = staggerMultiplier;
			Cue = cue;
		}
	}
}
