using Eidren.Data;
using Eidren.Interaction;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class WindowsPerformanceAuditRunner : MonoBehaviour
	{
		[Serializable]
		private sealed class PerformanceAuditReport
		{
			public string generatedUtc;

			public string unityVersion;

			public string applicationVersion;

			public int requestedWidth;

			public int requestedHeight;

			public bool requestedFullscreen;

			public int actualWidth;

			public int actualHeight;

			public string fullscreenMode;

			public int qualityLevel;

			public int vSyncCount;

			public int targetFrameRate;

			public string error;

			public List<PerformanceAuditSample> samples;
		}

		[Serializable]
		private sealed class PerformanceAuditSample
		{
			public string label;

			public string scene;

			public int frames;

			public double averageFps;

			public double minimumFps;

			public double maximumFrameMs;

			public double averageMainThreadMs;

			public double averageRenderThreadMs;

			public double averageGcBytesPerFrame;

			public long maximumGcBytesPerFrame;

			public int activeGameObjects;

			public int activeRenderers;

			public int activeAudioSources;

			public float sceneLoadSeconds;

			public double totalAllocatedMemoryMb;

			public double totalReservedMemoryMb;

			public double monoUsedMemoryMb;
		}

		private const int WarmupFrames = 60;

		private const int SampleFrames = 180;

		private const int CycleSettleFrames = 30;

		private const string AuditFlag = "-eidren-performance-audit";

		private const string OutputArgument = "-eidren-performance-output";

		private const string WidthArgument = "-eidren-performance-width";

		private const string HeightArgument = "-eidren-performance-height";

		private const string FullscreenArgument = "-eidren-performance-fullscreen";

		private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

		private PerformanceAuditReport _report;

		private string _outputPath;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void TryCreate()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			if (Array.IndexOf(commandLineArgs, "-eidren-performance-audit") >= 0 && !(UnityEngine.Object.FindFirstObjectByType<WindowsPerformanceAuditRunner>() != null))
			{
				GameObject gameObject = new GameObject("WindowsPerformanceAuditRunner");
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<WindowsPerformanceAuditRunner>();
			}
		}

		private void Awake()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			_outputPath = ReadArgument(commandLineArgs, "-eidren-performance-output", Path.Combine(Application.persistentDataPath, "windows-performance-audit.json"));
			int num = ReadIntArgument(commandLineArgs, "-eidren-performance-width", Screen.width);
			int num2 = ReadIntArgument(commandLineArgs, "-eidren-performance-height", Screen.height);
			bool flag = ReadBoolArgument(commandLineArgs, "-eidren-performance-fullscreen", fallback: false);
			Screen.SetResolution(num, num2, flag ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
			_report = new PerformanceAuditReport
			{
				generatedUtc = DateTime.UtcNow.ToString("O"),
				unityVersion = Application.unityVersion,
				applicationVersion = Application.version,
				requestedWidth = num,
				requestedHeight = num2,
				requestedFullscreen = flag,
				qualityLevel = QualitySettings.GetQualityLevel(),
				vSyncCount = QualitySettings.vSyncCount,
				targetFrameRate = Application.targetFrameRate,
				samples = new List<PerformanceAuditSample>()
			};
			StartCoroutine(RunAudit());
		}

		private IEnumerator RunAudit()
		{
			yield return null;
			yield return null;
			yield return LoadAndMeasure("MainMenu", "MainMenu");
			EidrenServiceRoot services = EidrenServiceRoot.FindOrCreate();
			services.GameSession.StartNewGame();
			yield return LoadAndMeasure("WorldMap", "WorldMap");
			yield return LoadAndMeasure("HomeBase", "HomeBase");
			yield return MeasureHomeBaseWindows();
			yield return LoadAndMeasure("Zone_Greenwood", "Zone_Greenwood");
			yield return LoadWithoutMeasurement("WorldMap");
			yield return LoadAndMeasure("Zone_Quarry", "Zone_Quarry");
			yield return LoadWithoutMeasurement("WorldMap");
			yield return LoadAndMeasure("Zone_Marsh", "Zone_Marsh");
			yield return LoadWithoutMeasurement("WorldMap");
			services.PlayerProgression.RecordEnemyDefeated(5100);
			yield return LoadAndMeasure("Zone_EmberRuins", "Zone_EmberRuins");
			yield return MeasureGaronFight();
			yield return LoadWithoutMeasurement("HomeBase");
			yield return RepeatTravelCycle();
			yield return MeasureCurrentScene("TravelCycle_Repeat_End", 120);
			_report.actualWidth = Screen.width;
			_report.actualHeight = Screen.height;
			_report.fullscreenMode = Screen.fullScreenMode.ToString();
			WriteReport();
			Application.Quit((!string.IsNullOrEmpty(_report.error)) ? 1 : 0);
		}

		private IEnumerator MeasureHomeBaseWindows()
		{
			PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
			if (!(player == null))
			{
				if (player.InventoryWindow != null)
				{
					player.InventoryWindow.Open();
					yield return WaitFrames(10);
					yield return MeasureCurrentScene("Inventory", 180);
					player.InventoryWindow.Close();
					yield return WaitFrames(10);
				}
				if (player.CraftingWindow != null)
				{
					player.CraftingWindow.Open(CraftingStationType.Workbench);
					yield return WaitFrames(10);
					yield return MeasureCurrentScene("Crafting", 180);
					player.CraftingWindow.Close();
					yield return WaitFrames(10);
				}
				StorageContainer container = UnityEngine.Object.FindFirstObjectByType<StorageContainer>();
				if (player.StorageWindow != null && container != null)
				{
					player.StorageWindow.Open(container);
					yield return WaitFrames(10);
					yield return MeasureCurrentScene("Storage", 180);
					player.StorageWindow.Close();
					yield return WaitFrames(10);
				}
			}
		}

		private IEnumerator MeasureGaronFight()
		{
			BossAreaController area = UnityEngine.Object.FindFirstObjectByType<BossAreaController>();
			if (!(area?.ActiveBoss == null))
			{
				area.ActiveBoss.BeginBattle();
				yield return WaitFrames(30);
				yield return MeasureCurrentScene("Garon_Fight", 180);
			}
		}

		private IEnumerator RepeatTravelCycle()
		{
			string[] cycle = new string[9] { "WorldMap", "Zone_Greenwood", "WorldMap", "Zone_Quarry", "WorldMap", "Zone_Marsh", "WorldMap", "Zone_EmberRuins", "HomeBase" };
			string[] array = cycle;
			foreach (string scene in array)
			{
				yield return LoadWithoutMeasurement(scene);
			}
		}

		private IEnumerator LoadAndMeasure(string sceneName, string label)
		{
			float startedAt = Time.realtimeSinceStartup;
			AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
			if (load == null)
			{
				throw new InvalidOperationException("Could not start loading scene '" + sceneName + "'.");
			}
			while (!load.isDone)
			{
				yield return null;
			}
			float loadSeconds = Time.realtimeSinceStartup - startedAt;
			yield return WaitFrames(60);
			yield return MeasureCurrentScene(label, 180, loadSeconds);
		}

		private IEnumerator LoadWithoutMeasurement(string sceneName)
		{
			AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
			if (load == null)
			{
				throw new InvalidOperationException("Could not start loading scene '" + sceneName + "'.");
			}
			while (!load.isDone)
			{
				yield return null;
			}
			yield return WaitFrames(30);
		}

		private IEnumerator MeasureCurrentScene(string label, int frameCount, float loadSeconds = -1f)
		{
			ProfilerRecorder mainThread = TryStartRecorder(ProfilerCategory.Internal, "Main Thread");
			ProfilerRecorder renderThread = TryStartRecorder(ProfilerCategory.Internal, "Render Thread");
			ProfilerRecorder gcAllocated = TryStartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
			double frameSeconds = 0.0;
			double minimumFps = 1.7976931348623157E+308;
			double maximumFrameMs = 0.0;
			double mainThreadMs = 0.0;
			double renderThreadMs = 0.0;
			double gcBytes = 0.0;
			long maximumGcBytes = 0L;
			int mainSamples = 0;
			int renderSamples = 0;
			int gcSamples = 0;
			for (int frame = 0; frame < frameCount; frame++)
			{
				yield return EndOfFrame;
				double seconds = Math.Max(1E-06, Time.unscaledDeltaTime);
				double frameMs = seconds * 1000.0;
				frameSeconds += seconds;
				minimumFps = Math.Min(minimumFps, 1.0 / seconds);
				maximumFrameMs = Math.Max(maximumFrameMs, frameMs);
				AccumulateMilliseconds(mainThread, ref mainThreadMs, ref mainSamples);
				AccumulateMilliseconds(renderThread, ref renderThreadMs, ref renderSamples);
				if (gcAllocated.Valid)
				{
					long value = Math.Max(0L, gcAllocated.LastValue);
					gcBytes += (double)value;
					maximumGcBytes = Math.Max(maximumGcBytes, value);
					gcSamples++;
				}
			}
			mainThread.Dispose();
			renderThread.Dispose();
			gcAllocated.Dispose();
			_report.samples.Add(new PerformanceAuditSample
			{
				label = label,
				scene = SceneManager.GetActiveScene().name,
				frames = frameCount,
				averageFps = (double)frameCount / Math.Max(1E-06, frameSeconds),
				minimumFps = ((minimumFps == 1.7976931348623157E+308) ? 0.0 : minimumFps),
				maximumFrameMs = maximumFrameMs,
				averageMainThreadMs = Average(mainThreadMs, mainSamples),
				averageRenderThreadMs = Average(renderThreadMs, renderSamples),
				averageGcBytesPerFrame = Average(gcBytes, gcSamples),
				maximumGcBytesPerFrame = maximumGcBytes,
				activeGameObjects = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length,
				activeRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length,
				activeAudioSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length,
				sceneLoadSeconds = loadSeconds,
				totalAllocatedMemoryMb = (double)Profiler.GetTotalAllocatedMemoryLong() / 1048576.0,
				totalReservedMemoryMb = (double)Profiler.GetTotalReservedMemoryLong() / 1048576.0,
				monoUsedMemoryMb = (double)Profiler.GetMonoUsedSizeLong() / 1048576.0
			});
		}

		private static IEnumerator WaitFrames(int frameCount)
		{
			for (int frame = 0; frame < frameCount; frame++)
			{
				yield return null;
			}
		}

		private static ProfilerRecorder TryStartRecorder(ProfilerCategory category, string marker)
		{
			try
			{
				return ProfilerRecorder.StartNew(category, marker);
			}
			catch
			{
				return default(ProfilerRecorder);
			}
		}

		private static void AccumulateMilliseconds(ProfilerRecorder recorder, ref double total, ref int samples)
		{
			if (recorder.Valid)
			{
				total += (double)Math.Max(0L, recorder.LastValue) / 1000000.0;
				samples++;
			}
		}

		private static double Average(double total, int count)
		{
			return (count > 0) ? (total / (double)count) : (-1.0);
		}

		private void WriteReport()
		{
			string directoryName = Path.GetDirectoryName(_outputPath);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(_outputPath, JsonUtility.ToJson(_report, prettyPrint: true));
			Debug.Log("Eidren Windows performance audit written to '" + _outputPath + "'.");
		}

		private static string ReadArgument(string[] arguments, string name, string fallback)
		{
			int num = Array.IndexOf(arguments, name);
			return (num >= 0 && num + 1 < arguments.Length) ? arguments[num + 1] : fallback;
		}

		private static int ReadIntArgument(string[] arguments, string name, int fallback)
		{
			int result;
			return int.TryParse(ReadArgument(arguments, name, string.Empty), out result) ? Math.Max(1, result) : fallback;
		}

		private static bool ReadBoolArgument(string[] arguments, string name, bool fallback)
		{
			bool result;
			return bool.TryParse(ReadArgument(arguments, name, string.Empty), out result) ? result : fallback;
		}
	}
}
