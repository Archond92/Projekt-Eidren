using Eidren.AI;
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System.Reflection;
using System;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class WildlingIntegrationTests
{
	private sealed class TestWorld
	{
		public readonly GameObject Ground;

		public readonly GameObject Obstacle;

		public readonly GameObject NavigationRoot;

		public readonly NavMeshSurface Surface;

		public readonly GameObject Player;

		public readonly Damageable PlayerHealth;

		public readonly WildlingController Wildling;

		public readonly EnemyDefinition Definition;

		public TestWorld(GameObject ground, GameObject obstacle, GameObject navigationRoot, NavMeshSurface surface, GameObject player, Damageable playerHealth, WildlingController wildling, EnemyDefinition definition)
		{
			Ground = ground;
			Obstacle = obstacle;
			NavigationRoot = navigationRoot;
			Surface = surface;
			Player = player;
			PlayerHealth = playerHealth;
			Wildling = wildling;
			Definition = definition;
		}
	}

	[UnityTest]
	public IEnumerator PerceptionTransitionsThroughAlertToChase()
	{
		TestWorld world = CreateWorld(new Vector3(0f, 0f, 6f));
		Assert.That<EnemyState>(world.Wildling.EnemyState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Alert));
		yield return WaitForState(world.Wildling, EnemyState.Chase);
		yield return null;
		Assert.That<bool>(world.Wildling.NavigationAgent.hasPath, (IResolveConstraint)(object)Is.True);
		yield return Clean(world);
	}

	[UnityTest]
	public IEnumerator AttackHitboxDamagesTargetOnlyOncePerAttack()
	{
		TestWorld world = CreateWorld(new Vector3(0f, 0f, 1.35f), 2.2f, 14f, 0.02f, 0.1f, 1f);
		int damageEvents = 0;
		world.PlayerHealth.Damaged += delegate
		{
			damageEvents++;
		};
		world.Wildling.TryDetectTarget();
		yield return WaitForState(world.Wildling, EnemyState.ExecuteAttack);
		yield return WaitForState(world.Wildling, EnemyState.Recover, 5000);
		Assert.That<int>(damageEvents, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<float>(world.PlayerHealth.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)482f).Within((object)0.01f));
		Assert.That<int>(world.Wildling.AttackHitbox.HitCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		yield return Clean(world);
	}

	[UnityTest]
	public IEnumerator FullStaggerInterruptsTelegraphAndAttack()
	{
		TestWorld world = CreateWorld(new Vector3(0f, 0f, 1.35f), 2.2f, 14f, 1f);
		world.Wildling.TryDetectTarget();
		yield return WaitForState(world.Wildling, EnemyState.Telegraph);
		Assert.That<bool>(world.Wildling.HasActiveTelegraph, (IResolveConstraint)(object)Is.True);
		world.Wildling.AddStagger(world.Wildling.MaxStagger);
		Assert.That<EnemyState>(world.Wildling.EnemyState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Staggered));
		Assert.That<bool>(world.Wildling.HasActiveTelegraph, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(world.Wildling.HasActiveAttack, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(world.Wildling.AttackHitbox.Hitbox.enabled, (IResolveConstraint)(object)Is.False);
		yield return Clean(world);
	}

	[UnityTest]
	public IEnumerator LeashTriggersReturnAndRegeneratesFully()
	{
		TestWorld world = CreateWorld(new Vector3(0f, 0f, 6f), 1.7f, 8f);
		world.Wildling.ApplyDamage(new DamageInfo(40f, 20f, world.Wildling.transform.position, world.Player, isBackAttack: false, "test.damage", "test"));
		yield return WaitForState(world.Wildling, EnemyState.Chase);
		world.Player.transform.position = new Vector3(0f, 0f, 12f);
		yield return WaitForState(world.Wildling, EnemyState.Return);
		Assert.That<bool>(world.Wildling.HasActiveAttack, (IResolveConstraint)(object)Is.False);
		world.Wildling.NavigationAgent.Warp(world.Wildling.HomePosition);
		yield return WaitForState(world.Wildling, EnemyState.Idle, 300);
		Assert.That<float>(world.Wildling.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)world.Wildling.MaxHealth));
		Assert.That<float>(world.Wildling.CurrentStagger, (IResolveConstraint)(object)Is.Zero);
		yield return Clean(world);
	}

	// F31-020-Repro (18.08.2026, Testermeldung aus v0.3.1): Nach der
	// Leinen-Rueckkehr griff der Gegner nie wieder an. Der alte Leash-Test
	// endet bei Idle+Regeneration und warpt den Heimweg — diese Wache laesst
	// den Gegner REAL heimlaufen und verlangt den erneuten Angriff.
	[UnityTest]
	public IEnumerator NachDerLeinenRueckkehr_GreiftDerGegnerWiederAn()
	{
		TestWorld world = CreateWorld(new Vector3(0f, 0f, 6f), 1.7f, 8f);
		world.Wildling.TryDetectTarget();
		yield return WaitForState(world.Wildling, EnemyState.Chase);
		// Spieler verlaesst die Leine: Gegner soll heimkehren.
		world.Player.transform.position = new Vector3(0f, 0f, 14f);
		Physics.SyncTransforms();
		yield return WaitForState(world.Wildling, EnemyState.Return);
		// Echten Heimlauf abwarten (Spielzeit-Budget, kein Warp).
		float zeit = 0f;
		while (world.Wildling.EnemyState == EnemyState.Return && zeit < 12f)
		{
			zeit += Time.deltaTime;
			yield return null;
		}
		Assert.That(world.Wildling.EnemyState, Is.Not.EqualTo(EnemyState.Return), "Gegner ist nie zu Hause angekommen.");
		// Spieler tritt wieder in die Witterung (Detection 8): Der Gegner
		// muss erneut angreifen.
		world.Player.transform.position = world.Wildling.transform.position + new Vector3(0f, 0f, 4f);
		Physics.SyncTransforms();
		zeit = 0f;
		bool wiederAggressiv = false;
		while (zeit < 8f)
		{
			EnemyState state = world.Wildling.EnemyState;
			if (state == EnemyState.Alert || state == EnemyState.Chase || state == EnemyState.ChooseAttack || state == EnemyState.Telegraph || state == EnemyState.ExecuteAttack)
			{
				wiederAggressiv = true;
				break;
			}
			zeit += Time.deltaTime;
			yield return null;
		}
		Assert.That(wiederAggressiv, Is.True,
			$"Gegner bleibt nach der Rueckkehr passiv (Zustand {world.Wildling.EnemyState}) — er muss den Spieler in Witterung erneut angreifen.");
		yield return Clean(world);
	}

	[UnityTest]
	public IEnumerator DeathDisablesNavigationHitboxAndRaisesLootHook()
	{
		TestWorld world = CreateWorld(new Vector3(0f, 0f, 1.35f), 2.2f, 14f, 0.02f, 1f);
		world.Wildling.TryDetectTarget();
		yield return WaitForState(world.Wildling, EnemyState.ExecuteAttack);
		bool lootRequested = false;
		world.Wildling.LootRequested += delegate
		{
			lootRequested = true;
		};
		world.Wildling.ApplyDamage(new DamageInfo(world.Wildling.MaxHealth, 0f, world.Wildling.transform.position, world.Player, isBackAttack: false, "test.lethal", "test"));
		Assert.That<EnemyState>(world.Wildling.EnemyState, (IResolveConstraint)(object)Is.EqualTo((object)EnemyState.Dead));
		Assert.That<bool>(world.Wildling.NavigationAgent.enabled, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(world.Wildling.HasActiveAttack, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(world.Wildling.AttackHitbox.Hitbox.enabled, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(lootRequested, (IResolveConstraint)(object)Is.True);
		yield return Clean(world);
	}

	[UnityTest]
	public IEnumerator HammerAndDaggersCanBothHitWildling()
	{
		TestWorld hammerWorld = CreateWorld(new Vector3(0f, 0f, 8f));
		TestWorld daggerWorld = CreateWorld(new Vector3(0f, 0f, 8f));
		daggerWorld.Wildling.NavigationAgent.Warp(new Vector3(10f, 0f, 0f));
		daggerWorld.Player.transform.position = new Vector3(10f, 0f, 8f);
		Physics.SyncTransforms();
		GameObject attacker = new GameObject("WildlingTest_Attacker");
		attacker.transform.position = hammerWorld.Wildling.transform.position + Vector3.back * 1.2f;
		attacker.transform.forward = Vector3.forward;
		MeleeWeaponHitbox hitbox = attacker.AddComponent<MeleeWeaponHitbox>();
		try
		{
			float hammerHealth = hammerWorld.Wildling.CurrentHealth;
			float daggerHealth = daggerWorld.Wildling.CurrentHealth;
			ApplyWeaponHit(hitbox, attacker.transform, hammerWorld.Wildling, 15f, 10f, "weapon.hammer.test");
			attacker.transform.position = daggerWorld.Wildling.transform.position + Vector3.back * 1.2f;
			ApplyWeaponHit(hitbox, attacker.transform, daggerWorld.Wildling, 12f, 2.5f, "weapon.daggers.test");
			Assert.That<float>(hammerWorld.Wildling.CurrentHealth, (IResolveConstraint)(object)Is.LessThan((object)hammerHealth));
			Assert.That<float>(daggerWorld.Wildling.CurrentHealth, (IResolveConstraint)(object)Is.LessThan((object)daggerHealth));
			Assert.That<float>(hammerWorld.Wildling.CurrentStagger, (IResolveConstraint)(object)Is.GreaterThan((object)daggerWorld.Wildling.CurrentStagger));
		}
		finally
		{
			UnityEngine.Object.Destroy(attacker);
		}
		yield return Clean(hammerWorld);
		yield return Clean(daggerWorld);
	}

	[UnityTest]
	public IEnumerator NavMeshAgentMovesAroundBlockingObstacle()
	{
		TestWorld world = CreateWorld(new Vector3(0f, 0f, 7f), 0.5f, 14f, 0.05f, 0.18f, 0.08f, addObstacle: true);
		world.Wildling.TryDetectTarget();
		yield return WaitForState(world.Wildling, EnemyState.Chase);
		float maximumSideOffset = 0f;
		float startedAt = Time.time;
		while (Time.time - startedAt < 3f)
		{
			maximumSideOffset = Mathf.Max(maximumSideOffset, Mathf.Abs(world.Wildling.transform.position.x));
			if (world.Wildling.transform.position.z > 5.3f)
			{
				break;
			}
			yield return null;
		}
		Assert.That<float>(maximumSideOffset, (IResolveConstraint)(object)Is.GreaterThan((object)2.1f));
		Assert.That<float>(world.Wildling.transform.position.z, (IResolveConstraint)(object)Is.GreaterThan((object)4.5f));
		yield return Clean(world);
	}

	[UnityTest]
	public IEnumerator OutdoorScenesInitializeExactWildlingCounts()
	{
		string[] scenes = new string[5] { "Zone_Greenwood", "Zone_Quarry", "Zone_Marsh", "Zone_EmberRuins", "HomeBase" };
		// F31-009: Laufzeit-Populator, Soll = regulaere Instanzen der Zonen-Assets
		int[] expected = new int[5] { 5, 6, 6, 14, 0 };
		for (int index = 0; index < scenes.Length; index++)
		{
			yield return LoadScene(scenes[index]);
			WildlingController[] array = UnityEngine.Object.FindObjectsByType<WildlingController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			Assert.That<int>(array.Length, (IResolveConstraint)(object)Is.EqualTo((object)expected[index]), scenes[index], Array.Empty<object>());
			Assert.That<bool>(Array.TrueForAll(array, (WildlingController value) => value.IsAlive && value.NavigationAgent != null && value.NavigationAgent.isOnNavMesh), (IResolveConstraint)(object)Is.True, scenes[index], Array.Empty<object>());
			// Seit dem 3D-Umbau (13.08.2026) tragen die Szenen-Wildlinge die
			// Kreaturenschicht. Die Wache bleibt scharf: der Locator muss die
			// 3D-Schicht liefern, KEINE Sprite-Darstellung darf daneben stehen
			// (ActorPresentationLocator nimmt die erste im Baum — ein
			// Nebeneinander hinge an der Kindreihenfolge), und der Animator
			// (arbeitet gegen IActorPresentation) bleibt Pflicht.
			Assert.That<bool>(Array.TrueForAll(array, (WildlingController value) => ActorPresentationLocator.Find(value.gameObject) is CreatureMeshPresentation && value.GetComponentInChildren<SpriteActorPresentation>(includeInactive: true) == null && value.GetComponentInChildren<SpriteActorAnimator>(includeInactive: true) != null), (IResolveConstraint)(object)Is.True, scenes[index] + ": Wildlings must use the canonical 3D creature visual.", Array.Empty<object>());
			PlayerPrefabBindings playerPrefabBindings = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
			Assert.That<PlayerPrefabBindings>(playerPrefabBindings, (IResolveConstraint)(object)Is.Not.Null, scenes[index], Array.Empty<object>());
			Assert.That<WeaponData>(playerPrefabBindings.Combat.ActiveWeapon, (IResolveConstraint)(object)Is.Null, scenes[index] + ": M8.2 startet ohne Waffe.", Array.Empty<object>());
		}
	}

	[UnityTest]
	public IEnumerator ScenePlayerHitsWildlingWithHammerAndDaggers()
	{
		EidrenServiceRoot services = EidrenServiceRoot.FindOrCreate();
		services.GameSession.StartNewGame();
		Assert.That<bool>(services.GameSession.PlayerEquipment.TryEquip(EquipmentSlot.Weapon1, ItemStack.Create(services.ContentDatabase.GetItem("hammer"), 1), out var hammerError), (IResolveConstraint)(object)Is.True, hammerError, Array.Empty<object>());
		Assert.That<bool>(services.GameSession.PlayerEquipment.TryEquip(EquipmentSlot.Weapon2, ItemStack.Create(services.ContentDatabase.GetItem("daggers"), 1), out var daggerError), (IResolveConstraint)(object)Is.True, daggerError, Array.Empty<object>());
		services.GameSession.SetActiveWeaponId("hammer");
		yield return LoadScene("Zone_Greenwood");
		PlayerPrefabBindings player = UnityEngine.Object.FindFirstObjectByType<PlayerPrefabBindings>();
		ZonePlayerSpawner spawner = UnityEngine.Object.FindFirstObjectByType<ZonePlayerSpawner>();
		WildlingController[] wildlings = UnityEngine.Object.FindObjectsByType<WildlingController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<ZonePlayerSpawner>(spawner, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<int>(wildlings.Length, (IResolveConstraint)(object)Is.EqualTo((object)5));
		WildlingController target = wildlings[0];
		for (int index = 1; index < wildlings.Length; index++)
		{
			wildlings[index].gameObject.SetActive(value: false);
		}
		Vector3 approach = target.transform.position - target.transform.forward * 1.2f + Vector3.up * 0.05f;
		player.Motor.Teleport(approach);
		Vector3 direction = target.transform.position - player.transform.position;
		direction.y = 0f;
		player.transform.rotation = Quaternion.LookRotation(direction.normalized);
		Physics.SyncTransforms();
		float beforeHammer = target.CurrentHealth;
		spawner.Input.PressAttack();
		yield return WaitUntil(() => target.CurrentHealth < beforeHammer, 2f, "Hammer did not hit Wildling.");
		yield return WaitUntil(() => !player.Combat.IsAttacking, 2f, "Hammer attack did not finish.");
		float hammerStagger = target.CurrentStagger;
		spawner.Input.PressSwitchWeapon();
		yield return null;
		Assert.That<string>(player.Combat.ActiveWeapon.Id, (IResolveConstraint)(object)Is.EqualTo((object)"daggers"));
		float beforeDaggers = target.CurrentHealth;
		spawner.Input.PressAttack();
		yield return WaitUntil(() => target.CurrentHealth < beforeDaggers, 2f, "Daggers did not hit Wildling.");
		Assert.That<float>(target.CurrentStagger, (IResolveConstraint)(object)Is.GreaterThan((object)hammerStagger));
	}

	[UnityTest]
	public IEnumerator ReloadingZoneRespawnsDefeatedWildling()
	{
		yield return LoadScene("Zone_Greenwood");
		WildlingController first = UnityEngine.Object.FindFirstObjectByType<WildlingController>();
		Assert.That<WildlingController>(first, (IResolveConstraint)(object)Is.Not.Null);
		first.ApplyDamage(new DamageInfo(first.MaxHealth, 0f, first.transform.position, null, isBackAttack: false, "test.scene.lethal", "test"));
		Assert.That<bool>(first.IsAlive, (IResolveConstraint)(object)Is.False);
		yield return LoadScene("Zone_Greenwood");
		WildlingController[] array = UnityEngine.Object.FindObjectsByType<WildlingController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		Assert.That<int>(array.Length, (IResolveConstraint)(object)Is.EqualTo((object)5));
		Assert.That<bool>(Array.TrueForAll(array, (WildlingController value) => value.IsAlive && value.CurrentHealth == value.MaxHealth), (IResolveConstraint)(object)Is.True);
	}

	private static void ApplyWeaponHit(MeleeWeaponHitbox hitbox, Transform attacker, WildlingController target, float damage, float stagger, string attackId)
	{
		AttackStepData step = new AttackStepData
		{
			HitboxRange = 2.5f,
			HitboxAngle = 140f
		};
		hitbox.BeginWindow(step);
		hitbox.Evaluate(attacker, delegate(CombatTarget combatTarget)
		{
			combatTarget.Damageable.ApplyDamage(new DamageInfo(damage, stagger, target.transform.position + Vector3.up, attacker.gameObject, isBackAttack: false, attackId, "player"));
		});
		hitbox.EndWindow();
	}

	private static TestWorld CreateWorld(Vector3 playerPosition, float attackRange = 1.7f, float leashRange = 14f, float telegraphDuration = 0.05f, float attackWindowDuration = 0.18f, float recoveryDuration = 0.08f, bool addObstacle = false)
	{
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		gameObject.name = "WildlingTest_Ground";
		gameObject.transform.position = new Vector3(0f, -0.5f, 0f);
		gameObject.transform.localScale = new Vector3(40f, 1f, 40f);
		GameObject obstacle = null;
		if (addObstacle)
		{
			obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
			obstacle.name = "WildlingTest_Obstacle";
			obstacle.transform.position = new Vector3(0f, 1f, 3.4f);
			obstacle.transform.localScale = new Vector3(4f, 2f, 1.6f);
		}
		Physics.SyncTransforms();
		GameObject navigationRoot = new GameObject("WildlingTest_Navigation");
		NavMeshSurface surface = navigationRoot.AddComponent<NavMeshSurface>();
		surface.collectObjects = CollectObjects.All;
		surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
		surface.BuildNavMesh();
		GameObject player = new GameObject("WildlingTest_Player");
		player.transform.position = playerPosition;
		CapsuleCollider capsuleCollider = player.AddComponent<CapsuleCollider>();
		capsuleCollider.radius = 0.48f;
		capsuleCollider.height = 2f;
		capsuleCollider.center = Vector3.up;
		Damageable playerHealth = player.AddComponent<Damageable>();
		playerHealth.Initialize(500f);
		GameObject wildlingObject = new GameObject("WildlingTest_Wildling");
		wildlingObject.AddComponent<CapsuleCollider>();
		GameObject visual = new GameObject("Visual");
		visual.transform.SetParent(wildlingObject.transform, worldPositionStays: false);
		GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cube);
		telegraph.name = "Telegraph";
		telegraph.transform.SetParent(wildlingObject.transform, worldPositionStays: false);
		telegraph.transform.localPosition = new Vector3(0f, 0.05f, 0.9f);
		telegraph.transform.localScale = new Vector3(1.5f, 0.05f, 1.7f);
		UnityEngine.Object.Destroy(telegraph.GetComponent<Collider>());
		telegraph.SetActive(value: false);
		GameObject gameObject2 = new GameObject("AttackHitbox");
		gameObject2.transform.SetParent(wildlingObject.transform, worldPositionStays: false);
		gameObject2.transform.localPosition = new Vector3(0f, 1f, 1f);
		BoxCollider attackCollider = gameObject2.AddComponent<BoxCollider>();
		attackCollider.size = new Vector3(1.6f, 1.8f, 1.5f);
		EnemyMeleeHitbox enemyHitbox = wildlingObject.AddComponent<EnemyMeleeHitbox>();
		enemyHitbox.Configure(attackCollider);
		EnemyDefinition definition = Definition(attackRange, leashRange, telegraphDuration, attackWindowDuration, recoveryDuration);
		WildlingController wildling = wildlingObject.AddComponent<WildlingController>();
		wildling.ConfigurePrefab(definition, enemyHitbox, telegraph, visual.transform);
		wildling.Initialize(player.transform, playerHealth);
		return new TestWorld(gameObject, obstacle, navigationRoot, surface, player, playerHealth, wildling, definition);
	}

	private static EnemyDefinition Definition(float attackRange, float leashRange, float telegraphDuration, float attackWindowDuration, float recoveryDuration)
	{
		EnemyDefinition enemyDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();
		Set(enemyDefinition, "id", "enemy.wildling.test");
		Set(enemyDefinition, "displayName", "Wildling Test");
		Set(enemyDefinition, "maximumHealth", 180f);
		Set(enemyDefinition, "maximumStagger", 90f);
		Set(enemyDefinition, "staggerDuration", 0.08f);
		Set(enemyDefinition, "attackDamage", 18f);
		Set(enemyDefinition, "telegraphDuration", telegraphDuration);
		Set(enemyDefinition, "attackWindowDuration", attackWindowDuration);
		Set(enemyDefinition, "recoveryDuration", recoveryDuration);
		Set(enemyDefinition, "deathDisableDelay", 10f);
		Set(enemyDefinition, "navigation", new EnemyNavigationData
		{
			MoveSpeed = 3.6f,
			Acceleration = 14f,
			AngularSpeed = 360f,
			AttackRange = attackRange,
			DetectionRange = 8f,
			LeashRange = leashRange,
			LeashFollowDistance = 1f,
			PatrolRadius = 0f,
			PatrolWait = 0.01f,
			AlertDuration = 0.02f,
			ReturnTolerance = 0.4f,
			NavMeshSampleDistance = 3f,
			AgentRadius = 0.52f,
			AgentHeight = 2.1f
		});
		return enemyDefinition;
	}

	private static void Set(EnemyDefinition target, string fieldName, object value)
	{
		typeof(EnemyDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
	}

	private static IEnumerator WaitForState(WildlingController wildling, EnemyState expected, int maximumFrames = 180)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			if (wildling.EnemyState == expected)
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail($"Wildling did not enter {expected}; current state is " + $"{wildling.EnemyState}.");
	}

	private static IEnumerator LoadScene(string sceneName)
	{
		AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
		Assert.That<AsyncOperation>(operation, (IResolveConstraint)(object)Is.Not.Null);
		while (!operation.isDone)
		{
			yield return null;
		}
		for (int frame = 0; frame < 3; frame++)
		{
			yield return null;
		}
	}

	private static IEnumerator WaitUntil(Func<bool> predicate, float timeout, string failure)
	{
		float startedAt = Time.time;
		while (Time.time - startedAt < timeout)
		{
			if (predicate())
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail(failure);
	}

	private static IEnumerator Clean(TestWorld world)
	{
		if (world.Surface != null)
		{
			world.Surface.RemoveData();
		}
		UnityEngine.Object.Destroy(world.Definition);
		UnityEngine.Object.Destroy(world.NavigationRoot);
		UnityEngine.Object.Destroy(world.Obstacle);
		UnityEngine.Object.Destroy(world.Ground);
		UnityEngine.Object.Destroy(world.Wildling.gameObject);
		UnityEngine.Object.Destroy(world.Player);
		yield return null;
	}
}
}
