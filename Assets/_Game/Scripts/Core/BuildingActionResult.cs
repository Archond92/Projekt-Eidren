namespace Eidren.Core.Services
{
	public enum BuildingActionResult
	{
		Success = 0,
		UnknownBuilding = 1,
		BuildingLocked = 2,
		InvalidLevel = 3,
		OutsideBuildArea = 4,
		BlockedZone = 5,
		Overlap = 6,
		MissingMaterials = 7,
		UnknownInstance = 8,
		StorageNotEmpty = 9,
		RefundInventoryFull = 10,
		EdgeOccupied = 11,
		FloorRequired = 12,
		NaturalGroundRequired = 13,
		MixedFoundation = 14,
		NotMovable = 15,
		BuildingBusy = 16,
		BuildingLimitReached = 17
	}
}
