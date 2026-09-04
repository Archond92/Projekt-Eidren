using UnityEngine;

namespace Eidren.Combat
{
	public interface IDamageable
	{
		bool IsAlive { get; }

		Transform TargetTransform { get; }

		void ApplyDamage(DamageInfo damage);
	}
}
