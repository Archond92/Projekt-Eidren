using Eidren.Data;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class WorldVisualAssetBuilder
{
	public const string LevelOneBuildingFolder = "Assets/_Game/Prefabs/Buildings/Level01";

	private const string BuildingArtFolder = "Assets/_Game/Art/Buildings/Level01";

	private const string BuildingMaterialFolder = "Assets/_Game/Art/Buildings/Materials";

	private const string ResourceArtFolder = "Assets/_Game/Art/Resources";

	private const string ResourceVisualFolder = "Assets/_Game/Prefabs/Resources/Visuals";

	private static readonly IReadOnlyDictionary<string, string> BuildingVisualIds = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["Smelter"] = "visual.building.smelter",
		["Sawmill"] = "visual.building.sawmill",
		["Ropewalk"] = "visual.building.ropewalk",
		["Stonecutter"] = "visual.building.stonecutter",
		["CookingPot"] = "visual.building.cooking_pot",
		["FarmPlot"] = "visual.building.farm_plot",
		["Wall"] = "visual.building.wall",
		["Floor"] = "visual.building.floor",
		["Door"] = "visual.building.door"
	};

	[MenuItem("Eidren/Art/Build Phase D World Visuals")]
	public static void BuildWorldVisuals()
	{
		EnsureFolders();
		foreach (string key in BuildingVisualIds.Keys)
		{
			BuildBuilding(key);
		}
		BuildBerryBushVisuals();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Verify();
		Debug.Log("Eidren: built nine unique level-1 building visuals and two berry-bush states.");
	}

	internal static void BuildBerryBushVisuals()
	{
		EnsureFolders();
		BuildResourceVisual("BerryBush_Active", "berry_bush_active", "visual.berry_bush.active");
		BuildResourceVisual("BerryBush_Exhausted", "berry_bush_exhausted", "visual.berry_bush.exhausted");
	}

	private static void BuildBuilding(string buildingName)
	{
		string assetName = "BLD_" + buildingName + "_L01";
		string spritePath = "Assets/_Game/Art/Buildings/Level01/" + assetName + ".png";
		if (!File.Exists(spritePath))
		{
			throw new FileNotFoundException("Building art is missing: " + spritePath);
		}
		ArtAssetImportUtility.ConfigureSprite(spritePath, 1024);
		Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
		Material material = EnsureSpriteMaterial("Assets/_Game/Art/Buildings/Materials/" + assetName + ".mat", Color.white);
		string destinationPath = "Assets/_Game/Prefabs/Buildings/Level01/" + assetName + ".prefab";
		if (AssetDatabase.LoadAssetAtPath<GameObject>(destinationPath) != null)
		{
			return;
		}
		string sourcePath = "Assets/_Game/Prefabs/Buildings/" + buildingName + ".prefab";
		GameObject root = PrefabUtility.LoadPrefabContents(sourcePath);
		if (root == null)
		{
			throw new InvalidOperationException("Building source prefab is missing: " + sourcePath);
		}
		try
		{
			root.name = assetName;
			RemoveChildren(root.transform);
			AddSpriteVisual(root.transform, sprite, material, Entry(BuildingVisualIds[buildingName]));
			PrefabUtility.SaveAsPrefabAsset(root, destinationPath);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	private static void BuildResourceVisual(string prefabName, string spriteName, string visualId)
	{
		string spritePath = "Assets/_Game/Art/Resources/" + spriteName + ".png";
		if (!File.Exists(spritePath))
		{
			throw new FileNotFoundException("Resource art is missing: " + spritePath);
		}
		ArtAssetImportUtility.ConfigureSprite(spritePath, 1024);
		Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
		Material material = EnsureSpriteMaterial("Assets/_Game/Art/Resources/Materials/RES_" + prefabName + ".mat", Color.white);
		string prefabPath = "Assets/_Game/Prefabs/Resources/Visuals/" + prefabName + ".prefab";
		GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		if (existing != null && existing.GetComponentInChildren<MeshRenderer>(includeInactive: true) != null && existing.GetComponentInChildren<SpriteRenderer>(includeInactive: true) == null)
		{
			return;
		}
		GameObject root = ((existing == null) ? new GameObject(prefabName) : PrefabUtility.LoadPrefabContents(prefabPath));
		try
		{
			root.name = prefabName;
			RemoveChildren(root.transform);
			AddSpriteVisual(root.transform, sprite, material, Entry(visualId));
			PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
		}
		finally
		{
			if (existing == null)
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
			else
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}
	}

	private static void AddSpriteVisual(Transform parent, Sprite sprite, Material material, VisualScaleEntry entry)
	{
		GameObject gameObject = new GameObject("GeneratedArt");
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = sprite;
		spriteRenderer.sharedMaterial = material;
		spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
		spriteRenderer.receiveShadows = false;
		VisualScaleBaker.Apply(parent.gameObject, entry);
	}

	private static VisualScaleEntry Entry(string visualId)
	{
		return VisualScaleTableBuilder.Load().Require(visualId);
	}

	private static Material EnsureSpriteMaterial(string path, Color color)
	{
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (material == null)
		{
			material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"));
			AssetDatabase.CreateAsset(material, path);
		}
		material.color = color;
		EditorUtility.SetDirty(material);
		return material;
	}

	private static void RemoveChildren(Transform root)
	{
		while (root.childCount > 0)
		{
			UnityEngine.Object.DestroyImmediate(root.GetChild(root.childCount - 1).gameObject);
		}
	}

	/* Verify-Nachzug (Stilumbau Etappe 5, Fix-Queue-Eintrag aus Task 3+4):
	   Alt-Vertrag (vor E1-E4) prüfte je Visual GENAU EINE Bauweise:
	     - Gebäude (BuildingVisualIds-Schleife): nur Existenz des Level01-Prefabs
	       ("wurde die Sprite-Karte gespeichert?") -- kein Geometrie-/Materialcheck.
	     - Ressourcen-Visuals (BerryBush_Active/_Exhausted): Pflicht-SpriteRenderer
	       ("wurde die Sprite-Karte auf ein Kind montiert?") -- das war der einzig
	       existierende Vertrag, als BuildResourceVisual() noch AddSpriteVisual()
	       ausschliesslich mit SpriteRenderer erzeugte.
	   Seit E1-E4 ersetzt die Fabrik-Pipeline (EidrenMeshFactory + EidrenWorldStyleAssets)
	   SpriteRenderer durch echte 3D-Geometrie mit Vertexfarben und dem einen geteilten
	   Material M_EidrenWorld_VertexLit -- die BerryBush-Visuals sind seit E3b so gebaut,
	   wodurch der alte SpriteRenderer-Pflichtcheck hier zuverlaessig mit
	   "Resource visual was not saved" scheiterte, obwohl das Visual korrekt gebaut war
	   (STALE CONTRACT, siehe Task-3+4-Report).
	   Neuer, NICHT abgeschwaechter Vertrag je Visual: Existenz PLUS (Fabrik-Kontrakt
	   ODER alter handgebauter Kontrakt). Ein Visual, das weder Fabrik-Geometrie noch
	   eine erkennbare Handgeometrie/Sprite traegt (z. B. leeres oder kaputtes Prefab),
	   faellt weiterhin durch -- das deckt auch neu hinzukommende, noch handgebaute
	   Assets ab (z. B. die Stationen Workbench/StorageChest, die laut Etappe-2-Entscheid
	   bewusst bei Geometry_A14 + eigenem Material bleiben und nie auf die
	   Fabrik-Materialvorgabe umgestellt werden). */
	private static void Verify()
	{
		foreach (string buildingName in BuildingVisualIds.Keys)
		{
			string path = "Assets/_Game/Prefabs/Buildings/Level01/BLD_" + buildingName + "_L01.prefab";
			VerifyVisualContract(path, "Building prefab");
		}
		string[] array = new string[2] { "BerryBush_Active", "BerryBush_Exhausted" };
		foreach (string name in array)
		{
			string path2 = "Assets/_Game/Prefabs/Resources/Visuals/" + name + ".prefab";
			VerifyVisualContract(path2, "Resource visual");
		}
	}

	private static void VerifyVisualContract(string path, string label)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		if (prefab == null)
		{
			throw new InvalidOperationException(label + " was not saved: " + path);
		}
		if (!HasFactoryVisualContract(prefab) && !HasHandAuthoredVisualContract(prefab))
		{
			throw new InvalidOperationException(label + " has neither factory geometry nor a recognised hand-authored visual: " + path);
		}
	}

	// Fabrik-Kontrakt (E1-E4, alle T1/T2/Props/Gebaeude): mindestens ein MeshFilter mit
	// Vertexfarben (colors.Length == vertexCount, echte Mesh-Daten) UND ausnahmslos alle
	// MeshRenderer nutzen das eine geteilte Material M_EidrenWorld_VertexLit.
	private static bool HasFactoryVisualContract(GameObject prefab)
	{
		bool hatVertexfarbenMesh = false;
		foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true))
		{
			Mesh mesh = filter.sharedMesh;
			if (mesh != null && mesh.vertexCount > 0 && mesh.colors.Length == mesh.vertexCount)
			{
				hatVertexfarbenMesh = true;
			}
		}
		if (!hatVertexfarbenMesh)
		{
			return false;
		}
		MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		if (renderers.Length == 0)
		{
			return false;
		}
		foreach (MeshRenderer renderer in renderers)
		{
			if (renderer.sharedMaterial == null || renderer.sharedMaterial.name != "M_EidrenWorld_VertexLit")
			{
				return false;
			}
		}
		return true;
	}

	// Alter handgebauter Kontrakt, zwei historische Varianten, beide weiterhin gueltig:
	//   1) Gebaeude-/Stationsmuster: Kind "Geometry_A14" mit mindestens einem Renderer
	//      (unabhaengig vom Material -- Workbench/StorageChest bleiben laut
	//      Etappe-2-Entscheid bewusst bei eigenem Material).
	//   2) Aeltestes Sprite-Karten-Muster (vor E1): direkter SpriteRenderer am Visual.
	private static bool HasHandAuthoredVisualContract(GameObject prefab)
	{
		Transform geometrie = prefab.transform.Find("Geometry_A14");
		if (geometrie != null && geometrie.GetComponentInChildren<Renderer>(includeInactive: true) != null)
		{
			return true;
		}
		return prefab.GetComponentInChildren<SpriteRenderer>(includeInactive: true) != null;
	}

	private static void EnsureFolders()
	{
		EnsureFolder("Assets/_Game/Prefabs/Buildings", "Level01");
		EnsureFolder("Assets/_Game/Art/Buildings", "Materials");
		EnsureFolder("Assets/_Game/Art/Resources", "Materials");
		EnsureFolder("Assets/_Game/Prefabs/Resources", "Visuals");
	}

	private static void EnsureFolder(string parent, string name)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + name))
		{
			AssetDatabase.CreateFolder(parent, name);
		}
	}
}
}
