using Eidren.Core.Services;
using Eidren.Input;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class MapExitCoordinator : MonoBehaviour
	{
		[SerializeField]
		private string zoneId;

		[SerializeField]
		private ZoneBoundarySettings boundarySettings;

		[SerializeField]
		private MapExitFeedback feedback;

		private readonly HashSet<MapExitVolume> _overlappingVolumes = new HashSet<MapExitVolume>();

		private readonly MapExitCountdown _countdown = new MapExitCountdown();

		private PlayerPrefabBindings _player;

		private PlayerInputReader _input;

		private SceneFlowService _sceneFlow;

		private GameSession _gameSession;

		private ITransitionCancellable[] _actionCancellers = Array.Empty<ITransitionCancellable>();

		private bool _transitionLocked;

		public bool IsCountdownRunning => _countdown.IsRunning;

		public bool IsTransitionLocked => _transitionLocked;

		public MapExitDirection PendingDirection => _countdown.Direction;

		public float CountdownProgress => _countdown.Progress;

		public int ConfirmedTransitionCount { get; private set; }

		public event Action<MapExitDirection> ExitConfirmed;

		public void CancelForPause()
		{
			CancelCountdown();
		}

		public void Configure(string configuredZoneId, ZoneBoundarySettings settings, MapExitFeedback exitFeedback)
		{
			zoneId = configuredZoneId;
			boundarySettings = settings;
			feedback = exitFeedback;
		}

		public void BindPlayer(PlayerPrefabBindings participant, PlayerInputReader input, SceneFlowService sceneFlow, GameSession gameSession)
		{
			UnbindDeath();
			_player = participant;
			_input = input;
			_sceneFlow = sceneFlow;
			_gameSession = gameSession;
			_actionCancellers = ((participant == null) ? Array.Empty<ITransitionCancellable>() : participant.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).OfType<ITransitionCancellable>().ToArray());
			if (_player != null && _player.Damageable != null)
			{
				_player.Damageable.Died += HandlePlayerDied;
			}
			_gameSession?.SetAreaStatus(AreaStatus.SafeInside);
		}

		public void NotifyEntered(MapExitVolume volume, PlayerPrefabBindings participant)
		{
			if (_transitionLocked || volume == null || !volume.ExitEnabled || participant == null || (_player != null && participant != _player))
			{
				return;
			}
			if ((object)_player == null)
			{
				_player = participant;
			}
			_overlappingVolumes.Add(volume);
			MapExitVolume mapExitVolume = SelectVolume();
			if (!(mapExitVolume == null))
			{
				if (_countdown.IsRunning)
				{
					_countdown.UpdateSelection(mapExitVolume.StableExitId, mapExitVolume.Direction, mapExitVolume.TargetScene);
				}
				else if (_countdown.Begin(mapExitVolume.StableExitId, mapExitVolume.Direction, mapExitVolume.TargetScene, mapExitVolume.TransitionDuration))
				{
					_gameSession?.SetAreaStatus(AreaStatus.ExitPending);
				}
				feedback?.Show(_countdown.Direction, _countdown.Progress);
			}
		}

		public void NotifyExited(MapExitVolume volume, PlayerPrefabBindings participant)
		{
			if (!(volume == null) && !(participant == null) && (!(_player != null) || !(participant != _player)))
			{
				_overlappingVolumes.Remove(volume);
			}
		}

		public void NotifyVolumeDisabled(MapExitVolume volume)
		{
			if (!(volume == null))
			{
				_overlappingVolumes.Remove(volume);
				if (_countdown.IsRunning && _countdown.ExitId == volume.StableExitId)
				{
					CancelCountdown();
				}
			}
		}

		private void Update()
		{
			if (_countdown.IsRunning && !_transitionLocked && !(_player == null) && !(boundarySettings == null))
			{
				MapExitVolume mapExitVolume = SelectVolume();
				if (mapExitVolume != null)
				{
					_countdown.UpdateSelection(mapExitVolume.StableExitId, mapExitVolume.Direction, mapExitVolume.TargetScene);
				}
				bool fullyInsideSafeArea = IsPlayerFullyInsideSafeArea();
				bool participantAlive = _player.Damageable != null && _player.Damageable.IsAlive;
				if (_countdown.Tick(Time.unscaledDeltaTime, fullyInsideSafeArea, participantAlive))
				{
					ConfirmExit();
				}
				else if (!_countdown.IsRunning)
				{
					CancelCountdown();
				}
				else
				{
					feedback?.Show(_countdown.Direction, _countdown.Progress);
				}
			}
		}

		private bool IsPlayerFullyInsideSafeArea()
		{
			if (_player == null || boundarySettings == null)
			{
				return false;
			}
			float participantRadius = ((_player.CharacterController != null) ? (_player.CharacterController.radius * Mathf.Max(Mathf.Abs(_player.transform.lossyScale.x), Mathf.Abs(_player.transform.lossyScale.z))) : 0f);
			return boundarySettings.IsFullyInsideSafeArea(_player.transform.position, participantRadius);
		}

		private MapExitVolume SelectVolume()
		{
			_overlappingVolumes.RemoveWhere((MapExitVolume volume) => volume == null || !volume.ExitEnabled);
			if (_overlappingVolumes.Count == 0)
			{
				return null;
			}
			MapSideFlags mapSideFlags = MapSideFlags.None;
			foreach (MapExitVolume overlappingVolume in _overlappingVolumes)
			{
				mapSideFlags |= MapExitDirectionResolver.ToFlag(overlappingVolume.Direction);
			}
			Vector3 currentWorldMovement = ((_player != null) ? _player.Motor.WorldMoveDirection : Vector3.zero);
			Vector3 lastValidWorldMovement = ((_player != null) ? _player.Motor.LastValidWorldMoveDirection : Vector3.forward);
			MapExitDirection direction = MapExitDirectionResolver.Resolve(currentWorldMovement, lastValidWorldMovement, mapSideFlags);
			return _overlappingVolumes.FirstOrDefault((MapExitVolume volume) => volume.Direction == direction);
		}

		private void ConfirmExit()
		{
			if (_transitionLocked || _player == null)
			{
				return;
			}
			if (boundarySettings != null && IsPlayerFullyInsideSafeArea())
			{
				CancelCountdown();
				return;
			}
			_transitionLocked = true;
			ConfirmedTransitionCount++;
			string targetScene = _countdown.TargetScene;
			MapExitDirection direction = _countdown.Direction;
			feedback?.Show(direction, 1f);
			_gameSession?.RecordConfirmedExit(zoneId, direction, targetScene);
			this.ExitConfirmed?.Invoke(direction);
			_input?.SetGameplayEnabled(enabled: false);
			_player.Motor.CancelForSceneTransition();
			_player.Combat.CancelForSceneTransition();
			_player.WeaponHitbox.EndWindow();
			_player.EidraTeam.CancelForSceneTransition();
			ITransitionCancellable[] actionCancellers = _actionCancellers;
			foreach (ITransitionCancellable transitionCancellable in actionCancellers)
			{
				transitionCancellable.CancelForSceneTransition();
			}
			if ((!(_sceneFlow != null) || !_sceneFlow.TryLoadScene(targetScene)) && (!(_sceneFlow != null) || !_sceneFlow.IsTransitioning))
			{
				_transitionLocked = false;
				_gameSession?.SetAreaStatus(AreaStatus.SafeInside);
				if (_player.Damageable != null && _player.Damageable.IsAlive)
				{
					_input?.SetGameplayEnabled(enabled: true);
					_player.Motor.SetMovementEnabled(enabled: true);
				}
				feedback?.Hide();
			}
		}

		private void HandlePlayerDied()
		{
			if (!_transitionLocked)
			{
				CancelCountdown();
			}
		}

		private void CancelCountdown()
		{
			_countdown.Cancel();
			feedback?.Hide();
			if (!_transitionLocked)
			{
				_gameSession?.SetAreaStatus(AreaStatus.SafeInside);
			}
		}

		private void UnbindDeath()
		{
			if (_player != null && _player.Damageable != null)
			{
				_player.Damageable.Died -= HandlePlayerDied;
			}
		}

		private void OnDisable()
		{
			if (!_transitionLocked)
			{
				CancelCountdown();
			}
			_overlappingVolumes.Clear();
		}

		private void OnDestroy()
		{
			UnbindDeath();
		}
	}
}
