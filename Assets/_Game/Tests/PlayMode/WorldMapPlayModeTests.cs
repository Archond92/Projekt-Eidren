using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using System.Reflection;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class WorldMapPlayModeTests
{
	private sealed class ImmediateSceneLoader : ISceneLoader
	{
		public int RequestCount;

		public string LastRequestedScene;

		public string ActiveSceneName => "WorldMap";

		public bool CanLoad(string sceneName)
		{
			return true;
		}

		public IEnumerator LoadAsync(string sceneName)
		{
			RequestCount++;
			LastRequestedScene = sceneName;
			yield return null;
		}
	}

	private sealed class BlockingSceneLoader : ISceneLoader
	{
		public bool Complete;

		public int RequestCount;

		public string LastRequestedScene;

		public string ActiveSceneName => "WorldMap";

		public bool CanLoad(string sceneName)
		{
			return true;
		}

		public IEnumerator LoadAsync(string sceneName)
		{
			RequestCount++;
			LastRequestedScene = sceneName;
			while (!Complete)
			{
				yield return null;
			}
		}
	}

	[TearDown]
	public void RestoreTimeScale()
	{
		Time.timeScale = 1f;
	}

	[UnityTest]
	public IEnumerator WorldMap_BindsAndSelectsAllEightDataNodes()
	{
		yield return LoadWorldMap();
		WorldMapController controller = UnityEngine.Object.FindFirstObjectByType<WorldMapController>();
		Assert.That<WorldMapController>(controller, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<int>(controller.BoundNodeCount, (IResolveConstraint)(object)Is.EqualTo((object)8));
		Assert.That<WorldMapNodeView[]>(UnityEngine.Object.FindObjectsByType<WorldMapNodeView>(FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)8));
		foreach (WorldMapNodeDefinition node in controller.Definition.Nodes.Where((WorldMapNodeDefinition candidate) => candidate.InitiallyAvailable))
		{
			Assert.That<bool>(controller.SelectNodeById(node.Id), (IResolveConstraint)(object)Is.True, node.Id, Array.Empty<object>());
			Assert.That<WorldMapNodeDefinition>(controller.SelectedNode, (IResolveConstraint)(object)Is.SameAs((object)node));
			Assert.That<string>(controller.InfoPanel.DisplayedNodeId, (IResolveConstraint)(object)Is.EqualTo((object)node.Id));
		}
		yield return Cleanup();
	}

	[UnityTest]
	public IEnumerator AvailableNodes_RequestTheirConfiguredSceneKeys()
	{
		yield return LoadWorldMap();
		WorldMapController controller = UnityEngine.Object.FindFirstObjectByType<WorldMapController>();
		ImmediateSceneLoader loader = new ImmediateSceneLoader();
		EidrenServiceRoot.Instance.SceneFlowService.Configure(null, loader);
		foreach (WorldMapNodeDefinition node in controller.Definition.Nodes.Where((WorldMapNodeDefinition candidate) => candidate.InitiallyAvailable))
		{
			Assert.That<bool>(controller.SelectNodeById(node.Id), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(controller.TryTravelSelected(), (IResolveConstraint)(object)Is.True, node.Id, Array.Empty<object>());
			yield return null;
			yield return null;
			Assert.That<string>(loader.LastRequestedScene, (IResolveConstraint)(object)Is.EqualTo((object)node.SceneKey), node.Id, Array.Empty<object>());
			Assert.That<string>(EidrenServiceRoot.Instance.GameSession.CurrentWorldMapNodeId, (IResolveConstraint)(object)Is.EqualTo((object)node.Id), node.Id, Array.Empty<object>());
			Assert.That<bool>(controller.TravelRequested, (IResolveConstraint)(object)Is.False);
		}
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.EqualTo((object)5));
		yield return Cleanup();
	}

	[UnityTest]
	public IEnumerator TierTwoTravel_RequiresExplicitTierTwoFlag()
	{
		yield return LoadWorldMap();
		WorldMapController worldMapController = UnityEngine.Object.FindFirstObjectByType<WorldMapController>();
		ImmediateSceneLoader loader = new ImmediateSceneLoader();
		EidrenServiceRoot.Instance.SceneFlowService.Configure(null, loader);
		GameSession session = EidrenServiceRoot.Instance.GameSession;
		Assert.That<bool>(worldMapController.SelectNodeById("zone_twilight_grove"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(worldMapController.TryTravelSelected(), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.Zero);
		session.TrySetProgressFlag("garon_defeated");
		worldMapController.SelectNodeById("zone_twilight_grove");
		Assert.That<bool>(worldMapController.TryTravelSelected(), (IResolveConstraint)(object)Is.False, "Garons Sieg darf T2 allein nicht freischalten.", Array.Empty<object>());
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.Zero);
		session.TrySetProgressFlag("tier_2_unlocked");
		worldMapController.SelectNodeById("zone_twilight_grove");
		Assert.That<bool>(worldMapController.TryTravelSelected(), (IResolveConstraint)(object)Is.True);
		yield return null;
		yield return null;
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<string>(loader.LastRequestedScene, (IResolveConstraint)(object)Is.EqualTo((object)"Zone_TwilightGrove"));
		yield return Cleanup();
	}

	[UnityTest]
	public IEnumerator TravelButtonClick_RequestsSelectedScene()
	{
		yield return LoadWorldMap();
		WorldMapController controller = UnityEngine.Object.FindFirstObjectByType<WorldMapController>();
		ImmediateSceneLoader loader = new ImmediateSceneLoader();
		EidrenServiceRoot.Instance.SceneFlowService.Configure(null, loader);
		Assert.That<bool>(controller.SelectNodeById("zone_greenwood"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(controller.View.TravelButton.IsAvailable, (IResolveConstraint)(object)Is.True);
		Assert.That<string>(controller.View.TravelButton.DisplayedLabel, (IResolveConstraint)(object)Is.EqualTo((object)"REISEN"));
		Assert.That<Color>(controller.View.TravelButton.LabelColor, (IResolveConstraint)(object)Is.EqualTo((object)controller.Definition.Theme.TextPrimary));
		controller.View.TravelButton.Button.onClick.Invoke();
		yield return null;
		yield return null;
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<string>(loader.LastRequestedScene, (IResolveConstraint)(object)Is.EqualTo((object)"Zone_Greenwood"));
		yield return Cleanup();
	}

	[UnityTest]
	public IEnumerator ParallelTravelRequest_IsBlocked()
	{
		yield return LoadWorldMap();
		WorldMapController worldMapController = UnityEngine.Object.FindFirstObjectByType<WorldMapController>();
		BlockingSceneLoader loader = new BlockingSceneLoader();
		EidrenServiceRoot.Instance.SceneFlowService.Configure(null, loader);
		Assert.That<bool>(worldMapController.SelectNodeById("zone_greenwood"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(worldMapController.TryTravelSelected(), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(worldMapController.TryTravelSelected(), (IResolveConstraint)(object)Is.False);
		yield return null;
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<string>(loader.LastRequestedScene, (IResolveConstraint)(object)Is.EqualTo((object)"Zone_Greenwood"));
		loader.Complete = true;
		yield return null;
		yield return null;
		yield return Cleanup();
	}

	[UnityTest]
	public IEnumerator LockedNode_CannotBeTravelled()
	{
		yield return LoadWorldMap();
		WorldMapController controller = UnityEngine.Object.FindFirstObjectByType<WorldMapController>();
		WorldMapNodeDefinition node = controller.Definition.Nodes.Single((WorldMapNodeDefinition candidate) => candidate.Id == "zone_quarry");
		FieldInfo availableField = typeof(WorldMapNodeDefinition).GetField("initiallyAvailable", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That<FieldInfo>(availableField, (IResolveConstraint)(object)Is.Not.Null);
		availableField.SetValue(node, false);
		try
		{
			Assert.That<bool>(controller.SelectNode(node), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(controller.TryTravelSelected(), (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			availableField.SetValue(node, true);
		}
		yield return Cleanup();
	}

	[UnityTest]
	public IEnumerator DisabledPlaceholders_DoNotRequestTravel()
	{
		yield return LoadWorldMap();
		WorldMapController controller = UnityEngine.Object.FindFirstObjectByType<WorldMapController>();
		Assert.That<WorldMapController>(controller, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(controller.SelectNodeById("zone_greenwood"), (IResolveConstraint)(object)Is.True);
		Button[] placeholderButtons = controller.View.QuickActions.PlaceholderButtons;
		foreach (Button obj in placeholderButtons)
		{
			Assert.That<bool>(obj.interactable, (IResolveConstraint)(object)Is.False);
			obj.onClick.Invoke();
		}
		placeholderButtons = controller.View.SideMenu.PlaceholderButtons;
		foreach (Button obj2 in placeholderButtons)
		{
			Assert.That<bool>(obj2.interactable, (IResolveConstraint)(object)Is.False);
			obj2.onClick.Invoke();
		}
		yield return null;
		Assert.That<bool>(controller.TravelRequested, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(EidrenServiceRoot.Instance.GameSession.IsWorldTravelPending, (IResolveConstraint)(object)Is.False);
		yield return Cleanup();
	}

	[UnityTest]
	public IEnumerator ResponsiveLayoutAndPanelMotion_WorkInLiveCanvas()
	{
		yield return LoadWorldMap();
		WorldMapController controller = UnityEngine.Object.FindFirstObjectByType<WorldMapController>();
		WorldMapResponsiveLayout layout = controller.View.ResponsiveLayout;
		(float, WorldMapInfoPanelPlacement)[] array = new(float, WorldMapInfoPanelPlacement)[4]
		{
			(1.7777778f, WorldMapInfoPanelPlacement.Bottom),
			(2f, WorldMapInfoPanelPlacement.Side),
			(2.1666667f, WorldMapInfoPanelPlacement.Side),
			(1.3333334f, WorldMapInfoPanelPlacement.Bottom)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(float, WorldMapInfoPanelPlacement) value = array[i];
			layout.ApplyForAspect(value.Item1);
			Assert.That<WorldMapInfoPanelPlacement>(layout.CurrentPlacement, (IResolveConstraint)(object)Is.EqualTo((object)value.Item2), value.Item1.ToString("0.00"), Array.Empty<object>());
			RectTransform travel = controller.View.TravelButton.GetComponent<RectTransform>();
			Assert.That<float>(travel.anchorMax.y - travel.anchorMin.y, (IResolveConstraint)(object)((value.Item2 == WorldMapInfoPanelPlacement.Bottom) ? Is.GreaterThanOrEqualTo((object)0.2f) : Is.GreaterThanOrEqualTo((object)0.05f)), "Travel button is too small at aspect " + $"{value.Item1:0.00}.", Array.Empty<object>());
		}
		Time.timeScale = 0f;
		controller.InfoPanel.Close();
		yield return WaitRealtime(0.32f);
		Assert.That<bool>(controller.InfoPanel.IsOpen, (IResolveConstraint)(object)Is.False);
		controller.InfoPanel.Open();
		yield return WaitRealtime(0.32f);
		Assert.That<bool>(controller.InfoPanel.IsOpen, (IResolveConstraint)(object)Is.True);
		Time.timeScale = 1f;
		yield return Cleanup();
	}

	[UnityTest]
	public IEnumerator AllEightDestinationScenes_AreStructuredAndReturnToMap()
	{
		string[] scenes = new string[8] { "HomeBase", "Zone_Greenwood", "Zone_Quarry", "Zone_Marsh", "Zone_EmberRuins", "Zone_TwilightGrove", "Zone_VeilMarsh", "Zone_GreyRifts" };
		string[] array = scenes;
		foreach (string sceneName in array)
		{
			yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
			yield return null;
			Assert.That<GameObject>(GameObject.Find("ZoneRoot"), (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			Assert.That<GameObject>(GameObject.Find("ZoneRoot/EnvironmentRoot/WalkableGround"), (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			Assert.That<GameObject>(GameObject.Find("ZoneRoot/PlayerSpawnPoints/Spawn_Default"), (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			Assert.That<Camera>(Camera.main, (IResolveConstraint)(object)Is.Not.Null, sceneName, Array.Empty<object>());
			MapExitVolume[] array2 = UnityEngine.Object.FindObjectsByType<MapExitVolume>(FindObjectsSortMode.None);
			Assert.That<MapExitVolume[]>(array2, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)4), sceneName, Array.Empty<object>());
			Assert.That<bool>(array2.All((MapExitVolume exit) => exit.TargetScene == "WorldMap"), (IResolveConstraint)(object)Is.True, sceneName, Array.Empty<object>());
		}
		yield return Cleanup();
	}

	private static IEnumerator LoadWorldMap()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		yield return SceneManager.LoadSceneAsync("WorldMap", LoadSceneMode.Single);
		yield return null;
		Assert.That<WorldMapController>(UnityEngine.Object.FindFirstObjectByType<WorldMapController>(), (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<EidrenServiceRoot>(EidrenServiceRoot.Instance, (IResolveConstraint)(object)Is.Not.Null);
	}

	private static IEnumerator Cleanup()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
		}
		SceneManager.SetActiveScene(SceneManager.CreateScene("WorldMapTestCleanup"));
		yield return null;
	}

	private static IEnumerator WaitRealtime(float seconds)
	{
		float deadline = Time.realtimeSinceStartup + seconds;
		while (Time.realtimeSinceStartup < deadline)
		{
			yield return null;
		}
	}
}
}
