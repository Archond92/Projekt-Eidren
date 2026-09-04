using Eidren.Data;
using Eidren.UI;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	public sealed class BuildingMenuLayoutTests
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/BuildingMenuWindow.prefab";

		[Test]
		public void Aktionen_TragenEinheitlicheUndEindeutigeBeschriftungen()
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(root, Is.Not.Null, "Baumenü-Prefab fehlt: " + PrefabPath);
			Assert.That(LabelOf(root, "PlaceButton"), Is.EqualTo("PLATZIEREN"),
				"Die primäre Aktion darf nicht technisch „Vorschau“ heißen");
			Assert.That(LabelOf(root, "MoveButton"), Is.EqualTo("VERSCHIEBEN"));
			Assert.That(LabelOf(root, "DemolishButton"), Is.EqualTo("ABREISSEN"));
			Assert.That(LabelOf(root, "CloseButton"), Is.EqualTo("SCHLIESSEN"));
		}

		[Test]
		public void PrimaereAktion_IstStaerkerGewichtetAlsSchliessen()
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			RectTransform place = RectOf(root, "PlaceButton");
			RectTransform close = RectOf(root, "CloseButton");
			float placeHeight = place.anchorMax.y - place.anchorMin.y;
			float closeHeight = close.anchorMax.y - close.anchorMin.y;
			Assert.That(placeHeight, Is.GreaterThan(closeHeight),
				"Der Schließen-Knopf darf das Bauen nicht optisch dominieren");
		}

		[Test]
		public void Katalogstreifen_LaesstKeineGrosseLeereFlaeche()
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			RectTransform list = RectOf(root, "BuildingList");
			float listHeight = list.anchorMax.y - list.anchorMin.y;
			Assert.That(listHeight, Is.LessThanOrEqualTo(0.36f),
				"Die Katalogreihe ist nur rund 92 px hoch; ein hoher Block hinterlässt eine leere Fläche");
		}

		[Test]
		public void Menuebereiche_UeberlappenEinanderNicht()
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			string[] areas = new string[6] { "BuildingList", "Details", "Feedback", "PlaceButton", "DemolishButton", "CloseButton" };
			for (int first = 0; first < areas.Length; first++)
			{
				for (int second = first + 1; second < areas.Length; second++)
				{
					Assert.That(Overlaps(RectOf(root, areas[first]), RectOf(root, areas[second])), Is.False,
						$"Bereiche überdecken einander: {areas[first]} / {areas[second]}");
				}
			}
		}

		[Test]
		public void JedeKategorie_BesitztEineLesbareUeberschrift()
		{
			foreach (BuildingCategory category in System.Enum.GetValues(typeof(BuildingCategory)))
			{
				Assert.That(BuildingMenuWindow.CategoryText(category), Is.Not.Empty,
					"Kategorie ohne Überschrift: " + category);
			}
		}

		[Test]
		public void Katalogzeilen_ReichenDieKategorieDurch()
		{
			string source = File.ReadAllText("Assets/_Game/Scripts/UI/BuildingMenuWindow.cs");
			int rowFor = source.IndexOf("private BuildingMenuRow RowFor(");
			Assert.That(rowFor, Is.GreaterThan(-1), "RowFor nicht gefunden");
			string body = source.Substring(rowFor, 400);
			Assert.That(body, Does.Contain("CategoryText("),
				"RowFor liefert weiterhin eine leere Kategorie; die Überschriften können nicht erscheinen");
		}

		[Test]
		public void Kostenanzeige_NenntBedarfUndBestandVerstaendlich()
		{
			Assert.That(BuildingMenuWindow.DetailCostLine("Holz", 10, 70),
				Is.EqualTo("Holz: 10 benötigt · 70 vorhanden"));
			Assert.That(BuildingMenuWindow.DetailCostLine("Holz", 10, 4),
				Is.EqualTo("Holz: 10 benötigt · 4 vorhanden · es fehlen 6"),
				"Fehlmengen müssen im Text stehen, nicht nur farblich");
		}

		[Test]
		public void KompakteKostenanzeige_MarkiertFehlmengeMitText()
		{
			Assert.That(BuildingMenuWindow.CompactCostEntry("Holz", 10, 70), Is.EqualTo("Holz 10/70"));
			Assert.That(BuildingMenuWindow.CompactCostEntry("Holz", 10, 4), Is.EqualTo("Holz 10/4 (fehlt 6)"),
				"Auch die Karte muss Mangel ohne Farbe erkennbar machen");
		}

		private static bool Overlaps(RectTransform first, RectTransform second)
		{
			bool separateX = first.anchorMax.x <= second.anchorMin.x + 0.001f || second.anchorMax.x <= first.anchorMin.x + 0.001f;
			bool separateY = first.anchorMax.y <= second.anchorMin.y + 0.001f || second.anchorMax.y <= first.anchorMin.y + 0.001f;
			return !separateX && !separateY;
		}

		private static RectTransform RectOf(GameObject root, string childName)
		{
			Transform found = FindDeep(root.transform, childName);
			Assert.That(found, Is.Not.Null, "Knoten fehlt: " + childName);
			return (RectTransform)found;
		}

		private static string LabelOf(GameObject root, string buttonName)
		{
			Transform button = FindDeep(root.transform, buttonName);
			Assert.That(button, Is.Not.Null, "Knopf fehlt: " + buttonName);
			Text label = button.GetComponentInChildren<Text>(includeInactive: true);
			Assert.That(label, Is.Not.Null, "Beschriftung fehlt: " + buttonName);
			return label.text;
		}

		private static Transform FindDeep(Transform root, string childName)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (child.name == childName)
				{
					return child;
				}
			}
			return null;
		}
	}
}
