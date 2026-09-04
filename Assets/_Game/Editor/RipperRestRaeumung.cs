using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Ausfuehrungsschritt der Ripper-Rest-Raeumung (14.08.2026): arbeitet
	/// die EIDREN_LOESCHLISTE ueber den bewaehrten AltlastLoescher ab und
	/// entfernt danach leere Ordner unter den Sammelwurzeln von unten nach
	/// oben (DeleteAsset raeumt nur Dateien samt .meta — verwaiste
	/// Ordnerskelette wie Resources/art/actors/* blieben sonst stehen).
	/// </summary>
	public static class RipperRestRaeumung
	{
		private static readonly string[] Wurzeln =
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

		public static void Loeschen()
		{
			AltlastLoescher.Loeschen();
			int ordnerEntfernt = 0;
			// Von unten nach oben: tiefste Ordner zuerst, die Wurzeln zuletzt.
			List<string> alleOrdner = Wurzeln
				.Where(Directory.Exists)
				.SelectMany(w => Directory.GetDirectories(w, "*", SearchOption.AllDirectories).Append(w))
				.OrderByDescending(o => o.Count(c => c == '/' || c == '\\'))
				.ToList();
			foreach (string ordner in alleOrdner)
			{
				string normal = ordner.Replace('\\', '/');
				if (Directory.Exists(normal) && Directory.GetFileSystemEntries(normal).Length == 0)
				{
					if (AssetDatabase.DeleteAsset(normal))
					{
						ordnerEntfernt++;
					}
				}
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log($"[RIPPER] Raeumung fertig, {ordnerEntfernt} leere Ordner entfernt.");
		}
	}
}
