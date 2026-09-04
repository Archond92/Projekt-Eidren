using Eidren.Data;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class FixCollectionArtBuilder
{
	private static readonly (string Item, string Icon)[] ArmorIcons = new(string, string)[4]
	{
		("Assets/_Game/Data/Items/WandererHood.asset", "Assets/_Game/Art/Items/ITEM_WandererHood.png"),
		("Assets/_Game/Data/Items/WandererCoat.asset", "Assets/_Game/Art/Items/ITEM_WandererCoat.png"),
		("Assets/_Game/Data/Items/WandererBracers.asset", "Assets/_Game/Art/Items/ITEM_WandererBracers.png"),
		("Assets/_Game/Data/Items/WandererLegs.asset", "Assets/_Game/Art/Items/ITEM_WandererLegs.png")
	};

	[MenuItem("Eidren/V0.2/Fixes/Import Interaction and Armor Art")]
	public static void ImportAndWire()
	{
		ImportSprite("Assets/_Game/Resources/Art/UI/ui_interaction_hand.png", 256);
		(string, string)[] armorIcons = ArmorIcons;
		for (int i = 0; i < armorIcons.Length; i++)
		{
			var (itemPath, iconPath) = armorIcons[i];
			ImportSprite(iconPath, 512);
			ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath);
			Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
			if (itemDefinition == null || icon == null)
			{
				throw new FileNotFoundException(itemPath + " / " + iconPath);
			}
			SerializedObject serializedObject = new SerializedObject(itemDefinition);
			serializedObject.FindProperty("icon").objectReferenceValue = icon;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(itemDefinition);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private static void ImportSprite(string path, int maximumSize)
	{
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
		TextureImporter obj = AssetImporter.GetAtPath(path) as TextureImporter;
		if (obj == null)
		{
			throw new InvalidDataException("No texture importer: " + path);
		}
		obj.textureType = TextureImporterType.Sprite;
		obj.spriteImportMode = SpriteImportMode.Single;
		obj.alphaIsTransparency = true;
		obj.mipmapEnabled = false;
		obj.textureCompression = TextureImporterCompression.Uncompressed;
		obj.maxTextureSize = maximumSize;
		obj.wrapMode = TextureWrapMode.Clamp;
		obj.SaveAndReimport();
	}
}
}
