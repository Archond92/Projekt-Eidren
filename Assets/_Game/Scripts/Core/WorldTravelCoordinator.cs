using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class WorldTravelCoordinator : MonoBehaviour
	{
		private GameSession _session;

		private SceneFlowService _sceneFlow;

		private bool _bound;

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
				_sceneFlow.SceneTransitionFailed += HandleTransitionFailed;
				_bound = true;
			}
		}

		private void HandleTransitionCompleted(string sceneKey)
		{
			_session?.CompleteWorldTravel(sceneKey);
		}

		private void HandleTransitionFailed(string sceneKey, string message)
		{
			if (_session != null && _session.PendingWorldMapSceneKey == sceneKey)
			{
				_session.CancelWorldTravel();
			}
		}

		private void Unbind()
		{
			if (_sceneFlow != null)
			{
				_sceneFlow.SceneTransitionCompleted -= HandleTransitionCompleted;
				_sceneFlow.SceneTransitionFailed -= HandleTransitionFailed;
			}
			_bound = false;
		}

		private void OnDestroy()
		{
			Unbind();
		}
	}
}
