# Kreaturenserie 3D — Arbeitsstand und Einstiegspunkt

**Stand: 14. August 2026 — Serie gebaut UND eingebaut, Testlandschaft grün**

Seit dem 14.08.: EditMode 1009/1009, PlayMode 120/120 (Details und alle
Werkzeuge im Abschnitt „Restbaseline-Triage" der
`KREATUREN_EINBAU_REFERENZ.md`). Sichtpolitur der Serie erledigt:
Garon-Heck-Goldornament, EmberEater-Glutader über den First, Terrock-Moos
plus Kruppen-Smaragd — Quellen, GLB-Exporte und Captures abgenommen.

Einstiegsdokument für die Fortsetzung der Arbeit. Wer hier anfängt, braucht
sonst nichts aus dem bisherigen Gesprächsverlauf.

## Was fertig ist

Spalte „gebaut" ist die Reihenfolge der Entstehung, nicht die Listenordnung
aus `V02ActorVisualBuilder`.

| gebaut | Figur | Höhe | Soll | Dreiecke | Bauart | Ordner |
| ---: | --- | ---: | ---: | ---: | --- | --- |
| — | Wanderer | 2,19 m | — | — | Zweibeiner | `Art/Actors/Player/Wanderer3D/` |
| — | Wildling | 1,645 m | — | 1.512 | Zweibeiner | `Art/Actors/Wildling/Wildling3D/` |
| 1 | Riftling | 1,700 m | 1,70 | 1.472 | Zweibeiner | `Art/Actors/Riftling/Riftling3D/` |
| 2 | MoorThrower | 1,900 m | 1,90 | 1.648 | Zweibeiner | `Art/Actors/MoorThrower/MoorThrower3D/` |
| 3 | RiftGuardian | 3,000 m | 3,00 | 1.056 | Zweibeiner | `Art/Actors/RiftGuardian/RiftGuardian3D/` |
| 4 | ForgeGuardian | 2,500 m | 2,50 | 1.204 | Zweibeiner | `Art/Actors/ForgeGuardian/ForgeGuardian3D/` |
| 5 | SealGuardian | 3,000 m | 3,00 | 844 | Zweibeiner | `Art/Actors/SealGuardian/SealGuardian3D/` |
| 6 | CoreGuardian | 4,500 m | 4,50 | 1.192 | Zweibeiner | `Art/Actors/CoreGuardian/CoreGuardian3D/` |
| 7 | EmberEater | 1,600 m | 1,60 | 1.310 | **Knöchelgänger** | `Art/Actors/EmberEater/EmberEater3D/` |
| 8 | RootCharger | 2,200 m | 2,20 | 1.946 | **Vierbeiner** | `Art/Actors/RootCharger/RootCharger3D/` |
| 9 | GraniteShell | 2,400 m | 2,40 | 1.530 | **Vierbeiner** | `Art/Actors/GraniteShell/GraniteShell3D/` |
| 10 | AshRunner | 1,800 m | 1,80 | 1.536 | **Vierbeiner + Schwanz** | `Art/Actors/AshRunner/AshRunner3D/` |
| 11 | Ignivar | 1,000 m | 1,00 | 2.046 | **Vierbeiner + Schwanz** | `Art/Actors/Ignivar/Ignivar3D/` |
| 12 | Noctarion | 1,000 m | 1,00 | 1.502 | **Vierbeiner + Schwanz** | `Art/Actors/Noctarion/Noctarion3D/` |
| 13 | Terrock | 1,000 m | 1,00 | 1.644 | **Vierbeiner + Schwanz** | `Art/Actors/Terrock/Terrock3D/` |
| 14 | Garon | 4,500 m | 4,50 | 1.688 | **Vierbeiner, Boss** | `Art/Actors/Garon/Garon3D/` |

Alle treffen ihre Sollhöhe auf **0,0 %** genau (dem Wildling ist keine
zugeordnet). Alle bestehen `verify_gltf.js`. Clipzahl: 8 bei den V02-Kreaturen, **10 bei Noctarion und Terrock**, **9 beim
Garon** — Eidra und Boss haben eigene Zustandssätze. Jede Figur hat ein eigenes README mit
Messwerten, Herleitung der Taktung und offenen Punkten.

Knochenzahl: 17 bei allen Zweibeinern und dem Knöchelgänger, **19 bei den
Vierbeinern**, **22 bei AshRunner und Ignivar** — dort kommen drei
Schwanzknochen dazu.

