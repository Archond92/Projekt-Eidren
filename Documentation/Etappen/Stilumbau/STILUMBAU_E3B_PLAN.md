# Stilumbau Etappe 3b — Tier-1-Ressourcen: Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ziel:** Die fünf Tier-1-Ressourcentypen (Tree, StoneDeposit, FiberPlant, CopperVein, BerryBush) rendern in Active/Exhausted im Vertexfarben-Stil; ihre Zonenvarianten werden neu geklont, die fünf Node-Prefabs neu gebacken. Auftraggeber-Entscheid vom 08.08.2026: voller Umbau aller fünf Typen (die drei Sprite-Kreuz-Hybriden Tree/BerryBush/FiberPlant UND die zwei echten 3D-Felsen), Präzedenzfälle „T1 tabu" sind damit für die Ressourcen aufgehoben.

**Einordnung:** Tree/BerryBush/FiberPlant sind gemalte 2D-Sprites auf Kreuz-Ebenen (sichtbare Kreuznaht, eingemaltes G-009-Tuch, G-005-Doppelstamm, G-010-Perspektivmix). Der Umbau zahlt direkt auf die offenen Grafikaufträge G-005/G-009/G-010 ein (deren formale Abnahme bleibt eigenständig). Die alten handgebauten Visuals bleiben vollständig in der Task-0-Sicherung erhalten.

**Architektur:** Neuer `T1ResourceVisualBuilder` (Struktur-Spiegel des umgebauten `T2ResourceVisualBuilder`: `BuildStandalone`-MenuItem, `Persist` mit Verwaisungsschutz unter `Art/Resources/T1Meshes`, `Teil`-Helfer, Fabrik-Geometrie). Downstream-Kette wie in 3a: schmales Varianten-Klonen (`AreaArtAssetBuilder.BuildTierOne()` analog `BuildTierTwo`, nutzt `CloneVariants`), schmaler Node-Rebake (`ResourceContentBuilder.RebuildTierOneNodePrefabs()` analog T2). Kein Szenen-Rebake (Ressourcen spawnen zur Laufzeit, BAKEKETTE Abschnitt c — Task 1 bestätigt das für die T1-Zonen).

