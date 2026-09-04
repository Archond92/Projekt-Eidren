using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(fileName = "ProgressionCurve_V01", menuName = "Eidren/Progression/Curve")]
	public sealed class ProgressionCurveDefinition : ScriptableObject
	{
	// F31-004: Die frueheren Felder gatheringExperiencePerUnit,
	// first/repeatCraftExperience, first/repeatBuildingExperience sowie die
	// serialisierten Stufen-Arrays waren tote Stellschrauben: Die wirksamen
	// Werte kommen aus ExperienceRules (25/50/90, 40/75/125) und
	// ProgressionFormula (auf 25 gerundet 125 + 55 x Level). Entfernt, damit
	// niemand wirkungslos daran dreht.





		[SerializeField]
		[Min(1f)]
		private int garonMinimumLevel = 18;

		[SerializeField]
		private ProgressionStageCurve[] stages = Array.Empty<ProgressionStageCurve>();






		public int GaronMinimumLevel => garonMinimumLevel;

		public bool TryGetStage(int stage, out ProgressionStageCurve curve)
		{
			if (stages != null)
			{
				ProgressionStageCurve[] array = stages;
				foreach (ProgressionStageCurve progressionStageCurve in array)
				{
					if (progressionStageCurve != null && progressionStageCurve.Stage == stage)
					{
						curve = progressionStageCurve;
						return true;
					}
				}
			}
			curve = null;
			return false;
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (stages == null || stages.Length == 0)
			{
				list.Add("At least progression stage 1 is required.");
				return list.ToArray();
			}
			HashSet<int> hashSet = new HashSet<int>();
			ProgressionStageCurve[] array = stages;
			foreach (ProgressionStageCurve progressionStageCurve in array)
			{
				if (progressionStageCurve == null)
				{
					list.Add("A progression stage is missing.");
					continue;
				}
				if (!hashSet.Add(progressionStageCurve.Stage))
				{
					list.Add($"Progression stage {progressionStageCurve.Stage} is duplicated.");
				}
				list.AddRange(progressionStageCurve.GetValidationErrors());
			}
			if (!hashSet.Contains(1))
			{
				list.Add("Progression stage 1 is missing.");
			}
			if (TryGetStage(1, out var curve) && (curve.MaximumLevel != 24 || garonMinimumLevel < 1 || garonMinimumLevel > curve.MaximumLevel))
			{
				list.Add("V0.2 requires stage 1 max level 24 and a reachable Garon minimum level.");
			}
			return list.ToArray();
		}
	}

	public static class ProgressionFormula
	{
		public static int ExperienceToNextLevel(int currentLevel)
		{
			if (currentLevel < 1)
			{
				throw new ArgumentOutOfRangeException("currentLevel");
			}
			long num = 125 + 55L * (long)currentLevel;
			long val = (num + 12) / 25 * 25;
			return (int)Math.Min(val, 2147483647L);
		}
	}

	[Serializable]
	public sealed class ProgressionStageCurve
	{
		[SerializeField]
		[Min(1f)]
		private int stage = 1;

		[SerializeField]
		[Min(1f)]
		private int maximumLevel = 20;



		public int Stage => stage;

		public int MaximumLevel => maximumLevel;

		public int ExperienceToNextLevel(int level)
		{
			return (level >= 1 && level <= maximumLevel) ? ProgressionFormula.ExperienceToNextLevel(level) : 0;
		}

		public int TechnologyPointsOnLevel(int level)
		{
			int num = stage;
			if (1 == 0)
			{
			}
			int result;
			if (num != 1)
			{
				if (num != 2 || level < 25 || level > 29)
				{
					goto IL_0051;
				}
				result = 1;
			}
			else if (level >= 2 && level <= 19)
			{
				result = 1;
			}
			else if (level == 20)
			{
				result = 2;
			}
			else
			{
				if (level < 21 || level > 24)
				{
					goto IL_0051;
				}
				result = 1;
			}
			goto IL_0055;
			IL_0051:
			result = 0;
			goto IL_0055;
			IL_0055:
			if (1 == 0)
			{
			}
			return result;
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (stage < 1)
			{
				list.Add("Progression stage must be positive.");
			}
			if (maximumLevel < 1)
			{
				list.Add($"Stage {stage} has no valid maximum level.");
			}
			return list.ToArray();
		}
	}
}
