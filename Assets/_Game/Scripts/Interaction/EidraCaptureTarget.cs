using Eidren.AI;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Player;
using System;
using UnityEngine;

namespace Eidren.Interaction
{
	[RequireComponent(typeof(EidraWildController))]
	public sealed class EidraCaptureTarget : MonoBehaviour, IInteractable
	{
		private const string NotConfiguredReason = "FANG NICHT VERFÜGBAR";

		[SerializeField]
		private EidraWildController controller;

		[SerializeField]
		[Min(0.5f)]
		private float interactionRange = InteractionUtility.StandardSurfaceRange;

		[SerializeField]
		[Min(0.1f)]
		private float holdDuration = 2.2f;

		[SerializeField]
		private int priority = 25;

		private CaptureEquipment _equipment;

		private EidraRosterService _roster;

		private PlayerMotor _channelMotor;

		public string InteractionId => (controller != null) ? controller.InstanceId : string.Empty;

		public InteractionType Type => InteractionType.Npc;

		public string DisplayText
		{
			get
			{
				EidraData eidraData = ((controller != null) ? controller.Eidra : null);
				string text = ((eidraData != null && !string.IsNullOrWhiteSpace(eidraData.DisplayName)) ? (eidraData.DisplayName.ToUpperInvariant() + " FANGEN") : "EIDRA FANGEN");
				if (eidraData == null || controller == null)
				{
					return text;
				}
				float num = CaptureResolution.RequirementAt(eidraData.Capture, controller.HealthFraction);
				return $"{text} · BEDARF {num:0}";
			}
		}

		public Sprite Icon => null;

		public float InteractionRange => interactionRange;

		public InteractionMode Mode => InteractionMode.Hold;

		public float HoldDuration => holdDuration;

		public int Priority => priority;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public event Action<EidraCaptureResult> Captured;

		public void Configure(CaptureEquipment equipment, EidraRosterService roster)
		{
			_equipment = equipment;
			_roster = roster;
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			return Evaluate(out blockedReason).CanCapture && base.isActiveAndEnabled;
		}

		public void BeginInteraction(in InteractionContext context)
		{
			ReleaseMovementLock();
			controller?.SetCaptureChannelActive(active: true);
			if (context.Actor != null)
			{
				_channelMotor = context.Actor.GetComponent<PlayerMotor>() ?? context.Actor.GetComponentInChildren<PlayerMotor>();
			}
			_channelMotor?.SetLocomotionLocked(locked: true);
		}

		public void UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
		}

		public void CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
			controller?.SetCaptureChannelActive(active: false);
			ReleaseMovementLock();
		}

		public void CompleteInteraction(in InteractionContext context)
		{
			try
			{
				string blockedReason;
				CaptureCheck captureCheck = Evaluate(out blockedReason);
				if (!captureCheck.CanCapture)
				{
					Debug.Log("Capture of '" + InteractionId + "' stopped at the commit point: " + blockedReason, this);
					return;
				}
				if (!_equipment.TryConsumeBattery(out var error))
				{
					Debug.LogError("Capture of '" + InteractionId + "' could not consume the battery: " + error, this);
					return;
				}
				if (!_roster.TryCapture(controller.Eidra.Id, out var instance, out var error2))
				{
					Debug.LogError("Capture of '" + InteractionId + "' consumed a battery but the roster refused it: " + error2, this);
					return;
				}
				controller.TryTakeForCapture();
				this.Captured?.Invoke(new EidraCaptureResult(InteractionId, instance, captureCheck.Charge, captureCheck.Requirement));
			}
			finally
			{
				controller?.SetCaptureChannelActive(active: false);
				ReleaseMovementLock();
			}
		}

		private CaptureCheck Evaluate(out string blockedReason)
		{
			if (controller == null || controller.Eidra == null || controller.HasDeparted || !controller.IsAlive)
			{
				blockedReason = "ZIEL NICHT VERFÜGBAR";
				return new CaptureCheck(canCapture: false, 0f, 0f, blockedReason);
			}
			if (_equipment == null || _roster == null)
			{
				blockedReason = "FANG NICHT VERFÜGBAR";
				return new CaptureCheck(canCapture: false, 0f, 0f, blockedReason);
			}
			CaptureCheck result = CaptureResolution.Evaluate(controller.Eidra.Capture, controller.HealthFraction, _equipment.ReadLoadout());
			blockedReason = result.BlockedReason;
			return result;
		}

		private void Reset()
		{
			controller = GetComponent<EidraWildController>();
		}

		private void Awake()
		{
			if (controller == null)
			{
				controller = GetComponent<EidraWildController>();
			}
		}

		private void OnDisable()
		{
			controller?.SetCaptureChannelActive(active: false);
			ReleaseMovementLock();
		}

		private void ReleaseMovementLock()
		{
			_channelMotor?.SetLocomotionLocked(locked: false);
			_channelMotor = null;
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

	public readonly struct EidraCaptureResult
	{
		public string ZoneInstanceId { get; }

		public EidraInstanceState Instance { get; }

		public float Charge { get; }

		public float Requirement { get; }

		public EidraCaptureResult(string zoneInstanceId, EidraInstanceState instance, float charge, float requirement)
		{
			ZoneInstanceId = zoneInstanceId ?? string.Empty;
			Instance = instance;
			Charge = charge;
			Requirement = requirement;
		}
	}
}
