using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.UI;
using NUnit.Framework;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// N04-003: Der Tooltip muss im echten HUD ankommen — Feld vorhanden,
	/// Auslöser an beiden Fähigkeitsknöpfen, und der Text kommt aus dem
	/// aktiven Eidra. Die reine Textrechnung steht in den EditMode-Tests.
	/// </summary>
	public sealed class Featurerunde04TooltipPlayTests
	{
		[UnityTest]
		public IEnumerator Faehigkeitsknopf_ZeigtDenTextDesAktivenEidra()
		{
			yield return LadeZone();
			PlayerPrefabBindings player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
			Assert.That(player, Is.Not.Null, "Kein Spieler in der Zone.");
			Assert.That(player.CombatHud, Is.Not.Null, "Kein Kampf-HUD.");

			HudTooltipPanel feld = player.CombatHud.GetComponentInChildren<HudTooltipPanel>(includeInactive: true);
			Assert.That(feld, Is.Not.Null,
				"Das HUD hat kein Tooltip-Feld — 'Eidren/V0.4/N04-003 Tooltip ins Kampf-HUD einbauen' laufen lassen.");

			HudAbilityTooltip[] ausloeser = player.CombatHud.GetComponentsInChildren<HudAbilityTooltip>(includeInactive: true);
			Assert.That(ausloeser.Length, Is.EqualTo(2), "Beide Fähigkeitsknöpfe brauchen einen Auslöser.");

			// Ohne Eidra gibt es nichts zu erklären.
			foreach (HudAbilityTooltip einzeln in ausloeser)
			{
				Assert.That(einzeln.Text(), Is.Empty, "Ohne aktives Eidra bleibt der Tooltip leer.");
			}

			EidraRosterService roster = EidrenServiceRoot.FindOrCreate().EidraRoster;
			Assert.That(roster.TryCapture("terrock", out var _, out var fehler), Is.True, fehler);
			yield return null;
			yield return null;

			bool gefunden = false;
			foreach (HudAbilityTooltip einzeln in ausloeser)
			{
				if (einzeln.Text().Contains("Felsbrecher") || einzeln.Text().Contains("Steinhaut"))
				{
					gefunden = true;
				}
			}
			Assert.That(gefunden, Is.True,
				"Nach dem Fang muss der Tooltip Terrocks Fähigkeiten nennen.");

			yield return Testumgebung.LeereWeltHinterlassen("Zone_EmberRuins");

			// Aufräumen: Das gefangene Eidra bleibt in der Sitzung und liefe
			// danach als Begleiter in jedem folgenden Test mit. Aktives
			// Gespann leeren, damit kein Begleiter zurückbleibt.
			roster.TrySetActiveTeam(System.Array.Empty<string>(), out var _);
		}

		private static IEnumerator LadeZone()
		{
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
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
