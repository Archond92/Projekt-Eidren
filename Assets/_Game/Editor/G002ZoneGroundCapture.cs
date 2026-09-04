using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Eidren.Editor
{
// G-002, Abnahmekriterium 7: jedes Gebiet braucht eine eigene, unterscheidbare
// Bodenwirkung.
//
// Die Capture-Strecke aus G-001 laeuft im gebauten Spiel und besucht nur
// Zone_Greenwood. Fuer den Nachweis ueber alle Gebiete waere ein Spielstand je
// Gebiet noetig. Statt dessen rendert dieser Editor-Lauf jedes Gebiet direkt
// aus seiner Szene, mit fuer alle Gebiete identischer Kamerageometrie - nur so
// sind die Bilder untereinander vergleichbar.
//
// Kameralage wie Position 04/05 der G-001-Strecke: 52 Grad geneigt, 45 Grad
// gedreht, orthografische Groesse 6. Damit zeigen diese Bilder denselben
// Massstab wie die Abnahmebilder.
//
// Aufruf (Batchmode ohne -nographics, sonst rendert nichts):
//   Unity.exe -batchmode -quit -projectPath . \
//     -executeMethod Eidren.Editor.G002ZoneGroundCapture.CaptureAll
public static class G002ZoneGroundCapture
{
	private const string OutputDir = "TempReview/G002-Zonen";
	private const int Width = 960;
	private const int Height = 540;
	private const float OrthoSize = 6f;

	// HomeBase liegt nicht unter Zone_*, gehoert aber dazu: v0.2 hat acht
	// Gebiete. Der Auftragstext nennt vier; das ist gegenueber dem Projekt
	// veraltet.
	private static readonly string[] Scenes =
	{
		"Assets/_Game/Scenes/Zone_Greenwood.unity",
		"Assets/_Game/Scenes/Zone_Marsh.unity",
		"Assets/_Game/Scenes/Zone_Quarry.unity",
		"Assets/_Game/Scenes/Zone_EmberRuins.unity",
		"Assets/_Game/Scenes/Zone_TwilightGrove.unity",
		"Assets/_Game/Scenes/Zone_VeilMarsh.unity",
		"Assets/_Game/Scenes/Zone_GreyRifts.unity",
		"Assets/_Game/Scenes/HomeBase.unity"
	};

	public static void CaptureAll()
	{
		Directory.CreateDirectory(OutputDir);

		foreach (string scenePath in Scenes)
		{
			string zone = Path.GetFileNameWithoutExtension(scenePath).Replace("Zone_", "");
			EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

			GameObject camObject = new GameObject("G002CaptureCamera");
			Camera cam = camObject.AddComponent<Camera>();
			cam.orthographic = true;
			cam.orthographicSize = OrthoSize;
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = Color.black;
			cam.nearClipPlane = 0.1f;
			cam.farClipPlane = 200f;
			cam.transform.rotation = Quaternion.Euler(52f, 45f, 0f);

			// Blick auf einen Punkt neben der Mitte: die Mitte traegt in
			// mehreren Gebieten gesetzte Requisiten, der Rand traegt die
			// Gebietskante. Der Versatz haelt beides aus dem Bild.
			Vector3 focus = new Vector3(-13f, 0f, -7f);
			cam.transform.position = focus - cam.transform.forward * 30f;

			List<GameObject> cover = FindGroundCover();

			// Zwei Bilder je Gebiet. Ohne Bewuchs zeigt sich das Bodenmaterial
			// selbst, mit Bewuchs die Wirkung, die der Spieler sieht. Kriterium 7
			// wird am Material gemessen; das Gesamtbild belegt zusaetzlich
			// Kriterium 5 fuer jedes Gebiet statt nur fuer Greenwood.
			SetActive(cover, false);
			Render(cam, Path.Combine(OutputDir, zone + "_boden.png"));

			SetActive(cover, true);
			Render(cam, Path.Combine(OutputDir, zone + "_gesamt.png"));

			Object.DestroyImmediate(camObject);
			Debug.Log($"G-002: {zone} gerendert ({cover.Count} Bewuchsknoten).");
		}

		Debug.Log("G-002 ZONENBILDER: fertig.");
	}

	private static void SetActive(List<GameObject> objects, bool active)
	{
		foreach (GameObject o in objects)
		{
			if (o != null) { o.SetActive(active); }
		}
	}

	private static List<GameObject> FindGroundCover()
	{
		List<GameObject> found = new List<GameObject>();
		foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
		{
			foreach (Transform t in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (t.name == AreaArtGroundCoverBuilder.RootName)
				{
					found.Add(t.gameObject);
				}
			}
		}
		return found;
	}

	private static void Render(Camera cam, string path)
	{
		RenderTexture target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
		target.antiAliasing = 1;
		cam.targetTexture = target;
		cam.Render();

		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = target;
		Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, mipChain: false);
		image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
		image.Apply();
		RenderTexture.active = previous;

		File.WriteAllBytes(path, image.EncodeToPNG());

		cam.targetTexture = null;
		Object.DestroyImmediate(image);
		target.Release();
		Object.DestroyImmediate(target);
	}
}
}
