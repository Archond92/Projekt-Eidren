# Stilumbau Etappe 2 Abnahme — Forge-Kisten

Stand: 07.08.2026
Bezug: `STILUMBAU_E2_PLAN` (Etappe 2: Forge-Kisten; Stationen per Owner-Entscheid aus dem
Scope entfernt), Tasks 0–5
Vorher-Stand: alte prozedurale Forge-Container-Builder, gesichert unter
`C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e2-20260807-1648`,
Baseline `TestResults-stil-e2-baseline.xml` (779 Tests, 707 bestanden, 72 Fehlschläge)

---

## Kurzfassung

Die fünf Bildkriterien der Spec sind an den acht Forge-Familien erfüllt. Der ursprüngliche
Namensdiff gegen die Baseline (`TestResults-stil-e2-final.xml`) war **nicht** bytegleich:
zusätzlich zum erwarteten grün gewordenen `ForgeContainers_AreEightUniqueStateful3DFamilies`
gab es einen neuen, ungeplanten Fehlschlag (`ZoneStructureTests.
HomeBase_HasNoGiftedStorageOrWorkbench`) — eine Nebenwirkung aus Task 4, die dessen Revert
nicht miterfasst hatte (siehe Abschnitt 2 für die vollständige Historie und den Fix-Nachweis).
**Update:** Die Regression wurde inzwischen behoben — `HomeBase.unity` wurde byte-identisch
aus dem Vor-Etappe-2-Backup wiederhergestellt und mit einer zweiten vollen Suite
(`TestResults-stil-e2-final2.xml`) bestätigt: Namensdiff jetzt bytegleich zur Baseline bis
auf den erwarteten `<=`-Fall. Die Freigabe der Etappe selbst ist **nicht** Teil dieses
Berichts.

| # | Kriterium | Ergebnis |
| --- | --- | --- |
| 1 | Kein Nachher-Bild leer/einfarbig | erfüllt, alle 16 PNGs 161–1315 KB, mehrfarbig |
| 2 | Sockel/Planken/Bogendeckel erkennbar | erfüllt, Segmentierung an allen geprüften Familien sichtbar |
| 3 | Acht Familien unterscheidbar (Bänder-/Kammzahl, Größe, Farbwelt) | erfüllt |
| 4 | Opened zeigt Gold-Loot | erfüllt, klar sichtbar an SmallRewardChest und EliteChest |
| 5 | Farbwelt entspricht Vorher | erfüllt, mit einer offen benannten Einschränkung (OptionalChest) |
| — | Keine Regression (Namensdiff, ursprünglich `final.xml`) | **nicht erfüllt gewesen** — 1 neuer Fehlschlag (`=>`), 1 grün geworden (`<=`) |
| — | Keine Regression (Namensdiff, nach Fix, `final2.xml`) | **erfüllt** — 0 neue Fehlschläge (`=>`), 1 grün geworden (`<=`) |

---

## 1. Nachher-Captures

`Eidren.Editor.WorldChestStyleCaptureUtility.CaptureForgeNachher`, ohne `-nographics`
(Rendering erforderlich), Log `stil-e2-t5-forge.log`. Wie im amendierten Brief: nur die
Forge-Kisten wurden fotografiert — die Stationen (`StorageChest`, `Workbench`,
`DeathBag`) sind seit der Task-4-Owner-Entscheidung aus dem Etappe-2-Scope entfernt;
ihre `Vorher`-Bilder aus Task 1 (`stationen-vorher/`) dokumentieren weiterhin den
unveränderten, handgebauten Bestand (`Geometry_A14`).

