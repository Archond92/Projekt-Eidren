# Fixsammlung v0.3.1

**Status:** Sammlung am 17.08.2026 auf Nutzerentscheid **geschlossen** —
19 Einträge (F31-001 bis F31-020; F31-005 entfallen, Nummer nicht neu
vergeben). Noch kein Eintrag ist umgesetzt; die Umsetzung wird gesondert
beauftragt.

**Bewusst nicht aufgenommen** (angesprochen, aber ohne Auftrag geblieben):
Änderungen an den Waffenreichweiten, ein Gnadenfenster für den Kombo-Abbruch
beim Ausweichen, sowie die Gegner-EP-Rechnung als Ergänzung in F31-006.

Die Nummerierung **F31-xxx** ist bewusst getrennt von der abgeschlossenen
`FIXSAMMLUNG_V0.2.1.md` (F-001 bis F-014) und der abgeschlossenen
`WUNSCHSAMMLUNG_20260814.md` (W-001 bis W-010, T-001). Wo ein Eintrag einen
dort zurückgestellten Punkt berührt, ist das vermerkt.

**F31-005 ist entfallen** (16.08.2026). Der Eintrag schlug vor, das Leveltempo
über höhere Ertragswerte zu beschleunigen; auf Entscheidung des Nutzers wird
stattdessen das Questsystem aus F31-006 eingeführt. Die Zahlengrundlage ist dort
übernommen. Die Nummer wird nicht neu vergeben.

**Basis:** v0.3.0-dev (Commit `370396f`, Tag `v0.3.0-dev`), Branch
`codex/v0.3-release`.

---

## F31-001 — Figur verschwindet hinter Bäumen und Wänden

**Bereich:** Kamera, Weltdarstellung, Spielerfigur

**Beobachtung (16.08.2026):** Steht die Figur hinter einem Objekt — Baum im
Freien, Wand oder Bauteil im Verlies und in der Schmiede — ist sie nicht mehr
zu sehen. Das Objekt soll sich in solchen Fällen **stellenweise** ausblenden,
sodass die Figur sichtbar bleibt.

> **Befund am 16.08.2026 korrigiert.** Die erste Fassung dieses Eintrags
> behauptete, es gebe keine Sichtlinien-Ausblendung. Das war falsch — die
> zugrunde liegende Suche war abgeschnitten und übersah
> `ActorOcclusionTransparency`. Der korrigierte Befund steht unten; die
> ursprünglich vorgeschlagene Shader-Lösung ist damit hinfällig.

**Befund (16.08.2026, Codestand v0.3.0-dev):**

- **Eine Sichtlinien-Ausblendung existiert bereits und ist vollständig
  implementiert:** `ActorOcclusionTransparency.cs` (327 Zeilen,
  `Eidren.Presentation`). Sie zieht pro Frame für **jeden registrierten
  Akteur** einen `SphereCast` (Radius 0,18) von der Kamera zur Figurenmitte
  (48 % der Körperhöhe) und blendet jeden getroffenen Verdecker über
  Materialklone weich aus (Alpha auf `_BaseColor` bzw. `_Color`,
  `SpriteRenderer` über die Farbe).
- **Warum sie bei Bäumen nicht greift — die Verdeckerbedingung ist zu eng.**
  `IsOccluder` lässt nur zwei Fälle zu:

  ```
  IsLargeOccluder(bounds)  =  bounds.size.y >= 1,35  UND  max(x, z) >= 1,6
        oder
  Komponente OcclusionFadeTarget am Objekt oder einem Elternteil
  ```

  Der Baum-Collider (`Tree.prefab`) ist eine Kapsel mit **Radius 0,6, Höhe
  2,4** — also 2,4 hoch (Bedingung erfüllt), aber nur **1,2 breit**. Er
  verfehlt die Breitenschwelle von 1,6 um 0,4 m und fällt deshalb durch. Es
  ist genau **eine Zahl**, an der die Baumverdeckung scheitert.
- **`OcclusionFadeTarget` ist projektweit an genau zwei Prefabs vergeben:**
  `BLD_Wall_L01` und `BLD_Door_L01` — beides Basisbau. Ressourcenknoten,
  Verlies- und Schmiedegeometrie tragen den Marker nicht und sind damit auf
  die Größenschwelle angewiesen.
- Zusätzlich existiert die Dachlösung: `BuildingRoofView.cs` blendet die
  **Dächer der Basisbau-Räume**
  aus. `LateUpdate` prüft je Raum `Room.Contains(spielerPosition)` und fährt
  das Dach über `FadeTowards` mit 4,5/s auf Alpha 0 herunter, sobald die Figur
  in einer Zelle des Raums steht. Das ist **positionsbasiert** („Figur ist im
  Raum"), nicht sichtlinienbasiert („Objekt steht im Weg"), und greift nur für
  Dachkacheln der Heimatbasis. Bäume, Verlies- und Schmiedegeometrie sind
  davon nicht erfasst.
- Die Kamera steht orthografisch mit fester Drehung 52°/45°
  (`IsometricCamera._fixedRotation`). Die Verdeckungsrichtung ist damit
  konstant — es gibt keine Kameradrehung, mit der man ein Objekt umgehen
  könnte.
- Alle statischen Weltobjekte aus der `EidrenMeshFactory` hängen an **einem
  einzigen** Material: `Assets/_Game/Art/World/Materials/M_EidrenWorld_VertexLit.mat`
  mit dem Shader `Eidren/World/VertexLit`
  (`Assets/_Game/Shaders/World/EidrenWorldVertexLit.shader`). Der Shader ist
  rein opak (`RenderType = Opaque`, `Queue = Geometry`), arbeitet mit
  Vertexfarben ohne Texturen und gibt Alpha fest als `1.0` zurück; ein
  Cutoff- oder Alpha-Kanal ist nicht vorgesehen. Pipeline ist URP 17.3.0 mit
  eigenem Renderer (`Eidren_UniversalRenderer`).
- Dass es **ein geteiltes** Material ist, ist für die Umsetzung der
  entscheidende Punkt: Ein Materialwechsel oder Alpha-Setzen am Material
  träfe schlagartig jedes Weltobjekt. Per-Objekt-Ausblendung bräuchte
  Materialklone je Renderer (der Weg, den `BuildingRoofView` für die
  Dachkacheln geht) — bei der Menge an Weltobjekten pro Zone ist das teuer.

**Stoßrichtung — kein Neubau, sondern die Erkennung erweitern.** Das System
steht; es erfasst die betroffenen Objekte nur nicht. Zwei Wege, die sich
ergänzen:

1. **`OcclusionFadeTarget` an die betroffenen Prefabs** — Bäume, Hartholzbäume,
   die übrigen Ressourcenknoten sowie Verlies- und Schmiedegeometrie. Das ist
   der Weg, den die Basisbau-Wände bereits gehen, und er ist ausdrücklich als
   Umgehung der Größenschwelle vorgesehen. Vorteil: gezielt, ohne Nebenwirkung
   auf andere Objekte. Nachteil: eine Liste, die gepflegt werden muss —
   `BuildingRoofContentBuilder` erzwingt den Marker deshalb per Test für
   Dachkanten (`EveryEdgePrefabIsAnOcclusionFadeTarget`); dasselbe Muster wäre
   für Ressourcen- und Verliesprefabs nötig.
2. **Die Größenschwelle lockern.** `IsLargeOccluder` verlangt Höhe **und**
   Breite. Für aufrechte, schlanke Verdecker wie Bäume ist die Breitenforderung
   von 1,6 m die falsche Bedingung — ein 2,4 m hoher Baum verdeckt die Figur
   vollständig, egal wie dünn er ist. Sinnvoller wäre eine Bedingung, die
   allein auf die Höhe abstellt, oder eine deutlich niedrigere Breitenschwelle
   (der Baum liegt bei 1,2).

Weg 2 ist die kleinere Änderung und trifft alle schlanken Verdecker auf einmal;
Weg 1 ist der genauere. Empfehlung: Schwelle anpassen **und** die Verliesteile
markieren, falls deren Collider aus anderen Gründen durchfallen.

**Zwingend vorher zu prüfen:** Ob der Fade an Weltobjekten überhaupt sichtbar
wird. `ActorOcclusionTransparency` setzt Alpha auf `_BaseColor` bzw. `_Color`.
Der Weltshader `Eidren/World/VertexLit` kennt **keine** dieser beiden
Properties (nur `_Tint`), ist als `Opaque` in der Geometry-Queue deklariert und
gibt Alpha fest als `1.0` zurück. Bei den Basisbau-Wänden funktioniert der Fade,
weil sie an anderen Materialien hängen. Es kann also sein, dass die Bäume nach
einer Lockerung der Schwelle zwar erkannt, aber trotzdem nicht durchsichtig
werden — dann braucht der Weltshader zusätzlich einen transparenzfähigen Pfad
oder der Fade einen Materialtausch. **Das ist der eigentliche Risikopunkt des
Eintrags und vor jeder Schätzung zu klären.**

**Randnotizen:**

- Die Kamera steht orthografisch mit fester Drehung 52°/45°. Die
  Verdeckungsrichtung ist konstant — es gibt keine Kameradrehung, mit der man
  ein Objekt umgehen könnte. Eine Ausblendung ist also die einzige Lösung.
- Der Wunsch lautete „stellenweise ausblenden". Das bestehende System blendet
  den **ganzen** Verdecker weich aus, nicht nur den Bereich um die Figur. Ob
  das genügt, ist eine Optikfrage — es ist der etablierte Weg im Projekt und
  funktioniert bei den Basisbau-Wänden sichtbar. Vorschlag: erst die Erkennung
  reparieren, dann im Bild entscheiden, ob eine teilweise Ausblendung
  überhaupt noch gebraucht wird.
- `BuildingRoofView` (Raum-Fade für Dächer) bleibt davon unberührt und sollte
  so bleiben.

**Status: gesammelt am 16.08.2026, nicht umgesetzt. Befund am selben Tag
korrigiert — Aufwand deutlich kleiner als zunächst angenommen.**

---

## F31-002 — Rüstungs-Icons: Werkbank und Inventar zeigen Platzhalter statt Rüstungsteile

**Bereich:** Werkbank (Crafting-UI), Inventar, Item-Icons

**Beobachtung (16.08.2026):** Die Stoffrüstung hat in der Werkbank keine
richtigen Bilder.

**Frage des Nutzers vorab beantwortet — ja, die Stoffrüstung ist 3D
modelliert, gleichrangig mit Kupfer und Eisen.** Die `Wanderer.glb` enthält
alle drei Stufen in allen vier Slots (12 Slot-Meshes):
`Helm_Stoff/_Kupfer/_Eisen`, `Harnisch_Stoff/…`, `Haende_Stoff/…`,
`Beine_Stoff/…` (`README_Wanderer3D.md`, verdrahtet in
`Wanderer3DPlayerBuilder.cs` Zeile 33, geprüft von `Wanderer3DAssetTests` und
`PlayerPrefabTests`). **Am 3D-Modell fehlt nichts** — das Problem liegt
ausschließlich bei den 2D-Icons.

**Ursache (diagnostiziert am 16.08.2026):** Zwei getrennte Fehler, die
zusammen dasselbe Bild ergeben.

1. **Das Rezept-Icon ist hart auf das Platzhalter-Präfix verdrahtet.** Die
   Werkbank zeigt `recipe.Icon` (`CraftingRecipeButtonView.cs:64`,
   `CraftingWindow.cs:389`), und `CraftingRecipeTable.cs:264` baut den
   Icon-Pfad so zusammen:

   ```
   LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/ITEM_TMP_" + iconItemAssetName + ".png")
   ```

   Das Präfix `ITEM_TMP_` steht fest im Code. Die vier Stoffrezepte übergeben
   `WandererHood/Coat/Bracers/Legs` und bekommen damit zwangsläufig die
   `ITEM_TMP_*`-Dateien. **Die fertigen Icons sind über diesen Pfad gar nicht
   erreichbar.** Es ist derselbe Fehler, der in `ItemContentTable.Material`
   bereits gefunden und behoben wurde — dort steht der Kommentar „icon
   WIRKLICH durchreichen: die alte Fassung ignorierte den Parameter und baute
   stur `ITEM_TMP_` + assetName" (Zeile 98–102). Im Rezeptbuilder ist er noch
   drin.

2. **Die vier verdrahteten `ITEM_TMP_Wanderer*.png` sind keine Item-Icons.**
   Alle vier am 16.08.2026 einzeln angesehen:

   | Datei | zeigt tatsächlich |
   |---|---|
   | `ITEM_TMP_WandererHood.png` | ganze Spielfigur mit Hammer, **grüner Greenscreen-Hintergrund** |
   | `ITEM_TMP_WandererCoat.png` | ganze Spielfigur mit zwei Dolchen, **grüner Hintergrund** |
   | `ITEM_TMP_WandererBracers.png` | **UI-Rahmenleiste auf Magenta** — ein völlig fremdes Asset |
   | `ITEM_TMP_WandererLegs.png` | **Charakter-Turnaround-Blatt** mit vier Ansichten auf Grau |

**Die richtigen Bilder liegen bereits im Projekt und sind unbenutzt.** Im
selben Ordner liegen vier fertige, freigestellte Einzelteil-Icons mit
Transparenz (alle vier angesehen, alle brauchbar): `ITEM_WandererHood.png`
(blaue Kapuze mit Lederborte), `ITEM_WandererCoat.png` (Stoffmantel mit
Schärpe), `ITEM_WandererBracers.png` (Handschuhe mit Lederwicklung),
`ITEM_WandererLegs.png` (Hose mit Stiefeln). Sie unterscheiden sich vom
Platzhalter nur durch das fehlende `TMP_` im Namen — und werden von keiner
Stelle referenziert.

> **Korrektur bei der Umsetzung (17.08.2026):** Der folgende Absatz
> überschätzte den Schaden. Der `Armor`-Helfer ignoriert seinen
> Icon-Parameter und baut stur `ITEM_TMP_` + Assetname — für Kupfer und
> Eisen ergibt das **zufällig die richtige Datei** (`ITEM_TMP_CopperHelmet`
> usw.), die Metall-Assets zeigten im Spiel also immer die richtigen Bilder.
> Die Tabelle log, nicht die Assets; der Rot-Test
> `Ruestung_TeiltKeinIconUeberMaterialstufen` hat das aufgedeckt. Falsch
> verdrahtet waren nur die vier Stoffteile und die vier Stoffrezepte;
> zusätzlich waren die acht toten Metall-Parameter irreführend und sind bei
> der Umsetzung auf die tatsächlichen Dateien gestellt worden.

