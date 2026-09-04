# MIDPOLY-000 – Gesamtumstellung von Eidren auf Mid-Poly-3D

**Status:** abgeschlossen und technisch abgenommen am 25.08.2026  
**Projekt:** Eidren  
**Geltungsbereich:** gesamtes Spiel, alle sichtbaren 3D-Weltobjekte und alle zugehörigen Animationen  
**Nicht Bestandteil dieses Dokuments:** unmittelbare Umsetzung oder ungeprüfter Austausch produktiver Prefabs  
**Referenzstand der Bestandsprüfung:** 20.08.2026

**Abschlussbeleg:** `MidPoly_Phase7/MIDPOLY_FINAL_ASSET_REPORT.json` — PASS,
150/150 Migrationszeilen freigegeben, 110 produktive Prefabs, zwölf Szenen,
null Legacy-Assets im Laufzeitbestand.

**Dokumentierte Produkt-Ausnahme ab 25.08.2026:** Der Wanderer und seine direkt
am Körper dargestellten Rüstungen, Werkzeuge und Waffen wurden auf
ausdrücklichen Nutzerwunsch wieder auf die produktive Low-Poly-Fassung
zurückgesetzt. Der restliche Projektumfang bleibt Mid-Poly. Der Abschlussbeleg
weist diese Ausnahme ausdrücklich aus; Details stehen unter
`WandererLowPoly/WANDERER_LOW_POLY_RUECKBAU_ABNAHME.md`.

---

## 1. Auftrag

Das gesamte Spiel wird von der derzeitigen Low-Poly-Darstellung auf eine einheitliche, stilisierte Mid-Poly-3D-Qualität umgestellt. Betroffen sind der Wanderer, alle Kreaturen und Bosse, sämtliche tragbaren Waffen, Werkzeuge und Rüstungen, Gebäude und Stationen, Ressourcen, Vegetation, Felsen, Ruinen, Truhen, Behälter, Beuteobjekte sowie alle weiteren im Spiel sichtbaren 3D-Modelle.

Die Umstellung ist keine bloße Polygonvermehrung. Jedes Modell wird gestalterisch neu interpretiert, erhält glaubwürdige Formen, bessere Proportionen, klar lesbare Materialien, funktionale Konstruktion und eine erkennbare Materialtrennung. Die bestehende Identität, Gameplay-Silhouette und Funktion des jeweiligen Assets bleiben erhalten.

Alle betroffenen Animationen werden auf die neuen Modelle übertragen oder, wenn Retargeting nicht genügt, neu erstellt. Hand-, Fuß-, Waffen- und Werkzeugkontakte müssen physisch glaubwürdig sein. Animationen dürfen weder Körper noch Rüstung oder Ausrüstung sichtbar durchdringen.

Die Migration erfolgt assetfamilienweise mit Freigaben und Rückfallmöglichkeit. Ein vollständiger Big-Bang-Austausch ohne Zwischenabnahmen ist ausdrücklich ausgeschlossen.

## 2. Zielbild

Eidren soll nach der Umstellung wie ein bewusst gestaltetes Mid-Poly-Spiel wirken:

- deutlich mehr Form- und Materialqualität als der aktuelle Low-Poly-Stand;
- stilisiert und performant, nicht fotorealistisch und nicht AAA-High-Poly;
- klare Silhouetten und gute Lesbarkeit aus der regulären Spielkamera;
- glaubwürdige Anatomie bei Figuren und Kreaturen;
- funktional nachvollziehbare Gebäude, Werkzeuge und Stationen;
- zusammenhängende Formensprache über alle Gebiete und Assetfamilien;
- saubere PBR-Materialreaktion mit kontrollierten, nicht überladenen Oberflächen;
- animationsfähige Topologie und stabile technische Integration in Unity.

Die bereits angefertigten Tests dienen als Qualitäts- und Richtungsreferenz, nicht als automatisch produktionsreife Endfassung:

- `TempReview/Wildling3D-MidPoly-Animated/Wildling_MidPoly_Animated.blend`
- `TempReview/Wanderer3D-CopperPickaxe-Reinterpretation/Wanderer_CopperPickaxe_MidPoly_FullArmor_TwoHand.blend`
- `TempReview/Workbench3D-MidPoly-Reinterpretation/Workbench_MidPoly_Reinterpreted.blend`

Der Wanderer-Test gibt insbesondere die gewünschte Richtung für Gesicht, Vollkörperrüstung, weniger runde Schuhe und beidhändige Werkzeugführung vor. Die freigegebene Spitzhackenbewegung wird als Referenz für Abbauanimationen behandelt: bestehende Bewegungslogik beibehalten, Werkzeug aber in beiden Händen glaubwürdig fixieren.

## 3. Verbindliche Stilregeln

### 3.1 Formensprache

- Große, klar lesbare Primärformen; sinnvoll ausgearbeitete Sekundärformen; Details nur dort, wo sie Silhouette, Material oder Funktion unterstützen.
- Keine bloß aufgeblasenen Low-Poly-Flächen und keine Subdivision ohne gestalterische Überarbeitung.
- Kontrollierte Facettierung darf sichtbar bleiben, Rundungen müssen aber dort rund wirken, wo Anatomie oder Funktion dies verlangt.
- Gesichter erhalten eine echte Nasenform, erkennbare Augenpartie, Lider/Augenhöhle, Mund-, Kiefer- und Wangenstruktur. Keine aufgesetzten Kugelaugen und keine maskenhafte Frontfläche.
- Hände müssen Waffen und Werkzeuge tatsächlich greifen können. Finger dürfen stilisiert vereinfacht sein, benötigen aber lesbare Handflächen-, Daumen- und Griffstruktur.
- Schuhe und Stiefel erhalten definierte Sohle, Zehenbox, Ferse und Schaft. Keine kugel- oder pantoffelförmigen Füße.
- Rüstungen sind konstruktiv glaubwürdig: überlappende Platten, textile oder lederne Unterlage, Verschlüsse, Kanten, Gelenkfreiräume und nachvollziehbare Materialstärken.
- Holz, Stein, Metall, Stoff, Leder, Knochen, Pflanzenmaterial und magische Stoffe müssen bereits über Form und Oberflächenreaktion unterscheidbar sein.

### 3.2 Identität und Gameplay

Unverändert zu bewahren sind, sofern nicht einzeln freigegeben:

- Grundsilhouette und Wiedererkennbarkeit;
- relative Körpergröße, Fußabdruck und Reichweite;
- Pivot, Bodenbezug, Vorwärtsrichtung und Unity-Maßstab;
- Farbfamilie, Fraktions- und Gebietscodes;
- lesbare Angriffs- und Interaktionsmerkmale;
- Collider-Funktion, Trefferflächen und Navigationsfreiraum;
- Socket-Funktion für Hände, Waffen, Werkzeuge, Rüstung und Effekte;
- aktive/erschöpfte Zustände von Ressourcen;
- Gameplay-Timing, Animationsevents und Root-Motion-Semantik.

Die neue Gestaltung darf die Persönlichkeit verstärken, aber keine andere Figur, Funktion oder Ressourcenart entstehen lassen.

### 3.3 Materialien und Texturen

- Stilisiertes PBR: Base Color, Normal, Roughness und Metallic nach Bedarf; Emission nur für etablierte magische, glühende oder geschmiedete Elemente.
- Keine fotografischen Oberflächen und kein künstliches Mikrodetailrauschen.
- Kanten, Materialwechsel und Gebrauchsspuren werden gezielt gesetzt, nicht flächendeckend verteilt.
- Gemeinsame Materialfamilien und Atlanten für wiederkehrende Assetgruppen bevorzugen.
- Richtwert: 1–4 Materialslots pro Standardasset; zusätzliche Slots nur bei funktionaler Begründung.
- Richtwert Texturauflösung: 1K für kleine Props/Vegetation, 2K für Figuren, größere Stationen und modulare Gebäude; 4K nur für nachweislich erforderliche Hero- oder Boss-Atlanten.
- Transparenz nur, wenn sie visuell nötig und technisch vertretbar ist. Blattwerk bevorzugt als geometrisch lesbare Cluster oder kontrollierte Alpha-Flächen.

