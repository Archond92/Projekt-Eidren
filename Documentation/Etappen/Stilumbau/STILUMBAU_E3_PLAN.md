# Stilumbau Etappe 3 — Tier-2-Ressourcenknoten: Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ziel:** Die vier Tier-2-Ressourcentypen (HardwoodTree, SwampHemp, GraniteDeposit, IronVein) rendern in Active und Exhausted im Vertexfarben-Stil; ihre sechs Zonenvarianten (TwilightGrove, VeilMarsh, GreyRifts) werden neu geklont. Die handgebauten Tier-1-Visuals und -Varianten (Geometry_A14, Stand 05.08.) bleiben unangetastet.

**Umfangsbefund (07.08.2026, ersetzt die Spec-Annahme für Etappe 3):** Alle 10 Tier-1-Visuals (Tree, StoneDeposit, FiberPlant, CopperVein, BerryBush × Active/Exhausted) und die Tier-1-Zonenvarianten tragen handgebaute `Geometry_A14`-Geometrie — analog zum Stationen-Entscheid aus Etappe 2 bleiben sie bestehen. Primitiv-Bestand und damit Etappe-3-Umfang sind ausschließlich: die 8 Tier-2-Basis-Visuals (`T2ResourceVisualBuilder`, Unity-Primitive mit Einfarb-Materialien) und die 12 Tier-2-Zonenvarianten (6 × Active/Exhausted, gebackene Kopien der Basis).

**Architektur:** `T2ResourceVisualBuilder` wird auf `EidrenMeshFactory` + `EidrenWorldStyleAssets.EnsureWorldMaterial()` umgestellt (Bäume mit Kronen-Lofts, Hanf mit getaperten Halmen, Felsen als Oktogon-Lofts). Die Zonenvarianten entstehen wie bisher über `AreaArtVariantPrefabBuilder.CloneVariant` aus den neuen Basis-Prefabs — der genaue Aufrufweg und der Bake-Umfang werden per Auflage aus dem E2-Abschlussreview VOR dem Umbau dokumentiert. Node-Prefabs betten die Visuals als Prefab-Instanzen ein und aktualisieren sich voraussichtlich automatisch (Verifikation in Task 4).

**Tech Stack:** wie Etappe 1/2 (Unity 6000.3, URP 17.3 Gamma+LDR, NUnit EditMode).

**Spezifikation:** `STILUMBAU_WELTINVENTAR_ENTWURF.md` · **Vorlagen:** `STILUMBAU_E1_PLAN.md`, `STILUMBAU_E2_PLAN.md`

## Globale Vorgaben

- Alle Etappe-1/2-Regeln gelten unverändert: Pfad quoten, Lockfile prüfen/nie löschen, Exit-Code lügt, Captures ohne `-nographics`, Tests mit `-nographics`, kein `git commit`, niemals Belege erfinden, deutsche Kommentare/Tabs/`sealed`, Belege mit Präfix `stil-e3-…`.
- **Tier-1 ist tabu:** Kein Builder-Lauf, der Tier-1-Visuals, Tier-1-Varianten oder deren Szenen neu schreibt. Vor jedem Lauf klären (Task 1), was der Einstiegspunkt alles anfasst; im Zweifel gezieltere Methode wählen oder eine neue schmale Einstiegsmethode ergänzen.
- **Persist-Verwaisungsregel (Auflage aus E2):** Der neue Mesh-Ablageort `Assets/_Game/Art/Resources/T2Meshes` wird von der Umstellung frisch angelegt; der Persist-Helfer dieser Etappe löscht vor dem Schreiben alle vorhandenen Assets, deren Pfad denselben Sequenzindex, aber einen anderen Teilnamen trägt (kein Verwaisen mehr).
- Bestandsverträge: Prefabpfade `Assets/_Game/Prefabs/Resources/Visuals/<Name>.prefab` und Wurzelnamen (`HardwoodTree_Active` usw.) unverändert; Visuals bleiben colliderfrei (Interaktions-/Blocker-Collider sitzen am Node-Prefab); die T2-Materialfarben aus `T2ResourceVisualBuilder.Build()` sind die Palettenquelle (`HardwoodBark` (0.19, 0.1, 0.07), `HardwoodLeaf` (0.09, 0.22, 0.14), `HardwoodLeafLight` (0.18, 0.34, 0.2), `SwampHemp` (0.3, 0.45, 0.24), `SwampHempHead` (0.62, 0.57, 0.28), `Granite` (0.38, 0.42, 0.46), `GraniteLight` (0.54, 0.57, 0.6), `IronRock` (0.18, 0.2, 0.23), `IronOre` (0.57, 0.39, 0.31)); die `.mat`-Dateien bleiben bis Etappe 5 liegen.
- Dreieckskorridore: HardwoodTree_Active ≤ 700, alle übrigen Visuals ≤ 500.
- Silhouetten bleiben lesbar: Höhen/Fußabdrücke der Primitiv-Vorgänger ± 15 % (Kronenhöhe ~5,75, Stumpf ~0,4, Hanf ~0,9, Felsen ~0,7) — die Größentabelle (`VisualScaleTableBuilder`) liefert Collider-Maße für Nodes und wird NICHT geändert.

