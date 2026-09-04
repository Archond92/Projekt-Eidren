# G-000, Stufe 0 — Befund und Machbarkeit

**Stand:** 5. August 2026
**Auftrag:** `GRAFIKAUFTRAEGE_V0.2.1.md`, G-000 „Quellprojekt wiederherstellen"
**Ergebnis:** Rekonstruktion ist machbar. Umfang und Grenzen stehen unten.

---

## 1. Es existiert kein Backup

Geprüft und ausgeschlossen:

| Quelle | Befund |
| --- | --- |
| Git-Historie | `.gitignore` enthält `*`; über alle drei Commits sind nur fünf Dateien getrackt. Die Quellen waren nie im Repository. |
| Weiteres Unity-Projekt auf C: | keines — Suche nach `ProjectVersion.txt` in Documents, Desktop, Downloads, OneDrive, source |
| Eidren-Archive außerhalb des Projekts | keine; unter `Releases/` liegen nur die beiden Build-ZIPs |
| Original-Kunst außerhalb des Projekts | keine |
| Papierkorb | zwölf Einträge, davon nur ein alter Build-Ordner |
| Schattenkopien, File History | nicht vorhanden bzw. nicht zugänglich; nur Laufwerk C: |
| Unity-Registry | `LastUsedProjectPath` verweist auf genau dieses Verzeichnis — das Projekt lag nie woanders |

Die Wiederherstellung erfolgt daher vollständig aus dem vorhandenen Bestand.

## 2. Sicherung

Vor jedem schreibenden Schritt gesichert nach
`C:\Users\phine\Documents\Projekt Eidren - Sicherung 2026-08-05`:

- `Library/`, `Assets/`, `TempReview/`, `Builds/` sowie die Wurzeldokumente
- **4,63 GB in 26 759 Dateien**

`Library/` ist die einzige verbliebene Spur der Projektstruktur. Ohne diese
Sicherung wäre jeder Fehlschlag im Rekonstruktionsverlauf endgültig.

## 3. Verwertbarer Bestand

### 3.1 `Library/SourceAssetDB` — das Pfadverzeichnis

16 MB, enthält **2571 eindeutige Assets-Pfade**:

| Endung | Anzahl | Endung | Anzahl |
| --- | ---: | --- | ---: |
| `.png` | 613 | `.unity` | 18 |
| `.asset` | 609 | `.asmdef` | 9 |
| `.cs` | 495 | `.shader` | 3 |
| `.mat` | 284 | `.json` | 2 |
| `.prefab` | 169 | `.mixer` | 1 |
| `.wav` | 163 | | |

Zwölf Spielszenen: `Bootstrap`, `MainMenu`, `HomeBase`, `EidraForge`,
`WorldMap/WorldMap` sowie `Zone_Greenwood`, `Zone_EmberRuins`, `Zone_GreyRifts`,
`Zone_Marsh`, `Zone_Quarry`, `Zone_TwilightGrove`, `Zone_VeilMarsh`.

Neun Assemblies: `Eidren.Editor`, `Eidren.Tests`, `Eidren.Composition`,
`Eidren.Core`, `Eidren.Data`, `Eidren.Gameplay`, `Eidren.Presentation`,
`Eidren.UI`, `Eidren.PlayMode.Tests`.

### 3.2 `Library/ScriptAssemblies` — Code samt Symbolen

Alle neun Eidren-Assemblies mit PDBs, Stand 4. August 18:30 — einschließlich
`Eidren.Editor` und beider Test-Assemblies, die im ausgelieferten Build gar
nicht enthalten sind.

### 3.3 Weiteres

- `Library/Artifacts`: 3821 Dateien, 1,64 GB importierte Assetform
- `Library/PackageCache`: 15 Pakete — Grundlage für `Packages/manifest.json`
- `Builds/…/Eidren_Data/data.unity3d`: 543 MB
- `.unity-editor/`: Unity **6000.3.0f1**
- `TempReview/Recovered*`: Dekompilate für vier der neun Assemblies aus einem
  früheren Anlauf (ILSpy-Stil, ein Typ je Datei)

## 4. Codeseite — vollständig geklärt

Die PDBs enthalten die **vollständigen Originalpfade** der Quelldateien. Gelesen
über Mono.Cecil aus dem Unity-PackageCache. Ergebnis für die 495 Skriptdateien:

| Kategorie | Dateien | Bewertung |
| --- | ---: | --- |
| Exakter Originalpfad aus dem PDB | 467 | vollständig rekonstruierbar |
| Über Typnamen zugeordnet | 11 | Enums und Interfaces ohne Methodenrümpfe |
| Sammeldatei `CombatTargetInterfaces.cs` | 1 | die vier Interfaces sind bekannt |
| Veraltete Einträge, Datei wurde verschoben | 2 | kein Verlust |
| Kein Projektcode | 10 | Test-Framework-Gerüst |
| **Endgültig verloren** | **4** | Typen existieren in keiner Assembly |

