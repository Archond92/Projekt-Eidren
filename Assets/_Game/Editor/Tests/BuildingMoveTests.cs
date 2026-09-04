using Eidren.Core.BuildGrid;
using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class BuildingMoveTests
{
	private GameObject _root;

	private ContentDatabase _content;

	private GameSession _session;

	private PlayerProgressionService _progression;

	private TechnologyUnlockService _technology;

	private BuildingService _service;

	private BuildingPlacementRule _rule;

	private int Wood => _session.PlayerInventory.GetTotalAmount("wood");

	private int Stone => _session.PlayerInventory.GetTotalAmount("stone");

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("BuildingMoveTests");
		_content = _root.AddComponent<ContentDatabase>();
		_session = _root.AddComponent<GameSession>();
		_session.Initialize();
		_session.ConfigureContentDatabase(_content);
		_session.StartNewGame();
		_progression = new PlayerProgressionService(Load<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset"));
		_technology = new TechnologyUnlockService(Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset"), _progression);
		_service = new BuildingService(_content, _session, _technology, _progression);
		_rule = new BuildingPlacementRule(_content, new BuildingPlacementArea(new Rect(-20f, -20f, 40f, 40f), Array.Empty<Rect>(), new BuildGridOrigin(0f, 0f)));
		UnlockAll();
		Stock("wood", 40);
		Stock("stone", 30);
		Stock("plant_fiber", 20);
		Stock("copper_bar", 10);
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_root);
	}

	[Test]
	public void MovingKeepsTheInstanceIdAndCostsNothing()
	{
		BuildingInstanceState placed = Place("building.workbench", 3f, 3f);
		int woodBefore = Wood;
		int stoneBefore = Stone;
		Assert.That<BuildingActionResult>(_service.TryMove(placed.InstanceId, new Vector3(8f, 0f, 8f), 2, _rule, out var moved), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<string>(moved.InstanceId, (IResolveConstraint)(object)Is.EqualTo((object)placed.InstanceId));
		Assert.That<Vector3>(moved.Position, (IResolveConstraint)(object)Is.EqualTo((object)new Vector3(8f, 0f, 8f)));
		Assert.That<int>(moved.QuarterTurns, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(Wood, (IResolveConstraint)(object)Is.EqualTo((object)woodBefore), "Kein Materialaufwand.", Array.Empty<object>());
		Assert.That<int>(Stone, (IResolveConstraint)(object)Is.EqualTo((object)stoneBefore), "Keine Erstattung.", Array.Empty<object>());
		Assert.That<BuildingInstanceState[]>(_session.Buildings.GetAll(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
	}

	[Test]
	public void TheOldCellIsFreeAfterwards()
	{
		BuildingInstanceState placed = Place("building.workbench", 3f, 3f);
		_service.TryMove(placed.InstanceId, new Vector3(8f, 0f, 8f), 0, _rule, out var _);
		Assert.That<BuildingActionResult>(_service.EvaluatePlacement(Candidate("building.smelter", 3f, 3f, 0), _rule), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.FloorRequired), "Nur noch der fehlende Boden blockiert, nicht die Werkbank.", Array.Empty<object>());
	}

	[Test]
	public void ABuildingMayBeNudgedOntoItsOwnOldFootprint()
	{
		BuildingInstanceState farm = Place("building.farm_plot", 3f, 3f);
		Assert.That<BuildingActionResult>(_service.TryMove(farm.InstanceId, new Vector3(4f, 0f, 3f), 0, _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Das Ziel ueberlappt die alte Flaeche — es kollidiert nicht mit sich selbst.", Array.Empty<object>());
	}

	[Test]
	public void AnOccupiedTargetIsRejectedAndNothingChanges()
	{
		BuildingInstanceState first = Place("building.workbench", 3f, 3f);
		Place("building.storage_chest", 8f, 8f);
		Assert.That<BuildingActionResult>(_service.TryMove(first.InstanceId, new Vector3(8f, 0f, 8f), 0, _rule, out var moved), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Overlap));
		Assert.That<BuildingInstanceState>(moved, (IResolveConstraint)(object)Is.Null);
		Assert.That<bool>(_session.Buildings.TryGet(first.InstanceId, out var unchanged), (IResolveConstraint)(object)Is.True);
		Assert.That<Vector3>(unchanged.Position, (IResolveConstraint)(object)Is.EqualTo((object)new Vector3(3f, 0f, 3f)));
	}

	[Test]
	public void FloorsAndWallsCanBeMoved()
	{
		BuildingInstanceState floor = Place("building.floor", 3f, 3f);
		BuildingInstanceState wall = Place("building.wall", 5f, 5f);
		Assert.That<BuildingActionResult>(_service.TryMove(floor.InstanceId, new Vector3(9f, 0f, 9f), 0, _rule, out var moved), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(_service.TryMove(wall.InstanceId, new Vector3(10f, 0f, 10f), 0, _rule, out moved), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
	}

	[Test]
	public void AFullChestStaysWhereItIs()
	{
		BuildingInstanceState chest = Place("building.storage_chest", 3f, 3f);
		ItemStack[] slots = new ItemStack[24];
		slots[0] = new ItemStack("wood", 1);
		_session.SetStorageState(new StorageContainerState(chest.InstanceId, slots));
		Assert.That<BuildingActionResult>(_service.TryMove(chest.InstanceId, new Vector3(8f, 0f, 8f), 0, _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.StorageNotEmpty));
	}

	[Test]
	public void AnEmptiedChestMovesAgain()
	{
		BuildingInstanceState chest = Place("building.storage_chest", 3f, 3f);
		ItemStack[] slots = new ItemStack[24];
		slots[0] = new ItemStack("wood", 1);
		_session.SetStorageState(new StorageContainerState(chest.InstanceId, slots));
		_session.SetStorageState(new StorageContainerState(chest.InstanceId, new ItemStack[24]));
		Assert.That<BuildingActionResult>(_service.TryMove(chest.InstanceId, new Vector3(8f, 0f, 8f), 0, _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
	}

	[Test]
	public void APlantedFarmStaysWhereItIs()
	{
		BuildingInstanceState farm = Place("building.farm_plot", 3f, 3f);
		Assert.That<bool>(_session.Buildings.TryUpdateProduction(farm.InstanceId, 1, 0), (IResolveConstraint)(object)Is.True);
		Assert.That<BuildingActionResult>(_service.TryMove(farm.InstanceId, new Vector3(9f, 0f, 9f), 0, _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.BuildingBusy));
	}

	[Test]
	public void AStationWithReadyOutputStaysWhereItIs()
	{
		Place("building.floor", 3f, 3f);
		BuildingInstanceState smelter = Place("building.smelter", 3f, 3f);
		_session.Buildings.TryUpdateProduction(smelter.InstanceId, 0, 2);
		Assert.That<BuildingActionResult>(_service.TryMove(smelter.InstanceId, new Vector3(9f, 0f, 9f), 0, _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.BuildingBusy));
	}

	[Test]
	public void MovingPreservesLevelAndDynamicState()
	{
		BuildingInstanceState chest = Place("building.storage_chest", 3f, 3f);
		_service.TryMove(chest.InstanceId, new Vector3(8f, 0f, 8f), 1, _rule, out var moved);
		Assert.That<int>(moved.Level, (IResolveConstraint)(object)Is.EqualTo((object)chest.Level));
		Assert.That<string>(moved.BuildingId, (IResolveConstraint)(object)Is.EqualTo((object)chest.BuildingId));
		Assert.That<int>(moved.PlantedSlots, (IResolveConstraint)(object)Is.EqualTo((object)0));
		Assert.That<int>(moved.ReadyOutput, (IResolveConstraint)(object)Is.EqualTo((object)0));
	}

	[Test]
	public void AnUnknownInstanceCannotBeMoved()
	{
		Assert.That<BuildingActionResult>(_service.TryMove("does-not-exist", Vector3.zero, 0, _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.UnknownInstance));
	}

	private BuildingInstanceState Place(string buildingId, float x, float z)
	{
		Assert.That<BuildingActionResult>(_service.TryPlace(Candidate(buildingId, x, z, 0), _rule, out var state), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), $"{buildingId} bei ({x}, {z})", Array.Empty<object>());
		return state;
	}

	private static BuildingPlacementCandidate Candidate(string buildingId, float x, float z, int quarterTurns)
	{
		return new BuildingPlacementCandidate(buildingId, new Vector3(x, 0f, z), quarterTurns);
	}

	private void Stock(string itemId, int amount)
	{
		Assert.That<int>(_session.PlayerInventory.Add(itemId, amount), (IResolveConstraint)(object)Is.Zero, itemId + " passt nicht vollständig ins Inventar.", Array.Empty<object>());
	}

	private void UnlockAll()
	{
		_progression.RecordEnemyDefeated(100000);
		foreach (TechnologyNodeDefinition node in Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset").Nodes)
		{
			if (node.SortOrder <= 17 && _technology.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
			{
				_technology.TryUnlock(node.Id);
			}
		}
	}

	private static T Load<T>(string path) where T : UnityEngine.Object
	{
		T val = AssetDatabase.LoadAssetAtPath<T>(path);
		Assert.That<T>(val, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		return val;
	}
}
}
