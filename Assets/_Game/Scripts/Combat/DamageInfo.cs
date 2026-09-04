using System;
using UnityEngine;

namespace Eidren.Combat
{
	public readonly struct DamageInfo
	{
		public readonly float HealthDamage;

		public readonly float StaggerDamage;

		public readonly Vector3 HitPoint;

		public readonly GameObject Source;

		public readonly bool IsBackAttack;

		public readonly string AttackId;

		public readonly string SourceId;

		public DamageInfo(float healthDamage, float staggerDamage, Vector3 hitPoint, GameObject source, bool isBackAttack, string attackId, string sourceId)
		{
			if (string.IsNullOrWhiteSpace(attackId))
			{
				throw new ArgumentException("DamageInfo requires a stable attack ID.", "attackId");
			}
			if (string.IsNullOrWhiteSpace(sourceId))
			{
				throw new ArgumentException("DamageInfo requires a stable source ID.", "sourceId");
			}
			HealthDamage = healthDamage;
			StaggerDamage = staggerDamage;
			HitPoint = hitPoint;
			Source = source;
			IsBackAttack = isBackAttack;
			AttackId = attackId;
			SourceId = sourceId;
		}
	}
}
