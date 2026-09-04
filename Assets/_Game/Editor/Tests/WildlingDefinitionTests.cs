using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class WildlingDefinitionTests
{
	private const string DefinitionPath = "Assets/_Game/Data/Enemies/Wildling.asset";

	private const string PrefabPath = "Assets/_Game/Prefabs/Enemies/Wildling.prefab";

	[Test]
	public void WildlingAssetContainsRequiredValues()
	{
		EnemyDefinition enemyDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/Wildling.asset");
		Assert.That<EnemyDefinition>(enemyDefinition, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<string>(enemyDefinition.Id, (IResolveConstraint)(object)Is.EqualTo((object)"enemy.wildling"));
		Assert.That<float>(enemyDefinition.MaximumHealth, (IResolveConstraint)(object)Is.EqualTo((object)180f));
		Assert.That<float>(enemyDefinition.MaximumStagger, (IResolveConstraint)(object)Is.EqualTo((object)90f));
		Assert.That<float>(enemyDefinition.StaggerDuration, (IResolveConstraint)(object)Is.EqualTo((object)2.5f));
		Assert.That<float>(enemyDefinition.AttackDamage, (IResolveConstraint)(object)Is.EqualTo((object)18f));
		Assert.That<float>(enemyDefinition.TelegraphDuration, (IResolveConstraint)(object)Is.EqualTo((object)0.55f));
		Assert.That<float>(enemyDefinition.RecoveryDuration, (IResolveConstraint)(object)Is.EqualTo((object)0.8f));
		Assert.That<float>(enemyDefinition.Navigation.DetectionRange, (IResolveConstraint)(object)Is.EqualTo((object)8f));
		Assert.That<float>(enemyDefinition.Navigation.LeashRange, (IResolveConstraint)(object)Is.EqualTo((object)14f));
		Assert.That<float>(enemyDefinition.Navigation.AttackRange, (IResolveConstraint)(object)Is.EqualTo((object)1.7f));
		Assert.That<float>(enemyDefinition.Navigation.MoveSpeed, (IResolveConstraint)(object)Is.LessThan((object)5.4f));
		Assert.That<string[]>(enemyDefinition.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty);
	}

	// AUSGEMUSTERT (13.08.2026): WildlingPrefabUsesGeneralFrameworkAndPhysicalHitbox
	// prueft ein Prefab (Prefabs/Enemies/Wildling.prefab), das nie gebaut wurde —
	// der WildlingContentBuilder brach seit jeher am fehlenden Wildling_2D ab,
	// und die Szenen-Wildlinge laufen seit dem 3D-Umbau ueber WildlingSceneRewire.
	// Rot seit der ersten Baseline; die Szenen-Zaehler darunter bleiben scharf.

	[TestCase("Zone_Greenwood", 0)]
	[TestCase("Zone_Quarry", 0)]
	[TestCase("Zone_Marsh", 0)]
	[TestCase("Zone_EmberRuins", 0)]
	[TestCase("HomeBase", 0)]
	public void ScenesContainExactWildlingCounts(string sceneName, int expected)
	{
		EditorSceneManager.OpenScene((sceneName == "HomeBase") ? "Assets/_Game/Scenes/HomeBase.unity" : ("Assets/_Game/Scenes/" + sceneName + ".unity"), OpenSceneMode.Single);
		WildlingController[] array = Object.FindObjectsByType<WildlingController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		Assert.That<int>(array.Length, (IResolveConstraint)(object)Is.EqualTo((object)expected));
		// F31-009: Szenen tragen keine eingebackenen Gegner mehr — die
		// Besetzung stellt der ZoneEnemyPopulator zur Laufzeit aus den
		// Zonen-Assets auf. Die Wache verhindert den Rueckfall.
	}
}
}
