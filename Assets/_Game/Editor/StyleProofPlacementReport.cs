using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eidren.Editor
{
	/* Task 4 (Stilumbau Etappe 4): Platzierungs-Beleg. Zaehlt PrefabInstanzen der 16
	   SP_-Requisiten in den 8 Zonenszenen, in die sie laut STILUMBAU_E4_BAKEKETTE.md (b)
	   gebacken werden (EidraForge.unity ist dort NICHT gelistet, daher hier ausgeschlossen).
	   Rein lesend: jede Szene wird geoeffnet, durchsucht und NIE gespeichert. Belegt die
	   BAKEKETTE-Erwartung, dass gebackene Instanzen nach dem Task-3-Prefab-Rebuild automatisch
	   die neue Geometrie zeigen, ohne dass ein Szenen-Rebake noetig ist (GUIDs blieben stabil). */
	internal static class StyleProofPlacementReport
	{
		private const string PrefabFolder = "Assets/_Game/Prefabs/Environment/StyleProof/";

		private const string ReportPath = "TempReview/StilumbauE4/platzierungsbeleg.txt";

		private static readonly string[] ZoneSzenen =
		{
			"Assets/_Game/Scenes/Zone_Greenwood.unity",
			"Assets/_Game/Scenes/Zone_Marsh.unity",
			"Assets/_Game/Scenes/Zone_Quarry.unity",
			"Assets/_Game/Scenes/Zone_EmberRuins.unity",
			"Assets/_Game/Scenes/Zone_TwilightGrove.unity",
			"Assets/_Game/Scenes/Zone_VeilMarsh.unity",
			"Assets/_Game/Scenes/Zone_GreyRifts.unity",
			"Assets/_Game/Scenes/HomeBase.unity",
		};

		[MenuItem("Eidren/V0.2/Stilumbau/Prop-Platzierungs-Beleg")]
		public static void CountAndReport()
		{
			List<string> zeilen = new List<string>();
			int gesamtSumme = 0;
			Dictionary<string, int> propGesamt = new Dictionary<string, int>();

			zeilen.Add("Stilumbau Etappe 4 — Task 4: Platzierungs-Beleg (SP_-PrefabInstanzen je Zonenszene)");
			zeilen.Add("Erzeugt: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			zeilen.Add("Szenen: " + ZoneSzenen.Length + " (EidraForge.unity nicht in STILUMBAU_E4_BAKEKETTE.md (b) gelistet, daher ausgeschlossen)");
			zeilen.Add("Verfahren: rein lesend, EditorSceneManager.OpenScene je Szene, KEIN SaveScene.");
			zeilen.Add(string.Empty);

			foreach (string szenenPfad in ZoneSzenen)
			{
				// OpenSceneMode.Single, aber NIE SaveScene() -- rein lesend.
				Scene szene = EditorSceneManager.OpenScene(szenenPfad, OpenSceneMode.Single);
				Dictionary<string, int> zaehler = new Dictionary<string, int>();
				foreach (GameObject wurzel in szene.GetRootGameObjects())
				{
					foreach (Transform t in wurzel.GetComponentsInChildren<Transform>(includeInactive: true))
					{
						GameObject quelle = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
						if (quelle == null)
						{
							continue;
						}
						string pfad = AssetDatabase.GetAssetPath(quelle);
						if (string.IsNullOrEmpty(pfad) || !pfad.StartsWith(PrefabFolder, StringComparison.Ordinal))
						{
							continue;
						}
						// Nur die Wurzel der PrefabInstance zaehlen, nicht jedes Kind:
						if (PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject) != t.gameObject)
						{
							continue;
						}
						string stamm = Path.GetFileNameWithoutExtension(pfad);
						zaehler[stamm] = zaehler.TryGetValue(stamm, out int vorhanden) ? vorhanden + 1 : 1;
					}
				}

				zeilen.Add("=== " + szenenPfad + " ===");
				int szenenSumme = 0;
				List<string> propNamen = new List<string>(zaehler.Keys);
				propNamen.Sort(StringComparer.Ordinal);
				foreach (string propName in propNamen)
				{
					int anzahl = zaehler[propName];
					zeilen.Add(propName + ": " + anzahl);
					szenenSumme += anzahl;
					propGesamt[propName] = propGesamt.TryGetValue(propName, out int alt) ? alt + anzahl : anzahl;
				}
				if (propNamen.Count == 0)
				{
					zeilen.Add("(keine SP_-PrefabInstanzen gefunden)");
				}
				zeilen.Add("Summe Szene: " + szenenSumme);
				zeilen.Add(string.Empty);
				gesamtSumme += szenenSumme;
			}

			zeilen.Add("=== Gesamt je Prop (alle 8 Szenen) ===");
			List<string> alleProps = new List<string>(propGesamt.Keys);
			alleProps.Sort(StringComparer.Ordinal);
			foreach (string propName in alleProps)
			{
				zeilen.Add(propName + ": " + propGesamt[propName]);
			}
			zeilen.Add(string.Empty);
			zeilen.Add("Verschiedene SP_-Prefabs platziert: " + alleProps.Count + " von 16");
			zeilen.Add("Gesamtsumme aller SP_-PrefabInstanzen (alle Szenen): " + gesamtSumme);

			string zielOrdner = Path.GetDirectoryName(ReportPath);
			if (!string.IsNullOrEmpty(zielOrdner) && !Directory.Exists(zielOrdner))
			{
				Directory.CreateDirectory(zielOrdner);
			}
			File.WriteAllLines(ReportPath, zeilen.ToArray());
			Debug.Log("[StilE4-Beleg] Platzierungs-Beleg geschrieben: " + ReportPath + " (" + gesamtSumme + " Instanzen ueber " + ZoneSzenen.Length + " Szenen)");
		}
	}
}
