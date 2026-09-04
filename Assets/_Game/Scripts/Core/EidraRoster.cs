using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class EidraRoster
	{
		private readonly List<EidraInstanceState> _instances = new List<EidraInstanceState>();

		private readonly List<string> _activeInstanceIds = new List<string>();

		public event Action Changed;

		public EidraInstanceState[] GetAll()
		{
			EidraInstanceState[] array = _instances.ToArray();
			Array.Sort(array, (EidraInstanceState left, EidraInstanceState right) => string.CompareOrdinal(left.InstanceId, right.InstanceId));
			return array;
		}

		public string[] GetActiveInstanceIds()
		{
			return _activeInstanceIds.ToArray();
		}

		public bool TryGet(string instanceId, out EidraInstanceState state)
		{
			if (!string.IsNullOrWhiteSpace(instanceId))
			{
				foreach (EidraInstanceState instance in _instances)
				{
					if (!string.Equals(instance.InstanceId, instanceId, StringComparison.Ordinal))
					{
						continue;
					}
					state = instance;
					return true;
				}
			}
			state = default(EidraInstanceState);
			return false;
		}

		public bool TryAdd(EidraInstanceState state)
		{
			if (!state.IsValid || TryGet(state.InstanceId, out var _))
			{
				return false;
			}
			_instances.Add(state);
			this.Changed?.Invoke();
			return true;
		}

		public bool TrySetActiveTeam(IReadOnlyList<string> instanceIds, int capacity, out string error)
		{
			int num = Math.Clamp(capacity, 0, 2);
			List<string> list = new List<string>(num);
			if (instanceIds != null)
			{
				foreach (string instanceId in instanceIds)
				{
					if (!TryGet(instanceId, out var _))
					{
						error = "Eidra instance '" + instanceId + "' is not in the roster.";
						return false;
					}
					if (list.Contains(instanceId))
					{
						error = "Eidra instance '" + instanceId + "' is listed twice in the active team.";
						return false;
					}
					list.Add(instanceId);
				}
			}
			if (list.Count > num)
			{
				error = $"At most {num} eidra can be active; " + $"{list.Count} were requested.";
				return false;
			}
			_activeInstanceIds.Clear();
			_activeInstanceIds.AddRange(list);
			error = string.Empty;
			this.Changed?.Invoke();
			return true;
		}

		public int NextOrdinalFor(string eidraId)
		{
			int num = 0;
			foreach (EidraInstanceState instance in _instances)
			{
				if (string.Equals(instance.EidraId, eidraId, StringComparison.Ordinal) && EidraInstanceState.TryParseOrdinal(instance.InstanceId, out var ordinal) && ordinal > num)
				{
					num = ordinal;
				}
			}
			return num + 1;
		}

		public void Restore(IReadOnlyList<EidraInstanceState> instances, IReadOnlyList<string> activeInstanceIds)
		{
			List<EidraInstanceState> list = new List<EidraInstanceState>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			if (instances != null)
			{
				foreach (EidraInstanceState instance in instances)
				{
					if (!instance.IsValid || !hashSet.Add(instance.InstanceId))
					{
						throw new InvalidOperationException("Eidra roster contains invalid or duplicate instance IDs.");
					}
					list.Add(instance);
				}
			}
			_instances.Clear();
			_instances.AddRange(list);
			_activeInstanceIds.Clear();
			if (!TrySetActiveTeam(activeInstanceIds, 2, out var error))
			{
				_instances.Clear();
				throw new InvalidOperationException(error);
			}
			this.Changed?.Invoke();
		}

		public void Reset()
		{
			_instances.Clear();
			_activeInstanceIds.Clear();
			this.Changed?.Invoke();
		}
	}
}
