using Eidren.UI;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// N04-003: Baut das Tooltip-Feld und die Ausloeser an den beiden
	/// Faehigkeitsknoepfen ins Kampf-HUD-Prefab.
	///
	/// PATCH-Semantik wie beim Aufgaben-Label: Das Prefab wird geladen,
	/// ergaenzt und zurueckgeschrieben — ein Neubau von Grund auf wuerde die
	/// gewachsenen Bestandteile abwerfen (Minimap, Aufgaben-Label,
	/// Statusleisten; die Falle ist in dieser Runde mehrfach dokumentiert).
	/// Mehrfache Laeufe erzeugen denselben Endzustand.
	/// </summary>
	public static class HudTooltipBuilder
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";

		private const string PanelName = "TooltipPanel";

		[MenuItem("Eidren/V0.4/N04-003 Tooltip ins Kampf-HUD einbauen")]
		public static void Build()
		{
			GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
			try
			{
				Transform safeArea = FindDeep(root.transform, "SafeArea");
				if (safeArea == null)
				{
					throw new InvalidOperationException("SafeArea fehlt im HUD.");
				}
				Transform previous = FindDeep(safeArea, PanelName);
				if (previous != null)
				{
					UnityEngine.Object.DestroyImmediate(previous.gameObject);
				}

				GameObject panelObject = new GameObject(PanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HudTooltipPanel));
				panelObject.transform.SetParent(safeArea, worldPositionStays: false);
				Image hintergrund = panelObject.GetComponent<Image>();
				hintergrund.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
				hintergrund.type = Image.Type.Sliced;
				hintergrund.color = new Color(0.02f, 0.04f, 0.05f, 0.92f);
				hintergrund.raycastTarget = false;
				RectTransform panelRect = panelObject.GetComponent<RectTransform>();
				// Ueber der Knopfleiste, mittig — dort verdeckt er weder die
				// Lebensleiste noch die Minimap.
				panelRect.anchorMin = new Vector2(0.5f, 0f);
				panelRect.anchorMax = new Vector2(0.5f, 0f);
				panelRect.pivot = new Vector2(0.5f, 0f);
				panelRect.sizeDelta = new Vector2(900f, 0f);
				panelRect.anchoredPosition = new Vector2(0f, 190f);

				// F32-009: Die Hoehe folgt dem Inhalt. Vorher stand hier eine
				// feste Hoehe von 46 und der Text lief rechts aus dem Balken
				// heraus (mit Bild gemeldet). Die Breite bleibt fest, die
				// Zeilenzahl waechst — bei Bedarf zwei oder mehr Reihen.
				VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
				layout.padding = new RectOffset(18, 18, 10, 10);
				layout.childControlWidth = true;
				layout.childControlHeight = true;
				layout.childForceExpandWidth = true;
				layout.childForceExpandHeight = false;
				ContentSizeFitter fitter = panelObject.AddComponent<ContentSizeFitter>();
				fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
				fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

				GameObject labelObject = new GameObject("TooltipText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
				labelObject.transform.SetParent(panelObject.transform, worldPositionStays: false);
				Text text = labelObject.GetComponent<Text>();
				text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
				text.fontSize = 19;
				text.alignment = TextAnchor.MiddleCenter;
				text.color = new Color(0.96f, 0.92f, 0.8f);
				// F32-009: UMBRECHEN, nicht ueberlaufen. Die uebrigen
				// HUD-Beschriftungen stehen bewusst auf Overflow — dort sind es
				// kurze Einzelwoerter. Der Tooltip ist lang und variabel; die
				// laengste Fassung ist der Schmelzbrand, seit er beide
				// Wirkungen nennt.
				text.horizontalOverflow = HorizontalWrapMode.Wrap;
				text.verticalOverflow = VerticalWrapMode.Overflow;
				text.raycastTarget = false;
				// Groesse kommt von der Layoutgruppe des Balkens — eigene
				// Anker wuerden sie ueberstimmen.

				panelObject.GetComponent<HudTooltipPanel>().ConfigureReferences(panelObject, text);
				panelObject.SetActive(value: false);

				Ausloeser(root.transform, "EidraSkill1");
				Ausloeser(root.transform, "EidraSkill2");

				if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
				{
					throw new InvalidOperationException("CombatHUD.prefab ließ sich nicht speichern.");
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
			AssetDatabase.SaveAssets();
			Debug.Log("[Eidren] N04-003: Tooltip-Feld und Auslöser im Kampf-HUD.");
		}

		private static void Ausloeser(Transform root, string knopfName)
		{
			Transform knopf = FindDeep(root, knopfName);
			if (knopf == null)
			{
				throw new InvalidOperationException("Fähigkeitsknopf fehlt im HUD: " + knopfName);
			}
			if (knopf.GetComponent<HudAbilityTooltip>() == null)
			{
				knopf.gameObject.AddComponent<HudAbilityTooltip>();
			}
		}

		private static Transform FindDeep(Transform root, string name)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (child.name == name)
				{
					return child;
				}
			}
			return null;
		}
	}
}
