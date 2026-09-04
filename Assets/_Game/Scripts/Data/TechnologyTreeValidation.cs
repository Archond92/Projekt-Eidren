using System.Collections.Generic;
using System.Linq;
using System;

namespace Eidren.Data
{
	internal static class TechnologyTreeValidation
	{
		public static string[] GetErrors(TechnologyNodeDefinition[] nodes)
		{
			List<string> list = new List<string>();
			if (nodes == null || nodes.Length == 0)
			{
				return new string[1] { "The technology tree has no nodes." };
			}
			Dictionary<string, TechnologyNodeDefinition> dictionary = new Dictionary<string, TechnologyNodeDefinition>(StringComparer.Ordinal);
			HashSet<int> orders = new HashSet<int>();
			foreach (TechnologyNodeDefinition node in nodes)
			{
				ValidateNode(node, dictionary, orders, list);
			}
			foreach (TechnologyNodeDefinition value in dictionary.Values)
			{
				foreach (string prerequisiteNodeId in value.PrerequisiteNodeIds)
				{
					if (!dictionary.ContainsKey(prerequisiteNodeId))
					{
						list.Add("Technology '" + value.Id + "' references missing prerequisite '" + prerequisiteNodeId + "'.");
					}
				}
			}
			DetectCycles(dictionary, list);
			ValidateUnlockOwnership(dictionary.Values, list);
			return list.ToArray();
		}

		private static void ValidateNode(TechnologyNodeDefinition node, IDictionary<string, TechnologyNodeDefinition> byId, ISet<int> orders, ICollection<string> errors)
		{
			if (node == null)
			{
				errors.Add("The technology tree contains a missing node.");
				return;
			}
			if (string.IsNullOrWhiteSpace(node.Id))
			{
				errors.Add("A technology node has no stable ID.");
			}
			else if (!byId.TryAdd(node.Id, node))
			{
				errors.Add("Technology ID '" + node.Id + "' is duplicated.");
			}
			if (string.IsNullOrWhiteSpace(node.DisplayName))
			{
				errors.Add("Technology '" + node.Id + "' has no display name.");
			}
			if (node.Stage < 1)
			{
				errors.Add("Technology '" + node.Id + "' has an invalid stage.");
			}
			if (node.SortOrder < 1 || !orders.Add(node.SortOrder))
			{
				errors.Add("Technology '" + node.Id + "' has an invalid or duplicate " + $"sort order {node.SortOrder}.");
			}
			if (node.InitiallyUnlocked && node.PointCost != 0)
			{
				errors.Add("Initially unlocked technology '" + node.Id + "' cannot have a point cost.");
			}
			else if (!node.InitiallyUnlocked && node.PointCost < 1)
			{
				errors.Add("Technology '" + node.Id + "' has no point cost.");
			}
			if (node.InitiallyUnlocked && node.PrerequisiteNodeIds.Count > 0)
			{
				errors.Add("Initially unlocked technology '" + node.Id + "' cannot have prerequisites.");
			}
			ValidateIds(node.Id, "prerequisite", node.PrerequisiteNodeIds, errors);
			ValidateIds(node.Id, "recipe", node.RecipeIds, errors);
			ValidateIds(node.Id, "building", node.BuildingIds, errors);
			ValidateIds(node.Id, "feature", node.FeatureIds, errors);
		}

		private static void DetectCycles(IReadOnlyDictionary<string, TechnologyNodeDefinition> nodes, ICollection<string> errors)
		{
			HashSet<string> visiting = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
			foreach (string key in nodes.Keys)
			{
				if (HasCycle(key, nodes, visiting, visited))
				{
					errors.Add("Technology graph contains a cycle at '" + key + "'.");
					break;
				}
			}
		}

		private static bool HasCycle(string id, IReadOnlyDictionary<string, TechnologyNodeDefinition> nodes, ISet<string> visiting, ISet<string> visited)
		{
			if (visited.Contains(id))
			{
				return false;
			}
			if (!visiting.Add(id))
			{
				return true;
			}
			if (nodes.TryGetValue(id, out var value))
			{
				foreach (string prerequisiteNodeId in value.PrerequisiteNodeIds)
				{
					if (nodes.ContainsKey(prerequisiteNodeId) && HasCycle(prerequisiteNodeId, nodes, visiting, visited))
					{
						return true;
					}
				}
			}
			visiting.Remove(id);
			visited.Add(id);
			return false;
		}

		private static void ValidateUnlockOwnership(IEnumerable<TechnologyNodeDefinition> nodes, ICollection<string> errors)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (TechnologyNodeDefinition node in nodes)
			{
				IEnumerable<string> enumerable = node.RecipeIds.Concat(node.BuildingIds).Concat(node.FeatureIds);
				foreach (string item in enumerable)
				{
					if (!hashSet.Add(item))
					{
						errors.Add("Unlock ID '" + item + "' belongs to more than one technology node.");
					}
				}
			}
		}

		private static void ValidateIds(string nodeId, string kind, IEnumerable<string> source, ICollection<string> errors)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (string item in source ?? Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(item))
				{
					errors.Add("Technology '" + nodeId + "' has an empty " + kind + " ID.");
				}
				else if (!hashSet.Add(item))
				{
					errors.Add("Technology '" + nodeId + "' repeats " + kind + " ID '" + item + "'.");
				}
			}
		}
	}
}
