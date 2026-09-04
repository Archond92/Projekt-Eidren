using Eidren.Combat;
using Eidren.Composition;
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
public sealed class ItemConsumableIntegrationTests
{
	[UnityTest]
	public IEnumerator HomeBaseConsumables_UseItemDefinitionValues()
	{
		AsyncOperation load = SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
		Assert.That<AsyncOperation>(load, (IResolveConstraint)(object)Is.Not.Null);
		while (!load.isDone)
		{
			yield return null;
		}
		for (int frame = 0; frame < 3; frame++)
		{
			yield return null;
		}
		EidrenServiceRoot eidrenServiceRoot = UnityEngine.Object.FindFirstObjectByType<EidrenServiceRoot>();
		Assert.That<EidrenServiceRoot>(eidrenServiceRoot, (IResolveConstraint)(object)Is.Not.Null);
		eidrenServiceRoot.GameSession.StartNewGame();
		Assert.That<int>(eidrenServiceRoot.GameSession.PlayerInventory.Add("healing_potion", 3), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(eidrenServiceRoot.GameSession.PlayerInventory.Add("buff_food", 2), (IResolveConstraint)(object)Is.Zero);
		yield return null;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		PlayerInputReader input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
		EidrenServiceRoot services = UnityEngine.Object.FindFirstObjectByType<EidrenServiceRoot>();
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerInputReader>(input, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<EidrenServiceRoot>(services, (IResolveConstraint)(object)Is.Not.Null);
		int fullHealthPotions = services.GameSession.PlayerInventory.GetTotalAmount("healing_potion");
		input.PressConsumable(0);
		yield return null;
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)fullHealthPotions), "A potion must not be removed at full health.", Array.Empty<object>());
		// 60 statt 100: Die Spieler-Maximalgesundheit ist seit M8.2 genau 100
		// (Player.prefab, von PlayerPrefabTests als Soll festgeschrieben) —
		// 100 Schaden TOETETEN den Spieler, der IsAlive-Guard des
		// ConsumableController blockte danach den Trank (gemessen: Heilung
		// 0,0 statt 55), und der Tod blieb als GameSession.LifeState in der
		// persistenten Session: JEDER nachfolgende Spawn startete mit
		// gesperrtem Input (ZonePlayerSpawner.HandleInputBlocked) — das war
		// die Vergiftung der Loot-Tests. Der Schaden muss nicht-toedlich
		// UND groesser als der Heilwert 55 sein, sonst deckelt die
		// Maximalgesundheit das gemessene Delta.
		player.Damageable.ApplyDamage(new DamageInfo(60f, 0f, player.transform.position, player.gameObject, isBackAttack: false, "test.item.potion", "test"));
		float beforeHealing = player.Damageable.CurrentHealth;
		input.PressConsumable(0);
		yield return null;
		Assert.That<float>(player.Damageable.CurrentHealth - beforeHealing, (IResolveConstraint)(object)Is.EqualTo((object)55f).Within((object)0.01f));
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)2));
		input.PressConsumable(1);
		yield return null;
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("buff_food"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(player.Consumables.FoodBuffActive, (IResolveConstraint)(object)Is.True);
		// F31-019: Buff-Dauer auf 2 Minuten angehoben (Nutzerentscheid 17.08.2026).
		Assert.That<float>(player.Consumables.FoodBuffRemaining, (IResolveConstraint)(object)Is.InRange((IComparable)119f, (IComparable)120f));
	}
}
}
