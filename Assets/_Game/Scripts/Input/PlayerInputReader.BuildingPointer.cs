using System;
using UnityEngine.InputSystem;
using UnityEngine;

namespace Eidren.Input
{
	public sealed class PlayerInputReader : MonoBehaviour
	{
		private InputAction _buildingPointerAction;

		private InputAction _buildingPointerConfirmAction;

		private InputAction _moveAction;

		private InputAction _attackAction;

		private InputAction _dodgeAction;

		private InputAction _interactAction;

		private InputAction _switchWeaponAction;

		private InputAction _switchEidraAction;

		private InputAction _skill1Action;

		private InputAction _skill2Action;

		private InputAction _potionAction;

		private InputAction _foodAction;

		private InputAction _restartAction;

		private InputAction _inventoryToggleAction;

		private InputAction _technologyToggleAction;

		// F32-006: Fenster fuer das Eidra-Gespann.
		private InputAction _eidraTeamToggleAction;

		private InputAction _buildingToggleAction;

		private InputAction _buildingRotateAction;

		private InputAction _buildingDemolishAction;

		private InputAction _craftingToggleAction;

		private InputAction _inventoryCloseAction;

		private InputAction _inventoryUseAction;

		private InputAction _inventoryNavigateAction;

		private Vector2 _virtualMove;

		private bool _virtualInteract;

		private bool _hardwareInteract;

		private bool _enabled;

		public Vector2 Move { get; private set; }

		public bool InteractHeld { get; private set; }

		public bool GameplayEnabled { get; private set; } = true;

		public InputDisplayFamily DisplayFamily { get; private set; }

		public event Action<Vector2> BuildingPointerMoved;

		public event Action BuildingPointerConfirmed;

		public event Action AttackPressed;

		public event Action DodgePressed;

		public event Action SwitchWeaponPressed;

		public event Action SwitchEidraPressed;

		public event Action<int> SkillPressed;

		public event Action<int, bool> SkillPreviewChanged;

		public event Action<int> ConsumablePressed;

		public event Action RestartPressed;

		public event Action InteractPressed;

		public event Action InteractReleased;

		public event Action<bool> GameplayEnabledChanged;

		public event Action InventoryTogglePressed;

		public event Action TechnologyTogglePressed;

		public event Action EidraTeamTogglePressed;

		public event Action BuildingTogglePressed;

		public event Action BuildingRotatePressed;

		public event Action BuildingDemolishPressed;

		public event Action CraftingTogglePressed;

		public event Action InventoryClosePressed;

		public event Action InventoryUsePressed;

		public event Action<Vector2> InventoryNavigate;

		public event Action<InputDisplayFamily> DisplayFamilyChanged;

		private void ConfigureBuildingPointerActions()
		{
			_buildingPointerAction = new InputAction("BuildingPointer", InputActionType.PassThrough, "<Pointer>/position");
			_buildingPointerConfirmAction = Button("ConfirmBuildingPointer", "<Pointer>/press");
			_buildingPointerAction.performed += delegate(InputAction.CallbackContext context)
			{
				this.BuildingPointerMoved?.Invoke(context.ReadValue<Vector2>());
			};
			_buildingPointerConfirmAction.performed += delegate
			{
				this.BuildingPointerConfirmed?.Invoke();
			};
		}

