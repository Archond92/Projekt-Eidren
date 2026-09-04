using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eidren.Editor
{
	/// <summary>
	/// Technische Probe fuer die Verlassene Eidra-Schmiede: klaert, ob eine
	/// unbeleuchtete Glutflaeche im fast schwarzen Verlies als Glut lesbar ist.
	/// Das Projekt rendert in Gamma ohne Nachbearbeitung, es gibt also kein Bloom,
	/// das die Glut traegt - der Effekt muss allein aus Farbe und Kontrast kommen.
	/// Rendert dieselbe Esse zweimal aus der Zonenkamera-Perspektive: einmal mit
	/// dem Glut-Material, einmal mit dem normalen Weltmaterial.
	/// </summary>
	public static class ForgeGlowProbe
	{
		private const string Ausgabe = "TempReview/SchmiedeGlut";

		/* Palette aus dem Grafikauftrag (Documentation/
		   GRAFIKAUFTRAG_SCHMIEDE_REFERENZBLAETTER.md): Stein #1F1A1C, Rost #6B1F06,
		   Glut #FF6B10. Die Werte entsprechen den vorhandenen Forge-Materialien
		   aus EidraForgeSceneBuilder. */
		/* Durchgang 1 mit Stein 0,12 und Richtungslicht 0,25 ergab eine schwarze
		   Silhouette: 0,12 Albedo mal 0,25 Licht liegt bei 0,03 und ist nicht mehr
		   lesbar. Stein deshalb auf 0,22 angehoben, Licht auf 0,8 - die Glut bleibt
		   trotzdem im Vorteil, weil sie unbeleuchtet bei vollem Wert liegt. */
		private static readonly Color EssenStein = new Color(0.22f, 0.19f, 0.2f);
		private static readonly Color EssenEisen = new Color(0.42f, 0.12f, 0.025f);
		private static readonly Color Glut = new Color(1f, 0.42f, 0.06f);
		private static readonly Color Boden = new Color(0.17f, 0.15f, 0.16f);

		[MenuItem("Eidren/V0.2/Schmiede/Glut-Probe")]
		public static void Run()
		{
			Directory.CreateDirectory(Ausgabe);
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			/* Verlies-Grundstimmung: Umgebungslicht fast schwarz mit blauem Stich,
			   Richtungslicht flach und schwach - es ist im Entwurf nur fuer
			   Figurenlesbarkeit und Wandschatten zustaendig, nicht fuer Helligkeit. */
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = new Color(0.055f, 0.06f, 0.08f);
			RenderSettings.fog = false;

			GameObject lichtObjekt = new GameObject("Grundlicht");
			Light richtung = lichtObjekt.AddComponent<Light>();
			richtung.type = LightType.Directional;
			richtung.intensity = 0.8f;
			richtung.color = new Color(0.72f, 0.78f, 1f);
			lichtObjekt.transform.rotation = Quaternion.Euler(30f, 250f, 0f);

			Rendern(mitGlutMaterial: true, "esse_mit_glutmaterial.png");
			Rendern(mitGlutMaterial: false, "esse_ohne_glutmaterial.png");

			Object.DestroyImmediate(lichtObjekt);
			Debug.Log("[Glutprobe] geschrieben nach " + Path.GetFullPath(Ausgabe));
		}

		private static void Rendern(bool mitGlutMaterial, string dateiname)
		{
			GameObject wurzel = Bauen(mitGlutMaterial);

			/* Zonenkamera der Schmiede: orthografisch, 52 Grad Neigung, 45 Grad
			   Drehung (Zone_EidraForge.asset). Blickpunkt auf halber Essenhoehe. */
			GameObject kameraObjekt = new GameObject("Probekamera");
			Camera kamera = kameraObjekt.AddComponent<Camera>();
			kamera.orthographic = true;
			kamera.orthographicSize = 9.5f;
			kamera.clearFlags = CameraClearFlags.SolidColor;
			kamera.backgroundColor = Color.black;
			kamera.nearClipPlane = 0.1f;
			kamera.farClipPlane = 140f;
			kameraObjekt.transform.rotation = Quaternion.Euler(52f, 45f, 0f);
			kameraObjekt.transform.position = new Vector3(0f, 5f, 0f) - kameraObjekt.transform.forward * 50f;

			RenderTexture ziel = new RenderTexture(900, 900, 24);
			try
			{
				kamera.targetTexture = ziel;
				kamera.Render();
				RenderTexture.active = ziel;
				Texture2D bild = new Texture2D(900, 900, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 900f, 900f), 0, 0);
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
				Object.DestroyImmediate(wurzel);
			}
		}

		private static GameObject Bauen(bool mitGlutMaterial)
		{
			GameObject wurzel = new GameObject("GlutProbe");
			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
			Material leucht = mitGlutMaterial ? EidrenWorldStyleAssets.EnsureGlowMaterial() : welt;

			/* TaperedBox waechst von der Basisflaeche nach oben (y=0), nicht zentriert.
			   Der Boden muss deshalb auf -0,4 sitzen, damit seine Oberkante auf y=0
			   liegt - in Durchgang 2 lag sie auf 0,2 und hat die Glutadern verschluckt. */
			Teil(wurzel, "Boden", EidrenMeshFactory.TaperedBox(new Vector3(34f, 0.4f, 34f), 1f, Boden),
				new Vector3(0f, -0.4f, 0f), welt);

			/* Essenkoerper: sieben Profilringe, Silhouette aus Blatt_1_Essenkern_v2.png.
			   Hoehe 10 m - bei 52 Grad Kamerawinkel verdeckt der nahe Grubenrand eine
			   auf Galerieebene buendig abschliessende Oeffnung. */
			EidrenMeshFactory.LoftProfile[] koerper =
			{
				new EidrenMeshFactory.LoftProfile(0f, 8f, 8f),
				new EidrenMeshFactory.LoftProfile(1.8f, 7.6f, 7.6f),
				new EidrenMeshFactory.LoftProfile(3.6f, 6.8f, 6.8f),
				new EidrenMeshFactory.LoftProfile(5.4f, 5.6f, 5.6f),
				new EidrenMeshFactory.LoftProfile(7.2f, 4.4f, 4.4f),
				new EidrenMeshFactory.LoftProfile(8.6f, 4f, 4f),
				new EidrenMeshFactory.LoftProfile(9.5f, 3.6f, 3.6f)
			};
			Teil(wurzel, "Koerper", EidrenMeshFactory.Loft(koerper, EidrenMeshFactory.LoftShape.Oct,
				EssenStein, capBottom: true, capTop: false), Vector3.zero, welt);

			Band(wurzel, "BandTief", 1.6f, 7.94f, welt);
			Band(wurzel, "BandMitte", 3.8f, 6.97f, welt);
			Band(wurzel, "BandHoch", 8.3f, 4.39f, welt);

			EidrenMeshFactory.LoftProfile[] deckel =
			{
				new EidrenMeshFactory.LoftProfile(0f, 4.4f, 4.4f),
				new EidrenMeshFactory.LoftProfile(0.5f, 4.2f, 4.2f)
			};
			Teil(wurzel, "Deckplatte", EidrenMeshFactory.Loft(deckel, EidrenMeshFactory.LoftShape.Oct,
				EssenStein, capBottom: false, capTop: true), new Vector3(0f, 9.5f, 0f), welt);

			/* Feueroeffnung auf der kamerazugewandten Facette (Kamera blickt aus -X/-Z).
			   Rahmen beleuchtet, Glutflaeche selbstleuchtend und mit kontaktAo:false,
			   damit die eingebackene Kontaktabdunklung die Glut nicht ausbleicht. */
			Teil(wurzel, "OeffnungRahmen", EidrenMeshFactory.TaperedBox(new Vector3(3.4f, 3.2f, 0.6f), 1f, EssenEisen),
				new Vector3(0f, 6f, -2.75f), welt);
			Teil(wurzel, "OeffnungGlut", EidrenMeshFactory.TaperedBox(new Vector3(2.6f, 2.4f, 0.2f), 1f, Glut, kontaktAo: false),
				new Vector3(0f, 6f, -3f), leucht);

			/* Drei Glutadern im Boden ohne eigenes Punktlicht - die Probe soll zeigen,
			   ob sie allein durch Selbstleuchten tragen. Im ersten Durchgang lagen sie
			   ausserhalb des Bildausschnitts; Lage jetzt gegen den Ausschnitt gerechnet
			   (orthografisch, Groesse 9,5, Blickpunkt 0/5/0). */
			Glutader(wurzel, "Glutader1", new Vector3(-7f, 0.01f, 2f), 5f, leucht);
			Glutader(wurzel, "Glutader2", new Vector3(5f, 0.01f, -5f), 3.5f, leucht);
			Glutader(wurzel, "Glutader3", new Vector3(2f, 0.01f, 6f), 6f, leucht);

			/* Warmlicht der Feueroeffnung: erst dadurch faerbt die Esse ihre Umgebung
			   ein. Reichweite 18 wie im Lichtplan, belegt einen der vier
			   Zusatzlicht-Plaetze pro Objekt. */
			GameObject essenLicht = new GameObject("EssenLicht");
			essenLicht.transform.SetParent(wurzel.transform, worldPositionStays: false);
			essenLicht.transform.localPosition = new Vector3(0f, 5.5f, -4.5f);
			Light warm = essenLicht.AddComponent<Light>();
			warm.type = LightType.Point;
			warm.range = 18f;
			warm.intensity = 3.2f;
			warm.color = new Color(1f, 0.5f, 0.16f);

			return wurzel;
		}

		private static void Glutader(GameObject wurzel, string name, Vector3 position, float laenge, Material material)
		{
			Teil(wurzel, name, EidrenMeshFactory.TaperedBox(new Vector3(0.4f, 0.12f, laenge), 1f, Glut, kontaktAo: false),
				position, material);
		}

		private static void Band(GameObject wurzel, string name, float hoehe, float breite, Material material)
		{
			EidrenMeshFactory.LoftProfile[] ring =
			{
				new EidrenMeshFactory.LoftProfile(0f, breite, breite),
				new EidrenMeshFactory.LoftProfile(0.35f, breite, breite)
			};
			Teil(wurzel, name, EidrenMeshFactory.Loft(ring, EidrenMeshFactory.LoftShape.Oct, EssenEisen,
				capBottom: false, capTop: false), new Vector3(0f, hoehe, 0f), material);
		}

		private static void Teil(GameObject wurzel, string name, Mesh mesh, Vector3 position, Material material)
		{
			GameObject teil = new GameObject(name);
			teil.transform.SetParent(wurzel.transform, worldPositionStays: false);
			teil.transform.localPosition = position;
			teil.AddComponent<MeshFilter>().sharedMesh = mesh;
			teil.AddComponent<MeshRenderer>().sharedMaterial = material;
		}
	}
}
