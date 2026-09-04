using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/* G-004 Task 1: misst die zusammengefassten Rendererbounds jedes Weltprefabs.
	   Grundlage fuer den Hoehenschwellwert der Aufnahmeregel — gemessen statt
	   geraten. Die Ordnerliste ist zugleich die Wahrheitsquelle fuer
	   GroundContactTests; eine Liste, nicht zwei. */
	public static class GroundContactAudit
	{
		public static readonly string[] Ordner =
		{
			"Assets/_Game/Prefabs/Resources/Visuals",
			"Assets/_Game/Prefabs/Environment/AreaArtVariants",
			"Assets/_Game/Prefabs/Environment/StyleProof",
			"Assets/_Game/Prefabs/Buildings/Level01",
			"Assets/_Game/Prefabs/Loot/WorldChests",
			"Assets/_Game/Prefabs/Containers/Forge",
			"Assets/_Game/Prefabs/Stations"
		};

		[MenuItem("Eidren/V0.2/G004/Hoehenbericht schreiben")]
		public static void SchreibeHoehenbericht()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Ordner;Prefab;HoeheY;BreiteX;TiefeZ;Renderer");
			int gezaehlt = 0;
			foreach (string ordner in Ordner)
			{
				if (!Directory.Exists(ordner))
				{
					Debug.LogWarning("[G004] Ordner fehlt: " + ordner);
					continue;
				}
				foreach (string datei in Directory.GetFiles(ordner, "*.prefab"))
				{
					string pfad = datei.Replace('\\', '/');
					GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pfad);
					if (prefab == null)
					{
						Debug.LogWarning("[G004] Prefab nicht ladbar: " + pfad);
						continue;
					}
					// NUR MeshRenderer: Das Kontaktdecal erdet die feste Geometrie.
					// Die drei Weltkisten tragen zusaetzlich die Sprite-Glyphen
					// OpeningHand_Left/Right (F-005); die blaehen die Bounds auf
					// 11,4 x 6,15 x 8,26 auf, obwohl die Kiste laut F-010 nur
					// 1,15 x 0,54 x 0,72 misst. Sprites und Partikel bleiben draußen.
					Renderer[] renderer = prefab.GetComponentsInChildren<MeshRenderer>(true);
					gezaehlt++;
					if (renderer.Length == 0)
					{
						sb.AppendLine($"{ordner};{Path.GetFileNameWithoutExtension(pfad)};0.000;0.000;0.000;0");
						continue;
					}
					Bounds b = renderer[0].bounds;
					for (int i = 1; i < renderer.Length; i++)
					{
						b.Encapsulate(renderer[i].bounds);
					}
					sb.AppendLine($"{ordner};{Path.GetFileNameWithoutExtension(pfad)};" +
						$"{b.size.y:F3};{b.size.x:F3};{b.size.z:F3};{renderer.Length}");
				}
			}
			File.WriteAllText("g004-hoehen.csv", sb.ToString(), Encoding.UTF8);
			Debug.Log($"[G004] Hoehenbericht: {gezaehlt} Prefabs nach g004-hoehen.csv");
		}

		public static void SchreibeHoehenberichtFuerAutomation()
		{
			SchreibeHoehenbericht();
		}
	}
}
