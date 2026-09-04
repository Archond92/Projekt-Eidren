# G-004 Höhentabelle — Aufnahmeregel für das Kontaktdecal

**Stand:** 11. August 2026 · **Bezug:** [G004_PLAN.md](G004_PLAN.md) Task 1, [G004_ENTWURF.md](G004_ENTWURF.md) Abschnitt 3.3

Gemessen mit `Eidren.Editor.GroundContactAudit` über alle 80 Weltprefabs der
sieben Gruppen. Belege: `g004-t1-hoehen.log`, `g004-t1-hoehen2.log`,
Rohdaten `g004-hoehen.csv`.

---

## Festlegung

**Der Schwellwert ist 0,2 m** (`bounds.size.y > 0.2f`).

Der Entwurf schlug 0,3 m vor. Die Messung widerlegt den Wert:

| Schwelle | ohne Decal | mit Decal | springende Zustandspaare |
| ---: | ---: | ---: | ---: |
| **0,2 m** | **3** | **77** | **0** |
| 0,3 m | 8 | 72 | 5 |

Bei 0,3 m fallen fünf Ressourcen in ihrem **abgebauten** Zustand unter die
Regel, während ihr aktiver Zustand darüber liegt:

| Paar | aktiv | abgebaut |
| --- | ---: | ---: |
| `FiberPlant` | 0,906 | 0,250 |
| `Marsh_fiber_plant` | 0,906 | 0,250 |
| `SwampHemp` | 1,378 | 0,228 |
| `TwilightGrove_swamp_hemp` | 1,405 | 0,234 |
| `VeilMarsh_swamp_hemp` | 1,405 | 0,234 |

Active- und Exhausted-Prefab tauschen zur Laufzeit. Läge die Schwelle bei
0,3 m, verschwände der Bodenschatten im Moment des Abbauens sichtbar — ein
Aufpoppen mitten in der Spielhandlung. Bei 0,2 m behalten beide Zustände ihr
Decal.

