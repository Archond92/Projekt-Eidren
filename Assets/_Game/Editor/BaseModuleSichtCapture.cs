using Eidren.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>Rendert ausschliesslich die wirklich integrierten Phase-4-Prefabs.</summary>
	public static class BaseModuleSichtCapture
	{
		private const string PrefabFolder = "Assets/_Game/Prefabs/Buildings/Level01";
		private const int Width = 1100;
		private const int Height = 900;

		[MenuItem("Eidren/Mid-Poly/Phase 4/Sichtpruefung Basismodule")]
		public static void Capture()
		{
			string folder = Environment.GetEnvironmentVariable("EIDREN_CAPTURE_ORDNER");
			if (string.IsNullOrWhiteSpace(folder))
				folder = "Documentation/Etappen/MidPoly/Phase4/InWorld/BaseModulesFinal";
			Directory.CreateDirectory(folder);

			List<GameObject> created = new List<GameObject>();
			GameObject cameraObject = New("BaseModuleCaptureCamera", created);
			GameObject keyObject = New("BaseModuleCaptureKey", created);
			GameObject fillObject = New("BaseModuleCaptureFill", created);
			GameObject rimObject = New("BaseModuleCaptureRim", created);
			try
			{
				Camera camera = cameraObject.AddComponent<Camera>();
				camera.clearFlags = CameraClearFlags.SolidColor;
				camera.backgroundColor = new Color(0.065f, 0.078f, 0.09f);
				camera.fieldOfView = 35f;
				Light key = keyObject.AddComponent<Light>();
				key.type = LightType.Directional;
				key.intensity = 1.28f;
				keyObject.transform.rotation = Quaternion.Euler(46f, 148f, 0f);
				Light fill = fillObject.AddComponent<Light>();
				fill.type = LightType.Directional;
				fill.intensity = 0.48f;
				fillObject.transform.rotation = Quaternion.Euler(30f, -38f, 0f);
				Light rim = rimObject.AddComponent<Light>();
				rim.type = LightType.Directional;
				rim.intensity = 0.40f;
				rimObject.transform.rotation = Quaternion.Euler(18f, 212f, 0f);

				GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
				ground.name = "BaseModuleCaptureGround";
				ground.transform.position = new Vector3(0f, -0.07f, 0f);
				ground.transform.localScale = new Vector3(9f, 0.10f, 9f);
				created.Add(ground);
				Material groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
				groundMaterial.color = new Color(0.10f, 0.12f, 0.13f);
				ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

				GameObject wall = Instance("Wall", created);
				Render(camera, folder, "base_module_wall_integrated.png", new Vector3(2.8f, 2.7f, 4.8f), new Vector3(0f, 1.28f, 0f));
				wall.SetActive(false);

				GameObject floor = Instance("Floor", created);
				Render(camera, folder, "base_module_floor_integrated.png", new Vector3(2.25f, 2.25f, 2.75f), new Vector3(0f, 0.08f, 0f));
				floor.SetActive(false);

				GameObject door = Instance("Door", created);
				BuildingDoorView view = door.GetComponentInChildren<BuildingDoorView>(true);
				Render(camera, folder, "base_module_door_closed_integrated.png", new Vector3(2.8f, 2.7f, 4.8f), new Vector3(0f, 1.28f, 0f));
				view.Blade.localRotation = Quaternion.Euler(0f, 105f, 0f);
				Render(camera, folder, "base_module_door_open_integrated.png", new Vector3(3.2f, 2.9f, 4.9f), new Vector3(0f, 1.22f, 0f));
				door.SetActive(false);

				// Kleines echtes Baufragment: drei Bodenfelder und Wall-Door-Wall
				// auf derselben Ein-Meter-Rasterlinie.
				for (int index = -1; index <= 1; index++)
				{
					GameObject tile = Instance("Floor", created);
					tile.transform.position = new Vector3(index, 0f, 0.05f);
					GameObject edge = Instance(index == 0 ? "Door" : "Wall", created);
					edge.transform.position = new Vector3(index, 0f, 0.54f);
				}
				Render(camera, folder, "base_module_kit_integrated.png", new Vector3(4.35f, 3.35f, 6.4f), new Vector3(0f, 1.15f, 0.25f));
				Debug.Log("[MIDPOLY-000] Basismodul-Sichtpruefung geschrieben: " + folder);
				UnityEngine.Object.DestroyImmediate(groundMaterial);
			}
			finally
			{
				foreach (GameObject item in created)
					if (item != null) UnityEngine.Object.DestroyImmediate(item);
			}
		}

		private static GameObject New(string name, List<GameObject> created)
		{
			GameObject item = new GameObject(name);
			created.Add(item);
			return item;
		}

		private static GameObject Instance(string name, List<GameObject> created)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/BLD_" + name + "_L01.prefab");
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			instance.name = name + "_IntegratedCapture";
			LODGroup group = instance.GetComponent<LODGroup>();
			if (group != null) group.ForceLOD(0);
			created.Add(instance);
			return instance;
		}

		private static void Render(Camera camera, string folder, string filename, Vector3 position, Vector3 target)
		{
			camera.transform.position = position;
			camera.transform.LookAt(target);
			RenderTexture texture = new RenderTexture(Width, Height, 24);
			camera.targetTexture = texture;
			camera.Render();
			RenderTexture.active = texture;
			Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
			image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
			image.Apply();
			File.WriteAllBytes(Path.Combine(folder, filename), image.EncodeToPNG());
			RenderTexture.active = null;
			camera.targetTexture = null;
			UnityEngine.Object.DestroyImmediate(texture);
			UnityEngine.Object.DestroyImmediate(image);
		}
	}
}
