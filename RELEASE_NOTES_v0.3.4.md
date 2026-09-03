# Eidren v0.3.4 – Mid-Poly-Entwicklerversion, Fixrunde

Version 0.3.4 ist die Fixrunde zur Mid-Poly-Entwicklerversion 0.3.3. Sie
behebt acht Meldungen aus dem Spielen von v0.3.3: unsichtbare Truhen, kaum
erkennbare Faserpflanzen, zu grobe Büsche, die Gebäudeauswahl beim
Verschieben, den ruckelnden Speer, den gestauchten Speerangriff und den
fehlenden Abbau ohne Werkzeug.

Wie v0.3.3 enthält dieser Download den Vollausbau-Spielstand und verwendet den
Produktnamen `Eidren Developer MidPoly`. Ein vorhandener Spielstand aus
v0.3.3 wird weiterverwendet; reguläre Eidren-Spielstände bleiben unberührt.

## Behoben

- **Weltkisten, Schmiedetruhen und Bodenbeute sind wieder sichtbar.** Ihre
  letzte Detailstufe wurde bei der normalen Spielkamera ausgeblendet, derselbe
  Fehler, der in v0.3.3 nur für die baubaren Gebäude behoben war. Auch alle
  Kreaturen erhalten die niedrige Ausblendgrenze.
- **Faserpflanzen sind im Sumpf erkennbar.** Die kleinste Ressource wurde
  komplett ausgeblendet; ihre Erkennungshalme trugen zudem die abgedunkelte
  Erschöpft-Farbe.
- **Büsche und Vegetation zeigen ihre feinste Detailstufe.** Alle
  Umwelt-, Ressourcen- und Zonenobjekte sowie Gebäude, Stationen, Truhen und
  Beute leiten ihre Detailstufen jetzt aus der Bildschirmgröße bei der
  orthografischen Spielkamera ab. Vorher waren es Werte für eine perspektivische
  Kamera, die im Spiel immer die gröbste Stufe wählten.
- **Verschieben und Abreißen wählen das Gebäude vor dem Bodenfeld.** Ein
  Bodenfeld ist nur noch wählbar, wenn nichts darauf steht; steht nur ein
  belegter Boden in Reichweite, sagt das Spiel das auch.
- **Der Speer ruckelt nicht mehr beim Laufen.** Der Wanderer-Export hatte jede
  Knochenebene einen Frame versetzt gebacken; alle 28 Animationsclips wurden
  neu exportiert.
- **Der Speerangriff läuft nicht mehr als Zuckung.** Die Abspielgeschwindigkeit
  zeitgebundener Clips ist auf das 1,5-Fache begrenzt.
- **Abbau von Hand zeigt eine Abbauhaltung.** Holz ohne Axt lief vorher in der
  Ruheschleife weiter.

## Abnahme

- EditMode: **1.357/1.357 bestanden, 0 fehlgeschlagen**.
- PlayMode mit Direct3D 11: **154 bestanden, 0 fehlgeschlagen, 3 bewusst übersprungen (reine Diagnose-/Capture-Helfer)**.
- Sichtbelege mit der echten Spielkamera (Qualität Medium): Weltkiste,
  Faserpflanze, Busch, Speer beim Laufen und im Angriff.
- Windows-x64-Development-Build und Player-Smoke-Test bestanden;
  Direct3D 11 und der Vollausbau-Spielstand wurden im Player-Log bestätigt.
- ZIP-Inhalt: **208/208 Dateien** gegenüber dem geprüften Buildordner, keine
  Abweichung.

## Download und technische Daten

- Archiv: `Eidren-v0.3.4-windows-x64.zip`
- Startdatei: `Eidren-Developer-MidPoly.exe`
- Plattform: Windows x64
- Unity: `6000.3.0f1`
- Grafikpfad: Direct3D 11
- Buildtyp: Unity Development Build
- Downloadgröße: 137.353.025 Bytes / 130,99 MiB
- Entpackt: 0,45 GiB
- SHA-256:
  `65ee14816ca0452ec55d68ae8cb8d595b9365ae235bd731a627daf010f9c72e1`

Das ZIP muss vollständig entpackt werden. Die EXE darf nicht ohne den
zugehörigen Data-Ordner und die DLL-Dateien verschoben werden. Der Build ist
nicht digital signiert; Windows kann deshalb eine SmartScreen-Warnung zeigen.

## Empfohlener Testpfad

1. In Grünwald und im Sumpf nach Weltkisten suchen; sie sind jetzt auf normale
   Spielentfernung sichtbar.
2. Im Sumpf Faserpflanzen finden und abbauen, einmal mit und einmal ohne Sense.
3. Büsche und Bäume in der Heimatbasis mit v0.3.3 vergleichen.
4. Einen Boden bauen, ein Gebäude darauf setzen und dann Verschieben und
   Abreißen auslösen: Das Gebäude muss zuerst gewählt werden.
5. Mit Speer laufen und angreifen; Holz ohne Axt abbauen.

## Bekannte Einordnung

- Die Angriffsclips sind länger als die Kampffenster der Waffen; der Deckel
  zeigt Anlauf und Stoß, der Rest wird überblendet. Eine Abstimmung der Clips
  ist Animationsarbeit für eine spätere Version.
- Der Axtschlag bewegt kaum die Arme; das ist der aktuelle Stand des
  Wanderer-Neubaus.
- Automatisches Spielen, Rucksäcke und Tooltips für Waffen und Tränke sind
  weiterhin offen.
