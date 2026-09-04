using Eidren.Data;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
// G-002: streut Bodendecker ueber die Gebietsflaeche.
//
// AreaArtSceneBuilder.BuildDecoration setzt zwoelf Requisiten auf feste
// Positionen, alle im Ring zwischen 20 und 30 Einheiten vom Mittelpunkt. Das
// Innere eines 80x80-Gebiets blieb dadurch vollstaendig leer: eine Bildschirm-
// breite von rund 21 Einheiten passt mehrfach in die Luecke. Abnahmekriterium 5
// verlangt, dass keine bildschirmgrossen leeren Flaechen mehr entstehen.
//
// Diese Klasse ergaenzt die Requisiten, sie ersetzt sie nicht. Gestreut wird
// nur, was AreaArtDecoration.Density groesser null gibt; grosse Requisiten wie
// Baeume und Ruinenwaende stehen weiter ausschliesslich auf ihren gesetzten
// Positionen.
//
// Alles landet unter einem eigenen Knoten "GroundCover", damit der Bewuchs fuer
// die Bodenmessung als Ganzes abschaltbar bleibt (siehe G001VisualAbnahmeRunner,
// Position 04_boden_leer).
public static class AreaArtGroundCoverBuilder
{
	public const string RootName = "GroundCover";

	// Gleiche Saat wie die Weltgenerierung. Der Bewuchs muss zwischen zwei
	// Laeufen pixelgleich sein, sonst ist der Vorher/Nachher-Vergleich der
	// Abnahme wertlos.
	private const int WorldSeed = 20491;

	// Abstand um den Spielerstart, der frei bleibt. Der Start liegt am Rand,
	// und die erste Sicht auf das Gebiet soll nicht verstellt sein.
	private const float SpawnClearance = 6f;

	// Abstand zur Gebietskante. Die Basisflaeche endet dort hart; Bewuchs
	// direkt auf der Kante wuerde ueber den Rand hinausragen.
	private const float EdgeMargin = 3f;

	public static void Build(Transform areaRoot, ZoneAreaArtDefinition areaArt, string key, float size, Vector3 spawnLocal)
	{
		Transform root = new GameObject(RootName).transform;
		root.SetParent(areaRoot, worldPositionStays: false);

		List<AreaArtDecoration> scatterable = new List<AreaArtDecoration>();
		float totalDensity = 0f;
		foreach (AreaArtDecoration decoration in areaArt.Decorations)
		{
			if (decoration.Prefab != null && decoration.Density > 0f)
			{
				scatterable.Add(decoration);
				totalDensity += decoration.Density;
			}
		}
		if (scatterable.Count == 0 || totalDensity <= 0f)
		{
			return;
		}

		// Density ist als Anzahl je 100 Quadrateinheiten definiert. Daraus
		// ergibt sich die Gesamtzahl und daraus die Zellgroesse eines
		// gejitterten Rasters: ein Platz je Zelle. Das Raster haelt den Abstand
		// gleichmaessig, ohne dass Positionen gegeneinander geprueft werden
		// muessen, und der Jitter nimmt ihm das Regelmaessige.
		float area = size * size;
		int target = Mathf.RoundToInt(totalDensity * area / 100f);
		float cell = Mathf.Sqrt(area / Mathf.Max(1, target));
		int cells = Mathf.Max(1, Mathf.FloorToInt(size / cell));

		float half = size * 0.5f;
		float limit = half - EdgeMargin;
		int placed = 0;

		for (int gz = 0; gz < cells; gz++)
		{
			for (int gx = 0; gx < cells; gx++)
			{
				// Der Zufall haengt nur an Gebiet und Zelle, nicht an der
				// Reihenfolge der Schleife. Damit bleibt die Streuung auch dann
				// gleich, wenn sich die Anzahl der Sorten spaeter aendert.
				uint state = Hash(key, gx, gz);

				float jx = NextFloat(ref state);
				float jz = NextFloat(ref state);
				float x = (gx + jx) / cells * size - half;
				float z = (gz + jz) / cells * size - half;

				if (Mathf.Abs(x) > limit || Mathf.Abs(z) > limit)
				{
					continue;
				}
				if (new Vector2(x - spawnLocal.x, z - spawnLocal.z).sqrMagnitude < SpawnClearance * SpawnClearance)
				{
					continue;
				}

				AreaArtDecoration decoration = Pick(scatterable, totalDensity, NextFloat(ref state));
				GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(decoration.Prefab);
				instance.name = $"{key}_Cover_{decoration.Prefab.name}_{placed + 1:000}";
				instance.transform.SetParent(root, worldPositionStays: false);
				instance.transform.localPosition = new Vector3(x, 0.02f, z);
				instance.transform.localRotation = Quaternion.Euler(0f, NextFloat(ref state) * 360f, 0f);

				// Groesse leicht streuen, sonst liest sich die Flaeche als
				// wiederholte Kopie desselben Objekts.
				float scale = Mathf.Lerp(0.75f, 1.25f, NextFloat(ref state));
				instance.transform.localScale = Vector3.one * scale;

				// Bodendecker ist Ausstattung, kein Hindernis: ohne Collider
				// bleiben Bewegung, Kampf, Ernte und Bauraster unberuehrt.
				Collider[] colliders = instance.GetComponentsInChildren<Collider>(includeInactive: true);
				for (int i = 0; i < colliders.Length; i++)
				{
					Object.DestroyImmediate(colliders[i]);
				}
				placed++;
			}
		}

		Debug.Log($"G-002: {key} Bodenbewuchs {placed} Instanzen aus {scatterable.Count} Sorten (Raster {cells}x{cells}).");
	}

	private static AreaArtDecoration Pick(IReadOnlyList<AreaArtDecoration> options, float totalDensity, float roll)
	{
		float cursor = roll * totalDensity;
		for (int i = 0; i < options.Count; i++)
		{
			cursor -= options[i].Density;
			if (cursor <= 0f)
			{
				return options[i];
			}
		}
		return options[options.Count - 1];
	}

	// Eigener Hash statt string.GetHashCode: dessen Ergebnis ist zwischen
	// Laufzeiten nicht garantiert stabil, und die Streuung muss reproduzierbar
	// sein.
	private static uint Hash(string key, int x, int z)
	{
		unchecked
		{
			uint h = 2166136261u;
			for (int i = 0; i < key.Length; i++)
			{
				h = (h ^ key[i]) * 16777619u;
			}
			h = (h ^ (uint)x) * 16777619u;
			h = (h ^ (uint)z) * 16777619u;
			h = (h ^ (uint)WorldSeed) * 16777619u;
			return h;
		}
	}

	private static float NextFloat(ref uint state)
	{
		unchecked
		{
			state ^= state << 13;
			state ^= state >> 17;
			state ^= state << 5;
			return (state & 0xffffff) / (float)0x1000000;
		}
	}
}
}
