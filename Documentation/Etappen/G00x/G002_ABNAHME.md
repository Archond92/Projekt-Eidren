# G-002 Abnahme — Bodenmaterial und Geländedarstellung

Stand: 06.08.2026
Bezug: `GRAFIKAUFTRAEGE_V0.2.1.md`, Auftrag G-002; Entwurf: `G002_ENTWURF.md`

Alle Zahlen dieses Berichts stammen aus dem Buildstand `0.2.0-dev` vom
06.08.2026 und sind mit den unten genannten Werkzeugen reproduzierbar.

---

## Kurzfassung

Acht von neun Kriterien sind erfüllt. Kriterium 1 ist gegen den **Auftragswert**
erfüllt (8,179 gegen geforderte 7,74) und gegen den **realen Ausgangswert
verfehlt** (1,49-fach statt 3-fach). Der zweite Maßstab stammt aus dem eigenen
Entwurf, nicht aus dem Auftrag; er wird in Abschnitt 1 mit Zahl benannt und
nicht umdefiniert.

| # | Kriterium | Ergebnis |
| --- | --- | --- |
| 1 | Nachbardelta mindestens dreifach | Auftragswert erfüllt, realer Maßstab verfehlt — siehe unten |
| 2 | Struktur ohne sichtbares Kachelraster | erfüllt |
| 3 | Drei mischbare Materialien mit Übergängen | erfüllt, alle 8 Gebiete |
| 4 | Gras und Erde mit erkennbarer Materialkante | erfüllt |
| 5 | Bewuchs und Streu gegen leere Flächen | erfüllt, 35–215 Instanzen je Gebiet |
| 6 | Gemalter Stil bleibt gewahrt | erfüllt |
| 7 | Jede Zone mit eigener Bodenwirkung | erfüllt, kleinster Abstand 20,8 |
| 8 | Bildrate fällt nicht messbar ab | erfüllt, +0,014 ms |
| 9 | Vorher/Nachher aus identischer Position | erfüllt |

---

## 1. Nachbardelta — der eine offene Punkt

Gemessen am ganzen Bild von `04_boden_leer.png` (1920×1080), Werkzeug
`Tools\Measure-Ground.ps1 -Bereich Voll`.

| Stand | Mittel | StdAbw | Nachbardelta |
| --- | ---: | ---: | ---: |
| Referenz (vor G-002) | 50,738 | 8,043 | **5,486** |
| nur Körnung | 61,968 | 13,811 | 7,444 |
| Körnung + Streu S1 | 61,975 | 14,203 | 7,733 |
| **Endstand (Streu S3)** | 61,978 | 14,810 | **8,179** |

**Gegen den Auftragswert:** Der Auftrag nennt 2,58 als Ausgangswert und fordert
das Dreifache, also 7,74. Erreicht sind **8,179** — der Wert ist mit 5,7 Prozent
Reserve erfüllt.

**Gegen den realen Ausgangswert:** Die 2,58 stammen aus den alten
`VisualFixSmokeFinal`-Captures, also von vor der Shader-Reparatur in G-001. Real
gemessen liegt der Ausgangswert bei **5,486**. Das Dreifache davon wären
**16,458**. Erreicht sind 8,179, das ist das **1,49-fache** statt des
3-fachen. Dieser Maßstab ist verfehlt.

Er ist nicht erreichbar, ohne Kriterium 6 zu brechen. Aus den Messpunkten der
Streureihe folgt eine Steigung von rund 42 Beitrag je Einheit Nachbarabstand der
Körnungslage. Für 16,458 bräuchte die Lage einen Nachbarabstand von etwa 0,29 —
also im Mittel rund 30 Prozent Helligkeitssprung zwischen je zwei benachbarten
Bildpunkten. Das ist kein Boden mehr, das ist Bildrauschen. Drei Kandidaten
dieser Größenordnung wurden erzeugt und verworfen, siehe Abschnitt 10.

Kriterium 6 wurde hier als bindend behandelt und Kriterium 1 nicht weiter
getrieben. Das ist eine Entscheidung, keine Messung — sie steht hier, damit sie
überprüfbar ist.

### 1.1 Teilflächen

| Bereich | Referenz | Endstand | Faktor |
| --- | ---: | ---: | ---: |
| Erdfläche | 5,457 | 6,773 | 1,24 |
| Grasfläche | 5,484 | 10,722 | 1,96 |
| ganzes Bild | 5,486 | 8,179 | 1,49 |

