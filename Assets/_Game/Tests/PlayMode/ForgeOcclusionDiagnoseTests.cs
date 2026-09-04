using Eidren.Composition;
using Eidren.Core.Services;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// Diagnose (18.08.2026): Sichtprobe im ECHT betretenen Verlies — über
	/// den Spielweg (Garon-Flag, Erstlauf, SceneFlow). Direktes Szenenladen
	/// erzeugt eine Phantomwelt und taugt nicht als Prüfstand. Die Route
	/// hier ist zugleich die Fixture für alle künftigen Verlies-Tests.
	/// </summary>
	public sealed class ForgeOcclusionDiagnoseTests
	{
		private static readonly (string Name, Vector3 Punkt)[] Probepunkte = new (string, Vector3)[]
		{
			("suedwand", new Vector3(-22f, 0f, -6f)),
			("rinne", new Vector3(0f, 0f, 2f)),
			("galerie", new Vector3(29f, 0f, 32f)),
			("nordkammer", new Vector3(-15f, 0f, 38f))
		};

		[UnityTest]
		public IEnumerator Verlies_SichtprobeMitBildbelegen()
		{
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
			{
				Assert.Ignore("Sichtprobe braucht ein Grafikgerät (Lauf ohne -nographics).");
			}
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			// Echte Route: erst eine Zone (voller Dienstaufbau), dann der
			// Betretensfluss des Verlieseingangs.
			yield return SceneManager.LoadSceneAsync("Zone_Greenwood", LoadSceneMode.Single);
			float zeit = 0f;
			while (Object.FindFirstObjectByType<PlayerPrefabBindings>() == null && zeit < 20f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			EidrenServiceRoot dienste = EidrenServiceRoot.Instance;
			Assert.That(dienste, Is.Not.Null);
			GameSession session = dienste.GameSession;
			session.TrySetProgressFlag("garon_defeated");
			Assert.That(session.EidraForge.TryBeginFirstRun(7), Is.True, "Der Erstlauf ließ sich nicht beginnen.");
			Assert.That(dienste.SceneFlowService.TryLoadScene("EidraForge"), Is.True, "Der Szenenwechsel wurde abgelehnt.");
			zeit = 0f;
			PlayerPrefabBindings player = null;
			while (zeit < 25f)
			{
				zeit += Time.deltaTime;
				if (SceneManager.GetActiveScene().name == "EidraForge")
				{
					player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
					if (player != null)
					{
						break;
					}
				}
				yield return null;
			}
			Assert.That(player, Is.Not.Null, "Die Schmiede hat auf der echten Route keinen Spieler erzeugt.");
			Directory.CreateDirectory("TempReview/verlies-sicht");
			foreach ((string name, Vector3 punkt) in Probepunkte)
			{
				player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
				Assert.That(player, Is.Not.Null, "Spieler verschwand vor Punkt '" + name + "'.");
				CharacterController controller = player.GetComponent<CharacterController>();
				if (controller != null)
				{
					controller.enabled = false;
				}
				player.transform.position = punkt + Vector3.up * 0.1f;
				if (controller != null)
				{
					controller.enabled = true;
				}
				Physics.SyncTransforms();
				zeit = 0f;
				while (zeit < 1.5f)
				{
					zeit += Time.deltaTime;
					yield return null;
				}
				Capture("TempReview/verlies-sicht/" + name + ".png");
			}
		}

		private static void Capture(string pfad)
		{
			Camera kamera = Camera.main;
			if (kamera == null)
			{
				return;
			}
			var zielTextur = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
			RenderTexture vorherAktiv = RenderTexture.active;
			RenderTexture vorherZiel = kamera.targetTexture;
			try
			{
				kamera.targetTexture = zielTextur;
				kamera.Render();
				RenderTexture.active = zielTextur;
				var bild = new Texture2D(1600, 900, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
				bild.Apply();
				File.WriteAllBytes(pfad, bild.EncodeToPNG());
				Object.Destroy(bild);
			}
			finally
			{
				kamera.targetTexture = vorherZiel;
				RenderTexture.active = vorherAktiv;
				zielTextur.Release();
				Object.Destroy(zielTextur);
			}
		}
	}
}
