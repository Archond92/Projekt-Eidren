# Eidren v0.3.3 – Mid-Poly-Entwicklerversion

Version 0.3.3 ist der neue Windows-Entwicklerstand zur grafischen und
mechanischen Abnahme. Gegenüber v0.3.2 wurde die sichtbare Spielwelt auf Mid
Poly umgestellt; der Wanderer wurde abschließend neu aufgebaut und ersetzt nun
auch im produktiven Spieler-Prefab die bisherige Low-Poly-Fassung.

Dieser Download enthält bewusst einen Vollausbau-Spielstand. Er verwendet den
eigenen Produktnamen `Eidren Developer MidPoly` und überschreibt daher keinen
Spielstand der regulären Eidren-Version.

## Neu: vollständige Mid-Poly-Spielwelt

- 150 von 150 erfassten Migrationsobjekten sind abgenommen.
- Wanderer, 14 Kreaturen und Garon besitzen produktive Mid-Poly-Modelle mit
  ihren vollständigen Rig- und Animationsverträgen.
- Gebäude, Produktionsstationen, Lager- und Weltkisten, Schmiedecontainer,
  Weltbeute, Ressourcen, Vegetation, Felsen, Ruinen und Zonenrequisiten wurden
  in dieselbe Formensprache überführt.
- Die produktiven Prefab-GUIDs, Collider, Interaktionsdaten und Gameplaywerte
  bleiben erhalten. Die Grafik verwendet abgestufte LOD-Modelle; alte Assets
  liegen nur noch im lokalen, nicht referenzierten Rückfallarchiv.

## Der neue Wanderer

- Ein gemeinsames 20-Knochen-Rig trägt Grundkörper, Haare, zwölf Rüstungsteile
  für Stoff, Kupfer und Eisen sowie sechs Waffen-/Werkzeugfamilien.
- 138 Meshteile mit insgesamt 76.926 Dreiecken über alle gespeicherten
  Varianten; im Spiel ist nur die gewählte Kombination sichtbar.
- 28 Animationsclips: Ruhe, Gehen und Laufen für Speer, Dolche, Hammer, Axt,
  Spitzhacke, Sense und unbewaffnet; dazu drei Angriffe, drei Abbauaktionen und
  Öffnen.
- Werkzeugstiele liegen in beiden Händen, zyklische Bewegungen schließen ohne
  Posensprung und Waffen oder Werkzeuge werden nicht durch Körper oder Rüstung
  geführt. Kapuze und Helm blenden die Haare passend aus.
- Stoff-, Kupfer- und Eisenrüstung verwenden dieselben Bewegungen wie der
  Grundkörper. Die vorherige Low-Poly-Fassung bleibt wiederherstellbar
  archiviert, wird vom Spiel aber nicht mehr verwendet.

## Mechanik- und Sichtbarkeitskorrekturen seit v0.3.2

- Interaktionen messen einheitlich bis zur sichtbaren Körperoberfläche eines
  Ziels. Die zulässige Distanz beträgt 1,5 m ab Collider beziehungsweise
  Renderer statt ab Objektursprung.
- Nahkampf, Zielhilfe und Speerreichweite messen bei großen Gegnern ebenfalls
  bis zum Körper statt bis zu dessen Mittelpunkt.
- Kleine gebaute Mid-Poly-Objekte verschwinden nicht mehr unmittelbar nach dem
  Platzieren; ihre letzte LOD-Stufe bleibt bei der normalen Spielkamera aktiv.
- Die Öffnen-Animation besitzt wieder eine klare Greif-, Öffnungs- und
  Rückkehrphase. Axt, Spitzhacke, Sense und Hammer werden sichtbar beidhändig
  geführt.

## Vollfreigeschalteter Teststand

Beim ersten Start wird automatisch ein eigener Tester-Spielstand angelegt:

- Stufe 2 und Level 40;
- alle 28 erreichbaren Technologien und alle acht Gebiete;
- Terrock, Noctarion und Ignivar, davon zwei aktiv;
- vollständige Eisenrüstung, Eisenspeer und Eisendolche;
- Material für alle derzeit verfügbaren Gebäudetypen.

## Abnahme

- EditMode: **1.355/1.355 bestanden**, 0 fehlgeschlagen.
- PlayMode mit Direct3D 11: **148 bestanden**, 0 fehlgeschlagen, 3 bewusst
  übersprungen (reine Diagnose-/Capture-Helfer).
- Zusätzliche Wanderer-/Vollausbau-Prüfung: **93/93 bestanden**.
- Windows-x64-Development-Build und 20-Sekunden-Player-Smoke bestanden;
  Direct3D 11 und der Vollausbau-Spielstand wurden im Player-Log bestätigt.
- ZIP-Inhalt: **208/208 Dateien** gegenüber dem geprüften Buildordner, keine
  Größenabweichung.

## Download und technische Daten

- Archiv: `Eidren-v0.3.3-windows-x64.zip`
- Startdatei: `Eidren-Developer-MidPoly.exe`
- Plattform: Windows x64
- Unity: `6000.3.0f1`
- Grafikpfad: Direct3D 11
- Buildtyp: Unity Development Build
- Downloadgröße: 301.801.047 Bytes / 287,82 MiB
- SHA-256:
  `58dfc80eb491ea1c3e3d34f34fcb9ff70fb408c46a411e621304cb10751f3f49`

Das ZIP muss vollständig entpackt werden. Die EXE darf nicht ohne den
zugehörigen Data-Ordner und die DLL-Dateien verschoben werden. Der Build ist
nicht digital signiert; Windows kann deshalb eine SmartScreen-Warnung zeigen.

## Empfohlener Testpfad

1. Wanderer ohne Rüstung sowie mit Stoff-, Kupfer- und Eisenrüstung prüfen.
2. Ruhe, Gehen und Laufen unbewaffnet und mit verschiedenen Handobjekten
   vergleichen.
3. Axt, Sense und Spitzhacke beim Abbau sowie Dolche, Hammer und Speer im Kampf
   testen; die Werkzeuge dürfen Körper und Rüstung nicht durchdringen.
4. Kleine Gebäude wie Boden, Kochtopf und Lagerkiste bauen und die Kamera auf
   normale Spielentfernung zurücknehmen.
5. Interaktionen an großen Gebäuden, Ressourcen und Kisten von deren sichtbarer
   Außenkante aus prüfen.

