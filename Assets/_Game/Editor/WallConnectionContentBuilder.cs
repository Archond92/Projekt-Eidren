using Eidren.Data;
using Eidren.Interaction;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class WallConnectionContentBuilder
{
	private const string DataFolder = "Assets/_Game/Data/Buildings";

	private const float PostThickness = 0.36f;

	private const float CapLength = 0.12f;

	private const float CapThickness = 0.4f;

	private const float EndOffset = 0.5f;

	[MenuItem("Eidren/Data/Apply Wall Connection Pieces")]
	public static void ApplyPieces()
	{
		int changed = 0;
		BuildingCostDefinition[] array = EdgePlans();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].TryGetLevel(1, out var _, out var prefab) && Apply(prefab))
			{
				changed++;
			}
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Verify();
		Debug.Log($"Eidren: wall connection pieces applied to {changed} edge " + "prefabs.");
	}

	private static bool Apply(GameObject prefab)
	{
		string path = AssetDatabase.GetAssetPath(prefab);
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		try
		{
			BoxCollider collider = root.GetComponentInChildren<BoxCollider>(includeInactive: true);
			if (collider == null)
			{
				return false;
			}
			float height = collider.size.y;
			float centreY = collider.center.y;
			Material material = SharedMaterial(root);
			GameObject postMinus = Piece(root, "Joint_MinusX", material, new Vector3(0.36f, height, 0.36f), new Vector3(-0.5f, centreY, 0f));
			GameObject postPlus = Piece(root, "Joint_PlusX", material, new Vector3(0.36f, height, 0.36f), new Vector3(0.5f, centreY, 0f));
			GameObject capMinus = Piece(root, "Cap_MinusX", material, new Vector3(0.12f, height, 0.4f), new Vector3(-0.5f, centreY, 0f));
			GameObject capPlus = Piece(root, "Cap_PlusX", material, new Vector3(0.12f, height, 0.4f), new Vector3(0.5f, centreY, 0f));
			WallConnectionView view = root.GetComponent<WallConnectionView>();
			if (view == null)
			{
				view = root.AddComponent<WallConnectionView>();
			}
			view.ConfigureReferences(postMinus, postPlus, capMinus, capPlus);
			PrefabUtility.SaveAsPrefabAsset(root, path);
			return true;
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	private static GameObject Piece(GameObject root, string pieceName, Material material, Vector3 size, Vector3 position)
	{
		Transform existing = root.transform.Find(pieceName);
		GameObject piece = ((existing != null) ? existing.gameObject : new GameObject(pieceName));
		piece.transform.SetParent(root.transform, worldPositionStays: false);
		piece.transform.localPosition = position;
		piece.transform.localRotation = Quaternion.identity;
		piece.transform.localScale = size;
		MeshFilter filter = piece.GetComponent<MeshFilter>();
		if (filter == null)
		{
			filter = piece.AddComponent<MeshFilter>();
		}
		MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
		if (renderer == null)
		{
			renderer = piece.AddComponent<MeshRenderer>();
		}
		if (!MidpolyBaseModuleMigration.TryApplyApprovedConnectionPieceVisual(piece))
		{
			filter.sharedMesh = BuiltInCube();
			renderer.sharedMaterial = material;
		}
		Collider[] components = piece.GetComponents<Collider>();
		for (int i = 0; i < components.Length; i++)
		{
			UnityEngine.Object.DestroyImmediate(components[i], allowDestroyingAssets: true);
		}
		piece.SetActive(value: false);
		return piece;
	}

	private static Material SharedMaterial(GameObject root)
	{
		MeshRenderer[] componentsInChildren = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		foreach (MeshRenderer renderer in componentsInChildren)
		{
			if (renderer.sharedMaterial != null && !IsPiece(renderer.name))
			{
				return renderer.sharedMaterial;
			}
		}
		throw new InvalidOperationException("Edge prefab '" + root.name + "' has no material to share.");
	}

	private static bool IsPiece(string objectName)
	{
		if (!objectName.StartsWith("Joint_", StringComparison.Ordinal))
		{
			return objectName.StartsWith("Cap_", StringComparison.Ordinal);
		}
		return true;
	}

	private static Mesh BuiltInCube()
	{
		GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
		try
		{
			return probe.GetComponent<MeshFilter>().sharedMesh;
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(probe);
		}
	}

	private static BuildingCostDefinition[] EdgePlans()
	{
		string[] array = AssetDatabase.FindAssets("t:BuildingCostDefinition", new string[1] { "Assets/_Game/Data/Buildings" });
		List<BuildingCostDefinition> plans = new List<BuildingCostDefinition>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			BuildingCostDefinition plan = AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>(AssetDatabase.GUIDToAssetPath(array2[i]));
			if (plan != null && plan.PlacementKind == BuildingPlacementKind.Edge)
			{
				plans.Add(plan);
			}
		}
		return plans.ToArray();
	}

	private static void Verify()
	{
		BuildingCostDefinition[] array = EdgePlans();
		foreach (BuildingCostDefinition plan in array)
		{
			if (!plan.TryGetLevel(1, out var _, out var prefab))
			{
				continue;
			}
			if (prefab.GetComponent<WallConnectionView>() == null)
			{
				throw new InvalidOperationException("Edge prefab of '" + plan.Id + "' has no WallConnectionView on disk.");
			}
			string[] array2 = new string[4] { "Joint_MinusX", "Joint_PlusX", "Cap_MinusX", "Cap_PlusX" };
			foreach (string pieceName in array2)
			{
				Transform piece = prefab.transform.Find(pieceName);
				if (piece == null)
				{
					throw new InvalidOperationException("Edge prefab of '" + plan.Id + "' is missing '" + pieceName + "' on disk.");
				}
				if (piece.GetComponent<Collider>() != null)
				{
					throw new InvalidOperationException("'" + pieceName + "' of '" + plan.Id + "' carries a collider; presentation must not change occupancy.");
				}
			}
		}
	}
}
}
