using Eidren.Core.Services;
using Eidren.Input;
using System;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class PauseMenuController : MonoBehaviour
	{
		private enum ConfirmationAction
		{
			None = 0,
			HomeBase = 1,
			MainMenu = 2,
			Quit = 3
		}

		[SerializeField]
		private GameObject canvasRoot;

		[SerializeField]
		private Button pauseButton;

		[SerializeField]
		private GameObject pausePanel;

		[SerializeField]
		private Button resumeButton;

		[SerializeField]
		private Button settingsButton;

		[SerializeField]
		private Button homeButton;

		[SerializeField]
		private Button mainMenuButton;

		[SerializeField]
		private Button quitButton;

		[SerializeField]
		private GameObject settingsPanel;

		[SerializeField]
		private Slider masterSlider;

		[SerializeField]
		private Slider musicSlider;

		[SerializeField]
		private Slider sfxSlider;

		[SerializeField]
		private Text masterValue;

		[SerializeField]
		private Text musicValue;

		[SerializeField]
		private Text sfxValue;

		[SerializeField]
		private Dropdown qualityDropdown;

		[SerializeField]
		private Dropdown frameRateDropdown;

		[SerializeField]
		private Button settingsBackButton;

		[SerializeField]
		private GameObject confirmationPanel;

		[SerializeField]
		private Text confirmationText;

		[SerializeField]
		private Text feedbackText;

		[SerializeField]
		private Button confirmButton;

		[SerializeField]
		private Button cancelButton;

		private PauseService _pause;

		private SettingsService _settings;

		private MenuInputService _menuInput;

		private SceneFlowService _sceneFlow;

		private GameSession _session;

		private PlayerPrefabBindings _player;

		private PlayerInputReader _playerInput;

		private MapExitCoordinator _exitCoordinator;

		private ConfirmationAction _confirmation;

		private bool _settingsOpenedFromPause;

		private bool _eventsBound;

		private bool _requestPending;

		public bool IsPauseOpen => pausePanel != null && pausePanel.activeSelf;

		public bool IsSettingsOpen => settingsPanel != null && settingsPanel.activeSelf;

		public bool IsConfirmationOpen => confirmationPanel != null && confirmationPanel.activeSelf;

		public void ConfigureReferences(GameObject root, Button pauseToggle, GameObject pauseWindow, Button resume, Button settings, Button home, Button mainMenu, Button quit, GameObject settingsWindow, Slider master, Slider music, Slider sfx, Text masterLabel, Text musicLabel, Text sfxLabel, Dropdown quality, Dropdown frameRate, Button settingsBack, GameObject confirmationWindow, Text confirmationLabel, Text feedbackLabel, Button confirm, Button cancel)
		{
			canvasRoot = root;
			pauseButton = pauseToggle;
			pausePanel = pauseWindow;
			resumeButton = resume;
			settingsButton = settings;
			homeButton = home;
			mainMenuButton = mainMenu;
			quitButton = quit;
			settingsPanel = settingsWindow;
			masterSlider = master;
			musicSlider = music;
			sfxSlider = sfx;
			masterValue = masterLabel;
			musicValue = musicLabel;
			sfxValue = sfxLabel;
			qualityDropdown = quality;
			frameRateDropdown = frameRate;
			settingsBackButton = settingsBack;
			confirmationPanel = confirmationWindow;
			confirmationText = confirmationLabel;
			feedbackText = feedbackLabel;
			confirmButton = confirm;
			cancelButton = cancel;
		}

		public void Initialize(PauseService pause, SettingsService settings, MenuInputService menuInput, SceneFlowService sceneFlow, GameSession session)
		{
			ValidateReferences();
			Unbind();
			_pause = pause ?? throw new ArgumentNullException("pause");
			_settings = settings ?? throw new ArgumentNullException("settings");
			_menuInput = menuInput ?? throw new ArgumentNullException("menuInput");
			_sceneFlow = sceneFlow ?? throw new ArgumentNullException("sceneFlow");
			_session = session ?? throw new ArgumentNullException("session");
			BindEvents();
			HideAll();
			RefreshPauseButton();
		}

		public void BindGameplay(PlayerPrefabBindings player, PlayerInputReader input, MapExitCoordinator exitCoordinator = null)
		{
			if (_player != null && _player.Damageable != null)
			{
				_player.Damageable.Died -= HandlePlayerDied;
			}
			_player = player;
			_playerInput = input;
			_exitCoordinator = exitCoordinator;
			if (_player != null && _player.Damageable != null)
			{
				_player.Damageable.Died += HandlePlayerDied;
			}
			RefreshPauseButton();
		}

		public void TogglePause()
		{
			if (IsPauseOpen || IsSettingsOpen || IsConfirmationOpen)
			{
				ResumeGameplay();
			}
			else if (!CloseTopGameplayWindow())
			{
				OpenPause();
			}
		}

		public void OpenPause()
		{
			if (!_requestPending && !IsMainMenuScene() && _pause.TryPause())
			{
				CancelPlayerActions();
				pausePanel.SetActive(value: true);
				settingsPanel.SetActive(value: false);
				confirmationPanel.SetActive(value: false);
				feedbackText.text = string.Empty;
				pauseButton.gameObject.SetActive(value: false);
				EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
			}
		}

		public bool PauseForApplicationLifecycle()
		{
			if (_requestPending || IsMainMenuScene() || _pause == null || (!_pause.IsPaused && !_pause.TryPause()))
			{
				return false;
			}
			CancelPlayerActions();
			canvasRoot.SetActive(value: true);
			pausePanel.SetActive(value: true);
			settingsPanel.SetActive(value: false);
			confirmationPanel.SetActive(value: false);
			feedbackText.text = string.Empty;
			pauseButton.gameObject.SetActive(value: false);
			EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
			return true;
		}

		public void ResumeGameplay()
		{
			_confirmation = ConfirmationAction.None;
			_requestPending = false;
			HideAll();
			_pause.Resume();
			RestorePlayerInput();
			RefreshPauseButton();
		}

		public void OpenSettingsFromPause()
		{
			OpenSettings(fromPause: true);
		}

		public void OpenSettingsFromMainMenu()
		{
			if (IsMainMenuScene())
			{
				OpenSettings(fromPause: false);
			}
		}

		public void CloseSettings()
		{
			if (IsSettingsOpen)
			{
				_settings.Save(out var _);
				settingsPanel.SetActive(value: false);
				if (_settingsOpenedFromPause && _pause.IsPaused)
				{
					pausePanel.SetActive(value: true);
					EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
				}
				else
				{
					canvasRoot.SetActive(value: false);
					EventSystem.current?.SetSelectedGameObject(null);
				}
			}
		}

		public void RequestHomeBase()
		{
			ShowConfirmation(ConfirmationAction.HomeBase, "Zur Heimatbasis zurückkehren?\nDer aktuelle Fortschritt wird gespeichert.");
		}

		public void RequestMainMenu()
		{
			ShowConfirmation(ConfirmationAction.MainMenu, "Zum Hauptmenü zurückkehren?\nDer aktuelle Fortschritt wird gespeichert.");
		}

		public void RequestQuit()
		{
			ShowConfirmation(ConfirmationAction.Quit, "Spiel wirklich beenden?");
		}

		public void ConfirmRequest()
		{
			if (!_requestPending)
			{
				_requestPending = true;
				CancelPlayerActions();
				bool flag;
				string error;
				switch (_confirmation)
				{
				case ConfirmationAction.HomeBase:
					flag = _pause.TryReturnHome(out error);
					break;
				case ConfirmationAction.MainMenu:
					flag = _pause.TryReturnToMainMenu(out error);
					break;
				case ConfirmationAction.Quit:
				{
					_settings.Save(out var _);
					_pause.Resume();
					Application.Quit();
					flag = true;
					error = string.Empty;
					break;
				}
				default:
					flag = false;
					error = "No pause action is selected.";
					break;
				}
				if (flag)
				{
					HideAll();
					return;
				}
				_requestPending = false;
				feedbackText.text = error;
				confirmButton.interactable = true;
			}
		}

		public void CancelConfirmation()
		{
			if (!_requestPending)
			{
				_confirmation = ConfirmationAction.None;
				confirmationPanel.SetActive(value: false);
				pausePanel.SetActive(value: true);
				feedbackText.text = string.Empty;
				EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
			}
		}

		public void HandleBackPressed()
		{
			TryHandleBackPressed();
		}

		private bool TryHandleBackPressed()
		{
			if (_session.Phase != GameSessionPhase.ActiveGame)
			{
				return false;
			}
			if (IsConfirmationOpen)
			{
				CancelConfirmation();
				return true;
			}
			if (IsSettingsOpen)
			{
				CloseSettings();
				return true;
			}
			if (CloseTopGameplayWindow())
			{
				return true;
			}
			if (IsPauseOpen)
			{
				ResumeGameplay();
			}
			else
			{
				OpenPause();
			}
			return true;
		}

		private void OpenSettings(bool fromPause)
		{
			_settingsOpenedFromPause = fromPause;
			canvasRoot.SetActive(value: true);
			pausePanel.SetActive(value: false);
			confirmationPanel.SetActive(value: false);
			settingsPanel.SetActive(value: true);
			RenderSettings();
			EventSystem.current?.SetSelectedGameObject(masterSlider.gameObject);
		}

		private void ShowConfirmation(ConfirmationAction action, string message)
		{
			if (IsPauseOpen && !_requestPending)
			{
				_confirmation = action;
				confirmationText.text = message;
				feedbackText.text = string.Empty;
				confirmButton.interactable = true;
				pausePanel.SetActive(value: false);
				confirmationPanel.SetActive(value: true);
				EventSystem.current?.SetSelectedGameObject(cancelButton.gameObject);
			}
		}

		private bool CloseTopGameplayWindow()
		{
			if (_player == null)
			{
				return false;
			}
			if (_player.BuildingMenu != null && _player.BuildingMenu.IsOpen)
			{
				_player.BuildingMenu.Close();
				return true;
			}
			if (_player.TechnologyWindow != null && _player.TechnologyWindow.IsOpen)
			{
				_player.TechnologyWindow.Close();
				return true;
			}
			if (_player.StorageWindow != null && _player.StorageWindow.IsOpen)
			{
				_player.StorageWindow.Close();
				return true;
			}
			if (_player.CraftingWindow != null && _player.CraftingWindow.IsOpen)
			{
				_player.CraftingWindow.Close();
				return true;
			}
			if (_player.InventoryWindow != null && _player.InventoryWindow.IsOpen)
			{
				_player.InventoryWindow.Close();
				return true;
			}
			return false;
		}

		private void CancelPlayerActions()
		{
			_exitCoordinator?.CancelForPause();
			if (!(_player == null))
			{
				_playerInput?.SetGameplayEnabled(enabled: false);
				_player.Motor.CancelForSceneTransition();
				_player.Motor.SetMovementEnabled(enabled: false);
				_player.Combat.CancelForSceneTransition();
				_player.WeaponHitbox.EndWindow();
				_player.Interaction.CancelForSceneTransition();
				_player.EidraTeam.CancelForSceneTransition();
			}
		}

		private void RestorePlayerInput()
		{
			if (!(_player == null) && !(_playerInput == null) && !_sceneFlow.IsInputBlocked && _player.Damageable.IsAlive)
			{
				_playerInput.SetGameplayEnabled(enabled: true);
				_player.Motor.SetMovementEnabled(enabled: true);
			}
		}

		private void RenderSettings()
		{
			SettingsData current = _settings.Current;
			masterSlider.SetValueWithoutNotify(current.masterVolume);
			musicSlider.SetValueWithoutNotify(current.musicVolume);
			sfxSlider.SetValueWithoutNotify(current.sfxVolume);
			qualityDropdown.SetValueWithoutNotify((int)current.quality);
			frameRateDropdown.SetValueWithoutNotify((current.targetFrameRate != 30) ? 1 : 0);
			UpdateVolumeLabels(current);
		}

		private void HandleMasterChanged(float value)
		{
			_settings.SetMasterVolume(value);
			UpdateVolumeLabels(_settings.Current);
		}

		private void HandleMusicChanged(float value)
		{
			_settings.SetMusicVolume(value);
			UpdateVolumeLabels(_settings.Current);
		}

		private void HandleSfxChanged(float value)
		{
			_settings.SetSfxVolume(value);
			UpdateVolumeLabels(_settings.Current);
		}

		private void HandleQualityChanged(int value)
		{
			_settings.SetQuality((EidrenQualityPreset)Mathf.Clamp(value, 0, 2));
		}

		private void HandleFrameRateChanged(int value)
		{
			_settings.SetTargetFrameRate((value == 0) ? 30 : 60);
		}

		private void UpdateVolumeLabels(SettingsData data)
		{
			masterValue.text = $"{Mathf.RoundToInt(data.masterVolume * 100f)} %";
			musicValue.text = $"{Mathf.RoundToInt(data.musicVolume * 100f)} %";
			sfxValue.text = $"{Mathf.RoundToInt(data.sfxVolume * 100f)} %";
		}

		private void HandlePlayerDied()
		{
			HideAll();
			_pause.Resume();
		}

		private void HandleSceneTransitionStarted(string sceneName)
		{
			HideAll();
			_pause.Resume();
		}

		private void HandleSceneTransitionCompleted(string sceneName)
		{
			_requestPending = false;
			_confirmation = ConfirmationAction.None;
			RefreshPauseButton();
		}

		private void RefreshPauseButton()
		{
			bool active = _session != null && _session.Phase == GameSessionPhase.ActiveGame && !IsMainMenuScene() && !_sceneFlow.IsTransitioning && !IsPauseOpen && !IsSettingsOpen && !IsConfirmationOpen;
			canvasRoot.SetActive(active);
			pauseButton.gameObject.SetActive(active);
		}

		private bool IsMainMenuScene()
		{
			string a = SceneManager.GetActiveScene().name;
			return string.Equals(a, "MainMenu", StringComparison.Ordinal) || string.Equals(a, "Bootstrap", StringComparison.Ordinal);
		}

		private void HideAll()
		{
			pausePanel.SetActive(value: false);
			settingsPanel.SetActive(value: false);
			confirmationPanel.SetActive(value: false);
			canvasRoot.SetActive(value: false);
			EventSystem.current?.SetSelectedGameObject(null);
		}

		private void BindEvents()
		{
			if (!_eventsBound)
			{
				pauseButton.onClick.AddListener(TogglePause);
				resumeButton.onClick.AddListener(ResumeGameplay);
				settingsButton.onClick.AddListener(OpenSettingsFromPause);
				homeButton.onClick.AddListener(RequestHomeBase);
				mainMenuButton.onClick.AddListener(RequestMainMenu);
				quitButton.onClick.AddListener(RequestQuit);
				settingsBackButton.onClick.AddListener(CloseSettings);
				confirmButton.onClick.AddListener(ConfirmRequest);
				cancelButton.onClick.AddListener(CancelConfirmation);
				masterSlider.onValueChanged.AddListener(HandleMasterChanged);
				musicSlider.onValueChanged.AddListener(HandleMusicChanged);
				sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
				qualityDropdown.onValueChanged.AddListener(HandleQualityChanged);
				frameRateDropdown.onValueChanged.AddListener(HandleFrameRateChanged);
				_menuInput.RegisterBackHandler(TryHandleBackPressed, 100);
				_sceneFlow.SceneTransitionStarted += HandleSceneTransitionStarted;
				_sceneFlow.SceneTransitionCompleted += HandleSceneTransitionCompleted;
				_eventsBound = true;
				quitButton.gameObject.SetActive(Application.platform == RuntimePlatform.WindowsPlayer);
			}
		}

		private void Unbind()
		{
			if (_eventsBound)
			{
				pauseButton.onClick.RemoveListener(TogglePause);
				resumeButton.onClick.RemoveListener(ResumeGameplay);
				settingsButton.onClick.RemoveListener(OpenSettingsFromPause);
				homeButton.onClick.RemoveListener(RequestHomeBase);
				mainMenuButton.onClick.RemoveListener(RequestMainMenu);
				quitButton.onClick.RemoveListener(RequestQuit);
				settingsBackButton.onClick.RemoveListener(CloseSettings);
				confirmButton.onClick.RemoveListener(ConfirmRequest);
				cancelButton.onClick.RemoveListener(CancelConfirmation);
				masterSlider.onValueChanged.RemoveListener(HandleMasterChanged);
				musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
				sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
				qualityDropdown.onValueChanged.RemoveListener(HandleQualityChanged);
				frameRateDropdown.onValueChanged.RemoveListener(HandleFrameRateChanged);
				_menuInput?.UnregisterBackHandler(TryHandleBackPressed);
				if (_sceneFlow != null)
				{
					_sceneFlow.SceneTransitionStarted -= HandleSceneTransitionStarted;
					_sceneFlow.SceneTransitionCompleted -= HandleSceneTransitionCompleted;
				}
				_eventsBound = false;
			}
		}

		private void ValidateReferences()
		{
			if (canvasRoot == null || pauseButton == null || pausePanel == null || resumeButton == null || settingsButton == null || homeButton == null || mainMenuButton == null || quitButton == null || settingsPanel == null || masterSlider == null || musicSlider == null || sfxSlider == null || masterValue == null || musicValue == null || sfxValue == null || qualityDropdown == null || frameRateDropdown == null || settingsBackButton == null || confirmationPanel == null || confirmationText == null || feedbackText == null || confirmButton == null || cancelButton == null)
			{
				throw new InvalidOperationException("PauseMenu prefab references are incomplete.");
			}
		}

		private void OnDestroy()
		{
			if (_player != null && _player.Damageable != null)
			{
				_player.Damageable.Died -= HandlePlayerDied;
			}
			Unbind();
			_pause?.Resume();
		}
	}
}
