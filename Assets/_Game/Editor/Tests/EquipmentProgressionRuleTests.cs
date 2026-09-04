using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Runtime.CompilerServices;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class EquipmentProgressionRuleTests
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static TestDelegate _003C_003E9__12_0;

		public static TestDelegate _003C_003E9__12_1;

		public static TestDelegate _003C_003E9__12_2;

		internal void _003CRuleInputs_RejectNegativeOrUnsupportedValues_003Eb__12_0()
		{
			HarvestProgressionRules.Yield(-1, 0);
		}

		internal void _003CRuleInputs_RejectNegativeOrUnsupportedValues_003Eb__12_1()
		{
			DurabilityRules.ToolWear(-1, 0);
		}

		internal void _003CRuleInputs_RejectNegativeOrUnsupportedValues_003Eb__12_2()
		{
			ExperienceRules.Award(ExperienceSource.ResourceNode, 3);
		}
	}

	[Test]
	public void Protection_AddsPartsCapsAtSixtyPercentAndReducesDamage()
	{
		float mixedSet = ProtectionRules.TotalProtection(new float[4] { 0.12f, 0.06f, 0.02f, 0.01f });
		float num = ProtectionRules.TotalProtection(new float[3] { 0.3f, 0.3f, 0.1f });
		Assert.That<float>(mixedSet, (IResolveConstraint)(object)Is.EqualTo((object)0.21f).Within((object)0.0001f));
		Assert.That<float>(num, (IResolveConstraint)(object)Is.EqualTo((object)0.6f).Within((object)0.0001f));
		Assert.That<float>(ProtectionRules.ReceivedDamage(100f, mixedSet), (IResolveConstraint)(object)Is.EqualTo((object)79f).Within((object)0.0001f));
	}

	[Test]
	public void ClothArmor_DataAddsToTenPercentProtection()
	{
		GameObject root = new GameObject("ClothProtection_Test");
		try
		{
			ContentDatabase content = root.AddComponent<ContentDatabase>();
			PlayerEquipment equipment = new PlayerEquipment(content);
			Equip(equipment, content, EquipmentSlot.Head, "armor_wanderer_hood");
			Equip(equipment, content, EquipmentSlot.Chest, "armor_wanderer_coat");
			Equip(equipment, content, EquipmentSlot.Hands, "armor_wanderer_bracers");
			Equip(equipment, content, EquipmentSlot.Legs, "armor_wanderer_legs");
			Assert.That<float>(new EquipmentDurabilityService(equipment, content).TotalArmorProtection(), (IResolveConstraint)(object)Is.EqualTo((object)0.1f).Within((object)0.0001f));
			Assert.That<float>(content.GetItem("armor_wanderer_coat").ProtectionContribution, (IResolveConstraint)(object)Is.EqualTo((object)0.04f).Within((object)0.0001f));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void BreakingArmor_ProtectsCurrentHitThenAllPartsDisappear()
	{
		GameObject root = new GameObject("ArmorBreakOrder_Test");
		try
		{
			ContentDatabase content = root.AddComponent<ContentDatabase>();
			PlayerEquipment equipment = new PlayerEquipment(content);
			Equip(equipment, content, EquipmentSlot.Head, "armor_wanderer_hood", 1);
			Equip(equipment, content, EquipmentSlot.Chest, "armor_wanderer_coat", 1);
			Equip(equipment, content, EquipmentSlot.Hands, "armor_wanderer_bracers", 1);
			Equip(equipment, content, EquipmentSlot.Legs, "armor_wanderer_legs", 1);
			EquipmentDurabilityService durability = new EquipmentDurabilityService(equipment, content);
			Damageable health = root.AddComponent<Damageable>();
			health.Initialize(100f);
			health.Protection = durability.TotalArmorProtection();
			ArmorWearSummary summary = default(ArmorWearSummary);
			health.Damaged += delegate(DamageInfo damage)
			{
				summary = durability.WearArmor(damage.HealthDamage);
				health.Protection = durability.TotalArmorProtection();
			};
			health.ApplyDamage(new DamageInfo(100f, 0f, Vector3.zero, root, isBackAttack: false, "test.armor.break", "test"));
			Assert.That<float>(health.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)10f).Within((object)0.0001f), "The full cloth set protects the hit that breaks it.", Array.Empty<object>());
			Assert.That<int>(summary.WornParts, (IResolveConstraint)(object)Is.EqualTo((object)4));
			Assert.That<int>(summary.BrokenItemNames.Count, (IResolveConstraint)(object)Is.EqualTo((object)4));
			Assert.That<float>(health.Protection, (IResolveConstraint)(object)Is.Zero);
			EquipmentSlot[] array = new EquipmentSlot[4]
			{
				EquipmentSlot.Head,
				EquipmentSlot.Chest,
				EquipmentSlot.Hands,
				EquipmentSlot.Legs
			};
			for (int num = 0; num < array.Length; num++)
			{
				EquipmentSlot slot = array[num];
				Assert.That<bool>(equipment.TryGetSlot(slot, out var _), (IResolveConstraint)(object)Is.False, slot.ToString(), Array.Empty<object>());
			}
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[Test]
	public void FullyPreventedDamage_DoesNotWearArmor()
	{
		GameObject root = new GameObject("ArmorNoDamage_Test");
		try
		{
			ContentDatabase content = root.AddComponent<ContentDatabase>();
			PlayerEquipment equipment = new PlayerEquipment(content);
			Equip(equipment, content, EquipmentSlot.Head, "armor_wanderer_hood", 5);
			EquipmentDurabilityService durability = new EquipmentDurabilityService(equipment, content);
			Damageable damageable = root.AddComponent<Damageable>();
			damageable.Initialize(100f);
			damageable.DamageTakenMultiplier = 0f;
			damageable.Protection = durability.TotalArmorProtection();
			damageable.Damaged += delegate(DamageInfo damage)
			{
				durability.WearArmor(damage.HealthDamage);
			};
			damageable.ApplyDamage(new DamageInfo(20f, 0f, Vector3.zero, root, isBackAttack: false, "test.armor.prevented", "test"));
			Assert.That<float>(damageable.CurrentHealth, (IResolveConstraint)(object)Is.EqualTo((object)100f));
			Assert.That<bool>(equipment.TryGetSlot(EquipmentSlot.Head, out var hood), (IResolveConstraint)(object)Is.True);
			Assert.That<int>(hood.Durability, (IResolveConstraint)(object)Is.EqualTo((object)5));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	[TestCase(false, 0)]
	[TestCase(true, 1)]
	public void WeaponWear_DependsOnSuccessfulAttack(bool successful, int expected)
	{
		Assert.That<int>(DurabilityRules.WeaponWear(successful), (IResolveConstraint)(object)Is.EqualTo((object)expected));
	}

	[TestCase(0f, 0)]
	[TestCase(0.01f, 1)]
	[TestCase(25f, 1)]
	public void ArmorWear_DependsOnReceivedDamage(float damage, int expected)
	{
		Assert.That<int>(DurabilityRules.ArmorWear(damage), (IResolveConstraint)(object)Is.EqualTo((object)expected));
	}

	[TestCase(0, 1, 4)]
	[TestCase(1, 2, 4)]
	[TestCase(0, 0, 3)]
	[TestCase(1, 1, 3)]
	[TestCase(2, 2, 3)]
	[TestCase(1, 0, 2)]
	[TestCase(2, 1, 2)]
	[TestCase(2, 0, 1)]
	public void ToolWear_FollowsRelativeTierTable(int toolTier, int resourceTier, int expected)
	{
		Assert.That<int>(DurabilityRules.ToolWear(toolTier, resourceTier), (IResolveConstraint)(object)Is.EqualTo((object)expected));
	}

	[Test]
	public void DurabilityApply_ClampsAtZeroAndMarksBreakage()
	{
		DurabilityResult intact = DurabilityRules.Apply(10, 3);
		DurabilityResult broken = DurabilityRules.Apply(2, 4);
		Assert.That<int>(intact.Remaining, (IResolveConstraint)(object)Is.EqualTo((object)7));
		Assert.That<bool>(intact.IsBroken, (IResolveConstraint)(object)Is.False);
		Assert.That<int>(broken.Remaining, (IResolveConstraint)(object)Is.Zero);
		Assert.That<bool>(broken.IsBroken, (IResolveConstraint)(object)Is.True);
	}

	[TestCase(0, null, 1)]
	[TestCase(0, 0, 2)]
	[TestCase(0, 1, 3)]
	[TestCase(0, 2, 4)]
	[TestCase(0, 3, 4)]
	[TestCase(1, null, 0)]
	[TestCase(1, 0, 2)]
	[TestCase(1, 1, 3)]
	[TestCase(1, 2, 4)]
	[TestCase(2, 0, 0)]
	[TestCase(2, 1, 2)]
	[TestCase(2, 2, 3)]
	[TestCase(2, 3, 4)]
	public void HarvestYield_FollowsEveryTierCombination(int resourceTier, int? toolTier, int expected)
	{
		Assert.That<int>(HarvestProgressionRules.Yield(resourceTier, toolTier), (IResolveConstraint)(object)Is.EqualTo((object)expected));
	}

	[TestCase(1, null, ToolStrain.Insufficient)]
	[TestCase(2, 0, ToolStrain.Insufficient)]
	[TestCase(2, 1, ToolStrain.High)]
	[TestCase(2, 2, ToolStrain.Normal)]
	[TestCase(2, 3, ToolStrain.Low)]
	public void ToolStrain_UsesPlayerFacingRelativeStates(int resourceTier, int? toolTier, ToolStrain expected)
	{
		Assert.That<ToolStrain>(HarvestProgressionRules.Strain(resourceTier, toolTier), (IResolveConstraint)(object)Is.EqualTo((object)expected));
	}

	[TestCase(1, 175)]
	[TestCase(2, 225)]
	[TestCase(20, 1225)]
	[TestCase(24, 1450)]
	[TestCase(39, 2275)]
	public void ExperienceCurve_RoundsToNearestTwentyFive(int level, int expected)
	{
		Assert.That<int>(ExperienceRules.ExperienceToNextLevel(level), (IResolveConstraint)(object)Is.EqualTo((object)expected));
	}

	[TestCase(new object[]
	{
		ExperienceSource.ResourceNode,
		0,
		false,
		8
	})]
	[TestCase(new object[]
	{
		ExperienceSource.ResourceNode,
		1,
		false,
		12
	})]
	[TestCase(new object[]
	{
		ExperienceSource.ResourceNode,
		2,
		false,
		18
	})]
	[TestCase(new object[]
	{
		ExperienceSource.Recipe,
		0,
		true,
		25
	})]
	[TestCase(new object[]
	{
		ExperienceSource.Recipe,
		1,
		true,
		50
	})]
	[TestCase(new object[]
	{
		ExperienceSource.Recipe,
		2,
		true,
		90
	})]
	[TestCase(new object[]
	{
		ExperienceSource.Recipe,
		0,
		false,
		1
	})]
	[TestCase(new object[]
	{
		ExperienceSource.Recipe,
		1,
		false,
		2
	})]
	[TestCase(new object[]
	{
		ExperienceSource.Recipe,
		2,
		false,
		4
	})]
	[TestCase(new object[]
	{
		ExperienceSource.Building,
		0,
		true,
		40
	})]
	[TestCase(new object[]
	{
		ExperienceSource.Building,
		1,
		true,
		75
	})]
	[TestCase(new object[]
	{
		ExperienceSource.Building,
		2,
		true,
		125
	})]
	[TestCase(new object[]
	{
		ExperienceSource.Building,
		2,
		false,
		4
	})]
	public void ExperienceAwards_FollowTierAndFirstCompletionTables(ExperienceSource source, int tier, bool first, int expected)
	{
		Assert.That<int>(ExperienceRules.Award(source, tier, first), (IResolveConstraint)(object)Is.EqualTo((object)expected));
	}

	[Test]
	public void RuleInputs_RejectNegativeOrUnsupportedValues()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		object obj = _003C_003Ec._003C_003E9__12_0;
		if (obj == null)
		{
			TestDelegate val = delegate
			{
				HarvestProgressionRules.Yield(-1, 0);
			};
			_003C_003Ec._003C_003E9__12_0 = val;
			obj = (object)val;
		}
		Assert.Throws<ArgumentOutOfRangeException>((TestDelegate)obj);
		object obj2 = _003C_003Ec._003C_003E9__12_1;
		if (obj2 == null)
		{
			TestDelegate val2 = delegate
			{
				DurabilityRules.ToolWear(-1, 0);
			};
			_003C_003Ec._003C_003E9__12_1 = val2;
			obj2 = (object)val2;
		}
		Assert.Throws<ArgumentOutOfRangeException>((TestDelegate)obj2);
		object obj3 = _003C_003Ec._003C_003E9__12_2;
		if (obj3 == null)
		{
			TestDelegate val3 = delegate
			{
				ExperienceRules.Award(ExperienceSource.ResourceNode, 3);
			};
			_003C_003Ec._003C_003E9__12_2 = val3;
			obj3 = (object)val3;
		}
		Assert.Throws<ArgumentOutOfRangeException>((TestDelegate)obj3);
	}

	private static void Equip(PlayerEquipment equipment, ContentDatabase content, EquipmentSlot slot, string itemId, int durability = -1)
	{
		ItemDefinition item = content.GetItem(itemId);
		Assert.That<bool>(equipment.TryEquip(slot, ItemStack.Create(item, 1, "test." + itemId, durability), out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
	}
}
}
