using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
public sealed class GaronMigrationTests
{
	private sealed class TestWorld
	{
		public readonly GameObject Ground;

		public readonly GameObject NavigationRoot;

		public readonly NavMeshSurface Surface;

		public readonly GameObject Player;

		public readonly Damageable PlayerHealth;

		public readonly BossController Boss;

		public readonly BossData Data;

		public TestWorld(GameObject ground, GameObject navigationRoot, NavMeshSurface surface, GameObject player, Damageable playerHealth, BossController boss, BossData data)
		{
			Ground = ground;
			NavigationRoot = navigationRoot;
			Surface = surface;
			Player = player;
			PlayerHealth = playerHealth;
			Boss = boss;
			Data = data;
		}
	}

	[UnityTest]
	public IEnumerator Chase_ChangesToReturnWhenLeashIsExceeded()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Testumgebung.LeereWeltBereitstellen();
		TestWorld world = CreateWorld(2f, 10f);
		world.Player.transform.position = new Vector3(0f, 0f, 6f);
		world.Boss.BeginBattle();
		yield return WaitForState(world.Boss, EnemyState.Chase);
		world.Player.transform.position = new Vector3(0f, 0f, 15f);
		yield return WaitForState(world.Boss, EnemyState.Return);
		Assert.That<EnemyState>(world.Boss.EnemyState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Return));
		yield return Clean(world);
	}

	[UnityTest]
	public IEnumerator Return_AbortsActiveAttackAndClearsDamageAreas()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Testumgebung.LeereWeltBereitstellen();
		TestWorld world = CreateWorld(9f, 10f);
		ConfigureLongExecution(world.Data);
		world.Player.transform.position = new Vector3(0f, 0f, 3f);
		world.Boss.BeginBattle();
		yield return WaitForState(world.Boss, EnemyState.ExecuteAttack);
		Assert.That<bool>(world.Boss.HasActiveAttackDamage, (IResolveConstraint)(object)Is.True);
		world.Player.transform.position = new Vector3(0f, 0f, 15f);
		yield return WaitForState(world.Boss, EnemyState.Return);
		Assert.That<bool>(world.Boss.HasActiveAttackDamage, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(world.Boss.HasActiveTelegraph, (IResolveConstraint)(object)Is.False);
		yield return Clean(world);
	}

	[UnityTest]
	public IEnumerator FullStagger_InterruptsActiveAttackImmediately()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Testumgebung.LeereWeltBereitstellen();
		TestWorld world = CreateWorld(9f, 10f);
		ConfigureLongExecution(world.Data);
		world.Player.transform.position = new Vector3(0f, 0f, 3f);
		world.Boss.BeginBattle();
		yield return WaitForState(world.Boss, EnemyState.ExecuteAttack);
		world.Boss.AddStagger(world.Data.MaxStagger);
		Assert.That<EnemyState>(world.Boss.EnemyState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Staggered));
		Assert.That<bool>(world.Boss.HasActiveAttackDamage, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(world.Boss.HasActiveTelegraph, (IResolveConstraint)(object)Is.False);
		yield return Clean(world);
	}

	[UnityTest]
	public IEnumerator ReturnHome_RegeneratesHealthAndStaggerCompletely()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Testumgebung.LeereWeltBereitstellen();
		TestWorld world = CreateWorld(2f, 10f);
		world.Player.transform.position = new Vector3(0f, 0f, 5f);
		world.Boss.BeginBattle();
		world.Boss.ApplyDamage(new DamageInfo(120f, 25f, world.Boss.transform.position, world.Player, isBackAttack: false, "test.return.damage", "test"));
		Assert.That<float>(world.Boss.CurrentHealth, (IResolveConstraint)(object)Is.LessThan((object)world.Boss.MaxHealth));
		Assert.That<float>(world.Boss.CurrentStagger, (IResolveConstraint)(object)Is.GreaterThan((object)0f));
		world.Player.transform.position = new Vector3(0f, 0f, 15f);
		yield return WaitForState(world.Boss, EnemyState.Return);
		yield return WaitForState(world.Boss, EnemyState.Idle);
		Assert.That<float>(world.Boss.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)world.Boss.MaxHealth));
		Assert.That<float>(world.Boss.CurrentStagger, (IResolveConstraint)(object)Is.Zero);
		yield return Clean(world);
	}

	[UnityTest]
	public IEnumerator Death_DisablesActiveAttackAndFurtherBossDamage()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Testumgebung.LeereWeltBereitstellen();
		TestWorld world = CreateWorld(9f, 10f);
		ConfigureLongExecution(world.Data);
		world.Player.transform.position = new Vector3(0f, 0f, 3f);
		world.Boss.BeginBattle();
		yield return WaitForState(world.Boss, EnemyState.ExecuteAttack);
		world.Boss.ApplyDamage(new DamageInfo(world.Boss.MaxHealth, 0f, world.Boss.transform.position, world.Player, isBackAttack: false, "test.lethal", "test"));
		float playerHealthAfterDeath = world.PlayerHealth.CurrentHealth;
		yield return null;
		yield return null;
		Assert.That<EnemyState>(world.Boss.EnemyState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Dead));
		Assert.That<bool>(world.Boss.HasActiveAttackDamage, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(world.Boss.HasActiveTelegraph, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(world.Boss.NavigationAgent.enabled, (IResolveConstraint)(object)Is.False);
		Assert.That<float>(world.PlayerHealth.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)playerHealthAfterDeath));
		yield return Clean(world);
	}

	[Test]
	public void AttackSelection_RespectsDistancePathAndRepeatRules()
	{
		GaronAttackSetData attacks = new GaronAttackSetData();
		Assert.That<bool>(GaronAttackSelection.TryChoose(attacks, 1f, chargePathClear: true, null, 0, 0, out var closeAttack), (IResolveConstraint)(object)Is.True);
		Assert.That<GaronAttackType>(closeAttack, (IResolveConstraint)(object)Is.EqualTo((object)GaronAttackType.Spin));
		Assert.That<bool>(GaronAttackSelection.TryChoose(attacks, 3f, chargePathClear: true, null, 0, 0, out var meleeAttack), (IResolveConstraint)(object)Is.True);
		Assert.That<GaronAttackType>(meleeAttack, (IResolveConstraint)(object)Is.EqualTo((object)GaronAttackType.Front));
		Assert.That<bool>(GaronAttackSelection.TryChoose(attacks, 7f, chargePathClear: false, null, 0, 0, out var selected), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(GaronAttackSelection.TryChoose(attacks, 7f, chargePathClear: true, null, 0, 0, out var chargeAttack), (IResolveConstraint)(object)Is.True);
		Assert.That<GaronAttackType>(chargeAttack, (IResolveConstraint)(object)Is.EqualTo((object)GaronAttackType.Charge));
		Assert.That<bool>(GaronAttackSelection.TryChoose(attacks, 7f, chargePathClear: true, GaronAttackType.Charge, attacks.MaxConsecutiveSameAttack, 0, out selected), (IResolveConstraint)(object)Is.False);
	}

	private static IEnumerator Clean(TestWorld world)
	{
		world.Surface.RemoveData();
		Object.Destroy(world.Data);
		Object.Destroy(world.NavigationRoot);
		Object.Destroy(world.Ground);
		Object.Destroy(world.Boss.gameObject);
		Object.Destroy(world.Player);
		yield return null;
	}

	private static TestWorld CreateWorld(float attackRange, float leashRange)
	{
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		gameObject.name = "GaronTest_Ground";
		gameObject.transform.position = new Vector3(0f, -0.5f, 0f);
		gameObject.transform.localScale = new Vector3(50f, 1f, 50f);
		GameObject navigationRoot = new GameObject("GaronTest_Navigation");
		NavMeshSurface surface = navigationRoot.AddComponent<NavMeshSurface>();
		surface.collectObjects = CollectObjects.All;
		surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
		surface.BuildNavMesh();
		GameObject player = new GameObject("GaronTest_Player");
		Damageable playerHealth = player.AddComponent<Damageable>();
		playerHealth.Initialize(500f);
		GameObject gameObject2 = new GameObject("GaronTest_Boss");
		BossData data = ScriptableObject.CreateInstance<BossData>();
		typeof(BossData).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(data, "garon.test");
		data.Navigation = new EnemyNavigationData
		{
			MoveSpeed = 2.1f,
			Acceleration = 12f,
			AngularSpeed = 300f,
			AttackRange = attackRange,
			DetectionRange = 12.5f,
			LeashRange = leashRange,
			LeashFollowDistance = 1f,
			PatrolRadius = 0f,
			PatrolWait = 0.01f,
			AlertDuration = 0.01f,
			ReturnTolerance = 0.65f,
			NavMeshSampleDistance = 4f,
			AgentRadius = 1.25f,
			AgentHeight = 3.1f
		};
		data.StaggerDuration = 0.05f;
		BossController boss = gameObject2.AddComponent<BossController>();
		boss.Initialize(data, player.transform, playerHealth, null);
		return new TestWorld(gameObject, navigationRoot, surface, player, playerHealth, boss, data);
	}

	private static void ConfigureLongExecution(BossData data)
	{
		data.Attacks.ChoiceDuration = 0.01f;
		data.Attacks.Front.TelegraphDuration = 0.01f;
		data.Attacks.Front.ExecuteDuration = 1f;
		data.Attacks.Spin.TelegraphDuration = 0.01f;
		data.Attacks.Spin.ExecuteDuration = 1f;
	}

	private static IEnumerator WaitForState(BossController boss, EnemyState expected)
	{
		for (int frame = 0; frame < 180; frame++)
		{
			if (boss.EnemyState == expected)
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail($"Garon did not enter {expected}; current state is " + $"{boss.EnemyState}.");
	}
}
}
