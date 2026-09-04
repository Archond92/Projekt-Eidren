using Eidren.Core.BuildGrid;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Interaction;
using Eidren.Presentation;
using Eidren.UI;
using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class BuildingPlacementController : MonoBehaviour
	{
		private readonly List<string> _orphanedViews = new List<string>();

		private readonly Dictionary<string, BuildingInstanceView> _views = new Dictionary<string, BuildingInstanceView>(StringComparer.Ordinal);

		private BuildingService _buildings;

		private ContentDatabase _content;

		private GameSession _session;

		private PlayerInputReader _input;

		private PlayerPrefabBindings _player;

		private ZoneController _zone;

		private BuildingMenuWindow _menu;

		private CraftingWindow _crafting;

		private StorageWindow _storage;

		private TechnologyUnlockService _technology;

		private SceneFlowService _sceneFlow;

		private BuildingGhostView _ghost;

		private Camera _placementCamera;

		private BuildingPlacementRule _rule;

		private BuildingPlacementCandidate _candidate;

		private bool _placementActive;

		private string _movingInstanceId = string.Empty;

		private BuildingInstanceView _movingOriginalView;

		private readonly GridOverlayPlan _overlayPlan = new GridOverlayPlan();

		private readonly List<GridCellRect> _blockedCells = new List<GridCellRect>();

		private readonly List<Rect> _blockedZones = new List<Rect>();

		private BuildGridOverlayView _overlay;

		private GridCellRect _buildableCells;

		private float _groundY;

		private GridCoordinate _overlayFocus;

		private bool _overlayFocusValid;

		private const string PlacementHint = "Pfeile: bewegen · Q: drehen · Enter: bauen";

		private const float MarkMargin = 0.7f;

		private static readonly Vector2 EdgeMark = new Vector2(1.2f, 0.8f);

		private BuildingActionResult _shownReason;

		private string _shownMissingItem = string.Empty;

		private bool _reasonShown;

		private readonly List<Vector3> _roomCells = new List<Vector3>();

		private BuildingRoofView _roofs;

		private ZoneThreatWatcher _threat;

		private readonly GridOccupancy _edges = new GridOccupancy();

		private readonly List<BuildingInstanceState> _edgeInstances = new List<BuildingInstanceState>();

		public bool IsPlacementActive => _placementActive;

		public BuildingPlacementCandidate Candidate => _candidate;

		private void AfterSuccessfulPlacement(string buildingId)
		{
			DespawnRemovedViews();
			RefreshWallConnections();
			RefreshRooms();
			if (!KeepsPlanSelected(buildingId))
			{
				CancelPlacement();
				_menu.SetFeedback("Gebäude errichtet.", success: true);
			}
			else
			{
				InvalidateGridFocus();
				RefreshGhost();
				_menu.SetFeedback("Gebaut · weiter platzieren · Escape beendet", success: true);
			}
		}

		private void DespawnRemovedViews()
		{
			_orphanedViews.Clear();
			foreach (KeyValuePair<string, BuildingInstanceView> view in _views)
			{
				if (!_session.Buildings.TryGet(view.Key, out var _))
				{
					_orphanedViews.Add(view.Key);
				}
			}
			foreach (string orphanedView in _orphanedViews)
			{
				if (_views.TryGetValue(orphanedView, out var value) && value != null)
				{
					UnityEngine.Object.Destroy(value.gameObject);
				}
				_views.Remove(orphanedView);
			}
		}

		private bool KeepsPlanSelected(string buildingId)
		{
			int result;
			if (_content.TryGetBuilding(buildingId, out var value))
			{
				BuildingPlacementKind placementKind = value.PlacementKind;
				result = ((placementKind == BuildingPlacementKind.Floor || placementKind == BuildingPlacementKind.Edge) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
		}

		public void Initialize(BuildingService buildings, ContentDatabase content, GameSession session, PlayerInputReader input, PlayerPrefabBindings player, ZoneController zone, BuildingMenuWindow menu, CraftingWindow crafting, StorageWindow storage, TechnologyUnlockService technology, SceneFlowService sceneFlow, Camera placementCamera)
		{
			_buildings = buildings ?? throw new ArgumentNullException("buildings");
			_content = content ?? throw new ArgumentNullException("content");
			_session = session ?? throw new ArgumentNullException("session");
			_input = input ?? throw new ArgumentNullException("input");
			_player = player ?? throw new ArgumentNullException("player");
			_zone = zone ?? throw new ArgumentNullException("zone");
			_menu = menu ?? throw new ArgumentNullException("menu");
			_crafting = crafting;
			_storage = storage;
			_technology = technology ?? throw new ArgumentNullException("technology");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_placementCamera = placementCamera ?? throw new ArgumentNullException("placementCamera");
			BuildingPlacementArea area = HomeBaseBuildingPlacement.CreateArea(zone);
			_rule = new BuildingPlacementRule(content, area);
			CreateOverlay(area);
			CreateThreatWatcher();
			CreateRoofs();
			GameObject gameObject = new GameObject("BuildingGhost");
			gameObject.transform.SetParent(zone.GameplayRoot, worldPositionStays: false);
			_ghost = gameObject.AddComponent<BuildingGhostView>();
			_ghost.Hide();
			Bind();
			SpawnSavedBuildings();
		}

		public void BeginPlacement(string buildingId)
		{
			if (_menu.IsOpen && _content.TryGetBuilding(buildingId, out var value) && value.TryGetLevel(1, out var _, out var _))
			{
				Vector3 position = _player.transform.position + Vector3.forward * 3f;
				position.y = _zone.WalkableGround.bounds.max.y;
				position = BuildingPlacementRule.Snap(position);
				_candidate = new BuildingPlacementCandidate(buildingId, position, 0);
				_movingInstanceId = string.Empty;
				_placementActive = true;
				ClearGridFocus();
				InvalidateReason();
				RefreshGhost();
				_menu.SetPlacementMode(active: true);
			}
		}

		public void MoveCandidate(Vector2 direction)
		{
			if (_placementActive)
			{
				int num = Math.Sign(direction.x);
				int num2 = Math.Sign(direction.y);
				if (num != 0 || num2 != 0)
				{
					Vector3 position = _candidate.Position + new Vector3(num, 0f, num2);
					_candidate = new BuildingPlacementCandidate(_candidate.BuildingId, BuildingPlacementRule.Snap(position), _candidate.QuarterTurns, _candidate.Level);
					RefreshGhost();
				}
			}
		}

		public void RotateCandidate()
		{
			if (_placementActive)
			{
				_candidate = new BuildingPlacementCandidate(_candidate.BuildingId, _candidate.Position, _candidate.QuarterTurns + 1, _candidate.Level);
				RefreshGhost();
			}
		}

		public void MoveCandidateToScreen(Vector2 screenPosition)
		{
			if (_placementActive && !(_placementCamera == null))
			{
				Ray ray = _placementCamera.ScreenPointToRay(screenPosition);
				float y = _zone.WalkableGround.bounds.max.y;
				if (new Plane(Vector3.up, new Vector3(0f, y, 0f)).Raycast(ray, out var enter))
				{
					Vector3 position = BuildingPlacementRule.Snap(ray.GetPoint(enter));
					_candidate = new BuildingPlacementCandidate(_candidate.BuildingId, position, _candidate.QuarterTurns, _candidate.Level);
					RefreshGhost();
				}
			}
		}

		public BuildingActionResult ConfirmPlacement()
		{
			if (!_placementActive)
			{
				return BuildingActionResult.UnknownBuilding;
			}
			if (!string.IsNullOrEmpty(_movingInstanceId))
			{
				return ConfirmMove();
			}
			BuildingInstanceState instance;
			BuildingActionResult buildingActionResult = _buildings.TryPlace(in _candidate, _rule, out instance);
			if (buildingActionResult == BuildingActionResult.Success)
			{
				SpawnInstance(instance);
				_session.RequestSave(SaveRequestReason.BuildingChanged);
				AfterSuccessfulPlacement(instance.BuildingId);
			}
			else
			{
				RefreshGhost();
				_menu.SetFeedback(BuildingActionText.For(buildingActionResult));
			}
			return buildingActionResult;
		}

		public void BeginMoveNearest()
		{
			BuildingInstanceView buildingInstanceView = FindNearestView(out var onlyOccupiedFloor);
			if (buildingInstanceView == null || !_session.Buildings.TryGet(buildingInstanceView.InstanceId, out var state))
			{
				_menu.SetFeedback(onlyOccupiedFloor ? OccupiedFloorFeedback : "Kein Gebäude in Reichweite.");
				return;
			}
			BuildingActionResult buildingActionResult = _buildings.EvaluateMoveReadiness(state.InstanceId);
			if (buildingActionResult == BuildingActionResult.BuildingBusy || buildingActionResult == BuildingActionResult.StorageNotEmpty)
			{
				_menu.SetFeedback(BuildingActionText.For(buildingActionResult));
				return;
			}
			_movingInstanceId = state.InstanceId;
			_movingOriginalView = buildingInstanceView;
			buildingInstanceView.gameObject.SetActive(value: false);
			_candidate = new BuildingPlacementCandidate(state.BuildingId, state.Position, state.QuarterTurns, state.Level);
			_placementActive = true;
			ClearGridFocus();
			InvalidateReason();
			RefreshGhost();
			_menu.SetPlacementMode(active: true);
		}

		private BuildingActionResult ConfirmMove()
		{
			string movingInstanceId = _movingInstanceId;
			BuildingInstanceState moved;
			BuildingActionResult buildingActionResult = _buildings.TryMove(movingInstanceId, _candidate.Position, _candidate.QuarterTurns, _rule, out moved);
			if (buildingActionResult != BuildingActionResult.Success)
			{
				RefreshGhost();
				_menu.SetFeedback(BuildingActionText.For(buildingActionResult));
				return buildingActionResult;
			}
			if (_views.TryGetValue(movingInstanceId, out var value))
			{
				_views.Remove(movingInstanceId);
				if (value != null)
				{
					UnityEngine.Object.Destroy(value.gameObject);
				}
			}
			_movingOriginalView = null;
			SpawnInstance(moved);
			RefreshWallConnections();
			RefreshRooms();
			_session.RequestSave(SaveRequestReason.BuildingChanged);
			CancelPlacement();
			_menu.SetFeedback("Gebäude verschoben.", success: true);
			return buildingActionResult;
		}

		public BuildingActionResult DemolishNearest()
		{
			BuildingInstanceView buildingInstanceView = FindNearestView(out var onlyOccupiedFloor);
			if (buildingInstanceView == null)
			{
				_menu.SetFeedback(onlyOccupiedFloor ? OccupiedFloorFeedback : "Kein Gebäude in Reichweite.");
				return BuildingActionResult.UnknownInstance;
			}
			string instanceId = buildingInstanceView.InstanceId;
			InventoryItemAmount[] refund;
			BuildingActionResult buildingActionResult = _buildings.TryDemolish(instanceId, out refund);
			if (buildingActionResult == BuildingActionResult.Success)
			{
				_views.Remove(instanceId);
				UnityEngine.Object.Destroy(buildingInstanceView.gameObject);
				RefreshWallConnections();
				RefreshRooms();
				InvalidateGridFocus();
				RefreshGhost();
				_session.RequestSave(SaveRequestReason.BuildingChanged);
				_menu.SetFeedback("Gebäude abgerissen.", success: true);
			}
			else
			{
				_menu.SetFeedback(BuildingActionText.For(buildingActionResult));
			}
			return buildingActionResult;
		}

		private void Bind()
		{
			_menu.PlaceRequested += BeginPlacement;
			_menu.DemolishRequested += HandleDemolish;
			_menu.MoveRequested += BeginMoveNearest;
			_menu.Closed += HandleMenuClosed;
			_input.BuildingTogglePressed += HandleToggle;
			_input.BuildingRotatePressed += RotateCandidate;
			_input.BuildingDemolishPressed += HandleDemolish;
			_input.InventoryClosePressed += HandleClose;
			_input.InventoryUsePressed += HandleUse;
			_input.InventoryNavigate += HandleNavigate;
			_input.BuildingPointerMoved += MoveCandidateToScreen;
			_input.BuildingPointerConfirmed += HandlePointerConfirm;
			if (_menu.PlacementBar != null)
			{
				_menu.PlacementBar.Bind(_input);
			}
		}

		private void HandleToggle()
		{
			if (_menu.IsOpen)
			{
				_menu.Close();
			}
			else if (_input.GameplayEnabled && !IsThreatened())
			{
				_menu.Initialize(_content, _technology, _buildings.Materials);
				_menu.Open();
				ShowGrid();
				_input.SetGameplayEnabled(enabled: false);
				_player.Motor.SetMovementEnabled(enabled: false);
			}
		}

		private void HandleClose()
		{
			if (_menu.IsOpen)
			{
				if (_placementActive)
				{
					CancelPlacement();
				}
				else
				{
					_menu.Close();
				}
			}
		}

		private void HandleUse()
		{
			if (_menu.IsOpen)
			{
				if (_placementActive)
				{
					ConfirmPlacement();
				}
				else
				{
					_menu.ActivateSelected();
				}
			}
		}

		private void HandleNavigate(Vector2 direction)
		{
			if (_menu.IsOpen)
			{
				if (_placementActive)
				{
					MoveCandidate(direction);
				}
				else if (Mathf.Abs(direction.x) > 0.5f)
				{
					_menu.MoveSelection(Math.Sign(direction.x));
				}
				else if (Mathf.Abs(direction.y) > 0.5f)
				{
					_menu.MoveSelection(-Math.Sign(direction.y));
				}
			}
		}

		private void HandleDemolish()
		{
			if (_menu.IsOpen)
			{
				DemolishNearest();
			}
		}

		private void HandlePointerConfirm()
		{
			if (_placementActive)
			{
				ConfirmPlacement();
			}
		}

		private void HandleMenuClosed()
		{
			CancelPlacement();
			HideGrid();
			bool flag = _player.Damageable.IsAlive && !_sceneFlow.IsInputBlocked;
			_input.SetGameplayEnabled(flag);
			_player.Motor.SetMovementEnabled(flag);
		}

		private void CancelPlacement()
		{
			if (_movingOriginalView != null)
			{
				_movingOriginalView.gameObject.SetActive(value: true);
			}
			_movingOriginalView = null;
			_placementActive = false;
			_movingInstanceId = string.Empty;
			_ghost?.Hide();
			ClearGridFocus();
			InvalidateReason();
			if (_menu != null && _menu.IsOpen)
			{
				_menu.SetPlacementMode(active: false);
			}
		}

		private void RefreshGhost()
		{
			if (_placementActive && _content.TryGetBuilding(_candidate.BuildingId, out var value) && value.TryGetLevel(_candidate.Level, out var footprint, out var prefab))
			{
				_ghost.Show(prefab, _candidate.Position, (float)_candidate.QuarterTurns * 90f, MarkFootprint(value, in footprint));
				BuildingPreview preview = (string.IsNullOrEmpty(_movingInstanceId) ? _buildings.PreviewPlacement(in _candidate, _rule) : _buildings.PreviewMove(_movingInstanceId, in _candidate, _rule));
				_ghost.SetState(SignalFor(preview.State));
				ShowReason(in preview);
				RefreshGridFocus();
			}
		}

		private void OnDestroy()
		{
			if (_menu != null)
			{
				_menu.PlaceRequested -= BeginPlacement;
				_menu.DemolishRequested -= HandleDemolish;
				_menu.MoveRequested -= BeginMoveNearest;
				_menu.Closed -= HandleMenuClosed;
			}
			if (_input != null)
			{
				_input.BuildingTogglePressed -= HandleToggle;
				_input.BuildingRotatePressed -= RotateCandidate;
				_input.BuildingDemolishPressed -= HandleDemolish;
				_input.InventoryClosePressed -= HandleClose;
				_input.InventoryUsePressed -= HandleUse;
				_input.InventoryNavigate -= HandleNavigate;
				_input.BuildingPointerMoved -= MoveCandidateToScreen;
				_input.BuildingPointerConfirmed -= HandlePointerConfirm;
			}
		}

		private void CreateOverlay(BuildingPlacementArea area)
		{
			_groundY = _zone.WalkableGround.bounds.max.y;
			_buildableCells = BuildGridAreaProjection.Buildable(in area);
			BuildGridAreaProjection.Blocked(in area, _blockedCells);
			_blockedZones.Clear();
			foreach (GridCellRect blockedCell in _blockedCells)
			{
				GridCellRect cells = Clip(blockedCell, in _buildableCells);
				if (!cells.IsEmpty)
				{
					_blockedZones.Add(WorldRect(in cells));
				}
			}
			GameObject gameObject = new GameObject("BuildGridOverlay");
			gameObject.transform.SetParent(_zone.GameplayRoot, worldPositionStays: false);
			_overlay = gameObject.AddComponent<BuildGridOverlayView>();
		}

		private void ShowGrid()
		{
			if (!(_overlay == null) && !_buildableCells.IsEmpty)
			{
				Rect rect = WorldRect(in _buildableCells);
				_overlay.ShowArea(new Vector3(rect.center.x, _groundY, rect.center.y), rect.size, _blockedZones);
				_overlayFocusValid = false;
			}
		}

		private void HideGrid()
		{
			_overlay?.Hide();
		}

		private void ClearGridFocus()
		{
			_overlay?.ClearMarks();
			_overlayFocusValid = false;
		}

		private void InvalidateGridFocus()
		{
			_overlayFocusValid = false;
		}

		private void RefreshGridFocus()
		{
			if (!(_overlay == null) && _rule != null && !_buildableCells.IsEmpty && _content.TryGetBuilding(_candidate.BuildingId, out var value))
			{
				Vector3 vector = BuildingPlacementRule.Snap(_candidate.Position);
				GridCoordinate gridCoordinate = _rule.Origin.ToCell(vector.x, vector.z);
				if (!_overlayFocusValid || !(gridCoordinate == _overlayFocus))
				{
					_overlayFocus = gridCoordinate;
					_overlayFocusValid = true;
					_overlayPlan.Build(in _buildableCells, _blockedCells, gridCoordinate, value.PlacementKind, _rule.DescribeOccupancy(_session.Buildings.GetAll()));
					PushMarks();
				}
			}
		}

		private void PushMarks()
		{
			_overlay.BeginMarks();
			foreach (GridOverlayCell cell in _overlayPlan.Cells)
			{
				_rule.Origin.ToWorldCentre(cell.Cell, out var worldX, out var worldZ);
				_overlay.AddCell(new Vector3(worldX, _groundY, worldZ), StyleOf(cell.State));
			}
			foreach (GridOverlayEdge edge in _overlayPlan.Edges)
			{
				_rule.Origin.ToWorldCentre(edge.Edge, out var worldX2, out var worldZ2);
				_overlay.AddEdge(new Vector3(worldX2, _groundY, worldZ2), edge.Edge.Orientation == GridEdgeOrientation.NorthSouth, StyleOf(edge.State));
			}
			foreach (GridNode node in _overlayPlan.Nodes)
			{
				_rule.Origin.ToWorldCentre(node, out var worldX3, out var worldZ3);
				_overlay.AddNode(new Vector3(worldX3, _groundY, worldZ3));
			}
			_overlay.EndMarks();
		}

		private static BuildGridMarkStyle StyleOf(GridOverlayState state)
		{
			if (1 == 0)
			{
			}
			BuildGridMarkStyle result = state switch
			{
				GridOverlayState.Blocked => BuildGridMarkStyle.Blocked, 
				GridOverlayState.Conflict => BuildGridMarkStyle.Conflict, 
				_ => BuildGridMarkStyle.Focus, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private Rect WorldRect(in GridCellRect cells)
		{
			_rule.Origin.ToWorldCentre(new GridCoordinate(cells.MinX, cells.MinZ), out var worldX, out var worldZ);
			_rule.Origin.ToWorldCentre(new GridCoordinate(cells.MaxX, cells.MaxZ), out var worldX2, out var worldZ2);
			return new Rect(worldX - 0.5f, worldZ - 0.5f, worldX2 - worldX + 1f, worldZ2 - worldZ + 1f);
		}

		private static GridCellRect Clip(in GridCellRect inner, in GridCellRect outer)
		{
			return new GridCellRect(Mathf.Max(inner.MinX, outer.MinX), Mathf.Max(inner.MinZ, outer.MinZ), Mathf.Min(inner.MaxX, outer.MaxX), Mathf.Min(inner.MaxZ, outer.MaxZ));
		}

		private void SpawnSavedBuildings()
		{
			BuildingInstanceState[] all = _session.Buildings.GetAll();
			foreach (BuildingInstanceState state in all)
			{
				SpawnInstance(state);
			}
			RefreshWallConnections();
			RefreshRooms();
		}

		private void SpawnInstance(BuildingInstanceState state)
		{
			if (!_content.TryGetBuilding(state.BuildingId, out var value) || !value.TryGetLevel(state.Level, out var _, out var prefab))
			{
				throw new InvalidOperationException($"Building '{state.BuildingId}' level {state.Level} " + "has no prefab.");
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(prefab, state.Position, Quaternion.Euler(0f, state.RotationDegrees, 0f), _zone.GameplayRoot);
			gameObject.name = value.DisplayName + "_" + state.InstanceId;
			BuildingInstanceView component = gameObject.GetComponent<BuildingInstanceView>();
			if (component == null)
			{
				throw new InvalidOperationException("Building prefab '" + prefab.name + "' has no instance view.");
			}
			component.Initialize(state, _content, _session);
			_views.Add(state.InstanceId, component);
			if (component.Workbench != null && _crafting != null)
			{
				component.Workbench.Activated += _crafting.Open;
			}
			if (component.Storage != null && _storage != null)
			{
				component.Storage.Activated += _storage.Open;
			}
		}

		private BuildingInstanceView FindNearestView()
		{
			return FindNearestView(out var _);
		}

		/// <summary>
		/// F34-004/F34-005: Auswahl fuer Verschieben und Abreissen. Gebaeude
		/// (Objekt, Dekoration, Kante) haben immer Vorrang vor Bodenfeldern,
		/// auch wenn der Boden unter den Fuessen naeher liegt. Ein Bodenfeld
		/// ist nur waehlbar, wenn auf keiner seiner Zellen ein Gebaeude steht.
		/// Innerhalb eines Rangs entscheidet die Distanz, bei Gleichstand die
		/// Instanz-ID — die Auswahl haengt damit nicht mehr von der
		/// Einfuegereihenfolge ab.
		/// </summary>
		private BuildingInstanceView FindNearestView(out bool onlyOccupiedFloorInRange)
		{
			onlyOccupiedFloorInRange = false;
			GridOccupancy occupancy = (_rule != null && _session != null) ? _rule.DescribeOccupancy(_session.Buildings.GetAll()) : null;
			BuildingInstanceView result = null;
			int bestRank = int.MaxValue;
			float bestSqrDistance = float.MaxValue;
			foreach (BuildingInstanceView value in _views.Values)
			{
				if (value == null)
				{
					continue;
				}
				float sqrMagnitude = (value.transform.position - _player.transform.position).sqrMagnitude;
				if (sqrMagnitude > SelectionRangeSqr)
				{
					continue;
				}
				int rank = SelectionRank(value, occupancy);
				if (rank < 0)
				{
					onlyOccupiedFloorInRange = true;
					continue;
				}
				bool better = rank < bestRank
					|| (rank == bestRank && (sqrMagnitude < bestSqrDistance
						|| (sqrMagnitude == bestSqrDistance && result != null && string.CompareOrdinal(value.InstanceId, result.InstanceId) < 0)));
				if (better)
				{
					bestRank = rank;
					bestSqrDistance = sqrMagnitude;
					result = value;
				}
			}
			if (result != null)
			{
				onlyOccupiedFloorInRange = false;
			}
			return result;
		}

		private const float SelectionRangeSqr = 20.25f;

		public const string OccupiedFloorFeedback = "Zuerst das Gebäude auf dem Bodenfeld versetzen.";

		/// <returns>0 = Gebaeude, 1 = freies Bodenfeld, -1 = belegtes Bodenfeld (nicht waehlbar).</returns>
		private int SelectionRank(BuildingInstanceView view, GridOccupancy occupancy)
		{
			if (_content == null || !_content.TryGetBuilding(view.BuildingId, out var building) || building.PlacementKind != BuildingPlacementKind.Floor)
			{
				return 0;
			}
			if (occupancy == null || _rule == null || !_session.Buildings.TryGet(view.InstanceId, out var state) || !building.TryGetLevel(state.Level, out var footprint, out var _))
			{
				return 1;
			}
			Vector3 snapped = BuildingPlacementRule.Snap(state.Position);
			_rule.Origin.ToGrid(snapped.x, snapped.z, out var gridX, out var gridZ);
			foreach (GridCoordinate cell in GridFootprint.FromCentre(gridX, gridZ, footprint.Width, footprint.Depth, state.QuarterTurns).Cells())
			{
				if (occupancy.TryGetCellOwner(BuildingPlacementKind.Object, cell, out var _) || occupancy.TryGetCellOwner(BuildingPlacementKind.Decoration, cell, out var _))
				{
					return -1;
				}
			}
			return 1;
		}

		private static BuildingPreviewSignal SignalFor(BuildingPreviewState state)
		{
			if (1 == 0)
			{
			}
			BuildingPreviewSignal result = state switch
			{
				BuildingPreviewState.Valid => BuildingPreviewSignal.Valid, 
				BuildingPreviewState.Conditional => BuildingPreviewSignal.Conditional, 
				_ => BuildingPreviewSignal.Invalid, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private static Vector2 MarkFootprint(BuildingCostDefinition building, in BuildingFootprint footprint)
		{
			return (building.PlacementKind == BuildingPlacementKind.Edge) ? EdgeMark : new Vector2((float)footprint.Width + 0.7f, (float)footprint.Depth + 0.7f);
		}

		private void ShowReason(in BuildingPreview preview)
		{
			if (!_reasonShown || preview.Reason != _shownReason || !string.Equals(preview.MissingItemId, _shownMissingItem, StringComparison.Ordinal))
			{
				_shownReason = preview.Reason;
				_shownMissingItem = preview.MissingItemId;
				_reasonShown = true;
				bool flag = preview.State == BuildingPreviewState.Valid;
				_menu.SetFeedback(flag ? "Pfeile: bewegen · Q: drehen · Enter: bauen" : BuildingActionText.For(in preview, _content), flag);
			}
		}

		private void InvalidateReason()
		{
			_reasonShown = false;
		}

		private void CreateRoofs()
		{
			GameObject gameObject = new GameObject("BuildingRoofs");
			gameObject.transform.SetParent(_zone.GameplayRoot, worldPositionStays: false);
			_roofs = gameObject.AddComponent<BuildingRoofView>();
			_roofs.Configure(_player.transform, AuthoredWallHeight());
		}

		private void RefreshRooms()
		{
			if (_roofs == null || _rule == null)
			{
				return;
			}
			GridOccupancy occupancy = _rule.DescribeOccupancy(_session.Buildings.GetAll());
			IReadOnlyList<GridRoom> readOnlyList = GridRoomAnalysis.Derive(occupancy);
			_roofs.BeginRooms();
			foreach (GridRoom item in readOnlyList)
			{
				if (!item.IsClosed)
				{
					continue;
				}
				_roomCells.Clear();
				foreach (GridCoordinate cell in item.Cells)
				{
					_rule.Origin.ToWorldCentre(cell, out var worldX, out var worldZ);
					_roomCells.Add(new Vector3(worldX, _groundY, worldZ));
				}
				_roofs.AddRoom(_roomCells);
			}
			_roofs.EndRooms();
		}

		private float AuthoredWallHeight()
		{
			if (_content.TryGetBuilding("building.wall", out var value) && value.TryGetLevel(1, out var _, out var prefab))
			{
				BoxCollider componentInChildren = prefab.GetComponentInChildren<BoxCollider>(includeInactive: true);
				if (componentInChildren != null)
				{
					return componentInChildren.center.y + componentInChildren.size.y * 0.5f;
				}
			}
			throw new InvalidOperationException("Ohne autorierte Wandhöhe gibt es keine Dachhöhe.");
		}

		private void CreateThreatWatcher()
		{
			_threat = new ZoneThreatWatcher(_zone);
		}

		private bool IsThreatened()
		{
			return _threat != null && _threat.IsThreatened();
		}

		private void Update()
		{
			if (_menu != null && _menu.IsOpen && IsThreatened())
			{
				_menu.Close();
			}
		}

		private void RefreshWallConnections()
		{
			CollectEdges();
			foreach (BuildingInstanceState edgeInstance in _edgeInstances)
			{
				if (_views.TryGetValue(edgeInstance.InstanceId, out var value) && !(value == null))
				{
					WallConnectionView component = value.GetComponent<WallConnectionView>();
					if (!(component == null))
					{
						component.Apply(GridWallConnection.Classify(EdgeOf(edgeInstance), _edges), edgeInstance.QuarterTurns);
					}
				}
			}
		}

		private void CollectEdges()
		{
			_edges.Clear();
			_edgeInstances.Clear();
			BuildingInstanceState[] all = _session.Buildings.GetAll();
			foreach (BuildingInstanceState buildingInstanceState in all)
			{
				if (buildingInstanceState != null && _content.TryGetBuilding(buildingInstanceState.BuildingId, out var value) && value.PlacementKind == BuildingPlacementKind.Edge)
				{
					_edges.TryOccupyEdge(EdgeOf(buildingInstanceState), buildingInstanceState.InstanceId);
					_edgeInstances.Add(buildingInstanceState);
				}
			}
		}

		private GridEdge EdgeOf(BuildingInstanceState state)
		{
			if (_rule == null)
			{
				throw new InvalidOperationException("Placement rule missing.");
			}
			Vector3 vector = BuildingPlacementRule.Snap(state.Position);
			return _rule.Origin.ToEdge(vector.x, vector.z, BuildGridOrigin.OrientationFor(state.QuarterTurns));
		}
	}
}
