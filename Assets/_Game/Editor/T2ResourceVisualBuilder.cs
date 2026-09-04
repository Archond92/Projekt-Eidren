using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
internal static class T2ResourceVisualBuilder
{
	private const string MeshRoot = "Assets/_Game/Art/Resources/T2Meshes";

	private static int _meshSequenz;

	/* Palette: exakt die Farbwerte der bisherigen T2-Materialien. */
	private static readonly Color Rinde = new Color(0.19f, 0.1f, 0.07f);
	private static readonly Color Laub = new Color(0.09f, 0.22f, 0.14f);
	private static readonly Color LaubHell = new Color(0.18f, 0.34f, 0.2f);
	private static readonly Color Hanf = new Color(0.3f, 0.45f, 0.24f);
	private static readonly Color HanfKopf = new Color(0.62f, 0.57f, 0.28f);
	private static readonly Color Granit = new Color(0.38f, 0.42f, 0.46f);
	private static readonly Color GranitHell = new Color(0.54f, 0.57f, 0.6f);
	private static readonly Color Eisenfels = new Color(0.18f, 0.2f, 0.23f);
	private static readonly Color Eisenerz = new Color(0.57f, 0.39f, 0.31f);

	/* Hoehenkorrektur (Fix-Runde nach Regressionsfund in VisualScaleTests.EveryBakedPrefab_MatchesItsTableHeight,
	   Messbasis TestResults-stil-e3-t3fix-baseline.xml vom 08.08.2026): die reine Gierrotation der Felsen
	   (nur Y-Achse) und die urspruengliche Hanfkopf-/Halmgeometrie liefern eine kleinere bzw. groessere
	   Achsen-Bounding-Box, als die Groessentabelle erwartet. Faktoren direkt aus den gemessenen Werten
	   hergeleitet (Ziel / gemessen), nicht aus geschaetzten Kippwinkeln — pruefbar per erneutem Testlauf. */
	private const float FelsSkalaGranitAktiv = 1.266f / 0.920f;

	private const float FelsSkalaVerbraucht = 0.749f / 0.280f;

	private const float FelsSkalaEisenAktiv = 1.266f / 0.920f;

	private const float ErzSkalaEisenAktiv = 1.408f / 1.180f;

	private const float HalmSkalaVerbraucht = 0.228f / 0.234f;

