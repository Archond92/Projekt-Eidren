using Eidren.Composition;
using Eidren.Interaction;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// Diagnose fuer den Item-Aufnahme-Bruch (vier Altfaelle, isoliert rot
	/// seit dem Fenster 03.-06.08.2026): loggt jede Station des Pfads
	/// virtueller Interact -> InteractionController -> Sofortaufnahme, um die
	/// Bruchstelle zu lokalisieren. Kein Suitentest — nur per Filter und mit
	/// gesetzter EIDREN_DIAGNOSE-Umgebungsvariable.
	/// </summary>
	public sealed class ItemAufnahmeDiagnoseTests
	{
		[UnityTest]
		[Explicit("Diagnose — loggt statt zu pruefen.")]
		public IEnumerator Aufnahmepfad_Stationen()
		{
			if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EIDREN_DIAGNOSE")))
			{
				Assert.Ignore("EIDREN_DIAGNOSE nicht gesetzt.");
			}

			AsyncOperation op = SceneManager.LoadSceneAsync("Zone_Greenwood", LoadSceneMode.Single);
			while (!op.isDone)
			{
				yield return null;
			}
			for (int i = 0; i < 5; i++)
			{
				yield return null;
			}

			PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
			ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
			Debug.Log("[DIAG] player=" + (player != null) + " spawner=" + (spawner != null)
				+ " spawnerInput=" + ((spawner != null && spawner.Input != null) ? spawner.Input.GetType().Name : "null"));

			// Doppel-Root-Verdacht: Wenn ein Vorgaengertest den ServiceRoot
			// zerstoert und neu aufbaut, koennten Spieler und Test an
			// VERSCHIEDENEN Sessions haengen — Aufnahme landet dann im einen
			// Inventar, gezaehlt wird das andere.
			EidrenServiceRoot[] roots = UnityEngine.Object.FindObjectsByType<EidrenServiceRoot>(
				FindObjectsInactive.Include, FindObjectsSortMode.None);
			Debug.Log("[DIAG] ServiceRoots=" + roots.Length
				+ " InstanceGesetzt=" + (EidrenServiceRoot.Instance != null)
				+ " FindOrCreate==Instance=" + ReferenceEquals(EidrenServiceRoot.FindOrCreate(), EidrenServiceRoot.Instance));

			var inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
			inventory.Clear();

			// WorldItem exakt wie der echte Test: ueber den ZoneLootController.
			ZoneLootController loot = UnityEngine.Object.FindFirstObjectByType<ZoneLootController>();
			int vorher = loot.ActiveItems.Count;
			int gespawnt = loot.SpawnGuaranteedItems(
				new Eidren.Core.Services.InventoryItemAmount[]
				{
					new Eidren.Core.Services.InventoryItemAmount("plant_fiber", 2)
				},
				player.transform.position + Vector3.forward * 1.5f, player.transform, "diag.loot");
			Debug.Log("[DIAG] SpawnGuaranteedItems=" + gespawnt
				+ " aktiveItems=" + loot.ActiveItems.Count + " (vorher " + vorher + ")");
			WorldItemController fiber = loot.ActiveItems[vorher];

			player.Motor.Teleport(fiber.transform.position + Vector3.back * 0.8f + Vector3.up * 0.05f);
			player.transform.forward = Vector3.forward;
			Physics.SyncTransforms();
			player.Interaction.RefreshTargetsNow();
			yield return null;

			Debug.Log("[DIAG] CurrentTarget=" + Beschreibe(player.Interaction.CurrentTarget)
				+ " istFiber=" + ReferenceEquals(player.Interaction.CurrentTarget, fiber)
				+ " IsInteracting=" + player.Interaction.IsInteracting
				+ " InteractHeld(vorher)=" + spawner.Input.InteractHeld);

			// Druck 1
			spawner.Input.SetVirtualInteract(held: true);
			Debug.Log("[DIAG] nach Set(true): InteractHeld=" + spawner.Input.InteractHeld);
			yield return null;
			Debug.Log("[DIAG] +1 Frame: IsInteracting=" + player.Interaction.IsInteracting
				+ " inv=" + inventory.GetTotalAmount("plant_fiber")
				+ " fiberAktiv=" + (fiber != null && fiber.gameObject.activeSelf));
			spawner.Input.SetVirtualInteract(held: false);
			yield return null;
			Debug.Log("[DIAG] nach Release: IsInteracting=" + player.Interaction.IsInteracting
				+ " inv=" + inventory.GetTotalAmount("plant_fiber")
				+ " fiberAktiv=" + (fiber != null && fiber.gameObject.activeSelf));

			// Druck 2 — entlarvt eine Release-Sperre aus der Bindephase.
			spawner.Input.SetVirtualInteract(held: true);
			yield return null;
			spawner.Input.SetVirtualInteract(held: false);
			yield return null;
			Debug.Log("[DIAG] nach Druck 2: inv=" + inventory.GetTotalAmount("plant_fiber")
				+ " fiberAktiv=" + (fiber != null && fiber.gameObject.activeSelf)
				+ " feedback=" + ((player.InventoryFeedback != null) ? player.InventoryFeedback.LastMessage : "(kein Presenter)"));
		}

		private static string Beschreibe(object ziel)
		{
			return (ziel == null) ? "null" : ziel.GetType().Name;
		}

		/// <summary>
		/// Spiegelt das EXAKTE Timing des echten roten Tests (LoadGreenwood:
		/// 4 Warteframes, Platzieren und Druecken im selben Frame) und
		/// protokolliert jeden Frame: der lockere Stationen-Ablauf oben bleibt
		/// nach dem Vergifter gruen, der echte Test rot — der Unterschied ist
		/// ein einziger Frame, und dieses Protokoll benennt den Schalter.
		/// </summary>
		[UnityTest]
		[Explicit("Diagnose — loggt statt zu pruefen.")]
		public IEnumerator Aufnahmepfad_EchtesTiming()
		{
			if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EIDREN_DIAGNOSE")))
			{
				Assert.Ignore("EIDREN_DIAGNOSE nicht gesetzt.");
			}

			AsyncOperation op = SceneManager.LoadSceneAsync("Zone_Greenwood", LoadSceneMode.Single);
			while (!op.isDone)
			{
				yield return null;
			}
			ZonePlayerSpawner spawner = null;
			PlayerPrefabBindings player = null;
			for (int frame = 0; frame < 4; frame++)
			{
				spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
				player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
				Debug.Log("[TIM] Warteframe " + frame + ": " + Zustand(spawner, player));
				yield return null;
			}
			player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
			spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
			var inventory = EidrenServiceRoot.FindOrCreate().GameSession.PlayerInventory;
			inventory.Clear();

			ZoneLootController loot = UnityEngine.Object.FindFirstObjectByType<ZoneLootController>();
			int vorher = loot.ActiveItems.Count;
			loot.SpawnGuaranteedItems(
				new Eidren.Core.Services.InventoryItemAmount[]
				{
					new Eidren.Core.Services.InventoryItemAmount("plant_fiber", 2)
				},
				player.transform.position + Vector3.forward * 1.5f, player.transform, "diag.loot");
			WorldItemController fiber = loot.ActiveItems[vorher];

			// PlacePlayerAt-Spiegel + SOFORTIGER Druck, wie im echten Test.
			player.Motor.Teleport(fiber.transform.position + Vector3.back * 0.8f + Vector3.up * 0.05f);
			player.transform.forward = Vector3.forward;
			Physics.SyncTransforms();
			player.Interaction.RefreshTargetsNow();
			Debug.Log("[TIM] Druckframe: ziel=" + ReferenceEquals(player.Interaction.CurrentTarget, fiber)
				+ " " + Zustand(spawner, player));
			spawner.Input.SetVirtualInteract(held: true);
			yield return null;
			Debug.Log("[TIM] +1: inv=" + inventory.GetTotalAmount("plant_fiber") + " " + Zustand(spawner, player));
			spawner.Input.SetVirtualInteract(held: false);
			yield return null;
			Debug.Log("[TIM] +2: inv=" + inventory.GetTotalAmount("plant_fiber")
				+ " fiberAktiv=" + (fiber != null && fiber.gameObject.activeSelf)
				+ " " + Zustand(spawner, player));
		}

		private static string Zustand(ZonePlayerSpawner spawner, PlayerPrefabBindings player)
		{
			var root = EidrenServiceRoot.Instance;
			string gp = (spawner != null && spawner.Input != null)
				? spawner.Input.GameplayEnabled.ToString() : "-";
			string held = (spawner != null && spawner.Input != null)
				? spawner.Input.InteractHeld.ToString() : "-";
			string blk = (root != null && root.SceneFlowService != null)
				? root.SceneFlowService.IsInputBlocked.ToString() : "-";
			string act = (player != null) ? player.Interaction.IsInteracting.ToString() : "-";
			return "gp=" + gp + " blk=" + blk + " held=" + held + " act=" + act;
		}
	}
}
