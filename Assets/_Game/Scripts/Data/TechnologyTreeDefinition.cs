using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(fileName = "TechnologyTree_V01", menuName = "Eidren/Progression/Technology Tree")]
	public sealed class TechnologyTreeDefinition : ScriptableObject
	{
		[SerializeField]
		private TechnologyNodeDefinition[] nodes = Array.Empty<TechnologyNodeDefinition>();

		public IReadOnlyList<TechnologyNodeDefinition> Nodes => nodes ?? Array.Empty<TechnologyNodeDefinition>();

		public bool TryGetNode(string nodeId, out TechnologyNodeDefinition node)
		{
			if (nodes != null)
			{
				TechnologyNodeDefinition[] array = nodes;
				foreach (TechnologyNodeDefinition technologyNodeDefinition in array)
				{
					if (technologyNodeDefinition != null && string.Equals(technologyNodeDefinition.Id, nodeId, StringComparison.Ordinal))
					{
						node = technologyNodeDefinition;
						return true;
					}
				}
			}
			node = null;
			return false;
		}

		public string[] GetValidationErrors()
		{
			return TechnologyTreeValidation.GetErrors(nodes);
		}
	}

	public static class BlueprintIds
	{
		public const string CopperSpear = "blueprint.copper_spear";
	}

	public static class TechnologyFeatureIds
	{
		public const string SecondEidraSlot = "feature.eidra_slot_2";

		public const string TierOneTools = "feature.t1_tools";

		public const string TierOneWeapons = "feature.t1_weapons";

		public const string TierTwoProcessing = "feature.t2_processing";

		public const string TierTwoTools = "feature.t2_tools";

		public static bool IsKnown(string id)
		{
			if (!string.Equals(id, "feature.eidra_slot_2", StringComparison.Ordinal) && !string.Equals(id, "feature.t1_tools", StringComparison.Ordinal) && !string.Equals(id, "feature.t1_weapons", StringComparison.Ordinal) && !string.Equals(id, "feature.t2_processing", StringComparison.Ordinal))
			{
				return string.Equals(id, "feature.t2_tools", StringComparison.Ordinal);
			}
			return true;
		}
	}

	[Serializable]
	public sealed class TechnologyNodeDefinition
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private string displayName;

		[SerializeField]
		[Min(1f)]
		private int stage = 1;

		[SerializeField]
		[Min(1f)]
		private int sortOrder = 1;

		[SerializeField]
		[Min(0f)]
		private int pointCost = 1;

		[SerializeField]
		private bool initiallyUnlocked;

		[SerializeField]
		private string[] prerequisiteNodeIds = Array.Empty<string>();

		[SerializeField]
		private string[] recipeIds = Array.Empty<string>();

		[SerializeField]
		private string[] buildingIds = Array.Empty<string>();

		[SerializeField]
		private string[] featureIds = Array.Empty<string>();

		[SerializeField]
		private string requiredProgressFlag;

		[SerializeField]
		private string requiredBlueprintId;

		public string Id => id ?? string.Empty;

		public string DisplayName => displayName ?? string.Empty;

		public int Stage => stage;

		public int SortOrder => sortOrder;

		public int PointCost => pointCost;

		public bool InitiallyUnlocked => initiallyUnlocked;

		public IReadOnlyList<string> PrerequisiteNodeIds => prerequisiteNodeIds ?? Array.Empty<string>();

		public IReadOnlyList<string> RecipeIds => recipeIds ?? Array.Empty<string>();

		public IReadOnlyList<string> BuildingIds => buildingIds ?? Array.Empty<string>();

		public IReadOnlyList<string> FeatureIds => featureIds ?? Array.Empty<string>();

		public string RequiredProgressFlag => requiredProgressFlag ?? string.Empty;

		public string RequiredBlueprintId => requiredBlueprintId ?? string.Empty;
	}

	public static class TechnologyNodeIds
	{
		public const string Workbench = "technology.01.workbench";

		public const string Axe = "technology.02.axe";

		public const string StorageChest = "technology.03.storage_chest";

		public const string Hammer = "technology.04.hammer";

		public const string Scythe = "technology.05.scythe";

		public const string Daggers = "technology.06.daggers";

		public const string Structures = "technology.07.structures";

		public const string FarmPlot = "technology.08.farm_plot";

		public const string Pickaxe = "technology.09.pickaxe";

		public const string Smelter = "technology.10.smelter";

		public const string CatchDevice = "technology.11.catch_device";

		public const string Sawmill = "technology.13.sawmill";

		public const string Ropewalk = "technology.14.ropewalk";

		public const string Stonecutter = "technology.15.stonecutter";

		public const string CookingPot = "technology.16.cooking_pot";

		public const string HealingPotion = "technology.17.healing_potion";

		public const string TierOneTools = "technology.18.t1_tools";

		public const string ClothHood = "technology.20.cloth_hood";

		public const string ClothCoat = "technology.21.cloth_coat";

		public const string ClothBracers = "technology.22.cloth_bracers";

		public const string ClothShoes = "technology.23.cloth_shoes";

		public const string ClothArmor = "technology.20.cloth_hood";

		public const string CopperHammer = "technology.24.copper_hammer";

		public const string CopperDaggers = "technology.25.copper_daggers";

		public const string CopperSpear = "technology.26.copper_spear";

		public const string CopperArmor = "technology.27.copper_armor";

		public const string TierTwoProcessing = "technology.28.t2_processing";

		public const string TierTwoTools = "technology.29.t2_tools";

		public const string IronHammer = "technology.30.iron_hammer";

		public const string IronDaggers = "technology.31.iron_daggers";

		public const string IronSpear = "technology.32.iron_spear";

		public const string IronArmor = "technology.33.iron_armor";
	}
}
