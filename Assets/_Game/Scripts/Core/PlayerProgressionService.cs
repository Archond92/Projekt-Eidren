using Eidren.Data;
using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class PlayerProgressionService
	{
		private readonly ProgressionCurveDefinition _curve;

		private readonly HashSet<string> _firstCraftedRecipes = new HashSet<string>(StringComparer.Ordinal);

		private readonly HashSet<string> _firstBuiltBuildings = new HashSet<string>(StringComparer.Ordinal);

		private readonly HashSet<string> _knownBlueprints = new HashSet<string>(StringComparer.Ordinal);

		private int _stage = 1;

		private int _level = 1;

		private int _experience;

		private int _availableTechnologyPoints;

		private int _spentTechnologyPoints;

		public PlayerProgressionSnapshot State
		{
			get
			{
				ProgressionStageCurve progressionStageCurve = CurrentStage();
				return new PlayerProgressionSnapshot(_stage, _level, _experience, progressionStageCurve.ExperienceToNextLevel(_level), progressionStageCurve.MaximumLevel, _availableTechnologyPoints, _spentTechnologyPoints);
			}
		}

		public bool CanChallengeGaron => _level >= _curve.GaronMinimumLevel;

		public event Action<PlayerProgressionSnapshot> Changed;

		public event Action<int> ExperienceGained;

		// F31-006: Quest-Ereignisse — der Questdienst hört hier zu, damit
		// die Aufruforte (Crafting, Bau, Ernte, Kampf) unangetastet bleiben.
		public event Action<string> QuestRecipeCrafted;

		public event Action<string> QuestBuildingConstructed;

		public event Action<string> QuestResourceNodeCompleted;

		public event Action QuestEnemyDefeated;

		public PlayerProgressionService(ProgressionCurveDefinition curve)
		{
			_curve = curve ?? throw new ArgumentNullException("curve");
			string[] validationErrors = curve.GetValidationErrors();
			if (validationErrors.Length != 0)
			{
				throw new ArgumentException(string.Join("; ", validationErrors), "curve");
			}
		}


		public int RecordResourceNodeCompleted(int tier, string nodeDefinitionId = null)
		{
			int awarded = AwardExperience(ExperienceRules.Award(ExperienceSource.ResourceNode, tier));
			if (!string.IsNullOrWhiteSpace(nodeDefinitionId))
			{
				this.QuestResourceNodeCompleted?.Invoke(nodeDefinitionId);
			}
			return awarded;
		}

		public int RecordRecipeCrafted(string recipeId, int tier = 0)
		{
			if (string.IsNullOrWhiteSpace(recipeId))
			{
				return 0;
			}
			bool firstCompletion = _firstCraftedRecipes.Add(recipeId);
			int awarded = AwardExperience(ExperienceRules.Award(ExperienceSource.Recipe, tier, firstCompletion));
			this.QuestRecipeCrafted?.Invoke(recipeId);
			return awarded;
		}

		public int RecordBuildingConstructed(string buildingId, int tier = 0)
		{
			if (string.IsNullOrWhiteSpace(buildingId))
			{
				return 0;
			}
			bool firstCompletion = _firstBuiltBuildings.Add(buildingId);
			int awarded = AwardExperience(ExperienceRules.Award(ExperienceSource.Building, tier, firstCompletion));
			this.QuestBuildingConstructed?.Invoke(buildingId);
			return awarded;
		}

		/// <summary>
		/// F31-006: EP eines abgeschlossenen Questschritts — fester Betrag
		/// aus dem Kettenkatalog, ohne Tier-Staffel.
		/// </summary>
		public int RecordQuestStepCompleted(int experience)
		{
			if (experience < 0)
			{
				throw new ArgumentOutOfRangeException("experience");
			}
			return AwardExperience(experience);
		}

		public int RecordEnemyDefeated(int experience, bool firstGaronVictory = false)
		{
			if (experience < 0)
			{
				throw new ArgumentOutOfRangeException("experience");
			}
			int awarded = AwardExperience(experience + (firstGaronVictory ? 500 : 0));
			this.QuestEnemyDefeated?.Invoke();
			return awarded;
		}

		public bool LearnBlueprint(string blueprintId)
		{
			if (string.IsNullOrWhiteSpace(blueprintId) || !_knownBlueprints.Add(blueprintId))
			{
				return false;
			}
			NotifyChanged();
			return true;
		}

		public bool KnowsBlueprint(string blueprintId)
		{
			return !string.IsNullOrWhiteSpace(blueprintId) && _knownBlueprints.Contains(blueprintId);
		}

		public bool TrySpendTechnologyPoints(int amount)
		{
			if (amount <= 0 || amount > _availableTechnologyPoints)
			{
				return false;
			}
			_availableTechnologyPoints -= amount;
			_spentTechnologyPoints += amount;
			NotifyChanged();
			return true;
		}

		public bool GrantTechnologyPoints(int amount)
		{
			if (amount <= 0)
			{
				return false;
			}
			_availableTechnologyPoints = (int)Math.Min((long)_availableTechnologyPoints + (long)amount, 2147483647L);
			NotifyChanged();
			return true;
		}

		public bool TryAdvanceAfterDungeonCompletion(string dungeonId)
		{
			if (string.IsNullOrWhiteSpace(dungeonId) || !_curve.TryGetStage(_stage + 1, out var curve))
			{
				return false;
			}
			int experience = _experience;
			_stage++;
			_level = Math.Min(_level, curve.MaximumLevel);
			_experience = 0;
			AwardExperience(experience);
			return true;
		}

		public PlayerProgressionRuntimeState Capture()
		{
			return new PlayerProgressionRuntimeState
			{
				Stage = _stage,
				Level = _level,
				Experience = _experience,
				AvailableTechnologyPoints = _availableTechnologyPoints,
				SpentTechnologyPoints = _spentTechnologyPoints,
				FirstCraftedRecipeIds = Sorted(_firstCraftedRecipes),
				FirstBuiltBuildingIds = Sorted(_firstBuiltBuildings),
				KnownBlueprintIds = Sorted(_knownBlueprints)
			};
		}

		public void Restore(PlayerProgressionRuntimeState state)
		{
			PlayerProgressionRuntimeState playerProgressionRuntimeState = state ?? new PlayerProgressionRuntimeState();
			_stage = ((!_curve.TryGetStage(playerProgressionRuntimeState.Stage, out var _)) ? 1 : playerProgressionRuntimeState.Stage);
			ProgressionStageCurve progressionStageCurve = CurrentStage();
			_level = Math.Max(1, Math.Min(playerProgressionRuntimeState.Level, progressionStageCurve.MaximumLevel));
			int num = progressionStageCurve.ExperienceToNextLevel(_level);
			_experience = Math.Max(0, Math.Min(playerProgressionRuntimeState.Experience, (_level >= progressionStageCurve.MaximumLevel) ? num : Math.Max(0, num - 1)));
			_availableTechnologyPoints = Math.Max(0, playerProgressionRuntimeState.AvailableTechnologyPoints);
			_spentTechnologyPoints = Math.Max(0, playerProgressionRuntimeState.SpentTechnologyPoints);
			RestoreIds(_firstCraftedRecipes, playerProgressionRuntimeState.FirstCraftedRecipeIds);
			RestoreIds(_firstBuiltBuildings, playerProgressionRuntimeState.FirstBuiltBuildingIds);
			RestoreIds(_knownBlueprints, playerProgressionRuntimeState.KnownBlueprintIds);
			NotifyChanged();
		}

		public void ResetForNewGame()
		{
			Restore(new PlayerProgressionRuntimeState());
		}

		private int AwardExperience(int amount)
		{
			ProgressionStageCurve progressionStageCurve = CurrentStage();
			if (amount <= 0)
			{
				return 0;
			}
			int num = (int)Math.Min(amount, ExperienceUntilCapacity(progressionStageCurve));
			long num2 = (long)_experience + (long)amount;
			while (_level < progressionStageCurve.MaximumLevel)
			{
				int num3 = progressionStageCurve.ExperienceToNextLevel(_level);
				if (num2 < num3)
				{
					break;
				}
				num2 -= num3;
				_level++;
				_availableTechnologyPoints += progressionStageCurve.TechnologyPointsOnLevel(_level);
			}
			int num4 = progressionStageCurve.ExperienceToNextLevel(_level);
			_experience = (int)Math.Min(num2, (_level >= progressionStageCurve.MaximumLevel) ? num4 : Math.Max(0, num4 - 1));
			NotifyChanged();
			if (num > 0)
			{
				this.ExperienceGained?.Invoke(num);
			}
			return num;
		}

		private long ExperienceUntilCapacity(ProgressionStageCurve stage)
		{
			long num = -_experience;
			for (int i = _level; i < stage.MaximumLevel; i++)
			{
				num += stage.ExperienceToNextLevel(i);
			}
			num += stage.ExperienceToNextLevel(stage.MaximumLevel);
			return Math.Max(0L, num);
		}

		private ProgressionStageCurve CurrentStage()
		{
			_curve.TryGetStage(_stage, out var curve);
			return curve;
		}

		private void NotifyChanged()
		{
			this.Changed?.Invoke(State);
		}


		private static string[] Sorted(HashSet<string> values)
		{
			string[] array = new string[values.Count];
			values.CopyTo(array);
			Array.Sort(array, StringComparer.Ordinal);
			return array;
		}

		private static void RestoreIds(HashSet<string> target, string[] source)
		{
			target.Clear();
			if (source == null)
			{
				return;
			}
			foreach (string text in source)
			{
				if (!string.IsNullOrWhiteSpace(text))
				{
					target.Add(text);
				}
			}
		}
	}

	public sealed class PlayerProgressionRuntimeState
	{
		public int Stage = 1;

		public int Level = 1;

		public int Experience;

		public int AvailableTechnologyPoints;

		public int SpentTechnologyPoints;

		public string[] FirstCraftedRecipeIds = Array.Empty<string>();

		public string[] FirstBuiltBuildingIds = Array.Empty<string>();

		public string[] UnlockedTechnologyNodeIds = Array.Empty<string>();

		public string[] KnownBlueprintIds = Array.Empty<string>();
	}

	public readonly struct PlayerProgressionSnapshot
	{
		public int Stage { get; }

		public int Level { get; }

		public int Experience { get; }

		public int ExperienceToNextLevel { get; }

		public int MaximumLevel { get; }

		public int AvailableTechnologyPoints { get; }

		public int SpentTechnologyPoints { get; }

		public PlayerProgressionSnapshot(int stage, int level, int experience, int experienceToNextLevel, int maximumLevel, int availableTechnologyPoints, int spentTechnologyPoints)
		{
			Stage = stage;
			Level = level;
			Experience = experience;
			ExperienceToNextLevel = experienceToNextLevel;
			MaximumLevel = maximumLevel;
			AvailableTechnologyPoints = availableTechnologyPoints;
			SpentTechnologyPoints = spentTechnologyPoints;
		}
	}
}
