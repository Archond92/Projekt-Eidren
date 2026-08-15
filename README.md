# Projekt Eidren – Windows-Testversion 0.2

Eidren ist ein isometrisches Action- und Aufbauspiel mit handgebauten
3D-Figuren in einer räumlichen Low-Poly-Welt. Version 0.2 erweitert den
vollständigen v0.1-Spielweg um modularen Rasterbau, Tier-2-Fortschritt,
Haltbarkeit und Rüstung, neue Gebiete und Gegner, den Speer, Ignivar sowie
die verlassene Eidra-Schmiede. Der Snapshot vom 14.08. stellt zudem die
gesamte Darstellung — Spielfigur und alle 15 Kreaturen — von 2D-Sprites
auf 3D-Modelle um.

Bei dieser Ausgabe handelt es sich um einen vollständigen Entwicklungs-Snapshot
zum Testen der v0.2-Mechaniken und Inhalte.

## Herunterladen und starten

1. Unter [Releases](https://github.com/Archond92/Projekt-Eidren/releases/tag/v0.2.0-dev)
   die Datei `Eidren-v0.2.0-dev-windows-x64.zip` herunterladen.
2. Das ZIP vollständig in einen neuen Ordner entpacken.
3. Im entpackten Ordner `Eidren.exe` starten.

Es ist keine Installation und kein Unity Editor erforderlich. Die EXE muss
zusammen mit `Eidren_Data`, `UnityPlayer.dll` und den übrigen Dateien aus dem
ZIP im selben Ordner bleiben.

Der Build ist nicht digital signiert. Windows kann deshalb eine
SmartScreen-Warnung anzeigen. Bitte prüfen, dass der Download direkt aus diesem
GitHub-Repository stammt.

## Voraussetzungen

- Windows x64
- Direct3D-11-fähige Grafikhardware
- rund 260 MiB für den Download und 360 MiB entpackt

## Steuerung

| Aktion | Eingabe |
| --- | --- |
| Bewegen | `WASD` oder Pfeiltasten |
| Angreifen | Linke Maustaste oder `F` |
| Ausweichen | Leertaste |
| Interagieren / Abbauen | `R` |
| Handarbeit | `C` |
| Baumenü | `B` |
| Technologiebaum | `T` |
| Waffe wechseln | `Q` |
| Eidra wechseln | `E` |
| Eidra-Fähigkeit 1 / 2 | `1` / `2` |
| Neustart nach Sieg oder Niederlage | Eingabetaste |

Gamepad-Eingaben werden ebenfalls unterstützt.

## Feedback und Fehlerberichte

Feedback kann unter [Issues](https://github.com/Archond92/Projekt-Eidren/issues)
eingereicht werden. Bitte Windows-Version, Eingabemethode, Schritte zum
Nachstellen und nach Möglichkeit einen Screenshot angeben.

## Prüfsumme

SHA-256 von `Eidren-v0.2.0-dev-windows-x64.zip`:

```text
d00c45359747a307b3dfcdca4b2d1f36d4d883803f7bcc1f838a6acfcc7344d8
```

Ausführliche Testschritte und der enthaltene Funktionsumfang stehen in den
[Release Notes](RELEASE_NOTES_v0.2.0-dev.md).

Dieses Repository enthält die spielbare Testversion, nicht den Unity-Quellcode
oder lokale Entwicklungsartefakte.
