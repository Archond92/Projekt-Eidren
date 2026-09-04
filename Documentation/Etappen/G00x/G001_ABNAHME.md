# G-001 — Abnahme

**Stand:** 5. August 2026
**Auftrag:** `GRAFIKAUFTRAEGE_V0.2.1.md`, G-001 „Capture- und Abnahmestrecke reparieren"
**Voraussetzung:** G-000 abgeschlossen ([G000_ABNAHME.md](G000_ABNAHME.md))
**Ergebnis:** alle acht Abnahmekriterien erfüllt, Nachweis in Abschnitt 3

---

## 1. Ursache der fehlgeschlagenen Captures

Die Ursache wurde vor jeder Änderung bestimmt, wie im Auftrag gefordert. Sie
liegt **nicht** im Capture-Mechanismus, sondern im Laufzeit-Shim, der die
Captures ausgelöst hat.

Alle drei fehlgeschlagenen Läufe stammen aus
`Eidren.VisualFixes.VisualFixCaptureRunner` — der injizierten Zusatz-DLL aus
`Tools/Deploy-VisualFix.ps1`, die G-000 abgeschafft hat. Die Belege stehen in
den Logs neben dem Build:

| Lauf | Ordner | Log | Befund |
| --- | --- | --- | --- |
| 1 | — | `visual-fix-daggers.log` | `FileNotFoundException: netstandard, Version=2.1.0.0` — die Shim-DLL lud gar nicht erst |
| 2 | `VisualFixDaggerCapture2` (leer) | `visual-fix-daggers-2.log` | `NullReferenceException` in `PlayerCombatController.SetWeaponAvailability`, ausgelöst aus `PlayerVisualFixes.cs:119` |
| 3 | `VisualFixDaggerCapture3` (leer) | `visual-fix-daggers-3.log` | `VISUAL FIX CAPTURE: Player combat did not initialize.` (`PlayerVisualFixes.cs:121`) |
| 4 | `VisualFixDaggerCapture4` (schwarzes BMP) | `visual-fix-daggers-4.log` | `NullReferenceException` in `Component.get_transform` aus `CaptureFacing` (`PlayerVisualFixes.cs:160`) |

**Die beiden leeren Ordner (2 und 3)** entstanden, weil der Shim abbrach, bevor
er einen einzigen Aufnahmeversuch unternahm. Es wurde nichts geschrieben, weil
nichts geschrieben werden konnte.

**Das schwarze Bild (Ordner 4)** ist der aufschlussreiche Fall: Der Shim schrieb
die Datei, nachdem `CaptureFacing` an einem zerstörten `Component` gescheitert
war. Die Aufnahme lief also gegen eine Szene, in der der Spieler nicht mehr
existierte, auf eine Kamera, die kein Ziel mehr hatte. Die im Auftrag genannte
Arbeitshypothese „Aufnahme vor dem ersten gerenderten Frame" trifft **nicht** zu
— der Frame war da, die Szene war es nicht.

**Gemeinsame Wurzel:** Der Shim suchte seine Ziele zur Laufzeit per
`FindObjectsByType` in einem fertigen Build und hatte keine Möglichkeit, auf
den tatsächlichen Initialisierungszustand des Spiels zu warten. Er griff auf
`PlayerCombatController` zu, bevor `BindRuntimeDependencies` gelaufen war. Das
ist dieselbe strukturelle Schwäche, die G-000 als Grund für die Abschaffung des
Injektionswegs benennt.

Die drei funktionierenden Captures unter `VisualFixSmokeFinal/` stammen dagegen
aus `PackageACaptureRunner` — einem regulären Runner **in den Quellen**, der
über Zustandsabfragen mit Zeitlimit auf die Initialisierung wartet. Dass genau
diese drei funktionierten und die vier Shim-Läufe nicht, bestätigt die Diagnose.

**Schlussfolgerung:** Zu reparieren war nicht der Aufnahmemechanismus, sondern
seine Verankerung. Die neue Strecke liegt deshalb vollständig in den Quellen.

### 1.1 Zweite Ursache: der Shader `Eidren/Area Art Blend`

Die neue Strecke lief, lieferte aber drei von vier Bildern mit **reinweißem
Boden**; Position `04_boden_leer` war vollständig einfarbig weiß. Die
Einfarbigkeitsprüfung meldete das korrekt — der Fehler lag nicht in ihr.

Die Eingrenzung erfolgte über eine Sichtbarkeitsdiagnose je Position:

| Position | `GroundBlend_1` sichtbar | `WalkableGround` sichtbar | Ergebnis |
| --- | --- | --- | --- |
| 01 | ja | ja | weiß, stdDev 93,69 |
| 02 | ja | ja | weiß, stdDev 96,18 |
| 03 | **nein** | ja | korrekt, stdDev 29,86 |
| 04 | ja | ja | vollständig weiß |

Die einzige Position ohne weißen Boden war zugleich die einzige, in der
`GroundBlend_1` außerhalb des Bildausschnitts lag. Dessen Material
`Greenwood_Ground_Blend_1` verweist auf den Shader `Eidren/Area Art Blend`.
Dieser Shader war ein **Dekompilat-Platzhalter**:

