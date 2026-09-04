# Stilumbau Etappe 1 Abnahme — Fundament und Weltkisten

Stand: 07.08.2026
Bezug: `STILUMBAU_WELTINVENTAR_ENTWURF.md` (Etappe 1: Weltkisten), `STILUMBAU_E1_PLAN.md`, Tasks 0–7
Vorher-Stand: alte prozedurale Kisten-Builder, gesichert unter
`C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e1-20260807-1458`,
Baseline `TestResults-stil-e1-baseline.xml` (765 Tests, 72 Fehlschläge)

---

## Kurzfassung

Alle fünf Abnahmekriterien der Spec sind aus den Bildpaaren und dem
Namensdiff heraus erfüllt. Der Namensdiff ist bytegleich: dieselben 72
Fehlschläge vor und nach dem Umbau, keine neuen, keine verschwundenen. Die
Freigabe der Etappe selbst ist **nicht** Teil dieses Berichts — sie obliegt
dem Auftraggeber nach Sichtung der Bildpaare (Abschnitt 6).

| # | Kriterium | Ergebnis |
| --- | --- | --- |
| 1 | Kein Nachher-Bild leer/einfarbig | erfüllt, alle 12 PNGs 31–47 KB, mehrfarbig |
| 2 | Planken/Sockel/Bogendeckel erkennbar | erfüllt, Segmentierung an allen drei Familien sichtbar |
| 3 | Familien unterscheidbar (Holz/Eisen+Bänder+Kantenpanzer/Bewuchs) | erfüllt |
| 4 | Vier Zustände je Familie unterscheidbar (voll/flach/kein Loot) | erfüllt, am Beispiel Common geprüft |
| 5 | Farbwelt entspricht Vorher | erfüllt, Grundtöne je Familie erhalten |
| — | Keine Regression (Namensdiff) | erfüllt, 72/72 identisch, `Compare-Object` leer |

---

## 1. Nachher-Captures

`Eidren.Editor.WorldChestStyleCaptureUtility.CaptureNachher`, ohne
`-nographics` (Rendering erforderlich), Log `stil-e1-t7-nachher.log`.