## 4. Technischer Mid-Poly-Standard

Die folgenden Werte sind Produktionsbudgets, keine Aufforderung, jede Obergrenze auszureizen. Sichtbare Qualität, Deformation und Silhouette entscheiden über die tatsächlich notwendige Dichte.

| Assetklasse | Zielbereich LOD0 | Typische Materialslots | Anmerkung |
|---|---:|---:|---|
| Wanderer-Basiskörper | 25.000–45.000 Tris | 2–4 | ohne alle gleichzeitig ausgeblendeten Ausrüstungsteile |
| Standardkreatur | 15.000–35.000 Tris | 1–3 | mehr Dichte an Gesicht und Gelenken |
| große/gerüstete Kreatur | 25.000–50.000 Tris | 2–4 | Schalen, Panzer und große Silhouette berücksichtigen |
| Boss | 40.000–80.000 Tris | 3–6 | nur bei entsprechender Bildschirmpräsenz |
| Rüstungsset gesamt | 6.000–20.000 Tris | 1–3 | Kopf, Brust, Hände, Beine modular |
| Waffe/Werkzeug | 2.000–10.000 Tris | 1–3 | Griffbereiche und Funktionskanten priorisieren |
| kleines Prop | 500–5.000 Tris | 1–2 | Varianten über gemeinsame Materialien |
| mittleres Prop/Truhe | 4.000–15.000 Tris | 1–3 | Öffnungsteile getrennt, falls animiert |
| Station/Werkbank | 8.000–30.000 Tris | 2–5 | Konstruktion und Arbeitsfunktion sichtbar |
| modulares Bauteil | 2.000–20.000 Tris | 1–3 | nahtlose Anschlüsse und Snap-Maße bewahren |
| großes Gebäude | 15.000–60.000 Tris | 2–5 | instanzierbare Module statt unnötiger Einzelteile |
| Baum/Ressourcenknoten | 2.000–15.000 Tris | 1–3 | aktive und erschöpfte Variante |
| Fels/Ruine/Vegetationsgruppe | 500–12.000 Tris | 1–3 | stark sichtbare Silhouette priorisieren |

### 4.1 LOD und Performance

- LOD0: freigegebene Mid-Poly-Qualität.
- LOD1: etwa 45–60 % der LOD0-Dreiecke, Silhouette weitgehend erhalten.
- LOD2: etwa 15–25 % der LOD0-Dreiecke, klare Fernlesbarkeit.
- Zusätzlicher Impostor oder Cull-LOD für große Vegetationsmengen und weit sichtbare Umgebungsobjekte nach Profiling.
- Skinned-Mesh-, Material-, Bone- und Draw-Call-Kosten müssen pro Szene gemessen werden.
- Häufig wiederholte Props erhalten Instancing-kompatible Materialien und nach Möglichkeit gemeinsame Mesh-/Materialfamilien.
- Keine LOD-Stufe darf Gameplay-Telegraphen, Ressourcenstatus oder Interaktionslesbarkeit entfernen.

### 4.2 Geometrie und Export

- 1 Unity Unit entspricht 1 Meter; vorhandene Maßstäbe sind die Referenz.
- Pivot grundsätzlich am vorhandenen funktionalen Ursprung; stehende Assets am Boden, Türen am Scharnier, Deckel an der Achse, Waffen am festgelegten Griff-/Socket-Ursprung.
- Transformationen vor Export anwenden; keine negativen Skalen und keine ungewollten Parent-Skalierungen.
- Normals, Tangents, Glättung und harte Kanten kontrollieren; keine invertierten Flächen.
- Keine unnötigen inneren Flächen, doppelten Vertices oder nicht-manifold Geometrie.
- Saubere UVs ohne ungewollte Überlappungen; Lightmap-UV nur dort, wo der Unity-Lichtpfad sie benötigt.
- Quads für die Arbeitsdatei bevorzugen, trianguliertes Ergebnis vor dem Export prüfen.
- GLB ohne unnötige Kameras, Lichter, Testobjekte oder versteckte Varianten exportieren.
- Vorwärtsrichtung, Up-Achse und Importrotation müssen exakt dem jeweiligen vorhandenen Referenzasset entsprechen.

### 4.3 Dateien und Benennung

Für jedes Asset werden geliefert:

1. editierbare `.blend`-Quelldatei;
2. getestete `.glb`-Runtime-Datei;
3. verwendete Texturen in eindeutig benannten Unterordnern;
4. LODs und Collider-Meshes, sofern erforderlich;
5. Turntable oder 8-Punkt-Showcase;
6. Low-vs-Mid-Vergleich aus identischer Kamera und Beleuchtung;
7. Asset-Manifest mit Tris, Materialien, Texturgrößen, Pivot, LODs, Rig, Clips und offenen Punkten.

Empfohlene Namen:

- `CHR_<Name>_Mid_Lod0.glb`
- `CRE_<Name>_Mid_Lod0.glb`
- `WPN_<Name>_<Tier>_Mid.glb`
- `TLS_<Name>_<Tier>_Mid.glb`
- `ARM_<Set>_<Slot>_Mid.glb`
- `BLD_<Name>_Mid_Lod0.glb`
- `PRP_<Name>_Mid_Lod0.glb`
- `RES_<Name>_<Active|Exhausted>_Mid_Lod0.glb`

Bestehende Unity-GUIDs bleiben nach Möglichkeit erhalten. Ist das nicht möglich, muss jede geänderte Referenz im Migrationsmanifest aufgeführt und automatisch validiert werden.

## 5. Geprüfter Projektbestand und Migrationsumfang

Die Bestandsprüfung zeigt, dass das Spiel nicht nur aus externen GLB-Modellen besteht. Viele sichtbare Modelle sind als Unity-Prefab-Geometrie oder durch Editor-Builder erzeugte `.asset`-Meshes vorhanden. Der Auftrag umfasst beide Herkunftsarten.

### 5.1 Bestandszahlen

- 16 eigenständige geriggte Figurenmodelle als produktive GLB-Dateien; zusätzlich eine Quellkopie des Wanderers unter `Source~`.
- 16 3D-Actor-Prefabs, 1 Player-Prefab, 12 Gegner-Prefabs und 1 Boss-Prefab.
- 13 Gegner-Datenobjekte; weitere Figuren wie Garon und CoreGuardian werden über eigene Inhalte/Builder geführt.
- 11 Gebäude-/Stationsdefinitionen, 9 Gebäude-Prefabs und 2 Stations-Prefabs.
- 40 Environment-Prefabs, 27 Ressourcen-Prefabs, 8 Schmiede-Container-Prefabs, 3 Welttruhen und 1 WorldItem-Prefab.
- Mindestens 611 erzeugte Komponenten-Meshassets in den geprüften Gebäude-, Ressourcen-, Container-, Loot-, StyleProof-, Geometry- und Zonenverzeichnissen. Diese Zahl bezeichnet technische Teilmeshes, nicht 611 eigenständige Motive.
- 58 Item-Datenobjekte, darunter mindestens 20 Werkzeug-/Waffenvarianten und 12 Rüstungsteile über drei Materialstufen. Ob jedes Item bereits als eigenes Weltmesh erscheint, wird im Assetregister gesondert markiert.

