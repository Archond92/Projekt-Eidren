using Eidren.Composition;
using Eidren.Core.Services;
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
public sealed class SceneFlowPlayModeTests
{
	private sealed class BlockingSceneLoader : ISceneLoader
	{
		public bool Complete;

		public int RequestCount;

		public string ActiveSceneName => "First";

		public bool CanLoad(string sceneName)
		{
			return true;
		}

		public IEnumerator LoadAsync(string sceneName)
		{
			RequestCount++;
			while (!Complete)
			{
				yield return null;
			}
		}
	}

	private sealed class ReplacingSceneLoader : ISceneLoader
	{
		private readonly SceneFlowService _service;

		private readonly ISceneTransitionView _replacement;

		private bool _replaced;

		public string ActiveSceneName => "Test";

		public ReplacingSceneLoader(SceneFlowService service, ISceneTransitionView replacement)
		{
			_service = service;
			_replacement = replacement;
		}

		public bool CanLoad(string sceneName)
		{
			return true;
		}

		public IEnumerator LoadAsync(string sceneName)
		{
			if (!_replaced)
			{
				_replaced = true;
				_service.SetTransitionView(_replacement);
			}
			yield return null;
		}
	}

	private sealed class RecordingTransitionView : ISceneTransitionView
	{
		public int FadeOutCount { get; private set; }

		public int FadeInCount { get; private set; }

		public bool IsBlocked { get; private set; }

		public void SetInputBlocked(bool blocked)
		{
			IsBlocked = blocked;
		}

		public IEnumerator FadeOut()
		{
			FadeOutCount++;
			yield return null;
		}

		public IEnumerator FadeIn()
		{
			FadeInCount++;
			yield return null;
		}
	}

	[UnityTest]
	public IEnumerator SceneFlow_RejectsParallelLoadRequests()
	{
		GameObject root = new GameObject("SceneFlowParallel_Test");
		BlockingSceneLoader loader = new BlockingSceneLoader();
		SceneFlowService service = root.AddComponent<SceneFlowService>();
		service.Configure(null, loader);
		Assert.That<bool>(service.TryLoadScene("First"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(service.IsTransitioning, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(service.TryLoadScene("Second"), (IResolveConstraint)(object)Is.False);
		yield return null;
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		loader.Complete = true;
		yield return WaitUntil(() => !service.IsTransitioning, "SceneFlowService did not complete its fake transition.");
		UnityEngine.Object.Destroy(root);
		yield return null;
	}

	[UnityTest]
	public IEnumerator SceneFlow_ViewChangedDuringLoad_FinishesOriginalFade()
	{
		GameObject root = new GameObject("SceneFlowViewSwap_Test");
		RecordingTransitionView original = new RecordingTransitionView();
		RecordingTransitionView worldMap = new RecordingTransitionView();
		SceneFlowService service = root.AddComponent<SceneFlowService>();
		ReplacingSceneLoader loader = new ReplacingSceneLoader(service, worldMap);
		service.Configure(original, loader);
		Assert.That<bool>(service.TryLoadScene("WorldMap"), (IResolveConstraint)(object)Is.True);
		yield return WaitUntil(() => !service.IsTransitioning, "Transition with a swapped view did not complete.");
		Assert.That<int>(original.FadeOutCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(original.FadeInCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(original.IsBlocked, (IResolveConstraint)(object)Is.False);
		Assert.That<int>(worldMap.FadeOutCount, (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(worldMap.FadeInCount, (IResolveConstraint)(object)Is.Zero);
		Assert.That<bool>(worldMap.IsBlocked, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(service.TryLoadScene("HomeBase"), (IResolveConstraint)(object)Is.True);
		yield return WaitUntil(() => !service.IsTransitioning, "The newly installed transition view was not usable.");
		Assert.That<int>(worldMap.FadeOutCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(worldMap.FadeInCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		UnityEngine.Object.Destroy(root);
		yield return null;
	}

	[UnityTest]
	public IEnumerator HomeBaseToWorldMap_ClearsPersistentBlackFade()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		yield return SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
		yield return WaitUntil(() => EidrenServiceRoot.Instance != null && UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>()?.SpawnedPlayer != null, "HomeBase did not initialize for the transition test.");
		SceneFlowService sceneFlow = EidrenServiceRoot.Instance.SceneFlowService;
		Assert.That<bool>(sceneFlow.TryLoadScene("WorldMap"), (IResolveConstraint)(object)Is.True);
		yield return WaitUntil(() => SceneManager.GetActiveScene().name == "WorldMap" && !sceneFlow.IsTransitioning, "HomeBase to WorldMap transition did not complete.");
		Assert.That<WorldMapController>(UnityEngine.Object.FindFirstObjectByType<WorldMapController>(), (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(sceneFlow.IsInputBlocked, (IResolveConstraint)(object)Is.False);
		Canvas canvas = Array.Find(EidrenServiceRoot.Instance.GetComponentsInChildren<Canvas>(includeInactive: true), (Canvas canvas2) => canvas2.name == "TransitionCanvas");
		Assert.That<Canvas>(canvas, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<bool>(canvas.enabled, (IResolveConstraint)(object)Is.False);
		Assert.That<float>(canvas.GetComponent<CanvasGroup>().alpha, (IResolveConstraint)(object)Is.Zero.Within((object)0.001f));
		Scene worldMapScene = SceneManager.GetActiveScene();
		SceneManager.SetActiveScene(SceneManager.CreateScene("WorldMapFadeTestCleanup"));
		UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
		yield return SceneManager.UnloadSceneAsync(worldMapScene);
		yield return null;
	}

	[UnityTest]
	public IEnumerator Bootstrap_MenuAndNewGame_LoadHomeBaseWithPlayer()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
		yield return WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu", "Bootstrap did not load MainMenu.");
		yield return WaitUntil(() => EidrenServiceRoot.Instance != null && !EidrenServiceRoot.Instance.SceneFlowService.IsTransitioning, "Bootstrap transition did not finish.");
		MainMenuController menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
		Assert.That<MainMenuController>(menu, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<EidrenServiceRoot>(EidrenServiceRoot.Instance, (IResolveConstraint)(object)Is.Not.Null);
		menu.StartNewGame();
		if (SceneManager.GetActiveScene().name == "MainMenu")
		{
			menu.StartNewGame();
		}
		yield return WaitUntil(() => SceneManager.GetActiveScene().name == "HomeBase", "New Game did not load HomeBase.");
		yield return WaitUntil(() => UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>()?.SpawnedPlayer != null, "HomeBase did not spawn the existing player prefab.");
		ZonePlayerSpawner zonePlayerSpawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		Assert.That<PlayerPrefabBindings>(zonePlayerSpawner.SpawnedPlayer, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<CharacterController>(zonePlayerSpawner.SpawnedPlayer.CharacterController, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<GameSessionPhase>(EidrenServiceRoot.Instance.GameSession.Phase, (IResolveConstraint)(object)Is.EqualTo((object)GameSessionPhase.ActiveGame));
		Assert.That<EidrenServiceRoot[]>(UnityEngine.Object.FindObjectsByType<EidrenServiceRoot>(FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		Scene homeScene = SceneManager.GetActiveScene();
		SceneManager.SetActiveScene(SceneManager.CreateScene("SceneFlowTestCleanup"));
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
		}
		yield return SceneManager.UnloadSceneAsync(homeScene);
		yield return null;
	}

	private static IEnumerator WaitUntil(Func<bool> condition, string failureMessage)
	{
		float deadline = Time.realtimeSinceStartup + 10f;
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
