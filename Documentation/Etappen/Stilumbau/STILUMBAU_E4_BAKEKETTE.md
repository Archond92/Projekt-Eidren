# Stilumbau Etappe 4 — Bake-Ketten-Nachtrag Props (Task 1)

Diese Analyse ist rein statisch (Quelltext- und Prefab-/Szenen-YAML-Lektüre) plus
AABB-Vermessung der 16 bestehenden `SP_`-Prefabs; es wurde **kein Unity-Lauf**
durchgeführt und **kein Code geändert**. Alle Befunde sind mit Datei:Zeile belegt.
Sie folgt der Struktur von `STILUMBAU_E3B_BAKEKETTE.md`.

Stand: 09.08.2026.

---

## (a) Wer baut die 16 `SP_`-Prefabs heute, und schmaler Einstieg

### Heute baut sie niemand — `StyleProofContentBuilder` validiert nur

Der einzige öffentliche Einstieg ist `StyleProofContentBuilder.BuildAll()`
(`Assets/_Game/Editor/StyleProofContentBuilder.cs:34-49`,
`[MenuItem("Eidren/Art/Build V0.1 Style Proof")]`). `BuildPrefabs()`
(Zeilen 170-197) — der Teil, der scheinbar die 16 Requisiten anlegt — ruft für
jede von ihnen `SaveSpriteProp(name, sprite, visualId)` (Zeilen 220-223) auf, die
wiederum nur `RequireGeometryPrefab(path)` (Zeilen 230-238) aufruft:

```csharp
private static GameObject RequireGeometryPrefab(string path)
{
    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if (prefab == null || prefab.GetComponentInChildren<MeshRenderer>(includeInactive: true) == null
        || prefab.GetComponentInChildren<SpriteRenderer>(includeInactive: true) != null)
    {
        throw new InvalidOperationException("Authored geometry prefab is missing: " + path + ...);
    }
    return prefab;
}
```

Das ist eine **Prüfung**, kein Bau: sie lädt das existierende Prefab und wirft nur,
wenn es fehlt oder noch ein reines Sprite-Prefab ist. Die 16 `SP_`-Prefabs sind
folglich handautorisiert (deckt sich mit dem Vermerk „Original-Handbau!" in
Etappe-4-Task-0). Es gibt aktuell **keinen** Codepfad, der ihre Geometrie
generiert — anders als bei den T1/T2-Ressourcenvisuals, für die
`T1ResourceVisualBuilder`/`T2ResourceVisualBuilder` bereits existieren.

### `BuildAll()` hat erhebliche Nebenwirkungen — und bricht heute zwei Tests

Ein Lauf von `BuildAll()` (Zeilen 34-49) macht zusätzlich zur reinen
Prefab-Prüfung:

| Schritt | Zeilen | Wirkung |
|---|---|---|
| `ConfigureAuthoredTextures` | 61-69 | Reimportiert 6 Texturen, schreibt Spriteatlanten neu |
| `BuildMaterials` | 117-134 | Erzeugt/überschreibt 12 `M_SP_*`-Materialien |
| `BuildResourceVisuals`/`BuildWildlingVisual` | 199-212 | Prüft 4 Ressourcenvisuals + `Wildling_Visual.prefab` (nur Validierung wie oben) |
| `BuildVisualLibrary` | 245-274 | Überschreibt `StyleProofVisualLibrary.asset`; `forestPropPrefabs` verdrahtet nur **12 von 16** Requisiten (Zeilen 261-265 — `SP_GroundCover_Moss`, `SP_RuinWall_B`, `SP_RuinMonument`, `SP_EidrenRune` fehlen in der Liste) |
| `BuildVolume` | 284-321 | Erzeugt/überschreibt `Greenwood_StyleProof_Volume.asset` |
| **`BuildGreenwood`** | 335-367 | **Öffnet `Zone_Greenwood.unity`, löscht/baut den kompletten Knoten `StyleProofReferenceArea` neu** (Boden, Felsgruppen, Baumgruppen, Vegetation, Ruine, NavMesh-Rebuild) und **speichert die Szene** |

`BuildGreenwood` reaktiviert damit exakt den Legacy-Referenzbereich, den zwei
bestehende Tests explizit verbieten:

- `StyleProofContentTests.Greenwood_ContainsCurrentAreaArtPresentation`
  (`Assets/_Game/Editor/Tests/StyleProofContentTests.cs:38`):
  `Assert.That(zoneRoot.transform.Find("StyleProofReferenceArea"), Is.Null,
  "... was removed by the AreaArt migration and must not return.")`
- `PlaceholderCleanupTests.KeineAusgelieferteSzene_EnthaeltEinAltesTesthindernis`
  (`Assets/_Game/Editor/Tests/PlaceholderCleanupTests.cs:32-45`): führt
  `"StyleProofReferenceArea"` in `LegacyPlaceholders` und prüft dessen Abwesenheit
  über alle 9 ausgelieferten Szenen (Zeilen 15-26).