### 5.2 Figuren, Kreaturen und Bosse

Alle folgenden Modelle sind neu zu interpretieren, zu riggen und animationskompatibel zu liefern:

| Figur | aktueller LOD0-Stand | Bones | vorhandene Clips |
|---|---:|---:|---|
| Wanderer | 6.586 Tris | 20 | 25 Clips; vollständige Liste in Abschnitt 7.2 |
| Wildling | 1.592 Tris | 17 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| Terrock | 1.996 Tris | 22 | Erscheinen, Felsbrecher, Flucht, Gehen, Ruhe, Steinhaut, Taumeln, Telegraph, Treffer, Wildangriff |
| MoorThrower | 1.816 Tris | 17 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| EmberEater | 1.530 Tris | 17 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| CoreGuardian | 1.368 Tris | 17 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| SealGuardian | 970 Tris | 17 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| RiftGuardian | 1.208 Tris | 17 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| Garon | 2.080 Tris | 22 | Ansturm, Frontschlag, Gehen, Rueckkehr, Ruhe, Taumeln, Tod, Treffer, Wirbel |
| Noctarion | 1.746 Tris | 22 | Erscheinen, Flucht, Gehen, Ruhe, Schattenmal, Schattenschritt, Taumeln, Telegraph, Treffer, Wildangriff |
| AshRunner | 1.778 Tris | 22 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| Ignivar | 2.194 Tris | 22 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| RootCharger | 2.164 Tris | 19 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| Riftling | 1.658 Tris | 17 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| ForgeGuardian | 1.376 Tris | 17 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |
| GraniteShell | 1.836 Tris | 19 | Angriff, Erscheinen, Gehen, Ruhe, Taumeln, Telegraph, Tod, Treffer |

Auch `WildEidra.prefab` und alle visuellen Varianten innerhalb der Gegner-Prefabs sind einzeln zu prüfen. Kein vorhandener Clip darf stillschweigend entfallen. Bei Figuren ohne aktuell exportierten Todesclip ist kein neuer Gameplayzustand zu erfinden; der Auftrag fordert zuerst die Übereinstimmung mit Controller und Datenbestand.

#### 5.2.1 Verbindlicher Animations-Diversitätsdurchgang für Zweibeiner

Der Bestandsaudit vom 21.08.2026 hat ergeben, dass Wildling, Riftling, EmberEater, RiftGuardian, ForgeGuardian, SealGuardian und CoreGuardian trotz unterschiedlicher Körperform und Ausrüstung exakt dieselbe Angriffsbewegung verwenden: einen symmetrischen beidarmigen Überkopfhieb mit identischem Ausfallschritt. Diese Wiederholung ist mit dem Zielbild dieses Auftrags nicht vereinbar und wird als eigener offener Produktionspunkt in Phase 3 aufgenommen. Der MoorThrower bleibt mit seinem asymmetrischen Überkopfwurf die bereits vorhandene positive Ausnahme.

Für die sieben betroffenen Zweibeiner sind körper- und ausrüstungsspezifische Angriffsanimationen zu erstellen:

- Wildling: schneller asymmetrischer Klauen- oder Prankenhieb;
- Riftling: rissartige, aggressiv versetzte Schlagbewegung statt symmetrischem Standardhieb;
- EmberEater: kurzer gebückter Körper-, Biss- oder Prankenstoß aus der Knöchelgängerhaltung;
- RiftGuardian: schwerer, massengetragener Bodenschlag;
- ForgeGuardian: einhändiger Axthieb, während der Schild seine Schutzfunktion sichtbar behält;
- SealGuardian: aufgeladener Stabstoß oder diagonaler Stabschlag, passend zum langen Telegraph;
- CoreGuardian: eigenständiger mehrstufiger Boss-Slam mit klarer Bosslesbarkeit.

Dies autorisiert keine neuen Gameplayangriffe. Clipnamen, Gesamtdauer, Telegraphdauer, Trefferfenster, Events, Reichweite, Root-Motion-Semantik, Collider und Controllerzustände bleiben kompatibel. Geändert wird die visuelle Choreografie innerhalb dieser Verträge. Wo das vorhandene Timing eine gewünschte Bewegung nicht zulässt, ist vor einer Gameplayänderung eine gesonderte Freigabe einzuholen.

Die Abnahme erfordert pro betroffener Figur mindestens Telegraph, Kontaktframe und Nachlauf aus Front-, Seiten- und regulärer Spielkamera sowie einen direkten Vergleich der sieben Angriffe. Eine rein umbenannte, zeitlich verschobene, gespiegelte oder minimal skalierte Kopie derselben Bone-Kurven gilt nicht als individuelle Animation.

### 5.3 Wanderer, Ausrüstung und sichtbare Gegenstände

Der Wanderer muss modular bleiben. Nicht alle Varianten dürfen als ein dauerhaft gerendertes Gesamtmesh exportiert werden.

**Werkzeug-/Waffenfamilien und Materialstufen:**

- Axt, Kupferaxt, Eisenaxt;
- Spitzhacke, Kupferspitzhacke, Eisenspitzhacke;
- Sense, Kupfersense, Eisensense;
- Hammer, Kupferhammer, Eisenhammer;
- Dolche, Kupferdolche, Eisendolche;
- Kupferspeer, Eisenspeer;
- Sonderwaffen: Ash Fangs, Ember Thorn, Sealbreaker.

**Modulare Rüstungsslots:**

- Wanderer/Stoff: Hood, Coat, Bracers, Legs;
- Kupfer: Helmet, Chest, Gloves, Legs;
- Eisen: Helmet, Chest, Gloves, Legs.

Jedes Teil muss alleine und in allen gültigen Kombinationen funktionieren. Schulter-, Ellbogen-, Hüft-, Knie- und Halsbereiche benötigen ausreichend Bewegungsfreiheit. Ausgeblendete Körperflächen dürfen Clipping reduzieren, dürfen aber beim Wechseln oder Zerbrechen von Rüstung keine Löcher sichtbar lassen.

Alle im Spiel als WorldItem, Beute, Ausrüstung oder Vorschau sichtbaren Ressourcen- und Herstellungsgegenstände sind ebenfalls zu prüfen. Reine UI-Icons sind nicht zu modellieren, müssen nach einer 3D-Änderung aber gegebenenfalls aus dem neuen Modell neu gerendert werden.

### 5.4 Gebäude und Stationen

Vollständig zu migrieren sind:

- Workbench;
- Storage Chest;
- Wall;
- Floor;
- Door;
- Farm Plot;
- Cooking Pot;
- Ropewalk;
- Sawmill;
- Smelter;
- Stonecutter.

Zusätzlich sind alle in der Eidra-Schmiede verwendeten baulichen und dekorativen 3D-Elemente einzubeziehen. Snap-Maße, Türachsen, Bauvorschau, begehbare Freiräume, Bedienpunkte und Collider bleiben funktional identisch. Das Mid-Poly-Modell der Werkbank dient als Gestaltungsreferenz für glaubwürdige Konstruktion, Werkzeugdetails und Materialtrennung.

### 5.5 Ressourcen und Vegetation

Für jede Gameplay-Ressource werden aktive und erschöpfte Zustände sowie Gebietsvarianten geprüft:

- Tree und gebietsspezifische Baumvarianten;
- Berry Bush;
- Fiber Plant;
- Swamp Hemp;
- Copper Vein;
- Iron Vein;
- Stone Deposit;
- Granite Deposit;
- Hardwood Tree.

