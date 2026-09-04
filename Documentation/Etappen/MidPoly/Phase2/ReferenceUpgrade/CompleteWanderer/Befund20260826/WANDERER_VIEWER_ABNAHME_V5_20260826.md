# Wanderer-Viewer V5 — Nachabnahme vom 26.08.2026

**Ergebnis: bestanden.** Die offenen Befunde F33-002 bis F33-006 aus der
V4-Prüfung sind im reproduzierbaren Wanderer-Generator, in den Runtime-GLBs
und im Viewer V5 behoben.

## Behobene Punkte

- `Ohne` zeigt den 628-Dreiecke-Grundkörper in LOD0, LOD1 und LOD2.
- Alle sechs Laufclips schließen ohne Positions- oder Rotationssprung.
- `Oeffnen` beginnt im Stand, greift sichtbar vor und kehrt zur Startpose zurück.
- Axt, Spitzhacke, Sense und Hammer besitzen gebackenen Zweithandkontakt.
- Der Viewer kann LOD0, LOD1 und LOD2 umschalten.
- Backface-Culling ist als Rückseitenprüfung standardmäßig aktiv und umschaltbar.
- Erstellzeitpunkt und SHA-256-Kurzprüfsumme werden je LOD angezeigt.
- Die Ausrüstungsfamilie bleibt beim Variantenwechsel korrekt zugeordnet.

## Messwerte

| Prüfung | Ergebnis |
| --- | --- |
| GLB-Dateien | Low Poly, LOD0, LOD1, LOD2 und Production bestanden |
| Clips | 25/25 je GLB |
| Schleifen | 0,000000 m / 0,000000 Grad Restfehler |
| `Oeffnen` | 6,43 cm Wurzelbewegung / 51,45 Grad Oberarmbewegung |
| Größter Zweithandabstand | 1,23 cm |
| Viewer-Grundkörper | 628 Dreiecke in 3/3 LODs |
| Unity-Tests | 12/12 bestanden |

Die Datenprüfung kontrolliert endliche Schlüsselwerte, streng aufsteigende
Zeitachsen, Schleifengrenzen, Öffnungsbewegung, Zweithandkontakt und die drei
eingebetteten Viewer-LODs. Die Unity-Tests prüfen zusätzlich beide importierten
Rigs, alle 256 Rüstungskombinationen, alle 20 Ausrüstungsvarianten, alle 25
Clips, den Grundkörper und unveränderte Knochen-Skalierung.

## Artefakte

- Viewer: `Befund20260824/WANDERER_3D_VIEWER_V5.html`
- Viewer-Manifest: `Befund20260824/WANDERER_3D_VIEWER_V5_MANIFEST.json`
- Animationsaudit: `Befund20260826/WANDERER_ANIMATION_AUDIT_V5.json`
- Reproduzierbarer Aufbau: `Tools/build_wanderer_viewer_v5.js`
- Reproduzierbare Abnahme: `Tools/audit_wanderer_animation_v5.js`

Die vor der Animationserneuerung gesicherten vier Mid-Poly-Runtime-GLBs liegen
unter `Backups/Wanderer_F33_003_005_20260826/`.

## Spielerpaket

Der abschließende Windows-Build `0.3.3-maxloadout` liegt unter
`Releases/Eidren-MaxLoadout-windows-x64/`. Der Produktname
`Eidren Max Loadout F33` trennt seinen Vollausbau-Spielstand von älteren
Testpaketen. Der 20-Sekunden-Starttest lief ohne Laufzeitfehler und bestätigte
die Übernahme des eingebetteten Tester-Spielstands. Die spielbare Figur bleibt
wie gefordert Low Poly.
