using UnityEngine;

namespace Eidren.Interaction
{
	public static class InteractionTargetSelector
	{
		// Rang und Blickrichtung zaehlen als Entfernungsbonus, nicht als Vorrang:
		// Ein Rangpunkt ist PriorityReach Meter wert, genaues Anvisieren bis zu
		// FacingReach Meter. Vorher entschied der Rang unbedingt vor der
		// Entfernung — eine Kiste (Rang 88) in Reichweite verdeckte damit das
		// Erz (Rang 10), vor dem der Spieler stand, und war aus dieser Richtung
		// gar nicht mehr abbaubar (Glutruinen, 15.08.2026).
		// F32-001: Schuld am gemeldeten Nachbarfall war allein die
		// BLICKRICHTUNG. Sie war 0,5 wert, also 1,0 m Spannweite zwischen
		// „genau angesehen" und „weggedreht" — auf einem Raster mit 1 m
		// Zellabstand konnte sie das Gebaeude schlagen, an dem die Figur
		// klebte. Jetzt 0,15 (0,3 m Spannweite).
		//
		// Der RANG bleibt bei 0,01. Ein erster Anlauf hatte ihn auf 0,002
		// mitgestutzt — das war eine Ueberkorrektur: Zwischen Werkbank (80)
		// und Lagerkiste (85) liegen ohnehin nur 0,05 m, der Rang war am
		// gemeldeten Fall unbeteiligt. Gebraucht wird er dort, wo die Raenge
		// WEIT auseinanderliegen: Eine frische Leiche (90) muss den
		// Beerenstrauch (10) daneben schlagen, sonst pflueckt man statt zu
		// pluendern (nachgewiesen von LootIntegrationTests).
		private const float PriorityReach = 0.01f;

		private const float FacingReach = 0.15f;

		// Mittelpunkt der Figur bis zur Wand bei einem CharacterController mit
		// Radius 0,48: Wer im 0,55-m-Band steht, beruehrt den Zielkoerper. In
		// diesem Band darf die Hysterese keinen weiter entfernten Nachbarn
		// festhalten (OP-02/F32-001).
		private const float BodyContactReach = 0.55f;

		public static IInteractable SelectBest(IInteractable[] candidates, int count, in InteractionContext context, IInteractable current, float directionHysteresis, float distanceHysteresis, out bool canInteract, out string blockedReason)
		{
			IInteractable interactable = null;
			IInteractable interactable2 = null;
			string text = string.Empty;
			int num = Mathf.Min(count, (candidates != null) ? candidates.Length : 0);
			for (int i = 0; i < num; i++)
			{
				IInteractable interactable3 = candidates[i];
				if (!IsInRange(interactable3, in context))
				{
					continue;
				}
				if (interactable3.CanInteract(in context, out var blockedReason2))
				{
					if (IsBetter(interactable3, interactable, in context))
					{
						interactable = interactable3;
					}
				}
				else if (IsBetter(interactable3, interactable2, in context))
				{
					interactable2 = interactable3;
					text = blockedReason2;
				}
			}
			IInteractable interactable4 = interactable ?? interactable2;
			canInteract = interactable != null;
			blockedReason = (canInteract ? string.Empty : text);
			if (interactable4 == null || current == null || interactable4 == current || !IsInRange(current, in context))
			{
				return interactable4;
			}
			string blockedReason3;
			bool flag = current.CanInteract(in context, out blockedReason3);
			if (flag != canInteract)
			{
				return interactable4;
			}
			// Der Rang steckt seit dem Glutruinen-Fix in der wirksamen Entfernung und
			// braucht keinen eigenen Sonderweg mehr. Frueher sprang das Ziel bei
			// abweichendem Rang ohne jede Daempfung um.
			float num2 = Alignment(interactable4, in context);
			float num3 = Alignment(current, in context);
			float num4 = EffectiveDistance(interactable4, in context);
			float num5 = EffectiveDistance(current, in context);
			float physicalDistance = Distance(interactable4, in context);
			float currentPhysicalDistance = Distance(current, in context);
			if (physicalDistance <= BodyContactReach && physicalDistance < currentPhysicalDistance)
			{
				return interactable4;
			}
			bool flag2 = num2 - num3 > Mathf.Max(0f, directionHysteresis);
			bool flag3 = num5 - num4 > Mathf.Max(0f, distanceHysteresis);
			if (flag2 || flag3)
			{
				return interactable4;
			}
			canInteract = flag;
			blockedReason = (flag ? string.Empty : blockedReason3);
			return current;
		}

		// Strenge Totalordnung: Jeder Vergleich entscheidet sich an einem Kriterium
		// mit exakter Ordnung, und die Instanz-Kennung schliesst die Kette ab. Nur
		// so liefert die Reihenfolge der Kandidaten — die Physics.OverlapSphere
		// nicht garantiert — immer dasselbe Ziel. Ein Toleranzvergleich wie
		// Mathf.Approximately taugt hier nicht: er ist nicht transitiv und erzeugt
		// Vergleichszyklen, bei denen das Ergebnis wieder an der Abfragereihenfolge
		// haengt.
		private static bool IsBetter(IInteractable candidate, IInteractable incumbent, in InteractionContext context)
		{
			if (incumbent == null)
			{
				return true;
			}
			float num = EffectiveDistance(candidate, in context);
			float num2 = EffectiveDistance(incumbent, in context);
			if (num != num2)
			{
				return num < num2;
			}
			if (candidate.Priority != incumbent.Priority)
			{
				return candidate.Priority > incumbent.Priority;
			}
			float num3 = Alignment(candidate, in context);
			float num4 = Alignment(incumbent, in context);
			if (num3 != num4)
			{
				return num3 > num4;
			}
			float num5 = Distance(candidate, in context);
			float num6 = Distance(incumbent, in context);
			if (num5 != num6)
			{
				return num5 < num6;
			}
			return string.CompareOrdinal(candidate.InteractionId, incumbent.InteractionId) < 0;
		}

		// Entfernung, verrechnet mit Rang und Blickrichtung. Kleiner ist besser.
		private static float EffectiveDistance(IInteractable target, in InteractionContext context)
		{
			return Distance(target, in context) - (float)target.Priority * PriorityReach - Alignment(target, in context) * FacingReach;
		}

		private static bool IsInRange(IInteractable target, in InteractionContext context)
		{
			return InteractionUtility.IsActive(target) && context.ActorTransform != null && Distance(target, in context) <= Mathf.Max(0f, target.InteractionRange);
		}

		private static float Distance(IInteractable target, in InteractionContext context)
		{
			return InteractionUtility.FlatDistanceToBody(context.ActorTransform.position, target);
		}

		private static float Alignment(IInteractable target, in InteractionContext context)
		{
			Vector3 vector = target.InteractionPosition - context.ActorTransform.position;
			vector.y = 0f;
			if (vector.sqrMagnitude < 0.0001f)
			{
				return 1f;
			}
			Vector3 vector2 = context.FacingDirection;
			vector2.y = 0f;
			if (vector2.sqrMagnitude < 0.0001f)
			{
				vector2 = Vector3.forward;
			}
			return Vector3.Dot(vector2.normalized, vector.normalized);
		}
	}
}
