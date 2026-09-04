# Stilumbau Etappe 5 — Gebäude: Implementierungsplan (Finale)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ziel:** Die 9 BLD_-Gebäude samt Wand-/Dachvarianten rendern im Vertexfarben-Stil; die fünf gebäudebezogenen Alt-Testmethoden werden nach der Spec-Regel nachgezogen; die In-Welt-Kamera wird höhenparametrisiert. Auftraggeber-Mandat 10.08.2026: „Erledige alles" — vollständige Durchführung inkl. der Abschlussarbeiten (eigener Abschnitt nach Task 6). Alle 9 Gebäude tragen handgebaute `Geometry_A14`-Geometrie (05.08.); per dreifachem Präzedenz-Entscheid (Stationen ausgenommen, T1/Props voll umgebaut) und dem Mandat werden sie neu gebaut; Originale in der Task-0-Sicherung.

**Globale Vorgaben:** wie `STILUMBAU_E4_PLAN.md` (voller Editor-Pfad in jedem Dispatch, Vordergrund-Läufe, `-runTests` nie mit `-quit`, Lockfile, kein git, echte Belege, `Select-String` statt Grep, deutsche Kommentare/Tabs). Belege `stil-e5-…`. Baseline: `TestResults-stil-e4-final2.xml` (Namensdiff-Referenz, 71 bekannte Fehlschläge). **Regressionsfilter ab Task 3 immer inkl. `Eidren.Tests.EditMode.VisualAssetTests`.**

**Maßverträge (härter als bisher):** je Gebäude Höhe (1 %, `VisualScaleTable`), **Breitenbudget** (`VisualScaleTests` für `visual.building.*`) und `Wall_OvertopsThePlayer`. Collider/Footprints/Bauraster byte-genau erhalten (BAKEKETTE-Tabelle). `BuildingRoofView`-/`WallConnection`-Verträge (Dach-Sichtbarkeit, Wandverbinder) funktional unverändert.

---

### Task 0: Sicherung
Wie gehabt (`vor-stilumbau-e5-<stamp>`); Listing muss die 9 BLD_-Prefabs plus Wand-/Dachvarianten-Assets zeigen. Kein Baseline-Lauf.

### Task 1: Bake-Kette Gebäude
**Create `Documentation/Etappen/Stilumbau/STILUMBAU_E5_BAKEKETTE.md`** (Datei:Zeile; `Select-String`): (a) Welche Builder erzeugen die BLD_-Geometrie heute bzw. validieren sie nur (BuildingCostContentBuilder, BuildingPreviewContentBuilder, BuildingRoofContentBuilder, ProductionContentBuilder, WallConnectionContentBuilder, WorldVisualAssetBuilder — Rollen klären); schmaler Neubau-Einstieg vorschlagen (`EidrenBuildingVisualBuilder.BuildStandalone` o. ä., T1/Props-Muster). (b) Platzierung: Laufzeitbau bestätigen (E4-Review-Vorbefund), Zählwerkzeug-Generalisierung (Pfadfilter-Parameter statt hartkodiertem StyleProof-Pfad) für den Beleg; binäre Szenen nur via Zählverfahren. (c) Collider-/Footprint-Tabelle aller 9 + Wandvarianten. (d) Maßziel-Tabelle: Höhe UND Breite je `visual.building.*` (+ Spielerhöhen-Bezug aus `Wall_OvertopsThePlayer`); Toleranzen aus dem Test. (e) Nachzieh-Inventar: die fünf Methoden aus `VisualAssetTests.cs` (M6Buildings, CookingPot, Workbench, Produktionsstationen, RemainingSupport) mit ihren Alt-Erwartungen (Geometry_A14, URP/Lit, Mehrfach-Material-Minima, Heldenteile) und je Methode der neue Fabrik-Vertrag (Muster: E3b-Nachzug — Absicht erhalten, Assertions auf Vertexfarben/gemeinsames Material/Strukturunterschiede; NICHT abschwächen). WICHTIG: Workbench/StorageChest/Stationen (Etappe-2-Entscheid: bleiben handgebaut!) — deren Testeinträge bleiben UNVERÄNDERT auf Geometry_A14; nur die 9 BLD_-Einträge und ggf. CookingPot (ist BLD_!) werden nachgezogen. Präzise Zuordnung, welcher Eintrag zu welcher Gruppe gehört. (f) Dach-/Wandvarianten-Kette: wie hängen `BuildingRoofContentBuilder`/`WallConnectionContentBuilder`-Ausgaben an den BLD_-Prefabs; Rebuild-Reihenfolge.

