using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class CoreDataSmokeTests
{
	[Test]
	public void CoreDataScriptableObjects_CanBeInstantiated()
	{
		AbilityData ability = null;
		BossData boss = null;
		EidraData eidra = null;
		WeaponData weapon = null;
		try
		{
			ability = ScriptableObject.CreateInstance<AbilityData>();
			boss = ScriptableObject.CreateInstance<BossData>();
			eidra = ScriptableObject.CreateInstance<EidraData>();
			weapon = ScriptableObject.CreateInstance<WeaponData>();
			Assert.That<AbilityData>(ability, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<BossData>(boss, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<EidraData>(eidra, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<WeaponData>(weapon, (IResolveConstraint)(object)Is.Not.Null);
		}
		finally
		{
			Object.DestroyImmediate(ability);
			Object.DestroyImmediate(boss);
			Object.DestroyImmediate(eidra);
			Object.DestroyImmediate(weapon);
		}
	}
}
}
