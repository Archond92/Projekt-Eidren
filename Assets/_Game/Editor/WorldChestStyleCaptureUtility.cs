using Eidren.Core.BuildGrid;
using Eidren.Interaction;
using Eidren.Presentation;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Rendert jede Weltkisten-Familie in allen vier Sichtzustaenden aus einer
	/// festen Kameraposition in PNGs. Positionsgleiche Vorher/Nachher-Paare
	/// sind die Bildabnahme der Stilumbau-Etappe 1.
	/// </summary>
	public static class WorldChestStyleCaptureUtility
	{
		private static readonly string[] Familien = { "WorldChest_Common", "WorldChest_Guarded", "WorldChest_Hidden" };

		private static readonly WorldChestVisualState[] Zustaende =
		{
			WorldChestVisualState.Closed, WorldChestVisualState.Opened,
			WorldChestVisualState.PartiallyEmptied, WorldChestVisualState.Emptied
		};

		[MenuItem("Eidren/V0.2/Stilumbau/Weltkisten Vorher-Captures")]
		public static void CaptureVorher()
		{
			CaptureAll("TempReview/StilumbauE1/vorher");
		}

		[MenuItem("Eidren/V0.2/Stilumbau/Weltkisten Nachher-Captures")]
		public static void CaptureNachher()
		{
			CaptureAll("TempReview/StilumbauE1/nachher");
		}

		public static void CaptureAll(string ordner)
		{
			Directory.CreateDirectory(ordner);
			foreach (string familie in Familien)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Loot/WorldChests/" + familie + ".prefab");
				if (prefab == null)
				{
					Debug.LogError("[StilE1] Prefab fehlt: " + familie);
					continue;
				}
				GameObject instanz = Object.Instantiate(prefab);
				GameObject lichtObjekt = new GameObject("CaptureLicht");
				Light licht = lichtObjekt.AddComponent<Light>();
				licht.type = LightType.Directional;
				licht.intensity = 1.1f;
				lichtObjekt.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
				GameObject kameraObjekt = new GameObject("CaptureKamera");
				Camera kamera = kameraObjekt.AddComponent<Camera>();
				kamera.clearFlags = CameraClearFlags.SolidColor;
				kamera.backgroundColor = new Color(0.16f, 0.17f, 0.18f);
				kameraObjekt.transform.position = new Vector3(1.6f, 1.5f, 2.1f);
				kameraObjekt.transform.LookAt(new Vector3(0f, 0.35f, 0f));
				WorldChestVisual visual = instanz.GetComponent<WorldChestVisual>();
				try
				{
					foreach (WorldChestVisualState zustand in Zustaende)
					{
						visual.Apply(zustand);
						RenderTexture ziel = new RenderTexture(768, 768, 24);
						kamera.targetTexture = ziel;
						kamera.Render();
						RenderTexture.active = ziel;
						Texture2D bild = new Texture2D(768, 768, TextureFormat.RGB24, mipChain: false);
						bild.ReadPixels(new Rect(0f, 0f, 768f, 768f), 0, 0);
						bild.Apply();
						string pfad = Path.Combine(ordner, $"{familie}_{zustand}.png");
						File.WriteAllBytes(pfad, bild.EncodeToPNG());
						RenderTexture.active = null;
						kamera.targetTexture = null;
						Object.DestroyImmediate(ziel);
						Object.DestroyImmediate(bild);
						Debug.Log("[StilE1] geschrieben: " + pfad);
					}
				}
				finally
				{
					Object.DestroyImmediate(instanz);
					Object.DestroyImmediate(kameraObjekt);
					Object.DestroyImmediate(lichtObjekt);
				}
			}
		}

		[MenuItem("Eidren/V0.2/Stilumbau/Weltkisten In-Welt-Captures")]
		public static void CaptureInWelt()
		{
			const string ordner = "TempReview/StilumbauE1/inwelt";
			Directory.CreateDirectory(ordner);
			UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_Greenwood.unity");
			/* Spawnpunkte aus WorldChestContentBuilder.WritePoints: common_01, guarded_01, hidden_01 */
			(string familie, Vector3 position, float drehung)[] plaetze =
			{
				("WorldChest_Common", new Vector3(-26f, 0.12f, -18f), 24f),
				("WorldChest_Guarded", new Vector3(1f, 0.12f, 10f), 180f),
				("WorldChest_Hidden", new Vector3(-30f, 0.12f, 26f), 122f)
			};
			foreach ((string familie, Vector3 position, float drehung) platz in plaetze)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Loot/WorldChests/" + platz.familie + ".prefab");
				GameObject instanz = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
				instanz.transform.SetPositionAndRotation(platz.position, Quaternion.Euler(0f, platz.drehung, 0f));
				GameObject kameraObjekt = new GameObject("InWeltKamera");
				Camera kamera = kameraObjekt.AddComponent<Camera>();
				/* Spielnahe Dreiviertelsicht: schraeg von oben, Blick auf die Kiste */
				kameraObjekt.transform.position = platz.position + new Vector3(3.2f, 4.6f, -4.2f);
				kameraObjekt.transform.LookAt(platz.position + Vector3.up * 0.4f);
				WorldChestVisual visual = instanz.GetComponent<WorldChestVisual>();
				try
				{
					foreach (WorldChestVisualState zustand in new[] { WorldChestVisualState.Closed, WorldChestVisualState.Opened })
					{
						visual.Apply(zustand);
						RenderTexture ziel = new RenderTexture(1280, 720, 24);
						kamera.targetTexture = ziel;
						kamera.Render();
						RenderTexture.active = ziel;
						Texture2D bild = new Texture2D(1280, 720, TextureFormat.RGB24, mipChain: false);
						bild.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
						bild.Apply();
						File.WriteAllBytes(Path.Combine(ordner, $"{platz.familie}_{zustand}.png"), bild.EncodeToPNG());
						RenderTexture.active = null;
						kamera.targetTexture = null;
						Object.DestroyImmediate(ziel);
						Object.DestroyImmediate(bild);
						Debug.Log("[StilE1] In-Welt geschrieben: " + platz.familie + "_" + zustand);
					}
				}
				finally
				{
					Object.DestroyImmediate(kameraObjekt);
					Object.DestroyImmediate(instanz);
				}
			}
		}

		private static readonly (string pfad, string name, float zielHoehe)[] Stationen =
		{
			("Assets/_Game/Prefabs/Stations/StorageChest.prefab", "StorageChest", 0.8f),
			("Assets/_Game/Prefabs/Stations/Workbench.prefab", "Workbench", 0.9f),
			("Assets/_Game/Resources/Prefabs/DeathBag.prefab", "DeathBag", 0.4f)
		};

		[MenuItem("Eidren/V0.2/Stilumbau/Stationen Vorher-Captures")]
		public static void CaptureStationenVorher()
		{
			CaptureStationen("TempReview/StilumbauE2/stationen-vorher");
		}

		[MenuItem("Eidren/V0.2/Stilumbau/Stationen Nachher-Captures")]
		public static void CaptureStationenNachher()
		{
			CaptureStationen("TempReview/StilumbauE2/stationen-nachher");
		}

		public static void CaptureStationen(string ordner)
		{
			(string pfad, string name, float zielHoehe, float distanz)[] liste = new (string, string, float, float)[Stationen.Length];
			for (int i = 0; i < Stationen.Length; i++)
			{
				/* Stationen nutzten bisher eine feste Kameraposition (2.4, 2.2, 3.1); das entspricht distanz=3.1 in der neuen Formel. */
				liste[i] = (Stationen[i].pfad, Stationen[i].name, Stationen[i].zielHoehe, 3.1f);
			}
			CapturePrefabListe(ordner, liste, "[StilE2]");
		}

		/// <summary>
		/// Verallgemeinertes Capture-Muster (Studio-Licht/RT wie bisher): rendert jeden
		/// Prefab-Eintrag aus einer distanzabhaengigen Kameraposition in ein PNG.
		/// </summary>
		public static void CapturePrefabListe(string ordner, (string pfad, string name, float zielHoehe, float distanz)[] liste)
		{
			CapturePrefabListe(ordner, liste, "[StilE3]");
		}

		private static void CapturePrefabListe(string ordner, (string pfad, string name, float zielHoehe, float distanz)[] liste, string logPraefix)
		{
			Directory.CreateDirectory(ordner);
			foreach ((string pfad, string name, float zielHoehe, float distanz) eintrag in liste)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(eintrag.pfad);
				if (prefab == null)
				{
					Debug.LogError(logPraefix + " Prefab fehlt: " + eintrag.pfad);
					continue;
				}
				GameObject instanz = Object.Instantiate(prefab);
				GameObject lichtObjekt = new GameObject("CaptureLicht");
				Light licht = lichtObjekt.AddComponent<Light>();
				licht.type = LightType.Directional;
				licht.intensity = 1.1f;
				lichtObjekt.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
				GameObject kameraObjekt = new GameObject("CaptureKamera");
				Camera kamera = kameraObjekt.AddComponent<Camera>();
				kamera.clearFlags = CameraClearFlags.SolidColor;
				kamera.backgroundColor = new Color(0.16f, 0.17f, 0.18f);
				kameraObjekt.transform.position = new Vector3(eintrag.distanz * 0.77f, eintrag.distanz * 0.71f, eintrag.distanz);
				kameraObjekt.transform.LookAt(new Vector3(0f, eintrag.zielHoehe, 0f));
				try
				{
					RenderTexture ziel = new RenderTexture(768, 768, 24);
					kamera.targetTexture = ziel;
					kamera.Render();
					RenderTexture.active = ziel;
					Texture2D bild = new Texture2D(768, 768, TextureFormat.RGB24, mipChain: false);
					bild.ReadPixels(new Rect(0f, 0f, 768f, 768f), 0, 0);
					bild.Apply();
					File.WriteAllBytes(Path.Combine(ordner, eintrag.name + ".png"), bild.EncodeToPNG());
					RenderTexture.active = null;
					kamera.targetTexture = null;
					Object.DestroyImmediate(ziel);
					Object.DestroyImmediate(bild);
					Debug.Log(logPraefix + " geschrieben: " + eintrag.name);
				}
				finally
				{
					Object.DestroyImmediate(instanz);
					Object.DestroyImmediate(kameraObjekt);
					Object.DestroyImmediate(lichtObjekt);
				}
			}
		}

		/* Die 8 Tier-2-Basis-Visuals und ihre 12 Zonenvarianten (gleiche zielHoehe/distanz wie ihr Basistyp). */
		private static readonly (string pfad, string name, float zielHoehe, float distanz)[] T2Ressourcen =
		{
			("Assets/_Game/Prefabs/Resources/Visuals/HardwoodTree_Active.prefab", "HardwoodTree_Active", 3.0f, 7f),
			("Assets/_Game/Prefabs/Resources/Visuals/HardwoodTree_Exhausted.prefab", "HardwoodTree_Exhausted", 0.5f, 3f),
			("Assets/_Game/Prefabs/Resources/Visuals/SwampHemp_Active.prefab", "SwampHemp_Active", 0.6f, 2.5f),
			("Assets/_Game/Prefabs/Resources/Visuals/SwampHemp_Exhausted.prefab", "SwampHemp_Exhausted", 0.3f, 2f),
			("Assets/_Game/Prefabs/Resources/Visuals/GraniteDeposit_Active.prefab", "GraniteDeposit_Active", 0.6f, 3f),
			("Assets/_Game/Prefabs/Resources/Visuals/GraniteDeposit_Exhausted.prefab", "GraniteDeposit_Exhausted", 0.3f, 2.5f),
			("Assets/_Game/Prefabs/Resources/Visuals/IronVein_Active.prefab", "IronVein_Active", 0.7f, 3f),
			("Assets/_Game/Prefabs/Resources/Visuals/IronVein_Exhausted.prefab", "IronVein_Exhausted", 0.3f, 2.5f),

			("Assets/_Game/Prefabs/Environment/AreaArtVariants/TwilightGrove_hardwood_tree_Active.prefab", "TwilightGrove_hardwood_tree_Active", 3.0f, 7f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/TwilightGrove_hardwood_tree_Exhausted.prefab", "TwilightGrove_hardwood_tree_Exhausted", 0.5f, 3f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/TwilightGrove_swamp_hemp_Active.prefab", "TwilightGrove_swamp_hemp_Active", 0.6f, 2.5f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/TwilightGrove_swamp_hemp_Exhausted.prefab", "TwilightGrove_swamp_hemp_Exhausted", 0.3f, 2f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/VeilMarsh_swamp_hemp_Active.prefab", "VeilMarsh_swamp_hemp_Active", 0.6f, 2.5f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/VeilMarsh_swamp_hemp_Exhausted.prefab", "VeilMarsh_swamp_hemp_Exhausted", 0.3f, 2f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/VeilMarsh_iron_vein_Active.prefab", "VeilMarsh_iron_vein_Active", 0.7f, 3f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/VeilMarsh_iron_vein_Exhausted.prefab", "VeilMarsh_iron_vein_Exhausted", 0.3f, 2.5f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/GreyRifts_granite_deposit_Active.prefab", "GreyRifts_granite_deposit_Active", 0.6f, 3f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/GreyRifts_granite_deposit_Exhausted.prefab", "GreyRifts_granite_deposit_Exhausted", 0.3f, 2.5f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/GreyRifts_iron_vein_Active.prefab", "GreyRifts_iron_vein_Active", 0.7f, 3f),
			("Assets/_Game/Prefabs/Environment/AreaArtVariants/GreyRifts_iron_vein_Exhausted.prefab", "GreyRifts_iron_vein_Exhausted", 0.3f, 2.5f)
		};

		[MenuItem("Eidren/V0.2/Stilumbau/T2-Ressourcen Vorher-Captures")]
		public static void CaptureT2RessourcenVorher()
		{
			CapturePrefabListe("TempReview/StilumbauE3/vorher", T2Ressourcen);
		}

		[MenuItem("Eidren/V0.2/Stilumbau/T2-Ressourcen Nachher-Captures")]
		public static void CaptureT2RessourcenNachher()
		{
			CapturePrefabListe("TempReview/StilumbauE3/nachher", T2Ressourcen);
		}

		[MenuItem("Eidren/V0.2/Stilumbau/T2-Ressourcen In-Welt-Stichprobe")]
		public static void CaptureT2InWelt()
		{
			CaptureT2InWelt("TempReview/StilumbauE3/inwelt");
		}

		/// <summary>
		/// Abnahme-Sammellauf fuer Task 5 (Auflage des Auftraggebers: Zeitersparnis durch EINEN
		/// gemeinsamen Unity-Start statt zwei getrennter Aufrufe): rendert nacheinander die 20
		/// Nachher-Studio-Captures der T2-Ressourcen und die In-Welt-Stichprobe (Hartholzbaum +
		/// Sumpfhanf). Beide Teilschritte bleiben inhaltlich unveraendert (reine Delegation an
		/// <see cref="CaptureT2RessourcenNachher"/> und <see cref="CaptureT2InWelt()"/>); nur der
		/// Prozessstart wird gebuendelt.
		/// </summary>
		[MenuItem("Eidren/V0.2/Stilumbau/T2 Abnahme-Captures")]
		public static void CaptureT2Abnahme()
		{
			CaptureT2RessourcenNachher();
			CaptureT2InWelt();
		}

		[MenuItem("Eidren/V0.2/Stilumbau/T1 Uebersichts-Captures")]
		public static void CaptureT1Uebersicht()
		{
			/* Handgebaute Tier-1-Visuals — reine Sichtungsbilder fuer die Stilentscheidung. */
			(string pfad, string name, float zielHoehe, float distanz)[] liste =
			{
				("Assets/_Game/Prefabs/Resources/Visuals/Tree_Active.prefab", "Tree_Active", 2.2f, 6f),
				("Assets/_Game/Prefabs/Resources/Visuals/Tree_Exhausted.prefab", "Tree_Exhausted", 0.4f, 2.5f),
				("Assets/_Game/Prefabs/Resources/Visuals/StoneDeposit_Active.prefab", "StoneDeposit_Active", 0.6f, 3f),
				("Assets/_Game/Prefabs/Resources/Visuals/StoneDeposit_Exhausted.prefab", "StoneDeposit_Exhausted", 0.3f, 2.5f),
				("Assets/_Game/Prefabs/Resources/Visuals/FiberPlant_Active.prefab", "FiberPlant_Active", 0.5f, 2.2f),
				("Assets/_Game/Prefabs/Resources/Visuals/FiberPlant_Exhausted.prefab", "FiberPlant_Exhausted", 0.25f, 2f),
				("Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Active.prefab", "CopperVein_Active", 0.7f, 3f),
				("Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Exhausted.prefab", "CopperVein_Exhausted", 0.3f, 2.5f),
				("Assets/_Game/Prefabs/Resources/Visuals/BerryBush_Active.prefab", "BerryBush_Active", 0.5f, 2.2f),
				("Assets/_Game/Prefabs/Resources/Visuals/BerryBush_Exhausted.prefab", "BerryBush_Exhausted", 0.3f, 2f)
			};
			CapturePrefabListe("TempReview/StilumbauE3/t1-uebersicht", liste);
		}

		/// <summary>
		/// In-Welt-Stichprobe fuer die T2-Ressourcenknoten (analog <see cref="CaptureInWelt"/>):
		/// oeffnet Zone_TwilightGrove und sucht per <see cref="ResourceNode"/>-Namensvergleich je ein
		/// Hartholzbaum- und ein Sumpfhanf-Exemplar. Ressourcenknoten werden laut
		/// STILUMBAU_E3_BAKEKETTE.md Abschnitt (c) nie in Szenen gebacken, sondern erst zur Laufzeit von
		/// ZoneResourcePopulator.Populate() instanziiert -- im Editor-Modus (kein Play-Modus) ist die Szene
		/// daher voraussichtlich leer; in diesem Fall werden die beiden Node-Prefabs temporaer an einer
		/// freien Stelle eingebettet, gerendert und danach wieder entfernt. Die Szene wird nicht gespeichert.
		/// </summary>
		public static void CaptureT2InWelt(string ordner)
		{
			Directory.CreateDirectory(ordner);
			UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_TwilightGrove.unity");
			ResourceNode[] vorhandeneKnoten = Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
			ResourceNode hartholz = null;
			ResourceNode sumpfhanf = null;
			foreach (ResourceNode knoten in vorhandeneKnoten)
			{
				string name = knoten.gameObject.name;
				if (hartholz == null && (name.Contains("HardwoodTree") || name.Contains("Hartholzbaum")))
				{
					hartholz = knoten;
				}
				if (sumpfhanf == null && (name.Contains("SwampHemp") || name.Contains("Sumpfhanf")))
				{
					sumpfhanf = knoten;
				}
			}

			List<GameObject> temporaereInstanzen = new List<GameObject>();
			bool ausPrefabInstanziiert = (hartholz == null || sumpfhanf == null);
			if (hartholz == null)
			{
				hartholz = InstanziereKnotenTemporaer("Assets/_Game/Prefabs/Resources/Nodes/HardwoodTree.prefab", Vector3.zero, temporaereInstanzen);
			}
			if (sumpfhanf == null)
			{
				sumpfhanf = InstanziereKnotenTemporaer("Assets/_Game/Prefabs/Resources/Nodes/SwampHemp.prefab", new Vector3(6f, 0f, 0f), temporaereInstanzen);
			}
			Debug.Log("[StilE3] In-Welt-Stichprobe-Pfad: " + (ausPrefabInstanziiert ? "temporaere Node-Prefab-Instanziierung (keine Laufzeit-Knoten in der Szene gefunden)" : "vorhandene Szenen-Knoten"));

			try
			{
				if (hartholz != null)
				{
					CaptureT2InWeltKnoten(hartholz, "HardwoodTree", ordner);
				}
				else
				{
					Debug.LogError("[StilE3] Kein Hartholzbaum-Knoten gefunden und Prefab konnte nicht instanziiert werden.");
				}
				if (sumpfhanf != null)
				{
					CaptureT2InWeltKnoten(sumpfhanf, "SwampHemp", ordner);
				}
				else
				{
					Debug.LogError("[StilE3] Kein Sumpfhanf-Knoten gefunden und Prefab konnte nicht instanziiert werden.");
				}
			}
			finally
			{
				foreach (GameObject instanz in temporaereInstanzen)
				{
					Object.DestroyImmediate(instanz);
				}
			}
			/* Szene bewusst nicht gespeichert -- reine Lese-/Stichprobenaufnahme. */
		}

		private static ResourceNode InstanziereKnotenTemporaer(string prefabPfad, Vector3 position, List<GameObject> sammlung)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPfad);
			if (prefab == null)
			{
				Debug.LogError("[StilE3] Node-Prefab fehlt: " + prefabPfad);
				return null;
			}
			GameObject instanz = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			instanz.transform.position = position;
			sammlung.Add(instanz);
			return instanz.GetComponent<ResourceNode>();
		}

		private static void CaptureT2InWeltKnoten(ResourceNode knoten, string dateiname, string ordner)
		{
			Vector3 fuss = knoten.transform.position;
			GameObject kameraObjekt = new GameObject("T2InWeltKamera");
			Camera kamera = kameraObjekt.AddComponent<Camera>();
			/* Gleicher Kameraversatz wie CaptureInWelt: spielnahe Dreiviertelsicht schraeg von oben. */
			kameraObjekt.transform.position = fuss + new Vector3(3.2f, 4.6f, -4.2f);
			kameraObjekt.transform.LookAt(fuss + Vector3.up * 0.4f);
			try
			{
				RenderTexture ziel = new RenderTexture(1280, 720, 24);
				kamera.targetTexture = ziel;
				kamera.Render();
				RenderTexture.active = ziel;
				Texture2D bild = new Texture2D(1280, 720, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
				bild.Apply();
				string pfad = Path.Combine(ordner, dateiname + ".png");
				File.WriteAllBytes(pfad, bild.EncodeToPNG());
				RenderTexture.active = null;
				kamera.targetTexture = null;
				Object.DestroyImmediate(ziel);
				Object.DestroyImmediate(bild);
				Debug.Log("[StilE3] In-Welt geschrieben: " + pfad);
			}
			finally
			{
				Object.DestroyImmediate(kameraObjekt);
			}
		}

		[MenuItem("Eidren/V0.2/Stilumbau/T1-Varianten Vorher-Captures")]
		public static void CaptureT1VariantenVorher()
		{
			/* Die 12 Tier-1-Zonenvarianten (4 Specs × 3 Ressourcentypen mit Active/Exhausted-Paaren;
			   EmberRuins_copper_vein entfaellt, da dieser Sonderfall nur das Basis-Prefab ladet).
			   Kamera-Parameter (zielHoehe/distanz) nach Basistyp: Tree variants wie Tree (2.2/6 active, 0.4/2.5 exhausted),
			   FiberPlant variants wie FiberPlant (0.5/2.2 active, 0.25/2 exhausted), StoneDeposit variants wie StoneDeposit (0.6/3 active, 0.3/2.5 exhausted).
			 */
			(string pfad, string name, float zielHoehe, float distanz)[] liste =
			{
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Greenwood_tree_Active.prefab", "Greenwood_tree_Active", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Greenwood_tree_Exhausted.prefab", "Greenwood_tree_Exhausted", 0.4f, 2.5f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Marsh_tree_Active.prefab", "Marsh_tree_Active", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Marsh_tree_Exhausted.prefab", "Marsh_tree_Exhausted", 0.4f, 2.5f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Marsh_fiber_plant_Active.prefab", "Marsh_fiber_plant_Active", 0.5f, 2.2f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Marsh_fiber_plant_Exhausted.prefab", "Marsh_fiber_plant_Exhausted", 0.25f, 2f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Quarry_tree_Active.prefab", "Quarry_tree_Active", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Quarry_tree_Exhausted.prefab", "Quarry_tree_Exhausted", 0.4f, 2.5f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Quarry_stone_deposit_Active.prefab", "Quarry_stone_deposit_Active", 0.6f, 3f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Quarry_stone_deposit_Exhausted.prefab", "Quarry_stone_deposit_Exhausted", 0.3f, 2.5f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/EmberRuins_tree_Active.prefab", "EmberRuins_tree_Active", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/EmberRuins_tree_Exhausted.prefab", "EmberRuins_tree_Exhausted", 0.4f, 2.5f)
			};
			CapturePrefabListe("TempReview/StilumbauE3b/varianten-vorher", liste);
		}

		/* Dieselbe Liste wie CaptureT1Uebersicht -- Nachher-Aufnahme der zehn T1-Basis-Visuals. */
		[MenuItem("Eidren/V0.2/Stilumbau/T1 Uebersicht Nachher-Captures")]
		public static void CaptureT1UebersichtNachher()
		{
			(string pfad, string name, float zielHoehe, float distanz)[] liste =
			{
				("Assets/_Game/Prefabs/Resources/Visuals/Tree_Active.prefab", "Tree_Active", 2.2f, 6f),
				("Assets/_Game/Prefabs/Resources/Visuals/Tree_Exhausted.prefab", "Tree_Exhausted", 0.4f, 2.5f),
				("Assets/_Game/Prefabs/Resources/Visuals/StoneDeposit_Active.prefab", "StoneDeposit_Active", 0.6f, 3f),
				("Assets/_Game/Prefabs/Resources/Visuals/StoneDeposit_Exhausted.prefab", "StoneDeposit_Exhausted", 0.3f, 2.5f),
				("Assets/_Game/Prefabs/Resources/Visuals/FiberPlant_Active.prefab", "FiberPlant_Active", 0.5f, 2.2f),
				("Assets/_Game/Prefabs/Resources/Visuals/FiberPlant_Exhausted.prefab", "FiberPlant_Exhausted", 0.25f, 2f),
				("Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Active.prefab", "CopperVein_Active", 0.7f, 3f),
				("Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Exhausted.prefab", "CopperVein_Exhausted", 0.3f, 2.5f),
				("Assets/_Game/Prefabs/Resources/Visuals/BerryBush_Active.prefab", "BerryBush_Active", 0.5f, 2.2f),
				("Assets/_Game/Prefabs/Resources/Visuals/BerryBush_Exhausted.prefab", "BerryBush_Exhausted", 0.3f, 2f)
			};
			CapturePrefabListe("TempReview/StilumbauE3b/nachher", liste);
		}

		/* Dieselbe Liste wie CaptureT1VariantenVorher -- Nachher-Aufnahme der zwoelf T1-Zonenvarianten. */
		[MenuItem("Eidren/V0.2/Stilumbau/T1-Varianten Nachher-Captures")]
		public static void CaptureT1VariantenNachher()
		{
			(string pfad, string name, float zielHoehe, float distanz)[] liste =
			{
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Greenwood_tree_Active.prefab", "Greenwood_tree_Active", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Greenwood_tree_Exhausted.prefab", "Greenwood_tree_Exhausted", 0.4f, 2.5f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Marsh_tree_Active.prefab", "Marsh_tree_Active", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Marsh_tree_Exhausted.prefab", "Marsh_tree_Exhausted", 0.4f, 2.5f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Marsh_fiber_plant_Active.prefab", "Marsh_fiber_plant_Active", 0.5f, 2.2f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Marsh_fiber_plant_Exhausted.prefab", "Marsh_fiber_plant_Exhausted", 0.25f, 2f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Quarry_tree_Active.prefab", "Quarry_tree_Active", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Quarry_tree_Exhausted.prefab", "Quarry_tree_Exhausted", 0.4f, 2.5f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Quarry_stone_deposit_Active.prefab", "Quarry_stone_deposit_Active", 0.6f, 3f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/Quarry_stone_deposit_Exhausted.prefab", "Quarry_stone_deposit_Exhausted", 0.3f, 2.5f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/EmberRuins_tree_Active.prefab", "EmberRuins_tree_Active", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/AreaArtVariants/EmberRuins_tree_Exhausted.prefab", "EmberRuins_tree_Exhausted", 0.4f, 2.5f)
			};
			CapturePrefabListe("TempReview/StilumbauE3b/varianten-nachher", liste);
		}

		/// <summary>
		/// In-Welt-Stichprobe fuer die T1-Ressourcenknoten Tree/BerryBush (Task 5): oeffnet
		/// Zone_Greenwood, instanziiert die beiden Node-Prefabs temporaer bei (0,0,0) bzw. (6,0,0),
		/// rendert sie mit einem je-Typ-Kameraversatz (hoher Baum braucht mehr Abstand/Hoehe als
		/// der niedrige Beerenstrauch) und entfernt die Instanzen wieder. Die Szene wird nicht
		/// gespeichert.
		/// </summary>
		public static void CaptureT1InWelt(string ordner)
		{
			Directory.CreateDirectory(ordner);
			UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_Greenwood.unity");

			GameObject baumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Resources/Nodes/Tree.prefab");
			GameObject strauchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Resources/Nodes/BerryBush.prefab");
			if (baumPrefab == null || strauchPrefab == null)
			{
				Debug.LogError("[StilE3b] T1-Node-Prefab fehlt: Tree=" + (baumPrefab == null) + " BerryBush=" + (strauchPrefab == null));
				return;
			}

			GameObject baumInstanz = (GameObject)PrefabUtility.InstantiatePrefab(baumPrefab);
			baumInstanz.transform.position = Vector3.zero;
			GameObject strauchInstanz = (GameObject)PrefabUtility.InstantiatePrefab(strauchPrefab);
			strauchInstanz.transform.position = new Vector3(6f, 0f, 0f);

			try
			{
				/* Der hohe Baum (Sollhoehe 6,0) braucht mehr Kameraabstand/-hoehe als der
				   niedrige Strauch, damit die Krone im Bild bleibt. */
				CaptureT1InWeltObjekt(baumInstanz.transform.position, new Vector3(4.5f, 6.5f, -6.0f), "Tree", ordner);
				CaptureT1InWeltObjekt(strauchInstanz.transform.position, new Vector3(3.2f, 4.6f, -4.2f), "BerryBush", ordner);
			}
			finally
			{
				Object.DestroyImmediate(baumInstanz);
				Object.DestroyImmediate(strauchInstanz);
			}
			/* Szene bewusst nicht gespeichert -- reine Lese-/Stichprobenaufnahme. */
		}

		private static void CaptureT1InWeltObjekt(Vector3 fuss, Vector3 kameraVersatz, string dateiname, string ordner)
		{
			GameObject kameraObjekt = new GameObject("T1InWeltKamera");
			Camera kamera = kameraObjekt.AddComponent<Camera>();
			kameraObjekt.transform.position = fuss + kameraVersatz;
			kameraObjekt.transform.LookAt(fuss + Vector3.up * 0.4f);
			try
			{
				RenderTexture ziel = new RenderTexture(1280, 720, 24);
				kamera.targetTexture = ziel;
				kamera.Render();
				RenderTexture.active = ziel;
				Texture2D bild = new Texture2D(1280, 720, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
				bild.Apply();
				string pfad = Path.Combine(ordner, dateiname + ".png");
				File.WriteAllBytes(pfad, bild.EncodeToPNG());
				RenderTexture.active = null;
				kamera.targetTexture = null;
				Object.DestroyImmediate(ziel);
				Object.DestroyImmediate(bild);
				Debug.Log("[StilE3b] In-Welt geschrieben: " + pfad);
			}
			finally
			{
				Object.DestroyImmediate(kameraObjekt);
			}
		}

		/// <summary>
		/// Abnahme-Sammellauf fuer Task 5 (Owner-Auflage aus 3a/3: Zeitersparnis durch EINEN
		/// gemeinsamen Unity-Start statt mehrerer getrennter Aufrufe): rendert nacheinander die
		/// Nachher-Studio-Captures der 10 T1-Basis-Visuals, die Nachher-Studio-Captures der 12
		/// T1-Zonenvarianten und die In-Welt-Stichprobe (Tree + BerryBush). Alle drei Teilschritte
		/// bleiben inhaltlich unveraendert (reine Delegation an <see cref="CaptureT1UebersichtNachher"/>,
		/// <see cref="CaptureT1VariantenNachher"/> und <see cref="CaptureT1InWelt(string)"/>); nur
		/// der Prozessstart wird gebuendelt.
		/// </summary>
		[MenuItem("Eidren/V0.2/Stilumbau/T1 Abnahme-Captures")]
		public static void CaptureT1Abnahme()
		{
			CaptureT1UebersichtNachher();
			CaptureT1VariantenNachher();
			CaptureT1InWelt("TempReview/StilumbauE3b/inwelt");
		}

		[MenuItem("Eidren/V0.2/Stilumbau/Props Uebersichts-Captures")]
		public static void CapturePropsUebersicht()
		{
			/* Die 16 Etappe-4-Stilproben-Props (StyleProof) -- reine Sichtungsbilder fuer die Stilentscheidung.
			   zielHoehe/distanz je nach Objektgroesse geschaetzt (Baeume hoch, Bodendecker sehr niedrig). */
			(string pfad, string name, float zielHoehe, float distanz)[] liste =
			{
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_A.prefab", "SP_Tree_A", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_B.prefab", "SP_Tree_B", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_C.prefab", "SP_Tree_C", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_RuinMonument.prefab", "SP_RuinMonument", 1.5f, 4.5f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_RuinWall_A.prefab", "SP_RuinWall_A", 1.0f, 3.5f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_RuinWall_B.prefab", "SP_RuinWall_B", 1.0f, 3.5f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Large.prefab", "SP_Rock_Large", 0.6f, 3f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Medium.prefab", "SP_Rock_Medium", 0.4f, 2.5f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Small.prefab", "SP_Rock_Small", 0.25f, 2f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Bush.prefab", "SP_Plant_Bush", 0.4f, 2.2f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Fern.prefab", "SP_Plant_Fern", 0.4f, 2.2f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Flowers.prefab", "SP_Plant_Flowers", 0.4f, 2.2f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_GroundCover_Grass.prefab", "SP_GroundCover_Grass", 0.15f, 1.8f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_GroundCover_Moss.prefab", "SP_GroundCover_Moss", 0.15f, 1.8f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Accent_GlowMushrooms.prefab", "SP_Accent_GlowMushrooms", 0.3f, 2f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_EidrenRune.prefab", "SP_EidrenRune", 0.8f, 3f)
			};
			CapturePrefabListe("TempReview/StilumbauE4/uebersicht", liste);
		}

		/* Dieselbe 16-Eintrag-Liste wie CapturePropsUebersicht -- Nachher-Aufnahme der 16 Etappe-4-Props. */
		private static void CapturePropsUebersichtNachher()
		{
			(string pfad, string name, float zielHoehe, float distanz)[] liste =
			{
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_A.prefab", "SP_Tree_A", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_B.prefab", "SP_Tree_B", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_C.prefab", "SP_Tree_C", 2.2f, 6f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_RuinMonument.prefab", "SP_RuinMonument", 1.5f, 4.5f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_RuinWall_A.prefab", "SP_RuinWall_A", 1.0f, 3.5f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_RuinWall_B.prefab", "SP_RuinWall_B", 1.0f, 3.5f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Large.prefab", "SP_Rock_Large", 0.6f, 3f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Medium.prefab", "SP_Rock_Medium", 0.4f, 2.5f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Small.prefab", "SP_Rock_Small", 0.25f, 2f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Bush.prefab", "SP_Plant_Bush", 0.4f, 2.2f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Fern.prefab", "SP_Plant_Fern", 0.4f, 2.2f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Flowers.prefab", "SP_Plant_Flowers", 0.4f, 2.2f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_GroundCover_Grass.prefab", "SP_GroundCover_Grass", 0.15f, 1.8f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_GroundCover_Moss.prefab", "SP_GroundCover_Moss", 0.15f, 1.8f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_Accent_GlowMushrooms.prefab", "SP_Accent_GlowMushrooms", 0.3f, 2f),
				("Assets/_Game/Prefabs/Environment/StyleProof/SP_EidrenRune.prefab", "SP_EidrenRune", 0.8f, 3f)
			};
			CapturePrefabListe("TempReview/StilumbauE4/nachher", liste, "[StilE4]");
		}

		/// <summary>
		/// In-Welt-Stichprobe fuer die Etappe-4-Props (Task 5): oeffnet Zone_Greenwood,
		/// instanziiert SP_Tree_A.prefab bei (0,0,0) und SP_Plant_Bush.prefab bei (6,0,0)
		/// temporaer, rendert sie mit demselben je-Typ-Kameraversatz wie die T1-In-Welt-
		/// Stichprobe (hoher Baum: (4.5, 6.5, -6.0); Strauch: (3.2, 4.6, -4.2)), 1280x720,
		/// und entfernt die Instanzen wieder. Die Szene wird nicht gespeichert.
		/// </summary>
		/// <summary>
		/// Stilumbau Etappe 5, Task 2: hoehenparametrisierter In-Welt-Kameraversatz.
		/// Ersetzt die zuvor je Objekttyp hartkodierten Vector3-Versaetze (z. B. Baum
		/// (4.5, 6.5, -6.0) fest, unabhaengig von der tatsaechlichen Objekthoehe), die bei
		/// hohen Objekten (SP_Tree_A) die Krone abschnitten. Formel aus
		/// STILUMBAU_E5_PLAN.md Task 2: distanz = zielHoehe*1.4 + 2.5; Versatz =
		/// (0.7*distanz, zielHoehe + 2.0, -0.9*distanz). Der zugehoerige LookAt-Punkt
		/// liegt auf halber Objekthoehe (siehe Aufrufer).
		/// </summary>
		private static Vector3 InWeltVersatz(float zielHoehe)
		{
			float distanz = zielHoehe * 1.4f + 2.5f;
			return new Vector3(0.7f * distanz, zielHoehe + 2.0f, -0.9f * distanz);
		}

		public static void CapturePropsInWelt(string ordner)
		{
			Directory.CreateDirectory(ordner);
			UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_Greenwood.unity");

			GameObject baumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_A.prefab");
			GameObject strauchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Bush.prefab");
			if (baumPrefab == null || strauchPrefab == null)
			{
				Debug.LogError("[StilE4] Props-In-Welt-Prefab fehlt: SP_Tree_A=" + (baumPrefab == null) + " SP_Plant_Bush=" + (strauchPrefab == null));
				return;
			}

			GameObject baumInstanz = (GameObject)PrefabUtility.InstantiatePrefab(baumPrefab);
			baumInstanz.transform.position = Vector3.zero;
			GameObject strauchInstanz = (GameObject)PrefabUtility.InstantiatePrefab(strauchPrefab);
			strauchInstanz.transform.position = new Vector3(6f, 0f, 0f);

			try
			{
				/* Stilumbau E5 Task 2: zielHoehe statt fester Vector3-Versaetze -- SP_Tree_A
				   ist deutlich hoeher als die frueher angenommenen 2.2 m (Krone wurde
				   abgeschnitten); 7.5/0.9 sind die im Task verifizierten Rahmungswerte. */
				CapturePropsInWeltObjekt(baumInstanz.transform.position, 7.5f, "SP_Tree_A", ordner);
				CapturePropsInWeltObjekt(strauchInstanz.transform.position, 0.9f, "SP_Plant_Bush", ordner);
			}
			finally
			{
				Object.DestroyImmediate(baumInstanz);
				Object.DestroyImmediate(strauchInstanz);
			}
			/* Szene bewusst nicht gespeichert -- reine Lese-/Stichprobenaufnahme. */
		}

		private static void CapturePropsInWeltObjekt(Vector3 fuss, float zielHoehe, string dateiname, string ordner)
		{
			CapturePropsInWeltObjekt(fuss, zielHoehe, dateiname, ordner, "[StilE4]");
		}

		private static void CapturePropsInWeltObjekt(Vector3 fuss, float zielHoehe, string dateiname, string ordner, string logPraefix)
		{
			GameObject kameraObjekt = new GameObject("PropsInWeltKamera");
			Camera kamera = kameraObjekt.AddComponent<Camera>();
			kameraObjekt.transform.position = fuss + InWeltVersatz(zielHoehe);
			kameraObjekt.transform.LookAt(fuss + Vector3.up * (zielHoehe * 0.5f));
			try
			{
				RenderTexture ziel = new RenderTexture(1280, 720, 24);
				kamera.targetTexture = ziel;
				kamera.Render();
				RenderTexture.active = ziel;
				Texture2D bild = new Texture2D(1280, 720, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
				bild.Apply();
				string pfad = Path.Combine(ordner, dateiname + ".png");
				File.WriteAllBytes(pfad, bild.EncodeToPNG());
				RenderTexture.active = null;
				kamera.targetTexture = null;
				Object.DestroyImmediate(ziel);
				Object.DestroyImmediate(bild);
				Debug.Log(logPraefix + " In-Welt geschrieben: " + pfad);
			}
			finally
			{
				Object.DestroyImmediate(kameraObjekt);
			}
		}

		/// <summary>
		/// Abnahme-Sammellauf fuer Task 5 (Owner-Auflage aus 3a/3/3b: Zeitersparnis durch EINEN
		/// gemeinsamen Unity-Start): rendert nacheinander die Nachher-Studio-Captures der 16
		/// Etappe-4-Props und die In-Welt-Stichprobe (SP_Tree_A + SP_Plant_Bush). Beide Teilschritte
		/// bleiben inhaltlich unveraendert (reine Delegation an <see cref="CapturePropsUebersichtNachher"/>
		/// und <see cref="CapturePropsInWelt"/>); nur der Prozessstart wird gebuendelt.
		/// </summary>
		[MenuItem("Eidren/V0.2/Stilumbau/Props Abnahme-Captures")]
		public static void CapturePropsAbnahme()
		{
			CapturePropsUebersichtNachher();
			CapturePropsInWelt("TempReview/StilumbauE4/inwelt");
		}

		[MenuItem("Eidren/V0.2/Stilumbau/Forge Vorher-Captures")]
		public static void CaptureForgeVorher()
		{
			CaptureForgeSzene("TempReview/StilumbauE2/forge-vorher");
		}

		[MenuItem("Eidren/V0.2/Stilumbau/Forge Nachher-Captures")]
		public static void CaptureForgeNachher()
		{
			CaptureForgeSzene("TempReview/StilumbauE2/forge-nachher");
		}

		public static void CaptureForgeSzene(string ordner)
		{
			Directory.CreateDirectory(ordner);
			UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/EidraForge.unity");
			WorldChestVisual[] kisten = Object.FindObjectsByType<WorldChestVisual>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
			HashSet<string> vergeben = new HashSet<string>();
			foreach (WorldChestVisual kiste in kisten)
			{
				string name = kiste.gameObject.name.Replace("(Clone)", string.Empty);
				if (!vergeben.Add(name))
				{
					continue; /* je Familie ein Exemplar reicht (SupplyChest steht mehrfach) */
				}
				Vector3 fuss = kiste.transform.position;
				GameObject kameraObjekt = new GameObject("ForgeCaptureKamera");
				Camera kamera = kameraObjekt.AddComponent<Camera>();
				/* Einzelne Instanzen stehen dicht an Architektur; fuer sie weicht die Kamera aus. */
				Dictionary<string, Vector3> sonderOffsets = new Dictionary<string, Vector3>
				{
					{ "RecoveryContainer_2D", new Vector3(-2.6f, 3.4f, 3.4f) }
				};
				Vector3 offset = sonderOffsets.TryGetValue(name, out Vector3 o) ? o : new Vector3(2.6f, 3.4f, -3.4f);
				kameraObjekt.transform.position = fuss + offset;
				kameraObjekt.transform.LookAt(fuss + Vector3.up * 0.5f);
				try
				{
					foreach (WorldChestVisualState zustand in new[] { WorldChestVisualState.Closed, WorldChestVisualState.Opened })
					{
						kiste.Apply(zustand);
						RenderTexture ziel = new RenderTexture(1280, 720, 24);
						kamera.targetTexture = ziel;
						kamera.Render();
						RenderTexture.active = ziel;
						Texture2D bild = new Texture2D(1280, 720, TextureFormat.RGB24, mipChain: false);
						bild.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
						bild.Apply();
						File.WriteAllBytes(Path.Combine(ordner, $"{name}_{zustand}.png"), bild.EncodeToPNG());
						RenderTexture.active = null;
						kamera.targetTexture = null;
						Object.DestroyImmediate(ziel);
						Object.DestroyImmediate(bild);
						Debug.Log("[StilE2] geschrieben: " + name + "_" + zustand);
					}
					kiste.Apply(WorldChestVisualState.Closed);
				}
				finally
				{
					Object.DestroyImmediate(kameraObjekt);
				}
			}
		}

		/* Die 9 BLD_-Gebaeudeprefabs (Level01) mit ihren Sollhoehen aus
		   STILUMBAU_E5_BAKEKETTE.md Abschnitt (d) (VisualScaleTableBuilder.AddBuildings,
		   Zeilen 117-127). Floor/FarmPlot sind GroundPlane-Eintraege mit Sollhoehe 0,0 (die
		   Tabelle prueft dort Breite statt Hoehe) -- fuer die Studio-Kamera wird dieser
		   Tabellenwert unveraendert als zielHoehe uebernommen. distanz je Task-2-Formel:
		   distanz = zielHoehe*1.2 + 2.2, Mindestwert 2.5. */
		private static readonly (string pfad, string name, float zielHoehe, float distanz)[] GebaeudeVorher =
		{
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Wall_L01.prefab", "BLD_Wall_L01", 2.6f, 5.32f),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Door_L01.prefab", "BLD_Door_L01", 2.6f, 5.32f),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Floor_L01.prefab", "BLD_Floor_L01", 0.0f, 2.5f),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_FarmPlot_L01.prefab", "BLD_FarmPlot_L01", 0.0f, 2.5f),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_CookingPot_L01.prefab", "BLD_CookingPot_L01", 1.0f, 3.4f),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Smelter_L01.prefab", "BLD_Smelter_L01", 2.0f, 4.6f),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Sawmill_L01.prefab", "BLD_Sawmill_L01", 2.2f, 4.84f),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Ropewalk_L01.prefab", "BLD_Ropewalk_L01", 1.6f, 4.12f),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Stonecutter_L01.prefab", "BLD_Stonecutter_L01", 1.4f, 3.88f)
		};

		[MenuItem("Eidren/V0.2/Stilumbau/Gebaeude Vorher-Captures")]
		public static void CaptureGebaeudeVorher()
		{
			CapturePrefabListe("TempReview/StilumbauE5/vorher", GebaeudeVorher, "[StilE5]");
		}

		/// <summary>
		/// Sammellauf fuer Task 2 der Etappe 5 (ein gemeinsamer Unity-Start statt zwei
		/// getrennter Aufrufe): rendert zuerst die zwei hoehenparametrisierten E4-Baum-
		/// Inwelt-Regressionsproben neu (SP_Tree_A/SP_Plant_Bush -- Kronenprobe fuer die
		/// neue <see cref="InWeltVersatz"/>-Formel, ueberschreibt dieselben Dateien unter
		/// TempReview/StilumbauE4/inwelt) und danach die Studio-Vorher-Captures der 9
		/// BLD_-Gebaeudeprefabs.
		/// </summary>
		[MenuItem("Eidren/V0.2/Stilumbau/E5 Task2 Kamera+Vorher-Captures")]
		public static void CaptureE5Task2()
		{
			CapturePropsInWelt("TempReview/StilumbauE4/inwelt");
			CaptureGebaeudeVorher();
		}

		/* Drei In-Welt-Stichproben fuer Task 6: BLD_Wall_L01/BLD_Smelter_L01/BLD_CookingPot_L01
		   bei (0,0,0)/(5,0,0)/(10,0,0) in HomeBase.unity, zielHoehe je aus
		   STILUMBAU_E5_BAKEKETTE.md (d) (VisualScaleTableBuilder.AddBuildings: Wall 2.6, Smelter
		   2.0, CookingPot 1.0). */
		private static readonly (string pfad, string name, float zielHoehe, Vector3 position)[] GebaeudeInWelt =
		{
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Wall_L01.prefab", "BLD_Wall_L01", 2.6f, new Vector3(0f, 0f, 0f)),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_Smelter_L01.prefab", "BLD_Smelter_L01", 2.0f, new Vector3(5f, 0f, 0f)),
			("Assets/_Game/Prefabs/Buildings/Level01/BLD_CookingPot_L01.prefab", "BLD_CookingPot_L01", 1.0f, new Vector3(10f, 0f, 0f))
		};

		/// <summary>
		/// Stilumbau Etappe 5, Task 6: In-Welt-Stichprobe fuer die Abnahme. Oeffnet
		/// <c>HomeBase.unity</c> (BAKEKETTE (b): Gebaeude werden dort nie gebacken, sondern
		/// laufzeitgebaut -- daher hier per <see cref="Object.Instantiate"/>, nicht
		/// <see cref="PrefabUtility.InstantiatePrefab"/>, analog <see cref="BuildingPlacementController"/>),
		/// instanziiert <see cref="GebaeudeInWelt"/> temporaer, rendert jedes mit dem
		/// hoehenparametrisierten <see cref="InWeltVersatz"/>-Kameraversatz und entfernt die
		/// Instanzen anschliessend wieder (<see cref="Object.DestroyImmediate(Object)"/>). Die
		/// Szene wird NICHT gespeichert.
		/// </summary>
		public static void CaptureGebaeudeInWelt(string ordner)
		{
			Directory.CreateDirectory(ordner);
			UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/HomeBase.unity");
			List<GameObject> instanzen = new List<GameObject>();
			try
			{
				foreach ((string pfad, string name, float zielHoehe, Vector3 position) eintrag in GebaeudeInWelt)
				{
					GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(eintrag.pfad);
					if (prefab == null)
					{
						Debug.LogError("[StilE5] Gebaeude-In-Welt-Prefab fehlt: " + eintrag.pfad);
						continue;
					}
					/* Laufzeitbau-Muster (kein PrefabUtility.InstantiatePrefab), siehe BAKEKETTE (b). */
					GameObject instanz = Object.Instantiate(prefab);
					instanz.transform.position = eintrag.position;
					instanzen.Add(instanz);
					/* Ein freistehendes Wall-Prefab hat laut WallConnectionContentBuilder.Piece alle 4
					   Joint_/Cap_-Kinder standardmaessig inaktiv (SetActive(false), Zeile ~93). Ohne
					   Nachbarwaende liefert GridWallConnection.Classify EndCap/EndCap (0 belegte Arme je
					   Knoten) -- exakt der Zustand, den BuildingPlacementController.Continuation.cs:786
					   fuer eine echte, isolierte Platzierung anwenden wuerde. Nachbilden, damit die
					   Abnahme-Aufnahme die Wandverbinder-Kappen tatsaechlich sichtbar zeigt. */
					WallConnectionView verbinder = instanz.GetComponent<WallConnectionView>();
					if (verbinder != null)
					{
						verbinder.Apply(new GridWallConnection(GridWallShape.EndCap, GridWallShape.EndCap), 0);
					}
					CapturePropsInWeltObjekt(eintrag.position, eintrag.zielHoehe, eintrag.name, ordner, "[StilE5]");
				}
			}
			finally
			{
				foreach (GameObject instanz in instanzen)
				{
					Object.DestroyImmediate(instanz);
				}
			}
			/* Szene bewusst nicht gespeichert -- reine Lese-/Stichprobenaufnahme. */
		}

		/// <summary>
		/// Abnahme-Sammellauf fuer Task 6 (EIN Unity-Start): Studio-Nachher-Captures der 9
		/// BLD_-Gebaeude (identische Liste/Werte wie <see cref="CaptureGebaeudeVorher"/>, jetzt
		/// gegen die Task-4-Fabrik-Geometrie) nach <c>TempReview/StilumbauE5/nachher/</c>, danach
		/// die In-Welt-Stichprobe (<see cref="CaptureGebaeudeInWelt"/>) nach
		/// <c>TempReview/StilumbauE5/inwelt/</c>.
		/// </summary>
		[MenuItem("Eidren/V0.2/Stilumbau/Gebaeude Abnahme-Captures")]
		public static void CaptureGebaeudeAbnahme()
		{
			CapturePrefabListe("TempReview/StilumbauE5/nachher", GebaeudeVorher, "[StilE5]");
			CaptureGebaeudeInWelt("TempReview/StilumbauE5/inwelt");
		}
	}
}
