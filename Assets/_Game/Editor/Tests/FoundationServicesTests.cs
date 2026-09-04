using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class FoundationServicesTests
{
	[Test]
	public void GameSession_Initialize_IsIdempotent()
	{
		GameObject root = new GameObject("GameSession_Test");
		try
		{
			GameSession gameSession = root.AddComponent<GameSession>();
			gameSession.Initialize();
			string firstId = gameSession.SessionId;
			gameSession.Initialize();
			Assert.That<bool>(gameSession.IsInitialized, (IResolveConstraint)(object)Is.True);
			Assert.That<string>(gameSession.SessionId, (IResolveConstraint)(object)Is.EqualTo((object)firstId));
			Assert.That<string>(gameSession.SessionId, (IResolveConstraint)(object)Is.Not.Empty);
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void ContentDatabase_ResolvesRegisteredContentByStableId()
	{
		GameObject root = new GameObject("ContentDatabase_Test");
		WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
		EidraData eidra = ScriptableObject.CreateInstance<EidraData>();
		AbilityData ability = ScriptableObject.CreateInstance<AbilityData>();
		BossData boss = ScriptableObject.CreateInstance<BossData>();
		try
		{
			SetId(weapon, "test-weapon");
			SetId(eidra, "test-eidra");
			SetId(ability, "test-ability");
			SetId(boss, "test-boss");
			ContentDatabase database = root.AddComponent<ContentDatabase>();
			database.Configure(new WeaponData[1] { weapon }, new EidraData[1] { eidra }, new AbilityData[1] { ability }, new BossData[1] { boss });
			Assert.That<WeaponData>(database.GetWeapon("test-weapon"), (IResolveConstraint)(object)Is.SameAs((object)weapon));
			Assert.That<EidraData>(database.GetEidra("test-eidra"), (IResolveConstraint)(object)Is.SameAs((object)eidra));
			Assert.That<AbilityData>(database.GetAbility("test-ability"), (IResolveConstraint)(object)Is.SameAs((object)ability));
			Assert.That<BossData>(database.GetBoss("test-boss"), (IResolveConstraint)(object)Is.SameAs((object)boss));
		}
		finally
		{
			Object.DestroyImmediate(root);
			Object.DestroyImmediate(weapon);
			Object.DestroyImmediate(eidra);
			Object.DestroyImmediate(ability);
			Object.DestroyImmediate(boss);
		}
	}

	[Test]
	public void SceneFlowService_CanBeInstantiated()
	{
		GameObject root = new GameObject("SceneFlowService_Test");
		try
		{
			Assert.That<SceneFlowService>(root.AddComponent<SceneFlowService>(), (IResolveConstraint)(object)Is.Not.Null);
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	private static void SetId(ScriptableObject value, string id)
	{
		SerializedObject serializedObject = new SerializedObject(value);
		serializedObject.FindProperty("id").stringValue = id;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
	}
}
}
