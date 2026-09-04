# Truhenfreiraum in der Zonengenerierung (Entwurf)

**Stand:** 16. August 2026
**Betroffene Dateien:** `Assets/_Game/Scripts/Core/World/ZoneLayoutGenerator.cs`,
`Assets/_Game/Editor/Tests/ZoneGenerationTests.cs`
**Sicherung vorab:** `Backups/truhenfreiraum_20260816/`

---

## 1. Befund

`ZoneLayoutGenerator.Generate` legt die Sperrliste `occupied` leer an und füllt
sie ausschliesslich mit Ressourcen- und Eidra-Platzierungen. Die in der
`ZoneDefinition` hinterlegten `WorldChestSpawnPoints` kommen nie hinein.

`IsFree` prüft dadurch nur den Abstand zu anderen Knoten (`PreferredSpacing`
3,25 m, relaxiert bis `MinimumSpacing` 1,6 m) sowie `space.IsPlaceable(candidate,
1.1f)`. Ein Ressourcenknoten kann also unmittelbar auf oder neben einer
Welttruhe landen.

Folgen:

| Wirkung | Grösse | Stelle |
| --- | --- | --- |
| Interaktionsradius der Truhe überdeckt den Knoten | 2,5 m | `WorldChestContainer.cs:21` |
| Objekte stehen sichtbar ineinander | Knotenfreiraum 1,1 m | `ZoneLayoutGenerator.cs:156` |
| Wächter einer bewachten Truhe zusätzlich | Elite 3,2 m, Begleiter 3,08 m | `ZoneWorldChestPopulator.cs:77` |

Das war der Auslöser des sporadischen PlayMode-Fehlschlags
`InteractionIntegrationTests.CopperVein_Hold225SecondsCollectsExactlyOnce`:
`zone_ember_ruins.world_chest.guarded.01` zog das Ziel vom Kupfervorkommen weg.
Zielauswahl und Test sind bereits gehärtet; die Weltgenerierung blieb bewusst
unangetastet und wird hier nachgezogen.

### 1.1 Die Zahlen der betroffenen Zonen

Sieben Zonen authorieren Truhenpunkte, und zwar in allen sieben **dieselben
zehn**: `x ∈ [−30, 30]`, `z ∈ [−27, 26]`, `y = 0,12`.

| Zone | Truhenpunkte | Wirtschaft | Neben | Eidra-Allokationen |
| --- | --- | --- | --- | --- |
| Zone_Greenwood, Zone_Marsh, Zone_Quarry, Zone_EmberRuins | 10 | 72 | 6 | 2 |
| Zone_GreyRifts, Zone_TwilightGrove, Zone_VeilMarsh | 10 | 72 | 6 | bis 6 |
| Zone_HomeBase, Zone_EidraForge | 0 | — | — | — |

Zone_EmberRuins verteilt damit 80 Platzierungen. Die Fläche ist im EditMode-Test
68 × 68 m (`ZoneGenerationTests.cs:253`), im Spiel `WalkableGround.bounds`
abzüglich 12 m je Achse (`ZoneResourcePopulator.cs:88`).

Jede Zone mit Punkten besitzt auch ein `worldChestLootProfile`. Ein Zusatzcheck
gegen das Profil erübrigt sich deshalb.

### 1.2 Kostet der Freiraum die Fläche?

Nein. Eine Simulation des Algorithmus über 400 Läufe mit 80 Platzierungen auf
68 × 68 m — einschliesslich pessimistisch angesetzter Spawnpunkt-Sperren von
5 m — landet selbst bei einem Truhenradius von **6 m** in keinem einzigen Fall
im `GridFallback`. Die Fläche ist rund sechsfach überdimensioniert.

Der im Auftrag genannte `InvalidOperationException`-Pfad
(`ZoneLayoutGenerator.cs:143`) bleibt trotzdem eine reale Sorge, weil die
Simulation die echten Prop-Collider nicht abbilden kann, über die
`ZonePhysicsPlacementSpace.IsPlaceable` stolpert. Dafür ist das Notventil in
Abschnitt 2.4 vorgesehen.

---

## 2. Entwurf

### 2.1 Datenfluss

