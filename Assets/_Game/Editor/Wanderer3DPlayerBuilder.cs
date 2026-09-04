using Eidren.Combat;
using Eidren.Composition;
using Eidren.Presentation;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Baut das Spieler-Prefab von der Sprite- auf die Wanderer3D-Darstellung um.
	/// Idempotent: mehrfache Laeufe erzeugen denselben Endzustand.
	/// </summary>
	public static class Wanderer3DPlayerBuilder
	{
		private const string GlbPath = "Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb";
		private const string MidPolyGlbPath = "Assets/_Game/Art/Actors/Player/Wanderer3D/Source~/Wanderer_MidPoly_Runtime.glb";
		private const string ArchivedGlbPath = "Documentation/Etappen/MidPoly/LegacyArchive/" + GlbPath;
		private const string MaterialPath = "Assets/_Game/Art/Actors/Player/Wanderer3D/M_Wanderer_VertexLit.mat";
		private const string VisualPrefabPath = "Assets/_Game/Prefabs/Actors/3D/Player_3D.prefab";
		private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player/Player.prefab";
		private const string ShaderName = "Eidren/Actors/WandererVertexLit";

		// GLTFast.AnimationMethod.Legacy == 1 (None=0, Legacy=1, Mecanim=2).
		// Siehe Library/PackageCache/com.unity.cloud.gltfast/Runtime/Scripts/AnimationMethod.cs.
		private const int GltfAnimationMethodLegacy = 1;
		private const string AnimationMethodPropertyPath = "importSettings.animationMethod";

		// W-003: inklusive der drei Werkzeuge — sie kamen am 07.08.2026 in die GLB
		// und blieben sonst nach dem Import dauerhaft aktiv an der Hand.
		private static readonly string[] InitiallyInactiveNodes = new string[18]
		{
			"Helm_Stoff", "Helm_Kupfer", "Helm_Eisen",
			"Harnisch_Stoff", "Harnisch_Kupfer", "Harnisch_Eisen",
			"Beine_Stoff", "Beine_Kupfer", "Beine_Eisen",
			"Haende_Stoff", "Haende_Kupfer", "Haende_Eisen",
			"Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer",
			"Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense"
		};

		[MenuItem("Eidren/V0.2/Build Wanderer3D Player")]
		public static void Build()
		{
			BuildMidPoly();
		}

		[MenuItem("Eidren/Wanderer/Mid-Poly produktiv bauen")]
		public static void BuildMidPoly()
		{
			RestoreMidPolyAsset();
			BuildCurrentAsset("MidPoly");
			Debug.Log("[W3D] Player.prefab auf den produktiven Mid-Poly-Wanderer gebaut.");
		}

		[MenuItem("Eidren/Wanderer/Auf Low-Poly zuruecksetzen")]
		public static void BuildLowPoly()
		{
			RestoreArchivedLowPolyAsset();
			BuildCurrentAsset("LowPoly");
			Debug.Log("[W3D] Player.prefab auf den archivierten Low-Poly-Wanderer zurueckgebaut.");
		}

		private static void BuildCurrentAsset(string productionTier)
		{
			// Vorstufe (Task-1-Befund): glTFast importiert die GLB standardmaessig mit
			// Mecanim/Animator statt Legacy-Clips. MeshActorPresentation und dieser Builder
			// erwarten aber eine Animation-Komponente mit Legacy-Clips. Deshalb wird die
			// Importeinstellung hier als allererster Schritt erzwungen (idempotent), bevor
			// irgendetwas vom Rig instanziert wird.
			EnsureLegacyAnimationImport();

			Material material = EnsureMaterial();
			BuildVisualPrefab(material);
			RewirePlayerPrefab();
			LabelProductionAssets(productionTier);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private static void RestoreArchivedLowPolyAsset()
		{
			if (!File.Exists(ArchivedGlbPath) || !File.Exists(ArchivedGlbPath + ".meta"))
			{
				throw new FileNotFoundException("Archivierter Low-Poly-Wanderer fehlt.", ArchivedGlbPath);
			}
			Directory.CreateDirectory(Path.GetDirectoryName(GlbPath));
			CopyIfDifferent(ArchivedGlbPath, GlbPath);
			CopyIfDifferent(ArchivedGlbPath + ".meta", GlbPath + ".meta");
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		}

		private static void RestoreMidPolyAsset()
		{
			if (!File.Exists(MidPolyGlbPath))
			{
				throw new FileNotFoundException("Exportierter Mid-Poly-Wanderer fehlt.", MidPolyGlbPath);
			}
			Directory.CreateDirectory(Path.GetDirectoryName(GlbPath));
			CopyIfDifferent(MidPolyGlbPath, GlbPath);
			// Die produktive .meta bleibt absichtlich unangetastet: ihre GUID haelt
			// alle bestehenden Prefab-Referenzen stabil.
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		}

		private static void CopyIfDifferent(string source, string destination)
		{
			if (File.Exists(destination)
				&& new FileInfo(source).Length == new FileInfo(destination).Length
				&& File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(destination)))
			{
				return;
			}
			File.Copy(source, destination, true);
		}

		private static void LabelProductionAssets(string productionTier)
		{
			foreach (string path in new[] { GlbPath, VisualPrefabPath, PlayerPrefabPath })
			{
				UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
				if (asset != null)
				{
					AssetDatabase.SetLabels(asset, new[] { "Approved", productionTier, "WandererProduction" });
				}
			}
		}

		/// <summary>
		/// Eigenstaendiger Vorflug-Schritt fuer die Batch-Verifikation (w3d-t6-legacy.log):
		/// erzwingt den Legacy-Import und loggt danach ueber die bestehende Diagnose
		/// (Wanderer3DImportDiagnose.Run), ob Clips legacy=True sind und eine
		/// Animation-Komponente vorhanden ist. Build() ruft EnsureLegacyAnimationImport()
		/// beim eigentlichen Lauf ohnehin selbst (idempotent) - diese Methode dient nur
		/// dazu, den Schritt isoliert und mit Beleg nachweisen zu koennen.
		/// </summary>
		[MenuItem("Eidren/V0.2/Wanderer3D Legacy Import erzwingen + Diagnose")]
		public static void EnsureLegacyAnimationImportAndDiagnose()
		{
			EnsureLegacyAnimationImport();
			Wanderer3DImportDiagnose.Run();
		}

		private static void EnsureLegacyAnimationImport()
		{
			AssetImporter importer = AssetImporter.GetAtPath(GlbPath);
			if (importer == null)
			{
				throw new InvalidOperationException("GLB-Importer nicht gefunden: " + GlbPath);
			}
			var serialized = new SerializedObject(importer);
			SerializedProperty animationMethod = serialized.FindProperty(AnimationMethodPropertyPath);
			if (animationMethod == null)
			{
				// Property nicht am erwarteten Pfad gefunden - nicht raten, sondern alle
				// Properties des Importers loggen, damit der tatsaechliche Pfad ersichtlich ist.
				LogAllSerializedProperties(serialized);
				throw new InvalidOperationException(
					"Property '" + AnimationMethodPropertyPath + "' nicht am GLB-Importer gefunden. " +
					"Siehe Log fuer die tatsaechlich vorhandenen Property-Pfade.");
			}
			if (animationMethod.intValue == GltfAnimationMethodLegacy)
			{
				return;
			}
			animationMethod.intValue = GltfAnimationMethodLegacy;
			serialized.ApplyModifiedProperties();
			importer.SaveAndReimport();
		}

		private static void LogAllSerializedProperties(SerializedObject serialized)
		{
			var builder = new StringBuilder();
			builder.AppendLine("[W3D] Properties des GLB-Importers:");
			SerializedProperty iterator = serialized.GetIterator();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren))
			{
				builder.AppendLine("[W3D]  " + iterator.propertyPath);
				enterChildren = false;
			}
			Debug.Log(builder.ToString());
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

		private static void BuildVisualPrefab(Material material)
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
			if (model == null)
			{
				throw new InvalidOperationException("GLB nicht importiert: " + GlbPath);
			}
			System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs/Actors/3D");
			var root = new GameObject("Player_3D");
			try
			{
				GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(model);
				rig.name = "Wanderer";
				rig.transform.SetParent(root.transform, worldPositionStays: false);

				// Renderer statt nur SkinnedMeshRenderer: die spaeter ergaenzten
				// Werkzeuge (Axt, Spitzhacke, Sense) haengen als STATISCHE
				// MeshRenderer an Handknochen und behielten deshalb den
				// glTF-Importshader (Shader Graphs/glTF-pbrMetallicRoughness) —
				// PlayerPrefabTests meldete das an Waffe_Axt.
				foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>(true))
				{
					renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
					renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
					renderer.receiveShadows = true;
				}

				foreach (string nodeName in InitiallyInactiveNodes)
				{
					Transform node = FindDeep(rig.transform, nodeName);
					if (node == null)
					{
						throw new InvalidOperationException("Knoten fehlt im Rig: " + nodeName);
					}
					node.gameObject.SetActive(false);
				}

				Animation animationPlayer = rig.GetComponentInChildren<Animation>(true);
				if (animationPlayer == null)
				{
					animationPlayer = rig.AddComponent<Animation>();
				}
				animationPlayer.playAutomatically = false;
				var clips = AssetDatabase.LoadAllAssetsAtPath(GlbPath).OfType<AnimationClip>()
					.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
				foreach (AnimationClip clip in clips)
				{
					if (animationPlayer.GetClip(clip.name) == null)
					{
						animationPlayer.AddClip(clip, clip.name);
					}
				}

				MeshActorPresentation presentation = root.AddComponent<MeshActorPresentation>();
				presentation.Configure(animationPlayer, rig.transform, 1.8f);

				PrefabUtility.SaveAsPrefabAsset(root, VisualPrefabPath);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		private static void RewirePlayerPrefab()
		{
			GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
			try
			{
				Transform art = root.transform.Find("Player_Visual");
				if (art == null)
				{
					throw new InvalidOperationException("Player prefab has no Player_Visual.");
				}
				for (int index = art.childCount - 1; index >= 0; index--)
				{
					UnityEngine.Object.DestroyImmediate(art.GetChild(index).gameObject);
				}
				GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
				var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
				visual.transform.SetParent(art, worldPositionStays: false);

				Transform driver = root.transform.Find("WeaponDriver");
				if (driver == null)
				{
					throw new InvalidOperationException("Player prefab has no WeaponDriver.");
				}
				for (int index = driver.childCount - 1; index >= 0; index--)
				{
					UnityEngine.Object.DestroyImmediate(driver.GetChild(index).gameObject);
				}
				PlayerWeaponVisual oldWeapon = driver.GetComponent<PlayerWeaponVisual>();
				if (oldWeapon != null)
				{
					UnityEngine.Object.DestroyImmediate(oldWeapon);
				}
				if (driver.GetComponent<WandererEquipmentVisual>() == null)
				{
					driver.gameObject.AddComponent<WandererEquipmentVisual>();
				}

				MeshActorPresentation mesh = visual.GetComponentInChildren<MeshActorPresentation>(true);
				PlayerPrefabBindings bindings = root.GetComponent<PlayerPrefabBindings>();
				var serialized = new SerializedObject(bindings);
				serialized.FindProperty("playerVisual").objectReferenceValue = mesh;
				serialized.ApplyModifiedPropertiesWithoutUndo();

				EditorUtility.SetDirty(root);
				PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		private static Transform FindDeep(Transform root, string childName)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
			{
				if (child.name == childName)
				{
					return child;
				}
			}
			return null;
		}
	}
}
