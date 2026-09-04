using UnityEngine;

namespace Eidren.Interaction
{
	public sealed class InteractionSession
	{
		private bool _completionIssued;

		public IInteractable Target { get; private set; }

		public InteractionState State { get; private set; }

		public float Progress { get; private set; }

		public InteractionCancelReason LastCancelReason { get; private set; }

		public bool IsActive => State == InteractionState.Starting || State == InteractionState.Holding || State == InteractionState.Completing;

		public bool Begin(IInteractable target, in InteractionContext context)
		{
			if (IsActive || !InteractionUtility.IsActive(target) || !target.CanInteract(in context, out var _))
			{
				return false;
			}
			Target = target;
			Progress = 0f;
			LastCancelReason = InteractionCancelReason.None;
			_completionIssued = false;
			State = InteractionState.Starting;
			target.BeginInteraction(in context);
			if (target.Mode == InteractionMode.Instant)
			{
				Complete(in context);
				return true;
			}
			State = InteractionState.Holding;
			target.UpdateInteraction(in context, 0f);
			return true;
		}

		public void Tick(float deltaTime, bool inputHeld, in InteractionContext context)
		{
			if (!IsActive || Target == null)
			{
				return;
			}
			if (!InteractionUtility.IsActive(Target))
			{
				Cancel(in context, InteractionCancelReason.TargetUnavailable);
				return;
			}
			if (Target.Mode == InteractionMode.Hold && !inputHeld)
			{
				Cancel(in context, InteractionCancelReason.InputReleased);
				return;
			}
			if (InteractionUtility.FlatDistanceToBody(context.ActorTransform.position, Target) > Mathf.Max(0f, Target.InteractionRange))
			{
				Cancel(in context, InteractionCancelReason.OutOfRange);
				return;
			}
			if (!Target.CanInteract(in context, out var _))
			{
				Cancel(in context, InteractionCancelReason.TargetUnavailable);
				return;
			}
			float num = Mathf.Max(0.01f, Target.HoldDuration);
			Progress = Mathf.Clamp01(Progress + Mathf.Max(0f, deltaTime) / num);
			Target.UpdateInteraction(in context, Progress);
			if (Progress >= 1f)
			{
				Complete(in context);
			}
		}

		public void Cancel(in InteractionContext context, InteractionCancelReason reason)
		{
			if (IsActive)
			{
				IInteractable target = Target;
				Target = null;
				Progress = 0f;
				LastCancelReason = reason;
				State = InteractionState.Cancelled;
				if (InteractionUtility.IsActive(target))
				{
					target.CancelInteraction(in context, reason);
				}
			}
		}

		public void ResetToIdle()
		{
			if (!IsActive)
			{
				Target = null;
				Progress = 0f;
				State = InteractionState.None;
			}
		}

		public void UpdateCancelledReason(InteractionCancelReason reason)
		{
			if (State == InteractionState.Cancelled)
			{
				LastCancelReason = reason;
			}
		}

		private void Complete(in InteractionContext context)
		{
			if (!_completionIssued && Target != null)
			{
				_completionIssued = true;
				IInteractable target = Target;
				State = InteractionState.Completing;
				target.CompleteInteraction(in context);
				Target = null;
				Progress = 1f;
				State = InteractionState.Completed;
			}
		}
	}
}
