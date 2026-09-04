using System;

namespace Eidren.Core.Services
{
	[Serializable]
	public sealed class SaveGameData
	{
		public int saveVersion = 15;

		public string createdUtc = string.Empty;

		public string lastSavedUtc = string.Empty;

		public string buildVersion = string.Empty;

		public SavePlayerData player = new SavePlayerData();

		public SaveStorageData[] storageContainers = Array.Empty<SaveStorageData>();

		public SaveWorldData world = new SaveWorldData();

		public SaveQuarantineEntry[] quarantinedEntries = Array.Empty<SaveQuarantineEntry>();

		public SaveDeathBagData deathBag = new SaveDeathBagData();

		public SaveZoneStateData[] zones = Array.Empty<SaveZoneStateData>();

		public SaveProgressionData progression = new SaveProgressionData();

		public SaveBuildingData[] buildings = Array.Empty<SaveBuildingData>();

		public SaveEidraForgeData eidraForge = new SaveEidraForgeData();

		public SaveQuestData quest = new SaveQuestData();
	}

	[Serializable]
	public sealed class SaveBuildingData
	{
		public string instanceId = string.Empty;

		public string buildingId = string.Empty;

		public float positionX;

		public float positionZ;

		public float positionY;

		public int quarterTurns;

		public int level = 1;

		public int cellX;

		public int cellZ;

		public int placementKind;

		public int edgeOrientation;

		public int plantedSlots;

		public int readyOutput;
	}

	[Serializable]
	public sealed class SaveDeathBagData
	{
		public bool exists;

		public string sceneKey = string.Empty;

		public float positionX;

		public float positionY;

		public float positionZ;
	}

	[Serializable]
	public sealed class SaveEidraForgeData
	{
		public int status;

		public string runId = string.Empty;

		public int lootSeed;

		public long completedUtcTicks;

		public bool firstCompletionGranted;

		public bool repeatRun;

		public bool ignivarCaptured;

		public bool teamSelectionPending;

		/// <summary>Formwall im Einbruch aufgebaut. Altstaende ohne dieses Feld
		/// laden als false - der Ausbau gilt dann als nicht vorhanden.</summary>
		public bool formwallGebaut;

		/// <summary>Ertrag dieses Laufs noch nicht abgeholt.</summary>
		public bool formwallErtragOffen;

		public SaveForgeEnemyData[] enemies = Array.Empty<SaveForgeEnemyData>();

		public SaveForgeContainerData[] chests = Array.Empty<SaveForgeContainerData>();

		public SaveForgeDropData[] drops = Array.Empty<SaveForgeDropData>();

		public SaveForgeRewardChestData[] rewardChests = Array.Empty<SaveForgeRewardChestData>();

		public SaveItemStackData[] recoverySlots = Array.Empty<SaveItemStackData>();
	}

	[Serializable]
	public sealed class SaveQuestData
	{
		public string chainId = string.Empty;

		public int stepIndex;

		public int stepProgress;

		public bool completed;
	}

	[Serializable]
	public sealed class SaveEidraInstanceData
	{
		public string instanceId = string.Empty;

		public string eidraId = string.Empty;
	}

	[Serializable]
	public sealed class SaveEquipmentEntry
	{
		public int slot;

		public SaveItemStackData stack = new SaveItemStackData();
	}

	[Serializable]
	public sealed class SaveForgeContainerData
	{
		public string instanceId = string.Empty;

		public int family;

		public bool opened;

		public SaveItemStackData[] slots = Array.Empty<SaveItemStackData>();
	}

	[Serializable]
	public sealed class SaveForgeDropData
	{
		public string dropId = string.Empty;

		public SaveItemStackData stack = new SaveItemStackData();

		public float positionX;

		public float positionY;

		public float positionZ;
	}

	[Serializable]
	public sealed class SaveForgeEnemyData
	{
		public string spawnId = string.Empty;

		public float health;

		public float stagger;

		public bool defeated;

		public bool markDropped;
	}

	[Serializable]
	public sealed class SaveForgeRewardChestData
	{
		public int size;

		public int purchaseIndex;

		public SaveItemStackData[] slots = Array.Empty<SaveItemStackData>();
	}

	[Serializable]
	public sealed class SaveItemStackData
	{
		public string itemId = string.Empty;

		public int quantity;

		public string instanceId = string.Empty;

		public int durability;
	}

	[Serializable]
	public sealed class SavePlayerData
	{
		public SaveItemStackData[] inventory = Array.Empty<SaveItemStackData>();

		public string activeWeaponId = "hammer";

		public SaveWeaponUpgradeData[] weaponUpgrades = Array.Empty<SaveWeaponUpgradeData>();

		public string activeEidraId = "terrock";

		public string lastSafeNodeId = "home_base";

		public string lastSafeSceneKey = "HomeBase";

		public SaveEquipmentEntry[] equipment = Array.Empty<SaveEquipmentEntry>();

		public SaveEidraInstanceData[] eidraRoster = Array.Empty<SaveEidraInstanceData>();

		public string[] activeEidraInstanceIds = Array.Empty<string>();
	}

	[Serializable]
	public sealed class SaveProgressionData
	{
		public int stage = 1;

		public int level = 1;

		public int experience;

		public int availableTechnologyPoints;

		public int spentTechnologyPoints;

		public string[] firstCraftedRecipeIds = Array.Empty<string>();

		public string[] firstBuiltBuildingIds = Array.Empty<string>();

		public string[] unlockedTechnologyNodeIds = Array.Empty<string>();

		public string[] knownBlueprintIds = Array.Empty<string>();
	}

	[Serializable]
	public sealed class SaveQuarantineEntry
	{
		public string source = string.Empty;

		public string stableId = string.Empty;

		public string reason = string.Empty;
	}

	[Serializable]
	public sealed class SaveStorageData
	{
		public string containerId = string.Empty;

		public SaveItemStackData[] slots = Array.Empty<SaveItemStackData>();
	}

	[Serializable]
	public sealed class SaveWeaponUpgradeData
	{
		public string weaponId = string.Empty;

		public int level = 1;

		public float healthDamageMultiplier = 1f;

		public float staggerDamageMultiplier = 1f;
	}

	[Serializable]
	public sealed class SaveWorldChestData
	{
		public string instanceId = string.Empty;

		public string spawnPointId = string.Empty;

		public int family;

		public bool opened;

		public int initialOccupiedSlotCount;

		public int initialTotalQuantity;

		public SaveItemStackData[] slots = Array.Empty<SaveItemStackData>();
	}

	[Serializable]
	public sealed class SaveWorldData
	{
		public string currentNodeId = "home_base";

		public string selectedNodeId = "home_base";

		public string[] visitedNodeIds = Array.Empty<string>();

		public string[] eventNodeIds = Array.Empty<string>();

		public string[] progressFlags = Array.Empty<string>();

		public string[] completedNodeIds = Array.Empty<string>();
	}

	[Serializable]
	public sealed class SaveZoneStateData
	{
		public string zoneId = string.Empty;

		public int seed;

		public int generatorVersion;

		public string[] harvestedNodeIds = Array.Empty<string>();

		public SaveWorldChestData[] worldChests = Array.Empty<SaveWorldChestData>();
	}
}