**Ein heutiger Lauf von `BuildAll()` würde beide Tests brechen** — der
Legacy-Referenzbereich wurde durch die AreaArt-Migration (Etappe „G-002" laut
`AreaArtSceneBuilder.RemoveLegacy`, siehe (b)) bewusst entfernt.
`StyleProofContentBuilder.BuildAll()` ist damit für Etappe 4 **nicht** der
richtige Einstieg — weder um die 16 Prefabs zu bauen (das tut er nicht) noch als
Nebeneffekt-Träger (der Nebeneffekt ist eine verbotene Regression).

### Vorschlag: neuer schmaler Builder `StyleProofPropBuilder`

Da `StyleProofContentBuilder` keine wiederverwendbare Baulogik für die 16
Requisiten enthält (nur Validierung), ist die saubere Lösung eine **neue
Klasse**, die dieselbe gemeinsame Infrastruktur nutzt wie
`T1ResourceVisualBuilder.cs` und `T2ResourceVisualBuilder.cs` — nämlich
`EidrenMeshFactory` (`Assets/_Game/Editor/EidrenMeshFactory.cs`) und
`EidrenWorldStyleAssets.EnsureWorldMaterial()`
(`Assets/_Game/Editor/EidrenWorldStyleAssets.cs:16-30`) — und **nichts**
Privates aus `StyleProofContentBuilder` wiederverwendet (dort gibt es nichts
Wiederverwendbares):

```csharp
// Assets/_Game/Editor/StyleProofPropBuilder.cs
internal static class StyleProofPropBuilder
{
    private const string PrefabFolder = "Assets/_Game/Prefabs/Environment/StyleProof";
    private const string MeshRoot = "Assets/_Game/Art/StyleProof/Meshes";
    private static int _meshSequenz;

    // Schmaler Einstieg: baut NUR die 16 SP_-Prop-Prefabs (Meshes + Prefabs),
    // ruehrt Texturen, StyleProof-Materialien, Volume, VisualLibrary oder
    // Zone_Greenwood.unity nicht an.
    [MenuItem("Eidren/V0.2/Stilumbau/Props bauen")]
    public static void BuildStandalone()
    {
        Build();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    internal static void Build()
    {
        EnsureFolder("Assets/_Game/Art/StyleProof", "Meshes");
        _meshSequenz = 0;
        BuildTreeA(); BuildTreeB(); BuildTreeC();
        BuildBush(); BuildFern(); BuildFlowers();
        BuildGroundCoverGrass(); BuildGroundCoverMoss();
        BuildGlowMushrooms();
        BuildRock("SP_Rock_Small", 2); BuildRock("SP_Rock_Medium", 3); BuildRock("SP_Rock_Large", 5);
        BuildRuinWallA(); BuildRuinWallB();
        BuildRuinMonument(); BuildEidrenRune();
    }

    // Persist mit Verwaisungsschutz, Muster wie T1ResourceVisualBuilder.Persist:
    // Assets am selben Sequenz-Index mit anderem Teilnamen werden geloescht.
    private static Mesh Persist(Mesh mesh, string teilName) { ... }

    // Speichert an denselben 16 Pfaden wie heute (SaveAsPrefabAsset ueberschreibt
    // die Datei in-place -> die .meta-GUID bleibt stabil, siehe (b)).
    private static void Save(GameObject root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }
}
```

Signatur: `public static void BuildStandalone()`, keine Parameter, neues MenuItem
`Eidren/V0.2/Stilumbau/Props bauen`. Berührt ausschließlich die 16
`SP_*.prefab`-Dateien unter `Assets/_Game/Prefabs/Environment/StyleProof/` (per
`PrefabUtility.SaveAsPrefabAsset` an denselben Pfaden wie heute — vgl.
`StyleProofContentBuilder.SavePrefab`, Zeilen 276-282, dasselbe Muster für
`SP_EidraReference`) sowie neue Mesh-Assets unter
`Assets/_Game/Art/StyleProof/Meshes`; schreibt **weder** `StyleProofVisualLibrary.asset`
noch `Greenwood_StyleProof_Volume.asset` noch `Zone_Greenwood.unity` neu — keine
Szene wird geöffnet. Task 3 kann diesen Vorschlag 1:1 übernehmen (der Plan nennt
in Task 3 bereits denselben Mesh-Root und dasselbe Namensschema
`SP_{NNN}_{Teilname}`).

**Nebenbefund für Task 3/4:** `StyleProofVisualLibrary.forestPropPaths`
(`StyleProofContentBuilder.cs:261-265`) verdrahtet nur 12 der 16 Requisiten
(fehlend: `SP_GroundCover_Moss`, `SP_RuinWall_B`, `SP_RuinMonument`,
`SP_EidrenRune`). Das ist ein vorbestehender Zustand, unabhängig vom
Stilumbau — hier nur als Beobachtung vermerkt, nicht als Auftrag für Task 1.

