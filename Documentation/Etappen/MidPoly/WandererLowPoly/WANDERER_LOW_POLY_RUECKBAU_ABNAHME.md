# Wanderer – Low-Poly-Rückbau und Viewer-Abnahme

**Stand:** 25.08.2026  
**Status:** technisch und visuell abgenommen

## Ergebnis

Der produktive Wanderer verwendet wieder die ursprüngliche Low-Poly-Datei
`Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb`. Das zugehörige
`Player_3D`-Prefab und das produktive `Player`-Prefab wurden daraus neu gebaut;
ihre bestehenden GUIDs blieben erhalten. Die Wanderer-Mid-Poly-Dateien bleiben
als Rückfall- und Historienstand archiviert, werden aber nicht mehr als
Produktionsdarstellung ausgewählt.

Die Rückkehr umfasst die direkt zum Wanderer gehörenden Inhalte:

- 6.506 von Unity importierte Dreiecke und das ursprüngliche Rig mit 20 Knochen;
- zwölf Rüstungsteile: Kopf, Brust, Hände und Beine jeweils in Stoff, Kupfer
  und Eisen;
- 20 benannte Waffen- und Werkzeugvarianten, wie im ursprünglichen Produktstand
  auf sechs Low-Poly-Familienmeshes abgebildet;
- 25 abspielbare Legacy-Animationsclips.

## Viewer

Review-Szene:
`Assets/_Game/Scenes/Review/WandererLowPolyViewer.unity`

Unity-Menü:
`Eidren > Viewer > Wanderer Low-Poly Viewer bauen und starten`

Der Viewer bietet vollständige Rüstungssets und einzelne Rüstungsplätze,
sämtliche Handitemvarianten, alle Animationen, Pause/Neustart, Abspieltempo,
Item-zu-Clip-Zuordnung, automatische Drehung, Mausorbit und Zoom. Das Layout
passt Bedienfeld und Modellrahmung an die aktuelle Game-View-Größe an. Die
Review-Szene ist absichtlich deaktiviert in den Player-Build-Einstellungen und
verändert den Spielfluss nicht.

Maschinenlesbarer Beleg:
`WANDERER_LOW_POLY_VIEWER_REPORT.json`

Visueller Beleg:
`WANDERER_LOW_POLY_VIEWER-1.png`

## Prüfungen

- gezielte Wanderer-/Prefab-/Viewer-Suite: 12/12 bestanden;
- vollständiger EditMode-Lauf: 1.355/1.355 bestanden;
- vollständiger PlayMode-Lauf: 148 bestanden, null fehlgeschlagen, drei bewusst
  übersprungen;
- Mid-Poly-Gesamtprüfung mit dokumentierter Wanderer-Ausnahme: PASS, keine
  unbeabsichtigten Legacy-Assets im Laufzeitbestand;
- Windows-x64-Snapshot: 101/101 Dateien zwischen Staging, ZIP und Manifest
  identisch; Direct3D-11-Smoke über 15 Sekunden ohne fatales Logmuster;
  SHA-256
  `cc2a07bad3192d88a1d269fa58ace8c9f1dfb2484da1c84ce6d7d85e323375df`.
