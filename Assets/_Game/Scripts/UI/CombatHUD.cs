using Eidren.AI;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Eidra;
using Eidren.Gameplay.Flow;
using Eidren.Input;
using Eidren.Interaction;
using Eidren.Player;
using System;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class CombatHUD : MonoBehaviour
	{
		[SerializeField]
		private Canvas canvas;

		[SerializeField]
		private SafeAreaPanel safeArea;

		[SerializeField]
		private CombatHudStatusPresenter status;

		[SerializeField]
		private CombatHudActionPresenter actions;

		[SerializeField]
		private CombatHudInteractionPresenter interactionView;

		[SerializeField]
		private CombatHudInputPresenter inputView;

		[SerializeField]
		private CombatHudResultPresenter result;

		// N04-001: Die Minimap wird vom Editor-Builder ins Prefab autoriert (§16).
		// Sie steht bewusst nicht in ValidateReferences — ein HUD ohne Karte bleibt
		// lauffaehig; dass sie im Prefab haengt, prueft CombatHudPrefabTests.
		[SerializeField]
		private MinimapPresenter minimap;

		public Canvas Canvas => canvas;

		public MinimapPresenter Minimap => minimap;

		public SafeAreaPanel SafeArea => safeArea;

		public CombatHudActionPresenter Actions => actions;

		public CombatHudInteractionPresenter InteractionView => interactionView;

		public CombatHudInputPresenter InputView => inputView;

		public void ConfigureReferences(Canvas authoredCanvas, SafeAreaPanel authoredSafeArea, CombatHudStatusPresenter statusPresenter, CombatHudActionPresenter actionPresenter, CombatHudInteractionPresenter interactionPresenter, CombatHudInputPresenter inputPresenter, CombatHudResultPresenter resultPresenter)
		{
			canvas = authoredCanvas;
			safeArea = authoredSafeArea;
			status = statusPresenter;
			actions = actionPresenter;
			interactionView = interactionPresenter;
			inputView = inputPresenter;
			result = resultPresenter;
		}

		public void ConfigureMinimap(MinimapPresenter presenter)
		{
			minimap = presenter;
		}

		public void Initialize(PlayerInputReader input, Damageable playerHealth, PlayerMotor motor, PlayerCombatController combat, ConsumableController consumables, PlayerInventory inventory, PlayerProgressionService progression, EidraTeamController eidra, BossController boss, InteractionController interaction, GameFlowController flow, Camera view = null)
		{
			ValidateReferences();
			if (input == null || playerHealth == null || motor == null || combat == null || consumables == null || inventory == null || progression == null || interaction == null)
			{
				throw new ArgumentNullException("input", "CombatHUD received an incomplete gameplay binding.");
			}
			status.Bind(playerHealth, motor, eidra, boss, flow, progression);
			actions.Bind(input, motor, combat, consumables, inventory, eidra);
			interactionView.Bind(input, interaction);
			inputView.Bind(input);
			result.Bind(input, flow);
			if (minimap != null)
			{
				minimap.Bind(motor, view);
			}
			interaction.RefreshTargetsNow();
		}

		private void ValidateReferences()
		{
			if (canvas == null || safeArea == null || status == null || actions == null || interactionView == null || inputView == null || result == null)
			{
				throw new InvalidOperationException("CombatHUD.prefab is incomplete. Re-author the prefab; runtime hierarchy fallbacks are forbidden.");
			}
		}
	}
}
