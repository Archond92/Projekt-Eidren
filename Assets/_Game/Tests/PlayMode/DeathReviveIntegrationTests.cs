using Eidren.AI;
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class DeathReviveIntegrationTests
{
	[UnityTest]
	public IEnumerator Death_CancelsAttackAndShowsOneDefeatWindow()
	{
		yield return LoadFreshZone("Zone_Greenwood");
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		PlayerPrefabBindings player = spawner.SpawnedPlayer;
		PlayerDeathController death = player.DeathController;
		EidrenServiceRoot services = UnityEngine.Object.FindFirstObjectByType<EidrenServiceRoot>();
		Assert.That<bool>(services.GameSession.PlayerEquipment.TryEquip(EquipmentSlot.Weapon1, ItemStack.Create(services.ContentDatabase.GetItem("hammer"), 1), out var equipError), (IResolveConstraint)(object)Is.True, equipError, Array.Empty<object>());
		yield return null;
		Assert.That<string>(player.Combat.ActiveWeapon.Id, (IResolveConstraint)(object)Is.EqualTo((object)"hammer"));
		spawner.Input.PressAttack();
		yield return WaitUntil(() => player.Combat.IsAttacking, "Player attack did not start.");
		Kill(player);
		yield return null;
		Assert.That<bool>(death.IsDead, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(death.DeathNotificationCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(death.Window.IsOpen, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(player.Combat.IsAttacking, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(player.WeaponHitbox.IsWindowActive, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(player.Motor.IsDodging, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(spawner.Input.GameplayEnabled, (IResolveConstraint)(object)Is.False);
		Kill(player);
		yield return null;
		Assert.That<int>(death.DeathNotificationCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[UnityTest]
	public IEnumerator Revive_LoadsHomeBaseAndRestoresRuntimeState()
	{
		yield return LoadFreshZone("Zone_Quarry");
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		PlayerPrefabBindings player = spawner.SpawnedPlayer;
		PlayerInventory inventory = services.GameSession.PlayerInventory;
		int woodBefore = inventory.GetTotalAmount("wood");
		Assert.That<int>(inventory.Add("wood", 3), (IResolveConstraint)(object)Is.Zero);
		int woodAfterAdd = inventory.GetTotalAmount("wood");
		spawner.Input.PressDodge();
		yield return WaitUntil(() => player.Motor.IsDodging, "Dodge did not start.");
		ItemDefinition food = services.ContentDatabase.GetItem("buff_food");
		Assert.That<bool>(player.Consumables.TryUse(food), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(player.Consumables.FoodBuffActive, (IResolveConstraint)(object)Is.True);
		player.Damageable.IsInvulnerable = false;
		Kill(player);
		yield return null;
		Assert.That<bool>(player.Motor.IsDodging, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(player.Consumables.FoodBuffActive, (IResolveConstraint)(object)Is.False);
		player.DeathController.Revive();
		yield return WaitUntil(() => SceneManager.GetActiveScene().name == "HomeBase" && !services.SceneFlowService.IsTransitioning, "Revive did not finish loading HomeBase.");
		yield return WaitUntil(() => UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>()?.SpawnedPlayer != null, "HomeBase player did not spawn.");
		ZonePlayerSpawner zonePlayerSpawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		PlayerPrefabBindings revived = zonePlayerSpawner.SpawnedPlayer;
		Assert.That<float>(revived.Damageable.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)revived.Damageable.MaxHealth));
		Assert.That<float>(revived.Motor.CurrentStamina, (IResolveConstraint)(object)Is.EqualTo((object)revived.Motor.MaxStamina));
		Assert.That<bool>(revived.Consumables.FoodBuffActive, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(revived.EidraTeam.enabled, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(zonePlayerSpawner.Input.GameplayEnabled, (IResolveConstraint)(object)Is.True);
		Assert.That<PlayerLifeState>(services.GameSession.LifeState, (IResolveConstraint)(object)Is.EqualTo((object)PlayerLifeState.Alive));
		Assert.That<string>(services.GameSession.LastSafeSceneKey, (IResolveConstraint)(object)Is.EqualTo((object)"HomeBase"));
		Assert.That<int>(woodAfterAdd, (IResolveConstraint)(object)Is.EqualTo((object)(woodBefore + 3)));
		Assert.That<int>(inventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<bool>(services.GameSession.DeathBag.LiesIn("Zone_Quarry"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(services.GameSession.TryGetStorageState("death.bag", out var bag), (IResolveConstraint)(object)Is.True);
		int woodInBag = 0;
		ItemStack[] slots = bag.Slots;
		for (int num = 0; num < slots.Length; num++)
		{
			ItemStack stack = slots[num];
			if (stack.ItemId == "wood")
			{
				woodInBag += stack.Quantity;
			}
		}
		Assert.That<int>(woodInBag, (IResolveConstraint)(object)Is.EqualTo((object)woodAfterAdd));
		Assert.That<PlayerPrefabBindings[]>(UnityEngine.Object.FindObjectsByType<PlayerPrefabBindings>(FindObjectsInactive.Include, FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		Assert.That<EnemyControllerBase[]>(UnityEngine.Object.FindObjectsByType<EnemyControllerBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None), (IResolveConstraint)(object)Is.Empty);
	}

	[UnityTest]
	public IEnumerator PlayerDeath_ImmediatelyStopsEnemyAttack()
	{
		yield return LoadFreshZone("Zone_Greenwood");
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		PlayerPrefabBindings player = spawner.SpawnedPlayer;
		WildlingController[] wildlings = UnityEngine.Object.FindObjectsByType<WildlingController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		WildlingController attacker = wildlings[0];
		for (int index = 1; index < wildlings.Length; index++)
		{
			wildlings[index].gameObject.SetActive(value: false);
		}
		player.Motor.Teleport(attacker.transform.position - attacker.transform.forward * 1.15f + Vector3.up * 0.05f);
		player.Damageable.IsInvulnerable = true;
		Physics.SyncTransforms();
		Assert.That<bool>(attacker.TryDetectTarget(), (IResolveConstraint)(object)Is.True);
		yield return WaitUntil(() => attacker.HasActiveAttack, "Wildling did not open its attack hitbox.");
		player.Damageable.IsInvulnerable = false;
		Kill(player);
		yield return null;
		Assert.That<bool>(attacker.HasActiveAttack, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(attacker.HasActiveTelegraph, (IResolveConstraint)(object)Is.False);
		Assert.That<EnemyState[]>(new EnemyState[2]
		{
			EnemyState.Return,
			EnemyState.Idle
		}, (IResolveConstraint)(object)Does.Contain((object)attacker.EnemyState));
	}

	[UnityTest]
	public IEnumerator DeathWindow_MainMenuUsesSceneFlowAndCleansSession()
	{
		yield return LoadFreshZone("Zone_Marsh");
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>().SpawnedPlayer;
		Kill(player);
		yield return null;
		player.DeathController.ReturnToMainMenu();
		yield return WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu" && !services.SceneFlowService.IsTransitioning, "Main menu transition did not finish.");
		Assert.That<GameSessionPhase>(services.GameSession.Phase, (IResolveConstraint)(object)Is.EqualTo((object)GameSessionPhase.Shell));
		Assert.That<PlayerLifeState>(services.GameSession.LifeState, (IResolveConstraint)(object)Is.EqualTo((object)PlayerLifeState.Alive));
		Assert.That<MainMenuController>(UnityEngine.Object.FindFirstObjectByType<MainMenuController>(), (IResolveConstraint)(object)Is.Not.Null);
	}

	[UnityTest]
	public IEnumerator EidraAbility_IsCancelledByPlayerDeath()
	{
		yield return ResetServices();
		EidrenServiceRoot eidrenServiceRoot = EidrenServiceRoot.FindOrCreate();
		eidrenServiceRoot.GameSession.StartNewGame();
		eidrenServiceRoot.PlayerProgression.RecordEnemyDefeated(10525);
		Assert.That<bool>(eidrenServiceRoot.EidraRoster.TryCapture("terrock", out var _, out var captureError), (IResolveConstraint)(object)Is.True, captureError, Array.Empty<object>());
		yield return LoadScene("Zone_EmberRuins");
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		PlayerInputReader playerInputReader = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
		BossController boss = UnityEngine.Object.FindFirstObjectByType<BossController>();
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerInputReader>(playerInputReader, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<BossController>(boss, (IResolveConstraint)(object)Is.Not.Null);
		boss.BeginBattle();
		playerInputReader.PressSkill(1);
		yield return WaitUntil(() => player.EidraTeam.IsAbilityRunning, "Eidra ability did not begin.");
		player.Damageable.IsInvulnerable = false;
		Kill(player);
		yield return null;
		Assert.That<bool>(player.EidraTeam.IsAbilityRunning, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(player.EidraTeam.HasActiveAbilityTelegraph, (IResolveConstraint)(object)Is.False);
	}

	[UnityTest]
	public IEnumerator DefeatWindow_AppearsAcrossExteriorZones()
	{
		string[] scenes = new string[3] { "Zone_Greenwood", "Zone_Quarry", "Zone_EmberRuins" };
		yield return ResetServices();
		string[] array = scenes;
		foreach (string scene in array)
		{
			EidrenServiceRoot.FindOrCreate().GameSession.StartNewGame();
			yield return LoadScene(scene);
			PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>().SpawnedPlayer;
			Kill(player);
			yield return null;
			Assert.That<bool>(player.DeathController.Window.IsOpen, (IResolveConstraint)(object)Is.True, scene, Array.Empty<object>());
		}
	}

	private static void Kill(PlayerPrefabBindings player)
	{
		player.Damageable.ApplyDamage(new DamageInfo(player.Damageable.MaxHealth * 10f, 0f, player.transform.position, player.gameObject, isBackAttack: false, "test.player.lethal", "test"));
	}

	private static IEnumerator LoadFreshZone(string sceneName)
	{
		yield return ResetServices();
		yield return LoadScene(sceneName);
		yield return WaitUntil(() => UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>()?.SpawnedPlayer != null, "Player did not spawn in " + sceneName + ".");
	}

	private static IEnumerator ResetServices()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
	}

	private static IEnumerator LoadScene(string sceneName)
	{
		AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
		Assert.That<AsyncOperation>(operation, (IResolveConstraint)(object)Is.Not.Null);
		while (!operation.isDone)
		{
			yield return null;
		}
		for (int frame = 0; frame < 3; frame++)
		{
			yield return null;
		}
	}

	private static IEnumerator WaitUntil(Func<bool> predicate, string failure, float timeout = 12f)
	{
		float deadline = Time.realtimeSinceStartup + timeout;
		while (Time.realtimeSinceStartup < deadline)
		{
			if (predicate())
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail(failure);
	}
}
}
