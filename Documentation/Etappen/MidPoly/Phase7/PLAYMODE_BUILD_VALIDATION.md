# Mid-Poly PlayMode- und Build-Validierung

**Stand:** 25.08.2026  
**Status:** PASS

## PlayMode

- Plattform: Unity PlayMode, Unity 6000.3.0f1
- Vollständiger Lauf: 151 Tests
- Bestanden: 148
- Fehlgeschlagen: 0
- Übersprungen: 3
- Dauer: 133,37 Sekunden

Bewusst übersprungen:

1. `ItemAufnahmeDiagnoseTests.Aufnahmepfad_EchtesTiming` – Diagnose, loggt statt zu prüfen;
2. `ItemAufnahmeDiagnoseTests.Aufnahmepfad_Stationen` – Diagnose, loggt statt zu prüfen;
3. `KreaturenInWeltCaptureTests.KreaturenInWelt_Captures` – manueller Capture-Lauf, schreibt PNGs und prüft keine Regel.

Vor dem Volllauf wurden die vier korrigierten Tests gezielt ausgeführt: 4 von 4 bestanden in 6,52 Sekunden.

Die Terrock- und Noctarion-Prüfungen laden jetzt ausschließlich `Prefabs/Actors/3D/Terrock_3D` beziehungsweise `Prefabs/Actors/3D/Noctarion_3D`, schließen parallele `SpriteActorPresentation`-Komponenten aus und prüfen alle zehn freigegebenen Zustandsclips. Die Occlusion-Prüfung erkennt sowohl den historischen Fade-Zwillingsshader als auch den shaderunabhängigen Transparenzpfad der Mid-Poly-Materialien über den Laufzeitklon und dessen Alphaeigenschaften.

Rohresultat des letzten Unity-Laufs: `C:\Users\phine\AppData\LocalLow\Eidren\Eidren\TestResults.xml`

## Windows-x64-Build

- Builder: `Eidren/Release/Build Windows Development Snapshot`
- Ergebnis: Success
- Version: 0.3.2
- Plattform: Windows x64
- Grafik-API: Direct3D 11
- Szenen: 12 produktive Szenen
- Unity-Inhalt: 289,2 MB komprimiert / 1,01 GB unkomprimiert
- ZIP-Einträge: 101
- ZIP-Größe: 293.916.651 Bytes
- Archiv: `Releases/Eidren-v0.3.2-windows-x64.zip`
- Manifest: `Releases/Eidren-v0.3.2-windows-x64.manifest.json`
- Prüfsumme: `Releases/Eidren-v0.3.2-windows-x64.sha256`

SHA-256:

`7273bea133cfcaa83fcf24b4c278255d2af9ca8020c106ae1de696f9fe6090d9`

Die Prüfsumme aus der `.sha256`-Datei, der Hash im Manifest und der neu berechnete Datei-Hash sind identisch. Die im Manifest gespeicherte Archivgröße stimmt ebenfalls exakt.

## Player-Smoke-Test

Der Player aus dem Staging-Verzeichnis lief 15 Sekunden stabil und wurde anschließend kontrolliert beendet. Nachgewiesen wurden:

- Direct3D 11.0, Feature Level 11.1;
- Assemblies vollständig geladen;
- PhysX initialisiert;
- `Eidren/World/VertexLitFade` als eingebauter Fade-Zwilling gefunden;
- keine Exception, kein Error und kein Crash im Startlog.

Startlog: `TempReview/midpoly-build-smoke-20260825-145631.log`

Die Unity-Editor-Konsole enthielt nach Build und Validierung 0 Fehler.