---

### Task 0: Sicherung, Vorflug und frische EditMode-Baseline

Wie E2-Task 0, mit Zielen `vor-stilumbau-e3-<stamp>`, `TestResults-stil-e3-baseline.xml`, `stil-e3-baseline.log`. Erwartet ≈ 71 bekannte Fehlschläge. Prüfnachweis: Sicherungslisting, `<test-run>`-Zeile.

---

### Task 1: Bake-Kette dokumentieren (Auflage aus dem E2-Abschlussreview)

**Files:**
- Create: `Documentation/Etappen/Stilumbau/STILUMBAU_E3_BAKEKETTE.md`

**Interfaces:**
- Produces: verbindliche Antworten für Task 4 — (a) welcher Codepfad `AreaArtVariantPrefabBuilder.CloneVariant` für die sechs T2-Varianten aufruft (vermutlich `AreaArtAssetBuilder`; Aufrufer, Methode, MenuItem benennen); (b) ob dieser Einstieg auch Tier-1-Varianten oder Szenen neu schreibt und wie ein T2-only-Lauf aussieht (vorhandene Methode oder benannter Vorschlag für eine neue schmale Einstiegsmethode mit Signatur); (c) welche Szenen die T2-Varianten-/Node-Prefabs referenzieren und ob nach dem Neuklonen ein Szenen-Rebake nötig ist oder Prefab-Referenzen genügen; (d) Bestätigung per Prefab-YAML, dass `ActiveVisual`/`ExhaustedVisual` in den vier T2-Node-Prefabs als Prefab-INSTANZEN eingebettet sind (dann aktualisieren sie sich ohne Node-Rebuild) oder als eingebackene Kopien (dann gehört `NodePrefab`-Neubau in Task 4 — nur für die vier T2-Definitionen).

**Steps:** Quelltext lesen (`AreaArtAssetBuilder.cs`, `AreaArtSceneBuilder.cs`, `EidrenSceneStructureBuilder.BuildResourceZoneScenes`, die vier T2-Node-Prefab-YAMLs), Befunde mit Datei:Zeile belegen, Dokument schreiben. Kein Unity-Lauf nötig. Prüfnachweis: das Dokument mit allen vier Antworten, jede mit Beleg.

---

### Task 2: Vorher-Captures der 20 Tier-2-Prefabs

**Files:**
- Modify: `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs`

**Interfaces:**
- Produces: `CapturePrefabListe(string ordner, (string pfad, string name, float zielHoehe, float distanz)[] liste)` — verallgemeinert das `CaptureStationen`-Muster (Studio-Licht/RT wie gehabt, Kamera auf `(distanz*0.77f, distanz*0.71f, distanz)` mit LookAt `(0, zielHoehe, 0)`); dazu MenuItems `Eidren/V0.2/Stilumbau/T2-Ressourcen Vorher-Captures` → `TempReview/StilumbauE3/vorher` und `…Nachher-Captures` → `…/nachher` über eine feste Liste `T2Ressourcen` mit 20 Einträgen: die 8 Basis-Visuals unter `Assets/_Game/Prefabs/Resources/Visuals/` (`HardwoodTree_Active` zielHoehe 3.0 distanz 7, `HardwoodTree_Exhausted` 0.5/3, `SwampHemp_Active` 0.6/2.5, `SwampHemp_Exhausted` 0.3/2, `GraniteDeposit_Active` 0.6/3, `GraniteDeposit_Exhausted` 0.3/2.5, `IronVein_Active` 0.7/3, `IronVein_Exhausted` 0.3/2.5) und die 12 T2-Varianten unter `Assets/_Game/Prefabs/Environment/AreaArtVariants/` (`TwilightGrove_hardwood_tree_*`, `TwilightGrove_swamp_hemp_*`, `VeilMarsh_swamp_hemp_*`, `VeilMarsh_iron_vein_*`, `GreyRifts_granite_deposit_*`, `GreyRifts_iron_vein_*` mit denselben Werten wie ihr Basistyp). `CaptureStationen` intern auf den neuen Helfer umstellen, Verhalten identisch.

