using Eidren.Core.BuildGrid;
using Eidren.Data;

namespace Eidren.Core.Services
{
	internal static class BuildingGroundRuleCheck
	{
		public static BuildingActionResult Evaluate(GridOccupancy occupancy, GridFootprint footprint, BuildingGroundRule rule)
		{
			int num = 0;
			int num2 = 0;
			foreach (GridCoordinate item in footprint.Cells())
			{
				num2++;
				if (occupancy.IsCellOccupied(BuildingPlacementKind.Floor, item))
				{
					num++;
				}
			}
			switch (rule)
			{
			case BuildingGroundRule.RequiresFloor:
				return (num != num2) ? BuildingActionResult.FloorRequired : BuildingActionResult.Success;
			case BuildingGroundRule.GroundOnly:
			case BuildingGroundRule.OutdoorOnly:
				return (num != 0) ? BuildingActionResult.NaturalGroundRequired : BuildingActionResult.Success;
			case BuildingGroundRule.GroundOrFloor:
				return (num != 0 && num != num2) ? BuildingActionResult.MixedFoundation : BuildingActionResult.Success;
			default:
				return BuildingActionResult.Success;
			}
		}
	}
}
