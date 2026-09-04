using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Zones/Area Art")]
	public sealed class ZoneAreaArtDefinition : ScriptableObject
	{
		public const float GroundTileWorldSize = 6f;

		[SerializeField]
		private string id;

		[SerializeField]
		private Texture2D baseGround;

		[SerializeField]
		private AreaArtGroundLayer[] groundBlends = Array.Empty<AreaArtGroundLayer>();

		[SerializeField]
		private AreaArtResourceVariant[] resourceVariants = Array.Empty<AreaArtResourceVariant>();

		[SerializeField]
		private AreaArtDecoration[] decorations = Array.Empty<AreaArtDecoration>();

		[SerializeField]
		private UnityEngine.Object volumeProfile;

		[SerializeField]
		private Color backgroundColor = new Color(0.12f, 0.16f, 0.14f, 1f);

		[SerializeField]
		private Color lightColor = Color.white;

		public string Id => id ?? string.Empty;

		public Texture2D BaseGround => baseGround;

		public IReadOnlyList<AreaArtGroundLayer> GroundBlends => groundBlends ?? Array.Empty<AreaArtGroundLayer>();

		public IReadOnlyList<AreaArtResourceVariant> ResourceVariants => resourceVariants ?? Array.Empty<AreaArtResourceVariant>();

		public IReadOnlyList<AreaArtDecoration> Decorations => decorations ?? Array.Empty<AreaArtDecoration>();

		public UnityEngine.Object VolumeProfile => volumeProfile;

		public Color BackgroundColor => backgroundColor;

		public Color LightColor => lightColor;

		public bool TryGetResourceVariant(string resourceNodeId, out AreaArtResourceVariant variant)
		{
			foreach (AreaArtResourceVariant resourceVariant in ResourceVariants)
			{
				if (resourceVariant.ResourceNodeId == resourceNodeId)
				{
					variant = resourceVariant;
					return true;
				}
			}
			variant = default(AreaArtResourceVariant);
			return false;
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (string.IsNullOrWhiteSpace(Id))
			{
				list.Add("Area art '" + base.name + "' has no stable ID.");
			}
			if (baseGround == null)
			{
				list.Add("Area art '" + base.name + "' has no base ground.");
			}
			if (GroundBlends.Count < 1 || GroundBlends.Count > 2)
			{
				list.Add("Area art '" + base.name + "' needs one or two ground blends.");
			}
			HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
			HashSet<UnityEngine.Object> hashSet = new HashSet<UnityEngine.Object>();
			if (baseGround != null)
			{
				hashSet.Add(baseGround);
			}
			foreach (AreaArtGroundLayer groundBlend in GroundBlends)
			{
				ValidateId(groundBlend.Id, "ground layer", ids, list);
				ValidateAsset(groundBlend.Texture, "ground layer", hashSet, list);
			}
			foreach (AreaArtResourceVariant resourceVariant in ResourceVariants)
			{
				ValidateId(resourceVariant.ResourceNodeId, "resource variant", ids, list);
				if (resourceVariant.ActiveVisualPrefab == null || resourceVariant.ExhaustedVisualPrefab == null)
				{
					list.Add("Variant '" + resourceVariant.ResourceNodeId + "' needs active and exhausted visuals.");
				}
				if (string.IsNullOrWhiteSpace(resourceVariant.RecognitionMarker))
				{
					list.Add("Variant '" + resourceVariant.ResourceNodeId + "' has no recognition marker note.");
				}
			}
			foreach (AreaArtDecoration decoration in Decorations)
			{
				ValidateId(decoration.Id, "decoration", ids, list);
				if (decoration.Prefab == null)
				{
					list.Add("Decoration '" + decoration.Id + "' has no prefab.");
				}
			}
			if (volumeProfile == null)
			{
				list.Add("Area art '" + base.name + "' has no volume profile.");
			}
			return list.ToArray();
		}

		private static void ValidateId(string value, string role, HashSet<string> ids, List<string> errors)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				errors.Add("An " + role + " has no stable ID.");
			}
			else if (!ids.Add(value))
			{
				errors.Add("Stable area-art ID '" + value + "' is duplicated.");
			}
		}

		private static void ValidateAsset(UnityEngine.Object asset, string role, HashSet<UnityEngine.Object> assets, List<string> errors)
		{
			if (asset == null)
			{
				errors.Add("A " + role + " has no asset.");
			}
			else if (!assets.Add(asset))
			{
				errors.Add("Asset '" + asset.name + "' is used in two roles.");
			}
		}
	}

	[Serializable]
	public struct AreaArtDecoration
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private GameObject prefab;

		[SerializeField]
		[Min(0f)]
		private float density;

		public string Id => id ?? string.Empty;

		public GameObject Prefab => prefab;

		public float Density => Mathf.Max(0f, density);
	}

	[Serializable]
	public struct AreaArtGroundLayer
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private Texture2D texture;

		[SerializeField]
		[Range(0.05f, 1f)]
		private float coverage;

		public string Id => id ?? string.Empty;

		public Texture2D Texture => texture;

		public float Coverage => Mathf.Clamp01(coverage);
	}

	[Serializable]
	public struct AreaArtResourceVariant
	{
		[SerializeField]
		private string resourceNodeId;

		[SerializeField]
		private GameObject activeVisualPrefab;

		[SerializeField]
		private GameObject exhaustedVisualPrefab;

		[SerializeField]
		private AreaArtVariantMedium medium;

		[SerializeField]
		private string recognitionMarker;

		public string ResourceNodeId => resourceNodeId ?? string.Empty;

		public GameObject ActiveVisualPrefab => activeVisualPrefab;

		public GameObject ExhaustedVisualPrefab => exhaustedVisualPrefab;

		public AreaArtVariantMedium Medium => medium;

		public string RecognitionMarker => recognitionMarker ?? string.Empty;
	}
}
