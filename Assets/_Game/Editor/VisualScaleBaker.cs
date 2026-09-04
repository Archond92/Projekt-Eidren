using Eidren.Data;
using Eidren.Presentation;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class VisualScaleBaker
{
	private const float GroundPlaneLift = 0.02f;

	public static VisualScaleBakeResult ApplyToPrefab(string prefabPath, VisualScaleEntry entry)
	{
		GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
		if (contents == null)
		{
			throw new InvalidOperationException("Prefab fehlt: " + prefabPath);
		}
		try
		{
			VisualScaleBakeResult result = Apply(contents, entry);
			PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
			return result;
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(contents);
		}
	}

	public static VisualScaleBakeResult ApplyToPrimitivePrefab(string prefabPath, VisualScaleEntry entry)
	{
		GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
		if (contents == null)
		{
			throw new InvalidOperationException("Prefab fehlt: " + prefabPath);
		}
		try
		{
			Renderer[] renderers = contents.GetComponentsInChildren<Renderer>(includeInactive: true);
			if (renderers.Length == 0)
			{
				throw new InvalidOperationException("'" + contents.name + "' hat keine Renderer.");
			}
			contents.transform.localScale = Vector3.one;
			Bounds bounds = renderers[0].bounds;
			for (int i = 1; i < renderers.Length; i++)
			{
				bounds.Encapsulate(renderers[i].bounds);
			}
			float current = bounds.size.y;
			if (current <= 0.0001f)
			{
				throw new InvalidOperationException("'" + contents.name + "' hat keine messbare Hoehe.");
			}
			float byHeight = entry.Height / current;
			float currentWidth = Mathf.Max(bounds.size.x, bounds.size.z);
			float factor = ((currentWidth > 0.0001f) ? Mathf.Min(byHeight, entry.WidthBudget / currentWidth) : byHeight);
			contents.transform.localScale = Vector3.one * factor;
			PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
			return new VisualScaleBakeResult(entry.Id, factor, current * factor, currentWidth * factor, factor < byHeight - 0.0001f, 0f);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(contents);
		}
	}

	public static VisualScaleBakeResult Apply(GameObject root, VisualScaleEntry entry)
	{
		if (root == null)
		{
			throw new ArgumentNullException("root");
		}
		if (entry == null)
		{
			throw new ArgumentNullException("entry");
		}
		SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>(includeInactive: true);
		if (renderer == null || renderer.sprite == null)
		{
			throw new InvalidOperationException("'" + root.name + "' hat kein Sprite. Die Groesse einer Primitivgeometrie laesst sich nicht aus einem Bild ableiten (M10.7).");
		}
		SpriteSilhouette silhouette = SpriteSilhouetteReader.Measure(renderer.sprite);
		if (silhouette.IsEmpty)
		{
			throw new InvalidOperationException("'" + root.name + "': die Silhouette von '" + renderer.sprite.name + "' ist leer. Vollstaendig transparentes Bild?");
		}
		Transform visual = renderer.transform;
		float scale = (entry.IsGroundPlane ? GroundPlaneScale(entry, silhouette) : BillboardScale(entry, silhouette));
		visual.localScale = Vector3.one * scale;
		ApplyPlacement(visual, entry, silhouette, scale);
		ResizeExistingCollider(root, entry);
		bool fitted = !entry.IsGroundPlane && scale < silhouette.ScaleForHeight(entry.Height) - 0.0001f;
		return new VisualScaleBakeResult(entry.Id, scale, silhouette.WorldHeight * scale, silhouette.WorldWidth * scale, fitted, visual.localPosition.y);
	}

	private static float BillboardScale(VisualScaleEntry entry, SpriteSilhouette silhouette)
	{
		if (!entry.ArtworkMissing)
		{
			return silhouette.ScaleForHeight(entry.Height);
		}
		return silhouette.ScaleToFit(entry.Height, entry.WidthBudget);
	}

	private static float GroundPlaneScale(VisualScaleEntry entry, SpriteSilhouette silhouette)
	{
		float largest = Mathf.Max(silhouette.WorldWidth, silhouette.WorldHeight);
		if (largest <= 0f)
		{
			return 1f;
		}
		return entry.WidthBudget / largest;
	}

	private static void ApplyPlacement(Transform visual, VisualScaleEntry entry, SpriteSilhouette silhouette, float scale)
	{
		if (entry.IsGroundPlane)
		{
			RuntimeSpriteVisual billboard = visual.GetComponent<RuntimeSpriteVisual>();
			if (billboard != null)
			{
				UnityEngine.Object.DestroyImmediate(billboard, allowDestroyingAssets: true);
			}
			visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
			visual.localPosition = new Vector3(0f, 0.02f, 0f);
		}
		else
		{
			float verticalProjection = Mathf.Cos(0.9075712f);
			visual.localRotation = Quaternion.Euler(52f, 45f, 0f);
			visual.localPosition = new Vector3(0f, (0f - silhouette.BottomOffset) * scale * verticalProjection, 0f);
		}
	}

	private static void ResizeExistingCollider(GameObject root, VisualScaleEntry entry)
	{
		if (!(entry.ColliderSize.sqrMagnitude <= 0f))
		{
			BoxCollider box = root.GetComponent<BoxCollider>();
			if (!(box == null))
			{
				box.center = new Vector3(0f, (box.size = entry.ColliderSize).y * 0.5f, 0f);
			}
		}
	}
}

public readonly struct VisualScaleBakeResult
{
	public string Id { get; }

	public float Scale { get; }

	public float Height { get; }

	public float Width { get; }

	public bool Fitted { get; }

	public float Lift { get; }

	public VisualScaleBakeResult(string id, float scale, float height, float width, bool fitted, float lift)
	{
		Id = id;
		Scale = scale;
		Height = height;
		Width = width;
		Fitted = fitted;
		Lift = lift;
	}

	public override string ToString()
	{
		string suffix = (Fitted ? " (eingepasst)" : string.Empty);
		return $"{Id}: h={Height:0.00} b={Width:0.00} " + $"Hub={Lift:0.00} Faktor {Scale:0.000}{suffix}";
	}
}
}