		private void Awake()
		{
			DisplayFamily = ((!Application.isMobilePlatform) ? InputDisplayFamily.KeyboardMouse : InputDisplayFamily.Touch);
			base.gameObject.AddComponent<InputDisplayFamilyMonitor>().Initialize(this);
			_moveAction = new InputAction("Move");
			_moveAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
				.With("Left", "<Keyboard>/a")
				.With("Right", "<Keyboard>/d");
			_moveAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
				.With("Left", "<Keyboard>/leftArrow")
				.With("Right", "<Keyboard>/rightArrow");
			_moveAction.AddBinding("<Gamepad>/leftStick");
			_attackAction = Button("Attack", "<Mouse>/leftButton", "<Keyboard>/f", "<Gamepad>/buttonSouth");
			_dodgeAction = Button("Dodge", "<Keyboard>/space", "<Gamepad>/buttonEast");
			_interactAction = Button("Interact", "<Keyboard>/r", "<Gamepad>/buttonWest");
			_switchWeaponAction = Button("SwitchWeapon", "<Keyboard>/q", "<Gamepad>/rightShoulder");
			_switchEidraAction = Button("SwitchEidra", "<Keyboard>/e", "<Gamepad>/leftShoulder");
			_skill1Action = Button("Skill1", "<Keyboard>/digit1", "<Gamepad>/leftTrigger");
			_skill2Action = Button("Skill2", "<Keyboard>/digit2", "<Gamepad>/rightTrigger");
			_potionAction = Button("Potion", "<Keyboard>/digit3", "<Gamepad>/dpad/up");
			_foodAction = Button("BuffFood", "<Keyboard>/digit4", "<Gamepad>/dpad/down");
			_restartAction = Button("Restart", "<Keyboard>/enter", "<Gamepad>/start");
			_inventoryToggleAction = Button("Inventory", "<Keyboard>/i", "<Keyboard>/tab", "<Gamepad>/select");
			_technologyToggleAction = Button("Technology", "<Keyboard>/t", "<Gamepad>/leftStickPress");
			_eidraTeamToggleAction = Button("EidraTeam", "<Keyboard>/g", "<Gamepad>/dpad/left");
			_buildingToggleAction = Button("Building", "<Keyboard>/b", "<Gamepad>/rightStickPress");
			_buildingRotateAction = Button("RotateBuilding", "<Keyboard>/q", "<Gamepad>/rightShoulder");
			_buildingDemolishAction = Button("DemolishBuilding", "<Keyboard>/delete", "<Gamepad>/leftShoulder");
			_craftingToggleAction = Button("HandCrafting", "<Keyboard>/c", "<Gamepad>/buttonNorth");
			_inventoryCloseAction = Button("CloseInventory", "<Gamepad>/buttonEast");
			_inventoryUseAction = Button("UseInventoryItem", "<Keyboard>/enter", "<Keyboard>/space", "<Gamepad>/buttonSouth");
			_inventoryNavigateAction = new InputAction("NavigateInventory");
			_inventoryNavigateAction.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
				.With("Left", "<Keyboard>/leftArrow")
				.With("Right", "<Keyboard>/rightArrow");
			_inventoryNavigateAction.AddBinding("<Gamepad>/dpad");
			ConfigureBuildingPointerActions();
			_attackAction.performed += delegate
			{
				this.AttackPressed?.Invoke();
			};
			_dodgeAction.performed += delegate
			{
				this.DodgePressed?.Invoke();
			};
			_interactAction.performed += delegate
			{
				SetHardwareInteract(held: true);
			};
			_interactAction.canceled += delegate
			{
				SetHardwareInteract(held: false);
			};
			_switchWeaponAction.performed += delegate
			{
				this.SwitchWeaponPressed?.Invoke();
			};
			_switchEidraAction.performed += delegate
			{
				this.SwitchEidraPressed?.Invoke();
			};
			_skill1Action.started += delegate
			{
				this.SkillPreviewChanged?.Invoke(0, arg2: true);
			};
			_skill1Action.canceled += delegate
			{
				this.SkillPreviewChanged?.Invoke(0, arg2: false);
			};
			_skill2Action.started += delegate
			{
				this.SkillPreviewChanged?.Invoke(1, arg2: true);
			};
			_skill2Action.canceled += delegate
			{
				this.SkillPreviewChanged?.Invoke(1, arg2: false);
			};
			_skill1Action.performed += delegate
			{
				this.SkillPressed?.Invoke(0);
			};
			_skill2Action.performed += delegate
			{
				this.SkillPressed?.Invoke(1);
			};
			_potionAction.performed += delegate
			{
				this.ConsumablePressed?.Invoke(0);
			};
			_foodAction.performed += delegate
			{
				this.ConsumablePressed?.Invoke(1);
			};
			_restartAction.performed += delegate
			{
				this.RestartPressed?.Invoke();
			};
			_inventoryToggleAction.performed += delegate
			{
				this.InventoryTogglePressed?.Invoke();
			};
			_technologyToggleAction.performed += delegate
			{
				this.TechnologyTogglePressed?.Invoke();
			};
			_eidraTeamToggleAction.performed += delegate
			{
				this.EidraTeamTogglePressed?.Invoke();
			};
			_buildingToggleAction.performed += delegate
			{
				this.BuildingTogglePressed?.Invoke();
			};
			_buildingRotateAction.performed += delegate
			{
				this.BuildingRotatePressed?.Invoke();
			};
			_buildingDemolishAction.performed += delegate
			{
				this.BuildingDemolishPressed?.Invoke();
			};
			_craftingToggleAction.performed += delegate
			{
				this.CraftingTogglePressed?.Invoke();
			};
			_inventoryCloseAction.performed += delegate
			{
				this.InventoryClosePressed?.Invoke();
			};
			_inventoryUseAction.performed += delegate
			{
				this.InventoryUsePressed?.Invoke();
			};
			_inventoryNavigateAction.performed += delegate(InputAction.CallbackContext context)
			{
				this.InventoryNavigate?.Invoke(context.ReadValue<Vector2>());
			};
		}

