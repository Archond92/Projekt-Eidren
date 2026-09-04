using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using NUnit.Framework;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// F32-002, zweiter Anlauf. Der erste Test hat den Rüstungsbonus VOR
	/// <c>Initialize</c> gesetzt — eine Reihenfolge, die im Spiel nie
	/// vorkommt. Beim Zonenwechsel entsteht die Figur neu, das Damageable ist
	/// frisch (Bonus 0), und erst nach dem Auffüllen wird die Rüstung
	/// angebunden. Deshalb blieb es bei 100/110, obwohl der Test grün war.
	///
	/// Dieser Test bildet den echten Ablauf ab: Rüstung liegt in der Sitzung,
	/// dann wird eine Zone betreten.
	/// </summary>
	public sealed class Fixrunde032LebenTests
	{
		[UnityTest]
		public IEnumerator Zonenankunft_FuelltMitRuestungAufDasEchteMaximum()
		{
			yield return LadeSzene("Zone_EmberRuins");
			EidrenServiceRoot services = EidrenServiceRoot.FindOrCreate();
			PlayerEquipment ausruestung = services.GameSession.PlayerEquipment;

			// Volle Stoffrüstung: 0,02 + 0,04 + 0,01 + 0,03 = 0,10 → +10 LP.
			Anlegen(ausruestung, EquipmentSlot.Head, "armor_wanderer_hood");
			Anlegen(ausruestung, EquipmentSlot.Chest, "armor_wanderer_coat");
			Anlegen(ausruestung, EquipmentSlot.Hands, "armor_wanderer_bracers");
			Anlegen(ausruestung, EquipmentSlot.Legs, "armor_wanderer_legs");
			yield return null;

			// Zonenwechsel — genau hier heilt das Spiel.
			yield return LadeSzene("HomeBase");
			PlayerPrefabBindings player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
			Assert.That(player, Is.Not.Null, "Kein Spieler in der Heimatbasis.");
			Damageable leben = player.Damageable;
			Assert.That(leben, Is.Not.Null);

			Assert.That(leben.MaxHealth, Is.EqualTo(110f).Within(0.01f),
				"Volle Stoffrüstung hebt das Maximum auf 110.");
			Assert.That(leben.CurrentHealth, Is.EqualTo(leben.MaxHealth).Within(0.01f),
				$"Die Ankunft muss auf das echte Maximum füllen, steht aber auf {leben.CurrentHealth}.");
		}

		private static void Anlegen(PlayerEquipment ausruestung, EquipmentSlot slot, string itemId)
		{
			// Haltbare Gegenstände brauchen eine eigene Instanzkennung und
			// eine Haltbarkeit zwischen 1 und 80.
			Assert.That(ausruestung.TryEquip(slot, new ItemStack(itemId, 1, "item.f32test." + itemId, 64), out var error), Is.True,
				itemId + ": " + error);
		}

		private static IEnumerator LadeSzene(string name)
		{
			// Feste Saat: sonst wuerfelt jeder Lauf die Zone neu.
			Testumgebung.SaatFestlegen();
			AsyncOperation load = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
			Assert.That(load, Is.Not.Null, "Szene nicht ladbar: " + name);
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
