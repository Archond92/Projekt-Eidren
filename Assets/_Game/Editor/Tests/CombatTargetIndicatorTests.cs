using System.Reflection;
using Eidren.AI;
using Eidren.Combat;
using Eidren.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class CombatTargetIndicatorTests
	{
		private const string PlayerPath = "Assets/_Game/Prefabs/Player/Player.prefab";

		[Test] public void StylesEscalateWithoutAddingHudInformation()
		{
			CombatTargetRingStyle normal = CombatTargetRingStyles.Resolve(CombatTargetCategory.Normal);
			CombatTargetRingStyle elite = CombatTargetRingStyles.Resolve(CombatTargetCategory.Elite);
			CombatTargetRingStyle boss = CombatTargetRingStyles.Resolve(CombatTargetCategory.Boss);
			Assert.That(normal.HasAccent, Is.False);
			Assert.That(elite.HasAccent, Is.True);
			Assert.That(boss.HasAccent, Is.True);
			Assert.That(elite.Width, Is.GreaterThan(normal.Width));
			Assert.That(boss.Width, Is.GreaterThan(elite.Width));
			Assert.That(boss.RadiusFactor, Is.GreaterThan(elite.RadiusFactor));
			Assert.That(boss.MaximumRadius, Is.GreaterThan(elite.MaximumRadius));
		}

		[Test] public void PlayerPrefabOwnsExactlyOneReusableCombatRing()
		{
			GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
			Assert.That(player, Is.Not.Null);
			CombatTargetIndicator[] indicators = player.GetComponentsInChildren<CombatTargetIndicator>(true);
			Assert.That(indicators, Has.Length.EqualTo(1));
			CombatTargetIndicator indicator = indicators[0];
			Assert.That(indicator.name, Is.EqualTo("CombatTargetRing"));
			Assert.That(indicator.PrimaryRing, Is.Not.Null);
			Assert.That(indicator.AccentRing, Is.Not.Null);
			Assert.That(indicator.PrimaryRing.enabled, Is.False);
			Assert.That(indicator.AccentRing.enabled, Is.False);
			Assert.That(indicator.PrimaryRing.loop, Is.True);
			Assert.That(indicator.PrimaryRing.useWorldSpace, Is.False);
			Assert.That(indicator.PrimaryRing.positionCount, Is.EqualTo(64));
			Assert.That(indicator.AccentRing.positionCount, Is.EqualTo(16));
			Assert.That(indicator.PrimaryRing.sharedMaterial, Is.SameAs(indicator.AccentRing.sharedMaterial));
			Assert.That(indicator.PrimaryRing.sharedMaterial.shader.name, Is.EqualTo("Eidren/CombatTargetRing"));
		}

		[Test] public void RingShaderStaysLegibleThroughVegetationWithoutWritingDepth()
		{
			Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Game/Shaders/CombatTargetRing.shader");
			Assert.That(shader, Is.Not.Null);
			string source = System.IO.File.ReadAllText("Assets/_Game/Shaders/CombatTargetRing.shader");
			Assert.That(source, Does.Contain("ZTest Always"));
			Assert.That(source, Does.Contain("ZWrite Off"));
			Assert.That(source, Does.Not.Contain("_MainTex"));
		}

		[Test] public void RuntimeCodeNeverCreatesOrClonesAMaterial()
		{
			string source = System.IO.File.ReadAllText("Assets/_Game/Scripts/Combat/CombatTargetIndicator.cs");
			Assert.That(source, Does.Not.Contain("new Material"));
			Assert.That(source, Does.Not.Contain(".material"));
			Assert.That(source, Does.Contain("startColor"));
		}

		[Test] public void AreaEliteDefinitionProducesEliteSnapshotAndAccentStyle()
		{
			EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/RiftGuardian.asset");
			Assert.That(definition, Is.Not.Null);
			Assert.That(definition.CombatStyle, Is.EqualTo(EnemyCombatStyle.AreaElite));
			GameObject target = new GameObject("HUD100_EliteSnapshot");
			try
			{
				Damageable health = target.AddComponent<Damageable>();
				health.Initialize(100f);
				WildlingController elite = target.AddComponent<WildlingController>();
				typeof(WildlingController).GetField("definition", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(elite, definition);
				typeof(EnemyControllerBase).GetField("_health", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(elite, health);
				CombatTargetSnapshot snapshot = new CombatTargetSnapshot(elite, Vector3.zero, 0f, 0f);
				Assert.That(snapshot.Category, Is.EqualTo(CombatTargetCategory.Elite));
				Assert.That(CombatTargetRingStyles.Resolve(snapshot.Category).HasAccent, Is.True);
			}
			finally { Object.DestroyImmediate(target); }
		}
	}
}