### 4.1 Die vier verlorenen Dateien

`Auftrag5CameraDiagnosticRunner.cs`, `Auftrag5RendererDiagnosticRunner.cs`,
`V02IconVariantBuilder.cs`, `AreaTransitionState.cs`.

Sie standen in der Asset-Datenbank, wurden aber vor dem letzten Kompilieren
gelöscht. Die ersten drei sind Wegwerf-Diagnosewerkzeuge aus Auftrag 5.

### 4.2 Die zwei verschobenen Dateien

`EidraForgeEntrance` und `PlayerExperienceFeedback` lagen ursprünglich unter
`Scripts/Interaction/` und `Scripts/Presentation/` und wurden nach
`Scripts/Composition/` verschoben. Die Asset-Datenbank hatte nur den alten
Eintrag behalten; der Code ist vorhanden.

### 4.3 Offene Restunschärfe

108 Enums und Interfaces besitzen keine Methodenrümpfe und damit keine
Sequenzpunkte. Ihre Assembly und ihr Namensraum sind bekannt, ihre exakte
Quelldatei nicht. Das betrifft die Originaltreue der Dateiaufteilung, **nicht**
die Kompilierbarkeit: C# bindet Typen an Assembly und Namensraum, nicht an
Dateien. Diese Typen werden nach Namensraum gruppiert abgelegt und im
Abschlussbericht als solche benannt.

## 5. Kunst und Audio — hier liegt der eigentliche Verlust

Gemessen mit UnityPy 1.25.3 über `data.unity3d`. **630 Projekttexturen:**

| Format | Anzahl | Qualität | Betrifft |
| --- | ---: | --- | --- |
| RGBA32 | 174 | **verlustfrei** | Weltkartenknoten, Marker, UI |
| RGB24 | 83 | **verlustfrei** | ein Teil der Normalmaps |
| Alpha8 | 9 | **verlustfrei** | Masken |
| DXT5Crunched | 121 | verlustbehaftet | Figuren-Albedo (doppelt komprimiert) |
| DXT1Crunched | 111 | verlustbehaftet | Emissionsmaps |
| BC5 | 111 | verlustbehaftet | Normalmaps |
| DXT1 | 13 | verlustbehaftet | Bodentexturen |
| DXT5 | 6 | verlustbehaftet | `player_portrait`, `ui_icon_atlas` |
| BC7 | 2 | verlustbehaftet | `greenwood_ground_v01`, Regionalkarte |

**266 von 630 Texturen (42,2 %) kommen bitgenau zurück.** Die übrigen 364 sind
blockkomprimiert und damit nicht in Originalqualität wiederherstellbar.

Besonders zu beachten:

- Die **Figuren-Spritesheets** liegen als DXT5Crunched vor — Blockkompression
  plus Crunch, also der ungünstigste Fall.
- Die **Bodentexturen** `greenwood_ground_v01` (BC7), `home_grass_v01` und
  `greenwood_soil_v01` (DXT1) sind genau das Material, das G-002 überarbeitet.
  Da G-002 sie ohnehin neu aufsetzt, ist der Verlust dort verschmerzbar.
- `player_portrait` liegt als DXT5 vor.

**Audio:** alle 163 Klänge liegen als Vorbis vor. Die WAV-Originale sind
nicht wiederherstellbar.

## 6. Werkzeuge

| Werkzeug | Stand | Zweck |
| --- | --- | --- |
| ILSpy / ilspycmd 9.1.0.7988 | lag bereits unter `.tools/` | Dekompilation |
| Mono.Cecil | aus dem Unity-PackageCache | PDB-Auswertung |
| UnityPy 1.25.3 | neu, in `.tools/pyenv` (Python 3.12.13) | Formatanalyse, gezielte Extraktion |
| AssetRipper 1.3.14 | neu, in `.tools/AssetRipper` | Szenen, Prefabs, Materialien |

AssetRipper unterstützt Unity 3.5.0 bis 6000.5.X; das Projekt liegt mit
6000.3.0f1 sicher darin. Der Download wurde gegen die erwartete Größe von
41 695 535 Bytes geprüft, SHA-256
`808CDDF66DD0357AD6B36B97DE3A2AEF5E3552E63AF3EE0610F9A03A0378101C`.

## 7. Bewertung

Machbar. Der Code kommt zu 96,6 % an seinen Originalplatz zurück, semantisch
vollständig, aber ohne Kommentare und mit den üblichen Umschreibungen des
Dekompilats. Der bleibende Verlust liegt bei vier Wegwerfdateien, der
Dateiaufteilung von 108 deklarativen Typen sowie der Bildqualität von 364
Texturen und aller 163 Klänge.

Für die nachfolgenden Grafikaufträge ist das tragfähig: G-002 bis G-010 setzen
Boden, Licht, Schatten und Anbindung ohnehin neu auf.
