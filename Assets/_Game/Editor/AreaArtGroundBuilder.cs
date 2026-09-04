using Eidren.Data;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class AreaArtGroundBuilder
{
	public static void Build(Transform ground, Transform areaRoot, ZoneAreaArtDefinition areaArt, string key, float size)
	{
		Renderer component = ground.GetComponent<Renderer>();
		component.enabled = true;
		component.sharedMaterial = EnsureGroundMaterial(key, "Base", areaArt.BaseGround, size);
		BuildBlends(areaRoot, areaArt, key, size);
	}

	private static void BuildBlends(Transform root, ZoneAreaArtDefinition areaArt, string key, float size)
	{
		IReadOnlyList<AreaArtGroundLayer> layers = areaArt.GroundBlends;
		for (int i = 0; i < layers.Count; i++)
		{
			Material material = EnsureBlendMaterial(key, i, layers[i].Texture);
			GameObject gameObject = new GameObject($"GroundBlend_{i + 1}");
			gameObject.name = $"GroundBlend_{i + 1}";
			gameObject.transform.SetParent(root, worldPositionStays: false);
			float width = ((i == 0) ? (size * 0.62f) : (size * 0.5f));
			float depth = ((i == 0) ? (size * 0.54f) : (size * 0.45f));
			gameObject.transform.localPosition = new Vector3((i == 0) ? ((0f - size) * 0.18f) : (size * 0.24f), 0.015f, (i == 0) ? (size * 0.1f) : ((0f - size) * 0.16f));
			gameObject.transform.localEulerAngles = new Vector3(0f, (i == 0) ? (-13f) : 19f, 0f);
			gameObject.AddComponent<MeshFilter>().sharedMesh = BuildFeatheredPatch(key, i, width, depth);
			gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
		}
	}

	private static Mesh BuildFeatheredPatch(string key, int index, float width, float depth)
	{
		int side = 13;
		Vector3[] vertices = new Vector3[side * side];
		Vector2[] uvs = new Vector2[vertices.Length];
		Color[] colors = new Color[vertices.Length];
		int[] triangles = new int[864];
		for (int z = 0; z <= 12; z++)
		{
			for (int x = 0; x <= 12; x++)
			{
				int vertex = z * side + x;
				float u = (float)x / 12f;
				float v = (float)z / 12f;
				vertices[vertex] = new Vector3((u - 0.5f) * width, 0f, (v - 0.5f) * depth);
				uvs[vertex] = new Vector2(u * width / 6f, v * depth / 6f);
				float radius = new Vector2((u - 0.5f) * 2f, (v - 0.5f) * 2f).magnitude;
				float noise = Mathf.PerlinNoise(u * 3.7f + (float)index * 2.1f, v * 4.3f + (float)key.Length);
				float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, 0.52f, radius + noise * 0.12f));
				colors[vertex] = new Color(1f, 1f, 1f, alpha);
			}
		}
		int triangle = 0;
		for (int i = 0; i < 12; i++)
		{
			for (int j = 0; j < 12; j++)
			{
				int a = i * side + j;
				int b = a + 1;
				int c = a + side;
				int d = c + 1;
				triangles[triangle++] = a;
				triangles[triangle++] = c;
				triangles[triangle++] = b;
				triangles[triangle++] = b;
				triangles[triangle++] = c;
				triangles[triangle++] = d;
			}
		}
		Mesh mesh = new Mesh();
		mesh.name = $"{key}_GroundBlendPatch_{index + 1}";
		mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.colors = colors;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	private const string DetailMapPath = "Assets/_Game/Art/Zones/Shared/ground_detail_grain_v01.png";

	private const string MacroMapPath = "Assets/_Game/Art/Zones/Shared/ground_macro_variation_v01.png";

	private static Material EnsureGroundMaterial(string key, string role, Texture2D texture, float size)
	{
		string path = "Assets/_Game/Art/Zones/Materials/" + key + "_Ground_" + role + ".mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		// G-002: der Basisboden lag auf URP/Unlit und zeigte damit nur die eine
		// gekachelte Textur. Eidren/Ground Detail legt eine Koernungs- und eine
		// Grossflaechenlage darueber, damit die Flaeche Struktur bekommt und das
		// 6-Einheiten-Kachelraster nicht mehr ablesbar ist.
		Shader shader = Shader.Find("Eidren/Ground Detail");
		if (shader == null)
		{
			throw new InvalidOperationException("Missing Eidren/Ground Detail shader.");
		}
		if (material == null)
		{
			material = new Material(shader);
			AssetDatabase.CreateAsset(material, path);
		}
		material.shader = shader;
		material.mainTexture = texture;
		float tiling = size / ZoneAreaArtDefinition.GroundTileWorldSize;
		material.mainTextureScale = new Vector2(tiling, tiling);
		material.color = Color.white;
		material.renderQueue = 2000;
		ApplyDetailLayers(material);
		EditorUtility.SetDirty(material);
		return material;
	}

	// Die Zusatzlagen sind fuer alle Gebiete dieselben Graustufenbilder; nur die
	// Basistextur unterscheidet die Gebiete. Fehlt eines der Bilder, bleibt die
	// jeweilige Lage neutral (Vorgabewert "grey" ergibt Faktor 1,0) und der
	// Boden sieht aus wie vorher, statt dass der Aufbau fehlschlaegt.
	private static void ApplyDetailLayers(Material material)
	{
		Texture2D detail = AssetDatabase.LoadAssetAtPath<Texture2D>(DetailMapPath);
		Texture2D macro = AssetDatabase.LoadAssetAtPath<Texture2D>(MacroMapPath);
		if (detail != null)
		{
			material.SetTexture("_DetailMap", detail);
		}
		if (macro != null)
		{
			material.SetTexture("_MacroMap", macro);
		}
		// Gemessen, nicht gewaehlt. _DetailScale 0,42 legt die Koernung in die
		// Vergroesserung (rund 1,1 Pixel je Texel), damit das Mipmapping sie
		// nicht mittelt. Zwei Vorlaeufer sind daran gescheitert: 2,7 war
		// 5,8-fach verkleinert und trug nichts bei, 1,05 war 2,24-fach
		// verkleinert und trug 0,73 statt der vorhergesagten 2,6 bei.
		// Herleitung in Tools\Try-Detail.ps1 und Tools\Predict-Detail.ps1.
		material.SetFloat("_DetailScale", 0.42f);
		material.SetFloat("_DetailStrength", 0.6f);
		material.SetFloat("_MacroScale", 0.137f);
		material.SetFloat("_MacroStrength", 0.4f);
	}

	private static Material EnsureBlendMaterial(string key, int index, Texture2D texture)
	{
		string path = "Assets/_Game/Art/Zones/Materials/" + $"{key}_Ground_Blend_{index + 1}.mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		Shader shader = Shader.Find("Eidren/Area Art Blend");
		if (shader == null)
		{
			throw new InvalidOperationException("Missing Eidren/Area Art Blend shader.");
		}
		if (material == null)
		{
			material = new Material(shader);
			AssetDatabase.CreateAsset(material, path);
		}
		material.shader = shader;
		material.mainTexture = texture;
		material.mainTextureScale = Vector2.one;
		material.color = Color.white;
		material.renderQueue = 3000;
		// G-002: die Erdflaechen der Gebiete bestehen ausschliesslich aus diesen
		// Flicken. Ohne Zusatzlagen blieben sie strukturlos, waehrend nur der
		// Basisboden Koernung bekam - gemessen: Erde 5,457 vorher, 5,348 nachher.
		// Die Kachelung des Flickens ist im Mesh dieselbe wie die des Basisbodens,
		// die Faktoren gelten deshalb unveraendert.
		ApplyDetailLayers(material);
		EditorUtility.SetDirty(material);
		return material;
	}
}
}
