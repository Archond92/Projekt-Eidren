using System.Collections.Generic;

namespace Eidren.Core.Services
{
	public interface IItemContainer
	{
		int SlotCapacity { get; }

		bool TryGetSlot(int index, out ItemStack stack);

		ItemStack[] ExportSlots();

		bool TryImportSlots(IReadOnlyList<ItemStack> slots, out string error);
	}
}
