# Grafikaufträge für Eidren v0.2.1

**Status:** G-000 bis G-003 sind abgeschlossen; G-004 bis G-010 sind offen.

**Version:** Eidren 0.2.1

In dieser Datei werden die Aufträge zur visuellen Überarbeitung für das
Wartungsupdate v0.2.1 gesammelt. Der Aufbau entspricht der Fixsammlung: jeder
Eintrag beschreibt Problem, Ursache, Sollverhalten und Abnahme. Zusätzlich
enthält jeder Eintrag einen **Prompt**, der unverändert als Arbeitsauftrag
übergeben werden kann.

## Grundlage der Analyse

Alle Beobachtungen stammen aus den drei In-Game-Captures des Visual-Fix-Builds
`Builds/Eidren-v0.2.0-dev-visual-fix-windows-x64/VisualFixSmokeFinal/`
(Szene `Zone_Greenwood`, 1920×1080), aus den daraus extrahierten Buildassets
unter `TempReview/AssetComparisons/CurrentBuild/` sowie aus den Referenzbildern
unter `TempReview/AssetComparisons/ReferenceExtracted/`.

Die Analysen sind aus dem gebauten Spiel abgeleitet, **nicht** aus dem
Quellcode: das Arbeitsverzeichnis enthält derzeit kein funktionsfähiges
Unity-Projekt mehr. Jede Ursachenangabe unterhalb ist deshalb als begründete
Arbeitshypothese zu lesen und im wiederhergestellten Projekt zuerst zu
bestätigen. G-000 ist Voraussetzung für alle übrigen Aufträge.

## Kernbefund

Das Ausgangsmaterial ist nicht die Fehlerquelle. Porträt, Referenzfiguren und
Einzelframe-Exporte sind stilsicher und sauber. Was den Gesamteindruck bricht,
ist die Darstellung in der Welt: Boden, Licht, Schatten, Objektverankerung und
Komposition. Die Aufträge sind entsprechend nach Wirkung pro Aufwand sortiert.

## Reihenfolge und Abhängigkeiten

Die Spalte **Status** gibt den Stand des jeweiligen Auftragsabschnitts wieder.
Maßgeblich ist das Statusfeld im Abschnitt selbst; diese Tabelle ist nur die
Übersicht darüber.

| Auftrag | Inhalt | Voraussetzung | Status |
| --- | --- | --- | --- |
| G-000 | Quellprojekt wiederherstellen | — | abgeschlossen |
| G-001 | Capture- und Abnahmestrecke reparieren | G-000 | abgeschlossen |
| G-002 | Bodenmaterial und Geländedarstellung | G-001 | abgeschlossen |
| G-003 | Beleuchtung, Tonwertaufbau und Bildabstimmung | G-002 | abgeschlossen |
| G-004 | Bodenschatten und Objektverankerung | G-003 | offen |
| G-005 | Dunkler Stammkörper an Bäumen | G-001 | offen |
| G-006 | Waffenanbindung, Maßstab und Einfachbelegung | G-000 | offen |
| G-007 | Sprite-Import und Darstellungsschärfe | G-001 | offen |
| G-008 | Interaktionsring als gestaltetes Bodendecal | G-001 | offen |
| G-009 | Objektdurchdringungen und Weltdichte | G-002 | offen |
| G-010 | Perspektiveinheitlichkeit der Weltobjekte | G-002 | offen |

---

## G-000 — Quellprojekt wiederherstellen

**Status:** abgeschlossen am 5. August 2026; Nachweis aller sieben
Abnahmekriterien in [G000_ABNAHME.md](G000_ABNAHME.md), Vorbefund in
[G000_STUFE0_BEFUND.md](G000_STUFE0_BEFUND.md). Nachtrag zu den Shadern siehe
Abschnitt 4a des Abnahmeberichts.

**Bereich:** Projektstruktur, Arbeitsfähigkeit und Werkzeugkette

**Beobachtung:** Im Arbeitsverzeichnis liegt kein vollständiges Unity-Projekt
mehr. `ProjectSettings/` und `Packages/` fehlen vollständig. `Assets/` enthält
14 Dateien: fünf `InitTestScene<GUID>.unity` des Test-Runners, den
Visual-Fix-Shim sowie ein einzelnes Item-PNG. Weder Szenen, Prefabs, Materialien
noch Spielskripte sind vorhanden.

**Analyse:** Die zuletzt durchgeführten Grafikkorrekturen wurden deshalb nicht
in den Quellen vorgenommen, sondern über `Tools/Deploy-VisualFix.ps1` als
kompilierte Zusatz-DLL in den fertigen Build injiziert.
`Assets/_Game/Scripts/VisualFixes/PlayerVisualFixes.cs` sucht dort zur Laufzeit
alle 0,35 Sekunden per `FindObjectsByType` nach `PlayerMotor`,
`PlayerWeaponVisual` und `PlayerCombatController` und hängt Korrekturkomponenten
an.

Dieses Vorgehen kann Materialien, Beleuchtung, Sortierung, Sprite-Importe und
Anbindungspunkte grundsätzlich nicht erreichen. Ein Teil der in G-006
beschriebenen Waffenartefakte ist eine unmittelbare Folge davon. Die
Buildlogs belegen, dass zum Buildzeitpunkt ein vollständiges Projekt unter
`-projectPath .` vorlag; `Library/` enthält weiterhin dessen Artefaktdatenbank.

**Sollverhalten:** Es existiert wieder ein vollständiges, im Unity-Editor
öffenbares Projekt mit `ProjectSettings/`, `Packages/`, allen Szenen, Prefabs,
Materialien, Skripten und Kunst-Assets. Der Visual-Fix-Weg über injizierte DLLs
wird nach der Wiederherstellung nicht weiter ausgebaut; bestehende Korrekturen
aus `PlayerVisualFixes.cs` werden inhaltlich in die Quellen überführt und dort
regulär gepflegt.

