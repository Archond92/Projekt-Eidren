# Mid-Poly Phase-0-Baseline

Diese Dateien werden von `Eidren.Editor.MidpolyPhase0Baseline.Run` erzeugt. CSV-Dateien sind UTF-8 mit Semikolon als Trennzeichen. Das JSON ist die vollständige maschinenlesbare Fassung des visuellen Registers.

Reproduktionsbefehl (PowerShell, ohne `-nographics`, weil Bilder gerendert werden):

```powershell
& '.\.unity-editor\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod Eidren.Editor.MidpolyPhase0Baseline.Run -logFile phase0-midpoly.log
```

Der Generator ersetzt den Capture-Ordner atomar auf Ordnerebene und verändert keine produktiven Prefabs, Datenobjekte oder Szenen.

Bekannter Bestandsfehler: `Assets/_Game/Editor/Tests/WeltsaatTests.cs` verweist derzeit auf das entfernte `ZoneStateService.DefaultMasterSeed`. Bis dieser unabhängige Compilerfehler behoben ist, muss die Editor-Testassembly für einen erneuten Auditlauf kontrolliert temporär ausgeschlossen und anschließend unverändert reaktiviert werden.
