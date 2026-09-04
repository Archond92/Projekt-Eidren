using Eidren.AI;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class GaronEmberRuinsContentTests
{
	[Test]
	public void EmberRuins_HasConfiguredOpenBossAreaAndGaronPrefab()
	{
		Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_EmberRuins.unity", OpenSceneMode.Additive);
		try
		{
			BossAreaController area = FindInScene<BossAreaController>(scene);
			ZoneController zoneController = FindInScene<ZoneController>(scene);
			Assert.That<BossAreaController>(area, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<ZoneController>(zoneController, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<string>(area.StableBossAreaId, (IResolveConstraint)(object)Is.EqualTo((object)"boss_area.ember_ruins.garon"));
			Assert.That<Transform>(area.HomePoint, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<Transform>(area.BossSpawnPoint, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<float>(area.TerritoryRadius, (IResolveConstraint)(object)Is.EqualTo((object)18f));
			Assert.That<float>(area.ActivationRadius, (IResolveConstraint)(object)Is.EqualTo((object)11f));
			Assert.That<float>(area.LeashBoundary, (IResolveConstraint)(object)Is.EqualTo((object)18f));
			Assert.That<Collider>(area.CameraFramingZone, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<string[]>(area.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty);
			Assert.That<NavMeshData>(zoneController.NavigationSurface.navMeshData, (IResolveConstraint)(object)Is.Not.Null);
			Bounds bounds = zoneController.WalkableGround.bounds;
			float rightClearance = bounds.max.x - area.HomePoint.position.x - 0.5f;
			float leftClearance = area.HomePoint.position.x - bounds.min.x - 0.5f;
			float bypassDistance = Mathf.Max(rightClearance, leftClearance);
			Vector3 bypassPoint = area.HomePoint.position + ((rightClearance >= leftClearance) ? Vector3.right : Vector3.left) * bypassDistance;
			bypassPoint.y = bounds.center.y;
			Assert.That<bool>(bounds.Contains(bypassPoint), (IResolveConstraint)(object)Is.True);
			Assert.That<float>(bypassDistance, (IResolveConstraint)(object)Is.GreaterThan((object)area.ActivationRadius));
		}
		finally
		{
			EditorSceneManager.CloseScene(scene, removeScene: true);
		}
	}

	[Test]
	public void Garon_UsesRequiredExistingDataValues()
	{
		BossData bossData = AssetDatabase.LoadAssetAtPath<BossData>("Assets/_Game/Data/Bosses/Garon.asset");
		BossController prefab = AssetDatabase.LoadAssetAtPath<BossController>("Assets/_Game/Prefabs/Bosses/Garon.prefab");
		Assert.That<BossData>(bossData, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<BossController>(prefab, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<float>(bossData.MaxHealth, (IResolveConstraint)(object)Is.EqualTo((object)2100f));
		Assert.That<float>(bossData.MaxStagger, (IResolveConstraint)(object)Is.EqualTo((object)340f));
		Assert.That<float>(bossData.StaggerDuration, (IResolveConstraint)(object)Is.EqualTo((object)6f));
		Assert.That<float>(bossData.PostStaggerResistanceDuration, (IResolveConstraint)(object)Is.EqualTo((object)4f));
		Assert.That<float>(bossData.Attacks.Front.Damage, (IResolveConstraint)(object)Is.EqualTo((object)34f));
		Assert.That<float>(bossData.Attacks.Charge.Damage, (IResolveConstraint)(object)Is.EqualTo((object)31f));
		Assert.That<float>(bossData.Attacks.Spin.Damage, (IResolveConstraint)(object)Is.EqualTo((object)25f));
		Assert.That<NavMeshAgent>(prefab.GetComponent<NavMeshAgent>(), (IResolveConstraint)(object)Is.Not.Null);
	}

	[Test]
	public void SessionProgress_IsRuntimeOnlyAndResetsWithNewGame()
	{
		GameObject root = new GameObject("GaronProgress_Test");
		try
		{
			GameSession gameSession = root.AddComponent<GameSession>();
			gameSession.Initialize();
			Assert.That<bool>(gameSession.TrySetProgressFlag("garon_defeated"), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(gameSession.TrySetProgressFlag("garon_defeated"), (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(gameSession.MarkWorldMapNodeCompleted("zone_ember_ruins"), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(gameSession.IsWorldMapNodeCompleted("zone_ember_ruins"), (IResolveConstraint)(object)Is.True);
			gameSession.StartNewGame();
			Assert.That<bool>(gameSession.HasProgressFlag("garon_defeated"), (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(gameSession.IsWorldMapNodeCompleted("zone_ember_ruins"), (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void FullInventory_RejectsWholeBossRewardAtomically()
	{
		GameObject root = new GameObject("GaronRewardInventory_Test");
		try
		{
			ContentDatabase database = root.AddComponent<ContentDatabase>();
			PlayerInventory inventory = new PlayerInventory(database);
			ItemStack[] full = (from _ in Enumerable.Range(0, 16)
				select new ItemStack("stone", database.GetItem("stone").MaximumStackSize)).ToArray();
			inventory.Reset(full);
			ItemStack[] before = inventory.ExportSlots();
			InventoryItemAmount[] reward = new InventoryItemAmount[2]
			{
				new InventoryItemAmount("copper_bar", 3),
				new InventoryItemAmount("healing_potion", 2)
			};
			Assert.That<bool>(inventory.CanAddBatch(reward), (IResolveConstraint)(object)Is.False);
			Assert.That<bool>(inventory.TryAddBatch(reward), (IResolveConstraint)(object)Is.False);
			Assert.That<IEnumerable<(string, int)>>(from stack in inventory.ExportSlots()
				select (ItemId: stack.ItemId, Quantity: stack.Quantity), (IResolveConstraint)(object)Is.EqualTo((object)before.Select((ItemStack stack) => (ItemId: stack.ItemId, Quantity: stack.Quantity))));
		}
		finally
		{
			Object.DestroyImmediate(root);
		}
	}

	private static T FindInScene<T>(Scene scene) where T : Component
	{
		GameObject[] rootGameObjects = scene.GetRootGameObjects();
		for (int i = 0; i < rootGameObjects.Length; i++)
		{
			T result = rootGameObjects[i].GetComponentInChildren<T>(includeInactive: true);
			if (result != null)
			{
				return result;
			}
		}
		return null;
	}
}
}
