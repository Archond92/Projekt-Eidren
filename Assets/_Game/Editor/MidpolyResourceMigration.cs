using Eidren.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Eidren.Gameplay.Presentation;

namespace Eidren.Editor
{
/// <summary>
/// Kontrollierte Pilotmigration der fuenf durch Showcase und GLB-Roundtrip
/// freigegebenen MIDPOLY-000-Ressourcen. Bestehende Prefab-Pfade und damit
/// GUIDs bleiben erhalten; die alten Builder bleiben als deaktivierbarer
/// Fallback verfuegbar.
/// </summary>
internal static class MidpolyResourceMigration
{
	private const string RegistryPath = "Assets/_Game/Art/MidPoly/MIDPOLY_MIGRATION_REGISTRY.json";
	private const string RuntimeRoot = "Assets/_Game/Art/MidPoly/Resources";
	private const string VisualRoot = "Assets/_Game/Prefabs/Resources/Visuals";
	private const string ReportPath = "Documentation/Etappen/MidPoly/Phase1/RESOURCE_PILOT_REPORT.json";

	private static readonly ResourceSpec[] Specs =
	{
		new ResourceSpec("resource.tree", "Tree", 6.0f, 0.7f, "Idle_Sway", "Harvest_Recoil"),
		new ResourceSpec("resource.stone_deposit", "StoneDeposit", 1.1f, 0.5f, "Harvest_Recoil"),
		new ResourceSpec("resource.copper_vein", "CopperVein", 1.3f, 0.58f, "Harvest_Recoil"),
		new ResourceSpec("resource.berry_bush", "BerryBush", 0.9f, 0.7f, "Idle_Sway", "Harvest_Recoil"),
		new ResourceSpec("resource.fiber_plant", "FiberPlant", 0.9f, 0.25f, "Idle_Sway", "Harvest_Recoil")
	};

	private static readonly string[] TierOneVariantStems =
	{
		"Greenwood_tree",
		"Marsh_tree",
		"Marsh_fiber_plant",
		"Quarry_tree",
		"Quarry_stone_deposit",
		"EmberRuins_tree"
	};

	[MenuItem("Eidren/Mid-Poly/Pilotfamilie/Ressourcen integrieren und pruefen")]
	public static void IntegrateApprovedResources()
	{
		if (!IsPilotEnabled())
		{
			throw new InvalidOperationException("Die Mid-Poly-Ressourcenpilotfamilie ist in " + RegistryPath + " nicht freigegeben.");
		}

		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		Dictionary<string, string> guids = Specs.ToDictionary(
			spec => spec.Stem,
			spec => AssetDatabase.AssetPathToGUID(VisualPath(spec, active: true)),
			StringComparer.Ordinal);

		BuildApprovedVisuals();
		CopperMiningFeedbackRebuilder.Rebuild();
		ResourceContentBuilder.RebuildTierOneNodePrefabs();
		AreaArtAssetBuilder.BuildTierOne();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

		PilotReport report = Validate(guids);
		WriteReport(report);
		if (!report.pass)
		{
			throw new InvalidOperationException("Mid-Poly-Ressourcenpilot ist nicht integrationsfaehig. Siehe " + ReportPath + ".");
		}
		Debug.Log("[MIDPOLY-000] Ressourcenpilot integriert: 5 Active/Exhausted-Paare, LODs, Node-Prefabs und Builder-Schutz PASS.");
	}

	/// <summary>
	/// Builder-Einstieg: true bedeutet, dass die freigegebenen Mid-Poly-GLBs
	/// erfolgreich gebaut wurden. false erlaubt den bisherigen T1-Fallback.
	/// </summary>
	internal static bool TryBuildApprovedTierOneVisuals()
	{
		if (!IsPilotEnabled() || !Specs.All(HasAllRuntimeModels))
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
			BuildVisual(spec, active: true);
			BuildVisual(spec, active: false);
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
				GameObject lod0 = InstantiateModel(ModelPath(spec, "Active_Lod0_Animated"), root.transform, "LOD0");
				GameObject lod1 = InstantiateModel(ModelPath(spec, "Active_Lod1"), root.transform, "LOD1");
				GameObject lod2 = InstantiateModel(ModelPath(spec, "Active_Lod2"), root.transform, "LOD2");

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

				AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath(spec, "Active_Lod0_Animated"))
					.OfType<AnimationClip>()
					.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
					.ToArray();
				Animator animator = lod0.GetComponentInChildren<Animator>(true);
				if (animator == null)
				{
					animator = lod0.AddComponent<Animator>();
				}
				AnimationClip idle = clips.FirstOrDefault(clip => clip.name == "Idle_Sway");
				AnimationClip harvest = clips.FirstOrDefault(clip => clip.name == "Harvest_Recoil");
				root.AddComponent<MidpolyResourceAnimationDriver>().Configure(animator, idle, harvest);

