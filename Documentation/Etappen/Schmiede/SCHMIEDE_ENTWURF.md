# Verlassene Eidra-Schmiede — Entwurf für den Neubau

Stand: 15.08.2026. Ersetzt das aktuelle Layout der Zone `eidra_forge`
(Szene `EidraForge.unity`, erzeugt von `EidraForgeSceneBuilder`).

Die Lichtwerte und die Essenhöhe in diesem Dokument sind durch die
Glut-Probe (`ForgeGlowProbe`, Belege in `TempReview/SchmiedeGlut/`)
gemessen, nicht geschätzt.

---

## 1. Ausgangslage

Der bestehende Dungeon besteht aus sechs Rechtecken auf einer geraden Linie
bei X=0, verbunden durch fünf Korridore. Drei der sechs Räume
(`Kampfhalle I–III`) sind identisch: gleiche Grundfläche 14 × 8, gleiche
Wände, gleiche Gegnermischung. Es gibt eine Bodenebene, keine Verzweigung,
keine Entscheidung.

Beim Nachrechnen der Builder-Koordinaten sind vier Fehler aufgefallen, die
mit dem Neubau entfallen:

| Fehler | Ort |
|---|---|
| Beide Seitenwege liegen hinter durchgehenden Wänden ohne Öffnung; auch außenherum fehlt Boden | `OptionalPath_Left`, `SealGuardianPath` |
| `forge.optional.02` steht auf (−10 \| 0 \| 7) — dort ist kein Boden | Gießhalle |
| Die ersten sechs AshRunner stehen auf identischen Koordinaten wie die ersten sechs EmberEater | `PlaceGroup`-Indexformel |
| Der Kernarena-Boden ist 18 breit, die Wände bleiben bei ±7,2 und laufen mitten durch den Raum | Kernarena |

Dazu die Ursache der fehlenden Stimmung: die Räume haben **keine Decke und
keine Stirnwände**, und die Zone erbt die Außenbeleuchtung aus
`Zone_EmberRuins`.

---

## 2. Leitidee

Der Grundriss **ist** ein Schmiedewerk. Der Spieler läuft von Süden nach
Norden genau den Weg, den die Luft im Betrieb nahm: vom Blasebalg durch die
Düse in die Gießhalle und von dort in die Esse.

**Verlassen** wird im Grundriss sichtbar: das Werk war symmetrisch gebaut,
aber die östliche Hälfte der Gießhalle ist eingestürzt. Die intakte
Westseite zeigt, wie es gemeint war; der `Einbruch` zeigt, was passiert ist.
Die Asymmetrie des Levels ist Verfall, nicht Willkür.

**Eidra**: die `Bindungskammer` liegt abseits an der Galerie. Wer geradeaus
zum Boss läuft, verpasst Ignivar und merkt es nie.

---

## 3. Grundriss

Koordinaten in Spieleinheiten. X quer, Z von Süd (Eingang) nach Nord (Boss),
Y die Höhe. Gesamtausdehnung **88 × 72**, gegenüber bisher 58 × 26.

### Akt 1 — Blasebalg

Der Raum verengt sich von 20 auf 6 Einheiten Breite. Die Form erzeugt den
Druck, nicht die Gegnerzahl.

| Fläche | X | Z | Anmerkung |
|---|---|---|---|
| Windfang | −10…10 | −38…−30 | Startpunkt, sicher |
| Schlund | −5…5 | −30…−28 | Verengung |
| Balgkammer | −7…7 | −28…−20 | erster Kampfraum |
| Düse | −3…3 | −20…−10 | Schlauch, kein Ausweichen |

### Akt 2 — Gießhalle

Die `Rinne` ist der schnelle Weg. Links die intakte Masselreihe, rechts der
`Einbruch`. Beide Seiten sind Rundwege und münden wieder auf die Galerie.