		private static InputAction Button(string name, params string[] bindings)
		{
			InputAction inputAction = new InputAction(name, InputActionType.Button);
			foreach (string path in bindings)
			{
				inputAction.AddBinding(path);
			}
			return inputAction;
		}

		private void OnEnable()
		{
			_enabled = true;
			SetActionsEnabled(enabled: true);
		}

		private void OnDisable()
		{
			_enabled = false;
			_virtualInteract = false;
			_hardwareInteract = false;
			SetInteractHeld(held: false);
			SetActionsEnabled(enabled: false);
		}

		private void Update()
		{
			Vector2 vector = _moveAction.ReadValue<Vector2>();
			if (vector.sqrMagnitude > 0.01f && _moveAction.activeControl != null)
			{
				SetDisplayFamily((!(_moveAction.activeControl.device is Gamepad)) ? InputDisplayFamily.KeyboardMouse : InputDisplayFamily.Gamepad);
			}
			Move = Vector2.ClampMagnitude((_virtualMove.sqrMagnitude > vector.sqrMagnitude) ? _virtualMove : vector, 1f);
			SetInteractHeld(_virtualInteract || _hardwareInteract);
		}

		public void SetVirtualMove(Vector2 value)
		{
			if (value.sqrMagnitude > 0.0001f)
			{
				SetDisplayFamily(InputDisplayFamily.Touch);
			}
			_virtualMove = Vector2.ClampMagnitude(value, 1f);
		}

		public void SetVirtualInteract(bool held)
		{
			if (held)
			{
				SetDisplayFamily(InputDisplayFamily.Touch);
			}
			_virtualInteract = held;
			SetInteractHeld(_virtualInteract || _hardwareInteract);
		}

		public void PressAttack()
		{
			InvokeFromTouch(this.AttackPressed);
		}

		public void PressDodge()
		{
			InvokeFromTouch(this.DodgePressed);
		}

		public void PressSwitchWeapon()
		{
			InvokeFromTouch(this.SwitchWeaponPressed);
		}

		public void PressSwitchEidra()
		{
			InvokeFromTouch(this.SwitchEidraPressed);
		}

		public void PressSkill(int index)
		{
			InvokeFromTouch(this.SkillPressed, index);
		}

		public void PressConsumable(int index)
		{
			InvokeFromTouch(this.ConsumablePressed, index);
		}

		public void PressRestart()
		{
			InvokeFromTouch(this.RestartPressed);
		}

		public void PressInventoryToggle()
		{
			this.InventoryTogglePressed?.Invoke();
		}

		public void PressTechnologyToggle()
		{
			this.TechnologyTogglePressed?.Invoke();
		}

		public void PressEidraTeamToggle()
		{
			this.EidraTeamTogglePressed?.Invoke();
		}

		public void PressBuildingToggle()
		{
			this.BuildingTogglePressed?.Invoke();
		}

		public void PressBuildingRotate()
		{
			this.BuildingRotatePressed?.Invoke();
		}

		public void PressBuildingDemolish()
		{
			this.BuildingDemolishPressed?.Invoke();
		}

		public void PressCraftingToggle()
		{
			this.CraftingTogglePressed?.Invoke();
		}

		public void PressInventoryClose()
		{
			this.InventoryClosePressed?.Invoke();
		}

		public void PressInventoryUse()
		{
			this.InventoryUsePressed?.Invoke();
		}

		public void PressInventoryNavigate(Vector2 direction)
		{
			this.InventoryNavigate?.Invoke(direction);
		}

