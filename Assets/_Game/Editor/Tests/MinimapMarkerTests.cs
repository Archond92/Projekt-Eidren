using Eidren.AI;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// N04-001: Die Spielobjekte melden sich selbst bei der Minimap an. Geprüft
	/// werden hier die gemeldeten Eigenschaften — die An- und Abmeldung im
	/// Lebenszyklus prüft der PlayMode-Test, weil EditMode kein OnEnable ausführt.
	/// </summary>
	public sealed class MinimapMarkerTests
	{
		private const string TreeAssetPath = "Assets/_Game/Data/Resources/Tree.asset";

		[Test]
		public void Ressourcenknoten_MeldetSichMitSeinemMaterialschluessel()
		{
			GameObject knoten = new GameObject("MinimapTest_Knoten");
			try
			{
				ResourceNodeDefinition baum = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(TreeAssetPath);
				Assert.That(baum, Is.Not.Null, "Baum-Definition fehlt: " + TreeAssetPath);
				ResourceNode node = knoten.AddComponent<ResourceNode>();
				node.Configure(baum, "minimaptest.baum", null, null, null, null);

				IMinimapMarker marker = node;

				Assert.That(marker.MinimapKind, Is.EqualTo(MinimapMarkerKind.Resource), "Ein Knoten gehört als Ressource auf die Karte");
				Assert.That(marker.MinimapPaletteId, Is.EqualTo("resource.tree"), "Die Einfärbung braucht den Materialschlüssel der Definition");
				Assert.That(marker.ShowsOnMinimap, Is.True, "Ein unberührter Knoten muss sichtbar sein");
			}
			finally
			{
				Object.DestroyImmediate(knoten);
			}
		}

		[Test]
		public void AbgebauterKnoten_VerschwindetVonDerKarte()
		{
			GameObject knoten = new GameObject("MinimapTest_Knoten");
			try
			{
				ResourceNodeDefinition baum = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(TreeAssetPath);
				ResourceNode node = knoten.AddComponent<ResourceNode>();
				node.Configure(baum, "minimaptest.baum", null, null, null, null);

				node.MarkAlreadyHarvested();

				IMinimapMarker marker = node;
				Assert.That(marker.ShowsOnMinimap, Is.False, "Ein abgebauter Knoten gehört gar nicht mehr auf die Karte (Nutzerentscheid 17.08.2026)");
			}
			finally
			{
				Object.DestroyImmediate(knoten);
			}
		}

		[Test]
		public void Knotenposition_IstDieWeltposition()
		{
			GameObject knoten = new GameObject("MinimapTest_Knoten");
			try
			{
				knoten.transform.position = new Vector3(4f, 1.5f, -9f);
				ResourceNode node = knoten.AddComponent<ResourceNode>();

				Assert.That(((IMinimapMarker)node).MinimapPosition, Is.EqualTo(new Vector3(4f, 1.5f, -9f)), "Der Marker muss dort sitzen, wo das Objekt steht");
			}
			finally
			{
				Object.DestroyImmediate(knoten);
			}
		}

		[Test]
		public void Wildling_MeldetSichAlsGegner()
		{
			GameObject gegner = new GameObject("MinimapTest_Gegner");
			try
			{
				WildlingController controller = gegner.AddComponent<WildlingController>();

				Assert.That(((IMinimapMarker)controller).MinimapKind, Is.EqualTo(MinimapMarkerKind.Enemy), "Ein Wildling ist ein gewöhnlicher Gegner");
			}
			finally
			{
				Object.DestroyImmediate(gegner);
			}
		}

		[Test]
		public void Boss_MeldetSichAlsBossUndNichtAlsGegner()
		{
			GameObject boss = new GameObject("MinimapTest_Boss");
			try
			{
				BossController controller = boss.AddComponent<BossController>();

				Assert.That(((IMinimapMarker)controller).MinimapKind, Is.EqualTo(MinimapMarkerKind.Boss), "Der Boss braucht sein eigenes Symbol");
			}
			finally
			{
				Object.DestroyImmediate(boss);
			}
		}

		[Test]
		public void Welttruhe_ErscheintErstNachdemSieGesehenWurde()
		{
			GameObject truhe = new GameObject("MinimapTest_Truhe");
			try
			{
				WorldChestContainer chest = truhe.AddComponent<WorldChestContainer>();
				IMinimapMarker marker = chest;

				Assert.That(marker.MinimapKind, Is.EqualTo(MinimapMarkerKind.Chest), "Kisten haben ein eigenes Symbol");
				Assert.That(marker.ShowsOnMinimap, Is.False, "Eine ungesehene Kiste darf die Karte nicht verraten");

				((IMinimapDiscoverable)chest).MarkSeenOnMinimap();

				Assert.That(marker.ShowsOnMinimap, Is.True, "Einmal gesehen, bleibt die Kiste auf der Karte");
			}
			finally
			{
				Object.DestroyImmediate(truhe);
			}
		}

		[Test]
		public void GeseheneTruhe_BleibtAuchNachWeiterlaufenSichtbar()
		{
			GameObject truhe = new GameObject("MinimapTest_Truhe");
			try
			{
				WorldChestContainer chest = truhe.AddComponent<WorldChestContainer>();
				((IMinimapDiscoverable)chest).MarkSeenOnMinimap();

				// Zweiter Aufruf darf den Zustand nicht zurücksetzen; der Marker ist
				// eine Einbahnstraße.
				((IMinimapDiscoverable)chest).MarkSeenOnMinimap();

				Assert.That(((IMinimapMarker)chest).ShowsOnMinimap, Is.True, "Die Entdeckung darf nicht wieder verfallen");
			}
			finally
			{
				Object.DestroyImmediate(truhe);
			}
		}

		[Test]
		public void GeoeffneteWelttruhe_VerschwindetVonDerKarte()
		{
			GameObject truhe = new GameObject("MinimapTest_Truhe");
			try
			{
				WorldChestContainer chest = truhe.AddComponent<WorldChestContainer>();
				ContentDatabase database = truhe.AddComponent<ContentDatabase>();
				GameSession session = truhe.AddComponent<GameSession>();
				chest.Initialize(database, session, new WorldChestState("minimap-truhe", "minimap-spawn", WorldChestFamily.Common, new ItemStack[24], opened: true));
				((IMinimapDiscoverable)chest).MarkSeenOnMinimap();

				Assert.That(((IMinimapMarker)chest).ShowsOnMinimap, Is.False, "Eine bereits geoeffnete Truhe gehoert nicht mehr auf die Karte (Nutzerentscheid 17.08.2026)");
			}
			finally
			{
				Object.DestroyImmediate(truhe);
			}
		}

		[Test]
		public void LeergeraeumterBeutesack_VerschwindetVonDerKarte()
		{
			GameObject sack = new GameObject("MinimapTest_Beutesack");
			try
			{
				EnemyLootContainer loot = sack.AddComponent<EnemyLootContainer>();
				ContentDatabase database = sack.AddComponent<ContentDatabase>();
				loot.Initialize(database, "minimap-beute", null, 2.2f);
				((IMinimapDiscoverable)loot).MarkSeenOnMinimap();

				Assert.That(((IMinimapMarker)loot).ShowsOnMinimap, Is.False, "Ein durchsuchter Beutesack gehoert nicht mehr auf die Karte");
			}
			finally
			{
				Object.DestroyImmediate(sack);
			}
		}

		[Test]
		public void EigeneLagerkiste_BleibtAuchLeerSichtbar()
		{
			GameObject kiste = new GameObject("MinimapTest_Lagerkiste");
			try
			{
				// Bewusste Ausnahme: Die eigene Lagerkiste ist kein Fund, sondern
				// Einrichtung. Sie verschwindet nicht, nur weil sie leer ist.
				StorageContainer storage = kiste.AddComponent<StorageContainer>();
				((IMinimapDiscoverable)storage).MarkSeenOnMinimap();

				Assert.That(((IMinimapMarker)storage).ShowsOnMinimap, Is.True, "Die eigene Lagerkiste bleibt auf der Karte, damit man sie wiederfindet");
			}
			finally
			{
				Object.DestroyImmediate(kiste);
			}
		}
	}
}
