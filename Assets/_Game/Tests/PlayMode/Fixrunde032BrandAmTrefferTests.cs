using Eidren.AI;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// F32-005, geschlossenes Testloch (20.08.2026).
	///
	/// Der bestehende Brandtest ruft <c>ApplyBurn</c> DIREKT am Gegner auf.
	/// Er beweist damit den Takt und die Schadenssumme — aber nicht, dass ein
	/// Waffentreffer den Brand ueberhaupt ausloest. Genau dazwischen sitzt
	/// <c>PlayerCombatController.ApplyPassiveBurn</c>, und genau die Stelle
	/// war ungeprueft: Faellt der Aufruf weg, bleibt die ganze bisherige
	/// Suite gruen und Ignivars Passiv ist im Spiel wirkungslos — derselbe
	/// Fehler, den F32-005 ueberhaupt erst behoben hat.
	///
	/// Deshalb hier die volle Kette in einer echten Zone: gefangenes Ignivar,
	/// ausgeruestete Waffe, echter Angriffsknopf, echter Treffer.
	/// </summary>
	public sealed class Fixrunde032BrandAmTrefferTests
	{
		private PlayerPrefabBindings _spieler;

		private ZonePlayerSpawner _spawner;

		private WildlingController _ziel;

		[UnityTest]
		public IEnumerator Waffentreffer_EntzuendetDasZielMitIgnivar()
		{
			yield return Aufbau("ignivar");

			float vorher = _ziel.CurrentHealth;
			_spawner.Input.PressAttack();
			yield return WarteBis(() => _ziel.CurrentHealth < vorher, 3f, "Der Hammer hat nicht getroffen.");

			Assert.That(_ziel.IsAlive, Is.True, "Vorbedingung: Der Treffer darf das Ziel nicht toeten.");
			Assert.That(_ziel.IsBurning, Is.True,
				"Der Waffentreffer muss das Ziel entzuenden — ApplyPassiveBurn haengt am Treffer.");

			// Und der Brand muss wirken, nicht nur gesetzt sein: Ohne weiteren
			// Angriff sinkt das Leben allein durch den Nachbrand weiter.
			float nachDemTreffer = _ziel.CurrentHealth;
			yield return WarteBis(() => !_ziel.IsBurning, 12f, "Der Brand ist nie ausgegangen.");
			Assert.That(_ziel.CurrentHealth, Is.LessThan(nachDemTreffer),
				"Der Nachbrand muss ohne weiteren Angriff Leben abziehen.");

			yield return Testumgebung.LeereWeltHinterlassen("Zone_Greenwood");
		}

		/// <summary>
		/// Gegenprobe. Ohne sie wuerde der Test oben auch dann gruen bleiben,
		/// wenn IRGENDETWAS jeden Treffer entzuendet — der Nachweis haenge
		/// dann nicht mehr an Ignivar.
		/// </summary>
		[UnityTest]
		public IEnumerator Waffentreffer_EntzuendetOhneIgnivarNicht()
		{
			yield return Aufbau("terrock");

			float vorher = _ziel.CurrentHealth;
			_spawner.Input.PressAttack();
			yield return WarteBis(() => _ziel.CurrentHealth < vorher, 3f, "Der Hammer hat nicht getroffen.");

			Assert.That(_ziel.IsBurning, Is.False,
				"Nur Ignivars Passiv entzuendet — sonst misst der Nachweis oben nicht das Passiv.");

			yield return Testumgebung.LeereWeltHinterlassen("Zone_Greenwood");
		}

		private IEnumerator Aufbau(string eidraId)
		{
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			EidrenServiceRoot dienste = EidrenServiceRoot.FindOrCreate();
			dienste.GameSession.StartNewGame();

			Assert.That(dienste.GameSession.PlayerEquipment.TryEquip(EquipmentSlot.Weapon1,
				ItemStack.Create(dienste.ContentDatabase.GetItem("hammer"), 1), out string waffenfehler),
				Is.True, waffenfehler);
			dienste.GameSession.SetActiveWeaponId("hammer");
			Assert.That(dienste.EidraRoster.TryCapture(eidraId, out var _, out string fangfehler),
				Is.True, fangfehler);

			AsyncOperation laden = SceneManager.LoadSceneAsync("Zone_Greenwood", LoadSceneMode.Single);
			Assert.That(laden, Is.Not.Null);
			while (!laden.isDone)
			{
				yield return null;
			}
			for (int i = 0; i < 3; i++)
			{
				yield return null;
			}

			_spieler = Object.FindFirstObjectByType<PlayerPrefabBindings>();
			_spawner = Object.FindFirstObjectByType<ZonePlayerSpawner>();
			Assert.That(_spieler, Is.Not.Null, "Kein Spieler in der Zone.");
			Assert.That(_spawner, Is.Not.Null, "Kein Spawner in der Zone.");
			Assert.That(_spieler.EidraTeam.ActiveData, Is.Not.Null, "Kein aktives Eidra gebunden.");
			Assert.That(_spieler.EidraTeam.ActiveData.Id, Is.EqualTo(eidraId),
				"Vorbedingung: Das gefangene Eidra muss auf Platz 1 stehen.");

			WildlingController[] wildlinge = Object.FindObjectsByType<WildlingController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			Assert.That(wildlinge, Is.Not.Empty, "Keine Gegner in der Zone.");
			_ziel = wildlinge[0];
			for (int i = 1; i < wildlinge.Length; i++)
			{
				// Fremde Gegner abschalten: Sonst kann ein zweiter Treffer
				// oder ein fremder Tod die Messung verwaschen.
				wildlinge[i].gameObject.SetActive(value: false);
			}

			Vector3 anlauf = _ziel.transform.position - _ziel.transform.forward * 1.2f + Vector3.up * 0.05f;
			_spieler.Motor.Teleport(anlauf);
			Vector3 richtung = _ziel.transform.position - _spieler.transform.position;
			richtung.y = 0f;
			_spieler.transform.rotation = Quaternion.LookRotation(richtung.normalized);
			Physics.SyncTransforms();
			yield return null;
		}

		/// <summary>
		/// Wartet in SPIELZEIT — im Batchmodus laufen Spiel- und echte Zeit
		/// weit auseinander, und der Brandtakt haengt an Time.deltaTime.
		/// </summary>
		private static IEnumerator WarteBis(Func<bool> bedingung, float budget, string meldung)
		{
			float zeit = 0f;
			while (!bedingung() && zeit < budget)
			{
				zeit += Time.deltaTime;
				yield return null;
			}
			Assert.That(bedingung(), Is.True, meldung);
		}
	}
}
