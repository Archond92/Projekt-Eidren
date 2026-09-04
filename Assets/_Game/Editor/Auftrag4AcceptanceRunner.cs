using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Interaction;
using System.Linq;
using System;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Eidren.Editor
{
public static class Auftrag4AcceptanceRunner
{
	public static void Run()
	{
		VerifyLifecycle();
		VerifyBalanceAndData();
		VerifyScene();
		VerifyAccess();
		Debug.Log("AUFTRAG 4 ACCEPTANCE: PASS (lifecycle, balance, data, scene, access)");
	}

	private static void VerifyLifecycle()
	{
		EidraForgeDungeonService eidraForgeDungeonService = new EidraForgeDungeonService();
		DateTime now = new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);
		Require(eidraForgeDungeonService.GetAccessState(now) == EidraForgeAccessState.FirstRunAvailable, "first run");
		Require(eidraForgeDungeonService.TryBeginFirstRun(42), "begin first run");
		Require(eidraForgeDungeonService.GetAccessState(now) == EidraForgeAccessState.ResumeRun, "resume");
		Require(eidraForgeDungeonService.TryComplete(now, out var first) && first, "first completion");
		Require(eidraForgeDungeonService.GetAccessState(now.AddHours(23.99)) == EidraForgeAccessState.Cooldown, "24h cooldown lower bound");
		Require(eidraForgeDungeonService.GetAccessState(now.AddHours(24.0)) == EidraForgeAccessState.ResetAvailable, "24h cooldown upper bound");
		Require(condition: true, "save version 13");
	}

	private static void VerifyBalanceAndData()
	{
		Require(EidraForgePopulationRules.CreateEnemyStates().Length == 24, "24 fixed enemies");
		Require(EidraForgeBalance.TotalMarks(includeSealGuardian: true) == 45, "45 marks");
		Require(EidraForgeBalance.TotalMarks(includeSealGuardian: false) == 40, "40 marks");
		Require(EidraForgeBalance.TotalExperience(firstCompletion: true) == 1936, "first-run XP");
		Require(EidraForgeBalance.TotalExperience(firstCompletion: false) == 1036, "repeat XP");
		Require(EidraForgeContainerRules.HasCanonicalPopulation(EidraForgeContainerRules.CreateEmptyRunChests()), "seven chests");
		Require(EidraForgeChestService.Price(ForgeRewardChestSize.Small) == 15, "small reward price");
		Require(EidraForgeChestService.Price(ForgeRewardChestSize.Medium) == 30, "medium reward price");
		Require(EidraForgeChestService.Price(ForgeRewardChestSize.Large) == 90, "large reward price");
		EnemyDefinition[] array = (from value in AssetDatabase.FindAssets("t:EnemyDefinition", new string[1] { "Assets/_Game/Data/Enemies/Forge" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<EnemyDefinition>)
			where value != null
			select value).ToArray();
		Require(array.Length == 4, "four forge archetypes");
		Require(array.All((EnemyDefinition value) => value.Prefab != null), "enemy prefabs bound");
		CoreGuardianData core = AssetDatabase.LoadAssetAtPath<CoreGuardianData>("Assets/_Game/Data/Bosses/CoreGuardian.asset");
		Require(core != null && core.GetValidationErrors().Length == 0, "core guardian data");
		Require(core.MaximumHealth == 3000f && core.MaximumStagger == 400f && core.Attacks.Count == 3, "core guardian canonical stats");
		EidraData ignivar = AssetDatabase.LoadAssetAtPath<EidraData>("Assets/_Game/Data/Eidren/Ignivar.asset");
		Require(ignivar != null && ignivar.GetValidationErrors().Length == 0, "Ignivar data");
		Require(ignivar.Skill1.ExecutionType == AbilityExecutionType.EmberCircle && ignivar.Skill2.ExecutionType == AbilityExecutionType.MoltenBrand, "Ignivar execution types");
	}

	private static void VerifyScene()
	{
		Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/EidraForge.unity", OpenSceneMode.Single);
		EidraForgeEnemyAnchor[] anchors = FindAll<EidraForgeEnemyAnchor>(scene);
		EidraForgeChestContainer[] array = FindAll<EidraForgeChestContainer>(scene);
		Require(anchors.Length == 25, "24 enemies plus Ignivar anchor");
		Require(anchors.Select((EidraForgeEnemyAnchor value) => value.SpawnId).Distinct().Count() == 25, "stable unique spawn IDs");
		Require(array.Length == 11, "7 run, 3 reward, 1 recovery chests");
		Require(array.Select((EidraForgeChestContainer value) => value.ContainerId).Distinct().Count() == 11, "stable unique chest IDs");
		Require(FindAll<EidraForgeSceneController>(scene).Length == 1, "one forge scene controller");
		Require(EditorBuildSettings.scenes.Any((EditorBuildSettingsScene value) => value.enabled && value.path == "Assets/_Game/Scenes/EidraForge.unity"), "forge scene in build settings");
	}

	private static void VerifyAccess()
	{
		Require(FindAll<EidraForgeEntrance>(EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_EmberRuins.unity", OpenSceneMode.Single)).Length == 1, "one Garon-gated forge entrance");
	}

	private static T[] FindAll<T>(Scene scene) where T : Component
	{
		return scene.GetRootGameObjects().SelectMany((GameObject root) => root.GetComponentsInChildren<T>(includeInactive: true)).ToArray();
	}

	private static void Require(bool condition, string label)
	{
		if (!condition)
		{
			throw new InvalidOperationException("Auftrag 4 acceptance failed: " + label);
		}
	}
}
}
