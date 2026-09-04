# MIDPOLY-000 – Phase 4 Arbeitsstand

**Stand:** 25.08.2026  
**Phase:** 4 – Gebäude, Stationen und Container  
**Status:** abgeschlossen  
**Referenz:** Workbench-Golden-Master  
**Abgeschlossen:** Storage Chest; Basismodul-Kit Wall/Floor/Door; Produktionsstations-Kit; Weltkisten-Kit Common/Guarded/Hidden; acht Schmiedecontainer; WorldItem und DeathBag

## Storage Chest – produktiv integriert

Die Heimatlagerkiste wurde als echte Mid-Poly-Konstruktion neu aufgebaut und am bestehenden produktiven Prefabpfad integriert. Die neue Fassung besitzt einen gewölbten Plankendeckel, sichtbare Holzstärken, einen vertieften Innenraum, umlaufende Kupferbänder, Scharniere, Schloss, Nieten, Seitenbeschläge und Tragegriffe. Kontrollierte Facettierung bleibt an den Verschleißkanten sichtbar; die funktionalen Rundungen sind nicht unnötig kantig.

Produktionsdaten:

- LOD0 / LOD1 / LOD2: 7.240 / 3.556 / 1.420 Dreiecke;
- drei Materialien: Holz, Schmiedeeisen, Kupfer;
- exakte geschlossene Maße: 0,98 × 0,80 × 0,72 m in Unity;
- verbindliche Sichthöhe 0,80 m und 1×1-m-Baufeld eingehalten;
- Deckel je LOD als getrennte Baugruppe an der hinteren Scharnierachse;
- geschlossene Pose und geöffneter Achsentest mit −105° geprüft;
- kein zusätzlicher Grafik-Collider.

Der vorhandene `StorageContainer` besitzt bislang keinen visuellen Öffnungszustand. Deshalb wurde keine neue Gameplay- oder UI-Logik ergänzt. Die Deckelbaugruppe ist animationsbereit; die geöffnete Pose dient als verbindlicher Achsen-, Innenraum- und Clippingtest.

## Integration und Rückfall

- produktiver Prefab-GUID erhalten: `bed0f5490a6014e4b955be7cdc8845ba`;
- Container-ID `home_base.storage.main`, 24 Slots, Text und Reichweite unverändert;
- Root-Trigger unverändert: Center `(0, 0.75, 0)`, Größe `(1, 1.5, 1)`, Trigger aktiv;
- semantische Alt-Namen und neue Funktionsanker vorhanden;
- `Geometry_A14` bleibt als Builder-Wächter erhalten; `StorageContentBuilder` setzt die neue Grafik nicht zurück;
- vollständiger Alt-Prefab unter `Assets/_Game/Prefabs/Stations/Fallback/StorageChest_Legacy.prefab` gesichert;
- Baukatalog-Icon aus dem neuen produktiven 3D-Prefab neu gerendert.

## Verifikation

- Blender-/GLB-Roundtrip: PASS für alle drei LODs;
- technische Unity-Migration: PASS;
- Storage-Chest-Produktionstests: 3/3 PASS;
- gemeinsame Mid-Poly-Regression: 83/83 PASS;
- `VisualAssetTests`: 11/11 PASS;
- `StorageContainerTests`: 9/9 PASS;
- Unity-Sichtprüfung geschlossen/geöffnet: PASS.

Die vollständige `VisualScaleTests`-Klasse steht derzeit bei 6/8, ausschließlich wegen des bereits vorhandenen Werkbank-Tabellenkonflikts (Werkbank gemessen 1,290 m statt Tabellenwert 1,100 m und 1,270 m Breite statt Budget 1,000 m). Die Storage Chest wird in beiden Fehlermeldungen nicht geführt und erfüllt ihre Maßverträge.

## Basismodul-Kit – produktiv integriert

`Wall`, `Floor` und `Door` wurden als gemeinsame natürliche Holzbau-Familie neu aufgebaut. Die Modelle verwenden kontrollierte Facettierung und kleine funktionale Rundungen statt harter Würfeloptik: unregelmäßig gesetzte Bretter, eingelassene Fugen, Holzmaserungsakzente und Aststellen, gefaste tragende Balken, Diagonalstreben, geschmiedete Beschläge, Nägel und eine steinerne Türschwelle. Alle drei Assets teilen dieselbe PBR-Materialfamilie.

