using Eidren.Core.Services;
using UnityEngine;

namespace Eidren.Interaction
{
	[DisallowMultipleComponent]
	public sealed class FarmPlotController : MonoBehaviour, IInteractable
	{
		[SerializeField]
		private Sprite icon;

		// F32-001: Der Acker ist als einziges Gebaeude 2x2 — Beruehrung heisst
		// hier 1,48 m zur Mitte, entsprechend mehr Reichweite als bei den
		// Einzelzellen (1,6).
		[SerializeField]
		[Min(0.5f)]
		private float interactionRange = InteractionUtility.StandardSurfaceRange;

		[SerializeField]
		private GameObject emptyVisual;

		[SerializeField]
		private GameObject plantedVisual;

		[SerializeField]
		private GameObject readyVisual;

		private string _instanceId = string.Empty;

		private FarmProductionService _production;

		private GameSession _session;

		public string InteractionId => _instanceId + ".farm";

		public InteractionType Type => InteractionType.Station;

		public string DisplayText => GetDisplayText();

		public Sprite Icon => icon;

		public float InteractionRange => interactionRange;

		public InteractionMode Mode => InteractionMode.Instant;

		public float HoldDuration => 0f;

		public int Priority => 82;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public void Initialize(string instanceId, FarmProductionService production, GameSession session)
		{
			UnbindProductionChanges();
			_instanceId = instanceId ?? string.Empty;
			_production = production;
			_session = session;
			if (_session != null)
			{
				_session.Buildings.Changed += RefreshVisualState;
			}
			RefreshVisualState();
		}

		public void ConfigureVisuals(GameObject empty, GameObject planted, GameObject ready)
		{
			emptyVisual = empty;
			plantedVisual = planted;
			readyVisual = ready;
			RefreshVisualState();
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			if (context.ActorTransform == null || _production == null || !_production.TryGetFarm(_instanceId, out var state))
			{
				blockedReason = "Acker ist nicht verfuegbar";
				return false;
			}
			if (state.ReadyOutput > 0)
			{
				blockedReason = string.Empty;
				return true;
			}
			if (state.PlantedSlots >= 4)
			{
				blockedReason = "Waechst bis zur naechsten Heimkehr";
				return false;
			}
			if (context.Inventory == null || !context.Inventory.Contains("wheat_seed"))
			{
				blockedReason = "Weizensamen fehlt";
				return false;
			}
			blockedReason = string.Empty;
			return true;
		}

		public void BeginInteraction(in InteractionContext context)
		{
		}

		public void UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
		}

		public void CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
		}

		public void CompleteInteraction(in InteractionContext context)
		{
			if (_production != null && _production.TryGetFarm(_instanceId, out var state) && ((state.ReadyOutput > 0) ? _production.TryHarvest(_instanceId) : _production.TryPlantSeed(_instanceId)) == FarmActionResult.Success)
			{
				RefreshVisualState();
				_session?.RequestSave(SaveRequestReason.ProductionChanged);
			}
		}

		private string GetDisplayText()
		{
			if (_production == null || !_production.TryGetFarm(_instanceId, out var state))
			{
				return "Acker";
			}
			if (state.ReadyOutput > 0)
			{
				return $"{state.ReadyOutput} Weizen ernten";
			}
			if (state.PlantedSlots >= 4)
			{
				return "Acker bestellt (4/4)";
			}
			return $"Samen einsetzen ({state.PlantedSlots}/" + $"{4})";
		}

		private void RefreshVisualState()
		{
			bool flag = false;
			bool flag2 = false;
			if (_production != null && _production.TryGetFarm(_instanceId, out var state))
			{
				flag = state.PlantedSlots > 0;
				flag2 = state.ReadyOutput > 0;
			}
			SetVisual(emptyVisual, !flag && !flag2);
			SetVisual(plantedVisual, flag && !flag2);
			SetVisual(readyVisual, flag2);
		}

		private static void SetVisual(GameObject target, bool visible)
		{
			if (target != null && target.activeSelf != visible)
			{
				target.SetActive(visible);
			}
		}

		private void UnbindProductionChanges()
		{
			if (_session != null)
			{
				_session.Buildings.Changed -= RefreshVisualState;
			}
		}

		private void OnDestroy()
		{
			UnbindProductionChanges();
		}

		bool IInteractable.CanInteract(in InteractionContext context, out string blockedReason)
		{
			return CanInteract(in context, out blockedReason);
		}

		void IInteractable.BeginInteraction(in InteractionContext context)
		{
			BeginInteraction(in context);
		}

		void IInteractable.UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
			UpdateInteraction(in context, normalizedProgress);
		}

		void IInteractable.CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
			CancelInteraction(in context, reason);
		}

		void IInteractable.CompleteInteraction(in InteractionContext context)
		{
			CompleteInteraction(in context);
		}
	}
}
