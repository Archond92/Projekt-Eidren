using Eidren.AI;
using Eidren.Composition;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// In-Welt-Abnahme der 3D-Kreaturen: laedt echte Zonen mit laufender
	/// Komposition (Spieler, Bodenschatten, Eidra-Populatoren, Zonenlicht)
	/// und rendert die ECHTE Spielkamera in PNGs. Die Studio-Captures der
	/// Sichtpruefung zeigen die Figur — erst dieses Bild zeigt das Spiel.
	///
	/// Kein Suitentest: laeuft nur, wenn EIDREN_CAPTURE_ORDNER gesetzt ist
	/// (sonst Ignore), zusaetzlich Explicit. Aufruf:
	///   -runTests -testPlatform PlayMode -testFilter KreaturenInWeltCaptureTests
	///   OHNE -nographics (sonst kein Grafikgeraet).
	/// </summary>
	public sealed class KreaturenInWeltCaptureTests
	{
		private const int Breite = 1280;
		private const int Hoehe = 720;

		[UnityTest]
		[Explicit("Nur fuer Capture-Laeufe — schreibt PNGs, prueft keine Regel.")]
		public IEnumerator KreaturenInWelt_Captures()
		{
			string ordner = Environment.GetEnvironmentVariable("EIDREN_CAPTURE_ORDNER");
			if (string.IsNullOrEmpty(ordner))
			{
				Assert.Ignore("EIDREN_CAPTURE_ORDNER nicht gesetzt — Capture-Lauf nur auf Anforderung.");
			}
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
			{
				Assert.Ignore("In-Welt-Captures brauchen ein echtes Grafikgeraet (Lauf ohne -nographics).");
			}
			Directory.CreateDirectory(ordner);

			// Greenwood: der Standard-Wildling im Spielbild, plus Uebersicht.
			yield return LadeZone("Zone_Greenwood");
			PlayerPrefabBindings player = null;
			yield return WarteAufSpieler(p => player = p);
			WildlingController wildling = FindeNaechsten<WildlingController>(new Vector3(10f, 0f, 12f));
			if (wildling != null)
			{
				yield return StelleVor(player, wildling.transform.position);
				yield return Rendere(ordner, "greenwood_wildling.png");
			}
			MovePlayer(player, new Vector3(-5f, 0.05f, 12f));
			yield return WarteFrames(30);
			yield return Rendere(ordner, "greenwood_uebersicht.png");

			// GreyRifts: je ein Bild pro szenenplatziertem Gegnertyp
			// (Rissling, Wurzelstuermer, Moorwerfer, Granitpanzer, Wildling).
			yield return LadeZone("Zone_GreyRifts");
			yield return WarteAufSpieler(p => player = p);
			var gesehen = new HashSet<string>(StringComparer.Ordinal);
			foreach (WildlingController gegner in UnityEngine.Object.FindObjectsByType<WildlingController>(
				FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				string id = (gegner.Definition != null) ? gegner.Definition.Id : "unbekannt";
				if (!gesehen.Add(id))
				{
					continue;
				}
				yield return StelleVor(player, gegner.transform.position);
				yield return Rendere(ordner, "greyrifts_" + id.Replace("enemy.", "") + ".png");
			}

			// Eidraschmiede: der gedrungene Glutzehrer im echten Forge-Licht.
			yield return LadeZone("EidraForge");
			yield return WarteAufSpieler(p => player = p);
			WildlingController emberEater = null;
			foreach (WildlingController gegner in UnityEngine.Object.FindObjectsByType<WildlingController>(
				FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				if (gegner.Definition != null && gegner.Definition.Id == "enemy.ember_eater")
				{
					emberEater = gegner;
					break;
				}
			}
			if (emberEater != null)
			{
				yield return StelleVor(player, emberEater.transform.position);
				yield return Rendere(ordner, "forge_embereater.png");
			}
			else
			{
#if UNITY_EDITOR
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
					"Assets/_Game/Prefabs/Actors/3D/EmberEater_3D.prefab");
				GameObject captureInstance = UnityEngine.Object.Instantiate(prefab);
				captureInstance.transform.position = player.transform.position + Vector3.left * 1.5f;
				Camera forgeCamera = Camera.main;
				Vector3 cameraPosition = forgeCamera.transform.position;
				Quaternion cameraRotation = forgeCamera.transform.rotation;
				forgeCamera.transform.position = captureInstance.transform.position + new Vector3(3.2f, 2.8f, 4.2f);
				forgeCamera.transform.LookAt(captureInstance.transform.position + Vector3.up * 0.8f);
				yield return Rendere(ordner, "forge_embereater.png");
				forgeCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
				UnityEngine.Object.Destroy(captureInstance);
#else
				Debug.LogWarning("[K3D-InWelt] kein EmberEater in EidraForge gefunden.");
#endif
			}

			WildlingController forgeGuardian = null;
			foreach (WildlingController gegner in UnityEngine.Object.FindObjectsByType<WildlingController>(
				FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				if (gegner.Definition != null && gegner.Definition.Id == "enemy.forge_guardian")
				{
					forgeGuardian = gegner;
					break;
				}
			}
			if (forgeGuardian != null)
			{
				yield return StelleVor(player, forgeGuardian.transform.position);
				yield return Rendere(ordner, "forge_forgeguardian.png");
			}
			else
			{
#if UNITY_EDITOR
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
					"Assets/_Game/Prefabs/Actors/3D/ForgeGuardian_3D.prefab");
				GameObject captureInstance = UnityEngine.Object.Instantiate(prefab);
				captureInstance.transform.position = player.transform.position + Vector3.left * 1.8f;
				captureInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
				yield return null;
				Renderer[] captureRenderers = captureInstance.GetComponentsInChildren<Renderer>();
				Assert.That(captureRenderers, Is.Not.Empty, "ForgeGuardian hat keine Renderer.");
				Bounds captureBounds = captureRenderers[0].bounds;
				for (int i = 1; i < captureRenderers.Length; i++)
				{
					captureBounds.Encapsulate(captureRenderers[i].bounds);
				}
				Camera forgeCamera = Camera.main;
				Vector3 cameraPosition = forgeCamera.transform.position;
				Quaternion cameraRotation = forgeCamera.transform.rotation;
				float cameraSize = forgeCamera.orthographicSize;
				forgeCamera.orthographicSize = Mathf.Max(1.7f, captureBounds.extents.y * 1.28f);
				forgeCamera.transform.position = captureBounds.center + new Vector3(3.8f, 3.0f, 4.6f);
				forgeCamera.transform.LookAt(captureBounds.center);
				GameObject lightObject = new GameObject("ForgeGuardianCaptureLight");
				Light captureLight = lightObject.AddComponent<Light>();
				captureLight.type = LightType.Point;
				captureLight.color = new Color(1f, 0.66f, 0.38f);
				captureLight.intensity = 18f;
				captureLight.range = 9f;
				lightObject.transform.position = captureBounds.center + new Vector3(-1f, 2.2f, -2f);
				yield return Rendere(ordner, "forge_forgeguardian.png", warteFrame: false);
				forgeCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
				forgeCamera.orthographicSize = cameraSize;
				UnityEngine.Object.Destroy(lightObject);
				UnityEngine.Object.Destroy(captureInstance);
#else
				Debug.LogWarning("[K3D-InWelt] kein ForgeGuardian in EidraForge gefunden.");
#endif
			}

			WildlingController sealGuardian = null;
			foreach (WildlingController gegner in UnityEngine.Object.FindObjectsByType<WildlingController>(
				FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				if (gegner.Definition != null && gegner.Definition.Id == "enemy.seal_guardian")
				{
					sealGuardian = gegner;
					break;
				}
			}
			if (sealGuardian != null)
			{
				yield return StelleVor(player, sealGuardian.transform.position);
				yield return Rendere(ordner, "forge_sealguardian.png");
			}
			else
			{
#if UNITY_EDITOR
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
					"Assets/_Game/Prefabs/Actors/3D/SealGuardian_3D.prefab");
				Assert.That(prefab, Is.Not.Null, "Produktiver SealGuardian-Visual-Prefab fehlt.");
				GameObject captureInstance = UnityEngine.Object.Instantiate(prefab);
				captureInstance.transform.position = player.transform.position + Vector3.left * 1.8f;
				captureInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
				yield return null;
				Renderer[] captureRenderers = captureInstance.GetComponentsInChildren<Renderer>();
				Assert.That(captureRenderers, Is.Not.Empty, "SealGuardian hat keine Renderer.");
				Bounds captureBounds = captureRenderers[0].bounds;
				for (int i = 1; i < captureRenderers.Length; i++)
				{
					captureBounds.Encapsulate(captureRenderers[i].bounds);
				}
				Camera forgeCamera = Camera.main;
				Vector3 cameraPosition = forgeCamera.transform.position;
				Quaternion cameraRotation = forgeCamera.transform.rotation;
				float cameraSize = forgeCamera.orthographicSize;
				forgeCamera.orthographicSize = Mathf.Max(1.9f, captureBounds.extents.y * 1.2f);
				forgeCamera.transform.position = captureBounds.center + new Vector3(4.2f, 3.5f, 5.0f);
				forgeCamera.transform.LookAt(captureBounds.center);
				GameObject lightObject = new GameObject("SealGuardianCaptureLight");
				Light captureLight = lightObject.AddComponent<Light>();
				captureLight.type = LightType.Point;
				captureLight.color = new Color(1f, 0.66f, 0.38f);
				captureLight.intensity = 18f;
				captureLight.range = 9f;
				lightObject.transform.position = captureBounds.center + new Vector3(-1f, 2.4f, -2f);
				yield return Rendere(ordner, "forge_sealguardian.png", warteFrame: false);
				forgeCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
				forgeCamera.orthographicSize = cameraSize;
				UnityEngine.Object.Destroy(lightObject);
				UnityEngine.Object.Destroy(captureInstance);
#else
				Debug.LogWarning("[K3D-InWelt] kein SealGuardian in EidraForge gefunden.");
#endif
			}

			WildlingController ashRunner = null;
			foreach (WildlingController gegner in UnityEngine.Object.FindObjectsByType<WildlingController>(
				FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				if (gegner.Definition != null && gegner.Definition.Id == "enemy.ash_runner")
				{
					ashRunner = gegner;
					break;
				}
			}
			if (ashRunner != null)
			{
				yield return StelleVor(player, ashRunner.transform.position);
				yield return Rendere(ordner, "forge_ashrunner.png");
			}
			else
			{
#if UNITY_EDITOR
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
					"Assets/_Game/Prefabs/Actors/3D/AshRunner_3D.prefab");
				Assert.That(prefab, Is.Not.Null, "Produktiver AshRunner-Visual-Prefab fehlt.");
				GameObject captureInstance = UnityEngine.Object.Instantiate(prefab);
				captureInstance.transform.position = player.transform.position + Vector3.left * 1.8f;
				captureInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
				Renderer[] captureRenderers = captureInstance.GetComponentsInChildren<Renderer>();
				Assert.That(captureRenderers, Is.Not.Empty, "AshRunner hat keine Renderer.");
				Bounds captureBounds = captureRenderers[0].bounds;
				for (int i = 1; i < captureRenderers.Length; i++)
				{
					captureBounds.Encapsulate(captureRenderers[i].bounds);
				}
				Camera forgeCamera = Camera.main;
				Vector3 cameraPosition = forgeCamera.transform.position;
				Quaternion cameraRotation = forgeCamera.transform.rotation;
				float cameraSize = forgeCamera.orthographicSize;
				forgeCamera.orthographicSize = Mathf.Max(1.7f, captureBounds.extents.y * 1.35f);
				forgeCamera.transform.position = captureBounds.center + new Vector3(3.8f, 2.7f, 4.8f);
				forgeCamera.transform.LookAt(captureBounds.center);
				GameObject lightObject = new GameObject("AshRunnerCaptureLight");
				Light captureLight = lightObject.AddComponent<Light>();
				captureLight.type = LightType.Point;
				captureLight.color = new Color(1f, 0.66f, 0.38f);
				captureLight.intensity = 18f;
				captureLight.range = 9f;
				lightObject.transform.position = captureBounds.center + new Vector3(-1f, 2.0f, -2f);
				yield return Rendere(ordner, "forge_ashrunner.png", warteFrame: false);
				forgeCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
				forgeCamera.orthographicSize = cameraSize;
				UnityEngine.Object.Destroy(lightObject);
				UnityEngine.Object.Destroy(captureInstance);
#else
				Debug.LogWarning("[K3D-InWelt] kein AshRunner in EidraForge gefunden.");
#endif
			}

			// Marsh und Quarry: die wilden Eidra (Noctarion, Terrock) — hier
			// laeuft der Laufzeitweg der Controller samt UiAccent-Toenung.
			yield return LadeZone("Zone_Marsh");
			yield return WarteAufSpieler(p => player = p);
			yield return CaptureEidra(player, ordner, "marsh_noctarion.png");

			yield return LadeZone("Zone_Quarry");
			yield return WarteAufSpieler(p => player = p);
			yield return CaptureEidra(player, ordner, "quarry_terrock.png");
		}

		private static IEnumerator CaptureEidra(PlayerPrefabBindings player, string ordner, string datei)
		{
			EidraWildController eidra = null;
			float timeout = Time.realtimeSinceStartup + 10f;
			while (eidra == null && Time.realtimeSinceStartup < timeout)
			{
				eidra = UnityEngine.Object.FindFirstObjectByType<EidraWildController>();
				yield return null;
			}
			if (eidra == null)
			{
				Debug.LogWarning("[K3D-InWelt] keine wilde Eidra gefunden — " + datei + " uebersprungen.");
				yield break;
			}
			yield return StelleVor(player, eidra.transform.position);
			yield return Rendere(ordner, datei);
		}

		private static IEnumerator LadeZone(string name)
		{
			AsyncOperation op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
			Assert.That(op, Is.Not.Null, name);
			while (!op.isDone)
			{
				yield return null;
			}
			for (int i = 0; i < 3; i++)
			{
				yield return null;
			}
		}

		private static IEnumerator WarteAufSpieler(Action<PlayerPrefabBindings> setze)
		{
			PlayerPrefabBindings player = null;
			float timeout = Time.realtimeSinceStartup + 12f;
			while (Time.realtimeSinceStartup < timeout)
			{
				player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
				if (player != null && Camera.main != null)
				{
					break;
				}
				yield return null;
			}
			Assert.That(player, Is.Not.Null, "Spieler ist nicht gespawnt.");
			Assert.That(Camera.main, Is.Not.Null, "Keine Spielkamera.");
			setze(player);
		}

		/// <summary>
		/// Spieler kurz vor das Ziel setzen und der Folgekamera Zeit geben —
		/// dieselbe Mechanik wie StyleProofCaptureRunner.MovePlayer.
		/// </summary>
		private static IEnumerator StelleVor(PlayerPrefabBindings player, Vector3 ziel)
		{
			Vector3 weg = player.transform.position - ziel;
			weg.y = 0f;
			if (weg.sqrMagnitude < 0.1f)
			{
				weg = Vector3.back;
			}
			MovePlayer(player, ziel + weg.normalized * 3.6f);
			yield return WarteFrames(30);
		}

		private static void MovePlayer(PlayerPrefabBindings player, Vector3 position)
		{
			CharacterController controller = player.CharacterController;
			if (controller != null)
			{
				controller.enabled = false;
			}
			player.transform.position = position;
			if (controller != null)
			{
				controller.enabled = true;
			}
		}

		private static IEnumerator WarteFrames(int n)
		{
			for (int i = 0; i < n; i++)
			{
				yield return null;
			}
		}

		/// <summary>
		/// Die ECHTE Spielkamera in eine RenderTexture — ScreenCapture braucht
		/// ein Fenster, das es im Batchmode nicht gibt; Camera.Render auf RT
		/// funktioniert headless mit Grafikgeraet (Muster des Terrock-Tests).
		/// </summary>
		private static IEnumerator Rendere(string ordner, string datei, bool warteFrame = true)
		{
			if (warteFrame)
			{
				yield return null;
			}
			Camera kamera = Camera.main;
			Assert.That(kamera, Is.Not.Null, datei);
			var ziel = new RenderTexture(Breite, Hoehe, 24, RenderTextureFormat.ARGB32);
			RenderTexture vorherAktiv = RenderTexture.active;
			RenderTexture vorherZiel = kamera.targetTexture;
			try
			{
				kamera.targetTexture = ziel;
				kamera.Render();
				RenderTexture.active = ziel;
				var bild = new Texture2D(Breite, Hoehe, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, Breite, Hoehe), 0, 0);
				bild.Apply();
				File.WriteAllBytes(Path.Combine(ordner, datei), bild.EncodeToPNG());
				UnityEngine.Object.Destroy(bild);
				Debug.Log("[K3D-InWelt] geschrieben: " + datei);
			}
			finally
			{
				kamera.targetTexture = vorherZiel;
				RenderTexture.active = vorherAktiv;
				ziel.Release();
				UnityEngine.Object.Destroy(ziel);
			}
		}

		private static T FindeNaechsten<T>(Vector3 position) where T : Component
		{
			T bester = null;
			float bestes = float.PositiveInfinity;
			foreach (T kandidat in UnityEngine.Object.FindObjectsByType<T>(
				FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				float d = (kandidat.transform.position - position).sqrMagnitude;
				if (d < bestes)
				{
					bestes = d;
					bester = kandidat;
				}
			}
			return bester;
		}
	}
}
