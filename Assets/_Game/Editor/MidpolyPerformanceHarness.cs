using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
	/// <summary>
	/// Creates a deliberately dense, non-persistent render set in the active
	/// scene. It is used with the Unity profiler MCP and is never saved.
	/// </summary>
	internal static class MidpolyPerformanceHarness
	{
		private const string RootName = "__MIDPOLY_WORST_CASE";
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase6/WORST_CASE_HARNESS.json";

		[MenuItem("Eidren/Mid-Poly/Phase 6/Worst-Case Profilerbesatz aufbauen")]
		public static void Build()
		{
			if (EditorApplication.isPlaying) throw new InvalidOperationException("Profilerbesatz muss im EditMode aufgebaut werden.");
			RemoveExisting();
			GameObject root = new GameObject(RootName);
			var paths = new List<SpawnSpec>();
			AddDirect(paths, "Actors", "Assets/_Game/Prefabs/Actors/3D", 1, path => !path.Contains("/Fallback/") && !path.Contains("/Review/"));
			AddDirect(paths, "Environment", "Assets/_Game/Prefabs/Environment/StyleProof", 4, _ => true);
			AddDirect(paths, "Resources", "Assets/_Game/Prefabs/Resources/Visuals", 3, path => path.EndsWith("_Active.prefab", StringComparison.Ordinal));
			AddDirect(paths, "Buildings", "Assets/_Game/Prefabs/Buildings/Level01", 1, _ => true);
			AddDirect(paths, "Stations", "Assets/_Game/Prefabs/Stations", 1, path => !path.Contains("/Fallback/"));
			AddDirect(paths, "WorldChests", "Assets/_Game/Prefabs/Loot/WorldChests", 1, path => !path.Contains("/Fallback/"));
			AddDirect(paths, "ForgeContainers", "Assets/_Game/Prefabs/Containers/Forge", 1, path => !path.Contains("/Fallback/"));
			AddDirect(paths, "WorldItems", "Assets/_Game/Prefabs/Items", 2, path => !path.Contains("/Fallback/"));

			int sequence = 0;
			var familyCounts = new Dictionary<string, int>(StringComparer.Ordinal);
			long triangles = 0;
			int rendererCount = 0;
			int skinnedCount = 0;
			int boneCount = 0;
			var materials = new HashSet<Material>();
			foreach (SpawnSpec spawn in paths)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spawn.Path);
				if (prefab == null) continue;
				for (int repeat = 0; repeat < spawn.Repeat; repeat++)
				{
					GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
					instance.name = spawn.Family + "_" + prefab.name + "_" + repeat;
					instance.transform.SetParent(root.transform, false);
					int column = sequence % 12;
					int row = sequence / 12;
					instance.transform.localPosition = new Vector3((column - 5.5f) * 6.5f, 0f, row * 6.5f);
					instance.transform.localRotation = Quaternion.Euler(0f, (sequence * 37) % 360, 0f);
					sequence++;
					familyCounts[spawn.Family] = familyCounts.TryGetValue(spawn.Family, out int count) ? count + 1 : 1;

					LODGroup[] groups = instance.GetComponentsInChildren<LODGroup>(true);
					foreach (LODGroup group in groups) group.ForceLOD(0);
					Renderer[] rendered = groups.Length == 0
						? instance.GetComponentsInChildren<Renderer>(true)
						: groups.SelectMany(group => group.GetLODs().Length == 0 ? Array.Empty<Renderer>() : group.GetLODs()[0].renderers)
							.Concat(instance.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.GetComponentInParent<LODGroup>() == null))
							.Where(renderer => renderer != null).Distinct().ToArray();
					rendererCount += rendered.Length;
					foreach (Renderer renderer in rendered)
					{
						foreach (Material material in renderer.sharedMaterials.Where(material => material != null)) materials.Add(material);
						Mesh mesh = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
						if (mesh != null) for (int sub = 0; sub < mesh.subMeshCount; sub++) triangles += (long)mesh.GetIndexCount(sub) / 3L;
						if (renderer is SkinnedMeshRenderer skinned)
						{
							skinnedCount++;
							boneCount += skinned.bones == null ? 0 : skinned.bones.Length;
						}
					}
				}
			}

			GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
			ground.name = "ProfilerGround";
			ground.transform.SetParent(root.transform, false);
			ground.transform.localPosition = new Vector3(0f, -0.02f, Mathf.Max(0f, (sequence / 12) * 3.25f));
			ground.transform.localScale = new Vector3(12f, 1f, 12f);
			Object.DestroyImmediate(ground.GetComponent<Collider>());

			GameObject lightObject = new GameObject("ProfilerDirectionalLight");
			lightObject.transform.SetParent(root.transform, false);
			lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
			Light light = lightObject.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1.15f;
			light.shadows = LightShadows.Soft;

			GameObject cameraObject = new GameObject("ProfilerCamera");
			cameraObject.transform.SetParent(root.transform, false);
			Camera camera = cameraObject.AddComponent<Camera>();
			camera.clearFlags = CameraClearFlags.Skybox;
			camera.fieldOfView = 48f;
			camera.nearClipPlane = 0.1f;
			camera.farClipPlane = 350f;
			float centerZ = Mathf.Max(0f, (sequence / 12) * 3.25f);
			cameraObject.transform.position = new Vector3(0f, 72f, centerZ - 86f);
			cameraObject.transform.LookAt(new Vector3(0f, 2.2f, centerZ));

			HarnessReport report = new HarnessReport
			{
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
				instances = sequence,
				renderersAtForcedLod0 = rendererCount,
				trianglesAtForcedLod0 = triangles,
				uniqueMaterials = materials.Count,
				skinnedMeshes = skinnedCount,
				boneReferences = boneCount,
				families = familyCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new FamilyCount { family = pair.Key, instances = pair.Value }).ToList()
			};
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
			Debug.Log(string.Format("[MIDPOLY-000] Worst-Case-Besatz: {0} Instanzen, {1:N0} LOD0-Tris, {2} Renderer, {3} Materialien.", sequence, triangles, rendererCount, materials.Count));
		}

		[MenuItem("Eidren/Mid-Poly/Phase 6/Worst-Case Profilerbesatz entfernen")]
		public static void Cleanup()
		{
			string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
			RemoveExisting();
			if (!string.IsNullOrEmpty(scenePath)) EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
		}

		private static void RemoveExisting()
		{
			GameObject existing = GameObject.Find(RootName);
			if (existing != null) Object.DestroyImmediate(existing);
		}

		private static void AddDirect(List<SpawnSpec> result, string family, string root, int repeat, Func<string, bool> predicate)
		{
			foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { root }).Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => string.Equals(Path.GetDirectoryName(path)?.Replace('\\', '/'), root, StringComparison.Ordinal))
				.Where(predicate).OrderBy(path => path, StringComparer.Ordinal))
			{
				result.Add(new SpawnSpec(family, path, repeat));
			}
		}

		private readonly struct SpawnSpec
		{
			public readonly string Family;
			public readonly string Path;
			public readonly int Repeat;
			public SpawnSpec(string family, string path, int repeat) { Family = family; Path = path; Repeat = repeat; }
		}

		[Serializable]
		private sealed class HarnessReport
		{
			public string generatedUtc;
			public string scene;
			public int instances;
			public int renderersAtForcedLod0;
			public long trianglesAtForcedLod0;
			public int uniqueMaterials;
			public int skinnedMeshes;
			public int boneReferences;
			public List<FamilyCount> families;
		}

		[Serializable]
		private sealed class FamilyCount
		{
			public string family;
			public int instances;
		}
	}
}