`Generate` liest zusätzlich `zone.WorldChestSpawnPoints` und baut daraus eine
reine Positionsliste. Diese wandert **getrennt von `occupied`** durch
`AppendAllocations` → `ResolvePosition` → `IsFree` sowie durch `AppendEidra` und
`GridFallback`.

Getrennt, nicht in `occupied` hineingemischt, weil die beiden Radien
unterschiedlich gross sind und unterschiedlich nachgeben: Knotenabstände
relaxieren von 3,25 m auf 1,6 m herunter, der Truhenfreiraum bleibt fest.

Gesperrt werden **alle zehn authorierten Punkte**, nicht nur die 2–6, die
`WorldChestPopulationGenerator.Generate` tatsächlich bestückt. Damit bleibt
`ZoneLayoutGenerator` unabhängig vom Truhengenerator und von dessen abgeleitetem
Seed, und die Sperre hält auch dann, wenn die Truhenauswahl später neu gewürfelt
wird. Laut Abschnitt 1.2 kostet die Grosszügigkeit nichts.

### 2.2 Zwei Konstanten

```csharp
private const float WorldChestClearance = 3.6f;
private const float WorldChestMinimumClearance = 2.5f;
```

`WorldChestClearance` = Interaktionsradius 2,5 m + Knotenfreiraum 1,1 m. Derselbe
Wert deckt zugleich den gesamten Wächterkranz ab, der bei 3,2 m endet. Kein
Knotenmittelpunkt liegt danach mehr im Zugriff einer Truhe, und keine
Knotengeometrie steht mehr in einem Wächter.

`WorldChestMinimumClearance` = der blosse Interaktionsradius. Er kommt
ausschliesslich im Rasternotfall zum Zuge (2.4).

### 2.3 Abstand flach in XZ

Knoten gegen Knoten bleibt unverändert bei `sqrMagnitude` — alle Knoten liegen
ohnehin auf `area.center.y`. Der Truhenabstand wird flach in XZ gemessen: die
Punkte sind auf `y = 0,12` authoriert, `area.center.y` ist im Spiel dagegen die
Oberkante des Bodens. Dieselbe Konvention benutzen `InteractionSession.cs:57`
(`InteractionUtility.FlatDistance`) und der Spawnpunkt-Check in
`ZonePhysicsPlacementSpace`.

### 2.4 Notventil im GridFallback

`GridFallback` würfelt seinen Startversatz **einmal** und benutzt ihn für beide
Durchgänge. Damit bleibt der Zufallsstrom identisch, gleichgültig ob der zweite
Durchgang überhaupt läuft.

1. 24 × 24-Raster mit vollem Truhenfreiraum (3,6 m).
2. Bleibt das leer: derselbe Raster mit `WorldChestMinimumClearance` (2,5 m).
3. Erst danach die bisherige `InvalidOperationException`, deren Text um den
   Truhenfreiraum ergänzt wird.

Die Garantie gibt im Notfall also von „nichts steht im Wächterkranz" auf „nichts
steht in Truhenreichweite" nach, statt die Zone abstürzen zu lassen.

### 2.5 Was sich verschiebt

Alle Layouts verändern sich, weil die zusätzlichen Ablehnungen den Zufallsstrom
anders verbrauchen. Das ist unvermeidlich und der Zweck der Übung. Unverändert
bleiben: Determinismus (gleicher Seed → gleiche Karte), Knotenzahlen, Mischung
45/18/6/3, Weizensamen-Regel und die Instanz-IDs, die als Save-Keys dienen.

`GeneratorVersion` bleibt bei 2. Die Konstante wird von keiner Stelle gelesen —
`ZoneResourcePopulator`, `ZoneWorldChestPopulator` und die Tests tragen überall
literale `2`. Ein Hochzählen wäre reine Kosmetik mit Desync-Risiko.

---

## 3. Prüfung

Neuer EditMode-Test `NoNodeStandsInAWorldChestsReach` in `ZoneGenerationTests`:
über alle sieben Zonen mit Truhenpunkten und mehrere Seeds hält jede Knoten-
**und** jede Eidra-Position flach mindestens `WorldChestClearance` zu jedem
authorierten Punkt.

