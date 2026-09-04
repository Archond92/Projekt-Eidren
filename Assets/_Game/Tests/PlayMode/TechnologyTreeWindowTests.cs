using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.UI;
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
public sealed class TechnologyTreeWindowTests
{
	[UnityTest]
	public IEnumerator LevelTwo_UnlocksWorkbenchThroughAuthoredWindow()
	{
		yield return LoadFreshHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		TechnologyTreeWindow window = player.TechnologyWindow;
		Assert.That<TechnologyTreeWindow>(window, (IResolveConstraint)(object)Is.Not.Null);
		services.PlayerProgression.RecordEnemyDefeated(175);
		UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>().PressTechnologyToggle();
		yield return null;
		Assert.That<bool>(window.IsOpen, (IResolveConstraint)(object)Is.True);
		int workbenchIndex = Array.FindIndex(window.NodeViews, (TechnologyNodeButtonView view) => view.NodeId == "technology.01.workbench");
		Assert.That<int>(workbenchIndex, (IResolveConstraint)(object)Is.EqualTo((object)3));
		window.SelectNode(workbenchIndex);
		Assert.That<TechnologyNodeState>(window.NodeViews[workbenchIndex].State, (IResolveConstraint)(object)Is.EqualTo((object)TechnologyNodeState.Available));
		Assert.That<bool>(window.UnlockButton.interactable, (IResolveConstraint)(object)Is.True);
		window.UnlockButton.onClick.Invoke();
		yield return null;
		Assert.That<bool>(services.TechnologyUnlocks.IsBuildingUnlocked("building.workbench"), (IResolveConstraint)(object)Is.True);
		Assert.That<TechnologyNodeState>(window.NodeViews[workbenchIndex].State, (IResolveConstraint)(object)Is.EqualTo((object)TechnologyNodeState.Unlocked));
		Assert.That<int>(services.PlayerProgression.State.AvailableTechnologyPoints, (IResolveConstraint)(object)Is.Zero);
		window.Close();
	}

	[UnityTest]
	public IEnumerator LockedRecipe_IsDisabledAndRejectedByWindow()
	{
		yield return LoadFreshHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		CraftingWindow window = player.CraftingWindow;
		PlayerInventory inventory = services.GameSession.PlayerInventory;
		inventory.Add("wood", 4);
		inventory.Add("stone", 3);
		inventory.Add("plant_fiber", 3);
		window.Open(CraftingStationType.Workbench);
		yield return null;
		int hammerIndex = FindRecipeIndex(window, "craft_hammer");
		window.SelectRecipe(hammerIndex);
		Assert.That<string>(window.VisibleRecipes[window.SelectedIndex].Id, (IResolveConstraint)(object)Is.EqualTo((object)"craft_hammer"));
		Assert.That<bool>(window.CraftButton.interactable, (IResolveConstraint)(object)Is.False);
		Assert.That<CraftingResultCode>(window.CraftSelected().Code, (IResolveConstraint)(object)Is.EqualTo((object)CraftingResultCode.RecipeLocked));
		Assert.That<int>(inventory.GetTotalAmount("hammer"), (IResolveConstraint)(object)Is.Zero);
		window.Close();
	}

	private static int FindRecipeIndex(CraftingWindow window, string recipeId)
	{
		for (int index = 0; index < window.VisibleRecipes.Count; index++)
		{
			if (window.VisibleRecipes[index].Id == recipeId)
			{
				return index;
			}
		}
		Assert.Fail("Recipe '" + recipeId + "' is not visible.");
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
		float deadline = Time.realtimeSinceStartup + 12f;
		while (Time.realtimeSinceStartup < deadline)
		{
			PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
			if (player != null && player.TechnologyWindow != null && player.CraftingWindow != null)
			{
				EidrenServiceRoot instance = EidrenServiceRoot.Instance;
				instance.GameSession.StartNewGame();
				instance.PlayerProgression.ResetForNewGame();
				instance.TechnologyUnlocks.ResetForNewGame();
				yield break;
			}
			yield return null;
		}
		Assert.Fail("HomeBase technology composition did not initialize.");
	}
}
}
