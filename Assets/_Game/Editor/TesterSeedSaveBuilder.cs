using Eidren.Core.Services;
using Eidren.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Erzeugt den Voll-Ausbau-Spielstand des Tester-Pakets: hoechste Stufe mit
	/// Maximallevel, alle freischaltbaren Technologien, alle Gebiete offen,
	/// komplette Eisenruestung, Eisenspeer und Eisendolche sowie Baumaterial.
	///
	/// Alle Kennungen stammen aus den echten Inhalten (Technologiebaum,
	/// Fortschrittskurve, Gegenstandskatalog) — nichts ist doppelt gepflegt.
	/// Dauerhaft stillgelegte Technologieknoten (Flag "retired.never") bleiben
	/// bewusst aus; sie sind im Spiel nicht erreichbar.
	///
	/// Der Spielstand landet als TextAsset in Resources und wird beim Start nur
	/// dann eingespielt, wenn noch kein eigener Spielstand existiert.
	/// Idempotent.
	/// </summary>
	public static class TesterSeedSaveBuilder
	{
		// Liegt bewusst NICHT unter Resources/: von dort wandert die Datei in
		// jeden Build und schaltet dem Spieler beim ersten Start alles frei.
		// Fuer ein Tester-Paket die erzeugte Datei nach
		// Assets/_Game/Resources/Data/ kopieren - der Ladepfad greift dann
		// wieder (EidrenServiceRoot, MainMenuController).
		private const string SeedPath = "Assets/_Game/Editor/TesterSeed/TesterSeedSave.json";
		private const string TreePath = "Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset";
		private const string CurvePath = "Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset";
		private const string ItemFolder = "Assets/_Game/Data/Items";
		private const string BuildingFolder = "Assets/_Game/Data/Buildings";
		private const string RetiredFlag = "retired.never";

		private const int InventorySlots = 16;

		private static readonly string[] WorldNodeIds =
		{
			"home_base", "zone_greenwood", "zone_quarry", "zone_marsh",
			"zone_ember_ruins", "zone_twilight_grove", "zone_veil_marsh", "zone_grey_rifts"
		};

		private static readonly string[] ProgressFlags = { "garon_defeated", "tier_2_unlocked" };

		private static readonly string[] SeedEidraIds = { "terrock", "noctarion", "ignivar" };

		[MenuItem("Eidren/Release/Tester-Spielstand (Voll-Ausbau) erzeugen")]
		public static void Build()
		{
			TechnologyTreeDefinition tree = Load<TechnologyTreeDefinition>(TreePath);
			ProgressionCurveDefinition curve = Load<ProgressionCurveDefinition>(CurvePath);

			int maxStage = tree.Nodes.Count > 0 ? tree.Nodes.Max(node => node.Stage) : 1;
			if (!curve.TryGetStage(maxStage, out ProgressionStageCurve stage))
			{
				throw new InvalidOperationException("Fortschrittskurve kennt Stufe " + maxStage + " nicht.");
			}

			TechnologyNodeDefinition[] unlockable = tree.Nodes
				.Where(node => !string.Equals(node.RequiredProgressFlag, RetiredFlag, StringComparison.Ordinal))
				.ToArray();
			int spentPoints = unlockable.Sum(node => node.PointCost);

			SaveGameData save = new SaveGameData
			{
				createdUtc = DateTime.UtcNow.ToString("O"),
				lastSavedUtc = DateTime.UtcNow.ToString("O"),
				buildVersion = "0.3.3-dev-midpoly",
				player = new SavePlayerData
				{
					activeWeaponId = "iron_spear",
					// Alle drei Eidra im Team; zwei davon aktiv (zweiter Platz
					// ist ueber technology.12 freigeschaltet).
					activeEidraId = "terrock",
					eidraRoster = SeedEidraIds
						.Select(id => new SaveEidraInstanceData { instanceId = "seed." + id, eidraId = id })
						.ToArray(),
					activeEidraInstanceIds = new[] { "seed.terrock", "seed.ignivar" },
					lastSafeNodeId = "home_base",
					lastSafeSceneKey = "HomeBase",
					equipment = new[]
					{
						Equip(EquipmentSlot.Weapon1, "iron_spear"),
						Equip(EquipmentSlot.Weapon2, "iron_daggers"),
						Equip(EquipmentSlot.Head, "armor_iron_helmet"),
						Equip(EquipmentSlot.Chest, "armor_iron_chest"),
						Equip(EquipmentSlot.Hands, "armor_iron_gloves"),
						Equip(EquipmentSlot.Legs, "armor_iron_legs")
					},
					inventory = BuildInventory()
				},
				world = new SaveWorldData
				{
					currentNodeId = "home_base",
					selectedNodeId = "home_base",
					visitedNodeIds = WorldNodeIds.ToArray(),
					progressFlags = ProgressFlags.ToArray()
				},
				progression = new SaveProgressionData
				{
					stage = maxStage,
					level = stage.MaximumLevel,
					experience = 0,
					availableTechnologyPoints = 5,
					spentTechnologyPoints = spentPoints,
					unlockedTechnologyNodeIds = unlockable.Select(node => node.Id).ToArray(),
					knownBlueprintIds = Array.Empty<string>()
				}
			};

			Directory.CreateDirectory(Path.GetDirectoryName(SeedPath));
			File.WriteAllText(SeedPath, JsonUtility.ToJson(save, prettyPrint: true));
			AssetDatabase.ImportAsset(SeedPath);
			AssetDatabase.SaveAssets();
			Debug.Log($"[Seed] Tester-Spielstand erzeugt: Stufe {maxStage}, Level {stage.MaximumLevel}, "
				+ $"{unlockable.Length} Technologien, {WorldNodeIds.Length} Gebiete → " + SeedPath);
		}

		/// <summary>
		/// Der Rucksack hat feste 16 Plaetze. Die Baukosten aller aktuellen
		/// Gebaeudedefinitionen werden einmal vollstaendig summiert und gemaess
		/// der echten Stapelgrenze aufgeteilt. Speer, Dolche und Ruestung liegen
		/// bereits in Ausruestungsslots und verbrauchen keinen Rucksackplatz.
		/// </summary>
		private static SaveItemStackData[] BuildInventory()
		{
			var entries = new List<SaveItemStackData>();
			var totals = new Dictionary<string, int>(StringComparer.Ordinal);
			BuildingCostDefinition[] buildings = AssetDatabase.FindAssets("t:BuildingCostDefinition", new[] { BuildingFolder })
				.Select(guid => AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
				.Where(building => building != null)
				.OrderBy(building => building.Id, StringComparer.Ordinal)
				.ToArray();
			if (buildings.Length == 0)
				throw new InvalidOperationException("Keine Gebaeudekosten fuer den Tester-Spielstand gefunden.");

			foreach (BuildingCostDefinition building in buildings)
			{
				foreach (CraftingIngredient ingredient in building.Cost)
				{
					totals.TryGetValue(ingredient.ItemId, out int current);
					totals[ingredient.ItemId] = checked(current + ingredient.Amount);
				}
			}

			string[] preferredOrder = { "wood", "stone", "plant_fiber", "copper_bar", "iron_bar" };
			foreach (KeyValuePair<string, int> total in totals
				.OrderBy(item => Array.IndexOf(preferredOrder, item.Key) < 0 ? int.MaxValue : Array.IndexOf(preferredOrder, item.Key))
				.ThenBy(item => item.Key, StringComparer.Ordinal))
			{
				ItemDefinition item = FindItem(total.Key);
				int remaining = total.Value;
				while (remaining > 0)
				{
					int quantity = Math.Min(remaining, Mathf.Max(1, item.MaximumStackSize));
					entries.Add(Stack(total.Key, quantity));
					remaining -= quantity;
				}
			}

			if (entries.Count > InventorySlots)
			{
				throw new InvalidOperationException($"Tester-Loadout benoetigt {entries.Count} von {InventorySlots} Rucksackplaetzen.");
			}
			SaveItemStackData[] slots = new SaveItemStackData[InventorySlots];
			for (int index = 0; index < slots.Length; index++)
			{
				slots[index] = (index < entries.Count)
					? entries[index]
					: new SaveItemStackData();
			}
			return slots;
		}

		private static SaveEquipmentEntry Equip(EquipmentSlot slot, string itemId)
		{
			return new SaveEquipmentEntry
			{
				slot = (int)slot,
				stack = Stack(itemId, 1)
			};
		}

		private static SaveItemStackData Stack(string itemId, int quantity)
		{
			ItemDefinition item = FindItem(itemId);
			int amount = Mathf.Clamp(quantity, 1, Mathf.Max(1, item.MaximumStackSize));
			return new SaveItemStackData
			{
				itemId = itemId,
				quantity = amount,
				// Haltbare Gegenstaende brauchen eine eigene Instanz-Kennung.
				instanceId = (item.MaximumDurability > 0) ? ("seed." + itemId) : string.Empty,
				durability = item.MaximumDurability
			};
		}

		private static ItemDefinition FindItem(string itemId)
		{
			foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition", new[] { ItemFolder }))
			{
				ItemDefinition candidate = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
				if (candidate != null && string.Equals(candidate.Id, itemId, StringComparison.Ordinal))
				{
					return candidate;
				}
			}
			throw new InvalidOperationException("Gegenstand fehlt im Katalog: " + itemId);
		}

		private static T Load<T>(string path) where T : UnityEngine.Object
		{
			T asset = AssetDatabase.LoadAssetAtPath<T>(path);
			if (asset == null)
			{
				throw new FileNotFoundException("Asset fehlt", path);
			}
			return asset;
		}
	}
}