Vorhandene Varianten umfassen unter anderem Greenwood, Marsh, Quarry, Ember Ruins, Grey Rifts, Veil Marsh und Twilight Grove. Aktive und erschöpfte Zustände müssen auf Anhieb unterscheidbar bleiben. Die Ressourcenposition, Interaktionsreichweite und der sichtbare Treffpunkt für Axt oder Spitzhacke dürfen sich nicht unbeabsichtigt verschieben.

### 5.6 Umgebung, Ruinen und Weltobjekte

Einzubeziehen sind mindestens:

- Tree A, Tree B, Tree C;
- Bush, Fern, Flowers, Grass, Moss und Glow Mushrooms;
- Rock Small, Medium und Large sowie alle Zonenvarianten;
- Ruin Wall A, Ruin Wall B und Ruin Monument;
- Eidren Rune und weitere magische Akzente;
- Common, Guarded und Hidden World Chest;
- sämtliche acht Schmiede-Container, auch wenn ihre aktuellen Prefabnamen noch `_2D` enthalten;
- WorldItem-Darstellung und sichtbare Loot-/Death-Bag-Objekte;
- alle zonen- und schmiedespezifischen Props, die durch Builder oder Szenen erzeugt werden.

2D-Billboards, die in der 3D-Welt stellvertretend für ein räumliches Objekt stehen, werden als Migrationskandidaten behandelt. UI, Weltkarte, HUD, reine Icons, Audio und eigenständige VFX sind nicht Teil der Modellierung; sie werden nur angepasst, wenn ein neues Modell ihre Ausrichtung, Maskierung, Vorschau oder Effekt-Sockets verändert.

### 5.7 Generierte Geometrie und Builder

Die Migration darf nicht nur die erzeugten Prefabs überschreiben. Folgende Pipeline-Arten müssen geprüft und entweder auf neue Quellen umgestellt oder kontrolliert stillgelegt werden:

- `EidrenBuildingVisualBuilder`, Gebäude-, Dach-, Tür-, Wand- und Preview-Builder;
- `T1ResourceVisualBuilder`, `T2ResourceVisualBuilder`, `ResourceContentBuilder` und `ResourceNodeVisualTable`;
- `StyleProofContentBuilder`, `StyleProofPropBuilder`, `AreaArtAssetBuilder` und Varianten-Builder;
- `EidraForgeGeometryBuilder`, `ForgeVisualAssetBuilder`, `ForgeContainerVisualBuilder` und Forge-Prop-Builder;
- `WorldChestContentBuilder`, `WorldVisualAssetBuilder` und Loot-Geometrie;
- `V02ActorVisualBuilder`, `Wanderer3DPlayerBuilder`, Kreaturen- und Boss-Content-Builder;
- alle MeshFactory-/Bake-/VisualScale-Werkzeuge, die Geometrie, Materialien oder Transformwerte erneut erzeugen.

Abnahmeregel: Ein Unity-Rebuild oder erneutes Ausführen eines Builders darf kein freigegebenes Mid-Poly-Asset wieder durch Low-Poly-Geometrie ersetzen.

## 6. Produktionsphasen und Reihenfolge

### Phase 0 – Baseline einfrieren

1. Vollständiges Assetregister mit Pfad, Prefab, Datenreferenz, Builder, Szene, Tris, Material, Collider, Rig und Clips erzeugen.
2. Von jedem sichtbaren Asset einen Standardvergleich und einen Gameplay-Screenshot erstellen.
3. Aktuelle Prefab-GUIDs, Bounds, Pivots, Collider und Animationscontroller sichern.
4. Migrationsstatus pro Asset: `ungeprüft`, `in Arbeit`, `Showcase`, `technisch geprüft`, `integriert`, `abgenommen`.

### Phase 1 – Golden Masters und Style Bible

Die drei Referenzfamilien werden produktionsreif finalisiert:

1. Wildling als Standardkreatur;
2. Wanderer mit Kupfervollrüstung und Kupferspitzhacke als geriggter modularer Charakter;
3. Werkbank als komplexes statisches Objekt.

Erst nach gemeinsamer Freigabe von Form, Material, Lichtreaktion, Dichte und Gameplay-Lesbarkeit werden weitere Familien in Serie produziert.

### Phase 2 – Wanderer und Ausrüstungssystem

Basiskörper, Gesicht, Haare, alle 12 Rüstungsteile, alle Waffen-/Werkzeugfamilien, Sockets, Sichtbarkeitsregeln und sämtliche 25 Clips migrieren. Gültige Ausrüstungskombinationen und das Zerbrechen/Ablegen von Rüstung testen.

### Phase 3 – Kreaturen und Bosse

Zuerst Wildling, danach je ein Golden Master für vierbeinige/gedrungene, gerüstete und Fernkampf-Kreaturen. Anschließend restliche Standardkreaturen, Elitegegner und Bosse. Jeder Gegner wird zusammen mit seinem vollständigen Clipset abgenommen.

### Phase 4 – Gebäude, Stationen und Container

Werkbank als Referenz; danach Storage Chest, Basismodule, Produktionsstationen, Truhen, Schmiedecontainer und WorldItem. Builder erst nach visueller Freigabe umstellen.

### Phase 5 – Ressourcen, Vegetation und Umgebung

Ressourcenfamilien mit aktiv/erschöpft, dann Bäume und Pflanzen, anschließend Felsen, Ruinen, Runen und Zonenprops. Varianten über gemeinsame Kits und Materialien effizient halten.

### Phase 6 – Integration und Optimierung

1. LODs und Collider einbauen.
2. Prefabs assetfamilienweise tauschen.
3. Builder/Bake-Ketten umstellen.
4. Animationsevents, Sockets und VFX-Anker prüfen.
5. Repräsentative Szenen mit Worst-Case-Besatz profilieren.
6. Low-Poly-Fallback erst entfernen, wenn die jeweilige Familie vollständig abgenommen ist.

### Phase 7 – Gesamt-QA

Alle Szenen, Spawnpfade, Bauvorschauen, Ausrüstungswechsel, Ressourcenstatus, Truhentypen, Gegnerzustände und Bosse prüfen. Fehlende oder noch alte Weltmodelle werden über einen automatischen Assetreport und eine manuelle Sichtprüfung identifiziert.

## 7. Animationsauftrag

### 7.1 Grundregeln

- Bestehende Clipnamen exakt beibehalten, solange Controller oder Code darauf verweisen.
- Bestehende Loop-Einstellungen, Abspielgeschwindigkeit, Übergänge, Events und Root-Motion-Semantik dokumentieren und erhalten.
- Das vorhandene Timing ist Ausgangspunkt. Änderungen an Schlagfenster, Telegraph, Treffer, Abbaukontakt oder Bewegungsgeschwindigkeit benötigen Gameplay-Freigabe.
- Füße stehen in Ruhe- und Kontaktphasen stabil; kein sichtbares Rutschen.
- Hände folgen Griffen ohne Schweben. Zweihändige Werkzeuge werden von beiden Händen geführt, nicht nur von einer Hand mit mitlaufendem freien Arm.
- Werkzeuge dürfen beim Ausholen nicht durch Kopf, Oberkörper, Rücken, Schulterplatten oder Hüftrüstung schneiden.
- Rüstungsteile deformieren plausibel oder bleiben als starre Teile korrekt an Bones gebunden.
- Gelenke benötigen saubere Gewichte ohne Volumenverlust, harte Knicke oder zusammenfallende Panzersegmente.
- Telegraphen bleiben aus der Spielkamera klar lesbar.
- Exportiertes GLB wird nach Reimport getestet; eine nur in der `.blend` funktionierende Animation gilt nicht als geliefert.

### 7.2 Verbindliche Wanderer-Clips

Folgende 25 Clipnamen müssen erhalten und auf dem modularen Mid-Poly-Wanderer geprüft werden:

```text
Abbau_Axt
Abbau_Sense
Abbau_Spitzhacke
Angriff_Dolche
Angriff_Hammer
Angriff_Speer
Gehen_Axt
Gehen_Dolche
Gehen_Hammer
Gehen_Sense
Gehen_Speer
Gehen_Spitzhacke
Laufen_Axt
Laufen_Dolche
Laufen_Hammer
Laufen_Sense
Laufen_Speer
Laufen_Spitzhacke
Oeffnen
Ruhe_Axt
Ruhe_Dolche
Ruhe_Hammer
Ruhe_Sense
Ruhe_Speer
Ruhe_Spitzhacke
```

Für jede Aktion sind Stoff-, Kupfer- und Eisen-Vollausrüstung sowie die jeweils relevante Werkzeug-/Waffenvariante zu testen. Bei Spitzhacke und Axt sind mindestens Ausgangsstellung, Ausholen, Scheitelpunkt, gerader Abwärtsschlag, Kontakt, Nachlauf und Rückkehr zu prüfen.

### 7.3 Kreaturenclips

Die exakten Clipsets aus Abschnitt 5.2 sind je Modell verbindlich. Standardclips wie Ruhe, Gehen, Angriff, Telegraph, Treffer, Taumeln, Erscheinen und Tod dürfen nicht als identische, mechanisch kopierte Animation auf alle Körperformen verteilt werden. Rhythmus, Masse, Schwerpunkt und Anatomie der jeweiligen Kreatur müssen erkennbar sein.

Der identische Bestandsangriff von Wildling, Riftling, EmberEater, RiftGuardian, ForgeGuardian, SealGuardian und CoreGuardian ist ausdrücklich zur Überarbeitung geöffnet. Bereits erfolgte Mid-Poly-Geometrie- oder Prefab-Freigaben schließen diesen Animationspunkt nicht automatisch ab; die Animationsfreigabe wird für diese sieben Figuren separat geführt.

Spezialclips wie Felsbrecher, Steinhaut, Wildangriff, Schattenmal, Schattenschritt, Ansturm, Frontschlag, Rueckkehr und Wirbel behalten ihre Gameplayfunktion und werden mit den neuen Körpermassen neu geprüft.

## 8. Promptpaket für die Produktion

Die folgenden Prompts sind für einen 3D-Produktionsagenten oder Blender-Artist gedacht. Jeder Prompt erhält als Eingabe mindestens das vorhandene GLB/Prefab, Vergleichsbilder aus der Spielkamera, Maße/Pivotdaten und den relevanten Abschnitt dieses Auftrags. Bildgeneratoren dürfen für frühe Concepts eingesetzt werden, ersetzen aber niemals die echte 3D-Quelldatei.

### Prompt 1 – Universeller Masterprompt

```text
AUFGABE
Erstelle eine produktionsfähige Mid-Poly-Neuinterpretation des Eidren-Assets [ASSETNAME] aus der Familie [ASSETFAMILIE]. Arbeite auf Basis von [QUELLPFAD/REFERENZEN]. Das Ergebnis muss ein echtes editierbares 3D-Modell sein, keine 2D-Illustration und kein reines Renderbild.

GESTALTUNGSZIEL
Stilisiertes Mid-Poly-Fantasy-3D mit klarer Silhouette, glaubwürdiger Konstruktion bzw. Anatomie, ausgearbeiteten Primär- und Sekundärformen und kontrolliertem Detailgrad. Das Modell soll einen deutlich sichtbaren Qualitätssprung gegenüber der Low-Poly-Vorlage darstellen, ohne fotorealistisch oder unnötig high-poly zu werden. Bewahre Identität, Gameplayfunktion, Farbfamilie und charakteristische Merkmale. Interpretiere Form, Material und Konstruktion sichtbar neu; erhöhe nicht nur die Polygonzahl.

TECHNIK
- Zielbudget LOD0: [TRISBUDGET]
- LOD1: 45–60 %, LOD2: 15–25 % von LOD0
- Unity-Maßstab, Pivot, Bodenbezug, Vorwärtsrichtung und Bounds aus der Vorlage bewahren
- saubere Topologie, Normals, UVs und Materialslots
- stilisiertes PBR; Textur- und Materialbudget gemäß MIDPOLY-000
- vorhandene Collider-/Socket-/Rig-Funktion berücksichtigen
- keine Kameras, Lichter oder Testobjekte im Runtime-GLB

IDENTITÄTSSCHUTZ
Nicht verändern: [UNVERÄNDERLICHE MERKMALE].
Verbessern: [GEWÜNSCHTE VERBESSERUNGEN].
Vermeiden: bloße Subdivision, weiche Knetformen, kugelige Schuhe/Füße, aufgesetzte Kugelaugen, zufälliges Mikrodetail, fotorealistische Texturen, unlesbare Silhouette, zusätzliche Gameplayfunktion, unfreigegebene Größenänderung.

LIEFERUNG
Liefere .blend, getestetes .glb, Texturen, LODs/Collider falls erforderlich, Assetmanifest, 8-Punkt-Turntable und einen Low-vs-Mid-Vergleich mit identischer Kamera und Beleuchtung. Integriere das Modell noch nicht in produktive Prefabs, bevor Showcase und technische Prüfung freigegeben wurden.
```

### Prompt 2 – Wanderer-Basiskörper und Gesicht

```text
Erstelle den Wanderer als modularen Mid-Poly-Spielcharakter neu. Nutze den aktuellen Wanderer und den freigegebenen Mid-Poly-Test als Identitätsreferenz. Behalte Größe, Proportionstyp, Rig-Kompatibilität, Pivots und Ausrüstungssockets bei, verbessere aber Anatomie und Ausdruck deutlich.

Das Gesicht benötigt eine echte stilisierte Nase mit Nasenrücken und Nasenflügeln, glaubwürdige Augenhöhlen, Augäpfel bzw. überzeugende stilisierte Augen, Ober- und Unterlider, Brauenbogen, Wangen, Kiefer und Mundform. Kein Puppen-, Masken- oder Kugelaugengesicht. Hände benötigen Daumen, Handfläche und eine für ein- und zweihändige Griffe funktionale Fingerform. Füße/Stiefel benötigen Sohle, Ferse, Zehenbox und Schaft; keine runden Pantoffelformen.

Erzeuge einen neutralen Basiskörper, der mit allen vorhandenen Stoff-, Kupfer- und Eisenrüstungsteilen kombinierbar ist. Plane verdeckbare Körperzonen gegen Clipping, ohne sichtbare Löcher beim Ausrüstungswechsel. Behalte die 20-Bone-Rig-Kompatibilität, sofern ein dokumentierter Rig-Test keine Erweiterung verlangt. Liefere neutrale Pose, Deformationstests für Hals, Schulter, Ellbogen, Handgelenk, Hüfte, Knie und Fuß sowie einen Gesichtsshowcase aus naher und regulärer Spielkamera.
```

### Prompt 3 – Modulare Vollkörperrüstung

```text
Erstelle für den Mid-Poly-Wanderer die Rüstungsfamilien Stoff/Wanderer, Kupfer und Eisen in den Slots Kopf, Brust, Hände und Beine. Jede vollständige Kombination soll wie eine vollwertige, funktionale Vollkörperrüstung wirken und trotzdem modular austauschbar bleiben.

Entwickle eine glaubwürdige Unterkleidung und darüber konstruktiv nachvollziehbare Rüstungsschichten. Kupfer und Eisen müssen sich nicht nur durch Farbe unterscheiden: Formensprache, Plattenaufteilung, Kanten, Nieten, Verstärkungen, Materialstärke und Abnutzung sollen die Progression lesbar machen. Erhalte Bewegungsfreiheit an Hals, Schulter, Ellbogen, Taille, Hüfte und Knie. Schuhe/Stiefel sind kantiger, mit klarer Sohle, Ferse und Schutzkappe zu gestalten.

Teste alle vier Slots einzeln, alle vollständigen Sets sowie gemischte gültige Kombinationen. Prüfe jeden Wanderer-Clip auf Clipping. Liefere je Set Front/Seite/Rückseite, Nahaufnahme der Gelenke und Animationskontaktprüfung. Keine dekorativen Teile, die durch Waffen, Werkzeuge oder Kamera regelmäßig schneiden.
```

