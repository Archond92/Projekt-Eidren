namespace Eidren.Core.Services
{
	/// <summary>
	/// F31-006: Abbildung zwischen dem Queststand im Spielstand und dem
	/// Laufzeitzustand des QuestProgressService.
	/// </summary>
	public static class SaveQuestMapper
	{
		public static SaveQuestData ToSave(QuestChainRuntimeState source)
		{
			if (source == null)
			{
				return new SaveQuestData();
			}
			return new SaveQuestData
			{
				chainId = source.ChainId ?? string.Empty,
				stepIndex = source.StepIndex,
				stepProgress = source.StepProgress,
				completed = source.Completed
			};
		}

		public static QuestChainRuntimeState ToRuntime(SaveQuestData source)
		{
			if (source == null || string.IsNullOrWhiteSpace(source.chainId))
			{
				return null;
			}
			return new QuestChainRuntimeState
			{
				ChainId = source.chainId,
				StepIndex = source.stepIndex,
				StepProgress = source.stepProgress,
				Completed = source.completed
			};
		}
	}
}
