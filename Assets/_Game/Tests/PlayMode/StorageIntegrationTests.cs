using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Interaction;
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
public sealed class StorageIntegrationTests : InputTestFixture
{
	private readonly struct RuntimeReferences
	{
		public StorageContainer Container { get; }

		public StorageWindow Window { get; }

		public PlayerPrefabBindings Player { get; }

		public PlayerInputReader Input { get; }

		public EidrenServiceRoot Services { get; }

		public RuntimeReferences(StorageContainer container, StorageWindow window, PlayerPrefabBindings player, PlayerInputReader input, EidrenServiceRoot services)
		{
			Container = container;
			Window = window;
			Player = player;
			Input = input;
			Services = services;
		}
	}

	private sealed class FakeSceneLoader : ISceneLoader
	{
		public string ActiveSceneName => "HomeBase";

		public bool CanLoad(string sceneName)
		{
			return !string.IsNullOrWhiteSpace(sceneName);
		}

		public IEnumerator LoadAsync(string sceneName)
		{
			yield return null;
		}
	}

	[UnityTest]
	public IEnumerator Storage_TransfersBothDirectionsAcrossInputs()
	{
		yield return LoadHomeBase();
		RuntimeReferences references = FindReferences();
		PlayerInventory inventory = references.Services.GameSession.PlayerInventory;
		inventory.Clear();
		inventory.Add("wood", 7);
		OpenStorage(references);
		references.Window.TransferButton.onClick.Invoke();
		yield return null;
		Assert.That<int>(inventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(Total(references.Container, "wood"), (IResolveConstraint)(object)Is.EqualTo((object)7));
		references.Window.StorageSlots[0].OnPointerClick(null);
		references.Window.TransferButton.onClick.Invoke();
		yield return null;
		Assert.That<int>(inventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)7));
		references.Window.CloseButton.onClick.Invoke();
		inventory.Clear();
		inventory.Add("stone", 6);
		OpenStorage(references);
		Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
		Tap(keyboard.enterKey);
		yield return null;
		Assert.That<int>(Total(references.Container, "stone"), (IResolveConstraint)(object)Is.EqualTo((object)6));
		Tap(keyboard.escapeKey);
		InputSystem.RemoveDevice(keyboard);
		OpenStorage(references);
		Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
		for (int step = 0; step < 4; step++)
		{
			Tap(gamepad.dpad.right);
			yield return null;
		}
		Assert.That<StorageSelectionSide>(references.Window.SelectedSide, (IResolveConstraint)(object)Is.EqualTo((object)StorageSelectionSide.Storage));
		Tap(gamepad.buttonSouth);
		yield return null;
		Assert.That<int>(inventory.GetTotalAmount("stone"), (IResolveConstraint)(object)Is.EqualTo((object)6));
		Tap(gamepad.buttonEast);
		yield return null;
		Assert.That<bool>(references.Window.IsOpen, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(references.Input.GameplayEnabled, (IResolveConstraint)(object)Is.True);
		InputSystem.RemoveDevice(gamepad);
	}

