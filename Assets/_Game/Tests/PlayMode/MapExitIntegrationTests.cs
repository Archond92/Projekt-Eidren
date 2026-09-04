using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Player;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class MapExitIntegrationTests
{
	private sealed class FakeSceneLoader : ISceneLoader
	{
		public int RequestCount;

		public bool Complete = true;

		public string ActiveSceneName => "HomeBase";

		public bool CanLoad(string sceneName)
		{
			return true;
		}

		public IEnumerator LoadAsync(string sceneName)
		{
			RequestCount++;
			while (!Complete)
			{
				yield return null;
			}
		}
	}

	[UnityTest]
	public IEnumerator MultipleTriggers_ProduceOneSceneRequest()
	{
		yield return LoadHomeBase();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		MapExitCoordinator coordinator = UnityEngine.Object.FindFirstObjectByType<MapExitCoordinator>();
		FakeSceneLoader loader = ReplaceSceneLoader();
		loader.Complete = false;
		MapExitVolume east = Volume(MapExitDirection.East);
		MapExitVolume north = Volume(MapExitDirection.North);
		spawner.SpawnedPlayer.Motor.Teleport(new Vector3(23.5f, 0.05f, 23.5f));
		spawner.SpawnedPlayer.WeaponHitbox.BeginWindow(new AttackStepData
		{
			HitboxRange = 2f,
			HitboxAngle = 90f
		});
		coordinator.NotifyEntered(east, spawner.SpawnedPlayer);
		coordinator.NotifyEntered(north, spawner.SpawnedPlayer);
		yield return WaitRealtime(0.55f);
		coordinator.NotifyEntered(east, spawner.SpawnedPlayer);
		yield return WaitRealtime(0.1f);
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(spawner.SpawnedPlayer.WeaponHitbox.IsWindowActive, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(spawner.Input.GameplayEnabled, (IResolveConstraint)(object)Is.False);
		Assert.That<int>(coordinator.ConfirmedTransitionCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<MapExitDirection>(EidrenServiceRoot.Instance.GameSession.LastExitDirection, (IResolveConstraint)(object)Is.EqualTo((object)MapExitDirection.North));
		Assert.That<string>(EidrenServiceRoot.Instance.GameSession.PreviousZoneId, (IResolveConstraint)(object)Is.EqualTo((object)"home_base"));
		loader.Complete = true;
		yield return null;
		yield return CleanupActiveScene();
	}

	[UnityTest]
	public IEnumerator DisabledVolume_CancelsRunningCountdown()
	{
		yield return LoadHomeBase();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		MapExitCoordinator coordinator = UnityEngine.Object.FindFirstObjectByType<MapExitCoordinator>();
		FakeSceneLoader loader = ReplaceSceneLoader();
		MapExitVolume west = Volume(MapExitDirection.West);
		spawner.SpawnedPlayer.Motor.Teleport(new Vector3(-23.5f, 0.05f, 0f));
		coordinator.NotifyEntered(west, spawner.SpawnedPlayer);
		Assert.That<bool>(coordinator.IsCountdownRunning, (IResolveConstraint)(object)Is.True);
		west.SetExitEnabled(active: false);
		yield return WaitRealtime(0.55f);
		Assert.That<bool>(coordinator.IsCountdownRunning, (IResolveConstraint)(object)Is.False);
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.EqualTo((object)0));
		yield return CleanupActiveScene();
	}

	[UnityTest]
	public IEnumerator ReturningInside_CancelsAndReentryRestartsCleanly()
	{
		yield return LoadHomeBase();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		MapExitCoordinator coordinator = UnityEngine.Object.FindFirstObjectByType<MapExitCoordinator>();
		FakeSceneLoader loader = ReplaceSceneLoader();
		MapExitVolume south = Volume(MapExitDirection.South);
		spawner.SpawnedPlayer.Motor.Teleport(new Vector3(0f, 0.05f, -23.5f));
		coordinator.NotifyEntered(south, spawner.SpawnedPlayer);
		yield return WaitRealtime(0.1f);
		spawner.SpawnedPlayer.Motor.Teleport(new Vector3(0f, 0.05f, 0f));
		yield return WaitRealtime(0.45f);
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.EqualTo((object)0));
		Assert.That<bool>(coordinator.IsCountdownRunning, (IResolveConstraint)(object)Is.False);
		spawner.SpawnedPlayer.Motor.Teleport(new Vector3(0f, 0.05f, -23.5f));
		coordinator.NotifyEntered(south, spawner.SpawnedPlayer);
		yield return WaitRealtime(0.55f);
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<MapExitDirection>(EidrenServiceRoot.Instance.GameSession.LastExitDirection, (IResolveConstraint)(object)Is.EqualTo((object)MapExitDirection.South));
		yield return CleanupActiveScene();
	}

	[UnityTest]
	public IEnumerator PlayerDeathDuringCountdown_DoesNotLoadScene()
	{
		yield return LoadHomeBase();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		MapExitCoordinator coordinator = UnityEngine.Object.FindFirstObjectByType<MapExitCoordinator>();
		FakeSceneLoader loader = ReplaceSceneLoader();
		MapExitVolume north = Volume(MapExitDirection.North);
		spawner.SpawnedPlayer.Motor.Teleport(new Vector3(0f, 0.05f, 23.5f));
		coordinator.NotifyEntered(north, spawner.SpawnedPlayer);
		spawner.SpawnedPlayer.Damageable.ApplyDamage(new DamageInfo(1000f, 0f, spawner.SpawnedPlayer.transform.position, null, isBackAttack: false, "test.map_exit.death", "test"));
		yield return WaitRealtime(0.55f);
		Assert.That<bool>(spawner.SpawnedPlayer.Damageable.IsAlive, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(coordinator.IsCountdownRunning, (IResolveConstraint)(object)Is.False);
		Assert.That<int>(loader.RequestCount, (IResolveConstraint)(object)Is.EqualTo((object)0));
		yield return CleanupActiveScene();
	}

	[UnityTest]
	public IEnumerator HomeBaseAndGreenwood_HaveFourOpenFullSideExits()
	{
		string[] array = new string[2] { "HomeBase", "Zone_Greenwood" };
		foreach (string sceneName in array)
		{
			yield return LoadZone(sceneName);
			ZoneBoundarySettings boundary = UnityEngine.Object.FindFirstObjectByType<ZoneBoundarySettings>();
			MapExitVolume[] volumes = UnityEngine.Object.FindObjectsByType<MapExitVolume>(FindObjectsSortMode.None);
			Assert.That<ZoneBoundarySettings>(boundary, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<MovementBoundaryShape>(boundary.BoundaryShape, (IResolveConstraint)(object)Is.EqualTo((object)MovementBoundaryShape.None));
			Assert.That<MapSideFlags>(boundary.OpenExitSides, (IResolveConstraint)(object)Is.EqualTo((object)MapSideFlags.All));
			Assert.That<MapExitVolume[]>(volumes, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)4));
			Assert.That<IEnumerable<MapExitDirection>>(volumes.Select((MapExitVolume volume) => volume.Direction), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new MapExitDirection[4]
			{
				MapExitDirection.North,
				MapExitDirection.East,
				MapExitDirection.South,
				MapExitDirection.West
			}));
			MapExitVolume[] array2 = volumes;
			for (int num = 0; num < array2.Length; num++)
			{
				BoxCollider trigger = array2[num].GetComponent<BoxCollider>();
				Assert.That<bool>(trigger.isTrigger, (IResolveConstraint)(object)Is.True);
				Assert.That<float>(Mathf.Max(trigger.size.x, trigger.size.z), (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)boundary.SafeAreaSize.x));
			}
		}
		yield return CleanupActiveScene();
	}

	[UnityTest]
	public IEnumerator AllOutdoorZones_ShareTheStandardSize()
	{
		string[] array = new string[4] { "Zone_Greenwood", "Zone_Quarry", "Zone_Marsh", "Zone_EmberRuins" };
		foreach (string sceneName in array)
		{
			yield return LoadZone(sceneName);
			ZoneBoundarySettings zoneBoundarySettings = UnityEngine.Object.FindFirstObjectByType<ZoneBoundarySettings>();
			Assert.That<ZoneBoundarySettings>(zoneBoundarySettings, (IResolveConstraint)(object)Is.Not.Null, sceneName + " hat keine ZoneBoundarySettings.", Array.Empty<object>());
			Assert.That<float>(zoneBoundarySettings.SafeAreaSize.x, (IResolveConstraint)(object)Is.EqualTo((object)76f).Within((object)0.01f), sceneName + " weicht von der Einheitsgroesse ab.", Array.Empty<object>());
			Assert.That<float>(zoneBoundarySettings.SafeAreaSize.y, (IResolveConstraint)(object)Is.EqualTo((object)76f).Within((object)0.01f), sceneName + " ist nicht quadratisch.", Array.Empty<object>());
		}
		yield return CleanupActiveScene();
	}

	private static IEnumerator LoadHomeBase()
	{
		yield return LoadZone("HomeBase");
	}

	private static IEnumerator LoadZone(string sceneName)
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
		float deadline = Time.realtimeSinceStartup + 5f;
		while (Time.realtimeSinceStartup < deadline)
		{
			ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
			if (spawner != null && spawner.SpawnedPlayer != null)
			{
				yield return null;
				yield break;
			}
			yield return null;
		}
		Assert.Fail("HomeBase player did not spawn.");
	}

	private static FakeSceneLoader ReplaceSceneLoader()
	{
		FakeSceneLoader loader = new FakeSceneLoader();
		EidrenServiceRoot.Instance.SceneFlowService.Configure(null, loader);
		return loader;
	}

	private static MapExitVolume Volume(MapExitDirection direction)
	{
		return UnityEngine.Object.FindObjectsByType<MapExitVolume>(FindObjectsSortMode.None).Single((MapExitVolume volume) => volume.Direction == direction);
	}

	private static IEnumerator WaitRealtime(float duration)
	{
		float until = Time.realtimeSinceStartup + duration;
		while (Time.realtimeSinceStartup < until)
		{
			yield return null;
		}
	}

	private static IEnumerator CleanupActiveScene()
	{
		Scene active = SceneManager.GetActiveScene();
		SceneManager.SetActiveScene(SceneManager.CreateScene("MapExitTestCleanup"));
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
		}
		yield return SceneManager.UnloadSceneAsync(active);
		yield return null;
	}
}
}
