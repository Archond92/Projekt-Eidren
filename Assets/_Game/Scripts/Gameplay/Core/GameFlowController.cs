using Eidren.AI;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Input;
using Eidren.Interaction;
using Eidren.Player;
using System.Linq;
using System;
using UnityEngine;

namespace Eidren.Gameplay.Flow
{
	public sealed class GameFlowController : MonoBehaviour
	{
		private PlayerInputReader _input;

		private Transform _player;

		private Damageable _playerHealth;

		private BossController _boss;

		private ResourceNode _resourceNode;

		private SceneFlowService _sceneFlow;

		private float _battleStartTime;

		private int _staggerCount;

		private bool _ended;

		private ITransitionCancellable[] _actionCancellers = Array.Empty<ITransitionCancellable>();

		private ConsumableController _consumables;

		public bool IsBattleActive { get; private set; }

		public int Resources { get; private set; }

		public event Action BattleStarted;

		public event Action<int> ResourceChanged;

		public event Action<bool, float, int> GameEnded;

		public void Initialize(PlayerInputReader input, Transform player, Damageable playerHealth, BossController boss, ResourceNode resourceNode, SceneFlowService sceneFlow)
		{
			_input = input;
			_player = player;
			_playerHealth = playerHealth;
			_boss = boss;
			_resourceNode = resourceNode;
			_sceneFlow = sceneFlow;
			_actionCancellers = player.GetComponentsInChildren<MonoBehaviour>(includeInactive: true).OfType<ITransitionCancellable>().ToArray();
			_consumables = player.GetComponentInChildren<ConsumableController>(includeInactive: true);
			_resourceNode.Mined += OnResourceMined;
			_boss.EnemyStateChanged += OnBossStateChanged;
			_boss.Died += delegate
			{
				EndGame(victory: true);
			};
			_playerHealth.Died += delegate
			{
				EndGame(victory: false);
			};
			_input.RestartPressed += Restart;
		}

		private void Update()
		{
			if (!IsBattleActive && !_ended && Vector3.Distance(_player.position, _boss.transform.position) < _boss.PerceptionRange)
			{
				StartBattle();
			}
		}

		public void Restart()
		{
			if (_ended)
			{
				_sceneFlow.ReloadActiveScene();
			}
		}

		private void StartBattle()
		{
			IsBattleActive = true;
			_battleStartTime = Time.time;
			_boss.BeginBattle();
			this.BattleStarted?.Invoke();
		}

		private void OnResourceMined()
		{
			Resources++;
			this.ResourceChanged?.Invoke(Resources);
		}

		private void OnBossStateChanged(EnemyState previous, EnemyState current)
		{
			if (current == EnemyState.Staggered)
			{
				_staggerCount++;
			}
		}

		private void EndGame(bool victory)
		{
			if (!_ended)
			{
				_ended = true;
				_input.SetGameplayEnabled(enabled: false);
				ITransitionCancellable[] actionCancellers = _actionCancellers;
				foreach (ITransitionCancellable transitionCancellable in actionCancellers)
				{
					transitionCancellable.CancelForSceneTransition();
				}
				_consumables?.CancelActiveEffects();
				float arg = (IsBattleActive ? (Time.time - _battleStartTime) : 0f);
				this.GameEnded?.Invoke(victory, arg, _staggerCount);
			}
		}
	}
}