**Abnahme:**

- Das Projekt öffnet im mitgelieferten Editor unter `.unity-editor/` ohne
  Kompilierfehler.
- `ProjectSettings/` und `Packages/` sind vorhanden und vollständig.
- Alle Spielszenen laden; `Zone_Greenwood` ist im Editor spielbar.
- Ein EditMode- und ein PlayMode-Testlauf laufen durch; das Ergebnis wird
  gegen die zuletzt dokumentierten 702 EditMode- und 113 PlayMode-Tests
  gestellt und jede Abweichung benannt.
- Ein Windows-x64-Build lässt sich aus den Quellen erzeugen.
- Die inhaltlichen Korrekturen aus `PlayerVisualFixes.cs` sind in den Quellen
  abgebildet oder ausdrücklich als überholt eingestuft.
- Der Zustand des wiederhergestellten Projekts ist gegenüber dem Stand des
  ausgelieferten v0.2-Builds dokumentiert, inklusive fehlender Inhalte.

**Prompt:**

```
Setze Auftrag G-000 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

Stelle das vollständige Unity-Quellprojekt für Eidren im Arbeitsverzeichnis
wieder her. Kläre zuerst mit mir, aus welcher Quelle wiederhergestellt wird
(Backup, anderer Rechner, externes Laufwerk oder Rekonstruktion) und schlage
nichts vor, bevor du den vorhandenen Bestand geprüft hast.

Prüfe und berichte zuerst:
- was in Library/ an Artefakten des alten Projekts noch verwertbar ist
- welche Szenen, Prefabs und Assets sich aus den Buildlogs und aus
  Builds/Eidren-v0.2.0-dev-visual-fix-windows-x64/Eidren_Data/data.unity3d
  namentlich belegen lassen
- welche Inhalte aus PlayerVisualFixes.cs in die Quellen zurückgeführt
  werden müssen

Wenn das Projekt wiederhergestellt ist, weise jedes Abnahmekriterium aus G-000
einzeln nach. Erfinde keine bestandenen Prüfungen; melde Compile- und
Testfehler mit Ausgabe. Beachte, dass ein Exit-Code 0 bei Unity-Batchläufen
Kompilierfehler verdecken kann, und prüfe die Logs zusätzlich inhaltlich.
```

---

## G-001 — Capture- und Abnahmestrecke reparieren

**Status:** abgeschlossen am 5. August 2026; Nachweis aller acht
Abnahmekriterien in [G001_ABNAHME.md](G001_ABNAHME.md)

**Bereich:** Werkzeugkette, visuelle Abnahme und Regressionsschutz

**Beobachtung:** Die vorhandene Capture-Strecke liefert nur teilweise
verwertbare Bilder. `VisualFixDaggerCapture4/daggers_south.bmp` ist ein
vollständig schwarzes Bild (1280×720). `VisualFixDaggerCapture2/` und
`VisualFixDaggerCapture3/` sind leer. Nutzbar sind allein die drei Captures
unter `VisualFixSmokeFinal/`.

Die Bilder werden zudem als unkomprimierte BMP mit je 6,2 MiB abgelegt, was
Vergleiche über mehrere Stände unnötig teuer macht.

**Analyse:** Ohne verlässliche Bildabnahme lässt sich keiner der folgenden
Aufträge sauber bewerten. Ein schwarzes Capture deutet auf eine Aufnahme vor
dem ersten gerenderten Frame, auf eine nicht aktive Kamera oder auf ein
fehlendes Ziel-RenderTexture hin. Die drei funktionierenden Captures zeigen,
dass der Grundmechanismus arbeitet, die Auslösebedingung aber nicht in allen
Fällen erfüllt ist.

**Sollverhalten:** Es existiert eine reproduzierbare Capture-Strecke, die aus
festen, benannten Kamerapositionen in einer festen Szene Bilder erzeugt. Jedes
erzeugte Bild ist nachweislich nicht leer. Die Ausgabe erfolgt als PNG. Zu
jedem Lauf gehört ein Report mit Szene, Auflösung, Kameraposition, Zeitstempel
und Buildstand. Ein Lauf kann vor und nach einer Änderung ausgeführt werden und
liefert positionsgleiche Bildpaare.

**Abnahme:**

- Jeder Capture-Lauf erzeugt für jede definierte Position genau ein PNG.
- Kein erzeugtes Bild ist einfarbig; die Prüfung erfolgt automatisiert über
  Helligkeitsstreuung und wird im Report festgehalten.
- Die Aufnahme erfolgt nachweislich nach dem ersten vollständig gerenderten
  Frame der geladenen Szene.
- Kamerapositionen sind benannt, versioniert und über Läufe hinweg identisch.
- Zwei Läufe ohne Änderung am Spiel erzeugen visuell deckungsgleiche Bilder.
- Der Report nennt Szene, Auflösung, jede Kameraposition, Zeitstempel und den
  verwendeten Buildstand.
- Die bestehenden Positionen der drei `VisualFixSmokeFinal`-Captures bleiben
  als Vergleichsbasis erhalten.
- Mindestens eine Position zeigt eine leere Bodenfläche ohne Objekte, damit
  G-002 messbar bleibt.

**Prompt:**

