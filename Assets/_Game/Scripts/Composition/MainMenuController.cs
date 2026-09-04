using Eidren.Core.Services;
using Eidren.UI;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class MainMenuController : MonoBehaviour
	{
		[SerializeField]
		private Button newGameButton;

		[SerializeField]
		private Button continueButton;

		[SerializeField]
		private Button settingsButton;

		[SerializeField]
		private Button quitButton;

		[SerializeField]
		private Button closeSettingsButton;

		[SerializeField]
		private GameObject settingsPanel;

		[SerializeField]
		private Text continueLabel;

		private GameSession _session;

		private SceneFlowService _sceneFlow;

		private SaveGameService _saveService;

		private PauseMenuController _pauseMenu;

		private MenuInputService _menuInput;

		private bool _eventsBound;

		private bool _newGameConfirmationPending;

		private bool _quitConfirmationPending;

		public bool SettingsOpen => (_pauseMenu != null && _pauseMenu.IsSettingsOpen) || (settingsPanel != null && settingsPanel.activeSelf);

		public void ConfigureUi(Button newGame, Button continueGame, Button settings, Button quit, Button closeSettings, GameObject settingsWindow, Text disabledContinueLabel)
		{
			newGameButton = newGame;
			continueButton = continueGame;
			settingsButton = settings;
			quitButton = quit;
			closeSettingsButton = closeSettings;
			settingsPanel = settingsWindow;
			continueLabel = disabledContinueLabel;
		}

		public void Initialize(GameSession session, SceneFlowService sceneFlow, SaveGameService saveService = null, PauseMenuController pauseMenu = null, MenuInputService menuInput = null)
		{
			_session = session;
			_sceneFlow = sceneFlow;
			_saveService = saveService;
			_pauseMenu = pauseMenu;
			_menuInput = menuInput;
			ConfigureInitialState();
			BindEvents();
		}

		public void StartNewGame()
		{
			if (!(_session == null) && !(_sceneFlow == null))
			{
				if (_saveService != null && _saveService.HasAnySaveFile() && !_newGameConfirmationPending)
				{
					_newGameConfirmationPending = true;
					SetNewGameLabel("ERNEUT KLICKEN: SPIELSTAND ERSETZEN");
					return;
				}
				_newGameConfirmationPending = false;
				SetNewGameLabel("NEUES SPIEL");
				_saveService?.BeginNewGame();
				_session.StartNewGame();
				ApplyTesterTemplateIfPresent();
				_sceneFlow.TryLoadScene("HomeBase");
			}
		}

		/// <summary>
		/// Tester-Paket: Liegt ein Voll-Ausbau-Spielstand bei, startet auch ein
		/// neues Spiel damit — sonst waere der Stand fuer alle unerreichbar, die
		/// schon einen eigenen Spielstand haben.
		/// </summary>
		private void ApplyTesterTemplateIfPresent()
		{
			TextAsset template = Resources.Load<TextAsset>("Data/TesterSeedSave");
			if (template == null || _saveService == null)
			{
				return;
			}
			if (!_saveService.TryApplyTemplate(template.text, out string error))
			{
				Debug.LogWarning("[Eidren] Tester-Spielstand nicht uebernommen: " + error);
			}
		}

		public void ContinueGame()
		{
			if (_saveService == null || _sceneFlow == null)
			{
				return;
			}
			if (_saveService.TryLoad(out var error))
			{
				_newGameConfirmationPending = false;
				_sceneFlow.TryLoadScene("HomeBase");
				return;
			}
			ConfigureContinueState();
			if (continueLabel != null)
			{
				continueLabel.text = "SPIELSTAND FEHLERHAFT\n" + error;
			}
		}

		public void OpenSettings()
		{
			_quitConfirmationPending = false;
			SetQuitLabel("BEENDEN");
			if (_pauseMenu != null)
			{
				_pauseMenu.OpenSettingsFromMainMenu();
			}
			else if (settingsPanel != null)
			{
				settingsPanel.SetActive(value: true);
			}
		}

		public void CloseSettings()
		{
			if (_pauseMenu != null && _pauseMenu.IsSettingsOpen)
			{
				_pauseMenu.CloseSettings();
			}
			if (settingsPanel != null)
			{
				settingsPanel.SetActive(value: false);
			}
		}

		public void QuitGame()
		{
			if (!_quitConfirmationPending)
			{
				_quitConfirmationPending = true;
				SetQuitLabel("ERNEUT DRÜCKEN: BEENDEN");
				return;
			}
			_quitConfirmationPending = false;
			SetQuitLabel("BEENDEN");
			if (_saveService != null && _session != null && _session.Phase == GameSessionPhase.ActiveGame)
			{
				_saveService.SaveNow(SaveRequestReason.ApplicationQuit, out var _);
			}
			if (Application.isEditor)
			{
				Debug.Log("Eidren: Beenden wurde im Editor angefordert; der Editor bleibt geöffnet.", this);
			}
			else
			{
				Application.Quit();
			}
		}

		private void Awake()
		{
			EnsureSafeAreaLayout();
			EidrenServiceRoot eidrenServiceRoot = EidrenServiceRoot.FindOrCreate();
			Initialize(eidrenServiceRoot.GameSession, eidrenServiceRoot.SceneFlowService, eidrenServiceRoot.SaveGameService, eidrenServiceRoot.PauseMenuController, eidrenServiceRoot.MenuInputService);
		}

		private void ConfigureInitialState()
		{
			if (settingsPanel != null)
			{
				settingsPanel.SetActive(value: false);
			}
			ConfigureContinueState();
		}

		private void BindEvents()
		{
			if (!_eventsBound)
			{
				newGameButton?.onClick.AddListener(StartNewGame);
				continueButton?.onClick.AddListener(ContinueGame);
				settingsButton?.onClick.AddListener(OpenSettings);
				quitButton?.onClick.AddListener(QuitGame);
				closeSettingsButton?.onClick.AddListener(CloseSettings);
				_menuInput?.RegisterBackHandler(TryHandleBackPressed, 200);
				_eventsBound = true;
			}
		}

		private void OnDestroy()
		{
			if (_eventsBound)
			{
				newGameButton?.onClick.RemoveListener(StartNewGame);
				continueButton?.onClick.RemoveListener(ContinueGame);
				settingsButton?.onClick.RemoveListener(OpenSettings);
				quitButton?.onClick.RemoveListener(QuitGame);
				closeSettingsButton?.onClick.RemoveListener(CloseSettings);
				_menuInput?.UnregisterBackHandler(TryHandleBackPressed);
				_eventsBound = false;
			}
		}

		private void ConfigureContinueState()
		{
			bool flag = _saveService != null && _saveService.HasRecoverableSave();
			if (continueButton != null)
			{
				continueButton.interactable = flag;
			}
			if (continueLabel != null)
			{
				continueLabel.text = (flag ? "FORTSETZEN" : "KEIN SPIELSTAND");
				continueLabel.color = (flag ? Color.white : new Color(0.48f, 0.52f, 0.56f, 1f));
			}
		}

		private void SetNewGameLabel(string value)
		{
			if (!(newGameButton == null))
			{
				Text componentInChildren = newGameButton.GetComponentInChildren<Text>();
				if (componentInChildren != null)
				{
					componentInChildren.text = value;
				}
			}
		}

		private void HandleBackPressed()
		{
			TryHandleBackPressed();
		}

		private bool TryHandleBackPressed()
		{
			if (_session == null || _session.Phase != GameSessionPhase.Shell)
			{
				return false;
			}
			if (SettingsOpen)
			{
				CloseSettings();
				return true;
			}
			QuitGame();
			return true;
		}

		private void SetQuitLabel(string value)
		{
			if (!(quitButton == null))
			{
				Text componentInChildren = quitButton.GetComponentInChildren<Text>();
				if (componentInChildren != null)
				{
					componentInChildren.text = value;
				}
			}
		}

		private void EnsureSafeAreaLayout()
		{
			Canvas componentInChildren = GetComponentInChildren<Canvas>(includeInactive: true);
			if (componentInChildren == null)
			{
				return;
			}
			RectTransform rectTransform = componentInChildren.transform as RectTransform;
			if (rectTransform == null)
			{
				return;
			}
			RectTransform rectTransform2 = rectTransform.Find("SafeArea") as RectTransform;
			if (rectTransform2 == null)
			{
				GameObject gameObject = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaPanel));
				rectTransform2 = gameObject.GetComponent<RectTransform>();
				rectTransform2.SetParent(rectTransform, worldPositionStays: false);
				rectTransform2.anchorMin = Vector2.zero;
				rectTransform2.anchorMax = Vector2.one;
				rectTransform2.offsetMin = Vector2.zero;
				rectTransform2.offsetMax = Vector2.zero;
			}
			for (int num = rectTransform.childCount - 1; num >= 0; num--)
			{
				Transform child = rectTransform.GetChild(num);
				if (!(child == rectTransform2) && !(child.name == "Background"))
				{
					child.SetParent(rectTransform2, worldPositionStays: false);
				}
			}
		}
	}
}
