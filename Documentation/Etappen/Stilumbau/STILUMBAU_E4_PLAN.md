# Stilumbau Etappe 4 — Props: Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ziel:** Alle 16 SP_-Props rendern im Vertexfarben-Stil. Auftraggeber-Entscheid 09.08.2026: voller Umbau — die 9 Sprite-Kreuz-Hybriden (SP_Tree_A/B/C, SP_Plant_Bush/Fern/Flowers, SP_GroundCover_Grass/Moss, SP_Accent_GlowMushrooms) UND die 7 bereits echten 3D-Props (SP_Rock_Small/Medium/Large, SP_RuinWall_A/B, SP_RuinMonument, SP_EidrenRune) für volle Pipeline-Einheitlichkeit.

**Tech Stack / Globale Vorgaben:** identisch zu `STILUMBAU_E3B_PLAN.md`, plus die dort etablierten Regeln in Kurzform: NUR `C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe` (voller Pfad in jedem Dispatch; `GetInstanceID`-Fehler = falscher Editor → STOPP); Unity-Läufe im Vordergrund; `-runTests` nie mit `-quit`; Captures ohne `-nographics`; Lockfile-Wache; kein git; keine erfundenen Belege; deutsche Kommentare/Tabs; Belege `stil-e4-…`. Baseline für den Namensdiff: `TestResults-stil-e3b-final2.xml` (805/734/71). **Regressionsfilter ab Task 2 immer inklusive `VisualAssetTests`** (E3b-Lehre: der schmale Filter versteckte den Bruch bis zur Abnahme) und `StyleProof`-bezogener Testklassen (Task 1 benennt sie).

**Bestandsverträge:** Prefabpfade/Namen unter `Assets/_Game/Prefabs/Environment/StyleProof/` unverändert; die Collider der **9** Collider-Props (3 Rocks, 2 RuinWalls, RuinMonument, 3 Trees — Korrektur laut BAKEKETTE (c), Plan sprach irrtümlich von 10) bleiben in Typ, Maßen und Center exakt erhalten (Werte aus der BAKEKETTE-Tabelle, im Builder explizit setzen); Props ohne Collider bleiben colliderfrei. `StyleProofVisualLibrary`-Verdrahtung bleibt funktional.

---

### Task 0: Sicherung

Wie E3b-Task 0, Ziel `vor-stilumbau-e4-<stamp>`; Listing muss die 16 SP_-Prefabs zeigen (Original-Handbau!). Kein Baseline-Lauf. Lockfile-Check.

### Task 1: Bake-Kette Props

**Create `Documentation/Etappen/Stilumbau/STILUMBAU_E4_BAKEKETTE.md`** (Datei:Zeile-Belege): (a) Wer baut die SP_-Prefabs heute (`StyleProofContentBuilder` — welcher Teil), und welcher SCHMALE Einstieg baut NUR die 16 Prop-Prefabs neu, ohne `BuildGreenwood`/Volume/Materialseiteneffekte (Vorschlag mit Signatur, Spiegel der E3-Muster)? (b) Wie kommen Props in die Zonen — gebacken in (binären) Szenen oder zur Laufzeit? Falls gebacken: WELCHE Szenen, und ein belastbares Beweisverfahren für „nur beabsichtigte Szenenänderungen" (Vorschlag: Editor-Skript zählt vor/nach dem Rebake je Szene die PrefabInstanzen pro SP_-GUID und schreibt einen Report — kein Text-Diff nötig; GUIDs bleiben stabil, daher ist zu erwarten, dass gebackene Instanzen die neuen Visuals automatisch zeigen und ein Szenen-Rebake ENTFÄLLT — genau das ist zu belegen). (c) Collider-Maße aller 10 Collider-Props als Tabelle. (d) Höhen-/Größenvorgaben: stehen SP_-Props in der `VisualScaleTable` oder in `StyleProofVisualLibrary`-Erwartungen? Zieltabelle mit Quellenangabe; wo keine Vorgabe existiert, gilt die Ist-Silhouette ±15 % (aus den Übersichtsbildern/AABBs der alten Prefabs — Task-Ausführender misst die Alt-AABBs VOR dem Umbau und schreibt sie in die Tabelle). (e) Welche Testklassen decken StyleProof/Props ab (für den Regressionsfilter).

### Task 2: Vorher-Captures

Die 16 Übersichts-PNGs (09.08., `TempReview/StilumbauE4/uebersicht/`) nach `TempReview/StilumbauE4/vorher/` kopieren (Dateikopie). Kein weiterer Lauf nötig (Props haben keine Zustände/Varianten).

### Task 3: StyleProof-Prop-Builder auf die Fabrik

**Files:** Modify `Assets/_Game/Editor/StyleProofContentBuilder.cs` (bzw. neuer schmaler Builder laut Task 1); Test: `EidrenWorldStyleTests.cs` (+16 TestCases `Prop_IstImVertexfarbenStil`, Muster wie T1/T2: Vertexfarben, Materialname `M_EidrenWorld_VertexLit`, Dreieckskorridor ≤ 500 je Prop, Collider-Erwartung je Prop laut Task-1-Tabelle — Collider-Props behalten exakt ihren Collider, die übrigen sind colliderfrei).

