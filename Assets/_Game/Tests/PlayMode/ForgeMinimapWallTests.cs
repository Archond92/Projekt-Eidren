using Eidren.Composition;
using Eidren.Core.Services;
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
	/// #29: Die Minimap zeigt im Verlies den Aufbau — helle Wandkonturen
	/// im Sichtradius um die Figur. Der Test fährt die echte Verlies-Route
	/// und weist nach, dass der Kartentakt Wandsegmente in der Wandfarbe
	/// zeichnet; im Grafiklauf entsteht zusätzlich ein Bildbeleg.
	/// </summary>
	public sealed class ForgeMinimapWallTests
	{
		[UnityTest]
		public IEnumerator Verlies_MinimapZeichnetWandkonturen()
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
			Assert.That(dienste, Is.Not.Null);
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
			Assert.That(spieler, Is.Not.Null, "Die Schmiede hat auf der echten Route keinen Spieler erzeugt.");
			// Der Betretensfluss des Verlieses bindet die Wandkontur —
			// ein paar Spielsekunden für Controller-Start und Kartentakt.
			zeit = 0f;
			while (zeit < 2f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			Assert.That(spieler.CombatHud, Is.Not.Null, "Der Spieler hat kein Kampf-HUD.");
			MinimapPresenter karte = spieler.CombatHud.Minimap;
			Assert.That(karte, Is.Not.Null, "Das HUD hat keine Minimap.");
			karte.Refresh();
			int wandlinien = 0;
			foreach (Image image in karte.GetComponentsInChildren<Image>(includeInactive: false))
			{
				if (image.color == MinimapPresenter.WallColor)
				{
					wandlinien++;
				}
			}
			Assert.That(wandlinien, Is.GreaterThan(0),
				"Die Karte zeichnet im Verlies keine einzige Wandkontur — der Aufbau bleibt unsichtbar.");
			WriteOutlineCapture(spieler.transform.position);
		}

		/// <summary>
		/// Bildbeleg: Die Wandkontur um die Figur (Radius 22) als gerastertes
		/// Draufsichtbild — synchron geschrieben, weil ScreenCapture im
		/// Batchmode erst am Frame-Ende liefert und die Datei sonst fehlt.
		/// </summary>
		private static void WriteOutlineCapture(Vector3 zentrum)
		{
			const int groesse = 420;
			const float radius = 22f;
			UnityEngine.AI.NavMeshTriangulation triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
			System.Collections.Generic.List<MinimapWallSegment> segmente =
				MinimapWallOutline.ExtractBoundaryEdges(triangulation.vertices, triangulation.indices);
			var bild = new Texture2D(groesse, groesse, TextureFormat.RGB24, mipChain: false);
			var pixel = new Color32[groesse * groesse];
			var hintergrund = new Color32(24, 22, 28, 255);
			for (int i = 0; i < pixel.Length; i++)
			{
				pixel[i] = hintergrund;
			}
			var wand = new Color32(237, 230, 209, 255);
			foreach (MinimapWallSegment segment in segmente)
			{
				float laenge = Vector3.Distance(segment.From, segment.To);
				int schritte = Mathf.Max(2, Mathf.CeilToInt(laenge / 0.05f));
				for (int i = 0; i <= schritte; i++)
				{
					Vector3 punkt = Vector3.Lerp(segment.From, segment.To, (float)i / schritte);
					float dx = punkt.x - zentrum.x;
					float dz = punkt.z - zentrum.z;
					if (dx * dx + dz * dz > radius * radius)
					{
						continue;
					}
					int px = Mathf.RoundToInt((dx / radius * 0.5f + 0.5f) * (groesse - 1));
					int py = Mathf.RoundToInt((dz / radius * 0.5f + 0.5f) * (groesse - 1));
					pixel[py * groesse + px] = wand;
				}
			}
			// Die Figur als Punkt in der Mitte.
			for (int dy = -2; dy <= 2; dy++)
			{
				for (int dx = -2; dx <= 2; dx++)
				{
					pixel[(groesse / 2 + dy) * groesse + groesse / 2 + dx] = new Color32(120, 200, 255, 255);
				}
			}
			bild.SetPixels32(pixel);
			bild.Apply();
			Directory.CreateDirectory("TempReview");
			File.WriteAllBytes("TempReview/verlies-minimap-waende.png", bild.EncodeToPNG());
			Object.Destroy(bild);
		}
	}
}
