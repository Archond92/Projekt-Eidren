using Eidren.AI;
using Eidren.Combat;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class CombatGeneralizationTests
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static TestDelegate _003C_003E9__2_0;

		public static TestDelegate _003C_003E9__2_1;

		internal void _003CDamageInfo_RejectsMissingStableIds_003Eb__2_0()
		{
			new DamageInfo(1f, 0f, Vector3.zero, null, isBackAttack: false, string.Empty, "player");
		}

		internal void _003CDamageInfo_RejectsMissingStableIds_003Eb__2_1()
		{
			new DamageInfo(1f, 0f, Vector3.zero, null, isBackAttack: false, "weapon.hammer.combo.1", string.Empty);
		}
	}

	[Test]
	public void GeneralTargetQuery_HitsPlainDamageableDummyOnlyOncePerStep()
	{
		GameObject attacker = new GameObject("CombatTest_Attacker");
		GameObject dummyObject = new GameObject("CombatTest_Dummy");
		try
		{
			attacker.transform.position = Vector3.zero;
			dummyObject.transform.position = new Vector3(0f, 0f, 2f);
			CombatTestDamageable dummy = dummyObject.AddComponent<CombatTestDamageable>();
			CombatTargetRegistry.Register(dummy);
			Assert.That<bool>(new SceneCombatTargetQuery().TryFindClosest(attacker.transform, 3f, out var target), (IResolveConstraint)(object)Is.True);
			Assert.That<IDamageable>(target.Damageable, (IResolveConstraint)(object)Is.SameAs((object)dummy));
			Assert.That<bool>(target.IsBackAttack(attacker.transform), (IResolveConstraint)(object)Is.True);
			DamageInfo damage = new DamageInfo(25f, 4f, dummyObject.transform.position, attacker, isBackAttack: true, "weapon.hammer.combo.1", "player");
			HashSet<int> hitTargetIds = new HashSet<int>();
			Assert.That<bool>(CombatHitResolver.TryApply(target, damage, hitTargetIds), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(CombatHitResolver.TryApply(target, damage, hitTargetIds), (IResolveConstraint)(object)Is.False);
			Assert.That<float>(dummy.Health, (IResolveConstraint)(object)Is.EqualTo((object)75f));
			Assert.That<string>(dummy.LastDamage.AttackId, (IResolveConstraint)(object)Is.EqualTo((object)"weapon.hammer.combo.1"));
			Assert.That<string>(dummy.LastDamage.SourceId, (IResolveConstraint)(object)Is.EqualTo((object)"player"));
		}
		finally
		{
			CombatTargetRegistry.Unregister((dummyObject != null) ? dummyObject.GetComponent<CombatTestDamageable>() : null);
			UnityEngine.Object.DestroyImmediate(dummyObject);
			UnityEngine.Object.DestroyImmediate(attacker);
		}
	}

	[Test]
	public void Garon_ImplementsGeneralCombatTargetContracts()
	{
		Assert.That<bool>(typeof(IDamageable).IsAssignableFrom(typeof(BossController)), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(typeof(IStaggerable).IsAssignableFrom(typeof(BossController)), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(typeof(IBackAttackTarget).IsAssignableFrom(typeof(BossController)), (IResolveConstraint)(object)Is.True);
	}

	[Test]
	public void DamageInfo_RejectsMissingStableIds()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		object obj = _003C_003Ec._003C_003E9__2_0;
		if (obj == null)
		{
			TestDelegate val = delegate
			{
				new DamageInfo(1f, 0f, Vector3.zero, null, isBackAttack: false, string.Empty, "player");
			};
			_003C_003Ec._003C_003E9__2_0 = val;
			obj = (object)val;
		}
		Assert.Throws<ArgumentException>((TestDelegate)obj);
		object obj2 = _003C_003Ec._003C_003E9__2_1;
		if (obj2 == null)
		{
			TestDelegate val2 = delegate
			{
				new DamageInfo(1f, 0f, Vector3.zero, null, isBackAttack: false, "weapon.hammer.combo.1", string.Empty);
			};
			_003C_003Ec._003C_003E9__2_1 = val2;
			obj2 = (object)val2;
		}
		Assert.Throws<ArgumentException>((TestDelegate)obj2);
	}
}

public sealed class CombatTestDamageable : MonoBehaviour, IDamageable
{
	public float Health { get; private set; } = 100f;

	public DamageInfo LastDamage { get; private set; }

	public bool IsAlive => Health > 0f;

	public Transform TargetTransform => base.transform;

	public void ApplyDamage(DamageInfo damage)
	{
		LastDamage = damage;
		Health = Mathf.Max(0f, Health - damage.HealthDamage);
	}
}
}
