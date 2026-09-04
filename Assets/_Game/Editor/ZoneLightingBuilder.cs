// G-003: Beleuchtung, Tonwertaufbau und Bildabstimmung je Zone.
//
// Herleitung der Sonnenwerte: Documentation/Etappen/G00x/G003_ENTWURF.md,
// Abschnitte 2 und 3. Der Bildwinkel 113,5 Grad ist gemessen
// (Tools/Measure-LightDirection.ps1), die Tiefe -0,35 ist gesetzt und in
// Abschnitt 3.2 begruendet. Die Euler-Werte folgen daraus per
// Tools/Convert-LightAngle.ps1 und sind NICHT frei abstimmbar: wer die
// Richtung aendert, bricht Abnahmekriterium 1.
//
// Zwei Abnehmer, eine Quelle: EidrenSceneStructureBuilder.CreateZoneLight
// (neue Szenen) und ApplyAll hier (bestehende Szenen) lesen dieselben
// Konstanten.
//
// Zum Tonemapping: Das Projekt laeuft im Gamma-Farbraum mit LDR-Grading
// (m_ColorGradingMode 0). URP baut die Grading-LUT dann ohne Tonemapper -
// eine Tonemapping-Komponente waere wirkungslos. Das ACES des StyleProof-
// Volumes war aus genau diesem Grund immer ein Leerlauf. Schwarz- und
// Weisspunkt setzt deshalb LiftGammaGain, das im LDR-Weg arbeitet.