| Fläche | X | Z | Anmerkung |
|---|---|---|---|
| Rinne (Sau) | −4…4 | −10…14 | Hauptgussrinne |
| Massel I | −24…−6 | −8…−4 | Formkammer |
| Massel II | −24…−6 | 0…4 | Formkammer |
| Steg | −24…−20 | −4…0 | verbindet I und II |
| Verbinder Rinne→Massel I | −6…−4 | −7…−5 | |
| Verbinder Rinne→Massel II | −6…−4 | 1…3 | |
| Verbinder Massel II→Ring | −20…−16 | 4…14 | |
| Einbruch A | 5…24 | −8…−2 | |
| Einbruch B | 8…28 | −2…4 | |
| Einbruch C | 5…20 | 4…10 | |
| Verbinder Rinne→Einbruch | 4…6 | −6…0 | Wanddurchbruch |
| Verbinder Einbruch→Ring | 12…18 | 10…14 | |

Die drei überlappenden Flächen des Einbruchs ergeben eine zerrissene
Silhouette, an der man erkennt, dass hier zwei spiegelgleiche Masselkammern
standen.

### Akt 3 — Esse

Ein geschlossener Ring um eine sechs Einheiten tiefe Grube. Der Kernwächter
ist von der Galerie aus die ganze Zeit sichtbar.

| Fläche | X | Z | Y | Anmerkung |
|---|---|---|---|---|
| Ring Süd | −20…20 | 14…22 | 0 | Zugang aus der Gießhalle |
| Ring West | −20…−10 | 22…42 | 0 | |
| Ring Ost | 10…20 | 22…42 | 0 | |
| Ring Nord | −20…20 | 42…50 | 0 | Abschlusstruhe |
| Essenkern | −10…10 | 22…42 | −6 | Boss |
| Treppe West | −16…−10 | 26…30 | 0 → −6 | |
| Treppe Ost | 10…16 | 34…38 | 0 → −6 | |
| Abstichrinne | −3…3 | 42…48 | −6 → 0 | einziger Ausgang aus der Grube |
| Hammerwerk | 22…36 | 26…38 | 0 | optional |
| Verbinder Hammerwerk | 20…22 | 30…34 | 0 | |
| Bindungskammer | −36…−22 | 26…38 | 0 | optional |
| Verbinder Bindungskammer | −22…−20 | 30…34 | 0 | |

Die beiden Treppen liegen versetzt, damit die Wahl der Anlaufseite eine
Entscheidung ist.

---

## 4. Wege

Drei Wege führen von der Gießhalle auf die Galerie:

- **Rinne** — schnell, keine Truhe, dafür beide ForgeGuardian im Weg
- **Masselreihe** — geordnet, zwei Vorratstruhen, Rundweg über den Steg
- **Einbruch** — unübersichtlich, der Bauplatz, dichteste Gegnergruppe

Kein Weg ist Pflicht, keiner ist eine Sackgasse. Rückwege sind kurz, weil
der Ring geschlossen ist.

---

## 5. Inhalt

### Gegner

| Raum | Besatzung |
|---|---|
| Windfang | — |
| Balgkammer | 2 EmberEater |
| Düse | 2 AshRunner |
| Rinne | 2 ForgeGuardian |
| Massel I | 3 EmberEater |
| Massel II | 2 EmberEater, 1 ForgeGuardian |
| Einbruch | 4 AshRunner, 3 EmberEater |
| Gichtbühne | 2 EmberEater, 1 ForgeGuardian (wandernd) |
| Hammerwerk | Siegelwächter |
| Essenkern | Kernwächter |
| Bindungskammer | Ignivar (fangbar, kein Kampfgegner) |

Summe: 12 EmberEater, 6 AshRunner, 4 ForgeGuardian, 1 Siegelwächter,
1 Kernwächter = **24**, exakt wie `EidraForgePopulationRules.TotalEnemyCount`.

Damit bleiben alle Balancewerte unverändert gültig: 45 Marken mit
Siegelwächter, 40 ohne (`EidraForgeBalance.TotalMarks`), 1036 EP pro Lauf
(`TotalExperience`). Es ist eine Umverteilung, keine Aufstockung —
`EidraForgePopulationRules` und `EidraForgeBalance` werden nicht angefasst.

