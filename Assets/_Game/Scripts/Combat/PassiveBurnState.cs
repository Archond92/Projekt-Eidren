using UnityEngine;

namespace Eidren.Combat
{
	/// <summary>
	/// F32-005: Nachbrennen aus Ignivars Passiv. Reine Logik ohne Unity —
	/// der Traeger (EnemyControllerBase) taktet sie nur. Der Brand STAPELT
	/// NICHT: Ein neuer Treffer ersetzt den laufenden, sonst summierten sich
	/// schnelle Waffen zu einer zweiten Schadensquelle.
	///
	/// Getaktet wird im Sekundenabstand wie beim Glutkreis — pro Sekunde
	/// eine lesbare Schadenszahl statt eines Zahlenregens pro Frame.
	/// </summary>
	public sealed class PassiveBurnState
	{
		private float _damagePerTick;

		private int _remainingTicks;

		private float _sinceLastTick;

		public bool IsBurning => _remainingTicks > 0;

		public void Refresh(float totalDamage, int ticks)
		{
			if (totalDamage <= 0f || ticks <= 0)
			{
				return;
			}
			_damagePerTick = totalDamage / ticks;
			_remainingTicks = ticks;
			_sinceLastTick = 0f;
		}

		public bool TryTick(float deltaTime, out float damage)
		{
			damage = 0f;
			if (_remainingTicks <= 0)
			{
				return false;
			}
			_sinceLastTick += Mathf.Max(0f, deltaTime);
			if (_sinceLastTick < 1f)
			{
				return false;
			}
			_sinceLastTick -= 1f;
			_remainingTicks--;
			damage = _damagePerTick;
			if (_remainingTicks <= 0)
			{
				_damagePerTick = 0f;
				_sinceLastTick = 0f;
			}
			return true;
		}

		public void Clear()
		{
			_damagePerTick = 0f;
			_remainingTicks = 0;
			_sinceLastTick = 0f;
		}
	}
}