---

## (b) Wie kommen die Props in die Zonen — gebacken oder Laufzeit?

### Gebacken, über zwei Builder-Pfade, in allen 8 Zonenspecs

`AreaArtSceneBuilder.Build(sceneName, areaKey, homeBase)`
(`Assets/_Game/Editor/AreaArtSceneBuilder.cs:97-141`) — aufgerufen von den 8
MenuItems `BuildGreenwood`/`BuildMarsh`/`BuildQuarry`/`BuildEmberRuins`/
`BuildTwilightGrove`/`BuildVeilMarsh`/`BuildGreyRifts`/`BuildHomeBase` (Zeilen
27-73) sowie gebündelt von `BuildAllForAutomation` (Zeilen 75-87) — ruft für
jede Zone unbedingt zwei Requisiten-Bauteile auf:

1. **`BuildDecoration`** (Zeilen 143-185): platziert bis zu 12 feste
   Requisiten (6 bei HomeBase, Zeile 167) aus `areaArt.Decorations`, entfernt
   deren Collider (Zeilen 178-182).
2. **`AreaArtGroundCoverBuilder.Build`** (aufgerufen Zeile 128): streut
   zusätzlich Requisiten mit `Density > 0` gitterbasiert über die Fläche
   (`AreaArtGroundCoverBuilder.cs:41-122`), ebenfalls ohne Collider (Zeilen
   110-116).

Beide instanziieren per `PrefabUtility.InstantiatePrefab(decoration.Prefab)`
(`AreaArtSceneBuilder.cs:173`, `AreaArtGroundCoverBuilder.cs:99`) — echte
PrefabInstances, keine Kopien — und die Szene wird anschließend gespeichert
(`AreaArtSceneBuilder.cs:135-139`).

`areaArt.Decorations` stammt aus `AreaArtAssetBuilder.WriteDecorations`
(`Assets/_Game/Editor/AreaArtAssetBuilder.cs:220-231`), die pro Spec-Eintrag
`AssetDatabase.LoadAssetAtPath<GameObject>(".../StyleProof/" + stem + ".prefab")`
lädt (Zeile 228). Alle 8 `AreaSpec`s (`AreaArtAssetBuilder.cs:326-419`) haben
ein nicht-leeres `Decorations`-Array aus `SP_`-Namen:

| Spec | Zeile | `Decorations` |
|---|---|---|
| Greenwood | 326 | Rock_Large/Medium/Small, Plant_Bush/Fern/Flowers, GroundCover_Moss, EidrenRune |
| Marsh | 338 | Tree_C, Rock_Small/Medium, Plant_Fern/Bush/Flowers, GroundCover_Moss, Accent_GlowMushrooms |
| Quarry | 350 | Rock_Large/Medium/Small, RuinWall_A/B, Plant_Bush, GroundCover_Grass, EidrenRune |
| EmberRuins | 362 | RuinMonument, RuinWall_A/B, Rock_Large/Medium, Plant_Bush, GroundCover_Grass, EidrenRune |
| TwilightGrove | 376 | Tree_A/B/C, Plant_Fern/Bush, GroundCover_Moss, RuinMonument, Accent_GlowMushrooms |
| VeilMarsh | 389 | Tree_C, Plant_Fern, GroundCover_Moss, Accent_GlowMushrooms, Rock_Small, RuinWall_A, Plant_Bush, EidrenRune |
| GreyRifts | 402 | Rock_Large/Medium/Small, RuinWall_A/B, RuinMonument, GroundCover_Grass, EidrenRune |
| HomeBase | 419 | Rock_Small, Plant_Bush, GroundCover_Grass, EidrenRune |

Die Vereinigung dieser 8 Listen deckt **alle 16** `SP_`-Prefabs ab — jedes der
16 wird also in mindestens einer Zonenszene gebacken.
`ScatterDensity` (`AreaArtAssetBuilder.cs:242-262`) gibt zusätzlich 7 der 16
(GroundCover_Grass/Moss, Plant_Flowers/Fern, Rock_Small, Plant_Bush,
Accent_GlowMushrooms) eine Streudichte > 0, d. h. diese erscheinen zusätzlich
vervielfacht über `AreaArtGroundCoverBuilder`.

### Was ist beweisbar, was ist Schlussfolgerung

GUIDs der 16 `SP_`-Prefabs wurden aus den `.meta`-Dateien gelesen und in allen
12 Szenendateien unter `Assets/_Game/Scenes/**/*.unity` per Byte-Grep gesucht.
`file` zeigt: **`HomeBase.unity` und `Bootstrap.unity` sind ASCII-Text, alle
übrigen 9 Szenen (`Zone_*.unity`, `EidraForge.unity`) sind binär (`data`,
`m_SerializationMode: 2` in `ProjectSettings/EditorSettings.asset`)** — exakt
derselbe Befund wie in `STILUMBAU_E3B_BAKEKETTE.md` (b).