Bei 0,2 m ist die Ausschlussmenge zugleich **exakt** die, die der Entwurf
benennt („Böden, Wege und flacher Bodenbewuchs"):

| Prefab | Höhe | Art |
| --- | ---: | --- |
| `SP_GroundCover_Moss` | 0,120 | Bodenbewuchs |
| `SP_GroundCover_Grass` | 0,150 | Bodenbewuchs |
| `BLD_Floor_L01` | 0,160 | Boden |

Der Abstand zum nächsthöheren Objekt (`SwampHemp_Exhausted`, 0,228) beträgt
0,068 m. Die Lücke ist schmaler als die bei 0,3 m, die Einteilung dafür
inhaltlich richtig.

---

## Messkorrektur: nur MeshRenderer

Der erste Lauf maß über alle `Renderer`. Die drei Weltkisten tragen die
Sprite-Glyphen `OpeningHand_Left/Right` aus F-005; sie verzerrten die Bounds
um ein Vielfaches:

| Prefab | mit Sprites | nur MeshRenderer |
| --- | --- | --- |
| `WorldChest_Common` | 11,408 × 6,151 × 8,264 | 1,219 × 0,594 × 0,792 |
| `WorldChest_Guarded` | 11,558 × 6,151 × 8,264 | 1,537 × 0,770 × 0,968 |
| `WorldChest_Hidden` | 11,333 × 6,151 × 8,264 | 1,208 × 0,535 × 0,954 |

Ein Decal aus den Sprite-Bounts wäre am Deckel von 4,5 m Breite abgeschnitten
worden und hätte die Kiste als Platte umgeben. `Attach` misst deshalb
ausschließlich `MeshRenderer` — das Kontaktdecal erdet die feste Geometrie.
Betroffen sind nur diese drei Prefabs; die übrigen 77 tragen keine Sprite-
oder Partikelrenderer.

**Nebenbefund für F-010:** Die korrigierten Werte bestätigen die Regel aus
F-010 („Höhe > Tiefe") als weiterhin **verletzt**. `WorldChest_Common` ist
0,594 hoch bei 0,792 Tiefe, `WorldChest_Hidden` 0,535 bei 0,954. Der Stilumbau
hat die Kisten neu gebaut, die Proportionen aber nicht korrigiert. Das gehört
zu F-010, nicht zu G-004 — hier nur festgehalten, damit der Messwert nicht
verlorengeht.

---

## Vollständige Messwerte

Sortiert nach Höhe je Gruppe. Spalte „Decal" nach der Regel `Höhe > 0,2 m`.
### AreaArtVariants — 24 Prefabs

| Prefab | Höhe m | Breite m | Tiefe m | Renderer | Decal |
| --- | ---: | ---: | ---: | ---: | :-: |
| `TwilightGrove_swamp_hemp_Exhausted` | 0,234 | 1,054 | 1,294 | 9 | ja |
| `VeilMarsh_swamp_hemp_Exhausted` | 0,234 | 1,054 | 1,294 | 9 | ja |
| `Marsh_fiber_plant_Exhausted` | 0,250 | 0,370 | 0,334 | 8 | ja |
| `Quarry_stone_deposit_Exhausted` | 0,503 | 2,071 | 1,683 | 6 | ja |
| `Quarry_tree_Exhausted` | 0,700 | 1,517 | 1,497 | 6 | ja |
| `Marsh_tree_Exhausted` | 0,700 | 1,517 | 1,497 | 6 | ja |
| `Greenwood_tree_Exhausted` | 0,700 | 1,517 | 1,497 | 6 | ja |
| `EmberRuins_tree_Exhausted` | 0,700 | 1,517 | 1,497 | 6 | ja |
| `VeilMarsh_iron_vein_Exhausted` | 0,762 | 2,037 | 1,633 | 6 | ja |
| `GreyRifts_granite_deposit_Exhausted` | 0,762 | 2,037 | 1,633 | 6 | ja |
| `GreyRifts_iron_vein_Exhausted` | 0,762 | 2,037 | 1,633 | 6 | ja |
| `TwilightGrove_hardwood_tree_Exhausted` | 0,853 | 1,996 | 1,996 | 4 | ja |
| `Marsh_fiber_plant_Active` | 0,906 | 0,665 | 0,672 | 21 | ja |
| `Quarry_stone_deposit_Active` | 1,102 | 2,113 | 1,931 | 9 | ja |
| `GreyRifts_granite_deposit_Active` | 1,301 | 2,037 | 1,843 | 9 | ja |
| `TwilightGrove_swamp_hemp_Active` | 1,405 | 1,480 | 1,400 | 24 | ja |
| `VeilMarsh_swamp_hemp_Active` | 1,405 | 1,480 | 1,400 | 24 | ja |
| `GreyRifts_iron_vein_Active` | 1,436 | 2,037 | 1,843 | 14 | ja |
| `VeilMarsh_iron_vein_Active` | 1,436 | 2,037 | 1,843 | 14 | ja |
| `EmberRuins_tree_Active` | 6,000 | 3,880 | 3,603 | 11 | ja |
| `Quarry_tree_Active` | 6,000 | 3,880 | 3,603 | 10 | ja |
| `Marsh_tree_Active` | 6,000 | 4,938 | 3,603 | 11 | ja |
| `Greenwood_tree_Active` | 6,000 | 3,880 | 3,603 | 9 | ja |
| `TwilightGrove_hardwood_tree_Active` | 6,685 | 3,540 | 2,956 | 6 | ja |

### Forge — 8 Prefabs

| Prefab | Höhe m | Breite m | Tiefe m | Renderer | Decal |
| --- | ---: | ---: | ---: | ---: | :-: |
| `SmallRewardChest_2D` | 0,780 | 1,007 | 0,748 | 12 | ja |
| `SupplyChest_2D` | 0,784 | 1,251 | 0,858 | 12 | ja |
| `OptionalChest_2D` | 0,862 | 1,325 | 0,902 | 14 | ja |
| `RecoveryContainer_2D` | 0,961 | 1,537 | 0,902 | 17 | ja |
| `MediumRewardChest_2D` | 0,978 | 1,272 | 0,902 | 14 | ja |
| `EliteChest_2D` | 1,060 | 1,505 | 1,012 | 16 | ja |
| `CompletionChest_2D` | 1,259 | 1,717 | 1,122 | 18 | ja |
| `LargeRewardChest_2D` | 1,259 | 1,643 | 1,122 | 17 | ja |

### Level01 — 9 Prefabs

| Prefab | Höhe m | Breite m | Tiefe m | Renderer | Decal |
| --- | ---: | ---: | ---: | ---: | :-: |
| `BLD_Floor_L01` | 0,160 | 1,000 | 0,960 | 18 | **nein** |
| `BLD_FarmPlot_L01` | 0,621 | 2,000 | 2,000 | 26 | ja |
| `BLD_CookingPot_L01` | 1,000 | 0,916 | 0,936 | 28 | ja |
| `BLD_Stonecutter_L01` | 1,400 | 0,905 | 0,850 | 9 | ja |
| `BLD_Ropewalk_L01` | 1,600 | 0,900 | 0,912 | 9 | ja |
| `BLD_Smelter_L01` | 2,000 | 0,980 | 0,960 | 12 | ja |
| `BLD_Sawmill_L01` | 2,200 | 0,900 | 0,900 | 9 | ja |
| `BLD_Wall_L01` | 2,600 | 1,360 | 0,400 | 16 | ja |
| `BLD_Door_L01` | 2,600 | 1,360 | 0,400 | 18 | ja |

### Stations — 2 Prefabs

| Prefab | Höhe m | Breite m | Tiefe m | Renderer | Decal |
| --- | ---: | ---: | ---: | ---: | :-: |
| `StorageChest` | 0,800 | 1,000 | 0,802 | 22 | ja |
| `Workbench` | 1,100 | 1,000 | 0,904 | 37 | ja |

### StyleProof — 16 Prefabs

| Prefab | Höhe m | Breite m | Tiefe m | Renderer | Decal |
| --- | ---: | ---: | ---: | ---: | :-: |
| `SP_GroundCover_Moss` | 0,120 | 1,200 | 1,137 | 6 | **nein** |
| `SP_GroundCover_Grass` | 0,150 | 1,188 | 1,200 | 8 | **nein** |
| `SP_Accent_GlowMushrooms` | 0,350 | 0,443 | 0,425 | 8 | ja |
| `SP_Plant_Flowers` | 0,400 | 0,391 | 0,405 | 10 | ja |
| `SP_Rock_Small` | 0,503 | 1,443 | 1,684 | 2 | ja |
| `SP_Plant_Fern` | 0,700 | 1,082 | 0,972 | 6 | ja |
| `SP_EidrenRune` | 0,800 | 0,650 | 0,180 | 4 | ja |
| `SP_Plant_Bush` | 0,900 | 1,247 | 1,109 | 1 | ja |
| `SP_Rock_Medium` | 1,106 | 2,113 | 1,709 | 3 | ja |
| `SP_Rock_Large` | 2,403 | 2,185 | 2,013 | 5 | ja |
| `SP_RuinWall_B` | 2,600 | 2,000 | 0,700 | 4 | ja |
| `SP_RuinWall_A` | 2,600 | 2,600 | 0,700 | 5 | ja |
| `SP_Tree_C` | 4,000 | 2,270 | 1,940 | 3 | ja |
| `SP_RuinMonument` | 5,000 | 1,478 | 1,386 | 4 | ja |
| `SP_Tree_B` | 6,000 | 3,326 | 3,049 | 6 | ja |
| `SP_Tree_A` | 7,500 | 2,956 | 2,679 | 6 | ja |

### Visuals — 18 Prefabs

| Prefab | Höhe m | Breite m | Tiefe m | Renderer | Decal |
| --- | ---: | ---: | ---: | ---: | :-: |
| `SwampHemp_Exhausted` | 0,228 | 1,054 | 1,294 | 7 | ja |
| `FiberPlant_Exhausted` | 0,250 | 0,370 | 0,334 | 5 | ja |
| `StoneDeposit_Exhausted` | 0,503 | 2,071 | 1,683 | 4 | ja |
| `Tree_Exhausted` | 0,700 | 1,517 | 1,497 | 3 | ja |
| `BerryBush_Exhausted` | 0,700 | 0,878 | 0,785 | 1 | ja |
| `GraniteDeposit_Exhausted` | 0,749 | 2,037 | 1,633 | 4 | ja |
| `IronVein_Exhausted` | 0,749 | 2,037 | 1,633 | 4 | ja |
| `HardwoodTree_Exhausted` | 0,840 | 1,996 | 1,996 | 2 | ja |
| `BerryBush_Active` | 0,900 | 1,201 | 1,062 | 10 | ja |
| `FiberPlant_Active` | 0,906 | 0,665 | 0,672 | 18 | ja |
| `StoneDeposit_Active` | 1,102 | 2,113 | 1,931 | 7 | ja |
| `GraniteDeposit_Active` | 1,266 | 2,037 | 1,843 | 7 | ja |
| `CopperVein_Active` | 1,302 | 2,124 | 1,943 | 12 | ja |
| `CopperVein_Exhausted` | 1,302 | 2,124 | 1,943 | 7 | ja |
| `SwampHemp_Active` | 1,378 | 1,480 | 1,400 | 22 | ja |
| `IronVein_Active` | 1,401 | 2,037 | 1,843 | 12 | ja |
| `Tree_Active` | 6,000 | 3,880 | 3,603 | 6 | ja |
| `HardwoodTree_Active` | 6,650 | 3,540 | 2,956 | 4 | ja |

### WorldChests — 3 Prefabs

| Prefab | Höhe m | Breite m | Tiefe m | Renderer | Decal |
| --- | ---: | ---: | ---: | ---: | :-: |
| `WorldChest_Hidden` | 0,535 | 1,208 | 0,954 | 19 | ja |
| `WorldChest_Common` | 0,594 | 1,219 | 0,792 | 16 | ja |
| `WorldChest_Guarded` | 0,770 | 1,537 | 0,968 | 21 | ja |


