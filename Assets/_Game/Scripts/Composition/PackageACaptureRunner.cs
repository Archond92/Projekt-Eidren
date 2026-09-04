using Eidren.AI;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Input;
using Eidren.Interaction;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class PackageACaptureRunner : MonoBehaviour
	{
		[Serializable]
		private sealed class CaptureReport
		{
			public string generatedUtc;

			public string scene;

			public int width;

			public int height;

			public string[] captures;
		}

		private const string Flag = "-eidren-package-a-capture";

		private const string OutputArgument = "-eidren-package-a-output";

		private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

		private string _output;

		private readonly List<string> _captures = new List<string>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void TryCreate()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			if (Array.IndexOf(commandLineArgs, "-eidren-package-a-capture") >= 0 && !(UnityEngine.Object.FindFirstObjectByType<PackageACaptureRunner>() != null))
			{
				GameObject gameObject = new GameObject("PackageACaptureRunner");
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<PackageACaptureRunner>();
			}
		}

		private void Awake()
		{
			_output = ReadArgument(Environment.GetCommandLineArgs(), "-eidren-package-a-output", Path.Combine(Application.persistentDataPath, "PackageA"));
			Directory.CreateDirectory(_output);
			Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
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
			float timeout = Time.realtimeSinceStartup + 12f;
			while (Time.realtimeSinceStartup < timeout)
			{
				player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
				input = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
				if (player != null && input != null)
				{
					break;
				}
				yield return null;
			}
			if (player == null || input == null)
			{
				Fail("Player or input did not initialize in Greenwood.");
				yield break;
			}
			EnemyControllerBase[] array = UnityEngine.Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None);
			foreach (EnemyControllerBase enemy in array)
			{
				enemy.gameObject.SetActive(value: false);
			}
			player.Motor.Teleport(new Vector3(-5f, 0.05f, 12f));
			Camera camera = Camera.main;
			if (camera != null)
			{
				camera.orthographicSize = 2.8f;
			}
			EquipSet(services, "copper_spear", "armor_copper_helmet", "armor_copper_chest", "armor_copper_gloves", "armor_copper_legs");
			player.Combat.TrySelectWeapon("copper_spear");
			yield return FaceForCapture(input);
			yield return Frames(20);
			yield return Capture("01_copper_set_regular_spear.bmp");
			EquipSet(services, "ember_thorn", "armor_iron_helmet", "armor_copper_chest", "armor_iron_gloves", "armor_copper_legs");
			player.Combat.TrySelectWeapon("ember_thorn");
			yield return FaceForCapture(input);
			yield return Frames(20);
			PlayerWeaponVisual weaponVisual = player.GetComponentInChildren<PlayerWeaponVisual>(includeInactive: true);
			if (weaponVisual == null || weaponVisual.CurrentSpearSprite != weaponVisual.EmberThornSprite)
			{
				Fail("Ember Thorn did not replace the regular spear sprite.");
				yield break;
			}
			yield return Capture("02_mixed_set_ember_thorn.bmp");
			PlayerInventory inventory = services.GameSession.PlayerInventory;
			inventory.Clear();
			if (inventory.Add("iron_axe", 1) != 0)
			{
				Fail("Iron axe could not be added to the capture inventory.");
				yield break;
			}
			ResourceNode tree = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault((ResourceNode value) => value.Definition != null && value.Definition.Id == "resource.tree");
			if (tree != null)
			{
				ResourceNode[] array2 = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
				foreach (ResourceNode resource in array2)
				{
					if (resource != tree)
					{
						resource.gameObject.SetActive(value: false);
					}
				}
				ItemDefinition resolvedTool = tree.ResolveTool(inventory);
				if (resolvedTool == null || resolvedTool.Id != "iron_axe")
				{
					Fail("Tree resolved the wrong capture tool: " + ((resolvedTool != null) ? resolvedTool.Id : "none"));
					yield break;
				}
				Vector3 playerSide = ((camera != null) ? (camera.transform.position - tree.transform.position) : Vector3.back);
				playerSide.y = 0f;
				playerSide = ((playerSide.sqrMagnitude > 0.01f) ? playerSide.normalized : Vector3.back);
				player.Motor.Teleport(tree.transform.position + playerSide * 1.4f + Vector3.up * 0.05f);
				Physics.SyncTransforms();
				yield return FaceForCapture(input);
				player.Interaction.RefreshTargetsNow();
				if (player.Interaction.CurrentTarget != tree)
				{
					Fail("Tree did not become the active interaction target.");
					yield break;
				}
				input.SetVirtualInteract(held: true);
				timeout = Time.realtimeSinceStartup + 4f;
				while (!(player.VisualAnimator.HarvestVisual?.ToolVisible ?? false) && Time.realtimeSinceStartup < timeout)
				{
					yield return null;
				}
				PlayerHarvestVisual harvest = player.VisualAnimator.HarvestVisual;
				if (harvest == null || !harvest.ToolVisible || harvest.ActiveToolItemId != "iron_axe")
				{
					input.SetVirtualInteract(held: false);
					Fail("Iron axe visual failure: " + $"interacting={player.Interaction.IsInteracting}, " + $"state={player.Interaction.State}, " + $"inputHeld={input.InteractHeld}, " + $"visual={harvest != null}, " + $"harvesting={harvest?.IsHarvesting}, " + $"visible={harvest?.ToolVisible}, " + "activeTool=" + (harvest?.ActiveToolItemId ?? "none") + ", " + $"snapshots={harvest?.SnapshotCount}, " + $"lastSnapshot={harvest?.LastSnapshotState}, " + "lastResolved=" + harvest?.LastResolvedToolItemId + ", " + $"configured={harvest?.PresentationConfigured}, " + string.Format("hasIronAxe={0}.", harvest?.HasPresentationFor("iron_axe")));
				}
				else
				{
					yield return Capture("03_iron_axe_harvest.bmp");
					input.SetVirtualInteract(held: false);
					File.WriteAllText(Path.Combine(_output, "package-a-capture-report.json"), JsonUtility.ToJson(new CaptureReport
					{
						generatedUtc = DateTime.UtcNow.ToString("O"),
						scene = SceneManager.GetActiveScene().name,
						width = Screen.width,
						height = Screen.height,
						captures = _captures.ToArray()
					}, prettyPrint: true));
					Application.Quit(0);
				}
			}
			else
			{
				Fail("No tree resource initialized in Greenwood.");
			}
		}

		private static void EquipSet(EidrenServiceRoot services, string weapon, string head, string chest, string hands, string legs)
		{
			PlayerEquipment playerEquipment = services.GameSession.PlayerEquipment;
			Equip(services, playerEquipment, EquipmentSlot.Weapon1, weapon);
			Equip(services, playerEquipment, EquipmentSlot.Head, head);
			Equip(services, playerEquipment, EquipmentSlot.Chest, chest);
			Equip(services, playerEquipment, EquipmentSlot.Hands, hands);
			Equip(services, playerEquipment, EquipmentSlot.Legs, legs);
		}

		private static void Equip(EidrenServiceRoot services, PlayerEquipment equipment, EquipmentSlot slot, string itemId)
		{
			ItemStack stack = ItemStack.Create(services.ContentDatabase.GetItem(itemId), 1);
			if (!equipment.TryEquip(slot, stack, out var error))
			{
				throw new InvalidOperationException(error);
			}
		}

		private IEnumerator Capture(string filename)
		{
			yield return EndOfFrame;
			string path = Path.Combine(_output, filename);
			Camera camera = Camera.main;
			if (camera == null)
			{
				throw new InvalidOperationException("Capture camera is missing.");
			}
			Texture2D capture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: false);
			RenderTexture target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
			RenderTexture previousActive = RenderTexture.active;
			RenderTexture previousTarget = camera.targetTexture;
			try
			{
				camera.targetTexture = target;
				RenderTexture.active = target;
				camera.Render();
				capture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0, recalculateMipMaps: false);
				capture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
			}
			finally
			{
				camera.targetTexture = previousTarget;
				RenderTexture.active = previousActive;
				target.Release();
				UnityEngine.Object.Destroy(target);
			}
			WriteBmp(capture, path);
			UnityEngine.Object.Destroy(capture);
			if (!File.Exists(path) || new FileInfo(path).Length < 100000)
			{
				throw new IOException("Capture was not written: " + path);
			}
			_captures.Add(filename);
		}

		private static void WriteBmp(Texture2D texture, string path)
		{
			int width = texture.width;
			int height = texture.height;
			int num = (width * 3 + 3) & -4;
			int num2 = num * height;
			Color32[] pixels = texture.GetPixels32();
			using FileStream output = File.Create(path);
			using BinaryWriter binaryWriter = new BinaryWriter(output);
			binaryWriter.Write((ushort)19778);
			binaryWriter.Write(54 + num2);
			binaryWriter.Write((ushort)0);
			binaryWriter.Write((ushort)0);
			binaryWriter.Write(54);
			binaryWriter.Write(40);
			binaryWriter.Write(width);
			binaryWriter.Write(height);
			binaryWriter.Write((ushort)1);
			binaryWriter.Write((ushort)24);
			binaryWriter.Write(0);
			binaryWriter.Write(num2);
			binaryWriter.Write(2835);
			binaryWriter.Write(2835);
			binaryWriter.Write(0);
			binaryWriter.Write(0);
			int num3 = num - width * 3;
			for (int i = 0; i < height; i++)
			{
				int num4 = i * width;
				for (int j = 0; j < width; j++)
				{
					Color32 color = pixels[num4 + j];
					binaryWriter.Write(color.b);
					binaryWriter.Write(color.g);
					binaryWriter.Write(color.r);
				}
				for (int k = 0; k < num3; k++)
				{
					binaryWriter.Write((byte)0);
				}
			}
		}

		private static IEnumerator Load(string sceneName)
		{
			AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
			while (load != null && !load.isDone)
			{
				yield return null;
			}
			yield return Frames(16);
		}

		private static IEnumerator Frames(int count)
		{
			for (int index = 0; index < count; index++)
			{
				yield return null;
			}
		}

		private static IEnumerator FaceForCapture(PlayerInputReader input)
		{
			input.SetGameplayEnabled(enabled: true);
			input.SetVirtualMove(new Vector2(0f, -1f));
			yield return Frames(2);
			input.SetVirtualMove(Vector2.zero);
			yield return Frames(4);
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

		private void Fail(string message)
		{
			File.WriteAllText(Path.Combine(_output, "package-a-capture-failure.txt"), message);
			Application.Quit(1);
		}
	}
}