```hlsl
//DummyShaderTextExporter
float4 frag(Vertex_Stage_Output input) : SV_TARGET
{
    return float4(1.0, 1.0, 1.0, 1.0); // RGBA
}
```

Er gab eine Konstante zurück, deklarierte weder Sampler für `_BaseMap` noch
einen Blend-Zustand und trug `RenderType = Opaque`. Die als transparent
konfigurierte Übergangsfläche wurde dadurch als deckend weiße Platte gezeichnet.
Betroffen sind **15 Materialien** in allen Zonen.

Alle 44 Dateien unter `Assets/Shader/` trugen die Markierung
`//DummyShaderTextExporter`. Bei den übrigen 43 hatte der Exporter wenigstens
`_MainTex * _Color` rekonstruiert, sie zeigten deshalb ihre Textur.
`Eidren/Area Art Blend` war der einzige vollständig entartete Fall — und das
erklärt, warum genau dessen Flächen weiß erschienen.

> **Nachtrag vom 5. August 2026, 21:52 Uhr.** G-000 hat diesen Befund
> anschließend als Fehlerklasse aufgegriffen und alle 44 Attrappen behandelt
> (`G000_ABNAHME.md`, Abschnitt 4a): 41 Paketshader wurden über den Shadernamen
> wieder den echten URP-Shadern zugeordnet, die drei projekteigenen an ihre
> Originalpfade gelegt. `Assets/Shader/` existiert nicht mehr; die hier
> reparierte Datei liegt jetzt als `Assets/_Game/Shaders/AreaArtBlend.shader`
> und ist inhaltlich unverändert. Was in diesem Abschnitt steht, bleibt die
> Ursachenanalyse von G-001 — der Umfang der Reparatur gehört zu G-000.

**Behebung im Rahmen von G-001:** Der Shader wurde so wiederhergestellt, dass
er das Verhalten ausführt, das die vorhandenen Materialdaten bereits
beschreiben (`_Surface 1`, `_SrcBlend 5`, `_DstBlend 10`, `_ZWrite 0`,
`_Cull 2`, Queue 3000, Keyword `_SURFACE_TYPE_TRANSPARENT`): `_BaseMap` mal
`_BaseColor`, Alpha-Blending, kein Tiefenschreiben. Die referenzierten Texturen
`greenwood_soil_v01.png` und `greenwood_meadow_v01.png` sind vorhanden.

Das ist bewusst **keine gestalterische Änderung**, sondern die Reparatur eines
Dekompilierschadens aus G-000. Ohne sie kann kein Abnahmekriterium von G-001
erfüllt werden und G-002 hätte keine messbare Bodenfläche. Die Gestaltung der
Bodenübergänge bleibt Sache von G-002.

### 1.2 Verworfene eigene Hypothesen

Der Vollständigkeit halber, weil sie Arbeitszeit gekostet haben und in Logs
auftauchen:

| Hypothese | Widerlegt durch |
| --- | --- |
| Terrain nicht geladen | `Zone_Greenwood` enthält 0 Terrain-Objekte |
| Texture-Streaming lädt Mipmaps nicht nach | `streamingMipmapsActive: 0` |
| Zeitabhängiger Texture-Upload | 5 s Anlauf und 59–78 stabile Frames änderten nichts; Position 03 war korrekt, 01/02/04 nicht — also ortsabhängig, nicht zeitabhängig |
| Fehlendes Textur-Asset | Beide Texturen liegen mit über 2 MiB auf der Platte |

## 2. Was gebaut wurde

| Datei | Zweck |
| --- | --- |
| `Assets/_Game/Scripts/Composition/G001VisualAbnahmeRunner.cs` | Laufzeit-Runner, vier benannte Positionen, PNG-Ausgabe, Einfarbigkeitsprüfung |
| `Assets/_Game/Editor/G001VisualCaptureBuilder.cs` | Batchmodus-Eintrittspunkt für den Player-Build |
| `Tools/G001-capture.ps1` | Treiber: Build, zwei Läufe, Kamera- und Bildvergleich, optionaler Vergleich gegen einen früheren Stand (Abschnitt 4.1), Gesamturteil |
| `Assets/_Game/Shaders/AreaArtBlend.shader` | **geändert**: Dekompilat-Platzhalter durch funktionsfähigen URP-Shader ersetzt (Abschnitt 1.1). Lag zunächst unter `Assets/Shader/`, von G-000 an den Originalpfad verschoben |

Der Runner wird über das Kommandozeilen-Flag `-eidren-g001-capture` aktiviert
und schreibt nach `-eidren-g001-output <Verzeichnis>`.

### 2.1 Kamerapositionen

Der Positionssatz trägt die Versionskennung **`g001-v2`**, die in jedem Report
mitgeschrieben wird. Alle Positionen liegen in `Zone_Greenwood` bei 1920×1080.
Die Kennung wurde von `g001-v1` angehoben, als die Standorte der Positionen 01
bis 03 auf feste Weltkoordinaten umgestellt wurden (Abschnitt 2.6) — Bilder aus
`g001-v1` sind mit den heutigen also nicht vergleichbar.

