using Eidren.Core.BuildGrid;
using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class BuildingBatchPlacementTests
{
	private GameObject _root;

	private ContentDatabase _content;

	private GameSession _session;

	private PlayerProgressionService _progression;

	private TechnologyUnlockService _technology;

	private BuildingService _service;

	private BuildingPlacementRule _rule;

	private static readonly BuildGridOrigin Origin = new BuildGridOrigin(0f, 0f);

	private int Wood => _session.PlayerInventory.GetTotalAmount("wood");

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("BuildingBatchPlacementTests");
		_content = _root.AddComponent<ContentDatabase>();
		_session = _root.AddComponent<GameSession>();
		_session.Initialize();
		_session.ConfigureContentDatabase(_content);
		_session.StartNewGame();
		_progression = new PlayerProgressionService(Load<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset"));
		_technology = new TechnologyUnlockService(Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset"), _progression);
		_service = new BuildingService(_content, _session, _technology, _progression);
		_rule = new BuildingPlacementRule(_content, new BuildingPlacementArea(new Rect(-20f, -20f, 40f, 40f), Array.Empty<Rect>(), Origin));
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
	public void ARectangleCoversEveryCellBetweenItsCorners()
	{
		IReadOnlyList<BuildingPlacementCandidate> readOnlyList = BuildingBatchPlan.Floor(Origin, "building.floor", new Vector3(2f, 0f, 2f), new Vector3(4f, 0f, 3f));
		Assert.That<int>(readOnlyList.Count, (IResolveConstraint)(object)Is.EqualTo((object)6));
		Assert.That<IEnumerable<(float, float)>>(readOnlyList.Select((BuildingPlacementCandidate value) => (x: value.Position.x, z: value.Position.z)), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new(float, float)[6]
		{
			(2f, 2f),
			(3f, 2f),
			(4f, 2f),
			(2f, 3f),
			(3f, 3f),
			(4f, 3f)
		}));
	}

	[Test]
	public void ASingleCellAndALineAreTheSameCase()
	{
		Assert.That<int>(BuildingBatchPlan.Floor(Origin, "building.floor", new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, 1f)).Count, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(BuildingBatchPlan.Floor(Origin, "building.floor", new Vector3(1f, 0f, 1f), new Vector3(5f, 0f, 1f)).Count, (IResolveConstraint)(object)Is.EqualTo((object)5));
	}

	[Test]
	public void CornerOrderDoesNotMatter()
	{
		Assert.That<IEnumerable<(float, float)>>(from value in BuildingBatchPlan.Floor(Origin, "building.floor", new Vector3(4f, 0f, 3f), new Vector3(2f, 0f, 2f))
			select (x: value.Position.x, z: value.Position.z), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)(from value in BuildingBatchPlan.Floor(Origin, "building.floor", new Vector3(2f, 0f, 2f), new Vector3(4f, 0f, 3f))
			select (x: value.Position.x, z: value.Position.z))));
	}

	[Test]
	public void AWallLineDoesNotBendWhenTheDragIsDiagonal()
	{
		IReadOnlyList<BuildingPlacementCandidate> readOnlyList = BuildingBatchPlan.Wall(Origin, "building.wall", new Vector3(1f, 0f, 1f), new Vector3(4f, 0f, 9f), 0);
		Assert.That<int>(readOnlyList.Count, (IResolveConstraint)(object)Is.EqualTo((object)4));
		Assert.That<bool>(readOnlyList.All((BuildingPlacementCandidate value) => Mathf.Approximately(value.Position.z, 1f)), (IResolveConstraint)(object)Is.True, "Eine Ost-West-Wandlinie bleibt auf ihrer Startzeile.", Array.Empty<object>());
	}

	[Test]
	public void AFloorRectangleIsBuiltInOneAction()
	{
		int woodBefore = Wood;
		Assert.That<BuildingActionResult>(_service.TryPlaceBatch(BuildingBatchPlan.Floor(Origin, "building.floor", new Vector3(2f, 0f, 2f), new Vector3(4f, 0f, 3f)), _rule, out var placed), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingInstanceState[]>(placed, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)6));
		Assert.That<BuildingInstanceState[]>(_session.Buildings.GetAll(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)6));
		Assert.That<int>(Wood, (IResolveConstraint)(object)Is.EqualTo((object)(woodBefore - 12)));
		Assert.That<int>(placed.Select((BuildingInstanceState state) => state.InstanceId).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)6), "Jedes Stueck bekommt eine eigene Instanz-ID.", Array.Empty<object>());
	}

	[Test]
	public void AWallLineIsBuiltInOneAction()
	{
		Assert.That<BuildingActionResult>(_service.TryPlaceBatch(BuildingBatchPlan.Wall(Origin, "building.wall", new Vector3(1f, 0f, 1f), new Vector3(5f, 0f, 1f), 0), _rule, out var placed), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingInstanceState[]>(placed, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)5));
	}

	[Test]
	public void OneBlockedCellCancelsTheWholeRectangle()
	{
		Assert.That<BuildingActionResult>(_service.TryPlace(new BuildingPlacementCandidate("building.farm_plot", new Vector3(3f, 0f, 3f), 0), _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		int woodBefore = Wood;
		int countBefore = _session.Buildings.GetAll().Length;
		Assert.That<BuildingActionResult>(_service.TryPlaceBatch(BuildingBatchPlan.Floor(Origin, "building.floor", new Vector3(1f, 0f, 1f), new Vector3(4f, 0f, 4f)), _rule, out var placed), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.NaturalGroundRequired));
		Assert.That<BuildingInstanceState[]>(placed, (IResolveConstraint)(object)Is.Empty);
		Assert.That<BuildingInstanceState[]>(_session.Buildings.GetAll(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)countBefore), "Bei einem Fehler wird nichts gebaut …", Array.Empty<object>());
		Assert.That<int>(Wood, (IResolveConstraint)(object)Is.EqualTo((object)woodBefore), "… und nichts verbraucht.", Array.Empty<object>());
	}

	[Test]
	public void AnUnaffordableRectangleBuildsNothing()
	{
		_session.PlayerInventory.Clear();
		Stock("wood", 10);
		Assert.That<BuildingActionResult>(_service.TryPlaceBatch(BuildingBatchPlan.Floor(Origin, "building.floor", new Vector3(1f, 0f, 1f), new Vector3(3f, 0f, 3f)), _rule, out var placed), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.MissingMaterials));
		Assert.That<BuildingInstanceState[]>(placed, (IResolveConstraint)(object)Is.Empty);
		Assert.That<BuildingInstanceState[]>(_session.Buildings.GetAll(), (IResolveConstraint)(object)Is.Empty);
		Assert.That<int>(Wood, (IResolveConstraint)(object)Is.EqualTo((object)10));
	}

	[Test]
	public void ARectangleOverAnExistingFloorIsRejectedAsAWhole()
	{
		Assert.That<BuildingActionResult>(_service.TryPlace(new BuildingPlacementCandidate("building.floor", new Vector3(2f, 0f, 2f), 0), _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		int countBefore = _session.Buildings.GetAll().Length;
		Assert.That<BuildingActionResult>(_service.TryPlaceBatch(BuildingBatchPlan.Floor(Origin, "building.floor", new Vector3(1f, 0f, 1f), new Vector3(3f, 0f, 3f)), _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Overlap));
		Assert.That<BuildingInstanceState[]>(_session.Buildings.GetAll(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)countBefore));
	}

	[Test]
	public void ObjectsAreNotAcceptedAsABatch()
	{
		Assert.That<BuildingActionResult>(_service.TryPlaceBatch(new BuildingPlacementCandidate[1]
		{
			new BuildingPlacementCandidate("building.workbench", new Vector3(2f, 0f, 2f), 0)
		}, _rule, out var _), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.UnknownBuilding), "Objekte werden einzeln gesetzt.", Array.Empty<object>());
	}

	[Test]
	public void AnEmptyBatchDoesNothing()
	{
		Assert.That<BuildingActionResult>(_service.TryPlaceBatch(Array.Empty<BuildingPlacementCandidate>(), _rule, out var placed), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.UnknownBuilding));
		Assert.That<BuildingInstanceState[]>(placed, (IResolveConstraint)(object)Is.Empty);
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
