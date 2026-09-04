using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
internal static class ResourceNodeVisualTable
{
	internal readonly struct VisualPair
	{
		public readonly GameObject Active;

		public readonly GameObject Exhausted;

		public VisualPair(GameObject active, GameObject exhausted)
		{
			Active = active;
			Exhausted = exhausted;
		}
	}

	private const string VisualFolder = "Assets/_Game/Prefabs/Resources/Visuals";

	internal static Dictionary<string, VisualPair> BuildAll()
	{
		return new Dictionary<string, VisualPair>(StringComparer.Ordinal)
		{
			["resource.tree"] = Pair("Tree"),
			["resource.stone_deposit"] = Pair("StoneDeposit"),
			["resource.fiber_plant"] = Pair("FiberPlant"),
			["resource.copper_vein"] = Pair("CopperVein"),
			["resource.berry_bush"] = Pair("BerryBush"),
			["resource.hardwood_tree"] = Pair("HardwoodTree"),
			["resource.swamp_hemp"] = Pair("SwampHemp"),
			["resource.granite_deposit"] = Pair("GraniteDeposit"),
			["resource.iron_vein"] = Pair("IronVein")
		};
	}

	private static VisualPair Pair(string stem)
	{
		return new VisualPair(RequireVisual(stem + "_Active"), RequireVisual(stem + "_Exhausted"));
	}

	private static GameObject RequireVisual(string name)
	{
		string path = "Assets/_Game/Prefabs/Resources/Visuals/" + name + ".prefab";
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		if (gameObject == null)
		{
			throw new InvalidOperationException("Required authored resource visual is missing: " + path);
		}
		return gameObject;
	}
}
}
