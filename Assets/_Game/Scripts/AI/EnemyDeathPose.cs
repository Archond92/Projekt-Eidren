using UnityEngine;

namespace Eidren.AI
{
	public static class EnemyDeathPose
	{
		private const float TiltDegrees = 78f;

		private static readonly Vector3 Squash = new Vector3(0.55f, 1.1f, 1f);

		public static void ApplyTipOver(Transform visualRoot, Quaternion restRotation)
		{
			if (!(visualRoot == null))
			{
				visualRoot.localRotation = restRotation * Quaternion.Euler(0f, 0f, 78f);
				visualRoot.localScale = Vector3.Scale(visualRoot.localScale, Squash);
			}
		}
	}
}
