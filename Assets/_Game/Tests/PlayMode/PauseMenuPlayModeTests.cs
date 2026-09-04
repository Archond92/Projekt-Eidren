using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Input;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class PauseMenuPlayModeTests : InputTestFixture
{
	[UnityTest]
	public IEnumerator Pause_LocksGameplay_BackClosesTopMenu_AndResumes()
	{
		yield return LoadHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		yield return WaitUntil(() => spawner != null && spawner.SpawnedPlayer != null, "HomeBase player did not spawn.");
		PauseMenuController menu = services.PauseMenuController;
		PlayerInputReader input = spawner.Input;
		Assert.That<PauseMenuController>(menu, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerInputReader>(input, (IResolveConstraint)(object)Is.Not.Null);
		Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
		Tap(keyboard.escapeKey);
		yield return null;
		Assert.That<bool>(services.PauseService.IsPaused, (IResolveConstraint)(object)Is.True);
		Assert.That<float>(Time.timeScale, (IResolveConstraint)(object)Is.Zero);
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(menu.IsPauseOpen, (IResolveConstraint)(object)Is.True);
		menu.OpenSettingsFromPause();
		Assert.That<bool>(menu.IsSettingsOpen, (IResolveConstraint)(object)Is.True);
		services.MenuInputService.PressBack();
		Assert.That<bool>(menu.IsSettingsOpen, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(menu.IsPauseOpen, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(services.PauseService.IsPaused, (IResolveConstraint)(object)Is.True);
		Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
		Tap(gamepad.startButton);
		yield return null;
		Assert.That<bool>(services.PauseService.IsPaused, (IResolveConstraint)(object)Is.False);
		Assert.That<float>(Time.timeScale, (IResolveConstraint)(object)Is.EqualTo((object)1f));
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.True);
		menu.transform.Find("CanvasRoot/SafeArea/PauseButton").GetComponent<Button>().onClick.Invoke();
		yield return null;
		Assert.That<bool>(services.PauseService.IsPaused, (IResolveConstraint)(object)Is.True);
		menu.ResumeGameplay();
		yield return CleanupCurrentScene();
	}

	[UnityTest]
	public IEnumerator ApplicationFocusLoss_PausesUntilExplicitResume()
	{
		yield return LoadHomeBase();
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		yield return WaitUntil(() => spawner != null && spawner.SpawnedPlayer != null, "HomeBase player did not spawn.");
		ApplicationLifecycleController lifecycle = services.ApplicationLifecycleController;
		Assert.That<ApplicationLifecycleController>(lifecycle, (IResolveConstraint)(object)Is.Not.Null);
		lifecycle.HandleApplicationFocus(focused: false);
		yield return null;
		Assert.That<bool>(services.PauseService.IsPaused, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(services.PauseMenuController.IsPauseOpen, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(spawner.Input.GameplayEnabled, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(AudioListener.pause, (IResolveConstraint)(object)Is.True);
		lifecycle.HandleApplicationFocus(focused: true);
		yield return null;
		Assert.That<bool>(services.PauseService.IsPaused, (IResolveConstraint)(object)Is.True, "Returning focus must not resume gameplay.", Array.Empty<object>());
		Assert.That<bool>(AudioListener.pause, (IResolveConstraint)(object)Is.True);
		services.PauseMenuController.ResumeGameplay();
		yield return null;
		Assert.That<bool>(services.PauseService.IsPaused, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(spawner.Input.GameplayEnabled, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(AudioListener.pause, (IResolveConstraint)(object)Is.False);
		yield return CleanupCurrentScene();
	}

	[UnityTest]
	public IEnumerator Pause_WorksInHomeBase()
	{
		yield return ClearPersistentServices();
		yield return SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
		yield return WaitUntil(() => EidrenServiceRoot.Instance != null && UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>() != null, "HomeBase did not initialize.");
		EidrenServiceRoot instance = EidrenServiceRoot.Instance;
		PlayerInputReader input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
		instance.PauseMenuController.OpenPause();
		Assert.That<bool>(instance.PauseService.IsPaused, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.False);
		instance.PauseMenuController.ResumeGameplay();
		Assert.That<bool>(instance.PauseService.IsPaused, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(input.GameplayEnabled, (IResolveConstraint)(object)Is.True);
		yield return CleanupCurrentScene();
	}

	[UnityTest]
	public IEnumerator MainMenuRequest_SavesBeforeSceneTransition()
	{
		yield return LoadHomeBase();
		EidrenServiceRoot instance = EidrenServiceRoot.Instance;
		bool saveCompleted = false;
		instance.SaveGameService.SaveCompleted += delegate(SaveRequestReason reason)
		{
			if (reason == SaveRequestReason.MainMenuRequested)
			{
				saveCompleted = true;
			}
		};
		PauseMenuController pauseMenuController = instance.PauseMenuController;
		pauseMenuController.OpenPause();
		pauseMenuController.RequestMainMenu();
		pauseMenuController.ConfirmRequest();
		Assert.That<bool>(saveCompleted, (IResolveConstraint)(object)Is.True);
		Assert.That<GameSessionPhase>(instance.GameSession.Phase, (IResolveConstraint)(object)Is.EqualTo((object)GameSessionPhase.Shell));
		yield return WaitUntil(() => SceneManager.GetActiveScene().name == "MainMenu", "Pause menu did not load MainMenu.");
		Assert.That<EidrenServiceRoot[]>(UnityEngine.Object.FindObjectsByType<EidrenServiceRoot>(FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		yield return CleanupCurrentScene();
	}

	private static IEnumerator LoadHomeBase()
	{
		yield return ClearPersistentServices();
		yield return SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
		yield return WaitUntil(() => EidrenServiceRoot.Instance != null, "HomeBase did not create services.");
		EidrenServiceRoot.Instance.GameSession.StartNewGame();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		if (spawner != null && spawner.SpawnedPlayer == null)
		{
			yield return null;
		}
	}

	private static IEnumerator ClearPersistentServices()
	{
		Time.timeScale = 1f;
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
	}

	private static IEnumerator CleanupCurrentScene()
	{
		Time.timeScale = 1f;
		Scene loaded = SceneManager.GetActiveScene();
		SceneManager.SetActiveScene(SceneManager.CreateScene("PauseSettingsTestCleanup"));
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
		}
		yield return null;
		if (loaded.IsValid() && loaded.isLoaded)
		{
			yield return SceneManager.UnloadSceneAsync(loaded);
		}
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

	private void Tap(ButtonControl button)
	{
		Press(button);
		Release(button);
	}
}
}
