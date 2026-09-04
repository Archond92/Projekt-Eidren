# Kreaturenserie 3D — Bauartbefund

**Bauartbestimmung: 11. August 2026 · Baustand nachgeführt: 12. August 2026**

Grundlage für die Planung der Figurenmodelle. Entstanden während der Arbeit an
Modell 1 (Riftling) und 2 (RootCharger).

## Bauart-Tabelle

Bauart bestimmt am 11.08.2026 durch Sichtung der `*_idle_albedo.png`-Vorlagen;
Spalte „Modell" am 12.08.2026 nachgeführt.

| Figur | Modell | Bauart | Merkmal |
| --- | --- | --- | --- |
| Wanderer | ✓ fertig | Zweibeiner | aufrecht, Ausrüstungsslots |
| Wildling | ✓ fertig | Zweibeiner | tief gebückt |
| **Riftling** | **✓ fertig** | Zweibeiner | digitigrad, Schädelkamm |
| **MoorThrower** | **✓ fertig** | Zweibeiner | gebückt, trägt Felsbrocken (Wurfangriff) |
| **RiftGuardian** | **✓ fertig** | Zweibeiner | Steinkoloss, Blockkopf, Kristallarme |
| **ForgeGuardian** | **✓ fertig** | Zweibeiner | Panzerwächter mit Schild und Axt |
| **SealGuardian** | **✓ fertig** | Zweibeiner | Umhang und Leuchtstab, Robe verdeckt die Beine |
| **CoreGuardian** | **✓ fertig** | Zweibeiner | Panzerkonstrukt, Ofenkern in der Brust |
| **EmberEater** | **✓ fertig** | Knöchelgänger | stark gebückt, Vorderglieder am Boden |
| **RootCharger** | **✓ fertig** | **Vierbeiner** | lang und schwer, Wurzelmähne |
| **GraniteShell** | **✓ fertig** | **Vierbeiner** | gedrungen, Felskuppel |
| **AshRunner** | **✓ fertig** | **Vierbeiner** | schlanker Läufer mit langem Schwanz |
| **Ignivar** | **✓ fertig** | **Vierbeiner** | Feuerfuchs mit Flammenmähne und -schweif |
| **Terrock** | **✓ fertig** | **Vierbeiner** | Panzerträger mit Kuppelschale, bodennah |
| **Garon** | **✓ fertig** | **Vierbeiner** | großer Panzerträger mit Kuppelschale |
| **Noctarion** | **✓ fertig** | **Vierbeiner** | pantherartig, Stachelmähne, langer Schweif |

## Bilanz — die Serie zerfällt sauber in zwei Hälften

**Zweibeiner, vorhandenes 17-Knochen-Rig — 8 Figuren, alle gebaut:**
Wanderer, Wildling, Riftling, MoorThrower, RiftGuardian, CoreGuardian,
ForgeGuardian, SealGuardian. Jede nach der ersten war eine **Anpassung** des
Riftlings. **Abgeschlossen.**

**Knöchelgänger — 1 Figur, gebaut:** EmberEater. Nutzt das Zweibeiner-Rig mit
tieferer Haltung, keine eigene Bauart — wohl aber eigene Werkzeuge: `loftZ()`
für den liegenden Rumpf, Grundrissfaktoren und ein achtpunktiger Bodenkontakt.
Einzelheiten im README der Figur.

**Vierbeiner, neues 19-Knochen-Rig — 7 Figuren, alle gebaut:**
RootCharger ✓, GraniteShell ✓, AshRunner ✓, Ignivar ✓, Noctarion ✓,
Terrock ✓, Garon ✓. **Abgeschlossen.**
Das Rig ist mit dem RootCharger **einmal** entstanden; der GraniteShell hat
es als erster geerbt, der AshRunner um drei Schwanzknochen erweitert, der
Ignivar diese Erweiterung ein zweites Mal genutzt — alles ohne Änderung an
der Werkzeugkette.

**Die Serie ist gebaut.** 16 Figuren, drei Rigs, eine Werkzeugkette.

Was die Vererbung tatsaechlich spart, ist am GraniteShell gemessen und in
`KREATURENSERIE_STAND.md` festgehalten.

## Besonderheiten, die je Figur Mehrarbeit bedeuten

| Figur | Besonderheit |
| --- | --- |
| MoorThrower | trägt einen Felsbrocken — Handslot oder eingebautes Wurfobjekt |
| ForgeGuardian | Schild und Axt — zwei Handslots |
| SealGuardian | langer Umhang, muss mitschwingen; Beine darunter vereinfachbar |



## Atlasformate — je Figur verschieden

Das Raster ist **nicht einheitlich** und muss je Figur bestimmt werden:

| Figur | Atlas | Raster |
| --- | --- | --- |
| alle elf V02-Kreaturen | 1024×1024 | 4×4, Frame 256×256 |
| Wildling | 874×4096 | 4×20 |
| **Terrock** | **je Zustand verschieden** | **4×20, Zelle 256×205** — aus dem Prefab |
| **Noctarion** | **je Zustand verschieden** | **4×20, Zelle 320×205** — nachgemessen |
| **Garon** | **je Zustand verschieden** | **4×20, Zelle 328×205** — nachgemessen, das Prefab fehlt |

Beim Noctarion streuen die Blattgrößen je Zustand (1279×4096, 1066×4096,
1280×3280). Die verlässlichste Quelle für das Raster ist **das jeweilige
`*_2D.prefab`**: es nennt `runtimeCellWidth`, `runtimeCellHeight` und
`visibleHeightPixels` im Klartext.

`messung.js` nimmt das Raster deshalb als Parameter. Vor jeder Messung die
Bilddimension prüfen.

## Kernbefund: mindestens drei Baupläne

Die Annahme, ein Modell sei eine Anpassung des vorigen, trägt **nur innerhalb
einer Bauart**.

- **Zweibeiner** (Wanderer, Wildling, Riftling): 17 Knochen, gemeinsames Rig.
  Der Riftling war eine Anpassung — Palette, Proportionen, Oberflächen und
  Clip-Taktung neu, Skelett und Clip-Funktionen übernommen. Aufwand: rund ein
  halber Arbeitstag.
- **Vierbeiner** (RootCharger, GraniteShell, AshRunner): 19 Knochen, mit
  Schwanz 22.
  `hips → spine → chest → neck → head → jaw`, Vorderläufe am `chest`,
  Hinterläufe an den `hips`. **Damit fallen auch alle acht Clip-Funktionen**,
  weil sie `B.armL_up`, `B.thighL` und `B.shinL` ansprechen. Aufwand war ein
  Neubau; er ist mit dem RootCharger am 12.08.2026 erledigt.
- Innerhalb der Vierbeiner unterscheiden sich die Proportionen stark
  (RootCharger lang und schwer, GraniteShell gedrungen und breit) — die
  Geometrie ist je Figur neu, das Rig aber wiederverwendbar.

**Empfehlung — umgesetzt und bestätigt.** Nach Bauart bündeln statt in der
Listenreihenfolge arbeiten: erst die Bauart aller Figuren aus den
Sprite-Vorlagen bestimmen, dann je Bauart ein Referenzmodell bauen und die
übrigen daraus ableiten.

Das Vorgehen hat getragen. Riftling und RootCharger sind die beiden
Referenzmodelle; alle Zweibeiner nach dem Riftling waren Anpassungen, und für
die vier verbleibenden Vierbeiner steht das Rig fertig da — mit Schwanz
(AshRunner) und ohne (GraniteShell) je einmal durchgespielt.

## Gemeinsamkeiten, die durchgehend gelten

- Alle elf V02-Atlanten sind **1024×1024, Raster 4×4, Einzelframe 256×256**.
  Der Wildling-Atlas weicht davon ab (4×20) — `messung.js` nimmt das Raster
  deshalb als Parameter.
- Alle elf V02-Kreaturen haben **acht Zustände**: `appear`, `attack`, `death`,
  `hit`, `idle`, `move`, `stagger`, `telegraph`. Dem Wildling fehlt `appear`;
  die Clipliste in `export_gltf.js` war entsprechend auf sieben verdrahtet und
  ist seit dem Riftling ergänzt.
- Für **jede** Figur existiert eine Sprite-Vorlage mit Albedo, Normal und
  Emission. Die Modellierung hat durchgehend eine belastbare Referenz.
- Die Zeitanker jedes Clips stehen in `Data/Enemies/<Name>.asset`, die
  Sollhöhe in `V02ActorVisualBuilder`. Beides ist bindend und wird nicht
  geschätzt.

## Emissionsverhalten ist figurabhängig

| Figur | Emission |
| --- | --- |
| Wildling | rote Augen, deutlich |
| Riftling | schwaches Purpur an Brust- und Hüftfugen (97 % schwarz) |
| RootCharger | **keine** (100 % schwarz) |
| GraniteShell | **keine** (nahezu 100 % schwarz) |
| AshRunner | keine — die Glut liegt im Albedo (Forge-Muster) |
| Ignivar | keine — die Flammen liegen im Albedo (Forge-Muster) |

Der gemeinsame Shader hat kein Emissive; Glut wird als aufgehellte Vertexfarbe
genähert. Wo keine Emission gemessen wird, entfällt sie im Modell — nicht
erfinden.

