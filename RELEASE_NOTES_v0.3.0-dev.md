# Eidren v0.3.0-dev – Windows-Testversion

Dieser Entwicklungs-Snapshot enthält den vollständigen bisherigen Spielweg. Er
ist für einen breiten Spieltest vorgesehen und noch nicht als finale Version
klassifiziert.

**Du beginnst bei null.** Anders als das interne Testpaket bringt diese Ausgabe
keinen vorgefertigten Spielstand mit — kein Maximallevel, keine
freigeschalteten Technologien, keine Ausrüstung. Der Weg von der leeren
Heimatbasis bis zur Eidra-Schmiede ist der Test.

## Wichtigste Änderungen gegenüber v0.2

Die letzte hier veröffentlichte Fassung war der v0.2-Snapshot vom 04.08.2026.
Alles Folgende ist seither entstanden.

### Das Spiel ist jetzt durchgehend räumlich

Die gesamte Darstellung wurde von gemalten 2D-Figuren auf handgebaute
3D-Modelle umgestellt — die größte einzelne Änderung dieser Ausgabe.

- Der Wanderer läuft als Modell mit 24 Animationszuständen. Werkzeuge und
  Waffen sind sichtbar in der Hand und wechseln mit der Ausrüstung.
- Alle fünfzehn Kreaturen sind Modelle mit vollem Animationssatz — Wildling,
  Riftling, Garon, Terrock, Noctarion, Ignivar, EmberEater, RootCharger,
  GraniteShell, AshRunner und die übrigen. 27 Gegner in sieben Gebieten wurden
  umgestellt.
- Rüstung wird als geschaltete Einzelteile am Modell getragen und lässt sich
  frei mischen.

### Die Welt sieht anders aus

- **Alle 44 Shader waren Platzhalter.** 170 Materialien waren betroffen — das
  war der Grund, warum die Welt vorher flach wirkte.
- **Boden und Gelände:** drei mischbare Materialien mit echten Übergängen in
  allen acht Gebieten, kein sichtbares Kachelraster mehr, 35 bis 215 Bewuchs-
  und Streuinstanzen je Gebiet, jede Zone mit eigener Bodenwirkung.
- **Beleuchtung:** Lichtrichtung und Schattenwurf stimmen überein, der
  Tonwertumfang am Boden ist um 35 Prozent gewachsen, jedes Gebiet hat eine
  eigene benannte Farbstimmung.
- **Bodenschatten:** 80 Weltobjekte und die Spielfigur werfen einen Schatten an
  ihrer Standposition.
- **Durchsichtige Objekte behoben:** die Seitenflächen aller gelofteten Objekte
  waren nach innen gewickelt und damit im Spiel unsichtbar.
- 87 Assets sind auf eine einheitliche Formsprache umgestellt.

### Die verlassene Eidra-Schmiede ist neu gebaut

Aus sechs gleichen Rechtecken auf einer Linie ist ein Grundriss geworden, der
ein Schmiedewerk ist: vom Blasebalg durch die Düse in die Gießhalle und von
dort in die Esse. Die eingestürzte Osthälfte der Gießhalle zeigt, was passiert
ist; die intakte Westseite zeigt, wie es gemeint war.

Drei Wege führen zur Esse, keiner ist Pflicht: die Rinne ist schnell, aber
beide Schmiedewächter stehen im Weg; die Masselreihe ist geordnet und hat zwei
Vorratstruhen; der Einbruch ist unübersichtlich und hat die dichteste
Gegnergruppe. Die Bindungskammer mit Ignivar liegt abseits — wer geradeaus zum
Boss läuft, verpasst sie.

Vier Fehler im alten Aufbau sind dabei entfallen: beide Seitenwege lagen hinter
geschlossenen Wänden, eine Truhe stand auf einer Stelle ohne Boden, sechs
Gegner steckten paarweise ineinander, und die Wände der Bossarena liefen mitten
durch den Raum.

### Neu: der Formwall

Der Einbruch hat statt einer Truhe einen **Bauplatz**. Wer das Nest der vier
Aschenläufer räumt und ihn ausbaut, macht die Schmiede an der Stelle wieder
betriebsfähig, an der sie gestorben ist.

| | |
| --- | --- |
| Kosten, einmalig | 24 Steinblöcke, 16 Bretter, 10 Kupferbarren, 6 Schmiedebeschläge |
| Ertrag | 2 Schmiedebeschläge je Durchlauf, garantiert |

Der Ausbau überdauert Durchläufe und bezahlte Resets. Der Ertrag lässt sich
einmal je Durchlauf abholen und sammelt sich nicht an. Die sechs Beschläge sind
Absicht: ein Eisenteil kostet zwei, man gibt für den Wall also drei
Rüstungsstücke auf, um danach verlässlich zu bekommen, was man bisher erwürfelt
hat.

### Zehn Wünsche aus dem Spieltest

- Beim Abbauen hält die Figur das echte Werkzeug und spielt den passenden
  Bewegungsablauf — mit allen neun Werkzeugen, nicht nur den Grundwerkzeugen.
