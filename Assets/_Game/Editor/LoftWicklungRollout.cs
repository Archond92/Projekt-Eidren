using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Rollout des Loft-Wicklungsfixes (14.08.2026): baut alle Loft-basierten
	/// Assets ueber ihre Builder neu und rendert Beleg-Captures der wichtigsten
	/// Prefabs. Muster wie CreatureSichtCapture, aber ohne Animations-Sampling,
	/// dafuer mit Seitenansicht knapp ueber dem Boden — dort waren die
	/// weggecullten Seitenflaechen am deutlichsten sichtbar.
	///
	/// Lauf OHNE -nographics (Captures brauchen ein Grafikgeraet), mit -quit.
	/// Zielordner per Umgebungsvariable EIDREN_CAPTURE_ORDNER, sonst
	/// Temp/LoftWicklungSicht im Projekt.
	/// </summary>
	public static class LoftWicklungRollout
	{
		private const int Groesse = 768;

		public static void RebuildUndCapture()
		{
			T1ResourceVisualBuilder.BuildStandalone();
			T2ResourceVisualBuilder.BuildStandalone();
			StyleProofPropBuilder.BuildStandalone();
			EidrenBuildingVisualBuilder.BuildStandalone();
			ForgeContainerVisualBuilder.Build();
			WorldChestContentBuilder.RebuildPrefabsForFixes();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			CaptureBelege();
			Debug.Log("[LoftFix] Neubau und Captures abgeschlossen.");
		}

		/// <summary>Nur die Captures, ohne Neubau (fuer Vorher-Bilder).</summary>
		public static void CaptureBelege()
		{
			(string name, string pfad, float hoehe)[] belege =
			{
				("tree", "Assets/_Game/Prefabs/Resources/Visuals/Tree_Active.prefab", 6f),
				("stone", "Assets/_Game/Prefabs/Resources/Visuals/StoneDeposit_Active.prefab", 1f),
				("berrybush", "Assets/_Game/Prefabs/Resources/Visuals/BerryBush_Active.prefab", 1.2f),
				("fiberplant", "Assets/_Game/Prefabs/Resources/Visuals/FiberPlant_Active.prefab", 1.2f),
				("sp_rock", "Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Medium.prefab", 1f),
				("chest", "Assets/_Game/Prefabs/Loot/WorldChests/WorldChest_Common.prefab", 0.8f)
			};
			foreach ((string name, string pfad, float hoehe) in belege)
			{
				Capture(name, pfad, hoehe);
			}
			Debug.Log("[LoftFix] alle Beleg-Captures geschrieben.");
		}

		private static void Capture(string name, string prefabPfad, float hoehe)
		{
			string ordner = Environment.GetEnvironmentVariable("EIDREN_CAPTURE_ORDNER");
			if (string.IsNullOrEmpty(ordner))
			{
				ordner = "Temp/LoftWicklungSicht";
			}
			Directory.CreateDirectory(ordner);

			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPfad);
			if (prefab == null)
			{
				throw new InvalidOperationException("Prefab fehlt: " + prefabPfad);
			}

			GameObject instanz = UnityEngine.Object.Instantiate(prefab);
			var lichtObjekt = new GameObject("CaptureLicht");
			var kameraObjekt = new GameObject("CaptureKamera");
			var boden = GameObject.CreatePrimitive(PrimitiveType.Quad);
			try
			{
				Light licht = lichtObjekt.AddComponent<Light>();
				licht.type = LightType.Directional;
				licht.intensity = 1.1f;
				lichtObjekt.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

				boden.name = "CaptureBoden";
				boden.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
				boden.transform.localScale = new Vector3(hoehe * 4f, hoehe * 4f, 1f);
				var bodenMaterial = new Material(Shader.Find("Unlit/Color"));
				bodenMaterial.color = new Color(0.30f, 0.31f, 0.30f);
				boden.GetComponent<MeshRenderer>().sharedMaterial = bodenMaterial;

				Camera kamera = kameraObjekt.AddComponent<Camera>();
				kamera.clearFlags = CameraClearFlags.SolidColor;
				kamera.backgroundColor = new Color(0.16f, 0.17f, 0.18f);

				/* Drei Blickwinkel: Spielkamera (steil von oben), Halbhoehe und
				   flach von der Seite. Bei einwaerts gewickelten Seitenflaechen
				   zeigt die flache Ansicht das Objekt hohl/durchsichtig. */
				(float elevation, string kurz)[] ansichten =
				{
					(55f, "spielwinkel"),
					(25f, "halbhoch"),
					(5f, "flach")
				};

				float distanz = Mathf.Max(2.6f, hoehe * 1.9f);
				Vector3 blickziel = new Vector3(0f, hoehe * 0.45f, 0f);

				foreach ((float elevation, string ansichtKurz) in ansichten)
				{
					float rad = elevation * Mathf.Deg2Rad;
					kameraObjekt.transform.position = new Vector3(
						Mathf.Sin(0.6f) * distanz * Mathf.Cos(rad),
						hoehe * 0.45f + Mathf.Sin(rad) * distanz,
						Mathf.Cos(0.6f) * distanz * Mathf.Cos(rad));
					kameraObjekt.transform.LookAt(blickziel);

					string datei = Path.Combine(ordner, name + "_" + ansichtKurz + ".png");
					Render(kamera, datei);
				}
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instanz);
				UnityEngine.Object.DestroyImmediate(kameraObjekt);
				UnityEngine.Object.DestroyImmediate(lichtObjekt);
				UnityEngine.Object.DestroyImmediate(boden);
			}
		}

		private static void Render(Camera kamera, string pfad)
		{
			var ziel = new RenderTexture(Groesse, Groesse, 24, RenderTextureFormat.ARGB32);
			kamera.targetTexture = ziel;
			kamera.Render();
			RenderTexture vorher = RenderTexture.active;
			RenderTexture.active = ziel;
			var bild = new Texture2D(Groesse, Groesse, TextureFormat.RGB24, mipChain: false);
			bild.ReadPixels(new Rect(0f, 0f, Groesse, Groesse), 0, 0);
			bild.Apply();
			File.WriteAllBytes(pfad, bild.EncodeToPNG());
			RenderTexture.active = vorher;
			kamera.targetTexture = null;
			UnityEngine.Object.DestroyImmediate(ziel);
			UnityEngine.Object.DestroyImmediate(bild);
		}
	}
}
