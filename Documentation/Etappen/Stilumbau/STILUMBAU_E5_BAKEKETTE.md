# Stilumbau Etappe 5 — Bake-Ketten-Nachtrag Gebäude (Task 1)

Diese Analyse ist rein statisch (Quelltext- und Prefab-YAML-Lektüre); es wurde
**kein Unity-Lauf** durchgeführt und **kein Code geändert**. Alle Befunde sind mit
Datei:Zeile belegt (`Select-String`/`grep` auf lokale Kopien der Dateien). Sie
übernimmt die Struktur von `STILUMBAU_E4_BAKEKETTE.md`/`STILUMBAU_E3B_BAKEKETTE.md`.

Stand: 10.08.2026.

---

## (a) Wer erzeugt die BLD_-Geometrie heute, und schmaler Neubau-Einstieg

### Die 9 BLD_-Prefabs existieren nur unter `Level01/`, ihre alte "Quelle" fehlt

`Assets/_Game/Prefabs/Buildings/Level01/` enthält genau 9 Dateien (Verzeichnislisting):
`BLD_CookingPot_L01`, `BLD_Door_L01`, `BLD_FarmPlot_L01`, `BLD_Floor_L01`,
`BLD_Ropewalk_L01`, `BLD_Sawmill_L01`, `BLD_Smelter_L01`, `BLD_Stonecutter_L01`,
`BLD_Wall_L01`. Jede trägt tatsächlich echte, handgebaute 3D-Geometrie unter einem
Kindobjekt `Geometry_A14` (bestätigt z. B. `BLD_Wall_L01.prefab:34`: `m_Name:
Geometry_A14`, gefolgt von `MortarShadow`, `BlockL_0..4`, `BlockR_0..4`, `WallCap`
— echte `MeshFilter`/`MeshRenderer`-Paare, keine Sprites).

### Rollentabelle der sechs genannten Builder

| Builder | Rolle bzgl. BLD_-Geometrie | Beleg |
|---|---|---|
| `WorldVisualAssetBuilder` | Ehemaliger "Bauer" (Sprite-Karten-Ersatz für ein Quellprefab), heute für alle 9 bestehenden BLD_-Prefabs ein **Guard-No-Op** — UND der Quellpfad, den er bräuchte, existiert nicht mehr | `Assets/_Game/Editor/WorldVisualAssetBuilder.cs:59-92` (`BuildBuilding`); Guard `71-74`: `if (AssetDatabase.LoadAssetAtPath<GameObject>(destinationPath) != null) { return; }`; Quellpfad `75`: `"Assets/_Game/Prefabs/Buildings/" + buildingName + ".prefab"` — per Verzeichnislisting **fehlt** dieser Ordner/diese 9 Dateien vollständig (nur `Level01/` existiert) |
| `BuildingCostContentBuilder` | Reine **Datenverdrahtung**: erzeugt/aktualisiert 11 `BuildingCostDefinition`-Assets (Kosten, Footprint-Breite/Tiefe, Prefab-Referenz per Pfad-String); rührt keine Geometrie an | `Assets/_Game/Editor/BuildingCostContentBuilder.cs:79-125` (`Ensure`), `127-138` (`PrefabPath`) |
| `BuildingPreviewContentBuilder` | Baut ein **eigenständiges Ghost-Overlay-System** (3 Texturen, 6 Materialien, 1 Marker-Prefab `BLD_Preview_Mark`) unter `Assets/_Game/Resources/Art/Buildings/` — unabhängig vom BLD_-Mesh, reine Platzierungs-UI | `Assets/_Game/Editor/BuildingPreviewContentBuilder.cs:38-58` |
| `BuildingRoofContentBuilder` | Baut ein **eigenständiges** Dach-Kachel-Prefab `BLD_Roof_Tile` + Material `BLD_Roof` (separates Asset, kein Kind eines BLD_-Prefabs); markiert nur `Wall`/`Door` (Edge-Kind) mit der Marker-Komponente `OcclusionFadeTarget` — keine Mesh-Änderung an den 9 Prefabs | `Assets/_Game/Editor/BuildingRoofContentBuilder.cs:121-146` (`BuildTile`), `148-170` (`MarkEdgePrefabs`) |
| `ProductionContentBuilder` | **Orchestriert** nur: ruft `WorldVisualAssetBuilder.BuildWorldVisuals()` (s. o. No-Op) und `BuildingCostContentBuilder.BuildBuildingCosts()` auf, verdrahtet Gameplay-Komponenten (`WorkbenchController`, `FarmPlotController`) auf die 5 Crafting- bzw. 1 Farm-Prefab — keine Geometrieerzeugung | `Assets/_Game/Editor/ProductionContentBuilder.cs:14-29` |
| `WallConnectionContentBuilder` | **Einziger** der sechs Builder, der tatsächlich Geometrie an den bestehenden BLD_-Prefabs anfügt: 4 Würfel-Teile (`Joint_MinusX/PlusX`, `Cap_MinusX/PlusX`) an `Wall` **und** `Door` (beide `PlacementKind.Edge`), liest das zu teilende Material vom ersten vorhandenen `MeshRenderer` | `Assets/_Game/Editor/WallConnectionContentBuilder.cs:41-72` (`Apply`), `103-114` (`SharedMaterial`) |

**Kernbefund:** Keiner der sechs Builder ist heute der Erbauer der Kern-Geometrie
(`Geometry_A14`-Hierarchie) der 9 BLD_-Prefabs. `WorldVisualAssetBuilder` war es
historisch (Sprite-Karten-Muster wie bei den alten T1-Ressourcenvisuals vor
Etappe 3b), ist aber durch den Existenz-Guard (Zeile 71-74) für alle 9
bestehenden Ziel-Prefabs inert — und selbst ein erzwungener Lauf würde an der
fehlenden Quelle (`Assets/_Game/Prefabs/Buildings/<Name>.prefab`, Zeile 75)
mit `InvalidOperationException` (Zeile 79) scheitern. Das deckt sich mit dem
Plan-Vermerk „Alle 9 Gebäude tragen handgebaute `Geometry_A14`-Geometrie" —
diese wurde außerhalb der heute vorhandenen Builder-Kette angelegt (Bestätigung:
Task-0-Sicherung ist die einzige Quelle laut Mandat).

