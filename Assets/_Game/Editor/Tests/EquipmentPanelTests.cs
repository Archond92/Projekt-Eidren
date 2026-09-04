using Eidren.Core.Services;
using Eidren.Data;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class EquipmentPanelTests
{
	private GameObject _root;

	private ContentDatabase _database;

	private PlayerInventory _inventory;

	private PlayerEquipment _equipment;

	private GameObject _window;

	private EquipmentPanelView _panel;

	private int _selectedInventorySlot;

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("EquipmentPanelTests");
		_database = _root.AddComponent<ContentDatabase>();
		_database.ConfigureItems(new ItemDefinition[3]
		{
			Item("WandererHood"),
			Item("Hammer"),
			Item("Stone")
		});
		_inventory = new PlayerInventory(_database);
		_equipment = new PlayerEquipment(_database);
		_selectedInventorySlot = 0;
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/UI/InventoryWindow.prefab");
		Assert.That<GameObject>(prefab, (IResolveConstraint)(object)Is.Not.Null);
		_window = UnityEngine.Object.Instantiate(prefab);
		_panel = _window.GetComponentInChildren<EquipmentPanelView>(includeInactive: true);
		Assert.That<EquipmentPanelView>(_panel, (IResolveConstraint)(object)Is.Not.Null);
		_panel.Initialize(_equipment, _inventory, _database, () => _selectedInventorySlot, delegate
		{
		});
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_window);
		UnityEngine.Object.DestroyImmediate(_root);
	}

	[Test]
	public void PrefabHasEightLargeMobileEquipmentTargets()
	{
		Assert.That<EquipmentSlotView[]>(_panel.SlotViews, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)8));
		Assert.That<IEnumerable<EquipmentSlot>>(_panel.SlotViews.Select((EquipmentSlotView view) => view.Slot), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new EquipmentSlot[8]
		{
			EquipmentSlot.Weapon1,
			EquipmentSlot.Weapon2,
			EquipmentSlot.Head,
			EquipmentSlot.Chest,
			EquipmentSlot.Hands,
			EquipmentSlot.Legs,
			EquipmentSlot.CatchDevice,
			EquipmentSlot.Battery
		}));
		GridLayoutGroup componentInChildren = _panel.GetComponentInChildren<GridLayoutGroup>(includeInactive: true);
		Assert.That<GridLayoutGroup>(componentInChildren, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<float>(componentInChildren.cellSize.x, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)220f));
		Assert.That<float>(componentInChildren.cellSize.y, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)88f));
	}

	[Test]
	public void EmptyEquipmentSlotsShowNoIcon()
	{
		EquipmentSlotView[] slotViews = _panel.SlotViews;
		foreach (EquipmentSlotView view in slotViews)
		{
			Assert.That<Sprite>(view.Icon.sprite, (IResolveConstraint)(object)Is.Null, view.Slot.ToString(), Array.Empty<object>());
			Assert.That<bool>(view.Icon.enabled, (IResolveConstraint)(object)Is.False, view.Slot.ToString(), Array.Empty<object>());
		}
	}

	[Test]
	public void TappingCompatibleSlotEquipsAndUnequipsSelectedItem()
	{
		_inventory.Add("armor_wanderer_hood", 1);
		EquipmentSlotView equipmentSlotView = View(EquipmentSlot.Head);
		equipmentSlotView.Button.onClick.Invoke();
		Assert.That<bool>(_equipment.TryGetSlot(EquipmentSlot.Head, out var worn), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(worn.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"armor_wanderer_hood"));
		Assert.That<int>(_inventory.GetTotalAmount("armor_wanderer_hood"), (IResolveConstraint)(object)Is.Zero);
		equipmentSlotView.Button.onClick.Invoke();
		Assert.That<bool>(_equipment.TryGetSlot(EquipmentSlot.Head, out var _), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(_inventory.GetTotalAmount("armor_wanderer_hood"), (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void IncompatibleSlotExplainsFailureWithoutChangingInventory()
	{
		_inventory.Add("armor_wanderer_hood", 1);
		View(EquipmentSlot.Chest).Button.onClick.Invoke();
		Assert.That<bool>(_equipment.TryGetSlot(EquipmentSlot.Chest, out var _), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(_inventory.GetTotalAmount("armor_wanderer_hood"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<string>(_panel.FeedbackText.text, (IResolveConstraint)(object)Does.Contain("passt nicht"));
	}

	[Test]
	public void UnequippedWeaponCanBeEquippedAgainFromInventory()
	{
		_inventory.Add("stone", 1);
		_inventory.Add("hammer", 1);
		SelectSlotHolding("hammer");
		EquipmentSlotView equipmentSlotView = View(EquipmentSlot.Weapon1);
		equipmentSlotView.Button.onClick.Invoke();
		equipmentSlotView.Button.onClick.Invoke();
		Assert.That<int>(_inventory.GetTotalAmount("hammer"), (IResolveConstraint)(object)Is.EqualTo((object)1), "Die abgelegte Waffe liegt nicht im Rucksack.", Array.Empty<object>());
		SelectSlotHolding("hammer");
		equipmentSlotView.Button.onClick.Invoke();
		Assert.That<bool>(_equipment.TryGetSlot(EquipmentSlot.Weapon1, out var equipped), (IResolveConstraint)(object)Is.True, _panel.FeedbackText.text, Array.Empty<object>());
		Assert.That<string>(equipped.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"hammer"));
		Assert.That<int>(_inventory.GetTotalAmount("hammer"), (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void UnequippedWeaponReturnsToTheSelectedInventorySlot()
	{
		_inventory.Add("hammer", 1);
		_inventory.Move(0, 1);
		_selectedInventorySlot = 1;
		EquipmentSlotView equipmentSlotView = View(EquipmentSlot.Weapon1);
		equipmentSlotView.Button.onClick.Invoke();
		equipmentSlotView.Button.onClick.Invoke();
		Assert.That<bool>(_inventory.TryGetSlot(1, out var returned), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(returned.ItemId, (IResolveConstraint)(object)Is.EqualTo((object)"hammer"), "Die Waffe ist auf einen anderen Platz gesprungen.", Array.Empty<object>());
		equipmentSlotView.Button.onClick.Invoke();
		Assert.That<bool>(_equipment.TryGetSlot(EquipmentSlot.Weapon1, out var _), (IResolveConstraint)(object)Is.True, _panel.FeedbackText.text, Array.Empty<object>());
	}

	[Test]
	public void RepeatedEquipCyclesKeepExactlyOneWeapon()
	{
		_inventory.Add("hammer", 1);
		EquipmentSlotView weapon = View(EquipmentSlot.Weapon1);
		for (int cycle = 0; cycle < 3; cycle++)
		{
			SelectSlotHolding("hammer");
			weapon.Button.onClick.Invoke();
			Assert.That<bool>(_equipment.TryGetSlot(EquipmentSlot.Weapon1, out var _), (IResolveConstraint)(object)Is.True, $"Runde {cycle}: {_panel.FeedbackText.text}", Array.Empty<object>());
			Assert.That<int>(_inventory.GetTotalAmount("hammer"), (IResolveConstraint)(object)Is.Zero, $"Runde {cycle}: Waffe verdoppelt.", Array.Empty<object>());
			weapon.Button.onClick.Invoke();
			Assert.That<int>(_inventory.GetTotalAmount("hammer"), (IResolveConstraint)(object)Is.EqualTo((object)1), $"Runde {cycle}: Waffe verloren.", Array.Empty<object>());
		}
	}

	private void SelectSlotHolding(string itemId)
	{
		for (int index = 0; index < 16; index++)
		{
			if (_inventory.TryGetSlot(index, out var stack) && stack.ItemId == itemId)
			{
				_selectedInventorySlot = index;
				return;
			}
		}
		Assert.Fail("'" + itemId + "' liegt in keinem Rucksackplatz.");
	}

	private EquipmentSlotView View(EquipmentSlot slot)
	{
		return _panel.SlotViews.Single((EquipmentSlotView view) => view.Slot == slot);
	}

	private static ItemDefinition Item(string assetName)
	{
		ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/" + assetName + ".asset");
		Assert.That<ItemDefinition>(itemDefinition, (IResolveConstraint)(object)Is.Not.Null, assetName, Array.Empty<object>());
		return itemDefinition;
	}
}
}
