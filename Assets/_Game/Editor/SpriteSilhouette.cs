using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public readonly struct SpriteSilhouette
{
	public const float AlphaThreshold = 0.02f;

	public float WorldWidth { get; }

	public float WorldHeight { get; }

	public float WidthFraction { get; }

	public float HeightFraction { get; }

	public float BottomOffset { get; }

	public float CenterOffsetX { get; }

	public bool IsEmpty
	{
		get
		{
			if (!(WorldHeight <= 0f))
			{
				return WorldWidth <= 0f;
			}
			return true;
		}
	}

	public float AspectRatio
	{
		get
		{
			if (!(WorldHeight <= 0f))
			{
				return WorldWidth / WorldHeight;
			}
			return 0f;
		}
	}

	public SpriteSilhouette(float worldWidth, float worldHeight, float widthFraction, float heightFraction, float bottomOffset, float centerOffsetX)
	{
		WorldWidth = worldWidth;
		WorldHeight = worldHeight;
		WidthFraction = widthFraction;
		HeightFraction = heightFraction;
		BottomOffset = bottomOffset;
		CenterOffsetX = centerOffsetX;
	}

	public float ScaleForHeight(float targetHeight)
	{
		if (WorldHeight <= 0f)
		{
			return 1f;
		}
		return targetHeight / WorldHeight;
	}

	public float ScaleToFit(float targetHeight, float widthBudget)
	{
		float byHeight = ScaleForHeight(targetHeight);
		if (WorldWidth <= 0f || widthBudget <= 0f)
		{
			return byHeight;
		}
		return Mathf.Min(byHeight, widthBudget / WorldWidth);
	}
}

public static class SpriteSilhouetteReader
{
	public readonly struct TextureFill
	{
		public float HeightFraction { get; }

		public float WidthFraction { get; }

		public float AspectRatio { get; }

		public float BottomFraction { get; }

		public bool IsEmpty => HeightFraction <= 0f;

		public TextureFill(float heightFraction, float widthFraction, float bottomFraction, float aspectRatio)
		{
			HeightFraction = heightFraction;
			WidthFraction = widthFraction;
			BottomFraction = bottomFraction;
			AspectRatio = aspectRatio;
		}

		public float FrameHeightFor(float silhouetteHeight)
		{
			if (HeightFraction <= 0f)
			{
				return silhouetteHeight;
			}
			return silhouetteHeight / HeightFraction;
		}
	}

	private static readonly Dictionary<string, Color32[]> PixelCache = new Dictionary<string, Color32[]>(StringComparer.Ordinal);

	private static readonly Dictionary<string, Vector2Int> SizeCache = new Dictionary<string, Vector2Int>(StringComparer.Ordinal);

	public static void ClearCaches()
	{
		PixelCache.Clear();
		SizeCache.Clear();
	}

	public static TextureFill MeasureTextureFile(string assetPath)
	{
		if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
		{
			return default(TextureFill);
		}
		if (!TryLoadSource(assetPath, out var pixels, out var size))
		{
			return default(TextureFill);
		}
		if (!TryFindOpaqueBounds(pixels, size, new RectInt(0, 0, size.x, size.y), out var opaque))
		{
			return default(TextureFill);
		}
		return new TextureFill((float)opaque.height / (float)size.y, (float)opaque.width / (float)size.x, (float)opaque.yMin / (float)size.y, (opaque.height <= 0) ? 0f : ((float)opaque.width / (float)opaque.height));
	}

	public static SpriteSilhouette Measure(Sprite sprite)
	{
		if (sprite == null)
		{
			return default(SpriteSilhouette);
		}
		string path = AssetDatabase.GetAssetPath(sprite);
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return default(SpriteSilhouette);
		}
		if (!TryLoadSource(path, out var pixels, out var size))
		{
			return default(SpriteSilhouette);
		}
		Texture2D imported = sprite.texture;
		if (imported == null || imported.width <= 0 || imported.height <= 0)
		{
			return default(SpriteSilhouette);
		}
		Rect rect = sprite.textureRect;
		int x0 = Mathf.Clamp(Mathf.RoundToInt(rect.x / (float)imported.width * (float)size.x), 0, size.x - 1);
		int y0 = Mathf.Clamp(Mathf.RoundToInt(rect.y / (float)imported.height * (float)size.y), 0, size.y - 1);
		int width = Mathf.Clamp(Mathf.RoundToInt(rect.width / (float)imported.width * (float)size.x), 1, size.x - x0);
		int height = Mathf.Clamp(Mathf.RoundToInt(rect.height / (float)imported.height * (float)size.y), 1, size.y - y0);
		if (!TryFindOpaqueBounds(pixels, size, new RectInt(x0, y0, width, height), out var opaque))
		{
			return default(SpriteSilhouette);
		}
		float widthFraction = (float)opaque.width / (float)width;
		float heightFraction = (float)opaque.height / (float)height;
		Vector2 spriteWorldSize = sprite.bounds.size;
		float bottomFraction = (float)(opaque.yMin - y0) / (float)height;
		float centerFractionX = ((float)opaque.xMin + (float)opaque.width * 0.5f - (float)x0) / (float)width;
		Bounds bounds = sprite.bounds;
		return new SpriteSilhouette(spriteWorldSize.x * widthFraction, spriteWorldSize.y * heightFraction, widthFraction, heightFraction, bounds.min.y + bottomFraction * spriteWorldSize.y, bounds.min.x + centerFractionX * spriteWorldSize.x);
	}

	private static bool TryLoadSource(string path, out Color32[] pixels, out Vector2Int size)
	{
		if (PixelCache.TryGetValue(path, out pixels) && SizeCache.TryGetValue(path, out size))
		{
			return true;
		}
		size = default(Vector2Int);
		Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		try
		{
			if (!decoded.LoadImage(File.ReadAllBytes(path), markNonReadable: false))
			{
				pixels = null;
				return false;
			}
			pixels = decoded.GetPixels32();
			size = new Vector2Int(decoded.width, decoded.height);
			PixelCache[path] = pixels;
			SizeCache[path] = size;
			return true;
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(decoded);
		}
	}

	private static bool TryFindOpaqueBounds(Color32[] pixels, Vector2Int textureSize, RectInt region, out RectInt bounds)
	{
		byte threshold = (byte)Mathf.CeilToInt(5.1f);
		int minX = int.MaxValue;
		int minY = int.MaxValue;
		int maxX = int.MinValue;
		int maxY = int.MinValue;
		for (int y = region.yMin; y < region.yMax; y++)
		{
			int row = y * textureSize.x;
			for (int x = region.xMin; x < region.xMax; x++)
			{
				if (pixels[row + x].a >= threshold)
				{
					if (x < minX)
					{
						minX = x;
					}
					if (x > maxX)
					{
						maxX = x;
					}
					if (y < minY)
					{
						minY = y;
					}
					if (y > maxY)
					{
						maxY = y;
					}
				}
			}
		}
		if (minX > maxX || minY > maxY)
		{
			bounds = default(RectInt);
			return false;
		}
		bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
		return true;
	}
}
}
