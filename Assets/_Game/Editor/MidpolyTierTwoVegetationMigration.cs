using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Interaction;
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
	/// Phase-5-Migration fuer Hartholzbaum und Sumpfhanf. Gameplay-Daten,
	/// Collider und Zonenbudgets bleiben unveraendert; die sichtbaren Zustands-
	/// Prefabs werden aus den freigegebenen Blender-/GLB-Quellen aufgebaut.
	/// </summary>
	internal static class MidpolyTierTwoVegetationMigration
	{
		private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
		private const string RuntimeRoot = "Assets/_Game/Art/MidPoly/Resources";
		private const string VisualRoot = "Assets/_Game/Prefabs/Resources/Visuals";
		private const string ReportPath = "Documentation/Etappen/MidPoly/Phase5/TIER_TWO_VEGETATION_TECHNICAL_REPORT.json";

		private static readonly ResourceSpec[] Specs =
		{
			new ResourceSpec("resource.hardwood_tree", "HardwoodTree", 6.65f, 0.84f, 7856, 4320, 1368, 1236),
			new ResourceSpec("resource.swamp_hemp", "SwampHemp", 1.372f, 0.228f, 3836, 2108, 692, 1290)
		};

		private static readonly string[] ZoneVariantStems =
		{
			"TwilightGrove_hardwood_tree",
			"TwilightGrove_swamp_hemp",
			"VeilMarsh_swamp_hemp"
		};

		[MenuItem("Eidren/Mid-Poly/Phase 5/Hartholz und Sumpfhanf integrieren und pruefen")]
		public static void IntegrateApprovedResources()
		{
			if (!IsEnabled())
			{
				throw new InvalidOperationException("Phase-5-Hartholz-/Sumpfhanf-Familie ist in " + RegistryPath + " nicht freigegeben.");
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			Dictionary<string, string> activeGuids = Specs.ToDictionary(
				spec => spec.Stem,
				spec => AssetDatabase.AssetPathToGUID(VisualPath(spec, true)),
				StringComparer.Ordinal);
			Dictionary<string, string> exhaustedGuids = Specs.ToDictionary(
				spec => spec.Stem,
				spec => AssetDatabase.AssetPathToGUID(VisualPath(spec, false)),
				StringComparer.Ordinal);

			BuildApprovedVisuals();
			ResourceContentBuilder.RebuildTierTwoNodePrefabs();
			AreaArtAssetBuilder.BuildTierTwo();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			MigrationReport report = Validate(activeGuids, exhaustedGuids);
			WriteReport(report);
			if (!report.pass)
			{
				throw new InvalidOperationException("Phase-5-Hartholz-/Sumpfhanf-Familie ist nicht integrationsfaehig. Siehe " + ReportPath + ".");
			}
			Debug.Log("[MIDPOLY-000] Phase 5 Hartholz/Sumpfhanf integriert: Active/Exhausted, drei LODs, Idle_Sway, Harvest_Recoil, Nodes und Zonenvarianten PASS.");
		}

		internal static bool TryBuildApprovedVisuals()
		{
			if (!IsEnabled() || !Specs.All(HasAllRuntimeModels))
			{
				return false;
			}
			BuildApprovedVisuals();
			return true;
		}

		private static void BuildApprovedVisuals()
		{
			foreach (ResourceSpec spec in Specs)
			{
				BuildVisual(spec, true);
				BuildVisual(spec, false);
			}
			AssetDatabase.SaveAssets();
		}

		private static void BuildVisual(ResourceSpec spec, bool active)
		{
			GameObject root = new GameObject(spec.Stem + (active ? "_Active" : "_Exhausted"));
			try
			{
				if (active)
				{
					string animatedPath = ModelPath(spec, "Active_Lod0_Animated");
					GameObject lod0 = InstantiateModel(animatedPath, root.transform, "LOD0");
					GameObject lod1 = InstantiateModel(ModelPath(spec, "Active_Lod1"), root.transform, "LOD1");
					GameObject lod2 = InstantiateModel(ModelPath(spec, "Active_Lod2"), root.transform, "LOD2");
					NormalizeHeight(lod0, spec.ActiveHeight);
					NormalizeHeight(lod1, spec.ActiveHeight);
					NormalizeHeight(lod2, spec.ActiveHeight);
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

					AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(animatedPath).OfType<AnimationClip>().ToArray();
					AnimationClip idle = clips.FirstOrDefault(clip => clip.name == "Idle_Sway");
					AnimationClip harvest = clips.FirstOrDefault(clip => clip.name == "Harvest_Recoil");
					if (idle == null || harvest == null)
					{
						throw new InvalidDataException(spec.Stem + " enthaelt nicht beide freigegebenen Animationsclips.");
					}
					Animator animator = lod0.GetComponentInChildren<Animator>(true) ?? lod0.AddComponent<Animator>();
					root.AddComponent<MidpolyResourceAnimationDriver>().Configure(animator, idle, harvest);
					// RecalculateBounds aktualisiert bei animierten SkinnedMeshRenderern die
					// Clip-Huelle. Danach noch einmal auf die verbindliche Tabellengroesse
					// normalisieren, damit LOD0 und die statischen LODs deckungsgleich sind.
					NormalizeHeight(lod0, spec.ActiveHeight);
					NormalizeHeight(lod1, spec.ActiveHeight);
					NormalizeHeight(lod2, spec.ActiveHeight);
					group.RecalculateBounds();
					// F34-001/F34-002: Schwellen fuer die orthografische Spielkamera statt Perspektiv-Werte.
					MidpolyLodThresholds.ApplyOrthographicThresholds(group);
					CreateImpactPoint(root.transform, "ImpactPoint_00", new Vector3(-0.24f, spec.ActiveHeight * 0.34f, 0.12f));
					CreateImpactPoint(root.transform, "ImpactPoint_01", new Vector3(0.12f, spec.ActiveHeight * 0.54f, -0.08f));
					CreateImpactPoint(root.transform, "ImpactPoint_02", new Vector3(0.28f, spec.ActiveHeight * 0.72f, 0.14f));
				}
				else
				{
					GameObject lod0 = InstantiateModel(ModelPath(spec, "Exhausted_Lod0"), root.transform, "LOD0");
					NormalizeHeight(lod0, spec.ExhaustedHeight);
				}

				WorldContactShadowBuilder.Attach(root);
				GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, VisualPath(spec, active));
				AssetDatabase.SetLabels(prefab, new[] { "MIDPOLY-000", "MidPoly", "Approved", "Phase5Vegetation" });
			}
			finally
			{
				Object.DestroyImmediate(root);
			}
		}

		private static GameObject InstantiateModel(string path, Transform parent, string name)
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (model == null)
			{
				throw new FileNotFoundException("Importiertes Mid-Poly-Modell fehlt: " + path);
			}
			GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
			instance.name = name;
			instance.transform.SetParent(parent, false);
			instance.transform.localPosition = Vector3.zero;
			instance.transform.localRotation = Quaternion.identity;
			instance.transform.localScale = Vector3.one;
			return instance;
		}

		private static void CreateImpactPoint(Transform parent, string name, Vector3 position)
		{
			GameObject point = new GameObject(name);
			point.transform.SetParent(parent, false);
			point.transform.localPosition = position;
		}

		private static void NormalizeHeight(GameObject model, float targetHeight)
		{
			Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
			if (renderers.Length == 0)
			{
				throw new InvalidDataException(model.name + " besitzt keinen Renderer fuer die Hoehennormalisierung.");
			}
			Bounds bounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers.Skip(1))
			{
				bounds.Encapsulate(renderer.bounds);
			}
			if (bounds.size.y <= 0.0001f)
			{
				throw new InvalidDataException(model.name + " besitzt eine ungueltige Hoehe.");
			}
			Vector3 scale = model.transform.localScale;
			scale.y *= targetHeight / bounds.size.y;
			model.transform.localScale = scale;
			renderers = model.GetComponentsInChildren<Renderer>(true);
			bounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers.Skip(1))
			{
				bounds.Encapsulate(renderer.bounds);
			}
			model.transform.position += Vector3.up * -bounds.min.y;
		}

		private static MigrationReport Validate(
			IReadOnlyDictionary<string, string> activeGuids,
			IReadOnlyDictionary<string, string> exhaustedGuids)
		{
			MigrationReport report = new MigrationReport
			{
				generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				assets = new List<AssetResult>(),
				zoneVariants = new List<VariantResult>()
			};

			foreach (ResourceSpec spec in Specs)
			{
				AssetResult result = new AssetResult { asset = spec.Stem, issues = new List<string>() };
				ValidateVisual(spec, true, result);
				ValidateVisual(spec, false, result);
				result.prefabGuidsPreserved =
					GuidPreserved(activeGuids[spec.Stem], VisualPath(spec, true)) &&
					GuidPreserved(exhaustedGuids[spec.Stem], VisualPath(spec, false));
				if (!result.prefabGuidsPreserved)
				{
					result.issues.Add("Active-/Exhausted-Prefab-GUID hat sich geaendert.");
				}

				ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/_Game/Data/Resources/" + spec.Stem + ".asset");
				if (definition == null || definition.ActiveVisualPrefab != AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath(spec, true)) || definition.ExhaustedVisualPrefab != AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath(spec, false)))
				{
					result.issues.Add("ResourceNodeDefinition verweist nicht auf beide freigegebenen Visuals.");
				}
				if (definition == null || definition.NodePrefab == null || definition.NodePrefab.GetComponent<ResourceNode>() == null)
				{
					result.issues.Add("ResourceNode/Node-Prefab fehlt.");
				}
				result.pass = result.issues.Count == 0;
				report.assets.Add(result);
			}

			foreach (string stem in ZoneVariantStems)
			{
				foreach (string state in new[] { "Active", "Exhausted" })
				{
					string path = "Assets/_Game/Prefabs/Environment/AreaArtVariants/" + stem + "_" + state + ".prefab";
					VariantResult variant = new VariantResult { path = path, issues = new List<string>() };
					GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
					if (prefab == null || prefab.GetComponentInChildren<MeshRenderer>(true) == null)
					{
						variant.issues.Add("Zonenvariante fehlt oder hat kein 3D-Mesh.");
					}
					if (state == "Active" && (prefab == null || prefab.GetComponent<LODGroup>() == null || prefab.GetComponent<MidpolyResourceAnimationDriver>() == null))
					{
						variant.issues.Add("Active-Zonenvariante besitzt nicht den LOD-/Animationsvertrag.");
					}
					variant.pass = variant.issues.Count == 0;
					report.zoneVariants.Add(variant);
				}
			}
			report.pass = report.assets.All(asset => asset.pass) && report.zoneVariants.All(variant => variant.pass);
			return report;
		}

		private static void ValidateVisual(ResourceSpec spec, bool active, AssetResult result)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath(spec, active));
			if (prefab == null)
			{
				result.issues.Add("Prefab fehlt: " + VisualPath(spec, active));
				return;
			}
			if (prefab.GetComponentsInChildren<Collider>(true).Length != 0)
			{
				result.issues.Add("Grafik-Prefab traegt einen Collider.");
			}
			Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true)
				.Where(renderer => renderer.gameObject.name != WorldContactShadowBuilder.ChildName)
				.ToArray();
			if (renderers.Length == 0)
			{
				result.issues.Add("Grafik-Prefab enthaelt keinen Renderer.");
				return;
			}
			Bounds bounds = renderers[0].bounds;
			foreach (Renderer renderer in renderers.Skip(1))
			{
				bounds.Encapsulate(renderer.bounds);
			}
			float expectedHeight = active ? spec.ActiveHeight : spec.ExhaustedHeight;
			if (Mathf.Abs(bounds.size.y - expectedHeight) > 0.015f)
			{
				result.issues.Add(string.Format("{0}-Hoehe {1:F3} statt {2:F3}.", active ? "Active" : "Exhausted", bounds.size.y, expectedHeight));
			}

			if (active)
			{
				LOD[] lods = prefab.GetComponent<LODGroup>()?.GetLODs();
				if (lods == null || lods.Length != 3)
				{
					result.issues.Add("Active-LODGroup ist nicht vollstaendig.");
				}
				MidpolyResourceAnimationDriver driver = prefab.GetComponent<MidpolyResourceAnimationDriver>();
				if (driver == null || !driver.IsConfigured)
				{
					result.issues.Add("Idle_Sway/Harvest_Recoil ist nicht konfiguriert.");
				}
				AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath(spec, "Active_Lod0_Animated")).OfType<AnimationClip>().ToArray();
				if (!clips.Any(clip => clip.name == "Idle_Sway") || !clips.Any(clip => clip.name == "Harvest_Recoil"))
				{
					result.issues.Add("GLB enthaelt nicht beide freigegebenen Animationsclips.");
				}
				result.lod0Triangles = TriangleCount(prefab.transform.Find("LOD0"));
				result.lod1Triangles = TriangleCount(prefab.transform.Find("LOD1"));
				result.lod2Triangles = TriangleCount(prefab.transform.Find("LOD2"));
				if (result.lod0Triangles != spec.Lod0 || result.lod1Triangles != spec.Lod1 || result.lod2Triangles != spec.Lod2)
				{
					result.issues.Add("LOD-Dreieckszahlen weichen vom Produktionsmanifest ab.");
				}
			}
			else
			{
				result.exhaustedTriangles = TriangleCount(prefab.transform.Find("LOD0"));
				if (result.exhaustedTriangles != spec.Exhausted)
				{
					result.issues.Add("Exhausted-Dreieckszahl weicht vom Produktionsmanifest ab.");
				}
			}
		}

		private static int TriangleCount(Transform root)
		{
			return root == null ? 0 : root.GetComponentsInChildren<MeshFilter>(true)
				.Where(filter => filter.sharedMesh != null)
				.Sum(filter => Enumerable.Range(0, filter.sharedMesh.subMeshCount)
					.Sum(index => (int)filter.sharedMesh.GetIndexCount(index) / 3));
		}

		private static bool GuidPreserved(string oldGuid, string path)
		{
			return string.IsNullOrEmpty(oldGuid) || string.Equals(oldGuid, AssetDatabase.AssetPathToGUID(path), StringComparison.Ordinal);
		}

		private static bool IsEnabled()
		{
			TextAsset registry = AssetDatabase.LoadAssetAtPath<TextAsset>(RegistryPath);
			MigrationRegistry parsed = registry == null ? null : JsonUtility.FromJson<MigrationRegistry>(registry.text);
			return parsed != null && parsed.phase5TierTwoVegetationKitEnabled;
		}

		private static bool HasAllRuntimeModels(ResourceSpec spec)
		{
			return File.Exists(ModelPath(spec, "Active_Lod0_Animated")) &&
				File.Exists(ModelPath(spec, "Active_Lod1")) &&
				File.Exists(ModelPath(spec, "Active_Lod2")) &&
				File.Exists(ModelPath(spec, "Exhausted_Lod0"));
		}

		private static string ModelPath(ResourceSpec spec, string suffix)
		{
			return RuntimeRoot + "/" + spec.Stem + "/Runtime/RES_" + spec.Stem + "_" + suffix + ".glb";
		}

		private static string VisualPath(ResourceSpec spec, bool active)
		{
			return VisualRoot + "/" + spec.Stem + (active ? "_Active.prefab" : "_Exhausted.prefab");
		}

		private static void WriteReport(MigrationReport report)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
		}

		private sealed class ResourceSpec
		{
			public readonly string Id;
			public readonly string Stem;
			public readonly float ActiveHeight;
			public readonly float ExhaustedHeight;
			public readonly int Lod0;
			public readonly int Lod1;
			public readonly int Lod2;
			public readonly int Exhausted;

			public ResourceSpec(string id, string stem, float activeHeight, float exhaustedHeight, int lod0, int lod1, int lod2, int exhausted)
			{
				Id = id;
				Stem = stem;
				ActiveHeight = activeHeight;
				ExhaustedHeight = exhaustedHeight;
				Lod0 = lod0;
				Lod1 = lod1;
				Lod2 = lod2;
				Exhausted = exhausted;
			}
		}

		[Serializable]
		private sealed class MigrationRegistry
		{
			public bool phase5TierTwoVegetationKitEnabled;
		}

		[Serializable]
		private sealed class MigrationReport
		{
			public bool pass;
			public string generatedUtc;
			public List<AssetResult> assets;
			public List<VariantResult> zoneVariants;
		}

		[Serializable]
		private sealed class AssetResult
		{
			public string asset;
			public bool pass;
			public bool prefabGuidsPreserved;
			public int lod0Triangles;
			public int lod1Triangles;
			public int lod2Triangles;
			public int exhaustedTriangles;
			public List<string> issues;
		}

		[Serializable]
		private sealed class VariantResult
		{
			public string path;
			public bool pass;
			public List<string> issues;
		}
	}
}