12/12 PNGs erzeugt unter `TempReview\StilumbauE1\nachher\`:

| Datei | Vorher (B) | Nachher (B) |
| --- | ---: | ---: |
| WorldChest_Common_Closed.png | 49 257 | 36 864 |
| WorldChest_Common_Opened.png | 43 310 | 39 697 |
| WorldChest_Common_PartiallyEmptied.png | 43 310 | 37 603 |
| WorldChest_Common_Emptied.png | 47 457 | 36 878 |
| WorldChest_Guarded_Closed.png | 57 146 | 43 817 |
| WorldChest_Guarded_Opened.png | 54 716 | 46 646 |
| WorldChest_Guarded_PartiallyEmptied.png | 54 716 | 45 336 |
| WorldChest_Guarded_Emptied.png | 57 400 | 44 273 |
| WorldChest_Hidden_Closed.png | 39 935 | 31 408 |
| WorldChest_Hidden_Opened.png | 36 349 | 33 205 |
| WorldChest_Hidden_PartiallyEmptied.png | 36 349 | 31 792 |
| WorldChest_Hidden_Emptied.png | 39 082 | 31 483 |

Auffällig, positiv: Im Vorher-Stand waren `Opened` und `PartiallyEmptied`
je Familie **bytegleich** (Task 5, dokumentierter Defekt — die alten Prefabs
hatten keine `lootFull`/`lootPartial`-Knoten). Im Nachher-Stand unterscheiden
sich beide Dateigrößen durchgehend (z. B. Common: 39 697 vs. 37 603 B) — der
Loot-Füllstand ist jetzt sichtbar unterschiedlich, siehe Abschnitt 5.

Log ohne `error CS`, 12× `[StilE1] geschrieben:`-Zeilen, endet mit
"Exiting batchmode successfully now!" / "Application will terminate with
return code 0".

---

## 2. Namensdiff gegen Baseline (keine Regression)

Volle EditMode-Suite, `-nographics`, `TestResults-stil-e1-final.xml`,
Log `stil-e1-final.log`:

```
<test-run testcasecount="779" total="779" passed="707" failed="72" ...>
```

Kein `error CS` im Log. Namensdiff exakt wie im Brief:

```powershell
function Get-Fehlgeschlagen($pfad) {
  ([xml](Get-Content $pfad)).SelectNodes("//test-case[@result='Failed']") | ForEach-Object { $_.fullname } | Sort-Object
}
$basis = Get-Fehlgeschlagen "TestResults-stil-e1-baseline.xml"
$neu = Get-Fehlgeschlagen "TestResults-stil-e1-final.xml"
Compare-Object $basis $neu
```

| | gesamt | bestanden | fehlgeschlagen |
| --- | ---: | ---: | ---: |
| Baseline (Task 0) | 765 | 693 | 72 |
| Endstand (Task 7) | 779 | 707 | 72 |

`Compare-Object` liefert **keine Einträge** — weder `=>` (neue Fehlschläge)
noch `<=` (verschwundene Fehlschläge). Die 72 Fehlschlags-Namen sind zwischen
Baseline und Endstand mengengleich (verifiziert per direktem Namensvergleich,
nicht nur Zählerdifferenz). Der Zuwachs von 765 auf 779 Gesamttests stammt aus
den in Task 1, 2, 3, 4 und 6 neu hinzugekommenen Testfällen (`EidrenMeshFactoryTests`,
`EidrenWorldStyleTests`, `WorldChestVisualStateTests`, `Weltkiste_IstImVertexfarbenStil`),
die alle grün sind — keiner ihrer Namen erscheint in der Failed-Liste.

Die 72 Fehlschläge sind dieselben Altlasten der Dekompilat-Rekonstruktion wie
in der Baseline (Task 0) und in Task 6 (dort 50 nach Namensfilter — Task 6 lief
mit einem älteren Prefab-/Testzähler vor Abschluss aller Tasks; Task 7 ist der
volle, abschließende Lauf nach Task 6).

---

## 3. Dreieckszahlen

Aus den `[StilE1]`-Log-Zeilen des Builder-Laufs (Task 6):

| Prefab | Dreiecke | Grenze (Spec) |
| --- | ---: | --- |
| WorldChest_Common | 206 | ≤ 400 ✓ |
| WorldChest_Guarded | 266 | ≤ 400 ✓ |
| WorldChest_Hidden | 246 | ≤ 400 ✓ |

---

## 4. Kriterium 1 — Kein Nachher-Bild leer oder einfarbig

Alle 12 PNGs liegen zwischen 31 KB und 47 KB (vgl. Tabelle Abschnitt 1) — ein
leeres oder einfarbiges Bild läge im Bereich weniger hundert Bytes. Visuell
geprüft (Read-Tool, sechs Bilder direkt angesehen, s. Abschnitt 5–6):
`WorldChest_Common_Closed.png`, `WorldChest_Guarded_Closed.png`,
`WorldChest_Hidden_Closed.png`, `WorldChest_Common_Opened.png`,
`WorldChest_Common_PartiallyEmptied.png`, `WorldChest_Common_Emptied.png` —
alle zeigen klar erkennbare, mehrfarbige Objekte vor dem definierten
dunkelgrauen Hintergrund. **✓ erfüllt.**

---

## 5. Kriterium 2 — Planken, Sockel, Bogendeckel erkennbar

Am Beispiel `WorldChest_Common_Closed.png` (Bildpaar Vorher/Nachher,
Abschnitt 6): Vorher ist die Kiste ein einzelner goldfarbener Volumenkörper
ohne innere Kanten. Nachher zerfällt dieselbe Silhouette sichtbar in:

- **Sockel** — flache, hellgoldene Bodenplatte, farblich vom Korpus abgesetzt
- **Planken** — brauner Korpuskörper mit sichtbarer Ton-Variation je Fläche
- **Bogendeckel** — gerundete Deckelsilhouette (Kontur oben links/rechts),
  mit First-Leiste (Mittelsteg) und `ForgedBand`-Streben, die den Übergang
  Deckel/Korpus markieren

Bei `WorldChest_Guarded_Closed.png` zusätzlich die Kantenpanzer-Platten
(dunkelgraue Zusatzflächen auf dem Deckel) klar von den drei
`ForgedBand`-Streben (kupferfarben) unterschieden. **✓ erfüllt.**

---

## 6. Kriterium 3 — Familien unterscheidbar

Alle drei Closed-Zustände nebeneinander geprüft (Bildpaare Vorher/Nachher):

| Familie | Vorher (Einzelton) | Nachher (segmentiert) |
| --- | --- | --- |
| Common (Holz) | einheitlich Gold | brauner Korpus, goldene Bänder/Sockel |
| Guarded (Eisen + Bänder + Kantenpanzer) | einheitlich Kupfer/Orange | grauer/schwarzer Metallkorpus, kupferfarbene Bänder, zusätzliche dunkle Kantenpanzer-Platten auf dem Deckel |
| Hidden (Bewuchs) | einheitlich Dunkelgrün | grüner Korpus, braune Holzbänder, sichtbare grünliche Bodenfläche |

Die drei Familien sind sowohl vorher als auch nachher klar auseinanderzuhalten
— die Guarded-Familie zusätzlich durch die (nur bei ihr vorhandenen)
Kantenpanzer-Platten eindeutig identifizierbar. **✓ erfüllt.**

---

## 7. Kriterium 4 — Vier Zustände je Familie unterscheidbar

Geprüft an der Familie Common (alle vier Nachher-Zustände einzeln
angesehen):

- **Opened**: großer gelbgoldener Loot-Block sichtbar im geöffneten Innenraum
  (`LootFill_Full`, `lootFull.SetActive(status == Opened)`)
- **PartiallyEmptied**: kleinerer, flacherer goldener Loot-Rest sichtbar
  (`LootFill_Partial`, `lootPartial.SetActive(status == PartiallyEmptied)`) —
  deutlich kleinere Fläche als bei Opened
- **Emptied**: Innenraum ohne Goldfläche, nur die helle `EmptyLining`-Fläche
  sichtbar — kein Loot
- **Closed**: Deckel geschlossen, kein Innenraum sichtbar

Die drei offenen Zustände sind anhand der Loot-Fläche eindeutig unterscheidbar
(voll → flach → leer), bestätigt durch die unterschiedlichen Dateigrößen
(Abschnitt 1: Opened 39 697 B, PartiallyEmptied 37 603 B, Emptied 36 878 B —
strikt fallend mit sinkendem Loot-Anteil). Das behebt den in Task 5
dokumentierten Vorher-Defekt (Opened/PartiallyEmptied bytegleich, da die alten
Prefabs keine Loot-Knoten hatten). **✓ erfüllt**, geprüft an einer Familie wie
im Brief gefordert.

---

## 8. Kriterium 5 — Farbwelt entspricht Vorher

Je Familie bleibt der dominante Grundton aus dem Vorher-Stand als Leitfarbe im
Nachher-Material erhalten (`ChestPalette` leitet `Plank`/`Metal`/`Interior`/
`Lining`/`Gold` deterministisch aus `Body`/`Accent` ab, Task 6):

- Common: Vorher durchgehend Gold → Nachher Gold als Sockel-/Bandfarbe, Braun
  als abgeleiteter Plankenton — keine Neuabstimmung, dieselbe Body-Farbe
  zerlegt in mehrere Flächen.
- Guarded: Vorher durchgehend Kupfer/Orange → Nachher Kupfer als Bandfarbe
  erhalten, Metallkorpus dunkler (Accent-Ableitung).
- Hidden: Vorher durchgehend Dunkelgrün → Nachher Grün als Korpus-/Bodenton
  erhalten, Braun als Holzband-Ableitung.

Die Umstellung von Einzelton auf mehrere abgeleitete Flächentöne ist der
beabsichtigte Effekt der Etappe (Segmentierung, Kriterium 2) — die
Grundtöne selbst wurden nicht neu abgestimmt, sondern aus `Body`/`Accent`
der jeweiligen Familie hergeleitet. **✓ erfüllt**, mit der Einschränkung, dass
„gleicher Grundton" hier als „gleiche Body/Accent-Familie, jetzt auf mehrere
Flächen verteilt" ausgelegt wird — diese Auslegung wird hier offen benannt,
da vorher ein einziger Flächenton vorlag und ein direkter Pixel-Farbvergleich
deshalb nicht sinnvoll ist.

---

## 9. Erzeugte / geänderte Assets (gesamte Etappe, Tasks 1–7)

**Neu (Editor-Werkzeuge und Tests)**
- `Assets/_Game/Editor/EidrenMeshFactory.cs` — `TaperedBox`, `Wedge`, `Loft`,
  `LoftShape`/`LoftProfile` (geschachtelt), Ton-Variation, Kontakt-AO (Task 1/2)
- `Assets/_Game/Editor/Tests/EidrenMeshFactoryTests.cs` — 8 Tests (Task 1/2)
- `Assets/_Game/Editor/EidrenWorldStyleAssets.cs` — Materialhelfer (Task 3)
- `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` — 2 Tests (Task 3),
  +3 TestCases `Weltkiste_IstImVertexfarbenStil` (Task 6)
- `Assets/_Game/Editor/Tests/WorldChestVisualStateTests.cs` — 1 Test (Task 4)
- `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs` — Capture-Werkzeug
  `CaptureVorher`/`CaptureNachher` (Task 5)

**Neu (Shader/Material)**
- `Assets/_Game/Shaders/World/EidrenWorldVertexLit.shader` (`Eidren/World/VertexLit`,
  Ableger von `WandererVertexLit`, Task 3)
- `Assets/_Game/Art/World/Materials/M_EidrenWorld_VertexLit.mat` (Task 3)

**Geändert (Laufzeit/Builder)**
- `Assets/_Game/Scripts/Presentation/WorldChestVisual.cs` — `lootFull`/
  `lootPartial`-Felder, erweiterte `Configure`-Überladung, Sichtbarkeitslogik
  (Task 4)
- `Assets/_Game/Editor/WorldChestContentBuilder.cs` — vollständig auf
  Vertexfarben-Stil umgestellt, `ChestPalette`, `Persist(Mesh)`, LootFill-Meshes
  (Task 6)

**Prefabs (rekonstruiert im neuen Stil, Task 6)**
- `WorldChest_Common` (206 Dreiecke), `WorldChest_Guarded` (266 Dreiecke),
  `WorldChest_Hidden` (246 Dreiecke) — Bestandsverträge (Collider, Teilnamen,
  `ConfigureVisual`) unverändert

**Bildmaterial**
- `TempReview/StilumbauE1/vorher/` — 12 PNGs (Task 5)
- `TempReview/StilumbauE1/nachher/` — 12 PNGs (Task 7)

**Testergebnisse (Projektwurzel)**
- `TestResults-stil-e1-baseline.xml` / `stil-e1-baseline.log` (Task 0)
- `TestResults-stil-e1-t1.xml` … `TestResults-stil-e1-t6-full.xml` (Tasks 1–6,
  RED/GREEN-Nachweise je Task)
- `TestResults-stil-e1-final.xml` / `stil-e1-final.log` (Task 7, Endstand)
- `stil-e1-t7-nachher.log` (Task 7, Nachher-Capture-Lauf)

**Sicherung**
- `C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e1-20260807-1458`
  (Quellstand vor der Etappe, Task 0)

---

## 10. Offener Punkt — Freigabe durch den Auftraggeber

Dieser Bericht weist die fünf Abnahmekriterien und die Regressionsfreiheit
nach, spricht aber **keine Freigabe der Etappe aus**. Vor Abschluss sind dem
Auftraggeber die 12 Bildpaare aus `TempReview\StilumbauE1\vorher\` und
`TempReview\StilumbauE1\nachher\` zu zeigen und seine Rückmeldung einzuholen
— insbesondere zur in Abschnitt 8 offen benannten Auslegung von „Farbwelt
entspricht Vorher" (Einzelton → mehrere abgeleitete Flächentöne).

Etappen 2–5 (Forge-Kisten/Stationen, Ressourcenknoten, Props, Gebäude) folgen
als eigene Pläne nach Abnahme dieses Piloten.

---

## 11. Nachvollziehen

Nachher-Captures neu erzeugen:

    & ".unity-editor\Editor\Unity.exe" -batchmode -projectPath "<Projektpfad>" `
      -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureNachher -quit `
      -logFile "stil-e1-t7-nachher.log"

Volle EditMode-Suite:

    & ".unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "<Projektpfad>" `
      -runTests -testPlatform EditMode -testResults "TestResults-stil-e1-final.xml" `
      -logFile "stil-e1-final.log"

Namensdiff (siehe Abschnitt 2 für die vollständige Funktion).
