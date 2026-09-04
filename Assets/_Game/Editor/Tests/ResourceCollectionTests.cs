using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
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
public sealed class ResourceCollectionTests
{
	private sealed class TestRuntime
	{
		private readonly GameObject _root;

		private readonly ContentDatabase _database;

		public PlayerInventory Inventory { get; }

		public InteractionContext Context { get; }

		public TestRuntime(ResourceNodeDefinition[] definitions, params ItemDefinition[] extraItems)
		{
			_root = new GameObject("ResourceCollectionTest");
			_database = _root.AddComponent<ContentDatabase>();
			List<ItemDefinition> items = (from value in definitions.Select((ResourceNodeDefinition value) => value.OutputItem).Concat(extraItems ?? Array.Empty<ItemDefinition>())
				where value != null
				select value).Distinct().ToList();
			_database.ConfigureItems(items.ToArray());
			Inventory = new PlayerInventory(_database);
			Context = new InteractionContext(_root, _root.transform, Vector3.forward, Inventory);
		}

		public ResourceNode Node(ResourceNodeDefinition definition)
		{
			GameObject gameObject = new GameObject("ResourceCollectionTest_" + definition.Id);
			gameObject.transform.SetParent(_root.transform);
			SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
			ResourceNode resourceNode = gameObject.AddComponent<ResourceNode>();
			resourceNode.Configure(definition, definition.Id + ".test", null, null, trigger, null);
			return resourceNode;
		}

		public void Dispose()
		{
			UnityEngine.Object.DestroyImmediate(_root);
		}
	}

	private static readonly string[] DefinitionPaths = new string[5] { "Assets/_Game/Data/Resources/Tree.asset", "Assets/_Game/Data/Resources/StoneDeposit.asset", "Assets/_Game/Data/Resources/FiberPlant.asset", "Assets/_Game/Data/Resources/CopperVein.asset", "Assets/_Game/Data/Resources/BerryBush.asset" };

	private const string YieldTablePath = "Assets/_Game/Data/Resources/HarvestYields_V01.asset";

