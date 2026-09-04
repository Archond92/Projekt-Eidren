using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
/* Stilumbau Etappe 5, Task 4: baut NUR die Geometry_A14-Kindhierarchie der 9 bestehenden
   BLD_-Prefabs neu (in-place ueberschrieben an denselben Level01-Pfaden), ruehrt Collider,
   NavMeshObstacle, WallConnectionView/Joint_-/Cap_-Kinder, OcclusionFadeTarget,
   WorkbenchController/FarmPlotController und BuildingInstanceView NICHT an -- diese bleiben
   Kinder/Komponenten derselben Prefab-Wurzel (siehe STILUMBAU_E5_BAKEKETTE.md (a)/(f)). Muss
   VOR WallConnectionContentBuilder.ApplyPieces() laufen (BAKEKETTE (f) Rebuild-Reihenfolge). */
internal static class EidrenBuildingVisualBuilder
{
	private const string BuildingFolder = "Assets/_Game/Prefabs/Buildings/Level01";

	private const string MeshRoot = "Assets/_Game/Art/Buildings/Meshes";

	private static int _meshSequenz;

	/* Palette aus den Materialfarben der Alt-Gebaeude ausgelesen (Task 4; GUID-Aufloesung ueber
	   die m_Materials-Eintraege der 9 BLD_*_L01.prefab-Dateien gegen die .meta-Dateien unter
	   Assets/_Game/Art/Geometry/Materials/WorldRefined und .../CookingPotHero; Werte + Belegpfade
	   in der Palettentabelle des Task-3+4-Reports dokumentiert). */
	private static readonly Color Holz = new Color(0.33f, 0.16f, 0.07f);            // M_World_Wood
	private static readonly Color HolzHell = new Color(0.56f, 0.31f, 0.13f);        // M_World_WoodLight
	private static readonly Color HolzDunkel = new Color(0.19f, 0.075f, 0.027f);    // M_World_WoodDark
	private static readonly Color Stein = new Color(0.42f, 0.46f, 0.45f);           // M_World_Stone
	private static readonly Color SteinHell = new Color(0.58f, 0.58f, 0.51f);       // M_World_StoneLight
	private static readonly Color SteinDunkel = new Color(0.25f, 0.28f, 0.28f);     // M_World_StoneDark
	private static readonly Color Metall = new Color(0.2f, 0.24f, 0.25f);           // M_World_Metal
	private static readonly Color Eisenkante = new Color(0.12f, 0.15f, 0.15f);      // M_World_IronEdge
	private static readonly Color Kupfer = new Color(0.76f, 0.33f, 0.13f);          // M_World_Copper
	private static readonly Color Leder = new Color(0.31f, 0.105f, 0.035f);         // M_World_Leather
	private static readonly Color Seil = new Color(0.68f, 0.5f, 0.22f);             // M_World_Rope
	private static readonly Color Erde = new Color(0.25f, 0.12f, 0.055f);           // M_World_Soil
	private static readonly Color Saat = new Color(0.28f, 0.46f, 0.2f);             // Aussaat-Akzent (analog StyleProof-Laub, kein direktes Alt-Material)
	private static readonly Color TopfEisen = new Color(0.075f, 0.1f, 0.11f);       // M_CookingPot_Iron
	private static readonly Color TopfKupfer = new Color(0.67f, 0.24f, 0.065f);     // M_CookingPot_Copper
	private static readonly Color TopfHolz = new Color(0.42f, 0.21f, 0.075f);       // M_CookingPot_WoodLight
	private static readonly Color TopfStein = new Color(0.48f, 0.46f, 0.39f);       // M_CookingPot_StoneWarm
	private static readonly Color TopfInhalt = new Color(0.72f, 0.31f, 0.065f);     // M_CookingPot_Stew
	private static readonly Color TopfAsche = new Color(0.075f, 0.07f, 0.065f);     // M_CookingPot_Ash
	private static readonly Color TopfGlut = new Color(0.94f, 0.28f, 0.045f);       // M_CookingPot_Ember
	private static readonly Color SchmelzeGlut = new Color(0.88f, 0.31f, 0.075f);   // Plan-Vorgabe Task 4 (nicht materialextrahiert)