Persist-Muster wie T1 (`Assets/_Game/Art/StyleProof/Meshes`, `SP_{NNN}_{Teilname}`, Verwaisungsschutz). Geometrie (Fabrik-Primitive, Höhen aus der Task-1-Zieltabelle, testgetrieben max. 4 Iterationen):

- **SP_Tree_A/B/C**: wie der T1-Baum (Okt-Stamm-Loft + gestufte Kronen-Lofts), aber drei unterscheidbare Silhouetten: A schlank-hoch (2 Kronenstufen), B breit (3 Stufen, versetzt), C knorrig (gekrümmter Stamm via OffsetX, 2 asymmetrische Stufen); Grüntöne der T1-Palette, Rinde (0.24, 0.14, 0.08).
- **SP_Plant_Bush**: Kuppel-Loft wie BerryBush ohne Beeren, Laub (0.16, 0.34, 0.15).
- **SP_Plant_Fern**: 6 gefächerte flache TaperedBoxes (topScale 0.2, Kippung 30–55°), Farn (0.2, 0.4, 0.18).
- **SP_Plant_Flowers**: 5 dünne Stiele + Blüten-Knollen in zwei Akzentfarben (0.85, 0.75, 0.3) und (0.75, 0.35, 0.5).
- **SP_GroundCover_Grass/Moss**: flache Cluster aus 8–12 kleinen Kegeln (Grass, topScale 0.15) bzw. 5–7 flachen Kuppel-Lofts (Moos (0.2, 0.36, 0.16)); Gesamthöhe ≤ 0.2.
- **SP_Accent_GlowMushrooms**: 4 Pilze (Stiel-TaperedBox + Okt-Kappen-Loft), Kappen in Glühfarbe (0.45, 0.75, 0.7) — der Shader hat kein Emissive, die helle Vertexfarbe trägt den Akzent; im Abnahmebericht ausweisen.
- **SP_Rock_Small/Medium/Large**: `BuildRock`-Muster (dreiachsig gekippte Okt-Lofts), 2/3/5 Felsen, Grau (0.44, 0.47, 0.5)/(0.58, 0.61, 0.64).
- **SP_RuinWall_A/B**: verwitterte Mauern — 2 Lagen TaperedBoxes mit unregelmäßiger Oberkante (2–3 Wedge-Zinnen, Fabrik-`Wedge`!), Stein (0.5, 0.48, 0.44); B gespiegelt/kürzer.
- **SP_RuinMonument**: Sockel-Loft + 2 gestapelte, leicht verdrehte TaperedBoxes + gebrochene Spitze (Wedge), Stein + Akzent (0.42, 0.35, 0.22).
- **SP_EidrenRune**: stehende Platte (TaperedBox) + 3 eingesetzte Akzent-Boxen als Runenzeichen, Platte (0.35, 0.37, 0.42), Runen-Akzent (0.45, 0.75, 0.7).

TDD wie gehabt (RED → Builder → Höhen-/Silhouetten-Iteration → kombinierter Lauf mit den Task-1-Testklassen + `VisualAssetTests` + `VisualScaleTests` + `EidrenWorldStyleTests` — alles `Passed` bzw. Alt-Fehlschläge unverändert).

### Task 4: Platzierungs-Beleg

Gemäß Task-1-Verfahren: PrefabInstanz-Zählung je Szene vor/nach (falls Szenen betroffen; erwartet: keine Szenenänderung nötig, GUIDs stabil — Beleg erbringen). Zeitstempelwache über StyleProof-Prefabs/-Meshes + Szenen + Data. Regressionslauf (Filter aus Task 1 + `V02ContainerVisualTests`).

### Task 5: Abnahme

Gebündelter Capture-Lauf: 16 Nachher-Studio + In-Welt-Stichprobe Zone_Greenwood (SP_Tree_A und SP_Plant_Bush temporär instanziieren, Baumkamera (4.5, 6.5, −6.0)). Volle Suite (`TestResults-stil-e4-final.xml`) + Namensdiff gegen `TestResults-stil-e3b-final2.xml` — keine `=>`. `STILUMBAU_E4_ABNAHME.md` (Kriterien: nicht leer; ein Stamm/keine Kreuznaht bei den 9 Ex-Sprites; A/B/C-Bäume unterscheidbar; Collider unverändert per YAML-Diff; Silhouetten ±15 %; Pilz-Glühfarbe als Vertexfarben-Näherung ausgewiesen). Freigabe beim Auftraggeber.

---

## Selbstreview-Vermerk

Umfang per Auftraggeber-Entscheid 09.08. (alle 16). E3b-Lehren verankert: voller Editor-Pfad, Vordergrund-Läufe, `VisualAssetTests` im Filter ab Task 3, Persist-Verwaisungsschutz, Collider-Verträge explizit, Silhouetten-Iteration mit Messtabelle aus Task 1, PrefabInstanz-Zählverfahren statt Binär-Text-Diff. Geometriewerte sind Startwerte der testgetriebenen Iteration.
