# Wildling 3D — Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ziel:** Eine Low-Poly-3D-Figur `Wildling.glb` (Mesh, Skelett, 7 Animationsclips, Vertexfarben) nach dem Wanderer3D-Muster, treu zum bestehenden 2D-Sprite.

**Architektur:** Eine editierbare Three-Datei `wildling.html` (prozedurale Geometrie + Rig + Clips + Browser-Viewer) ist die einzige Quelle. Node-Skripte schneiden per Textmarker Abschnitte daraus aus und führen sie aus: `export_gltf.js` schreibt das GLB, `verify_gltf.js` prüft das GLB gegen das Live-Rig, `render.js` rastert Prüfbilder ohne Browser. Alle vier Dateien werden vom Wanderer kopiert und angepasst.

**Tech-Stack:** Node.js (ohne Abhängigkeiten), WebGL-1-Viewer im Browser, glTF 2.0 (GLB), Unity-Import später via glTFast.

**Spezifikation:** `Documentation/Etappen/Wanderer3D/WILDLING3D_ENTWURF.md`

## Globale Vorgaben

- Maßstab 1 Einheit = 1 m; Blickrichtung +Z; Figur gebückt ~1,6–1,7 m, aufgerichtet ~2,0 m (Agent: Höhe 2,1, Radius 0,52)
- Vertexfarben sRGB, AO und Augen-Leuchten eingebacken; Flat Shading über Normalen; keine Texturen
- Harte Skinnierung: genau 1 Knochen pro Vertex, Gewicht 1,0
- Dreieckszahl gesamt: 1.500–2.500
- Clipnamen im GLB exakt: `Ruhe`, `Gehen`, `Telegraph`, `Angriff`, `Treffer`, `Taumeln`, `Tod` — 30 fps
- Clip-Zeitanker aus `Assets/_Game/Data/Enemies/Wildling.asset`: Telegraph 0,55 s; Trefferfenster des Angriffs ~0,18 s; Taumeln 2,5 s haltbar; MoveSpeed 3,6 m/s
- Zielordner: `Assets/_Game/Art/Actors/Wildling/Wildling3D/` mit `Wildling.glb`, `README_Wildling3D.md`, `Source~/`
- **Keine Git-Commits:** Das Repo trackt per `.gitignore` (`*`) nur Release-Notes. Statt Commit-Schritten schließt jede Aufgabe mit einem Prüfnachweis (Render-Bild oder Skriptausgabe).
- **Fremde Dateien:** Ein paralleler codex-Agent arbeitet zeitweise im Projekt. Nur unter `Wildling3D/` neue Dateien anlegen; nichts außerhalb ändern; Unity-Lockfile nie löschen.
- Alle `node`-Aufrufe im Ordner `Assets/_Game/Art/Actors/Wildling/Wildling3D/Source~/` ausführen.

## Pflicht-Marker in `wildling.html`

Die drei Node-Skripte schneiden den Quelltext per `indexOf` an diesen Zeichenketten. Sie müssen exakt so in `wildling.html` vorkommen (Kopie aus `wanderer.html` behält sie automatisch):

1. `"use strict";` — Beginn des ausführbaren Abschnitts
2. eine Abschnittsüberschrift, deren Kopfzeile mit `/* =====` beginnt und die Zeichenkette `5 — WebGL` enthält — Ende des Geometrieabschnitts
3. `const boneWorld = BONES.map` — Beginn des Rig-Abschnitts
4. `/* ---- Kamera und Eingabe` — Ende des Rig-Abschnitts

Zwischen Marker 2 und 3 darf kein Code stehen, den Export/Render brauchen; alles, was die Skripte nutzen (`G`, `parts`, `vCount`, `BONES`, `B`, Paletten, Hilfsfunktionen), liegt vor Marker 2, das Rig (`CLIPS`, `pose`, `updateBones`, `plantFeet`, `skin`, `clipDur`) zwischen Marker 3 und 4.

---

### Aufgabe 1: Ordner anlegen, Skripte kopieren, Sprite-Farben messen

**Dateien:**
- Erstellen: `Assets/_Game/Art/Actors/Wildling/Wildling3D/Source~/lupe.js` (unveränderte Kopie)
- Erstellen: `Assets/_Game/Art/Actors/Wildling/Wildling3D/Source~/MESSWERTE.md`

**Schnittstellen:**
- Liefert: gemessene Hex-Farbwerte der Sprite-Vorlage in `MESSWERTE.md` — Aufgabe 2 übernimmt sie in die `HEX`-Palette von `wildling.html`.

- [ ] **Schritt 1: Ordner anlegen und `lupe.js` kopieren**

```bash
mkdir -p "Assets/_Game/Art/Actors/Wildling/Wildling3D/Source~"
cp "Assets/_Game/Art/Actors/Player/Wanderer3D/Source~/lupe.js" "Assets/_Game/Art/Actors/Wildling/Wildling3D/Source~/lupe.js"
```

- [ ] **Schritt 2: Farbprofil der Idle-Vorlage messen**

```bash
cd "Assets/_Game/Art/Actors/Wildling/Wildling3D/Source~"
node lupe.js "../../../../../Resources/Art/Actors/Wildling/wildling_idle_albedo.png"
```

Erwartet: Bildgröße 874×4096 und 20 Zeilenbänder mit mittlerer Farbe. Der Atlas hat 4 Spalten (Richtungen) × 20 Zeilen (Frames); ein Einzelframe ist ~218×205 px.

- [ ] **Schritt 3: Einzelframe vergrößern und Detailfarben messen**

Frame oben links (Front-Idle) herauszoomen und gezielt nachmessen:

```bash
node lupe.js "../../../../../Resources/Art/Actors/Wildling/wildling_idle_albedo.png" 0 0 218 205 4 wildling_frame0.png
```

