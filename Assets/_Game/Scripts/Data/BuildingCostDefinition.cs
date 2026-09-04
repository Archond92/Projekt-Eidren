using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Buildings/Building Cost Definition")]
	public sealed class BuildingCostDefinition : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private string displayName;

		[SerializeField]
		private Sprite icon;

		[SerializeField]
		private int tier;

		[SerializeField]
		private CraftingIngredient[] cost = Array.Empty<CraftingIngredient>();

		[SerializeField]
		private bool occupiesBuildField = true;

		[SerializeField]
		private BuildingPlacementKind placementKind = BuildingPlacementKind.Object;

		[SerializeField]
		private BuildingGroundRule groundRule = BuildingGroundRule.GroundOrFloor;

		[SerializeField]
		private bool blocksNavigation = true;

		[SerializeField]
		private BuildingCategory category = BuildingCategory.Structure;

		[SerializeField]
		private BuildingFootprint[] levelFootprints = Array.Empty<BuildingFootprint>();

		[SerializeField]
		private GameObject[] levelPrefabs = Array.Empty<GameObject>();

		[SerializeField]
		private CraftingStationType craftingStation;

		[SerializeField]
		[Min(0.01f)]
		private float baseRate = 1f;

		[SerializeField]
		[Min(0.01f)]
		private float eidraFactor = 1f;

		// F31-010: 0 = ohne Mengengrenze; sonst hoechstens so viele Instanzen
		// dieses Gebaeudes gleichzeitig (Acker: 2).
		[SerializeField]
		[Min(0f)]
		private int maximumCount;

		public string Id => id ?? string.Empty;

		public string DisplayName => displayName ?? string.Empty;

		public Sprite Icon => icon;

		public int Tier => Mathf.Max(0, tier);

		public IReadOnlyList<CraftingIngredient> Cost => cost ?? Array.Empty<CraftingIngredient>();

		public int MaximumCount => Mathf.Max(0, maximumCount);

		public bool OccupiesBuildField => occupiesBuildField;

		public BuildingPlacementKind PlacementKind => placementKind;

		public BuildingGroundRule GroundRule => groundRule;

		public bool BlocksNavigation => blocksNavigation;

		public BuildingCategory Category => category;

		public IReadOnlyList<BuildingFootprint> LevelFootprints => levelFootprints ?? Array.Empty<BuildingFootprint>();

		public CraftingStationType CraftingStation => craftingStation;

		public float BaseRate => baseRate;

		public float EidraFactor => eidraFactor;

		public bool TryGetLevel(int level, out BuildingFootprint footprint, out GameObject prefab)
		{
			int num = level - 1;
			if (num < 0 || num >= LevelFootprints.Count || levelPrefabs == null || num >= levelPrefabs.Length || levelPrefabs[num] == null)
			{
				footprint = default(BuildingFootprint);
				prefab = null;
				return false;
			}
			footprint = LevelFootprints[num];
			prefab = levelPrefabs[num];
			return true;
		}

		public bool TryValidate(out string error)
		{
			if (string.IsNullOrWhiteSpace(Id))
			{
				error = "Building '" + base.name + "' has no stable ID.";
				return false;
			}
			if (string.IsNullOrWhiteSpace(DisplayName))
			{
				error = "Building '" + Id + "' has no display name.";
				return false;
			}
			if (Icon == null)
			{
				error = "Building '" + Id + "' has no menu icon.";
				return false;
			}
			if (tier < 0)
			{
				error = $"Building '{Id}' has a negative tier {tier}.";
				return false;
			}
			if (Cost.Count == 0)
			{
				error = "Building '" + Id + "' has no build cost.";
				return false;
			}
			if (float.IsNaN(baseRate) || float.IsInfinity(baseRate) || baseRate <= 0f)
			{
				error = "Building '" + Id + "' has invalid base rate.";
				return false;
			}
			if (float.IsNaN(eidraFactor) || float.IsInfinity(eidraFactor) || eidraFactor <= 0f)
			{
				error = "Building '" + Id + "' has invalid Eidra factor.";
				return false;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (CraftingIngredient item in Cost)
			{
				if (string.IsNullOrWhiteSpace(item.ItemId))
				{
					error = "Building '" + Id + "' has an empty cost item ID.";
					return false;
				}
				if (!hashSet.Add(item.ItemId))
				{
					error = "Building '" + Id + "' lists '" + item.ItemId + "' twice.";
					return false;
				}
			}
			if (LevelFootprints.Count == 0)
			{
				error = "Building '" + Id + "' has no footprint for level 1.";
				return false;
			}
			if (levelPrefabs == null || levelPrefabs.Length != LevelFootprints.Count)
			{
				error = "Building '" + Id + "' requires one prefab per level.";
				return false;
			}
			for (int i = 0; i < levelPrefabs.Length; i++)
			{
				if (levelPrefabs[i] == null)
				{
					error = "Building '" + Id + "' has no prefab for level " + $"{i + 1}.";
					return false;
				}
			}
			return ValidatePlacement(out error);
		}

		private bool ValidatePlacement(out string error)
		{
			bool flag = placementKind == BuildingPlacementKind.Object;
			if (flag != occupiesBuildField)
			{
				error = "Building '" + Id + "' declares placement kind " + $"{placementKind} but occupiesBuildField=" + occupiesBuildField.ToString().ToLowerInvariant() + ". Only object-like plans occupy a build field.";
				return false;
			}
			switch (placementKind)
			{
			case BuildingPlacementKind.Floor:
				if (groundRule == BuildingGroundRule.GroundOnly)
				{
					break;
				}
				error = "Building '" + Id + "' is a floor and must use GroundOnly: a floor lies on natural ground, never on another floor.";
				return false;
			case BuildingPlacementKind.Edge:
				if (groundRule == BuildingGroundRule.GroundOrFloor)
				{
					break;
				}
				error = "Building '" + Id + "' occupies an edge. Edges are not cells, so no cell-based ground rule applies; use GroundOrFloor.";
				return false;
			}
			return ValidateNavigation(out error);
		}

		private bool ValidateCategory(out string error)
		{
			bool flag = placementKind == BuildingPlacementKind.Object;
			bool flag2 = category == BuildingCategory.Structure;
			if (flag == flag2)
			{
				error = (flag ? ("Building '" + Id + "' is object-like and needs a catalog category other than Structure (section 13).") : ($"Building '{Id}' occupies {placementKind} and belongs " + "in the Structure category."));
				return false;
			}
			error = string.Empty;
			return true;
		}

		private bool ValidateNavigation(out string error)
		{
			switch (placementKind)
			{
			case BuildingPlacementKind.Floor:
				if (!blocksNavigation)
				{
					break;
				}
				error = "Building '" + Id + "' is a floor and must not block navigation: a floor is walked on, not around.";
				return false;
			case BuildingPlacementKind.Decoration:
				if (!blocksNavigation)
				{
					break;
				}
				error = "Building '" + Id + "' is decoration and must not block navigation (section 2).";
				return false;
			case BuildingPlacementKind.Object:
				if (blocksNavigation)
				{
					break;
				}
				error = "Building '" + Id + "' is object-like and must block navigation.";
				return false;
			}
			return ValidateCategory(out error);
		}
	}

	public enum BuildingCategory
	{
		Structure = 0,
		Workshops = 1,
		Supply = 2,
		Farming = 3
	}

	[Serializable]
	public struct BuildingFootprint
	{
		[SerializeField]
		[Min(1f)]
		private int width;

		[SerializeField]
		[Min(1f)]
		private int depth;

		public int Width => Mathf.Max(1, width);

		public int Depth => Mathf.Max(1, depth);
	}

	public enum BuildingGroundRule
	{
		GroundOrFloor = 0,
		RequiresFloor = 1,
		GroundOnly = 2,
		OutdoorOnly = 3
	}

	public static class BuildingIds
	{
		public const string Workbench = "building.workbench";

		public const string StorageChest = "building.storage_chest";

		public const string FarmPlot = "building.farm_plot";

		public const string Wall = "building.wall";

		public const string Floor = "building.floor";

		public const string Door = "building.door";

		public const string Smelter = "building.smelter";

		public const string Sawmill = "building.sawmill";

		public const string Ropewalk = "building.ropewalk";

		public const string Stonecutter = "building.stonecutter";

		public const string CookingPot = "building.cooking_pot";
	}

	public enum BuildingPlacementKind
	{
		Object = 0,
		Floor = 1,
		Edge = 2,
		Decoration = 3
	}
}