				if (spec.Stem == "CopperVein")
				{
					CreateImpactPoint(root.transform, "ImpactPoint_00", new Vector3(-0.48f, 0.62f, 0.18f));
					CreateImpactPoint(root.transform, "ImpactPoint_01", new Vector3(0.22f, 0.82f, -0.08f));
					CreateImpactPoint(root.transform, "ImpactPoint_02", new Vector3(0.58f, 0.48f, 0.24f));
				}
			}
			else
			{
				GameObject exhausted = InstantiateModel(ModelPath(spec, "Exhausted_Lod0"), root.transform, "LOD0");
				if (spec.Stem == "CopperVein") NeutralizeExhaustedCopper(exhausted);
			}

			WorldContactShadowBuilder.Attach(root);
			string path = VisualPath(spec, active);
			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
			AssetDatabase.SetLabels(prefab, new[] { "MIDPOLY-000", "MidPoly", "Approved", "ResourcePilot" });
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	private static void NeutralizeExhaustedCopper(GameObject exhausted)
	{
		Material hostRock = exhausted.GetComponentsInChildren<Renderer>(true)
			.SelectMany(renderer => renderer.sharedMaterials)
			.FirstOrDefault(material => material != null && material.name == "RES_Copper_HostRock");
		if (hostRock == null) return;

		foreach (Renderer renderer in exhausted.GetComponentsInChildren<Renderer>(true))
		{
			Material[] materials = renderer.sharedMaterials;
			bool changed = false;
			for (int index = 0; index < materials.Length; index++)
			{
				if (materials[index] == null || materials[index].name != "RES_Copper_Ore") continue;
				materials[index] = hostRock;
				changed = true;
			}
			if (changed) renderer.sharedMaterials = materials;
		}
	}

	private static GameObject InstantiateModel(string path, Transform parent, string name)
	{
		GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		if (model == null)
		{
			throw new FileNotFoundException("Importiertes Mid-Poly-Modell fehlt oder ist kein GameObject: " + path);
		}
		GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
		instance.name = name;
		instance.transform.SetParent(parent, worldPositionStays: false);
		instance.transform.localPosition = Vector3.zero;
		instance.transform.localRotation = Quaternion.identity;
		instance.transform.localScale = Vector3.one;
		return instance;
	}

	private static void CreateImpactPoint(Transform parent, string name, Vector3 position)
	{
		GameObject point = new GameObject(name);
		point.transform.SetParent(parent, worldPositionStays: false);
		point.transform.localPosition = position;
	}

