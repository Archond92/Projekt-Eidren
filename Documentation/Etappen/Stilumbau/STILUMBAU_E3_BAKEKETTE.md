# Stilumbau Etappe 3 — Bake-Kette der Tier-2-Zonenvarianten (Task 1)

Auflage aus dem E2-Abschlussreview: Vor jedem Builder-Lauf in Etappe 3 muss dokumentiert
sein, was der jeweilige Einstiegspunkt tatsächlich anfasst — Hintergrund war ein `BuildAll`,
das in einer früheren Etappe eine Szene gespeichert hat, die niemand erwartet hat. Diese
Analyse ist rein statisch (Quelltext- und Prefab-YAML-Lektüre); es wurde **kein Unity-Lauf**
durchgeführt und **kein Code geändert**. Alle Befunde sind mit Datei:Zeile belegt.

Stand: 07.08.2026.

---

## (a) Welcher Codepfad ruft `CloneVariant` für die sechs T2-Varianten auf?

**Einstieg:** `AreaArtAssetBuilder.BuildAll()`
MenuItem `Eidren/Art/Areas/Build Area Art Assets`
— `Assets/_Game/Editor/AreaArtAssetBuilder.cs:45-59`

```
[MenuItem("Eidren/Art/Areas/Build Area Art Assets")]   // Zeile 45
public static void BuildAll()                          // Zeile 46
{
    ...
    foreach (AreaSpec item in specs) { Build(item); }   // Zeilen 49-53
    AssignZoneDefinitions(specs);                       // Zeile 54
    ...
    VerifyZoneDefinitions(specs);                       // Zeile 57
}
```

`BuildAll` iteriert **ausnahmslos über alle 8 `AreaSpec`-Einträge** aus `Specs()`
(`AreaArtAssetBuilder.cs:254-361`), darunter die drei T2-Gebiete. Für jedes Gebiet ruft
`Build(AreaSpec spec)` (`AreaArtAssetBuilder.cs:94-134`) je Ressourcenvariante
`AreaArtVariantPrefabBuilder.CloneVariant(...)` auf (`AreaArtAssetBuilder.cs:112-113`):

```
activeVariants.Add(AreaArtVariantPrefabBuilder.CloneVariant(spec, resourceId,
    "Assets/_Game/Prefabs/Resources/Visuals/" + sourceStem + "_Active.prefab", exhausted: false));   // Zeile 112
exhaustedVariants.Add(AreaArtVariantPrefabBuilder.CloneVariant(spec, resourceId,
    "Assets/_Game/Prefabs/Resources/Visuals/" + sourceStem + "_Exhausted.prefab", exhausted: true));  // Zeile 113
```

`CloneVariant` selbst (`Assets/_Game/Editor/AreaArtVariantPrefabBuilder.cs:10-34`) lädt die
**Quelle** per `PrefabUtility.LoadPrefabContents(sourcePath)` (Zeile 20), hängt eine
Gebiets-Identitätsgeometrie an und schreibt per `PrefabUtility.SaveAsPrefabAsset(root, path)`
(Zeile 31) auf den festen Pfad `Assets/_Game/Prefabs/Environment/AreaArtVariants/<Key>_<Resource>_<State>.prefab`.

Die sechs T2-Varianten × Active/Exhausted (= 12 `CloneVariant`-Aufrufe) entstehen aus den
drei T2-`AreaSpec`-Einträgen in `Specs()`:

| Spec (Key) | Zeile | VariantResourceIds / VariantSources | CloneVariant-Aufrufe |
|---|---|---|---|
| TwilightGrove | 304-317 | `resource.hardwood_tree`/`HardwoodTree`, `resource.swamp_hemp`/`SwampHemp` | 4 (Active+Exhausted × 2) |
| VeilMarsh | 318-330 | `resource.swamp_hemp`/`SwampHemp`, `resource.iron_vein`/`IronVein` | 4 |
| GreyRifts | 331-343 | `resource.granite_deposit`/`GraniteDeposit`, `resource.iron_vein`/`IronVein` | 4 |