**Beweisbar (`HomeBase.unity`, ASCII):** Parst man die Datei auf
`--- !u!1001 &... PrefabInstance:`-Blöcke, ergeben sich exakt **41** Blöcke
(deckungsgleich mit dem in E3b bereits notierten Wert), davon lösen 4 eindeutige
`m_SourcePrefab`-GUIDs auf `SP_`-Prefabs auf:

| Prefab | Instanzen in `HomeBase.unity` |
|---|---|
| `SP_GroundCover_Grass` | 26 |
| `SP_Plant_Bush` | 8 |
| `SP_Rock_Small` | 6 |
| `SP_EidrenRune` | 1 |

Das deckt sich exakt mit `AreaSpec.HomeBase.Decorations`
(`AreaArtAssetBuilder.cs:419`: `Rock_Small, Plant_Bush, GroundCover_Grass,
EidrenRune`) plus Streuung für `GroundCover_Grass`/`Plant_Bush`/`Rock_Small`
(`ScatterDensity`, s. o.).

**Nicht beweisbar per Grep (die 7 restlichen Zonenszenen, binär):** Ein
ASCII-Byte-Grep auf alle 16 GUIDs ergab in allen 7 binären Zonenszenen
(`Zone_Greenwood/Marsh/Quarry/EmberRuins/TwilightGrove/VeilMarsh/GreyRifts.unity`)
**0 Treffer**. Das ist — wie in E3b (c) bereits festgestellt — **kein
verlässlicher Negativbeweis**: die binäre Serialisierung kodiert GUIDs
wahrscheinlich nicht als 32-Zeichen-Hex-ASCII, sondern als Rohbytes, weshalb ein
Text-Grep hier grundsätzlich nichts findet — unabhängig davon, ob der Inhalt da
ist. Der Gegenbeweis, dass ASCII-Suche bei echtem ASCII-Inhalt (`HomeBase.unity`)
zuverlässig funktioniert (41/41 Treffer korrekt zugeordnet), stützt diese
Einschätzung.

**Die belastbare Aussage für die 7 binären Zonenszenen stammt aus dem
Codepfad:** `AreaArtSceneBuilder.Build()` (Zeilen 97-141) ruft `BuildDecoration`
und `AreaArtGroundCoverBuilder.Build` **unbedingt** für jede der 8 Specs auf,
sofern `Decorations` nicht leer ist (ist für alle 8 der Fall, s. o.); dieselbe
Codezeile lief nachweislich für `HomeBase` (ASCII-belegt). Es gibt keinen
Unterschied im Codepfad zwischen `HomeBase` und den 7 Außenzonen — der einzige
Unterschied ist die Szenen-Serialisierung (ASCII vs. binär), die die
Beweisbarkeit, nicht den Sachverhalt selbst betrifft.

### Erwartung: kein Szenen-Rebake nötig, weil GUIDs stabil bleiben

Der in (a) vorgeschlagene `StyleProofPropBuilder` überschreibt die 16
`SP_*.prefab`-Dateien **an denselben Pfaden** (`PrefabUtility.SaveAsPrefabAsset`
in-place, wie bereits heute `StyleProofContentBuilder.SavePrefab`,
Zeilen 276-282, es für `SP_EidraReference` tut) — die `.meta`-GUID bleibt dabei
stabil. Jede gebackene `PrefabInstance` referenziert ihre Quelle ausschließlich
über `m_SourcePrefab: {fileID, guid}`, nicht über den Prefab-Inhalt. **Erwartung:
Ein Rebuild der 16 Prefabs lässt jede bereits gebackene Instanz automatisch die
neue Vertexfarben-Geometrie zeigen, ohne dass eine Szene neu geöffnet oder
gespeichert werden muss** — das ist exakt die Behauptung, die Task 4 empirisch
belegen soll.

### Vorschlag: Zählverfahren statt Text-Diff (für Task 4)