## Die Serie ist gebaut

**Alle 16 Figuren stehen** — acht Zweibeiner, ein Knöchelgänger, sieben
Vierbeiner. Jede trifft ihre Sollhöhe auf 0,0 %, jede besteht
`verify_gltf.js`, jede hat ein eigenes README mit Messwerten, Herleitung der
Taktung, Bodenkontakt-Tabelle und offenen Punkten.

Drei Rigs tragen alles: 17 Knochen für Zweibeiner und Knöchelgänger,
19 für Vierbeiner, 22 mit Schwanz. Die Werkzeugkette (`export_gltf.js`,
`verify_gltf.js`, `render.js`) hat alle drei ohne strukturelle Änderung
mitgemacht — nur zwei Datenstellen mussten je Figur angefasst werden:
die Cliptabelle `CLIP_EXPORT` und der fest benannte Prüfclip in
`verify_gltf.js`.

## Was offen bleibt

**Der Einbau in Unity ist abgeschlossen (13.08.2026)** — Darstellungs-Prefabs
für **15** Figuren gebaut (die 14 der Serie **plus den Wildling**, der am
Abend nachgezogen wurde: Erscheinen-Clip aus riftling.html portiert, jetzt
8 Clips), elf Gegner-Prefabs umgestellt (zehn Gegner + Boss), die drei Eidra
über Laufzeit-Wrapper, und **27 szenenplatzierte Gegner** (12 Wildlinge und
15 ausgerollte 2D-Kopien von Riftling/RootCharger/MoorThrower/GraniteShell in
den TierTwo-Zonen) über den Szenen-Umbau `WildlingSceneRewire`. Einzelheiten
in `KREATUREN_EINBAU_REFERENZ.md`. Stand der Voraussetzungen, geprüft am
12.08.2026 (historisch):

**Blocker 1 ist erledigt.** Die Notiz „`ITEM_TMP_SmithingMark.png` fehlt und
legt `CraftingContentBuilder.BuildAll()` still" ist **überholt**. Kein
Quelltext verweist mehr auf diesen Namen; `IgnivarContentBuilder` lädt
`Assets/_Game/Art/Items/ITEM_SmithingMark.png`, und die Datei ist vorhanden.

**Blocker 2 besteht.** `Assets/_Game/Prefabs/Actors/2D/` existiert **gar
nicht**. Damit fehlen drei Dateien, auf die Quelltext verweist:

| Datei | wer verlangt sie | Folge |
| --- | --- | --- |
| `Wildling_2D.prefab` | `V02ActorVisualBuilder` Zeile 74 | wirft `FileNotFoundException`, Builder tot |
| `Player_2D.prefab` | `VisualScaleAssetMap` | Eintrag `visual.player` zeigt ins Leere |
| `Garon_2D.prefab` | `VisualScaleAssetMap` | Eintrag `visual.boss_garon` zeigt ins Leere |

Vorhanden sind nur `Resources/Prefabs/Actors/2D/Noctarion_2D.prefab` und
`Terrock_2D.prefab` — die beiden, deren Raster ich auslesen konnte.

**Das erklärt vermutlich auch zwei der Sprite-Testfehlschläge:**
`WildlingSpriteVisualTests` und `GaronSpriteVisualTests` laden ihr Prefab aus
`Assets/_Game/Prefabs/Actors/2D/` und finden dort nichts. Beim Noctarion liegt
es anders — sein Prefab existiert, dort scheitert der Test an den Atlasmaßen.
**Zwei verschiedene Ursachen**, nicht eine.

### Und der Einbau ist größer, als diese Notiz bisher behauptet hat

`MeshActorPresentation` ist **für den Wanderer gebaut**, nicht für Kreaturen:

- `ResolveClipName(stem, stance)` ist auf die Spielernamen verdrahtet —
  `Ruhe_<Haltung>`, `Laufen_<Haltung>`, `Angriff_Hammer`, `Angriff_Dolche`,
  `Angriff_Speer`. Die Kreaturen haben weder Haltungen noch diese Namen.
- Wo ein Clip fehlt, fällt die Schicht auf **prozedurale Posen** zurück
  (Treffer, Taumeln, Tod, Erscheinen, Ausweichen, Ernten) — weil die
  `Wanderer.glb` diese Clips nicht hat. Die Kreaturen haben sie alle als
  echte Clips; der Rückfall wäre bei ihnen ein Rückschritt.
