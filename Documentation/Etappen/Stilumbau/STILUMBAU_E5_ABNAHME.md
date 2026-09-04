# Stilumbau Etappe 5 Abnahme — 9 Gebäude im Fabrik-Stil

Stand: 10.08.2026
Bezug: `STILUMBAU_E5_PLAN.md` (Etappe 5: alle 9 `BLD_`-Gebäude auf die
`EidrenMeshFactory`/Vertexfarben-Pipeline; Auftraggeber-Mandat 10.08.2026
„Erledige alles"), Tasks 0–6.
Vorher-Stand: handgebaute `Geometry_A14`-Prefabs (Original-Rekonstruktion,
siehe Memory-Notiz „Eidren-Quellprojekt rekonstruiert"), gesichert unter
`vor-stilumbau-e5-20260810-1518` (Task 0). Baseline für den Namensdiff
(Zeitspar-Regel, kein neuer Baseline-Lauf): `TestResults-stil-e4-final2.xml`
(822 Tests, 751 bestanden, 71 Fehlschläge).

**Diese Abnahme spricht keine Freigabe aus.** Sie legt die Belege vor; die
Entscheidung liegt beim Auftraggeber. Das Mandat „Erledige alles" deckt die
**Durchführung** der Etappe, nicht die Abnahme-Entscheidung selbst — die
Bildpaare (Vorher `TempReview/StilumbauE5/vorher/`, Nachher
`TempReview/StilumbauE5/nachher/`, In-Welt `TempReview/StilumbauE5/inwelt/`)
gehen an den Auftraggeber.

---

## Kurzfassung

Alle 9 `BLD_`-Gebäude sind auf `EidrenMeshFactory`-Geometrie mit Vertexfarben
umgestellt (104–714 Dreiecke, Task 3+4), alle 9 Höhen **und** alle 9
Breitenbudgets auf 1 % Toleranz getroffen, die Wand übertrifft weiterhin die
Spielerhöhe (`Wall_OvertopsThePlayer`, Sollwert 2,6 m ≥ 2,0 m Spieler), alle 9
Wurzel-`BoxCollider` sind per YAML-Diff gegen die Task-0-Sicherung
byte-identisch bestätigt, Dach-/Wandverbinder-Mechanik bleibt funktional
(`BuildingRoofContentTests`/`BuildGridWallConnectionTests` grün), und die 0
gebackenen `BLD_`-Instanzen über alle 11 Projektszenen bestätigen den
Laufzeitbau (kein Szenen-Rebake nötig). Der volle Suite-Namensdiff gegen die
Etappe-4-Baseline zeigt **0 neue und 0 behobene Fehlschläge** (71/71,
byte-identische Namensmengen).

| # | Kriterium | Ergebnis |
| --- | --- | --- |
| 1 | Maße: Höhe + Breite + Spielerbezug | erfüllt, siehe Abschnitt 3 |
| 2 | Footprints/Collider byte-gleich | erfüllt, siehe Abschnitt 4 |
| 3 | Dach-/Wandmechanik funktional | erfüllt, siehe Abschnitt 5 |
| 4 | Silhouetten je Gebäude erkennbar | erfüllt, siehe Abschnitt 1 |
| 5 | Platzierung: 0 gebackene Instanzen | erfüllt, siehe Abschnitt 6 |
| — | Keine Regression (Namensdiff) | erfüllt — 0 `=>`, 0 `<=` gegen `stil-e4-final2.xml` (Baseline 822/751) |
| — | Vorfälle transparent ausgewiesen | erfüllt, siehe Abschnitt 7 |
| — | Freigabe durch Auftraggeber | **aussteht** |

---

## 1. Kombinierter Nachher- + In-Welt-Capture-Lauf (Task 6, EIN Unity-Start)

**Code-Ergänzung** `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs`:
- `CaptureGebaeudeAbnahme()`, MenuItem `Eidren/V0.2/Stilumbau/Gebaeude
  Abnahme-Captures` — Studio-Nachher der 9 `BLD_`-Prefabs (identische
  Liste/Werte wie `CaptureGebaeudeVorher`, Task 2) nach
  `TempReview/StilumbauE5/nachher/`, danach `CaptureGebaeudeInWelt(...)`.
- `CaptureGebaeudeInWelt(string ordner)` — öffnet `HomeBase.unity`
  (BAKEKETTE (b): Gebäude werden dort nie gebacken, daher `Object.Instantiate`,
  kein `PrefabUtility.InstantiatePrefab`, analog `BuildingPlacementController`),
  instanziiert `BLD_Wall_L01` bei `(0,0,0)`, `BLD_Smelter_L01` bei `(5,0,0)`,
  `BLD_CookingPot_L01` bei `(10,0,0)` temporär, rendert jedes mit dem
  höhenparametrisierten `InWeltVersatz(zielHoehe)`-Kameraversatz (Task 2;
  Sollhöhen aus BAKEKETTE (d): Wall 2,6 / Smelter 2,0 / CookingPot 1,0),
  `DestroyImmediate` danach. Szene wird nicht gespeichert.
- **Nachtrag während dieses Tasks:** Ein freistehend instanziiertes
  `BLD_Wall_L01` zeigt standardmäßig **keine** Wandverbinder-Kappen —
  `WallConnectionContentBuilder.Piece` setzt alle vier `Joint_`-/`Cap_`-Kinder
  bei ihrer Erzeugung auf `SetActive(false)` (nur `WallConnectionView.Apply`
  schaltet sie sichtbar). `CaptureGebaeudeInWelt` ruft deshalb nach der
  Instanziierung `verbinder.Apply(new GridWallConnection(GridWallShape.EndCap,
  GridWallShape.EndCap), 0)` auf die Wand-Instanz — exakt der Zustand, den
  `GridWallConnection.Classify` für eine isolierte Wand ohne Nachbarn
  liefern würde (0 belegte Nachbararme je Knoten → `EndCap`/`EndCap`,
  `GridWallConnection.cs:51-54`), und exakt der Aufrufpfad, den
  `BuildingPlacementController.Continuation.cs:786` für eine echte Platzierung
  verwendet. Kein Fabrik-/Testvertrag geändert — reine Capture-Nachbildung des
  Laufzeitverhaltens.

**Lauf:** Lockfile vor dem Start geprüft (`Library/UnityLockfile`,
`Temp/UnityLockfile`, Prozessliste `Unity.exe`) — keins vorhanden, kein
laufender Prozess.

```
"C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode `
  -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
  -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureGebaeudeAbnahme -quit `
  -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e5-t6-captures.log"
```

Ohne `-nographics` (Vorgabe für Captures). Exit 0 (zweimal ausgeführt: einmal
vor, einmal nach dem Wandverbinder-Nachtrag oben — beide Läufe exit 0). `error
CS`: 0 Treffer. `exception` (case-insensitiv): 0 Treffer. Sauberer Exit:
`Exiting batchmode successfully now!`. 12 `[StilE5]`-„geschrieben"-Zeilen —
exakt 9 (Studio-Nachher) + 3 (In-Welt) aus einem Lauf.

**Ergebnis:** `TempReview/StilumbauE5/nachher/` — 9/9 PNGs (14–47 KB, keine
leere/einfarbige Datei), `inwelt/` — 3/3 PNGs (1,35–1,43 MB). **Kriterium 1
(kein leeres Bild) erfüllt.**

### Alle 12 Bilder einzeln gesichtet (per Bildwerkzeug, inkl. Zoom-Ausschnitt auf Wand und Schmelze)

| Datei | Sichtbefund |
| --- | --- |
| `nachher/BLD_Wall_L01.png` | Plankenwand mit 5 sichtbaren Vertikalfugen, mittiger Riegel, dunkle Kappe oben, heller Steinsockel unten — vollständig im Bild, nicht beschnitten |
| `nachher/BLD_Door_L01.png` | Türblatt mit Diagonalstrebe, Schlossblech, Griff, 2 Scharnierbänder — vollständig im Bild |
| `nachher/BLD_Floor_L01.png` | Flache 8-Planken-Bodenplatte mit sichtbaren Nagelpunkten — vollständig im Bild |
| `nachher/BLD_FarmPlot_L01.png` | Erdbett mit 11 Furchen, Einfassungsrahmen, 9 grüne Saatmarker, diagonaler Handrechen — vollständig im Bild |
| `nachher/BLD_CookingPot_L01.png` | Kessel mit Lippe, 3-beiniges Gestell, 9 Herdsteine im Ring, Glutakzent — vollständig im Bild |
| `nachher/BLD_Smelter_L01.png` | Ofenkorpus mit Kamin+Kaminkappe, Balg, Tiegel+Kupferbarren, **Glutakzent deutlich als leuchtend-oranger Balken vor dem Feuerloch sichtbar** (Zoom-Ausschnitt geprüft) — vollständig im Bild |
| `nachher/BLD_Sawmill_L01.png` | Gestell mit 2 Stützen, Überkopfbalken, geneigtes Sägeblatt (Wedge) über Stammholz, Kurbel — vollständig im Bild |
| `nachher/BLD_Ropewalk_L01.png` | Gestellrahmen mit Spannbalken, 3 gedrehte Seilstränge, Antriebsrad, fertige Seilrolle — vollständig im Bild |
| `nachher/BLD_Stonecutter_L01.png` | Schneideblock mit Steinplatte, Klingenrad (Okt-Loft), Achse+Kurbel — vollständig im Bild |
| `inwelt/BLD_Wall_L01.png` | In `HomeBase.unity`, umgeben von platzierbaren Requisiten (Bäume, Gras-Cluster): Wand freistehend, **beide Kappen (`Cap_MinusX`/`Cap_PlusX`) als helle Verbinderstücke links und rechts der Plankenfläche sichtbar** (Zoom-Ausschnitt geprüft, EndCap-Zustand nach `Apply`) — nicht beschnitten |
| `inwelt/BLD_Smelter_L01.png` | Im Zonenkontext neben der Wand: Ofen samt Kamin, Balg, Glutakzent erkennbar — nicht beschnitten |
| `inwelt/BLD_CookingPot_L01.png` | Im Zonenkontext neben Wand und Schmelze: Kessel samt Dreibein-Gestell und Herdsteinring erkennbar — nicht beschnitten |

Kein Bild zeigt eine abgeschnittene Silhouette, ein leeres/einfarbiges Feld
oder eine fehlende Geometrie. **Kriterium 4 (Silhouetten je Gebäude
erkennbar) erfüllt** — alle 9 Gebäudetypen sind im direkten Bildvergleich
eindeutig unterscheidbar (Wand/Tür: flache Plankenkörper; Boden/Acker:
horizontale Platten; Kessel: rundliche Loft-Form mit Dreibein; Schmelze:
hohe Kegelform mit Kamin; Sägewerk/Seilerei/Steinschneider: je
charakteristische Gestell-/Radsilhouette).

---

## 2. Namensdiff gegen Baseline (Regressionsprüfung)

Task 5 führte den vollen EditMode-Suite-Lauf bereits als Teil des
Task-5-Auftrags durch (`-nographics`, ohne `-quit`); dieser Abschnitt fasst
das für die Abnahme zusammen. `TestResults-stil-e5-final.xml`, Log
`stil-e5-final.log`:

```
<test-run id="2" testcasecount="831" result="Failed(Child)" total="831" passed="760" failed="71" ...>
```

`error CS` im Log: 0 Treffer.

| | gesamt | bestanden | fehlgeschlagen |
| --- | ---: | ---: | ---: |
| Baseline (`TestResults-stil-e4-final2.xml`) | 822 | 751 | 71 |
| Endstand (Task 5) | 831 | 760 | 71 |

`Compare-Object` über die `fullname`-Attribute der `result="Failed"`-Fälle
(PowerShell, Task-5-Report): **leer.** 71 Fehlschläge in beiden Läufen,
identische Namensmengen. **0 `=>` (keine neuen Fehlschläge), 0 `<=` (keine
behobenen Fehlschläge, erwartungsgemäß — die zwei StyleProof-Altlasten werden
laut Plan erst in den Abschlussarbeiten A1 nachgezogen, nicht in Etappe 5).**
**Regressionsfreiheit vollständig erfüllt.**

---

## 3. Kriterium 1 — Maße: Höhe + Breite + Spielerbezug

Quelle: `VisualScaleTableBuilder.AddBuildings()` (BAKEKETTE (d)), Toleranz
1 % Höhe (`VisualScaleTests.cs:94`), +1 % Breitenbudget nur für Gebäude
(`VisualScaleTests.cs:110-116`).

`TestResults-stil-e5-t4.xml` (Task-3+4-Regressionslauf, 107/104/3 — die 3
Fehlschläge sind bekannte Baseline-Altlasten außerhalb des Gebäude-Scopes,
siehe Task-34-Report):

| Test | Ergebnis |
| --- | --- |
| `VisualScaleTests.EveryBakedPrefab_MatchesItsTableHeight` | `Passed` (alle 9 BLD_-Einträge, inkl. GroundPlane-Breitenmessung für Floor/FarmPlot) |
| `VisualScaleTests.EveryBakedPrefab_StaysWithinItsWidthBudget` | `Passed` (alle `visual.building.*`-Einträge) |
| `VisualScaleTests.Wall_OvertopsThePlayer` | `Passed` (Wandhöhe 2,6 m ≥ 2,0 m Spielerhöhe, `AssertPlayerHeightRatio("visual.building.wall", 1f)`) |

Zusätzlich prüfen die 9 neuen, parametrisierten
`EidrenWorldStyleTests.Gebaeude_IstImVertexfarbenStil`-Fälle je Gebäude
Höhe/Breite **und** die exakte Collider-Größe/-Center byte-genau gegen die
BAKEKETTE-(c)-Tabelle (Testnamen tragen die erwarteten Werte direkt, z. B.
`Gebaeude_IstImVertexfarbenStil("Wall",200,1.0f,2.6f,0.2f,0.0f,1.3f,0.0f)`) —
alle 9 `Passed`. **Kriterium 1 erfüllt.**

### Dreieckszahlen je Gebäude (Task 4, finaler Stand)

| Prefab | Dreiecke | Vertices | Teile | Korridor (≤1200) |
| --- | ---: | ---: | ---: | ---: |
| BLD_Wall_L01 | 144 | 288 | 12 | 12 % |
| BLD_Door_L01 | 168 | 336 | 14 | 14 % |
| BLD_Floor_L01 | 216 | 432 | 18 | 18 % |
| BLD_FarmPlot_L01 | 312 | 624 | 26 | 26 % |
| BLD_CookingPot_L01 | 714 | 1566 | 28 | 60 % |
| BLD_Sawmill_L01 | 104 | 210 | 9 | 9 % |
| BLD_Smelter_L01 | 208 | 440 | 12 | 17 % |
| BLD_Stonecutter_L01 | 124 | 260 | 9 | 10 % |
| BLD_Ropewalk_L01 | 140 | 304 | 9 | 12 % |

Spanne 104–714 Dreiecke, kein Gebäude überschreitet den 1200-Dreiecks-Korridor
des Plans. 2 Größen-Iterationen bis vollständig grün (Limit 4, eingehalten —
Floor-Plankenabstand nachjustiert, s. Task-34-Report Abschnitt 4).

---

## 4. Kriterium 2 — Footprints/Collider byte-gleich

`TestResults-stil-e5-t4.xml` bestätigte bereits programmatisch (Abschnitt 3
oben, Collider-Werte in den Testnamen selbst). Zusätzlich manueller YAML-Diff
(Task-3+4-Review, Task-34-Report Abschnitt 5): alle 9 `BoxCollider`-Blöcke
(`m_Size`/`m_Center`) zwischen der Task-0-Sicherung
(`vor-stilumbau-e5-20260810-1519/_Game/Prefabs/Buildings/Level01/`) und dem
Endstand — **alle 9 identisch** (CookingPot, Door, FarmPlot, Floor, Ropewalk,
Sawmill, Smelter, Stonecutter, Wall). `NavMeshObstacle`-Anzahl je Prefab
unverändert (1 je Datei). Wand-/Tür-Verbinderkette nach dem Rebuild per
GUID-Suche verifiziert: `WallConnectionView`- und `OcclusionFadeTarget`-
Skript-GUIDs je 1 Treffer in `BLD_Wall_L01.prefab`/`BLD_Door_L01.prefab`,
alle 4 `Joint_`-/`Cap_`-Kinder in beiden Dateien vorhanden. **Kriterium 2
erfüllt, empirisch per YAML-Diff UND Testassertion doppelt belegt.**

---

## 5. Kriterium 3 — Dach-/Wandmechanik funktional

Rebuild-Kette (BAKEKETTE (f)) vollständig durchlaufen und protokolliert
(Task 4): `EidrenBuildingVisualBuilder.BuildStandalone` (`stil-e5-t4-build3.log`)
→ `WallConnectionContentBuilder.ApplyPieces` (`stil-e5-t4-wallconn2.log`:
„wall connection pieces applied to 2 edge prefabs") →
`BuildingCostContentBuilder.BuildBuildingCosts` (`stil-e5-t4-buildingcost.log`)
→ `BuildingRoofContentBuilder.BuildRoofs` (`stil-e5-t4-roofs.log`), alle vier
Läufe Exit 0, keine `error CS`/Exceptions.

`TestResults-stil-e5-t4.xml`:

| Testklasse | Ergebnis |
| --- | --- |
| `BuildingRoofContentTests` (6 Fälle: Dach-Tile authored/ladbar, physik-/navigationsneutral, 1 Zelle flach, fade-fähig, Edge-Prefabs tragen `OcclusionFadeTarget`, Fade-Mark rührt Physik nicht an) | alle `Passed` |
| `BuildGridWallConnectionTests` (Topologie-Logik `GridWallShape`/`GridWallConnection.Classify`) | alle `Passed` |

Material-Referenzen der Wand-/Tür-Geometrie nach dem Rebuild vollständig auf
`M_EidrenWorld_VertexLit` umgestellt (16 Treffer `BLD_Wall_L01.prefab`, 18
`BLD_Door_L01.prefab` — `WallConnectionContentBuilder.SharedMaterial` liest
deterministisch das neue Fabrik-Material). Task 6 bestätigt zusätzlich
**visuell**, dass `WallConnectionView.Apply` die Kappen tatsächlich sichtbar
schaltet (Abschnitt 1, `inwelt/BLD_Wall_L01.png`). **Kriterium 3 erfüllt.**

---

## 6. Platzierungs-Beleg (Task 5, hier zusammengefasst)

`BuildingPlacementReport.CountAndReport()` (rein lesend, keine Szene
gespeichert) zählte in allen 11 Projektszenen (8 Zonenszenen + `Bootstrap`,
`MainMenu`, `EidraForge`) jede `PrefabInstance`-Wurzel unter
`Assets/_Game/Prefabs/Buildings/Level01/`:

**Ergebnis (`TempReview/StilumbauE5/platzierungsbeleg.txt`): 0 Instanzen in
allen 11 Szenen, Gesamtsumme 0.** Bestätigt BAKEKETTE (b):
`BuildingPlacementController.SpawnInstance` nutzt ausschließlich
`Object.Instantiate` zur Laufzeit aus persistiertem `BuildingInstanceState`
— kein Gebäude ist je in eine Szene gebacken. Zeitstempelwache: vollständig
leer (kein `SaveScene()` während des Zähllaufs). **Kriterium 5 erfüllt.**

---

## 7. Vorfälle und Abweichungen (transparent ausgewiesen)

**1. Zwei adjudizierte Testvertrag-Abweichungen (Task 3+4, Controller-Review
bestätigt, siehe `progress.md` Zeile 6):**
- **CookingPot-Vertexminimum 2600 → 1400:** Flach schattierte Fabrik-Geometrie
  hat ein Vertices/Dreiecke-Verhältnis von ca. 2:1; 2600 Vertices würden ca.
  1300 Dreiecke voraussetzen — mehr als der 1200-Dreiecks-Korridor je Gebäude
  erlaubt. Ersetzt durch den tatsächlichen, gemessenen Fabrik-Ertrag (1566
  Vertices bei 714 Dreiecken, Sicherheitsabstand auf 1400 gesetzt). Teilezahl
  (≥28) blieb exakt erhalten.
- **Mindestteilzahl der 4 Produktionsstationen 28/27/20/22 → 12/9/9/9:** Der
  Plan (Task 4) gibt für Smelter/Sawmill/Ropewalk/Stonecutter explizit „6–12
  charakteristische Fabrikteile" vor — präziser als die generische
  BAKEKETTE-(e)-Beispielliste. Neue Mindestzahl auf den tatsächlichen Ertrag
  kalibriert (Smelter 12, die anderen drei je 9).
  Controller-Adjudikation: **beide Abweichungen akzeptiert** — die
  Plan-Geometrievorgabe regiert, die Testböden folgen der realen Geometrie,
  der Schutz gegen Löschung/Entartung bleibt über Name + Vertexfarben-Pflicht
  + gemeinsames Material vollständig erhalten (keine Abschwächung im Sinne
  des Auftrags).

**2. Verify-Nachzug (Task 5, unabhängiger Altlast-Fund):**
`WorldVisualAssetBuilder.Verify` prüfte noch den alten Ressourcen-Vertrag
(Pflicht-`SpriteRenderer`) und brach dadurch `ProductionContentBuilder` an
`BerryBush_Active.prefab` (seit Etappe 3b Fabrik-Mesh statt Sprite) mit
`InvalidOperationException` ab — ein bereits während Task 3+4 als „STALE
CONTRACT, kein paralleler Task" dokumentierter Fund (`progress.md` Zeile 5).
Task 5 hat `Verify()` auf einen Fabrik-**oder**-Handbau-**oder**-Sprite-
Kontrakt erweitert (nicht abgeschwächt: ein Visual ganz ohne erkennbare
Geometrie/Sprite fällt weiterhin durch). Danach lief
`ProductionContentBuilder.BuildProductionContent` erstmals vollständig durch
(`stil-e5-verify-fix.log`, Exit 0). Task 4 hatte den betroffenen
Kettenschritt zuvor per dokumentiertem Workaround (`BuildingCostContentBuilder.
BuildBuildingCosts` direkt statt über `ProductionContentBuilder`) umgangen,
ohne den Fund zu verdecken.

**3. Leere Paketabschnitte im Task-3+4-Review-Paket (Prozessnotiz):** Das
automatisch erzeugte Review-Paket `task-34-package.txt` enthielt für die
Abschnitte „DIFF VisualAssetTests.cs" und „DIFF EidrenWorldStyleTests.cs"
keine Diff-Zeilen — ein **Verpackungsfehler des ausführenden Controllers**
(falscher Logname beim Sammellauf), nicht ein fehlender Code-Diff selbst; der
Reviewer rekonstruierte den Diff unabhängig aus dem Arbeitsstand
(`progress.md` Zeile 6). Kein inhaltlicher Verlust, hier zur Transparenz
festgehalten.

**4. Kleinerer, zurückgestellter Minor-Befund (aus dem Task-3+4-Review):**
Ein stale gewordener Geometrie-Kommentar „(29 Teile)" beim CookingPot-Builder
wurde im Review als Minor markiert, aber nicht in dieser Etappe korrigiert
(kein funktionaler Einfluss — die tatsächliche Teilezahl ist 28 und wird per
Test geprüft, nicht per Kommentar).

**5. `OcclusionFadeTarget` ohne erkennbaren Konsumenten (BAKEKETTE (f),
vorbestehend, nicht durch Etappe 5 verursacht):** Kein Verwendungscode im
rekonstruierten Quellbaum gefunden (0 Treffer außer Definition und Builder).
Komponente bleibt auf `Wall`/`Door` erhalten (per Test abgesichert), Zweck im
aktuellen Code nicht sichtbar — vorbestehende Beobachtung, hier nur erneut
festgehalten.

Alle fünf Punkte sind dokumentierte, keine verdeckten Abweichungen — jede ist
entweder Controller-adjudiziert (1), durch einen unabhängigen Fix behoben (2)
oder als reine Prozess-/Kommentarnotiz ohne Funktionsfolge markiert (3–5).

---

## 8. Erzeugte / geänderte Assets (gesamte Etappe 5, Tasks 0–6)

**Geändert (Editor-Werkzeuge)**
- `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs` — `InWeltVersatz`-
  Helfer, `CapturePropsInWeltObjekt`-Signatur (Task 2), `CaptureGebaeudeVorher`,
  `CaptureE5Task2` (Task 2), `CaptureGebaeudeAbnahme`, `CaptureGebaeudeInWelt`
  (neu, dieser Task)
- `Assets/_Game/Editor/Tests/VisualAssetTests.cs` — 4 der 5 BAKEKETTE-(e)-
  Methoden nachgezogen (Task 3+4)
- `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` —
  `Gebaeude_IstImVertexfarbenStil` mit 9 Testfällen ergänzt (Task 3+4)
- `Assets/_Game/Editor/WorldVisualAssetBuilder.cs` — `Verify()` + 3 neue
  private Helfer (Task 5, Verify-Nachzug)

**Neu**
- `Assets/_Game/Editor/EidrenBuildingVisualBuilder.cs` (Task 3+4)
- `Assets/_Game/Editor/PlacementReportCore.cs` (Task 5)
- `Assets/_Game/Editor/BuildingPlacementReport.cs` (Task 5)

**Prefabs (Task 3+4, in-place überschrieben, gleiche Pfade/GUIDs)**
- `Assets/_Game/Prefabs/Buildings/Level01/BLD_*_L01.prefab` (9 Stück,
  104–714 Dreiecke, Abschnitt 3; Collider/Komponenten unverändert, Abschnitt 4)
- `Assets/_Game/Art/Buildings/Meshes/` — neue Mesh-Assets

**Nicht verändert (per Zeitstempel-/YAML-Diff belegt)**
- Alle 11 `.unity`-Szenendateien (Abschnitt 6)
- `BuildingCostDefinition`-Assets (nur idempotent resaved, kein Inhaltsdiff
  außer den Task-5-Verify-Fix-Resaves, dokumentiert im Task-5-Report)

**Bildmaterial**
- `TempReview/StilumbauE5/vorher/` — 9 PNGs (Task 2, Ausgangszustand)
- `TempReview/StilumbauE5/nachher/` — 9 PNGs (Task 6, dieser Bericht)
- `TempReview/StilumbauE5/inwelt/` — 3 PNGs (Task 6, dieser Bericht)
- `TempReview/StilumbauE5/platzierungsbeleg.txt` — Zählreport (Task 5)

**Testergebnisse (Projektwurzel)**
- `TestResults-stil-e4-final2.xml` (Baseline, aus Etappe 4 übernommen)
- `TestResults-stil-e5-t4.xml` (Task 3+4 kombinierter Regressionslauf,
  107/104/3)
- `TestResults-stil-e5-verifyfix.xml` (Task 5, Verify-Nachzug-Style-Test,
  56/56)
- `TestResults-stil-e5-final.xml` / `stil-e5-final.log` (Task 5, voller
  Suite-Lauf — 831/760/71, Namensdiff 0 `=>`/0 `<=`, maßgeblicher Endstand)
- `stil-e5-t6-captures.log` (Task 6, kombinierter Nachher-/In-Welt-Capture-Lauf)

**Dokumentation**
- `Documentation/Etappen/Stilumbau/STILUMBAU_E5_BAKEKETTE.md` (Task 1)
- `Documentation/Etappen/Stilumbau/STILUMBAU_E5_ABNAHME.md` (dieser Bericht, Task 6)

**Sicherung**
- `vor-stilumbau-e5-20260810-1518` (Quellstand vor der Etappe, Task 0)

---

## 9. Deferred Minors / Offene Punkte

1. **Zwei vorbestehende `StyleProofContentTests`-Fehlschläge** bleiben in der
   71er-Failed-Menge unverändert offen — außerhalb des Etappe-5-Scopes, laut
   Plan erst in den Abschlussarbeiten A1 nachzuziehen (Abschnitt 7 „Vorfälle").
2. **Stale Geometrie-Kommentar „(29 Teile)"** beim CookingPot-Builder
   (Abschnitt 7, Punkt 4) — rein kosmetisch, kein Funktionsbezug, für die
   Abschlussarbeiten (A3 Mini-Cleanups) vorgemerkt.
3. **`OcclusionFadeTarget` ohne erkennbaren Konsumenten** (Abschnitt 7, Punkt
   5) — vorbestehende Beobachtung, kein Etappe-5-Defekt.
4. **Abschlussarbeiten A1–A4** (StyleProof-Altlasten-Nachzug,
   Alt-Material-Rückbau, Mini-Cleanups, Abschlussdokument) sind laut Plan
   eigener, nach Task 6 folgender Arbeitsabschnitt desselben Mandats — nicht
   Teil dieser Abnahme, hier nur zur Vollständigkeit referenziert.
5. **Freigabe durch den Auftraggeber steht aus.** Dieser Bericht weist alle
   fünf Plan-Kriterien sowie vollständige Regressionsfreiheit nach
   (Kurzfassung-Tabelle). Vor einer Entscheidung sind dem Auftraggeber die 12
   Bilder aus `TempReview\StilumbauE5\{nachher,inwelt}\`, der Namensdiff aus
   Abschnitt 2 und die fünf transparent ausgewiesenen Vorfälle (Abschnitt 7)
   vorzulegen.

---

## 10. Nachvollziehen

Kombinierten Nachher-/In-Welt-Capture-Lauf neu erzeugen:

    & "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode `
      -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
      -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureGebaeudeAbnahme -quit `
      -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e5-t6-captures.log"

Volle EditMode-Suite (ohne `-quit`, siehe Abschnitt 2):

    & "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics `
      -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
      -runTests -testPlatform EditMode -testResults "TestResults-stil-e5-final.xml" `
      -logFile "stil-e5-final.log"

Namensdiff siehe Abschnitt 2 für die vollständige PowerShell-Funktion (Muster
identisch zu allen vorherigen Etappen-Abnahmen).