```
Setze Auftrag G-001 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

Repariere und verfestige die Capture-Strecke für die visuelle Abnahme.
Finde zuerst die Ursache dafür, dass VisualFixDaggerCapture4/daggers_south.bmp
vollständig schwarz ist und VisualFixDaggerCapture2 und 3 leer sind, und
benenne sie, bevor du etwas änderst.

Baue die Strecke so um, dass sie:
- aus benannten, versionierten Kamerapositionen in Zone_Greenwood aufnimmt
- erst nach dem ersten vollständig gerenderten Frame auslöst
- PNG statt BMP schreibt
- jedes Bild automatisch auf Einfarbigkeit prüft und das Ergebnis im Report
  vermerkt
- die drei bestehenden Positionen aus VisualFixSmokeFinal als Vergleichsbasis
  behält und zusätzlich eine Position auf reine Bodenfläche ohne Objekte bietet

Weise anschließend jedes Abnahmekriterium aus G-001 nach, indem du die Strecke
zweimal ohne zwischenzeitliche Änderung laufen lässt und die Ergebnisse
vergleichst.
```

---

## G-002 — Bodenmaterial und Geländedarstellung

**Status:** abgeschlossen am 6. August 2026; Nachweis in
[G002_ABNAHME.md](G002_ABNAHME.md), Entwurf in [G002_ENTWURF.md](G002_ENTWURF.md).
Acht von neun Kriterien erfüllt. Kriterium 1 ist gegen den Auftragswert erfüllt
(8,179 gegen geforderte 7,74), gegen den real gemessenen Ausgangswert von 5,486
jedoch nur mit dem 1,49-fachen statt des 3-fachen — mit Texturmitteln nicht
weiter steigerbar, ohne den gemalten Stil (Kriterium 6) zu brechen; Herleitung
in Abschnitt 1 des Abnahmeberichts. Fortsetzung unter Beleuchtung siehe G-003.

**Bereich:** Weltgrafik, Bodenmaterial und Gesamteindruck

**Beobachtung:** Der Boden füllt in allen Captures den weit überwiegenden Teil
des Bildes und trägt praktisch keine Bildinformation. Auf einer leeren
Erdfläche beträgt der mittlere Helligkeitsunterschied zwischen benachbarten
Pixeln **2,58 von 255**, die Standardabweichung liegt bei 5,3 bei einer
mittleren Helligkeit von 51,6. Die Grasfläche erreicht 8,84 beziehungsweise
11,1 bei 65,3 und ist damit zwar besser, aber ebenfalls flau.

Der Übergang zwischen Gras und Erde ist ein weicher Verlauf ohne
Materialkante. Es gibt keine Kachelstruktur, keine erkennbare Oberfläche, keine
Decals, keinen Bodenbewuchs und keine Wegespuren.

**Analyse:** Die sichtbare Struktur entspricht einer stark vergrößerten,
niedrig aufgelösten Textur ohne Detaillage. Bei der gegebenen Kameradistanz
deckt eine einzelne Texeleinheit ein Vielfaches eines Bildschirmpixels ab,
wodurch die Fläche zu einer gleichmäßigen Masse verwischt. Da diese Fläche den
größten Teil des Bildes ausmacht, bestimmt sie den Qualitätseindruck des
gesamten Spiels stärker als jedes Einzelasset.

**Sollverhalten:** Der Boden besitzt eine erkennbare, kachelnde Oberfläche mit
Detaillage und Normalinformation, die bei der tatsächlichen Spielkameradistanz
lesbar ist. Mehrere Bodenmaterialien sind mischbar und besitzen erkennbare
Übergänge. Eine großflächige Variation verhindert sichtbare Wiederholung.
Bodenbewuchs, Streu und Decals brechen leere Flächen auf. Der Stil bleibt
gemalt und fügt sich zu den vorhandenen Figuren; eine fotorealistische
Oberfläche ist ausdrücklich nicht das Ziel.

**Abnahme:**

- Auf einer leeren Bodenfläche liegt der mittlere Helligkeitsunterschied
  benachbarter Pixel bei mindestens dem Dreifachen des heutigen Wertes von
  2,58; die Messung erfolgt an derselben Kameraposition wie im Ausgangsbild.
- Aus normaler Spielkameradistanz ist eine Oberflächenstruktur erkennbar, ohne
  dass ein Kachelraster sichtbar wird.
- Mindestens drei Bodenmaterialien sind mischbar und besitzen sichtbare
  Übergänge statt reiner Farbverläufe.
- Gras und Erde treffen mit erkennbarer Materialkante aufeinander.
- Bodenbewuchs und Streu sind vorhanden und verhindern zusammenhängende leere
  Flächen in Bildgröße.
- Der gemalte Gesamtstil bleibt gewahrt; Figuren wirken vor dem Boden nicht
  wie Fremdkörper.
- Alle vier Startgebiete besitzen eine eigene, unterscheidbare Bodenwirkung.
- Die Bildrate im Windows-x64-Build sinkt gegenüber dem Ausgangsstand an
  denselben Positionen nicht messbar ab.
- Vorher-Nachher-Captures aus identischer Kameraposition liegen vor.

**Prompt:**

```
Setze Auftrag G-002 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

Überarbeite Bodenmaterial und Geländedarstellung. Das ist der Auftrag mit der
größten Wirkung auf den Gesamteindruck; behandle ihn entsprechend gründlich.

Erzeuge zuerst mit der Strecke aus G-001 ein Ausgangs-Capture der leeren
Bodenfläche und miss darauf mittlere Helligkeit, Standardabweichung und
mittleren Nachbarpixel-Delta. Halte die Werte fest.

Baue dann:
- eine kachelnde Bodentextur mit Detail- und Normallage, lesbar aus der
  tatsächlichen Spielkameradistanz
- mindestens drei mischbare Bodenmaterialien mit echten Materialübergängen
- eine großflächige Variation gegen sichtbare Wiederholung
- Bodenbewuchs, Streu und Decals gegen leere Flächen

Halte den gemalten Stil der vorhandenen Figuren; kein Fotorealismus. Orientiere
dich an TempReview/AssetComparisons/ReferenceExtracted/ für die Stillage.

Miss anschließend an derselben Position erneut und weise jedes Abnahmekriterium
aus G-002 mit Zahlen und Bildern nach. Prüfe auch die Bildrate gegen den
Ausgangsstand.
```

