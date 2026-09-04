using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class ZoneRegenerationCoordinator : MonoBehaviour
	{
		private GameSession _session;

		private SceneFlowService _sceneFlow;

		private bool _bound;

		public int LastRegeneratedZoneCount { get; private set; }

		public int LastProducedUnitCount { get; private set; }

		public void Configure(GameSession session, SceneFlowService sceneFlow)
		{
			if (_bound)
			{
				Unbind();
			}
			_session = session;
			_sceneFlow = sceneFlow;
			if (!(_sceneFlow == null))
			{
				_sceneFlow.SceneTransitionCompleted += HandleTransitionCompleted;
				_bound = true;
			}
		}

		public int RegenerateForHomecoming()
		{
			if (_session == null)
			{
				return 0;
			}
			LastRegeneratedZoneCount = _session.ZoneStates.InvalidateAllForHomecoming(2);
			LastProducedUnitCount = _session.FarmProduction.AdvanceForHomecoming();
			return LastRegeneratedZoneCount;
		}

		private void HandleTransitionCompleted(string sceneKey)
		{
			if (!(_session == null) && _session.Phase == GameSessionPhase.ActiveGame && string.Equals(sceneKey, "HomeBase", StringComparison.Ordinal))
			{
				RegenerateForHomecoming();
			}
		}

		private void Unbind()
		{
			if (_sceneFlow != null)
			{
				_sceneFlow.SceneTransitionCompleted -= HandleTransitionCompleted;
			}
			_bound = false;
		}

		private void OnDestroy()
		{
			Unbind();
		}
	}
}