- `ActorVisualState` hat **acht** Werte: Idle, Move, Ability1, Ability2, Hit,
  Stagger, Death, Appear. Für die V02-Kreaturen geht das auf. **Für die Eidra
  und den Boss nicht:** Noctarion und Terrock haben zehn Zustände (Flucht und
  zwei Fähigkeiten haben keinen Platz, Death keinen Clip), Garon neun
  (Ansturm und Rückkehr haben keinen Platz, Appear keinen Clip).

**Entschieden und umgesetzt am 13.08.2026:** eine eigene Auflösungsschicht
für Kreaturen neben der des Wanderers. `ActorVisualState` bleibt unangetastet.

Der Einbau ist einmal vollständig am **Riftling** durchgespielt und in
**`KREATUREN_EINBAU_REFERENZ.md`** beschrieben. Kurz:

- **`CreatureMeshPresentation`** (Schwesterklasse zu `MeshActorPresentation`):
  ein Zustand, ein Clipname, kein prozeduraler Ersatz. Die zusätzlichen
  Zustände der Eidra und des Bosses laufen über `SetAuthoredState(stem)`;
  `StemToClip` übersetzt alle 18 Sprite-Stämme der Serie.
- **`CreatureMeshBuilder`** baut die Darstellungs-Prefabs, idempotent. Erster
  Schritt je Figur ist der erzwungene **Legacy-Import** — glTFast importiert
  GLB sonst mit Mecanim, und die Schicht braucht Legacy-Clips. Derselbe
  Befund wie beim Wanderer.
- Referenzlauf am Riftling, danach `BuildAll()`: **alle 14 Prefabs** liegen in
  `Prefabs/Actors/3D/`. Jedes trägt die Schicht, die richtige Sollhöhe und
  alle Clips seiner GLB — geprüft, **27 von 27 Testfällen bestanden**.

**Die GLB-Dateien waren vorher nie importiert** — keine der 15 hatte eine
`.meta`. Der erste Builderlauf hat den Import ausgelöst.

**Entschieden und umgesetzt am 13.08.2026:** die Gegner werden von 2D auf 3D
**umgestellt, nicht ergänzt**. `ActorPresentationLocator.Find` sucht mit
`includeInactive: true` und nimmt die erste Darstellung im Baum — ein
Nebeneinander hinge an der Kindreihenfolge und wäre nicht stabil. Deaktivieren
genügt aus demselben Grund nicht.

`CreatureEnemyRewire` macht das, idempotent und auf den `Visual`-Knoten
begrenzt. Am **Riftling** durchgespielt: Sprite-Darstellung entfernt,
`Riftling_3D.prefab` als `Mesh3D` eingehängt, `SpriteActorAnimator`
umgehängt; Bodenschatten, Anker, Trefferzonen und Statusbalken unberührt, weil
keiner von ihnen am Aktorbild hängt.

**Seit dem 13.08.2026 sind alle 14 im Spiel:** nach der Abnahme des
Riftling-Referenzfalls kamen die übrigen zehn Gegner-Prefabs samt Garon
(`RewireAll`) und die drei Eidra über Laufzeit-Wrapper
(`EidraVisual3DBuilder`, Pfadtabellen in beiden Eidra-Controllern).
Die Sichtprüfung in Unity deckte dabei einen Serienfehler auf — einseitige
`platte()`-Flächen, im Quell-Viewer unsichtbar —, der in allen 14 Quellen
behoben wurde. Beides in `KREATUREN_EINBAU_REFERENZ.md` beschrieben.

Beim Einbau zu beachten: Noctarion, Terrock und Garon tragen **andere
Clipnamen** als ihre Sprite-Zustände (`Wildangriff`, `Flucht`,
`Schattenschritt`, `Felsbrecher`, `Frontschlag`, `Wirbel`, `Ansturm`,
`Rückkehr`). Die Namensgleichheit der V02-Kreaturen gilt dort nicht.

### Drei Annahmen, die bei den Eidra und beim Boss nicht mehr tragen

Am Noctarion aufgefallen, gilt für Terrock und Garon gleichermaßen:

**1. Der Atlas ist nicht 4×4.** Beim Noctarion 4×20, Zelle 320×205 px. Das
jeweilige `*_2D.prefab` nennt die Werte im Klartext (`runtimeCellWidth`,
`runtimeCellHeight`, `visibleHeightPixels`) — das ist die verlässlichste
Quelle, verlässlicher als jede Messung und als die Sprite-Tests.

