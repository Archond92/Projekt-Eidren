namespace Eidren.Core.Services
{
	public enum InventoryTransactionFailure
	{
		None = 0,
		InvalidRequest = 1,
		UnknownItem = 2,
		MissingItems = 3,
		InventoryFull = 4
	}
}