### Prompt 4 – Waffen und Werkzeuge

```text
Erstelle [WAFFE/WERKZEUG] in den Stufen [STUFEN] als zusammengehörige Mid-Poly-Familie. Bewahre Länge, Reichweite, Griffposition, Socket-Ausrichtung und Gameplay-Silhouette der vorhandenen Modelle. Verbessere Materialtrennung, konstruktive Logik, Griffwicklung, Schaftquerschnitt, Kopf-/Klingenbefestigung, Schneide bzw. Schlagfläche und sichtbare Progression.

Holz, Kupfer, Eisen und Sondermaterialien müssen über Form, Roughness, Kanten und Herstellungsweise unterscheidbar sein. Das Modell darf nicht wie ein vergrößertes Spielzeug wirken. Griffbereiche müssen zu den Händen des Wanderers passen. Für zweihändige Werkzeuge ist ein klar definierter Haupt- und Nebengriffbereich erforderlich.

Liefere Socket-Test, Greiftest in Ruhe/Gehen/Laufen/Aktion, Kollisionsprüfung mit allen Rüstungsstufen und LODs. Die effektive Treffer-/Arbeitsposition darf nicht unbemerkt verändert werden.
```

### Prompt 5 – Wanderer-Animationen und beidhändige Werkzeuge

```text
Übertrage und verfeinere alle 25 vorhandenen Wanderer-Clips auf den modularen Mid-Poly-Wanderer. Behalte Clipnamen, Loopstatus, grundlegendes Timing, Root-Motion-Semantik und Gameplayevents bei. Verwende die freigegebene ursprüngliche Abbaubewegung als Basis; ändere nicht unnötig ihre Choreografie.

Bei Axt und Spitzhacke greifen beide Hände das Werkzeug dauerhaft und plausibel. Die führende und die stützende Hand dürfen je nach Schlagphase kontrolliert am Schaft gleiten, aber nicht loslassen, schweben oder nur optisch mitlaufen. Der Schlag wird aus Füßen, Hüfte, Rumpf, Schultern und Armen getragen. Prüfe insbesondere, dass Werkzeug, Hände und Unterarme beim Ausholen und Abwärtsschlag nicht durch Kopf, Körper, Rücken oder Rüstung schneiden.

Teste jeden Clip mit Stoff-, Kupfer- und Eisen-Vollrüstung. Liefere pro Aktion Seiten-, Front- und Spielkameraansicht, markierte Kontaktframes, Foot-Slip-Prüfung und einen GLB-Reimporttest. Keine Änderungen an Treffer- oder Abbauzeitpunkten ohne dokumentierte Freigabe.
```

### Prompt 6 – Humanoide Standardkreatur

```text
Interpretiere [KREATURENNAME] als produktionsfähige Mid-Poly-Kreatur neu. Nutze die Low-Poly-Figur für Identität, Proportionstyp, Farbfamilie und Gameplay-Silhouette, aber entwickle Anatomie, Gesicht, Hände/Füße, Materialübergänge und charakteristische Körpermerkmale sichtbar weiter. Die Figur muss aus der Spielkamera eindeutig als [ROLLE/FRAKTION] lesbar sein.

Vermeide menschliche Standardanatomie, wenn die Vorlage bewusst fremdartig ist; jede Abweichung benötigt jedoch strukturelle Logik. Augen, Mund, Nase/Schnauze, Gelenke und Extremitäten dürfen nicht wie aufgesetzte Primitive wirken. Erhalte Rig-/Controller-Kompatibilität oder dokumentiere eine saubere Retarget-Migration.

Übertrage exakt diese Clips: [CLIPLISTE]. Jeder Clip erhält körperformspezifische Masse, Schwerpunkt und Rhythmus. Telegraph, Angriff und Trefferreaktion müssen in regulärer Spielkamera klar bleiben. Bei Wildling, Riftling, EmberEater, RiftGuardian, ForgeGuardian, SealGuardian und CoreGuardian darf der identische beidarmige Bestands-Überkopfhieb nicht als Endfassung übernommen werden; entwickle die in Abschnitt 5.2.1 festgelegte individuelle Choreografie innerhalb der bestehenden Gameplayzeiten. Liefere LODs, Deformationstest, GLB-Reimport und Low-vs-Mid-Showcase.
```

### Prompt 7 – Vierbeinige, gedrungene oder gepanzerte Kreatur

```text
Erstelle [KREATURENNAME] als schwere Mid-Poly-Kreatur mit glaubwürdigem Skelett-, Muskel- und Panzeraufbau. Die bestehende Silhouette und Hitbox bleiben Referenz. Entwickle Lastverteilung, Fußaufsatz, Schulter-/Beckenmechanik und Übergänge zwischen organischen, steinernen, wurzelartigen oder metallischen Körperteilen aus.

Panzer und Schalen brauchen Materialstärke, Überlappung und Gelenkfreiräume; sie dürfen nicht wie aufgezeichnete Farbfelder wirken. Bei Fortbewegung müssen Gewicht und Bodenkontakt erkennbar sein. Angriffe entstehen aus Schwerpunktverlagerung und Körpermasse, ohne dass Gliedmaßen oder Panzer ineinander schneiden.

Übertrage [CLIPLISTE], bewahre Gameplay-Timing und liefere Kontakt-, Silhouetten-, Collider-, LOD- und Reimporttests.
```

### Prompt 8 – Fernkampf- oder Werferkreatur

```text
Erstelle [KREATURENNAME] als Mid-Poly-Fernkampfgegner neu. Bewahre die eindeutige Fernkampflesbarkeit bereits in der Silhouette. Gestalte Körper, Trageweise, Wurf-/Schussorgan oder Projektilquelle konstruktiv nachvollziehbar und definiere einen stabilen Effekt-/Projektilsocket.

Überarbeite [CLIPLISTE] so, dass Vorbereitung, Zielphase, Auslösung und Nachlauf klar erkennbar sind. Das Auslöseevent bleibt zeitlich kompatibel. Hände, Projektil und Ausrüstung dürfen nicht schweben oder den Körper durchdringen. Prüfe Lesbarkeit aus maximaler Kampfdistanz und in Gruppen mit Nahkampfgegnern.
```

### Prompt 9 – Boss

```text
Interpretiere den Boss [BOSSNAME] in Mid-Poly-Qualität neu. Erhalte seine ikonische Silhouette, Größenwirkung, Arena-Lesbarkeit, Schwachstellen, Angriffsvorbereitung und alle gameplayrelevanten Effektanker. Steigere Formhierarchie und Materialkomplexität gegenüber Standardgegnern, ohne in fotorealistisches High-Poly oder unlesbares Oberflächendetail zu wechseln.

Übertrage exakt [CLIPLISTE]. Prüfe alle Attacken aus Arena- und Nahkamera, besonders Reichweite, Bodenkontakt, Drehpunkt, Effektposition und Trefferfenster. Liefere LODs, optimierte Skinned-Mesh-Aufteilung, Materialbudget, Bounds-/Collidervergleich und einen vollständigen Boss-Showcase pro Phase bzw. Spezialaktion.
```

