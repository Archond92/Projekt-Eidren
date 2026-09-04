using Eidren.AI;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class ZoneEidraPopulator
	{
		private const string EidraRootName = "Eidra";

		private readonly ZoneController _zone;

		private readonly GameSession _session;

		private readonly CaptureEquipment _equipment;

		private readonly EidraRosterService _roster;

		private readonly PlayerProgressionService _progression;

		private readonly List<EidraWildController> _spawned = new List<EidraWildController>();

		private readonly Dictionary<EidraWildController, Action<EidraDepartureReason>> _departureHandlers = new Dictionary<EidraWildController, Action<EidraDepartureReason>>();

		public IReadOnlyList<EidraWildController> SpawnedEidra => _spawned;

		public ZoneEidraPopulator(ZoneController zone, GameSession session, CaptureEquipment equipment, EidraRosterService roster, PlayerProgressionService progression = null)
		{
			_zone = zone;
			_session = session;
			_equipment = equipment;
			_roster = roster;
			_progression = progression;
		}

		/// <summary>
		/// Kampfbelohnung beim Abgang eines Welt-Eidra: Nur die Flucht an der
		/// Lebensschwelle gilt als besiegt. Fang, Szenenabbau und technisches
		/// Entfernen vergeben keine XP.
		/// </summary>
		public static int ResolveDepartureReward(EnemyDefinition definition, EidraDepartureReason reason)
		{
			return (reason == EidraDepartureReason.Fled && definition != null) ? definition.ExperienceReward : 0;
		}

		public void Populate(ZoneLayout layout)
		{
			_spawned.Clear();
			if (_zone == null || _zone.Definition == null || _zone.PopulationRoot == null || layout == null || layout.EidraPlacements.Count == 0)
			{
				return;
			}
			Transform root = ResolveEidraRoot();
			ClearExisting(root);
			if (!_session.ZoneStates.TryGet(_zone.Definition.Id, out var state))
			{
				return;
			}
			foreach (ZoneEidraPlacement eidraPlacement in layout.EidraPlacements)
			{
				Spawn(eidraPlacement, root, state);
			}
			Physics.SyncTransforms();
		}

		public void InitializeWithPlayer(PlayerPrefabBindings player)
		{
			if (player == null)
			{
				return;
			}
			foreach (EidraWildController item in _spawned)
			{
				if (item != null && !item.HasDeparted)
				{
					item.Initialize(player.transform, player.Damageable);
				}
			}
		}

		private void Spawn(in ZoneEidraPlacement placement, Transform root, ZoneState state)
		{
			if (placement.Definition == null || placement.Definition.Prefab == null)
			{
				Debug.LogError("Eidra '" + placement.InstanceId + "' has no prefab; the zone cannot build it from the seed.");
			}
			else
			{
				if (state.IsHarvested(placement.InstanceId))
				{
					return;
				}
				string instanceId = placement.InstanceId;
				GameObject gameObject = UnityEngine.Object.Instantiate(placement.Definition.Prefab, placement.Position, Quaternion.Euler(0f, placement.RotationY, 0f), root);
				gameObject.name = instanceId;
				EidraWildController controller = gameObject.GetComponent<EidraWildController>();
				EidraCaptureTarget component = gameObject.GetComponent<EidraCaptureTarget>();
				if (controller == null || component == null)
				{
					Debug.LogError("Eidra prefab '" + placement.Definition.name + "' needs both EidraWildController and EidraCaptureTarget.", gameObject);
					return;
				}
				controller.ConfigureInstance(instanceId, placement.Definition);
				Action<EidraDepartureReason> handler = null;
				handler = delegate(EidraDepartureReason reason)
				{
					controller.Departed -= handler;
					_departureHandlers.Remove(controller);
					HandleDeparted(instanceId, controller.Definition, reason);
				};
				controller.Departed -= handler;
				controller.Departed += handler;
				_departureHandlers[controller] = handler;
				component.Configure(_equipment, _roster);
				_spawned.Add(controller);
			}
		}

		private void HandleDeparted(string instanceId, EnemyDefinition definition, EidraDepartureReason reason)
		{
			// Der Handler meldet sich vor diesem Aufruf selbst ab; pro Instanz
			// kann die Belohnung deshalb höchstens einmal verbucht werden.
			int reward = ResolveDepartureReward(definition, reason);
			if (reward > 0)
			{
				_progression?.RecordEnemyDefeated(reward);
			}
			_session.ZoneStates.MarkHarvested(_zone.Definition.Id, instanceId);
		}

		private Transform ResolveEidraRoot()
		{
			Transform transform = _zone.PopulationRoot.Find("Eidra");
			if (transform != null)
			{
				return transform;
			}
			GameObject gameObject = new GameObject("Eidra");
			gameObject.transform.SetParent(_zone.PopulationRoot, worldPositionStays: false);
			return gameObject.transform;
		}

		private void ClearExisting(Transform root)
		{
			UnbindDepartures();
			EidraWildController[] componentsInChildren = _zone.PopulationRoot.GetComponentsInChildren<EidraWildController>(includeInactive: true);
			foreach (EidraWildController eidraWildController in componentsInChildren)
			{
				if (!(eidraWildController == null))
				{
					eidraWildController.gameObject.SetActive(value: false);
					if (Application.isPlaying)
					{
						UnityEngine.Object.Destroy(eidraWildController.gameObject);
					}
					else
					{
						UnityEngine.Object.DestroyImmediate(eidraWildController.gameObject);
					}
				}
			}
			if (root != null)
			{
				root.gameObject.SetActive(value: true);
			}
		}

		private void UnbindDepartures()
		{
			foreach (KeyValuePair<EidraWildController, Action<EidraDepartureReason>> departureHandler in _departureHandlers)
			{
				if (departureHandler.Key != null)
				{
					departureHandler.Key.Departed -= departureHandler.Value;
				}
			}
			_departureHandlers.Clear();
		}
	}
}
