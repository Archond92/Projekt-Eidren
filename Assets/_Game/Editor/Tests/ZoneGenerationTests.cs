using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eidren.Tests.EditMode
{
public sealed class ZoneGenerationTests
{
	private sealed class OpenSpace : IZonePlacementSpace
	{
		public bool IsPlaceable(Vector3 position, float clearance)
		{
			return true;
		}
	}

	private static readonly string[] OutdoorZoneAssets = new string[4] { "Zone_Greenwood", "Zone_Marsh", "Zone_Quarry", "Zone_EmberRuins" };

	private static readonly string[] WorldChestZoneAssets = new string[7] { "Zone_Greenwood", "Zone_Marsh", "Zone_Quarry", "Zone_EmberRuins", "Zone_GreyRifts", "Zone_TwilightGrove", "Zone_VeilMarsh" };

	private static readonly int[] ExpectedMixture = new int[4] { 45, 18, 6, 3 };

	private const int EconomyNodesPerZone = 72;

	private const float WorldChestClearance = 3.6f;

	private const float WorldChestMinimumClearance = 2.5f;

	[Test]
	public void EveryOutdoorZone_Carries72EconomyNodesInTheM17Mixture()
	{
		string[] outdoorZoneAssets = OutdoorZoneAssets;
		foreach (string assetName in outdoorZoneAssets)
		{
			ZoneDefinition zoneDefinition = Zone(assetName);
			int[] array = (from allocation in zoneDefinition.ResourceAllocations
				select allocation.Count into count
				orderby count descending
				select count).ToArray();
			Assert.That<int>(array.Sum(), (IResolveConstraint)(object)Is.EqualTo((object)72), assetName + ": M1.7 verlangt genau " + $"{72} Wirtschaftsknoten.", Array.Empty<object>());
			Assert.That<int[]>(array, (IResolveConstraint)(object)Is.EqualTo((object)ExpectedMixture), assetName + ": Das Muster 45/18/6/3 ist beim Balancing eine Zahl statt sechzehn.", Array.Empty<object>());
			Assert.That<int>(zoneDefinition.ResourceAllocations.Select((ZoneResourceAllocation allocation) => allocation.Definition.Id).Distinct(StringComparer.Ordinal).Count(), (IResolveConstraint)(object)Is.EqualTo((object)4), assetName + ": vier Materialien, jedes genau einmal.", Array.Empty<object>());
		}
	}

	[Test]
	public void EveryMaterial_HasExactlyOneBestSourceWithinM17Ratio()
	{
		Dictionary<string, List<int>> perMaterial = new Dictionary<string, List<int>>(StringComparer.Ordinal);
		string[] outdoorZoneAssets = OutdoorZoneAssets;
		for (int i = 0; i < outdoorZoneAssets.Length; i++)
		{
			foreach (ZoneResourceAllocation allocation in Zone(outdoorZoneAssets[i]).ResourceAllocations)
			{
				string id = allocation.Definition.Id;
				if (!perMaterial.TryGetValue(id, out var counts))
				{
					counts = new List<int>();
					perMaterial.Add(id, counts);
				}
				counts.Add(allocation.Count);
			}
		}
		Assert.That<Dictionary<string, List<int>>>(perMaterial, (IResolveConstraint)(object)((ConstraintExpression)Has.Count).EqualTo((object)4));
		foreach (KeyValuePair<string, List<int>> material in perMaterial)
		{
			int[] sorted = material.Value.OrderByDescending((int count) => count).ToArray();
			Assert.That<int[]>(sorted, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)4), material.Key, Array.Empty<object>());
			Assert.That<float>((float)sorted[0] / (float)sorted[1], (IResolveConstraint)(object)Is.InRange((IComparable)2f, (IComparable)3f), material.Key + ": Abstand zur zweitbesten Quelle.", Array.Empty<object>());
		}
	}

	[Test]
	public void BerryBushes_StandInAllFourZonesOutsideTheBudget()
	{
		int greenwood = 0;
		string[] outdoorZoneAssets = OutdoorZoneAssets;
		foreach (string assetName in outdoorZoneAssets)
		{
			ZoneDefinition zoneDefinition = Zone(assetName);
			int bushes = zoneDefinition.SideNodeAllocations.Where((ZoneResourceAllocation allocation) => allocation.Definition != null && allocation.Definition.Id == "resource.berry_bush").Sum((ZoneResourceAllocation allocation) => allocation.Count);
			Assert.That<int>(bushes, (IResolveConstraint)(object)Is.GreaterThan((object)0), assetName, Array.Empty<object>());
			Assert.That<bool>(zoneDefinition.ResourceAllocations.Any((ZoneResourceAllocation allocation) => allocation.Definition != null && allocation.Definition.Id == "resource.berry_bush"), (IResolveConstraint)(object)Is.False, assetName + ": Nebenknoten zaehlen nicht in die 72.", Array.Empty<object>());
			if (assetName == "Zone_Greenwood")
			{
				greenwood = bushes;
			}
			else
			{
				Assert.That<int>(greenwood, (IResolveConstraint)(object)Is.GreaterThan((object)bushes));
			}
		}
	}

	[Test]
	public void TwoRunsWithTheSameSeed_ProduceIdenticalMaps()
	{
		ZoneDefinition zone = Zone("Zone_Greenwood");
		ZoneLayout first = Generate(zone, 4711);
		ZoneLayout second = Generate(zone, 4711);
		Assert.That<int>(second.Placements.Count, (IResolveConstraint)(object)Is.EqualTo((object)first.Placements.Count));
		for (int index = 0; index < first.Placements.Count; index++)
		{
			ZoneNodePlacement left = first.Placements[index];
			ZoneNodePlacement right = second.Placements[index];
			Assert.That<string>(right.NodeInstanceId, (IResolveConstraint)(object)Is.EqualTo((object)left.NodeInstanceId));
			Assert.That<Vector3>(right.Position, (IResolveConstraint)(object)Is.EqualTo((object)left.Position));
			Assert.That<float>(right.RotationY, (IResolveConstraint)(object)Is.EqualTo((object)left.RotationY));
			Assert.That<bool>(right.CarriesWheatSeed, (IResolveConstraint)(object)Is.EqualTo((object)left.CarriesWheatSeed));
		}
	}

	[Test]
	public void ADifferentSeed_MovesTheNodesButNotTheirNumber()
	{
		ZoneDefinition zone = Zone("Zone_Greenwood");
		ZoneLayout first = Generate(zone, 4711);
		ZoneLayout second = Generate(zone, 1337);
		Assert.That<int>(second.EconomyNodeCount, (IResolveConstraint)(object)Is.EqualTo((object)first.EconomyNodeCount));
		Assert.That<int>(second.Placements.Count, (IResolveConstraint)(object)Is.EqualTo((object)first.Placements.Count));
		Assert.That<bool>(Enumerable.Range(0, first.Placements.Count).Any((int index) => first.Placements[index].Position != second.Placements[index].Position), (IResolveConstraint)(object)Is.True, "Ein anderer Seed muss eine andere Karte ergeben.", Array.Empty<object>());
	}

	[Test]
	public void EveryGeneratedZone_Carries72EconomyNodesAndItsSideNodes()
	{
		string[] outdoorZoneAssets = OutdoorZoneAssets;
		foreach (string assetName in outdoorZoneAssets)
		{
			ZoneDefinition zone = Zone(assetName);
			ZoneLayout layout = Generate(zone, 20250729);
			Assert.That<int>(layout.EconomyNodeCount, (IResolveConstraint)(object)Is.EqualTo((object)72), assetName, Array.Empty<object>());
			Assert.That<int>(layout.Placements.Count - layout.EconomyNodeCount, (IResolveConstraint)(object)Is.EqualTo((object)zone.SideNodeAllocations.Sum((ZoneResourceAllocation allocation) => allocation.Count)), assetName + ": Nebenknoten stehen zusaetzlich.", Array.Empty<object>());
			Assert.That<int>(layout.Placements.Select((ZoneNodePlacement placement) => placement.NodeInstanceId).Distinct(StringComparer.Ordinal).Count(), (IResolveConstraint)(object)Is.EqualTo((object)layout.Placements.Count), assetName + ": Knoten-IDs sind Save-Keys (§12).", Array.Empty<object>());
		}
	}

	[Test]
	public void EveryThirdFiberNode_CarriesAWheatSeed()
	{
		string[] outdoorZoneAssets = OutdoorZoneAssets;
		foreach (string assetName in outdoorZoneAssets)
		{
			ZoneDefinition zoneDefinition = Zone(assetName);
			int fiberNodes = zoneDefinition.ResourceAllocations.Where((ZoneResourceAllocation allocation) => allocation.Definition.Id == "resource.fiber_plant").Sum((ZoneResourceAllocation allocation) => allocation.Count);
			ZoneLayout zoneLayout = Generate(zoneDefinition, 99);
			Assert.That<int>(zoneLayout.WheatSeedNodeCount, (IResolveConstraint)(object)Is.EqualTo((object)(fiberNodes / 3)), assetName, Array.Empty<object>());
			Assert.That<bool>(zoneLayout.Placements.Where((ZoneNodePlacement placement) => placement.CarriesWheatSeed).All((ZoneNodePlacement placement) => placement.Definition.Id == "resource.fiber_plant"), (IResolveConstraint)(object)Is.True, assetName + ": Samen kommen nur aus Faserknoten.", Array.Empty<object>());
		}
	}

	[Test]
	public void NoNodeStandsInAWorldChestsReach()
	{
		int[] seeds = new int[5] { 4711, 1337, 20250729, 99, 918273 };
		List<string> offenders = new List<string>();
		string[] worldChestZoneAssets = WorldChestZoneAssets;
		foreach (string assetName in worldChestZoneAssets)
		{
			ZoneDefinition zone = Zone(assetName);
			Assert.That<int>(zone.WorldChestSpawnPoints.Count, (IResolveConstraint)(object)Is.GreaterThan((object)0), assetName + ": Die Zone soll authorierte Truhenpunkte fuehren.", Array.Empty<object>());
			int[] array = seeds;
			foreach (int seed in array)
			{
				ZoneLayout layout = Generate(zone, seed);
				foreach (ZoneNodePlacement placement in layout.Placements)
				{
					CollectChestOverlaps(zone, assetName, seed, placement.NodeInstanceId, placement.Position, offenders);
				}
				foreach (ZoneEidraPlacement placement2 in layout.EidraPlacements)
				{
					CollectChestOverlaps(zone, assetName, seed, placement2.InstanceId, placement2.Position, offenders);
				}
			}
		}
		Assert.That<List<string>>(offenders, (IResolveConstraint)(object)Is.Empty, $"Kein Knoten darf naeher als {WorldChestClearance:0.00} m an einem authorierten Truhenpunkt stehen: der Interaktionsradius der Truhe (2,50 m) und ihr Waechterkranz (3,20 m) muessen frei bleiben.\n" + string.Join("\n", offenders), Array.Empty<object>());
	}

	[Test]
	public void AZoneTooTightForTheFullClearance_GivesItUpInsteadOfThrowing()
	{
		Bounds area = new Bounds(Vector3.zero, new Vector3(5f, 0f, 5f));
		float farthestCorner = new Vector2(area.extents.x, area.extents.z).magnitude;
		Assert.That<float>(farthestCorner, (IResolveConstraint)(object)Is.LessThan((object)WorldChestClearance), $"Aufbau des Tests: keine einzige Stelle der Flaeche darf den vollen Truhenfreiraum von {WorldChestClearance:0.00} m erfuellen, sonst beweist der Test nichts.", Array.Empty<object>());
		ZoneDefinition zone = SyntheticZoneWithOneChestAtTheCentre(3);
		ZoneLayout layout = ZoneLayoutGenerator.Generate(zone, new ZoneState(zone.Id, 4711, 2), area, new OpenSpace());
		Assert.That<int>(layout.Placements.Count, (IResolveConstraint)(object)Is.EqualTo((object)3), "Die Zone muss ihr Knotenbudget halten, statt an der InvalidOperationException zu zerbrechen.", Array.Empty<object>());
		foreach (ZoneNodePlacement placement in layout.Placements)
		{
			float distance = new Vector2(placement.Position.x, placement.Position.z).magnitude;
			Assert.That<float>(distance, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)WorldChestMinimumClearance), $"{placement.NodeInstanceId}: Der Freiraum darf im Notfall bis auf den Interaktionsradius von {WorldChestMinimumClearance:0.00} m nachgeben, aber keinen Zentimeter weiter.", Array.Empty<object>());
		}
	}

	private static ZoneDefinition SyntheticZoneWithOneChestAtTheCentre(int nodeCount)
	{
		ResourceNodeDefinition node = ScriptableObject.CreateInstance<ResourceNodeDefinition>();
		SerializedObject serializedNode = new SerializedObject(node);
		serializedNode.FindProperty("id").stringValue = "resource.test_node";
		serializedNode.ApplyModifiedPropertiesWithoutUndo();
		ZoneDefinition zone = ScriptableObject.CreateInstance<ZoneDefinition>();
		SerializedObject serializedZone = new SerializedObject(zone);
		serializedZone.FindProperty("id").stringValue = "zone_tight";
		serializedZone.FindProperty("enemiesAllowed").boolValue = false;
		SerializedProperty allocations = serializedZone.FindProperty("resourceAllocations");
		allocations.arraySize = 1;
		allocations.GetArrayElementAtIndex(0).FindPropertyRelative("definition").objectReferenceValue = node;
		allocations.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue = nodeCount;
		SerializedProperty points = serializedZone.FindProperty("worldChestSpawnPoints");
		points.arraySize = 1;
		SerializedProperty point = points.GetArrayElementAtIndex(0);
		point.FindPropertyRelative("stableId").stringValue = "common_01";
		point.FindPropertyRelative("position").vector3Value = Vector3.zero;
		point.FindPropertyRelative("allowedFamilies").intValue = 1;
		serializedZone.ApplyModifiedPropertiesWithoutUndo();
		return zone;
	}

	private static void CollectChestOverlaps(ZoneDefinition zone, string assetName, int seed, string instanceId, Vector3 position, List<string> offenders)
	{
		foreach (WorldChestSpawnPointDefinition point in zone.WorldChestSpawnPoints)
		{
			float distance = new Vector2(point.Position.x - position.x, point.Position.z - position.z).magnitude;
			if (distance < WorldChestClearance)
			{
				offenders.Add($"{assetName} (Seed {seed}): {instanceId} steht {distance:0.00} m von Truhenpunkt '{point.StableId}'.");
			}
		}
	}

	[Test]
	public void ZoneGeneration_UsesNoUnityRandom()
	{
		string[] obj = new string[5] { "Assets/_Game/Scripts/Core/World/ZoneLayoutGenerator.cs", "Assets/_Game/Scripts/Core/World/ZoneStateService.cs", "Assets/_Game/Scripts/Core/World/ZoneState.cs", "Assets/_Game/Scripts/Composition/ZoneResourcePopulator.cs", "Assets/_Game/Scripts/Composition/ZonePhysicsPlacementSpace.cs" };
		List<string> offenders = new List<string>();
		string[] array = obj;
		foreach (string path in array)
		{
			Assert.That<bool>(File.Exists(path), (IResolveConstraint)(object)Is.True, path, Array.Empty<object>());
			string[] lines = File.ReadAllLines(path);
			for (int index = 0; index < lines.Length; index++)
			{
				string line = lines[index];
				if (!line.TrimStart().StartsWith("//", StringComparison.Ordinal) && (line.Contains("UnityEngine.Random") || line.Contains("Random.InitState") || line.Contains("Random.Range") || line.Contains("Random.value") || line.Contains("Random.insideUnitCircle") || line.Contains("Random.state")))
				{
					offenders.Add($"{path}:{index + 1}");
				}
			}
		}
		Assert.That<List<string>>(offenders, (IResolveConstraint)(object)Is.Empty, "M1.2: Pro Zone eine eigene System.Random-Instanz aus dem Zonen-Seed:\n" + string.Join("\n", offenders), Array.Empty<object>());
	}

	[Test]
	public void ReenteringWithoutHomecoming_KeepsSeedAndHarvestedNodes()
	{
		ZoneStateService zoneStateService = new ZoneStateService();
		zoneStateService.Reset(1234);
		ZoneState orCreate = zoneStateService.GetOrCreate("zone_greenwood", 2);
		orCreate.MarkHarvested("zone_greenwood.resource.tree.007");
		int seed = orCreate.Seed;
		ZoneState orCreate2 = zoneStateService.GetOrCreate("zone_greenwood", 2);
		Assert.That<int>(orCreate2.Seed, (IResolveConstraint)(object)Is.EqualTo((object)seed));
		Assert.That<bool>(orCreate2.IsHarvested("zone_greenwood.resource.tree.007"), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(zoneStateService.Count, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void Homecoming_RerollsEveryOutdoorZoneButNotTheHomeBase()
	{
		ZoneStateService states = new ZoneStateService();
		states.Reset(1234);
		Dictionary<string, int> before = new Dictionary<string, int>(StringComparer.Ordinal);
		string[] array = new string[5] { "home_base", "zone_greenwood", "zone_marsh", "zone_quarry", "zone_ember_ruins" };
		foreach (string zoneId in array)
		{
			ZoneState state = states.GetOrCreate(zoneId, 2);
			state.MarkHarvested(zoneId + ".resource.tree.001");
			before.Add(zoneId, state.Seed);
		}
		Assert.That<int>(states.InvalidateAllForHomecoming(2), (IResolveConstraint)(object)Is.EqualTo((object)4), "M1.1: Heimkehr wuerfelt alle Aussengebiete neu.", Array.Empty<object>());
		foreach (KeyValuePair<string, int> entry in before)
		{
			Assert.That<bool>(states.TryGet(entry.Key, out var after), (IResolveConstraint)(object)Is.True);
			bool isHome = entry.Key == "home_base";
			Assert.That<bool>(after.Seed == entry.Value, (IResolveConstraint)(object)Is.EqualTo((object)isHome), entry.Key + ": Die Heimatbasis ist ausgenommen (M1.1).", Array.Empty<object>());
			Assert.That<int>(after.HarvestedCount, (IResolveConstraint)(object)Is.EqualTo((object)(isHome ? 1 : 0)), entry.Key, Array.Empty<object>());
		}
	}

	[Test]
	public void ANewGeneratorVersion_RerollsTheZone()
	{
		ZoneStateService zoneStateService = new ZoneStateService();
		zoneStateService.Reset(77);
		zoneStateService.GetOrCreate("zone_marsh", 1).MarkHarvested("zone_marsh.resource.fiber_plant.003");
		ZoneState orCreate = zoneStateService.GetOrCreate("zone_marsh", 2);
		Assert.That<int>(orCreate.GeneratorVersion, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(orCreate.HarvestedCount, (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void SaveState_HoldsSeedsInsteadOfLayoutAndStaysBounded()
	{
		ZoneStateService states = new ZoneStateService();
		states.Reset(4242);
		string[] array = new string[4] { "zone_greenwood", "zone_marsh", "zone_quarry", "zone_ember_ruins" };
		foreach (string zoneId in array)
		{
			for (int visit = 0; visit < 25; visit++)
			{
				states.GetOrCreate(zoneId, 2);
			}
		}
		ZoneState[] array2 = states.Export();
		Assert.That<ZoneState[]>(array2, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)4), "Hundert Besuche ergeben vier Eintraege -- die Zahl haengt an den Zonen, nicht an den Besuchen (M1.2).", Array.Empty<object>());
		ZoneState[] array3 = array2;
		foreach (ZoneState obj in array3)
		{
			Assert.That<int>(obj.HarvestedCount, (IResolveConstraint)(object)Is.Zero);
			Assert.That<int>(obj.GeneratorVersion, (IResolveConstraint)(object)Is.EqualTo((object)2));
		}
		Assert.That<bool>((from property in typeof(ZoneState).GetProperties()
			select property.PropertyType).Any((Type type) => type == typeof(Vector3) || type == typeof(Vector3[])), (IResolveConstraint)(object)Is.False, "M1.2: Das Layout wird abgeleitet, nicht gespeichert.", Array.Empty<object>());
	}

	private static ZoneLayout Generate(ZoneDefinition zone, int seed)
	{
		ZoneState state = new ZoneState(zone.Id, seed, 2);
		return ZoneLayoutGenerator.Generate(zone, state, new Bounds(Vector3.zero, new Vector3(68f, 0f, 68f)), new OpenSpace());
	}

	private static ZoneDefinition Zone(string assetName)
	{
		ZoneDefinition zoneDefinition = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/" + assetName + ".asset");
		Assert.That<ZoneDefinition>(zoneDefinition, (IResolveConstraint)(object)Is.Not.Null, assetName, Array.Empty<object>());
		return zoneDefinition;
	}
}
}