---

## G-003 — Beleuchtung, Tonwertaufbau und Bildabstimmung

**Status:** abgeschlossen am 6. August 2026; Nachweis aller acht Kriterien in
[G003_ABNAHME.md](G003_ABNAHME.md), Entwurf mit Messherleitung in
[G003_ENTWURF.md](G003_ENTWURF.md). Kriterium 1 ist als
Richtungsübereinstimmung nachgewiesen (die Schattenseite der Figuren ist
gemalt, nicht gerechnet — Normal-Maps praktisch flach), Kriterium 7
strukturell (HUD auf Screen Space Overlay liegt nach dem Post-Processing).
Nebenbefund: Der seit jeher unsichtbare Bodenschatten der Figuren beruhte auf
drei Rekonstruktionsfehlern und ist in den Quellen behoben — Vorgriff auf
G-004, dort nur noch Bewegung, Sprung, Rolle und Hangneigung.

**Bereich:** Szenenbeleuchtung, Nachbearbeitung und Bildwirkung

**Beobachtung:** Die Szene wirkt flach und unbeleuchtet. Boden und Grasfläche
liegen bei mittleren Helligkeiten von 51,6 und 65,3 in einem sehr engen
Tonwertband. Es gibt keine erkennbare Lichtrichtung, keine Formmodellierung an
Objekten, keine Abdunklung in Kontaktbereichen und keine Bildabstimmung.

**Analyse:** Die gemalten Figuren tragen ihr eigenes, im Bild eingebranntes
Licht. Die Szenenbeleuchtung nimmt darauf keinen Bezug, wodurch Figuren und
Welt nicht als ein Bild zusammenfinden. Da zusätzlich jede Tiefenstaffelung
über Helligkeit fehlt, liegen Vorder- und Hintergrund im selben Tonwert und das
Bild verliert Ordnung.

**Sollverhalten:** Eine klar definierte Lichtrichtung, die zur eingemalten
Lichtrichtung der Figuren passt, formt die Welt. Ein weiches Umgebungslicht
hebt Schattenbereiche an, ohne sie flach zu machen. Eine Bildabstimmung setzt
Schwarzpunkt, Weißpunkt und Farbstimmung pro Gebiet. Der Tonwertumfang des
Bildes ist deutlich größer als heute, ohne dass Figuren absaufen oder
ausbrennen.

**Abnahme:**

- Die Lichtrichtung der Szene stimmt mit der eingemalten Lichtrichtung der
  Figurensprites überein; die Prüfung erfolgt an einer Figur mit deutlicher
  Schattenseite.
- Der Tonwertumfang eines Standardbildes ist gegenüber dem Ausgangsstand
  messbar größer; Standardabweichung der Bildhelligkeit und Histogramm werden
  vorher und nachher dokumentiert.
- Figuren heben sich vor dem Boden ab, ohne dass ein Umriss nötig ist.
- Kontaktbereiche zwischen Objekten und Boden sind abgedunkelt.
- Jedes der vier Startgebiete besitzt eine eigene, benannte Farbstimmung.
- Keine Bildbereiche brennen aus oder saufen ab; Zeichnung bleibt in Lichtern
  und Tiefen erhalten.
- Die HUD-Elemente bleiben von der Bildabstimmung unbeeinflusst lesbar.
- Die Bildrate im Windows-x64-Build sinkt an denselben Positionen nicht
  messbar ab.

**Prompt:**

```
Setze Auftrag G-003 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

Baue Beleuchtung, Tonwertaufbau und Bildabstimmung auf. Voraussetzung ist ein
abgeschlossenes G-002.

Bestimme zuerst aus den Figurensprites unter
TempReview/AssetComparisons/CurrentBuild/ die eingemalte Lichtrichtung und
halte sie fest. Richte die Szenenbeleuchtung danach aus, nicht umgekehrt.

Baue dann:
- ein gerichtetes Hauptlicht passend zu dieser Lichtrichtung
- ein weiches Umgebungslicht, das Schatten anhebt ohne sie flachzudrücken
- Abdunklung in Kontaktbereichen zwischen Objekten und Boden
- eine Bildabstimmung mit Schwarzpunkt, Weißpunkt und je einer benannten
  Farbstimmung pro Startgebiet

Dokumentiere Histogramm und Helligkeitsstreuung vorher und nachher und weise
jedes Abnahmekriterium aus G-003 nach. Prüfe ausdrücklich, dass das HUD lesbar
bleibt und die Bildrate nicht abfällt.
```

---

## G-004 — Bodenschatten und Objektverankerung

**Status:** offen

**Bereich:** Figurendarstellung, Objektverankerung und räumliche Lesbarkeit

**Beobachtung:** Die Spielfigur besitzt in keinem der Captures einen
Bodenschatten. Sie steht dadurch nicht sichtbar auf dem Boden, sondern wirkt
davorgesetzt. Dasselbe gilt für Grasbüschel und kleinere Objekte.

**Analyse:** Bei gemalten 2D-Figuren in einer räumlichen Welt ist der
Bodenschatten der wesentliche Hinweis auf die Standposition. Fehlt er, kann der
Betrachter die Figur nicht eindeutig im Raum verorten, und die Trennung
zwischen Figurenebene und Weltebene wird sichtbar. Ein einfacher weicher
Schattenfleck unter jedem stehenden Objekt löst das Problem verlässlicher als
ein voll berechneter Schattenwurf und ist zudem günstiger.

