using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
internal static class StyleProofPropBuilder
{
	private const string PrefabFolder = "Assets/_Game/Prefabs/Environment/StyleProof";

	private const string MeshRoot = "Assets/_Game/Art/StyleProof/Meshes";

	private static int _meshSequenz;

	/* Palette: Baeume nutzen die T1-Gruentoene (Wiedererkennung), die uebrigen Gruppen
	   bekommen eigene Toene laut Plan-Direktiven (STILUMBAU_E4_PLAN.md Task 3). */
	private static readonly Color Rinde = new Color(0.24f, 0.14f, 0.08f);
	private static readonly Color Laub = new Color(0.16f, 0.34f, 0.15f);
	private static readonly Color LaubHell = new Color(0.28f, 0.46f, 0.2f);
	private static readonly Color Farn = new Color(0.2f, 0.4f, 0.18f);
	private static readonly Color BlueteGelb = new Color(0.85f, 0.75f, 0.3f);
	private static readonly Color BluetePink = new Color(0.75f, 0.35f, 0.5f);
	private static readonly Color GrasGruen = new Color(0.24f, 0.42f, 0.2f);
	private static readonly Color Moos = new Color(0.2f, 0.36f, 0.16f);
	private static readonly Color PilzStiel = new Color(0.55f, 0.48f, 0.38f);
	private static readonly Color PilzGluehen = new Color(0.45f, 0.75f, 0.7f);
	private static readonly Color Stein = new Color(0.44f, 0.47f, 0.5f);
	private static readonly Color SteinHell = new Color(0.58f, 0.61f, 0.64f);
	private static readonly Color RuinStein = new Color(0.5f, 0.48f, 0.44f);
	private static readonly Color RuinAkzent = new Color(0.42f, 0.35f, 0.22f);
	private static readonly Color RunenPlatte = new Color(0.35f, 0.37f, 0.42f);
	private static readonly Color RunenAkzent = new Color(0.45f, 0.75f, 0.7f);

	/* Felsskalen fuer SP_Rock_Small/Medium/Large: Startwerte aus dem lokalen Hoehenwert der
	   jeweils hoechsten Einzelfelsformel (siehe BuildRock), gegen VisualScaleTests.
	   EveryBakedPrefab_MatchesItsTableHeight iteriert (Zahlenverlauf im Task-3-Report,
	   Muster wie T1ResourceVisualBuilder.FelsSkala*).
	   Iteration 1 (Startwerte 0,667/1,1/2,4) ergab: rock_small gemessen 0,592 (erwartet 0,500),
	   rock_medium gemessen 1,186 (erwartet 1,100), rock_large gemessen 2,483 (erwartet 2,400) -
	   Korrekturfaktoren direkt aus Ziel/gemessen der Iteration 1 ergaenzt. Iteration 2 bestaetigte
	   rock_medium/rock_large (beide im Toleranzband), nur rock_small lag mit 0,514 (erwartet
	   0,500) noch knapp ausserhalb 1% - zweiter Korrekturfaktor ergaenzt. */
	private const float FelsSkalaRockSmall = 0.5f / 0.75f * 0.500f / 0.592f * 0.500f / 0.514f;

	private const float FelsSkalaRockMedium = 1.1f / 1f * 1.100f / 1.186f;

	private const float FelsSkalaRockLarge = 2.4f / 1f * 2.400f / 2.483f;

	/* Fernwedel-Laenge: Ziel ist die Hoehe des am wenigsten gekippten Wedels (30 Grad) auf
	   die Sollhoehe 0,7 (Tabelle Task 1 (d)); Kippung reduziert die Hoehe um cos(30 Grad).
	   Iteration 1 (Startwert 0,8083) ergab gemessen 0,720 (erwartet 0,700) - Korrekturfaktor
	   ergaenzt. */
	private const float FarnLaenge = 0.7f / 0.8660254f * 0.700f / 0.720f;

	/* Bodendeckenskalen: Iteration 1 (unskaliert) ergab ground_grass gemessen 1,216 (erwartet
	   1,200), ground_moss gemessen 1,214 (erwartet 1,200) - die gesamte Anordnung (Radius +
	   Kegel-/Kuppelbreite) wird gleichfoermig um den Korrekturfaktor gestaucht, das skaliert
	   die gemessene Extent linear mit. */
	private const float GrassSkala = 1.2f / 1.216f;

	private const float MoosSkala = 1.2f / 1.214f;

