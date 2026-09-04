# Stilumbau Etappe 3b — Bake-Ketten-Nachtrag T1 und Höhenziele (Task 1)

Diese Analyse ist rein statisch (Quelltext- und Prefab-/Szenen-YAML-Lektüre); es wurde
**kein Unity-Lauf** durchgeführt und **kein Code geändert**. Alle Befunde sind mit
Datei:Zeile belegt. Sie baut auf `STILUMBAU_E3_BAKEKETTE.md` (Tier-2-Analyse, 07.08.2026)
auf und übernimmt deren Struktur; wo sich Zeilennummern seit 3a verschoben haben (z. B.
weil `BuildTierTwo()`/`CloneVariants()` in `AreaArtAssetBuilder.cs` neu dazukamen), gelten
die aktuellen Nummern aus diesem Dokument.

Stand: 08.08.2026.

---

## (a) T1-Varianten-Klonwege

### Welche `AreaSpec`s sind Tier-1?

`AreaArtAssetBuilder.Specs()` (`Assets/_Game/Editor/AreaArtAssetBuilder.cs:288-395`) liefert
acht Gebiets-Specs. Vier davon sind Tier-1 (identifiziert über ihre `VariantResourceIds`, die
ausschließlich T1-Ressourcen-IDs referenzieren — `resource.tree`, `resource.fiber_plant`,
`resource.stone_deposit`, `resource.copper_vein` — im Gegensatz zu den drei T2-Specs, die
`resource.hardwood_tree`/`resource.swamp_hemp`/`resource.granite_deposit`/`resource.iron_vein`
referenzieren):

| Spec (Key) | Zeile | `VariantResourceIds` / `VariantSources` | `CloneVariant`-Aufrufe |
|---|---|---|---|
| Greenwood | 290-301 | `resource.tree` / `Tree` | 2 (Active+Exhausted) |
| Marsh | 302-313 | `resource.tree`/`Tree`, `resource.fiber_plant`/`FiberPlant` | 4 |
| Quarry | 314-325 | `resource.tree`/`Tree`, `resource.stone_deposit`/`StoneDeposit` | 4 |
| EmberRuins | 326-337 | `resource.tree`/`Tree`, `resource.copper_vein`/`CopperVein`* | 2 |

\* `resource.copper_vein` ist wie schon in 3a Sonderfall: `CloneVariants()`
(`AreaArtAssetBuilder.cs:123-142`) lädt für ihn das existierende Basis-Prefab direkt per
`AssetDatabase.LoadAssetAtPath` (Zeilen 131-135) statt `CloneVariant` aufzurufen — es entsteht
**keine** Datei `EmberRuins_copper_vein_*.prefab`. Bestätigt per Verzeichnislisting:
`Assets/_Game/Prefabs/Environment/AreaArtVariants/` enthält `EmberRuins_tree_Active/Exhausted`,
aber kein `EmberRuins_copper_vein_*`.

`HomeBase` (Zeilen 378-394) ist **kein** T1-Klon-Ziel: `VariantResourceIds`/`VariantSources`
sind `Array.Empty<string>()` (Zeilen 391-392) — `WriteVariants` schreibt für HomeBase ein
leeres `resourceVariants`-Array. Bestätigt in der Daten-Asset-Datei
`Assets/_Game/Data/AreaArt/AreaArt_HomeBase.asset:23`: `resourceVariants: []`. Siehe (c) für
die Konsequenz (HomeBase-Ressourcenknoten nutzen die Basis-Visuals, keine Zonenvariante).

Macht insgesamt 12 T1-`CloneVariant`-Aufrufe → 12 tatsächliche Varianten-Dateien (verifiziert
per Verzeichnislisting):

```
Greenwood_tree_Active/Exhausted.prefab                (2)
Marsh_tree_Active/Exhausted.prefab                    (2)
Marsh_fiber_plant_Active/Exhausted.prefab              (2)
Quarry_tree_Active/Exhausted.prefab                    (2)
Quarry_stone_deposit_Active/Exhausted.prefab           (2)
EmberRuins_tree_Active/Exhausted.prefab                (2)
```

(12 `CloneVariant`-Aufrufe insgesamt, alle über den regulären Schreibpfad → 12 Dateien. Der
EmberRuins/copper_vein-Sonderfall liegt außerhalb dieser 12 Aufrufe — er läuft über den
`LoadAssetAtPath`-Zweig ohne `CloneVariant` und erzeugt separat keine neue Datei.)

Quelle→Ziel-Muster identisch zu T2: `CloneVariant(spec, resourceId, sourcePath, exhausted)`
(`AreaArtVariantPrefabBuilder.cs:10-34`) lädt `Assets/_Game/Prefabs/Resources/Visuals/<Stem>_<State>.prefab`
per `PrefabUtility.LoadPrefabContents` (Zeile 20) und schreibt nach
`Assets/_Game/Prefabs/Environment/AreaArtVariants/<Key>_<safeResource>_<State>.prefab`
(Zeile 19, 31).

