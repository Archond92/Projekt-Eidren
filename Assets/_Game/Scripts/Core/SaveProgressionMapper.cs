using System;

namespace Eidren.Core.Services
{
	public static class SaveProgressionMapper
	{
		public static SaveProgressionData ToSave(PlayerProgressionRuntimeState source)
		{
			PlayerProgressionRuntimeState playerProgressionRuntimeState = source ?? new PlayerProgressionRuntimeState();
			return new SaveProgressionData
			{
				stage = playerProgressionRuntimeState.Stage,
				level = playerProgressionRuntimeState.Level,
				experience = playerProgressionRuntimeState.Experience,
				availableTechnologyPoints = playerProgressionRuntimeState.AvailableTechnologyPoints,
				spentTechnologyPoints = playerProgressionRuntimeState.SpentTechnologyPoints,
				firstCraftedRecipeIds = (playerProgressionRuntimeState.FirstCraftedRecipeIds ?? Array.Empty<string>()),
				firstBuiltBuildingIds = (playerProgressionRuntimeState.FirstBuiltBuildingIds ?? Array.Empty<string>()),
				unlockedTechnologyNodeIds = (playerProgressionRuntimeState.UnlockedTechnologyNodeIds ?? Array.Empty<string>()),
				knownBlueprintIds = (playerProgressionRuntimeState.KnownBlueprintIds ?? Array.Empty<string>())
			};
		}

		public static bool TryToRuntime(SaveProgressionData source, out PlayerProgressionRuntimeState result, out string error)
		{
			if (source == null || source.stage < 1 || source.level < 1 || source.experience < 0 || source.availableTechnologyPoints < 0 || source.spentTechnologyPoints < 0)
			{
				result = null;
				error = "Save progression data is invalid.";
				return false;
			}
			result = new PlayerProgressionRuntimeState
			{
				Stage = source.stage,
				Level = source.level,
				Experience = source.experience,
				AvailableTechnologyPoints = source.availableTechnologyPoints,
				SpentTechnologyPoints = source.spentTechnologyPoints,
				FirstCraftedRecipeIds = (source.firstCraftedRecipeIds ?? Array.Empty<string>()),
				FirstBuiltBuildingIds = (source.firstBuiltBuildingIds ?? Array.Empty<string>()),
				UnlockedTechnologyNodeIds = (source.unlockedTechnologyNodeIds ?? Array.Empty<string>()),
				KnownBlueprintIds = (source.knownBlueprintIds ?? Array.Empty<string>())
			};
			error = string.Empty;
			return true;
		}
	}
}
