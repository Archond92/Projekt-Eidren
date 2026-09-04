using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Verbrauchskarte fuer die verbliebenen Ripper-Sammelordner (Aufraeum-
	/// Pruefung 14.08.2026). Erweiterung des AltlastInventar-Musters:
	/// Verbraucher werden PROJEKTWEIT gesucht (GetDependencies deckt auch
	/// Binaerszenen ab), zusaetzlich werden die ProjectSettings-YAMLs per
	/// Textscan auf Kandidaten-GUIDs geprueft — GraphicsSettings/Quality
	/// referenzieren den URP-Renderer, ohne dass ein Asset ihn zieht.
	/// Kandidaten, die nur von ANDEREN KandidATEN gezogen werden, gelten
	/// als unreferenziert (interne Cluster fallen gemeinsam).
	/// </summary>
	public static class RipperRestInventar
	{
		private static readonly string[] KandidatWurzeln =
		{
			"Assets/ComputeShader",
			"Assets/Font",
			"Assets/GameObject",
			"Assets/Material",
			"Assets/Mesh",
			"Assets/MonoBehaviour",
			"Assets/NavMeshData",
			"Assets/Resources",
			"Assets/Sprite",
			"Assets/Texture2D"
		};

		public static void Inventar()
		{
			HashSet<string> kandidaten = new HashSet<string>(
				AssetDatabase.GetAllAssetPaths()
					.Where(p => KandidatWurzeln.Any(w => p.StartsWith(w + "/", StringComparison.Ordinal) || p == w))
					.Where(p => !AssetDatabase.IsValidFolder(p)),
				StringComparer.Ordinal);
			Dictionary<string, List<string>> verbraucher = kandidaten.ToDictionary(k => k, k => new List<string>(), StringComparer.Ordinal);

			// (a) Asset-Referenzen projektweit, Binaerszenen eingeschlossen.
			foreach (string pfad in AssetDatabase.GetAllAssetPaths())
			{
				if (!pfad.StartsWith("Assets/", StringComparison.Ordinal) || kandidaten.Contains(pfad) || AssetDatabase.IsValidFolder(pfad))
				{
					continue;
				}
				foreach (string abhaengig in AssetDatabase.GetDependencies(pfad, recursive: false))
				{
					if (kandidaten.Contains(abhaengig))
					{
						verbraucher[abhaengig].Add(pfad);
					}
				}
			}

			// (b) ProjectSettings: Text-YAMLs auf Kandidaten-GUIDs scannen.
			Dictionary<string, string> guidZuKandidat = kandidaten.ToDictionary(
				k => AssetDatabase.AssetPathToGUID(k), k => k, StringComparer.Ordinal);
			foreach (string einstellung in Directory.GetFiles("ProjectSettings", "*.asset"))
			{
				string text = File.ReadAllText(einstellung);
				foreach (KeyValuePair<string, string> paar in guidZuKandidat)
				{
					if (text.Contains(paar.Key))
					{
						verbraucher[paar.Value].Add(einstellung.Replace('\\', '/'));
					}
				}
			}

			var frei = verbraucher.Where(p => p.Value.Count == 0).Select(p => p.Key).OrderBy(p => p, StringComparer.Ordinal).ToArray();
			var belegt = verbraucher.Where(p => p.Value.Count > 0).OrderBy(p => p.Key, StringComparer.Ordinal).ToArray();
			var zeilen = new List<string>
			{
				"# Ripper-Rest-Inventar " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
				"# Kandidaten: " + kandidaten.Count + " | unreferenziert: " + frei.Length + " | referenziert: " + belegt.Length,
				"",
				"== UNREFERENZIERT (loeschbar nach Sicherung):"
			};
			zeilen.AddRange(frei);
			zeilen.Add("");
			zeilen.Add("== REFERENZIERT (behalten; erster Verbraucher als Beleg):");
			zeilen.AddRange(belegt.Select(p => p.Key + "  <-  " + p.Value[0] + (p.Value.Count > 1 ? $" (+{p.Value.Count - 1})" : "")));
			File.WriteAllLines("ripper-rest-inventar.txt", zeilen);
			Debug.Log($"[RIPPER] Inventar fertig: {kandidaten.Count} Kandidaten, {frei.Length} unreferenziert -> ripper-rest-inventar.txt");
		}
	}
}
