using System.IO;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class WindowsReleaseTexturePolicy
{
	private const string ActorTextureRoot = "Assets/_Game/Resources/Art/Actors";

	private const string StandalonePlatform = "Standalone";

	private const int ActorMaxSize = 4096;

	[MenuItem("Eidren/Release/Apply Windows Texture Policy")]
	public static void ApplyFromMenu()
	{
		int changed = Apply();
		Debug.Log($"Eidren release texture policy applied: {changed} " + "actor textures updated.");
	}

	public static int Apply()
	{
		string[] array = AssetDatabase.FindAssets("t:Texture2D", new string[1] { "Assets/_Game/Resources/Art/Actors" });
		int changed = 0;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string path = AssetDatabase.GUIDToAssetPath(array2[i]);
			if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
			{
				TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
				if (!(importer == null) && Apply(importer, path))
				{
					importer.SaveAndReimport();
					changed++;
				}
			}
		}
		AssetDatabase.SaveAssets();
		return changed;
	}

	private static bool Apply(TextureImporter importer, string path)
	{
		TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Standalone");
		TextureImporterFormat format = FormatFor(path);
		bool crunch = format == TextureImporterFormat.DXT1Crunched || format == TextureImporterFormat.DXT5Crunched;
		if (!importer.mipmapEnabled && settings.overridden && settings.maxTextureSize == 4096 && settings.format == format && settings.textureCompression == TextureImporterCompression.CompressedHQ && settings.compressionQuality == 80 && settings.crunchedCompression == crunch)
		{
			return false;
		}
		importer.mipmapEnabled = false;
		settings.overridden = true;
		settings.maxTextureSize = 4096;
		settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
		settings.format = format;
		settings.textureCompression = TextureImporterCompression.CompressedHQ;
		settings.compressionQuality = 80;
		settings.crunchedCompression = crunch;
		importer.SetPlatformTextureSettings(settings);
		return true;
	}

	private static TextureImporterFormat FormatFor(string path)
	{
		string stem = Path.GetFileNameWithoutExtension(path);
		if (stem.EndsWith("_normal", StringComparison.OrdinalIgnoreCase))
		{
			return TextureImporterFormat.BC5;
		}
		if (stem.EndsWith("_emission", StringComparison.OrdinalIgnoreCase))
		{
			return TextureImporterFormat.DXT1Crunched;
		}
		return TextureImporterFormat.DXT5Crunched;
	}
}
}