**Weiterer Einstiegspfad, der denselben Codepfad kettet:**
`AreaArtSceneBuilder.BuildAllForAutomation()` (`Assets/_Game/Editor/AreaArtSceneBuilder.cs:75-87`)
ruft in Zeile 77 zuerst `AreaArtAssetBuilder.BuildAll()` und baut/speichert danach **alle acht**
Zonenszenen neu (Zeilen 78-85, siehe (b)). Dies ist mit hoher Wahrscheinlichkeit der
Mechanismus hinter dem in einer früheren Etappe beobachteten unerwarteten Szenen-Save.

---

## (b) Schreibt dieser Einstieg auch Tier-1-Varianten/Szenen — und wie sieht ein T2-only-Lauf aus?

### Befund: Ja, `BuildAll()` schreibt auch Tier-1-Varianten neu

`Specs()` liefert außer den drei T2-Einträgen auch vier T1-Einträge mit eigenen
`VariantResourceIds`/`VariantSources`, die in `Build(spec)` denselben `CloneVariant`-Pfad
durchlaufen:

| Spec (Key) | Zeile | Ressourcen | CloneVariant-Aufrufe |
|---|---|---|---|
| Greenwood | 256-267 | `resource.tree`/`Tree` | 2 |
| Marsh | 268-279 | `resource.tree`/`Tree`, `resource.fiber_plant`/`FiberPlant` | 4 |
| Quarry | 280-291 | `resource.tree`/`Tree`, `resource.stone_deposit`/`StoneDeposit` | 4 |
| EmberRuins | 292-303 | `resource.tree`/`Tree`, `resource.copper_vein`/`CopperVein`* | 2 |

\* `resource.copper_vein` ist Sonderfall: `Build()` lädt für ihn das existierende Prefab
direkt per `AssetDatabase.LoadAssetAtPath` (`AreaArtAssetBuilder.cs:107-108`) statt
`CloneVariant` aufzurufen — kein Schreibzugriff für diese eine Variante.

Macht insgesamt 12 T1-`CloneVariant`-Aufrufe neben den 12 T2-Aufrufen bei jedem `BuildAll()`-Lauf.

### Kritische Prüfung: Zerstört ein T1-Reclone die handgebaute `Geometry_A14`-Geometrie?

Grep über `Geometry_A14` (Objektname, `m_Name:`-Feld in der YAML) zeigt: **sowohl** die
Quell-Visuals **als auch** die Zonenvarianten tragen genau je einen Treffer:

- Quelle `Assets/_Game/Prefabs/Resources/Visuals/Tree_Active.prefab:226` → `m_Name: Geometry_A14`
- Variante `Assets/_Game/Prefabs/Environment/AreaArtVariants/Greenwood_tree_Active.prefab:228` → `m_Name: Geometry_A14`
- Ebenso je 1 Treffer in `Marsh_tree_Active.prefab`, `Quarry_tree_Active.prefab`, `EmberRuins_tree_Active.prefab`
  (alle T1-Baum-Varianten) sowie in allen zehn T1-Visual-Prefabs unter `Visuals/`
  (`Tree_*`, `FiberPlant_*`, `StoneDeposit_*`, `CopperVein_*`, `BerryBush_*`).

**Schlussfolgerung zur Fließrichtung:** Weil `CloneVariant` **jedes Mal frisch aus der Quelle**
lädt (`LoadPrefabContents(sourcePath)`, `AreaArtVariantPrefabBuilder.cs:20`) und die Zielda­tei
komplett überschreibt (`SaveAsPrefabAsset`, Zeile 31), ist `Geometry_A14` in der Variante kein
eigenständig autorisierter Inhalt, sondern ein **mechanisches Nebenprodukt** des Klonens aus der
(unveränderten) Quelle. Die Autorenschaft liegt in den Quell-Visuals unter `Visuals/`, nicht in
den Varianten selbst. Ein erneuter T1-Klonlauf würde `Geometry_A14` also **inhaltlich
reproduzieren, nicht zerstören** — Etappe 3 rührt die zehn T1-Quell-Visuals nicht an.

