using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Sammelaufruf fuer die Builder, deren fehlende Ausgaben die
	/// Restbaseline rot hielten (Triage 13.08.2026): WorldMap-Prefabs und
	/// -Sprites (8 Tests), Item-/Loot-Kette nach dem Material()-Icon-Fix
	/// (5 Tests), Ignivar-Inhalte (Ability-Icons), und der Wanderer3D-Build
	/// mit dem Renderer-statt-SkinnedMeshRenderer-Materialfix (Waffe_Axt trug
	/// den glTF-Importshader). Ein Editorlauf statt vier.
	/// </summary>
	public static class RestbaselineNachzug
	{
		public static void Build()
		{
			WorldMapContentBuilder.BuildRegionalWorldMap();
			EidraForgeLootAssetBuilder.Build();
			IgnivarContentBuilder.Build();
			Wanderer3DPlayerBuilder.Build();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("[NACHZUG] Builder-Sammelauf abgeschlossen.");
		}
	}
}
