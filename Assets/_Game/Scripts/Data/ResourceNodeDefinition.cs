using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Resources/Resource Node Definition")]
	public sealed class ResourceNodeDefinition : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private string displayName;

		[SerializeField]
		private string interactionText = "SAMMELN";

		[SerializeField]
		private ItemDefinition outputItem;

		[SerializeField]
		private HarvestYieldTable harvestYields;

		[SerializeField]
		[Min(0.01f)]
		private float interactionDuration = 1f;

		[SerializeField]
		[Min(0.1f)]
		private float interactionRange = 1.5f;

		[SerializeField]
		private ResourceRespawnMode respawnMode = ResourceRespawnMode.OnZoneEntry;

		[SerializeField]
		private GameObject nodePrefab;

		[SerializeField]
		private GameObject activeVisualPrefab;

		[SerializeField]
		private GameObject exhaustedVisualPrefab;

		[SerializeField]
		[Tooltip("W-005: Abgebaute Knoten behalten die aktive Optik; nur die Interaktion endet (z. B. Kupferader).")]
		private bool keepsAppearanceWhenExhausted;

		[SerializeField]
		private Sprite collectionIcon;

		[SerializeField]
		private bool requiresTool;

		[SerializeField]
		private string requiredToolItemId;

		[SerializeField]
		private bool blocksNavigation;

		public string Id => id ?? string.Empty;

		public string DisplayName => displayName ?? string.Empty;

		public string InteractionText => string.IsNullOrWhiteSpace(interactionText) ? "SAMMELN" : interactionText;

		public ItemDefinition OutputItem => outputItem;

		public HarvestYieldTable HarvestYields => harvestYields;

		public float InteractionDuration => Mathf.Max(0.01f, interactionDuration);

		public float InteractionRange => Mathf.Max(0.1f, interactionRange);

		public ResourceRespawnMode RespawnMode => respawnMode;

		public GameObject NodePrefab => nodePrefab;

		public GameObject ActiveVisualPrefab => activeVisualPrefab;

		public GameObject ExhaustedVisualPrefab => exhaustedVisualPrefab;

		public bool KeepsAppearanceWhenExhausted => keepsAppearanceWhenExhausted;

		public Sprite CollectionIcon => (collectionIcon != null) ? collectionIcon : ((outputItem != null) ? outputItem.Icon : null);

		public bool RequiresTool => requiresTool;

		public string RequiredToolItemId => requiredToolItemId ?? string.Empty;

		public bool BlocksNavigation => blocksNavigation;

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (string.IsNullOrWhiteSpace(Id))
			{
				list.Add("Stable resource node ID is empty.");
			}
			if (string.IsNullOrWhiteSpace(DisplayName))
			{
				list.Add("Resource '" + base.name + "' has no display name.");
			}
			if (outputItem == null)
			{
				list.Add("Resource '" + base.name + "' has no output item.");
			}
			if (harvestYields == null)
			{
				list.Add("Resource '" + base.name + "' has no harvest yield table.");
			}
			if (interactionDuration <= 0f)
			{
				list.Add("Resource '" + base.name + "' requires a positive duration.");
			}
			if (activeVisualPrefab == null || exhaustedVisualPrefab == null)
			{
				list.Add("Resource '" + base.name + "' requires active and exhausted visuals.");
			}
			if (nodePrefab == null)
			{
				list.Add("Resource '" + base.name + "' has no node prefab.");
			}
			if (requiresTool && string.IsNullOrWhiteSpace(requiredToolItemId))
			{
				list.Add("Resource '" + base.name + "' requires a tool but names no required tool item ID.");
			}
			if (!Enum.IsDefined(typeof(ResourceRespawnMode), respawnMode))
			{
				list.Add("Resource '" + base.name + "' has an invalid respawn mode.");
			}
			return list.ToArray();
		}
	}

	public static class ResourceNodeIds
	{
		public const string Tree = "resource.tree";

		public const string StoneDeposit = "resource.stone_deposit";

		public const string FiberPlant = "resource.fiber_plant";

		public const string CopperVein = "resource.copper_vein";

		public const string HardwoodTree = "resource.hardwood_tree";

		public const string SwampHemp = "resource.swamp_hemp";

		public const string GraniteDeposit = "resource.granite_deposit";

		public const string IronVein = "resource.iron_vein";

		public const string BerryBush = "resource.berry_bush";
	}
}
