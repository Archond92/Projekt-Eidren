using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class TechnologyUnlockService
	{
		private readonly TechnologyTreeDefinition _tree;

		private readonly PlayerProgressionService _progression;

		private readonly Func<string, bool> _hasProgressFlag;

		private readonly HashSet<string> _unlocked = new HashSet<string>(StringComparer.Ordinal);

		public event Action Changed;

		public TechnologyUnlockService(TechnologyTreeDefinition tree, PlayerProgressionService progression, Func<string, bool> hasProgressFlag = null)
		{
			_tree = tree ?? throw new ArgumentNullException("tree");
			_progression = progression ?? throw new ArgumentNullException("progression");
			_hasProgressFlag = hasProgressFlag ?? ((Func<string, bool>)((string _) => false));
			string[] validationErrors = tree.GetValidationErrors();
			if (validationErrors.Length != 0)
			{
				throw new ArgumentException(string.Join("; ", validationErrors), "tree");
			}
			AddInitialUnlocks();
		}

		public TechnologyNodeState GetNodeState(string nodeId)
		{
			if (!_tree.TryGetNode(nodeId, out var node))
			{
				return TechnologyNodeState.Locked;
			}
			if (_unlocked.Contains(nodeId))
			{
				return TechnologyNodeState.Unlocked;
			}
			if (node.Stage > _progression.State.Stage)
			{
				return TechnologyNodeState.Locked;
			}
			if (!string.IsNullOrWhiteSpace(node.RequiredProgressFlag) && !_hasProgressFlag(node.RequiredProgressFlag))
			{
				return TechnologyNodeState.Locked;
			}
			if (!string.IsNullOrWhiteSpace(node.RequiredBlueprintId) && !_progression.KnowsBlueprint(node.RequiredBlueprintId))
			{
				return TechnologyNodeState.Locked;
			}
			foreach (string prerequisiteNodeId in node.PrerequisiteNodeIds)
			{
				if (!_unlocked.Contains(prerequisiteNodeId))
				{
					return TechnologyNodeState.Locked;
				}
			}
			return TechnologyNodeState.Available;
		}

		public bool IsRecipeUnlocked(string recipeId)
		{
			return IsUnlocked(recipeId, (TechnologyNodeDefinition node) => node.RecipeIds);
		}

		public bool IsBuildingUnlocked(string buildingId)
		{
			return IsUnlocked(buildingId, (TechnologyNodeDefinition node) => node.BuildingIds);
		}

		public bool IsFeatureUnlocked(string featureId)
		{
			return IsUnlocked(featureId, (TechnologyNodeDefinition node) => node.FeatureIds);
		}

		public string GetLockReason(string nodeId)
		{
			if (!_tree.TryGetNode(nodeId, out var node))
			{
				return "Unbekannter Knoten";
			}
			if (_unlocked.Contains(nodeId))
			{
				return string.Empty;
			}
			if (node.Stage > _progression.State.Stage)
			{
				return $"Stufe {node.Stage} erforderlich";
			}
			if (!string.IsNullOrWhiteSpace(node.RequiredProgressFlag) && !_hasProgressFlag(node.RequiredProgressFlag))
			{
				return (node.RequiredProgressFlag == "garon_defeated") ? "Garon muss besiegt sein" : ((node.RequiredProgressFlag == "tier_2_unlocked") ? "Gewölbeabschluss / T2-Flag erforderlich" : "Fortschrittsflag fehlt");
			}
			if (!string.IsNullOrWhiteSpace(node.RequiredBlueprintId) && !_progression.KnowsBlueprint(node.RequiredBlueprintId))
			{
				return "Bauplan erforderlich";
			}
			foreach (string prerequisiteNodeId in node.PrerequisiteNodeIds)
			{
				if (!_unlocked.Contains(prerequisiteNodeId))
				{
					return "Voraussetzung: " + prerequisiteNodeId;
				}
			}
			return string.Empty;
		}

		public bool TryGetUnlockingNode(string buildingId, out TechnologyNodeDefinition node)
		{
			foreach (TechnologyNodeDefinition node2 in _tree.Nodes)
			{
				foreach (string buildingId2 in node2.BuildingIds)
				{
					if (string.Equals(buildingId2, buildingId, StringComparison.Ordinal))
					{
						node = node2;
						return true;
					}
				}
			}
			node = null;
			return false;
		}

		public TechnologyUnlockResult TryUnlock(string nodeId)
		{
			if (!_tree.TryGetNode(nodeId, out var node))
			{
				return TechnologyUnlockResult.UnknownNode;
			}
			if (_unlocked.Contains(nodeId))
			{
				return TechnologyUnlockResult.AlreadyUnlocked;
			}
			if (node.Stage > _progression.State.Stage)
			{
				return TechnologyUnlockResult.WrongProgressionStage;
			}
			if (GetNodeState(nodeId) == TechnologyNodeState.Locked)
			{
				return TechnologyUnlockResult.PrerequisiteMissing;
			}
			if (!_progression.TrySpendTechnologyPoints(node.PointCost))
			{
				return TechnologyUnlockResult.NotEnoughTechnologyPoints;
			}
			_unlocked.Add(nodeId);
			this.Changed?.Invoke();
			return TechnologyUnlockResult.Success;
		}

		public string[] CaptureUnlockedNodeIds()
		{
			string[] array = new string[_unlocked.Count];
			_unlocked.CopyTo(array);
			Array.Sort(array, StringComparer.Ordinal);
			return array;
		}

		public bool ValidateUnlockedNodeIds(string[] nodeIds, int progressionStage, out string error)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			string[] array = nodeIds ?? Array.Empty<string>();
			foreach (string text in array)
			{
				if (string.IsNullOrWhiteSpace(text) || !hashSet.Add(text))
				{
					error = "Technology save contains an empty or duplicate ID.";
					return false;
				}
				if (!_tree.TryGetNode(text, out var node))
				{
					error = "Technology save contains unknown ID '" + text + "'.";
					return false;
				}
				if (node.Stage > progressionStage)
				{
					error = "Technology '" + text + "' belongs to a locked stage.";
					return false;
				}
			}
			foreach (string item in hashSet)
			{
				_tree.TryGetNode(item, out var node2);
				foreach (string prerequisiteNodeId in node2.PrerequisiteNodeIds)
				{
					if (!hashSet.Contains(prerequisiteNodeId))
					{
						error = "Technology '" + item + "' is missing prerequisite '" + prerequisiteNodeId + "' in the save.";
						return false;
					}
				}
			}
			error = string.Empty;
			return true;
		}

		public void Restore(string[] nodeIds)
		{
			if (!ValidateUnlockedNodeIds(nodeIds, _progression.State.Stage, out var error))
			{
				throw new InvalidOperationException(error);
			}
			_unlocked.Clear();
			string[] array = nodeIds ?? Array.Empty<string>();
			foreach (string item in array)
			{
				_unlocked.Add(item);
			}
			AddInitialUnlocks();
			this.Changed?.Invoke();
		}

		public void ResetForNewGame()
		{
			_unlocked.Clear();
			AddInitialUnlocks();
			this.Changed?.Invoke();
		}

		private void AddInitialUnlocks()
		{
			foreach (TechnologyNodeDefinition node in _tree.Nodes)
			{
				if (node != null && node.InitiallyUnlocked)
				{
					_unlocked.Add(node.Id);
				}
			}
		}

		private bool IsUnlocked(string unlockId, Func<TechnologyNodeDefinition, IReadOnlyList<string>> selector)
		{
			if (string.IsNullOrWhiteSpace(unlockId))
			{
				return false;
			}
			foreach (TechnologyNodeDefinition node in _tree.Nodes)
			{
				if (node == null || !_unlocked.Contains(node.Id))
				{
					continue;
				}
				foreach (string item in selector(node))
				{
					if (string.Equals(item, unlockId, StringComparison.Ordinal))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
