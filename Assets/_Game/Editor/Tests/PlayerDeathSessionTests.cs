using Eidren.Combat;
using Eidren.Core.Services;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class PlayerDeathSessionTests
{
	[Test]
	public void Damageable_ReportsDeathExactlyOnce()
	{
		GameObject root = new GameObject("Damageable_DeathOnce_Test");
		try
		{
			Damageable damageable = root.AddComponent<Damageable>();
			damageable.Initialize(100f);
			int notifications = 0;
			damageable.Died += delegate
			{
				notifications++;
			};
			damageable.ApplyDamage(Lethal(root));
			damageable.ApplyDamage(Lethal(root));
			Assert.That<bool>(damageable.IsAlive, (IResolveConstraint)(object)Is.False);
			Assert.That<int>(notifications, (IResolveConstraint)(object)Is.EqualTo((object)1));
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void Session_DeathAndRespawnAreSingleAtomicTransition()
	{
		GameObject root = new GameObject("Session_Respawn_Test");
		try
		{
			GameSession gameSession = root.AddComponent<GameSession>();
			gameSession.StartNewGame();
			Assert.That<bool>(gameSession.RecordPlayerDeath(), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(gameSession.RecordPlayerDeath(), (IResolveConstraint)(object)Is.False);
			Assert.That<PlayerLifeState>(gameSession.LifeState, (IResolveConstraint)(object)Is.EqualTo((object)PlayerLifeState.Dead));
			Assert.That<bool>(gameSession.BeginRespawn("home_base", "HomeBase"), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(gameSession.BeginRespawn("home_base", "HomeBase"), (IResolveConstraint)(object)Is.False);
			Assert.That<PlayerLifeState>(gameSession.LifeState, (IResolveConstraint)(object)Is.EqualTo((object)PlayerLifeState.Respawning));
			Assert.That<bool>(gameSession.CompleteRespawn("Zone_Greenwood"), (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(gameSession.CompleteRespawn("HomeBase"), (IResolveConstraint)(object)Is.True);
			Assert.That<PlayerLifeState>(gameSession.LifeState, (IResolveConstraint)(object)Is.EqualTo((object)PlayerLifeState.Alive));
			Assert.That<string>(gameSession.LastSafeNodeId, (IResolveConstraint)(object)Is.EqualTo((object)"home_base"));
			Assert.That<string>(gameSession.LastSafeSceneKey, (IResolveConstraint)(object)Is.EqualTo((object)"HomeBase"));
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void Respawn_DoesNotChangeInventory()
	{
		GameObject root = new GameObject("Session_InventoryDeath_Test");
		try
		{
			GameSession gameSession = root.AddComponent<GameSession>();
			gameSession.StartNewGame();
			PlayerInventory sameInventory = gameSession.PlayerInventory;
			int potionsBefore = sameInventory.GetTotalAmount("healing_potion");
			int foodBefore = sameInventory.GetTotalAmount("buff_food");
			gameSession.RecordPlayerDeath();
			gameSession.BeginRespawn("home_base", "HomeBase");
			gameSession.CompleteRespawn("HomeBase");
			Assert.That<PlayerInventory>(gameSession.PlayerInventory, (IResolveConstraint)(object)Is.SameAs((object)sameInventory));
			Assert.That<int>(sameInventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)potionsBefore));
			Assert.That<int>(sameInventory.GetTotalAmount("buff_food"), (IResolveConstraint)(object)Is.EqualTo((object)foodBefore));
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void DeathWindowPrefab_HasSafeAreaAndBothActions()
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/UI/PlayerDeathWindow.prefab");
		Assert.That<GameObject>(prefab, (IResolveConstraint)(object)Is.Not.Null);
		bool hasSafeArea = false;
		MonoBehaviour[] componentsInChildren = prefab.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
		foreach (MonoBehaviour component in componentsInChildren)
		{
			if (component != null && component.GetType().FullName == "Eidren.UI.SafeAreaPanel")
			{
				hasSafeArea = true;
				break;
			}
		}
		Assert.That<bool>(hasSafeArea, (IResolveConstraint)(object)Is.True);
		Assert.That<Transform>(prefab.transform.Find("SafeArea/DeathPanel/DefeatCard/ReviveButton"), (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Transform>(prefab.transform.Find("SafeArea/DeathPanel/DefeatCard/MainMenuButton"), (IResolveConstraint)(object)Is.Not.Null);
	}

	private static DamageInfo Lethal(GameObject source)
	{
		return new DamageInfo(1000f, 0f, source.transform.position, source, isBackAttack: false, "test.player.lethal", "test");
	}
}
}
