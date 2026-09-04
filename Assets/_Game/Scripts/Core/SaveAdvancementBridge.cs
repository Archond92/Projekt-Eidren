using System;

namespace Eidren.Core.Services
{
	internal sealed class SaveAdvancementBridge
	{
		private readonly PlayerProgressionService _progression;

		private readonly TechnologyUnlockService _technology;

		private readonly QuestProgressService _quest;

		public SaveAdvancementBridge(PlayerProgressionService progression, TechnologyUnlockService technology, QuestProgressService quest = null)
		{
			_progression = progression;
			_technology = technology;
			_quest = quest;
		}

		public SaveProgressionData Capture()
		{
			PlayerProgressionRuntimeState playerProgressionRuntimeState = _progression?.Capture() ?? new PlayerProgressionRuntimeState();
			playerProgressionRuntimeState.UnlockedTechnologyNodeIds = _technology?.CaptureUnlockedNodeIds() ?? Array.Empty<string>();
			return SaveProgressionMapper.ToSave(playerProgressionRuntimeState);
		}

		public SaveQuestData CaptureQuest()
		{
			return SaveQuestMapper.ToSave(_quest?.CaptureState());
		}

		public void ResetForNewGame()
		{
			_progression?.ResetForNewGame();
			_technology?.ResetForNewGame();
			_quest?.ResetForNewGame();
		}

		public bool TryMap(SaveProgressionData source, out PlayerProgressionRuntimeState state, out string error)
		{
			if (!SaveProgressionMapper.TryToRuntime(source, out state, out error))
			{
				return false;
			}
			if (_technology != null && !_technology.ValidateUnlockedNodeIds(state.UnlockedTechnologyNodeIds, state.Stage, out error))
			{
				state = null;
				return false;
			}
			return true;
		}

		public void Restore(PlayerProgressionRuntimeState state)
		{
			if (state != null)
			{
				_progression?.Restore(state);
				_technology?.Restore(state.UnlockedTechnologyNodeIds);
			}
		}

		/// <summary>
		/// F31-006: Queststand nach dem Fortschritt wiederherstellen. Ein
		/// gespeicherter Stand derselben Kette zählt direkt; fehlt er
		/// (Altbestand, Migration), leitet der Dienst den Stand aus den
		/// Erstlisten und dem Eidra-Bestand ab — ohne EP.
		/// </summary>
		public void RestoreQuest(SaveQuestData saved, bool hasCapturedEidra)
		{
			if (_quest == null)
			{
				return;
			}
			_quest.ResetForNewGame();
			QuestChainRuntimeState state = SaveQuestMapper.ToRuntime(saved);
			if (state == null || !_quest.RestoreState(state))
			{
				PlayerProgressionRuntimeState progression = _progression?.Capture();
				_quest.AlignWithLegacyProgress(progression?.FirstCraftedRecipeIds, progression?.FirstBuiltBuildingIds, hasCapturedEidra);
			}
		}
	}
}
