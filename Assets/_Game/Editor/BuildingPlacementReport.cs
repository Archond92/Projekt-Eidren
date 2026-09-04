using UnityEditor;

namespace Eidren.Editor
{
	/* Task 5 (Stilumbau Etappe 5): Platzierungs-Beleg fuer die 9 BLD_-Gebaeude. Geschwisterwerkzeug
	   zu StyleProofPlacementReport (Etappe 4), BAKEKETTE (b), zweite Variante: eigene Klasse statt
	   Umbau des bestehenden E4-Werkzeugs, gleiche Kernlogik (PlacementReportCore), aber ALLE 11
	   Projektszenen (nicht nur die 8 Zonenszenen aus E4) -- EidraForge.unity, Bootstrap.unity und
	   MainMenu.unity sind hier bewusst mit dabei, da BAKEKETTE (b) sie fuer Gebaeude nicht
	   ausschliesst ("im Plan nicht ausgeschlossen") und Task 5 den Beleg ueber "alle Szenen" fordert.
	   Erwartung laut BAKEKETTE (b): 0 Treffer in jeder Szene -- Gebaeude werden ausschliesslich zur
	   Laufzeit per Object.Instantiate() aus BuildingInstanceState gebaut (BuildingPlacementController.
	   Continuation.cs, SpawnInstance), nie als PrefabInstance in eine Szene gebacken. */
	internal static class BuildingPlacementReport
	{
		private const string PrefabFolder = "Assets/_Game/Prefabs/Buildings/Level01/";

		private const string ReportPath = "TempReview/StilumbauE5/platzierungsbeleg.txt";

		private static readonly string[] AlleSzenen =
		{
			"Assets/_Game/Scenes/Bootstrap.unity",
			"Assets/_Game/Scenes/MainMenu.unity",
			"Assets/_Game/Scenes/HomeBase.unity",
			"Assets/_Game/Scenes/EidraForge.unity",
			"Assets/_Game/Scenes/Zone_Greenwood.unity",
			"Assets/_Game/Scenes/Zone_Marsh.unity",
			"Assets/_Game/Scenes/Zone_Quarry.unity",
			"Assets/_Game/Scenes/Zone_EmberRuins.unity",
			"Assets/_Game/Scenes/Zone_TwilightGrove.unity",
			"Assets/_Game/Scenes/Zone_VeilMarsh.unity",
			"Assets/_Game/Scenes/Zone_GreyRifts.unity",
		};

		[MenuItem("Eidren/V0.2/Stilumbau/Gebaeude-Platzierungs-Beleg")]
		public static void CountAndReport()
		{
			PlacementReportCore.CountAndReport(
				"Stilumbau Etappe 5 — Task 5: Platzierungs-Beleg (BLD_-PrefabInstanzen je Szene)",
				PrefabFolder,
				AlleSzenen,
				ReportPath,
				"alle Projektszenen inkl. Bootstrap/MainMenu/EidraForge — BAKEKETTE (b): Gebaeude werden nie in eine Szene gebacken");
		}
	}
}
