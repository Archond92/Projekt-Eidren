# Stilumbau Etappe 3 Abnahme — Tier-2-Ressourcenknoten

> **Nachtrag (Fix-Runde, 08.08.2026, nach 17:00):** Der in diesem Bericht dokumentierte
> `=>`-Regressionsfund (`VisualScaleTests.EveryBakedPrefab_MatchesItsTableHeight`, Abschnitt 5)
> ist behoben. Der ursprüngliche Befund bleibt unten unverändert stehen (Transparenz); Abschnitt
> 12 fasst Fix, erneute Verifikation und den aktualisierten Freigabestatus zusammen.

Stand: 08.08.2026
Bezug: `STILUMBAU_E3_PLAN` (Etappe 3: die vier T2-Ressourcentypen HardwoodTree, SwampHemp,
GraniteDeposit, IronVein — 8 Basis-Visuals + 12 Zonenvarianten; Tier-1 tabu), Tasks 0–5
Vorher-Stand: alte `T2ResourceVisualBuilder`-Primitive (Einfarb-Materialien, keine
Vertexfarben), gesichert unter
`C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e3-20260807-1859`,
Baseline `TestResults-stil-e3-baseline.xml` (787 Tests, 716 bestanden, 71 Fehlschläge)

---

## Kurzfassung

Die acht T2-Basis-Visuals sind auf `EidrenMeshFactory`/Vertexfarben umgestellt, alle 12
Zonenvarianten neu geklont, die vier Node-Prefabs neu gebacken. Der Namensdiff gegen die
Baseline ist **nicht** bytegleich: ein neuer, ungeplanter Fehlschlag
(`VisualScaleTests.EveryBakedPrefab_MatchesItsTableHeight`) — die neu gebauten Fels-Cluster
von `GraniteDeposit`/`IronVein` unterschreiten ihre in `VisualScaleTableBuilder` hinterlegte
Zielhöhe teils deutlich außerhalb des im Plan selbst vorgegebenen ±15 %-Silhouettenkorridors.
Dieser Bericht spricht deshalb **keine** uneingeschränkte Freigabe aus, siehe Abschnitt 5 und
9. Die Freigabe der Etappe selbst ist **nicht** Teil dieses Berichts (Auftraggeber-Entscheid
steht aus).

| # | Kriterium | Ergebnis |
| --- | --- | --- |
| 1 | Kein Nachher-Bild leer | erfüllt, alle 20 PNGs 17–37 KB, alle 20 einzeln gesichtet |
| 2 | Silhouetten ±15 % Vorher/Nachher | **teilweise nicht erfüllt** — GraniteDeposit/IronVein Exhausted −62,6 %, GraniteDeposit Active −27,3 %, IronVein Active −16,2 % (Details Abschnitt 5) |
| 3 | Active/Exhausted unterscheidbar | erfüllt, an allen vier Typen |
| 4 | Zonenvarianten tragen Identitätsaufsätze weiterhin | erfüllt (YAML-Nachweis), Sichtbarkeitseinschränkung bei Granit/Eisen bleibt bestehen (Occlusion, aus Task 2 bekannt) |
| 5 | Farbwelt = alte Materialfarben | erfüllt, inkl. behobenem HardwoodTree-Kronen-Altdefekt (siehe Abschnitt 4) |
| 6 | T1 nachweislich unangetastet | erfüllt, Zeitstempelbeleg aus Task 4 |
| — | Keine Regression (Namensdiff) | **nicht erfüllt** — 1 neuer Fehlschlag (`=>`), 0 grün geworden (`<=`) |

---

## 1. Kombinierter Nachher- + In-Welt-Capture-Lauf (Owner-Auflage: EIN Unity-Start)

Die Aufträge für Plan-Step 1 (Nachher-Captures) und Plan-Step 2 (In-Welt-Stichprobe) wurden
laut ausdrücklicher Auflage des Auftraggebers **in einem gemeinsamen Unity-Start**
zusammengefasst, statt wie in Etappe 1/2 zwei getrennte `-executeMethod`-Aufrufe zu machen.

**Code-Ergänzung** `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs`:
- `CaptureT2InWelt(string ordner)` (bereits vorhanden aus einem früheren Arbeitsstand dieser
  Task) — öffnet `Zone_TwilightGrove.unity`, sucht per `Object.FindObjectsByType<ResourceNode>`
  (Namespace `Eidren.Interaction`, verifiziert vor dem Schreiben) je ein Exemplar, dessen
  GameObject-Name `"HardwoodTree"`/`"Hartholzbaum"` bzw. `"SwampHemp"`/`"Sumpfhanf"` enthält;
  findet die Editor-Szene (erwartungsgemäß laut `STILUMBAU_E3_BAKEKETTE.md` Abschnitt c) keine
  Laufzeit-Knoten, werden `HardwoodTree.prefab` bei `(0,0,0)` und `SwampHemp.prefab` bei
  `(6,0,0)` temporär instanziiert, mit Kameraversatz `(3.2, 4.6, −4.2)` in 1280×720 gerendert
  und per `DestroyImmediate` wieder entfernt; Szene wird nicht gespeichert.
