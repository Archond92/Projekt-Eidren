using System.IO;
using System.Security.Cryptography;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
internal static class ArtAssetImportUtility
{
	private const string SourceHashPrefix = "Eidren.SourceSha256=";

	internal static Sprite EnsureMirroredSprite(string sourcePath, string destinationPath, int maximumSize)
	{
		if (!File.Exists(sourcePath))
		{
			throw new FileNotFoundException("Art source is missing: " + sourcePath);
		}
		string sourceHash = Hash(sourcePath);
		if (!File.Exists(destinationPath))
		{
			if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
			{
				throw new IOException("Could not create art asset '" + destinationPath + "'.");
			}
		}
		else
		{
			ReplaceGeneratedCopyWhenSafe(sourcePath, destinationPath, sourceHash);
		}
		AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceSynchronousImport);
		TextureImporter obj = AssetImporter.GetAtPath(destinationPath) as TextureImporter;
		if (obj == null)
		{
			throw new InvalidOperationException("Texture has no importer: " + destinationPath);
		}
		obj.textureType = TextureImporterType.Sprite;
		obj.spriteImportMode = SpriteImportMode.Single;
		obj.spritePixelsPerUnit = 100f;
		obj.mipmapEnabled = false;
		obj.alphaIsTransparency = true;
		obj.alphaSource = TextureImporterAlphaSource.FromInput;
		obj.sRGBTexture = true;
		obj.wrapMode = TextureWrapMode.Clamp;
		obj.filterMode = FilterMode.Bilinear;
		obj.textureCompression = TextureImporterCompression.Uncompressed;
		obj.maxTextureSize = maximumSize;
		obj.userData = "Eidren.SourceSha256=" + sourceHash;
		obj.SaveAndReimport();
		Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(destinationPath);
		if (sprite == null)
		{
			throw new InvalidOperationException("Texture could not be imported as Sprite: " + destinationPath);
		}
		return sprite;
	}

	internal static void ConfigureSprite(string path, int maximumSize)
	{
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
		TextureImporter obj = AssetImporter.GetAtPath(path) as TextureImporter;
		if (obj == null)
		{
			throw new InvalidOperationException("Texture has no importer: " + path);
		}
		obj.textureType = TextureImporterType.Sprite;
		obj.spriteImportMode = SpriteImportMode.Single;
		obj.spritePixelsPerUnit = 100f;
		obj.mipmapEnabled = false;
		obj.alphaIsTransparency = true;
		obj.alphaSource = TextureImporterAlphaSource.FromInput;
		obj.sRGBTexture = true;
		obj.wrapMode = TextureWrapMode.Clamp;
		obj.filterMode = FilterMode.Bilinear;
		obj.textureCompression = TextureImporterCompression.Uncompressed;
		obj.maxTextureSize = maximumSize;
		obj.SaveAndReimport();
	}

	private static void ReplaceGeneratedCopyWhenSafe(string sourcePath, string destinationPath, string sourceHash)
	{
		string destinationHash = Hash(destinationPath);
		if (!(destinationHash == sourceHash))
		{
			AssetImporter importer = AssetImporter.GetAtPath(destinationPath);
			string recordedHash = ((importer != null && importer.userData.StartsWith("Eidren.SourceSha256=", StringComparison.Ordinal)) ? importer.userData.Substring("Eidren.SourceSha256=".Length) : string.Empty);
			if (!string.IsNullOrEmpty(recordedHash) && !string.Equals(recordedHash, destinationHash, StringComparison.Ordinal))
			{
				Debug.LogWarning("Eidren: preserved manually changed art asset '" + destinationPath + "'.");
			}
			else
			{
				File.Copy(sourcePath, destinationPath, overwrite: true);
			}
		}
	}

	private static string Hash(string path)
	{
		using SHA256 sha = SHA256.Create();
		using FileStream stream = File.OpenRead(path);
		return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
	}
}
}
