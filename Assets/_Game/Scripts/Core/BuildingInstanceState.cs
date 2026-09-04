using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	[Serializable]
	public sealed class BuildingInstanceState
	{
		public string InstanceId { get; }

		public string BuildingId { get; }

		public Vector3 Position { get; }

		public int QuarterTurns { get; }

		public int Level { get; }

		public int PlantedSlots { get; }

		public int ReadyOutput { get; }

		public float RotationDegrees => (float)QuarterTurns * 90f;

		public BuildingInstanceState(string instanceId, string buildingId, Vector3 position, int quarterTurns, int level)
			: this(instanceId, buildingId, position, quarterTurns, level, 0, 0)
		{
		}

		public BuildingInstanceState(string instanceId, string buildingId, Vector3 position, int quarterTurns, int level, int plantedSlots, int readyOutput)
		{
			InstanceId = instanceId ?? string.Empty;
			BuildingId = buildingId ?? string.Empty;
			Position = position;
			QuarterTurns = NormalizeQuarterTurns(quarterTurns);
			Level = level;
			PlantedSlots = Math.Max(0, plantedSlots);
			ReadyOutput = Math.Max(0, readyOutput);
		}

		public BuildingInstanceState Copy()
		{
			return new BuildingInstanceState(InstanceId, BuildingId, Position, QuarterTurns, Level, PlantedSlots, ReadyOutput);
		}

		public BuildingInstanceState WithProduction(int plantedSlots, int readyOutput)
		{
			return new BuildingInstanceState(InstanceId, BuildingId, Position, QuarterTurns, Level, plantedSlots, readyOutput);
		}

		private static int NormalizeQuarterTurns(int value)
		{
			return (value % 4 + 4) % 4;
		}
	}
}