### Sonderfall `resource.tree`: `AddTreeIdentity` + `AddRecognitionMarker`

Für `resource.tree` ruft `CloneVariant` **beide** Zusatzschritte auf
(`AreaArtVariantPrefabBuilder.cs:22-26`), für alle anderen Ressourcen nur
`AddLeadResourceIdentity` (Zeile 29):

```csharp
if (resourceId == "resource.tree")            // Zeile 22
{
    AddTreeIdentity(root.transform, key, exhausted);      // Zeile 24
    AddRecognitionMarker(root.transform, exhausted);       // Zeile 25
}
```

**`AddTreeIdentity`** (Zeilen 36-63) hängt Gebiets-Silhouettenäste (`SilhouetteBranch`,
Cylinder-Primitive) für Marsh/Quarry/EmberRuins an — Marsh und EmberRuins je 2, Quarry 1
(Greenwood bekommt keine, `switch` Zeilen 48-61 hat keinen `case "Greenwood"`). Für die Farbe
dieser Äste gilt `bark = FirstMaterial(root)` (Zeile 47), **nur wenn `!exhausted`** — bei
`exhausted` bleibt `AreaIdentityGeometry` leer (keine Äste im verbrauchten Zustand).

**`AddRecognitionMarker`** (Zeilen 99-116) ist unabhängig vom Gebiet (kein `switch` auf `key`)
und legt ein **eigenes** Objekt `RecognitionMarker_AxeOchre` mit drei Kind-Boxen an: `AxeHandle`
(Holz, `(0.28, 0.13, 0.05)`), `AxeHead` (Metall, `(0.48, 0.52, 0.5)`), `OchreCloth` (ocker,
`(0.72, 0.42, 0.08)`) — Zeilen 113-115. Dieses Objekt entsteht für **beide** Zustände
(Active und Exhausted, mit `factor = 0.36` für Exhausted, Zeile 111) und für **alle vier**
T1-Baum-Varianten gleich. Der zugehörige Text in `AreaArtAssetBuilder.WriteVariants`
(`AreaArtAssetBuilder.cs:193`) lautet für `resource.tree`:
`"Axt mit ockerfarbenem Tuch am Stamm"` — bestätigt, dass die Axt+Ocker-Tuch-Kombination die
absichtliche Erkennungsmarke ist.

**Ersetzt der Marker das eingemalte G-009-Tuch?** Ja, strukturell: Das aktuelle
`Tree_Active.prefab` enthält ein Kind `CrossedAlphaGeometry` (Zeile 15) mit Material
`visual_tree_active_crown_alpha.mat` (Zeile 68-69), das laut `.mat`-Datei einen `_BaseMap`/
`_MainTex`-Texturslot hat (`Assets/_Game/Art/Geometry/Materials/visual_tree_active_crown_alpha.mat:28-33`)
— d. h. eine bemalte Alpha-Textur auf gekreuzten Ebenen, konsistent mit der Plan-Prämisse
„eingemaltes G-009-Tuch". `RecognitionMarker_AxeOchre` ist dagegen reine Vertexfarben-Boxgeometrie
ohne Textur (`EnsureColorMaterial`, Zeilen 152-165) — ein **eigenständiges Objekt**, keine
Textur-Ersetzung auf derselben Geometrie. Sobald Task 3 `Tree_Active.prefab` komplett neu baut
(kein `CrossedAlphaGeometry` mehr, nach T2-Muster — siehe `T2ResourceVisualBuilder.cs`, das
nirgends `SpriteRenderer`/Alpha-Ebenen erzeugt), verschwindet das gemalte Tuch mit der gesamten
alten Geometrie; der Marker bleibt als separates, unabhängig weiterlaufendes Objekt bestehen.
Kein Konflikt, aber auch keine Übernahme von Farbwerten — der Marker war schon vorher unabhängig
vom Sprite-Inhalt.

### `FirstMaterial(root)` nach dem T1-Umbau — Risiko für `SilhouetteBranch`

`FirstMaterial` (`AreaArtVariantPrefabBuilder.cs:142-150`) gibt
`root.GetComponentInChildren<Renderer>(includeInactive: true).sharedMaterial` zurück — den
Renderer des **ersten** in der Hierarchie gefundenen Objekts, aus der frisch geladenen
**Quelle** `Tree_Active.prefab` (nicht aus der Variante).

**Heute:** `Tree_Active.prefab` hat mehrere Kind-Renderer mit unterschiedlichen Materialien
(`Trunk` → `visual_tree_active_bark.mat`, Zeile 191-192; `Geometry_A14`/`CrossedAlphaGeometry`
→ `visual_tree_active_crown_alpha.mat`; `ContactShadow` → ein drittes Material) — welches davon
`FirstMaterial` konkret trifft, hängt von der Geschwister-Reihenfolge im Transform ab und wurde
hier nicht aus der YAML-Byte-Reihenfolge abgeleitet (die Datei-Reihenfolge ist nicht die
Hierarchie-Reihenfolge). Für die Bewertung des T1-Umbaus ist das aber irrelevant, siehe unten.
Die Bark-Referenzfarbe `visual_tree_active_bark.mat` trägt `_BaseColor: (0.26, 0.14, 0.07, 1)`
(Zeile 68 der `.mat`-Datei) — ein klassisches farbwert-getriebenes Material, unabhängig von
Vertexfarben.

