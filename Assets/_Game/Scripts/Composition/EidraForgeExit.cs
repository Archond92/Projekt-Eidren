using Eidren.Core.Services;
using Eidren.Interaction;
using UnityEngine;

namespace Eidren.Composition
{
	/// <summary>
	/// Tester-Runde 19.08.2026: Der Ausgang des Verlieses am Windfang —
	/// vorher war die Schmiede eine Einbahnstraße. Spiegel des
	/// EidraForgeEntrance: Halte-Interaktion, danach laden die Glutruinen
	/// und der Eingang setzt den Rückkehrer direkt ans Portal (der
	/// bestätigte Übergang trägt „EidraForge" als Herkunft).
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class EidraForgeExit : MonoBehaviour, IInteractable
	{
		[SerializeField]
		private Sprite interactionIcon;

		[SerializeField]
		[Min(0.5f)]
		private float interactionRange = InteractionUtility.StandardSurfaceRange;

		public string InteractionId => "exit.eidra_forge";

		public InteractionType Type => InteractionType.WorldObject;

		public string DisplayText => "VERLIES VERLASSEN";

		public Sprite Icon => interactionIcon;

		public float InteractionRange => interactionRange;

		public InteractionMode Mode => InteractionMode.Hold;

		public float HoldDuration => 0.8f;

		public int Priority => 96;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			if (EidrenServiceRoot.Instance == null || context.ActorTransform == null)
			{
				blockedReason = "AUSGANG NICHT VERFÜGBAR";
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
			// Der bestätigte Übergang macht die Herkunft „EidraForge" für den
			// Rückkehr-Teleport des Eingangs lesbar (und speichert).
			instance.GameSession.RecordConfirmedExit("EidraForge", MapExitDirection.None, EidrenScenes.ZoneEmberRuins);
			instance.SceneFlowService.TryLoadScene(EidrenScenes.ZoneEmberRuins);
		}
	}
}
