using System;

namespace Eidren.Core.Services
{
	public static class HarvestProgressionRules
	{
		public static bool CanHarvest(int resourceTier, int? toolTier)
		{
			ValidateTier(resourceTier, "resourceTier");
			if (!toolTier.HasValue)
			{
				return resourceTier == 0;
			}
			ValidateTier(toolTier.Value, "toolTier");
			return toolTier.Value >= MinimumToolTier(resourceTier);
		}

		public static int Yield(int resourceTier, int? toolTier)
		{
			if (!CanHarvest(resourceTier, toolTier))
			{
				return 0;
			}
			if (!toolTier.HasValue)
			{
				return 1;
			}
			int val = 2 + toolTier.Value - MinimumToolTier(resourceTier);
			return Math.Min(4, val);
		}

		public static ToolStrain Strain(int resourceTier, int? toolTier)
		{
			if (!CanHarvest(resourceTier, toolTier))
			{
				return ToolStrain.Insufficient;
			}
			if (!toolTier.HasValue)
			{
				return ToolStrain.High;
			}
			int num = toolTier.Value - resourceTier;
			if (num < 0)
			{
				return ToolStrain.High;
			}
			return (num == 0) ? ToolStrain.Normal : ToolStrain.Low;
		}

		private static int MinimumToolTier(int resourceTier)
		{
			return Math.Max(0, resourceTier - 1);
		}

		private static void ValidateTier(int tier, string parameterName)
		{
			if (tier < 0)
			{
				throw new ArgumentOutOfRangeException(parameterName);
			}
		}
	}
}
