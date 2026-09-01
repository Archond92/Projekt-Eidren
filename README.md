# Projekt Eidren – Windows-Entwicklerversion 0.3.3

Eidren ist ein isometrisches Action- und Aufbauspiel mit handgebauten
3D-Figuren. Version 0.3.3 stellt die sichtbare Spielwelt auf Mid Poly um:
Wanderer, Kreaturen, Bosse, Gebäude, Stationen, Container, Ressourcen und
Umgebung besitzen neue Modelle mit abgestuften Detailstufen.

Diese Ausgabe ist ausdrücklich ein Entwickler- und Teststand mit
Vollfreischaltung. Ein eigener, von der regulären Version getrennter
Spielstand beginnt auf Level 40 mit allen erreichbaren Technologien und
Gebieten, kompletter Eisenausrüstung, allen drei Eidra und Baumaterial.

## Herunterladen und starten

1. Unter [Releases](https://github.com/Archond92/Projekt-Eidren/releases/tag/v0.3.3)
   die Datei `Eidren-v0.3.3-windows-x64.zip` herunterladen.
2. Das ZIP vollständig in einen neuen Ordner entpacken.
3. Im entpackten Ordner `Eidren-Developer-MidPoly.exe` starten.

Es ist keine Installation und kein Unity Editor erforderlich. Die EXE muss
zusammen mit `Eidren-Developer-MidPoly_Data`, `UnityPlayer.dll` und den
übrigen Dateien aus dem ZIP im selben Ordner bleiben.

Der Build ist nicht digital signiert. Windows kann deshalb eine
SmartScreen-Warnung anzeigen. Bitte prüfen, dass der Download direkt aus diesem
GitHub-Repository stammt.

## Voraussetzungen

- Windows x64
- Direct3D-11-fähige Grafikhardware
- rund 288 MiB für den Download und 1,11 GiB entpackt

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
| Eidra-Gespann verwalten | `G` |
| Eidra-Fähigkeit 1 / 2 | `1` / `2` |
| Neustart nach Sieg oder Niederlage | Eingabetaste |

Gamepad-Eingaben werden ebenfalls unterstützt.

## Feedback und Fehlerberichte

Feedback kann unter [Issues](https://github.com/Archond92/Projekt-Eidren/issues)
eingereicht werden. Bitte Windows-Version, Eingabemethode, Schritte zum
Nachstellen und nach Möglichkeit einen Screenshot angeben.

## Prüfsumme

SHA-256 von `Eidren-v0.3.3-windows-x64.zip`:

```text
58dfc80eb491ea1c3e3d34f34fcb9ff70fb408c46a411e621304cb10751f3f49
```

Ausführliche Testschritte und der enthaltene Funktionsumfang stehen in den
[Release Notes](RELEASE_NOTES_v0.3.3.md).

Dieses Repository enthält die spielbare Testversion, nicht den Unity-Quellcode
oder lokale Entwicklungsartefakte.