```csharp
// Sketch — nicht implementiert, rein lesend (keine Szene wird gespeichert).
internal static class StyleProofPrefabInstanceCounter
{
    private static readonly string[] AffectedScenes =
    {
        "Assets/_Game/Scenes/Zone_Greenwood.unity",
        "Assets/_Game/Scenes/Zone_Marsh.unity",
        "Assets/_Game/Scenes/Zone_Quarry.unity",
        "Assets/_Game/Scenes/Zone_EmberRuins.unity",
        "Assets/_Game/Scenes/Zone_TwilightGrove.unity",
        "Assets/_Game/Scenes/Zone_VeilMarsh.unity",
        "Assets/_Game/Scenes/Zone_GreyRifts.unity",
        "Assets/_Game/Scenes/HomeBase.unity",
    };

    [MenuItem("Eidren/V0.2/Stilumbau/SP-PrefabInstanzen zaehlen")]
    public static void CountAndReport()
    {
        List<string> lines = new List<string>();
        foreach (string scenePath in AffectedScenes)
        {
            // OpenSceneMode.Single, aber NIE SaveScene() -- rein lesend.
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
                    if (src == null) continue;
                    string path = AssetDatabase.GetAssetPath(src);
                    if (!path.StartsWith("Assets/_Game/Prefabs/Environment/StyleProof/SP_")) continue;
                    // Nur Wurzel der PrefabInstance zaehlen, nicht jedes Kind:
                    if (PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject) != t.gameObject) continue;
                    string stem = Path.GetFileNameWithoutExtension(path);
                    counts[stem] = counts.TryGetValue(stem, out int c) ? c + 1 : 1;
                }
            }
            foreach (KeyValuePair<string, int> kv in counts)
            {
                lines.Add($"{scenePath}\t{kv.Key}\t{kv.Value}");
            }
        }
        File.WriteAllLines("Documentation/StilumbauE4_PrefabInstanzen_<stamp>.tsv", lines);
        Debug.Log($"SP-PrefabInstanzen gezaehlt: {lines.Count} Zeilen, {AffectedScenes.Length} Szenen.");
    }
}
```

Vor/Nach-Lauf (vor Task 3 und nach Task 3/4) erzeugt je eine TSV-Zeile pro
Szene×Prefab mit Instanzzahl; ein Diff der beiden Dateien muss **keine
Differenz** zeigen (gleiche Zeilen, gleiche Zahlen) — das belegt „kein
Szenen-Rebake nötig" robuster als ein Byte-Diff der binären Szenendateien
(die sich durch Timestamps/Layout minimal ändern können, ohne dass Instanzen
sich ändern).

---

## (c) Collider-Tabelle der Collider-Props

**Befund vor der Tabelle:** Der Plan (`STILUMBAU_E4_PLAN.md:9,19`) spricht von
„10 Props mit Collider". Tatsächlich tragen nur **9** der 16 `SP_`-Prefabs einen
Collider (per `grep -c "^BoxCollider:\|^SphereCollider:\|^CapsuleCollider:\|^MeshCollider:"`
über alle 16 Dateien): Rock_Small/Medium/Large (3), RuinWall_A/B (2),
RuinMonument (1), Tree_A/B/C (3) = 9. Die restlichen 7
(`SP_Accent_GlowMushrooms`, `SP_EidrenRune`, `SP_GroundCover_Grass/Moss`,
`SP_Plant_Bush/Fern/Flowers`) haben **keinen** Collider. Die Plan-Zahl „10" ist
damit um eins zu hoch — als Diskrepanz hier vermerkt, nicht stillschweigend
korrigiert.

Alle 9 Collider sind vom Typ `BoxCollider`:

| Prefab | Zeile (`m_Size`/`m_Center`) | `m_Size` | `m_Center` | Deckt sich mit `VisualScaleTableBuilder.cs` `ColliderSize` |
|---|---|---|---|---|
| `SP_Rock_Small.prefab` | 142-143 | `(0.8, 0.4, 0.8)` | `(0, 0.2, 0)` | Zeile 139: `(0.8f, 0.4f, 0.8f)` ✓ |
| `SP_Rock_Medium.prefab` | 142-143 | `(1.3, 0.8, 1.3)` | `(0, 0.4, 0)` | Zeile 138: `(1.3f, 0.8f, 1.3f)` ✓ |
| `SP_Rock_Large.prefab` | 142-143 | `(2.2, 1.8, 2.2)` | `(0, 0.9, 0)` | Zeile 137: `(2.2f, 1.8f, 2.2f)` ✓ |
| `SP_RuinWall_A.prefab` | 142-143 | `(2.6, 2.2, 0.8)` | `(0, 1.1, 0)` | Zeile 146: `(2.6f, 2.2f, 0.8f)` ✓ |
| `SP_RuinWall_B.prefab` | 142-143 | `(2, 2.2, 0.8)` | `(0, 1.1, 0)` | Zeile 147: `(2f, 2.2f, 0.8f)` ✓ |
| `SP_RuinMonument.prefab` | 142-143 | `(1.4, 3, 1.4)` | `(0, 1.5, 0)` | Zeile 148: `(1.4f, 3f, 1.4f)` ✓ |
| `SP_Tree_A.prefab` | 124-125 | `(0.9, 3, 0.9)` | `(0, 1.5, 0)` | Zeile 134: `(0.9f, 3f, 0.9f)` ✓ |
| `SP_Tree_B.prefab` | 124-125 | `(0.8, 2.4, 0.8)` | `(0, 1.2, 0)` | Zeile 135: `(0.8f, 2.4f, 0.8f)` ✓ |
| `SP_Tree_C.prefab` | 124-125 | `(0.7, 1.6, 0.7)` | `(0, 0.8, 0)` | Zeile 136: `(0.7f, 1.6f, 0.7f)` ✓ |

