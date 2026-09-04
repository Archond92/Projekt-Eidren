using Eidren.Core.Services;
using Eidren.Input;
using Eidren.UI;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class PlayerDeathController : MonoBehaviour
	{
		private const string HomeNodeId = "home_base";

		private PlayerPrefabBindings _player;

		private PlayerInputReader _input;

		private GameSession _session;

		private SceneFlowService _sceneFlow;

		private PlayerDeathWindow _window;

		private string _areaName;

		private ITransitionCancellable[] _actionCancellers = Array.Empty<ITransitionCancellable>();

		public bool IsDead { get; private set; }

		public bool IsRequestPending { get; private set; }

		public int DeathNotificationCount { get; private set; }

		public PlayerDeathWindow Window => _window;

		public event Action PlayerDied;

		public void Initialize(PlayerPrefabBindings player, PlayerInputReader input, GameSession session, SceneFlowService sceneFlow, PlayerDeathWindow window, string areaName)
		{
			Unbind();
			_player = player ?? throw new ArgumentNullException("player");
			_input = input ?? throw new ArgumentNullException("input");
			_session = session ?? throw new ArgumentNullException("session");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_window = window ?? throw new ArgumentNullException("window");
			_areaName = areaName ?? string.Empty;
			_actionCancellers = player.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).OfType<ITransitionCancellable>().ToArray();
			_window.Initialize();
			_window.ReviveRequested += Revive;
			_window.MainMenuRequested += ReturnToMainMenu;
			_player.Damageable.Died += HandlePlayerDied;
			_sceneFlow.SceneTransitionFailed += HandleTransitionFailed;
		}

		public void Revive()
		{
			if (IsDead && !IsRequestPending && _session.BeginRespawn("home_base", "HomeBase"))
			{
				BeginRequest();
				if (!_sceneFlow.TryLoadScene("HomeBase"))
				{
					_session.CancelRespawn();
					EndFailedRequest();
				}
			}
		}

		public void ReturnToMainMenu()
		{
			if (IsDead && !IsRequestPending)
			{
				BeginRequest();
				if (_sceneFlow.TryLoadScene("MainMenu"))
				{
					_session.PrepareForMainMenu();
				}
				else
				{
					EndFailedRequest();
				}
			}
		}

		private void HandlePlayerDied()
		{
			if (!IsDead && _session.RecordPlayerDeath())
			{
				IsDead = true;
				DeathNotificationCount++;
				_session.TryDropDeathBag(SceneManager.GetActiveScene().name, _player.transform.position);
				_input.SetGameplayEnabled(enabled: false);
				ITransitionCancellable[] actionCancellers = _actionCancellers;
				foreach (ITransitionCancellable transitionCancellable in actionCancellers)
				{
					transitionCancellable.CancelForSceneTransition();
				}
				_player.Motor.CancelForSceneTransition();
				_player.Combat.CancelForSceneTransition();
				_player.WeaponHitbox.EndWindow();
				_player.EidraTeam.CancelForSceneTransition();
				_player.Consumables.CancelActiveEffects();
				_window.Open(_areaName);
				this.PlayerDied?.Invoke();
			}
		}

		private void BeginRequest()
		{
			IsRequestPending = true;
			_window.SetRequestPending(pending: true);
		}

		private void EndFailedRequest()
		{
			IsRequestPending = false;
			_window.SetRequestPending(pending: false);
		}

		private void HandleTransitionFailed(string sceneName, string message)
		{
			if (IsRequestPending)
			{
				if (string.Equals(sceneName, "HomeBase", StringComparison.Ordinal))
				{
					_session.CancelRespawn();
				}
				else if (string.Equals(sceneName, "MainMenu", StringComparison.Ordinal))
				{
					_session.RestoreDeathAfterMenuFailure();
				}
				EndFailedRequest();
			}
		}

		private void Unbind()
		{
			if (_window != null)
			{
				_window.ReviveRequested -= Revive;
				_window.MainMenuRequested -= ReturnToMainMenu;
			}
			if (_player != null && _player.Damageable != null)
			{
				_player.Damageable.Died -= HandlePlayerDied;
			}
			if (_sceneFlow != null)
			{
				_sceneFlow.SceneTransitionFailed -= HandleTransitionFailed;
			}
		}

		private void OnDestroy()
		{
			Unbind();
			if (_window != null)
			{
				UnityEngine.Object.Destroy(_window.gameObject);
			}
		}
	}
}