**2. Die Sollhöhe steht nicht im `V02ActorVisualBuilder`,** sondern in
`Resources/Data/VisualScaleTable.asset`. Dort steht außerdem ein
**`widthBudget`** — eine verbindliche Obergrenze für die Breite. Das ist die
Antwort auf die Frage, die seit dem EmberEater offen war: es gibt eine
Gestaltungsvorgabe für die Breite, und sie ist nicht der `AgentRadius`.

| Eintrag | Höhe | Breitenbudget | gilt für |
| --- | ---: | ---: | --- |
| `visual.eidra_small` | 1,00 m | 1,40 m | Terrock, Noctarion |
| `visual.boss_garon` | 4,50 m | 6,30 m | Garon |
| `visual.wildling` | 1,90 m | 2,66 m | Wildling |
| `visual.player` | 2,00 m | 1,80 m | Wanderer |

**3. Die Zustände sind figurabhängig.** Noctarion und Terrock haben zehn und
keinen Tod; Garon hat neun und keinen `appear`. Die Cliptabelle `CLIP_EXPORT`
in `export_gltf.js` ist eine Datenliste und ließ sich ohne Eingriff in den
Export austauschen. `verify_gltf.js` musste angefasst werden — der
Skinning-Vergleich lief fest auf einen Clip namens `Angriff`.

**Und ein Befund vom Terrock: die Clipschicht ist beim Erben kein reiner
Namensraum.** Die acht Kampfclips trugen unverändert von Noctarion zu
Terrock. Die beiden **Fähigkeiten** nicht: `shadowstep` ist ein Versetzen,
`rockbreaker` ein Doppelschlag; `backmark` stößt ein Zeichen nach vorn,
`stonehide` rollt die Figur ein. Ein Umbenennen allein hätte einen Terrock
ergeben, der sich beim Felsbrechen duckt.

### Ein Befund aus dem AshRunner: das Rig verträgt Zusatzknochen

Drei Schwanzknochen wurden **hinten angehängt**, nicht in der Mitte
eingeschoben. Damit bleiben alle bestehenden Indizes gültig — und die
Elternauflösung in `updateBones()`, die Sohlenliste, der Bodenschatten und
die gesamte Clipschicht liefen unverändert weiter. `export_gltf.js`,
`verify_gltf.js` und `render.js` brauchten **keine Änderung**.

Der Helfer `schwanz(neigen, wedeln, rollen)` setzt alle drei Knochen mit der
Übersetzung 1 : 1,6 : 2,2, damit die Bewegung nachläuft statt zu schwenken.

### Ein Befund aus dem Ignivar: `ranke()` gibt ihren Endpunkt zurück

Eine Flammenzunge braucht zwei Ketten übereinander — dunkler Fuß, heller
Kern. Der Anschlusspunkt lässt sich **nicht schätzen**: `ranke()` biegt bei
jedem Glied ab, der Endpunkt ist nicht vorhersagbar. Die hellen Spitzen
standen frei neben ihren Flammen.

`ranke()` liefert deshalb jetzt `{ p, d }` — Endpunkt und Endrichtung in
Weltmaß — und ist in `rankeAb()` aufgeteilt, das dort ansetzt. Bestehende
Aufrufe sind unberührt; sie ignorieren den Rückgabewert.

**Falle:** `rankeAb()` erwartet die Länge in ungeskalierten Einheiten und
multipliziert intern mit `SY`. Beim Anschluss nicht noch einmal selbst
skalieren.

## Was die Vererbung tatsächlich spart

Am GraniteShell gemessen, dem ersten Erben des Vierbeiner-Rigs:

**Unverändert übernommen, Byte für Byte:** Rig-Struktur (Knochennamen,
Reihenfolge, Elternkette), `lauf` / `setzeLauf` / `alleLaeufe`, die Clips
`Ruhe`, `Treffer` und `Taumeln`, die Helfer `loftZ`, `ringPtsZ`, `platte` und
`ranke`, `plantFeet`, `skin` und der gesamte WebGL-Teil.

**Angepasst, Struktur blieb:** die 19 Drehpunkte, `Gehen` (kleinere
Ausschläge), `Tod` und `Erscheinen` (flachere Neigungen), die acht
Sohlenpunkte, die Taktung.

**Neu:** Palette, gesamte Geometrie, `Telegraph` und `Angriff` (aus einem
Ansturm wurde ein Schlag), ein figurspezifischer Helfer `kuppelReihe()`.