- **Neu in dieser Task:** `CaptureT2Abnahme()`, MenuItem
  `Eidren/V0.2/Stilumbau/T2 Abnahme-Captures` — ruft ausschließlich
  `CaptureT2RessourcenNachher()` gefolgt von `CaptureT2InWelt()` auf; reine Delegation, kein
  Verhaltensunterschied gegenüber den Einzelaufrufen, nur ein gebündelter Prozessstart.

**Lauf:** Lockfile vor dem Start geprüft (`Library/UnityLockfile`, `Temp/UnityLockfile`,
`tasklist`) — keins vorhanden, kein Unity-Prozess aktiv.

```
& ".unity-editor\Editor\Unity.exe" -batchmode -projectPath "<Projektpfad>" `
  -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureT2Abnahme `
  -logFile "stil-e3-t5-captures.log" -quit
```

Ohne `-nographics` (Vorgabe für Captures). **Ein** Prozessstart, **eine** Compile-Phase,
**ein** `start-time`-Fenster im Log. `error CS`: 0 Treffer. `exception`: 0 Treffer (case-
insensitiv geprüft). Sauberer Exit: `Batchmode quit successfully invoked` /
`Exiting batchmode successfully now!` / „Application will terminate with return code 0".
23 `[StilE3]`-Zeilen: 20 „geschrieben:"-Zeilen (Nachher-Studio-Captures), 1
„In-Welt-Stichprobe-Pfad:"-Zeile, 2 „In-Welt geschrieben:"-Zeilen — exakt die erwartete
Summe aus einem Lauf.

