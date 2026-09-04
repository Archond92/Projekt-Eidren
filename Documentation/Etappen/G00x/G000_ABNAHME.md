# G-000 — Abnahme

**Stand:** 5. August 2026
**Auftrag:** `GRAFIKAUFTRAEGE_V0.2.1.md`, G-000 „Quellprojekt wiederherstellen"
**Ort:** Projektwurzel `C:\Users\phine\Documents\Projekt Eidren`
**Vorbefund:** [G000_STUFE0_BEFUND.md](G000_STUFE0_BEFUND.md)

---

## 1. Kurzfassung

Das Unity-Quellprojekt ist wiederhergestellt. Es öffnet im mitgelieferten
Editor, kompiliert fehlerfrei, alle zwölf Spielszenen laden ohne ein einziges
fehlendes Skript, und beide Testläufe laufen durch.

Die Rekonstruktion erreicht **99,97 % Strukturtreue** im Code und bringt
**81,5 %** der Assets an ihren Originalpfad zurück. Was fehlt, fehlt, weil es im
ausgelieferten Build nie enthalten war — Unity nimmt nur auf, was referenziert
wird. Der Abschnitt „Was fehlt" benennt das im Einzelnen.

## 2. Abnahmekriterien

| Kriterium | Ergebnis |
| --- | --- |
| Öffnet im Editor unter `.unity-editor/` ohne Kompilierfehler | **erfüllt** |
| `ProjectSettings/` und `Packages/` vorhanden und vollständig | **erfüllt mit Einschränkung** |
| Alle Spielszenen laden, `Zone_Greenwood` spielbar | **erfüllt** |
| EditMode- und PlayMode-Lauf, Abweichungen benannt | **erfüllt** |
| Windows-x64-Build aus den Quellen | **erfüllt** |
| Korrekturen aus `PlayerVisualFixes.cs` überführt oder als überholt eingestuft | **erfüllt** |
| Zustand gegenüber dem v0.2-Build dokumentiert | dieses Dokument |

## 3. Code

Alle neun Assemblies werden gebaut, einschließlich `Eidren.Editor`,
`Eidren.Tests` und `Eidren.PlayMode.Tests`, die im ausgelieferten Build gar
nicht enthalten waren und aus `Library/ScriptAssemblies` stammen.

| Assembly | Mitglieder | fehlen | Treue |
| --- | ---: | ---: | ---: |
| Eidren.Composition | 1598 | 0 | 100,00 % |
| Eidren.Core | 2077 | 0 | 100,00 % |
| Eidren.Data | 1610 | 0 | 100,00 % |
| Eidren.Gameplay | 2065 | 2 | 99,90 % |
| Eidren.Presentation | 547 | 0 | 100,00 % |
| Eidren.UI | 869 | 2 | 99,77 % |
| Eidren.Editor | 1105 | 0 | 100,00 % |
| Eidren.Tests | 1334 | 0 | 100,00 % |
| Eidren.PlayMode.Tests | 360 | 0 | 100,00 % |
| **Gesamt** | **11 565** | **4** | **99,97 %** |

Die vier verbleibenden Abweichungen sind compilergenerierte Lambda-Methoden
(`<BindButtons>b__47_0` und drei weitere); der Compiler vergibt diese Namen beim
Neuübersetzen anders. Inhaltlich fehlt nichts.

### 3.1 Was der Code nicht zurückbringt

- **Kommentare.** Dekompilat enthält keine.
- **Die ursprüngliche Formulierung.** Ausdrücke sind ausgeschrieben, Casts
  explizit, Schleifen teils umgeformt. Deshalb sind die Dateien länger als im
  Original — siehe Abschnitt 5.2.
- **Vier Dateien endgültig:** `Auftrag5CameraDiagnosticRunner.cs`,
  `Auftrag5RendererDiagnosticRunner.cs`, `V02IconVariantBuilder.cs`,
  `AreaTransitionState.cs`. Sie standen in der Asset-Datenbank, wurden aber vor
  dem letzten Kompilieren gelöscht; ihre Typen existieren in keiner Assembly.
  Die ersten drei sind Wegwerf-Diagnosewerkzeuge aus Auftrag 5.
