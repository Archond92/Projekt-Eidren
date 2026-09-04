namespace Eidren.Core.Services
{
	public readonly struct BuildingPreview
	{
		public BuildingPreviewState State { get; }

		public BuildingActionResult Reason { get; }

		public string MissingItemId { get; }

		public BuildingPreview(BuildingActionResult reason, string missingItemId = null)
		{
			Reason = reason;
			MissingItemId = missingItemId ?? string.Empty;
			State = StateFor(reason);
		}

		public static BuildingPreviewState StateFor(BuildingActionResult result)
		{
			if (1 == 0)
			{
			}
			BuildingPreviewState result2;
			switch (result)
			{
			case BuildingActionResult.Success:
				result2 = BuildingPreviewState.Valid;
				break;
			case BuildingActionResult.BuildingLocked:
			case BuildingActionResult.MissingMaterials:
				result2 = BuildingPreviewState.Conditional;
				break;
			default:
				result2 = BuildingPreviewState.Invalid;
				break;
			}
			if (1 == 0)
			{
			}
			return result2;
		}
	}
}
