using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Interaction;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// F34-Sichtbelege mit der echten Spielkamera (orthografisch, Qualitaet
	/// Medium): Weltkiste, Faserpflanze, Busch, Wanderer mit Speer beim Laufen
	/// und Angriff, Abbau von Hand. Bilder landen unter TempReview/F34-Sicht/.
	/// Braucht ein Grafikgeraet (Lauf ohne -nographics).
	/// </summary>
	public sealed class Fixrunde034SichtCaptureTests
	{
		private const string OutputDir = "TempReview/F34-Sicht";

		[UnityTest]
		public IEnumerator Weltkiste_Greenwood_IstSichtbar()
		{
			yield return Vorbereiten("Zone_Greenwood");
			PlayerPrefabBindings player = Spieler();
			WorldChestContainer kiste = Object.FindFirstObjectByType<WorldChestContainer>();
			Assert.That(kiste, Is.Not.Null, "Keine Weltkiste in Greenwood.");
			Stellen(player, kiste.transform.position + new Vector3(1.6f, 0f, -1.2f));
			yield return Warten(1.2f);
			Speichern("f34-003-weltkiste.png");
			Assert.That(SichtbareRenderer(kiste.gameObject), Is.GreaterThan(0),
				$"Weltkiste {kiste.name} hat keinen sichtbaren Renderer (LOD-Bias {QualitySettings.lodBias:0.00}).");
		}

		[UnityTest]
		public IEnumerator Faserpflanze_Marsh_IstSichtbar()
		{
			yield return Vorbereiten("Zone_Marsh");
			PlayerPrefabBindings player = Spieler();
			ResourceNode faser = Finde(node => node.Definition != null && node.Definition.Id.Contains("fiber"));
			Assert.That(faser, Is.Not.Null, "Keine Faserpflanze im Sumpf.");
			Stellen(player, faser.transform.position + new Vector3(1.4f, 0f, -1.0f));
			yield return Warten(1.2f);
			Speichern("f34-001-faserpflanze.png");
			Assert.That(SichtbareRenderer(faser.gameObject), Is.GreaterThan(0),
				$"Faserpflanze {faser.name} hat keinen sichtbaren Renderer (LOD-Bias {QualitySettings.lodBias:0.00}).");
		}

		[UnityTest]
		public IEnumerator Busch_HomeBase_ZeigtFeinsteStufe()
		{
			yield return Vorbereiten("HomeBase");
			PlayerPrefabBindings player = Spieler();
			LODGroup busch = null;
			foreach (LODGroup group in Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
			{
				if (group.name.Contains("SP_Plant_Bush"))
				{
					busch = group;
					break;
				}
			}
			Assert.That(busch, Is.Not.Null, "Kein Busch in der Heimatbasis.");
			Stellen(player, busch.transform.position + new Vector3(1.6f, 0f, -1.2f));
			yield return Warten(1.2f);
			Speichern("f34-002-busch.png");
			LOD[] lods = busch.GetLODs();
			int lod0 = 0;
			foreach (Renderer renderer in lods[0].renderers)
			{
				if (renderer != null && renderer.isVisible) lod0++;
			}
			Assert.That(lod0, Is.GreaterThan(0), $"Busch {busch.name}: LOD0 wird nicht gerendert (LOD-Bias {QualitySettings.lodBias:0.00}).");
		}

		[UnityTest]
		public IEnumerator Wanderer_Speer_LaufenUndAngriff()
		{
			yield return Vorbereiten("Zone_Greenwood");
			PlayerPrefabBindings player = Spieler();
			EidrenServiceRoot dienste = EidrenServiceRoot.Instance;
			string speerId = null;
			foreach (string kandidat in new[] { "copper_spear", "iron_spear", "spear" })
			{
				if (dienste.ContentDatabase.GetItem(kandidat) != null) { speerId = kandidat; break; }
			}
			Assert.That(speerId, Is.Not.Null, "Kein Speer im Inhalt.");
			Assert.That(dienste.GameSession.PlayerEquipment.TryEquip(EquipmentSlot.Weapon1,
				ItemStack.Create(dienste.ContentDatabase.GetItem(speerId), 1), out string fehler), Is.True, fehler);
			PlayerInputReader eingabe = Object.FindFirstObjectByType<PlayerInputReader>();
			yield return Warten(0.6f);
			Speichern("f34-006-speer-ruhe.png");
			eingabe.SetVirtualMove(Vector2.right);
			for (int i = 0; i < 6; i++)
			{
				yield return Warten(0.12f);
				Speichern($"f34-006-speer-laufen-{i}.png");
			}
			eingabe.SetVirtualMove(Vector2.zero);
			yield return Warten(0.4f);
			eingabe.PressAttack();
			for (int i = 0; i < 5; i++)
			{
				yield return Warten(0.1f);
				Speichern($"f34-007-speer-angriff-{i}.png");
			}
		}

		[UnityTest]
		public IEnumerator Abbau_HolzOhneAxt_ZeigtAbbauzustand()
		{
			yield return Vorbereiten("Zone_Greenwood");
			PlayerPrefabBindings player = Spieler();
			ResourceNode baum = Finde(node => node.Definition != null && node.Definition.Id.Contains("tree"));
			Assert.That(baum, Is.Not.Null, "Kein Baum in Greenwood.");
			Stellen(player, baum.transform.position + new Vector3(1.2f, 0f, -0.9f));
			PlayerInputReader eingabe = Object.FindFirstObjectByType<PlayerInputReader>();
			yield return Warten(0.5f);
			eingabe.SetVirtualInteract(true);
			yield return Warten(0.8f);
			Speichern("f34-008-abbau-holz.png");
			var harvest = player.VisualAnimator != null ? player.VisualAnimator.HarvestVisual : null;
			Assert.That(harvest, Is.Not.Null, "Kein HarvestVisual am Spieler.");
			Debug.Log($"[F34-Sicht] Abbau: IsHarvesting={harvest.IsHarvesting}, Werkzeug='{harvest.LastResolvedToolItemId}'");
			Assert.That(harvest.IsHarvesting, Is.True, "Abbau laeuft nicht (Ziel zu weit oder Eingabe kam nicht an).");
			eingabe.SetVirtualInteract(false);
			yield return null;
		}

		// ------------------------------------------------------------------

		private static IEnumerator Vorbereiten(string szene)
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
			yield return SceneManager.LoadSceneAsync(szene, LoadSceneMode.Single);
			float zeit = 0f;
			while (Object.FindFirstObjectByType<PlayerPrefabBindings>() == null && zeit < 20f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			QualitySettings.SetQualityLevel(2, applyExpensiveChanges: true);
			Debug.Log($"[F34-Sicht] Szene {szene}, Qualitaet {QualitySettings.names[QualitySettings.GetQualityLevel()]}, LOD-Bias {QualitySettings.lodBias:0.00}, Kamera ortho {(Camera.main != null ? Camera.main.orthographicSize : -1f):0.0}");
			Directory.CreateDirectory(OutputDir);
			yield return Warten(0.5f);
		}

		private static PlayerPrefabBindings Spieler()
		{
			PlayerPrefabBindings player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
			Assert.That(player, Is.Not.Null, "Szene hat keinen Spieler erzeugt.");
			return player;
		}

		private static ResourceNode Finde(System.Func<ResourceNode, bool> filter)
		{
			foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
			{
				if (filter(node)) return node;
			}
			return null;
		}

		private static void Stellen(PlayerPrefabBindings player, Vector3 ziel)
		{
			CharacterController controller = player.GetComponent<CharacterController>();
			if (controller != null) controller.enabled = false;
			player.transform.position = ziel;
			if (controller != null) controller.enabled = true;
			Physics.SyncTransforms();
		}

		private static IEnumerator Warten(float sekunden)
		{
			float zeit = 0f;
			while (zeit < sekunden)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
		}

		private static int SichtbareRenderer(GameObject root)
		{
			int sichtbar = 0;
			foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
			{
				if (renderer.isVisible) sichtbar++;
			}
			return sichtbar;
		}

		private static void Speichern(string datei)
		{
			Camera kamera = Camera.main;
			Assert.That(kamera, Is.Not.Null, "Keine Hauptkamera.");
			var ziel = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
			RenderTexture vorherAktiv = RenderTexture.active;
			RenderTexture vorherZiel = kamera.targetTexture;
			try
			{
				kamera.targetTexture = ziel;
				kamera.Render();
				RenderTexture.active = ziel;
				var bild = new Texture2D(1600, 900, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
				bild.Apply();
				File.WriteAllBytes(Path.Combine(OutputDir, datei), bild.EncodeToPNG());
				Object.Destroy(bild);
			}
			finally
			{
				kamera.targetTexture = vorherZiel;
				RenderTexture.active = vorherAktiv;
				ziel.Release();
				Object.Destroy(ziel);
			}
		}
	}
}
