using Eidren.AI;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Interaction;
using NUnit.Framework;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// F31-009: Die Glutruinen (Gefahr 5) stellen zur Laufzeit 14 reguläre
	/// Gegner auf; die pro Lauf gewürfelten Weltkisten werden im
	/// Abstandsring bewacht, und alle Gegner stehen auf dem NavMesh.
	/// </summary>
	public sealed class Fixrunde031EnemyPopulationPlayTests
	{
		[UnityTest]
		public IEnumerator Glutruinen_StellenVierzehnGegnerMitKistenwachenAuf()
		{
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			yield return SceneManager.LoadSceneAsync("Zone_EmberRuins", LoadSceneMode.Single);
			float zeit = 0f;
			ZoneController zone = null;
			while (zone == null && zeit < 20f)
			{
				zeit += Time.deltaTime;
				zone = Object.FindFirstObjectByType<ZoneController>();
				yield return null;
			}
			Assert.That(zone, Is.Not.Null, "Glutruinen haben keinen ZoneController erzeugt.");
			ZoneEnemyPopulator populator = zone.EnemyPopulator;
			Assert.That(populator, Is.Not.Null, "Der ZoneEnemyPopulator wurde nicht aufgebaut.");
			Assert.That(populator.Spawned.Count, Is.EqualTo(14),
				"Gefahr 5 verlangt 14 Gegner (3 x 5 - 1); die Zone hat nicht alle aufgestellt.");
			int kisten = zone.WorldChests.Count;
			Assert.That(kisten, Is.GreaterThan(0), "Die Zone hat keine Weltkisten gewürfelt.");
			Assert.That(populator.BossKeepOut.HasValue, Is.True,
				"Die Glutruinen sind Bosszone — die Sperrzone um den Bossbereich muss existieren.");
			Bounds sperre = populator.BossKeepOut.Value;
			// Kisten im Bossbereich bekommen keine Feldwache — dort ist der
			// Boss die Wache; das Budget bilden nur die übrigen Kisten.
			int bewachbare = 0;
			foreach (WorldChestContainer kiste in zone.WorldChests)
			{
				Vector3 position = kiste.transform.position;
				if (!sperre.Contains(new Vector3(position.x, sperre.center.y, position.z)))
				{
					bewachbare++;
				}
			}
			int budget = ZoneEnemyPopulationRules.ChestGuardBudget(bewachbare, populator.Spawned.Count);
			Assert.That(populator.ChestGuards.Count, Is.EqualTo(budget),
				$"{bewachbare} bewachbare von {kisten} Kisten und 14 Gegner ergeben {budget} Wachen.");
			foreach ((Vector3 kiste, WildlingController wache) in populator.ChestGuards)
			{
				Assert.That(wache, Is.Not.Null, "Eine Kistenwache fehlt.");
				float abstand = new Vector2(wache.transform.position.x - kiste.x, wache.transform.position.z - kiste.z).magnitude;
				Assert.That(abstand,
					Is.InRange(ZoneEnemyPopulationRules.ChestGuardMinDistance - 0.6f, ZoneEnemyPopulationRules.ChestGuardMaxDistance + 0.6f),
					$"Wache '{wache.name}' steht {abstand:0.00} von ihrer Kiste — außerhalb des Wachenrings (Toleranz: NavMesh-Projektion).");
			}
			// Lastprobe: einige Spielsekunden laufen lassen — alle Agenten
			// stehen auf dem NavMesh, kein Gegner wirft Fehler.
			zeit = 0f;
			while (zeit < 3f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			// Die Spawn-Sperre umfasst den Patrouillenweg (+3,5 je Seite);
			// die harte Garantie „stoert den Bosskampf nicht" gilt fuer die
			// KERNZONE — Randgegner patrouillieren planmaessig in den Saum.
			Bounds bossSperre = sperre;
			bossSperre.Expand(new Vector3(-7f, 0f, -7f));
			foreach (WildlingController gegner in populator.Spawned)
			{
				Assert.That(gegner, Is.Not.Null, "Ein Gegner ist während der Lastprobe verschwunden.");
				NavMeshAgent agent = gegner.GetComponent<NavMeshAgent>();
				Assert.That(agent != null && agent.isOnNavMesh, Is.True,
					$"Gegner '{gegner.name}' steht nicht auf dem NavMesh.");
				Vector3 position = gegner.transform.position;
				Assert.That(bossSperre.Contains(new Vector3(position.x, bossSperre.center.y, position.z)), Is.False,
					$"Gegner '{gegner.name}' steht im Bossbereich — Feldgegner dürfen den Bosskampf nicht stören.");
			}
			WriteOverviewCapture();
		}

		/// <summary>
		/// Bildbeleg im Grafiklauf: Draufsicht auf die Zone mit der vollen
		/// Besetzung. Im Lauf ohne Grafikgerät entfällt nur das Bild — die
		/// harten Nachweise oben laufen immer.
		/// </summary>
		private static void WriteOverviewCapture()
		{
			if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
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
			float vorherGroesse = kamera.orthographicSize;
			var zielTextur = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
			RenderTexture vorherAktiv = RenderTexture.active;
			RenderTexture vorherZiel = kamera.targetTexture;
			try
			{
				kamera.transform.position = new Vector3(0f, 60f, 0f);
				kamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
				kamera.orthographicSize = 42f;
				kamera.targetTexture = zielTextur;
				kamera.Render();
				RenderTexture.active = zielTextur;
				var bild = new Texture2D(1600, 900, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
				bild.Apply();
				System.IO.Directory.CreateDirectory("TempReview");
				System.IO.File.WriteAllBytes("TempReview/f31-gegnerbesetzung-glutruinen.png", bild.EncodeToPNG());
				Object.Destroy(bild);
			}
			finally
			{
				kamera.targetTexture = vorherZiel;
				RenderTexture.active = vorherAktiv;
				kamera.transform.SetPositionAndRotation(vorherPosition, vorherRotation);
				kamera.orthographicSize = vorherGroesse;
				zielTextur.Release();
				Object.Destroy(zielTextur);
			}
		}
	}
}
