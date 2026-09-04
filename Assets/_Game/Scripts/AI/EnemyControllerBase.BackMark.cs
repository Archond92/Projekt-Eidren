using Eidren.Combat;
using Eidren.Presentation;
using UnityEngine;

namespace Eidren.AI
{
	/// <summary>
	/// F32-003/F32-004: Das Rueckenmal (Noctarions zweite Faehigkeit) lag am
	/// BossController und war damit auf die zwei Bosse beschraenkt — gegen
	/// jeden Feldgegner meldete die Faehigkeit „ZIEL KANN NICHT MARKIERT
	/// WERDEN". Es gehoert an den gemeinsamen Gegnertyp: Keine Faehigkeit
	/// ist an einen Gegnertyp gebunden (Grundsatz F32-004).
	/// </summary>
	public abstract partial class EnemyControllerBase
	{
		private float _backMarkUntil;

		private float _backMarkMultiplier = 1f;

		private GameObject _backMarkVisual;

		public float BackMarkRemaining => Mathf.Max(0f, _backMarkUntil - Time.time);

		/// <summary>
		/// Aura-Masse. Grosse Gegner ueberschreiben sie, damit der Ring nicht
		/// in der Figur liegt (Garon: 2,35 / 4,7).
		/// </summary>
		protected virtual float BackMarkAuraRadius => 1.35f;

		protected virtual float BackMarkLabelHeight => 3.25f;

		public void MarkBack(float duration, float multiplier)
		{
			_backMarkUntil = Time.time + duration;
			_backMarkMultiplier = Mathf.Max(1f, multiplier);
			DestroyBackMarkVisual();
			_backMarkVisual = CombatFeedback.SpawnAura(base.transform, new Color(0.72f, 0.32f, 1f, 0.95f), duration, $"RÜCKENMAL  ×{_backMarkMultiplier:0.##}", BackMarkAuraRadius, BackMarkLabelHeight);
			CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * BackMarkLabelHeight, "RÜCKENMAL!\nRÜCKENTREFFER VERSTÄRKT", new Color(0.82f, 0.42f, 1f), 1.5f);
		}

		public void ClearBackMark()
		{
			_backMarkUntil = 0f;
			_backMarkMultiplier = 1f;
			DestroyBackMarkVisual();
		}

		/// <summary>
		/// Verstaerkt Rueckentreffer, solange die Markierung laeuft. Wird aus
		/// <c>ModifyHealthDamage</c> heraus angewandt — auch von den
		/// Ableitungen, die den Schaden zusaetzlich veraendern.
		/// </summary>
		protected float ApplyBackMark(DamageInfo damage)
		{
			float value = damage.HealthDamage;
			if (damage.IsBackAttack && Time.time < _backMarkUntil)
			{
				value *= _backMarkMultiplier;
			}
			return value;
		}

		private void DestroyBackMarkVisual()
		{
			if (!(_backMarkVisual == null))
			{
				CombatFeedback.Release(_backMarkVisual);
				_backMarkVisual = null;
			}
		}
	}
}
