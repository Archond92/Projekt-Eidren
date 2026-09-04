using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class ZoneWorldChestPopulator
	{
		private const string RootName = "WorldChests";

		private readonly ZoneController _zone;

		private readonly GameSession _session;

		private readonly ContentDatabase _content;

		private readonly List<WorldChestContainer> _spawned = new List<WorldChestContainer>();

		public IReadOnlyList<WorldChestContainer> Spawned => _spawned;

		public ZoneWorldChestPopulator(ZoneController zone, GameSession session, ContentDatabase content)
		{
			_zone = zone;
			_session = session;
			_content = content;
		}

		public WorldChestPlacement[] Populate()
		{
			WorldChestLootProfile worldChestLootProfile = _zone?.Definition?.WorldChestLootProfile;
			if (worldChestLootProfile == null || _zone.Definition.WorldChestSpawnPoints.Count == 0)
			{
				return Array.Empty<WorldChestPlacement>();
			}
			ZoneState orCreate = _session.ZoneStates.GetOrCreate(_zone.Definition.Id, 2);
			WorldChestPlacement[] array = WorldChestPopulationGenerator.Generate(_zone.Definition.Id, orCreate.Seed, _zone.Definition.WorldChestSpawnPoints);
			Transform root = ResolveRoot();
			Clear(root);
			WorldChestProgressContext progress = new WorldChestProgressContext(_session.HasProgressFlag("garon_defeated"), _session.HasProgressFlag("tier_2_unlocked"));
			WorldChestPlacement[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				WorldChestPlacement placement = array2[i];
				if (!orCreate.TryGetWorldChest(placement.InstanceId, out var state))
				{
					ItemStack[] slots = WorldChestLootGenerator.Generate(worldChestLootProfile, placement.Family, placement.InstanceId, placement.ContentSeed, progress, _content);
					state = new WorldChestState(placement.InstanceId, placement.SpawnPointId, placement.Family, slots);
					orCreate.SetWorldChest(state);
				}
				Spawn(worldChestLootProfile, in placement, state, root);
				if (placement.Family == WorldChestFamily.Guarded)
				{
					SpawnGuardedEncounter(worldChestLootProfile, in placement, root);
				}
			}
			Physics.SyncTransforms();
			return array;
		}

		private static void SpawnGuardedEncounter(WorldChestLootProfile profile, in WorldChestPlacement placement, Transform root)
		{
			if (!(profile.GuardedElitePrefab == null))
			{
				Transform transform = root.Find("Guardians");
				if (transform == null)
				{
					GameObject gameObject = new GameObject("Guardians");
					gameObject.transform.SetParent(root, worldPositionStays: false);
					transform = gameObject.transform;
				}
				Vector3 vector = Quaternion.Euler(0f, placement.RotationY, 0f) * Vector3.forward;
				Vector3 normalized = Vector3.Cross(Vector3.up, vector).normalized;
				CreateGuardian(profile.GuardedElitePrefab, placement.Position + vector * 3.2f, placement.InstanceId + ".guardian", transform);
				IReadOnlyList<GameObject> guardedCompanionPrefabs = profile.GuardedCompanionPrefabs;
				for (int i = 0; i < 2; i++)
				{
					GameObject prefab = guardedCompanionPrefabs[(int)((uint)(placement.ContentSeed + i * 31) % (uint)guardedCompanionPrefabs.Count)];
					Vector3 vector2 = ((i == 0) ? normalized : (-normalized));
					CreateGuardian(prefab, placement.Position + vector * 1.8f + vector2 * 2.5f, $"{placement.InstanceId}.companion.{i + 1}", transform);
				}
			}
		}

		private static void CreateGuardian(GameObject prefab, Vector3 position, string name, Transform root)
		{
			if (!(prefab == null))
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity, root);
				gameObject.name = name;
			}
		}

		private void Spawn(WorldChestLootProfile profile, in WorldChestPlacement placement, WorldChestState state, Transform root)
		{
			GameObject gameObject = profile.PrefabFor(placement.Family);
			if (gameObject == null)
			{
				throw new InvalidOperationException($"World-chest profile '{profile.Id}' lacks {placement.Family} prefab.");
			}
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, placement.Position, Quaternion.Euler(0f, placement.RotationY, 0f), root);
			gameObject2.name = placement.InstanceId;
			WorldChestContainer component = gameObject2.GetComponent<WorldChestContainer>();
			if (component == null)
			{
				throw new InvalidOperationException("Chest prefab '" + gameObject.name + "' has no container.");
			}
			component.Initialize(_content, _session, state);
			_spawned.Add(component);
		}

		private Transform ResolveRoot()
		{
			Transform transform = _zone.PopulationRoot.Find("WorldChests");
			if (transform != null)
			{
				return transform;
			}
			GameObject gameObject = new GameObject("WorldChests");
			gameObject.transform.SetParent(_zone.PopulationRoot, worldPositionStays: false);
			return gameObject.transform;
		}

		private void Clear(Transform root)
		{
			_spawned.Clear();
			for (int num = root.childCount - 1; num >= 0; num--)
			{
				GameObject gameObject = root.GetChild(num).gameObject;
				gameObject.SetActive(value: false);
				if (Application.isPlaying)
				{
					UnityEngine.Object.Destroy(gameObject);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(gameObject);
				}
			}
		}
	}
}
