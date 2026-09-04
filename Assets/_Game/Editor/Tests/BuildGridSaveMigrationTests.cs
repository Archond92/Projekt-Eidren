using Eidren.Core.BuildGrid;
using Eidren.Core.Services;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class BuildGridSaveMigrationTests
{
	[Test]
	public void TheCurrentVersionFollowsTheGridVersion()
	{
		Assert.That<int>(new SaveGameData().saveVersion, (IResolveConstraint)(object)Is.EqualTo((object)15));
		Assert.That<int>(11, (IResolveConstraint)(object)Is.GreaterThan((object)10), "§17: Jede Formataenderung erhoeht die Version.", Array.Empty<object>());
	}

	[Test]
	public void AnOldObjectLandsOnTheNearestCell()
	{
		Assert.That<bool>(SaveGameMigration.TryMigrate(LegacySave(Legacy("building.workbench", 3.4f, 2.6f, 0)), out var result, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		SaveBuildingData obj = result.buildings[0];
		Assert.That<int>(obj.cellX, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(obj.cellZ, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(obj.placementKind, (IResolveConstraint)(object)Is.EqualTo((object)0));
	}

	[Test]
	public void AnOldFloorKeepsItsLayer()
	{
		SaveGameMigration.TryMigrate(LegacySave(Legacy("building.floor", -2.2f, 5.5f, 0)), out var result, out var _);
		Assert.That<int>(result.buildings[0].placementKind, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(result.buildings[0].cellX, (IResolveConstraint)(object)Is.EqualTo((object)(-2)));
		Assert.That<int>(result.buildings[0].cellZ, (IResolveConstraint)(object)Is.EqualTo((object)6), "Ein genaues .5 geht nach oben — deterministisch.", Array.Empty<object>());
	}

	[TestCase(0, GridEdgeOrientation.EastWest)]
	[TestCase(1, GridEdgeOrientation.NorthSouth)]
	[TestCase(2, GridEdgeOrientation.EastWest)]
	[TestCase(3, GridEdgeOrientation.NorthSouth)]
	public void AnOldWallLandsOnTheEdgeMatchingItsRotation(int quarterTurns, GridEdgeOrientation expected)
	{
		SaveGameMigration.TryMigrate(LegacySave(Legacy("building.wall", 4f, 4f, quarterTurns)), out var result, out var _);
		Assert.That<int>(result.buildings[0].placementKind, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(result.buildings[0].edgeOrientation, (IResolveConstraint)(object)Is.EqualTo((object)(int)expected));
	}

	[Test]
	public void AnOldDoorIsAnEdgeToo()
	{
		SaveGameMigration.TryMigrate(LegacySave(Legacy("building.door", 1f, 1f, 1)), out var result, out var _);
		Assert.That<int>(result.buildings[0].placementKind, (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	[Test]
	public void NoStableIdIsRenamedByTheMigration()
	{
		SaveGameData saveGameData = LegacySave(Legacy("building.workbench", 1f, 1f, 0), Legacy("building.wall", 2f, 2f, 1), Legacy("building.floor", 3f, 3f, 0));
		string[] instanceIds = saveGameData.buildings.Select((SaveBuildingData value) => value.instanceId).ToArray();
		string[] buildingIds = saveGameData.buildings.Select((SaveBuildingData value) => value.buildingId).ToArray();
		SaveGameMigration.TryMigrate(saveGameData, out var result, out var _);
		Assert.That<IEnumerable<string>>(result.buildings.Select((SaveBuildingData value) => value.instanceId), (IResolveConstraint)(object)Is.EqualTo((object)instanceIds));
		Assert.That<IEnumerable<string>>(result.buildings.Select((SaveBuildingData value) => value.buildingId), (IResolveConstraint)(object)Is.EqualTo((object)buildingIds));
	}

	[Test]
	public void MigratingTwiceChangesNothing()
	{
		SaveGameMigration.TryMigrate(LegacySave(Legacy("building.workbench", 3.4f, 2.6f, 0)), out var once, out var error);
		int cellX = once.buildings[0].cellX;
		int cellZ = once.buildings[0].cellZ;
		Assert.That<bool>(SaveGameMigration.TryMigrate(once, out var twice, out error), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(twice.buildings[0].cellX, (IResolveConstraint)(object)Is.EqualTo((object)cellX));
		Assert.That<int>(twice.buildings[0].cellZ, (IResolveConstraint)(object)Is.EqualTo((object)cellZ));
		Assert.That<int>(twice.saveVersion, (IResolveConstraint)(object)Is.EqualTo((object)15));
	}

	[Test]
	public void ASaveFromTheFutureIsRefusedInsteadOfGuessed()
	{
		SaveGameData saveGameData = LegacySave();
		saveGameData.saveVersion = 16;
		Assert.That<bool>(SaveGameMigration.TryMigrate(saveGameData, out var _, out var error), (IResolveConstraint)(object)Is.False);
		Assert.That<string>(error, (IResolveConstraint)(object)Does.Contain("newer"));
	}

	[Test]
	public void TheGridCoordinateSurvivesSaveAndLoad()
	{
		BuildingInstanceState state = new BuildingInstanceState("instance-1", "building.farm_plot", new Vector3(7f, 1.25f, -4f), 3, 1, 2, 5);
		SaveBuildingData[] array = SaveBuildingMapper.ToSave(new BuildingInstanceState[1] { state });
		Assert.That<int>(array[0].cellX, (IResolveConstraint)(object)Is.EqualTo((object)7));
		Assert.That<int>(array[0].cellZ, (IResolveConstraint)(object)Is.EqualTo((object)(-4)));
		BuildingInstanceState[] array2 = SaveBuildingMapper.ToRuntime(array, Content(), null);
		Assert.That<BuildingInstanceState[]>(array2, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		Assert.That<string>(array2[0].InstanceId, (IResolveConstraint)(object)Is.EqualTo((object)"instance-1"));
		Assert.That<Vector3>(array2[0].Position, (IResolveConstraint)(object)Is.EqualTo((object)new Vector3(7f, 1.25f, -4f)));
		Assert.That<int>(array2[0].QuarterTurns, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(array2[0].PlantedSlots, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(array2[0].ReadyOutput, (IResolveConstraint)(object)Is.EqualTo((object)5));
	}

	[Test]
	public void NoOccupancyOrRoomListIsStored()
	{
		string[] fields = (from field in typeof(SaveBuildingData).GetFields()
			select field.Name.ToLowerInvariant()).ToArray();
		string[] array = new string[4] { "occupied", "room", "roof", "connection" };
		foreach (string forbidden in array)
		{
			Assert.That<bool>(fields.Any((string name) => name.Contains(forbidden)), (IResolveConstraint)(object)Is.False, "Abgeleitetes '" + forbidden + "' gehoert nicht in den Stand.", Array.Empty<object>());
		}
	}

	private static ContentDatabase Content()
	{
		return new GameObject("SaveMigrationContent").AddComponent<ContentDatabase>();
	}

	private static SaveBuildingData Legacy(string buildingId, float x, float z, int quarterTurns)
	{
		return new SaveBuildingData
		{
			instanceId = $"{buildingId}-{x}-{z}",
			buildingId = buildingId,
			positionX = x,
			positionY = 0f,
			positionZ = z,
			quarterTurns = quarterTurns,
			level = 1
		};
	}

	private static SaveGameData LegacySave(params SaveBuildingData[] buildings)
	{
		return new SaveGameData
		{
			saveVersion = 9,
			buildings = buildings
		};
	}
}
}