Am erzeugten `wildling_frame0.png` (und per weiteren `lupe.js`-Ausschnitten) mindestens diese Werte bestimmen: Moos hell/mittel/dunkel, Rinde/Ranken hell/dunkel, Bauch-/Brustton, Augenfarbe, Krallenfarbe. Zusätzlich `wildling_idle_emission.png` mit demselben Ausschnitt messen, um die Leuchtpartien (Augen) zu verorten.

- [ ] **Schritt 4: Messwerte festhalten**

`MESSWERTE.md` schreiben: Tabelle `Zweck | Hex | Fundort (x,y im Frame)` mit den gemessenen Werten. Keine geratenen Farben — jede Zeile hat einen Fundort.

- [ ] **Schritt 5: Prüfnachweis**

`MESSWERTE.md` existiert, enthält ≥ 8 gemessene Farben mit Fundort; `wildling_frame0.png` liegt im Ordner.

---

### Aufgabe 2: `wildling.html` Grundgerüst — Kopie, Rückbau, Skelett, Proxy-Figur

**Dateien:**
- Erstellen: `…/Wildling3D/Source~/wildling.html` (Ausgang: Kopie von `…/Wanderer3D/Source~/wanderer.html`)

**Schnittstellen:**
- Verbraucht: Farbtabelle aus `MESSWERTE.md` (Aufgabe 1)
- Liefert (für alle Skripte, per Marker-Slice):
  - `G = { p, n, c, b, a, e }` (Arrays), `vCount` (Anzahl Vertices), `parts` (`{ bone, start, count }` — **ohne** `slot`/`set`)
  - `BONES` (`{ name, parent, pivot }`), `B` (Name→Index)
  - `equip = {}` und `visible = (pt) => true` (Kompatibilität mit den kopierten Skripten)
  - Rig-Abschnitt: `CLIPS`, `clipDur(k)`, `pose(t)`, `updateBones()`, `plantFeet()`, `skin()`, `boneWorld`
  - Geometrie-Baukasten unverändert: `tri`, `quad`, `loft`, `mirror`, `ringPts`, `capPoly`, `part`, `SH`, `C`, Mathefunktionen

- [ ] **Schritt 1: Kopieren**

```bash
cp "Assets/_Game/Art/Actors/Player/Wanderer3D/Source~/wanderer.html" "Assets/_Game/Art/Actors/Wildling/Wildling3D/Source~/wildling.html"
```

- [ ] **Schritt 2: Wanderer-Inhalte zurückbauen**

