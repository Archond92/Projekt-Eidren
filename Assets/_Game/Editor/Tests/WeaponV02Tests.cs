using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class WeaponV02Tests
{
	[Test]
	public void Signatures_ApplyOnlyTheirSpecifiedNormalAttackRule()
	{
		WeaponHitModifiers hammer = WeaponCombatRules.Resolve(WeaponSignature.Impact, 2, isBackAttack: false, 2f, 3.4f);
		WeaponHitModifiers dagger = WeaponCombatRules.Resolve(WeaponSignature.Ambush, 0, isBackAttack: true, 1.5f, 3f);
		WeaponHitModifiers spearInner = WeaponCombatRules.Resolve(WeaponSignature.ReachWindow, 0, isBackAttack: false, 2.79f, 4.2f);
		WeaponHitModifiers spearOuter = WeaponCombatRules.Resolve(WeaponSignature.ReachWindow, 0, isBackAttack: false, 2.8f, 4.2f);
		Assert.That<WeaponSignatureCue>(hammer.Cue, (IResolveConstraint)(object)Is.EqualTo((object)WeaponSignatureCue.HammerImpact));
		Assert.That<float>(hammer.HealthMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)1f));
		Assert.That<WeaponSignatureCue>(dagger.Cue, (IResolveConstraint)(object)Is.EqualTo((object)WeaponSignatureCue.DaggerAmbush));
		Assert.That<float>(dagger.HealthMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)1.75f));
		Assert.That<WeaponSignatureCue>(spearInner.Cue, (IResolveConstraint)(object)Is.EqualTo((object)WeaponSignatureCue.None));
		Assert.That<WeaponSignatureCue>(spearOuter.Cue, (IResolveConstraint)(object)Is.EqualTo((object)WeaponSignatureCue.SpearReachWindow));
		Assert.That<float>(spearOuter.HealthMultiplier, (IResolveConstraint)(object)Is.EqualTo((object)1.25f));
	}

	[Test]
	public void WeaponCatalog_ContainsAllFixedVariantsAndMovesets()
	{
		WeaponCatalogDefinition weaponCatalogDefinition = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>("Assets/_Game/Resources/Data/WeaponCatalog_V02.asset");
		Assert.That<WeaponCatalogDefinition>(weaponCatalogDefinition, (IResolveConstraint)(object)Is.Not.Null);
		WeaponData[] weaponArray = weaponCatalogDefinition.WeaponArray;
		Assert.That<WeaponData[]>(weaponArray, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)11));
		Assert.That<int>(weaponArray.Select((WeaponData value) => value.Id).Distinct().Count(), (IResolveConstraint)(object)Is.EqualTo((object)11));
		AssertWeapon(weaponArray, "hammer", 15f, 10f, 0, named: false);
		AssertWeapon(weaponArray, "copper_hammer", 18f, 12f, 1, named: false);
		AssertWeapon(weaponArray, "iron_hammer", 21.8f, 14.5f, 2, named: false);
		AssertWeapon(weaponArray, "sealbreaker", 23.5f, 15.7f, 1, named: true);
		AssertWeapon(weaponArray, "daggers", 12f, 2.5f, 0, named: false);
		AssertWeapon(weaponArray, "copper_daggers", 14.4f, 3f, 1, named: false);
		AssertWeapon(weaponArray, "iron_daggers", 17.4f, 3.6f, 2, named: false);
		AssertWeapon(weaponArray, "ash_fangs", 18.8f, 3.9f, 1, named: true);
		AssertWeapon(weaponArray, "copper_spear", 17f, 6f, 1, named: false);
		AssertWeapon(weaponArray, "iron_spear", 20.5f, 7.2f, 2, named: false);
		AssertWeapon(weaponArray, "ember_thorn", 22.1f, 7.8f, 1, named: true);
		WeaponData spear = weaponArray.Single((WeaponData value) => value.Id == "copper_spear");
		Assert.That<AttackStepData[]>(spear.Combo, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)3));
		Assert.That<IEnumerable<float>>(spear.Combo.Select((AttackStepData step) => step.HitboxRange), (IResolveConstraint)(object)Is.All.EqualTo((object)4.2f));
		Assert.That<IEnumerable<float>>(spear.Combo.Select((AttackStepData step) => step.HitboxAngle), (IResolveConstraint)(object)Is.All.EqualTo((object)35f));
		Assert.That<IEnumerable<float>>(spear.Combo.Select((AttackStepData step) => step.DamageMultiplier), (IResolveConstraint)(object)Is.EqualTo((object)new float[3] { 0.95f, 1.05f, 1.3f }));
		Assert.That<float>(spear.Combo[2].ForwardMotion, (IResolveConstraint)(object)Is.GreaterThan((object)spear.Combo[1].ForwardMotion));
	}

	[Test]
	public void SuccessfulWeaponWear_RemovesBrokenInstance()
	{
		ItemDefinition hammer = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/Hammer.asset");
		GameObject root = new GameObject("WeaponWearTest");
		try
		{
			ContentDatabase content = root.AddComponent<ContentDatabase>();
			content.ConfigureItems(new ItemDefinition[1] { hammer });
			PlayerEquipment playerEquipment = new PlayerEquipment(content);
			Assert.That<bool>(playerEquipment.TryEquip(EquipmentSlot.Weapon1, ItemStack.Create(hammer, 1, "weapon.test", 1), out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
			Assert.That<bool>(new EquipmentDurabilityService(playerEquipment, content).TryWearWeapon("hammer", DurabilityRules.WeaponWear(successfulAttack: true), out var broken), (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(broken, (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(playerEquipment.TryGetSlot(EquipmentSlot.Weapon1, out var _), (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void WeaponItems_CarryFixedDurabilityAndCraftability()
	{
		AssertItem("copper_hammer", 1, 150, craftable: true);
		AssertItem("iron_hammer", 2, 180, craftable: true);
		AssertItem("sealbreaker", 1, 200, craftable: false);
		AssertItem("copper_daggers", 1, 275, craftable: true);
		AssertItem("iron_daggers", 2, 330, craftable: true);
		AssertItem("ash_fangs", 1, 365, craftable: false);
		AssertItem("copper_spear", 1, 190, craftable: true);
		AssertItem("iron_spear", 2, 230, craftable: true);
		AssertItem("ember_thorn", 1, 255, craftable: false);
	}

	private static void AssertWeapon(IEnumerable<WeaponData> weapons, string id, float damage, float stagger, int tier, bool named)
	{
		WeaponData weaponData = weapons.Single((WeaponData value) => value.Id == id);
		Assert.That<float>(weaponData.BaseDamage, (IResolveConstraint)(object)Is.EqualTo((object)damage), id, Array.Empty<object>());
		Assert.That<float>(weaponData.BaseStaggerDamage, (IResolveConstraint)(object)Is.EqualTo((object)stagger), id, Array.Empty<object>());
		Assert.That<int>(weaponData.Identity.Tier, (IResolveConstraint)(object)Is.EqualTo((object)tier), id, Array.Empty<object>());
		Assert.That<bool>(weaponData.Identity.IsNamedVariant, (IResolveConstraint)(object)Is.EqualTo((object)named), id, Array.Empty<object>());
		Assert.That<WeaponFamily>(weaponData.Identity.Family, (IResolveConstraint)(object)Is.Not.EqualTo((object)WeaponFamily.None), id, Array.Empty<object>());
		Assert.That<WeaponSignature>(weaponData.Identity.Signature, (IResolveConstraint)(object)Is.Not.EqualTo((object)WeaponSignature.None), id, Array.Empty<object>());
		Assert.That<AttackStepData[]>(weaponData.ResolvedMoveset.Combo, (IResolveConstraint)(object)Is.Not.Empty, id, Array.Empty<object>());
	}

	private static void AssertItem(string id, int tier, int durability, bool craftable)
	{
		ItemDefinition itemDefinition = AssetDatabase.FindAssets("t:ItemDefinition", new string[1] { "Assets/_Game/Data/Items" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ItemDefinition>)
			.Single((ItemDefinition value) => value != null && value.Id == id);
		Assert.That<ItemCategory>(itemDefinition.Category, (IResolveConstraint)(object)Is.EqualTo((object)ItemCategory.Weapon), id, Array.Empty<object>());
		Assert.That<int>(itemDefinition.Tier, (IResolveConstraint)(object)Is.EqualTo((object)tier), id, Array.Empty<object>());
		Assert.That<int>(itemDefinition.MaximumDurability, (IResolveConstraint)(object)Is.EqualTo((object)durability), id, Array.Empty<object>());
		Assert.That<bool>(itemDefinition.CanBeCrafted, (IResolveConstraint)(object)Is.EqualTo((object)craftable), id, Array.Empty<object>());
		Assert.That<string[]>(itemDefinition.ValidateDefinition().Errors, (IResolveConstraint)(object)Is.Empty, id, Array.Empty<object>());
	}
}
}