- Werkzeuge kleben nicht mehr dauerhaft an der Hand.
- Der Angriffsbutton zeigt die tatsächlich getragene Waffe; ohne Zweitwaffe ist
  der Wechselbutton gesperrt statt ein Phantom-Icon anzubieten.
- Die Gebäudebilder im Baumenü liegen nicht mehr übereinander.
- Beim Öffnen einer Kiste kniet die Figur davor und arbeitet am Deckel.
- Der Technologiebaum ist scrollbar; die Knöpfe überdecken keine Karten mehr.
- Neue Hand-Glyphe für den Kontrollbutton, Versionslabel im Startbildschirm.

### Weniger Frust beim Zielen

Eine Welttruhe in der Nähe zieht das Ziel nicht mehr vom Ressourcenknoten weg.
Umgekehrt stehen Ressourcen jetzt nicht mehr in Truhen: bei der Kartenerzeugung
wird um jeden Truhenplatz Freiraum gehalten.

### Halb so groß

Der Download schrumpft von 512 auf rund 255 MiB, weil 545 MB nicht mehr
benötigter Bilddaten entfallen sind.

## Empfohlener Testpfad

1. Neues Spiel beginnen und den Sammel-, Crafting-, Inventar- und Lagerpfad in
   der Heimatbasis prüfen.
2. Basisobjekte im Raster bauen, Böden als Rechteck und Wände als Linie ziehen,
   Türen einsetzen, Objekte kostenfrei versetzen; Räume schließen und wieder
   öffnen.
3. Axt, Sense und Spitzhacke verwenden — auf die Abbau-Animation achten und
   darauf, dass danach die Waffe zurückkommt.
4. Stoff- und Kupferrüstung ausrüsten, Teile mischen, Schutz und
   Haltbarkeitsverlust kontrollieren.
5. Hammer, Dolche und Speere einsetzen; Variantenwechsel, Verschleiß, Wucht,
   Hinterhalt und das Distanzfenster der Speere ausprobieren.
6. Terrock und Noctarion fangen, Garon besiegen, Kupferspeer-Bauplan und
   Tier-2-Fortschritt freischalten.
7. Dämmerhain, Schleiermoor und Grauklüfte bereisen.
8. Die Eidra-Schmiede vollständig spielen: alle drei Wege durch die Gießhalle
   ausprobieren, das Hammerwerk mitnehmen, die Bindungskammer finden,
   Kernwächter, Reset und Persistenz testen.
9. Den Formwall ausbauen und über zwei Durchläufe prüfen, ob der Ertrag
   ankommt und der Ausbau erhalten bleibt.
10. Gegnerbeute über die durchsuchbaren Behälter übernehmen.
11. Speichern, Anwendung neu starten und alles erneut prüfen.
12. Die 3D-Figuren bewusst von allen Seiten ansehen — besonders Rückansichten
    im Kampf, den Garon-Bosskampf und Terrock im Steinbruch.
13. Nach Möglichkeit Tastatur/Maus und Gamepad sowie verschiedene Auflösungen
    testen.

## Abnahme

- vollständiger EditMode-Lauf: **1106 bestanden, 0 fehlgeschlagen, 0
  übersprungen**
- vollständiger PlayMode-Lauf: **121 bestanden, 0 fehlgeschlagen**, 3 bewusste
  Übersprünge (zwei Diagnoseläufe und ein Aufnahmelauf)
- Windows-x64-Build erstellt und mit Prüfsumme versehen
- Player-Smoke-Test bestanden

## Technische Daten

- Plattform: Windows x64
- Grafikpfad: Direct3D 11
- Buildtyp: Unity Development Build
- Unity: `6000.3.0f1`
- Download: 265.629.566 Bytes / 253,32 MiB
- entpackt: 366.842.654 Bytes / 349,85 MiB
- Dateien im Paket: 101
- SHA-256: `f84786557b505668ec361d2e2623570ca36e31ce37cdfa2085593576aadb3a0a`

Der Build ist nicht digital signiert. Eine Windows-SmartScreen-Warnung ist
daher möglich. Bitte nur das direkt aus diesem Repository geladene ZIP testen.

## Bekannte Einordnung

Dies ist ein Entwicklungs-Snapshot zur mechanischen und inhaltlichen Abnahme.
Der Teststand enthält keine Mehrspielerfunktionen. Drei Punkte sind bekannt und
noch offen:

- **Die Schmiede ist zu dunkel.** Die Beleuchtung steht, der Feinabgleich der
  Helligkeiten fehlt. Wer dort spielt, sieht weniger, als vorgesehen ist.
- **Der Formwall wechselt sein Aussehen nicht.** Bauen und Ernten
  funktionieren, aber der Bauplatz zeigt danach weiter das leere Gerüst.
- **Die Gebiete werden bei jedem Start neu ausgewürfelt.** Ressourcen und
  Truhen liegen nach einem Neustart woanders.

Rückmeldungen zu Balancing und Lesbarkeit sind ausdrücklich erwünscht.