	/* Schmaler Einstieg fuer Task 3, Schritt 4: baut NUR die acht T2-Visuals (Meshes + Prefabs),
	   ruehrt keine Node-Prefabs, keine Ressourcen-Daten und keine Zonen-Szenen an. */
	[MenuItem("Eidren/V0.2/Stilumbau/T2-Visuals bauen")]
	public static void BuildStandalone()
	{
		Build();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	internal static void Build()
	{
		EnsureFolder("Assets/_Game/Art/Resources", "T2Meshes");
		_meshSequenz = 0;
		// MIDPOLY-000 Phase 5: freigegebene Vegetations-GLBs sind nach der
		// Migration die Quelle der Wahrheit. Der Fallback bleibt fuer ein
		// unvollstaendiges oder explizit deaktiviertes Produktionskit erhalten.
		if (!MidpolyTierTwoVegetationMigration.TryBuildApprovedVisuals())
		{
			BuildHardwood(active: true);
			BuildHardwood(active: false);
			BuildHemp(active: true);
			BuildHemp(active: false);
		}
		// Granit-/Eisen-GLBs besitzen denselben Schutz vor Builder-Rueckbau.
		if (MidpolyTierTwoOreMigration.TryBuildApprovedVisuals())
		{
			return;
		}
		BuildRock("GraniteDeposit", active: true, Granit, GranitHell, crystals: false, felsSkala: FelsSkalaGranitAktiv);
		BuildRock("GraniteDeposit", active: false, Granit, GranitHell, crystals: false, felsSkala: FelsSkalaVerbraucht);
		BuildRock("IronVein", active: true, Eisenfels, Eisenerz, crystals: true, felsSkala: FelsSkalaEisenAktiv, erzSkala: ErzSkalaEisenAktiv);
		BuildRock("IronVein", active: false, Eisenfels, Eisenerz, crystals: true, felsSkala: FelsSkalaVerbraucht);
	}

	/* Persist mit Verwaisungsschutz: Assets am selben Index mit anderem Teilnamen werden geloescht. */
	private static Mesh Persist(Mesh mesh, string teilName)
	{
		string pfad = string.Format("{0}/T2_{1:000}_{2}.asset", MeshRoot, _meshSequenz, teilName);
		string muster = string.Format("T2_{0:000}_", _meshSequenz);
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

	private static void BuildHardwood(bool active)
	{
		GameObject root = new GameObject(active ? "HardwoodTree_Active" : "HardwoodTree_Exhausted");
		if (active)
		{
			/* Stamm: sich verjuengender Oktogon-Loft mit leichtem Schwung. Wurzelanlauf (G-005)
			   fuegt nur einen Ring bei y=0 ein und schiebt den alten Fussring auf y=0,5 - die
			   Gesamt-Y-Spanne 0..4,4 und alle Ast-/Kronenpositionen bleiben unveraendert,
			   Sollhoehe 6,65 m bleibt exakt. */
			EidrenMeshFactory.LoftProfile[] stamm =
			{
				new EidrenMeshFactory.LoftProfile(0f, 2.03f, 2.03f),         // Wurzelanlauf: 1,35x Stammfussbreite
				new EidrenMeshFactory.LoftProfile(0.5f, 1.5f, 1.5f),         // Anlauf verjuengt sich auf normale Stammbreite
				new EidrenMeshFactory.LoftProfile(1.4f, 1.15f, 1.15f, 0.08f),
				new EidrenMeshFactory.LoftProfile(3f, 0.9f, 0.9f, 0.14f, 0.06f),
				new EidrenMeshFactory.LoftProfile(4.4f, 0.72f, 0.72f, 0.08f)
			};
			Teil(root, "AncientTrunk", Persist(EidrenMeshFactory.Loft(stamm, EidrenMeshFactory.LoftShape.Oct, Rinde, capBottom: true, capTop: true), "Stamm"), Vector3.zero, Vector3.zero);
			Teil(root, "BrokenBranch", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.3f, 2.1f, 0.3f), 0.4f, Rinde), "Ast"), new Vector3(0.42f, 3.3f, 0f), new Vector3(0f, 0f, -38f));
			/* Krone: zwei gestufte Oktogon-Lofts statt glatter Kugeln. kontaktAo:false (G-005:
			   Kronen duerfen nicht wie Bodenkontakt behandelt werden); CrownLow sinkt 0,4 m in
			   den Stammkopf (gegen den "Krone schwebt ueber dem Stamm"-Befund). */
			EidrenMeshFactory.LoftProfile[] kroneTief =
			{
				new EidrenMeshFactory.LoftProfile(-0.4f, 2.2f, 2f),  // sinkt 0,4 m in den Stammkopf (statt 0f)
				new EidrenMeshFactory.LoftProfile(0.55f, 3.7f, 3.2f),
				new EidrenMeshFactory.LoftProfile(1.3f, 3.1f, 2.7f),
				new EidrenMeshFactory.LoftProfile(1.75f, 1.9f, 1.7f)
			};
			Teil(root, "CrownLow", Persist(EidrenMeshFactory.Loft(kroneTief, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true, kontaktAo: false), "KroneTief"), new Vector3(0f, 3.65f, 0f), Vector3.zero);
			EidrenMeshFactory.LoftProfile[] kroneHoch =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1.6f, 1.5f),
				new EidrenMeshFactory.LoftProfile(0.6f, 2.7f, 2.5f),
				new EidrenMeshFactory.LoftProfile(1.35f, 2f, 1.9f),
				new EidrenMeshFactory.LoftProfile(1.8f, 1f, 0.95f)
			};
			Teil(root, "CrownHigh", Persist(EidrenMeshFactory.Loft(kroneHoch, EidrenMeshFactory.LoftShape.Oct, LaubHell, capBottom: true, capTop: true, kontaktAo: false), "KroneHoch"), new Vector3(-0.35f, 4.85f, 0.1f), Vector3.zero);
		}
		else
		{
			/* Stumpf bleibt in Bodenkontakt (Kontakt-AO unveraendert), bekommt aber denselben
			   Wurzelanlauf wie der aktive Baum (G-005). */
			EidrenMeshFactory.LoftProfile[] stumpf =
			{
				new EidrenMeshFactory.LoftProfile(0f, 2.16f, 2.16f),        // Wurzelanlauf: 1,35x Stumpffussbreite
				new EidrenMeshFactory.LoftProfile(0.1f, 1.6f, 1.6f),        // Anlauf verjuengt sich auf normale Stumpfbreite
				new EidrenMeshFactory.LoftProfile(0.5f, 1.3f, 1.3f, 0.05f),
				new EidrenMeshFactory.LoftProfile(0.84f, 1.15f, 1.1f)
			};
			Teil(root, "WideStump", Persist(EidrenMeshFactory.Loft(stumpf, EidrenMeshFactory.LoftShape.Oct, Rinde, capBottom: true, capTop: true), "Stumpf"), Vector3.zero, Vector3.zero);
			Teil(root, "Split", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.8f, 0.22f, 0.3f), 0.7f, Rinde), "Splitter"), new Vector3(0.48f, 0.07f, 0.12f), new Vector3(0f, 22f, 0f));
		}
		Save(root);
	}

	private static void BuildHemp(bool active)
	{
		GameObject root = new GameObject(active ? "SwampHemp_Active" : "SwampHemp_Exhausted");
		int count = (active ? 11 : 7);
		for (int index = 0; index < count; index++)
		{
			float x = (float)(index * 37 % 9 - 4) * 0.16f;
			float z = (float)(index * 53 % 11 - 5) * 0.12f;
			float height = (active ? (0.82f + (float)(index % 4) * 0.12f) : (0.17f + (float)(index % 2) * 0.05f) * HalmSkalaVerbraucht);
			Teil(root, $"Stalk_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.09f, height, 0.09f), 0.45f, Hanf), "Halm"), new Vector3(x, 0f, z), new Vector3((float)(index % 3) * 3f, (float)(index * 31 % 360), (float)(index % 2) * 4f));
			if (active)
			{
				Teil(root, $"Head_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.2f, 0.22f, 0.2f), 0.25f, HanfKopf), "Kopf"), new Vector3(x, height - 0.03f, z), Vector3.zero);
			}
		}
		Save(root);
	}

	private static void BuildRock(string stem, bool active, Color host, Color accent, bool crystals, float felsSkala, float erzSkala = 1f)
	{
		GameObject root = new GameObject(stem + "_" + (active ? "Active" : "Exhausted"));
		int count = (active ? 7 : 4);
		for (int index = 0; index < count; index++)
		{
			float height = felsSkala * (active ? (0.48f + (float)(index % 3) * 0.22f) : (0.18f + (float)(index % 2) * 0.1f));
			EidrenMeshFactory.LoftProfile[] fels =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.68f, 0.6f),
				new EidrenMeshFactory.LoftProfile(height * 0.45f, 0.62f, 0.55f, 0.05f, -0.03f),
				new EidrenMeshFactory.LoftProfile(height, 0.34f, 0.3f, -0.04f)
			};
			Teil(root, $"Rock_{index:00}", Persist(EidrenMeshFactory.Loft(fels, EidrenMeshFactory.LoftShape.Oct, (index % 3 == 0) ? accent : host, capBottom: true, capTop: true), "Fels"), new Vector3((float)(index * 31 % 7 - 3) * 0.22f, 0f, (float)(index * 47 % 7 - 3) * 0.19f), new Vector3(0f, (float)index * 31f, 0f));
		}
		if (active && crystals)
		{
			for (int i = 0; i < 5; i++)
			{
				Vector3 erzGroesse = new Vector3(0.16f, 0.58f * erzSkala, 0.16f);
				float erzY = (0.43f + (float)(i % 2) * 0.17f) * erzSkala;
				Teil(root, $"OreShard_{i:00}", Persist(EidrenMeshFactory.TaperedBox(erzGroesse, 0.12f, accent), "Erzsplitter"), new Vector3(-0.52f + (float)i * 0.25f, erzY, 0.15f - (float)(i % 3) * 0.18f), new Vector3(12f, (float)i * 18f, 18f));
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
		Debug.Log($"[StilE3] {root.name}: {dreiecke} Dreiecke");
		string path = "Assets/_Game/Prefabs/Resources/Visuals/" + root.name + ".prefab";
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
