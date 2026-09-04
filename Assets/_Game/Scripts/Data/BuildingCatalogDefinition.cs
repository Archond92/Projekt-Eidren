using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(fileName = "BuildingCatalog_V01", menuName = "Eidren/Buildings/Building Catalog")]
	public sealed class BuildingCatalogDefinition : ScriptableObject
	{
		[SerializeField]
		private BuildingCostDefinition[] buildings = Array.Empty<BuildingCostDefinition>();

		public IReadOnlyList<BuildingCostDefinition> Buildings => buildings ?? Array.Empty<BuildingCostDefinition>();

		public BuildingCostDefinition[] BuildingArray => (buildings != null) ? ((BuildingCostDefinition[])buildings.Clone()) : Array.Empty<BuildingCostDefinition>();
	}
}
