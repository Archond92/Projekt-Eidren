using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Bosses/Core Guardian")]
	public sealed class CoreGuardianData : ScriptableObject
	{
		[SerializeField]
		private string id = "enemy.core_guardian";

		[SerializeField]
		private float maximumHealth = 3000f;

		[SerializeField]
		private float maximumStagger = 400f;

		[SerializeField]
		private float closedProtection = 0.35f;

		[SerializeField]
		private float overheatSeconds = 1.2f;

		[SerializeField]
		private float normalOpenSeconds = 8f;

		[SerializeField]
		private float staggerOpenSeconds = 6f;

		[SerializeField]
		private float overheatHazardDamage = 25f;

		[SerializeField]
		private float overheatHazardRadius = 2.6f;

		[SerializeField]
		private EnemyNavigationData navigation;

		[SerializeField]
		private CoreGuardianAttackData[] attacks = Array.Empty<CoreGuardianAttackData>();

		public string Id => id ?? string.Empty;

		public float MaximumHealth => Mathf.Max(1f, maximumHealth);

		public float MaximumStagger => Mathf.Max(1f, maximumStagger);

		public float ClosedProtection => Mathf.Clamp01(closedProtection);

		public float OverheatSeconds => Mathf.Max(0.01f, overheatSeconds);

		public float NormalOpenSeconds => Mathf.Max(0.01f, normalOpenSeconds);

		public float StaggerOpenSeconds => Mathf.Max(0.01f, staggerOpenSeconds);

		public float OverheatHazardDamage => Mathf.Max(0f, overheatHazardDamage);

		public float OverheatHazardRadius => Mathf.Max(0.1f, overheatHazardRadius);

		public EnemyNavigationData Navigation => navigation;

		public IReadOnlyList<CoreGuardianAttackData> Attacks => attacks ?? Array.Empty<CoreGuardianAttackData>();

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (!string.Equals(Id, "enemy.core_guardian", StringComparison.Ordinal))
			{
				list.Add("Core Guardian requires its canonical stable ID.");
			}
			if (attacks == null || attacks.Length != 3)
			{
				list.Add("Core Guardian requires exactly three attacks.");
			}
			if (navigation.AttackRange <= 0f || navigation.DetectionRange <= navigation.AttackRange)
			{
				list.Add("Core Guardian navigation ranges are invalid.");
			}
			return list.ToArray();
		}
	}

	[Serializable]
	public struct CoreGuardianAttackData
	{
		[SerializeField]
		private CoreGuardianAttackType type;

		[SerializeField]
		[Min(0f)]
		private float damage;

		[SerializeField]
		[Min(0.01f)]
		private float telegraphSeconds;

		[SerializeField]
		[Min(0.1f)]
		private float radius;

		public CoreGuardianAttackType Type => type;

		public float Damage => Mathf.Max(0f, damage);

		public float TelegraphSeconds => Mathf.Max(0.01f, telegraphSeconds);

		public float Radius => Mathf.Max(0.1f, radius);
	}
}
