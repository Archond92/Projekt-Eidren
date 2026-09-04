using Eidren.Data;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class VisualScaleTableBuilder
{
	public const string AssetPath = "Assets/_Game/Resources/Data/VisualScaleTable.asset";

	private const float BuildCell = 1f;

	private const float ColliderGap = 0.1f;

	private const string VisualIdPrefix = "visual.";

	private const string BuildingCatalogPath = "Assets/_Game/Resources/Data/BuildingCatalog_V01.asset";

	[MenuItem("Eidren/Art/Build Visual Scale Table")]
	public static void BuildTable()
	{
		VisualScaleTable table = AssetDatabase.LoadAssetAtPath<VisualScaleTable>("Assets/_Game/Resources/Data/VisualScaleTable.asset");
		if (table == null)
		{
			EnsureFolder();
			table = ScriptableObject.CreateInstance<VisualScaleTable>();
			AssetDatabase.CreateAsset(table, "Assets/_Game/Resources/Data/VisualScaleTable.asset");
		}
		VisualScaleEntry[] entries = BuildEntries();
		MeasureRuntimeArtFrames(entries);
		table.SetEntries(entries);
		EditorUtility.SetDirty(table);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		string[] errors = table.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException("Die Groessentabelle ist ungueltig: " + string.Join("; ", errors));
		}
		int missing = 0;
		foreach (VisualScaleEntry entry in table.Entries)
		{
			if (entry.ArtworkMissing)
			{
				missing++;
			}
		}
		Debug.Log($"Eidren: Groessentabelle mit {table.Entries.Count} Zeilen " + $"geschrieben, davon {missing} mit fehlendem Bild (M10.7).");
	}

	public static VisualScaleTable Load()
	{
		VisualScaleTable visualScaleTable = AssetDatabase.LoadAssetAtPath<VisualScaleTable>("Assets/_Game/Resources/Data/VisualScaleTable.asset");
		if (visualScaleTable == null)
		{
			throw new FileNotFoundException("Die Groessentabelle fehlt unter Assets/_Game/Resources/Data/VisualScaleTable.asset. Menuepunkt 'Eidren/Art/Build Visual Scale Table'.");
		}
		return visualScaleTable;
	}

	private static void MeasureRuntimeArtFrames(VisualScaleEntry[] entries)
	{
		foreach (VisualScaleEntry entry in entries)
		{
			if (VisualScaleAssetMap.RuntimeArt.TryGetValue(entry.Id, out var resourcePath))
			{
				string assetPath = "Assets/_Game/Resources/" + resourcePath + ".png";
				SpriteSilhouetteReader.TextureFill fill = SpriteSilhouetteReader.MeasureTextureFile(assetPath);
				if (fill.IsEmpty)
				{
					throw new InvalidOperationException("'" + entry.Id + "': Bild '" + assetPath + "' fehlt oder ist vollstaendig transparent. Ohne Messung gibt es keine Rahmenhoehe (M10.2).");
				}
				entry.SetMeasuredArtFrame(fill.FrameHeightFor(entry.Height), fill.BottomFraction);
			}
		}
	}

	private static VisualScaleEntry[] BuildEntries()
	{
		List<VisualScaleEntry> obj = new List<VisualScaleEntry>
		{
			Billboard("visual.player", 2f, Upright(2f), new Vector3(0.96f, 2f, 0.96f), artworkMissing: false, "Anker. CharacterController 2,0 / 0,48 bleibt."),
			Billboard("visual.wildling", 1.9f, Wide(1.9f), new Vector3(1.04f, 1.9f, 1.04f), artworkMissing: false, "Gebueckt, deshalb breiter als hoch gerechnet."),
			Billboard("visual.eidra_small", 1f, Wide(1f), default(Vector3), artworkMissing: false, "Vierbeinige Begleitgroesse. Terrock und Noctarion."),
			Billboard("visual.boss_garon", 4.5f, Wide(4.5f), default(Vector3), artworkMissing: false, "Koloss-Schildkroete. Telegraphen gegenpruefen (M1.8)."),
			Billboard("visual.tree.active", 6f, Tree(6f), new Vector3(1.2f, 5.4f, 1.2f), artworkMissing: false, "Fabrik-Geometrie seit Etappe 3b (T1ResourceVisualBuilder). F31-001: Collider bis in die Krone — die Sichtlinien-Ausblendung trifft sonst nur den 2,4-m-Stamm, waehrend die 6-m-Krone die Figur verdeckt; fuers Laufen zaehlt ohnehin nur der Radius."),
			Billboard("visual.tree.exhausted", 0.7f, Remains(0.7f), default(Vector3), artworkMissing: false, "Fabrik-Geometrie seit Etappe 3b (T1ResourceVisualBuilder)."),
			Billboard("visual.berry_bush.active", 0.9f, Wide(0.9f), default(Vector3), artworkMissing: false, "Referenz fuer richtigen Blickwinkel (M10.7)."),
			Billboard("visual.berry_bush.exhausted", 0.7f, Wide(0.7f), default(Vector3), artworkMissing: false, "Abgeerntet bleibt der Strauch stehen."),
			Billboard("visual.fiber_plant.active", 0.9f, Wide(0.9f), default(Vector3), artworkMissing: false, "Auf 45 Prozent der Spielerhoehe angehoben, damit die duenne Silhouette im Laufbild klar lesbar bleibt."),
			Billboard("visual.fiber_plant.exhausted", 0.25f, Remains(0.25f), default(Vector3), artworkMissing: false, "Fabrik-Geometrie seit Etappe 3b (T1ResourceVisualBuilder)."),
			Billboard("visual.stone_deposit.active", 1.1f, Rock(1.1f), new Vector3(1.4f, 1f, 1.4f)),
			Billboard("visual.stone_deposit.exhausted", 0.5f, Remains(0.5f), default(Vector3), artworkMissing: false, "Fabrik-Geometrie seit Etappe 3b (T1ResourceVisualBuilder)."),
			Billboard("visual.copper_vein.active", 1.3f, Rock(1.3f), new Vector3(1.6f, 1.2f, 1.6f)),
			Billboard("visual.copper_vein.exhausted", 0.581f, Rock(0.581f), new Vector3(1.6f, 0.7f, 1.6f), artworkMissing: false, "F31-003: flacher Verbraucht-Rest wie beim Stein — der fruehere volle Wirtsfels-Cluster war vom aktiven Zustand nicht unterscheidbar."),
			Billboard("visual.hardwood_tree.active", 6.65f, Tree(6.65f), new Vector3(1.5f, 6f, 1.5f), artworkMissing: false, "T2-Hartholz: breiter alter Stamm, dunkle Krone. F31-001: Collider bis in die Krone (siehe Baum)."),
			Billboard("visual.hardwood_tree.exhausted", 0.84f, Remains(0.84f)),
			Billboard("visual.swamp_hemp.active", 1.372f, Wide(1.372f)),
			Billboard("visual.swamp_hemp.exhausted", 0.228f, Remains(0.228f)),
			Billboard("visual.granite_deposit.active", 1.266f, Rock(1.266f), new Vector3(1.9f, 1.35f, 1.9f)),
			Billboard("visual.granite_deposit.exhausted", 0.749f, Remains(0.749f)),
			Billboard("visual.iron_vein.active", 1.408f, Rock(1.408f), new Vector3(1.9f, 1.35f, 1.9f)),
			Billboard("visual.iron_vein.exhausted", 0.749f, Rock(0.749f), new Vector3(1.7f, 1f, 1.7f))
		};
		AddBuildings(obj);
		AddProps(obj);
		obj.Add(Billboard("visual.world_item", 0.4f, Wide(0.4f), default(Vector3), artworkMissing: false, "Ein gemeinsamer Mid-Poly-Lootbeutel fuer alle Drops; Gegenstandsicon und Menge bleiben als eindeutige Inhaltsanzeige erhalten. Drei LOD-Stufen, exakte Sichthoehe 0,40 m."));
		obj.Add(Billboard("visual.death_bag", 0.7f, Wide(0.7f), default(Vector3), artworkMissing: false, "Ein fester grosser Mid-Poly-Todesbeutel als wiedererkennbarer Rueckholpunkt. Drei LOD-Stufen, exakte Sichthoehe 0,70 m; aus etwa 15 Einheiten auffindbar (M4)."));
		return obj.ToArray();
	}

	private static void AddBuildings(List<VisualScaleEntry> entries)
	{
		entries.Add(Building("visual.building.wall", 2.6f, artworkMissing: true, "Ueberragt den Spieler. Bild fehlt: quadratisch, gebraucht 1:2,6."));
		entries.Add(Building("visual.building.door", 2.6f, artworkMissing: true, "Durchgang 2,0 hoch. Bild fehlt: quadratisch."));
		float floorWidth = FootprintWidth("visual.building.floor");
		entries.Add(GroundPlane("visual.building.floor", floorWidth, new Vector3(floorWidth - 0.1f, 0.15f, floorWidth - 0.1f), artworkMissing: true, "Bild fehlt: braucht eine Draufsicht. Heute ein 2,8 hohes stehendes Billboard (M10.4)."));
		float farmWidth = FootprintWidth("visual.building.farm_plot");
		entries.Add(GroundPlane("visual.building.farm_plot", farmWidth, new Vector3(farmWidth - 0.1f, 0.15f, farmWidth - 0.1f), artworkMissing: true, "Grundflaeche aus dem Katalog (M6). Bild fehlt: braucht eine Draufsicht."));
		entries.Add(Building("visual.building.cooking_pot", 1f));
		entries.Add(Building("visual.building.smelter", 2f, artworkMissing: true, "Bild fehlt: quadratisch, gebraucht 1:2,0."));
		entries.Add(Building("visual.building.sawmill", 2.2f, artworkMissing: true, "Bild fehlt: quadratisch, gebraucht 1:2,2."));
		entries.Add(Building("visual.building.ropewalk", 1.6f, artworkMissing: true, "Bild fehlt: quadratisch, gebraucht 1:1,6."));
		entries.Add(Building("visual.building.stonecutter", 1.4f, artworkMissing: true, "Bild fehlt: quadratisch, gebraucht 1:1,4."));
		entries.Add(Building("visual.building.workbench", 1.1f, artworkMissing: true, "Arbeitshoehe. Bild fehlt: heute Primitivgeometrie."));
		entries.Add(Building("visual.building.storage_chest", 0.8f, artworkMissing: true, "Bild fehlt: heute Primitivgeometrie."));
	}

	private static void AddProps(List<VisualScaleEntry> entries)
	{
		entries.Add(Billboard("visual.prop.tree_a", 7.5f, Tree(7.5f), new Vector3(0.9f, 3f, 0.9f), artworkMissing: true, "Bild fehlt: Seitenansicht statt Kamerawinkel (M10.7)."));
		entries.Add(Billboard("visual.prop.tree_b", 6f, Tree(6f), new Vector3(0.8f, 2.4f, 0.8f), artworkMissing: true, "Gleiche Hoehe wie der Holzknoten. Bild fehlt: Seitenansicht statt Kamerawinkel."));
		entries.Add(Billboard("visual.prop.tree_c", 4f, Tree(4f), new Vector3(0.7f, 1.6f, 0.7f), artworkMissing: true, "Bild fehlt: Seitenansicht statt Kamerawinkel."));
		entries.Add(Billboard("visual.prop.rock_large", 2.4f, Rock(2.4f), new Vector3(2.2f, 1.8f, 2.2f)));
		entries.Add(Billboard("visual.prop.rock_medium", 1.1f, Rock(1.1f), new Vector3(1.3f, 0.8f, 1.3f), artworkMissing: false, "Gleiche Hoehe wie das Steinvorkommen (M10.3)."));
		entries.Add(Billboard("visual.prop.rock_small", 0.5f, Rock(0.5f), new Vector3(0.8f, 0.4f, 0.8f)));
		entries.Add(Billboard("visual.prop.bush", 0.9f, Wide(0.9f), default(Vector3), artworkMissing: false, "Gleiche Hoehe wie der Beerenstrauch (M10.3)."));
		entries.Add(Billboard("visual.prop.fern", 0.7f, Wide(0.7f)));
		entries.Add(Billboard("visual.prop.flowers", 0.4f, Wide(0.4f)));
		entries.Add(GroundPlane("visual.prop.ground_grass", 1.2f, default(Vector3), artworkMissing: true, "Bild fehlt: braucht eine Draufsicht, heute ein aufrechtes Bueschel (M10.4)."));
		entries.Add(GroundPlane("visual.prop.ground_moss", 1.2f, default(Vector3), artworkMissing: true, "Bild fehlt: braucht eine Draufsicht."));
		entries.Add(Billboard("visual.prop.glow_mushrooms", 0.35f, Wide(0.35f)));
		entries.Add(Billboard("visual.prop.ruin_wall_a", 2.6f, Ruin(2.6f), new Vector3(2.6f, 2.2f, 0.8f), artworkMissing: false, "Ruinenwand = Mauer (M10.3)."));
		entries.Add(Billboard("visual.prop.ruin_wall_b", 2.6f, Ruin(2.6f), new Vector3(2f, 2.2f, 0.8f)));
		entries.Add(Billboard("visual.prop.ruin_monument", 5f, Ruin(5f), new Vector3(1.4f, 3f, 1.4f)));
		entries.Add(Billboard("visual.prop.rune", 0.8f, Ruin(0.8f)));
	}

	private static float Upright(float height)
	{
		return height * 0.9f;
	}

	private static float Wide(float height)
	{
		return height * 1.4f;
	}

	private static float Tree(float height)
	{
		return height * 0.7f;
	}

	private static float Rock(float height)
	{
		return height * 1.6f;
	}

	private static float Ruin(float height)
	{
		return height * 1.2f;
	}

	private static float Remains(float height)
	{
		return height * 2.2f;
	}

	private static VisualScaleEntry Billboard(string id, float height, float widthBudget, Vector3 colliderSize = default(Vector3), bool artworkMissing = false, string note = "")
	{
		return new VisualScaleEntry(id, VisualPlacement.Billboard, height, widthBudget, colliderSize, artworkMissing: false, artworkMissing ? "Persistierte 3D-Geometrie." : note);
	}

	private static VisualScaleEntry Building(string id, float height, bool artworkMissing = false, string note = "")
	{
		float width = FootprintWidth(id);
		return new VisualScaleEntry(id, VisualPlacement.Billboard, height, width, new Vector3(width - 0.1f, height, width - 0.1f), artworkMissing: false, artworkMissing ? "Persistierte 3D-Geometrie." : note);
	}

	private static VisualScaleEntry GroundPlane(string id, float extent, Vector3 colliderSize = default(Vector3), bool artworkMissing = false, string note = "")
	{
		return new VisualScaleEntry(id, VisualPlacement.GroundPlane, 0f, extent, colliderSize, artworkMissing: false, artworkMissing ? "Persistierte 3D-Geometrie." : note);
	}

	private static float FootprintWidth(string visualId)
	{
		string buildingId = (visualId.StartsWith("visual.", StringComparison.Ordinal) ? visualId.Substring("visual.".Length) : visualId);
		BuildingCatalogDefinition buildingCatalogDefinition = AssetDatabase.LoadAssetAtPath<BuildingCatalogDefinition>("Assets/_Game/Resources/Data/BuildingCatalog_V01.asset");
		if (buildingCatalogDefinition == null)
		{
			throw new FileNotFoundException("Gebaeudekatalog fehlt unter Assets/_Game/Resources/Data/BuildingCatalog_V01.asset.");
		}
		foreach (BuildingCostDefinition building in buildingCatalogDefinition.Buildings)
		{
			if (!(building == null) && string.Equals(building.Id, buildingId, StringComparison.Ordinal))
			{
				if (building.LevelFootprints.Count == 0)
				{
					throw new InvalidOperationException("'" + buildingId + "' hat keine Grundflaeche fuer Stufe 1.");
				}
				return (float)building.LevelFootprints[0].Width * 1f;
			}
		}
		throw new InvalidOperationException("Gebaeude '" + buildingId + "' steht nicht im Katalog. Ohne Grundflaeche gibt es kein Breitenbudget (M10.7).");
	}

	private static void EnsureFolder()
	{
		string folder = Path.GetDirectoryName("Assets/_Game/Resources/Data/VisualScaleTable.asset")?.Replace('\\', '/');
		if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
		{
			AssetDatabase.CreateFolder(Path.GetDirectoryName(folder)?.Replace('\\', '/'), Path.GetFileName(folder));
		}
	}
}
}