**Trotzdem bleibt ein voller `BuildAll()`-Lauf riskant und ist zu vermeiden:**
1. `PrefabUtility.SaveAsPrefabAsset` vergibt für neu instanzierte Kind-GameObjects/Komponenten
   bei jedem Lauf potenziell neue interne `fileID`s — der Dateiinhalt/Zeitstempel ändert sich
   auch bei identischer Geometrie. Task 4 Schritt 1 verlangt aber exakt: *"Tier-1-Varianten:
   Zeitstempel vor/nach dem Lauf vergleichen — sie dürfen sich NICHT ändern."* Ein `BuildAll()`-
   Lauf würde dieses Abnahmekriterium unabhängig vom Geometrieinhalt verletzen.
2. `AssignZoneDefinitions`/`VerifyZoneDefinitions` (Zeilen 54/57) laufen ebenfalls unconditional
   über **alle** Specs, inklusive `HomeBase`.
3. Der verkettete Automation-Einstieg `BuildAllForAutomation` speichert zusätzlich alle acht
   Zonenszenen neu (s. u.) — das ist der eigentliche Hochrisiko-Nebeneffekt.

### Wie sieht ein T2-only-Lauf aus?

Es existiert **keine** vorhandene schmale Methode auf Asset-Ebene für nur die drei T2-Specs
(anders als bei den Szenen, siehe unten). Vorschlag für eine neue, schmale Einstiegsmethode,
die ausschließlich vorhandene private Bausteine wiederverwendet:

```csharp
// in AreaArtAssetBuilder.cs, neben BuildAll()
private static readonly string[] TierTwoKeys = { "TwilightGrove", "VeilMarsh", "GreyRifts" };

[MenuItem("Eidren/Art/Areas/Build Tier Two Area Art Assets")]
public static void BuildTierTwo()
{
    EnsureFolders();                                                     // wiederverwendet, Zeile 238
    List<AreaSpec> tierTwo = new List<AreaSpec>(Specs())                 // wiederverwendet, Zeile 254
        .FindAll(s => Array.IndexOf(TierTwoKeys, s.Key) >= 0);
    foreach (AreaSpec item in tierTwo)
    {
        Build(item);                                                     // wiederverwendet, Zeile 94
    }
    AssignZoneDefinitions(tierTwo);                                      // wiederverwendet, Zeile 61
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    VerifyZoneDefinitions(tierTwo);                                      // wiederverwendet, Zeile 80
    Debug.Log("Tier-two area-art assets built.");
}
```

Signatur: `public static void BuildTierTwo()`, keine Parameter, neues MenuItem
`Eidren/Art/Areas/Build Tier Two Area Art Assets`. Berührt ausschließlich die drei T2-Specs;
`Build(item)` wird für Greenwood/Marsh/Quarry/EmberRuins/HomeBase nicht aufgerufen, also auch
kein `CloneVariant` für T1.

**Auf Szenenebene existiert die schmale Methode bereits:**
`AreaArtSceneBuilder.BuildTierTwoForAutomation()` (`AreaArtSceneBuilder.cs:89-95`) ruft nur
`BuildTwilightGrove()` / `BuildVeilMarsh()` / `BuildGreyRifts()` auf (Zeilen 91-93) — **ohne**
vorher `AreaArtAssetBuilder.BuildAll()` zu rufen, und **ohne** die T1-Zonenszenen anzufassen.
Sie ist die richtige Wahl, falls (siehe (c)) doch ein Szenen-Rebake nötig würde.

---

## (c) Welche Szenen referenzieren die T2-Prefabs — ist ein Szenen-Rebake nötig?

### Textsuche in den Szenendateien schlägt fehl — Szenen sind binär

`ProjectSettings/EditorSettings.asset` setzt `m_SerializationMode: 2` (Force Binary). Der
`file`-Befehl bestätigt für alle drei T2-Zonenszenen `data` (kein Text); eine Grep-Suche nach
der GUID des Node-Prefabs `HardwoodTree.prefab` (`a1c255cad003779439ba536f93283eec`,
aus `Assets/_Game/Prefabs/Resources/Nodes/HardwoodTree.prefab.meta`) in
`Assets/_Game/Scenes/Zone_TwilightGrove.unity` liefert **keinen Treffer** — wie im Auftrag
erwartet, muss der Befund aus dem Builder-/Laufzeit-Code abgeleitet werden.

