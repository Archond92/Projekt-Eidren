using Eidren.AI;
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// Diagnose #34 (18.08.2026): "In der Schmiede kann ich den Wächter
	/// nicht angreifen." Der Test fährt die echte Verlies-Route und prüft
	/// jedes Glied der Trefferkette einzeln: Spawn, Lebenszustand,
	/// Erreichbarkeit über die echte Spieler-Hitbox, Schadenswirkung in
	/// geschlossener Phase (Schutz 0,35), Kernöffnung über Stagger und
	/// voller Schaden in offener Phase. Bricht ein Glied, benennt die
	/// Fehlermeldung die Wurzel; läuft alles grün, ist die Mechanik in
	/// Ordnung und das Problem eine Lesbarkeitsfrage.
	/// </summary>
	public sealed class ForgeGuardianCombatTests
	{
		private const float ProbeHealthDamage = 40f;

		private const float ProbeStaggerDamage = 25f;

		[UnityTest]
		public IEnumerator Kernwaechter_IstUeberDieEchteTrefferketteVerwundbar()
		{
			if (EidrenServiceRoot.Instance != null)
			{
				Object.Destroy(EidrenServiceRoot.Instance.gameObject);
				yield return null;
			}
			// Echte Route: erst eine Zone (voller Dienstaufbau), dann der
			// Betretensfluss des Verlieseingangs (Direktladen = Phantomwelt).
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

			// Glied 1: Der Kernwächter steht nach dem Betreten in der Szene.
			zeit = 0f;
			CoreGuardianController waechter = null;
			while (waechter == null && zeit < 10f)
			{
				zeit += Time.deltaTime;
				waechter = Object.FindFirstObjectByType<CoreGuardianController>();
				yield return null;
			}
			Assert.That(waechter, Is.Not.Null, "Der Kernwächter wurde im Verlies nicht aufgestellt.");

			// Glied 2: Er lebt und trägt echte Werte (gegen die Hypothese
			// leerer Anchor-Daten oder eines Totspawns über den Roster-Stand).
			Assert.That(waechter.IsAlive, Is.True, "Der Kernwächter spawnt bereits tot — Totspawn statt Kampfproblem.");
			Assert.That(waechter.MaxHealth, Is.GreaterThan(1f), "Der Kernwächter hat kein Lebensmaximum — die Anchor-Daten sind leer.");
			Assert.That(waechter.CurrentHealth, Is.EqualTo(waechter.MaxHealth).Within(0.5f),
				"Der Kernwächter beginnt den Erstlauf nicht mit vollem Leben.");
			Assert.That(waechter.Phase, Is.EqualTo(CoreGuardianPhase.Closed), "Der Kern muss geschlossen beginnen.");

			// Spieler in Waffenreichweite stellen und auf den Wächter richten.
			Vector3 richtung = spieler.transform.position - waechter.transform.position;
			richtung.y = 0f;
			richtung = (richtung.sqrMagnitude < 0.01f) ? Vector3.back : richtung.normalized;
			Vector3 standort = waechter.transform.position + richtung * 2.2f;
			CharacterController kapsel = spieler.GetComponent<CharacterController>();
			if (kapsel != null)
			{
				kapsel.enabled = false;
			}
			spieler.transform.position = standort;
			spieler.transform.rotation = Quaternion.LookRotation(-richtung);
			if (kapsel != null)
			{
				kapsel.enabled = true;
			}
			Physics.SyncTransforms();

			// Glied 3: Die ECHTE Spieler-Hitbox (Layer, OverlapSphere,
			// Registry, Sektorgeometrie) muss den Wächter als Ziel liefern.
			// Die Testsession trägt kein Loadout — der Schritt nutzt die
			// Hammer-Parametrik, die Hitbox-Komponente bleibt die echte.
			MeleeWeaponHitbox klinge = spieler.GetComponentInChildren<MeleeWeaponHitbox>(includeInactive: true);
			Assert.That(klinge, Is.Not.Null, "Der Spieler hat keine Nahkampf-Hitbox.");
			var schritt = new Eidren.Data.AttackStepData
			{
				Duration = 0.6f,
				HitTime = 0.2f,
				HitWindowDuration = 0.2f,
				HitboxRange = 3f,
				HitboxAngle = 120f
			};
			float vorher = waechter.CurrentHealth;
			var getroffen = new List<Transform>();
			klinge.BeginWindow(schritt);
			int treffer = klinge.Evaluate(spieler.transform, ziel =>
			{
				getroffen.Add(ziel.Transform);
				ziel.Damageable.ApplyDamage(new DamageInfo(ProbeHealthDamage, ProbeStaggerDamage,
					ziel.Transform.position, spieler.gameObject, isBackAttack: false, "diagnose.schlag", "spieler"));
			});
			klinge.EndWindow();
			Assert.That(treffer, Is.GreaterThan(0),
				"Die Spieler-Hitbox findet im Verlies überhaupt kein Ziel — Kette bricht vor der Registry.");
			Assert.That(getroffen.Contains(waechter.transform), Is.True,
				"Die Spieler-Hitbox trifft den Kernwächter nicht, obwohl er direkt vor ihr steht.");

			// Glied 4: Geschlossener Kern nimmt reduzierten Schaden (Schutz 0,35).
			float verlustGeschlossen = vorher - waechter.CurrentHealth;
			Assert.That(verlustGeschlossen, Is.EqualTo(ProbeHealthDamage * 0.65f).Within(1f),
				$"Geschlossen müssen 65 % Schaden durchgehen; tatsächlich sanken die HP um {verlustGeschlossen:0.0}.");

			// Lesbarkeit (#34): Der Wächter trägt die Gegner-Statusleiste —
			// gebunden, mit Namen, sinkendem Lebensbalken und Schutzlabel.
			Eidren.UI.WildlingStatusBars leiste = waechter.GetComponentInChildren<Eidren.UI.WildlingStatusBars>(includeInactive: true);
			Assert.That(leiste, Is.Not.Null, "Der Kernwächter hat keine Statusleiste — Treffer bleiben unsichtbar.");
			Assert.That(leiste.NameLabel, Is.Not.Null, "Die Wächterleiste hat kein Namenslabel.");
			Assert.That(leiste.NameLabel.text, Is.Not.Empty, "Das Namenslabel des Wächters bleibt leer.");
			Assert.That(leiste.HealthFill.fillAmount, Is.LessThan(1f),
				"Der Lebensbalken des Wächters bewegt sich nach einem Treffer nicht.");
			UnityEngine.UI.Text schutz = leiste.WorldCanvas.transform.Find("ProtectionLabel")?.GetComponent<UnityEngine.UI.Text>();
			Assert.That(schutz, Is.Not.Null, "Die Wächterleiste hat kein Schutzlabel.");
			Assert.That(schutz.gameObject.activeSelf, Is.True, "Das Schutzlabel muss in geschlossener Phase sichtbar sein.");

			// Glied 5: Stagger öffnet den Kern (Schwelle 400 im Zyklus).
			for (int i = 0; i < 16 && waechter.Phase == CoreGuardianPhase.Closed; i++)
			{
				waechter.AddStagger(80f);
			}
			Assert.That(waechter.Phase, Is.EqualTo(CoreGuardianPhase.Open),
				"400 Stagger müssen den Kern öffnen — der Öffnungszyklus ist defekt.");
			// Das Schutzlabel folgt dem Phasenwechsel (ein Frame für LateUpdate).
			yield return null;
			Assert.That(schutz.gameObject.activeSelf, Is.False,
				"Das Schutzlabel muss bei offenem Kern verschwinden — sonst liest sich die offene Phase als geschützt.");

			// Glied 6: Offener Kern nimmt vollen Schaden.
			vorher = waechter.CurrentHealth;
			waechter.ApplyDamage(new DamageInfo(ProbeHealthDamage, 0f,
				waechter.transform.position, spieler.gameObject, isBackAttack: false, "diagnose.offen", "spieler"));
			float verlustOffen = vorher - waechter.CurrentHealth;
			Assert.That(verlustOffen, Is.EqualTo(ProbeHealthDamage).Within(1f),
				$"Offen muss der volle Schaden durchgehen; tatsächlich sanken die HP um {verlustOffen:0.0}.");
		}
	}
}
