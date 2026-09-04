# Stilumbau Weltinventar — Entwurf

**Stand:** 7. August 2026 · **Status:** Entwurf, vom Auftraggeber abschnittsweise freigegeben

**Ziel:** Alle bestehenden prozeduralen 3D-Weltobjekte werden im „Vorschau-Stil"
nachgebaut: segmentierte Geometrie (Planken, Bögen, Sockel, Beschläge),
Ton-Variation pro Fläche und Kontakt-Abdunklung — technisch über Vertexfarben
mit einem gemeinsamen Shader. Die Farbwelt der bestehenden Materialien bleibt
erhalten. Kisten erhalten zusätzlich einen sichtbaren Loot-Füllstand.

## Umfang

Alle prozedural gebauten Weltobjekte, in fünf Etappen:

1. **Weltkisten** — 3 Familien (Common, Guarded, Hidden) × 4 Sichtzustände
2. **Forge-Kisten + Stationen** — 8 Forge-Typen, StorageChest, Workbench, DeathBag
3. **Ressourcenknoten** — 9 Typen × Active/Exhausted × Zonenvarianten
4. **Props** — 16 SP_*-Objekte (Bäume, Felsen, Ruinen, Pflanzen, Bodendecker)
5. **Gebäude** — 9 BLD_*-Typen samt Wand-/Dachvarianten

**Nicht im Umfang:** Figuren (laufen über die GLB-Pipeline), WorldItem,
UI/WorldMap, neue Paletten (Farben kommen aus dem Bestand), neue
Spielmechanik. Der Teilgeleert-Zustand wird für Forge-Kisten **nicht** neu
eingeführt, falls er dort nicht existiert.

## Ausgangslage

- Alle Zielobjekte entstehen über C#-Editor-Builder (`WorldChestContentBuilder`,
  `ForgeContainerVisualBuilder`, `StorageContentBuilder`,
  `PlayerDeathContentBuilder`, `ResourceContentBuilder`,
  `T2ResourceVisualBuilder`, `StyleProofContentBuilder`,
  `AreaArtVariantPrefabBuilder` u. a.). `TaperedBox`/`Wedge`-Helfer liegen als
  Privatkopien in mehreren Buildern.
- Farbgebung heute: ein Einfarb-Material pro Bauteil (URP Lit, `_BaseColor`).
- `WandererVertexLit` (Vertexfarben-Shader mit Skinning-Kontext) existiert
  bereits und dient als Vorlage.
- Bekannte Lücke: Die Forge-Kisten-Prefabs unter
  `Assets/_Game/Prefabs/Containers/Forge` fehlen; die Kisten existieren nur
  eingebacken in `EidraForge.unity`. Etappe 2 behebt das mit.
- Im Spiel sehen `Opened` und `PartiallyEmptied` bei Weltkisten identisch aus
  (beide zeigen nur `DarkInterior`).

## Architektur

### EidrenMeshFactory (neu, Editor-only)

`Assets/_Game/Editor/EidrenMeshFactory.cs`, statische Klasse. API:

- `TaperedBox(size, topScale, color)` — Ersatz der Builder-Privatkopien,
  Farbe als Vertexfarbe im Mesh
- `Wedge(size, color)` — wie bestehende Wedge-Primitive, mit Vertexfarbe
- `Loft(profile[], color)` — Profilstapel für Bögen (Kistendeckel),
  Kronenstufen, Felsformen; C#-Gegenstück zum `loft`-Baukasten der
  Wanderer/Wildling-HTML-Quellen

Stilregeln, von jeder Funktion automatisch angewandt:

- **Ton-Variation:** ±6 % Helligkeit pro Fläche, deterministisch aus der
  Flächenposition gehasht (Regeneration ⇒ identisches Ergebnis)
- **Kontakt-AO:** Vertices nahe y=0 werden zum Boden hin abgedunkelt
  (Abdunklung in Kontaktbereichen gemäß G-003 direkt im Asset)
- **Flat Shading:** getrennte Normalen pro Fläche, keine geglätteten Kanten
- Vertexfarben in sRGB, wie bei Wanderer/Wildling

Meshes werden wie bisher als `.asset` unter dem `MeshRoot` des jeweiligen
Builders gespeichert (Benennung nach dem Muster der bestehenden
`Forge_0XX_*`-Assets).

### Shader und Material

- **`Eidren/World/VertexLit`** (neu): Ableger von `WandererVertexLit` ohne
  Skinning-Bezug. Vertexfarbe × Hauptlicht mit Schattenempfang + Ambient-SH,
  Gamma+LDR-konform, `_Tint`-Property (Laufzeit-Einfärbung: Treffer-Blitz,
  Bau-Vorschau). ShadowCaster/DepthOnly per `UsePass` aus dem URP-Lit.
- **`M_EidrenWorld_VertexLit`** (neu, eines für alle Weltobjekte): ersetzt die
  Einfarb-Materialien der umgestellten Objekte. Die bestehenden Farbwerte
  werden vor der Umstellung je Objekt aus den `.mat`-Dateien ausgelesen und in
  die Builder-Spezifikationen übernommen. Die alten Materialien bleiben bis
  nach Etappe 5 liegen und werden erst dann entfernt.

### Loot-Füllstand (Weltkisten, Forge-Belohnungskisten)

- Fabrik baut zwei Kindobjekte pro Weltkiste: `LootFill_Full` (gehäufter
  Goldhaufen) und `LootFill_Partial` (flacher Rest), Goldpalette als
  Vertexfarben.
- `WorldChestVisual.Apply()` schaltet nach bestehendem `SetActive`-Muster:
  Full bei `Opened`, Partial bei `PartiallyEmptied`, keins bei `Emptied`.
