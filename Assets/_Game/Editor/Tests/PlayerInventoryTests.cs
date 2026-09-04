using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Linq;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class PlayerInventoryTests
{
	private sealed class FakeUseHandler : IItemUseHandler
	{
		public bool Result { get; set; }

		public int Calls { get; private set; }

		public FakeUseHandler(bool result)
		{
			Result = result;
		}

		public bool TryUse(ItemDefinition item)
		{
			Calls++;
			return Result;
		}
	}

	private GameObject _databaseObject;

	private ContentDatabase _database;

	private PlayerInventory _inventory;

	[SetUp]
	public void SetUp()
	{
		_databaseObject = new GameObject("PlayerInventoryDatabase_Test");
		_database = _databaseObject.AddComponent<ContentDatabase>();
		_inventory = new PlayerInventory(_database);
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_databaseObject);
	}

	[Test]
	public void NewInventory_HasExactlySixteenEmptySlots()
	{
		Assert.That<int>(_inventory.Count, (IResolveConstraint)(object)Is.EqualTo((object)16));
		for (int index = 0; index < 16; index++)
		{
			Assert.That<bool>(_inventory.TryGetSlot(index, out var stack), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(stack.IsEmpty, (IResolveConstraint)(object)Is.True);
		}
	}

	[Test]
	public void Add_PutsItemIntoEmptySlot()
	{
		Assert.That<int>(_inventory.Add("healing_potion", 3), (IResolveConstraint)(object)Is.Zero);
		Assert.That<bool>(_inventory.TryGetSlot(0, out var stack), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(stack.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"healing_potion"));
		Assert.That<int>(stack.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)3));
	}

	[Test]
	public void Add_FillsExistingStackBeforeFreeSlot()
	{
		_inventory.Add("healing_potion", 8);
		Assert.That<int>(_inventory.Add("healing_potion", 5), (IResolveConstraint)(object)Is.Zero);
		_inventory.TryGetSlot(0, out var first);
		_inventory.TryGetSlot(1, out var second);
		Assert.That<int>(first.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)10));
		Assert.That<int>(second.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)3));
	}

	[Test]
	public void Add_ReturnsRemainderWhenInventoryIsFull()
	{
		int capacity = 16 * _database.GetItem("stone").MaximumStackSize;
		Assert.That<int>(_inventory.Add("stone", capacity), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_inventory.Add("wood", 4), (IResolveConstraint)(object)Is.EqualTo((object)4));
		Assert.That<int>(_inventory.GetTotalAmount("stone"), (IResolveConstraint)(object)Is.EqualTo((object)capacity));
		Assert.That<int>(_inventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void Remove_IsAtomicAndDoesNotLoseItems()
	{
		_inventory.Add("healing_potion", 13);
		Assert.That<bool>(_inventory.Remove("healing_potion", 14), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(_inventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)13));
		Assert.That<bool>(_inventory.Remove("healing_potion", 11), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(_inventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	[Test]
	public void Swap_ExchangesTwoSlots()
	{
		_inventory.Add("healing_potion", 3);
		_inventory.Add("buff_food", 2);
		Assert.That<bool>(_inventory.Swap(0, 1), (IResolveConstraint)(object)Is.True);
		_inventory.TryGetSlot(0, out var first);
		_inventory.TryGetSlot(1, out var second);
		Assert.That<string>(first.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"buff_food"));
		Assert.That<string>(second.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"healing_potion"));
	}

	[Test]
	public void Move_ReordersIntoEmptySlotAndCombinesMatchingStacks()
	{
		_inventory.Reset(new ItemStack[2]
		{
			new ItemStack("healing_potion", 7),
			new ItemStack("healing_potion", 3)
		});
		Assert.That<bool>(_inventory.Move(1, 3), (IResolveConstraint)(object)Is.True);
		_inventory.TryGetSlot(1, out var emptied);
		_inventory.TryGetSlot(3, out var moved);
		Assert.That<bool>(emptied.IsEmpty, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(moved.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<bool>(_inventory.Move(3, 0), (IResolveConstraint)(object)Is.True);
		_inventory.TryGetSlot(0, out var combined);
		_inventory.TryGetSlot(3, out var source);
		Assert.That<int>(combined.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)10));
		Assert.That<bool>(source.IsEmpty, (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void Use_RemovesExactlyOneOnlyAfterSuccessfulExecution()
	{
		_inventory.Add("healing_potion", 3);
		FakeUseHandler handler = new FakeUseHandler(result: true);
		Assert.That<bool>(_inventory.Use(0, handler), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(handler.Calls, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_inventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)2));
		handler.Result = false;
		Assert.That<bool>(_inventory.Use(0, handler), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(_inventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	[Test]
	public void BuffFood_IsRemovedExactlyOnce()
	{
		_inventory.Add("buff_food", 2);
		Assert.That<bool>(_inventory.Use(0, new FakeUseHandler(result: true)), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(_inventory.GetTotalAmount("buff_food"), (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void UnknownItemId_DoesNotChangeExistingContents()
	{
		_inventory.Add("wood", 7);
		int changeCount = 0;
		InventoryFeedbackType feedback = InventoryFeedbackType.InventoryFull;
		_inventory.Changed += delegate
		{
			changeCount++;
		};
		_inventory.Feedback += delegate(InventoryFeedback value)
		{
			feedback = value.Type;
		};
		Assert.That<int>(_inventory.Add("missing.item", 5), (IResolveConstraint)(object)Is.EqualTo((object)5));
		Assert.That<int>(changeCount, (IResolveConstraint)(object)Is.Zero);
		Assert.That<InventoryFeedbackType>(feedback, (IResolveConstraint)(object)Is.EqualTo((object)InventoryFeedbackType.UnknownItemId));
		Assert.That<int>(_inventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)7));
	}

	[Test]
	public void MultiSlotChange_RaisesExactlyOneUpdateEvent()
	{
		int changes = 0;
		_inventory.Changed += delegate
		{
			changes++;
		};
		_inventory.Add("healing_potion", 23);
		Assert.That<int>(changes, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_inventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)23));
	}

	[Test]
	public void NoOpOperations_DoNotRaiseUpdateEvents()
	{
		int changes = 0;
		_inventory.Changed += delegate
		{
			changes++;
		};
		Assert.That<bool>(_inventory.Swap(0, 1), (IResolveConstraint)(object)Is.False);
		_inventory.Clear();
		_inventory.Reset(Array.Empty<ItemStack>());
		Assert.That<int>(changes, (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void NewGame_StartsWithAnEmptyBackpack()
	{
		GameObject root = new GameObject("InventorySession_Test");
		try
		{
			GameSession gameSession = root.AddComponent<GameSession>();
			gameSession.Initialize();
			gameSession.ConfigureContentDatabase(_database);
			gameSession.StartNewGame();
			Assert.That<int>(gameSession.PlayerInventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.Zero);
			Assert.That<int>(gameSession.PlayerInventory.GetTotalAmount("buff_food"), (IResolveConstraint)(object)Is.Zero);
			Assert.That<int>(gameSession.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.Zero);
			Assert.That<bool>(gameSession.PlayerInventory.ExportSlots().All((ItemStack stack) => stack.IsEmpty), (IResolveConstraint)(object)Is.True, "M8.2: Ein neues Spiel schenkt keine Gegenstaende.", Array.Empty<object>());
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}
}
}