Damit trägt die Vorhersage aus `KREATURENSERIE_BEFUND.md`: je Erbfigur neu
sind Palette, Proportionen, Geometrie und Taktung — Skelett, Clipgerüst und
Werkzeuge bleiben.

**Eine Einschränkung, die dort nicht stand:** Clips, die den Rumpf stark
neigen (`Tod`, `Erscheinen`, ein zuschlagender `Angriff`), müssen je Figur
nachgemessen werden. `plantFeet()` fängt das nicht ab, weil nur die Pranken
getastet werden.

Zwei Male aufgetreten, aus zwei verschiedenen Gründen:

- **GraniteShell** trägt den Schädel tiefer als die Vorlage (y 0,44 gegen
  0,52). Drei Clips rammten ihn in den Boden, bis zu 563 mm.
- **Ignivar** ist mit 1,00 m schlicht kleiner. Dieselben Winkel wiegen dort
  ungleich schwerer: 327 mm im `Tod` sind ein Drittel der Figurenhöhe.

Beide Male half nur, die geerbten Amplituden auf rund ein Drittel zu nehmen
und neu zu messen.

## Arbeitsablauf je Modell

1. **Daten lesen** aus `Data/Enemies/<Name>.asset` beziehungsweise
   `Data/Enemies/Forge/` oder `Data/Bosses/` — Bosse haben ein anderes Schema.
   Sollhöhe aus `V02ActorVisualBuilder`.
2. **Ordner anlegen**, Werkzeuge und die passende Vorlagendatei kopieren,
   Namen in den Skripten ersetzen.
3. **Farben messen**: `node messung.js <png> <cols> <rows> [col] [row]`.
   Atlasraster prüfen — V02 ist 4×4 auf 1024×1024, die Altfiguren sind hoch.
4. **Silhouette messen** — Zeilenprofil der Vorlage, siehe unten.
5. **`MESSWERTE.md` schreiben** mit Palette, Gestaltbefund, Profil und
   Zeitankern.
6. **Palette, `SOLLHOEHE`, Clips setzen.**
7. **Geometrie bauen**, Teil für Teil — nach jedem Teil rendern.
8. **Höhe und Grundriss messen**, `ISTHOEHE` korrigieren, neu exportieren.
9. **Ein tief getragener Kopf rammt sich in den Boden — bei vier von fünf
Vierbeinern aufgetreten.** GraniteShell (−563 mm), Ignivar (−327 mm),
Terrock (−30 mm) und Garon (−158 mm) hatten in mindestens einem Clip Kopf
oder Kiefer als tiefsten Punkt. `plantFeet()` meldet es **nicht**, weil nur
die Pranken getastet werden. Prüfen, bevor eine Figur abgenommen wird.

**Bodenkontakt über alle Clips messen.**
10. **Verify, Abnahmebilder, README.**

## Regeln, die aus Fehlern entstanden sind

**Der Quell-Viewer verbirgt einseitige Flächen — Sichtprüfung in Unity, auch
von hinten.** Der Viewer zeichnet Backfaces, Unity cullt sie. `platte()` baute
bis zum 13.08.2026 keine Rückfläche; der Riftling war in Unity von hinten
kopflos, beim Noctarion sah man in den offenen Rumpf — und JEDES
Viewer-Render, auch `abnahme_hinten.png`, sah korrekt aus. Kein Skript und
keine Viewer-Abnahme kann diese Klasse finden; nur ein Unity-Render der
Rückseite (`CreatureSichtCapture.CaptureAlle`, Lauf ohne `-nographics`).

**Prüfskripte messen die innere Stimmigkeit, nicht die Ähnlichkeit.** Der erste
EmberEater bestand `verify_gltf.js`, traf die Sollhöhe auf 0,0 % und hatte acht
Clips — und stand als hochbeiniges Gestell da, wo die Vorlage einen massigen,
tief hängenden Leib zeigt. Kein Skript hat das gemeldet.

**Das Zeilenprofil der Vorlage vor dem Bau erheben.** Je Bildzeile Breite,
Deckungsgrad und Anzahl getrennter Streifen. Daraus fallen drei Werte, die
sonst geschätzt werden:

- **Wo sich die Silhouette in Läufe teilt** = Bodenfreiheit. Beim EmberEater
  erst bei 82 % der Höhe; gebaut war er mit 52 %.
- **Wo die hellste Stelle liegt** = die Höhe des Kopfes. Beim EmberEater bei
  37 % von oben, also unter dem Widerrist — nicht am Scheitel.
