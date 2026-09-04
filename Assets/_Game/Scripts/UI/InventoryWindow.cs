using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Player;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.UI
{
	public sealed class InventoryWindow : MonoBehaviour
	{
		[SerializeField]
		private GameObject panelRoot;

		[SerializeField]
		private Button openButton;

		[SerializeField]
		private Button useButton;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private InventorySlotView[] slots;

		[SerializeField]
		private Image detailIcon;

		[SerializeField]
		private Text detailName;

		[SerializeField]
		private Text detailDescription;

		[SerializeField]
		private Text feedbackText;

		[SerializeField]
		private EquipmentPanelView equipmentPanel;

		private PlayerInventory _inventory;

		private ContentDatabase _database;

		private IItemUseHandler _useHandler;

		private PlayerInputReader _input;

		private PlayerMotor _motor;

		private PlayerCombatController _combat;

		private Damageable _health;

		private SceneFlowService _sceneFlow;

		private int _selectedIndex;

		private int _lastPointerSelected = -1;

		private int _dragSourceIndex = -1;

		private Image _dragPreview;

		private bool _initialized;

		public bool IsOpen { get; private set; }

		public int SelectedIndex => _selectedIndex;

		public IReadOnlyList<InventorySlotView> Slots => slots;

		public Button OpenButton => openButton;

		public Button UseButton => useButton;

		public Button CloseButton => closeButton;

		public bool IsDragging => _dragSourceIndex >= 0;

		public event Action<bool> VisibilityChanged;

		public void ConfigureReferences(GameObject configuredPanelRoot, Button configuredOpenButton, Button configuredUseButton, Button configuredCloseButton, InventorySlotView[] configuredSlots, Image configuredDetailIcon, Text configuredDetailName, Text configuredDetailDescription, Text configuredFeedbackText, EquipmentPanelView configuredEquipmentPanel)
		{
			panelRoot = configuredPanelRoot;
			openButton = configuredOpenButton;
			useButton = configuredUseButton;
			closeButton = configuredCloseButton;
			slots = configuredSlots;
			detailIcon = configuredDetailIcon;
			detailName = configuredDetailName;
			detailDescription = configuredDetailDescription;
			feedbackText = configuredFeedbackText;
			equipmentPanel = configuredEquipmentPanel;
		}

		public void Initialize(PlayerInventory inventory, PlayerEquipment equipment, ContentDatabase database, IItemUseHandler useHandler, PlayerInputReader input, PlayerMotor motor, PlayerCombatController combat, Damageable health, SceneFlowService sceneFlow)
		{
			ValidateReferences();
			Unbind();
			_inventory = inventory ?? throw new ArgumentNullException("inventory");
			_database = database ?? throw new ArgumentNullException("database");
			_useHandler = useHandler ?? throw new ArgumentNullException("useHandler");
			_input = input ?? throw new ArgumentNullException("input");
			_motor = motor ?? throw new ArgumentNullException("motor");
			_combat = combat ?? throw new ArgumentNullException("combat");
			_health = health ?? throw new ArgumentNullException("health");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			equipmentPanel.Initialize(equipment, inventory, database, () => _selectedIndex, Refresh);
			for (int num = 0; num < slots.Length; num++)
			{
				slots[num].Bind(num, HandlePointerSlot, SelectSlot, HandleSubmitSlot, HandleBeginDrag, HandleDrag, HandleEndDrag, HandleDrop);
			}
			openButton.onClick.RemoveAllListeners();
			useButton.onClick.RemoveAllListeners();
			closeButton.onClick.RemoveAllListeners();
			openButton.onClick.AddListener(Open);
			useButton.onClick.AddListener(UseSelected);
			closeButton.onClick.AddListener(Close);
			_inventory.Changed += HandleInventoryChanged;
			_inventory.Feedback += HandleFeedback;
			_input.InventoryTogglePressed += Toggle;
			_input.InventoryClosePressed += HandleCloseInput;
			_input.InventoryUsePressed += HandleUseInput;
			_input.InventoryNavigate += HandleNavigateInput;
			_sceneFlow.InputBlockedChanged += HandleSceneInputBlocked;
			_health.Died += HandlePlayerDied;
			_initialized = true;
			panelRoot.SetActive(value: false);
			openButton.gameObject.SetActive(value: true);
			SelectFirstOccupiedSlot();
			Refresh();
		}

		public void Open()
		{
			if (_initialized && !IsOpen && !_sceneFlow.IsInputBlocked && _input.GameplayEnabled && _health.IsAlive)
			{
				IsOpen = true;
				_lastPointerSelected = -1;
				_combat.CancelForSceneTransition();
				_motor.CancelTransientMovement();
				_motor.SetMovementEnabled(enabled: false);
				_input.SetGameplayEnabled(enabled: false);
				panelRoot.SetActive(value: true);
				openButton.gameObject.SetActive(value: false);
				SelectFirstOccupiedSlot();
				Refresh();
				EventSystem.current?.SetSelectedGameObject(slots[_selectedIndex].gameObject);
				this.VisibilityChanged?.Invoke(obj: true);
			}
		}

		public void Close()
		{
			CloseInternal(restoreGameplay: true);
		}

		public void UseSelected()
		{
			TryUseSelected();
		}

		public bool TryUseSelected()
		{
			if (!IsOpen || !_initialized)
			{
				return false;
			}
			return _inventory.Use(_selectedIndex, _useHandler);
		}

		public void SelectSlot(int index)
		{
			if (index >= 0 && index < slots.Length)
			{
				_selectedIndex = index;
				Refresh();
			}
		}

		private void Toggle()
		{
			if (IsOpen)
			{
				Close();
			}
			else
			{
				Open();
			}
		}

		private void HandleCloseInput()
		{
			if (IsOpen)
			{
				Close();
			}
		}

		private void HandleUseInput()
		{
			if (IsOpen)
			{
				TryUseSelected();
			}
		}

		private void HandleNavigateInput(Vector2 direction)
		{
			if (IsOpen && !(direction.sqrMagnitude < 0.25f))
			{
				int num = _selectedIndex / 4;
				int num2 = _selectedIndex % 4;
				if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
				{
					num2 = Mathf.Clamp(num2 + ((direction.x > 0f) ? 1 : (-1)), 0, 3);
				}
				else
				{
					num = Mathf.Clamp(num + ((!(direction.y > 0f)) ? 1 : (-1)), 0, 3);
				}
				SelectSlot(num * 4 + num2);
				EventSystem.current?.SetSelectedGameObject(slots[_selectedIndex].gameObject);
			}
		}

		private void HandlePointerSlot(int index)
		{
			if (_lastPointerSelected == index)
			{
				_selectedIndex = index;
				TryUseSelected();
			}
			else
			{
				_lastPointerSelected = index;
				SelectSlot(index);
			}
		}

		private void HandleSubmitSlot(int index)
		{
			SelectSlot(index);
			TryUseSelected();
		}

		private void HandleBeginDrag(int index, PointerEventData eventData)
		{
			if (IsOpen && _inventory.TryGetSlot(index, out var stack) && !stack.IsEmpty)
			{
				ClearDrag();
				_dragSourceIndex = index;
				_selectedIndex = index;
				_database.TryGetItem(stack.ItemId, out var value);
				Canvas componentInParent = GetComponentInParent<Canvas>();
				if (componentInParent != null)
				{
					GameObject gameObject = new GameObject("InventoryDragPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
					gameObject.transform.SetParent(componentInParent.transform, worldPositionStays: false);
					gameObject.transform.SetAsLastSibling();
					_dragPreview = gameObject.GetComponent<Image>();
					_dragPreview.sprite = ((value != null) ? value.Icon : null);
					_dragPreview.color = new Color(1f, 1f, 1f, 0.9f);
					_dragPreview.preserveAspect = true;
					_dragPreview.raycastTarget = false;
					_dragPreview.rectTransform.sizeDelta = new Vector2(78f, 78f);
					gameObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
					PositionDragPreview(eventData);
				}
				Refresh();
			}
		}

		private void HandleDrag(PointerEventData eventData)
		{
			if (IsDragging)
			{
				PositionDragPreview(eventData);
			}
		}

		private void HandleDrop(int targetIndex)
		{
			if (IsDragging && targetIndex != _dragSourceIndex && _inventory.TryGetSlot(_dragSourceIndex, out var stack))
			{
				_inventory.TryGetSlot(targetIndex, out var stack2);
				bool flag = _inventory.Move(_dragSourceIndex, targetIndex);
				if (!flag && !stack2.IsEmpty && !string.Equals(stack.ItemId, stack2.ItemId, StringComparison.Ordinal))
				{
					flag = _inventory.Swap(_dragSourceIndex, targetIndex);
				}
				if (flag)
				{
					_selectedIndex = targetIndex;
					_lastPointerSelected = -1;
				}
			}
		}

		private void HandleEndDrag(PointerEventData eventData)
		{
			ClearDrag();
			Refresh();
		}

		private void PositionDragPreview(PointerEventData eventData)
		{
			if (_dragPreview != null && eventData != null)
			{
				_dragPreview.rectTransform.position = eventData.position;
			}
		}

		private void ClearDrag()
		{
			_dragSourceIndex = -1;
			if (_dragPreview != null)
			{
				UnityEngine.Object.Destroy(_dragPreview.gameObject);
			}
			_dragPreview = null;
		}

		private void SelectFirstOccupiedSlot()
		{
			_selectedIndex = 0;
			for (int i = 0; i < 16; i++)
			{
				if (_inventory.TryGetSlot(i, out var stack) && !stack.IsEmpty)
				{
					_selectedIndex = i;
					break;
				}
			}
		}

		private void HandleInventoryChanged(InventoryChange change)
		{
			Refresh();
		}

		private void HandleFeedback(InventoryFeedback feedback)
		{
			Text text = feedbackText;
			InventoryFeedbackType type = feedback.Type;
			if (1 == 0)
			{
			}
			string text2 = type switch
			{
				InventoryFeedbackType.InventoryFull => "Inventar voll", 
				InventoryFeedbackType.ItemAdded => "Item aufgenommen", 
				InventoryFeedbackType.ItemUsed => "Item verwendet", 
				InventoryFeedbackType.UseNotPossible => "Verwendung nicht möglich", 
				InventoryFeedbackType.NotEnoughItems => "Nicht genug Menge", 
				InventoryFeedbackType.UnknownItemId => "Unbekannte Item-ID", 
				_ => string.Empty, 
			};
			if (1 == 0)
			{
			}
			text.text = text2;
		}

		private void HandleSceneInputBlocked(bool blocked)
		{
			if (blocked && IsOpen)
			{
				CloseInternal(restoreGameplay: false);
			}
		}

		private void HandlePlayerDied()
		{
			if (IsOpen)
			{
				CloseInternal(restoreGameplay: false);
			}
		}

		private void CloseInternal(bool restoreGameplay)
		{
			if (IsOpen)
			{
				IsOpen = false;
				ClearDrag();
				panelRoot.SetActive(value: false);
				openButton.gameObject.SetActive(value: true);
				EventSystem.current?.SetSelectedGameObject(null);
				if (restoreGameplay && !_sceneFlow.IsInputBlocked && _health.IsAlive)
				{
					_input.SetGameplayEnabled(enabled: true);
					_motor.SetMovementEnabled(enabled: true);
				}
				this.VisibilityChanged?.Invoke(obj: false);
			}
		}

		private void Refresh()
		{
			if (_inventory == null)
			{
				return;
			}
			for (int i = 0; i < slots.Length; i++)
			{
				_inventory.TryGetSlot(i, out var stack);
				ItemDefinition value = null;
				if (!stack.IsEmpty)
				{
					_database.TryGetItem(stack.ItemId, out value);
				}
				slots[i].Render(stack, value, i == _selectedIndex);
			}
			_inventory.TryGetSlot(_selectedIndex, out var stack2);
			ItemDefinition value2 = null;
			if (!stack2.IsEmpty)
			{
				_database.TryGetItem(stack2.ItemId, out value2);
			}
			bool flag = value2 != null;
			detailIcon.enabled = flag && value2.Icon != null;
			detailIcon.sprite = (detailIcon.enabled ? value2.Icon : null);
			detailName.text = (flag ? value2.DisplayName : "Leerer Platz");
			detailDescription.text = (flag ? (value2.Description + EquipmentComparisonFormatter.Format(stack2, value2, _database)) : "Wähle einen belegten Inventarplatz.");
			useButton.interactable = flag && value2.CanBeUsed;
			equipmentPanel.Refresh();
		}

		private void ValidateReferences()
		{
			if (panelRoot == null || openButton == null || useButton == null || closeButton == null || detailIcon == null || detailName == null || detailDescription == null || feedbackText == null || equipmentPanel == null || slots == null || slots.Length != 16)
			{
				throw new InvalidOperationException("InventoryWindow requires its prefab references and " + $"exactly {16} slots.");
			}
		}

		private void Unbind()
		{
			if (_inventory != null)
			{
				_inventory.Changed -= HandleInventoryChanged;
				_inventory.Feedback -= HandleFeedback;
			}
			if (_input != null)
			{
				_input.InventoryTogglePressed -= Toggle;
				_input.InventoryClosePressed -= HandleCloseInput;
				_input.InventoryUsePressed -= HandleUseInput;
				_input.InventoryNavigate -= HandleNavigateInput;
			}
			if (_sceneFlow != null)
			{
				_sceneFlow.InputBlockedChanged -= HandleSceneInputBlocked;
			}
			if (_health != null)
			{
				_health.Died -= HandlePlayerDied;
			}
		}

		private void OnDestroy()
		{
			if (IsOpen && _input != null && _motor != null && (_health == null || _health.IsAlive) && (_sceneFlow == null || !_sceneFlow.IsInputBlocked))
			{
				_input.SetGameplayEnabled(enabled: true);
				_motor.SetMovementEnabled(enabled: true);
			}
			Unbind();
		}
	}
}