### Task 2: Kamera-Parametrisierung + Vorher-Captures
`CaptureInWelt`-Familie in `WorldChestStyleCaptureUtility` höhenparametrisieren: Versatz aus `zielHoehe` abgeleitet (`distanz = zielHoehe * 1.4 + 2.5`, Versatz `(0.7*distanz, 1.0*zielHoehe + 2.0, -0.9*distanz)`, LookAt halbe Objekthöhe — Werte im Task verifizieren/feinjustieren, sodass ein 6-m-Baum UND ein 0,3-m-Floor voll im Bild sind; die zwei E4-Baum-Inwelt-Bilder als Regressionsprobe neu rendern: Krone jetzt vollständig). Danach Studio-Vorher-Captures der 9 BLD_-Prefabs (`CaptureGebaeudeVorher`, Werte je Gebäude aus der BAKEKETTE-Maßtabelle) + Sichtliste.

### Task 3: Test-Nachzug (VOR dem Geometrie-Umbau)
Die fünf Methoden gemäß BAKEKETTE (e) nachziehen — zweistufig: Assertions so umbauen, dass sie den ALTEN wie den NEUEN Vertrag akzeptieren? NEIN — sauberer: Methoden auf die Fabrik-Verträge für die 9 BLD_-Objekte umschreiben, aber mit den ALTEN Prefabs laufen sie dann ROT. Deshalb Reihenfolge im selben Task: Tests umschreiben (RED gegen Alt-Geometrie dokumentieren) und DIREKT ANSCHLIESSEND Task 4 beginnt. Alternativ (wenn der RED-Zwischenstand die Kette stören würde): Tests erst in Task 5 nach dem Neubau nachziehen — der Task-Ausführende entscheidet nach Lage und dokumentiert die gewählte Reihenfolge. Nicht-BLD-Einträge (Workbench, StorageChest, Produktionsstationen laut Zuordnung) bleiben unangetastet.

### Task 4: Gebäude-Builder
Neuer Builder nach etabliertem Muster (Persist-Verwaisungsschutz unter `Art/Buildings/Meshes`, `BLD_{NNN}_{Teilname}`, `[StilE5]`-Log, MenuItem `Eidren/V0.2/Stilumbau/Gebaeude bauen`). Geometrie-Leitlinien (Werte gegen Maßtabelle iterieren, max. 4): Wand = Plankenlagen + Fachwerk-Streben (TaperedBoxes) + Verbinderkanten erhalten; Floor = flache Plankenplatte; Door = Rahmen + Türblatt (Scharnier-Transform erhalten, falls vorhanden); FarmPlot = Erdbett-Loft + Einfassung; CookingPot = Topf-Okt-Loft + Gestell; Sawmill/Smelter/Stonecutter/Ropewalk = charakteristische Silhouetten aus je 6–12 Fabrikteilen (Säge: Block+Blatt-Wedge; Schmelze: Ofen-Loft + Kaminwedges + Glut-Akzent (0.88, 0.31, 0.075); Steinschneider: Block + Klingenrad-Okt; Seilerei: Gestellrahmen + Spannbalken). Paletten aus den bestehenden Materialfarben der Alt-Gebäude (im Task auslesen). Dach-/Wandvarianten gemäß BAKEKETTE (f) neu erzeugen. Kombi-Regressionslauf inkl. `VisualAssetTests` + `V02ContainerVisualTests` + BuildingRoof-/WallConnection-Testklassen (BAKEKETTE benennt sie).
Danach direkt: Höhen-/Breiten-Iteration; alle Maßtests grün.

