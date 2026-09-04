using Eidren.Core.Services;
using Eidren.Interaction;
using Eidren.UI;
using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class StorageWindowDepositTests
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
			_databaseObject = new GameObject("DepositDatabase_Test");
			_database = _databaseObject.AddComponent<ContentDatabase>();
			_sessionObject = new GameObject("DepositSession_Test");
			_session = _sessionObject.AddComponent<GameSession>();
			_session.Initialize();
			_session.ConfigureContentDatabase(_database);
			_session.StartNewGame();
			_inventory = _session.PlayerInventory;
			_inventory.Clear();
			_storageObject = new GameObject("DepositStorage_Test");
			_storage = _storageObject.AddComponent<StorageContainer>();
			_storage.Configure("test.storage.deposit", "Testlager", null, 2.5f);
			_storage.Initialize(_database, _session);
			_service = new ItemTransferService(_database);
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_storageObject);
			Object.DestroyImmediate(_sessionObject);
			Object.DestroyImmediate(_databaseObject);
		}

		[Test]
		public void GleichesAblegen_GiltNurFuerHeimatlager()
		{
			GameObject host = new GameObject("DepositRule_Test");
			try
			{
				Assert.That(StorageWindow.AllowsDepositMatching(host.AddComponent<StorageContainer>()), Is.True,
					"Heimatlagerkisten bieten GLEICHES ABLEGEN an");
				Assert.That(StorageWindow.AllowsDepositMatching(host.AddComponent<WorldChestContainer>()), Is.False,
					"Gebietskisten erhalten den Button nicht durch diesen Fix");
				Assert.That(StorageWindow.AllowsDepositMatching(host.AddComponent<EnemyLootContainer>()), Is.False,
					"Gegnerloot erhält den Button nicht durch diesen Fix");
				Assert.That(StorageWindow.AllowsDepositMatching(null), Is.False);
			}
			finally
			{
				Object.DestroyImmediate(host);
			}
		}

		[Test]
		public void GleichesAblegen_UebertraegtNurBereitsVorhandeneArten()
		{
			SetStorageSlot(0, "wood", 3);
			_inventory.Add("wood", 7);
			_inventory.Add("stone", 5);

			int moved = StorageWindow.DepositMatching(_service, _inventory, _storage);

			Assert.That(moved, Is.EqualTo(7), "Genau das vorhandene Holz wandert in die Kiste");
			Assert.That(Total(_storage, "wood"), Is.EqualTo(10), "Vorhandener Stapel wird aufgefüllt");
			Assert.That(_inventory.GetTotalAmount("wood"), Is.Zero, "Holz verlässt den Rucksack");
			Assert.That(Total(_storage, "stone"), Is.Zero, "Erz lag nicht in der Kiste und bleibt draußen");
			Assert.That(_inventory.GetTotalAmount("stone"), Is.EqualTo(5), "Erz bleibt unverändert im Rucksack");
		}

		[Test]
		public void GleichesAblegen_LaesstLeereKisteUnveraendert()
		{
			_inventory.Add("wood", 4);

			int moved = StorageWindow.DepositMatching(_service, _inventory, _storage);

			Assert.That(moved, Is.Zero, "Ohne vorhandene Gegenstandsart gibt es nichts abzulegen");
			Assert.That(_inventory.GetTotalAmount("wood"), Is.EqualTo(4), "Der Bestand bleibt unverändert");
		}

		[Test]
		public void GleichesAblegen_NimmtKeineArtAufDieErstWaehrendDerAktionFreiWird()
		{
			// Die Kiste kennt nur Holz. Auch wenn das Ablegen des Holzes einen
			// Rucksackplatz raeumt, darf Erz nicht nachruecken.
			SetStorageSlot(0, "wood", 1);
			_inventory.Add("wood", 2);
			_inventory.Add("stone", 9);

			StorageWindow.DepositMatching(_service, _inventory, _storage);

			Assert.That(Total(_storage, "stone"), Is.Zero, "Die erlaubten Arten werden vor der Aktion bestimmt");
			Assert.That(_inventory.GetTotalAmount("stone"), Is.EqualTo(9));
		}

		[Test]
		public void GleichesAblegen_VerliertNichtsBeiVollerKiste()
		{
			for (int index = 0; index < _storage.SlotCapacity; index++)
			{
				SetStorageSlot(index, "stone", 1);
			}
			_inventory.Add("wood", 6);
			_inventory.Add("stone", 6);
			int stoneBefore = Total(_storage, "stone") + _inventory.GetTotalAmount("stone");

			StorageWindow.DepositMatching(_service, _inventory, _storage);

			Assert.That(Total(_storage, "stone") + _inventory.GetTotalAmount("stone"), Is.EqualTo(stoneBefore),
				"Bei voller Kiste entstehen weder Verluste noch Duplikate");
			Assert.That(_inventory.GetTotalAmount("wood"), Is.EqualTo(6), "Holz lag nicht in der Kiste und bleibt im Rucksack");
		}

		private void SetStorageSlot(int index, string itemId, int amount)
		{
			ItemStack[] slots = _storage.ExportSlots();
			slots[index] = new ItemStack(itemId, amount);
			Assert.That(_storage.TryImportSlots(slots, out string error), Is.True, error);
		}

		private static int Total(IItemContainer container, string itemId)
		{
			int total = 0;
			for (int index = 0; index < container.SlotCapacity; index++)
			{
				if (container.TryGetSlot(index, out ItemStack stack) && stack.ItemId == itemId)
				{
					total += stack.Quantity;
				}
			}
			return total;
		}
	}
}
