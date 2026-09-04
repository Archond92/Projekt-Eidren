using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eidren.Editor
{
	/// <summary>
	/// Verbrauchskarte fuer die 2D-Altlasten: Wer referenziert die Kandidaten
	/// unter Resources/Art/Actors und Resources/Prefabs/Actors/{2D,Forge}?
	/// Zwei Kanaele, die Textsuche nicht abdeckt: (a) GUID-Referenzen aus
	/// ALLEN Assets inklusive der BINAER serialisierten Szenen (via
	/// AssetDatabase.GetDependencies), (b) serialisierte String-Ressourcen-
	/// pfade in Szenenkomponenten (GroundShadow/Vfx laden per Resources.Load;
	/// dafuer werden alle zwoelf Szenen geoeffnet und die String-Properties
	/// aller MonoBehaviours durchsucht). Nur lesen, nichts aendern.
	/// </summary>
	public static class AltlastInventar
	{
		private static readonly string[] KandidatWurzeln =
		{
			"Assets/_Game/Resources/Art/Actors",
			"Assets/_Game/Resources/Prefabs/Actors/2D",
			"Assets/_Game/Resources/Prefabs/Actors/Forge"
		};

		public static void Inventar()
		{
			string[] kandidaten = AssetDatabase.FindAssets("", KandidatWurzeln)
				.Select(AssetDatabase.GUIDToAssetPath)
				.Distinct()
				.Where(p => !AssetDatabase.IsValidFolder(p))
				.OrderBy(p => p, StringComparer.Ordinal)
				.ToArray();
			var verbraucher = new Dictionary<string, List<string>>(StringComparer.Ordinal);
			foreach (string k in kandidaten)
			{
				verbraucher[k] = new List<string>();
			}

			// (a) GUID-Referenzen aus allen Projekt-Assets (auch Binaerszenen).
			foreach (string pfad in AssetDatabase.GetAllAssetPaths())
			{
				if (!pfad.StartsWith("Assets/_Game", StringComparison.Ordinal))
				{
					continue;
				}
				if (kandidaten.Contains(pfad))
				{
					continue;
				}
				foreach (string dep in AssetDatabase.GetDependencies(pfad, false))
				{
					if (verbraucher.TryGetValue(dep, out List<string> liste))
					{
						liste.Add(pfad);
					}
				}
			}

			// (b) String-Ressourcenpfade in den Szenen.
			var szenenStrings = new List<string>();
			foreach (string szenenPfad in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game/Scenes" })
				.Select(AssetDatabase.GUIDToAssetPath))
			{
				Scene szene = EditorSceneManager.OpenScene(szenenPfad, OpenSceneMode.Single);
				foreach (GameObject wurzel in szene.GetRootGameObjects())
				{
					foreach (MonoBehaviour mb in wurzel.GetComponentsInChildren<MonoBehaviour>(true))
					{
						if (mb == null)
						{
							continue;
						}
						var so = new SerializedObject(mb);
						SerializedProperty prop = so.GetIterator();
						while (prop.NextVisible(true))
						{
							if (prop.propertyType == SerializedPropertyType.String
								&& prop.stringValue != null
								&& (prop.stringValue.Contains("Art/Actors")
									|| prop.stringValue.Contains("Prefabs/Actors")))
							{
								szenenStrings.Add(szene.name + " | " + mb.GetType().Name
									+ "." + prop.propertyPath + " = " + prop.stringValue);
							}
						}
					}
				}
			}

			foreach (string k in kandidaten)
			{
				if (k.EndsWith(".meta", StringComparison.Ordinal))
				{
					continue;
				}
				List<string> liste = verbraucher[k];
				Debug.Log("[ALT] " + ((liste.Count == 0) ? "FREI  " : "BELEGT") + " " + k
					+ ((liste.Count > 0) ? ("  <- " + string.Join(" ; ", liste.Distinct().Take(6))) : ""));
			}
			Debug.Log("[ALT] Szenen-Stringpfade (" + szenenStrings.Count + "):");
			foreach (string s in szenenStrings.Distinct())
			{
				Debug.Log("[ALT]   " + s);
			}
			Debug.Log("[ALT] Inventar fertig: " + kandidaten.Length + " Kandidaten.");
		}
	}
}
