using Eidren.AI;
using Eidren.Core.Services;
using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class BossAreaController : MonoBehaviour
	{
		private static readonly InventoryItemAmount[] GaronReward = new InventoryItemAmount[3]
		{
			new InventoryItemAmount("copper_bar", 3),
			new InventoryItemAmount("healing_potion", 2),
			new InventoryItemAmount("blueprint_item_copper_spear", 1)
		};

		[SerializeField]
		private string stableBossAreaId = "boss_area.ember_ruins.garon";

		[SerializeField]
		private Transform homePoint;

		[SerializeField]
		private Transform bossSpawnPoint;

		[SerializeField]
		[Min(1f)]
		private float territoryRadius = 18f;

		[SerializeField]
		[Min(1f)]
		private float activationRadius = 11f;

		[SerializeField]
		[Min(1f)]
		private float leashBoundary = 18f;

		[SerializeField]
		private Collider cameraFramingZone;

		[SerializeField]
		private BossController bossPrefab;

		[SerializeField]
		private BossData bossData;

		[SerializeField]
		private Material telegraphMaterial;

		[SerializeField]
		private BossEncounterView encounterViewPrefab;

		private GameSession _session;

		private ZoneLootController _loot;

		private PlayerPrefabBindings _player;

		private BossEncounterView _view;

		private bool _initialized;

		private bool _victoryHandled;

		public string StableBossAreaId => stableBossAreaId;

		public Transform HomePoint => homePoint;

		public Transform BossSpawnPoint => bossSpawnPoint;

		public float TerritoryRadius => territoryRadius;

		public float ActivationRadius => activationRadius;

		public float LeashBoundary => leashBoundary;

		public Collider CameraFramingZone => cameraFramingZone;

		public BossController ActiveBoss { get; private set; }

		public BossEncounterView EncounterView => _view;

		public bool BossSuppressedByProgress { get; private set; }

		public void Configure(string areaId, Transform configuredHomePoint, Transform configuredSpawnPoint, float configuredTerritoryRadius, float configuredActivationRadius, float configuredLeashBoundary, Collider configuredCameraFramingZone, BossController configuredBossPrefab, BossData configuredBossData, Material configuredTelegraphMaterial, BossEncounterView configuredViewPrefab)
		{
			stableBossAreaId = areaId;
			homePoint = configuredHomePoint;
			bossSpawnPoint = configuredSpawnPoint;
			territoryRadius = configuredTerritoryRadius;
			activationRadius = configuredActivationRadius;
			leashBoundary = configuredLeashBoundary;
			cameraFramingZone = configuredCameraFramingZone;
			bossPrefab = configuredBossPrefab;
			bossData = configuredBossData;
			telegraphMaterial = configuredTelegraphMaterial;
			encounterViewPrefab = configuredViewPrefab;
		}

		public void Initialize(PlayerPrefabBindings player, EidrenServiceRoot services, ZoneLootController loot)
		{
			if (!_initialized)
			{
				string[] validationErrors = GetValidationErrors();
				if (validationErrors.Length != 0)
				{
					throw new InvalidOperationException("BossArea '" + stableBossAreaId + "' is invalid: " + string.Join("; ", validationErrors));
				}
				_player = player ?? throw new ArgumentNullException("player");
				_session = services.GameSession;
				_loot = loot ?? throw new ArgumentNullException("loot");
				_initialized = true;
				if (_session.HasProgressFlag("garon_defeated") || !services.PlayerProgression.CanChallengeGaron)
				{
					BossSuppressedByProgress = true;
					return;
				}
				ActiveBoss = UnityEngine.Object.Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation, base.transform);
				ActiveBoss.name = "Garon";
				ActiveBoss.Initialize(bossData, _player.transform, _player.Damageable, telegraphMaterial);
				ActiveBoss.Died += HandleBossDied;
				_view = UnityEngine.Object.Instantiate(encounterViewPrefab);
				_view.name = "BossEncounterView";
				_view.Bind(ActiveBoss, bossData.DisplayName);
			}
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (string.IsNullOrWhiteSpace(stableBossAreaId))
			{
				list.Add("Stable BossArea ID is missing.");
			}
			if (homePoint == null)
			{
				list.Add("Home point is missing.");
			}
			if (bossSpawnPoint == null)
			{
				list.Add("Boss spawn point is missing.");
			}
			if (bossPrefab == null)
			{
				list.Add("Garon prefab is missing.");
			}
			if (bossData == null)
			{
				list.Add("Garon data is missing.");
			}
			if (encounterViewPrefab == null)
			{
				list.Add("Boss encounter view prefab is missing.");
			}
			if (activationRadius >= territoryRadius)
			{
				list.Add("Activation radius must be below territory radius.");
			}
			if (Mathf.Abs(leashBoundary - ((bossData != null) ? bossData.Navigation.LeashRange : leashBoundary)) > 0.01f)
			{
				list.Add("BossArea leash boundary must match Garon's data-driven navigation leash.");
			}
			return list.ToArray();
		}

		private void Update()
		{
			if (_initialized && !BossSuppressedByProgress && !(ActiveBoss == null) && ActiveBoss.IsAlive && !ActiveBoss.BattleActive && !(_player == null) && _player.Damageable.IsAlive)
			{
				Vector3 position = ActiveBoss.transform.position;
				Vector3 position2 = _player.transform.position;
				position.y = 0f;
				position2.y = 0f;
				if (Vector3.Distance(position, position2) <= activationRadius)
				{
					ActiveBoss.BeginBattle();
				}
			}
		}

		private void HandleBossDied()
		{
			if (_victoryHandled)
			{
				return;
			}
			_victoryHandled = true;
			bool flag = _session.TrySetProgressFlag("garon_defeated");
			_session.MarkWorldMapNodeCompleted("zone_ember_ruins");
			bool flag2 = true;
			if (flag)
			{
				EidrenServiceRoot instance = EidrenServiceRoot.Instance;
				instance.PlayerProgression.RecordEnemyDefeated(bossData.ExperienceReward, firstGaronVictory: true);
				instance.PlayerProgression.GrantTechnologyPoints(1);
				_session.TrySetProgressFlag("garon_v02_rewards_granted");
				flag2 = _session.PlayerInventory.TryAddBatch(GaronReward);
				if (!flag2)
				{
					_loot.SpawnGuaranteedItems(GaronReward, ActiveBoss.transform.position, ActiveBoss.transform, "garon_defeated.reward");
				}
				_session.RequestSave(SaveRequestReason.BossDefeated);
			}
			_view?.ShowVictory(flag2);
		}

		private void OnDrawGizmosSelected()
		{
			if (!(homePoint == null))
			{
				Gizmos.color = new Color(0.95f, 0.45f, 0.15f, 0.8f);
				Gizmos.DrawWireSphere(homePoint.position, territoryRadius);
				Gizmos.color = new Color(0.95f, 0.78f, 0.2f, 0.8f);
				Gizmos.DrawWireSphere(homePoint.position, activationRadius);
			}
		}

		private void OnDestroy()
		{
			if (ActiveBoss != null)
			{
				ActiveBoss.Died -= HandleBossDied;
			}
		}
	}
}
