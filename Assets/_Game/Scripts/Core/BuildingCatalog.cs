using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public static class BuildingCatalog
	{
		public static void Rebuild(ContentDatabase content, TechnologyUnlockService technology, List<BuildingCatalogEntry> target)
		{
			if (content == null)
			{
				throw new ArgumentNullException("content");
			}
			if (technology == null)
			{
				throw new ArgumentNullException("technology");
			}
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}
			target.Clear();
			BuildingCostDefinition[] buildings = content.GetBuildings();
			foreach (BuildingCostDefinition buildingCostDefinition in buildings)
			{
				if (!(buildingCostDefinition == null))
				{
					bool flag = technology.IsBuildingUnlocked(buildingCostDefinition.Id);
					target.Add(new BuildingCatalogEntry(buildingCostDefinition, flag, flag ? string.Empty : RequirementFor(technology, buildingCostDefinition.Id)));
				}
			}
			target.Sort(Compare);
		}

		private static string RequirementFor(TechnologyUnlockService technology, string buildingId)
		{
			TechnologyNodeDefinition node;
			return technology.TryGetUnlockingNode(buildingId, out node) ? node.DisplayName : string.Empty;
		}

		private static int Compare(BuildingCatalogEntry left, BuildingCatalogEntry right)
		{
			int num = ((int)left.Category).CompareTo((int)right.Category);
			return (num != 0) ? num : string.CompareOrdinal(left.Building.DisplayName, right.Building.DisplayName);
		}
	}

	public readonly struct BuildingCatalogEntry
	{
		public BuildingCostDefinition Building { get; }

		public bool IsUnlocked { get; }

		public string Requirement { get; }

		public BuildingCategory Category => (Building != null) ? Building.Category : BuildingCategory.Structure;

		public BuildingCatalogEntry(BuildingCostDefinition building, bool unlocked, string requirement)
		{
			Building = building;
			IsUnlocked = unlocked;
			Requirement = requirement ?? string.Empty;
		}
	}
}
