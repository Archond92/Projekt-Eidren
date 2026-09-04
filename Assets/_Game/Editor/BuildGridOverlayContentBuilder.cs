using Eidren.Presentation;
using System;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class BuildGridOverlayContentBuilder
{
	private readonly struct Plate
	{
		public string Asset { get; }

		public string Piece { get; }

		public Vector2 Size { get; }

		public string Material { get; }

		public Plate(string asset, string piece, Vector2 size, string material)
		{
			Asset = asset;
			Piece = piece;
			Size = size;
			Material = material;
		}
	}

	private const string ResourceParent = "Assets/_Game/Resources/Art";

	private const string Folder = "Assets/_Game/Resources/Art/BuildGrid";

	private const int TextureSize = 64;

	private static readonly Plate[] Plates = new Plate[6]
	{
		new Plate("BLD_Grid_Area", "Plate", new Vector2(1f, 1f), "BLD_Grid_Area"),
		new Plate("BLD_Grid_Zone", "Plate", new Vector2(1f, 1f), "BLD_Grid_Blocked"),
		new Plate("BLD_Grid_Boundary", "Bar", new Vector2(1f, 0.18f), "BLD_Grid_Boundary"),
		new Plate("BLD_Grid_Cell", "Plate", new Vector2(0.92f, 0.92f), "BLD_Grid_Focus"),
		new Plate("BLD_Grid_Edge", "Bar", new Vector2(1f, 0.16f), "BLD_Grid_Focus"),
		new Plate("BLD_Grid_Node", "Node", new Vector2(0.18f, 0.18f), "BLD_Grid_Focus")
	};

	[MenuItem("Eidren/Art/Build Grid Overlay")]
	public static void BuildOverlay()
	{
		EnsureFolder();
		Texture2D weave = EnsureTexture("BLD_Grid_Weave", WeavePixels());
		Texture2D mark = EnsureTexture("BLD_Grid_Mark", MarkPixels());
		weave.wrapMode = TextureWrapMode.Repeat;
		mark.wrapMode = TextureWrapMode.Clamp;
		EnsureMaterial("BLD_Grid_Area", weave, new Color(0.86f, 0.82f, 0.7f, 0.22f));
		EnsureMaterial("BLD_Grid_Boundary", mark, new Color(0.78f, 0.71f, 0.52f, 0.55f));
		EnsureMaterial("BLD_Grid_Focus", mark, new Color(0.88f, 0.9f, 0.84f, 0.55f));
		EnsureMaterial("BLD_Grid_Blocked", mark, new Color(0.36f, 0.42f, 0.52f, 0.5f));
		EnsureMaterial("BLD_Grid_Conflict", mark, new Color(0.85f, 0.35f, 0.3f, 0.55f));
		Plate[] plates = Plates;
		for (int i = 0; i < plates.Length; i++)
		{
			BuildPlate(plates[i]);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Verify();
		Debug.Log("Eidren: build grid overlay authored — 2 textures, " + string.Format("5 materials, {0} prefabs in {1}.", Plates.Length, "Assets/_Game/Resources/Art/BuildGrid"));
	}

	private static Color[] WeavePixels()
	{
		Color[] pixels = new Color[4096];
		for (int y = 0; y < 64; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				int toCornerX = Mathf.Min(x, 64 - x);
				int toCornerY = Mathf.Min(y, 64 - y);
				bool notch = (toCornerX <= 8 && toCornerY <= 1) || (toCornerY <= 8 && toCornerX <= 1);
				pixels[y * 64 + x] = new Color(1f, 1f, 1f, notch ? 1f : 0f);
			}
		}
		return pixels;
	}

	private static Color[] MarkPixels()
	{
		Color[] pixels = new Color[4096];
		for (int y = 0; y < 64; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				int toEdgeX = Mathf.Min(x, 63 - x);
				int toEdgeY = Mathf.Min(y, 63 - y);
				float alpha = ((Mathf.Min(toEdgeX, toEdgeY) >= 5) ? 0.14f : ((toEdgeX < 20 && toEdgeY < 20) ? 1f : 0.45f));
				pixels[y * 64 + x] = new Color(1f, 1f, 1f, alpha);
			}
		}
		return pixels;
	}

	private static Texture2D EnsureTexture(string assetName, Color[] pixels)
	{
		string path = "Assets/_Game/Resources/Art/BuildGrid/" + assetName + ".asset";
		Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		bool num = texture == null;
		if (num)
		{
			texture = new Texture2D(64, 64, TextureFormat.RGBA32, mipChain: true)
			{
				name = assetName
			};
		}
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

	private static void EnsureMaterial(string assetName, Texture2D texture, Color color)
	{
		Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
		if (shader == null)
		{
			throw new InvalidOperationException("URP Unlit shader is required for the build grid.");
		}
		string path = "Assets/_Game/Resources/Art/BuildGrid/" + assetName + ".mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (material == null)
		{
			material = new Material(shader);
			AssetDatabase.CreateAsset(material, path);
		}
		material.shader = shader;
		material.SetOverrideTag("RenderType", "Transparent");
		material.SetTexture("_BaseMap", texture);
		material.SetColor("_BaseColor", color);
		material.color = color;
		material.SetFloat("_Surface", 1f);
		material.SetFloat("_Blend", 0f);
		material.SetFloat("_AlphaClip", 0f);
		material.SetFloat("_Cull", 0f);
		material.SetFloat("_ZWrite", 0f);
		material.SetFloat("_SrcBlend", 5f);
		material.SetFloat("_DstBlend", 10f);
		material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
		material.DisableKeyword("_ALPHATEST_ON");
		material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		material.renderQueue = 3000;
		EditorUtility.SetDirty(material);
	}

	private static void BuildPlate(Plate plate)
	{
		GameObject root = new GameObject(plate.Asset);
		try
		{
			MeshRenderer renderer = AddQuad(root.transform, plate.Piece, plate.Size, RequireMaterial(plate.Material));
			root.AddComponent<BuildGridMark>().ConfigureReferences(new MeshRenderer[1] { renderer });
			PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/Art/BuildGrid/" + plate.Asset + ".prefab");
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static MeshRenderer AddQuad(Transform parent, string pieceName, Vector2 size, Material material)
	{
		GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Quad);
		piece.name = pieceName;
		Collider[] components = piece.GetComponents<Collider>();
		for (int i = 0; i < components.Length; i++)
		{
			UnityEngine.Object.DestroyImmediate(components[i]);
		}
		piece.transform.SetParent(parent, worldPositionStays: false);
		piece.transform.localPosition = Vector3.zero;
		piece.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		piece.transform.localScale = new Vector3(size.x, size.y, 1f);
		MeshRenderer component = piece.GetComponent<MeshRenderer>();
		component.sharedMaterial = material;
		component.shadowCastingMode = ShadowCastingMode.Off;
		component.receiveShadows = false;
		return component;
	}

	private static Material RequireMaterial(string assetName)
	{
		string path = "Assets/_Game/Resources/Art/BuildGrid/" + assetName + ".mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (!(material != null))
		{
			throw new InvalidOperationException("Grid material is missing: " + path);
		}
		return material;
	}

	private static void EnsureFolder()
	{
		if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources/Art/BuildGrid"))
		{
			AssetDatabase.CreateFolder("Assets/_Game/Resources/Art", "BuildGrid");
		}
	}

	private static void Verify()
	{
		Plate[] plates = Plates;
		for (int i = 0; i < plates.Length; i++)
		{
			Plate plate = plates[i];
			string path = "Assets/_Game/Resources/Art/BuildGrid/" + plate.Asset + ".prefab";
			GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (gameObject == null)
			{
				throw new InvalidOperationException("Grid prefab was not saved: " + path);
			}
			BuildGridMark mark = gameObject.GetComponent<BuildGridMark>();
			if (mark == null || mark.Pieces.Length == 0)
			{
				throw new InvalidOperationException("Grid prefab '" + plate.Asset + "' has no authored pieces on disk.");
			}
			if (gameObject.GetComponentInChildren<Collider>(includeInactive: true) != null)
			{
				throw new InvalidOperationException("Grid prefab '" + plate.Asset + "' carries a collider; the grid must not become a raycast target.");
			}
			Vector3 scale = mark.Pieces[0].transform.localScale;
			if (!Mathf.Approximately(scale.x, plate.Size.x) || !Mathf.Approximately(scale.y, plate.Size.y))
			{
				throw new InvalidOperationException($"Grid prefab '{plate.Asset}' is {scale} on disk, " + $"expected {plate.Size}.");
			}
		}
	}
}
}
