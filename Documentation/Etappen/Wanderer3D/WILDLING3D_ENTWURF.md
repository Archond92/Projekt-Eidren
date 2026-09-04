# Wildling 3D — Low-Poly-Gegnerfigur (Design)

Datum: 06.08.2026 · Status: vom Auftraggeber freigegeben (Chat)

## Ziel

Den Gegner „Wildling" (bisher 2D-Sprite-Billboard) als Low-Poly-3D-Figur
nach dem bewährten Wanderer3D-Muster erstellen: prozedurale Three.js-Quelle,
Export als GLB mit Vertexfarben, Verifikation und Prüfbilder ohne Unity.

**Nicht Teil dieses Auftrags:** Einbau ins Spiel (Umbau von
WildlingController/Prefab, Ablösen der 2D-Sprites). Das ist ein separater
Folgeschritt.

## Gestalt

Treu zum bestehenden 2D-Sprite
(`Assets/_Game/Resources/Art/Actors/Wildling/wildling_idle_albedo.png`):

- Gebückte humanoide Moos-/Ranken-Kreatur; effektive Höhe gebückt
  ca. 1,6–1,7 m, aufgerichtet ca. 2,0 m (NavMesh-Agent: Höhe 2,1, Radius 0,52)
- Lange Arme mit Krallenhänden, kurze kräftige Beine
- Farbwelt aus dem Sprite: moosgrüner Körper, braune Rinden-/Rankenpartien,
  glimmende Augen (Emission in Vertexfarben eingebacken)
- Ziel ~1.500–2.500 Dreiecke, Flat Shading über Normalen, sRGB-Vertexfarben,
  keine Texturen
- Maßstab 1 Einheit = 1 m, Blickrichtung +Z (Konvention wie Wanderer)

## Rig

Eigenes Skelett, ca. 15 Knochen: Wurzel, Hüfte, Wirbelsäule ×2, Kopf,
je Arm 3 (Schulter/Ellbogen/Hand), je Bein 3 (Hüfte/Knie/Fuß).
Harte Skinnierung: genau 1 Knochen pro Vertex, Gewicht 1,0.

## Animationsclips

7 Clips, 30 fps. Zeiten orientieren sich an `Wildling.asset`:

| Clip | Verhalten | Anmerkung |
| --- | --- | --- |
| `Ruhe` | Atmen, Umherblicken | schleift sauber |
| `Gehen` | Bewegung, passend zu MoveSpeed 3,6 m/s | schleift sauber |
| `Telegraph` | Aufladen vor dem Angriff | 0,55 s (telegraphDuration) |
| `Angriff` | Prankenhieb | Trefferfenster ~0,18 s (attackWindowDuration) |
| `Treffer` | kurzes Zucken | |
| `Taumeln` | Benommenheit, 2,5 s haltbar | schleift sauber (staggerDuration) |
| `Tod` | Zusammensinken | endet in Endpose, schleift nicht |

## Ablage

`Assets/_Game/Art/Actors/Wildling/Wildling3D/` — spiegelbildlich zum
Wanderer (`…/Player/Wanderer3D/`):

- `Wildling.glb` — das Ergebnis (Mesh + Rig + 7 Clips, Vertexfarben)
- `README_Wildling3D.md` — Inhalt, Import (glTFast), Maße, Regeneration
- `Source~/` (von Unity ignoriert):
  - `wildling.html` — Geometrie, Rig, Clips und Browser-Viewer in einem
  - `export_gltf.js` — erzeugt `Wildling.glb` (`node export_gltf.js`)
  - `verify_gltf.js` — prüft GLB gegen das Rig (Skinning-Abgleich)
  - `render.js` — Offline-Renderer für Prüfbilder ohne Browser

Die Skripte werden vom Wanderer übernommen und auf das Wildling-Rig
angepasst.

## Prüfung / Abnahme

1. `node export_gltf.js` läuft fehlerfrei und schreibt `Wildling.glb`
2. `node verify_gltf.js` meldet keinen Skinning-Fehler
3. Prüfbilder aus `render.js`: mindestens eine Pose pro Clip (7 Bilder)
   plus eine Ansicht von vorn/seitlich; Abgleich mit dem 2D-Sprite auf
   Wiedererkennbarkeit (Silhouette, Farbwelt, Augen)
4. Dreieckszahl im Zielkorridor (~1.500–2.500)

## Risiken

- Farbabgleich mit dem Sprite geschieht über Messwerte (`lupe.js`-Ansatz
  des Wanderers kann mitverwendet werden), nicht nach Gefühl
- Ein paralleler codex-Agent arbeitet zeitweise im Projekt: keine fremden
  Dateien anfassen, Unity-Lockfile nur prüfen, nie löschen
