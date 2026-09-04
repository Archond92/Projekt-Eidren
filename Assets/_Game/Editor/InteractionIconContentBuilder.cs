using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class InteractionIconContentBuilder
{
	private const int Size = 256;

	private static readonly string[] Paths = new string[4] { "Assets/_Game/Resources/Art/UI/ui_interaction_hand.png", "Assets/_Game/Resources/Art/UI/ui_interaction_axe.png", "Assets/_Game/Resources/Art/UI/ui_interaction_scythe.png", "Assets/_Game/Resources/Art/UI/ui_interaction_pickaxe.png" };

	[MenuItem("Eidren/Art/Prepare Interaction Icons")]
	public static void BuildInteractionIcons()
	{
		string[] paths = Paths;
		foreach (string path in paths)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("Missing authored interaction glyph: " + path);
			}
			ImportAsSprite(path);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Verify();
		Debug.Log("Eidren: prepared four authored interaction glyphs.");
	}

	private static void ImportAsSprite(string path)
	{
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
		if (AssetImporter.GetAtPath(path) is TextureImporter importer)
		{
			importer.textureType = TextureImporterType.Sprite;
			importer.spriteImportMode = SpriteImportMode.Single;
			importer.alphaIsTransparency = true;
			importer.mipmapEnabled = false;
			importer.maxTextureSize = 256;
			importer.filterMode = FilterMode.Bilinear;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.SaveAndReimport();
		}
	}

	private static void Verify()
	{
		string[] paths = Paths;
		foreach (string path in paths)
		{
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
			if (sprite == null)
			{
				throw new FileNotFoundException("Interaction glyph was not imported: " + path);
			}
			if (sprite.rect.width != 256f || sprite.rect.height != 256f)
			{
				throw new InvalidDataException($"Interaction glyph must be {256}x{256}: {path}");
			}
		}
	}
}
}
