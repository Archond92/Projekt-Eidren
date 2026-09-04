# Spezifikation: Mid-Poly-Grundkörper „Wanderer"

**Stand:** 27.08.2026 · **Status:** vom Nutzer freigegeben · **Nummernkreis:** GK-xx

Erster Baustein des Neubaus, nachdem der alte Mid-Poly-Wanderer am 27.08.2026
komplett entfernt wurde (Sicherung:
`C:\Users\phine\Documents\Eidren-Sicherungen\MidPolyWanderer_Komplett_20260827\`).

**Vorlage:** `Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb`, Objekt
`Basis` — 764 Dreiecke, 2.292 Vertices (jedes Dreieck lose, keine geteilten
Ecken), Vertexfarben, Rig `WandererSkin` mit 20 Knochen.

---

## GK-01 — Zielbild

Ein **vollständiger Körper einschließlich Kopf und Gesicht**, der ohne jede
Ausrüstung für sich steht. Das ist die Umkehrung des Kardinalfehlers der
Altfassung: dort bestand der Grundkörper nur aus einem Kopf, ein
unausgerüsteter Wanderer war ein schwebender Kopf.

„Nackt" bedeutet Unterkleidung, keine Anatomie-Studie: nackter Oberkörper,
Hose, Stiefel — wie in der Vorlage.

## GK-02 — Dreiecksbudget

**Ziel ~10.000, Obergrenze 10.500 Dreiecke** (Vorlage: 764, Faktor ~13).

Maßstab im Projekt (LOD0 der übrigen Mid-Poly-Figuren): MoorThrower 15.680,
GraniteShell 16.868, RootCharger 25.172 — jeweils für die **ganze** Kreatur.
Voll ausgerüstet landet der Wanderer damit bei rund 25.000–30.000. Der alte
Mid-Poly-Wanderer lag bei **162.764** in LOD0 und war damit sechs- bis
zehnmal schwerer als jede andere Figur des Projekts; diese Grenze ist die
zentrale Lehre aus dem Rückbau.

Verteilung: Kopf mit Gesicht ~2.400 · Rumpf und Hüfte ~1.900 · beide Arme
mit Fäusten ~1.900 · beide Beine mit Füßen ~2.800 · Hals, Gelenkringe und
Kappen ~1.000.

## GK-03 — Topologie: eine durchgehende Haut

Arme und Beine sind in Rumpf und Hüfte **eingebunden**, nicht angesetzt. Die
Vorlage stapelt lose Ringe je Knochen, weshalb ihre Gelenke in starken
Beugungen sichtbar aufreißen; der Neubau schließt das.

Bauweise: ein grobes, geschlossenes Steuernetz (Cage) wird per bmesh aus
Extrusionen erzeugt — Hüfte → Rumpf → Hals → Kopf, Arme aus den
Schulterflächen, Beine aus den Hüftflächen, Füße nach vorn. Die Verschweißung
entsteht dabei konstruktiv. Anschließend zwei Stufen Unterteilung auf die
Zieldichte.

Jedes Gelenk (Schulter, Ellbogen, Hüfte, Knie, Knöchel) erhält drei eng
gesetzte Zwischenringe mit weicher Zwei-Knochen-Gewichtung.

## GK-04 — Proportionen (bindend)

Exakt die Maße der Vorlage, damit die späteren Rüstungsmodule passen und alle
25 Clips ohne Nacharbeit sitzen.

| Teil | Breite × Tiefe (m) | Höhe z (m) |
|---|---|---|
| Kopf | 0,216 × 0,261 | 1,470–1,798 |
| Rumpf | 0,386 × 0,246 | 1,030–1,500 |
| Hüfte | 0,362 × 0,274 | 0,880–1,030 |
| Oberarm | 0,153 × 0,128 | 1,150–1,375 |
| Unterarm | 0,108 × 0,115 | 0,905–1,150 |
| Faust | 0,094 × 0,104 | 0,782–0,912 |
| Oberschenkel | 0,195 × 0,205 | 0,560–0,900 |
| Wade | 0,152 × 0,163 | 0,210–0,560 |
| Fuß | 0,128 × 0,222 | 0,002–0,210 |

Arme seitlich: Oberarm x 0,133–0,286 · Unterarm x 0,190–0,298 · Faust
x 0,214–0,308. Gesamthöhe 0,002–1,798 m.

## GK-05 — Farbe

Vertexfarben wie die Vorlage, drei Zonen mit leichter Ring-zu-Ring-Streuung
(sie gibt dem Low-Poly seine lebendige Oberfläche):

- Haut `(0.79, 0.60, 0.45)`
- Hose `(0.13, 0.11, 0.10)`
- Stiefel `(0.08, 0.07, 0.05)`

## GK-06 — Rig

Bindung an das vorhandene Rig `WandererSkin` (20 Knochen). Der Körper nutzt
die 15 Körperknochen; `root`, `skirt` und die drei Waffenknochen bleiben
Rüstung und Waffen vorbehalten. Knochennamen und -lagen bleiben unverändert,
damit die 25 vorhandenen Clips unmittelbar greifen.

## GK-07 — Bewusst nicht enthalten

Keine einzelnen Finger (Faust wie in der Vorlage), keine Gesichtsanimation,
keine Anatomie-Studie, keine LOD-Stufen in diesem Schritt.

## GK-08 — Abnahmekriterien (messbar)

1. Höchstens **10.500 Dreiecke**.
2. **Geschlossenes Volumen** — keine offenen Ränder (`non_manifold` leer).
3. Silhouette weicht an keiner Stelle mehr als **2 cm** von der Vorlage ab.
4. Gesamthöhe **1,798 m** (±5 mm), Fußunterkante bei 0,002 m.
5. In allen **25 Clips**: keine aufreißenden Gelenke, keine
   Selbstdurchdringung des Körpers.
6. Alle Vertices gewichtet (kein ungewichteter Vertex).
7. Aufbau **reproduzierbar per Skript**, Parameter (Dichte, Radien,
   Proportionen) an einer Stelle einstellbar.

## Offene Punkte

- **Farbträger:** Vertexfarben (hier festgelegt, wie die Vorlage) gegenüber
  benannten Materialien wie bei den übrigen Mid-Poly-Figuren. Umstellbar,
  falls die Unity-Anbindung es verlangt.
- **LOD-Kette:** erst nach Abnahme des Grundkörpers.
