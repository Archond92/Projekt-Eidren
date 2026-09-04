using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Interaction;
using NUnit.Framework;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// Diagnose (18.08.2026, Tester: "Marken-Kisten zeigen keine Kosten"):
	/// sammelt im ECHT betretenen Verlies die Interaktionstexte aller
	/// Schmiede-Kisten und legt sie als Textbeleg ab — die Grundlage für
	/// die Entscheidung, wo der Preis sichtbar werden muss. Zusätzlich
	/// ein Bildbeleg des Verlies-Eingangs in den Glutruinen (Tester:
	/// "sieht nicht gut aus").
	/// </summary>
	public sealed class ForgeChestPromptDiagnoseTests
	{
		[UnityTest]
		public IEnumerator Verlies_KistenTexteUndEingangsbild()
		{
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			// Erst die Glutruinen: Bildbeleg des Verlies-Eingangs.
			yield return SceneManager.LoadSceneAsync("Zone_EmberRuins", LoadSceneMode.Single);
			float zeit = 0f;
			while (Object.FindFirstObjectByType<PlayerPrefabBindings>() == null && zeit < 20f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			EidraForgeEntrance eingang = Object.FindFirstObjectByType<EidraForgeEntrance>(FindObjectsInactive.Include);
			Assert.That(eingang, Is.Not.Null, "Die Glutruinen haben keinen Verlies-Eingang.");
			CaptureAround(eingang.transform.position, "TempReview/verlies-eingang-ist.png");

			// Dann die echte Verlies-Route für die Kistentexte.
			EidrenServiceRoot dienste = EidrenServiceRoot.Instance;
			GameSession session = dienste.GameSession;
			session.TrySetProgressFlag("garon_defeated");
			Assert.That(session.EidraForge.TryBeginFirstRun(7), Is.True, "Der Erstlauf ließ sich nicht beginnen.");
			Assert.That(dienste.SceneFlowService.TryLoadScene("EidraForge"), Is.True, "Der Szenenwechsel wurde abgelehnt.");
			zeit = 0f;
			PlayerPrefabBindings spieler = null;
			while (zeit < 25f)
			{
				zeit += Time.deltaTime;
				if (SceneManager.GetActiveScene().name == "EidraForge")
				{
					spieler = Object.FindFirstObjectByType<PlayerPrefabBindings>();
					if (spieler != null)
					{
						break;
					}
				}
				yield return null;
			}
			Assert.That(spieler, Is.Not.Null, "Die Schmiede hat keinen Spieler erzeugt.");
			zeit = 0f;
			while (zeit < 2f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			EidraForgeChestContainer[] kisten = Object.FindObjectsByType<EidraForgeChestContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			Assert.That(kisten.Length, Is.GreaterThan(0), "Das Verlies hat keine Schmiede-Kisten.");
			var bericht = new StringBuilder();
			bericht.AppendLine("Kisten-Interaktionstexte im echten Verlies (Erstlauf, Saat 7):");
			int marken = 0;
			foreach (EidraForgeChestContainer kiste in kisten)
			{
				string text = kiste.DisplayText;
				bericht.AppendLine($"- {kiste.ContainerId}: \"{text}\" @ {kiste.transform.position}");
				if (text.Contains("MARKEN"))
				{
					marken++;
				}
			}
			Directory.CreateDirectory("TempReview");
			File.WriteAllText("TempReview/verlies-kisten-texte.txt", bericht.ToString(), Encoding.UTF8);
			Debug.Log(bericht.ToString());
			// Kernfrage der Diagnose: Wie viele Kisten nennen den Markenpreis?
			Assert.That(marken, Is.GreaterThanOrEqualTo(0));
		}

		private static void CaptureAround(Vector3 ziel, string pfad)
		{
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
			{
				return;
			}
			Camera kamera = Camera.main;
			if (kamera == null)
			{
				return;
			}
			Vector3 vorherPosition = kamera.transform.position;
			Quaternion vorherRotation = kamera.transform.rotation;
			var zielTextur = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
			RenderTexture vorherAktiv = RenderTexture.active;
			RenderTexture vorherZiel = kamera.targetTexture;
			try
			{
				kamera.transform.position = ziel + new Vector3(-7f, 9f, -11f);
				kamera.transform.rotation = Quaternion.LookRotation(ziel + Vector3.up * 2.5f - kamera.transform.position);
				kamera.targetTexture = zielTextur;
				kamera.Render();
				RenderTexture.active = zielTextur;
				var bild = new Texture2D(1600, 900, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
				bild.Apply();
				Directory.CreateDirectory("TempReview");
				File.WriteAllBytes(pfad, bild.EncodeToPNG());
				Object.Destroy(bild);
			}
			finally
			{
				kamera.targetTexture = vorherZiel;
				RenderTexture.active = vorherAktiv;
				kamera.transform.SetPositionAndRotation(vorherPosition, vorherRotation);
				zielTextur.Release();
				Object.Destroy(zielTextur);
			}
		}
	}
}