**Ergebnis:** `TempReview\StilumbauE3\nachher\` — 20/20 PNGs, `TempReview\StilumbauE3\inwelt\`
— 2/2 PNGs (`HardwoodTree.png`, `SwampHemp.png`). In-Welt-Pfad laut Log: „temporaere
Node-Prefab-Instanziierung (keine Laufzeit-Knoten in der Szene gefunden)" — der in Task 1
vorhergesagte Fall (Knoten spawnen nur zur Laufzeit via `ZoneResourcePopulator`), kein Fehler.

### Nachher-PNGs (20/20, alle einzeln mit dem Read-Tool angesehen)

| Datei | Bytes | Sichtbefund |
| --- | ---: | --- |
| HardwoodTree_Active.png | 23 100 | Stamm + **grüne** zweistufige Krone vollständig im Bild — Altdefekt behoben, siehe Abschnitt 4 |
| HardwoodTree_Exhausted.png | 17 158 | Stumpf + Splitter, vollständig im Bild |
| SwampHemp_Active.png | 37 240 | 11 Halme mit hellen Köpfen, vollständig im Bild |
| SwampHemp_Exhausted.png | 17 734 | 7 Halme ohne Köpfe, vollständig im Bild |
| GraniteDeposit_Active.png | 28 763 | Felscluster (7 Rocks), hellgrau/blaugrau, vollständig im Bild |
| GraniteDeposit_Exhausted.png | 18 740 | Kleinerer Felscluster (4 Rocks), vollständig im Bild |
| IronVein_Active.png | 30 808 | Felscluster + orangebraune Erzsplitter, vollständig im Bild |
| IronVein_Exhausted.png | 17 994 | Felscluster (4 Rocks) ohne Erzsplitter, vollständig im Bild |
| TwilightGrove_hardwood_tree_Active.png | 23 147 | Wie Basis, zusätzlich brauner Identitäts-Klotz im Blattwerk sichtbar |
| TwilightGrove_hardwood_tree_Exhausted.png | 17 256 | Stumpf vollständig im Bild |
| TwilightGrove_swamp_hemp_Active.png | 37 386 | Halme + brauner Identitäts-Klotz zwischen den Stängeln klar sichtbar |
| TwilightGrove_swamp_hemp_Exhausted.png | 17 564 | Wie Active, Identitäts-Klotz weiterhin sichtbar |
| VeilMarsh_swamp_hemp_Active.png | 37 386 | Wie TwilightGrove-Pendant |
| VeilMarsh_swamp_hemp_Exhausted.png | 17 564 | Wie TwilightGrove-Pendant |
| VeilMarsh_iron_vein_Active.png | 30 717 | Wie IronVein-Basis, Identitäts-Klotz aus dieser Kameraperspektive verdeckt (Occlusion, s. Task 2) |
| VeilMarsh_iron_vein_Exhausted.png | 18 183 | Wie IronVein-Basis, verdeckt |
| GreyRifts_granite_deposit_Active.png | 28 586 | Wie GraniteDeposit-Basis, Identitäts-Klotz verdeckt |
| GreyRifts_granite_deposit_Exhausted.png | 18 854 | Wie GraniteDeposit-Basis, verdeckt |
| GreyRifts_iron_vein_Active.png | 30 717 | Wie IronVein-Basis, verdeckt |
| GreyRifts_iron_vein_Exhausted.png | 18 183 | Wie IronVein-Basis, verdeckt |

Kein leeres oder rein einfarbiges Bild (alle > 17 KB), kein Beschnitt am Bildrand. **Kriterium
1 erfüllt.**

**Ergänzender Hash-Nachweis (Update gegenüber Task 2):** Anders als bei den alten Primitiven
(Task 2: Granit-/Eisen-Basis↔Varianten-Paare waren SHA-256-identisch) sind nach dem Task-3/4-
Neubau **keine** der geprüften Basis/Varianten-Paare mehr byte-identisch (`GraniteDeposit_*`
↔ `GreyRifts_granite_deposit_*`, `IronVein_*` ↔ `GreyRifts_iron_vein_*`/`VeilMarsh_iron_vein_*`,
`HardwoodTree_Exhausted` ↔ `TwilightGrove_hardwood_tree_Exhausted`). Die Dateigrößen bleiben
sich aber sehr ähnlich (z. B. 28 763 vs. 28 586 Byte) und visuell ist an Granit/Eisen aus
dieser Kameraperspektive weiterhin kein Identitäts-Klotz zu erkennen — die geänderte
Loft-Geometrie erzeugt minimal andere Kantenglättung/Schatten, aber die Occlusion selbst
besteht laut Sichtprüfung fort (siehe Kriterium 4, Abschnitt 6).

### In-Welt-PNGs (2/2, angesehen)

| Datei | Bytes | Sichtbefund |
| --- | ---: | --- |
| HardwoodTree.png | 1 395 696 | Kamera sehr nah am Stammfuß; der Rahmen zeigt praktisch nur den unteren, gegenlicht-dunklen Stamm samt Bodenschatten auf der Zone-Wiese — die Krone (Zielhöhe ~5,75) liegt bei diesem fest vorgegebenen Kameraversatz `(3.2, 4.6, −4.2)` oberhalb des Bildausschnitts |
| SwampHemp.png | 1 421 773 | Halme (dunkel, gegenlicht) klar als Gruppe erkennbar, Baumstamm des benachbarten HardwoodTree-Exemplars am linken Bildrand mit im Bild |

**Einordnung:** Die In-Welt-Stichprobe ist laut Plan Step 2 mit einem festen, aus der
Weltkisten-Nahaufnahme (Etappe 1) übernommenen Kameraversatz spezifiziert. Dieser Versatz ist
für kompakte Objekte (Kisten, ~1 m) kalibriert; beim ~5,75 Einheiten hohen HardwoodTree
schneidet er die Krone aus dem Bild und zeigt vorwiegend den unbeleuchteten Stamm. Das ist eine
Einschränkung der vorgegebenen Kameraspezifikation, kein Befund am Builder — die Studio-
Nachher-Aufnahme (`HardwoodTree_Active.png`, Abschnitt 1) zeigt die Krone vollständig und
bestätigt den Farbfix unabhängig davon. Als offener Punkt vermerkt (Abschnitt 9).

---

## 2. Namensdiff gegen Baseline (Regressionsprüfung)

Volle EditMode-Suite, `-nographics`, **ohne** `-quit` (Erkenntnis dieser Task: `-runTests`
und `-quit` zusammen lassen Unity nach dem Asset-Refresh vorzeitig beenden, bevor der
Testlauf überhaupt startet — zwei fehlgeschlagene Versuche mit `-quit` endeten nach 6–8 s
ohne jede Testausführung, siehe `stil-e3-final.log`-Historie; der dritte Versuch ohne `-quit`
lief korrekt durch und beendete sich über den Testrunner selbst). `TestResults-stil-e3-
final.xml`, Log `stil-e3-final.log`:

```
<test-run id="2" testcasecount="795" result="Failed(Child)" total="795" passed="723" failed="72" ... start-time="2026-08-08 15:15:23Z" end-time="2026-08-08 15:15:37Z" duration="14,337553">
```

`error CS` im Log: 0 Treffer.

| | gesamt | bestanden | fehlgeschlagen |
| --- | ---: | ---: | ---: |
| Baseline (Task 0) | 787 | 716 | 71 |
| Endstand (Task 5) | 795 | 723 | 72 |

Der Zuwachs von 787 auf 795 Gesamttests stammt aus den 8 neuen
`T2Visual_IstImVertexfarbenStil`-Fällen (Task 3) — alle grün, keiner in der Failed-Liste.

```powershell
$basis = ([xml](Get-Content "TestResults-stil-e3-baseline.xml")).SelectNodes("//test-case[@result='Failed']") | % { $_.fullname } | Sort-Object -Unique
$neu   = ([xml](Get-Content "TestResults-stil-e3-final.xml")).SelectNodes("//test-case[@result='Failed']")   | % { $_.fullname } | Sort-Object -Unique
Compare-Object $basis $neu
```

```
InputObject                                                                    SideIndicator
-----------                                                                    -------------
Eidren.Tests.EditMode.VisualScaleTests.EveryBakedPrefab_MatchesItsTableHeight  =>
```

**`<=` (grün geworden):** keine Einträge.

**`=>` (neuer Fehlschlag, NICHT erwartet):** `VisualScaleTests.
EveryBakedPrefab_MatchesItsTableHeight`. Root-Cause siehe Abschnitt 5 — die Testmeldung
selbst:

```
§25: Die Hoehe steht im Prefab oder sie steht nirgends:
visual.swamp_hemp.active: gemessen 1,488, erwartet 1,372
visual.swamp_hemp.exhausted: gemessen 0,234, erwartet 0,228
visual.granite_deposit.active: gemessen 0,920, erwartet 1,266
visual.granite_deposit.exhausted: gemessen 0,280, erwartet 0,749
visual.iron_vein.active: gemessen 1,180, erwartet 1,408
visual.iron_vein.exhausted: gemessen 0,280, erwartet 0,749
```

Dieser Test lief in Task 3 und Task 4 **nicht mit** — beide Tasks filterten gezielt auf
`Eidren.Tests.EditMode.EidrenWorldStyleTests` bzw. `V02ContainerVisualTests` (dokumentiert,
schmale, absichtliche Regressionswache für den jeweiligen Task-Umfang), nicht auf die volle
Suite. `VisualScaleTests` war Teil der Baseline (Task 0, volle Suite) und dort grün — Task 5
ist damit der erste Lauf seit dem Geometrie-Neubau, der diesen Test überhaupt wieder
ausführt, und deckt die Abweichung auf. Kein Flake: reproduzierbar über zwei unabhängige
Läufe (der hier dokumentierte sowie ein früherer, separat gestarteter Lauf mit identischem
Ergebnis 795/723/72).

Die übrigen 71 Fehlschläge sind namensgleich zur Baseline (dieselben Altlasten der
Dekompilat-Rekonstruktion, vgl. Memory-Notiz „EditMode-Baseline hat 73 Fehlschläge" — für
Etappe 3 gilt die hier gezogene, aktuelle Baseline mit 71).

---

## 3. Dreieckszahlen (8 T2-Basis-Visuals)

Aus den `[StilE3]`-Log-Zeilen des Builder-Laufs (Task 3, `stil-e3-t3-build2.log`):

| Prefab | Dreiecke | Korridor (Spec) |
| --- | ---: | --- |
| HardwoodTree_Active | 192 | ≤ 700 ✓ |
| HardwoodTree_Exhausted | 56 | ≤ 500 ✓ |
| SwampHemp_Active | 264 | ≤ 500 ✓ |
| SwampHemp_Exhausted | 84 | ≤ 500 ✓ |
| GraniteDeposit_Active | 308 | ≤ 500 ✓ |
| GraniteDeposit_Exhausted | 176 | ≤ 500 ✓ |
| IronVein_Active | 368 | ≤ 500 ✓ |
| IronVein_Exhausted | 176 | ≤ 500 ✓ |

Alle acht liegen im spezifizierten Korridor (56–368 Dreiecke, deutlich unter der 500er- bzw.
700er-Grenze). Bestätigt durch `T2Visual_IstImVertexfarbenStil` (8/8 grün, Abschnitt 2).

---

## 4. HardwoodTree-Kronenfarbe — gezielte Bestätigung

In Task 2 wurde als Nebenbefund dokumentiert: das alte `HardwoodTree_Active`-Basis-Visual
rendert komplett rotbraun (auch die Krone trägt die Rinden-Farbe) — ein Materialfehler des
Vorher-Stands, unabhängig vom Umbau. `TwilightGrove_hardwood_tree_Active` (die Zonenvariante)
zeigte dagegen schon vorher eine grüne Krone.

**Nachher-Befund (dieser Task):** `HardwoodTree_Active.png` (Abschnitt 1) zeigt eine deutlich
**grüne**, zweistufige Krone (`Laub`/`LaubHell`-Palette aus `T2ResourceVisualBuilder`) über
dem rotbraunen Stamm — der Altdefekt ist mit dem Fabrik-Umbau in Task 3 behoben, ohne dass er
gezielt adressiert wurde (er verschwand automatisch, weil `Teil()` jedem Meshteil sein eigenes
`Color`-Argument mitgibt statt eines einzigen, für das ganze Prefab geltenden Materials wie
beim alten `Material`-Ansatz). `TwilightGrove_hardwood_tree_Active.png` zeigt weiterhin die
gleiche grüne Krone — Basis und Variante sind jetzt farblich konsistent, was vorher nicht der
Fall war. **Bestätigt und hervorgehoben wie im Auftrag verlangt.**

---

## 5. Kriterium — Silhouetten ±15 % Vorher/Nachher (zentraler Befund dieser Task)

Der Plan definiert den Silhouettenkorridor über die Primitiv-Vorgänger-Maße
(„Kronenhöhe ~5,75, Stumpf ~0,4, Hanf ~0,9, Felsen ~0,7") und verweist auf
`VisualScaleTableBuilder` als Quelle der Collider-Maße, die **nicht geändert werden darf**.
Der in Abschnitt 2 gefundene `VisualScaleTests`-Fehlschlag liefert dafür exakte Zahlen:

| Visual | Gemessen | Erwartet (Tabelle) | Abweichung | Im ±15 %-Korridor? |
| --- | ---: | ---: | ---: | :---: |
| SwampHemp Active | 1,488 | 1,372 | +8,5 % | ✓ |
| SwampHemp Exhausted | 0,234 | 0,228 | +2,6 % | ✓ |
| GraniteDeposit Active | 0,920 | 1,266 | **−27,3 %** | ✗ |
| GraniteDeposit Exhausted | 0,280 | 0,749 | **−62,6 %** | ✗ |
| IronVein Active | 1,180 | 1,408 | **−16,2 %** | ✗ (knapp) |
| IronVein Exhausted | 0,280 | 0,749 | **−62,6 %** | ✗ |
| HardwoodTree Active/Exhausted | — | — | — | nicht in der Failed-Liste — `T2ResourceVisualBuilder` wurde für Hardwood erkennbar gezielt auf die Tabellenhöhen (6,65 / 0,84) hin gebaut (Loft-Positionen summieren sich exakt) |

**Befund:** SwampHemp bleibt innerhalb des ±15 %-Korridors. HardwoodTree trifft die
Tabellenhöhen exakt (kein Fehlschlag). **GraniteDeposit und IronVein verfehlen den Korridor**
— am stärksten die beiden Exhausted-Varianten (jeweils −62,6 %, die Fels-Cluster sind nach
dem Neubau nur rund ein Drittel so hoch wie vorher/wie die Tabelle erwartet), aber auch
GraniteDeposit Active (−27,3 %) und IronVein Active (−16,2 %, knapp über der Grenze).

**Root Cause:** `T2ResourceVisualBuilder.BuildRock()` (Task 3) berechnet die Fels-Höhe pro
Cluster-Instanz aus einer festen Formel (`0.48 + (index % 3) * 0.22` für Active,
`0.18 + (index % 2) * 0.1` für Exhausted), unabhängig vom `VisualScaleTableBuilder`-Zielwert.
Für Hardwood wurde die Loft-Geometrie erkennbar auf die Tabellenhöhe hin dimensioniert
(Kronenspitze bei y = 4,85 + 1,8 = 6,65, exakt der Tabellenwert) — für die Felsvisuals fehlt
diese Abstimmung.

**Bewertung:** Dies ist kein Test-Artefakt und keine reine 1 %-Toleranz-Pedanterie — bei
Exhausted-Granit/Eisen liegt die tatsächliche Abweichung mit −62,6 % weit außerhalb des vom
Plan selbst gesetzten ±15 %-Fensters, also ein echter Verstoß gegen die im Plankopf
festgehaltene Silhouetten-Vorgabe, nicht nur gegen die (strengere) 1 %-Testschwelle. Eine
Korrektur würde eine Anpassung der `BuildRock()`-Höhenformel (Task-3-Scope) oder eine bewusste
Neukalibrierung der Tabellenwerte erfordern — beides liegt außerhalb des Task-5-Auftrags
(Captures, Diff, Dokumentation) und wird hier als offener Punkt an den Auftraggeber
weitergereicht (Abschnitt 9), nicht eigenmächtig behoben.

---

## 6. Kriterium — Active/Exhausted unterscheidbar, Identitätsaufsätze, Farbwelt

**Active/Exhausted:** an allen vier Typen klar unterscheidbar (Abschnitt 1) — HardwoodTree
(volle Krone vs. Stumpf+Splitter), SwampHemp (11 Halme mit Köpfen vs. 7 kahle Halme),
GraniteDeposit/IronVein (7-teiliger vs. 4-teiliger Cluster, IronVein Active zusätzlich mit 5
Erzsplittern). **Erfüllt.**

**Identitätsaufsätze:** `AreaIdentityGeometry` per `grep` in den aktuellen (Task-4-neu-
geklonten) Variant-Prefabs verifiziert — vorhanden in `GreyRifts_granite_deposit_Active`,
`GreyRifts_iron_vein_Active`, `VeilMarsh_iron_vein_Active`, `TwilightGrove_swamp_hemp_Active`
(stellvertretend geprüft). Sichtbar bei Hemp (brauner Klotz zwischen den Halmen, alle vier
Hemp-Varianten), bei Granit/Eisen aus der Studio-Kameraperspektive weiterhin verdeckt (aus
Task 2 bekannte, hier bestätigt fortbestehende Einschränkung — siehe Hash-Nachweis Abschnitt
1). Die Geometrie ist nachweislich vorhanden, nur an zwei von sechs Zonenvarianten-Paaren aus
diesem Blickwinkel nicht sichtbar. **Erfüllt, mit dokumentierter Einschränkung.**

**Farbwelt:** alle acht Paletten-Konstanten aus `T2ResourceVisualBuilder.Build()` sind exakt
die alten Materialfarbwerte (`Rinde`, `Laub`, `LaubHell`, `Hanf`, `HanfKopf`, `Granit`,
`GranitHell`, `Eisenfels`, `Eisenerz` — siehe Plankopf, unverändert übernommen). Visuell
bestätigt: rotbrauner Stamm, grüne Krone (Abschnitt 4), oliv-grüner Hanf mit hellen Köpfen,
grau-blaue Granit-Töne, dunkler Eisenfels mit ockerfarbenen Erzsplittern. **Erfüllt.**

---

## 7. Kriterium — Tier-1 nachweislich unangetastet

Aus Task 4 (Zeitstempeldiff, siehe `task-4-report.md`, dort Schritt 1–2, erweiterter Fix-
Nachweis „Fix 3"): 120 Dateien vor/nach den beiden T2-only-Läufen (`BuildTierTwo`,
`RebuildTierTwoNodePrefabs`) erfasst — 10 T1-Visual-Prefabs, 10 T1-Zonenvarianten (je
Active/Exhausted), 5 T1/Sonderfall-Node-Prefabs, alle `AreaArt_*.asset`/`Zone_*.asset`
Datenassets samt `.meta`-Dateien. **Alle** T1-bezogenen Dateien zeitstempel- und (für die
sechs anfangs mitgeschriebenen Datenassets) byte-identisch zum Vor-Etappe-3-Backup — nur die
12 T2-Varianten-Prefabs und 4 T2-Node-Prefabs änderten sich. **Erfüllt**, Beleg bereits in
Task 4 erbracht, hier für die Abnahme referenziert statt dupliziert.

---

## 8. Beide Vorfälle dieser Etappe (transparent, aus den Task-Reports übernommen)

### 8a. Task 3 — Session-Unterbrechung über Nacht + toter Frühlauf überschrieb das Build-Log

Der ursprüngliche `BuildStandalone`-Lauf (07.08., 20:34) baute die acht Visual-Prefabs
erfolgreich (Zeitstempel- und Asset-Zahl-Beleg: 62 Mesh-Assets unter `T2Meshes/`), das
zugehörige Log wurde jedoch von einem späteren, toten Zwischenlauf (08.08., 11:10 —
abgebrochen während der Skriptkompilierung, vor jedem Schreibzugriff, kein Assetschaden)
überschrieben. Die Dreieckszahlen des Ursprungslaufs waren dadurch nicht mehr direkt belegt.
**Wiederherstellung:** ein idempotenter Zweitlauf (`stil-e3-t3-build2.log`) reproduzierte
exakt dieselben Prefabs (deterministischer `Persist`-Mechanismus, stabile GUIDs) und lieferte
die in Abschnitt 3 dokumentierten, jetzt belegten Dreieckszahlen. Kein Datenverlust, nur ein
Log-Beleg musste neu erzeugt werden.

### 8b. Task 4 — `BuildTierTwo` schrieb ungeplant sechs T2-Datenassets mit, Fix per `CloneVariants`-Extraktion

Die Review deckte auf, dass die erste Fassung von `AreaArtAssetBuilder.BuildTierTwo()` das
bestehende `Build(spec)` unverändert weiterverwendete — inklusive des Teils, der
`AreaArt_{Key}.asset` und über `AssignZoneDefinitions` auch `Zone_{Key}.asset` neu schreibt.
Der ursprüngliche 86-Datei-Zeitstempel-Snapshot deckte diese sechs Datenassets nicht ab.
**Fix:** der reine Klon-Teil wurde in eine neue private Methode `CloneVariants(...)`
ausgelagert; `BuildTierTwo()` ruft jetzt nur noch diese auf — kein `AreaArt_*.asset`/
`Zone_*.asset`-Schreiben mehr. Die sechs in Runde 1 bereits mitgeschriebenen Dateien wurden
per Diff gegen das Vor-Etappe-3-Backup als byte-identisch (reine Re-Serialisierung ohne
Inhaltsänderung) verifiziert und bewusst nicht zurückgesetzt. Ein erweiterter 120-Datei-
Snapshot mit dem verengten Codepfad bestätigte danach: nur noch die 12 T2-Varianten ändern
sich. Vollständiger Nachweis in `task-4-report.md`.

---

## 9. Offene Punkte

1. **Silhouettenkorridor verfehlt bei GraniteDeposit/IronVein** (Abschnitt 5, zentraler
   Befund) — `GraniteDeposit_Exhausted`/`IronVein_Exhausted` je −62,6 %, `GraniteDeposit_
   Active` −27,3 %, `IronVein_Active` −16,2 % gegenüber der `VisualScaleTableBuilder`-
   Zielhöhe, alle außerhalb des im Plankopf selbst vorgegebenen ±15 %-Fensters. Verursacht
   den einzigen `=>`-Regressionsfund in Abschnitt 2 (`VisualScaleTests.
   EveryBakedPrefab_MatchesItsTableHeight`). **Nicht behoben** — Korrektur läge in
   `T2ResourceVisualBuilder.BuildRock()` (Task-3-Scope) oder erforderte eine bewusste
   Neukalibrierung der Tabellenwerte; beides außerhalb des Task-5-Auftrags. Entscheidung beim
   Auftraggeber.
2. **In-Welt-Capture `HardwoodTree.png` zeigt kaum mehr als den Stamm** (Abschnitt 1) — der
   plankonform feste Kameraversatz `(3.2, 4.6, −4.2)` schneidet die Krone eines ~5,75 Einheiten
   hohen Baums aus dem Bild; die Studio-Aufnahme bestätigt die Krone unabhängig davon. Keine
   Nacharbeit in dieser Task (Kameraspezifikation ist Plan-Vorgabe, nicht Ermessenssache von
   Task 5).
3. **`-runTests` + `-quit` beendet Unity vorzeitig** (Abschnitt 2, neu beobachtet in dieser
   Etappe) — zwei Läufe mit explizitem `-quit` endeten nach 6–8 s ohne Testausführung, weil
   `-quit` offenbar vor dem asynchron gestarteten Testrunner greift. Für zukünftige Test-Läufe
   in diesem Projekt: `-runTests` ohne `-quit` verwenden (der Runner beendet den Prozess
   selbst). Nicht plan-kritisch, aber als Werkzeug-Erkenntnis vermerkt (ergänzt die
   `unity-batch-tests`-Projektnotiz).
4. **Freigabe durch den Auftraggeber steht aus.** Dieser Bericht weist die Kriterien 1, 3, 4,
   5, 6 nach, benennt Kriterium 2 (Silhouettenkorridor) für zwei von acht Visualtypen als
   **nicht erfüllt** und macht damit **keine** Freigabeempfehlung. Vor einer Entscheidung sind
   dem Auftraggeber die 20 Bildpaare aus `TempReview\StilumbauE3\vorher\` und
   `TempReview\StilumbauE3\nachher\` sowie der Befund aus Abschnitt 5 vorzulegen.

---

## 10. Erzeugte / geänderte Assets (gesamte Etappe 3, Tasks 0–5)

**Geändert (Editor-Werkzeuge)**
- `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs` — `CapturePrefabListe`-Helfer
  (Task 2), `CaptureT2InWelt` (früherer Arbeitsstand dieser Task), `CaptureT2Abnahme` (neu,
  diese Task — kombinierter Sammellauf)
- `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` — `T2Visual_IstImVertexfarbenStil` mit
  8 Testfällen ergänzt (Task 3)
- `Assets/_Game/Editor/T2ResourceVisualBuilder.cs` — vollständig neu auf
  `EidrenMeshFactory`/Vertexfarben umgestellt (Task 3)
- `Assets/_Game/Editor/AreaArtAssetBuilder.cs` — `CloneVariants`-Extraktion + `BuildTierTwo()`
  (Task 4)
- `Assets/_Game/Editor/ResourceContentBuilder.cs` — `RebuildTierTwoNodePrefabs()` (Task 4)

**Prefabs (Task 3)**
- `Assets/_Game/Prefabs/Resources/Visuals/{HardwoodTree,SwampHemp,GraniteDeposit,IronVein}_
  {Active,Exhausted}.prefab` (8 Stück, 56–368 Dreiecke, Abschnitt 3)
- `Assets/_Game/Art/Resources/T2Meshes/` — 62 Mesh-Assets

**Prefabs (Task 4)**
- 12 T2-Zonenvarianten unter `Assets/_Game/Prefabs/Environment/AreaArtVariants/`
  (`TwilightGrove_hardwood_tree_*`, `TwilightGrove_swamp_hemp_*`, `VeilMarsh_swamp_hemp_*`,
  `VeilMarsh_iron_vein_*`, `GreyRifts_granite_deposit_*`, `GreyRifts_iron_vein_*`)
- 4 T2-Node-Prefabs unter `Assets/_Game/Prefabs/Resources/Nodes/`
  (`HardwoodTree`, `SwampHemp`, `GraniteDeposit`, `IronVein`)

**Nicht verändert (Tier-1-Tabu, per Zeitstempeldiff aus Task 4 belegt)**
- Alle 10 T1-Visual-Prefabs, alle 10 T1-Zonenvarianten, die 5 T1/Sonderfall-Node-Prefabs
- Alle `Data/AreaArt/*.asset`, `Data/Zones/*.asset` (nach dem Task-4-Fix inhaltlich
  unverändert, s. Abschnitt 8b)

**Bildmaterial**
- `TempReview/StilumbauE3/vorher/` — 20 PNGs (Task 2)
- `TempReview/StilumbauE3/nachher/` — 20 PNGs (Task 5, dieser Bericht)
- `TempReview/StilumbauE3/inwelt/` — 2 PNGs (Task 5, dieser Bericht)

**Testergebnisse (Projektwurzel)**
- `TestResults-stil-e3-baseline.xml` / `stil-e3-baseline.log` (Task 0)
- `TestResults-stil-e3-t3a.xml` … `TestResults-stil-e3-t4d.xml` (Tasks 3–4, RED/GREEN- und
  Regressionsnachweise)
- `TestResults-stil-e3-final.xml` / `stil-e3-final.log` (Task 5, Endstand)
- `stil-e3-t5-captures.log` (Task 5, kombinierter Nachher-/In-Welt-Capture-Lauf)

**Dokumentation**
- `Documentation/Etappen/Stilumbau/STILUMBAU_E3_BAKEKETTE.md` (Task 1)
- `Documentation/Etappen/Stilumbau/STILUMBAU_E3_ABNAHME.md` (dieser Bericht, Task 5)

**Sicherung**
- `C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e3-20260807-1859`
  (Quellstand vor der Etappe, Task 0)

---

## 11. Nachvollziehen

Kombinierten Nachher-/In-Welt-Capture-Lauf neu erzeugen:

    & ".unity-editor\Editor\Unity.exe" -batchmode -projectPath "<Projektpfad>" `
      -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureT2Abnahme -quit `
      -logFile "stil-e3-t5-captures.log"

Volle EditMode-Suite (ohne `-quit`, siehe Abschnitt 9 Punkt 3):

    & ".unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "<Projektpfad>" `
      -runTests -testPlatform EditMode -testResults "TestResults-stil-e3-final.xml" `
      -logFile "stil-e3-final.log"

Namensdiff siehe Abschnitt 2 für die vollständige PowerShell-Funktion.

---

## 12. Nachtrag — Fix-Runde (08.08.2026, nach 17:00)

Diese Runde behebt den in Abschnitt 5/9 dokumentierten Regressionsfund. Vollständiger
technischer Nachweis in `task-3-report.md` (Abschnitt „Fix-Runde"); hier nur die für die
Freigabe relevante Zusammenfassung.

### 12.1 Root Cause (bestätigt)

`T2ResourceVisualBuilder.BuildRock()` berechnete die Fels-Höhe aus einer festen Formel ohne
Bezug zum `VisualScaleTableBuilder`-Zielwert; die reine Gierrotation der Felsen (nur Y-Achse)
liefert zusätzlich eine kleinere Achsen-Bounding-Box als die alte, primitivbasierte Geometrie,
auf die die Tabelle kalibriert ist. Ergänzend fiel auf, dass auch `BuildHemp()` leicht daneben
lag (Kopf-/Halmgeometrie), was der schmale Task-3/4-Testfilter (`EidrenWorldStyleTests`) nicht
erfasst hatte, weil er keine Höhentoleranz gegen die Tabelle prüft.

### 12.2 Fix

`Assets/_Game/Editor/T2ResourceVisualBuilder.cs`: `BuildRock()` erhält zwei neue Parameter
(`felsSkala`, `erzSkala`), die die Fels-Höhenformel bzw. die Erzsplitter-Position/-Höhe
skalieren; `BuildHemp()` erhält einen Korrekturfaktor `HalmSkalaVerbraucht` für den
Exhausted-Halm sowie eine korrigierte Kopfgeometrie/-position. Alle Faktoren sind aus der
gemessenen Baseline (`TestResults-stil-e3-t3fix-baseline.xml`) hergeleitet: für drei der vier
Felswerte (Granit Active/Exhausted, Eisen Exhausted) ist die Korrektur analytisch exakt, weil
diese Bounding-Box ausschließlich aus reiner Y-Skalierung ohne Kipprotation entsteht. Für
Eisen Active (Erzsplitter tragen zur Bounding-Box bei, mit Kipprotation) wurde der Faktor
empirisch bestätigt (siehe 12.3). Komposition (7/4 Felsen, 5 Splitter), Footprint und Palette
unverändert, wie vom Auftrag verlangt.

### 12.3 Verifikation

| Nachweis | Datei | Ergebnis |
| --- | --- | --- |
| Neubau der 8 Basis-Visuals | `stil-e3-t3-build3.log` | 8 `[StilE3]`-Zeilen, Dreieckszahlen unverändert (192/56/264/84/308/176/368/176), 0 `error CS` |
| Isolierter Höhentest | `TestResults-stil-e3-t3fix.xml` | `total="1" passed="1" failed="0"` |
| Kombiniert (Stil + Größentabelle) | `TestResults-stil-e3-t3fix-combined.xml` | `total="29" passed="29" failed="0"` |
| T2-Zonenvarianten neu geklont | `stil-e3-t4-variants3.log` | „Tier-two zone variants cloned.", 0 `error CS`, T1-Varianten zeitstempel-unverändert (06.08.) |
| T2-Node-Prefabs neu gebacken | `stil-e3-t4-nodes3.log` | „Tier-two node prefabs rebuilt.", 0 `error CS` |
| Volle EditMode-Suite | `TestResults-stil-e3-final2.xml` | `total="795" passed="724" failed="71"` — namensgleich zur Baseline (`TestResults-stil-e3-baseline.xml`, 71 Fehlschläge), Namensdiff: **0** `=>`, **0** `<=` |
| Nachher-Recapture (20 PNGs) | `stil-e3-t5-nachher2.log` → `TempReview/StilumbauE3/nachher/` | 20/20 geschrieben; die 10 fels-/erzbezogenen PNGs (`GraniteDeposit_*`, `IronVein_*`, `GreyRifts_granite_deposit_*`, `GreyRifts_iron_vein_*`, `VeilMarsh_iron_vein_*`) einzeln angesehen: sichtbar höhere, proportionale Fels-Cluster, Erzsplitter bei IronVein Active ragen wie vorgesehen über die Felsen hinaus, keine Clipping-Artefakte, Identitäts-Klotz-Occlusion wie zuvor dokumentiert unverändert |

Gemessene Endwerte (Tabellenziel in Klammern): GraniteDeposit Active 1,266 (1,266, analytisch
exakt), GraniteDeposit Exhausted 0,749 (0,749, exakt), IronVein Exhausted 0,749 (0,749, exakt),
IronVein Active innerhalb ±1 % von 1,408 (Erzsplitter-Kipprotation macht den Wert nicht
geschlossen berechenbar; belegt durch den bestandenen Test, kein exakter Messwert protokolliert,
da eine bestandene Assertion keine Zahl ausgibt — ehrlich als Bereich statt als erfundene
Kommastelle ausgewiesen). SwampHemp Active/Exhausted ebenfalls innerhalb ±1 % (Test bestanden).

### 12.4 Aktualisierter Freigabestatus

| # | Kriterium | Ergebnis (Nachtrag) |
| --- | --- | --- |
| 2 | Silhouetten ±15 % Vorher/Nachher | **jetzt erfüllt** — alle vier Werte treffen die Tabellenziele innerhalb der 1 %-Testtoleranz, deutlich innerhalb ±15 % |
| — | Keine Regression (Namensdiff) | **jetzt erfüllt** — 0 `=>`, 0 `<=` gegen die Task-0-Baseline |

Damit sind alle sieben Kriterien aus der Kurzfassung erfüllt. Die übrigen offenen Punkte aus
Abschnitt 9 (Kameraversatz-Einschränkung beim In-Welt-Capture, `-quit`-Erkenntnis) sind
kosmetisch/werkzeugbezogen und bleiben unverändert bestehen. **Freigabe-Entscheid liegt weiterhin
beim Auftraggeber** — dieser Nachtrag entfernt den einzigen inhaltlichen Blocker, spricht aber
selbst keine Freigabe aus. Die In-Welt-Bilder wurden nach der Höhenfix erneuert (19:07 UTC, siehe
Logdatei `stil-e3-t5-inwelt2.log` und `TempReview/StilumbauE3/inwelt/` mit aktualisierten PNGs).