**Sollverhalten:** Jedes stehende Objekt, jede Figur und jeder Gegner besitzt
einen weichen Bodenschatten an der tatsächlichen Standposition. Der Schatten
folgt Bewegung, Sprung und Ausweichrolle. Größe und Deckkraft richten sich nach
Objektgröße und Höhe über dem Boden. Der Schatten liegt auf der Bodenoberfläche
und folgt deren Neigung.

**Abnahme:**

- Spielfigur, alle Gegner, alle Eidra und alle stehenden Weltobjekte besitzen
  einen Bodenschatten.
- Der Schatten sitzt an der Standposition und nicht versetzt.
- Bei Bewegung folgt der Schatten ohne sichtbare Verzögerung.
- Während der Ausweichrolle und in Sprungphasen verkleinert sich der Schatten
  nachvollziehbar und die Figur löst sich sichtbar vom Boden.
- Auf geneigtem Untergrund liegt der Schatten auf der Oberfläche, ohne sie zu
  durchschneiden oder über ihr zu schweben.
- Die Schattenrichtung stimmt mit der Lichtrichtung aus G-003 überein.
- Auf dem überarbeiteten Boden aus G-002 bleibt der Schatten sichtbar, ohne
  als harter Fleck zu wirken.
- Die Bildrate sinkt bei voller Gegnerzahl nicht messbar ab.

**Prompt:**

```
Setze Auftrag G-004 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

Gib jeder Figur und jedem stehenden Weltobjekt einen weichen Bodenschatten, der
die Standposition eindeutig macht. Voraussetzung ist ein abgeschlossenes G-003,
damit Schattenrichtung und Lichtrichtung zusammenpassen.

Setze bewusst auf einen weichen Schattenfleck pro Objekt statt auf vollen
Schattenwurf, und begründe im Ergebnis kurz, warum die gewählte Umsetzung für
gemalte 2D-Figuren in dieser Welt trägt.

Achte besonders auf:
- Verhalten während Ausweichrolle und Sprungphasen
- geneigten Untergrund
- Sichtbarkeit auf dem überarbeiteten Boden aus G-002

Weise jedes Abnahmekriterium aus G-004 mit Bildern nach, darunter je ein
Capture bei Bewegung und eines während der Ausweichrolle. Prüfe die Bildrate
bei voller Gegnerzahl.
```

---

## G-005 — Dunkler Stammkörper an Bäumen

**Status:** offen

**Bereich:** Weltgrafik, Baumdarstellung und Platzhalter

**Beobachtung:** An jedem Baum liegt ein glatter, hart abgegrenzter,
undurchsichtiger dunkelbrauner Körper über dem gemalten Stamm. Er beginnt
innerhalb der Krone, läuft senkrecht nach unten und endet mit einer gerundeten
Kante unterhalb des Baumes auf dem Boden. Er besitzt keine Struktur, keine
Zeichnung und keinen Übergang zum umliegenden Material. Der gemalte, detailliert
ausgearbeitete Stamm ist daneben und darunter weiterhin sichtbar.

**Analyse:** Es liegen zwei Stämme übereinander. Die wahrscheinlichste Ursache
ist ein untexturiertes oder falsch materialisiertes 3D-Stammelement der
Low-Poly-Welt, das durch das gemalte Baum-Sprite hindurchstößt. Ein als
undurchsichtige Fläche gerendertes Schattenelement ist die zweite mögliche
Ursache. Welche zutrifft, ist ohne Quellprojekt nicht entscheidbar und muss
zuerst bestimmt werden.

Dieser Körper ist das auffälligste einzelne Platzhalterartefakt in allen
ausgewerteten Bildern und beschädigt jeden Screenshot, in dem ein Baum
vorkommt.

**Sollverhalten:** Pro Baum ist genau ein Stamm sichtbar. Dieser besitzt
Zeichnung und Material im Stil der übrigen Weltgrafik und geht sichtbar in
Krone und Boden über. Kein untexturiertes Element und keine undurchsichtige
Schattenfläche bleibt sichtbar.

**Abnahme:**

- Die Ursache ist eindeutig bestimmt und im Auftrag nachgetragen.
- An keinem Baum in keinem Startgebiet ist ein zweiter, strukturloser
  Stammkörper sichtbar.
- Der sichtbare Stamm besitzt Zeichnung und Material passend zur übrigen
  Weltgrafik.
- Der Übergang zwischen Stamm und Krone zeigt keine harte Kante und keine
  Durchdringung.
- Der Übergang zwischen Stamm und Boden ist sauber; der Stamm schwebt nicht
  und versinkt nicht.
- Der Baumschatten entsteht ausschließlich über den Mechanismus aus G-004.
- Die Prüfung erfolgt an mindestens fünf Bäumen aus mindestens zwei Gebieten
  und aus zwei verschiedenen Kamerawinkeln.
- Kollision, Abbaubarkeit und Ertrag der Bäume bleiben unverändert.

**Prompt:**

```
Setze Auftrag G-005 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

An jedem Baum liegt ein strukturloser dunkelbrauner Körper über dem gemalten
Stamm und läuft senkrecht bis auf den Boden durch.

Bestimme zuerst eindeutig, was dieses Element ist: ein untexturiertes
3D-Stammelement, eine undurchsichtige Schattenfläche oder etwas anderes.
Trage die tatsächliche Ursache in G-005 nach, bevor du sie behebst. Rate nicht.

Sorge dann dafür, dass pro Baum genau ein Stamm mit Zeichnung und passendem
Material sichtbar ist, der sauber in Krone und Boden übergeht. Der Baumschatten
soll ausschließlich über den Mechanismus aus G-004 entstehen.

Prüfe an mindestens fünf Bäumen aus zwei Gebieten und aus zwei Kamerawinkeln
und weise jedes Abnahmekriterium aus G-005 mit Bildern nach. Bestätige, dass
Kollision, Abbaubarkeit und Ertrag unverändert sind.
```