### Befund aus dem Code: Ressourcenknoten stehen gar nicht statisch in der Szene

1. `EidrenSceneStructureBuilder.BuildZoneScene` (`Assets/_Game/Editor/EidrenSceneStructureBuilder.cs:224` ff.)
   legt beim Szenenbau nur eine **leere** `PopulationRoot` an (Zeile 260) — keine Node- oder
   Varianten-Prefabs werden hier instanziert oder gespeichert.
2. Zur Laufzeit füllt `ZoneResourcePopulator.Populate()` (`Assets/_Game/Scripts/Composition/ZoneResourcePopulator.cs:35-52`)
   diese Root bei jedem Zonenbetreten neu: `Spawn()` (Zeilen 54-77) instanziert
   `placement.Definition.NodePrefab` (Zeile 62: `Object.Instantiate(nodePrefab, ...)`) — die
   `NodePrefab`-Referenz kommt aus dem `ResourceNodeDefinition`-Asset, gesetzt von
   `ResourceContentBuilder.LinkNodePrefab` (`Assets/_Game/Editor/ResourceContentBuilder.cs:153-169`).
3. Direkt danach tauscht `ResourceNodeVariantApplicator.Apply()`
   (`Assets/_Game/Scripts/Composition/ResourceNodeVariantApplicator.cs:9-34`, aufgerufen aus
   `ZoneResourcePopulator.cs:67`) das Basis-Visual gegen die zonenspezifische Variante:
   `Object.Instantiate(variant.ActiveVisualPrefab, transform)` / `...ExhaustedVisualPrefab`
   (Zeilen 26 und 28). `variant` stammt aus `ZoneAreaArtDefinition.TryGetResourceVariant`
   (`Assets/_Game/Scripts/Data/ZoneAreaArtDefinition.cs:52-64`), gespeist von
   `AreaArtAssetBuilder.WriteVariants` (`AreaArtAssetBuilder.cs:148-161`) — genau den
   `GameObject`-Referenzen, die `CloneVariant` zurückgibt.

### Schlussfolgerung

Die T2-Varianten-/Node-Prefabs werden **von keiner Szene referenziert** — weder als
Prefab-Instanz noch als eingebackene Kopie. Platzierung geschieht ausschließlich prozedural
zur Laufzeit über Asset-Referenzen (GUID) auf `ZoneAreaArtDefinition`- und
`ResourceNodeDefinition`-Assets. Da `CloneVariant` (`AreaArtVariantPrefabBuilder.cs:19`) und
`NodePrefab` (`ResourceContentBuilder.cs:216`) immer denselben Asset-Pfad neu beschreiben,
bleibt die `.meta`-GUID über den Rebuild hinweg stabil (Unity ändert die GUID einer
bestehenden Datei beim Überschreiben nicht) — jede vorhandene Referenz löst beim nächsten
Zonenbetreten automatisch den neuen Prefab-Inhalt auf.

**Ein Szenen-Rebake ist nach dem Neuklonen der T2-Varianten NICHT nötig.** Weder
`AreaArtSceneBuilder` (Boden/Dekoration) noch `EidrenSceneStructureBuilder` (Zonenstruktur)
müssen für die reine Visual-Erneuerung erneut laufen — Task 4 Schritt 3 trifft also den
"kein Rebake nötig"-Fall und sollte das ausdrücklich so festhalten, statt einen Szenen-Builder
zu starten.

---

## (d) Sind `ActiveVisual`/`ExhaustedVisual` in den vier T2-Node-Prefabs Prefab-Instanzen oder gebackene Kopien?

### Befund: gebackene Kopien (keine Prefab-Instanzen)

`grep -c "!u!1001"` (Unity-YAML-Tag für `PrefabInstance`-Blöcke) ergibt **0** für alle vier
T2-Node-Prefabs:

```
Assets/_Game/Prefabs/Resources/Nodes/HardwoodTree.prefab:  0
Assets/_Game/Prefabs/Resources/Nodes/SwampHemp.prefab:      0
Assets/_Game/Prefabs/Resources/Nodes/GraniteDeposit.prefab: 0
Assets/_Game/Prefabs/Resources/Nodes/IronVein.prefab:       0
```