Die Dichte variiert bewusst: die Düse ist ein Zwei-Gegner-Hinterhalt, der
Einbruch ein Nest, die Galerie hat wandernde Patrouillen statt stehender
Ankerpunkte.

### Truhen

| Truhe | Familie | Ort |
|---|---|---|
| `forge.supply.01` | Supply | Massel I |
| `forge.supply.02` | Supply | Massel II |
| `forge.supply.03` | Supply | Ring West |
| `forge.optional.01` | Optional | Bindungskammer |
| `forge.optional.02` | Optional | Ring Ost |
| `forge.elite.01` | Elite | Hammerwerk, hinter dem Siegelwächter |
| `forge.completion.01` | Completion | Ring Nord, hinter dem Abstich |

3 Supply, 2 Optional, 1 Elite, 1 Completion — die Verteilung, die
`EidraForgeContainerRules.HasCanonicalPopulation` verlangt. Dazu im Windfang
unverändert die drei Belohnungstruhen und der Bergungsbehälter.

Jeder optionale Bereich zahlt in einer eigenen Währung: der Einbruch in
Dauerhaftigkeit, das Hammerwerk in Beute, die Bindungskammer in einem Eidra.

---

## 6. Der Einbruch als Sonderraum: der Formwall

Der Einbruch bekommt statt einer Truhe einen **Bauplatz**. Wer ihn
ausbaut, macht die Schmiede in dem Punkt wieder betriebsfähig, an dem sie
gestorben ist.

**Bedingung.** Der Bauplatz ist gesperrt, solange die vier AshRunner im
Schutt leben. Erst räumen, dann bauen — damit ist der Raum eine Aufgabe und
kein Automat.

**Kosten (einmalig).** 24 Steinblöcke, 16 Bretter, 10 Kupferbarren,
**6 Schmiedebeschläge**. Der letzte Posten ist der wichtigste: einmal Glück
nötig, danach nie wieder.

Die sechs sind bewusst gewählt und nicht als Balancehebel gedacht — als
solcher wären sie fast wirkungslos, weil sich der Wall bei zwei Beschlägen
Ertrag pro Lauf ohnehin nach drei Läufen auszahlt. Ihr Zweck ist die
Abwägung: **Kosten und Ertrag sind dieselbe Währung.** Ein Eisenteil kostet
genau 2 Beschläge, also gibt man für den Wall drei Rüstungsstücke auf, um
später verlässlich zu bekommen, was man bisher erwürfelt hat. Bei zwei
Beschlägen wäre das keine Entscheidung, sondern eine Verzögerung um ein
einziges Teil.

Die Massenmaterialien bleiben, wie sie sind. Stein, Bretter und Kupfer
wachsen in der Basis nach; sie tragen das Gefühl eines Bauvorhabens, aber
das Tor sind die Beschläge — und es soll klar erkennbar eines sein.

**Ertrag.** 2 Schmiedebeschläge pro Lauf, garantiert, aus dem erstarrten
Auslauf gegossen. Sie werden am fertigen Formwall abgeholt: einmal pro Lauf
benutzbar, danach bis zum nächsten Lauf erschöpft. Wer den Einbruch in
einem Lauf auslässt, verliert den Ertrag dieses Laufs — er sammelt sich
nicht an. Zum Vergleich: über die Truhen liefert ein Lauf im Mittel
rund 0,45 Beschläge (10 % aus zwei optionalen, 25 % aus der Elitetruhe).
Ein Eisenteil kostet 2 Beschläge, die volle Eisenausrüstung 20 — mit dem
Formwall also zehn Läufe statt vierzig. Die Baukosten sind nach dem ersten
Lauf wieder drin.

**Einstufig.** Kein Ausbaupfad in dieser Fassung.

