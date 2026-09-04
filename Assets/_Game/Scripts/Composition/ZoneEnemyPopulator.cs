using Eidren.AI;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Eidren.Composition
{
	/// <summary>
	/// F31-009: Platziert die reguläre Gegnerbesetzung einer Zone zur
	/// Laufzeit. Vorher waren die Gegner beim Szenenbau fest eingebacken —
	/// mit nur neun Kandidatenplätzen und ohne Bezug zu den pro Lauf
	/// gewürfelten Weltkisten. Jetzt bewacht ein Teil der Besetzung die
	/// tatsächlich gespawnten Kisten in einem Abstandsring
	/// (ZoneEnemyPopulationRules), der Rest verteilt sich frei.
	/// </summary>
	public sealed class ZoneEnemyPopulator
	{
		private const string RootName = "Enemies";

		private const float EnemySpacing = 7f;

		private const float RelaxedEnemySpacing = 3.5f;

		private const float SpawnClearance = 8f;

		private const float ChestClearance = 3.6f;

		private const float BodyClearance = 1.25f;

		private const float NavMeshSampleDistance = 1.5f;

		private const int GuardAttempts = 48;

		private const int FreeAttempts = 48;

		private readonly ZoneController _zone;

		private readonly GameSession _session;

		private readonly List<WildlingController> _spawned = new List<WildlingController>();

		private readonly List<(Vector3 ChestPosition, WildlingController Guard)> _chestGuards = new List<(Vector3, WildlingController)>();

		private readonly List<Vector3> _occupied = new List<Vector3>();

		private Bounds? _bossKeepOut;

		public IReadOnlyList<WildlingController> Spawned => _spawned;

		public IReadOnlyList<(Vector3 ChestPosition, WildlingController Guard)> ChestGuards => _chestGuards;

		/// <summary>Sperrzone um den Bossbereich (falls die Zone einen hat).</summary>
		public Bounds? BossKeepOut => _bossKeepOut;

		public ZoneEnemyPopulator(ZoneController zone, GameSession session)
		{
			_zone = zone;
			_session = session;
		}

		public void Populate(IReadOnlyList<WorldChestPlacement> chests)
		{
			_spawned.Clear();
			_chestGuards.Clear();
			_occupied.Clear();
			if (_zone == null || _zone.Definition == null || _zone.PopulationRoot == null || !_zone.Definition.EnemiesAllowed)
			{
				return;
			}
			Transform root = ResolveRoot();
			ClearExisting(root);
			ResolveBossKeepOut();
			List<EnemyDefinition> instances = CollectInstances();
			if (instances.Count == 0)
			{
				return;
			}
			ZoneState state = _session.ZoneStates.GetOrCreate(_zone.Definition.Id, ZoneLayoutGenerator.GeneratorVersion);
			Random random = new Random(WorldChestPopulationGenerator.DeriveSeed(state.Seed, "enemies"));
			Shuffle(instances, random);
			List<Vector3> guardableChests = CollectGuardableChests(chests);
			int budget = ZoneEnemyPopulationRules.ChestGuardBudget(guardableChests.Count, instances.Count);
			for (int index = 0; index < instances.Count; index++)
			{
				bool guard = index < budget;
				Vector3 chest = guard ? guardableChests[index] : Vector3.zero;
				if (guard && TryFindGuardPosition(chest, random, out Vector3 ringPosition))
				{
					_chestGuards.Add((chest, Spawn(instances[index], ringPosition, random, root, index)));
				}
				else if (TryFindFreePosition(random, out Vector3 freePosition))
				{
					Spawn(instances[index], freePosition, random, root, index);
				}
				else
				{
					Debug.LogWarning("Zone '" + _zone.Definition.Id + "' hat keinen freien Platz für '" + instances[index].Id + "' gefunden; Instanz übersprungen.");
				}
			}
			Physics.SyncTransforms();
		}

		/// <summary>
		/// Kisten, deren Wachenring in der Boss-Sperrzone liegt, bekommen
		/// keine Feldwache — dort ist der Boss die Wache.
		/// </summary>
		private List<Vector3> CollectGuardableChests(IReadOnlyList<WorldChestPlacement> chests)
		{
			List<Vector3> list = new List<Vector3>(chests?.Count ?? 0);
			if (chests == null)
			{
				return list;
			}
			foreach (WorldChestPlacement chest in chests)
			{
				if (!_bossKeepOut.HasValue
					|| !_bossKeepOut.Value.Contains(new Vector3(chest.Position.x, _bossKeepOut.Value.center.y, chest.Position.z)))
				{
					list.Add(chest.Position);
				}
			}
			return list;
		}

		private List<EnemyDefinition> CollectInstances()
		{
			List<EnemyDefinition> list = new List<EnemyDefinition>();
			foreach (ZoneEnemyAllocation allocation in _zone.Definition.EnemyAllocations)
			{
				EnemyDefinition definition = allocation.Definition;
				if (definition == null || definition.IsCapturableEidra || allocation.Count <= 0)
				{
					continue;
				}
				if (definition.Prefab == null)
				{
					Debug.LogError("Gegner '" + definition.Id + "' hat kein Prefab; die Zone kann ihn nicht aufstellen.");
					continue;
				}
				for (int i = 0; i < allocation.Count; i++)
				{
					list.Add(definition);
				}
			}
			return list;
		}

		private static void Shuffle(List<EnemyDefinition> values, Random random)
		{
			for (int i = values.Count - 1; i > 0; i--)
			{
				int j = random.Next(i + 1);
				(values[i], values[j]) = (values[j], values[i]);
			}
		}

		private WildlingController Spawn(EnemyDefinition definition, Vector3 position, Random random, Transform root, int index)
		{
			GameObject gameObject = Object.Instantiate(definition.Prefab, position, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), root);
			gameObject.name = $"{definition.DisplayName}_{index + 1:00}";
			_occupied.Add(position);
			WildlingController controller = gameObject.GetComponent<WildlingController>();
			if (controller != null)
			{
				_spawned.Add(controller);
			}
			return controller;
		}

		private bool TryFindGuardPosition(Vector3 chest, Random random, out Vector3 position)
		{
			for (int i = 0; i < GuardAttempts; i++)
			{
				Vector3 candidate = ZoneEnemyPopulationRules.ChestGuardCandidate(chest, random);
				// Wachen stehen bewusst nah an der Kiste — der Kistenfreiraum
				// gilt nur für frei verteilte Gegner.
				if (TryGround(candidate, out position) && IsFree(position, chestClearance: 0f))
				{
					return true;
				}
			}
			position = Vector3.zero;
			return false;
		}

		private bool TryFindFreePosition(Random random, out Vector3 position)
		{
			Bounds area = PlacementArea();
			for (float spacing = EnemySpacing; spacing >= RelaxedEnemySpacing; spacing *= 0.7f)
			{
				for (int i = 0; i < FreeAttempts; i++)
				{
					Vector3 candidate = new Vector3(
						Mathf.Lerp(area.min.x, area.max.x, (float)random.NextDouble()),
						area.center.y,
						Mathf.Lerp(area.min.z, area.max.z, (float)random.NextDouble()));
					if (TryGround(candidate, out position) && IsFree(position, ChestClearance, spacing))
					{
						return true;
					}
				}
			}
			position = Vector3.zero;
			return false;
		}

		private Bounds PlacementArea()
		{
			Bounds bounds = ((_zone.WalkableGround != null) ? _zone.WalkableGround.bounds : new Bounds(Vector3.zero, Vector3.one * 60f));
			return new Bounds(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z), new Vector3(Mathf.Max(1f, bounds.size.x - 12f), 0f, Mathf.Max(1f, bounds.size.z - 12f)));
		}

		private bool TryGround(Vector3 candidate, out Vector3 grounded)
		{
			if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
			{
				grounded = hit.position;
				return true;
			}
			grounded = Vector3.zero;
			return false;
		}

		/// <summary>
		/// F31-009: Feldgegner bleiben aus dem Bossbereich — sonst zieht der
		/// Bosskampf zufällige Zusatzgegner und die Leinen-Rückkehr des
		/// Bosses wird gestört.
		/// </summary>
		private void ResolveBossKeepOut()
		{
			_bossKeepOut = null;
			Transform bossArea = _zone.BossArea;
			if (bossArea == null)
			{
				return;
			}
			Collider[] colliders = bossArea.GetComponentsInChildren<Collider>(includeInactive: true);
			Bounds bounds;
			if (colliders.Length > 0)
			{
				bounds = colliders[0].bounds;
				for (int i = 1; i < colliders.Length; i++)
				{
					bounds.Encapsulate(colliders[i].bounds);
				}
			}
			else
			{
				bounds = new Bounds(bossArea.position, new Vector3(24f, 4f, 24f));
			}
			// Seit dem Leinen-Fix patrouillieren Feldgegner real (Radius bis
			// 3,5) — die Sperrzone muss den Patrouillenweg einschliessen,
			// sonst laeuft ein Randgegner in die Bosskammer.
			bounds.Expand(new Vector3(15f, 200f, 15f));
			_bossKeepOut = bounds;
		}

		private bool IsFree(Vector3 position, float chestClearance, float spacing = EnemySpacing)
		{
			if (_bossKeepOut.HasValue && _bossKeepOut.Value.Contains(new Vector3(position.x, _bossKeepOut.Value.center.y, position.z)))
			{
				return false;
			}
			float spacingSquare = spacing * spacing;
			foreach (Vector3 other in _occupied)
			{
				if ((other - position).sqrMagnitude < spacingSquare)
				{
					return false;
				}
			}
			for (MapExitDirection direction = MapExitDirection.None; direction <= MapExitDirection.West; direction++)
			{
				Transform spawn = _zone.GetSpawnForEntry(direction);
				if (spawn != null && Vector3.Distance(spawn.position, position) < SpawnClearance)
				{
					return false;
				}
			}
			if (chestClearance > 0f)
			{
				foreach (WorldChestContainer chest in _zone.WorldChests)
				{
					if (chest != null && Vector3.Distance(chest.transform.position, position) < chestClearance)
					{
						return false;
					}
				}
			}
			Collider ground = _zone.WalkableGround;
			foreach (Collider collider in Physics.OverlapSphere(position + Vector3.up, BodyClearance, -5, QueryTriggerInteraction.Ignore))
			{
				if (collider != null && collider != ground)
				{
					return false;
				}
			}
			return true;
		}

		private Transform ResolveRoot()
		{
			Transform transform = _zone.PopulationRoot.Find(RootName);
			if (transform != null)
			{
				return transform;
			}
			GameObject gameObject = new GameObject(RootName);
			gameObject.transform.SetParent(_zone.PopulationRoot, worldPositionStays: false);
			return gameObject.transform;
		}

		private void ClearExisting(Transform root)
		{
			// Nur der eigene Wurzelknoten wird geräumt — die Wächter der
			// bewachten Kisten (WorldChests/Guardians) bleiben unberührt.
			for (int i = root.childCount - 1; i >= 0; i--)
			{
				GameObject child = root.GetChild(i).gameObject;
				child.SetActive(value: false);
				if (Application.isPlaying)
				{
					Object.Destroy(child);
				}
				else
				{
					Object.DestroyImmediate(child);
				}
			}
		}
	}
}
