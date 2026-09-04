using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class PauseService : MonoBehaviour
	{
		private const string MainMenuScene = "MainMenu";

		private GameSession _session;

		private SceneFlowService _sceneFlow;

		private SaveGameService _saveService;

		private float _previousTimeScale = 1f;

		private bool _initialized;

		public bool IsPaused { get; private set; }

		public event Action<bool> PauseChanged;

		public void Initialize(GameSession session, SceneFlowService sceneFlow, SaveGameService saveService)
		{
			if (_sceneFlow != null)
			{
				_sceneFlow.SceneTransitionStarted -= HandleSceneTransitionStarted;
			}
			_session = session ?? throw new ArgumentNullException("session");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_saveService = saveService ?? throw new ArgumentNullException("saveService");
			_sceneFlow.SceneTransitionStarted += HandleSceneTransitionStarted;
			_initialized = true;
		}

		public bool TryPause()
		{
			EnsureInitialized();
			if (IsPaused || _sceneFlow.IsTransitioning || _session.Phase != GameSessionPhase.ActiveGame || _session.LifeState != PlayerLifeState.Alive)
			{
				return false;
			}
			_previousTimeScale = ((Time.timeScale > 0f) ? Time.timeScale : 1f);
			Time.timeScale = 0f;
			IsPaused = true;
			this.PauseChanged?.Invoke(obj: true);
			return true;
		}

		public bool Resume()
		{
			if (!IsPaused)
			{
				return false;
			}
			Time.timeScale = Mathf.Max(0.01f, _previousTimeScale);
			IsPaused = false;
			this.PauseChanged?.Invoke(obj: false);
			return true;
		}

		public bool TryReturnHome(out string error)
		{
			if (!CanLeaveGameplay(out error))
			{
				return false;
			}
			if (!_saveService.SaveNow(SaveRequestReason.PauseReturnHome, out error))
			{
				return false;
			}
			Resume();
			if (_sceneFlow.TryLoadScene("HomeBase"))
			{
				return true;
			}
			error = "HomeBase scene transition was rejected.";
			return false;
		}

		public bool TryReturnToMainMenu(out string error)
		{
			if (!CanLeaveGameplay(out error))
			{
				return false;
			}
			if (!_saveService.SaveNow(SaveRequestReason.MainMenuRequested, out error))
			{
				return false;
			}
			Resume();
			if (!_sceneFlow.TryLoadScene("MainMenu"))
			{
				error = "MainMenu scene transition was rejected.";
				return false;
			}
			_session.PrepareForMainMenu(requestSave: false);
			return true;
		}

		private bool CanLeaveGameplay(out string error)
		{
			EnsureInitialized();
			if (_session.Phase != GameSessionPhase.ActiveGame || _session.LifeState != PlayerLifeState.Alive)
			{
				error = "Player is not in an active living game state.";
				return false;
			}
			if (_sceneFlow.IsTransitioning)
			{
				error = "A scene transition is already active.";
				return false;
			}
			error = string.Empty;
			return true;
		}

		private void HandleSceneTransitionStarted(string sceneName)
		{
			Resume();
		}

		private void EnsureInitialized()
		{
			if (!_initialized)
			{
				throw new InvalidOperationException("PauseService must be initialized before use.");
			}
		}

		private void OnDisable()
		{
			Resume();
		}

		private void OnDestroy()
		{
			if (_sceneFlow != null)
			{
				_sceneFlow.SceneTransitionStarted -= HandleSceneTransitionStarted;
			}
			Resume();
		}
	}
}