- **Die Dateiaufteilung von 82 deklarativen Typen.** Enums und Interfaces ohne
  Methodenrümpfe hinterlassen keine Spur im PDB. Sie liegen jetzt als eigene
  Datei in dem Ordner, den ihr Namensraum vorgibt. Der Ordner ist belegt, die
  ursprüngliche Gruppierung nicht — das behauptet die Rekonstruktion auch nicht.

### 3.2 Drei rekonstruierte Ausdrücke

An drei Stellen hat der Dekompilierer Code verloren, den ich aus dem Umfeld
wiederhergestellt habe. Alle drei sind im Quelltext als Rekonstruktion
kommentiert:

- `GridCoordinate` und `GridEdge`: `operator !=` fehlte, obwohl
  `op_Inequality` in der Assembly vorhanden ist.
- `BuildingSystemIntegrationTests`: ein Lambda war zur nackten Methodengruppe
  `Enumerable.Count` zusammengefallen. Aus dem Cast auf
  `ActualValueDelegate<int>`, dem Testaufbau und der vorhandenen API
  wiederhergestellt.
- `BuildingRoofContentTests`: eine LINQ-Bereichsvariable kollidierte mit einer
  hochgezogenen `out`-Variablen; umbenannt.

## 4. Assets

| Endung | Soll | am Originalpfad | fehlt |
| --- | ---: | ---: | ---: |
| `.png` | 613 | 607 | 6 |
| `.asset` | 609 | 523 | 86 |
| `.cs` | 495 | 454 | 41 |
| `.mat` | 284 | 198 | 86 |
| `.prefab` | 169 | 121 | 48 |
| `.wav` | 163 | 163 als `.ogg` | 0 |
| `.unity` | 12 | 12 | 0 |
| `.asmdef` | 9 | 9 | 0 |
| `.shader` | 3 | 3 | 0 | *(siehe 4a)* |
| `.mixer` | 1 | 1 | 0 |

Die Zuordnung erfolgte über die Dateinamen gegen das Pfadverzeichnis in
`Library/SourceAssetDB`. Der exaktere Weg über GUIDs war nicht gangbar:
`SourceAssetDB` enthält zwar eine Pfad-zu-GUID-Zuordnung, deren Satzaufbau ich
entschlüsseln konnte, aber AssetRipper vergibt beim Export neue GUIDs — keine
Kodierung stimmte überein.

**Audio:** Die 163 Klänge liegen an ihrem Originalpfad, aber als `.ogg`. Im
Build ist Audio durchgängig Vorbis; die WAV-Originale sind nicht
wiederherstellbar. Die Endung sagt jetzt, was tatsächlich drinsteht.

**Skriptverweise:** 3475 von 3475 `m_Script`-Verweisen lösen auf. Dafür mussten
64 GUIDs auf die installierten Pakete umgebogen werden — AssetRipper
dekompiliert auch Paketskripte wie `Image`, `Text` und `NavMeshSurface` und
vergibt ihnen eigene GUIDs, sodass die Prefabs ins Leere zeigten.

**Szenen:** alle zwölf laden, 1666 Objekte, **0 fehlende Skripte**.

**`Zone_Greenwood` spielbar:** Die Szene wird von den PlayMode-Tests 20-mal
geladen und im Spielmodus bespielt; 110 von 115 dieser Tests bestehen. Das ist
der belastbare Nachweis, nicht nur das Öffnen im Editor.

## 4a. Nachtrag Shader — Korrektur einer Fehlaussage

**Dieser Abschnitt korrigiert die erste Fassung dieses Berichts.** Dort stand
„3 Shader fehlen". Das war falsch, und der tatsächliche Befund war ungünstiger:
Die Shader waren **vorhanden, aber funktionslos**. Fehlend wäre besser gewesen,
denn dann hätte Unity es gemeldet.

