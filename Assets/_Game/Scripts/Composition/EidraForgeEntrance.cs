using Eidren.Core.Services;
using Eidren.Interaction;
using System.Collections;
using System;
using UnityEngine;

namespace Eidren.Composition
{
	[DisallowMultipleComponent]
	public sealed class EidraForgeEntrance : MonoBehaviour, IInteractable
	{
		[SerializeField]
		private Sprite interactionIcon;

		[SerializeField]
		[Min(0.5f)]
		private float interactionRange = InteractionUtility.StandardSurfaceRange;

		/// <summary>
		/// Rückkehr aus dem Verlies (19.08.2026): Der Ausgang hinterlässt
		/// „EidraForge" als Herkunft — dann gehört der frisch gespawnte
		/// Spieler VOR dieses Portal statt an den Zonen-Spawn. Die Herkunft
		/// wird genau einmal verbraucht (der Ankunftsstatus taugt nicht als
		/// Signal, den setzt der MapExitCoordinator beim Spawn sofort um).
		/// </summary>
		private IEnumerator Start()
		{
			EidrenServiceRoot instance = EidrenServiceRoot.Instance;
			if (instance == null || !instance.GameSession.TryConsumeReturnFrom("EidraForge"))
			{
				yield break;
			}
			float zeit = 0f;
			PlayerPrefabBindings spieler = null;
			while (spieler == null && zeit < 10f)
			{
				zeit += Time.deltaTime;
				spieler = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
				if (spieler == null)
				{
					yield return null;
				}
			}
			if (spieler == null)
			{
				yield break;
			}
			CharacterController kapsel = spieler.GetComponent<CharacterController>();
			if (kapsel != null)
			{
				kapsel.enabled = false;
			}
			spieler.transform.position = base.transform.position + new Vector3(0f, 0.1f, -3f);
			if (kapsel != null)
			{
				kapsel.enabled = true;
			}
			Physics.SyncTransforms();
		}

		public string InteractionId => "entrance.eidra_forge";

		public InteractionType Type => InteractionType.WorldObject;

		public string DisplayText => "EIDRA-SCHMIEDE BETRETEN";

		public Sprite Icon => interactionIcon;

		public float InteractionRange => interactionRange;

		public InteractionMode Mode => InteractionMode.Hold;

		public float HoldDuration => 0.8f;

		public int Priority => 96;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			EidrenServiceRoot instance = EidrenServiceRoot.Instance;
			if (instance == null || context.ActorTransform == null)
			{
				blockedReason = "SCHMIEDE NICHT VERFÜGBAR";
				return false;
			}
			if (!instance.GameSession.HasProgressFlag("garon_defeated"))
			{
				blockedReason = "GARON BEWACHT DEN ZUGANG";
				return false;
			}
			if (instance.GameSession.EidraForge.GetAccessState(DateTime.UtcNow) == EidraForgeAccessState.Cooldown)
			{
				blockedReason = $"ABKLINGZEIT · {Math.Max(0, (int)Math.Ceiling((new DateTime(instance.GameSession.EidraForge.ReadyUtcTicks, DateTimeKind.Utc) - DateTime.UtcNow).TotalHours))}H";
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
			if (!CanInteract(in context, out var _))
			{
				return;
			}
			EidrenServiceRoot instance = EidrenServiceRoot.Instance;
			GameSession gameSession = instance.GameSession;
			switch (gameSession.EidraForge.GetAccessState(DateTime.UtcNow))
			{
			case EidraForgeAccessState.FirstRunAvailable:
				if (!gameSession.EidraForge.TryBeginFirstRun(Environment.TickCount))
				{
					return;
				}
				break;
			case EidraForgeAccessState.ResetAvailable:
			{
				if (!gameSession.TryResetEidraForge(DateTime.UtcNow, Environment.TickCount, playerInside: false, out var _))
				{
					return;
				}
				break;
			}
			}
			gameSession.RequestSave(SaveRequestReason.AreaTransitionConfirmed);
			instance.SceneFlowService.TryLoadScene("EidraForge");
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