(Zum Vergleich: auch das T1-Kontrollprefab `Nodes/Tree.prefab` hat 0 Treffer — das
Baked-Copy-Verhalten ist projektweit für alle Node-Prefabs identisch, nicht T2-spezifisch.)

Konkret in `Assets/_Game/Prefabs/Resources/Nodes/HardwoodTree.prefab`:
- Jedes GameObject trägt `m_CorrespondingSourceObject: {fileID: 0}` und
  `m_PrefabInstance: {fileID: 0}` (z. B. Zeilen 7-9, 26-28, 42-44, 60-62, 78-80, 96-98,
  114-116, 130-132, 147-149) — d. h. keine Verbindung zu einem Quell-Prefab.
- `ActiveVisual` (GameObject, Zeile 22-37, `m_Name: ActiveVisual` Zeile 32) enthält als
  eigenständige Kind-GameObjects `AncientTrunk` (Zeile 38-55, Name Zeile 50),
  `BrokenBranch` (56-73, Name 68), `CrownLow` (74-91, Name 86), `CrownHigh` (92-109, Name 104)
  — vollständige `Transform`/`MeshFilter`/`MeshRenderer`-Ketten je Teil, keine Referenz auf ein
  Prefab-Asset.
- Diese Teilenamen (`AncientTrunk`, `BrokenBranch`, `CrownLow`, `CrownHigh`,
  `WideStump`/`Split` in `ExhaustedVisual`, Zeilen 110-161) sind exakt die Bauteile, die der
  geplante `T2ResourceVisualBuilder.BuildHardwood()` aus dem E3-Plan erzeugt — bestätigt, dass
  der Node beim letzten Build-Lauf eine **Momentaufnahme** des damaligen
  `Visuals/HardwoodTree_Active.prefab`-Inhalts eingebettet bekommen hat, keine lebende
  Verknüpfung.

### Mechanismus (warum keine Prefab-Instanz entsteht)

`ResourceContentBuilder.NodePrefab(ResourceNodeDefinition definition)`
(`Assets/_Game/Editor/ResourceContentBuilder.cs:188-218`):

```
GameObject active = (GameObject)PrefabUtility.InstantiatePrefab(definition.ActiveVisualPrefab);  // Zeile 208
...
GameObject spent = (GameObject)PrefabUtility.InstantiatePrefab(definition.ExhaustedVisualPrefab); // Zeile 211
...
PrefabUtility.SaveAsPrefabAsset(root, NodePrefabPath(definition.Id));                              // Zeile 216
```

`definition.ActiveVisualPrefab`/`ExhaustedVisualPrefab` verweisen auf die **Basis-Visuals**
unter `Visuals/` (nicht auf die Zonenvarianten) — belegt über `BuildResourceDefinitions`
(`ResourceContentBuilder.cs:31-65`), das die Definitionen mit `visuals["resource.hardwood_tree"]`
usw. aus `ResourceNodeVisualTable.BuildAll()` (`Assets/_Game/Editor/ResourceNodeVisualTable.cs:25`)
befüllt, welches wiederum direkt `Visuals/HardwoodTree_Active.prefab` lädt
(`ResourceNodeVisualTable.cs:41-43`, Konvention `<Stem>_Active`/`<Stem>_Exhausted`). Obwohl
`InstantiatePrefab` zunächst eine Prefab-Instanz erzeugt, geht diese Verbindung beim
anschließenden `SaveAsPrefabAsset` im vorliegenden Projekt nachweislich verloren (siehe
YAML-Befund oben) — das Ergebnis ist eine vollständig eigenständige, gebackene Kopie.

### Schlussfolgerung für Task 4

Die vier T2-Node-Prefabs aktualisieren sich **nicht automatisch**, wenn Task 3 die
Basis-Visuals `HardwoodTree_Active/Exhausted`, `SwampHemp_Active/Exhausted`,
`GraniteDeposit_Active/Exhausted`, `IronVein_Active/Exhausted` neu baut. **Task 4 muss die
vier Node-Prefabs explizit über `ResourceContentBuilder.NodePrefab` neu erzeugen.**

