using Eidren.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Baut die 3D-Darstellungsprefabs der Kreaturenserie aus ihren GLB-Dateien.
	/// Muster und Reihenfolge sind vom Wanderer3DPlayerBuilder uebernommen; der
	/// Unterschied liegt in der Darstellungsschicht (CreatureMeshPresentation
	/// statt MeshActorPresentation) und darin, dass es keine Ruestungs- und
	/// Waffenknoten zu schalten gibt.
	///
	/// Idempotent: mehrfache Laeufe erzeugen denselben Endzustand.
	///
	/// Der erste Schritt je Figur ist der erzwungene Legacy-Import. glTFast
	/// importiert GLB standardmaessig mit Mecanim/Animator; die Schicht braucht
	/// eine Animation-Komponente mit Legacy-Clips. Derselbe Befund wie beim
	/// Wanderer (Task-1-Befund, w3d-t6-legacy.log).
	/// </summary>
	public static class CreatureMeshBuilder
	{
		private const string ShaderName = "Eidren/Actors/WandererVertexLit";
		private const string MaterialPath = "Assets/_Game/Art/Actors/M_Kreatur_VertexLit.mat";
		private const string VisualPrefabFolder = "Assets/_Game/Prefabs/Actors/3D";
		private const string ActorRoot = "Assets/_Game/Art/Actors";

		// GLTFast.AnimationMethod.Legacy == 1 (None=0, Legacy=1, Mecanim=2).
		private const int GltfAnimationMethodLegacy = 1;
		private const string AnimationMethodPropertyPath = "importSettings.animationMethod";

		/// <summary>
		/// Name und Sollhoehe je Figur. Die Hoehen sind die verbindlichen Werte
		/// aus V02ActorVisualBuilder beziehungsweise — fuer die Eidra und den
		/// Boss — aus Resources/Data/VisualScaleTable.asset. Sie stehen hier
		/// noch einmal, weil das Prefab sie fuer die Verdeckungs-Raycasts
		/// braucht (IActorPresentation.WorldHeight).
		/// </summary>
		public static readonly (string Name, float Height)[] Creatures =
		{
			("Riftling", 1.70f),
			("MoorThrower", 1.90f),
			("RiftGuardian", 3.00f),
			("ForgeGuardian", 2.50f),
			("SealGuardian", 3.00f),
			("CoreGuardian", 4.50f),
			("EmberEater", 1.60f),
			("RootCharger", 2.20f),
			("GraniteShell", 2.40f),
			("AshRunner", 1.80f),
			("Ignivar", 1.00f),
			("Noctarion", 1.00f),
			("Terrock", 1.00f),
			("Garon", 4.50f),
			// Der Wildling ist die Ur-Figur der Serie (06.08.2026) und traegt
			// seit dem 13.08. den nachgeruesteten Erscheinen-Clip. Sein Modell
			// misst 1,645 m — das ist die gegen die Sprite-Vorlage abgenommene
			// Sichthoehe; die 1,90 sind der Raycast-Wert aus
			// VisualScaleTable (visual.wildling), wie ihn auch das 2D-Quad trug.
			("Wildling", 1.90f)
		};

		public static string GlbPath(string name)
		{
			return ActorRoot + "/" + name + "/" + name + "3D/" + name + ".glb";
		}

		public static string VisualPrefabPath(string name)
		{
			return VisualPrefabFolder + "/" + name + "_3D.prefab";
		}

		[MenuItem("Eidren/V0.2/Build Riftling3D (Referenzfall)")]
		public static void BuildRiftling()
		{
			Build("Riftling", 1.70f);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("[K3D] Riftling_3D.prefab gebaut.");
		}

		[MenuItem("Eidren/V0.2/Build alle Kreaturen 3D")]
		public static void BuildAll()
		{
			var fehler = new List<string>();
			foreach ((string name, float height) in Creatures)
			{
				try
				{
					Build(name, height);
				}
				catch (Exception ex)
				{
					fehler.Add(name + ": " + ex.Message);
				}
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			if (fehler.Count > 0)
			{
				Debug.LogError("[K3D] " + fehler.Count + " Figuren nicht gebaut:\n"
					+ string.Join("\n", fehler));
				return;
			}
			Debug.Log("[K3D] " + Creatures.Length + " Kreaturen-Prefabs gebaut.");
		}

		public static void Build(string name, float worldHeight)
		{
			if (name == "AshRunner" && MidpolyAshRunnerMigration.TryBuildApprovedVisual())
			{
				return;
			}
			if (name == "SealGuardian" && MidpolySealGuardianMigration.TryBuildApprovedVisual())
			{
				return;
			}
			if (name == "ForgeGuardian" && MidpolyForgeGuardianMigration.TryBuildApprovedVisual())
			{
				return;
			}
			if (name == "RiftGuardian" && MidpolyRiftGuardianMigration.TryBuildApprovedVisual())
			{
				return;
			}
			if (name == "EmberEater" && MidpolyEmberEaterMigration.TryBuildApprovedVisual())
			{
				return;
			}
			if (name == "Riftling" && MidpolyRiftlingMigration.TryBuildApprovedVisual())
			{
				return;
			}
			if (name == "Wildling" && MidpolyWildlingMigration.TryBuildApprovedVisual())
			{
				return;
			}
			if (name == "RootCharger" && MidpolyRootChargerMigration.TryBuildApprovedVisual())
			{
				return;
			}
			if (name == "GraniteShell" && MidpolyGraniteShellMigration.TryBuildApprovedVisual())
			{
				return;
			}
			if (name == "MoorThrower" && MidpolyMoorThrowerMigration.TryBuildApprovedVisual())
			{
				return;
			}
			string glb = GlbPath(name);
			if (!File.Exists(glb))
			{
				throw new InvalidOperationException("GLB fehlt: " + glb);
			}
			EnsureLegacyAnimationImport(glb);
			Material material = EnsureMaterial();
			BuildVisualPrefab(name, glb, worldHeight, material);
		}

		private static void EnsureLegacyAnimationImport(string glb)
		{
			AssetImporter importer = AssetImporter.GetAtPath(glb);
			if (importer == null)
			{
				throw new InvalidOperationException("GLB-Importer nicht gefunden: " + glb
					+ " — die Datei ist vermutlich noch nicht importiert.");
			}
			var serialized = new SerializedObject(importer);
			SerializedProperty animationMethod = serialized.FindProperty(AnimationMethodPropertyPath);
			if (animationMethod == null)
			{
				throw new InvalidOperationException("Property '" + AnimationMethodPropertyPath
					+ "' nicht am GLB-Importer gefunden: " + glb);
			}
			if (animationMethod.intValue == GltfAnimationMethodLegacy)
			{
				return;
			}
			animationMethod.intValue = GltfAnimationMethodLegacy;
			serialized.ApplyModifiedProperties();
			importer.SaveAndReimport();
		}

		private static Material EnsureMaterial()
		{
			Shader shader = Shader.Find(ShaderName);
			if (shader == null)
			{
				throw new InvalidOperationException("Shader fehlt: " + ShaderName);
			}
			Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
			if (material == null)
			{
				material = new Material(shader);
				AssetDatabase.CreateAsset(material, MaterialPath);
			}
			else
			{
				material.shader = shader;
			}
			return material;
		}

		private static void BuildVisualPrefab(string name, string glb, float worldHeight,
			Material material)
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(glb);
			if (model == null)
			{
				throw new InvalidOperationException("GLB nicht importiert: " + glb);
			}
			Directory.CreateDirectory(VisualPrefabFolder);
			var root = new GameObject(name + "_3D");
			try
			{
				GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(model);
				rig.name = name;
				rig.transform.SetParent(root.transform, worldPositionStays: false);

				foreach (SkinnedMeshRenderer renderer in
					rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
				{
					renderer.sharedMaterials = Enumerable
						.Repeat(material, renderer.sharedMaterials.Length).ToArray();
					renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
					renderer.receiveShadows = true;
				}

				Animation animationPlayer = rig.GetComponentInChildren<Animation>(true);
				if (animationPlayer == null)
				{
					animationPlayer = rig.AddComponent<Animation>();
				}
				animationPlayer.playAutomatically = false;

				AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(glb)
					.OfType<AnimationClip>()
					.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
					.ToArray();
				if (clips.Length == 0)
				{
					throw new InvalidOperationException("Keine Clips in " + glb
						+ " — steht der Import auf Legacy?");
				}
				foreach (AnimationClip clip in clips)
				{
					if (animationPlayer.GetClip(clip.name) == null)
					{
						animationPlayer.AddClip(clip, clip.name);
					}
				}

				CreatureMeshPresentation presentation =
					root.AddComponent<CreatureMeshPresentation>();
				presentation.Configure(animationPlayer, rig.transform, worldHeight);

				PrefabUtility.SaveAsPrefabAsset(root, VisualPrefabPath(name));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}
	}
}
