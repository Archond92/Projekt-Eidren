using Eidren.AI;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Presentation;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Eidren.Eidra
{
	public sealed class EmberCircleField : MonoBehaviour
	{
		private const string AttackId = "eidra.ignivar.ember_circle";

		public static EmberCircleField Spawn(Vector3 position, float radius, float duration, float damagePerSecond, GameObject source)
		{
			GameObject gameObject = new GameObject("Ignivar_EmberCircle");
			gameObject.transform.position = position;
			EmberCircleField emberCircleField = gameObject.AddComponent<EmberCircleField>();
			emberCircleField.StartCoroutine(emberCircleField.Run(Mathf.Max(0.1f, radius), Mathf.Max(0.1f, duration), Mathf.Max(0f, damagePerSecond), source));
			CombatFeedback.SpawnAura(gameObject.transform, new Color(1f, 0.24f, 0.04f, 0.9f), duration, "GLUTKREIS", radius, 1.4f);
			return emberCircleField;
		}

		private IEnumerator Run(float radius, float duration, float damagePerSecond, GameObject source)
		{
			int maximumTicks = Mathf.Max(1, Mathf.RoundToInt(duration));
			EmberCircleDamageBudget budget = new EmberCircleDamageBudget(damagePerSecond, maximumTicks);
			for (float elapsed = 0f; elapsed < duration; elapsed += 1f)
			{
				ApplyTick(radius, budget, source);
				yield return new WaitForSeconds(1f);
			}
			Object.Destroy(base.gameObject);
		}

		private void ApplyTick(float radius, EmberCircleDamageBudget budget, GameObject source)
		{
			List<IDamageable> list = new List<IDamageable>(CombatTargetRegistry.Targets);
			float num = radius * radius;
			foreach (IDamageable item in list)
			{
				if (item is EnemyControllerBase && item.IsAlive && !((item.TargetTransform.position - base.transform.position).sqrMagnitude > num))
				{
					string targetId = item.TargetTransform.GetInstanceID().ToString();
					float num2 = budget.TryApplyTick(targetId);
					if (!(num2 <= 0f))
					{
						item.ApplyDamage(new DamageInfo(num2, 0f, item.TargetTransform.position, (source != null) ? source : base.gameObject, isBackAttack: false, "eidra.ignivar.ember_circle", "ignivar"));
					}
				}
			}
		}
	}
}
