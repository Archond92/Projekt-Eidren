using Eidren.Data;
using Eidren.Eidra;
using UnityEngine.EventSystems;
using UnityEngine;

namespace Eidren.UI
{
	/// <summary>
	/// N04-003: Zeigt an einem Faehigkeitsknopf, was die Faehigkeit tut.
	///
	/// Zwei Ausloeser, weil Beruehrung kein „Ueberfahren" kennt:
	/// Am Rechner erscheint der Tooltip beim Ueberfahren mit der Maus, auf
	/// dem Handy nach kurzem Halten. Kurzes Antippen loest die Faehigkeit
	/// weiter aus — der Tooltip kostet also keinen Tastendruck im Kampf.
	///
	/// Der Text kommt bei jedem Anzeigen frisch aus den Daten des aktiven
	/// Eidra: Das Gespann laesst sich im Kampf wechseln, ein einmal
	/// gespeicherter Text waere danach falsch.
	/// </summary>
	public sealed class HudAbilityTooltip : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
	{
		/// <summary>Haltedauer fuer die Beruehrungsbedienung.</summary>
		public const float Haltedauer = 0.4f;

		private EidraTeamController _eidra;

		private int _skillIndex;

		private HudTooltipPanel _panel;

		private bool _gedrueckt;

		private float _gedruecktSeit;

		private bool _zeigtGerade;

		public void Initialize(EidraTeamController eidra, int skillIndex, HudTooltipPanel panel)
		{
			_eidra = eidra;
			_skillIndex = skillIndex;
			_panel = panel;
			_gedrueckt = false;
			_zeigtGerade = false;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			// Auf dem Handy feuert Enter beim Antippen mit — dort wartet der
			// Tooltip auf das Halten, sonst blitzt er bei jedem Einsatz auf.
			if (!Application.isMobilePlatform)
			{
				Zeigen();
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			_gedrueckt = false;
			Verbergen();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			_gedrueckt = true;
			_gedruecktSeit = Time.unscaledTime;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			_gedrueckt = false;
			if (Application.isMobilePlatform)
			{
				Verbergen();
			}
		}

		private void Update()
		{
			if (_gedrueckt && !_zeigtGerade && Time.unscaledTime - _gedruecktSeit >= Haltedauer)
			{
				Zeigen();
			}
		}

		/// <summary>Text der gerade belegten Faehigkeit — leer, wenn kein Eidra aktiv ist.</summary>
		public string Text()
		{
			EidraData active = (_eidra != null) ? _eidra.ActiveData : null;
			if (active == null)
			{
				return string.Empty;
			}
			return EidraAbilityText.For((_skillIndex == 0) ? active.Skill1 : active.Skill2);
		}

		private void Zeigen()
		{
			string text = Text();
			if (_panel == null || string.IsNullOrEmpty(text))
			{
				return;
			}
			_panel.Show(text);
			_zeigtGerade = true;
		}

		private void Verbergen()
		{
			if (_zeigtGerade && _panel != null)
			{
				_panel.Hide();
			}
			_zeigtGerade = false;
		}

		private void OnDisable()
		{
			Verbergen();
			_gedrueckt = false;
		}
	}
}
