using Eidren.Data;
using Eidren.Presentation;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class VisualScaleInventoryReport
{
	private enum ArtClass
	{
		A,
		B,
		C
	}

	public const string ReportPath = "Documentation/Reference/VISUAL_SCALE_INVENTORY.md";

	private const string MissingArtLabel = "„Bild fehlt“";

	[MenuItem("Eidren/Art/Write Visual Scale Inventory")]
	public static void WriteReport()
	{
		VisualScaleTable visualScaleTable = VisualScaleTableBuilder.Load();
		SpriteSilhouetteReader.ClearCaches();
		StringBuilder rows = new StringBuilder();
		List<string> disagreements = new List<string>();
		int classA = 0;
		int classB = 0;
		int classC = 0;
		int flagged = 0;
		foreach (VisualScaleEntry entry in visualScaleTable.Entries)
		{
			SpriteSilhouette silhouette = MeasureCurrent(entry);
			ArtClass verdict = Classify(entry, silhouette);
			switch (verdict)
			{
			case ArtClass.A:
				classA++;
				break;
			case ArtClass.B:
				classB++;
				break;
			default:
				classC++;
				break;
			}
			if (entry.ArtworkMissing)
			{
				flagged++;
			}
			if (verdict != ArtClass.A != entry.ArtworkMissing)
			{
				disagreements.Add($"- `{entry.Id}`: gemessen **{verdict}**, " + "Markierung „Bild fehlt“ steht auf " + $"**{entry.ArtworkMissing}**.");
			}
			rows.AppendLine(Row(entry, silhouette, verdict));
		}
		string document = Document(rows.ToString(), disagreements, classA, classB, classC, flagged);
		Directory.CreateDirectory(Path.GetDirectoryName("Documentation/Reference/VISUAL_SCALE_INVENTORY.md") ?? ".");
		File.WriteAllText("Documentation/Reference/VISUAL_SCALE_INVENTORY.md", document);
		AssetDatabase.ImportAsset("Documentation/Reference/VISUAL_SCALE_INVENTORY.md");
		Debug.Log("Eidren: Bestandsaufnahme geschrieben (Documentation/Reference/VISUAL_SCALE_INVENTORY.md). " + $"A={classA} B={classB} C={classC}, " + string.Format("Markierungen {0}={1}, ", "„Bild fehlt“", flagged) + $"Abweichungen Messung gegen Markierung={disagreements.Count}.");
	}

	private static ArtClass Classify(VisualScaleEntry entry, SpriteSilhouette silhouette)
	{
		if (silhouette.IsEmpty)
		{
			return ArtClass.C;
		}
		float expected = (entry.IsGroundPlane ? entry.WidthBudget : entry.Height);
		if (!(Mathf.Abs(silhouette.WorldHeight - expected) <= expected * 0.01f))
		{
			return ArtClass.B;
		}
		return ArtClass.A;
	}

	private static string Row(VisualScaleEntry entry, SpriteSilhouette silhouette, ArtClass verdict)
	{
		string targetRatio = (entry.IsGroundPlane ? "Bodenebene" : Number((entry.Height <= 0f) ? 0f : (entry.WidthBudget / entry.Height)));
		string currentRatio = (silhouette.IsEmpty ? "—" : Number(silhouette.AspectRatio));
		string currentHeight = (silhouette.IsEmpty ? "—" : Number(silhouette.WorldHeight));
		string targetHeight = (entry.IsGroundPlane ? (Number(entry.WidthBudget) + " (Fläche)") : Number(entry.Height));
		string reason = ((!string.IsNullOrEmpty(entry.Note)) ? entry.Note : ((verdict == ArtClass.A) ? "—" : "siehe Messung"));
		return $"| `{entry.Id}` | {verdict} | {currentRatio} | " + targetRatio + " | " + currentHeight + " | " + targetHeight + " | " + reason + " |";
	}

	private static SpriteSilhouette MeasureCurrent(VisualScaleEntry entry)
	{
		if (VisualScaleAssetMap.BakedPrefabs.TryGetValue(entry.Id, out var prefabPath))
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			if (prefab == null)
			{
				return default(SpriteSilhouette);
			}
			GameObject instance = UnityEngine.Object.Instantiate(prefab);
			try
			{
				SpriteActorPresentation actor = instance.GetComponentInChildren<SpriteActorPresentation>(includeInactive: true);
				if (actor != null)
				{
					return new SpriteSilhouette(entry.WidthBudget, actor.WorldHeight, 1f, 1f, 0f, 0f);
				}
				Renderer[] renderers = FindGeometryRoot(instance.transform).GetComponentsInChildren<Renderer>(includeInactive: true);
				if (renderers.Length == 0)
				{
					return default(SpriteSilhouette);
				}
				Bounds bounds = renderers[0].bounds;
				for (int index = 1; index < renderers.Length; index++)
				{
					bounds.Encapsulate(renderers[index].bounds);
				}
				float horizontal = Mathf.Max(bounds.size.x, bounds.size.z);
				float measured = (entry.IsGroundPlane ? horizontal : bounds.size.y);
				return new SpriteSilhouette(horizontal, measured, 1f, 1f, 0f, 0f);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(instance);
			}
		}
		return default(SpriteSilhouette);
	}

	private static Transform FindGeometryRoot(Transform root)
	{
		Transform direct = root.Find("Geometry_A14");
		if (direct != null)
		{
			return direct;
		}
		foreach (Transform child in root)
		{
			Transform nested = FindGeometryRoot(child);
			if (nested != child)
			{
				return nested;
			}
			if (child.name == "Geometry_A14")
			{
				return child;
			}
		}
		return root;
	}

	private static string Number(float value)
	{
		return value.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');
	}

	private static string Document(string rows, IReadOnlyList<string> disagreements, int classA, int classB, int classC, int flagged)
	{
		StringBuilder document = new StringBuilder();
		document.AppendLine("# Bestandsaufnahme der Objektgrößen");
		document.AppendLine();
		document.AppendLine("**Erzeugt** von `Eidren/Art/Write Visual Scale Inventory`. Nicht von Hand pflegen.");
		document.AppendLine();
		document.AppendLine("Finaler Stand von Version 0.1 vom 1. August 2026. Die Zielwerte stehen in **M10.3**, die Renderer-Messung in **M10.8/M10.9**.");
		document.AppendLine();
		document.AppendLine("## Klassen");
		document.AppendLine();
		document.AppendLine("- **A** — Geometrie trifft die Tabellenhöhe.");
		document.AppendLine("- **B** — Renderer-Ausdehnung weicht von der Tabelle ab.");
		document.AppendLine("- **C** — Kein Renderer vorhanden.");
		document.AppendLine();
		document.AppendLine($"A: **{classA}** · B: **{classB}** · C: **{classC}** · " + string.Format("Zeilen mit Markierung {0}: **{1}**", "„Bild fehlt“", flagged));
		document.AppendLine();
		document.AppendLine("Bei 3D-Weltobjekten stammen Breite und Höhe aus der vereinigten sichtbaren Renderer-Ausdehnung. Bei 2D-Akteuren ist die im Prefab persistierte WorldHeight maßgeblich (M10.2). Für Gebäude ist das Soll-Verhältnis zugleich die Grenze ihres Baufelds.");
		document.AppendLine();
		document.AppendLine("| Kennung | Klasse | Ist-Verh. | Soll-Verh. | Höhe heute | Höhe Ziel | Begründung |");
		document.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
		document.Append(rows);
		document.AppendLine();
		document.AppendLine("## Messung gegen Markierung");
		document.AppendLine();
		if (disagreements.Count == 0)
		{
			document.AppendLine("Keine Abweichung. Alle Tabellenzeilen besitzen passende Renderer; die historische Markierung „Bild fehlt“ bleibt vollständig abgebaut.");
		}
		else
		{
			document.AppendLine("Die Messung widerspricht der Tabellenmarkierung. Jede Zeile hier ist vor dem nächsten Weltasset-Commit zu klären:");
			document.AppendLine();
			foreach (string line in disagreements)
			{
				document.AppendLine(line);
			}
		}
		document.AppendLine();
		document.AppendLine("*Erzeugt am " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + ".*");
		return document.ToString();
	}
}
}