AssetRipper kann Shader nicht zurückübersetzen — im Build liegen sie nur als
übersetzter Code. Er schreibt deshalb Attrappen mit dem Marker
`//DummyShaderTextExporter`, die den Eigenschaftenblock übernehmen und im
Fragmentprogramm eine Konstante oder eine einfache Texturabfrage zurückgeben.
**Alle 44 exportierten Shader waren solche Attrappen.**

### Die Paketshader — 170 betroffene Materialien

41 der 44 gehören zu Unity-Paketen. AssetRipper hatte ihnen eigene GUIDs
vergeben, sodass die Materialien auf die Attrappen zeigten statt auf die echten
Shader des installierten URP-Pakets:

| Shader | Materialien |
| --- | ---: |
| `Universal Render Pipeline/Lit` | 103 |
| `Universal Render Pipeline/Unlit` | 64 |
| `URP 2D Sprite-Unlit-Default` | 3 |

Praktisch jedes Material im Spiel wurde dadurch unbeleuchtet und ohne Normalmaps
gezeichnet. Es ist dieselbe Fehlerklasse wie bei den Paket-**Skripten** in
Abschnitt 4; dort wurde sie erkannt und behoben, bei den Shadern nicht.

Behoben nach demselben Muster: Zuordnung über den Shadernamen, **alle 41
abbildbar**, 188 Verweise in 172 Assets umgeschrieben, Attrappen entfernt.

### Die drei projekteigenen Shader

Sie lagen unter `Assets/Shader/` statt an ihren Originalpfaden und sind jetzt
zurückgelegt, mit erhaltenen GUIDs:

| Shader | Pfad | Zustand |
| --- | --- | --- |
| `HandPaintedActorSprite` | `_Game/Shaders/Actors/` | neu geschrieben |
| `ActorGroundShadow` | `_Game/Shaders/Actors/` | neu geschrieben |
| `AreaArtBlend` | `_Game/Shaders/` | unter G-001 wiederhergestellt |

Der Figurenshader wiegt am schwersten: Seine Attrappe verwendete **zwei von
fünfzehn** deklarierten Eigenschaften und zeichnete jede Figur unbeleuchtet,
ohne Normalmap, ohne Emission, ohne Rüstungsebene und ohne Umriss.

Die neuen Fassungen sind **Rekonstruktionen gegen den nachweisbaren Vertrag**,
keine Originalquellen: die deklarierten Eigenschaften und die Werte, die
`SpriteActorPresentation.ApplyMaterialProperties()` per MaterialPropertyBlock
setzt. Beide tragen diesen Hinweis im Kopf.

**Ausdrücklich nicht erfunden:** Die Bedeutung von `_ArmorTiers` ist aus dem
Build nicht erkennbar. Der Wert wird deklariert und übergeben, aber nicht
ausgewertet. `_ArmorParts` dient nur als Schalter — eine teilweise Überblendung
einzelner Körperpartien bräuchte eine Partiemaske, die es im Projekt nicht gibt.
**G-006 und G-007 müssen beides festlegen.**

### Nachtrag AudioMixer

Beim systematischen Prüfen aller Verweise fiel ein zweiter Fehler derselben Art
auf. Die drei ausgesetzten Parameter in `EidrenAudioMixer.mixer` trugen die von
AssetRipper erfundenen Namen `OtWVUHN`, `mposwoH` und `tuRmmTJ`. `SettingsService`
spricht sie aber über feste Namen an — **die Lautstärkeregler im Pausenmenü
waren damit wirkungslos.**

Die Zuordnung war lückenlos belegbar: Gruppenname → `m_Volume`-Kennung in
derselben Datei → Konstante in `SettingsService`.

| Gruppe | Parameterkennung | Name jetzt |
| --- | --- | --- |
| Master | `add9a677…` | `MasterVolume` |
| Music | `3214f37e…` | `MusicVolume` |
| SFX | `a8e60609…` | `SFXVolume` |

Die Gruppe `UI` ist nicht ausgesetzt, was zu genau drei Parametern passt.
`AudioMixer_ContainsRequiredGroupsAndParameters` steht seither auf Passed.

### Vollständige Verweisprüfung

