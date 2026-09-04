using Eidren.AI;
using Eidren.Composition;
using NUnit.Framework;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// F31-020-Repro in der ECHTEN Zone (18.08.2026): Die Testwelt hat
	/// PatrolRadius 0 und verdeckte den Fehler — im echten Prefab hält der
	/// Agent mit Kampf-Stoppdistanz 1,6 vor jedem Rückkehr-/Patrouillenziel
	/// an und die 0,55er-Toleranz wird nie erreicht.
	/// </summary>
	public sealed class LeashReaggroZoneTests
	{
		[UnityTest]
		public IEnumerator Gruenwald_GegnerGreiftNachDerLeinenRueckkehrWiederAn()
		{
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
			ZoneController zone = Object.FindFirstObjectByType<ZoneController>();
			Assert.That(zone.EnemyPopulator.Spawned.Count, Is.GreaterThan(0), "Keine Gegner aufgestellt.");
			WildlingController gegner = zone.EnemyPopulator.Spawned[0];
			Vector3 heim = gegner.transform.position;
			// Aggro: Spieler direkt in die Witterung stellen.
			Teleport(player, heim + new Vector3(0f, 0f, 3f));
			zeit = 0f;
			while (gegner.EnemyState != EnemyState.Chase && gegner.EnemyState != EnemyState.Alert && zeit < 10f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			Assert.That(gegner.EnemyState, Is.EqualTo(EnemyState.Chase).Or.EqualTo(EnemyState.Alert),
				"Gegner hat den Spieler nie gewittert.");
			// Spieler weit über die Leine (18) hinausziehen.
			Teleport(player, heim + new Vector3(0f, 0f, 26f));
			zeit = 0f;
			while (gegner.EnemyState != EnemyState.Return && zeit < 12f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			Assert.That(gegner.EnemyState, Is.EqualTo(EnemyState.Return), "Gegner hat die Rückkehr nie begonnen.");
			// Heimweg REAL abwarten (kein Warp): Er muss Return verlassen.
			zeit = 0f;
			while (gegner.EnemyState == EnemyState.Return && zeit < 25f)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			Assert.That(gegner.EnemyState, Is.Not.EqualTo(EnemyState.Return),
				$"Gegner hängt {Vector3.Distance(gegner.transform.position, heim):0.00} m vor seinem Heimpunkt dauerhaft im Rückkehr-Zustand.");
			// Wieder in die Witterung: Er muss erneut angreifen.
			Teleport(player, gegner.transform.position + new Vector3(0f, 0f, 3.5f));
			zeit = 0f;
			bool wiederAggressiv = false;
			while (zeit < 10f)
			{
				EnemyState state = gegner.EnemyState;
				if (state == EnemyState.Alert || state == EnemyState.Chase || state == EnemyState.ChooseAttack || state == EnemyState.Telegraph || state == EnemyState.ExecuteAttack)
				{
					wiederAggressiv = true;
					break;
				}
				zeit += Time.deltaTime;
				yield return null;
			}
			Assert.That(wiederAggressiv, Is.True,
				$"Gegner bleibt nach der Rückkehr passiv (Zustand {gegner.EnemyState}).");
		}

		private static void Teleport(PlayerPrefabBindings player, Vector3 ziel)
		{
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
		}
	}
}