## Vorlage für neue Modelle

Ab sofort ist `riftling.html` die Zweibeiner-Vorlage statt `wildling.html`:
Es trägt bereits den achten Clip `Erscheinen` und den Plattenhelfer
`platte(b, t, s, col, ao, colHi)`.

Für die Vierbeiner ist **`rootcharger.html`** die Vorlage (Stand 12.08.2026).
Sie trägt das 19-Knochen-Rig, alle acht Clips auf `lauf()`, den Helfer
`ranke()` für Mähnen und Schweife, `loftZ()` für den liegenden Rumpf, die
Grundrissfaktoren `BREITE`/`LAENGE` und den vierpunktigen Bodenkontakt.

Für Vierbeiner **mit Schwanz** ist **`ashrunner.html`** die Vorlage: dasselbe
plus drei Schwanzknochen (hinten angehängt, damit alle Indizes gültig bleiben)
und den Helfer `schwanz(neigen, wedeln, rollen)`.

Je Erbfigur neu zu bauen sind nur Palette, Proportionen, Geometrie und
Taktung — Skelett, Clipschicht und Werkzeuge bleiben. Am GraniteShell
gemessen und in `KREATURENSERIE_STAND.md` aufgeschlüsselt.

## Größenfaktor — verbindlich ab Modell 4

Die Geometrie ist in absoluten Weltkoordinaten geschrieben; sie skaliert
**nicht** mit dem Skelett. Bis zum MoorThrower fiel das nicht auf, weil alle
Figuren nahe 1,7 m lagen. Sieben der zehn verbleibenden Modelle weichen
deutlich ab (Ignivar 1,00 m bis CoreGuardian 4,50 m).

**Regel:** Jede Figurendatei trägt oben

```js
const SOLLHOEHE = <aus V02ActorVisualBuilder>;
const ISTHOEHE  = <am GLB gemessene Rohhoehe der Geometrie>;
const SKAL = SOLLHOEHE / ISTHOEHE;
```

Angewandt in `ringPts` (erfasst jedes `loft`), in `ringPtsZ` (jedes `loftZ`)
und in `platte()`. Die Knochen laufen über `K()`, damit Skelett und Körper im
selben Maß stehen.

**Ausnahme seit dem RootCharger:** `ranke()` wendet den Faktor selbst an und
ruft `quad()` direkt auf. Seine Ringe stehen frei im Raum — senkrecht zur
Laufrichtung einer gekrümmten Wurzel — und lassen sich nicht über die
achsenparallelen Helfer bauen. Wer den Größenfaktor ändert, muss diese dritte
Stelle mitprüfen.

Seit dem EmberEater kommen `BREITE` und `LAENGE` hinzu:
`SX = SKAL·BREITE`, `SY = SKAL`, `SZ = SKAL·LAENGE`. `SKAL` setzt nur die
Höhe; der Grundriss läuft sonst davon.

**Höhenprüfung ist Pflicht.** Die Rohhöhe wird über die Accessor-Grenzen des
GLB gemessen:

```js
const b = fs.readFileSync(glb);
const j = JSON.parse(b.slice(20, 20 + b.readUInt32LE(12)).toString("utf8"));
let mn = 1e9, mx = -1e9;
for (const a of j.accessors)
  if (a.min && a.min.length === 3) { mn = Math.min(mn, a.min[1]); mx = Math.max(mx, a.max[1]); }
console.log(mx - mn);
```

Stand nach der Korrektur am 11.08.2026:

| Figur | gemessen | Soll | Abweichung |
| --- | ---: | ---: | ---: |
| Riftling | 1,700 m | 1,70 | 0,0 % |
| MoorThrower | 1,900 m | 1,90 | 0,0 % |
| RiftGuardian | 3,000 m | 3,00 | 0,0 % |

**Wie der Fehler auffiel:** Der MoorThrower war mit 1,515 m ein Fünftel zu
klein und wurde trotzdem als fertig gemeldet. Knochen und Geometrie waren
zueinander stimmig, nur eben beide zu klein — Export und Verify melden das
nicht, weil beide nur die innere Konsistenz prüfen. Erst die Messung gegen den
Sollwert aus `V02ActorVisualBuilder` deckte es auf.

## Lehre aus Modell 1

Zwei Geometriefehler am Riftling meldeten Export und Verify durchgehend als
„bestanden": ein Schädel-Loft ohne Deckel (hohle Röhre) und ein Schädel, der
vollständig im Brustkorb steckte und von außen nicht sichtbar war.
**Geometrieprüfung ersetzt die Sichtprüfung nicht — nach jedem Bauteil
rendern.**
