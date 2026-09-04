using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Rendert Gebäude-Prefabs zu Katalog-Icons (F31-013). Werkbank und
	/// Lagerkiste haben — anders als die neun übrigen Gebäude — nie gemalte
	/// 1024er-Illustrationen bekommen; bis ein Grafikauftrag sie nachliefert,
	/// zeigen sie das Engine-Render ihres tatsächlichen Spielmodells statt
	/// fachfremder Item-Icons (Hammer, Brett).
	/// Läuft NUR ohne -nographics (braucht ein echtes Grafikgerät).
	/// </summary>
	public static class BuildingIconRenderTool
	{
		private const int Aufloesung = 1024;

		private static readonly (string PrefabPath, string IconPath)[] Auftraege =
		{
			("Assets/_Game/Prefabs/Stations/Workbench.prefab", "Assets/_Game/Art/Buildings/Level01/BLD_Workbench_L01.png"),
			("Assets/_Game/Prefabs/Stations/StorageChest.prefab", "Assets/_Game/Art/Buildings/Level01/BLD_StorageChest_L01.png")
		};

		[MenuItem("Eidren/Art/Render Building Icons (Workbench, StorageChest)")]
		public static void RenderMissingIcons()
		{
			if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
			{
				throw new InvalidDataException("Icon-Render braucht ein Grafikgeraet — ohne -nographics starten.");
			}
			foreach ((string prefabPath, string iconPath) in Auftraege)
			{
				Render(prefabPath, iconPath);
			}
			AssetDatabase.Refresh();
			foreach ((string _, string iconPath) in Auftraege)
			{
				ArtAssetImportUtility.ConfigureSprite(iconPath, 512);
			}
			Debug.Log("Eidren: 2 Gebaeude-Icons gerendert (Workbench, StorageChest).");
		}

		/// <summary>
		/// F31-003: Beide Kupferader-Zustände nebeneinander für den
		/// Bildabgleich — aktiv links, abgebaut rechts.
		/// </summary>
		[MenuItem("Eidren/Art/Render Kupferader-Vergleich (F31-003)")]
		public static void RenderCopperVeinComparison()
		{
			if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
			{
				throw new InvalidDataException("Render braucht ein Grafikgeraet — ohne -nographics starten.");
			}
			GameObject aktivPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Active.prefab");
			GameObject verbrauchtPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Exhausted.prefab");
			if (aktivPrefab == null || verbrauchtPrefab == null)
			{
				throw new InvalidDataException("Kupferader-Prefabs fehlen.");
			}
			GameObject aktiv = Object.Instantiate(aktivPrefab, new Vector3(-1.6f, 0f, 0f), Quaternion.identity);
			GameObject verbraucht = Object.Instantiate(verbrauchtPrefab, new Vector3(1.6f, 0f, 0f), Quaternion.identity);
			GameObject lightObject = new GameObject("VeinRender_Light");
			GameObject cameraObject = new GameObject("VeinRender_Camera");
			RenderTexture target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
			try
			{
				Light light = lightObject.AddComponent<Light>();
				light.type = LightType.Directional;
				light.intensity = 1.35f;
				light.transform.rotation = Quaternion.Euler(50f, -32f, 0f);
				Camera camera = cameraObject.AddComponent<Camera>();
				camera.orthographic = true;
				camera.clearFlags = CameraClearFlags.SolidColor;
				camera.backgroundColor = new Color(0.13f, 0.15f, 0.13f, 1f);
				Quaternion view = Quaternion.Euler(38f, 45f, 0f);
				camera.transform.rotation = view;
				camera.transform.position = new Vector3(0f, 0.6f, 0f) - view * Vector3.forward * 12f;
				camera.orthographicSize = 2.1f;
				camera.targetTexture = target;
				camera.Render();
				RenderTexture previous = RenderTexture.active;
				RenderTexture.active = target;
				Texture2D image = new Texture2D(1600, 900, TextureFormat.RGB24, mipChain: false);
				image.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
				image.Apply();
				RenderTexture.active = previous;
				Directory.CreateDirectory("TempReview");
				File.WriteAllBytes("TempReview/f31-kupferader-vergleich.png", image.EncodeToPNG());
				Object.DestroyImmediate(image);
				Debug.Log("Eidren: Kupferader-Vergleich geschrieben.");
			}
			finally
			{
				Object.DestroyImmediate(aktiv);
				Object.DestroyImmediate(verbraucht);
				Object.DestroyImmediate(lightObject);
				Object.DestroyImmediate(cameraObject);
				Object.DestroyImmediate(target);
			}
		}

		private static void Render(string prefabPath, string iconPath)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			if (prefab == null)
			{
				throw new InvalidDataException("Prefab fehlt: " + prefabPath);
			}
			GameObject instance = Object.Instantiate(prefab);
			GameObject lightObject = new GameObject("IconRender_Light");
			GameObject cameraObject = new GameObject("IconRender_Camera");
			RenderTexture target = new RenderTexture(Aufloesung, Aufloesung, 24, RenderTextureFormat.ARGB32);
			try
			{
				Light light = lightObject.AddComponent<Light>();
				light.type = LightType.Directional;
				light.intensity = 1.35f;
				light.transform.rotation = Quaternion.Euler(50f, -32f, 0f);
				Bounds bounds = RendererBounds(instance);
				Camera camera = cameraObject.AddComponent<Camera>();
				camera.orthographic = true;
				camera.clearFlags = CameraClearFlags.SolidColor;
				camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
				// Blickwinkel der Spielkamera (52°/45°), leicht abgeflacht,
				// damit die Front sichtbar bleibt wie bei den gemalten Icons.
				Quaternion view = Quaternion.Euler(38f, 45f, 0f);
				camera.transform.rotation = view;
				camera.transform.position = bounds.center - view * Vector3.forward * (bounds.size.magnitude * 2f + 4f);
				camera.orthographicSize = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)) * 1.25f;
				camera.nearClipPlane = 0.1f;
				camera.farClipPlane = bounds.size.magnitude * 6f + 20f;
				camera.targetTexture = target;
				camera.Render();
				RenderTexture previous = RenderTexture.active;
				RenderTexture.active = target;
				Texture2D image = new Texture2D(Aufloesung, Aufloesung, TextureFormat.RGBA32, mipChain: false);
				image.ReadPixels(new Rect(0f, 0f, Aufloesung, Aufloesung), 0, 0);
				image.Apply();
				RenderTexture.active = previous;
				File.WriteAllBytes(iconPath, image.EncodeToPNG());
				Object.DestroyImmediate(image);
			}
			finally
			{
				Object.DestroyImmediate(instance);
				Object.DestroyImmediate(lightObject);
				Object.DestroyImmediate(cameraObject);
				Object.DestroyImmediate(target);
			}
		}

		private static Bounds RendererBounds(GameObject instance)
		{
			Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(includeInactive: false);
			if (renderers.Length == 0)
			{
				throw new InvalidDataException("Prefab '" + instance.name + "' hat keine Renderer.");
			}
			Bounds bounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers)
			{
				bounds.Encapsulate(renderer.bounds);
			}
			return bounds;
		}
	}
}
