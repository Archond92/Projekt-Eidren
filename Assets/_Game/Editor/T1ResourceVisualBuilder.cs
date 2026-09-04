using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
internal static class T1ResourceVisualBuilder
{
	private const string VisualFolder = "Assets/_Game/Prefabs/Resources/Visuals";

	private const string MeshRoot = "Assets/_Game/Art/Resources/T1Meshes";

	private static int _meshSequenz;

	/* Palette: Greenwood-Baum, unterscheidbar vom Hartholzbaum (satteres Gruen). */
	private static readonly Color Rinde = new Color(0.24f, 0.14f, 0.08f);
	private static readonly Color Laub = new Color(0.16f, 0.34f, 0.15f);
	private static readonly Color LaubHell = new Color(0.28f, 0.46f, 0.2f);
	private static readonly Color Beere = new Color(0.62f, 0.12f, 0.14f);
	private static readonly Color Faser = new Color(0.42f, 0.5f, 0.28f);
	private static readonly Color Rispe = new Color(0.72f, 0.66f, 0.45f);
	private static readonly Color Stein = new Color(0.44f, 0.47f, 0.5f);
	private static readonly Color SteinHell = new Color(0.58f, 0.61f, 0.64f);
	private static readonly Color Wirt = new Color(0.35f, 0.3f, 0.27f);
	private static readonly Color Kupfer = new Color(0.72f, 0.4f, 0.2f);

	// F31-003: Verbraucht-Akzent der Kupferader — heller Wirtsfels statt
	// Erz-Orange, analog zu Stein/SteinHell. Das Erz ist weg, also darf der
	// Rest nicht weiter kupferfarben leuchten.
	private static readonly Color WirtHell = new Color(0.47f, 0.42f, 0.38f);

	/* Hoehenskalen fuer die dreiachsig gekippten Felscluster (StoneDeposit/CopperVein):
	   Startwerte, gegen VisualScaleTests.EveryBakedPrefab_MatchesItsTableHeight iteriert
	   (Zahlenverlauf im Task-3-Report dokumentiert, Muster wie T2ResourceVisualBuilder). */
	/* Iteration 1 (Startwerte 1.05/1.4/1.24) ergab: stone_deposit.active gemessen 1,136 (erwartet 1,100),
	   stone_deposit.exhausted gemessen 0,576 (erwartet 0,500), copper_vein.active/exhausted gemessen
	   1,325 (erwartet 1,300 je) - Faktoren direkt aus Ziel/gemessen der Iteration 1 hergeleitet.
	   Iteration 2 bestaetigte Stein-Aktiv/Kupfer-Aktiv/Kupfer-Verbraucht (alle im Toleranzband),
	   nur stone_deposit.exhausted lag mit 1,01278 (gemessen 0,513) noch knapp ausserhalb 1% von
	   0,500 - zweiter Korrekturfaktor 0,500/0,513 ergaenzt. */
	private const float FelsSkalaSteinAktiv = 1.05f * 1.100f / 1.136f;

	private const float FelsSkalaSteinVerbraucht = 1.4f * 0.500f / 0.576f * 0.500f / 0.513f;

	private const float FelsSkalaKupferAktiv = 1.24f * 1.300f / 1.325f;

	// F31-003: Verbraucht-Skala der Kupferader im selben Verhaeltnis wie beim
	// Stein (Verbraucht/Aktiv), damit der Rest flach und ausgeschlagen liest.
	private const float FelsSkalaKupferVerbraucht = FelsSkalaKupferAktiv * (FelsSkalaSteinVerbraucht / FelsSkalaSteinAktiv);

	private const float ErzSkalaKupfer = 0.9f;

