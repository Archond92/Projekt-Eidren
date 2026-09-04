using Eidren.Composition;
using Eidren.Interaction;
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
	/// F31-001: Bildbeleg — steht die Figur hinter einem Baum, blendet die
	/// Sichtlinien-Ausblendung den Baum weich aus. Braucht ein Grafikgerät.
	/// </summary>
	public sealed class Fixrunde031OcclusionCaptureTests
	{
		private const string OutputPath = "TempReview/f31-sichtverdeckung-baum.png";

		[UnityTest]
		public IEnumerator BaumVorDerFigur_WirdAusgeblendet()
		{
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
			{
				Assert.Ignore("Capture braucht ein echtes Grafikgeraet (Lauf ohne -nographics).");
			}
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			yield return SceneManager.LoadSceneAsync("Zone_Greenwood", LoadSceneMode.Single);
			float zeit = 0f;
			PlayerPrefabBindings player = null;
			while (player == null && zeit < 20f)
			{
				zeit += Time.deltaTime;
				player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
				yield return null;
			}
			Assert.That(player, Is.Not.Null, "Grünwald hat keinen Spieler erzeugt.");
			// Einen Baum finden und die Figur aus Kamerasicht dahinterstellen.
			ResourceNode baum = null;
			foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
			{
				if (node.name.Contains("Tree") || (node.Definition != null && node.Definition.Id.Contains("tree")))
				{
					baum = node;
					break;
				}
			}
			Assert.That(baum, Is.Not.Null, "Kein Baum in der Zone gefunden.");
			Camera kamera = Camera.main;
			Assert.That(kamera, Is.Not.Null, "Grünwald hat keine aktive Hauptkamera.");
			Vector3 blickrichtung = kamera.transform.forward;
			blickrichtung.y = 0f;
			blickrichtung.Normalize();
			Vector3 ziel = baum.transform.position + blickrichtung * 1.7f;
			CharacterController controller = player.GetComponent<CharacterController>();
			if (controller != null)
			{
				controller.enabled = false;
			}
			player.transform.position = ziel;
			if (controller != null)
			{
				controller.enabled = true;
			}
			Physics.SyncTransforms();
			// Fade abwarten: Auffrischung 0,08 s (unskaliert) + 5,5 Alpha/s.
			zeit = 0f;
			while (zeit < 2.5f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			// Kettennachweis:
			Assert.That(Object.FindFirstObjectByType<Eidren.Presentation.ActorOcclusionTransparency>(), Is.Not.Null,
				"Occlusion-Dienst ist nicht installiert.");
			int registrierte = 0;
			foreach (var eintrag in Eidren.Presentation.ActorPresentationRegistry.Active) { registrierte++; }
			Assert.That(registrierte, Is.GreaterThan(0), "Kein Akteur in der ActorPresentationRegistry.");
			bool fadeMaterialAktiv = false;
			float kleinstesAlpha = 1f;
			foreach (Renderer renderer in baum.GetComponentsInChildren<Renderer>())
			{
				foreach (Material material in renderer.sharedMaterials)
				{
					if (material != null && material.name.EndsWith(" (Occlusion Fade)"))
					{
						fadeMaterialAktiv = true;
						if (material.HasProperty("_BaseColor"))
						{
							kleinstesAlpha = Mathf.Min(kleinstesAlpha, material.GetColor("_BaseColor").a);
						}
						if (material.HasProperty("_Color"))
						{
							kleinstesAlpha = Mathf.Min(kleinstesAlpha, material.GetColor("_Color").a);
						}
					}
				}
			}
			Assert.That(fadeMaterialAktiv, Is.True,
				"Der Baum trägt kein Fade-Material — der Verdecker-Strahl hat ihn nicht erfasst.");
			Assert.That(kleinstesAlpha, Is.LessThan(0.5f),
				$"Fade-Material da, aber Alpha {kleinstesAlpha:0.00} — Ausblendung läuft nicht.");
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
				Directory.CreateDirectory("TempReview");
				File.WriteAllBytes(OutputPath, bild.EncodeToPNG());
				Object.Destroy(bild);
			}
			finally
			{
				kamera.targetTexture = vorherZiel;
				RenderTexture.active = vorherAktiv;
				zielTextur.Release();
				Object.Destroy(zielTextur);
			}
			Assert.That(File.Exists(OutputPath), Is.True, "Bildbeleg wurde nicht geschrieben.");
		}
	}
}
