using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Sichtpruefung der umgebauten Gegner: rendert das GEGNER-Prefab (nicht das
	/// Darstellungs-Prefab), damit die ganze Kette g: Prefab -> Mesh3D ->
	/// Material -> Shader gesehen wird. Muster wie
	/// WorldChestStyleCaptureUtility.CapturePrefabListe, ergaenzt um das Samplen
	/// der Legacy-Clips (im EditMode laeuft kein Animationssystem — die Pose
	/// muss per SampleAnimation gestellt werden).
	///
	/// Lauf OHNE -nographics, sonst gibt es kein Grafikgeraet zum Rendern.
	/// Zielordner per Umgebungsvariable EIDREN_CAPTURE_ORDNER, sonst
	/// Temp/KreaturenSicht im Projekt.
	/// </summary>
	public static class CreatureSichtCapture
	{
		private const int Groesse = 768;

		[MenuItem("Eidren/V0.2/Sichtpruefung Riftling (Gegner-Prefab)")]
		public static void CaptureRiftling()
		{
			Capture("Riftling", CreatureEnemyRewire.EnemyPrefabPath("Riftling"), 1.70f);
			Debug.Log("[K3D-Sicht] Riftling-Captures geschrieben.");
		}

		[MenuItem("Eidren/Mid-Poly/Phase 3/Sichtpruefung Ignivar (Produktions-Wrapper)")]
		public static void CaptureIgnivar()
		{
			Capture("Ignivar", "Assets/_Game/Resources/Prefabs/Actors/3D/Ignivar_3D.prefab", 1.00f);
			Debug.Log("[K3D-Sicht] Ignivar-Produktionscaptures geschrieben.");
		}

		/// <summary>
		/// Nach einem Neuexport der GLB: Prefabs neu bauen und direkt die
		/// Sichtpruefung rendern — ein Editorlauf statt zwei.
		/// </summary>
		public static void BuildAlleUndCapture()
		{
			CreatureMeshBuilder.BuildAll();
			CaptureAlle();
		}

		/// <summary>
		/// Sammellauf fuer den Nachzug: Kupfer-Abbaufeedback wiederherstellen
		/// und die Sichtpruefung wiederholen (GraniteShell-Heckkappe ist seit
		/// dem letzten Capture von Fuge auf Stein umgestellt).
		/// </summary>
		public static void NachzugKupferUndCapture()
		{
			CopperMiningFeedbackRebuilder.Rebuild();
			CreatureMeshBuilder.BuildAll();
			CaptureAlle();
		}

		/// <summary>
		/// Sichtpruefung aller 14 Figuren auf ihren Darstellungs-Prefabs —
		/// unabhaengig vom Gegner-Umbau. Vorder-, Rueck- und Seitenansicht,
		/// weil die Einseitigkeitsanalyse Verdachtsfaelle gemeldet hat, deren
		/// Rueckseiten im Quell-Viewer (Backface-Flip) nie sichtbar waren.
		/// </summary>
		public static void CaptureAlle()
		{
			foreach ((string name, float hoehe) in CreatureMeshBuilder.Creatures)
			{
				Capture(name, CreatureMeshBuilder.VisualPrefabPath(name), hoehe);
			}
			Debug.Log("[K3D-Sicht] alle Kreaturen-Captures geschrieben.");
		}

		/// <summary>
		/// Diagnose zum fehlenden Kopf: Bindpose ohne Clip-Sampling, Original-
		/// gegen doppelseitigen Shader, dazu Mesh- und Renderer-Bounds im Log.
		/// Trennt drei Hypothesen: Mesh unvollstaendig importiert (Bounds zu
		/// niedrig), Flaechen gecullt (nur doppelseitig sichtbar), Skinning
		/// verformt (nur mit Clip falsch).
		/// </summary>
		public static void DiagnoseRiftling()
		{
			string ordner = Environment.GetEnvironmentVariable("EIDREN_CAPTURE_ORDNER");
			if (string.IsNullOrEmpty(ordner))
			{
				ordner = "Temp/KreaturenSicht";
			}
			Directory.CreateDirectory(ordner);

			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
				CreatureEnemyRewire.EnemyPrefabPath("Riftling"));
			GameObject instanz = UnityEngine.Object.Instantiate(prefab);
			var lichtObjekt = new GameObject("CaptureLicht");
			var kameraObjekt = new GameObject("CaptureKamera");
			try
			{
				Light licht = lichtObjekt.AddComponent<Light>();
				licht.type = LightType.Directional;
				licht.intensity = 1.1f;
				lichtObjekt.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
				Camera kamera = kameraObjekt.AddComponent<Camera>();
				kamera.clearFlags = CameraClearFlags.SolidColor;
				kamera.backgroundColor = new Color(0.16f, 0.17f, 0.18f);
				kameraObjekt.transform.position = new Vector3(0f, 1.6f, 3.2f);
				kameraObjekt.transform.LookAt(new Vector3(0f, 0.9f, 0f));

				var renderer = instanz.GetComponentInChildren<SkinnedMeshRenderer>(true);
				Debug.Log("[K3D-Diag] sharedMesh: " + renderer.sharedMesh.vertexCount
					+ " Punkte, Mesh-Bounds " + renderer.sharedMesh.bounds.min.y.ToString("0.000")
					+ " .. " + renderer.sharedMesh.bounds.max.y.ToString("0.000")
					+ " | Renderer-Bounds " + renderer.bounds.min.y.ToString("0.000")
					+ " .. " + renderer.bounds.max.y.ToString("0.000")
					+ " | Knochen: " + renderer.bones.Length);
				foreach (Transform knochen in renderer.bones)
				{
					Debug.Log("[K3D-Diag] Knochen " + knochen.name
						+ " y=" + knochen.position.y.ToString("0.000")
						+ " skala=" + knochen.lossyScale.y.ToString("0.000"));
				}

				// 1. Bindpose, Originalmaterial — KEIN SampleAnimation.
				Render(kamera, Path.Combine(ordner, "diag_bindpose_original.png"));

				// 2. Bindpose, doppelseitig (Sprites/Default zeichnet beide
				//    Seiten und nutzt die Vertexfarben).
				Material original = renderer.sharedMaterial;
				var doppelseitig = new Material(Shader.Find("Sprites/Default"));
				renderer.sharedMaterials = new[] { doppelseitig };
				Render(kamera, Path.Combine(ordner, "diag_bindpose_doppelseitig.png"));
				renderer.sharedMaterials = new[] { original };

				// 3. Ruhe gesampelt, Originalmaterial (Vergleichsbild).
				Animation anim = instanz.GetComponentInChildren<Animation>(true);
				anim.GetClip("Ruhe").SampleAnimation(anim.gameObject, 0f);
				Debug.Log("[K3D-Diag] nach Ruhe-Sample: Renderer-Bounds "
					+ renderer.bounds.min.y.ToString("0.000") + " .. "
					+ renderer.bounds.max.y.ToString("0.000"));
				foreach (Transform knochen in renderer.bones)
				{
					Debug.Log("[K3D-DiagRuhe] " + knochen.name
						+ " y=" + knochen.position.y.ToString("0.000")
						+ " skala=" + knochen.lossyScale.y.ToString("0.000"));
				}
				Render(kamera, Path.Combine(ordner, "diag_ruhe_original.png"));
				Debug.Log("[K3D-Diag] fertig.");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instanz);
				UnityEngine.Object.DestroyImmediate(kameraObjekt);
				UnityEngine.Object.DestroyImmediate(lichtObjekt);
			}
		}

		public static void Capture(string name, string prefabPfad, float hoehe)
		{
			string ordner = Environment.GetEnvironmentVariable("EIDREN_CAPTURE_ORDNER");
			if (string.IsNullOrEmpty(ordner))
			{
				ordner = "Temp/KreaturenSicht";
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

				// Bodenplatte bei y=0 — die Bodenlage ist ein Pruefpunkt.
				boden.name = "CaptureBoden";
				boden.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
				boden.transform.localScale = new Vector3(6f, 6f, 1f);
				var bodenMaterial = new Material(Shader.Find("Unlit/Color"));
				bodenMaterial.color = new Color(0.30f, 0.31f, 0.30f);
				boden.GetComponent<MeshRenderer>().sharedMaterial = bodenMaterial;

				Camera kamera = kameraObjekt.AddComponent<Camera>();
				kamera.clearFlags = CameraClearFlags.SolidColor;
				kamera.backgroundColor = new Color(0.16f, 0.17f, 0.18f);

				Animation anim = instanz.GetComponentInChildren<Animation>(true);
				if (anim == null)
				{
					throw new InvalidOperationException("Keine Animation-Komponente in " + prefabPfad);
				}

				(string clip, float zeit, string kurz)[] posen =
				{
					("Ruhe", 0f, "ruhe"),
					("Gehen", 0.35f, "gehen")
				};
				// Kamerawinkel um die Figur. Im Prefab steht das Modell
				// ungedreht: seine Front zeigt nach +z (im Spiel dreht die
				// Darstellungsschicht per Gierwinkel). 0 Grad = Kamera auf +z,
				// also von vorn; 180 = Rueckseite. Die Rueckansicht gehoert
				// zur Pruefung, weil der Quell-Viewer Backfaces flippt und
				// einseitige Flaechen dort nie auffallen konnten.
				(float winkel, string kurz)[] ansichten =
				{
					(0f, "vorn"),
					(180f, "hinten"),
					(90f, "seite")
				};

				float distanz = Mathf.Max(2.6f, hoehe * 1.9f);
				Vector3 blickziel = new Vector3(0f, hoehe * 0.52f, 0f);

				foreach ((string clipName, float zeit, string poseKurz) in posen)
				{
					AnimationClip clip = anim.GetClip(clipName);
					if (clip == null)
					{
						Debug.LogError("[K3D-Sicht] Clip fehlt: " + clipName);
						continue;
					}
					clip.SampleAnimation(anim.gameObject, zeit * clip.length);

					foreach ((float winkel, string ansichtKurz) in ansichten)
					{
						float rad = winkel * Mathf.Deg2Rad;
						kameraObjekt.transform.position = new Vector3(
							Mathf.Sin(rad) * distanz,
							hoehe * 0.62f + 0.55f,
							Mathf.Cos(rad) * distanz);
						kameraObjekt.transform.LookAt(blickziel);

						string datei = Path.Combine(ordner,
							name.ToLowerInvariant() + "_" + poseKurz + "_" + ansichtKurz + ".png");
						Render(kamera, datei);
					}
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