	private static PilotReport Validate(IReadOnlyDictionary<string, string> previousGuids)
	{
		PilotReport report = new PilotReport
		{
			generatedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
			registry = RegistryPath,
			assets = new List<AssetResult>()
		};

		foreach (ResourceSpec spec in Specs)
		{
			AssetResult result = new AssetResult { asset = spec.Stem, issues = new List<string>() };
			ValidateVisual(spec, active: true, spec.ActiveHeight, result);
			ValidateVisual(spec, active: false, spec.ExhaustedHeight, result);

			string activePath = VisualPath(spec, active: true);
			string oldGuid = previousGuids.TryGetValue(spec.Stem, out string value) ? value : string.Empty;
			string newGuid = AssetDatabase.AssetPathToGUID(activePath);
			result.prefabGuidPreserved = string.IsNullOrEmpty(oldGuid) || string.Equals(oldGuid, newGuid, StringComparison.Ordinal);
			if (!result.prefabGuidPreserved)
			{
				result.issues.Add("Active-Prefab-GUID hat sich geaendert.");
			}

			string lod0Path = ModelPath(spec, "Active_Lod0_Animated");
			result.clips = AssetDatabase.LoadAllAssetsAtPath(lod0Path)
				.OfType<AnimationClip>()
				.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
				.Select(clip => clip.name)
				.Distinct(StringComparer.Ordinal)
				.OrderBy(name => name, StringComparer.Ordinal)
				.ToArray();
			foreach (string expected in spec.ExpectedClips)
			{
				if (!result.clips.Contains(expected, StringComparer.Ordinal))
				{
					result.issues.Add("Erwarteter Clip fehlt im Unity-Reimport: " + expected);
				}
			}

			ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/_Game/Data/Resources/" + spec.Stem + ".asset");
			if (definition == null || definition.ActiveVisualPrefab == null || definition.ExhaustedVisualPrefab == null)
			{
				result.issues.Add("ResourceNodeDefinition verweist nicht auf beide Visuals.");
			}
			if (spec.Stem == "CopperVein" && AssetDatabase.LoadAssetAtPath<GameObject>(activePath).GetComponent<Eidren.Gameplay.Presentation.CopperMiningVisualFeedback>() == null)
			{
				result.issues.Add("Kupfer-Abbaufeedback fehlt.");
			}

			result.pass = result.issues.Count == 0;
			report.assets.Add(result);
		}
		report.zoneVariants = ValidateZoneVariants();
		report.pass = report.assets.All(asset => asset.pass) && report.zoneVariants.All(variant => variant.pass);
		return report;
	}

