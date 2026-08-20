# Projekt Eidren – Windows-Testversion 0.3.2

Eidren ist ein isometrisches Action- und Aufbauspiel mit handgebauten
3D-Figuren in einer räumlichen Low-Poly-Welt. Version 0.3.2 ist die zweite
Fixrunde nach dem v0.3-Test: neun gemeldete Punkte sind umgesetzt, dazu zwei
neue Bedienhilfen — Tooltips an den Fähigkeitsknöpfen und ein Fenster zum
Verwalten des Eidra-Gespanns. Der Schwerpunkt liegt auf dem Kampf: Jede
Fähigkeit und jede Waffe wirkt jetzt gegen jeden Gegner, mehrere versteckte
Sperren an Gegnertypen sind entfallen.

Bei dieser Ausgabe handelt es sich um eine Testversion zum Prüfen der
Mechaniken und Inhalte. Das Spiel beginnt bei null — es liegt kein
vorgefertigter Spielstand bei; bestehende Spielstände werden beim Laden übernommen.

## Herunterladen und starten

1. Unter [Releases](https://github.com/Archond92/Projekt-Eidren/releases/tag/v0.3.2)
   die Datei `Eidren-v0.3.2-windows-x64.zip` herunterladen.
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

SHA-256 von `Eidren-v0.3.2-windows-x64.zip`:

```text
e7ffcbe61842378ca9d4cdd3d410490dab34c8f5ae60744bf7fed759377f335d
```

Ausführliche Testschritte und der enthaltene Funktionsumfang stehen in den
[Release Notes](RELEASE_NOTES_v0.3.2.md).

Dieses Repository enthält die spielbare Testversion, nicht den Unity-Quellcode
oder lokale Entwicklungsartefakte.
