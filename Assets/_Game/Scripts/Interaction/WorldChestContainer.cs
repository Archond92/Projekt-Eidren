using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Presentation;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Interaction
{
	[DisallowMultipleComponent]
	public sealed class WorldChestContainer : MonoBehaviour, IInspectableContainer, IItemContainer, IInteractable, IMinimapDiscoverable
	{
		[SerializeField]
		private WorldChestVisual visual;

		[SerializeField]
		private Sprite interactionIcon;

		[SerializeField]
		[Min(0.5f)]
		private float interactionRange = InteractionUtility.StandardSurfaceRange;

		[SerializeField]
		[Min(0.5f)]
		private float holdDuration = 1.6f;

		private ContentDatabase _database;

		private GameSession _session;

		private WorldChestState _state;

		public string ContainerId => _state?.InstanceId ?? string.Empty;

		public string ContainerDisplayName => FamilyDisplayName;

		public int SlotCapacity => 24;

		public string InteractionId => ContainerId;

		public InteractionType Type => InteractionType.Container;

		public string DisplayText => (_state == null) ? "KISTE ÖFFNEN" : (FamilyDisplayName.ToUpperInvariant() + " ÖFFNEN");

		public Sprite Icon => interactionIcon;

		public float InteractionRange => interactionRange;

		// Timed statt Hold: Ein einmaliges Auslösen genügt wie beim Ressourcenabbau;
		// die Sicherheitsabbrüche (Reichweite, Angriff, Schaden, Szenenwechsel) greifen weiterhin.
		public InteractionMode Mode => (Status == WorldChestStatus.Closed) ? InteractionMode.Timed : InteractionMode.Instant;

		public float HoldDuration => (Mode == InteractionMode.Timed) ? holdDuration : 0f;

		public int Priority => 88;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public WorldChestStatus Status => _state?.Status ?? WorldChestStatus.Closed;

		private string FamilyDisplayName
		{
			get
			{
				WorldChestFamily? worldChestFamily = _state?.Family;
				if (1 == 0)
				{
				}
				string result = worldChestFamily switch
				{
					WorldChestFamily.Guarded => "Bewachte Kiste", 
					WorldChestFamily.Hidden => "Versteckte Kiste", 
					_ => "Gewöhnliche Kiste", 
				};
				if (1 == 0)
				{
				}
				return result;
			}
		}

		public event Action<StorageContainerChange> Changed;

		public event Action<WorldChestContainer> Activated;

		public void ConfigureVisual(WorldChestVisual configuredVisual, Sprite configuredIcon, float range)
		{
			visual = configuredVisual;
			interactionIcon = configuredIcon;
			interactionRange = Mathf.Max(0.5f, range);
		}

		public void Initialize(ContentDatabase database, GameSession session, WorldChestState state)
		{
			_database = database ?? throw new ArgumentNullException("database");
			_session = session ?? throw new ArgumentNullException("session");
			_state = state ?? throw new ArgumentNullException("state");
			if (!ValidateSlots(_state.Slots, out var error))
			{
				throw new InvalidOperationException(error);
			}
			ApplyVisual();
		}

		public bool TryGetSlot(int index, out ItemStack stack)
		{
			if (_state == null || index < 0 || index >= SlotCapacity)
			{
				stack = ItemStack.Empty;
				return false;
			}
			stack = _state.Slots[index];
			return true;
		}

		public ItemStack[] ExportSlots()
		{
			return _state?.Slots ?? new ItemStack[SlotCapacity];
		}

		public bool TryImportSlots(IReadOnlyList<ItemStack> slots, out string error)
		{
			if (_state == null)
			{
				error = "World chest is not initialized.";
				return false;
			}
			if (!ValidateSlots(slots, out error))
			{
				return false;
			}
			_state.ReplaceSlots(slots);
			ApplyVisual();
			this.Changed?.Invoke(new StorageContainerChange(ContainerId));
			error = string.Empty;
			return true;
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			bool flag = _state != null && context.ActorTransform != null;
			blockedReason = (flag ? string.Empty : "KISTE NICHT VERFÜGBAR");
			return flag;
		}

		public void BeginInteraction(in InteractionContext context)
		{
			if (Status == WorldChestStatus.Closed)
			{
				visual?.SetOpeningProgress(0f);
			}
		}

		public void UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
			if (Status == WorldChestStatus.Closed)
			{
				visual?.SetOpeningProgress(normalizedProgress);
			}
		}

		public void CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
			if (Status == WorldChestStatus.Closed)
			{
				visual?.SetOpeningProgress(0f);
			}
		}

		public void CompleteInteraction(in InteractionContext context)
		{
			if (_state != null)
			{
				if (_state.MarkOpened())
				{
					visual?.SetOpeningProgress(1f);
					ApplyVisual();
					this.Changed?.Invoke(new StorageContainerChange(ContainerId));
					_session.RequestSave(SaveRequestReason.StorageClosed);
				}
				this.Activated?.Invoke(this);
			}
		}

		private bool ValidateSlots(IReadOnlyList<ItemStack> slots, out string error)
		{
			if (slots == null || slots.Count != SlotCapacity)
			{
				error = $"World chest '{ContainerId}' requires {SlotCapacity} slots.";
				return false;
			}
			for (int i = 0; i < slots.Count; i++)
			{
				if (!slots[i].TryValidate(_database, out var error2))
				{
					error = $"World chest '{ContainerId}' slot {i}: {error2}";
					return false;
				}
			}
			error = string.Empty;
			return true;
		}

		private void ApplyVisual()
		{
			if (!(visual == null) && _state != null)
			{
				visual.Apply((WorldChestVisualState)_state.Status);
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

		// N04-001: Eine Truhe erscheint erst auf der Karte, wenn sie einmal im
		// Bild war — und bleibt danach sichtbar.
		private bool _seenOnMinimap;

		MinimapMarkerKind IMinimapMarker.MinimapKind => MinimapMarkerKind.Chest;

		Vector3 IMinimapMarker.MinimapPosition => base.transform.position;

		// Nutzerentscheid 17.08.2026: Eine bereits geoeffnete Truhe verschwindet
		// von der Karte — sie ist erledigt.
		bool IMinimapMarker.ShowsOnMinimap => _seenOnMinimap && Status == WorldChestStatus.Closed;

		string IMinimapMarker.MinimapPaletteId => string.Empty;

		void IMinimapDiscoverable.MarkSeenOnMinimap()
		{
			_seenOnMinimap = true;
		}

		private void OnEnable()
		{
			MinimapRegistry.Register(this);
			// F32-007: Auch Weltkisten werden von der Sichtlinie freigehalten.
			Eidren.Presentation.OcclusionFocusRegistry.Register(this);
		}

		private void OnDisable()
		{
			MinimapRegistry.Unregister(this);
			Eidren.Presentation.OcclusionFocusRegistry.Unregister(this);
		}
	}
}
