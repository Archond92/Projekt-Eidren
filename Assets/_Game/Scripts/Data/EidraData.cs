using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Eidra")]
	public sealed class EidraData : ScriptableObject
	{
		[SerializeField]
		private string id;

		public string DisplayName;

		public Color UiAccent = Color.white;

		public EidraRole Role;

		public float PassiveStaggerMultiplier = 1f;

		public float PassiveBackDamageMultiplier = 1f;

		/// <summary>
		/// F32-005: Anteil des Trefferschadens, der am Ziel nachbrennt
		/// (0 = kein Brand). Ignivars Passiv — wirkt gegen JEDEN Gegner und
		/// haengt an keiner Eigenschaft des Ziels.
		/// </summary>
		public float PassiveBurnFraction;

		/// <summary>Dauer des Nachbrands in Sekundentakten.</summary>
		public int PassiveBurnSeconds;

		public AbilityData Skill1;

		public AbilityData Skill2;

		[SerializeField]
		private EidraProductionRole productionRole = EidraProductionRole.Unassigned;

		[SerializeField]
		private EidraCaptureProfile capture;

		public string Id => id;

		public EidraProductionRole ProductionRole => productionRole;

		public EidraCaptureProfile Capture => capture;

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (string.IsNullOrWhiteSpace(Id))
			{
				list.Add("Stable eidra ID is empty.");
			}
			if (string.IsNullOrWhiteSpace(DisplayName))
			{
				list.Add("Eidra '" + base.name + "' has no display name.");
			}
			if (Skill1 == null || Skill2 == null)
			{
				list.Add("Eidra '" + base.name + "' requires exactly two abilities.");
			}
			if (capture.RequirementAtFullHealth <= 0f)
			{
				list.Add("Eidra '" + base.name + "' requires a positive capture requirement at full health.");
			}
			if (capture.MinimumRequirement <= 0f)
			{
				list.Add("Eidra '" + base.name + "' requires a positive minimum capture requirement — otherwise the battery tier stops mattering once the target is worn down.");
			}
			if (capture.MinimumRequirement > capture.RequirementAtFullHealth)
			{
				list.Add("Eidra '" + base.name + "' has a minimum capture requirement above its requirement at full health; the demand must fall with health, not rise.");
			}
			float fleeHealthFraction = capture.FleeHealthFraction;
			if (fleeHealthFraction <= 0f || fleeHealthFraction >= 1f)
			{
				list.Add("Eidra '" + base.name + "' requires a flee threshold strictly between 0 and 1.");
			}
			return list.ToArray();
		}
	}

	[Serializable]
	public struct EidraCaptureProfile
	{
		[SerializeField]
		[Min(0f)]
		private float requirementAtFullHealth;

		[SerializeField]
		[Min(0f)]
		private float minimumRequirement;

		[SerializeField]
		[Range(0f, 1f)]
		private float fleeHealthFraction;

		public float RequirementAtFullHealth => Mathf.Max(0f, requirementAtFullHealth);

		public float MinimumRequirement => Mathf.Clamp(minimumRequirement, 0f, RequirementAtFullHealth);

		public float FleeHealthFraction => Mathf.Clamp01(fleeHealthFraction);

		public EidraCaptureProfile(float requirementAtFullHealth, float minimumRequirement, float fleeHealthFraction)
		{
			this.requirementAtFullHealth = requirementAtFullHealth;
			this.minimumRequirement = minimumRequirement;
			this.fleeHealthFraction = fleeHealthFraction;
		}
	}
}
