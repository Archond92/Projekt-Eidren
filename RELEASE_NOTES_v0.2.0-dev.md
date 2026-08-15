# Eidren v0.2.0-dev – Windows-Testversion

Dieser Entwicklungs-Snapshot enthält den vollständigen bisherigen Spielweg und
die integrierten Mechaniken, Inhalte und Laufzeit-Assets von Version 0.2. Er ist
für einen breiten Spieltest vorgesehen und noch nicht als finale Version 0.2.0
klassifiziert.

## Neu in diesem Snapshot: vollständiger 3D-Umstieg

Der Snapshot vom 14.08.2026 ersetzt die bisherige 2D-Sprite-Darstellung durch
echte 3D-Figuren im Fabrik-Pipeline-Stil:

- der Wanderer (Spielfigur) läuft als 3D-Modell mit 24 Animationszuständen,
  sichtbaren Werkzeugen und Waffen
- alle 15 Kreaturen (Wildling, Riftling, Garon, Terrock, Noctarion, Ignivar,
  EmberEater, RootCharger, GraniteShell, AshRunner und weitere) sind als
  handgebaute 3D-Modelle mit vollem Animationssatz eingebaut; 27 Szenengegner
  in 7 Zonen wurden umgestellt
- Sichtpolitur: Garon trägt ein Heck-Goldornament, der EmberEater eine
  durchgehende Glut-Rückenader, Terrock Moosflächen und einen Kruppen-Smaragd
  für bessere Lesbarkeit im Steinbruch
- die 2D-Altlasten (545 MB Sprite-Atlanten) wurden beweisgetrieben entfernt;
  Platzhalter-Item-Icons der Familien (Kupfer/Eisen, Holz/Hartholz,
  Stein/Granit) sind jetzt farblich unterscheidbar
- Farm-Beete zeigen ihre drei Zustände (leer/bepflanzt/erntereif) wieder mit
  eigenen Visuals

## Testfassung mit Voll-Ausbau-Spielstand (15.08.2026)

Dieses Paket bringt einen fertigen Testspielstand mit, damit alle Inhalte
sofort erreichbar sind — ohne vorher hochzuspielen:

- **Stufe 2, Level 40** (das Maximum der höchsten Fortschrittsstufe)
- **29 von 33 Technologien** freigeschaltet — alle, die im Spiel erreichbar
  sind. Vier Knoten (T1-Waffen, Stoffmantel, Stoffarmschienen, Stoffschuhe)
  sind dauerhaft stillgelegt und erscheinen im Baum als „FORTSCHRITTSFLAG
  FEHLT"; sie sind auch regulär nicht erreichbar.
- **alle acht Gebiete offen**, inklusive der drei Tier-2-Gebiete
  (Dämmerhain, Schleiermoor, Grauklüfte) und der Eidra-Schmiede
- **komplette Eisenausrüstung angelegt**: Eisenhelm, -harnisch,
  -handschuhe, -beinschutz sowie Eisenhammer und Eisendolche in beiden
  Waffenplätzen
- **alle Eisenwaffen und -werkzeuge**: der Eisenspeer liegt im Rucksack
  (Waffenplätze fassen zwei Waffen — im Inventar tauschbar), dazu Eisenaxt,
  Eisenspitzhacke und Eisensense
- **alle drei Eidra** im Team (Terrock, Noctarion, Ignivar; zwei aktiv)
- Baumaterial-Vorräte (Holz, Stein, Fasern, Eisen- und Kupferbarren)

**So kommst du hinein:**

- Ohne vorhandenen Spielstand wird er beim ersten Start automatisch
  angelegt — einfach **FORTSETZEN** wählen.
- Mit vorhandenem Spielstand startet **NEUES SPIEL** in dieser Testfassung
  direkt im Voll-Ausbau-Stand. Dein bisheriger Spielstand wird dabei nicht
  angetastet und bleibt über FORTSETZEN erreichbar, bis du im neuen Spiel
  speicherst.

## Nachtrag 15.08.2026: Wunschrunden-Fixes im neu gebauten Paket

Das Paket wurde am 15.08.2026 neu gebaut und enthält die sechs Fixes der
Wunschsammlung vom 14.08. (`Documentation/Eidren_V0.2/WUNSCHSAMMLUNG_20260814.md`):

