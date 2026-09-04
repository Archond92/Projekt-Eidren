using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Player;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	/// <summary>
	/// F32-006: Verwaltung des aktiven Eidra-Gespanns. Vorher konnte ein
	/// gefangenes Eidra jenseits der zwei Plaetze nie wieder eingesetzt
	/// werden — es gab keine Oberflaeche dafuer, obwohl der Dienst
	/// (TrySetActiveTeam) alles mitbrachte.
	///
	/// Das Fenster ruft nur den Dienst; die Laufzeit zieht von selbst nach,
	/// weil EidraTeamRosterBinding auf Roster-Aenderungen hoert.
	/// </summary>
	public sealed class EidraTeamWindow : MonoBehaviour
	{
		[SerializeField]
		private GameObject panelRoot;

		[SerializeField]
		private Text summaryText;

		[SerializeField]
		private Text feedbackText;

		[SerializeField]
		private Text overflowText;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private EidraTeamRowView[] rowViews = Array.Empty<EidraTeamRowView>();

		private EidraRosterService _roster;

		private ContentDatabase _content;

		private PlayerInputReader _input;

		private PlayerMotor _motor;

		private PlayerCombatController _combat;

		private Damageable _health;

		private SceneFlowService _sceneFlow;

		private GameSession _session;

		private string[] _instanceIds = Array.Empty<string>();

		private bool _initialized;

		public bool IsOpen { get; private set; }

		public int VisibleRowCount { get; private set; }

		public void ConfigureReferences(GameObject configuredPanel, Text configuredSummary, Text configuredFeedback, Text configuredOverflow, Button configuredClose, EidraTeamRowView[] configuredRows)
		{
			panelRoot = configuredPanel;
			summaryText = configuredSummary;
			feedbackText = configuredFeedback;
			overflowText = configuredOverflow;
			closeButton = configuredClose;
			rowViews = configuredRows ?? Array.Empty<EidraTeamRowView>();
		}

		public void Initialize(EidraRosterService roster, ContentDatabase content, PlayerInputReader input, PlayerMotor motor, PlayerCombatController combat, Damageable health, SceneFlowService sceneFlow, GameSession session)
		{
			Unbind();
			_roster = roster ?? throw new ArgumentNullException("roster");
			_content = content ?? throw new ArgumentNullException("content");
			_input = input ?? throw new ArgumentNullException("input");
			_motor = motor ?? throw new ArgumentNullException("motor");
			_combat = combat ?? throw new ArgumentNullException("combat");
			_health = health ?? throw new ArgumentNullException("health");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_session = session;
			for (int i = 0; i < rowViews.Length; i++)
			{
				rowViews[i].Bind(i, AssignSlot);
			}
			closeButton.onClick.RemoveAllListeners();
			closeButton.onClick.AddListener(Close);
			_input.EidraTeamTogglePressed += HandleToggle;
			_input.InventoryClosePressed += HandleCloseInput;
			_sceneFlow.InputBlockedChanged += HandleInputBlocked;
			_sceneFlow.SceneTransitionStarted += HandleTransition;
			_health.Died += HandleDeath;
			_roster.Roster.Changed += Refresh;
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
			}
		}

		public void Close()
		{
			CloseInternal(restoreGameplay: true);
		}

		/// <summary>
		/// Setzt das Eidra der Zeile auf den gewaehlten Platz. Die Regel
		/// steht in <see cref="EidraTeamAssignment"/> (rein und geprueft),
		/// hier bleibt nur der Weg zum Dienst.
		/// </summary>
		public void AssignSlot(int rowIndex, int slot)
		{
			if (!_initialized || rowIndex < 0 || rowIndex >= _instanceIds.Length)
			{
				return;
			}
			string[] gespann = EidraTeamAssignment.Assign(_roster.Roster.GetActiveInstanceIds(), _roster.ActiveSlotCapacity, _instanceIds[rowIndex], slot);
			if (!_roster.TrySetActiveTeam(gespann, out var error))
			{
				feedbackText.text = error ?? "Wechsel nicht moeglich.";
				return;
			}
			feedbackText.text = string.Empty;
			_session?.RequestSave(SaveRequestReason.StorageClosed);
			Refresh();
		}

		public void Refresh()
		{
			if (!_initialized)
			{
				return;
			}
			EidraInstanceState[] all = _roster.Roster.GetAll();
			_instanceIds = new string[all.Length];
			for (int j = 0; j < all.Length; j++)
			{
				_instanceIds[j] = all[j].InstanceId;
			}
			string[] activeInstanceIds = _roster.Roster.GetActiveInstanceIds();
			int capacity = _roster.ActiveSlotCapacity;
			summaryText.text = Summary(activeInstanceIds, capacity);
			int shown = Math.Min(_instanceIds.Length, rowViews.Length);
			VisibleRowCount = shown;
			for (int i = 0; i < rowViews.Length; i++)
			{
				if (i >= shown)
				{
					rowViews[i].Hide();
					continue;
				}
				string instanceId = _instanceIds[i];
				bool onFirst = activeInstanceIds.Length > 0 && string.Equals(activeInstanceIds[0], instanceId, StringComparison.Ordinal);
				bool onSecond = activeInstanceIds.Length > 1 && string.Equals(activeInstanceIds[1], instanceId, StringComparison.Ordinal);
				rowViews[i].Show(DisplayName(instanceId), State(onFirst, onSecond, capacity), capacity > 1, onFirst, onSecond);
			}
			overflowText.text = ((_instanceIds.Length > rowViews.Length) ? $"… und {_instanceIds.Length - rowViews.Length} weitere" : string.Empty);
		}

		private string Summary(string[] activeInstanceIds, int capacity)
		{
			string first = (activeInstanceIds.Length > 0) ? DisplayName(activeInstanceIds[0]) : "frei";
			if (capacity <= 1)
			{
				return "PLATZ 1: " + first + "  ·  PLATZ 2: noch nicht freigeschaltet";
			}
			string second = (activeInstanceIds.Length > 1) ? DisplayName(activeInstanceIds[1]) : "frei";
			return "PLATZ 1: " + first + "  ·  PLATZ 2: " + second;
		}

		private static string State(bool onFirst, bool onSecond, int capacity)
		{
			if (onFirst)
			{
				return "AKTIV · PLATZ 1";
			}
			if (onSecond)
			{
				return "AKTIV · PLATZ 2";
			}
			return (capacity > 1) ? "auf der Bank" : "auf der Bank (nur ein Platz frei)";
		}

		private string DisplayName(string instanceId)
		{
			if (!_roster.Roster.TryGet(instanceId, out var state))
			{
				return instanceId ?? string.Empty;
			}
			if (_content.TryGetEidra(state.EidraId, out var data) && data != null && !string.IsNullOrWhiteSpace(data.DisplayName))
			{
				return data.DisplayName + "  (" + instanceId + ")";
			}
			return instanceId;
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

		private void HandleInputBlocked(bool blocked)
		{
			if (blocked && IsOpen)
			{
				CloseInternal(restoreGameplay: false);
			}
		}

		private void HandleTransition(string sceneName)
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

		private void CloseInternal(bool restoreGameplay)
		{
			if (IsOpen)
			{
				IsOpen = false;
				panelRoot.SetActive(value: false);
				if (restoreGameplay && _health.IsAlive && !_sceneFlow.IsInputBlocked)
				{
					_input.SetGameplayEnabled(enabled: true);
					_motor.SetMovementEnabled(enabled: true);
				}
			}
		}

		private void Unbind()
		{
			if (_input != null)
			{
				_input.EidraTeamTogglePressed -= HandleToggle;
				_input.InventoryClosePressed -= HandleCloseInput;
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
			if (_roster != null)
			{
				_roster.Roster.Changed -= Refresh;
			}
			_initialized = false;
		}

		private void OnDestroy()
		{
			Unbind();
		}
	}
}
