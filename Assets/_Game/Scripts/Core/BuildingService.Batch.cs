using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class BuildingService
	{
		private readonly ContentDatabase _content;

		private readonly GameSession _session;

		private readonly TechnologyUnlockService _technology;

		private readonly PlayerProgressionService _progression;

		private readonly IMaterialStock _materials;

		public IMaterialStock Materials => _materials;

		public BuildingActionResult TryPlaceBatch(IReadOnlyList<BuildingPlacementCandidate> candidates, IBuildingPlacementRule rule, out BuildingInstanceState[] placed)
		{
			placed = Array.Empty<BuildingInstanceState>();
			if (candidates == null || candidates.Count == 0)
			{
				return BuildingActionResult.UnknownBuilding;
			}
			if (rule == null)
			{
				throw new ArgumentNullException("rule");
			}
			List<BuildingInstanceState> list = new List<BuildingInstanceState>(candidates.Count);
			List<BuildingInstanceState> list2 = new List<BuildingInstanceState>(_session.Buildings.GetAll());
			Dictionary<string, int> totalCost = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (BuildingPlacementCandidate candidate in candidates)
			{
				BuildingInstanceState instance;
				BuildingActionResult buildingActionResult = Admit(candidate, rule, list2, totalCost, out instance);
				if (buildingActionResult != BuildingActionResult.Success)
				{
					return buildingActionResult;
				}
				list.Add(instance);
				list2.Add(instance);
			}
			if (!CanAfford(totalCost))
			{
				return BuildingActionResult.MissingMaterials;
			}
			if (!_materials.TryApplyTransaction(ToAmounts(totalCost), string.Empty, 0, out var failure))
			{
				if (failure != InventoryTransactionFailure.MissingItems)
				{
					return BuildingActionResult.UnknownBuilding;
				}
				return BuildingActionResult.MissingMaterials;
			}
			foreach (BuildingInstanceState item in list)
			{
				if (!_session.Buildings.TryAdd(item))
				{
					throw new InvalidOperationException("A freshly generated building instance ID collided.");
				}
				BuildingCostDefinition value;
				int tier = (_content.TryGetBuilding(item.BuildingId, out value) ? value.Tier : 0);
				_progression.RecordBuildingConstructed(item.BuildingId, tier);
			}
			placed = list.ToArray();
			return BuildingActionResult.Success;
		}

		private BuildingActionResult Admit(in BuildingPlacementCandidate candidate, IBuildingPlacementRule rule, List<BuildingInstanceState> provisional, Dictionary<string, int> totalCost, out BuildingInstanceState instance)
		{
			instance = null;
			if (!_content.TryGetBuilding(candidate.BuildingId, out var value))
			{
				return BuildingActionResult.UnknownBuilding;
			}
			if (value.PlacementKind != BuildingPlacementKind.Floor && value.PlacementKind != BuildingPlacementKind.Edge)
			{
				return BuildingActionResult.UnknownBuilding;
			}
			if (!_technology.IsBuildingUnlocked(candidate.BuildingId))
			{
				return BuildingActionResult.BuildingLocked;
			}
			BuildingPlacementEvaluation buildingPlacementEvaluation = rule.Evaluate(in candidate, provisional);
			if (buildingPlacementEvaluation.Result != BuildingActionResult.Success)
			{
				return buildingPlacementEvaluation.Result;
			}
			if (buildingPlacementEvaluation.ReplacesExisting)
			{
				return BuildingActionResult.EdgeOccupied;
			}
			foreach (CraftingIngredient item in value.Cost)
			{
				totalCost.TryGetValue(item.ItemId, out var value2);
				totalCost[item.ItemId] = value2 + item.Amount;
			}
			instance = new BuildingInstanceState(Guid.NewGuid().ToString("N"), candidate.BuildingId, BuildingPlacementRule.Snap(candidate.Position), candidate.QuarterTurns, candidate.Level);
			return BuildingActionResult.Success;
		}

		private bool CanAfford(Dictionary<string, int> totalCost)
		{
			foreach (KeyValuePair<string, int> item in totalCost)
			{
				if (_materials.GetTotalAmount(item.Key) < item.Value)
				{
					return false;
				}
			}
			return true;
		}

		private static InventoryItemAmount[] ToAmounts(Dictionary<string, int> totalCost)
		{
			InventoryItemAmount[] array = new InventoryItemAmount[totalCost.Count];
			int num = 0;
			foreach (KeyValuePair<string, int> item in totalCost)
			{
				array[num++] = new InventoryItemAmount(item.Key, item.Value);
			}
			return array;
		}

		public BuildingService(ContentDatabase content, GameSession session, TechnologyUnlockService technology, PlayerProgressionService progression)
		{
			_content = content ?? throw new ArgumentNullException("content");
			_session = session ?? throw new ArgumentNullException("session");
			_technology = technology ?? throw new ArgumentNullException("technology");
			_progression = progression ?? throw new ArgumentNullException("progression");
			_materials = new HomeBaseMaterialStock(content, session);
		}

		public BuildingActionResult EvaluatePlacement(in BuildingPlacementCandidate candidate, IBuildingPlacementRule rule)
		{
			return Evaluate(in candidate, rule).Result;
		}

		private BuildingPlacementEvaluation Evaluate(in BuildingPlacementCandidate candidate, IBuildingPlacementRule rule)
		{
			if (!_content.TryGetBuilding(candidate.BuildingId, out var value))
			{
				return new BuildingPlacementEvaluation(BuildingActionResult.UnknownBuilding);
			}
			BuildingPlacementEvaluation evaluation = (rule ?? throw new ArgumentNullException("rule")).Evaluate(in candidate, _session.Buildings.GetAll());
			if (evaluation.Result != BuildingActionResult.Success)
			{
				return evaluation;
			}
			if (!_technology.IsBuildingUnlocked(candidate.BuildingId))
			{
				return new BuildingPlacementEvaluation(BuildingActionResult.BuildingLocked);
			}
			// F31-010: Mengengrenze je Gebaeudetyp (0 = unbegrenzt). Ein im
			// selben Zug ersetztes Gebaeude zaehlt nicht mit.
			if (value.MaximumCount > 0)
			{
				int existing = 0;
				foreach (BuildingInstanceState state in _session.Buildings.GetAll())
				{
					if (string.Equals(state.BuildingId, candidate.BuildingId, StringComparison.Ordinal) && (!evaluation.ReplacesExisting || !string.Equals(state.InstanceId, evaluation.ReplacedInstanceId, StringComparison.Ordinal)))
					{
						existing++;
					}
				}
				if (existing >= value.MaximumCount)
				{
					return new BuildingPlacementEvaluation(BuildingActionResult.BuildingLimitReached);
				}
			}
			if (!HasCosts(value, RefundFor(in evaluation)))
			{
				return new BuildingPlacementEvaluation(BuildingActionResult.MissingMaterials, evaluation.ReplacedInstanceId);
			}
			return evaluation;
		}

		public BuildingActionResult TryPlace(in BuildingPlacementCandidate candidate, IBuildingPlacementRule rule, out BuildingInstanceState instance)
		{
			instance = null;
			BuildingPlacementEvaluation buildingPlacementEvaluation = Evaluate(in candidate, rule);
			if (buildingPlacementEvaluation.Result != BuildingActionResult.Success)
			{
				return buildingPlacementEvaluation.Result;
			}
			_content.TryGetBuilding(candidate.BuildingId, out var value);
			if (buildingPlacementEvaluation.ReplacesExisting)
			{
				InventoryItemAmount[] refund;
				BuildingActionResult buildingActionResult = TryDemolish(buildingPlacementEvaluation.ReplacedInstanceId, out refund);
				if (buildingActionResult != BuildingActionResult.Success)
				{
					return buildingActionResult;
				}
			}
			if (!_materials.TryApplyTransaction(ToAmounts(value.Cost), string.Empty, 0, out var failure))
			{
				if (failure != InventoryTransactionFailure.MissingItems)
				{
					return BuildingActionResult.UnknownBuilding;
				}
				return BuildingActionResult.MissingMaterials;
			}
			instance = new BuildingInstanceState(Guid.NewGuid().ToString("N"), candidate.BuildingId, BuildingPlacementRule.Snap(candidate.Position), candidate.QuarterTurns, candidate.Level);
			if (!_session.Buildings.TryAdd(instance))
			{
				throw new InvalidOperationException("A freshly generated building instance ID collided.");
			}
			_progression.RecordBuildingConstructed(candidate.BuildingId, value.Tier);
			return BuildingActionResult.Success;
		}

		private InventoryItemAmount[] RefundFor(in BuildingPlacementEvaluation evaluation)
		{
			if (!evaluation.ReplacesExisting || !_session.Buildings.TryGet(evaluation.ReplacedInstanceId, out var state) || !_content.TryGetBuilding(state.BuildingId, out var value))
			{
				return Array.Empty<InventoryItemAmount>();
			}
			return Refund(value.Cost);
		}

		public BuildingActionResult TryDemolish(string instanceId, out InventoryItemAmount[] refund)
		{
			refund = Array.Empty<InventoryItemAmount>();
			if (!_session.Buildings.TryGet(instanceId, out var state) || !_content.TryGetBuilding(state.BuildingId, out var value))
			{
				return BuildingActionResult.UnknownInstance;
			}
			BuildingActionResult buildingActionResult = CheckMovable(state);
			if (buildingActionResult != BuildingActionResult.Success)
			{
				return buildingActionResult;
			}
			refund = Refund(value.Cost);
			if (refund.Length != 0 && !_session.PlayerInventory.CanAddBatch(refund))
			{
				refund = Array.Empty<InventoryItemAmount>();
				return BuildingActionResult.RefundInventoryFull;
			}
			if (string.Equals(state.BuildingId, "building.storage_chest", StringComparison.Ordinal) && !_session.IsStorageEmpty(instanceId))
			{
				refund = Array.Empty<InventoryItemAmount>();
				return BuildingActionResult.StorageNotEmpty;
			}
			if (refund.Length != 0 && !_session.PlayerInventory.TryAddBatch(refund))
			{
				refund = Array.Empty<InventoryItemAmount>();
				return BuildingActionResult.RefundInventoryFull;
			}
			if (!_session.Buildings.Remove(instanceId))
			{
				throw new InvalidOperationException("Building vanished at commit.");
			}
			_session.RemoveStorageState(instanceId);
			return BuildingActionResult.Success;
		}

		private bool HasCosts(BuildingCostDefinition building, IReadOnlyList<InventoryItemAmount> incoming)
		{
			string itemId;
			return !TryFindMissing(building, incoming, out itemId);
		}

		private bool TryFindMissing(BuildingCostDefinition building, IReadOnlyList<InventoryItemAmount> incoming, out string itemId)
		{
			foreach (CraftingIngredient item in building.Cost)
			{
				int num = _materials.GetTotalAmount(item.ItemId);
				foreach (InventoryItemAmount item2 in incoming)
				{
					if (string.Equals(item2.ItemId, item.ItemId, StringComparison.Ordinal))
					{
						num += item2.Amount;
					}
				}
				if (num < item.Amount)
				{
					itemId = item.ItemId;
					return true;
				}
			}
			itemId = string.Empty;
			return false;
		}

		private static InventoryItemAmount[] ToAmounts(IReadOnlyList<CraftingIngredient> cost)
		{
			InventoryItemAmount[] array = new InventoryItemAmount[cost.Count];
			for (int i = 0; i < cost.Count; i++)
			{
				array[i] = new InventoryItemAmount(cost[i].ItemId, cost[i].Amount);
			}
			return array;
		}

		private static InventoryItemAmount[] Refund(IReadOnlyList<CraftingIngredient> cost)
		{
			List<InventoryItemAmount> list = new List<InventoryItemAmount>();
			foreach (CraftingIngredient item in cost)
			{
				int amount = item.Amount;
				if (amount > 0)
				{
					list.Add(new InventoryItemAmount(item.ItemId, amount));
				}
			}
			return list.ToArray();
		}

		public BuildingActionResult EvaluateMoveReadiness(string instanceId)
		{
			if (!_session.Buildings.TryGet(instanceId, out var state))
			{
				return BuildingActionResult.UnknownInstance;
			}
			return CheckMovable(state);
		}

		public BuildingActionResult TryMove(string instanceId, Vector3 targetPosition, int quarterTurns, IBuildingPlacementRule rule, out BuildingInstanceState moved)
		{
			moved = null;
			if (!_session.Buildings.TryGet(instanceId, out var state) || !_content.TryGetBuilding(state.BuildingId, out var _))
			{
				return BuildingActionResult.UnknownInstance;
			}
			BuildingActionResult buildingActionResult = CheckMovable(state);
			if (buildingActionResult != BuildingActionResult.Success)
			{
				return buildingActionResult;
			}
			BuildingPlacementCandidate candidate = new BuildingPlacementCandidate(state.BuildingId, targetPosition, quarterTurns, state.Level);
			BuildingPlacementEvaluation buildingPlacementEvaluation = (rule ?? throw new ArgumentNullException("rule")).Evaluate(in candidate, Without(instanceId));
			if (buildingPlacementEvaluation.Result != BuildingActionResult.Success)
			{
				return buildingPlacementEvaluation.Result;
			}
			moved = new BuildingInstanceState(state.InstanceId, state.BuildingId, BuildingPlacementRule.Snap(targetPosition), quarterTurns, state.Level, state.PlantedSlots, state.ReadyOutput);
			if (!_session.Buildings.Remove(instanceId) || !_session.Buildings.TryAdd(moved))
			{
				moved = null;
				return BuildingActionResult.UnknownInstance;
			}
			return BuildingActionResult.Success;
		}

		private BuildingActionResult CheckMovable(BuildingInstanceState state)
		{
			if (state.PlantedSlots > 0 || state.ReadyOutput > 0)
			{
				return BuildingActionResult.BuildingBusy;
			}
			if (!_session.IsStorageEmpty(state.InstanceId))
			{
				return BuildingActionResult.StorageNotEmpty;
			}
			return BuildingActionResult.Success;
		}

		private BuildingInstanceState[] Without(string instanceId)
		{
			BuildingInstanceState[] all = _session.Buildings.GetAll();
			List<BuildingInstanceState> list = new List<BuildingInstanceState>(all.Length);
			BuildingInstanceState[] array = all;
			foreach (BuildingInstanceState buildingInstanceState in array)
			{
				if (!string.Equals(buildingInstanceState.InstanceId, instanceId, StringComparison.Ordinal))
				{
					list.Add(buildingInstanceState);
				}
			}
			return list.ToArray();
		}

		public BuildingPreview PreviewPlacement(in BuildingPlacementCandidate candidate, IBuildingPlacementRule rule)
		{
			BuildingPlacementEvaluation evaluation = Evaluate(in candidate, rule);
			if (evaluation.Result != BuildingActionResult.MissingMaterials || !_content.TryGetBuilding(candidate.BuildingId, out var value))
			{
				return new BuildingPreview(evaluation.Result);
			}
			TryFindMissing(value, RefundFor(in evaluation), out var itemId);
			return new BuildingPreview(evaluation.Result, itemId);
		}

		public BuildingPreview PreviewMove(string instanceId, in BuildingPlacementCandidate candidate, IBuildingPlacementRule rule)
		{
			if (!_session.Buildings.TryGet(instanceId, out var _))
			{
				return new BuildingPreview(BuildingActionResult.UnknownInstance);
			}
			return new BuildingPreview((rule ?? throw new ArgumentNullException("rule")).Evaluate(in candidate, Without(instanceId)).Result);
		}
	}
}
