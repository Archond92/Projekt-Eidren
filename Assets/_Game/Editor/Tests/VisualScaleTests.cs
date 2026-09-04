using Eidren.Data;
using Eidren.Editor;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class VisualScaleTests
{
	private readonly struct VisualMeasurement
	{
		public float Width { get; }

		public float Height { get; }

		public float Depth { get; }

		public SpriteSilhouette Silhouette { get; }

		public float SpriteScale { get; }

		public bool IsSprite { get; }

		public float HorizontalExtent => Mathf.Max(Width, Depth);

		public float GroundExtent => HorizontalExtent;

		private VisualMeasurement(float width, float height, float depth, SpriteSilhouette silhouette, float spriteScale, bool isSprite)
		{
			Width = width;
			Height = height;
			Depth = depth;
			Silhouette = silhouette;
			SpriteScale = spriteScale;
			IsSprite = isSprite;
		}

		public float FittedHeight(float height, float widthBudget)
		{
			return Silhouette.WorldHeight * Silhouette.ScaleToFit(height, widthBudget);
		}

		public static VisualMeasurement FromSprite(SpriteSilhouette silhouette, float scale)
		{
			return new VisualMeasurement(silhouette.WorldWidth * scale, silhouette.WorldHeight * scale, 0f, silhouette, scale, isSprite: true);
		}

		public static VisualMeasurement FromAuthoredSprite(float height)
		{
			return new VisualMeasurement(0f, height, 0f, default(SpriteSilhouette), 1f, isSprite: true);
		}

		public static VisualMeasurement FromBounds(Bounds bounds)
		{
			return new VisualMeasurement(bounds.size.x, bounds.size.y, bounds.size.z, default(SpriteSilhouette), 0f, isSprite: false);
		}
	}

	private const float HeightTolerance = 0.01f;

	private const int MaximumArtworkDebt = 0;

	private static VisualScaleTable Table()
	{
		SpriteSilhouetteReader.ClearCaches();
		return VisualScaleTableBuilder.Load();
	}

	[Test]
	public void Table_ValidatesItself()
	{
		Assert.That<string[]>(Table().GetValidationErrors(), (IResolveConstraint)(object)Is.Empty, "§15: Die Groessentabelle validiert sich selbst.", Array.Empty<object>());
	}

	[Test]
	public void EveryBakedPrefab_MatchesItsTableHeight()
	{
		VisualScaleTable table = Table();
		List<string> offenders = new List<string>();
		foreach (KeyValuePair<string, string> pair in VisualScaleAssetMap.BakedPrefabs)
		{
			if (TryMeasurePrefab(pair.Value, out var visual))
			{
				VisualScaleEntry entry = table.Require(pair.Key);
				float measured = (entry.IsGroundPlane ? visual.GroundExtent : visual.Height);
				float expected = (entry.IsGroundPlane ? entry.WidthBudget : ((entry.ArtworkMissing && visual.IsSprite) ? visual.FittedHeight(entry.Height, entry.WidthBudget) : entry.Height));
				if (Mathf.Abs(measured - expected) > expected * 0.01f)
				{
					offenders.Add($"{pair.Key}: gemessen {measured:0.000}, " + $"erwartet {expected:0.000}");
				}
			}
		}
		Assert.That<List<string>>(offenders, (IResolveConstraint)(object)Is.Empty, "§25: Die Hoehe steht im Prefab oder sie steht nirgends:\n" + string.Join("\n", offenders), Array.Empty<object>());
	}

	[Test]
	public void EveryBakedPrefab_StaysWithinItsWidthBudget()
	{
		VisualScaleTable table = Table();
		List<string> offenders = new List<string>();
		foreach (KeyValuePair<string, string> pair in VisualScaleAssetMap.BakedPrefabs)
		{
			if (pair.Key.StartsWith("visual.building.") && TryMeasurePrefab(pair.Value, out var visual))
			{
				VisualScaleEntry entry = table.Require(pair.Key);
				float measured = visual.HorizontalExtent;
				if (measured > entry.WidthBudget * 1.01f)
				{
					offenders.Add($"{pair.Key}: {measured:0.000} breit, Budget " + $"{entry.WidthBudget:0.000}");
				}
			}
		}
		Assert.That<List<string>>(offenders, (IResolveConstraint)(object)Is.Empty, "M10.7: Kein Objekt ist breiter als sein Budget, und kein Gebaeude ragt ueber sein Baufeld:\n" + string.Join("\n", offenders), Array.Empty<object>());
	}

	[Test]
	public void TableAndPrefabs_HaveNoOrphansInEitherDirection()
	{
		VisualScaleTable table = Table();
		HashSet<string> known = new HashSet<string>(table.Entries.Select((VisualScaleEntry visualScaleEntry) => visualScaleEntry.Id));
		List<string> orphans = new List<string>();
		foreach (string id in VisualScaleAssetMap.BakedPrefabs.Keys)
		{
			if (!known.Contains(id))
			{
				orphans.Add("Prefab-Zuordnung ohne Tabellenzeile: " + id);
			}
		}
		foreach (string id2 in VisualScaleAssetMap.RuntimeArt.Keys)
		{
			if (!known.Contains(id2))
			{
				orphans.Add("Laufzeitbild ohne Tabellenzeile: " + id2);
			}
		}
		foreach (VisualScaleEntry entry in table.Entries)
		{
			if (!VisualScaleAssetMap.BakedPrefabs.ContainsKey(entry.Id) && !VisualScaleAssetMap.RuntimeArt.ContainsKey(entry.Id) && !VisualScaleAssetMap.WithoutOwnArtwork.Contains(entry.Id))
			{
				orphans.Add("Tabellenzeile ohne Datei und ohne Vermerk: " + entry.Id);
			}
		}
		Assert.That<List<string>>(orphans, (IResolveConstraint)(object)Is.Empty, "§6: Jede Zeile gehoert zu einer Datei und jede Datei zu einer Zeile:\n" + string.Join("\n", orphans), Array.Empty<object>());
	}

	[Test]
	public void ArtworkDebt_OnlyEverShrinks()
	{
		int missing = Table().Entries.Count((VisualScaleEntry entry) => entry.ArtworkMissing);
		Assert.That<int>(missing, (IResolveConstraint)(object)Is.LessThanOrEqualTo((object)0), "M10.7: Markierungen „Bild fehlt\" sind Schulden. Erwartet " + $"hoechstens {0}, gefunden {missing}. " + "Wer eine hinzufuegt, benennt sie.", Array.Empty<object>());
	}

	[Test]
	public void FiberPlant_RemainsReadableBelowHalfAPlayerHeight()
	{
		AssertPlayerHeightRatio("visual.fiber_plant.active", 0.44f, 0.46f);
	}

	[Test]
	public void Tree_ReachesTwoAndAHalfPlayerHeights()
	{
		AssertPlayerHeightRatio("visual.tree.active", 2.5f);
	}

	[Test]
	public void Wall_OvertopsThePlayer()
	{
		AssertPlayerHeightRatio("visual.building.wall", 1f);
	}

	private void AssertPlayerHeightRatio(string id, float minimum = 0f, float maximum = float.MaxValue)
	{
		float playerHeight = Table().Require("visual.player").Height;
		Assert.That<float>(playerHeight, (IResolveConstraint)(object)Is.GreaterThan((object)0f));
		Assert.That<bool>(TryMeasurePrefab(VisualScaleAssetMap.BakedPrefabs[id], out var visual), (IResolveConstraint)(object)Is.True, "'" + id + "' ist nicht messbar.", Array.Empty<object>());
		float ratio = visual.Height / playerHeight;
		Assert.That<float>(ratio, (IResolveConstraint)(object)((Constraint)Is.GreaterThanOrEqualTo((object)minimum)).And.LessThanOrEqualTo((object)maximum), $"'{id}': {ratio:0.00} Spielerhoehen.", Array.Empty<object>());
	}

	private static bool TryMeasurePrefab(string prefabPath, out VisualMeasurement measurement)
	{
		measurement = default(VisualMeasurement);
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		SpriteActorPresentation actor = ((prefab == null) ? null : prefab.GetComponentInChildren<SpriteActorPresentation>(includeInactive: true));
		if (actor != null)
		{
			measurement = VisualMeasurement.FromAuthoredSprite(actor.WorldHeight);
			return true;
		}
		if (prefab != null && FindGeometryRoot(prefab.transform) != prefab.transform)
		{
			return TryMeasureGeometry(prefab, out measurement);
		}
		SpriteRenderer renderer = ((prefab == null) ? null : prefab.GetComponentInChildren<SpriteRenderer>(includeInactive: true));
		if (renderer == null || renderer.sprite == null)
		{
			return TryMeasureGeometry(prefab, out measurement);
		}
		SpriteSilhouette silhouette = SpriteSilhouetteReader.Measure(renderer.sprite);
		if (silhouette.IsEmpty)
		{
			return false;
		}
		measurement = VisualMeasurement.FromSprite(silhouette, renderer.transform.localScale.y);
		return true;
	}

	private static bool TryMeasureGeometry(GameObject prefab, out VisualMeasurement measurement)
	{
		measurement = default(VisualMeasurement);
		if (prefab == null)
		{
			return false;
		}
		GameObject instance = UnityEngine.Object.Instantiate(prefab);
		try
		{
			Renderer[] renderers = FindGeometryRoot(instance.transform).GetComponentsInChildren<Renderer>(includeInactive: true);
			if (renderers.Length == 0)
			{
				return false;
			}
			Bounds bounds = renderers[0].bounds;
			for (int index = 1; index < renderers.Length; index++)
			{
				bounds.Encapsulate(renderers[index].bounds);
			}
			measurement = VisualMeasurement.FromBounds(bounds);
			return true;
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(instance);
		}
	}

	private static Transform FindGeometryRoot(Transform root)
	{
		Transform direct = root.Find("Geometry_A14") ?? root.Find("Geometry_MidPoly");
		if (direct != null)
		{
			return direct;
		}
		foreach (Transform child in root)
		{
			Transform nested = FindGeometryRoot(child);
			if (nested != child)
			{
				return nested;
			}
			if (child.name == "Geometry_A14" || child.name == "Geometry_MidPoly")
			{
				return child;
			}
		}
		return root;
	}
}
}