| ID | Beschreibung | Ortho | Herkunft |
| --- | --- | ---: | --- |
| `01_copper_set_spear` | Kupfersatz plus Speer, Blick nach Süden | 2,8 | Vergleichsbasis aus `VisualFixSmokeFinal/01` |
| `02_mixed_ember_thorn` | Gemischter Satz plus Ember Thorn | 2,8 | Vergleichsbasis aus `VisualFixSmokeFinal/02` |
| `03_iron_axe_harvest` | Eisenaxt-Ernte-Pose am Baum | 2,8 | Vergleichsbasis aus `VisualFixSmokeFinal/03` |
| `04_boden_leer` | Leere Bodenfläche, Objekte deaktiviert | 6,0 | **neu**, Messfläche für G-002 |

Die Kamera wird für jede Aufnahme explizit gesetzt: Rotation
`Euler(52, 45, 0)` — identisch mit `IsometricCamera._fixedRotation`, also der
tatsächlichen Spielperspektive — und ein fester Versatz entlang der
Kamerarückrichtung. Danach wird der vorherige Kamerazustand vollständig
wiederhergestellt. Dadurch beeinflusst eine Aufnahme die nächste nicht.

Position 04 deaktiviert Spieler und alle `ResourceNode`-Objekte, nimmt einen
festen Weltpunkt auf und stellt den Zustand danach wieder her. Damit steht
G-002 eine reine Bodenfläche zur Messung zur Verfügung.

### 2.2 Auslösung nach vollständig gerendertem Frame

Der Lauf wartet zuerst über Zustandsabfragen mit Zeitlimit auf
`PlayerPrefabBindings` und `PlayerInputReader`; ohne beide bricht er mit
Fehlermeldung und Exit 1 ab, statt ein leeres Bild zu schreiben. Danach folgt
ein Anlauf von **300 Frames** — als Framezahl, nicht als Sekundenzahl, siehe
Abschnitt 2.6.

Die Aufnahme selbst erfolgt über
`ScreenCapture.CaptureScreenshotAsTexture()` nach `WaitForEndOfFrame`. Damit ist
per Konstruktion der fertige Frame gemeint: `ScreenCapture` liest den
präsentierten Backbuffer. Der Beleg steht im Bild selbst — das
„Development Build"-Wasserzeichen des Players ist mit aufgenommen, und dieses
Overlay wird erst am Ende des Frames gezeichnet.

Statt einer festen Framezahl wartet der Runner auf **Bildstabilität**: Er
vergleicht aufeinanderfolgende Aufnahmen und gibt erst frei, wenn mindestens 30
Frames vergangen und 30 aufeinanderfolgende Vergleiche unter einer mittleren
Kanaldifferenz von 0,5 geblieben sind. Ein Zeitlimit von 45 s je Position und
ein Watchdog von 300 s je Lauf verhindern Hänger.

**Zwei Irrwege, die dabei ausgeschlossen wurden — beide URP-bedingt:**

- `Camera.Render()` von Hand aufzurufen ist unter URP nicht unterstützt und
  lieferte unvollständige Frames mit falscher Löschfarbe.
- Der Kamera zur Laufzeit ein `targetTexture` zuzuweisen führte zu einer
  RenderTexture, in die nie geschrieben wurde: Alle Pixel waren exakt
  (255, 255, 255), während die Löschfarbe der Kamera (0,075 / 0,12 / 0,14)
  ist. Die Kamera hatte das Ziel nachweislich nie berührt.

Zusätzlich wird für die Dauer der Aufnahme die `IsometricCamera`-Komponente
abgeschaltet. Deren `LateUpdate` bewegt die Kamera per `Vector3.SmoothDamp` und
zieht die `orthographicSize` per `Mathf.Lerp` nach — beides zeitabhängig und
damit über zwei Läufe hinweg nicht reproduzierbar. Danach wird der vorherige
Zustand vollständig wiederhergestellt.

Der Unterschied zum Shim: Die neue Strecke prüft den Spielzustand, bevor sie
auslöst, und meldet jeden Fehlschlag als Fehlschlag, statt eine unbrauchbare
Datei zu schreiben. Sie ist damit gegen die Ursache aus Abschnitt 1 gesichert —
nicht aber automatisch gegen jede andere Fehlerart; der Hänger, der bei der
Entwicklung auftrat (Ausnahme in `Awake`, die Unity verschluckt), wurde erst
durch den Watchdog sichtbar.

### 2.3 PNG statt BMP

`Texture2D.EncodeToPNG()` ersetzt die handgeschriebene BMP-Ausgabe.

### 2.4 Einfarbigkeitsprüfung

