using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class BuildingPlacementDataTests
{
	private const string Folder = "Assets/_Game/Data/Buildings";

	[TestCase("Workbench", BuildingPlacementKind.Object, BuildingGroundRule.GroundOrFloor)]
	[TestCase("StorageChest", BuildingPlacementKind.Object, BuildingGroundRule.GroundOrFloor)]
	[TestCase("Smelter", BuildingPlacementKind.Object, BuildingGroundRule.RequiresFloor)]
	[TestCase("Sawmill", BuildingPlacementKind.Object, BuildingGroundRule.RequiresFloor)]
	[TestCase("Ropewalk", BuildingPlacementKind.Object, BuildingGroundRule.RequiresFloor)]
	[TestCase("Stonecutter", BuildingPlacementKind.Object, BuildingGroundRule.RequiresFloor)]
	[TestCase("FarmPlot", BuildingPlacementKind.Object, BuildingGroundRule.GroundOnly)]
	[TestCase("CookingPot", BuildingPlacementKind.Object, BuildingGroundRule.GroundOrFloor)]
	[TestCase("Floor", BuildingPlacementKind.Floor, BuildingGroundRule.GroundOnly)]
	[TestCase("Wall", BuildingPlacementKind.Edge, BuildingGroundRule.GroundOrFloor)]
	[TestCase("Door", BuildingPlacementKind.Edge, BuildingGroundRule.GroundOrFloor)]
	public void AuthoredPlan_CarriesItsPlacementKindAndGroundRule(string assetName, BuildingPlacementKind expectedKind, BuildingGroundRule expectedRule)
	{
		BuildingCostDefinition buildingCostDefinition = Load(assetName);
		Assert.That<BuildingPlacementKind>(buildingCostDefinition.PlacementKind, (IResolveConstraint)(object)Is.EqualTo((object)expectedKind));
		Assert.That<BuildingGroundRule>(buildingCostDefinition.GroundRule, (IResolveConstraint)(object)Is.EqualTo((object)expectedRule));
	}

	[Test]
	public void EveryAuthoredPlan_IsValidOnDisk()
	{
		BuildingCostDefinition[] array = All();
		foreach (BuildingCostDefinition plan in array)
		{
			Assert.That<bool>(plan.TryValidate(out var error), (IResolveConstraint)(object)Is.True, plan.name + ": " + error, Array.Empty<object>());
		}
	}

	[Test]
	public void OnlyObjectPlans_OccupyABuildField()
	{
		BuildingCostDefinition[] array = All();
		foreach (BuildingCostDefinition plan in array)
		{
			Assert.That<bool>(plan.OccupiesBuildField, (IResolveConstraint)(object)Is.EqualTo((object)(plan.PlacementKind == BuildingPlacementKind.Object)), plan.name + " koppelt Baufeld und Platzierungsart nicht.", Array.Empty<object>());
		}
	}

	[Test]
	public void NoPlanUsesOutdoorOnly_ItIsAPreparedOptionOnly()
	{
		BuildingCostDefinition[] array = All();
		foreach (BuildingCostDefinition plan in array)
		{
			Assert.That<BuildingGroundRule>(plan.GroundRule, (IResolveConstraint)(object)Is.Not.EqualTo((object)BuildingGroundRule.OutdoorOnly), plan.name + ": Auftrag 01 fuegt kein Gebaeude dieser Art hinzu.", Array.Empty<object>());
		}
	}

	[Test]
	public void EdgePlans_CoverExactlyWallAndDoor()
	{
		List<string> edgePlans = new List<string>();
		BuildingCostDefinition[] array = All();
		foreach (BuildingCostDefinition plan in array)
		{
			if (plan.PlacementKind == BuildingPlacementKind.Edge)
			{
				edgePlans.Add(plan.Id);
			}
		}
		Assert.That<List<string>>(edgePlans, (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[2] { "building.wall", "building.door" }));
	}

	[Test]
	public void FloorPlan_WithAnyOtherGroundRule_IsRejected()
	{
		BuildingCostDefinition copy = Clone("Floor");
		try
		{
			SetPlacement(copy, BuildingPlacementKind.Floor, BuildingGroundRule.RequiresFloor);
			Assert.That<bool>(copy.TryValidate(out var error), (IResolveConstraint)(object)Is.False);
			Assert.That<string>(error, (IResolveConstraint)(object)Does.Contain("GroundOnly"));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(copy);
		}
	}

	[Test]
	public void EdgePlan_WithACellBasedGroundRule_IsRejected()
	{
		BuildingCostDefinition copy = Clone("Wall");
		try
		{
			SetPlacement(copy, BuildingPlacementKind.Edge, BuildingGroundRule.RequiresFloor);
			Assert.That<bool>(copy.TryValidate(out var error), (IResolveConstraint)(object)Is.False);
			Assert.That<string>(error, (IResolveConstraint)(object)Does.Contain("GroundOrFloor"));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(copy);
		}
	}

	[Test]
	public void PlacementKind_AndBuildField_MayNotDrift()
	{
		BuildingCostDefinition copy = Clone("Workbench");
		try
		{
			SerializedObject serializedObject = new SerializedObject(copy);
			serializedObject.FindProperty("occupiesBuildField").boolValue = false;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			Assert.That<bool>(copy.TryValidate(out var error), (IResolveConstraint)(object)Is.False);
			Assert.That<string>(error, (IResolveConstraint)(object)Does.Contain("occupiesBuildField"));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(copy);
		}
	}

	private static BuildingCostDefinition Load(string assetName)
	{
		BuildingCostDefinition buildingCostDefinition = AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>("Assets/_Game/Data/Buildings/" + assetName + ".asset");
		Assert.That<BuildingCostDefinition>(buildingCostDefinition, (IResolveConstraint)(object)Is.Not.Null, assetName + ".asset fehlt unter Assets/_Game/Data/Buildings.", Array.Empty<object>());
		return buildingCostDefinition;
	}

	private static BuildingCostDefinition[] All()
	{
		string[] guids = AssetDatabase.FindAssets("t:BuildingCostDefinition", new string[1] { "Assets/_Game/Data/Buildings" });
		Assert.That<int>(guids.Length, (IResolveConstraint)(object)Is.EqualTo((object)11), "v0.1 kennt elf Baupläne; Auftrag 01 fügt keinen hinzu.", Array.Empty<object>());
		BuildingCostDefinition[] plans = new BuildingCostDefinition[guids.Length];
		for (int index = 0; index < guids.Length; index++)
		{
			plans[index] = AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>(AssetDatabase.GUIDToAssetPath(guids[index]));
		}
		return plans;
	}

	private static BuildingCostDefinition Clone(string assetName)
	{
		return UnityEngine.Object.Instantiate(Load(assetName));
	}

	private static void SetPlacement(BuildingCostDefinition plan, BuildingPlacementKind kind, BuildingGroundRule rule)
	{
		SerializedObject serializedObject = new SerializedObject(plan);
		serializedObject.FindProperty("placementKind").enumValueIndex = (int)kind;
		serializedObject.FindProperty("groundRule").enumValueIndex = (int)rule;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
	}
}
}
