using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
	/// <summary>
	/// Promotes the 16 StyleProof environment props to the approved Phase-5
	/// Blender/GLB production kit while preserving prefab GUIDs and root colliders.
	/// </summary>
	internal static class MidpolyEnvironmentMigration
	{
		private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
		private const string RuntimeRoot = "Assets/_Game/Art/MidPoly/Environment/Phase5/Runtime";
		private const string MaterialRoot = "Assets/_Game/Art/MidPoly/Environment/Phase5/Materials";
		private const string SourceBlend = "Assets/_Game/Art/MidPoly/Environment/Phase5/Source~/PRP_EnvironmentKit_Mid_Phase5.blend";
		private const string PrefabRoot = "Assets/_Game/Prefabs/Environment/StyleProof";
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase5/ENVIRONMENT_KIT_TECHNICAL_REPORT.json";

		private static readonly EnvironmentSpec[] Specs =
		{
			new EnvironmentSpec("SP_Tree_A", 3.570f, 7.500f, 2.736f, 5404, 2800, 1124),
			new EnvironmentSpec("SP_Tree_B", 4.024f, 6.000f, 3.096f, 5404, 2800, 1124),
			new EnvironmentSpec("SP_Tree_C", 2.715f, 4.000f, 2.028f, 4124, 2136, 856),
			new EnvironmentSpec("SP_Plant_Bush", 1.501f, 0.900f, 1.176f, 3840, 1992, 804),
			new EnvironmentSpec("SP_Plant_Fern", 1.305f, 0.700f, 1.037f, 504, 252, 98),
			new EnvironmentSpec("SP_Plant_Flowers", 0.493f, 0.400f, 0.459f, 3780, 1960, 784),
			new EnvironmentSpec("SP_GroundCover_Grass", 1.188f, 0.150f, 1.200f, 640, 320, 120, false),
			new EnvironmentSpec("SP_GroundCover_Moss", 1.200f, 0.120f, 1.137f, 1920, 996, 396, false),
			new EnvironmentSpec("SP_Accent_GlowMushrooms", 0.540f, 0.350f, 0.484f, 3000, 1548, 612),
			new EnvironmentSpec("SP_Rock_Small", 1.826f, 0.503f, 1.684f, 2560, 1328, 536),
			new EnvironmentSpec("SP_Rock_Medium", 2.506f, 1.106f, 1.819f, 3840, 1992, 804),
			new EnvironmentSpec("SP_Rock_Large", 2.646f, 2.403f, 2.064f, 5120, 2656, 1072),
			new EnvironmentSpec("SP_RuinWall_A", 2.772f, 2.600f, 1.506f, 3240, 1680, 660),
			new EnvironmentSpec("SP_RuinWall_B", 2.168f, 2.600f, 1.240f, 2592, 1344, 528),
			new EnvironmentSpec("SP_RuinMonument", 1.795f, 5.000f, 1.436f, 4208, 2182, 858),
			new EnvironmentSpec("SP_EidrenRune", 0.721f, 0.800f, 0.440f, 1240, 644, 256)
		};

		[MenuItem("Eidren/Mid-Poly/Phase 5/Umgebungskit integrieren und pruefen")]
		public static void IntegrateApprovedEnvironment()
		{
			if (!IsEnabled())
			{
				throw new InvalidOperationException("Phase-5-Umgebungskit ist in " + RegistryPath + " nicht freigegeben.");
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			Dictionary<string, string> guids = Specs.ToDictionary(spec => spec.Name, spec => AssetDatabase.AssetPathToGUID(PrefabPath(spec)), StringComparer.Ordinal);
			Dictionary<string, string> colliders = Specs.ToDictionary(spec => spec.Name, spec => ColliderSignature(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(spec))), StringComparer.Ordinal);
			BuildApprovedVisuals();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			MigrationReport report = Validate(guids, colliders);
			WriteReport(report);
			if (!report.pass)
			{
				throw new InvalidOperationException("Phase-5-Umgebungskit ist nicht integrationsfaehig. Siehe " + ReportPath + ".");
			}
			Debug.Log("[MIDPOLY-000] Phase 5 Umgebungskit integriert: 16 Props, Blender-Quellen, drei LODs, GUIDs und Collider PASS.");
		}

		internal static bool TryBuildApprovedVisuals()
		{
			if (!IsEnabled() || !File.Exists(SourceBlend) || !Specs.All(HasAllRuntimeModels))
			{
				return false;
			}
			BuildApprovedVisuals();
			return true;
		}

		private static void BuildApprovedVisuals()
		{
			EnsureFolder("Assets/_Game/Art/MidPoly/Environment/Phase5", "Materials");
			foreach (EnvironmentSpec spec in Specs)
			{
				string path = PrefabPath(spec);
				GameObject root = PrefabUtility.LoadPrefabContents(path);
				try
				{
					for (int child = root.transform.childCount - 1; child >= 0; child--)
					{
						Object.DestroyImmediate(root.transform.GetChild(child).gameObject);
					}
					foreach (LODGroup oldGroup in root.GetComponents<LODGroup>())
					{
						Object.DestroyImmediate(oldGroup);
					}

					GameObject lod0 = InstantiateModel(ModelPath(spec, 0), root.transform, "LOD0");
					GameObject lod1 = InstantiateModel(ModelPath(spec, 1), root.transform, "LOD1");
					GameObject lod2 = InstantiateModel(ModelPath(spec, 2), root.transform, "LOD2");
					Normalize(lod0, spec);
					Normalize(lod1, spec);
					Normalize(lod2, spec);
					AssignSharedMaterials(lod0);
					AssignSharedMaterials(lod1);
					AssignSharedMaterials(lod2);

					LODGroup group = root.AddComponent<LODGroup>();
					group.fadeMode = LODFadeMode.CrossFade;
					group.animateCrossFading = true;
					group.SetLODs(new[]
					{
						new LOD(0.55f, lod0.GetComponentsInChildren<Renderer>(true)),
						new LOD(0.24f, lod1.GetComponentsInChildren<Renderer>(true)),
						new LOD(0.06f, lod2.GetComponentsInChildren<Renderer>(true))
					});
					group.RecalculateBounds();
					// F34-001/F34-002: Schwellen fuer die orthografische Spielkamera statt Perspektiv-Werte.
					MidpolyLodThresholds.ApplyOrthographicThresholds(group);
					if (spec.ContactShadow)
					{
						WorldContactShadowBuilder.Attach(root);
					}

					PrefabUtility.SaveAsPrefabAsset(root, path);
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				AssetDatabase.SetLabels(prefab, new[] { "MIDPOLY-000", "MidPoly", "Approved", "Phase5Environment" });
			}
		}

		private static GameObject InstantiateModel(string path, Transform parent, string name)
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (model == null)
			{
				throw new FileNotFoundException("Importiertes Mid-Poly-Modell fehlt: " + path);
			}
			GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
			instance.name = name;
			instance.transform.localPosition = Vector3.zero;
			instance.transform.localRotation = Quaternion.identity;
			instance.transform.localScale = Vector3.one;
			return instance;
		}

		private static void Normalize(GameObject model, EnvironmentSpec spec)
		{
			Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
			if (renderers.Length == 0)
			{
				throw new InvalidDataException(model.name + " besitzt keinen Renderer.");
			}
			Bounds bounds = CombinedBounds(renderers);
			if (bounds.size.y <= 0.0001f)
			{
				throw new InvalidDataException(model.name + " besitzt eine ungueltige Hoehe.");
			}
			// glTF preserves Blender's non-uniform root scaling around rotated
			// children differently than Blender's evaluated world bounds. Normalize
			// all three Unity axes after import so the approved table dimensions are
			// authoritative for every renderer hierarchy and every LOD.
			Vector3 scale = model.transform.localScale;
			scale.x *= spec.Width / bounds.size.x;
			scale.y *= spec.Height / bounds.size.y;
			scale.z *= spec.Depth / bounds.size.z;
			model.transform.localScale = scale;
			bounds = CombinedBounds(model.GetComponentsInChildren<Renderer>(true));
			model.transform.position += Vector3.up * -bounds.min.y;
		}

		private static void AssignSharedMaterials(GameObject model)
		{
			foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
			{
				renderer.sharedMaterials = renderer.sharedMaterials.Select(EnsureSharedMaterial).ToArray();
			}
		}

		private static Material EnsureSharedMaterial(Material source)
		{
			if (source == null) return null;
			string cleanName = source.name.Replace(" (Instance)", string.Empty).Replace('/', '_');
			string path = MaterialRoot + "/" + cleanName + ".mat";
			Material shared = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (shared == null)
			{
				shared = new Material(source) { name = cleanName };
				shared.enableInstancing = true;
				AssetDatabase.CreateAsset(shared, path);
			}
			else
			{
				shared.enableInstancing = true;
				EditorUtility.SetDirty(shared);
			}
			return shared;
		}

		private static MigrationReport Validate(IReadOnlyDictionary<string, string> oldGuids, IReadOnlyDictionary<string, string> oldColliders)
		{
			MigrationReport report = new MigrationReport
			{
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				sourceBlend = SourceBlend,
				assets = new List<AssetResult>()
			};
			foreach (EnvironmentSpec spec in Specs)
			{
				AssetResult result = new AssetResult { asset = spec.Name, issues = new List<string>() };
				string path = PrefabPath(spec);
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab == null)
				{
					result.issues.Add("Prefab fehlt: " + path);
					report.assets.Add(result);
					continue;
				}

				result.prefabGuidPreserved = string.Equals(oldGuids[spec.Name], AssetDatabase.AssetPathToGUID(path), StringComparison.Ordinal);
				result.colliderPreserved = string.Equals(oldColliders[spec.Name], ColliderSignature(prefab), StringComparison.Ordinal);
				if (!result.prefabGuidPreserved) result.issues.Add("Prefab-GUID hat sich geaendert.");
				if (!result.colliderPreserved) result.issues.Add("Root-Collidervertrag hat sich geaendert.");
				if (prefab.GetComponentsInChildren<SpriteRenderer>(true).Length != 0) result.issues.Add("SpriteRenderer im 3D-Produktionsprefab gefunden.");
				LOD[] lods = prefab.GetComponent<LODGroup>()?.GetLODs();
				if (lods == null || lods.Length != 3) result.issues.Add("LODGroup besitzt nicht drei Stufen.");

				Renderer[] visualRenderers = prefab.GetComponentsInChildren<Renderer>(true)
					.Where(renderer => renderer.gameObject.name != WorldContactShadowBuilder.ChildName)
					.ToArray();
				if (visualRenderers.Length == 0)
				{
					result.issues.Add("Kein 3D-Renderer vorhanden.");
				}
				else
				{
					Bounds bounds = CombinedBounds(visualRenderers.Where(renderer => renderer.transform.IsChildOf(prefab.transform.Find("LOD0"))).ToArray());
					result.size = new[] { bounds.size.x, bounds.size.y, bounds.size.z };
					if (Mathf.Abs(bounds.size.x - spec.Width) > 0.03f || Mathf.Abs(bounds.size.y - spec.Height) > 0.015f || Mathf.Abs(bounds.size.z - spec.Depth) > 0.03f)
					{
						result.issues.Add(string.Format("LOD0-Abmessung {0:F3}x{1:F3}x{2:F3} statt {3:F3}x{4:F3}x{5:F3}.", bounds.size.x, bounds.size.y, bounds.size.z, spec.Width, spec.Height, spec.Depth));
					}
				}

				if (visualRenderers.SelectMany(renderer => renderer.sharedMaterials).Any(material => material == null))
				{
					result.issues.Add("Fehlendes Material gefunden.");
				}
				result.lod0Triangles = TriangleCount(prefab.transform.Find("LOD0"));
				result.lod1Triangles = TriangleCount(prefab.transform.Find("LOD1"));
				result.lod2Triangles = TriangleCount(prefab.transform.Find("LOD2"));
				if (result.lod0Triangles != spec.Lod0 || result.lod1Triangles != spec.Lod1 || result.lod2Triangles != spec.Lod2)
				{
					result.issues.Add("LOD-Dreieckszahlen weichen vom Produktionsmanifest ab.");
				}
				result.pass = result.issues.Count == 0;
				report.assets.Add(result);
			}
			report.pass = File.Exists(SourceBlend) && report.assets.Count == Specs.Length && report.assets.All(asset => asset.pass);
			return report;
		}

		private static Bounds CombinedBounds(Renderer[] renderers)
		{
			if (renderers == null || renderers.Length == 0) return new Bounds();
			Bounds bounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
			return bounds;
		}

		private static int TriangleCount(Transform value)
		{
			return value == null ? 0 : value.GetComponentsInChildren<MeshFilter>(true)
				.Where(filter => filter.sharedMesh != null)
				.Sum(filter => Enumerable.Range(0, filter.sharedMesh.subMeshCount).Sum(index => (int)filter.sharedMesh.GetIndexCount(index) / 3));
		}

		private static string ColliderSignature(GameObject prefab)
		{
			if (prefab == null) return "missing";
			return string.Join("|", prefab.GetComponents<Collider>().Select(collider =>
			{
				BoxCollider box = collider as BoxCollider;
				return box == null
					? collider.GetType().Name + ":" + collider.isTrigger
					: string.Format("Box:{0:F5},{1:F5},{2:F5}:{3:F5},{4:F5},{5:F5}:{6}", box.center.x, box.center.y, box.center.z, box.size.x, box.size.y, box.size.z, box.isTrigger);
			}).OrderBy(value => value, StringComparer.Ordinal));
		}

		private static bool IsEnabled()
		{
			TextAsset registry = AssetDatabase.LoadAssetAtPath<TextAsset>(RegistryPath);
			MigrationRegistry parsed = registry == null ? null : JsonUtility.FromJson<MigrationRegistry>(registry.text);
			return parsed != null && parsed.phase5EnvironmentKitEnabled;
		}

		private static bool HasAllRuntimeModels(EnvironmentSpec spec)
		{
			return Enumerable.Range(0, 3).All(lod => File.Exists(ModelPath(spec, lod)));
		}

		private static string ModelPath(EnvironmentSpec spec, int lod) => RuntimeRoot + "/PRP_" + spec.Name + "_Mid_LOD" + lod + ".glb";
		private static string PrefabPath(EnvironmentSpec spec) => PrefabRoot + "/" + spec.Name + ".prefab";

		private static void WriteReport(MigrationReport report)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
		}

		private static void EnsureFolder(string parent, string name)
		{
			if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
		}

		private sealed class EnvironmentSpec
		{
			public readonly string Name;
			public readonly float Width;
			public readonly float Height;
			public readonly float Depth;
			public readonly int Lod0;
			public readonly int Lod1;
			public readonly int Lod2;
			public readonly bool ContactShadow;

			public EnvironmentSpec(string name, float width, float height, float depth, int lod0, int lod1, int lod2, bool contactShadow = true)
			{
				Name = name;
				Width = width;
				Height = height;
				Depth = depth;
				Lod0 = lod0;
				Lod1 = lod1;
				Lod2 = lod2;
				ContactShadow = contactShadow;
			}
		}

		[Serializable]
		private sealed class MigrationRegistry
		{
			public bool phase5EnvironmentKitEnabled;
		}

		[Serializable]
		private sealed class MigrationReport
		{
			public bool pass;
			public string generatedUtc;
			public string sourceBlend;
			public List<AssetResult> assets;
		}

		[Serializable]
		private sealed class AssetResult
		{
			public string asset;
			public bool pass;
			public bool prefabGuidPreserved;
			public bool colliderPreserved;
			public float[] size;
			public int lod0Triangles;
			public int lod1Triangles;
			public int lod2Triangles;
			public List<string> issues;
		}
	}
}