**Nach dem T1-Umbau (Task 3):** `T1ResourceVisualBuilder` wird — analog zu
`T2ResourceVisualBuilder.Teil()` (`T2ResourceVisualBuilder.cs:91-100`, insbesondere Zeile 98:
`gameObject.AddComponent<MeshRenderer>().sharedMaterial = EidrenWorldStyleAssets.EnsureWorldMaterial();`)
— **jedem** Bauteil dasselbe eine geteilte Material zuweisen:
`M_EidrenWorld_VertexLit` (`Assets/_Game/Art/World/Materials/M_EidrenWorld_VertexLit.mat`,
definiert in `EidrenWorldStyleAssets.cs:12-30`). Weil dann **alle** Kind-Renderer dasselbe
Material tragen, ist die genaue Traversierungsreihenfolge irrelevant:
`FirstMaterial(root)` liefert **deterministisch** `M_EidrenWorld_VertexLit`.

**Bewertung — braucht einen Hinweis für Task 3/4:** `M_EidrenWorld_VertexLit`
(`Assets/_Game/Shaders/World/EidrenWorldVertexLit.shader:62`) berechnet das Albedo **rein aus
den Vertexfarben** der Mesh (`albedo = input.color.rgb * _Tint.rgb`, `_Tint` default weiß) —
es gibt keine Texture/`_BaseColor`-Fallback-Eigenschaft. Vertexfarben schreibt im Projekt
ausschließlich `EidrenMeshFactory.Loft`/`TaperedBox` über `mesh.SetColors(_farben)`
(`Assets/_Game/Editor/EidrenMeshFactory.cs:195`). Die `SilhouetteBranch`-Objekte in
`AddBranch` (`AreaArtVariantPrefabBuilder.cs:118-128`) sind aber **keine** Fabrik-Meshes,
sondern `GameObject.CreatePrimitive(PrimitiveType.Cylinder)` (Zeile 120) — Unitys eingebautes
Cylinder-Mesh, das nirgends im Projekt mit einem `COLOR`-Vertexstream versehen wird. Ergebnis:
Sobald `bark = FirstMaterial(root)` auf `M_EidrenWorld_VertexLit` zeigt, rendern die
`SilhouetteBranch`-Äste in Marsh/Quarry/EmberRuins mit einer vom fehlenden Vertexfarben-Stream
abhängigen, nicht mehr absichtlich gesetzten Farbe (plattform-/GPU-abhängiger Default für ein
fehlendes Attribut) statt der vorherigen bewussten Rindenfarbe `(0.26, 0.14, 0.07)`. **Das ist
eine Verhaltensänderung, die dokumentiert und in Task 3 oder 4 geprüft/adressiert werden
sollte** — entweder durch einen Ersatz für `bark` (z. B. eine feste Farbkonstante statt
`FirstMaterial(root)`) oder durch eine bewusste Freigabe des neuen Erscheinungsbilds nach
Sichtprüfung in Task 2/5.

---

## (b) Signaturvorschlag `BuildTierOne()`

`AreaArtAssetBuilder.BuildTierTwo()` existiert bereits seit 3a
(`AreaArtAssetBuilder.cs:70-84`) und nutzt den seit 3a ausgelagerten `CloneVariants`-Helfer
(Zeilen 123-142):

```csharp
private static readonly string[] TierTwoKeys = { "TwilightGrove", "VeilMarsh", "GreyRifts" };

[MenuItem("Eidren/V0.2/Stilumbau/T2-Zonenvarianten klonen")]
public static void BuildTierTwo()
{
    EnsureFolders();
    List<AreaSpec> tierTwo = new List<AreaSpec>(Specs()).FindAll((AreaSpec s) => Array.IndexOf(TierTwoKeys, s.Key) >= 0);
    foreach (AreaSpec item in tierTwo)
    {
        CloneVariants(item, out _, out _);
    }
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log("Tier-two zone variants cloned.");
}
```

Vorschlag `BuildTierOne()`, exaktes Spiegelbild mit T1-Spec-Filterschlüsseln
(`Greenwood`, `Marsh`, `Quarry`, `EmberRuins` — **ohne** `HomeBase`, da dessen
`VariantResourceIds` ohnehin leer sind, siehe (a)):

```csharp
private static readonly string[] TierOneKeys = { "Greenwood", "Marsh", "Quarry", "EmberRuins" };

[MenuItem("Eidren/V0.2/Stilumbau/T1-Zonenvarianten klonen")]
public static void BuildTierOne()
{
    EnsureFolders();
    List<AreaSpec> tierOne = new List<AreaSpec>(Specs()).FindAll((AreaSpec s) => Array.IndexOf(TierOneKeys, s.Key) >= 0);
    foreach (AreaSpec item in tierOne)
    {
        CloneVariants(item, out _, out _);
    }
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log("Tier-one zone variants cloned.");
}
```

