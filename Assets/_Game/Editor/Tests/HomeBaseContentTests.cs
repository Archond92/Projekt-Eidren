using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class HomeBaseContentTests
{
	private GameObject _root;

	private ContentDatabase _content;

	private GameSession _session;

	private HomeBaseMaterialStock _stock;

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("HomeBaseContentTests");
		_content = _root.AddComponent<ContentDatabase>();
		_session = _root.AddComponent<GameSession>();
		_session.Initialize();
		_session.ConfigureContentDatabase(_content);
		_session.StartNewGame();
		_session.PlayerInventory.Clear();
		_stock = new HomeBaseMaterialStock(_content, _session);
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_root);
	}

	[Test]
	public void SharedStockConsumesPlayerThenChestsInStableOrder()
	{
		_session.PlayerInventory.Add("wood", 2);
		AddChest("home.chest.b", "wood", 4);
		AddChest("home.chest.a", "wood", 3);
		Assert.That<bool>(_stock.TryApplyTransaction(new InventoryItemAmount[1]
		{
			new InventoryItemAmount("wood", 7)
		}, string.Empty, 0, out var failure), (IResolveConstraint)(object)Is.True, failure.ToString(), Array.Empty<object>());
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(StorageAmount("home.chest.a", "wood"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(StorageAmount("home.chest.b", "wood"), (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	[Test]
	public void InsufficientDistributedStockIsAtomic()
	{
		_session.PlayerInventory.Add("stone", 2);
		AddChest("home.chest.a", "stone", 3);
		Assert.That<bool>(_stock.TryApplyTransaction(new InventoryItemAmount[1]
		{
			new InventoryItemAmount("stone", 6)
		}, string.Empty, 0, out var failure), (IResolveConstraint)(object)Is.False);
		Assert.That<InventoryTransactionFailure>(failure, (IResolveConstraint)(object)Is.EqualTo((object)InventoryTransactionFailure.MissingItems));
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("stone"), (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(StorageAmount("home.chest.a", "stone"), (IResolveConstraint)(object)Is.EqualTo((object)3));
	}

	[Test]
	public void OutsideContainersNeverEnterHomeStock()
	{
		_session.SetStorageState(new StorageContainerState("zone_quarry.loot.01", Slots("wood", 9)));
		_session.SetStorageState(new StorageContainerState("home_base.authored_supply", Slots("wood", 4)));
		Assert.That<int>(_stock.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)4));
	}

	[Test]
	public void ProductionPrefabsCarryAllAuthoredStateViews()
	{
		Assert.That<TechnologyNodeButtonView[]>(Load<GameObject>("Assets/_Game/Resources/UI/TechnologyTreeWindow.prefab").GetComponent<TechnologyTreeWindow>().NodeViews, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)23));
		// Riftling statt Wildling: das Wildling-Prefab wurde nie gebaut (der
		// ContentBuilder brach am fehlenden 2D-Visual ab, die Szenen-Wildlinge
		// laufen ohne Prefab). Der Riftling traegt dieselben StatusBars-Views
		// und ist das kanonische Gegner-Prefab.
		WildlingStatusBars component = Load<GameObject>("Assets/_Game/Prefabs/Enemies/TierTwo/Riftling.prefab").GetComponent<WildlingStatusBars>();
		Assert.That<WildlingStatusBars>(component, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Canvas>(component.WorldCanvas, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Image>(component.HealthFill, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Image>(component.StaggerFill, (IResolveConstraint)(object)Is.Not.Null);
		// BLD_FarmPlot_L01 statt FarmPlot: die Gebaeude-Prefabs sind nach
		// Prefabs/Buildings/Level01/BLD_<Name>_L01 umgezogen (gleiche
		// Umstellung wie in den InteractionIconTests).
		FarmPlotController componentInChildren = Load<GameObject>("Assets/_Game/Prefabs/Buildings/Level01/BLD_FarmPlot_L01.prefab").GetComponentInChildren<FarmPlotController>(includeInactive: true);
		SerializedObject source = new SerializedObject(componentInChildren);
		Assert.That<GameObject>(Visual(source, "emptyVisual"), (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<GameObject>(Visual(source, "plantedVisual"), (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<GameObject>(Visual(source, "readyVisual"), (IResolveConstraint)(object)Is.Not.Null);
		Transform transform = componentInChildren.transform.Find("Visual_A20");
		Assert.That<Transform>(transform, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Collider[]>(transform.GetComponentsInChildren<Collider>(includeInactive: true), (IResolveConstraint)(object)Is.Empty, "Farm visuals must not alter the mechanical collider.", Array.Empty<object>());
	}

	[Test]
	public void PermanentHomeResourceHasNoExhaustedRemainder()
	{
		GameObject nodeObject = new GameObject("PermanentResource");
		nodeObject.transform.SetParent(_root.transform);
		GameObject active = new GameObject("Active");
		active.transform.SetParent(nodeObject.transform);
		GameObject exhausted = new GameObject("Exhausted");
		exhausted.transform.SetParent(nodeObject.transform);
		BoxCollider trigger = nodeObject.AddComponent<BoxCollider>();
		BoxCollider blocker = nodeObject.AddComponent<BoxCollider>();
		ResourceNodeDefinition definition = ScriptableObject.CreateInstance<ResourceNodeDefinition>();
		try
		{
			SetDefinition(definition);
			ResourceNode resourceNode = nodeObject.AddComponent<ResourceNode>();
			resourceNode.Configure(definition, "home_base.resource.test", active, exhausted, trigger, blocker);
			resourceNode.ConfigureInstanceId("home_base.resource.test", ResourceRespawnMode.None);
			resourceNode.MarkAlreadyHarvested();
			Assert.That<bool>(active.activeSelf, (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(exhausted.activeSelf, (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(trigger.enabled, (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(blocker.enabled, (IResolveConstraint)(object)Is.False);
			resourceNode.ResetForZoneEntry();
			Assert.That<bool>(active.activeSelf, (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(definition);
		}
	}

	private void AddChest(string id, string itemId, int amount)
	{
		Assert.That<bool>(_session.Buildings.TryAdd(new BuildingInstanceState(id, "building.storage_chest", Vector3.zero, 0, 1)), (IResolveConstraint)(object)Is.True);
		_session.SetStorageState(new StorageContainerState(id, Slots(itemId, amount)));
	}

	private int StorageAmount(string id, string itemId)
	{
		Assert.That<bool>(_session.TryGetStorageState(id, out var state), (IResolveConstraint)(object)Is.True);
		return state.Slots.Where((ItemStack slot) => slot.ItemId == itemId).Sum((ItemStack slot) => slot.Quantity);
	}

	private static ItemStack[] Slots(string itemId, int amount)
	{
		ItemStack[] array = new ItemStack[24];
		array[0] = new ItemStack(itemId, amount);
		return array;
	}

	private static GameObject Visual(SerializedObject source, string property)
	{
		return source.FindProperty(property).objectReferenceValue as GameObject;
	}

	private static void SetDefinition(ResourceNodeDefinition definition)
	{
		SerializedObject serializedObject = new SerializedObject(definition);
		serializedObject.FindProperty("id").stringValue = "resource.node.auftrag20";
		serializedObject.FindProperty("outputItem").objectReferenceValue = Load<ItemDefinition>("Assets/_Game/Data/Items/Wood.asset");
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
	}

	private static T Load<T>(string path) where T : UnityEngine.Object
	{
		T val = AssetDatabase.LoadAssetAtPath<T>(path);
		Assert.That<T>(val, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		return val;
	}
}
}