---

## G-006 — Waffenanbindung, Maßstab und Einfachbelegung

**Status:** offen

**Bereich:** Spielerdarstellung, Waffenvisualisierung und Sortierung

**Beobachtung:** Im Speer-Capture kreuzt der Speer den Oberkörper auf
Hüfthöhe und liegt vor dem Körper. Es gibt keinen sichtbaren Handkontakt; die
Waffe wirkt aufgelegt statt gehalten.

Im Axt-Capture ist das Axtblatt größer als der Oberkörper der Figur und verdeckt
diese fast vollständig. Im selben Bild ist zusätzlich ein Speer sichtbar, der
diagonal durch die Figur verläuft. Es werden also zwei Waffen gleichzeitig
dargestellt.

Die Waffen sind zudem deutlich metallisch-realistischer ausgearbeitet als die
Figuren und fallen dadurch stilistisch heraus.

**Analyse:** Die Waffe wird als eigenes Sprite ohne verlässlichen
Anbindungspunkt pro Richtung und Animationsphase über die Figur gelegt. Ein
Ausschalten der jeweils nicht aktiven Waffe findet nicht zuverlässig statt. Der
Maßstab ist nicht an die Figurenhöhe gebunden.

Die Laufzeitkorrektur in `PlayerVisualFixes.cs` hängt über `DualDaggerHands`
zusätzliche Komponenten an gefundene `PlayerWeaponVisual`-Instanzen. Eine
solche nachträgliche Korrektur an einem fertigen Build kann Anbindungspunkte
und Sortierung nicht verlässlich herstellen und ist als Ursachenverstärker
einzustufen. Die Behebung gehört in die Quellen.

**Sollverhalten:** Jede Waffe besitzt pro Blickrichtung und pro Animationsphase
einen definierten Anbindungspunkt an der Hand. Die Waffe folgt diesem Punkt und
wird relativ zur Figur korrekt vor oder hinter den Körperteilen einsortiert. Es
ist immer genau eine Waffe sichtbar. Der Maßstab ist an die Figurenhöhe
gebunden und gedeckelt. Die Waffengrafiken werden stilistisch an die Figuren
angeglichen.

**Abnahme:**

- In jeder der acht Blickrichtungen sitzt jede der elf Waffenvarianten
  sichtbar in der Hand.
- Keine Waffe kreuzt den Oberkörper ohne Handkontakt.
- Die Sortierung stimmt in allen acht Richtungen: die Waffe liegt vor oder
  hinter der Figur, wie es die Haltung erfordert.
- Zu jedem Zeitpunkt ist genau eine Waffe sichtbar; ein Waffenwechsel über
  `Q` hinterlässt keine zweite sichtbare Waffe.
- Keine Waffe überschreitet den festgelegten Maßstabsdeckel im Verhältnis zur
  Figurenhöhe.
- Angriffs-, Abbau- und Leerlaufphasen zeigen die Waffe durchgehend korrekt
  angebunden.
- Die Waffengrafiken wirken stilistisch als Teil derselben Welt wie die
  Figuren.
- Die entsprechenden Korrekturen aus `PlayerVisualFixes.cs` sind in den
  Quellen abgebildet und der Laufzeitpatch für diesen Bereich entfällt.
- Sichtbare Rüstungsteile bleiben mit der überarbeiteten Anbindung korrekt
  dargestellt.

**Prompt:**

```
Setze Auftrag G-006 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

Bring die Waffendarstellung des Spielers in Ordnung. Aktuell kreuzt der Speer
den Oberkörper ohne Handkontakt, das Axtblatt ist größer als der Torso, und in
einem Bild werden Axt und Speer gleichzeitig gerendert.

Arbeite ausschließlich in den Quellen, nicht über den Laufzeitpatch in
PlayerVisualFixes.cs. Führe die dortigen Korrekturen für diesen Bereich in die
Quellen zurück und entferne anschließend den Laufzeitweg dafür.

Setze um:
- definierte Anbindungspunkte an der Hand pro Blickrichtung und
  Animationsphase, für alle elf Waffenvarianten
- korrekte Sortierung vor und hinter der Figur je nach Haltung
- garantiert genau eine sichtbare Waffe, auch über den Waffenwechsel mit Q
- einen an die Figurenhöhe gebundenen Maßstabsdeckel
- stilistische Angleichung der Waffengrafiken an die Figuren

Prüfe systematisch alle acht Blickrichtungen für alle elf Varianten und weise
jedes Abnahmekriterium aus G-006 nach. Bestätige, dass sichtbare Rüstungsteile
weiterhin korrekt dargestellt werden.
```

---

## G-007 — Sprite-Import und Darstellungsschärfe

**Status:** offen

**Bereich:** Texturimport, Filterung und Bildschärfe

**Beobachtung:** Die Figuren wirken im Spiel weich und leicht verwaschen. Das
ist nicht durch die Auflösung des Ausgangsmaterials begründet: der Atlas
`player_wanderer_idle_albedo.png` misst 614×4096 bei vier Spalten, ein
Einzelframe also rund 153×205 Pixel. Im Bild wird die Figur mit rund 97×175
Pixeln dargestellt und damit leicht verkleinert, nicht vergrößert.

An den Silhouettenkanten liegt zudem ein ein bis zwei Pixel breiter, sehr
dunkler Saum. Auf Zeile y=520 fällt die Helligkeit unmittelbar an der Kante von
etwa 48 auf 14, bevor die hellen Figurenwerte von über 200 beginnen.