**Steps:** Methode ergänzen (Muster: bestehende `CaptureStationen`; `[StilE3]`-Logzeilen), Vorher-Lauf ohne `-nographics` (`stil-e3-t2-vorher.log`), 20 PNGs prüfen, JEDES Bild ansehen (Lehre aus E2-Task 1 — keine Pauschalaussagen), Sichtliste in den Report. Prüfnachweis: Listing, Logauszug, Sichtliste.

---

### Task 3: T2ResourceVisualBuilder auf die Fabrik umstellen

**Files:**
- Modify: `Assets/_Game/Editor/T2ResourceVisualBuilder.cs`
- Test: `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` (Ergänzung)

**Interfaces:**
- Consumes: `EidrenMeshFactory.TaperedBox/Loft` (geschachtelte Typen), `EidrenWorldStyleAssets.EnsureWorldMaterial()`.
- Produces: acht neu gebaute Visual-Prefabs an unveränderten Pfaden; `Persist`-Helfer mit Verwaisungsschutz; `Build()` behält Signatur (wird weiter von `ResourceContentBuilder.BuildResourceDefinitions` gerufen).

- [ ] **Step 1: Failing Test ergänzen** — in `EidrenWorldStyleTests.cs` anhängen:

```csharp
		[TestCase("HardwoodTree_Active", 700)]
		[TestCase("HardwoodTree_Exhausted", 500)]
		[TestCase("SwampHemp_Active", 500)]
		[TestCase("SwampHemp_Exhausted", 500)]
		[TestCase("GraniteDeposit_Active", 500)]
		[TestCase("GraniteDeposit_Exhausted", 500)]
		[TestCase("IronVein_Active", 500)]
		[TestCase("IronVein_Exhausted", 500)]
		public void T2Visual_IstImVertexfarbenStil(string name, int korridor)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Resources/Visuals/" + name + ".prefab");
			Assert.That(prefab, Is.Not.Null, name);
			int dreiecke = 0;
			foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true))
			{
				Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
					name + "/" + filter.name + " ohne Vertexfarben");
				dreiecke += filter.sharedMesh.triangles.Length / 3;
			}
			Assert.That(dreiecke, Is.LessThanOrEqualTo(korridor), name + " ueberschreitet den Dreieckskorridor");
			foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
			{
				Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
					name + "/" + renderer.name + " nutzt nicht das gemeinsame Material");
			}
			Assert.That(prefab.GetComponentsInChildren<Collider>(includeInactive: true), Is.Empty,
				name + " Visual darf keine Collider tragen");
		}
```

- [ ] **Step 2: Testlauf — die 8 neuen Fälle scheitern** (alte Primitive ohne Vertexfarben): XML `TestResults-stil-e3-t3a.xml`, Log `stil-e3-t3a.log`.

- [ ] **Step 3: Builder umstellen** — `T2ResourceVisualBuilder.cs` vollständig neu (Struktur bleibt: `Build()`, je Typ eine Bau-Methode, `Save`); `Part`/`Material` entfallen. Kern:

```csharp
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

	internal static void Build()
	{
		EnsureFolder("Assets/_Game/Art/Resources", "T2Meshes");
		_meshSequenz = 0;
		BuildHardwood(active: true);
		BuildHardwood(active: false);
		BuildHemp(active: true);
		BuildHemp(active: false);
		BuildRock("GraniteDeposit", active: true, Granit, GranitHell, crystals: false);
		BuildRock("GraniteDeposit", active: false, Granit, GranitHell, crystals: false);
		BuildRock("IronVein", active: true, Eisenfels, Eisenerz, crystals: true);
		BuildRock("IronVein", active: false, Eisenfels, Eisenerz, crystals: true);
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
			Object.DestroyImmediate(mesh);
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
			/* Stamm: sich verjuengender Oktogon-Loft mit leichtem Schwung */
			EidrenMeshFactory.LoftProfile[] stamm =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1.5f, 1.5f),
				new EidrenMeshFactory.LoftProfile(1.4f, 1.15f, 1.15f, 0.08f),
				new EidrenMeshFactory.LoftProfile(3f, 0.9f, 0.9f, 0.14f, 0.06f),
				new EidrenMeshFactory.LoftProfile(4.4f, 0.72f, 0.72f, 0.08f)
			};
			Teil(root, "AncientTrunk", Persist(EidrenMeshFactory.Loft(stamm, EidrenMeshFactory.LoftShape.Oct, Rinde, capBottom: true, capTop: true), "Stamm"), Vector3.zero, Vector3.zero);
			Teil(root, "BrokenBranch", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.3f, 2.1f, 0.3f), 0.4f, Rinde), "Ast"), new Vector3(0.42f, 3.3f, 0f), new Vector3(0f, 0f, -38f));
			/* Krone: zwei gestufte Oktogon-Lofts statt glatter Kugeln */
			EidrenMeshFactory.LoftProfile[] kroneTief =
			{
				new EidrenMeshFactory.LoftProfile(0f, 2.2f, 2f),
				new EidrenMeshFactory.LoftProfile(0.55f, 3.7f, 3.2f),
				new EidrenMeshFactory.LoftProfile(1.3f, 3.1f, 2.7f),
				new EidrenMeshFactory.LoftProfile(1.75f, 1.9f, 1.7f)
			};
			Teil(root, "CrownLow", Persist(EidrenMeshFactory.Loft(kroneTief, EidrenMeshFactory.LoftShape.Oct, Laub, capBottom: true, capTop: true), "KroneTief"), new Vector3(0f, 3.65f, 0f), Vector3.zero);
			EidrenMeshFactory.LoftProfile[] kroneHoch =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1.6f, 1.5f),
				new EidrenMeshFactory.LoftProfile(0.6f, 2.7f, 2.5f),
				new EidrenMeshFactory.LoftProfile(1.35f, 2f, 1.9f),
				new EidrenMeshFactory.LoftProfile(1.8f, 1f, 0.95f)
			};
			Teil(root, "CrownHigh", Persist(EidrenMeshFactory.Loft(kroneHoch, EidrenMeshFactory.LoftShape.Oct, LaubHell, capBottom: true, capTop: true), "KroneHoch"), new Vector3(-0.35f, 4.85f, 0.1f), Vector3.zero);
		}
		else
		{
			EidrenMeshFactory.LoftProfile[] stumpf =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1.6f, 1.6f),
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
			float height = (active ? (0.82f + (float)(index % 4) * 0.12f) : (0.17f + (float)(index % 2) * 0.05f));
			Teil(root, $"Stalk_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.09f, height, 0.09f), 0.45f, Hanf), "Halm"), new Vector3(x, 0f, z), new Vector3((float)(index % 3) * 3f, (float)(index * 31 % 360), (float)(index % 2) * 4f));
			if (active)
			{
				Teil(root, $"Head_{index:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.2f, 0.34f, 0.2f), 0.25f, HanfKopf), "Kopf"), new Vector3(x, height - 0.04f, z), Vector3.zero);
			}
		}
		Save(root);
	}

	private static void BuildRock(string stem, bool active, Color host, Color accent, bool crystals)
	{
		GameObject root = new GameObject(stem + "_" + (active ? "Active" : "Exhausted"));
		int count = (active ? 7 : 4);
		for (int index = 0; index < count; index++)
		{
			float height = (active ? (0.48f + (float)(index % 3) * 0.22f) : (0.18f + (float)(index % 2) * 0.1f));
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
				Teil(root, $"OreShard_{i:00}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.16f, 0.58f, 0.16f), 0.12f, accent), "Erzsplitter"), new Vector3(-0.52f + (float)i * 0.25f, 0.43f + (float)(i % 2) * 0.17f, 0.15f - (float)(i % 3) * 0.18f), new Vector3(12f, (float)i * 18f, 18f));
			}
		}
		Save(root);
	}
```

`Save`/`EnsureFolder` bleiben wie im Bestand; benötigte usings ergänzen (`using System;` für `StringComparison`). Die alte `Material`-Methode und `MaterialFolder`-Konstante entfallen ersatzlos (T2Materials-Ordner bleibt liegen).

- [ ] **Step 4: Builder-Lauf** — Einstieg laut Task-1-Dokument, der NUR die T2-Visuals neu baut (voraussichtlich reicht `-executeMethod` einer neuen schmalen Methode oder eines vorhandenen T2-Einstiegs; NICHT `BuildResourceCollection` — der backt alle Zonen-Szenen). Log `stil-e3-t3-build.log`; Dreieckszahlen mit `[StilE3]`-Zeilen loggen (in `Save` einbauen: Summe der MeshFilter wie in E1/E2).
- [ ] **Step 5: Tests laufen lassen** — Filter `Eidren.Tests.EditMode.EidrenWorldStyleTests`, XML `TestResults-stil-e3-t3.xml`. Erwartet: alle (inkl. 8 neue) `Passed`.
- [ ] **Step 6: Prüfnachweis** — Logs, XML, Dreieckszahlen.

