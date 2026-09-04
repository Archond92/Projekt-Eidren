using Eidren.Core.BuildGrid;
using Eidren.Core.Services;
using Eidren.Data;
using System;
using UnityEngine.AI;
using UnityEngine;

namespace Eidren.Interaction
{
	[DisallowMultipleComponent]
	public sealed class BuildingInstanceView : MonoBehaviour
	{
		[SerializeField]
		private NavMeshObstacle navigationObstacle;

		public string InstanceId { get; private set; } = string.Empty;

		public string BuildingId { get; private set; } = string.Empty;

		public int Level { get; private set; } = 1;

		public WorkbenchController Workbench { get; private set; }

		public StorageContainer Storage { get; private set; }

		public FarmPlotController FarmPlot { get; private set; }

		public void ConfigureReferences(NavMeshObstacle obstacle)
		{
			navigationObstacle = obstacle;
		}

		public static Vector3 EdgeOffset(BuildingCostDefinition building, int quarterTurns)
		{
			if (building == null || building.PlacementKind != BuildingPlacementKind.Edge)
			{
				return Vector3.zero;
			}
			return (BuildGridOrigin.OrientationFor(quarterTurns) == GridEdgeOrientation.EastWest) ? new Vector3(0f, 0f, 0.5f) : new Vector3(0.5f, 0f, 0f);
		}

		public void Initialize(BuildingInstanceState state, ContentDatabase content, GameSession session)
		{
			if (state == null)
			{
				throw new ArgumentNullException("state");
			}
			InstanceId = state.InstanceId;
			BuildingId = state.BuildingId;
			Level = state.Level;
			if (!content.TryGetBuilding(state.BuildingId, out var value))
			{
				throw new InvalidOperationException("Building '" + state.BuildingId + "' is not in content.");
			}
			base.transform.SetPositionAndRotation(state.Position + EdgeOffset(value, state.QuarterTurns), Quaternion.Euler(0f, state.RotationDegrees, 0f));
			Workbench = GetComponent<WorkbenchController>();
			if (Workbench != null)
			{
				// F32-001: Reichweite kommt aus der Komponente selbst — vorher
				// stand hier eine zweite Zahl neben dem Vorgabewert, und beide
				// mussten von Hand gleich gehalten werden.
				Workbench.Configure(InstanceId + ".crafting", value.DisplayName + " benutzen", null, Workbench.InteractionRange, value.CraftingStation);
			}
			else if (value.CraftingStation != CraftingStationType.None)
			{
				throw new InvalidOperationException("Building prefab '" + base.name + "' needs a crafting station.");
			}
			Storage = GetComponent<StorageContainer>();
			if (Storage != null)
			{
				Storage.Configure(InstanceId, "Lagerkiste öffnen", null, Storage.InteractionRange);
				Storage.Initialize(content, session);
			}
			FarmPlot = GetComponent<FarmPlotController>();
			if (FarmPlot != null)
			{
				FarmPlot.Initialize(InstanceId, session.FarmProduction, session);
			}
			else if (string.Equals(BuildingId, "building.farm_plot", StringComparison.Ordinal))
			{
				throw new InvalidOperationException("Building prefab '" + base.name + "' needs a farm controller.");
			}
			if (navigationObstacle == null)
			{
				throw new InvalidOperationException("Building prefab '" + base.name + "' needs a NavMeshObstacle.");
			}
			navigationObstacle.carving = value.BlocksNavigation;
			navigationObstacle.enabled = value.BlocksNavigation;
		}
	}
}