**Tech Stack / Globale Vorgaben:** identisch zu `STILUMBAU_E3_PLAN.md` (Pfad quoten, Lockfile, Exit-Code lügt, **Unity-Läufe im Vordergrund eines Tool-Aufrufs** — keine Hintergrund-Wächter, `-runTests` NIE mit `-quit`, Captures ohne `-nographics`, kein git, keine erfundenen Belege, deutsche Kommentare/Tabs, Belege `stil-e3b-…`). Zusätzlich verbindlich aus dem 3a-Abschlussreview: Dreierkette Basis→Klonen→Node-Rebake ist eine atomare Sequenz; `BuildTierTwoResourceCollection` erhält im Zuge von Task 4 einen Warnkommentar („baut ALLE neun Typen"); Baseline für den Namensdiff ist `TestResults-stil-e3-final2.xml` (787/716/71) — **kein neuer Baseline-Lauf** (Zeitspar-Regel).

**Höhenlehre aus 3a (verbindlich):** Task 1 extrahiert die Sollhöhen der zehn `visual.<typ>.<zustand>`-Einträge aus der Größentabelle; Task 3 iteriert Bau ↔ `VisualScaleTests.EveryBakedPrefab_MatchesItsTableHeight` von Beginn an (max. 4 Iterationen). Gekippte Bauteile vergrößern die Hüllbox — Kippungen der Felsen wie in 3a dreiachsig.

---

### Task 0: Sicherung und Vorflug

Wie 3a-Task 0, Ziel `vor-stilumbau-e3b-<stamp>` — diese Sicherung enthält die einzigen Kopien der handgebauten T1-Visuals außerhalb älterer Sicherungen; Listing im Report ausdrücklich die zehn `Visuals/*.prefab` nennen. KEIN Baseline-Testlauf (final2 übernimmt). Lockfile-Check.

---

### Task 1: Bake-Ketten-Nachtrag T1 und Höhenziele

**Files:** Create `Documentation/Etappen/Stilumbau/STILUMBAU_E3B_BAKEKETTE.md`

Beantworten mit Datei:Zeile-Belegen: (a) T1-Varianten-Klonwege (welche `AreaSpec`s sind T1; `CloneVariant`-Quellen; Sonderfall `resource.tree` mit `AddTreeIdentity`/`AddRecognitionMarker` — der Marker ersetzt künftig das eingemalte G-009-Tuch als eigenes Objekt: dokumentieren); (b) Signaturvorschlag `BuildTierOne()` (Spiegel von `BuildTierTwo`, `CloneVariants`-Helfer existiert); (c) Laufzeit-Spawning auch für die fünf T1-Zonen bestätigen (HomeBase!) — falls irgendeine Szene T1-Visuals/Nodes baked referenziert: benennen und Umgang vorschlagen; (d) die zehn Sollhöhen aus `VisualScaleTableBuilder`/Testquelle als Tabelle; (e) welche Node-Prefabs existieren (alle fünf inkl. BerryBush?) und ihre Collider-Quellen. Kein Unity-Lauf, keine Codeänderung.

---

### Task 2: Vorher-Captures formalisieren

Die 10 Übersichts-PNGs vom 08.08. (`TempReview/StilumbauE3/t1-uebersicht/`) sind die Vorher-Basis: nach `TempReview/StilumbauE3b/vorher/` kopieren (Dateisystem, kein Unity). Zusätzlich die T1-Zonenvarianten-Vorher-Bilder erzeugen: `CaptureT1VariantenVorher` MenuItem mit `CapturePrefabListe` über die T1-Varianten unter `Prefabs/Environment/AreaArtVariants/` (Greenwood/Marsh/Quarry/EmberRuins × tree, dazu Marsh_fiber_plant, Quarry_stone_deposit usw. — Liste aus Task 1 (a) übernehmen; zielHoehe/distanz je Basistyp wie t1-uebersicht). Ein Vordergrund-Unity-Lauf, jedes Bild sichten.

---

### Task 3: T1ResourceVisualBuilder

**Files:** Create `Assets/_Game/Editor/T1ResourceVisualBuilder.cs`; Test: `EidrenWorldStyleTests.cs` (+10 TestCases `T1Visual_IstImVertexfarbenStil`, Korridore: Tree_Active ≤ 800, alle übrigen ≤ 500, Muster identisch zu `T2Visual_…`)

Struktur exakt wie der umgebaute `T2ResourceVisualBuilder` (Persist mit Verwaisungsschutz unter `Assets/_Game/Art/Resources/T1Meshes`, Namensmuster `T1_{NNN}_{Teilname}`, `Teil`-Helfer, `BuildStandalone` MenuItem `Eidren/V0.2/Stilumbau/T1-Visuals bauen`, `[StilE3b]`-Dreieckslog in `Save`). Prefabpfade/Wurzelnamen unverändert (`Assets/_Game/Prefabs/Resources/Visuals/Tree_Active.prefab` usw.); Visuals colliderfrei.

Geometrie (Höhen sind Startwerte — gegen die Task-1-Zieltabelle iterieren):

- **Tree** (Greenwood-Charakter, unterscheidbar vom Hartholzbaum): Stamm Okt-Loft 4 Profile (Basis 1,1 → 0,55, leichter Schwung), zwei Astansätze (TaperedBox, gekippt), Krone DREI gestufte Okt-Lofts in zwei Grüntönen (satter als Hartholz: `Laub (0.16, 0.34, 0.15)`, `LaubHell (0.28, 0.46, 0.2)`, Rinde `(0.24, 0.14, 0.08)`); Exhausted: Stumpf-Loft + zwei Splitter.
- **BerryBush**: gedrungener Kuppel-Loft (Okt, 3 Profile) in Laubgrün + 8–10 Beeren-Knollen (kleine TaperedBoxes 0,08–0,12, `Beere (0.62, 0.12, 0.14)`) auf der Kuppel verteilt (deterministisches Index-Muster wie Hanf-Halme); Exhausted: flacher kahler Kuppelrest ohne Beeren.
- **FiberPlant**: 9 schmale Halm-TaperedBoxes (Basis 0,06, topScale 0,3, Fächer-Kippung ±12°) in `Faser (0.42, 0.5, 0.28)` mit hellen Rispen-Köpfen (`Rispe (0.72, 0.66, 0.45)`, Höhe 0,16); Exhausted: 5 kurze Stoppeln.
- **StoneDeposit**: wie `BuildRock` (3a, dreiachsig gekippt) ohne Kristalle, Farben `Stein (0.44, 0.47, 0.5)` / `SteinHell (0.58, 0.61, 0.64)`.
- **CopperVein**: `BuildRock`-Muster mit Kristallen, Wirt `(0.35, 0.3, 0.27)`, Erz `Kupfer (0.72, 0.4, 0.2)`.

Ablauf: 10 failing Tests → RED-Lauf → Builder implementieren → `BuildStandalone` (Vordergrund) → Höhentest-Iteration bis grün (max. 4; Zahlenverlauf dokumentieren) → kombinierter Lauf Stiltests+VisualScale (`-testFilter "…EidrenWorldStyleTests;…VisualScaleTests"`, OHNE `-quit`) → alles `Passed`.

---

### Task 4: T1-Varianten klonen, Nodes neu backen

`AreaArtAssetBuilder.BuildTierOne()` und `ResourceContentBuilder.RebuildTierOneNodePrefabs()` gemäß Task-1-Vorschlägen (Spiegel der T2-Einstiege; MenuItems `Eidren/V0.2/Stilumbau/`). Warnkommentar an `BuildTierTwoResourceCollection` ergänzen (3a-Auflage). Zeitstempelwache: erweiterter Snapshot (Varianten, Nodes, ALLE Visuals, `Data/AreaArt`, `Data/Zones`, `.meta`) — nur die erwarteten T1-Dateien (Varianten laut Task-1-Liste + 5 Nodes) dürfen sich ändern; T2-Bestand und Datenassets unverändert. Danach kombinierter Regressionslauf (Stil + VisualScale + `Eidren.Tests.V02ContainerVisualTests`).

---

### Task 5: Nachher-Captures, Namensdiff, Abnahme

Gebündelter Capture-Lauf (ein Unity-Start): T1-Visuals nachher + T1-Varianten nachher + In-Welt-Stichprobe `Zone_Greenwood` (Tree- und BerryBush-Node instanziieren wie `CaptureT2InWelt`, Kameraversatz für den hohen Baum (4.5, 6.5, −6.0)). Volle EditMode-Suite (final3) + Namensdiff gegen `TestResults-stil-e3-final2.xml` — keine `=>`-Einträge; `VisualScaleTests`-Eintrag darf NICHT neu auftauchen. `STILUMBAU_E3B_ABNAHME.md`: Kriterien wie 3a plus „Kreuznaht/eingemaltes Tuch nicht mehr vorhanden; Baum hat genau EINEN Stamm (G-005-Bezug)"; Hinweis, dass die formale G-005/G-009/G-010-Abnahme eigenständig bleibt. Freigabe beim Auftraggeber.

---

## Selbstreview-Vermerk

Umfang per Auftraggeber-Entscheid 08.08. (alle fünf Typen) dokumentiert; Spec erhält bei Abschluss einen Nachtrag an der Etappe-3-Zeile. Höhen-Iteration, Vordergrund-Läufe, atomare Dreierkette, Baseline-Wiederverwendung und Warnkommentar-Auflage aus 3a sind eingearbeitet. Geometriewerte sind ausdrücklich Startwerte einer testgetriebenen Iteration — der Zahlenverlauf gehört in den Report.
