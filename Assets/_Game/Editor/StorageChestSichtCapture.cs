using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Rendert den wirklich integrierten Produktions-Prefab geschlossen und auf
	/// der geprueften Scharnierachse geoeffnet. Es wird keine Laufzeitanimation
	/// vorgetaeuscht; die geoeffnete Pose dient als Achsen-/Clippingabnahme.
	/// </summary>
	public static class StorageChestSichtCapture
	{
		private const string PrefabPath = "Assets/_Game/Prefabs/Stations/StorageChest.prefab";
		private const int Size = 900;

		[MenuItem("Eidren/Mid-Poly/Phase 4/Sichtpruefung Lagerkiste")]
		public static void Capture()
		{
			string folder = Environment.GetEnvironmentVariable("EIDREN_CAPTURE_ORDNER");
			if (string.IsNullOrWhiteSpace(folder))
				folder = "Documentation/Etappen/MidPoly/Phase4/InWorld/StorageChestFinal";
			Directory.CreateDirectory(folder);

			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			GameObject cameraObject = new GameObject("StorageChestCaptureCamera");
			GameObject keyObject = new GameObject("StorageChestCaptureKey");
			GameObject fillObject = new GameObject("StorageChestCaptureFill");
			try
			{
				LODGroup group = instance.GetComponent<LODGroup>();
				if (group != null) group.ForceLOD(0);
				Camera camera = cameraObject.AddComponent<Camera>();
				camera.clearFlags = CameraClearFlags.SolidColor;
				camera.backgroundColor = new Color(0.075f, 0.09f, 0.105f);
				camera.fieldOfView = 36f;
				Light key = keyObject.AddComponent<Light>();
				key.type = LightType.Directional;
				key.intensity = 1.25f;
				keyObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
				Light fill = fillObject.AddComponent<Light>();
				fill.type = LightType.Directional;
				fill.intensity = 0.52f;
				fillObject.transform.rotation = Quaternion.Euler(28f, 142f, 0f);

				Render(camera, folder, "storage_chest_closed_hero.png", new Vector3(1.40f, 1.15f, -2.15f), new Vector3(0f, 0.39f, 0f));
				Render(camera, folder, "storage_chest_closed_front.png", new Vector3(0f, 0.67f, -2.35f), new Vector3(0f, 0.38f, 0f));
				Render(camera, folder, "storage_chest_closed_side.png", new Vector3(2.15f, 0.82f, -0.20f), new Vector3(0f, 0.39f, 0f));

				Transform lid = instance.GetComponentsInChildren<Transform>(true)
					.First(item => item.name.StartsWith("ChestLid_LOD0", StringComparison.Ordinal));
				Quaternion closed = lid.localRotation;
				lid.localRotation = closed * Quaternion.AngleAxis(-105f, Vector3.right);
				Render(camera, folder, "storage_chest_open_hero.png", new Vector3(1.48f, 1.42f, -2.28f), new Vector3(0f, 0.55f, 0f));
				Render(camera, folder, "storage_chest_open_side.png", new Vector3(2.25f, 1.30f, -0.12f), new Vector3(0f, 0.56f, 0f));
				lid.localRotation = closed;
				Debug.Log("[MIDPOLY-000] Lagerkisten-Sichtpruefung geschrieben: " + folder);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
				UnityEngine.Object.DestroyImmediate(cameraObject);
				UnityEngine.Object.DestroyImmediate(keyObject);
				UnityEngine.Object.DestroyImmediate(fillObject);
			}
		}

		private static void Render(Camera camera, string folder, string filename, Vector3 position, Vector3 target)
		{
			camera.transform.position = position;
			camera.transform.LookAt(target);
			RenderTexture texture = new RenderTexture(Size, Size, 24);
			camera.targetTexture = texture;
			camera.Render();
			RenderTexture.active = texture;
			Texture2D image = new Texture2D(Size, Size, TextureFormat.RGB24, false);
			image.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
			image.Apply();
			File.WriteAllBytes(Path.Combine(folder, filename), image.EncodeToPNG());
			RenderTexture.active = null;
			camera.targetTexture = null;
			UnityEngine.Object.DestroyImmediate(texture);
			UnityEngine.Object.DestroyImmediate(image);
		}
	}
}