Signatur: `public static void BuildTierOne()`, keine Parameter, neues MenuItem
`Eidren/V0.2/Stilumbau/T1-Zonenvarianten klonen`. `CloneVariants` bleibt unverändert
wiederverwendet (inkl. des `resource.copper_vein`-Sonderfalls, Zeilen 131-135, der generisch
und nicht Tier-spezifisch ist). Berührt ausschließlich die vier T1-Specs; schreibt — wie
`BuildTierTwo` — bewusst weder `AreaArt_*.asset` noch `Zone_*.asset` neu (die
Variant-Prefab-GUIDs bleiben stabil, vorhandene Referenzen in `AreaArt_*.asset` bleiben
gültig). T2-Varianten, HomeBase und alle Zonen-/AreaArt-Daten bleiben unangetastet.

---

## (c) Laufzeit-Spawning für die fünf T1-Zonen inkl. HomeBase

### Szenenbau legt nur eine leere `PopulationRoot` an — auch für HomeBase

`EidrenSceneStructureBuilder.BuildZoneScene` (`Assets/_Game/Editor/EidrenSceneStructureBuilder.cs:224-...`)
legt **unabhängig vom `homeBase`-Flag** nur eine leere `PopulationRoot` an
(Zeile 260: `GameObject populationRoot = Child(zoneRoot.transform, "PopulationRoot");` — kein
`if (!homeBase)` davor, im Gegensatz zu `PopulateEnemies`/`NavMeshSurface`, die für HomeBase
explizit übersprungen werden, Zeilen 261-280). Bestätigt durch Live-Grep in
`Assets/_Game/Scenes/HomeBase.unity:124`: `m_Name: PopulationRoot` existiert dort ebenfalls,
leer wie bei den Zonenszenen.

`BuildResourceZoneScenes()` (`EidrenSceneStructureBuilder.cs:117-...`) baut die vier
T1-Außenzonen (Greenwood Zeile 125, Quarry 126, Marsh 127, EmberRuins 128) — HomeBase wird hier
**nicht** neu gebaut (das übernehmen die volleren Einstiege in den Zeilen 68/88), ist aber davon
unabhängig bereits vorhanden.

### Laufzeitfüllung ist zonen-generisch, nicht T1-/T2-spezifisch

`ZoneController.BuildZoneResources` (`Assets/_Game/Scripts/Composition/ZoneController.cs:332-340`)
instanziiert `ZoneResourcePopulator` und ruft `Populate()` für **jede** Zone unbedingt auf
(Zeilen 336-337) — der einzige Filter liegt in `ZoneResourcePopulator.Populate()` selbst
(`Assets/_Game/Scripts/Composition/ZoneResourcePopulator.cs:37`):
`if (... || !_zone.Definition.ResourcesAllowed) return null;`.

**`ResourcesAllowed` ist für HomeBase `true`:** `Assets/_Game/Data/Zones/Zone_HomeBase.asset:28`:
`resourcesAllowed: 1`. Bestätigt zusätzlich durch einen bestehenden Test:
`EmptyStartAndHomeSupplyTests.HomeBase_CarriesTheFiniteOneTimeSupply`
(`Assets/_Game/Editor/Tests/EmptyStartAndHomeSupplyTests.cs:65-75`), der prüft:
`home.ResourcesAllowed == true`, `home.ResourceRespawnMode == ResourceRespawnMode.None`, und die
Allokationen `resource.tree: 26`, `resource.stone_deposit: 18`, `resource.fiber_plant: 10`
sowie `SideNodeAllocations`: `resource.berry_bush: 4`. Diese Zahlen stehen wortgleich in
`Zone_HomeBase.asset:30-38` (Datenquelle: `ResourceContentBuilder.ConfigureZoneAllocations`,
`Assets/_Game/Editor/ResourceContentBuilder.cs:271`:
`SetAllocations("Zone_HomeBase", ResourceRespawnMode.None, (berry, 4), (tree, 26), (stone_deposit, 18), (fiber_plant, 10))`).

**Ergebnis:** HomeBase spawnt zur Laufzeit vier der fünf T1-Ressourcentypen als endliche,
einmalige Zuteilung (Tree, StoneDeposit, FiberPlant als Hauptknoten, BerryBush als
Nebenknoten) — nur CopperVein fehlt (kein Kupfer in der Heimatbasis). Die vier Außenzonen
Greenwood/Marsh/Quarry/EmberRuins spawnen alle fünf T1-Typen mit `ResourceRespawnMode.OnZoneEntry`
(`ResourceContentBuilder.cs:272-275`; BerryBush zusätzlich in **allen** neun Zonen als
Nebenknoten inkl. der drei T2-Zonen, Zeilen 271-278).

### HomeBase-Knoten nutzen mangels Variante die Basis-Visuals, nicht die Zonenvariante

