using System;
using UnityEngine;

namespace Eidren.AI
{
	public sealed class EnemyStateMachine
	{
		public EnemyState CurrentState { get; private set; }

		public bool IsDead => CurrentState == EnemyState.Dead;

		public event Action<EnemyState, EnemyState> StateChanged;

		public EnemyStateMachine(EnemyState initialState = EnemyState.Idle)
		{
			CurrentState = initialState;
		}

		public bool TrySetState(EnemyState next)
		{
			if (IsDead || next == CurrentState)
			{
				return false;
			}
			EnemyState currentState = CurrentState;
			CurrentState = next;
			this.StateChanged?.Invoke(currentState, next);
			return true;
		}
	}

	public static class EnemyLeashRules
	{
		public static bool ShouldReturn(Vector3 home, Vector3 enemy, Vector3 target, float maximumChaseDistance, float targetFollowDistance)
		{
			if (maximumChaseDistance <= 0f)
			{
				return false;
			}
			float num = maximumChaseDistance * maximumChaseDistance;
			float num2 = maximumChaseDistance + Mathf.Max(0f, targetFollowDistance);
			return FlatDistanceSquared(home, enemy) > num || FlatDistanceSquared(home, target) > num2 * num2;
		}

		public static bool HasReached(Vector3 current, Vector3 destination, float tolerance)
		{
			float num = Mathf.Max(0.01f, tolerance);
			return FlatDistanceSquared(current, destination) <= num * num;
		}

		private static float FlatDistanceSquared(Vector3 first, Vector3 second)
		{
			float num = first.x - second.x;
			float num2 = first.z - second.z;
			return num * num + num2 * num2;
		}
	}

	public static class EnemyThreat
	{
		public static bool IsEngaged(EnemyState state)
		{
			if (1 == 0)
			{
			}
			bool result = state switch
			{
				EnemyState.Alert => true, 
				EnemyState.Chase => true, 
				EnemyState.ChooseAttack => true, 
				EnemyState.Telegraph => true, 
				EnemyState.ExecuteAttack => true, 
				EnemyState.Recover => true, 
				EnemyState.Staggered => true, 
				_ => false, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}
}