**Persistenz.** Der Zustand überlebt Läufe und bezahlte Resets, analog zu
`IgnivarCaptured` und `FirstCompletionGranted` in
`EidraForgeDungeonService.BeginRun`.

**Sichtbare Wirkung.** Vor dem Bau: Gerüststangen, leeres Materialgestell,
ein abgesteckter Grund von 4 × 3. Nach dem Bau: stehende Formwand mit
laufender Gießrinne und eigenem Warmlicht. Der Raum wird sichtbar heller.

---

## 7. Höhen und Kamera

Die Zonenkamera ist orthografisch und fest: Offset (−8,3 \| 15 \| −8,3),
Drehung 52°/45°, Größe 7,4 (`Zone_EidraForge.asset`). Der Blick geht
immer aus Südwesten nach Nordosten.

**Keine Decken.** Bei fester Draufsicht verdeckt eine Decke genau das, was
man sehen soll. Die Enge kommt stattdessen aus:

- **Wandhöhe 5,5** statt bisher 3,2, mit nach innen geneigter, gebrochener
  Krone. Bei 52° füllt eine 5,5er Wand rund ein Viertel der Bildhöhe.
- **Stirnwände** an Nord- und Südseite jedes Raums — die fehlen bisher
  vollständig.
- **Wandschatten**: das Richtungslicht steht flach (rund 30° über dem
  Horizont, aus Westsüdwest) und wirft acht bis zehn Einheiten lange
  Schrägschatten über die Böden. Das ist das Dunkel, das sonst die Decke
  gäbe.
- **Balken** über Rinne und Galerie auf Höhe 4,5 als einziges Element über
  dem Kopf. Sie werfen echte Schatten, weil das Richtungslicht als einzige
  Quelle schattenfähig ist.

**Platzierungsregel aus dem Kamerawinkel:** hohe Objekte an Nord- und
Ostwände (dort sind sie Kulisse), niedrige nach Südwesten (dort verdecken
hohe Objekte die Spielfigur).

**Weitere Höhen:** Grube 6 tief, Esse 10 hoch, Windrohr auf Wandkonsolen
bei 3,5.

Die Essenhöhe ist eine Korrektur aus der Referenzarbeit: ursprünglich waren
6 Einheiten bündig mit der Galerie geplant, damit man quer über die
Feueröffnung schaut. Bei 52° verdeckt der nahe Grubenrand eine solche
Öffnung, und die bündige Oberkante wäre nur eine flache Achteckfläche am
Boden. Eine Esse, die drei bis vier Einheiten über die Galerie aufragt, hat
ihre Öffnung frei im Bild.

---

## 8. Objekte

Referenzblätter: `Reference/Verlassene_Schmiede/`, Auftrag in
`GRAFIKAUFTRAG_SCHMIEDE_REFERENZBLAETTER.md`. Alle Objekte entstehen als
Profilstapel mit `EidrenMeshFactory` (`Loft` mit Rect/Oct, `TaperedBox`,
`Wedge`), vertexgefärbt, ohne Texturen.

**Zu beachten:** `TaperedBox` und `Wedge` wachsen von der Basisfläche bei
y=0 nach oben, sie sind nicht zentriert.

### Leitmotiv

Das **Windrohr** läuft auf Wandkonsolen durch jeden Raum: vom Blasebalg
unter der Wandkrone entlang, über der Gießhalle verzweigt, im Einbruch
aufgerissen, in der Esse als Düsenstein endend. Ein Objekt, das man überall
wiedersieht und das immer nach vorn zeigt. Bögen entstehen allein aus dem
seitlichen Versatz der Profilringe, ohne Rotation.

### Bauteile (wiederkehrend)

