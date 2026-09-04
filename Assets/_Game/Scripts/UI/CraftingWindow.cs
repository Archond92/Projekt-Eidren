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
	public sealed class CraftingWindow : MonoBehaviour
	{
		[SerializeField]
		private GameObject panelRoot;

		[SerializeField]
		private CraftingRecipeButtonView[] recipeViews;

		[SerializeField]
		private CraftingIngredientView[] ingredientViews;

		[SerializeField]
		private Image detailIcon;

		[SerializeField]
		private Text detailName;

		[SerializeField]
		private Text detailDescription;

		[SerializeField]
		private Text resultText;

		[SerializeField]
		private Text stationText;

		[SerializeField]
		private Text feedbackText;

		[SerializeField]
		private Button craftButton;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private ScrollRect recipeScroll;

		private CraftingService _service;

		private ContentDatabase _database;

		private PlayerInventory _inventory;

		private PlayerInputReader _input;

		private PlayerMotor _motor;

		private PlayerCombatController _combat;

		private Damageable _health;

		private SceneFlowService _sceneFlow;

		private SaveGameService _saveService;

		private CraftingRecipeDefinition[] _catalogRecipes = Array.Empty<CraftingRecipeDefinition>();

		private CraftingRecipeDefinition[] _recipes = Array.Empty<CraftingRecipeDefinition>();

		private CraftingStationType _station;

		private int _selectedIndex;

		private bool _initialized;

		private bool _requestInProgress;

		private WorkbenchController _stationTarget;

		public bool IsOpen { get; private set; }

		public int SelectedIndex => _selectedIndex;

		public Button CraftButton => craftButton;

		public Button CloseButton => closeButton;

		public string FeedbackText => feedbackText.text;

		public CraftingStationType CurrentStation => _station;

		public IReadOnlyList<CraftingRecipeDefinition> VisibleRecipes => _recipes;

		public IReadOnlyList<CraftingRecipeButtonView> RecipeViews => recipeViews;

		public event Action<bool> VisibilityChanged;

		public event Action<CraftingResult> CraftResultPresented;

		public void ConfigureReferences(GameObject configuredPanelRoot, CraftingRecipeButtonView[] configuredRecipeViews, CraftingIngredientView[] configuredIngredientViews, Image configuredDetailIcon, Text configuredDetailName, Text configuredDetailDescription, Text configuredResultText, Text configuredStationText, Text configuredFeedbackText, Button configuredCraftButton, Button configuredCloseButton, ScrollRect configuredRecipeScroll)
		{
			panelRoot = configuredPanelRoot;
			recipeViews = configuredRecipeViews;
			ingredientViews = configuredIngredientViews;
			detailIcon = configuredDetailIcon;
			detailName = configuredDetailName;
			detailDescription = configuredDetailDescription;
			resultText = configuredResultText;
			stationText = configuredStationText;
			feedbackText = configuredFeedbackText;
			craftButton = configuredCraftButton;
			closeButton = configuredCloseButton;
			recipeScroll = configuredRecipeScroll;
		}

		public void Initialize(CraftingService service, ContentDatabase database, PlayerInventory inventory, PlayerInputReader input, PlayerMotor motor, PlayerCombatController combat, Damageable health, SceneFlowService sceneFlow, SaveGameService saveService = null)
		{
			ValidateReferences();
			Unbind();
			_service = service ?? throw new ArgumentNullException("service");
			_database = database ?? throw new ArgumentNullException("database");
			_inventory = inventory ?? throw new ArgumentNullException("inventory");
			_input = input ?? throw new ArgumentNullException("input");
			_motor = motor ?? throw new ArgumentNullException("motor");
			_combat = combat ?? throw new ArgumentNullException("combat");
			_health = health ?? throw new ArgumentNullException("health");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_saveService = saveService;
			_catalogRecipes = _database.GetCraftingRecipes();
			if (_catalogRecipes.Length > recipeViews.Length)
			{
				throw new InvalidOperationException("CraftingWindow has fewer recipe views than the configured recipe catalog.");
			}
			for (int i = 0; i < recipeViews.Length; i++)
			{
				recipeViews[i].Bind(i, SelectRecipe);
			}
			craftButton.onClick.RemoveAllListeners();
			closeButton.onClick.RemoveAllListeners();
			craftButton.onClick.AddListener(HandleCraftButton);
			closeButton.onClick.AddListener(Close);
			_inventory.Changed += HandleInventoryChanged;
			_service.CraftCompleted += HandleCraftCompleted;
			_input.InventoryClosePressed += HandleCloseInput;
			_input.InventoryUsePressed += HandleCraftInput;
			_input.InventoryNavigate += HandleNavigateInput;
			_input.CraftingTogglePressed += HandleCraftingToggle;
			_sceneFlow.InputBlockedChanged += HandleSceneInputBlocked;
			_sceneFlow.SceneTransitionStarted += HandleSceneTransitionStarted;
			_health.Died += HandlePlayerDied;
			_initialized = true;
			panelRoot.SetActive(value: false);
			Refresh();
		}

		public void Open(CraftingStationType station)
		{
			OpenInternal(station, null);
		}

		public void Open(WorkbenchController station)
		{
			if (!(station == null))
			{
				OpenInternal(station.Station, station);
			}
		}

		private void OpenInternal(CraftingStationType station, WorkbenchController stationTarget)
		{
			if (_initialized && !IsOpen && !_sceneFlow.IsInputBlocked && _input.GameplayEnabled && _health.IsAlive)
			{
				_station = station;
				_stationTarget = stationTarget;
				BuildVisibleRecipes(station);
				_selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _recipes.Length - 1));
				IsOpen = true;
				feedbackText.text = string.Empty;
				_combat.CancelForSceneTransition();
				_motor.CancelTransientMovement();
				_motor.SetMovementEnabled(enabled: false);
				_input.SetGameplayEnabled(enabled: false);
				panelRoot.SetActive(value: true);
				Refresh();
				if (_recipes.Length != 0)
				{
					EventSystem.current?.SetSelectedGameObject(recipeViews[_selectedIndex].gameObject);
				}
				this.VisibilityChanged?.Invoke(obj: true);
			}
		}

		private void Update()
		{
			if (IsOpen && !(_stationTarget == null))
			{
				float num = InteractionUtility.FlatDistanceToBody(_motor.transform.position, _stationTarget);
				if (num > _stationTarget.InteractionRange + 0.25f)
				{
					CloseInternal(restoreGameplay: true);
				}
			}
		}

		public void Close()
		{
			CloseInternal(restoreGameplay: true);
		}

		public void SelectRecipe(int index)
		{
			if (index >= 0 && index < _recipes.Length)
			{
				_selectedIndex = index;
				feedbackText.text = string.Empty;
				Refresh();
				ScrollToSelected();
			}
		}

		public CraftingResult CraftSelected()
		{
			if (!IsOpen || _requestInProgress || _recipes.Length == 0)
			{
				return new CraftingResult(CraftingResultCode.Busy, string.Empty);
			}
			_requestInProgress = true;
			craftButton.interactable = false;
			try
			{
				CraftingRecipeDefinition craftingRecipeDefinition = _recipes[_selectedIndex];
				CraftingResult result = _service.TryCraft(craftingRecipeDefinition.Id, _station);
				if (result.Code == CraftingResultCode.Busy)
				{
					ShowFeedback(result.Code);
				}
				return result;
			}
			finally
			{
				_requestInProgress = false;
				Refresh();
			}
		}

		private void HandleCraftButton()
		{
			CraftSelected();
		}

		private void HandleCraftCompleted(CraftingResult result)
		{
			this.CraftResultPresented?.Invoke(result);
			ShowFeedback(result.Code);
			if (result.Succeeded)
			{
				_saveService?.SaveNow(SaveRequestReason.CraftingCompleted, out var _);
			}
			Refresh();
		}

		private void ShowFeedback(CraftingResultCode code)
		{
			Text text = feedbackText;
			if (1 == 0)
			{
			}
			string text2 = code switch
			{
				CraftingResultCode.Success => "Erfolgreich hergestellt", 
				CraftingResultCode.MissingIngredients => "Zutaten fehlen", 
				CraftingResultCode.InventoryFull => "Inventar voll", 
				CraftingResultCode.UpgradeAlreadyOwned => "Upgrade bereits vorhanden", 
				CraftingResultCode.WrongStation => "Falsche Station", 
				CraftingResultCode.UnknownRecipe => "Unbekanntes Rezept", 
				CraftingResultCode.RecipeLocked => "Rezept nicht freigeschaltet", 
				CraftingResultCode.Busy => "Herstellung läuft bereits", 
				_ => "Rezept ist ungültig", 
			};
			if (1 == 0)
			{
			}
			text.text = text2;
		}

		private void HandleCloseInput()
		{
			if (IsOpen)
			{
				Close();
			}
		}

		private void HandleCraftingToggle()
		{
			if (IsOpen)
			{
				Close();
			}
			else
			{
				Open(CraftingStationType.None);
			}
		}

		private void HandleCraftInput()
		{
			if (IsOpen)
			{
				CraftSelected();
			}
		}

		private void HandleNavigateInput(Vector2 direction)
		{
			if (IsOpen && _recipes.Length != 0 && !(Mathf.Abs(direction.y) < 0.25f))
			{
				int num = ((!(direction.y > 0f)) ? 1 : (-1));
				SelectRecipe(Mathf.Clamp(_selectedIndex + num, 0, _recipes.Length - 1));
				EventSystem.current?.SetSelectedGameObject(recipeViews[_selectedIndex].gameObject);
			}
		}

		private void HandleInventoryChanged(InventoryChange change)
		{
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
			if (!_initialized)
			{
				return;
			}
			for (int i = 0; i < recipeViews.Length; i++)
			{
				CraftingRecipeDefinition craftingRecipeDefinition = ((i < _recipes.Length) ? _recipes[i] : null);
				string state = string.Empty;
				bool locked = false;
				if (craftingRecipeDefinition != null)
				{
					CraftingResultCode code = _service.Evaluate(craftingRecipeDefinition, _station).Code;
					state = StateLabel(code);
					locked = code == CraftingResultCode.RecipeLocked;
				}
				recipeViews[i].Render(craftingRecipeDefinition, i == _selectedIndex, state, locked);
			}
			if (_recipes.Length == 0)
			{
				craftButton.interactable = false;
				return;
			}
			CraftingRecipeDefinition craftingRecipeDefinition2 = _recipes[_selectedIndex];
			detailIcon.sprite = craftingRecipeDefinition2.Icon;
			detailIcon.enabled = craftingRecipeDefinition2.Icon != null;
			detailName.text = craftingRecipeDefinition2.DisplayName;
			detailDescription.text = craftingRecipeDefinition2.Description;
			stationText.text = "Station: " + StationLabel(craftingRecipeDefinition2.RequiredStation);
			resultText.text = ResultLabel(craftingRecipeDefinition2);
			for (int j = 0; j < ingredientViews.Length; j++)
			{
				if (j >= craftingRecipeDefinition2.Ingredients.Count)
				{
					ingredientViews[j].gameObject.SetActive(value: false);
					continue;
				}
				CraftingIngredient craftingIngredient = craftingRecipeDefinition2.Ingredients[j];
				_database.TryGetItem(craftingIngredient.ItemId, out var value);
				ingredientViews[j].Render(value, _service.GetAvailableAmount(craftingIngredient.ItemId), craftingIngredient.Amount);
			}
			CraftingResult craftingResult = _service.Evaluate(craftingRecipeDefinition2, _station);
			craftButton.interactable = !_requestInProgress && craftingResult.Succeeded;
		}

		private string ResultLabel(CraftingRecipeDefinition recipe)
		{
			if (recipe.ResultType == CraftingResultType.WeaponUpgrade)
			{
				return "Ergebnis: Hammer Stufe 2\n+15 % HP-Schaden · +10 % Stagger";
			}
			ItemDefinition value;
			return _database.TryGetItem(recipe.ResultItemId, out value) ? $"Ergebnis: {recipe.ResultAmount}× {value.DisplayName}" : "Ergebnis: Unbekannt";
		}

		private static string StationLabel(CraftingStationType station)
		{
			if (1 == 0)
			{
			}
			string result = station switch
			{
				CraftingStationType.Workbench => "Werkbank", 
				CraftingStationType.Smelter => "Schmelzofen", 
				CraftingStationType.Sawmill => "Sägewerk", 
				CraftingStationType.Ropewalk => "Seilerei", 
				CraftingStationType.Stonecutter => "Steinmetz", 
				CraftingStationType.CookingPot => "Kochtopf", 
				_ => "Handarbeit / Werkbank", 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private void BuildVisibleRecipes(CraftingStationType station)
		{
			List<CraftingRecipeDefinition> list = new List<CraftingRecipeDefinition>(_catalogRecipes.Length);
			CraftingRecipeDefinition[] catalogRecipes = _catalogRecipes;
			foreach (CraftingRecipeDefinition craftingRecipeDefinition in catalogRecipes)
			{
				if (craftingRecipeDefinition != null && CraftingService.IsStationCompatible(craftingRecipeDefinition.RequiredStation, station))
				{
					list.Add(craftingRecipeDefinition);
				}
			}
			_recipes = list.ToArray();
		}

		private void ScrollToSelected()
		{
			if (!(recipeScroll == null) && _recipes.Length > 1)
			{
				recipeScroll.verticalNormalizedPosition = 1f - (float)_selectedIndex / (float)(_recipes.Length - 1);
			}
		}

		private static string StateLabel(CraftingResultCode code)
		{
			if (1 == 0)
			{
			}
			string result = code switch
			{
				CraftingResultCode.Success => "Bereit", 
				CraftingResultCode.MissingIngredients => "Zutaten fehlen", 
				CraftingResultCode.InventoryFull => "Inventar voll", 
				CraftingResultCode.UpgradeAlreadyOwned => "Hergestellt", 
				CraftingResultCode.WrongStation => "Falsche Station", 
				CraftingResultCode.RecipeLocked => "Gesperrt", 
				_ => "Nicht verfügbar", 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private void CloseInternal(bool restoreGameplay)
		{
			if (IsOpen)
			{
				IsOpen = false;
				panelRoot.SetActive(value: false);
				_stationTarget = null;
				EventSystem.current?.SetSelectedGameObject(null);
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
			if (panelRoot == null || recipeViews == null || recipeViews.Length < 3 || ingredientViews == null || ingredientViews.Length < 5 || detailIcon == null || detailName == null || detailDescription == null || resultText == null || stationText == null || feedbackText == null || craftButton == null || closeButton == null || recipeScroll == null)
			{
				throw new InvalidOperationException("CraftingWindow prefab references are incomplete.");
			}
		}

		private void Unbind()
		{
			if (_inventory != null)
			{
				_inventory.Changed -= HandleInventoryChanged;
			}
			if (_service != null)
			{
				_service.CraftCompleted -= HandleCraftCompleted;
			}
			if (_input != null)
			{
				_input.InventoryClosePressed -= HandleCloseInput;
				_input.InventoryUsePressed -= HandleCraftInput;
				_input.InventoryNavigate -= HandleNavigateInput;
				_input.CraftingTogglePressed -= HandleCraftingToggle;
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