**Analyse:** Die Weichheit entsteht in der Darstellung, nicht im Material.
Wahrscheinlichste Ursachen sind die Auswahl einer niedrigeren Mip-Stufe unter
perspektivischer Kamera sowie Texturkompression auf den Figurenatlanten. Der
dunkle Saum deutet auf fehlende Alpha-Ausdehnung in den Atlanten hin: bei
Filterung werden transparente, dunkle Randpixel in die sichtbare Kante
gemischt. Beides ist über Importeinstellungen und Atlasaufbereitung zu lösen
und erfordert keine neue Kunst.

**Sollverhalten:** Figuren, Gegner und Weltobjekte werden bei normaler
Spielkameradistanz scharf dargestellt. An Silhouettenkanten entsteht kein
dunkler Saum. Die Kompressionsqualität der Figurenatlanten ist so gewählt, dass
keine sichtbaren Blockartefakte auftreten.

**Abnahme:**

- Die Ursache der Weichheit ist eindeutig bestimmt und benannt, bevor
  Einstellungen geändert werden.
- Figuren sind bei normaler Spielkameradistanz sichtbar schärfer als im
  Ausgangsstand; der Vergleich erfolgt an positionsgleichen Captures.
- An den Silhouettenkanten liegt kein dunkler Saum mehr; die Messung erfolgt
  an derselben Bildzeile wie im Ausgangsbefund.
- Auf den Figurenatlanten ist eine Alpha-Ausdehnung angewandt.
- Es treten keine sichtbaren Kompressionsartefakte auf.
- Beim Herauszoomen entsteht kein Flimmern an Figurenkanten.
- Der Speicherbedarf der Texturen im Build ist dokumentiert und gegenüber dem
  Ausgangsstand bewertet.
- Die Bildrate sinkt nicht messbar ab.

**Prompt:**

```
Setze Auftrag G-007 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

Die Figuren werden im Spiel weich dargestellt, obwohl das Ausgangsmaterial
ausreicht: ein Einzelframe misst rund 153x205 Pixel und wird mit rund 97x175
Pixeln dargestellt. Zusätzlich liegt an den Silhouettenkanten ein ein bis zwei
Pixel breiter dunkler Saum.

Bestimme zuerst die tatsächliche Ursache der Weichheit, bevor du
Importeinstellungen änderst. Meine Arbeitshypothese ist Mip-Auswahl unter
perspektivischer Kamera plus Texturkompression, und für den Saum eine fehlende
Alpha-Ausdehnung in den Atlanten. Bestätige oder widerlege das.

Setze dann die passenden Import- und Atlaseinstellungen und weise jedes
Abnahmekriterium aus G-007 nach. Miss den Saum an derselben Bildzeile wie im
Ausgangsbefund. Prüfe ausdrücklich auf Flimmern beim Herauszoomen und
dokumentiere die Auswirkung auf den Texturspeicher im Build.
```

---

## G-008 — Interaktionsring als gestaltetes Bodendecal

**Status:** offen

**Bereich:** Interaktionsrückmeldung, Weltdarstellung und Lesbarkeit

**Beobachtung:** Der Interaktionsring um aufnehmbare und abbaubare Objekte ist
eine grelle, reingrüne, hart abgegrenzte Linie ohne Übergang zum Boden. Er
liegt vor allen anderen Bildelementen und wirkt wie ein Entwicklerhilfsmittel,
nicht wie ein gestaltetes Spielelement. Die Ringgeometrie selbst ist in Ordnung:
sie sitzt an der Objektbasis und ist perspektivisch korrekt abgeflacht.

**Analyse:** Es fehlt nicht die Funktion, sondern die Gestaltung. Ein grelles
Reingrün ohne Materialbezug ist in einer gemalten Welt ein Fremdkörper.

**Sollverhalten:** Der Interaktionsring wird als auf den Boden projiziertes
Decal dargestellt, das der Bodenoberfläche und deren Neigung folgt. Er besitzt
eine weiche Kante und eine zur Weltgestaltung passende Farbe. Er bleibt aus
normaler Spielkameradistanz eindeutig als Interaktionshinweis erkennbar und
lenkt nicht vom Objekt ab.

**Abnahme:**

- Der Ring liegt sichtbar auf dem Boden und folgt dessen Neigung.
- Die Kante ist weich; es entsteht keine harte Linie.
- Die Farbe ist zur Weltgestaltung abgestimmt und kein Reingrün.
- Der Ring bleibt aus normaler Spielkameradistanz eindeutig als
  Interaktionshinweis lesbar.
- Der Ring verdeckt die Spielfigur nicht und wird nicht über sie gelegt.
- Auf allen Bodenmaterialien aus G-002 bleibt der Ring sichtbar.
- Unterscheidbare Interaktionsarten bleiben unterscheidbar, sofern sie es
  heute sind.
- Reichweite, Auslösung und Interaktionslogik bleiben unverändert.

**Prompt:**

```
Setze Auftrag G-008 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

Der Interaktionsring ist funktional korrekt platziert, aber grafisch ein
Fremdkörper: grelles Reingrün, harte Linie, kein Bezug zum Boden. Er liest sich
als Entwicklerhilfsmittel.

Ersetze ihn durch ein auf den Boden projiziertes Decal mit weicher Kante und
einer zur Weltgestaltung passenden Farbe, das der Bodenneigung folgt. Ändere
Reichweite, Auslösung und Interaktionslogik nicht.

Prüfe die Lesbarkeit auf allen Bodenmaterialien aus G-002 und weise jedes
Abnahmekriterium aus G-008 mit Bildern nach.
```

---

## G-009 — Objektdurchdringungen und Weltdichte

**Status:** offen

**Bereich:** Gebietsaufbau, Objektplatzierung und Bildkomposition

**Beobachtung:** In den Bäumen stecken sichtbar Fremdobjekte: ein Holzstapel,
ein oranges Tuch und ein Axtblatt sind in die Krone hineinkomponiert. Sie sind
weder verdeckt noch sinnvoll platziert.

