using Eidren.Core.Services;
using Eidren.Data;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class BuildingServiceTests
{
	private GameObject _root;

	private ContentDatabase _content;

	private GameSession _session;

	private PlayerProgressionService _progression;

	private TechnologyUnlockService _technology;

	private BuildingService _service;

	private IBuildingPlacementRule _rule;

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("BuildingServiceTests");
		_content = _root.AddComponent<ContentDatabase>();
		_session = _root.AddComponent<GameSession>();
		_session.Initialize();
		_session.ConfigureContentDatabase(_content);
		_session.StartNewGame();
		ProgressionCurveDefinition curve = Load<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
		TechnologyTreeDefinition tree = Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
		_progression = new PlayerProgressionService(curve);
		_technology = new TechnologyUnlockService(tree, _progression);
		_service = new BuildingService(_content, _session, _technology, _progression);
		_rule = new BuildingPlacementRule(_content, new BuildingPlacementArea(new Rect(-10f, -10f, 20f, 20f), new Rect[2]
		{
			new Rect(-2f, -2f, 4f, 4f),
			new Rect(7f, -10f, 3f, 4f)
		}));
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_root);
	}

	[Test]
	public void PlacementRejectsLockedOverlapOutsideAndBlockedZones()
	{
		AddMaterials();
		BuildingPlacementCandidate workbench = Candidate("building.workbench", -5f, -5f);
		Assert.That<BuildingActionResult>(_service.TryPlace(in workbench, _rule, out var instance), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.BuildingLocked));
		UnlockAll();
		Assert.That<BuildingActionResult>(_service.TryPlace(in workbench, _rule, out instance), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		AddMaterials();
		Assert.That<BuildingActionResult>(_service.TryPlace(in workbench, _rule, out instance), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Overlap));
		Assert.That<BuildingActionResult>(_service.EvaluatePlacement(Candidate("building.workbench", 10f, 0f), _rule), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.OutsideBuildArea));
		Assert.That<BuildingActionResult>(_service.EvaluatePlacement(Candidate("building.workbench", 0f, 0f), _rule), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.BlockedZone));
	}

	[Test]
	public void InventoryIsChargedOnlyAtCommitAndRechecked()
	{
		UnlockAll();
		AddMaterials();
		BuildingPlacementCandidate candidate = Candidate("building.workbench", -5f, -5f);
		Assert.That<BuildingActionResult>(_service.EvaluatePlacement(in candidate, _rule), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)100));
		_session.PlayerInventory.Clear();
		Assert.That<BuildingActionResult>(_service.TryPlace(in candidate, _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.MissingMaterials));
		Assert.That<BuildingInstanceState[]>(_session.Buildings.GetAll(), (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void RotationAndLevelSurviveRegistryState()
	{
		UnlockAll();
		AddMaterials();
		BuildingPlacementCandidate candidate = new BuildingPlacementCandidate("building.workbench", new Vector3(-5.2f, 0f, -4.8f), 1);
		Assert.That<BuildingActionResult>(_service.TryPlace(in candidate, _rule, out var state), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<Vector3>(state.Position, (IResolveConstraint)(object)Is.EqualTo((object)new Vector3(-5f, 0f, -5f)));
		Assert.That<int>(state.QuarterTurns, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<float>(state.RotationDegrees, (IResolveConstraint)(object)Is.EqualTo((object)90f));
		Assert.That<int>(state.Level, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<string>(state.InstanceId, (IResolveConstraint)(object)Is.Not.Empty);
	}

	[Test]
	public void FarmOccupiesTwoByTwoAndEveryOtherBuildingOneByOne()
	{
		BuildingCostDefinition[] buildings = _content.GetBuildings();
		BuildingCostDefinition[] array = buildings;
		foreach (BuildingCostDefinition building in array)
		{
			Assert.That<bool>(building.TryGetLevel(1, out var footprint, out var prefab), (IResolveConstraint)(object)Is.True, building.Id, Array.Empty<object>());
			Assert.That<GameObject>(prefab, (IResolveConstraint)(object)Is.Not.Null, building.Id, Array.Empty<object>());
			if (building.Id == "building.farm_plot")
			{
				Assert.That<int>(footprint.Width, (IResolveConstraint)(object)Is.EqualTo((object)2));
				Assert.That<int>(footprint.Depth, (IResolveConstraint)(object)Is.EqualTo((object)2));
			}
			else
			{
				Assert.That<int>(footprint.Width, (IResolveConstraint)(object)Is.EqualTo((object)1), building.Id, Array.Empty<object>());
				Assert.That<int>(footprint.Depth, (IResolveConstraint)(object)Is.EqualTo((object)1), building.Id, Array.Empty<object>());
			}
		}
		Assert.That<BuildingCostDefinition[]>(buildings, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)11));
	}

	[Test]
	public void FullStorageCannotBeDemolishedThenReturnsFullCostWhenEmpty()
	{
		UnlockAll();
		AddMaterials();
		BuildingPlacementCandidate candidate = Candidate("building.storage_chest", -5f, -5f);
		Assert.That<BuildingActionResult>(_service.TryPlace(in candidate, _rule, out var state), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		ItemStack[] slots = new ItemStack[24];
		slots[0] = new ItemStack("wood", 1);
		_session.SetStorageState(new StorageContainerState(state.InstanceId, slots));
		Assert.That<BuildingActionResult>(_service.TryDemolish(state.InstanceId, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.StorageNotEmpty));
		_session.SetStorageState(new StorageContainerState(state.InstanceId, new ItemStack[24]));
		int woodBefore = _session.PlayerInventory.GetTotalAmount("wood");
		int stoneBefore = _session.PlayerInventory.GetTotalAmount("stone");
		Assert.That<BuildingActionResult>(_service.TryDemolish(state.InstanceId, out var refund2), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<IEnumerable<(string, int)>>(refund2.Select((InventoryItemAmount value) => (ItemId: value.ItemId, Amount: value.Amount)), (IResolveConstraint)(object)Does.Contain((object)("wood", 10)));
		// 4 statt 5 Stein: die StorageChest kostet laut BuildingCostDefinition
		// heute wood:10 + stone:4 — die Voll-Erstattung folgt den Ist-Kosten.
		Assert.That<IEnumerable<(string, int)>>(refund2.Select((InventoryItemAmount value) => (ItemId: value.ItemId, Amount: value.Amount)), (IResolveConstraint)(object)Does.Contain((object)("stone", 4)));
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)(woodBefore + 10)));
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("stone"), (IResolveConstraint)(object)Is.EqualTo((object)(stoneBefore + 4)));
		Assert.That<BuildingInstanceState[]>(_session.Buildings.GetAll(), (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void MenuPrefabHasElevenAuthoredRows()
	{
		Assert.That<BuildingMenuItemView[]>(Load<GameObject>("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab").GetComponentsInChildren<BuildingMenuItemView>(includeInactive: true), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)11));
	}

	[Test]
	public void MenuRowsShowIconsAndCanBeSelected()
	{
		UnlockAll();
		GameObject instance = UnityEngine.Object.Instantiate(Load<GameObject>("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab"));
		try
		{
			BuildingMenuWindow component = instance.GetComponent<BuildingMenuWindow>();
			component.Initialize(_content, _technology, _service.Materials);
			component.Open();
			Assert.That<int>(component.VisibleBuildings.Count, (IResolveConstraint)(object)Is.EqualTo((object)11));
			Assert.That<bool>(component.ItemViews.Take(11).All((BuildingMenuItemView view) => view.Icon != null && view.Icon.sprite != null), (IResolveConstraint)(object)Is.True);
			component.ItemViews[1].Button.onClick.Invoke();
			Assert.That<int>(component.SelectedIndex, (IResolveConstraint)(object)Is.EqualTo((object)1));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(instance);
		}
	}

	private static BuildingPlacementCandidate Candidate(string buildingId, float x, float z)
	{
		return new BuildingPlacementCandidate(buildingId, new Vector3(x, 0f, z), 0);
	}

	private void AddMaterials()
	{
		_session.PlayerInventory.Add("wood", 100);
		_session.PlayerInventory.Add("stone", 100);
		_session.PlayerInventory.Add("plant_fiber", 100);
		_session.PlayerInventory.Add("copper_bar", 10);
	}

	private void UnlockAll()
	{
		_progression.RecordEnemyDefeated(100000);
		foreach (TechnologyNodeDefinition node in Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset").Nodes)
		{
			if (node.SortOrder <= 17 && _technology.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
			{
				Assert.That<TechnologyUnlockResult>(_technology.TryUnlock(node.Id), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success), node.Id, Array.Empty<object>());
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
