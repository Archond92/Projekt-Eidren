namespace Eidren.Core.Services
{
	public enum SaveRequestReason
	{
		HomeBaseEntered = 0,
		BuildingChanged = 1,
		ProductionChanged = 2,
		CraftingCompleted = 3,
		StorageClosed = 4,
		BossDefeated = 5,
		AreaTransitionConfirmed = 6,
		MainMenuRequested = 7,
		PauseReturnHome = 8,
		ApplicationPause = 9,
		ApplicationQuit = 10
	}
}