Die Grasfläche trägt den Zuwachs, die Erdfläche bleibt zurück. Die
naheliegende Erklärung: Die Erdflächen sind Blend-Flicken, deren Deckung in
G-002 zurückgenommen wurde, sie zeigen jetzt mehr Übergang und weniger eigene
Fläche. Nachgemessen wurde diese Erklärung nicht — sie ist eine Vermutung.

### 1.2 Wie der Zielwert gefunden wurde

Nachbarabstände zweier unabhängiger Lagen setzen sich **quadratisch** zusammen,
nicht linear. Das war der Grund, warum drei frühere Abstimmrunden ohne Wirkung
blieben — sie hatten linear gerechnet.

    Beitrag der Körnung = Wurzel(7,444² − 5,486²) = 5,031
    nötiger Beitrag     = Wurzel(7,740² − 5,486²) = 5,460

Das Modell sagte für die erste Streulage 8,33 voraus; gemessen wurden 7,733. Die
Annahme, der Beitrag skaliere linear mit dem Nachbarabstand der Lage, war
falsch: 24,5 Prozent mehr Lage brachten nur 8,3 Prozent mehr Beitrag, weil die
bilineare Filterung die harte Kante zwei Texel großer Körner wieder aufweicht.
Aus den zwei Messpunkten folgt die oben genannte Steigung von rund 42; sie sagte
für die zweite Streulage 8,13 voraus, gemessen wurden 8,179 (0,6 Prozent
Abweichung).

---

## 2. Kachelraster

Die Zusatzlagen liegen bewusst auf krummen Faktoren: `_DetailScale 0,42`
(Wiederholung alle 14,3 Welteinheiten) und `_MacroScale 0,137` (alle 43,8
Einheiten) gegen eine Basiskachel von 6 Einheiten. Keiner der Faktoren ist ein
ganzzahliger Teiler, deshalb fällt keine Lage phasengleich auf die andere.

Nachweis: `TempReview\Ausschnitte\boden_final_S3.png` — Ausschnitt aus dem
Abnahmebild in Originalgröße, ein Bildpunkt hier ist ein Bildpunkt dort. Kein
wiederkehrendes Muster erkennbar.

---

## 3. Drei mischbare Materialien

Alle acht Gebiete tragen `Base` + `Blend_1` + `Blend_2`:

    EmberRuins, Greenwood, GreyRifts, HomeBase,
    Marsh, Quarry, TwilightGrove, VeilMarsh

HomeBase hatte vor G-002 nur zwei Lagen und bekam `HomeBase_Ground_Blend_2`.

Die Übergänge wirken, weil in G-002 die Vertexfarben-Ausblendung im Shader
`Eidren/Area Art Blend` wiederhergestellt wurde. Vorher zeichneten die
Blend-Flicken deckend — das war der „flaue braune Boden" aus dem Auftragstext,
ein Dekompilierschaden derselben Art wie in G-001.

---

## 4. Materialkante Gras/Erde

`TempReview\G002-Zonen\Greenwood_gesamt.png`: links Erdfläche, rechts Wiese,
dazwischen ein weicher, aber lesbarer Materialrand. Ebenso in TwilightGrove,
VeilMarsh und HomeBase.

Vorher/Nachher an identischer Position:
`G001-Captures\Referenz\04_boden_leer.png` gegen `G001-Captures\Lauf1\04_boden_leer.png`.

---

## 5. Bewuchs und Streu

Aus den Szenendateien ausgezählt (nicht aus einem Log übernommen):

| Gebiet | Instanzen | Sorten |
| --- | ---: | ---: |
| Marsh | 215 | 6 |
| Greenwood | 194 | 5 |
| VeilMarsh | 162 | 5 |
| TwilightGrove | 141 | 4 |
| Quarry | 124 | 3 |
| EmberRuins | 103 | 2 |
| GreyRifts | 102 | 2 |
| HomeBase | 35 | 3 |

Der Bewuchs sitzt auf einem gejitterten Raster, hängt nur an Gebiet und Zelle
und ist damit reproduzierbar. Collider werden entfernt: Bodendecker ist
Ausstattung, kein Hindernis — Bewegung, Kampf, Ernte und Bauraster bleiben
unberührt.

HomeBase liegt bewusst niedriger: 48 statt 80 Einheiten Kantenlänge und eine
bebaute Fläche.

