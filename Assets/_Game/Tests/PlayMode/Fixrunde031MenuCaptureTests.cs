using Eidren.Composition;
using Eidren.Data;
using Eidren.Core.Services;
using Eidren.Input;
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
	/// Bildbasierte Abnahme des Baumenüs (F31-012/013): echte HomeBase, echtes
	/// Menü, echter Screenshot — die Prüfung, die bei W-007 fehlte. Der Lauf
	/// braucht ein Grafikgerät (ohne -nographics) und überspringt sich sonst,
	/// wie die übrigen Capture-Tests.
	/// </summary>
	public sealed class Fixrunde031MenuCaptureTests
	{
		private const string OutputPath = "TempReview/f31-baumenue-abnahme.png";

		[UnityTest]
		public IEnumerator Baumenue_AbnahmeCapture()
		{
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
			{
				Assert.Ignore("Capture braucht ein echtes Grafikgeraet (Lauf ohne -nographics).");
			}
			if (EidrenServiceRoot.Instance != null)
			{
				UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			yield return SceneManager.LoadSceneAsync("HomeBase", LoadSceneMode.Single);
			float deadline = Time.realtimeSinceStartup + 15f;
			PlayerPrefabBindings player = null;
			while (player == null && Time.realtimeSinceStartup < deadline)
			{
				player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
				yield return null;
			}
			Assert.That(player, Is.Not.Null, "HomeBase hat keinen Spieler erzeugt.");
			EidrenServiceRoot services = EidrenServiceRoot.Instance;
			services.GameSession.StartNewGame();
			services.PlayerProgression.ResetForNewGame();
			services.TechnologyUnlocks.ResetForNewGame();
			services.PlayerProgression.RecordEnemyDefeated(100000);
			foreach (TechnologyNodeDefinition node in services.TechnologyTree.Nodes)
			{
				if (node.SortOrder <= 17 && services.TechnologyUnlocks.GetNodeState(node.Id) != TechnologyNodeState.Unlocked)
				{
					services.TechnologyUnlocks.TryUnlock(node.Id);
				}
			}
			services.GameSession.PlayerInventory.Add("wood", 20);
			services.GameSession.PlayerInventory.Add("stone", 12);
			services.GameSession.PlayerInventory.Add("plant_fiber", 10);
			UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>().PressBuildingToggle();
			yield return null;
			Assert.That(player.BuildingMenu.IsOpen, Is.True, "Baumenü hat sich nicht geöffnet.");
			for (int i = 0; i < 8; i++)
			{
				yield return null;
			}
			// ScreenCapture braucht ein Fenster; im Batchmode stattdessen die
			// Spielkamera in eine RenderTexture (Muster der In-Welt-Captures).
			// Dafuer haengt die Menue-Canvas kurz an der Kamera.
			Canvas canvas = player.BuildingMenu.GetComponentInParent<Canvas>();
			Assert.That(canvas, Is.Not.Null, "Baumenü hat keine Canvas.");
			Camera kamera = Camera.main;
			Assert.That(kamera, Is.Not.Null, "Keine Hauptkamera.");
			RenderMode vorherModus = canvas.renderMode;
			canvas.renderMode = RenderMode.ScreenSpaceCamera;
			canvas.worldCamera = kamera;
			canvas.planeDistance = Mathf.Max(1f, kamera.nearClipPlane + 0.5f);
			yield return null;
			yield return null;
			Directory.CreateDirectory("TempReview");
			var ziel = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
			RenderTexture vorherAktiv = RenderTexture.active;
			RenderTexture vorherZiel = kamera.targetTexture;
			try
			{
				kamera.targetTexture = ziel;
				kamera.Render();
				RenderTexture.active = ziel;
				var bild = new Texture2D(1920, 1080, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
				bild.Apply();
				File.WriteAllBytes(OutputPath, bild.EncodeToPNG());
				UnityEngine.Object.Destroy(bild);
			}
			finally
			{
				kamera.targetTexture = vorherZiel;
				RenderTexture.active = vorherAktiv;
				ziel.Release();
				UnityEngine.Object.Destroy(ziel);
				canvas.renderMode = vorherModus;
			}
			Assert.That(File.Exists(OutputPath), Is.True, "Screenshot wurde nicht geschrieben.");
		}
	}
}
