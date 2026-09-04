using Eidren.Composition;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
public sealed class CompositionCompletenessTests
{
	[UnityTest]
	public IEnumerator EveryZone_SpawnsAFullyWiredPlayer()
	{
		EidrenServiceRoot.FindOrCreate().GameSession.StartNewGame();
		string[] array = new string[5] { "HomeBase", "Zone_Greenwood", "Zone_Quarry", "Zone_Marsh", "Zone_EmberRuins" };
		foreach (string sceneName in array)
		{
			yield return LoadZone(sceneName);
			ZonePlayerSpawner zonePlayerSpawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
			Assert.That<ZonePlayerSpawner>(zonePlayerSpawner, (IResolveConstraint)(object)Is.Not.Null, sceneName + ": kein ZonePlayerSpawner.", Array.Empty<object>());
			Assert.That<PlayerPrefabBindings>(zonePlayerSpawner.SpawnedPlayer, (IResolveConstraint)(object)Is.Not.Null, sceneName + ": kein Spieler gespawnt.", Array.Empty<object>());
			PlayerPrefabBindings player = zonePlayerSpawner.SpawnedPlayer;
			List<string> missing = new List<string>();
			Require(missing, player.Motor, "Motor");
			Require(missing, player.Combat, "Combat");
			Assert.That(player.Combat.Targeting, Is.Not.Null, sceneName + ": gemeinsames Combat-Targeting fehlt.");
			Assert.That(player.EidraTeam.Targeting, Is.SameAs(player.Combat.Targeting), sceneName + ": konkurrierende Zielservices.");
			Require(missing, player.GetComponentInChildren<CombatTargetIndicator>(true), "CombatTargetIndicator");
			Require(missing, player.Damageable, "Damageable");
			Require(missing, player.Interaction, "Interaction");
			Require(missing, player.Consumables, "Consumables");
			Require(missing, player.WeaponHitbox, "WeaponHitbox");
			Require(missing, player.VisualAnimator, "VisualAnimator");
			Require(missing, player.EidraTeam, "EidraTeam");
			Require(missing, player.CombatHud, "CombatHud");
			Require(missing, player.InventoryWindow, "InventoryWindow");
			if (player.EidraTeam != null && !player.EidraTeam.IsInitialized)
			{
				missing.Add("EidraTeam (Komponente da, aber nicht initialisiert)");
			}
			Assert.That<List<string>>(missing, (IResolveConstraint)(object)Is.Empty, sceneName + ": Der Kompositionspfad hat Systeme ausgelassen. Fehlende Verdrahtung ist ein Fehler, kein Nichts (§21):\n" + string.Join("\n", missing), Array.Empty<object>());
			Assert.That<WeaponData>(player.Combat.ActiveWeapon, (IResolveConstraint)(object)Is.Null, sceneName + ": M8.2 verbietet eine Startwaffe.", Array.Empty<object>());
			Assert.That<EidraData>(player.EidraTeam.ActiveData, (IResolveConstraint)(object)Is.Null, sceneName + ": M8.2 verbietet ein Start-Eidra.", Array.Empty<object>());
			if (sceneName == "HomeBase")
			{
				Require(missing, player.TechnologyWindow, "TechnologyWindow");
				Require(missing, player.BuildingMenu, "BuildingMenu");
				Require(missing, player.BuildingPlacement, "BuildingPlacement");
				Require(missing, player.CraftingWindow, "CraftingWindow");
				Require(missing, player.StorageWindow, "StorageWindow");
				Assert.That<List<string>>(missing, (IResolveConstraint)(object)Is.Empty, "HomeBase building composition is incomplete.", Array.Empty<object>());
				ZoneController zoneController = UnityEngine.Object.FindFirstObjectByType<ZoneController>();
				Assert.That<ZoneLayout>(zoneController.ResourceLayout, (IResolveConstraint)(object)Is.Not.Null);
				Assert.That<int>(zoneController.ResourceLayout.Placements.Count, (IResolveConstraint)(object)Is.EqualTo((object)58), "26 Holz + 18 Stein + 10 Faser + 4 Beeren.", Array.Empty<object>());
				Assert.That<IReadOnlyList<ZoneNodePlacement>>(zoneController.ResourceLayout.Placements, (IResolveConstraint)(object)Has.All.Matches<ZoneNodePlacement>((Predicate<ZoneNodePlacement>)((ZoneNodePlacement placement) => placement.Definition != null)), "Jeder Vorratsknoten braucht eine Definition.", Array.Empty<object>());
			}
		}
	}

	private static void Require(List<string> missing, UnityEngine.Object component, string label)
	{
		if (component == null)
		{
			missing.Add(label);
		}
	}

	private static IEnumerator LoadZone(string sceneName)
	{
		AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
		Assert.That<AsyncOperation>(load, (IResolveConstraint)(object)Is.Not.Null);
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
