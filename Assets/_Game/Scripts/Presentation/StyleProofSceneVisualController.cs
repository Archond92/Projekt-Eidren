using UnityEngine;

namespace Eidren.Presentation
{
	public sealed class StyleProofSceneVisualController : MonoBehaviour
	{
		private static readonly Color Backdrop = new Color(0.035f, 0.065f, 0.07f, 1f);

		private Camera _camera;

		private void LateUpdate()
		{
			if (_camera == null)
			{
				_camera = Camera.main;
			}
			if (!(_camera == null))
			{
				_camera.clearFlags = CameraClearFlags.Color;
				_camera.backgroundColor = Backdrop;
			}
		}
	}
}