	private static List<VariantResult> ValidateZoneVariants()
	{
		List<VariantResult> results = new List<VariantResult>();
		foreach (string stem in TierOneVariantStems)
		{
			foreach (string state in new[] { "Active", "Exhausted" })
			{
				string path = "Assets/_Game/Prefabs/Environment/AreaArtVariants/" + stem + "_" + state + ".prefab";
				VariantResult result = new VariantResult { path = path, issues = new List<string>() };
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab == null)
				{
					result.issues.Add("Gebietsvariante fehlt.");
				}
				else
				{
					if (prefab.GetComponentInChildren<MeshRenderer>(true) == null)
					{
						result.issues.Add("Gebietsvariante besitzt kein 3D-Mesh.");
					}
					if (state == "Active")
					{
						LODGroup group = prefab.GetComponent<LODGroup>();
						if (group == null || group.GetLODs().Length != 3)
						{
							result.issues.Add("Active-Gebietsvariante hat keinen Mid-Poly-LOD0/1/2-Vertrag.");
						}
						MidpolyResourceAnimationDriver driver = prefab.GetComponent<MidpolyResourceAnimationDriver>();
						if (driver == null || !driver.IsConfigured)
						{
							result.issues.Add("Active-Gebietsvariante hat keinen konfigurierten Animationsdriver.");
						}
					}
				}
				result.pass = result.issues.Count == 0;
				results.Add(result);
			}
		}
		return results;
	}

	private static void ValidateVisual(ResourceSpec spec, bool active, float expectedHeight, AssetResult result)
	{
		string path = VisualPath(spec, active);
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		if (prefab == null)
		{
			result.issues.Add("Prefab fehlt: " + path);
			return;
		}
		Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true)
			.Where(renderer => renderer.gameObject.name != WorldContactShadowBuilder.ChildName)
			.ToArray();
		if (renderers.Length == 0)
		{
			result.issues.Add((active ? "Active" : "Exhausted") + " besitzt keinen Renderer.");
			return;
		}
		Bounds bounds = renderers[0].bounds;
		foreach (Renderer renderer in renderers.Skip(1))
		{
			bounds.Encapsulate(renderer.bounds);
		}
		float tolerance = Mathf.Max(0.05f, expectedHeight * 0.04f);
		if (Mathf.Abs(bounds.size.y - expectedHeight) > tolerance)
		{
			result.issues.Add(string.Format("{0}-Hoehe {1:F3} statt {2:F3} (+/-{3:F3}).", active ? "Active" : "Exhausted", bounds.size.y, expectedHeight, tolerance));
		}
			if (active)
			{
			LODGroup group = prefab.GetComponent<LODGroup>();
				if (group == null || group.GetLODs().Length != 3 || group.GetLODs().Any(lod => lod.renderers.Length == 0))
			{
				result.issues.Add("Active-LODGroup ist nicht vollstaendig (LOD0/1/2).");
				}
				MidpolyResourceAnimationDriver driver = prefab.GetComponent<MidpolyResourceAnimationDriver>();
				if (driver == null || !driver.IsConfigured)
				{
					result.issues.Add("Active-Animationsdriver ist nicht vollstaendig konfiguriert.");
				}
				result.activeTriangles = TriangleCount(prefab.transform.Find("LOD0"));
			result.lod1Triangles = TriangleCount(prefab.transform.Find("LOD1"));
			result.lod2Triangles = TriangleCount(prefab.transform.Find("LOD2"));
		}
		else
		{
			result.exhaustedTriangles = TriangleCount(prefab.transform.Find("LOD0"));
		}
	}

	private static int TriangleCount(Transform root)
	{
		if (root == null)
		{
			return 0;
		}
		return root.GetComponentsInChildren<MeshFilter>(true)
			.Where(filter => filter.sharedMesh != null)
			.Sum(filter => Enumerable.Range(0, filter.sharedMesh.subMeshCount)
				.Sum(index => (int)filter.sharedMesh.GetIndexCount(index) / 3));
	}

	private static bool IsPilotEnabled()
	{
		TextAsset registry = AssetDatabase.LoadAssetAtPath<TextAsset>(RegistryPath);
		if (registry == null)
		{
			return false;
		}
		MigrationRegistry parsed = JsonUtility.FromJson<MigrationRegistry>(registry.text);
		return parsed != null && parsed.resourcePilotEnabled;
	}

	private static bool HasAllRuntimeModels(ResourceSpec spec)
	{
		return File.Exists(ModelPath(spec, "Active_Lod0_Animated"))
			&& File.Exists(ModelPath(spec, "Active_Lod1"))
			&& File.Exists(ModelPath(spec, "Active_Lod2"))
			&& File.Exists(ModelPath(spec, "Exhausted_Lod0"));
	}

	private static string ModelPath(ResourceSpec spec, string suffix)
	{
		return RuntimeRoot + "/" + spec.Stem + "/Runtime/RES_" + spec.Stem + "_" + suffix + ".glb";
	}

	private static string VisualPath(ResourceSpec spec, bool active)
	{
		return VisualRoot + "/" + spec.Stem + (active ? "_Active.prefab" : "_Exhausted.prefab");
	}

	private static void WriteReport(PilotReport report)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
		File.WriteAllText(ReportPath, JsonUtility.ToJson(report, prettyPrint: true));
	}

	private sealed class ResourceSpec
	{
		public readonly string Id;
		public readonly string Stem;
		public readonly float ActiveHeight;
		public readonly float ExhaustedHeight;
		public readonly string[] ExpectedClips;

		public ResourceSpec(string id, string stem, float activeHeight, float exhaustedHeight, params string[] expectedClips)
		{
			Id = id;
			Stem = stem;
			ActiveHeight = activeHeight;
			ExhaustedHeight = exhaustedHeight;
			ExpectedClips = expectedClips;
		}
	}

	[Serializable]
	private sealed class MigrationRegistry
	{
		public bool resourcePilotEnabled;
	}

	[Serializable]
	private sealed class PilotReport
	{
		public bool pass;
		public string generatedUtc;
		public string registry;
		public List<AssetResult> assets;
		public List<VariantResult> zoneVariants;
	}

	[Serializable]
	private sealed class VariantResult
	{
		public string path;
		public bool pass;
		public List<string> issues;
	}

	[Serializable]
	private sealed class AssetResult
	{
		public string asset;
		public bool pass;
		public bool prefabGuidPreserved;
		public int activeTriangles;
		public int lod1Triangles;
		public int lod2Triangles;
		public int exhaustedTriangles;
		public string[] clips;
		public List<string> issues;
	}
}
}