	/* Schmaler Einstieg: baut NUR die 16 SP_-Prop-Prefabs (Meshes + Prefabs), ruehrt
	   Texturen, StyleProof-Materialien, Volume, VisualLibrary oder Zone_Greenwood.unity
	   nicht an (siehe STILUMBAU_E4_BAKEKETTE.md (a)). */
	[MenuItem("Eidren/V0.2/Stilumbau/Props bauen")]
	public static void BuildStandalone()
	{
		Build();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	internal static void Build()
	{
		if (MidpolyEnvironmentMigration.TryBuildApprovedVisuals())
		{
			return;
		}
		EnsureFolder("Assets/_Game/Art/StyleProof", "Meshes");
		_meshSequenz = 0;
		BuildTreeA();
		BuildTreeB();
		BuildTreeC();
		BuildBush();
		BuildFern();
		BuildFlowers();
		BuildGroundCoverGrass();
		BuildGroundCoverMoss();
		BuildGlowMushrooms();
		BuildRock("SP_Rock_Small", 2, FelsSkalaRockSmall, new Vector3(0.8f, 0.4f, 0.8f), new Vector3(0f, 0.2f, 0f));
		BuildRock("SP_Rock_Medium", 3, FelsSkalaRockMedium, new Vector3(1.3f, 0.8f, 1.3f), new Vector3(0f, 0.4f, 0f));
		BuildRock("SP_Rock_Large", 5, FelsSkalaRockLarge, new Vector3(2.2f, 1.8f, 2.2f), new Vector3(0f, 0.9f, 0f));
		BuildRuinWallA();
		BuildRuinWallB();
		BuildRuinMonument();
		BuildEidrenRune();
	}

	/* Persist mit Verwaisungsschutz, Muster wie T1ResourceVisualBuilder.Persist: Assets am
	   selben Sequenz-Index mit anderem Teilnamen werden geloescht. */
	private static Mesh Persist(Mesh mesh, string teilName)
	{
		string pfad = string.Format("{0}/SP_{1:000}_{2}.asset", MeshRoot, _meshSequenz, teilName);
		string muster = string.Format("SP_{0:000}_", _meshSequenz);
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

	/* SP_Tree_A: schlank-hoch, DREI Kronenstufen (G-005: Mittelstufe ergaenzt, Muster wie
	   T1ResourceVisualBuilder.BuildTree/aktiv). Sollhoehe 7,5 m (Tabelle Task 1 (d)) bleibt exakt,
	   da nur der obere Rand von CrownLow/CrownMid neu verteilt wird, der Gipfel von CrownHigh
	   unveraendert bei Stammfusshoehe + 3,1 m liegt. Stammfuss bekommt einen Wurzelanlauf
	   (G-005: gegen den "schwebt ueber dem Boden"-Befund), Kronen bekommen kontaktAo:false und
	   senken sich ~0,4 m in den Stamm (gegen den "Krone schwebt ueber dem Stamm"-Befund). */
	private static void BuildTreeA()
	{
		GameObject root = new GameObject("SP_Tree_A");
		EidrenMeshFactory.LoftProfile[] stamm =
		{
			new EidrenMeshFactory.LoftProfile(0f, 1.35f, 1.35f),        // Wurzelanlauf: 1,35x Stammfussbreite
			new EidrenMeshFactory.LoftProfile(0.5f, 1f, 1f),             // Anlauf verjuengt sich auf normale Stammbreite
			new EidrenMeshFactory.LoftProfile(1.4f, 0.82f, 0.82f, 0.04f),
			new EidrenMeshFactory.LoftProfile(3f, 0.62f, 0.62f, 0.1f, 0.04f),
			new EidrenMeshFactory.LoftProfile(4.4f, 0.46f, 0.46f, 0.06f)
		};
		Teil(root, "Trunk", Persist(EidrenMeshFactory.Loft(stamm, EidrenMeshFactory.LoftShape.Oct, Rinde, capBottom: true, capTop: true), "TreeA_Stamm"), Vector3.zero, Vector3.zero);
		Teil(root, "BranchA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.22f, 0.9f, 0.22f), 0.4f, Rinde), "TreeA_AstA"), new Vector3(0.4f, 3f, 0.1f), new Vector3(0f, 0f, -32f));
		Teil(root, "BranchB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.18f, 0.7f, 0.18f), 0.4f, Rinde), "TreeA_AstB"), new Vector3(-0.35f, 3.3f, -0.15f), new Vector3(0f, 150f, 28f));
		EidrenMeshFactory.LoftProfile[] kroneTief =
		{
			new EidrenMeshFactory.LoftProfile(-0.4f, 2f, 1.9f),  // sinkt 0,4 m in den Stammkopf (statt 0f)
			new EidrenMeshFactory.LoftProfile(0.6f, 3.2f, 2.9f),
			new EidrenMeshFactory.LoftProfile(1.3f, 2f, 1.8f)
		};
		Teil(root, "CrownLow", Persist(EidrenMeshFactory.Loft(kroneTief, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true, kontaktAo: false), "TreeA_KroneTief"), new Vector3(0f, 4.4f, 0f), Vector3.zero);
		EidrenMeshFactory.LoftProfile[] kroneMitte =
		{
			new EidrenMeshFactory.LoftProfile(0f, 1.7f, 1.55f),
			new EidrenMeshFactory.LoftProfile(0.4f, 2.2f, 2f),
			new EidrenMeshFactory.LoftProfile(0.75f, 1.5f, 1.35f),
			new EidrenMeshFactory.LoftProfile(1f, 0.7f, 0.6f)
		};
		Teil(root, "CrownMid", Persist(EidrenMeshFactory.Loft(kroneMitte, EidrenMeshFactory.LoftShape.Oct, LaubHell, capBottom: true, capTop: true, kontaktAo: false), "TreeA_KroneMitte"), new Vector3(0f, 5.7f, 0f), Vector3.zero);
		EidrenMeshFactory.LoftProfile[] kroneHoch =
		{
			new EidrenMeshFactory.LoftProfile(0f, 0.75f, 0.7f),
			new EidrenMeshFactory.LoftProfile(0.25f, 1.05f, 0.95f),
			new EidrenMeshFactory.LoftProfile(0.55f, 0.7f, 0.6f),
			new EidrenMeshFactory.LoftProfile(0.8f, 0.2f, 0.15f)
		};
		Teil(root, "CrownHigh", Persist(EidrenMeshFactory.Loft(kroneHoch, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true, kontaktAo: false), "TreeA_KroneHoch"), new Vector3(0f, 6.7f, 0f), Vector3.zero);
		SetCollider(root, new Vector3(0.9f, 3f, 0.9f), new Vector3(0f, 1.5f, 0f));
		Save(root);
	}

	/* SP_Tree_B: breit, drei versetzte Kronenstufen. Sollhoehe 6,0 m bleibt exakt (Wurzelanlauf
	   fuegt nur einen Ring bei y=0 ein und schiebt den alten Fussring auf y=0,35 - die Gesamt-
	   Y-Spanne 0..2,8 des Stamms und alle Kronenpositionen/-gipfel sind unveraendert). Krone
	   bekommt kontaktAo:false, CrownLow sinkt 0,4 m in den Stammkopf (G-005, Muster wie Tree_A). */
	private static void BuildTreeB()
	{
		GameObject root = new GameObject("SP_Tree_B");
		EidrenMeshFactory.LoftProfile[] stamm =
		{
			new EidrenMeshFactory.LoftProfile(0f, 1.42f, 1.42f),         // Wurzelanlauf: 1,35x Stammfussbreite
			new EidrenMeshFactory.LoftProfile(0.35f, 1.05f, 1.05f),      // Anlauf verjuengt sich auf normale Stammbreite
			new EidrenMeshFactory.LoftProfile(1f, 0.9f, 0.9f, 0.03f),
			new EidrenMeshFactory.LoftProfile(2f, 0.75f, 0.75f, 0.06f, 0.03f),
			new EidrenMeshFactory.LoftProfile(2.8f, 0.6f, 0.6f, 0.03f)
		};
		Teil(root, "Trunk", Persist(EidrenMeshFactory.Loft(stamm, EidrenMeshFactory.LoftShape.Oct, Rinde, capBottom: true, capTop: true), "TreeB_Stamm"), Vector3.zero, Vector3.zero);
		Teil(root, "BranchA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.26f, 1f, 0.26f), 0.42f, Rinde), "TreeB_AstA"), new Vector3(0.45f, 2f, 0.1f), new Vector3(0f, 10f, -36f));
		Teil(root, "BranchB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.22f, 0.8f, 0.22f), 0.4f, Rinde), "TreeB_AstB"), new Vector3(-0.4f, 2.3f, -0.2f), new Vector3(0f, 160f, 30f));
		EidrenMeshFactory.LoftProfile[] kroneTief =
		{
			new EidrenMeshFactory.LoftProfile(-0.4f, 2.6f, 2.4f),  // sinkt 0,4 m in den Stammkopf (statt 0f)
			new EidrenMeshFactory.LoftProfile(0.5f, 3.6f, 3.3f),
			new EidrenMeshFactory.LoftProfile(1f, 2.9f, 2.6f),
			new EidrenMeshFactory.LoftProfile(1.3f, 1.6f, 1.4f)
		};
		Teil(root, "CrownLow", Persist(EidrenMeshFactory.Loft(kroneTief, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true, kontaktAo: false), "TreeB_KroneTief"), new Vector3(0f, 2.8f, 0f), Vector3.zero);
		EidrenMeshFactory.LoftProfile[] kroneMitte =
		{
			new EidrenMeshFactory.LoftProfile(0f, 1.9f, 1.7f, 0.15f),
			new EidrenMeshFactory.LoftProfile(0.4f, 2.6f, 2.3f, 0.15f),
			new EidrenMeshFactory.LoftProfile(0.8f, 1.8f, 1.6f, 0.1f),
			new EidrenMeshFactory.LoftProfile(1f, 0.9f, 0.8f)
		};
		Teil(root, "CrownMid", Persist(EidrenMeshFactory.Loft(kroneMitte, EidrenMeshFactory.LoftShape.Oct, LaubHell, capBottom: true, capTop: true, kontaktAo: false), "TreeB_KroneMitte"), new Vector3(0f, 4.1f, 0f), Vector3.zero);
		EidrenMeshFactory.LoftProfile[] kroneHoch =
		{
			new EidrenMeshFactory.LoftProfile(0f, 1.1f, 1f, -0.1f),
			new EidrenMeshFactory.LoftProfile(0.3f, 1.5f, 1.3f, -0.1f),
			new EidrenMeshFactory.LoftProfile(0.6f, 0.9f, 0.8f),
			new EidrenMeshFactory.LoftProfile(0.9f, 0.3f, 0.25f)
		};
		Teil(root, "CrownHigh", Persist(EidrenMeshFactory.Loft(kroneHoch, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true, kontaktAo: false), "TreeB_KroneHoch"), new Vector3(0f, 5.1f, 0f), Vector3.zero);
		SetCollider(root, new Vector3(0.8f, 2.4f, 0.8f), new Vector3(0f, 1.2f, 0f));
		Save(root);
	}

	/* SP_Tree_C: knorrig, gekruemmter Stamm (OffsetX), zwei asymmetrische Kronenstufen bleiben
	   bewusst asymmetrisch (kein drittes Stufe noetig). Sollhoehe 4,0 m bleibt exakt (gleiches
	   Wurzelanlauf-Muster wie Tree_A/B, Gesamt-Y-Spanne 0..2,6 unveraendert). CrownA (Stammkontakt)
	   bekommt kontaktAo:false und sinkt 0,4 m in den Stammkopf; CrownB ist bereits luecklos an
	   CrownA angeschlossen und bekommt nur kontaktAo:false. */
	private static void BuildTreeC()
	{
		GameObject root = new GameObject("SP_Tree_C");
		EidrenMeshFactory.LoftProfile[] stamm =
		{
			new EidrenMeshFactory.LoftProfile(0f, 1.28f, 1.28f),          // Wurzelanlauf: 1,35x Stammfussbreite
			new EidrenMeshFactory.LoftProfile(0.3f, 0.95f, 0.95f),        // Anlauf verjuengt sich auf normale Stammbreite
			new EidrenMeshFactory.LoftProfile(0.9f, 0.8f, 0.78f, 0.12f),
			new EidrenMeshFactory.LoftProfile(1.8f, 0.62f, 0.6f, 0.28f, 0.05f),
			new EidrenMeshFactory.LoftProfile(2.6f, 0.48f, 0.46f, 0.42f)
		};
		Teil(root, "Trunk", Persist(EidrenMeshFactory.Loft(stamm, EidrenMeshFactory.LoftShape.Oct, Rinde, capBottom: true, capTop: true), "TreeC_Stamm"), Vector3.zero, Vector3.zero);
		EidrenMeshFactory.LoftProfile[] kroneA =
		{
			new EidrenMeshFactory.LoftProfile(-0.4f, 1.6f, 1.5f, 0.1f),  // sinkt 0,4 m in den Stammkopf (statt 0f)
			new EidrenMeshFactory.LoftProfile(0.45f, 2.4f, 2.1f, 0.15f),
			new EidrenMeshFactory.LoftProfile(0.9f, 1.7f, 1.4f, 0.05f),
			new EidrenMeshFactory.LoftProfile(1.2f, 0.7f, 0.6f)
		};
		Teil(root, "CrownA", Persist(EidrenMeshFactory.Loft(kroneA, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true, kontaktAo: false), "TreeC_KroneA"), new Vector3(0.42f, 2.6f, 0f), Vector3.zero);
		EidrenMeshFactory.LoftProfile[] kroneB =
		{
			new EidrenMeshFactory.LoftProfile(0f, 0.7f, 0.6f, -0.15f),
			new EidrenMeshFactory.LoftProfile(0.2f, 0.85f, 0.7f, -0.15f)
		};
		Teil(root, "CrownB", Persist(EidrenMeshFactory.Loft(kroneB, EidrenMeshFactory.LoftShape.Oct, LaubHell, capBottom: true, capTop: true, kontaktAo: false), "TreeC_KroneB"), new Vector3(0.42f, 3.8f, 0f), Vector3.zero);
		SetCollider(root, new Vector3(0.7f, 1.6f, 0.7f), new Vector3(0f, 0.8f, 0f));
		Save(root);
	}

	/* SP_Plant_Bush: Kuppel-Loft wie der T1-Beerenstrauch, ohne Beeren. Sollhoehe 0,9 m. */
	private static void BuildBush()
	{
		GameObject root = new GameObject("SP_Plant_Bush");
		EidrenMeshFactory.LoftProfile[] kuppel =
		{
			new EidrenMeshFactory.LoftProfile(0f, 1f, 0.9f),
			new EidrenMeshFactory.LoftProfile(0.55f, 1.35f, 1.2f),
			new EidrenMeshFactory.LoftProfile(0.9f, 0.3f, 0.25f)
		};
		Teil(root, "Dome", Persist(EidrenMeshFactory.Loft(kuppel, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true), "Bush_Kuppel"), Vector3.zero, Vector3.zero);
		Save(root);
	}

	/* SP_Plant_Fern: 6 gefaecherte flache TaperedBoxes, topScale 0,2, Kippung 30-55 Grad.
	   Sollhoehe 0,7 m (bestimmt vom am wenigsten gekippten Wedel bei 30 Grad). */
	private static void BuildFern()
	{
		GameObject root = new GameObject("SP_Plant_Fern");
		for (int index = 0; index < 6; index++)
		{
			float tiltX = 30f + (float)index * 5f;
			float spin = (float)index * 60f;
			Teil(root, $"Frond_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.14f, FarnLaenge, 0.03f), 0.2f, Farn), "Fern_Wedel"), Vector3.zero, new Vector3(tiltX, spin, 0f));
		}
		Save(root);
	}

	/* SP_Plant_Flowers: 5 duenne Stiele + Bluetenknollen in zwei Akzentfarben. Sollhoehe 0,4 m. */
	private static void BuildFlowers()
	{
		GameObject root = new GameObject("SP_Plant_Flowers");
		const float stielHoehe = 0.32f;
		const float knolleHoehe = 0.08f;
		for (int index = 0; index < 5; index++)
		{
			float spin = (float)index * 72f;
			float radius = 0.15f;
			float x = Mathf.Cos(spin * Mathf.Deg2Rad) * radius;
			float z = Mathf.Sin(spin * Mathf.Deg2Rad) * radius;
			Teil(root, $"Stalk_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.04f, stielHoehe, 0.04f), 0.5f, Farn), "Flowers_Stiel"), new Vector3(x, 0f, z), Vector3.zero);
			Color knolle = (index % 2 == 0) ? BlueteGelb : BluetePink;
			Teil(root, $"Bloom_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.12f, knolleHoehe, 0.12f), 0.6f, knolle), "Flowers_Bluete"), new Vector3(x, stielHoehe, z), Vector3.zero);
		}
		Save(root);
	}

	/* SP_GroundCover_Grass: 8 kleine Kegel (Oct-Loft, topScale 0,15), im Kreis mit Radius so
	   gewaehlt, dass die Grundflaeche exakt das Breitenbudget 1,2 m trifft (ein Kegel jeweils
	   bei 0/90/180/270 Grad liefert den vollen Kreisdurchmesser als Extent). Gesamthoehe 0,15 m
	   (informeller Hoehenkorridor aus dem Plan, ≤ 0,2 m). */
	private static void BuildGroundCoverGrass()
	{
		GameObject root = new GameObject("SP_GroundCover_Grass");
		const int anzahl = 8;
		const float kegelBreite = 0.12f * GrassSkala;
		const float radius = (1.2f - 0.12f) * 0.5f * GrassSkala;
		for (int index = 0; index < anzahl; index++)
		{
			float winkel = (float)index / anzahl * 360f;
			float x = Mathf.Cos(winkel * Mathf.Deg2Rad) * radius;
			float z = Mathf.Sin(winkel * Mathf.Deg2Rad) * radius;
			EidrenMeshFactory.LoftProfile[] kegel =
			{
				new EidrenMeshFactory.LoftProfile(0f, kegelBreite, kegelBreite),
				new EidrenMeshFactory.LoftProfile(0.15f, kegelBreite * 0.15f, kegelBreite * 0.15f)
			};
			Teil(root, $"Blade_{index:00}", Persist(EidrenMeshFactory.Loft(kegel, EidrenMeshFactory.LoftShape.Oct, GrasGruen, capBottom: true, capTop: true), "Grass_Kegel"), new Vector3(x, 0f, z), new Vector3(0f, (float)index * 41f, 0f));
		}
		Save(root);
	}

	/* SP_GroundCover_Moss: 6 flache Kuppel-Lofts, gleiches Radiusprinzip wie Grass, trifft
	   das Breitenbudget 1,2 m ueber die X-Achse (Kuppeln bei 0/180 Grad). Gesamthoehe 0,12 m. */
	private static void BuildGroundCoverMoss()
	{
		GameObject root = new GameObject("SP_GroundCover_Moss");
		const int anzahl = 6;
		const float kuppelBreite = 0.3f * MoosSkala;
		const float radius = (1.2f - 0.3f) * 0.5f * MoosSkala;
		for (int index = 0; index < anzahl; index++)
		{
			float winkel = (float)index / anzahl * 360f;
			float x = Mathf.Cos(winkel * Mathf.Deg2Rad) * radius;
			float z = Mathf.Sin(winkel * Mathf.Deg2Rad) * radius;
			EidrenMeshFactory.LoftProfile[] kuppel =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.22f, 0.2f),
				new EidrenMeshFactory.LoftProfile(0.06f, kuppelBreite, kuppelBreite * 0.93f),
				new EidrenMeshFactory.LoftProfile(0.12f, 0.08f, 0.07f)
			};
			Teil(root, $"Clump_{index:00}", Persist(EidrenMeshFactory.Loft(kuppel, EidrenMeshFactory.LoftShape.Oct, Moos, capBottom: true, capTop: true), "Moss_Kuppel"), new Vector3(x, 0f, z), new Vector3(0f, (float)index * 53f, 0f));
		}
		Save(root);
	}

	/* SP_Accent_GlowMushrooms: 4 Pilze (Stiel-TaperedBox + Okt-Kappen-Loft). Sollhoehe 0,35 m.
	   Der Shader hat kein Emissive; die helle Vertexfarbe der Kappe traegt den Gluehakzent
	   als Naeherung (im Abnahmebericht auszuweisen). */
	private static void BuildGlowMushrooms()
	{
		GameObject root = new GameObject("SP_Accent_GlowMushrooms");
		const float stielHoehe = 0.22f;
		for (int index = 0; index < 4; index++)
		{
			float spin = (float)index * 90f;
			float radius = 0.12f;
			float x = Mathf.Cos(spin * Mathf.Deg2Rad) * radius;
			float z = Mathf.Sin(spin * Mathf.Deg2Rad) * radius;
			Teil(root, $"Stem_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.06f, stielHoehe, 0.06f), 0.7f, PilzStiel), "Mushroom_Stiel"), new Vector3(x, 0f, z), Vector3.zero);
			EidrenMeshFactory.LoftProfile[] kappe =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.16f, 0.16f),
				new EidrenMeshFactory.LoftProfile(0.05f, 0.22f, 0.2f),
				new EidrenMeshFactory.LoftProfile(0.13f, 0.04f, 0.04f)
			};
			Teil(root, $"Cap_{index:00}", Persist(EidrenMeshFactory.Loft(kappe, EidrenMeshFactory.LoftShape.Oct, PilzGluehen, capBottom: true, capTop: true), "Mushroom_Kappe"), new Vector3(x, stielHoehe, z), Vector3.zero);
		}
		Save(root);
	}

	/* SP_Rock_Small/Medium/Large: dreiachsig gekippte Okt-Lofts, Muster wie
	   T1ResourceVisualBuilder.BuildRock. felsSkala ist der iterierte Korrekturfaktor
	   (Startwerte im Kommentar oben, Zahlenverlauf im Task-3-Report). */
	private static void BuildRock(string rootName, int count, float felsSkala, Vector3 colliderSize, Vector3 colliderCenter)
	{
		GameObject root = new GameObject(rootName);
		for (int index = 0; index < count; index++)
		{
			float height = felsSkala * (0.5f + (float)(index % 3) * 0.25f);
			EidrenMeshFactory.LoftProfile[] fels =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.7f, 0.62f),
				new EidrenMeshFactory.LoftProfile(height * 0.45f, 0.64f, 0.57f, 0.05f, -0.03f),
				new EidrenMeshFactory.LoftProfile(height, 0.32f, 0.28f, -0.04f)
			};
			float tiltX = (float)(index * 17 % 13) - 6f;
			float tiltZ = (float)(index * 23 % 11) - 5f;
			Teil(root, $"Rock_{index:00}", Persist(EidrenMeshFactory.Loft(fels, EidrenMeshFactory.LoftShape.Oct, (index % 3 == 0) ? SteinHell : Stein, capBottom: true, capTop: true), "Rock_Fels"),
				new Vector3((float)(index * 31 % 7 - 3) * 0.22f, 0f, (float)(index * 47 % 7 - 3) * 0.19f),
				new Vector3(tiltX, (float)index * 31f, tiltZ));
		}
		SetCollider(root, colliderSize, colliderCenter);
		Save(root);
	}

	/* SP_RuinWall_A: zwei Mauerlagen (TaperedBoxes) + drei Wedge-Zinnen fuer eine
	   unregelmaessige Oberkante. Sollhoehe 2,6 m. */
	private static void BuildRuinWallA()
	{
		GameObject root = new GameObject("SP_RuinWall_A");
		Teil(root, "LayerLow", Persist(EidrenMeshFactory.TaperedBox(new Vector3(2.6f, 1.3f, 0.7f), 0.92f, RuinStein), "RuinA_LagenTief"), Vector3.zero, Vector3.zero);
		Teil(root, "LayerHigh", Persist(EidrenMeshFactory.TaperedBox(new Vector3(2.3f, 1f, 0.6f), 0.9f, RuinStein), "RuinA_LagenHoch"), new Vector3(0f, 1.3f, 0f), Vector3.zero);
		Teil(root, "CrenelA", Persist(EidrenMeshFactory.Wedge(new Vector3(0.5f, 0.3f, 0.5f), RuinStein), "RuinA_ZinneA"), new Vector3(-0.7f, 2.3f, 0f), Vector3.zero);
		Teil(root, "CrenelB", Persist(EidrenMeshFactory.Wedge(new Vector3(0.45f, 0.18f, 0.45f), RuinStein), "RuinA_ZinneB"), new Vector3(0f, 2.3f, 0f), Vector3.zero);
		Teil(root, "CrenelC", Persist(EidrenMeshFactory.Wedge(new Vector3(0.4f, 0.24f, 0.4f), RuinStein), "RuinA_ZinneC"), new Vector3(0.75f, 2.3f, 0f), Vector3.zero);
		SetCollider(root, new Vector3(2.6f, 2.2f, 0.8f), new Vector3(0f, 1.1f, 0f));
		Save(root);
	}

	/* SP_RuinWall_B: gespiegelt und schmaler als A (Grundflaeche 2,0 statt 2,6 m), gleiche
	   Sollhoehe 2,6 m. */
	private static void BuildRuinWallB()
	{
		GameObject root = new GameObject("SP_RuinWall_B");
		Teil(root, "LayerLow", Persist(EidrenMeshFactory.TaperedBox(new Vector3(2f, 1.3f, 0.7f), 0.92f, RuinStein), "RuinB_LagenTief"), Vector3.zero, Vector3.zero);
		Teil(root, "LayerHigh", Persist(EidrenMeshFactory.TaperedBox(new Vector3(1.75f, 1f, 0.6f), 0.9f, RuinStein), "RuinB_LagenHoch"), new Vector3(0f, 1.3f, 0f), Vector3.zero);
		Teil(root, "CrenelA", Persist(EidrenMeshFactory.Wedge(new Vector3(0.4f, 0.24f, 0.4f), RuinStein), "RuinB_ZinneA"), new Vector3(0.6f, 2.3f, 0f), new Vector3(0f, 180f, 0f));
		Teil(root, "CrenelB", Persist(EidrenMeshFactory.Wedge(new Vector3(0.45f, 0.3f, 0.45f), RuinStein), "RuinB_ZinneB"), new Vector3(-0.1f, 2.3f, 0f), new Vector3(0f, 180f, 0f));
		SetCollider(root, new Vector3(2f, 2.2f, 0.8f), new Vector3(0f, 1.1f, 0f));
		Save(root);
	}

	/* SP_RuinMonument: Sockel-Loft + zwei gestapelte, leicht verdrehte TaperedBoxes +
	   gebrochene Spitze (Wedge). Sollhoehe 5,0 m. */
	private static void BuildRuinMonument()
	{
		GameObject root = new GameObject("SP_RuinMonument");
		EidrenMeshFactory.LoftProfile[] sockel =
		{
			new EidrenMeshFactory.LoftProfile(0f, 1.6f, 1.5f),
			new EidrenMeshFactory.LoftProfile(0.5f, 1.4f, 1.3f, 0.05f),
			new EidrenMeshFactory.LoftProfile(1f, 1f, 0.95f)
		};
		Teil(root, "Base", Persist(EidrenMeshFactory.Loft(sockel, EidrenMeshFactory.LoftShape.Oct, RuinStein, capBottom: true, capTop: true), "Monument_Sockel"), Vector3.zero, Vector3.zero);
		Teil(root, "BodyLow", Persist(EidrenMeshFactory.TaperedBox(new Vector3(1f, 1.8f, 0.9f), 0.85f, RuinStein), "Monument_KoerperTief"), new Vector3(0f, 1f, 0f), new Vector3(0f, 12f, 0f));
		Teil(root, "BodyHigh", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.8f, 1.6f, 0.7f), 0.8f, RuinAkzent), "Monument_KoerperHoch"), new Vector3(0f, 2.8f, 0f), new Vector3(0f, -15f, 0f));
		Teil(root, "BrokenTip", Persist(EidrenMeshFactory.Wedge(new Vector3(0.6f, 0.6f, 0.5f), RuinAkzent), "Monument_Spitze"), new Vector3(0f, 4.4f, 0f), new Vector3(0f, 20f, 0f));
		SetCollider(root, new Vector3(1.4f, 3f, 1.4f), new Vector3(0f, 1.5f, 0f));
		Save(root);
	}

	/* SP_EidrenRune: stehende Platte (TaperedBox) + drei eingesetzte Akzentboxen als
	   Runenzeichen. Sollhoehe 0,8 m. */
	private static void BuildEidrenRune()
	{
		GameObject root = new GameObject("SP_EidrenRune");
		Teil(root, "Plate", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.65f, 0.8f, 0.13f), 0.85f, RunenPlatte), "Rune_Platte"), Vector3.zero, Vector3.zero);
		Teil(root, "MarkA", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.16f, 0.16f, 0.05f), 0.8f, RunenAkzent), "Rune_ZeichenA"), new Vector3(0f, 0.6f, 0.09f), Vector3.zero);
		Teil(root, "MarkB", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.2f, 0.14f, 0.05f), 0.8f, RunenAkzent), "Rune_ZeichenB"), new Vector3(0f, 0.4f, 0.09f), Vector3.zero);
		Teil(root, "MarkC", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.14f, 0.18f, 0.05f), 0.8f, RunenAkzent), "Rune_ZeichenC"), new Vector3(0f, 0.18f, 0.09f), Vector3.zero);
		Save(root);
	}

	private static void SetCollider(GameObject root, Vector3 size, Vector3 center)
	{
		BoxCollider collider = root.AddComponent<BoxCollider>();
		collider.size = size;
		collider.center = center;
	}

	private static void Save(GameObject root)
	{
		int dreiecke = 0;
		foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
		{
			dreiecke += filter.sharedMesh.triangles.Length / 3;
		}
		Debug.Log($"[StilE4] {root.name}: {dreiecke} Dreiecke");
		string path = PrefabFolder + "/" + root.name + ".prefab";
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