Nach zwei Fehlern derselben Klasse wurde jede GUID geprüft, die in einem Asset
oder in den Projekteinstellungen referenziert wird: 10 744 bekannte GUIDs gegen
1141 Dateien. **Kein Verweis zeigt ins Leere.**

Ebenfalls geprüft: die 270 Assets, die noch in AssetRippers Typordnern liegen
(`Assets/Mesh`, `Assets/Sprite`, …). **170 davon sind tragend** — Netze der
Weltkisten, Bodenübergangsflächen je Zone, Sprites — und werden aus `_Game`
heraus referenziert. Sie hatten nur deshalb keinen Pfad in `SourceAssetDB`, weil
sie zur Bauzeit erzeugt wurden. Sie bleiben, wo sie sind; ein Aufräumen wäre rein
kosmetisch und würde Szenen und Prefabs gefährden.

### Was das für die Folgeaufträge bedeutet

G-002 bis G-010 beschreiben Beobachtungen aus einem Build, in dem sämtliche
Materialien durch Attrappen gezeichnet wurden. Ein Teil der dort beschriebenen
Symptome — flache Beleuchtung, fehlende Bodenschatten, aufgesetzt wirkende
Objekte — kann allein hierauf zurückgehen. **Die Befunde sollten gegen einen
neuen Build gegengeprüft werden, bevor daraus Arbeit abgeleitet wird.**

## 5. Testläufe

| Lauf | dokumentiert | jetzt | bestanden | Fehler | übersprungen |
| --- | ---: | ---: | ---: | ---: | ---: |
| EditMode | 702 | 709 | 636 | 73 | 0 |
| PlayMode | 113 | 115 | 110 | 4 | 1 |

Verlauf des EditMode-Laufs über die Nachträge aus Abschnitt 4a:

| Stand | bestanden | Fehler |
| --- | ---: | ---: |
| erste Abnahme | 633 | 76 |
| nach der Shaderreparatur | 635 | 74 |
| nach der Mixerreparatur | 636 | 73 |

`ActorShaders_HaveNoCompilerErrors` und
`AudioMixer_ContainsRequiredGroupsAndParameters` bestätigen beide Reparaturen und
stehen auf **Passed**. Der PlayMode-Lauf ist unverändert — keine Regression.

Es laufen sieben EditMode- und zwei PlayMode-Tests mehr als dokumentiert. Die
Test-Assemblies stammen aus `Library/ScriptAssemblies` und bilden damit den
Stand vom 4. August ab, nicht zwingend den Stand der Dokumentation.

### 5.1 Die 76 EditMode-Fehler

| Ursache | Anzahl |
| --- | ---: |
| Fehlendes Datenasset (`.asset`) | 24 |
| Fehlendes Prefab | 15 |
| Fehlendes Kunst-Asset (`.png`) | 10 |
| Fehlender Shader | 2 |
| Fehlender AudioMixer-Kanal | 1 |
| Architekturregel (§7) | 2 |
| Ohne eindeutige Zuordnung | 22 |

Die 22 ohne Zuordnung sind bei Durchsicht ebenfalls Folgefehler fehlender
Inhalte: erwartete Texturgrößen, Zählungen über Kataloge, `Expected: not null`
auf nicht vorhandene Assets.

### 5.2 Die zwei Architekturfehler sind ein Dekompilat-Artefakt

`ArchitectureGuardTests.NoNewFileExceedsTheClassBudget` meldet sieben Dateien
über der 400-Zeilen-Grenze, darunter
`Composition/BuildingPlacementController.Continuation.cs` mit 817 Zeilen. Das
sind Originaldateien, keine von mir zusammengeführten. Dekompilierter Code ist
länger als sein Original — die Regel schlägt an, weil die Zeilen anders
umbrochen sind, nicht weil die Struktur verletzt wäre. Umgekehrt meldet
`GrandfatheredBudgetList_ContainsNoStaleEntries`, dass
`WindowsPerformanceAuditRunner.cs` jetzt *unter* der Grenze liegt.