`ResourceNodeVariantApplicator.Apply` (`Assets/_Game/Scripts/Composition/ResourceNodeVariantApplicator.cs:9-34`)
tauscht das Visual nur, wenn `areaArt.TryGetResourceVariant(...)` `true` liefert (Zeile 11).
`ZoneAreaArtDefinition.TryGetResourceVariant` (`Assets/_Game/Scripts/Data/ZoneAreaArtDefinition.cs:52-64`)
durchsucht `ResourceVariants` — für HomeBase leer (`AreaArt_HomeBase.asset:23`:
`resourceVariants: []`, weil `AreaSpec.HomeBase.VariantResourceIds` leer ist, siehe (a)). Für
HomeBase liefert `Apply` daher immer `false` und **die im Node-Prefab bereits gebackenen
Basis-Visuals bleiben aktiv** — das sind exakt die `Visuals/Tree_Active.prefab` usw., die Task 3
neu baut (siehe `ResourceNodeVisualTable.BuildAll`, `Assets/_Game/Editor/ResourceNodeVisualTable.cs:29-38`,
lädt direkt `Visuals/<Stem>_Active/Exhausted.prefab`, keine Zonenvariante). Für Greenwood/Marsh/
Quarry/EmberRuins liefert `TryGetResourceVariant` dagegen `true` (deren `AreaArt_*.asset`
enthält die per (a) geklonten Varianten) — dort ersetzt `Apply` das Visual live durch die
Zonenvariante (`ResourceNodeVariantApplicator.cs:26-29`).

### Sind T1-Visuals/Nodes in irgendeiner Szene gebacken referenziert?

**Geprüft: alle fünf T1-Node-Prefab-GUIDs in `Zone_Greenwood.unity` und `HomeBase.unity`.**

```
Tree        (guid 7f1b3d8776e942b4692950428b1310b4): 0 Treffer in beiden Szenen
StoneDeposit(guid 91b1a17d8a47d89409adfd75672af408): 0 Treffer
FiberPlant  (guid 6d908ca57d7f3d54ebfad14911bc1ff5): 0 Treffer
CopperVein  (guid ac2664d614a69cf45804b37eb83072bd): 0 Treffer
BerryBush   (guid 49c167512bfbd6c44b5e15dc18206ccc): 0 Treffer
```

`ProjectSettings/EditorSettings.asset` setzt `m_SerializationMode: 2` (Force Binary). Der
`file`-Befehl zeigt: **die meisten** Szenen sind binär (`Zone_Greenwood.unity`,
`Zone_Marsh.unity`, `Zone_Quarry.unity`, `Zone_EmberRuins.unity`, `Zone_TwilightGrove.unity`,
`Zone_VeilMarsh.unity`, `Zone_GreyRifts.unity`, `EidraForge.unity` = alle `data`) — **aber
`HomeBase.unity` und `Bootstrap.unity` sind `ASCII text`** (ungewöhnliche Abweichung, nicht
weiter untersucht, da für Task 1 nicht relevant). Für `HomeBase.unity` konnte die GUID-Suche
daher direkt und zuverlässig als Textsuche laufen (kein Binär-Fallback nötig); für
`Zone_Greenwood.unity` wurde zusätzlich ein Byte-Grep auf die ASCII-Repräsentation der GUID
versucht (ergab ebenfalls 0 Treffer) — das ist bei binärer Serialisierung aber kein sicherer
Negativbeweis, sondern eine Zusatzprüfung; die belastbare Aussage stützt sich auf den
Code-Pfad unten.

**Zur im Auftrag genannten „StorageChest-Historie": In diesem Repository und in den
vorhandenen Etappe-1/2/3-Dokumenten (`STILUMBAU_E1_*.md`, `STILUMBAU_E2_*.md`,
`STILUMBAU_E3_BAKEKETTE.md`) wurde keine Erwähnung von `StorageChest` als Beleg für
„Szene enthält gebackenen Inhalt" gefunden — dieser konkrete Präzedenzfall lässt sich hier
nicht nachweisen und wird daher NICHT als Beleg verwendet.** Real nachweisbar ist stattdessen:
`HomeBase.unity` enthält **41 `PrefabInstance`-Blöcke** (`grep -c "^--- !u!1001"` = 41), mit vier
eindeutigen Quell-GUIDs, die alle auf `StyleProof`-Dekorationsprefabs auflösen
(`SP_Rock_Small`, `SP_GroundCover_Grass`, `SP_Plant_Bush`, `SP_EidrenRune` — exakt die
`Decorations`-Liste aus `AreaSpec.HomeBase`, `AreaArtAssetBuilder.cs:393`). Das belegt allgemein:
**Szenen können und tun gebackenen Inhalt enthalten** — hier aber nur gestreute Dekoration
(gebaut von `AreaArtSceneBuilder`, nicht von einem T1-Ressourcen-Builder), keine
T1-Ressourcenknoten oder -Visuals. `StorageChest.prefab`
(`Assets/_Game/Prefabs/Stations/StorageChest.prefab`, guid `bed0f5490a6014e4b955be7cdc8845ba`)
wurde ebenfalls mit 0 Treffern in allen elf Szenendateien geprüft — auch dieses Stations-Prefab
ist in keiner Szene gebacken, sondern (wie bei Stationen/Gebäuden üblich) laufzeit-/
platzierungsgetrieben.

