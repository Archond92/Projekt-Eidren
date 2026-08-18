# Eidren v0.3.1 – Windows-Testversion

**Paket am 18.08.2026 erneut ersetzt (fünfter Austausch):** Zwei
Verlies-Verbesserungen. Der **Kernwächter der Schmiede** trägt jetzt wie
alle Gegner Name, Lebens- und Staggerbalken samt Schutzanzeige — in
geschlossener Phase steht „SCHUTZ 35 %" über ihm, bei offenem Kern
verschwindet sie. Seine Kampfmechanik war intakt (Treffer wirken, Stagger
öffnet den Kern), aber ohne Anzeige wirkten Schläge gegen seine 3000
Lebenspunkte folgenlos. Und die **Minimap zeigt im Verlies den Aufbau**:
Wände, Säulen und Kanten erscheinen als helle Konturlinien im Sichtradius
um die Figur.

**Vierter Austausch — der entscheidende:** Der Transparenz-Shader der
Sichtlinien-Ausblendung wurde von Unity aus JEDEM bisherigen Paket
entfernt, weil ihn nichts referenzierte — die Ausblendung war deshalb in
allen ausgelieferten Fassungen wirkungslos, obwohl jeder Test im Editor
grün war. Der Shader ist jetzt fest im Paket verankert und meldet sich
beim Spielstart im Log. Damit blenden Bäume, Mauern, Verlieswände und
Überführungen erstmals im Spiel wirklich aus.

**Dritter Austausch:** Zusätzlich
neu darin: Die Sichtlinien-Ausblendung erfasst jetzt auch hängende
Verdecker — Balken, Stege und Galerien im Verlies werden durchscheinend,
statt die Figur zu verschlucken.

**Zweiter Austausch:** Nach den ersten drei
Tester-Fixes (Verlies-Eingang, Gegner-Statusleiste, Zielring) behebt
diese Fassung drei weitere Meldungen: Gegner greifen nach der
Leinen-Rückkehr wieder an (sie hingen vorher dauerhaft passiv in der
Patrouille), der Wächter erscheint wieder im Verlies (Roster gespeicherter
Läufe wird beim Laden repariert), und Verlieskisten bieten „Alles
nehmen". Wer ein früheres Paket geladen hat, lädt bitte neu — die
Prüfsumme unten gilt für die aktuelle Fassung.

Dies ist die Fixrunde nach dem breiten v0.3-Test: **alle 19 gesammelten
Rückmeldungen sind umgesetzt**, dazu kommen zwei neue Systeme — die
Tutorial-Questkette und die Minimap. Wie v0.3.0-dev beginnt diese Ausgabe
bei null: kein vorgefertigter Spielstand, der Weg von der leeren
Heimatbasis bis zur Eidra-Schmiede ist der Test. Bestehende v0.3-Spielstände
werden beim Laden automatisch übernommen (Spielstand-Version 15).

## Neu in dieser Ausgabe

### Geführter Einstieg: die Tutorial-Questkette

17 Schritte begleiten den Anfang — „Ernte 2 Faserpflanzen", „Baue eine
Werkbank", „Fertige die Axt" … bis „Fange deinen ersten Eidra". Jeder
Schritt gibt EP, zusammen 940; der geführte Weg trägt bis etwa Level 5.
Der aktuelle Schritt steht links oben im HUD. Wer schon weiter ist, bekommt
erledigte Schritte beim Laden still angerechnet — EP fließen nur für echte,
neue Abschlüsse.

### Minimap

Das Kampf-HUD trägt jetzt eine Minimap mit dem Kartenrand des Gebiets,
Markierungen für entdeckte Weltkisten und den Boss. In der Eidra-Schmiede
deckt der Kartenrand den ganzen Grundriss ab.

### Die Figur bleibt sichtbar

Steht die Figur hinter einem Baum oder einer Mauer, wird der Verdecker
weich durchscheinend und die Figur bleibt zu sehen — im Freien wie im
Verlies. Die in früheren Fassungen angelegte Ausblendung hatte in den
Gebieten schlicht nie gegriffen; das ist jetzt behoben.

### Die Welt wehrt sich

Die Gegnerbesetzung folgt jetzt der Gefahrenstufe der Weltkarte
(3 × Gefahr − 1): Grünwald 5, Steinbruch und Nebelmoor je 8, Dämmerhain und
Schleiermoor je 11, Glutruinen und Grauklüfte je 14 Gegner. Die Glutruinen
standen als Gefahr-5-Gebiet vorher mit zwei Wildlingen da. Ein Teil der
Gegner bewacht die Weltkisten — nah genug, dass niemand unbeobachtet
plündert, aber nie auf der Kiste. Der Bossbereich bleibt gegnerfrei. Ein
Weltdurchlauf bringt damit 2.400 statt 1.035 EP.

## Behoben

- **Türen funktionieren**: Sie öffnen beim Annähern, geben den Weg frei und
  schließen wieder — vorher war die Tür eine Mauer mit Türoptik.
