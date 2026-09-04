using Eidren.Combat;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class MeleeHitboxTests
{
	[Test]
	public void HitboxGeometry_EnforcesWeaponRangeAngleAndFront()
	{
		Vector3 zero = Vector3.zero;
		Vector3 forward = Vector3.forward;
		Vector3 targetAtFiftyDegrees = Quaternion.Euler(0f, 50f, 0f) * forward * 2.5f;
		Assert.That<bool>(MeleeHitboxGeometry.Contains(zero, forward, targetAtFiftyDegrees, 3.4f, 120f), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(MeleeHitboxGeometry.Contains(zero, forward, targetAtFiftyDegrees, 3f, 75f), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(MeleeHitboxGeometry.Contains(zero, forward, forward * 3.2f, 3.4f, 120f), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(MeleeHitboxGeometry.Contains(zero, forward, forward * 3.2f, 3f, 75f), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(MeleeHitboxGeometry.Contains(zero, forward, -forward, 3.4f, 120f), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(MeleeHitboxGeometry.Contains(zero, forward, -forward, 3.4f, 120f, omnidirectional: true), (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void ActiveHitWindow_HitsPhysicalTargetOnlyOnce()
	{
		GameObject attacker = new GameObject("HitboxTest_Attacker");
		GameObject targetObject = new GameObject("HitboxTest_Target");
		try
		{
			MeleeWeaponHitbox meleeWeaponHitbox = attacker.AddComponent<MeleeWeaponHitbox>();
			targetObject.transform.position = Vector3.forward * 2f;
			targetObject.AddComponent<BoxCollider>();
			CombatTestDamageable target = targetObject.AddComponent<CombatTestDamageable>();
			CombatTargetRegistry.Register(target);
			Physics.SyncTransforms();
			AttackStepData step = new AttackStepData
			{
				HitboxRange = 3f,
				HitboxAngle = 90f,
				HitWindowDuration = 0.1f
			};
			int opened = 0;
			int closed = 0;
			int callbacks = 0;
			meleeWeaponHitbox.WindowOpened += delegate
			{
				opened++;
			};
			meleeWeaponHitbox.WindowClosed += delegate
			{
				closed++;
			};
			meleeWeaponHitbox.BeginWindow(step);
			meleeWeaponHitbox.Evaluate(attacker.transform, delegate(CombatTarget combatTarget)
			{
				callbacks++;
				combatTarget.Damageable.ApplyDamage(new DamageInfo(10f, 0f, targetObject.transform.position, attacker, isBackAttack: false, "weapon.test.combo.1", "player"));
			});
			meleeWeaponHitbox.Evaluate(attacker.transform, delegate
			{
				callbacks++;
			});
			meleeWeaponHitbox.EndWindow();
			meleeWeaponHitbox.EndWindow();
			Assert.That<int>(opened, (IResolveConstraint)(object)Is.EqualTo((object)1));
			Assert.That<int>(closed, (IResolveConstraint)(object)Is.EqualTo((object)1));
			Assert.That<int>(callbacks, (IResolveConstraint)(object)Is.EqualTo((object)1));
			Assert.That<int>(meleeWeaponHitbox.HitCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
			Assert.That<float>(target.Health, (IResolveConstraint)(object)Is.EqualTo((object)90f));
			Assert.That<bool>(meleeWeaponHitbox.IsWindowActive, (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			CombatTargetRegistry.Unregister(targetObject.GetComponent<CombatTestDamageable>());
			Object.DestroyImmediate(targetObject);
			Object.DestroyImmediate(attacker);
		}
	}

	[Test]
	public void LargeTarget_IsHitByItsBodyEvenWhenItsPivotIsOutOfRange()
	{
		GameObject attacker = new GameObject("HitboxTest_Surface_Attacker");
		GameObject targetObject = new GameObject("HitboxTest_Surface_Target");
		try
		{
			MeleeWeaponHitbox hitbox = attacker.AddComponent<MeleeWeaponHitbox>();
			targetObject.transform.position = Vector3.forward * 4f;
			BoxCollider body = targetObject.AddComponent<BoxCollider>();
			body.size = new Vector3(2f, 2f, 3f);
			CombatTestDamageable target = targetObject.AddComponent<CombatTestDamageable>();
			CombatTargetRegistry.Register(target);
			Physics.SyncTransforms();
			hitbox.BeginWindow(new AttackStepData
			{
				HitboxRange = 3f,
				HitboxAngle = 90f,
				HitWindowDuration = 0.1f
			});
			int hits = hitbox.Evaluate(attacker.transform, delegate { });
			Assert.That(hits, Is.EqualTo(1), "Die 2,5 m entfernte Koerperoberflaeche liegt in Reichweite, obwohl der Pivot 4 m entfernt ist.");
		}
		finally
		{
			CombatTargetRegistry.Unregister(targetObject.GetComponent<CombatTestDamageable>());
			Object.DestroyImmediate(targetObject);
			Object.DestroyImmediate(attacker);
		}
	}

	[Test]
	public void TargetQuery_UsesPhysicalSurfaceInsteadOfPivot()
	{
		GameObject attacker = new GameObject("HitboxTest_Query_Attacker");
		GameObject targetObject = new GameObject("HitboxTest_Query_Target");
		try
		{
			targetObject.transform.position = Vector3.forward * 4f;
			BoxCollider body = targetObject.AddComponent<BoxCollider>();
			body.size = new Vector3(2f, 2f, 3f);
			CombatTestDamageable target = targetObject.AddComponent<CombatTestDamageable>();
			CombatTargetRegistry.Register(target);
			Physics.SyncTransforms();
			Assert.That(new SceneCombatTargetQuery().TryFindClosest(attacker.transform, 3f, out CombatTarget found), Is.True);
			Assert.That(found.Damageable, Is.SameAs(target));
		}
		finally
		{
			CombatTargetRegistry.Unregister(targetObject.GetComponent<CombatTestDamageable>());
			Object.DestroyImmediate(targetObject);
			Object.DestroyImmediate(attacker);
		}
	}

	[Test]
	public void WeaponAssets_ExposeRequiredHitboxDifferences()
	{
		WeaponData hammer = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Hammer.asset");
		WeaponData daggers = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Daggers.asset");
		Assert.That<WeaponData>(hammer, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<WeaponData>(daggers, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<float>(hammer.AttackRange, (IResolveConstraint)(object)Is.GreaterThan((object)daggers.AttackRange));
		Assert.That<float>(hammer.AttackAssistAngle, (IResolveConstraint)(object)Is.GreaterThan((object)daggers.AttackAssistAngle));
		Assert.That<float>(hammer.BaseStaggerDamage, (IResolveConstraint)(object)Is.GreaterThan((object)daggers.BaseStaggerDamage));
		Assert.That<float>(daggers.BackDamageMultiplier, (IResolveConstraint)(object)Is.GreaterThan((object)hammer.BackDamageMultiplier));
		AttackStepData[] combo = hammer.Combo;
		for (int i = 0; i < combo.Length; i++)
		{
			AssertValidWindow(combo[i]);
		}
		combo = daggers.Combo;
		for (int i = 0; i < combo.Length; i++)
		{
			AssertValidWindow(combo[i]);
		}
	}

	private static void AssertValidWindow(AttackStepData step)
	{
		Assert.That<float>(step.HitWindowDuration, (IResolveConstraint)(object)Is.GreaterThan((object)0f));
		Assert.That<float>(step.HitTime, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)0f));
		Assert.That<float>(step.HitTime + step.HitWindowDuration, (IResolveConstraint)(object)Is.LessThanOrEqualTo((object)step.Duration));
		Assert.That<float>(step.HitboxRange, (IResolveConstraint)(object)Is.GreaterThan((object)0f));
		Assert.That<float>(step.HitboxAngle, (IResolveConstraint)(object)Is.GreaterThan((object)0f));
	}
}
}
