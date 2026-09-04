using Eidren.AI;
using Eidren.Interaction;
using Eidren.Presentation;
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
	public sealed class StyleProofCaptureRunner : MonoBehaviour
	{
		[Serializable]
		private sealed class StyleProofCaptureReport
		{
			public string generatedUtc;

			public string scene;

			public int width;

			public int height;

			public float averageFrameMs;

			public float averageFps;

			public int rendererCount;

			public int uniqueMaterialCount;

			public int missingMaterialCount;

			public int meshColliderCount;

			public int rigidbodyCount;

			public bool activeEidraPresent;

			public string[] screenshots;
		}

		private const string CaptureFlag = "-eidren-style-proof-capture";

		private const string OutputArgument = "-eidren-style-proof-output";

		private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

		private string _outputDirectory;

		private readonly List<string> _screenshots = new List<string>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void TryCreate()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			if (Array.IndexOf(commandLineArgs, "-eidren-style-proof-capture") >= 0 && !(UnityEngine.Object.FindFirstObjectByType<StyleProofCaptureRunner>() != null))
			{
				GameObject gameObject = new GameObject("StyleProofCaptureRunner");
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<StyleProofCaptureRunner>();
			}
		}

		private void Awake()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			_outputDirectory = ReadArgument(commandLineArgs, "-eidren-style-proof-output", Path.Combine(Application.persistentDataPath, "StyleProof"));
			Directory.CreateDirectory(_outputDirectory);
			Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
			StartCoroutine(Run());
		}

		private IEnumerator Run()
		{
			yield return null;
			EidrenServiceRoot services = EidrenServiceRoot.FindOrCreate();
			services.GameSession.StartNewGame();
			AsyncOperation load = SceneManager.LoadSceneAsync("WorldMap", LoadSceneMode.Single);
			while (load != null && !load.isDone)
			{
				yield return null;
			}
			yield return WaitFrames(12);
			load = SceneManager.LoadSceneAsync("Zone_Greenwood", LoadSceneMode.Single);
			while (load != null && !load.isDone)
			{
				yield return null;
			}
			yield return WaitFrames(45);
			PlayerPrefabBindings player = null;
			Camera camera = null;
			GameObject proof = null;
			StyleProofEidraCompanion activeEidra = null;
			float timeout = Time.realtimeSinceStartup + 12f;
			while (Time.realtimeSinceStartup < timeout)
			{
				player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
				camera = Camera.main;
				proof = GameObject.Find("StyleProofReferenceArea");
				activeEidra = UnityEngine.Object.FindFirstObjectByType<StyleProofEidraCompanion>();
				if (player != null && camera != null && proof != null && activeEidra != null)
				{
					break;
				}
				yield return null;
			}
			if (player == null || camera == null || proof == null || activeEidra == null)
			{
				WriteFailure("Greenwood player, active Eidra, camera or StyleProofReferenceArea did not initialize.");
				Application.Quit(1);
				yield break;
			}
			IsometricCamera follow = camera.GetComponent<IsometricCamera>();
			MovePlayer(player, new Vector3(-5f, 0.05f, 12f));
			yield return WaitFrames(28);
			CombatFeedback.SpawnStyleCue(player.transform.position + Vector3.up, StyleCue.EidraEnergy);
			yield return WaitFrames(4);
			yield return Capture("01_normal_gameplay.png");
			ResourceNode resource = FindNearest<ResourceNode>(new Vector3(-19f, 0f, 7f));
			if (resource != null)
			{
				MovePlayer(player, resource.transform.position + new Vector3(1.7f, 0f, -1.7f));
				yield return WaitFrames(24);
				CombatFeedback.SpawnStyleCue(resource.transform.position + Vector3.up * 0.8f, StyleCue.ResourceHarvest);
				yield return WaitFrames(4);
			}
			yield return Capture("02_resource_interaction.png");
			WildlingController wildling = FindNearest<WildlingController>(new Vector3(10f, 0f, 12f));
			if (wildling != null)
			{
				Vector3 away = player.transform.position - wildling.transform.position;
				away.y = 0f;
				if (away.sqrMagnitude < 0.1f)
				{
					away = Vector3.back;
				}
				MovePlayer(player, wildling.transform.position + away.normalized * 3.6f);
				yield return WaitFrames(28);
				CombatFeedback.SpawnStyleCue(wildling.transform.position + Vector3.up, StyleCue.HammerHit);
				yield return WaitFrames(4);
			}
			yield return Capture("03_wildling_combat.png");
			MovePlayer(player, new Vector3(-23.5f, 0.05f, 32.5f));
			yield return WaitFrames(24);
			CombatFeedback.SpawnStyleCue(new Vector3(-21.5f, 0.75f, 32f), StyleCue.ItemPickup);
			yield return WaitFrames(4);
			yield return Capture("04_ruin_and_world_item.png");
			if (follow != null)
			{
				follow.enabled = false;
			}
			camera.orthographicSize = 24f;
			Vector3 focus = new Vector3(-2f, 1.35f, 11f);
			camera.transform.position = focus + new Vector3(-24f, 38f, -24f);
			camera.transform.rotation = Quaternion.Euler(52f, 45f, 0f);
			yield return WaitFrames(8);
			yield return Capture("05_reference_overview.png");
			float total = 0f;
			for (int i = 0; i < 180; i++)
			{
				yield return EndOfFrame;
				total += Time.unscaledDeltaTime;
			}
			Renderer[] renderers = proof.GetComponentsInChildren<Renderer>(includeInactive: true);
			HashSet<Material> materials = new HashSet<Material>();
			int missingMaterials = 0;
			Renderer[] array = renderers;
			foreach (Renderer renderer in array)
			{
				Material[] sharedMaterials = renderer.sharedMaterials;
				foreach (Material material in sharedMaterials)
				{
					if (material == null)
					{
						missingMaterials++;
					}
					else
					{
						materials.Add(material);
					}
				}
			}
			File.WriteAllText(contents: JsonUtility.ToJson(new StyleProofCaptureReport
			{
				generatedUtc = DateTime.UtcNow.ToString("O"),
				scene = SceneManager.GetActiveScene().name,
				width = Screen.width,
				height = Screen.height,
				averageFrameMs = total / 180f * 1000f,
				averageFps = 180f / Mathf.Max(0.001f, total),
				rendererCount = renderers.Length,
				uniqueMaterialCount = materials.Count,
				missingMaterialCount = missingMaterials,
				meshColliderCount = proof.GetComponentsInChildren<MeshCollider>(includeInactive: true).Length,
				rigidbodyCount = proof.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length,
				activeEidraPresent = (activeEidra != null),
				screenshots = _screenshots.ToArray()
			}, prettyPrint: true), path: Path.Combine(_outputDirectory, "style-proof-runtime-report.json"));
			Application.Quit((missingMaterials != 0) ? 1 : 0);
		}

		private IEnumerator Capture(string filename)
		{
			yield return EndOfFrame;
			string path = Path.Combine(_outputDirectory, filename);
			MethodInfo captureMethod = Type.GetType("UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule")?.GetMethod("CaptureScreenshot", new Type[1] { typeof(string) });
			if (captureMethod == null)
			{
				throw new InvalidOperationException("Unity screen capture API is unavailable.");
			}
			captureMethod.Invoke(null, new object[1] { path });
			_screenshots.Add(filename);
			float timeout = Time.realtimeSinceStartup + 5f;
			while (!File.Exists(path) && Time.realtimeSinceStartup < timeout)
			{
				yield return null;
			}
		}

		private static void MovePlayer(PlayerPrefabBindings player, Vector3 position)
		{
			CharacterController characterController = player.CharacterController;
			if (characterController != null)
			{
				characterController.enabled = false;
			}
			player.transform.position = position;
			if (characterController != null)
			{
				characterController.enabled = true;
			}
		}

		private static T FindNearest<T>(Vector3 position) where T : Component
		{
			T[] array = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			T result = null;
			float num = 1f / 0f;
			T[] array2 = array;
			foreach (T val in array2)
			{
				float sqrMagnitude = (val.transform.position - position).sqrMagnitude;
				if (!(sqrMagnitude >= num))
				{
					result = val;
					num = sqrMagnitude;
				}
			}
			return result;
		}

		private static IEnumerator WaitFrames(int count)
		{
			for (int i = 0; i < count; i++)
			{
				yield return null;
			}
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

		private void WriteFailure(string message)
		{
			File.WriteAllText(Path.Combine(_outputDirectory, "style-proof-runtime-failure.txt"), message);
		}
	}
}