Für jedes Bild wird die Standardabweichung der Pixelhelligkeit berechnet
(ITU-R-Wichtung 0,299 R / 0,587 G / 0,114 B, jeder vierte Pixel abgetastet).
Liegt sie unter **3,0**, gilt das Bild als einfarbig. Das Ergebnis steht als
`brightnessStdDev` und `isMonochrome` je Position im Report; der Runner
protokolliert zusätzlich eine Warnung. `Tools/G001-capture.ps1` bricht ab,
sobald ein Bild als einfarbig gemeldet wird.

Zur Einordnung: Das schwarze `daggers_south.bmp` hätte eine Streuung von 0,0.

### 2.5 Report

`g001-capture-report.json` je Lauf, mit Positionssatz-Version, Szene,
Auflösung, Buildstand (`Application.version`), Erzeugungszeitstempel sowie je
Position: ID, Beschreibung, Dateiname, **Kameraposition, Kamerarotation und
orthografische Größe**, Aufnahmezeitstempel, Helligkeitsstreuung,
Einfarbigkeits- und Stabilitätsurteil.

Die Kamerawerte stehen bewusst im Report und nicht nur im Log: Der Auftrag
verlangt, dass der Report jede Kameraposition nennt und dass die Positionen
über Läufe hinweg identisch sind. Beides ist damit durch einen Vergleich der
beiden Report-Dateien überprüfbar, statt zugesichert werden zu müssen.
`Tools/G001-capture.ps1` führt diesen Vergleich als eigenen Schritt vor dem
Bildvergleich durch. Die Werte werden mit fester Kultur und vier
Nachkommastellen formatiert — `Vector3.ToString()` rundet auf eine Stelle und
folgt dem Dezimaltrennzeichen des Systems, beides untauglich für einen
textuell vergleichbaren Report.

### 2.5a Bodendiagnose bleibt im Code

`LogGroundDiagnostics` schreibt je Position Kamerazustand sowie Sichtbarkeit,
Material und Shader der Bodenobjekte ins Log. Das ist die Ausgabe, mit der der
Shader-Defekt aus Abschnitt 1.1 überhaupt lokalisiert wurde — die Zeile
`shader=Eidren/Area Art Blend shaderUnterstuetzt=True` bei gleichzeitig weißer
Fläche war der entscheidende Hinweis.

Sie bleibt bewusst erhalten, obwohl sie das Log deutlich verlängert: G-002
arbeitet am selben Boden und wird dieselben Angaben brauchen. Sie schreibt
ausschließlich ins Log, nie in den Report, und beeinflusst kein
Abnahmekriterium.

### 2.6 Wiederholbarkeit über zwei Läufe

Das Kriterium „zwei Läufe erzeugen visuell deckungsgleiche Bilder" war der
aufwendigste Teil des Auftrags. Es hat vier voneinander unabhängige
Zufallsquellen aufgedeckt, die alle erst durch Messung sichtbar wurden — keine
davon war vorher plausibel zu erraten. Die mittlere Pixeldifferenz zwischen den
beiden Läufen (Schwelle 1,0) je Ausbaustufe:

| Position | Paar 1 | Paar 2 | Paar 3 | Paar 4 | Paar 5 | Paar 6 | Paar 7 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `01_copper_set_spear` | 4,009 | 8,182 | 0,001 | bytegl. | 0,001 | bytegl. | 0,001 |
| `02_mixed_ember_thorn` | 10,629 | 8,182 | 0,003 | bytegl. | 0,003 | bytegl. | 0,001 |
| `03_iron_axe_harvest` | 19,935 | 13,492 | 15,011 | bytegl. | 0,010 | bytegl. | 0,011 |
| `04_boden_leer` | bytegl. | 0,031 | bytegl. | bytegl. | bytegl. | bytegl. | bytegl. |

Paar 4 bis 6 liefen mit identischem Spielstand; dazwischen wurden nur die
Kamerafelder des Reports ergänzt (Abschnitt 2.5) und der Referenzvergleich
hinzugefügt (Abschnitt 4.1), nichts an der Darstellung. Paar 7 lief nach der
Shaderreparatur aus G-000 (Abschnitt 6) — die Bildinhalte sind dort andere, die
Wiederholbarkeit ist unverändert.

**Quelle 1 — laufende Spielzeit.** Die Sprite-Animationen der Figur und die
Positionsinterpolation liefen zum Aufnahmezeitpunkt weiter, sodass zwei Läufe
verschiedene Animationsphasen erwischten. Behoben durch
`Time.captureFramerate = 60` (macht die Simulation framegetaktet statt
wanduhrgetaktet) und `Time.timeScale = 0` unmittelbar vor jeder Aufnahme.
Aus demselben Grund ist der Anlauf als Framezahl formuliert und nicht als
Sekundenzahl: eine Sekundenzahl hätte je nach Maschinenlast eine andere Zahl
von Simulationsschritten bedeutet.

**Quelle 2 — driftende Spielerposition.** Der Spieler stand in beiden Läufen
nicht exakt gleich. Behoben durch feste Weltkoordinaten (`Stand01`, `Stand02`),
die vor der Aufnahme erneut gesetzt werden, gefolgt von
`Physics.SyncTransforms()`.

