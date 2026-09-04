using System;

namespace Eidren.Core.Services
{
	public static class DurabilityRules
	{
		public static int WeaponWear(bool successfulAttack)
		{
			return successfulAttack ? 1 : 0;
		}

		public static int ArmorWear(float receivedDamage)
		{
			return (receivedDamage > 0f) ? 1 : 0;
		}

		public static int ToolWear(int toolTier, int resourceTier)
		{
			ValidateTier(toolTier, "toolTier");
			ValidateTier(resourceTier, "resourceTier");
			int num = toolTier - resourceTier;
			if (num <= -1)
			{
				return 4;
			}
			int result;
			switch (num)
			{
			case 0:
				return 3;
			default:
				result = 1;
				break;
			case 1:
				result = 2;
				break;
			}
			return result;
		}

		public static DurabilityResult Apply(int current, int wear)
		{
			if (current <= 0)
			{
				throw new ArgumentOutOfRangeException("current");
			}
			if (wear < 0)
			{
				throw new ArgumentOutOfRangeException("wear");
			}
			return new DurabilityResult(Math.Max(0, current - wear));
		}

		private static void ValidateTier(int tier, string parameterName)
		{
			if (tier < 0)
			{
				throw new ArgumentOutOfRangeException(parameterName);
			}
		}
	}

	public readonly struct DurabilityResult
	{
		public int Remaining { get; }

		public bool IsBroken => Remaining == 0;

		public DurabilityResult(int remaining)
		{
			Remaining = remaining;
		}
	}
}