Die Weltdichte ist zugleich sehr gering. Ein Bild in 1920×1080 zeigt vier Bäume
und fünf Grasbüschel; der übrige Bildraum ist leer.

**Analyse:** Beides ist Platzierung, nicht Kunst. Die Fremdobjekte sind
vermutlich Reste einer nicht abgeschlossenen Objektplatzierung oder falsch
verankerte Weltobjekte. Die geringe Dichte lässt zusätzlich die Schwäche des
Bodens aus G-002 voll durchschlagen: je weniger im Bild steht, desto stärker
wirkt die leere Fläche.

**Sollverhalten:** Keine sichtbare Objektdurchdringung. Die Gebiete besitzen
eine Bepflanzungs- und Objektdichte, die aus normaler Spielkameradistanz ein
gefülltes, gestaltetes Bild ergibt, ohne Bewegung, Kampf oder Bauraster zu
behindern.

**Abnahme:**

- In keinem Startgebiet steckt ein Objekt sichtbar in einem anderen.
- Die Fremdobjekte in den Baumkronen sind entfernt oder sinnvoll als eigene
  Weltobjekte platziert.
- Aus normaler Spielkameradistanz enthält kein Bildausschnitt eine
  zusammenhängende leere Fläche in Bildgröße.
- Bewegung, Kampf, Abbau und Bauraster bleiben unbehindert; die begehbare
  Fläche verringert sich nicht.
- Die vier Startgebiete bleiben visuell unterscheidbar und gewinnen jeweils
  eigene Merkmale.
- Die Bildrate sinkt bei erhöhter Objektzahl nicht messbar ab.
- Die Prüfung erfolgt aus mindestens vier Kamerapositionen pro Gebiet.

**Prompt:**

```
Setze Auftrag G-009 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

Zwei Themen: erstens stecken in den Baumkronen Fremdobjekte (Holzstapel,
oranges Tuch, Axtblatt). Zweitens ist die Weltdichte zu gering; ein 1080p-Bild
zeigt vier Bäume und fünf Grasbüschel auf sonst leerer Fläche.

Räume die Durchdringungen auf und erhöhe die Objekt- und Bepflanzungsdichte so,
dass aus normaler Spielkameradistanz ein gefülltes, gestaltetes Bild entsteht.
Behindere dabei Bewegung, Kampf, Abbau und Bauraster nicht und verkleinere die
begehbare Fläche nicht.

Gib den vier Startgebieten dabei jeweils eigene Merkmale. Prüfe aus mindestens
vier Kamerapositionen pro Gebiet, weise jedes Abnahmekriterium aus G-009 nach
und kontrolliere die Bildrate bei der erhöhten Objektzahl.
```

---

## G-010 — Perspektiveinheitlichkeit der Weltobjekte

**Status:** offen

**Bereich:** Weltgrafik, Bildperspektive und Stileinheit

**Beobachtung:** In einem einzelnen Bild treffen drei Perspektiven aufeinander.
Die Baumkrone ist von oben gemalt, der Stamm von der Seite, die Figuren stehen
in einer Dreiviertel-Isometrie. Die Objekte lesen sich dadurch nicht als Teil
desselben Raums.

**Analyse:** Die Einzelassets sind für sich gut ausgearbeitet, folgen aber
keiner gemeinsam festgelegten Blickhöhe. Ohne verbindliche Perspektivvorgabe
setzt sich der Fehler bei jedem neuen Asset fort. Der Auftrag ist deshalb
weniger eine Korrektur als die Festlegung einer Regel plus Nacharbeit der
Abweichler.

**Sollverhalten:** Für alle Weltobjekte gilt eine verbindliche, dokumentierte
Blickhöhe und Perspektive, die zur tatsächlichen Spielkamera und zur
Figurenperspektive passt. Bestehende Objekte, die davon abweichen, werden
nachgearbeitet. Die Vorgabe ist so festgehalten, dass neue Assets ihr ohne
Rückfrage folgen können.

**Abnahme:**

- Eine verbindliche Perspektivvorgabe für Weltobjekte ist schriftlich
  festgehalten und nennt Blickhöhe und Winkel.
- Die Vorgabe stimmt mit der tatsächlichen Spielkamera und mit der
  Figurenperspektive überein.
- Bäume zeigen Krone und Stamm in derselben Perspektive.
- Alle Weltobjekte der vier Startgebiete sind gegen die Vorgabe geprüft;
  Abweichler sind benannt und nachgearbeitet.
- In keinem Bildausschnitt treffen erkennbar zwei verschiedene Blickhöhen bei
  benachbarten Objekten aufeinander.
- Die Prüfung ist an mindestens einem Beispiel pro Objektart dokumentiert.
- Der gemalte Gesamtstil bleibt erhalten.

**Prompt:**

```
Setze Auftrag G-010 aus Documentation/Auftraege/GRAFIKAUFTRAEGE_V0.2.1.md um.

In einem Bild treffen drei Perspektiven aufeinander: Baumkrone von oben, Stamm
von der Seite, Figuren in Dreiviertel-Isometrie.

Lege zuerst eine verbindliche Perspektivvorgabe für alle Weltobjekte fest, die
zur tatsächlichen Spielkamera und zur Figurenperspektive passt, und halte sie
schriftlich mit Blickhöhe und Winkel fest. Erst danach arbeite die Abweichler
nach, beginnend bei den Bäumen.

Prüfe alle Weltobjekte der vier Startgebiete gegen die Vorgabe, benenne jeden
Abweichler und weise jedes Abnahmekriterium aus G-010 nach. Der gemalte
Gesamtstil muss erhalten bleiben.
```
