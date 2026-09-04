using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class Auftrag4ContentBuilder
{
	[MenuItem("Eidren/Build/Build Auftrag 4 Content")]
	public static void BuildAvailableContent()
	{
		EidraForgeLootAssetBuilder.Build();
		V02ActorVisualBuilder.Build();
		ForgeVisualAssetBuilder.Build();
		IgnivarContentBuilder.Build();
		EidraForgeEnemyContentBuilder.Build();
		EidraForgeSceneBuilder.Build();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Eidren: Auftrag 4 authored content build completed.");
	}
}
}