Alle 9 Collider-Maße stimmen exakt mit dem `colliderSize`-Parameter der
zugehörigen `Billboard(...)`-Zeile in `VisualScaleTableBuilder.AddProps`
(`Assets/_Game/Editor/VisualScaleTableBuilder.cs:134-149`) überein — die
Größentabelle ist bereits heute die konsistente Quelle für Collider **und**
Höhe (siehe (d)). Task 3 sollte die Collider-Werte aus dieser Tabelle
übernehmen (nicht neu erfinden), dann bleibt der Bestandsvertrag automatisch
erfüllt.

---

## (d) Höhen-/Größenvorgaben

### Es existiert bereits eine explizite Zieltabelle für alle 16 Props

Anders als die Plan-Prämisse „wo keine Vorgabe existiert, gilt die
Ist-Silhouette ±15 %" nahelegt: **Für alle 16 `SP_`-Props existiert bereits eine
exakte Höhenvorgabe**, in `VisualScaleTableBuilder.AddProps()`
(`Assets/_Game/Editor/VisualScaleTableBuilder.cs:132-150`), verdrahtet auf die
Prefab-Pfade über `VisualScaleAssetMap.BakedPrefabs`
(`Assets/_Game/Editor/VisualScaleAssetMap.cs:42-57`), und **scharf getestet**
über `VisualScaleTests.EveryBakedPrefab_MatchesItsTableHeight`
(`Assets/_Game/Editor/Tests/VisualScaleTests.cs:82-101`, Toleranz `expected *
0.01f` = 1 % der Sollhöhe, Zeile 94). Ein ±15-%-Korridor ist für die Höhe also
**nicht** nötig — die 1-%-Toleranz des bestehenden Tests ist die verbindliche
Vorgabe.

`EveryBakedPrefab_StaysWithinItsWidthBudget`
(`VisualScaleTests.cs:103-121`) prüft die Fußabdruck-/Breitenvorgabe dagegen nur
für `visual.building.*`-IDs (Zeile 110: `pair.Key.StartsWith("visual.building.")`)
— für Props existiert **kein** automatisierter Breiten-/Fußabdrucktest. Hier gilt
die Plan-Prämisse „Ist-Silhouette ±15 %" tatsächlich, und die unten gemessenen
Fußabdruckwerte sind die Grundlage dafür.