### Vorschlag: neuer schmaler Builder `EidrenBuildingVisualBuilder`

Nach dem in `STILUMBAU_E4_BAKEKETTE.md` (a) etablierten Muster
(`StyleProofPropBuilder`) und dem in Task 4 des Plans bereits vorgegebenen Namen:

```csharp
// Assets/_Game/Editor/EidrenBuildingVisualBuilder.cs
internal static class EidrenBuildingVisualBuilder
{
    private const string BuildingFolder = "Assets/_Game/Prefabs/Buildings/Level01";
    private const string MeshRoot = "Art/Buildings/Meshes"; // unter Assets/_Game/
    private static int _meshSequenz;

    // Schmaler Einstieg: baut NUR die Geometry_A14-Kindhierarchie der 9
    // bestehenden BLD_-Prefabs neu (in-place ueberschrieben an denselben
    // Level01-Pfaden), ruehrt Collider, NavMeshObstacle, WallConnectionView/
    // Joint_-/Cap_-Kinder, OcclusionFadeTarget, WorkbenchController/
    // FarmPlotController und BuildingInstanceView NICHT an -- diese bleiben
    // Kinder/Komponenten derselben Prefab-Wurzel.
    [MenuItem("Eidren/V0.2/Stilumbau/Gebaeude bauen")]
    public static void BuildStandalone()
    {
        Build();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    internal static void Build()
    {
        EnsureFolder("Assets/_Game/Art/Buildings", "Meshes");
        _meshSequenz = 0;
        BuildWall(); BuildFloor(); BuildDoor(); BuildFarmPlot(); BuildCookingPot();
        BuildSawmill(); BuildSmelter(); BuildStonecutter(); BuildRopewalk();
    }

    // Laedt das BESTEHENDE Prefab (nicht eine kaputte "Quelle"), ersetzt NUR
    // das Kind "Geometry_A14" durch frisch gebaute Fabrik-Geometrie, laesst
    // Wurzel-BoxCollider/NavMeshObstacle/Joint_-Cap_-Kinder/Komponenten unberuehrt.
    private static void RebuildGeometry(string buildingName, Action<Transform> baueTeile)
    {
        string path = BuildingFolder + "/BLD_" + buildingName + "_L01.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform alt = root.transform.Find("Geometry_A14");
            if (alt != null) UnityEngine.Object.DestroyImmediate(alt.gameObject);
            GameObject geometrie = new GameObject("Geometry_A14");
            geometrie.transform.SetParent(root.transform, worldPositionStays: false);
            baueTeile(geometrie.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
```

Signatur: `public static void BuildStandalone()`, keine Parameter, MenuItem
`Eidren/V0.2/Stilumbau/Gebaeude bauen` (identisch mit der bereits im Plan
(Task 4) genannten Bezeichnung). Berührt **ausschließlich** das Kind
`Geometry_A14` der 9 bestehenden `BLD_*_L01.prefab`-Dateien (in-place
überschrieben an denselben Pfaden, GUIDs bleiben stabil — wie
`WallConnectionContentBuilder.Apply`/`BuildingRoofContentBuilder.MarkEdgePrefabs`
es bereits für andere Prefab-Teile vormachen: `LoadPrefabContents` →
verändern → `SaveAsPrefabAsset` an **demselben** Pfad). Nutzt `EidrenMeshFactory`
(`TaperedBox`/`Wedge`/`Loft`) und `EidrenWorldStyleAssets.EnsureWorldMaterial()`
wie alle Fabrik-Builder seit Etappe 1. **Muss vor** `WallConnectionContentBuilder.ApplyPieces()`
laufen (siehe (f) Rebuild-Reihenfolge), weil letzterer beim Lauf den Collider
und das erste vorhandene Material der bereits gebauten Geometrie liest.

---

## (b) Platzierung: Laufzeitbau bestätigt, Zählwerkzeug-Generalisierung

### Kein `PrefabUtility.InstantiatePrefab`, sondern reines `Object.Instantiate` zur Laufzeit

`BuildingPlacementController` (Datei-Suffix `.Continuation.cs`, partial class)
instanziiert platzierte Gebäude ausschließlich über
`UnityEngine.Object.Instantiate(prefab, state.Position, ...)`
(`Assets/_Game/Scripts/Composition/BuildingPlacementController.Continuation.cs:634`,
in `SpawnInstance`, aufgerufen aus `SpawnSavedBuildings` Zeile 617-626 beim
Zonen-Laden aus persistiertem `BuildingInstanceState`). Das ist **kein**
`PrefabUtility.InstantiatePrefab` — es entsteht keine editor-verfolgte
`PrefabInstance`-Verknüpfung, sondern eine reine Laufzeitkopie. `prefab` kommt aus
`_content.TryGetBuilding(state.BuildingId, ...).TryGetLevel(...)`, also aus der
`BuildingCostDefinition.levelPrefabs[0]`-Referenz (BLD_-Asset), nicht aus einer
gebackenen Szenenreferenz.

### Beleg: 0 `BLD_`-Treffer in `HomeBase.unity` (ASCII-Szene)