**Quelle 3 — prozedurale Zonenerzeugung.** Nach der Behebung von Quelle 1 und 2
zeigten die Bilder identische Kamera und identischen Spieler, aber **anderes
Gelände**. `GameSession.StartNewGame()` ruft `ZoneStates.Reset()` ohne Argument
auf; ohne Saat zieht `ZoneStateService` seinen Zufallsstrom aus der Uhr. Behoben
durch einen anschließenden Aufruf mit fester Weltsaat (`WorldSeed = 20491`).
Der Aufruf muss **nach** `StartNewGame()` stehen, weil dieses selbst zurücksetzt.

**Quelle 4 — Objektauswahl ohne feste Reihenfolge.** Position 03 blieb auch
danach abweichend. Die Diagnoseausgabe zeigte die Kamera bei `(-28,66 | 11,43)`
gegen `(-5,30 | -22,25)`: Es wurde je Lauf ein **anderer Baum** aufgenommen.
`FindObjectsByType` sichert keine Reihenfolge zu, und `FirstOrDefault` nahm
darum einen beliebigen Treffer. Behoben durch `OrderBy` auf den Objektnamen
(die `NodeInstanceId` des Zonengenerators, bei fester Weltsaat stabil) vor der
Auswahl. Die Anlaufrichtung zum Baum wurde zugleich von einer aus der
Spielerposition abgeleiteten auf eine konstante, aus der Kamerarotation
abgeleitete Richtung umgestellt.

Die Reihenfolge ist bezeichnend: Jede Behebung machte erst die nächste Quelle
sichtbar. Insbesondere wurde Quelle 3 nur gefunden, weil die Bilder **angesehen**
und nicht weiter über Zahlen interpretiert wurden — Lauf-Paar 2 sah nach einer
Verschlechterung aus, war aber der Nachweis, dass Quelle 1 und 2 erledigt waren
und etwas Größeres darunter lag.

**Was bleibt:** Bytegleichheit ist *nicht* zugesichert. Paar 4 und 6 lieferten
sie für alle vier Positionen, Paar 5 mit demselben Spielstand nur für
`04_boden_leer`. Es bleibt also ein Rest in der Größenordnung 0,001–0,010
mittlerer Pixeldifferenz, der von Lauf zu Lauf auftreten kann oder nicht. Das
Abnahmekriterium lautet „visuell deckungsgleich", und dieser Rest liegt zwei
Größenordnungen unter der Vergleichsschwelle von 1,0 — sichtbar ist er nicht.
Die Strecke prüft deshalb bewusst gegen einen Schwellwert und nicht gegen einen
Hash. Wer Bytegleichheit erzwingen will, müsste die verbleibende Quelle
(vermutlich Treiber- bzw. GPU-Seite) erst noch bestimmen; für G-001 war das
nicht nötig.

## 3. Abnahmekriterien

Nachgewiesen durch den Doppellauf vom 5. August 2026, 21:56–22:00 Uhr:
`.\Tools\G001-capture.ps1` baut den Player einmal und lässt ihn danach
**zweimal ohne jede Änderung dazwischen** laufen. Belege sind
`G001-Captures/Lauf1` und `G001-Captures/Lauf2` mit je vier PNG und einem
Report (`generatedUtc` 2026-08-05T20:00:14Z).

Dies ist die **Nachprüfung nach der Shaderreparatur aus G-000** (Abschnitt 6).
Ein erster vollständiger Nachweis lag um 21:12 Uhr vor; er ist durch die
Reparatur überholt, weil sie die Darstellung verändert hat. Die Strecke selbst
war davon nicht betroffen — die Kriterien sind mit denselben Mechanismen
erneut erfüllt, nur mit anderen Bildinhalten.

| # | Kriterium | Ergebnis |
| --- | --- | --- |
| 1 | Jeder Lauf erzeugt je Position genau ein PNG | **erfüllt** |
| 2 | Kein Bild einfarbig, automatisiert geprüft und im Report festgehalten | **erfüllt** |
| 3 | Aufnahme nach dem ersten vollständig gerenderten Frame | **erfüllt** |
| 4 | Positionen benannt, versioniert, über Läufe identisch | **erfüllt** |
| 5 | Zwei Läufe ohne Änderung liefern deckungsgleiche Bilder | **erfüllt** |
| 6 | Report nennt Szene, Auflösung, Positionen, Zeitstempel, Buildstand | **erfüllt** |
| 7 | Die drei `VisualFixSmokeFinal`-Positionen bleiben erhalten | **erfüllt** |
| 8 | Mindestens eine Position zeigt leere Bodenfläche ohne Objekte | **erfüllt** |

### Zu 1 — genau ein PNG je Position

Beide Läufe melden `Positionen: 4`, beide Verzeichnisse enthalten vier PNG mit
identischen Dateinamen. Der Treiber bricht ab, wenn die Anzahl abweicht; der
Runner bricht ab, wenn eine Datei fehlt oder kleiner als 1000 Byte ist.

### Zu 2 — kein Bild einfarbig

Gemessene Helligkeitsstreuung, in beiden Läufen gleich:

| Position | stdDev Lauf 1 | stdDev Lauf 2 | Schwelle | Urteil |
| --- | ---: | ---: | ---: | --- |
| `01_copper_set_spear` | 18,06 | 18,06 | 3,0 | nicht einfarbig |
| `02_mixed_ember_thorn` | 17,24 | 17,24 | 3,0 | nicht einfarbig |
| `03_iron_axe_harvest` | 28,05 | 28,05 | 3,0 | nicht einfarbig |
| `04_boden_leer` | 8,05 | 8,05 | 3,0 | nicht einfarbig |

Die Werte stehen als `brightnessStdDev` und `isMonochrome` je Position im
Report — das Kriterium verlangt genau diese Festhaltung. Der niedrigste Wert
ist `04_boden_leer` mit 8,05: erwartbar, weil dort nur Bodentextur im Bild ist.
Vor der Shader-Reparatur aus Abschnitt 1.1 lag dieselbe Position bei einer
vollständig weißen Fläche. Zum Vergleich: Das schwarze `daggers_south.bmp` aus
dem Auftragstext hätte 0,0.

### Zu 3 — Aufnahme nach dem fertigen Frame

Drei voneinander unabhängige Belege:

1. **Aufnahmeweg.** `ScreenCapture.CaptureScreenshotAsTexture()` nach
   `WaitForEndOfFrame` liest den präsentierten Backbuffer. Ein noch nicht
   fertiger Frame ist auf diesem Weg nicht erreichbar.
2. **Sichtbarer Beleg im Bild.** Das „Development Build"-Wasserzeichen steht in
   allen vier PNG unten rechts. Dieses Overlay zeichnet der Player als letztes
   im Frame — es kann nur in einem abgeschlossenen Frame enthalten sein.
3. **Stabilitätsnachweis.** Der Report führt je Position `viewStable: true`.
   Alle vier Positionen wurden nach 59 Frames (0,98–0,99 s) stabil gemeldet,
   also nach 30 aufeinanderfolgenden unveränderten Vergleichsaufnahmen. Kein
   Zeitlimit hat gegriffen.

### Zu 4 — benannt, versioniert, identisch

Positionssatz `g001-v2`, in beiden Reports gleich. Die Kamerawerte stehen seit
diesem Lauf im Report selbst, sodass die Gleichheit prüfbar ist statt
behauptet — `Schritt 3b` des Treibers vergleicht sie und bricht bei Abweichung
ab, **bevor** ein Pixel verglichen wird:

| ID | Kameraposition (x, y, z) | Rotation | Ortho |
| --- | --- | --- | ---: |
| `01_copper_set_spear` | −10,2200 / 10,0100 / 6,7800 | 52 / 45 / 0 | 2,8 |
| `02_mixed_ember_thorn` | −10,2200 / 10,0100 / 6,7800 | 52 / 45 / 0 | 2,8 |
| `03_iron_axe_harvest` | −36,1289 / 10,0100 / 24,1874 | 52 / 45 / 0 | 2,8 |
| `04_boden_leer` | −13,0000 / 23,6500 / −7,0000 | 52 / 45 / 0 | 6,0 |

Alle vier: `OK identisch`. Position 03 ist der aussagekräftigste Fall — sie
wird nicht als Konstante gesetzt, sondern aus dem ausgewählten Baum berechnet,
und trifft trotzdem in beiden Läufen auf vier Nachkommastellen denselben Punkt.
Das ist der Nachweis, dass Weltsaat und Objektsortierung aus Abschnitt 2.6
greifen.

### Zu 5 — zwei Läufe, deckungsgleiche Bilder

Zwischen den beiden Läufen wurde nichts geändert; sie stammen aus demselben
Aufruf desselben Skripts gegen denselben Build.

| Position | mittlere Pixeldifferenz | Schwelle | Urteil |
| --- | ---: | ---: | --- |
| `01_copper_set_spear` | 0,001 | 1,0 | deckungsgleich |
| `02_mixed_ember_thorn` | 0,001 | 1,0 | deckungsgleich |
| `03_iron_axe_harvest` | 0,011 | 1,0 | deckungsgleich |
| `04_boden_leer` | bytegleich | — | identisch |

Die Einschränkung dazu steht in Abschnitt 2.6: Bytegleichheit tritt auf, ist
aber **nicht zugesichert**. Zwei der sieben Durchgänge lieferten sie für alle
vier Positionen, dieser hier nur für `04_boden_leer`. Das Kriterium lautet
„visuell deckungsgleich"; der Rest liegt zwei Größenordnungen unter der
Vergleichsschwelle.

### Zu 6 — Reportinhalt

`g001-capture-report.json`, Werte aus Lauf 1:

| Gefordert | Feld | Wert |
| --- | --- | --- |
| Szene | `scene` | `Zone_Greenwood` |
| Auflösung | `width` / `height` | 1920 × 1080 |
| Jede Kameraposition | `cameraPosition`, `cameraRotationEuler`, `orthographicSize` | siehe Tabelle zu 4 |
| Zeitstempel | `generatedUtc`, je Position `capturedUtc` | `2026-08-05T18:51:14.3112309Z` bzw. je Aufnahme |
| Buildstand | `buildVersion` | `0.2.0-dev` |

