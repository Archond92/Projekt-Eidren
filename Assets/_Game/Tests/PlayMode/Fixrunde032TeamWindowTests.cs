using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.UI;
using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// F32-006: Ein gefangenes Eidra jenseits der aktiven Plätze war nicht
	/// mehr einsetzbar. Geprüft wird die ganze Kette in einer echten Zone:
	/// Der Spawner bindet das Fenster an, ein Knopfdruck in der Zeile setzt
	/// das Eidra auf den Platz, und der Roster übernimmt es.
	/// </summary>
	public sealed class Fixrunde032TeamWindowTests
	{
		[UnityTest]
		public IEnumerator Bankspieler_LaesstSichUeberDasFensterEinwechseln()
		{
			yield return LadeZone();
			PlayerPrefabBindings player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
			Assert.That(player, Is.Not.Null, "Kein Spieler in der Zone.");
			EidraTeamWindow fenster = player.EidraTeamWindow;
			Assert.That(fenster, Is.Not.Null, "Der Spawner muss das Eidra-Fenster anbinden.");

			EidraRosterService roster = EidrenServiceRoot.FindOrCreate().EidraRoster;
			Assert.That(roster.TryCapture("terrock", out var erstes, out var fehler1), Is.True, fehler1);
			Assert.That(roster.TryCapture("noctarion", out var bank, out var fehler2), Is.True, fehler2);
			yield return null;

			// Der erste Fang belegt Platz 1, der zweite landet je nach
			// Freischaltung auf Platz 2 oder auf der Bank. Für den Nachweis
			// zählt Platz 1: Dort muss sich der zweite einwechseln lassen.
			fenster.Refresh();
			Assert.That(roster.Roster.GetActiveInstanceIds()[0], Is.EqualTo(erstes.InstanceId),
				"Vorbedingung: Der erste Fang steht auf Platz 1.");

			int zeile = ZeileVon(roster, bank.InstanceId);
			Assert.That(zeile, Is.GreaterThanOrEqualTo(0), "Der Bankspieler muss eine Zeile haben.");
			KnopfDruecken(fenster, zeile);
			yield return null;

			Assert.That(roster.Roster.GetActiveInstanceIds()[0], Is.EqualTo(bank.InstanceId),
				"Nach dem Knopfdruck muss der Bankspieler auf Platz 1 stehen.");
			Assert.That(roster.Roster.GetActiveInstanceIds(), Has.No.Member(erstes.InstanceId).Or.Length.EqualTo(2),
				"Der bisherige Platzhalter rutscht auf die Bank oder auf den zweiten Platz.");
		}

		/// <summary>Index der Zeile, die zu dieser Instanz gehört (Roster ist sortiert).</summary>
		private static int ZeileVon(EidraRosterService roster, string instanceId)
		{
			EidraInstanceState[] alle = roster.Roster.GetAll();
			for (int i = 0; i < alle.Length; i++)
			{
				if (alle[i].InstanceId == instanceId)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Drückt den „auf Platz 1"-Knopf der Zeile — über den echten
		/// Button, damit die Verdrahtung im Prefab mitgeprüft wird.
		/// </summary>
		private static void KnopfDruecken(EidraTeamWindow fenster, int zeile)
		{
			EidraTeamRowView[] zeilen = (EidraTeamRowView[])typeof(EidraTeamWindow)
				.GetField("rowViews", BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(fenster);
			Assert.That(zeilen, Is.Not.Null.And.Length.GreaterThan(zeile), "Zu wenige Zeilen im Prefab.");
			zeilen[zeile].FirstSlotButton.onClick.Invoke();
		}

		private static IEnumerator LadeZone()
		{
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			// Feste Saat: sonst wuerfelt jeder Lauf die Zone neu.
			Testumgebung.SaatFestlegen();
			AsyncOperation load = SceneManager.LoadSceneAsync("Zone_EmberRuins", LoadSceneMode.Single);
			Assert.That(load, Is.Not.Null);
			while (!load.isDone)
			{
				yield return null;
			}
			for (int i = 0; i < 3; i++)
			{
				yield return null;
			}
		}
	}
}
