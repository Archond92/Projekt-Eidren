using UnityEngine;

namespace Eidren.Presentation
{
	public interface ICameraBoundsProvider
	{
		Bounds CameraBounds { get; }
	}
}
