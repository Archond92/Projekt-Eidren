using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class ZoneResourcePopulator
	{
		private const string ResourcesRootName = "Resources";

		private const float PlacementInset = 6f;

		private readonly ZoneController _zone;

		private readonly GameSession _session;

		private readonly ItemDefinition _wheatSeed;

		private readonly PlayerProgressionService _progression;

		private readonly List<ResourceNode> _spawned = new List<ResourceNode>();

		public IReadOnlyList<ResourceNode> SpawnedNodes => _spawned;

		public ZoneResourcePopulator(ZoneController zone, GameSession session, ContentDatabase content, PlayerProgressionService progression)
		{
			_zone = zone;
			_session = session;
			_progression = progression;
			_wheatSeed = ((content != null && content.TryGetItem("wheat_seed", out var value)) ? value : null);
		}

		public ZoneLayout Populate()
		{
			if (_zone == null || _zone.Definition == null || _zone.PopulationRoot == null || !_zone.Definition.ResourcesAllowed)
			{
				return null;
			}
			Physics.SyncTransforms();
			Transform root = ResolveResourcesRoot();
			ClearExistingNodes(root);
			ZoneState orCreate = _session.ZoneStates.GetOrCreate(_zone.Definition.Id, 2);
			ZoneLayout zoneLayout = ZoneLayoutGenerator.Generate(_zone.Definition, orCreate, PlacementArea(), new ZonePhysicsPlacementSpace(_zone));
			foreach (ZoneNodePlacement placement in zoneLayout.Placements)
			{
				Spawn(placement, root, orCreate);
			}
			Physics.SyncTransforms();
			return zoneLayout;
		}

		private void Spawn(in ZoneNodePlacement placement, Transform root, ZoneState state)
		{
			GameObject nodePrefab = placement.Definition.NodePrefab;
			if (nodePrefab == null)
			{
				Debug.LogError("Resource '" + placement.Definition.Id + "' has no node prefab; the zone cannot build it from the seed.", nodePrefab);
				return;
			}
			GameObject gameObject = Object.Instantiate(nodePrefab, placement.Position, Quaternion.Euler(0f, placement.RotationY, 0f), root);
			gameObject.name = placement.NodeInstanceId;
			ResourceNode component = gameObject.GetComponent<ResourceNode>();
			if (!(component == null))
			{
				ResourceNodeVariantApplicator.Apply(component, _zone.Definition.AreaArt);
				component.ConfigureInstanceId(placement.NodeInstanceId, _zone.Definition.ResourceRespawnMode);
				component.ConfigureByproduct(placement.CarriesWheatSeed ? _wheatSeed : null, 1);
				if (state.IsHarvested(placement.NodeInstanceId))
				{
					component.MarkAlreadyHarvested();
				}
				component.Collected += HandleCollected;
				_spawned.Add(component);
			}
		}

		private void HandleCollected(ResourceCollectionResult result)
		{
			_session.ZoneStates.MarkHarvested(_zone.Definition.Id, result.NodeInstanceId);
			_progression?.RecordResourceNodeCompleted(result.Item.Tier, result.NodeDefinitionId);
		}

		private Bounds PlacementArea()
		{
			Bounds bounds = ((_zone.WalkableGround != null) ? _zone.WalkableGround.bounds : new Bounds(Vector3.zero, Vector3.one * 60f));
			return new Bounds(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z), new Vector3(Mathf.Max(1f, bounds.size.x - 12f), 0f, Mathf.Max(1f, bounds.size.z - 12f)));
		}

		private Transform ResolveResourcesRoot()
		{
			Transform transform = _zone.PopulationRoot.Find("Resources");
			if (transform != null)
			{
				return transform;
			}
			GameObject gameObject = new GameObject("Resources");
			gameObject.transform.SetParent(_zone.PopulationRoot, worldPositionStays: false);
			return gameObject.transform;
		}

		private void ClearExistingNodes(Transform root)
		{
			_spawned.Clear();
			ResourceNode[] componentsInChildren = _zone.PopulationRoot.GetComponentsInChildren<ResourceNode>(includeInactive: true);
			foreach (ResourceNode resourceNode in componentsInChildren)
			{
				if (!(resourceNode == null))
				{
					resourceNode.Collected -= HandleCollected;
					resourceNode.gameObject.SetActive(value: false);
					if (Application.isPlaying)
					{
						Object.Destroy(resourceNode.gameObject);
					}
					else
					{
						Object.DestroyImmediate(resourceNode.gameObject);
					}
				}
			}
			if (root != null)
			{
				root.gameObject.SetActive(value: true);
			}
		}
	}
}
