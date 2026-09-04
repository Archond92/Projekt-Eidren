using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class BuildingRegistry
	{
		private readonly Dictionary<string, BuildingInstanceState> _states = new Dictionary<string, BuildingInstanceState>(StringComparer.Ordinal);

		public event Action Changed;

		public BuildingInstanceState[] GetAll()
		{
			BuildingInstanceState[] array = new BuildingInstanceState[_states.Count];
			int num = 0;
			foreach (BuildingInstanceState value in _states.Values)
			{
				array[num++] = value.Copy();
			}
			Array.Sort(array, (BuildingInstanceState left, BuildingInstanceState right) => string.CompareOrdinal(left.InstanceId, right.InstanceId));
			return array;
		}

		public bool TryGet(string instanceId, out BuildingInstanceState state)
		{
			if (!string.IsNullOrWhiteSpace(instanceId) && _states.TryGetValue(instanceId, out var value))
			{
				state = value.Copy();
				return true;
			}
			state = null;
			return false;
		}

		public bool TryAdd(BuildingInstanceState state)
		{
			if (state == null || string.IsNullOrWhiteSpace(state.InstanceId) || string.IsNullOrWhiteSpace(state.BuildingId) || state.Level < 1 || !_states.TryAdd(state.InstanceId, state.Copy()))
			{
				return false;
			}
			this.Changed?.Invoke();
			return true;
		}

		public bool Remove(string instanceId)
		{
			bool flag = !string.IsNullOrWhiteSpace(instanceId) && _states.Remove(instanceId);
			if (flag)
			{
				this.Changed?.Invoke();
			}
			return flag;
		}

		public bool TryUpdateProduction(string instanceId, int plantedSlots, int readyOutput)
		{
			if (string.IsNullOrWhiteSpace(instanceId) || plantedSlots < 0 || readyOutput < 0 || !_states.TryGetValue(instanceId, out var value))
			{
				return false;
			}
			if (value.PlantedSlots == plantedSlots && value.ReadyOutput == readyOutput)
			{
				return true;
			}
			_states[instanceId] = value.WithProduction(plantedSlots, readyOutput);
			this.Changed?.Invoke();
			return true;
		}

		public void Restore(IReadOnlyList<BuildingInstanceState> states)
		{
			Dictionary<string, BuildingInstanceState> dictionary = new Dictionary<string, BuildingInstanceState>(StringComparer.Ordinal);
			if (states != null)
			{
				foreach (BuildingInstanceState state in states)
				{
					if (state == null || string.IsNullOrWhiteSpace(state.InstanceId) || string.IsNullOrWhiteSpace(state.BuildingId) || state.Level < 1 || !dictionary.TryAdd(state.InstanceId, state.Copy()))
					{
						throw new InvalidOperationException("Building state contains invalid or duplicate IDs.");
					}
				}
			}
			_states.Clear();
			foreach (KeyValuePair<string, BuildingInstanceState> item in dictionary)
			{
				_states.Add(item.Key, item.Value);
			}
			this.Changed?.Invoke();
		}

		public void Reset()
		{
			_states.Clear();
			this.Changed?.Invoke();
		}
	}
}
