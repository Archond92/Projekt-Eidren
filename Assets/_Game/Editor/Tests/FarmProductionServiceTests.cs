using Eidren.Core.Services;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.IO;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class FarmProductionServiceTests
{
	private GameObject _root;

	private ContentDatabase _content;

	private GameSession _session;

	private FarmProductionService _production;

	private const string FarmId = "test.farm";

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("FarmProductionTests");
		_content = _root.AddComponent<ContentDatabase>();
		_session = _root.AddComponent<GameSession>();
		_session.Initialize();
		_session.ConfigureContentDatabase(_content);
		_session.StartNewGame();
		Assert.That<bool>(_session.Buildings.TryAdd(new BuildingInstanceState("test.farm", "building.farm_plot", Vector3.zero, 0, 1)), (IResolveConstraint)(object)Is.True);
		_production = _session.FarmProduction;
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_root);
	}

	[Test]
	public void FarmAcceptsFourSeedsAndRejectsTheFifth()
	{
		_session.PlayerInventory.Add("wheat_seed", 5);
		for (int slot = 0; slot < 4; slot++)
		{
			Assert.That<FarmActionResult>(_production.TryPlantSeed("test.farm"), (IResolveConstraint)(object)Is.EqualTo((object)FarmActionResult.Success));
		}
		Assert.That<FarmActionResult>(_production.TryPlantSeed("test.farm"), (IResolveConstraint)(object)Is.EqualTo((object)FarmActionResult.FarmFull));
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wheat_seed"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(_production.TryGetFarm("test.farm", out var state), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(state.PlantedSlots, (IResolveConstraint)(object)Is.EqualTo((object)4));
	}

	[Test]
	public void OnlyHomecomingProducesAndSeedsDoNotReturn()
	{
		_session.PlayerInventory.Add("wheat_seed", 4);
		for (int index = 0; index < 4; index++)
		{
			Assert.That<FarmActionResult>(_production.TryPlantSeed("test.farm"), (IResolveConstraint)(object)Is.EqualTo((object)FarmActionResult.Success));
		}
		AssertState(4, 0);
		Assert.That<int>(_production.AdvanceForHomecoming(), (IResolveConstraint)(object)Is.EqualTo((object)12));
		AssertState(0, 12);
		Assert.That<int>(_production.AdvanceForHomecoming(), (IResolveConstraint)(object)Is.Zero);
		AssertState(0, 12);
		Assert.That<FarmActionResult>(_production.TryHarvest("test.farm"), (IResolveConstraint)(object)Is.EqualTo((object)FarmActionResult.Success));
		AssertState(0, 0);
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wheat"), (IResolveConstraint)(object)Is.EqualTo((object)12));
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wheat_seed"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<FarmActionResult>(_production.TryPlantSeed("test.farm"), (IResolveConstraint)(object)Is.EqualTo((object)FarmActionResult.NoSeed));
	}

	[Test]
	public void SaveRoundTripPreservesProgressWithoutAdvancingIt()
	{
		_session.PlayerInventory.Add("wheat_seed", 2);
		Assert.That<FarmActionResult>(_production.TryPlantSeed("test.farm"), (IResolveConstraint)(object)Is.EqualTo((object)FarmActionResult.Success));
		Assert.That<FarmActionResult>(_production.TryPlantSeed("test.farm"), (IResolveConstraint)(object)Is.EqualTo((object)FarmActionResult.Success));
		GameSessionRuntimeState saved = _session.ExportRuntimeState();
		_session.StartNewGame();
		Assert.That<bool>(_session.TryRestoreRuntimeState(saved, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		AssertState(2, 0);
	}

	[Test]
	public void ProductionSourcesContainNoSecondClock()
	{
		string scriptRoot = Path.Combine(Application.dataPath, "_Game", "Scripts");
		string[] paths = new string[3]
		{
			Path.Combine(scriptRoot, "Core", "FarmProductionService.cs"),
			Path.Combine(scriptRoot, "Interaction", "FarmPlotController.cs"),
			Path.Combine(scriptRoot, "Core", "World", "ZoneRegenerationCoordinator.cs")
		};
		string[] forbidden = new string[4] { "Time.deltaTime", "DateTime", "UtcNow", "realtimeSinceStartup" };
		string[] array = paths;
		foreach (string path in array)
		{
			string source = File.ReadAllText(path);
			string[] array2 = forbidden;
			foreach (string token in array2)
			{
				Assert.That<string>(source, (IResolveConstraint)(object)Does.Not.Contain(token), Path.GetFileName(path) + " contains '" + token + "'.", Array.Empty<object>());
			}
		}
	}

	private void AssertState(int planted, int ready)
	{
		Assert.That<bool>(_production.TryGetFarm("test.farm", out var state), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(state.PlantedSlots, (IResolveConstraint)(object)Is.EqualTo((object)planted));
		Assert.That<int>(state.ReadyOutput, (IResolveConstraint)(object)Is.EqualTo((object)ready));
	}
}
}
