using Eidren.UI;
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// W-010: Der Technologiebaum ist scrollbar — die Knotenkarten liegen in
	/// einem ScrollRect, Bedienhinweis und Aktionsbuttons in einer festen
	/// Fussleiste ausserhalb des Scrollinhalts. Die Auswahl folgt per
	/// VerticalScrollTargetFor in den sichtbaren Bereich.
	/// </summary>
	public sealed class TechnologyTreeScrollTests
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/TechnologyTreeWindow.prefab";

		// Fenster oben (current=1), Karte im sichtbaren Bereich -> unveraendert
		[TestCase(2000f, 700f, 0f, 92f, 1f, 1f)]
		// Karte unterhalb des Fensters -> Fenster rutscht, bis die Kartenunterkante sichtbar ist
		[TestCase(2000f, 700f, 1900f, 1992f, 1f, 0.00615f)]
		// Fenster unten (current=0), Karte oberhalb -> Fenster springt zur Kartenoberkante
		[TestCase(2000f, 700f, 100f, 192f, 0f, 0.92308f)]
		// Inhalt passt komplett hinein -> immer oben
		[TestCase(600f, 700f, 0f, 92f, 0.5f, 1f)]
		public void VerticalScrollTargetFor_HaeltDieKarteImSichtfenster(float contentHeight, float viewportHeight, float cardTop, float cardBottom, float current, float expected)
		{
			Assert.That(TechnologyTreeWindow.VerticalScrollTargetFor(contentHeight, viewportHeight, cardTop, cardBottom, current),
				Is.EqualTo(expected).Within(0.001f));
		}

		[Test]
		public void Prefab_KartenLiegenImScrollinhaltButtonsInDerFussleiste()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(prefab, Is.Not.Null);
			ScrollRect scroll = prefab.GetComponentInChildren<ScrollRect>(true);
			Assert.That(scroll, Is.Not.Null, "Kein ScrollRect im Technologiebaum");
			Assert.That(scroll.content, Is.Not.Null, "ScrollRect ohne Inhalt");
			Assert.That(scroll.vertical, Is.True);
			Assert.That(scroll.horizontal, Is.False);
			Assert.That(scroll.content.GetComponent<GridLayoutGroup>(), Is.Not.Null,
				"Das Kartenraster muss der Scrollinhalt sein");

			TechnologyNodeButtonView[] views = prefab.GetComponentsInChildren<TechnologyNodeButtonView>(true);
			Assert.That(views.Length, Is.GreaterThanOrEqualTo(23));
			foreach (TechnologyNodeButtonView view in views)
			{
				Assert.That(view.transform.IsChildOf(scroll.content), Is.True,
					view.name + " liegt nicht im Scrollinhalt");
			}

			TechnologyTreeWindow window = prefab.GetComponentInChildren<TechnologyTreeWindow>(true);
			Assert.That(window, Is.Not.Null);
			Assert.That(window.UnlockButton.transform.IsChildOf(scroll.content), Is.False,
				"FREISCHALTEN darf nicht mitscrollen");
			Button close = prefab.GetComponentsInChildren<Button>(true)
				.First(button => button.name == "CloseButton");
			Assert.That(close.transform.IsChildOf(scroll.content), Is.False,
				"SCHLIESSEN darf nicht mitscrollen");
			Transform hint = prefab.GetComponentsInChildren<Transform>(true)
				.FirstOrDefault(t => t.name == "Hint");
			Assert.That(hint, Is.Not.Null);
			Assert.That(hint.IsChildOf(scroll.content), Is.False,
				"Der Bedienhinweis darf nicht mitscrollen");
		}
	}
}
