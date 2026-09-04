using Eidren.AI;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Eidra;
using Eidren.Gameplay.Flow;
using Eidren.Gameplay.Presentation;
using Eidren.Input;
using Eidren.Interaction;
using Eidren.Player;
using Eidren.Presentation;
using Eidren.UI;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class PlayerPrefabBindings : MonoBehaviour
	{
		[Header("Prefab Components")]
		[SerializeField]
		private CharacterController characterController;

		[SerializeField]
		private Damageable damageable;

		[SerializeField]
		private PlayerMotor motor;

		[SerializeField]
		private EidraTeamController eidraTeam;

		[SerializeField]
		private PlayerCombatController combat;

		[SerializeField]
		private InteractionController interaction;

		[SerializeField]
		private MeleeWeaponHitbox weaponHitbox;

		[SerializeField]
		private ConsumableController consumables;

		[SerializeField]
		private PlayerVisualAnimator visualAnimator;

		[Header("Visual References")]
		[SerializeField]
		private MonoBehaviour playerVisual;   // muss IActorPresentation implementieren

		[SerializeField]
		private Transform weaponVisual;

		[Header("Prototype Values")]
		[SerializeField]
		private float maxHealth = 100f;

		[SerializeField]
		private int playerArtSortingOrder = 12;

		private GameSession _boundSession;

		private PlayerEquipment _boundEquipment;

		private ContentDatabase _boundContent;

		private EquipmentDurabilityService _equipmentDurability;

		private WeaponData _defaultHammer;

		private WeaponData _defaultDaggers;

		public CharacterController CharacterController => characterController;

		public Damageable Damageable => damageable;

		public PlayerMotor Motor => motor;

		public EidraTeamController EidraTeam => eidraTeam;

		public PlayerCombatController Combat => combat;

		public InteractionController Interaction => interaction;

		public MeleeWeaponHitbox WeaponHitbox => weaponHitbox;

		public ConsumableController Consumables => consumables;

		public PlayerVisualAnimator VisualAnimator => visualAnimator;

		public IActorPresentation PlayerVisual => playerVisual as IActorPresentation;

		public Transform WeaponVisual => weaponVisual;

		public float MaxHealth => maxHealth;

		public CombatHUD CombatHud { get; private set; }

		// N04-001: Die Minimap braucht die Spielkamera fuer die Sichtpruefung der
		// Kisten. Sie kommt beim Bewegungs-Binden herein, gebraucht wird sie erst
		// beim HUD-Binden.
		private Camera _gameplayCamera;

		public InventoryWindow InventoryWindow { get; private set; }

		public CraftingWindow CraftingWindow { get; private set; }

		public TechnologyTreeWindow TechnologyWindow { get; private set; }

		public EidraTeamWindow EidraTeamWindow { get; private set; }

		public BuildingMenuWindow BuildingMenu { get; private set; }

		public BuildingPlacementController BuildingPlacement { get; private set; }

		public StorageWindow StorageWindow { get; private set; }

		public PlayerDeathController DeathController { get; private set; }

		public InventoryFeedbackPresenter InventoryFeedback { get; private set; }

		public void ConfigurePrefabReferences(CharacterController controller, Damageable health, PlayerMotor playerMotor, EidraTeamController team, PlayerCombatController combatController, InteractionController interactionController, MeleeWeaponHitbox meleeHitbox, ConsumableController consumableController, PlayerVisualAnimator animator, MonoBehaviour visual, Transform weapon)
		{
			characterController = controller;
			damageable = health;
			motor = playerMotor;
			eidraTeam = team;
			combat = combatController;
			interaction = interactionController;
			weaponHitbox = meleeHitbox;
			consumables = consumableController;
			visualAnimator = animator;
			playerVisual = visual;
			weaponVisual = weapon;
		}

		public void PrepareForRuntime()
		{
			ValidateReferences();
			damageable.Initialize(maxHealth);
		}

		public void BindRuntimeDependencies(PlayerInputReader input, Camera gameplayCamera, BossController boss, WeaponData hammer, WeaponData daggers, EidraData terrock, EidraData noctarion, ItemDefinition healingPotion, ItemDefinition buffFood, PlayerInventory inventory, Material eidraVisualMaterial, Material rangeMaterial, Vector3 arenaCenter, float arenaRadius, WeaponProgressionState weaponProgression = null, GameSession session = null)
		{
			ValidateReferences();
			_gameplayCamera = gameplayCamera;
			motor.Initialize(input, gameplayCamera, damageable, arenaCenter, arenaRadius);
			var targeting = new PersistentCombatTargetQuery(transform, motor, combat, damageable);
			eidraTeam.Initialize(input, motor, damageable, boss, terrock, noctarion, eidraVisualMaterial, rangeMaterial, targeting);
			combat.Initialize(input, motor, targeting, weaponHitbox, eidraTeam, hammer, daggers, weaponVisual, weaponProgression);
			BindSessionSelections(session);
			visualAnimator.Initialize(motor, combat, damageable);
			consumables.Initialize(input, damageable, combat, inventory, healingPotion, buffFood);
		}

		public void BindExplorationDependencies(PlayerInputReader input, Camera gameplayCamera, Vector3 movementCenter, float movementRadius)
		{
			BindExplorationDependencies(input, gameplayCamera, PlayerMovementBoundary.Circle(movementCenter, movementRadius));
		}

		public void BindExplorationDependencies(PlayerInputReader input, Camera gameplayCamera, PlayerMovementBoundary movementBoundary)
		{
			ValidateReferences();
			_gameplayCamera = gameplayCamera;
			motor.Initialize(input, gameplayCamera, damageable, movementBoundary);
			visualAnimator.InitializeExploration(motor, damageable);
		}

		public void BindInteractionDependencies(PlayerInputReader input, SceneFlowService sceneFlow, PlayerInventory inventory)
		{
			ValidateReferences();
			interaction.Initialize(input, motor, combat, damageable, sceneFlow, inventory);
			visualAnimator.BindInteraction(interaction, inventory);
		}

		public void BindExplorationCombat(PlayerInputReader input, WeaponData hammer, WeaponData daggers, WeaponProgressionState weaponProgression = null, GameSession session = null, EidraData terrock = null, EidraData noctarion = null, ContentDatabase content = null)
		{
			ValidateReferences();
			var targeting = new PersistentCombatTargetQuery(transform, motor, combat, damageable);
			eidraTeam.Initialize(input, motor, damageable, null, terrock, noctarion, null, null, targeting);
			_defaultHammer = hammer; _defaultDaggers = daggers; _boundContent = content;
			combat.Initialize(input, motor, targeting, weaponHitbox, eidraTeam.IsInitialized ? eidraTeam : null, hammer, daggers, weaponVisual, weaponProgression);
			BindSessionSelections(session);
			visualAnimator.BindCombat(combat);
		}

		public CombatHUD BindCombatHud(PlayerInputReader input, PlayerInventory inventory, PlayerProgressionService progression, BossController boss = null, GameFlowController flow = null)
		{
			if (CombatHud != null)
			{
				return CombatHud;
			}
			ValidateReferences();
			EnsureEventSystem();
			CombatHUD combatHUD = Resources.Load<CombatHUD>("UI/CombatHUD");
			if (combatHUD == null)
			{
				throw new InvalidOperationException("Required HUD prefab 'Resources/UI/CombatHUD' is missing. Runtime layout fallbacks are forbidden.");
			}
			CombatHud = UnityEngine.Object.Instantiate(combatHUD);
			CombatHud.name = "CombatHUD";
			CombatHud.Initialize(input, damageable, motor, combat, consumables, inventory, progression, eidraTeam.IsInitialized ? eidraTeam : null, boss, interaction, flow, _gameplayCamera);
			PlayerExperienceFeedback playerExperienceFeedback = GetComponent<PlayerExperienceFeedback>();
			if (playerExperienceFeedback == null)
			{
				playerExperienceFeedback = base.gameObject.AddComponent<PlayerExperienceFeedback>();
			}
			playerExperienceFeedback.Bind(progression);
			return CombatHud;
		}

		private void BindSessionSelections(GameSession session)
		{
			if (!(session == null))
			{
				if (_boundEquipment != null)
				{
					_boundEquipment.Changed -= HandleEquipmentChanged;
				}
				_boundSession = session;
				_boundEquipment = session.PlayerEquipment;
				_equipmentDurability = ((_boundContent != null) ? new EquipmentDurabilityService(_boundEquipment, _boundContent) : null);
				_boundEquipment.Changed += HandleEquipmentChanged;
				damageable.Damaged -= HandlePlayerDamaged;
				damageable.Damaged += HandlePlayerDamaged;
				combat.HitLanded -= HandleWeaponHitLanded;
				combat.HitLanded += HandleWeaponHitLanded;
				RefreshEquippedWeapons();
				RefreshArmor();
				// F32-002: Die Ankunft in einer Zone füllt auf das ECHTE
				// Maximum. Erst hier ist der Rüstungsbonus bekannt —
				// PrepareForRuntime läuft davor auf einem frischen Damageable
				// (Bonus 0) und kann nur den Grundwert füllen. Ohne diese
				// Zeile stand die Figur mit voller Stoffrüstung dauerhaft auf
				// 100/110.
				//
				// Bewusst HIER und nicht in RefreshArmor: Die Methode läuft
				// auch beim Anlegen von Rüstung im laufenden Spiel, und dort
				// darf sie nicht heilen (F31-018: Anlegen ist kein
				// Gratis-Heilen).
				damageable.HealToFull();
				combat.WeaponChanged -= HandleSessionWeaponChanged;
				combat.WeaponChanged += HandleSessionWeaponChanged;
				if (eidraTeam.ActiveData != null)
				{
					eidraTeam.TrySelectEidra(session.ActiveEidraId);
					eidraTeam.ActiveEidraChanged -= HandleSessionEidraChanged;
					eidraTeam.ActiveEidraChanged += HandleSessionEidraChanged;
				}
			}
		}

		private void HandleEquipmentChanged(EquipmentChange change)
		{
			RefreshEquippedWeapons();
			RefreshArmor();
		}

		private void RefreshEquippedWeapons()
		{
			if (_boundContent != null)
			{
				combat.SetEquippedWeapons(ResolveEquippedWeapon(EquipmentSlot.Weapon1), ResolveEquippedWeapon(EquipmentSlot.Weapon2), _boundSession?.ActiveWeaponId);
				return;
			}
			bool hammerAvailable = IsWeaponEquipped("hammer");
			bool daggersAvailable = IsWeaponEquipped("daggers");
			combat.SetWeaponAvailability(hammerAvailable, daggersAvailable, _boundSession?.ActiveWeaponId);
		}

		private WeaponData ResolveEquippedWeapon(EquipmentSlot slot)
		{
			if (_boundEquipment != null && _boundEquipment.TryGetSlot(slot, out var stack) && _boundContent.TryGetWeapon(stack.ItemId, out var value))
			{
				return value;
			}
			if (slot == EquipmentSlot.Weapon1 && IsWeaponEquipped("hammer"))
			{
				return _defaultHammer;
			}
			if (slot == EquipmentSlot.Weapon2 && IsWeaponEquipped("daggers"))
			{
				return _defaultDaggers;
			}
			return null;
		}

		private void HandleWeaponHitLanded(bool backAttack)
		{
			WeaponData activeWeapon = combat.ActiveWeapon;
			if (!(activeWeapon == null) && _equipmentDurability != null && _equipmentDurability.TryWearWeapon(activeWeapon.Id, DurabilityRules.WeaponWear(successfulAttack: true), out var broken) && broken)
			{
				CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * 2f, activeWeapon.DisplayName + " zerbrochen", new Color(1f, 0.35f, 0.18f), 1.4f);
			}
		}

		private bool IsWeaponEquipped(string itemId)
		{
			return HasEquippedItem(EquipmentSlot.Weapon1, itemId) || HasEquippedItem(EquipmentSlot.Weapon2, itemId);
		}

		private void RefreshArmor()
		{
			damageable.Protection = ((_equipmentDurability != null) ? _equipmentDurability.TotalArmorProtection() : 0f);
			damageable.SetMaxHealthBonus((_equipmentDurability != null) ? _equipmentDurability.TotalArmorHealthBonus() : 0f);
			visualAnimator.SetEquipmentTiers(ArmorTier(EquipmentSlot.Head), ArmorTier(EquipmentSlot.Chest), ArmorTier(EquipmentSlot.Hands), ArmorTier(EquipmentSlot.Legs));
		}

		private int ArmorTier(EquipmentSlot slot)
		{
			if (_boundEquipment == null || !_boundEquipment.TryGetSlot(slot, out var stack) || _boundContent == null || !_boundContent.TryGetItem(stack.ItemId, out var value) || value.Category != ItemCategory.Armor)
			{
				return 0;
			}
			return Mathf.Clamp(value.Tier + 1, 1, 3);
		}

		private bool HasArmorEquipped(EquipmentSlot slot)
		{
			if (_boundEquipment == null || !_boundEquipment.TryGetSlot(slot, out var stack))
			{
				return false;
			}
			if (_boundContent != null && _boundContent.TryGetItem(stack.ItemId, out var value))
			{
				return value.Category == ItemCategory.Armor;
			}
			return stack.ItemId == "armor_wanderer_hood" || stack.ItemId == "armor_wanderer_coat" || stack.ItemId == "armor_wanderer_bracers" || stack.ItemId == "armor_wanderer_legs";
		}

		private void HandlePlayerDamaged(DamageInfo damage)
		{
			if (_equipmentDurability != null)
			{
				ArmorWearSummary armorWearSummary = _equipmentDurability.WearArmor(damage.HealthDamage);
				for (int i = 0; i < armorWearSummary.BrokenItemNames.Count; i++)
				{
					CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * (2f + (float)i * 0.22f), armorWearSummary.BrokenItemNames[i] + " zerbrochen", new Color(1f, 0.35f, 0.18f), 1.4f);
				}
			}
		}

		private bool HasEquippedItem(EquipmentSlot slot, string itemId)
		{
			ItemStack stack;
			return _boundEquipment != null && _boundEquipment.TryGetSlot(slot, out stack) && stack.ItemId == itemId;
		}

		private void HandleSessionWeaponChanged(WeaponData active, WeaponData previous)
		{
			_boundSession?.SetActiveWeaponId(active?.Id);
		}

		private void HandleSessionEidraChanged(EidraData active)
		{
			if (active != null)
			{
				_boundSession?.SetActiveEidraId(active.Id);
			}
		}

		private void OnDestroy()
		{
			if (combat != null)
			{
				combat.WeaponChanged -= HandleSessionWeaponChanged;
				combat.HitLanded -= HandleWeaponHitLanded;
			}
			if (_boundEquipment != null)
			{
				_boundEquipment.Changed -= HandleEquipmentChanged;
			}
			if (damageable != null)
			{
				damageable.Damaged -= HandlePlayerDamaged;
			}
			if (eidraTeam != null)
			{
				eidraTeam.ActiveEidraChanged -= HandleSessionEidraChanged;
			}
		}

		public void BindExplorationConsumables(PlayerInputReader input, PlayerInventory inventory, ItemDefinition healingPotion, ItemDefinition buffFood)
		{
			ValidateReferences();
			consumables.Initialize(input, damageable, combat, inventory, healingPotion, buffFood);
		}

		public InventoryWindow BindInventoryWindow(PlayerInventory inventory, PlayerEquipment equipment, ContentDatabase database, PlayerInputReader input, SceneFlowService sceneFlow)
		{
			if (InventoryWindow != null)
			{
				return InventoryWindow;
			}
			EnsureEventSystem();
			InventoryWindow inventoryWindow = Resources.Load<InventoryWindow>("UI/InventoryWindow");
			if (inventoryWindow == null)
			{
				throw new InvalidOperationException("Inventory prefab 'Resources/UI/InventoryWindow' is missing. Run Eidren/UI/Build Inventory Prefab.");
			}
			InventoryWindow = UnityEngine.Object.Instantiate(inventoryWindow);
			InventoryWindow.name = "InventoryWindow";
			InventoryWindow.Initialize(inventory, equipment, database, consumables, input, motor, combat, damageable, sceneFlow);
			return InventoryWindow;
		}

		public CraftingWindow BindCraftingWindow(PlayerInventory inventory, PlayerEquipment equipment, WeaponProgressionState weaponProgression, PlayerProgressionService progression, TechnologyUnlockService technology, ContentDatabase database, PlayerInputReader input, SceneFlowService sceneFlow, SaveGameService saveService = null, IMaterialStock materials = null)
		{
			if (CraftingWindow != null)
			{
				return CraftingWindow;
			}
			EnsureEventSystem();
			CraftingWindow craftingWindow = Resources.Load<CraftingWindow>("UI/CraftingWindow");
			if (craftingWindow == null)
			{
				throw new InvalidOperationException("Crafting prefab 'Resources/UI/CraftingWindow' is missing. Run Eidren/Crafting/Build V0.1.");
			}
			CraftingWindow = UnityEngine.Object.Instantiate(craftingWindow);
			CraftingWindow.name = "CraftingWindow";
			CraftingWindow.Initialize(new CraftingService(database, inventory, weaponProgression, technology, equipment, progression, materials), database, inventory, input, motor, combat, damageable, sceneFlow, saveService);
			return CraftingWindow;
		}

		public TechnologyTreeWindow BindTechnologyTreeWindow(TechnologyTreeDefinition tree, TechnologyUnlockService unlocks, PlayerProgressionService progression, PlayerInputReader input, SceneFlowService sceneFlow)
		{
			if (TechnologyWindow != null)
			{
				return TechnologyWindow;
			}
			EnsureEventSystem();
			TechnologyTreeWindow technologyTreeWindow = Resources.Load<TechnologyTreeWindow>("UI/TechnologyTreeWindow");
			if (technologyTreeWindow == null)
			{
				throw new InvalidOperationException("Technology tree prefab is missing.");
			}
			TechnologyWindow = UnityEngine.Object.Instantiate(technologyTreeWindow);
			TechnologyWindow.name = "TechnologyTreeWindow";
			TechnologyWindow.Initialize(tree, unlocks, progression, input, motor, combat, damageable, sceneFlow);
			return TechnologyWindow;
		}

		/// <summary>
		/// F32-006: Fenster fuer das Eidra-Gespann. Muster wie der
		/// Technologiebaum — Prefab aus Resources, Dienste per Initialize.
		/// </summary>
		public EidraTeamWindow BindEidraTeamWindow(EidraRosterService roster, ContentDatabase content, PlayerInputReader input, SceneFlowService sceneFlow, GameSession session)
		{
			if (EidraTeamWindow != null)
			{
				return EidraTeamWindow;
			}
			EnsureEventSystem();
			EidraTeamWindow prefab = Resources.Load<EidraTeamWindow>("UI/EidraTeamWindow");
			if (prefab == null)
			{
				throw new InvalidOperationException("Eidra team window prefab is missing.");
			}
			EidraTeamWindow = UnityEngine.Object.Instantiate(prefab);
			EidraTeamWindow.name = "EidraTeamWindow";
			EidraTeamWindow.Initialize(roster, content, input, motor, combat, damageable, sceneFlow, session);
			return EidraTeamWindow;
		}

		public StorageWindow BindStorageWindow(PlayerInventory inventory, ContentDatabase database, PlayerInputReader input, SceneFlowService sceneFlow, SaveGameService saveService = null)
		{
			if (StorageWindow != null)
			{
				return StorageWindow;
			}
			EnsureEventSystem();
			StorageWindow storageWindow = Resources.Load<StorageWindow>("UI/StorageWindow");
			if (storageWindow == null)
			{
				throw new InvalidOperationException("Storage prefab 'Resources/UI/StorageWindow' is missing. Run Eidren/Storage/Build V0.1.");
			}
			StorageWindow = UnityEngine.Object.Instantiate(storageWindow);
			StorageWindow.name = "StorageWindow";
			StorageWindow.Initialize(new ItemTransferService(database), inventory, database, input, motor, combat, damageable, sceneFlow, saveService);
			return StorageWindow;
		}

		public BuildingPlacementController BindBuildingSystem(BuildingService buildings, TechnologyUnlockService technology, ContentDatabase content, GameSession session, PlayerInputReader input, SceneFlowService sceneFlow, ZoneController zone, CraftingWindow crafting, StorageWindow storage, Camera placementCamera)
		{
			if (BuildingPlacement != null)
			{
				return BuildingPlacement;
			}
			EnsureEventSystem();
			BuildingMenuWindow buildingMenuWindow = Resources.Load<BuildingMenuWindow>("UI/BuildingMenuWindow");
			if (buildingMenuWindow == null)
			{
				throw new InvalidOperationException("Building menu prefab is missing.");
			}
			BuildingMenu = UnityEngine.Object.Instantiate(buildingMenuWindow);
			BuildingMenu.name = "BuildingMenuWindow";
			BuildingMenu.Initialize(content, technology, buildings.Materials);
			BuildingPlacement = base.gameObject.AddComponent<BuildingPlacementController>();
			BuildingPlacement.Initialize(buildings, content, session, input, this, zone, BuildingMenu, crafting, storage, technology, sceneFlow, placementCamera);
			return BuildingPlacement;
		}

		public PlayerDeathController BindDeathFlow(PlayerInputReader input, GameSession session, SceneFlowService sceneFlow, string areaName)
		{
			if (DeathController != null)
			{
				return DeathController;
			}
			EnsureEventSystem();
			PlayerDeathWindow playerDeathWindow = Resources.Load<PlayerDeathWindow>("UI/PlayerDeathWindow");
			if (playerDeathWindow == null)
			{
				throw new InvalidOperationException("Death window prefab 'Resources/UI/PlayerDeathWindow' is missing. Run Eidren/Player/Build Death Window.");
			}
			PlayerDeathWindow playerDeathWindow2 = UnityEngine.Object.Instantiate(playerDeathWindow);
			playerDeathWindow2.name = "PlayerDeathWindow";
			DeathController = base.gameObject.AddComponent<PlayerDeathController>();
			DeathController.Initialize(this, input, session, sceneFlow, playerDeathWindow2, areaName);
			return DeathController;
		}

		public InventoryFeedbackPresenter BindInventoryFeedback(PlayerInventory inventory, ContentDatabase database)
		{
			if (InventoryFeedback != null)
			{
				return InventoryFeedback;
			}
			EnsureEventSystem();
			GameObject gameObject = new GameObject("InventoryFeedbackPresenter");
			InventoryFeedback = gameObject.AddComponent<InventoryFeedbackPresenter>();
			InventoryFeedback.Initialize(inventory, database);
			return InventoryFeedback;
		}

		public void ValidateReferences()
		{
			List<string> list = new List<string>();
			AddIfMissing(characterController, "characterController", list);
			AddIfMissing(damageable, "damageable", list);
			AddIfMissing(motor, "motor", list);
			AddIfMissing(eidraTeam, "eidraTeam", list);
			AddIfMissing(combat, "combat", list);
			AddIfMissing(interaction, "interaction", list);
			AddIfMissing(weaponHitbox, "weaponHitbox", list);
			AddIfMissing(consumables, "consumables", list);
			AddIfMissing(visualAnimator, "visualAnimator", list);
			if (PlayerVisual == null)
			{
				list.Add("playerVisual");
			}
			AddIfMissing(weaponVisual, "weaponVisual", list);
			if (list.Count == 0)
			{
				return;
			}
			throw new InvalidOperationException("Player prefab '" + base.name + "' is missing required serialized references: " + string.Join(", ", list) + ". Rebuild it with Eidren/Prefabs/Migrate Player Prefab.");
		}

		private static void AddIfMissing(UnityEngine.Object value, string fieldName, ICollection<string> missing)
		{
			if (value == null)
			{
				missing.Add(fieldName);
			}
		}

		private static void EnsureEventSystem()
		{
			if (!(EventSystem.current != null))
			{
				GameObject gameObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
				gameObject.transform.SetAsLastSibling();
			}
		}
	}
}
