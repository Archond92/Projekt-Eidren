using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class ForgeVisualAssetBuilder
{
	[MenuItem("Eidren/Art/Build Forge Package C-D")]
	public static void Build()
	{
		ForgeContainerVisualBuilder.Build();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: built forge visual packages C-D.");
	}
}
}
