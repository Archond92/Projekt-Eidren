using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	public enum QuestConditionKind
	{
		ResourceNodeCompleted,
		RecipeCrafted,
		BuildingConstructed,
		EnemyDefeated,
		EidraCaptured
	}

	/// <summary>
	/// Reiner Schrittdatensatz der Questkette — die Logik lebt im
	/// QuestProgressService (Core); die Daten liegen hier in der
	/// Data-Schicht, weil Core auf Data verweist, nicht umgekehrt.
	/// </summary>
	public readonly struct QuestStepData
	{
		public string Id { get; }

		public string DisplayText { get; }

		public QuestConditionKind Condition { get; }

		public string TargetId { get; }

		public int TargetCount { get; }

		public int ExperienceReward { get; }

		public QuestStepData(string id, string displayText, QuestConditionKind condition, string targetId, int targetCount, int experienceReward)
		{
			Id = id;
			DisplayText = displayText;
			Condition = condition;
			TargetId = targetId;
			TargetCount = targetCount;
			ExperienceReward = experienceReward;
		}
	}

	/// <summary>
	/// F31-006: Katalog der Tutorial-Questkette — geordnete Schritte mit
	/// Bedingung und EP, gebaut vom QuestChainContentBuilder. Zur Laufzeit
	/// wird die Kette in reine QuestStepData übersetzt; die Logik lebt im
	/// QuestProgressService.
	/// </summary>
	[CreateAssetMenu(menuName = "Eidren/Progression/Quest Chain")]
	public sealed class QuestChainDefinition : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private QuestStepEntry[] steps = Array.Empty<QuestStepEntry>();

		public string Id => id;

		public IReadOnlyList<QuestStepEntry> Steps => steps ?? Array.Empty<QuestStepEntry>();

		public List<QuestStepData> BuildSteps()
		{
			List<QuestStepData> list = new List<QuestStepData>(Steps.Count);
			foreach (QuestStepEntry entry in Steps)
			{
				list.Add(new QuestStepData(entry.Id, entry.DisplayText, entry.Condition, entry.TargetId, entry.TargetCount, entry.ExperienceReward));
			}
			return list;
		}
	}

	[Serializable]
	public struct QuestStepEntry
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private string displayText;

		[SerializeField]
		private QuestConditionKind condition;

		[SerializeField]
		private string targetId;

		[SerializeField]
		[Min(1f)]
		private int targetCount;

		[SerializeField]
		[Min(0f)]
		private int experienceReward;

		public string Id => id;

		public string DisplayText => displayText;

		public QuestConditionKind Condition => condition;

		public string TargetId => targetId;

		public int TargetCount => Mathf.Max(1, targetCount);

		public int ExperienceReward => Mathf.Max(0, experienceReward);
	}
}
