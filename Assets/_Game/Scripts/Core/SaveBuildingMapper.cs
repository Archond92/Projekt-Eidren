using Eidren.Core.BuildGrid;
using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public static class SaveBuildingMapper
	{
		public static SaveBuildingData[] ToSave(IReadOnlyList<BuildingInstanceState> source, ContentDatabase content = null)
		{
			if (source == null)
			{
				return Array.Empty<SaveBuildingData>();
			}
			SaveBuildingData[] array = new SaveBuildingData[source.Count];
			for (int i = 0; i < source.Count; i++)
			{
				BuildingInstanceState buildingInstanceState = source[i];
				BuildGridOrigin homeBase = BuildGridOrigin.HomeBase;
				GridCoordinate gridCoordinate = homeBase.ToCell(buildingInstanceState.Position.x, buildingInstanceState.Position.z);
				array[i] = new SaveBuildingData
				{
					instanceId = buildingInstanceState.InstanceId,
					buildingId = buildingInstanceState.BuildingId,
					cellX = gridCoordinate.X,
					cellZ = gridCoordinate.Z,
					positionY = buildingInstanceState.Position.y,
					quarterTurns = buildingInstanceState.QuarterTurns,
					level = buildingInstanceState.Level,
					placementKind = (int)KindOf(buildingInstanceState.BuildingId, content),
					edgeOrientation = (int)BuildGridOrigin.OrientationFor(buildingInstanceState.QuarterTurns),
					plantedSlots = buildingInstanceState.PlantedSlots,
					readyOutput = buildingInstanceState.ReadyOutput
				};
			}
			return array;
		}

		public static BuildingInstanceState[] ToRuntime(IReadOnlyList<SaveBuildingData> source, ContentDatabase content, ICollection<SaveQuarantineEntry> quarantine)
		{
			if (source == null)
			{
				return Array.Empty<BuildingInstanceState>();
			}
			List<BuildingInstanceState> list = new List<BuildingInstanceState>();
			HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
			foreach (SaveBuildingData item in source)
			{
				if (!TryMap(item, content, ids, out var state))
				{
					quarantine?.Add(new SaveQuarantineEntry
					{
						source = "buildings",
						stableId = (item?.instanceId ?? string.Empty),
						reason = "Invalid building ID, level, position, or duplicate instance ID."
					});
				}
				else
				{
					list.Add(state);
				}
			}
			return list.ToArray();
		}

		private static bool TryMap(SaveBuildingData saved, ContentDatabase content, ISet<string> ids, out BuildingInstanceState state)
		{
			state = null;
			if (saved != null && !string.IsNullOrWhiteSpace(saved.instanceId) && ids.Add(saved.instanceId) && !string.IsNullOrWhiteSpace(saved.buildingId) && !(content == null) && content.TryGetBuilding(saved.buildingId, out var value) && value.TryGetLevel(saved.level, out var _, out var _) && saved.plantedSlots >= 0 && saved.readyOutput >= 0)
			{
				bool num;
				if (!string.Equals(saved.buildingId, "building.farm_plot", StringComparison.Ordinal))
				{
					if (saved.plantedSlots != 0)
					{
						goto IL_00c3;
					}
					num = saved.readyOutput != 0;
				}
				else
				{
					num = saved.plantedSlots > 4;
				}
				if (!num && IsFinite(saved.positionY))
				{
					BuildGridOrigin homeBase = BuildGridOrigin.HomeBase;
					homeBase.ToWorldCentre(new GridCoordinate(saved.cellX, saved.cellZ), out var worldX, out var worldZ);
					state = new BuildingInstanceState(saved.instanceId, saved.buildingId, new Vector3(worldX, saved.positionY, worldZ), saved.quarterTurns, saved.level, saved.plantedSlots, saved.readyOutput);
					return true;
				}
			}
			goto IL_00c3;
			IL_00c3:
			return false;
		}

		private static BuildingPlacementKind KindOf(string buildingId, ContentDatabase content)
		{
			BuildingCostDefinition value;
			return (content != null && content.TryGetBuilding(buildingId, out value)) ? value.PlacementKind : BuildingPlacementKind.Object;
		}

		private static bool IsFinite(float value)
		{
			return !float.IsNaN(value) && !float.IsInfinity(value);
		}
	}
}
