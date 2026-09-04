namespace Eidren.Core.Services
{
	public enum CraftingResultCode
	{
		Success = 0,
		UnknownRecipe = 1,
		RecipeLocked = 2,
		WrongStation = 3,
		MissingIngredients = 4,
		InventoryFull = 5,
		UpgradeAlreadyOwned = 6,
		Busy = 7,
		InvalidRecipe = 8
	}
}