- **Abbau-Animation (W-001):** Beim Abbauen hält die Spielfigur jetzt das
  echte Werkzeug-Mesh (Axt, Spitzhacke oder Sense) und spielt den
  zugehörigen Abbau-Clip aus dem 3D-Modell; das alte 2D-Sprite-Pendel ist
  entfernt. Danach kehrt die ausgerüstete Waffe sichtbar zurück.
- **Werkzeug-Geister (W-003):** Die drei Werkzeuge hängen nicht mehr
  dauerhaft an der Hand der Figur (vorher ragte z. B. die Sense trotz
  ausgerüstetem Eisenspeer aus der Figur).
- **Waffen-Icons im Kampf-HUD (W-004):** Der Angriffsbutton zeigt das Icon
  der tatsächlichen Waffenfamilie (inkl. neuem Speer-Icon); der
  Waffenwechselbutton zeigt die real getragene Zweitwaffe und ist ohne
  Zweitwaffe gesperrt statt ein Phantom-Icon anzubieten.
- **Kontrollbutton (W-002):** Neue Hand-Glyphe im Stil der
  Werkzeug-Icons ersetzt die alte Fingerhand.
- **Startbildschirm (W-006):** Untertitel zeigt jetzt „V0.2 · TESTVERSION"
  statt „V0.1 · Aufbruch".
- Nebenreparatur: Das Schlag-Feedback am Kupfer (Funken/Wackeln) war durch
  einen Asset-Neubau erneut verloren gegangen und ist wiederhergestellt.

## Zweite Fixrunde (15.08.2026, nach dem ersten Spieltest)

- **Baumenü (W-007):** Die Gebäudebilder lagen übereinander — alle
  Katalogkarten waren null Pixel breit, weil die Layoutgruppe die
  vorgegebene Kartenbreite ignorierte. Die Karten stehen jetzt
  nebeneinander, mit Rahmen, Namen und Kosten.
- **Abbau mit Tier-2-Werkzeug (W-008):** Holzfällen und Abbauen zeigten
  mit Kupfer- und Eisenwerkzeug gar keine Animation — die Zuordnung kannte
  nur die Grundwerkzeuge. Jetzt spielen alle neun Werkzeuge (Grund, Kupfer,
  Eisen) ihren Abbau-Clip.
- **Kistenöffnung (W-009):** Die Figur kniet jetzt vor der Kiste und
  arbeitet mit den Händen am Deckel — ein neuer Animationsclip im
  3D-Modell. Die alten aufgesetzten 2D-Handsymbole sind entfernt.
- **Technologiebaum (W-010):** Der Baum ist scrollbar (Mausrad und Ziehen);
  Bedienhinweis und die Knöpfe FREISCHALTEN/SCHLIESSEN sitzen in einer
  festen Fußleiste und überdecken keine Karten mehr. Die Auswahl scrollt
  bei Tastatur- und Gamepad-Navigation automatisch mit.
- **Kupferader (W-005, zurückgenommen):** Nach dem Spieltest gilt wieder
  der sichtbare Wechsel zur abgebauten Darstellung.

Abnahme des Neubaus: vollständiger EditMode-Lauf **1064 bestanden, 0
fehlgeschlagen**; vollständiger PlayMode-Lauf **119 bestanden, 0
fehlgeschlagen** (4 erwartete Headless-Übersprünge der Capture- und
Grafiktests).

## Empfohlener Testpfad

1. Ein neues Spiel beginnen und den grundlegenden Sammel-, Crafting-, Inventar-
   und Lagerpfad in der Heimatbasis prüfen.
2. Basisobjekte im Raster bauen, Böden als Rechteck und Wände als Linie ziehen,
   Türen einsetzen sowie Objekte kostenfrei versetzen.
3. Räume schließen und wieder öffnen; Dachableitung, Wandverbindungen,
   Kollisionshinweise und Kamerafreistellung kontrollieren.
4. Axt, Sense und Spitzhacke verwenden und Werkzeugertrag, Belastungsanzeige,
   Haltbarkeit und Bruch testen.
5. Stoff- und Kupferrüstung ausrüsten, Rüstungsteile mischen und Schutz sowie
   Haltbarkeitsverlust kontrollieren.
6. Hammer, Dolche und Speere einsetzen; Variantenwechsel, Waffenverschleiß,
   Wucht, Hinterhalt und das Distanzfenster der Speere ausprobieren.
7. Terrock und Noctarion fangen und einsetzen, Garon besiegen und den
   Kupferspeer-Bauplan sowie den Tier-2-Fortschritt freischalten.
