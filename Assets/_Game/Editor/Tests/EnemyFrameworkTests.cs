using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using UnityEngine.AI;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class EnemyFrameworkTests
{
	[Test]
	public void RepathPolicy_UpdatesOnlyAfterIntervalOrMeaningfulMovement()
	{
		Vector3 last = new Vector3(4f, 0f, 8f);
		Assert.That<bool>(EnemyRepathPolicy.ShouldUpdateDestination(hasDestination: false, 1f, 2f, last, last, 0.25f), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(EnemyRepathPolicy.ShouldUpdateDestination(hasDestination: true, 1f, 2f, last, last + Vector3.right * 0.49f, 0.25f), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(EnemyRepathPolicy.ShouldUpdateDestination(hasDestination: true, 1f, 2f, last, last + Vector3.right * 0.5f, 0.25f), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(EnemyRepathPolicy.ShouldUpdateDestination(hasDestination: true, 2f, 2f, last, last, 0.25f), (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void StateMachine_UsesRequiredStatesAndDeathIsTerminal()
	{
		EnemyStateMachine enemyStateMachine = new EnemyStateMachine();
		int changes = 0;
		enemyStateMachine.StateChanged += delegate
		{
			changes++;
		};
		Assert.That<EnemyState>(enemyStateMachine.CurrentState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Idle));
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.Patrol), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.Alert), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.Chase), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.ChooseAttack), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.Telegraph), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.ExecuteAttack), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.Recover), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.Staggered), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.Return), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.Dead), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(enemyStateMachine.TrySetState(EnemyState.Idle), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(enemyStateMachine.IsDead, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(changes, (IResolveConstraint)(object)Is.EqualTo((object)10));
	}

	[Test]
	public void LeashRules_UseEnemyAndTargetDistanceFromHome()
	{
		Vector3 home = new Vector3(10f, 0f, 10f);
		Assert.That<bool>(EnemyLeashRules.ShouldReturn(home, new Vector3(20f, 8f, 10f), new Vector3(10f, -5f, 20f), 18f, 2.5f), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(EnemyLeashRules.ShouldReturn(home, new Vector3(28.1f, 0f, 10f), home, 18f, 2.5f), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(EnemyLeashRules.ShouldReturn(home, home, new Vector3(10f, 0f, -10.6f), 18f, 2.5f), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(EnemyLeashRules.HasReached(home, new Vector3(10.5f, 20f, 10f), 0.65f), (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void GeneralEnemy_ActivatesChaseAndEndsInDeathState()
	{
		GameObject target = new GameObject("EnemyFrameworkTest_Target");
		GameObject enemyObject = new GameObject("EnemyFrameworkTest_Enemy");
		try
		{
			Damageable targetHealth = target.AddComponent<Damageable>();
			targetHealth.Initialize(100f);
			TestEnemyController enemy = enemyObject.AddComponent<TestEnemyController>();
			enemy.InitializeForTest(target.transform, targetHealth);
			enemy.AuthorizeCombat();
			Assert.That<EnemyState>(enemy.EnemyState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Alert));
			Assert.That<NavMeshAgent>(enemy.GetComponent<NavMeshAgent>(), (IResolveConstraint)(object)Is.SameAs((object)enemy.NavigationAgent));
			enemy.ApplyDamage(new DamageInfo(100f, 0f, enemy.transform.position, target, isBackAttack: false, "test.lethal", "test"));
			Assert.That<bool>(enemy.IsAlive, (IResolveConstraint)(object)Is.False);
			Assert.That<EnemyState>(enemy.EnemyState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Dead));
			Assert.That<bool>(enemy.NavigationAgent.enabled, (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			Object.DestroyImmediate(enemyObject);
			Object.DestroyImmediate(target);
		}
	}

	[Test]
	public void Garon_UsesEnemyFrameworkAndConfiguredNavMeshAgent()
	{
		GameObject target = new GameObject("EnemyFrameworkTest_Player");
		GameObject bossObject = new GameObject("EnemyFrameworkTest_Garon");
		BossData data = ScriptableObject.CreateInstance<BossData>();
		try
		{
			Damageable targetHealth = target.AddComponent<Damageable>();
			targetHealth.Initialize(100f);
			data.MaxHealth = 2100f;
			data.MaxStagger = 340f;
			data.StaggerDuration = 6f;
			data.Navigation = TestNavigation();
			BossController bossController = bossObject.AddComponent<BossController>();
			bossController.Initialize(data, target.transform, targetHealth, null);
			Assert.That<BossController>(bossController, (IResolveConstraint)(object)Is.InstanceOf<EnemyControllerBase>());
			Assert.That<NavMeshAgent>(bossController.NavigationAgent, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<float>(bossController.NavigationAgent.speed, (IResolveConstraint)(object)Is.EqualTo((object)2.1f));
			Assert.That<float>(bossController.NavigationAgent.stoppingDistance, (IResolveConstraint)(object)Is.EqualTo((object)8.9f));
			bossController.BeginBattle();
			Assert.That<EnemyState>(bossController.EnemyState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Alert));
		}
		finally
		{
			Object.DestroyImmediate(data);
			Object.DestroyImmediate(bossObject);
			Object.DestroyImmediate(target);
		}
	}

	private static EnemyNavigationData TestNavigation()
	{
		return new EnemyNavigationData
		{
			MoveSpeed = 2.1f,
			Acceleration = 12f,
			AngularSpeed = 300f,
			AttackRange = 9f,
			DetectionRange = 12.5f,
			LeashRange = 18f,
			LeashFollowDistance = 2.5f,
			PatrolRadius = 3f,
			PatrolWait = 2f,
			AlertDuration = 0.25f,
			ReturnTolerance = 0.65f,
			NavMeshSampleDistance = 4f,
			AgentRadius = 1.25f,
			AgentHeight = 3.1f
		};
	}
}

public sealed class TestEnemyController : EnemyControllerBase
{
	public void InitializeForTest(Transform target, Damageable targetHealth)
	{
		InitializeEnemy("test.enemy", 50f, 20f, 0.1f, target, targetHealth, TestNavigation());
	}

	public void AuthorizeCombat()
	{
		ActivateCombat();
	}

	protected override IEnumerator ExecuteAttack()
	{
		yield break;
	}

	private static EnemyNavigationData TestNavigation()
	{
		return new EnemyNavigationData
		{
			MoveSpeed = 2f,
			Acceleration = 8f,
			AngularSpeed = 240f,
			AttackRange = 2f,
			DetectionRange = 8f,
			LeashRange = 12f,
			LeashFollowDistance = 2f,
			PatrolRadius = 2f,
			PatrolWait = 1f,
			AlertDuration = 0.1f,
			ReturnTolerance = 0.5f,
			NavMeshSampleDistance = 2f,
			AgentRadius = 0.5f,
			AgentHeight = 2f
		};
	}
}
}
