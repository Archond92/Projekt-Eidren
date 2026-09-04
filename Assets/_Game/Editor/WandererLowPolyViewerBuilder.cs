using Eidren.Presentation;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Eidren.Editor
{
	public static class WandererLowPolyViewerBuilder
	{
		private const string VisualPath = "Assets/_Game/Prefabs/Actors/3D/Player_3D.prefab";
		private const string GlbPath = "Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb";
		private const string ScenePath = "Assets/_Game/Scenes/Review/WandererLowPolyViewer.unity";
		private const string MaterialRoot = "Assets/_Game/Materials/Viewer";
		private const string ReportPath = "Documentation/Etappen/MidPoly/WandererLowPoly/WANDERER_LOW_POLY_VIEWER_REPORT.json";

		[Serializable]
		private sealed class ViewerReport
		{
			public bool pass;
			public string generatedUtc;
			public string scene;
			public string visualPrefab;
			public int armorPieces;
			public int triangles;
			public int animationClips;
			public int handItemVariants;
			public string[] errors;
		}

		[MenuItem("Eidren/Viewer/Wanderer Low-Poly Viewer bauen und oeffnen")]
		public static void BuildAndOpen()
		{
			BuildViewerScene();
			EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
			FrameWanderer();
		}

		[MenuItem("Eidren/Viewer/Wanderer Low-Poly Viewer bauen und starten")]
		public static void BuildAndPlay()
		{
			BuildAndOpen();
			EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
		}

		public static void BuildViewerScene()
		{
			Wanderer3DPlayerBuilder.BuildLowPoly();
			Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
			Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			GameObject root = new GameObject("WandererLowPolyViewer");
			GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
			if (visualPrefab == null) throw new InvalidOperationException("Low-Poly-Visual fehlt: " + VisualPath);
			GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, scene);
			visual.name = "Wanderer_LowPoly";
			visual.transform.SetParent(root.transform, false);
			MeshActorPresentation presentation = visual.GetComponent<MeshActorPresentation>();
			Animation animationPlayer = visual.GetComponentInChildren<Animation>(true);
			if (presentation == null || animationPlayer == null)
				throw new InvalidOperationException("Viewer benoetigt MeshActorPresentation und Animation.");
			presentation.enabled = false;

			Camera camera = CreateCamera();
			CreateLighting();
			CreateStage();
			WandererLowPolyViewerController controller = root.AddComponent<WandererLowPolyViewerController>();
			controller.Configure(visual.transform, presentation, animationPlayer, camera);

			RenderSettings.ambientMode = AmbientMode.Trilight;
			RenderSettings.ambientSkyColor = new Color(0.48f, 0.52f, 0.60f);
			RenderSettings.ambientEquatorColor = new Color(0.30f, 0.33f, 0.39f);
			RenderSettings.ambientGroundColor = new Color(0.15f, 0.16f, 0.19f);
			RenderSettings.fog = false;
			RenderSettings.skybox = null;

			EditorSceneManager.SaveScene(scene, ScenePath);
			EnsureDisabledBuildEntry();
			WriteAndValidateReport(visual);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("[W3D] Low-Poly-Viewer gebaut: " + ScenePath);
		}

		private static Camera CreateCamera()
		{
			GameObject cameraObject = new GameObject("ViewerCamera");
			cameraObject.tag = "MainCamera";
			Camera camera = cameraObject.AddComponent<Camera>();
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0.055f, 0.070f, 0.095f);
			camera.fieldOfView = 32f;
			camera.nearClipPlane = 0.05f;
			camera.farClipPlane = 60f;
			camera.allowHDR = true;
			cameraObject.AddComponent<AudioListener>();
			return camera;
		}

		private static void CreateLighting()
		{
			GameObject keyObject = new GameObject("KeyLight");
			Light key = keyObject.AddComponent<Light>();
			key.type = LightType.Directional;
			key.color = new Color(1f, 0.84f, 0.68f);
			key.intensity = 2.8f;
			key.shadows = LightShadows.Soft;
			key.transform.rotation = Quaternion.Euler(38f, -35f, 0f);

			CreatePointLight("FrontLight", new Vector3(0f, 2.4f, 3.6f), new Color(1f, 0.92f, 0.82f), 6.5f, 8f);
			CreatePointLight("FillLight", new Vector3(-2.5f, 2.4f, 2.4f), new Color(0.36f, 0.58f, 1f), 3.5f, 7f);
			CreatePointLight("RimLight", new Vector3(2.2f, 2.8f, -2.1f), new Color(1f, 0.32f, 0.14f), 5f, 7f);
		}

		private static void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
		{
			GameObject lightObject = new GameObject(name);
			Light light = lightObject.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = color;
			light.intensity = intensity;
			light.range = range;
			light.shadows = LightShadows.None;
			light.transform.position = position;
		}

		private static void CreateStage()
		{
			Material floor = EnsureMaterial(MaterialRoot + "/M_ViewerFloor.mat", new Color(0.055f, 0.065f, 0.085f), 0.05f, 0.72f);
			Material pedestal = EnsureMaterial(MaterialRoot + "/M_ViewerPedestal.mat", new Color(0.16f, 0.18f, 0.22f), 0.35f, 0.45f);

			GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
			plane.name = "ViewerFloor";
			plane.transform.position = new Vector3(0f, -0.12f, 0f);
			plane.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
			plane.GetComponent<Renderer>().sharedMaterial = floor;
			UnityEngine.Object.DestroyImmediate(plane.GetComponent<Collider>());

			GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			plinth.name = "ViewerPedestal";
			plinth.transform.position = new Vector3(0f, -0.06f, 0f);
			plinth.transform.localScale = new Vector3(0.82f, 0.06f, 0.82f);
			plinth.GetComponent<Renderer>().sharedMaterial = pedestal;
			UnityEngine.Object.DestroyImmediate(plinth.GetComponent<Collider>());
		}

		private static Material EnsureMaterial(string path, Color color, float metallic, float smoothness)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
			Shader shader = Shader.Find("Universal Render Pipeline/Lit");
			if (shader == null) throw new InvalidOperationException("URP/Lit-Shader fehlt.");
			if (material == null)
			{
				material = new Material(shader);
				AssetDatabase.CreateAsset(material, path);
			}
			material.shader = shader;
			material.color = color;
			material.SetFloat("_Metallic", metallic);
			material.SetFloat("_Smoothness", smoothness);
			EditorUtility.SetDirty(material);
			return material;
		}

		private static void EnsureDisabledBuildEntry()
		{
			EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath).ToArray();
			EditorBuildSettings.scenes = scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, false) }).ToArray();
		}

		private static void WriteAndValidateReport(GameObject visual)
		{
			string[] errors = Validate(visual);
			int clips = AssetDatabase.LoadAllAssetsAtPath(GlbPath).OfType<AnimationClip>()
				.Count(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
			ViewerReport report = new ViewerReport
			{
				pass = errors.Length == 0,
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				scene = ScenePath,
				visualPrefab = VisualPath,
				armorPieces = WandererLowPolyViewerController.ArmorPieceCount,
				triangles = TriangleCount(visual),
				animationClips = clips,
				handItemVariants = WandererLowPolyViewerController.HandItemVariantCount,
				errors = errors
			};
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
			if (!report.pass) throw new InvalidOperationException("Low-Poly-Viewer FAIL:\n" + string.Join("\n", errors));
		}

		private static string[] Validate(GameObject visual)
		{
			var errors = new System.Collections.Generic.List<string>();
			foreach (string node in new[]
			{
				"Basis", "Helm_Stoff", "Helm_Kupfer", "Helm_Eisen", "Harnisch_Stoff", "Harnisch_Kupfer", "Harnisch_Eisen",
				"Haende_Stoff", "Haende_Kupfer", "Haende_Eisen", "Beine_Stoff", "Beine_Kupfer", "Beine_Eisen",
				"Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer", "Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense"
			})
				if (FindDeep(visual.transform, node) == null) errors.Add("Knoten fehlt: " + node);
			string[] clips = AssetDatabase.LoadAllAssetsAtPath(GlbPath).OfType<AnimationClip>()
				.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).Select(clip => clip.name).OrderBy(name => name).ToArray();
			if (!clips.SequenceEqual(WandererLowPolyViewerController.ExpectedClipNames.OrderBy(name => name), StringComparer.Ordinal))
				errors.Add("Clipvertrag weicht von den 25 erwarteten Animationen ab.");
			if (visual.GetComponent<LODGroup>() != null) errors.Add("Low-Poly-Wanderer darf keine Mid-Poly-LODGroup tragen.");
			return errors.ToArray();
		}

		private static Transform FindDeep(Transform root, string name)
		{
			return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
		}

		private static int TriangleCount(GameObject root)
		{
			return root.GetComponentsInChildren<Renderer>(true).Select(renderer =>
			{
				if (renderer is SkinnedMeshRenderer skinned) return skinned.sharedMesh;
				MeshFilter filter = renderer.GetComponent<MeshFilter>();
				return filter != null ? filter.sharedMesh : null;
			}).Where(mesh => mesh != null).Distinct()
				.Sum(mesh => Enumerable.Range(0, mesh.subMeshCount).Sum(index => (int)mesh.GetIndexCount(index) / 3));
		}

		private static void FrameWanderer()
		{
			GameObject wanderer = GameObject.Find("Wanderer_LowPoly");
			if (wanderer == null) return;
			Selection.activeGameObject = wanderer;
			SceneView.lastActiveSceneView?.FrameSelected();
		}
	}
}
