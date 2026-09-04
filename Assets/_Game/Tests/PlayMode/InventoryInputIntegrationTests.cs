using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Input;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
public sealed class InventoryInputIntegrationTests : InputTestFixture
{
	[UnityTest]
	public IEnumerator Inventory_OpensAndClosesWithKeyboardGamepadAndTouch()
	{
		yield return LoadHomeBase();
		PlayerInputReader input = Object.FindFirstObjectByType<PlayerInputReader>();
		InventoryWindow window = Object.FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);
		PlayerPrefabBindings player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
		Assert.That<PlayerInputReader>(input, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<InventoryWindow>(window, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(input.HasInventoryBinding("<Keyboard>/i"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(input.HasInventoryBinding("<Gamepad>/select"), (IResolveConstraint)(object)Is.True);
		Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
		Tap(keyboard.iKey);
		yield return null;
		Assert.That<bool>(window.IsOpen, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(player.Motor.IsDodging, (IResolveConstraint)(object)Is.False);
		Tap(keyboard.rightArrowKey);
		yield return null;
		Assert.That<int>(window.SelectedIndex, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Tap(keyboard.leftArrowKey);
		yield return null;
		Assert.That<int>(window.SelectedIndex, (IResolveConstraint)(object)Is.Zero);
		Tap(keyboard.escapeKey);
		yield return null;
		Assert.That<bool>(window.IsOpen, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.True);
		InputSystem.RemoveDevice(keyboard);
		Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
		Tap(gamepad.selectButton);
		yield return null;
		Assert.That<bool>(window.IsOpen, (IResolveConstraint)(object)Is.True);
		Tap(gamepad.dpad.right);
		yield return null;
		Assert.That<int>(window.SelectedIndex, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Tap(gamepad.dpad.left);
		yield return null;
		Assert.That<int>(window.SelectedIndex, (IResolveConstraint)(object)Is.Zero);
		Tap(gamepad.buttonEast);
		yield return null;
		Assert.That<bool>(window.IsOpen, (IResolveConstraint)(object)Is.False);
		InputSystem.RemoveDevice(gamepad);
		window.OpenButton.onClick.Invoke();
		yield return null;
		Assert.That<bool>(window.IsOpen, (IResolveConstraint)(object)Is.True);
		window.CloseButton.onClick.Invoke();
		yield return null;
		Assert.That<bool>(window.IsOpen, (IResolveConstraint)(object)Is.False);
	}

	[UnityTest]
	public IEnumerator Inventory_ItemUseWorksThroughTouchKeyboardAndGamepad()
	{
		yield return LoadHomeBase();
		PlayerInputReader playerInputReader = Object.FindFirstObjectByType<PlayerInputReader>();
		InventoryWindow window = Object.FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);
		PlayerPrefabBindings player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
		EidrenServiceRoot services = Object.FindFirstObjectByType<EidrenServiceRoot>();
		Assert.That<PlayerInputReader>(playerInputReader, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<InventoryWindow>(window, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<EidrenServiceRoot>(services, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<int>(services.GameSession.PlayerInventory.Add("healing_potion", 3), (IResolveConstraint)(object)Is.Zero);
		DamagePlayer(player);
		int initial = services.GameSession.PlayerInventory.GetTotalAmount("healing_potion");
		window.OpenButton.onClick.Invoke();
		window.Slots[0].OnPointerClick(null);
		window.Slots[0].OnPointerClick(null);
		yield return null;
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)(initial - 1)));
		window.Close();
		DamagePlayer(player);
		Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
		Tap(keyboard.iKey);
		yield return null;
		Tap(keyboard.spaceKey);
		yield return null;
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)(initial - 2)));
		Tap(keyboard.escapeKey);
		yield return null;
		InputSystem.RemoveDevice(keyboard);
		DamagePlayer(player);
		Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
		Tap(gamepad.selectButton);
		yield return null;
		Tap(gamepad.buttonSouth);
		yield return null;
		Assert.That<int>(services.GameSession.PlayerInventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)(initial - 3)));
		InputSystem.RemoveDevice(gamepad);
	}

	[UnityTest]
	public IEnumerator Inventory_DragAndDropMovesAndSwapsSlots()
	{
		yield return LoadHomeBase();
		InventoryWindow window = Object.FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);
		EidrenServiceRoot services = Object.FindFirstObjectByType<EidrenServiceRoot>();
		PlayerInventory inventory = services.GameSession.PlayerInventory;
		inventory.Add("axe", 1);
		inventory.Add("scythe", 1);
		window.OpenButton.onClick.Invoke();
		yield return null;
		window.Slots[0].OnBeginDrag(null);
		Assert.That<bool>(window.IsDragging, (IResolveConstraint)(object)Is.True);
		window.Slots[1].OnDrop(null);
		window.Slots[0].OnEndDrag(null);
		yield return null;
		Assert.That<bool>(window.IsDragging, (IResolveConstraint)(object)Is.False);
		inventory.TryGetSlot(0, out var first);
		inventory.TryGetSlot(1, out var second);
		Assert.That<string>(first.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"scythe"));
		Assert.That<string>(second.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"axe"));
	}

	private static void DamagePlayer(PlayerPrefabBindings player)
	{
		player.Damageable.ApplyDamage(new DamageInfo(60f, 0f, player.transform.position, player.gameObject, isBackAttack: false, "test.inventory.damage", "test"));
	}

	private void Tap(ButtonControl button)
	{
		Press(button);
		Release(button);
	}

	private static IEnumerator LoadHomeBase()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		EidrenServiceRoot.FindOrCreate().GameSession.StartNewGame();
		AsyncOperation load = SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
		Assert.That<AsyncOperation>(load, (IResolveConstraint)(object)Is.Not.Null);
		while (!load.isDone)
		{
			yield return null;
		}
		for (int frame = 0; frame < 4; frame++)
		{
			yield return null;
		}
		Assert.That<EidrenServiceRoot>(EidrenServiceRoot.Instance, (IResolveConstraint)(object)Is.Not.Null);
	}
}
}
