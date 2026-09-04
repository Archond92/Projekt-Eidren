using Eidren.Data;
using System;
using System.Collections.Generic;

namespace Eidren.Core.Services
{
	[Serializable]
	public sealed class QuestChainRuntimeState
	{
		public string ChainId;

		public int StepIndex;

		public int StepProgress;

		public bool Completed;
	}

	/// <summary>
	/// F31-006: Führt die Tutorial-Questkette. Schritte docken ausschließlich
	/// an bestehende Spielereignisse an; EP vergibt der Aufrufer über das
	/// übergebene Delegate (rein testbar, kein Unity-Bezug). Für
	/// Altbestände merkt sich der Dienst die schon erfüllten Rezept- und
	/// Gebäudeschritte und hakt sie beim Erreichen ohne EP ab.
	/// </summary>
	public sealed class QuestProgressService
	{
		private readonly string _chainId;

		private readonly IReadOnlyList<QuestStepData> _steps;

		private readonly Action<int> _awardExperience;

		private readonly HashSet<string> _legacyCraftedRecipes = new HashSet<string>(StringComparer.Ordinal);

		private readonly HashSet<string> _legacyBuiltBuildings = new HashSet<string>(StringComparer.Ordinal);

		private int _stepIndex;

		private int _stepProgress;

		private bool _completed;

		public QuestProgressService(string chainId, IReadOnlyList<QuestStepData> steps, Action<int> awardExperience)
		{
			if (string.IsNullOrWhiteSpace(chainId))
			{
				throw new ArgumentException("A stable quest chain ID is required.", "chainId");
			}
			_chainId = chainId;
			_steps = steps ?? throw new ArgumentNullException("steps");
			_awardExperience = awardExperience;
			_completed = _steps.Count == 0;
		}

		public event Action<int> StepChanged;

		public event Action ChainCompleted;

		public int CurrentStepIndex => _completed ? _steps.Count : _stepIndex;

		public int CurrentStepProgress => _completed ? 0 : _stepProgress;

		public bool IsCompleted => _completed;

		public IReadOnlyList<QuestStepData> Steps => _steps;

		public bool TryGetCurrentStep(out QuestStepData step)
		{
			if (_completed || _stepIndex >= _steps.Count)
			{
				step = default;
				return false;
			}
			step = _steps[_stepIndex];
			return true;
		}

		public void NotifyResourceNodeCompleted(string nodeId)
		{
			Handle(QuestConditionKind.ResourceNodeCompleted, nodeId);
		}

		public void NotifyRecipeCrafted(string recipeId)
		{
			Handle(QuestConditionKind.RecipeCrafted, recipeId);
		}

		public void NotifyBuildingConstructed(string buildingId)
		{
			Handle(QuestConditionKind.BuildingConstructed, buildingId);
		}

		public void NotifyEnemyDefeated()
		{
			Handle(QuestConditionKind.EnemyDefeated, null);
		}

		public void NotifyEidraCaptured()
		{
			Handle(QuestConditionKind.EidraCaptured, null);
		}

		public QuestChainRuntimeState CaptureState()
		{
			return new QuestChainRuntimeState
			{
				ChainId = _chainId,
				StepIndex = CurrentStepIndex,
				StepProgress = CurrentStepProgress,
				Completed = _completed
			};
		}

		public bool RestoreState(QuestChainRuntimeState state)
		{
			if (state == null || !string.Equals(state.ChainId, _chainId, StringComparison.Ordinal))
			{
				return false;
			}
			_stepIndex = Math.Max(0, Math.Min(state.StepIndex, _steps.Count));
			_stepProgress = Math.Max(0, state.StepProgress);
			_completed = state.Completed || _stepIndex >= _steps.Count;
			return true;
		}

		public void ResetForNewGame()
		{
			_stepIndex = 0;
			_stepProgress = 0;
			_completed = _steps.Count == 0;
			_legacyCraftedRecipes.Clear();
			_legacyBuiltBuildings.Clear();
		}

		/// <summary>
		/// Migration von Altbeständen: Ein vorhandener Eidra schließt die
		/// Kette ab (ohne Abschlussmeldung — das Ereignis bleibt echten
		/// Fängen vorbehalten). Bereits gefertigte Rezepte und gebaute
		/// Gebäude werden gemerkt und beim Erreichen ohne EP übersprungen —
		/// EP fließen nur für echte, neue Abschlüsse.
		/// </summary>
		public void AlignWithLegacyProgress(IReadOnlyCollection<string> firstCraftedRecipeIds, IReadOnlyCollection<string> firstBuiltBuildingIds, bool hasCapturedEidra)
		{
			if (hasCapturedEidra)
			{
				_completed = true;
				return;
			}
			if (firstCraftedRecipeIds != null)
			{
				_legacyCraftedRecipes.UnionWith(firstCraftedRecipeIds);
			}
			if (firstBuiltBuildingIds != null)
			{
				_legacyBuiltBuildings.UnionWith(firstBuiltBuildingIds);
			}
			if (!_completed)
			{
				SkipLegacySatisfiedSteps();
				if (_stepIndex >= _steps.Count)
				{
					_completed = true;
				}
			}
		}

		private void Handle(QuestConditionKind kind, string id)
		{
			if (_completed || _stepIndex >= _steps.Count)
			{
				return;
			}
			QuestStepData step = _steps[_stepIndex];
			if (step.Condition != kind)
			{
				return;
			}
			if (!string.IsNullOrEmpty(step.TargetId) && !string.Equals(step.TargetId, id, StringComparison.Ordinal))
			{
				return;
			}
			_stepProgress++;
			if (_stepProgress < Math.Max(1, step.TargetCount))
			{
				return;
			}
			if (step.ExperienceReward > 0)
			{
				_awardExperience?.Invoke(step.ExperienceReward);
			}
			_stepIndex++;
			_stepProgress = 0;
			SkipLegacySatisfiedSteps();
			if (_stepIndex >= _steps.Count)
			{
				_completed = true;
				this.ChainCompleted?.Invoke();
			}
			else
			{
				this.StepChanged?.Invoke(_stepIndex);
			}
		}

		private void SkipLegacySatisfiedSteps()
		{
			while (_stepIndex < _steps.Count && IsLegacySatisfied(_steps[_stepIndex]))
			{
				_stepIndex++;
				_stepProgress = 0;
			}
		}

		private bool IsLegacySatisfied(in QuestStepData step)
		{
			if (string.IsNullOrEmpty(step.TargetId))
			{
				return false;
			}
			return step.Condition switch
			{
				QuestConditionKind.RecipeCrafted => _legacyCraftedRecipes.Contains(step.TargetId),
				QuestConditionKind.BuildingConstructed => _legacyBuiltBuildings.Contains(step.TargetId),
				_ => false
			};
		}
	}
}
