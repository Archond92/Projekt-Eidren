namespace Eidren.Core.Services
{
	public enum TechnologyUnlockResult
	{
		Success = 0,
		UnknownNode = 1,
		AlreadyUnlocked = 2,
		PrerequisiteMissing = 3,
		WrongProgressionStage = 4,
		NotEnoughTechnologyPoints = 5
	}
}