- **Deckungsgrad und Streifenbreite** = Masseverteilung und Gliedstärke.

**Größenfaktor.** Jede Datei trägt `SOLLHOEHE`, `ISTHOEHE`, `SKAL`; angewandt
in `ringPts` und `platte()`, Knochen über `K()`. `ISTHOEHE` ist die **am GLB
gemessene Rohhöhe dieser Geometrie**, nach dem Bau nachgemessen — nicht die
einer anderen Figur. Der MoorThrower war ohne diese Prüfung 20 % zu klein und
galt trotzdem als fertig.

**Der Grundriss folgt nicht aus der Sollhöhe.** `SKAL` setzt die Höhe; Breite
und Länge bleiben davon unberührt. Der EmberEater maß nach dem Umbau 1,83 m
breit und 2,09 m lang bei 1,60 m Höhe. Er trägt deshalb zusätzlich `BREITE`
und `LAENGE`, angewandt als `SX = SKAL·BREITE`, `SY = SKAL`,
`SZ = SKAL·LAENGE` an denselben Stellen. Die Sollhöhe bleibt unangetastet.

**Höhenmessung ist Pflicht:**

```js
const b = fs.readFileSync(glb);
const j = JSON.parse(b.slice(20, 20 + b.readUInt32LE(12)).toString("utf8"));
let mn = 1e9, mx = -1e9;
for (const a of j.accessors)
  if (a.min && a.min.length === 3) { mn = Math.min(mn, a.min[1]); mx = Math.max(mx, a.max[1]); }
console.log(mx - mn);
```

**Bodenkontakt über alle Clips messen.** `plantFeet()` senkt die Wurzel, bis
der tiefste getastete Punkt den Boden berührt — getastet werden aber nur die
Punkte in `SOLE`. Alles, was tiefer reicht, sinkt ein, ohne dass ein Skript
das meldet. Die geerbten Werte `[±0.14, 0.002, ±0.10]` sind zudem **nicht**
vom Größenfaktor erfasst und stimmen für keine Figur genau.

Gemessener Stand der Serie (tiefster Punkt unter dem Boden):

| Figur | Ruhe | Gehen | Tod |
| --- | ---: | ---: | ---: |
| Riftling | +10 mm | −43 mm | −178 mm |
| CoreGuardian | +42 mm | −155 mm | −44 mm |
| SealGuardian | +29 mm | −193 mm | −462 mm |
| EmberEater | −9 mm | −17 mm | −167 mm |
| RootCharger | −4 mm | −48 mm | −314 mm |
| GraniteShell | −8 mm | −70 mm | −240 mm |
| AshRunner | −3 mm | −24 mm | −313 mm |
| Ignivar | −1 mm | −13 mm | −146 mm |
| Noctarion | −2 mm | −16 mm | kein Tod |
| Terrock | −2 mm | −33 mm | kein Tod |
| Garon | −33 mm | −160 mm | −416 mm |

Seit dem EmberEater tasten **acht** Punkte: Ballen und
Krallenspitze je Sohle beziehungsweise Pranke. Der zweite Punkt je Glied ist
nötig — mit nur einem am Ballen blieb der Fehler fast unverändert, weil die
Krallen weiter vorn liegen und beim Drehen um das Gelenk viel tiefer fallen.

`Tod` liegt bei jeder Figur am tiefsten, weil dort die Fußsetzung planmäßig
ausgeblendet wird, damit der Körper aufliegt. Der Wert ist nur im Verhältnis
zur Figurenhöhe vergleichbar: Riftling 10 %, RootCharger 14 %.

**Nach jedem Bauteil rendern.** Export und Verify prüfen nur die innere
Konsistenz. Ein hohler Schädel-Loft ohne Deckel, ein Kopf der im Brustkorb
steckt, eine Glut hinter ihrer eigenen Fassung — alle drei meldeten
„bestanden".

**Kameraachse.** `render.js`: Figuren schauen nach +Z.
Front `AZ 0,0` · Seite `AZ 1,57` · Rücken `AZ 3,14`.

**`platte()` nur auf flachen Partien.** Brust, Schulter, Oberschenkel — nicht
entlang stark gekrümmter Rotationsflächen wie einer Robe. Die ebene Fläche
steht dort zwangsläufig über.

