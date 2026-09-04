using Eidren.AI;
using Eidren.Input;
using Eidren.Interaction;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class CombatHudCaptureRunner : MonoBehaviour
	{
		[Serializable]
		private sealed class CaptureReport
		{
			public string generatedUtc;

			public CaptureRecord[] captures;
		}

		[Serializable]
		private sealed class CaptureRecord
		{
			public string file;

			public string state;

			public string scene;

			public int width;

			public int height;
		}

		private const string Flag = "-eidren-hud-capture";

		private const string OutputArgument = "-eidren-hud-output";

		private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

		private string _output;

		private readonly List<CaptureRecord> _records = new List<CaptureRecord>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void TryCreate()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			if (Array.IndexOf(commandLineArgs, "-eidren-hud-capture") >= 0 && !(UnityEngine.Object.FindFirstObjectByType<CombatHudCaptureRunner>() != null))
			{
				GameObject gameObject = new GameObject("CombatHudCaptureRunner");
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<CombatHudCaptureRunner>();
			}
		}

		private void Awake()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			_output = ReadArgument(commandLineArgs, "-eidren-hud-output", Path.Combine(Application.persistentDataPath, "CombatHud"));
			Directory.CreateDirectory(_output);
			StartCoroutine(Run());
		}

		private IEnumerator Run()
		{
			yield return null;
			EidrenServiceRoot services = EidrenServiceRoot.FindOrCreate();
			services.GameSession.StartNewGame();
			yield return Load("WorldMap");
			yield return Load("Zone_Greenwood");
			PlayerPrefabBindings player = null;
			PlayerInputReader input = null;
			yield return WaitForPlayer(delegate(PlayerPrefabBindings value)
			{
				player = value;
			}, delegate(PlayerInputReader value)
			{
				input = value;
			});
			if (player == null || input == null)
			{
				Fail("Greenwood player/input did not initialize.");
				yield break;
			}
			player.Motor.Teleport(new Vector3(-5f, 0.05f, 12f));
			yield return Frames(16);
			yield return SetFormat(1920, 1080);
			FreezeInputFamily(input, InputDisplayFamily.Touch);
			yield return Frames(3);
			yield return Capture("_capture_warmup.png", "warmup");
			_records.Clear();
			File.Delete(Path.Combine(_output, "_capture_warmup.png"));
			yield return Frames(3);
			yield return Capture("after_16x9_touch_free.png", "touch-free");
			yield return SetFormat(2340, 1080);
			yield return Capture("after_20x9_touch_safearea.png", "touch-20x9-safe-area");
			yield return SetFormat(1920, 1200);
			yield return Capture("after_16x10_touch_free.png", "touch-16x10");
			yield return SetFormat(1920, 1080);
			input.SetDisplayFamily(InputDisplayFamily.KeyboardMouse);
			yield return Frames(3);
			yield return Capture("after_16x9_keyboard.png", "keyboard-no-joystick");
			FreezeInputFamily(input, InputDisplayFamily.Touch);
			yield return CaptureHold(services, player, input);
			PrepareEidra(services, player);
			yield return Frames(5);
			yield return Capture("after_16x9_two_skills_ready.png", "two-skills-ready");
			input.PressSkill(1);
			yield return Frames(4);
			yield return Capture("after_16x9_skill_cooldown.png", "skill-cooldown");
			yield return Capture("after_16x9_potion_zero.png", "potion-zero");
			services.PlayerProgression.RecordEnemyDefeated(5100);
			yield return Load("Zone_EmberRuins");
			player = null;
			input = null;
			yield return WaitForPlayer(delegate(PlayerPrefabBindings value)
			{
				player = value;
			}, delegate(PlayerInputReader value)
			{
				input = value;
			});
			if (player == null || input == null)
			{
				Fail("Ember Ruins player/input did not initialize.");
				yield break;
			}
			input.SetDisplayFamily(InputDisplayFamily.Touch);
			BossAreaController area = UnityEngine.Object.FindFirstObjectByType<BossAreaController>();
			if (area != null && area.ActiveBoss != null)
			{
				player.CharacterController.enabled = false;
				player.transform.position = area.ActiveBoss.transform.position + Vector3.back * 5f;
				player.CharacterController.enabled = true;
				Physics.SyncTransforms();
				area.ActiveBoss.BeginBattle();
			}
			yield return Frames(8);
			yield return Capture("after_16x9_boss.png", "boss");
			File.WriteAllText(Path.Combine(_output, "capture-report.json"), JsonUtility.ToJson(new CaptureReport
			{
				generatedUtc = DateTime.UtcNow.ToString("O"),
				captures = _records.ToArray()
			}, prettyPrint: true));
			Application.Quit(0);
		}

		private IEnumerator CaptureHold(EidrenServiceRoot services, PlayerPrefabBindings player, PlayerInputReader input)
		{
			EnemyControllerBase[] array = UnityEngine.Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);
			foreach (EnemyControllerBase enemy in array)
			{
				enemy.gameObject.SetActive(value: false);
			}
			ResourceNode resource = UnityEngine.Object.FindFirstObjectByType<ResourceNode>();
			if (!(resource == null))
			{
				string tool = resource.Definition.RequiredToolItemId;
				if (!string.IsNullOrWhiteSpace(tool) && !services.GameSession.PlayerInventory.Contains(tool))
				{
					services.GameSession.PlayerInventory.Add(tool, 1);
				}
				player.Motor.Teleport(resource.transform.position + Vector3.back * 1.5f + Vector3.up * 0.05f);
				Physics.SyncTransforms();
				player.Interaction.RefreshTargetsNow();
				yield return Frames(2);
				input.SetVirtualInteract(held: true);
				float timeout = Time.realtimeSinceStartup + 4f;
				while (player.Interaction.Progress < 0.48f && Time.realtimeSinceStartup < timeout)
				{
					yield return null;
				}
				Time.timeScale = 0f;
				yield return Capture("after_16x9_hold_50.png", "hold-50-percent");
				Time.timeScale = 1f;
				input.SetVirtualInteract(held: false);
			}
		}

		private static void PrepareEidra(EidrenServiceRoot services, PlayerPrefabBindings player)
		{
			player.EidraTeam.SetTeam(services.ContentDatabase.GetEidra("terrock"), services.ContentDatabase.GetEidra("noctarion"));
			player.EidraTeam.TrySelectEidra("terrock");
		}

		private static void FreezeInputFamily(PlayerInputReader input, InputDisplayFamily family)
		{
			InputDisplayFamilyMonitor[] array = UnityEngine.Object.FindObjectsByType<InputDisplayFamilyMonitor>(FindObjectsSortMode.None);
			foreach (InputDisplayFamilyMonitor inputDisplayFamilyMonitor in array)
			{
				inputDisplayFamilyMonitor.enabled = false;
			}
			input.SetDisplayFamily(family);
		}

		private IEnumerator SetFormat(int width, int height)
		{
			Screen.SetResolution(width, height, FullScreenMode.Windowed);
			yield return Frames(6);
		}

		private IEnumerator Capture(string filename, string state)
		{
			yield return EndOfFrame;
			string path = Path.Combine(_output, filename);
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			MethodInfo capture = Type.GetType("UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule")?.GetMethod("CaptureScreenshot", new Type[1] { typeof(string) });
			if (capture == null)
			{
				throw new InvalidOperationException("Unity screen capture API is unavailable.");
			}
			capture.Invoke(null, new object[1] { path });
			yield return Frames(4);
			float timeout = Time.realtimeSinceStartup + 5f;
			while ((!File.Exists(path) || new FileInfo(path).Length < 100000) && Time.realtimeSinceStartup < timeout)
			{
				yield return null;
			}
			_records.Add(new CaptureRecord
			{
				file = filename,
				state = state,
				width = Screen.width,
				height = Screen.height,
				scene = SceneManager.GetActiveScene().name
			});
		}

		private static IEnumerator Load(string sceneName)
		{
			AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
			while (load != null && !load.isDone)
			{
				yield return null;
			}
			yield return Frames(12);
		}

		private static IEnumerator WaitForPlayer(Action<PlayerPrefabBindings> setPlayer, Action<PlayerInputReader> setInput)
		{
			float timeout = Time.realtimeSinceStartup + 12f;
			while (Time.realtimeSinceStartup < timeout)
			{
				PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
				PlayerInputReader input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
				if (player != null && input != null)
				{
					setPlayer(player);
					setInput(input);
					break;
				}
				yield return null;
			}
		}

		private static IEnumerator Frames(int count)
		{
			for (int i = 0; i < count; i++)
			{
				yield return null;
			}
		}

		private void Fail(string message)
		{
			File.WriteAllText(Path.Combine(_output, "capture-failure.txt"), message);
			Application.Quit(1);
		}

		private static string ReadArgument(IReadOnlyList<string> arguments, string key, string fallback)
		{
			for (int i = 0; i < arguments.Count - 1; i++)
			{
				if (string.Equals(arguments[i], key, StringComparison.OrdinalIgnoreCase))
				{
					return Path.GetFullPath(arguments[i + 1]);
				}
			}
			return fallback;
		}
	}
}