### Task 5: Platzierungs-Beleg + Regression
Generalisiertes Zählwerkzeug über alle Szenen mit BLD_-GUID-Filter (Erwartung laut BAKEKETTE (b): 0 gebackene Instanzen — Laufzeitbau; Beleg schreiben). Zeitstempelwache (nur erwartete Dateien). Volle Suite (`TestResults-stil-e5-final.xml`) + Namensdiff gegen `TestResults-stil-e4-final2.xml`: keine `=>` außer dokumentierten Nachzieh-Umbenennungen (idealerweise keine); nachgezogene Ex-Fehlschläge als `<=` sind erwünscht, falls die zwei StyleProof-Altlasten im Abschluss (unten) fallen.

### Task 6: Abnahme
Gebündelte Nachher-/In-Welt-Captures (parametrisierte Kamera; In-Welt: Wall, Smelter, CookingPot in HomeBase-Kontext temporär instanziert). `STILUMBAU_E5_ABNAHME.md` nach bekanntem Muster (Kriterien: Maße Höhe+Breite+Spielerbezug; Footprints byte-gleich; Dach-/Wandmechanik funktional — BuildingRoofView-/WallConnection-Tests grün; Silhouetten je Gebäude erkennbar; Vorfälle transparent). KEINE Selbst-Freigabe — Bildpaare gehen an den Auftraggeber.

---

## Abschlussarbeiten (nach Task 6, Teil des Mandats „Erledige alles")

- **A1 — StyleProof-Altlasten:** die zwei Baseline-Fehlschläge (`ReusableStyleProofAssets_ArePresentAndCompatible`, `GreenwoodVolume_ContainsRequiredOverrides`) per Spec-Regel nachziehen (Verträge sind durch E4 obsolet: kein Prop nutzt mehr `M_SP_*`-Sprite-Materialien; Volume laut G-003 wirkungslos) — Tests auf den Ist-Vertrag umschreiben, nicht Assets backfillen. Baseline schrumpft (erwünschte `<=`).
- **A2 — Alt-Material-Rückbau:** alle durch den Stilumbau obsoleten Einfarb-Materialien entfernen (WorldChest_*-, Forge-*_Body/Accent/Interior-, RES_*-soweit unbenutzt, T2Materials, StyleProof-Materialordner) — VORHER je Material per `Select-String` über alle Prefabs/Szenen/Builder nachweisen, dass keine Referenz mehr existiert (GUID-Suche); Liste in den Report; die 20 verwaisten Forge-Meshes ebenso. Sicherung vorab (die Task-0-Sicherung deckt es).
- **A3 — Mini-Cleanups:** tote Konstanten (`MaterialRoot`/`PrefabRoot` im Forge-Builder, `VisualFolder` im T2-Builder), temporärer `BaumfixRebuildChain.cs`-Helfer entfernen, BAKEKETTE-/Plan-Restkorrekturen. KEIN Umbau von RuinWall_B (Kenntnisnahme genügt laut Review).
- **A4 — Abschlussdokument:** `STILUMBAU_ABSCHLUSS.md`: Gesamtbilanz aller Etappen (Objekte, Dreiecke, Vorfälle, Namensdiff-Historie, verbleibende bekannte Fehlschläge mit Begründung), Verweisliste aller Abnahmen/Sicherungen.
- Nach A1–A3: letzte volle Suite + Namensdiff (Referenz `stil-e5-final`; erwartete `<=`: die zwei StyleProof-Altlasten).

## Selbstreview-Vermerk
E4-Auflagen eingearbeitet (Kamera-Parametrisierung als Task 2, Nachzieh-Task explizit, VisualAssetTests im Filter, Select-String-Regel). Stationen-Ausnahme aus Etappe 2 im Nachzieh-Inventar geschützt. Reihenfolge-Flexibilität in Task 3 bewusst dokumentiert statt starr verordnet.