Wie in `STILUMBAU_E3B_BAKEKETTE.md`(c)/`STILUMBAU_E4_BAKEKETTE.md`(b) beschrieben,
ist `HomeBase.unity` (und `Bootstrap.unity`) ASCII-serialisiert, alle 8
Zonenszenen sind binär. Ein Text-Grep auf den Literal-String `"BLD_"` in
`Assets/_Game/Scenes/HomeBase.unity` ergibt **0 Treffer** — kein einziges
gebackenes Gebäude-Prefab ist dort referenziert (Player-gebaute Gebäude
existieren nur als Laufzeit-`BuildingInstanceState` in der Session, nicht als
Szeneninhalt). Für die 7 binären Außenzonen gilt dieselbe methodische
Einschränkung wie in E3b/E4: kein verlässlicher Text-Negativbeweis möglich,
aber derselbe Codepfad (`SpawnSavedBuildings`/`SpawnInstance`, s. o.) gilt
zonenunabhängig — es gibt keinen Bau-/Platzierungscode, der Gebäude in eine
Szene backt.

**Zusatzbeleg `AuthoredWallHeight()`:** Das Dachsystem liest die Wandhöhe direkt
aus dem PREFAB (nicht aus einer Szeneninstanz):
`Assets/_Game/Scripts/Composition/BuildingPlacementController.Continuation.cs:745-756`
— `_content.TryGetBuilding("building.wall", ...)` liefert den BLD_Wall-Prefab,
dessen `BoxCollider` ausgelesen wird. Bestätigt zusätzlich, dass die "autorisierte"
Wandhöhe eine reine Prefab-Eigenschaft ist, kein Szenenwert.

### Zählwerkzeug-Generalisierung: `StyleProofPlacementReport` → Pfadfilter-Parameter

`StyleProofPlacementReport.CountAndReport()`
(`Assets/_Game/Editor/StyleProofPlacementReport.cs:17-117`) hat den Präfix
hartkodiert: `private const string PrefabFolder = "Assets/_Game/Prefabs/Environment/StyleProof/";`
(Zeile 19), sowie eine feste `ZoneSzenen`-Liste ohne `HomeBase`-... — nein,
`HomeBase.unity` ist bereits enthalten (Zeile 32), aber **ohne** `EidraForge.unity`
und ohne die 12 T1-Varianten-/Node-Ordner. Vorschlag zur Generalisierung
(minimaler Eingriff, kein neues Werkzeug nötig):

```csharp
// Vorschlag: PrefabFolder als Parameter statt Konstante.
[MenuItem("Eidren/V0.2/Stilumbau/Prefab-Platzierungs-Beleg")]
public static void CountAndReportForFolder(string prefabFolderPrefix, string reportSuffix)
{
    // ... identischer Koerper wie CountAndReport() (Zeilen 38-114), aber
    // 'PrefabFolder' (Zeile 19) wird zum Methodenparameter 'prefabFolderPrefix',
    // 'ReportPath' (Zeile 21) haengt 'reportSuffix' an.
}

// Fuer Task 5: CountAndReportForFolder("Assets/_Game/Prefabs/Buildings/Level01/", "gebaeude");
// Erwartung laut (b): 0 Treffer in allen 8 Szenen -- Laufzeitbau bestaetigt.
```

