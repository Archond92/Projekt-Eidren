using System;

namespace Eidren.Core.Services
{
	public sealed class GameSessionRuntimeState
	{
		public string ActiveWeaponId = "hammer";

		public string ActiveEidraId = "terrock";

		public string LastSafeNodeId = "home_base";

		public string LastSafeSceneKey = "HomeBase";

		public string CurrentNodeId = "home_base";

		public string SelectedNodeId = "home_base";

		public ItemStack[] Inventory = Array.Empty<ItemStack>();

		public EquipmentAssignment[] Equipment = Array.Empty<EquipmentAssignment>();

		public DeathBagLocation DeathBag = DeathBagLocation.None;

		public StorageContainerState[] StorageContainers = Array.Empty<StorageContainerState>();

		public BuildingInstanceState[] Buildings = Array.Empty<BuildingInstanceState>();

		public EidraInstanceState[] EidraRoster = Array.Empty<EidraInstanceState>();

		public string[] ActiveEidraInstanceIds = Array.Empty<string>();

		public ZoneState[] ZoneStates = Array.Empty<ZoneState>();

		public WeaponUpgradeRuntimeState[] WeaponUpgrades = Array.Empty<WeaponUpgradeRuntimeState>();

		public string[] VisitedNodeIds = Array.Empty<string>();

		public string[] EventNodeIds = Array.Empty<string>();

		public string[] ProgressFlags = Array.Empty<string>();

		public string[] CompletedNodeIds = Array.Empty<string>();

		public EidraForgeRunState EidraForge = EidraForgeRunState.CreateUnstarted();
	}

	public readonly struct WeaponUpgradeRuntimeState
	{
		public string WeaponId { get; }

		public int Level { get; }

		public float HealthDamageMultiplier { get; }

		public float StaggerDamageMultiplier { get; }

		public WeaponUpgradeRuntimeState(string weaponId, int level, float healthDamageMultiplier, float staggerDamageMultiplier)
		{
			WeaponId = weaponId ?? string.Empty;
			Level = level;
			HealthDamageMultiplier = healthDamageMultiplier;
			StaggerDamageMultiplier = staggerDamageMultiplier;
		}
	}
}
