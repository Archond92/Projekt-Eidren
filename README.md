# Projekt Eidren – Windows-Testversion 0.3

Eidren ist ein isometrisches Action- und Aufbauspiel mit handgebauten
3D-Figuren in einer räumlichen Low-Poly-Welt. Version 0.3 stellt die gesamte
Darstellung von gemalten 2D-Figuren auf Modelle um, baut die verlassene
Eidra-Schmiede vollständig neu, ergänzt den Formwall als ausbaubare Anlage und
bringt Boden, Beleuchtung und Bodenschatten der ganzen Welt auf einen neuen
Stand.

Bei dieser Ausgabe handelt es sich um einen Entwicklungs-Snapshot zum Testen
der Mechaniken und Inhalte. Das Spiel beginnt bei null — es liegt kein
vorgefertigter Spielstand bei.

## Herunterladen und starten

1. Unter [Releases](https://github.com/Archond92/Projekt-Eidren/releases/tag/v0.3.0-dev)
   die Datei `Eidren-v0.3.0-dev-windows-x64.zip` herunterladen.
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

SHA-256 von `Eidren-v0.3.0-dev-windows-x64.zip`:

```text
f84786557b505668ec361d2e2623570ca36e31ce37cdfa2085593576aadb3a0a
```

Ausführliche Testschritte und der enthaltene Funktionsumfang stehen in den
[Release Notes](RELEASE_NOTES_v0.3.0-dev.md).

Dieses Repository enthält die spielbare Testversion, nicht den Unity-Quellcode
oder lokale Entwicklungsartefakte.