16/16 PNGs erzeugt unter `TempReview\StilumbauE2\forge-nachher\`:

| Datei | Vorher (B) | Nachher (B) |
| --- | ---: | ---: |
| SupplyChest_2D_Closed.png | 320 022 | 318 022 |
| SupplyChest_2D_Opened.png | 319 739 | 318 190 |
| SmallRewardChest_2D_Closed.png | 181 422 | 161 497 |
| SmallRewardChest_2D_Opened.png | 180 376 | 161 850 |
| RecoveryContainer_2D_Closed.png | 465 215 | 441 556 |
| RecoveryContainer_2D_Opened.png | 463 784 | 442 028 |
| OptionalChest_2D_Closed.png | 834 472 | 1 314 646 |
| OptionalChest_2D_Opened.png | 833 062 | 1 314 871 |
| EliteChest_2D_Closed.png | 585 060 | 587 533 |
| EliteChest_2D_Opened.png | 583 182 | 588 068 |
| LargeRewardChest_2D_Closed.png | 291 082 | 258 513 |
| LargeRewardChest_2D_Opened.png | 287 408 | 257 934 |
| CompletionChest_2D_Closed.png | 586 690 | 594 213 |
| CompletionChest_2D_Opened.png | 583 509 | 592 888 |
| MediumRewardChest_2D_Closed.png | 232 031 | 205 137 |
| MediumRewardChest_2D_Opened.png | 229 204 | 203 944 |

Log ohne `error CS`, 16× `[StilE2] geschrieben:`-Zeilen, endet mit "Exiting batchmode
successfully now!" / "Application will terminate with return code 0". Alle Dateigrößen
liegen weit über dem Bereich eines leeren/einfarbigen Bildes (wenige hundert Byte) — mit
Ausnahme von OptionalChest ist die Nachher-Datei tendenziell etwas *kleiner* als die
Vorher-Datei (mehr flächige Einfarbigkeit durch die Fabrik-Palette, weniger
Materialrauschen als beim alten Ad-hoc-Shading); OptionalChest ist deutlich größer
(834 KB → 1 315 KB), plausibel durch die zusätzlichen sichtbaren Flächen/Kanten der
Sockel-Rahmenkonstruktion in dieser Kamera-Perspektive.

---

## 2. Namensdiff gegen Baseline (Regressionsprüfung)

Volle EditMode-Suite, `-nographics`, `TestResults-stil-e2-final.xml`,
Log `stil-e2-final.log`:

```
<test-run testcasecount="787" total="787" passed="715" failed="72" ...>
```

Kein `error CS` im Log. Namensdiff exakt wie im Brief (identisch zu E1-Task 7):

```powershell
function Get-Fehlgeschlagen($pfad) {
  ([xml](Get-Content $pfad)).SelectNodes("//test-case[@result='Failed']") | ForEach-Object { $_.fullname } | Sort-Object
}
$basis = Get-Fehlgeschlagen "TestResults-stil-e2-baseline.xml"
$neu = Get-Fehlgeschlagen "TestResults-stil-e2-final.xml"
Compare-Object $basis $neu
```

| | gesamt | bestanden | fehlgeschlagen |
| --- | ---: | ---: | ---: |
| Baseline (Task 0) | 779 | 707 | 72 |
| Endstand (Task 5) | 787 | 715 | 72 |

Gleiche Fehlschlagszahl (72), aber **nicht** dieselbe Menge. `Compare-Object` liefert
zwei Einträge:

```
InputObject                                                                           SideIndicator
-----------                                                                           -------------
Eidren.Tests.EditMode.ZoneStructureTests.HomeBase_HasNoGiftedStorageOrWorkbench       =>
Eidren.Tests.V02ContainerVisualTests.ForgeContainers_AreEightUniqueStateful3DFamilies <=
```

**`<=` (grün geworden, erwartet):** `ForgeContainers_AreEightUniqueStateful3DFamilies`
— war in der Baseline ein Fehlschlag, ist jetzt grün. Genau der im Brief als Beispiel
genannte Fall (Task 3 bestätigt das bereits separat).

**`=>` (neuer Fehlschlag, NICHT erwartet):** `ZoneStructureTests.
HomeBase_HasNoGiftedStorageOrWorkbench`. Root-Cause-Analyse:

- Der Test öffnet `HomeBase.unity` und erwartet, dass die Szene **keinen**
  `StorageContainer` und **keinen** `WorkbenchController` enthält (kein "geschenktes"
  Startinventar).
- In der Baseline (Task 0, Stand vor Etappe 2) war der Test grün (`TestResults-stil-e2-
  baseline.xml`, `result="Passed"`).
- Im Endstand schlägt er fehl: `Expected: null / But was: <StorageChest
  (Eidren.Interaction.StorageContainer)>`.
- Direkte Prüfung von `Assets/_Game/Scenes/HomeBase.unity`: enthält eine
  `PrefabInstance` mit `m_Name: StorageChest` (GUID `bed0f5490a6014e4b955be7cdc8845ba`),
  platziert bei `m_TransformParent: {fileID: 68}` — physisch in der Szene gespeichert.
- Die Backup-Kopie `C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e2-
  20260807-1648\_Game\Scenes\HomeBase.unity` (Stand vor Etappe 2) enthält **keine**
  `StorageChest`-Referenz (`grep -c "StorageChest"` → 0) und unterscheidet sich von der
  aktuellen Datei (`diff -q` bestätigt Differenz).
- Ursache: Task 4 hat `StorageContentBuilder.BuildAll()` ausgeführt (Log-Zeile
  `stil-e2-t4-storage.log`: „Eidren: storage UI, chest prefab, and HomeBase placement
  built."). Der `Geometry_A14`-Wächter hat zwar den Prefab-*Mesh*-Neubau übersprungen
  (siehe Task-4-Report), aber die HomeBase-*Platzierung* eines `StorageChest` lief
  offenbar unabhängig davon und wurde in `HomeBase.unity` gespeichert. Der anschließende
  Task-4-Revert hat laut eigenem Bericht ausschließlich die drei `.cs`-Quelldateien
  byte-identisch zum Backup zurückgesetzt (verifiziert per `diff`) — die Szenendatei
  `HomeBase.unity` war davon nicht erfasst und blieb im veränderten Zustand.

Das ist eine echte, reproduzierbare Regression — kein Flake und keine
Testreihenfolge-Artefakt (Nachweis liegt direkt in der auf Platte gespeicherten
Szenendatei, nicht nur im Testlauf). Sie liegt außerhalb des Task-5-Umfangs
(„keine Quellcode-Änderung") und wurde hier zunächst als offener Punkt gemeldet
(ursprünglich Abschnitt 11 Punkt 1 dieses Dokuments) — inzwischen behoben, siehe
Fix-Nachweis unten. Dieser Absatz bleibt als Vorfallsbericht unverändert stehen.

Die übrigen 71 Fehlschläge sind namensgleich zur Baseline (dieselben Altlasten der
Dekompilat-Rekonstruktion). Der Zuwachs von 779 auf 787 Gesamttests stammt aus den 8
neuen `ForgeKiste_IstImVertexfarbenStil`-Fällen (Task 2) — alle grün, keiner erscheint
in der Failed-Liste.

### 2a. Leck und Fix (transparenter Nachtrag)

**Vorfall:** `StorageContentBuilder.BuildAll` ruft unbedingt (ohne Abfrage, ob die
Stationen noch im Scope sind) `PlaceStorageChestInHomeBase()` auf. Diese Methode
instanziiert das `StorageChest`-Prefab in `Assets/_Game/Scenes/HomeBase.unity` und
**speichert die Szene**. Der Task-4-Builder-Lauf (Storage-Teil, vor dem Owner-Entscheid,
die Stationen aus dem Scope zu nehmen) hat das getan. Der anschließende Task-4-Revert hat
laut eigenem Bericht ausschließlich die drei `.cs`-Quelldateien byte-identisch
zurückgesetzt — die Szenendatei war davon nicht erfasst und blieb verändert. Das führte
zu genau dem oben beschriebenen `=>`-Fehlschlag in `TestResults-stil-e2-final.xml`.

**Diagnose (Szenen-Diff vor dem Fix):**

```
$ grep -c "StorageChest" "Assets/_Game/Scenes/HomeBase.unity"
1
$ grep -c "StorageChest" ".../vor-stilumbau-e2-20260807-1648/_Game/Scenes/HomeBase.unity"
0
$ diff ".../vor-stilumbau-e2-20260807-1648/_Game/Scenes/HomeBase.unity" "Assets/_Game/Scenes/HomeBase.unity"
534a535
>   - {fileID: 239873705}
1729a1731,1792
>   m_PrefabAsset: {fileID: 0}
> --- !u!1001 &239873704
> PrefabInstance:
...  (63 Zeilen, ausschließlich die eine PrefabInstance + stripped Transform des
       StorageChest sowie sein Eintrag in der m_Children-Liste von GameplayRoot)
