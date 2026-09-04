using Eidren.Combat;
using Eidren.Data;
using Eidren.Input;
using Eidren.Player;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class WeaponSwitchTests
{
	[Test]
	public void HitWindow_BuffersExactlyOneWeaponSwitch()
	{
		AttackStepData step = new AttackStepData
		{
			AllowWeaponSwitchDuringWindup = true,
			AllowWeaponSwitchDuringRecovery = true
		};
		WeaponSwitchBuffer weaponSwitchBuffer = new WeaponSwitchBuffer();
		Assert.That<WeaponSwitchRequestResult>(weaponSwitchBuffer.Request(PlayerAttackPhase.HitWindow, step), (IResolveConstraint)(object)Is.EqualTo((object)WeaponSwitchRequestResult.Buffered));
		Assert.That<WeaponSwitchRequestResult>(weaponSwitchBuffer.Request(PlayerAttackPhase.HitWindow, step), (IResolveConstraint)(object)Is.EqualTo((object)WeaponSwitchRequestResult.AlreadyBuffered));
		Assert.That<bool>(weaponSwitchBuffer.HasBufferedSwitch, (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(weaponSwitchBuffer.Consume(), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(weaponSwitchBuffer.Consume(), (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void WindupAndRecovery_RespectStepConfiguration()
	{
		AttackStepData cancellable = new AttackStepData
		{
			AllowWeaponSwitchDuringWindup = true,
			AllowWeaponSwitchDuringRecovery = true
		};
		AttackStepData committed = new AttackStepData
		{
			AllowWeaponSwitchDuringWindup = false,
			AllowWeaponSwitchDuringRecovery = false
		};
		Assert.That<bool>(WeaponSwitchRules.CanSwitchImmediately(PlayerAttackPhase.Windup, cancellable), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(WeaponSwitchRules.CanSwitchImmediately(PlayerAttackPhase.Recovery, cancellable), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(WeaponSwitchRules.CanSwitchImmediately(PlayerAttackPhase.HitWindow, cancellable), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(WeaponSwitchRules.CanSwitchImmediately(PlayerAttackPhase.Windup, committed), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(WeaponSwitchRules.CanSwitchImmediately(PlayerAttackPhase.Recovery, committed), (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void WeaponChangedEvent_ObservesAtomicDataAndHitboxState()
	{
		GameObject player = new GameObject("WeaponSwitchTest_Player");
		try
		{
			PlayerInputReader input = player.AddComponent<PlayerInputReader>();
			PlayerMotor motor = player.AddComponent<PlayerMotor>();
			MeleeWeaponHitbox hitbox = player.AddComponent<MeleeWeaponHitbox>();
			PlayerCombatController combat = player.AddComponent<PlayerCombatController>();
			GameObject weaponObject = new GameObject("WeaponDriver");
			weaponObject.transform.SetParent(player.transform, worldPositionStays: false);
			WeaponData hammer = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Hammer.asset");
			WeaponData daggers = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Daggers.asset");
			Assert.That<WeaponData>(hammer, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<WeaponData>(daggers, (IResolveConstraint)(object)Is.Not.Null);
			combat.Initialize(input, motor, new SceneCombatTargetQuery(), hitbox, null, hammer, daggers, weaponObject.transform);
			combat.SetWeaponAvailability(hammerAvailable: true, daggersAvailable: true, hammer.Id);
			hitbox.BeginWindow(hammer.Combo[0]);
			int changeCount = 0;
			bool eventObservedAtomicState = false;
			combat.WeaponChanged += delegate(WeaponData current, WeaponData previous)
			{
				changeCount++;
				eventObservedAtomicState = current == daggers && previous == hammer && combat.ActiveWeapon == daggers && !hitbox.IsWindowActive && hitbox.CurrentStep.HitboxRange == daggers.Combo[0].HitboxRange;
			};
			input.PressSwitchWeapon();
			Assert.That<int>(changeCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
			Assert.That<bool>(eventObservedAtomicState, (IResolveConstraint)(object)Is.True);
			Assert.That<bool>(hitbox.IsWindowActive, (IResolveConstraint)(object)Is.False);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(player);
		}
	}

	[Test]
	public void EquippedVariants_DefaultToFirstAndSelectByStableItemId()
	{
		GameObject player = new GameObject("WeaponVariantSlots_Player");
		try
		{
			PlayerInputReader input = player.AddComponent<PlayerInputReader>();
			PlayerMotor motor = player.AddComponent<PlayerMotor>();
			MeleeWeaponHitbox hitbox = player.AddComponent<MeleeWeaponHitbox>();
			PlayerCombatController playerCombatController = player.AddComponent<PlayerCombatController>();
			GameObject weaponObject = new GameObject("WeaponDriver");
			weaponObject.transform.SetParent(player.transform, worldPositionStays: false);
			PlayerWeaponVisual visual = weaponObject.AddComponent<PlayerWeaponVisual>();
			GameObject hammerRoot = new GameObject("Hammer");
			GameObject daggersRoot = new GameObject("Daggers");
			GameObject spearRoot = new GameObject("Spear");
			hammerRoot.transform.SetParent(weaponObject.transform, worldPositionStays: false);
			daggersRoot.transform.SetParent(weaponObject.transform, worldPositionStays: false);
			spearRoot.transform.SetParent(weaponObject.transform, worldPositionStays: false);
			hammerRoot.AddComponent<SpriteRenderer>();
			daggersRoot.AddComponent<SpriteRenderer>();
			Sprite regularSpear = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/ITEM_TMP_CopperSpear.png");
			Sprite emberThornSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/ITEM_TMP_EmberThorn.png");
			spearRoot.AddComponent<SpriteRenderer>().sprite = regularSpear;
			visual.Configure(hammerRoot, daggersRoot, spearRoot);
			visual.ConfigureNamedSprites(null, null, emberThornSprite);
			WeaponData hammer = Load("Hammer");
			WeaponData daggers = Load("Daggers");
			WeaponData copperHammer = Load("CopperHammer");
			WeaponData copperSpear = Load("CopperSpear");
			WeaponData emberThorn = Load("EmberThorn");
			playerCombatController.Initialize(input, motor, new SceneCombatTargetQuery(), hitbox, null, hammer, daggers, weaponObject.transform);
			playerCombatController.SetEquippedWeapons(copperHammer, copperSpear, string.Empty);
			Assert.That<WeaponData>(playerCombatController.ActiveWeapon, (IResolveConstraint)(object)Is.SameAs((object)copperHammer));
			Assert.That<float>(hitbox.CurrentStep.HitboxRange, (IResolveConstraint)(object)Is.EqualTo((object)hammer.Combo[0].HitboxRange));
			Assert.That<bool>(playerCombatController.TrySelectWeapon("copper_spear"), (IResolveConstraint)(object)Is.True);
			Assert.That<WeaponData>(playerCombatController.ActiveWeapon, (IResolveConstraint)(object)Is.SameAs((object)copperSpear));
			Assert.That<float>(hitbox.CurrentStep.HitboxRange, (IResolveConstraint)(object)Is.EqualTo((object)4.2f));
			playerCombatController.SetEquippedWeapons(copperHammer, emberThorn, "ember_thorn");
			Assert.That<WeaponData>(playerCombatController.ActiveWeapon, (IResolveConstraint)(object)Is.SameAs((object)emberThorn));
			Assert.That<Sprite>(visual.CurrentSpearSprite, (IResolveConstraint)(object)Is.SameAs((object)emberThornSprite), "Replacing an active slot must refresh its named sprite.", Array.Empty<object>());
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(player);
		}
	}

	private static WeaponData Load(string name)
	{
		WeaponData weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/" + name + ".asset");
		Assert.That<WeaponData>(weaponData, (IResolveConstraint)(object)Is.Not.Null, name, Array.Empty<object>());
		return weaponData;
	}
}
}
