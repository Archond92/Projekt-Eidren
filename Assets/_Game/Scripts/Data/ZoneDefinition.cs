using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Zones/Definition")]
	public sealed class ZoneDefinition : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private string displayName;

		[SerializeField]
		private string sceneKey;

		[SerializeField]
		private WorldRegionType regionType;

		[SerializeField]
		private ZoneCameraSettings camera = ZoneCameraSettings.Default;

		[SerializeField]
		private ZoneExitSides allowedExitSides = ZoneExitSides.All;

		[SerializeField]
		private bool safeZone;

		[SerializeField]
		private bool enemiesAllowed = true;

		[SerializeField]
		private bool resourcesAllowed = true;

		[SerializeField]
		private ResourceRespawnMode resourceRespawnMode = ResourceRespawnMode.OnZoneEntry;

		[SerializeField]
		private bool bossZone;

		[SerializeField]
		private string worldMapNodeId;

		[SerializeField]
		private ZoneAreaArtDefinition areaArt;

		[SerializeField]
		private ZoneResourceAllocation[] resourceAllocations = Array.Empty<ZoneResourceAllocation>();

		[SerializeField]
		private ZoneResourceAllocation[] sideNodeAllocations = Array.Empty<ZoneResourceAllocation>();

		[SerializeField]
		private ZoneEnemyAllocation[] enemyAllocations = Array.Empty<ZoneEnemyAllocation>();

		[SerializeField]
		private WorldChestLootProfile worldChestLootProfile;

		[SerializeField]
		private WorldChestSpawnPointDefinition[] worldChestSpawnPoints = Array.Empty<WorldChestSpawnPointDefinition>();

		public string Id => id;

		public string DisplayName => displayName;

		public string SceneKey => sceneKey;

		public WorldRegionType RegionType => regionType;

		public ZoneCameraSettings Camera => camera;

		public ZoneExitSides AllowedExitSides => allowedExitSides;

		public bool IsSafeZone => safeZone;

		public bool EnemiesAllowed => enemiesAllowed;

		public bool ResourcesAllowed => resourcesAllowed;

		public ResourceRespawnMode ResourceRespawnMode => resourceRespawnMode;

		public bool IsBossZone => bossZone;

		public string WorldMapNodeId => worldMapNodeId;

		public ZoneAreaArtDefinition AreaArt => areaArt;

		public IReadOnlyList<ZoneResourceAllocation> ResourceAllocations => resourceAllocations ?? Array.Empty<ZoneResourceAllocation>();

		public IReadOnlyList<ZoneResourceAllocation> SideNodeAllocations => sideNodeAllocations ?? Array.Empty<ZoneResourceAllocation>();

		public IReadOnlyList<ZoneEnemyAllocation> EnemyAllocations => enemyAllocations ?? Array.Empty<ZoneEnemyAllocation>();

		public WorldChestLootProfile WorldChestLootProfile => worldChestLootProfile;

		public IReadOnlyList<WorldChestSpawnPointDefinition> WorldChestSpawnPoints => worldChestSpawnPoints ?? Array.Empty<WorldChestSpawnPointDefinition>();
	}

	[Serializable]
	public struct ZoneCameraSettings
	{
		public Vector3 Offset;

		public Vector3 Rotation;

		[Min(1f)]
		public float BaseOrthographicSize;

		[Min(1f)]
		public float MinimumOrthographicSize;

		[Min(0f)]
		public float BoundsInset;

		public static ZoneCameraSettings Default => new ZoneCameraSettings
		{
			Offset = new Vector3(-8.3f, 15f, -8.3f),
			Rotation = new Vector3(52f, 45f, 0f),
			BaseOrthographicSize = 7.4f,
			MinimumOrthographicSize = 5.5f,
			BoundsInset = 4.8f
		};
	}

	[Serializable]
	public struct ZoneEnemyAllocation
	{
		[SerializeField]
		private EnemyDefinition definition;

		[SerializeField]
		[Min(0f)]
		private int count;

		public EnemyDefinition Definition => definition;

		public int Count => Mathf.Max(0, count);
	}

	[Flags]
	public enum ZoneExitSides
	{
		None = 0,
		North = 1,
		East = 2,
		South = 4,
		West = 8,
		All = 0xF
	}

	[Serializable]
	public struct ZoneResourceAllocation
	{
		[SerializeField]
		private ResourceNodeDefinition definition;

		[SerializeField]
		[Min(0f)]
		private int count;

		public ResourceNodeDefinition Definition => definition;

		public int Count => Mathf.Max(0, count);
	}
}
