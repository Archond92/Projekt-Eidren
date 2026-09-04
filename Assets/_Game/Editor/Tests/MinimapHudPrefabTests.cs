using Eidren.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// N04-001: Die Karte muss im autorierten Prefab haengen (§16) und darf die
	/// beiden Eckknoepfe nicht verdecken — der Nutzerentscheid lautete "unter die
	/// Knoepfe".
	/// </summary>
	public sealed class MinimapHudPrefabTests
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";

		private const string SymbolFolder = "Assets/_Game/Art/UI/Minimap/";

		[Test]
		public void KampfHud_TraegtDieVerdrahteteMinimap()
		{
			CombatHUD hud = LoadHud();

			Assert.That(hud.Minimap, Is.Not.Null, "Die Minimap fehlt im HUD-Prefab — Laufzeitaufbau ist nach §16 verboten");
		}

		[Test]
		public void Minimap_LiegtUnterhalbDerBeidenEckknoepfe()
		{
			CombatHUD hud = LoadHud();
			RectTransform karte = hud.Minimap.GetComponent<RectTransform>();
			RectTransform inventar = Find(hud, "InventoryEdgeAction");
			RectTransform eidra = Find(hud, "EidraSwitch");

			Assert.That(karte.anchorMin, Is.EqualTo(new Vector2(1f, 1f)), "Die Karte muss oben rechts verankert sein");
			float knopfUnterkante = Mathf.Min(Bottom(inventar), Bottom(eidra));

			Assert.That(Top(karte), Is.LessThanOrEqualTo(knopfUnterkante), "Die Karte darf Inventarknopf und Eidra-Wechsel nicht ueberdecken");
		}

		[Test]
		public void Minimap_HatMarkerwurzelUndAbgeschalteteVorlage()
		{
			CombatHUD hud = LoadHud();
			Transform karte = hud.Minimap.transform;

			Transform markerRoot = karte.Find("Marker");
			Transform randRoot = karte.Find("Rand");
			Transform vorlage = karte.Find("MarkerVorlage");

			Assert.That(markerRoot, Is.Not.Null, "Ohne Markerwurzel hat der Pool kein Ziel");
			Assert.That(randRoot, Is.Not.Null, "Ohne Randwurzel laesst sich das Ende der Karte nicht zeichnen");
			Assert.That(vorlage, Is.Not.Null, "Ohne Vorlage kann der Pool nichts erzeugen");
			Assert.That(vorlage.gameObject.activeSelf, Is.False, "Die Vorlage darf nicht mitgezeichnet werden");
			Assert.That(vorlage.GetComponent<Image>(), Is.Not.Null, "Die Vorlage braucht ein Image");
		}

		[Test]
		public void AlleKartensymbole_SindAlsSpriteImportiert()
		{
			foreach (string datei in new string[4] { "UI_MinimapDot.png", "UI_MinimapBoss.png", "UI_MinimapChest.png", "UI_MinimapLine.png" })
			{
				Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SymbolFolder + datei);
				Assert.That(sprite, Is.Not.Null, "Symbol nicht als Sprite importiert: " + datei);
			}
		}

		private static float Top(RectTransform rect)
		{
			return rect.anchoredPosition.y + rect.sizeDelta.y * (1f - rect.pivot.y);
		}

		private static float Bottom(RectTransform rect)
		{
			return rect.anchoredPosition.y - rect.sizeDelta.y * rect.pivot.y;
		}

		private static RectTransform Find(CombatHUD hud, string name)
		{
			foreach (RectTransform candidate in hud.GetComponentsInChildren<RectTransform>(includeInactive: true))
			{
				if (candidate.name == name)
				{
					Assert.That(candidate.anchorMin, Is.EqualTo(new Vector2(1f, 1f)), name + " ist nicht mehr oben rechts verankert — der Vergleich der Kanten gilt dann nicht");
					return candidate;
				}
			}
			Assert.Fail("Element fehlt im HUD-Prefab: " + name);
			return null;
		}

		private static CombatHUD LoadHud()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(prefab, Is.Not.Null, "HUD-Prefab fehlt: " + PrefabPath);
			CombatHUD hud = prefab.GetComponent<CombatHUD>();
			Assert.That(hud, Is.Not.Null, "CombatHUD-Komponente fehlt im Prefab");
			return hud;
		}
	}
}