		public void SetGameplayEnabled(bool enabled)
		{
			enabled &= _enabled;
			bool flag = GameplayEnabled != enabled;
			GameplayEnabled = enabled;
			if (flag && !enabled)
			{
				this.GameplayEnabledChanged?.Invoke(obj: false);
			}
			InputAction[] array = new InputAction[10] { _moveAction, _attackAction, _dodgeAction, _interactAction, _switchWeaponAction, _switchEidraAction, _skill1Action, _skill2Action, _potionAction, _foodAction };
			foreach (InputAction inputAction in array)
			{
				if (enabled)
				{
					inputAction.Enable();
				}
				else
				{
					inputAction.Disable();
				}
			}
			_restartAction.Enable();
			_inventoryToggleAction.Enable();
			_technologyToggleAction.Enable();
			_eidraTeamToggleAction.Enable();
			_buildingToggleAction.Enable();
			_buildingRotateAction.Enable();
			_buildingDemolishAction.Enable();
			_craftingToggleAction.Enable();
			_inventoryCloseAction.Enable();
			_inventoryUseAction.Enable();
			_inventoryNavigateAction.Enable();
			_buildingPointerAction.Enable();
			_buildingPointerConfirmAction.Enable();
			if (!enabled)
			{
				Move = Vector2.zero;
				_virtualMove = Vector2.zero;
				_virtualInteract = false;
				_hardwareInteract = false;
				SetInteractHeld(held: false);
			}
			if (flag && enabled)
			{
				this.GameplayEnabledChanged?.Invoke(obj: true);
			}
		}

		public void SetDisplayFamily(InputDisplayFamily family)
		{
			if (DisplayFamily != family)
			{
				DisplayFamily = family;
				this.DisplayFamilyChanged?.Invoke(family);
			}
		}

		public bool HasInteractionBinding(string controlPath)
		{
			if (_interactAction == null || string.IsNullOrWhiteSpace(controlPath))
			{
				return false;
			}
			foreach (InputBinding binding in _interactAction.bindings)
			{
				if (string.Equals(binding.path, controlPath, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasInventoryBinding(string controlPath)
		{
			if (_inventoryToggleAction == null || string.IsNullOrWhiteSpace(controlPath))
			{
				return false;
			}
			foreach (InputBinding binding in _inventoryToggleAction.bindings)
			{
				if (string.Equals(binding.path, controlPath, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		private void SetHardwareInteract(bool held)
		{
			_hardwareInteract = held;
			SetInteractHeld(_virtualInteract || _hardwareInteract);
		}

		private void InvokeFromTouch(Action action)
		{
			SetDisplayFamily(InputDisplayFamily.Touch);
			action?.Invoke();
		}

		private void InvokeFromTouch(Action<int> action, int value)
		{
			SetDisplayFamily(InputDisplayFamily.Touch);
			action?.Invoke(value);
		}

		private void SetInteractHeld(bool held)
		{
			if (InteractHeld != held)
			{
				InteractHeld = held;
				if (held)
				{
					this.InteractPressed?.Invoke();
				}
				else
				{
					this.InteractReleased?.Invoke();
				}
			}
		}

		private void SetActionsEnabled(bool enabled)
		{
			InputAction[] array = new InputAction[23]
			{
				_moveAction, _attackAction, _dodgeAction, _interactAction, _switchWeaponAction, _switchEidraAction, _skill1Action, _skill2Action, _potionAction, _foodAction,
				_restartAction, _inventoryToggleAction, _inventoryCloseAction, _inventoryUseAction, _inventoryNavigateAction, _buildingPointerAction, _buildingPointerConfirmAction, _technologyToggleAction, _buildingToggleAction, _buildingRotateAction,
				_buildingDemolishAction, _craftingToggleAction, _eidraTeamToggleAction
			};
			foreach (InputAction inputAction in array)
			{
				if (enabled)
				{
					inputAction.Enable();
				}
				else
				{
					inputAction.Disable();
				}
			}
		}

		private void OnDestroy()
		{
			InputAction[] array = new InputAction[23]
			{
				_moveAction, _attackAction, _dodgeAction, _interactAction, _switchWeaponAction, _switchEidraAction, _skill1Action, _skill2Action, _potionAction, _foodAction,
				_restartAction, _inventoryToggleAction, _inventoryCloseAction, _inventoryUseAction, _inventoryNavigateAction, _buildingPointerAction, _buildingPointerConfirmAction, _technologyToggleAction, _buildingToggleAction, _buildingRotateAction,
				_buildingDemolishAction, _craftingToggleAction, _eidraTeamToggleAction
			};
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Dispose();
			}
		}
	}
}
