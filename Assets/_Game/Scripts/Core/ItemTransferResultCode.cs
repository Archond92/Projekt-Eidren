namespace Eidren.Core.Services
{
	public enum ItemTransferResultCode
	{
		Success = 0,
		Partial = 1,
		SourceEmpty = 2,
		TargetFull = 3,
		InvalidRequest = 4,
		Busy = 5
	}
}
