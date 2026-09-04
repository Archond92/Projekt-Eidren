using Eidren.Core.Services;
using System;
using System.Text;
using UnityEngine;

namespace Eidren.Interaction
{
	/// <summary>
	/// Der Bauplatz des Formwalls im Einbruch. Erst raeumen, dann bauen, danach
	/// einmal pro Lauf ernten - die Regeln liegen im EidraForgeDungeonService,
	/// hier steht nur die Bedienung.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class EidraForgeFormwallSite : MonoBehaviour, IInteractable
	{
		[SerializeField]
		private string stableId = "forge.formwall";

		[SerializeField]
		private Sprite interactionIcon;

		[SerializeField]
		[Min(0.5f)]
		private float interactionRange = InteractionUtility.StandardSurfaceRange;

		private GameSession _session;

		private EidraForgeDungeonService _dungeon;

		private bool _initialized;

		/// <summary>Meldet, ob gerade gebaut oder geerntet wurde - fuer Rueckmeldung
		/// und spaeteres Umschalten des Aussehens.</summary>
		public event Action<bool> Ausgefuehrt;

		public string InteractionId => stableId ?? string.Empty;

		public InteractionType Type => InteractionType.Station;

		public string DisplayText => (_dungeon != null && _dungeon.IstFormwallGebaut) ? "BESCHLÄGE ABHOLEN" : "FORMWALL AUFBAUEN";

		public Sprite Icon => interactionIcon;

		public float InteractionRange => interactionRange;

		/* Bauen ist eine Investition, kein Versehen: Halten statt Antippen. Das
		   Ernten ist harmlos und darf sofort gehen. */
		public InteractionMode Mode => (_dungeon != null && _dungeon.IstFormwallGebaut)
			? InteractionMode.Instant
			: InteractionMode.Hold;

		public float HoldDuration => (_dungeon != null && _dungeon.IstFormwallGebaut) ? 0f : 1.5f;

		public int Priority => 91;

		public Vector3 InteractionPosition => transform.position;

		public GameObject InteractionObject => gameObject;

		public void Initialize(GameSession session)
		{
			_session = session ?? throw new ArgumentNullException("session");
			_dungeon = session.EidraForge;
			_initialized = true;
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			if (!_initialized || context.ActorTransform == null)
			{
				blockedReason = "BAUPLATZ NICHT VERFÜGBAR";
				return false;
			}
			if (_dungeon.IstFormwallGebaut)
			{
				if (!_dungeon.IstFormwallErtragOffen)
				{
					blockedReason = "ERTRAG BEREITS ABGEHOLT";
					return false;
				}
				blockedReason = string.Empty;
				return true;
			}
			if (!_dungeon.IstBauplatzFrei())
			{
				blockedReason = "ASCHELÄUFER NISTEN NOCH IM SCHUTT";
				return false;
			}
			string fehlend = FehlendesMaterial();
			if (fehlend.Length > 0)
			{
				blockedReason = "ES FEHLT: " + fehlend;
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
			if (!_initialized)
			{
				return;
			}
			if (_dungeon.IstFormwallGebaut)
			{
				if (_dungeon.TryErnteFormwall(_session.PlayerInventory))
				{
					Ausgefuehrt?.Invoke(false);
				}
				return;
			}
			if (_dungeon.TryBaueFormwall(_session.PlayerInventory, out _))
			{
				Ausgefuehrt?.Invoke(true);
			}
		}

		/// <summary>
		/// Benennt, was zum Bauen noch fehlt. Ohne diese Auskunft stuende der
		/// Spieler vor einem gesperrten Bauplatz, ohne zu wissen warum.
		/// </summary>
		private string FehlendesMaterial()
		{
			StringBuilder liste = new StringBuilder();
			foreach (InventoryItemAmount posten in EidraForgeDungeonService.FormwallKostenListe)
			{
				int vorhanden = _session.PlayerInventory.GetTotalAmount(posten.ItemId);
				if (vorhanden >= posten.Amount)
				{
					continue;
				}
				if (liste.Length > 0)
				{
					liste.Append(", ");
				}
				liste.Append(posten.Amount - vorhanden).Append("× ").Append(Anzeigename(posten.ItemId));
			}
			return liste.ToString();
		}

		private static string Anzeigename(string itemId)
		{
			return itemId switch
			{
				"stone_block" => "STEINBLOCK",
				"plank" => "BRETT",
				"copper_bar" => "KUPFERBARREN",
				"smithing_fitting" => "SCHMIEDEBESCHLAG",
				_ => itemId.ToUpperInvariant()
			};
		}
	}
}
