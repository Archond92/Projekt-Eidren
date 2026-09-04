using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eidren.Editor
{
	/// <summary>
	/// Sichtpruefung fuer den Schmiede-Grundriss: oeffnet die gebaute Szene und
	/// legt zwei Ansichten ab - eine Draufsicht zum Abgleich mit dem Plan
	/// (SCHMIEDE_ENTWURF.md Abschnitt 3) und eine aus dem Zonenkamerawinkel.
	/// </summary>
	public static class ForgeLayoutCapture
	{
		private const string Ausgabe = "TempReview/SchmiedeE3";

		[MenuItem("Eidren/V0.2/Schmiede/Grundriss-Captures")]
		public static void Run()
		{
			/* Neutrale Beleuchtung: prueft die Geometrie, nicht die Stimmung. */
			Aufnehmen("grundriss", new Color(0.45f, 0.45f, 0.48f), 0.9f, Color.white);
		}

		/// <summary>
		/// Zweite Ansicht mit den Verlieswerten aus ForgeGlowProbe. Nur hier zeigt
		/// sich, ob die Glutflaechen als Glut lesbar sind - unter neutralem Licht
		/// sieht jede orange Flaeche gleich aus.
		/// </summary>
		[MenuItem("Eidren/V0.2/Schmiede/Verlies-Captures")]
		public static void RunDunkel()
		{
			Aufnehmen("verlies", new Color(0.055f, 0.06f, 0.08f), 0.8f, new Color(0.72f, 0.78f, 1f));
		}

		/// <summary>
		/// Aufnahme mit der Beleuchtung, die tatsaechlich in der Szene steht - ohne
		/// jede Ueberschreibung. Seit Etappe 3 ist das die einzige ehrliche Ansicht;
		/// die anderen beiden setzen eigenes Licht und zeigen damit etwas, das im
		/// Spiel so nie zu sehen ist.
		/// </summary>
		[MenuItem("Eidren/V0.2/Schmiede/Szenenlicht-Captures")]
		public static void RunSzenenlicht()
		{
			Aufnehmen("szene", null, 0f, Color.white);
		}

		private static void Aufnehmen(string praefix, Color? umgebung, float intensitaet, Color lichtfarbe)
		{
			Directory.CreateDirectory(Ausgabe);
			EditorSceneManager.OpenScene(EidraForgeSceneBuilder.ScenePath, OpenSceneMode.Single);

			GameObject lichtObjekt = null;
			if (umgebung.HasValue)
			{
				RenderSettings.ambientMode = AmbientMode.Flat;
				RenderSettings.ambientLight = umgebung.Value;
				RenderSettings.fog = false;

				lichtObjekt = new GameObject("CaptureLicht");
				Light licht = lichtObjekt.AddComponent<Light>();
				licht.type = LightType.Directional;
				licht.intensity = intensitaet;
				licht.color = lichtfarbe;
				lichtObjekt.transform.rotation = Quaternion.Euler(50f, 200f, 0f);
			}

			Rendern($"{praefix}_draufsicht.png", new Vector3(0f, 80f, 6f), new Vector3(90f, 0f, 0f), 48f);
			Rendern($"{praefix}_zonenwinkel.png", new Vector3(0f, 6f, 6f), new Vector3(52f, 45f, 0f), 52f);
			Rendern($"{praefix}_essenkern.png", new Vector3(0f, 0f, 32f), new Vector3(52f, 45f, 0f), 18f);
			/* Spielzoom: die Zonenkamera arbeitet mit Groesse 7,4 (Zone_EidraForge).
			   Ueber Glutadern und Kohlenpfannen laesst sich nur hier urteilen -
			   in den Uebersichten sind sie sieben Mal zu klein. */
			Rendern($"{praefix}_spielzoom_einbruch.png", new Vector3(13f, 0f, 0f), new Vector3(52f, 45f, 0f), 7.4f);
			Rendern($"{praefix}_spielzoom_esse.png", new Vector3(0f, 0f, 26f), new Vector3(52f, 45f, 0f), 7.4f);
			Rendern($"{praefix}_spielzoom_massel.png", new Vector3(-15f, 0f, -2f), new Vector3(52f, 45f, 0f), 9f);
			Rendern($"{praefix}_spielzoom_grubenkante.png", new Vector3(-11f, 0f, 30f), new Vector3(52f, 45f, 0f), 8f);
			Rendern($"{praefix}_spielzoom_balgkammer.png", new Vector3(-3f, 0f, -23f), new Vector3(52f, 45f, 0f), 9f);
			Rendern($"{praefix}_spielzoom_hammerwerk.png", new Vector3(27f, 0f, 31f), new Vector3(52f, 45f, 0f), 8f);
			Rendern($"{praefix}_spielzoom_bindung.png", new Vector3(-30f, 0f, 32f), new Vector3(52f, 45f, 0f), 8f);

			if (lichtObjekt != null)
			{
				Object.DestroyImmediate(lichtObjekt);
			}
			Debug.Log("[SchmiedeCapture] geschrieben nach " + Path.GetFullPath(Ausgabe));
		}

		private static void Rendern(string dateiname, Vector3 blickpunkt, Vector3 drehung, float groesse)
		{
			GameObject kameraObjekt = new GameObject("Capturekamera");
			Camera kamera = kameraObjekt.AddComponent<Camera>();
			kamera.orthographic = true;
			kamera.orthographicSize = groesse;
			kamera.clearFlags = CameraClearFlags.SolidColor;
			kamera.backgroundColor = new Color(0.06f, 0.06f, 0.07f);
			kamera.nearClipPlane = 0.1f;
			kamera.farClipPlane = 400f;
			kameraObjekt.transform.rotation = Quaternion.Euler(drehung);
			/* Abstand an die Bildgroesse gekoppelt statt fest 150: der Nebel wird
			   aus der Kameraentfernung berechnet, und aus 150 Einheiten verschluckt
			   er bei Nebelende 60 die gesamte Szene. Die Zonenkamera steht real
			   rund 19 Einheiten entfernt (Offset -8,3/15/-8,3), was bei Groesse 7,4
			   dem Faktor 2,6 entspricht. */
			kameraObjekt.transform.position = blickpunkt - kameraObjekt.transform.forward * (groesse * 2.6f);

			RenderTexture ziel = new RenderTexture(1100, 1100, 24);
			try
			{
				kamera.targetTexture = ziel;
				kamera.Render();
				RenderTexture.active = ziel;
				Texture2D bild = new Texture2D(1100, 1100, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 1100f, 1100f), 0, 0);
				bild.Apply();
				File.WriteAllBytes(Path.Combine(Ausgabe, dateiname), bild.EncodeToPNG());
				Object.DestroyImmediate(bild);
			}
			finally
			{
				RenderTexture.active = null;
				kamera.targetTexture = null;
				Object.DestroyImmediate(ziel);
				Object.DestroyImmediate(kameraObjekt);
			}
		}
	}
}