`NodePrefab` ist `private` und wird bislang nur aus `BuildResourceDefinitions()`
(`ResourceContentBuilder.cs:31-65`) aufgerufen, die **alle neun** Ressourcentypen (T1 + T2)
neu baut sowie `ItemContentAssetBuilder.BuildItemDefinitions()` (Zeile 33) und
`ConfigureZoneAllocations` (bei Aufruf über `BuildResourceCollection`, Zeile 70, für **alle**
Zonen inkl. T1) mitreißt — das ist zu breit. Auch das vorhandene, vielversprechend benannte
`[MenuItem("Eidren/Data/Build Tier Two Resource Collection")]` /
`BuildTierTwoResourceCollection()` (`ResourceContentBuilder.cs:77-84`) ist **irreführend
benannt**: Es ruft intern ebenfalls das volle `BuildResourceDefinitions()` auf (Zeile 80) und
baut damit trotz des Namens alle neun Node-Prefabs und alle Zonen-Allokationen neu — für den
T1-Tabu-Zweck also **nicht geeignet**.

Vorschlag für eine echte schmale Einstiegsmethode, die nur die vier T2-Node-Prefabs anfasst
und ausschließlich vorhandene private Bausteine wiederverwendet:

```csharp
// in ResourceContentBuilder.cs, neben BuildResourceDefinitions()
private static readonly string[] TierTwoResourceIds =
{
    "resource.hardwood_tree", "resource.swamp_hemp", "resource.granite_deposit", "resource.iron_vein"
};

[MenuItem("Eidren/Data/Rebuild Tier Two Node Prefabs")]
public static void RebuildTierTwoNodePrefabs()
{
    foreach (string id in TierTwoResourceIds)
    {
        string assetName = NodePrefabPath(id).Split('/')[^1].Replace(".prefab", string.Empty); // wiederverwendet Namenskonvention, Zeile 279
        ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(
            "Assets/_Game/Data/Resources/" + assetName + ".asset");
        NodePrefab(definition);                                                                 // wiederverwendet, Zeile 188
    }
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log("Tier-two node prefabs rebuilt.");
}
```

Signatur: `public static void RebuildTierTwoNodePrefabs()`, keine Parameter. Berührt nur die
vier bestehenden `ResourceNodeDefinition`-Assets (deren `ActiveVisualPrefab`/
`ExhaustedVisualPrefab`-Referenzen unverändert bleiben — nur der Node-Prefab-Inhalt wird aus
den frisch gebauten Basis-Visuals neu gebacken) und nicht `ItemContentAssetBuilder`,
`ConfigureZoneAllocations`, `BuildCatalog` oder ein T1-Ressourcentyp.

---

## Zusammenfassung für Task 4

| Frage | Antwort |
|---|---|
| (a) Codepfad | `AreaArtAssetBuilder.BuildAll()` → `Build(spec)` → `AreaArtVariantPrefabBuilder.CloneVariant` für alle 8 Specs; MenuItem `Eidren/Art/Areas/Build Area Art Assets`. |
| (b) T1 betroffen? | Ja, `BuildAll()` reclont auch T1-Varianten (Geometrieinhalt reproduzierbar, aber Zeitstempel/fileIDs ändern sich — verletzt Task-4-Abnahme). Schmale T2-only-Methode `AreaArtAssetBuilder.BuildTierTwo()` neu vorgeschlagen. |
| (c) Szenen-Rebake nötig? | Nein. Resource-Nodes werden nie in Szenen gebacken, sondern laufzeit-prozedural über GUID-Referenzen instanziert; Neuklonen am selben Pfad hält die GUID stabil. |
| (d) Node-Prefabs Instanz oder Kopie? | Gebackene Kopien (0 `PrefabInstance`-Blöcke in allen vier T2-Node-Prefabs). Task 4 muss die vier Nodes über `ResourceContentBuilder.NodePrefab` (neue schmale Methode `RebuildTierTwoNodePrefabs()` vorgeschlagen) explizit neu bauen. |
