using Eidren.UI;
using System.IO;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
public static class FixCollectionHudBuilder
{
	private const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";

	private const string BarSpritePath = "Assets/_Game/Resources/Art/UI/ui_bar_fill.png";

	/// <summary>
	/// Schlichte weisse Flaeche als Fuellgrafik der Statusbalken. Ohne Sprite
	/// faellt eine Image mit Type.Filled auf das volle Rechteck zurueck und
	/// ignoriert fillAmount — der Balken stuende dauerhaft voll (F-002).
	/// </summary>
	public static Sprite EnsureBarSprite()
	{
		Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(BarSpritePath);
		if (existing != null)
		{
			return existing;
		}
		Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, mipChain: false);
		Color[] pixels = new Color[64];
		for (int index = 0; index < pixels.Length; index++)
		{
			pixels[index] = Color.white;
		}
		texture.SetPixels(pixels);
		texture.Apply();
		Directory.CreateDirectory(Path.GetDirectoryName(BarSpritePath));
		File.WriteAllBytes(BarSpritePath, texture.EncodeToPNG());
		Object.DestroyImmediate(texture);
		AssetDatabase.ImportAsset(BarSpritePath, ImportAssetOptions.ForceUpdate);
		TextureImporter importer = AssetImporter.GetAtPath(BarSpritePath) as TextureImporter;
		if (importer != null)
		{
			importer.textureType = TextureImporterType.Sprite;
			importer.spriteImportMode = SpriteImportMode.Single;
			importer.alphaIsTransparency = true;
			importer.mipmapEnabled = false;
			importer.maxTextureSize = 32;
			importer.filterMode = FilterMode.Bilinear;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.SaveAndReimport();
		}
		Sprite created = AssetDatabase.LoadAssetAtPath<Sprite>(BarSpritePath);
		if (created == null)
		{
			throw new IOException("Could not import bar fill sprite: " + BarSpritePath);
		}
		return created;
	}

	/// <summary>
	/// Gibt jeder gefuellten Statusgrafik eine Fuellgrafik, damit der gesetzte
	/// Fuellwert ueberhaupt gezeichnet wird. Idempotent.
	/// </summary>
	[MenuItem("Eidren/V0.2/Fixes/Upgrade Status Bars")]
	public static void UpgradeStatusBars()
	{
		Sprite bar = EnsureBarSprite();
		GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
		try
		{
			int patched = 0;
			Image[] images = root.GetComponentsInChildren<Image>(includeInactive: true);
			foreach (Image image in images)
			{
				if (image.type == Image.Type.Filled && image.sprite == null)
				{
					image.sprite = bar;
					patched++;
				}
			}
			Debug.Log($"[F-002] Statusbalken mit Fuellgrafik versehen: {patched}");
			if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
			{
				throw new IOException("Could not save CombatHUD prefab.");
			}
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	[MenuItem("Eidren/V0.2/Fixes/Upgrade XP HUD")]
	public static void UpgradeXpHud()
	{
		GameObject root = PrefabUtility.LoadPrefabContents("Assets/_Game/Resources/UI/CombatHUD.prefab");
		try
		{
			CombatHUD component = root.GetComponent<CombatHUD>();
			CombatHudStatusPresenter status = root.GetComponentInChildren<CombatHudStatusPresenter>(includeInactive: true);
			SafeAreaPanel safe = root.GetComponentInChildren<SafeAreaPanel>(includeInactive: true);
			if (component == null || status == null || safe == null)
			{
				throw new InvalidDataException("Combat HUD is incomplete.");
			}
			RectTransform playerStatus = safe.transform.Find("PlayerStatus") as RectTransform;
			if (playerStatus == null)
			{
				throw new InvalidDataException("PlayerStatus block is missing.");
			}
			Font font = root.GetComponentInChildren<Text>(includeInactive: true).font;
			// F-001: Die Altfassung lag als eigenes Panel oben rechts hinter dem
			// Rucksack-Button und nutzte das erstbeste Sprite (Charakterporträt).
			Transform legacyPanel = safe.transform.Find("ExperiencePanel");
			if (legacyPanel != null)
			{
				Object.DestroyImmediate(legacyPanel.gameObject);
			}
			// F-004: Level und XP-Leiste gehören in den Spielerstatus links oben,
			// unterhalb der Resonanzleiste, als reine Farbflächen ohne Fremd-Sprite.
			Image back = Ensure<Image>(Rect(playerStatus, "ExperienceBack").gameObject);
			RectTransform backRect = back.rectTransform;
			backRect.anchorMin = new Vector2(0f, 1f);
			backRect.anchorMax = new Vector2(0f, 1f);
			backRect.pivot = new Vector2(0f, 1f);
			backRect.anchoredPosition = new Vector2(132f, -131f);
			backRect.sizeDelta = new Vector2(178f, 12f);
			back.sprite = null;
			back.color = new Color(0.05f, 0.06f, 0.07f, 0.95f);
			back.raycastTarget = false;
			Image fill = Ensure<Image>(Rect(backRect, "ExperienceFill").gameObject);
			Stretch(fill.rectTransform);
			fill.sprite = EnsureBarSprite();
			fill.type = Image.Type.Filled;
			fill.fillMethod = Image.FillMethod.Horizontal;
			fill.fillOrigin = 0;
			fill.fillAmount = 0f;
			fill.color = new Color(0.94f, 0.62f, 0.18f);
			fill.raycastTarget = false;
			Text level = Ensure<Text>(Rect(playerStatus, "LevelValue").gameObject);
			RectTransform levelRect = level.rectTransform;
			levelRect.anchorMin = new Vector2(0f, 1f);
			levelRect.anchorMax = new Vector2(0f, 1f);
			levelRect.pivot = new Vector2(0f, 1f);
			levelRect.anchoredPosition = new Vector2(318f, -128f);
			levelRect.sizeDelta = new Vector2(72f, 18f);
			level.font = font;
			level.fontSize = 14;
			level.fontStyle = FontStyle.Bold;
			level.alignment = TextAnchor.MiddleLeft;
			level.color = new Color(0.95f, 0.88f, 0.67f);
			level.text = "LV 1";
			level.raycastTarget = false;
			status.ConfigureProgressionReferences(fill, level);
			EditorUtility.SetDirty(status);
			if (PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/UI/CombatHUD.prefab") == null)
			{
				throw new IOException("Could not save CombatHUD prefab.");
			}
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private static RectTransform Rect(Transform parent, string name)
	{
		Transform existing = parent.Find(name);
		if (existing != null)
		{
			return (RectTransform)existing;
		}
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		return (RectTransform)gameObject.transform;
	}

	private static T Ensure<T>(GameObject value) where T : Component
	{
		T component = value.GetComponent<T>();
		if (!(component != null))
		{
			return value.AddComponent<T>();
		}
		return component;
	}

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}
}
}
