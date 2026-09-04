using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Interaction;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class CraftingIntegrationTests : InputTestFixture
{
	[UnityTest]
	public IEnumerator Workbench_CraftsAllRecipesAcrossInputMethods()
	{
		yield return LoadHomeBase();
		WorkbenchController station = UnityEngine.Object.FindFirstObjectByType<WorkbenchController>();
		CraftingWindow window = UnityEngine.Object.FindFirstObjectByType<CraftingWindow>(FindObjectsInactive.Include);
		InventoryWindow inventoryWindow = UnityEngine.Object.FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		PlayerInputReader input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
		EidrenServiceRoot services = UnityEngine.Object.FindFirstObjectByType<EidrenServiceRoot>();
		Assert.That<WorkbenchController>(station, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<CraftingWindow>(window, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<InventoryWindow>(inventoryWindow, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerInputReader>(input, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<EidrenServiceRoot>(services, (IResolveConstraint)(object)Is.Not.Null);
		PlayerInventory inventory = services.GameSession.PlayerInventory;
		WeaponData hammer = FindWeapon("hammer");
		WeaponData daggers = FindWeapon("daggers");
		float hammerDamageBefore = player.Combat.GetModifiedBaseHealthDamage(hammer);
		float hammerStaggerBefore = player.Combat.GetModifiedBaseStaggerDamage(hammer);
		float daggerDamageBefore = player.Combat.GetModifiedBaseHealthDamage(daggers);
		float daggerStaggerBefore = player.Combat.GetModifiedBaseStaggerDamage(daggers);
		inventory.Add("wood", 4);
		inventory.Add("stone", 3);
		inventory.Add("plant_fiber", 3);
		CraftingResult hammerResult = new CraftingService(services.ContentDatabase, inventory, services.GameSession.WeaponProgression, services.TechnologyUnlocks, services.GameSession.PlayerEquipment).TryCraft("craft_hammer", CraftingStationType.Workbench);
		yield return null;
		Assert.That<bool>(hammerResult.Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<WeaponData>(player.Combat.ActiveWeapon, (IResolveConstraint)(object)Is.SameAs((object)hammer));
		Assert.That<string>(services.GameSession.ActiveWeaponId, (IResolveConstraint)(object)Is.EqualTo((object)"hammer"));
		Assert.That<bool>(services.GameSession.PlayerEquipment.TryGetSlot(EquipmentSlot.Weapon1, out var equippedHammer), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(equippedHammer.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"hammer"));
		Assert.That<bool>(player.Combat.TrySelectWeapon("daggers"), (IResolveConstraint)(object)Is.False, "Dolche dürfen vor ihrer Herstellung nicht wählbar sein.", Array.Empty<object>());
		inventory.Add("wood", 3);
		inventory.Add("stone", 2);
		inventory.Add("plant_fiber", 2);
		Open(station, player, inventory);
		yield return null;
		Assert.That<bool>(window.IsOpen, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.False);
		Assert.That<int>(window.SelectedIndex, (IResolveConstraint)(object)Is.Zero);
		window.CraftButton.onClick.Invoke();
		yield return null;
		Assert.That<int>(inventory.GetTotalAmount("axe"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		window.CloseButton.onClick.Invoke();
		yield return null;
		Assert.That<bool>(window.IsOpen, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.True);
		inventory.Add("wood", 2);
		inventory.Add("stone", 2);
		inventory.Add("plant_fiber", 3);
		Open(station, player, inventory);
		Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
		Tap(keyboard.downArrowKey);
		yield return null;
		Assert.That<int>(window.SelectedIndex, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Tap(keyboard.enterKey);
		yield return null;
		Assert.That<int>(inventory.GetTotalAmount("scythe"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		Tap(keyboard.escapeKey);
		yield return null;
		InputSystem.RemoveDevice(keyboard);
		inventory.Add("wood", 2);
		inventory.Add("stone", 5);
		inventory.Add("plant_fiber", 3);
		inventory.Add("rope", 2);
		inventory.Add("copper_bar", 1);
		Open(station, player, inventory);
		Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
		yield return SelectRecipe(window, gamepad.dpad.down, RecipeIndex(window, "craft_daggers"));
		Tap(gamepad.buttonSouth);
		yield return null;
		Assert.That<bool>(services.GameSession.PlayerEquipment.TryGetSlot(EquipmentSlot.Weapon2, out var equippedDaggers), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(equippedDaggers.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"daggers"));
		Assert.That<bool>(player.Combat.TrySelectWeapon("daggers"), (IResolveConstraint)(object)Is.True);
		yield return SelectRecipe(window, gamepad.dpad.down, RecipeIndex(window, "craft_battery"));
		Tap(gamepad.buttonSouth);
		yield return null;
		Assert.That<bool>(services.GameSession.PlayerEquipment.TryGetSlot(EquipmentSlot.Battery, out var equippedBattery), (IResolveConstraint)(object)Is.True, "Die erste Batterie rückt in den freien Batterieplatz; nur Ersatzbatterien bleiben im Rucksack.", Array.Empty<object>());
		Assert.That<string>(equippedBattery.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"battery"));
		Assert.That<int>(inventory.GetTotalAmount("battery"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(services.GameSession.WeaponProgression.GetLevel("hammer"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<float>(player.Combat.GetModifiedBaseHealthDamage(hammer), (IResolveConstraint)(object)Is.EqualTo((object)hammerDamageBefore).Within((object)0.0001f));
		Assert.That<float>(player.Combat.GetModifiedBaseStaggerDamage(hammer), (IResolveConstraint)(object)Is.EqualTo((object)hammerStaggerBefore).Within((object)0.0001f));
		Assert.That<float>(player.Combat.GetModifiedBaseHealthDamage(daggers), (IResolveConstraint)(object)Is.EqualTo((object)daggerDamageBefore).Within((object)0.0001f));
		Assert.That<float>(player.Combat.GetModifiedBaseStaggerDamage(daggers), (IResolveConstraint)(object)Is.EqualTo((object)daggerStaggerBefore).Within((object)0.0001f));
		Tap(gamepad.selectButton);
		yield return null;
		Assert.That<bool>(inventoryWindow.IsOpen, (IResolveConstraint)(object)Is.False, "Inventory must stay closed while crafting owns input.", Array.Empty<object>());
		Tap(gamepad.buttonEast);
		yield return null;
		Assert.That<bool>(window.IsOpen, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.True);
		InputSystem.RemoveDevice(gamepad);
	}

	private static int RecipeIndex(CraftingWindow window, string recipeId)
	{
		for (int index = 0; index < window.VisibleRecipes.Count; index++)
		{
			if (window.VisibleRecipes[index].Id == recipeId)
			{
				return index;
			}
		}
		Assert.Fail("Rezept '" + recipeId + "' fehlt im Katalog.");
		return -1;
	}

	private IEnumerator SelectRecipe(CraftingWindow window, ButtonControl down, int targetIndex)
	{
		for (int guard = 0; guard < 64; guard++)
		{
			if (window.SelectedIndex == targetIndex)
			{
				break;
			}
			Tap(down);
			yield return null;
		}
		Assert.That<int>(window.SelectedIndex, (IResolveConstraint)(object)Is.EqualTo((object)targetIndex));
	}

	private static void Open(WorkbenchController station, PlayerPrefabBindings player, PlayerInventory inventory)
	{
		CharacterController characterController = player.CharacterController;
		characterController.enabled = false;
		player.transform.position = station.transform.position + Vector3.forward;
		characterController.enabled = true;
		station.CompleteInteraction(new InteractionContext(player.gameObject, player.transform, player.transform.forward, inventory));
	}

	private void Tap(ButtonControl button)
	{
		Press(button);
		Release(button);
	}

	private static WeaponData FindWeapon(string id)
	{
		WeaponData[] array = Resources.FindObjectsOfTypeAll<WeaponData>();
		foreach (WeaponData weapon in array)
		{
			if (weapon != null && weapon.Id == id)
			{
				return weapon;
			}
		}
		Assert.Fail("Weapon '" + id + "' is not loaded.");
		return null;
	}

	private static IEnumerator LoadHomeBase()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		AsyncOperation load = SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
		Assert.That<AsyncOperation>(load, (IResolveConstraint)(object)Is.Not.Null);
		while (!load.isDone)
		{
			yield return null;
		}
		for (int frame = 0; frame < 8; frame++)
		{
			yield return null;
		}
		EidrenServiceRoot services = UnityEngine.Object.FindFirstObjectByType<EidrenServiceRoot>();
		Assert.That<EidrenServiceRoot>(services, (IResolveConstraint)(object)Is.Not.Null);
		services.GameSession.StartNewGame();
		services.PlayerProgression.ResetForNewGame();
		services.TechnologyUnlocks.ResetForNewGame();
		services.PlayerProgression.RecordEnemyDefeated(100000);
		foreach (TechnologyNodeDefinition node in services.TechnologyTree.Nodes)
		{
			if (node.SortOrder <= 17 && services.TechnologyUnlocks.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
			{
				Assert.That<TechnologyUnlockResult>(services.TechnologyUnlocks.TryUnlock(node.Id), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success), node.Id, Array.Empty<object>());
			}
		}
		PlayerPrefabBindings playerPrefabBindings = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		PlayerInventory playerInventory = services.GameSession.PlayerInventory;
		playerInventory.Add("wood", 8);
		playerInventory.Add("stone", 6);
		UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>().PressBuildingToggle();
		playerPrefabBindings.BuildingPlacement.BeginPlacement("building.workbench");
		Assert.That<BuildingActionResult>(playerPrefabBindings.BuildingPlacement.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		playerPrefabBindings.BuildingMenu.Close();
		yield return null;
	}
}
}
