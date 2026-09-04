# Stilumbau Etappe 3b Abnahme — Tier-1-Ressourcenknoten

> **Nachtrag (Fix-Runde, 09.08.2026, nach 16:00):** Der in diesem Bericht ursprünglich
> dokumentierte `=>`-Regressionsfund (3 neue Fehlschläge in `VisualAssetTests`, Abschnitt 2) ist
> behoben. Controller-Entscheid: `STILUMBAU_WELTINVENTAR_ENTWURF.md` Abschnitt „Abnahme und
> Tests" verlangt ausdrücklich „Bestehende Sichttests … werden auf die Fabrik-Ausgaben
> nachgezogen, nicht gelöscht" — die drei Fälle prüften Eigenschaften der ersetzten
> Hero-Geometrie und mussten daher angepasst, nicht als Zielkonflikt stehen gelassen werden. Der
> ursprüngliche Befund bleibt unten unverändert stehen (Transparenz); Abschnitt 2 fasst Fix,
> erneute Verifikation und den aktualisierten Namensdiff zusammen (jetzt **0** `=>`, **0** `<=`).

Stand: 09.08.2026
Bezug: `STILUMBAU_E3B_PLAN.md` (Etappe 3b: die fünf Tier-1-Ressourcentypen Tree, StoneDeposit,
FiberPlant, CopperVein, BerryBush — 10 Basis-Visuals + 12 Zonenvarianten; Auftraggeber-Entscheid
vom 08.08.2026: voller Umbau aller fünf Typen, Präzedenzfall „T1 tabu" damit aufgehoben), Tasks 0–5
Vorher-Stand: alte Sprite-Kreuz-Hybriden (Tree/BerryBush/FiberPlant) bzw. alte
`T1ResourceVisualBuilder`-Vorgänger-Primitive (StoneDeposit/CopperVein), gesichert unter
`vor-stilumbau-e3b-<stamp>` (Task 0). Baseline für den Namensdiff (Zeitspar-Regel, kein neuer
Baseline-Lauf): `TestResults-stil-e3-final2.xml` (795 Tests, 724 bestanden, 71 Fehlschläge).

---

## Kurzfassung

Die zehn T1-Basis-Visuals sind auf `EidrenMeshFactory`/Vertexfarben umgestellt (44–368 Dreiecke,
Task 3), alle 12 Zonenvarianten neu geklont (inkl. korrigierter Gebietsäste, Task 4), die fünf
Node-Prefabs neu gebacken. Der Namensdiff gegen die Baseline zeigte in der ersten Runde **drei**
neue, ungeplante Fehlschläge in `VisualAssetTests` (vorbestehende Struktur-Tests, die Eigenschaften
der **alten**, handgebauten Hero-Geometrie hart codierten) — nach der Fix-Runde (Nachtrag oben,
Abschnitt 2) sind alle drei auf den Fabrik-Vertrag nachgezogen und **bytegleich zur Baseline**: 0
`=>`, 0 `<=`. Der im Plan explizit ausgeschlossene Fall (`VisualScaleTests` taucht erneut auf) trat
zu keinem Zeitpunkt ein — alle acht `VisualScaleTests`-Fälle sind grün, die zehn Sollhöhen sind
getroffen. Die Freigabe der Etappe selbst ist **nicht** Teil dieses Berichts (Auftraggeber-Entscheid
steht aus); ebenso bleibt die formale G-005/G-009/G-010-Abnahme laut Planvorgabe eigenständig und
ist durch diesen Bericht nicht abgedeckt.

| # | Kriterium | Ergebnis |
| --- | --- | --- |
| 1 | Kein Nachher-/In-Welt-Bild leer | erfüllt, alle 24 PNGs nicht-leer, alle 24 einzeln gesichtet |
| 2 | Baum: genau EIN Stamm, keine Kreuznaht, kein eingemaltes Tuch (G-005/G-009/G-010-Bezug) | erfüllt für die Rendertechnik (Abschnitt 4); G-009-Fremdobjekt-Befund als Marker-Geometrie bewusst weitergeführt, siehe Einschränkung dort |
| 3 | Active/Exhausted unterscheidbar (alle fünf Typen) | erfüllt |
| 4 | Zonenvarianten behalten Identitätsaufsätze inkl. korrigierter Gebietsäste | erfüllt, Sichtprüfung + Zeitstempelbeleg aus Task 4 |
| 5 | Farbwelt = Planvorgabe | erfüllt, alle 10 Paletten-Konstanten exakt wie im Plankopf |
| 6 | Höhen auf Tabellenziel (`VisualScaleTests`) | erfüllt, 8/8 grün, `VisualScaleTests` nicht erneut in der Failed-Liste |
| — | Keine Regression (Namensdiff) | **erfüllt (nach Fix-Runde)** — final2: 0 `=>`, 0 `<=` gegen die Baseline; ursprünglicher Befund (3 `=>`) in Abschnitt 2 dokumentiert |

---

## 1. Kombinierter Nachher- + In-Welt-Capture-Lauf (Owner-Auflage: EIN Unity-Start)

**Code-Ergänzung** `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs`:
- `CaptureT1UebersichtNachher()` — dieselbe 10-Eintrag-Liste wie `CaptureT1Uebersicht`
  (Kameraparameter unverändert), Zielordner `TempReview/StilumbauE3b/nachher`.
- `CaptureT1VariantenNachher()` — dieselbe 12-Eintrag-Liste wie `CaptureT1VariantenVorher`,
  Zielordner `TempReview/StilumbauE3b/varianten-nachher`.
- `CaptureT1InWelt(string ordner)` — öffnet `Zone_Greenwood.unity`, instanziiert `Tree.prefab` bei
  `(0,0,0)` und `BerryBush.prefab` bei `(6,0,0)` temporär (Task-1-Bakekette Abschnitt (c) bestätigt:
  T1-Ressourcenknoten sind in keiner Szene gebacken, spawnen nur zur Laufzeit — die Editor-Szene ist
  daher leer und die temporäre Instanziierung ist der erwartete Pfad, kein Fallback-Notfall), rendert
  den Baum mit dem für seine Höhe (Sollhöhe 6,0) vergrößerten Kameraversatz `(4.5, 6.5, −6.0)` und den
  Strauch mit dem Standardversatz `(3.2, 4.6, −4.2)`, 1280×720, `DestroyImmediate` danach. Szene wird
  nicht gespeichert.
- `CaptureT1Abnahme()`, MenuItem `Eidren/V0.2/Stilumbau/T1 Abnahme-Captures` — reine Delegation:
  `CaptureT1UebersichtNachher()` → `CaptureT1VariantenNachher()` → `CaptureT1InWelt(...)`, gebündelter
  Prozessstart wie bei `CaptureT2Abnahme` (3a-Vorbild).

**Lauf:** Lockfile vor dem Start geprüft (`Library/UnityLockfile`, `Temp/UnityLockfile`, `tasklist`)
— keins vorhanden.

```
& ".unity-editor\Editor\Unity.exe" -batchmode -projectPath "<Projektpfad>" `
  -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureT1Abnahme -quit `
  -logFile "stil-e3b-t5-captures.log"
```

Ohne `-nographics` (Vorgabe für Captures). Exit 0. `error CS`: 0 Treffer. `exception`
(case-insensitiv): 0 Treffer. Sauberer Exit: `Batchmode quit successfully invoked` /
`Exiting batchmode successfully now!` / Rückgabecode 0. 24 `[StilE3b]`-„geschrieben:"-Zeilen —
exakt 10 (Basis-Nachher) + 12 (Varianten-Nachher) + 2 (In-Welt) aus einem Lauf.

**Ergebnis:** `TempReview/StilumbauE3b/nachher/` — 10/10 PNGs (15–33 KB), `varianten-nachher/` —
12/12 PNGs (14–33 KB), `inwelt/` — 2/2 PNGs (`Tree.png` 1,62 MB, `BerryBush.png` 1,68 MB). Kein
leeres oder rein einfarbiges Bild. **Kriterium 1 erfüllt.**

### Alle 24 Bilder einzeln gesichtet (Read-Tool, visuelle Prüfung)

| Datei | Sichtbefund |
| --- | --- |
| `nachher/Tree_Active.png` | Ein Stamm, zwei Astansätze, drei gestufte Okt-Loft-Kronen (dunkel-/hellgrün), keine Kreuznaht, kein bemaltes Element |
| `nachher/Tree_Exhausted.png` | Stumpf + zwei Splitter, klar von Active unterscheidbar |
| `nachher/BerryBush_Active.png` | Kuppel mit 9 sichtbaren roten Beeren-Knollen |
| `nachher/BerryBush_Exhausted.png` | Kahle Kuppel ohne Beeren |
| `nachher/FiberPlant_Active.png` | 9 gefächerte Halme mit hellen Rispen-Köpfen |
| `nachher/FiberPlant_Exhausted.png` | 5 kurze kahle Stoppeln |
| `nachher/StoneDeposit_Active.png` | 7-teiliger, dreiachsig gekippter Felscluster, Stein/SteinHell |
| `nachher/StoneDeposit_Exhausted.png` | Kleinerer 4-teiliger Cluster |
| `nachher/CopperVein_Active.png` | Wirtsfels-Cluster + sichtbare orange Erzsplitter-Kristalle |
| `nachher/CopperVein_Exhausted.png` | Gleicher Wirtsfels-Cluster, keine Erzsplitter-Kristalle mehr (2 Rock-Instanzen behalten die Kupfer-Akzentfarbe der Wirtsgeometrie selbst — kein Widerspruch, siehe Abschnitt 6) |
| `varianten-nachher/Greenwood_tree_Active.png` | Wie Basis, zusätzlich `RecognitionMarker_AxeOchre` (Axt+ockerfarbenes Tuch) am Stamm sichtbar, keine Gebietsäste (Greenwood hat laut Bakekette keinen `case`) |
| `varianten-nachher/Greenwood_tree_Exhausted.png` | Stumpf + verkleinerter Marker (Faktor 0,36), keine Gebietsäste (erwartungsgemäß leer im Exhausted-Zustand) |
| `varianten-nachher/Marsh_tree_Active.png` | Wie Greenwood, zusätzlich 2 `SilhouetteBranch`-Fabrikäste in korrekter (nach dem Task-4-Fix halbierter) Dicke, ein Stamm |
| `varianten-nachher/Marsh_tree_Exhausted.png` | Stumpf + Marker, keine Äste (Exhausted-Zustand) |
| `varianten-nachher/Marsh_fiber_plant_Active.png` | Wie Basis-FiberPlant Active |
| `varianten-nachher/Marsh_fiber_plant_Exhausted.png` | Wie Basis-FiberPlant Exhausted |
| `varianten-nachher/Quarry_tree_Active.png` | 1 Gebietsast (Quarry hat laut Bakekette nur einen `case`), Marker, ein Stamm |
| `varianten-nachher/Quarry_tree_Exhausted.png` | Stumpf + Marker, kein Ast |
| `varianten-nachher/Quarry_stone_deposit_Active.png` | Wie Basis-StoneDeposit Active, Identitätsklotz aus dieser Perspektive verdeckt (bekanntes Occlusion-Muster aus 3/3a) |
| `varianten-nachher/Quarry_stone_deposit_Exhausted.png` | Wie Basis-StoneDeposit Exhausted, Identitätsklotz-Ecke am mittleren Fels sichtbar |
| `varianten-nachher/EmberRuins_tree_Active.png` | 2 Gebietsäste, Marker, ein Stamm |
| `varianten-nachher/EmberRuins_tree_Exhausted.png` | Stumpf + Marker, keine Äste |
| `inwelt/Tree.png` | In der Zonenszene: ein Stamm, zwei Astansätze, dreistufige Krone (Kronenspitze am oberen Bildrand angeschnitten), keine Kreuznaht, kein bemaltes Tuch — das Geldbild für Kriterium 2 |
| `inwelt/BerryBush.png` | Kuppel mit sichtbaren roten Beeren im Zonenkontext, neben dem Baumfuß |

Kein Bild zeigt zwei übereinanderliegende Stammkörper, keine sichtbare Kreuznaht-Ebene, keine
bemalte Alpha-Textur — alle zehn Basis- und zwölf Varianten-Bilder bestehen ausschließlich aus
Fabrik-Loft-/TaperedBox-Geometrie mit Vertexfarben.

---

## 2. Namensdiff gegen Baseline (Regressionsprüfung) — zentraler Befund dieser Task

Volle EditMode-Suite, `-nographics`, **ohne** `-quit` (etablierte Erkenntnis aus E3/E3a: `-runTests`
+ `-quit` beendet den Prozess vor dem eigentlichen Testlauf). `TestResults-stil-e3b-final.xml`,
Log `stil-e3b-final.log`:

```
<test-run id="2" testcasecount="805" result="Failed(Child)" total="805" passed="731" failed="74" ... start-time="2026-08-09 16:03:51Z" end-time="2026-08-09 16:04:20Z" duration="28,467">
```

`error CS` im Log: 0 Treffer. `exception` (case-insensitiv): 9 Treffer, ausschließlich die
NUnit-eigene `UnityLogCheckDelegatingCommand:CaptureException`-Infrastrukturzeile des
Testrunners selbst (kein Absturz, keine Editor-Exception).

| | gesamt | bestanden | fehlgeschlagen |
| --- | ---: | ---: | ---: |
| Baseline (`stil-e3-final2.xml`) | 795 | 724 | 71 |
| Endstand (Task 5) | 805 | 731 | 74 |

Der Zuwachs von 795 auf 805 stammt aus den 10 neuen `T1Visual_IstImVertexfarbenStil`-Fällen
(Task 3) — alle 10 grün, keiner in der Failed-Liste.

```powershell
$basis = ([xml](Get-Content "TestResults-stil-e3-final2.xml")).SelectNodes("//test-case[@result='Failed']") | % { $_.fullname } | Sort-Object -Unique
$neu   = ([xml](Get-Content "TestResults-stil-e3b-final.xml")).SelectNodes("//test-case[@result='Failed']")   | % { $_.fullname } | Sort-Object -Unique
Compare-Object $basis $neu
```

```
InputObject                                                                               SideIndicator
-----------                                                                               -------------
Eidren.Tests.EditMode.VisualAssetTests.BerryBushUsesTwoDistinctCompleteVisualStates       =>
Eidren.Tests.EditMode.VisualAssetTests.CopperVeinUsesDetailedThreeDimensionalHeroGeometry =>
Eidren.Tests.EditMode.VisualAssetTests.RemainingSupportAssetsUseLayeredHeroGeometry       =>
```

**`<=` (grün geworden):** keine Einträge.

**`=>` (neue Fehlschläge, NICHT erwartet, Planvorgabe „keine `=>`-Einträge" verletzt):** drei
Fälle in `Assets/_Game/Editor/Tests/VisualAssetTests.cs` — ein vorbestehendes Testmodul, das
außerhalb des Task-3/4-Scopes liegt und in Etappe 3b nicht angefasst wurde. Root Cause je Fall:

- **`BerryBushUsesTwoDistinctCompleteVisualStates`** (Zeile 88–101): prüft
  `Assert.That(componentInChildren.sharedMaterial, Is.Not.SameAs(exhausted.sharedMaterial))` —
  verlangt **unterschiedliche** Materialien für Active/Exhausted. Fehlermeldung:
  `Expected: not same as <M_EidrenWorld_VertexLit>. But was: <M_EidrenWorld_VertexLit>`. Der neue
  `T1ResourceVisualBuilder` weist — wie sein T2-Vorbild und wie in der Bakekette (Abschnitt a)
  hergeleitet — allen Bauteilen bewusst dasselbe geteilte Weltmaterial zu; die Farbdifferenzierung
  läuft über Vertexfarben, nicht über Materialwechsel. Das ist die vom Plan vorgegebene Architektur,
  keine Nachlässigkeit — der Test prüft eine Eigenschaft der alten, materialbasierten Pipeline.
- **`CopperVeinUsesDetailedThreeDimensionalHeroGeometry`** (Zeile 144–171): sucht
  `gameObject.transform.Find("Geometry_A14")` und darunter benannte Teile (`OreCrown`,
  `CopperMiningVisualFeedback`-Komponente, ≥20 `MeshFilter`, ≥2500 Vertices, „Universal Render
  Pipeline/Lit"-Shader). Fehlermeldung: `Assets/…/CopperVein_Active.prefab — Expected: not null.
  But was: null` (kein `Geometry_A14`-Kind mehr). Der Fabrik-Builder legt Bauteile flach unter dem
  Wurzelobjekt ab (`T1_{NNN}_{Teilname}`-Namensmuster, `M_EidrenWorld_VertexLit`-Shader) — die alte
  Hero-Geometrie-Hierarchie (Task-0-Sicherung) existiert nach dem Umbau nicht mehr, per Auftrag.
- **`RemainingSupportAssetsUseLayeredHeroGeometry`** (Zeile 257ff., Fall
  `StoneDeposit_Active.prefab`): identischer Mechanismus — sucht `Geometry_A14` und Kindobjekte wie
  `MainFacetedBoulder`/`FreshBrokenFace`, die im Fabrik-Cluster (`Rock_00`…`Rock_06`) nicht mehr
  existieren.

**Bewertung:** Alle drei Fälle sind Struktur-Assertions auf die **alte**, handgebaute
Hero-Geometrie (Task-0-Sicherung), die der Auftraggeber-Entscheid vom 08.08. („voller Umbau aller
fünf Typen") strukturell ersetzt. Kein Fall betrifft `resource.tree`, `resource.fiber_plant`
direkt — aber `BerryBush` und `CopperVein`/`StoneDeposit` sind exakt die T1-Typen dieser Etappe.
Analog zum `VisualScaleTests`-Fund in E3 (dort Fels-Höhenformel) deckt der erste volle Suite-Lauf
seit dem Geometrie-Neubau eine Testannahme auf, die Task 3/4 nicht im Blick hatten, weil deren
schmaler Regressionsfilter (`EidrenWorldStyleTests`/`VisualScaleTests`) `VisualAssetTests` nicht
einschloss. Anders als in E3 (dort ein reiner Kalibrierungsfehler, in einer Nachtragsrunde
behoben) handelt es sich hier um einen **Zielkonflikt zwischen zwei Testmodulen**: `VisualAssetTests`
kodiert Erwartungen der alten Pipeline (Material-pro-Zustand, benannte Hero-Geometrie-Hierarchie),
`EidrenWorldStyleTests`/`VisualScaleTests` kodieren die neue Fabrik-Pipeline (Vertexfarben-Material,
`Teil`-Namensmuster). Eine Korrektur würde `VisualAssetTests.cs` an die neue Architektur anpassen
oder bewusst als für T1-Typen überholt markieren/entfernen — beides liegt außerhalb des
Task-5-Auftrags (Captures, Diff, Dokumentation) und wird hier als zentraler offener Punkt an den
Auftraggeber weitergereicht (Abschnitt 10), **nicht** eigenmächtig behoben.

Die übrigen 71 Fehlschläge sind namensgleich zur Baseline (Altlasten der Dekompilat-
Rekonstruktion, vgl. Memory-Notiz „EditMode-Baseline hat 73 Fehlschläge").

**Planvorgabe „`VisualScaleTests`-Eintrag darf NICHT neu auftauchen": eingehalten.** Kein
`VisualScaleTests`-Fall in der Failed-Liste — alle acht Fälle `Passed` (Abschnitt 6).

### 2.1 Fix-Runde (09.08.2026, nach 16:00) — Nachziehen statt Löschen

**Ruling:** `STILUMBAU_WELTINVENTAR_ENTWURF.md`, Abschnitt „Abnahme und Tests": „Bestehende
Sichttests (u. a. `V02ContainerVisualTests`) werden auf die Fabrik-Ausgaben nachgezogen, nicht
gelöscht." Die drei `=>`-Fälle sind exakt dieser Fall — vorbestehende Sichttests, die eine jetzt
ersetzte Geometrie prüften. Sie wurden in `Assets/_Game/Editor/Tests/VisualAssetTests.cs`
angepasst, nicht entfernt oder abgeschwächt (jeder Fall schlägt weiterhin fehl, wenn die Visuals
gelöscht oder degeneriert wären).

**Intent-Zuordnung alt → neu:**

| Test | Ursprüngliche Absicht | Root Cause des Fehlschlags | Neue Prüfung (Fabrik-Vertrag) |
| --- | --- | --- | --- |
| `BerryBushUsesTwoDistinctCompleteVisualStates` | Active/Exhausted sind zwei vollständige, optisch unterscheidbare Sichtzustände | Test verlangte unterschiedliche Materialien pro Zustand; die Fabrik teilt bewusst `M_EidrenWorld_VertexLit` (Farbdifferenzierung über Vertexfarben) | Beide Zustände: `ActiveVisualPrefab`/`ExhaustedVisualPrefab` vorhanden, nicht identisch, mit Renderern; Teileanzahl (`MeshFilter`-Count) unterscheidet sich strukturell (9 Beeren vs. kahle Kuppel); alle Meshes mit Vertexfarben (`colors.Length == vertexCount`); beide Renderer auf `M_EidrenWorld_VertexLit`; keine Collider |
| `CopperVeinUsesDetailedThreeDimensionalHeroGeometry` | Kupfererz bleibt detaillierte echte 3D-Geometrie mit eingebetteten Kupferakzenten, kein Sprite-Platzhalter | Test suchte `Geometry_A14`-Kind, `OreCrown`-Transform, `CopperMiningVisualFeedback`-Komponente, URP/Lit-Shader — diese Hero-Geometrie-Hierarchie existiert nach dem Fabrik-Umbau nicht mehr | Kein `SpriteRenderer`; ≥1 `Rock_`-Wirtsfels-Teil, ≥1 `OreShard_`-Erzsplitter-Teil (das „eingebettete Kupfer"-Äquivalent); ≥10 Mesh-Teile gesamt (Degenerations-Schwelle); Vertexfarben auf allen Teilen; alle Renderer auf `M_EidrenWorld_VertexLit`; keine Collider |
| `RemainingSupportAssetsUseLayeredHeroGeometry` | Alle sieben gelisteten Support-Assets (5 Gebäude + StoneDeposit Active/Exhausted) tragen benannte, mehrschichtige Hero-Geometrie | Die 5 Gebäude-Einträge (StorageChest, FarmPlot, Wall, Floor, Door) sind vom T1-Umbau unberührt und weiterhin korrekt — nur die 2 `StoneDeposit_*`-Einträge im selben Array brachen die `Geometry_A14`-Suche ab, bevor sie geprüft werden konnten | Die 5 Gebäude-Einträge unverändert in der ursprünglichen Schleife belassen (weiterhin `Geometry_A14`-Prüfung, weiterhin korrekt); `StoneDeposit_Active`/`_Exhausted` in einen neuen Helfer `AssertStoneDepositUsesFactoryGeometry` ausgelagert: `Rock_`-Teileanzahl exakt 7 bzw. 4 (aus dem Builder-Aufruf `BuildRock(..., 7, ...)`/`BuildRock(..., 4, ...)` bestätigt), Vertexfarben, gemeinsames Weltmaterial, keine Collider |

Keine Umbenennung nötig — alle drei Testnamen beschreiben weiterhin wahre Eigenschaften der neuen
Geometrie („zwei distinkte Sichtzustände", „detaillierte 3D-Geometrie", „Support-Assets mit
mehrschichtiger/strukturierter Geometrie"); der Namensdiff unten enthält daher keine
Umbenennungs-Paare.

**Isolierter Verifikationslauf** (`Eidren.Tests.EditMode.VisualAssetTests`, `-runTests`, ohne
`-quit`), Lockfile vorab geprüft (nicht vorhanden) — `TestResults-stil-e3b-t5fix.xml`,
`stil-e3b-t5fix.log`:

```
<test-run id="2" testcasecount="11" result="Failed(Child)" total="11" passed="8" failed="3" start-time="2026-08-09 16:16:31Z" end-time="2026-08-09 16:16:31Z" duration="0,285555">
```

0 `error CS`. Die drei verbliebenen Fehlschläge (`PhaseEUiIsAuthoredAndUsesUniqueArt`,
`V01ItemsHaveOneUniqueConsistentlyImportedIcon`, `WorldMapVisualsAreTwentyUniqueImportedSprites`)
sind gegen `TestResults-stil-e3-final2.xml` verifiziert vorbestehende Baseline-Fehlschläge,
unberührt von diesem Fix. Alle drei angepassten Fälle (`BerryBushUsesTwoDistinctCompleteVisualStates`,
`CopperVeinUsesDetailedThreeDimensionalHeroGeometry`, `RemainingSupportAssetsUseLayeredHeroGeometry`)
`Passed`.

**Voller Suite-Re-Run** (ohne `-quit`, Prozessstart + Poll auf die XML, kein Hintergrund-Wächter),
Lockfile vorab geprüft (nicht vorhanden) — `TestResults-stil-e3b-final2.xml`, `stil-e3b-final2.log`:

```
<test-run id="2" testcasecount="805" result="Failed(Child)" total="805" passed="734" failed="71" start-time="2026-08-09 16:18:06Z" end-time="2026-08-09 16:18:24Z" duration="17,9502358">
```

0 `error CS`. 9 `exception`-Treffer (case-insensitiv), ausschließlich dieselbe benigne
`CaptureException`-Infrastrukturzeile wie im vorherigen Lauf.

**Namensdiff (final2) gegen `TestResults-stil-e3-final2.xml`:**

```powershell
$basis = ([xml](Get-Content "TestResults-stil-e3-final2.xml")).SelectNodes("//test-case[@result='Failed']") | % { $_.fullname } | Sort-Object -Unique
$neu   = ([xml](Get-Content "TestResults-stil-e3b-final2.xml")).SelectNodes("//test-case[@result='Failed']")   | % { $_.fullname } | Sort-Object -Unique
Compare-Object $basis $neu
```

Ergebnis: **leer** — 71 Fehlschläge in beiden Läufen, identische Namensmengen. **0 `=>`, 0 `<=`.**
`VisualScaleTests` weiterhin nicht in der Failed-Liste (alle 8 Fälle grün, Test bereits vor dem Fix
bestätigt und durch den Re-Run erneut mitgeführt). Regressionsfreiheit **erfüllt.**

---

## 3. Dreieckszahlen (10 T1-Basis-Visuals)

Aus den `[StilE3b]`-Log-Zeilen des Builder-Laufs (Task 3, `stil-e3b-t3-build3.log`, finaler Stand
nach Höhen-Iteration 3 — Dreieckszahlen über alle drei Build-Iterationen unverändert):

| Prefab | Dreiecke | Korridor (Plan) |
| --- | ---: | --- |
| Tree_Active | 264 | ≤ 800 ✓ |
| Tree_Exhausted | 84 | ≤ 500 ✓ |
| BerryBush_Active | 152 | ≤ 500 ✓ |
| BerryBush_Exhausted | 44 | ≤ 500 ✓ |
| FiberPlant_Active | 216 | ≤ 500 ✓ |
| FiberPlant_Exhausted | 60 | ≤ 500 ✓ |
| StoneDeposit_Active | 308 | ≤ 500 ✓ |
| StoneDeposit_Exhausted | 176 | ≤ 500 ✓ |
| CopperVein_Active | 368 | ≤ 500 ✓ |
| CopperVein_Exhausted | 308 | ≤ 500 ✓ |

Alle zehn liegen im spezifizierten Korridor (44–368 Dreiecke). Bestätigt durch
`T1Visual_IstImVertexfarbenStil` (10/10 grün, Abschnitt 6).

---

## 4. Kriterium — Baum: genau EIN Stamm, keine Kreuznaht, kein eingemaltes Tuch (G-005/G-009/G-010-Bezug)

**G-005 (dunkler Stammkörper / Doppelstamm):** Alle 4 Baum-Bildpaare (Basis + 3 Zonenvarianten mit
Ästen, je Active/Exhausted, plus die In-Welt-Aufnahme) zeigen **genau einen** Stammkörper — die
alte Konstellation aus gemaltem Sprite-Stamm + zusätzlichem undurchsichtigem 3D-Platzhalterkörper
(G-005-Beobachtung) existiert nach dem Vollumbau nicht mehr, weil der gesamte Baum jetzt aus einer
einzigen zusammenhängenden Fabrik-Geometrie besteht (kein Sprite mehr, das von einem zweiten
Element durchstoßen werden könnte). **Erfüllt für die Rendertechnik.**

**Kreuznaht (`CrossedAlphaGeometry`):** Die alte gekreuzte Alpha-Ebenen-Krone
(`visual_tree_active_crown_alpha.mat`, Bakekette Abschnitt a) existiert in keinem der 12
Nachher-Bilder mehr — alle Kronen sind geschlossene Okt-Loft-Geometrie ohne Texturkanten oder
sichtbare Ebenenüberschneidung. **Erfüllt.**

**Eingemaltes Tuch (G-009-Bezug):** Das alte gemalte `OchreCloth`-Element auf einer Textur-Ebene
existiert nicht mehr — an seiner Stelle steht jetzt, unverändert aus Task 4, das eigenständige
`RecognitionMarker_AxeOchre`-Objekt (Axt + ockerfarbenes Tuch, beide als Vertexfarben-Boxgeometrie)
in allen vier Baum-Zonenvarianten (Active, verkleinert bei Exhausted). **Wichtige Einschränkung
(bereits in Bakekette Abschnitt a vorhergesagt, hier bestätigt):** Der Marker **ersetzt** die
Rendertechnik (Textur → Geometrie), löst aber die G-009-Kernbeobachtung „Fremdobjekt in der
Baumkrone" nicht auf — Axt und Tuch sitzen weiterhin sichtbar am Stamm/in Kronen-Nähe. Die formale
G-009-Abnahme bleibt daher, wie im Plan vorgegeben, **eigenständig und offen**; dieser Bericht
bewertet nur, dass das Element nicht mehr als bemalte Textur, sondern als reguläres 3D-Objekt
vorliegt.

**G-010 (Perspektivmix):** Da Krone, Stamm und Astgeometrie jetzt durchgängig echte 3D-Geometrie
aus einer Kamera sind (kein gemalter Draufsicht-Kronen-Sprite mehr neben einem seitlich gemalten
Stamm), tritt der spezifische G-010-Befund „drei Perspektiven in einem Bild" an den fünf T1-Typen
strukturell nicht mehr auf. Die formale G-010-Abnahme (projektweite dokumentierte
Blickhöhen-Vorgabe) ist davon unberührt und bleibt, wie im Plan vorgegeben, eigenständig.

---

## 5. Kriterium — Active/Exhausted unterscheidbar (alle fünf Typen)

Aus Abschnitt 1 bestätigt für alle fünf Typen: Tree (volle Krone vs. Stumpf+Splitter), BerryBush
(9 Beeren vs. kahle Kuppel), FiberPlant (9 Halme mit Köpfen vs. 5 kahle Stoppeln), StoneDeposit
(7-teiliger vs. 4-teiliger Cluster), CopperVein (mit vs. ohne sichtbare Erzsplitter-Kristalle, bei
gleichbleibendem Wirtsfels-Cluster wie im Plan gefordert „Wirtsfels bleibt, nur das Erz
verschwindet"). **Erfüllt.**

---

## 6. Kriterium — Zonenvarianten behalten Identitätsaufsätze inkl. korrigierter Gebietsäste

**Identitätsaufsätze:** `RecognitionMarker_AxeOchre` sichtbar in allen vier Baum-Varianten
(Abschnitt 1, Active groß + Exhausted verkleinert). `AddLeadResourceIdentity`-Klötze bei den
Nicht-Baum-Varianten aus der Studio-Kameraperspektive teils verdeckt (`Quarry_stone_deposit_*`) —
dasselbe, bereits aus 3/3a bekannte Occlusion-Muster, keine neue Einschränkung.

**Gebietsäste (Task-4-Fix):** Marsh (2 Äste) und EmberRuins (2 Äste) und Quarry (1 Ast) zeigen in
den Active-Bildern durchgehend **dünne**, proportionale `SilhouetteBranch`-Fabrikäste — visuell
konsistent mit dem in `task-4-report.md` dokumentierten Dicken-Fix (`TaperedBox`-Größe von
`scale*2` auf `(scale.x, scale.y*2, scale.z)` korrigiert, AABB-Beleg dort). Greenwood bleibt
astfrei (kein `case "Greenwood"`, wie in der Bakekette hergeleitet). Bei Exhausted verschwinden die
Äste erwartungsgemäß vollständig (`AreaIdentityGeometry` nur bei `!exhausted` befüllt). **Erfüllt.**

**CopperVein-Farbnuance (Nebenbefund, kein Fehler):** In `CopperVein_Exhausted.png` tragen 2 von 7
Wirtsfels-Instanzen (Index 0/3/6, `BuildRock`-Zeile 253: `index % 3 == 0 ? accent : host`) selbst
die Kupfer-Akzentfarbe — unabhängig vom `crystals`-Flag, das nur die separate
Erzsplitter-Kristallgeometrie steuert. Das ist beabsichtigtes Design (kupfergeäderter Wirtsfels
bleibt sichtbar, nur die separaten Erzsplitter verschwinden), keine Verwechslung mit der
Kristallgeometrie aus dem Active-Bild.

---

## 7. Kriterium — Farbwelt = Planvorgabe

Alle 10 Paletten-Konstanten in `T1ResourceVisualBuilder.cs` (Zeilen 15–24) exakt gegen den
Plankopf (Task 3) geprüft: `Rinde (0.24, 0.14, 0.08)`, `Laub (0.16, 0.34, 0.15)`,
`LaubHell (0.28, 0.46, 0.2)`, `Beere (0.62, 0.12, 0.14)`, `Faser (0.42, 0.5, 0.28)`,
`Rispe (0.72, 0.66, 0.45)`, `Stein (0.44, 0.47, 0.5)`, `SteinHell (0.58, 0.61, 0.64)`,
`Wirt (0.35, 0.3, 0.27)`, `Kupfer (0.72, 0.4, 0.2)` — alle zehn wortgleich, keine Abweichung.
**Erfüllt.**

---

## 8. Kriterium — Höhen auf Tabellenziel (`VisualScaleTests`)

Filter `Eidren.Tests.EditMode.VisualScaleTests`, Iteration 3 (finaler Stand,
`TestResults-stil-e3b-hoehen3.xml`): `total="8" passed="8" failed="0"`. Auch im vollen Suite-Lauf
(Abschnitt 2) bestätigt: alle 8 `VisualScaleTests`-Fälle `Passed`, inkl.
`EveryBakedPrefab_MatchesItsTableHeight`.

| Visual-ID | Sollhöhe (m, Bakekette Abschnitt d) | Ergebnis |
| --- | ---: | --- |
| `visual.tree.active` | 6,0 | erreicht (analytisch exakt, Loft-Positionen ohne Kippung auf Zielhöhe dimensioniert) |
| `visual.tree.exhausted` | 0,7 | erreicht (analytisch exakt) |
| `visual.berry_bush.active` | 0,9 | erreicht (analytisch exakt) |
| `visual.berry_bush.exhausted` | 0,7 | erreicht (analytisch exakt) |
| `visual.fiber_plant.active` | 0,9 | erreicht (analytisch exakt) |
| `visual.fiber_plant.exhausted` | 0,25 | erreicht (analytisch exakt) |
| `visual.stone_deposit.active` | 1,1 | erreicht (Iteration 3, `FelsSkalaSteinAktiv` kalibriert, innerhalb 1 % Toleranz) |
| `visual.stone_deposit.exhausted` | 0,5 | erreicht (Iteration 3, zwei Korrekturrunden, innerhalb 1 % Toleranz) |
| `visual.copper_vein.active` | 1,3 | erreicht (Iteration 2, `FelsSkalaKupferAktiv` kalibriert, innerhalb 1 % Toleranz) |
| `visual.copper_vein.exhausted` | 1,3 | erreicht (Iteration 2, teilt `FelsSkalaKupferAktiv` mit Active, innerhalb 1 % Toleranz) |

3 Iterationen bis vollständig grün (Limit im Auftrag: 5, Limit im Plantext: 4 — beide eingehalten,
vollständiger Zahlenverlauf in `task-3-report.md`). **Erfüllt.**

---

## 9. Beide Vorfälle dieser Etappe (transparent, aus den Task-Reports übernommen)

### 9a. Task 2 — Falscher Unity-Editor, vollständiger Rollback

Ein früherer Bearbeitungsstand von Task 2 verwendete nachweislich **nicht** den projektvorgegebenen
`.unity-editor\Editor\Unity.exe`, sondern einen anderen, lokal installierten Unity-6-Editor — erkennbar
an 24 `CS0619`-Kompilierfehlern (`Object.GetInstanceID()`-Deprecation), die mit dem projekteigenen
Editor nicht auftreten. Dieser Bearbeitungsstand meldete fälschlich einen „Blocking Issue" wegen
angeblich vorbestehender Kompilierfehler und produzierte 0 der 12 geforderten Varianten-Vorher-PNGs.
**Rollback:** Der gesamte fehlerhafte Stand wurde verworfen; `task-2-report.md` hält ihn nur noch als
durchgestrichene Historie fest („SUPERSEDED"). Der Re-Run (08.08.2026) verwendete ausschließlich den
korrekten Editor, verifizierte, dass `CaptureT1VariantenVorher()` den Rollback unbeschadet überstanden
hatte (Methode und 12-Eintrag-Liste intakt), und lieferte alle 12 PNGs fehlerfrei — kein
`GetInstanceID`-Fehler in irgendeinem nachfolgenden Log dieser gesamten Etappe. Alle Unity-Läufe ab
diesem Zeitpunkt (Tasks 3–5, inkl. dieser Task) sind gegen den korrekten Editor verifiziert. Die vom
Fremdeditor erzeugte `Packages/packages-lock.json.fremdeditor-quarantaene` wurde in der
Fix-Runde nach `Eidren-Sicherungen\vor-stilumbau-e3b-20260808-1927\` verschoben, statt weiter
lose im Projekt zu liegen. Der ebenfalls vom Fremdeditor angelegte, leere
`ProjectSettings/PhysicsCoreProjectSettings2D.asset`-Rest bleibt als akzeptierte, beobachtete
Alt-Spur im Projekt stehen (harmlos-leer, keine Funktionsänderung).

### 9b. Task 4 — Gebietsäste doppelt so dick, Fix per Formelkorrektur

Die Review von Task 4 deckte auf, dass `AddBranch` den Dispatch-Formelvorschlag
`TaperedBox(scale * 2f, ...)` wörtlich übernahm und damit **alle drei** Achsen verdoppelte. Unitys
alter, ersetzter Zylinder hatte baseline Durchmesser 1 (Halbbreite `0.5 * scale.x`), aber baseline
Höhe 2 (Halbhöhe `1 * scale.y`) — nur die Y-Achse brauchte die Verdopplung. **Root Cause:** die
ursprüngliche Formel selbst, nicht die Umsetzung. **Fix:** Meshgröße von
`(scale.x*2, scale.y*2, scale.z*2)` auf `(scale.x, scale.y*2, scale.z)` korrigiert; Y-Achse und
Höhenversatz blieben unverändert (bereits korrekt). AABB-Beleg an allen fünf `Ast_*`-Assets
bestätigt die Korrektur analytisch exakt (`x/z = scale.x * 0.5`). Kein Re-Run von
`RebuildTierOneNodePrefabs()` nötig (Node-Prefabs referenzieren nie die Zonenvarianten-Äste,
verifiziert über den Code-Pfad `ResourceNodeVisualTable.BuildAll()`). Re-Bake, Zeitstempelwache und
Regressionslauf (39/39) nach dem Fix erneut grün. Diese Task (Abschnitt 1/6) bestätigt den Fix
zusätzlich visuell: alle Gebietsäste in den 6 betroffenen Nachher-Bildern (Marsh/Quarry/EmberRuins
× Active) wirken proportional dünn, nicht wie im ursprünglichen, unkorrigierten Zustand.

---

## 10. Offene Punkte

1. **Drei `VisualAssetTests`-Fehlschläge — behoben in der Fix-Runde (Abschnitt 2.1).** Ursprünglich
   ein zentraler Befund dieser Task (`BerryBushUsesTwoDistinctCompleteVisualStates`,
   `CopperVeinUsesDetailedThreeDimensionalHeroGeometry`, `RemainingSupportAssetsUseLayeredHeroGeometry`
   prüften Struktureigenschaften der **alten** Hero-Geometrie, die der Vollumbau ersetzte). Per
   Controller-Ruling (Spec-Zitat: „Bestehende Sichttests … werden auf die Fabrik-Ausgaben
   nachgezogen, nicht gelöscht") auf den Fabrik-Vertrag umgestellt, ohne die Prüftiefe zu senken.
   Isolierter Lauf 8/11 grün (die 3 verbleibenden sind vorbestehende, unberührte
   Baseline-Fehlschläge), voller Suite-Re-Run 0 `=>`/0 `<=` gegen die Baseline. Kein offener Punkt
   mehr.
2. **G-009-Fremdobjekt-Befund bleibt inhaltlich offen** (Abschnitt 4) — `RecognitionMarker_AxeOchre`
   ersetzt nur die Rendertechnik des alten „eingemalten Tuchs", nicht die G-009-Kernbeobachtung
   (Fremdobjekt sitzt weiterhin sichtbar am Baum). Wie im Plan vorgegeben bleibt die formale
   G-009-Abnahme eigenständig; hier nur als Klarstellung vermerkt, keine Nacharbeit in dieser Task.
3. **Formale G-005/G-009/G-010-Abnahme ist nicht Teil dieses Berichts** (Planvorgabe) — dieser
   Bericht bewertet nur die für die fünf T1-Typen sichtbaren Auswirkungen des Stilumbaus, nicht die
   projektweiten Abnahmekriterien der drei Grafikaufträge.
4. **Freigabe durch den Auftraggeber steht aus.** Dieser Bericht weist die Kriterien 1, 3–6 und (nach
   der Fix-Runde) die Regressionsfreiheit nach, markiert Kriterium 2 weiterhin mit einer
   dokumentierten Einschränkung (G-009, Punkt 2 oben) — das ist die einzige inhaltlich verbleibende
   Einschränkung dieses Berichts. Vor einer Entscheidung sind dem Auftraggeber die 24 Bilder aus
   `TempReview\StilumbauE3b\{nachher,varianten-nachher,inwelt}\` sowie der Namensdiff aus Abschnitt 2
   vorzulegen.

---

## 11. Erzeugte / geänderte Assets (gesamte Etappe 3b, Tasks 0–5)

**Geändert (Editor-Werkzeuge)**
- `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs` — `CaptureT1Uebersicht`,
  `CaptureT1VariantenVorher` (Task 2), `CaptureT1UebersichtNachher`, `CaptureT1VariantenNachher`,
  `CaptureT1InWelt`, `CaptureT1Abnahme` (neu, diese Task)
- `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` — `T1Visual_IstImVertexfarbenStil` mit 10
  Testfällen ergänzt (Task 3)
- `Assets/_Game/Editor/T1ResourceVisualBuilder.cs` — neu, Fabrik-Geometrie/Vertexfarben (Task 3)
- `Assets/_Game/Editor/AreaArtAssetBuilder.cs` — `BuildTierOne()` (Task 4)
- `Assets/_Game/Editor/AreaArtVariantPrefabBuilder.cs` — `AddBranch` auf Fabrik-Geometrie
  umgestellt, inkl. Dicken-Fix (Task 4)
- `Assets/_Game/Editor/ResourceContentBuilder.cs` — `RebuildTierOneNodePrefabs()`,
  Warnkommentar an `BuildTierTwoResourceCollection` (Task 4)
- `Assets/_Game/Editor/Tests/VisualAssetTests.cs` — `BerryBushUsesTwoDistinctCompleteVisualStates`,
  `CopperVeinUsesDetailedThreeDimensionalHeroGeometry`, `RemainingSupportAssetsUseLayeredHeroGeometry`
  auf den Fabrik-Vertrag nachgezogen (Task 5 Fix-Runde, Abschnitt 2.1)

**Prefabs (Task 3)**
- `Assets/_Game/Prefabs/Resources/Visuals/{Tree,BerryBush,FiberPlant,StoneDeposit,CopperVein}_
  {Active,Exhausted}.prefab` (10 Stück, 44–368 Dreiecke, Abschnitt 3)
- `Assets/_Game/Art/Resources/T1Meshes/` — Mesh-Assets

**Prefabs (Task 4)**
- 12 T1-Zonenvarianten unter `Assets/_Game/Prefabs/Environment/AreaArtVariants/`
  (`Greenwood_tree_*`, `Marsh_tree_*`, `Marsh_fiber_plant_*`, `Quarry_tree_*`,
  `Quarry_stone_deposit_*`, `EmberRuins_tree_*`)
- 5 T1-Node-Prefabs unter `Assets/_Game/Prefabs/Resources/Nodes/`
  (`Tree`, `StoneDeposit`, `FiberPlant`, `CopperVein`, `BerryBush`)
- `Assets/_Game/Art/Zones/Meshes/Ast_000..004_Ast.asset` — 5 Gebietsast-Meshes

**Nicht verändert (T2-Bestand, per Zeitstempeldiff aus Task 4 belegt)**
- Alle T2-Basis-Visuals, T2-Zonenvarianten, T2-Node-Prefabs
- Alle `Data/AreaArt/*.asset`, `Data/Zones/*.asset`

**Bildmaterial**
- `TempReview/StilumbauE3b/vorher/` — 10 PNGs (Task 2, kopiert aus `StilumbauE3/t1-uebersicht/`)
- `TempReview/StilumbauE3b/varianten-vorher/` — 12 PNGs (Task 2)
- `TempReview/StilumbauE3b/nachher/` — 10 PNGs (Task 5, dieser Bericht)
- `TempReview/StilumbauE3b/varianten-nachher/` — 12 PNGs (Task 5, dieser Bericht)
- `TempReview/StilumbauE3b/inwelt/` — 2 PNGs (Task 5, dieser Bericht)

**Testergebnisse (Projektwurzel)**
- `TestResults-stil-e3-final2.xml` (Baseline, aus Etappe 3 übernommen)
- `TestResults-stil-e3b-t3a.xml` … `TestResults-stil-e3b-t4c.xml` (Tasks 3–4, RED/GREEN- und
  Regressionsnachweise), `TestResults-stil-e3b-hoehen1-3.xml` (Höhen-Iteration)
- `TestResults-stil-e3b-final.xml` / `stil-e3b-final.log` (Task 5, erste Runde — 3 `=>`-Regressionen)
- `TestResults-stil-e3b-t5fix.xml` / `stil-e3b-t5fix.log` (Task 5 Fix-Runde, isolierter
  `VisualAssetTests`-Lauf, 8/11 grün — die 3 restlichen sind unberührte Baseline-Fehlschläge)
- `TestResults-stil-e3b-final2.xml` / `stil-e3b-final2.log` (Task 5 Fix-Runde, finaler Suite-Lauf —
  805/734/71, Namensdiff 0 `=>`/0 `<=`, maßgeblicher Endstand)
- `stil-e3b-t5-captures.log` (Task 5, kombinierter Nachher-/Varianten-/In-Welt-Capture-Lauf)

**Dokumentation**
- `Documentation/Etappen/Stilumbau/STILUMBAU_E3B_BAKEKETTE.md` (Task 1)
- `Documentation/Etappen/Stilumbau/STILUMBAU_E3B_ABNAHME.md` (dieser Bericht, Task 5)

**Sicherung**
- `vor-stilumbau-e3b-<stamp>` (Quellstand vor der Etappe, Task 0)

---

## 12. Nachvollziehen

Kombinierten Nachher-/Varianten-/In-Welt-Capture-Lauf neu erzeugen:

    & ".unity-editor\Editor\Unity.exe" -batchmode -projectPath "<Projektpfad>" `
      -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureT1Abnahme -quit `
      -logFile "stil-e3b-t5-captures.log"

Volle EditMode-Suite (ohne `-quit`, siehe Abschnitt 2; `final2.xml` ist der maßgebliche Endstand
nach der Fix-Runde):

    & ".unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "<Projektpfad>" `
      -runTests -testPlatform EditMode -testResults "TestResults-stil-e3b-final2.xml" `
      -logFile "stil-e3b-final2.log"

Isolierter Verifikationslauf nur der reparierten Testklasse (ohne `-quit`):

    & ".unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "<Projektpfad>" `
      -runTests -testPlatform EditMode -testFilter "Eidren.Tests.EditMode.VisualAssetTests" `
      -testResults "TestResults-stil-e3b-t5fix.xml" -logFile "stil-e3b-t5fix.log"

Namensdiff siehe Abschnitt 2 für die vollständige PowerShell-Funktion.
