using System;

namespace Eidren.Core.Services
{
	public sealed class FarmProductionService
	{
		public const int SlotCapacity = 4;

		public const int WheatPerSeed = 3;

		private readonly BuildingRegistry _buildings;

		private readonly PlayerInventory _inventory;

		public FarmProductionService(BuildingRegistry buildings, PlayerInventory inventory)
		{
			_buildings = buildings ?? throw new ArgumentNullException("buildings");
			_inventory = inventory ?? throw new ArgumentNullException("inventory");
		}

		public int AdvanceForHomecoming()
		{
			int num = 0;
			BuildingInstanceState[] all = _buildings.GetAll();
			checked
			{
				foreach (BuildingInstanceState buildingInstanceState in all)
				{
					if (IsFarm(buildingInstanceState) && buildingInstanceState.PlantedSlots != 0)
					{
						int num2 = buildingInstanceState.PlantedSlots * 3;
						if (!_buildings.TryUpdateProduction(buildingInstanceState.InstanceId, 0, buildingInstanceState.ReadyOutput + num2))
						{
							throw new InvalidOperationException("Farm '" + buildingInstanceState.InstanceId + "' vanished during homecoming.");
						}
						num += num2;
					}
				}
				return num;
			}
		}

		public FarmActionResult TryPlantSeed(string instanceId)
		{
			if (!TryGetFarm(instanceId, out var state))
			{
				return FarmActionResult.UnknownFarm;
			}
			if (state.PlantedSlots >= 4)
			{
				return FarmActionResult.FarmFull;
			}
			if (!_inventory.Remove("wheat_seed", 1))
			{
				return FarmActionResult.NoSeed;
			}
			if (!_buildings.TryUpdateProduction(state.InstanceId, state.PlantedSlots + 1, state.ReadyOutput))
			{
				_inventory.TryAddAll("wheat_seed", 1);
				return FarmActionResult.UnknownFarm;
			}
			return FarmActionResult.Success;
		}

		public FarmActionResult TryHarvest(string instanceId)
		{
			if (!TryGetFarm(instanceId, out var state))
			{
				return FarmActionResult.UnknownFarm;
			}
			if (state.ReadyOutput <= 0)
			{
				return FarmActionResult.NothingReady;
			}
			if (!_inventory.TryAddAll("wheat", state.ReadyOutput))
			{
				return FarmActionResult.InventoryFull;
			}
			if (!_buildings.TryUpdateProduction(state.InstanceId, state.PlantedSlots, 0))
			{
				throw new InvalidOperationException("Farm '" + state.InstanceId + "' vanished during harvest.");
			}
			return FarmActionResult.Success;
		}

		public bool TryGetFarm(string instanceId, out BuildingInstanceState state)
		{
			if (_buildings.TryGet(instanceId, out state) && IsFarm(state))
			{
				return true;
			}
			state = null;
			return false;
		}

		private static bool IsFarm(BuildingInstanceState state)
		{
			return state != null && string.Equals(state.BuildingId, "building.farm_plot", StringComparison.Ordinal);
		}
	}
}