Die Nebenwirkungen decken die bestehenden Tests ab:
`TwoRunsWithTheSameSeed_ProduceIdenticalMaps`,
`EveryGeneratedZone_Carries72EconomyNodesAndItsSideNodes`,
`EveryThirdFiberNode_CarriesAWheatSeed`, `ZoneGeneration_UsesNoUnityRandom`.

Abschliessend die vollständige EditMode- und PlayMode-Suite headless, Ergebnis
ausschliesslich aus XML und Log gelesen. Baseline vom 16. August 2026: EditMode
1103/1103, PlayMode 120 grün und 4 bewusste Skips.

---

## 4. Ergebnis

### 4.1 Rot-Grün

`NoNodeStandsInAWorldChestsReach` vor dem Eingriff: **277 Verstösse** über 35
Layouts (7 Zonen × 5 Seeds), der engste bei 1,68 m — also mitten im
Interaktionsradius. Nach dem Eingriff: keiner.

Das Notventil aus 2.4 wird von keiner realen Zone erreicht. Eine zweite
Simulation zeigt: selbst eine auf 30 × 30 m geschrumpfte Fläche zwingt keine
Platzierung ins Raster, weil die Abstandsrelaxation auf 1,6 m vorher greift. Der
`GridFallback` ist über die Fläche gar nicht erreichbar, sondern nur über
`IZonePlacementSpace.IsPlaceable`, also echte Prop-Collider.

Damit das Notventil nicht ungeprüft bleibt, deckt
`AZoneTooTightForTheFullClearance_GivesItUpInsteadOfThrowing` es direkt ab: eine
synthetische Zone mit einem Truhenpunkt im Mittelpunkt einer 5 × 5 m-Fläche. Der
Eckabstand beträgt 3,54 m und liegt damit **unter** dem vollen Freiraum von
3,6 m — keine einzige Stelle erfüllt ihn, der erste Rasterdurchgang muss also
leer ausgehen. Der Test belegt: die Zone hält ihr Knotenbudget und kein Knoten
rückt näher als 2,5 m.

Gegenprobe: mit entferntem zweitem Durchgang scheitert derselbe Test an der
`InvalidOperationException`.

### 4.2 Suiten

Gelaufen am 16.08.2026 um 16:00 und 16:02 mit dem mitgelieferten Editor,
Ergebnis ausschliesslich aus XML und Log gelesen. Belege liegen als
`TestResults-truhenfreiraum-editmode.xml` und
`TestResults-truhenfreiraum-playmode.xml` im Projektstamm; beide Logs sind frei
von `error CS`.

| Lauf | Ergebnis |
| --- | --- |
| EditMode vollständig | **1105 / 1105**, 0 Fehlschläge, 0 Skips |
| PlayMode vollständig | **121 grün**, 0 Fehlschläge, 3 Skips (124 gesamt) |
| `NoNodeStandsInAWorldChestsReach` | grün |
| `AZoneTooTightForTheFullClearance_GivesItUpInsteadOfThrowing` | grün |
| `CopperVein_Hold225SecondsCollectsExactlyOnce` | grün (2,67 s) |

EditMode 1105 = Baseline 1103 plus die zwei neuen Tests.

**Abweichung von der Baseline bei den Skips.** Erwartet waren vier, gelaufen
sind drei: die beiden Diagnoseläufe der `ItemAufnahmeDiagnoseTests` und der
Capture-Lauf `KreaturenInWelt_Captures`. Der vierte,
`Terrock2D_RendersInGreenwoodUnderTwoLights`, hat sich **nicht** übersprungen,
sondern ist grün durchgelaufen — er braucht ein echtes Grafikgerät und
entscheidet das je Lauf selbst. Im Vorlauf um 15:21 war er noch übersprungen.
Die Zahl der Skips ist bei diesem Test also umgebungsabhängig und taugt nicht
als feste Baseline; die Zahl der Fehlschläge schon.

### 4.3 Offen

Die Zonen-Saat bleibt ungesetzt (`ZoneStateService._seedSource = new Random()`),
die Welt würfelt also weiterhin pro Lauf neu. Dieser Eingriff nimmt der
Würfelei nur die Truhenüberlappung; er ersetzt keine gesetzte Saat.
