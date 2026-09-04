using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Presentation;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Interaction
{
	[DisallowMultipleComponent]
	public sealed class EidraForgeChestContainer : MonoBehaviour, IInspectableContainer, IItemContainer, IInteractable, IMinimapDiscoverable
	{
		[SerializeField]
		private ForgePhysicalChestKind kind;

		[SerializeField]
		private string stableId = string.Empty;

		[SerializeField]
		private ForgeRewardChestSize rewardSize;

		[SerializeField]
		private Sprite interactionIcon;

		[SerializeField]
		[Min(0.5f)]
		private float interactionRange = InteractionUtility.StandardSurfaceRange;

		private readonly ItemStack[] _slots = new ItemStack[24];

		private EidraForgeChestService _chests;

		private EidraForgeDungeonService _dungeon;

		private EidraForgeLootProfile _profile;

		private ContentDatabase _content;

		private GameSession _session;

		private WorldChestVisual _visual;

		private bool _initialized;

		public string ContainerId => stableId ?? string.Empty;

		public string ContainerDisplayName
		{
			get
			{
				ForgePhysicalChestKind forgePhysicalChestKind = kind;
				if (1 == 0)
				{
				}
				string result = forgePhysicalChestKind switch
				{
					ForgePhysicalChestKind.Reward => RewardName(rewardSize) + " Schmiedekiste", 
					ForgePhysicalChestKind.Recovery => "Bergungscontainer", 
					_ => "Gewölbekiste", 
				};
				if (1 == 0)
				{
				}
				return result;
			}
		}

		public int SlotCapacity => _slots.Length;

		public string InteractionId => ContainerId;

		public InteractionType Type => InteractionType.Container;

		public string DisplayText => (kind == ForgePhysicalChestKind.Reward) ? (ContainerDisplayName.ToUpperInvariant() + " · " + $"{EidraForgeChestService.Price(rewardSize)} MARKEN") : (ContainerDisplayName.ToUpperInvariant() + " ÖFFNEN");

		/// <summary>Der Markenpreis dieser Kiste (nur Schmiedekisten zahlen).</summary>
		public int MarkPrice => (kind == ForgePhysicalChestKind.Reward) ? EidraForgeChestService.Price(rewardSize) : 0;

		/// <summary>
		/// Wahr, solange der nächste Zugriff Marken kostet — eine leere
		/// Schmiedekiste wartet auf den Kauf; gefüllt ist sie schon bezahlt.
		/// </summary>
		public bool RequiresMarkPayment => kind == ForgePhysicalChestKind.Reward && IsEmpty();

		public Sprite Icon => interactionIcon;

		public float InteractionRange => interactionRange;

		public InteractionMode Mode => InteractionMode.Instant;

		public float HoldDuration => 0f;

		public int Priority => 91;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public event Action<StorageContainerChange> Changed;

		public event Action<EidraForgeChestContainer> Activated;

		public void Configure(ForgePhysicalChestKind configuredKind, string configuredId, ForgeRewardChestSize configuredRewardSize = ForgeRewardChestSize.Small)
		{
			kind = configuredKind;
			stableId = configuredId ?? string.Empty;
			rewardSize = configuredRewardSize;
		}

		public void Initialize(GameSession session, ContentDatabase content, EidraForgeLootProfile profile)
		{
			_session = session ?? throw new ArgumentNullException("session");
			_content = content ?? throw new ArgumentNullException("content");
			_profile = profile ?? throw new ArgumentNullException("profile");
			_chests = session.EidraForgeChests;
			_dungeon = session.EidraForge;
			_visual = GetComponentInChildren<WorldChestVisual>(includeInactive: true);
			RefreshSlots();
			_initialized = true;
		}

		public bool TryGetSlot(int index, out ItemStack stack)
		{
			if (index < 0 || index >= _slots.Length)
			{
				stack = ItemStack.Empty;
				return false;
			}
			stack = _slots[index].Copy();
			return true;
		}

		public ItemStack[] ExportSlots()
		{
			ItemStack[] array = new ItemStack[_slots.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = _slots[i].Copy();
			}
			return array;
		}

		public bool TryImportSlots(IReadOnlyList<ItemStack> slots, out string error)
		{
			if (!_initialized || slots == null || slots.Count != _slots.Length)
			{
				error = "Gewölbekiste ist nicht bereit.";
				return false;
			}
			ItemStack[] array = new ItemStack[_slots.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = slots[i].Copy();
				if (!array[i].TryValidate(_content, out error))
				{
					return false;
				}
			}
			ForgePhysicalChestKind forgePhysicalChestKind = kind;
			if (1 == 0)
			{
			}
			bool flag = forgePhysicalChestKind switch
			{
				ForgePhysicalChestKind.Run => _chests.UpdateRunChestContents(stableId, array), 
				ForgePhysicalChestKind.Reward => _chests.UpdateRewardChestContents(rewardSize, array), 
				_ => TryUpdateRecovery(array), 
			};
			if (1 == 0)
			{
			}
			if (!flag)
			{
				error = "Der Kisteninhalt konnte nicht gespeichert werden.";
				return false;
			}
			CopyInto(array);
			UpdateVisual();
			this.Changed?.Invoke(new StorageContainerChange(ContainerId));
			_session.RequestSave(SaveRequestReason.StorageClosed);
			error = string.Empty;
			return true;
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			if (!_initialized || context.ActorTransform == null)
			{
				blockedReason = "KISTE NICHT VERFÜGBAR";
				return false;
			}
			if (kind == ForgePhysicalChestKind.Reward && IsEmpty() && _session.PlayerInventory.GetTotalAmount("smithing_mark") < EidraForgeChestService.Price(rewardSize))
			{
				blockedReason = "NICHT GENUG SCHMIEDEMARKEN";
				return false;
			}
			if (kind == ForgePhysicalChestKind.Run && string.Equals(stableId, "forge.elite.01", StringComparison.Ordinal) && !IsDefeated("forge.enemy.seal_guardian.01"))
			{
				blockedReason = "SIEGELWÄCHTER LEBT";
				return false;
			}
			if (kind == ForgePhysicalChestKind.Run && string.Equals(stableId, "forge.completion.01", StringComparison.Ordinal) && !IsDefeated("forge.enemy.core_guardian.01"))
			{
				blockedReason = "KERNWÄCHTER LEBT";
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
			if (kind == ForgePhysicalChestKind.Reward && IsEmpty())
			{
				if (!_chests.TryPurchaseRewardChest(rewardSize, _session.PlayerInventory, _profile, _content, out var _))
				{
					return;
				}
				RefreshSlots();
				_session.RequestSave(SaveRequestReason.StorageClosed);
			}
			this.Activated?.Invoke(this);
		}

		private void RefreshSlots()
		{
			ForgePhysicalChestKind forgePhysicalChestKind = kind;
			if (1 == 0)
			{
			}
			ItemStack[] array = forgePhysicalChestKind switch
			{
				ForgePhysicalChestKind.Run => _chests.GetRunChest(stableId)?.Slots, 
				ForgePhysicalChestKind.Reward => _chests.GetRewardChest(rewardSize).Slots, 
				_ => _dungeon.GetRecoverySlots(), 
			};
			if (1 == 0)
			{
			}
			ItemStack[] source = array ?? Array.Empty<ItemStack>();
			CopyInto(source);
			UpdateVisual();
		}

		private void UpdateVisual()
		{
			if (!(_visual == null) && _chests != null)
			{
				if (kind == ForgePhysicalChestKind.Run)
				{
					ForgeContainerState runChest = _chests.GetRunChest(stableId);
					_visual.Apply((runChest != null && runChest.Opened) ? (IsEmpty() ? WorldChestVisualState.Emptied : WorldChestVisualState.PartiallyEmptied) : WorldChestVisualState.Closed);
				}
				else if (kind == ForgePhysicalChestKind.Reward)
				{
					ForgeRewardChestState rewardChest = _chests.GetRewardChest(rewardSize);
					_visual.Apply((!IsEmpty()) ? WorldChestVisualState.Opened : ((rewardChest.PurchaseIndex > 0) ? WorldChestVisualState.Emptied : WorldChestVisualState.Closed));
				}
				else
				{
					_visual.Apply((!IsEmpty()) ? WorldChestVisualState.Opened : WorldChestVisualState.Emptied);
				}
			}
		}

		private bool TryUpdateRecovery(ItemStack[] replacement)
		{
			return _dungeon.TryUpdateRecoverySlots(replacement);
		}

		private void CopyInto(IReadOnlyList<ItemStack> source)
		{
			Array.Clear(_slots, 0, _slots.Length);
			int num = Mathf.Min(source?.Count ?? 0, _slots.Length);
			for (int i = 0; i < num; i++)
			{
				_slots[i] = source[i].Copy();
			}
		}

		private bool IsEmpty()
		{
			ItemStack[] slots = _slots;
			foreach (ItemStack itemStack in slots)
			{
				if (!itemStack.IsEmpty)
				{
					return false;
				}
			}
			return true;
		}

		private bool IsDefeated(string spawnId)
		{
			return _dungeon.IsEnemyDefeated(spawnId);
		}

		private static string RewardName(ForgeRewardChestSize size)
		{
			if (1 == 0)
			{
			}
			string result = size switch
			{
				ForgeRewardChestSize.Small => "Kleine", 
				ForgeRewardChestSize.Medium => "Mittlere", 
				_ => "Große", 
			};
			if (1 == 0)
			{
			}
			return result;
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

		// N04-001: Schmiedetruhen erscheinen erst auf der Karte, wenn sie einmal
		// im Bild waren — und bleiben danach sichtbar.
		private bool _seenOnMinimap;

		MinimapMarkerKind IMinimapMarker.MinimapKind => MinimapMarkerKind.Chest;

		Vector3 IMinimapMarker.MinimapPosition => base.transform.position;

		// Nutzerentscheid 17.08.2026: Geleerte Schmiedetruhen verschwinden von der
		// Karte, wie die Welttruhen.
		bool IMinimapMarker.ShowsOnMinimap => _seenOnMinimap && !IsEmpty();

		string IMinimapMarker.MinimapPaletteId => string.Empty;

		void IMinimapDiscoverable.MarkSeenOnMinimap()
		{
			_seenOnMinimap = true;
		}

		private void OnEnable()
		{
			MinimapRegistry.Register(this);
			// F32-007: Kisten werden von der Sichtlinie freigehalten. Ohne das
			// stand im Verlies nur das Preisschild im Raum, waehrend die Kiste
			// darunter im Fels steckte.
			Eidren.Presentation.OcclusionFocusRegistry.Register(this);
		}

		private void OnDisable()
		{
			MinimapRegistry.Unregister(this);
			Eidren.Presentation.OcclusionFocusRegistry.Unregister(this);
		}
	}
}
