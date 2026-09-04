using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Presentation;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Interaction
{
	public sealed partial class ResourceNode : MonoBehaviour, IInteractable
	{
		[SerializeField]
		private ResourceNodeDefinition definition;

		[SerializeField]
		private string instanceId = "resource.instance.unconfigured";

		[SerializeField]
		private GameObject activeVisual;

		[SerializeField]
		private GameObject exhaustedVisual;

		[SerializeField]
		private Collider interactionTrigger;

		[SerializeField]
		private Collider blockingCollider;

		[SerializeField]
		private int priority = 10;

		[SerializeField]
		private ResourceNodeByproduct byproduct = new ResourceNodeByproduct();

		private HarvestYieldResolver _yields;

		private bool _hasRespawnModeOverride;

		private ResourceRespawnMode _respawnModeOverride;

		private Sprite _actionIcon;

		private bool _actionIconResolved;

		private string _toolStrainText = string.Empty;

		public ResourceNodeDefinition Definition => definition;

		public string InstanceId => instanceId ?? string.Empty;

		public string InteractionId => InstanceId;

		public InteractionType Type => InteractionType.Resource;

		public string DisplayText => (!(definition != null)) ? "SAMMELN" : (string.IsNullOrWhiteSpace(_toolStrainText) ? definition.InteractionText : (definition.InteractionText + " · " + _toolStrainText));

		public float InteractionRange => (definition != null) ? definition.InteractionRange : InteractionUtility.StandardSurfaceRange;

		public InteractionMode Mode => InteractionMode.Timed;

		public float HoldDuration => (definition != null) ? definition.InteractionDuration : 0.01f;

		public int Priority => priority;

		public Vector3 InteractionPosition => base.transform.position;

		public GameObject InteractionObject => base.gameObject;

		public bool IsExhausted { get; private set; }

		public bool IsMined => IsExhausted;

		public float Progress { get; private set; }

		public int CollectedAmount { get; private set; }

		public Collider BlockingCollider => blockingCollider;

		public Collider InteractionTrigger => interactionTrigger;

		public ResourceRespawnMode RespawnMode => _hasRespawnModeOverride ? _respawnModeOverride : ((definition != null) ? definition.RespawnMode : ResourceRespawnMode.None);

		public ResourceNodeByproduct Byproduct => byproduct;

		public Sprite Icon
		{
			get
			{
				if (!_actionIconResolved)
				{
					_actionIcon = BuildActionIcon();
					_actionIconResolved = true;
				}
				return _actionIcon;
			}
		}

		public event Action<float> ProgressChanged;

		public event Action Mined;

		public event Action<ResourceCollectionResult> Collected;

		public event Action<string> CollectionBlocked;

		public void ConfigureByproduct(ItemDefinition item, int amount)
		{
			byproduct.Configure(item, amount);
		}

		public void MarkAlreadyHarvested()
		{
			IsExhausted = true;
			Progress = 1f;
			CollectedAmount = 0;
			if (interactionTrigger != null)
			{
				interactionTrigger.enabled = false;
			}
			ApplyVisualState(exhausted: true);
			this.ProgressChanged?.Invoke(Progress);
		}

		public void ConfigureInstanceId(string stableInstanceId, ResourceRespawnMode? respawnModeOverride = null)
		{
			if (string.IsNullOrWhiteSpace(stableInstanceId))
			{
				throw new ArgumentException("A stable resource-node instance ID is required.", "stableInstanceId");
			}
			instanceId = stableInstanceId;
			_hasRespawnModeOverride = respawnModeOverride.HasValue;
			if (respawnModeOverride.HasValue)
			{
				_respawnModeOverride = respawnModeOverride.Value;
			}
		}

		public void Configure(ResourceNodeDefinition configuredDefinition, string stableInstanceId, GameObject configuredActiveVisual, GameObject configuredExhaustedVisual, Collider configuredInteractionTrigger, Collider configuredBlockingCollider, int configuredPriority = 10)
		{
			definition = configuredDefinition ?? throw new ArgumentNullException("configuredDefinition");
			instanceId = (string.IsNullOrWhiteSpace(stableInstanceId) ? (definition.Id + ".unconfigured") : stableInstanceId);
			activeVisual = configuredActiveVisual;
			exhaustedVisual = configuredExhaustedVisual;
			interactionTrigger = configuredInteractionTrigger;
			blockingCollider = configuredBlockingCollider;
			priority = configuredPriority;
			BindYields();
			ConfigureTrigger();
			ResetForZoneEntry();
		}

		public int ResolveYield(in InteractionContext context)
		{
			return (_yields != null) ? _yields.Resolve(definition, context.Inventory).Amount : 0;
		}

		public ItemDefinition ResolveTool(PlayerInventory inventory)
		{
			return (definition != null && _yields != null) ? _yields.Resolve(definition, inventory).Tool : null;
		}

		public void ResetForZoneEntry()
		{
			if (!(definition == null) && (!IsExhausted || RespawnMode != ResourceRespawnMode.None))
			{
				IsExhausted = false;
				Progress = 0f;
				CollectedAmount = 0;
				ConfigureTrigger();
				if (interactionTrigger != null)
				{
					interactionTrigger.enabled = true;
				}
				ApplyVisualState(exhausted: false);
				this.ProgressChanged?.Invoke(0f);
			}
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			if (definition == null || definition.OutputItem == null || _yields == null)
			{
				blockedReason = "RESSOURCE NICHT KONFIGURIERT";
				return false;
			}
			if (IsExhausted)
			{
				blockedReason = "ERSCHÖPFT";
				return false;
			}
			if (context.Inventory == null)
			{
				blockedReason = "INVENTAR NICHT VERFÜGBAR";
				return false;
			}
			HarvestOutcome harvestOutcome = _yields.Resolve(definition, context.Inventory);
			_toolStrainText = StrainText(harvestOutcome.Strain);
			if (!harvestOutcome.IsAllowed)
			{
				blockedReason = "WERKZEUG UNZUREICHEND";
				return false;
			}
			if (!byproduct.FitsWith(context.Inventory, definition.OutputItem.Id, harvestOutcome.Amount))
			{
				blockedReason = "INVENTAR VOLL";
				return false;
			}
			blockedReason = string.Empty;
			return base.isActiveAndEnabled;
		}

		public void BeginInteraction(in InteractionContext context)
		{
			Progress = 0f;
			this.ProgressChanged?.Invoke(Progress);
		}

		public void UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
			if (!IsExhausted)
			{
				Progress = Mathf.Clamp01(normalizedProgress);
				this.ProgressChanged?.Invoke(Progress);
			}
		}

		public void CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
			if (!IsExhausted)
			{
				Progress = 0f;
				this.ProgressChanged?.Invoke(Progress);
			}
		}

		public void CompleteInteraction(in InteractionContext context)
		{
			if (!CanInteract(in context, out var blockedReason))
			{
				Progress = 0f;
				this.ProgressChanged?.Invoke(Progress);
				this.CollectionBlocked?.Invoke(blockedReason);
				return;
			}
			ItemDefinition outputItem = definition.OutputItem;
			HarvestOutcome harvestOutcome = _yields.Resolve(definition, context.Inventory);
			int amount = harvestOutcome.Amount;
			if (!context.Inventory.TryAddAll(outputItem.Id, amount))
			{
				Progress = 0f;
				this.ProgressChanged?.Invoke(Progress);
				this.CollectionBlocked?.Invoke("INVENTAR VOLL");
				return;
			}
			byproduct.AwardTo(context.Inventory);
			if (harvestOutcome.Tool != null)
			{
				int wear = DurabilityRules.ToolWear(harvestOutcome.Tool.Tier, outputItem.Tier);
				if (context.Inventory.TryWearDurableItem(harvestOutcome.Tool.Id, wear, out var broken) && broken)
				{
					CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 2.1f, harvestOutcome.Tool.DisplayName + " zerbrochen", new Color(1f, 0.35f, 0.18f), 1.4f);
				}
			}
			CollectedAmount = amount;
			Progress = 1f;
			IsExhausted = true;
			this.ProgressChanged?.Invoke(Progress);
			if (interactionTrigger != null)
			{
				interactionTrigger.enabled = false;
			}
			ApplyVisualState(exhausted: true);
			ResourceCollectionResult obj = new ResourceCollectionResult(InstanceId, outputItem, CollectedAmount, (definition != null) ? definition.Id : null);
			this.Collected?.Invoke(obj);
			this.Mined?.Invoke();
			CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 2.4f, $"+{CollectedAmount} {outputItem.DisplayName}", new Color(0.42f, 0.92f, 0.55f), 1.4f);
			CombatFeedback.SpawnStyleCue(base.transform.position + Vector3.up * 0.65f, StyleCue.ResourceHarvest);
		}

		private void Awake()
		{
			EnsureVisuals();
			BindYields();
			ResetForZoneEntry();
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
				sphereCollider2.radius = Mathf.Max(0.5f, InteractionRange * 0.45f);
			}
		}

		private void EnsureVisuals()
		{
			if (!(definition == null))
			{
				if (activeVisual == null && definition.ActiveVisualPrefab != null)
				{
					activeVisual = UnityEngine.Object.Instantiate(definition.ActiveVisualPrefab, base.transform);
					activeVisual.name = "ActiveVisual";
				}
				if (exhaustedVisual == null && definition.ExhaustedVisualPrefab != null)
				{
					exhaustedVisual = UnityEngine.Object.Instantiate(definition.ExhaustedVisualPrefab, base.transform);
					exhaustedVisual.name = "ExhaustedVisual";
				}
			}
		}

		private void ApplyVisualState(bool exhausted)
		{
			bool flag = exhausted && RespawnMode == ResourceRespawnMode.None;
			// W-005: Knoten mit Erhaltens-Flag (Kupferader) behalten die aktive
			// Optik nach dem Abbau; nur die Interaktion endet.
			bool keep = definition != null && definition.KeepsAppearanceWhenExhausted;
			if (activeVisual != null)
			{
				activeVisual.SetActive(!exhausted || keep);
			}
			if (exhaustedVisual != null)
			{
				exhaustedVisual.SetActive(exhausted && !flag && !keep);
			}
			if (blockingCollider != null)
			{
				blockingCollider.enabled = !flag;
			}
		}

		private static string StrainText(ToolStrain strain)
		{
			if (1 == 0)
			{
			}
			string result = strain switch
			{
				ToolStrain.Insufficient => "Werkzeug unzureichend", 
				ToolStrain.High => "Belastung hoch", 
				ToolStrain.Normal => "Belastung normal", 
				ToolStrain.Low => "Belastung gering", 
				_ => string.Empty, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private void BindYields()
		{
			_yields = ((definition != null && definition.HarvestYields != null) ? new HarvestYieldResolver(definition.HarvestYields) : null);
			_actionIconResolved = false;
		}

		private Sprite BuildActionIcon()
		{
			if (definition == null || definition.HarvestYields == null || definition.OutputItem == null)
			{
				return null;
			}
			if (definition.RequiresTool && definition.HarvestYields.TryGetToolById(definition.RequiredToolItemId, out var entry) && entry.Tool != null)
			{
				return entry.Tool.InteractionIcon;
			}
			HarvestToolYield entry2;
			return (definition.HarvestYields.TryGetToolForFamily(definition.OutputItem.MaterialFamily, out entry2) && entry2.Tool != null) ? entry2.Tool.InteractionIcon : null;
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

	public readonly struct ResourceCollectionResult
	{
		public string NodeInstanceId { get; }

		// F31-006: Die Katalog-Id des Knotens — die Questkette zählt nach
		// Knotentyp, die Instanz-Id trägt ihn nur eingebettet.
		public string NodeDefinitionId { get; }

		public ItemDefinition Item { get; }

		public int Amount { get; }

		public ResourceCollectionResult(string nodeInstanceId, ItemDefinition item, int amount, string nodeDefinitionId = null)
		{
			NodeInstanceId = nodeInstanceId ?? string.Empty;
			NodeDefinitionId = nodeDefinitionId ?? string.Empty;
			Item = item;
			Amount = amount;
		}
	}
}
