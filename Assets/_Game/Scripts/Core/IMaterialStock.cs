using System.Collections.Generic;

namespace Eidren.Core.Services
{
	public interface IMaterialStock
	{
		int GetTotalAmount(string itemId);

		bool CanApplyTransaction(IReadOnlyList<InventoryItemAmount> removals, string additionItemId, int additionAmount, out InventoryTransactionFailure failure);

		bool TryApplyTransaction(IReadOnlyList<InventoryItemAmount> removals, string additionItemId, int additionAmount, out InventoryTransactionFailure failure);
	}
}
