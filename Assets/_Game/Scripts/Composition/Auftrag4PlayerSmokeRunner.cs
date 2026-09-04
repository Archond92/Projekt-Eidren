using Eidren.AI;
using Eidren.Core.Services;
using Eidren.Interaction;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class Auftrag4PlayerSmokeRunner : MonoBehaviour
	{
		private const string Flag = "-auftrag4Smoke";

		private static bool _started;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetState()
		{
			_started = false;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void StartWhenRequested()
		{
			if (!_started && Environment.GetCommandLineArgs().Contains("-auftrag4Smoke", StringComparer.OrdinalIgnoreCase))
			{
				_started = true;
				GameObject gameObject = new GameObject("Auftrag4PlayerSmokeRunner");
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<Auftrag4PlayerSmokeRunner>().StartCoroutine(Run());
			}
		}

		private static IEnumerator Run()
		{
			yield return null;
			EidrenServiceRoot services = EidrenServiceRoot.FindOrCreate();
			if (services.GameSession.Phase == GameSessionPhase.Shell)
			{
				services.GameSession.StartNewGame();
			}
			services.GameSession.TrySetProgressFlag("garon_defeated");
			if (services.GameSession.EidraForge.GetAccessState(DateTime.UtcNow) == EidraForgeAccessState.FirstRunAvailable)
			{
				services.GameSession.EidraForge.TryBeginFirstRun(4042026);
			}
			if (!services.SceneFlowService.TryLoadScene("EidraForge"))
			{
				Fail("scene transition could not start");
			}
			float deadline = Time.realtimeSinceStartup + 20f;
			while (SceneManager.GetActiveScene().name != "EidraForge" && Time.realtimeSinceStartup < deadline)
			{
				yield return null;
			}
			yield return new WaitForSecondsRealtime(3f);
			Require(SceneManager.GetActiveScene().name == "EidraForge", "forge scene loaded");
			Require(UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>() != null, "player spawned");
			Require(UnityEngine.Object.FindFirstObjectByType<EidraForgeSceneController>() != null, "forge scene controller active");
			int regular = UnityEngine.Object.FindObjectsByType<WildlingController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
			int eidren = UnityEngine.Object.FindObjectsByType<EidraWildController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
			int cores = UnityEngine.Object.FindObjectsByType<CoreGuardianController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
			Require(regular == 23 && eidren == 1 && cores == 1, $"enemy population 23/1/1, got {regular}/{eidren}/{cores}");
			int chests = UnityEngine.Object.FindObjectsByType<EidraForgeChestContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
			Require(chests == 11, $"11 physical chests, got {chests}");
			Debug.Log("AUFTRAG 4 PLAYER SMOKE: PASS");
			Application.Quit(0);
		}

		private static void Require(bool condition, string label)
		{
			if (!condition)
			{
				Fail(label);
			}
		}

		private static void Fail(string label)
		{
			Debug.LogError("AUFTRAG 4 PLAYER SMOKE: FAIL - " + label);
			Application.Quit(1);
			throw new InvalidOperationException(label);
		}
	}
}
