using Eidren.Core.BuildGrid;
using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class BuildingLayerPlacementTests
{
	private GameObject _root;

	private ContentDatabase _content;

	private GameSession _session;

	private PlayerProgressionService _progression;

	private TechnologyUnlockService _technology;

	private BuildingService _service;

	private IBuildingPlacementRule _rule;

	private const BuildingActionResult Ok = BuildingActionResult.Success;

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("BuildingLayerPlacementTests");
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
		_rule = new BuildingPlacementRule(_content, new BuildingPlacementArea(new Rect(-20f, -20f, 40f, 40f), Array.Empty<Rect>(), new BuildGridOrigin(0f, 0f)));
		UnlockAll();
		AddMaterials();
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_root);
	}

	[Test]
	public void Floor_Object_AndEnclosingWalls_ShareTheSameCell()
	{
		Assert.That<BuildingActionResult>(Place("building.floor", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.workbench", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Ein Gebaeude darf auf einem Boden stehen.", Array.Empty<object>());
		Assert.That<BuildingActionResult>(Place("building.wall", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Die Nordkante derselben Zelle bleibt frei.", Array.Empty<object>());
		Assert.That<BuildingActionResult>(Place("building.wall", 3f, 3f, 1), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Die Ostkante ebenfalls.", Array.Empty<object>());
		Assert.That<BuildingInstanceState[]>(_session.Buildings.GetAll(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)4));
	}

	[Test]
	public void SecondFloorOnTheSameCell_IsRejected()
	{
		Assert.That<BuildingActionResult>(Place("building.floor", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.floor", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Overlap));
	}

	[Test]
	public void SecondObjectOnTheSameCell_IsRejected()
	{
		Place("building.floor", 3f, 3f);
		Assert.That<BuildingActionResult>(Place("building.workbench", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.smelter", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Overlap));
	}

	[Test]
	public void SecondWallOnTheSameEdge_IsRejected()
	{
		Assert.That<BuildingActionResult>(Place("building.wall", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.wall", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.EdgeOccupied));
	}

	[Test]
	public void RotationSelectsADifferentEdgeOfTheSameCell()
	{
		Assert.That<BuildingActionResult>(Place("building.wall", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.wall", 3f, 3f, 1), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.wall", 3f, 3f, 2), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.EdgeOccupied), "Eine halbe Drehung trifft dieselbe Kante wie ohne Drehung.", Array.Empty<object>());
	}

	[Test]
	public void ADoorReplacesTheWallOnTheSameEdge_InOneAction()
	{
		Assert.That<BuildingActionResult>(_service.TryPlace(Candidate("building.wall", 3f, 3f, 0), _rule, out var wall), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.door", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<bool>(_session.Buildings.TryGet(wall.InstanceId, out var _), (IResolveConstraint)(object)Is.False, "Die Wand ist in derselben Aktion verschwunden.", Array.Empty<object>());
		Assert.That<int>(_session.Buildings.GetAll().Count((BuildingInstanceState buildingInstanceState) => buildingInstanceState.BuildingId == "building.door"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<BuildingInstanceState[]>(_session.Buildings.GetAll(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
	}

	[Test]
	public void TheReplacementIsBilledAsDemolitionPlusBuildCost()
	{
		Place("building.wall", 3f, 3f);
		int woodBefore = _session.PlayerInventory.GetTotalAmount("wood");
		Assert.That<BuildingActionResult>(Place("building.door", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		// +2 statt +1: der Wand-Abriss erstattet inzwischen die VOLLEN Kosten
		// (wood:2), nicht mehr die Haelfte — dieselbe Regel, die auch
		// BuildingServiceTests und BuildCraftAndDemolish messen. Tuer kostet 4.
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)(woodBefore + 2 - 4)));
	}

	[Test]
	public void ADoorDoesNotReplaceAnotherDoor()
	{
		Place("building.wall", 3f, 3f);
		Assert.That<BuildingActionResult>(Place("building.door", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.door", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.EdgeOccupied));
	}

	[Test]
	public void AWallDoesNotReplaceADoor()
	{
		Place("building.wall", 3f, 3f);
		Place("building.door", 3f, 3f);
		Assert.That<BuildingActionResult>(Place("building.wall", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.EdgeOccupied));
	}

	[Test]
	public void AWorkshopOnBareGround_IsRejected()
	{
		Assert.That<BuildingActionResult>(Place("building.smelter", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.FloorRequired));
	}

	[Test]
	public void WorkbenchAndChestWorkOnBothFoundations()
	{
		Assert.That<BuildingActionResult>(Place("building.workbench", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.storage_chest", 4f, 4f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.floor", 6f, 6f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.floor", 7f, 7f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.workbench", 6f, 6f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.storage_chest", 7f, 7f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
	}

	[Test]
	public void AFarmPlotOnAFloor_IsRejected()
	{
		Place("building.floor", 3f, 3f);
		Assert.That<BuildingActionResult>(Place("building.farm_plot", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.NaturalGroundRequired), "Der Acker verlangt natuerlichen Untergrund.", Array.Empty<object>());
	}

	[Test]
	public void AFarmPlotOnBareGround_IsAccepted()
	{
		Assert.That<BuildingActionResult>(Place("building.farm_plot", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
	}

	[Test]
	public void AFloorMayBeSlidUnderAnExistingBuilding()
	{
		Assert.That<BuildingActionResult>(Place("building.storage_chest", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.floor", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Die Schichten sind unabhaengig; die Reihenfolge zaehlt nicht.", Array.Empty<object>());
		Assert.That<BuildingActionResult>(Place("building.workbench", 4f, 4f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		Assert.That<BuildingActionResult>(Place("building.floor", 4f, 4f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
	}

	[Test]
	public void NoFloorSlidesUnderABuildingThatDemandsBareGround()
	{
		Assert.That<BuildingActionResult>(Place("building.farm_plot", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		(float, float)[] array = new(float, float)[4]
		{
			(3f, 3f),
			(4f, 3f),
			(3f, 4f),
			(4f, 4f)
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (x, z) = array[i];
			Assert.That<BuildingActionResult>(Place("building.floor", x, z), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.NaturalGroundRequired), $"Zelle ({x}, {z}) liegt unter dem Acker.", Array.Empty<object>());
		}
		Assert.That<BuildingActionResult>(Place("building.floor", 6f, 6f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
	}

	[Test]
	public void TheCookingPotWorksOnBothFoundations()
	{
		Assert.That<BuildingActionResult>(Place("building.cooking_pot", 8f, 8f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Vollstaendig auf natuerlichem Untergrund.", Array.Empty<object>());
		Place("building.floor", 3f, 3f);
		Assert.That<BuildingActionResult>(Place("building.cooking_pot", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success), "Vollstaendig auf Boden.", Array.Empty<object>());
	}

	[Test]
	public void AFarmPlotCoversFourCells_AndBlocksAllOfThem()
	{
		Assert.That<BuildingActionResult>(Place("building.farm_plot", 3f, 3f), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		(float, float)[] array = new(float, float)[4]
		{
			(3f, 3f),
			(4f, 3f),
			(3f, 4f),
			(4f, 4f)
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (x, z) = array[i];
			Assert.That<BuildingActionResult>(_service.EvaluatePlacement(Candidate("building.cooking_pot", x, z, 0), _rule), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Overlap), $"Zelle ({x}, {z}) gehoert zum Acker.", Array.Empty<object>());
		}
		Assert.That<BuildingActionResult>(_service.EvaluatePlacement(Candidate("building.cooking_pot", 5f, 5f, 0), _rule), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
	}

	private BuildingActionResult Place(string buildingId, float x, float z, int quarterTurns = 0)
	{
		BuildingInstanceState instance;
		return _service.TryPlace(Candidate(buildingId, x, z, quarterTurns), _rule, out instance);
	}

	private static BuildingPlacementCandidate Candidate(string buildingId, float x, float z, int quarterTurns)
	{
		return new BuildingPlacementCandidate(buildingId, new Vector3(x, 0f, z), quarterTurns);
	}

	private void AddMaterials()
	{
		Stock("wood", 40);
		Stock("stone", 30);
		Stock("plant_fiber", 20);
		Stock("copper_bar", 10);
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
