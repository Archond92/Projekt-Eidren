using Eidren.Composition;
using Eidren.Core.Services;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class SceneFlowAndMenuTests
{
	private sealed class RejectingSceneLoader : ISceneLoader
	{
		public string ActiveSceneName => "Test";

		public bool CanLoad(string sceneName)
		{
			return false;
		}

		public IEnumerator LoadAsync(string sceneName)
		{
			yield break;
		}
	}

	[Test]
	public void ServiceRoot_CreatesEveryServiceOnlyOnce()
	{
		EidrenServiceRoot first = null;
		GameObject duplicateObject = null;
		try
		{
			first = EidrenServiceRoot.FindOrCreate();
			EidrenServiceRoot eidrenServiceRoot = EidrenServiceRoot.FindOrCreate();
			duplicateObject = new GameObject("Duplicate_EIDREN_SERVICES");
			duplicateObject.AddComponent<EidrenServiceRoot>();
			Assert.That<EidrenServiceRoot>(eidrenServiceRoot, (IResolveConstraint)(object)Is.SameAs((object)first));
			Assert.That<GameSession[]>(Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
			Assert.That<SceneFlowService[]>(Object.FindObjectsByType<SceneFlowService>(FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
			Assert.That<ContentDatabase[]>(Object.FindObjectsByType<ContentDatabase>(FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		}
		finally
		{
			if (duplicateObject != null)
			{
				Object.DestroyImmediate(duplicateObject);
			}
			if (first != null)
			{
				Object.DestroyImmediate(first.gameObject);
			}
		}
	}

	[Test]
	public void NewGame_ResetsSessionToDefinedStartingState()
	{
		GameObject root = new GameObject("NewGameSession_Test");
		try
		{
			GameSession gameSession = root.AddComponent<GameSession>();
			gameSession.Initialize();
			string shellSessionId = gameSession.SessionId;
			gameSession.StartNewGame();
			Assert.That<GameSessionPhase>(gameSession.Phase, (IResolveConstraint)(object)Is.EqualTo((object)GameSessionPhase.ActiveGame));
			Assert.That<int>(gameSession.NewGameGeneration, (IResolveConstraint)(object)Is.EqualTo((object)1));
			Assert.That<string>(gameSession.SessionId, (IResolveConstraint)(object)Is.Not.EqualTo((object)shellSessionId));
			Assert.That<string>(gameSession.SessionId, (IResolveConstraint)(object)Is.Not.Empty);
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void UnknownScene_ReportsControlledError()
	{
		GameObject root = new GameObject("UnknownScene_Test");
		try
		{
			SceneFlowService sceneFlowService = root.AddComponent<SceneFlowService>();
			sceneFlowService.Configure(null, new RejectingSceneLoader());
			LogAssert.Expect(LogType.Error, "Scene 'Does_Not_Exist' is not available in the active Unity Build Settings scene list.");
			Assert.That<bool>(sceneFlowService.TryLoadScene("Does_Not_Exist"), (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(sceneFlowService.IsTransitioning, (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void MainMenu_OpensAndClosesSettings()
	{
		GameObject root = new GameObject("MainMenuSettings_Test");
		GameObject settings = new GameObject("SettingsPanel_Test");
		root.SetActive(value: false);
		settings.transform.SetParent(root.transform, worldPositionStays: false);
		MainMenuController controller = root.AddComponent<MainMenuController>();
		controller.ConfigureUi(null, null, null, null, null, settings, null);
		settings.SetActive(value: false);
		try
		{
			controller.OpenSettings();
			Assert.That<bool>(controller.SettingsOpen, (IResolveConstraint)(object)Is.True);
			controller.CloseSettings();
			Assert.That<bool>(controller.SettingsOpen, (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}
}
}
