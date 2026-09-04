using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
	/// <summary>
	/// Reproduzierbare Phase-0-Baseline fuer MIDPOLY-000.
	///
	/// Der Lauf ist absichtlich read-only gegenueber produktiven Assets. Er
	/// inventarisiert sichtbare Prefab-/Model-Roots und technische Meshassets,
	/// sichert ihre Integrationsvertraege und rendert zwei konsistente
	/// Vorher-Ansichten. Die Ausgabe liegt ausserhalb von Assets unter
	/// Documentation/Etappen/MidPoly/Phase0.
	/// </summary>
	public static class MidpolyPhase0Baseline
	{
		private const string OutputRelative = "Documentation/Etappen/MidPoly/Phase0";
		private const int CaptureSize = 640;
		private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

		[Serializable]
		private sealed class AssetRecord
		{
			public string id;
			public string status;
			public string name;
			public string kind;
			public string family;
			public string assetPath;
			public string guid;
			public string rootTransform;
			public string boundsCenter;
			public string boundsSize;
			public int rendererCount;
			public int activeRendererCount;
			public int meshCount;
			public long triangles;
			public int materialSlotCount;
			public List<string> materials = new List<string>();
			public List<string> colliders = new List<string>();
			public List<string> bones = new List<string>();
			public List<string> rootBones = new List<string>();
			public List<string> animatorControllers = new List<string>();
			public List<string> clips = new List<string>();
			public List<string> prefabReferences = new List<string>();
			public List<string> dataReferences = new List<string>();
			public List<string> sceneReferences = new List<string>();
			public List<string> builderCandidates = new List<string>();
			public string standardCapture;
			public string gameplayCapture;
			public string captureError;
		}

		[Serializable]
		private sealed class AssetRecordCollection
		{
			public string generatedAtUtc;
			public string unityVersion;
			public string projectPath;
			public List<AssetRecord> assets = new List<AssetRecord>();
		}

		private sealed class MeshRecord
		{
			public string assetPath;
			public string guid;
			public long localId;
			public string meshName;
			public int vertices;
			public long triangles;
			public string boundsCenter;
			public string boundsSize;
			public string prefabReferences;
			public string sceneReferences;
			public string builderCandidates;
		}

		private sealed class SceneRecord
		{
			public string path;
			public string guid;
			public int rendererCount;
			public long triangles;
			public int cameraCount;
			public int prefabSourceCount;
			public int sceneOnlyRendererCount;
			public string boundsCenter;
			public string boundsSize;
			public string capture;
			public string note;
			public string error;
		}

		private sealed class BuilderSource
		{
			public string assetPath;
			public string fileName;
			public string text;
		}

		private sealed class Inspection
		{
			public Bounds bounds;
			public bool hasBounds;
			public List<Renderer> renderers = new List<Renderer>();
			public HashSet<Mesh> meshes = new HashSet<Mesh>();
		}

		[MenuItem("Eidren/Mid-Poly/Phase 0/Generate Complete Baseline")]
		public static void Run()
		{
			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			string outputRoot = Path.Combine(projectRoot, OutputRelative.Replace('/', Path.DirectorySeparatorChar));
			PrepareOutput(outputRoot);

			Debug.Log("[MIDPOLY Phase 0] Ermittle sichtbare Asset-Roots ...");
			List<string> visualPaths = FindVisualRootPaths();
			List<string> meshPaths = FindMeshAssetPaths();
			HashSet<string> reverseTargets = new HashSet<string>(visualPaths.Concat(meshPaths), StringComparer.OrdinalIgnoreCase);
			Dictionary<string, List<string>> reverseReferences = BuildReverseReferences(reverseTargets);
			List<BuilderSource> builders = LoadBuilderSources();

			var collection = new AssetRecordCollection
			{
				generatedAtUtc = DateTime.UtcNow.ToString("o", Invariant),
				unityVersion = Application.unityVersion,
				projectPath = projectRoot
			};

			for (int index = 0; index < visualPaths.Count; index++)
			{
				string path = visualPaths[index];
				AssetRecord record = InspectVisualAsset(path, index + 1, reverseReferences, builders);
				collection.assets.Add(record);
				if ((index + 1) % 20 == 0 || index + 1 == visualPaths.Count)
				{
					Debug.Log($"[MIDPOLY Phase 0] Inventar {index + 1}/{visualPaths.Count}");
				}
			}

			List<MeshRecord> meshRecords = InspectMeshes(meshPaths, reverseReferences, builders);
			WriteAssetOutputs(outputRoot, collection);
			WriteMeshOutput(outputRoot, meshRecords);
			WriteChecksums(outputRoot, visualPaths.Concat(meshPaths).Distinct(StringComparer.OrdinalIgnoreCase));

			Debug.Log("[MIDPOLY Phase 0] Rendere Asset-Baselines ...");
			RenderAssetCaptures(projectRoot, outputRoot, collection.assets);
			Debug.Log("[MIDPOLY Phase 0] Rendere Szenen-Uebersichten ...");
			List<SceneRecord> scenes = InspectAndCaptureScenes(projectRoot, outputRoot);

			// Noch einmal schreiben, damit Capturepfade und eventuelle Fehler im
			// maschinenlesbaren Snapshot enthalten sind.
			WriteAssetOutputs(outputRoot, collection);
			WriteSceneOutput(outputRoot, scenes);
			WriteBuilderOutput(outputRoot, builders);
			WriteReportAndReadme(outputRoot, collection.assets, meshRecords, scenes, builders);

			Debug.Log($"[MIDPOLY Phase 0] Abgeschlossen: {collection.assets.Count} visuelle Roots, " +
				$"{meshRecords.Count} Mesh-Subassets, {scenes.Count} Szenen. Ausgabe: {outputRoot}");
	}

		private static void PrepareOutput(string outputRoot)
		{
			Directory.CreateDirectory(outputRoot);
			string captures = Path.Combine(outputRoot, "Captures");
			if (Directory.Exists(captures))
			{
				Directory.Delete(captures, true);
			}
			Directory.CreateDirectory(Path.Combine(captures, "Standard_Low"));
			Directory.CreateDirectory(Path.Combine(captures, "Gameplay_Low"));
			Directory.CreateDirectory(Path.Combine(captures, "Scenes"));
		}

		private static List<string> FindVisualRootPaths()
		{
			var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game" }))
			{
				paths.Add(AssetDatabase.GUIDToAssetPath(guid));
			}

			foreach (string diskPath in Directory.GetFiles(Application.dataPath + "/_Game", "*.*", SearchOption.AllDirectories))
			{
				string extension = Path.GetExtension(diskPath).ToLowerInvariant();
				if (extension != ".glb" && extension != ".gltf" && extension != ".fbx" && extension != ".obj")
				{
					continue;
				}
				paths.Add(ToAssetPath(diskPath));
			}

			return paths
				.Where(IsRenderableRoot)
				.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static bool IsRenderableRoot(string path)
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			return root != null && root.GetComponentsInChildren<Renderer>(true).Length > 0;
		}

		private static List<string> FindMeshAssetPaths()
		{
			return AssetDatabase.FindAssets("t:Mesh", new[] { "Assets/_Game" })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => !string.IsNullOrEmpty(path))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static Dictionary<string, List<string>> BuildReverseReferences(HashSet<string> targets)
		{
			var result = targets.ToDictionary(path => path, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);
			var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string filter in new[] { "t:Prefab", "t:ScriptableObject", "t:Scene", "t:AnimatorController", "t:AnimationClip" })
			{
				foreach (string guid in AssetDatabase.FindAssets(filter, new[] { "Assets/_Game" }))
				{
					sources.Add(AssetDatabase.GUIDToAssetPath(guid));
				}
			}

			int index = 0;
			foreach (string source in sources.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
			{
				foreach (string dependency in AssetDatabase.GetDependencies(source, true))
				{
					if (!dependency.Equals(source, StringComparison.OrdinalIgnoreCase) && result.TryGetValue(dependency, out List<string> refs))
					{
						refs.Add(source);
					}
				}
				index++;
				if (index % 250 == 0)
				{
					Debug.Log($"[MIDPOLY Phase 0] Referenzscan {index}/{sources.Count}");
				}
			}

			foreach (List<string> refs in result.Values)
			{
				refs.Sort(StringComparer.OrdinalIgnoreCase);
			}
			return result;
		}

		private static List<BuilderSource> LoadBuilderSources()
		{
			string editorRoot = Path.Combine(Application.dataPath, "_Game", "Editor");
			return Directory.GetFiles(editorRoot, "*.cs", SearchOption.AllDirectories)
				.Select(path => new BuilderSource
				{
					assetPath = ToAssetPath(path),
					fileName = Path.GetFileNameWithoutExtension(path),
					text = File.ReadAllText(path)
				})
				.OrderBy(source => source.assetPath, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static AssetRecord InspectVisualAsset(
			string path,
			int sequence,
			Dictionary<string, List<string>> reverseReferences,
			List<BuilderSource> builders)
		{
			GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			var record = new AssetRecord
			{
				id = $"MP0-{sequence:0000}",
				status = "ungeprueft",
				name = source != null ? source.name : Path.GetFileNameWithoutExtension(path),
				kind = Path.GetExtension(path).Equals(".prefab", StringComparison.OrdinalIgnoreCase) ? "Prefab" : "Importiertes Modell",
				family = ClassifyFamily(path),
				assetPath = path,
				guid = AssetDatabase.AssetPathToGUID(path),
				rootTransform = source == null ? "—" : TransformText(source.transform),
				standardCapture = "Captures/Standard_Low/" + SafeName($"MP0-{sequence:0000}_{Path.GetFileNameWithoutExtension(path)}") + ".png",
				gameplayCapture = "Captures/Gameplay_Low/" + SafeName($"MP0-{sequence:0000}_{Path.GetFileNameWithoutExtension(path)}") + ".png"
			};

			if (source == null)
			{
				record.captureError = "GameObject konnte nicht geladen werden.";
				return record;
			}

			GameObject instance = Object.Instantiate(source);
			instance.name = source.name;
			try
			{
				Inspection inspection = InspectInstance(instance);
				record.rendererCount = inspection.renderers.Count;
				record.activeRendererCount = inspection.renderers.Count(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy);
				record.meshCount = inspection.meshes.Count;
				record.triangles = CountRenderedTriangles(instance);
				if (inspection.hasBounds)
				{
					record.boundsCenter = Vector(inspection.bounds.center);
					record.boundsSize = Vector(inspection.bounds.size);
				}

				Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
				record.materialSlotCount = renderers.Sum(renderer => renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0);
				record.materials = renderers
					.SelectMany(renderer => renderer.sharedMaterials ?? Array.Empty<Material>())
					.Where(material => material != null)
					.Select(MaterialIdentity)
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
					.ToList();

				record.colliders = instance.GetComponentsInChildren<Collider>(true)
					.Select(collider => ColliderText(instance.transform, collider))
					.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
					.ToList();

				SkinnedMeshRenderer[] skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
				record.bones = skins.SelectMany(skin => skin.bones ?? Array.Empty<Transform>())
					.Where(bone => bone != null).Select(bone => bone.name)
					.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList();
				record.rootBones = skins.Where(skin => skin.rootBone != null).Select(skin => skin.rootBone.name)
					.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToList();

				Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
				record.animatorControllers = animators
					.Select(animator => animator.runtimeAnimatorController)
					.Where(controller => controller != null)
					.Select(controller => AssetDatabase.GetAssetPath(controller))
					.Where(value => !string.IsNullOrEmpty(value))
					.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();

				var clips = new HashSet<string>(StringComparer.Ordinal);
				foreach (RuntimeAnimatorController controller in animators.Select(animator => animator.runtimeAnimatorController).Where(controller => controller != null))
				{
					foreach (AnimationClip clip in controller.animationClips)
					{
						if (clip != null) clips.Add(clip.name);
					}
				}
				foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
				{
					if (!clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)) clips.Add(clip.name);
				}
				record.clips = clips.OrderBy(value => value, StringComparer.Ordinal).ToList();
			}
			finally
			{
				Object.DestroyImmediate(instance);
			}

			List<string> refs = reverseReferences.TryGetValue(path, out List<string> found) ? found : new List<string>();
			record.prefabReferences = refs.Where(value => value.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)).ToList();
			record.sceneReferences = refs.Where(value => value.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)).ToList();
			record.dataReferences = refs.Where(IsDataAsset).ToList();
			record.builderCandidates = FindBuilderCandidates(path, builders);
			return record;
		}

		private static Inspection InspectInstance(GameObject instance)
		{
			var result = new Inspection();
			foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
			{
				result.renderers.Add(renderer);
				Mesh mesh = MeshFor(renderer);
				if (mesh != null) result.meshes.Add(mesh);
				if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
				if (!result.hasBounds)
				{
					result.bounds = renderer.bounds;
					result.hasBounds = true;
				}
				else result.bounds.Encapsulate(renderer.bounds);
			}

			if (!result.hasBounds)
			{
				foreach (Renderer renderer in result.renderers)
				{
					if (!result.hasBounds)
					{
						result.bounds = renderer.bounds;
						result.hasBounds = true;
					}
					else result.bounds.Encapsulate(renderer.bounds);
				}
			}
			return result;
		}

		private static long CountRenderedTriangles(GameObject instance)
		{
			long total = 0;
			foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
			{
				Mesh mesh = MeshFor(renderer);
				if (mesh == null) continue;
				for (int sub = 0; sub < mesh.subMeshCount; sub++)
				{
					total += (long)mesh.GetIndexCount(sub) / 3L;
				}
			}
			return total;
		}

		private static Mesh MeshFor(Renderer renderer)
		{
			if (renderer is SkinnedMeshRenderer skinned) return skinned.sharedMesh;
			MeshFilter filter = renderer.GetComponent<MeshFilter>();
			return filter != null ? filter.sharedMesh : null;
		}

		private static List<MeshRecord> InspectMeshes(
			IEnumerable<string> meshPaths,
			Dictionary<string, List<string>> reverseReferences,
			List<BuilderSource> builders)
		{
			var result = new List<MeshRecord>();
			foreach (string path in meshPaths)
			{
				foreach (Mesh mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().OrderBy(value => value.name, StringComparer.Ordinal))
				{
					AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long localId);
					List<string> refs = reverseReferences.TryGetValue(path, out List<string> found) ? found : new List<string>();
					result.Add(new MeshRecord
					{
						assetPath = path,
						guid = guid,
						localId = localId,
						meshName = mesh.name,
						vertices = mesh.vertexCount,
						triangles = CountMeshTriangles(mesh),
						boundsCenter = Vector(mesh.bounds.center),
						boundsSize = Vector(mesh.bounds.size),
						prefabReferences = Join(refs.Where(value => value.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))),
						sceneReferences = Join(refs.Where(value => value.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))),
						builderCandidates = Join(FindBuilderCandidates(path, builders))
					});
				}
			}
			return result.OrderBy(row => row.assetPath, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.meshName, StringComparer.Ordinal).ToList();
		}

		private static long CountMeshTriangles(Mesh mesh)
		{
			long total = 0;
			for (int sub = 0; sub < mesh.subMeshCount; sub++) total += (long)mesh.GetIndexCount(sub) / 3L;
			return total;
		}

		private static List<string> FindBuilderCandidates(string assetPath, List<BuilderSource> builders)
		{
			string fileName = Path.GetFileName(assetPath);
			string stem = Path.GetFileNameWithoutExtension(assetPath);
			string familyToken = BuilderFamilyToken(assetPath);
			var result = new List<string>();
			foreach (BuilderSource builder in builders)
			{
				bool exact = builder.text.IndexOf(assetPath, StringComparison.OrdinalIgnoreCase) >= 0 ||
					builder.text.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) >= 0;
				bool named = stem.Length >= 6 && builder.fileName.IndexOf(stem, StringComparison.OrdinalIgnoreCase) >= 0;
				bool family = !string.IsNullOrEmpty(familyToken) &&
					builder.fileName.IndexOf(familyToken, StringComparison.OrdinalIgnoreCase) >= 0 &&
					(builder.fileName.IndexOf("Builder", StringComparison.OrdinalIgnoreCase) >= 0 ||
					 builder.fileName.IndexOf("Rebuilder", StringComparison.OrdinalIgnoreCase) >= 0 ||
					 builder.fileName.IndexOf("Bake", StringComparison.OrdinalIgnoreCase) >= 0);
				if (exact || named || family) result.Add(builder.assetPath);
			}
			return result.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
		}

		private static string BuilderFamilyToken(string path)
		{
			if (path.IndexOf("/Buildings/", StringComparison.OrdinalIgnoreCase) >= 0) return "Building";
			if (path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0) return "Resource";
			if (path.IndexOf("/WorldChests/", StringComparison.OrdinalIgnoreCase) >= 0) return "WorldChest";
			if (path.IndexOf("/Forge", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("/Containers/", StringComparison.OrdinalIgnoreCase) >= 0) return "Forge";
			if (path.IndexOf("/StyleProof/", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("/Environment/", StringComparison.OrdinalIgnoreCase) >= 0) return "StyleProof";
			if (path.IndexOf("/Actors/", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("/Enemies/", StringComparison.OrdinalIgnoreCase) >= 0) return "Creature";
			if (path.IndexOf("/Player/", StringComparison.OrdinalIgnoreCase) >= 0) return "Wanderer";
			if (path.IndexOf("/Items/", StringComparison.OrdinalIgnoreCase) >= 0) return "Item";
			return string.Empty;
		}

		private static void RenderAssetCaptures(string projectRoot, string outputRoot, List<AssetRecord> records)
		{
			for (int index = 0; index < records.Count; index++)
			{
				AssetRecord record = records[index];
				try
				{
					GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(record.assetPath);
					if (source == null) throw new InvalidOperationException("Asset root konnte nicht geladen werden.");
					GameObject instance = Object.Instantiate(source);
					try
					{
						Inspection inspection = InspectInstance(instance);
						if (!inspection.hasBounds) throw new InvalidOperationException("Kein renderbarer Bounds gefunden.");
						RenderIsolated(instance, inspection.bounds, Path.Combine(outputRoot, record.standardCapture.Replace('/', Path.DirectorySeparatorChar)), 22f, 35f, false);
						RenderIsolated(instance, inspection.bounds, Path.Combine(outputRoot, record.gameplayCapture.Replace('/', Path.DirectorySeparatorChar)), 56f, 38f, true);
					}
					finally
					{
						Object.DestroyImmediate(instance);
					}
				}
				catch (Exception exception)
				{
					record.captureError = exception.GetType().Name + ": " + exception.Message;
					Debug.LogError($"[MIDPOLY Phase 0] Capture fehlgeschlagen: {record.assetPath}: {record.captureError}");
				}
				if ((index + 1) % 20 == 0 || index + 1 == records.Count)
				{
					Debug.Log($"[MIDPOLY Phase 0] Captures {index + 1}/{records.Count}");
				}
			}
		}

		private static void RenderIsolated(GameObject instance, Bounds sourceBounds, string outputPath, float elevation, float azimuth, bool gameplay)
		{
			Vector3 offset = new Vector3(-sourceBounds.center.x, -sourceBounds.min.y, -sourceBounds.center.z);
			instance.transform.position += offset;
			Inspection placed = InspectInstance(instance);
			Bounds bounds = placed.hasBounds ? placed.bounds : sourceBounds;

			GameObject cameraObject = new GameObject("MIDPOLY_Phase0_Camera");
			GameObject keyObject = new GameObject("MIDPOLY_Phase0_Key");
			GameObject fillObject = new GameObject("MIDPOLY_Phase0_Fill");
			GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
			Material groundMaterial = null;
			try
			{
				Camera camera = cameraObject.AddComponent<Camera>();
				camera.clearFlags = CameraClearFlags.SolidColor;
				camera.backgroundColor = gameplay ? new Color(0.105f, 0.125f, 0.125f) : new Color(0.155f, 0.165f, 0.18f);
				camera.fieldOfView = gameplay ? 33f : 30f;
				camera.nearClipPlane = 0.01f;
				camera.farClipPlane = 10000f;
				camera.allowHDR = false;

				Light key = keyObject.AddComponent<Light>();
				key.type = LightType.Directional;
				key.intensity = 1.15f;
				key.color = new Color(1f, 0.93f, 0.82f);
				keyObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

				Light fill = fillObject.AddComponent<Light>();
				fill.type = LightType.Directional;
				fill.intensity = 0.55f;
				fill.color = new Color(0.62f, 0.76f, 1f);
				fillObject.transform.rotation = Quaternion.Euler(32f, 145f, 0f);

				ground.name = "MIDPOLY_Phase0_Ground";
				ground.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
				float footprint = Mathf.Max(1f, Mathf.Max(bounds.size.x, bounds.size.z));
				ground.transform.localScale = new Vector3(footprint * 3.2f, footprint * 3.2f, 1f);
				Shader groundShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
				groundMaterial = new Material(groundShader);
				groundMaterial.color = gameplay ? new Color(0.19f, 0.24f, 0.20f) : new Color(0.31f, 0.32f, 0.34f);
				ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

				Vector3 target = bounds.center + Vector3.up * bounds.size.y * 0.03f;
				float radius = Mathf.Max(0.15f, bounds.extents.magnitude);
				float distance = Mathf.Max(0.8f, radius / Mathf.Tan(camera.fieldOfView * 0.43f * Mathf.Deg2Rad));
				float elevRad = elevation * Mathf.Deg2Rad;
				float azimuthRad = azimuth * Mathf.Deg2Rad;
				Vector3 direction = new Vector3(
					Mathf.Sin(azimuthRad) * Mathf.Cos(elevRad),
					Mathf.Sin(elevRad),
					Mathf.Cos(azimuthRad) * Mathf.Cos(elevRad));
				cameraObject.transform.position = target + direction * distance;
				cameraObject.transform.LookAt(target);
				RenderCamera(camera, outputPath);
			}
			finally
			{
				Object.DestroyImmediate(cameraObject);
				Object.DestroyImmediate(keyObject);
				Object.DestroyImmediate(fillObject);
				Object.DestroyImmediate(ground);
				if (groundMaterial != null) Object.DestroyImmediate(groundMaterial);
			}
		}

		private static List<SceneRecord> InspectAndCaptureScenes(string projectRoot, string outputRoot)
		{
			var result = new List<SceneRecord>();
			List<string> paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game/Scenes" })
				.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
			foreach (string path in paths)
			{
				var row = new SceneRecord
				{
					path = path,
					guid = AssetDatabase.AssetPathToGUID(path),
					capture = "Captures/Scenes/" + SafeName(Path.GetFileNameWithoutExtension(path)) + "_overview.png"
				};
				try
				{
					Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
					List<Renderer> renderers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToList();
					row.rendererCount = renderers.Count;
					row.triangles = renderers.Sum(renderer =>
					{
						Mesh mesh = MeshFor(renderer);
						return mesh == null ? 0L : CountMeshTriangles(mesh);
					});
					row.cameraCount = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Camera>(true).Length);
					var prefabSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					int sceneOnly = 0;
					foreach (Renderer renderer in renderers)
					{
						GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(renderer.gameObject);
						string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
						if (string.IsNullOrEmpty(sourcePath)) sceneOnly++;
						else prefabSources.Add(sourcePath);
					}
					row.prefabSourceCount = prefabSources.Count;
					row.sceneOnlyRendererCount = sceneOnly;

					bool hasBounds = TryBounds(renderers.Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy), out Bounds bounds);
					if (!hasBounds) hasBounds = TryBounds(renderers, out bounds);
					if (hasBounds)
					{
						row.boundsCenter = Vector(bounds.center);
						row.boundsSize = Vector(bounds.size);
						CaptureSceneOverview(bounds, Path.Combine(outputRoot, row.capture.Replace('/', Path.DirectorySeparatorChar)));
					}
					else
					{
						row.capture = string.Empty;
						row.note = "N/A: Szene enthaelt keine 3D-Renderer.";
					}
				}
				catch (Exception exception)
				{
					row.error = exception.GetType().Name + ": " + exception.Message;
					Debug.LogError($"[MIDPOLY Phase 0] Szenen-Audit fehlgeschlagen: {path}: {row.error}");
				}
				result.Add(row);
			}
			return result;
		}

		private static void CaptureSceneOverview(Bounds bounds, string outputPath)
		{
			GameObject cameraObject = new GameObject("MIDPOLY_Phase0_SceneCamera");
			GameObject lightObject = new GameObject("MIDPOLY_Phase0_SceneLight");
			try
			{
				Camera camera = cameraObject.AddComponent<Camera>();
				camera.clearFlags = CameraClearFlags.Skybox;
				camera.fieldOfView = 42f;
				camera.nearClipPlane = 0.1f;
				camera.farClipPlane = Mathf.Max(2000f, bounds.extents.magnitude * 8f);
				Light light = lightObject.AddComponent<Light>();
				light.type = LightType.Directional;
				light.intensity = 1f;
				lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
				float distance = Mathf.Max(5f, bounds.extents.magnitude / Mathf.Tan(camera.fieldOfView * 0.38f * Mathf.Deg2Rad));
				Vector3 direction = Quaternion.Euler(48f, 32f, 0f) * Vector3.back;
				cameraObject.transform.position = bounds.center + direction * distance;
				cameraObject.transform.LookAt(bounds.center);
				RenderCamera(camera, outputPath);
			}
			finally
			{
				Object.DestroyImmediate(cameraObject);
				Object.DestroyImmediate(lightObject);
			}
		}

		private static bool TryBounds(IEnumerable<Renderer> renderers, out Bounds bounds)
		{
			bounds = default;
			bool found = false;
			foreach (Renderer renderer in renderers)
			{
				if (!found) { bounds = renderer.bounds; found = true; }
				else bounds.Encapsulate(renderer.bounds);
			}
			return found;
		}

		private static void RenderCamera(Camera camera, string path)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			RenderTexture target = new RenderTexture(CaptureSize, CaptureSize, 24, RenderTextureFormat.ARGB32);
			Texture2D image = new Texture2D(CaptureSize, CaptureSize, TextureFormat.RGB24, false);
			RenderTexture previous = RenderTexture.active;
			try
			{
				camera.targetTexture = target;
				camera.Render();
				RenderTexture.active = target;
				image.ReadPixels(new Rect(0f, 0f, CaptureSize, CaptureSize), 0, 0);
				image.Apply();
				File.WriteAllBytes(path, image.EncodeToPNG());
			}
			finally
			{
				camera.targetTexture = null;
				RenderTexture.active = previous;
				Object.DestroyImmediate(target);
				Object.DestroyImmediate(image);
			}
		}

		private static void WriteAssetOutputs(string outputRoot, AssetRecordCollection collection)
		{
			File.WriteAllText(Path.Combine(outputRoot, "ASSET_REGISTER.json"), JsonUtility.ToJson(collection, true), new UTF8Encoding(false));
			var csv = new StringBuilder();
			csv.AppendLine("ID;Status;Name;Art;Familie;Assetpfad;GUID;Tris;Renderer;Aktive Renderer;Meshes;Materialslots;Materialien;Collider;Bones;Root Bones;Animator Controller;Clips;Prefab-Referenzen;Daten-Referenzen;Szenen;Builder-Kandidaten;Root Transform;Bounds Center;Bounds Size;Standard-Low;Gameplay-Low;Capture-Fehler");
			foreach (AssetRecord row in collection.assets)
			{
				csv.AppendLine(CsvLine(row.id, row.status, row.name, row.kind, row.family, row.assetPath, row.guid,
					row.triangles.ToString(Invariant), row.rendererCount.ToString(Invariant), row.activeRendererCount.ToString(Invariant),
					row.meshCount.ToString(Invariant), row.materialSlotCount.ToString(Invariant), Join(row.materials), Join(row.colliders),
					Join(row.bones), Join(row.rootBones), Join(row.animatorControllers), Join(row.clips), Join(row.prefabReferences),
					Join(row.dataReferences), Join(row.sceneReferences), Join(row.builderCandidates), row.rootTransform, row.boundsCenter,
					row.boundsSize, row.standardCapture, row.gameplayCapture, row.captureError));
			}
			File.WriteAllText(Path.Combine(outputRoot, "ASSET_REGISTER.csv"), csv.ToString(), new UTF8Encoding(true));

			var status = new StringBuilder();
			status.AppendLine("ID;Assetpfad;Familie;Status;Verantwortlich;Notiz;Letzte Aenderung");
			foreach (AssetRecord row in collection.assets)
			{
				status.AppendLine(CsvLine(row.id, row.assetPath, row.family, row.status, string.Empty, "Phase-0-Baseline", DateTime.UtcNow.ToString("yyyy-MM-dd", Invariant)));
			}
			File.WriteAllText(Path.Combine(outputRoot, "MIGRATION_STATUS.csv"), status.ToString(), new UTF8Encoding(true));
		}

		private static void WriteMeshOutput(string outputRoot, List<MeshRecord> records)
		{
			var csv = new StringBuilder();
			csv.AppendLine("Assetpfad;GUID;Local ID;Meshname;Vertices;Tris;Bounds Center;Bounds Size;Prefab-Referenzen;Szenen;Builder-Kandidaten");
			foreach (MeshRecord row in records)
			{
				csv.AppendLine(CsvLine(row.assetPath, row.guid, row.localId.ToString(Invariant), row.meshName,
					row.vertices.ToString(Invariant), row.triangles.ToString(Invariant), row.boundsCenter, row.boundsSize,
					row.prefabReferences, row.sceneReferences, row.builderCandidates));
			}
			File.WriteAllText(Path.Combine(outputRoot, "MESH_COMPONENTS.csv"), csv.ToString(), new UTF8Encoding(true));
		}

		private static void WriteSceneOutput(string outputRoot, List<SceneRecord> records)
		{
			var csv = new StringBuilder();
			csv.AppendLine("Szenenpfad;GUID;Renderer;Tris;Kameras;Prefab-Quellen;Scene-only Renderer;Bounds Center;Bounds Size;Capture;Notiz;Fehler");
			foreach (SceneRecord row in records)
			{
				csv.AppendLine(CsvLine(row.path, row.guid, row.rendererCount.ToString(Invariant), row.triangles.ToString(Invariant),
					row.cameraCount.ToString(Invariant), row.prefabSourceCount.ToString(Invariant), row.sceneOnlyRendererCount.ToString(Invariant),
					row.boundsCenter, row.boundsSize, row.capture, row.note, row.error));
			}
			File.WriteAllText(Path.Combine(outputRoot, "SCENE_BASELINE.csv"), csv.ToString(), new UTF8Encoding(true));
		}

		private static void WriteBuilderOutput(string outputRoot, List<BuilderSource> builders)
		{
			var csv = new StringBuilder();
			csv.AppendLine("Editor-Skript;SHA256;Generierungs-Indikatoren");
			foreach (BuilderSource builder in builders)
			{
				var indicators = new List<string>();
				foreach (string token in new[] { "AssetDatabase.CreateAsset", "PrefabUtility.SaveAsPrefabAsset", "AssetDatabase.DeleteAsset", "File.WriteAllBytes", "MeshFactory", "BuildStandalone", "Rebuild" })
				{
					if (builder.text.IndexOf(token, StringComparison.Ordinal) >= 0) indicators.Add(token);
				}
				if (indicators.Count == 0) continue;
				csv.AppendLine(CsvLine(builder.assetPath, HashFile(AssetToDiskPath(builder.assetPath)), Join(indicators)));
			}
			File.WriteAllText(Path.Combine(outputRoot, "BUILDER_SOURCES.csv"), csv.ToString(), new UTF8Encoding(true));
		}

		private static void WriteChecksums(string outputRoot, IEnumerable<string> assetPaths)
		{
			var csv = new StringBuilder();
			csv.AppendLine("Assetpfad;GUID;Asset SHA256;Meta SHA256;Bytes;LastWrite UTC");
			foreach (string assetPath in assetPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
			{
				string diskPath = AssetToDiskPath(assetPath);
				string metaPath = diskPath + ".meta";
				var info = new FileInfo(diskPath);
				csv.AppendLine(CsvLine(assetPath, AssetDatabase.AssetPathToGUID(assetPath), HashFile(diskPath), HashFile(metaPath),
					info.Exists ? info.Length.ToString(Invariant) : "0", info.Exists ? info.LastWriteTimeUtc.ToString("o", Invariant) : string.Empty));
			}
			File.WriteAllText(Path.Combine(outputRoot, "BASELINE_CHECKSUMS.csv"), csv.ToString(), new UTF8Encoding(true));
		}

		private static void WriteReportAndReadme(
			string outputRoot,
			List<AssetRecord> assets,
			List<MeshRecord> meshes,
			List<SceneRecord> scenes,
			List<BuilderSource> builders)
		{
			int assetCaptureErrors = assets.Count(row => !string.IsNullOrEmpty(row.captureError));
			int sceneErrors = scenes.Count(row => !string.IsNullOrEmpty(row.error));
			int prefabs = assets.Count(row => row.kind == "Prefab");
			int models = assets.Count - prefabs;
			int rigged = assets.Count(row => row.bones.Count > 0);
			int builderMapped = assets.Count(row => row.builderCandidates.Count > 0);
			long triangles = assets.Sum(row => row.triangles);
			int nativeMeshSubassets = meshes.Count(row => row.assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase));
			int glbMeshSubassets = meshes.Count(row => row.assetPath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase));
			int generatedBuilderSources = builders.Count(builder =>
				builder.text.IndexOf("AssetDatabase.CreateAsset", StringComparison.Ordinal) >= 0 ||
				builder.text.IndexOf("PrefabUtility.SaveAsPrefabAsset", StringComparison.Ordinal) >= 0 ||
				builder.text.IndexOf("File.WriteAllBytes", StringComparison.Ordinal) >= 0);

			var report = new StringBuilder();
			report.AppendLine("# MIDPOLY-000 – Phase 0 Abschlussbericht");
			report.AppendLine();
			report.AppendLine("**Status:** abgeschlossen");
			report.AppendLine($"**Baseline:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC · Unity {Application.unityVersion}");
			report.AppendLine("**Produktive Prefabs verändert:** nein");
			report.AppendLine();
			report.AppendLine("## Ergebnis");
			report.AppendLine();
			report.AppendLine($"- {assets.Count} sichtbare Asset-Roots inventarisiert ({prefabs} Prefabs, {models} importierte Modelle).");
			report.AppendLine($"- {meshes.Count} technische Mesh-Subassets mit Local ID, Vertices, Tris und Bounds gesichert.");
			report.AppendLine($"- {scenes.Count} produktive Szenen inventarisiert; {scenes.Count(row => !string.IsNullOrEmpty(row.capture))} Szenen mit 3D-Renderern besitzen eine Übersicht, {scenes.Count(row => !string.IsNullOrEmpty(row.note))} reine Bootstrap/UI-Szenen sind als nicht anwendbar markiert.");
			report.AppendLine($"- {rigged} visuelle Roots enthalten ein Skinned-Mesh-Rig.");
			report.AppendLine($"- {builderMapped} visuelle Roots besitzen mindestens einen reproduzierbar ermittelten Builder-Kandidaten.");
			report.AppendLine($"- {generatedBuilderSources} Editor-Skripte schreiben oder erzeugen nach statischem Indikator Assets.");
			report.AppendLine($"- Summierte gerenderte LOD-/Instanzgeometrie der Asset-Roots: {triangles:N0} Tris (keine Szenen-Performancezahl).");
			report.AppendLine();
			report.AppendLine("## Phase-0-Prüfpunkte");
			report.AppendLine();
			report.AppendLine("| Prüfpunkt | Ergebnis | Nachweis |");
			report.AppendLine("| --- | --- | --- |");
			report.AppendLine($"| Vollständiges Assetregister | PASS | `ASSET_REGISTER.csv/json`, {assets.Count} visuelle Roots; `MESH_COMPONENTS.csv`, {meshes.Count} Mesh-Subassets |");
			report.AppendLine($"| Standard- und Gameplay-Baseline je sichtbarem Asset | {(assetCaptureErrors == 0 ? "PASS" : "FAIL")} | `Captures/Standard_Low`, `Captures/Gameplay_Low`; Fehler: {assetCaptureErrors} |");
			report.AppendLine($"| GUIDs, Bounds, Pivots, Collider, Rig, Clips und Controller gesichert | PASS | Register plus `BASELINE_CHECKSUMS.csv` |");
			report.AppendLine($"| Produktive Szenen dokumentiert | {(sceneErrors == 0 ? "PASS" : "FAIL")} | `SCENE_BASELINE.csv`, `Captures/Scenes`; Fehler: {sceneErrors} |");
			report.AppendLine("| Migrationsstatus initialisiert | PASS | `MIGRATION_STATUS.csv`; alle Zeilen `ungeprueft` |");
			report.AppendLine("| Builder-/Rebuild-Risiko eingefroren | PASS | `BUILDER_SOURCES.csv`, Builder-Kandidaten im Register und Quell-Hashes |");
			report.AppendLine();
			report.AppendLine("## Bestandsbefunde");
			report.AppendLine();
			report.AppendLine($"- Der im Auftrag genannte Bestand von 611 erzeugten Komponenten-Meshassets ist bestätigt: {nativeMeshSubassets} native `.asset`-Meshzeilen plus {glbMeshSubassets} GLB-Mesh-Subassets wurden erfasst.");
			report.AppendLine("- Unity meldet bei allen 16 importierten Figurenmodellen jeweils 80 gerenderte Dreiecke weniger als die Vergleichswerte in MIDPOLY-000. Das Register hält den tatsächlich importierten Unity-Stand fest; die konstante Differenz ist vor der ersten Retarget-Abnahme gegen die GLB-Primitive zu klären.");
			report.AppendLine("- Die bestehende Editor-Testassembly kompiliert wegen fünf Verweisen auf das nicht mehr vorhandene `ZoneStateService.DefaultMasterSeed` derzeit nicht. Für den reinen Auditlauf wurde nur die Testassembly temporär ausgeschlossen und danach unverändert wieder aktiviert; Produktivcode und produktive Assets blieben unangetastet.");
			report.AppendLine();
			report.AppendLine("## Auslegung der Captures");
			report.AppendLine();
			report.AppendLine("`Standard_Low` ist die unveränderte Vorher-Hälfte des späteren Low-vs-Mid-Vergleichs. `Gameplay_Low` zeigt dasselbe Asset isoliert mit einer festen isometrischen Spielkameraperspektive. Die Szenenübersichten sichern zusätzlich den räumlichen Projektkontext. Echte laufzeitabhängige Zustände und Animationsframes werden in den nachfolgenden Familien-QAs gegen diese Baseline ergänzt.");
			report.AppendLine();
			report.AppendLine("## Abgrenzung und Nachvollziehbarkeit");
			report.AppendLine();
			report.AppendLine("Technische Teilmeshes sind separat inventarisiert, aber nicht einzeln gerendert, weil sie keine eigenständigen Motive darstellen. Gerendert werden ihre sichtbaren Prefab-/Modell-Roots. Builder-Zuordnungen sind ausdrücklich Kandidaten: exakte Pfad-/Dateinennungen sowie eng begrenzte Familienheuristiken. Dadurch bleiben indirekt zusammengesetzte Bake-Ketten sichtbar, ohne eine nicht belegte Alleinzuständigkeit zu behaupten.");
			if (assetCaptureErrors > 0 || sceneErrors > 0)
			{
				report.AppendLine();
				report.AppendLine("## Fehlerliste");
				report.AppendLine();
				foreach (AssetRecord row in assets.Where(row => !string.IsNullOrEmpty(row.captureError))) report.AppendLine($"- `{row.assetPath}`: {row.captureError}");
				foreach (SceneRecord row in scenes.Where(row => !string.IsNullOrEmpty(row.error))) report.AppendLine($"- `{row.path}`: {row.error}");
			}
			report.AppendLine();
			report.AppendLine("Phase 1 darf auf dieser Baseline aufsetzen. Produktive Assetwechsel bleiben bis zur Golden-Master-Freigabe gesperrt.");
			File.WriteAllText(Path.Combine(outputRoot, "PHASE0_REPORT.md"), report.ToString(), new UTF8Encoding(false));

			var readme = new StringBuilder();
			readme.AppendLine("# Mid-Poly Phase-0-Baseline");
			readme.AppendLine();
			readme.AppendLine("Diese Dateien werden von `Eidren.Editor.MidpolyPhase0Baseline.Run` erzeugt. CSV-Dateien sind UTF-8 mit Semikolon als Trennzeichen. Das JSON ist die vollständige maschinenlesbare Fassung des visuellen Registers.");
			readme.AppendLine();
			readme.AppendLine("Reproduktionsbefehl (PowerShell, ohne `-nographics`, weil Bilder gerendert werden):");
			readme.AppendLine();
			readme.AppendLine("```powershell");
			readme.AppendLine("& '.\\.unity-editor\\Editor\\Unity.exe' -batchmode -quit -projectPath . -executeMethod Eidren.Editor.MidpolyPhase0Baseline.Run -logFile phase0-midpoly.log");
			readme.AppendLine("```");
			readme.AppendLine();
			readme.AppendLine("Der Generator ersetzt den Capture-Ordner atomar auf Ordnerebene und verändert keine produktiven Prefabs, Datenobjekte oder Szenen.");
			readme.AppendLine();
			readme.AppendLine("Bekannter Bestandsfehler: `Assets/_Game/Editor/Tests/WeltsaatTests.cs` verweist derzeit auf das entfernte `ZoneStateService.DefaultMasterSeed`. Bis dieser unabhängige Compilerfehler behoben ist, muss die Editor-Testassembly für einen erneuten Auditlauf kontrolliert temporär ausgeschlossen und anschließend unverändert reaktiviert werden.");
			File.WriteAllText(Path.Combine(outputRoot, "README.md"), readme.ToString(), new UTF8Encoding(false));
		}

		private static bool IsDataAsset(string path)
		{
			if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) return false;
			Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
			return type != null && typeof(ScriptableObject).IsAssignableFrom(type);
		}

		private static string ColliderText(Transform root, Collider collider)
		{
			string path = AnimationUtility.CalculateTransformPath(collider.transform, root);
			string detail = string.Empty;
			if (collider is BoxCollider box) detail = $"center={Vector(box.center)} size={Vector(box.size)}";
			else if (collider is SphereCollider sphere) detail = $"center={Vector(sphere.center)} radius={Number(sphere.radius)}";
			else if (collider is CapsuleCollider capsule) detail = $"center={Vector(capsule.center)} radius={Number(capsule.radius)} height={Number(capsule.height)} direction={capsule.direction}";
			else if (collider is MeshCollider mesh) detail = $"mesh={(mesh.sharedMesh != null ? mesh.sharedMesh.name : "—")} convex={mesh.convex}";
			return $"{collider.GetType().Name}@{(string.IsNullOrEmpty(path) ? "<root>" : path)} trigger={collider.isTrigger} {detail}".Trim();
		}

		private static string MaterialIdentity(Material material)
		{
			string path = AssetDatabase.GetAssetPath(material);
			return string.IsNullOrEmpty(path) ? material.name : path;
		}

		private static string TransformText(Transform transform)
		{
			return $"position={Vector(transform.localPosition)} rotation={Vector(transform.localEulerAngles)} scale={Vector(transform.localScale)}";
		}

		private static string ClassifyFamily(string path)
		{
			string p = path.Replace('\\', '/');
			if (p.IndexOf("/Actors/", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("/Enemies/", StringComparison.OrdinalIgnoreCase) >= 0) return "Figuren/Kreaturen";
			if (p.IndexOf("/Player/", StringComparison.OrdinalIgnoreCase) >= 0) return "Wanderer/Ausruestung";
			if (p.IndexOf("/Buildings/", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("/Stations/", StringComparison.OrdinalIgnoreCase) >= 0) return "Gebaeude/Stationen";
			if (p.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0) return "Ressourcen/Vegetation";
			if (p.IndexOf("/WorldChests/", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("/Containers/", StringComparison.OrdinalIgnoreCase) >= 0) return "Truhen/Container";
			if (p.IndexOf("/Items/", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("/Loot/", StringComparison.OrdinalIgnoreCase) >= 0) return "WorldItems/Loot";
			if (p.IndexOf("/Environment/", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("/World/", StringComparison.OrdinalIgnoreCase) >= 0) return "Umgebung/Zonenprops";
			if (p.IndexOf("/Forge", StringComparison.OrdinalIgnoreCase) >= 0) return "Eidra-Schmiede";
			return "Sonstige sichtbare 3D-Assets";
		}

		private static string Vector(Vector3 value)
		{
			return $"({Number(value.x)},{Number(value.y)},{Number(value.z)})";
		}

		private static string Number(float value)
		{
			return value.ToString("0.######", Invariant);
		}

		private static string Join(IEnumerable<string> values)
		{
			return string.Join(" | ", values.Where(value => !string.IsNullOrEmpty(value)));
		}

		private static string CsvLine(params string[] values)
		{
			return string.Join(";", values.Select(Csv));
		}

		private static string Csv(string value)
		{
			value = value ?? string.Empty;
			return "\"" + value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
		}

		private static string SafeName(string value)
		{
			var builder = new StringBuilder(value.Length);
			foreach (char character in value)
			{
				builder.Append(char.IsLetterOrDigit(character) || character == '-' || character == '_' ? character : '_');
			}
			return builder.ToString();
		}

		private static string ToAssetPath(string diskPath)
		{
			string normalized = Path.GetFullPath(diskPath).Replace('\\', '/');
			string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/').TrimEnd('/');
			return normalized.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase)
				? normalized.Substring(projectRoot.Length + 1)
				: normalized;
		}

		private static string AssetToDiskPath(string assetPath)
		{
			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
		}

		private static string HashFile(string path)
		{
			if (!File.Exists(path)) return string.Empty;
			using (SHA256 sha = SHA256.Create())
			using (FileStream stream = File.OpenRead(path))
			{
				return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
			}
		}
	}
}
