using Eidren.UI;
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// W-007: Die Katalogeintraege des Baumenues muessen nach dem Layoutlauf
	/// echte Breiten besitzen und nebeneinander liegen — nicht als 0 Pixel
	/// breite Eintraege uebereinander (childControlWidth war aus, wodurch die
	/// preferredWidth der Eintraege ignoriert wurde).
	/// </summary>
	public sealed class BuildingMenuCatalogLayoutTests
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/BuildingMenuWindow.prefab";

		[Test]
		public void Katalogeintraege_LiegenNebeneinanderMitEchterBreite()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(prefab, Is.Not.Null);
			GameObject instance = Object.Instantiate(prefab);
			try
			{
				BuildingMenuItemView[] views = instance.GetComponentsInChildren<BuildingMenuItemView>(true);
				Assert.That(views, Is.Not.Empty);
				RectTransform content = (RectTransform)views[0].transform.parent;
				LayoutRebuilder.ForceRebuildLayoutImmediate(content);

				RectTransform[] active = views
					.Where(view => view.gameObject.activeSelf)
					.Select(view => (RectTransform)view.transform)
					.ToArray();
				Assert.That(active.Length, Is.GreaterThanOrEqualTo(2), "Zu wenige aktive Katalogeintraege fuer die Pruefung");
				foreach (RectTransform entry in active)
				{
					Assert.That(entry.rect.width, Is.GreaterThan(100f), entry.name + " hat keine aufgeloeste Breite");
				}
				RectTransform[] ordered = active.OrderBy(entry => entry.anchoredPosition.x).ToArray();
				for (int index = 1; index < ordered.Length; index++)
				{
					Assert.That(ordered[index].anchoredPosition.x,
						Is.GreaterThanOrEqualTo(ordered[index - 1].anchoredPosition.x + ordered[index - 1].rect.width),
						"Eintraege ueberlappen: " + ordered[index - 1].name + " / " + ordered[index].name);
				}
			}
			finally
			{
				Object.DestroyImmediate(instance);
			}
		}
	}
}