### Schlussfolgerung

Wie in `STILUMBAU_E3_BAKEKETTE.md` Abschnitt (c) für T2 festgestellt, gilt dasselbe
architektonische Muster für alle fünf T1-Zonen inkl. HomeBase: Ressourcenknoten werden
**nicht** in Szenen gebacken, sondern prozedural zur Laufzeit über GUID-Referenzen auf
`ResourceNodeDefinition`/`ZoneAreaArtDefinition`-Assets instanziiert
(`ZoneResourcePopulator.Spawn`, `ZoneResourcePopulator.cs:54-77`;
`ResourceNodeVariantApplicator.Apply`, s. o.). **Ein Szenen-Rebake ist nach dem T1-Umbau nicht
nötig** — das gilt provably für Greenwood/Marsh/Quarry/EmberRuins (Code-Pfad + GUID-Nullbefund)
und ebenso für HomeBase (Code-Pfad + GUID-Nullbefund in der textuell direkt durchsuchbaren
`HomeBase.unity`). Der einzige Unterschied zu T2: HomeBase nutzt die T1-Basis-Visuals direkt
(keine Zonenvariante, s. o.), während die vier T1-Außenzonen die per (a) geklonten Varianten
zur Laufzeit einsetzen.

---

## (d) Die zehn Sollhöhen

Quelle: `VisualScaleTableBuilder.BuildEntries()`, `Assets/_Game/Editor/VisualScaleTableBuilder.cs:89-98`.
Toleranz laut `VisualScaleTests.EveryBakedPrefab_MatchesItsTableHeight`
(`Assets/_Game/Editor/Tests/VisualScaleTests.cs:83-101`): Zeile 94 prüft
`Mathf.Abs(measured - expected) > expected * 0.01f` — **1 % der Sollhöhe**, absolut ausgewertet.
(Die benannte Konstante `HeightTolerance = 0.01f`, Zeile 66, wird in diesem Test nicht direkt
verwendet, sondern die Toleranz ist inline als `expected * 0.01f` codiert — inhaltlich derselbe
Wert.) Für den `ArtworkMissing`-Sonderfall (Sprite statt Geometrie) würde `expected` aus
`FittedHeight` berechnet (Zeile 93) — für die zehn T1-Einträge unten ist keiner
`artworkMissing` außer den beiden `fiber_plant`-Zuständen und `stone_deposit.exhausted`, deren
`expected` in diesem Fall trotzdem `entry.Height` bleibt, weil die künftigen Baked-Prefabs
keine Sprites mehr sind (`visual.IsSprite == false` nach dem Umbau) — die Bedingung
`entry.ArtworkMissing && visual.IsSprite` in Zeile 93 greift dann nicht mehr.

| Visual-ID | Sollhöhe (m) | Zeile | Collider-Maß (`ColliderSize`) | Bemerkung im Quelltext |
|---|---|---|---|---|
| `visual.tree.active` | 6,0 | 89 | `(1.2, 2.4, 1.2)` | „Blocker ist der Stamm, nicht die Krone." |
| `visual.tree.exhausted` | 0,7 | 90 | `(0,0,0)` — kein Blocker | „heute ein Zylinder" (Platzhalter-Hinweis) |
| `visual.berry_bush.active` | 0,9 | 91 | `(0,0,0)` — kein Blocker | „Referenz für richtigen Blickwinkel" |
| `visual.berry_bush.exhausted` | 0,7 | 92 | `(0,0,0)` — kein Blocker | „Abgeerntet bleibt der Strauch stehen." |
| `visual.fiber_plant.active` | 0,9 | 93 | `(0,0,0)` — kein Blocker | „auf 45 % der Spielerhöhe" |
| `visual.fiber_plant.exhausted` | 0,25 | 94 | `(0,0,0)` — kein Blocker | „heute ein Würfel" (Platzhalter-Hinweis) |
| `visual.stone_deposit.active` | 1,1 | 95 | `(1.4, 1.0, 1.4)` | — |
| `visual.stone_deposit.exhausted` | 0,5 | 96 | `(0,0,0)` — kein Blocker | „heute eine Kugel" (Platzhalter-Hinweis) |
| `visual.copper_vein.active` | 1,3 | 97 | `(1.6, 1.2, 1.6)` | — |
| `visual.copper_vein.exhausted` | 1,3 | 98 | `(1.6, 1.2, 1.6)` | „Wirtsfels bleibt, nur das Erz verschwindet." |

Die drei „Platzhalter-Hinweise" (Zylinder/Würfel/Kugel) markieren `artworkMissing: true`
(implizit außer bei `fiber_plant`, wo es explizit `true` steht) — das sind exakt die
Prefabs, die Task 3 durch echte Fabrik-Geometrie ersetzt; nach dem Umbau sollte
`ArtworkDebt_OnlyEverShrinks` (`VisualScaleTests.cs:153-158`) entsprechend sinken, sofern
Task 3/4 die `artworkMissing`-Markierung in der Tabelle mit anpassen — das ist außerhalb des
Scopes von Task 1 und wird hier nur als Beobachtung vermerkt, nicht als Auftrag.

