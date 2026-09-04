using UnityEngine;
using UnityEngine.UI;

namespace Eidren.UI
{
	/// <summary>
	/// Zweiter Teil von TechnologyTreeWindow (partial): das Scrollverhalten des
	/// Kartenrasters (W-010). Der Baum ist hoeher als der Bildschirm; die
	/// Auswahl muss der Tastatur-/Gamepad-Navigation in den sichtbaren Bereich
	/// folgen.
	/// </summary>
	public sealed partial class TechnologyTreeWindow
	{
		/// <summary>
		/// W-010: Liefert die vertikale Scrollposition (1 = oben), bei der die
		/// Karte [cardTop..cardBottom] (Abstaende von der Inhalts-Oberkante)
		/// vollstaendig im Sichtfenster liegt; bereits sichtbare Karten lassen
		/// die Position unveraendert.
		/// </summary>
		public static float VerticalScrollTargetFor(float contentHeight, float viewportHeight, float cardTop, float cardBottom, float current)
		{
			float overflow = contentHeight - viewportHeight;
			if (overflow <= 0f)
			{
				return 1f;
			}
			float windowTop = (1f - Mathf.Clamp01(current)) * overflow;
			if (cardTop < windowTop)
			{
				windowTop = cardTop;
			}
			else if (cardBottom > windowTop + viewportHeight)
			{
				windowTop = cardBottom - viewportHeight;
			}
			return 1f - Mathf.Clamp01(windowTop / overflow);
		}

		private void EnsureSelectedVisible()
		{
			// W-010: Die Auswahl folgt in den sichtbaren Scrollbereich —
			// wichtig fuer Tastatur- und Gamepad-Navigation.
			if (_scroll == null)
			{
				_scroll = panelRoot.GetComponentInChildren<ScrollRect>(true);
			}
			if (_scroll == null || _scroll.content == null || _selectedIndex >= nodeViews.Length)
			{
				return;
			}
			RectTransform card = (RectTransform)nodeViews[_selectedIndex].transform;
			RectTransform content = _scroll.content;
			RectTransform viewport = (_scroll.viewport != null) ? _scroll.viewport : (RectTransform)_scroll.transform;
			Vector2 local = content.InverseTransformPoint(card.TransformPoint(card.rect.center));
			float centerFromTop = content.rect.yMax - local.y;
			float halfHeight = card.rect.height * 0.5f;
			_scroll.verticalNormalizedPosition = VerticalScrollTargetFor(
				content.rect.height, viewport.rect.height,
				centerFromTop - halfHeight, centerFromTop + halfHeight,
				_scroll.verticalNormalizedPosition);
		}
	}
}
