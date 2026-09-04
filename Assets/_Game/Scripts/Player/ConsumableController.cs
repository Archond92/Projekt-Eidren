using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Presentation;
using System.Collections;
using System;
using UnityEngine;

namespace Eidren.Player
{
	public sealed class ConsumableController : MonoBehaviour, IItemUseHandler
	{
		private PlayerInputReader _input;

		private Damageable _health;

		private PlayerCombatController _combat;

		private PlayerInventory _inventory;

		private ItemDefinition _healingPotion;

		private ItemDefinition _buffFood;

		private Coroutine _foodRoutine;

		public bool FoodBuffActive => FoodBuffRemaining > 0f;

		public float FoodBuffRemaining { get; private set; }

		public event Action<bool, float> FoodBuffChanged;

		public event Action<ItemUseActionType> ConsumableUsed;

		public void Initialize(PlayerInputReader input, Damageable health, PlayerCombatController combat, PlayerInventory inventory, ItemDefinition healingPotion, ItemDefinition buffFood)
		{
			if (_input != null)
			{
				_input.ConsumablePressed -= HandleConsumablePressed;
			}
			_input = input ?? throw new ArgumentNullException("input");
			_health = health ?? throw new ArgumentNullException("health");
			_combat = combat ?? throw new ArgumentNullException("combat");
			_inventory = inventory ?? throw new ArgumentNullException("inventory");
			_healingPotion = RequireUseDefinition(healingPotion, ItemUseActionType.Heal);
			_buffFood = RequireUseDefinition(buffFood, ItemUseActionType.TimedCombatBuff);
			_input.ConsumablePressed += HandleConsumablePressed;
		}

		private void HandleConsumablePressed(int index)
		{
			if (!(_input == null) && _input.GameplayEnabled && !(_health == null) && _health.IsAlive)
			{
				switch (index)
				{
				case 0:
					_inventory.UseFirst(_healingPotion.Id, this);
					break;
				case 1:
					_inventory.UseFirst(_buffFood.Id, this);
					break;
				}
			}
		}

		public bool TryUse(ItemDefinition item)
		{
			if (item == null || !item.CanBeUsed || _health == null || !_health.IsAlive)
			{
				return false;
			}
			ItemUseActionType actionType = item.UseConfiguration.ActionType;
			if (1 == 0)
			{
			}
			bool flag = actionType switch
			{
				ItemUseActionType.Heal => TryDrinkPotion(item.UseConfiguration), 
				ItemUseActionType.TimedCombatBuff => TryEatFood(item.UseConfiguration), 
				_ => false, 
			};
			if (1 == 0)
			{
			}
			bool flag2 = flag;
			if (flag2)
			{
				this.ConsumableUsed?.Invoke(actionType);
			}
			return flag2;
		}

		private bool TryDrinkPotion(ItemUseConfiguration useConfiguration)
		{
			float currentHealth = _health.CurrentHealth;
			if (!_health.Heal(useConfiguration.HealingAmount))
			{
				return false;
			}
			CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 3f, $"+{_health.CurrentHealth - currentHealth:0} HP", new Color(0.3f, 0.9f, 1f));
			CombatFeedback.SpawnStyleCue(base.transform.position + Vector3.up * 1.1f, StyleCue.Healing);
			return true;
		}

		private bool TryEatFood(ItemUseConfiguration useConfiguration)
		{
			float num = (useConfiguration.DamageMultiplier - 1f) * 100f;
			float num2 = (useConfiguration.StaggerDamageMultiplier - 1f) * 100f;
			CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 3f, $"BUFFFOOD\n+{num:0}% SCHADEN  " + $"+{num2:0}% STAGGER", new Color(1f, 0.7f, 0.2f), 1.6f);
			if (_foodRoutine != null)
			{
				StopCoroutine(_foodRoutine);
				_combat.SetConsumableBuff(1f, 1f);
			}
			_foodRoutine = StartCoroutine(FoodRoutine(useConfiguration));
			return true;
		}

		private IEnumerator FoodRoutine(ItemUseConfiguration useConfiguration)
		{
			FoodBuffRemaining = useConfiguration.Duration;
			_combat.SetConsumableBuff(useConfiguration.DamageMultiplier, useConfiguration.StaggerDamageMultiplier);
			this.FoodBuffChanged?.Invoke(arg1: true, FoodBuffRemaining);
			while (FoodBuffRemaining > 0f)
			{
				FoodBuffRemaining = Mathf.Max(0f, FoodBuffRemaining - Time.deltaTime);
				this.FoodBuffChanged?.Invoke(arg1: true, FoodBuffRemaining);
				yield return null;
			}
			_combat.SetConsumableBuff(1f, 1f);
			this.FoodBuffChanged?.Invoke(arg1: false, 0f);
			_foodRoutine = null;
		}

		public void CancelActiveEffects()
		{
			if (_foodRoutine != null)
			{
				StopCoroutine(_foodRoutine);
				_foodRoutine = null;
			}
			FoodBuffRemaining = 0f;
			if (_combat != null)
			{
				_combat.SetConsumableBuff(1f, 1f);
			}
			this.FoodBuffChanged?.Invoke(arg1: false, 0f);
		}

		private static ItemDefinition RequireUseDefinition(ItemDefinition definition, ItemUseActionType expectedAction)
		{
			if (definition == null)
			{
				throw new ArgumentNullException("definition");
			}
			if (!definition.CanBeUsed || definition.UseConfiguration.ActionType != expectedAction)
			{
				throw new InvalidOperationException("Item '" + definition.Id + "' must be usable with action " + $"'{expectedAction}'.");
			}
			return definition;
		}

		private void OnDisable()
		{
			CancelActiveEffects();
		}

		private void OnDestroy()
		{
			if (_input != null)
			{
				_input.ConsumablePressed -= HandleConsumablePressed;
			}
		}
	}
}