	[Test]
	public void DefinitionsHaveRequiredOutputsDurationsAndYields()
	{
		ResourceNodeDefinition[] values = Definitions();
		AssertDefinition(values, "resource.tree", "wood", 1.8f);
		AssertDefinition(values, "resource.stone_deposit", "stone", 2f);
		AssertDefinition(values, "resource.fiber_plant", "plant_fiber", 0.9f);
		AssertDefinition(values, "resource.copper_vein", "copper_ore", 2.25f);
		AssertDefinition(values, "resource.berry_bush", "berry", 0.9f);
		Assert.That<int>(values.Select((ResourceNodeDefinition resourceNodeDefinition) => resourceNodeDefinition.Id).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)DefinitionPaths.Length));
		Assert.That<bool>(values.All((ResourceNodeDefinition resourceNodeDefinition) => resourceNodeDefinition.RespawnMode == ResourceRespawnMode.OnZoneEntry), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(values.All((ResourceNodeDefinition resourceNodeDefinition) => resourceNodeDefinition.GetValidationErrors().Length == 0), (IResolveConstraint)(object)Is.True);
		GameObject databaseObject = new GameObject("ResourceCollectionTest_Catalog");
		try
		{
			ContentDatabase database = databaseObject.AddComponent<ContentDatabase>();
			ResourceNodeDefinition[] array = values;
			foreach (ResourceNodeDefinition value in array)
			{
				Assert.That<ResourceNodeDefinition>(database.GetResourceNode(value.Id), (IResolveConstraint)(object)Is.SameAs((object)value));
			}
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(databaseObject);
		}
	}

	[TestCase("Zone_Greenwood")]
	[TestCase("Zone_Quarry")]
	[TestCase("Zone_Marsh")]
	[TestCase("Zone_EmberRuins")]
	public void EveryOutdoorZone_CanBeHarvestedByHand(string zoneName)
	{
		ZoneDefinition zone = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/" + zoneName + ".asset");
		Assert.That<ZoneDefinition>(zone, (IResolveConstraint)(object)Is.Not.Null, zoneName, Array.Empty<object>());
		ZoneResourceAllocation[] array = zone.ResourceAllocations.Concat(zone.SideNodeAllocations).ToArray();
		Assert.That<ZoneResourceAllocation[]>(array, (IResolveConstraint)(object)Is.Not.Empty, zoneName, Array.Empty<object>());
		int byHand = 0;
		ZoneResourceAllocation[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			ZoneResourceAllocation allocation = array2[i];
			ResourceNodeDefinition definition = allocation.Definition;
			Assert.That<ResourceNodeDefinition>(definition, (IResolveConstraint)(object)Is.Not.Null, zoneName, Array.Empty<object>());
			Assert.That<HarvestYieldTable>(definition.HarvestYields, (IResolveConstraint)(object)Is.Not.Null, definition.Id, Array.Empty<object>());
			HarvestOutcome outcome = new HarvestYieldResolver(definition.HarvestYields).Resolve(definition, null);
			if (definition.RequiresTool)
			{
				Assert.That<string>(definition.Id, (IResolveConstraint)(object)Is.EqualTo((object)"resource.copper_vein"), zoneName + "/" + definition.Id + ": Nur Kupfererz ist gesperrt (M1.9).", Array.Empty<object>());
				Assert.That<bool>(outcome.IsAllowed, (IResolveConstraint)(object)Is.False, definition.Id, Array.Empty<object>());
			}
			else
			{
				Assert.That<bool>(outcome.IsAllowed, (IResolveConstraint)(object)Is.True, definition.Id, Array.Empty<object>());
				Assert.That<int>(outcome.Amount, (IResolveConstraint)(object)Is.EqualTo((object)1), definition.Id, Array.Empty<object>());
				byHand += allocation.Count;
			}
		}
		Assert.That<int>(byHand, (IResolveConstraint)(object)Is.GreaterThan((object)0), zoneName + " traegt keinen einzigen Knoten, der ohne Werkzeug abbaubar waere.", Array.Empty<object>());
	}

	[Test]
	public void NavigationBlockingMatchesResourceType()
	{
		ResourceNodeDefinition[] array = Definitions();
		foreach (ResourceNodeDefinition definition in array)
		{
			GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(NodePrefabPath(definition.Id));
			Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null);
			ResourceNode node = gameObject.GetComponent<ResourceNode>();
			Assert.That<ResourceNode>(node, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<Collider>(node.InteractionTrigger, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<bool>(node.InteractionTrigger.isTrigger, (IResolveConstraint)(object)Is.True);
			if (!definition.BlocksNavigation)
			{
				Assert.That<Collider>(node.BlockingCollider, (IResolveConstraint)(object)Is.Null, definition.Id, Array.Empty<object>());
				continue;
			}
			Assert.That<Collider>(node.BlockingCollider, (IResolveConstraint)(object)Is.Not.Null, definition.Id, Array.Empty<object>());
			Assert.That<bool>(node.BlockingCollider.isTrigger, (IResolveConstraint)(object)Is.False);
		}
	}

	[Test]
	public void EachTypeCollectsItsResolvedYieldExactlyOnce()
	{
		ResourceNodeDefinition[] definitions = Definitions();
		TestRuntime runtime = new TestRuntime(definitions, Item("Axe"), Item("Scythe"), Item("Pickaxe"));
		try
		{
			runtime.Inventory.Add("axe", 1);
			runtime.Inventory.Add("scythe", 1);
			runtime.Inventory.Add("pickaxe", 1);
			ResourceNodeDefinition[] array = definitions;
			foreach (ResourceNodeDefinition definition in array)
			{
				int expected = ((definition.Id == "resource.berry_bush") ? 1 : 2);
				ResourceNode resourceNode = runtime.Node(definition);
				Assert.That<int>(resourceNode.ResolveYield(runtime.Context), (IResolveConstraint)(object)Is.EqualTo((object)expected), definition.Id, Array.Empty<object>());
				int before = runtime.Inventory.GetTotalAmount(definition.OutputItem.Id);
				resourceNode.CompleteInteraction(runtime.Context);
				resourceNode.CompleteInteraction(runtime.Context);
				Assert.That<int>(runtime.Inventory.GetTotalAmount(definition.OutputItem.Id) - before, (IResolveConstraint)(object)Is.EqualTo((object)expected), definition.Id, Array.Empty<object>());
				Assert.That<int>(resourceNode.CollectedAmount, (IResolveConstraint)(object)Is.EqualTo((object)expected), definition.Id, Array.Empty<object>());
				Assert.That<bool>(resourceNode.IsExhausted, (IResolveConstraint)(object)Is.True, definition.Id, Array.Empty<object>());
			}
		}
		finally
		{
			runtime.Dispose();
		}
	}

	[Test]
	public void WoodNode_YieldsOneByHandAndTwoWithTheAxe()
	{
		ResourceNodeDefinition[] array = Definitions();
		ResourceNodeDefinition tree = array.Single((ResourceNodeDefinition value) => value.Id == "resource.tree");
		TestRuntime runtime = new TestRuntime(array, Item("Axe"));
		try
		{
			ResourceNode node = runtime.Node(tree);
			Assert.That<int>(node.ResolveYield(runtime.Context), (IResolveConstraint)(object)Is.EqualTo((object)1));
			Assert.That<int>(Harvest(runtime, node, tree), (IResolveConstraint)(object)Is.EqualTo((object)1));
			node.ResetForZoneEntry();
			Assert.That<int>(Harvest(runtime, node, tree), (IResolveConstraint)(object)Is.EqualTo((object)1), "M1.2: derselbe Knoten, derselbe Ertrag.", Array.Empty<object>());
			runtime.Inventory.Add("axe", 1);
			node.ResetForZoneEntry();
			Assert.That<int>(node.ResolveYield(runtime.Context), (IResolveConstraint)(object)Is.EqualTo((object)2));
			Assert.That<int>(Harvest(runtime, node, tree), (IResolveConstraint)(object)Is.EqualTo((object)2));
			node.ResetForZoneEntry();
			Assert.That<int>(Harvest(runtime, node, tree), (IResolveConstraint)(object)Is.EqualTo((object)2), "M1.2: derselbe Knoten, derselbe Ertrag.", Array.Empty<object>());
		}
		finally
		{
			runtime.Dispose();
		}
	}

	[Test]
	public void CompletedNode_WearsToolAndBreaksOnlyAfterAward()
	{
		ResourceNodeDefinition[] array = Definitions();
		ResourceNodeDefinition tree = array.Single((ResourceNodeDefinition value) => value.Id == "resource.tree");
		TestRuntime runtime = new TestRuntime(array, Item("Axe"));
		try
		{
			runtime.Inventory.Add("axe", 1);
			ItemStack[] slots = runtime.Inventory.ExportSlots();
			int axeSlot = Array.FindIndex(slots, (ItemStack value) => value.ItemId == "axe");
			Assert.That<int>(axeSlot, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)0));
			slots[axeSlot] = new ItemStack("axe", 1, slots[axeSlot].InstanceId, 3);
			Assert.That<bool>(runtime.Inventory.TryImportSlots(slots, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
			ResourceNode node = runtime.Node(tree);
			Assert.That<bool>(node.CanInteract(runtime.Context, out var _), (IResolveConstraint)(object)Is.True);
			Assert.That<string>(node.DisplayText, (IResolveConstraint)(object)Does.Contain("Belastung normal"));
			Assert.That<int>(Harvest(runtime, node, tree), (IResolveConstraint)(object)Is.EqualTo((object)2));
			Assert.That<bool>(runtime.Inventory.Contains("axe"), (IResolveConstraint)(object)Is.False, "T0 tool loses three durability after the node.", Array.Empty<object>());
			node.ResetForZoneEntry();
			Assert.That<int>(node.ResolveYield(runtime.Context), (IResolveConstraint)(object)Is.EqualTo((object)1), "The broken tool is gone before the next node.", Array.Empty<object>());
		}
		finally
		{
			runtime.Dispose();
		}
	}

	[Test]
	public void CancelledNode_DoesNotWearTool()
	{
		ResourceNodeDefinition[] array = Definitions();
		ResourceNodeDefinition tree = array.Single((ResourceNodeDefinition value) => value.Id == "resource.tree");
		TestRuntime runtime = new TestRuntime(array, Item("Axe"));
		try
		{
			runtime.Inventory.Add("axe", 1);
			ResourceNode resourceNode = runtime.Node(tree);
			ItemStack before = runtime.Inventory.ExportSlots().Single((ItemStack value) => value.ItemId == "axe");
			resourceNode.BeginInteraction(runtime.Context);
			resourceNode.UpdateInteraction(runtime.Context, 0.8f);
			resourceNode.CancelInteraction(runtime.Context, InteractionCancelReason.InputReleased);
			Assert.That<int>(runtime.Inventory.ExportSlots().Single((ItemStack value) => value.ItemId == "axe").Durability, (IResolveConstraint)(object)Is.EqualTo((object)before.Durability));
			Assert.That<bool>(resourceNode.IsExhausted, (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			runtime.Dispose();
		}
	}

	[Test]
	public void HarvestYieldTable_FollowsTheToolAssignmentOfM19()
	{
		HarvestYieldTable table = AssetDatabase.LoadAssetAtPath<HarvestYieldTable>("Assets/_Game/Data/Resources/HarvestYields_V01.asset");
		Assert.That<HarvestYieldTable>(table, (IResolveConstraint)(object)Is.Not.Null, "Run Eidren/Data/Build Resource Definitions V0.1.", Array.Empty<object>());
		Assert.That<string[]>(table.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty);
		Assert.That<int>(table.HandYield, (IResolveConstraint)(object)Is.EqualTo((object)1));
		AssertToolFor(table, "wood", "axe");
		AssertToolFor(table, "fiber", "scythe");
		AssertToolFor(table, "stone", "pickaxe");
		AssertToolFor(table, "ore", "pickaxe");
		Assert.That<int>(table.ToolYields.Count, (IResolveConstraint)(object)Is.EqualTo((object)9), "Drei Werkzeugfamilien decken T0 bis T2 ab.", Array.Empty<object>());
		ResourceNodeDefinition[] array = Definitions();
		foreach (ResourceNodeDefinition definition in array)
		{
			Assert.That<HarvestYieldTable>(definition.HarvestYields, (IResolveConstraint)(object)Is.SameAs((object)table), definition.Id, Array.Empty<object>());
		}
	}

	[Test]
	public void Tools_LoseThreeDurabilityPerEqualTierNode()
	{
		ResourceNodeDefinition[] array = Definitions();
		ResourceNodeDefinition tree = array.Single((ResourceNodeDefinition value) => value.Id == "resource.tree");
		ItemDefinition axe = Item("Axe");
		TestRuntime runtime = new TestRuntime(array, axe);
		try
		{
			runtime.Inventory.Reset(new ItemStack[1] { ItemStack.Create(axe, 1, "axe.test", 50) });
			for (int round = 0; round < 4; round++)
			{
				ResourceNode node = runtime.Node(tree);
				Assert.That<int>(Harvest(runtime, node, tree), (IResolveConstraint)(object)Is.EqualTo((object)2));
			}
			Assert.That<int>(runtime.Inventory.GetTotalAmount("axe"), (IResolveConstraint)(object)Is.EqualTo((object)1));
			Assert.That<bool>(runtime.Inventory.TryGetSlot(0, out var stack), (IResolveConstraint)(object)Is.True);
			Assert.That<string>(stack.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"axe"));
			Assert.That<int>(stack.Durability, (IResolveConstraint)(object)Is.EqualTo((object)38));
		}
		finally
		{
			runtime.Dispose();
		}
	}

	private static void AssertToolFor(HarvestYieldTable table, string materialFamily, string expectedToolItemId)
	{
		Assert.That<bool>(table.TryGetToolForFamily(materialFamily, out var entry), (IResolveConstraint)(object)Is.True, materialFamily, Array.Empty<object>());
		Assert.That<string>(entry.Tool.Id, (IResolveConstraint)(object)Is.EqualTo((object)expectedToolItemId), materialFamily, Array.Empty<object>());
		Assert.That<int>(entry.Yield, (IResolveConstraint)(object)Is.EqualTo((object)2), materialFamily, Array.Empty<object>());
	}

	[Test]
	public void FullInventoryBlocksCollectionWithoutExhaustingNode()
	{
		ResourceNodeDefinition[] definitions = Definitions();
		ItemDefinition filler = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/Axe.asset");
		TestRuntime runtime = new TestRuntime(definitions, filler);
		try
		{
			Assert.That<int>(runtime.Inventory.Add(filler.Id, 16), (IResolveConstraint)(object)Is.Zero);
			ResourceNode resourceNode = runtime.Node(definitions[0]);
			Assert.That<bool>(resourceNode.CanInteract(runtime.Context, out var reason), (IResolveConstraint)(object)Is.False);
			Assert.That<string>(reason, (IResolveConstraint)(object)Is.EqualTo((object)"INVENTAR VOLL"));
			resourceNode.CompleteInteraction(runtime.Context);
			Assert.That<bool>(resourceNode.IsExhausted, (IResolveConstraint)(object)Is.False);
			Assert.That<int>(runtime.Inventory.GetTotalAmount(definitions[0].OutputItem.Id), (IResolveConstraint)(object)Is.Zero);
		}
		finally
		{
			runtime.Dispose();
		}
	}

	[Test]
	public void ZoneEntryResetRestoresExhaustedNode()
	{
		ResourceNodeDefinition definition = Definitions()[0];
		TestRuntime runtime = new TestRuntime(new ResourceNodeDefinition[1] { definition });
		try
		{
			ResourceNode resourceNode = runtime.Node(definition);
			resourceNode.CompleteInteraction(runtime.Context);
			Assert.That<bool>(resourceNode.IsExhausted, (IResolveConstraint)(object)Is.True);
			resourceNode.ResetForZoneEntry();
			Assert.That<bool>(resourceNode.IsExhausted, (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(resourceNode.InteractionTrigger.enabled, (IResolveConstraint)(object)Is.True);
		}
		finally
		{
			runtime.Dispose();
		}
	}

	[Test]
	public void GatedNodes_NameTheRequiredProgressionTool()
	{
		ResourceNodeDefinition[] array = Definitions();
		foreach (ResourceNodeDefinition value in array)
		{
			string expected = value.Id switch
			{
				"resource.copper_vein" => "pickaxe", 
				"resource.hardwood_tree" => "copper_axe", 
				"resource.swamp_hemp" => "copper_scythe", 
				"resource.granite_deposit" => "copper_pickaxe", 
				"resource.iron_vein" => "copper_pickaxe", 
				_ => string.Empty, 
			};
			Assert.That<bool>(value.RequiresTool, (IResolveConstraint)(object)Is.EqualTo((object)(!string.IsNullOrEmpty(expected))), value.Id, Array.Empty<object>());
			Assert.That<string>(value.RequiredToolItemId, (IResolveConstraint)(object)Is.EqualTo((object)expected), value.Id, Array.Empty<object>());
		}
	}

	[Test]
	public void CopperVein_IsBlockedWithoutAPickaxeAndYieldsTwoWithIt()
	{
		ResourceNodeDefinition[] array = Definitions();
		ResourceNodeDefinition copper = array.Single((ResourceNodeDefinition value) => value.Id == "resource.copper_vein");
		TestRuntime runtime = new TestRuntime(array, Item("Pickaxe"));
		try
		{
			ResourceNode node = runtime.Node(copper);
			Assert.That<bool>(node.CanInteract(runtime.Context, out var blockedReason), (IResolveConstraint)(object)Is.False);
			Assert.That<string>(blockedReason, (IResolveConstraint)(object)Is.EqualTo((object)"WERKZEUG UNZUREICHEND"));
			Assert.That<int>(node.ResolveYield(runtime.Context), (IResolveConstraint)(object)Is.Zero);
			Assert.That<int>(Harvest(runtime, node, copper), (IResolveConstraint)(object)Is.Zero);
			Assert.That<bool>(node.IsExhausted, (IResolveConstraint)(object)Is.False);
			runtime.Inventory.Add("pickaxe", 1);
			Assert.That<bool>(node.CanInteract(runtime.Context, out var _), (IResolveConstraint)(object)Is.True);
			Assert.That<int>(node.ResolveYield(runtime.Context), (IResolveConstraint)(object)Is.EqualTo((object)2));
			Assert.That<int>(Harvest(runtime, node, copper), (IResolveConstraint)(object)Is.EqualTo((object)2));
		}
		finally
		{
			runtime.Dispose();
		}
	}

	[Test]
	public void BerryBush_StaysOutsideTheZoneBudget()
	{
		string[] array = new string[4] { "Zone_Greenwood", "Zone_Quarry", "Zone_Marsh", "Zone_EmberRuins" };
		foreach (string assetName in array)
		{
			ZoneDefinition zoneDefinition = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/" + assetName + ".asset");
			Assert.That<ZoneDefinition>(zoneDefinition, (IResolveConstraint)(object)Is.Not.Null, assetName, Array.Empty<object>());
			Assert.That<bool>(zoneDefinition.ResourceAllocations.Any((ZoneResourceAllocation allocation) => allocation.Definition != null && allocation.Definition.Id == "resource.berry_bush"), (IResolveConstraint)(object)Is.False, assetName, Array.Empty<object>());
		}
	}

	[Test]
	public void ToolRequirement_NeedsAToolIdInsteadOfBeingBlocked()
	{
		ResourceNodeDefinition node = ScriptableObject.CreateInstance<ResourceNodeDefinition>();
		try
		{
			SetToolRequirement(node, requiresTool: true, string.Empty);
			string[] withoutId = node.GetValidationErrors();
			Assert.That<bool>(withoutId.Any((string error) => error.Contains("names no required tool")), (IResolveConstraint)(object)Is.True, string.Join("; ", withoutId), Array.Empty<object>());
			SetToolRequirement(node, requiresTool: true, "placeholder_tool");
			string[] withId = node.GetValidationErrors();
			Assert.That<bool>(withId.Any((string error) => error.Contains("required tool")), (IResolveConstraint)(object)Is.False, string.Join("; ", withId), Array.Empty<object>());
			Assert.That<bool>(withId.Any((string error) => error.Contains("disabled in V0.1")), (IResolveConstraint)(object)Is.False, string.Join("; ", withId), Array.Empty<object>());
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(node);
		}
	}

	private static void SetToolRequirement(ResourceNodeDefinition node, bool requiresTool, string requiredToolItemId)
	{
		SerializedObject serializedObject = new SerializedObject(node);
		serializedObject.FindProperty("requiresTool").boolValue = requiresTool;
		serializedObject.FindProperty("requiredToolItemId").stringValue = requiredToolItemId;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
	}

	private static ResourceNodeDefinition[] Definitions()
	{
		ResourceNodeDefinition[] array = DefinitionPaths.Select((string path) => AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(path)).ToArray();
		Assert.That<bool>(array.All((ResourceNodeDefinition value) => value != null), (IResolveConstraint)(object)Is.True, "Run Eidren/Data/Build Resource Collection V0.1.", Array.Empty<object>());
		return array;
	}

	private static string NodePrefabPath(string resourceId)
	{
		return "Assets/_Game/Prefabs/Resources/Nodes/" + resourceId switch
		{
			"resource.tree" => "Tree", 
			"resource.stone_deposit" => "StoneDeposit", 
			"resource.fiber_plant" => "FiberPlant", 
			"resource.copper_vein" => "CopperVein", 
			"resource.berry_bush" => "BerryBush", 
			_ => string.Empty, 
		} + ".prefab";
	}

	private static void AssertDefinition(IEnumerable<ResourceNodeDefinition> values, string id, string itemId, float duration)
	{
		ResourceNodeDefinition resourceNodeDefinition = values.Single((ResourceNodeDefinition entry) => entry.Id == id);
		Assert.That<string>(resourceNodeDefinition.OutputItem.Id, (IResolveConstraint)(object)Is.EqualTo((object)itemId));
		Assert.That<float>(resourceNodeDefinition.InteractionDuration, (IResolveConstraint)(object)Is.EqualTo((object)duration).Within((object)0.001f));
		Assert.That<HarvestYieldTable>(resourceNodeDefinition.HarvestYields, (IResolveConstraint)(object)Is.Not.Null, id, Array.Empty<object>());
	}

	private static ItemDefinition Item(string assetName)
	{
		ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/" + assetName + ".asset");
		Assert.That<ItemDefinition>(itemDefinition, (IResolveConstraint)(object)Is.Not.Null, assetName, Array.Empty<object>());
		return itemDefinition;
	}

	private static int Harvest(TestRuntime runtime, ResourceNode node, ResourceNodeDefinition definition)
	{
		string itemId = definition.OutputItem.Id;
		int before = runtime.Inventory.GetTotalAmount(itemId);
		node.CompleteInteraction(runtime.Context);
		return runtime.Inventory.GetTotalAmount(itemId) - before;
	}
}
}
