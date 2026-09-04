using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Player;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.UI
{
	public sealed partial class TechnologyTreeWindow : MonoBehaviour
	{
		[SerializeField]
		private GameObject panelRoot;

		[SerializeField]
		private Text pointsText;

		[SerializeField]
		private Text levelText;

		[SerializeField]
		private Text feedbackText;

		[SerializeField]
		private TechnologyNodeButtonView[] nodeViews;

		[SerializeField]
		private Button unlockButton;

		[SerializeField]
		private Button closeButton;

		private TechnologyTreeDefinition _tree;

		private TechnologyUnlockService _unlocks;

		private PlayerProgressionService _progression;

		private PlayerInputReader _input;

		private PlayerMotor _motor;

		private PlayerCombatController _combat;

		private Damageable _health;

		private SceneFlowService _sceneFlow;

		private TechnologyNodeDefinition[] _nodes = Array.Empty<TechnologyNodeDefinition>();

		private ScrollRect _scroll;

		private int _selectedIndex;

		private bool _initialized;

		public bool IsOpen { get; private set; }

		public int SelectedIndex => _selectedIndex;

		public Button UnlockButton => unlockButton;

		public TechnologyNodeButtonView[] NodeViews => nodeViews;

		public void ConfigureReferences(GameObject configuredPanelRoot, Text configuredPointsText, Text configuredLevelText, Text configuredFeedbackText, TechnologyNodeButtonView[] configuredNodeViews, Button configuredUnlockButton, Button configuredCloseButton)
		{
			panelRoot = configuredPanelRoot;
			pointsText = configuredPointsText;
			levelText = configuredLevelText;
			feedbackText = configuredFeedbackText;
			nodeViews = configuredNodeViews;
			unlockButton = configuredUnlockButton;
			closeButton = configuredCloseButton;
		}

		public void Initialize(TechnologyTreeDefinition tree, TechnologyUnlockService unlocks, PlayerProgressionService progression, PlayerInputReader input, PlayerMotor motor, PlayerCombatController combat, Damageable health, SceneFlowService sceneFlow)
		{
			ValidateReferences();
			Unbind();
			_tree = tree ?? throw new ArgumentNullException("tree");
			_unlocks = unlocks ?? throw new ArgumentNullException("unlocks");
			_progression = progression ?? throw new ArgumentNullException("progression");
			_input = input ?? throw new ArgumentNullException("input");
			_motor = motor ?? throw new ArgumentNullException("motor");
			_combat = combat ?? throw new ArgumentNullException("combat");
			_health = health ?? throw new ArgumentNullException("health");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_nodes = SortedNodes(tree);
			EnsureNodeViewCapacity(_nodes.Length);
			for (int i = 0; i < nodeViews.Length; i++)
			{
				nodeViews[i].Bind(i, SelectNode);
			}
			unlockButton.onClick.RemoveAllListeners();
			closeButton.onClick.RemoveAllListeners();
			unlockButton.onClick.AddListener(UnlockSelected);
			closeButton.onClick.AddListener(Close);
			_unlocks.Changed += HandleChanged;
			_progression.Changed += HandleProgressionChanged;
			_input.TechnologyTogglePressed += HandleToggle;
			_input.InventoryClosePressed += HandleCloseInput;
			_input.InventoryUsePressed += HandleUseInput;
			_input.InventoryNavigate += HandleNavigate;
			_sceneFlow.InputBlockedChanged += HandleInputBlocked;
			_sceneFlow.SceneTransitionStarted += HandleTransition;
			_health.Died += HandleDeath;
			_initialized = true;
			panelRoot.SetActive(value: false);
			Refresh();
		}

		public void Open()
		{
			if (_initialized && !IsOpen && _input.GameplayEnabled && !_sceneFlow.IsInputBlocked && _health.IsAlive)
			{
				IsOpen = true;
				feedbackText.text = string.Empty;
				_combat.CancelForSceneTransition();
				_motor.CancelTransientMovement();
				_motor.SetMovementEnabled(enabled: false);
				_input.SetGameplayEnabled(enabled: false);
				panelRoot.SetActive(value: true);
				Refresh();
				SelectCurrentObject();
			}
		}

		public void Close()
		{
			CloseInternal(restoreGameplay: true);
		}

		public void SelectNode(int index)
		{
			if (index >= 0 && index < _nodes.Length)
			{
				_selectedIndex = index;
				feedbackText.text = string.Empty;
				Refresh();
				EnsureSelectedVisible();
			}
		}

		public void UnlockSelected()
		{
			if (IsOpen && _nodes.Length != 0)
			{
				TechnologyUnlockResult result = _unlocks.TryUnlock(_nodes[_selectedIndex].Id);
				feedbackText.text = ResultLabel(result);
				Refresh();
			}
		}

		private void HandleToggle()
		{
			if (IsOpen)
			{
				Close();
			}
			else
			{
				Open();
			}
		}

		private void HandleCloseInput()
		{
			if (IsOpen)
			{
				Close();
			}
		}

		private void HandleUseInput()
		{
			if (IsOpen)
			{
				UnlockSelected();
			}
		}

		private void HandleNavigate(Vector2 direction)
		{
			if (IsOpen && _nodes.Length != 0)
			{
				int num = ((!(Mathf.Abs(direction.x) > Mathf.Abs(direction.y))) ? ((direction.y > 0f) ? (-5) : 5) : ((direction.x > 0f) ? 1 : (-1)));
				SelectNode(Mathf.Clamp(_selectedIndex + num, 0, _nodes.Length - 1));
				SelectCurrentObject();
			}
		}

		private void HandleChanged()
		{
			if (IsOpen)
			{
				Refresh();
			}
		}

		private void HandleProgressionChanged(PlayerProgressionSnapshot _)
		{
			if (IsOpen)
			{
				Refresh();
			}
		}

		private void HandleInputBlocked(bool blocked)
		{
			if (blocked && IsOpen)
			{
				CloseInternal(restoreGameplay: false);
			}
		}

		private void HandleTransition(string _)
		{
			if (IsOpen)
			{
				CloseInternal(restoreGameplay: false);
			}
		}

		private void HandleDeath()
		{
			if (IsOpen)
			{
				CloseInternal(restoreGameplay: false);
			}
		}

		private void Refresh()
		{
			if (_initialized)
			{
				PlayerProgressionSnapshot state = _progression.State;
				pointsText.text = $"TECHNOLOGIEPUNKTE: {state.AvailableTechnologyPoints}";
				levelText.text = $"STUFE {state.Stage}  ·  LEVEL {state.Level}/" + state.MaximumLevel;
				for (int i = 0; i < nodeViews.Length; i++)
				{
					TechnologyNodeDefinition technologyNodeDefinition = ((i < _nodes.Length) ? _nodes[i] : null);
					nodeViews[i].Render(technologyNodeDefinition, (technologyNodeDefinition != null) ? _unlocks.GetNodeState(technologyNodeDefinition.Id) : TechnologyNodeState.Locked, i == _selectedIndex, (technologyNodeDefinition != null) ? _unlocks.GetLockReason(technologyNodeDefinition.Id) : string.Empty);
				}
				TechnologyNodeDefinition technologyNodeDefinition2 = ((_nodes.Length != 0) ? _nodes[_selectedIndex] : null);
				unlockButton.interactable = technologyNodeDefinition2 != null && _unlocks.GetNodeState(technologyNodeDefinition2.Id) == TechnologyNodeState.Available && state.AvailableTechnologyPoints >= technologyNodeDefinition2.PointCost;
			}
		}

		private void CloseInternal(bool restoreGameplay)
		{
			if (IsOpen)
			{
				IsOpen = false;
				panelRoot.SetActive(value: false);
				EventSystem.current?.SetSelectedGameObject(null);
				if (restoreGameplay && !_sceneFlow.IsInputBlocked && _health.IsAlive)
				{
					_input.SetGameplayEnabled(enabled: true);
					_motor.SetMovementEnabled(enabled: true);
				}
			}
		}

		private void SelectCurrentObject()
		{
			if (_nodes.Length != 0)
			{
				EventSystem.current?.SetSelectedGameObject(nodeViews[_selectedIndex].gameObject);
			}
		}

		private void ValidateReferences()
		{
			if (panelRoot == null || pointsText == null || levelText == null || feedbackText == null || nodeViews == null || nodeViews.Length < 23 || unlockButton == null || closeButton == null)
			{
				throw new InvalidOperationException("TechnologyTreeWindow prefab references are incomplete.");
			}
		}

		private void EnsureNodeViewCapacity(int required)
		{
			if (required > nodeViews.Length)
			{
				TechnologyNodeButtonView technologyNodeButtonView = nodeViews[nodeViews.Length - 1];
				TechnologyNodeButtonView[] array = new TechnologyNodeButtonView[required];
				Array.Copy(nodeViews, array, nodeViews.Length);
				for (int i = nodeViews.Length; i < required; i++)
				{
					TechnologyNodeButtonView technologyNodeButtonView2 = UnityEngine.Object.Instantiate(technologyNodeButtonView, technologyNodeButtonView.transform.parent);
					technologyNodeButtonView2.name = $"TechnologyNode_{i + 1:00}";
					array[i] = technologyNodeButtonView2;
				}
				nodeViews = array;
			}
		}

		private void Unbind()
		{
			if (_unlocks != null)
			{
				_unlocks.Changed -= HandleChanged;
			}
			if (_progression != null)
			{
				_progression.Changed -= HandleProgressionChanged;
			}
			if (_input != null)
			{
				_input.TechnologyTogglePressed -= HandleToggle;
				_input.InventoryClosePressed -= HandleCloseInput;
				_input.InventoryUsePressed -= HandleUseInput;
				_input.InventoryNavigate -= HandleNavigate;
			}
			if (_sceneFlow != null)
			{
				_sceneFlow.InputBlockedChanged -= HandleInputBlocked;
				_sceneFlow.SceneTransitionStarted -= HandleTransition;
			}
			if (_health != null)
			{
				_health.Died -= HandleDeath;
			}
		}

		private void OnDestroy()
		{
			if (IsOpen && _input != null && _motor != null && (_health == null || _health.IsAlive) && (_sceneFlow == null || !_sceneFlow.IsInputBlocked))
			{
				_input.SetGameplayEnabled(enabled: true);
				_motor.SetMovementEnabled(enabled: true);
			}
			Unbind();
		}

		private static TechnologyNodeDefinition[] SortedNodes(TechnologyTreeDefinition tree)
		{
			TechnologyNodeDefinition[] array = new TechnologyNodeDefinition[tree.Nodes.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = tree.Nodes[i];
			}
			Array.Sort(array, (TechnologyNodeDefinition first, TechnologyNodeDefinition second) => first.SortOrder.CompareTo(second.SortOrder));
			return array;
		}

		private static string ResultLabel(TechnologyUnlockResult result)
		{
			if (1 == 0)
			{
			}
			string result2 = result switch
			{
				TechnologyUnlockResult.Success => "Technologie freigeschaltet", 
				TechnologyUnlockResult.NotEnoughTechnologyPoints => "Technologiepunkt fehlt", 
				TechnologyUnlockResult.PrerequisiteMissing => "Voraussetzung fehlt", 
				TechnologyUnlockResult.WrongProgressionStage => "Progressionsstufe gesperrt", 
				TechnologyUnlockResult.AlreadyUnlocked => "Bereits freigeschaltet", 
				_ => "Unbekannte Technologie", 
			};
			if (1 == 0)
			{
			}
			return result2;
		}
	}
}
