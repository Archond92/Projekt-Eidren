using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eidren.Editor
{
	/// <summary>
	/// Klaert mit Zahlen statt mit Bildern, ob Punktlichter den Weltshader
	/// ueberhaupt erreichen. In der Schmiede hat eine Verdopplung aller
	/// Intensitaeten das Bild kaum veraendert - das deutet auf einen toten
	/// Zusatzlichtpfad hin, nicht auf zu kleine Zahlen.
	///
	/// Aufbau: eine Bodenkachel, ein Punktlicht darueber, kein Richtungslicht,
	/// Umgebungslicht fast schwarz. Gemessen wird die mittlere Helligkeit der
	/// Kachel bei ausgeschaltetem und eingeschaltetem Licht.
	/// </summary>
	public static class ForgeLichtProbe
	{
		[MenuItem("Eidren/V0.2/Schmiede/Zusatzlicht messen")]
		public static void Run()
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.02f);
			RenderSettings.fog = false;

			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
			GameObject boden = new GameObject("Probekachel");
			boden.transform.position = new Vector3(0f, -0.4f, 0f);
			boden.AddComponent<MeshFilter>().sharedMesh =
				EidrenMeshFactory.TaperedBox(new Vector3(8f, 0.4f, 8f), 1f, new Color(0.5f, 0.5f, 0.5f));
			boden.AddComponent<MeshRenderer>().sharedMaterial = welt;

			GameObject lichtObjekt = new GameObject("Probelicht");
			lichtObjekt.transform.position = new Vector3(0f, 3f, 0f);
			Light licht = lichtObjekt.AddComponent<Light>();
			licht.type = LightType.Point;
			licht.range = 12f;
			licht.intensity = 4f;
			licht.color = Color.white;
			licht.shadows = LightShadows.None;

			StringBuilder bericht = new StringBuilder();
			bericht.AppendLine("[Lichtprobe] Mittlere Helligkeit der Kachel:");
			licht.enabled = false;
			bericht.AppendLine($"  ohne Punktlicht: {Messen():0.0000}");
			licht.enabled = true;
			bericht.AppendLine($"  mit  Punktlicht: {Messen():0.0000}");

			/* Gegenprobe mit dem eingebauten URP-Lit-Shader: zeigt, ob die Grenze
			   am Weltshader liegt oder an Projekteinstellungen. */
			Shader urp = Shader.Find("Universal Render Pipeline/Lit");
			if (urp != null)
			{
				Material vergleich = new Material(urp) { color = new Color(0.5f, 0.5f, 0.5f) };
				boden.GetComponent<MeshRenderer>().sharedMaterial = vergleich;
				licht.enabled = false;
				bericht.AppendLine($"  URP-Lit ohne Licht: {Messen():0.0000}");
				licht.enabled = true;
				bericht.AppendLine($"  URP-Lit mit  Licht: {Messen():0.0000}");
				Object.DestroyImmediate(vergleich);
			}

			Debug.Log(bericht.ToString());
			Object.DestroyImmediate(lichtObjekt);
			Object.DestroyImmediate(boden);
		}

		private static float Messen()
		{
			GameObject kameraObjekt = new GameObject("Probekamera");
			Camera kamera = kameraObjekt.AddComponent<Camera>();
			kamera.orthographic = true;
			kamera.orthographicSize = 2f;
			kamera.clearFlags = CameraClearFlags.SolidColor;
			kamera.backgroundColor = Color.black;
			kameraObjekt.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
			kameraObjekt.transform.position = new Vector3(0f, 12f, 0f);

			RenderTexture ziel = new RenderTexture(64, 64, 24);
			float summe = 0f;
			try
			{
				kamera.targetTexture = ziel;
				kamera.Render();
				RenderTexture.active = ziel;
				Texture2D bild = new Texture2D(64, 64, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 64f, 64f), 0, 0);
				bild.Apply();
				Color[] pixel = bild.GetPixels();
				foreach (Color farbe in pixel)
				{
					summe += (farbe.r + farbe.g + farbe.b) / 3f;
				}
				summe /= pixel.Length;
				Object.DestroyImmediate(bild);
			}
			finally
			{
				RenderTexture.active = null;
				kamera.targetTexture = null;
				Object.DestroyImmediate(ziel);
				Object.DestroyImmediate(kameraObjekt);
			}
			return summe;
		}
	}
}