Produktionsdaten:

- Wall LOD0 / LOD1 / LOD2: 3.492 / 956 / 484 Dreiecke;
- Floor LOD0 / LOD1 / LOD2: 2.876 / 1.068 / 308 Dreiecke;
- Door LOD0 / LOD1 / LOD2: 3.644 / 1.264 / 800 Dreiecke;
- drei gemeinsame Materialien: Holz, Schmiedeeisen und Fundamentstein;
- Wall exakt 1,00 × 2,60 × 0,19 m, Door exakt 1,00 × 2,60 × 0,22 m, Floor exakt 1,00 × 0,15 × 1,00 m in Unity;
- eigener, gefaster Mid-Poly-Verbinder für `Joint_MinusX`, `Joint_PlusX`, `Cap_MinusX` und `Cap_PlusX`;
- Türblatt aller drei LODs unter einer gemeinsamen Angel bei lokal X = −0,5 m;
- Rahmen, Blatt, Beschläge und Griff im geschlossenen und geöffneten Zustand geprüft.

## Basismodul-Integration und Rückfall

- produktive Prefab-GUIDs erhalten: Wall `4ce0f1c1124b50e49ae05aa76a79ca02`, Floor `6d4d5579cbb97554787e976ecb41b48a`, Door `e3ae2b2d6c1da604e93bdbe81cb67676`;
- Root-Collider bytegleich erhalten: Wall/Door `(1, 2,6, 0,2)`, Floor `(1, 0,15, 1)` einschließlich Center;
- Türsensor unverändert: Center `(0, 1, 0)`, Radius `2,1`, Trigger aktiv;
- Wandverbinder-Presenter und vier schaltbare Root-Stücke erhalten;
- `EidrenBuildingVisualBuilder` schützt die freigegebenen Module vor Low-Poly-Rückbau;
- `WallConnectionContentBuilder` setzt bei späteren Rebuilds weiterhin das neue Verbinder-Mesh;
- vollständige Alt-Prefabs unter `Assets/_Game/Prefabs/Buildings/Fallback/` gesichert.

## Basismodul-Verifikation

- Blender-/GLB-Roundtrip: PASS für neun LOD-Dateien und das Verbinder-Mesh;
- technische Unity-Migration: PASS;
- Basismodul-Produktionstests: 7/7 PASS;
- `VisualAssetTests`: 11/11 PASS;
- `BuildGridPhysicsTests`: 24/24 PASS;
- `BuildingPlacementDataTests`: 18/18 PASS;
- PlayMode-Türdurchgang mit Öffnen, Durchgehen und Schließen: 1/1 PASS;
- alle neun Gebäude-Fälle der aktualisierten Gebäude-Stilprüfung, darunter Wall/Floor/Door: PASS;
- Unity-Sichtprüfung einzeln, als Kit sowie Door geschlossen/geöffnet: PASS.

Die vollständige `Fixrunde031Tests`-Klasse steht bei 23/24; ausschließlich die bereits migrierte Kupferader scheitert an der alten Vertexfarben-Erzfarbenerkennung. Die Tür- und Basismodulprüfungen dieser Klasse bestehen. Die vollständige `EidrenWorldStyleTests`-Klasse steht bei 46/56; die zehn Fehler sind ausschließlich die alten T1-Ressourcenfälle, die weiterhin den überholten Vertexfarben-/Low-Poly-Vertrag erwarten. Wall, Floor und Door bestehen dort vollständig.

## Produktionsstations-Kit – produktiv integriert

`Smelter`, `Sawmill`, `Stonecutter`, `Ropewalk`, `CookingPot` und `FarmPlot` wurden als funktional klar unterscheidbare Mid-Poly-Familie neu aufgebaut. Die Stationen teilen eine kontrolliert facettierte Material- und Konstruktionssprache, bleiben aber über ihre Arbeitsabläufe sofort lesbar:

- Smelter mit facettiertem Ofenkörper, Feueröffnung, Kamin, Blasebalg, Tiegel und Kupferbarren;
- Sawmill mit gezahntem Sägeblatt, Stammwagen, Führung, Schwungrad und Handkurbel;
- Stonecutter mit schwerem Schneidbett, angeritzter Steinplatte, Schleifrad, Kurbel und Meißeln;
- Ropewalk mit drei wirklich verdrillten Strängen, Antriebs- und Führungsrad sowie fertiger Seilrolle;
- CookingPot als offener Kessel mit sichtbarem Inhalt, Dreibein, Herdsteinen, Brennholz und Glut;
- FarmPlot als 2×2-m-Hochbeet mit Furchen, Saatmarkern und Handrechen sowie getrennten Keim- und Erntereif-Modellen.

Produktionsdaten LOD0 / LOD1 / LOD2:

- Smelter: 4.704 / 1.984 / 1.492 Dreiecke;
- Sawmill: 3.380 / 1.544 / 1.438 Dreiecke;
- Stonecutter: 2.912 / 1.152 / 996 Dreiecke;
- Ropewalk: 5.724 / 2.328 / 2.192 Dreiecke;
- CookingPot: 5.436 / 2.396 / 1.452 Dreiecke;
- FarmPlot-Grundkörper: 1.448 / 752 / 460 Dreiecke; Keimzustand 1.992 und Reifezustand 5.520 Dreiecke.

## Produktionsstations-Integration und Rückfall

- alle sechs produktiven Prefab-GUIDs erhalten;
- Root-BoxCollider und `NavMeshObstacle` einschließlich Center, Größe, Trigger-/Carving-Zustand unverändert;
- `WorkbenchController` der fünf Handwerksstationen bytegleich erhalten;
- `FarmPlotController` mit `VIS_Empty`, `VIS_Planted` und `VIS_Ready` neu und eindeutig verdrahtet;
- Arbeitsseite und Interaktionspunkt als semantische Anker an jeder Station hinterlegt;
- `EidrenBuildingVisualBuilder` schützt alle freigegebenen Stationsmodelle vor Low-Poly-Rückbau;
- vollständige Alt-Prefabs unter `Assets/_Game/Prefabs/Buildings/Fallback/` gesichert;
- keine Grafik-Hierarchie trägt zusätzliche Collider.

## Produktionsstations-Verifikation

- Blender-/GLB-Roundtrip: PASS für 18 LOD-Dateien und zwei Farmzustandsmodelle;
- technische Unity-Migration: PASS;
- Produktionsstations-Vertragstests: 8/8 PASS;
- `VisualAssetTests`: 11/11 PASS;
- `ProductionContentTests`: 2/2 PASS;
- `HomeBaseContentTests`: 5/5 PASS;
- `BuildGridPhysicsTests`: 24/24 PASS;
- `BuildingPlacementDataTests`: 18/18 PASS;
- `InteractionIconTests`: 16/16 PASS;
- alle sechs Stationsfälle der aktualisierten Gebäude-Stilprüfung: PASS;
- Unity-Sichtprüfung einzeln, als Familie sowie FarmPlot im Reifezustand: PASS.

Die vollständige `EidrenWorldStyleTests`-Klasse bleibt bei 46/56; die zehn Fehler sind ausschließlich die schon dokumentierten alten T1-Ressourcenfälle. Alle neun Gebäude-Fälle einschließlich der sechs neuen Produktionsstationen bestehen.

## Weltkisten-Kit – produktiv integriert

`WorldChest_Common`, `WorldChest_Guarded` und `WorldChest_Hidden` wurden als zusammengehörige, natürlich facettierte Mid-Poly-Familie neu aufgebaut. Common verwendet eine schlichte Eichenkonstruktion mit Eisenbändern und Tragegriffen. Guarded ist größer, dreifach kupferverstärkt und besitzt Eckpanzerung, Siegelplatte und Abwehrspitzen. Hidden ist kleiner, unauffälliger und durch Wurzelzüge sowie Moosauflagen getarnt. Alle drei bewahren getrennte Holzplanken, eingelassene Fugen, Scharniere, Schloss, Nieten, Innenraum und sichtbare Lootfüllung.

Produktionsdaten LOD0 / LOD1 / LOD2:

- Common: 8.478 / 3.726 / 2.560 Dreiecke, vier Materialien;
- Guarded: 9.350 / 4.262 / 3.092 Dreiecke, vier Materialien;
- Hidden: 8.724 / 3.816 / 2.726 Dreiecke, fünf Materialien;
- exakte Altmaße beibehalten: Common 1,15 × 0,54 × 0,72 m, Guarded 1,45 × 0,70 × 0,88 m, Hidden 1,00 × 0,48 × 0,64 m;
- vier bestehende Zustände `Closed`, `Opened`, `PartiallyEmptied` und `Emptied` eindeutig verdrahtet;
- Deckel aller drei LODs an einer gemeinsamen Hinterachse; Öffnung mit dem bestehenden −72°-Vertrag geprüft.

## Weltkisten-Integration und Rückfall

- produktive Prefab-GUIDs erhalten: Common `8c3645e8fc018da459bc7e43090224f0`, Guarded `f0cedfc8322e79345b147d34813c50d1`, Hidden `7da2b6a7f5ce60f43942187feb66df5a`;
- Root-BoxCollider und sphärischer Interaktionstrigger mit Radius 1,45 m unverändert;
- Interaktionsreichweite 2,6 m, Öffnungsdauer 1,6 s, 24 Slots und Loot-/Zonenprofile unverändert;
- `LootFill_Full`, `LootFill_Partial`, Innenraum, Leerzustand, `LootSocket` und `InteractionPoint` vorhanden;
- W-009 bleibt erfüllt: keine `OpeningHand`-Sprites und leere Handreferenzen;
- `WorldChestContentBuilder` schützt die freigegebenen Modelle vor Low-Poly-Rückbau;
- vollständige Alt-Prefabs unter `Assets/_Game/Prefabs/Loot/WorldChests/Fallback/` gesichert;
- keine Grafik-Hierarchie trägt zusätzliche Collider.

## Weltkisten-Verifikation

- Blender-/GLB-Roundtrip: PASS für neun LOD-Dateien und drei kombinierte Produktionsmodelle;
- technische Unity-Migration: PASS;
- Weltkisten-, Zustands-, Hand- und Interaktionsvertragstests: 9/9 PASS;
- Inhalts- und aktualisierte Weltstilregression: 7/7 PASS;
- Builder-Wächter nach explizitem Rebuild: 5/5 PASS;
- PlayMode-Zoneninteraktion einschließlich Wanderer-Clip `Oeffnen`: 1/1 PASS;
- Unity-Sichtprüfung als Familie sowie je Variante geschlossen/geöffnet: PASS.

## Schmiedecontainer-Kit – produktiv integriert

Die acht visuellen Container der Eidra-Schmiede wurden als gemeinsame, natürlich facettierte Mid-Poly-Familie neu aufgebaut. Getrennte Planken, vertiefte Fugen, geschmiedete Rahmen, Bänder, Scharniere, Schlossplatten, Nieten, Innenraum und sichtbare Lootfüllung bilden die gemeinsame Konstruktion. Die Rollen bleiben über Maß, Beschlagdichte, Siegel und Farbcode eindeutig:

- `SupplyChest`: schlichte robuste Versorgungskiste mit Eisenrahmen und Seitengriffen;
- `OptionalChest`: kupferbetonte optionale Kiste mit eigener Runen-/Siegelplatte;
- `EliteChest`: schwere Eckpanzerung, zusätzliche Bänder und Abwehrzähne;
- `CompletionChest`: größte Fassung mit fünf Beschlagachsen und zentralem Kern-/Abschlusssiegel;
- `SmallRewardChest`, `MediumRewardChest`, `LargeRewardChest`: klar ansteigende Größen-, Goldbeschlag- und Siegelstufen;
- `RecoveryContainer`: eigener petrolfarbener Bergungs-/Schutzcode und niedrigere Bergungssilhouette.

Produktionsdaten LOD0 / LOD1 / LOD2:

