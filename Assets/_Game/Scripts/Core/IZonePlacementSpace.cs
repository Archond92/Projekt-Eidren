using UnityEngine;

namespace Eidren.Core.Services
{
	public interface IZonePlacementSpace
	{
		bool IsPlaceable(Vector3 position, float clearance);
	}
}
