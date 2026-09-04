using Eidren.AI;
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Interaction;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class GaronEmberRuinsIntegrationTests
{
	[UnityTest]
	public IEnumerator Encounter_ActivatesReturnsResetsAndWinsOnce()
	{
		yield return LoadFreshEmberRuins();
		BossAreaController area = UnityEngine.Object.FindFirstObjectByType<BossAreaController>();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		BossController boss = area.ActiveBoss;
		PlayerPrefabBindings player = spawner.SpawnedPlayer;
		Assert.That<BossController>(boss, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(boss.BattleActive, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(area.EncounterView.BossVisible, (IResolveConstraint)(object)Is.False);
		MovePlayer(player, boss.transform.position + Vector3.back * 5f);
		yield return WaitUntil(() => boss.BattleActive && area.EncounterView.BossVisible, "Garon did not activate inside the BossArea.");
		boss.ApplyDamage(new DamageInfo(100f, 40f, boss.transform.position, player.gameObject, isBackAttack: false, "test.ember.damage", "test"));
		Assert.That<float>(boss.CurrentHealth, (IResolveConstraint)(object)Is.LessThan((object)boss.MaxHealth));
		Assert.That<float>(boss.CurrentStagger, (IResolveConstraint)(object)Is.GreaterThan((object)0f));
		MovePlayer(player, area.HomePoint.position + Vector3.back * 28f);
		yield return WaitUntil(() => boss.EnemyState == EnemyState.Return || boss.EnemyState == EnemyState.Idle, "Garon did not return after the player left the leash.");
		Assert.That<bool>(area.EncounterView.BossVisible, (IResolveConstraint)(object)Is.False);
		yield return WaitUntil(() => boss.EnemyState == EnemyState.Idle, "Garon did not finish returning home.");
		Assert.That<float>(boss.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)boss.MaxHealth));
		Assert.That<float>(boss.CurrentStagger, (IResolveConstraint)(object)Is.Zero);
		MovePlayer(player, boss.transform.position + Vector3.back * 5f);
		yield return WaitUntil(() => boss.BattleActive, "Garon did not reactivate after Return.");
		KillBoss(boss, player.gameObject);
		yield return null;
		GameSession gameSession = EidrenServiceRoot.Instance.GameSession;
		Assert.That<bool>(gameSession.HasProgressFlag("garon_defeated"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(gameSession.IsWorldMapNodeCompleted("zone_ember_ruins"), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(gameSession.PlayerInventory.GetTotalAmount("copper_bar"), (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(gameSession.PlayerInventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)2), "M8.2: Der Bossdrop addiert sich nicht mehr auf drei geschenkte Starttraenke.", Array.Empty<object>());
		Assert.That<bool>(area.EncounterView.BossVisible, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(area.EncounterView.ResultVisible, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(gameSession.TrySetProgressFlag("garon_defeated"), (IResolveConstraint)(object)Is.False);
		yield return SceneManager.LoadSceneAsync("Zone_EmberRuins", LoadSceneMode.Single);
		yield return WaitUntil(() => UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>()?.SpawnedPlayer != null, "Ember Ruins did not reload.");
		BossAreaController bossAreaController = UnityEngine.Object.FindFirstObjectByType<BossAreaController>();
		Assert.That<bool>(bossAreaController.BossSuppressedByProgress, (IResolveConstraint)(object)Is.True);
		Assert.That<BossController>(bossAreaController.ActiveBoss, (IResolveConstraint)(object)Is.Null);
	}

	[UnityTest]
	public IEnumerator FullInventory_DropsCompleteBossRewardAsWorldItems()
	{
		yield return LoadFreshEmberRuins();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		ItemStack[] full = (from _ in Enumerable.Range(0, 16)
			select new ItemStack("stone", services.ContentDatabase.GetItem("stone").MaximumStackSize)).ToArray();
		services.GameSession.PlayerInventory.Reset(full);
		BossAreaController area = UnityEngine.Object.FindFirstObjectByType<BossAreaController>();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		MovePlayer(spawner.SpawnedPlayer, area.ActiveBoss.transform.position + Vector3.back * 5f);
		yield return WaitUntil(() => area.ActiveBoss.BattleActive, "Garon did not activate.");
		KillBoss(area.ActiveBoss, spawner.SpawnedPlayer.gameObject);
		yield return null;
		ZoneLootController zoneLootController = UnityEngine.Object.FindFirstObjectByType<ZoneLootController>();
		Assert.That<IReadOnlyList<WorldItemController>>(zoneLootController.ActiveItems, (IResolveConstraint)(object)((ConstraintExpression)Has.Count).EqualTo((object)3));
		Assert.That<bool>(zoneLootController.ActiveItems.Any((WorldItemController item) => item.Item.Id == "copper_bar" && item.Quantity == 3), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(zoneLootController.ActiveItems.Any((WorldItemController item) => item.Item.Id == "healing_potion" && item.Quantity == 2), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("copper_bar"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<bool>(area.EncounterView.ResultVisible, (IResolveConstraint)(object)Is.True);
	}

	[UnityTest]
	public IEnumerator CampaignGaron_IsSuppressedBeforeLevelTwelve()
	{
		yield return PrepareEmberRuinsAtExperience(4995);
		yield return SceneManager.LoadSceneAsync("Zone_EmberRuins", LoadSceneMode.Single);
		yield return WaitUntil(() => UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>()?.SpawnedPlayer != null, "Ember Ruins did not initialize.");
		BossAreaController bossAreaController = UnityEngine.Object.FindFirstObjectByType<BossAreaController>();
		Assert.That<int>(EidrenServiceRoot.Instance.PlayerProgression.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)11));
		Assert.That<bool>(bossAreaController.BossSuppressedByProgress, (IResolveConstraint)(object)Is.True);
		Assert.That<BossController>(bossAreaController.ActiveBoss, (IResolveConstraint)(object)Is.Null);
	}

	private static IEnumerator LoadFreshEmberRuins()
	{
		yield return PrepareEmberRuinsAtExperience(5000);
		yield return SceneManager.LoadSceneAsync("Zone_EmberRuins", LoadSceneMode.Single);
		yield return WaitUntil(() => UnityEngine.Object.FindFirstObjectByType<BossAreaController>()?.ActiveBoss != null && UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>()?.SpawnedPlayer != null, "Ember Ruins encounter did not initialize.");
	}

	private static IEnumerator PrepareEmberRuinsAtExperience(int experience)
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		EidrenServiceRoot eidrenServiceRoot = EidrenServiceRoot.FindOrCreate();
		eidrenServiceRoot.GameSession.StartNewGame();
		eidrenServiceRoot.PlayerProgression.RecordEnemyDefeated(experience);
	}

	private static void MovePlayer(PlayerPrefabBindings player, Vector3 position)
	{
		CharacterController characterController = player.CharacterController;
		characterController.enabled = false;
		player.transform.position = position;
		characterController.enabled = true;
	}

	private static void KillBoss(BossController boss, GameObject source)
	{
		boss.ApplyDamage(new DamageInfo(boss.MaxHealth, 0f, boss.transform.position, source, isBackAttack: false, "test.ember.lethal", "test"));
	}

	private static IEnumerator WaitUntil(Func<bool> condition, string failureMessage)
	{
		float deadline = Time.realtimeSinceStartup + 12f;
		while (Time.realtimeSinceStartup < deadline)
		{
			if (condition())
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail(failureMessage);
	}
}
}
