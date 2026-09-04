using System;

namespace Eidren.Core.Services
{
	public sealed class CoreGuardianCycle
	{
		public const float NormalOpenSeconds = 8f;

		public const float StaggerOpenSeconds = 6f;

		private int _closedAttacks;

		private float _remaining;

		private float _stagger;

		public CoreGuardianPhase Phase { get; private set; }

		public float Protection
		{
			get
			{
				if (Phase != CoreGuardianPhase.Open)
				{
					return 0.35f;
				}
				return 0f;
			}
		}

		public float RemainingSeconds => _remaining;

		public int HazardCount(float healthFraction)
		{
			if (!(healthFraction < 0.5f))
			{
				return 1;
			}
			return 2;
		}

		public float IntervalMultiplier(float healthFraction)
		{
			if (!(healthFraction < 0.5f))
			{
				return 1f;
			}
			return 0.9f;
		}

		public bool RecordClosedAttack()
		{
			if (Phase != CoreGuardianPhase.Closed)
			{
				return false;
			}
			_closedAttacks++;
			if (_closedAttacks < 3)
			{
				return false;
			}
			Phase = CoreGuardianPhase.Overheating;
			_closedAttacks = 0;
			return true;
		}

		public bool FinishOverheat()
		{
			if (Phase != CoreGuardianPhase.Overheating)
			{
				return false;
			}
			Open(8f);
			return true;
		}

		public bool AddStagger(float amount)
		{
			if (Phase != CoreGuardianPhase.Closed || amount <= 0f)
			{
				return false;
			}
			_stagger = Math.Min(400f, _stagger + amount);
			if (_stagger < 400f)
			{
				return false;
			}
			Open(6f);
			return true;
		}

		public bool Tick(float deltaTime)
		{
			if (Phase != CoreGuardianPhase.Open || deltaTime <= 0f)
			{
				return false;
			}
			_remaining = Math.Max(0f, _remaining - Math.Min(deltaTime, 0.5f));
			if (_remaining > 0f)
			{
				return false;
			}
			Phase = CoreGuardianPhase.Closed;
			_stagger = 0f;
			return true;
		}

		public float ProtectionWithMoltenBrand(bool active)
		{
			if (Phase != CoreGuardianPhase.Open)
			{
				if (!active)
				{
					return 0.35f;
				}
				return 0.2f;
			}
			return 0f;
		}

		private void Open(float duration)
		{
			Phase = CoreGuardianPhase.Open;
			_remaining = duration;
			_stagger = 0f;
		}
	}
}
