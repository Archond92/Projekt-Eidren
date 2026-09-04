using System;

namespace Eidren.Core.Services
{
	public sealed class EidraForgeCompletionService
	{
		private readonly GameSession _session;

		private readonly PlayerProgressionService _progression;

		public EidraForgeCompletionService(GameSession session, PlayerProgressionService progression)
		{
			_session = session ?? throw new ArgumentNullException("session");
			_progression = progression ?? throw new ArgumentNullException("progression");
		}

		public bool CompleteCoreGuardian(DateTime utcNow)
		{
			if (!_session.EidraForge.TryComplete(utcNow, out var firstCompletion))
			{
				return false;
			}
			if (firstCompletion)
			{
				_session.TrySetProgressFlag("tier_2_unlocked");
				_progression.TryAdvanceAfterDungeonCompletion("dungeon.eidra_forge");
				_progression.GrantTechnologyPoints(1);
			}
			_progression.RecordEnemyDefeated(500 + (firstCompletion ? 900 : 0));
			_session.RequestSave(SaveRequestReason.BossDefeated);
			return true;
		}
	}
}
