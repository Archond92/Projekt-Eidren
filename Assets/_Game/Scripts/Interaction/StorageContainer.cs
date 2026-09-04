using Eidren.Core.Services;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Interaction
{
	[DisallowMultipleComponent]
	public sealed class StorageContainer : MonoBehaviour, IInspectableContainer, IItemContainer, IInteractable, IMinimapDiscoverable
	{
		public const int StorageSlotCount = 24;

		[SerializeField]
		private string containerId = "home_base.storage.main";

		[SerializeField]
		private string displayText = "Lagerkiste öffnen";

		[SerializeField]
		private Sprite icon;

		// F32-001: Beruehrung einer Gebaeudezelle heisst 0,98 m zur Zellmitte;
		// 2,5 liess knapp zwei Zellbreiten Luft.
		[SerializeField]
		[Min(0.5f)]
		private float interactionRange = InteractionUtility.StandardSurfaceRange;

		[SerializeField]
		private ItemStack[] initialSlots = new ItemStack[24];

		private readonly ItemStack[] _slots = new ItemStack[24];

		private ContentDatabase _database;

		private GameSession _session;

		private bool _initialized;

		public string ContainerId => containerId ?? string.Empty;

		public string ContainerDisplayName => "Lagerkiste";

		public int SlotCapacity => 24;

		public string InteractionId => ContainerId;

		public InteractionType Type => InteractionType.Container;

		public string DisplayText => displayText ?? string.Empty;

		public Sprite Icon => icon;

		public float InteractionRange => interactionRange;

		public InteractionMode Mode => InteractionMode.Instant;

		public float HoldDuration => 0f;

		public int Priority => 85;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public event Action<StorageContainerChange> Changed;

		public event Action<StorageContainer> Activated;

		public void Configure(string stableId, string text, Sprite configuredIcon, float range)
		{
			containerId = stableId ?? string.Empty;
			displayText = text ?? string.Empty;
			icon = configuredIcon;
			interactionRange = Mathf.Max(0.5f, range);
			initialSlots = new ItemStack[24];
		}

		public void Initialize(ContentDatabase database, GameSession session)
		{
			_database = database ?? throw new ArgumentNullException("database");
			_session = session ?? throw new ArgumentNullException("session");
			if (string.IsNullOrWhiteSpace(ContainerId))
			{
				throw new InvalidOperationException("StorageContainer '" + base.name + "' requires a stable ID.");
			}
			if (_session.TryGetStorageState(ContainerId, out var state))
			{
				if (!TryImportState(state, out var error))
				{
					throw new InvalidOperationException(error);
				}
			}
			else
			{
				ItemStack[] slots = ((initialSlots != null && initialSlots.Length == 24) ? initialSlots : new ItemStack[24]);
				if (!TryImportSlots(slots, out var error2))
				{
					throw new InvalidOperationException(error2);
				}
			}
			_initialized = true;
		}

		public bool TryGetSlot(int index, out ItemStack stack)
		{
			if (index < 0 || index >= 24)
			{
				stack = ItemStack.Empty;
				return false;
			}
			stack = _slots[index].Copy();
			return true;
		}

		public ItemStack[] ExportSlots()
		{
			ItemStack[] array = new ItemStack[24];
			for (int i = 0; i < 24; i++)
			{
				array[i] = _slots[i].Copy();
			}
			return array;
		}

		public StorageContainerState ExportState()
		{
			return new StorageContainerState(ContainerId, ExportSlots());
		}

		public bool TryImportState(StorageContainerState state, out string error)
		{
			if (state == null || !string.Equals(state.ContainerId, ContainerId, StringComparison.Ordinal))
			{
				error = "Storage state ID does not match '" + ContainerId + "'.";
				return false;
			}
			return TryImportSlots(state.Slots, out error);
		}

		public bool TryImportSlots(IReadOnlyList<ItemStack> slots, out string error)
		{
			if (slots == null || slots.Count != 24)
			{
				error = "Storage container '" + ContainerId + "' requires exactly " + $"{24} slots.";
				return false;
			}
			if (_database == null)
			{
				error = "Storage container '" + ContainerId + "' is not initialized.";
				return false;
			}
			ItemStack[] array = new ItemStack[24];
			bool flag = false;
			for (int i = 0; i < 24; i++)
			{
				array[i] = slots[i].Copy();
				if (!array[i].TryValidate(_database, out var error2))
				{
					error = $"Storage slot {i} is invalid: " + error2;
					return false;
				}
				flag |= !StacksEqual(array[i], _slots[i]);
			}
			if (!flag)
			{
				error = string.Empty;
				return true;
			}
			Array.Copy(array, _slots, 24);
			_session?.SetStorageState(ExportState());
			this.Changed?.Invoke(new StorageContainerChange(ContainerId));
			error = string.Empty;
			return true;
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			bool flag = _initialized && context.ActorTransform != null;
			blockedReason = (flag ? string.Empty : "Lager ist nicht verfügbar");
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
			if (_initialized)
			{
				this.Activated?.Invoke(this);
			}
		}

		private static bool StacksEqual(ItemStack first, ItemStack second)
		{
			return first.Quantity == second.Quantity && first.Durability == second.Durability && string.Equals(first.ItemId, second.ItemId, StringComparison.Ordinal) && string.Equals(first.InstanceId, second.InstanceId, StringComparison.Ordinal);
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

		// N04-001: Lagerkisten erscheinen erst auf der Karte, wenn sie einmal im
		// Bild waren — und bleiben danach sichtbar.
		private bool _seenOnMinimap;

		MinimapMarkerKind IMinimapMarker.MinimapKind => MinimapMarkerKind.Chest;

		Vector3 IMinimapMarker.MinimapPosition => base.transform.position;

		// Bewusste Ausnahme zum Nutzerentscheid vom 17.08.2026: Die eigene
		// Lagerkiste ist kein Fund, sondern Einrichtung — sie bleibt auf der
		// Karte, auch wenn sie leer ist.
		bool IMinimapMarker.ShowsOnMinimap => _seenOnMinimap;

		string IMinimapMarker.MinimapPaletteId => string.Empty;

		void IMinimapDiscoverable.MarkSeenOnMinimap()
		{
			_seenOnMinimap = true;
		}

		private void OnEnable()
		{
			MinimapRegistry.Register(this);
		}

		private void OnDisable()
		{
			MinimapRegistry.Unregister(this);
		}
	}

	public readonly struct StorageContainerChange
	{
		public string ContainerId { get; }

		public StorageContainerChange(string containerId)
		{
			ContainerId = containerId ?? string.Empty;
		}
	}
}