| Objekt | Maße | Bauweise |
|---|---|---|
| Windrohr | ⌀ 1,2, Höhe 3,5 | Oct-Loft, offen, Bögen über `offsetX`/`offsetZ` |
| Wandkonsole | 0,4 → 0,25 | TaperedBox, alle 8 Einheiten |
| Schlackenhaufen | 1,5–2,5 breit, 0,8 hoch | Oct-Loft, 3 Ringe, dunkler und glasiger als Mauerwerk |
| Formkasten | 1,8 × 1,2 × 0,6 | Rect-Loft, in Zweier- und Dreierstapeln |
| Kohlenpfanne | ⌀ 0,9, Höhe 1,1 | Oct-Loft-Schale auf drei TaperedBox-Beinen |
| Glutader | 2–5 lang | Segmente in Bodenrissen, Glut-Material |

### Hauptobjekte

| Raum | Objekt | Maße |
|---|---|---|
| Windfang | zerbrochener Erzkarren, ein Rad ab | — |
| Balgkammer | Blasebalg, flacher Keil, eine Hälfte eingesackt | 8 lang, 3,5 breit, 3 hoch |
| Düse | Düsenstein, der Gang führt hindurch | 6 breit, 4 hoch, Öffnung 2,5 |
| Rinne | drei Stege quer, darunter erstarrtes Rinnenmetall | Stege auf 4,5 |
| Massel I/II | je drei Formbetten, exakt parallel | 4 × 1,2 × 0,4 |
| Einbruch | geborstener Formwall, Deckentrümmer, Auslauf, Bauplatz | Bauplatz 4 × 3 |
| Gichtbühne | Geländer an der Grubenkante, zwei Gichtkübel an Auslegern | Kübel ragen frei über die Grube |
| Bindungskammer | drei Eisenreife hochkant im Boden | ⌀ 2,5 |
| Hammerwerk | Schwanzhammer, mitten im Schlag stehengeblieben | Gerüst 4 hoch |
| Essenkern | Esse, achteckig, verjüngt, drei Eisenbänder | 8 breit, 10 hoch |

Die Esse als Profilstapel (aus der Probe, bewährt):

| Ring | Höhe | Breite |
|---|---|---|
| 1 | 0,0 | 8,0 |
| 2 | 1,8 | 7,6 |
| 3 | 3,6 | 6,8 |
| 4 | 5,4 | 5,6 |
| 5 | 7,2 | 4,4 |
| 6 | 8,6 | 4,0 |
| 7 | 9,5 | 3,6 |

Eisenbänder als flache Oct-Lofts mit 0,3 Überstand bei Höhe 1,6 / 3,8 / 8,3,
Deckplatte bei 9,5.

---

## 9. Licht

Alle Werte aus `ForgeGlowProbe` gemessen. Das Projekt rendert im
Gamma-Farbraum, HDR ist an, aber **kein Volume-Profil enthält
Nachbearbeitung** — es gibt kein Bloom. Glut entsteht aus Farbe und
Kontrast, nicht aus Abstrahlung.

### Grundwerte

| Größe | Wert |
|---|---|
| Umgebungslicht (flach) | 0,10 / 0,105 / 0,13 |
| Richtungslicht | Intensität 0,8, Farbe 0,72 / 0,78 / 1,0, rund 30° aus Westsüdwest |
| Wandalbedo unten | 0,34 / 0,30 / 0,31 |
| Wandalbedo oben | 0,27 / 0,24 / 0,25 |
| Bodenalbedo | 0,30 / 0,27 / 0,28 |
| Requisitenstein | 0,32 / 0,29 / 0,30 |
| Eisen | 0,15 / 0,13 / 0,14 |
| Rost, Eisenbänder | 0,42 / 0,12 / 0,025 |
| Glut | 1,0 / 0,42 / 0,06 |
| Warmlicht der Esse | Punkt, Reichweite 18, Intensität 7, Farbe 1,0 / 0,5 / 0,16 |
| Kohlenpfannen | Punkt, Reichweite 7, Intensität 3,4 |