Collider-Maß-Herkunft und Verwendung siehe (e).

---

## (e) Node-Prefabs und ihre Collider-Quellen

### Existenz

Alle fünf T1-Node-Prefabs existieren unter `Assets/_Game/Prefabs/Resources/Nodes/`
(Verzeichnislisting bestätigt, inkl. `.meta`-GUIDs):

| Node-Prefab | GUID (`.meta`) |
|---|---|
| `Tree.prefab` | `7f1b3d8776e942b4692950428b1310b4` |
| `StoneDeposit.prefab` | `91b1a17d8a47d89409adfd75672af408` |
| `FiberPlant.prefab` | `6d908ca57d7f3d54ebfad14911bc1ff5` |
| `CopperVein.prefab` | `ac2664d614a69cf45804b37eb83072bd` |
| `BerryBush.prefab` | `49c167512bfbd6c44b5e15dc18206ccc` |

Wie schon für T2 in `STILUMBAU_E3_BAKEKETTE.md` Abschnitt (d) festgestellt, sind auch die
T1-Node-Prefabs **gebackene Kopien, keine Prefab-Instanzen**: `grep -c "^--- !u!1001"` ergibt
für alle fünf Dateien `0`.

### Collider-Quellen

`ResourceContentBuilder.NodePrefab(ResourceNodeDefinition definition)`
(`Assets/_Game/Editor/ResourceContentBuilder.cs:217-247`) baut je Node **zwei** mögliche
Collider:

1. **Trigger** (immer): `SphereCollider`, `isTrigger = true`, `radius = definition.InteractionRange * 0.45f`
   (Zeilen 220-222) — unabhängig von der Größentabelle, aus `interactionDuration`/`interactionRange`
   der `ResourceNodeDefinition` (für T1 gesetzt in `Definition(...)`-Aufrufen,
   `ResourceContentBuilder.cs:40-44`: Tree 1.8s, StoneDeposit 2s, FiberPlant 0.9s,
   CopperVein 2.25s, BerryBush 0.9s Interaktionsdauer; `interactionRange` einheitlich 2.7,
   Zeile 169).
2. **Blocker** (nur wenn `definition.BlocksNavigation == true`): `CapsuleCollider`, `radius = size.x * 0.5f`,
   `height = size.y`, `center = Vector3.up * height * 0.5f` (Zeilen 231-235), wobei
   `size = VisualScaleTableBuilder.Load().Require(ActiveVisualId(definition.Id)).ColliderSize`
   (Zeile 226) — **immer** die `*.active`-Zeile der Größentabelle, auch wenn gerade der
   Exhausted-Zustand aktiv ist (ein Node hat nur einen physischen Blocker für beide Zustände).
   Ist `size.sqrMagnitude <= 0f`, wirft der Builder eine `InvalidOperationException`
   (Zeile 227-230) — `BlocksNavigation` und ein Non-Zero-`ColliderSize`-Eintrag müssen also
   zusammenpassen.

`BlocksNavigation` ist für T1 in `ResourceContentBuilder.cs:40-44` gesetzt:
`Tree: true`, `StoneDeposit: true`, `FiberPlant: false`, `CopperVein: true`, `BerryBush: false`
— deckungsgleich mit den Non-Zero-`ColliderSize`-Werten aus (d) (`tree.active`,
`stone_deposit.active`, `copper_vein.active` haben Werte; die übrigen sind `(0,0,0)`).

`definition.ActiveVisualPrefab`/`ExhaustedVisualPrefab` (Zeilen 237, 240) stammen — wie bei T2 —
aus den **Basis-Visuals**, nicht den Zonenvarianten: `ResourceNodeVisualTable.BuildAll()`
(`Assets/_Game/Editor/ResourceNodeVisualTable.cs:29-38`) lädt für jede der fünf T1-IDs
direkt `Visuals/<Stem>_Active.prefab`/`_Exhausted.prefab` (`Pair`/`RequireVisual`,
Zeilen 41-55) — generisch für alle neun Ressourcentypen, kein T1-/T2-Unterschied.

### `RebuildTierOneNodePrefabs()` — Spiegel von `RebuildTierTwoNodePrefabs()`

Vorbild `RebuildTierTwoNodePrefabs()` (`ResourceContentBuilder.cs:97-113`):

```csharp
private static readonly string[] TierTwoResourceIds =
{
    "resource.hardwood_tree", "resource.swamp_hemp", "resource.granite_deposit", "resource.iron_vein"
};

[MenuItem("Eidren/V0.2/Stilumbau/T2-Node-Prefabs neu bauen")]
public static void RebuildTierTwoNodePrefabs()
{
    foreach (string id in TierTwoResourceIds)
    {
        string assetName = Path.GetFileNameWithoutExtension(NodePrefabPath(id));
        ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(DataFolder + "/" + assetName + ".asset");
        if (definition == null) throw new FileNotFoundException(...);
        NodePrefab(definition);
    }
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log("Tier-two node prefabs rebuilt.");
}
```

