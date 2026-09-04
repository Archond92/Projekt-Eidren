using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Combat/Boss")]
	public sealed class BossData : ScriptableObject
	{
		[SerializeField]
		private string id;

		public string DisplayName = "Garon";

		public float MaxHealth = 2100f;

		public float MaxStagger = 340f;

		public float StaggerDuration = 6f;

		public float PostStaggerResistanceDuration = 4f;

		public float PostStaggerResistanceMultiplier = 0.38f;

		[Min(0f)]
		public int ExperienceReward = 350;

		public EnemyNavigationData Navigation;

		public GaronAttackSetData Attacks = new GaronAttackSetData();

		public string Id => id;
	}

	[Serializable]
	public sealed class GaronAttackData
	{
		public float MinimumDistance;

		public float MaximumDistance = 4f;

		public float TelegraphDuration = 0.9f;

		public float ExecuteDuration = 0.25f;

		public float RecoveryDuration = 0.75f;

		public float Damage = 34f;

		public float HitDistance = 4f;

		public float MoveSpeed;

		public float PathRadius = 0.8f;

		public float TelegraphForwardOffset = 2.5f;

		public Vector3 TelegraphScale = new Vector3(5.5f, 0.05f, 4f);

		public bool IsSuitable(float distance)
		{
			return distance >= Mathf.Max(0f, MinimumDistance) && distance <= Mathf.Max(MinimumDistance, MaximumDistance);
		}
	}

	[Serializable]
	public sealed class GaronAttackSetData
	{
		public float ChoiceDuration = 0.25f;

		public float FailedChoiceDelay = 0.2f;

		public float SpinPreferredDistance = 2.4f;

		public int MaxConsecutiveSameAttack = 2;

		public GaronAttackData Front = new GaronAttackData
		{
			MinimumDistance = 2.2f,
			MaximumDistance = 4f,
			TelegraphDuration = 0.9f,
			ExecuteDuration = 0.25f,
			RecoveryDuration = 0.75f,
			Damage = 34f,
			HitDistance = 4f,
			TelegraphForwardOffset = 2.5f,
			TelegraphScale = new Vector3(5.5f, 0.05f, 4f)
		};

		public GaronAttackData Charge = new GaronAttackData
		{
			MinimumDistance = 5f,
			MaximumDistance = 9f,
			TelegraphDuration = 1.15f,
			ExecuteDuration = 0.68f,
			RecoveryDuration = 1.05f,
			Damage = 31f,
			HitDistance = 2.5f,
			MoveSpeed = 9f,
			PathRadius = 0.8f,
			TelegraphForwardOffset = 5f,
			TelegraphScale = new Vector3(3.2f, 0.05f, 11f)
		};

		public GaronAttackData Spin = new GaronAttackData
		{
			MinimumDistance = 0f,
			MaximumDistance = 4.7f,
			TelegraphDuration = 0.9f,
			ExecuteDuration = 0.25f,
			RecoveryDuration = 0.75f,
			Damage = 25f,
			HitDistance = 4.7f,
			TelegraphForwardOffset = 0f,
			TelegraphScale = new Vector3(9.4f, 0.05f, 9.4f)
		};
	}
}