Zusätzlich je Position: `positionId`, `description`, `file`,
`brightnessStdDev`, `isMonochrome`, `viewStable` — und für den Satz als Ganzes
`positionSetVersion`.

### Zu 7 — die drei Vergleichspositionen

`01_copper_set_spear`, `02_mixed_ember_thorn` und `03_iron_axe_harvest`
übernehmen Motiv und Zweck der drei `VisualFixSmokeFinal`-Captures und bleiben
damit als Vergleichsbasis erhalten. Die alten Bilder wurden nicht gelöscht.

Zwei ehrliche Einschränkungen dazu:

- Die Standorte sind gegenüber `g001-v1` auf feste Weltkoordinaten umgestellt.
  Das war für Kriterium 5 unvermeidbar. Deshalb die Versionskennung: Bilder aus
  `g001-v1` sind Vergleichsbasis *dem Motiv nach*, nicht pixelweise.
- Die Ausrüstung, nach der die Positionen 01 und 02 benannt sind, war in der
  ersten Fassung dieser Abnahme **nicht ablesbar**: Beide zeigten die Figur in
  einem schwarzen Rechteck mit Streifen aus benachbarten Atlas-Kacheln. Seit
  der Shaderreparatur aus G-000 ist sie es — auf `01_copper_set_spear` sind
  Kupfersatz und Speer klar erkennbar (Abschnitt 6). Der Vorbehalt entfällt
  damit; er steht hier nur, weil er die Bewertung der ersten Fassung erklärt.

### Zu 8 — leere Bodenfläche für G-002

`04_boden_leer` bei (−13 / 23,65 / −7), Ortho 6,0: Spieler und alle
`ResourceNode`-Objekte sind für die Aufnahme abgeschaltet, danach
wiederhergestellt. Das Bild zeigt ausschließlich Bodentextur, keine Objekte.
Mit stdDev 8,05 ist die Fläche eindeutig strukturiert und damit messbar —
genau das, was G-002 als Ausgangspunkt braucht. Vor der Shader-Reparatur war
diese Position vollständig weiß und für G-002 wertlos.

## 4. Verwendung

```powershell
# Vollständig: Build, zwei Läufe, Vergleich
.\Tools\G001-capture.ps1

# Ohne Build, gegen den vorhandenen Player
.\Tools\G001-capture.ps1 -SkipBuild

# Zusätzlich gegen einen früheren Stand vergleichen
.\Tools\G001-capture.ps1 -Baseline .\G001-Captures\Referenz
```

Die Ausgabe landet unter `G001-Captures/Lauf1` und `G001-Captures/Lauf2`.

### 4.1 Vorher/Nachher um eine Änderung herum

Der Vergleich Lauf 1 gegen Lauf 2 zeigt nur, dass die Strecke stabil ist. Ob
eine Änderung am Spiel gewirkt hat — und ob sie **nur dort** gewirkt hat, wo
sie sollte — zeigt erst der Vergleich gegen einen früheren Stand. Genau das ist
der Regressionsschutz, für den der Auftrag die Strecke vorsieht.

```powershell
# 1. Vor der Änderung: Stand sichern
.\Tools\G001-capture.ps1
Copy-Item -Recurse .\G001-Captures\Lauf1 .\G001-Captures\Referenz

# 2. Änderung am Spiel vornehmen

# 3. Nach der Änderung: erneut laufen und vergleichen
.\Tools\G001-capture.ps1 -Baseline .\G001-Captures\Referenz
```

Schritt 4b weist dann je Position `unveraendert` oder `VERAENDERT` mit
Pixeldifferenz aus, ebenso neu hinzugekommene und entfallene Positionen.

**Eine Abweichung gilt dort ausdrücklich nicht als Fehler.** Wer den Boden
ändert, erwartet ein anderes Bild; das Skript urteilt deshalb an dieser Stelle
nicht, sondern legt offen, was sich geändert hat. Der Exit-Code bleibt allein
vom Abnahmekriterium „zwei Läufe deckungsgleich" bestimmt. Weicht der
Positionssatz der Referenz vom aktuellen ab, wird gewarnt — die Bilder zeigen
dann andere Ausschnitte und die Pixeldifferenz ist nicht als Änderung am Spiel
lesbar.

Für G-002 heißt das konkret: `04_boden_leer` **soll** sich ändern, die drei
Figurenpositionen nur insoweit, wie der Boden in ihnen sichtbar ist.

`G001-Captures/Referenz` liegt bereits vor und enthält den hier abgenommenen
Stand. G-002 kann also ohne Vorlauf direkt mit
`.\Tools\G001-capture.ps1 -Baseline .\G001-Captures\Referenz` arbeiten und
sieht sofort, was die eigene Änderung bewirkt hat.

## 5. Befunde außerhalb des Auftragsumfangs

Beim Ansehen der Bilder sind zwei Dinge aufgefallen, die **nicht** zu G-001
gehören und deshalb bewusst nicht angefasst wurden. Sie stehen hier, damit sie
nicht ein zweites Mal gesucht werden müssen.