**Der Fehler trifft auch Kupfer und Eisen.** `ItemContentTable.cs` Zeile 85–92
verdrahtet **alle acht** Kupfer- und Eisenrüstungsteile ebenfalls auf die vier
`ITEM_TMP_Wanderer*`-Platzhalter:

| Item | verdrahtet auf | vorhanden wäre |
|---|---|---|
| Kupferhelm / Eisenhelm | `ITEM_TMP_WandererHood` | `ITEM_TMP_CopperHelmet` / `ITEM_TMP_IronHelmet` |
| Kupferharnisch / Eisenharnisch | `ITEM_TMP_WandererCoat` | `ITEM_TMP_CopperChest` / `ITEM_TMP_IronChest` |
| Kupferhandschuhe / Eisenhandschuhe | `ITEM_TMP_WandererBracers` | `ITEM_TMP_CopperGloves` / `ITEM_TMP_IronGloves` |
| Kupferbeinschutz / Eisenbeinschutz | `ITEM_TMP_WandererLegs` | `ITEM_TMP_CopperLegs` / `ITEM_TMP_IronLegs` |

Die acht Kupfer-/Eisen-Icons existieren als Dateien und sind **keine**
Platzhalter, trotz des `TMP_` im Namen: `ITEM_TMP_CopperHelmet.png` zeigt
einen sauber freigestellten, patinierten Kupferhelm (angesehen). Damit zeigen
derzeit **alle zwölf Rüstungsteile** eines von vier falschen Bildern —
Ganzfigur, Turnaround-Blatt oder UI-Leiste.

**Stilabgleich der drei Rüstungssätze (16.08.2026, alle zwölf Icons
angesehen und vermessen):** Die vier Stoff-Icons passen stilistisch zu Kupfer
und Eisen — sie stammen sichtbar aus derselben Produktion: gerenderte
Dreiviertelansicht, sauber freigestellt auf Transparenz, weiches Oberlicht von
links, materialgetreue Oberflächen, brauner Lederanteil als gemeinsamer Nenner
in allen drei Sätzen, keine Outline, kein UI-Rahmen. Zwei messbare
Unterschiede:

| | Auflösung | Motiv füllt Rahmen (Ø) | Rand (min) |
|---|---|---|---|
| Stoff (`ITEM_Wanderer*`) | **512×512** | 78,5 % | 7,0–13,5 % |
| Kupfer (`ITEM_TMP_Copper*`) | 256×256 | 83,7 % | 6,2–10,5 % |
| Eisen (`ITEM_TMP_Iron*`) | 256×256 | 87,9 % | 4,7–5,9 % |

- **Auflösung:** Die Stoff-Icons sind mit 512×512 doppelt so groß wie die
  Metall-Icons. Kein Stilbruch, aber die Importeinstellungen sollten beim
  Umbau angeglichen werden. Die Weichheit der Freistellungskante ist nach
  Auflösungskorrektur in allen drei Sätzen identisch (Stoff 0,9–1,0 % der
  Fläche bei doppelter Kantenlänge entspricht den 1,6–3,2 % der Metallsätze).
- **Motivgröße:** Stoffteile sitzen etwas kleiner im Rahmen als Eisenteile
  (78,5 % gegen 87,9 %). Nebeneinander in der Werkbank wirkt Stoff dadurch
  eine Spur zurückgesetzt. Die Streuung **innerhalb** jeder Familie ist
  allerdings ähnlich groß (Stoffarmschienen 71,5 % gegen Stoffmantel 84,8 %) —
  die Motivgröße ist also generell nicht normiert. Feinschliff, kein Blocker.

**Für den Fix heißt das: keine neue Grafik nötig.** Alle zwölf Icons sind
vorhanden und brauchbar; es geht ausschließlich um die Verdrahtung.

**Stoßrichtung (noch nicht beauftragt):**

- `CraftingRecipeTable.cs:264` den Icon-Namen durchreichen statt `ITEM_TMP_`
  fest anzuhängen — analog zu der Korrektur, die in `ItemContentTable.Material`
  schon gemacht wurde. Danach in der Rezepttabelle die vier Stoffrezepte auf
  die fertigen `ITEM_Wanderer*`-Icons zeigen lassen.
- In `ItemContentTable.cs` die vier Stoffrüstungsteile auf `ITEM_Wanderer*`
  und die acht Kupfer-/Eisenteile auf ihre eigenen Icons umstellen.
- Test, der für **jedes** Rüstungs-Item und **jedes** Rüstungsrezept prüft,
  dass ein Icon gesetzt ist und dass kein Icon von zwei Items unterschiedlicher
  Rüstungsstufe geteilt wird. Ein reiner „Icon ist nicht null"-Test hätte
  diesen Fehler nicht gefunden.
- Die vier `ITEM_TMP_Wanderer*.png` sind Referenz- und Fremdmaterial, keine
  Item-Icons. Nach dem Umbau prüfen, ob sie noch irgendwo gebraucht werden;
  wenn nein, entfernen, damit sie nicht erneut verdrahtet werden.

**Zu klären:** Ob es für Kupfer- und Eisenrüstung überhaupt Rezepte gibt (die
Werkbank-Rezeptliste enthält nur die vier Stoffteile; ob die Metallrüstung an
der Schmiede gefertigt wird, ist nicht geprüft). Falls ja, gilt Punkt 1 dort
gleichermaßen.

**Berührt:** dieselbe Fehlerklasse wie W-004 (falsche Waffen-Icons im
Kampf-HUD) und der Nebenbefund „Eisenspeer zeigt Spitzhacken-Icon" aus
`WUNSCHSAMMLUNG_20260814.md` — dieser Nebenbefund ist weiterhin offen und
sollte in derselben Runde miterledigt werden.

**Status: gesammelt am 16.08.2026, nicht umgesetzt.**

---

## F31-003 — Kupferader sieht nach dem Abbau unverändert aus

**Bereich:** Ressourcen, Gebietsdarstellung (Folge zu W-005)

**Beobachtung (16.08.2026):** Die Kupferader zeigt nach dem Abbau immer noch
dieselbe Grafik. Wunsch: Verhalten wie beim Steinvorkommen.

**Befund (16.08.2026) — die Kupferader ist bereits exakt wie das
Steinvorkommen konfiguriert.** Die gesamte Kette wurde geprüft, jede Stufe ist
korrekt:

| Prüfstelle | Kupferader | Steinvorkommen |
|---|---|---|
| `respawnMode` | 1 (`OnZoneEntry`) | 1 (`OnZoneEntry`) — identisch |
| `keepsAppearanceWhenExhausted` | **0** (W-005-Rücknahme wirksam) | Feld nicht gesetzt (= 0) |
| Aktiv-/Abgebaut-Prefab in der Definition | zwei verschiedene | zwei verschiedene |
| `activeVisual`/`exhaustedVisual` im Node-Prefab | zwei verschiedene Kinder | zwei verschiedene Kinder |

`ResourceNode.ApplyVisualState` (Zeile 323–341) schaltet damit nachweislich um:
`keep` ist false, `flag` ist false (weil `RespawnMode != None`), also
`activeVisual.SetActive(false)` und `exhaustedVisual.SetActive(true)`.

**Der Optikunterschied ist bei der Kupferader sogar größer als beim Stein.**
Aus den Prefabs ausgezählt:

| | Teile aktiv | Teile abgebaut | höchster Punkt aktiv → abgebaut |
|---|---|---|---|
| Kupferader | 13 (7 Fels + 5 Erzsplitter + Sockel) | 8 (nur Fels) | 1,00 → 0,55 |
| Steinvorkommen | 8 | 5 | 0,55 → 0,50 |

Beim Abbau fallen also alle fünf Erzsplitter weg und der Brocken sackt auf gut
die halbe Höhe — beim Stein schrumpft nur die Teilezahl, die Höhe bleibt fast
gleich.

**Damit ist „wie bei Stein umsetzen" nicht der Fix — das ist bereits der
Zustand.** Drei Kandidaten kamen für die Beobachtung in Frage; zwei sind
ausgeschlossen:

1. ~~**Getestete Fassung.**~~ Ausgeschlossen: Der Nutzer hat am 16.08.2026
   bestätigt, in der **v0.3.0-dev** vom 16.08. beobachtet zu haben. Die
   W-005-Rücknahme ist dort enthalten. (Sie entstand am 15.08. in der
   `w578`-Runde und fehlt nur in der v0.2.0-dev-Exe `d00c4535…`.)
2. ~~**Zonenwiedereintritt.**~~ Unwahrscheinlich: `OnZoneEntry` stellt die
   Ader beim erneuten Betreten wieder her, das gilt für Stein aber genauso —
   und dort wird der Wechsel erkannt.
3. **Der Unterschied wird nicht als „abgebaut" gelesen — das ist es.** Beide
   Zustände sind graue Felsbrocken; was fehlt, sind die Kupferstellen. Es geht
   also **nicht** ums Schalten, sondern um eine deutlichere Abgebaut-Optik.
   Bemerkenswert: Geometrisch schrumpft die Kupferader stärker als der Stein
   und wird trotzdem nicht als abgebaut erkannt. Der Grund dürfte sein, dass
   beim Stein die Formänderung als „Haufen kleiner geworden" lesbar ist,
   während bei der Ader ein weiterhin großer Fels stehenbleibt, dem nur die
   Erzstellen fehlen.

**Stoßrichtung:** Es liegt bereits ein eigens
gebautes Abgebaut-Mesh im Projekt, das **nirgends referenziert** wird:
`Assets/_Game/Art/Geometry/Meshes/CopperOreHero/CopperVein_DepletedRemnant.asset`.
Der ganze `CopperOreHero`-Satz (`CopperVein_Metal_0–3`, `CopperVein_Crack_0–3`,
`CopperVein_DepletedRemnant`) ist verwaist — kein Prefab, kein Asset und keine
Szene verweist darauf. Das Abgebaut-Prefab nutzt stattdessen generische
`T1_*_Fels`-Meshes. Ein sichtbar „ausgeschlagener" Rest wäre also ohne neue
Grafik erreichbar.

**Nebenbefund:** Dass ein vollständiger Hero-Mesh-Satz für die Kupferader
ungenutzt im Projekt liegt, ist unabhängig von diesem Eintrag klärungswürdig —
entweder einbauen oder entfernen.

**Vor der Umsetzung:** Beide Zustände nebeneinander rendern und ansehen. Erst
daran lässt sich entscheiden, ob es reicht, das `DepletedRemnant`-Mesh
einzubauen, oder ob die Abgebaut-Form insgesamt flacher und ausgeschlagener
werden muss. Ein bestandener Schalttest beweist hier nichts — der Schaltvorgang
funktioniert ja bereits.

**Status: gesammelt am 16.08.2026, nicht umgesetzt. Fassung geklärt
(v0.3.0-dev), Ursache eingegrenzt auf die zu geringe optische Unterscheidbarkeit.**

---

## F31-004 — Tote Fortschrittsdaten: eine Stellschraube, die keine ist

**Bereich:** Fortschritt, Erfahrungskurve (Aufräumen, kein Verhaltensfehler)

**Befund (16.08.2026):** Zwei Stellen im Fortschrittssystem sehen aus wie
wirksame Einstellungen, werden aber nie gelesen.

1. **Serialisierte Levelkurve im Asset.** `ProgressionCurve_V01.asset` trägt für
   Stufe 1 ein ausgefülltes Feld `experienceToNextLevel` mit 20 Werten in
   30er-Schritten (60, 90, 120, 150 …). Gelesen wird es nie:
   `ProgressionStageCurve.ExperienceToNextLevel` ruft ausnahmslos
   `ProgressionFormula.ExperienceToNextLevel`, also
   `auf 25 gerundet (125 + 55 × Level)` → 175, 225, 300, 350 … Wer die Werte im
   Inspector ändert, ändert nichts. Für Stufe 2 ist das Feld leer, was den
   Widerspruch zusätzlich verdeckt.

   Der Unterschied ist erheblich: Die tote Tabelle entspräche **8.970 EP** bis
   Level 24, die wirksame Formel **18.050 EP** — gut das Doppelte.

2. **`PlayerProgressionService.RecordGatheredUnits`** vergibt
   `gatheringExperiencePerUnit` (Asset: 5) EP je gesammelter Einheit. Die
   Methode ist vollständig implementiert und getestet, wird im Spielfluss aber
   **von niemandem** aufgerufen — die einzigen Aufrufer sind EditMode-Tests
   sowie `CombatHudCaptureRunner` und `WindowsPerformanceAuditRunner`.

**Das ist kein vergessener Anschluss, sondern eine verworfene Mechanik.** Die
Programmierbibel (M13.4) sagt ausdrücklich: „Sammel-EP hängen nicht am
Materialertrag." Einzelnes Abbauen soll also nichts geben; EP kommen erst, wenn
der Knoten abgeschlossen ist. Der Code widerspricht der Regel nicht im
Verhalten, aber im Bestand.

**Stoßrichtung (noch nicht beauftragt):** Beides entfernen —
`experienceToNextLevel`/`technologyPointsOnLevel` aus dem Kurven-Asset und dem
`ProgressionStageCurve`-Feldbestand, `RecordGatheredUnits` samt
`gatheringExperiencePerUnit` und den zugehörigen Tests. Wenn das Feld erhalten
bleiben soll, muss es stattdessen tatsächlich Vorrang vor der Formel bekommen —
aber nur eines von beidem, nicht beides nebeneinander.

**Warum das zählt:** Eine tote Stellschraube ist gefährlicher als eine fehlende.
Die nächste Balance-Änderung landet mit hoher Wahrscheinlichkeit genau in diesem
Feld und wirkt nicht.

**Berührt:** F31-006 — bevor Questschritte EP vergeben, muss klar sein, welche
der beiden Quellen die Levelkurve bestimmt.

**Status: gesammelt am 16.08.2026, nicht umgesetzt.**

---

## F31-006 — Tutorial als Questkette bis zum ersten Eidra

**Bereich:** Fortschritt, Spielereinstieg (Neubau, kein Fix)

**Wunsch (16.08.2026):** Ein einfaches Questsystem, das wie ein Tutorial
funktioniert und in Schritten durch den Einstieg führt — „Sammle diese
Ressource", „Baue die Axt", „Baue die Sense" und so weiter. Jeder Questschritt
gibt EP. Das Tutorial endet mit dem **Fangen des ersten Eidra** und führt
langsam über alle notwendigen Zwischenschritte dorthin.

**Warum das der Weg ist — die Zahlen dahinter (16.08.2026 ausgezählt):** Anlass
war der Wunsch, dass das Leveln schneller gehen soll. Die Prüfung ergab: Nicht
die Levelkurve ist das Problem, sondern das Verhältnis von einmaligen zu
wiederholbaren Quellen.

