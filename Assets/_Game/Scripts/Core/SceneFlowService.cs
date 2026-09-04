using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class SceneFlowService : MonoBehaviour
	{
		private sealed class UnitySceneLoader : ISceneLoader
		{
			public string ActiveSceneName => SceneManager.GetActiveScene().name;

			public bool CanLoad(string sceneName)
			{
				return Application.CanStreamedLevelBeLoaded(sceneName);
			}

			public IEnumerator LoadAsync(string sceneName)
			{
				AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
				if (operation == null)
				{
					throw new InvalidOperationException("Unity could not begin loading scene '" + sceneName + "'.");
				}
				operation.allowSceneActivation = false;
				while (operation.progress < 0.9f)
				{
					yield return null;
				}
				operation.allowSceneActivation = true;
				while (!operation.isDone)
				{
					yield return null;
				}
			}
		}

		private ISceneTransitionView _transitionView;

		private ISceneTransitionView _defaultTransitionView;

		private ISceneTransitionView _pendingTransitionView;

		private bool _hasPendingTransitionView;

		private ISceneLoader _sceneLoader;

		private Exception _stepException;

		private Coroutine _transitionRoutine;

		public bool IsTransitioning { get; private set; }

		public bool IsInputBlocked { get; private set; }

		public event Action<string> SceneTransitionStarted;

		public event Action<string> SceneTransitionCompleted;

		public event Action<string, string> SceneTransitionFailed;

		public event Action<bool> InputBlockedChanged;

		public void Configure(ISceneTransitionView transitionView, ISceneLoader sceneLoader = null)
		{
			_defaultTransitionView = transitionView;
			_transitionView = transitionView;
			_pendingTransitionView = null;
			_hasPendingTransitionView = false;
			_sceneLoader = sceneLoader ?? new UnitySceneLoader();
			_transitionView?.SetInputBlocked(blocked: false);
		}

		public void SetTransitionView(ISceneTransitionView transitionView)
		{
			ISceneTransitionView sceneTransitionView = transitionView ?? _defaultTransitionView;
			if (IsTransitioning)
			{
				_pendingTransitionView = sceneTransitionView;
				_hasPendingTransitionView = true;
			}
			else
			{
				_transitionView = sceneTransitionView;
				_transitionView?.SetInputBlocked(IsInputBlocked);
			}
		}

		public bool TryLoadScene(string sceneName)
		{
			if (IsTransitioning)
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(sceneName))
			{
				ReportInvalidScene(sceneName, "The scene name is empty.");
				return false;
			}
			if (_sceneLoader == null)
			{
				_sceneLoader = new UnitySceneLoader();
			}
			if (!_sceneLoader.CanLoad(sceneName))
			{
				ReportInvalidScene(sceneName, "Scene '" + sceneName + "' is not available in the active Unity Build Settings scene list.");
				return false;
			}
			IsTransitioning = true;
			this.SceneTransitionStarted?.Invoke(sceneName);
			ISceneTransitionView transitionView = _transitionView;
			_transitionRoutine = StartCoroutine(TransitionRoutine(sceneName, transitionView));
			return true;
		}

		public void LoadScene(string sceneName)
		{
			TryLoadScene(sceneName);
		}

		public bool TryReloadActiveScene()
		{
			if (_sceneLoader == null)
			{
				_sceneLoader = new UnitySceneLoader();
			}
			return TryLoadScene(_sceneLoader.ActiveSceneName);
		}

		public void ReloadActiveScene()
		{
			TryReloadActiveScene();
		}

		private IEnumerator TransitionRoutine(string sceneName, ISceneTransitionView transitionView)
		{
			SetInputBlocked(blocked: true);
			if (transitionView != null)
			{
				yield return RunStep(transitionView.FadeOut());
				if (!CompleteStepOrFail(sceneName))
				{
					yield break;
				}
			}
			yield return RunStep(_sceneLoader.LoadAsync(sceneName));
			if (!CompleteStepOrFail(sceneName))
			{
				yield break;
			}
			if (transitionView != null)
			{
				yield return RunStep(transitionView.FadeIn());
				if (!CompleteStepOrFail(sceneName))
				{
					yield break;
				}
			}
			FinishTransition();
			this.SceneTransitionCompleted?.Invoke(sceneName);
		}

		private IEnumerator RunStep(IEnumerator step)
		{
			_stepException = null;
			if (step == null)
			{
				yield break;
			}
			while (true)
			{
				object current;
				try
				{
					if (!step.MoveNext())
					{
						break;
					}
					current = step.Current;
				}
				catch (Exception ex)
				{
					Exception exception = ex;
					_stepException = exception;
					break;
				}
				yield return current;
			}
		}

		private bool CompleteStepOrFail(string sceneName)
		{
			if (_stepException == null)
			{
				return true;
			}
			string text = "Scene transition to '" + sceneName + "' failed: " + _stepException.Message;
			Debug.LogError(text, this);
			this.SceneTransitionFailed?.Invoke(sceneName, text);
			FinishTransition();
			return false;
		}

		private void ReportInvalidScene(string sceneName, string message)
		{
			Debug.LogError(message, this);
			this.SceneTransitionFailed?.Invoke(sceneName ?? string.Empty, message);
		}

		private void FinishTransition()
		{
			_transitionRoutine = null;
			IsTransitioning = false;
			SetInputBlocked(blocked: false);
			ApplyPendingTransitionView();
		}

		private void ApplyPendingTransitionView()
		{
			if (_hasPendingTransitionView)
			{
				_transitionView = _pendingTransitionView;
				_pendingTransitionView = null;
				_hasPendingTransitionView = false;
				_transitionView?.SetInputBlocked(IsInputBlocked);
			}
		}

		private void SetInputBlocked(bool blocked)
		{
			IsInputBlocked = blocked;
			_transitionView?.SetInputBlocked(blocked);
			this.InputBlockedChanged?.Invoke(blocked);
		}

		private void OnDisable()
		{
			if (_transitionRoutine != null)
			{
				StopCoroutine(_transitionRoutine);
			}
			_transitionRoutine = null;
			IsTransitioning = false;
			SetInputBlocked(blocked: false);
			ApplyPendingTransitionView();
		}
	}
}
