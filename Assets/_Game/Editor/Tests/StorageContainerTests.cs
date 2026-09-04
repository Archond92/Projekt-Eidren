using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class StorageContainerTests
{
	private GameObject _databaseObject;

	private ContentDatabase _database;

	private GameObject _sessionObject;

	private GameSession _session;

	private GameObject _storageObject;

	private StorageContainer _storage;

	private PlayerInventory _inventory;

	private ItemTransferService _service;

	[SetUp]
	public void SetUp()
	{
		_databaseObject = new GameObject("StorageDatabase_Test");
		_database = _databaseObject.AddComponent<ContentDatabase>();
		_sessionObject = new GameObject("StorageSession_Test");
		_session = _sessionObject.AddComponent<GameSession>();
		_session.Initialize();
		_session.ConfigureContentDatabase(_database);
		_session.StartNewGame();
		_inventory = _session.PlayerInventory;
		_inventory.Clear();
		_storageObject = new GameObject("Storage_Test");
		_storage = _storageObject.AddComponent<StorageContainer>();
		_storage.Configure("test.storage.main", "Testlager", null, 2.5f);
		_storage.Initialize(_database, _session);
		_service = new ItemTransferService(_database);
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_storageObject);
		UnityEngine.Object.DestroyImmediate(_sessionObject);
		UnityEngine.Object.DestroyImmediate(_databaseObject);
	}

	[Test]
	public void Transfer_PlayerToStorage_MovesWholeStack()
	{
		_inventory.Add("wood", 7);
		int inventoryEvents = 0;
		int storageEvents = 0;
		_inventory.Changed += delegate
		{
			inventoryEvents++;
		};
		_storage.Changed += delegate
		{
			storageEvents++;
		};
		ItemTransferResult result = _service.Transfer(_inventory, 0, _storage);
		Assert.That<ItemTransferResultCode>(result.Code, (IResolveConstraint)(object)Is.EqualTo((object)ItemTransferResultCode.Success));
		Assert.That<int>(result.Transferred, (IResolveConstraint)(object)Is.EqualTo((object)7));
		Assert.That<int>(_inventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(Total(_storage, "wood"), (IResolveConstraint)(object)Is.EqualTo((object)7));
		Assert.That<int>(inventoryEvents, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(storageEvents, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void Transfer_StorageToPlayer_MovesWholeStack()
	{
		SetStorageSlot(0, "stone", 9);
		Assert.That<bool>(_service.Transfer(_storage, 0, _inventory).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(Total(_storage, "stone"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_inventory.GetTotalAmount("stone"), (IResolveConstraint)(object)Is.EqualTo((object)9));
	}

	[Test]
	public void Transfer_TargetFull_ChangesNeitherContainer()
	{
		int capacity = FullInventoryAmount("stone");
		_inventory.Add("stone", capacity);
		SetStorageSlot(0, "wood", 6);
		Assert.That<ItemTransferResultCode>(_service.Transfer(_storage, 0, _inventory).Code, (IResolveConstraint)(object)Is.EqualTo((object)ItemTransferResultCode.TargetFull));
		Assert.That<int>(Total(_storage, "wood"), (IResolveConstraint)(object)Is.EqualTo((object)6));
		Assert.That<int>(_inventory.GetTotalAmount("stone"), (IResolveConstraint)(object)Is.EqualTo((object)capacity));
	}

	[Test]
	public void Transfer_Partial_KeepsRemainderAtSource()
	{
		int woodStack = StackSize("wood");
		_inventory.Add("wood", woodStack - 2);
		_inventory.Add("stone", 15 * StackSize("stone"));
		SetStorageSlot(0, "wood", 5);
		ItemTransferResult result = _service.Transfer(_storage, 0, _inventory);
		Assert.That<ItemTransferResultCode>(result.Code, (IResolveConstraint)(object)Is.EqualTo((object)ItemTransferResultCode.Partial));
		Assert.That<int>(result.Transferred, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(result.Remainder, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(Total(_storage, "wood"), (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(_inventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)woodStack));
	}

	[Test]
	public void Transfer_FillsExistingStackBeforeFreeSlot()
	{
		int woodStack = StackSize("wood");
		_inventory.Add("wood", woodStack - 5);
		SetStorageSlot(0, "wood", woodStack);
		_service.Transfer(_storage, 0, _inventory);
		_inventory.TryGetSlot(0, out var first);
		_inventory.TryGetSlot(1, out var second);
		Assert.That<int>(first.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)woodStack));
		Assert.That<int>(second.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)5));
	}

	[Test]
	public void Transfer_DamagedEquipmentPreservesIdentityAndDurability()
	{
		ItemDefinition hammer = _database.GetItem("hammer");
		ItemStack[] slots = _storage.ExportSlots();
		slots[0] = ItemStack.Create(hammer, 1, "hammer.storage.roundtrip", 37);
		Assert.That<bool>(_storage.TryImportSlots(slots, out var importError), (IResolveConstraint)(object)Is.True, importError, Array.Empty<object>());
		Assert.That<bool>(_service.Transfer(_storage, 0, _inventory).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_inventory.TryGetSlot(0, out var carried), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(carried.InstanceId, (IResolveConstraint)(object)Is.EqualTo((object)"hammer.storage.roundtrip"));
		Assert.That<int>(carried.Durability, (IResolveConstraint)(object)Is.EqualTo((object)37));
		Assert.That<bool>(_service.Transfer(_inventory, 0, _storage).Succeeded, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_storage.TryGetSlot(0, out var returned), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(returned.InstanceId, (IResolveConstraint)(object)Is.EqualTo((object)"hammer.storage.roundtrip"));
		Assert.That<int>(returned.Durability, (IResolveConstraint)(object)Is.EqualTo((object)37));
	}

	private int StackSize(string itemId)
	{
		return _database.GetItem(itemId).MaximumStackSize;
	}

	private int FullInventoryAmount(string itemId)
	{
		return 16 * StackSize(itemId);
	}

	[Test]
	public void Container_HasStableUniqueIdAndExactlyTwentyFourSlots()
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Stations/StorageChest.prefab");
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null);
		StorageContainer component = gameObject.GetComponent<StorageContainer>();
		Assert.That<StorageContainer>(component, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<string>(component.ContainerId, (IResolveConstraint)(object)Is.EqualTo((object)"home_base.storage.main"));
		Assert.That<int>(component.SlotCapacity, (IResolveConstraint)(object)Is.EqualTo((object)24));
		Assert.That<int>(component.ExportSlots().Length, (IResolveConstraint)(object)Is.EqualTo((object)24));
		Assert.That<string>(component.ContainerId, (IResolveConstraint)(object)Is.Not.EqualTo((object)"home_base.workbench"));
	}

	[Test]
	public void ExportImport_UsesCopiesAndPersistsInGameSession()
	{
		SetStorageSlot(3, "copper_ore", 4);
		_storage.ExportState().Slots[3] = ItemStack.Empty;
		_storage.TryGetSlot(3, out var unchanged);
		Assert.That<int>(unchanged.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)4));
		Assert.That<bool>(_session.TryGetStorageState(_storage.ContainerId, out var saved), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(saved.Slots[3].Quantity, (IResolveConstraint)(object)Is.EqualTo((object)4));
	}

	[Test]
	public void ReentrantTransfer_IsBlockedAndMovesOnlyOnce()
	{
		_inventory.Add("plant_fiber", 4);
		ItemTransferResult nested = default(ItemTransferResult);
		_service.TransferCompleted += delegate
		{
			nested = _service.Transfer(_inventory, 0, _storage);
		};
		Assert.That<int>(_service.Transfer(_inventory, 0, _storage, 2).Transferred, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<ItemTransferResultCode>(nested.Code, (IResolveConstraint)(object)Is.EqualTo((object)ItemTransferResultCode.Busy));
		Assert.That<int>(_inventory.GetTotalAmount("plant_fiber"), (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(Total(_storage, "plant_fiber"), (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	private void SetStorageSlot(int index, string itemId, int amount)
	{
		ItemStack[] slots = _storage.ExportSlots();
		slots[index] = new ItemStack(itemId, amount);
		Assert.That<bool>(_storage.TryImportSlots(slots, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
	}

	private static int Total(IItemContainer container, string itemId)
	{
		int total = 0;
		for (int index = 0; index < container.SlotCapacity; index++)
		{
			if (container.TryGetSlot(index, out var stack) && stack.ItemId == itemId)
			{
				total += stack.Quantity;
			}
		}
		return total;
	}
}
}