Diese beiden Tests messen die Textform des Quelltexts. Sie lassen sich erst
wieder sinnvoll bewerten, wenn die Dateien redaktionell überarbeitet sind.

### 5.3 Die vier PlayMode-Fehler

`BuildCraftAndDemolish_UsesTheRealHomeBaseChain` (erwartet 11 Gebäude im
Katalog), `HomeBaseConsumables_UseItemDefinitionValues`, sowie zwei
Loot-Integrationstests. Alle vier hängen an fehlenden `.asset`-Definitionen.

## 6. Was fehlt und warum

Der Grund ist in allen Fällen derselbe: **Unity nimmt in einen Build nur auf,
was referenziert wird.** Nicht referenzierte Materialien, editor-seitige
Prefabs, Slice-Tabellen und Werkzeug-Assets bleiben draußen und sind aus dem
Build nicht rekonstruierbar.

- **86 Materialien**, **86 Datenassets**, **48 Prefabs**, **6 PNG**,
  **3 Shader** — im Build nicht enthalten.
- **364 von 630 Texturen** sind blockkomprimiert und damit nicht in
  Originalqualität wiederherstellbar; besonders die Figuren-Spritesheets
  (DXT5Crunched) und die Bodentexturen. 266 Texturen kommen bitgenau zurück.
- **Alle 163 Klänge** liegen als Vorbis vor.

Die vollständigen Listen stehen in `g000-fehlende-assets.txt` und
`g000-assets-ohne-sollpfad.txt` im Projektwurzelverzeichnis.

## 6a. Windows-Build

`Eidren.Editor.WindowsReleaseBuilder.BuildDevelopment` läuft aus den
rekonstruierten Quellen durch und erzeugt

```
Releases/Staging/Eidren-v0.2.0-dev-windows-x64/
```

mit `Eidren.exe`, **0,56 GB**. Der ausgelieferte v0.2-Build umfasst 0,65 GB; die
Differenz entspricht den fehlenden Inhalten aus Abschnitt 6.

**Nach den Nachträgen aus Abschnitt 4a neu gebaut** (5. August, 21:51,
101 Dateien): null Kompilierfehler, **null Shaderfehler**, sauberer Abschluss.
Das ist der erste Stand, der mit echten statt mit Attrappen-Shadern entstanden
ist — und damit die Grundlage, gegen die die Beobachtungen aus G-002 bis G-010
gegengeprüft werden sollten.

Der ebenfalls unter `Releases/Staging` liegende `Eidren-v0.1.0-windows-x64`
stammt vom 1. August und gehört nicht zu dieser Rekonstruktion.

## 7. Projekteinstellungen

`ProjectSettings.asset` liegt vor, wurde aber **nicht** aus dem Build
übernommen: AssetRipper und UnityPy scheitern beide identisch daran, weil ihr
Typbaum für Unity 6000.3.0f1 vier Bytes zu kurz ist (das Objekt hat 864 Bytes,
gelesen werden 860). Unity hat die Datei beim ersten Öffnen mit Standardwerten
erzeugt.

Aus den Rohbytes des Build-Objekts ausgelesen und eingesetzt:

| Feld | Wert | Herkunft |
| --- | --- | --- |
| `companyName` | Eidren | aus dem Build ausgelesen |
| `productName` | Eidren | aus dem Build ausgelesen |
| `bundleVersion` | 0.2.0-dev | aus dem Build ausgelesen |
| `activeInputHandler` | 1 (Input-System-Paket) | aus dem Code belegt: keine Verwendung der alten API, 12 Dateien nutzen das Paket |

**Offen und ausdrücklich nicht belegt:** der Farbraum. Unitys Standard ist
Gamma; URP-Projekte laufen üblicherweise unter Linear. Ich habe dafür keinen
Beleg aus dem Build gefunden und stelle den Wert deshalb nicht auf Verdacht um.
Das gehört zu **G-003** (Beleuchtung, Tonwertaufbau und Bildabstimmung) und
muss dort vor jeder Bildbewertung geklärt werden — ein falscher Farbraum macht
jeden Vergleich mit den Referenzaufnahmen wertlos.

