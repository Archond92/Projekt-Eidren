using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eidren.Tests.EditMode
{
	public sealed class PlaceholderCleanupTests
	{
		private const string GenericMaterialPath = "Assets/_Game/Resources/EidrenRuntimeMaterial.mat";

		private static readonly string[] ShippedScenes = new string[9]
		{
			"Assets/_Game/Scenes/HomeBase.unity",
			"Assets/_Game/Scenes/Zone_Greenwood.unity",
			"Assets/_Game/Scenes/Zone_Marsh.unity",
			"Assets/_Game/Scenes/Zone_Quarry.unity",
			"Assets/_Game/Scenes/Zone_EmberRuins.unity",
			"Assets/_Game/Scenes/Zone_TwilightGrove.unity",
			"Assets/_Game/Scenes/Zone_VeilMarsh.unity",
			"Assets/_Game/Scenes/Zone_GreyRifts.unity",
			"Assets/_Game/Scenes/EidraForge.unity"
		};

		/// <summary>
		/// Vollstaendige Liste der alten Testhindernisse — die bisherige Pruefung
		/// deckte nur einzelne Vertreter ab.
		/// </summary>
		private static readonly string[] LegacyPlaceholders = new string[13]
		{
			"StyleProofReferenceArea", "Ground_StyleLayer", "GroundTransition_West", "GroundTransition_North",
			"ReferencePath", "SafeArea", "LargeTree_Trunk", "LargeRock_Shelf", "MassiveDeadTree",
			"MassiveObstacle", "LargeObstacle_West", "LargeObstacle_East", "LargeRock_East"
		};

		private static readonly string[] LegacyRuinPlaceholders = new string[3]
		{
			"RuinsWall_West", "RuinsPillar_NorthEast", "RuinsWall_SouthEast"
		};

		[Test]
		public void KeineAusgelieferteSzene_EnthaeltEinAltesTesthindernis()
		{
			List<string> found = new List<string>();
			foreach (string scenePath in ShippedScenes)
			{
				foreach (GameObject root in OpenRoots(scenePath))
				{
					foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
					{
						if (System.Array.IndexOf(LegacyPlaceholders, child.name) >= 0 || System.Array.IndexOf(LegacyRuinPlaceholders, child.name) >= 0)
						{
							found.Add(Path.GetFileNameWithoutExtension(scenePath) + ": " + child.name);
						}
					}
				}
			}
			Assert.That(found, Is.Empty, "Alte Testhindernisse in ausgelieferten Szenen: " + string.Join(", ", found));
		}

		[Test]
		public void KeinSichtbaresWeltobjekt_NutztDasGenerischeMaterial()
		{
			Material generic = AssetDatabase.LoadAssetAtPath<Material>(GenericMaterialPath);
			Assert.That(generic, Is.Not.Null, "Generisches Material fehlt: " + GenericMaterialPath);
			List<string> found = new List<string>();
			foreach (string scenePath in ShippedScenes)
			{
				foreach (GameObject root in OpenRoots(scenePath))
				{
					foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
					{
						if (renderer.enabled && System.Array.IndexOf(renderer.sharedMaterials, generic) >= 0)
						{
							found.Add(Path.GetFileNameWithoutExtension(scenePath) + ": " + renderer.name);
						}
					}
				}
			}
			Assert.That(found, Is.Empty,
				"Sichtbare Weltobjekte mit generischem Platzhaltermaterial: " + string.Join(", ", found));
		}

		/// <summary>
		/// Der Szenenbauer darf die alten Testkoerper nicht mehr erzeugen können,
		/// sonst bringt ein Neuaufbau sie in bereits bereinigte Szenen zurueck.
		/// </summary>
		[Test]
		public void SzenenbauerErzeugt_KeineGrauenTesthindernisseMehr()
		{
			string builder = File.ReadAllText("Assets/_Game/Editor/EidrenSceneStructureBuilder.cs");
			Assert.That(builder, Does.Not.Contain("CreateMassiveTestObstacles"),
				"Der Szenenbauer erzeugt weiterhin graue Testhindernisse; ein Neuaufbau macht die Bereinigung rueckgaengig");
			foreach (string name in LegacyPlaceholders)
			{
				if (name == "SafeArea")
				{
					continue;
				}
				Assert.That(builder, Does.Not.Contain("\"" + name + "\""),
					"Der Szenenbauer kennt weiterhin den Platzhalternamen " + name);
			}
		}

		private static GameObject[] OpenRoots(string scenePath)
		{
			Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
			Assert.That(scene.IsValid(), Is.True, "Szene laesst sich nicht oeffnen: " + scenePath);
			return scene.GetRootGameObjects();
		}
	}
}