---

## 6. Gemalter Stil

Die Gebietstexturen für TwilightGrove, VeilMarsh und GreyRifts wurden **nicht neu
gemalt**, sondern aus der Textur des Nachbargebiets abgeleitet: Farbgradation
plus eine gebietseigene Zusatzlage (Laubstreu, Nassschlick, Geröll). Die gemalte
Helligkeitsstruktur der Vorlage bleibt dabei erhalten.

Sichtprüfung bei 1:1 an `boden_final_S3.png` und den acht Gebietsbildern: Der
Boden liest sich als gemalte Erde mit Griesel, nicht als Rauschen. Die Figuren
stehen nicht fremd darauf.

Zur Belastbarkeit dieser Aussage: Sie ist ein Urteil, kein Messwert. Belegt ist
nur, dass die verworfenen Alternativen deutlich schlechter aussahen — die Bilder
dazu liegen in `TempReview\G002-Streu\` und `TempReview\G002-Streu2\`.

---

## 7. Eigene Bodenwirkung je Gebiet

Mittlere Bodenfarbe und Abstand zum nächstähnlichen Gebiet
(`Tools\Compare-Zones.ps1`):

| Gebiet | R | G | B | nächstes | Abstand |
| --- | ---: | ---: | ---: | --- | ---: |
| HomeBase | 197,2 | 113,0 | 56,9 | Quarry | 102,2 |
| Quarry | 106,1 | 105,3 | 102,5 | GreyRifts | 26,9 |
| GreyRifts | 84,0 | 90,6 | 106,0 | Quarry | 26,9 |
| Marsh | 36,5 | 35,3 | 15,9 | EmberRuins | 25,5 |
| EmberRuins | 41,7 | 40,0 | 40,5 | Marsh | 25,5 |
| Greenwood | 65,7 | 75,7 | 34,3 | TwilightGrove | 21,6 |
| TwilightGrove | 54,9 | 64,6 | 49,4 | VeilMarsh | 20,8 |
| VeilMarsh | 63,8 | 71,7 | 66,8 | TwilightGrove | 20,8 |

Kleinster Abstand: **20,8**. Vorher war er **0,0** — TwilightGrove, VeilMarsh und
GreyRifts benutzten die Textur eines Nachbargebiets unverändert.

Offen benannt: Quarry und GreyRifts sind mit 26,9 das nach Augenschein
ähnlichste Paar. Sie unterscheiden sich in der Temperatur (warmes gegen kaltes
Grau), nicht im Helligkeitsniveau.

---

## 8. Bildrate

Gemessen im Windows-x64-Build, 240 Frames je Konfiguration nach 60 Frames
Vorlauf, vSync und `captureFramerate` abgeschaltet:

| Konfiguration | Median | Renderer |
| --- | ---: | ---: |
| `boden_vorher` — Basisboden auf URP/Unlit, ohne Bewuchs | 1,462 ms | 8 |
| `boden_nachher` — Basisboden auf Eidren/Ground Detail | 1,471 ms | 8 |
| `boden_bewuchs` — dazu der Bodenbewuchs | 1,476 ms | 25 |

Der Ground-Detail-Shader kostet **+0,009 ms**, der Bewuchs mit 17 zusätzlichen
Renderern weitere **+0,005 ms**. Zusammen **+0,014 ms** auf einen Frame von rund
1,5 ms, gegen ein Budget von 16,7 ms bei 60 Bildern je Sekunde.

Die absoluten Werte schwanken zwischen Läufen mit der Maschinenlast; belastbar
ist nur der Vergleich innerhalb eines Laufs, und dieser stammt aus demselben
Build wie die Messung in Abschnitt 1.

---

## 9. Vorher/Nachher aus identischer Position

`Tools\G001-capture.ps1` fährt fünf feste Kamerapositionen an und läuft jeden
Stand zweimal. Beide Läufe des Endstands sind an allen fünf Positionen
deckungsgleich: die beiden Bodenbilder bytegleich, die drei Bilder mit Figuren
mit Pixeldifferenzen von 0,001, 0,001 und 0,011. Der Schwellwert liegt bei 1,0.

Erhaltene Stände in `G001-Captures\`:

| Verzeichnis | Bedeutung |
| --- | --- |
| `Referenz` | Zustand vor G-002 |
| `Zwischenstand-Vertexfix` | nach der Shader-Reparatur, vor den Texturen |
| `Stand-G002-Bild` | nach den Gebietstexturen |
| `Runde3-Mipmaps` | nach der Abstimmung von `_DetailScale` |
| `Stand-Koernung-ohne-Streu` | Körnung allein (7,444) |
| `Stand-Streu-S1` | erste Streulage (7,733) |
| `Lauf1`, `Lauf2` | Endstand (8,179) |

---

## 10. Verworfene Wege

Vollständig, damit niemand sie ein zweites Mal geht:

**Detaillage allein, ohne Reparatur der Blend-Deckung.** Offline auf das echte
Capture gerechnet: 5,486 → 7,046. Las sich als Schleifpapier. Das Problem war
nie die fehlende Textur, sondern der deckend gezeichnete Erdflicken.

**Detaillage in der Verkleinerung** (`_DetailScale 1,05`). Vorhergesagt waren 2,6
zusätzliches Nachbardelta, gemessen 0,73. Die Vorhersage hatte mit bikubischer
Verkleinerung gerechnet, die GPU nimmt Mipmaps, und die Kameraneigung von 52
Grad verstärkt die Verkleinerung entlang der Tiefenachse. Ab `_DetailScale 0,42`
liegt die Lage in der Vergrößerung und die Fehlerquelle entfällt.

**Zwei Oktaven statt vier.** Erreichte 0,054 statt 0,041 Nachbarabstand, sah aber
wie Bildstörung aus.

**Importvorgaben als Ursache** (Mipmaps, sRGB, Kompression). Hypothese: die GPU
mittelt die Körnung weg. Gemessen: 6,012 → 5,996, also wirkungslos. Ursache: Das
Projekt läuft im **Gamma-Farbraum** — `m_ColorSpace` fehlt in
`ProjectSettings.asset` —, deshalb ändert das sRGB-Flag am Abtasten nichts. Die
Vorgaben in `GroundMapImportSettings.cs` sind für Datenmasken trotzdem richtig
und bleiben.

Am Endstand nachgeprüft: `enableMipMap: 0`, `sRGBTexture: 0`,
`textureCompression: 0`, `filterMode: 1`, `wrapU/V/W: 0` auf beiden Zusatzlagen.
Die Vorgaben haben zwei Neuerzeugungen der Texturen überstanden — dafür wurde
ein `AssetPostprocessor` gewählt statt einer einmaligen Umstellung: Da
`Tools\G002-Textures.ps1` die PNGs überschreibt, hätte Unity bei jedem Import
wieder seine Standardvorgaben daraufgelegt.

**Gleichverteilte Streukörner** (`Tools\Try-Streu.ps1`). Drei von vier Kandidaten
erreichten den damals angenommenen Zielwert, alle vier sahen bei 1:1 wie
Rauschen aus. 12 bis 28 Prozent Kornfläche.

**Geklumpte Streukörner** (`Tools\Try-Streu2.ps1`). Eine tieffrequente Maske
sollte die Körner in Nester legen. Das funktionierte mechanisch, aber der Rand
des Nests zeichnete sich als eigene Kontur ab — eine Kante, die es auf Boden
nicht gibt.

**Der eigentliche Fehler hinter beiden Streurunden:** Sie rechneten gegen den
Erd-Ausschnitt (5,457) statt gegen das ganze Bild (5,486) und kamen so auf einen
nötigen Nachbarabstand von 0,084 statt der tatsächlich nötigen 0,0443 — fast das
Doppelte. Der Ausschnitt war als Diagnose gedacht, um zu sehen, *wo* Struktur
fehlt, und wurde versehentlich zum Abnahmemaß gemacht. Erst der Blick zurück in
`G002_ENTWURF.md` Abschnitt 3.1 hat das aufgedeckt.

---

## 11. Was geändert wurde

**Shader**
- `Assets\_Game\Shaders\AreaArtBlend.shader` — Vertexfarben-Alpha wiederhergestellt
- `Assets\_Game\Shaders\GroundDetail.shader` — Detail- und Makrolage

**Editor**
- `Assets\_Game\Editor\AreaArtGroundBuilder.cs` — Detailparameter, HomeBase dritte Lage
- `Assets\_Game\Editor\AreaArtGroundCoverBuilder.cs` — Bodenbewuchs
- `Assets\_Game\Editor\GroundMapImportSettings.cs` — Importvorgaben der Datenmasken
- `Assets\_Game\Editor\G002ZoneGroundCapture.cs` — Gebietsbilder bei identischer Kamera

**Texturen** (erzeugt durch `Tools\G002-Textures.ps1`, deterministisch)
- `Shared\ground_detail_grain_v01.png` — 96 Zellen, 4 Oktaven, Kontrast 1,00,
  Persistenz 0,75, Streu: Korn 2 Texel, Deckung 0,04, Amplitude 0,35
- `Shared\ground_macro_variation_v01.png` — 4 Zellen, 4 Oktaven, Kontrast 0,80
- `TwilightGrove\twilight_leaf_litter_v01.png`
- `VeilMarsh\veil_wet_silt_v01.png`
- `GreyRifts\rift_cold_scree_v01.png`

**Werkzeuge**
- `Tools\Measure-Ground.ps1`, `Tools\Compare-Zones.ps1`, `Tools\Crop-Image.ps1`
- `Tools\Predict-Detail.ps1`, `Tools\Try-Detail.ps1`
- `Tools\Try-Streu.ps1`, `Tools\Try-Streu2.ps1`, `Tools\Try-Streu3.ps1`

Die drei Streu-Skripte bleiben erhalten, obwohl zwei davon Sackgassen sind:
Ihre Kopfkommentare tragen die Messreihen, aus denen der Endstand hergeleitet
ist.

---

## 12. Keine Regression

Die EditMode-Suite wurde nach den G-002-Änderungen erneut ausgeführt und gegen
den letzten Lauf davor (`g000-tests-editmode.xml`, 05.08.2026 21:45) verglichen:

| | gesamt | bestanden | fehlgeschlagen |
| --- | ---: | ---: | ---: |
| vor G-002 | 709 | 636 | 73 |
| nach G-002 | 709 | 636 | 73 |

Nicht nur die Zahlen stimmen überein, sondern die **Menge der fehlgeschlagenen
Tests Namen für Namen** — verglichen mit `Compare-Object`, damit ein behobener
und ein neu zerbrochener Test sich nicht gegenseitig verdecken. G-002 hat keinen
Test verändert.

Die 73 Fehlschläge sind Altlasten der Dekompilat-Rekonstruktion: fehlende
`ITEM_TMP_`-Platzhalter, WorldMap-Prefabs, `Hammer.asset`, abweichende
Sprite-Zählungen, Zeilenlimit-Wächter. Keiner betrifft Boden, Blend-Shader oder
Bewuchs. Sie gehören nicht zu G-002 und werden hier nur benannt, damit die
Zahl 73 nicht später als Folge dieses Auftrags gelesen wird.

Vorher/Nachher reproduzieren:

    .unity-editor\Editor\Unity.exe -batchmode -projectPath "<Projektpfad>" `
      -runTests -testPlatform EditMode `
      -testResults g002-tests-editmode.xml -logFile g002-tests-editmode.log

---

## 13. Ausblick auf G-003

Kriterium 1 gegen den realen Ausgangswert (Abschnitt 1) ist mit Texturmitteln
erschöpft. Der wahrscheinlich tragfähige Weg liegt in G-003: Der Boden wird
derzeit **unbeleuchtet** bewertet — `Ground_Base` liegt auf `URP/Unlit`. Unter
Beleuchtung erzeugt Höhenvariation im Boden Schattierungsunterschiede, und die
zählen im Nachbardelta genauso mit wie Texturkontrast, ohne den gemalten Stil
anzugreifen. Was hier an Textur nicht mehr herauszuholen war, ohne Kriterium 6
zu brechen, könnte dort ohne diesen Konflikt entstehen.

Das ist eine Vermutung, keine Messung. Sie steht hier als Startpunkt für G-003,
nicht als Zusage.

---

## 14. Nachvollziehen

    .\Tools\G002-Textures.ps1
    .\Tools\G001-capture.ps1 -Baseline .\G001-Captures\Referenz
    .\Tools\Measure-Ground.ps1 -Bereich Voll -Pfad .\G001-Captures\Lauf1\04_boden_leer.png
    .\Tools\Compare-Zones.ps1

Gebietsbilder neu rendern (ohne `-nographics`, sonst bleibt das Bild leer):

    .unity-editor\Editor\Unity.exe -batchmode -projectPath "<Projektpfad>" `
      -executeMethod Eidren.Editor.G002ZoneGroundCapture.CaptureAll `
      -logFile g002-zonen.log -quit
