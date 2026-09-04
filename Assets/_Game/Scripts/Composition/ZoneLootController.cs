using Eidren.AI;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.Loot;
using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class ZoneLootController : MonoBehaviour
	{
		[SerializeField]
		private ZoneController zone;

		[SerializeField]
		private Transform lootRoot;

		[SerializeField]
		private WorldItemController fallbackWorldItemPrefab;

		[SerializeField]
		private int deterministicSeed = 20491;

		private const float LootInteractionRange = InteractionUtility.StandardSurfaceRange;

		private readonly List<WildlingController> _wildlings = new List<WildlingController>();

		private readonly List<WorldItemController> _activeItems = new List<WorldItemController>();

		private readonly List<EnemyLootContainer> _lootContainers = new List<EnemyLootContainer>();

		private ContentDatabase _database;

		private PlayerProgressionService _progression;

		private System.Random _random;

		private int _deathSequence;

		private bool _initialized;

		public IReadOnlyList<WorldItemController> ActiveItems => _activeItems;

		public IReadOnlyList<EnemyLootContainer> LootContainers => _lootContainers;

		public Transform LootRoot => lootRoot;

		public event Action<EnemyLootContainer> LootContainerCreated;

		public void Configure(ZoneController configuredZone, Transform configuredLootRoot, WorldItemController configuredFallbackPrefab, int seed)
		{
			zone = configuredZone;
			lootRoot = configuredLootRoot;
			fallbackWorldItemPrefab = configuredFallbackPrefab;
			deterministicSeed = seed;
		}

		public void Initialize(ContentDatabase database)
		{
			if (_initialized)
			{
				return;
			}
			_database = database ?? throw new ArgumentNullException("database");
			_progression = EidrenServiceRoot.Instance?.PlayerProgression;
			if (zone == null || lootRoot == null || fallbackWorldItemPrefab == null)
			{
				throw new InvalidOperationException("ZoneLootController in '" + base.gameObject.scene.name + "' is missing its zone, LootRoot, or WorldItem prefab.");
			}
			_random = new System.Random(deterministicSeed ^ StableHash(zone.ZoneId));
			WildlingController[] componentsInChildren = zone.PopulationRoot.GetComponentsInChildren<WildlingController>(includeInactive: true);
			WildlingController[] array = componentsInChildren;
			foreach (WildlingController wildlingController in array)
			{
				if (!(wildlingController == null))
				{
					_wildlings.Add(wildlingController);
					wildlingController.LootRequested += HandleLootRequested;
				}
			}
			_initialized = true;
		}

		public int SpawnLoot(in WildlingLootRequest request)
		{
			if (!_initialized)
			{
				throw new InvalidOperationException("ZoneLootController must be initialized before spawning loot.");
			}
			_progression?.RecordEnemyDefeated(request.ExperienceReward);
			if (request.LootTable == null)
			{
				return 0;
			}
			IReadOnlyList<LootRollResult> readOnlyList = LootRoller.Roll(request.LootTable, _random);
			List<InventoryItemAmount> list = new List<InventoryItemAmount>(readOnlyList.Count);
			foreach (LootRollResult item in readOnlyList)
			{
				if (!_database.TryGetItem(item.ItemId, out var _))
				{
					Debug.LogError("Loot table '" + request.LootTable.Id + "' references unknown item ID '" + item.ItemId + "'.", this);
				}
				else
				{
					list.Add(new InventoryItemAmount(item.ItemId, item.Amount));
				}
			}
			if (list.Count == 0)
			{
				return 0;
			}
			int num = _deathSequence++;
			EnemyLootContainer enemyLootContainer = CreateLootContainer(in request, num);
			enemyLootContainer.Initialize(_database, $"{zone.ZoneId}.loot.{num}", list, 2.2f);
			enemyLootContainer.Emptied += HandleLootEmptied;
			_lootContainers.Add(enemyLootContainer);
			this.LootContainerCreated?.Invoke(enemyLootContainer);
			return list.Count;
		}

		private EnemyLootContainer CreateLootContainer(in WildlingLootRequest request, int deathIndex)
		{
			GameObject gameObject = new GameObject($"EnemyLoot_{deathIndex:000}");
			Transform parent = ((request.Source != null) ? request.Source : lootRoot);
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			if (request.Source == null)
			{
				gameObject.transform.position = request.Position;
			}
			else
			{
				gameObject.transform.localPosition = Vector3.zero;
			}
			SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
			sphereCollider.isTrigger = true;
			sphereCollider.radius = 2.2f;
			return gameObject.AddComponent<EnemyLootContainer>();
		}

		private void HandleLootEmptied(EnemyLootContainer container)
		{
			container.Emptied -= HandleLootEmptied;
			_lootContainers.Remove(container);
		}

		public int SpawnGuaranteedItems(IReadOnlyList<InventoryItemAmount> items, Vector3 origin, Transform source, string stablePrefix)
		{
			if (!_initialized)
			{
				throw new InvalidOperationException("ZoneLootController must be initialized before spawning guaranteed items.");
			}
			if (items == null || items.Count == 0)
			{
				return 0;
			}
			int num = _deathSequence++;
			int num2 = 0;
			for (int i = 0; i < items.Count; i++)
			{
				InventoryItemAmount inventoryItemAmount = items[i];
				if (inventoryItemAmount.Amount <= 0 || !_database.TryGetItem(inventoryItemAmount.ItemId, out var value))
				{
					Debug.LogError("Guaranteed zone reward references invalid item '" + inventoryItemAmount.ItemId + "'.", this);
					continue;
				}
				Vector3 position = origin;
				if (!LootDropPositionResolver.TryResolve(zone, origin, source, i, out position))
				{
					Debug.LogWarning("Using the boss death position for reward '" + inventoryItemAmount.ItemId + "' because no alternate safe drop position was found.", this);
				}
				WorldItemController original = ResolveWorldItemPrefab(value);
				WorldItemController worldItemController = UnityEngine.Object.Instantiate(original, position, Quaternion.identity, lootRoot);
				string arg = (string.IsNullOrWhiteSpace(stablePrefix) ? (zone.ZoneId + ".reward") : stablePrefix);
				worldItemController.name = $"WorldItem_{value.Id}_reward_{num:000}_{i:00}";
				worldItemController.Initialize(value, inventoryItemAmount.Amount, $"{arg}.{num}.{i}");
				EidrenServiceRoot.Instance?.AudioRuntimeBinder?.BindWorldItem(worldItemController);
				worldItemController.PickedUp += HandlePickedUp;
				_activeItems.Add(worldItemController);
				num2++;
			}
			return num2;
		}

		private WorldItemController ResolveWorldItemPrefab(ItemDefinition item)
		{
			if (item.WorldDropPrefab != null && item.WorldDropPrefab.TryGetComponent<WorldItemController>(out var component))
			{
				return component;
			}
			return fallbackWorldItemPrefab;
		}

		private void HandleLootRequested(WildlingLootRequest request)
		{
			SpawnLoot(in request);
		}

		private void HandlePickedUp(WorldItemPickupResult result)
		{
			for (int num = _activeItems.Count - 1; num >= 0; num--)
			{
				WorldItemController worldItemController = _activeItems[num];
				if (worldItemController == null || string.Equals(worldItemController.InteractionId, result.InstanceId, StringComparison.Ordinal))
				{
					if (worldItemController != null)
					{
						worldItemController.PickedUp -= HandlePickedUp;
					}
					_activeItems.RemoveAt(num);
				}
			}
		}

		private static int StableHash(string value)
		{
			uint num = 2166136261u;
			string text = value ?? string.Empty;
			for (int i = 0; i < text.Length; i++)
			{
				num ^= text[i];
				num *= 16777619;
			}
			return (int)num;
		}

		private void OnDestroy()
		{
			foreach (WildlingController wildling in _wildlings)
			{
				if (wildling != null)
				{
					wildling.LootRequested -= HandleLootRequested;
				}
			}
			foreach (WorldItemController activeItem in _activeItems)
			{
				if (activeItem != null)
				{
					activeItem.PickedUp -= HandlePickedUp;
				}
			}
			foreach (EnemyLootContainer lootContainer in _lootContainers)
			{
				if (lootContainer != null)
				{
					lootContainer.Emptied -= HandleLootEmptied;
				}
			}
		}
	}
}
