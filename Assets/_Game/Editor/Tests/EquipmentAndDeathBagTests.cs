using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Interaction;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class EquipmentAndDeathBagTests
{
	private GameObject _root;

	private ContentDatabase _content;

	private GameSession _session;

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("EquipmentTests");
		_content = _root.AddComponent<ContentDatabase>();
		_session = _root.AddComponent<GameSession>();
		_session.Initialize();
		_session.ConfigureContentDatabase(_content);
		_session.StartNewGame();
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_root);
	}

	[Test]
	public void SlotEnumeration_IsCompleteAndStable()
	{
		Assert.That<int>(Enum.GetValues(typeof(EquipmentSlot)).Length, (IResolveConstraint)(object)Is.EqualTo((object)13));
		Assert.That<int>(0, (IResolveConstraint)(object)Is.EqualTo((object)0));
		Assert.That<int>(1, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(10, (IResolveConstraint)(object)Is.EqualTo((object)10));
		Assert.That<int>(11, (IResolveConstraint)(object)Is.EqualTo((object)11));
		Assert.That<int>(12, (IResolveConstraint)(object)Is.EqualTo((object)12));
	}

	[Test]
	public void OnlyEightSlotsAcceptItemsInV01()
	{
		foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
		{
			bool usable = slot == EquipmentSlot.Weapon1 || slot == EquipmentSlot.Weapon2 || slot == EquipmentSlot.Head || slot == EquipmentSlot.Chest || slot == EquipmentSlot.Hands || slot == EquipmentSlot.Legs || slot == EquipmentSlot.CatchDevice || slot == EquipmentSlot.Battery;
			Assert.That<bool>(PlayerEquipment.IsUsableInV01(slot), (IResolveConstraint)(object)Is.EqualTo((object)usable), slot.ToString(), Array.Empty<object>());
		}
	}

	[Test]
	public void NewGame_HasSixteenBackpackSlotsAndEmptyEquipment()
	{
		Assert.That<int>(_session.PlayerInventory.SlotCapacity, (IResolveConstraint)(object)Is.EqualTo((object)16));
		foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
		{
			Assert.That<bool>(_session.PlayerEquipment.TryGetSlot(slot, out var _), (IResolveConstraint)(object)Is.False, slot.ToString(), Array.Empty<object>());
		}
	}

	[Test]
	public void EquippedItem_DoesNotOccupyABackpackSlot()
	{
		int before = CountOccupiedBackpackSlots();
		Assert.That<bool>(_session.PlayerEquipment.TryEquip(EquipmentSlot.CatchDevice, new ItemStack("catch_device", 1), out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(CountOccupiedBackpackSlots(), (IResolveConstraint)(object)Is.EqualTo((object)before));
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("catch_device"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_session.PlayerInventory.SlotCapacity, (IResolveConstraint)(object)Is.EqualTo((object)16));
	}

	[Test]
	public void BatterySlot_HoldsExactlyOneBattery()
	{
		Assert.That<bool>(_session.PlayerEquipment.TryEquip(EquipmentSlot.Battery, new ItemStack("battery", 3), out var _), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(_session.PlayerEquipment.TryEquip(EquipmentSlot.Battery, new ItemStack("battery", 1), out var error2), (IResolveConstraint)(object)Is.True, error2, Array.Empty<object>());
	}

	[Test]
	public void WeaponSlots_AcceptOnlyWeaponItems()
	{
		Assert.That<bool>(_session.PlayerEquipment.TryEquip(EquipmentSlot.Weapon1, ItemStack.Create(_content.GetItem("hammer"), 1), out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<bool>(_session.PlayerEquipment.TryEquip(EquipmentSlot.Weapon2, ItemStack.Create(_content.GetItem("daggers"), 1), out error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<bool>(_session.PlayerEquipment.TryEquip(EquipmentSlot.Weapon1, ItemStack.Create(_content.GetItem("axe"), 1), out error), (IResolveConstraint)(object)Is.False);
		Assert.That<string>(error, (IResolveConstraint)(object)Does.Contain("not compatible"));
	}

	[Test]
	public void ArmorSlots_AcceptOnlyTheirWearableType()
	{
		Assert.That<bool>(_session.PlayerEquipment.TryEquip(EquipmentSlot.Head, ItemStack.Create(_content.GetItem("armor_wanderer_hood"), 1), out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<bool>(_session.PlayerEquipment.TryEquip(EquipmentSlot.Head, ItemStack.Create(_content.GetItem("armor_wanderer_coat"), 1), out error), (IResolveConstraint)(object)Is.False);
		Assert.That<string>(error, (IResolveConstraint)(object)Does.Contain("not compatible"));
	}

	[Test]
	public void UnusedSlots_AcceptNothing()
	{
		Assert.That<bool>(_session.PlayerEquipment.TryEquip(EquipmentSlot.Ring1, new ItemStack("berry", 1), out var error), (IResolveConstraint)(object)Is.False);
		Assert.That<string>(error, (IResolveConstraint)(object)Does.Contain("takes nothing"));
	}

	[Test]
	public void Death_DropsTheWholeBackpackAndKeepsEquipment()
	{
		_session.PlayerInventory.Clear();
		_session.PlayerInventory.Add("wood", 7);
		_session.PlayerEquipment.TryEquip(EquipmentSlot.CatchDevice, new ItemStack("catch_device", 1), out var _);
		Assert.That<bool>(_session.RecordPlayerDeath(), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_session.TryDropDeathBag("Zone_Greenwood", Vector3.one), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.Zero, "M4: Der Rucksack faellt vollstaendig.", Array.Empty<object>());
		Assert.That<bool>(_session.PlayerEquipment.TryGetSlot(EquipmentSlot.CatchDevice, out var _), (IResolveConstraint)(object)Is.True, "M4: Ausruestungsplaetze bleiben beim Spieler.", Array.Empty<object>());
		Assert.That<bool>(_session.DeathBag.Exists, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_session.DeathBag.LiesIn("Zone_Greenwood"), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(TotalInDeathBag("wood"), (IResolveConstraint)(object)Is.EqualTo((object)7));
	}

	[Test]
	public void SecondDeath_ReplacesTheFirstBag()
	{
		_session.PlayerInventory.Clear();
		_session.PlayerInventory.Add("wood", 5);
		_session.RecordPlayerDeath();
		_session.TryDropDeathBag("Zone_Greenwood", Vector3.zero);
		_session.BeginRespawn("home_base", "HomeBase");
		_session.CompleteRespawn("HomeBase");
		_session.PlayerInventory.Add("stone", 2);
		_session.RecordPlayerDeath();
		_session.TryDropDeathBag("Zone_Quarry", Vector3.one);
		Assert.That<int>(TotalInDeathBag("wood"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(TotalInDeathBag("stone"), (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<bool>(_session.DeathBag.LiesIn("Zone_Quarry"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_session.DeathBag.LiesIn("Zone_Greenwood"), (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void EquipmentAndDeathBag_SurviveAnExportImportRoundTrip()
	{
		_session.PlayerEquipment.TryEquip(EquipmentSlot.Battery, new ItemStack("battery", 1), out var _);
		_session.RecordPlayerDeath();
		_session.TryDropDeathBag("Zone_Marsh", new Vector3(1f, 2f, 3f));
		GameSessionRuntimeState state = _session.ExportRuntimeState();
		_session.StartNewGame();
		Assert.That<bool>(_session.TryRestoreRuntimeState(state, out var error2), (IResolveConstraint)(object)Is.True, error2, Array.Empty<object>());
		Assert.That<bool>(_session.PlayerEquipment.TryGetSlot(EquipmentSlot.Battery, out var battery), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(battery.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"battery"));
		Assert.That<bool>(_session.DeathBag.LiesIn("Zone_Marsh"), (IResolveConstraint)(object)Is.True);
		Assert.That<Vector3>(_session.DeathBag.Position, (IResolveConstraint)(object)Is.EqualTo((object)new Vector3(1f, 2f, 3f)));
	}

	[Test]
	public void DeathBagPrefab_ExistsAndCarriesTheStableContainerId()
	{
		StorageContainer storageContainer = Resources.Load<StorageContainer>("Prefabs/DeathBag");
		Assert.That<StorageContainer>(storageContainer, (IResolveConstraint)(object)Is.Not.Null, "Run Eidren/Storage/Build V0.1.", Array.Empty<object>());
		Assert.That<string>(storageContainer.ContainerId, (IResolveConstraint)(object)Is.EqualTo((object)"death.bag"));
		Assert.That<int>(storageContainer.SlotCapacity, (IResolveConstraint)(object)Is.EqualTo((object)24));
	}

	[Test]
	public void EmptyBackpack_ReplacesTheBagButPlacesNothing()
	{
		_session.PlayerInventory.Clear();
		_session.PlayerInventory.Add("wood", 4);
		_session.RecordPlayerDeath();
		_session.TryDropDeathBag("Zone_Greenwood", Vector3.zero);
		Assert.That<bool>(_session.HasDeathBagContents(), (IResolveConstraint)(object)Is.True);
		_session.BeginRespawn("home_base", "HomeBase");
		_session.CompleteRespawn("HomeBase");
		_session.PlayerInventory.Clear();
		_session.RecordPlayerDeath();
		_session.TryDropDeathBag("Zone_Marsh", Vector3.zero);
		Assert.That<int>(TotalInDeathBag("wood"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<bool>(_session.HasDeathBagContents(), (IResolveConstraint)(object)Is.False);
		Assert.That<StorageContainer>(DeathBagSpawner.TrySpawn(_session, "Zone_Marsh"), (IResolveConstraint)(object)Is.Null);
	}

	private int CountOccupiedBackpackSlots()
	{
		int count = 0;
		ItemStack[] array = _session.PlayerInventory.ExportSlots();
		foreach (ItemStack stack in array)
		{
			if (!stack.IsEmpty)
			{
				count++;
			}
		}
		return count;
	}

	private int TotalInDeathBag(string itemId)
	{
		if (!_session.TryGetStorageState("death.bag", out var state))
		{
			return 0;
		}
		int total = 0;
		ItemStack[] slots = state.Slots;
		for (int i = 0; i < slots.Length; i++)
		{
			ItemStack stack = slots[i];
			if (string.Equals(stack.ItemId, itemId, StringComparison.Ordinal))
			{
				total += stack.Quantity;
			}
		}
		return total;
	}
}
}
