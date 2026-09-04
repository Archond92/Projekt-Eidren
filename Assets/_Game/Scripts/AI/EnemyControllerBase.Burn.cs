using Eidren.Combat;
using Eidren.Presentation;
using UnityEngine;

namespace Eidren.AI
{
	/// <summary>
	/// F32-005: Nachbrand am Gegner (Ignivars Passiv). Wie das Rueckenmal
	/// liegt er am GEMEINSAMEN Gegnertyp — jeder Gegner kann brennen, an
	/// keine Eigenschaft und keinen Typ gebunden (oberste Praemisse).
	/// </summary>
	public abstract partial class EnemyControllerBase
	{
		private const string BurnAttackId = "eidra.ignivar.burn";

		private readonly PassiveBurnState _burn = new PassiveBurnState();

		private GameObject _burnSource;

		public bool IsBurning => _burn.IsBurning;

		/// <summary>
		/// Entzuendet das Ziel. Der Gesamtschaden verteilt sich auf
		/// <paramref name="seconds"/> Sekundentakte; ein neuer Treffer
		/// frischt auf, statt zu stapeln.
		/// </summary>
		public void ApplyBurn(float totalDamage, int seconds, GameObject source)
		{
			if (totalDamage <= 0f || seconds <= 0 || !IsAlive)
			{
				return;
			}
			bool warAus = !_burn.IsBurning;
			_burn.Refresh(totalDamage, seconds);
			_burnSource = source;
			if (warAus)
			{
				CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * DamageFeedbackHeight, "BRENNT", new Color(1f, 0.45f, 0.1f), 0.9f);
			}
		}

		public void ClearBurn()
		{
			_burn.Clear();
			_burnSource = null;
		}

		/// <summary>
		/// Wird aus dem bestehenden Update getaktet — kein neuer Frame-Pfad.
		/// </summary>
		private void TickBurn(float deltaTime)
		{
			if (!IsAlive)
			{
				if (_burn.IsBurning)
				{
					ClearBurn();
				}
				return;
			}
			if (_burn.TryTick(deltaTime, out var damage))
			{
				ApplyDamage(new DamageInfo(damage, 0f, base.transform.position, _burnSource, isBackAttack: false, BurnAttackId, "ignivar"));
			}
		}
	}
}