**Prüfen, ob eine Fläche überhaupt sichtbar ist.** Beim EmberEater verdecken
die Glieder die Rumpfflanke vollständig (Vorderglieder z −0,01 bis 0,33,
Keulen −0,34 bis 0,04). Dort gesetzte Lavarisse waren aus keiner Richtung zu
sehen.

**Splice-Grenzen vorher prüfen.** Funktionsgrenzen erheben, dann schneiden,
danach Klammerbilanz zählen. Zweimal sind Konstanten oder Codeblöcke
mitverschluckt worden.

**Emission nicht erfinden.** Gemessen wird die Emissionskarte. Bei
ForgeGuardian, SealGuardian, CoreGuardian und EmberEater ist sie fast schwarz
und violett getönt, während die warmen Glutstellen im **Albedo** liegen. Wo
keine Emission gemessen wird, gibt es keine.

**Glutfarben laufen ohne `LIFT`.** Die Grundfarben werden um 1,5 angehoben,
weil die Vorlage bereits abgeschattet ist. Für die hellsten Messwerte gilt das
nicht: der Faktor treibt Rot in die Sättigung, während Grün nachzieht — aus
Orange wird Weiß. Beim EmberEater lagen zuerst alle Adern auf `#ffef6b` und
lasen sich als helle Kratzer statt als Glut.

**Clusterfarben mitteln schmale Merkmale weg.** Die Aderfarbe des EmberEaters
steckte in keinem Cluster; sie ergab sich erst aus einer gezielten Auswertung
des Warmanteils (Perzentile 15 / 65 / 85).

## Werkzeugänderungen

Gegenüber dem Wildling:

- `messung.js` nimmt das Atlasraster als Parameter statt fest 4×20.
- `export_gltf.js` kennt den achten Clip `Erscheinen` (alle V02-Kreaturen
  haben ihn, der Wildling nicht).
- Neuer Helfer `platte(b, t, s, col, ao, colHi)` für Platten mit aufgehelltem
  Oberrand; ersetzt den Wildling-Helfer `leaf()`.

Neu mit dem EmberEater (12.08.2026):

- **`loftZ(secs, opt)`** — Ringe in der x/y-Ebene, entlang z gestapelt.
  Abschnitt `{ z, w, h, ox, oy }`, Deckel über `capBack` / `capFront`.
  Für **waagerechte** Körperteile. Mit den waagerechten Ringen von `loft()`
  wird so etwas zwangsläufig zur Platte: Wandern die Ringe 0,30 in z, liegen
  dabei aber nur 0,03 in y auseinander, entsteht eine Pfanne statt einer Röhre
  — daran ist der erste EmberEater-Schädel gescheitert.
  **Für die sieben Vierbeiner ist das der Regelfall.**
- **`render.js` nimmt einen Z-Zielpunkt** als elftes Argument:
  `node render.js <az> <el> <datei> <wire> <clip> <phasen> <phase> <abstand> <ziel-y> <ziel-z>`.
  Lange Figuren liegen nicht um z = 0; ohne Versatz läuft der Kopf in der
  Seitenansicht aus dem Bild. Die Vierbeiner brauchen das durchgehend.
- **Grundrissfaktoren `BREITE` / `LAENGE`** (siehe oben).
- **`plantFeet()` mit frei besetzbarer Punktliste** statt der festen
  `SOLE`-Konstante.

Neu mit dem RootCharger (12.08.2026), alles in `rootcharger.html`:

- **19-Knochen-Vierbeiner-Rig.** `root / hips / spine / chest / neck / head /
  jaw`, Vorderläufe am `chest`, Hinterläufe an den `hips`, je Lauf drei
  Knochen. Gegenüber dem Zweibeiner sind Hals und Kiefer dazugekommen.
- **`lauf(ph, amp)`** — ein Laufzyklus aus drei Winkeln, Standphase um `ph` 0,
  Schwungphase um `ph` π. Grundlage aller acht Clips; Vorder- und Hinterläufe
  unterscheiden sich nur im Ausschlag. Dazu `setzeLauf(links, vorn, w)`.
- **`ranke(b, dir, len, br, krd, n, col, colHi)`** — verjüngte, gekrümmte
  **Röhre** aus `n` Gliedern für Wurzelmähnen, Stachelmähnen und Schweife.
  Nicht aus `platte()` gebaut: von der Seite sähe man nur Kanten, und die
  Mähne läse sich als Reihe flacher Rauten. Dritte Stelle neben `ringPts` und
  `platte`, an der `SX`/`SY`/`SZ` angewandt werden — die Ringe stehen frei im
  Raum.
