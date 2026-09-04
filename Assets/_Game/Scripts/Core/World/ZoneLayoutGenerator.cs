using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEngine;
using Random = System.Random;

namespace Eidren.Core.Services
{
	public static class ZoneLayoutGenerator
	{
		public const int GeneratorVersion = 2;

		private const int WheatSeedNodeInterval = 3;

		private const float PreferredSpacing = 3.25f;

		private const float MinimumSpacing = 1.6f;

		private const float SpacingRelaxation = 0.8f;

		private const float NodeClearance = 1.1f;

		private const float WorldChestClearance = 3.6f;

		private const float WorldChestMinimumClearance = 2.5f;

		private const int AttemptsPerSpacing = 48;

		private const int GridFallbackSteps = 24;

		public static ZoneLayout Generate(ZoneDefinition zone, ZoneState state, Bounds area, IZonePlacementSpace space)
		{
			if (zone == null)
			{
				throw new ArgumentNullException("zone");
			}
			if (state == null)
			{
				throw new ArgumentNullException("state");
			}
			if (space == null)
			{
				throw new ArgumentNullException("space");
			}
			System.Random random = new System.Random(state.Seed);
			List<ZoneNodePlacement> list = new List<ZoneNodePlacement>();
			List<Vector3> occupied = new List<Vector3>();
			Vector3[] chestPoints = CollectWorldChestPoints(zone);
			AppendAllocations(zone.ResourceAllocations, economyNodes: true, zone.Id, random, area, space, list, occupied, chestPoints);
			AppendAllocations(zone.SideNodeAllocations, economyNodes: false, zone.Id, random, area, space, list, occupied, chestPoints);
			List<ZoneEidraPlacement> list2 = AppendEidra(zone, random, area, space, occupied, chestPoints);
			return new ZoneLayout(zone.Id, state.Seed, state.GeneratorVersion, list.ToArray(), list2.ToArray());
		}

		private static Vector3[] CollectWorldChestPoints(ZoneDefinition zone)
		{
			IReadOnlyList<WorldChestSpawnPointDefinition> worldChestSpawnPoints = zone.WorldChestSpawnPoints;
			if (worldChestSpawnPoints.Count == 0)
			{
				return Array.Empty<Vector3>();
			}
			Vector3[] array = new Vector3[worldChestSpawnPoints.Count];
			for (int i = 0; i < worldChestSpawnPoints.Count; i++)
			{
				array[i] = worldChestSpawnPoints[i].Position;
			}
			return array;
		}

		private static List<ZoneEidraPlacement> AppendEidra(ZoneDefinition zone, System.Random random, Bounds area, IZonePlacementSpace space, List<Vector3> occupied, IReadOnlyList<Vector3> chestPoints)
		{
			List<ZoneEidraPlacement> list = new List<ZoneEidraPlacement>();
			if (!zone.EnemiesAllowed)
			{
				return list;
			}
			foreach (ZoneEnemyAllocation enemyAllocation in zone.EnemyAllocations)
			{
				EnemyDefinition definition = enemyAllocation.Definition;
				if (!(definition == null) && definition.IsCapturableEidra && enemyAllocation.Count > 0)
				{
					for (int i = 0; i < enemyAllocation.Count; i++)
					{
						Vector3 vector = ResolvePosition(random, area, space, occupied, chestPoints);
						occupied.Add(vector);
						list.Add(new ZoneEidraPlacement(definition, EidraInstanceId(zone.Id, definition.Id, i), vector, (float)random.NextDouble() * 360f));
					}
				}
			}
			return list;
		}

		public static string EidraInstanceId(string zoneId, string enemyId, int index)
		{
			return $"{zoneId}.eidra.{enemyId}.{index + 1:000}";
		}