using Eidren.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Eidren.Editor
{
public static class ZoneLightingBuilder
{
	// === Gemeinsame Sonnenwerte (G003_ENTWURF.md Abschnitt 3.2) ===
	//
	// Tiefe -0,678 wie der Ist-Stand: HandPaintedLitSprite bildet die
	// Figurennormale aus der Billboard-Flaeche, N*L der Figuren ist damit
	// genau der negative Tiefenanteil. Wer die Tiefe aendert, aendert die
	// Belichtung jeder Figur. Korrigiert wird nur der Bildwinkel:
	// 27,5 -> 113,5 Grad.

	public static readonly Vector3 SunEuler = new Vector3(71.7f, 156.2f, 0f);

	// Unveraendert zum Ist-Stand, damit die Figurenbelichtung stabil bleibt.
	// Der Boden wechselt zugleich von unbeleuchtet auf beleuchtet; sein
	// Helligkeitsabgleich laeuft ueber die Trilight-Werte unten und wird an
	// den Abnahmemessungen kalibriert, nicht ueber diese Konstante.
	public const float SunIntensity = 1.15f;

	// G-004: Weich statt hart. Die Begruendung der Vorfassung ("weiche Schatten
	// sind im URP-Asset abgeschaltet") ist ueberholt — G-004 setzt
	// m_SoftShadowsSupported auf 1. Seit dem Stilumbau und dem Wanderer3D-Einbau
	// sind Spielfigur und alle 80 Weltobjekte echte 3D-Geometrie mit
	// ShadowCaster-Pass; der Wurf traegt damit Formmodellierung, Bewegung,
	// Sprung und Ausweichrolle ohne eigenen Code.
	//
	// Staerke bleibt bei 0,55: Die Kontaktabdunklung liegt weiterhin zusaetzlich
	// unter den Objekten, und bei 71,7 Grad Sonnenhoehe ist der Wurf ohnehin kurz
	// (ein 5-m-Baum wirft rund 1,65 m). SunEuler bleibt unangetastet —
	// G-003-Invariante, Bildwinkel 113,5 Grad und N*L -0,678 der Figuren.
	public const LightShadows SunShadows = LightShadows.Soft;
	public const float SunShadowStrength = 0.55f;

	private sealed class ZoneMood
	{
		public string SceneName;
		public string AreaKey;

		// Benannte Farbstimmung - Abnahmekriterium 5 verlangt den Namen.
		public string MoodName;

		// Trilight-Umgebungslicht
		public Color Sky;
		public Color Equator;
		public Color Ground;

		// Bildabstimmung
		public float Contrast;
		public float Saturation;
		public Color Filter;
		public Vector4 Lift;
		public Vector4 Gamma;
		public Vector4 Gain;
		public float Vignette;
	}

	// Die Umgebungsfarben sind aus zwei vorhandenen Datenquellen abgeleitet:
	// Himmel aus der Lichtfarbe (AreaArt-Definition), Mitte und Boden aus der
	// Hintergrundfarbe derselben Definition. Startregel: Himmel = Licht * 0,35,
	// Mitte = Hintergrund * 1,2, Boden = Hintergrund * 0,65. Die Zahlen unten
	// sind das ausgerechnete Ergebnis dieser Regel und werden bei der Abnahme
	// als Ganzes verschoben, nicht einzeln verbogen.
	private static readonly ZoneMood[] Moods = new ZoneMood[]
	{
		new ZoneMood
		{
			SceneName = "Zone_Greenwood", AreaKey = "Greenwood",
			MoodName = "Blattgold",
			Sky = new Color(0.3f, 0.33f, 0.27f),
			Equator = new Color(0.12f, 0.19f, 0.14f),
			Ground = new Color(0.07f, 0.1f, 0.08f),
			Contrast = 12f, Saturation = 4f,
			Filter = new Color(0.99f, 1f, 0.95f),
			Lift = new Vector4(0.98f, 1f, 0.99f, -0.01f),
			Gamma = new Vector4(1f, 1f, 1f, 0f),
			Gain = new Vector4(1f, 1f, 0.97f, 0.06f),
			Vignette = 0.1f,
		},
		new ZoneMood
		{
			SceneName = "Zone_Marsh", AreaKey = "Marsh",
			MoodName = "Nebelgruen",
			// Bewusst warm-oliv gehalten: das Schwesterpaar VeilMarsh liegt
			// im kalten Blau. Erste Fassung beider Stimmungen lag gerendert
			// nur 9,3 Farbabstand auseinander - die beiden Suempfe muessen
			// in entgegengesetzte Temperaturen.
			Sky = new Color(0.24f, 0.27f, 0.22f),
			Equator = new Color(0.15f, 0.19f, 0.16f),
			Ground = new Color(0.08f, 0.1f, 0.08f),
			Contrast = 10f, Saturation = 2f,
			Filter = new Color(0.99f, 1f, 0.93f),
			Lift = new Vector4(0.98f, 1f, 0.96f, -0.01f),
			Gamma = new Vector4(1f, 1f, 1f, 0f),
			Gain = new Vector4(0.99f, 1f, 0.95f, 0.05f),
			Vignette = 0.12f,
		},
		new ZoneMood
		{
			SceneName = "Zone_Quarry", AreaKey = "Quarry",
			MoodName = "Steinocker",
			Sky = new Color(0.32f, 0.29f, 0.23f),
			Equator = new Color(0.24f, 0.23f, 0.2f),
			Ground = new Color(0.13f, 0.12f, 0.11f),
			Contrast = 12f, Saturation = 2f,
			Filter = new Color(1f, 0.98f, 0.94f),
			Lift = new Vector4(1f, 0.99f, 0.97f, -0.01f),
			Gamma = new Vector4(1f, 1f, 1f, 0f),
			Gain = new Vector4(1f, 0.99f, 0.95f, 0.06f),
			Vignette = 0.1f,
		},
		new ZoneMood
		{
			SceneName = "Zone_EmberRuins", AreaKey = "EmberRuins",
			MoodName = "Glutrand",
			Sky = new Color(0.35f, 0.18f, 0.1f),
			Equator = new Color(0.19f, 0.1f, 0.07f),
			Ground = new Color(0.1f, 0.05f, 0.04f),
			Contrast = 14f, Saturation = 6f,
			Filter = new Color(1f, 0.94f, 0.88f),
			Lift = new Vector4(1f, 0.97f, 0.95f, -0.02f),
			Gamma = new Vector4(1f, 1f, 1f, 0f),
			Gain = new Vector4(1f, 0.96f, 0.9f, 0.07f),
			Vignette = 0.14f,
		},
		new ZoneMood
		{
			SceneName = "Zone_TwilightGrove", AreaKey = "TwilightGrove",
			MoodName = "Daemmermoos",
			Sky = new Color(0.17f, 0.24f, 0.18f),
			Equator = new Color(0.04f, 0.09f, 0.07f),
			Ground = new Color(0.02f, 0.05f, 0.04f),
			Contrast = 10f, Saturation = 0f,
			Filter = new Color(0.95f, 1f, 0.96f),
			Lift = new Vector4(0.97f, 1f, 0.98f, -0.02f),
			Gamma = new Vector4(1f, 1f, 1f, 0f),
			Gain = new Vector4(0.96f, 1f, 0.97f, 0.04f),
			Vignette = 0.14f,
		},
		new ZoneMood
		{
			SceneName = "Zone_VeilMarsh", AreaKey = "VeilMarsh",
			MoodName = "Schleierblau",
			// Gegenpol zu Marsh (siehe dort): entschieden kalt-blau.
			Sky = new Color(0.15f, 0.21f, 0.27f),
			Equator = new Color(0.06f, 0.11f, 0.16f),
			Ground = new Color(0.03f, 0.06f, 0.09f),
			Contrast = 10f, Saturation = 0f,
			Filter = new Color(0.91f, 0.97f, 1f),
			Lift = new Vector4(0.94f, 0.98f, 1f, -0.02f),
			Gamma = new Vector4(1f, 1f, 1f, 0f),
			Gain = new Vector4(0.93f, 0.98f, 1f, 0.04f),
			Vignette = 0.13f,
		},
		new ZoneMood
		{
			SceneName = "Zone_GreyRifts", AreaKey = "GreyRifts",
			MoodName = "Kaltschiefer",
			Sky = new Color(0.25f, 0.24f, 0.24f),
			Equator = new Color(0.14f, 0.14f, 0.16f),
			Ground = new Color(0.07f, 0.08f, 0.09f),
			Contrast = 12f, Saturation = -4f,
			Filter = new Color(0.97f, 0.98f, 1f),
			Lift = new Vector4(0.97f, 0.98f, 1f, -0.02f),
			Gamma = new Vector4(1f, 1f, 1f, 0f),
			Gain = new Vector4(0.97f, 0.98f, 1f, 0.05f),
			Vignette = 0.12f,
		},
		new ZoneMood
		{
			SceneName = "HomeBase", AreaKey = "HomeBase",
			MoodName = "Herdlicht",
			Sky = new Color(0.33f, 0.31f, 0.24f),
			Equator = new Color(0.22f, 0.25f, 0.19f),
			Ground = new Color(0.12f, 0.14f, 0.1f),
			Contrast = 10f, Saturation = 4f,
			Filter = new Color(1f, 0.98f, 0.93f),
			Lift = new Vector4(1f, 0.99f, 0.96f, -0.01f),
			Gamma = new Vector4(1f, 1f, 1f, 0f),
			Gain = new Vector4(1f, 0.98f, 0.94f, 0.06f),
			Vignette = 0.08f,
		},
	};

	[MenuItem("Eidren/Art/Lighting/Apply Zone Lighting (G-003)")]
	public static void ApplyAll()
	{
		foreach (ZoneMood mood in Moods)
		{
			Apply(mood);
		}
		AssetDatabase.SaveAssets();
	}

	// Einstieg fuer den Batchlauf:
	//   Unity.exe -batchmode -executeMethod Eidren.Editor.ZoneLightingBuilder.ApplyAllForAutomation
	public static void ApplyAllForAutomation()
	{
		ApplyAll();
	}

	private static void Apply(ZoneMood mood)
	{
		string scenePath = "Assets/_Game/Scenes/" + mood.SceneName + ".unity";
		Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

		ZoneAreaArtDefinition areaArt = AssetDatabase.LoadAssetAtPath<ZoneAreaArtDefinition>(
			"Assets/_Game/Data/AreaArt/AreaArt_" + mood.AreaKey + ".asset");
		if (areaArt == null)
		{
			throw new InvalidOperationException("Missing area art for " + mood.AreaKey + ".");
		}

		ConfigureSun(scene, areaArt, mood.SceneName);
		ConfigureCamera(scene, mood.SceneName);
		ConfigureAmbient(mood);
		FillVolumeProfile(mood);
		AddDecorationContactShadows(scene);

		EditorSceneManager.MarkSceneDirty(scene);
		if (!EditorSceneManager.SaveScene(scene, scenePath))
		{
			throw new InvalidOperationException("Could not save " + scenePath + ".");
		}
	}

	private static void ConfigureSun(Scene scene, ZoneAreaArtDefinition areaArt, string sceneName)
	{
		// Nicht ueber den Namen "Sun" suchen, sondern ueber den Typ: die Szene
		// besitzt genau ein Richtungslicht. Punktlichter (etwa aus
		// EnemyLootContainer) entstehen erst zur Laufzeit.
		Light[] lights = scene.GetRootGameObjects()
			.SelectMany((GameObject root) => root.GetComponentsInChildren<Light>(includeInactive: true))
			.Where((Light light) => light.type == LightType.Directional)
			.ToArray();
		if (lights.Length != 1)
		{
			throw new InvalidOperationException(
				sceneName + " has " + lights.Length + " directional lights; expected exactly one.");
		}

		Light sun = lights[0];
		sun.transform.rotation = Quaternion.Euler(SunEuler);
		sun.intensity = SunIntensity;
		sun.color = areaArt.LightColor;
		sun.shadows = SunShadows;
		sun.shadowStrength = SunShadowStrength;
	}

	private static void ConfigureCamera(Scene scene, string sceneName)
	{
		// Ohne dieses Flag rendert URP die Volume-Profile nicht - der Grund,
		// warum die leeren Profile nie aufgefallen sind.
		Camera[] cameras = scene.GetRootGameObjects()
			.SelectMany((GameObject root) => root.GetComponentsInChildren<Camera>(includeInactive: true))
			.Where((Camera cam) => cam.CompareTag("MainCamera"))
			.ToArray();
		if (cameras.Length != 1)
		{
			throw new InvalidOperationException(
				sceneName + " has " + cameras.Length + " main cameras; expected exactly one.");
		}
		cameras[0].GetUniversalAdditionalCameraData().renderPostProcessing = true;
	}

	// G-003 Abschnitt 4.4: Kontaktabdunklung unter den gebackenen
	// Dekorationsinstanzen. Die Ressourcen-Prefabs versorgt
	// WorldContactShadowBuilder.BuildAll direkt.
	private static void AddDecorationContactShadows(Scene scene)
	{
		Material material = WorldContactShadowBuilder.EnsureMaterial();
		foreach (GameObject root in scene.GetRootGameObjects())
		{
			foreach (Transform areaRoot in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (areaRoot.name == AreaArtSceneBuilder.RootName)
				{
					WorldContactShadowBuilder.AddToDecorations(areaRoot, material);
				}
			}
		}
	}

	private static void ConfigureAmbient(ZoneMood mood)
	{
		// RenderSettings gehoeren zur gerade geoeffneten Szene und werden mit
		// ihr gespeichert.
		RenderSettings.ambientMode = AmbientMode.Trilight;
		RenderSettings.ambientSkyColor = mood.Sky;
		RenderSettings.ambientEquatorColor = mood.Equator;
		RenderSettings.ambientGroundColor = mood.Ground;
	}

	private static void FillVolumeProfile(ZoneMood mood)
	{
		string path = "Assets/_Game/Settings/AreaArt/" + mood.AreaKey + "_AreaArt_Volume.asset";
		VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
		if (profile == null)
		{
			throw new InvalidOperationException("Missing volume profile: " + path);
		}
		profile.components.RemoveAll((VolumeComponent component) => component == null);

		if (!profile.TryGet<ColorAdjustments>(out var color))
		{
			color = AddVolumeComponent<ColorAdjustments>(profile);
		}
		color.contrast.Override(mood.Contrast);
		color.saturation.Override(mood.Saturation);
		color.colorFilter.Override(mood.Filter);

		if (!profile.TryGet<LiftGammaGain>(out var lgg))
		{
			lgg = AddVolumeComponent<LiftGammaGain>(profile);
		}
		lgg.lift.Override(mood.Lift);
		lgg.gamma.Override(mood.Gamma);
		lgg.gain.Override(mood.Gain);

		if (!profile.TryGet<Vignette>(out var vignette))
		{
			vignette = AddVolumeComponent<Vignette>(profile);
		}
		vignette.intensity.Override(mood.Vignette);
		vignette.smoothness.Override(0.4f);

		EditorUtility.SetDirty(profile);
	}

	private static T AddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
	{
		T component = ScriptableObject.CreateInstance<T>();
		component.name = typeof(T).Name;
		component.active = true;
		profile.components.Add(component);
		AssetDatabase.AddObjectToAsset(component, profile);
		EditorUtility.SetDirty(component);
		EditorUtility.SetDirty(profile);
		return component;
	}

	// Fuer die Abnahme: welche Stimmung traegt welche Zone.
	public static IEnumerable<(string SceneName, string MoodName)> MoodNames()
	{
		return Moods.Select((ZoneMood mood) => (mood.SceneName, mood.MoodName));
	}
}
}
