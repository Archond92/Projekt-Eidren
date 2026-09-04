using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class HomeBaseMaterialStock : IMaterialStock
	{
		private const string AuthoredHomeStoragePrefix = "home_base.";

		private readonly ContentDatabase _content;

		private readonly GameSession _session;

		private readonly PlayerInventory _inventory;

		public HomeBaseMaterialStock(ContentDatabase content, GameSession session)
		{
			_content = content ?? throw new ArgumentNullException("content");
			_session = session ?? throw new ArgumentNullException("session");
			_inventory = session.PlayerInventory ?? throw new ArgumentException("The session must be initialized.", "session");
		}

		public int GetTotalAmount(string itemId)
		{
			if (string.IsNullOrWhiteSpace(itemId))
			{
				return 0;
			}
			int num = _inventory.GetTotalAmount(itemId);
			StorageContainerState[] array = EligibleStorages();
			for (int i = 0; i < array.Length; i++)
			{
				ItemStack[] slots = array[i].Slots;
				for (int j = 0; j < slots.Length; j++)
				{
					ItemStack itemStack = slots[j];
					if (string.Equals(itemStack.ItemId, itemId, StringComparison.Ordinal))
					{
						num = checked(num + itemStack.Quantity);
					}
				}
			}
			return num;
		}

		public bool CanApplyTransaction(IReadOnlyList<InventoryItemAmount> removals, string additionItemId, int additionAmount, out InventoryTransactionFailure failure)
		{
			ItemStack[] inventorySlots;
			StorageContainerState[] storages;
			return TryCreateCandidate(removals, additionItemId, additionAmount, out inventorySlots, out storages, out failure);
		}

		public bool TryApplyTransaction(IReadOnlyList<InventoryItemAmount> removals, string additionItemId, int additionAmount, out InventoryTransactionFailure failure)
		{
			if (!TryCreateCandidate(removals, additionItemId, additionAmount, out var inventorySlots, out var storages, out failure))
			{
				return false;
			}
			if (!_inventory.TryImportSlots(inventorySlots, out var _))
			{
				failure = InventoryTransactionFailure.InvalidRequest;
				return false;
			}
			StorageContainerState[] array = storages;
			foreach (StorageContainerState storageState in array)
			{
				_session.SetStorageState(storageState);
			}
			failure = InventoryTransactionFailure.None;
			return true;
		}

		private bool TryCreateCandidate(IReadOnlyList<InventoryItemAmount> removals, string additionItemId, int additionAmount, out ItemStack[] inventorySlots, out StorageContainerState[] storages, out InventoryTransactionFailure failure)
		{
			inventorySlots = null;
			storages = null;
			if (!TryAggregate(removals, out var required, out failure) || additionAmount < 0 || (additionAmount > 0 && !_content.TryGetItem(additionItemId, out var _)))
			{
				if (failure == InventoryTransactionFailure.None)
				{
					failure = InventoryTransactionFailure.UnknownItem;
				}
				return false;
			}
			foreach (KeyValuePair<string, int> item in required)
			{
				if (GetTotalAmount(item.Key) < item.Value)
				{
					failure = InventoryTransactionFailure.MissingItems;
					return false;
				}
			}
			StorageContainerState[] array = EligibleStorages();
			ItemStack[][] array2 = new ItemStack[array.Length][];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = array[i].Slots;
			}
			List<InventoryItemAmount> list = new List<InventoryItemAmount>();
			foreach (KeyValuePair<string, int> item2 in required)
			{
				int num = Math.Min(item2.Value, _inventory.GetTotalAmount(item2.Key));
				if (num > 0)
				{
					list.Add(new InventoryItemAmount(item2.Key, num));
				}
				int remaining = item2.Value - num;
				RemoveFromStorages(array2, item2.Key, ref remaining);
				if (remaining != 0)
				{
					failure = InventoryTransactionFailure.MissingItems;
					return false;
				}
			}
			PlayerInventory playerInventory = new PlayerInventory(_content);
			if (!playerInventory.TryImportSlots(_inventory.ExportSlots(), out var _) || !playerInventory.TryApplyTransaction(list, additionItemId, additionAmount, out failure))
			{
				return false;
			}
			inventorySlots = playerInventory.ExportSlots();
			storages = new StorageContainerState[array.Length];
			for (int j = 0; j < array.Length; j++)
			{
				storages[j] = new StorageContainerState(array[j].ContainerId, array2[j]);
			}
			failure = InventoryTransactionFailure.None;
			return true;
		}

		private bool TryAggregate(IReadOnlyList<InventoryItemAmount> removals, out SortedDictionary<string, int> required, out InventoryTransactionFailure failure)
		{
			required = new SortedDictionary<string, int>(StringComparer.Ordinal);
			if (removals == null)
			{
				failure = InventoryTransactionFailure.InvalidRequest;
				return false;
			}
			foreach (InventoryItemAmount removal in removals)
			{
				if (string.IsNullOrWhiteSpace(removal.ItemId) || removal.Amount <= 0)
				{
					failure = InventoryTransactionFailure.InvalidRequest;
					return false;
				}
				if (!_content.TryGetItem(removal.ItemId, out var value))
				{
					failure = InventoryTransactionFailure.UnknownItem;
					return false;
				}
				required.TryGetValue(value.Id, out var value2);
				required[value.Id] = checked(value2 + removal.Amount);
			}
			failure = InventoryTransactionFailure.None;
			return true;
		}

		private StorageContainerState[] EligibleStorages()
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			BuildingInstanceState[] all = _session.Buildings.GetAll();
			foreach (BuildingInstanceState buildingInstanceState in all)
			{
				if (string.Equals(buildingInstanceState.BuildingId, "building.storage_chest", StringComparison.Ordinal))
				{
					hashSet.Add(buildingInstanceState.InstanceId);
				}
			}
			List<StorageContainerState> list = new List<StorageContainerState>();
			StorageContainerState[] storageStates = _session.GetStorageStates();
			foreach (StorageContainerState storageContainerState in storageStates)
			{
				if (hashSet.Contains(storageContainerState.ContainerId) || storageContainerState.ContainerId.StartsWith("home_base.", StringComparison.Ordinal))
				{
					list.Add(storageContainerState);
				}
			}
			list.Sort((StorageContainerState left, StorageContainerState right) => string.CompareOrdinal(left.ContainerId, right.ContainerId));
			return list.ToArray();
		}

		private void RemoveFromStorages(ItemStack[][] storageSlots, string itemId, ref int remaining)
		{
			ItemDefinition item = _content.GetItem(itemId);
			for (int i = 0; i < storageSlots.Length; i++)
			{
				if (remaining <= 0)
				{
					break;
				}
				ItemStack[] array = storageSlots[i];
				for (int j = 0; j < array.Length; j++)
				{
					if (remaining <= 0)
					{
						break;
					}
					ItemStack itemStack = array[j];
					if (string.Equals(itemStack.ItemId, itemId, StringComparison.Ordinal))
					{
						int num = Math.Min(itemStack.Quantity, remaining);
						int num2 = itemStack.Quantity - num;
						array[j] = ((num2 == 0) ? ItemStack.Empty : itemStack.WithQuantity(item, num2));
						remaining -= num;
					}
				}
			}
		}
	}
}
