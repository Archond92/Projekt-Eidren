using System;
using System.Collections;
using System.IO;
using Eidren.AI;
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
	/// <summary>HUD-100: reproduzierbare Windows-Renderbelege, keine mobile Geraeteabnahme.</summary>
	public sealed class CombatHudBaselineCaptureTests
	{
		[UnityTest]
		public IEnumerator Exploration_Combat_Garon_WriteCurrentHudEvidence()
		{
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
				Assert.Ignore("HUD-Bildbelege brauchen ein Grafikgeraet; ohne -nographics ausfuehren.");
			string directory = Path.GetFullPath(Path.Combine("TestResults-Archiv", "HUD100", "captures-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
			Directory.CreateDirectory(directory);
			yield return Testumgebung.LeereWeltBereitstellen();
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			EidrenServiceRoot services = EidrenServiceRoot.FindOrCreate();
			// Testspielstaende bleiben im neuen Belegordner, nie im Benutzerspielstand.
			services.SaveGameService.Initialize(services.GameSession, services.ContentDatabase, services.SceneFlowService,
				rootPath: Path.Combine(directory, "test-save"), progression: services.PlayerProgression,
				technology: services.TechnologyUnlocks, quest: services.QuestProgress);
			services.GameSession.StartNewGame();

			yield return Load("HomeBase");
			PlayerPrefabBindings player = Player();
			Capture(directory, "exploration", player, "HomeBase; kein Kampf", 1920, 1080);
			Capture(directory, "exploration-19_5x9", player, "HomeBase; keine Notch-Simulation", 2340, 1080);
			Capture(directory, "exploration-20x9", player, "HomeBase; keine Notch-Simulation", 2400, 1080);
			Capture(directory, "exploration-16x10", player, "HomeBase; kein Kampf", 1920, 1200);

			yield return Load("Zone_Greenwood");
			player = Player();
			Assert.That(new SceneCombatTargetQuery().TryFindClosest(player.transform, 1000f, out CombatTarget target), Is.True);
			EnemyControllerBase enemy = target.Transform.GetComponent<EnemyControllerBase>();
			Assert.That(enemy, Is.Not.Null);
			player.Motor.Teleport(enemy.transform.position + Vector3.back * 2.5f + Vector3.up * .05f);
			Physics.SyncTransforms();
			yield return SettleCamera(player.transform, enemy.transform);
			// Ein kleiner echter Treffer loest Alarm aus; keine kuenstliche HUD-Anzeige.
			enemy.ApplyDamage(new DamageInfo(1f, 0f, enemy.transform.position, player.gameObject, false, "hud_baseline_hit", "player"));
			for (int i = 0; i < 12; i++) yield return null;
			Assert.That(enemy.IsAlive && player.Damageable.IsAlive, Is.True);
			Assert.That(enemy.EnemyState, Is.Not.EqualTo(EnemyState.Idle).And.Not.EqualTo(EnemyState.Patrol).And.Not.EqualTo(EnemyState.Return));
			Capture(directory, "combat", player, enemy.name + ":" + enemy.EnemyState, 1920, 1080);

			services.PlayerProgression.RecordEnemyDefeated(5100);
			yield return Load("Zone_EmberRuins");
			player = Player();
			BossAreaController area = Object.FindFirstObjectByType<BossAreaController>();
			Assert.That(area, Is.Not.Null);
			BossController boss = area.ActiveBoss;
			Assert.That(boss, Is.Not.Null);
			player.Motor.Teleport(boss.transform.position + Vector3.back * 5f + Vector3.up * .05f);
			yield return SettleCamera(player.transform, boss.transform);
			boss.BeginBattle();
			for (int i = 0; i < 12; i++) yield return null;
			Assert.That(boss.BattleActive && boss.IsAlive, Is.True);
			Capture(directory, "garon", player, boss.name + ":" + boss.EnemyState, 1920, 1080);
			Debug.Log("HUD100 Captures: " + directory);
		}

		[UnityTearDown]
		public IEnumerator Cleanup()
		{
			yield return Testumgebung.LeereWeltBereitstellen();
			if (EidrenServiceRoot.Instance != null) Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}

		private static IEnumerator Load(string name)
		{
			yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
			for (int i = 0; i < 24; i++) yield return null;
			foreach (InputDisplayFamilyMonitor monitor in Object.FindObjectsByType<InputDisplayFamilyMonitor>(FindObjectsSortMode.None))
				monitor.enabled = false;
			PlayerInputReader input = Object.FindFirstObjectByType<PlayerInputReader>();
			Assert.That(input, Is.Not.Null);
			input.SetDisplayFamily(InputDisplayFamily.Touch);
			yield return null;
		}

		private static PlayerPrefabBindings Player()
		{
			PlayerPrefabBindings player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
			Assert.That(player, Is.Not.Null);
			Assert.That(player.CombatHud, Is.Not.Null);
			return player;
		}

		private static IEnumerator SettleCamera(Transform player, Transform target)
		{
			Camera camera = Camera.main;
			Assert.That(camera, Is.Not.Null);
			float start = Time.realtimeSinceStartup;
			Vector3 previous = camera.transform.position;
			while (Time.realtimeSinceStartup - start < 5f)
			{
				yield return null;
				bool settled = Vector3.Distance(previous, camera.transform.position) < .005f;
				previous = camera.transform.position;
				if (Time.realtimeSinceStartup - start > .5f && settled && InFrame(camera, player) && InFrame(camera, target)) yield break;
			}
			Assert.Fail("Capture-Kamera hat Spieler und Ziel nach Teleport nicht stabil im Bild.");
		}

		private static bool InFrame(Camera camera, Transform actor)
		{
			Vector3 foot = camera.WorldToViewportPoint(actor.position);
			Vector3 head = camera.WorldToViewportPoint(actor.position + Vector3.up * 4f);
			return foot.z > 0 && foot.x > .1f && foot.x < .9f && foot.y > .1f && foot.y < .9f
				&& head.z > 0 && head.y > .1f && head.y < .9f;
		}

		private static void Capture(string directory, string name, PlayerPrefabBindings player, string state, int width, int height)
		{
			Camera camera = Camera.main;
			Assert.That(camera, Is.Not.Null);
			Canvas canvas = player.CombatHud.Canvas;
			CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
			bool previousScalerEnabled = scaler.enabled;
			float previousScale = canvas.scaleFactor;
			RenderMode previousMode = canvas.renderMode;
			Camera previousCamera = canvas.worldCamera;
			float previousPlane = canvas.planeDistance;
			RenderTexture previousTarget = camera.targetTexture;
			RenderTexture previousActive = RenderTexture.active;
			var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
			Texture2D pixels = null;
			try
			{
				camera.targetTexture = target;
				canvas.renderMode = RenderMode.ScreenSpaceCamera;
				canvas.worldCamera = camera;
				canvas.planeDistance = camera.nearClipPlane + 1f;
				scaler.enabled = false;
				canvas.scaleFactor = Mathf.Pow(width / scaler.referenceResolution.x, 1f - scaler.matchWidthOrHeight)
					* Mathf.Pow(height / scaler.referenceResolution.y, scaler.matchWidthOrHeight);
				Canvas.ForceUpdateCanvases();
				camera.Render();
				RenderTexture.active = target;
				pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
				pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				pixels.Apply();
				File.WriteAllBytes(Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
				File.WriteAllText(Path.Combine(directory, name + ".txt"),
					"UTC: " + DateTime.UtcNow.ToString("O") + "\nUnity: " + Application.unityVersion
					+ "\nScene: " + SceneManager.GetActiveScene().name + "\nState: " + state
					+ "\nSize: " + width + "x" + height + "\nSeed: " + Testumgebung.Saat
					+ "\nInput: Touch\nMethod: Windows Camera.Render; HUD temporaer ScreenSpaceCamera."
					+ "\nKein nativer Screen-Capture, keine Notch-Simulation, kein Touchgeraet-Nachweis.\n");
			}
			finally
			{
				canvas.renderMode = previousMode;
				canvas.worldCamera = previousCamera;
				canvas.planeDistance = previousPlane;
				canvas.scaleFactor = previousScale;
				scaler.enabled = previousScalerEnabled;
				camera.targetTexture = previousTarget;
				RenderTexture.active = previousActive;
				if (pixels != null) Object.Destroy(pixels);
				target.Release();
				Object.Destroy(target);
			}
		}
	}
}
