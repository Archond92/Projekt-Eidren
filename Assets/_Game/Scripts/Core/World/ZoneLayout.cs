using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Core.Services
{
	public sealed class ZoneLayout
	{
		private readonly ZoneNodePlacement[] _placements;

		private readonly ZoneEidraPlacement[] _eidraPlacements;

		public string ZoneId { get; }

		public int Seed { get; }

		public int GeneratorVersion { get; }

		public IReadOnlyList<ZoneNodePlacement> Placements => _placements;

		public IReadOnlyList<ZoneEidraPlacement> EidraPlacements => _eidraPlacements;

		public int EconomyNodeCount
		{
			get
			{
				int num = 0;
				ZoneNodePlacement[] placements = _placements;
				foreach (ZoneNodePlacement zoneNodePlacement in placements)
				{
					if (zoneNodePlacement.IsEconomyNode)
					{
						num++;
					}
				}
				return num;
			}
		}

		public int WheatSeedNodeCount
		{
			get
			{
				int num = 0;
				ZoneNodePlacement[] placements = _placements;
				foreach (ZoneNodePlacement zoneNodePlacement in placements)
				{
					if (zoneNodePlacement.CarriesWheatSeed)
					{
						num++;
					}
				}
				return num;
			}
		}

		public ZoneLayout(string zoneId, int seed, int generatorVersion, ZoneNodePlacement[] placements, ZoneEidraPlacement[] eidraPlacements = null)
		{
			ZoneId = zoneId ?? string.Empty;
			Seed = seed;
			GeneratorVersion = generatorVersion;
			_placements = placements ?? Array.Empty<ZoneNodePlacement>();
			_eidraPlacements = eidraPlacements ?? Array.Empty<ZoneEidraPlacement>();
		}
	}

	public readonly struct ZoneEidraPlacement
	{
		public EnemyDefinition Definition { get; }

		public string InstanceId { get; }

		public Vector3 Position { get; }

		public float RotationY { get; }

		public EidraData Eidra
		{
			get
			{
				if (!(Definition != null))
				{
					return null;
				}
				return Definition.CapturableEidra;
			}
		}

		public ZoneEidraPlacement(EnemyDefinition definition, string instanceId, Vector3 position, float rotationY)
		{
			Definition = definition;
			InstanceId = instanceId ?? string.Empty;
			Position = position;
			RotationY = rotationY;
		}
	}

	public readonly struct ZoneNodePlacement
	{
		public ResourceNodeDefinition Definition { get; }

		public string NodeInstanceId { get; }

		public Vector3 Position { get; }

		public float RotationY { get; }

		public bool IsEconomyNode { get; }

		public bool CarriesWheatSeed { get; }

		public ZoneNodePlacement(ResourceNodeDefinition definition, string nodeInstanceId, Vector3 position, float rotationY, bool isEconomyNode, bool carriesWheatSeed)
		{
			Definition = definition;
			NodeInstanceId = nodeInstanceId ?? string.Empty;
			Position = position;
			RotationY = rotationY;
			IsEconomyNode = isEconomyNode;
			CarriesWheatSeed = carriesWheatSeed;
		}
	}
}