**Sprites wurden mit harten schwarzen Rechtecken gezeichnet.** ~~Offen~~ —
**erledigt durch G-000.** Figur und Bäume zeigten ihre Alpha-Kante als
schwarzen Kasten, aus derselben Ursache wie in Abschnitt 1.1: Die Shader für
Figur und Bodenschatten waren ebenfalls Dekompilat-Platzhalter mit
`RenderType = Opaque` und ohne `Blend`-Zustand, die Alpha schlicht ignorierten.

G-000 hat beide neu geschrieben (`Assets/_Game/Shaders/Actors/`,
`G000_ABNAHME.md` Abschnitt 4a). Der Figurenshader wog dabei am schwersten: Die
Attrappe verwendete zwei von fünfzehn deklarierten Eigenschaften. Die Wirkung
auf die Captures ist in Abschnitt 6 gemessen.

**Ein zweiter Agent arbeitet zeitgleich im selben Projektordner.** *(Es war der
G-000-Lauf, siehe Abschnitt 6.)* Während der
Arbeit an G-001 sind Dateien unter `Assets/_Game/Editor/Model3D/`,
`Tools/Blender/figure_scale.json` und
`Documentation/PLAN_3D_PROTOTYP_EQUIPMENT.md` aus einem parallelen
`codex`-Lauf dazugekommen. Unity erlaubt nur eine Instanz je Projekt; dadurch
entstanden ein `Aborting batchmode`-Abbruch und ein Kompilierfehler `CS0234`,
der nichts mit den hier beschriebenen Änderungen zu tun hatte. Die Läufe dieser
Abnahme warten deshalb auf `Temp/UnityLockfile`, statt es zu entfernen.

## 6. Nachprüfung nach der Shaderreparatur aus G-000

Nach dem ersten vollständigen Nachweis (21:12 Uhr) hat G-000 die in
Abschnitt 1.1 gefundene Fehlerklasse aufgegriffen und **alle 44 Shader-Attrappen
behandelt**: 41 Paketshader wieder den echten URP-Shadern zugeordnet
(170 Materialien), die drei projekteigenen an ihre Originalpfade gelegt und
zwei davon neu geschrieben.

Das ändert die Darstellung grundlegend und macht die damals gemessenen Werte
ungültig. Die Strecke wurde deshalb um 21:56 Uhr erneut vollständig
durchlaufen — mit dem alten Stand als Referenz, also genau in dem
Vorher/Nachher-Modus aus Abschnitt 4.1. Das ist zugleich dessen erster
Einsatz an einer echten Änderung.

| Position | vorher | nachher | Differenz zur Referenz |
| --- | ---: | ---: | ---: |
| `01_copper_set_spear` | stdDev 20,26 | 18,06 | **2,799 verändert** |
| `02_mixed_ember_thorn` | stdDev 20,03 | 17,24 | **2,613 verändert** |
| `03_iron_axe_harvest` | stdDev 30,99 | 28,05 | **4,191 verändert** |
| `04_boden_leer` | stdDev 8,05 | 8,05 | 0,000 unverändert |

**Das Muster ist der eigentliche Befund.** G-000 hat die Shader für Figur und
Bodenschatten neu geschrieben — verändert haben sich exakt die drei Positionen
mit Figur und Bäumen. `04_boden_leer` enthält keine Akteure und ist auf vier
Nachkommastellen **unverändert**, obwohl derselbe Durchgang 170 Materialien
umgestellt hat. Zwei Dinge sind damit unabhängig voneinander belegt:

- Die Reparatur wirkte dort, wo sie sollte, und **nur** dort.
- Die Bodenfläche, auf der G-002 messen wird, ist von ihr nicht berührt. Der
  Ausgangswert für G-002 bleibt stdDev 8,05.

Sichtbar ist die Wirkung deutlich: Die harten schwarzen Rechtecke um Figur,
Bäume und Büsche sind verschwunden, Alpha wird korrekt ausgewertet. Die
gesunkene Helligkeitsstreuung passt dazu — die extremen Schwarzkanten fielen
weg. Auf `01_copper_set_spear` sind Kupfersatz und Speer jetzt klar erkennbar,
womit auch der Vorbehalt aus Abschnitt 3 „Zu 7" entfällt.

**Alle acht Abnahmekriterien sind gegen den neuen Stand erneut erfüllt.** Kein
Mechanismus der Strecke musste dafür angepasst werden — geändert haben sich nur
die Bildinhalte und die daraus gemessenen Werte.

`G001-Captures/Referenz` wurde anschließend auf diesen Stand aktualisiert; die
alten Bilder waren als Vergleichsbasis für G-002 wertlos geworden.

**Ein Befund für G-006 nebenbei:** Auf `03_iron_axe_harvest` trägt die Figur
sichtbar mehrere Waffen gleichzeitig. Das ist genau die Einfachbelegung, die
G-006 behandelt — vorher war es unter den schwarzen Kästen nicht erkennbar.
