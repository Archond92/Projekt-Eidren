using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public static class IgnivarAbilityRules
	{
		public const float EmberCircleRange = 8f;

		public const float EmberCircleRadius = 2.75f;

		public const float EmberCircleDuration = 5f;

		public const float EmberCircleDamagePerSecond = 6f;

		public const float EmberCircleCooldown = 10f;

		public const float MoltenBrandRange = 10f;

		public const float MoltenBrandDuration = 6f;

		public const float MoltenBrandCooldown = 12f;

		public const float MoltenBrandReduction = 0.15f;

		public static float ProtectionWithMoltenBrand(float protection)
		{
			if (protection < 0f || protection > 1f)
			{
				throw new ArgumentOutOfRangeException("protection");
			}
			return Math.Max(0f, protection - 0.15f);
		}
	}

	public sealed class EmberCircleDamageBudget
	{
		private readonly Dictionary<string, int> _ticks = new Dictionary<string, int>(StringComparer.Ordinal);

		private readonly float _damagePerTick;

		private readonly int _maximumTicks;

		public EmberCircleDamageBudget(float damagePerTick = 6f, int maximumTicks = 5)
		{
			_damagePerTick = Math.Max(0f, damagePerTick);
			_maximumTicks = Math.Max(0, maximumTicks);
		}

		public float TryApplyTick(string targetId)
		{
			if (string.IsNullOrWhiteSpace(targetId))
			{
				return 0f;
			}
			_ticks.TryGetValue(targetId, out var value);
			if (value >= _maximumTicks)
			{
				return 0f;
			}
			_ticks[targetId] = value + 1;
			return _damagePerTick;
		}
	}
}
