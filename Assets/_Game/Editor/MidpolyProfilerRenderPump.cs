using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Continuously renders the temporary profiler camera while Play Mode is
	/// active. This keeps profiling deterministic when the Game view is hidden.
	/// </summary>
	[InitializeOnLoad]
	internal static class MidpolyProfilerRenderPump
	{
		private static RenderTexture _target;

		static MidpolyProfilerRenderPump()
		{
			EditorApplication.update -= Render;
			EditorApplication.update += Render;
			EditorApplication.playModeStateChanged -= OnPlayModeChanged;
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
		}

		private static void Render()
		{
			if (!EditorApplication.isPlaying) return;
			GameObject cameraObject = GameObject.Find("ProfilerCamera");
			Camera camera = cameraObject == null ? null : cameraObject.GetComponent<Camera>();
			if (camera == null) return;

			if (_target == null)
			{
				_target = new RenderTexture(1280, 720, 24, RenderTextureFormat.Default)
				{
					name = "MidpolyProfilerTarget",
					hideFlags = HideFlags.DontSave,
					antiAliasing = 1
				};
				_target.Create();
			}

			camera.enabled = false;
			camera.targetTexture = _target;
			camera.Render();
		}

		private static void OnPlayModeChanged(PlayModeStateChange state)
		{
			if (state != PlayModeStateChange.ExitingPlayMode && state != PlayModeStateChange.EnteredEditMode) return;
			if (_target == null) return;
			_target.Release();
			Object.DestroyImmediate(_target);
			_target = null;
		}
	}
}
