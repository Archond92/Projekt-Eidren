using UnityEngine;

namespace Eidren.Combat
{
	public interface IBackAttackTarget
	{
		bool IsBackAttack(Transform attacker, float threshold = -0.35f);
	}
}
