using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class WorldChestV02Tests
{
	private sealed class ContentFixture : IDisposable
	{
		private readonly GameObject _root;

		public ContentDatabase Database { get; }

		public ContentFixture()
		{
			_root = new GameObject("WorldChestContentFixture");
			Database = _root.AddComponent<ContentDatabase>();
			ItemDefinition[] items = (from value in AssetDatabase.FindAssets("t:ItemDefinition", new string[1] { "Assets/_Game/Data/Items" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ItemDefinition>)
				where value != null
				select value).ToArray();
			Database.ConfigureItems(items);
		}

		public void Dispose()
		{
			UnityEngine.Object.DestroyImmediate(_root);
		}
	}

	private static readonly string[] ZoneNames = new string[7] { "Greenwood", "Marsh", "Quarry", "EmberRuins", "TwilightGrove", "VeilMarsh", "GreyRifts" };

	[Test]
	public void EveryWorldZone_HasStablePopulationWithinRequiredCounts()
	{
		string[] zoneNames = ZoneNames;
		foreach (string name in zoneNames)
		{
			ZoneDefinition zone = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/Zone_" + name + ".asset");
			Assert.That<ZoneDefinition>(zone, (IResolveConstraint)(object)Is.Not.Null, name, Array.Empty<object>());
			Assert.That<WorldChestLootProfile>(zone.WorldChestLootProfile, (IResolveConstraint)(object)Is.Not.Null, name, Array.Empty<object>());
			Assert.That<int>(zone.WorldChestSpawnPoints.Count, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)10), name, Array.Empty<object>());
			WorldChestPlacement[] first = WorldChestPopulationGenerator.Generate(zone.Id, 918273, zone.WorldChestSpawnPoints);
			Assert.That<string>(Signature(WorldChestPopulationGenerator.Generate(zone.Id, 918273, zone.WorldChestSpawnPoints)), (IResolveConstraint)(object)Is.EqualTo((object)Signature(first)), name, Array.Empty<object>());
			Assert.That<int>(first.Count((WorldChestPlacement value) => value.Family == WorldChestFamily.Common), (IResolveConstraint)(object)Is.InRange((IComparable)2, (IComparable)4), name, Array.Empty<object>());
			Assert.That<int>(first.Count((WorldChestPlacement value) => value.Family == WorldChestFamily.Guarded), (IResolveConstraint)(object)Is.InRange((IComparable)0, (IComparable)1), name, Array.Empty<object>());
			Assert.That<int>(first.Count((WorldChestPlacement value) => value.Family == WorldChestFamily.Hidden), (IResolveConstraint)(object)Is.InRange((IComparable)0, (IComparable)1), name, Array.Empty<object>());
			Assert.That<int>(first.Select((WorldChestPlacement value) => value.SpawnPointId).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)first.Length), name, Array.Empty<object>());
		}
	}

	[Test]
	public void ChestState_TracksOpenedPartialEmptyAndRegeneration()
	{
		ItemStack[] slots = new ItemStack[24];
		slots[0] = new ItemStack("wood", 3);
		slots[1] = new ItemStack("berry", 1);
		WorldChestState chest = new WorldChestState("zone.chest.01", "point.01", WorldChestFamily.Common, slots);
		ZoneState zoneState = new ZoneState("zone.test", 42, 2);
		zoneState.SetWorldChest(chest);
		Assert.That<WorldChestStatus>(chest.Status, (IResolveConstraint)(object)Is.EqualTo((object)WorldChestStatus.Closed));
		chest.MarkOpened();
		Assert.That<WorldChestStatus>(chest.Status, (IResolveConstraint)(object)Is.EqualTo((object)WorldChestStatus.Opened));
		slots[0] = new ItemStack("wood", 2);
		chest.ReplaceSlots(slots);
		Assert.That<WorldChestStatus>(chest.Status, (IResolveConstraint)(object)Is.EqualTo((object)WorldChestStatus.PartiallyEmptied));
		chest.ReplaceSlots(new ItemStack[24]);
		Assert.That<WorldChestStatus>(chest.Status, (IResolveConstraint)(object)Is.EqualTo((object)WorldChestStatus.Emptied));
		zoneState.Regenerate(84, 2);
		Assert.That<int>(zoneState.WorldChestCount, (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void LootProbabilitiesAndTierWeights_MatchAuthoredContract()
	{
		using ContentFixture fixture = new ContentFixture();
		AssertProfile(fixture, "Greenwood", tierTwo: false);
		AssertProfile(fixture, "TwilightGrove", tierTwo: true);
	}

	[Test]
	public void VersionElevenMigration_AddsChestAndUnstartedDungeonState()
	{
		SaveGameData saveGameData = new SaveGameData();
		saveGameData.saveVersion = 11;
		saveGameData.world = new SaveWorldData
		{
			progressFlags = new string[1] { "garon_defeated" }
		};
		saveGameData.deathBag = new SaveDeathBagData
		{
			exists = true,
			sceneKey = "Zone_Marsh",
			positionX = 3f
		};
		saveGameData.zones = new SaveZoneStateData[1]
		{
			new SaveZoneStateData
			{
				zoneId = "zone_greenwood",
				seed = 123,
				generatorVersion = 2,
				harvestedNodeIds = new string[1] { "tree.01" },
				worldChests = null
			}
		};
		Assert.That<bool>(SaveGameMigration.TryMigrate(saveGameData, out var migrated, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(migrated.saveVersion, (IResolveConstraint)(object)Is.EqualTo((object)15));
		Assert.That<SaveWorldChestData[]>(migrated.zones[0].worldChests, (IResolveConstraint)(object)Is.Empty);
		Assert.That<int>(migrated.zones[0].seed, (IResolveConstraint)(object)Is.EqualTo((object)123));
		Assert.That<string[]>(migrated.zones[0].harvestedNodeIds, (IResolveConstraint)(object)Is.EqualTo((object)new string[1] { "tree.01" }));
		Assert.That<string[]>(migrated.world.progressFlags, (IResolveConstraint)(object)Is.EqualTo((object)new string[1] { "garon_defeated" }));
		Assert.That<bool>(migrated.deathBag.exists, (IResolveConstraint)(object)Is.True);
		Assert.That<float>(migrated.deathBag.positionX, (IResolveConstraint)(object)Is.EqualTo((object)3f));
		Assert.That<SaveEidraForgeData>(migrated.eidraForge, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<int>(migrated.eidraForge.status, (IResolveConstraint)(object)Is.EqualTo((object)0));
	}

	private static void AssertProfile(ContentFixture fixture, string name, bool tierTwo)
	{
		ZoneDefinition zoneDefinition = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/Zone_" + name + ".asset");
		Assert.That<ZoneDefinition>(zoneDefinition, (IResolveConstraint)(object)Is.Not.Null, name, Array.Empty<object>());
		WorldChestLootProfile profile = zoneDefinition.WorldChestLootProfile;
		Assert.That<WorldChestLootProfile>(profile, (IResolveConstraint)(object)Is.Not.Null, name, Array.Empty<object>());
		Assert.That<string[]>(profile.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty);
		foreach (WorldChestFamily family in Enum.GetValues(typeof(WorldChestFamily)))
		{
			int equipment = 0;
			int refinement = 0;
			int fitting = 0;
			int named = 0;
			int[] tiers = new int[3];
			for (int seed = 1; seed <= 30000; seed++)
			{
				ItemStack[] array = WorldChestLootGenerator.Generate(profile, family, $"{name}.{family}.{seed}", seed, new WorldChestProgressContext(garonDefeated: true, tierTwoUnlocked: true), fixture.Database);
				Assert.That<int>(array[0].Quantity, (IResolveConstraint)(object)((family == WorldChestFamily.Common) ? Is.InRange((IComparable)1, (IComparable)3) : Is.InRange((IComparable)2, (IComparable)4)));
				Assert.That<int>(array[1].Quantity, (IResolveConstraint)(object)Is.EqualTo((object)1));
				foreach (ItemStack stack in array.Where((ItemStack value) => !value.IsEmpty))
				{
					ItemDefinition item = fixture.Database.GetItem(stack.ItemId);
					if (profile.RegionalRefinementItemIds.Contains(stack.ItemId))
					{
						refinement++;
					}
					if (stack.ItemId == "smithing_fitting")
					{
						fitting++;
					}
					if (profile.NamedWeaponItemIds.Contains(stack.ItemId))
					{
						named++;
					}
					ItemCategory category = item.Category;
					if ((category == ItemCategory.Weapon || category == ItemCategory.Armor || category == ItemCategory.Tool) && !profile.NamedWeaponItemIds.Contains(stack.ItemId))
					{
						equipment++;
						tiers[item.Tier]++;
						if (family == WorldChestFamily.Common)
						{
							Assert.That<int>(stack.Durability, (IResolveConstraint)(object)Is.InRange((IComparable)Mathf.CeilToInt((float)item.MaximumDurability * 0.4f), (IComparable)Mathf.CeilToInt((float)item.MaximumDurability * 0.8f)));
						}
					}
				}
			}
			AssertRate(equipment, 30000, family switch
			{
				WorldChestFamily.Common => 0.01, 
				WorldChestFamily.Guarded => 0.05, 
				_ => 0.1, 
			}, 0.009, $"{name} {family} equipment");
			if (family == WorldChestFamily.Guarded)
			{
				AssertRate(refinement, 30000, 0.2, 0.015, name + " guarded refinement");
			}
			if (family == WorldChestFamily.Hidden)
			{
				AssertRate(refinement, 30000, 0.5, 0.015, name + " hidden refinement");
			}
			if (tierTwo && family == WorldChestFamily.Hidden)
			{
				AssertRate(fitting, 30000, 0.1, 0.012, "T2 fitting");
				AssertRate(named, 30000, 0.01, 0.005, "T2 named weapon");
				AssertRate(tiers[0], equipment, 0.25, 0.04, "T2 T0 weight");
				AssertRate(tiers[1], equipment, 0.35, 0.04, "T2 T1 weight");
				AssertRate(tiers[2], equipment, 0.4, 0.04, "T2 T2 weight");
			}
			if (!tierTwo)
			{
				Assert.That<int>(tiers[2], (IResolveConstraint)(object)Is.Zero);
			}
		}
	}

	private static void AssertRate(int count, int total, double expected, double tolerance, string label)
	{
		Assert.That<int>(total, (IResolveConstraint)(object)Is.GreaterThan((object)0), label, Array.Empty<object>());
		Assert.That<double>((double)count / (double)total, (IResolveConstraint)(object)Is.InRange((IComparable)(expected - tolerance), (IComparable)(expected + tolerance)), label, Array.Empty<object>());
	}

	private static string Signature(IEnumerable<WorldChestPlacement> values)
	{
		return string.Join("|", values.Select((WorldChestPlacement value) => $"{value.InstanceId}:{value.SpawnPointId}:{value.ContentSeed}"));
	}
}
}
