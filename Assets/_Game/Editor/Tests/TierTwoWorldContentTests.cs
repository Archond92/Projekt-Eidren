using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class TierTwoWorldContentTests
{
	private static readonly string[] Zones = new string[3] { "TwilightGrove", "VeilMarsh", "GreyRifts" };

	[Test]
	public void TierTwoZones_HaveExactResourceBudgetAndNoLegacyEconomyNodes()
	{
		int[][] expected = new int[3][]
		{
			new int[4] { 45, 18, 6, 3 },
			new int[4] { 18, 45, 3, 6 },
			new int[4] { 6, 3, 45, 18 }
		};
		string[] ids = new string[4] { "resource.hardwood_tree", "resource.swamp_hemp", "resource.granite_deposit", "resource.iron_vein" };
		for (int zoneIndex = 0; zoneIndex < Zones.Length; zoneIndex++)
		{
			ZoneDefinition zone = LoadZone(Zones[zoneIndex]);
			Assert.That<int>(zone.ResourceAllocations.Sum((ZoneResourceAllocation value) => value.Count), (IResolveConstraint)(object)Is.EqualTo((object)72), Zones[zoneIndex], Array.Empty<object>());
			int idIndex;
			for (idIndex = 0; idIndex < ids.Length; idIndex++)
			{
				Assert.That<int>(zone.ResourceAllocations.Single((ZoneResourceAllocation value) => value.Definition.Id == ids[idIndex]).Count, (IResolveConstraint)(object)Is.EqualTo((object)expected[zoneIndex][idIndex]), ids[idIndex], Array.Empty<object>());
			}
			Assert.That<IReadOnlyList<ZoneResourceAllocation>>(zone.SideNodeAllocations, (IResolveConstraint)(object)Has.Exactly(1).Matches<ZoneResourceAllocation>((Predicate<ZoneResourceAllocation>)((ZoneResourceAllocation value) => value.Definition.Id == "resource.berry_bush" && value.Count > 0)));
		}
	}

	[Test]
	public void TierTwoResourceDefinitions_RequireCopperToolsAndHaveAuthoredStates()
	{
		(string, string)[] array = new(string, string)[4]
		{
			("HardwoodTree", "copper_axe"),
			("SwampHemp", "copper_scythe"),
			("GraniteDeposit", "copper_pickaxe"),
			("IronVein", "copper_pickaxe")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string) tuple = array[i];
			string asset = tuple.Item1;
			string tool = tuple.Item2;
			ResourceNodeDefinition resourceNodeDefinition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>("Assets/_Game/Data/Resources/" + asset + ".asset");
			Assert.That<ResourceNodeDefinition>(resourceNodeDefinition, (IResolveConstraint)(object)Is.Not.Null, asset, Array.Empty<object>());
			Assert.That<int>(resourceNodeDefinition.OutputItem.Tier, (IResolveConstraint)(object)Is.EqualTo((object)2), asset, Array.Empty<object>());
			Assert.That<string>(resourceNodeDefinition.RequiredToolItemId, (IResolveConstraint)(object)Is.EqualTo((object)tool), asset, Array.Empty<object>());
			Assert.That<GameObject>(resourceNodeDefinition.NodePrefab, (IResolveConstraint)(object)Is.Not.Null, asset, Array.Empty<object>());
			Assert.That<GameObject>(resourceNodeDefinition.ActiveVisualPrefab, (IResolveConstraint)(object)Is.Not.Null, asset, Array.Empty<object>());
			Assert.That<GameObject>(resourceNodeDefinition.ExhaustedVisualPrefab, (IResolveConstraint)(object)Is.Not.Null, asset, Array.Empty<object>());
			Assert.That<string[]>(resourceNodeDefinition.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty, asset, Array.Empty<object>());
		}
	}

	[Test]
	public void TierTwoEnemies_HaveExactFixedStatsAndAuthoredPrefabs()
	{
		(string, string, float, float, float, float, int)[] array = new(string, string, float, float, float, float, int)[5]
		{
			("Riftling", "enemy.riftling", 220f, 80f, 20f, 0f, 25),
			("RootCharger", "enemy.root_charger", 380f, 140f, 32f, 0f, 45),
			("MoorThrower", "enemy.moor_thrower", 200f, 70f, 24f, 0f, 30),
			("GraniteShell", "enemy.granite_shell", 450f, 180f, 30f, 0.2f, 65),
			("RiftGuardian", "enemy.rift_guardian", 850f, 260f, 30f, 0.15f, 180)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, float, float, float, float, int) expected = array[i];
			EnemyDefinition enemyDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/" + expected.Item1 + ".asset");
			Assert.That<EnemyDefinition>(enemyDefinition, (IResolveConstraint)(object)Is.Not.Null, expected.Item1, Array.Empty<object>());
			Assert.That<string>(enemyDefinition.Id, (IResolveConstraint)(object)Is.EqualTo((object)expected.Item2));
			Assert.That<float>(enemyDefinition.MaximumHealth, (IResolveConstraint)(object)Is.EqualTo((object)expected.Item3));
			Assert.That<float>(enemyDefinition.MaximumStagger, (IResolveConstraint)(object)Is.EqualTo((object)expected.Item4));
			Assert.That<float>(enemyDefinition.AttackDamage, (IResolveConstraint)(object)Is.EqualTo((object)expected.Item5));
			Assert.That<float>(enemyDefinition.Protection, (IResolveConstraint)(object)Is.EqualTo((object)expected.Item6).Within((object)0.001f));
			Assert.That<int>(enemyDefinition.ExperienceReward, (IResolveConstraint)(object)Is.EqualTo((object)expected.Item7));
			Assert.That<GameObject>(enemyDefinition.Prefab, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<string[]>(enemyDefinition.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty);
		}
		EnemyDefinition enemyDefinition2 = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/RiftGuardian.asset");
		Assert.That<float>(enemyDefinition2.SecondaryAttackDamage, (IResolveConstraint)(object)Is.EqualTo((object)40f));
		Assert.That<float>(enemyDefinition2.StaggerDuration, (IResolveConstraint)(object)Is.EqualTo((object)3.5f));
		Assert.That<float>(enemyDefinition2.PostStaggerResistanceDuration, (IResolveConstraint)(object)Is.EqualTo((object)3f));
		Assert.That<float>(enemyDefinition2.PostStaggerDamageMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)0.5f));
	}

	[Test]
	public void TierTwoMapNodes_AreVisibleButRequireExplicitTierTwoFlag()
	{
		WorldMapDefinition map = AssetDatabase.LoadAssetAtPath<WorldMapDefinition>("Assets/_Game/Data/WorldMap/Maps/WorldMap_RegionalV01.asset");
		string[] array = new string[3] { "zone_twilight_grove", "zone_veil_marsh", "zone_grey_rifts" };
		foreach (string id in array)
		{
			Assert.That<bool>(map.TryGetNode(id, out var node), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(node.InitiallyAvailable, (IResolveConstraint)(object)Is.False);
			Assert.That<string>(node.SceneKey, (IResolveConstraint)(object)Is.Not.Empty);
		}
	}

	private static ZoneDefinition LoadZone(string name)
	{
		return AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/Zone_" + name + ".asset");
	}
}
}
