using Eidren.Data;
using UnityEngine;

namespace Eidren.Core.Services
{
	public static class CaptureResolution
	{
		public const string NoDeviceReason = "FANGGERÄT FEHLT";

		public const string NoBatteryReason = "BATTERIE FEHLT";

		public const string UnknownChargeReason = "BATTERIESTUFE NICHT HINTERLEGT";

		public const string TargetGoneReason = "ZIEL NICHT VERFÜGBAR";

		public static float RequirementAt(in EidraCaptureProfile profile, float healthFraction)
		{
			float t = Mathf.Clamp01(healthFraction);
			return Mathf.Lerp(profile.MinimumRequirement, profile.RequirementAtFullHealth, t);
		}

		public static bool IsBelowFleeThreshold(in EidraCaptureProfile profile, float healthFraction)
		{
			return Mathf.Clamp01(healthFraction) < profile.FleeHealthFraction;
		}

		public static float FleeHealthFloor(in EidraCaptureProfile profile, float maximumHealth)
		{
			return Mathf.Max(0f, maximumHealth) * profile.FleeHealthFraction;
		}

		public static float ClampDamageAboveFlee(in EidraCaptureProfile profile, float maximumHealth, float currentHealth, float healthDamage)
		{
			float num = FleeHealthFloor(in profile, maximumHealth);
			float max = Mathf.Max(0f, currentHealth - num);
			return Mathf.Clamp(healthDamage, 0f, max);
		}

		public static CaptureCheck Evaluate(in EidraCaptureProfile profile, float healthFraction, in CaptureLoadout loadout)
		{
			float num = RequirementAt(in profile, healthFraction);
			if (!loadout.HasDevice)
			{
				return new CaptureCheck(canCapture: false, num, 0f, "FANGGERÄT FEHLT");
			}
			if (!loadout.HasBattery)
			{
				return new CaptureCheck(canCapture: false, num, 0f, "BATTERIE FEHLT");
			}
			if (!loadout.ChargeIsKnown)
			{
				return new CaptureCheck(canCapture: false, num, 0f, "BATTERIESTUFE NICHT HINTERLEGT");
			}
			if (loadout.Charge < num)
			{
				return new CaptureCheck(canCapture: false, num, loadout.Charge, $"LADUNG {loadout.Charge:0} / BEDARF {num:0}");
			}
			return new CaptureCheck(canCapture: true, num, loadout.Charge, string.Empty);
		}
	}

	public readonly struct CaptureCheck
	{
		public bool CanCapture { get; }

		public float Requirement { get; }

		public float Charge { get; }

		public string BlockedReason { get; }

		public CaptureCheck(bool canCapture, float requirement, float charge, string blockedReason)
		{
			CanCapture = canCapture;
			Requirement = requirement;
			Charge = charge;
			BlockedReason = blockedReason ?? string.Empty;
		}
	}

	public readonly struct CaptureLoadout
	{
		public bool HasDevice { get; }

		public bool HasBattery { get; }

		public bool ChargeIsKnown { get; }

		public float Charge { get; }

		public static CaptureLoadout Empty => new CaptureLoadout(hasDevice: false, hasBattery: false, chargeIsKnown: false, 0f);

		public CaptureLoadout(bool hasDevice, bool hasBattery, bool chargeIsKnown, float charge)
		{
			HasDevice = hasDevice;
			HasBattery = hasBattery;
			ChargeIsKnown = chargeIsKnown;
			Charge = Mathf.Max(0f, charge);
		}
	}
}
