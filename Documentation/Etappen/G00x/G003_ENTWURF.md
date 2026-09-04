# G-003 — Beleuchtung, Tonwertaufbau und Bildabstimmung (Entwurf)

**Stand:** 6. August 2026
**Auftrag:** `Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md`, G-003
**Voraussetzung:** G-000, G-001 und G-002 abgeschlossen

---

## 1. Befund vor der Arbeit

Der Auftrag beschreibt die Szene als „flach und unbeleuchtet" und nennt als
Ursache, die Szenenbeleuchtung nehme auf das eingemalte Licht der Figuren keinen
Bezug. Beides trifft zu. Die Messung zeigt aber, dass drei der vier Bausteine
bereits im Projekt liegen und nur falsch eingestellt oder nie gefüllt wurden.

### 1.1 Was schon da ist

| Baustein | Zustand | Stelle |
| --- | --- | --- |
| Richtungslicht je Zone | vorhanden, Euler(48, −32, 0), ohne Schatten | `EidrenSceneStructureBuilder.cs:587` |
| Lichtfarbe je Gebiet | **in den Daten vorhanden, wird nie benutzt** | `AreaArtAssetBuilder.cs:263` ff. |
| Volume je Gebiet | acht Profile vorhanden, **alle mit `components: []`** | `Settings/AreaArt/*_AreaArt_Volume.asset` |
| Volume in der Szene | wird bereits eingehängt | `AreaArtSceneBuilder.cs:189` |
| Beleuchteter Figurenshader | vorhanden, mit Normal-, Emissions- und Schattenpfad | `Eidren/Actors/HandPaintedLitSprite` |
| Bodenschattenshader | vorhanden, liest bereits das Richtungslicht | `ActorGroundShadow.shader`, `DynamicActorGroundShadow.cs` |
| Vollständiges Vorbild | ACES, Bloom, ColorAdjustments, Vignette, Trilight, Fog | `StyleProofContentBuilder.cs:493` |

Die acht Lichtfarben aus `AreaArtAssetBuilder` stehen seit jeher in
`ZoneAreaArtDefinition.lightColor` und werden von keiner Stelle gelesen. Die
Sonne ist in allen acht Zonen identisch. Das ist kein Gestaltungsentschluss,
sondern eine nicht angeschlossene Leitung.

### 1.2 Der Boden erreicht das Licht nicht

`GroundDetail.shader` deklariert `LightMode = UniversalForward`, bindet aber nur
`Core.hlsl` ein, nicht `Lighting.hlsl`. Es findet keine Lichtrechnung statt. Der
Boden ist damit die größte Fläche im Bild und zugleich die einzige, die von
jeder Beleuchtungsänderung unberührt bleibt. Das deckt sich mit Abschnitt 13 des
G-002-Berichts.

**Wichtige Einschränkung dazu:** `Lighting.hlsl` allein genügt nicht. Die
Bodenfläche ist ein ebenes Mesh mit einer einzigen, nach oben zeigenden
Normalen, und `_DetailMap` und `_MacroMap` sind reine Graustufen-Multiplikatoren
auf die Albedo. Ein Richtungslicht auf diese Fläche liefert **einen einzigen
konstanten Faktor über den gesamten Boden** — keine Reliefwirkung und keinen
Zuwachs am Nachbardelta. Die Vermutung aus G-002 Abschnitt 13 trägt nur, wenn
aus der vorhandenen Körnung zuerst eine Normale abgeleitet wird.

### 1.3 Die Figuren können vom Licht nicht geformt werden

Gemessen über alle gedeckten Pixel der Normal-Maps, Neigung gegen die
Blickrichtung:

| Atlas | Mittel | Median | 90 % | 99 % | max | Anteil < 5° |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `player_wanderer_idle_normal` | 2,23° | 0,32° | 8,02° | 19,49° | 37,49° | 83,5 % |
| `garon_idle_normal` | 3,21° | 0,32° | 10,27° | 20,92° | 38,78° | 75,9 % |
| `terrock_idle_normal` | 3,01° | 0,32° | 9,30° | 22,97° | 44,26° | 78,3 % |

