using Eidren.Core.Services;
using System;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class ApplicationLifecycleController : MonoBehaviour
	{
		private GameSession _session;

		private PauseService _pause;

		private SaveGameService _save;

		private PauseMenuController _pauseMenu;

		private bool _initialized;

		private bool _applicationPaused;

		private bool _hasFocus = true;

		private bool _suspensionHandled;

		private bool _requiresUserResume;

		private bool _audioPausedByLifecycle;

		public void Initialize(GameSession session, PauseService pause, SaveGameService save, PauseMenuController pauseMenu)
		{
			if (_pause != null)
			{
				_pause.PauseChanged -= HandlePauseChanged;
			}
			_session = session ?? throw new ArgumentNullException("session");
			_pause = pause ?? throw new ArgumentNullException("pause");
			_save = save ?? throw new ArgumentNullException("save");
			_pauseMenu = pauseMenu;
			_pause.PauseChanged += HandlePauseChanged;
			_initialized = true;
		}

		public void HandleApplicationPause(bool paused)
		{
			_applicationPaused = paused;
			if (paused)
			{
				SuspendApplication();
			}
			else
			{
				TryRestoreForegroundAudio();
			}
		}

		public void HandleApplicationFocus(bool focused)
		{
			_hasFocus = focused;
			if (!focused)
			{
				SuspendApplication();
				return;
			}
			if (_requiresUserResume && _session != null && _session.Phase == GameSessionPhase.ActiveGame)
			{
				EnsureLifecyclePauseVisible();
			}
			TryRestoreForegroundAudio();
		}

		private void OnApplicationPause(bool paused)
		{
			HandleApplicationPause(paused);
		}

		private void OnApplicationFocus(bool focused)
		{
			HandleApplicationFocus(focused);
		}

		private void OnApplicationQuit()
		{
			if (_initialized && _session.Phase == GameSessionPhase.ActiveGame)
			{
				_save.SaveNow(SaveRequestReason.ApplicationQuit, out var error);
				if (!string.IsNullOrEmpty(error))
				{
					Debug.LogWarning("Eidren lifecycle quit save was not written: " + error, this);
				}
			}
		}

		private void SuspendApplication()
		{
			if (!_initialized)
			{
				return;
			}
			PauseAudio();
			if (_session.Phase != GameSessionPhase.ActiveGame)
			{
				return;
			}
			_requiresUserResume = true;
			EnsureLifecyclePauseVisible();
			if (!_suspensionHandled)
			{
				_suspensionHandled = true;
				if (!_save.SaveNow(SaveRequestReason.ApplicationPause, out var error) && !string.IsNullOrEmpty(error))
				{
					Debug.LogWarning("Eidren lifecycle pause save was not written: " + error, this);
				}
			}
		}

		private void EnsureLifecyclePauseVisible()
		{
			if (!(_pauseMenu != null) || !_pauseMenu.PauseForApplicationLifecycle())
			{
				_pause.TryPause();
			}
		}

		private void HandlePauseChanged(bool paused)
		{
			if (!paused)
			{
				_requiresUserResume = false;
				_suspensionHandled = false;
				TryRestoreForegroundAudio();
			}
		}

		private void PauseAudio()
		{
			if (!_audioPausedByLifecycle)
			{
				AudioListener.pause = true;
				_audioPausedByLifecycle = true;
			}
		}

		private void TryRestoreForegroundAudio()
		{
			if (_audioPausedByLifecycle && !_applicationPaused && _hasFocus && !_requiresUserResume)
			{
				AudioListener.pause = false;
				_audioPausedByLifecycle = false;
				_suspensionHandled = false;
			}
		}

		private void OnDestroy()
		{
			if (_pause != null)
			{
				_pause.PauseChanged -= HandlePauseChanged;
			}
			if (_audioPausedByLifecycle)
			{
				AudioListener.pause = false;
			}
		}
	}
}