- `WorldChestVisual.Configure(...)` erhält zwei zusätzliche optionale
  Parameter (Standard `null`); Bestandsaufrufer kompilieren unverändert.
- Forge-Belohnungskisten nutzen dieselben Loot-Bausteine über ihren
  vorhandenen Öffnungszustand; existiert dort kein Teilzustand, gibt es nur
  voll/leer. Keine Änderung an Save-Daten oder `EidraForgePersistentState`.

## Etappen und Datenfluss

Jede Etappe: Sicherung → Builder-Umstellung → Menüpunkt-Lauf regeneriert
Meshes + Prefabs → Tests → Vorher/Nachher-Captures → Abnahme. Kein Prefab
wird manuell editiert; jeder Stand ist per Builder reproduzierbar.

| Etappe | Gruppe | Builder | Besonderheit |
| --- | --- | --- | --- |
| 1 | Weltkisten | `WorldChestContentBuilder` | Pilot: validiert Fabrik, Shader, Capture-Abnahme; Deckelbogen, Sockel, Beschläge, Loot-Füllstand |
| 2 | Forge-Kisten (Stationen entfallen, siehe Nachtrag) | `ForgeContainerVisualBuilder` | legt die fehlenden Forge-Prefabs an; danach `EidraForgeSceneBuilder`-Neubau der Szene. **Nachtrag 07.08.2026:** StorageChest, Workbench und DeathBag tragen seit 05.08. handgebaute `Geometry_A14`-Geometrie und bleiben per Auftraggeber-Entscheid unangetastet |
| 3 | Ressourcenknoten | `ResourceContentBuilder`, `T2ResourceVisualBuilder` | gestufte Kronen-Lofts, strukturierte Stämme (zahlt auf G-005 ein). **Nachtrag 08.08.2026:** Nur die vier Tier-2-Resourcen (HardwoodTree, SwampHemp, GraniteDeposit, IronVein) sind umgebaut; die zehn Tier-1-Visuals und ihre Zonenvarianten tragen handgebaute `Geometry_A14`-Geometrie (05.08.) und bleiben per Auftraggeber-Entscheid unangetastet. **Nachtrag 2 (08.08.2026, Etappe 3b):** Per erneutem Auftraggeber-Entscheid wurden anschließend auch die fünf Tier-1-Typen samt Varianten und Nodes im Fabrik-Stil neu gebaut (`T1ResourceVisualBuilder`); die handgebauten Originale liegen in der Sicherung vor-stilumbau-e3b-20260808-1927. |
| 4 | Props | `StyleProofContentBuilder` | Felsen als unregelmäßige Lofts, Moosknollen wie Hidden-Kiste |
| 5 | Gebäude | `AreaArtVariantPrefabBuilder` + Wand-/Dachbuilder | größte Gruppe zuletzt; Footprints bleiben exakt gleich |

## Invarianten (bleiben unverändert)

- Collider und deren Maße, Interaktionsreichweiten, Abbau-Erträge
- `WorldChestVisual`-API und −72°-Deckelöffnung (`openedLidEuler`)
- Active/Exhausted-Prefabpaare und deren Verdrahtung
- Bauraster-Footprints, NavMesh-relevante Grundflächen
- Vier Gebietsstimmungen aus G-003 (Farbwelt bleibt Bestand)

Der Stilumbau tauscht ausschließlich Renderer-tragende Kindobjekte.

## Abnahme und Tests

**Abnahme pro Etappe:**

- Vorher/Nachher-Captures aus positionsgleichen Kamerapositionen (G-001-
  Strecke); für Kisten/Stationen Nahaufnahmen je Sichtzustand
- Dreieckskorridore (Builder-Konsole meldet Istwerte):
  Kiste/Station ≤ 400, Ressourcenknoten ≤ 600, Prop ≤ 500, Gebäude ≤ 1.200
- Bildrate an denselben Positionen nicht messbar schlechter

**Tests:**

- `EidrenMeshFactoryTests` (EditMode, neu): Vertexfarben vorhanden,
  Determinismus (doppelter Aufruf ⇒ identische Vertexdaten), Kontakt-AO
  dunkelt untere Flächen ab, Dreieckskorridore eingehalten
- Vier-Zustände-Test: je Weltkisten-Familie ergeben die vier
  `WorldChestVisualState`-Werte vier unterscheidbare
  Sichtbarkeitskombinationen
- Bestehende Sichttests (u. a. `V02ContainerVisualTests`) werden auf die
  Fabrik-Ausgaben nachgezogen, nicht gelöscht
- Regressionsmaßstab ist der **Namensdiff gegen die EditMode-Baseline**
  (73 bekannte Fehlschläge), nicht `failed="0"`

## Absicherung und Risiken

- Vor jeder Etappe: Kopie von `Assets/_Game` nach
  `C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e<N>-<stamp>`
  (Quellstand ist unversioniert)
- Unity-Batch: Lockfile prüfen statt löschen (paralleler codex-Agent),
  Logs inhaltlich auf `error CS` prüfen, Exit-Code nicht trauen
- Risiko `UsePass`-Passnamen (URP-Version): Prüfmuster aus dem
  Wanderer-Shader übernehmen
- Risiko Materialtausch: alte `.mat`-Dateien erst nach Etappe 5 entfernen,
  damit jede Etappe einzeln rückrollbar bleibt (Sicherung + Builder-Lauf)

## Offene Punkte für den Implementierungsplan

- Existiert bei Forge-Kisten ein Teilgeleert-Zustand? (Prüfung in Etappe 2;
  Entwurfsantwort: nein ⇒ nur voll/leer)
- Exakte Profilformen je Objekt (Kronenstufen, Felslofts) werden je Etappe
  gegen die 2D-Vorlagen bzw. Bestandssilhouetten festgelegt