```

Beide Diff-Hunks gehören ausschließlich zu dieser einen `StorageChest`-Instanz — keine
anderen Abweichungen gefunden. Auf dieser Grundlage wurde die Szene ohne Gefahr, andere
Änderungen zu verlieren, zurückgesetzt.

**Fix:** `Assets/_Game/Scenes/HomeBase.unity` byte-identisch aus
`C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e2-20260807-1648\_Game\Scenes\HomeBase.unity`
wiederhergestellt (`.meta`-Datei unangetastet), `diff -q` bestätigt danach Identität.

**Nachweis 1 — Einzeltest:** `-testFilter "Eidren.Tests.EditMode.ZoneStructureTests"`,
XML `TestResults-stil-e2-t4fix.xml`, Log `stil-e2-t4fix.log`:

```xml
<test-run id="2" testcasecount="16" result="Passed" total="16" passed="16" failed="0" inconclusive="0" skipped="0" asserts="0" engine-version="3.5.0.0" clr-version="4.0.30319.42000" start-time="2026-08-07 15:40:48Z" end-time="2026-08-07 15:40:48Z" duration="0,6297541">
```

16/16 `ZoneStructureTests`-Fälle grün, darunter `HomeBase_HasNoGiftedStorageOrWorkbench`.
Kein `error CS` im Log.

**Nachweis 2 — volle EditMode-Suite:** XML `TestResults-stil-e2-final2.xml`,
Log `stil-e2-final2.log`:

```xml
<test-run id="2" testcasecount="787" result="Failed(Child)" total="787" passed="716" failed="71" inconclusive="0" skipped="0" asserts="0" engine-version="3.5.0.0" clr-version="4.0.30319.42000" start-time="2026-08-07 15:41:17Z" end-time="2026-08-07 15:41:31Z" duration="13,5600385">
```

Namensdiff (`Compare-Object`-Äquivalent, Fehlschlags-Fullnames Baseline vs. final2):

```
Baseline (Task 0) fehlgeschlagen: 72
final2 fehlgeschlagen: 71