		private static void AppendAllocations(IReadOnlyList<ZoneResourceAllocation> allocations, bool economyNodes, string zoneId, System.Random random, Bounds area, IZonePlacementSpace space, List<ZoneNodePlacement> placements, List<Vector3> occupied, IReadOnlyList<Vector3> chestPoints)
		{
			foreach (ZoneResourceAllocation allocation in allocations)
			{
				ResourceNodeDefinition definition = allocation.Definition;
				if (!(definition == null) && allocation.Count > 0)
				{
					bool flag = IsFiberNode(definition);
					for (int i = 0; i < allocation.Count; i++)
					{
						Vector3 vector = ResolvePosition(random, area, space, occupied, chestPoints);
						occupied.Add(vector);
						placements.Add(new ZoneNodePlacement(definition, NodeInstanceId(zoneId, definition.Id, i), vector, (float)random.NextDouble() * 360f, economyNodes, flag && (i + 1) % 3 == 0));
					}
				}
			}
		}

		public static string NodeInstanceId(string zoneId, string resourceNodeId, int index)
		{
			return $"{zoneId}.{resourceNodeId}.{index + 1:000}";
		}

		private static bool IsFiberNode(ResourceNodeDefinition definition)
		{
			if (definition.OutputItem != null)
			{
				return string.Equals(definition.OutputItem.MaterialFamily, "fiber", StringComparison.Ordinal);
			}
			return false;
		}

		private static Vector3 ResolvePosition(System.Random random, Bounds area, IZonePlacementSpace space, IReadOnlyList<Vector3> occupied, IReadOnlyList<Vector3> chestPoints)
		{
			for (float num = 3.25f; num >= 1.6f; num *= 0.8f)
			{
				for (int i = 0; i < 48; i++)
				{
					Vector3 vector = RandomPoint(random, area);
					if (IsFree(vector, num, space, occupied, chestPoints, 3.6f))
					{
						return vector;
					}
				}
			}
			return GridFallback(random, area, space, occupied, chestPoints);
		}

		private static Vector3 GridFallback(System.Random random, Bounds area, IZonePlacementSpace space, IReadOnlyList<Vector3> occupied, IReadOnlyList<Vector3> chestPoints)
		{
			int offset = random.Next(0, 24);
			if (TryScanGrid(offset, area, space, occupied, chestPoints, 3.6f, out var position))
			{
				return position;
			}
			if (TryScanGrid(offset, area, space, occupied, chestPoints, 2.5f, out position))
			{
				return position;
			}
			throw new InvalidOperationException("The zone has no free placement left for its node budget, not even once the world-chest clearance falls back to the bare interaction radius. Either the playable area shrank or the allocation grew beyond what M1.7 assumes.");
		}

		private static bool TryScanGrid(int offset, Bounds area, IZonePlacementSpace space, IReadOnlyList<Vector3> occupied, IReadOnlyList<Vector3> chestPoints, float chestClearance, out Vector3 position)
		{
			for (int i = 0; i < 24; i++)
			{
				int num2 = (i + offset) % 24;
				float z = Mathf.Lerp(area.min.z, area.max.z, ((float)num2 + 0.5f) / 24f);
				for (int j = 0; j < 24; j++)
				{
					float x = Mathf.Lerp(area.min.x, area.max.x, ((float)j + 0.5f) / 24f);
					Vector3 vector = new Vector3(x, area.center.y, z);
					if (IsFree(vector, 1.6f, space, occupied, chestPoints, chestClearance))
					{
						position = vector;
						return true;
					}
				}
			}
			position = Vector3.zero;
			return false;
		}

		private static bool IsFree(Vector3 candidate, float spacing, IZonePlacementSpace space, IReadOnlyList<Vector3> occupied, IReadOnlyList<Vector3> chestPoints, float chestClearance)
		{
			float num = spacing * spacing;
			foreach (Vector3 item in occupied)
			{
				if ((item - candidate).sqrMagnitude < num)
				{
					return false;
				}
			}
			float num2 = chestClearance * chestClearance;
			foreach (Vector3 chestPoint in chestPoints)
			{
				float num3 = chestPoint.x - candidate.x;
				float num4 = chestPoint.z - candidate.z;
				if (num3 * num3 + num4 * num4 < num2)
				{
					return false;
				}
			}
			return space.IsPlaceable(candidate, 1.1f);
		}

		private static Vector3 RandomPoint(System.Random random, Bounds area)
		{
			return new Vector3(Mathf.Lerp(area.min.x, area.max.x, (float)random.NextDouble()), area.center.y, Mathf.Lerp(area.min.z, area.max.z, (float)random.NextDouble()));
		}
	}
}