### Prompt 10 – Werkbank und Produktionsstation

```text
Erstelle [STATIONSNAME] als funktional glaubwürdiges Mid-Poly-Arbeitsobjekt. Nutze das aktuelle Prefab für Abmessungen, Bedienseite, Pivot, Collider und Gameplayfunktion; nutze den Mid-Poly-Werkbanktest als Qualitätsreferenz. Interpretiere Konstruktion, Verbindungen und Arbeitsablauf sichtbar neu.

Zeige tragende Rahmen, Materialstärken, Zapfen/Verbindungen, Beschläge, Arbeitsfläche, Werkzeuge und funktionsspezifische Baugruppen. Kein zufälliger Detailteppich: Jedes größere Teil muss erklären, wie die Station steht und arbeitet. Holz, Metall, Stein, Seil, Leder und Glut erhalten klar getrennte Form- und Materialwirkung.

Bewahre Interaktionspunkt, Bau-Footprint, Navigationsfreiraum und Vorschau-Bounds. Trenne bewegliche Teile, wenn eine Animation oder Zustandsdarstellung vorgesehen ist. Liefere LODs, vereinfachten Collider, Front-/Rückseitenlesbarkeit und Gameplay-Kameratest.
```

### Prompt 11 – Modulares Gebäude

```text
Erstelle das modulare Bauteil bzw. Gebäude [NAME] als Mid-Poly-Kit neu. Alle Snap-Maße, Rasterpunkte, Pivots, Wand-/Bodenstärken, Türöffnungen und Platzierungsbounds müssen exakt kompatibel bleiben. Verbessere Balken, Bretter, Mauerwerk, Beschläge, Kanten, Schäden und Materialtrennung, ohne die Anschlusslogik zu verändern.

Wiederholte Elemente sollen als wiederverwendbare Module und Materialfamilien angelegt werden. Vermeide sichtbar identische Zufallsduplikate, übermäßige Einzelobjekte und Spalten zwischen Modulen. Prüfe gerade Strecke, Innen-/Außenecke, Türanschluss, Bodenanschluss, Bauvorschau und mehrere benachbarte Exemplare.

Liefere LODs, Collider, Snap-Testszene, Material-/Draw-Call-Bericht und Vergleich zur aktuellen Konstruktion.
```

### Prompt 12 – Ressourcenfamilie aktiv/erschöpft

```text
Erstelle die Ressource [RESSOURCE] als Mid-Poly-Familie mit den Zuständen Aktiv und Erschöpft sowie den Gebietsvarianten [VARIANTEN]. Bewahre Fußabdruck, Pivot, Interaktionsreichweite, Ernte-/Schlagpunkt und sofortige Ressourcenlesbarkeit.

Aktiv und erschöpft müssen dieselbe biologische oder geologische Identität teilen, sich aber schon aus mittlerer Entfernung deutlich unterscheiden. Zeige bei Erz nachvollziehbare Einlagerungen, Bruchflächen und Restmaterial; bei Pflanzen oder Bäumen Wuchsstruktur, Schnitt-/Erntespuren und verbleibende Stümpfe. Varianten unterscheiden sich gezielt durch Wuchs, Material, Akzentfarbe und Umweltspuren, nicht durch beliebiges Umfärben.

Liefere beide Zustände nebeneinander, Gebietssatz, LODs, Collider, Instancing-Test und Abbaukontaktprüfung mit Axt bzw. Spitzhacke.
```

### Prompt 13 – Vegetationskit

```text
Erstelle [BAUM/PFLANZE] als stilisiertes Mid-Poly-Vegetationskit. Entwickle glaubwürdige Stamm-, Ast-, Blatt-, Wedel-, Halm- oder Blütenstruktur mit klarer Silhouette aus der Spielkamera. Behalte die Eidren-Farbwelt bei und vermeide sowohl primitive Low-Poly-Kegel als auch fotorealistisches Blattchaos.

Erzeuge mindestens [ANZAHL] Varianten aus einem gemeinsamen Material-/Modulkit, mit deutlich unterschiedlichen Silhouetten und kontrollierter Dichte. Berücksichtige Windbewegung, Instancing, Overdraw, LOD-Popping, Bodenübergang und Gruppierung. Liefere Einzel-, Gruppen- und Fernansicht sowie Performancewerte in einer repräsentativen Vegetationsszene.
```

### Prompt 14 – Felsen, Ruinen, Runen und Zonenprops

```text
Erstelle [PROP/FAMILIE] als Mid-Poly-Umgebungsset neu. Bewahre die charakteristische Gebietszugehörigkeit und den vorhandenen Footprint. Entwickle klar lesbare Bruchkanten, Schichtung, Bearbeitungsspuren, Erosion, Materialstärke und Bodenübergang. Ruinen müssen konstruktiv nachvollziehbar wirken; Runen und magische Akzente erhalten definierte Vertiefung/Erhöhung und kontrollierte Emission.

Erzeuge Varianten mit eigenständiger Silhouette statt bloßer Rotation oder Skalierung desselben Meshes. Vermeide Rauschen, überall gleich starke Beschädigung und sichtbare Wiederholungsmuster. Liefere Kit-Übersicht, Scatter-/Gruppentest, LODs, Collider und Prüfung in allen verwendeten Zonenbeleuchtungen.
```

### Prompt 15 – Truhe, Container und WorldItem

```text
Erstelle [TRUHE/CONTAINER/WORLDITEM] als Mid-Poly-Weltobjekt neu. Bewahre Interaktionspunkt, Footprint, Pivot, Öffnungsachse, Loot-Socket und die eindeutige Seltenheits-/Funktionslesbarkeit. Common, Guarded, Hidden und Schmiedecontainer sollen als Familie erkennbar sein, aber unterschiedliche Rolle und Wertigkeit zeigen.

Konstruktion, Scharniere, Schloss, Bänder, Deckelstärke, Innenraum und Materialverschleiß müssen plausibel sein. Trenne Deckel und bewegliche Beschläge für Animationen. Für aktuell `_2D` benannte Weltcontainer ist eine echte räumliche Lösung zu liefern, sofern die Szene sie als 3D-Weltobjekt nutzt.

Liefere geschlossen/geöffnet, LODs, Collider, Öffnungsanimation bzw. Achsentest und Vergleich in der tatsächlichen Spielsituation.
```

### Prompt 16 – Retargeting und Rig-QA

```text
Prüfe und migriere das Rig von [ASSETNAME] auf das neue Mid-Poly-Mesh. Nutze das vorhandene Skelett und die vorhandenen Clipnamen als technische Referenz. Erweitere oder ersetze das Rig nur, wenn die Deformationsqualität sonst nicht erreichbar ist, und dokumentiere dann jede Mapping- und Controlleränderung.

Validiere Bone-Namen, Hierarchie, Bindpose, Skalen, Root, Avatar/Importtyp, Skinweights, maximale Bone-Influences, Socket-Bones und Clipbereiche. Teste alle Clips nach GLB-Export und Unity-Reimport. Dokumentiere Foot Slip, Hand-/Werkzeugkontakt, Clipping, Volumenverlust, Eventframes, Root Motion, Bounds und Loopnaht.

Die Lieferung ist abgelehnt, wenn ein Clip fehlt, umbenannt ist, nur in Blender funktioniert, Events verschoben sind oder sichtbare Durchdringungen in regulärer Spielkamera auftreten.
```

### Prompt 17 – Technische und visuelle Endprüfung

