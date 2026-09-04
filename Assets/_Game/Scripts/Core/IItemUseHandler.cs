using Eidren.Data;

namespace Eidren.Core.Services
{
	public interface IItemUseHandler
	{
		bool TryUse(ItemDefinition item);
	}
}