Alternativ (näher am Plan-Wortlaut „Pfadfilter-Parameter ... oder ein
Geschwisterwerkzeug"): eine neue, fast identische Klasse
`BuildingPlacementReport` mit `PrefabFolder = "Assets/_Game/Prefabs/Buildings/Level01/"`
und derselben `ZoneSzenen`-Liste (ggf. + `EidraForge.unity`, da dort ebenfalls
grundsätzlich Gebäude stehen könnten — im Plan nicht ausgeschlossen). Beide
Varianten sind rein lesend (kein `SaveScene`, wie im E4-Vorbild dokumentiert)
und liefern den in Task 5 geforderten Beleg „0 gebackene Instanzen".

---

## (c) Collider-/Footprint-Tabelle der 9 BLD_-Prefabs + Wandvarianten

### Es gibt genau einen `BoxCollider` je Prefab, an der Wurzel

Geprüft per Grep über alle 9 Dateien (`^BoxCollider:`/`m_Size:`/`m_Center:`),
jeweils genau ein Treffer je Prefab (die zweite `m_Center`-Zeile pro Datei gehört
zu einem begleitenden `NavMeshObstacle` mit identischen Werten, z. B.
`BLD_Door_L01.prefab:369-383`, kein zweiter Collider):

| Prefab | `BoxCollider`-Zeile | `m_Size` (Zeile) | `m_Center` (Zeile) | Footprint (Breite×Tiefe, Zellen) | Footprint-Quelle |
|---|---|---|---|---|---|
| `BLD_CookingPot_L01.prefab` | 578 | `(1, 1, 1)` (596) | `(0, 0.5, 0)` (597) | 1×1 | `BuildingCostContentBuilder.cs:71` |
| `BLD_Door_L01.prefab` | 349 | `(1, 2.6, 0.2)` (367) | `(0, 1.3, 0)` (368) | 1×1 | `BuildingCostContentBuilder.cs:42` |
| `BLD_FarmPlot_L01.prefab` | 578 | `(2, 0.15, 2)` (596) | `(0, 0.075, 0)` (597) | 2×2 | `BuildingCostContentBuilder.cs:35` |
| `BLD_Floor_L01.prefab` | 361 | `(1, 0.15, 1)` (379) | `(0, 0.075, 0)` (380) | 1×1 | `BuildingCostContentBuilder.cs:37` |
| `BLD_Ropewalk_L01.prefab` | 434 | `(1, 1.6, 1)` (452) | `(0, 0.8, 0)` (453) | 1×1 | `BuildingCostContentBuilder.cs:59` |
| `BLD_Sawmill_L01.prefab` | 560 | `(1, 2.2, 1)` (578) | `(0, 1.1, 0)` (579) | 1×1 | `BuildingCostContentBuilder.cs:53` |
| `BLD_Smelter_L01.prefab` | 614 | `(1, 2, 1)` (632) | `(0, 1, 0)` (633) | 1×1 | `BuildingCostContentBuilder.cs:47` |
| `BLD_Stonecutter_L01.prefab` | 470 | `(1, 1.4, 1)` (488) | `(0, 0.7, 0)` (489) | 1×1 | `BuildingCostContentBuilder.cs:65` |
| `BLD_Wall_L01.prefab` | 349 | `(1, 2.6, 0.2)` (367) | `(0, 1.3, 0)` (368) | 1×1 (Edge) | `BuildingCostContentBuilder.cs:36` |

Alle Collider-`m_Size.y`-Werte stimmen exakt mit der Sollhöhe aus
`VisualScaleTableBuilder.AddBuildings` überein (siehe (d)) — die Wurzelcollider
sind bereits heute die konsistente Quelle für Höhe **und** Footprint-Tiefe.

### „Wandvarianten" = keine separaten Prefabs, sondern Laufzeit-Sichtbarkeitszustände

Der Plan spricht von „Wand-/Dachvarianten". Es existiert **keine** zweite
`BLD_Wall_*`-Datei — die Wandvarianten sind fünf Formzustände
(`Eidren.Core.BuildGrid.GridWallShape`: `EndCap`, `Straight`, `Corner`, `Tee`,
`Cross`, `Assets/_Game/Scripts/Core/BuildGrid/GridWallShape.cs:3-10`), die
`WallConnectionView.Apply` (`Assets/_Game/Scripts/Interaction/WallConnectionView.cs:36-51`)
zur Laufzeit durch Ein-/Ausschalten von **vier fest im Prefab gebackenen**
Würfel-Kindern realisiert (`Joint_MinusX`/`Joint_PlusX`/`Cap_MinusX`/`Cap_PlusX`,
angelegt von `WallConnectionContentBuilder.Apply`,
`Assets/_Game/Editor/WallConnectionContentBuilder.cs:55-58`). Bestätigt in
**beiden** Edge-Prefabs (`Wall` und `Door`, `PlacementKind.Edge`):

| Prefab | `Joint_MinusX`-Zeile | `Joint_PlusX`-Zeile | `Cap_MinusX`-Zeile | `Cap_PlusX`-Zeile |
|---|---|---|---|---|
| `BLD_Wall_L01.prefab` | 268 | 286 | 304 | 322 |
| `BLD_Door_L01.prefab` | 268 | 286 | 304 | 322 |

Größe/Position dieser 4 Teile (aus `WallConnectionContentBuilder.cs:15-21,55-58`,
abgeleitet vom jeweiligen Wurzel-`BoxCollider`): `Joint_*` = `(0.36, height, 0.36)`
bei `x = ∓0.5`; `Cap_*` = `(0.12, height, 0.4)` bei `x = ∓0.5`; `height`/`centreY`
= `collider.size.y`/`collider.center.y` der Wurzel (für Wall **und** Door
identisch `2.6`/`1.3`, s. o.). **Keines der 4 Teile trägt einen Collider**
(`WallConnectionContentBuilder.Piece`, Zeilen 94-98 entfernt jeden Collider;
`Verify`, Zeilen 175-177, verbietet ihn explizit) — die Fußabdruck-/
Kollisionsaussage kommt ausschließlich vom einen Wurzel-`BoxCollider`.

„Dachvarianten" existieren ebenfalls nicht als Prefab-Set — siehe (f):
`BuildingRoofView` instanziiert zur Laufzeit dieselbe `BLD_Roof_Tile` beliebig
oft je geschlossenem Raum, unabhängig vom Gebäudetyp.

---

## (d) Maßziel-Tabelle: Höhe UND Breite je `visual.building.*`

### Quelle

`VisualScaleTableBuilder.AddBuildings()`
(`Assets/_Game/Editor/VisualScaleTableBuilder.cs:115-130`) definiert Höhe und
Breitenbudget für alle 11 `visual.building.*`-IDs (9 BLD_ + Workbench +
StorageChest). Breite kommt für alle über `FootprintWidth(id)`
(Zeilen 198-218) — liest `BuildingCatalogDefinition.Buildings[...].LevelFootprints[0].Width`
aus dem gebauten Katalog, der wiederum 1:1 aus den `width`-Parametern von
`BuildingCostContentBuilder.Ensure(...)` (Zeilen 21-71) stammt.

| Visual-ID | BLD_-Prefab | Sollhöhe (m) | Zeile | Breitenbudget (m) | Herkunft Breite (Footprint-Zeile) | `ArtworkMissing` |
|---|---|---|---|---|---|---|
| `visual.building.wall` | `BLD_Wall_L01` | 2,6 | 117 | 1,0 | `BuildingCostContentBuilder.cs:36` (Breite 1) | true |
| `visual.building.door` | `BLD_Door_L01` | 2,6 | 118 | 1,0 | `:42` (Breite 1) | true |
| `visual.building.floor` | `BLD_Floor_L01` | 0,0 (GroundPlane) | 120 | 1,0 | `:37` (Breite 1) | true |
| `visual.building.farm_plot` | `BLD_FarmPlot_L01` | 0,0 (GroundPlane) | 122 | 2,0 | `:35` (Breite 2) | true |
| `visual.building.cooking_pot` | `BLD_CookingPot_L01` | 1,0 | 123 | 1,0 | `:71` (Breite 1) | false |
| `visual.building.smelter` | `BLD_Smelter_L01` | 2,0 | 124 | 1,0 | `:47` (Breite 1) | true |
| `visual.building.sawmill` | `BLD_Sawmill_L01` | 2,2 | 125 | 1,0 | `:53` (Breite 1) | true |
| `visual.building.ropewalk` | `BLD_Ropewalk_L01` | 1,6 | 126 | 1,0 | `:59` (Breite 1) | true |
| `visual.building.stonecutter` | `BLD_Stonecutter_L01` | 1,4 | 127 | 1,0 | `:65` (Breite 1) | true |
| (`visual.building.workbench`) | *nicht BLD_* (`Stations/Workbench.prefab`) | 1,1 | 128 | 1,0 | `:25` — außerhalb Scope (Etappe-2-Entscheid) | true |
| (`visual.building.storage_chest`) | *nicht BLD_* (`Stations/StorageChest.prefab`) | 0,8 | 129 | 1,0 | `:30` — außerhalb Scope (Etappe-2-Entscheid) | true |

Für `wall`/`door`/`farm_plot`/`floor` steht `ArtworkMissing: true` — das ist der
Sprite-Ära-Vermerk „Bild fehlt" (z. B. Zeile 117: „Ueberragt den Spieler. Bild
fehlt: quadratisch, gebraucht 1:2,6."), inhaltlich überholt seit die Prefabs
echte `Geometry_A14`-Geometrie tragen; die Höhentoleranz gilt trotzdem
unverändert (siehe unten — der Sprite-Sonderfall in Zeile 93 der Testdatei
greift nur, wenn tatsächlich eine Sprite-Silhouette gemessen wird, was für alle
9 BLD_ nicht der Fall ist, s. `FindGeometryRoot`).

### Tests, Toleranzen, exakte Zitate

**Höhentoleranz — `EveryBakedPrefab_MatchesItsTableHeight`**
(`Assets/_Game/Editor/Tests/VisualScaleTests.cs:82-101`), Kernzeile 94:

```csharp
if (Mathf.Abs(measured - expected) > expected * 0.01f)
```

→ **1 % der Sollhöhe**, gilt für alle `VisualScaleAssetMap.BakedPrefabs`-Einträge,
also auch für alle 9 BLD_. Für `GroundPlane`-Einträge (`floor`, `farm_plot`)
wird stattdessen die Breite (`GroundExtent`) gegen `entry.WidthBudget` geprüft
(Zeile 92-93: `entry.IsGroundPlane ? visual.GroundExtent : visual.Height`).

**Breitenbudget — `EveryBakedPrefab_StaysWithinItsWidthBudget`**
(`VisualScaleTests.cs:103-121`), gefiltert **ausschließlich** auf
`visual.building.*` (Zeile 110: `pair.Key.StartsWith("visual.building.")`),
Kernzeilen 113-116:

```csharp
float measured = visual.HorizontalExtent;
if (measured > entry.WidthBudget * 1.01f)
```

→ Toleranz **+1 %** über dem Budget, Messung = `Max(bounds.size.x, bounds.size.z)`
der Wurzel-Renderer-Bounds (`HorizontalExtent`, Zeile 31). Das ist der einzige
Ort im Testcode, an dem eine Breitenprüfung für Gebäude existiert — für Props
gibt es (wie in E4 (d) festgestellt) keine automatisierte Breitenprüfung.

**Geometrie-Messung — `TryMeasurePrefab`/`FindGeometryRoot`**
(`VisualScaleTests.cs:187-264`): Sucht zuerst ein Kind namens `"Geometry_A14"`
(Zeilen 244-264) und misst dessen `Renderer`-Bounds — genau der Mechanismus, der
für alle 9 heutigen BLD_-Prefabs bereits greift (sie sind keine Sprites). Ein
Neubau mit Fabrik-Geometrie muss **denselben** Kindnamen `Geometry_A14`
beibehalten, sonst greift `FindGeometryRoot` auf die Wurzel selbst zurück statt
korrekt zu messen.

**Spielerhöhen-Bezug — `Wall_OvertopsThePlayer`** (`VisualScaleTests.cs:172-176`):

```csharp
[Test]
public void Wall_OvertopsThePlayer()
{
    AssertPlayerHeightRatio("visual.building.wall", 1f);
}
```

`AssertPlayerHeightRatio` (Zeilen 178-185) berechnet `ratio = visual.Height /
playerHeight` und verlangt `ratio >= minimum` (hier `1f`, kein Maximum). Spieler-
Sollhöhe: `Billboard("visual.player", 2f, ...)` (`VisualScaleTableBuilder.cs:85`)
= **2,0 m**. Mit der Wand-Sollhöhe 2,6 m (s. o.) ergibt sich heute `ratio = 1,3`
— die Wand muss nach dem Umbau **mindestens** genauso hoch bleiben wie der
Spieler (`≥ 2,0 m`); die 1-%-Höhentoleranz oben hält sie de facto exakt bei 2,6 m
und damit klar über der 1,0-Grenze.

---

## (e) Test-Nachzieh-Inventar (`VisualAssetTests.cs`, 5 Methoden)

### Präzise Zuordnung BLD_ vs. handgebaut-per-Entscheid

| Methode | Zeilen | Geprüfte Objekte | BLD_ (nachziehen) | Handgebaut/Etappe-2-Ausnahme (unverändert) |
|---|---|---|---|---|
| `M6BuildingsUseLevelNamedUniqueVisualPrefabs` | 128-165 | Alle 9 `BLD_*_L01` (Zeile 130: `Smelter, Sawmill, Ropewalk, Stonecutter, CookingPot, FarmPlot, Wall, Floor, Door`) + Preview-Materialien + `BuildingCostDefinition`-Prefabpfade | **Alle 9** | — |
| `CookingPotUsesReadableOpenCauldronHeroGeometry` | 205-230 | `BLD_CookingPot_L01.prefab` (Zeile 208) | **CookingPot** (ist BLD_, s. Plan-Hinweis) | — |
| `WorkbenchUsesReadableHandmadeHeroGeometry` | 232-254 | `Assets/_Game/Prefabs/Stations/Workbench.prefab` (Zeile 235) | — | **Workbench** — Etappe-2-Entscheid, bleibt `Geometry_A14`/URP-Lit unverändert |
| `RemainingProductionStationsUseFunctionalHeroGeometry` | 256-286 | 4 Einträge, alle über `"Assets/_Game/Prefabs/Buildings/Level01/BLD_" + name + "_L01.prefab"` (Zeile 272) geladen: `Smelter, Sawmill, Ropewalk, Stonecutter` (Zeile 259-265) | **Alle 4 — sind BLD_-Gebäude, keine separaten Stationsprefabs** (Klärung der Plan-Vorsicht) | — |
| `RemainingSupportAssetsUseLayeredHeroGeometry` | 295-326 | 5 Pfade (Zeile 298-305): `Stations/StorageChest.prefab`, `BLD_FarmPlot_L01`, `BLD_Wall_L01`, `BLD_Floor_L01`, `BLD_Door_L01`; **zusätzlich** separater Aufruf `AssertStoneDepositUsesFactoryGeometry` für `StoneDeposit_Active/_Exhausted` (Zeile 324-325) | **FarmPlot, Wall, Floor, Door** (4 von 5 Pfaden) | **StorageChest** — Etappe-2-Entscheid, unverändert. **StoneDeposit_Active/_Exhausted sind T1-Ressourcen, keine Gebäude** — bereits seit Etappe 3b auf Fabrik-Vertrag umgestellt (Kommentar Zeilen 288-294 bestätigt das explizit), von Task 5 **nicht** berührt |

**Klärung der im Auftrag genannten Unsicherheit „Produktionsstationen":** Der
Methodenname `RemainingProductionStationsUseFunctionalHeroGeometry` prüft
ausschließlich die 4 BLD_-Produktionsgebäude (Smelter/Sawmill/Ropewalk/
Stonecutter) über deren `Level01`-Prefabpfade — **keine** separaten
„Stations"-Prefabs existieren für diese vier (anders als `Workbench`/
`StorageChest`, die tatsächlich unter `Assets/_Game/Prefabs/Stations/` liegen).
Damit zählen alle 9 heutigen BLD_-Level01-Prefabs zu **mindestens einer**
dieser fünf Testmethoden — Deckung ist vollständig:

