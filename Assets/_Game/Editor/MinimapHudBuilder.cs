using Eidren.Data;
using Eidren.UI;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Editor
{
	/// <summary>
	/// N04-001: Baut die Minimap ins Kampf-HUD-Prefab. UI wird autoriert, nicht
	/// zur Laufzeit erzeugt (§16). Idempotent: Der Lauf entfernt einen vorhandenen
	/// Kartenknoten und baut ihn neu, das Ergebnis ist immer dasselbe.
	///
	/// Platzierung nach Nutzerentscheid: unter den beiden Eckknoepfen
	/// (InventoryEdgeAction, EidraSwitch), damit sich nichts Gewohntes verschiebt.
	/// </summary>
	public static class MinimapHudBuilder
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";

		private const string SymbolFolder = "Assets/_Game/Art/UI/Minimap/";

		private const string MinimapName = "Minimap";

		// Vorschlagswerte. Groesse, Radius und Symbolgroessen werden nach dem
		// ersten Bildabgleich gemeinsam nachgezogen.
		private const float MapSize = 240f;

		private const float MapRadius = 110f;

		private const float WorldRadius = 22f;

		private const float MapYaw = 45f;

		private const float RightMargin = -24f;

		private const float TopMargin = -96f;

		[MenuItem("Eidren/V0.3.1/N04-001 Minimap ins Kampf-HUD einbauen")]
		public static void Build()
		{
			Sprite dot = LoadSymbol("UI_MinimapDot.png");
			Sprite line = LoadSymbol("UI_MinimapLine.png");
			Sprite boss = LoadSymbol("UI_MinimapBoss.png");
			Sprite chest = LoadSymbol("UI_MinimapChest.png");

			GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
			try
			{
				CombatHUD hud = root.GetComponent<CombatHUD>();
				if (hud == null)
				{
					throw new InvalidOperationException("CombatHUD-Komponente fehlt in " + PrefabPath);
				}
				Transform safeArea = FindDeep(root.transform, "SafeArea");
				if (safeArea == null)
				{
					throw new InvalidOperationException("SafeArea fehlt im HUD — die Karte muss innerhalb der sicheren Flaeche liegen.");
				}
				Transform previous = FindDeep(safeArea, MinimapName);
				if (previous != null)
				{
					UnityEngine.Object.DestroyImmediate(previous.gameObject);
				}

				MinimapPresenter presenter = BuildMinimap(safeArea, dot, boss, chest, line);
				hud.ConfigureMinimap(presenter);
				EditorUtility.SetDirty(hud);

				if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
				{
					throw new InvalidOperationException("CombatHUD.prefab liess sich nicht speichern.");
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
			AssetDatabase.SaveAssets();
			Debug.Log("[N04-001] Minimap ins Kampf-HUD eingebaut.");
		}

		private static MinimapPresenter BuildMinimap(Transform parent, Sprite dot, Sprite boss, Sprite chest, Sprite line)
		{
			GameObject minimap = NewRect(parent, MinimapName);
			RectTransform rect = (RectTransform)minimap.transform;
			rect.anchorMin = new Vector2(1f, 1f);
			rect.anchorMax = new Vector2(1f, 1f);
			rect.pivot = new Vector2(1f, 1f);
			rect.sizeDelta = new Vector2(MapSize, MapSize);
			rect.anchoredPosition = new Vector2(RightMargin, TopMargin);

			Image background = minimap.AddComponent<Image>();
			background.color = new Color(0.06f, 0.07f, 0.09f, 0.72f);
			background.raycastTarget = false;
			// Sicherheitsnetz: Der Radius verwirft bereits alles ausserhalb, aber
			// ein halb ueberstehendes Symbol am Rand soll sauber abschneiden.
			minimap.AddComponent<RectMask2D>();

			GameObject markers = NewRect(minimap.transform, "Marker");
			RectTransform markerRect = (RectTransform)markers.transform;
			markerRect.anchorMin = new Vector2(0.5f, 0.5f);
			markerRect.anchorMax = new Vector2(0.5f, 0.5f);
			markerRect.pivot = new Vector2(0.5f, 0.5f);
			markerRect.sizeDelta = Vector2.zero;
			markerRect.anchoredPosition = Vector2.zero;

			// Der Kartenrand liegt unter den Markern, damit Symbole nicht von der
			// Linie durchschnitten werden.
			GameObject edges = NewRect(minimap.transform, "Rand");
			RectTransform edgeRect = (RectTransform)edges.transform;
			edgeRect.anchorMin = new Vector2(0.5f, 0.5f);
			edgeRect.anchorMax = new Vector2(0.5f, 0.5f);
			edgeRect.pivot = new Vector2(0.5f, 0.5f);
			edgeRect.sizeDelta = Vector2.zero;
			edgeRect.anchoredPosition = Vector2.zero;
			edges.transform.SetSiblingIndex(0);

			GameObject template = NewRect(minimap.transform, "MarkerVorlage");
			Image templateImage = template.AddComponent<Image>();
			templateImage.sprite = dot;
			templateImage.raycastTarget = false;
			CenterRect((RectTransform)template.transform, 12f);
			template.SetActive(value: false);

			// Die Figur sitzt immer in der Mitte — ein fester Punkt, kein Marker
			// aus dem Pool.
			GameObject self = NewRect(minimap.transform, "Spieler");
			Image selfImage = self.AddComponent<Image>();
			selfImage.sprite = dot;
			selfImage.color = new Color(0.96f, 0.97f, 1f, 1f);
			selfImage.raycastTarget = false;
			CenterRect((RectTransform)self.transform, 16f);

			MinimapPresenter presenter = minimap.AddComponent<MinimapPresenter>();
			presenter.ConfigureReferences(rect, markerRect, edgeRect, templateImage, dot, boss, chest, line);
			presenter.ConfigureRange(WorldRadius, MapYaw, MapRadius);
			presenter.ConfigureStyle(MinimapStyle.Default, ResourcePalette());
			EditorUtility.SetDirty(presenter);
			return presenter;
		}

		/// <summary>
		/// Eine Farbe je Materialfamilie: Holz gruen, Faser hellgruen, Stein grau,
		/// Erz metallisch, Beeren rot. T2-Varianten stehen dunkler daneben.
		/// </summary>
		private static MinimapResourceColor[] ResourcePalette()
		{
			return new MinimapResourceColor[9]
			{
				Entry(ResourceNodeIds.Tree, 0.42f, 0.68f, 0.36f),
				Entry(ResourceNodeIds.HardwoodTree, 0.25f, 0.50f, 0.28f),
				Entry(ResourceNodeIds.FiberPlant, 0.76f, 0.82f, 0.44f),
				Entry(ResourceNodeIds.SwampHemp, 0.56f, 0.66f, 0.34f),
				Entry(ResourceNodeIds.StoneDeposit, 0.68f, 0.70f, 0.72f),
				Entry(ResourceNodeIds.GraniteDeposit, 0.48f, 0.52f, 0.58f),
				Entry(ResourceNodeIds.CopperVein, 0.87f, 0.52f, 0.27f),
				Entry(ResourceNodeIds.IronVein, 0.62f, 0.68f, 0.78f),
				Entry(ResourceNodeIds.BerryBush, 0.82f, 0.34f, 0.44f)
			};
		}

		private static MinimapResourceColor Entry(string paletteId, float r, float g, float b)
		{
			return new MinimapResourceColor
			{
				paletteId = paletteId,
				color = new Color(r, g, b, 1f)
			};
		}

		private static void CenterRect(RectTransform rect, float size)
		{
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = new Vector2(size, size);
			rect.anchoredPosition = Vector2.zero;
		}

		private static GameObject NewRect(Transform parent, string name)
		{
			GameObject created = new GameObject(name, typeof(RectTransform));
			created.transform.SetParent(parent, worldPositionStays: false);
			created.layer = parent.gameObject.layer;
			return created;
		}

		private static Transform FindDeep(Transform parent, string name)
		{
			if (string.Equals(parent.name, name, StringComparison.Ordinal))
			{
				return parent;
			}
			for (int i = 0; i < parent.childCount; i++)
			{
				Transform found = FindDeep(parent.GetChild(i), name);
				if (found != null)
				{
					return found;
				}
			}
			return null;
		}

		/// <summary>
		/// Die drei Symbole kommen als PNG aus <c>Tools/Make-MinimapSymbols.py</c>
		/// und muessen als Sprite importiert sein, sonst laesst Unity sie nicht in
		/// ein <c>Image</c> haengen.
		/// </summary>
		private static Sprite LoadSymbol(string fileName)
		{
			string path = SymbolFolder + fileName;
			TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
			if (importer == null)
			{
				throw new InvalidOperationException("Minimap-Symbol fehlt: " + path);
			}
			if (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency || importer.mipmapEnabled)
			{
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Single;
				importer.alphaIsTransparency = true;
				importer.mipmapEnabled = false;
				importer.filterMode = FilterMode.Bilinear;
				importer.SaveAndReimport();
			}
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
			if (sprite == null)
			{
				throw new InvalidOperationException("Minimap-Symbol liess sich nicht als Sprite laden: " + path);
			}
			return sprite;
		}
	}
}
