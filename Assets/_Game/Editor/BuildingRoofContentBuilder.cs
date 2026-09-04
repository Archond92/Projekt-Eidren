using Eidren.Data;
using Eidren.Presentation;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class BuildingRoofContentBuilder
{
	private const string DataFolder = "Assets/_Game/Data/Buildings";

	private const string ResourceParent = "Assets/_Game/Resources/Art";

	private const string Folder = "Assets/_Game/Resources/Art/Buildings";

	private const string TileAsset = "BLD_Roof_Tile";

	private const string MaterialAsset = "BLD_Roof";

	private const string TextureAsset = "BLD_Roof_Thatch";

	private const int TextureSize = 128;

	private static readonly Color Thatch = new Color(0.46f, 0.33f, 0.19f, 1f);

	[MenuItem("Eidren/Art/Build Room Roofs")]
	public static void BuildRoofs()
	{
		EnsureFolder();
		EnsureMaterial(EnsureTexture("BLD_Roof_Thatch", ThatchPixels()));
		BuildTile();
		int marked = MarkEdgePrefabs();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Verify();
		Debug.Log("Eidren: room roofs authored — 1 texture, 1 material and " + string.Format("1 prefab in {0}; {1} edge prefabs marked as ", "Assets/_Game/Resources/Art/Buildings", marked) + "occlusion fade targets.");
	}

	private static Color[] ThatchPixels()
	{
		Color[] pixels = new Color[16384];
		for (int y = 0; y < 128; y++)
		{
			float layer = ((float)y + 0.5f) / 128f * 7f;
			float withinLayer = layer - Mathf.Floor(layer);
			for (int x = 0; x < 128; x++)
			{
				float u = ((float)x + 0.5f) / 128f;
				float straw = 0.5f + 0.5f * Mathf.Sin(u * 41f + layer * 2.3f);
				float binding = ((withinLayer < 0.14f) ? 0.55f : 1f);
				float lift = Mathf.Lerp(0.82f, 1.12f, withinLayer);
				float shade = Mathf.Clamp01((0.78f + straw * 0.22f) * binding * lift);
				pixels[y * 128 + x] = new Color(shade, shade, shade, 1f);
			}
		}
		return pixels;
	}

	private static Texture2D EnsureTexture(string assetName, Color[] pixels)
	{
		string path = "Assets/_Game/Resources/Art/Buildings/" + assetName + ".asset";
		Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		bool num = texture == null;
		if (num)
		{
			texture = new Texture2D(128, 128, TextureFormat.RGBA32, mipChain: true)
			{
				name = assetName
			};
		}
		texture.wrapMode = TextureWrapMode.Repeat;
		texture.filterMode = FilterMode.Bilinear;
		texture.SetPixels(pixels);
		texture.Apply(updateMipmaps: true);
		if (num)
		{
			AssetDatabase.CreateAsset(texture, path);
		}
		EditorUtility.SetDirty(texture);
		return texture;
	}

	private static void EnsureMaterial(Texture2D texture)
	{
		Shader shader = Shader.Find("Universal Render Pipeline/Lit");
		if (shader == null)
		{
			throw new InvalidOperationException("URP Lit shader is required for room roofs.");
		}
		string path = "Assets/_Game/Resources/Art/Buildings/BLD_Roof.mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (material == null)
		{
			material = new Material(shader);
			AssetDatabase.CreateAsset(material, path);
		}
		material.shader = shader;
		material.SetTexture("_BaseMap", texture);
		material.SetColor("_BaseColor", Thatch);
		material.color = Thatch;
		material.SetFloat("_Smoothness", 0.08f);
		material.SetFloat("_Metallic", 0f);
		material.SetOverrideTag("RenderType", "Transparent");
		material.SetFloat("_Surface", 1f);
		material.SetFloat("_Blend", 0f);
		material.SetFloat("_AlphaClip", 0f);
		material.SetFloat("_ZWrite", 0f);
		material.SetFloat("_SrcBlend", 5f);
		material.SetFloat("_DstBlend", 10f);
		material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
		material.DisableKeyword("_ALPHATEST_ON");
		material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		material.renderQueue = 3000;
		EditorUtility.SetDirty(material);
	}

	private static void BuildTile()
	{
		GameObject root = new GameObject("BLD_Roof_Tile");
		try
		{
			GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Quad);
			piece.name = "Thatch";
			Collider[] components = piece.GetComponents<Collider>();
			for (int i = 0; i < components.Length; i++)
			{
				UnityEngine.Object.DestroyImmediate(components[i]);
			}
			piece.transform.SetParent(root.transform, worldPositionStays: false);
			piece.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
			piece.transform.localScale = Vector3.one;
			MeshRenderer component = piece.GetComponent<MeshRenderer>();
			component.sharedMaterial = RequireMaterial();
			component.shadowCastingMode = ShadowCastingMode.Off;
			component.receiveShadows = true;
			PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/Art/Buildings/BLD_Roof_Tile.prefab");
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static int MarkEdgePrefabs()
	{
		int marked = 0;
		foreach (GameObject item in EdgePrefabs())
		{
			string path = AssetDatabase.GetAssetPath(item);
			GameObject root = PrefabUtility.LoadPrefabContents(path);
			try
			{
				if (!(root.GetComponent<OcclusionFadeTarget>() != null))
				{
					root.AddComponent<OcclusionFadeTarget>();
					PrefabUtility.SaveAsPrefabAsset(root, path);
					marked++;
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}
		return marked;
	}

	private static IEnumerable<GameObject> EdgePrefabs()
	{
		string[] guids = AssetDatabase.FindAssets("t:BuildingCostDefinition", new string[1] { "Assets/_Game/Data/Buildings" });
		string[] array = guids;
		for (int i = 0; i < array.Length; i++)
		{
			BuildingCostDefinition plan = AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>(AssetDatabase.GUIDToAssetPath(array[i]));
			if (!(plan == null) && plan.PlacementKind == BuildingPlacementKind.Edge && plan.TryGetLevel(1, out var _, out var prefab))
			{
				yield return prefab;
			}
		}
	}

	private static Material RequireMaterial()
	{
		string path = "Assets/_Game/Resources/Art/Buildings/BLD_Roof.mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (!(material != null))
		{
			throw new InvalidOperationException("Roof material is missing: " + path);
		}
		return material;
	}

	private static void EnsureFolder()
	{
		if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources/Art"))
		{
			AssetDatabase.CreateFolder("Assets/_Game/Resources", "Art");
		}
		if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources/Art/Buildings"))
		{
			AssetDatabase.CreateFolder("Assets/_Game/Resources/Art", "Buildings");
		}
	}

	private static void Verify()
	{
		if (RequireMaterial().GetTexture("_BaseMap") == null)
		{
			throw new InvalidOperationException("'BLD_Roof' has no thatch texture on disk.");
		}
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/Art/Buildings/BLD_Roof_Tile.prefab");
		if (prefab == null || prefab.GetComponentInChildren<MeshRenderer>(includeInactive: true) == null)
		{
			throw new InvalidOperationException("'BLD_Roof_Tile' was not saved with a renderer.");
		}
		if (prefab.GetComponentInChildren<Collider>(includeInactive: true) != null)
		{
			throw new InvalidOperationException("'BLD_Roof_Tile' carries a collider; a roof must not change navigation or occupancy.");
		}
		foreach (GameObject edge in EdgePrefabs())
		{
			if (edge.GetComponent<OcclusionFadeTarget>() == null)
			{
				throw new InvalidOperationException("Edge prefab '" + edge.name + "' has no OcclusionFadeTarget on disk; it would stay opaque in front of the player.");
			}
		}
	}
}
}