`Smelter, Sawmill, Ropewalk, Stonecutter` → `RemainingProductionStations...`;
`CookingPot` → eigene Methode; `FarmPlot, Wall, Floor, Door` →
`RemainingSupportAssets...`; **alle 9** zusätzlich generisch in `M6Buildings...`.

### Neue Fabrik-Vertrag-Assertions je adaptierter Methode (E3b-Muster, nicht abschwächen)

Vorbild ist der bereits vollzogene T1-Nachzug in dieser Datei:
`CopperVeinUsesDetailedThreeDimensionalHeroGeometry` (Zeilen 174-203) und die
private Hilfsmethode `AssertStoneDepositUsesFactoryGeometry` (Zeilen 328-349).
Beide ersetzen — bei **unveränderter Prüfabsicht** — drei Assertion-Klassen:

1. „URP/Lit-Shader je Renderer, N distinkte Materialien" →
   **„gemeinsames Material `M_EidrenWorld_VertexLit`"**
   (`renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit")`,
   Muster Zeile 198-199/344-345). **Nicht abschwächen**: die alte
   „≥N distinkte Materialien"-Prüfung (z. B. CookingPot Zeile 223: `≥12`,
   Workbench Zeile 247: `≥8`) entfällt ersatzlos, weil die Fabrik-Architektur
   bewusst **ein** geteiltes Material für alle Teile vorsieht (wie in
   `STILUMBAU_E3B_BAKEKETTE.md` (a) begründet) — das ist keine Abschwächung,
   sondern eine Vertragsänderung, die dem tatsächlichen Baumuster folgt.
