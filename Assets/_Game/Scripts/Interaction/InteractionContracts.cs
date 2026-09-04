using Eidren.Core.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Interaction
{
	public interface IInteractable
	{
		string InteractionId { get; }

		InteractionType Type { get; }

		string DisplayText { get; }

		Sprite Icon { get; }

		float InteractionRange { get; }

		InteractionMode Mode { get; }

		float HoldDuration { get; }

		int Priority { get; }

		Vector3 InteractionPosition { get; }

		GameObject InteractionObject { get; }

		bool CanInteract(in InteractionContext context, out string blockedReason);

		void BeginInteraction(in InteractionContext context);

		void UpdateInteraction(in InteractionContext context, float normalizedProgress);

		void CancelInteraction(in InteractionContext context, InteractionCancelReason reason);

		void CompleteInteraction(in InteractionContext context);
	}

	public readonly struct InteractionContext
	{
		public readonly GameObject Actor;

		public readonly Transform ActorTransform;

		public readonly Vector3 FacingDirection;

		public readonly PlayerInventory Inventory;

		public InteractionContext(GameObject actor, Transform actorTransform, Vector3 facingDirection, PlayerInventory inventory = null)
		{
			Actor = actor;
			ActorTransform = actorTransform;
			FacingDirection = ((facingDirection.sqrMagnitude > 0.001f) ? facingDirection.normalized : ((actorTransform != null) ? actorTransform.forward : Vector3.forward));
			Inventory = inventory;
		}
	}

	public readonly struct InteractionSnapshot
	{
		public readonly InteractionState State;

		public readonly string InteractionId;

		public readonly InteractionType Type;

		public readonly string DisplayText;

		public readonly Sprite Icon;

		public readonly bool HasTarget;

		public readonly bool CanInteract;

		public readonly InteractionMode Mode;

		public readonly float Progress;

		public readonly string BlockedReason;

		public static InteractionSnapshot Empty => new InteractionSnapshot(InteractionState.None, null, canInteract: false, 0f, string.Empty);

		public InteractionSnapshot(InteractionState state, IInteractable target, bool canInteract, float progress, string blockedReason)
		{
			if (!InteractionUtility.IsActive(target))
			{
				target = null;
			}
			State = state;
			InteractionId = target?.InteractionId ?? string.Empty;
			Type = target?.Type ?? InteractionType.WorldObject;
			DisplayText = target?.DisplayText ?? string.Empty;
			Icon = target?.Icon;
			HasTarget = target != null;
			CanInteract = target != null && canInteract;
			Mode = target?.Mode ?? InteractionMode.Instant;
			Progress = Mathf.Clamp01(progress);
			BlockedReason = blockedReason ?? string.Empty;
		}
	}

	public static class InteractionUtility
	{
		private static readonly List<Collider> ColliderBuffer = new List<Collider>(16);

		private static readonly List<Renderer> RendererBuffer = new List<Renderer>(16);

		/// <summary>
		/// Einheitlicher Abstand vom Mittelpunkt der Figur bis zur sichtbaren
		/// Koerperoberflaeche eines Interaktionsziels. 1,5 m entspricht bei
		/// einem 1x1-Gebaeude den bestaetigten 2,0 m ab Zellmitte und beim
		/// 2x2-Acker den bestaetigten 2,5 m ab Zellmitte.
		/// </summary>
		public const float StandardSurfaceRange = 1.5f;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetBuffers()
		{
			ColliderBuffer.Clear();
			RendererBuffer.Clear();
		}

		public static bool IsActive(IInteractable target)
		{
			if (target == null)
			{
				return false;
			}
			if (target is Object obj && obj == null)
			{
				return false;
			}
			GameObject interactionObject = target.InteractionObject;
			return interactionObject != null && interactionObject.activeInHierarchy;
		}

		public static float FlatDistance(Vector3 first, Vector3 second)
		{
			first.y = 0f;
			second.y = 0f;
			return Vector3.Distance(first, second);
		}

		/// <summary>
		/// Misst flach bis zum naechsten Punkt des physischen Zielkoerpers.
		/// Nicht-ausloesende Collider sind verbindlich; Renderer und danach
		/// Trigger dienen nur als Rueckfall fuer rein visuelle bzw. Portalziele.
		/// Ziele ohne Koerper behalten die bisherige Punktsemantik.
		/// </summary>
		public static float FlatDistanceToBody(Vector3 origin, IInteractable target)
		{
			if (target == null)
			{
				return float.PositiveInfinity;
			}
			return FlatDistance(origin, ClosestBodyPoint(origin, target));
		}

		public static Vector3 ClosestBodyPoint(Vector3 origin, IInteractable target)
		{
			if (target == null || target.InteractionObject == null)
			{
				return target?.InteractionPosition ?? origin;
			}
			GameObject root = target.InteractionObject;
			if (TryClosestColliderPoint(root, origin, triggers: false, out Vector3 point)
				|| TryClosestRendererPoint(root, origin, out point)
				|| TryClosestColliderPoint(root, origin, triggers: true, out point))
			{
				return point;
			}
			return target.InteractionPosition;
		}

		private static bool TryClosestColliderPoint(GameObject root, Vector3 origin, bool triggers, out Vector3 point)
		{
			point = origin;
			float best = float.PositiveInfinity;
			bool found = false;
			ColliderBuffer.Clear();
			root.GetComponentsInChildren(false, ColliderBuffer);
			foreach (Collider collider in ColliderBuffer)
			{
				if (collider == null || !collider.enabled || collider.isTrigger != triggers)
				{
					continue;
				}
				Vector3 candidate = collider.bounds.ClosestPoint(origin);
				float distance = FlatSqrDistance(origin, candidate);
				if (distance < best)
				{
					best = distance;
					point = candidate;
					found = true;
				}
			}
			return found;
		}

		private static bool TryClosestRendererPoint(GameObject root, Vector3 origin, out Vector3 point)
		{
			point = origin;
			float best = float.PositiveInfinity;
			bool found = false;
			RendererBuffer.Clear();
			root.GetComponentsInChildren(false, RendererBuffer);
			foreach (Renderer renderer in RendererBuffer)
			{
				if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
				{
					continue;
				}
				Vector3 candidate = renderer.bounds.ClosestPoint(origin);
				float distance = FlatSqrDistance(origin, candidate);
				if (distance < best)
				{
					best = distance;
					point = candidate;
					found = true;
				}
			}
			return found;
		}

		private static float FlatSqrDistance(Vector3 first, Vector3 second)
		{
			float x = first.x - second.x;
			float z = first.z - second.z;
			return x * x + z * z;
		}
	}
}