	[UnityTest]
	public IEnumerator BlockingWindows_CloseForDistanceAndSceneChange()
	{
		yield return LoadHomeBase();
		RuntimeReferences references = FindReferences();
		CraftingWindow crafting = Object.FindFirstObjectByType<CraftingWindow>(FindObjectsInactive.Include);
		WorkbenchController workbench = Object.FindFirstObjectByType<WorkbenchController>();
		Assert.That<CraftingWindow>(crafting, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<WorkbenchController>(workbench, (IResolveConstraint)(object)Is.Not.Null);
		MovePlayer(references.Player, workbench.transform.position + Vector3.forward);
		workbench.CompleteInteraction(Context(references.Player, references.Services.GameSession.PlayerInventory));
		yield return null;
		Assert.That<bool>(crafting.IsOpen, (IResolveConstraint)(object)Is.True);
		Assert.That<CraftingStationType>(crafting.CurrentStation, (IResolveConstraint)(object)Is.EqualTo((object)CraftingStationType.Workbench));
		MovePlayer(references.Player, workbench.transform.position + Vector3.right * 8f);
		yield return null;
		Assert.That<bool>(crafting.IsOpen, (IResolveConstraint)(object)Is.False);
		MovePlayer(references.Player, references.Container.transform.position + Vector3.forward);
		references.Container.CompleteInteraction(Context(references.Player, references.Services.GameSession.PlayerInventory));
		yield return null;
		Assert.That<bool>(references.Window.IsOpen, (IResolveConstraint)(object)Is.True);
		crafting.Open(workbench);
		Assert.That<bool>(crafting.IsOpen, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(references.Window.IsOpen, (IResolveConstraint)(object)Is.True);
		bool closedAtTransitionStart = false;
		references.Services.SceneFlowService.SceneTransitionStarted += delegate
		{
			closedAtTransitionStart = !references.Window.IsOpen;
		};
		references.Services.SceneFlowService.Configure(null, new FakeSceneLoader());
		Assert.That<bool>(references.Services.SceneFlowService.TryLoadScene("WorldMap"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(closedAtTransitionStart, (IResolveConstraint)(object)Is.True);
		while (references.Services.SceneFlowService.IsTransitioning)
		{
			yield return null;
		}
	}

	private static RuntimeReferences FindReferences()
	{
		RuntimeReferences result = new RuntimeReferences(Object.FindFirstObjectByType<StorageContainer>(), Object.FindFirstObjectByType<StorageWindow>(FindObjectsInactive.Include), Object.FindFirstObjectByType<PlayerPrefabBindings>(), Object.FindFirstObjectByType<PlayerInputReader>(), Object.FindFirstObjectByType<EidrenServiceRoot>());
		Assert.That<StorageContainer>(result.Container, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<StorageWindow>(result.Window, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerPrefabBindings>(result.Player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerInputReader>(result.Input, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<EidrenServiceRoot>(result.Services, (IResolveConstraint)(object)Is.Not.Null);
		return result;
	}

	private static void OpenStorage(RuntimeReferences references)
	{
		MovePlayer(references.Player, references.Container.transform.position + Vector3.forward);
		references.Container.CompleteInteraction(Context(references.Player, references.Services.GameSession.PlayerInventory));
	}

	private static InteractionContext Context(PlayerPrefabBindings player, PlayerInventory inventory)
	{
		return new InteractionContext(player.gameObject, player.transform, player.transform.forward, inventory);
	}

	private static void MovePlayer(PlayerPrefabBindings player, Vector3 position)
	{
		CharacterController characterController = player.CharacterController;
		characterController.enabled = false;
		player.transform.position = position;
		characterController.enabled = true;
	}

	private static int Total(IItemContainer container, string itemId)
	{
		int total = 0;
		for (int index = 0; index < container.SlotCapacity; index++)
		{
			if (container.TryGetSlot(index, out var stack) && stack.ItemId == itemId)
			{
				total += stack.Quantity;
			}
		}
		return total;
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
		AsyncOperation load = SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
		while (!load.isDone)
		{
			yield return null;
		}
		for (int frame = 0; frame < 8; frame++)
		{
			yield return null;
		}
		EidrenServiceRoot services = Object.FindFirstObjectByType<EidrenServiceRoot>();
		services.GameSession.StartNewGame();
		services.PlayerProgression.ResetForNewGame();
		services.TechnologyUnlocks.ResetForNewGame();
		services.PlayerProgression.RecordEnemyDefeated(100000);
		foreach (TechnologyNodeDefinition node in services.TechnologyTree.Nodes)
		{
			if (node.SortOrder <= 17 && services.TechnologyUnlocks.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
			{
				Assert.That<TechnologyUnlockResult>(services.TechnologyUnlocks.TryUnlock(node.Id), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success));
			}
		}
		PlayerInventory playerInventory = services.GameSession.PlayerInventory;
		playerInventory.Add("wood", 18);
		playerInventory.Add("stone", 10);
		PlayerPrefabBindings playerPrefabBindings = Object.FindFirstObjectByType<PlayerPrefabBindings>();
		Object.FindFirstObjectByType<PlayerInputReader>().PressBuildingToggle();
		playerPrefabBindings.BuildingPlacement.BeginPlacement("building.workbench");
		Assert.That<BuildingActionResult>(playerPrefabBindings.BuildingPlacement.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		playerPrefabBindings.BuildingPlacement.BeginPlacement("building.storage_chest");
		playerPrefabBindings.BuildingPlacement.MoveCandidate(Vector2.right * 3f);
		Assert.That<BuildingActionResult>(playerPrefabBindings.BuildingPlacement.ConfirmPlacement(), (IResolveConstraint)(object)Is.EqualTo((object)BuildingActionResult.Success));
		playerPrefabBindings.BuildingMenu.Close();
		yield return null;
	}
}
}