2. Benannte Legacy-Teile (z. B. `OpenCauldron`, `HearthStone_0..8`,
   `BlockL_0`/`BlockR_4`/`WallCap`, `WorktopPlank_0`, `FacetedKilnBody` usw.) →
   **neue, dem Task-4-Geometrieplan entsprechende Teilnamen** (Plan-Vorgabe:
   Wand = Plankenlagen+Fachwerk-Streben+Verbinderkanten, Floor = Plankenplatte,
   Door = Rahmen+Türblatt, FarmPlot = Erdbett-Loft+Einfassung, CookingPot =
   Topf-Loft+Gestell, Sawmill/Smelter/Stonecutter/Ropewalk = 6-12-teilige
   Silhouetten). Die **Struktur-Unterscheidungs-Absicht** bleibt erhalten (z. B.
   `alleTeile.Length, Is.Not.EqualTo(...)` bei BerryBush, Zeile 108-109) —
   Task 3 muss für jede Methode mindestens so viele benannte
   Teil-Assertionen wie heute vorsehen, nur mit neuen Namen.
3. Mindest-Teilanzahl/-Vertexzahl (z. B. CookingPot `≥28` MeshFilter/`≥2600`
   Vertices, Wand `≥11`, Workbench `≥34`) → **beibehalten oder erhöhen**, dazu
   **neu**: Vertexfarben-Pflicht je `MeshFilter`
   (`filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount)`,
   Muster Zeile 190-192/339-341) — das ist die zentrale, nicht verhandelbare
   Fabrik-Kontraktbedingung (ohne Vertexfarben ist `M_EidrenWorld_VertexLit`
   funktionslos, siehe `STILUMBAU_E3B_BAKEKETTE.md` (a) Risikoabschnitt zu
   `SilhouetteBranch`).