=== NEU (=>, unerwartet) ===
(keine Einträge)

=== behoben (<=, erwartet) ===
<= Eidren.Tests.V02ContainerVisualTests.ForgeContainers_AreEightUniqueStateful3DFamilies
```

**Keine `=>`-Einträge mehr.** Der Namensdiff ist jetzt — bis auf den erwarteten,
bereits in Task 3 bestätigten `<=`-Fall — bytegleich zur Baseline. Die Regression aus
diesem Abschnitt ist geschlossen.

---

## 3. Dreieckszahlen (8 Forge-Familien)

Aus den `[StilE2]`-Log-Zeilen des Builder-Laufs (Task 3):

| Prefab | Dreiecke | Grenze (Spec) |
| --- | ---: | --- |
| SupplyChest | 158 | ≤ 500 ✓ |
| SmallRewardChest | 158 | ≤ 500 ✓ |
| OptionalChest | 182 | ≤ 500 ✓ |
| MediumRewardChest | 182 | ≤ 500 ✓ |
| EliteChest | 206 | ≤ 500 ✓ |
| LargeRewardChest | 218 | ≤ 500 ✓ |
| RecoveryContainer | 218 | ≤ 500 ✓ |
| CompletionChest | 230 | ≤ 500 ✓ |

Alle acht liegen im spezifizierten Korridor (158–230 Dreiecke, deutlich unter der
500er-Grenze).

Hinweis zu „allen 11 Objekten" aus dem ursprünglichen Brief-Wortlaut: die drei
Stationen (`StorageChest`, `Workbench`, `DeathBag`) sind per Task-4-Owner-Entscheid aus
dem Etappe-2-Scope entfernt und **nicht** im Vertexfarben-Stil neu gebaut — ihre
`Geometry_A14`-Geometrie (05.08.2026, Rekonstruktion) bleibt unverändert erhalten und
hat daher keine „neue" Dreieckszahl zu berichten. Es gibt somit 8 statt 11
Dreieckszahlen zu dokumentieren.

---

## 4. Kriterium 1 — Kein Nachher-Bild leer oder einfarbig

Alle 16 PNGs liegen zwischen 161 KB und 1 315 KB (Abschnitt 1) — ein leeres oder
einfarbiges Bild läge im Bereich weniger hundert Bytes. Visuell geprüft (Read-Tool, acht
Bildpaare direkt angesehen — alle acht Familien, teils Closed+Opened):
`SupplyChest_2D_Closed`, `RecoveryContainer_2D_Closed`, `CompletionChest_2D_Opened`,
`LargeRewardChest_2D_Opened`, `SmallRewardChest_2D_Opened`, `EliteChest_2D_Opened`,
`OptionalChest_2D_Closed`, `MediumRewardChest_2D_Closed` (jeweils Vorher+Nachher) — alle
zeigen klar erkennbare, mehrfarbige Objekte vor dem Szenenhintergrund der
`EidraForge`-Szene. **✓ erfüllt.**

---

## 5. Kriterium 2 — Sockel, Planken, Bogendeckel erkennbar

Am Beispiel `SupplyChest_2D_Closed.png` (Bildpaar Vorher/Nachher): Vorher ist die Kiste
ein einzelner rotbrauner Volumenkörper ohne innere Kanten (kaum vom orangenen
Platzhalter-Szenenhintergrund zu unterscheiden). Nachher zerfällt dieselbe Silhouette
sichtbar in:

- **Sockel/Rahmen** — dunklerer Rahmen an der Basis, farblich vom Korpus abgesetzt
- **Planken** — brauner Korpuskörper
- **Bogendeckel** — hellere, cremefarbene Deckelfläche, diagonal eingesetzt, deutlich
  vom dunkleren Rahmen abgegrenzt

Bei den „Kammkisten" (`CompletionChest`, `EliteChest`, `LargeRewardChest`,
`MediumRewardChest`, `OptionalChest`) zusätzlich der `Crest`-Kamm (mehrere spitze,
akzentfarbene Zähne) klar vom dunkleren Sockelkorpus abgesetzt — das ist die stärkste
Segmentierungslinie an diesen Familien. Bei `SmallRewardChest` (Opened) zusätzlich die
dunkle Innenraumfläche klar vom helleren Außenkorpus unterschieden. **✓ erfüllt.**

---

## 6. Kriterium 3 — Acht Familien unterscheidbar

Alle acht Familien wurden angesehen (Abschnitt 4). Zusammenfassung nach Kammzahl,
Größe und Farbwelt:

| Familie | Silhouette/Kamm | Akzentfarbe | Auffälligkeit |
| --- | --- | --- | --- |
| SupplyChest | flacher Kasten, kein Kamm | warmes Braun/Creme | kleinste, kompakteste Form |
| SmallRewardChest | kleiner Kasten, kein Kamm | Braun/Tan | dunkles Innenfutter im Opened-Zustand |
| RecoveryContainer | niedriger Kasten, 3er-Kamm | Türkis/Grün | einzige Familie mit Grünton |
| OptionalChest | Kasten mit Bandrahmen, kein durchgängiger Kamm | Grau + Orange | auf abweichender Bodentextur (separate Instanz in der Szene) |
| EliteChest | Kasten, 4er-Kamm | Orange | Gold-Glitzer im Opened-Zustand zwischen den Zähnen sichtbar |
| LargeRewardChest | Kasten, 5er-Kamm | Orange | breiteste Kammreihe |
| CompletionChest | kompakter Kasten, 4er-Kamm | Orange | dunkelster/kühlster Sockelton |
| MediumRewardChest | Kasten, 2–3er-Kamm | Orange/Braun | mittlere Kammzahl, mittlere Größe |

Die Familien sind sowohl über die Akzentfarbe (Türkis bei RecoveryContainer sticht klar
heraus, Grau+Orange bei OptionalChest ebenso) als auch über Kammzahl/-form und
Gesamtgröße auseinanderzuhalten. Übereinstimmend mit `Specs()` (Task 2: Palette
abgeleitet aus `spec.Body`/`spec.Accent` je Familie). **✓ erfüllt.**

---

## 7. Kriterium 4 — Opened zeigt Gold-Loot

Geprüft an `SmallRewardChest_2D_Opened.png` und `EliteChest_2D_Opened.png` (Nachher):

- **SmallRewardChest**: im geöffneten Innenraum ist ein deutlich gelbgoldener
  Loot-Block sichtbar (`LootFill_Full`), klar vom dunklen Innenfutter abgesetzt —
  eindeutigstes Beispiel.
- **EliteChest**: zwischen den `Crest`-Zähnen sind zwei kleine goldgelbe Flächen
  sichtbar — dieselbe `LootFill_Full`-Logik, aus dieser Kameraperspektive durch den Kamm
  teilweise verdeckt, aber erkennbar vorhanden.
- Zum Vergleich die Vorher-Bilder derselben zwei Familien: kein Goldton sichtbar (der in
  Task 1/E1 dokumentierte Vorher-Defekt — die alten Prefabs hatten keine
  `lootFull`/`lootPartial`-Knoten).

Bei `CompletionChest_2D_Opened.png` und `LargeRewardChest_2D_Opened.png` ist aus der
gewählten Kameraperspektive kein eindeutig als Gold identifizierbarer Farbfleck zu
erkennen (der Kamm verdeckt den Innenraum aus diesem Blickwinkel stärker als bei
SmallRewardChest/EliteChest) — das mindert die Aussage nicht grundsätzlich (dieselbe
Builder-Logik erzeugt `LootFill_Full` für alle acht Familien identisch, Task 2/3), wird
aber als Einschränkung offen benannt: nicht an allen acht Familien ist der Loot aus der
automatischen Kameraperspektive gleich gut sichtbar. **✓ erfüllt**, geprüft an zwei von
acht Familien wie im Brief gefordert („Forge-Kisten zeigen offen den Loot").

---

## 8. Kriterium 5 — Farbwelt entspricht Vorher

Je Familie bleibt der dominante Akzentton aus dem Vorher-Stand als Leitfarbe erhalten
(`Specs()`-Palette unverändert, Task 2 hat nur die Geometrieerzeugung umgestellt, nicht
die Farbwerte):

- SupplyChest, SmallRewardChest: Vorher Rotbraun/Braun (schwer lesbar wegen
  Platzhalter-Szenenbeleuchtung) → Nachher Braun/Creme — konsistent.
- RecoveryContainer: Vorher Türkis/Grün-Kamm → Nachher Türkis/Grün-Kamm — unverändert,
  einzige grüne Familie.
- EliteChest, LargeRewardChest, CompletionChest, MediumRewardChest: Vorher orangener
  Kamm → Nachher orangener Kamm — Grundton je Familie erhalten.
- OptionalChest: Vorher ein sehr dunkles, kaum unterscheidbares Objekt (flach schattiert,
  wirkt nahezu schwarz vor dem dunklen Szenenhintergrund) → Nachher Grau mit orangenen
  Bändern. Ein direkter Tonvergleich ist hier **nicht zuverlässig möglich**, da das
  Vorher-Bild zu dunkel ist, um den Grundton eindeutig zu bestimmen — offen benannt statt
  stillschweigend als „erfüllt" gewertet.

**✓ erfüllt** für 7 von 8 Familien mit direkt lesbarem Tonvergleich; bei OptionalChest
bleibt die Aussage mangels lesbarem Vorher-Referenzton eine Annahme (Palette-Code
unverändert, visuell nicht verifizierbar).

---

## 9. Erzeugte / geänderte Assets (gesamte Etappe 2, Tasks 0–5)

**Geändert (Editor-Werkzeuge)**
- `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs` — `CaptureStationen`,
  `CaptureForgeSzene` (+ zugehörige Vorher/Nachher-MenuItems) ergänzt (Task 1), inkl.
  Sonder-Kameraoffset für `RecoveryContainer_2D` (Fix-Runde 1)
- `Assets/_Game/Editor/ForgeContainerVisualBuilder.cs` — `BuildPrefab` vollständig auf
  `EidrenMeshFactory`-Primitive umgestellt, `Persist(Mesh)` neu, alte
  `AddCrest`/`TaperedBox`/`Wedge`/`MeshFrom`/`MaterialFor` entfernt (Task 2)
- `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` — `ForgeKiste_
  IstImVertexfarbenStil` mit 8 Testfällen ergänzt (Task 2)

**Prefabs (rekonstruiert im neuen Stil, Task 3)**
- `Assets/_Game/Prefabs/Containers/Forge/SupplyChest_2D.prefab` (158 Dreiecke)
- `Assets/_Game/Prefabs/Containers/Forge/SmallRewardChest_2D.prefab` (158 Dreiecke)
- `Assets/_Game/Prefabs/Containers/Forge/OptionalChest_2D.prefab` (182 Dreiecke)
- `Assets/_Game/Prefabs/Containers/Forge/MediumRewardChest_2D.prefab` (182 Dreiecke)
- `Assets/_Game/Prefabs/Containers/Forge/EliteChest_2D.prefab` (206 Dreiecke)
- `Assets/_Game/Prefabs/Containers/Forge/LargeRewardChest_2D.prefab` (218 Dreiecke)
- `Assets/_Game/Prefabs/Containers/Forge/RecoveryContainer_2D.prefab` (218 Dreiecke)
- `Assets/_Game/Prefabs/Containers/Forge/CompletionChest_2D.prefab` (230 Dreiecke)

**Szene**
- `Assets/_Game/Scenes/EidraForge.unity` — neu gebackt (Task 3), enthält jetzt die acht
  neuen Prefab-Instanzen im Vertexfarben-Stil

**Nicht verändert (bewusst, Owner-Entscheid Task 4)**
- `Assets/_Game/Prefabs/Stations/StorageChest.prefab`,
  `Assets/_Game/Prefabs/Stations/Workbench.prefab`,
  `Assets/_Game/Resources/Prefabs/DeathBag.prefab` — `Geometry_A14`-Geometrie
  (05.08.2026-Rekonstruktion) bleibt vollständig erhalten, kein Neubau
- `Assets/_Game/Editor/EidrenWorldStyleAssets.cs`,
  `Assets/_Game/Editor/StorageContentBuilder.cs`,
  `Assets/_Game/Editor/CraftingContentBuilder.cs` — nach Task-4-Revert byte-identisch
  zum Backup vom 07.08.2026 16:48 (per `diff -q` verifiziert)

**Unbeabsichtigt verändert und nicht zurückgesetzt (siehe Abschnitt 2/10)**
- `Assets/_Game/Scenes/HomeBase.unity` — enthält seit dem Task-4-Builder-Lauf eine
  `StorageChest`-Prefab-Instanz, die vor Etappe 2 dort nicht vorhanden war; der
  Task-4-Revert hat nur die drei `.cs`-Dateien zurückgesetzt, nicht diese Szene

**Bildmaterial**
- `TempReview/StilumbauE2/stationen-vorher/` — 3 PNGs (Task 1, dokumentiert den
  unveränderten Stationsbestand, kein Nachher-Gegenstück mehr geplant)
- `TempReview/StilumbauE2/forge-vorher/` — 16 PNGs (Task 1, inkl. Fix-Runde 1 für
  RecoveryContainer)
- `TempReview/StilumbauE2/forge-nachher/` — 16 PNGs (Task 5)

**Testergebnisse (Projektwurzel)**
- `TestResults-stil-e2-baseline.xml` / `stil-e2-baseline.log` (Task 0)
- `TestResults-stil-e2-t2a.xml` … `TestResults-stil-e2-t4r.xml` (Tasks 2–4,
  RED/GREEN-Nachweise je Task)
- `TestResults-stil-e2-final.xml` / `stil-e2-final.log` (Task 5, Endstand)
- `stil-e2-t5-forge.log` (Task 5, Nachher-Capture-Lauf)

**Sicherung**
- `C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e2-20260807-1648`
  (Quellstand vor der Etappe, Task 0)

---

### 9a. Nachtrag — finale Fix-Welle nach Abnahme (transparent dokumentiert)

Im Rahmen der abschließenden Whole-Branch-Review von „Stilumbau Etappe 2" wurden drei
weitere, bis dahin nicht dokumentierte Nebeneffekte des Task-4-Laufs gefunden und wie
folgt behandelt:

**Wiederhergestellt**
- `Assets/_Game/Resources/UI/StorageWindow.prefab` — von
  `StorageContentBuilder.BuildAll` (Task 4) inhaltsgleich neu geschrieben (vollständige
  fileID-Rotation; keine externen fileID-Referenzen betroffen, daher unauffällig in allen
  bisherigen Tests). In der finalen Fix-Welle byte-identisch aus
  `C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e2-20260807-1648\_Game\Resources\UI\StorageWindow.prefab`
  wiederhergestellt, per SHA-256-Hashvergleich verifiziert (`.meta`-Datei unangetastet
  gelassen).

**Bekannt, harmlos — Aufräumen auf Etappe 5 verschoben**
- 16 alte Forge-Materialien unter `Assets/_Game/Art/Containers/Forge/Materials/`
  (die `*_Body.mat`- und `*_Accent.mat`-Dateien aller acht Familien): das Legacy-Feld
  `_Color` wurde vom Task-4-era-Lauf auf den Wert von `_BaseColor` synchronisiert;
  `_BaseColor` selbst blieb unverändert. Die zugehörigen `*_Interior.mat` (Stand
  05.08.2026) sind davon nicht betroffen. Harmlos, da der aktive Shader nur `_BaseColor`
  liest — Bereinigung (Vereinheitlichung/Entfernen des Legacy-Felds) verschoben auf
  Etappe 5.
- 20 verwaiste Mesh-Assets unter `Assets/_Game/Art/Containers/Forge/Meshes/` (alte
  `Forge_0XX_TaperedBox`/`Forge_0XX_Wedge`-Dateien, Stand 05.08.2026, an den Indizes
  003, 013, 015, 023, 024, 029, 035–037, 044–045, 052–053, 063–065, 074–077): an diesen
  Indizes schreibt der neue Builder (`ForgeContainerVisualBuilder`, Task 2/3) andere
  Teilnamen; `Persist(Mesh)` überschreibt ein Asset nur bei Namensgleichheit, legt bei
  abweichendem Namen ein zusätzliches neues Asset an, statt das alte zu ersetzen — die
  alten 20 Dateien werden dadurch nicht mehr referenziert, bleiben aber auf der Platte.
  Ohne Laufzeit- oder Testauswirkung — Bereinigung ebenfalls verschoben auf Etappe 5.
  Die zugrunde liegende Namenslogik von `Persist` ist als offener Punkt für die
  Etappe-3-Planung vorgemerkt.

---

## 10. Bekannte Abweichung — Crest-Zähne

Wie im Task-5-Brief-Selbstreview vermerkt: die Forge-Kämme (`Crest_N`) sind jetzt
getaperte Pyramidenzähne statt der alten Mittelfirst-Keile — die Fabrik-`Wedge`-Grund-
form hat die Firstkante hinten, nicht mittig; ein Sonderprimitiv dafür wäre YAGNI
gewesen (bewusste Entscheidung aus Task 2). In den Bildern (Abschnitt 4–8) sichtbar,
z. B. an `LargeRewardChest`/`EliteChest`/`CompletionChest`: spitz zulaufende, nach oben
schmaler werdende Zähne statt eines First-Kamms. Ändert nichts an der Erfüllung der
fünf Abnahmekriterien, wird hier zur Vollständigkeit dokumentiert.

---

## 11. Offene Punkte

1. **Regression `HomeBase_HasNoGiftedStorageOrWorkbench`** (Abschnitt 2/2a) —
   **behoben.** Ursache war ein `StorageChest`, das in `HomeBase.unity` platziert und
   dort gespeichert war (Task-4-Builder-Lauf; der Revert hatte nur die drei
   `.cs`-Dateien zurückgesetzt, nicht die Szene). Fix: `HomeBase.unity` byte-identisch
   aus `vor-stilumbau-e2-20260807-1648\_Game\Scenes\HomeBase.unity` wiederhergestellt
   (Szenen-Diff vorher bestätigte, dass die einzige Abweichung die eine
   `StorageChest`-Instanz war). Nachweis: `TestResults-stil-e2-t4fix.xml` (16/16
   `ZoneStructureTests` grün) und `TestResults-stil-e2-final2.xml` (Namensdiff gegen
   Baseline jetzt ohne `=>`-Einträge). Kein offener Punkt mehr.
2. **Stationen aus Scope entfernt** (Task 4): `StorageChest`, `Workbench`, `DeathBag`
   bleiben im alten `Geometry_A14`-Rekonstruktionsstand. Ihre Vorher-Bilder
   (`stationen-vorher/`) sind der finale dokumentierte Zustand für Etappe 2 — es gibt
   kein Nachher-Gegenstück. Eine etwaige spätere Umstellung dieser drei Objekte müsste
   entweder das bestehende `Geometry_A14`-Geflecht bewusst ersetzen oder als eigener,
   gesondert zu entscheidender Task geplant werden.
3. **OptionalChest-Farbvergleich** (Abschnitt 8): das Vorher-Bild ist zu dunkel
   geschattet, um den Grundton zuverlässig mit dem Nachher-Bild zu vergleichen; die
   Aussage „Farbwelt erhalten" stützt sich hier auf den unveränderten Palette-Code, nicht
   auf einen visuellen Vergleich.
4. **Loot-Sichtbarkeit aus Kameraperspektive** (Abschnitt 7): bei `CompletionChest` und
   `LargeRewardChest` verdeckt der Kamm den Innenraum aus dem automatischen
   Capture-Winkel stärker als bei den anderen Familien — funktional identisch
   (`LootFill_Full` wird bei allen acht Familien gleich gesetzt), aber visuell nicht an
   allen acht Familien gleich gut demonstrierbar.
5. **Freigabe durch den Auftraggeber steht aus.** Dieser Bericht weist die Kriterien und
   (nach dem Fix in Abschnitt 2a) die vollständige Regressionsfreiheit gegen die
   Baseline nach, spricht aber **keine Freigabe der Etappe aus**. Vor Abschluss sind dem
   Auftraggeber die 16 Bildpaare aus `TempReview\StilumbauE2\forge-vorher\` und
   `TempReview\StilumbauE2\forge-nachher\` zur Entscheidung vorzulegen.

---

## 12. Nachvollziehen

Nachher-Captures neu erzeugen:

    & ".unity-editor\Editor\Unity.exe" -batchmode -projectPath "<Projektpfad>" `
      -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureForgeNachher -quit `
      -logFile "stil-e2-t5-forge.log"

Volle EditMode-Suite:

    & ".unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "<Projektpfad>" `
      -runTests -testPlatform EditMode -testResults "TestResults-stil-e2-final.xml" `
      -logFile "stil-e2-final.log"

Namensdiff (siehe Abschnitt 2 für die vollständige Funktion).
