using Eidren.Data;
using System;

namespace Eidren.AI
{
	public static class GaronAttackSelection
	{
		public static bool TryChoose(GaronAttackSetData attacks, float distance, bool chargePathClear, GaronAttackType? previous, int consecutiveUses, int randomIndex, out GaronAttackType selected)
		{
			selected = GaronAttackType.Front;
			if (attacks == null)
			{
				return false;
			}
			bool flag = attacks.Front != null && attacks.Front.IsSuitable(distance);
			bool flag2 = attacks.Charge != null && attacks.Charge.IsSuitable(distance) && chargePathClear;
			bool flag3 = attacks.Spin != null && attacks.Spin.IsSuitable(distance);
			int num = Math.Max(1, attacks.MaxConsecutiveSameAttack);
			if (previous.HasValue && consecutiveUses >= num)
			{
				switch (previous.Value)
				{
				case GaronAttackType.Front:
					flag = false;
					break;
				case GaronAttackType.Charge:
					flag2 = false;
					break;
				case GaronAttackType.Spin:
					flag3 = false;
					break;
				}
			}
			if (distance <= attacks.SpinPreferredDistance && flag3)
			{
				selected = GaronAttackType.Spin;
				return true;
			}
			int num2 = (flag ? 1 : 0) + (flag2 ? 1 : 0) + (flag3 ? 1 : 0);
			if (num2 == 0)
			{
				return false;
			}
			int num3 = (randomIndex & 0x7FFFFFFF) % num2;
			if (flag && num3-- == 0)
			{
				selected = GaronAttackType.Front;
			}
			else if (flag2 && num3-- == 0)
			{
				selected = GaronAttackType.Charge;
			}
			else
			{
				selected = GaronAttackType.Spin;
			}
			return true;
		}
	}
}
