using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Interaction;
using Eidren.Player;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class StorageWindow : MonoBehaviour
	{
		[SerializeField]
		private GameObject panelRoot;

		[SerializeField]
		private InventorySlotView[] playerSlots;

		[SerializeField]
		private InventorySlotView[] storageSlots;

		[SerializeField]
		private Image detailIcon;

		[SerializeField]
		private Text detailName;

		[SerializeField]
		private Text amountText;

		[SerializeField]
		private Text feedbackText;

		[SerializeField]
		private Button transferButton;

		[SerializeField]
		private Button takeAllButton;

		[SerializeField]
		private Button depositMatchingButton;

		[SerializeField]
		private Button closeButton;

		private ItemTransferService _transferService;

		private PlayerInventory _inventory;

		private ContentDatabase _database;

		private PlayerInputReader _input;

		private PlayerMotor _motor;

		private PlayerCombatController _combat;

		private Damageable _health;

		private SceneFlowService _sceneFlow;

		private SaveGameService _saveService;

		private IInspectableContainer _container;

		private int _playerIndex;

		private int _storageIndex;

		private bool _initialized;

		private bool _requestInProgress;

		private bool _storageDirty;

		public bool IsOpen { get; private set; }

		public StorageSelectionSide SelectedSide { get; private set; }

		public int SelectedIndex => (SelectedSide == StorageSelectionSide.Player) ? _playerIndex : _storageIndex;

		public IReadOnlyList<InventorySlotView> PlayerSlots => playerSlots;

		public IReadOnlyList<InventorySlotView> StorageSlots => storageSlots;

		public Button TransferButton => transferButton;

		public Button TakeAllButton => takeAllButton;

		public Button DepositMatchingButton => depositMatchingButton;

		public Button CloseButton => closeButton;

		public string FeedbackText => feedbackText.text;

		public event Action<bool> VisibilityChanged;

		public void ConfigureReferences(GameObject configuredPanelRoot, InventorySlotView[] configuredPlayerSlots, InventorySlotView[] configuredStorageSlots, Image configuredDetailIcon, Text configuredDetailName, Text configuredAmountText, Text configuredFeedbackText, Button configuredTransferButton, Button configuredTakeAllButton, Button configuredCloseButton, Button configuredDepositMatchingButton = null)
		{
			depositMatchingButton = configuredDepositMatchingButton;
			panelRoot = configuredPanelRoot;
			playerSlots = configuredPlayerSlots;
			storageSlots = configuredStorageSlots;
			detailIcon = configuredDetailIcon;
			detailName = configuredDetailName;
			amountText = configuredAmountText;
			feedbackText = configuredFeedbackText;
			transferButton = configuredTransferButton;
			takeAllButton = configuredTakeAllButton;
			closeButton = configuredCloseButton;
		}

		public void Initialize(ItemTransferService transferService, PlayerInventory inventory, ContentDatabase database, PlayerInputReader input, PlayerMotor motor, PlayerCombatController combat, Damageable health, SceneFlowService sceneFlow, SaveGameService saveService = null)
		{
			ValidateReferences();
			Unbind();
			_transferService = transferService ?? throw new ArgumentNullException("transferService");
			_inventory = inventory ?? throw new ArgumentNullException("inventory");
			_database = database ?? throw new ArgumentNullException("database");
			_input = input ?? throw new ArgumentNullException("input");
			_motor = motor ?? throw new ArgumentNullException("motor");
			_combat = combat ?? throw new ArgumentNullException("combat");
			_health = health ?? throw new ArgumentNullException("health");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_saveService = saveService;
			for (int i = 0; i < playerSlots.Length; i++)
			{
				playerSlots[i].Bind(i, SelectPlayerSlot, SelectPlayerSlot, SelectPlayerSlot);
			}
			for (int j = 0; j < storageSlots.Length; j++)
			{
				storageSlots[j].Bind(j, SelectStorageSlot, SelectStorageSlot, SelectStorageSlot);
			}
			transferButton.onClick.RemoveAllListeners();
			takeAllButton.onClick.RemoveAllListeners();
			closeButton.onClick.RemoveAllListeners();
			transferButton.onClick.AddListener(HandleTransferButton);
			takeAllButton.onClick.AddListener(TakeAll);
			closeButton.onClick.AddListener(Close);
			if (depositMatchingButton != null)
			{
				depositMatchingButton.onClick.RemoveAllListeners();
				depositMatchingButton.onClick.AddListener(DepositMatchingSelected);
			}
			_inventory.Changed += HandleInventoryChanged;
			_transferService.TransferCompleted += HandleTransferCompleted;
			_input.InventoryClosePressed += HandleCloseInput;
			_input.InventoryUsePressed += HandleTransferInput;
			_input.InventoryNavigate += HandleNavigateInput;
			_sceneFlow.InputBlockedChanged += HandleSceneInputBlocked;
			_sceneFlow.SceneTransitionStarted += HandleSceneTransitionStarted;
			_health.Died += HandlePlayerDied;
			_initialized = true;
			panelRoot.SetActive(value: false);
			Refresh();
		}

		public void Open(IInspectableContainer container)
		{
			if (_initialized && !IsOpen && container != null && !_sceneFlow.IsInputBlocked && _input.GameplayEnabled && _health.IsAlive)
			{
				if (_container != null)
				{
					_container.Changed -= HandleStorageChanged;
				}
				_container = container;
				_container.Changed += HandleStorageChanged;
				SelectedSide = StorageSelectionSide.Player;
				_playerIndex = FirstOccupied(_inventory, 16);
				_storageIndex = FirstOccupied(_container, _container.SlotCapacity);
				IsOpen = true;
				_storageDirty = false;
				feedbackText.text = container.ContainerDisplayName;
				_combat.CancelForSceneTransition();
				_motor.CancelTransientMovement();
				_motor.SetMovementEnabled(enabled: false);
				_input.SetGameplayEnabled(enabled: false);
				panelRoot.SetActive(value: true);
				Refresh();
				FocusSelected();
				this.VisibilityChanged?.Invoke(obj: true);
			}
		}

		public void Close()
		{
			CloseInternal(restoreGameplay: true);
		}

		public ItemTransferResult TransferSelected()
		{
			if (!IsOpen || _container == null || _requestInProgress)
			{
				return new ItemTransferResult(ItemTransferResultCode.Busy, string.Empty, 0, 0);
			}
			_requestInProgress = true;
			transferButton.interactable = false;
			try
			{
				return (SelectedSide == StorageSelectionSide.Player) ? _transferService.Transfer(_inventory, _playerIndex, _container) : _transferService.Transfer(_container, _storageIndex, _inventory);
			}
			finally
			{
				_requestInProgress = false;
				Refresh();
			}
		}

		/// <summary>
		/// Sammelentnahme gilt nur für echte Beutequellen (Gebietskisten, Gegnerloot),
		/// nicht für dauerhafte Heimatlager.
		/// </summary>
		public static bool AllowsTakeAll(IInspectableContainer container)
		{
			// Nach-Release-Fix (18.08.2026): Auch die Verlieskisten der
			// Eidra-Schmiede sind einmalige Fundbehälter, kein Heimatlager.
			return container is WorldChestContainer || container is EnemyLootContainer || container is EidraForgeChestContainer;
		}

		/// <summary>
		/// Die Sammelablage ist das Gegenstück und gilt ausschließlich für die
		/// dauerhaften Lagerkisten der Heimatbasis.
		/// </summary>
		public static bool AllowsDepositMatching(IInspectableContainer container)
		{
			return container is StorageContainer;
		}

		/// <summary>
		/// Legt aus dem Rucksack alle Gegenstandsarten ab, die zu Beginn der
		/// Aktion bereits im Ziel liegen. Die Auswahl wird einmal vorab bestimmt:
		/// Eine Art, die erst währenddessen Platz findet, rückt nicht nach.
		/// Liefert die Anzahl tatsächlich übertragener Gegenstände.
		/// </summary>
		public static int DepositMatching(ItemTransferService transferService, IItemContainer backpack, IItemContainer storage)
		{
			if (transferService == null || backpack == null || storage == null)
			{
				return 0;
			}
			HashSet<string> allowed = SnapshotItemIds(storage);
			if (allowed.Count == 0)
			{
				return 0;
			}
			int moved = 0;
			for (int index = 0; index < backpack.SlotCapacity; index++)
			{
				if (backpack.TryGetSlot(index, out var stack) && !stack.IsEmpty && allowed.Contains(stack.ItemId))
				{
					// Kein Abbruch bei vollem Ziel: Eine andere Art kann weiterhin
					// einen angebrochenen Stapel auffüllen.
					moved += transferService.Transfer(backpack, index, storage).Transferred;
				}
			}
			return moved;
		}

		private static HashSet<string> SnapshotItemIds(IItemContainer container)
		{
			HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
			for (int index = 0; index < container.SlotCapacity; index++)
			{
				if (container.TryGetSlot(index, out var stack) && !stack.IsEmpty)
				{
					ids.Add(stack.ItemId);
				}
			}
			return ids;
		}

		public void DepositMatchingSelected()
		{
			if (!IsOpen || _requestInProgress || !AllowsDepositMatching(_container))
			{
				return;
			}
			_requestInProgress = true;
			int moved = 0;
			try
			{
				moved = DepositMatching(_transferService, _inventory, _container);
			}
			finally
			{
				_requestInProgress = false;
				feedbackText.text = ((moved > 0) ? $"{moved} abgelegt" : "Nichts passte in die Kiste");
				Refresh();
			}
		}

		public void TakeAll()
		{
			if (!IsOpen || _requestInProgress || !AllowsTakeAll(_container))
			{
				return;
			}
			_requestInProgress = true;
			int num = 0;
			try
			{
				for (int i = 0; i < _container.SlotCapacity; i++)
				{
					ItemTransferResult itemTransferResult = _transferService.Transfer(_container, i, _inventory);
					num += itemTransferResult.Transferred;
					if (itemTransferResult.Code == ItemTransferResultCode.TargetFull)
					{
						break;
					}
				}
			}
			finally
			{
				_requestInProgress = false;
				feedbackText.text = ((num > 0) ? $"{num} Gegenstände genommen" : "Nichts konnte genommen werden");
				Refresh();
			}
		}

		public void SelectPlayerSlot(int index)
		{
			if (index >= 0 && index < playerSlots.Length)
			{
				SelectedSide = StorageSelectionSide.Player;
				_playerIndex = index;
				feedbackText.text = string.Empty;
				Refresh();
			}
		}

		public void SelectStorageSlot(int index)
		{
			if (index >= 0 && index < storageSlots.Length)
			{
				SelectedSide = StorageSelectionSide.Storage;
				_storageIndex = index;
				feedbackText.text = string.Empty;
				Refresh();
			}
		}

		private void Update()
		{
			if (IsOpen && _container != null)
			{
				float num = InteractionUtility.FlatDistanceToBody(_motor.transform.position, _container);
				if (num > _container.InteractionRange + 0.25f)
				{
					CloseInternal(restoreGameplay: true);
				}
			}
		}

		private void HandleTransferButton()
		{
			TransferSelected();
		}

		private void HandleTransferCompleted(ItemTransferResult result)
		{
			Text text = feedbackText;
			ItemTransferResultCode code = result.Code;
			if (1 == 0)
			{
			}
			string text2 = code switch
			{
				ItemTransferResultCode.Success => $"{result.Transferred} übertragen", 
				ItemTransferResultCode.Partial => $"{result.Transferred} übertragen, " + $"{result.Remainder} verbleiben", 
				ItemTransferResultCode.TargetFull => "Zielinventar voll", 
				ItemTransferResultCode.SourceEmpty => "Kein Gegenstand ausgewählt", 
				ItemTransferResultCode.Busy => "Transfer läuft bereits", 
				_ => "Transfer nicht möglich", 
			};
			if (1 == 0)
			{
			}
			text.text = text2;
			Refresh();
		}

		private void HandleCloseInput()
		{
			if (IsOpen)
			{
				Close();
			}
		}

		private void HandleTransferInput()
		{
			if (IsOpen)
			{
				TransferSelected();
			}
		}

		private void HandleNavigateInput(Vector2 direction)
		{
			if (!IsOpen || direction.sqrMagnitude < 0.25f)
			{
				return;
			}
			bool flag = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
			int selectedIndex = SelectedIndex;
			int num = selectedIndex / 4;
			int num2 = selectedIndex % 4;
			if (flag)
			{
				int num3 = ((direction.x > 0f) ? 1 : (-1));
				if (SelectedSide == StorageSelectionSide.Player && num2 == 3 && num3 > 0)
				{
					SelectedSide = StorageSelectionSide.Storage;
					_storageIndex = Mathf.Min(num * 4, storageSlots.Length - 1);
				}
				else if (SelectedSide == StorageSelectionSide.Storage && num2 == 0 && num3 < 0)
				{
					SelectedSide = StorageSelectionSide.Player;
					_playerIndex = Mathf.Min(num * 4 + 3, playerSlots.Length - 1);
				}
				else
				{
					num2 = Mathf.Clamp(num2 + num3, 0, 3);
					SetSelectedIndex(num * 4 + num2);
				}
			}
			else
			{
				int num4 = ((SelectedSide == StorageSelectionSide.Player) ? 4 : 6);
				num = Mathf.Clamp(num + ((!(direction.y > 0f)) ? 1 : (-1)), 0, num4 - 1);
				SetSelectedIndex(num * 4 + num2);
			}
			Refresh();
			FocusSelected();
		}

		private void SetSelectedIndex(int index)
		{
			if (SelectedSide == StorageSelectionSide.Player)
			{
				_playerIndex = Mathf.Clamp(index, 0, 15);
			}
			else
			{
				_storageIndex = Mathf.Clamp(index, 0, 23);
			}
		}

		private void FocusSelected()
		{
			GameObject selectedGameObject = ((SelectedSide == StorageSelectionSide.Player) ? playerSlots[_playerIndex].gameObject : storageSlots[_storageIndex].gameObject);
			EventSystem.current?.SetSelectedGameObject(selectedGameObject);
		}

		private void HandleInventoryChanged(InventoryChange change)
		{
			if (IsOpen)
			{
				Refresh();
			}
		}

		private void HandleStorageChanged(StorageContainerChange change)
		{
			if (IsOpen)
			{
				_storageDirty = true;
			}
			if (IsOpen)
			{
				Refresh();
			}
		}

		private void HandleSceneInputBlocked(bool blocked)
		{
			if (blocked && IsOpen)
			{
				CloseInternal(restoreGameplay: false);
			}
		}

		private void HandleSceneTransitionStarted(string sceneName)
		{
			if (IsOpen)
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

		private void Refresh()
		{
			if (_initialized)
			{
				if (takeAllButton != null)
				{
					takeAllButton.gameObject.SetActive(IsOpen && AllowsTakeAll(_container));
				}
				if (depositMatchingButton != null)
				{
					depositMatchingButton.gameObject.SetActive(IsOpen && AllowsDepositMatching(_container));
				}
				RenderSlots(_inventory, playerSlots, SelectedSide == StorageSelectionSide.Player, _playerIndex);
				RenderSlots(_container, storageSlots, SelectedSide == StorageSelectionSide.Storage, _storageIndex);
				IItemContainer itemContainer;
				if (SelectedSide != StorageSelectionSide.Player)
				{
					IItemContainer container = _container;
					itemContainer = container;
				}
				else
				{
					IItemContainer container = _inventory;
					itemContainer = container;
				}
				IItemContainer itemContainer2 = itemContainer;
				ItemStack stack = ItemStack.Empty;
				bool flag = itemContainer2 != null && itemContainer2.TryGetSlot(SelectedIndex, out stack) && !stack.IsEmpty;
				ItemDefinition value = null;
				if (flag)
				{
					_database.TryGetItem(stack.ItemId, out value);
				}
				detailIcon.enabled = value != null && value.Icon != null;
				detailIcon.sprite = (detailIcon.enabled ? value.Icon : null);
				detailName.text = ((value != null) ? value.DisplayName : "Leerer Platz");
				amountText.text = (flag ? $"Menge: {stack.Quantity}" : "Menge: 0");
				transferButton.interactable = !_requestInProgress && flag;
			}
		}

		private void RenderSlots(IItemContainer container, InventorySlotView[] views, bool activeSide, int selectedIndex)
		{
			for (int i = 0; i < views.Length; i++)
			{
				ItemStack stack = ItemStack.Empty;
				container?.TryGetSlot(i, out stack);
				ItemDefinition value = null;
				if (!stack.IsEmpty)
				{
					_database.TryGetItem(stack.ItemId, out value);
				}
				views[i].Render(stack, value, activeSide && i == selectedIndex);
			}
		}

		private static int FirstOccupied(IItemContainer container, int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (container.TryGetSlot(i, out var stack) && !stack.IsEmpty)
				{
					return i;
				}
			}
			return 0;
		}

		private void CloseInternal(bool restoreGameplay)
		{
			if (IsOpen)
			{
				IsOpen = false;
				panelRoot.SetActive(value: false);
				EventSystem.current?.SetSelectedGameObject(null);
				if (_container != null)
				{
					_container.Changed -= HandleStorageChanged;
				}
				_container = null;
				if (_storageDirty)
				{
					_saveService?.SaveNow(SaveRequestReason.StorageClosed, out var _);
					_storageDirty = false;
				}
				if (restoreGameplay && !_sceneFlow.IsInputBlocked && _health.IsAlive)
				{
					_input.SetGameplayEnabled(enabled: true);
					_motor.SetMovementEnabled(enabled: true);
				}
				this.VisibilityChanged?.Invoke(obj: false);
			}
		}

		private void ValidateReferences()
		{
			if (panelRoot == null || playerSlots == null || playerSlots.Length != 16 || storageSlots == null || storageSlots.Length != 24 || detailIcon == null || detailName == null || amountText == null || feedbackText == null || transferButton == null || takeAllButton == null || closeButton == null)
			{
				throw new InvalidOperationException("StorageWindow prefab references are incomplete.");
			}
		}

		private void Unbind()
		{
			if (_inventory != null)
			{
				_inventory.Changed -= HandleInventoryChanged;
			}
			if (_transferService != null)
			{
				_transferService.TransferCompleted -= HandleTransferCompleted;
			}
			if (_input != null)
			{
				_input.InventoryClosePressed -= HandleCloseInput;
				_input.InventoryUsePressed -= HandleTransferInput;
				_input.InventoryNavigate -= HandleNavigateInput;
			}
			if (_sceneFlow != null)
			{
				_sceneFlow.InputBlockedChanged -= HandleSceneInputBlocked;
				_sceneFlow.SceneTransitionStarted -= HandleSceneTransitionStarted;
			}
			if (_health != null)
			{
				_health.Died -= HandlePlayerDied;
			}
			if (_container != null)
			{
				_container.Changed -= HandleStorageChanged;
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
