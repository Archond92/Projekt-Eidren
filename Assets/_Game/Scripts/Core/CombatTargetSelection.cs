using System;
using System.Collections.Generic;

namespace Eidren.Core
{
	public readonly struct CombatTargetCandidate
	{
		public readonly int Id;
		public readonly float Distance, Alignment, StateBonus;
		public readonly bool InWeaponRange, Boss, Reachable;
		public CombatTargetCandidate(int id, float distance, float alignment, bool inWeaponRange, bool boss = false, bool reachable = true, float stateBonus = 0f)
		{
			Id = id; Distance = distance; Alignment = alignment; InWeaponRange = inWeaponRange;
			Boss = boss; Reachable = reachable; StateBonus = stateBonus;
		}
	}

	public sealed class CombatTargetSelectionSettings
	{
		public float AcquisitionRange { get; }
		public float ReleaseRange { get; }
		public float SwitchAdvantage { get; }
		public float LossGrace { get; }
		public float RecentDuration { get; }
		public float DistanceWeight => 3f;
		public float DirectionWeight => 2f;
		public float WeaponRangeBonus => 4f;
		public float BossBonus => .35f;
		public float RecentBonus => .8f;
		public CombatTargetSelectionSettings(float acquisitionRange = 12f, float releaseRange = 14f, float switchAdvantage = .65f, float lossGrace = .25f, float recentDuration = 1.2f)
		{
			if (!Finite(acquisitionRange) || !Finite(releaseRange) || !Finite(switchAdvantage) || !Finite(lossGrace) || !Finite(recentDuration)
				|| acquisitionRange <= 0 || releaseRange < acquisitionRange || switchAdvantage < 0 || lossGrace < 0 || recentDuration < 0)
				throw new ArgumentOutOfRangeException(nameof(acquisitionRange));
			AcquisitionRange = acquisitionRange; ReleaseRange = releaseRange; SwitchAdvantage = switchAdvantage;
			LossGrace = lossGrace; RecentDuration = recentDuration;
		}
		internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
	}

	/// <summary>Reine Entscheidung ohne Unity: fehlend = hart ungueltig, unerreichbar = kurze Gnadenzeit.</summary>
	public sealed class CombatTargetSelection
	{
		private readonly CombatTargetSelectionSettings _settings;
		private int _recentId;
		private float _recentAt = float.NegativeInfinity;
		public int CurrentId { get; private set; }
		public float SelectedAt { get; private set; }
		public float ConfirmedAt { get; private set; }
		public CombatTargetSelection(CombatTargetSelectionSettings settings) { _settings = settings ?? throw new ArgumentNullException(nameof(settings)); }
		public void ConfirmEngagement(int id, float now) { _recentId = id; _recentAt = now; }
		public void Clear() { CurrentId = 0; SelectedAt = ConfirmedAt = 0f; _recentId = 0; _recentAt = float.NegativeInfinity; }

		public int Select(IReadOnlyList<CombatTargetCandidate> candidates, float now)
		{
			if (!CombatTargetSelectionSettings.Finite(now)) throw new ArgumentOutOfRangeException(nameof(now));
			int bestId = 0;
			float bestScore = float.NegativeInfinity, currentScore = float.NegativeInfinity;
			bool currentPresent = false, currentEligible = false;
			for (int i = 0; i < candidates.Count; i++)
			{
				CombatTargetCandidate candidate = candidates[i];
				bool eligible = Eligible(candidate);
				float score = eligible ? Score(candidate, now) : float.NegativeInfinity;
				if (candidate.Id == CurrentId && candidate.Id != 0 && candidate.Distance <= _settings.ReleaseRange)
				{
					currentPresent = true; currentEligible = eligible; currentScore = score;
				}
				if (eligible && (score > bestScore || (score == bestScore && (bestId == 0 || candidate.Id < bestId))))
				{
					bestId = candidate.Id; bestScore = score;
				}
			}
			if (currentPresent)
			{
				if (currentEligible)
				{
					ConfirmedAt = now;
					if (bestId == CurrentId || bestScore <= currentScore + _settings.SwitchAdvantage) return CurrentId;
				}
				else if (now - ConfirmedAt < _settings.LossGrace) return CurrentId;
			}
			if (CurrentId != bestId) SelectedAt = now;
			CurrentId = bestId;
			ConfirmedAt = bestId == 0 ? 0f : now;
			return CurrentId;
		}

		public bool Eligible(CombatTargetCandidate candidate) => candidate.Id != 0 && candidate.Reachable
			&& CombatTargetSelectionSettings.Finite(candidate.Distance) && candidate.Distance >= 0 && candidate.Distance <= _settings.AcquisitionRange
			&& CombatTargetSelectionSettings.Finite(candidate.Alignment) && CombatTargetSelectionSettings.Finite(candidate.StateBonus);

		public float Score(CombatTargetCandidate candidate, float now) =>
			_settings.DistanceWeight * (1f - candidate.Distance / _settings.AcquisitionRange)
			+ _settings.DirectionWeight * (Math.Max(-1f, Math.Min(1f, candidate.Alignment)) + 1f) * .5f
			+ (candidate.InWeaponRange ? _settings.WeaponRangeBonus : 0f)
			+ (candidate.Boss ? _settings.BossBonus : 0f) + candidate.StateBonus
			+ (candidate.Id == _recentId && now >= _recentAt && now - _recentAt <= _settings.RecentDuration ? _settings.RecentBonus : 0f);
	}
}
