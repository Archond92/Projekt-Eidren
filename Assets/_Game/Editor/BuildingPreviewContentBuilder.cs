using Eidren.Presentation;
using System;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class BuildingPreviewContentBuilder
{
	private const string ResourceParent = "Assets/_Game/Resources/Art";

	private const string Folder = "Assets/_Game/Resources/Art/Buildings";

	private const int TextureSize = 128;

	private static readonly Color[] BodyColours = new Color[3]
	{
		new Color(0.24f, 0.86f, 0.62f, 1f),
		new Color(0.95f, 0.72f, 0.24f, 1f),
		new Color(0.91f, 0.36f, 0.31f, 1f)
	};

	private static readonly Color[] MarkColours = new Color[3]
	{
		new Color(0.34f, 0.94f, 0.7f, 0.78f),
		new Color(0.98f, 0.79f, 0.34f, 0.8f),
		new Color(0.95f, 0.42f, 0.36f, 0.82f)
	};

	private static readonly string[] StateNames = new string[3] { "Valid", "Conditional", "Invalid" };

	private const float Wash = 0.12f;

	private const float BorderWidth = 0.055f;

	[MenuItem("Eidren/Art/Build Placement Preview")]
	public static void BuildPreview()
	{
		EnsureFolder();
		Texture2D[] marks = new Texture2D[3]
		{
			EnsureTexture("BLD_PreviewMark_Contour", ContourPixels()),
			EnsureTexture("BLD_PreviewMark_Lock", LockPixels()),
			EnsureTexture("BLD_PreviewMark_Blocked", BlockedPixels())
		};
		for (int state = 0; state < StateNames.Length; state++)
		{
			EnsureBodyMaterial("BLD_Preview_" + StateNames[state], BodyColours[state]);
			EnsureMarkMaterial("BLD_PreviewMark_" + StateNames[state], marks[state], MarkColours[state]);
		}
		BuildMarkPrefab();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Verify();
		Debug.Log("Eidren: placement preview authored — 3 textures, 6 materials and 1 prefab in Assets/_Game/Resources/Art/Buildings.");
	}

	private static Color[] ContourPixels()
	{
		return Paint((float u, float v) => (!InBorder(u, v)) ? 0.12f : 1f);
	}

	private static Color[] LockPixels()
	{
		return Paint(delegate(float u, float v)
		{
			if (IsLock(u, v))
			{
				return 1f;
			}
			return (!InBorder(u, v) || !IsDash(u, v)) ? 0.12f : 1f;
		});
	}

	private static Color[] BlockedPixels()
	{
		return Paint(delegate(float u, float v)
		{
			if (IsCross(u, v))
			{
				return 1f;
			}
			if (!InBorder(u, v))
			{
				return 0.12f;
			}
			return (!IsHatch(u, v)) ? 0.35f : 1f;
		});
	}

	private static bool IsHatch(float u, float v)
	{
		return Mathf.Repeat((u + v) * 18f, 2f) < 1f;
	}

	private static bool InBorder(float u, float v)
	{
		return Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) < 0.055f;
	}

	private static bool IsDash(float u, float v)
	{
		float num = Mathf.Min(v, 1f - v);
		float vertical = Mathf.Min(u, 1f - u);
		return (int)(((num <= vertical) ? u : v) * 14f) % 2 == 0;
	}

	private static bool IsLock(float u, float v)
	{
		if (u >= 0.42f && u <= 0.58f && v >= 0.36f && v <= 0.5f)
		{
			return true;
		}
		float du = u - 0.5f;
		float dv = v - 0.5f;
		if (dv < 0f)
		{
			return false;
		}
		float radius = Mathf.Sqrt(du * du + dv * dv);
		if (radius >= 0.045f)
		{
			return radius <= 0.075f;
		}
		return false;
	}

	private static bool IsCross(float u, float v)
	{
		float du = u - 0.5f;
		float dv = v - 0.5f;
		if (Mathf.Max(Mathf.Abs(du), Mathf.Abs(dv)) > 0.2f)
		{
			return false;
		}
		if (!(Mathf.Abs(du - dv) < 0.038f))
		{
			return Mathf.Abs(du + dv) < 0.038f;
		}
		return true;
	}

	private static Color[] Paint(Func<float, float, float> alpha)
	{
		Color[] pixels = new Color[16384];
		for (int y = 0; y < 128; y++)
		{
			float v = ((float)y + 0.5f) / 128f;
			for (int x = 0; x < 128; x++)
			{
				float u = ((float)x + 0.5f) / 128f;
				pixels[y * 128 + x] = new Color(1f, 1f, 1f, alpha(u, v));
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
		texture.wrapMode = TextureWrapMode.Clamp;
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

	private static void EnsureBodyMaterial(string assetName, Color colour)
	{
		Material material = EnsureMaterial(assetName);
		material.SetTexture("_BaseMap", null);
		material.SetColor("_BaseColor", colour);
		material.color = colour;
		material.SetFloat("_Surface", 0f);
		material.SetFloat("_ZWrite", 1f);
		material.SetFloat("_Cull", 0f);
		material.SetOverrideTag("RenderType", "Opaque");
		material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
		material.renderQueue = 2000;
		EditorUtility.SetDirty(material);
	}

	private static void EnsureMarkMaterial(string assetName, Texture2D texture, Color colour)
	{
		Material material = EnsureMaterial(assetName);
		material.SetTexture("_BaseMap", texture);
		material.SetColor("_BaseColor", colour);
		material.color = colour;
		material.SetOverrideTag("RenderType", "Transparent");
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

	private static Material EnsureMaterial(string assetName)
	{
		Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
		if (shader == null)
		{
			throw new InvalidOperationException("URP Unlit shader is required for building previews.");
		}
		string path = "Assets/_Game/Resources/Art/Buildings/" + assetName + ".mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (material == null)
		{
			material = new Material(shader);
			AssetDatabase.CreateAsset(material, path);
		}
		material.shader = shader;
		return material;
	}

	private static void BuildMarkPrefab()
	{
		GameObject root = new GameObject("BLD_Preview_Mark");
		try
		{
			GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Quad);
			piece.name = "Plate";
			Collider[] components = piece.GetComponents<Collider>();
			for (int i = 0; i < components.Length; i++)
			{
				UnityEngine.Object.DestroyImmediate(components[i]);
			}
			piece.transform.SetParent(root.transform, worldPositionStays: false);
			piece.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
			piece.transform.localScale = Vector3.one;
			MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
			renderer.sharedMaterial = RequireMaterial("BLD_PreviewMark_Valid");
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			root.AddComponent<BuildGridMark>().ConfigureReferences(new MeshRenderer[1] { renderer });
			PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/Art/Buildings/BLD_Preview_Mark.prefab");
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static Material RequireMaterial(string assetName)
	{
		string path = "Assets/_Game/Resources/Art/Buildings/" + assetName + ".mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (!(material != null))
		{
			throw new InvalidOperationException("Preview material is missing: " + path);
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
		string[] stateNames = StateNames;
		foreach (string state in stateNames)
		{
			RequireMaterial("BLD_Preview_" + state);
			if (RequireMaterial("BLD_PreviewMark_" + state).GetTexture("_BaseMap") == null)
			{
				throw new InvalidOperationException("'BLD_PreviewMark_" + state + "' has no shape texture on disk — colour would be its only signal.");
			}
		}
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/Art/Buildings/BLD_Preview_Mark.prefab");
		if (prefab == null || prefab.GetComponent<BuildGridMark>() == null)
		{
			throw new InvalidOperationException("BLD_Preview_Mark was not saved with its mark component.");
		}
		if (prefab.GetComponentInChildren<Collider>(includeInactive: true) != null)
		{
			throw new InvalidOperationException("BLD_Preview_Mark carries a collider; the preview must not become a raycast target.");
		}
	}
}
}