`Packages/manifest.json` musste ich selbst schreiben. AssetRippers Fassung
enthielt nur die eingebauten Module und keines der 15 tatsächlich verwendeten
Pakete; die Versionen stammen aus `Library/PackageCache`.

## 8. Visual Fixes

| Korrektur | Entscheidung |
| --- | --- |
| `PlayerFacingCorrection` | in die Quellen überführt |
| `DualDaggerHands` | in die Quellen überführt, standardmäßig inaktiv |
| `DaggerShowcaseStarter` | als überholt eingestuft |

**`PlayerFacingCorrection`** behob einen echten Fehler und steht jetzt in
`PlayerVisualAnimator.ResolveFacingDirection()`. `PlayerMotor` setzt
`IsMoving = … && !IsDodging` und dreht den Transform nur verzögert per Slerp;
die Quelle richtete die Figur beim Ausweichen deshalb an der nachlaufenden
Transformrichtung aus statt an der Ausweichrichtung. Die Quellfassung nutzt
zusätzlich den projekteigenen `EightDirectionResolver` samt Hysterese statt der
gröberen Quantisierung des Shims.

**`DualDaggerHands`** steht als `PlayerWeaponVisual.PoseDaggerHands()` in den
Quellen, gesteuert über ein serialisiertes Sprite-Feld statt über einen
Laufzeit-Suchlauf und Rohdaten aus StreamingAssets. Das Kunstasset liegt als
`Assets/_Game/Art/Items/ITEM_PlayerDagger_Single.png` mit Pivot 0,17/0,16 und
512 Pixel pro Einheit. Solange das Feld in den Prefabs leer ist, bleibt das
Verhalten wie in den Quellen. **Über die endgültige Waffenanbindung entscheidet
G-006** — bis dahin wird nichts behauptet.

**`DaggerShowcaseStarter`** war reine Diagnostik zum Erzwingen der Dolchauswahl
und entfällt.

`Tools/Deploy-VisualFix.ps1` bricht beim Aufruf mit einem Hinweis ab. Der Weg
über injizierte DLLs wird nicht weiter ausgebaut; die StreamingAssets des Shims
sind entfernt.

## 9. Übernahme in die Projektwurzel

Das Projekt wurde aus `TempReview/Rekonstruktion` in die Projektwurzel gezogen:
`Assets/` (5262 Dateien), `Library/`, `Packages/`, `ProjectSettings/` und
`UserSettings/`. Der Windows-Build liegt unter
`Releases/Staging/Eidren-v0.2.0-dev-windows-x64`.

Entfernt wurden dabei das alte `Assets/` mit dem Visual-Fix-Shim, die alte
`Library/` des verlorenen Projekts sowie die beiden AssetRipper-Zwischenstände
(1,7 GB). Alles davon liegt in der Sicherung; die Exporte sind aus dem Build
jederzeit neu erzeugbar.

**Wichtig für alle weiteren Aufträge:** Die alte `Library/` war die einzige
Spur der Projektstruktur — `SourceAssetDB` mit 2571 Assetpfaden und
`ScriptAssemblies` mit allen neun Assemblies samt PDBs. Sie existiert jetzt nur
noch in der Sicherung. Wer dort aufräumt, vernichtet die Belege, gegen die
diese Rekonstruktion geprüft wurde.

## 10. Werkzeuge und Sicherung

Die Sicherung des Ausgangszustands liegt unter
`C:\Users\phine\Documents\Projekt Eidren - Sicherung 2026-08-05`
(4,63 GB, 26 759 Dateien).

| Werkzeug | Zweck |
| --- | --- |
| AssetRipper 1.3.14 (`.tools/AssetRipper`) | Szenen, Prefabs, Materialien, Texturen |
| ilspycmd 9.1 (`.tools/ilspycmd`) | Editor- und Test-Assemblies, 92 nachgezogene Typen |
| Mono.Cecil (Unity-PackageCache) | PDB-Auswertung, Strukturvergleich |
| UnityPy 1.25.3 (`.tools/pyenv`) | Formatanalyse, Rohbytes der PlayerSettings |