- Supply: 8.510 / 3.758 / 2.560 Dreiecke;
- Optional: 8.956 / 3.932 / 2.792 Dreiecke;
- Elite: 9.884 / 4.468 / 3.176 Dreiecke;
- Completion: 11.024 / 5.148 / 3.544 Dreiecke;
- Reward Small: 7.554 / 3.250 / 2.390 Dreiecke;
- Reward Medium: 8.324 / 3.628 / 2.616 Dreiecke;
- Reward Large: 10.022 / 4.542 / 3.238 Dreiecke;
- Recovery: 9.264 / 4.176 / 3.000 Dreiecke;
- fünf gemeinsame Materialien: Holz, Eisen, Rollenakzent, Innenraum und Loot.

## Schmiedecontainer-Integration und Rückfall

- alle acht produktiven Prefab-GUIDs erhalten;
- Root-BoxCollider einschließlich Center, Größe, Trigger- und Aktivzustand unverändert;
- der vom `EidraForgeSceneBuilder` erzeugte Szenenhost bleibt unverändert und trägt weiterhin den 1,2-m-Interaktionstrigger sowie `EidraForgeChestContainer`;
- vier bestehende Sichtzustände `Closed`, `Opened`, `PartiallyEmptied` und `Emptied` eindeutig verdrahtet;
- Deckel aller LODs teilen die hintere Achse und öffnen mit dem bestehenden −72°-Vertrag;
- Elite bleibt bis zum SealGuardian, Completion bis zum CoreGuardian gesperrt; drei Reward-Preisstaffeln und Recovery-Inhalt unverändert;
- `ForgeContainerVisualBuilder` schützt die freigegebenen Prefabs vor Low-Poly-Rückbau;
- vollständige Alt-Prefabs unter `Assets/_Game/Prefabs/Containers/Forge/Fallback/` gesichert;
- keine Grafik-Hierarchie trägt zusätzliche Collider.

## Schmiedecontainer-Verifikation

- Blender-/GLB-Produktion: PASS für 24 LOD-Dateien und acht kombinierte Produktionsmodelle;
- technische Unity-Migration: PASS;
- neue Mid-Poly-Vertragstests: 5/5 PASS;
- bestehende Familien-, Stil-, Schmiede-Dungeon- und Take-All-Regressionen: 19/19 PASS;
- Builder-Wächter nach explizitem Alt-Builder-Lauf: 5/5 PASS;
- Unity-D3D11-Sichtprüfung als Familie und je Rolle geschlossen/geöffnet: PASS.

## WorldItem und DeathBag – produktiv integriert

Das lose Weltitem verwendet nun einen klar lesbaren Mid-Poly-Lootbeutel mit vorhandener Icon- und Mengenanzeige. Die persistente DeathBag besitzt eine größere Bergungssilhouette, Kupferbeschläge und einen leuchtenden Rückgewinnungskern. Beide Assets besitzen LOD0/1/2 und wurden ohne zusätzliche Grafik-Collider am bestehenden produktiven Prefabpfad eingebaut.

Erhalten blieben die Prefab-GUIDs, der WorldItem-Pickup-Trigger mit Radius 0,72 m, Itemidentität, Stapelanzeige und Sofortaufnahme sowie bei der DeathBag der 1,8 × 1,4 × 1,8-m-Trigger, Container-ID `death.bag`, 24 Slots, Reichweite 2,6 m und Persistenzvertrag. `LootContentBuilder` und `StorageContentBuilder` schützen die neue Grafik vor Low-Poly-Rückbau; vollständige Alt-Prefabs liegen in den jeweiligen `Fallback`-Ordnern.

Verifikation:

- Blender-/GLB-Produktion und Unity-Reimport: PASS;
- technische Migration einschließlich GUID-, Trigger-, Daten- und Builder-Vertrag: PASS;
- World-Loot-Vertragstests und gemeinsame Ressourcen-/Visualregression: PASS;
- Unity-D3D11-Sichtprüfung einzeln und als Paar: PASS.

Nach Phase 4 standen 72 Assets auf `abgenommen`. Mit dem ersten Phase-5-Block und seinen sechs Zonenvarianten lautet der aktuelle synchronisierte Gesamtstand 84 `abgenommen`, 18 `fallback`, 12 `technisch-integriert`, 36 `ungeprueft`.

## Nächster Produktionsblock

Phase 5 setzt mit den noch offenen Gameplay-Ressourcen und anschließend Vegetation, Felsen, Ruinen, Runen und Zonenprops fort.