| Einmalquelle | Anzahl | EP |
|---|---|---|
| Rezepte (9× T0, 18× T1, 14× T2) | 41 | 2.385 |
| Gebäude (6× T0, 5× T1, 0× T2) | 11 | 615 |
| Garon erstmals besiegt | 1 | 850 |
| Eidra-Schmiede erstmals abgeschlossen | 1 | 1.400 |
| **Summe** | | **5.250** |

Dem stehen **18.050 EP** bis Level 24 und **47.775 EP** bis Level 40 gegenüber.
Alle Einmalquellen zusammen decken damit bis Level 24 nur **29 %**, bis Level 40
noch **11 %** — der ganze Rest ist Wiederholung. Bis Level 24 fehlen 12.800 EP,
das entspricht rund **1.070 Kupferadern** (12 EP je Knoten).

**Eine Questkette erzeugt genau das, was fehlt: einmalige, geführte EP in der
frühen Phase.** Sie hebt das Tempo dort, wo der Spieler neu ist, ohne die
Erträge im späten Spiel zu verzerren — und ohne die Levelkurve
`auf 25 gerundet (125 + 55 × Level)` anzutasten, die in der Bibel (M13.4)
festgeschrieben ist und die Prüfpunkte „Basisvorrat trägt bis Level 4" und
„Garon ab Level 12" trägt. Die Zielsetzung „bis zum ersten Eidra" passt dazu:
Die Bibel stellt das Fanggerät bewusst früh in T1 direkt hinter den Schmelzofen,
damit die Eidra die gesamte zweite Spielhälfte begleiten (M8.6-Begründung).

**Damit ist die Alternative — Ertragswerte hochdrehen — vom Tisch.** Sie war als
eigener Eintrag F31-005 gesammelt und wurde am 16.08.2026 zugunsten dieses
Wegs gestrichen.

**Befund zur Ausgangslage (16.08.2026):** Es gibt **kein** Quest-, Aufgaben-
oder Tutorialsystem im Projekt. Suche über alle Laufzeit- und Editor-Skripte
nach `quest`/`tutorial`: kein einziger Treffer. Auch die Programmierbibel kennt
den Begriff nicht. Das ist also ein Neubau auf leerem Feld, kein Anbau.

**Was dafür gebraucht wird (Umfangseinschätzung, nicht beauftragt):**

- **Datenmodell:** Questdefinition mit geordneten Schritten, je Schritt eine
  Bedingung und eine EP-Belohnung — als ScriptableObject im Stil der übrigen
  Kataloge (`TechnologyTree_V01`, `CraftingRecipeCatalog_V01`).
- **Fortschrittsverfolgung:** Die Bedingungen müssen an bestehende Ereignisse
  andocken. Vorhanden und nutzbar sind die Aufrufpunkte aus
  `PlayerProgressionService` (`RecordResourceNodeCompleted`,
  `RecordRecipeCrafted`, `RecordBuildingConstructed`, `RecordEnemyDefeated`)
  sowie der Fangvorgang für das Abschlussziel. Für „sammle x Einheiten" gibt es
  bisher **keinen** Ereignispunkt im Spielfluss — siehe den toten
  `RecordGatheredUnits`-Pfad in F31-004. Hier ist zu entscheiden, ob ein
  Sammelzähler eingeführt wird oder ob Questschritte nur an Knotenabschlüsse,
  Rezepte und Gebäude andocken.
- **Persistenz:** Queststand im Spielstand, mit Migration — der Spielstand ist
  versioniert und hat bereits eine Migrationskette.
- **Darstellung:** Anzeige des aktuellen Schritts im HUD und eine Abschlussmeldung.
- **EP-Werte:** gehören in die Bibel (M13.4), sonst weicht sie vom Code ab.

**Empfehlung zum Vorgehen:** Das ist kein Sammlungseintrag, der sich nebenbei
abarbeiten lässt, sondern ein eigenes Vorhaben mit Entwurf und Plan — Umfang
vergleichbar mit dem Technologiebaum. Vorschlag: nach den kleinen F31-Fixes als
eigenes Vorhaben aufsetzen, mit einer ersten Fassung, die **nur** an bereits
vorhandene Ereignisse andockt (Rezept gefertigt, Gebäude gebaut, Knoten
abgeschlossen, Eidra gefangen). Damit steht die Kette, ohne dass ein neuer
Sammelzähler nötig wird.

**Offen:** Die konkrete Schrittfolge. Ein belegter Anhaltspunkt aus der Bibel:
Der Einmalvorrat der Heimatbasis trägt bis Level 4 und deckt genau Werkbank,
handgefertigte Axt, Lagerkiste und Hammer — das ist ein natürlicher Anfang der
Kette.

**Zuordnung (Nutzerentscheid 16.08.2026):** Der Eintrag bleibt bewusst in der
Fixrunde **v0.3.1** und wandert nicht in die `FEATURESAMMLUNG_V0.4.md`, obwohl
er vom Umfang her ein Feature wie N04-001/N04-002 ist. Grund: Das Leveltempo
soll schon in v0.3.1 besser werden. Die Runde wird dadurch größer und später
fertig als eine reine Fixrunde — das ist eingepreist.

**Status: am 16.08.2026 gesammelt und entschieden — das Questsystem wird
eingeführt (Nutzerentscheid). Umsetzung noch nicht begonnen; als eigenes
Vorhaben mit Entwurf und Plan zu führen, nicht als Fix nebenbei.**

---

## F31-007 — Ausgemusterte Technologieknoten aus dem Baum entfernen

**Bereich:** Technologiebaum

**Wunsch (16.08.2026):** Die ausgemusterten Technologieknoten entfernen. Sie
erfüllen keinen Zweck.

**Befund (16.08.2026):** Der Baum enthält **33 Knoten, davon 4 stillgelegt**.
Sie tragen das Flag `requiredProgressFlag: retired.never`, das nie gesetzt wird,
und heißen im Klartext bereits nach ihrem Zustand:

| Knoten | Anzeigename | Punktkosten |
|---|---|---|
| `technology.19.t1_weapons` | Ausgemusterte T1-Waffenfreigabe | 1 |
| `technology.21.cloth_coat` | Ausgemusterter Stoffmantel-Knoten | 1 |
| `technology.22.cloth_bracers` | Ausgemusterter Armschienen-Knoten | 1 |
| `technology.23.cloth_shoes` | Ausgemusterter Stoffschuhe-Knoten | 1 |

**Zur Annahme, sie würden die Levelanforderung der Folgeknoten erhöhen: das
ist nicht der Fall.** Ausgezählt aus dem Baum-Asset:

- **Kein einziger Knoten** führt einen der vier als `prerequisiteNodeIds` — sie
  blockieren nichts. (`technology.19` hat selbst den Schmelzofen als
  Voraussetzung, ist aber nirgends Voraussetzung.)
- Da sie unerreichbar sind, werden ihre Punktkosten nie fällig. Von 30
  Punktkosten im Baum entfallen 4 auf die toten Knoten; die 26 übrigen stehen
  31 erreichbaren Punkten gegenüber (24 aus Stufe 1, je einer für Garon und den
  Schmiedeabschluss, fünf aus Level 25–29). Punkte sind also nicht knapp.
- Eine Levelanforderung je Knoten existiert im Datenmodell überhaupt nicht;
  `TechnologyNodeDefinition` kennt nur `prerequisiteNodeIds`, `pointCost`,
  `requiredProgressFlag` und `requiredBlueprintId`.

**Der Schaden ist ein anderer, aber real:** Die vier Karten sind im Baum
sichtbar und tragen laut T-001 die Beschriftung „FORTSCHRITTSFLAG FEHLT". Sie
belegen vier Plätze im Raster, lassen den Baum größer und teurer wirken als er
ist und kosten Platz in einem Fenster, das ohnehin zu klein war (W-010:
Kartenraster ragte aus dem Bildschirm). Entfernen lohnt sich also — aus
Aufräum- und Darstellungsgründen, nicht wegen der Levelanforderungen.

**Zu beachten bei der Umsetzung:**

- Es existiert eine Spielstandmigration, die genau diese Knoten behandelt:
  `V02SaveMigration` / Test `VersionTen_CombinesClothNodesAndRefundsRemovedNodes`
  legt die Stoffknoten zusammen und erstattet Punkte entfernter Knoten. Alte
  Spielstände, die die vier Knoten freigeschaltet haben, müssen weiterhin sauber
  laden.
