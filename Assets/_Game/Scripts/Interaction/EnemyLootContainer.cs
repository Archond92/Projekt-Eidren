using Eidren.Core.Services;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Interaction
{
	[DisallowMultipleComponent]
	public sealed class EnemyLootContainer : MonoBehaviour, IInspectableContainer, IItemContainer, IInteractable, IMinimapDiscoverable
	{
		public const int LootSlotCount = 24;

		private const int LootPriority = 90;

		private readonly ItemStack[] _slots = new ItemStack[24];

		private ContentDatabase _database;

		private string _containerId = string.Empty;

		private float _interactionRange = InteractionUtility.StandardSurfaceRange;

		private bool _initialized;

		private GameObject _marker;

		public string ContainerId => _containerId;

		public string ContainerDisplayName => "Gegnerbeute";

		public int SlotCapacity => 24;

		public string InteractionId => _containerId;

		public InteractionType Type => InteractionType.Container;

		public string DisplayText => "BEUTE DURCHSUCHEN";

		public Sprite Icon => null;

		public float InteractionRange => _interactionRange;

		public InteractionMode Mode => InteractionMode.Instant;

		public float HoldDuration => 0f;

		public int Priority => 90;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public bool IsEmpty
		{
			get
			{
				for (int i = 0; i < 24; i++)
				{
					if (!_slots[i].IsEmpty)
					{
						return false;
					}
				}
				return true;
			}
		}

		public event Action<StorageContainerChange> Changed;

		public event Action<EnemyLootContainer> Activated;

		public event Action<EnemyLootContainer> Emptied;

		public void Initialize(ContentDatabase database, string stableContainerId, IReadOnlyList<InventoryItemAmount> contents, float range)
		{
			_database = database ?? throw new ArgumentNullException("database");
			if (string.IsNullOrWhiteSpace(stableContainerId))
			{
				throw new ArgumentException("An enemy loot container requires a stable ID.", "stableContainerId");
			}
			_containerId = stableContainerId;
			_interactionRange = Mathf.Max(0.5f, range);
			Array.Clear(_slots, 0, 24);
			if (contents != null)
			{
				Fill(contents);
			}
			_initialized = true;
			UpdateMarker();
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

		public bool TryImportSlots(IReadOnlyList<ItemStack> slots, out string error)
		{
			if (slots == null || slots.Count != 24)
			{
				error = "Enemy loot '" + _containerId + "' requires exactly " + $"{24} slots.";
				return false;
			}
			if (_database == null)
			{
				error = "Enemy loot '" + _containerId + "' is not initialized.";
				return false;
			}
			ItemStack[] array = new ItemStack[24];
			bool flag = false;
			for (int i = 0; i < 24; i++)
			{
				array[i] = slots[i].Copy();
				if (!array[i].TryValidate(_database, out var error2))
				{
					error = $"Enemy loot slot {i} is invalid: " + error2;
					return false;
				}
				flag |= !StacksEqual(array[i], _slots[i]);
			}
			if (!flag)
			{
				error = string.Empty;
				return true;
			}
			bool isEmpty = IsEmpty;
			Array.Copy(array, _slots, 24);
			UpdateMarker();
			this.Changed?.Invoke(new StorageContainerChange(_containerId));
			if (!isEmpty && IsEmpty)
			{
				this.Emptied?.Invoke(this);
			}
			error = string.Empty;
			return true;
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			if (!_initialized || context.ActorTransform == null)
			{
				blockedReason = "BEUTE NICHT VERFÜGBAR";
				return false;
			}
			if (IsEmpty)
			{
				blockedReason = "DURCHSUCHT";
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
			if (_initialized && !IsEmpty)
			{
				this.Activated?.Invoke(this);
			}
		}

		private void Fill(IReadOnlyList<InventoryItemAmount> contents)
		{
			foreach (InventoryItemAmount content in contents)
			{
				if (content.Amount <= 0 || !_database.TryGetItem(content.ItemId, out var value))
				{
					Debug.LogError("Enemy loot '" + _containerId + "' references the unknown item '" + content.ItemId + "'.", this);
					continue;
				}
				int num = content.Amount;
				for (int i = 0; i < 24; i++)
				{
					if (num <= 0)
					{
						break;
					}
					if (_slots[i].IsEmpty)
					{
						int num2 = Mathf.Min(num, value.MaximumStackSize);
						_slots[i] = ItemStack.Create(value, num2);
						num -= num2;
					}
				}
				if (num > 0)
				{
					Debug.LogError("Enemy loot '" + _containerId + "' has no room for " + $"{num}x '{content.ItemId}'.", this);
				}
			}
		}

		private void UpdateMarker()
		{
			if (IsEmpty)
			{
				if (_marker != null)
				{
					_marker.SetActive(value: false);
				}
				return;
			}
			if (_marker == null)
			{
				_marker = CreateMarker();
			}
			_marker.SetActive(value: true);
		}

		private GameObject CreateMarker()
		{
			GameObject gameObject = new GameObject("LootGlow");
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			gameObject.transform.localPosition = new Vector3(0f, 0.45f, 0f);
			Light light = gameObject.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = new Color(0.94f, 0.68f, 0.28f);
			light.intensity = 1.6f;
			light.range = 2.4f;
			light.shadows = LightShadows.None;
			return gameObject;
		}

		private static bool StacksEqual(ItemStack first, ItemStack second)
		{
			return first.Quantity == second.Quantity && string.Equals(first.ItemId, second.ItemId, StringComparison.Ordinal) && string.Equals(first.InstanceId, second.InstanceId, StringComparison.Ordinal);
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

		// N04-001: Beutesaecke erscheinen erst auf der Karte, wenn sie einmal im
		// Bild waren — und bleiben danach sichtbar.
		private bool _seenOnMinimap;

		MinimapMarkerKind IMinimapMarker.MinimapKind => MinimapMarkerKind.Chest;

		Vector3 IMinimapMarker.MinimapPosition => base.transform.position;

		// Nutzerentscheid 17.08.2026: Ein durchsuchter Beutesack verschwindet von
		// der Karte.
		bool IMinimapMarker.ShowsOnMinimap => _seenOnMinimap && !IsEmpty;

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
}
