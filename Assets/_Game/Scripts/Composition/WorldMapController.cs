using Eidren.Core.Services;
using Eidren.Data;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class WorldMapController : MonoBehaviour
	{
		[SerializeField]
		private WorldMapDefinition definition;

		[SerializeField]
		private WorldMapCanvasView view;

		private GameSession _session;

		private SceneFlowService _sceneFlow;

		private ContentDatabase _contentDatabase;

		private WorldMapNodeDefinition _selectedNode;

		private bool _eventsBound;

		private bool _travelRequested;

		public WorldMapDefinition Definition => definition;

		public WorldMapNodeDefinition SelectedNode => _selectedNode;

		public WorldMapInfoPanel InfoPanel => view.InfoPanel;

		public WorldMapCanvasView View => view;

		public int BoundNodeCount { get; private set; }

		public bool TravelRequested => _travelRequested;

		public void ConfigureUi(WorldMapDefinition mapDefinition, WorldMapCanvasView canvasView)
		{
			definition = mapDefinition;
			view = canvasView;
		}

		public void Initialize(GameSession session, SceneFlowService sceneFlow, ContentDatabase contentDatabase)
		{
			_session = session ?? throw new ArgumentNullException("session");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_contentDatabase = contentDatabase ?? throw new ArgumentNullException("contentDatabase");
			definition.ValidateOrThrow();
			view.ValidateReferences();
			if (view.NodeViews.Length != definition.Nodes.Count)
			{
				throw new InvalidOperationException($"WorldMap canvas has {view.NodeViews.Length} node views " + $"for {definition.Nodes.Count} data nodes.");
			}
			_contentDatabase.ConfigureWorldMaps(new WorldMapDefinition[1] { definition });
			view.Apply(definition);
			_sceneFlow.SetTransitionView(view.TransitionOverlay);
			BindNodes();
			ConfigureStatusModules();
			BindEvents();
			string nodeId = ((!string.IsNullOrWhiteSpace(_session.PreviousZoneId)) ? _session.PreviousZoneId : _session.SelectedWorldMapNodeId);
			if (!definition.TryGetNode(nodeId, out var node))
			{
				node = definition.Nodes[0];
			}
			SelectNode(node);
			view.InfoPanel.Open(immediate: true);
			view.EventSystem.firstSelectedGameObject = view.NodeViews[0].Button.gameObject;
		}

		public bool SelectNode(WorldMapNodeDefinition node)
		{
			if (node == null || !ContainsNode(node.Id))
			{
				return false;
			}
			_selectedNode = node;
			_session.SelectWorldMapNode(node.Id);
			view.InfoPanel.Bind(node, _session.IsWorldMapNodeVisited(node.Id), IsBossDefeated(node));
			view.InfoPanel.Open();
			RefreshNodeStates();
			RefreshTravelButton();
			return true;
		}

		public bool SelectNodeById(string nodeId)
		{
			WorldMapNodeDefinition node;
			return definition.TryGetNode(nodeId, out node) && SelectNode(node);
		}

		public bool TryTravelSelected()
		{
			if (_selectedNode == null || !IsAvailable(_selectedNode) || _travelRequested || _session.IsWorldTravelPending || _sceneFlow.IsTransitioning)
			{
				return false;
			}
			if (!_session.BeginWorldTravel(_selectedNode.Id, _selectedNode.SceneKey))
			{
				return false;
			}
			if (!_sceneFlow.TryLoadScene(_selectedNode.SceneKey))
			{
				_session.CancelWorldTravel();
				return false;
			}
			_travelRequested = true;
			view.TravelButton.PlayConfirmation();
			view.TransitionOverlay.PreserveThroughSceneLoad();
			RefreshTravelButton();
			return true;
		}

		public bool ReturnToCurrentArea()
		{
			string nodeId = ((!string.IsNullOrWhiteSpace(_session.PreviousZoneId)) ? _session.PreviousZoneId : _session.CurrentWorldMapNodeId);
			if (!SelectNodeById(nodeId))
			{
				return false;
			}
			return TryTravelSelected();
		}

		private void Awake()
		{
			EidrenServiceRoot eidrenServiceRoot = EidrenServiceRoot.FindOrCreate();
			Initialize(eidrenServiceRoot.GameSession, eidrenServiceRoot.SceneFlowService, eidrenServiceRoot.ContentDatabase);
		}

		private void BindNodes()
		{
			BoundNodeCount = 0;
			for (int i = 0; i < definition.Nodes.Count; i++)
			{
				view.NodeViews[i].Bind(definition.Nodes[i], definition.FallbackNodeIcon, definition.Theme, HandleNodeSelected);
				BoundNodeCount++;
			}
		}

		private void ConfigureStatusModules()
		{
			string[,] entries = new string[7, 2]
			{
				{ "EREIGNIS", "Keine" },
				{ "TIMER", "--:--" },
				{ "KALENDER", "Tag 1" },
				{ "ENERGIE", "—" },
				{ "MÜNZEN", "—" },
				{ "SELTEN", "—" },
				{ "HINWEISE", "0" }
			};
			view.TopStatusBar.SetEntries(entries);
		}

		private void BindEvents()
		{
			if (!_eventsBound)
			{
				view.TravelButton.TravelRequested += HandleTravelPressed;
				view.SideMenu.BackRequested += HandleBackPressed;
				_sceneFlow.SceneTransitionStarted += HandleSceneTransitionStarted;
				_sceneFlow.SceneTransitionFailed += HandleTravelFailed;
				_sceneFlow.SceneTransitionCompleted += HandleTravelCompleted;
				_eventsBound = true;
			}
		}

		private void HandleNodeSelected(WorldMapNodeDefinition node)
		{
			SelectNode(node);
		}

		private void HandleTravelPressed()
		{
			TryTravelSelected();
		}

		private void HandleSceneTransitionStarted(string sceneKey)
		{
			view.TransitionOverlay?.PreserveThroughSceneLoad();
		}

		private void HandleBackPressed()
		{
			ReturnToCurrentArea();
		}

		private void HandleTravelFailed(string sceneKey, string message)
		{
			if (!(_selectedNode == null) && !(_selectedNode.SceneKey != sceneKey))
			{
				_travelRequested = false;
				_session.CancelWorldTravel();
				RefreshTravelButton();
			}
		}

		private void HandleTravelCompleted(string sceneKey)
		{
			if (!(_selectedNode == null) && !(_selectedNode.SceneKey != sceneKey))
			{
				_travelRequested = false;
				RefreshNodeStates();
				view.InfoPanel.Bind(_selectedNode, _session.IsWorldMapNodeVisited(_selectedNode.Id), IsBossDefeated(_selectedNode));
				RefreshTravelButton();
			}
		}

		private void RefreshNodeStates()
		{
			WorldMapNodeView[] nodeViews = view.NodeViews;
			foreach (WorldMapNodeView worldMapNodeView in nodeViews)
			{
				WorldMapNodeDefinition worldMapNodeDefinition = worldMapNodeView.Definition;
				worldMapNodeView.ApplyState(new WorldMapNodeVisualState(IsAvailable(worldMapNodeDefinition), worldMapNodeDefinition == _selectedNode, _session.IsWorldMapNodeVisited(worldMapNodeDefinition.Id), _session.IsWorldMapNodeEventActive(worldMapNodeDefinition.Id), worldMapNodeDefinition.HasBoss, IsBossDefeated(worldMapNodeDefinition), _session.IsWorldMapNodeCompleted(worldMapNodeDefinition.Id)));
			}
		}

		private bool IsBossDefeated(WorldMapNodeDefinition node)
		{
			return node != null && string.Equals(node.Id, "zone_ember_ruins", StringComparison.Ordinal) && _session.HasProgressFlag("garon_defeated");
		}

		private void RefreshTravelButton()
		{
			view.TravelButton.SetAvailable(_selectedNode != null && IsAvailable(_selectedNode) && !_travelRequested && !_session.IsWorldTravelPending && !_sceneFlow.IsTransitioning);
		}

		private bool ContainsNode(string nodeId)
		{
			WorldMapNodeDefinition node;
			return definition.TryGetNode(nodeId, out node);
		}

		private bool IsAvailable(WorldMapNodeDefinition node)
		{
			return node != null && (node.InitiallyAvailable || (WorldMapNodeIds.IsTierTwo(node.Id) && _session.HasProgressFlag("tier_2_unlocked")));
		}

		private void OnDestroy()
		{
			if (!_eventsBound)
			{
				return;
			}
			view.TravelButton.TravelRequested -= HandleTravelPressed;
			view.SideMenu.BackRequested -= HandleBackPressed;
			if (_sceneFlow != null)
			{
				_sceneFlow.SceneTransitionFailed -= HandleTravelFailed;
				_sceneFlow.SceneTransitionStarted -= HandleSceneTransitionStarted;
				_sceneFlow.SceneTransitionCompleted -= HandleTravelCompleted;
				_sceneFlow.SetTransitionView(null);
				if (!_travelRequested && view.TransitionOverlay != null && view.TransitionOverlay.IsPreservedForTransition)
				{
					UnityEngine.Object.Destroy(view.TransitionOverlay.gameObject);
				}
			}
			_eventsBound = false;
		}
	}
}
