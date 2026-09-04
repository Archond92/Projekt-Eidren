using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Linq;
using System;
using UnityEditor;

namespace Eidren.Tests.EditMode
{
public sealed class PlayerProgressionServiceTests
{
	private const string CurvePath = "Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset";

	private ProgressionCurveDefinition _curve;

	private PlayerProgressionService _service;

	[SetUp]
	public void SetUp()
	{
		_curve = AssetDatabase.LoadAssetAtPath<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
		Assert.That<ProgressionCurveDefinition>(_curve, (IResolveConstraint)(object)Is.Not.Null, "Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset", Array.Empty<object>());
		_service = new PlayerProgressionService(_curve);
	}

	[Test]
	public void Curve_IsValidAndUsesV02Anchors()
	{
		Assert.That<string[]>(_curve.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty);
		Assert.That<int>(ExperienceRules.ExperienceToNextLevel(1), (IResolveConstraint)(object)Is.EqualTo((object)175));
		Assert.That<int>(ExperienceRules.Award(ExperienceSource.Recipe, 0, firstCompletion: true), (IResolveConstraint)(object)Is.EqualTo((object)25));
		Assert.That<int>(_curve.GaronMinimumLevel, (IResolveConstraint)(object)Is.EqualTo((object)12));
	}


	[Test]
	public void FirstRecipeAndBuilding_AwardDiscoveryThenRepeatAmount()
	{
		int num = _service.RecordRecipeCrafted("recipe.test");
		int repeatCraft = _service.RecordRecipeCrafted("recipe.test");
		int firstBuilding = _service.RecordBuildingConstructed("building.test");
		int repeatBuilding = _service.RecordBuildingConstructed("building.test");
		Assert.That<int>(num, (IResolveConstraint)(object)Is.EqualTo((object)25));
		Assert.That<int>(repeatCraft, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(firstBuilding, (IResolveConstraint)(object)Is.EqualTo((object)40));
		Assert.That<int>(repeatBuilding, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void EachCompletedNodeAwardsOnceRegardlessOfYield()
	{
		ZoneDefinition home = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/Zone_HomeBase.asset");
		int units = home.ResourceAllocations.Sum((ZoneResourceAllocation value) => value.Count) + home.SideNodeAllocations.Sum((ZoneResourceAllocation value) => value.Count);
		for (int index = 0; index < units; index++)
		{
			_service.RecordResourceNodeCompleted(0);
		}
		Assert.That<int>(units, (IResolveConstraint)(object)Is.EqualTo((object)58));
		Assert.That<int>(_service.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(_service.State.Experience, (IResolveConstraint)(object)Is.EqualTo((object)64));
	}

	[Test]
	public void GaronGate_OpensAtLevelTwelveAndNotBefore()
	{
		_service.RecordEnemyDefeated(4995);
		Assert.That<int>(_service.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)11));
		Assert.That<bool>(_service.CanChallengeGaron, (IResolveConstraint)(object)Is.False);
		_service.RecordEnemyDefeated(5);
		Assert.That<int>(_service.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)12));
		Assert.That<bool>(_service.CanChallengeGaron, (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void OneHourCriticalPathBudget_ReachesGaronGate()
	{
		_service.RecordEnemyDefeated(3600);
		for (int index = 0; index < 30; index++)
		{
			_service.RecordResourceNodeCompleted(0);
		}
		for (int i = 0; i < 12; i++)
		{
			_service.RecordRecipeCrafted($"pacing.recipe.{i}");
		}
		for (int j = 0; j < 8; j++)
		{
			_service.RecordBuildingConstructed($"pacing.building.{j}");
		}
		for (int k = 0; k < 9; k++)
		{
			_service.RecordEnemyDefeated(60);
		}
		Assert.That<int>(_service.State.Level, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)12));
		Assert.That<bool>(_service.CanChallengeGaron, (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void ExperienceGained_ReportsAcceptedAmountExactlyOnce()
	{
		int calls = 0;
		int reported = 0;
		_service.ExperienceGained += delegate(int amount)
		{
			calls++;
			reported += amount;
		};
		Assert.That<int>(_service.RecordEnemyDefeated(73), (IResolveConstraint)(object)Is.EqualTo((object)73));
		Assert.That<int>(calls, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(reported, (IResolveConstraint)(object)Is.EqualTo((object)73));
	}

	[Test]
	public void MaximumLevel_DiscardsOverflowAndStopsAllFurtherXp()
	{
		_service.RecordEnemyDefeated(100000);
		Assert.That<int>(_service.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)24));
		Assert.That<int>(_service.State.Experience, (IResolveConstraint)(object)Is.EqualTo((object)1450));
		Assert.That<int>(_service.State.AvailableTechnologyPoints, (IResolveConstraint)(object)Is.EqualTo((object)24));
		Assert.That<int>(_service.RecordEnemyDefeated(50), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_service.State.Experience, (IResolveConstraint)(object)Is.EqualTo((object)1450));
	}

	[Test]
	public void EnemyAndBlueprintContractsArePersistentAndDeterministic()
	{
		Assert.That<int>(_service.RecordEnemyDefeated(15), (IResolveConstraint)(object)Is.EqualTo((object)15));
		Assert.That<int>(_service.RecordEnemyDefeated(350, firstGaronVictory: true), (IResolveConstraint)(object)Is.EqualTo((object)850));
		Assert.That<bool>(_service.LearnBlueprint("blueprint.copper_spear"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_service.LearnBlueprint("blueprint.copper_spear"), (IResolveConstraint)(object)Is.False);
		PlayerProgressionRuntimeState state = _service.Capture();
		PlayerProgressionService playerProgressionService = new PlayerProgressionService(_curve);
		playerProgressionService.Restore(state);
		Assert.That<bool>(playerProgressionService.KnowsBlueprint("blueprint.copper_spear"), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(playerProgressionService.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)_service.State.Level));
		Assert.That<int>(playerProgressionService.State.Experience, (IResolveConstraint)(object)Is.EqualTo((object)_service.State.Experience));
	}

	[Test]
	public void OnlyDungeonCompletionCanAdvanceToStageTwo()
	{
		_service.RecordEnemyDefeated(100000);
		_service.TrySpendTechnologyPoints(10);
		_service.RecordRecipeCrafted("recipe.test");
		_service.RecordBuildingConstructed("building.test");
		Assert.That<int>(_service.State.Stage, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(_service.TryAdvanceAfterDungeonCompletion(string.Empty), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(_service.TryAdvanceAfterDungeonCompletion("dungeon.future.first"), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(_service.State.Stage, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(_service.State.MaximumLevel, (IResolveConstraint)(object)Is.EqualTo((object)40));
	}
}
}
