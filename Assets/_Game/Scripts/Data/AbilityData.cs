using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Combat/Ability")]
	public sealed class AbilityData : ScriptableObject
	{
		[SerializeField]
		private string id;

		public string DisplayName;

		public Sprite Icon;

		public float Cooldown = 6f;

		public float Range = 8f;

		public AbilityExecutionType ExecutionType;

		public float CastDuration;

		public float EffectDuration;

		public float StaggerAmount;

		public float BackDamageMultiplier = 1f;

		public float TeleportBehindDistance;

		public float Radius;

		public float HealthDamagePerSecond;

		public float ProtectionReduction;

		public string Id => id;
	}
}
