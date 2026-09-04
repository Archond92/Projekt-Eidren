using System;

namespace Eidren.Core.Services
{
	public sealed class EidraForgeRunState
	{
		internal EidraForgeRunStatus Status;

		internal string RunId = string.Empty;

		internal int LootSeed;

		internal long CompletedUtcTicks;

		internal bool FirstCompletionGranted;

		internal bool RepeatRun;

		internal bool IgnivarCaptured;

		internal bool TeamSelectionPending;

		/// <summary>Ist der Formwall im Einbruch wieder aufgebaut? Ueberlebt Laeufe
		/// und bezahlte Resets, genau wie IgnivarCaptured.</summary>
		internal bool FormwallGebaut;

		/// <summary>Steht der Ertrag dieses Laufs noch zur Abholung? Wird bei jedem
		/// Laufbeginn auf FormwallGebaut gesetzt - daraus folgt, dass der Ertrag
		/// sich nicht ansammelt, ohne dass es dafuer eine Sonderregel braucht.</summary>
		internal bool FormwallErtragOffen;

		internal ForgeEnemyState[] Enemies = Array.Empty<ForgeEnemyState>();

		internal ForgeContainerState[] Chests = Array.Empty<ForgeContainerState>();

		internal ForgeDropState[] Drops = Array.Empty<ForgeDropState>();

		internal ForgeRewardChestState[] RewardChests = Array.Empty<ForgeRewardChestState>();

		internal ItemStack[] RecoverySlots = Array.Empty<ItemStack>();

		internal static EidraForgeRunState CreateUnstarted()
		{
			return new EidraForgeRunState();
		}

		internal EidraForgeRunState Copy()
		{
			return new EidraForgeRunState
			{
				Status = Status,
				RunId = (RunId ?? string.Empty),
				LootSeed = LootSeed,
				CompletedUtcTicks = CompletedUtcTicks,
				FirstCompletionGranted = FirstCompletionGranted,
				RepeatRun = RepeatRun,
				IgnivarCaptured = IgnivarCaptured,
				TeamSelectionPending = TeamSelectionPending,
				FormwallGebaut = FormwallGebaut,
				FormwallErtragOffen = FormwallErtragOffen,
				Enemies = DungeonStateCopy.Enemies(Enemies),
				Chests = DungeonStateCopy.Chests(Chests),
				Drops = DungeonStateCopy.Drops(Drops),
				RewardChests = DungeonStateCopy.Rewards(RewardChests),
				RecoverySlots = DungeonStateCopy.Stacks(RecoverySlots)
			};
		}
	}
}
