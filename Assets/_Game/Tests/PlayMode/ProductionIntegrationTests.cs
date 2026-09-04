using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class ProductionIntegrationTests
{
	private const string FarmInstanceId = "integration.farm";

	private const string PotInstanceId = "integration.cooking_pot";

	[UnityTest]
	public IEnumerator SeedsHomecomingHarvestAndBreadUseRealComponents()
	{
		yield return LoadFreshHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		UnlockAll(services);
		Assert.That<bool>(services.GameSession.Buildings.TryAdd(new BuildingInstanceState("integration.farm", "building.farm_plot", new Vector3(5f, 0f, 4f), 0, 1)), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(services.GameSession.Buildings.TryAdd(new BuildingInstanceState("integration.cooking_pot", "building.cooking_pot", new Vector3(-5f, 0f, 4f), 0, 1)), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(services.SceneFlowService.TryReloadActiveScene(), (IResolveConstraint)(object)Is.True);
		yield return WaitForTransition(services);
		yield return WaitForPlayer();
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		FarmPlotController farm = UnityEngine.Object.FindFirstObjectByType<FarmPlotController>();
		Assert.That<FarmPlotController>(farm, (IResolveConstraint)(object)Is.Not.Null);
		PlayerInventory inventory = services.GameSession.PlayerInventory;
		inventory.Add("wheat_seed", 4);
		InteractionContext farmContext = new InteractionContext(player.gameObject, player.transform, player.transform.forward, inventory);
		for (int slot = 0; slot < 4; slot++)
		{
			Assert.That<bool>(farm.CanInteract(in farmContext, out var reason), (IResolveConstraint)(object)Is.True, reason, Array.Empty<object>());
			farm.CompleteInteraction(in farmContext);
		}
		AssertFarmState(services, 4, 0);
		for (int frame = 0; frame < 8; frame++)
		{
			yield return null;
		}
		AssertFarmState(services, 4, 0);
		ZoneRegenerationCoordinator zoneRegenerationCoordinator = UnityEngine.Object.FindFirstObjectByType<ZoneRegenerationCoordinator>();
		Assert.That<ZoneRegenerationCoordinator>(zoneRegenerationCoordinator, (IResolveConstraint)(object)Is.Not.Null);
		zoneRegenerationCoordinator.RegenerateForHomecoming();
		yield return null;
		AssertFarmState(services, 0, 12);
		farm = UnityEngine.Object.FindFirstObjectByType<FarmPlotController>();
		farm.CompleteInteraction(new InteractionContext(player.gameObject, player.transform, player.transform.forward, inventory));
		Assert.That<int>(inventory.GetTotalAmount("wheat"), (IResolveConstraint)(object)Is.EqualTo((object)12));
		Assert.That<int>(inventory.GetTotalAmount("wheat_seed"), (IResolveConstraint)(object)Is.Zero);
		WorkbenchController cookingPot = UnityEngine.Object.FindObjectsByType<WorkbenchController>(FindObjectsSortMode.None).Single((WorkbenchController value) => value.Station == CraftingStationType.CookingPot);
		MovePlayer(player, cookingPot.transform.position + Vector3.forward);
		cookingPot.CompleteInteraction(new InteractionContext(player.gameObject, player.transform, player.transform.forward, inventory));
		yield return null;
		Assert.That<bool>(player.CraftingWindow.IsOpen, (IResolveConstraint)(object)Is.True);
		Assert.That<CraftingStationType>(player.CraftingWindow.CurrentStation, (IResolveConstraint)(object)Is.EqualTo((object)CraftingStationType.CookingPot));
		int breadIndex = RecipeIndex(player.CraftingWindow, "craft_bread");
		player.CraftingWindow.SelectRecipe(breadIndex);
		Assert.That<bool>(player.CraftingWindow.CraftSelected().Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(player.CraftingWindow.CraftSelected().Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(inventory.GetTotalAmount("buff_food"), (IResolveConstraint)(object)Is.EqualTo((object)4));
		Assert.That<int>(inventory.GetTotalAmount("wheat"), (IResolveConstraint)(object)Is.Zero);
	}

	private static void UnlockAll(EidrenServiceRoot services)
	{
		services.PlayerProgression.RecordEnemyDefeated(100000);
		foreach (TechnologyNodeDefinition node in services.TechnologyTree.Nodes)
		{
			if (node.SortOrder <= 17 && services.TechnologyUnlocks.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
			{
				Assert.That<TechnologyUnlockResult>(services.TechnologyUnlocks.TryUnlock(node.Id), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success), node.Id, Array.Empty<object>());
			}
		}
	}

	private static void AssertFarmState(EidrenServiceRoot services, int planted, int ready)
	{
		Assert.That<bool>(services.GameSession.FarmProduction.TryGetFarm("integration.farm", out var state), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(state.PlantedSlots, (IResolveConstraint)(object)Is.EqualTo((object)planted));
		Assert.That<int>(state.ReadyOutput, (IResolveConstraint)(object)Is.EqualTo((object)ready));
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
		Assert.Fail("Recipe '" + recipeId + "' is missing.");
		return -1;
	}

	private static IEnumerator LoadFreshHomeBase()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		yield return SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
		yield return WaitForPlayer();
		EidrenServiceRoot instance = EidrenServiceRoot.Instance;
		instance.GameSession.StartNewGame();
		instance.PlayerProgression.ResetForNewGame();
		instance.TechnologyUnlocks.ResetForNewGame();
	}

	private static IEnumerator WaitForTransition(EidrenServiceRoot services)
	{
		while (services.SceneFlowService.IsTransitioning)
		{
			yield return null;
		}
	}

	private static IEnumerator WaitForPlayer()
	{
		for (int frame = 0; frame < 240; frame++)
		{
			PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
			if (player != null && player.CraftingWindow != null && player.BuildingPlacement != null)
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail("HomeBase player composition did not initialize.");
	}

	private static void MovePlayer(PlayerPrefabBindings player, Vector3 position)
	{
		CharacterController characterController = player.CharacterController;
		characterController.enabled = false;
		player.transform.position = position;
		characterController.enabled = true;
	}
}
}
