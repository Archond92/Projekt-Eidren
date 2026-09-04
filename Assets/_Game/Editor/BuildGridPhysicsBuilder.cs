using Eidren.Data;
using System;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine;

namespace Eidren.Editor
{
public static class BuildGridPhysicsBuilder
{
	private const string DataFolder = "Assets/_Game/Data/Buildings";

	private const float EdgeThickness = 0.2f;

	[MenuItem("Eidren/Data/Apply Build Grid Physics")]
	public static void ApplyPhysics()
	{
		string[] array = AssetDatabase.FindAssets("t:BuildingCostDefinition", new string[1] { "Assets/_Game/Data/Buildings" });
		int changed = 0;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			BuildingCostDefinition plan = AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>(AssetDatabase.GUIDToAssetPath(array2[i]));
			if (!(plan == null) && plan.TryGetLevel(1, out var footprint, out var prefab) && Apply(plan, footprint, prefab))
			{
				changed++;
			}
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Verify();
		Debug.Log($"Eidren: build-grid physics applied to {changed} building " + "prefabs.");
	}

	private static bool Apply(BuildingCostDefinition plan, BuildingFootprint footprint, GameObject prefab)
	{
		string path = AssetDatabase.GetAssetPath(prefab);
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		try
		{
			BoxCollider collider = root.GetComponentInChildren<BoxCollider>(includeInactive: true);
			NavMeshObstacle obstacle = root.GetComponentInChildren<NavMeshObstacle>(includeInactive: true);
			if (collider == null)
			{
				return false;
			}
			Vector3 size = (collider.size = SizeFor(plan, footprint, collider.size.y));
			collider.center = new Vector3(0f, size.y * 0.5f, 0f);
			if (obstacle != null)
			{
				obstacle.size = size;
				obstacle.center = collider.center;
				obstacle.enabled = plan.BlocksNavigation;
				obstacle.carving = plan.BlocksNavigation;
			}
			PrefabUtility.SaveAsPrefabAsset(root, path);
			return true;
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	private static Vector3 SizeFor(BuildingCostDefinition plan, BuildingFootprint footprint, float height)
	{
		return plan.PlacementKind switch
		{
			BuildingPlacementKind.Edge => new Vector3(1f, height, 0.2f), 
			BuildingPlacementKind.Floor => new Vector3(1f, height, 1f), 
			_ => new Vector3(footprint.Width, height, footprint.Depth), 
		};
	}

	private static void Verify()
	{
		string[] array = AssetDatabase.FindAssets("t:BuildingCostDefinition", new string[1] { "Assets/_Game/Data/Buildings" });
		for (int i = 0; i < array.Length; i++)
		{
			BuildingCostDefinition plan = AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>(AssetDatabase.GUIDToAssetPath(array[i]));
			if (plan == null || !plan.TryGetLevel(1, out var footprint, out var prefab))
			{
				continue;
			}
			BoxCollider collider = prefab.GetComponentInChildren<BoxCollider>(includeInactive: true);
			if (!(collider == null))
			{
				Vector3 expected = SizeFor(plan, footprint, collider.size.y);
				if ((collider.size - expected).sqrMagnitude > 0.0001f)
				{
					throw new InvalidOperationException($"Collider of '{plan.Id}' is {collider.size} on " + $"disk, expected {expected}.");
				}
				NavMeshObstacle obstacle = prefab.GetComponentInChildren<NavMeshObstacle>(includeInactive: true);
				if (obstacle != null && obstacle.enabled != plan.BlocksNavigation)
				{
					throw new InvalidOperationException("Navigation obstacle of '" + plan.Id + "' is " + $"{obstacle.enabled} on disk.");
				}
			}
		}
	}
}
}