| Visual-ID | Sollhöhe (m) | Zeile | `ArtworkMissing` | `ColliderSize` |
|---|---|---|---|---|
| `visual.prop.tree_a` | 7,5 | 134 | true („Seitenansicht statt Kamerawinkel") | `(0.9, 3, 0.9)` |
| `visual.prop.tree_b` | 6,0 | 135 | true | `(0.8, 2.4, 0.8)` |
| `visual.prop.tree_c` | 4,0 | 136 | true | `(0.7, 1.6, 0.7)` |
| `visual.prop.rock_large` | 2,4 | 137 | false | `(2.2, 1.8, 2.2)` |
| `visual.prop.rock_medium` | 1,1 | 138 | false | `(1.3, 0.8, 1.3)` |
| `visual.prop.rock_small` | 0,5 | 139 | false | `(0.8, 0.4, 0.8)` |
| `visual.prop.bush` | 0,9 | 140 | false | kein Blocker |
| `visual.prop.fern` | 0,7 | 141 | false | kein Blocker |
| `visual.prop.flowers` | 0,4 | 142 | false | kein Blocker |
| `visual.prop.ground_grass` | 0,0 (GroundPlane, Extent 1,2) | 143 | true | kein Blocker |
| `visual.prop.ground_moss` | 0,0 (GroundPlane, Extent 1,2) | 144 | true | kein Blocker |
| `visual.prop.glow_mushrooms` | 0,35 | 145 | false | kein Blocker |
| `visual.prop.ruin_wall_a` | 2,6 | 146 | false | `(2.6, 2.2, 0.8)` |
| `visual.prop.ruin_wall_b` | 2,6 | 147 | false | `(2, 2.2, 0.8)` |
| `visual.prop.ruin_monument` | 5,0 | 148 | false | `(1.4, 3, 1.4)` |
| `visual.prop.rune` | 0,8 | 149 | false | kein Blocker |

### Ist-Silhouette der aktuellen (Sprite-Kreuz-Hybrid-)Prefabs — gemessen

Methode: Für jedes `SP_`-Prefab wurden alle `MeshFilter`-Referenzen ausgelesen
(`m_Mesh: {fileID, guid}`), die zugehörigen Mesh-Assets identifiziert (Unity-
Builtin-Primitive über bekannte `fileID`s wie `10202`=Cube, `10206`=Zylinder,
oder eigene `.asset`-Dateien unter `Assets/Mesh/*.asset`, deren
`m_LocalAABB`-Feld `m_Center`/`m_Extent` direkt aus der YAML gelesen wurde,
z. B. `Assets/Mesh/visual_prop_tree_a_crown_Canopy.asset:153-155`). Die 8
Eckpunkte jeder lokalen AABB wurden durch die Transform-Kette vom Mesh-Knoten
bis zur Prefab-Wurzel transformiert (Position/Rotation/Skalierung aus den
`Transform`-Blöcken der Prefab-YAML), alle Eckpunkte aller Meshes eines Prefabs
vereinigt. Ergebnis ist eine konservative (achsenausgerichtete) Näherung der
Ist-Silhouette — für Objekte mit gekippten Teilen (z. B. `SP_Rock_*`) liegt sie
tendenziell etwas über der tatsächlichen visuellen Ausdehnung.

**Gegenprobe:** Die so gemessene Höhe trifft für alle Fälle mit klarer
Höhenvorgabe **exakt** die Sollhöhe aus der Tabelle oben (z. B. `SP_Tree_A`
gemessen 7,500 m = Soll 7,5 m; `SP_RuinMonument` gemessen 5,000 m = Soll 5,0 m) —
das bestätigt sowohl die Messmethode als auch, dass die aktuellen Prefabs exakt
auf die Tabellenwerte gebaut wurden.

| Prefab | Gemessene Höhe (m) | Footprint X×Z (m) | Herangezogene Mesh-Assets |
|---|---|---|---|
| `SP_Tree_A` | 7,500 | 4,80 × 4,16 | Builtin-Zylinder (Trunk) + `93377474a8757ec4c814bb70b51868a6` (Krone) |
| `SP_Tree_B` | 6,000 | 3,60 × 3,12 | Builtin-Zylinder + `885762c723ef53c459725ec5324a789a` |
| `SP_Tree_C` | 4,000 | 2,50 × 2,17 | Builtin-Zylinder + `1be01441e606a224c8effa571e8f7d99` |
| `SP_Rock_Large` | 2,400 | 3,48 × 3,56 | `e11552e62cb6e9b40b1561b8eefad276`, `87cb28982fdc2894fb3c740a1fc3f5ff` (×2) |
| `SP_Rock_Medium` | 1,100 | 1,72 × 1,57 | `37fad9bc65a83b64eaaa1a011a891e35`, `9807a13417695324ca2584714f7df235` (×2) |
| `SP_Rock_Small` | 0,500 | 0,83 × 0,74 | `102c1622eea71e849a3dc3c4de40c372`, `02a09607e8574ff4ba533f37e252f366` (×2) |
| `SP_Plant_Bush` | 0,900 | 1,20 × 1,04 | `e61d9143dfad7fa4190a754dde412e81` |
| `SP_Plant_Fern` | 0,700 | 0,95 × 0,82 | `7c92180e440cd9f4a899b2ab8bf97e46` |
| `SP_Plant_Flowers` | 0,400 | 0,55 × 0,55 | `42fa29cbac612724a94cc0d0314118fc` |
| `SP_GroundCover_Grass` | 0,000 (flache Bodenplatte, y≈0,018) | 1,20 × 1,20 | `41ba261cf6af4024295cda3d66ad36e9` |
| `SP_GroundCover_Moss` | 0,000 (flache Bodenplatte, y≈0,018) | 1,20 × 1,20 | `3d4e96548e741054299a0b65f2b91c93` |
| `SP_Accent_GlowMushrooms` | 0,350 | 0,48 × 0,42 | `a07f9e48bd6bf1d44b8a5f448cfc968b` |
| `SP_RuinWall_A` | 2,600 | 2,70 × 0,58 | Builtin-Cube (×2) + `8a022e9567c3d5246a678767ba8a1b3a` |
| `SP_RuinWall_B` | 2,600 | 2,25 × 0,58 | Builtin-Cube (×2) + `04603a13d2015fd489199524235c60f6` |
| `SP_RuinMonument` | 5,000 | 1,70 × 1,30 | Builtin-Cube (×2) + `c30416ccbaea1de40b16ae728f745ddd` |
| `SP_EidrenRune` | 0,800 | 0,65 × 0,13 | Builtin-Cube + `4b1ef41beb75238469b3dc48f9a60bd8` |

Für Task 3 gilt: Höhen-Zielwert = Tabelle oben (1-%-Testtoleranz, verbindlich);
Footprint-Korridor = gemessene Ist-Silhouette ±15 % (kein Test, informell, aus
obiger Tabelle).

Messskript (nicht Teil der Codebasis, nur zur Nachvollziehbarkeit dieser
Analyse verwendet): `measure_aabb.py`, parst Prefab-YAML +
`m_LocalAABB`-Einträge der referenzierten Mesh-Assets, siehe Vorgehensbeschreibung
oben.

---

## (e) Testklassen für den Regressionsfilter

Suche nach `StyleProof`/`SP_`/`styleproof` in `Assets/_Game/Editor/Tests/` plus
gezielte Prüfung der indirekt betroffenen Klassen (Zuordnung über
`VisualScaleAssetMap`, die selbst keine `SP_`-Literale enthält, aber alle 16
Prefabpfade referenziert):

| Testklasse | Datei | Bezug |
|---|---|---|
| `StyleProofContentTests` | `Assets/_Game/Editor/Tests/StyleProofContentTests.cs` | Direkt: prüft die 12 `M_SP_*`-Materialien und ≥14 StyleProof-Prefabs (Zeilen 17-31); verbietet `StyleProofReferenceArea` in `Zone_Greenwood.unity` (Zeile 38); prüft `Greenwood_StyleProof_Volume.asset` (Zeilen 51-63) |
| `EidrenWorldStyleTests` | `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` | Muster-Vorlage für die 16 neuen `Prop_IstImVertexfarbenStil`-TestCases aus Task 3 (bestehende `Weltkiste_...`/`ForgeKiste_...`-TestCases als Vorbild, Zeilen 28-56, 58-... ) |
| `VisualScaleTests` | `Assets/_Game/Editor/Tests/VisualScaleTests.cs` | Indirekt über `VisualScaleAssetMap.BakedPrefabs` (alle 16 `visual.prop.*`-Einträge): `EveryBakedPrefab_MatchesItsTableHeight` (Zeilen 82-101, 1-%-Höhentoleranz), `TableAndPrefabs_HaveNoOrphansInEitherDirection` (Zeilen 123-151), `ArtworkDebt_OnlyEverShrinks` (Zeilen 153-158) |
| `AreaArtTests` | `Assets/_Game/Editor/Tests/AreaArtTests.cs` | Randbezug: prüft Abwesenheit alter `StyleProof`-Pfad-Texturen/-Materialien (Zeilen 105-107) |
| `PlaceholderCleanupTests` | `Assets/_Game/Editor/Tests/PlaceholderCleanupTests.cs` | Randbezug: führt `StyleProofReferenceArea` in der Legacy-Verbotsliste über alle 9 ausgelieferten Szenen (Zeilen 15-26, 32-45) |

Empfehlung für den Regressionsfilter ab Task 2 (Plan-Vorgabe: immer inklusive
`VisualAssetTests`): zusätzlich **`StyleProofContentTests`, `EidrenWorldStyleTests`,
`VisualScaleTests`, `AreaArtTests`, `PlaceholderCleanupTests`** — die letzten
beiden, weil ein versehentlicher Wiedereinbau von `BuildGreenwood`-Nebenwirkungen
(siehe (a)) sonst unbemerkt bliebe.

---

## Zusammenfassung für Task 2/3/4

| Frage | Antwort |
|---|---|
| (a) Heutiger Bauer | Niemand — `StyleProofContentBuilder.BuildPrefabs()` validiert nur (`RequireGeometryPrefab`), baut keine Geometrie. `BuildAll()` hat schwere Nebenwirkungen inkl. `BuildGreenwood`, das zwei bestehende Tests bricht (Legacy-Referenzbereich verboten). Vorschlag: neue Klasse `StyleProofPropBuilder.BuildStandalone()`, mirrort `T1ResourceVisualBuilder`, nutzt nur `EidrenMeshFactory`/`EidrenWorldStyleAssets`, schreibt ausschließlich an den 16 bestehenden Prefabpfaden. |
| (b) Bake-Weg | Gebacken über `AreaArtSceneBuilder.BuildDecoration` (fest) + `AreaArtGroundCoverBuilder.Build` (gestreut), für alle 8 `AreaSpec`s, alle 16 Prefabs erfasst. Beweisbar direkt nur für `HomeBase.unity` (ASCII, 41 PrefabInstance-Blöcke, 4 GUIDs). Für die 7 binären Zonenszenen gilt der Codepfad-Beweis, kein GUID-Grep (methodisch unzuverlässig bei Binärformat). GUIDs bleiben stabil bei In-Place-Überschreiben ⇒ kein Szenen-Rebake erwartet — Zählverfahren-Skizze oben für den empirischen Beleg in Task 4. |
| (c) Collider | Nur 9, nicht 10 (Plan-Diskrepanz vermerkt): Rock_Small/Medium/Large, RuinWall_A/B, RuinMonument, Tree_A/B/C — alle `BoxCollider`, Maße decken sich exakt mit `VisualScaleTableBuilder.ColliderSize`. |
| (d) Zielhöhen | Bereits vollständig in `VisualScaleTableBuilder.AddProps()` für alle 16 Props definiert und über `VisualScaleTests` mit 1-%-Toleranz getestet — kein ±15-%-Korridor für die Höhe nötig. Footprint ungetestet; gemessene Ist-Silhouette (Tabelle oben) liefert den ±15-%-Korridor dafür. |
| (e) Testklassen | `StyleProofContentTests`, `EidrenWorldStyleTests`, `VisualScaleTests`, `AreaArtTests`, `PlaceholderCleanupTests`. |