Die Normalen sind praktisch flach. Ein gerichtetes Licht erzeugt darauf keine
Schattenseite, sondern nur einen gleichmäßigen Helligkeitsfaktor.

Das widerspricht dem Auftrag nicht, sondern bestätigt dessen eigene Analyse:
„Die gemalten Figuren tragen ihr eigenes, im Bild eingebranntes Licht." Die
Aufgabe der Szenenbeleuchtung ist demnach, mit diesem gemalten Licht
**übereinzustimmen**, nicht es zu ersetzen.

**Folge für Kriterium 1.** Der Auftrag verlangt die Prüfung „an einer Figur mit
deutlicher Schattenseite". Diese Schattenseite ist gemalt und nicht gerechnet.
Der Nachweis erfolgt deshalb als Übereinstimmung der Szenenlichtrichtung mit der
gemessenen gemalten Richtung (Abschnitt 2), nicht als erzeugte Schattenseite.
Das ist eine Auslegung des Kriteriums und steht hier, damit sie überprüfbar ist.

---

## 2. Die eingemalte Lichtrichtung

Gemessen mit `Tools\Measure-LightDirection.ps1` auf den Einzelframes unter
`TempReview\AssetComparisons\CurrentBuild\` sowie auf drei aus
`player_wanderer_idle_albedo.png` geschnittenen Atlaszellen.

Das Werkzeug rechnet zwei voneinander unabhängige Verfahren: **A** stellt den
Helligkeits- gegen den Flächenschwerpunkt, **B** wertet je Bildzeile links gegen
rechts und je Spalte oben gegen unten aus. A ist formabhängig, B nicht.
Stimmen beide überein, ist der Befund belastbar; weichen sie ab, ist es eine
Form- und keine Lichtaussage.

Winkelkonvention: 0° = Licht von rechts, 90° = von oben, 180° = von links.

| Figur | A | B | Abweichung | Wertung |
| --- | ---: | ---: | ---: | --- |
| Spieler, Zelle 0 Reihe 0 | 113,4° | 115,9° | 2,5° | belastbar |
| Spieler, Zelle 0 Reihe 2 | 113,6° | 115,6° | 2,0° | belastbar |
| Terrock | 87,6° | 91,5° | 3,9° | belastbar |
| Garon | 106,4° | 128,5° | 22,1° | schwach |
| Noctarion | 262,5° | 138,6° | 123,9° | **verworfen** |

Noctarion wird nach der Regel des Werkzeugs verworfen: 58 Pixel
Silhouettenbreite sind für die spaltenweise Auswertung zu schmal. Der Wert wird
nicht eingemittelt.

Der Spielerwert liegt über drei Frames bei 113,4 / 113,5 / 113,6. Diese
Stabilität ist selbst ein Beleg, dass das Verfahren greift und nicht Rauschen
misst.

**Als Sollwert wird 113,5° genommen** — der Spieler ist die Figur, die in jedem
Bild vorkommt. Terrock liegt mit 88–92° höher und flacher; die Abweichung von
rund 22° wird in Kauf genommen und in der Abnahme benannt.

---

## 3. Umrechnung in eine Sonnenstellung

Zwischen der gemessenen Bildrichtung und der Euler-Drehung des Lichts steht die
Kamera, und die steht auf `Euler(52, 45, 0)` — geneigt **und** gedreht
(`G001VisualAbnahmeRunner.cs:133`). Die Umrechnung erledigt
`Tools\Convert-LightAngle.ps1` in beide Richtungen, mit Gegenprobe.

### 3.1 Der Ist-Stand steht fast quer

    .\Tools\Convert-LightAngle.ps1 -SonneX 48 -SonneY -32

| Größe | Wert |
| --- | ---: |
| Sonnenhöhe | 48,0° |
| **Bildwinkel** | **27,5°** |
| Tiefenanteil | −0,678 (Frontlicht) |

Gegen die eingemalten 113,5° ist das eine Abweichung von **86,0°**, also nahezu
senkrecht. Das ist die messbare Ursache dafür, dass Figuren und Welt „nicht als
ein Bild zusammenfinden".

### 3.2 Die Wahl der Tiefe

Zu jedem Bildwinkel gibt es eine ganze Schar von Sonnenstellungen, die sich nur
im Anteil senkrecht zur Bildebene unterscheiden. Dieser Anteil ändert den
Bildwinkel **nicht** und ist damit frei wählbar. Er entscheidet aber über zwei
Dinge gleichzeitig, die gegeneinander laufen:

- Für eine flache, zur Kamera zeigende Figurennormale ist `N·L` genau der
  negative Tiefenanteil. Bei Tiefe 0 bekämen die Figuren **gar kein** gerichtetes
  Licht mehr.
- Die Reliefwirkung auf dem Boden wächst mit dem Kosinus der Sonnenhöhe. Eine
  tief stehende Sonne streift und zeichnet, eine hohe Sonne flacht ab.

| Tiefe | Sonne | Höhe | `N·L` Figur | `N·L` Boden | Relief (cos Höhe) |
| ---: | --- | ---: | ---: | ---: | ---: |
| 0 | (34,4, −163,9, 0) | 34,4° | **0,000** | 0,565 | 0,825 |
| −0,30 | (50,8, −172,0, 0) | 50,8° | 0,300 | 0,775 | 0,632 |
| −0,35 | (53,6, −174,0, 0) | 53,6° | 0,350 | 0,806 | 0,593 |
| −0,50 | (62,0, 177,6, 0) | 62,0° | 0,500 | 0,883 | 0,469 |
| **−0,678** | **(71,7, 156,2, 0)** | **71,7°** | **0,678** | **0,949** | **0,313** |
| *Ist* | *(48, −32, 0)* | *48,0°* | *0,678* | *0,743* | *0,669* |

**Gewählt: `Euler(71.7, 156.2, 0)`**, Tiefe −0,678 — **derselbe Tiefenanteil wie
der Ist-Stand**. Ein erster Entwurf hatte −0,35 vorgesehen; das wurde beim Blick
in den Figurenshader verworfen. `HandPaintedLitSprite` bildet die Normale aus
der Billboard-Fläche (`faceWS`), womit `N·L` der Figuren exakt der negative
Tiefenanteil ist: Tiefe −0,35 hätte das gerichtete Licht auf **jeder Figur
nahezu halbiert** (0,35 statt 0,678). Der Tiefenanteil ist auf einem Billboard
grundsätzlich nicht messbar — die heutige, sichtbar richtige Belichtung der
Figuren ist der einzige Anker, und sie bleibt mit −0,678 unverändert. Korrigiert
wird ausschließlich die messbare Größe: der Bildwinkel, von 27,5° auf 113,5°.

Der Preis steht in der Tabelle: Die Reliefempfindlichkeit des Bodens sinkt auf
0,313. Das wird über eine höhere `_NormalStrength` ausgeglichen — dieselbe
Reliefwirkung, nur anders parametrisiert. Die steile Sonne (71,7°) wirft zudem
kurze Schlagschatten; für die Draufsicht ist das eher ein Gewinn, und die
Objektverankerung übernimmt ohnehin der Bodenschatten aus G-004.

Was **nicht** verschoben werden darf, ist der Bildwinkel von 113,5°.

---

## 4. Was gebaut wird

### 4.1 Sonne je Zone

`EidrenSceneStructureBuilder.CreateZoneLight` bekommt die neue Ausrichtung,
aktivierte Schatten (`LightShadows.Hard` wie im Style-Proof — weiche Schatten
sind im URP-Asset abgeschaltet —, Stärke 0,55 statt dessen 0,72, weil die
Kontaktabdunklung aus 4.4 zusätzlich unter den Figuren liegt) und liest endlich
`ZoneAreaArtDefinition.lightColor`, statt für alle acht Zonen dieselbe Farbe zu
setzen. Die Werte selbst liegen in `ZoneLightingBuilder`, damit neu gebaute und
nachträglich umgestellte Szenen dieselbe Sonne tragen.

### 4.2 Beleuchteter Boden

`GroundDetail.shader` bindet `Lighting.hlsl` ein und leitet die Normale per
Finite-Differenz aus `_DetailMap` und `_MacroMap` ab. Die Stärke liegt auf einem
eigenen `_NormalStrength` und startet bewusst niedrig.

Die Ableitung greift auf dieselben Lagen zu, deren Kachelfaktoren in G-002
mühsam abgestimmt wurden (`_DetailScale 0,42`, `_MacroScale 0,137`). Diese
Faktoren bleiben unangetastet.

### 4.3 Umgebungslicht und Farbstimmung

Ein neuer `ZoneLightingBuilder` setzt je Gebiet ein Trilight-Umgebungslicht nach
dem Muster aus `StyleProofContentBuilder.cs:512` und füllt die acht leeren
Volume-Profile mit Tonemapping, `ColorAdjustments`,
`ShadowsMidtonesHighlights` und `Vignette`. Jedes Gebiet bekommt eine benannte
Farbstimmung, abgeleitet aus der bereits vorhandenen `lightColor` und der in
G-002 gemessenen mittleren Bodenfarbe.

**Tonemapping wird auf `Neutral` gesetzt, nicht auf `ACES`.** Das Projekt läuft
im Gamma-Farbraum (`m_ColorSpace` fehlt in `ProjectSettings.asset`, siehe G-002
Abschnitt 10). ACES rechnet dort auf bereits gammakodierten Werten und drückt
Mitten und Sättigung sichtbar weg. Der Style-Proof benutzt ACES — das ist der
eine Punkt, an dem dieser Auftrag bewusst von ihm abweicht.
`m_ColorGradingMode` bleibt auf LDR.

### 4.4 Kontaktabdunklung

Ein Bodendecal unter jedem stehenden Objekt, aufgesetzt auf den vorhandenen
`ActorGroundShadow`. Der Mechanismus wird so gebaut, dass G-004 ihn um Bewegung,
Sprung, Ausweichrolle und Hangneigung **erweitern** kann, statt ihn zu ersetzen.
Die Schattenrichtung ergibt sich automatisch, weil `DynamicActorGroundShadow`
das Richtungslicht bereits ausliest.

**Nachtrag aus der Umsetzung — warum der Figurenschatten nie sichtbar war.**
Das Spieler-Prefab trägt die Komponente seit jeher, und sie lief fehlerfrei.
Unsichtbar war der Schatten durch drei sich verstärkende Fehler der
Dekompilat-Rekonstruktion, alle in den Quellen behoben:

1. **Deckkraft doppelt multipliziert.** Material-`_Color.a` (0,34–0,46) und
   Komponenten-`baseOpacity` (0,38) kodieren dieselbe Größe; zusammen mit dem
   Texturalpha blieb eine Spitzendeckung unter 7 Prozent. Die Materialien
   tragen jetzt Alpha 1, die Deckkraft steuert allein die Komponente.
2. **Größe doppelt angewandt.** `Sprite.Create` normierte bereits auf
   `baseSize.x`, `localScale` multiplizierte `baseSize` ein zweites Mal — aus
   1,22 × 0,48 wurde ein Streifen von 1,49 × 0,20. Jetzt gilt: `baseSize` ist
   die Weltgrundfläche, normiert über die tatsächliche Spritegröße.
3. **Texturinhalt unbrauchbar.** Die sechs `*_ground_shadow.png` tragen ihre
   Form als kleine, hochkant stehende Ellipse in der 384×128-Leinwand
   (Spieler: Median-Alpha 0, nur ein Sechstel der Breite gedeckt). Auf die
   Grundfläche gespannt blieb ein Fleck von ~0,19 Einheiten. Die Texturen
   werden deterministisch als formatfüllende weiche Ellipse regeneriert
   (`WorldContactShadowBuilder.RegenerateActorShadowTextures`), Spitzendeckung
   111/255 wie das Original; die Originaldateien liegen in der Sicherung vom
   5. August 2026.

Die Kette ist seither durch `GroundShadowPlayModeTests` abgesichert: Sprite
erzeugt, Renderer aktiv, Vertragsgröße, Vertragsdeckkraft, Fußposition,
Material-Alpha 1.

---

## 5. Abnahme

| # | Kriterium | Nachweis |
| --- | --- | --- |
| 1 | Lichtrichtung stimmt mit eingemalter überein | `Convert-LightAngle.ps1` gegen `Measure-LightDirection.ps1`, Auslegung nach Abschnitt 1.3 |
| 2 | Tonwertumfang messbar größer | `Measure-Ground.ps1` plus neues `Measure-Histogram.ps1`, vorher/nachher |
| 3 | Figuren heben sich ohne Umriss ab | positionsgleiche Captures |
| 4 | Kontaktbereiche abgedunkelt | Ausschnitt in Originalgröße über `Crop-Image.ps1` |
| 5 | Jedes Gebiet mit benannter Farbstimmung | `Compare-Zones.ps1` plus Namensliste |
| 6 | Nichts brennt aus oder säuft ab | Histogrammränder, Anteil bei 0 und 255 |
| 7 | HUD bleibt lesbar | Capture; strukturell bereits durch Screen Space Overlay gesichert |
| 8 | Bildrate fällt nicht messbar ab | Verfahren aus G-002 Abschnitt 8 |

Vor der ersten Änderung wird über `Tools\G001-capture.ps1` ein vollständiger
Vorher-Satz gesichert. Zusätzlich wird die EditMode-Suite gegen
`g002-tests-editmode.xml` gestellt — 709 gesamt, 636 bestanden, 73
fehlgeschlagen — und zwar **Namen für Namen** per `Compare-Object`, damit ein
behobener und ein neu zerbrochener Test sich nicht gegenseitig verdecken.

---

## 6. Risiken

**Die Bodennormale kann G-002 Kriterium 6 brechen.** Zu stark eingestellt sieht
der Boden nach Kies aus statt nach gemalter Erde. Die Abstimmung erfolgt an
denselben 1:1-Ausschnitten, an denen G-002 seine Streu-Kandidaten verworfen hat.
Im Zweifel gilt Kriterium 6, wie schon in G-002.

**Die neue Sonnenrichtung ändert jedes bestehende Capture.** Die
G-002-Referenzbilder bleiben als Vorher-Stand gültig, aber die Gebietsbilder in
`TempReview\G002-Zonen\` zeigen danach einen überholten Stand und werden neu
gerendert.

**Der Tiefenanteil ist eine Setzung.** Abschnitt 3.2 begründet −0,35, aber die
Wahl ist ein Kompromiss zwischen Figurenhelligkeit und Bodenrelief und keine
aus den Daten folgende Notwendigkeit.

---

## 7. Verworfene Wege

**Normal-Maps aus der Albedo neu backen.** Naheliegend, weil dasselbe Verfahren
für den Boden ohnehin gebaut wird, und es würde den Figuren echte
Formmodellierung geben. Verworfen: Das gemalte Licht der Figuren liegt schon in
der Albedo. Ein daraus abgeleitetes Relief würde von der Szenensonne ein zweites
Mal beleuchtet, und beide Lichter kämen aus verschiedenen Richtungen, sobald die
Figur sich dreht. Der Auftrag verlangt ausdrücklich, die Szenenbeleuchtung dem
gemalten Licht anzupassen — nicht, das gemalte Licht zu verdoppeln.

**SSAO als Renderer Feature für die Kontaktabdunklung.** Die lehrbuchmäßige
Lösung. Verworfen: SSAO arbeitet im Bildraum, und in einer Welt aus
Billboard-Sprites setzt es sichtbare Höfe um jede Figurensilhouette. Dazu kämen
Frametime-Kosten gegen Kriterium 8.

**Wechsel in den linearen Farbraum.** Er wäre das saubere Fundament für
Beleuchtung und Tonwertaufbau und ist vor der Lichtabstimmung billiger als
danach. Verworfen als eigener Auftrag: Der Wechsel interpretiert alle 200
Materialien und sämtliche Texturen neu, verschiebt den ausgelieferten Look und
entwertet die Messwerte aus G-002. Diese Entscheidung wurde am 6. August 2026
ausdrücklich getroffen und ist keine Unterlassung.
