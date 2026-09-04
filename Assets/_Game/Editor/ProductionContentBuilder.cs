using Eidren.Interaction;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class ProductionContentBuilder
{
	private const string BuildingFolder = "Assets/_Game/Prefabs/Buildings/Level01";

	private static readonly string[] CraftingPrefabPaths = new string[5] { "Assets/_Game/Prefabs/Buildings/Level01/BLD_Smelter_L01.prefab", "Assets/_Game/Prefabs/Buildings/Level01/BLD_Sawmill_L01.prefab", "Assets/_Game/Prefabs/Buildings/Level01/BLD_Ropewalk_L01.prefab", "Assets/_Game/Prefabs/Buildings/Level01/BLD_Stonecutter_L01.prefab", "Assets/_Game/Prefabs/Buildings/Level01/BLD_CookingPot_L01.prefab" };

	[MenuItem("Eidren/Production/Build V0.1")]
	public static void BuildProductionContent()
	{
		WorldVisualAssetBuilder.BuildWorldVisuals();
		string[] craftingPrefabPaths = CraftingPrefabPaths;
		for (int i = 0; i < craftingPrefabPaths.Length; i++)
		{
			EnsureComponent<WorkbenchController>(craftingPrefabPaths[i]);
		}
		EnsureComponent<FarmPlotController>("Assets/_Game/Prefabs/Buildings/Level01/BLD_FarmPlot_L01.prefab");
		BuildingCostContentBuilder.BuildBuildingCosts();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Verify();
		Debug.Log("Eidren: production stations and farm content are ready.");
	}

	private static void EnsureComponent<T>(string path) where T : Component
	{
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		if (root == null)
		{
			throw new InvalidOperationException("Production prefab is missing: " + path);
		}
		try
		{
			if (root.GetComponent<T>() == null)
			{
				root.AddComponent<T>();
			}
			PrefabUtility.SaveAsPrefabAsset(root, path);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	private static void Verify()
	{
		string[] craftingPrefabPaths = CraftingPrefabPaths;
		foreach (string path in craftingPrefabPaths)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab == null || prefab.GetComponent<WorkbenchController>() == null)
			{
				throw new InvalidOperationException("Crafting station component missing at '" + path + "'.");
			}
		}
		string farmPath = "Assets/_Game/Prefabs/Buildings/Level01/BLD_FarmPlot_L01.prefab";
		GameObject farm = AssetDatabase.LoadAssetAtPath<GameObject>(farmPath);
		if (farm == null || farm.GetComponent<FarmPlotController>() == null)
		{
			throw new InvalidOperationException("Farm component missing at '" + farmPath + "'.");
		}
	}
}
}
