using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class ContentAssetTests
{
	private static readonly string[] ExpectedIds = new string[21]
	{
		"hammer", "daggers", "copper_hammer", "iron_hammer", "sealbreaker", "copper_daggers", "iron_daggers", "ash_fangs", "copper_spear", "iron_spear",
		"ember_thorn", "terrock", "noctarion", "ignivar", "felsbrecher", "steinhaut", "schattenschritt", "rueckenmal", "glutkreis", "schmelzbrand",
		"garon"
	};

	[Test]
	public void V02Content_HasTwentyOneUniqueStableIds()
	{
		List<string> ids = (from asset in AssetDatabase.FindAssets("t:ScriptableObject", new string[1] { "Assets/_Game/Data" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
			where asset is WeaponData || asset is EidraData || asset is AbilityData || asset is BossData
			select asset).Select(GetId).ToList();
		Assert.That<List<string>>(ids, (IResolveConstraint)(object)((ConstraintExpression)Has.Count).EqualTo((object)21));
		Assert.That<List<string>>(ids, (IResolveConstraint)(object)Is.All.Not.Empty);
		Assert.That<int>(ids.Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)ids.Count));
		Assert.That<List<string>>(ids, (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)ExpectedIds));
	}

	[Test]
	public void MigratedPrototypeContent_PreservesCurrentPrototypeValues()
	{
		WeaponData weaponData = Load<WeaponData>("Weapons/Hammer.asset");
		Assert.That<string>(weaponData.DisplayName, (IResolveConstraint)(object)Is.EqualTo((object)"Hammer"));
		Assert.That<Color>(weaponData.Accent, (IResolveConstraint)(object)Is.EqualTo((object)new Color(0.95f, 0.62f, 0.15f)));
		Assert.That<float>(weaponData.BaseDamage, (IResolveConstraint)(object)Is.EqualTo((object)15f));
		Assert.That<float>(weaponData.BaseStaggerDamage, (IResolveConstraint)(object)Is.EqualTo((object)10f));
		Assert.That<float>(weaponData.AttackRange, (IResolveConstraint)(object)Is.EqualTo((object)3.4f));
		Assert.That<float>(weaponData.AttackAssistAngle, (IResolveConstraint)(object)Is.EqualTo((object)120f));
		Assert.That<float>(weaponData.BackDamageMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)1f));
		Assert.That<AttackStepData[]>(weaponData.Combo, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)3));
		AssertStep(weaponData.Combo[0], 0.62f, 0.31f, 0.13f, 3.4f, 120f, 1f, 1f, 0.25f, 0.64f);
		AssertStep(weaponData.Combo[1], 0.68f, 0.34f, 0.15f, 3.4f, 120f, 1.08f, 1.18f, 0.3f, 0.62f);
		AssertStep(weaponData.Combo[2], 0.88f, 0.48f, 0.18f, 3.4f, 120f, 1.45f, 1.8f, 0.45f, 0.66f);
		WeaponData weaponData2 = Load<WeaponData>("Weapons/Daggers.asset");
		Assert.That<string>(weaponData2.DisplayName, (IResolveConstraint)(object)Is.EqualTo((object)"Dolche"));
		Assert.That<Color>(weaponData2.Accent, (IResolveConstraint)(object)Is.EqualTo((object)new Color(0.22f, 0.78f, 0.92f)));
		Assert.That<float>(weaponData2.BaseDamage, (IResolveConstraint)(object)Is.EqualTo((object)12f));
		Assert.That<float>(weaponData2.BaseStaggerDamage, (IResolveConstraint)(object)Is.EqualTo((object)2.5f));
		Assert.That<float>(weaponData2.AttackRange, (IResolveConstraint)(object)Is.EqualTo((object)3f));
		Assert.That<float>(weaponData2.AttackAssistAngle, (IResolveConstraint)(object)Is.EqualTo((object)75f));
		Assert.That<float>(weaponData2.BackDamageMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)1.75f));
		Assert.That<AttackStepData[]>(weaponData2.Combo, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)4));
		AssertStep(weaponData2.Combo[0], 0.34f, 0.16f, 0.08f, 3f, 75f, 0.85f, 0.5f, 0.3f, 0.52f);
		AssertStep(weaponData2.Combo[1], 0.31f, 0.14f, 0.08f, 3f, 75f, 0.9f, 0.5f, 0.35f, 0.5f);
		AssertStep(weaponData2.Combo[2], 0.36f, 0.17f, 0.09f, 3f, 75f, 1f, 0.6f, 0.4f, 0.5f);
		AssertStep(weaponData2.Combo[3], 0.48f, 0.23f, 0.11f, 3f, 75f, 1.35f, 0.7f, 0.5f, 0.56f);
		AbilityData felsbrecher = Load<AbilityData>("Abilities/Felsbrecher.asset");
		AssertAbility(felsbrecher, "Felsbrecher", 7f, 8f, AbilityExecutionType.StaggerStrike, 0.42f, 0f, 62f, 1f, 0f);
		AbilityData steinhaut = Load<AbilityData>("Abilities/Steinhaut.asset");
		AssertAbility(steinhaut, "Steinhaut", 11f, 0f, AbilityExecutionType.Shield, 0.32f, 2.1f, 0f, 1f, 0f);
		AbilityData schattenschritt = Load<AbilityData>("Abilities/Schattenschritt.asset");
		AssertAbility(schattenschritt, "Schattenschritt", 6f, 12f, AbilityExecutionType.ShadowStep, 0f, 0f, 0f, 1f, 2.8f);
		AbilityData rueckenmal = Load<AbilityData>("Abilities/Rueckenmal.asset");
		AssertAbility(rueckenmal, "Rückenmal", 9f, 12f, AbilityExecutionType.BackMark, 0f, 6f, 0f, 1.45f, 0f);
		EidraData eidraData = Load<EidraData>("Eidren/Terrock.asset");
		Assert.That<string>(eidraData.DisplayName, (IResolveConstraint)(object)Is.EqualTo((object)"Terrock"));
		Assert.That<Color>(eidraData.UiAccent, (IResolveConstraint)(object)Is.EqualTo((object)new Color(0.77f, 0.55f, 0.2f)));
		Assert.That<EidraRole>(eidraData.Role, (IResolveConstraint)(object)Is.EqualTo((object)EidraRole.Defend));
		Assert.That<float>(eidraData.PassiveStaggerMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)1.22f));
		Assert.That<float>(eidraData.PassiveBackDamageMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)1f));
		Assert.That<AbilityData>(eidraData.Skill1, (IResolveConstraint)(object)Is.SameAs((object)felsbrecher));
		Assert.That<AbilityData>(eidraData.Skill2, (IResolveConstraint)(object)Is.SameAs((object)steinhaut));
		EidraData eidraData2 = Load<EidraData>("Eidren/Noctarion.asset");
		Assert.That<string>(eidraData2.DisplayName, (IResolveConstraint)(object)Is.EqualTo((object)"Noctarion"));
		Assert.That<Color>(eidraData2.UiAccent, (IResolveConstraint)(object)Is.EqualTo((object)new Color(0.58f, 0.27f, 0.83f)));
		Assert.That<EidraRole>(eidraData2.Role, (IResolveConstraint)(object)Is.EqualTo((object)EidraRole.Attack));
		Assert.That<float>(eidraData2.PassiveStaggerMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)0.92f));
		Assert.That<float>(eidraData2.PassiveBackDamageMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)1.18f));
		Assert.That<AbilityData>(eidraData2.Skill1, (IResolveConstraint)(object)Is.SameAs((object)schattenschritt));
		Assert.That<AbilityData>(eidraData2.Skill2, (IResolveConstraint)(object)Is.SameAs((object)rueckenmal));
		BossData bossData = Load<BossData>("Bosses/Garon.asset");
		Assert.That<string>(bossData.DisplayName, (IResolveConstraint)(object)Is.EqualTo((object)"Garon"));
		Assert.That<float>(bossData.MaxHealth, (IResolveConstraint)(object)Is.EqualTo((object)2100f));
		Assert.That<float>(bossData.MaxStagger, (IResolveConstraint)(object)Is.EqualTo((object)340f));
		Assert.That<float>(bossData.StaggerDuration, (IResolveConstraint)(object)Is.EqualTo((object)6f));
		Assert.That<float>(bossData.PostStaggerResistanceDuration, (IResolveConstraint)(object)Is.EqualTo((object)4f));
		Assert.That<float>(bossData.PostStaggerResistanceMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)0.38f));
		Assert.That<float>(bossData.Navigation.MoveSpeed, (IResolveConstraint)(object)Is.EqualTo((object)2.1f));
		Assert.That<float>(bossData.Navigation.Acceleration, (IResolveConstraint)(object)Is.EqualTo((object)12f));
		Assert.That<float>(bossData.Navigation.AngularSpeed, (IResolveConstraint)(object)Is.EqualTo((object)300f));
		Assert.That<float>(bossData.Navigation.AttackRange, (IResolveConstraint)(object)Is.EqualTo((object)9f));
		Assert.That<float>(bossData.Navigation.DetectionRange, (IResolveConstraint)(object)Is.EqualTo((object)12.5f));
		Assert.That<float>(bossData.Navigation.LeashRange, (IResolveConstraint)(object)Is.EqualTo((object)18f));
		Assert.That<float>(bossData.Navigation.LeashFollowDistance, (IResolveConstraint)(object)Is.EqualTo((object)2.5f));
		Assert.That<float>(bossData.Navigation.PatrolRadius, (IResolveConstraint)(object)Is.EqualTo((object)3f));
		Assert.That<float>(bossData.Navigation.PatrolWait, (IResolveConstraint)(object)Is.EqualTo((object)2f));
		Assert.That<float>(bossData.Navigation.AlertDuration, (IResolveConstraint)(object)Is.EqualTo((object)0.25f));
		Assert.That<float>(bossData.Navigation.ReturnTolerance, (IResolveConstraint)(object)Is.EqualTo((object)0.65f));
		Assert.That<float>(bossData.Navigation.NavMeshSampleDistance, (IResolveConstraint)(object)Is.EqualTo((object)4f));
		Assert.That<float>(bossData.Navigation.AgentRadius, (IResolveConstraint)(object)Is.EqualTo((object)1.25f));
		Assert.That<float>(bossData.Navigation.AgentHeight, (IResolveConstraint)(object)Is.EqualTo((object)3.1f));
		Assert.That<float>(bossData.Attacks.Front.Damage, (IResolveConstraint)(object)Is.EqualTo((object)34f));
		Assert.That<float>(bossData.Attacks.Front.TelegraphDuration, (IResolveConstraint)(object)Is.EqualTo((object)0.9f));
		Assert.That<float>(bossData.Attacks.Charge.Damage, (IResolveConstraint)(object)Is.EqualTo((object)31f));
		Assert.That<float>(bossData.Attacks.Charge.MoveSpeed, (IResolveConstraint)(object)Is.EqualTo((object)9f));
		Assert.That<float>(bossData.Attacks.Charge.TelegraphDuration, (IResolveConstraint)(object)Is.EqualTo((object)1.15f));
		Assert.That<float>(bossData.Attacks.Spin.Damage, (IResolveConstraint)(object)Is.EqualTo((object)25f));
		Assert.That<float>(bossData.Attacks.Spin.TelegraphDuration, (IResolveConstraint)(object)Is.EqualTo((object)0.9f));
	}

	[Test]
	public void ItemAssets_CarryTheV01MaterialFieldsAndValidate()
	{
		ItemDefinition[] array = (from itemDefinition in AssetDatabase.FindAssets("t:ItemDefinition", new string[1] { "Assets/_Game/Data/Items" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ItemDefinition>)
			where itemDefinition != null
			select itemDefinition).ToArray();
		Assert.That<ItemDefinition[]>(array, (IResolveConstraint)(object)Is.Not.Empty);
		ItemDefinition[] array2 = array;
		foreach (ItemDefinition item in array2)
		{
			string[] errors = item.ValidateDefinition().Errors;
			Assert.That<string[]>(errors, (IResolveConstraint)(object)Is.Empty, item.name + ": " + string.Join("; ", errors), Array.Empty<object>());
			Assert.That<int>(item.Tier, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)0), item.name, Array.Empty<object>());
			Assert.That<Sprite>(item.Icon, (IResolveConstraint)(object)Is.Not.Null, item.name + " must have an authored icon.", Array.Empty<object>());
			if (item.Category == ItemCategory.Material)
			{
				Assert.That<string>(item.MaterialFamily, (IResolveConstraint)(object)Is.Not.Empty, item.name, Array.Empty<object>());
			}
			if (!string.IsNullOrEmpty(item.MaterialFamily))
			{
				Assert.That<bool>(MaterialFamilies.IsKnown(item.MaterialFamily), (IResolveConstraint)(object)Is.True, item.name + ": '" + item.MaterialFamily + "'", Array.Empty<object>());
			}
			bool durable = item.Category == ItemCategory.Tool || item.Category == ItemCategory.Weapon || item.Category == ItemCategory.Armor;
			if (item.Id == "catch_device")
			{
				durable = false;
			}
			int maximumDurability = item.MaximumDurability;
			IResolveConstraint obj;
			if (!durable)
			{
				IResolveConstraint zero = (IResolveConstraint)(object)Is.Zero;
				obj = zero;
			}
			else
			{
				IResolveConstraint zero = (IResolveConstraint)(object)Is.GreaterThan((object)0);
				obj = zero;
			}
			Assert.That<int>(maximumDurability, obj, item.name, Array.Empty<object>());
		}
	}

	[Test]
	public void BuildingCosts_CoverTheM89TableAndValidate()
	{
		BuildingCostDefinition[] buildings = (from buildingCostDefinition3 in AssetDatabase.FindAssets("t:BuildingCostDefinition", new string[1] { "Assets/_Game/Data/Buildings" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>)
			where buildingCostDefinition3 != null
			select buildingCostDefinition3).ToArray();
		Assert.That<BuildingCostDefinition[]>(buildings, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)11));
		Assert.That<int>(buildings.Select((BuildingCostDefinition buildingCostDefinition3) => buildingCostDefinition3.Id).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)buildings.Length));
		BuildingCostDefinition[] array = buildings;
		foreach (BuildingCostDefinition building in array)
		{
			Assert.That<bool>(building.TryValidate(out var error), (IResolveConstraint)(object)Is.True, building.name + ": " + error, Array.Empty<object>());
			Assert.That<int>(building.Cost.Count, (IResolveConstraint)(object)Is.LessThanOrEqualTo((object)3), building.name, Array.Empty<object>());
			Assert.That<IReadOnlyList<BuildingFootprint>>(building.LevelFootprints, (IResolveConstraint)(object)Is.Not.Empty, building.name, Array.Empty<object>());
		}
		BuildingCostDefinition buildingCostDefinition = buildings.Single((BuildingCostDefinition buildingCostDefinition3) => buildingCostDefinition3.Id == "building.farm_plot");
		Assert.That<int>(buildingCostDefinition.LevelFootprints[0].Width, (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(buildingCostDefinition.LevelFootprints[0].Depth, (IResolveConstraint)(object)Is.EqualTo((object)2));
		BuildingCostDefinition buildingCostDefinition2 = buildings.Single((BuildingCostDefinition buildingCostDefinition3) => buildingCostDefinition3.Id == "building.smelter");
		Assert.That<int>(buildingCostDefinition2.Tier, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(buildingCostDefinition2.Cost.All((CraftingIngredient ingredient) => ingredient.ItemId != "copper_bar"), (IResolveConstraint)(object)Is.True);
	}

	private static string GetId(ScriptableObject asset)
	{
		if (!(asset is WeaponData { Id: var id }))
		{
			if (!(asset is EidraData { Id: var id2 }))
			{
				if (!(asset is AbilityData { Id: var id3 }))
				{
					if (!(asset is BossData { Id: var id4 }))
					{
						return null;
					}
					return id4;
				}
				return id3;
			}
			return id2;
		}
		return id;
	}

	private static T Load<T>(string relativePath) where T : ScriptableObject
	{
		T val = AssetDatabase.LoadAssetAtPath<T>("Assets/_Game/Data/" + relativePath);
		Assert.That<T>(val, (IResolveConstraint)(object)Is.Not.Null, "Missing content asset: " + relativePath, Array.Empty<object>());
		return val;
	}

	private static void AssertAbility(AbilityData ability, string displayName, float cooldown, float range, AbilityExecutionType executionType, float castDuration, float effectDuration, float staggerAmount, float backDamageMultiplier, float teleportBehindDistance)
	{
		Assert.That<string>(ability.DisplayName, (IResolveConstraint)(object)Is.EqualTo((object)displayName));
		Assert.That<float>(ability.Cooldown, (IResolveConstraint)(object)Is.EqualTo((object)cooldown));
		Assert.That<float>(ability.Range, (IResolveConstraint)(object)Is.EqualTo((object)range));
		Assert.That<AbilityExecutionType>(ability.ExecutionType, (IResolveConstraint)(object)Is.EqualTo((object)executionType));
		Assert.That<float>(ability.CastDuration, (IResolveConstraint)(object)Is.EqualTo((object)castDuration));
		Assert.That<float>(ability.EffectDuration, (IResolveConstraint)(object)Is.EqualTo((object)effectDuration));
		Assert.That<float>(ability.StaggerAmount, (IResolveConstraint)(object)Is.EqualTo((object)staggerAmount));
		Assert.That<float>(ability.BackDamageMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)backDamageMultiplier));
		Assert.That<float>(ability.TeleportBehindDistance, (IResolveConstraint)(object)Is.EqualTo((object)teleportBehindDistance));
	}

	private static void AssertStep(AttackStepData step, float duration, float hitTime, float hitWindowDuration, float hitboxRange, float hitboxAngle, float damage, float stagger, float motion, float queue)
	{
		Assert.That<float>(step.Duration, (IResolveConstraint)(object)Is.EqualTo((object)duration));
		Assert.That<float>(step.HitTime, (IResolveConstraint)(object)Is.EqualTo((object)hitTime));
		Assert.That<float>(step.HitWindowDuration, (IResolveConstraint)(object)Is.EqualTo((object)hitWindowDuration));
		Assert.That<float>(step.HitboxRange, (IResolveConstraint)(object)Is.EqualTo((object)hitboxRange));
		Assert.That<float>(step.HitboxAngle, (IResolveConstraint)(object)Is.EqualTo((object)hitboxAngle));
		Assert.That<bool>(step.IsOmnidirectional, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(step.AllowWeaponSwitchDuringWindup, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(step.AllowWeaponSwitchDuringRecovery, (IResolveConstraint)(object)Is.True);
		Assert.That<float>(step.DamageMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)damage));
		Assert.That<float>(step.StaggerMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)stagger));
		Assert.That<float>(step.ForwardMotion, (IResolveConstraint)(object)Is.EqualTo((object)motion));
		Assert.That<float>(step.ComboQueueStart, (IResolveConstraint)(object)Is.EqualTo((object)queue));
	}
}
}
