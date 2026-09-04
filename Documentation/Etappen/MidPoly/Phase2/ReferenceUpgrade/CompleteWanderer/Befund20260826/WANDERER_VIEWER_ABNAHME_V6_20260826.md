# Wanderer V6 – visuelle Abnahme vom 26.08.2026

## Kurzurteil

Der korrigierte Mid-Poly-Stand ist technisch und in den deterministischen
Blender-Ansichten abgenommen. Der Kopf blickt in `Oeffnen` nach vorn. Bei
`Abbau_Axt`, `Abbau_Spitzhacke`, `Abbau_Sense` und `Angriff_Hammer` führen
beide Hände das Werkzeug; die Ellbogen bleiben außerhalb des Rumpfs und die
Werkzeuge vor Körper, Hals und Kopf.

Der Viewer V6 enthält bytegenau die neu exportierten LOD0-, LOD1- und
LOD2-GLBs. Eine erneute interaktive Browser-Abnahme der lokalen `file:`-Seite
war in dieser Ausführung durch die Browser-Sicherheitsrichtlinie gesperrt.
Deshalb wird kein ausschließlich browserbasiertes PASS behauptet; die neue
Animation wurde stattdessen direkt aus der verbindlichen Blender-Quelle und
den daraus erzeugten Runtime-Dateien geprüft.

## Visuell geprüft

- `Oeffnen`: 50 %, vorn und links; Kopf und Gesicht korrekt nach vorn.
- Vier korrigierte Zweihandaktionen: 0 %, 25 %, 50 %, 75 % und 100 %.
- Jeweils Vorder- und rechte Seitenansicht ohne Rüstung.
- Repräsentative Vorder-/Seiten-/Rückansichten mit vollständiger
  Kupferrüstung.
- Keine sichtbare Arm-Rumpf-Durchdringung, keine überstreckten oder nach innen
  geknickten Ellbogen und keine Werkzeugdurchdringung von Kopf, Hals oder
  Torso.

Die 40 vollständigen Aktionsbilder liegen unter `FullActionChecks`; die
Kopf- und Rüstungsbilder unter `ScreenshotsV6`.

## Technische Gegenprüfung

- Primärhand: maximal 0,0000121 m Abstand zur Griffoberfläche.
- Zweithand: maximal 0,021183 m Abstand; Grenzwert 0,03 m.
- Unterarm/Rumpf-Freigabescore: mindestens 1,4528; Grenzwert größer 1,0.
- Hände mindestens 0,2415 m vor der Rumpfmittellinie.
- Alle relevanten Frames geprüft, nicht nur Schlüsselbilder.
- Vier Runtime-GLBs mit 28 Clips; alle Loop-Grenzen 0 m / 0°.
- Unity EditMode: 6/6 Tests bestanden.
- Produktiver Low-Poly-Wanderer unverändert.

## Bewusst akzeptierte Restabweichungen

- Die Hände besitzen beim stilisierten Wanderer keine einzeln animierten
  Finger; der Griff wird über den Hand-Effektor am Schaft bewertet.
- Die Sense erreicht im ungünstigsten Frame 2,12 cm Zweithandabstand. Das
  liegt innerhalb des verbindlichen 3-cm-Zielwerts und ist in den geprüften
  Ansichten nicht als schwebender Griff sichtbar.
- Der produktive Spieler bleibt absichtlich auf Low-Poly, weil
  `wandererPilotEnabled=false`; es wurde kein Produktionswechsel erzwungen.
- Die erneute interaktive lokale Viewer-Sichtprüfung bleibt wegen der oben
  genannten Browser-Sperre offen. Viewer und Runtime-Dateien sind jedoch
  byteidentisch.
