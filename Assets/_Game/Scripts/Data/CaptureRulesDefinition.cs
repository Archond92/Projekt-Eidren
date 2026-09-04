using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(fileName = "CaptureRules_V01", menuName = "Eidren/Eidra/Capture Rules")]
	public sealed class CaptureRulesDefinition : ScriptableObject
	{
		[SerializeField]
		private BatteryChargeEntry[] batteryCharges = Array.Empty<BatteryChargeEntry>();

		public IReadOnlyList<BatteryChargeEntry> BatteryCharges => batteryCharges ?? Array.Empty<BatteryChargeEntry>();

		public bool TryGetCharge(int tier, out float charge)
		{
			foreach (BatteryChargeEntry batteryCharge in BatteryCharges)
			{
				if (batteryCharge.Tier != tier)
				{
					continue;
				}
				charge = batteryCharge.Charge;
				return true;
			}
			charge = 0f;
			return false;
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (BatteryCharges.Count == 0)
			{
				list.Add("Capture rules '" + base.name + "' carry no battery charge at all; no capture could ever succeed.");
				return list.ToArray();
			}
			HashSet<int> hashSet = new HashSet<int>();
			float num = -1f / 0f;
			int num2 = -2147483648;
			foreach (BatteryChargeEntry batteryCharge in BatteryCharges)
			{
				if (!hashSet.Add(batteryCharge.Tier))
				{
					list.Add("Capture rules '" + base.name + "' define tier " + $"{batteryCharge.Tier} twice.");
				}
				if (batteryCharge.Charge <= 0f)
				{
					list.Add($"Capture rules '{base.name}' give tier {batteryCharge.Tier} " + "no charge.");
				}
				if (batteryCharge.Tier > num2 && batteryCharge.Charge <= num)
				{
					list.Add($"Capture rules '{base.name}': tier {batteryCharge.Tier} does " + "not charge more than the tier below it.");
				}
				num2 = batteryCharge.Tier;
				num = batteryCharge.Charge;
			}
			return list.ToArray();
		}
	}

	[Serializable]
	public struct BatteryChargeEntry
	{
		[SerializeField]
		[Min(0f)]
		private int tier;

		[SerializeField]
		[Min(0f)]
		private float charge;

		public int Tier => Mathf.Max(0, tier);

		public float Charge => Mathf.Max(0f, charge);

		public BatteryChargeEntry(int tier, float charge)
		{
			this.tier = tier;
			this.charge = charge;
		}
	}
}