- `TesterSeedSaveTests` prüft derzeit ausdrücklich, dass stillgelegte Knoten im
  Baum **vorhanden** sind („Erwartet: stillgelegte Knoten im Baum"). Dieser Test
  und die Zählung „29 von 33 Technologien" aus T-001 müssen mitgezogen werden.
- Nach dem Entfernen sind es 29 Knoten — die Aussage „alle Technologien" wird
  damit endlich wörtlich wahr.

**Status: gesammelt am 16.08.2026, nicht umgesetzt.**

---

## F31-008 — Die Tür lässt sich bauen, aber nicht öffnen und nicht durchschreiten

**Bereich:** Basisbau, Physik, Navigation

**Beobachtung (17.08.2026):** Die Tür kann gebaut werden, aber sie öffnet sich
nicht, man kann nicht hindurchgehen. Folglich lässt sie sich auch nicht
schließen.

**Befund (17.08.2026) — es gibt überhaupt keine Türlogik.** Geprüft und jeweils
belegt:

- **Kein Türverhalten im Code.** Es existiert kein Skript für Türen. Die Tür ist
  **kein `IInteractable`** — die vollständige Liste der Implementierungen lautet
  `ResourceNode`, `StorageContainer`, `WorkbenchController`,
  `WorldChestContainer`, `EidraForgeChestContainer`, `EidraForgeFormwallSite`,
  `FarmPlotController`, `WorldItemController`, `EidraCaptureTarget`,
  `EnemyLootContainer`. Keine Tür.
- **Kein `Animator` im Prefab** (`BLD_Door_L01.prefab`, Komponenten ausgezählt).

Es gibt also nichts, was öffnen oder schließen könnte. **Die Beobachtung ist
damit vollständig erklärt und hängt nicht an der getesteten Exe** — sie gilt für
jeden Stand, weil die Funktion nie existiert hat.

**Die Tür ist physisch eine Mauer.** Beide Prefabs gegenübergestellt; der
Kollisionskörper ist Wert für Wert derselbe:

| | `BLD_Wall_L01` | `BLD_Door_L01` |
|---|---|---|
| Collider | BoxCollider | BoxCollider |
| `m_IsTrigger` | 0 | 0 |
| `m_Size` | 1 / 2,6 / 0,2 | **1 / 2,6 / 0,2** |
| `m_Center` | 0 / 1,3 / 0 | **0 / 1,3 / 0** |
| Komponenten | Collider, NavMeshObstacle, 3 MonoBehaviours | identisch |

Der Kasten reicht über die volle Breite und die volle Höhe von 2,6 m. Der Spieler
läuft mit seinem `CharacterController` dagegen — deshalb kommt er nicht durch.

**Die Daten sagen aber ausdrücklich das Gegenteil:**

| Asset | `blocksNavigation` |
|---|---|
| `Wall.asset` | **1** |
| `Door.asset` | **0** |

`BuildingInstanceView.cs:84–85` wendet dieses Flag **ausschließlich** auf den
`NavMeshObstacle` an (`carving` und `enabled`), **nie auf den Collider**.

**Daraus folgt die eigentliche Fehlerbeschreibung: Die Tür ist für die
Wegfindung offen und für die Physik zu.** Der Wille „hier kommt man durch" ist in
den Daten bereits hinterlegt und wird für Gegner umgesetzt — der Spieler ist der
Einzige, den sie aufhält. Diese Asymmetrie ist im Spiel nachprüfbar: Ein Gegner
sollte einer Türkante folgen können, an der der Spieler steht und nicht
weiterkommt.

**Die Grafik fehlt nicht — nur die Bewegung.** Das Türblatt ist vollständig
modelliert: `DoorPlank_0` bis `DoorPlank_4`, `DiagonalBrace`, `Griff`,
`LockPlate`, `Scharnierband_0`/`_1` und die vier Rahmenteile. Es ist ein
fertiges Türblatt mit Beschlag an einer Angel, das sich nie bewegt.

**Die Bibel verlangt das Durchschreiten.** In der Größentabelle steht:
„Tür | 2,6 | 1,30 | **Durchgang 2,0 hoch, also genau Spielerhöhe**". M13.1
schließt für v0.2 nur **Türschlösser** und **komplexe Türanimationen** aus —
nicht das Hindurchgehen. Der Durchgang ist spezifiziert, aber nie gebaut worden.

**Was funktioniert, funktioniert vollständig:** Die Platzierung ersetzt eine Wand
atomar durch eine Tür (`BuildingPlacementRule.cs:134`, `building.door` auf
`building.wall`), die Kosten stehen (4 Holz + 2 Faser), und die
Spielstandmigration behandelt Wand und Tür gleich (`SaveGameMigration.cs:46`).
Der Fehler sitzt allein zwischen Datenwille und Kollisionskörper.

**Stoßrichtung (Vorschlag, noch nicht beauftragt):**

- **Türzustand offen/zu mit geschalteter Kollision.** Nähert sich der Spieler,
  schwingt das Blatt um die vorhandene Angel auf und der Collider wird
  abgeschaltet; entfernt er sich, schließt sie wieder. Das beantwortet beide
  Hälften der Meldung — Öffnen **und** Schließen — ohne Interaktionstaste und
  ohne neuen HUD-Knopf.
- **Kein Spielstandfeld nötig.** Wenn die Tür beim Laden immer geschlossen ist,
  bleibt §17 (Save-Format-Politik) unberührt. Das ist der Grund, die Automatik
  der Interaktionslösung vorzuziehen.
- **Der `NavMeshObstacle` bleibt unverändert** — die Daten stimmen dort bereits.
- **Alternative ohne jede Bewegung:** den Collider zum Türrahmen umbauen (zwei
  Pfosten plus Sturz oberhalb 2,0 m). Billiger, aber dann läuft der Spieler durch
  ein sichtbar geschlossenes Türblatt. Nur vertretbar, wenn das Blatt dauerhaft
  offen dargestellt wird.
- **Tests:** ein Physiktest, der belegt, dass der Spieler die Türkante bei
  offener Tür passiert und bei geschlossener nicht, plus ein Wegtest nach §26
  durch eine Türöffnung. Ein „Tür lässt sich bauen"-Test hätte diesen Fehler
  nicht gefunden — genau das ist geschehen.

**Zu klären:**

- **Automatik beim Annähern oder Interaktionstaste?** Empfehlung Automatik,
  Begründung oben (kein Spielstandfeld, beide Richtungen gelöst).
- **Sollen Gegner Türen benutzen?** Heute können sie es, weil der Obstacle aus
  ist. Wer das nicht will, braucht die umgekehrte Änderung — dann blockiert die
  Tür die Navigation und öffnet nur für den Spieler.
- **Bleibt der Raum bei offener Tür geschlossen?** Die Raumerkennung arbeitet
  auf Kantendaten, nicht auf Collidern — die Dachausblendung (`BuildingRoofView`)
  sollte also unberührt bleiben. Vor der Umsetzung einmal nachweisen, sonst
  flackern beim Durchgehen die Dächer.
- **Türgeräusch:** Es gibt bisher **kein** Audio-Ereignis für Türen
  (`AudioEventIds` enthält nur `music.outdoor` — der Treffer bei einer Suche nach
  „door" ist ein Wortteil, keine Tür). Falls gewünscht, ist das ein eigener
  kleiner Auftrag.

**Status: gesammelt am 17.08.2026, nicht umgesetzt.**

---

## F31-009 — Gegnerbesetzung passt nicht zur Gefahrenstufe der Weltkarte

**Bereich:** Gebiete, Gegnerbesetzung, Balance

**Wunsch (17.08.2026):** Die Gegneranzahl je Gebiet erhöhen und an der
Gefahrenstufe der Weltkarte ausrichten. Im kleinsten Gebiet sollen mindestens
fünf Wildlinge stehen.

**Befund (17.08.2026):** Die Weltkarte führt je Knoten eine Gefahrenstufe von
1 bis 5 (`WorldMapDangerIndicator`, Anzeige „GEFAHR x/5"). Die tatsächliche
Besetzung stammt unabhängig davon aus `enemyAllocations` der Zonen-Assets.
Beide Werte sind nirgends miteinander gekoppelt — und passen entsprechend
nicht zusammen:

| Gebiet | Gefahr | Gegner heute | EP | Besetzung |
|---|---|---|---|---|
| Heimatbasis | 1 | 0 | 0 | (sichere Zone) |
| Grünwald | 2 | 3 | 45 | 3 Wildling |
| Steinbruch | 3 | 4 | 150 | 2 Wildling, 2 Terrock |
| Nebelmoor | 3 | 4 | 150 | 2 Wildling, 2 Noctarion |
| Dämmerhain | 4 | 6 | 205 | 1 Wildling, 2 Rissling, 1 Wurzelstürmer, 1 Moorwerfer, 1 Granitpanzer |
| Schleiermoor | 4 | 6 | 210 | 1 Wildling, 1 Rissling, 1 Wurzelstürmer, 2 Moorwerfer, 1 Granitpanzer |
| **Glutruinen** | **5** | **2** | **30** | **2 Wildling** |
| Grauklüfte | 5 | 6 | 245 | 1 Wildling, 1 Rissling, 1 Wurzelstürmer, 1 Moorwerfer, 2 Granitpanzer |

**Der auffälligste Widerspruch sind die Glutruinen.** Sie tragen die höchste
Gefahrenstufe 5, stehen aber mit **zwei Wildlingen** da — dem schwächsten
Gegner im Spiel — und sind damit das dünnste besetzte Gebiet überhaupt. Dass
dort zusätzlich Garon steht (`bossZone: 1`), macht den Widerspruch nicht
kleiner: Der Weg zum Boss ist völlig unbewacht.

**Vorschlag für die Staffelung.** Eine einfache, nachvollziehbare Regel, die
die Mindestvorgabe von fünf im schwächsten Gebiet erfüllt:

```
Gegnerzahl = 3 x Gefahrenstufe - 1
```

| Gefahr | Gebiete | heute | neu |
|---|---|---|---|
| 1 | Heimatbasis | 0 | 0 (sichere Zone, Ausnahme) |
| 2 | Grünwald | 3 | **5** |
| 3 | Steinbruch, Nebelmoor | 4 | **8** |
| 4 | Dämmerhain, Schleiermoor | 6 | **11** |
| 5 | Glutruinen, Grauklüfte | 2 / 6 | **14** |

Das hebt die Gesamtzahl der Außengegner von 31 auf 71 und die EP eines
Weltdurchlaufs von 1.035 auf grob das Zweieinhalbfache — was zugleich auf
F31-006 einzahlt, wo der zu geringe Anteil an nicht-wiederholbaren Quellen
festgehalten ist.

**Zu entscheiden:** Nicht nur die Anzahl, sondern auch die **Zusammensetzung**
sollte der Gefahrenstufe folgen. Heute steht in jedem T2-Gebiet dieselbe
Mischung. Vorschlag: Gefahr 2 bleibt reines Wildling-Gebiet, ab Gefahr 3 kommen
die Gebietstiere dazu, ab Gefahr 4 überwiegen sie, bei Gefahr 5 stehen fast nur
noch starke Gegner. Für die Glutruinen wäre außerdem zu klären, welche Gegner
thematisch dorthin gehören — die Schmiede-Gegner (Glutzehrer, Aschenläufer)
liegen nahe, sind aber bisher dem Dungeon vorbehalten.

**Zu beachten:** Die Zonenfläche ist laut Befund vom 16.08. großzügig
bemessen; Platz ist also nicht der begrenzende Faktor. Ob die Wegfindung mit
14 gleichzeitigen Gegnern je Zone noch flüssig läuft, ist vor der Umsetzung zu
messen.

**Ergänzung (17.08.2026): Gegner sollen in der Nähe von Kisten stehen.** Ein
Teil der Gegner soll bewusst im Umfeld der Weltkisten platziert werden — nicht
unmittelbar daneben, aber nah genug, dass eine Kiste bewacht wirkt und nicht
unbeobachtet eingesammelt werden kann.

Die Grundlage dafür ist vorhanden: Jede Zone führt feste
`worldChestSpawnPoints` mit `stableId`, `position`, `rotationY` und
`allowedFamilies` — die Kistenplätze sind also bekannte Koordinaten und nicht
zufällig. Die Gegnerplatzierung dagegen läuft heute über die
Layout-Erzeugung, ohne Bezug zu diesen Punkten.

Zu klären ist der Abstandsbereich: Ein Ring um die Kiste mit einer Unter- und
einer Obergrenze — die Untergrenze, damit der Gegner nicht auf der Kiste steht
und den Zugriff blockiert, die Obergrenze, damit er noch als Bewachung wirkt.
Ein Anhaltspunkt für die Untergrenze ist der Aktivierungsradius der Gegner;
darunter greift der Gegner an, bevor die Kiste überhaupt erreichbar ist.
Ebenfalls zu entscheiden: Wie viele der Gegner eines Gebiets an Kisten
gebunden werden und wie viele frei verteilt bleiben — alle an Kisten zu binden
würde den Rest des Gebiets leerräumen.

**Zu beachten:** Die Zonen werden pro Lauf neu gewürfelt. Ein einzelner grüner
Durchlauf beweist deshalb nicht, dass der Abstand immer eingehalten wird — die
Prüfung muss über viele Saaten laufen, so wie es beim Truhenfreiraum nötig war.

**Status: gesammelt am 17.08.2026, nicht umgesetzt.**

---

## F31-010 — Höchstens zwei Äcker gleichzeitig

**Bereich:** Basisbau, Gebäudegrenzen

**Wunsch (17.08.2026):** Der Charakter soll maximal zwei Äcker gleichzeitig
bauen können.

**Befund (17.08.2026):** Eine Mengenbegrenzung je Gebäudetyp existiert
**nicht**. `BuildingCostDefinition` kennt die Felder `id`, `displayName`,
`tier`, `occupiesBuildField`, `blocksNavigation`, `placementKind`,
`groundRule`, `category`, `levelFootprints`, `levelPrefabs`,
`craftingStation`, `baseRate` und `eidraFactor` — **kein** Feld für eine
Höchstzahl. Entsprechend prüft auch der Bauvorgang nirgends eine Anzahl.

**Nebenbefund:** `baseRate` und `eidraFactor` werden von **keiner Stelle im
Code gelesen** — zwei weitere tote Felder derselben Art wie in F31-004.

**Stoßrichtung (noch nicht beauftragt):** Ein Feld `maximumCount` (0 = ohne
Grenze) in `BuildingCostDefinition`, geprüft im Bauvorgang gegen die im
`BuildingRegistry` bereits vorhandenen Instanzen desselben Typs. Für den Acker
auf 2 setzen. Im Baumenü sollte der Eintrag bei erreichter Grenze gesperrt
erscheinen und den Grund nennen, statt erst bei der Platzierung abzulehnen.

**Zu klären:** Zählt ein abgerissener Acker sofort wieder frei (vermutlich ja),
und gilt die Grenze pro Basis oder global? Da es nur eine Heimatbasis gibt, ist
das derzeit dasselbe — die Entscheidung sollte trotzdem bewusst fallen.

**Status: gesammelt am 17.08.2026, nicht umgesetzt.**

---

## F31-011 — Fanggerät wird vor seinen Produktionsgebäuden freigeschaltet

**Bereich:** Technologiebaum, Fortschrittsreihenfolge

**Beobachtung (17.08.2026):** Fanggerät und Batterie lassen sich nur aus
T1-Materialien bauen, die zugehörigen Produktionsgebäude werden aber erst
danach freigeschaltet. Zusätzlich der Wunsch: Der zweite Eidra-Platz soll aus
dem Technologiebaum verschwinden und stattdessen zusammen mit dem Fanggerät
freigeschaltet werden.

**Befund (17.08.2026) — die Reihenfolge ist nachweislich widersprüchlich.**
Die Rezepte verlangen:

| Rezept | Zutaten | Kommt aus |
|---|---|---|
| Fanggerät | 2 Brett, 2 Seil, 2 Kupferbarren | Sägewerk, Seilerei, Schmelzofen |
| Batterie | 2 Seil, 1 Kupferbarren | Seilerei, Schmelzofen |

Die Baumstruktur hängt aber alles nebeneinander an den Schmelzofen:

```
technology.10.smelter
   +-- technology.11.catch_device   (Fanggeraet + Batterie)
   +-- technology.13.sawmill        (Brett)
   +-- technology.14.ropewalk       (Seil)
   +-- technology.15.stonecutter    (Steinblock)
```

`catch_device` liegt damit **parallel** zu Sägewerk und Seilerei, nicht
dahinter. Wer den Knoten zuerst nimmt, hat das Rezept freigeschaltet, kann aber
weder Bretter noch Seile herstellen — die Freischaltung ist wertlos, bis zwei
weitere Knoten gekauft sind.

**Der zweite Eidra-Platz** hängt heute als eigener Knoten
`technology.12.second_eidra_slot` (Feature `feature.eidra_slot_2`, Kosten 1)
hinter `catch_device`.

**Stoßrichtung (noch nicht beauftragt):**

1. `technology.11.catch_device` bekommt `sawmill` und `ropewalk` als
   Voraussetzungen statt `smelter`. Damit ist das Fanggerät erst erreichbar,
   wenn seine Materialien herstellbar sind.
2. `feature.eidra_slot_2` von Knoten 12 nach Knoten 11 verschieben, Knoten 12
   entfernen. Fanggerät und zweiter Eidra-Platz kommen damit gemeinsam — was
   auch inhaltlich passt, weil das Fangen erst mit Platz im Team Sinn ergibt.
3. Der Steinblock (`stonecutter`) wird von keinem der beiden Rezepte gebraucht
   und kann bleiben, wo er ist.

**Zu beachten:** Entfernt man Knoten 12, greift dieselbe Migrationsfrage wie in
F31-007 — Spielstände, die ihn freigeschaltet haben, müssen weiter laden und
das Feature behalten. Beide Eingriffe gehören deshalb in einen Arbeitsgang.
Außerdem sinkt die Zahl der Knoten erneut; die Angabe „29 von 33" aus T-001 ist
danach zweimal veraltet.

**Berührt:** F31-007 (Knotenentfernung und Migration).

**Status: gesammelt am 17.08.2026, nicht umgesetzt.**

---

## F31-012 — Baumenü läuft über den Bildschirmrand hinaus

**Bereich:** Baumenü (BASISBAU), Layout

**Beobachtung (17.08.2026, mit Screenshot):** Die Katalogzeile des Baumenüs
reicht links und rechts über den Bildschirmrand hinaus. Einträge sind am Rand
angeschnitten, ein Teil der Gebäude ist gar nicht erreichbar.

**Zusammenhang:** Das ist die Folgestufe von **W-007** aus
`WUNSCHSAMMLUNG_20260814.md`. Dort lagen die Katalogeinträge übereinander, weil
`childControlWidth = false` die `preferredWidth = 300` der elf Einträge
ignorierte. Der Fix setzte `childControlWidth = true` — seither haben die
Einträge ihre volle Breite, aber die Zeile hat **keine Begrenzung auf die
Bildschirmbreite**: elf Einträge zu 300 px plus Abstände ergeben rund 3.400 px
Gesamtbreite.

**Stoßrichtung (noch nicht beauftragt):** Die Katalogzeile in einen horizontalen
Scrollbereich setzen — dieselbe Lösung, die W-010 für den Technologiebaum
gebracht hat (`TechnologyTreeScrollBuilder`: ScrollRect mit Maske und
ContentSizeFitter). Alternativ ein Raster über mehrere Zeilen statt einer
langen Reihe; bei elf Einträgen und wachsendem Katalog ist das vermutlich die
haltbarere Lösung.

**Test:** Ein Layouttest, der belegt, dass **alle** Einträge innerhalb der
Canvas-Grenzen liegen. Der bestehende `BuildingMenuCatalogLayoutTests` prüft
Eintragsbreiten > 0 und disjunkte X-Bereiche — beides ist erfüllt, obwohl die
Einträge aus dem Bild laufen. Genau diese Lücke hat den Fehler durchgelassen.

**Berührt:** W-007 (dessen Fix diesen Zustand erzeugt hat), W-010 (Vorlage für
die Lösung), F31-013 (gleiche Ansicht).

**Status: gesammelt am 17.08.2026, nicht umgesetzt.**

---

## F31-013 — Baumenü wirkt unaufgeräumt, Icons passen nicht zu den Gebäuden

**Bereich:** Baumenü (BASISBAU), Darstellung, Icons

**Beobachtung (17.08.2026, mit Screenshot):** Das Baumenü sieht unaufgeräumt
aus. Beschriftungen, Kostenangaben und Icons überlagern einander; Namen brechen
mitten im Wort um („Schm elzofe n", „Lager kiste", „Werkb ank"). Die Icons
passen nicht zu den Gebäuden.

**Was im Screenshot sichtbar ist:**

- Der Gebäudename und die Kostenzeile liegen **übereinander** statt
  untereinander — „Holz 2/20" steht quer über „Tür", „Holz 10/20" über
  „Lagerkiste".
- Namen werden auf zu schmalem Raum umbrochen und zerfallen in Silben.
- Die **Lagerkiste** trägt das Bild eines **Metallbarrens**, nicht das einer
  Kiste. Die Kategorieüberschriften („Werkstätten", „Versorgung",
  „Landwirtschaft") sitzen teils über den Einträgen der Nachbarspalte.

**Verdacht zur Icon-Frage:** Dieselbe Fehlerklasse wie F31-002 — die Icons
werden über einen Namen zusammengebaut oder von Hand verdrahtet, und mehrere
Gebäude teilen sich dabei das falsche Bild. Vor der Umsetzung ist je Gebäude zu
prüfen, welches Icon-Asset tatsächlich referenziert ist und ob ein passendes
existiert. Bei den Rüstungs-Icons lagen die richtigen Bilder bereits ungenutzt
im Projekt; das ist hier ebenfalls möglich.

**Stoßrichtung (noch nicht beauftragt):** Die Katalogkarte als eigenes,
sauber gegliedertes Layout aufbauen — Icon oben, Name darunter, Kosten unten,
jeweils in eigenen Layoutelementen mit ausreichender Breite statt frei
positionierter Textfelder. Das gehört in denselben Arbeitsgang wie F31-012,
weil beide dieselbe Ansicht betreffen und der Umbau der Zeile die Kartengröße
ohnehin verändert.

**Test:** Über einen reinen Layouttest hinaus braucht es hier eine
**bildbasierte Abnahme** — genau daran ist W-007 schon einmal gescheitert, wo
die Abnahme ausdrücklich noch ausstand.

**Berührt:** F31-012 (gleiche Ansicht), F31-002 (gleiche Icon-Fehlerklasse).

**Status: gesammelt am 17.08.2026, nicht umgesetzt.**

---

## F31-014 — Kupfererz wird beim Schmelzen scheinbar nicht verbraucht

**Bereich:** Handwerk, Lager, Schmelzofen

**Beobachtung (17.08.2026):** Beim Herstellen von Kupferbarren im Schmelzofen
wird kein Kupfererz verbraucht — der Bestand im Lager bleibt unverändert.

**Befund (17.08.2026) — der Verbrauch ist implementiert.** Die Kette wurde
geprüft:

- Das Rezept `craft_copper_bar` verlangt **3 Kupfererz** je Barren
  (`CraftingRecipeTable.cs:59`, Station `Smelter`).
- `CraftingService.TryCraft` sammelt die Zutaten und übergibt sie als
  Abzugsliste an `HomeBaseMaterialStock.TryApplyTransaction`.
- Diese Transaktion zieht die Menge ab und schreibt geänderte Lagerzustände
  über `_session.SetStorageState` zurück.
- Eine automatische Gebäudeproduktion, die am Handwerk vorbei produzieren
  könnte, existiert **nicht**: `baseRate` und `eidraFactor` aus
  `BuildingCostDefinition` werden von keiner Stelle gelesen.

**Wahrscheinlichste Erklärung — Reihenfolge der Entnahme.**
`TryCreateCandidate` nimmt die Zutaten **zuerst vollständig aus dem Rucksack**
und erst den Rest aus den Lagern:

```
int num = Math.Min(benoetigt, _inventory.GetTotalAmount(itemId));   // Rucksack zuerst
int remaining = benoetigt - num;
RemoveFromStorages(...)                                            // dann Lager
```

Wer genug Kupfererz im Rucksack trägt, sieht den **Lagerbestand unverändert** —
abgezogen wurde aus dem Rucksack. Das wäre kein Fehler, sondern eine
Fehlwahrnehmung, und in einer Minute zu prüfen: einmal mit leerem Rucksack
schmelzen und beobachten, ob der Lagerbestand sinkt.

**Falls der Bestand auch dann unverändert bleibt**, liegt der Fehler im
Rückschreiben der Lagerzustände. Ansatzpunkt: `EligibleStorages()` — welche
Kisten dort einbezogen werden — und ob `array2[i] = array[i].Slots` auf einer
Kopie oder auf der Originalreferenz arbeitet.

**Status: gesammelt am 17.08.2026, nicht umgesetzt — Gegenprobe mit leerem
Rucksack steht aus und entscheidet, ob überhaupt ein Fehler vorliegt.**

---

## F31-015 — Gegner schauen beim Angriff vom Spieler weg

**Bereich:** Gegner, Animation, Ausrichtung

**Beobachtung (17.08.2026):** Gegner führen ihren Angriff aus, dabei blickt die
Figur jedoch vom Spieler weg — entgegengesetzt zur Richtung des
Angriffstelegraphen.

**Befund (17.08.2026, Verortung):** Die Ausrichtung selbst ist regelkonform
programmiert. `EnemyControllerBase.cs:306` dreht den Gegner mit

```
Quaternion.LookRotation(direction.normalized)
```

zum Ziel; `LookRotation` richtet die **+Z-Achse** auf das Ziel aus. Der
Telegraph wird aus derselben Ausrichtung abgeleitet und zeigt laut Beobachtung
korrekt — Ziellogik und Telegraph stimmen also überein, nur das sichtbare
Modell nicht.

Damit deutet alles auf eine **um 180 Grad verdrehte Blickrichtung der
Kreaturengeometrie**: Das Modell schaut in −Z statt in +Z. Das
Wildling-Prefab (`Wildling_3D.prefab`) trägt keine ausgleichende Drehung
(`m_LocalRotation` ist 0/0/0/1) und besteht nur aus dem Wurzelobjekt mit
`CreatureMeshPresentation` — die Geometrie entsteht zur Laufzeit, eine
Korrekturdrehung könnte also nur dort oder in der Quellgeometrie liegen.

**Nächster Prüfschritt vor der Umsetzung:** Feststellen, in welche Richtung die
gebaute Kreaturengeometrie blickt — im `CreatureMeshBuilder` bzw. in den
HTML-Quellen der Kreaturenserie. Zu klären ist außerdem, ob **alle** Kreaturen
betroffen sind oder nur einzelne; das entscheidet, ob die Quellgeometrie
gedreht wird oder eine Korrektur an einer zentralen Stelle greift. Eine
Korrektur nur am Prefab wäre der falsche Weg, solange die Meshes zur Laufzeit
erzeugt werden.

**Zu beachten:** Die Figuren sind bereits mehrfach Gegenstand von Prüfläufen
gewesen, ohne dass die Blickrichtung aufgefallen ist — ein Prüfskript, das
Geometrie und Skinning vergleicht, sieht eine Drehung um die Hochachse nicht.
Die Abnahme braucht ein Bild aus dem Spiel, nicht nur einen grünen Lauf.

**Status: gesammelt am 17.08.2026, nicht umgesetzt.**

---

## F31-016 — Eidra-Fähigkeiten funktionieren nur gegen die zwei Bosse

**Bereich:** Eidra, Fähigkeiten, Kampf

**Beobachtung (17.08.2026):** Terrocks Fähigkeiten melden „KEIN GÜLTIGES ZIEL",
obwohl ein Gegner in der Nähe steht. Besonders unverständlich bei der
Schild-Fähigkeit, die auf die eigene Figur wirkt und gar kein Ziel braucht.

**Befund (17.08.2026) — die Beobachtung ist vollständig erklärt, und die
Ursache reicht weiter als die Meldung vermuten lässt.**

**1. Ein „gültiges Ziel" kann nur ein Boss sein.** `_combatTarget` wird an
genau zwei Stellen gesetzt: `AttachBoss(BossController)` und
`AttachForgeBoss(CoreGuardianController)` — also ausschließlich für **Garon**
und den **Kernwächter** der Eidra-Schmiede. Für Wildlinge, Risslinge,
Granitpanzer und alle übrigen Gegner wird es **nie** gesetzt und bleibt `null`.

Verschärfend die zweite Bedingung (`EidraTeamController.cs:95`):

```csharp
private bool TargetBattleActive =>
    (_combatTarget is BossController b) ? b.BattleActive
                                        : (_combatTarget is CoreGuardianController);
```

Auch wenn ein Ziel gesetzt wäre, gilt es nur als kampfbereit, wenn es einer
dieser beiden Typen ist. Ein normaler Gegner kann diese Prüfung nicht bestehen.

**Damit sind die aktiven Eidra-Fähigkeiten im gesamten regulären Spiel
unbenutzbar** — sie funktionieren ausschließlich in zwei Bosskämpfen. Das
steht im Widerspruch zu M0 der Programmierbibel, wonach die Eidra tragend sein
sollen, und zur Begründung in M8.6, das Fanggerät bewusst früh einzuhängen,
damit die Eidra „die gesamte zweite Hälfte begleiten".

**2. Die Schild-Fähigkeit scheitert an einer Prüfung, die für sie nicht gilt.**
In `TryValidateAbility` steht die Zielprüfung **vor** der Abzweigung für den
Schild:

```csharp
if (_combatTarget == null || !_combatTarget.IsAlive || !TargetBattleActive || …)
{
    failure = "KEIN GÜLTIGES ZIEL";      // <- greift auch fuer den Schild
    return false;
}
if (ability.ExecutionType == AbilityExecutionType.Shield) { … return true; }
```

Dass der Schild kein Ziel braucht, ist in den Daten ausdrücklich hinterlegt:
`Steinhaut.asset` trägt **Reichweite 0**, während alle anderen Fähigkeiten
Reichweiten von 8 bis 12 führen. Die Reihenfolge der Prüfungen widerspricht
also den eigenen Daten.

**Terrocks Ausstattung:** Rolle `Defend`, Fähigkeiten `Felsbrecher`
(`StaggerStrike`, Reichweite 8) und `Steinhaut` (`Shield`, Reichweite 0).
Beide sind betroffen — der Felsbrecher, weil normale Gegner kein Ziel sein
können, die Steinhaut zusätzlich durch die Prüfreihenfolge.

**Stoßrichtung (noch nicht beauftragt) — zwei getrennt umsetzbare Schritte:**

1. **Sofort und klein: die Schild-Abzweigung vor die Zielprüfung ziehen.**
   Fähigkeiten mit Reichweite 0 bzw. vom Typ `Shield` dürfen nicht an einer
   Zielprüfung scheitern. Das ist eine Umstellung weniger Zeilen und macht
   Steinhaut sofort überall nutzbar.
2. **Eigentlicher Fix: normale Gegner als Ziel zulassen.** `_combatTarget` darf
   nicht länger nur über die beiden Boss-Anbindungen gefüllt werden. Es gibt
   dafür bereits eine passende Zielsuche im Projekt:
   `SceneCombatTargetQuery.TryFindClosest(attacker, range, out target)` liefert
   das nächstgelegene lebende Ziel aus der `CombatTargetRegistry` und wird von
   den Spielerwaffen benutzt. Sie mit der Reichweite der jeweiligen Fähigkeit
   aufzurufen wäre die naheliegende Lösung. `TargetBattleActive` muss dabei
   entsprechend erweitert werden — die Sonderbehandlung nach Typ ist der
   eigentliche Konstruktionsfehler.

**Zu klären:** Die drei Fähigkeiten `BackMark`, `MoltenBrand` und `ShadowStep`
tragen zusätzliche Sonderbedingungen — `BackMark` verlangt ausdrücklich
`_boss != null` („ZIEL KANN NICHT MARKIERT WERDEN"), `MoltenBrand` einen
Schutzwert am Ziel. Bei der Erweiterung ist je Fähigkeit zu entscheiden, ob
sie gegen normale Gegner sinnvoll ist oder bewusst auf Bosse beschränkt bleibt.
`BackMark` auf Bosse zu beschränken kann gewollt sein; `StaggerStrike` und
`Shield` sicher nicht.

**Test:** Ein PlayMode-Test, der eine Fähigkeit gegen einen **normalen** Gegner
auslöst und den Erfolg belegt. Die bestehenden Prüfungen können den Fehler
nicht gefunden haben, weil sie den Bosskampf abbilden — genau dort funktioniert
alles.

**Status: gesammelt am 17.08.2026, nicht umgesetzt.**

---

## F31-017 — Grüne Bauumrandung verschwindet über einem gelegten Boden

**Bereich:** Basisbau, Platzierungsvorschau

**Beobachtung (16.08.2026):** Steht ein Gebäude auf einem Boden, ist die grüne
Umrandung der Platzierungsvorschau nicht mehr zu sehen.

**Befund (16.08.2026) — die Markierung liegt unter dem Boden, weil ihre
Höhenanhebung nie angewendet wird.**

`BuildingGhostView.cs:12` deklariert genau dafür eine Konstante:

```csharp
private const float MarkHeight = 0.07f;
```

Sie kommt in der ganzen Datei **exakt einmal vor — in ihrer eigenen
Deklaration**. Kein Aufruf, keine Zuweisung, keine Verwendung. Die
Vorschaumarkierung wird also ohne jede Anhebung auf der Rasterebene gezeichnet.

Dem gegenüber steht das Boden-Prefab `BLD_Floor_L01`: Seine Bestandteile
liegen auf **y = 0,0 / 0,06 / 0,14**. Die Bodenoberfläche liegt damit deutlich
über der Rasterebene — und verdeckt die Markierung vollständig. Selbst wenn
`MarkHeight` angewendet würde, läge die Markierung mit 0,07 immer noch unter
den Bodenteilen auf 0,14 und knapp an denen auf 0,06, was zusätzlich
Z-Fighting erzeugen würde.

Die Farben selbst sind vorhanden und korrekt: `BuildingPreviewContentBuilder`
führt drei Zustände — `Valid` in Grün (`0.24 / 0.86 / 0.62`), `Conditional` in
Orange, `Invalid` in Rot. Es fehlt nichts an der Grafik; sie liegt nur an der
falschen Höhe.

**Stoßrichtung (noch nicht beauftragt):** Die Markierung über der
tatsächlichen Oberfläche ausrichten statt über der Rasterebene. Zwei Varianten:

1. **`MarkHeight` endlich anwenden und auf einen Wert über der Bodenoberkante
   heben** — mindestens 0,16, besser 0,20, damit auch der Bodenrand auf 0,14
   sicher unterschritten wird. Einfach, aber ein fester Wert, der bei neuen
   Bauteilen wieder brechen kann.
2. **Die Höhe aus dem Untergrund ableiten** — liegt an der Stelle ein Boden,
   die Markierung auf dessen Oberkante plus einen kleinen Abstand setzen.
   Robuster, weil sie mit künftigen Bauteilen mitgeht.

Empfehlung: Variante 1 als Sofortlösung, weil sie eine Zeile ist und den
gemeldeten Fall behebt; Variante 2 nur, falls weitere Bauteile mit
unterschiedlichen Höhen dazukommen.

**Test:** Ein Test, der die Y-Position der Vorschaumarkierung gegen die
Oberkante des darunterliegenden Bauteils prüft. Ein reiner „Vorschau ist
sichtbar"-Test genügt nicht — sie *ist* sichtbar, nur eben verdeckt.

**Einordnung:** Das ist innerhalb dieser Runde das **fünfte** tote Feld
derselben Art — nach `AttackRange` und `BackDamageMultiplier` (F31-015-Umfeld),
`experienceToNextLevel` und `RecordGatheredUnits` (F31-004) sowie `baseRate`
und `eidraFactor` (F31-010). Es lohnt, am Ende der Runde einmal gezielt nach
deklarierten, aber nie gelesenen Feldern zu suchen; das Muster wiederholt sich
zu oft, um Zufall zu sein.

**Status: gesammelt am 16.08.2026, nicht umgesetzt.**

---

## F31-018 — Rüstung soll zusätzlich Lebenspunkte geben

**Bereich:** Ausrüstung, Balance

**Anlass (16.08.2026):** Die vollständige Stoffrüstung senkt den Schaden eines
Wildlings um weniger als zwei Lebenspunkte. Das ist zwar exakt so
spezifiziert, fühlt sich aber nach keiner Belohnung an. Entscheidung: Rüstung
soll **zusätzlich zum Schutz auch Lebenspunkte** geben.

**Ausgangslage (16.08.2026 geprüft):**

- Der Schutz ist korrekt umgesetzt und entspricht der Bibel (Zeile 1419):
  vollständiges T0/T1/T2 gibt **10/20/30 Prozent**. Formel:
  `Schaden × (1 − Schutz)`, Kappung bei 60 Prozent.
- Die Gegnerschäden liegen im gesamten Spiel zwischen **12 und 34**
  (Glutzehrer 12, Wildling 18, Granitpanzer 30, Wurzelstürmer 32, Garons
  Fronthieb 34). Prozentualer Schutz bleibt dadurch immer im einstelligen
  Bereich.
- **Die Lebenspunkte des Spielers sind fest auf 100** (`PlayerPrefabBindings`,
  `maxHealth = 100f`) und **steigen zu keinem Zeitpunkt** — es gibt keine
  Kopplung an Level, Stufe oder Fortschritt. Auf Level 40 hat die Figur
  dieselben 100 Lebenspunkte wie auf Level 1.

Damit wächst die Überlebensfähigkeit über das gesamte Spiel nur von 5,6 auf
7,9 Treffer gegen einen Wildling. **Das ist die eigentliche Lücke** — der
gewünschte Lebenspunktebonus schließt sie.

**Vorgeschlagenes Balancing: „So viele Lebenspunkte wie Prozent Schutz."**

Jedes Rüstungsteil gibt so viele Lebenspunkte, wie sein Schutzwert in Prozent
beträgt — der Wert ist also schlicht `Schutz × 100`. Damit gibt es **keine
zweite Zahlenreihe zu pflegen**, die Staffelung folgt automatisch der bereits
abgenommenen Schutzstaffelung, und die Regel ist in einem Satz erklärbar.

| Slot | Stoff (T0) | Kupfer (T1) | Eisen (T2) |
|---|---|---|---|
| Helm | +2 | +4 | +6 |
| Brust | +4 | +8 | +12 |
| Hände | +1 | +2 | +3 |
| Beine | +3 | +6 | +9 |
| **Vollständig** | **+10** | **+20** | **+30** |

Teilsets wirken anteilig, genau wie beim Schutz — wer nur den Harnisch trägt,
bekommt dessen Anteil.

**Wirkung gegen einen Wildling (18 Schaden):**

| Ausrüstung | Leben | Schutz | Schaden je Treffer | Treffer bis Tod |
|---|---|---|---|---|
| ohne | 100 | 0 % | 18,0 | 5,6 |
| Stoff | 110 | 10 % | 16,2 | **6,8** |
| Kupfer | 120 | 20 % | 14,4 | **8,3** |
| Eisen | 130 | 30 % | 12,6 | **10,3** |

Gegen Garons stärksten Angriff (34 Schaden): 2,9 Treffer ohne Rüstung, 5,5 mit
vollem Eisen.

**Warum das gut trägt:** Lebenspunkte und Schadensreduktion **multiplizieren**
sich. Beide Werte für sich bleiben klein und unauffällig, ihre Kombination
verdoppelt die Überlebensdauer über die drei Materialstufen — genau die
Belohnung, die heute fehlt. Zugleich bleiben die absoluten Zahlen so niedrig,
dass die bestehende Gegnerbalance nicht umgeworfen wird.

**Vier Punkte, die bei der Umsetzung zu klären sind:**

1. **Beim Anlegen nicht heilen.** Der Maximalwert steigt, die aktuellen
   Lebenspunkte bleiben. Sonst wäre ein Rüstungswechsel im Kampf ein
   kostenloser Heiltrank.
2. **Beim Ablegen begrenzen.** Sinkt der Maximalwert unter den aktuellen Wert,
   muss dieser mitgezogen werden — sonst steht dauerhaft „130/100" im
   Lebensbalken.
3. **Haltbarkeit beachten.** Rüstung hat aktive Haltbarkeit (v0.2). Geht ein
   Teil kaputt, entfällt sein Bonus — es gilt derselbe Fall wie beim Ablegen
   und muss denselben Weg nehmen.
4. **Der Lebensbalken muss den neuen Höchstwert anzeigen.** `Damageable`
   kennt bisher nur `Initialize(maxHealth)` und keine Modifikatoren für den
   Maximalwert — für den Schutz existiert bereits ein Modifikatorsystem
   (`_protectionModifiers`), das als Vorlage dienen kann.

**Zwingend mitzuziehen:** Die Bibel führt die Schutzstaffelung in Zeile 1419
und die Schutzregeln ab Zeile 3204. Der Lebenspunktebonus gehört an dieselben
Stellen, sonst weicht die Bibel vom Code ab.

**Test:** Je Rüstungsteil und je Stufe der Bonus einzeln, das vollständige Set
gegen die Summe, sowie die drei Randfälle Anlegen, Ablegen und Bruch — der
Fall „aktueller Wert über Maximum" ist der, der in der Praxis auffallen würde.

**Nebenbefund, nicht Teil dieses Eintrags:** Dass die Lebenspunkte über 40
Level konstant bleiben, ist unabhängig von der Rüstung eine eigene
Designentscheidung. Falls Leben künftig mit dem Level wachsen soll, müssen die
hier vorgeschlagenen Werte erneut betrachtet werden — sie sind gegen eine
feste Basis von 100 bemessen.

**Status: gesammelt am 16.08.2026, nicht umgesetzt. Balancing vorgeschlagen,
noch nicht freigegeben.**

---

## F31-019 — Brot-Buff lief 2 Minuten statt der beschriebenen 20 Sekunden

**Bereich:** Verbrauchsgegenstände, Kampf-Buff, Datenpflege

**Beobachtung (17.08.2026, in der Exe v0.3.0-dev):** Der Buff der ersten
Nahrung (Brot) hält rund 2 Minuten an, nicht die 20 Sekunden, die die
Beschreibung nennt.

**Befund (17.08.2026) — im aktuellen Projektstand ist alles konsistent auf
20 Sekunden:**

- `BuffFood.asset`: `duration: 20`, Beschreibung „Erhöht Schaden und
  Stagger-Schaden für 20 Sekunden." (+20 % Schaden, +15 % Stagger).
- Die Quelltabelle `ItemContentTable.cs` (Stand 14.08., Zeile 35) übergibt
  `20f`.
- `ConsumableController.FoodRoutine` zählt exakt `Duration` in Echtzeit
  herunter (`Time.deltaTime`, keine Zeitskalierung im Spielbetrieb; nur das
  Pausenmenü hält die Zeit an — Fenster wie Werkbank oder Basisbau pausieren
  nicht).
- Das HUD zeigt die Restzeit als ganze Sekundenzahl.

**Die wahrscheinliche Erklärung ist Datendrift zwischen Asset und Tabelle.**
Belegbar ist: **Alle 58 Item-Assets wurden am 17.08. um 01:55 von einem
Builder-Lauf neu gestempelt** (einheitlicher Zeitstempel im ganzen Ordner;
die Tabelle selbst blieb unverändert vom 14.08.). Der Builder schreibt die
Tabellenwerte über alles, was im Inspector steht. Wenn das Asset vor diesem
Lauf abweichende 120 Sekunden trug — etwa aus einer Inspector-Änderung, die
nie in die Tabelle übernommen wurde —, dann hat die Exe vom 16.08. diese 120
eingebaut, und der Builder-Lauf von heute Nacht hat die Abweichung
anschließend stillschweigend beseitigt. Nachprüfbar ist das nicht mehr:
Assets liegen nicht in der Versionskontrolle, der Vorzustand ist weg.

**Entscheidung (17.08.2026, Nutzer):** Der Buff soll **vorerst 2 Minuten**
halten — die beobachtete Exe-Dauer wird also zum Sollwert erklärt, das
Feintuning folgt in einer späteren Balance-Runde.

**Damit zu ändern (ein Arbeitsgang, drei Stellen):**

1. `ItemContentTable.cs` Zeile 35: `20f` → `120f` **und** den
   Beschreibungstext auf „für 2 Minuten" umstellen — die Dauer steht dort
   doppelt (Wert und Text).
2. Builder laufen lassen, damit `BuffFood.asset` den Tabellenwert erhält —
   nicht das Asset direkt editieren, sonst entsteht exakt die Drift, die
   dieser Eintrag dokumentiert.
3. Prüfen, ob ein Test die 20 festschreibt (`ContentAssetTests` und
   Umfeld); falls ja, auf 120 mitziehen.

Die HUD-Anzeige braucht keine Änderung — sie zeigt die Restzeit als nackte
Sekundenzahl und zählt von jedem Startwert herunter.

**Einordnung:** Der eigentliche Befund ist nicht das Brot, sondern der
Mechanismus: Inspector-Änderungen an Item-Assets überleben keinen
Builder-Lauf, und ohne Versionskontrolle auf den Assets ist die Abweichung
unsichtbar, bis sie jemand im Spiel bemerkt. Das ist dasselbe Muster wie die
tote Kurventabelle in F31-004 — zwei Wahrheiten für denselben Wert, nur eine
wirkt.

**Status: gesammelt am 17.08.2026, nicht umgesetzt. Entschieden: Dauer auf
2 Minuten anheben (Tabelle + Beschreibung + Builder-Lauf); spätere
Balance-Runde vorbehalten.**

---

## F31-020 — Gegner bleiben nach dem Zurücksetzen dauerhaft passiv

**Bereich:** Gegner-KI, Navigation

**Beobachtung (17.08.2026):** Wenn Gegner zurücksetzen (nach einer
Verfolgung zum Startpunkt zurücklaufen), greifen sie danach nie wieder an.
Sie tun gar nichts mehr.

**Befund (17.08.2026) — der Rückweg kann nicht abgeschlossen werden; die KI
hängt dauerhaft im Zustand `Return`. Die Ursache ist ein Zahlenkonflikt
zwischen Agent und Rückkehrprüfung:**

- `ConfigureAgent` setzt die Stoppdistanz des NavMeshAgent auf
  `AttackRange − 0,1` — beim Wildling also **1,6 m** (`EnemyControllerBase.cs:573`,
  `autoBraking = true`). Der Agent hält damit von **jedem** Ziel 1,6 m
  Abstand, auch vom eigenen Heimatpunkt.
- `ReturnRoutine` (Zeile 418) läuft aber, bis der Gegner dem Heimatpunkt auf
  `ReturnTolerance` nahegekommen ist — und die beträgt in allen
  Gegner-Assets **0,55 m**.
- **1,6 m Stoppabstand > 0,55 m Ankunftstoleranz:** Der Agent bremst von
  selbst 1,6 m vor dem Heimatpunkt ab und bleibt stehen; `HasReached` wird
  nie wahr; die Schleife `MoveTowards(HomePosition)` läuft ewig weiter,
  ohne dass sich etwas bewegt (das Ziel ist unverändert, die Repath-Sperre
  greift, der Agent betrachtet sich als angekommen).

Damit steckt das Hirn der KI dauerhaft in der `ReturnRoutine` fest — der
Zustand bleibt `Return`, und alles Folgende ist damit erklärt:

- **Kein erneuter Angriff:** Die Wiedererkennung des Spielers
  (`CanReacquireTarget`, Erkennungsradius 7–10 m je Gegner) sitzt in der
  Hirnschleife **nach** der Rückkehr — sie wird nie wieder erreicht.
- **Auch Draufschlagen weckt sie nicht:** Der Treffer-Aggro-Pfad
  (`OnDamageResolved` → `ActivateCombat`) greift nur, solange
  `CombatAuthorized` noch falsch ist — nach dem ersten Kampf ist es dauerhaft
  wahr. Zusätzlich wird Stagger-Schaden im Zustand `Return` bewusst auf 0
  gesetzt (`ApplyDamage`, Zeile 141–144): Der Gegner nimmt zwar
  Lebensschaden, reagiert aber sichtbar auf nichts.
- **Erledigt aussehende Heilung:** `OnReturnedHome` (Volleilung) wird nie
  erreicht — der Gegner bleibt auf dem Stand, den er beim Abbruch hatte.

**Betroffen sind alle Gegner der Außengebiete** — Stoppdistanz
(AttackRange 1,65–1,85 → 1,55–1,75 m) liegt bei jedem geprüften Gegner über
der Toleranz von 0,55 m. Der Fehler ist deterministisch: Jede
Leine-Rückkehr endet in diesem Zustand. Dieselbe Falle liegt im Pfad nach
dem Spielertod (`ReturnAfterTargetDeathRoutine` wartet ebenfalls auf die nie
eintretende Ankunft).

**Stoßrichtung (noch nicht beauftragt):** Die Stoppdistanz ist ein
Angriffskonzept („bleib auf Schlagweite stehen") und hat auf dem Heimweg
nichts verloren. Sauberster Fix: `ReturnRoutine` setzt die Stoppdistanz des
Agenten für die Dauer des Rückwegs auf 0 und stellt sie bei Ankunft wieder
her. Alternativ die Ankunftsprüfung gegen
`stoppingDistance + ReturnTolerance` rechnen — dann bleibt der Gegner aber
sichtbar neben seinem Heimatpunkt stehen. Datenänderung (Toleranz > 1,75)
wäre die schlechteste Variante: neun Assets anfassen für einen Codefehler.

**Test:** PlayMode — Gegner ködern, bis die Leine greift, Rückkehr abwarten,
dann belegen: Zustand verlässt `Return`, und bei Wiederannäherung unter den
Erkennungsradius greift der Gegner erneut an. Der Test muss die Ankunft über
den Zustandswechsel prüfen, nicht über eine Positionsnähe — sonst testet er
an genau dem vorbei, was kaputt ist.

**Diagnose bei der Umsetzung widerlegt (17.08.2026).** Ein neuer
PlayMode-Test (`Fixrunde031PlayModeTests.GegnerKehrtSelbststaendigHeimHeiltUndGreiftErneutAn`)
baut die Leine-Rückkehr mit den echten Werten nach (Stoppdistanz 1,6,
Toleranz 0,55, **ohne** den `Warp`-Teleport, mit dem der bestehende
Leash-Test die Ankunft abkürzt) — und der Gegner kommt **selbstständig an,
heilt voll und greift bei Wiederannäherung erneut an**. Die
Stoppdistanz-Theorie sagte einen Deadlock voraus; er tritt nicht ein. Der
Test bleibt als Regressionsschutz im Bestand: Er deckt die Kette
Rückkehr → Ankunft → Heilung → erneutes Aggro erstmals ohne Abkürzung ab.

**Damit ist die gemeldete Beobachtung wieder offen.** Der Codepfad der
reinen Leine-Rückkehr ist nachweislich gesund; die Passivität muss einen
anderen Auslöser haben. Unverifizierte Kandidaten: der Pfad nach dem
**Spielertod** (`ReturnAfterTargetDeathRoutine`), eine hängende
Interaktionspause (`SetInteractionPaused`) beim Zonenwechsel, oder ein
Gegnertyp mit eigenem Controller-Verhalten (beobachtet wurde nicht
festgehalten, welcher Gegner es war). **Vor weiterer Arbeit braucht es die
konkrete Situation aus dem Spiel:** welcher Gegner, welches Gebiet, und was
unmittelbar vor dem Reset passiert ist (weggelaufen, gestorben,
Zonenwechsel?).

**Status: Umsetzung am 17.08.2026 begonnen und sauber abgebrochen —
Diagnose durch Test widerlegt, kein Produktionscode geändert.
Regressionstest hinzugefügt. Wartet auf eine reproduzierbare Situation.**

---

## F31-021 — Minimap-Kartenrand im ersten Verlies so groß wie eine Außenzone

**Bereich:** Minimap (N04-001), Eidra-Schmiede

**Beobachtung (17.08.2026):** Im ersten Verlies ist die Karte genauso groß
wie in den anderen Gebieten, obwohl das Verlies größer ist.

**Befund (17.08.2026) — die Szenenkopie behielt das Zonenquadrat der
Emberruinen.** `EidraForge.unity` entstand als Kopie von
`Zone_EmberRuins.unity`. `EidraForgeSceneBuilder.ConfigureZone` passte zwar
die Kamerabegrenzung an den Grundriss an (72×88, Mitte z=+6), ließ die
`ZoneBoundarySettings` aber unangetastet — dort stand weiter das
80×80-Quadrat um den Ursprung. Genau daraus zieht die Minimap ihr „Ende der
Karte" (`CreateMapEdgeBoundary` → `SetMapEdge`): Der Rand wurde wie in jeder
Außenzone gezeichnet und lag im Norden mitten im Verlies — der Grundriss
reicht laut `EidraForgeLayout` von X −36..36 und Z −38..+50.
`ForgeSceneCleanup` kennt dieselbe Ausdehnung längst als Konstante
(`BodenGroesse` 72×1×88), nur die Boundary-Komponente hatte nie jemand
nachgezogen. Beleg: neuer Szenentest rot mit „Center.z erwartet 6, war 0".

**Fix:** `EidraForgeSceneBuilder.ConfigureZone` konfiguriert die
`ZoneBoundarySettings` jetzt beim Neubau mit: Shape bleibt None (Bewegung
wie in allen Zonenszenen unbounded, die Wände halten die Figur),
Rechteck 72×88 mit Mitte (0|0|6), Safe-Area analog Grundriss −4 (68×84).
Szene per `Build Eidra Forge` neu gebaut.

**Tests:** `ZoneBoundaryMapEdgeTests.EidraForge_KartenrandDecktDenGanzenGrundriss`
(EditMode, öffnet die echte Szene — Textsuche beweist bei Binärszenen
nichts) rot→grün; gebündelter Lauf mit `EidraForgeSceneTests`,
`EidraForgeDungeonTests`, `EidraForgeLayoutTests`: 28/28 grün.

**Status: umgesetzt am 17.08.2026. In der Exe erst nach dem nächsten
Exe-Neubau sichtbar.**

---

## Umsetzungsstand Paket 1 (17.08.2026)

Beauftragt als erste Umsetzungsrunde; alle Nachweise rot→grün, sofern nicht
anders vermerkt. Abschlussläufe: **EditMode voll 1153/1153**, PlayMode-Nachlauf
der berührten Klassen 12/12 (voller PlayMode-Lauf zuvor: einzige Rotstelle war
der alte 20-Sekunden-Festschreibungstest, danach behoben). Neue Testdateien:
`Fixrunde031Tests` (EditMode, 8 Tests), `Fixrunde031PlayModeTests` (1 Test).

| Eintrag | Stand |
|---|---|
| F31-019 | **Umgesetzt.** Brot-Buff 120 s in Tabelle, Beschreibung und Asset; zwei Festschreibungstests (Edit- und PlayMode) auf 120 nachgezogen. |
| F31-002 | **Umgesetzt.** Stoff-Items und -Rezepte auf die fertigen ITEM_Wanderer*-Icons; Icon-Durchreichung in `Armor`-Helfer, Rezept-Builder und `ItemContentAssetBuilder` (fertige Icons direkt, ITEM_TMP_-Quellen weiter über den Spiegel-und-Tönungs-Weg — die erste, zu breite Regel hätte IronBar auf die CopperBar-Datei gelegt; vom vollen Lauf gefangen und enger gefasst). Platzhalter-PNGs bleiben liegen (Alt-Verweis aus `Assets/Sprite/`). |
| F31-016a | **Umgesetzt.** Schild-Prüfung vor der Zielprüfung; Steinhaut wirkt in Zonen ohne Boss. Teil 2 (normale Gegner als Fähigkeitsziel) bleibt offen. |
| F31-010 | **Umgesetzt.** `maximumCount` in Definition + Builder (Acker 2, alle übrigen 0), Grenzprüfung in `Evaluate` (ersetzte Gebäude zählen nicht mit), neues Ergebnis `BuildingLimitReached` mit Menütext. |
| F31-017 | **Umgesetzt.** `MarkLocalPosition` (0,2) statt des toten `MarkHeight`-Literals 0,07; liegt über der Bodenoberkante 0,14. |
| F31-020 | **Nicht umgesetzt — Diagnose widerlegt.** Der neue PlayMode-Test belegt: Rückkehr, Heilung und erneutes Aggro funktionieren ohne Teleport-Abkürzung. Regressionstest bleibt; Eintrag wartet auf eine reproduzierbare Situation (welcher Gegner, welches Gebiet, was ging dem Reset voraus). |

Sicherung vor den Builder-Läufen: `Backups/f31-paket1-20260817/` (Items,
Crafting). Testartefakte: `TestResults-f31-*.xml`, Logs `f31-*.log`.

---

## Umsetzungsstand Paket 2 (17.08.2026)

Abschlussläufe: EditMode voll 1155, alle Rotstellen behoben (Nachlauf der
Versions-Pin-Klassen 26/26); PlayMode voll 120 bestanden + 4 bekannte
Headless-Skips; die einzige Rotstelle war die in der Wunschsammlung
dokumentierte Kupferader-Zeitflake (Einzelwiederholung grün).

| Eintrag | Stand |
|---|---|
| F31-004 | **Umgesetzt.** Entfernt: `RecordGatheredUnits` + `gatheringExperiencePerUnit`, die nie gelesenen Erstbonus-Felder (`first/repeatCraftExperience`, `first/repeatBuildingExperience`) und die serialisierten Stufen-Arrays aus Kurvenklasse und Asset. Alle 20+ Test- und Runner-Aufrufer auf `RecordEnemyDefeated` (×5-Äquivalente) umgestellt. |
| F31-007 | **Umgesetzt.** Die vier stillgelegten Knoten sind aus dem Baum entfernt; kein Knoten trägt mehr `retired.never`. Baum hat 28 Knoten. |
| F31-011 | **Umgesetzt.** `catch_device` setzt Sägewerk + Seilerei voraus, trägt `feature.eidra_slot_2` selbst und steht in Liste und Anzeige hinter beiden; Knoten 12 entfällt. Spielstand-Migration 13→14 entfernt ihn mit Punktrückgabe (`MigrationVierzehn`-Test). Tester-Seed neu erzeugt. Bibel M8.6 und CHANGELOG nachgezogen. |

Offen aus dem Paket-Zuschnitt: F31-012+013 (Baumenü) — braucht die
bildbasierte Abnahme und ist der nächste Schritt.

---

## Umsetzungsstand Paket 2b — Baumenü (17.08.2026)

Abschluss der beauftragten Runde. Schlussläufe über alles: **EditMode
1159/1159**, **PlayMode 121 bestanden / 0 fehlgeschlagen** (5 bekannte
Headless-Skips, darunter der neue Abnahme-Capture).

| Eintrag | Stand |
|---|---|
| F31-012 | **Umgesetzt.** Katalog liegt jetzt in `BuildingList → CatalogViewport (RectMask2D) → CatalogContent (ScrollRect, horizontal, Clamped)`. Der ContentSizeFitter sitzt auf dem Content statt auf dem verankerten Panel — die Zeile kann den Bildschirm nicht mehr verlassen. Tastatur-/Gamepad-Auswahl zieht die gewählte Karte per `ScrollSelectedIntoView` in den Viewport. |
| F31-013 | **Umgesetzt.** Karte als vertikaler Stapel (200×~290): Kategorie-Streifen 26 px, Icon 88×88 oben mittig, Name volle Breite mit Wortumbruch, Kosten unten mit Umbruch, Auswahl als Bodenlinie. Werkbank und Lagerkiste zeigen erstmals ihr eigenes Gebäude: Engine-Render der Spielmodelle über das neue `BuildingIconRenderTool` (gemalte 1024er-Fassungen existieren für diese zwei nie — als Grafikauftrag offen). `IconPath`-Sonderfälle (Hammer/Brett) entfernt. |

Nachweise: vier neue Prefab-Tests in `Fixrunde031Tests` (Scroll+Maske,
vertikale Trennung, eigene Gebäude-Icons, Kopf ohne Überlagerung — alle
rot→grün) plus bestehende `BuildingMenu*Tests` grün. **Bildabnahme:**
`TempReview/f31-baumenue-abnahme.png` — echter In-Spiel-Screenshot über den
neuen PlayMode-Test `Fixrunde031MenuCaptureTests` (ScreenCapture schreibt im
Batchmode nichts; die Menü-Canvas hängt für den Schuss kurz an der
Spielkamera). Ein editor-seitiger Capture-Versuch ohne Frame-Loop blieb leer
und wurde wieder entfernt.

**Offene Punkte aus der Bildabnahme:** (1) Stilbruch der zwei
Engine-Render gegen die neun gemalten Icons — Grafikauftrag für gemalte
Fassungen von Werkbank und Lagerkiste anlegen, falls gewünscht. (2) Die
Kartentexte gesperrter Gebäude wirken im Capture gedimmt-weich — im echten
Fenster gegenprüfen. (3) Feinjustage von Kartenbreite/Schriftgrößen nach
Nutzerblick.

---

## Umsetzungsstand Paket 3, erste Hälfte (17.08.2026)

Schlussläufe: **EditMode 1165/1165**, **PlayMode 122 bestanden / 0
fehlgeschlagen** (5 bekannte Headless-Skips).

| Eintrag | Stand |
|---|---|
| F31-014 | **Geschlossen — kein Fehler.** Der neue Beweis-Test `Schmelzen_ZiehtErzAusDemLagerWennDerRucksackLeerIst` war auf Anhieb grün: Mit leerem Rucksack und Erz nur in der Lagerkiste zieht das Schmelzen nachweislich aus dem Lager ab. Die Meldung war die Entnahme-Reihenfolge (Rucksack zuerst), keine Fehlfunktion. Test bleibt als Beleg. |
| F31-015 | **Umgesetzt.** Ursache war eine Doppelrotation: Der Facing-Gierwinkel ist ein Welt-Winkel, `modelRoot` hängt aber unter der vom Controller gedrehten Wurzel — nach Osten 90° daneben, nach Süden exakt entgegengesetzt. Beim Wanderer unauffällig (Wurzel rotiert nie), bei 2D-Sprites wirkungslos (Billboards gieren nicht). Fix: `CreatureMeshPresentation.ModelLocalYaw` kompensiert die Wurzeldrehung (rot→grün); In-Welt-Captures neu erzeugt (`TempReview/f31-kreaturen/`), Granitpanzer wendet dem Ziel die Front zu. |
| F31-018 | **Umgesetzt.** `ProtectionRules.ArmorHealthBonus` (Schutz × 100, bewusst ohne die 60-%-Kappe), `Damageable.SetMaxHealthBonus` (Anlegen heilt nicht, Ablegen/Bruch klemmt), Summierer im `EquipmentDurabilityService`, Haken in `RefreshArmor` (deckt Anlegen, Ablegen und Haltbarkeitsbruch über den bestehenden Änderungspfad). Bibel M13.3-Umfeld ergänzt. |
| F31-008 | **Umgesetzt.** `BuildingDoorContentBuilder` hängt das Türblatt (5 Planken, Strebe, Griff, Schloss, Scharniere) unter eine Angel `Geometry_A14/DoorBlade` (Fabrik-Vertrag bleibt erfüllt; `VisualAssetTests` auf Angel-Pfade nachgezogen) und autoriert Sensor + `BuildingDoorView`: Tür öffnet automatisch beim Annähern der Figur (Trigger, 2,1 m), schaltet den Sperr-Collider, schwenkt das Blatt 105° und schließt nach dem Verlassen. Kein Spielstandfeld (§17 unberührt), NavMeshObstacle unverändert. PlayMode-Nachweis: öffnen → durchgehen → schließen → sperrt wieder. |

**Testlandschafts-Lektion aus dem Tür-Nachweis:** Batch-PlayMode-Frames haben
winzige `deltaTime`-Werte — Frame- und Echtzeitfristen kippen bei
zeitbasierten Animationen. Wartezyklen in Spielzeit budgetieren
(`zeit += Time.deltaTime`), Bewegung mit fester Schrittweite je Frame.

**Noch offen aus Paket 3:** F31-001 (Sichtverdeckung — Shader-Risikopunkt
zuerst), F31-003 (Kupferader-Bildabgleich), F31-009 (Gegnerbesetzung +
Kistenwachen), F31-016b (Eidra-Fähigkeiten gegen normale Gegner). Danach
Paket 4 (F31-006 Questsystem, eigener Entwurf).

---

## Umsetzungsstand Paket 3, zweite Hälfte (17.08.2026)

| Eintrag | Stand |
|---|---|
| F31-016b | **Umgesetzt.** `EidraTeamController` erwirbt beim Tastendruck das nächste lebende Ziel in Fähigkeitsreichweite aus der `CombatTargetRegistry` (`TryAcquireFieldTarget`, §8-konform nur bei Eingabe); das Kampf-Tor akzeptiert Feldgegner (`TargetBattleActive`). Ein laufender Bosskampf hat Vorfahrt — dort bleibt das Ziel der Boss. `BackMark` bleibt bewusst Boss-exklusiv (nur `BossController` trägt `MarkBack`). PlayMode-Nachweis rot→grün: Felsbrecher gegen einen Wildling — kein „KEIN GÜLTIGES ZIEL", Stagger steigt. |
| F31-003 | **Umgesetzt, Bildabgleich beim Nutzer.** `T1ResourceVisualBuilder`: `CopperVein_Exhausted` nutzt jetzt die flache Verbraucht-Formel wie der Stein (4 statt 7 Teile, Verbraucht-Skala im Stein-Verhältnis) — vorher behielt die Ader den vollen hohen Cluster und war vom aktiven Zustand nicht unterscheidbar (Messung: beide 1,22 hoch; jetzt 0,58 gegen 1,22). Silhouetten-Test rot→grün; Vergleichsbild `TempReview/f31-kupferader-vergleich.png` (neuer Renderer im `BuildingIconRenderTool`). Der ungenutzte `CopperOreHero`-Meshsatz bleibt als offener Aufräumfall notiert. |

**Zwei erwartbare Folgen des Ader-Neubaus, beide über die etablierten Wege
behoben:** die Höhentabelle (`VisualScaleTableBuilder`: 1,3 → 0,581) und die
erneut abgeworfene `CopperMiningVisualFeedback`-Komponente
(`CopperMiningFeedbackRebuilder` — exakt die dokumentierte Falle aus der
Wunschrunde). Nachläufe: EditMode 29/29, PlayMode-Interaktionen 9/9; zuvor
volle Suiten EditMode 1165(+1 Höhen-Pin)/1166, PlayMode 122/128 (+Feedback-Pin,
5 Skips) — beide Pins danach grün.

**Noch offen:** F31-001 (Sichtverdeckung — als Erstes die Alpha-Probe am
Weltshader), F31-009 (Gegnerbesetzung nach Gefahrenstufe + Kistenwachen),
Paket 4 (F31-006 Questsystem mit eigenem Entwurf).

**Nachschärfung F31-003 (17.08.2026, Nutzer-Bildabgleich):** Der erste flache
Rest trug noch die Kupfer-Akzentfarbe (jeder dritte Fels orange) und las sich
weiter als erzhaltig. Jetzt wie beim Stein: Wirtsfels + heller Wirtsfels
(`WirtHell`), kein Erz-Orange mehr. Neuer Wächter
`Kupferader_AbgebautTraegtKeineErzfarbe` (rot→grün); Läufe 86/86,
Vergleichsbild erneuert.

## Umsetzungsstand F31-001 — Sichtverdeckung (17.08.2026, nachts)

**F31-001 umgesetzt.** Der Kernbefund widerlegt die eigene Sammlung erneut:
Das vorhandene System (`ActorOcclusionTransparency`) war nicht „auf Bäume
auszuweiten" — es hat **in Zonen nie funktioniert**. `IsActorHierarchy`
verglich gegen `transform.root`; da Zoneninhalt unter einer gemeinsamen
Szenenwurzel hängt, galt ALLES (Bäume, Boden, Gegner) als Figuren-Hierarchie,
und der Dienst hat nie einen einzigen Verdecker angefragt. Der Fix prüft
echte Verwandtschaft (`IsChildOf` in beide Richtungen) und aktiviert die
Ausblendung damit erstmals überall — auch an Verlies-/Schmiedewänden und der
neuen Tür (F31-008), die denselben Weltshader tragen.

Die Kette dahinter, jeder Punkt einzeln rot→grün:

- **Fade-Zwilling** `Eidren/World/VertexLitFade`: Der Weltshader ist bewusst
  opak (kein Blend, kein Alphakanal — die Alpha-Probe aus der Sammlung
  bestätigte den Risikopunkt). `CreateFadeMaterial` tauscht Weltmaterialien
  zur Laufzeit auf den Zwilling (`_Color.a` steuert die Deckkraft, Queue
  3000, kein ZWrite/ShadowCaster); Fremdmaterialien behalten den
  URP-Transparenzumbau.
- **Verdecker-Regel nur über Höhe** (>= 1,35 m): Die alte Zusatzbedingung
  (Spanne >= 1,6 m) ließ jeden Baum (Kollider 1,2 m breit) durchfallen.
- **Zweiter Sichtstrahl auf Fußhöhe** (0,12 m) neben dem Mittelstrahl: Bei
  der steilen Kamera (52°) läuft der Mittelstrahl über niedrige Stammkapseln
  hinweg, obwohl die Krone die Figur verdeckt.
- **Baum-Kollider tauglich gemacht:** Kapseln standen halb im Boden
  (Zentrum 0/0/0 statt geerdet) und endeten bei 2,4 m unter einer 6-m-Krone.
  Builder erdet die Kapseln, Höhentabelle hebt Baum auf 5,4 m / Harthaube
  auf 6 m; Knoten-Prefabs über `BuildResourceDefinitions` neu gebaut
  (inkl. der dokumentierten Folge: `CopperMiningFeedbackRebuilder`).

Wächter: `Sichtverdeckung_HoheSchlankeVerdeckerZaehlen` und
`Sichtverdeckung_WeltmaterialWechseltAufFadeZwilling` (EditMode) sowie der
Bildbeleg-Test `Fixrunde031OcclusionCaptureTests.BaumVorDerFigur_WirdAusgeblendet`
(PlayMode, braucht Grafikgerät): Figur hinter Baum in Grünwald, erwartet
Fade-Material + Alpha < 0,5, schreibt `TempReview/f31-sichtverdeckung-baum.png`.
Der Wunsch „stellenweise ausblenden" ist als Ganzobjekt-Fade umgesetzt —
weicher Übergang (5,5 Alpha/s) statt Lochmaske; wirkt ruhiger und braucht
keinen Screen-Space-Eingriff.

Schlussläufe nach Aufräumen der Diagnose-Instrumentierung: EditMode
**1169/1169**, PlayMode **123/0** (6 Skips, Grafiktests). Damit sind
**17 von 19 Einträgen** erledigt; offen: F31-009 (Gegnerbesetzung),
F31-006 (Questsystem).

## Umsetzungsstand F31-009 — Gegnerbesetzung (17.08.2026, nachts)

**F31-009 umgesetzt.** Die Besetzung folgt jetzt der Gefahrenstufe der
Weltkarte über die Staffel **3 x Gefahr - 1** (`ZoneEnemyPopulationRules`):
Grünwald 5, Steinbruch/Nebelmoor 8, Dämmerhain/Schleiermoor 11,
Glutruinen/Grauklüfte 14. Ein Weltdurchlauf bringt 2.400 statt 1.035 EP
(Faktor 2,3 — zahlt auf F31-006 ein). Zusammensetzung gestaffelt: Gefahr 2
rein Wildling, Gefahr 3 je ein thematisches Gebietstier (Steinbruch:
Rissling, Nebelmoor: Moorwerfer) + Fang-Eidra, ab Gefahr 4 überwiegen die
Tier-2-Gegner (9/11 bzw. 12/14); Glutruinen risslinglastig als Boss-Vorfeld,
Grauklüfte granitlastig.

**Drei strukturelle Befunde nebenbei behoben:**

- **Drei Builder schrieben dasselbe Array in unterschiedlicher Semantik**
  (Wildling: ersetzen auf 1 Zeile, TierTwo: ersetzen auf 5, EidraCapture:
  Upsert) — die Besetzung hing von der Laufreihenfolge ab; die 2-Wildling-
  Glutruinen waren genau so ein Unfall. Jetzt schreibt zentral der
  `ZoneEnemyPopulationBuilder`, die alten Pfade delegieren.
- **Gegner waren fest in die Szenen gebacken** (9 Kandidatenplätze, ohne
  Bezug zu den pro Lauf gewürfelten Kisten). Jetzt stellt der
  `ZoneEnemyPopulator` die Besetzung zur Laufzeit auf — nach Kisten,
  Ressourcen und Eidra, mit deren Plätzen als Ausweichgrund.
- **Der Wildling hatte nie ein eigenes Prefab** — seine Instanzen lagen nur
  ausgerollt in den Szenen (der alte Bake ersetzte das leere Feld still).
  Neu gebaut aus der Riftling-Vorlage (Kopie des einstigen Wildling-Prefabs)
  + `CreatureEnemyRewire` auf Wildling_3D; `WildlingContentBuilder.
  RebuildPrefab` hält den Weg fest.

**Kistenwachen:** Ring 3,5–7 m (Untergrenze: Kiste nicht blockieren;
Obergrenze: unter der Witterungsreichweite 10, damit die Wache den Zugriff
bemerkt). Höchstens eine Wache je Kiste und höchstens die Hälfte der
Besetzung; der Rest verteilt sich frei. Wachen stehen an den TATSÄCHLICH
gewürfelten Kisten des Laufs (2–6 aus 10 Punkten). Kisten im Bossbereich
bekommen keine Feldwache — dort ist der Boss die Wache. Ringgeometrie
multi-seed-geprüft (500 Saaten).

**Bossbereich gesperrt:** Feldgegner meiden die BossArea (Collider-Bounds
+ 4 m Puffer) — sonst zieht der Bosskampf Zusatzgegner; genau das hatte den
Garon-Leinen-Test gestört.

**Rebuild-Falle auf Szenenebene dokumentiert und entschärft:** Der
Struktur-Rebuild wirft die AreaArt ab (Rohbau-Boden, alte Spawns) und
setzte die BuildSettings auf 11 Kern-Szenen zurück — die EidraForge flog
aus der Build-Liste. AreaArt per `AreaArtSceneBuilder.BuildAllForAutomation`
wiederhergestellt; die Schmiede steht jetzt fest in den `OrderedScenePaths`.

**Testfolgen:** Die alten Szenen-Zählpins (3/2/2/2) prüften eingebackene
Gegner — umgestellt: Szenen tragen 0 gebackene Gegner (Rückfall-Wache),
die Laufzeit-Zählungen erwarten 5/6/6/14. Neue Nachweise:
`Fixrunde031EnemyPopulationTests` (6, EditMode: Staffel, Reinheit Gefahr 2,
T2-Übergewicht ab Gefahr 4, Ring, Budget, Prefab-Pflicht) und
`Fixrunde031EnemyPopulationPlayTests` (PlayMode: Glutruinen stellen 14 auf,
Wachen im Ring, Bossbereich frei, alle auf dem NavMesh; schreibt den
Bildbeleg `TempReview/f31-gegnerbesetzung-glutruinen.png`).

Schlussläufe: EditMode **1175/1175**, PlayMode **123 grün + 6 Skips**; der
eine Rote war die dokumentierte Kupfer-Zeitflake
(`CopperVein_Hold225SecondsCollectsExactlyOnce`), isoliert grün. Damit sind **18 von 19 Einträgen** erledigt; offen: F31-006
(Questsystem).

## Umsetzungsstand F31-006 — Questsystem (18.08.2026, nachts)

**F31-006 umgesetzt — damit sind alle 19 Einträge der Sammlung erledigt.**
Entwurf zuerst (ENTWURF_QUESTSYSTEM_V031.md), dann Umsetzung Schicht für
Schicht rot→grün:

- **Kette:** 17 Schritte von „Ernte 2 Faserpflanzen" bis „Fange deinen
  ersten Eidra", zusammen **940 EP** (Tabelle in Entwurf und Bibel M13.4).
  Katalog `QuestChain_V01` über `QuestChainContentBuilder`; die Sense steht
  auf ausdrücklichen Wunsch in der Kette.
- **Logik:** `QuestProgressService` (reines C#, Data-Typen in der
  Data-Schicht — Core verweist auf Data, nicht umgekehrt): erzwungene
  Reihenfolge, Zählschritte, EP je Abschluss über
  `PlayerProgression.RecordQuestStepCompleted`, Abschlussereignis.
- **Verdrahtung ohne neue Spielfluss-Eingriffe:** Die Progression feuert
  Quest-Ereignisse aus ihren bestehenden Record-Methoden (Rezept, Gebäude,
  Knoten mit neuer Katalog-Id im `ResourceCollectionResult`, Gegner); das
  Roster meldet den Fang. Der ServiceRoot bindet alles zusammen.
- **Persistenz:** Spielstand Version **15** (`SaveQuestData`, Migration
  Fall 14, `SaveQuestMapper`, `SaveAdvancementBridge`). Altbestände: ein
  vorhandener Eidra schließt die Kette ab; erledigte Rezept- und
  Gebäudeschritte werden beim Erreichen ohne EP übersprungen — nur das
  Fanggerät zu besitzen schließt bewusst NICHT ab (der Fang-Schritt samt
  seiner 150 EP bleibt sichtbar). Zählschritte sind nicht ableitbar und
  bleiben offen.
- **HUD:** Aufgaben-Label im Kampf-HUD (`QuestHudBuilder`,
  `QuestHudPresenter` — Text nur bei Änderung neu gebaut, Dienst kommt per
  Configure aus der Composition-Schicht); nach dem Abschluss verabschiedet
  sich das Label und blendet dauerhaft aus.

Nachweise: `Fixrunde031QuestTests` (9, reine Logik inkl. Altbestand),
`Fixrunde031QuestKatalogTests` (7, Katalog + Ereignisse + Save v15),
`Fixrunde031QuestPlayTests` (PlayMode: Kette startet im HUD, echte
Ereignisse rücken vor und vergeben EP). Etappenende: EditMode
**1191/1191** (ein übersehener Versionspin der v13-Migration auf 15
gehoben), PlayMode **125/0** + 6 Skips.

**Damit ist die Fixrunde v0.3.1 vollständig: 19 von 19 Einträgen
umgesetzt** (F31-005 war zugunsten des Questsystems gestrichen worden).
