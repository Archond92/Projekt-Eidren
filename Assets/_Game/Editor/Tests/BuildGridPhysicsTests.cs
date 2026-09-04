using Eidren.Core.BuildGrid;
using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class BuildGridPhysicsTests
{
	private const string Folder = "Assets/_Game/Data/Buildings";

	[Test]
	public void AFloorLiesFlatAndDoesNotBlockNavigation()
	{
		var (collider, obstacle) = Physics("Floor");
		Assert.That<float>(collider.size.x, (IResolveConstraint)(object)Is.EqualTo((object)1f).Within((object)0.001f));
		Assert.That<float>(collider.size.z, (IResolveConstraint)(object)Is.EqualTo((object)1f).Within((object)0.001f));
		Assert.That<float>(collider.size.y, (IResolveConstraint)(object)Is.LessThan((object)0.5f), "Boden liegt, er steht nicht (M10.4).", Array.Empty<object>());
		Assert.That<bool>(obstacle == null || !obstacle.enabled, (IResolveConstraint)(object)Is.True, "Auf Boden laeuft man.", Array.Empty<object>());
	}

	[Test]
	public void AWallBlocksOnlyItsNarrowEdge()
	{
		var (collider, navMeshObstacle) = Physics("Wall");
		Assert.That<float>(collider.size.x, (IResolveConstraint)(object)Is.EqualTo((object)1f).Within((object)0.001f), "Einen Meter lang, entlang ihrer Kante.", Array.Empty<object>());
		Assert.That<float>(collider.size.z, (IResolveConstraint)(object)Is.LessThan((object)0.5f), "Und schmal quer dazu.", Array.Empty<object>());
		Assert.That<NavMeshObstacle>(navMeshObstacle, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(navMeshObstacle.enabled, (IResolveConstraint)(object)Is.True);
		Assert.That<float>(navMeshObstacle.size.z, (IResolveConstraint)(object)Is.EqualTo((object)collider.size.z).Within((object)0.001f), "Hindernis und Collider beschreiben dasselbe Stueck.", Array.Empty<object>());
	}

	[Test]
	public void ADoorIsAWalkableOpening()
	{
		var (collider, obstacle) = Physics("Door");
		Assert.That<float>(collider.size.z, (IResolveConstraint)(object)Is.LessThan((object)0.5f), "Die Tuer sitzt auf derselben schmalen Kante wie die Wand.", Array.Empty<object>());
		Assert.That<bool>(obstacle == null || !obstacle.enabled, (IResolveConstraint)(object)Is.True, "Sie bleibt fuer den Spieler begehbar.", Array.Empty<object>());
	}

	[Test]
	public void AnObjectColliderMatchesItsRealFootprint()
	{
		BoxCollider item = Physics("FarmPlot").Item1;
		Assert.That<float>(item.size.x, (IResolveConstraint)(object)Is.EqualTo((object)2f).Within((object)0.001f));
		Assert.That<float>(item.size.z, (IResolveConstraint)(object)Is.EqualTo((object)2f).Within((object)0.001f));
		BoxCollider item2 = Physics("Workbench").Item1;
		Assert.That<float>(item2.size.x, (IResolveConstraint)(object)Is.EqualTo((object)1f).Within((object)0.001f));
		Assert.That<float>(item2.size.z, (IResolveConstraint)(object)Is.EqualTo((object)1f).Within((object)0.001f));
	}

	[Test]
	public void EveryPlanCarriesItsSizeInThePrefab()
	{
		string[] array = new string[11]
		{
			"Workbench", "StorageChest", "FarmPlot", "Wall", "Floor", "Door", "Smelter", "Sawmill", "Ropewalk", "Stonecutter",
			"CookingPot"
		};
		foreach (string name in array)
		{
			BoxCollider item = Physics(name).Item1;
			Assert.That<float>(item.size.x, (IResolveConstraint)(object)Is.GreaterThan((object)0f), name, Array.Empty<object>());
			Assert.That<float>(item.size.z, (IResolveConstraint)(object)Is.GreaterThan((object)0f), name, Array.Empty<object>());
		}
	}

	[Test]
	public void ThePrefabObstacleFollowsTheAuthoredData()
	{
		string[] array = new string[11]
		{
			"Workbench", "StorageChest", "FarmPlot", "Wall", "Floor", "Door", "Smelter", "Sawmill", "Ropewalk", "Stonecutter",
			"CookingPot"
		};
		foreach (string name in array)
		{
			NavMeshObstacle item = Physics(name).Item2;
			Assert.That<NavMeshObstacle>(item, (IResolveConstraint)(object)Is.Not.Null, name, Array.Empty<object>());
			Assert.That<bool>(item.enabled, (IResolveConstraint)(object)Is.EqualTo((object)Plan(name).BlocksNavigation), name, Array.Empty<object>());
		}
	}

	[Test]
	public void OnlyFloorAndDoorAreWalkable()
	{
		Assert.That<bool>(Plan("Floor").BlocksNavigation, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(Plan("Door").BlocksNavigation, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(Plan("Wall").BlocksNavigation, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(Plan("Workbench").BlocksNavigation, (IResolveConstraint)(object)Is.True);
	}

	[TestCase(0, 0f, 0.5f)]
	[TestCase(1, 0.5f, 0f)]
	[TestCase(2, 0f, 0.5f)]
	[TestCase(3, 0.5f, 0f)]
	public void AnEdgePieceSitsOnItsEdge_NotInTheCellCentre(int quarterTurns, float expectedX, float expectedZ)
	{
		Vector3 vector = BuildingInstanceView.EdgeOffset(Plan("Wall"), quarterTurns);
		Assert.That<float>(vector.x, (IResolveConstraint)(object)Is.EqualTo((object)expectedX).Within((object)0.001f));
		Assert.That<float>(vector.z, (IResolveConstraint)(object)Is.EqualTo((object)expectedZ).Within((object)0.001f));
	}

	[Test]
	public void HalfATurnMeansTheSameEdge_NotTheNeighbourCell()
	{
		Assert.That<Vector3>(BuildingInstanceView.EdgeOffset(Plan("Wall"), 0), (IResolveConstraint)(object)Is.EqualTo((object)BuildingInstanceView.EdgeOffset(Plan("Wall"), 2)), "Sonst wanderte eine gedrehte Wand auf die Nachbarkante.", Array.Empty<object>());
	}

	[Test]
	public void FloorsAndObjectsAreNotOffset()
	{
		Assert.That<Vector3>(BuildingInstanceView.EdgeOffset(Plan("Floor"), 0), (IResolveConstraint)(object)Is.EqualTo((object)Vector3.zero));
		Assert.That<Vector3>(BuildingInstanceView.EdgeOffset(Plan("Workbench"), 1), (IResolveConstraint)(object)Is.EqualTo((object)Vector3.zero));
	}

	[Test]
	public void TheOffsetMatchesTheRuleOrientation()
	{
		Assert.That<GridEdgeOrientation>(BuildGridOrigin.OrientationFor(0), (IResolveConstraint)(object)Is.EqualTo((object)GridEdgeOrientation.EastWest));
		Assert.That<float>(BuildingInstanceView.EdgeOffset(Plan("Wall"), 0).z, (IResolveConstraint)(object)Is.GreaterThan((object)0f), "Ost-West verlaeuft entlang X und versetzt in Z.", Array.Empty<object>());
	}

	[TestCase("Wall")]
	[TestCase("Door")]
	public void AnEdgePrefabCarriesItsConnectionPieces(string assetName)
	{
		GameObject prefab = Prefab(assetName);
		Assert.That<WallConnectionView>(prefab.GetComponent<WallConnectionView>(), (IResolveConstraint)(object)Is.Not.Null, "Ohne Presenter bleibt die abgeleitete Form unsichtbar.", Array.Empty<object>());
		string[] array = new string[4] { "Joint_MinusX", "Joint_PlusX", "Cap_MinusX", "Cap_PlusX" };
		foreach (string pieceName in array)
		{
			Transform transform = prefab.transform.Find(pieceName);
			Assert.That<Transform>(transform, (IResolveConstraint)(object)Is.Not.Null, pieceName, Array.Empty<object>());
			Assert.That<MeshRenderer>(transform.GetComponent<MeshRenderer>(), (IResolveConstraint)(object)Is.Not.Null, pieceName, Array.Empty<object>());
		}
	}

	[TestCase("Wall")]
	[TestCase("Door")]
	public void ConnectionPiecesSitAtTheEndsAndCarryNoCollider(string assetName)
	{
		GameObject prefab = Prefab(assetName);
		string[] array = new string[4] { "Joint_MinusX", "Joint_PlusX", "Cap_MinusX", "Cap_PlusX" };
		foreach (string pieceName in array)
		{
			Transform transform = prefab.transform.Find(pieceName);
			Assert.That<float>(Mathf.Abs(transform.localPosition.x), (IResolveConstraint)(object)Is.EqualTo((object)0.5f).Within((object)0.001f), pieceName + " gehoert an ein Ende der Kante.", Array.Empty<object>());
			Assert.That<float>(transform.localPosition.z, (IResolveConstraint)(object)Is.EqualTo((object)0f).Within((object)0.001f), pieceName, Array.Empty<object>());
			Assert.That<Collider>(transform.GetComponent<Collider>(), (IResolveConstraint)(object)Is.Null, pieceName + ": Praesentation aendert keine Belegung (Abschnitt 14).", Array.Empty<object>());
			Assert.That<NavMeshObstacle>(transform.GetComponent<NavMeshObstacle>(), (IResolveConstraint)(object)Is.Null, pieceName, Array.Empty<object>());
		}
	}

	[Test]
	public void TheJointFillsTheCornerAndTheCapStaysInsideItsCell()
	{
		GameObject gameObject = Prefab("Wall");
		Vector3 joint = gameObject.transform.Find("Joint_PlusX").localScale;
		Vector3 localScale = gameObject.transform.Find("Cap_PlusX").localScale;
		Assert.That<float>(joint.x, (IResolveConstraint)(object)Is.EqualTo((object)joint.z).Within((object)0.001f), "Ein Knotenpfosten ist quadratisch, sonst passt er nur in eine der beiden Richtungen.", Array.Empty<object>());
		Assert.That<float>(localScale.x, (IResolveConstraint)(object)Is.LessThan((object)joint.x), "Die Endkappe schliesst ab, statt in die Nachbarzelle zu ragen.", Array.Empty<object>());
	}

	[TestCase(0, true)]
	[TestCase(1, false)]
	[TestCase(2, false)]
	[TestCase(3, true)]
	public void LocalPlusXFollowsTheEdgeDirection(int quarterTurns, bool pointsToUpperNode)
	{
		Assert.That<bool>(WallConnectionView.LocalXPointsToUpperNode(quarterTurns), (IResolveConstraint)(object)Is.EqualTo((object)pointsToUpperNode));
	}

	[Test]
	public void TheAxisMappingSurvivesFullTurns()
	{
		for (int turns = -8; turns <= 8; turns++)
		{
			Assert.That<bool>(WallConnectionView.LocalXPointsToUpperNode(turns), (IResolveConstraint)(object)Is.EqualTo((object)WallConnectionView.LocalXPointsToUpperNode((turns % 4 + 4) % 4)), turns.ToString(), Array.Empty<object>());
		}
	}

	private static GameObject Prefab(string assetName)
	{
		Assert.That<bool>(Plan(assetName).TryGetLevel(1, out var _, out var prefab), (IResolveConstraint)(object)Is.True, assetName, Array.Empty<object>());
		return prefab;
	}

	private static BuildingCostDefinition Plan(string assetName)
	{
		BuildingCostDefinition buildingCostDefinition = AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>("Assets/_Game/Data/Buildings/" + assetName + ".asset");
		Assert.That<BuildingCostDefinition>(buildingCostDefinition, (IResolveConstraint)(object)Is.Not.Null, assetName, Array.Empty<object>());
		return buildingCostDefinition;
	}

	private static (BoxCollider, NavMeshObstacle) Physics(string assetName)
	{
		Assert.That<bool>(Plan(assetName).TryGetLevel(1, out var _, out var prefab), (IResolveConstraint)(object)Is.True, assetName, Array.Empty<object>());
		BoxCollider componentInChildren = prefab.GetComponentInChildren<BoxCollider>(includeInactive: true);
		Assert.That<BoxCollider>(componentInChildren, (IResolveConstraint)(object)Is.Not.Null, assetName + " ohne Collider.", Array.Empty<object>());
		return (componentInChildren, prefab.GetComponentInChildren<NavMeshObstacle>(includeInactive: true));
	}
}
}