---

### Task 4: T2-Zonenvarianten neu klonen, Nodes verifizieren

**Files:** gemäß Task-1-Dokument (voraussichtlich nur ein schmaler neuer Einstieg im Varianten-/AreaArt-Builder).

- [ ] **Step 1:** Den in Task 1 dokumentierten T2-only-Klonweg ausführen (die 12 Varianten entstehen neu aus den neuen Basis-Visuals; `AreaIdentityGeometry`-Aufsätze wie bisher). Tier-1-Varianten: Zeitstempel vor/nach dem Lauf vergleichen — sie dürfen sich NICHT ändern.
- [ ] **Step 2:** Node-Verifikation gemäß Task-1-Antwort (d): Sind die Visuals Prefab-Instanzen in den Nodes, genügt der Basis-Neubau; sonst `NodePrefab` nur für die vier T2-Definitionen neu erzeugen (Weg laut Task-1-Dokument).
- [ ] **Step 3:** Falls Task 1 Szenen-Rebake für nötig erklärt: exakt den dort benannten Einstieg ausführen; sonst ausdrücklich festhalten, dass keiner nötig war.
- [ ] **Step 4:** Voller Stiltest-Lauf + `V02ContainerVisualTests` (Regressionswache), Logs/XMLs `stil-e3-t4…`. Prüfnachweis: Varianten-Zeitstempel (T2 neu, T1 unverändert), Testresultate.

---

### Task 5: Nachher-Captures, In-Welt-Stichprobe, Namensdiff, Abnahme

- [ ] **Step 1:** Nachher-Lauf des Task-2-Werkzeugs (20 PNGs, `stil-e3-t5-nachher.log`), jedes Bild ansehen.
- [ ] **Step 2:** In-Welt-Stichprobe: analog `CaptureInWelt`, aber Szene `Zone_TwilightGrove` öffnen und an zwei per `WorldChestVisual`-freier Suche gewählten Punkten rendern — konkret: je ein Capture eines `HardwoodTree`- und eines `SwampHemp`-Node-Exemplars, gefunden über `Object.FindObjectsByType<ResourceNode>` und Namensvergleich; Kameraversatz `(3.2f, 4.6f, -4.2f)`. Kleine Methode `CaptureT2InWelt` nach dem Muster von `CaptureInWelt` ergänzen; Szene nicht speichern.
- [ ] **Step 3:** Volle EditMode-Suite + Namensdiff gegen `TestResults-stil-e3-baseline.xml` (`TestResults-stil-e3-final.xml`). Erwartet: keine `=>`-Einträge.
- [ ] **Step 4:** `Documentation/Etappen/Stilumbau/STILUMBAU_E3_ABNAHME.md` nach E2-Muster: Kriterien (nicht leer; Silhouetten ± 15 % gewahrt; Active/Exhausted unterscheidbar; Zonenvarianten tragen ihre Identitätsaufsätze weiterhin; Farbwelt = alte Materialfarben; T1 nachweislich unangetastet), Dreieckszahlen, Namensdiff, Assetliste, offene Punkte. Freigabe beim Auftraggeber.

---

## Selbstreview-Vermerk

- Umfangsänderung gegenüber der Spec (nur T2) ist im Kopf dokumentiert und folgt dem freigegebenen Stationen-Präzedenzfall; die Spec erhält bei Etappenabschluss einen Nachtrag.
- Auflagen aus dem E2-Abschlussreview eingearbeitet: Bake-Kette vorab (Task 1), Persist-Verwaisungsschutz (Task 3), T1-Tabu als globale Vorgabe.
- Typkonsistenz: geschachtelte `EidrenMeshFactory.LoftProfile`-Nutzung wie E1/E2; `Teil`-Helfer kapselt MeshFilter/Renderer + gemeinsames Material; Collider-Freiheit der Visuals wird jetzt erstmals per Test erzwungen (war im Bestand implizit über `DestroyImmediate(GetComponent<Collider>())`).
- Bewusst offen (kein Platzhalter, sondern Task-1-Auftrag): der exakte T2-only-Einstiegspunkt für Klonen/Bake — er ist Ergebnis der mandatierten Analyse und wird vor Task 3/4 schriftlich fixiert.