**Die Albedowerte sind zweimal korrigiert worden, und die zweite Korrektur ist
die wichtigere.** Die ursprünglichen Forge-Materialien lagen bei 0,12 und waren
unlesbar. Die erste Korrektur auf 0,22 entstand in `ForgeGlowProbe` — dort stand
aber ein einzelnes Objekt unter starkem Richtungslicht nah an der Kamera. In
einem Raum, in den die 5,5 hohe Wand ihren Schatten wirft und nur ein schwaches
Punktlicht reicht, gilt diese Messung nicht mehr: Boden und Wände blieben
schwarz.

`ForgeLichtProbe` hat den Grund mit Zahlen geklärt. Ein Punktlicht hebt eine
Fläche mit Albedo 0,5 um das **Vierzehnfache** (0,0118 auf 0,1691) und damit
stärker als der eingebaute URP-Lit-Shader im selben Aufbau (7,7-fach). Der
Zusatzlichtpfad im Weltshader arbeitet also einwandfrei — dunkel blieb das
Verlies, weil die Flächen fast nichts zurückwarfen. Licht kann nicht sichtbar
machen, was kein Licht reflektiert.

**Lehre für spätere Messungen:** ein Messwert gilt nur unter den Bedingungen,
unter denen er entstanden ist. Albedo, das unter Richtungslicht stimmt, stimmt
im Schatten nicht.

Der Feinabgleich der Intensitäten steht noch aus. Für flüssiges Spielen ist das
Verlies weiterhin dunkel; die Ursachen sind aber geklärt und die Wirkungskette
belegt.

### Glut

Selbstleuchtende Flächen bekommen das Material
`M_EidrenWorld_VertexGlow.mat` (Shader `Eidren/World/VertexGlow`), das die
Vertexfarbe unbeleuchtet ausgibt. Betroffen: Feueröffnung der Esse,
Glutadern, Kohlenpfannen, der erstarrte Auslauf im Einbruch, die Gießrinne
des fertigen Formwalls.

Meshes für dieses Material mit `kontaktAo: false` bauen, sonst bleicht die
eingebackene Kontaktabdunklung die Glut an den Rändern aus.

**Glutflächen brauchen kein eigenes Punktlicht.** Die Probe zeigt sie ohne
jede Lichtquelle als auffälligste Elemente im Bild. Licht wird nur dort
gesetzt, wo die Umgebung mitgefärbt werden soll — an der Esse, an
Kohlenpfannen, am fertigen Formwall.

### Grenzen des Renderers

- `m_AdditionalLightShadowsSupported: 0` — nur das Richtungslicht wirft
  Schatten. Punktlichter geben Farbe, das Richtungslicht gibt Form.
- `m_AdditionalLightsPerObjectLimit: 4` — höchstens vier Zusatzlichter pro
  Objekt, und die Auswahl fällt pro Objekt. Deshalb müssen die Böden in
  **Kacheln von 4 × 4** zerlegt werden statt ein Quader pro Raum zu sein;
  grob 160 Kacheln für den ganzen Dungeon, statische Geometrie.
- Regel: kein Bodenstück darf von mehr als vier Punktlichtern erreicht
  werden. Im Essenkern belegt die Esse bereits einen Platz auf jeder Kachel.

### Farbregel

Höchstens drei Lichtfarben im Bild. Orange und Nahezu-Schwarz sind der
Normalfall. **Blau kommt genau einmal vor**: durch die Bresche im Einbruch.
Dazu dunkler Nebel mit kurzer Reichweite.

Große Wandflächen bekommen kein Verlaufslicht — flat shading erzeugt pro
Fläche einen Ton. Die 5,5 hohen Wände werden deshalb in Segmente zerlegt,
deren Vertexfarben leicht gestaffelt sind. Das ersetzt den Verlauf durch
abgestufte Bänder und kostet einen Profilring mehr.

---

## 10. Abweichungen vom bestehenden Builder

`EidraForgeSceneBuilder` erzeugt heute Böden, zwei Seitenwände pro Raum und
Korridore. Der Neubau braucht darüber hinaus:

