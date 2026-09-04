using Eidren.Core.Services;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System;

namespace Eidren.Tests.EditMode
{
public sealed class DurableItemSaveMigrationTests
{
	[Test]
	public void VersionTen_FillsLegacyDurabilityAcrossEveryContainer()
	{
		SaveGameData legacy = LegacySave();
		legacy.player.inventory[0] = Stack("hammer");
		legacy.player.equipment = new SaveEquipmentEntry[1]
		{
			new SaveEquipmentEntry
			{
				slot = 3,
				stack = Stack("armor_wanderer_coat")
			}
		};
		legacy.storageContainers = new SaveStorageData[1]
		{
			new SaveStorageData
			{
				containerId = "home.chest.1",
				slots = new SaveItemStackData[1] { Stack("pickaxe") }
			}
		};
		legacy.player.weaponUpgrades = new SaveWeaponUpgradeData[1]
		{
			new SaveWeaponUpgradeData
			{
				weaponId = "hammer",
				level = 2,
				healthDamageMultiplier = 2f,
				staggerDamageMultiplier = 2f
			}
		};
		Assert.That<bool>(SaveGameMigration.TryMigrate(legacy, out var migrated, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		AssertDurable(migrated.player.inventory[0], 120);
		AssertDurable(migrated.player.equipment[0].stack, 80);
		AssertDurable(migrated.storageContainers[0].slots[0], 100);
		Assert.That<SaveWeaponUpgradeData[]>(migrated.player.weaponUpgrades, (IResolveConstraint)(object)Is.Empty, "Das abgeloeste Waffen-Upgrade-Modell darf keinen Bonus in v0.2 tragen.", Array.Empty<object>());
	}

	[Test]
	public void VersionTen_MigrationKeepsExistingInstanceIdentity()
	{
		SaveGameData saveGameData = LegacySave();
		saveGameData.player.inventory[0] = new SaveItemStackData
		{
			itemId = "daggers",
			quantity = 1,
			instanceId = "item.existing",
			durability = 3
		};
		SaveGameMigration.TryMigrate(saveGameData, out var migrated, out var _);
		Assert.That<string>(migrated.player.inventory[0].instanceId, (IResolveConstraint)(object)Is.EqualTo((object)"item.existing"));
		Assert.That<int>(migrated.player.inventory[0].durability, (IResolveConstraint)(object)Is.EqualTo((object)220));
	}

	[Test]
	public void VersionTen_MigrationLeavesOrdinaryStacksUnchanged()
	{
		SaveGameData saveGameData = LegacySave();
		saveGameData.player.inventory[0] = new SaveItemStackData
		{
			itemId = "wood",
			quantity = 7
		};
		SaveGameMigration.TryMigrate(saveGameData, out var migrated, out var _);
		Assert.That<int>(migrated.player.inventory[0].quantity, (IResolveConstraint)(object)Is.EqualTo((object)7));
		Assert.That<int>(migrated.player.inventory[0].durability, (IResolveConstraint)(object)Is.Zero);
		Assert.That<string>(migrated.player.inventory[0].instanceId, (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void VersionTen_TransfersTheCurrentLevelXpPercentage()
	{
		SaveGameData saveGameData = LegacySave();
		saveGameData.progression = new SaveProgressionData
		{
			level = 10,
			experience = 165
		};
		SaveGameMigration.TryMigrate(saveGameData, out var migrated, out var _);
		Assert.That<int>(migrated.progression.level, (IResolveConstraint)(object)Is.EqualTo((object)10));
		Assert.That<int>(migrated.progression.experience, (IResolveConstraint)(object)Is.EqualTo((object)337), "165/330 der alten Anforderung werden auf 337/675 der neuen Kurve uebertragen.", Array.Empty<object>());
	}

	[Test]
	public void VersionTen_CombinesClothNodesAndRefundsRemovedNodes()
	{
		SaveGameData legacy = LegacySave();
		legacy.progression = new SaveProgressionData
		{
			availableTechnologyPoints = 2,
			spentTechnologyPoints = 7,
			unlockedTechnologyNodeIds = new string[7] { "technology.01.workbench", "technology.18.t1_tools", "technology.19.t1_weapons", "technology.20.cloth_hood", "technology.21.cloth_coat", "technology.22.cloth_bracers", "technology.23.cloth_shoes" }
		};
		SaveGameMigration.TryMigrate(legacy, out var migrated, out var _);
		Assert.That<string[]>(migrated.progression.unlockedTechnologyNodeIds, (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[3] { "technology.01.workbench", "technology.18.t1_tools", "technology.20.cloth_hood" }));
		Assert.That<int>(migrated.progression.availableTechnologyPoints, (IResolveConstraint)(object)Is.EqualTo((object)6));
		Assert.That<int>(migrated.progression.spentTechnologyPoints, (IResolveConstraint)(object)Is.EqualTo((object)3));
	}

	[Test]
	public void DefeatedGaron_ReceivesOnlyTheRetroactiveV02Rewards()
	{
		SaveGameData legacy = LegacySave();
		legacy.progression = new SaveProgressionData
		{
			availableTechnologyPoints = 3,
			experience = 17
		};
		legacy.world.progressFlags = new string[1] { "garon_defeated" };
		SaveGameMigration.TryMigrate(legacy, out var migrated, out var _);
		Assert.That<int>(migrated.progression.availableTechnologyPoints, (IResolveConstraint)(object)Is.EqualTo((object)4));
		Assert.That<string[]>(migrated.progression.knownBlueprintIds, (IResolveConstraint)(object)Does.Contain("blueprint.copper_spear"));
		Assert.That<string[]>(migrated.world.progressFlags, (IResolveConstraint)(object)Does.Contain("garon_v02_rewards_granted"));
		Assert.That<int>(migrated.progression.experience, (IResolveConstraint)(object)Is.EqualTo((object)49), "Nur die prozentuale Kurvenmigration gilt; Boss-EP werden nicht erneut vergeben.", Array.Empty<object>());
	}

	private static SaveGameData LegacySave()
	{
		return new SaveGameData
		{
			saveVersion = 10,
			player = new SavePlayerData
			{
				inventory = new SaveItemStackData[16]
			}
		};
	}

	private static SaveItemStackData Stack(string itemId)
	{
		return new SaveItemStackData
		{
			itemId = itemId,
			quantity = 1
		};
	}

	private static void AssertDurable(SaveItemStackData stack, int expectedMaximum)
	{
		Assert.That<int>(stack.durability, (IResolveConstraint)(object)Is.EqualTo((object)expectedMaximum));
		Assert.That<string>(stack.instanceId, (IResolveConstraint)(object)Is.Not.Empty);
	}
}
}
