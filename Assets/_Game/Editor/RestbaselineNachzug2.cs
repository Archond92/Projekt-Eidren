using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Zweites Sammelpaket der Restbaseline-Triage (13.08.2026): Phase-E-
	/// UI-Prefabs (Kunst lag bereit, der Builder lief nie), Alpha-Stanzung
	/// der Terrock-/Noctarion-Begleitkarten, FarmPlot-Zustands-Visuals und
	/// die Familien-Toenung der Platzhalter-Icons. Ein Editorlauf statt vier.
	/// </summary>
	public static class RestbaselineNachzug2
	{
		public static void Build()
		{
			UiVisualAssetBuilder.BuildUiPrefabs();
			// Alpha-Stanzung der 2D-Begleitkarten entfaellt: Karten und Werkzeug
			// wurden am 03.09.2026 als Altlast entfernt (Terrock/Noctarion sind Mid-Poly-3D).
			FarmPlotVisualRebuilder.Bauen();
			ItemIconFamilienToenung.Toenen();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("[NACHZUG2] Sammelauf abgeschlossen.");
		}
	}
}
