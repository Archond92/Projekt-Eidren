# G-003 Abnahme — Beleuchtung, Tonwertaufbau und Bildabstimmung

Stand: 06.08.2026
Bezug: `GRAFIKAUFTRAEGE_V0.2.1.md`, Auftrag G-003; Entwurf: `G003_ENTWURF.md`

Alle Zahlen stammen aus dem Buildstand `0.2.0-dev` vom 06.08.2026. Vorher-Stand
ist der G-002-Endstand, gesichert unter `G001-Captures\Stand-vor-G003\` samt
Messwerten (`histogramm-vorher.csv`, `boden-vorher.csv`).

---

## Kurzfassung

Alle acht Kriterien sind erfüllt. Zwei Auslegungen und ein Einzelbefund sind
unten offen benannt: Kriterium 1 wird als Richtungsübereinstimmung nachgewiesen,
weil die Schattenseite der Figuren gemalt und nicht gerechnet ist (Abschnitt 1);
Kriterium 7 wird strukturell nachgewiesen (Abschnitt 7); ein einzelner
PlayMode-Testfehlschlag trat einmal auf und ist in drei Kontrollläufen nicht
reproduzierbar (Abschnitt 9).

| # | Kriterium | Ergebnis |
| --- | --- | --- |
| 1 | Lichtrichtung stimmt mit eingemalter überein | erfüllt, 113,5° gegen 113,5° gemessen |
| 2 | Tonwertumfang messbar größer | erfüllt, Boden +35 %, alle fünf Positionen gewachsen |
| 3 | Figuren heben sich ohne Umriss-Zwang ab | erfüllt, Figurenhelligkeit stabil bei ±2 % |
| 4 | Kontaktbereiche abgedunkelt | erfüllt, 3,23 Lumastufen gemessen; 42 Prefabs + Dekoration |
| 5 | Benannte Farbstimmung je Gebiet | erfüllt, acht benannte Stimmungen |
| 6 | Nichts brennt aus oder säuft ab | erfüllt, max. 0,136 % / 0,029 % |
| 7 | HUD bleibt lesbar | erfüllt, strukturell (Screen Space Overlay) |
| 8 | Bildrate fällt nicht messbar ab | erfüllt, Differenzen innerhalb des Laufrauschens |

---

## 1. Lichtrichtung — Messung statt Behauptung

**Eingemalte Richtung** (`Tools\Measure-LightDirection.ps1`, zwei unabhängige
Verfahren, 0° = Licht von rechts, 90° = von oben):

| Figur | Verfahren A | Verfahren B | Abweichung | Wertung |
| --- | ---: | ---: | ---: | --- |
| Spieler, Zelle 0/Reihe 0 | 113,4° | 115,9° | 2,5° | belastbar |
| Spieler, Zelle 0/Reihe 2 | 113,6° | 115,6° | 2,0° | belastbar |
| Terrock | 87,6° | 91,5° | 3,9° | belastbar |
| Garon | 106,4° | 128,5° | 22,1° | schwach |
| Noctarion | 262,5° | 138,6° | 123,9° | verworfen (58 px zu schmal) |

Sollwert: **113,5°** (Spieler, über drei Frames auf 0,2° stabil). Terrock liegt
rund 22° höher; die Abweichung wird in Kauf genommen und hier benannt.

**Szenenrichtung:** Die Sonne steht seit G-003 auf `Euler(71.7, 156.2, 0)`.
Rückrechnung über `Tools\Convert-LightAngle.ps1`: Bildwinkel **113,5°**,
Abweichung zum Sollwert **0,0°**. Der Ist-Stand davor lag bei 27,5° — 86° neben
dem gemalten Licht, nahezu senkrecht dazu.

**Auslegung, offen benannt:** Der Auftrag verlangt die Prüfung „an einer Figur
mit deutlicher Schattenseite". Die Normal-Maps aller Figuren sind praktisch
flach (mittlere Neigung 2,2°–3,2°, Median 0,32°, 76–84 % unter 5°); ein
Richtungslicht kann auf ihnen keine Schattenseite **erzeugen**. Die
Schattenseite ist gemalt — der Auftragstext sagt das selbst („tragen ihr
eigenes, im Bild eingebranntes Licht"). Nachgewiesen wird deshalb die
Übereinstimmung der Richtungen, nicht eine gerechnete Formmodellierung.

Der Tiefenanteil der Sonne blieb beim Ist-Wert −0,678: `HandPaintedLitSprite`
bildet die Figurennormale aus der Billboard-Fläche, `N·L` der Figuren ist damit
exakt dieser Wert. Herleitung und verworfener Erstentwurf (−0,35, hätte das
Figurenlicht halbiert): `G003_ENTWURF.md` Abschnitt 3.2.

---

## 2. Tonwertumfang

`Tools\Measure-Histogram.ps1`, P99 − P1 auf identischen Kamerapositionen:

| Position | vorher | nachher | StdAbw vorher | StdAbw nachher |
| --- | ---: | ---: | ---: | ---: |
| 01_copper_set_spear | 130 | 135 | 21,96 | 24,73 |
| 02_mixed_ember_thorn | 125 | 129 | 21,35 | 24,16 |
| 03_iron_axe_harvest | 152 | 157 | 30,02 | 34,80 |
| **04_boden_leer** | **72** | **97** | **14,81** | **20,70** |
| 05_boden_bewuchs | 81 | 105 | 16,44 | 21,71 |

Der Boden — vorher das engste Tonwertband im Spiel — trägt den größten Zuwachs:
+35 % Umfang, +40 % Streuung. Histogramme beider Stände liegen als CSV neben den
Captures.

---

## 3. Figuren vor dem Boden

Die Figurenbelichtung blieb konstruktiv stabil (Tiefenanteil unverändert,
Abschnitt 1): mittlere Bildhelligkeit der Figurenpositionen 56,5→55,5 und
68,9→70,2 (±2 %). Der Boden unter ihnen gewann Struktur und Tonwerte, die
Figuren stehen sichtbar darauf statt davor; positionsgleiche Bildpaare
`Stand-vor-G003\01–03` gegen `Lauf1\01–03`. Der vorhandene Konturzeichner des
Figurenshaders blieb unverändert — er ist Stilmittel, nicht Notbehelf.

---

## 4. Kontaktabdunklung

**Messung am Spieler** (Position 01, Kasten unter den Füßen gegen zwei gleich
hohe Referenzkästen; die Vignette dunkelt die Referenzen stärker ab als die
Mitte, die Messung arbeitet also **gegen** den Nachweis):

| Stand | unter den Füßen | Referenzen | Abdunklung |
| --- | ---: | ---: | ---: |
| vorher | 53,54 | 54,54 / 54,40 | 0,93 (Texturrauschen) |
| **nachher** | **51,77** | **55,11 / 54,90** | **3,23** |

**Weltobjekte:** 42 Prefabs (Ressourcen-Visuals und Zonen-Varianten) tragen ein
statisches `ContactShadow`-Decal, die gebackenen Dekorationsinstanzen aller acht
Szenen ebenso (`WorldContactShadowBuilder`, Bewuchs bewusst ausgenommen).
Gleicher Shader, gleiche Tonwerte, gleiche Versatzformel wie die Figuren —
G-004 erweitert diesen einen Mechanismus, statt einen zweiten zu bauen.

**Warum unter den Figuren nie ein Schatten sichtbar war** — drei sich
verstärkende Fehler der Dekompilat-Rekonstruktion, alle in den Quellen behoben
(Herleitung: `G003_ENTWURF.md` Abschnitt 4.4):

1. Deckkraft doppelt: Material-Alpha (0,34–0,46) × Komponenten-Alpha (0,38) —
   zusammen unter 7 % Spitze. Jetzt: Material 1, Komponente steuert.
2. Größe doppelt: `pixelsPerUnit` und `localScale` wandten `baseSize` beide an —
   1,49 × 0,20 statt 1,22 × 0,48. Jetzt: `baseSize` ist die Weltgrundfläche.
3. Texturinhalt: die Schattenform lag als kleine Hochkant-Ellipse in der
   384×128-Leinwand (Spieler: Median-Alpha 0). Alle sechs Texturen wurden
   deterministisch als formatfüllende weiche Ellipse regeneriert, Spitze 111/255
   wie das Original; Originale in der Sicherung vom 05.08.2026.

Die Kette sichert seither `GroundShadowPlayModeTests` ab (bestanden).

---

## 5. Farbstimmung je Gebiet

Acht benannte Stimmungen (`ZoneLightingBuilder`), je Gebiet Trilight-Umgebung,
Sonnenfarbe aus der bislang **ungenutzten** `lightColor` der AreaArt-Definition,
und ein Volume-Profil mit `ColorAdjustments`, `LiftGammaGain` und `Vignette` —
die acht Profile waren seit ihrer Anlage leer (`components: []`) und die
Zonenkameras hatten Post-Processing nie aktiviert.

| Gebiet | Stimmung | gerenderte Bodenfarbe (R/G/B) |
| --- | --- | --- |
| Greenwood | Blattgold | 77,9 / 79,1 / 33,2 |
| Marsh | Nebelgrün | 34,4 / 39,3 / 25,2 |
| Quarry | Steinocker | 134,4 / 121,1 / 96,8 |
| EmberRuins | Glutrand | 74,3 / 37,3 / 20,3 |
| TwilightGrove | Dämmermoos | 44,6 / 54,7 / 21,4 |
| VeilMarsh | Schleierblau | 25,9 / 32,9 / 25,4 |
| GreyRifts | Kaltschiefer | 103,3 / 100,8 / 99,9 |
| HomeBase | Herdlicht | 219,3 / 148,6 / 54,3 |

Kleinster Abstand unter den vier Startgebieten: **40,2** (EmberRuins–Marsh).
Engstes Paar insgesamt, offen benannt: **Marsh–VeilMarsh mit 10,7** — zwei
dunkle Sümpfe. Sie trennen sich über die Temperatur, nicht die Helligkeit:
Blau-zu-Grün-Verhältnis 0,64 (warm-oliv) gegen 0,77 (kaltblau); eine erste
Fassung lag bei 9,3 und wurde gezielt auseinandergezogen. Das ist dieselbe
Situation und dieselbe Behandlung wie Quarry–GreyRifts (26,9) in G-002
Abschnitt 7. Zonenbilder: `TempReview\G002-Zonen\`.

Kein Tonemapping: Das Projekt rendert in Gamma mit LDR-Grading — URP baut die
Grading-LUT dann ohne Tonemapper, eine Tonemapping-Komponente wäre Leerlauf.
Aus demselben Grund war das ACES des Style-Proof-Volumes immer wirkungslos.
Schwarz- und Weißpunkt setzt `LiftGammaGain` (Lift −0,01…−0,02, Gain
+0,04…+0,07 je Gebiet).

---

## 6. Lichter und Tiefen

Anteile an den Bildrändern des Tonwertbereichs (Schwellen ≤5 bzw. ≥250):

| Position | abgesoffen | ausgebrannt |
| --- | ---: | ---: |
| 01 | 0,034 % | 0,028 % |
| 02 | 0,035 % | 0,012 % |
| 03 | 0,136 % | 0,029 % |
| 04 | 0,000 % | 0,012 % |
| 05 | 0,011 % | 0,012 % |

P1 liegt bei allen Positionen ≥15, P99 ≤172 — Zeichnung bleibt an beiden Enden
erhalten.

---

## 7. HUD

Struktureller Nachweis: Alle HUD-Fenster laufen auf `ScreenSpaceOverlay`
(zwölf Builder-/Skriptstellen, geprüft per Suche); Overlay-Canvases werden nach
dem Post-Processing gezeichnet und sind von Volume-Profilen, Tonwert- und
Farbanpassungen konstruktiv unerreichbar. Einzige `WorldSpace`-Canvas ist die
Eidra-Fanganzeige — eine In-Welt-Anzeige, kein HUD. Die Capture-Strecke blendet
das HUD aus; ein eigener HUD-Capture entfällt deshalb, der Nachweis ist die
Bauart, nicht ein Bild.

---

## 8. Bildrate

Verfahren aus G-002 Abschnitt 8 (240 Frames nach 60 Vorlauf, vSync und
captureFramerate aus), beide Läufe des Endstands:

| Konfiguration | Lauf 1 Median | Lauf 2 Median |
| --- | ---: | ---: |
| Boden unbeleuchtet (URP/Unlit), ohne Bewuchs | 0,914 ms | 0,948 ms |
| **Boden beleuchtet (Eidren/Ground Detail)** | **0,878 ms** | **0,876 ms** |
| dazu Bewuchs | 0,892 ms | 0,905 ms |

Der beleuchtete Boden misst sich in beiden Läufen nicht langsamer als der
unbeleuchtete; die Differenzen liegen im Laufrauschen (die Unlit-Konfiguration
zahlt als erste Messung den Aufwärmpreis). Kein messbarer Abfall.

---

## 9. Keine Regression

**EditMode** nach allen Änderungen gegen den G-002-Stand, Namen für Namen per
`Compare-Object`:

| | gesamt | bestanden | fehlgeschlagen |
| --- | ---: | ---: | ---: |
| G-002-Stand | 709 | 636 | 73 |
| G-003-Endstand | 709 | 636 | 73 |

Menge der Fehlschläge identisch — dieselben 73 Altlasten der
Dekompilat-Rekonstruktion wie in G-002 Abschnitt 12.

**PlayMode** gegen die G-000-Basis (115 Tests, 4 Fehlschläge):

| | gesamt | bestanden | fehlgeschlagen |
| --- | ---: | ---: | ---: |
| G-000-Basis | 115 | 110 | 4 |
| G-003-Endstand | 116 | 112 | 4 |

Der Zuwachs ist der neue `GroundShadowPlayModeTests` (bestanden); die vier
Fehlschläge sind namensgleich die der Basis.

**Einzelbefund, offen benannt:** Im ersten vollen PlayMode-Lauf fiel
`FloatingText_IsResetAndReused` einmalig (erwartet inaktiv, war aktiv). Der
Test bestand isoliert, bestand im Paarlauf mit dem neuen Test und bestand im
vollständigen Wiederholungslauf; in allen zehn archivierten früheren Läufen war
er grün. Einstufung: nicht reproduzierbarer Einzelfall, keine Folge von G-003 —
aber hier festgehalten, damit ein erneutes Auftreten ein Muster ergibt statt
einer Überraschung.

**Deckungsgleichheit:** Zwei Capture-Läufe ohne Änderung liefern an allen fünf
Positionen bytegleiche Bilder.

---

## 10. Anschluss an G-002 Kriterium 1

G-002 ließ offen: Nachbardelta 8,179 = 1,49-faches des realen Ausgangswerts
5,486; Abschnitt 13 vermutete den weiteren Weg in der Beleuchtung. Die
Vermutung hat sich bestätigt:

| Stand | Nachbardelta | Faktor zum realen Ausgangswert |
| --- | ---: | ---: |
| vor G-002 | 5,486 | 1,00 |
| G-002-Endstand | 8,179 | 1,49 |
| **G-003-Endstand** | **11,504** | **2,10** |

Das Dreifache (16,458) ist weiterhin nicht erreicht und wird weiterhin nicht
erzwungen — die Grenze bleibt der gemalte Stil, wie in G-002 entschieden. Der
Zuwachs von +41 % kam ohne neue Textur: Relief aus der vorhandenen Körnung
(`_NormalStrength` 0,4), Schattenwurf und Bildabstimmung.

---

## 11. Was geändert wurde

**Shader**
- `Assets\_Game\Shaders\GroundDetail.shader` — beleuchtet; Normale per Finite-Differenz aus der Körnung
- `Assets\_Game\Shaders\AreaArtBlend.shader` — identische Lichtrechnung, keine Naht an Flickenkanten
- `Assets\_Game\Shaders\EidrenGroundLighting.hlsl` — gemeinsame Lichtfunktion beider Bodenshader (neu)

**Editor**
- `Assets\_Game\Editor\ZoneLightingBuilder.cs` — Sonne, Trilight, Volume-Füllung, Stimmungstabelle (neu)
- `Assets\_Game\Editor\WorldContactShadowBuilder.cs` — Kontaktdecals, Texturregeneration (neu)
- `Assets\_Game\Editor\EidrenSceneStructureBuilder.cs` — CreateZoneLight/CreateZoneCamera lesen die gemeinsamen Werte

**Laufzeit**
- `Assets\_Game\Scripts\Presentation\DynamicActorGroundShadow.cs` — Deckkraft- und Größenvertrag repariert

**Daten**
- 8 Volume-Profile gefüllt, 8 Szenen (Sonne, Ambient, Kamera-Flag, Dekorationsdecals)
- 5 Schattenmaterialien auf Alpha 1
- 6 Akteur-Schattentexturen regeneriert, 1 Welt-Decal-Textur erzeugt
- 42 Prefabs mit `ContactShadow`-Kind

**Tests und Werkzeuge**
- `Assets\_Game\Tests\PlayMode\GroundShadowPlayModeTests.cs` (neu)
- `Tools\Measure-LightDirection.ps1`, `Tools\Convert-LightAngle.ps1`, `Tools\Measure-Histogram.ps1` (neu)

---

## 12. Verworfene Wege

**Sonnentiefe −0,35** (Erstentwurf). Hätte den Bildwinkel korrekt gesetzt, aber
das gerichtete Licht jeder Figur nahezu halbiert, weil die Figurennormale die
Billboard-Fläche ist. Der Tiefenanteil ist auf Billboards nicht messbar; die
heutige Belichtung ist der einzige Anker. `G003_ENTWURF.md` Abschnitt 3.2.

**Normal-Maps der Figuren neu backen.** Hätte echte Formmodellierung gegeben,
aber das gemalte Licht läge dann doppelt im Bild und beide Lichter drehten
gegeneinander bei jedem Richtungswechsel. Entwurf Abschnitt 7.

**SSAO für die Kontaktabdunklung.** Bildraum-Höfe um jede Billboard-Silhouette
plus Frametime gegen Kriterium 8. Entwurf Abschnitt 7.

**Tonemapping (Neutral wie ACES).** In Gamma + LDR-Grading baut URP die LUT
ohne Tonemapper — die Komponente wäre dokumentierte Wirkungslosigkeit. Der
Befund erklärt rückwirkend, warum das ACES im Style-Proof nie etwas tat.

**Mathf.SmoothStep für den Texturabfall.** Ist ein geglätteter Lerp zwischen
zwei Werten, nicht das erwartete Kanten-Smoothstep; die Spitzendeckung der
ersten Texturfassung lag ein Viertel unter Soll (83 statt 111) und wurde mit
korrekt implementiertem Abfall neu erzeugt.

---

## 13. Nachvollziehen

    .\Tools\Measure-LightDirection.ps1 -Pfad (Get-ChildItem TempReview\AssetComparisons\CurrentBuild\*_frame_00.png).FullName
    .\Tools\Convert-LightAngle.ps1 -SonneX 71.7 -SonneY 156.2
    .\Tools\G001-capture.ps1 -Baseline .\G001-Captures\Stand-vor-G003
    .\Tools\Measure-Histogram.ps1 -Pfad (Get-ChildItem G001-Captures\Lauf1\*.png).FullName
    .\Tools\Measure-Ground.ps1 -Bereich Voll -Pfad G001-Captures\Lauf1\04_boden_leer.png

Szenen und Profile neu aufbauen:

    .unity-editor\Editor\Unity.exe -batchmode -projectPath "<Projektpfad>" `
      -executeMethod Eidren.Editor.ZoneLightingBuilder.ApplyAllForAutomation -quit
    .unity-editor\Editor\Unity.exe -batchmode -projectPath "<Projektpfad>" `
      -executeMethod Eidren.Editor.WorldContactShadowBuilder.BuildAllForAutomation -quit

Tests:

    .unity-editor\Editor\Unity.exe -batchmode -projectPath "<Projektpfad>" `
      -runTests -testPlatform EditMode -testResults g003-tests-editmode-2.xml
    .unity-editor\Editor\Unity.exe -batchmode -projectPath "<Projektpfad>" `
      -runTests -testPlatform PlayMode -testResults g003-tests-playmode-2.xml
