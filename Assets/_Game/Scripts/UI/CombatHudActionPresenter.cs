using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Eidra;
using Eidren.Input;
using Eidren.Player;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class CombatHudActionPresenter : MonoBehaviour
	{
		private const float RefreshInterval = 0.1f;

		private const float FeedbackDuration = 1.15f;

		[SerializeField]
		private CanvasGroup actionCluster;

		[SerializeField]
		private CombatHudActionSlot attack;

		[SerializeField]
		private CombatHudActionSlot dodge;

		[SerializeField]
		private CombatHudActionSlot skill1;

		[SerializeField]
		private CombatHudActionSlot skill2;

		[SerializeField]
		private CombatHudActionSlot weaponSwitch;

		[SerializeField]
		private CombatHudActionSlot eidraSwitch;

		[SerializeField]
		private CombatHudActionSlot potion;

		[SerializeField]
		private CombatHudActionSlot food;

		[SerializeField]
		private Button inventoryButton;

		[SerializeField]
		private Text actionFeedback;

		[SerializeField]
		private Sprite hammerIcon;

		[SerializeField]
		private Sprite daggersIcon;

		[SerializeField]
		private Sprite spearIcon;

		[SerializeField]
		private Sprite eidraIcon;

		[SerializeField]
		private Sprite potionIcon;

		[SerializeField]
		private Sprite foodIcon;

		[SerializeField]
		private Sprite rockbreakerIcon;

		[SerializeField]
		private Sprite stoneskinIcon;

		[SerializeField]
		private Sprite shadowstepIcon;

		[SerializeField]
		private Sprite backmarkIcon;

		[SerializeField]
		private Color readyFrame = new Color(0.96f, 0.93f, 0.82f, 0.88f);

		[SerializeField]
		private Color focusFrame = new Color(0.96f, 0.66f, 0.16f, 1f);

		[SerializeField]
		private Color dangerFrame = new Color(0.94f, 0.28f, 0.2f, 1f);

		private PlayerInputReader _input;

		private PlayerMotor _motor;

		private PlayerCombatController _combat;

		private ConsumableController _consumables;

		private PlayerInventory _inventory;

		private EidraTeamController _eidra;

		private float _nextRefresh;

		private float _feedbackUntil;

		public CanvasGroup ActionCluster => actionCluster;

		public CombatHudActionSlot Attack => attack;

		public CombatHudActionSlot WeaponSwitch => weaponSwitch;

		public CombatHudActionSlot Skill1 => skill1;

		public CombatHudActionSlot Skill2 => skill2;

		public CombatHudActionSlot Potion => potion;

		public CombatHudActionSlot Food => food;

		public void ConfigureReferences(CanvasGroup cluster, CombatHudActionSlot authoredAttack, CombatHudActionSlot authoredDodge, CombatHudActionSlot authoredSkill1, CombatHudActionSlot authoredSkill2, CombatHudActionSlot authoredWeaponSwitch, CombatHudActionSlot authoredEidraSwitch, CombatHudActionSlot authoredPotion, CombatHudActionSlot authoredFood, Button authoredInventoryButton, Text feedback, Sprite[] icons)
		{
			actionCluster = cluster;
			attack = authoredAttack;
			dodge = authoredDodge;
			skill1 = authoredSkill1;
			skill2 = authoredSkill2;
			weaponSwitch = authoredWeaponSwitch;
			eidraSwitch = authoredEidraSwitch;
			potion = authoredPotion;
			food = authoredFood;
			inventoryButton = authoredInventoryButton;
			actionFeedback = feedback;
			hammerIcon = icons[0];
			daggersIcon = icons[1];
			eidraIcon = icons[2];
			potionIcon = icons[3];
			foodIcon = icons[4];
			rockbreakerIcon = icons[5];
			stoneskinIcon = icons[6];
			shadowstepIcon = icons[7];
			backmarkIcon = icons[8];
			// W-004: Speer-Icon ist optional nachgeruestet; aeltere Aufrufer
			// mit neun Icons bleiben gueltig.
			spearIcon = (icons.Length > 9) ? icons[9] : spearIcon;
		}

		public void Bind(PlayerInputReader input, PlayerMotor motor, PlayerCombatController combat, ConsumableController consumables, PlayerInventory inventory, EidraTeamController eidra)
		{
			Unbind();
			_input = input;
			_motor = motor;
			_combat = combat;
			_consumables = consumables;
			_inventory = inventory;
			_eidra = eidra;
			attack.Button.onClick.AddListener(_input.PressAttack);
			dodge.Button.onClick.AddListener(_input.PressDodge);
			weaponSwitch.Button.onClick.AddListener(_input.PressSwitchWeapon);
			eidraSwitch.Button.onClick.AddListener(_input.PressSwitchEidra);
			skill1.Button.onClick.AddListener(PressSkill1);
			skill2.Button.onClick.AddListener(PressSkill2);
			potion.Button.onClick.AddListener(UsePotion);
			food.Button.onClick.AddListener(UseFood);
			inventoryButton.onClick.AddListener(_input.PressInventoryToggle);
			_combat.WeaponChanged += RefreshWeapon;
			_inventory.Changed += HandleInventoryChanged;
			_input.GameplayEnabledChanged += RefreshGameplayEnabled;
			if (_eidra != null)
			{
				_eidra.ActiveEidraChanged += RefreshEidra;
				_eidra.AbilityFailed += ShowAbilityFailure;
				skill1.Button.GetComponent<AbilityRangePreview>().Initialize(_eidra, 0);
				skill2.Button.GetComponent<AbilityRangePreview>().Initialize(_eidra, 1);
				// N04-003: Tooltip-Auslöser an denselben Knöpfen. Das Feld
				// liegt einmal im HUD; fehlt es (altes Prefab), bleiben die
				// Auslöser still statt zu werfen.
				HudTooltipPanel tooltip = GetComponentInChildren<HudTooltipPanel>(includeInactive: true);
				skill1.Button.GetComponent<HudAbilityTooltip>()?.Initialize(_eidra, 0, tooltip);
				skill2.Button.GetComponent<HudAbilityTooltip>()?.Initialize(_eidra, 1, tooltip);
			}
			RefreshGameplayEnabled(_input.GameplayEnabled);
			RefreshWeapon(_combat.ActiveWeapon, null);
			RefreshEidra((_eidra != null) ? _eidra.ActiveData : null);
			RefreshConsumables();
			RefreshDynamicState();
		}

		private void Update()
		{
			if (!(_input == null) && !(Time.unscaledTime < _nextRefresh))
			{
				_nextRefresh = Time.unscaledTime + 0.1f;
				RefreshDynamicState();
			}
		}

		private void RefreshDynamicState()
		{
			dodge.SetLocked(_motor.IsDodging || _motor.CurrentStamina <= 0.01f);
			if (_eidra == null || _eidra.ActiveData == null)
			{
				skill1.SetLocked(locked: true);
				skill2.SetLocked(locked: true);
				skill1.SetCooldown(0f, 0f);
				skill2.SetCooldown(0f, 0f);
			}
			else
			{
				RefreshSkill(0, skill1, _eidra.ActiveData.Skill1);
				RefreshSkill(1, skill2, _eidra.ActiveData.Skill2);
			}
			bool flag = Time.unscaledTime < _feedbackUntil;
			actionFeedback.gameObject.SetActive(flag);
			attack.Frame.color = (flag ? dangerFrame : readyFrame);
			if (_consumables.FoodBuffActive)
			{
				food.Value.text = Mathf.CeilToInt(_consumables.FoodBuffRemaining).ToString();
			}
		}

		private void RefreshSkill(int index, CombatHudActionSlot slot, AbilityData ability)
		{
			float cooldownRemaining = _eidra.GetCooldownRemaining(index);
			float ratio = ((ability.Cooldown <= 0f) ? 0f : (cooldownRemaining / ability.Cooldown));
			slot.SetLocked(locked: false);
			slot.SetCooldown(ratio, cooldownRemaining);
			slot.Frame.color = ((cooldownRemaining > 0.05f) ? (readyFrame * new Color(1f, 1f, 1f, 0.55f)) : readyFrame);
		}

		private void RefreshWeapon(WeaponData current, WeaponData previous)
		{
			// W-004: Icons je Waffenfamilie statt des alten Hammer/Dolche-Paars;
			// der Wechselbutton zeigt die tatsaechlich verfuegbare Zweitwaffe
			// und sperrt sich ohne eine solche, statt ein Phantom anzubieten.
			bool flag = current != null;
			attack.SetLocked(!flag);
			attack.Icon.sprite = IconForWeapon(current);
			WeaponData alternate = (_combat != null) ? _combat.AlternateWeapon : null;
			bool hasAlternate = alternate != null;
			weaponSwitch.SetLocked(!flag || !hasAlternate);
			weaponSwitch.Icon.sprite = (hasAlternate ? IconForWeapon(alternate) : null);
			weaponSwitch.Icon.enabled = hasAlternate;
			attack.Frame.color = (flag ? focusFrame : readyFrame);
		}

		private Sprite IconForWeapon(WeaponData weapon)
		{
			if (weapon == null)
			{
				return hammerIcon;
			}
			switch (weapon.Identity.Family)
			{
			case WeaponFamily.Daggers:
				return daggersIcon;
			case WeaponFamily.Spear:
				return (spearIcon != null) ? spearIcon : hammerIcon;
			default:
				return hammerIcon;
			}
		}

		private void RefreshEidra(EidraData data)
		{
			bool flag = data != null;
			eidraSwitch.SetLocked(!flag);
			eidraSwitch.Icon.sprite = ((!flag) ? eidraIcon : ((data.Id == "noctarion") ? rockbreakerIcon : shadowstepIcon));
			skill1.SetLocked(!flag);
			skill2.SetLocked(!flag);
			if (!flag)
			{
				skill1.Icon.sprite = rockbreakerIcon;
				skill2.Icon.sprite = stoneskinIcon;
			}
			else
			{
				bool flag2 = data.Id == "noctarion";
				skill1.Icon.sprite = ((data.Skill1 != null && data.Skill1.Icon != null) ? data.Skill1.Icon : (flag2 ? shadowstepIcon : rockbreakerIcon));
				skill2.Icon.sprite = ((data.Skill2 != null && data.Skill2.Icon != null) ? data.Skill2.Icon : (flag2 ? backmarkIcon : stoneskinIcon));
			}
		}

		private void HandleInventoryChanged(InventoryChange change)
		{
			RefreshConsumables();
		}

		private void RefreshConsumables()
		{
			int totalAmount = _inventory.GetTotalAmount("healing_potion");
			int totalAmount2 = _inventory.GetTotalAmount("buff_food");
			potion.Icon.sprite = potionIcon;
			potion.Value.text = totalAmount.ToString();
			potion.SetLocked(totalAmount <= 0);
			food.Icon.sprite = foodIcon;
			food.Value.text = totalAmount2.ToString();
			food.SetLocked(totalAmount2 <= 0);
		}

		private void PressSkill1()
		{
			_input.PressSkill(0);
		}

		private void PressSkill2()
		{
			_input.PressSkill(1);
		}

		private void UsePotion()
		{
			UseConsumable(0, "healing_potion");
		}

		private void UseFood()
		{
			UseConsumable(1, "buff_food");
		}

		private void UseConsumable(int index, string itemId)
		{
			int totalAmount = _inventory.GetTotalAmount(itemId);
			_input.PressConsumable(index);
			if (totalAmount <= 0)
			{
				ShowFeedback("LEER");
			}
		}

		private void ShowAbilityFailure(int index, string message)
		{
			ShowFeedback(message);
		}

		private void ShowFeedback(string message)
		{
			actionFeedback.text = message;
			_feedbackUntil = Time.unscaledTime + 1.15f;
		}

		private void RefreshGameplayEnabled(bool enabled)
		{
			actionCluster.interactable = enabled;
			actionCluster.blocksRaycasts = enabled;
			actionCluster.alpha = (enabled ? 1f : 0.34f);
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_input != null)
			{
				attack.Button.onClick.RemoveListener(_input.PressAttack);
				dodge.Button.onClick.RemoveListener(_input.PressDodge);
				weaponSwitch.Button.onClick.RemoveListener(_input.PressSwitchWeapon);
				eidraSwitch.Button.onClick.RemoveListener(_input.PressSwitchEidra);
				_input.GameplayEnabledChanged -= RefreshGameplayEnabled;
				inventoryButton.onClick.RemoveListener(_input.PressInventoryToggle);
			}
			skill1?.Button.onClick.RemoveListener(PressSkill1);
			skill2?.Button.onClick.RemoveListener(PressSkill2);
			potion?.Button.onClick.RemoveListener(UsePotion);
			food?.Button.onClick.RemoveListener(UseFood);
			if (_combat != null)
			{
				_combat.WeaponChanged -= RefreshWeapon;
			}
			if (_inventory != null)
			{
				_inventory.Changed -= HandleInventoryChanged;
			}
			if (_eidra != null)
			{
				_eidra.ActiveEidraChanged -= RefreshEidra;
				_eidra.AbilityFailed -= ShowAbilityFailure;
			}
			_input = null;
			_motor = null;
			_combat = null;
			_consumables = null;
			_inventory = null;
			_eidra = null;
		}
	}
}
