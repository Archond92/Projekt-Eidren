using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEngine;
using Random = System.Random;

namespace Eidren.Core.Services
{
	public static class WorldChestPopulationGenerator
	{
		public static WorldChestPlacement[] Generate(string zoneId, int zoneSeed, IReadOnlyList<WorldChestSpawnPointDefinition> points)
		{
			if (string.IsNullOrWhiteSpace(zoneId))
			{
				throw new ArgumentException("A stable zone ID is required.");
			}
			if (points == null)
			{
				throw new ArgumentNullException("points");
			}
			Random random = new Random(DeriveSeed(zoneSeed, "population"));
			List<WorldChestSpawnPointDefinition> list = new List<WorldChestSpawnPointDefinition>(points.Count);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (WorldChestSpawnPointDefinition point in points)
			{
				if (string.IsNullOrWhiteSpace(point.StableId) || !hashSet.Add(point.StableId))
				{
					throw new InvalidOperationException("Zone '" + zoneId + "' has an empty or duplicate chest point ID.");
				}
				list.Add(point);
			}
			List<WorldChestPlacement> list2 = new List<WorldChestPlacement>(6);
			Append(zoneId, zoneSeed, WorldChestFamily.Common, random.Next(2, 5), random, list, list2);
			Append(zoneId, zoneSeed, WorldChestFamily.Guarded, random.Next(0, 2), random, list, list2);
			Append(zoneId, zoneSeed, WorldChestFamily.Hidden, random.Next(0, 2), random, list, list2);
			return list2.ToArray();
		}

		public static int DeriveSeed(int zoneSeed, string scope)
		{
			uint num = 2166136261u;
			num = (num ^ (uint)zoneSeed) * 16777619;
			string text = scope ?? string.Empty;
			for (int i = 0; i < text.Length; i++)
			{
				num = (num ^ text[i]) * 16777619;
			}
			return (int)num;
		}

		private static void Append(string zoneId, int zoneSeed, WorldChestFamily family, int count, Random random, List<WorldChestSpawnPointDefinition> available, List<WorldChestPlacement> result)
		{
			for (int i = 0; i < count; i++)
			{
				List<int> list = new List<int>();
				for (int j = 0; j < available.Count; j++)
				{
					if (available[j].Allows(family))
					{
						list.Add(j);
					}
				}
				if (list.Count == 0)
				{
					throw new InvalidOperationException($"Zone '{zoneId}' has too few authored points for {family} chests.");
				}
				int index = list[random.Next(list.Count)];
				WorldChestSpawnPointDefinition worldChestSpawnPointDefinition = available[index];
				available.RemoveAt(index);
				string text = $"{zoneId}.world_chest.{family.ToString().ToLowerInvariant()}.{i + 1:00}";
				result.Add(new WorldChestPlacement(text, worldChestSpawnPointDefinition.StableId, family, worldChestSpawnPointDefinition.Position, worldChestSpawnPointDefinition.RotationY, DeriveSeed(zoneSeed, text)));
			}
		}
	}

	public readonly struct WorldChestPlacement
	{
		public string InstanceId { get; }

		public string SpawnPointId { get; }

		public WorldChestFamily Family { get; }

		public Vector3 Position { get; }

		public float RotationY { get; }

		public int ContentSeed { get; }

		public WorldChestPlacement(string instanceId, string spawnPointId, WorldChestFamily family, Vector3 position, float rotationY, int contentSeed)
		{
			InstanceId = instanceId ?? string.Empty;
			SpawnPointId = spawnPointId ?? string.Empty;
			Family = family;
			Position = position;
			RotationY = rotationY;
			ContentSeed = contentSeed;
		}
	}
}
