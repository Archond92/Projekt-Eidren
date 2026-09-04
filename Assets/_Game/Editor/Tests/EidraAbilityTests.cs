using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using Eidren.Eidra;
using Eidren.Input;
using Eidren.Player;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class EidraAbilityTests
{
	[Test]
	public void TargetValidation_RequiresLivingActiveTargetInsideRange()
	{
		GameObject target = new GameObject("EidraAbilityTest_Target");
		try
		{
			target.transform.position = new Vector3(0f, 5f, 12f);
			Assert.That<bool>(EidraAbilityRules.IsTargetInRange(Vector3.zero, target.transform, targetIsAlive: true, targetIsActive: true, 12f), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(EidraAbilityRules.IsTargetInRange(Vector3.zero, target.transform, targetIsAlive: false, targetIsActive: true, 12f), (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(EidraAbilityRules.IsTargetInRange(Vector3.zero, target.transform, targetIsAlive: true, targetIsActive: false, 12f), (IResolveConstraint)(object)Is.False);
			target.transform.position = new Vector3(0f, 0f, 12.01f);
			Assert.That<bool>(EidraAbilityRules.IsTargetInRange(Vector3.zero, target.transform, targetIsAlive: true, targetIsActive: true, 12f), (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			Object.DestroyImmediate(target);
		}
	}

	[Test]
	public void ShadowStep_SearchesBehindTargetAndRejectsBlockedResult()
	{
		GameObject target = new GameObject("EidraAbilityTest_ShadowTarget");
		try
		{
			target.transform.position = new Vector3(0f, 0f, 5f);
			target.transform.rotation = Quaternion.identity;
			int checks = 0;
			Assert.That<bool>(EidraAbilityRules.TryFindShadowStepDestination(Vector3.zero, target.transform, targetIsAlive: true, targetIsActive: true, 12f, 2.8f, (Vector3 _) => ++checks >= 2, out var destination), (IResolveConstraint)(object)Is.True);
			Assert.That<int>(checks, (IResolveConstraint)(object)Is.EqualTo((object)2));
			Assert.That<float>(Vector3.Distance(target.transform.position, destination), (IResolveConstraint)(object)Is.EqualTo((object)2.8f).Within((object)0.001f));
			Vector3 forward = target.transform.forward;
			Vector3 destination2 = destination - target.transform.position;
			Assert.That<float>(Vector3.Dot(forward, destination2.normalized), (IResolveConstraint)(object)Is.LessThan((object)0f));
			Assert.That<bool>(EidraAbilityRules.TryFindShadowStepDestination(Vector3.zero, target.transform, targetIsAlive: true, targetIsActive: true, 12f, 2.8f, (Vector3 _) => false, out destination2), (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			Object.DestroyImmediate(target);
		}
	}

	[Test]
	public void PlayerMotor_TeleportValidationRejectsObstacleAndMapExit()
	{
		EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		GameObject player = new GameObject("EidraAbilityTest_Player");
		GameObject cameraObject = new GameObject("EidraAbilityTest_Camera");
		GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
		try
		{
			player.AddComponent<CharacterController>();
			Damageable health = player.AddComponent<Damageable>();
			health.Initialize(100f);
			PlayerInputReader input = player.AddComponent<PlayerInputReader>();
			PlayerMotor playerMotor = player.AddComponent<PlayerMotor>();
			Camera camera = cameraObject.AddComponent<Camera>();
			playerMotor.Initialize(input, camera, health, Vector3.zero, 3f);
			obstacle.name = "EidraAbilityTest_Obstacle";
			obstacle.transform.position = new Vector3(2f, 1f, 0f);
			obstacle.transform.localScale = new Vector3(1f, 2f, 1f);
			Physics.SyncTransforms();
			Assert.That<bool>(playerMotor.CanTeleportTo(new Vector3(-2f, 0f, 0f)), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(playerMotor.CanTeleportTo(new Vector3(2f, 0f, 0f)), (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(playerMotor.CanTeleportTo(new Vector3(3f, 0f, 0f)), (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			Object.DestroyImmediate(obstacle);
			Object.DestroyImmediate(cameraObject);
			Object.DestroyImmediate(player);
		}
	}

	[Test]
	public void SteinhautSource_IsNonStackingAndComposesWithDodgeFlag()
	{
		GameObject player = new GameObject("EidraAbilityTest_Health");
		try
		{
			Damageable damageable = player.AddComponent<Damageable>();
			damageable.Initialize(100f);
			damageable.SetInvulnerabilitySource("eidra.steinhaut", active: true);
			damageable.SetInvulnerabilitySource("eidra.steinhaut", active: true);
			Assert.That<bool>(damageable.IsInvulnerable, (IResolveConstraint)(object)Is.True);
			damageable.IsInvulnerable = true;
			damageable.SetInvulnerabilitySource("eidra.steinhaut", active: false);
			Assert.That<bool>(damageable.IsInvulnerable, (IResolveConstraint)(object)Is.True);
			damageable.IsInvulnerable = false;
			Assert.That<bool>(damageable.IsInvulnerable, (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			Object.DestroyImmediate(player);
		}
	}

	[Test]
	public void BackMark_AppliesOnlyToBackHitAndClearsOnTargetDeath()
	{
		GameObject player = new GameObject("EidraAbilityTest_Attacker");
		GameObject bossObject = new GameObject("EidraAbilityTest_Boss");
		BossData data = ScriptableObject.CreateInstance<BossData>();
		try
		{
			Damageable playerHealth = player.AddComponent<Damageable>();
			playerHealth.Initialize(100f);
			data.MaxHealth = 100f;
			data.MaxStagger = 100f;
			BossController boss = bossObject.AddComponent<BossController>();
			boss.Initialize(data, player.transform, playerHealth, null);
			boss.BeginBattle();
			boss.MarkBack(6f, 1.45f);
			boss.ApplyDamage(new DamageInfo(10f, 0f, boss.transform.position, player, isBackAttack: false, "test.front", "test"));
			Assert.That<float>(boss.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)90f));
			boss.ApplyDamage(new DamageInfo(10f, 0f, boss.transform.position, player, isBackAttack: true, "test.back", "test"));
			Assert.That<float>(boss.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)75.5f).Within((object)0.001f));
			boss.ApplyDamage(new DamageInfo(100f, 0f, boss.transform.position, player, isBackAttack: false, "test.lethal", "test"));
			Assert.That<bool>(boss.IsAlive, (IResolveConstraint)(object)Is.False);
			Assert.That<float>(boss.BackMarkRemaining, (IResolveConstraint)(object)Is.EqualTo((object)0f));
		}
		finally
		{
			Object.DestroyImmediate(data);
			Object.DestroyImmediate(bossObject);
			Object.DestroyImmediate(player);
		}
	}
}
}
