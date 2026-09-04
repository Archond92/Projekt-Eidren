using Eidren.Core.Services;
using Eidren.Data;
using System;
using UnityEngine.Audio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
	public sealed class EidrenServiceRoot : MonoBehaviour
	{
		private bool _blueprintBridgeBound;

		private bool _processingBlueprintPickup;

		private bool _composed;

		public static EidrenServiceRoot Instance { get; private set; }

		public GameSession GameSession { get; private set; }

		public ContentDatabase ContentDatabase { get; private set; }

		public SceneFlowService SceneFlowService { get; private set; }

		public SaveGameService SaveGameService { get; private set; }

		public SettingsService SettingsService { get; private set; }

		public AudioService AudioService { get; private set; }

		public PauseService PauseService { get; private set; }

		public MenuInputService MenuInputService { get; private set; }

		public PauseMenuController PauseMenuController { get; private set; }

		public PlayerProgressionService PlayerProgression { get; private set; }

		public TechnologyTreeDefinition TechnologyTree { get; private set; }

		public TechnologyUnlockService TechnologyUnlocks { get; private set; }

		public BuildingService Buildings { get; private set; }

		public EidraRosterService EidraRoster { get; private set; }

		public QuestProgressService QuestProgress { get; private set; }

		public CaptureEquipment CaptureEquipment { get; private set; }

		public AudioRuntimeBinder AudioRuntimeBinder { get; private set; }

		public ApplicationLifecycleController ApplicationLifecycleController { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			Instance = null;
		}

		public static EidrenServiceRoot FindOrCreate()
		{
			if (Instance != null)
			{
				Instance.Compose();
				return Instance;
			}
			EidrenServiceRoot eidrenServiceRoot = UnityEngine.Object.FindFirstObjectByType<EidrenServiceRoot>();
			if (eidrenServiceRoot != null)
			{
				eidrenServiceRoot.Compose();
				return eidrenServiceRoot;
			}
			GameObject gameObject = new GameObject("EIDREN_SERVICES");
			return gameObject.AddComponent<EidrenServiceRoot>();
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				if (Application.isPlaying)
				{
					UnityEngine.Object.Destroy(base.gameObject);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(base.gameObject);
				}
				return;
			}
			Instance = this;
			if (Application.isPlaying)
			{
				UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			}
			Compose();
		}

		private void Compose()
		{
			if (Instance != null && Instance != this)
			{
				return;
			}
			if ((object)GameSession == null)
			{
				GameSession gameSession = (GameSession = GetOrAdd<GameSession>());
			}
			if ((object)ContentDatabase == null)
			{
				ContentDatabase contentDatabase = (ContentDatabase = GetOrAdd<ContentDatabase>());
			}
			if ((object)SceneFlowService == null)
			{
				SceneFlowService sceneFlowService = (SceneFlowService = GetOrAdd<SceneFlowService>());
			}
			if ((object)SaveGameService == null)
			{
				SaveGameService saveGameService = (SaveGameService = GetOrAdd<SaveGameService>());
			}
			if ((object)SettingsService == null)
			{
				SettingsService settingsService = (SettingsService = GetOrAdd<SettingsService>());
			}
			if ((object)AudioService == null)
			{
				AudioService audioService = (AudioService = GetOrAdd<AudioService>());
			}
			if ((object)PauseService == null)
			{
				PauseService pauseService = (PauseService = GetOrAdd<PauseService>());
			}
			if ((object)MenuInputService == null)
			{
				MenuInputService menuInputService = (MenuInputService = GetOrAdd<MenuInputService>());
			}
			if ((object)AudioRuntimeBinder == null)
			{
				AudioRuntimeBinder audioRuntimeBinder = (AudioRuntimeBinder = GetOrAdd<AudioRuntimeBinder>());
			}
			if ((object)ApplicationLifecycleController == null)
			{
				ApplicationLifecycleController applicationLifecycleController = (ApplicationLifecycleController = GetOrAdd<ApplicationLifecycleController>());
			}
			SceneTransitionCanvas orAdd11 = GetOrAdd<SceneTransitionCanvas>();
			WorldTravelCoordinator orAdd12 = GetOrAdd<WorldTravelCoordinator>();
			ZoneRegenerationCoordinator orAdd13 = GetOrAdd<ZoneRegenerationCoordinator>();
			GameSession.Initialize();
			GameSession.ConfigureContentDatabase(ContentDatabase);
			ProgressionCurveDefinition progressionCurveDefinition = Resources.Load<ProgressionCurveDefinition>("Data/Progression/ProgressionCurve_V01");
			if (progressionCurveDefinition == null)
			{
				throw new InvalidOperationException("Progression curve resource is missing.");
			}
			if (PlayerProgression == null)
			{
				PlayerProgression = new PlayerProgressionService(progressionCurveDefinition);
			}
			BindBlueprintPickupBridge();
			if ((object)TechnologyTree == null)
			{
				TechnologyTreeDefinition technologyTreeDefinition = (TechnologyTree = Resources.Load<TechnologyTreeDefinition>("Data/Progression/TechnologyTree_V01"));
			}
			if (TechnologyTree == null)
			{
				throw new InvalidOperationException("Technology tree resource is missing.");
			}
			string[] errors = TechnologyTreeContentValidator.GetErrors(TechnologyTree, ContentDatabase, progressionCurveDefinition);
			if (errors.Length != 0)
			{
				throw new InvalidOperationException(string.Join("; ", errors));
			}
			if (TechnologyUnlocks == null)
			{
				TechnologyUnlockService technologyUnlockService = (TechnologyUnlocks = new TechnologyUnlockService(TechnologyTree, PlayerProgression, GameSession.HasProgressFlag));
			}
			if (Buildings == null)
			{
				BuildingService buildingService = (Buildings = new BuildingService(ContentDatabase, GameSession, TechnologyUnlocks, PlayerProgression));
			}
			if (EidraRoster == null)
			{
				EidraRosterService eidraRosterService = (EidraRoster = new EidraRosterService(GameSession.Eidra, TechnologyUnlocks));
			}
			if (CaptureEquipment == null)
			{
				CaptureEquipment captureEquipment = (CaptureEquipment = new CaptureEquipment(GameSession.PlayerEquipment, ContentDatabase, GameSession.PlayerInventory));
			}
			if (QuestProgress == null)
			{
				// F31-006: Tutorial-Questkette. Der Dienst hoert auf die
				// Quest-Ereignisse der Progression und den Fang des Rosters —
				// die Aufruforte im Spielfluss bleiben unangetastet.
				QuestChainDefinition questChain = Resources.Load<QuestChainDefinition>("Data/Progression/QuestChain_V01");
				if (questChain == null)
				{
					throw new InvalidOperationException("Quest chain resource is missing.");
				}
				QuestProgressService questProgress = (QuestProgress = new QuestProgressService(questChain.Id, questChain.BuildSteps(), delegate(int xp)
				{
					PlayerProgression.RecordQuestStepCompleted(xp);
				}));
				PlayerProgression.QuestRecipeCrafted += questProgress.NotifyRecipeCrafted;
				PlayerProgression.QuestBuildingConstructed += questProgress.NotifyBuildingConstructed;
				PlayerProgression.QuestResourceNodeCompleted += questProgress.NotifyResourceNodeCompleted;
				PlayerProgression.QuestEnemyDefeated += questProgress.NotifyEnemyDefeated;
				EidraRoster.Captured += questProgress.NotifyEidraCaptured;
			}
			if (_composed)
			{
				return;
			}
			SceneFlowService.Configure(orAdd11);
			orAdd13.Configure(GameSession, SceneFlowService);
			SaveGameService.Initialize(GameSession, ContentDatabase, SceneFlowService, null, null, PlayerProgression, TechnologyUnlocks, QuestProgress);
			SeedTesterSaveIfMissing();
			AudioMixer mixer = Resources.Load<AudioMixer>("Audio/EidrenAudioMixer");
			SettingsService.Initialize(mixer);
			AudioEventCatalogDefinition catalog = Resources.Load<AudioEventCatalogDefinition>("Data/Audio/AudioEventCatalog_V01");
			AudioService.Initialize(catalog, SettingsService);
			PauseService.Initialize(GameSession, SceneFlowService, SaveGameService);
			PauseMenuController = GetComponentInChildren<PauseMenuController>(includeInactive: true);
			if (PauseMenuController == null)
			{
				PauseMenuController pauseMenuController = Resources.Load<PauseMenuController>("UI/PauseMenu");
				if (pauseMenuController != null)
				{
					PauseMenuController = UnityEngine.Object.Instantiate(pauseMenuController, base.transform);
					PauseMenuController.name = "PauseMenu";
				}
			}
			if (PauseMenuController != null)
			{
				PauseMenuController.Initialize(PauseService, SettingsService, MenuInputService, SceneFlowService, GameSession);
			}
			AudioRuntimeBinder.Initialize(AudioService, SceneFlowService, PauseService);
			ApplicationLifecycleController.Initialize(GameSession, PauseService, SaveGameService, PauseMenuController);
			orAdd12.Configure(GameSession, SceneFlowService);
			_composed = true;
		}

		private void BindBlueprintPickupBridge()
		{
			if (!_blueprintBridgeBound && GameSession.PlayerInventory != null)
			{
				GameSession.PlayerInventory.Changed += HandleBlueprintPickup;
				_blueprintBridgeBound = true;
				HandleBlueprintPickup(default(InventoryChange));
			}
		}

		private void HandleBlueprintPickup(InventoryChange _)
		{
			if (_processingBlueprintPickup || PlayerProgression == null)
			{
				return;
			}
			PlayerInventory playerInventory = GameSession.PlayerInventory;
			if (playerInventory.GetTotalAmount("blueprint_item_copper_spear") <= 0)
			{
				return;
			}
			_processingBlueprintPickup = true;
			try
			{
				if (playerInventory.Remove("blueprint_item_copper_spear", 1))
				{
					PlayerProgression.LearnBlueprint("blueprint.copper_spear");
				}
			}
			finally
			{
				_processingBlueprintPickup = false;
			}
		}

		private T GetOrAdd<T>() where T : Component
		{
			T component = GetComponent<T>();
			return (component != null) ? component : base.gameObject.AddComponent<T>();
		}

		/// <summary>
		/// Tester-Paket: Liegt ein mitgelieferter Voll-Ausbau-Spielstand bei und
		/// hat der Spieler noch keinen eigenen, wird er einmalig eingespielt.
		/// Vorhandene Spielstaende bleiben unangetastet.
		///
		/// In oeffentlichen Builds liegt die Vorlage bewusst NICHT bei: sie
		/// wohnt unter Assets/_Game/Editor/TesterSeed/ und wird nur fuer
		/// Tester-Pakete nach Resources/Data/ kopiert. Ohne sie faellt der
		/// Start auf den regulaeren Weg zurueck.
		/// </summary>
		private void SeedTesterSaveIfMissing()
		{
			TextAsset template = Resources.Load<TextAsset>("Data/TesterSeedSave");
			if (template == null)
			{
				return;
			}
			if (SaveGameService.TrySeedFromTemplate(template.text, out string error))
			{
				Debug.Log("[Eidren] Tester-Spielstand eingespielt (Voll-Ausbau).");
			}
			else
			{
				Debug.Log("[Eidren] Tester-Spielstand nicht eingespielt: " + error);
			}
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}
	}
}
