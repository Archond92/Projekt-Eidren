using Eidren.Data;
using System;
using UnityEngine;

namespace Eidren.Interaction
{
	[DisallowMultipleComponent]
	public sealed class WorkbenchController : MonoBehaviour, IInteractable
	{
		[SerializeField]
		private string interactionId = "home_base.workbench";

		[SerializeField]
		private string displayText = "Werkbank benutzen";

		[SerializeField]
		private Sprite icon;

		// F32-001: Gemessen wird bis zur Zellmitte. Eine Gebaeudezelle ist 1 m
		// breit, die Figur hat Radius 0,48 — Beruehrung heisst 0,98 m. Mit 2,4
		// liessen sich Gebaeude aus knapp zwei Zellbreiten Luft anwaehlen.
		[SerializeField]
		[Min(0.5f)]
		private float interactionRange = InteractionUtility.StandardSurfaceRange;

		[SerializeField]
		private CraftingStationType station = CraftingStationType.Workbench;

		public string InteractionId => interactionId ?? string.Empty;

		public InteractionType Type => InteractionType.Station;

		public string DisplayText => displayText ?? string.Empty;

		public Sprite Icon => icon;

		public float InteractionRange => interactionRange;

		public InteractionMode Mode => InteractionMode.Instant;

		public float HoldDuration => 0f;

		public int Priority => 80;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public CraftingStationType Station => station;

		public event Action<WorkbenchController> Activated;

		public void Configure(string id, string text, Sprite configuredIcon, float range, CraftingStationType stationType)
		{
			interactionId = id ?? string.Empty;
			displayText = text ?? string.Empty;
			icon = configuredIcon;
			interactionRange = Mathf.Max(0.5f, range);
			station = stationType;
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			bool flag = context.ActorTransform != null;
			blockedReason = (flag ? string.Empty : "Kein Spieler verfügbar");
			return flag;
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
			this.Activated?.Invoke(this);
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
