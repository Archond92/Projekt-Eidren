using Eidren.Core.Services;
using System;

namespace Eidren.Interaction
{
	public interface IInspectableContainer : IItemContainer, IInteractable
	{
		string ContainerDisplayName { get; }

		event Action<StorageContainerChange> Changed;
	}
}
