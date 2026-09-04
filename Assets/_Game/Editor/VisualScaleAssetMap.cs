using System.Collections.Generic;
using System;

namespace Eidren.Editor
{
public static class VisualScaleAssetMap
{
	public const string ResourceVisualFolder = "Assets/_Game/Prefabs/Resources/Visuals";

	public const string PropFolder = "Assets/_Game/Prefabs/Environment/StyleProof";

	public const string BuildingFolder = "Assets/_Game/Prefabs/Buildings/Level01";

	public const string StationFolder = "Assets/_Game/Prefabs/Stations";

	public const string ActorVisualFolder = "Assets/_Game/Prefabs/Actors";

	public static IReadOnlyDictionary<string, string> BakedPrefabs { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["visual.player"] = "Assets/_Game/Prefabs/Actors/2D/Player_2D.prefab",
		["visual.eidra_small"] = "Assets/_Game/Resources/Prefabs/Actors/2D/Terrock_2D.prefab",
		["visual.boss_garon"] = "Assets/_Game/Prefabs/Actors/2D/Garon_2D.prefab",
		["visual.tree.active"] = "Assets/_Game/Prefabs/Resources/Visuals/Tree_Active.prefab",
		["visual.tree.exhausted"] = "Assets/_Game/Prefabs/Resources/Visuals/Tree_Exhausted.prefab",
		["visual.berry_bush.active"] = "Assets/_Game/Prefabs/Resources/Visuals/BerryBush_Active.prefab",
		["visual.berry_bush.exhausted"] = "Assets/_Game/Prefabs/Resources/Visuals/BerryBush_Exhausted.prefab",
		["visual.fiber_plant.active"] = "Assets/_Game/Prefabs/Resources/Visuals/FiberPlant_Active.prefab",
		["visual.fiber_plant.exhausted"] = "Assets/_Game/Prefabs/Resources/Visuals/FiberPlant_Exhausted.prefab",
		["visual.stone_deposit.active"] = "Assets/_Game/Prefabs/Resources/Visuals/StoneDeposit_Active.prefab",
		["visual.stone_deposit.exhausted"] = "Assets/_Game/Prefabs/Resources/Visuals/StoneDeposit_Exhausted.prefab",
		["visual.copper_vein.active"] = "Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Active.prefab",
		["visual.copper_vein.exhausted"] = "Assets/_Game/Prefabs/Resources/Visuals/CopperVein_Exhausted.prefab",
		["visual.hardwood_tree.active"] = "Assets/_Game/Prefabs/Resources/Visuals/HardwoodTree_Active.prefab",
		["visual.hardwood_tree.exhausted"] = "Assets/_Game/Prefabs/Resources/Visuals/HardwoodTree_Exhausted.prefab",
		["visual.swamp_hemp.active"] = "Assets/_Game/Prefabs/Resources/Visuals/SwampHemp_Active.prefab",
		["visual.swamp_hemp.exhausted"] = "Assets/_Game/Prefabs/Resources/Visuals/SwampHemp_Exhausted.prefab",
		["visual.granite_deposit.active"] = "Assets/_Game/Prefabs/Resources/Visuals/GraniteDeposit_Active.prefab",
		["visual.granite_deposit.exhausted"] = "Assets/_Game/Prefabs/Resources/Visuals/GraniteDeposit_Exhausted.prefab",
		["visual.iron_vein.active"] = "Assets/_Game/Prefabs/Resources/Visuals/IronVein_Active.prefab",
		["visual.iron_vein.exhausted"] = "Assets/_Game/Prefabs/Resources/Visuals/IronVein_Exhausted.prefab",
		["visual.wildling"] = "Assets/_Game/Prefabs/Actors/2D/Wildling_2D.prefab",
		["visual.prop.tree_a"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_A.prefab",
		["visual.prop.tree_b"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_B.prefab",
		["visual.prop.tree_c"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_Tree_C.prefab",
		["visual.prop.rock_large"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Large.prefab",
		["visual.prop.rock_medium"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Medium.prefab",
		["visual.prop.rock_small"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_Rock_Small.prefab",
		["visual.prop.bush"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Bush.prefab",
		["visual.prop.fern"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Fern.prefab",
		["visual.prop.flowers"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_Plant_Flowers.prefab",
		["visual.prop.ground_grass"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_GroundCover_Grass.prefab",
		["visual.prop.ground_moss"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_GroundCover_Moss.prefab",
		["visual.prop.glow_mushrooms"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_Accent_GlowMushrooms.prefab",
		["visual.prop.ruin_wall_a"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_RuinWall_A.prefab",
		["visual.prop.ruin_wall_b"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_RuinWall_B.prefab",
		["visual.prop.ruin_monument"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_RuinMonument.prefab",
		["visual.prop.rune"] = "Assets/_Game/Prefabs/Environment/StyleProof/SP_EidrenRune.prefab",
		["visual.building.wall"] = "Assets/_Game/Prefabs/Buildings/Level01/BLD_Wall_L01.prefab",
		["visual.building.door"] = "Assets/_Game/Prefabs/Buildings/Level01/BLD_Door_L01.prefab",
		["visual.building.floor"] = "Assets/_Game/Prefabs/Buildings/Level01/BLD_Floor_L01.prefab",
		["visual.building.farm_plot"] = "Assets/_Game/Prefabs/Buildings/Level01/BLD_FarmPlot_L01.prefab",
		["visual.building.cooking_pot"] = "Assets/_Game/Prefabs/Buildings/Level01/BLD_CookingPot_L01.prefab",
		["visual.building.smelter"] = "Assets/_Game/Prefabs/Buildings/Level01/BLD_Smelter_L01.prefab",
		["visual.building.sawmill"] = "Assets/_Game/Prefabs/Buildings/Level01/BLD_Sawmill_L01.prefab",
		["visual.building.ropewalk"] = "Assets/_Game/Prefabs/Buildings/Level01/BLD_Ropewalk_L01.prefab",
		["visual.building.stonecutter"] = "Assets/_Game/Prefabs/Buildings/Level01/BLD_Stonecutter_L01.prefab",
		["visual.building.workbench"] = "Assets/_Game/Prefabs/Stations/Workbench.prefab",
		["visual.building.storage_chest"] = "Assets/_Game/Prefabs/Stations/StorageChest.prefab",
		["visual.world_item"] = "Assets/_Game/Prefabs/Items/WorldItem.prefab",
		["visual.death_bag"] = "Assets/_Game/Resources/Prefabs/DeathBag.prefab"
	};

	public static IReadOnlyDictionary<string, string> RuntimeArt { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

	public static IReadOnlyCollection<string> WithoutOwnArtwork { get; } = new HashSet<string>(StringComparer.Ordinal);

	public static IEnumerable<string> PrefabFolders()
	{
		yield return "Assets/_Game/Prefabs/Resources/Visuals";
		yield return "Assets/_Game/Prefabs/Environment/StyleProof";
		yield return "Assets/_Game/Prefabs/Buildings/Level01";
		yield return "Assets/_Game/Prefabs/Stations";
		yield return "Assets/_Game/Prefabs/Actors";
	}
}
}
