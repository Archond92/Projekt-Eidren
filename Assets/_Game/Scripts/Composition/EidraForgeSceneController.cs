using Eidren.AI;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using Eidren.UI;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.AI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class EidraForgeSceneController : MonoBehaviour
	{
		[SerializeField]
		private EidraForgeLootProfile lootProfile;

		[SerializeField]
		private Transform populationRoot;

		[SerializeField]
		private Transform dropRoot;

		private GameSession _session;

		private ContentDatabase _content;

		private PlayerProgressionService _progression;

		private PlayerPrefabBindings _player;

		private EidrenServiceRoot _services;

		private readonly Dictionary<string, ForgeEnemyState> _states = new Dictionary<string, ForgeEnemyState>();

		public void Configure(EidraForgeLootProfile profile, Transform configuredPopulationRoot, Transform configuredDropRoot)
		{
			lootProfile = profile;
			populationRoot = configuredPopulationRoot;
			dropRoot = configuredDropRoot;
		}

		private IEnumerator Start()
		{
			EidrenServiceRoot services = (_services = EidrenServiceRoot.FindOrCreate());
			_session = services.GameSession;
			_content = services.ContentDatabase;
			_progression = services.PlayerProgression;
			if (!_session.HasProgressFlag("garon_defeated"))
			{
				services.SceneFlowService.TryLoadScene("Zone_EmberRuins");
				yield break;
			}
			if (_session.EidraForge.GetAccessState(DateTime.UtcNow) == EidraForgeAccessState.FirstRunAvailable)
			{
				_session.EidraForge.TryBeginFirstRun(Environment.TickCount);
			}
			if (lootProfile == null)
			{
				lootProfile = Resources.Load<EidraForgeLootProfile>("Data/EidraForgeLoot_V02");
			}
			if (lootProfile == null)
			{
				throw new InvalidOperationException("Forge loot profile is missing.");
			}
			_session.EidraForgeChests.EnsureRunChestsGenerated(lootProfile, _content);
			while ((_player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>()) == null)
			{
				yield return null;
			}
			BindChests();
			BindFormwall();
			BindMinimapWalls();
			IndexStates();
			SpawnEnemies();
			SpawnPersistentDrops();
			_session.RequestSave(SaveRequestReason.StorageClosed);
		}

		/// <summary>
		/// #29: Die Karte zeigt im Verlies den Aufbau — die Wandkontur sind
		/// die Aussenkanten der NavMesh-Triangulation, einmalig beim
		/// Betreten berechnet (die Verliesgeometrie ist statisch). Der
		/// Presenter clippt selbst auf den Sichtradius um die Figur.
		/// </summary>
		private void BindMinimapWalls()
		{
			if (_player.CombatHud == null || _player.CombatHud.Minimap == null)
			{
				return;
			}
			NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
			_player.CombatHud.Minimap.SetWallOutline(
				MinimapWallOutline.ExtractBoundaryEdges(triangulation.vertices, triangulation.indices));
		}

		private void IndexStates()
		{
			_states.Clear();
			ForgeEnemyState[] all = _session.EidraForgeEnemies.GetAll();
			foreach (ForgeEnemyState forgeEnemyState in all)
			{
				if (forgeEnemyState != null)
				{
					_states[forgeEnemyState.SpawnId] = forgeEnemyState;
				}
			}
		}

		private void BindChests()
		{
			EidraForgeChestContainer[] array = UnityEngine.Object.FindObjectsByType<EidraForgeChestContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (EidraForgeChestContainer eidraForgeChestContainer in array)
			{
				if (!(eidraForgeChestContainer.gameObject.scene != base.gameObject.scene))
				{
					eidraForgeChestContainer.Initialize(_session, _content, lootProfile);
					if (_player.StorageWindow != null)
					{
						eidraForgeChestContainer.Activated -= _player.StorageWindow.Open;
						eidraForgeChestContainer.Activated += _player.StorageWindow.Open;
					}
					// 19.08.2026: Schmiedekisten tragen ihren Markenpreis als
					// Weltlabel — angebracht hier, weil die UI-Schicht selbst
					// nichts sucht und die Kisten zur Laufzeit gebunden werden.
					if (eidraForgeChestContainer.MarkPrice > 0
						&& eidraForgeChestContainer.GetComponent<ForgeChestPriceLabel>() == null)
					{
						eidraForgeChestContainer.gameObject.AddComponent<ForgeChestPriceLabel>().Configure(eidraForgeChestContainer);
					}
				}
			}
		}

		/* Der Bauplatz des Formwalls wird wie die Truhen an die Sitzung gebunden -
		   ohne diese Anbindung waere er blosse Geometrie und der Ausbau
		   unerreichbar. */
		private void BindFormwall()
		{
			EidraForgeFormwallSite[] bauplaetze = UnityEngine.Object.FindObjectsByType<EidraForgeFormwallSite>(
				FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (EidraForgeFormwallSite bauplatz in bauplaetze)
			{
				if (bauplatz.gameObject.scene == base.gameObject.scene)
				{
					bauplatz.Initialize(_session);
				}
			}
		}

		private void SpawnEnemies()
		{
			EidraForgeEnemyAnchor[] componentsInChildren = populationRoot.GetComponentsInChildren<EidraForgeEnemyAnchor>(includeInactive: true);
			foreach (EidraForgeEnemyAnchor eidraForgeEnemyAnchor in componentsInChildren)
			{
				ForgeEnemyState value;
				if (string.Equals(eidraForgeEnemyAnchor.SpawnId, "forge.ignivar.01", StringComparison.Ordinal))
				{
					SpawnIgnivar(eidraForgeEnemyAnchor);
				}
				else if (_states.TryGetValue(eidraForgeEnemyAnchor.SpawnId, out value) && !value.Defeated && !(eidraForgeEnemyAnchor.EnemyPrefab == null))
				{
					GameObject gameObject = UnityEngine.Object.Instantiate(eidraForgeEnemyAnchor.EnemyPrefab, eidraForgeEnemyAnchor.transform.position, eidraForgeEnemyAnchor.transform.rotation, populationRoot);
					gameObject.name = eidraForgeEnemyAnchor.SpawnId;
					WildlingController component2;
					if (gameObject.TryGetComponent<CoreGuardianController>(out var component))
					{
						component.Initialize(eidraForgeEnemyAnchor.CoreGuardianData, _player.transform, _player.Damageable);
						RestoreAndBind(component, value, isCore: true);
						_player.EidraTeam.AttachForgeBoss(component);
						component.BeginBattle();
					}
					else if (gameObject.TryGetComponent<WildlingController>(out component2))
					{
						component2.Initialize(_player.transform, _player.Damageable);
						RestoreAndBind(component2, value, isCore: false);
					}
				}
			}
		}

		private void SpawnIgnivar(EidraForgeEnemyAnchor anchor)
		{
			if (_session.EidraForge.IsIgnivarCaptured || anchor.EnemyPrefab == null || anchor.EnemyDefinition == null)
			{
				return;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(anchor.EnemyPrefab, anchor.transform.position, anchor.transform.rotation, populationRoot);
			gameObject.name = anchor.SpawnId;
			EidraWildController component = gameObject.GetComponent<EidraWildController>();
			if (!(component == null))
			{
				component.ConfigureInstance(anchor.SpawnId, anchor.EnemyDefinition);
				component.Initialize(_player.transform, _player.Damageable);
				EidraCaptureTarget component2 = gameObject.GetComponent<EidraCaptureTarget>();
				component2.Configure(_services.CaptureEquipment, _services.EidraRoster);
				component2.Captured += delegate(EidraCaptureResult result)
				{
					_session.EidraForge.MarkIgnivarCaptured();
					SelectIgnivarForBoss(result.Instance.InstanceId);
					_player.EidraTeam.ResetCooldowns();
					_session.RequestSave(SaveRequestReason.StorageClosed);
				};
			}
		}

		private void SelectIgnivarForBoss(string ignivarInstanceId)
		{
			string[] activeInstanceIds = _services.EidraRoster.Roster.GetActiveInstanceIds();
			int activeSlotCapacity = _services.EidraRoster.ActiveSlotCapacity;
			string[] instanceIds = ((activeSlotCapacity <= 1 || activeInstanceIds.Length == 0) ? new string[1] { ignivarInstanceId } : new string[2]
			{
				activeInstanceIds[0],
				ignivarInstanceId
			});
			if (_services.EidraRoster.TrySetActiveTeam(instanceIds, out var _))
			{
				_session.EidraForge.ConsumeTeamSelection();
			}
		}

		private void RestoreAndBind(EnemyControllerBase enemy, ForgeEnemyState state, bool isCore)
		{
			float num = Mathf.Max(0f, enemy.MaxHealth - state.Health);
			if (num > 0f)
			{
				enemy.ApplyDamage(new DamageInfo(num, 0f, enemy.transform.position, base.gameObject, isBackAttack: false, "forge.restore", "forge"));
			}
			if (state.Stagger > 0f)
			{
				enemy.AddStagger(state.Stagger);
			}
			enemy.HealthChanged += delegate(float health, float _)
			{
				PersistEnemy(state.SpawnId, health, enemy.CurrentStagger);
			};
			enemy.StaggerChanged += delegate(float stagger, float _)
			{
				PersistEnemy(state.SpawnId, enemy.CurrentHealth, stagger);
			};
			enemy.Died += delegate
			{
				HandleDefeat(state.SpawnId, enemy, isCore);
			};
		}

		private void PersistEnemy(string id, float health, float stagger)
		{
			_session.EidraForgeEnemies.Update(id, health, stagger);
			_session.RequestSave(SaveRequestReason.StorageClosed);
		}

		private void HandleDefeat(string id, EnemyControllerBase enemy, bool isCore)
		{
			if (_session.EidraForgeEnemies.TryRecordDefeat(id, enemy.transform.position, _content, out var reward))
			{
				if (!isCore)
				{
					_progression.RecordEnemyDefeated(reward.Experience);
				}
				SpawnDrop(id + ".smithing_mark", reward.MarkDrop, enemy.transform.position);
			}
			if (isCore)
			{
				new EidraForgeCompletionService(_session, _progression).CompleteCoreGuardian(DateTime.UtcNow);
			}
			_session.RequestSave(SaveRequestReason.BossDefeated);
		}

		private void SpawnPersistentDrops()
		{
			ForgeDropState[] drops = _session.EidraForge.GetDrops();
			foreach (ForgeDropState forgeDropState in drops)
			{
				SpawnDrop(forgeDropState.DropId, forgeDropState.Stack, forgeDropState.Position);
			}
		}

		private void SpawnDrop(string id, ItemStack stack, Vector3 position)
		{
			if (stack.IsEmpty)
			{
				return;
			}
			ItemDefinition item = _content.GetItem(stack.ItemId);
			if ((object)item == null)
			{
				return;
			}
			GameObject worldDropPrefab = item.WorldDropPrefab;
			if (!(worldDropPrefab == null) && worldDropPrefab.TryGetComponent<WorldItemController>(out var _))
			{
				WorldItemController worldItemController = UnityEngine.Object.Instantiate(worldDropPrefab.GetComponent<WorldItemController>(), position, Quaternion.identity, dropRoot);
				worldItemController.name = id;
				worldItemController.Initialize(item, stack.Quantity, id);
				worldItemController.PickedUp += delegate(WorldItemPickupResult result)
				{
					_session.EidraForgeEnemies.RemoveDrop(result.InstanceId);
					_session.RequestSave(SaveRequestReason.StorageClosed);
				};
			}
		}
	}
}
