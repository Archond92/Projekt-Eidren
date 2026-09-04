using System;
using Eidren.Core;
using Eidren.Data;
using NUnit.Framework;

namespace Eidren.Tests.EditMode
{
	public sealed class CombatTargetFacingRulesTests
	{
		[TestCase(WeaponFamily.Hammer, .70f, 32f)]
		[TestCase(WeaponFamily.Daggers, .85f, 28f)]
		[TestCase(WeaponFamily.Spear, .65f, 22f)]
		public void ProfilesAreExplicitPerWeaponFamily(WeaponFamily family, float fraction, float maximum)
		{
			CombatTargetFacingProfile profile = CombatTargetFacingRules.For(family);
			Assert.That(profile.CorrectionFraction, Is.EqualTo(fraction));
			Assert.That(profile.MaximumTurnDegrees, Is.EqualTo(maximum));
		}

		[TestCase(10f, .5f, 30f, 5f)]
		[TestCase(100f, .5f, 30f, 30f)]
		[TestCase(0f, .5f, 30f, 0f)]
		[TestCase(-10f, .5f, 30f, 0f)]
		public void TurnIsProportionalAndCapped(float angle, float fraction, float maximum, float expected)
		{
			float result = CombatTargetFacingRules.ResolveTurnDegrees(angle, new CombatTargetFacingProfile(fraction, maximum));
			Assert.That(result, Is.EqualTo(expected).Within(.001f));
		}

		[Test] public void InvalidProfileCannotEnterRuntime()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new CombatTargetFacingProfile(float.NaN, 20f));
			Assert.Throws<ArgumentOutOfRangeException>(() => new CombatTargetFacingProfile(1.1f, 20f));
			Assert.Throws<ArgumentOutOfRangeException>(() => new CombatTargetFacingProfile(.5f, -1f));
		}
	}
}

