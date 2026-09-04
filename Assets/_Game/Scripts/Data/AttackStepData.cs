using System;

namespace Eidren.Data
{
	[Serializable]
	public struct AttackStepData
	{
		public float Duration;

		public float HitTime;

		public float HitWindowDuration;

		public float HitboxRange;

		public float HitboxAngle;

		public bool IsOmnidirectional;

		public bool AllowWeaponSwitchDuringWindup;

		public bool AllowWeaponSwitchDuringRecovery;

		public float DamageMultiplier;

		public float StaggerMultiplier;

		public float ForwardMotion;

		public float ComboQueueStart;
	}
}