1. **Stirnwände** an Nord- und Südseite jedes Raums
2. **Wandhöhe 5,5** statt 3,2, mit gebrochener Krone und Segmentierung
3. **Bodenkacheln 4 × 4** statt eines Quaders pro Raum
4. **Eigene Beleuchtung** statt der aus `Zone_EmberRuins` geerbten
5. **Glut-Material** für selbstleuchtende Teile — vorhanden seit dieser Etappe
6. **Albedowerte laut Abschnitt 9** statt 0,12 in den Forge-Materialien

Nicht angefasst werden `EidraForgePopulationRules`, `EidraForgeBalance` und
`EidraForgeContainerRules`. Der Formwall kommt als eigener Zustand hinzu.

### Drei Randbedingungen, die beim Bau nicht verhandelbar sind

Alle drei haben beim ersten Anlauf Zeit gekostet und stehen hier, damit sie
niemand zweimal entdecken muss.

**Rampen müssen echte Schrägen sein, keine Stufen.** Der NavMesh-Agent hat
Radius 0,5 und erodiert von jeder Kante einen halben Meter — ein Auftritt von
einem Meter behält null Breite, die Stufen bleiben unverbunden. Längere
Auftritte sprengen bei sechs Metern Gefälle die Kletterhöhe von 0,75. Nur eine
durchgehende Schräge hält beide Grenzen ein: 10 Einheiten Lauf auf 6 Einheiten
Gefälle, rund 31 Grad. Die Treppen belegen deshalb die volle Schenkelbreite.

**`WalkableGround` muss vom NavMesh ausgenommen werden** (`NavMeshModifier` mit
`ignoreFromBuild`). Die Fläche spannt sich als geschlossener Kasten über den
ganzen Grundriss, ihre Oberkante liegt auf 0; gebacken wird daraus eine flache
Laufebene über Grube und Rampen, und die Essenkammer ist unerreichbar. Der
Collider muss bleiben — `ZoneController` verlangt die Referenz per `Require`,
und `LootDropPositionResolver` raycastet darauf.

**Wände dürfen an Rampen nicht entstehen, auch nicht von der Nachbarfläche
aus.** Die Stützwand eines Ringschenkels läuft sonst über die volle Länge und
mauert den Rampenausgang zu. Die Prüfung muss beide Seiten der Kante ansehen,
nicht nur die Nachbarzelle.

Belegt sind alle drei durch den Wegtest in `EidraForgeSceneTests`: er sucht
einen vollständigen Pfad vom Windfang bis in die Grube. Ohne ihn war die
Bossarena unerreichbar, ohne dass ein einziger Test es bemerkte — alle
vorhandenen prüften nur, *dass* ein NavMesh existiert, nicht *ob* man irgendwo
hinkommt.

---

## 11. Nicht Teil dieser Fassung

- mehrstufiger Formwall mit steigendem Ertrag
- Bedienung des Formwalls durch Eidra (`eidraFactor` der Gebäudedefinitionen)
- Einbahn-Abkürzung vom Essenkern zurück zum Windfang
- volle Decken mit kameraabhängiger Ausblendung
- begehbarer Schutt im Einbruch

---

## 12. Offene Punkte

- **Glutadern als Form.** In der Probe lesen sie sich als gleichmäßig
  breite Leuchtstreifen, nicht als Glut in Rissen. Sie brauchen schmalere,
  verzweigte Segmente mit gestaffelter Helligkeit, gesetzt in eine dunklere
  Vertiefung.
- **Vier Objekte ohne Bauvorlage.** Geländer, Deckenbalken, Bindungsreife
  und der Formwall-Bauplatz kommen bisher nur in den Stimmungsbildern vor.
- **Ringgröße.** 40 × 36 Außenmaß kann weitläufig oder leer wirken. Beim
  Bau prüfen und eher verkleinern als vollstopfen.
- **Blasebalg-Silhouette.** Das Referenzobjekt steht unversehrt und
  symmetrisch; die eingesackte Hälfte entsteht erst beim Bau durch Kippen
  der Deckplatte.
