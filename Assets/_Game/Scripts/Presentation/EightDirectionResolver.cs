using UnityEngine;

namespace Eidren.Presentation
{
	public sealed class EightDirectionResolver
	{
		private const float SectorDegrees = 45f;

		private const float HalfSectorDegrees = 22.5f;

		private readonly float _hysteresisDegrees;

		private bool _hasFacing;

		public ActorFacing8 Facing { get; private set; } = ActorFacing8.S;

		public EightDirectionResolver(float hysteresisDegrees = 6f)
		{
			_hysteresisDegrees = Mathf.Clamp(hysteresisDegrees, 0f, 22.4f);
		}

		public ActorFacing8 Resolve(Vector3 worldDirection, ActorFacing8 fallback = ActorFacing8.S)
		{
			worldDirection.y = 0f;
			if (worldDirection.sqrMagnitude < 0.0001f)
			{
				if (!_hasFacing)
				{
					Facing = fallback;
				}
				return Facing;
			}
			float num = Mathf.Atan2(worldDirection.x, worldDirection.z) * 57.29578f;
			if (num < 0f)
			{
				num += 360f;
			}
			ActorFacing8 facing = Quantize(num);
			if (!_hasFacing)
			{
				Facing = facing;
				_hasFacing = true;
				return Facing;
			}
			if (Mathf.Abs(Mathf.DeltaAngle((float)Facing * 45f, num)) > 22.5f + _hysteresisDegrees)
			{
				Facing = facing;
			}
			return Facing;
		}

		public void Reset(ActorFacing8 facing = ActorFacing8.S)
		{
			Facing = facing;
			_hasFacing = false;
		}

		public static ActorFacing8 Quantize(Vector3 worldDirection)
		{
			worldDirection.y = 0f;
			if (worldDirection.sqrMagnitude < 0.0001f)
			{
				return ActorFacing8.S;
			}
			float num = Mathf.Atan2(worldDirection.x, worldDirection.z) * 57.29578f;
			if (num < 0f)
			{
				num += 360f;
			}
			return Quantize(num);
		}

		private static ActorFacing8 Quantize(float angle)
		{
			return (ActorFacing8)(Mathf.RoundToInt(angle / 45f) % 8);
		}
	}
}
