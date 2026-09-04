using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Loescht die per AltlastInventar + String-Schutzrechnung bestimmten
	/// 2D-Altlasten. Die Liste kommt aus einer JSON-Datei (Umgebungsvariable
	/// EIDREN_LOESCHLISTE), damit Rechnung und Ausfuehrung getrennt bleiben
	/// und die Liste vor dem Lauf gesichert werden kann. AssetDatabase.
	/// DeleteAsset raeumt die .meta mit ab. Bricht beim ersten Fehler ab.
	/// </summary>
	public static class AltlastLoescher
	{
		public static void Loeschen()
		{
			string listePfad = Environment.GetEnvironmentVariable("EIDREN_LOESCHLISTE");
			if (string.IsNullOrEmpty(listePfad) || !File.Exists(listePfad))
			{
				throw new InvalidOperationException("EIDREN_LOESCHLISTE fehlt: " + listePfad);
			}
			string json = File.ReadAllText(listePfad);
			var pfade = new List<string>();
			foreach (string roh in json.Split('"'))
			{
				if (roh.StartsWith("Assets/", StringComparison.Ordinal))
				{
					pfade.Add(roh);
				}
			}
			int geloescht = 0;
			var fehler = new List<string>();
			foreach (string pfad in pfade)
			{
				if (AssetDatabase.LoadMainAssetAtPath(pfad) == null && !File.Exists(pfad))
				{
					continue;
				}
				if (AssetDatabase.DeleteAsset(pfad))
				{
					geloescht++;
				}
				else
				{
					fehler.Add(pfad);
				}
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			if (fehler.Count > 0)
			{
				Debug.LogError("[ALT] " + fehler.Count + " nicht loeschbar:\n" + string.Join("\n", fehler));
			}
			Debug.Log("[ALT] geloescht: " + geloescht + " von " + pfade.Count + " Eintraegen.");
		}
	}
}
