using Eidren.Core.Services;
using Eidren.Data;
using Eidren.UI;
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
public sealed class TechnologyTreeTests
{
	private TechnologyTreeDefinition _tree;

	private ProgressionCurveDefinition _curve;

	private PlayerProgressionService _progression;

	private TechnologyUnlockService _technology;

	private GameObject _databaseObject;

	private ContentDatabase _content;

	[SetUp]
	public void SetUp()
	{
		_tree = Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
		_curve = Load<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
		_progression = new PlayerProgressionService(_curve);
		_technology = new TechnologyUnlockService(_tree, _progression);
		_databaseObject = new GameObject("TechnologyTree_ContentDatabase");
		_content = _databaseObject.AddComponent<ContentDatabase>();
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_databaseObject);
	}

	[Test]
	public void V02Tree_HasStableLegacyAndNewNodeOrder()
	{
		// F31-007/011: vier stillgelegte Knoten und der separate Slot-Knoten
		// entfernt; Fanggeraet steht hinter Saegewerk und Seilerei.
		string[] expected = new string[28]
		{
			"technology.01.workbench", "technology.02.axe", "technology.03.storage_chest", "technology.04.hammer", "technology.05.scythe", "technology.06.daggers", "technology.07.structures", "technology.08.farm_plot", "technology.09.pickaxe", "technology.10.smelter",
			"technology.14.ropewalk", "technology.13.sawmill", "technology.11.catch_device", "technology.15.stonecutter", "technology.16.cooking_pot", "technology.17.healing_potion", "technology.18.t1_tools", "technology.20.cloth_hood",
			"technology.24.copper_hammer", "technology.25.copper_daggers", "technology.26.copper_spear", "technology.27.copper_armor", "technology.28.t2_processing", "technology.29.t2_tools", "technology.30.iron_hammer",
			"technology.31.iron_daggers", "technology.32.iron_spear", "technology.33.iron_armor"
		};
		Assert.That<IEnumerable<string>>(_tree.Nodes.Select((TechnologyNodeDefinition node) => node.Id), (IResolveConstraint)(object)Is.EqualTo((object)expected));
		Assert.That<bool>(_tree.Nodes.Take(22).All((TechnologyNodeDefinition node) => node.Stage == 1), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_tree.Nodes.Skip(22).All((TechnologyNodeDefinition node) => node.Stage == 2), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_tree.Nodes.All((TechnologyNodeDefinition node) => node.PointCost == ((!node.InitiallyUnlocked) ? 1 : 0)), (IResolveConstraint)(object)Is.True);
		Assert.That<IEnumerable<string>>(from node in _tree.Nodes
			where node.InitiallyUnlocked
			select node.Id, (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[3] { "technology.02.axe", "technology.05.scythe", "technology.09.pickaxe" }));
	}

	[Test]
	public void DisplayOrder_StartsWithToolsThenWorkbench()
	{
		Assert.That<string[]>((from node in _tree.Nodes.OrderBy((TechnologyNodeDefinition node) => node.SortOrder).Take(4)
			select node.Id).ToArray(), (IResolveConstraint)(object)Is.EqualTo((object)new string[4] { "technology.02.axe", "technology.05.scythe", "technology.09.pickaxe", "technology.01.workbench" }));
	}

	[Test]
	public void Tree_SelfValidationAndAllContentReferencesPass()
	{
		Assert.That<string[]>(_tree.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty);
		Assert.That<string[]>(TechnologyTreeContentValidator.GetErrors(_tree, _content, _curve), (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void Prefab_ContainsTwentyThreeAuthoredNodeViews()
	{
		GameObject gameObject = Load<GameObject>("Assets/_Game/Resources/UI/TechnologyTreeWindow.prefab");
		TechnologyTreeWindow component = gameObject.GetComponent<TechnologyTreeWindow>();
		Assert.That<TechnologyTreeWindow>(component, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<TechnologyNodeButtonView[]>(component.NodeViews, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)23));
		Assert.That<TechnologyNodeButtonView[]>(gameObject.GetComponentsInChildren<TechnologyNodeButtonView>(includeInactive: true), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)23));
	}

	[Test]
	public void ProductionChainStartsAfterFarmPlot()
	{
		AssertPrerequisite("technology.10.smelter", "technology.08.farm_plot");
		string[] array = new string[5] { "technology.13.sawmill", "technology.14.ropewalk", "technology.15.stonecutter", "technology.16.cooking_pot", "technology.18.t1_tools" };
		foreach (string branch in array)
		{
			AssertPrerequisite(branch, "technology.10.smelter");
		}
		// F31-011: Das Fanggeraet haengt hinter seinen Materialquellen.
		AssertPrerequisite("technology.11.catch_device", "technology.13.sawmill");
		AssertPrerequisite("technology.11.catch_device", "technology.14.ropewalk");
		AssertPrerequisite("technology.17.healing_potion", "technology.16.cooking_pot");
		AssertPrerequisite("technology.20.cloth_hood", "technology.14.ropewalk");
	}

	[Test]
	public void NodeWithoutPrerequisite_CannotBePurchased()
	{
		_progression.RecordEnemyDefeated(100000);
		Assert.That<TechnologyUnlockResult>(_technology.TryUnlock("technology.03.storage_chest"), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.PrerequisiteMissing));
		Assert.That<bool>(_technology.IsRecipeUnlocked("craft_axe"), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(_progression.State.AvailableTechnologyPoints, (IResolveConstraint)(object)Is.EqualTo((object)24));
	}

	[Test]
	public void V02EquipmentNodesRequireProgressFlagsAndBlueprint()
	{
		_progression.RecordEnemyDefeated(100000);
		Assert.That<TechnologyNodeState>(_technology.GetNodeState("technology.18.t1_tools"), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyNodeState.Locked));
		Assert.That<string>(_technology.GetLockReason("technology.18.t1_tools"), (IResolveConstraint)(object)Does.Contain("Garon"));
		Assert.That<TechnologyNodeState>(_technology.GetNodeState("technology.26.copper_spear"), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyNodeState.Locked));
		Assert.That<TechnologyNodeState>(_technology.GetNodeState("technology.28.t2_processing"), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyNodeState.Locked));
	}

	[Test]
	public void BuildingUsesTheSameLockedThenUnlockedTruth()
	{
		Assert.That<bool>(_technology.IsBuildingUnlocked("building.workbench"), (IResolveConstraint)(object)Is.False);
		_progression.RecordEnemyDefeated(175);
		Assert.That<int>(_progression.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<TechnologyUnlockResult>(_technology.TryUnlock("technology.01.workbench"), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success));
		Assert.That<bool>(_technology.IsBuildingUnlocked("building.workbench"), (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void EmptyTree_DoesNotBlockHandHarvesting()
	{
		ResourceNodeDefinition tree = Load<ResourceNodeDefinition>("Assets/_Game/Data/Resources/Tree.asset");
		HarvestOutcome outcome = new HarvestYieldResolver(tree.HarvestYields).Resolve(tree, null);
		Assert.That<string[]>(_technology.CaptureUnlockedNodeIds(), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[3] { "technology.02.axe", "technology.05.scythe", "technology.09.pickaxe" }));
		Assert.That<bool>(outcome.IsAllowed, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(outcome.Amount, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	private void AssertPrerequisite(string nodeId, string expected)
	{
		Assert.That<bool>(_tree.TryGetNode(nodeId, out var node), (IResolveConstraint)(object)Is.True);
		Assert.That<IReadOnlyList<string>>(node.PrerequisiteNodeIds, (IResolveConstraint)(object)Does.Contain(expected), nodeId, Array.Empty<object>());
	}

	private static T Load<T>(string path) where T : UnityEngine.Object
	{
		T val = AssetDatabase.LoadAssetAtPath<T>(path);
		Assert.That<T>(val, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		return val;
	}
}
}
