# Projekt Eidren – Windows-Testversion 0.3.1

Eidren ist ein isometrisches Action- und Aufbauspiel mit handgebauten
3D-Figuren in einer räumlichen Low-Poly-Welt. Version 0.3.1 ist die Fixrunde
nach dem breiten v0.3-Test: alle 19 gesammelten Rückmeldungen sind umgesetzt —
darunter die Sichtbarkeit der Figur hinter Bäumen und Mauern, funktionierende
Türen und die an der Gefahrenstufe ausgerichtete Gegnerbesetzung. Neu dazu
kommen die Tutorial-Questkette (17 geführte Schritte bis zum ersten Eidra)
und die Minimap im Kampf-HUD.

Bei dieser Ausgabe handelt es sich um eine Testversion zum Prüfen der
Mechaniken und Inhalte. Das Spiel beginnt bei null — es liegt kein
vorgefertigter Spielstand bei; v0.3-Spielstände werden beim Laden übernommen.

## Herunterladen und starten

1. Unter [Releases](https://github.com/Archond92/Projekt-Eidren/releases/tag/v0.3.1)
   die Datei `Eidren-v0.3.1-windows-x64.zip` herunterladen.
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

SHA-256 von `Eidren-v0.3.1-windows-x64.zip`:

```text
41826941c39a9abcceddd763170941aa1cb7b88a5c4d45b04b98555c1af66fc2
```

Ausführliche Testschritte und der enthaltene Funktionsumfang stehen in den
[Release Notes](RELEASE_NOTES_v0.3.1.md).

Dieses Repository enthält die spielbare Testversion, nicht den Unity-Quellcode
oder lokale Entwicklungsartefakte.
