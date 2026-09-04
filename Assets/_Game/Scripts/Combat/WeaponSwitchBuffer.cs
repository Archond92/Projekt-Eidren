using Eidren.Data;

namespace Eidren.Combat
{
	public sealed class WeaponSwitchBuffer
	{
		public bool HasBufferedSwitch { get; private set; }

		public WeaponSwitchRequestResult Request(PlayerAttackPhase phase, AttackStepData step)
		{
			if (WeaponSwitchRules.CanSwitchImmediately(phase, step))
			{
				return WeaponSwitchRequestResult.ExecuteImmediately;
			}
			if (HasBufferedSwitch)
			{
				return WeaponSwitchRequestResult.AlreadyBuffered;
			}
			HasBufferedSwitch = true;
			return WeaponSwitchRequestResult.Buffered;
		}

		public bool Consume()
		{
			if (!HasBufferedSwitch)
			{
				return false;
			}
			HasBufferedSwitch = false;
			return true;
		}

		public void Clear()
		{
			HasBufferedSwitch = false;
		}
	}

	public static class WeaponSwitchRules
	{
		public static bool CanSwitchImmediately(PlayerAttackPhase phase, AttackStepData step)
		{
			if (1 == 0)
			{
			}
			bool result = phase switch
			{
				PlayerAttackPhase.None => true, 
				PlayerAttackPhase.Windup => step.AllowWeaponSwitchDuringWindup, 
				PlayerAttackPhase.HitWindow => false, 
				PlayerAttackPhase.Recovery => step.AllowWeaponSwitchDuringRecovery, 
				_ => false, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}
}
