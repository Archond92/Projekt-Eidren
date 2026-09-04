namespace Eidren.Data
{
	public static class AudioEventIds
	{
		public const string PlayerFootstep = "player.footstep";

		public const string PlayerDodge = "player.dodge";

		public const string PlayerHammer = "player.attack.hammer";

		public const string PlayerDaggers = "player.attack.daggers";

		public const string PlayerSpear = "player.attack.spear";

		public const string PlayerHit = "player.hit";

		public const string PlayerDamage = "player.damage";

		public const string PlayerHealingPotion = "player.healing_potion";

		public const string PlayerBuffFood = "player.buff_food";

		public const string ResourceWood = "resource.wood";

		public const string ResourceStone = "resource.stone";

		public const string ResourcePlantFiber = "resource.plant_fiber";

		public const string ResourceCopperOre = "resource.copper_ore";

		public const string UiItemPickup = "ui.item_pickup";

		public const string UiButton = "ui.button";

		public const string UiMenuOpen = "ui.menu_open";

		public const string UiMenuClose = "ui.menu_close";

		public const string UiCraftSuccess = "ui.crafting_success";

		public const string UiCraftFailure = "ui.crafting_failure";

		public const string WildlingDiscover = "wildling.discover";

		public const string WildlingAttack = "wildling.attack";

		public const string WildlingHit = "wildling.hit";

		public const string WildlingStagger = "wildling.stagger";

		public const string WildlingDeath = "wildling.death";

		public const string GaronDiscover = "garon.discover";

		public const string GaronFront = "garon.attack.front";

		public const string GaronCharge = "garon.attack.charge";

		public const string GaronSpin = "garon.attack.spin";

		public const string GaronHit = "garon.hit";

		public const string GaronStagger = "garon.stagger";

		public const string GaronDeath = "garon.death";

		public const string Victory = "state.victory";

		public const string Defeat = "state.defeat";

		public const string SceneTransition = "state.scene_transition";

		public const string MusicMainMenu = "music.main_menu";

		public const string MusicWorldMap = "music.world_map";

		public const string MusicHomeBase = "music.home_base";

		public const string MusicOutdoor = "music.outdoor";

		public const string MusicGaron = "music.garon";

		public const string AmbienceGreenwood = "ambience.greenwood";

		public const string AmbienceQuarry = "ambience.quarry";

		public const string AmbienceMarsh = "ambience.marsh";

		public const string AmbienceEmberRuins = "ambience.ember_ruins";

		public const string V02MusicForge = "v02.music.eidra_forge";

		public const string V02AmbienceTwilightGrove = "v02.ambience.twilight_grove";

		public const string V02AmbienceVeilMarsh = "v02.ambience.veil_marsh";

		public const string V02AmbienceGreyRifts = "v02.ambience.grey_rifts";

		public const string V02AmbienceEidraForge = "v02.ambience.eidra_forge";

		public static string V02Enemy(string enemyId, string cue)
		{
			return "v02.enemy." + enemyId + "." + cue;
		}

		public static string V02Container(string family, string cue)
		{
			return "v02.container." + family + "." + cue;
		}

		public static string V02Resource(string resourceId, string cue)
		{
			return "v02.resource." + resourceId + "." + cue;
		}

		public static string V02Ignivar(string cue)
		{
			return "v02.ignivar." + cue;
		}

		public static string V02Ui(string cue)
		{
			return "v02.ui." + cue;
		}

		public static string V02WeaponImpact(string family, string surface)
		{
			return "v02.weapon." + family + ".impact." + surface;
		}
	}
}
