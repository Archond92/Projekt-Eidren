using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	internal static class SaveEidraMapper
	{
		internal static EidraInstanceState[] ToRuntime(SaveEidraInstanceData[] source, ContentDatabase content, List<SaveQuarantineEntry> quarantine)
		{
			if (source == null || source.Length == 0)
			{
				return Array.Empty<EidraInstanceState>();
			}
			List<EidraInstanceState> list = new List<EidraInstanceState>(source.Length);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < source.Length; i++)
			{
				SaveEidraInstanceData saveEidraInstanceData = source[i];
				string context = $"player.eidraRoster[{i}]";
				if (saveEidraInstanceData == null || string.IsNullOrWhiteSpace(saveEidraInstanceData.instanceId) || string.IsNullOrWhiteSpace(saveEidraInstanceData.eidraId) || !hashSet.Add(saveEidraInstanceData.instanceId) || content == null || !content.TryGetEidra(saveEidraInstanceData.eidraId, out var _))
				{
					Quarantine(quarantine, context, saveEidraInstanceData?.eidraId, "Unknown eidra or duplicate instance id.");
				}
				else
				{
					list.Add(new EidraInstanceState(saveEidraInstanceData.instanceId, saveEidraInstanceData.eidraId));
				}
			}
			return list.ToArray();
		}

		internal static string[] ActiveToRuntime(string[] source, IReadOnlyList<EidraInstanceState> instances, List<SaveQuarantineEntry> quarantine)
		{
			if (source == null || source.Length == 0)
			{
				return Array.Empty<string>();
			}
			List<string> list = new List<string>(2);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < source.Length; i++)
			{
				string text = source[i];
				string context = $"player.activeEidraInstanceIds[{i}]";
				if (string.IsNullOrWhiteSpace(text) || !hashSet.Add(text) || !Contains(instances, text))
				{
					Quarantine(quarantine, context, text, "Active eidra is not in the roster.");
				}
				else if (list.Count >= 2)
				{
					Quarantine(quarantine, context, text, "At most two eidra can be active at once.");
				}
				else
				{
					list.Add(text);
				}
			}
			return list.ToArray();
		}

		internal static SaveEidraInstanceData[] ToSave(EidraInstanceState[] source)
		{
			if (source == null || source.Length == 0)
			{
				return Array.Empty<SaveEidraInstanceData>();
			}
			SaveEidraInstanceData[] array = new SaveEidraInstanceData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = new SaveEidraInstanceData
				{
					instanceId = source[i].InstanceId,
					eidraId = source[i].EidraId
				};
			}
			return array;
		}

		private static bool Contains(IReadOnlyList<EidraInstanceState> instances, string instanceId)
		{
			if (instances == null)
			{
				return false;
			}
			foreach (EidraInstanceState instance in instances)
			{
				if (string.Equals(instance.InstanceId, instanceId, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		private static void Quarantine(List<SaveQuarantineEntry> quarantine, string context, string stableId, string reason)
		{
			quarantine?.Add(new SaveQuarantineEntry
			{
				source = context,
				stableId = (stableId ?? string.Empty),
				reason = reason
			});
		}
	}
}
