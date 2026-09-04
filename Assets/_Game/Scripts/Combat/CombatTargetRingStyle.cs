using UnityEngine;

namespace Eidren.Combat
{
	public readonly struct CombatTargetRingStyle
	{
		public readonly Color PrimaryColor;
		public readonly Color AccentColor;
		public readonly float RadiusFactor, MaximumRadius, Width, PulseAmount, PulseSpeed;
		public readonly bool HasAccent;

		public CombatTargetRingStyle(Color primary, Color accent, float radiusFactor, float maximumRadius,
			float width, float pulseAmount, float pulseSpeed, bool hasAccent)
		{
			PrimaryColor = primary; AccentColor = accent; RadiusFactor = radiusFactor;
			MaximumRadius = maximumRadius; Width = width; PulseAmount = pulseAmount;
			PulseSpeed = pulseSpeed; HasAccent = hasAccent;
		}
	}

	public static class CombatTargetRingStyles
	{
		public static CombatTargetRingStyle Resolve(CombatTargetCategory category)
		{
			switch (category)
			{
				case CombatTargetCategory.Elite:
					return new CombatTargetRingStyle(new Color(1f, .42f, .08f, .92f), new Color(1f, .78f, .28f, .9f), 1.20f, 3f, .095f, .045f, 2.2f, true);
				case CombatTargetCategory.Boss:
					return new CombatTargetRingStyle(new Color(.95f, .08f, .14f, .96f), new Color(1f, .58f, .16f, .94f), 1.30f, 5f, .13f, .065f, 1.65f, true);
				default:
					return new CombatTargetRingStyle(new Color(.92f, .16f, .10f, .86f), new Color(1f, .55f, .18f, .75f), 1.14f, 2.7f, .075f, .025f, 1.9f, false);
			}
		}
	}
}