In `wildling.html`:
- Abschnitt 4 („Die Figur"): alle Wanderer-Bauteile entfernen (`legParts`, `hipParts`, `torsoParts`, `armParts`, `headParts`, `helmetParts`, `hoodParts`, `ironHelmParts`, `staffParts`, `daggerParts`, `hammerParts` samt Aufrufen)
- `equip`/`visible` ersetzen durch:

```js
  /* Der Wildling hat keine Ausrüstungsslots; die Felder bleiben für die
     Werkzeugskripte erhalten. */
  const equip = {};
  const visible = (pt) => true;
```

- Abschnitt 6 (Rig): Waffen-/Slot-Spezifika entfernen (`hammerCarry`, `solveHammerGrip`-Aufruf in `plantFeet`, `staffAim`/`gripY`-Logik, alle `equip.waffe`-Zweige in Clips), Wanderer-Clipfunktionen (`clipIdle`, `gait`, `clipWalk`, `clipRun`, `clipAttack*`) vorerst durch leere Platzhalter unten in Aufgabe 5–7 ersetzen — bis dahin:

```js
  function clipIdle(p) {}
  const CLIPS = {
    ruhe: { label: "Ruhe", dur: 5.00, fn: clipIdle }
  };
  function clipDur(k) { return CLIPS[k].dur; }
```

- `plantFeet()` reduzieren auf Fußsetzung ohne Waffen-IK (Struktur von `SOLE`/`plantAmount` beibehalten, `solveHammerGrip()` streichen)
- UI-Reste des Viewers (Ausrüstungs-/Waffenknöpfe, `SWATCHES`) auf den Wildling eindampfen; Titel „Wildling" setzen
- **Alle vier Pflicht-Marker (siehe oben) müssen erhalten bleiben.**

- [ ] **Schritt 3: Palette und Skelett einsetzen**

`HEX`-Tabelle durch die Messwerte aus Aufgabe 1 ersetzen (Schlüssel z. B. `moos`, `moosDk`, `moosLt`, `rinde`, `rindeDk`, `bauch`, `kralle`, `auge`). `BONES` ersetzen (gebückte Ruhepose — die Pivots tragen die Beuge, gebaut wird direkt in dieser Haltung):

```js
  const BONES = [
    { name: "root",      parent: -1, pivot: [0, 0, 0] },
    { name: "hips",      parent: 0,  pivot: [0, 0.86, 0] },
    { name: "spine",     parent: 1,  pivot: [0, 1.00, 0.02] },
    { name: "chest",     parent: 2,  pivot: [0, 1.22, 0.10] },
    { name: "head",      parent: 3,  pivot: [0, 1.42, 0.24] },
    { name: "armL_up",   parent: 3,  pivot: [0.26, 1.38, 0.14] },
    { name: "armL_fore", parent: 5,  pivot: [0.34, 1.02, 0.16] },
    { name: "handL",     parent: 6,  pivot: [0.38, 0.62, 0.18] },
    { name: "armR_up",   parent: 3,  pivot: [-0.26, 1.38, 0.14] },
    { name: "armR_fore", parent: 8,  pivot: [-0.34, 1.02, 0.16] },
    { name: "handR",     parent: 9,  pivot: [-0.38, 0.62, 0.18] },
    { name: "thighL",    parent: 1,  pivot: [0.13, 0.84, 0] },
    { name: "shinL",     parent: 11, pivot: [0.14, 0.48, -0.02] },
    { name: "footL",     parent: 12, pivot: [0.14, 0.10, -0.04] },
    { name: "thighR",    parent: 1,  pivot: [-0.13, 0.84, 0] },
    { name: "shinR",     parent: 14, pivot: [-0.14, 0.48, -0.02] },
    { name: "footR",     parent: 15, pivot: [-0.14, 0.10, -0.04] }
  ];
```

(17 Knochen. Werte sind Startwerte; Aufgabe 3/4 justiert sie mit der Geometrie.)

- [ ] **Schritt 4: Proxy-Figur bauen**

Damit Rendern und Skinnen sofort prüfbar sind, pro Hauptknochen einen groben Kasten bauen (wird in Aufgabe 3/4 ersetzt):

```js
  function proxyParts() {
    const box = (bone, cx, cy, cz, w, h, d, col) => part(bone, () => {
      loft([{ y: cy - h/2, w, d, ox: cx, oz: cz }, { y: cy + h/2, w, d, ox: cx, oz: cz }],
           { col, shape: SH.rect, capTop: true, capBottom: true });
    });
    box(B.hips,  0, 0.93, 0.02, 0.34, 0.22, 0.26, P.moos);
    box(B.spine, 0, 1.12, 0.06, 0.38, 0.24, 0.30, P.moos);
    box(B.chest, 0, 1.33, 0.16, 0.42, 0.22, 0.32, P.moos);
    box(B.head,  0, 1.52, 0.30, 0.26, 0.22, 0.26, P.rinde);
    for (const s of [1, -1]) {
      box(s>0?B.armL_up:B.armR_up,     s*0.30, 1.20, 0.15, 0.14, 0.36, 0.14, P.rinde);
      box(s>0?B.armL_fore:B.armR_fore, s*0.36, 0.82, 0.17, 0.12, 0.40, 0.12, P.rinde);
      box(s>0?B.handL:B.handR,         s*0.38, 0.50, 0.18, 0.16, 0.24, 0.16, P.kralle);
      box(s>0?B.thighL:B.thighR,       s*0.135,0.66, -0.01,0.16, 0.36, 0.16, P.moosDk);
      box(s>0?B.shinL:B.shinR,         s*0.14, 0.29, -0.03,0.13, 0.38, 0.13, P.moosDk);
      box(s>0?B.footL:B.footR,         s*0.14, 0.05, 0.02, 0.14, 0.10, 0.24, P.rindeDk);
    }
  }
  proxyParts();
```

- [ ] **Schritt 5: Viewer im Browser prüfen**

`wildling.html` im Browser öffnen. Erwartet: Proxy-Figur aus Kästen in gebückter Silhouette, drehbar, Clip „Ruhe" (statisch) läuft ohne Konsolenfehler.

- [ ] **Schritt 6: Prüfnachweis**

Screenshot oder Konsolenausgabe ohne Fehler; `vCount / 3` (im Viewer-Footer bzw. per Konsole) notieren.

---

### Aufgabe 3: `render.js` anpassen und Proxy offline rendern

**Dateien:**
- Erstellen: `…/Wildling3D/Source~/render.js` (Ausgang: Kopie vom Wanderer)

**Schnittstellen:**
- Verbraucht: `wildling.html` mit Pflicht-Markern (Aufgabe 2); `B.hips`, `B.footL`, `B.footR`, `BONES`, `boneWorld`
- Liefert: `node render.js <az> <el> <datei> <wire> <clip> <phasen> [phase] [dist] [targetY]` — Prüfbilder für alle Folgeaufgaben

- [ ] **Schritt 1: Kopieren und anpassen**

```bash
cp "Assets/_Game/Art/Actors/Player/Wanderer3D/Source~/render.js" "Assets/_Game/Art/Actors/Wildling/Wildling3D/Source~/render.js"
```

Änderungen:
- `wanderer.html` → `wildling.html` (Dateiname im `readFileSync`)
- Ausrüstungs-Argument `EQARG` und den `M.equip`-Block entfernen; `vmask` bleibt (mit `visible = () => true` sind alle Teile sichtbar)
- Kontaktschatten-Blobs: den Speer-Blob (`B.staff`) entfernen; Fuß-Offsets an die Wildling-Füße anpassen:

```js
  const fL = xp(bw[B.footL], [0.14, 0.002, 0.02]);
  const fR = xp(bw[B.footR], [-0.14, 0.002, 0.02]);
  const bd = xp(bw[B.hips], BONES[B.hips].pivot);
  const blobs = [blob(bd[0], bd[2], air, 0.46, 0.55),
                 blob(fL[0], fL[2], fL[1], 0.22, 0.82),
                 blob(fR[0], fR[2], fR[1], 0.22, 0.82)];
```

- Kamera-Standard `TARGET`-Höhe von 1.0 auf 0.9 (gebückte Figur)

- [ ] **Schritt 2: Proxy rendern**

```bash
node render.js 0.58 0.16 proxy_front.png 0 ruhe 1
node render.js 2.20 0.16 proxy_seite.png 0 ruhe 1
```

Erwartet: zwei PNGs, Proxy-Figur sichtbar, Konsole meldet Dreiecke/Knochen/Clip.

- [ ] **Schritt 3: Prüfnachweis**

Beide Bilder ansehen: Figur steht auf dem Boden (Füße nicht eingesunken), Kontaktschatten unter Körper und Füßen, Silhouette gebückt.

---

### Aufgabe 4: Körpergeometrie — Rumpf, Kopf, Augen

**Dateien:**
- Ändern: `…/Wildling3D/Source~/wildling.html` (Abschnitt 4)

**Schnittstellen:**
- Verbraucht: Baukasten (`loft`, `SH`, `part`, `mirror`), Palette `P`, `BONES`/`B`
- Liefert: `hipParts()`, `torsoParts()`, `headParts()` — ersetzen die entsprechenden Proxy-Kästen; Augen mit `curEmit = 1` als Leuchtpartie

- [ ] **Schritt 1: Rumpf bauen (Hüfte→Brust als ein Loft-Stapel)**

Proxy-Kästen für `hips`/`spine`/`chest` entfernen. Konkreter Aufbau (Startwerte, gegen Sprite justieren):

```js
  function torsoParts() {
    /* Hüfte bis Schulter als ein Stapel; der Knochenwechsel läuft über part() */
    part(B.hips, () => loft([
      { y: 0.80, w: 0.30, d: 0.24, ao: 0.85 },
      { y: 0.94, w: 0.36, d: 0.28, oz: 0.01 },
      { y: 1.02, w: 0.38, d: 0.30, oz: 0.03 }
    ], { col: P.moosDk, shape: SH.oct, capBottom: true }));
    part(B.spine, () => loft([
      { y: 1.02, w: 0.38, d: 0.30, oz: 0.03 },
      { y: 1.14, w: 0.42, d: 0.33, oz: 0.07, band: P.moos },
      { y: 1.24, w: 0.46, d: 0.35, oz: 0.11 }
    ], { col: P.moos, shape: SH.oct }));
    part(B.chest, () => loft([
      { y: 1.24, w: 0.46, d: 0.35, oz: 0.11 },
      { y: 1.36, w: 0.50, d: 0.36, oz: 0.16, band: P.moos },
      { y: 1.46, w: 0.42, d: 0.30, oz: 0.20, ao: 0.9 }
    ], { col: P.moos, shape: SH.oct, capTop: true, capTopCol: P.moosDk }));
  }
```

Dazu Bauchplatte (hellerer `bauch`-Ton als flaches Quad-Band vorn) und 2–3 Ranken-„Wülste" (schmale Lofts mit `P.rinde` quer über Schulter/Flanke) — Details nach Sprite-Vorlage `wildling_frame0.png`.

- [ ] **Schritt 2: Kopf mit Augen bauen**

```js
  function headParts() {
    part(B.head, () => {
      loft([
        { y: 1.42, w: 0.20, d: 0.20, oz: 0.24 },
        { y: 1.50, w: 0.28, d: 0.30, oz: 0.28 },
        { y: 1.60, w: 0.24, d: 0.26, oz: 0.26, ao: 0.9 }
      ], { col: P.rinde, shape: SH.hex, capTop: true, capBottom: true });
      /* Augen: zwei kleine Quads vorn, Leuchtwert an */
      curEmit = 1;
      for (const s of [1, -1]) {
        const x = s * 0.075, y = 1.525, z = 0.405;   /* z = Kopfvorderkante */
        quad([x-0.025, y-0.018, z], [x+0.025, y-0.018, z],
             [x+0.025, y+0.018, z], [x-0.025, y+0.018, z],
             P.auge, [1, 1, 1, 1]);
      }
      curEmit = 0;
    });
  }
```

(Die z-Werte der Augen an die tatsächliche Kopfvorderkante anpassen; Position gegen die Emission-Vorlage `wildling_idle_emission.png` prüfen.)

- [ ] **Schritt 3: Rendern und gegen Sprite vergleichen**

```bash
node render.js 0.0 0.10 koerper_front.png 0 ruhe 1
node render.js 1.57 0.10 koerper_seite.png 0 ruhe 1
```

Vergleich mit `wildling_frame0.png`: Farbwelt, Bauch heller als Rücken, Augen leuchten, Buckel-Silhouette von der Seite.

- [ ] **Schritt 4: Prüfnachweis**

Beide Renderbilder; Dreieckszahl aus der Konsole notieren (Ziel nach dieser Aufgabe: grob 600–1.000 inkl. verbleibender Proxy-Gliedmaßen).

---

### Aufgabe 5: Gliedmaßen — lange Arme mit Krallen, kurze Beine

**Dateien:**
- Ändern: `…/Wildling3D/Source~/wildling.html` (Abschnitt 4)

**Schnittstellen:**
- Verbraucht: wie Aufgabe 4; zusätzlich `mirror()` für die Spiegelung
- Liefert: `armParts(side)`, `legParts(side)` mit `side` +1/−1 — ersetzen die restlichen Proxy-Kästen; danach ist `proxyParts()` vollständig entfernt

- [ ] **Schritt 1: Arme bauen**

Pro Seite drei Segmente (Oberarm ab Schulterpivot, Unterarm, Hand mit 3 Krallenfingern). Muster:

```js
  function armParts(s) {
    const up = s > 0 ? B.armL_up : B.armR_up;
    const fore = s > 0 ? B.armL_fore : B.armR_fore;
    const hand = s > 0 ? B.handL : B.handR;
    part(up, () => loft([
      { y: 1.40, w: 0.15, d: 0.15, ox: s*0.27, oz: 0.14 },
      { y: 1.20, w: 0.13, d: 0.13, ox: s*0.31, oz: 0.15 },
      { y: 1.03, w: 0.11, d: 0.11, ox: s*0.34, oz: 0.16 }
    ], { col: P.rinde, shape: SH.hex, capTop: true }));
    part(fore, () => loft([
      { y: 1.03, w: 0.11, d: 0.11, ox: s*0.34, oz: 0.16 },
      { y: 0.80, w: 0.10, d: 0.10, ox: s*0.36, oz: 0.17, band: P.moosDk },
      { y: 0.63, w: 0.09, d: 0.09, ox: s*0.38, oz: 0.18 }
    ], { col: P.rinde, shape: SH.hex }));
    part(hand, () => {
      loft([
        { y: 0.63, w: 0.11, d: 0.10, ox: s*0.38, oz: 0.18 },
        { y: 0.52, w: 0.13, d: 0.11, ox: s*0.385, oz: 0.19 }
      ], { col: P.rindeDk, shape: SH.rect });
      /* drei Krallen, nach unten-vorn auslaufend */
      for (const k of [-1, 0, 1]) loft([
        { y: 0.52, w: 0.030, d: 0.030, ox: s*(0.385 + k*0.038), oz: 0.20 },
        { y: 0.40, w: 0.012, d: 0.012, ox: s*(0.385 + k*0.045), oz: 0.235 }
      ], { col: P.kralle, shape: SH.rect, capBottom: true });
    });
  }
```

Die Hände enden nahe Kniehöhe (~0,40–0,52) — das trägt die „lange Arme"-Silhouette des Sprites.

- [ ] **Schritt 2: Beine bauen**

Kurz und kräftig, leicht gebeugte Ruhepose (Pivots aus Aufgabe 2), Fuß mit Zehenkeilen statt Schuhform:

```js
  function legParts(s) {
    const thigh = s > 0 ? B.thighL : B.thighR;
    const shin = s > 0 ? B.shinL : B.shinR;
    const foot = s > 0 ? B.footL : B.footR;
    part(thigh, () => loft([
      { y: 0.86, w: 0.19, d: 0.20, ox: s*0.13 },
      { y: 0.64, w: 0.16, d: 0.17, ox: s*0.135, oz: -0.01, band: P.moosDk },
      { y: 0.50, w: 0.14, d: 0.15, ox: s*0.14, oz: -0.02 }
    ], { col: P.moosDk, shape: SH.hex, capTop: true }));
    part(shin, () => loft([
      { y: 0.50, w: 0.14, d: 0.15, ox: s*0.14, oz: -0.02 },
      { y: 0.28, w: 0.11, d: 0.12, ox: s*0.14, oz: -0.03 },
      { y: 0.12, w: 0.10, d: 0.11, ox: s*0.14, oz: -0.04 }
    ], { col: P.rindeDk, shape: SH.hex }));
    part(foot, () => {
      loft([
        { y: 0.12, w: 0.12, d: 0.14, ox: s*0.14, oz: -0.02 },
        { y: 0.02, w: 0.14, d: 0.20, ox: s*0.14, oz: 0.02 }
      ], { col: P.rindeDk, shape: SH.rect, capBottom: true });
      /* drei Zehenkeile nach vorn */
      for (const k of [-1, 0, 1]) loft([
        { y: 0.06, w: 0.034, d: 0.05, ox: s*0.14 + k*0.042, oz: 0.11 },
        { y: 0.01, w: 0.016, d: 0.02, ox: s*0.14 + k*0.050, oz: 0.16 }
      ], { col: P.kralle, shape: SH.rect, capBottom: true });
    });
  }
```

- [ ] **Schritt 3: `proxyParts()` restlos entfernen und rendern**

```bash
node render.js 0.58 0.16 figur_front.png 0 ruhe 1
node render.js 2.20 0.16 figur_seite.png 0 ruhe 1
node render.js 0.58 0.16 figur_wire.png 1 ruhe 1
```

- [ ] **Schritt 4: Prüfnachweis**

Drei Bilder; Dreieckszahl 1.500–2.500 (Konsole); Silhouettenvergleich mit `wildling_frame0.png` von vorn und `wildling_move_albedo.png` (Spalte 2/3) für die Seitenansicht.

---

### Aufgabe 6: Clips „Ruhe" und „Gehen"

**Dateien:**
- Ändern: `…/Wildling3D/Source~/wildling.html` (Abschnitt 6, Clip-Funktionen und `CLIPS`)

**Schnittstellen:**
- Verbraucht: `rot`, `off`, `track()`, `TAU`, `B`, `clear()` — vorhandene Rig-Bausteine
- Liefert: `CLIPS`-Einträge `ruhe` (dur 5.00, Label `Ruhe`) und `gehen` (dur 0.80, Label `Gehen`); beide schleifen sauber (Endpose = Anfangspose)

- [ ] **Schritt 1: `clipIdle` implementieren**

Schweres Atmen mit Schulterhub, langsames Umherblicken, leichtes Schwanken:

```js
  function clipIdle(p) {
    const t = p * TAU, br = Math.sin(t * 3);          /* 3 Atemzüge pro 5 s */
    rot[B.spine] = [br * 0.030, Math.sin(t * 0.9) * 0.03, 0];
    rot[B.chest] = [br * 0.045, 0, Math.sin(t * 1.1) * 0.015];
    rot[B.head]  = [Math.sin(t * 1.2) * 0.05 - 0.04,
                    Math.sin(t) * 0.16 + Math.sin(t * 0.37) * 0.08, 0];
    for (const s of [1, -1]) {
      rot[s>0?B.armL_up:B.armR_up]     = [br * 0.035, 0, s * (0.04 + br * 0.012)];
      rot[s>0?B.armL_fore:B.armR_fore] = [br * 0.028 + 0.05, 0, 0];
    }
    off[B.root][1] = br * 0.009;
  }
```

- [ ] **Schritt 2: `clipGehen` implementieren**

Stapfender, breitbeiniger Trott mit pendelnden Armen; Zyklus 0,80 s deckt bei 3,6 m/s ~2,9 m Schrittstrecke ab — Schrittwinkel entsprechend groß wählen (`stride` ~0,55). Grundlage ist die `gait`-Struktur des Wanderers, reduziert auf den Wildling (keine Waffenzweige, `chest` statt Rock, beide Arme pendeln gegengleich mit großem Ausschlag `arm` ~0,5):

```js
  function clipGehen(p) {
    const a = p * TAU;
    const leg = (ph) => {
      const thigh = -0.55 * Math.cos(ph);
      const knee  = 1.10 * (0.10 + 0.90 * Math.pow(Math.max(-Math.sin(ph - 0.55), 0), 0.7));
      const foot  = -(thigh + knee) * 0.92 + 0.34 * Math.sin(ph - 0.40);
      return [thigh, knee, foot];
    };
    const L = leg(a), R = leg(a + Math.PI);
    rot[B.thighL] = [L[0], 0, 0]; rot[B.shinL] = [L[1], 0, 0]; rot[B.footL] = [L[2], 0, 0];
    rot[B.thighR] = [R[0], 0, 0]; rot[B.shinR] = [R[1], 0, 0]; rot[B.footR] = [R[2], 0, 0];
    rot[B.hips]  = [0.06, -0.14 * Math.cos(a), 0.06 * Math.sin(a)];
    rot[B.spine] = [0.10, 0.11 * Math.cos(a), -0.04 * Math.sin(a)];
    rot[B.chest] = [0.06, 0.05 * Math.cos(a), 0];
    rot[B.head]  = [-0.10, -0.05 * Math.cos(a), 0];
    for (const s of [1, -1]) {
      rot[s>0?B.armL_up:B.armR_up]     = [s * 0.50 * Math.cos(a), 0, s * 0.06];
      rot[s>0?B.armL_fore:B.armR_fore] = [0.10 + 0.18 * (0.5 + 0.5 * s * Math.cos(a)), 0, 0];
    }
    off[B.root][1] = -0.030 * Math.cos(2 * a);
    off[B.root][0] = 0.018 * Math.sin(a);
  }
```

- [ ] **Schritt 3: `CLIPS` erweitern**

```js
  const CLIPS = {
    ruhe:  { label: "Ruhe",  dur: 5.00, fn: clipIdle },
    gehen: { label: "Gehen", dur: 0.80, fn: clipGehen }
  };
```

- [ ] **Schritt 4: Kontaktbogen rendern und prüfen**

```bash
node render.js 1.57 0.12 gehen_zyklus.png 0 gehen 8
node render.js 0.58 0.16 ruhe_zyklus.png 0 ruhe 6
```

Erwartet: 8-Phasen-Reihe des Gehens — Füße setzen ohne Gleiten auf (Fußsetzung via `plantFeet`), keine Knie-Überstreckung; Ruhe-Reihe zeigt sichtbares Atmen ohne Sprung zwischen Phase 0 und letzter Phase.

- [ ] **Schritt 5: Prüfnachweis**

Beide Phasenbilder abgelegt und begutachtet.

---

### Aufgabe 7: Clips „Telegraph", „Angriff", „Treffer"

**Dateien:**
- Ändern: `…/Wildling3D/Source~/wildling.html` (Abschnitt 6)

**Schnittstellen:**
- Verbraucht: `track()` für Schlüsselbild-Kurven
- Liefert: `CLIPS`-Einträge `telegraph` (dur 0.55), `angriff` (dur 0.80, Treffer bei ~35–55 %), `treffer` (dur 0.35); `telegraph` endet in der Pose, mit der `angriff` beginnt

- [ ] **Schritt 1: `clipTelegraph`**

Aufladen: beide Arme heben sich seitlich-hinter den Kopf, Oberkörper richtet sich auf (0,55 s, endet in Endpose — kein Loop-Zwang):

```js
  function clipTelegraph(p) {
    const lift = track(p, [[0, 0], [0.7, 1, "ab"], [1, 1]]);
    rot[B.spine] = [-0.18 * lift, 0, 0];
    rot[B.chest] = [-0.14 * lift, 0, 0];
    rot[B.head]  = [0.10 * lift, 0, 0];
    for (const s of [1, -1]) {
      rot[s>0?B.armL_up:B.armR_up]     = [-1.9 * lift, 0, s * 0.55 * lift];
      rot[s>0?B.armL_fore:B.armR_fore] = [-0.5 * lift, 0, 0];
    }
    off[B.root][1] = 0.02 * lift;
  }
```

- [ ] **Schritt 2: `clipAngriff`**

Beidarmiger Prankenhieb von oben: Start = Endpose des Telegraphs, Schlag schnell („an"), Treffer-Tiefpunkt bei ~45 %, Ausschwingen zurück zur Ruhehaltung:

```js
  function clipAngriff(p) {
    const swing = track(p, [[0, 1], [0.30, 1], [0.45, -0.55, "an"], [0.70, -0.35, "ab"], [1, 0]]);
    /* swing 1 = aufgeladen, -0.55 = tiefster Punkt des Hiebs */
    const armX = swing >= 0 ? -1.9 * swing : 1.5 * -swing;
    rot[B.spine] = [swing >= 0 ? -0.18 * swing : 0.30 * -swing, 0, 0];
    rot[B.chest] = [swing >= 0 ? -0.14 * swing : 0.22 * -swing, 0, 0];
    rot[B.head]  = [0.10 * swing, 0, 0];
    for (const s of [1, -1]) {
      rot[s>0?B.armL_up:B.armR_up]     = [armX, 0, s * 0.55 * Math.max(swing, 0.1)];
      rot[s>0?B.armL_fore:B.armR_fore] = [swing >= 0 ? -0.5 * swing : -0.2, 0, 0];
    }
    const lunge = track(p, [[0, 0], [0.45, 0.16, "an"], [1, 0, "ab"]]);
    off[B.root][2] = lunge;
    off[B.root][1] = -0.4 * lunge * 0.3;
  }
```

- [ ] **Schritt 3: `clipTreffer`**

Kurzes Zurückzucken (0,35 s, endet in Ruhepose):

```js
  function clipTreffer(p) {
    const k = track(p, [[0, 0], [0.18, 1, "an"], [1, 0, "ab"]]);
    rot[B.spine] = [-0.22 * k, 0.08 * k, 0];
    rot[B.chest] = [-0.16 * k, 0, 0];
    rot[B.head]  = [-0.20 * k, 0, 0];
    off[B.root][2] = -0.10 * k;
  }
```

- [ ] **Schritt 4: `CLIPS` erweitern und rendern**

`telegraph: { label: "Telegraph", dur: 0.55, fn: clipTelegraph }`, `angriff: { label: "Angriff", dur: 0.80, fn: clipAngriff }`, `treffer: { label: "Treffer", dur: 0.35, fn: clipTreffer }`.

```bash
node render.js 0.58 0.16 telegraph_zyklus.png 0 telegraph 6
node render.js 0.58 0.16 angriff_zyklus.png 0 angriff 8
node render.js 0.58 0.16 treffer_zyklus.png 0 treffer 5
```

Prüfen: Telegraph-Endphase ≈ Angriff-Startphase (Bild 6 von Telegraph gegen Bild 1 von Angriff); der Hieb-Tiefpunkt liegt bei Angriff-Phase ~4/8.

- [ ] **Schritt 5: Prüfnachweis**

Drei Phasenbilder abgelegt und begutachtet; Übergang Telegraph→Angriff visuell stetig.

---

### Aufgabe 8: Clips „Taumeln" und „Tod"

**Dateien:**
- Ändern: `…/Wildling3D/Source~/wildling.html` (Abschnitt 6)

**Schnittstellen:**
- Liefert: `CLIPS`-Einträge `taumeln` (dur 2.50, schleift sauber) und `tod` (dur 1.60, endet in liegender Endpose, schleift nicht)

- [ ] **Schritt 1: `clipTaumeln`**

Benommenes Schwanken: Oberkörper kreist unrund, Kopf hängt, Arme baumeln — Sinusbasis, damit p=0 und p=1 identisch sind:

```js
  function clipTaumeln(p) {
    const t = p * TAU;
    rot[B.hips]  = [0.05, 0, Math.sin(t) * 0.10];
    rot[B.spine] = [0.16 + Math.sin(t * 2) * 0.05, Math.sin(t) * 0.12, Math.sin(t + 0.8) * 0.10];
    rot[B.chest] = [0.10, Math.sin(t) * 0.08, 0];
    rot[B.head]  = [0.28 + Math.sin(t * 2 + 0.5) * 0.08, Math.sin(t * 1.0) * 0.20, 0];
    for (const s of [1, -1]) {
      rot[s>0?B.armL_up:B.armR_up]     = [0.10 + Math.sin(t + s) * 0.06, 0, s * 0.10];
      rot[s>0?B.armL_fore:B.armR_fore] = [0.15, 0, 0];
    }
    off[B.root][0] = Math.sin(t) * 0.05;
    off[B.root][1] = -0.03 + Math.sin(t * 2) * 0.012;
  }
```

- [ ] **Schritt 2: `clipTod`**

Zusammensinken: kurzes Aufbäumen, dann vornüber auf die Knie und flach; letztes Drittel hält die Endpose (glTF-Sampler klemmen am letzten Keyframe):

```js
  function clipTod(p) {
    const rear = track(p, [[0, 0], [0.15, 1, "ab"], [0.30, 0.6], [1, 0.6]]);
    const fall = track(p, [[0, 0], [0.25, 0], [0.62, 1, "an"], [0.75, 0.94, "ab"], [1, 0.94]]);
    const sink = track(p, [[0, 0], [0.35, 0.35, "an"], [0.75, 0.80, "ab"], [1, 0.80]]);
    rot[B.spine] = [-0.25 * rear + 1.05 * fall, 0, 0.06 * fall];
    rot[B.chest] = [-0.15 * rear + 0.55 * fall, 0, 0];
    rot[B.head]  = [0.15 * rear + 0.45 * fall, 0.1 * fall, 0];
    for (const s of [1, -1]) {
      rot[s>0?B.thighL:B.thighR] = [1.35 * sink, 0, s * 0.12 * sink];
      rot[s>0?B.shinL:B.shinR]   = [-1.9 * sink, 0, 0];
      rot[s>0?B.armL_up:B.armR_up]     = [-0.35 * rear + 0.5 * fall, 0, s * (0.15 + 0.25 * fall)];
      rot[s>0?B.armL_fore:B.armR_fore] = [-0.2 * rear, 0, 0];
    }
    off[B.root][1] = -0.62 * sink;
    plantAmount = 1 - fall;          /* am Boden keine Fußsetzung mehr erzwingen */
  }
```

- [ ] **Schritt 3: `CLIPS` vervollständigen und rendern**

```js
  const CLIPS = {
    ruhe:      { label: "Ruhe",      dur: 5.00, fn: clipIdle },
    gehen:     { label: "Gehen",     dur: 0.80, fn: clipGehen },
    telegraph: { label: "Telegraph", dur: 0.55, fn: clipTelegraph },
    angriff:   { label: "Angriff",   dur: 0.80, fn: clipAngriff },
    treffer:   { label: "Treffer",   dur: 0.35, fn: clipTreffer },
    taumeln:   { label: "Taumeln",   dur: 2.50, fn: clipTaumeln },
    tod:       { label: "Tod",       dur: 1.60, fn: clipTod }
  };
```

```bash
node render.js 0.58 0.16 taumeln_zyklus.png 0 taumeln 6
node render.js 1.10 0.25 tod_zyklus.png 0 tod 8
```

Prüfen: Taumeln Phase 0 = letzte Phase; Tod endet flach am Boden ohne Durchdringung des Bodens (Endpose in den letzten 2 Phasen identisch).

- [ ] **Schritt 4: Prüfnachweis**

Beide Phasenbilder abgelegt und begutachtet; alle 7 Clips im Browser-Viewer anwählbar und fehlerfrei.

---

### Aufgabe 9: Export- und Prüfskript anpassen, GLB erzeugen

**Dateien:**
- Erstellen: `…/Wildling3D/Source~/export_gltf.js` (Kopie vom Wanderer, angepasst)
- Erstellen: `…/Wildling3D/Source~/verify_gltf.js` (Kopie vom Wanderer, angepasst)
- Erzeugt: `…/Wildling3D/Wildling.glb`

**Schnittstellen:**
- Verbraucht: `wildling.html` vollständig (Aufgaben 2–8)
- Liefert: `Wildling.glb` — 1 Mesh `Wildling`, Skin `WildlingSkin`, Material `M_Wildling_Vertexfarben`, 7 Clips `Ruhe|Gehen|Telegraph|Angriff|Treffer|Taumeln|Tod`

- [ ] **Schritt 1: `export_gltf.js` anpassen**

Kopieren, dann:
- `wanderer.html` → `wildling.html`; Standard-Ausgabe `path.join(DIR, "..", "Wildling.glb")`
- Gruppierung ersetzen — ein einziges Mesh:

```js
  const ORDER = ["Wildling"];
  const groups = new Map([["Wildling", parts]]);
```

(`SLOT_NAME`/`SET_NAME`/`groupKey` entfallen; die Rückgabe des `new Function`-Blocks braucht kein `equip` mehr, `M.equip`-Zeilen streichen.)
- Clip-Tabelle ersetzen:

```js
  const CLIP_EXPORT = [
    { key: "ruhe",      name: "Ruhe" },
    { key: "gehen",     name: "Gehen" },
    { key: "telegraph", name: "Telegraph" },
    { key: "angriff",   name: "Angriff" },
    { key: "treffer",   name: "Treffer" },
    { key: "taumeln",   name: "Taumeln" },
    { key: "tod",       name: "Tod" }
  ];
```

  und in der Exportschleife `M.equip.waffe = cl.waffe;` streichen.
- Benennungen: Szene `Wildling`, Skin `WildlingSkin`, Material `M_Wildling_Vertexfarben`, Generator `Eidren Wildling Export`

- [ ] **Schritt 2: Exportieren**

```bash
node export_gltf.js
```

Erwartet: Konsole listet 1 Mesh (`Wildling`, 1.500–2.500 Dreiecke), 17 Knochen, 7 Clips mit plausiblen Dauern (5,00 / 0,80 / 0,55 / 0,80 / 0,35 / 2,50 / 1,60 s); `Wildling.glb` liegt neben dem README-Zielort.

- [ ] **Schritt 3: `verify_gltf.js` anpassen und laufen lassen**

Kopieren, dann: `wanderer.html`→`wildling.html`, GLB-Standardpfad `../Wildling.glb`, Vergleichsclip `Angriff` (Bild 12 → mittleres Drittel), `M.equip`-Zeilen streichen, Stichprobenfilter auf alle Teile stellen (`if (…) continue;`-Zeile entfernen).

```bash
node verify_gltf.js
```

Erwartet: `Alle Accessor-Grenzen ok`, Skinning-Abweichung < 2 mm, `PRUEFUNG BESTANDEN`.

- [ ] **Schritt 4: Prüfnachweis**

Konsolenausgaben von Export und Verify vollständig festhalten (keine gekürzten oder behaupteten Ergebnisse).

---

### Aufgabe 10: README und Abnahme

**Dateien:**
- Erstellen: `…/Wildling3D/README_Wildling3D.md`

**Schnittstellen:**
- Verbraucht: Ergebnisse aller Aufgaben (Dreieckszahl, Clipdauern, Prüfbilder)

- [ ] **Schritt 1: README schreiben**

Nach dem Muster von `README_Wanderer3D.md`: Inhalt des GLB (1 Mesh, 17 Knochen, 7-Clip-Tabelle mit Dauer und Anmerkung inkl. Treffer-Zeitpunkt des Angriffs), Unity-Import (glTFast, Vertexfarben-Shader), Maße/Konventionen (Maßstab, Dreieckszahl, harte Skinnierung, gebückte Ruhepose), Quelle & Regeneration (`Source~`-Dateien, Reihenfolge Export→Verify).

- [ ] **Schritt 2: Abnahmebilder erzeugen**

```bash
node render.js 0.58 0.16 abnahme_front.png 0 ruhe 1
node render.js 2.20 0.16 abnahme_seite.png 0 ruhe 1
node render.js 0.58 0.16 abnahme_clips_gehen.png 0 gehen 8
node render.js 0.58 0.16 abnahme_clips_angriff.png 0 angriff 8
node render.js 1.10 0.25 abnahme_clips_tod.png 0 tod 8
```

- [ ] **Schritt 3: Abnahme gegen den Entwurf**

Jedes Kriterium aus `WILDLING3D_ENTWURF.md`, Abschnitt „Prüfung / Abnahme", einzeln nachweisen:
1. Export fehlerfrei ✓/✗ (Ausgabe zeigen)
2. Verify bestanden ✓/✗ (Ausgabe zeigen)
3. Prüfbilder: mind. eine Pose pro Clip + Front/Seite; Wiedererkennbarkeit gegen Sprite begründen
4. Dreieckszahl im Korridor ✓/✗ (Zahl nennen)

Dem Auftraggeber die Abnahmebilder zeigen und Rückmeldung einholen, bevor die Arbeit für abgeschlossen erklärt wird.
