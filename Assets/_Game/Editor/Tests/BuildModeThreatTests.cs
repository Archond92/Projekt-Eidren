using Eidren.AI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System;

namespace Eidren.Tests.EditMode
{
public sealed class BuildModeThreatTests
{
	[TestCase(EnemyState.Alert)]
	[TestCase(EnemyState.Chase)]
	[TestCase(EnemyState.ChooseAttack)]
	[TestCase(EnemyState.Telegraph)]
	[TestCase(EnemyState.ExecuteAttack)]
	[TestCase(EnemyState.Recover)]
	[TestCase(EnemyState.Staggered)]
	public void AnEngagedEnemyIsAThreat(EnemyState state)
	{
		Assert.That<bool>(EnemyThreat.IsEngaged(state), (IResolveConstraint)(object)Is.True);
	}

	[TestCase(EnemyState.Idle)]
	[TestCase(EnemyState.Patrol)]
	[TestCase(EnemyState.Dead)]
	public void AnUnengagedEnemyIsNoThreat(EnemyState state)
	{
		Assert.That<bool>(EnemyThreat.IsEngaged(state), (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void AReturningEnemyEndsTheThreat()
	{
		Assert.That<bool>(EnemyThreat.IsEngaged(EnemyState.Return), (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void EveryEnemyStateIsClassified()
	{
		EnemyState[] array = (EnemyState[])Enum.GetValues(typeof(EnemyState));
		foreach (EnemyState state in array)
		{
			bool num = EnemyThreat.IsEngaged(state);
			bool peaceful = state == EnemyState.Idle || state == EnemyState.Patrol || state == EnemyState.Return || state == EnemyState.Dead;
			Assert.That<bool>(num, (IResolveConstraint)(object)Is.EqualTo((object)(!peaceful)), $"'{state}' ist weder ausdrücklich Kampf noch " + "ausdrücklich Ruhe.", Array.Empty<object>());
		}
	}
}
}
