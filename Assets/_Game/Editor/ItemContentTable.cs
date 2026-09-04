using Eidren.Data;

namespace Eidren.Editor
{
internal static class ItemContentTable
{
	private const string ItemArt = "Assets/_Game/Art/Items";

	private const string V02IconSource = "Assets/_Game/Art/Items/ITEM_TMP_";

	private const string PotionIcon = "Assets/_Game/Art/Items/ITEM_TMP_HealingPotion.png";

	private const string HammerIcon = "Assets/_Game/Art/Items/ITEM_TMP_Hammer.png";

	private const string DaggersIcon = "Assets/_Game/Art/Items/ITEM_TMP_Daggers.png";

	private const string PlayerHoodArt = "Assets/_Game/Art/Items/ITEM_WandererHood.png";

	private const string PlayerCoatArt = "Assets/_Game/Art/Items/ITEM_WandererCoat.png";

	private const string PlayerBracersArt = "Assets/_Game/Art/Items/ITEM_WandererBracers.png";

	private const string PlayerLegsArt = "Assets/_Game/Art/Items/ITEM_WandererLegs.png";

	internal static ItemDefinition[] EnsureAll()
	{
		return new ItemDefinition[58]
		{
			ItemContentAssetBuilder.EnsureItem("Wood", "wood", "Holz", "Robustes Bau- und Herstellungsmaterial.", ItemCategory.Resource, "wood", 0, "Assets/_Game/Art/Items/ITEM_TMP_Wood.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: false, new string[2] { "resource", "wood" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("Stone", "stone", "Stein", "Gewöhnliches mineralisches Baumaterial.", ItemCategory.Resource, "stone", 0, "Assets/_Game/Art/Items/ITEM_TMP_Stone.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: false, new string[2] { "resource", "stone" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("PlantFiber", "plant_fiber", "Pflanzenfaser", "Flexible Fasern aus regionalen Pflanzen.", ItemCategory.Resource, "fiber", 0, "Assets/_Game/Art/Items/ITEM_TMP_PlantFiber.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: false, new string[2] { "resource", "fiber" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("CopperOre", "copper_ore", "Kupfererz", "Kupferhaltiges Erz zur späteren Verhüttung.", ItemCategory.Resource, "ore", 1, "Assets/_Game/Art/Items/ITEM_TMP_CopperOre.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: false, new string[3] { "resource", "ore", "copper" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("CopperBar", "copper_bar", "Kupferbarren", "Verarbeiteter Kupferbarren für spätere Rezepte.", ItemCategory.Material, "ore", 1, "Assets/_Game/Art/Items/ITEM_TMP_CopperBar.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "material", "metal", "copper" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("HealingPotion", "healing_potion", "Heiltrank", "Stellt bei Verwendung 55 Lebenspunkte wieder her.", ItemCategory.Consumable, string.Empty, 1, "Assets/_Game/Art/Items/ITEM_TMP_HealingPotion.png", 10, canBeUsed: true, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[2] { "consumable", "healing" }, ItemUseActionType.Heal, 55f),
			ItemContentAssetBuilder.EnsureItem("BuffFood", "buff_food", "Brot", "Erhöht Schaden und Stagger-Schaden für 2 Minuten.", ItemCategory.Consumable, string.Empty, 1, "Assets/_Game/Art/Items/ITEM_TMP_BuffFood.png", 10, canBeUsed: true, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "consumable", "food", "combat_buff" }, ItemUseActionType.TimedCombatBuff, 0f, 120f, 1.2f, 1.15f),
			ItemContentAssetBuilder.EnsureItem("Berry", "berry", "Beeren", "Roh essbar. Heilt sofort, aber deutlich schwächer als ein Heiltrank.", ItemCategory.Resource, string.Empty, 0, "Assets/_Game/Art/Items/ITEM_TMP_Berry.png", 10, canBeUsed: true, canBeDropped: true, canBeStored: true, canBeCrafted: false, new string[3] { "resource", "food", "healing" }, ItemUseActionType.Heal, 15f),
			ItemContentAssetBuilder.EnsureItem("WheatSeed", "wheat_seed", "Weizensamen", "Nebenprodukt der Faserknoten. Wird beim Einsetzen verbraucht.", ItemCategory.Resource, string.Empty, 0, "Assets/_Game/Art/Items/ITEM_TMP_WheatSeed.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: false, new string[2] { "resource", "seed" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("Axe", "axe", "Axt", "Erhöht den Ertrag beim Holzfällen.", ItemCategory.Tool, string.Empty, 0, "Assets/_Game/Art/Items/ITEM_TMP_Axe.png", 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[2] { "tool", "wood" }, ItemUseActionType.None, 0f, 0f, 0f, 0f, 100),
			ItemContentAssetBuilder.EnsureItem("Scythe", "scythe", "Sense", "Erhöht den Ertrag beim Schneiden von Fasern.", ItemCategory.Tool, string.Empty, 0, "Assets/_Game/Art/Items/ITEM_TMP_Scythe.png", 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[2] { "tool", "fiber" }, ItemUseActionType.None, 0f, 0f, 0f, 0f, 100),
			ItemContentAssetBuilder.EnsureItem("Pickaxe", "pickaxe", "Spitzhacke", "Erhöht den Ertrag an Stein und ist die einzige Möglichkeit, Kupfererz abzubauen.", ItemCategory.Tool, string.Empty, 0, "Assets/_Game/Art/Items/ITEM_TMP_Pickaxe.png", 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "tool", "stone", "ore" }, ItemUseActionType.None, 0f, 0f, 0f, 0f, 100),
			ItemContentAssetBuilder.EnsureItem("Hammer", "hammer", "Hammer", "Schwere Nahkampfwaffe mit einer Dreierkombo und hohem Stagger-Schaden.", ItemCategory.Weapon, string.Empty, 0, "Assets/_Game/Art/Items/ITEM_TMP_Hammer.png", 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "weapon", "hammer", "stagger" }, ItemUseActionType.None, 0f, 0f, 0f, 0f, 120),
			ItemContentAssetBuilder.EnsureItem("Daggers", "daggers", "Dolche", "Schnelle Nahkampfwaffen mit einer Viererkombo und hohem Rückenschaden.", ItemCategory.Weapon, string.Empty, 0, "Assets/_Game/Art/Items/ITEM_TMP_Daggers.png", 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "weapon", "daggers", "back_attack" }, ItemUseActionType.None, 0f, 0f, 0f, 0f, 220),
			ItemContentAssetBuilder.EnsureItem("Plank", "plank", "Brett", "Zugeschnittenes Holz aus dem Sägewerk.", ItemCategory.Material, "wood", 1, "Assets/_Game/Art/Items/ITEM_TMP_Plank.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[2] { "material", "wood" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("Rope", "rope", "Seil", "Gedrehte Fasern aus der Seilerei. Die Bindung aller T1-Bauteile.", ItemCategory.Material, "fiber", 1, "Assets/_Game/Art/Items/ITEM_TMP_Rope.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[2] { "material", "fiber" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("StoneBlock", "stone_block", "Steinblock", "Behauener Stein vom Steinmetz.", ItemCategory.Material, "stone", 1, "Assets/_Game/Art/Items/ITEM_TMP_StoneBlock.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[2] { "material", "stone" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("Wheat", "wheat", "Weizen", "Ernte des Ackers. Wird am Kochtopf zu Brot.", ItemCategory.Resource, string.Empty, 1, "Assets/_Game/Art/Items/ITEM_TMP_Wheat.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: false, new string[2] { "resource", "food" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("CatchDevice", "catch_device", "Fanggerät", "Permanentes Gerät zum Fangen von Eidra. Die eingesetzte Batterie bestimmt die Ladung.", ItemCategory.Tool, string.Empty, 1, "Assets/_Game/Art/Items/ITEM_TMP_CatchDevice.png", 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "tool", "eidra", "catch" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("Battery", "battery", "Batterie", "Kupferspule für das Fanggerät. Wird nur bei einem erfolgreichen Fang verbraucht.", ItemCategory.Consumable, string.Empty, 1, "Assets/_Game/Art/Items/ITEM_TMP_Battery.png", 10, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "consumable", "eidra", "catch" }, ItemUseActionType.None),
			ItemContentAssetBuilder.EnsureItem("WandererHood", "armor_wanderer_hood", "Stoffkapuze", "Leichte Kapuze aus Seil und Pflanzenfaser.", ItemCategory.Armor, string.Empty, 0, PlayerHoodArt, 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "armor", "head", "wanderer" }, ItemUseActionType.None, 0f, 0f, 0f, 0f, 80, WearableSlot.Head, 0.02f),
			ItemContentAssetBuilder.EnsureItem("WandererCoat", "armor_wanderer_coat", "Stoffmantel", "Robuster Mantel aus dicht gebundener Pflanzenfaser.", ItemCategory.Armor, string.Empty, 0, PlayerCoatArt, 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "armor", "chest", "wanderer" }, ItemUseActionType.None, 0f, 0f, 0f, 0f, 80, WearableSlot.Chest, 0.04f),
			ItemContentAssetBuilder.EnsureItem("WandererBracers", "armor_wanderer_bracers", "Stoffarmschienen", "Gebundene Armschienen für Hände und Unterarme.", ItemCategory.Armor, string.Empty, 0, PlayerBracersArt, 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "armor", "hands", "wanderer" }, ItemUseActionType.None, 0f, 0f, 0f, 0f, 80, WearableSlot.Hands, 0.01f),
			ItemContentAssetBuilder.EnsureItem("WandererLegs", "armor_wanderer_legs", "Stoffschuhe", "Leichte, trittfeste Schuhe aus Faser und Seil.", ItemCategory.Armor, string.Empty, 0, PlayerLegsArt, 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[3] { "armor", "legs", "wanderer" }, ItemUseActionType.None, 0f, 0f, 0f, 0f, 80, WearableSlot.Legs, 0.03f),
			Equipment("CopperHammer", "copper_hammer", "Kupferhammer", ItemCategory.Weapon, 1, "Assets/_Game/Art/Items/ITEM_TMP_Hammer.png", 150, "weapon", "hammer"),
			Equipment("IronHammer", "iron_hammer", "Eisenhammer", ItemCategory.Weapon, 2, "Assets/_Game/Art/Items/ITEM_TMP_Hammer.png", 180, "weapon", "hammer"),
			Equipment("Sealbreaker", "sealbreaker", "Siegelbrecher", ItemCategory.Weapon, 1, "Assets/_Game/Art/Items/ITEM_TMP_Hammer.png", 200, false, "weapon", "hammer", "named"),
			Equipment("CopperDaggers", "copper_daggers", "Kupferdolche", ItemCategory.Weapon, 1, "Assets/_Game/Art/Items/ITEM_TMP_Daggers.png", 275, "weapon", "daggers"),
			Equipment("IronDaggers", "iron_daggers", "Eisendolche", ItemCategory.Weapon, 2, "Assets/_Game/Art/Items/ITEM_TMP_Daggers.png", 330, "weapon", "daggers"),
			Equipment("AshFangs", "ash_fangs", "Aschenzähne", ItemCategory.Weapon, 1, "Assets/_Game/Art/Items/ITEM_TMP_Daggers.png", 365, false, "weapon", "daggers", "named"),
			Equipment("CopperSpear", "copper_spear", "Kupferspeer", ItemCategory.Weapon, 1, "Assets/_Game/Art/Items/ITEM_TMP_Pickaxe.png", 190, "weapon", "spear"),
			Equipment("IronSpear", "iron_spear", "Eisenspeer", ItemCategory.Weapon, 2, "Assets/_Game/Art/Items/ITEM_TMP_Pickaxe.png", 230, "weapon", "spear"),
			Equipment("EmberThorn", "ember_thorn", "Glutdorn", ItemCategory.Weapon, 1, "Assets/_Game/Art/Items/ITEM_TMP_Pickaxe.png", 255, false, "weapon", "spear", "named"),
			Material("Hardwood", "hardwood", "Hartholz", ItemCategory.Resource, "wood", 2, "Assets/_Game/Art/Items/ITEM_TMP_Wood.png", "resource", "wood"),
			Material("HardwoodPlank", "hardwood_plank", "Hartholzbrett", ItemCategory.Material, "wood", 2, "Assets/_Game/Art/Items/ITEM_TMP_Plank.png", "material", "wood"),
			// Eigene Quelle statt Spiegel auf Stone/StoneBlock: der Spiegel-
			// mechanismus (EnsureMirroredSprite) zog jede Familien-Toenung der
			// Basis-Icons sofort in die Ableitung nach und hielt die Duplikate
			// damit am Leben. Die Zieldateien existieren; source==destination
			// nimmt den Kurzschluss ohne Kopieren.
			Material("Granite", "granite", "Granit", ItemCategory.Resource, "stone", 2, "Assets/_Game/Art/Items/ITEM_TMP_Granite.png", "resource", "stone"),
			Material("CutGranite", "cut_granite", "Behauener Granit", ItemCategory.Material, "stone", 2, "Assets/_Game/Art/Items/ITEM_TMP_CutGranite.png", "material", "stone"),
			Material("SwampHemp", "swamp_hemp", "Sumpfhanf", ItemCategory.Resource, "fiber", 2, "Assets/_Game/Art/Items/ITEM_TMP_PlantFiber.png", "resource", "fiber"),
			Material("RobustCloth", "robust_cloth", "Robustes Tuch", ItemCategory.Material, "fiber", 2, "Assets/_Game/Art/Items/ITEM_TMP_Rope.png", "material", "fiber"),
			Material("IronOre", "iron_ore", "Eisenerz", ItemCategory.Resource, "ore", 2, "Assets/_Game/Art/Items/ITEM_TMP_CopperOre.png", "resource", "ore"),
			Material("IronBar", "iron_bar", "Eisenbarren", ItemCategory.Material, "ore", 2, "Assets/_Game/Art/Items/ITEM_TMP_CopperBar.png", "material", "metal"),
			Material("SmithingFitting", "smithing_fitting", "Schmiedebeschlag", ItemCategory.Material, "ore", 2, "Assets/_Game/Art/Items/ITEM_TMP_CopperBar.png", "material", "rare"),
			Material("SmithingMark", "smithing_mark", "Schmiedemarke", ItemCategory.Material, "ore", 1, "Assets/_Game/Art/Items/ITEM_SmithingMark.png", "currency", "forge"),
			// Eigene Quelle statt Spiegel auf die Pickaxe (siehe Granit-Kommentar).
			Material("CopperSpearBlueprint", "blueprint_item_copper_spear", "Bauplan: Kupferspeer", ItemCategory.Material, "ore", 1, "Assets/_Game/Art/Items/ITEM_TMP_CopperSpearBlueprint.png", "blueprint", "spear"),
			Equipment("CopperAxe", "copper_axe", "Kupferaxt", ItemCategory.Tool, 1, "Assets/_Game/Art/Items/ITEM_TMP_Axe.png", 120, "tool", "wood"),
			Equipment("CopperScythe", "copper_scythe", "Kupfersense", ItemCategory.Tool, 1, "Assets/_Game/Art/Items/ITEM_TMP_Scythe.png", 120, "tool", "fiber"),
			Equipment("CopperPickaxe", "copper_pickaxe", "Kupferspitzhacke", ItemCategory.Tool, 1, "Assets/_Game/Art/Items/ITEM_TMP_Pickaxe.png", 120, "tool", "stone", "ore"),
			Equipment("IronAxe", "iron_axe", "Eisenaxt", ItemCategory.Tool, 2, "Assets/_Game/Art/Items/ITEM_TMP_Axe.png", 140, "tool", "wood"),
			Equipment("IronScythe", "iron_scythe", "Eisensense", ItemCategory.Tool, 2, "Assets/_Game/Art/Items/ITEM_TMP_Scythe.png", 140, "tool", "fiber"),
			Equipment("IronPickaxe", "iron_pickaxe", "Eisenspitzhacke", ItemCategory.Tool, 2, "Assets/_Game/Art/Items/ITEM_TMP_Pickaxe.png", 140, "tool", "stone", "ore"),
			Armor("CopperHelmet", "armor_copper_helmet", "Kupferhelm", 1, 120, WearableSlot.Head, 0.04f, "Assets/_Game/Art/Items/ITEM_TMP_CopperHelmet.png"),
			Armor("CopperChest", "armor_copper_chest", "Kupferharnisch", 1, 120, WearableSlot.Chest, 0.08f, "Assets/_Game/Art/Items/ITEM_TMP_CopperChest.png"),
			Armor("CopperGloves", "armor_copper_gloves", "Kupferhandschuhe", 1, 120, WearableSlot.Hands, 0.02f, "Assets/_Game/Art/Items/ITEM_TMP_CopperGloves.png"),
			Armor("CopperLegs", "armor_copper_legs", "Kupferbeinschutz", 1, 120, WearableSlot.Legs, 0.06f, "Assets/_Game/Art/Items/ITEM_TMP_CopperLegs.png"),
			Armor("IronHelmet", "armor_iron_helmet", "Eisenhelm", 2, 170, WearableSlot.Head, 0.06f, "Assets/_Game/Art/Items/ITEM_TMP_IronHelmet.png"),
			Armor("IronChest", "armor_iron_chest", "Eisenharnisch", 2, 170, WearableSlot.Chest, 0.12f, "Assets/_Game/Art/Items/ITEM_TMP_IronChest.png"),
			Armor("IronGloves", "armor_iron_gloves", "Eisenhandschuhe", 2, 170, WearableSlot.Hands, 0.03f, "Assets/_Game/Art/Items/ITEM_TMP_IronGloves.png"),
			Armor("IronLegs", "armor_iron_legs", "Eisenbeinschutz", 2, 170, WearableSlot.Legs, 0.09f, "Assets/_Game/Art/Items/ITEM_TMP_IronLegs.png")
		};
	}

	private static ItemDefinition Material(string assetName, string id, string name, ItemCategory category, string family, int tier, string icon, params string[] tags)
	{
		// icon WIRKLICH durchreichen: die alte Fassung ignorierte den
		// Parameter und baute stur "ITEM_TMP_" + assetName — deshalb suchte
		// der Builder fuer smithing_mark die nie vorhandene
		// ITEM_TMP_SmithingMark.png, obwohl die Tabelle laengst auf
		// ITEM_SmithingMark.png zeigte (fuenf EidraForgeDungeonTests rot).
		return ItemContentAssetBuilder.EnsureItem(assetName, id, name, $"Material der Stufe T{tier}.", category, family, tier, icon, 10, canBeUsed: false, canBeDropped: true, canBeStored: true, category == ItemCategory.Material, tags, ItemUseActionType.None);
	}

	private static ItemDefinition Equipment(string assetName, string id, string name, ItemCategory category, int tier, string icon, int durability, params string[] tags)
	{
		return Equipment(assetName, id, name, category, tier, icon, durability, craftable: true, tags);
	}

	private static ItemDefinition Equipment(string assetName, string id, string name, ItemCategory category, int tier, string icon, int durability, bool craftable, params string[] tags)
	{
		return ItemContentAssetBuilder.EnsureItem(assetName, id, name, $"Reguläre Ausrüstung der Stufe T{tier}.", category, string.Empty, tier, "Assets/_Game/Art/Items/ITEM_TMP_" + assetName + ".png", 1, canBeUsed: false, canBeDropped: true, canBeStored: true, craftable, tags, ItemUseActionType.None, 0f, 0f, 0f, 0f, durability);
	}

	private static ItemDefinition Armor(string assetName, string id, string name, int tier, int durability, WearableSlot slot, float protection, string icon)
	{
		// icon WIRKLICH durchreichen — dieselbe Falle wie im Material-Helfer:
		// die alte Fassung baute stur "ITEM_TMP_" + assetName und traf fuer
		// Kupfer/Eisen nur zufaellig die richtige Datei (F31-002).
		return ItemContentAssetBuilder.EnsureItem(assetName, id, name, $"Rüstungsteil der Stufe T{tier} mit " + $"{protection:P0} direktem Schutz.", ItemCategory.Armor, string.Empty, tier, icon, 1, canBeUsed: false, canBeDropped: true, canBeStored: true, canBeCrafted: true, new string[2]
		{
			"armor",
			slot.ToString().ToLowerInvariant()
		}, ItemUseActionType.None, 0f, 0f, 0f, 0f, durability, slot, protection);
	}
}
}
