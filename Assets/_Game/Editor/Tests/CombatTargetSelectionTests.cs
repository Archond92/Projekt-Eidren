using Eidren.Core;
using NUnit.Framework;

namespace Eidren.Tests.EditMode
{
	public sealed class CombatTargetSelectionTests
	{
		private static CombatTargetCandidate C(int id, float distance = 2f, float alignment = 1f, bool weapon = true, bool boss = false, bool reachable = true)
			=> new CombatTargetCandidate(id, distance, alignment, weapon, boss, reachable);
		private static CombatTargetSelection Selector() => new CombatTargetSelection(new CombatTargetSelectionSettings());

		[Test] public void FrontBeatsRear() => Assert.That(Selector().Select(new[] { C(1, alignment: -1), C(2) }, 0), Is.EqualTo(2));
		[Test] public void TieIsIndependentOfRegistryOrder()
		{
			Assert.That(Selector().Select(new[] { C(2), C(1) }, 0), Is.EqualTo(1));
			Assert.That(Selector().Select(new[] { C(1), C(2) }, 0), Is.EqualTo(1));
		}
		[Test] public void SimilarCandidatesDoNotFlicker()
		{
			var selector = Selector(); selector.Select(new[] { C(2) }, 0);
			Assert.That(selector.Select(new[] { C(1, 1.9f), C(2) }, 1), Is.EqualTo(2));
			Assert.That(selector.SelectedAt, Is.EqualTo(0));
		}
		[Test] public void ClearAdvantageSwitches()
		{
			var selector = Selector(); selector.Select(new[] { C(2, 8, weapon: false) }, 0);
			Assert.That(selector.Select(new[] { C(2, 8, weapon: false), C(1) }, .1f), Is.EqualTo(1));
		}
		[Test] public void NearbyFieldEnemyBeatsRemoteBoss() => Assert.That(Selector().Select(new[] { C(1, 10, weapon: false, boss: true), C(2) }, 0), Is.EqualTo(2));
		[Test] public void LossGraceExpiresAndDoesNotRenewItself()
		{
			var selector = Selector(); selector.Select(new[] { C(1) }, 0);
			Assert.That(selector.Select(new[] { C(1, 13), C(2) }, .1f), Is.EqualTo(1));
			Assert.That(selector.Select(new[] { C(1, 13), C(2) }, .3f), Is.EqualTo(2));
		}
		[Test] public void RemovedOrFarAwayTargetHasNoGrace()
		{
			var selector = Selector(); selector.Select(new[] { C(1) }, 0);
			Assert.That(selector.Select(new[] { C(1, 15) }, .01f), Is.Zero);
			selector.Select(new[] { C(1) }, 1);
			Assert.That(selector.Select(new CombatTargetCandidate[0], 1.01f), Is.Zero);
		}
		[Test] public void BriefOcclusionKeepsIdentityButCannotAcquireBlockedTarget()
		{
			var selector = Selector(); Assert.That(selector.Select(new[] { C(1, reachable: false) }, 0), Is.Zero);
			selector.Select(new[] { C(1) }, 1);
			Assert.That(selector.Select(new[] { C(1, reachable: false) }, 1.1f), Is.EqualTo(1));
			Assert.That(selector.Select(new[] { C(1, reachable: false) }, 1.3f), Is.Zero);
		}
		[Test] public void RecentEngagementIsBoundedAndExpires()
		{
			var selector = Selector(); selector.ConfirmEngagement(2, 0);
			Assert.That(selector.Select(new[] { C(1), C(2) }, .1f), Is.EqualTo(2));
			Assert.That(selector.Score(C(1), 2), Is.EqualTo(selector.Score(C(2), 2)));
		}
		[Test] public void WeaponRangeChangeCanChangePreferredTarget()
		{
			var selector = Selector(); selector.Select(new[] { C(1), C(2, 3, weapon: false) }, 0);
			Assert.That(selector.Select(new[] { C(1, weapon: false), C(2, 3) }, 1), Is.EqualTo(2));
		}
		[Test] public void ClearRemovesHistory()
		{
			var selector = Selector(); selector.ConfirmEngagement(2, 0); selector.Select(new[] { C(2) }, 0); selector.Clear();
			Assert.That(selector.Select(new[] { C(1), C(2) }, .1f), Is.EqualTo(1));
		}
		[Test] public void InvalidNumericsCannotBecomeTargets() => Assert.That(Selector().Select(new[] { C(1, float.NaN), C(2, alignment: float.PositiveInfinity) }, 0), Is.Zero);
	}
}
