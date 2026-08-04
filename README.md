# Projekt Eidren – Windows-Testversion 0.2

Eidren ist ein isometrisches Action- und Aufbauspiel mit handgemalten,
achtgerichteten 2D-Figuren in einer räumlichen Low-Poly-Welt. Version 0.2
erweitert den vollständigen v0.1-Spielweg um modularen Rasterbau, Tier-2-
Fortschritt, Haltbarkeit und Rüstung, neue Gebiete und Gegner, den Speer,
Ignivar sowie die verlassene Eidra-Schmiede.

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
- mindestens 650 MiB freier Speicherplatz

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
68c7204293c25a2a4701d5cb205f4c90364adf4c34fc484d6ecb5755054f4a04
```

Ausführliche Testschritte und der enthaltene Funktionsumfang stehen in den
[Release Notes](RELEASE_NOTES_v0.2.0-dev.md).

Dieses Repository enthält die spielbare Testversion, nicht den Unity-Quellcode
oder lokale Entwicklungsartefakte.