Vorschlag `RebuildTierOneNodePrefabs()` mit den fünf T1-Ressourcen-IDs, geladen aus den
bereits vorhandenen `ResourceNodeDefinition`-Assets (bestätigt vorhanden:
`Assets/_Game/Data/Resources/Tree.asset`, `StoneDeposit.asset`, `FiberPlant.asset`,
`CopperVein.asset`, `BerryBush.asset`):

```csharp
private static readonly string[] TierOneResourceIds =
{
    "resource.tree", "resource.stone_deposit", "resource.fiber_plant", "resource.copper_vein", "resource.berry_bush"
};

[MenuItem("Eidren/V0.2/Stilumbau/T1-Node-Prefabs neu bauen")]
public static void RebuildTierOneNodePrefabs()
{
    foreach (string id in TierOneResourceIds)
    {
        string assetName = Path.GetFileNameWithoutExtension(NodePrefabPath(id));
        ResourceNodeDefinition definition = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(DataFolder + "/" + assetName + ".asset");
        if (definition == null)
        {
            throw new FileNotFoundException("Ressourcendefinition fehlt fuer '" + id + "': " + assetName + ".asset");
        }
        NodePrefab(definition);
    }
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log("Tier-one node prefabs rebuilt.");
}
```

Signatur: `public static void RebuildTierOneNodePrefabs()`, keine Parameter. Berührt nur die
fünf bestehenden T1-`ResourceNodeDefinition`-Assets (deren `ActiveVisualPrefab`/
`ExhaustedVisualPrefab`-Referenzen unverändert bleiben — nur der Node-Prefab-Inhalt wird aus
den frisch gebauten Basis-Visuals neu gebacken); nicht `ItemContentAssetBuilder`,
`ConfigureZoneAllocations`, `BuildCatalog` oder einen T2-Ressourcentyp. Weil `NodePrefab()`
(Zeile 226) die Collider-Maße aus der Größentabelle liest, muss Task 3 die
`VisualScaleTable` **vor** einem `RebuildTierOneNodePrefabs()`-Lauf mit den finalen T1-Höhen
konsistent halten (die zehn Werte aus (d) sind bereits so hinterlegt — Task 3 iteriert die
Geometrie gegen sie, nicht umgekehrt).

---

## Zusammenfassung für Task 3/4

| Frage | Antwort |
|---|---|
| (a) T1-Klonwege | 4 T1-Specs (Greenwood/Marsh/Quarry/EmberRuins), 12 `CloneVariant`-Aufrufe → 12 Dateien (copper_vein-Sonderfall liegt außerhalb dieser 12 und erzeugt separat keine neue Datei). `resource.tree` bekommt zusätzlich `AddRecognitionMarker` (Axt+Ockertuch, eigenes Objekt) — ersetzt strukturell das gemalte G-009-Tuch. Risiko: `FirstMaterial(root)` liefert nach dem T1-Umbau immer `M_EidrenWorld_VertexLit`; die `SilhouetteBranch`-Primitive haben keine Vertexfarben → Farbe der Gebietsäste wird undefiniert, braucht Prüfung/Hinweis in Task 3/4. |
| (b) `BuildTierOne()` | Spiegelbild von `BuildTierTwo()`, Keys `Greenwood/Marsh/Quarry/EmberRuins`, nutzt `CloneVariants` unverändert. |
| (c) Laufzeit-Spawning | Bestätigt für alle fünf T1-Zonen inkl. HomeBase (`resourcesAllowed: 1`, belegt durch `EmptyStartAndHomeSupplyTests`). Kein T1-Visual/-Node ist in `Zone_Greenwood.unity` oder `HomeBase.unity` als GUID auffindbar (0 Treffer, HomeBase textuell direkt geprüft). HomeBase nutzt mangels eigener Zonenvariante die Basis-Visuals direkt. „StorageChest-Historie" aus dem Auftrag konnte in den vorhandenen Dokumenten NICHT verifiziert werden — stattdessen belegt: HomeBase.unity hat 41 gebackene Dekorations-`PrefabInstance`s (StyleProof), keine Ressourcenknoten. Kein Szenen-Rebake nötig. |
| (d) Zehn Sollhöhen | Tabelle oben, Quelle `VisualScaleTableBuilder.cs:89-98`, Toleranz 1 % der Sollhöhe (`VisualScaleTests.cs:94`). |
| (e) Node-Prefabs | Alle fünf existieren, gebackene Kopien (0 `PrefabInstance`-Blöcke). Trigger immer `SphereCollider` aus `InteractionRange`; Blocker nur bei `BlocksNavigation` (Tree/StoneDeposit/CopperVein) als `CapsuleCollider` aus der `*.active`-`ColliderSize` der Größentabelle. `RebuildTierOneNodePrefabs()` als Spiegel von `RebuildTierTwoNodePrefabs()` vorgeschlagen. |
