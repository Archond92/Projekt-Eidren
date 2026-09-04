using System.Reflection;
using Eidren.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class MeshActorPresentationTests
	{
		[TestCase("idle", "Dolche", "Ruhe_Dolche")]
		[TestCase("idle", "Ohne", "Ruhe_Ohne")]
		[TestCase("move", "Ohne", "Laufen_Ohne")]
		[TestCase("move", "Speer", "Laufen_Speer")]
		[TestCase("hammer1", "Hammer", "Angriff_Hammer")]
		[TestCase("hammer3", "Hammer", "Angriff_Hammer")]
		[TestCase("dagger4", "Dolche", "Angriff_Dolche")]
		[TestCase("spear2", "Speer", "Angriff_Speer")]
		// W-001: Abbau-Clips existieren nur fuer die Werkzeug-Traeger; die
		// Traeger-Varianten von Ruhe/Laufen gelten auch fuer Werkzeuge.
		[TestCase("harvest", "Axt", "Abbau_Axt")]
		[TestCase("harvest", "Spitzhacke", "Abbau_Spitzhacke")]
		[TestCase("harvest", "Sense", "Abbau_Sense")]
		[TestCase("idle", "Axt", "Ruhe_Axt")]
		[TestCase("move", "Spitzhacke", "Laufen_Spitzhacke")]
		// W-009: Der Oeffnen-Clip ist traegerunabhaengig (leere Haende).
		[TestCase("open", "Dolche", "Oeffnen")]
		[TestCase("open", "Speer", "Oeffnen")]
		[TestCase("open", "Axt", "Oeffnen")]
		public void ResolveClipName_LiefertClipFuerBekannteStems(string stem, string stance, string expected)
		{
			Assert.That(MeshActorPresentation.ResolveClipName(stem, stance), Is.EqualTo(expected));
		}

		[TestCase("hit")]
		[TestCase("death")]
		[TestCase("dodge")]
		[TestCase("harvest")]
		[TestCase("appear")]
		[TestCase("")]
		public void ResolveClipName_LiefertNullFuerProzeduraleStems(string stem)
		{
			Assert.That(MeshActorPresentation.ResolveClipName(stem, MeshActorPresentation.StanceDaggers), Is.Null);
		}

		[TestCase(0, 1, "Helm_Stoff")]
		[TestCase(1, 2, "Harnisch_Kupfer")]
		[TestCase(2, 3, "Haende_Eisen")]
		[TestCase(3, 1, "Beine_Stoff")]
		[TestCase(0, 0, null)]
		[TestCase(2, 4, null)]
		public void ArmorMeshName_BildetSlotUndStufeAufKnotennamenAb(int slot, int tier, string expected)
		{
			Assert.That(MeshActorPresentation.ArmorMeshName(slot, tier), Is.EqualTo(expected));
		}

		[Test]
		public void Kopfslot_VerbirgtHaareUnterKapuzeUndZeigtSieOhneHelm()
		{
			GameObject actor = new GameObject("MeshActorPresentationHairActor");
			try
			{
				foreach (string nodeName in new[] { "Haare", "Helm_Stoff", "Helm_Kupfer", "Helm_Eisen" })
				{
					new GameObject(nodeName).transform.SetParent(actor.transform, worldPositionStays: false);
				}
				MeshActorPresentation presentation = actor.AddComponent<MeshActorPresentation>();
				presentation.Configure(null, actor.transform);

				presentation.SetArmorTiers(1, 0, 0, 0);
				Assert.That(actor.transform.Find("Haare").gameObject.activeSelf, Is.False);
				Assert.That(actor.transform.Find("Helm_Stoff").gameObject.activeSelf, Is.True);

				presentation.SetArmorTiers(0, 0, 0, 0);
				Assert.That(actor.transform.Find("Haare").gameObject.activeSelf, Is.True);
				Assert.That(actor.transform.Find("Helm_Stoff").gameObject.activeSelf, Is.False);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(actor);
			}
		}

		[Test]
		public void FacingToYaw_NordIstNullGradUndSuedIst180()
		{
			Assert.That(MeshActorPresentation.FacingToYaw(ActorFacing8.N), Is.EqualTo(0f));
			Assert.That(MeshActorPresentation.FacingToYaw(ActorFacing8.E), Is.EqualTo(90f));
			Assert.That(MeshActorPresentation.FacingToYaw(ActorFacing8.S), Is.EqualTo(180f));
			Assert.That(MeshActorPresentation.FacingToYaw(ActorFacing8.NW), Is.EqualTo(315f));
		}

		[Test]
		public void SetVisualState_AppearNachTodHebtDieTodesSperreAuf()
		{
			// Regression: StartProcedural blockte jeden neuen Zustand dauerhaft, sobald
			// einmal Death gesetzt war (Revive/Gegner-Pooling wuerden die Figur einfrieren).
			// Appear muss die Sperre wieder aufheben. Der interne Zustand (_procedural) ist
			// bewusst private - er wird hier per Reflection gelesen statt die Produktionsklasse
			// mit einer internal-Abfrage nur fuers Testen aufzuweiten (einfachste beobachtbare
			// Assertion ohne neue oeffentliche/interne API-Flaeche).
			GameObject actor = new GameObject("MeshActorPresentationTestActor");
			try
			{
				MeshActorPresentation presentation = actor.AddComponent<MeshActorPresentation>();
				FieldInfo proceduralField = typeof(MeshActorPresentation).GetField("_procedural", BindingFlags.NonPublic | BindingFlags.Instance);

				presentation.SetVisualState(ActorVisualState.Death);
				Assert.That(proceduralField.GetValue(presentation).ToString(), Is.EqualTo("Death"));

				presentation.SetVisualState(ActorVisualState.Appear);
				Assert.That(proceduralField.GetValue(presentation).ToString(), Is.EqualTo("Appear"));
				Assert.That(presentation.VisualState, Is.EqualTo(ActorVisualState.Appear));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(actor);
			}
		}

		// W-001: Ohne Werkzeug (blosse Haende oder unbekanntes Item) gibt es
		// keinen Abbau-Clip — der prozedurale Rueckfall bleibt zustaendig.
		[TestCase("harvest", "Dolche")]
		[TestCase("harvest", "Speer")]
		[TestCase("harvest", "Hammer")]
		public void ResolveClipName_HarvestOhneWerkzeugTraegerBleibtProzedural(string stem, string stance)
		{
			Assert.That(MeshActorPresentation.ResolveClipName(stem, stance), Is.Null);
		}

		[TestCase("axe", MeshActorPresentation.StanceAxe)]
		[TestCase("pickaxe", MeshActorPresentation.StancePickaxe)]
		[TestCase("scythe", MeshActorPresentation.StanceScythe)]
		// W-008: auch die Stufen-Werkzeuge muessen ihren Traeger finden —
		// Achtung, "…pickaxe" endet ebenfalls auf "…axe".
		[TestCase("copper_axe", MeshActorPresentation.StanceAxe)]
		[TestCase("iron_axe", MeshActorPresentation.StanceAxe)]
		[TestCase("copper_pickaxe", MeshActorPresentation.StancePickaxe)]
		[TestCase("iron_pickaxe", MeshActorPresentation.StancePickaxe)]
		[TestCase("copper_scythe", MeshActorPresentation.StanceScythe)]
		[TestCase("iron_scythe", MeshActorPresentation.StanceScythe)]
		[TestCase("sword", null)]
		[TestCase("", null)]
		[TestCase(null, null)]
		public void ToolStanceForItem_BildetWerkzeugItemsAufTraegerAb(string itemId, string expected)
		{
			Assert.That(MeshActorPresentation.ToolStanceForItem(itemId), Is.EqualTo(expected));
		}

		[TestCase("hammer", "Waffe_Hammer_Base")]
		[TestCase("copper_hammer", "Waffe_Hammer_Kupfer")]
		[TestCase("iron_hammer", "Waffe_Hammer_Eisen")]
		[TestCase("sealbreaker", "Waffe_Hammer_Sealbreaker")]
		[TestCase("daggers", "Waffe_Dolche_Base")]
		[TestCase("copper_daggers", "Waffe_Dolche_Kupfer")]
		[TestCase("iron_daggers", "Waffe_Dolche_Eisen")]
		[TestCase("ash_fangs", "Waffe_Dolche_AshFangs")]
		[TestCase("copper_spear", "Waffe_Speer_Kupfer")]
		[TestCase("iron_spear", "Waffe_Speer_Eisen")]
		[TestCase("ember_thorn", "Waffe_Speer_EmberThorn")]
		[TestCase("axe", "Waffe_Axt_Base")]
		[TestCase("copper_axe", "Waffe_Axt_Kupfer")]
		[TestCase("iron_axe", "Waffe_Axt_Eisen")]
		[TestCase("pickaxe", "Waffe_Spitzhacke_Base")]
		[TestCase("copper_pickaxe", "Waffe_Spitzhacke_Kupfer")]
		[TestCase("iron_pickaxe", "Waffe_Spitzhacke_Eisen")]
		[TestCase("scythe", "Waffe_Sense_Base")]
		[TestCase("copper_scythe", "Waffe_Sense_Kupfer")]
		[TestCase("iron_scythe", "Waffe_Sense_Eisen")]
		[TestCase("unknown", null)]
		public void ItemVisualModule_BildetJedesPhase2ItemAufSeinMeshAb(string itemId, string expected)
		{
			Assert.That(MeshActorPresentation.ItemVisualModule(itemId), Is.EqualTo(expected));
		}

		[Test]
		public void SetHarvestTool_ZeigtWerkzeugMeshUndClearStelltDieWaffeWieder()
		{
			// W-001: Waehrend des Abbaus ersetzt das Werkzeug-Mesh die Waffe;
			// nach dem Abbau kehrt die ausgeruestete Waffe zurueck.
			GameObject actor = new GameObject("MeshActorPresentationHarvestActor");
			try
			{
				string[] nodes = { "Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer", "Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense" };
				foreach (string nodeName in nodes)
				{
					new GameObject(nodeName).transform.SetParent(actor.transform, worldPositionStays: false);
				}
				MeshActorPresentation presentation = actor.AddComponent<MeshActorPresentation>();
				presentation.Configure(null, actor.transform);
				presentation.SetWeaponStance(MeshActorPresentation.StanceSpear, showWeaponMesh: true);

				presentation.SetHarvestTool(MeshActorPresentation.StancePickaxe);
				Assert.That(actor.transform.Find("Waffe_Spitzhacke").gameObject.activeSelf, Is.True, "Werkzeug-Mesh muss waehrend des Abbaus sichtbar sein");
				Assert.That(actor.transform.Find("Waffe_Speer").gameObject.activeSelf, Is.False, "Waffe muss waehrend des Abbaus ausgeblendet sein");

				presentation.ClearHarvestTool();
				Assert.That(actor.transform.Find("Waffe_Spitzhacke").gameObject.activeSelf, Is.False, "Werkzeug-Mesh muss nach dem Abbau wieder weg sein");
				Assert.That(actor.transform.Find("Waffe_Speer").gameObject.activeSelf, Is.True, "Waffe muss nach dem Abbau wieder sichtbar sein");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(actor);
			}
		}

		[Test]
		public void PlayTimed_DeckeltDieAbspielgeschwindigkeitAufAnderthalb()
		{
			// F34-007: Ein 1,2-s-Clip in einem 0,48-s-Kampffenster lief frueher mit
			// 2,5-facher Geschwindigkeit; der Deckel haelt ihn bei 1,5.
			GameObject actor = new GameObject("MeshActorPresentationSpeedActor");
			try
			{
				Animation animation = actor.AddComponent<Animation>();
				AnimationClip clip = new AnimationClip { legacy = true };
				clip.SetCurve(string.Empty, typeof(Transform), "localPosition.y", AnimationCurve.Linear(0f, 0f, 1.2f, 1f));
				animation.AddClip(clip, "Angriff_Speer");
				MeshActorPresentation presentation = actor.AddComponent<MeshActorPresentation>();
				presentation.Configure(animation, actor.transform);
				presentation.SetWeaponStance(MeshActorPresentation.StanceSpear, showWeaponMesh: false);

				presentation.SetAuthoredTimedState("spear1", 0.48f);
				Assert.That(animation["Angriff_Speer"].speed, Is.EqualTo(MeshActorPresentation.MaxTimedPlaybackSpeed).Within(0.001f), "Zu kurzes Fenster wird gedeckelt");

				presentation.SetAuthoredTimedState("spear1", 1.0f);
				Assert.That(animation["Angriff_Speer"].speed, Is.EqualTo(1.2f).Within(0.001f), "Passendes Fenster laeuft ungedeckelt");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(actor);
			}
		}

		[Test]
		public void SetBareHands_VerbirgtDieWaffeUndClearStelltSieWieder()
		{
			// W-009: Waehrend der Kistenoeffnung sind die Haende leer; danach
			// kehrt die ausgeruestete Waffe zurueck.
			GameObject actor = new GameObject("MeshActorPresentationBareHandsActor");
			try
			{
				foreach (string nodeName in new[] { "Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer", "Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense" })
				{
					new GameObject(nodeName).transform.SetParent(actor.transform, worldPositionStays: false);
				}
				MeshActorPresentation presentation = actor.AddComponent<MeshActorPresentation>();
				presentation.Configure(null, actor.transform);
				presentation.SetWeaponStance(MeshActorPresentation.StanceSpear, showWeaponMesh: true);

				presentation.SetBareHands();
				Assert.That(actor.transform.Find("Waffe_Speer").gameObject.activeSelf, Is.False, "Waffe muss bei leeren Haenden verborgen sein");

				presentation.ClearHarvestTool();
				Assert.That(actor.transform.Find("Waffe_Speer").gameObject.activeSelf, Is.True, "Waffe muss nach der Oeffnung zurueckkehren");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(actor);
			}
		}

		[Test]
		public void SetWeaponStance_DeaktiviertAuchDieWerkzeugMeshes()
		{
			// W-003: Die Werkzeuge (Waffe_Axt/_Spitzhacke/_Sense) haengen seit dem
			// GLB-Import aktiv am Rig. SetWeaponStance muss sie wie die uebrigen
			// Waffenmeshes ausblenden, sonst traegt die Figur dauerhaft die Sense.
			GameObject actor = new GameObject("MeshActorPresentationToolActor");
			try
			{
				string[] nodes = { "Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer", "Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense" };
				foreach (string nodeName in nodes)
				{
					new GameObject(nodeName).transform.SetParent(actor.transform, worldPositionStays: false);
				}
				MeshActorPresentation presentation = actor.AddComponent<MeshActorPresentation>();
				presentation.Configure(null, actor.transform);

				presentation.SetWeaponStance(MeshActorPresentation.StanceSpear, showWeaponMesh: true);

				Assert.That(actor.transform.Find("Waffe_Speer").gameObject.activeSelf, Is.True, "Waffe_Speer muss sichtbar sein");
				foreach (string nodeName in new[] { "Waffe_Dolche", "Waffe_Hammer", "Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense" })
				{
					Assert.That(actor.transform.Find(nodeName).gameObject.activeSelf, Is.False, nodeName + " muss inaktiv sein");
				}
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(actor);
			}
		}

		[TestCase(Eidren.Data.WeaponFamily.Hammer, MeshActorPresentation.StanceHammer, true)]
		[TestCase(Eidren.Data.WeaponFamily.Daggers, MeshActorPresentation.StanceDaggers, true)]
		[TestCase(Eidren.Data.WeaponFamily.Spear, MeshActorPresentation.StanceSpear, true)]
		[TestCase(Eidren.Data.WeaponFamily.None, MeshActorPresentation.StanceBare, false)]
		public void ResolveStance_BildetWaffenfamilieAufStanceAb(Eidren.Data.WeaponFamily family, string expectedStance, bool expectedVisible)
		{
			var (stance, visible) = Eidren.Combat.WandererEquipmentVisual.ResolveStance(family);
			Assert.That(stance, Is.EqualTo(expectedStance));
			Assert.That(visible, Is.EqualTo(expectedVisible));
		}
	}
}
