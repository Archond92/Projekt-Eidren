using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Editor
{
	/// <summary>
	/// W-010: Macht den Technologiebaum scrollbar. Das Kartenraster (NodeGrid
	/// mit GridLayoutGroup) wandert in ein ScrollRect mit Maske; Bedienhinweis,
	/// FREISCHALTEN und SCHLIESSEN ziehen in eine feste Fussleiste, damit sie
	/// die Karten nicht mehr ueberlagern. Laufzeit-Klone (EnsureNodeViewCapacity)
	/// fliessen weiter ueber die GridLayoutGroup ins Raster. Idempotent.
	/// </summary>
	public static class TechnologyTreeScrollBuilder
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/TechnologyTreeWindow.prefab";

		[MenuItem("Eidren/V0.2/W-010 Technologiebaum scrollbar machen")]
		public static void Build()
		{
			GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
			try
			{
				Transform card = root.transform.Find("SafeArea/PanelRoot/TechnologyCard");
				if (card == null)
				{
					throw new InvalidOperationException("TechnologyCard fehlt im Prefab.");
				}
				RectTransform grid = card.Find("NodeGrid") as RectTransform
					?? card.GetComponentInChildren<GridLayoutGroup>(true)?.GetComponent<RectTransform>();
				if (grid == null)
				{
					throw new InvalidOperationException("NodeGrid mit GridLayoutGroup fehlt.");
				}

				ScrollRect scroll = card.GetComponentInChildren<ScrollRect>(true);
				if (scroll == null)
				{
					GameObject scrollGo = new GameObject("NodeScroll",
						typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
					RectTransform scrollRect = (RectTransform)scrollGo.transform;
					scrollRect.SetParent(card, worldPositionStays: false);
					scrollRect.anchorMin = Vector2.zero;
					scrollRect.anchorMax = Vector2.one;
					// oben Platz fuer Titel/Punkte, unten fuer die Fussleiste
					scrollRect.offsetMin = new Vector2(24f, 120f);
					scrollRect.offsetMax = new Vector2(-24f, -96f);
					Image raycastSurface = scrollGo.GetComponent<Image>();
					raycastSurface.color = new Color(0f, 0f, 0f, 0.01f);
					scroll = scrollGo.GetComponent<ScrollRect>();
					scroll.horizontal = false;
					scroll.vertical = true;
					scroll.movementType = ScrollRect.MovementType.Clamped;
					scroll.scrollSensitivity = 40f;
					scroll.viewport = scrollRect;

					grid.SetParent(scrollRect, worldPositionStays: false);
					grid.anchorMin = new Vector2(0f, 1f);
					grid.anchorMax = new Vector2(1f, 1f);
					grid.pivot = new Vector2(0.5f, 1f);
					grid.offsetMin = new Vector2(0f, grid.offsetMin.y);
					grid.offsetMax = new Vector2(0f, 0f);
					grid.anchoredPosition = Vector2.zero;
					scroll.content = grid;

					ContentSizeFitter fitter = grid.GetComponent<ContentSizeFitter>();
					if (fitter == null)
					{
						fitter = grid.gameObject.AddComponent<ContentSizeFitter>();
					}
					fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
					fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
				}

				RectTransform footer = card.Find("Footer") as RectTransform;
				if (footer == null)
				{
					GameObject footerGo = new GameObject("Footer", typeof(RectTransform));
					footer = (RectTransform)footerGo.transform;
					footer.SetParent(card, worldPositionStays: false);
					footer.anchorMin = new Vector2(0f, 0f);
					footer.anchorMax = new Vector2(1f, 0f);
					footer.pivot = new Vector2(0.5f, 0f);
					footer.offsetMin = new Vector2(24f, 12f);
					footer.offsetMax = new Vector2(-24f, 104f);
				}
				MoveToFooter(card, footer, "Hint", new Vector2(0f, 0f), new Vector2(0.5f, 1f));
				MoveToFooter(card, footer, "UnlockButton", new Vector2(0.54f, 0.08f), new Vector2(0.76f, 0.92f));
				MoveToFooter(card, footer, "CloseButton", new Vector2(0.79f, 0.08f), new Vector2(1f, 0.92f));

				if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
				{
					throw new InvalidOperationException("TechnologyTreeWindow.prefab liess sich nicht speichern.");
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
			AssetDatabase.SaveAssets();
			Debug.Log("[W010] Technologiebaum ist scrollbar; Hinweis und Buttons sitzen in der Fussleiste.");
		}

		private static void MoveToFooter(Transform card, RectTransform footer, string childName, Vector2 anchorMin, Vector2 anchorMax)
		{
			Transform child = card.Find(childName) ?? footer.Find(childName);
			if (child == null)
			{
				throw new InvalidOperationException(childName + " fehlt im Prefab.");
			}
			RectTransform rect = (RectTransform)child;
			rect.SetParent(footer, worldPositionStays: false);
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			rect.anchoredPosition = Vector2.zero;
		}
	}
}
