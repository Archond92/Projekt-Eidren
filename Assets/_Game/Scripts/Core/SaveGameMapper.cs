using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class SaveGameMapper
	{
		private const int InventorySlots = 16;

		private readonly ContentDatabase _content;

		public SaveGameMapper(ContentDatabase content)
		{
			_content = content ?? throw new ArgumentNullException("content");
		}

		public SaveGameData Capture(GameSession session, string createdUtc, string savedUtc, string buildVersion)
		{
			GameSessionRuntimeState gameSessionRuntimeState = session.ExportRuntimeState();
			return new SaveGameData
			{
				createdUtc = createdUtc,
				lastSavedUtc = savedUtc,
				buildVersion = buildVersion,
				player = new SavePlayerData
				{
					inventory = ToSaveStacks(gameSessionRuntimeState.Inventory),
					activeWeaponId = gameSessionRuntimeState.ActiveWeaponId,
					activeEidraId = gameSessionRuntimeState.ActiveEidraId,
					lastSafeNodeId = gameSessionRuntimeState.LastSafeNodeId,
					lastSafeSceneKey = gameSessionRuntimeState.LastSafeSceneKey,
					weaponUpgrades = ToSaveUpgrades(gameSessionRuntimeState.WeaponUpgrades),
					equipment = SaveEquipmentMapper.ToSave(gameSessionRuntimeState.Equipment),
					eidraRoster = SaveEidraMapper.ToSave(gameSessionRuntimeState.EidraRoster),
					activeEidraInstanceIds = gameSessionRuntimeState.ActiveEidraInstanceIds
				},
				deathBag = SaveEquipmentMapper.ToSave(gameSessionRuntimeState.DeathBag),
				storageContainers = ToSaveStorage(gameSessionRuntimeState.StorageContainers),
				buildings = SaveBuildingMapper.ToSave(gameSessionRuntimeState.Buildings, _content),
				eidraForge = SaveEidraForgeMapper.ToSave(gameSessionRuntimeState.EidraForge),
				zones = SaveZoneStateMapper.ToSave(gameSessionRuntimeState.ZoneStates),
				world = new SaveWorldData
				{
					currentNodeId = gameSessionRuntimeState.CurrentNodeId,
					selectedNodeId = gameSessionRuntimeState.SelectedNodeId,
					visitedNodeIds = gameSessionRuntimeState.VisitedNodeIds,
					eventNodeIds = gameSessionRuntimeState.EventNodeIds,
					progressFlags = gameSessionRuntimeState.ProgressFlags,
					completedNodeIds = gameSessionRuntimeState.CompletedNodeIds
				}
			};
		}

		public bool TryMapToRuntime(SaveGameData save, out GameSessionRuntimeState state, out SaveQuarantineEntry[] quarantine, out string error)
		{
			state = null;
			List<SaveQuarantineEntry> list = new List<SaveQuarantineEntry>();
			if (save?.player == null || save.world == null)
			{
				quarantine = Array.Empty<SaveQuarantineEntry>();
				error = "Save data is missing player or world state.";
				return false;
			}
			ItemStack[] inventory = MapStacks(save.player.inventory, 16, "player.inventory", list);
			StorageContainerState[] storageContainers = MapStorage(save.storageContainers, list);
			QuarantineUnknownNode(save.world.currentNodeId, "world.current", list);
			QuarantineUnknownNode(save.world.selectedNodeId, "world.selected", list);
			QuarantineUnknownNode(save.player.lastSafeNodeId, "player.lastSafeNode", list);
			string activeWeaponId = ValidateWeapon(save.player.activeWeaponId);
			string activeEidraId = ValidateEidra(save.player.activeEidraId);
			EidraInstanceState[] array = SaveEidraMapper.ToRuntime(save.player.eidraRoster, _content, list);
			state = new GameSessionRuntimeState
			{
				EidraRoster = array,
				ActiveEidraInstanceIds = SaveEidraMapper.ActiveToRuntime(save.player.activeEidraInstanceIds, array, list),
				Equipment = SaveEquipmentMapper.ToRuntime(save.player.equipment, _content, list),
				DeathBag = SaveEquipmentMapper.ToRuntime(save.deathBag),
				ActiveWeaponId = activeWeaponId,
				ActiveEidraId = activeEidraId,
				LastSafeNodeId = "home_base",
				LastSafeSceneKey = "HomeBase",
				CurrentNodeId = "home_base",
				SelectedNodeId = "home_base",
				Inventory = inventory,
				StorageContainers = storageContainers,
				Buildings = SaveBuildingMapper.ToRuntime(save.buildings, _content, list),
				ZoneStates = SaveZoneStateMapper.ToRuntime(save.zones, list),
				EidraForge = SaveEidraForgeMapper.ToRuntime(save.eidraForge, _content, list),
				WeaponUpgrades = MapUpgrades(save.player.weaponUpgrades, list),
				VisitedNodeIds = FilterNodeIds(save.world.visitedNodeIds, list, "world.visited"),
				EventNodeIds = FilterNodeIds(save.world.eventNodeIds, list, "world.events"),
				ProgressFlags = CleanIds(save.world.progressFlags),
				CompletedNodeIds = FilterNodeIds(save.world.completedNodeIds, list, "world.completed")
			};
			quarantine = list.ToArray();
			error = string.Empty;
			return true;
		}

		private ItemStack[] MapStacks(SaveItemStackData[] source, int capacity, string context, List<SaveQuarantineEntry> quarantine)
		{
			ItemStack[] array = new ItemStack[capacity];
			if (source == null)
			{
				return array;
			}
			int num = Math.Min(capacity, source.Length);
			for (int i = 0; i < num; i++)
			{
				SaveItemStackData saveItemStackData = source[i];
				if (saveItemStackData != null && saveItemStackData.quantity != 0)
				{
					if (saveItemStackData.quantity < 0 || saveItemStackData.durability < 0 || string.IsNullOrWhiteSpace(saveItemStackData.itemId) || !_content.TryGetItem(saveItemStackData.itemId, out var value) || saveItemStackData.quantity > value.MaximumStackSize)
					{
						Quarantine(quarantine, $"{context}[{i}]", saveItemStackData?.itemId, "Unknown item ID or invalid stack quantity.");
					}
					else
					{
						array[i] = new ItemStack(saveItemStackData.itemId, saveItemStackData.quantity, saveItemStackData.instanceId, saveItemStackData.durability);
					}
				}
			}
			return array;
		}

		private StorageContainerState[] MapStorage(SaveStorageData[] source, List<SaveQuarantineEntry> quarantine)
		{
			if (source == null)
			{
				return Array.Empty<StorageContainerState>();
			}
			List<StorageContainerState> list = new List<StorageContainerState>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (SaveStorageData saveStorageData in source)
			{
				if (saveStorageData == null || string.IsNullOrWhiteSpace(saveStorageData.containerId) || !hashSet.Add(saveStorageData.containerId))
				{
					Quarantine(quarantine, "storage", saveStorageData?.containerId, "Missing or duplicate container ID.");
				}
				else
				{
					list.Add(new StorageContainerState(saveStorageData.containerId, MapStacks(saveStorageData.slots, 24, "storage." + saveStorageData.containerId, quarantine)));
				}
			}
			return list.ToArray();
		}

		private WeaponUpgradeRuntimeState[] MapUpgrades(SaveWeaponUpgradeData[] source, List<SaveQuarantineEntry> quarantine)
		{
			if (source == null)
			{
				return Array.Empty<WeaponUpgradeRuntimeState>();
			}
			List<WeaponUpgradeRuntimeState> list = new List<WeaponUpgradeRuntimeState>();
			foreach (SaveWeaponUpgradeData saveWeaponUpgradeData in source)
			{
				if (saveWeaponUpgradeData == null || saveWeaponUpgradeData.level <= 1 || string.IsNullOrWhiteSpace(saveWeaponUpgradeData.weaponId) || !_content.TryGetWeapon(saveWeaponUpgradeData.weaponId, out var _))
				{
					Quarantine(quarantine, "player.weaponUpgrades", saveWeaponUpgradeData?.weaponId, "Unknown weapon or invalid upgrade level.");
				}
				else
				{
					list.Add(new WeaponUpgradeRuntimeState(saveWeaponUpgradeData.weaponId, saveWeaponUpgradeData.level, Math.Max(1f, saveWeaponUpgradeData.healthDamageMultiplier), Math.Max(1f, saveWeaponUpgradeData.staggerDamageMultiplier)));
				}
			}
			return list.ToArray();
		}

		private string ValidateWeapon(string id)
		{
			if (string.IsNullOrWhiteSpace(id) || !_content.TryGetWeapon(id, out var _))
			{
				return string.Empty;
			}
			return id;
		}

		private string ValidateEidra(string id)
		{
			if (string.IsNullOrWhiteSpace(id) || !_content.TryGetEidra(id, out var _))
			{
				return string.Empty;
			}
			return id;
		}

		private static string[] FilterNodeIds(string[] source, List<SaveQuarantineEntry> quarantine, string context)
		{
			if (source == null)
			{
				return Array.Empty<string>();
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (string text in source)
			{
				if (WorldMapNodeIds.IsKnown(text))
				{
					hashSet.Add(text);
				}
				else if (!string.IsNullOrWhiteSpace(text))
				{
					Quarantine(quarantine, context, text, "Unknown world-map node ID.");
				}
			}
			string[] array = new string[hashSet.Count];
			hashSet.CopyTo(array);
			Array.Sort(array, StringComparer.Ordinal);
			return array;
		}

		private static string[] CleanIds(string[] source)
		{
			if (source == null)
			{
				return Array.Empty<string>();
			}
			HashSet<string> hashSet = new HashSet<string>(source, StringComparer.Ordinal);
			hashSet.RemoveWhere(string.IsNullOrWhiteSpace);
			string[] array = new string[hashSet.Count];
			hashSet.CopyTo(array);
			Array.Sort(array, StringComparer.Ordinal);
			return array;
		}

		private static void QuarantineUnknownNode(string id, string context, ICollection<SaveQuarantineEntry> quarantine)
		{
			if (!string.IsNullOrWhiteSpace(id) && !WorldMapNodeIds.IsKnown(id))
			{
				Quarantine(quarantine, context, id, "Unknown world-map node ID; HomeBase fallback used.");
			}
		}

		private static SaveItemStackData[] ToSaveStacks(ItemStack[] source)
		{
			if (source == null)
			{
				return Array.Empty<SaveItemStackData>();
			}
			SaveItemStackData[] array = new SaveItemStackData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				ItemStack itemStack = source[i];
				array[i] = new SaveItemStackData
				{
					itemId = itemStack.ItemId,
					quantity = itemStack.Quantity,
					instanceId = itemStack.InstanceId,
					durability = itemStack.Durability
				};
			}
			return array;
		}

		private static SaveStorageData[] ToSaveStorage(StorageContainerState[] source)
		{
			if (source == null)
			{
				return Array.Empty<SaveStorageData>();
			}
			SaveStorageData[] array = new SaveStorageData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = new SaveStorageData
				{
					containerId = source[i].ContainerId,
					slots = ToSaveStacks(source[i].Slots)
				};
			}
			return array;
		}

		private static SaveWeaponUpgradeData[] ToSaveUpgrades(WeaponUpgradeRuntimeState[] source)
		{
			if (source == null)
			{
				return Array.Empty<SaveWeaponUpgradeData>();
			}
			SaveWeaponUpgradeData[] array = new SaveWeaponUpgradeData[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				WeaponUpgradeRuntimeState weaponUpgradeRuntimeState = source[i];
				array[i] = new SaveWeaponUpgradeData
				{
					weaponId = weaponUpgradeRuntimeState.WeaponId,
					level = weaponUpgradeRuntimeState.Level,
					healthDamageMultiplier = weaponUpgradeRuntimeState.HealthDamageMultiplier,
					staggerDamageMultiplier = weaponUpgradeRuntimeState.StaggerDamageMultiplier
				};
			}
			return array;
		}

		private static void Quarantine(ICollection<SaveQuarantineEntry> target, string source, string id, string reason)
		{
			target.Add(new SaveQuarantineEntry
			{
				source = (source ?? string.Empty),
				stableId = (id ?? string.Empty),
				reason = (reason ?? string.Empty)
			});
		}
	}
}