```text
Führe die Abnahme für [ASSETNAME] gemäß MIDPOLY-000 durch. Vergleiche Low-Poly und Mid-Poly unter identischer Kamera, Beleuchtung, Pose und Skalierung. Prüfe nicht nur Schönheit, sondern Identität, Gameplay-Lesbarkeit, Silhouette, Konstruktion/Anatomie, Materialien, LODs, Collider, Pivot, Sockets, Rig, Animationen, Export und Unity-Integration.

Erstelle einen Befund mit PASS/FAIL je Prüfkriterium, reproduzierbaren Screenshots und konkreten Korrekturen. Markiere jede unfreigegebene Änderung an Größe, Reach, Hitbox, Timing oder Interaktionspunkt als Blocker. Prüfe zusätzlich, ob ein Builder oder Rebuild das Asset wieder überschreibt.

Freigabestufen: Showcase, technische Freigabe, Gameplay-Freigabe, Integrationsfreigabe. Ein visueller Showcase allein ist keine Produktionsabnahme.
```

## 9. Abnahmekriterien pro Asset

Ein Asset gilt nur dann als abgeschlossen, wenn alle zutreffenden Punkte erfüllt sind:

### Visuell

- Der Qualitätssprung ist aus regulärer Spielkamera sichtbar, nicht nur in Nahaufnahme.
- Das Ergebnis ist eine echte Neuinterpretation und keine geglättete Low-Poly-Kopie.
- Silhouette, Proportion, Materialtrennung und Funktion sind klar lesbar.
- Das Asset passt zu den Golden Masters und wirkt nicht wie aus einem anderen Spiel.
- Gesicht, Hände, Füße, Rüstung oder Mechanik erfüllen die jeweiligen Spezialregeln.

### Technisch

- Dreiecks-, Material-, Textur-, Bone- und Draw-Call-Budget eingehalten oder begründet.
- Pivot, Maßstab, Achsen, Bounds und Sockets korrekt.
- LODs funktionieren ohne auffällige Pops oder verlorene Gameplaymerkmale.
- Collider entsprechen der Funktion und sind nicht unnötig komplex.
- GLB-Roundtrip und Unity-Reimport ohne Fehler.
- Keine Missing References, keine gebrochenen Prefabs und keine Builder-Rücksetzung auf Low-Poly.

### Animation

- Vollständiges Clipset vorhanden und korrekt benannt.
- Angriffe unterscheiden sich bei den in Abschnitt 5.2.1 genannten Zweibeinern klar in Choreografie, Gewichtsverlagerung, Waffenführung und Kontaktpose.
- Keine Abnahme einer lediglich gespiegelten, zeitversetzten oder minimal skalierten Kopie derselben Angriffskurven.
- Kontakte glaubwürdig; keine schwebenden Hände/Füße/Werkzeuge.
- Kein sichtbares Clipping mit Körper, Vollrüstung oder Umgebung in Standardfällen.
- Root Motion, Loop, Events, Telegraph und Treffer-/Abbauzeitpunkte kompatibel.
- Jede Animation in Blender und nach Unity-Import geprüft.

### Gameplay

- Reichweite, Trefferflächen, Interaktionspunkt und Navigationsfreiraum unverändert oder ausdrücklich freigegeben.
- Aktive/erschöpfte, offen/geschlossen und Seltenheitszustände bleiben eindeutig.
- Ausrüstungswechsel, Rüstungsbruch und alle gültigen Kombinationen funktionieren.
- Repräsentative Szenen bleiben innerhalb der vereinbarten CPU-/GPU-/Speicherbudgets.

## 10. Automatisierte Prüfungen

Für die Migration ist ein Editor-Report anzulegen, der mindestens prüft:

- alle produktiven Prefabs auf Low-Poly-/Legacy-Meshreferenzen;
- alle Renderer auf fehlende Materialien oder unerwartete Materialslotzahlen;
- alle Actor-GLBs auf erwartete Bones und Clipnamen;
- alle Ressourcen auf Active/Exhausted-Paar und Collider;
- alle Gebäude auf Pivot, Bounds und Preview-/Placement-Kompatibilität;
- alle LODGroups auf Vollständigkeit und gültige Schwellen;
- alle Builder auf Ausgabepfade, die freigegebene Mid-Poly-Assets überschreiben könnten;
- alle Szenen auf noch sichtbare 2D-Platzhalter für räumliche Weltobjekte;
- Asset-GUIDs und Prefabreferenzen nach jedem Familienwechsel.

Ein manueller visueller Rundgang bleibt zusätzlich erforderlich, weil Formqualität, Clipping, Materialwirkung und Lesbarkeit nicht vollständig automatisiert beurteilt werden können.

## 11. Änderungs- und Freigaberegeln

- Bestehende Low-Poly-Quellen bleiben bis zur vollständigen Familienabnahme erhalten.
- Arbeit erfolgt in separaten Mid-Poly-Quell- und Reviewpfaden; produktive Prefabs werden erst nach Showcase und technischer Prüfung umgestellt.
- Jede Assetfamilie erhält eine verantwortliche Freigabe und ein dokumentiertes Vergleichsblatt.
- Neue Gameplayfunktionen, neue Angriffe, veränderte Hitboxen, andere Gebäudegrößen oder veränderte Ressourcenreichweiten sind nicht durch diesen Grafikauftrag autorisiert.
- Bei Konflikten zwischen schönerer Form und Gameplaykompatibilität wird eine bewusste Freigabeentscheidung dokumentiert; die Grafikseite entscheidet dies nicht stillschweigend.

## 12. Definition of Done für die Gesamtumstellung

Die Umstellung ist erst abgeschlossen, wenn:

1. alle im Assetregister erfassten 3D-Weltmodelle den Status `abgenommen` tragen;
2. alle 16 Figuren und ihre vollständigen vorhandenen Clipsets in Mid-Poly funktionieren und der Zweibeiner-Diversitätsdurchgang aus Abschnitt 5.2.1 abgenommen ist;
3. der Wanderer alle Ausrüstungsstufen und 25 Clips ohne relevante Kontakt- oder Clippingfehler unterstützt;
4. alle Gebäude, Stationen, Ressourcen, Umgebungsprops, Truhen, Schmiedecontainer und WorldItems migriert sind;
5. aktive/erschöpfte und offene/geschlossene Zustände vollständig sind;
6. kein produktiver Builder freigegebene Mid-Poly-Arbeit zurücksetzt;
7. der automatische Legacy-Report keine ungeklärten sichtbaren Low-Poly- oder 2D-Weltplatzhalter meldet;
8. alle repräsentativen Szenen die festgelegten Performancebudgets erfüllen;
9. eine abschließende Spielrunde keine sichtbaren Stilbrüche, fehlenden Modelle, gebrochenen Animationen oder ungewollten Gameplayänderungen zeigt;
10. Low-Poly-Fallbacks erst danach gezielt archiviert oder entfernt werden.

---

## Kurzfassung für die Auftragsübergabe

> Stelle sämtliche sichtbaren 3D-Assets von Eidren auf eine einheitliche stilisierte Mid-Poly-Qualität um. Nutze die aktuellen Modelle als Identitäts-, Maß-, Pivot-, Rig- und Gameplayreferenz, interpretiere Anatomie, Konstruktion und Materialien aber deutlich hochwertiger neu. Migriere nicht nur GLBs, sondern auch alle Prefab- und Builder-erzeugten Meshes. Bewahre alle vorhandenen Clips, Events, Kontakte, Sockets, Collider und Gameplaymaße. Arbeite in freigegebenen Assetfamilien, beginnend mit den Golden Masters Wildling, Wanderer und Werkbank. Liefere pro Asset editierbare Blenderquelle, getestetes GLB, LODs, Collider, Manifest und Low-vs-Mid-Vergleich. Keine produktive Integration vor visueller und technischer Freigabe; kein Big-Bang-Austausch.
