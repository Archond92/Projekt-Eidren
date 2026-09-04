using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Interaction;
using Eidren.UI;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// Tester-Runde 19.08.2026, drei Verlies-Verbesserungen auf der echten
	/// Route: (1) Die drei Schmiedekisten tragen ein Weltlabel mit dem
	/// Markenpreis, solange der Kauf ansteht. (2) Am Windfang steht ein
	/// Ausgang; sein Abschluss lädt die Glutruinen und setzt den Spieler
	/// direkt ans Verlies-Portal. (3) Der Eingang in den Glutruinen ist
	/// ein Felsportal (Bildbeleg) statt roter Scheibe mit gequetschtem
	/// Quader.
	/// </summary>
	public sealed class ForgeQualityOfLifeTests
	{
		[UnityTest]
		public IEnumerator Verlies_Preislabels_Ausgang_UndPortalbild()
		{
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			yield return SceneManager.LoadSceneAsync("Zone_Greenwood", LoadSceneMode.Single);
			float zeit = 0f;
			while (Object.FindFirstObjectByType<PlayerPrefabBindings>() == null && zeit < 20f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
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

			// (1) Preislabels: jede Schmiedekiste trägt ein aktives Weltlabel
			// mit ihrem Markenpreis; Gewölbekisten bleiben ohne.
			EidraForgeChestContainer[] kisten = Object.FindObjectsByType<EidraForgeChestContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			int preislabels = 0;
			foreach (EidraForgeChestContainer kiste in kisten)
			{
				ForgeChestPriceLabel label = kiste.GetComponentInChildren<ForgeChestPriceLabel>(includeInactive: true);
				if (kiste.RequiresMarkPayment)
				{
					Assert.That(label, Is.Not.Null, kiste.ContainerId + ": Schmiedekiste ohne Preislabel.");
					Text text = label.GetComponentInChildren<Text>(includeInactive: true);
					Assert.That(text, Is.Not.Null, kiste.ContainerId + ": Preislabel ohne Text.");
					Assert.That(text.text, Does.Contain(kiste.MarkPrice.ToString()).And.Contain("MARKEN"),
						kiste.ContainerId + $": Labeltext \"{text.text}\" nennt nicht den Preis {kiste.MarkPrice}.");
					Assert.That(text.gameObject.activeInHierarchy, Is.True,
						kiste.ContainerId + ": Preislabel ist unsichtbar, obwohl der Kauf ansteht.");
					preislabels++;
				}
				else
				{
					Assert.That(label == null || !label.gameObject.activeInHierarchy, Is.True,
						kiste.ContainerId + ": Gewölbekiste zeigt fälschlich ein Preislabel.");
				}
			}
			Assert.That(preislabels, Is.EqualTo(3), "Es müssen genau drei Schmiedekisten mit Preislabel existieren.");

			// (2) Ausgang am Windfang: vorhanden, südlich, und sein Abschluss
			// bringt den Spieler in die Glutruinen ans Portal.
			EidraForgeExit ausgang = Object.FindFirstObjectByType<EidraForgeExit>(FindObjectsInactive.Include);
			Assert.That(ausgang, Is.Not.Null, "Das Verlies hat keinen Ausgang.");
			Assert.That(ausgang.transform.position.z, Is.LessThan(-30f), "Der Ausgang gehört an den Windfang (Süden).");
			var kontext = new InteractionContext(spieler.gameObject, spieler.transform, Vector3.forward, session.PlayerInventory);
			Assert.That(ausgang.CanInteract(in kontext, out string grund), Is.True, "Der Ausgang ist blockiert: " + grund);
			ausgang.CompleteInteraction(in kontext);
			zeit = 0f;
			PlayerPrefabBindings rueckkehrer = null;
			while (zeit < 25f)
			{
				zeit += Time.deltaTime;
				if (SceneManager.GetActiveScene().name == "Zone_EmberRuins")
				{
					rueckkehrer = Object.FindFirstObjectByType<PlayerPrefabBindings>();
					if (rueckkehrer != null)
					{
						break;
					}
				}
				yield return null;
			}
			Assert.That(rueckkehrer, Is.Not.Null, "Die Glutruinen haben nach dem Verlassen keinen Spieler erzeugt.");
			// Der Rückkehr-Teleport läuft im Entrance-Start — ein paar Frames geben.
			zeit = 0f;
			while (zeit < 2f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			EidraForgeEntrance portal = Object.FindFirstObjectByType<EidraForgeEntrance>(FindObjectsInactive.Include);
			Assert.That(portal, Is.Not.Null, "Die Glutruinen haben keinen Verlies-Eingang.");
			rueckkehrer = Object.FindFirstObjectByType<PlayerPrefabBindings>();
			Assert.That(rueckkehrer, Is.Not.Null, "Der Spieler verschwand nach der Rückkehr.");
			float abstand = Vector3.Distance(rueckkehrer.transform.position, portal.transform.position);
			Assert.That(abstand, Is.LessThan(6f),
				$"Nach dem Verlassen muss der Spieler am Portal stehen — tatsächlich {abstand:0.0} entfernt.");

			// (3) Bildbeleg des neuen Felsportals für die Nutzer-Abnahme.
			CaptureAround(portal.transform.position, "TempReview/verlies-eingang-neu.png");
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