4. **Neu, spezifisch für Gebäude (nicht im T1/Prop-Muster, weil dort keine
   Collider betroffen waren):** Der Wurzel-`BoxCollider`/`NavMeshObstacle`
   (Tabelle (c)) muss **byte-gleich** bleiben — als zusätzliche Assertion je
   Methode, die ihn heute noch nicht prüft (`M6Buildings...` prüft ihn nicht;
   ein Nachtrag `Assert.That(prefab.GetComponent<BoxCollider>().size, ...)`
   gegen die Werte aus (c) wäre die Ergänzung, die den Maßvertrag aus dem Plan
   („Collider/Footprints/Bauraster byte-genau erhalten") technisch verankert).
5. Für `Wall`/`Door` zusätzlich: `WallConnectionView`-Referenzen und die 4
   `Joint_`/`Cap_`-Kinder (s. (c)/(f)) dürfen durch den Geometrie-Rebuild **nicht**
   verloren gehen — sinnvoll durch `BuildGridWallConnectionTests`/
   `BuildingRoofContentTests` bereits indirekt abgesichert (s. (f)), zusätzlich
   in `RemainingSupportAssetsUseLayeredHeroGeometry` direkt prüfbar.

`WorkbenchUsesReadableHandmadeHeroGeometry` bleibt **unverändert** (keine der
obigen vier Punkte gilt) — Etappe-2-Entscheid bindend.

---

## (f) Dach-/Wandvarianten-Kette: Bezug zu den BLD_-Prefabs, Rebuild-Reihenfolge

### Dach: eigenständiges, gebäudeunabhängiges Laufzeitsystem — kein Sub-Mesh der BLD_-Prefabs

`BuildingRoofView` (`Assets/_Game/Scripts/Presentation/BuildingRoofView.cs`) lädt
zur Laufzeit **einmalig** `BLD_Roof_Tile`/`BLD_Roof` aus `Resources.Load`
(Zeilen 142-143, Pfad `"Art/Buildings/..."`) und instanziiert daraus beliebig
viele Kacheln pro geschlossenem Raum (`Room.Tile`, Zeilen 95-111) — **unabhängig
vom Gebäudetyp**: `AddRoom` (Zeilen 165-175) kennt keine BLD_-Prefab-Referenz,
nur Zellkoordinaten und eine Höhe. Diese Höhe kommt aus `AuthoredWallHeight()`
(`BuildingPlacementController.Continuation.cs:745-756`), die den
`BoxCollider` **des `BLD_Wall_L01`-Prefabs** ausliest — das ist die **einzige**
strukturelle Verbindung zwischen Dach und einem konkreten BLD_-Prefab: die
Dachhöhe hängt an der Wand-Collider-Höhe, nicht an einer Dach-Geometrie im
Wand-Prefab selbst. `BuildingRoofContentBuilder.BuildTile()`
(`Assets/_Game/Editor/BuildingRoofContentBuilder.cs:121-146`) erzeugt
`BLD_Roof_Tile` als **eigenständiges** Prefab unter
`Assets/_Game/Resources/Art/Buildings/`, kein Kind irgendeines BLD_-Prefabs.

`BuildingRoofContentBuilder.MarkEdgePrefabs()` (Zeilen 148-170) fügt den beiden
Edge-Prefabs (`Wall`, `Door`) zusätzlich die Marker-Komponente
`OcclusionFadeTarget` hinzu (leere Markerklasse,
`Assets/_Game/Scripts/Presentation/OcclusionFadeTarget.cs`) — **kein**
Konsument dieser Komponente wurde im rekonstruierten Quellbaum gefunden (Grep
über `Assets/_Game/Scripts` und `Assets/_Game` insgesamt: 0 Treffer außer
Definition und Builder). Für Task 4 heißt das: die Komponente muss trotzdem
erhalten bleiben (per Test abgesichert, s. u.), auch wenn ihr Verwendungszweck
im aktuellen Code nicht sichtbar ist.

### Wand-/Türverbinder: echte Geometrie-Kinder der BLD_-Prefabs (s. (c))

Anders als das Dach sind die 4 `Joint_`/`Cap_`-Würfel **tatsächliche Kinder**
der `Wall`/`Door`-Prefab-Wurzel (siehe (c)), gebaut von
`WallConnectionContentBuilder.Apply` und zur Laufzeit von `WallConnectionView.Apply`
(`Assets/_Game/Scripts/Interaction/WallConnectionView.cs:36-51`) je nach
Nachbarschaftstopologie (`GridWallShape`, s. (c)) sichtbar geschaltet.
`WallConnectionContentBuilder.SharedMaterial` (Zeilen 103-114) sucht das Material
am **ersten vorhandenen, nicht selbst hinzugefügten** Renderer der Prefab-Wurzel
— nach einem Geometrie-Rebuild mit `EidrenBuildingVisualBuilder` (Vorschlag (a))
liefert das deterministisch `M_EidrenWorld_VertexLit` (identisches Muster zum
in `STILUMBAU_E3B_BAKEKETTE.md` (a) beschriebenen `FirstMaterial`-Risiko bei
`SilhouetteBranch` — hier jedoch unkritisch, da die `Joint_`/`Cap_`-Würfel
ohnehin keine eigene Bedeutung als Vertexfarben-Fläche haben und ein einfarbig
gerendertes Verbinderstück optisch plausibel bleibt).

### Rebuild-Reihenfolge (Task 4)

1. **`EidrenBuildingVisualBuilder.BuildStandalone()`** (neu, Vorschlag (a)) —
   ersetzt `Geometry_A14` in allen 9 BLD_-Prefabs; Wurzel-`BoxCollider`/
   `NavMeshObstacle`/vorhandene `Joint_`/`Cap_`-Kinder/Komponenten (`WallConnectionView`,
   `OcclusionFadeTarget`, `WorkbenchController`, `FarmPlotController`,
   `BuildingInstanceView`) bleiben unangetastet, da nur das `Geometry_A14`-Kind
   ersetzt wird.
2. **`WallConnectionContentBuilder.ApplyPieces()`** — läuft idempotent erneut,
   liest jetzt `M_EidrenWorld_VertexLit` als `SharedMaterial` (statt des alten
   Materials); erneuert `Joint_`/`Cap_`-Größe/Position aus dem (unveränderten)
   Wurzel-Collider.
3. **`ProductionContentBuilder.BuildProductionContent()`** (oder direkt
   `BuildingCostContentBuilder.BuildBuildingCosts()`) — rein datenseitig
   idempotent, stellt sicher, dass `levelPrefabs`/`levelFootprints` weiterhin auf
   dieselben (jetzt neu-geometrisierten) Pfade zeigen.
4. **`BuildingRoofContentBuilder.BuildRoofs()`** — erneuert `BLD_Roof_Tile`/
   `BLD_Roof` (unverändert vom Gebäude-Rebuild betroffen) und bestätigt per
   `Verify()` (Zeilen 209-231), dass `Wall`/`Door` weiterhin `OcclusionFadeTarget`
   tragen.

### Testklassen für den Regressionsfilter (Ergänzung zu `VisualAssetTests`)

| Testklasse | Datei | Bezug |
|---|---|---|
| `BuildingRoofContentTests` | `Assets/_Game/Editor/Tests/BuildingRoofContentTests.cs` | Direkt: `TheRoofTileIsAuthoredAndLoadable`/`...ChangesNeitherPhysicsNorNavigation`/`...CoversExactlyOneCellAndLiesFlat`/`TheRoofMaterialCanFade` (Dach-Asset, unbeeinflusst vom Gebäude-Mesh) sowie `EveryEdgePrefabIsAnOcclusionFadeTarget`/`TheFadeMarkLeavesPhysicsAlone` (Zeilen 53-73, prüfen `OcclusionFadeTarget`-Komponente **und** vorhandenen Collider auf `Wall`/`Door` — bricht, falls der Geometrie-Rebuild versehentlich den Wurzel-Collider verliert) |
| `BuildGridWallConnectionTests` | `Assets/_Game/Editor/Tests/BuildGridWallConnectionTests.cs` | Indirekt: reine Topologie-Logik (`GridWallShape`/`GridWallConnection.Classify`), unabhängig von Prefab-Geometrie — vom Plan als „WallConnection-Testklasse" benannt, aber durch den Geometrie-Rebuild strukturell nicht gefährdet; Aufnahme ist Vorsichtsmaßnahme, keine erwartete Regression |
| `VisualScaleTests` | `Assets/_Game/Editor/Tests/VisualScaleTests.cs` | Bereits durch die Maßverträge des Plans abgedeckt (s. (d)) |

Empfehlung für den Kombi-Regressionslauf in Task 4 (zusätzlich zur
Plan-Pflichtangabe `VisualAssetTests`): **`BuildingRoofContentTests`,
`BuildGridWallConnectionTests`, `VisualScaleTests`**.

---

## Zusammenfassung für Task 2/3/4/5

| Frage | Antwort |
|---|---|
| (a) Heutiger Bauer | Niemand baut die `Geometry_A14`-Kerngeometrie der 9 BLD_-Prefabs aktiv; `WorldVisualAssetBuilder` ist dafür geguarded/inert und seine Quelle fehlt. Einzige Geometrie-schreibende Stelle heute: `WallConnectionContentBuilder` (nur 4 Verbinderwürfel an Wall/Door). Vorschlag: neuer `EidrenBuildingVisualBuilder.BuildStandalone()`, MenuItem `Eidren/V0.2/Stilumbau/Gebaeude bauen`, ersetzt nur das `Geometry_A14`-Kind in-place. |
| (b) Platzierung | Bestätigt laufzeit-/sitzungsgebaut (`Object.Instantiate` aus `BuildingInstanceState`, kein `PrefabUtility.InstantiatePrefab`); 0 `BLD_`-Treffer in `HomeBase.unity`. `StyleProofPlacementReport` lässt sich durch einen Pfadfilter-Parameter (oder eine Schwesterklasse) auf `Assets/_Game/Prefabs/Buildings/Level01/` generalisieren; erwartet 0 Treffer in allen Szenen. |
| (c) Collider/Footprint | 9 Wurzel-`BoxCollider` (Tabelle oben), Werte identisch mit den Sollhöhen aus (d). „Wandvarianten" sind keine Extra-Prefabs, sondern 4 fest gebackene, kollisionslose `Joint_`/`Cap_`-Würfel je Wall/Door, laufzeit-sichtbarkeitsgeschaltet nach `GridWallShape`. |
| (d) Maßziele | `VisualScaleTableBuilder.AddBuildings()` (Zeilen 115-130) liefert Höhe+Breite je `visual.building.*`; Höhentoleranz 1 % (`VisualScaleTests.cs:94`), Breitenbudget-Toleranz 1 % nur für Gebäude (`VisualScaleTests.cs:114`), `Wall_OvertopsThePlayer` verlangt Wandhöhe ≥ Spielerhöhe (2,0 m) — heutiger Sollwert 2,6 m erfüllt das mit Marge. |
| (e) Nachzieh-Inventar | 5 Methoden, präzise zugeordnet: `M6Buildings...` (alle 9 BLD_), `CookingPot...` (BLD_), `Workbench...` (Ausnahme, unverändert), `RemainingProductionStations...` (Smelter/Sawmill/Ropewalk/Stonecutter — **sind** BLD_, keine separaten Stationsprefabs), `RemainingSupportAssets...` (FarmPlot/Wall/Floor/Door BLD_ + StorageChest-Ausnahme + StoneDeposit bereits E3b-migriert, unberührt). Neuer Vertrag je Methode: gemeinsames `M_EidrenWorld_VertexLit`-Material statt URP/Lit-Materialvielfalt, Vertexfarben-Pflicht, neue Teilnamen nach Task-4-Geometrieplan, unveränderte/erhöhte Mindestteilzahl, zusätzlich Collider-Byte-Treue. |
| (f) Dach-/Wandkette | Dach ist ein vom Gebäudetyp unabhängiges Laufzeitsystem (`BuildingRoofView` + eigenständiges `BLD_Roof_Tile`), einzige Kopplung ist die aus dem Wand-Collider gelesene Höhe. Wandverbinder sind echte Prefab-Kinder von Wall/Door, Material-Lookup läuft automatisch auf `M_EidrenWorld_VertexLit` um. Rebuild-Reihenfolge: `EidrenBuildingVisualBuilder` → `WallConnectionContentBuilder.ApplyPieces` → `ProductionContentBuilder`/`BuildingCostContentBuilder` → `BuildingRoofContentBuilder.BuildRoofs`. Regressionsfilter zusätzlich zu `VisualAssetTests`: `BuildingRoofContentTests`, `BuildGridWallConnectionTests`, `VisualScaleTests`. |
