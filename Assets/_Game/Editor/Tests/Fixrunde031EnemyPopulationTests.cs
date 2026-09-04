using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// F31-009: Die Gegnerbesetzung der Außenzonen folgt der Gefahrenstufe
	/// der Weltkarte (3 x Gefahr - 1), die Zusammensetzung staffelt sich mit
	/// der Gefahr, und ein Teil der Gegner bewacht die Weltkisten in einem
	/// Abstandsring.
	/// </summary>
	public sealed class Fixrunde031EnemyPopulationTests
	{
		private const string WildlingId = "enemy.wildling";

		[Test]
		public void Gegnerzahl_FolgtDerGefahrenstufe()
		{
			int geprueft = 0;
			foreach ((ZoneDefinition zone, WorldMapNodeDefinition node) in ZonesWithMapNode())
			{
				int soll = zone.IsSafeZone ? 0 : (3 * node.DangerLevel - 1);
				Assert.That(ZoneEnemyPopulationRules.TargetTotal(node.DangerLevel), Is.EqualTo(3 * node.DangerLevel - 1),
					"Die Regel TargetTotal weicht von der Staffel 3 x Gefahr - 1 ab.");
				int ist = AllocationTotal(zone);
				Assert.That(ist, Is.EqualTo(soll),
					$"Zone '{zone.Id}' (Gefahr {node.DangerLevel}) führt {ist} Gegner, Soll {soll}.");
				geprueft++;
			}
			Assert.That(geprueft, Is.GreaterThanOrEqualTo(8), "Zu wenige Zonen mit Weltkartenknoten gefunden.");
		}

		[Test]
		public void GefahrZwei_BleibtReinesWildlingGebiet()
		{
			foreach ((ZoneDefinition zone, WorldMapNodeDefinition node) in ZonesWithMapNode())
			{
				if (zone.IsSafeZone || node.DangerLevel != 2)
				{
					continue;
				}
				foreach (ZoneEnemyAllocation allocation in zone.EnemyAllocations)
				{
					if (allocation.Definition != null && allocation.Count > 0)
					{
						Assert.That(allocation.Definition.Id, Is.EqualTo(WildlingId),
							$"Zone '{zone.Id}' (Gefahr 2) führt '{allocation.Definition.Id}' — Gefahr 2 bleibt reines Wildling-Gebiet.");
					}
				}
			}
		}

		[Test]
		public void AbGefahrVier_UeberwiegenTierZweiGegner()
		{
			int geprueft = 0;
			foreach ((ZoneDefinition zone, WorldMapNodeDefinition node) in ZonesWithMapNode())
			{
				if (zone.IsSafeZone || node.DangerLevel < 4)
				{
					continue;
				}
				int wildlinge = 0;
				int tierZwei = 0;
				foreach (ZoneEnemyAllocation allocation in zone.EnemyAllocations)
				{
					if (allocation.Definition == null || allocation.Count <= 0 || allocation.Definition.IsCapturableEidra)
					{
						continue;
					}
					if (allocation.Definition.Id == WildlingId)
					{
						wildlinge += allocation.Count;
					}
					else
					{
						tierZwei += allocation.Count;
					}
				}
				Assert.That(tierZwei, Is.GreaterThan(wildlinge),
					$"Zone '{zone.Id}' (Gefahr {node.DangerLevel}): {tierZwei} Tier-2-Gegner gegen {wildlinge} Wildlinge — ab Gefahr 4 überwiegen die Gebietstiere.");
				geprueft++;
			}
			Assert.That(geprueft, Is.EqualTo(4), "Dämmerhain, Schleiermoor, Glutruinen und Grauklüfte erwartet.");
		}

		[Test]
		public void Kistenwachen_KandidatenLiegenImRing()
		{
			Assert.That(ZoneEnemyPopulationRules.ChestGuardMinDistance, Is.EqualTo(3.5f).Within(0.001f),
				"Untergrenze des Wachenrings: nicht auf der Kiste stehen und den Zugriff blockieren.");
			Assert.That(ZoneEnemyPopulationRules.ChestGuardMaxDistance, Is.EqualTo(7f).Within(0.001f),
				"Obergrenze des Wachenrings: unterhalb der Witterungsreichweite (10), damit die Wache den Zugriff bemerkt.");
			Vector3 kiste = new Vector3(11f, 0.12f, -9f);
			for (int saat = 0; saat < 500; saat++)
			{
				System.Random random = new System.Random(saat);
				for (int i = 0; i < 8; i++)
				{
					Vector3 kandidat = ZoneEnemyPopulationRules.ChestGuardCandidate(kiste, random);
					float abstand = new Vector2(kandidat.x - kiste.x, kandidat.z - kiste.z).magnitude;
					Assert.That(abstand, Is.InRange(3.5f, 7f),
						$"Saat {saat}, Kandidat {i}: Abstand {abstand:0.00} liegt außerhalb des Wachenrings.");
					Assert.That(kandidat.y, Is.EqualTo(kiste.y).Within(0.001f),
						"Wachenkandidaten bleiben auf der Kistenhöhe; die Bodenprojektion übernimmt die Platzierung.");
				}
			}
		}

		[Test]
		public void Kistenwachen_BudgetBindetHoechstensDieHaelfte()
		{
			Assert.That(ZoneEnemyPopulationRules.ChestGuardBudget(6, 14), Is.EqualTo(6),
				"Bei 14 Gegnern und 6 Kisten bekommt jede Kiste eine Wache.");
			Assert.That(ZoneEnemyPopulationRules.ChestGuardBudget(2, 5), Is.EqualTo(2),
				"Bei 5 Gegnern und 2 Kisten bekommt jede Kiste eine Wache.");
			Assert.That(ZoneEnemyPopulationRules.ChestGuardBudget(4, 2), Is.EqualTo(1),
				"Höchstens die Hälfte der Besetzung wird gebunden — der Rest bleibt frei im Gebiet.");
			Assert.That(ZoneEnemyPopulationRules.ChestGuardBudget(0, 14), Is.EqualTo(0),
				"Ohne Kisten gibt es keine Wachen.");
		}

		[Test]
		public void AlleZugeteiltenGegner_HabenEinPrefab()
		{
			// Der alte Szenen-Bake hat ein fehlendes Prefab still durch das
			// Wildling-Prefab ersetzt; der Laufzeit-Populator braucht die
			// Verdrahtung im Asset.
			foreach ((ZoneDefinition zone, WorldMapNodeDefinition _) in ZonesWithMapNode())
			{
				foreach (ZoneEnemyAllocation allocation in zone.EnemyAllocations)
				{
					if (allocation.Definition != null && allocation.Count > 0 && !allocation.Definition.IsCapturableEidra)
					{
						Assert.That(allocation.Definition.Prefab, Is.Not.Null,
							$"Gegner '{allocation.Definition.Id}' (Zone '{zone.Id}') hat kein Prefab — die Zone kann ihn zur Laufzeit nicht aufstellen.");
					}
				}
			}
		}

		private static int AllocationTotal(ZoneDefinition zone)
		{
			int summe = 0;
			foreach (ZoneEnemyAllocation allocation in zone.EnemyAllocations)
			{
				if (allocation.Definition != null)
				{
					summe += allocation.Count;
				}
			}
			return summe;
		}

		private static IEnumerable<(ZoneDefinition, WorldMapNodeDefinition)> ZonesWithMapNode()
		{
			Dictionary<string, WorldMapNodeDefinition> nodes = new Dictionary<string, WorldMapNodeDefinition>(StringComparer.Ordinal);
			foreach (string guid in AssetDatabase.FindAssets("t:WorldMapNodeDefinition", new[] { "Assets/_Game/Data/WorldMap" }))
			{
				WorldMapNodeDefinition node = AssetDatabase.LoadAssetAtPath<WorldMapNodeDefinition>(AssetDatabase.GUIDToAssetPath(guid));
				if (node != null && !string.IsNullOrWhiteSpace(node.Id))
				{
					nodes[node.Id] = node;
				}
			}
			foreach (string guid in AssetDatabase.FindAssets("t:ZoneDefinition", new[] { "Assets/_Game/Data/Zones" }))
			{
				ZoneDefinition zone = AssetDatabase.LoadAssetAtPath<ZoneDefinition>(AssetDatabase.GUIDToAssetPath(guid));
				if (zone != null && !string.IsNullOrWhiteSpace(zone.WorldMapNodeId) && nodes.TryGetValue(zone.WorldMapNodeId, out WorldMapNodeDefinition node))
				{
					yield return (zone, node);
				}
			}
		}
	}
}
