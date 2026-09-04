using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using Eidren.AI;
using Eidren.Core;
using Eidren.Data;
using Eidren.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
	public sealed class PersistentCombatTargetQueryTests
	{
		private sealed class ProbeEnemy : EnemyControllerBase
		{
			protected override IEnumerator ExecuteAttack() { yield break; }
		}
		private readonly List<GameObject> _objects = new List<GameObject>();
		private readonly List<IDamageable> _registered = new List<IDamageable>();
		private Transform _owner;
		private PersistentCombatTargetQuery _query;
		private static readonly Vector3 Origin = new Vector3(5000, 0, 5000);
		[SetUp] public void Setup()
		{
			_owner = Make("Owner", Vector3.zero).transform;
			_query = new PersistentCombatTargetQuery(_owner);
		}
		[TearDown] public void Cleanup()
		{
			foreach (IDamageable target in _registered) CombatTargetRegistry.Unregister(target);
			_registered.Clear();
			foreach (GameObject obj in _objects) if (obj != null) Object.DestroyImmediate(obj);
			_objects.Clear();
		}
		private GameObject Make(string name, Vector3 position)
		{
			var obj = new GameObject(name); obj.transform.position = Origin + position; _objects.Add(obj); return obj;
		}
		private Damageable Target(Vector3 position, float size = 1)
		{
			GameObject obj = Make("Target", position);
			obj.AddComponent<BoxCollider>().size = new Vector3(size, 2, size);
			Damageable health = obj.AddComponent<Damageable>(); health.Initialize(100);
			// OnEnable eines normalen MonoBehaviours laeuft im EditMode nicht.
			CombatTargetRegistry.Register(health); _registered.Add(health);
			Physics.SyncTransforms(); return health;
		}
		private EnemyControllerBase Enemy(Vector3 position, float size = 1)
		{
			Damageable health = Target(position, size);
			var enemy = health.gameObject.AddComponent<ProbeEnemy>();
			typeof(EnemyControllerBase).GetField("_health", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, health);
			CombatTargetRegistry.Register(enemy); _registered.Add(enemy); return enemy;
		}
		[Test] public void NoTargetIsSafe()
		{
			_query.Tick(0, true); Assert.That(_query.Current.Identity, Is.Zero);
			Assert.That(_query.TryFindClosest(_owner, 3, out _), Is.False);
		}
		[Test] public void SurfaceDistanceAllowsLargeTargetOutsideCenterRange()
		{
			Damageable target = Target(new Vector3(0, 0, 6), 8);
			Assert.That(_query.TryFindClosest(_owner, 2.1f, out CombatTarget found), Is.True);
			Assert.That(found.Damageable, Is.SameAs(target)); Assert.That(_query.Current.Distance, Is.EqualTo(2).Within(.01f));
		}
		[Test] public void DeadTargetClearsBeforeNextScheduledScan()
		{
			Damageable target = Target(Vector3.forward * 2); _query.Tick(0, true);
			Assert.That(_query.Current.Identity, Is.Not.Zero);
			target.ApplyDamage(new DamageInfo(1000, 0, target.transform.position, _owner.gameObject, false, "test.hit", "test"));
			Assert.That(_query.Current.Identity, Is.Zero);
		}
		[Test] public void DisabledAndDestroyedTargetsClearImmediately()
		{
			Damageable target = Target(Vector3.forward * 2); _query.Tick(0, true);
			Assert.That(_query.Current.Identity, Is.Not.Zero);
			target.gameObject.SetActive(false); Assert.That(_query.Current.Identity, Is.Zero);
			target.gameObject.SetActive(true); _query.Tick(1, true); Object.DestroyImmediate(target.gameObject);
			Assert.That(_query.Current.Identity, Is.Zero);
		}
		[Test] public void OwnHierarchyIsExcluded()
		{
			Damageable target = Target(Vector3.forward); target.transform.SetParent(_owner);
			_query.Tick(0, true); Assert.That(_query.Current.Identity, Is.Zero);
		}
		[Test] public void WallBlocksAcquisition()
		{
			Target(Vector3.forward * 4);
			Make("Wall", Vector3.forward * 2).AddComponent<BoxCollider>().size = new Vector3(4, 4, 1);
			Physics.SyncTransforms(); _query.Tick(0, true); Assert.That(_query.Current.Identity, Is.Zero);
		}
		[Test] public void TriggerDoesNotBlock()
		{
			Target(Vector3.forward * 4); Make("Trigger", Vector3.forward * 2).AddComponent<BoxCollider>().isTrigger = true;
			Physics.SyncTransforms(); _query.Tick(0, true); Assert.That(_query.Current.Identity, Is.Not.Zero);
		}
		[Test] public void NoDuplicateChangeEventsForUnchangedVisibleState()
		{
			Target(Vector3.forward * 2); int events = 0; _query.Changed += _ => events++;
			_query.Tick(0, true); _query.Tick(1, true); Assert.That(events, Is.EqualTo(1));
			Assert.That(_query.Current.ConfirmedAt, Is.EqualTo(1));
			_query.Clear(); _query.Clear(); Assert.That(events, Is.EqualTo(2));
		}
		[Test] public void SnapshotReadsActualHealth()
		{
			Damageable target = Target(Vector3.forward * 2); _query.Tick(0, true);
			target.ApplyDamage(new DamageInfo(10, 0, target.transform.position, _owner.gameObject, false, "test.hit", "test"));
			_query.Tick(1, true); Assert.That(_query.Current.Vitals.Health, Is.EqualTo(new Vector2(90, 100)));
		}
		[Test] public void RangeFallbackDoesNotReplaceSharedIdentity()
		{
			Damageable far = Target(Vector3.forward * 5); _query.Tick(0, true);
			Damageable near = Target(Vector3.back * 2);
			Assert.That(_query.TryFindClosest(_owner, 3, out CombatTarget result), Is.True);
			Assert.That(result.Damageable, Is.SameAs(near)); Assert.That(_query.Current.Target.Damageable, Is.SameAs(far));
		}
		[Test] public void OwnerDeactivationClearsSnapshot()
		{
			Target(Vector3.forward * 2); _query.Tick(0, true); _owner.gameObject.SetActive(false);
			Assert.That(_query.Current.Identity, Is.Zero);
		}
		[Test] public void EnemyWrapperWinsAndReturnCannotBypassFilterThroughDamageable()
		{
			EnemyControllerBase enemy = Enemy(Vector3.forward * 2); _query.Tick(0, true);
			Assert.That(_query.Current.Target.Damageable, Is.SameAs(enemy));
			CombatTargetRegistry.Unregister(enemy.GetComponent<Damageable>());
			CombatTargetRegistry.Register(enemy.GetComponent<Damageable>()); _query.Tick(1, true);
			Assert.That(_query.Current.Target.Damageable, Is.SameAs(enemy));
			var machine = (EnemyStateMachine)typeof(EnemyControllerBase).GetField("_stateMachine", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(enemy);
			machine.TrySetState(EnemyState.Return); _query.Tick(2, true);
			Assert.That(_query.Current.Identity, Is.Zero);
		}
		[Test] public void AbilityFilterMayRejectSharedTargetWithoutCompetingPersistentState()
		{
			EnemyControllerBase far = Enemy(Vector3.forward * 6, 8); _query.Tick(0, true);
			EnemyControllerBase near = Enemy(Vector3.back * 2);
			Assert.That(_query.TryFindEnemy(3, out EnemyControllerBase selected,
				enemy => Vector3.Distance(_owner.position, enemy.transform.position) <= 3), Is.True);
			Assert.That(selected, Is.SameAs(near)); Assert.That(_query.Current.Target.Damageable, Is.SameAs(far));
		}
		[TestCase(30f, WeaponFamily.Hammer, 120f, true)]
		[TestCase(58.5f, WeaponFamily.Hammer, 120f, true)]
		[TestCase(61.5f, WeaponFamily.Hammer, 120f, false)]
		[TestCase(37f, WeaponFamily.Daggers, 75f, true)]
		[TestCase(24.5f, WeaponFamily.Spear, 50f, true)]
		[TestCase(180f, WeaponFamily.Hammer, 120f, false)]
		public void FacingUsesWeaponConeAndBoundedOneShotTurn(float angle, WeaponFamily family, float cone, bool turns)
		{
			Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
			Damageable target = Target(direction * 2, .1f);
			WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
			try
			{
				weapon.AttackAssistAngle = cone;
				weapon.Identity = new WeaponIdentityData { Family = family };
				Vector3 targetDirection = CombatTargetGeometry.ClosestBodyPoint(target.transform, _owner.position) - _owner.position;
				targetDirection.y = 0f;
				float measuredAngle = Vector3.Angle(Vector3.forward, targetDirection.normalized);
				float expectedTurn = turns ? CombatTargetFacingRules.ResolveTurnDegrees(measuredAngle, CombatTargetFacingRules.For(family)) : 0f;
				CombatTargetFacing.Apply(_owner, _query, new AttackStepData { HitboxRange = 3 }, weapon);
				float actualTurn = Vector3.Angle(Vector3.forward, _owner.forward);
				Assert.That(actualTurn > .1f, Is.EqualTo(turns));
				Assert.That(actualTurn, Is.EqualTo(expectedTurn).Within(.06f));
				Assert.That(_owner.position, Is.EqualTo(Origin));
			}
			finally { Object.DestroyImmediate(weapon); }
		}
		[Test] public void FacingWithoutTargetChangesNeitherPositionNorRotation()
		{
			WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
			try
			{
				weapon.AttackAssistAngle = 120f;
				weapon.Identity = new WeaponIdentityData { Family = WeaponFamily.Hammer };
				CombatTargetFacing.Apply(_owner, _query, new AttackStepData { HitboxRange = 3f }, weapon);
				Assert.That(_owner.position, Is.EqualTo(Origin));
				Assert.That(_owner.rotation, Is.EqualTo(Quaternion.identity));
			}
			finally { Object.DestroyImmediate(weapon); }
		}
		[TestCase("Assets/_Game/Data/Weapons/Hammer.asset", WeaponFamily.Hammer, 3)]
		[TestCase("Assets/_Game/Data/Weapons/Daggers.asset", WeaponFamily.Daggers, 4)]
		[TestCase("Assets/_Game/Data/Weapons/CopperSpear.asset", WeaponFamily.Spear, 3)]
		public void EveryWeaponFamilyComboStepUsesBoundedFacing(string path, WeaponFamily family, int stepCount)
		{
			Target((Quaternion.Euler(0f, 20f, 0f) * Vector3.forward) * 2f, .1f);
			WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
			Assert.That(weapon, Is.Not.Null);
			Assert.That(weapon.Identity.Family, Is.EqualTo(family));
			Assert.That(weapon.Combo, Has.Length.EqualTo(stepCount));
			foreach (AttackStepData step in weapon.Combo)
			{
				_owner.rotation = Quaternion.identity;
				CombatTargetFacing.Apply(_owner, _query, step, weapon);
				float turn = Vector3.Angle(Vector3.forward, _owner.forward);
				Assert.That(turn, Is.GreaterThan(0f));
				Assert.That(turn, Is.LessThanOrEqualTo(CombatTargetFacingRules.For(family).MaximumTurnDegrees + .01f));
			}
		}
		[Test] public void TransitionCancellationClearsSharedTarget()
		{
			PlayerCombatController combat = _owner.gameObject.AddComponent<PlayerCombatController>();
			typeof(PlayerCombatController).GetField("_targetQuery", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(combat, _query);
			Target(Vector3.forward * 2); _query.Tick(0, true); Assert.That(_query.Current.Identity, Is.Not.Zero);
			combat.CancelForSceneTransition(); Assert.That(_query.Current.Identity, Is.Zero);
		}

		[Test] public void WarmMultiTargetScan_ReportsCpuAndAllocations()
		{
			for (int i = 0; i < 48; i++)
			{
				float angle = i * Mathf.PI * 2f / 48;
				Target(new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * 8, .2f);
			}
			for (int i = 0; i < 20; i++) _query.Tick(i, true);
			var timer = new System.Diagnostics.Stopwatch();
			long before = System.GC.GetAllocatedBytesForCurrentThread();
			timer.Start();
			for (int i = 0; i < 200; i++) _query.Tick(20 + i, true);
			timer.Stop();
			long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
			TestContext.WriteLine("HUD100 48 targets, 200 forced warm scans: " + timer.Elapsed.TotalMilliseconds + " ms; managed bytes=" + allocated);
			Assert.That(_query.Current.Identity, Is.Not.Zero);
			Assert.That(allocated, Is.Zero, "Warme Zielbewertung darf keinen Managed-Garbage erzeugen.");
		}
	}
}