- **Eidra-Fähigkeiten wirken gegen jeden Gegner** — vorher fanden sie
  außerhalb der zwei Bosskämpfe nie ein Ziel („KEIN GÜLTIGES ZIEL");
  Terrocks Steinhaut wirkt in jeder Zone.
- **Baumenü aufgeräumt**: scrollt statt über den Bildschirmrand zu laufen,
  Karten neu geschnitten (Icon oben, voller Name, Kosten unten), Werkbank
  und Lagerkiste zeigen erstmals ihr eigenes Gebäude.
- **Rüstung gibt Lebenspunkte**: je Teil so viele, wie es Prozent Schutz
  beiträgt (volle Stoff/Kupfer/Eisen-Rüstung +10/+20/+30).
- **3D-Kreaturen schauen im Kampf ihr Ziel an** — Modell und
  Angriffstelegraph zeigten vorher in entgegengesetzte Richtungen.
- **Stoffrüstung** zeigt in Werkbank und Inventar fertige Einzelteil-Icons.
- **Abgebaute Kupferader** ist als abgebaut erkennbar — flacher Rest wie
  beim Steinvorkommen.
- **Brot-Buff** hält 2 Minuten statt 20 Sekunden.
- **Bauvorschau** bleibt auf gelegten Böden sichtbar; höchstens zwei Äcker
  gleichzeitig.
- **Technologiebaum bereinigt**: die vier ausgemusterten Knoten sind raus,
  das Fanggerät setzt Sägewerk und Seilerei voraus und bringt den zweiten
  Eidra-Platz direkt mit (Punktrückgabe per Spielstand-Migration).

## Empfohlener Testpfad

1. Neues Spiel beginnen und der Questkette folgen — sie führt durch
   Sammeln, Werkbank, Werkzeuge, Kampf, Schmelzofen, Sägewerk, Seilerei und
   Fanggerät bis zum ersten Eidra. Prüfen: Zähler im HUD, EP je Schritt,
   Abschlussmeldung.
2. Hinter Bäume und Mauern laufen — im Grünwald, im Steinbruch und in der
   Eidra-Schmiede. Der Verdecker soll durchscheinend werden, die Figur
   sichtbar bleiben.
3. Eine Tür in die eigene Basis bauen, durchgehen, schließen lassen.
4. Die Gebiete der Reihe nach bereisen und die Besetzung prüfen — auf der
   Minimap entdeckte Kisten ansteuern: Wirken sie bewacht? Bleibt der
   Bossbereich der Glutruinen frei?
5. Terrocks Steinhaut und die Angriffs-Fähigkeiten gegen normale
   Feldgegner wirken — nicht nur gegen Bosse.
6. Rüstung an- und ablegen und die Lebenspunkte-Änderung beobachten;
   Teile brechen lassen.
7. Einen v0.3.0-Spielstand laden: Fortschritt bleibt, die Questkette
   springt auf den ersten wirklich offenen Schritt, der Technologiebaum
   zeigt 28 Karten.
8. Speichern, Anwendung neu starten und alles erneut prüfen.

## Abnahme

- vollständiger EditMode-Lauf: **1209 bestanden, 0 fehlgeschlagen**
- vollständiger PlayMode-Lauf: **134 bestanden, 0 fehlgeschlagen**, 3
  bewusste Übersprünge (Grafik- und Aufnahmeläufe)
- Windows-x64-Build erstellt und mit Prüfsumme versehen; Startbeweis des
  Transparenz-Shaders im Spielstart-Log geprüft

## Technische Daten

- Plattform: Windows x64
- Grafikpfad: Direct3D 11
- Buildtyp: Unity Development Build
- Unity: `6000.3.0f1`
- Download: 265.686.141 Bytes / 253,38 MiB
- entpackt: 366.958.968 Bytes / 349,96 MiB
- Dateien im Paket: 101
- SHA-256: `0dba912b6b3f2de45acca5f72bac9f94591fa05748621b3919f3d8e0ca681639`

Der Build ist nicht digital signiert. Eine Windows-SmartScreen-Warnung ist
daher möglich. Bitte nur das direkt aus diesem Repository geladene ZIP testen.

## Bekannte Einordnung

Dies ist eine Testversion zur mechanischen und inhaltlichen Abnahme, ohne
Mehrspielerfunktionen. Bekannt und noch offen:

- Die Außengebiete werden weiterhin pro Start neu ausgewürfelt (ungesetzte
  Weltsaat).
- Eine Testermeldung, wonach Gegner nach dem Zurücksetzen passiv bleiben,
  ließ sich bisher nicht nachstellen — der zugehörige Verdacht wurde per
  Test widerlegt; Hinweise mit Schrittfolge sind willkommen.
- Werkbank und Lagerkiste zeigen im Baumenü Engine-Renderbilder; gemalte
  Fassungen im Stil der übrigen Icons stehen noch aus.
