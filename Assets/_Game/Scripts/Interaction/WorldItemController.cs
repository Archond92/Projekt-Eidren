using Eidren.Data;
using Eidren.Presentation;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Interaction
{
	public sealed class WorldItemController : MonoBehaviour, IInteractable
	{
		[SerializeField]
		private ItemDefinition item;

		[SerializeField]
		[Min(1f)]
		private int quantity = 1;

		[SerializeField]
		private string instanceId = "world_item.unconfigured";

		[SerializeField]
		private SpriteRenderer itemIcon;

		[SerializeField]
		private TextMesh quantityLabel;

		[SerializeField]
		private GameObject highlight;

		[SerializeField]
		private Collider interactionTrigger;

		[SerializeField]
		[Min(0.5f)]
		private float interactionRange = InteractionUtility.StandardSurfaceRange;

		[SerializeField]
		private int priority = 30;

		private bool _collected;

		public ItemDefinition Item => item;

		public int Quantity => Mathf.Max(1, quantity);

		public bool IsCollected => _collected;

		public string InteractionId => instanceId ?? string.Empty;

		public InteractionType Type => InteractionType.WorldObject;

		public string DisplayText => (item == null) ? "AUFHEBEN" : $"{item.DisplayName} x{Quantity}\nAUFHEBEN";

		public Sprite Icon => (item != null) ? item.Icon : null;

		public float InteractionRange => Mathf.Max(0.5f, interactionRange);

		public InteractionMode Mode => InteractionMode.Instant;

		public float HoldDuration => 0f;

		public int Priority => priority;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public event Action<WorldItemPickupResult> PickedUp;

		public event Action<string> PickupBlocked;

		public event Action PickupAudioRequested;

		public void ConfigurePrefab(SpriteRenderer configuredIcon, TextMesh configuredQuantityLabel, GameObject configuredHighlight, Collider configuredTrigger)
		{
			itemIcon = configuredIcon;
			quantityLabel = configuredQuantityLabel;
			highlight = configuredHighlight;
			interactionTrigger = configuredTrigger;
			ConfigureTrigger();
		}

		public void Initialize(ItemDefinition definition, int amount, string stableInstanceId)
		{
			if (definition == null)
			{
				throw new ArgumentNullException("definition");
			}
			if (amount < 1)
			{
				throw new ArgumentOutOfRangeException("amount", amount, "A world item requires a positive quantity.");
			}
			if (string.IsNullOrWhiteSpace(stableInstanceId))
			{
				throw new ArgumentException("A stable world-item instance ID is required.", "stableInstanceId");
			}
			item = definition;
			quantity = amount;
			instanceId = stableInstanceId;
			_collected = false;
			ConfigureTrigger();
			if (interactionTrigger != null)
			{
				interactionTrigger.enabled = true;
			}
			if (highlight != null)
			{
				highlight.SetActive(value: true);
			}
			RefreshVisual();
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			if (_collected || item == null)
			{
				blockedReason = "BEUTE NICHT VERFÜGBAR";
				return false;
			}
			if (context.Inventory == null)
			{
				blockedReason = "INVENTAR NICHT VERFÜGBAR";
				return false;
			}
			if (!context.Inventory.CanAdd(item.Id, Quantity, out var _))
			{
				blockedReason = "INVENTAR VOLL";
				return false;
			}
			blockedReason = string.Empty;
			return base.isActiveAndEnabled;
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
			if (!CanInteract(in context, out var blockedReason))
			{
				this.PickupBlocked?.Invoke(blockedReason);
				return;
			}
			if (!context.Inventory.TryAddAll(item.Id, Quantity))
			{
				this.PickupBlocked?.Invoke("INVENTAR VOLL");
				return;
			}
			_collected = true;
			if (interactionTrigger != null)
			{
				interactionTrigger.enabled = false;
			}
			if (highlight != null)
			{
				highlight.SetActive(value: false);
			}
			WorldItemPickupResult obj = new WorldItemPickupResult(InteractionId, item, Quantity);
			this.PickedUp?.Invoke(obj);
			this.PickupAudioRequested?.Invoke();
			CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 1.8f, $"+{Quantity} {item.DisplayName}", new Color(0.42f, 0.92f, 0.55f), 1.25f);
			CombatFeedback.SpawnStyleCue(base.transform.position + Vector3.up * 0.55f, StyleCue.ItemPickup);
			base.gameObject.SetActive(value: false);
			UnityEngine.Object.Destroy(base.gameObject);
		}

		private void Awake()
		{
			ConfigureTrigger();
			RefreshVisual();
		}

		private void ConfigureTrigger()
		{
			if (interactionTrigger == null)
			{
				SphereCollider sphereCollider = GetComponent<SphereCollider>();
				if (sphereCollider == null)
				{
					sphereCollider = base.gameObject.AddComponent<SphereCollider>();
				}
				interactionTrigger = sphereCollider;
			}
			interactionTrigger.isTrigger = true;
			if (interactionTrigger is SphereCollider sphereCollider2)
			{
				sphereCollider2.radius = 0.72f;
			}
		}

		private void RefreshVisual()
		{
			if (itemIcon != null)
			{
				itemIcon.sprite = ((item != null) ? item.Icon : null);
				itemIcon.enabled = itemIcon.sprite != null;
				Camera main = Camera.main;
				if (main != null)
				{
					itemIcon.transform.rotation = main.transform.rotation;
				}
			}
			if (quantityLabel != null)
			{
				quantityLabel.text = ((Quantity > 1) ? $"x{Quantity}" : string.Empty);
				Camera main2 = Camera.main;
				if (main2 != null)
				{
					quantityLabel.transform.rotation = main2.transform.rotation;
				}
			}
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

	public readonly struct WorldItemPickupResult
	{
		public string InstanceId { get; }

		public ItemDefinition Item { get; }

		public int Amount { get; }

		public WorldItemPickupResult(string instanceId, ItemDefinition item, int amount)
		{
			InstanceId = instanceId ?? string.Empty;
			Item = item;
			Amount = amount;
		}
	}
}
