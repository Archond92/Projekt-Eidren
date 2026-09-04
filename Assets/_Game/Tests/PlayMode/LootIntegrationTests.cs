using Eidren.AI;
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class LootIntegrationTests : InputTestFixture
{
	[UnityTest]
	public IEnumerator WildlingDeathFillsExactlyOneContainerOnce()
	{
		yield return LoadGreenwood();
		WildlingController wildling = UnityEngine.Object.FindFirstObjectByType<WildlingController>();
		ZoneLootController loot = UnityEngine.Object.FindFirstObjectByType<ZoneLootController>();
		Assert.That<WildlingController>(wildling, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<ZoneLootController>(loot, (IResolveConstraint)(object)Is.Not.Null);
		Kill(wildling);
		yield return null;
		Assert.That<IReadOnlyList<EnemyLootContainer>>(loot.LootContainers, (IResolveConstraint)(object)((ConstraintExpression)Has.Count).EqualTo((object)1), "Ein besiegter Gegner erzeugt genau einen Lootbestand.", Array.Empty<object>());
		Assert.That<IReadOnlyList<WorldItemController>>(loot.ActiveItems, (IResolveConstraint)(object)Is.Empty, "Beute darf nicht als lose Gegenstände umherliegen.", Array.Empty<object>());
		Assert.That<int>(TotalOf(loot.LootContainers[0], "plant_fiber"), (IResolveConstraint)(object)Is.InRange((IComparable)1, (IComparable)2));
		Kill(wildling);
		yield return null;
		Assert.That<IReadOnlyList<EnemyLootContainer>>(loot.LootContainers, (IResolveConstraint)(object)((ConstraintExpression)Has.Count).EqualTo((object)1));
	}

	[UnityTest]
	public IEnumerator EachWildlingDeathProducesItsOwnContainer()
	{
		yield return LoadGreenwood();
		WildlingController[] wildlings = UnityEngine.Object.FindObjectsByType<WildlingController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		ZoneLootController loot = UnityEngine.Object.FindFirstObjectByType<ZoneLootController>();
		WildlingController[] array = wildlings;
		for (int i = 0; i < array.Length; i++)
		{
			Kill(array[i]);
		}
		yield return null;
		Assert.That<int>(wildlings.Length, (IResolveConstraint)(object)Is.EqualTo((object)5));
		Assert.That<IReadOnlyList<EnemyLootContainer>>(loot.LootContainers, (IResolveConstraint)(object)((ConstraintExpression)Has.Count).EqualTo((object)5));
		foreach (EnemyLootContainer lootContainer in loot.LootContainers)
		{
			Assert.That<int>(TotalOf(lootContainer, "plant_fiber"), (IResolveConstraint)(object)Is.GreaterThan((object)0));
		}
	}

	[UnityTest]
	public IEnumerator CorpseIsTheInteractionTargetAndShowsTheHand()
	{
		yield return LoadGreenwood();
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		ZoneLootController loot = UnityEngine.Object.FindFirstObjectByType<ZoneLootController>();
		Kill(UnityEngine.Object.FindFirstObjectByType<WildlingController>());
		yield return null;
		EnemyLootContainer container = loot.LootContainers[0];
		PlacePlayerAt(player, container.transform.position);
		yield return null;
		Assert.That<IInteractable>(player.Interaction.CurrentTarget, (IResolveConstraint)(object)Is.SameAs((object)container));
		Assert.That<bool>(player.CombatHud.InteractionView.Snapshot.CanInteract, (IResolveConstraint)(object)Is.True);
		Assert.That<Sprite>(player.CombatHud.InteractionView.Snapshot.Icon, (IResolveConstraint)(object)Is.Null, "Der Behälter bringt kein eigenes Symbol mit.", Array.Empty<object>());
		Assert.That<Sprite>(player.CombatHud.InteractionView.ResolveIcon(player.CombatHud.InteractionView.Snapshot), (IResolveConstraint)(object)Is.Not.Null, "Der Button zeigt dann die Hand, keine leere Fläche.", Array.Empty<object>());
	}

	[UnityTest]
	public IEnumerator PartialTakeLeavesTheRestInTheSameContainer()
	{
		yield return LoadGreenwood();
		ZoneLootController loot = UnityEngine.Object.FindFirstObjectByType<ZoneLootController>();
		PlayerInventory inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
		inventory.Clear();
		Kill(UnityEngine.Object.FindFirstObjectByType<WildlingController>());
		yield return null;
		EnemyLootContainer container = loot.LootContainers[0];
		Assert.That<int>(TotalOf(container, "plant_fiber"), (IResolveConstraint)(object)Is.GreaterThan((object)0));
		int kinds = OccupiedSlots(container);
		ItemStack[] slots = container.ExportSlots();
		int taken = -1;
		for (int index = 0; index < slots.Length; index++)
		{
			if (!slots[index].IsEmpty)
			{
				taken = index;
				Assert.That<bool>(inventory.TryAddAll(slots[index].ItemId, slots[index].Quantity), (IResolveConstraint)(object)Is.True);
				slots[index] = ItemStack.Empty;
				break;
			}
		}
		Assert.That<int>(taken, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)0));
		Assert.That<bool>(container.TryImportSlots(slots, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(OccupiedSlots(container), (IResolveConstraint)(object)Is.EqualTo((object)(kinds - 1)), "Der Rest bleibt im selben Behälter.", Array.Empty<object>());
		Assert.That<bool>(container.IsEmpty, (IResolveConstraint)(object)Is.EqualTo((object)(kinds == 1)));
		Assert.That<IReadOnlyList<WorldItemController>>(loot.ActiveItems, (IResolveConstraint)(object)Is.Empty);
	}

	[UnityTest]
	public IEnumerator EmptyingRetiresTheContainerWithoutDuplicates()
	{
		yield return LoadGreenwood();
		ZoneLootController loot = UnityEngine.Object.FindFirstObjectByType<ZoneLootController>();
		PlayerInventory inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
		inventory.Clear();
		Kill(UnityEngine.Object.FindFirstObjectByType<WildlingController>());
		yield return null;
		EnemyLootContainer container = loot.LootContainers[0];
		Dictionary<string, int> expected = new Dictionary<string, int>();
		ItemStack[] array = container.ExportSlots();
		for (int i = 0; i < array.Length; i++)
		{
			ItemStack stack = array[i];
			if (!stack.IsEmpty)
			{
				expected.TryGetValue(stack.ItemId, out var current);
				expected[stack.ItemId] = current + stack.Quantity;
			}
		}
		Assert.That<Dictionary<string, int>>(expected, (IResolveConstraint)(object)Is.Not.Empty);
		foreach (KeyValuePair<string, int> entry in expected)
		{
			Assert.That<bool>(inventory.TryAddAll(entry.Key, entry.Value), (IResolveConstraint)(object)Is.True);
		}
		Assert.That<bool>(container.TryImportSlots(new ItemStack[24], out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<bool>(container.IsEmpty, (IResolveConstraint)(object)Is.True);
		Assert.That<IReadOnlyList<EnemyLootContainer>>(loot.LootContainers, (IResolveConstraint)(object)Has.No.Member((object)container), "Ein geleerter Behälter bleibt nicht in der Liste.", Array.Empty<object>());
		Assert.That<IReadOnlyList<WorldItemController>>(loot.ActiveItems, (IResolveConstraint)(object)Is.Empty, "Leeren erzeugt keine zusätzlichen losen Gegenstände.", Array.Empty<object>());
		foreach (KeyValuePair<string, int> entry2 in expected)
		{
			Assert.That<int>(inventory.GetTotalAmount(entry2.Key), (IResolveConstraint)(object)Is.EqualTo((object)entry2.Value), "'" + entry2.Key + "' wurde verdoppelt oder verloren.", Array.Empty<object>());
		}
	}

	[UnityTest]
	public IEnumerator CorpseKeepsLootButIsNoLongerACombatTarget()
	{
		yield return LoadGreenwood();
		WildlingController wildling = UnityEngine.Object.FindFirstObjectByType<WildlingController>();
		Kill(wildling);
		yield return null;
		Assert.That<bool>(wildling.IsAlive, (IResolveConstraint)(object)Is.False);
		Assert.That<EnemyState>(wildling.EnemyState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Dead));
		Assert.That<bool>(wildling.NavigationAgent.enabled, (IResolveConstraint)(object)Is.False, "Eine Leiche navigiert nicht mehr.", Array.Empty<object>());
		Assert.That<bool>(wildling.gameObject.activeSelf, (IResolveConstraint)(object)Is.True, "Die Leiche bleibt liegen, solange sie Beute trägt.", Array.Empty<object>());
	}

	[UnityTest]
	public IEnumerator InstantInteractionAddsWholeStackAndRemovesWorldItem()
	{
		yield return LoadGreenwood();
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		PlayerInventory inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
		inventory.Clear();
		WorldItemController fiber = SpawnWorldItem(player, 2);
		int amount = fiber.Quantity;
		PlacePlayerAt(player, fiber.transform.position);
		Assert.That<IInteractable>(player.Interaction.CurrentTarget, (IResolveConstraint)(object)Is.SameAs((object)fiber));
		spawner.Input.SetVirtualInteract(held: true);
		yield return null;
		spawner.Input.SetVirtualInteract(held: false);
		yield return null;
		Assert.That<int>(inventory.GetTotalAmount("plant_fiber"), (IResolveConstraint)(object)Is.EqualTo((object)amount));
		Assert.That<bool>(fiber == null || !fiber.gameObject.activeSelf, (IResolveConstraint)(object)Is.True);
		Assert.That<string>(player.InventoryFeedback.LastMessage, (IResolveConstraint)(object)Does.Contain("Pflanzenfaser"));
	}

	[UnityTest]
	public IEnumerator FullInventoryKeepsLootAndShowsBlockedUi()
	{
		yield return LoadGreenwood();
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		PlayerInventory inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
		inventory.Clear();
		int capacity = 16 * EidrenServiceRoot.FindOrCreate().ContentDatabase.GetItem("stone").MaximumStackSize;
		Assert.That<int>(inventory.Add("stone", capacity), (IResolveConstraint)(object)Is.Zero);
		WorldItemController fiber = SpawnWorldItem(player, 2);
		PlacePlayerAt(player, fiber.transform.position);
		yield return null;
		Assert.That<IInteractable>(player.Interaction.CurrentTarget, (IResolveConstraint)(object)Is.SameAs((object)fiber));
		Assert.That<bool>(player.CombatHud.InteractionView.Snapshot.CanInteract, (IResolveConstraint)(object)Is.False);
		Assert.That<string>(player.CombatHud.InteractionView.Snapshot.BlockedReason, (IResolveConstraint)(object)Is.EqualTo((object)"INVENTAR VOLL"));
		Assert.That<bool>(fiber.gameObject.activeSelf, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(inventory.GetTotalAmount("plant_fiber"), (IResolveConstraint)(object)Is.Zero);
	}

	[UnityTest]
	public IEnumerator InsufficientCapacityDoesNotPartiallyRemoveStack()
	{
		yield return LoadGreenwood();
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		ContentDatabase database = EidrenServiceRoot.FindOrCreate().ContentDatabase;
		PlayerInventory inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
		List<ItemStack> fullSlots = new List<ItemStack>();
		for (int index = 0; index < 15; index++)
		{
			fullSlots.Add(new ItemStack("stone", database.GetItem("stone").MaximumStackSize));
		}
		fullSlots.Add(new ItemStack("wood", database.GetItem("wood").MaximumStackSize - 1));
		inventory.Reset(fullSlots);
		ItemDefinition wood = database.GetItem("wood");
		WorldItemController template = wood.WorldDropPrefab.GetComponent<WorldItemController>();
		WorldItemController worldItem = UnityEngine.Object.Instantiate(template, player.transform.position + Vector3.forward, Quaternion.identity);
		worldItem.Initialize(wood, 2, "loot.test.atomic");
		worldItem.CompleteInteraction(new InteractionContext(player.gameObject, player.transform, Vector3.forward, inventory));
		yield return null;
		Assert.That<int>(inventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)(database.GetItem("wood").MaximumStackSize - 1)));
		Assert.That<WorldItemController>(worldItem, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(worldItem.IsCollected, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(worldItem.gameObject.activeSelf, (IResolveConstraint)(object)Is.True);
		UnityEngine.Object.Destroy(worldItem.gameObject);
	}

	[UnityTest]
	public IEnumerator KeyboardAndGamepadPickUpActualWorldItems()
	{
		yield return LoadGreenwood();
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		PlayerInventory inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
		inventory.Clear();
		WorldItemController keyboardItem = SpawnWorldItem(player, 2);
		int keyboardAmount = keyboardItem.Quantity;
		PlacePlayerAt(player, keyboardItem.transform.position);
		Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
		Press(keyboard.rKey);
		yield return null;
		Release(keyboard.rKey);
		yield return null;
		Assert.That<int>(inventory.GetTotalAmount("plant_fiber"), (IResolveConstraint)(object)Is.EqualTo((object)keyboardAmount));
		InputSystem.RemoveDevice(keyboard);
		WorldItemController gamepadItem = SpawnWorldItem(player, 2);
		int gamepadAmount = gamepadItem.Quantity;
		PlacePlayerAt(player, gamepadItem.transform.position);
		Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
		Press(gamepad.buttonWest);
		yield return null;
		Release(gamepad.buttonWest);
		yield return null;
		Assert.That<int>(inventory.GetTotalAmount("plant_fiber"), (IResolveConstraint)(object)Is.EqualTo((object)(keyboardAmount + gamepadAmount)));
		InputSystem.RemoveDevice(gamepad);
		Assert.That<bool>(spawner.Input.InteractHeld, (IResolveConstraint)(object)Is.False);
	}

	private static WorldItemController SpawnWorldItem(PlayerPrefabBindings player, int amount)
	{
		ZoneLootController loot = UnityEngine.Object.FindFirstObjectByType<ZoneLootController>();
		int before = loot.ActiveItems.Count;
		Assert.That<int>(loot.SpawnGuaranteedItems(new InventoryItemAmount[1]
		{
			new InventoryItemAmount("plant_fiber", amount)
		}, player.transform.position + Vector3.forward * 1.5f, player.transform, "test.loot"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<IReadOnlyList<WorldItemController>>(loot.ActiveItems, (IResolveConstraint)(object)((ConstraintExpression)Has.Count).EqualTo((object)(before + 1)));
		WorldItemController worldItemController = loot.ActiveItems[before];
		AssertSafeDrop(worldItemController);
		return worldItemController;
	}

	private static int TotalOf(EnemyLootContainer container, string itemId)
	{
		return (from stack in container.ExportSlots()
			where stack.ItemId == itemId
			select stack).Sum((ItemStack stack) => stack.Quantity);
	}

	private static int OccupiedSlots(EnemyLootContainer container)
	{
		return container.ExportSlots().Count((ItemStack stack) => !stack.IsEmpty);
	}

	private static void Kill(WildlingController wildling)
	{
		wildling.ApplyDamage(new DamageInfo(wildling.MaxHealth * 2f, 0f, wildling.transform.position, null, isBackAttack: false, "test.loot.lethal", "test"));
	}

	private static void AssertSafeDrop(WorldItemController item)
	{
		ZoneController zone = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
		Assert.That<bool>(zone.WalkableGround.bounds.Contains(new Vector3(item.transform.position.x, zone.WalkableGround.bounds.max.y, item.transform.position.z)), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(NavMesh.SamplePosition(item.transform.position, out var _, 0.5f, -1), (IResolveConstraint)(object)Is.True);
		foreach (MapExitVolume exitVolume in zone.ExitVolumes)
		{
			Assert.That<bool>(exitVolume.TriggerBounds.Contains(item.transform.position), (IResolveConstraint)(object)Is.False);
		}
	}

	private static void PlacePlayerAt(PlayerPrefabBindings player, Vector3 position)
	{
		player.Motor.Teleport(position + Vector3.back * 0.8f + Vector3.up * 0.05f);
		player.transform.forward = Vector3.forward;
		Physics.SyncTransforms();
		player.Interaction.RefreshTargetsNow();
	}

	private static IEnumerator LoadGreenwood()
	{
		AsyncOperation load = SceneManager.LoadSceneAsync("Zone_Greenwood", LoadSceneMode.Single);
		Assert.That<AsyncOperation>(load, (IResolveConstraint)(object)Is.Not.Null);
		while (!load.isDone)
		{
			yield return null;
		}
		for (int frame = 0; frame < 4; frame++)
		{
			yield return null;
		}
	}
}
}
