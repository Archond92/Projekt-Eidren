using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eidren.Editor
{
	/* Stilumbau Etappe 5, Task 5: generalisierte Zaehl-/Beleg-Kernlogik mit Pfadfilter-Parameter
	   (BAKEKETTE (b), zweite Variante: "Geschwisterwerkzeug" statt Umbau des bestehenden
	   E4-Werkzeugs). StyleProofPlacementReport (Etappe 4, unveraendert) bleibt bewusst
	   eigenstaendig, damit sein bereits abgenommener Beleg unangetastet bleibt; nur der NEUE
	   BuildingPlacementReport (Etappe 5) nutzt diese Kernlogik. Rein lesend: jede Szene wird
	   geoeffnet, durchsucht und NIE gespeichert. */
	internal static class PlacementReportCore
	{
		internal static void CountAndReport(string ueberschrift, string prefabFolder, string[] zoneSzenen, string reportPath, string szenenHinweis)
		{
			List<string> zeilen = new List<string>();
			int gesamtSumme = 0;
			Dictionary<string, int> propGesamt = new Dictionary<string, int>();

			zeilen.Add(ueberschrift);
			zeilen.Add("Erzeugt: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			zeilen.Add("Praefix: " + prefabFolder);
			zeilen.Add("Szenen: " + zoneSzenen.Length + (string.IsNullOrEmpty(szenenHinweis) ? string.Empty : " (" + szenenHinweis + ")"));
			zeilen.Add("Verfahren: rein lesend, EditorSceneManager.OpenScene je Szene, KEIN SaveScene.");
			zeilen.Add(string.Empty);

			foreach (string szenenPfad in zoneSzenen)
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
						if (string.IsNullOrEmpty(pfad) || !pfad.StartsWith(prefabFolder, StringComparison.Ordinal))
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
					zeilen.Add("(keine PrefabInstanzen unter diesem Praefix gefunden)");
				}
				zeilen.Add("Summe Szene: " + szenenSumme);
				zeilen.Add(string.Empty);
				gesamtSumme += szenenSumme;
			}

			zeilen.Add("=== Gesamt je Prefab (alle " + zoneSzenen.Length + " Szenen) ===");
			List<string> alleProps = new List<string>(propGesamt.Keys);
			alleProps.Sort(StringComparer.Ordinal);
			foreach (string propName in alleProps)
			{
				zeilen.Add(propName + ": " + propGesamt[propName]);
			}
			zeilen.Add(string.Empty);
			zeilen.Add("Verschiedene Prefabs unter Praefix platziert: " + alleProps.Count);
			zeilen.Add("Gesamtsumme aller PrefabInstanzen unter Praefix (alle Szenen): " + gesamtSumme);

			string zielOrdner = Path.GetDirectoryName(reportPath);
			if (!string.IsNullOrEmpty(zielOrdner) && !Directory.Exists(zielOrdner))
			{
				Directory.CreateDirectory(zielOrdner);
			}
			File.WriteAllLines(reportPath, zeilen.ToArray());
			Debug.Log("[Platzierungs-Beleg] geschrieben: " + reportPath + " (" + gesamtSumme + " Instanzen ueber " + zoneSzenen.Length + " Szenen, Praefix " + prefabFolder + ")");
		}
	}
}