8. Dämmerhain, Schleiermoor und Grauklüfte bereisen; neue Ressourcen,
   Weltkisten, Tier-2-Gegner und Gebietsdarstellung prüfen.
9. Die verlassene Eidra-Schmiede vollständig spielen: Laufkisten,
   Schmiedemarken, Gegnergruppen, Kernwächter, Reset und Persistenz testen.
10. Ignivar erhalten und Glutkreis sowie Schmelzbrand im Kampf verwenden.
11. Gegnerbeute über durchsuchbare Leichenbehälter einzeln und vollständig
    übernehmen; Lagertransfers und Zustandswerte kontrollieren.
12. Speichern, Anwendung neu starten und Basis, Inventar, Ausrüstung,
    Fortschritt, Freischaltungen und persistente Kisten erneut prüfen.
13. Nach Möglichkeit Tastatur/Maus und Gamepad sowie verschiedene Auflösungen
    testen.
14. Neu für diesen Snapshot: die 3D-Figuren bewusst von allen Seiten
    ansehen — Spielfigur mit Werkzeug/Waffe in der Hand, Gegner im Kampf
    (besonders Rückansichten), Garon-Bosskampf, Terrock im Steinbruch
    (Lesbarkeit vor Steinboden) und die Farm-Beete in allen drei Zuständen.

## Wichtigste Erweiterungen gegenüber v0.1

- kachelbasierter modularer Basisbau mit Böden, Wänden, Türen, Räumen und Dächern
- drei neue Tier-2-Gebiete und die verlassene Eidra-Schmiede
- Ignivar als drittes Eidra mit zwei aktiven Fähigkeiten
- Speer als dritte Waffengattung und insgesamt elf feste Waffenvarianten
- aktive Haltbarkeit für Waffen, Werkzeuge und Rüstung
- direkter Rüstungsschutz und frei kombinierbare sichtbare Rüstungsteile
- 57 Gegenstände, 41 Rezepte und 33 Technologieknoten im v0.2-Katalog
- Tier-2-Ressourcen, -Werkzeuge, -Rüstung, -Gegner und -Verarbeitung
- persistente Weltkisten und gebündelte Gegnerbeute in Leichenbehältern
- überarbeitete Erfahrungskurve und Fortschritt bis Level 40
- vollständig integrierte Spieltest-Fixsammlung F-001 bis F-023

## Abnahme des Snapshots (14.08.2026)

- vollständiger EditMode-Lauf: **1009 bestanden, 0 fehlgeschlagen, 0 übersprungen**
- vollständiger PlayMode-Lauf: **120 bestanden, 0 fehlgeschlagen**
- damit ist die Testlandschaft erstmals komplett grün (zuvor 61 bekannte
  Altfälle; Herleitung in `Documentation/Eidren_V0.2/KREATUREN_EINBAU_REFERENZ.md`)
- Windows-x64-Build erfolgreich erstellt
- Player-Smoke-Test bestanden: 20 Sekunden Laufzeit, Direct3D-11-Start,
  Bootstrap und Hauptmenü geladen (4363 Objekte), null Fehler im Player-Log
- Build-Stand: Commit `e5966ec` (release: refresh v0.2.0-dev tester
  package for the 3D snapshot), Arbeitsbaum der verfolgten Dateien sauber;
  Assets und Dokumente dieses Projekts sind grundsätzlich nicht
  git-verfolgt

## Technische Daten

- Plattform: Windows x64
- Grafikpfad: Direct3D 11
- Buildtyp: Unity Development Build
- Unity: `6000.3.0f1`
- Download: 267.220.148 Bytes / 254,84 MiB (halbiert gegenüber dem
  06.08.-Snapshot — die 2D-Altlast-Räumung wirkt auch im Paket)
- entpackt: 368.660.568 Bytes / 351,58 MiB
- Dateien im Paket: 101
- SHA-256: `3ecb1bf30d7eba2408a43d54dd45c0473017f8faebc7c20cf591ce5741ce5b14`

Der Build ist nicht digital signiert. Eine Windows-SmartScreen-Warnung ist
daher möglich. Bitte nur das direkt aus diesem Repository geladene ZIP testen.

## Bekannte Einordnung

Dies ist ein Entwicklungs-Snapshot zur mechanischen und inhaltlichen Abnahme.
Balancing, visuelles Feintuning und einzelne Grafikintegrationstests können bis
zur finalen Version 0.2.0 noch angepasst werden. Der Teststand enthält keine
Mehrspielerfunktionen.