- **Bodenschatten auf vier Pranken** in `render.js` und im WebGL-Teil. Die
  Punkte kommen aus `SOHLEN`, damit Schatten und Fußsetzung dieselben Stellen
  benutzen. `uBlob` fasst genau vier — für einen Vierbeiner geht das auf, der
  Rumpfschatten des Zweibeiners entfällt.

`loftZ` und die Grundrissfaktoren sind aus `embereater.html` übernommen und
stehen jetzt auch in `rootcharger.html`.

## Vorlagen für neue Modelle

- **Zweibeiner:** `riftling.html`.
- **Knöchelgänger:** `embereater.html`.
- **Vierbeiner:** `rootcharger.html`. Es trägt das 19-Knochen-Rig, alle acht
  Clips auf `lauf()`, `ranke()`, `loftZ()`, die Grundrissfaktoren und den
  vierpunktigen Bodenkontakt.
- **Vierbeiner mit Schwanz:** `ashrunner.html` — dasselbe plus drei
  Schwanzknochen und `schwanz()`.

## Was die Serie NICHT umfasst

Der **Einbau in Unity** ist inzwischen erfolgt und hat dieser Abschnitt
überholt — siehe oben und `KREATUREN_EINBAU_REFERENZ.md`. Er folgt **nicht**
dem Wanderer-Muster über `MeshActorPresentation`, sondern läuft über die eigene
Schicht `CreatureMeshPresentation`. Von den beiden hier genannten Blockern ist
der erste (`ITEM_TMP_SmithingMark.png`) überholt, der zweite
(`Wildling_2D.prefab`) besteht, hat den 3D-Einbau aber nicht behindert.

**Modellbreite gegen Navigationsradius.** Die zuletzt gebauten Figuren sind
deutlich breiter als ihr Navigations-Agent:

| Figur | Modellbreite | Agentbreite | Verhältnis |
| --- | ---: | ---: | ---: |
| Riftling | 0,811 m | 0,915 m | 0,89 |
| RiftGuardian | 1,688 m | 1,612 m | 1,05 |
| MoorThrower | 1,273 m | 0,998 m | 1,27 |
| EmberEater | 1,314 m | 0,826 m | 1,59 |
| **RootCharger** | **2,061 m** | **1,227 m** | **1,68** |

Beim RootCharger kommt es aus der Bauart: ein Vierbeiner mit seitlich
stehenden Läufen und einer Mähne über den Schultern ist zwangsläufig breiter
als ein Zweibeiner. **Die Frage stellt sich für alle sieben Vierbeiner
gleich** und sollte einmal für die ganze Bauart entschieden werden, nicht je
Figur. Der `AgentRadius` ist Spieldatum und wurde nicht angefasst.

**Fremdes Motiv im GraniteShell-Spriteblatt.** Jede Kachel von
`granite_shell_idle_albedo.png` enthält unter der Kreatur (y 22–167) ein
zweites Alphaband (y 186–241): einen gepanzerten, behelmten Ritter zwischen
zwei Steinblöcken. Er gehört inhaltlich nicht zur Figur.

Von den elf V02-Kreaturen haben nur `granite_shell` und `moor_thrower` ein
zweites Band; beim MoorThrower ist es Geröll und passt zum Wurfangriff.
`V02ActorVisualBuilder` Zeile 92 zeichnet die volle 256er-Kachel als Quad —
ob der Ritter im Spiel sichtbar wird, ist **ungeklärt**. Das 3D-Modell wurde
ausschließlich am Kreaturenband gemessen.

**Folge für alle künftigen Messungen:** vor dem Messen die Alphabänder je
Kachel zählen. Über eine Kachel mit zwei Motiven gemessen sind Farbcluster und
Zeilenprofil verfälscht.

Kosmetisch offen: alle Figurendateien außer `embereater.html`,
`rootcharger.html` und `graniteshell.html` tragen noch
`<title>Wildling — Low-Poly-Gegner</title>`, `<h1>Wildling</h1>` und den
Wildling-Namen im OBJ-Export — Reste der Vorlage, aus der sie kopiert wurden.

Verwandte Dokumente: `KREATUREN_EINBAU_REFERENZ.md` (Einbau in Unity),
`KREATURENSERIE_BEFUND.md` (Bauarten, Atlasformate,
Vierbeiner-Rig-Vorschlag), `G004_ABNAHME.md` (Bodenschatten, abgeschlossen).
