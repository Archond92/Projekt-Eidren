using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class ZoneState
	{
		private readonly HashSet<string> _harvestedNodeIds = new HashSet<string>(StringComparer.Ordinal);

		private readonly Dictionary<string, WorldChestState> _worldChests = new Dictionary<string, WorldChestState>(StringComparer.Ordinal);

		public string ZoneId { get; }

		public int Seed { get; private set; }

		public int GeneratorVersion { get; private set; }

		public int HarvestedCount => _harvestedNodeIds.Count;

		public int WorldChestCount => _worldChests.Count;

		public ZoneState(string zoneId, int seed, int generatorVersion, IEnumerable<string> harvestedNodeIds = null, IEnumerable<WorldChestState> worldChests = null)
		{
			if (string.IsNullOrWhiteSpace(zoneId))
			{
				throw new ArgumentException("A stable zone ID is required.", "zoneId");
			}
			ZoneId = zoneId;
			Seed = seed;
			GeneratorVersion = generatorVersion;
			if (harvestedNodeIds != null)
			{
				foreach (string harvestedNodeId in harvestedNodeIds)
				{
					if (!string.IsNullOrWhiteSpace(harvestedNodeId))
					{
						_harvestedNodeIds.Add(harvestedNodeId);
					}
				}
			}
			if (worldChests == null)
			{
				return;
			}
			foreach (WorldChestState worldChest in worldChests)
			{
				if (worldChest != null)
				{
					_worldChests[worldChest.InstanceId] = worldChest;
				}
			}
		}

		public bool IsHarvested(string nodeInstanceId)
		{
			if (!string.IsNullOrWhiteSpace(nodeInstanceId))
			{
				return _harvestedNodeIds.Contains(nodeInstanceId);
			}
			return false;
		}

		public bool MarkHarvested(string nodeInstanceId)
		{
			if (!string.IsNullOrWhiteSpace(nodeInstanceId))
			{
				return _harvestedNodeIds.Add(nodeInstanceId);
			}
			return false;
		}

		public bool TryGetWorldChest(string instanceId, out WorldChestState state)
		{
			if (string.IsNullOrWhiteSpace(instanceId))
			{
				state = null;
				return false;
			}
			return _worldChests.TryGetValue(instanceId, out state);
		}

		public void SetWorldChest(WorldChestState state)
		{
			if (state == null)
			{
				throw new ArgumentNullException("state");
			}
			_worldChests[state.InstanceId] = state;
		}

		public WorldChestState[] GetWorldChests()
		{
			WorldChestState[] array = new WorldChestState[_worldChests.Count];
			_worldChests.Values.CopyTo(array, 0);
			Array.Sort(array, (WorldChestState first, WorldChestState second) => string.CompareOrdinal(first.InstanceId, second.InstanceId));
			return array;
		}

		public void Regenerate(int seed, int generatorVersion)
		{
			Seed = seed;
			GeneratorVersion = generatorVersion;
			_harvestedNodeIds.Clear();
			_worldChests.Clear();
		}

		public string[] GetHarvestedNodeIds()
		{
			string[] array = new string[_harvestedNodeIds.Count];
			_harvestedNodeIds.CopyTo(array);
			Array.Sort(array, StringComparer.Ordinal);
			return array;
		}
	}
}