	/* Schmaler Einstieg: baut NUR die Geometry_A14-Kindhierarchie der 9 bestehenden BLD_-Prefabs
	   neu, ruehrt Kosten-/Vorschau-/Dach-/Wandverbinder-Ketten nicht an (die laufen separat,
	   siehe BAKEKETTE (f) Rebuild-Reihenfolge). */
	[MenuItem("Eidren/V0.2/Stilumbau/Gebaeude bauen")]
	public static void BuildStandalone()
	{
		Build();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	internal static void Build()
	{
		EnsureFolder("Assets/_Game/Art/Buildings", "Meshes");
		_meshSequenz = 0;
		if (!MidpolyBaseModuleMigration.IsApprovedModule("Wall")) BuildWall();
		if (!MidpolyBaseModuleMigration.IsApprovedModule("Door")) BuildDoor();
		if (!MidpolyBaseModuleMigration.IsApprovedModule("Floor")) BuildFloor();
		if (!MidpolyProductionStationMigration.IsApprovedStation("FarmPlot")) BuildFarmPlot();
		if (!MidpolyProductionStationMigration.IsApprovedStation("CookingPot")) BuildCookingPot();
		if (!MidpolyProductionStationMigration.IsApprovedStation("Sawmill")) BuildSawmill();
		if (!MidpolyProductionStationMigration.IsApprovedStation("Smelter")) BuildSmelter();
		if (!MidpolyProductionStationMigration.IsApprovedStation("Stonecutter")) BuildStonecutter();
		if (!MidpolyProductionStationMigration.IsApprovedStation("Ropewalk")) BuildRopewalk();
	}

	/* Laedt das bestehende Prefab, ersetzt NUR das Kind "Geometry_A14" durch frisch gebaute
	   Fabrik-Geometrie, laesst Wurzel-BoxCollider/NavMeshObstacle/Joint_-Cap_-Kinder/Komponenten
	   unberuehrt (BAKEKETTE (a) Vorschlag, woertlich uebernommen). */
	private static void RebuildGeometry(string buildingName, Action<Transform> baueTeile)
	{
		string path = BuildingFolder + "/BLD_" + buildingName + "_L01.prefab";
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		try
		{
			Transform alt = root.transform.Find("Geometry_A14");
			if (alt != null)
			{
				UnityEngine.Object.DestroyImmediate(alt.gameObject);
			}
			GameObject geometrie = new GameObject("Geometry_A14");
			geometrie.transform.SetParent(root.transform, worldPositionStays: false);
			baueTeile(geometrie.transform);
			int dreiecke = 0;
			int vertices = 0;
			MeshFilter[] alleTeile = geometrie.GetComponentsInChildren<MeshFilter>(includeInactive: true);
			foreach (MeshFilter filter in alleTeile)
			{
				dreiecke += filter.sharedMesh.triangles.Length / 3;
				vertices += filter.sharedMesh.vertexCount;
			}
			Debug.Log($"[StilE5] BLD_{buildingName}_L01: {dreiecke} Dreiecke, {vertices} Vertices, {alleTeile.Length} Teile");
			// G-004: Bodenerdung als Bauteil, nicht als Nachlauf.
			WorldContactShadowBuilder.Attach(root);
			PrefabUtility.SaveAsPrefabAsset(root, path);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	/* Persist mit Verwaisungsschutz, Muster wie StyleProofPropBuilder.Persist/T1ResourceVisualBuilder.Persist. */
	private static Mesh Persist(Mesh mesh, string teilName)
	{
		string pfad = string.Format("{0}/BLD_{1:000}_{2}.asset", MeshRoot, _meshSequenz, teilName);
		string muster = string.Format("BLD_{0:000}_", _meshSequenz);
		_meshSequenz++;
		foreach (string vorhanden in AssetDatabase.FindAssets("t:Mesh", new[] { MeshRoot }))
		{
			string vorhandenerPfad = AssetDatabase.GUIDToAssetPath(vorhanden);
			if (System.IO.Path.GetFileName(vorhandenerPfad).StartsWith(muster, StringComparison.Ordinal) && vorhandenerPfad != pfad)
			{
				AssetDatabase.DeleteAsset(vorhandenerPfad);
			}
		}
		Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(pfad);
		if (existing != null)
		{
			EditorUtility.CopySerialized(mesh, existing);
			UnityEngine.Object.DestroyImmediate(mesh);
			EditorUtility.SetDirty(existing);
			return existing;
		}
		AssetDatabase.CreateAsset(mesh, pfad);
		return mesh;
	}

	private static GameObject Teil(Transform parent, string name, Mesh mesh, Vector3 position, Vector3 euler)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.localPosition = position;
		gameObject.transform.localEulerAngles = euler;
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.AddComponent<MeshRenderer>().sharedMaterial = EidrenWorldStyleAssets.EnsureWorldMaterial();
		return gameObject;
	}

	/* BLD_Wall_L01: Plankenlagen + Fachwerk-Streben + Rahmen/Riegel/Kappe/Sockel (12 Teile).
	   Sollhoehe 2,6 m (Wurzel-Kappe endet exakt bei y=2,6), Breitenbudget 1,0 m (Rahmenposten bei
	   x=+-0,47 mit 0,06 m Breite ergeben Extent exakt 1,0 m). */
	private static void BuildWall()
	{
		RebuildGeometry("Wall", geometrie =>
		{
			for (int index = 0; index < 5; index++)
			{
				float x = -0.4f + (float)index * 0.2f;
				Teil(geometrie, $"Plank_{index}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.19f, 2.48f, 0.06f), 1f, Holz), "Wand_Planke"), new Vector3(x, 0f, 0f), Vector3.zero);
			}
			Teil(geometrie, "RahmenL", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, 2.48f, 0.1f), 1f, HolzDunkel), "Wand_RahmenL"), new Vector3(-0.47f, 0f, 0f), Vector3.zero);
			Teil(geometrie, "RahmenR", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, 2.48f, 0.1f), 1f, HolzDunkel), "Wand_RahmenR"), new Vector3(0.47f, 0f, 0f), Vector3.zero);
			/* Kurze, mittig gehaltene Fachwerk-Streben: eine lange schraege Strebe wuerde bei
			   Z-Rotation ihre Achsenausdehnung in X aufblaehen (AABB einer rotierten Box, siehe
			   Task-Report) und das Breitenbudget sprengen -- daher bewusst kurz (0,45 m) und nahe
			   der Mitte platziert (|x|<=0,28), bleibt so sicher innerhalb der 1,0-m-Rahmenbreite. */
			Teil(geometrie, "StrebeA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.07f, 0.45f, 0.05f), 1f, HolzDunkel), "Wand_StrebeA"), new Vector3(-0.15f, 0.5f, 0f), new Vector3(0f, 0f, 25f));
			Teil(geometrie, "StrebeB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.07f, 0.45f, 0.05f), 1f, HolzDunkel), "Wand_StrebeB"), new Vector3(0.15f, 0.5f, 0f), new Vector3(0f, 0f, -25f));
			Teil(geometrie, "Riegel", Persist(EidrenMeshFactory.TaperedBox(new Vector3(1f, 0.1f, 0.08f), 1f, HolzDunkel), "Wand_Riegel"), new Vector3(0f, 1.2f, 0f), Vector3.zero);
			Teil(geometrie, "WallCap", Persist(EidrenMeshFactory.TaperedBox(new Vector3(1f, 0.12f, 0.14f), 1f, HolzDunkel), "Wand_Kappe"), new Vector3(0f, 2.48f, 0f), Vector3.zero);
			Teil(geometrie, "Sockel", Persist(EidrenMeshFactory.TaperedBox(new Vector3(1f, 0.1f, 0.16f), 1f, SteinDunkel), "Wand_Sockel"), new Vector3(0f, 0f, 0f), Vector3.zero);
		});
	}

	/* BLD_Door_L01: Rahmen + Tuerblatt (Plankenlagen) + Diagonalstrebe + Beschlaege (14 Teile).
	   Sollhoehe 2,6 m (RahmenOben endet exakt bei y=2,6), Breitenbudget 1,0 m (identisches
	   Rahmenmass wie Wand). Kein Scharnier-Transform im Alt-Prefab gefunden (Grep ueber
	   Assets/_Game/Scripts: 0 Treffer auf "Hinge"/"Scharnier"), daher kein Pivot-Objekt noetig. */
	private static void BuildDoor()
	{
		RebuildGeometry("Door", geometrie =>
		{
			Teil(geometrie, "RahmenOben", Persist(EidrenMeshFactory.TaperedBox(new Vector3(1f, 0.12f, 0.14f), 1f, HolzDunkel), "Tuer_RahmenOben"), new Vector3(0f, 2.48f, 0f), Vector3.zero);
			Teil(geometrie, "RahmenUnten", Persist(EidrenMeshFactory.TaperedBox(new Vector3(1f, 0.12f, 0.14f), 1f, HolzDunkel), "Tuer_RahmenUnten"), new Vector3(0f, 0f, 0f), Vector3.zero);
			Teil(geometrie, "RahmenLinks", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, 2.6f, 0.1f), 1f, HolzDunkel), "Tuer_RahmenLinks"), new Vector3(-0.47f, 0f, 0f), Vector3.zero);
			Teil(geometrie, "RahmenRechts", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, 2.6f, 0.1f), 1f, HolzDunkel), "Tuer_RahmenRechts"), new Vector3(0.47f, 0f, 0f), Vector3.zero);
			for (int index = 0; index < 5; index++)
			{
				float x = -0.32f + (float)index * 0.16f;
				Teil(geometrie, $"DoorPlank_{index}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.15f, 2.36f, 0.04f), 1f, HolzHell), "Tuer_Planke"), new Vector3(x, 0.12f, 0f), Vector3.zero);
			}
			/* TaperedBox hat die lokale Y-Achse NICHT zentriert (Basis bei y=0, Kopf bei y=size.y) --
			   bei Z-Rotation einer LANGEN Box (die urspruengliche 1,7-m-Strebe) verschiebt das die
			   rotierte AABB einseitig um bis zu H*sin(theta) aus der Ankerposition heraus (hier ca.
			   0,9 m), was sowohl das Breiten- als auch das Hoehenbudget gesprengt haette (im Report
			   dokumentiert). Deshalb bewusst kurz (0,9 m) und mit x=0,24/y=1,0 so platziert, dass die
			   volle rotierte Ausdehnung innerhalb des Tuerblatts (x:+-0,5, y:0,12..2,48) bleibt. */
			Teil(geometrie, "DiagonalBrace", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.08f, 0.9f, 0.03f), 1f, HolzDunkel), "Tuer_Diagonalstrebe"), new Vector3(0.24f, 1.0f, 0.02f), new Vector3(0f, 0f, 32f));
			Teil(geometrie, "LockPlate", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.12f, 0.18f, 0.03f), 1f, Eisenkante), "Tuer_Schlossblech"), new Vector3(0.3f, 1.3f, 0.03f), Vector3.zero);
			Teil(geometrie, "Griff", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.05f, 0.05f, 0.1f), 0.8f, Metall), "Tuer_Griff"), new Vector3(0.32f, 1.3f, 0.05f), Vector3.zero);
			Teil(geometrie, "Scharnierband_0", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.18f, 0.08f, 0.04f), 1f, Metall), "Tuer_Scharnierband"), new Vector3(-0.35f, 2.0f, 0.02f), Vector3.zero);
			Teil(geometrie, "Scharnierband_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.18f, 0.08f, 0.04f), 1f, Metall), "Tuer_Scharnierband"), new Vector3(-0.35f, 0.6f, 0.02f), Vector3.zero);
		});
	}

	/* BLD_Floor_L01: flache Plankenplatte + Unterzuege + Naegel (18 Teile). GroundPlane-Eintrag
	   (Hoehe unbewertet), Breitenbudget 1,0 m (Planken innerhalb +-0,4955 m). */
	private static void BuildFloor()
	{
		RebuildGeometry("Floor", geometrie =>
		{
			/* Iteration 1 (Startabstand 0,1235) ergab GroundExtent gemessen 0,975 (erwartet 1,000,
			   1-%-Toleranz) -- Abstand direkt aus dem Sollmass hergeleitet: 8 Planken je 0,11 m
			   Breite sollen die volle 1,0-m-Breite ausfuellen (7 Zwischenraeume à (1,0-0,11)/7). */
			for (int index = 0; index < 8; index++)
			{
				float x = -0.445f + (float)index * (0.89f / 7f);
				Teil(geometrie, $"Plank_{index}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.11f, 0.08f, 0.96f), 1f, Holz), "Floor_Planke"), new Vector3(x, 0.06f, 0f), Vector3.zero);
			}
			Teil(geometrie, "UnderBraceA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.9f, 0.06f, 0.12f), 1f, HolzDunkel), "Floor_Unterzug"), new Vector3(0f, 0f, -0.3f), Vector3.zero);
			Teil(geometrie, "UnderBraceB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.9f, 0.06f, 0.12f), 1f, HolzDunkel), "Floor_Unterzug"), new Vector3(0f, 0f, 0.3f), Vector3.zero);
			for (int col = 0; col < 4; col++)
			{
				for (int row = -1; row <= 0; row++)
				{
					float x = -0.36f + (float)col * 0.24f;
					float z = (float)row * 0.3f + 0.15f;
					Teil(geometrie, $"FloorNail_{col}_{row}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.025f, 0.02f, 0.025f), 1f, Eisenkante), "Floor_Nagel"), new Vector3(x, 0.14f, z), Vector3.zero);
				}
			}
		});
	}

	/* BLD_FarmPlot_L01: Erdbett-Furchen + Einfassung + Aussaatmarker + Handrechen (26 Teile).
	   GroundPlane-Eintrag, Breitenbudget 2,0 m (Einfassung-Aussenkante exakt bei +-1,0 m). */
	private static void BuildFarmPlot()
	{
		RebuildGeometry("FarmPlot", geometrie =>
		{
			for (int index = 0; index < 11; index++)
			{
				float x = -0.75f + (float)index * 0.15f;
				Teil(geometrie, $"RaisedFurrow_{index}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.12f, 0.12f, 1.7f), 0.8f, Erde), "Farm_Furche"), new Vector3(x, 0f, 0f), Vector3.zero);
			}
			int markerIndex = 0;
			for (int col = -1; col <= 1; col++)
			{
				for (int row = -1; row <= 1; row++)
				{
					Teil(geometrie, $"SeedMarker_{markerIndex}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, 0.05f, 0.06f), 0.6f, Saat), "Farm_Saatmarker"), new Vector3((float)col * 0.5f, 0.12f, (float)row * 0.5f), Vector3.zero);
					markerIndex++;
				}
			}
			Teil(geometrie, "EinfassungN", Persist(EidrenMeshFactory.TaperedBox(new Vector3(1.96f, 0.15f, 0.12f), 1f, Holz), "Farm_Einfassung"), new Vector3(0f, 0f, -0.94f), Vector3.zero);
			Teil(geometrie, "EinfassungS", Persist(EidrenMeshFactory.TaperedBox(new Vector3(1.96f, 0.15f, 0.12f), 1f, Holz), "Farm_Einfassung"), new Vector3(0f, 0f, 0.94f), Vector3.zero);
			Teil(geometrie, "EinfassungO", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.12f, 0.15f, 1.96f), 1f, Holz), "Farm_Einfassung"), new Vector3(0.94f, 0f, 0f), Vector3.zero);
			Teil(geometrie, "EinfassungW", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.12f, 0.15f, 1.96f), 1f, Holz), "Farm_Einfassung"), new Vector3(-0.94f, 0f, 0f), Vector3.zero);
			/* Position bei x=0,78 statt 0,85/1,0: die Z-Rotations-AABB der geneigten Teile (siehe
			   Wand_StrebeA/B-Kommentar) verschiebt die tatsaechliche Aussenkante ueber die
			   Ankerposition hinaus -- bei x=0,78 bleibt sie unter der 1,0-m-Halbbreite (Budget 2,0 m). */
			Teil(geometrie, "HandRakeShaft", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.03f, 0.7f, 0.03f), 1f, HolzHell), "Farm_RechenStiel"), new Vector3(0.78f, 0f, 0.78f), new Vector3(0f, 0f, 30f));
			Teil(geometrie, "HandRakeHead", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.24f, 0.05f, 0.04f), 1f, Metall), "Farm_RechenKopf"), new Vector3(0.78f, 0.35f, 0.78f), new Vector3(0f, 0f, 30f));
		});
	}

	/* BLD_CookingPot_L01: Topf-Okt-Loft (Kessel + Lippe) + dreibeiniges Gestell + Feuerstein-Ring
	   (28 Teile). Sollhoehe 1,0 m (Aufhaengeoese endet exakt bei y=1,0), Breitenbudget 1,0 m. */
	private static void BuildCookingPot()
	{
		RebuildGeometry("CookingPot", geometrie =>
		{
			EidrenMeshFactory.LoftProfile[] kessel =
			{
				new EidrenMeshFactory.LoftProfile(0.28f, 0.2f, 0.2f),
				new EidrenMeshFactory.LoftProfile(0.35f, 0.42f, 0.42f),
				new EidrenMeshFactory.LoftProfile(0.55f, 0.46f, 0.46f),
				new EidrenMeshFactory.LoftProfile(0.75f, 0.38f, 0.38f),
				new EidrenMeshFactory.LoftProfile(0.85f, 0.42f, 0.42f)
			};
			Teil(geometrie, "CauldronBody", Persist(EidrenMeshFactory.Loft(kessel, EidrenMeshFactory.LoftShape.Oct, TopfEisen, capBottom: true, capTop: false), "Topf_Kessel"), Vector3.zero, Vector3.zero);
			EidrenMeshFactory.LoftProfile[] lippe =
			{
				new EidrenMeshFactory.LoftProfile(0.85f, 0.44f, 0.44f),
				new EidrenMeshFactory.LoftProfile(0.9f, 0.4f, 0.4f)
			};
			Teil(geometrie, "CauldronRim", Persist(EidrenMeshFactory.Loft(lippe, EidrenMeshFactory.LoftShape.Oct, Eisenkante, capBottom: true, capTop: true), "Topf_Lippe"), Vector3.zero, Vector3.zero);
			Teil(geometrie, "Inhalt", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.3f, 0.05f, 0.3f), 0.9f, TopfInhalt), "Topf_Inhalt"), new Vector3(0f, 0.83f, 0f), Vector3.zero);
			for (int index = 0; index < 3; index++)
			{
				float winkel = (float)index * 120f;
				float x = Mathf.Cos(winkel * Mathf.Deg2Rad) * 0.3f;
				float z = Mathf.Sin(winkel * Mathf.Deg2Rad) * 0.3f;
				Teil(geometrie, $"Bein_{index}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.07f, 0.32f, 0.07f), 0.6f, TopfEisen), "Topf_Bein"), new Vector3(x, 0f, z), Vector3.zero);
				Teil(geometrie, $"Fussplatte_{index}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.1f, 0.02f, 0.1f), 1f, TopfEisen), "Topf_Fussplatte"), new Vector3(x, 0f, z), Vector3.zero);
				float xm = Mathf.Cos((winkel + 60f) * Mathf.Deg2Rad) * 0.3f;
				float zm = Mathf.Sin((winkel + 60f) * Mathf.Deg2Rad) * 0.3f;
				Teil(geometrie, $"Querbalken_{index}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.3f, 0.04f, 0.04f), 1f, TopfEisen), "Topf_Querbalken"), new Vector3(xm, 0.2f, zm), new Vector3(0f, winkel + 90f, 0f));
			}
			Teil(geometrie, "Griff_0", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, 0.1f, 0.06f), 0.5f, TopfKupfer), "Topf_Griff"), new Vector3(-0.42f, 0.8f, 0f), Vector3.zero);
			Teil(geometrie, "Griff_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, 0.1f, 0.06f), 0.5f, TopfKupfer), "Topf_Griff"), new Vector3(0.42f, 0.8f, 0f), Vector3.zero);
			Teil(geometrie, "Deckelstuetze", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.15f, 0.04f, 0.04f), 1f, TopfHolz), "Topf_Deckelstuetze"), new Vector3(0f, 0.85f, 0.35f), Vector3.zero);
			Teil(geometrie, "Aufhaengeoese", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.08f, 0.1f, 0.08f), 0.6f, Metall), "Topf_Aufhaengeoese"), new Vector3(0f, 0.9f, 0f), Vector3.zero);
			for (int index = 0; index < 9; index++)
			{
				float winkel = (float)index / 9f * 360f;
				float x = Mathf.Cos(winkel * Mathf.Deg2Rad) * 0.38f;
				float z = Mathf.Sin(winkel * Mathf.Deg2Rad) * 0.38f;
				EidrenMeshFactory.LoftProfile[] stein =
				{
					new EidrenMeshFactory.LoftProfile(0f, 0.14f, 0.14f),
					new EidrenMeshFactory.LoftProfile(0.06f, 0.16f, 0.16f),
					new EidrenMeshFactory.LoftProfile(0.1f, 0.08f, 0.08f)
				};
				Teil(geometrie, $"HearthStone_{index}", Persist(EidrenMeshFactory.Loft(stein, EidrenMeshFactory.LoftShape.Oct, (index % 2 == 0) ? Stein : SteinHell, capBottom: true, capTop: true), "Topf_Herdstein"), new Vector3(x, 0f, z), new Vector3(0f, (float)index * 27f, 0f));
			}
			Teil(geometrie, "TopfGlutAkzent", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.16f, 0.03f, 0.16f), 1f, TopfGlut), "Topf_Glutakzent"), new Vector3(0f, 0.02f, 0f), Vector3.zero);
			EidrenMeshFactory.LoftProfile[] asche =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.5f, 0.5f),
				new EidrenMeshFactory.LoftProfile(0.02f, 0.46f, 0.46f)
			};
			Teil(geometrie, "AshRing", Persist(EidrenMeshFactory.Loft(asche, EidrenMeshFactory.LoftShape.Oct, TopfAsche, capBottom: true, capTop: true), "Topf_Aschering"), Vector3.zero, Vector3.zero);
			Teil(geometrie, "Kohlenest_0", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.08f, 0.03f, 0.08f), 1f, TopfAsche), "Topf_Kohlenest"), new Vector3(0.18f, 0.02f, -0.1f), Vector3.zero);
		});
	}

	/* BLD_Sawmill_L01: Grundgestell + Pfosten + Ueberkopfbalken + Saegeblatt-Wedge + Stammholz
	   + Fuehrung + Kurbel (9 Teile). Sollhoehe 2,2 m (Ueberkopfbalken endet exakt bei y=2,2),
	   Breitenbudget 1,0 m. Neuer Fabrik-Teileumfang folgt Plan Task 4 ("6-12 charakteristische
	   Fabrikteile" statt der alten Mindestzahl 27 -- Vertragsentscheidung im Report begruendet. */
	private static void BuildSawmill()
	{
		RebuildGeometry("Sawmill", geometrie =>
		{
			Teil(geometrie, "Grundgestell", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.9f, 0.15f, 0.9f), 1f, HolzDunkel), "Saege_Grundgestell"), Vector3.zero, Vector3.zero);
			Teil(geometrie, "StuetzeA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.1f, 1.95f, 0.1f), 1f, Holz), "Saege_Stuetze"), new Vector3(-0.35f, 0.15f, -0.3f), Vector3.zero);
			Teil(geometrie, "StuetzeB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.1f, 1.95f, 0.1f), 1f, Holz), "Saege_Stuetze"), new Vector3(0.35f, 0.15f, -0.3f), Vector3.zero);
			Teil(geometrie, "OverheadBeam", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.8f, 0.1f, 0.15f), 1f, HolzDunkel), "Saege_Ueberkopfbalken"), new Vector3(0f, 2.1f, -0.3f), Vector3.zero);
			Teil(geometrie, "TimberLog", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.7f, 0.22f, 0.22f), 0.95f, HolzHell), "Saege_Stammholz"), new Vector3(0f, 0.15f, 0.1f), Vector3.zero);
			/* Unrotiert (nicht wie urspruenglich um 90 Grad um X gekippt): eine X-Rotation eines
			   0,85 m hohen Keils tauscht Hoehen- gegen Tiefenausdehnung und liesse das Blatt flach
			   im Stamm "liegen" statt aufrecht zu stehen -- die Keilform allein liest sich bereits
			   als geneigtes Sae­geblatt. */
			Teil(geometrie, "Blade", Persist(EidrenMeshFactory.Wedge(new Vector3(0.5f, 0.85f, 0.03f), Eisenkante), "Saege_Saegeblatt"), new Vector3(0f, 0.15f, 0.05f), Vector3.zero);
			Teil(geometrie, "SlidingGuide", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.8f, 0.05f, 0.06f), 1f, Metall), "Saege_Fuehrung"), new Vector3(0f, 0.15f, 0.32f), Vector3.zero);
			Teil(geometrie, "CrankHandle", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, 0.3f, 0.06f), 1f, Metall), "Saege_Kurbel"), new Vector3(0.38f, 0.9f, -0.35f), new Vector3(0f, 0f, 20f));
			Teil(geometrie, "CrankGrip", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.1f, 0.06f, 0.06f), 1f, HolzHell), "Saege_Kurbelgriff"), new Vector3(0.38f, 1.05f, -0.35f), Vector3.zero);
		});
	}

	/* BLD_Smelter_L01: Ofen-Oktloft + Kamin + Balg + Tiegel + Glut-Akzent (12 Teile). Sollhoehe
	   2,0 m (Kaminkappe endet exakt bei y=2,0), Breitenbudget 1,0 m. Glut-Akzentfarbe (0,88; 0,31;
	   0,075) ist Plan-Vorgabe (Task 4), nicht aus Alt-Material extrahiert. */
	private static void BuildSmelter()
	{
		RebuildGeometry("Smelter", geometrie =>
		{
			EidrenMeshFactory.LoftProfile[] ofen =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.9f, 0.9f),
				new EidrenMeshFactory.LoftProfile(0.6f, 0.9f, 0.9f),
				new EidrenMeshFactory.LoftProfile(1.3f, 0.7f, 0.7f),
				new EidrenMeshFactory.LoftProfile(1.5f, 0.5f, 0.5f)
			};
			Teil(geometrie, "FacetedKilnBody", Persist(EidrenMeshFactory.Loft(ofen, EidrenMeshFactory.LoftShape.Oct, SteinDunkel, capBottom: true, capTop: true), "Schmelze_Ofenkorpus"), Vector3.zero, Vector3.zero);
			Teil(geometrie, "FireMouthDark", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.35f, 0.35f, 0.08f), 1f, Eisenkante), "Schmelze_Feuerloch"), new Vector3(0f, 0.3f, 0.44f), Vector3.zero);
			Teil(geometrie, "Chimney", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.25f, 0.35f, 0.25f), 0.9f, SteinDunkel), "Schmelze_Kamin"), new Vector3(0.15f, 1.5f, 0f), Vector3.zero);
			Teil(geometrie, "ChimneyCap", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.3f, 0.15f, 0.3f), 0.5f, SteinHell), "Schmelze_Kaminkappe"), new Vector3(0.15f, 1.85f, 0f), Vector3.zero);
			Teil(geometrie, "LeatherBellows", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.3f, 0.18f, 0.18f), 0.7f, Leder), "Schmelze_Balg"), new Vector3(-0.35f, 0.2f, 0.3f), Vector3.zero);
			Teil(geometrie, "BellowsNozzle", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.15f, 0.08f, 0.08f), 1f, Metall), "Schmelze_Balgduese"), new Vector3(-0.15f, 0.24f, 0.35f), Vector3.zero);
			EidrenMeshFactory.LoftProfile[] tiegel =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.16f, 0.16f),
				new EidrenMeshFactory.LoftProfile(0.14f, 0.2f, 0.2f)
			};
			Teil(geometrie, "Crucible", Persist(EidrenMeshFactory.Loft(tiegel, EidrenMeshFactory.LoftShape.Oct, Eisenkante, capBottom: true, capTop: true), "Schmelze_Tiegel"), new Vector3(0.32f, 0.15f, 0.35f), Vector3.zero);
			Teil(geometrie, "CopperIngot", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.12f, 0.06f, 0.06f), 1f, Kupfer), "Schmelze_Barren"), new Vector3(0.32f, 0.3f, 0.35f), Vector3.zero);
			Teil(geometrie, "SmelterEmberAccent", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.22f, 0.04f, 0.05f), 1f, SchmelzeGlut), "Schmelze_Glutakzent"), new Vector3(0f, 0.14f, 0.4f), Vector3.zero);
			Teil(geometrie, "Sockel", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.96f, 0.06f, 0.96f), 1f, SteinDunkel), "Schmelze_Sockel"), Vector3.zero, Vector3.zero);
			Teil(geometrie, "StuetzeA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.1f, 0.1f, 0.1f), 1f, Stein), "Schmelze_Stuetze"), new Vector3(-0.4f, 0f, -0.4f), Vector3.zero);
			Teil(geometrie, "StuetzeB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.1f, 0.1f, 0.1f), 1f, Stein), "Schmelze_Stuetze"), new Vector3(0.4f, 0f, -0.4f), Vector3.zero);
		});
	}

	/* BLD_Stonecutter_L01: Schneideblock + Steinplatte + Klingenrad (Oktloft) + Achse (9 Teile).
	   Sollhoehe 1,4 m (Achse endet exakt bei y=1,4), Breitenbudget 1,0 m. */
	private static void BuildStonecutter()
	{
		RebuildGeometry("Stonecutter", geometrie =>
		{
			Teil(geometrie, "HeavyCuttingBed", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.85f, 0.4f, 0.85f), 1f, Stein), "Stein_Schneideblock"), Vector3.zero, Vector3.zero);
			Teil(geometrie, "UncutStoneSlab", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.5f, 0.12f, 0.4f), 1f, SteinHell), "Stein_Steinplatte"), new Vector3(0f, 0.4f, 0.1f), Vector3.zero);
			EidrenMeshFactory.LoftProfile[] rad =
			{
				new EidrenMeshFactory.LoftProfile(0.9f, 0.7f, 0.7f),
				new EidrenMeshFactory.LoftProfile(1.0f, 0.7f, 0.7f)
			};
			Teil(geometrie, "GrindingWheel", Persist(EidrenMeshFactory.Loft(rad, EidrenMeshFactory.LoftShape.Oct, SteinDunkel, capBottom: true, capTop: true), "Stein_Klingenrad"), Vector3.zero, Vector3.zero);
			Teil(geometrie, "Axle", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, 0.5f, 0.06f), 1f, Metall), "Stein_Achse"), new Vector3(0f, 0.9f, 0f), Vector3.zero);
			Teil(geometrie, "HandCrank", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.05f, 0.28f, 0.05f), 1f, Metall), "Stein_Kurbel"), new Vector3(0.36f, 0.65f, 0.36f), new Vector3(0f, 0f, 15f));
			Teil(geometrie, "CrankGrip", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.08f, 0.05f, 0.05f), 1f, HolzHell), "Stein_Kurbelgriff"), new Vector3(0.44f, 0.78f, 0.36f), Vector3.zero);
			Teil(geometrie, "StuetzeA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.09f, 0.2f, 0.09f), 1f, SteinDunkel), "Stein_Stuetze"), new Vector3(-0.3f, 0f, -0.3f), Vector3.zero);
			Teil(geometrie, "StuetzeB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.09f, 0.2f, 0.09f), 1f, SteinDunkel), "Stein_Stuetze"), new Vector3(0.3f, 0f, -0.3f), Vector3.zero);
			Teil(geometrie, "CutScoreA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.4f, 0.02f, 0.03f), 1f, Eisenkante), "Stein_Schnittkerbe"), new Vector3(0f, 0.47f, 0.1f), Vector3.zero);
		});
	}

	/* BLD_Ropewalk_L01: Gestellrahmen + Spannbalken + gedrehte Straenge + Antriebsrad + Seilrolle
	   (9 Teile). Sollhoehe 1,6 m (Spannbalken endet exakt bei y=1,6), Breitenbudget 1,0 m. */
	private static void BuildRopewalk()
	{
		RebuildGeometry("Ropewalk", geometrie =>
		{
			Teil(geometrie, "GestellA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.1f, 1.48f, 0.1f), 1f, HolzDunkel), "Seilerei_Gestell"), new Vector3(-0.35f, 0f, 0f), Vector3.zero);
			Teil(geometrie, "GestellB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.1f, 1.48f, 0.1f), 1f, HolzDunkel), "Seilerei_Gestell"), new Vector3(0.35f, 0f, 0f), Vector3.zero);
			Teil(geometrie, "Spannbalken", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.8f, 0.12f, 0.12f), 1f, HolzDunkel), "Seilerei_Spannbalken"), new Vector3(0f, 1.48f, 0f), Vector3.zero);
			for (int index = 0; index < 3; index++)
			{
				float x = -0.15f + (float)index * 0.15f;
				Teil(geometrie, $"TwistedStrand_{index}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.05f, 1.2f, 0.05f), 0.9f, Seil), "Seilerei_Strang"), new Vector3(x, 0.15f, 0f), new Vector3(0f, (float)index * 8f, 0f));
			}
			EidrenMeshFactory.LoftProfile[] rad =
			{
				new EidrenMeshFactory.LoftProfile(0.05f, 0.35f, 0.35f),
				new EidrenMeshFactory.LoftProfile(0.15f, 0.35f, 0.35f)
			};
			Teil(geometrie, "DriveWheel", Persist(EidrenMeshFactory.Loft(rad, EidrenMeshFactory.LoftShape.Oct, HolzDunkel, capBottom: true, capTop: true), "Seilerei_Antriebsrad"), new Vector3(0.28f, 0f, 0.3f), Vector3.zero);
			EidrenMeshFactory.LoftProfile[] rolle =
			{
				new EidrenMeshFactory.LoftProfile(0.05f, 0.3f, 0.3f),
				new EidrenMeshFactory.LoftProfile(0.25f, 0.3f, 0.3f)
			};
			Teil(geometrie, "FinishedRopeCoil", Persist(EidrenMeshFactory.Loft(rolle, EidrenMeshFactory.LoftShape.Oct, Seil, capBottom: true, capTop: true), "Seilerei_Seilrolle"), new Vector3(-0.28f, 0f, 0.3f), Vector3.zero);
			Teil(geometrie, "Sockel", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.9f, 0.06f, 0.9f), 1f, HolzDunkel), "Seilerei_Sockel"), Vector3.zero, Vector3.zero);
		});
	}

	private static void EnsureFolder(string parent, string name)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + name))
		{
			AssetDatabase.CreateFolder(parent, name);
		}
	}
}
}
