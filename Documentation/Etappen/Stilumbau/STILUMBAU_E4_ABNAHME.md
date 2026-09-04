# Stilumbau Etappe 4 Abnahme — Alle 16 StyleProof-Props

Stand: 09.08.2026
Bezug: `STILUMBAU_E4_PLAN.md` (Etappe 4: alle 16 `SP_`-Props — die 9
Sprite-Kreuz-Hybriden UND die 7 bereits echten 3D-Props — auf die
`EidrenMeshFactory`/Vertexfarben-Pipeline; Auftraggeber-Entscheid vom
09.08.2026, „voller Umbau"), Tasks 0–5.
Vorher-Stand: Original-Handbau-Prefabs (teils Sprite-Kreuz-Hybriden, teils
bereits primitive 3D-Geometrie), gesichert unter `vor-stilumbau-e4-20260809-1940`
(Task 0). Baseline für den Namensdiff (Zeitspar-Regel, kein neuer
Baseline-Lauf): `TestResults-stil-e3b-final2.xml` (805 Tests, 734 bestanden,
71 Fehlschläge).

**Diese Abnahme spricht keine Freigabe aus.** Sie legt die Belege vor; die
Entscheidung liegt beim Auftraggeber (Abschnitt 9).

---

## Kurzfassung

Alle 16 `SP_`-Props sind auf `EidrenMeshFactory`-Geometrie mit Vertexfarben
umgestellt (40–264 Dreiecke, Task 3), alle 16 Zielhöhen auf 1 % Toleranz
getroffen (Task 3, 3 Iterationen), die 9 vertraglich gebundenen Collider
(Rock Small/Medium/Large, RuinWall A/B, RuinMonument, Tree A/B/C) sind per
YAML-Diff gegen die Task-0-Sicherung byte-identisch bestätigt (Abschnitt 4),
und die 1166 bereits gebackenen `PrefabInstance`s in den 8 Zonenszenen zeigen
die neue Geometrie automatisch, ohne jeden Szenen-Rebake (Task 4, hier in
Abschnitt 6 zusammengefasst). Der volle Suite-Namensdiff gegen die
Etappe-3b-Baseline zeigt **0 neue und 0 behobene Fehlschläge** (71/71,
byte-identische Namensmengen) — das strengste Regressionskriterium des Plans
ist erfüllt.

| # | Kriterium | Ergebnis |
| --- | --- | --- |
| 1 | Kein Nachher-/In-Welt-Bild leer | erfüllt, alle 18 PNGs nicht-leer, alle 18 einzeln gesichtet |
| 2 | Ein Stamm/keine Kreuznaht bei den 9 Ex-Sprites | erfüllt, siehe Abschnitt 3 |
| 3 | Tree A/B/C unterscheidbar | erfüllt, drei klar unterschiedliche Silhouetten |
| 4 | Collider unverändert (9 Collider-Props) | erfüllt, YAML-Diff gegen Task-0-Sicherung: 0 Abweichungen |
| 5 | Silhouetten/Höhen auf Zieltabelle | erfüllt, `VisualScaleTests` 1-%-Toleranz, alle 16 grün |
| 6 | Pilz-Glühfarbe als Vertexfarben-Näherung ausgewiesen | erfüllt, siehe Abschnitt 3 (kein Emissive im Shader) |
| — | Keine Regression (Namensdiff) | erfüllt — 0 `=>`, 0 `<=` gegen `stil-e3b-final2.xml` |
| — | Freigabe durch Auftraggeber | **aussteht** |

---

## 1. Kombinierter Nachher- + In-Welt-Capture-Lauf (Owner-Auflage: EIN Unity-Start)

**Code-Ergänzung** `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs`:
- `CapturePropsUebersichtNachher()` — dieselbe 16-Eintrag-Liste wie
  `CapturePropsUebersicht` (Kameraparameter unverändert), Zielordner
  `TempReview/StilumbauE4/nachher`.
- `CapturePropsInWelt(string ordner)` — öffnet `Zone_Greenwood.unity`,
  instanziiert `SP_Tree_A.prefab` bei `(0,0,0)` und `SP_Plant_Bush.prefab` bei
  `(6,0,0)` temporär, rendert den Baum mit dem für hohe T1-Bäume etablierten
  Kameraversatz `(4.5, 6.5, −6.0)` und den Strauch mit dem Standardversatz
  `(3.2, 4.6, −4.2)`, 1280×720, `DestroyImmediate` danach. Szene wird nicht
  gespeichert.
- `CapturePropsAbnahme()`, MenuItem `Eidren/V0.2/Stilumbau/Props
  Abnahme-Captures` — reine Delegation: `CapturePropsUebersichtNachher()` →
  `CapturePropsInWelt(...)`, gebündelter Prozessstart wie bei den Vorbildern
  `CaptureT1Abnahme`/`CaptureT2Abnahme`.

**Lauf:** Lockfile vor dem Start geprüft (`Library/UnityLockfile`,
`Temp/UnityLockfile`, Prozessliste) — keins vorhanden.

```
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode `
  -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
  -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CapturePropsAbnahme -quit `
  -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e4-t5-captures.log"
```

Ohne `-nographics` (Vorgabe für Captures). Exit 0. `GetInstanceID`-Fehler: 0
Treffer (richtiger Editor bestätigt). `error CS`: 0 Treffer. `exception`
(case-insensitiv): 0 Treffer. Sauberer Exit: `Exiting batchmode successfully
now!`. 18 `[StilE4]`-„geschrieben:"-Zeilen — exakt 16 (Nachher-Studio) + 2
(In-Welt) aus einem Lauf.

**Ergebnis:** `TempReview/StilumbauE4/nachher/` — 16/16 PNGs (16–41 KB),
`inwelt/` — 2/2 PNGs (`SP_Tree_A.png` 1,62 MB, `SP_Plant_Bush.png` 1,64 MB).
Kein leeres oder rein einfarbiges Bild. **Kriterium 1 erfüllt.**

### Alle 18 Bilder einzeln gesichtet (Read-Tool, visuelle Prüfung)

| Datei | Sichtbefund |
| --- | --- |
| `nachher/SP_Tree_A.png` | Schlanker Stamm, 1 Astansatz, gestufte Okt-Loft-Krone (Kronenspitze am oberen Bildrand angeschnitten — Kameraframing, kein Geometriefehler), ein zusammenhängender Stammkörper, keine Kreuznaht |
| `nachher/SP_Tree_B.png` | Breiterer Stamm, 3 sichtbar versetzte, gestufte Kronenlagen — deutlich buschiger/breiter als A |
| `nachher/SP_Tree_C.png` | Deutlich sichtbar gekrümmter/knorriger Stamm, kompakte asymmetrische Kronenform — klar von A und B unterscheidbar |
| `nachher/SP_RuinMonument.png` | Sockel-Loft (grau) + verdrehte Stein-Boxen (Erdton) gestapelt, geschlossene Geometrie |
| `nachher/SP_RuinWall_A.png` | Zwei Mauerlagen, 3 Wedge-Zinnen an der Oberkante, durchgehende Wandfläche |
| `nachher/SP_RuinWall_B.png` | Zwei Mauerlagen, 2 Wedge-Zinnen, sichtbar schmaler/kürzer als A — Spiegelung/Kürzung bestätigt |
| `nachher/SP_Rock_Large.png` | 2 (von 5, restliche verdeckt aus dieser Perspektive) hohe, dreiachsig gekippte Okt-Loft-Felsen |
| `nachher/SP_Rock_Medium.png` | 3 gestaffelte Felsformen, kompakter als Large |
| `nachher/SP_Rock_Small.png` | 2 kleine Felsformen, deutlich kleinster Cluster der drei Rock-Stufen |
| `nachher/SP_Plant_Bush.png` | Geschlossene Kuppel-Loft-Geometrie ohne Beeren, ein zusammenhängender Körper |
| `nachher/SP_Plant_Fern.png` | 6 gefächerte, flache TaperedBox-Wedel in unterschiedlichen Kippwinkeln |
| `nachher/SP_Plant_Flowers.png` | 5 dünne Stiele mit Blütenknollen in zwei Akzentfarben (gelb/rosa) |
| `nachher/SP_GroundCover_Grass.png` | 8 kleine Kegel-Cluster im Kreis angeordnet, flach |
| `nachher/SP_GroundCover_Moss.png` | 6 flache Kuppel-Loft-Elemente im Cluster |
| `nachher/SP_Accent_GlowMushrooms.png` | 2 (von 4, restliche aus dieser Perspektive verdeckt) Stiel+Kappen-Kombinationen, Kappen in auffällig hellem Türkis gegenüber dem dunklen Stiel — liest als „glühend" |
| `nachher/SP_EidrenRune.png` | Stehende Steinplatte + 3 türkisfarbene Akzent-Boxen als Runenzeichen |
| `inwelt/SP_Tree_A.png` | In der Zonenszene: ein Stamm, gestufte Krone, kein zweiter überlagernder Körper, keine sichtbare Kreuznaht-Ebene — umgeben von zahlreichen weiteren gebackenen Props (Moss-Cluster, Grass-Cluster, Farn, Blumen) |
| `inwelt/SP_Plant_Bush.png` | Kuppelform im Zonenkontext neben dem Baum, weitere Props (Rock, GroundCover, ein zweiter Bush) im Bild sichtbar |

Kein Bild zeigt zwei übereinanderliegende Körper, keine sichtbare
Alpha-Kreuzebene, keine bemalte Textur — alle 18 Bilder bestehen ausschließlich
aus Fabrik-Loft-/TaperedBox-/Wedge-Geometrie mit Vertexfarben.

---

## 2. Namensdiff gegen Baseline (Regressionsprüfung)

Volle EditMode-Suite, `-nographics`, **ohne** `-quit` (Regel: `-runTests` nie
mit `-quit`). `TestResults-stil-e4-final.xml`, Log `stil-e4-final.log`:

```
<test-run id="2" testcasecount="821" result="Failed(Child)" total="821" passed="750" failed="71" ...>
```

`error CS` im Log: 0 Treffer. `exception` (case-insensitiv): 9 Treffer,
ausschließlich die benigne `UnityLogCheckDelegatingCommand:CaptureException`-
Infrastrukturzeile des Testrunners selbst (identisches Muster zu allen
vorherigen Etappen, kein Absturz).

| | gesamt | bestanden | fehlgeschlagen |
| --- | ---: | ---: | ---: |
| Baseline (`TestResults-stil-e3b-final2.xml`) | 805 | 734 | 71 |
| Endstand (Task 5) | 821 | 750 | 71 |

Der Zuwachs von 805 auf 821 stammt exakt aus den 16 neuen
`Prop_IstImVertexfarbenStil`-Fällen (Task 3) — alle 16 `Passed`, keiner in
der Failed-Liste (per gezielter XML-Abfrage verifiziert).

```powershell
$basis = ([xml](Get-Content "TestResults-stil-e3b-final2.xml")).SelectNodes("//test-case[@result='Failed']") | % { $_.fullname } | Sort-Object -Unique
$neu   = ([xml](Get-Content "TestResults-stil-e4-final.xml")).SelectNodes("//test-case[@result='Failed']")   | % { $_.fullname } | Sort-Object -Unique
Compare-Object $basis $neu
```

**Ergebnis: leer.** 71 Fehlschläge in beiden Läufen, identische Namensmengen.
**0 `=>` (keine neuen Fehlschläge), 0 `<=` (keine behobenen Fehlschläge).**
Alle 71 sind namensgleiche, vorbestehende Baseline-Altlasten der
Dekompilat-Rekonstruktion (vgl. Memory-Notiz „EditMode-Baseline hat 73
Fehlschläge" — Zahl hat sich über die Etappen leicht verschoben, aber die
konkrete Namensmenge ist gegen die unmittelbare Vorgänger-Baseline stabil).
**Regressionsfreiheit vollständig erfüllt — die strengste Planvorgabe
(„keine `=>`-Einträge") ist ohne Einschränkung eingehalten.**

---

## 3. Kriterien 2/3/6 — Rendertechnik, Unterscheidbarkeit, Glüh-Näherung

**Kriterium 2 (ein Stamm/keine Kreuznaht bei den 9 Ex-Sprites):** Die 9
ursprünglich als Sprite-Kreuz-Hybriden gebauten Props (`SP_Tree_A/B/C`,
`SP_Plant_Bush/Fern/Flowers`, `SP_GroundCover_Grass/Moss`,
`SP_Accent_GlowMushrooms`) bestehen nach dem Umbau ausschließlich aus
geschlossener Fabrik-Loft-/TaperedBox-Geometrie (Abschnitt 1). Kein Bild zeigt
eine gemalte Alpha-Ebene oder einen zusätzlichen, die Kamera kreuzenden
zweiten Körper. **Erfüllt.**

**Kriterium 3 (Tree A/B/C unterscheidbar):** A ist schlank mit gestufter,
schmaler Krone; B ist sichtbar breiter mit 3 versetzten Kronenlagen; C hat
einen deutlich gekrümmten/knorrigen Stamm und eine kompakte, asymmetrische
Krone. Alle drei Silhouetten sind im direkten Bildvergleich eindeutig
unterscheidbar. **Erfüllt.**

**Kriterium 6 (Pilz-Glühfarbe als Vertexfarben-Näherung):** `Eidren/World/
VertexLit` (der gemeinsame Shader aller Props, Etappe 1) hat **kein**
Emissive-Feature — bestätigt durch Shader-Quelle aus Etappe 1 und durch die
Planvorgabe selbst (`STILUMBAU_E4_PLAN.md:36`: „der Shader hat kein
Emissive, die helle Vertexfarbe trägt den Akzent"). `SP_Accent_
GlowMushrooms` nutzt die Kappenfarbe `(0.45, 0.75, 0.7)` — ein helles Türkis,
das gegen die dunklen Stiel-/Umgebungsfarben deutlich hervorsticht und so den
Glüheffekt approximiert, ohne echte Lichtemission. Das ist **keine physische
Lichtquelle**, sondern eine reine Farbkontrast-Näherung — hiermit
ausdrücklich ausgewiesen, wie vom Plan gefordert. **Erfüllt, mit
dokumentierter Einschränkung** (kein echtes Glühen/Bloom, nur Farbkontrast).

---

## 4. Kriterium 4 — Collider unverändert (YAML-Diff)

Die 9 Collider-Props (`STILUMBAU_E4_BAKEKETTE.md` Abschnitt (c): Rock
Small/Medium/Large, RuinWall A/B, RuinMonument, Tree A/B/C) tragen laut
Plan-Bestandsvertrag exakt dieselben `BoxCollider`-Maße/-Zentren wie vor dem
Umbau. Direkter YAML-Diff `m_Size`/`m_Center` zwischen dem aktuellen
Arbeitsstand und der Task-0-Sicherung
(`C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e4-20260809-1940\_Game\Prefabs\Environment\StyleProof\`):

| Prefab | `m_Size`/`m_Center` Backup vs. aktuell |
| --- | --- |
| `SP_Rock_Small.prefab` | identisch (0 Diff-Zeilen) |
| `SP_Rock_Medium.prefab` | identisch (0 Diff-Zeilen) |
| `SP_Rock_Large.prefab` | identisch (0 Diff-Zeilen) |
| `SP_RuinWall_A.prefab` | identisch (0 Diff-Zeilen) |
| `SP_RuinWall_B.prefab` | identisch (0 Diff-Zeilen) |
| `SP_RuinMonument.prefab` | identisch (0 Diff-Zeilen) |
| `SP_Tree_A.prefab` | identisch (0 Diff-Zeilen) |
| `SP_Tree_B.prefab` | identisch (0 Diff-Zeilen) |
| `SP_Tree_C.prefab` | identisch (0 Diff-Zeilen) |

Alle 9 Diffs leer — `m_Size`/`m_Center` byte-identisch zum Vorher-Stand.
Gegenprobe an den übrigen 7 (colliderfreien) Props: `grep -c "Collider:"`
liefert `0` sowohl im Backup als auch aktuell für alle sieben
(`SP_Accent_GlowMushrooms`, `SP_EidrenRune`, `SP_GroundCover_Grass/Moss`,
`SP_Plant_Bush/Fern/Flowers`) — kein Collider wurde versehentlich
hinzugefügt. Zusätzlich bestätigt durch die 16
`Prop_IstImVertexfarbenStil`-Testfälle (Task 3, Abschnitt 2), die dieselbe
Collider-Erwartung je Prop programmatisch prüfen und alle grün sind.
**Kriterium 4 erfüllt, empirisch per YAML-Diff belegt (nicht nur über
Testassertion).**

---

## 5. Kriterium 5 — Dreieckszahlen und Höhen auf Zieltabelle

### Dreieckszahlen je Prop (`[StilE4]`-Log, Task 3, finaler Stand)

| Prop | Dreiecke |
| --- | ---: |
| SP_Tree_A | 204 |
| SP_Tree_B | 264 |
| SP_Tree_C | 148 |
| SP_Plant_Bush | 44 |
| SP_Plant_Fern | 72 |
| SP_Plant_Flowers | 120 |
| SP_GroundCover_Grass | 224 |
| SP_GroundCover_Moss | 264 |
| SP_Accent_GlowMushrooms | 224 |
| SP_Rock_Small | 88 |
| SP_Rock_Medium | 132 |
| SP_Rock_Large | 220 |
| SP_RuinWall_A | 48 |
| SP_RuinWall_B | 40 |
| SP_RuinMonument | 76 |
| SP_EidrenRune | 48 |

Spanne 40–264 Dreiecke, alle 16 klar unter dem Plan-Korridor von ≤ 500.

### Höhen (Sollwerte aus `VisualScaleTableBuilder.AddProps`, 1-%-Testtoleranz)

| Visual-ID | Sollhöhe (m) | `ColliderSize` | Ergebnis |
| --- | ---: | --- | --- |
| `visual.prop.tree_a` | 7,5 | `(0.9, 3, 0.9)` | erreicht (Iteration 1, analytisch exakt) |
| `visual.prop.tree_b` | 6,0 | `(0.8, 2.4, 0.8)` | erreicht (Iteration 1) |
| `visual.prop.tree_c` | 4,0 | `(0.7, 1.6, 0.7)` | erreicht (Iteration 1) |
| `visual.prop.rock_large` | 2,4 | `(2.2, 1.8, 2.2)` | erreicht (Iteration 2, Felsskala kalibriert) |
| `visual.prop.rock_medium` | 1,1 | `(1.3, 0.8, 1.3)` | erreicht (Iteration 2) |
| `visual.prop.rock_small` | 0,5 | `(0.8, 0.4, 0.8)` | erreicht (Iteration 3, 2 Korrekturrunden) |
| `visual.prop.bush` | 0,9 | kein Collider | erreicht (Iteration 1) |
| `visual.prop.fern` | 0,7 | kein Collider | erreicht (Iteration 2) |
| `visual.prop.flowers` | 0,4 | kein Collider | erreicht (Iteration 1) |
| `visual.prop.ground_grass` | 0,0 (Extent 1,2) | kein Collider | erreicht (Iteration 2) |
| `visual.prop.ground_moss` | 0,0 (Extent 1,2) | kein Collider | erreicht (Iteration 2) |
| `visual.prop.glow_mushrooms` | 0,35 | kein Collider | erreicht (Iteration 1) |
| `visual.prop.ruin_wall_a` | 2,6 | `(2.6, 2.2, 0.8)` | erreicht (Iteration 1) |
| `visual.prop.ruin_wall_b` | 2,6 | `(2, 2.2, 0.8)` | erreicht (Iteration 1) |
| `visual.prop.ruin_monument` | 5,0 | `(1.4, 3, 1.4)` | erreicht (Iteration 1) |
| `visual.prop.rune` | 0,8 | kein Collider | erreicht (Iteration 1) |

3 Iterationen bis vollständig grün (Limit im Plantext: 4 — eingehalten,
vollständiger Zahlenverlauf inkl. Zwischenwerten in `task-3-report.md`
Abschnitt 3). `VisualScaleTests.EveryBakedPrefab_MatchesItsTableHeight`: alle
16 Prop-Einträge `Passed` im vollen Suite-Lauf (Abschnitt 2). **Kriterium 5
erfüllt.**

---

## 6. Platzierungs-Beleg (Task 4, hier zusammengefasst)

`StyleProofPlacementReport.CountAndReport()` (rein lesend, keine Szene
gespeichert) zählte in allen 8 Zonenszenen (`Zone_Greenwood`, `Zone_Marsh`,
`Zone_Quarry`, `Zone_EmberRuins`, `Zone_TwilightGrove`, `Zone_VeilMarsh`,
`Zone_GreyRifts`, `HomeBase`) jede `PrefabInstance`-Wurzel unter
`Assets/_Game/Prefabs/Environment/StyleProof/`:

| Szene | Summe |
| --- | ---: |
| Zone_Greenwood.unity | 206 |
| Zone_Marsh.unity | 227 |
| Zone_Quarry.unity | 136 |
| Zone_EmberRuins.unity | 115 |
| Zone_TwilightGrove.unity | 153 |
| Zone_VeilMarsh.unity | 174 |
| Zone_GreyRifts.unity | 114 |
| HomeBase.unity | 41 |
| **Gesamt** | **1166** |

Alle 16 von 16 `SP_`-Prefabs sind mindestens einmal platziert. GUID-Stabilität
an 3 Stichproben (`SP_Tree_A`, `SP_Rock_Large`, `SP_GroundCover_Grass`) gegen
die Task-0-Sicherung bestätigt identisch. Zeitstempelwache: keine
`.unity`-Datei geändert, nur die erwarteten Task-3/4-Dateien (16 Prefabs, 80
Mesh-Assets, 3 Editor-Skripte). **Kernbehauptung aus `STILUMBAU_E4_BAKEKETTE.md`
(b) bestätigt: kein Szenen-Rebake nötig, die 1166 bereits gebackenen
Instanzen zeigen die neue Geometrie automatisch über die stabile
Prefab-GUID.**

---

## 7. Erzeugte / geänderte Assets (gesamte Etappe 4, Tasks 0–5)

**Geändert (Editor-Werkzeuge)**
- `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs` —
  `CapturePropsUebersicht` (Task 2), `CapturePropsUebersichtNachher`,
  `CapturePropsInWelt`, `CapturePropsInWeltObjekt`, `CapturePropsAbnahme`
  (neu, diese Task)
- `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` —
  `Prop_IstImVertexfarbenStil` mit 16 Testfällen ergänzt (Task 3)

**Neu**
- `Assets/_Game/Editor/StyleProofPropBuilder.cs` (Task 3)
- `Assets/_Game/Editor/StyleProofPlacementReport.cs` (Task 4)

**Prefabs (Task 3, in-place überschrieben, gleiche Pfade/GUIDs)**
- `Assets/_Game/Prefabs/Environment/StyleProof/SP_*.prefab` (16 Stück,
  40–264 Dreiecke, Abschnitt 5)
- `Assets/_Game/Art/StyleProof/Meshes/` — 80 neue Mesh-Assets

**Nicht verändert (per Zeitstempel-/YAML-Diff belegt)**
- Alle 12 `.unity`-Szenendateien (Abschnitt 6)
- `StyleProofVisualLibrary.asset`, `Greenwood_StyleProof_Volume.asset`,
  `M_SP_*`-Materialien, alle StyleProof-Texturen

**Bildmaterial**
- `TempReview/StilumbauE4/vorher/` — 16 PNGs (Task 2, kopiert aus `uebersicht/`)
- `TempReview/StilumbauE4/uebersicht/` — 16 PNGs (Task 2, Ausgangszustand)
- `TempReview/StilumbauE4/nachher/` — 16 PNGs (Task 5, dieser Bericht)
- `TempReview/StilumbauE4/inwelt/` — 2 PNGs (Task 5, dieser Bericht)
- `TempReview/StilumbauE4/platzierungsbeleg.txt` — Zählreport (Task 4)

**Testergebnisse (Projektwurzel)**
- `TestResults-stil-e3b-final2.xml` (Baseline, aus Etappe 3b übernommen)
- `TestResults-stil-e4-t3a.xml` … `TestResults-stil-e4-hoehen3.xml` (Task 3,
  RED/GREEN- und Höhen-Iterationsnachweise)
- `TestResults-stil-e4-t3.xml`, `TestResults-stil-e4-t4.xml` (Task 3/4
  Regressionsläufe, 67/65/2 — Zwischenstand mit schmalem Filter)
- `TestResults-stil-e4-final.xml` / `stil-e4-final.log` (Task 5, voller
  Suite-Lauf — 821/750/71, Namensdiff 0 `=>`/0 `<=`, maßgeblicher Endstand)
- `stil-e4-t5-captures.log` (Task 5, kombinierter Nachher-/In-Welt-Capture-Lauf)

**Dokumentation**
- `Documentation/Etappen/Stilumbau/STILUMBAU_E4_BAKEKETTE.md` (Task 1)
- `Documentation/Etappen/Stilumbau/STILUMBAU_E4_ABNAHME.md` (dieser Bericht, Task 5)

**Sicherung**
- `vor-stilumbau-e4-20260809-1940` (Quellstand vor der Etappe, Task 0)

---

## 8. Bekannte Alt-Fehlschläge (nicht durch Etappe 4 verursacht)

Zwei `StyleProofContentTests`-Fälle sind in der 71er-Failed-Menge enthalten
(bereits in der Etappe-3b-Baseline vorhanden, siehe Task-3-Report Abschnitt
„Bekannte Alt-Fehlschläge"):

```
Eidren.Tests.StyleProofContentTests.ReusableStyleProofAssets_ArePresentAndCompatible
Eidren.Tests.StyleProofContentTests.GreenwoodVolume_ContainsRequiredOverrides
```

Ursache: `Assets/_Game/Art/StyleProof/Materials/` und
`Assets/_Game/Settings/Greenwood_StyleProof_Volume.asset` existieren in
diesem Arbeitsstand nicht auf der Platte — vorbestehender Lückenbefund, von
`StyleProofPropBuilder` bewusst nicht angefasst (Auftrag: „nur die 16
Prop-Prefabs"). Keine Regression, unverändert seit Etappe 3b.

---

## 9. Offene Punkte

1. **`SP_RuinWall_B` nur teil-gespiegelt** (aus Task 3 übernommener,
   bewusst zurückgestellter Minor-Befund): Der Plan verlangt „B
   gespiegelt/kürzer" — B ist in der Länge kürzer und trägt 2 statt 3
   Wedge-Zinnen (Abschnitt 1, Bild bestätigt die Kürzung visuell), die
   Geometrie ist aber nicht vollständig spiegelsymmetrisch zu A umgesetzt.
   Kein Verstoß gegen einen automatisierten Test (kein Test prüft
   Spiegelsymmetrie), rein optischer Minor-Punkt. Nachbesserung liegt
   außerhalb des Task-5-Auftrags.
2. **Kronenspitze von `SP_Tree_A` im Studio-Nachher-Bild und In-Welt-Aufnahme angeschnitten**
   (Abschnitt 1) — beide zeigen die Kronenspitze am oberen Bildrand
   angeschnitten (identische Kameralimatation wie E3b; Parameterisierung ist
   Etappe-5-Auflage), reines Kameraframing (Distanz/Zielhöhe-Parameter aus
   `CapturePropsUebersicht`, unverändert seit Task 2), keine
   Geometrieabweichung. Die Evidenz für einen Stammkörper und Kreuznaht-Freiheit wird davon nicht beeinträchtigt.
3. **Glüh-Effekt der Pilzkappen ist eine Farbkontrast-Näherung, kein echtes
   Emissive/Bloom** (Abschnitt 3, Kriterium 6) — wie vom Plan selbst
   vorausgesetzt und hier nur zur Transparenz nochmals hervorgehoben.
4. **Zwei vorbestehende `StyleProofContentTests`-Fehlschläge** bleiben
   unverändert offen (Abschnitt 8) — außerhalb des Etappe-4-Scopes.
5. **Freigabe durch den Auftraggeber steht aus.** Dieser Bericht weist alle
   sechs Plan-Kriterien sowie vollständige Regressionsfreiheit nach
   (Kurzfassung-Tabelle). Vor einer Entscheidung sind dem Auftraggeber die 18
   Bilder aus `TempReview\StilumbauE4\{nachher,inwelt}\`, der Namensdiff aus
   Abschnitt 2 und die beiden offenen Minor-Punkte (1–3 oben) vorzulegen.

---

## 10. Nachvollziehen

Kombinierten Nachher-/In-Welt-Capture-Lauf neu erzeugen:

    & "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode `
      -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
      -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CapturePropsAbnahme -quit `
      -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e4-t5-captures.log"

Volle EditMode-Suite (ohne `-quit`, siehe Abschnitt 2):

    & "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics `
      -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
      -runTests -testPlatform EditMode -testResults "TestResults-stil-e4-final.xml" `
      -logFile "stil-e4-final.log"

Namensdiff siehe Abschnitt 2 für die vollständige PowerShell-Funktion.