	/* Schmaler Einstieg: baut NUR die zehn T1-Visuals (Meshes + Prefabs), ruehrt keine
	   Node-Prefabs, Zonenvarianten oder Ressourcen-Daten an. */
	[MenuItem("Eidren/V0.2/Stilumbau/T1-Visuals bauen")]
	public static void BuildStandalone()
	{
		Build();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	internal static void Build()
	{
		// MIDPOLY-000: Sobald die freigegebene Pilotfamilie aktiviert ist, sind
		// die authorisierten GLB-Modelle die Quelle der Wahrheit. Dadurch kann
		// kein alter Stil-/Content-Rebuild die produktiven Mid-Poly-Prefabs
		// unbemerkt wieder durch prozedurale Low-Poly-Geometrie ersetzen.
		// Fehlen Registry oder Runtime-Modelle, bleibt der bisherige Builder als
		// ausdruecklicher, vollstaendiger Fallback erhalten.
		if (MidpolyResourceMigration.TryBuildApprovedTierOneVisuals())
		{
			return;
		}

		EnsureFolder("Assets/_Game/Art/Resources", "T1Meshes");
		_meshSequenz = 0;
		BuildTree(active: true);
		BuildTree(active: false);
		BuildBerryBush(active: true);
		BuildBerryBush(active: false);
		BuildFiberPlant(active: true);
		BuildFiberPlant(active: false);
		BuildRock("StoneDeposit_Active", 7, tallFormula: true, Stein, SteinHell, crystals: false, FelsSkalaSteinAktiv);
		BuildRock("StoneDeposit_Exhausted", 4, tallFormula: false, Stein, SteinHell, crystals: false, FelsSkalaSteinVerbraucht);
		BuildRock("CopperVein_Active", 7, tallFormula: true, Wirt, Kupfer, crystals: true, FelsSkalaKupferAktiv, ErzSkalaKupfer);
		// F31-003: abgebaut = flacher Rest wie beim Stein — vorher behielt die
		// Ader den vollen hohen Cluster und niemand las den Wechsel als
		// "abgebaut" (nur das Erz fehlte).
		BuildRock("CopperVein_Exhausted", 4, tallFormula: false, Wirt, WirtHell, crystals: false, FelsSkalaKupferVerbraucht);
	}

	/* Persist mit Verwaisungsschutz: Assets am selben Index mit anderem Teilnamen werden geloescht. */
	private static Mesh Persist(Mesh mesh, string teilName)
	{
		string pfad = string.Format("{0}/T1_{1:000}_{2}.asset", MeshRoot, _meshSequenz, teilName);
		string muster = string.Format("T1_{0:000}_", _meshSequenz);
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

	private static GameObject Teil(GameObject root, string name, Mesh mesh, Vector3 position, Vector3 euler)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(root.transform, worldPositionStays: false);
		gameObject.transform.localPosition = position;
		gameObject.transform.localEulerAngles = euler;
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.AddComponent<MeshRenderer>().sharedMaterial = EidrenWorldStyleAssets.EnsureWorldMaterial();
		return gameObject;
	}

	private static void BuildTree(bool active)
	{
		GameObject root = new GameObject(active ? "Tree_Active" : "Tree_Exhausted");
		if (active)
		{
			/* Stamm: Okt-Loft, Basis 1,1 -> 0,55, leichter Schwung. Wurzelanlauf (G-005) fuegt
			   nur einen Ring bei y=0 ein und schiebt den alten Fussring auf y=0,4 - die Gesamt-
			   Y-Spanne 0..3,2 und alle Astpositionen/Kronenhoehen bleiben unveraendert, Sollhoehe
			   6,0 m bleibt exakt. */
			EidrenMeshFactory.LoftProfile[] stamm =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1.48f, 1.48f),         // Wurzelanlauf: 1,35x Stammfussbreite
				new EidrenMeshFactory.LoftProfile(0.4f, 1.1f, 1.1f),         // Anlauf verjuengt sich auf normale Stammbreite
				new EidrenMeshFactory.LoftProfile(1f, 0.92f, 0.92f, 0.05f),
				new EidrenMeshFactory.LoftProfile(2.2f, 0.72f, 0.72f, 0.1f, 0.04f),
				new EidrenMeshFactory.LoftProfile(3.2f, 0.55f, 0.55f, 0.06f)
			};
			Teil(root, "Trunk", Persist(EidrenMeshFactory.Loft(stamm, EidrenMeshFactory.LoftShape.Oct, Rinde, capBottom: true, capTop: true), "Stamm"), Vector3.zero, Vector3.zero);
			/* Zwei Astansaetze, gekippt. */
			Teil(root, "BranchA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.32f, 1.6f, 0.32f), 0.42f, Rinde), "AstA"), new Vector3(0.5f, 2.1f, 0.15f), new Vector3(0f, 0f, -36f));
			Teil(root, "BranchB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.28f, 1.4f, 0.28f), 0.4f, Rinde), "AstB"), new Vector3(-0.45f, 2.5f, -0.2f), new Vector3(0f, 140f, 32f));
			/* Krone: DREI gestufte Okt-Lofts in zwei Gruentoenen. kontaktAo:false (G-005: Kronen
			   duerfen nicht wie Bodenkontakt behandelt werden); CrownLow sinkt 0,4 m in den
			   Stammkopf (gegen den "Krone schwebt ueber dem Stamm"-Befund). */
			EidrenMeshFactory.LoftProfile[] kroneTief =
			{
				new EidrenMeshFactory.LoftProfile(-0.4f, 2.6f, 2.4f),  // sinkt 0,4 m in den Stammkopf (statt 0f)
				new EidrenMeshFactory.LoftProfile(0.5f, 4.2f, 3.9f),
				new EidrenMeshFactory.LoftProfile(1.1f, 3.6f, 3.3f),
				new EidrenMeshFactory.LoftProfile(1.6f, 2.2f, 2f)
			};
			Teil(root, "CrownLow", Persist(EidrenMeshFactory.Loft(kroneTief, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true, kontaktAo: false), "KroneTief"), new Vector3(0f, 2.9f, 0f), Vector3.zero);
			EidrenMeshFactory.LoftProfile[] kroneMitte =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1.9f, 1.8f),
				new EidrenMeshFactory.LoftProfile(0.5f, 3f, 2.8f),
				new EidrenMeshFactory.LoftProfile(1.05f, 2.3f, 2.1f),
				new EidrenMeshFactory.LoftProfile(1.5f, 1.1f, 1f)
			};
			Teil(root, "CrownMid", Persist(EidrenMeshFactory.Loft(kroneMitte, EidrenMeshFactory.LoftShape.Oct, LaubHell, capBottom: true, capTop: true, kontaktAo: false), "KroneMitte"), new Vector3(0f, 4.2f, 0f), Vector3.zero);
			EidrenMeshFactory.LoftProfile[] kroneHoch =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1f, 0.95f),
				new EidrenMeshFactory.LoftProfile(0.2f, 1.5f, 1.4f),
				new EidrenMeshFactory.LoftProfile(0.4f, 1.05f, 0.95f),
				new EidrenMeshFactory.LoftProfile(0.6f, 0.35f, 0.3f)
			};
			Teil(root, "CrownHigh", Persist(EidrenMeshFactory.Loft(kroneHoch, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true, kontaktAo: false), "KroneHoch"), new Vector3(0f, 5.4f, 0f), Vector3.zero);
		}
		else
		{
			/* Stumpf bleibt in Bodenkontakt (Kontakt-AO unveraendert), bekommt aber denselben
			   Wurzelanlauf wie der aktive Baum (G-005). */
			EidrenMeshFactory.LoftProfile[] stumpf =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1.62f, 1.62f),        // Wurzelanlauf: 1,35x Stumpffussbreite
				new EidrenMeshFactory.LoftProfile(0.08f, 1.2f, 1.2f),       // Anlauf verjuengt sich auf normale Stumpfbreite
				new EidrenMeshFactory.LoftProfile(0.3f, 1f, 1f, 0.03f),
				new EidrenMeshFactory.LoftProfile(0.55f, 0.9f, 0.85f),
				new EidrenMeshFactory.LoftProfile(0.7f, 0.75f, 0.7f)
			};
			Teil(root, "Stump", Persist(EidrenMeshFactory.Loft(stumpf, EidrenMeshFactory.LoftShape.Oct, Rinde, capBottom: true, capTop: true), "Stumpf"), Vector3.zero, Vector3.zero);
			Teil(root, "SplinterA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.6f, 0.16f, 0.22f), 0.55f, Rinde), "SplitterA"), new Vector3(0.45f, 0.02f, 0.1f), new Vector3(0f, 25f, 0f));
			Teil(root, "SplinterB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.5f, 0.14f, 0.2f), 0.5f, Rinde), "SplitterB"), new Vector3(-0.4f, 0.02f, -0.15f), new Vector3(0f, -18f, 0f));
		}
		Save(root);
	}

	private static void BuildBerryBush(bool active)
	{
		GameObject root = new GameObject(active ? "BerryBush_Active" : "BerryBush_Exhausted");
		EidrenMeshFactory.LoftProfile[] kuppel = active
			? new[]
			{
				new EidrenMeshFactory.LoftProfile(0f, 1f, 0.9f),
				new EidrenMeshFactory.LoftProfile(0.55f, 1.3f, 1.15f),
				new EidrenMeshFactory.LoftProfile(0.9f, 0.3f, 0.25f)
			}
			: new[]
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.85f, 0.75f),
				new EidrenMeshFactory.LoftProfile(0.35f, 0.95f, 0.85f),
				new EidrenMeshFactory.LoftProfile(0.7f, 0.45f, 0.4f)
			};
		Teil(root, "Dome", Persist(EidrenMeshFactory.Loft(kuppel, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true), "Kuppel"), Vector3.zero, Vector3.zero);
		if (active)
		{
			/* 9 Beeren-Knollen, deterministisches Index-Muster wie die Hanf-Halme in T2. */
			for (int index = 0; index < 9; index++)
			{
				float x = (float)(index * 37 % 9 - 4) * 0.09f;
				float z = (float)(index * 53 % 11 - 5) * 0.08f;
				float y = 0.22f + (float)(index % 4) * 0.13f;
				Vector3 groesse = new Vector3(0.08f + (float)(index % 3) * 0.02f, 0.1f + (float)(index % 2) * 0.02f, 0.08f + (float)(index % 3) * 0.02f);
				Teil(root, $"Berry_{index:00}", Persist(EidrenMeshFactory.TaperedBox(groesse, 0.5f, Beere), "Beere"), new Vector3(x, y, z), Vector3.zero);
			}
		}
		Save(root);
	}

	private static void BuildFiberPlant(bool active)
	{
		GameObject root = new GameObject(active ? "FiberPlant_Active" : "FiberPlant_Exhausted");
		if (active)
		{
			/* 9 schmale Halme, Faecher-Kippung -12..+12 Grad um die lokale X-Achse (Y-Spin aendert
			   die Hoehe nicht, siehe Kommentar unten). */
			const float stalkHeight = 0.8f;
			for (int index = 0; index < 9; index++)
			{
				float tiltX = -12f + (float)index * 3f;
				float spin = (float)index * 40f;
				float radius = 0.25f;
				float x = Mathf.Cos(spin * Mathf.Deg2Rad) * radius;
				float z = Mathf.Sin(spin * Mathf.Deg2Rad) * radius;
				Teil(root, $"Stalk_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, stalkHeight, 0.06f), 0.3f, Faser), "Halm"), new Vector3(x, 0f, z), new Vector3(tiltX, spin, 0f));
				/* Rotation um lokal Y aendert die Welt-Y-Koordinate eines Punktes nicht (Rotationsachse
				   ist die Hochachse) - die Halmspitze liegt daher bei stalkHeight * cos(tiltX). */
				float spitze = stalkHeight * Mathf.Cos(tiltX * Mathf.Deg2Rad);
				Teil(root, $"Head_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.18f, 0.16f, 0.18f), 0.45f, Rispe), "Rispe"), new Vector3(x, spitze - 0.06f, z), Vector3.zero);
			}
		}
		else
		{
			/* 5 kurze Stoppeln, aufrecht. */
			for (int index = 0; index < 5; index++)
			{
				float spin = (float)index * 61f;
				float radius = 0.16f;
				float x = Mathf.Cos(spin * Mathf.Deg2Rad) * radius;
				float z = Mathf.Sin(spin * Mathf.Deg2Rad) * radius;
				Teil(root, $"Stubble_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.05f, 0.25f, 0.05f), 0.3f, Faser), "Stoppel"), new Vector3(x, 0f, z), Vector3.zero);
			}
		}
		Save(root);
	}

	/* Felscluster fuer StoneDeposit/CopperVein: dreiachsig gekippt (X/Z-Kippung zusaetzlich zur
	   Y-Gierrotation), im Gegensatz zum reinen Y-Spin des T2-Pendants. tallFormula waehlt die
	   "aktive" Hoehenformel unabhaengig vom Zustandsnamen. Seit F31-003 nutzt auch
	   CopperVein_Exhausted die flache Verbraucht-Formel — der fruehere volle
	   Wirtsfels-Cluster war vom aktiven Zustand nicht zu unterscheiden. */
	private static void BuildRock(string rootName, int count, bool tallFormula, Color host, Color accent, bool crystals, float felsSkala, float erzSkala = 1f)
	{
		GameObject root = new GameObject(rootName);
		for (int index = 0; index < count; index++)
		{
			float height = felsSkala * (tallFormula ? (0.5f + (float)(index % 3) * 0.25f) : (0.22f + (float)(index % 2) * 0.12f));
			EidrenMeshFactory.LoftProfile[] fels =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.7f, 0.62f),
				new EidrenMeshFactory.LoftProfile(height * 0.45f, 0.64f, 0.57f, 0.05f, -0.03f),
				new EidrenMeshFactory.LoftProfile(height, 0.32f, 0.28f, -0.04f)
			};
			float tiltX = (float)(index * 17 % 13) - 6f;
			float tiltZ = (float)(index * 23 % 11) - 5f;
			Teil(root, $"Rock_{index:00}", Persist(EidrenMeshFactory.Loft(fels, EidrenMeshFactory.LoftShape.Oct, (index % 3 == 0) ? accent : host, capBottom: true, capTop: true), "Fels"),
				new Vector3((float)(index * 31 % 7 - 3) * 0.22f, 0f, (float)(index * 47 % 7 - 3) * 0.19f),
				new Vector3(tiltX, (float)index * 31f, tiltZ));
		}
		if (crystals)
		{
			for (int i = 0; i < 5; i++)
			{
				Vector3 erzGroesse = new Vector3(0.15f, 0.55f * erzSkala, 0.15f);
				float erzY = (0.4f + (float)(i % 2) * 0.16f) * erzSkala;
				Teil(root, $"OreShard_{i:00}", Persist(EidrenMeshFactory.TaperedBox(erzGroesse, 0.12f, accent), "Erzsplitter"),
					new Vector3(-0.5f + (float)i * 0.24f, erzY, 0.14f - (float)(i % 3) * 0.16f), new Vector3(10f, (float)i * 18f, 14f));
			}
		}
		Save(root);
	}

	private static void Save(GameObject root)
	{
		int dreiecke = 0;
		foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
		{
			dreiecke += filter.sharedMesh.triangles.Length / 3;
		}
		Debug.Log($"[StilE3b] {root.name}: {dreiecke} Dreiecke");
		string path = VisualFolder + "/" + root.name + ".prefab";
		// G-004: Bodenerdung als Bauteil, nicht als Nachlauf.
		WorldContactShadowBuilder.Attach(root);
		PrefabUtility.SaveAsPrefabAsset(root, path);
		UnityEngine.Object.DestroyImmediate(root);
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
