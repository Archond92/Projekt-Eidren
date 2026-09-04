using Eidren.Core.Services;
using System;

namespace Eidren.Composition
{
	public static class ZoneEntryRules
	{
		public static MapExitDirection Opposite(MapExitDirection exitDirection)
		{
			if (1 == 0)
			{
			}
			MapExitDirection result = exitDirection switch
			{
				MapExitDirection.North => MapExitDirection.South, 
				MapExitDirection.East => MapExitDirection.West, 
				MapExitDirection.South => MapExitDirection.North, 
				MapExitDirection.West => MapExitDirection.East, 
				_ => MapExitDirection.None, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		public static bool ShouldUseDirectionalSpawn(GameSession session, string loadedSceneKey)
		{
			return session != null && !session.IsWorldTravelPending && session.LastValidAreaStatus == AreaStatus.TransitionConfirmed && session.LastExitDirection != MapExitDirection.None && string.Equals(session.RequestedTargetScene, loadedSceneKey, StringComparison.Ordinal);
		}
	}
}
