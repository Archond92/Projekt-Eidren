using Eidren.AI;
using Eidren.Core.Services;
using Eidren.Interaction;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class Auftrag5CaptureRunner : MonoBehaviour
	{
		[Serializable]
		private sealed class CaptureReport
		{
			public string generatedUtc;

			public int width;

			public int height;

			public CaptureRecord[] captures;
		}

		[Serializable]
		private sealed class CaptureRecord
		{
			public string scene;

			public string file;

			public int enemies;

			public int resources;

			public int forgeChests;

			public long bytes;
		}

		private const string Flag = "-eidren-auftrag5-capture";

		private const string OutputArgument = "-eidren-auftrag5-output";

		private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

		private readonly List<CaptureRecord> _records = new List<CaptureRecord>();

		private string _output;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void TryCreate()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			if (Array.IndexOf(commandLineArgs, "-eidren-auftrag5-capture") >= 0 && !(UnityEngine.Object.FindFirstObjectByType<Auftrag5CaptureRunner>() != null))
			{
				GameObject gameObject = new GameObject("Auftrag5CaptureRunner");
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<Auftrag5CaptureRunner>();
			}
		}

		private void Awake()
		{
			_output = ReadArgument(Environment.GetCommandLineArgs(), "-eidren-auftrag5-output", Path.Combine(Application.persistentDataPath, "Auftrag5"));
			Directory.CreateDirectory(_output);
			Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
			StartCoroutine(Run());
		}

		private IEnumerator Run()
		{
			yield return null;
			EidrenServiceRoot services = EidrenServiceRoot.FindOrCreate();
			services.GameSession.StartNewGame();
			services.GameSession.TrySetProgressFlag("tier_2_unlocked");
			yield return LoadWarmupScene("WorldMap");
			yield return CaptureScene("Zone_TwilightGrove", "01_twilight_grove.bmp");
			yield return CaptureScene("Zone_VeilMarsh", "02_veil_marsh.bmp");
			yield return CaptureScene("Zone_GreyRifts", "03_grey_rifts.bmp");
			services.GameSession.TrySetProgressFlag("garon_defeated");
			if (services.GameSession.EidraForge.GetAccessState(DateTime.UtcNow) == EidraForgeAccessState.FirstRunAvailable)
			{
				services.GameSession.EidraForge.TryBeginFirstRun(5042026);
			}
			yield return CaptureScene("EidraForge", "04_eidra_forge.bmp");
			File.WriteAllText(Path.Combine(_output, "auftrag5-capture-report.json"), JsonUtility.ToJson(new CaptureReport
			{
				generatedUtc = DateTime.UtcNow.ToString("O"),
				width = Screen.width,
				height = Screen.height,
				captures = _records.ToArray()
			}, prettyPrint: true));
			Debug.Log($"AUFTRAG 5 CAPTURE: PASS ({_records.Count} gameplay scenes)");
			Application.Quit(0);
		}

		private static IEnumerator LoadWarmupScene(string sceneName)
		{
			AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
			if (operation == null)
			{
				Fail("Warm-up scene load did not start: " + sceneName);
			}
			while (!operation.isDone)
			{
				yield return null;
			}
			for (int frame = 0; frame < 16; frame++)
			{
				yield return null;
			}
		}

		private IEnumerator CaptureScene(string sceneName, string fileName)
		{
			AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
			if (operation == null)
			{
				Fail("Scene load did not start: " + sceneName);
			}
			while (!operation.isDone)
			{
				yield return null;
			}
			float deadline = Time.realtimeSinceStartup + 15f;
			PlayerPrefabBindings player = null;
			Camera camera = null;
			ZonePlayerSpawner spawner = null;
			while (Time.realtimeSinceStartup < deadline)
			{
				spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
				player = ((spawner != null) ? spawner.SpawnedPlayer : null);
				camera = ResolveSceneCamera();
				if (player != null && camera != null)
				{
					break;
				}
				yield return null;
			}
			if (player == null || camera == null)
			{
				Fail("Player or camera missing in " + sceneName);
			}
			deadline = Time.realtimeSinceStartup + 12f;
			int enemies;
			int resources;
			int forgeChests;
			do
			{
				enemies = UnityEngine.Object.FindObjectsByType<EnemyControllerBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
				resources = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
				forgeChests = UnityEngine.Object.FindObjectsByType<EidraForgeChestContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
				if ((sceneName == "EidraForge") ? (forgeChests == 11) : (enemies > 0 && resources > 0))
				{
					break;
				}
				yield return null;
			}
			while (Time.realtimeSinceStartup < deadline);
			yield return new WaitForSecondsRealtime(2f);
			enemies = UnityEngine.Object.FindObjectsByType<EnemyControllerBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
			resources = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
			forgeChests = UnityEngine.Object.FindObjectsByType<EidraForgeChestContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
			if (sceneName == "EidraForge" && forgeChests != 11)
			{
				Fail($"Forge expected 11 chests, found {forgeChests}");
			}
			if (sceneName != "EidraForge" && enemies == 0)
			{
				Fail("T2 scene has no active enemies: " + sceneName);
			}
			PlayerPrefabBindings capturePlayer = spawner.SpawnedPlayer;
			Vector3 focus = Vector3.zero;
			if (capturePlayer != null)
			{
				focus = capturePlayer.transform.position;
			}
			Vector3 searchOrigin = focus;
			EnemyControllerBase[] actors = UnityEngine.Object.FindObjectsByType<EnemyControllerBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			bool actorFocusFound = false;
			float nearestActorDistance = 1f / 0f;
			EnemyControllerBase[] array = actors;
			foreach (EnemyControllerBase actor in array)
			{
				if (actor == null)
				{
					continue;
				}
				Vector3 candidate = actor.transform.position;
				if (!(Mathf.Abs(candidate.y - searchOrigin.y) > 4f))
				{
					float distance = (candidate - searchOrigin).sqrMagnitude;
					if (!(distance >= nearestActorDistance))
					{
						nearestActorDistance = distance;
						focus = candidate;
						actorFocusFound = true;
					}
				}
			}
			if (actorFocusFound && capturePlayer != null && capturePlayer.Motor != null)
			{
				capturePlayer.Motor.Teleport(focus + new Vector3(-2.4f, 0.05f, -2.4f));
				focus += new Vector3(-1.2f, 0f, -1.2f);
			}
			if (!actorFocusFound)
			{
				EidraForgeChestContainer[] chests = UnityEngine.Object.FindObjectsByType<EidraForgeChestContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
				EidraForgeChestContainer[] array2 = chests;
				foreach (EidraForgeChestContainer chest in array2)
				{
					if (!(chest == null))
					{
						focus = chest.transform.position;
						break;
					}
				}
			}
			yield return new WaitForSecondsRealtime(1f);
			Debug.Log("AUFTRAG 5 CAPTURE FOCUS: scene=" + sceneName + "; " + $"origin={searchOrigin}; focus={focus}; actors={actors.Length}");
			string path = Path.Combine(_output, fileName);
			yield return Capture(path, focus);
			_records.Add(new CaptureRecord
			{
				scene = sceneName,
				file = fileName,
				enemies = enemies,
				resources = resources,
				forgeChests = forgeChests,
				bytes = new FileInfo(path).Length
			});
		}

		private static string ReadArgument(string[] arguments, string name, string fallback)
		{
			int num = Array.IndexOf(arguments, name);
			return (num >= 0 && num + 1 < arguments.Length) ? arguments[num + 1] : fallback;
		}

		private static void Fail(string message)
		{
			Debug.LogError("AUFTRAG 5 CAPTURE: FAIL - " + message);
			Application.Quit(1);
			throw new InvalidOperationException(message);
		}

		private static IEnumerator Capture(string path, Vector3 focus)
		{
			yield return EndOfFrame;
			Camera camera = ResolveSceneCamera();
			if (camera == null)
			{
				Fail("Gameplay camera disappeared before capture: " + path);
			}
			camera.transform.position = focus + new Vector3(0f, 13f, -11f);
			camera.transform.LookAt(focus + Vector3.up * 0.8f);
			if (camera.orthographic)
			{
				camera.orthographicSize = 6.5f;
			}
			int previousCullingMask = camera.cullingMask;
			Rect previousViewport = camera.rect;
			camera.cullingMask = -1;
			camera.rect = new Rect(0f, 0f, 1f, 1f);
			Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			bool[] canvasStates = new bool[canvases.Length];
			for (int index = 0; index < canvases.Length; index++)
			{
				if (!(canvases[index] == null))
				{
					canvasStates[index] = canvases[index].enabled;
					canvases[index].enabled = false;
				}
			}
			Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, mipChain: false);
			RenderTexture target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
			RenderTexture previousActive = RenderTexture.active;
			RenderTexture previousTarget = camera.targetTexture;
			try
			{
				camera.targetTexture = target;
				RenderTexture.active = target;
				GL.Clear(clearDepth: true, clearColor: true, camera.backgroundColor);
				camera.Render();
				camera.Render();
				image.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0, recalculateMipMaps: false);
				image.Apply(updateMipmaps: false, makeNoLongerReadable: false);
			}
			finally
			{
				camera.targetTexture = previousTarget;
				camera.cullingMask = previousCullingMask;
				camera.rect = previousViewport;
				RenderTexture.active = previousActive;
				target.Release();
				UnityEngine.Object.Destroy(target);
				for (int i = 0; i < canvases.Length; i++)
				{
					if (canvases[i] != null)
					{
						canvases[i].enabled = canvasStates[i];
					}
				}
			}
			WriteBmp(image, path);
			UnityEngine.Object.Destroy(image);
			if (!File.Exists(path) || new FileInfo(path).Length < 100000)
			{
				Fail("Capture was not written: " + path);
			}
		}

		private static Camera ResolveSceneCamera()
		{
			Scene activeScene = SceneManager.GetActiveScene();
			Camera camera = null;
			Camera[] array = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (Camera camera2 in array)
			{
				if (!(camera2 == null) && camera2.cullingMask != 0 && !(camera2.gameObject.scene != activeScene))
				{
					if ((object)camera == null)
					{
						camera = camera2;
					}
					if (camera2.CompareTag("MainCamera"))
					{
						return camera2;
					}
				}
			}
			return (camera != null) ? camera : Camera.main;
		}

		private static void WriteBmp(Texture2D texture, string path)
		{
			int width = texture.width;
			int height = texture.height;
			int num = (width * 3 + 3) & -4;
			Color32[] pixels = texture.GetPixels32();
			using FileStream output = File.Create(path);
			using BinaryWriter binaryWriter = new BinaryWriter(output);
			binaryWriter.Write((ushort)19778);
			binaryWriter.Write(54 + num * height);
			binaryWriter.Write(0);
			binaryWriter.Write(54);
			binaryWriter.Write(40);
			binaryWriter.Write(width);
			binaryWriter.Write(height);
			binaryWriter.Write((ushort)1);
			binaryWriter.Write((ushort)24);
			binaryWriter.Write(0);
			binaryWriter.Write(num * height);
			binaryWriter.Write(2835);
			binaryWriter.Write(2835);
			binaryWriter.Write(0);
			binaryWriter.Write(0);
			int num2 = num - width * 3;
			for (int i = 0; i < height; i++)
			{
				int num3 = i * width;
				for (int j = 0; j < width; j++)
				{
					Color32 color = pixels[num3 + j];
					binaryWriter.Write(color.b);
					binaryWriter.Write(color.g);
					binaryWriter.Write(color.r);
				}
				for (int k = 0; k < num2; k++)
				{
					binaryWriter.Write((byte)0);
				}
			}
		}
	}
}
